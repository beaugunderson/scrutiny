using System;
using System.Runtime.InteropServices;

namespace NTFS
{
    [Serializable]
    public class UsnJournalEntry : IComparable<UsnJournalEntry>
    {
        private const UInt32 FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

        public string OldName
        {
            get
            {
                return (UsnRecord.FileAttributes & 
                    (uint)Enums.UsnReasonCode.USN_REASON_RENAME_OLD_NAME) != 0 ? Name : null;
            }
        }

        public bool IsFolder
        {
            get
            {
                return (UsnRecord.FileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0;
            }
        }

        public bool IsFile
        {
            get
            {
                return (UsnRecord.FileAttributes & FILE_ATTRIBUTE_DIRECTORY) == 0;
            }
        }

        public Types.USN_RECORD UsnRecord
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public UsnJournalEntry(IntPtr pointer)
        {
            object usnRecord = Marshal.PtrToStructure(pointer, typeof(Types.USN_RECORD));

            if (usnRecord == null)
            {
                throw new Exception("usnRecord was null.");
            }

            UsnRecord = (Types.USN_RECORD)usnRecord;

            // We have to marshal this ourselves since the unmanaged code relies on a property of C arrays
            // (C allows you to specify an out-of-bounds array index)
            Name = Marshal.PtrToStringUni(pointer + UsnRecord.FileNameOffset, UsnRecord.FileNameLength / sizeof(char));
        }

        public int CompareTo(UsnJournalEntry other)
        {
            // XXX: Always ignores case
            return string.Compare(Name, other.Name, true);
        }
    }
}