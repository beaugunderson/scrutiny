using System;

namespace NTFS
{
    public class NewUsnRecordEventArgs : EventArgs
    {
        public UsnJournalEntry UsnJournalEntry
        {
            get;
            set;
        }

        public NewUsnRecordEventArgs(UsnJournalEntry usnEntry)
        {
            UsnJournalEntry = usnEntry;
        }
    }
}