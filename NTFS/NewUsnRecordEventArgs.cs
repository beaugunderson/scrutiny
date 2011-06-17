using System;
using NTFS.PInvoke;

namespace NTFS
{
    public class NewUsnRecordEventArgs : EventArgs
    {
        public Win32.UsnEntry UsnEntry
        {
            get;
            set;
        }

        public NewUsnRecordEventArgs(Win32.UsnEntry usnEntry)
        {
            UsnEntry = usnEntry;
        }
    }
}