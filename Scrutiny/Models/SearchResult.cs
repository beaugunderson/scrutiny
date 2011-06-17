﻿using System;
using System.ComponentModel;
using System.IO;

using NTFS;
using NTFS.PInvoke;
using Scrutiny.Utilities;
using Scrutiny.Extensions;

namespace Scrutiny.Models
{
    [Serializable]
    public class SearchResult : INotifyPropertyChanged, IComparable
    {
        public Win32.UsnEntry UsnEntry
        {
            get;
            set;
        }

        private static readonly AlphanumComparator<SearchResult>  AlphanumComparator = new AlphanumComparator<SearchResult>();

        private readonly string _driveName;

        [NonSerialized]
        private UsnJournal _journal;
        private UsnJournal Journal
        {
            get
            {
                return _journal ?? (_journal = new UsnJournal(new DriveInfo(_driveName)));
            }
        }

        public SearchResult(Win32.UsnEntry usnEntry, UsnJournal journal)
        {
            UsnEntry = usnEntry;

            _journal = journal;
            _driveName = journal.RootDirectory.FullName;
        }

        private string _location;
        public string Location
        {
            get
            {
                if (String.IsNullOrEmpty(_location))
                {
                    string path;

                    var result = Journal.GetPathFromFileReference(UsnEntry.ParentFileReferenceNumber, out path);

                    if (result != UsnJournal.UsnJournalReturnCode.USN_JOURNAL_SUCCESS)
                    {
                        _location = "";
                    }
                    else
                    {
                        path = path.TrimStart('\\');

                        _location = Path.Combine(Journal.RootDirectory.FullName, path);
                    }
                }

                return _location;
            }
        }

        private Win32.BY_HANDLE_FILE_INFORMATION? _fileInformation;
        public Win32.BY_HANDLE_FILE_INFORMATION FileInformation
        {
            get
            {
                if (!_fileInformation.HasValue)
                {
                    Journal.GetFileInformation(FilePath, out _fileInformation);
                }

                if (_fileInformation == null)
                {
                    throw new Exception("Failed to get file information");
                }

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
                return Win32.FiletimeToDateTime(FileInformation.LastWriteTime);
            }
        }

        public string Name
        {
           get
           {
               return UsnEntry.Name;
           } 
        }

        public string FilePath
        {
            get
            {
                return Path.Combine(Location, UsnEntry.Name);
            }
        }

        public override string ToString()
        {
            return FilePath;
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