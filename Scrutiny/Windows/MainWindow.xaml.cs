using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

using NTFS;
using NTFS.PInvoke;

using Scrutiny.Models;
using Scrutiny.Utilities;

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

        private const int SearchTimeout = 250;

        private readonly BlockingCollection<DescriptiveTask> _tasks = new BlockingCollection<DescriptiveTask>();
        private readonly List<DescriptiveTask> _runningTasks = new List<DescriptiveTask>();
        
        private Task _taskConsumer;

        private readonly DispatcherSynchronizationContext _uiSynchronizationContext;

        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private const string JournalCacheName = "Journal.cache";

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

        public static readonly DependencyProperty CaseSensitiveProperty =
            DependencyProperty.Register("CaseSensitive",
            typeof (bool),
            typeof (MainWindow), 
            new PropertyMetadata(default(bool)));

        public bool CaseSensitive
        {
            get
            {
                return (bool) GetValue(CaseSensitiveProperty);
            }
            
            set
            {
                SetValue(CaseSensitiveProperty, value);
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

            AddTask(() => EnumerateJournals(drives), "Enumerating journals");
        }

        private void EnumerateJournals(IEnumerable<string> drives)
        {
            foreach (var drive in drives)
            {
                _journals.Add(new UsnJournal(new DriveInfo(drive)));
            }
        }

        private void AddTask(Action action, string description)
        {
            _tasks.Add(new DescriptiveTask(action, _tokenSource.Token, description));
        }

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

        private void UpdateRunningTasks()
        {
            var runningTasks = string.Join(", ", _runningTasks
                .Select(task => task.Description).ToList());

            InvokeIfNeeded(() =>
            {
                RunningTasks = String.IsNullOrEmpty(runningTasks) ? "Idle" : runningTasks;
            });
        }

        private void FilterResults()
        {
            if (string.IsNullOrWhiteSpace(searchTextBox.Text))
            {
                DisplaySearchResults = new ThreadSafeObservableCollection<SearchResult>();
            }
            else
            {
                IEnumerable<SearchResult> list;

                if (CaseSensitive)
                {
                    list = SearchResults.ToList().FindAll(SearchNameMultiple);
                }
                else
                {
                    list = SearchResults.ToList().FindAll(SearchNameCaseInsensitiveMultiple);
                }

                DisplaySearchResults = new ThreadSafeObservableCollection<SearchResult>(list);

                // Update the items bound to the search term here since it's deferred from the actual update
                var bindingExpression = searchTextBox.GetBindingExpression(TextBox.TextProperty);

                if (bindingExpression != null)
                {
                    bindingExpression.UpdateSource();
                }
            }
        }

        private bool SearchNameCaseInsensitive(SearchResult result)
        {
            return result.Name.ToLower().Contains(SearchTerm.ToLower());
        }

        private bool SearchNameCaseInsensitiveMultiple(SearchResult result)
        {
            var terms = SearchTerm.ToLower().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            // Could also use terms.Any
            return terms.All(term => result.Name.Contains(term));
        }

        private bool SearchName(SearchResult result)
        {
            return result.Name.Contains(SearchTerm);
        }

        private bool SearchNameMultiple(SearchResult result)
        {
            var terms = SearchTerm.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            // Could also use terms.Any
            return terms.All(term => result.Name.Contains(term));
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
            List<Win32.UsnEntry> folders;

            journal.GetNtfsVolumeFolders(out folders);

            foreach (var folder in folders)
            {
                SearchResults.Add(new SearchResult(folder, journal));
            }
        }

        private void ScanFiles(UsnJournal journal)
        {
            List<Win32.UsnEntry> files;

            journal.GetFilesMatchingFilter("*", out files);

            foreach (var file in files)
            {
                SearchResults.Add(new SearchResult(file, journal));
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

        private ListCollectionView GetView()
        {
            return (ListCollectionView)CollectionViewSource.GetDefaultView(DisplaySearchResults);
        }

        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _updateFilter.Defer(SearchTimeout);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var task = new DescriptiveTask(delegate
            {
                SearchResults = ThreadSafeObservableCollection<SearchResult>.DeserializeFromList(Paths.CombineBaseDirectory(JournalCacheName), _uiSynchronizationContext);
            },
            _tokenSource.Token,
            "Loading cache");

            if (File.Exists(Paths.CombineBaseDirectory(JournalCacheName)))
            {
                AddTask(task);
            }

            AddTask(delegate
            {
                // XXX: What happens if the file didn't exist and the task above never ran?
                task.Wait();

                if (SearchResults.Count == 0)
                {
                    RefreshUsnJournal();
                }
            }, "Refreshing USN journal");
        }

        private void resultsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void OpenLocation_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private bool _descending;
        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            var column = sender as GridViewColumnHeader;

            if (column == null)
            {
                return;
            }

            Func<SearchResult, object> function = item => item.Location;
            IComparer<object> comparer = null;

            switch (column.Name)
            {
                case "Name":
                    function = item => item.Name;
                    comparer = new AlphanumComparator<object>();

                    break;
                case "Location":
                    function = item => item.Location;
                    comparer = new AlphanumComparator<object>();

                    break;
                case "Size":
                    function = item => item.Size;

                    break;
                case "LastModified":
                    function = item => item.LastModified;

                    break;
            }

            if (_descending)
            {
                AddTask(() => DisplaySearchResults.SortDescending(function, comparer), "Sorting items");
            }
            else
            {
                AddTask(() => DisplaySearchResults.Sort(function, comparer), "Sorting items");
            }

            _descending = !_descending;
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