using System;

namespace NTFS
{
    public class NewUsnRecordEventArgs : EventArgs
    {
        public NativeMethods.UsnEntry UsnEntry
        {
            get;
            set;
        }

        public NewUsnRecordEventArgs(NativeMethods.UsnEntry usnEntry)
        {
            UsnEntry = usnEntry;
        }
    }
}