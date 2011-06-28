using System;
using System.ComponentModel;
using System.IO;
using System.Threading;

using NTFS;
using NTFS.Extensions;

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

        public SearchResult(UsnJournalEntry usnEntry, UsnJournal journal)
        {
            UsnJournalEntry = usnEntry;

            _journal = journal;
            _driveName = journal.VolumeName;
        }

        public UsnJournalEntry UsnJournalEntry
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
                    string path = Journal.GetPathFromFileReference(UsnJournalEntry.UsnRecord.ParentFileReferenceNumber);

                    if (path == null)
                    {
                        return string.Empty;
                    }

                    return System.IO.Path.Combine(Journal.VolumeName, path.TrimStart('\\'));
                });

                return _path;
            }
        }

        private Types.BY_HANDLE_FILE_INFORMATION _fileInformation;
        public Types.BY_HANDLE_FILE_INFORMATION FileInformation
        {
            get
            {
                //LazyInitializer.EnsureInitialized(ref _fileInformation, delegate
                //{
                var fileInformation = UsnJournal.GetFileInformation(PathAndName);

                _fileInformation = fileInformation;
                //});

                return _fileInformation;
            }
        }

        public Int64 SizeInBytes
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
                return SizeInBytes.FormatBytes();
            }
        }

        public DateTime LastModified
        {
            get
            {
                return FileInformation.LastWriteTime.ToDateTime();
            }
        }

        public string Name
        {
           get
           {
               return UsnJournalEntry.Name;
           } 
        }

        public string PathAndName
        {
            get
            {
                return System.IO.Path.Combine(Path, UsnJournalEntry.Name);
            }
        }

        public override string ToString()
        {
            return PathAndName;
        }

        public int CompareTo(object other)
        {
            return AlphanumComparator.Compare(this, other as SearchResult);
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