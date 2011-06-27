namespace NTFS
{
    public static class Enums
    {
        public enum UsnJournalReturnCode
        {
            INVALID_HANDLE_VALUE = -1,
            USN_JOURNAL_SUCCESS = 0,
            ERROR_INVALID_FUNCTION = 1,
            ERROR_FILE_NOT_FOUND = 2,
            ERROR_PATH_NOT_FOUND = 3,
            ERROR_TOO_MANY_OPEN_FILES = 4,
            ERROR_ACCESS_DENIED = 5,
            ERROR_INVALID_HANDLE = 6,
            ERROR_INVALID_DATA = 13,
            ERROR_HANDLE_EOF = 38,
            ERROR_NOT_SUPPORTED = 50,
            ERROR_INVALID_PARAMETER = 87,
            ERROR_JOURNAL_DELETE_IN_PROGRESS = 1178,
            USN_JOURNAL_NOT_ACTIVE = 1179,
            ERROR_JOURNAL_ENTRY_DELETED = 1181,
            ERROR_INVALID_USER_BUFFER = 1784,
            USN_JOURNAL_INVALID = 17001,
            VOLUME_NOT_NTFS = 17003,
            INVALID_FILE_REFERENCE_NUMBER = 17004,
            USN_JOURNAL_ERROR = 17005
        }

        public enum UsnReasonCode : uint
        {
            USN_REASON_DATA_OVERWRITE = 0x00000001,
            USN_REASON_DATA_EXTEND = 0x00000002,
            USN_REASON_DATA_TRUNCATION = 0x00000004,
            USN_REASON_NAMED_DATA_OVERWRITE = 0x00000010,
            USN_REASON_NAMED_DATA_EXTEND = 0x00000020,
            USN_REASON_NAMED_DATA_TRUNCATION = 0x00000040,
            USN_REASON_FILE_CREATE = 0x00000100,
            USN_REASON_FILE_DELETE = 0x00000200,
            USN_REASON_EA_CHANGE = 0x00000400,
            USN_REASON_SECURITY_CHANGE = 0x00000800,
            USN_REASON_RENAME_OLD_NAME = 0x00001000,
            USN_REASON_RENAME_NEW_NAME = 0x00002000,
            USN_REASON_INDEXABLE_CHANGE = 0x00004000,
            USN_REASON_BASIC_INFO_CHANGE = 0x00008000,
            USN_REASON_HARD_LINK_CHANGE = 0x00010000,
            USN_REASON_COMPRESSION_CHANGE = 0x00020000,
            USN_REASON_ENCRYPTION_CHANGE = 0x00040000,
            USN_REASON_OBJECT_ID_CHANGE = 0x00080000,
            USN_REASON_REPARSE_POINT_CHANGE = 0x00100000,
            USN_REASON_STREAM_CHANGE = 0x00200000,
            USN_REASON_CLOSE = 0x80000000
        }

        public enum GetLastErrorEnum
        {
            INVALID_HANDLE_VALUE = -1,
            ERROR_SUCCESS = 0,
            ERROR_INVALID_FUNCTION = 1,
            ERROR_FILE_NOT_FOUND = 2,
            ERROR_PATH_NOT_FOUND = 3,
            ERROR_TOO_MANY_OPEN_FILES = 4,
            ERROR_ACCESS_DENIED = 5,
            ERROR_INVALID_HANDLE = 6,
            ERROR_INVALID_DATA = 13,
            ERROR_HANDLE_EOF = 38,
            ERROR_NOT_SUPPORTED = 50,
            ERROR_INVALID_PARAMETER = 87,
            ERROR_JOURNAL_DELETE_IN_PROGRESS = 1178,
            ERROR_JOURNAL_NOT_ACTIVE = 1179,
            ERROR_JOURNAL_ENTRY_DELETED = 1181,
            ERROR_INVALID_USER_BUFFER = 1784
        }

        public enum FILE_INFO_BY_HANDLE_CLASS {
            FileBasicInfo = 0,
            FileStandardInfo = 1,
            FileNameInfo = 2,
            FileRenameInfo = 3,
            FileDispositionInfo = 4,
            FileAllocationInfo = 5,
            FileEndOfFileInfo = 6,
            FileStreamInfo = 7,
            FileCompressionInfo = 8,
            FileAttributeTagInfo = 9,
            FileIdBothDirectoryInfo = 10,  // 0xA
            FileIdBothDirectoryRestartInfo = 11,  // 0xB
            FileIoPriorityHintInfo = 12,  // 0xC
            FileRemoteProtocolInfo = 13,  // 0xD
            MaximumFileInfoByHandlesClass = 14   // 0xE
        }
    }
}