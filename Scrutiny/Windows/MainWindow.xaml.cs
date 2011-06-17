using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

using NTFS;
using NTFS.PInvoke;

using Scrutiny.Models;
using Scrutiny.Utilities;
using Scrutiny.WPF;
using Scrutiny.Extensions;

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

        public bool CaseSensitive { get; set; }
        public bool SearchInPath { get; set; }

        private const int SearchTimeout = 250;

        private readonly BlockingCollection<DescriptiveTask> _tasks = new BlockingCollection<DescriptiveTask>();
        private Task _taskConsumer;

        private readonly TaskScheduler _uiTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

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

        private DispatcherSynchronizationContext _uiSynchronizationContext;

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

        public MainWindow()
        {
            InitializeComponent();

            _uiSynchronizationContext = new DispatcherSynchronizationContext(Application.Current.Dispatcher);
            
            //SynchronizationContext.SetSynchronizationContext(context);

            _taskConsumer = Task.Factory.StartNew(() => ConsumeTasks(_tasks));
            
            SearchResults = new ThreadSafeObservableCollection<SearchResult>(null, 750000);
            DisplaySearchResults = new ThreadSafeObservableCollection<SearchResult>();

            CaseSensitive = true;
            SearchInPath = false;
            
            _updateFilter = new DeferredAction(FilterResults);

            string[] drives = { "C" };

            _journals = new List<UsnJournal>();

            DataContext = this;

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            _tasks.Add(new DescriptiveTask(delegate
            {
                foreach (var drive in drives)
                {
                    _journals.Add(new UsnJournal(new DriveInfo(drive)));
                }
            }, "Enumerating journals"));
        }

        private void ConsumeTasks(BlockingCollection<DescriptiveTask> tasks)
        {
            while (!tasks.IsCompleted)
            {
                foreach (var task in tasks.GetConsumingEnumerable()) {
                    var currentTask = task;

                    UpdateStatus(task.Description);

                    Console.WriteLine("Starting task: {0}", task.Description);

                    if (currentTask.IsUiTask)
                    {
                        currentTask.Start(_uiTaskScheduler);
                    }
                    else
                    {
                        currentTask.Start();
                    }
                }
            }
        }

        private void FilterResults()
        {
            if (string.IsNullOrWhiteSpace(searchTextBox.Text))
            {
                DisplaySearchResults = new ThreadSafeObservableCollection<SearchResult>();
            }
            else
            {
                UpdateStatus("Filtering");

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

                UpdateStatus("Idle");
            }
        }

        private bool SearchNameCaseInsensitive(SearchResult result)
        {
            if (result.Name.ToLower().Contains(SearchTerm.ToLower()))
            {
                return true;
            }

            return false;
        }

        private bool SearchNameCaseInsensitiveMultiple(SearchResult result)
        {
            var terms = SearchTerm.ToLower().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            // Could also use terms.Any
            return terms.All(term => result.Name.Contains(term));
        }

        private bool SearchName(SearchResult result)
        {
            if (result.Name.Contains(SearchTerm))
            {
                return true;
            }

            return false;
        }

        private bool SearchNameMultiple(SearchResult result)
        {
            var terms = SearchTerm.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            // Could also use terms.Any
            return terms.All(term => result.Name.Contains(term));
        }

        private void UpdateStatus(object format, params object[] text)
        {
            InvokeIfNeeded(() => 
                statusTextBlock.Text = string.Format(Convert.ToString(format), text.Select(Convert.ToString)));
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

                _tasks.Add(new DescriptiveTask(() => ScanDirectories(localJournal),
                    string.Format("Scanning {0} files", journal.RootDirectory)));

                _tasks.Add(new DescriptiveTask(() => ScanFiles(localJournal),
                    string.Format("Scanning {0} folders", journal.RootDirectory)));
            }

            UpdateStatus("Idle");
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
            }, "Loading cache");

            if (File.Exists(Paths.CombineBaseDirectory(JournalCacheName)))
            {
                _tasks.Add(task);
            }

            _tasks.Add(new DescriptiveTask(delegate
            {
                task.Wait();

                if (SearchResults.Count == 0)
                {
                    RefreshUsnJournal();
                }
            }, "Refreshing USN journal"));
        }

        private void resultsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void OpenLocation_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            _tasks.Add(new DescriptiveTask(() => 
                 DisplaySearchResults.Sort(item => item.Location, new AlphanumComparator<string>()), 
                "Sorting items"));
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
            SearchResults.SerializeToList(Paths.CombineBaseDirectory(JournalCacheName));
        }
    }

    /// <summary>
    /// Takes two values, one being a string and the other being a search term to highlight
    /// </summary>
    /// <returns>
    /// A <see cref="TextBlock"/> with Inlines collection filled with <see cref="Run"/> elements.
    /// </returns>
    public class HighlightConverter : IMultiValueConverter
    {
        private readonly ColorCombination[] _colorCombinations = new[]
        {
            new ColorCombination(new SolidColorBrush(Colors.GreenYellow), new SolidColorBrush(Colors.Green)), 
            new ColorCombination(new SolidColorBrush(Colors.OrangeRed), new SolidColorBrush(Colors.DarkRed)), 
            new ColorCombination(new SolidColorBrush(Colors.Yellow), new SolidColorBrush(Colors.DarkOrange)) 
        };

        private class ColorMatch
        {
            public int ColorIndex
            {
                get;
                set;
            }

            public Match Match
            {
                get;
                set;
            }

            public ColorMatch(Match match, int colorIndex)
            {
                Match = match;
                ColorIndex = colorIndex;
            }
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)(DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue))
            {
                return values;
            }

            var value = System.Convert.ToString(values[0]);

            if (values[1] == DependencyProperty.UnsetValue)
            {
                return value;
            }

            var terms = (string)values[1];

            if (string.IsNullOrEmpty(terms))
            {
                return value;
            }

            // Highlight the search terms
            int count = 0;

            var colorMatches = new List<ColorMatch>();

            foreach (var term in terms.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries))
            {
                var matches = Regex.Matches(value, Regex.Escape(term));

                colorMatches.AddRange(from Match match in matches select new ColorMatch(match, count));

                count++;
            }

            colorMatches.Sort(item => item.Match.Index);

            var textBlock = new TextBlock
            {
                Padding = new Thickness(0, 1, 0, 1)
            };

            int index = 0;

            foreach (var match in colorMatches)
            {
                textBlock.Inlines.Add(value.Slice(index, match.Match.Index));

                index = match.Match.Index + match.Match.Length;

                // TODO: Change look to a user setting
                var border = new Border
                {
                    Background = _colorCombinations[match.ColorIndex].Background,
                    BorderBrush = _colorCombinations[match.ColorIndex].Border,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0.5, -1, 0.5, -1),
                    CornerRadius = new CornerRadius(1),
                    Child = new TextBlock(new Run(match.Match.Value))
                    {
                        Foreground = _colorCombinations[match.ColorIndex].Foreground,
                        Padding = new Thickness(0)
                    }
                };

                var container = new InlineUIContainer(border)
                {
                    BaselineAlignment = BaselineAlignment.Bottom,
                    FontWeight = FontWeights.Bold
                };

                textBlock.Inlines.Add(container);
            }

            textBlock.Inlines.Add(value.Slice(index, value.Length));

            return textBlock;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}