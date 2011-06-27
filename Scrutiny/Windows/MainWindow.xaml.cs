using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using NTFS;

using Scrutiny.Models;
using Scrutiny.Properties;
using Scrutiny.Utilities;
using Scrutiny.WPF;

namespace Scrutiny.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private readonly List<UsnJournal> _journals;
        private readonly DeferredAction _updateFilter;
        
        private ThreadSafeObservableCollection<SearchResult> _searchResults;
        private ThreadSafeObservableCollection<SearchResult> _displaySearchResults;

        // XXX: Is it silly to have a field too?
        private string _searchTerm;
        private string _searchTermLower;

        private string[] _searchTerms;
        private string[] _searchTermsLower;
        
        private readonly Dictionary<PredicateOption, Func<SearchResult, bool>> _predicates;

        private const int SearchTimeout = 250;

        private readonly BlockingCollection<DescriptiveTask> _tasks = new BlockingCollection<DescriptiveTask>();
        private readonly List<DescriptiveTask> _runningTasks = new List<DescriptiveTask>();
        
        private Task _taskConsumer;

        private readonly DispatcherSynchronizationContext _uiSynchronizationContext;

        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private const string JournalCacheName = "Journal.cache";

        private bool _descending;

        private readonly PropertyChangeNotifier _searchTermNotifier;
        private GridViewColumnHeader _lastColumnClicked;

        #region Dependency Properties
        public ThreadSafeObservableCollection<SearchResult> SearchResults
        {
            get
            {
                return _searchResults;
            }

            private set
            {
                _searchResults = value;

                OnPropertyChanged("SearchResults");
            }
        }

        public ThreadSafeObservableCollection<SearchResult> DisplaySearchResults
        {
            get
            {
                return _displaySearchResults;
            }

            private set {
                _displaySearchResults = value;
            
                OnPropertyChanged("DisplaySearchResults");
            }
        }

        public static readonly DependencyProperty IsRegularExpressionProperty =
            DependencyProperty.Register("IsRegularExpression", typeof (bool), typeof (MainWindow), new PropertyMetadata(default(bool)));

        public bool IsRegularExpression
        {
            get
            {
                return (bool) GetValue(IsRegularExpressionProperty);
            }
            set
            {
                SetValue(IsRegularExpressionProperty, value);
            }
        }

        public static readonly DependencyProperty IsCaseSensitiveProperty =
            DependencyProperty.Register("IsCaseSensitive", typeof (bool), typeof (MainWindow), new PropertyMetadata(default(bool)));

        public bool IsCaseSensitive
        {
            get
            {
                return (bool) GetValue(IsCaseSensitiveProperty);
            }
            set
            {
                SetValue(IsCaseSensitiveProperty, value);
            }
        }

        public static readonly DependencyProperty IsMultipleTermsProperty =
            DependencyProperty.Register("IsMultipleTerms",
                typeof (bool),
                typeof (MainWindow),
                new PropertyMetadata(default(bool)));

        public bool IsMultipleTerms
        {
            get
            {
                return (bool) GetValue(IsMultipleTermsProperty);
            }
            set
            {
                SetValue(IsMultipleTermsProperty, value);
            }
        }

        public static readonly DependencyProperty CurrentResultProperty =
            DependencyProperty.Register("CurrentResult",
                typeof(SearchResult),
                typeof(MainWindow),
                new PropertyMetadata(default(SearchResult)));

        public SearchResult CurrentResult
        {
            get
            {
                return (SearchResult)GetValue(CurrentResultProperty);
            }

            set
            {
                SetValue(CurrentResultProperty, value);
            }
        }

        public static readonly DependencyProperty SearchTermProperty =
            DependencyProperty.Register("SearchTerm",
                typeof(string),
                typeof(MainWindow),
                new PropertyMetadata(default(string)));

        public string SearchTerm
        {
            get
            {
                return (string)GetValue(SearchTermProperty);
            }

            set
            {
                SetValue(SearchTermProperty, value);
            }
        }

        public static readonly DependencyProperty SearchInNameProperty =
            DependencyProperty.Register("SearchInName", typeof (bool), typeof (MainWindow), new PropertyMetadata(default(bool)));

        public bool SearchInName
        {
            get
            {
                return (bool) GetValue(SearchInNameProperty);
            }
            set
            {
                SetValue(SearchInNameProperty, value);
            }
        }

        public static readonly DependencyProperty SearchInLocationProperty =
            DependencyProperty.Register("SearchInLocation", typeof (bool), typeof (MainWindow), new PropertyMetadata(default(bool)));

        public bool SearchInLocation
        {
            get
            {
                return (bool) GetValue(SearchInLocationProperty);
            }
         
            set
            {
                SetValue(SearchInLocationProperty, value);
            }
        }

        public static readonly DependencyProperty RunningTasksProperty =
            DependencyProperty.Register("RunningTasks",
                typeof (string),
                typeof (MainWindow),
                new PropertyMetadata(default(string)));

        public string RunningTasks
        {
            get
            {
                return (string)GetValue(RunningTasksProperty);
            }

            set
            {
                SetValue(RunningTasksProperty, value);
            }
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            SearchResults = new ThreadSafeObservableCollection<SearchResult>(null, 750000);
            DisplaySearchResults = new ThreadSafeObservableCollection<SearchResult>();

            _uiSynchronizationContext = new DispatcherSynchronizationContext(Application.Current.Dispatcher);
            _taskConsumer = Task.Factory.StartNew(() => ConsumeTasks(_tasks));
            
            _updateFilter = new DeferredAction(FilterResults);

            _journals = new List<UsnJournal>();

            // TODO: Convert to a user setting
            string[] drives = { "C" };

            DataContext = this;

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            _predicates = new Dictionary<PredicateOption, Func<SearchResult, bool>>
            {
                {
                    PredicateOption.None,
                    result => SearchCaseInsensitive(result.Name)
                }, 
                {
                    PredicateOption.MultipleTerms,
                    result => SearchCaseInsensitiveMultiple(result.Name)
                },
                { 
                    PredicateOption.CaseSensitive,
                    result => Search(result.Name) 
                },
                {
                    PredicateOption.CaseSensitive | PredicateOption.MultipleTerms,
                    result => SearchMultiple(result.Name)
                }
            };

            _searchTermNotifier = new PropertyChangeNotifier(this, "SearchTerm");
            _searchTermNotifier.ValueChanged += OnSearchTermChanged;

            AddTask(() => EnumerateJournals(drives), "Enumerating journals");
        }

        private void OnSearchTermChanged(object sender, EventArgs eventArgs)
        {
            _searchTerm = (string)_searchTermNotifier.Value;
            _searchTermLower = _searchTerm.ToLower();

            _searchTerms = _searchTerm.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            _searchTermsLower = _searchTerms.Select(s => s.ToLower()).ToArray();
        
            _updateFilter.Defer(SearchTimeout);
        }

        private void EnumerateJournals(IEnumerable<string> drives)
        {
            foreach (var drive in drives)
            {
                _journals.Add(new UsnJournal(new DriveInfo(drive)));
            }
        }

        // TODO: Move to class
        private void AddTask(Action action, string description)
        {
            _tasks.Add(new DescriptiveTask(action, _tokenSource.Token, description));
        }

        // TODO: Move to class
        private void AddTask(DescriptiveTask task)
        {
            _tasks.Add(task);
        }

        private void ConsumeTasks(BlockingCollection<DescriptiveTask> tasks)
        {
            while (!tasks.IsCompleted)
            {
                foreach (var task in tasks.GetConsumingEnumerable()) {
                    var currentTask = task;

                    currentTask.Start();

                    _runningTasks.Add(currentTask);

                    UpdateRunningTasks();

                    currentTask.ContinueWith(result =>
                    {
                        _runningTasks.Remove(currentTask);

                        UpdateRunningTasks();
                    });
                }
            }
        }

        /// <summary>
        /// Update the status bar text with the list of running tasks.
        /// </summary>
        private void UpdateRunningTasks()
        {
            var runningTasks = string.Join(", ", _runningTasks
                .Select(task => task.Description).ToList());

            InvokeIfNeeded(() =>
            {
                RunningTasks = String.IsNullOrEmpty(runningTasks) ? "Idle" : runningTasks;
            });
        }

        /// <summary>
        /// Filter the entire list of USN journal entries and display them in the ListView.
        /// </summary>
        private void FilterResults()
        {
            if (string.IsNullOrWhiteSpace(searchTextBox.Text))
            {
                DisplaySearchResults = new ThreadSafeObservableCollection<SearchResult>();
            }
            else
            {
                IEnumerable<SearchResult> list;

                // TODO: Benchmark parallel vs. non-parallel
                // TODO: Benchmark ToList().FindAll() vs. .Where()
                var options = GetOptions();
                var predicate = _predicates[options];

                if (Settings.Default.Parallel)
                {
                    list = SearchResults.AsParallel().Where(predicate);
                }
                else
                {
                    list = SearchResults.Where(predicate);
                }

                DisplaySearchResults = new ThreadSafeObservableCollection<SearchResult>(list);
            }
        }

        private PredicateOption GetOptions()
        {
            var option = PredicateOption.None;

            if (IsMultipleTerms)
            {
                option = option | PredicateOption.MultipleTerms;
            }

            if (IsRegularExpression)
            {
                option = option | PredicateOption.RegularExpression;
            }

            if (IsCaseSensitive)
            {
                option = option | PredicateOption.CaseSensitive;
            }

            return option;
        }

        private string ActiveFields(SearchResult searchResult)
        {
            if (SearchInName && SearchInLocation)
            {
                return searchResult.PathAndName;
            }

            if (SearchInName)
            {
                return searchResult.Name;
            }

            if (SearchInLocation)
            {
                return searchResult.Path;
            }

            throw new ArgumentException();
        }

        // TODO: Move search predicates to a utility class
        private bool Search(string field)
        {
            return field.Contains(_searchTerm);
        }

        private bool SearchMultiple(string field)
        {
            // Could also use terms.Any
            return _searchTerms.All(field.Contains);
        }

        private bool SearchCaseInsensitive(string field)
        {
            return field.ToLower().Contains(_searchTermLower);
        }

        private bool SearchCaseInsensitiveMultiple(string field)
        {
            // Could also use terms.Any
            return _searchTermsLower.All(field.ToLower().Contains);
        }

        private void InvokeIfNeeded(Action action)
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                Dispatcher.Invoke(action);
            }
            else
            {
                action();
            }
        }

        private void ScanDirectories(UsnJournal journal)
        {
            if (Settings.Default.Parallel)
            {
                journal.GetNtfsVolumeFolders()
                    .AsParallel()
                    .WithDegreeOfParallelism(10)
                    .ForAll(entry => SearchResults.Add(new SearchResult(entry, journal)));
            }
            else
            {
                journal.GetNtfsVolumeFolders()
                    .ToList()
                    .ForEach(entry => SearchResults.Add(new SearchResult(entry, journal)));
            }
        }

        private void ScanFiles(UsnJournal journal)
        {
            if (Settings.Default.Parallel)
            {
                journal.GetFilesMatchingFilter("*")
                    .AsParallel()
                    .WithDegreeOfParallelism(10)
                    .ForAll(entry => SearchResults.Add(new SearchResult(entry, journal)));
            }
            else
            {
                journal.GetFilesMatchingFilter("*")
                    .ToList()
                    .ForEach(entry => SearchResults.Add(new SearchResult(entry, journal)));
            }
        }

        private void RefreshUsnJournal()
        {
            foreach (var journal in _journals)
            {
                var localJournal = journal;

                AddTask(() => ScanDirectories(localJournal),
                    string.Format("Scanning {0} files", journal.RootDirectory));

                AddTask(() => ScanFiles(localJournal),
                    string.Format("Scanning {0} folders", journal.RootDirectory));
            }
        }

/*
        private ListCollectionView GetView()
        {
            return (ListCollectionView)CollectionViewSource.GetDefaultView(DisplaySearchResults);
        }
*/

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var task = new DescriptiveTask(delegate
            {
                try
                {
                    SearchResults =
                        ThreadSafeObservableCollection<SearchResult>.DeserializeFromList(
                            Paths.CombineBaseDirectory(JournalCacheName), _uiSynchronizationContext);
                }
                catch (SerializationException se)
                {
                    Console.WriteLine("Unable to deserialize the cache.");
                }
            },
            _tokenSource.Token,
            "Loading cache");

            if (File.Exists(Paths.CombineBaseDirectory(JournalCacheName)))
            {
                AddTask(task);

                AddTask(delegate
                {
                    // XXX: Use ContinueWith here?
                    task.Wait();

                    if (SearchResults.Count == 0)
                    {
                        RefreshUsnJournal();
                    }
                }, "Refreshing USN journal");
            }
            else
            {
                AddTask(RefreshUsnJournal, "Refreshing USN journal");
            }
        }

        private void resultsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void OpenLocation_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentResult == null)
            {
                return;
            }

            ShowSelectedInExplorer.FileOrFolder(CurrentResult.PathAndName);
        }

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            var column = sender as GridViewColumnHeader;

            if (column == null)
            {
                return;
            }

            Func<SearchResult, object> function = item => item.Path;
            IComparer<object> comparer = null;

            switch (column.Name)
            {
                case "FileName":
                    function = item => item.Name;
                    comparer = new AlphanumComparator<object>();

                    break;
                case "Location":
                    function = item => item.Path;
                    comparer = new AlphanumComparator<object>();

                    break;
                case "Size":
                    function = item => item.SizeInBytes;

                    break;
                case "LastModified":
                    function = item => item.LastModified;

                    break;
            }

            if (column != _lastColumnClicked)
            {
                _descending = false;

                if (_lastColumnClicked != null)
                {
                    _lastColumnClicked.ContentTemplate = null;
                }
            }
            else
            {
                _descending = !_descending;
            }

            // TODO: Cancel other running sort jobs
            if (_descending)
            {
                column.ContentTemplate = Resources["DescendingTemplate"] as DataTemplate;

                AddTask(() => DisplaySearchResults.SortDescending(function, comparer), "Sorting items");
            }
            else
            {
                column.ContentTemplate = Resources["AscendingTemplate"] as DataTemplate;

                AddTask(() => DisplaySearchResults.Sort(function, comparer), "Sorting items");
            }

            _lastColumnClicked = column;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _tokenSource.Cancel();

            // TODO: Display progress window
            SearchResults.SerializeToList(Paths.CombineBaseDirectory(JournalCacheName));
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}