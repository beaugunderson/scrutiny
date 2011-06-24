using System;
using System.IO;
using System.Runtime.InteropServices;

namespace NTFS
{
    public static class NativeMethods
    {
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

        public enum UsnJournalDeleteFlags
        {
            USN_DELETE_FLAG_DELETE = 1,
            USN_DELETE_FLAG_NOTIFY = 2
        }

        public enum FILE_INFORMATION_CLASS
        {
            FileDirectoryInformation = 1,
            FileFullDirectoryInformation = 2,
            FileBothDirectoryInformation = 3,
            FileBasicInformation = 4,
            FileStandardInformation = 5,
            FileInternalInformation = 6,
            FileEaInformation = 7,
            FileAccessInformation = 8,
            FileNameInformation = 9,
            FileRenameInformation = 10,
            FileLinkInformation = 11,
            FileNamesInformation = 12,
            FileDispositionInformation = 13,
            FilePositionInformation = 14,
            FileFullEaInformation = 15,
            FileModeInformation = 16,
            FileAlignmentInformation = 17,
            FileAllInformation = 18,
            FileAllocationInformation = 19,
            FileEndOfFileInformation = 20,
            FileAlternateNameInformation = 21,
            FileStreamInformation = 22,
            FilePipeInformation = 23,
            FilePipeLocalInformation = 24,
            FilePipeRemoteInformation = 25,
            FileMailslotQueryInformation = 26,
            FileMailslotSetInformation = 27,
            FileCompressionInformation = 28,
            FileObjectIdInformation = 29,
            FileCompletionInformation = 30,
            FileMoveClusterInformation = 31,
            FileQuotaInformation = 32,
            FileReparsePointInformation = 33,
            FileNetworkOpenInformation = 34,
            FileAttributeTagInformation = 35,
            FileTrackingInformation = 36,
            FileIdBothDirectoryInformation = 37,
            FileIdFullDirectoryInformation = 38,
            FileValidDataLengthInformation = 39,
            FileShortNameInformation = 40,
            FileHardLinkInformation = 46
        }

        public const Int32 INVALID_HANDLE_VALUE = -1;

        public const UInt32 GENERIC_READ = 0x80000000;
        public const UInt32 GENERIC_WRITE = 0x40000000;
        public const UInt32 FILE_SHARE_READ = 0x00000001;
        public const UInt32 FILE_SHARE_WRITE = 0x00000002;
        public const UInt32 FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

        public const UInt32 CREATE_NEW = 1;
        public const UInt32 CREATE_ALWAYS = 2;
        public const UInt32 OPEN_EXISTING = 3;
        public const UInt32 OPEN_ALWAYS = 4;
        public const UInt32 TRUNCATE_EXISTING = 5;

        public const UInt32 FILE_ATTRIBUTE_NORMAL = 0x80;
        public const UInt32 FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
        public const UInt32 FileNameInformationClass = 9;
        public const UInt32 FILE_OPEN_FOR_BACKUP_INTENT = 0x4000;
        public const UInt32 FILE_OPEN_BY_FILE_ID = 0x2000;
        public const UInt32 FILE_OPEN = 0x1;
        public const UInt32 OBJ_CASE_INSENSITIVE = 0x40;
        //public const OBJ_KERNEL_HANDLE = 0x200;

        // CTL_CODE( DeviceType, Function, Method, Access ) (((DeviceType) << 16) | ((Access) << 14) | ((Function) << 2) | (Method))
        private const UInt32 FILE_DEVICE_FILE_SYSTEM = 0x00000009;
        
        private const UInt32 METHOD_NEITHER = 3;
        private const UInt32 METHOD_BUFFERED = 0;

        private const UInt32 FILE_ANY_ACCESS = 0;
        private const UInt32 FILE_SPECIAL_ACCESS = 0;
        private const UInt32 FILE_READ_ACCESS = 1;
        private const UInt32 FILE_WRITE_ACCESS = 2;

        public const UInt32 FSCTL_GET_OBJECT_ID = 0x9009c;

        // FSCTL_ENUM_USN_DATA = CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 44,  METHOD_NEITHER, FILE_ANY_ACCESS)
        public const UInt32 FSCTL_ENUM_USN_DATA = (FILE_DEVICE_FILE_SYSTEM << 16) | (FILE_ANY_ACCESS << 14) | (44 << 2) | METHOD_NEITHER;

        // FSCTL_READ_USN_JOURNAL = CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 46,  METHOD_NEITHER, FILE_ANY_ACCESS)
        public const UInt32 FSCTL_READ_USN_JOURNAL = (FILE_DEVICE_FILE_SYSTEM << 16) | (FILE_ANY_ACCESS << 14) | (46 << 2) | METHOD_NEITHER;

        //  FSCTL_CREATE_USN_JOURNAL        CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 57,  METHOD_NEITHER, FILE_ANY_ACCESS)
        public const UInt32 FSCTL_CREATE_USN_JOURNAL = (FILE_DEVICE_FILE_SYSTEM << 16) | (FILE_ANY_ACCESS << 14) | (57 << 2) | METHOD_NEITHER;

        //  FSCTL_QUERY_USN_JOURNAL         CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 61, METHOD_BUFFERED, FILE_ANY_ACCESS)
        public const UInt32 FSCTL_QUERY_USN_JOURNAL = (FILE_DEVICE_FILE_SYSTEM << 16) | (FILE_ANY_ACCESS << 14) | (61 << 2) | METHOD_BUFFERED;

        // FSCTL_DELETE_USN_JOURNAL        CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 62, METHOD_BUFFERED, FILE_ANY_ACCESS)
        public const UInt32 FSCTL_DELETE_USN_JOURNAL = (FILE_DEVICE_FILE_SYSTEM << 16) | (FILE_ANY_ACCESS << 14) | (62 << 2) | METHOD_BUFFERED;

        /// <summary>
        /// Creates the file specified by 'lpFileName' with desired access, share mode, security attributes,
        /// creation disposition, flags and attributes.
        /// </summary>
        /// <param name="lpFileName">Fully qualified path to a file</param>
        /// <param name="dwDesiredAccess">Requested access (write, read, read/write, none)</param>
        /// <param name="dwShareMode">Share mode (read, write, read/write, delete, all, none)</param>
        /// <param name="lpSecurityAttributes">IntPtr to a 'SECURITY_ATTRIBUTES' structure</param>
        /// <param name="dwCreationDisposition">Action to take on file or device specified by 'lpFileName' (CREATE_NEW,
        /// CREATE_ALWAYS, OPEN_ALWAYS, OPEN_EXISTING, TRUNCATE_EXISTING)</param>
        /// <param name="dwFlagsAndAttributes">File or device attributes and flags (typically FILE_ATTRIBUTE_NORMAL)</param>
        /// <param name="hTemplateFile">IntPtr to a valid handle to a template file with 'GENERIC_READ' access right</param>
        /// <returns>IntPtr handle to the 'lpFileName' file or device or 'INVALID_HANDLE_VALUE'</returns>
        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
        public static extern IntPtr
            CreateFile(string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        /// <summary>
        /// Closes the file specified by the IntPtr 'hObject'.
        /// </summary>
        /// <param name="hObject">IntPtr handle to a file</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool
            CloseHandle(
            IntPtr hObject);

        /// <summary>
        /// Fills the 'BY_HANDLE_FILE_INFORMATION' structure for the file specified by 'hFile'.
        /// </summary>
        /// <param name="hFile">Fully qualified name of a file</param>
        /// <param name="lpFileInformation">Out BY_HANDLE_FILE_INFORMATION argument</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool
            GetFileInformationByHandle(
            IntPtr hFile,
            out BY_HANDLE_FILE_INFORMATION lpFileInformation);

        /// <summary>
        /// Deletes the file specified by 'fileName'.
        /// </summary>
        /// <param name="fileName">Fully qualified path to the file to delete</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet=CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteFile(string fileName);

/*
        /// <summary>
        /// Read data from the file specified by 'hFile'.
        /// </summary>
        /// <param name="hFile">IntPtr handle to the file to read</param>
        /// <param name="lpBuffer">IntPtr to a buffer of bytes to receive the bytes read from 'hFile'</param>
        /// <param name="nNumberOfBytesToRead">Number of bytes to read from 'hFile'</param>
        /// <param name="lpNumberOfBytesRead">Number of bytes read from 'hFile'</param>
        /// <param name="lpOverlapped">IntPtr to an 'OVERLAPPED' structure</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadFile(
            IntPtr hFile,
            IntPtr lpBuffer,
            uint nNumberOfBytesToRead,
            out uint lpNumberOfBytesRead,
            IntPtr lpOverlapped);
*/

/*
        /// <summary>
        /// Writes the 
        /// </summary>
        /// <param name="hFile">IntPtr handle to the file to write</param>
        /// <param name="bytes">IntPtr to a buffer of bytes to write to 'hFile'</param>
        /// <param name="nNumberOfBytesToWrite">Number of bytes in 'lpBuffer' to write to 'hFile'</param>
        /// <param name="lpNumberOfBytesWritten">Number of bytes written to 'hFile'</param>
        /// <param name="overlapped">IntPtr to an 'OVERLAPPED' structure</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WriteFile(
            IntPtr hFile,
            IntPtr bytes,
            uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten,
            int overlapped);
*/

/*
        /// <summary>
        /// Writes the data in 'lpBuffer' to the file specified by 'hFile'.
        /// </summary>
        /// <param name="hFile">IntPtr handle to file to write</param>
        /// <param name="lpBuffer">Buffer of bytes to write to file 'hFile'</param>
        /// <param name="nNumberOfBytesToWrite">Number of bytes in 'lpBuffer' to write to 'hFile'</param>
        /// <param name="lpNumberOfBytesWritten">Number of bytes written to 'hFile'</param>
        /// <param name="overlapped">IntPtr to an 'OVERLAPPED' structure</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WriteFile(
            IntPtr hFile,
            byte[] lpBuffer,
            uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten,
            int overlapped);
*/

        /// <summary>
        /// Sends the 'dwIoControlCode' to the device specified by 'hDevice'.
        /// </summary>
        /// <param name="hDevice">IntPtr handle to the device to receive 'dwIoControlCode'</param>
        /// <param name="dwIoControlCode">Device IO Control Code to send</param>
        /// <param name="lpInBuffer">Input buffer if required</param>
        /// <param name="nInBufferSize">Size of input buffer</param>
        /// <param name="lpOutBuffer">Output buffer if required</param>
        /// <param name="nOutBufferSize">Size of output buffer</param>
        /// <param name="lpBytesReturned">Number of bytes returned in output buffer</param>
        /// <param name="lpOverlapped">IntPtr to an 'OVERLAPPED' structure</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeviceIoControl(
            IntPtr hDevice,
            UInt32 dwIoControlCode,
            IntPtr lpInBuffer,
            Int32 nInBufferSize,
            out USN_JOURNAL_DATA lpOutBuffer,
            Int32 nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped);

        /// <summary>
        /// Sends the control code 'dwIoControlCode' to the device driver specified by 'hDevice'.
        /// </summary>
        /// <param name="hDevice">IntPtr handle to the device to receive 'dwIoControlCode</param>
        /// <param name="dwIoControlCode">Device IO Control Code to send</param>
        /// <param name="lpInBuffer">Input buffer if required</param>
        /// <param name="nInBufferSize">Size of input buffer </param>
        /// <param name="lpOutBuffer">Output buffer if required</param>
        /// <param name="nOutBufferSize">Size of output buffer</param>
        /// <param name="lpBytesReturned">Number of bytes returned</param>
        /// <param name="lpOverlapped">Pointer to an 'OVERLAPPED' struture</param>
        /// <returns></returns>
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeviceIoControl(
            IntPtr hDevice,
            UInt32 dwIoControlCode,
            IntPtr lpInBuffer,
            Int32 nInBufferSize,
            IntPtr lpOutBuffer,
            Int32 nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped);

        /// <summary>
        /// Sets the number of bytes specified by 'size' of the memory associated with the argument 'ptr' 
        /// to zero.
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="size"></param>
        [DllImport("kernel32.dll")]
        public static extern void ZeroMemory(IntPtr ptr, int size);

        /// <summary>
        /// Creates a new file or directory, or opens an existing file, device, directory, or volume
        /// </summary>
        /// <param name="handle">A pointer to a variable that receives the file handle if the call is successful (out)</param>
        /// <param name="access">ACCESS_MASK value that expresses the type of access that the caller requires to the file or directory (in)</param>
        /// <param name="objectAttributes">A pointer to a structure already initialized with InitializeObjectAttributes (in)</param>
        /// <param name="ioStatus">A pointer to a variable that receives the final completion status and information about the requested operation (out)</param>
        /// <param name="allocSize">The initial allocation size in bytes for the file (in)(optional)</param>
        /// <param name="fileAttributes">file attributes (in)</param>
        /// <param name="share">type of share access that the caller would like to use in the file (in)</param>
        /// <param name="createDisposition">what to do, depending on whether the file already exists (in)</param>
        /// <param name="createOptions">options to be applied when creating or opening the file (in)</param>
        /// <param name="eaBuffer">Pointer to an EA buffer used to pass extended attributes (in)</param>
        /// <param name="eaLength">Length of the EA buffer</param>
        /// <returns>either STATUS_SUCCESS or an appropriate error status. If it returns an error status, the caller can find more information about the cause of the failure by checking the IoStatusBlock</returns>
        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = true)]
        public static extern int NtCreateFile(
            ref IntPtr handle,
            FileAccess access,
            ref OBJECT_ATTRIBUTES objectAttributes,
            ref IO_STATUS_BLOCK ioStatus,
            ref long allocSize,
            uint fileAttributes,
            FileShare share,
            uint createDisposition,
            uint createOptions,
            IntPtr eaBuffer,
            uint eaLength);

        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = true)]
        public static extern int NtQueryInformationFile(
            IntPtr fileHandle,
            ref IO_STATUS_BLOCK IoStatusBlock,
            IntPtr pInfoBlock,
            uint length,
            FILE_INFORMATION_CLASS fileInformation);

        /// <summary>
        /// We define this struct ourselves only so we can mark it as [Serializable].
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct FILETIME
        {
            public uint LowDateTime;
            public uint HighDateTime;
        }

        public static DateTime FiletimeToDateTime(FILETIME fileTime)
        {
            long fileTimeLong = (((long)fileTime.HighDateTime) << 32) | fileTime.LowDateTime;

            // XXX: FromFileTimeUTC returns a UTC DateTime which is less useful
            return DateTime.FromFileTime(fileTimeLong);
        }

        public static FILETIME DateTimeToFiletime(DateTime time)
        {
            FILETIME dateTimeToFiletime;

            long fileTimeLong = time.ToFileTimeUtc();
            
            dateTimeToFiletime.LowDateTime = (uint)(fileTimeLong & 0xFFFFFFFF);
            dateTimeToFiletime.HighDateTime = (uint)(fileTimeLong >> 32);
            
            return dateTimeToFiletime;
        }

        /// <summary>
        /// By Handle File Information structure, contains File Attributes(32bits), Creation Time(FILETIME),
        /// Last Access Time(FILETIME), Last Write Time(FILETIME), Volume Serial Number(32bits),
        /// File Size High(32bits), File Size Low(32bits), Number of Links(32bits), File Index High(32bits),
        /// File Index Low(32bits).
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BY_HANDLE_FILE_INFORMATION
        {
            public uint FileAttributes;
            public FILETIME CreationTime;
            public FILETIME LastAccessTime;
            public FILETIME LastWriteTime;
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
        /// Create USN Journal Data structure, contains Maximum Size(64bits) and Allocation Delta(64(bits).
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CREATE_USN_JOURNAL_DATA
        {
            public UInt64 MaximumSize;
            public UInt64 AllocationDelta;
        }

        /// <summary>
        /// Create USN Journal Data structure, contains Maximum Size(64bits) and Allocation Delta(64(bits).
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DELETE_USN_JOURNAL_DATA
        {
            public UInt64 UsnJournalID;
            public UInt32 DeleteFlags;
            public UInt32 Reserved;
        }

        /// <summary>
        /// Contains the USN Record Length(32bits), USN(64bits), File Reference Number(64bits), 
        /// Parent File Reference Number(64bits), Reason Code(32bits), File Attributes(32bits),
        /// File Name Length(32bits), the File Name Offset(32bits) and the File Name.
        /// </summary>
        [Serializable]
        public class UsnEntry : IComparable<UsnEntry>
        {
            private const int FR_OFFSET = 8;
            private const int PFR_OFFSET = 16;
            private const int USN_OFFSET = 24;
            private const int REASON_OFFSET = 40;
            private const int FA_OFFSET = 52;
            private const int FNL_OFFSET = 56;
            private const int FN_OFFSET = 58;

            public uint RecordLength
            {
                get;
                private set;
            }

            public long USN
            {
                get;
                private set;
            }

            public ulong FileReferenceNumber
            {
                get;
                private set;
            }

            public ulong ParentFileReferenceNumber
            {
                get;
                private set;
            }

            public uint Reason
            {
                get;
                private set;
            }

            public string Name
            {
                get;
                private set;
            }

            private string _oldName;
            public string OldName
            {
                get
                {
                    return 0 != (_fileAttributes & (uint)UsnJournal.UsnReasonCode.USN_REASON_RENAME_OLD_NAME) ? _oldName : null;
                }

                set { _oldName = value; }
            }

            private readonly UInt32 _fileAttributes;

            public bool IsFolder
            {
                get
                {
                    return 0 != (_fileAttributes & FILE_ATTRIBUTE_DIRECTORY);
                }
            }

            public bool IsFile
            {
                get
                {
                    return 0 == (_fileAttributes & FILE_ATTRIBUTE_DIRECTORY);
                }
            }

            /// <summary>
            /// USN Record Constructor
            /// </summary>
            /// <param name="usnEntryPointer">Buffer pointer to first byte of the USN Record</param>
            public UsnEntry(IntPtr usnEntryPointer)
            {
                RecordLength = (UInt32)Marshal.ReadInt32(usnEntryPointer);
                FileReferenceNumber = (UInt64)Marshal.ReadInt64(usnEntryPointer, FR_OFFSET);
                ParentFileReferenceNumber = (UInt64)Marshal.ReadInt64(usnEntryPointer, PFR_OFFSET);
                USN = Marshal.ReadInt64(usnEntryPointer, USN_OFFSET);
                Reason = (UInt32)Marshal.ReadInt32(usnEntryPointer, REASON_OFFSET);

                _fileAttributes = (UInt32)Marshal.ReadInt32(usnEntryPointer, FA_OFFSET);

                short fileNameLength = Marshal.ReadInt16(usnEntryPointer, FNL_OFFSET);
                short fileNameOffset = Marshal.ReadInt16(usnEntryPointer, FN_OFFSET);
                
                Name = Marshal.PtrToStringUni(new IntPtr(usnEntryPointer.ToInt32() + fileNameOffset), fileNameLength / sizeof(char));
            }

            #region IComparable<UsnEntry> Members

            public int CompareTo(UsnEntry other)
            {
                return string.Compare(Name, other.Name, true);
            }

            #endregion
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
    }
}

