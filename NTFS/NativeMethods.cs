using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

using Microsoft.Win32.SafeHandles;

namespace NTFS
{
    [SuppressUnmanagedCodeSecurityAttribute]
    public static class NativeMethods
    {
        public const Int32 INVALID_HANDLE_VALUE = -1;

        public const UInt32 GENERIC_READ = 0x80000000;
        public const UInt32 GENERIC_WRITE = 0x40000000;
        public const UInt32 FILE_SHARE_READ = 0x00000001;
        public const UInt32 FILE_SHARE_WRITE = 0x00000002;

        public const UInt32 CREATE_NEW = 1;
        public const UInt32 CREATE_ALWAYS = 2;
        public const UInt32 OPEN_EXISTING = 3;
        public const UInt32 OPEN_ALWAYS = 4;
        public const UInt32 TRUNCATE_EXISTING = 5;

        public const UInt32 FILE_ATTRIBUTE_NORMAL = 0x80;
        public const UInt32 FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
        public const UInt32 FILE_OPEN_FOR_BACKUP_INTENT = 0x4000;
        public const UInt32 FILE_OPEN_BY_FILE_ID = 0x2000;
        public const UInt32 FILE_OPEN = 0x1;
        public const UInt32 OBJ_CASE_INSENSITIVE = 0x40;

        private const UInt32 FILE_DEVICE_FILE_SYSTEM = 0x00000009;

        public const int CREATION_DISPOSITION_OPEN_EXISTING = 3;
        
        private const UInt32 METHOD_NEITHER = 3;
        private const UInt32 METHOD_BUFFERED = 0;

        private const UInt32 FILE_ANY_ACCESS = 0;
        private const UInt32 FILE_SPECIAL_ACCESS = 0;
        private const UInt32 FILE_READ_ACCESS = 1;
        private const UInt32 FILE_WRITE_ACCESS = 2;

        public const UInt32 FSCTL_GET_OBJECT_ID = 0x9009c;

        public const UInt32 FSCTL_ENUM_USN_DATA = (FILE_DEVICE_FILE_SYSTEM << 16) | (FILE_ANY_ACCESS << 14) | (44 << 2) | METHOD_NEITHER;
        public const UInt32 FSCTL_READ_USN_JOURNAL = (FILE_DEVICE_FILE_SYSTEM << 16) | (FILE_ANY_ACCESS << 14) | (46 << 2) | METHOD_NEITHER;
        public const UInt32 FSCTL_QUERY_USN_JOURNAL = (FILE_DEVICE_FILE_SYSTEM << 16) | (FILE_ANY_ACCESS << 14) | (61 << 2) | METHOD_BUFFERED;

        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
        public static extern IntPtr
            CreateFile(string lpFileName,
                uint dwDesiredAccess,
                uint dwShareMode,
                IntPtr lpSecurityAttributes,
                uint dwCreationDisposition,
                uint dwFlagsAndAttributes,
                IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool
            CloseHandle(
                IntPtr hObject);

        [DllImport("kernel32.dll", EntryPoint="GetFileInformationByHandle")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool 
            GetFileInformationByHandle(
                [In] IntPtr hFile, 
                [Out] out Types.BY_HANDLE_FILE_INFORMATION lpFileInformation);

        [DllImport("kernel32.dll", EntryPoint="GetFileInformationByHandleEx", CharSet=CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool 
            GetFileInformationByHandleEx(
                IntPtr hFile,
                Enums.FILE_INFO_BY_HANDLE_CLASS fileInformationClass,
                IntPtr fileInfo,
                uint dwBufferSize);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool
            DeviceIoControl(
                IntPtr hDevice,
                UInt32 dwIoControlCode,
                IntPtr lpInBuffer,
                Int32 nInBufferSize,
                out Types.USN_JOURNAL_DATA lpOutBuffer,
                Int32 nOutBufferSize,
                out uint lpBytesReturned,
                IntPtr lpOverlapped);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool 
            DeviceIoControl(
                IntPtr hDevice,
                UInt32 dwIoControlCode,
                IntPtr lpInBuffer,
                Int32 nInBufferSize,
                IntPtr lpOutBuffer,
                Int32 nOutBufferSize,
                out uint lpBytesReturned,
                IntPtr lpOverlapped);

        //// XXX: From P/Invoke Interop Assistant
        //[DllImport("kernel32.dll", EntryPoint = "DeviceIoControl")]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //public static extern bool DeviceIoControl(
        //    [In] IntPtr hDevice, 
        //    uint dwIoControlCode, 
        //    [In] IntPtr lpInBuffer, 
        //    uint nInBufferSize, 
        //    IntPtr lpOutBuffer, 
        //    uint nOutBufferSize, 
        //    IntPtr lpBytesReturned, 
        //    IntPtr lpOverlapped);

        //[DllImport("Kernel32.dll", SetLastError = false, CharSet = CharSet.Auto)]
        //public static extern bool DeviceIoControl(
        //    Microsoft.Win32.SafeHandles.SafeFileHandle hDevice,
        //    uint IoControlCode,
        //    [MarshalAs(UnmanagedType.AsAny)]
        //    [In] object InBuffer,
        //    uint nInBufferSize,
        //    [MarshalAs(UnmanagedType.AsAny)]
        //    [Out] object OutBuffer,
        //    uint nOutBufferSize,
        //    ref uint pBytesReturned,
        //    [In] ref System.Threading.NativeOverlapped Overlapped);

        [DllImport("kernel32.dll")]
        public static extern void ZeroMemory(IntPtr ptr, int size);

        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = true)]
        public static extern int 
            NtCreateFile(
                ref IntPtr handle,
                FileAccess access,
                ref Types.OBJECT_ATTRIBUTES objectAttributes,
                ref Types.IO_STATUS_BLOCK ioStatus,
                ref long allocSize,
                uint fileAttributes,
                FileShare share,
                uint createDisposition,
                uint createOptions,
                IntPtr eaBuffer,
                uint eaLength);

        [DllImport("kernel32.dll", EntryPoint = "GetFinalPathNameByHandleW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetFinalPathNameByHandle(
            IntPtr handle, 
            [In, Out] StringBuilder path,
            int bufLen, 
            int flags);

        [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeFileHandle 
            NtCreateFile(
                string lpFileName, 
                int dwDesiredAccess, 
                int dwShareMode,
                IntPtr SecurityAttributes, 
                int dwCreationDisposition, 
                uint dwFlagsAndAttributes, 
                IntPtr hTemplateFile);
    }
}