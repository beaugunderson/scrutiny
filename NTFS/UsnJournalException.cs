using System;

namespace NTFS
{
    [Serializable]
    public class UsnJournalException : Exception
    {
        public UsnJournalException(Enums.UsnJournalReturnCode returnCode)
        {
        }
    }
}