using System;
using System.Runtime.InteropServices;

namespace NTFS
{
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

        public UsnJournalEntry(IntPtr usnEntryPointer)
        {
            UsnRecord = (Types.USN_RECORD) Marshal.PtrToStructure(usnEntryPointer, typeof(Types.USN_RECORD));
        
            // XXX: Is it possible to marshal this automatically?
            Name = Marshal.PtrToStringUni(new IntPtr(usnEntryPointer.ToInt32() + UsnRecord.FileNameOffset), UsnRecord.FileNameLength / sizeof(char));
        }

        public int CompareTo(UsnJournalEntry other)
        {
            // XXX: Always ignores case
            return string.Compare(Name, other.Name, true);
        }
    }
}