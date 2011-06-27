using System;
using System.Runtime.InteropServices;

namespace NTFS
{
    public static class Types
    {
        /// <summary>
        /// By Handle File Information structure, contains File Attributes(32bits), Creation Time(FileTime),
        /// Last Access Time(FileTime), Last Write Time(FileTime), Volume Serial Number(32bits),
        /// File Size High(32bits), File Size Low(32bits), Number of Links(32bits), File Index High(32bits),
        /// File Index Low(32bits).
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BY_HANDLE_FILE_INFORMATION
        {
            public uint FileAttributes;
            public FileTime CreationTime;
            public FileTime LastAccessTime;
            public FileTime LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        /// <summary>
        /// USN Journal Data structure, contains USN Journal ID(64bits), First USN(64bits), Next USN(64bits),
        /// Lowest Valid USN(64bits), Max USN(64bits), Maximum Size(64bits) and Allocation Delta(64bits).
        /// </summary>
        // XXX: To class? How?
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct USN_JOURNAL_DATA
        {
            public UInt64 UsnJournalID;
            public Int64 FirstUsn;
            public Int64 NextUsn;
            public Int64 LowestValidUsn;
            public Int64 MaxUsn;
            public UInt64 MaximumSize;
            public UInt64 AllocationDelta;
        }

        /// <summary>
        /// MFT Enum Data structure, contains Start File Reference Number(64bits), Low USN(64bits),
        /// High USN(64bits).
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MFT_ENUM_DATA
        {
            public UInt64 StartFileReferenceNumber;
            public Int64 LowUsn;
            public Int64 HighUsn;
        }

        /// <summary>
        /// Contains the Start USN(64bits), Reason Mask(32bits), Return Only on Close flag(32bits),
        /// Time Out(64bits), Bytes To Wait For(64bits), and USN Journal ID(64bits).
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct READ_USN_JOURNAL_DATA
        {
            public Int64 StartUsn;
            public UInt32 ReasonMask;
            public UInt32 ReturnOnlyOnClose;
            public UInt64 Timeout;
            public UInt64 bytesToWaitFor;
            public UInt64 UsnJournalId;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct IO_STATUS_BLOCK
        {
            public uint Status;
            public ulong Information;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct OBJECT_ATTRIBUTES
        {
            public Int32 Length;
            public IntPtr RootDirectory;
            public IntPtr ObjectName;
            public Int32 Attributes;
            public Int32 SecurityDescriptor;
            public Int32 SecurityQualityOfService;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct UNICODE_STRING
        {
            public Int16 Length;
            public Int16 MaximumLength;
            public IntPtr Buffer;
        }

        /// <summary>
        /// We define this struct ourselves only so we can mark it as [Serializable].
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct FileTime
        {
            public uint LowDateTime;
            public uint HighDateTime;
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct USN_RECORD
        {
            public UInt32 RecordLength;
            public UInt16 MajorVersion;
            public UInt16 MinorVersion;
            public UInt64 FileReferenceNumber;
            public UInt64 ParentFileReferenceNumber;
            public Int64 Usn;
            public Int64 TimeStamp;  // strictly, this is a LARGE_INTEGER in C
            public UInt32 Reason;
            public UInt32 SourceInfo;
            public UInt32 SecurityId;
            public UInt32 FileAttributes;
            public UInt16 FileNameLength;
            public UInt16 FileNameOffset;
            // immediately after the FileNameOffset comes an array of WCHARs containing the FileName
        }
    }
}