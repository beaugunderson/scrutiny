using System;
using System.ComponentModel;
using System.IO;
using System.Threading;

using NTFS;

using Scrutiny.Utilities;
using Scrutiny.Extensions;

namespace Scrutiny.Models
{
    // TODO: Do we need INotifyPropertyChanged?
    [Serializable]
    public class SearchResult : INotifyPropertyChanged, IComparable
    {
        private static readonly AlphanumComparator<SearchResult>  AlphanumComparator = new AlphanumComparator<SearchResult>();

        private readonly string _driveName;

        public SearchResult(NativeMethods.UsnEntry usnEntry, UsnJournal journal)
        {
            UsnEntry = usnEntry;

            _journal = journal;
            _driveName = journal.VolumeName;
        }

        public NativeMethods.UsnEntry UsnEntry
        {
            get;
            set;
        }
        
        [NonSerialized]
        private UsnJournal _journal;
        private UsnJournal Journal
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _journal, () => new UsnJournal(new DriveInfo(_driveName)));

                return _journal;
            }
        }

        private string _path;
        public string Path
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _path, delegate
                {
                    string path = Journal.GetPathFromFileReference(UsnEntry.ParentFileReferenceNumber);

                    if (path == null)
                    {
                        return string.Empty;
                    }

                    return System.IO.Path.Combine(Journal.RootDirectory.FullName, path.TrimStart('\\'));
                });

                return _path;
            }
        }

        private NativeMethods.BY_HANDLE_FILE_INFORMATION? _fileInformation;
        public NativeMethods.BY_HANDLE_FILE_INFORMATION FileInformation
        {
            get
            {
                //LazyInitializer.EnsureInitialized(ref _fileInformation, delegate
                //{
                NativeMethods.BY_HANDLE_FILE_INFORMATION? fileInformation;

                Journal.GetFileInformation(PathAndName, out fileInformation);

                if (!fileInformation.HasValue)
                {
                    throw new ApplicationException("Failed to get file information");
                }

                _fileInformation = fileInformation.Value;
                //});

                return _fileInformation.Value;
            }
        }

        public Int64 Size
        {
            get
            {
                return (((Int64)FileInformation.FileSizeHigh) << 32) + FileInformation.FileSizeLow;
            }
        }

        public string FormattedSize
        {
            get
            {
                return Size.FormatBytes();
            }
        }

        public DateTime LastModified
        {
            get
            {
                return NativeMethods.FiletimeToDateTime(FileInformation.LastWriteTime);
            }
        }

        public string Name
        {
           get
           {
               return UsnEntry.Name;
           } 
        }

        public string PathAndName
        {
            get
            {
                return System.IO.Path.Combine(Path, UsnEntry.Name);
            }
        }

        public override string ToString()
        {
            return PathAndName;
        }

        public int CompareTo(object obj)
        {
            var searchResult = obj as SearchResult;

            if (searchResult == null)
            {
                throw new ArgumentException("Comparison object is not a SearchResult");
            }

            return AlphanumComparator.Compare(this, searchResult);
        }

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}