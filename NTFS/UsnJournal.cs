using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace NTFS
{
    public sealed class UsnJournal : IDisposable
    {
        private readonly DriveInfo _driveInfo;
        private readonly uint _volumeSerialNumber;
        private IntPtr _usnJournalRootHandle;

        private bool _readMore;

        public string VolumeName
        {
            get { return _driveInfo.Name; }
        }

        public long AvailableFreeSpace
        {
            get { return _driveInfo.AvailableFreeSpace; }
        }

        public long TotalFreeSpace
        {
            get { return _driveInfo.TotalFreeSpace; }
        }

        public string Format
        {
            get { return _driveInfo.DriveFormat; }
        }

        public DirectoryInfo RootDirectory
        {
            get { return _driveInfo.RootDirectory; }
        }

        public long TotalSize
        {
            get { return _driveInfo.TotalSize; }
        }

        public string VolumeLabel
        {
            get { return _driveInfo.VolumeLabel; }
        }

        public uint VolumeSerialNumber
        {
            get { return _volumeSerialNumber; }
        }

        public static string GetSymbolicLinkTarget(DirectoryInfo symlink)
        {
            var directoryHandle = NativeMethods.NtCreateFile(
                symlink.FullName, 
                0, 
                2, 
                IntPtr.Zero,
                NativeMethods.CREATION_DISPOSITION_OPEN_EXISTING, 
                NativeMethods.FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero);

            if (directoryHandle.IsInvalid)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var path = new StringBuilder(512);
            
            int size = NativeMethods.GetFinalPathNameByHandle(directoryHandle.DangerousGetHandle(), path, path.Capacity, 0);
            
            if (size < 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            
            // The remarks section of GetFinalPathNameByHandle mentions the return being prefixed with "\\?\"
            // More information about "\\?\" here -> http://msdn.microsoft.com/en-us/library/aa365247(v=VS.85).aspx
            if (path[0] == '\\' && path[1] == '\\' && path[2] == '?' && path[3] == '\\')
            {
                return path.ToString().Substring(4);
            }
            
            return path.ToString();
        }

        /// <summary>
        /// Constructor for NtfsUsnJournal class.  If no exception is thrown, _usnJournalRootHandle and
        /// _volumeSerialNumber can be assumed to be good. If an exception is thrown, the NtfsUsnJournal
        /// object is not usable.
        /// </summary>
        /// <param name="driveInfo">DriveInfo object that provides access to information about a volume</param>
        /// <remarks> 
        /// An exception thrown if the volume is not an 'NTFS' volume or
        /// if GetRootHandle() or GetVolumeSerialNumber() functions fail. 
        /// Each public method checks to see if the volume is NTFS and if the _usnJournalRootHandle is
        /// valid.  If these two conditions aren't met, then the public function will return a UsnJournalReturnCode
        /// error.
        /// </remarks>
        public UsnJournal(DriveInfo driveInfo)
        {
            _driveInfo = driveInfo;

            if (0 != string.Compare(_driveInfo.DriveFormat, "ntfs", true))
            {
                throw new Exception(string.Format("{0} is not an 'NTFS' volume.", _driveInfo.Name));
            }

            IntPtr rootHandle;

            var returnCode = GetRootHandle(out rootHandle);

            if (returnCode != Enums.UsnJournalReturnCode.USN_JOURNAL_SUCCESS)
            {
                throw new Win32Exception((int)returnCode);
            }

            _usnJournalRootHandle = rootHandle;

            returnCode = GetVolumeSerialNumber(_driveInfo, out _volumeSerialNumber);

            if (returnCode != Enums.UsnJournalReturnCode.USN_JOURNAL_SUCCESS)
            {
                throw new Win32Exception((int)returnCode);
            }
        }

        /// <summary>
        /// GetNtfsVolumeFolders() reads the Master File Table to find all of the folders on a volume 
        /// and returns them in a List<UInt64, UsnJournalEntry> folders out parameter.
        /// </summary>
        /// <param name="folders">A List<string, UInt64> list where string is
        /// the filename and UInt64 is the parent folder's file reference number
        /// </param>
        /// <remarks>
        /// If function returns ERROR_ACCESS_DENIED you need to run application as an Administrator.
        /// </remarks>
        public IEnumerable<UsnJournalEntry> 
            GetNtfsVolumeFolders()
        {
            if (_usnJournalRootHandle.ToInt32() == NativeMethods.INVALID_HANDLE_VALUE)
            {
                throw new UsnJournalException(Enums.UsnJournalReturnCode.INVALID_HANDLE_VALUE);
            }

            var usnState = new Types.USN_JOURNAL_DATA();

            var returnCode = QueryUsnJournal(ref usnState);

            if (returnCode != Enums.UsnJournalReturnCode.USN_JOURNAL_SUCCESS)
            {
                throw new UsnJournalException(returnCode);
            }

            Types.MFT_ENUM_DATA med;

            med.StartFileReferenceNumber = 0;
            med.LowUsn = 0;
            med.HighUsn = usnState.NextUsn;

            Int32 sizeMftEnumData = Marshal.SizeOf(med);
            IntPtr medBuffer = Marshal.AllocHGlobal(sizeMftEnumData);
            NativeMethods.ZeroMemory(medBuffer, sizeMftEnumData);
            Marshal.StructureToPtr(med, medBuffer, true);

            // Set up the data buffer which receives the USN_RECORD data
            const int pDataSize = sizeof (UInt64) + 10000;
            
            IntPtr pData = Marshal.AllocHGlobal(pDataSize);
            
            NativeMethods.ZeroMemory(pData, pDataSize);

            uint outBytesReturned;

            // Gather up volume's directories
            while (NativeMethods.DeviceIoControl(
                _usnJournalRootHandle,
                NativeMethods.FSCTL_ENUM_USN_DATA,
                medBuffer,
                sizeMftEnumData,
                pData,
                pDataSize,
                out outBytesReturned,
                IntPtr.Zero))
            {
                var usnEntryPointer = new IntPtr(pData.ToInt32() + sizeof (Int64));

                while (outBytesReturned > 60)
                {
                    var usnEntry = new UsnJournalEntry(usnEntryPointer);

                    if (usnEntry.IsFolder)
                    {
                        yield return usnEntry;
                    }

                    usnEntryPointer = new IntPtr(usnEntryPointer.ToInt32() + usnEntry.UsnRecord.RecordLength);

                    outBytesReturned -= usnEntry.UsnRecord.RecordLength;
                }

                Marshal.WriteInt64(medBuffer, Marshal.ReadInt64(pData, 0));
            }

            Marshal.FreeHGlobal(pData);
        }

        public IEnumerable<UsnJournalEntry> TestGetFiles()
        {
            var usnState = new Types.USN_JOURNAL_DATA();

            var returnCode = QueryUsnJournal(ref usnState);

            if (returnCode != Enums.UsnJournalReturnCode.USN_JOURNAL_SUCCESS)
            {
                throw new UsnJournalException(returnCode);
            }

            // Set up MFT_ENUM_DATA structure
            Types.MFT_ENUM_DATA med;

            med.StartFileReferenceNumber = 0;
            med.LowUsn = 0;
            med.HighUsn = usnState.NextUsn;
            
            Int32 sizeMftEnumData = Marshal.SizeOf(med);
            IntPtr medBuffer = Marshal.AllocHGlobal(sizeMftEnumData);
            
            NativeMethods.ZeroMemory(medBuffer, sizeMftEnumData);
            
            Marshal.StructureToPtr(med, medBuffer, true);

            // Setup the data buffer which receives the USN_RECORD data
            const int pDataSize = sizeof (UInt64) + 10000;

            IntPtr pData = Marshal.AllocHGlobal(pDataSize);
            
            NativeMethods.ZeroMemory(pData, pDataSize);
            
            uint bytesReturned;

            // Gather up the volume's directories
            while (NativeMethods.DeviceIoControl(
                _usnJournalRootHandle,
                NativeMethods.FSCTL_ENUM_USN_DATA,
                medBuffer,
                sizeMftEnumData,
                pData,
                pDataSize,
                out bytesReturned,
                IntPtr.Zero))
            {
                var usnEntryPointer = new IntPtr(pData.ToInt32() + sizeof (Int64));

                while (bytesReturned > 60)
                {
                    var usnEntry = new UsnJournalEntry(usnEntryPointer);

                    yield return usnEntry;

                    usnEntryPointer = new IntPtr(usnEntryPointer.ToInt32() + usnEntry.UsnRecord.RecordLength);

                    bytesReturned -= usnEntry.UsnRecord.RecordLength;
                }

                Marshal.WriteInt64(medBuffer, Marshal.ReadInt64(pData, 0));
            }

            Marshal.FreeHGlobal(pData);
        }

        public IEnumerable<UsnJournalEntry>
            GetFilesMatchingFilter(string filter)
        {
            filter = filter.Trim().ToLower();

            bool acceptAll = filter == "*";

            string[] fileTypes = filter.Split(' ', ',', ';');

            if (_usnJournalRootHandle.ToInt32() == NativeMethods.INVALID_HANDLE_VALUE)
            {
                throw new UsnJournalException(Enums.UsnJournalReturnCode.ERROR_INVALID_HANDLE);
            }

            var usnState = new Types.USN_JOURNAL_DATA();

            var returnCode = QueryUsnJournal(ref usnState);

            if (returnCode != Enums.UsnJournalReturnCode.USN_JOURNAL_SUCCESS)
            {
                throw new UsnJournalException(returnCode);
            }

            // Set up MFT_ENUM_DATA structure
            Types.MFT_ENUM_DATA med;

            med.StartFileReferenceNumber = 0;
            med.LowUsn = 0;
            med.HighUsn = usnState.NextUsn;

            Int32 sizeMftEnumData = Marshal.SizeOf(med);
            IntPtr medBuffer = Marshal.AllocHGlobal(sizeMftEnumData);

            NativeMethods.ZeroMemory(medBuffer, sizeMftEnumData);

            Marshal.StructureToPtr(med, medBuffer, true);

            // Setup the data buffer which receives the USN_RECORD data
            const int pDataSize = sizeof(UInt64) + 10000;

            IntPtr pData = Marshal.AllocHGlobal(pDataSize);

            NativeMethods.ZeroMemory(pData, pDataSize);

            uint bytesReturned;

            // Gather up the volume's directories
            while (NativeMethods.DeviceIoControl(
                _usnJournalRootHandle,
                NativeMethods.FSCTL_ENUM_USN_DATA,
                medBuffer,
                sizeMftEnumData,
                pData,
                pDataSize,
                out bytesReturned,
                IntPtr.Zero))
            {
                var usnEntryPointer = new IntPtr(pData.ToInt32() + sizeof(Int64));

                while (bytesReturned > 60)
                {
                    var usnEntry = new UsnJournalEntry(usnEntryPointer);

                    if (usnEntry.IsFile)
                    {
                        string extension = Path.GetExtension(usnEntry.Name);

                        if (acceptAll)
                        {
                            yield return usnEntry;
                        }
                        else if (!string.IsNullOrEmpty(extension))
                        {
                            if (fileTypes.Any(extension.ToLower().Contains))
                            {
                                yield return usnEntry;
                            }
                        }
                    }

                    usnEntryPointer = new IntPtr(usnEntryPointer.ToInt32() + usnEntry.UsnRecord.RecordLength);

                    bytesReturned -= usnEntry.UsnRecord.RecordLength;
                }

                Marshal.WriteInt64(medBuffer, Marshal.ReadInt64(pData, 0));
            }

            Marshal.FreeHGlobal(pData);
        }

        private static string GetPathFromHandle(IntPtr handle)
        {
            IntPtr fileInfoBuffer = Marshal.AllocHGlobal(2048);

            NativeMethods.ZeroMemory(fileInfoBuffer, 2048);

            bool result = NativeMethods.GetFileInformationByHandleEx(
                handle,
                Enums.FILE_INFO_BY_HANDLE_CLASS.FileNameInfo,
                fileInfoBuffer,
                2048);

            int length = Marshal.ReadInt32(fileInfoBuffer);

            string path = Marshal.PtrToStringUni((IntPtr) (fileInfoBuffer.ToInt32() + 4), length / sizeof(char));

            return path;
        }

        /// <summary>
        /// Given a file reference number GetPathFromFrn() calculates the full path in the out parameter 'path'.
        /// </summary>
        /// <param name="frn">A 64-bit file reference number</param>
        /// <remarks>
        /// If function returns ERROR_ACCESS_DENIED you need to run application as an Administrator.
        /// </remarks>
        public string GetPathFromFileReference(UInt64 frn)
        {
            if (_usnJournalRootHandle.ToInt32() == NativeMethods.INVALID_HANDLE_VALUE)
            {
                throw new UsnJournalException(Enums.UsnJournalReturnCode.INVALID_HANDLE_VALUE);
            }

            if (frn == 0)
            {
                throw new UsnJournalException(Enums.UsnJournalReturnCode.INVALID_FILE_REFERENCE_NUMBER);
            }

            long allocSize = 0;

            Types.UNICODE_STRING unicodeString;

            var objectAttributes = new Types.OBJECT_ATTRIBUTES();
            var ioStatusBlock = new Types.IO_STATUS_BLOCK();

            IntPtr hFile = IntPtr.Zero;

            IntPtr buffer = Marshal.AllocHGlobal(4096);
            IntPtr refPtr = Marshal.AllocHGlobal(8);

            IntPtr objAttIntPtr = Marshal.AllocHGlobal(Marshal.SizeOf(objectAttributes));

            // Pointer >> FRN
            Marshal.WriteInt64(refPtr, (long) frn);

            unicodeString.Length = 8;
            unicodeString.MaximumLength = 8;
            unicodeString.Buffer = refPtr;

            // Copy unicode structure to pointer
            Marshal.StructureToPtr(unicodeString, objAttIntPtr, true);

            // InitializeObjectAttributes 
            objectAttributes.Length = Marshal.SizeOf(objectAttributes);
            objectAttributes.ObjectName = objAttIntPtr;
            objectAttributes.RootDirectory = _usnJournalRootHandle;
            objectAttributes.Attributes = (int) NativeMethods.OBJ_CASE_INSENSITIVE;

            int result = NativeMethods.NtCreateFile(
                ref hFile,
                FileAccess.Read,
                ref objectAttributes,
                ref ioStatusBlock,
                ref allocSize,
                0,
                FileShare.ReadWrite,
                NativeMethods.FILE_OPEN,
                NativeMethods.FILE_OPEN_BY_FILE_ID | NativeMethods.FILE_OPEN_FOR_BACKUP_INTENT,
                IntPtr.Zero, 0);

            string path = null;

            if (result == 0)
            {
                path = GetPathFromHandle(hFile);
            }

            NativeMethods.CloseHandle(hFile);

            Marshal.FreeHGlobal(buffer);
            Marshal.FreeHGlobal(objAttIntPtr);
            Marshal.FreeHGlobal(refPtr);

            return path;
        }

        /// <summary>
        /// GetUsnJournalState() gets the current state of the USN Journal if it is active.
        /// </summary>
        /// <param name="usnJournalState">
        /// Reference to usn journal data object filled with the current USN Journal state.
        /// </param>
        /// <remarks>
        /// If function returns ERROR_ACCESS_DENIED you need to run application as an Administrator.
        /// </remarks>
        public Enums.UsnJournalReturnCode
            GetUsnJournalState(ref Types.USN_JOURNAL_DATA usnJournalState)
        {
            if (_usnJournalRootHandle.ToInt32() == NativeMethods.INVALID_HANDLE_VALUE)
            {
                return Enums.UsnJournalReturnCode.INVALID_HANDLE_VALUE;
            }

            return QueryUsnJournal(ref usnJournalState);
        }

        /// <summary>
        /// Given a previous state, GetUsnJournalEntries() determines if the USN Journal is active and
        /// no USN Journal entries have been lost (i.e. USN Journal is valid), then
        /// it loads a List<UsnJournalEntry> list and returns it as the out parameter 'usnEntries'.
        /// If GetUsnJournalChanges returns anything but USN_JOURNAL_SUCCESS, the usnEntries list will 
        /// be empty.
        /// </summary>
        /// <param name="previousUsnState">The USN Journal state the last time volume 
        /// changes were requested.</param>
        /// <param name="reasonMask"></param>
        /// <param name="usnEntries"></param>
        /// <param name="newUsnState"></param>
        /// <remarks>
        /// If function returns ERROR_ACCESS_DENIED you need to run application as an Administrator.
        /// </remarks>
        public Enums.UsnJournalReturnCode
            GetUsnJournalEntries(Types.USN_JOURNAL_DATA previousUsnState,
                UInt32 reasonMask,
                out List<UsnJournalEntry> usnEntries,
                out Types.USN_JOURNAL_DATA newUsnState)
        {
            usnEntries = new List<UsnJournalEntry>();
            newUsnState = new Types.USN_JOURNAL_DATA();

            if (_usnJournalRootHandle.ToInt32() == NativeMethods.INVALID_HANDLE_VALUE)
            {
                return Enums.UsnJournalReturnCode.INVALID_HANDLE_VALUE;
            }
            
            // Get current journal state
            var returnCode = QueryUsnJournal(ref newUsnState);

            if (returnCode != Enums.UsnJournalReturnCode.USN_JOURNAL_SUCCESS)
            {
                return returnCode;
            }

            bool readMore = true;

            // Sequentially process the usn journal looking for image file entries
            const int pbDataSize = sizeof (UInt64)*0x4000;

            IntPtr pbData = Marshal.AllocHGlobal(pbDataSize);

            NativeMethods.ZeroMemory(pbData, pbDataSize);

            var rujd = new Types.READ_USN_JOURNAL_DATA
            {
                StartUsn = previousUsnState.NextUsn,
                ReasonMask = reasonMask,
                ReturnOnlyOnClose = 0,
                Timeout = 0,
                bytesToWaitFor = 0,
                UsnJournalId = previousUsnState.UsnJournalID
            };

            int sizeRujd = Marshal.SizeOf(rujd);

            IntPtr rujdBuffer = Marshal.AllocHGlobal(sizeRujd);
            NativeMethods.ZeroMemory(rujdBuffer, sizeRujd);
            Marshal.StructureToPtr(rujd, rujdBuffer, true);

            // Read USN journal entries
            while (readMore)
            {
                uint outBytesReturned;

                bool result = NativeMethods.DeviceIoControl(
                    _usnJournalRootHandle,
                    NativeMethods.FSCTL_READ_USN_JOURNAL,
                    rujdBuffer,
                    sizeRujd,
                    pbData,
                    pbDataSize,
                    out outBytesReturned,
                    IntPtr.Zero);

                var lastWin32Error = (Enums.GetLastErrorEnum) Marshal.GetLastWin32Error();

                if (result)
                {
                    var usnEntryPointer = new IntPtr(pbData.ToInt32() + sizeof (UInt64));

                    // While there is at least one entry in the usn journal 
                    while (outBytesReturned > 60)
                    {
                        var usnEntry = new UsnJournalEntry(usnEntryPointer);

                        // Only read until the current USN points beyond the current state's USN
                        if (usnEntry.UsnRecord.Usn >= newUsnState.NextUsn)
                        {
                            readMore = false;

                            break;
                        }

                        usnEntries.Add(usnEntry);

                        usnEntryPointer = new IntPtr(usnEntryPointer.ToInt32() + usnEntry.UsnRecord.RecordLength);

                        outBytesReturned -= usnEntry.UsnRecord.RecordLength;
                    }
                }
                else
                {
                    if (lastWin32Error == Enums.GetLastErrorEnum.ERROR_HANDLE_EOF)
                    {
                        returnCode = Enums.UsnJournalReturnCode.USN_JOURNAL_SUCCESS;
                    }
                    else
                    {
                        returnCode = ConvertWin32ErrorToUsnError(lastWin32Error);
                    }

                    break;
                }

                Int64 nextUsn = Marshal.ReadInt64(pbData, 0);

                if (nextUsn >= newUsnState.NextUsn)
                {
                    break;
                }

                Marshal.WriteInt64(rujdBuffer, nextUsn);
            }

            Marshal.FreeHGlobal(rujdBuffer);
            Marshal.FreeHGlobal(pbData);

            return returnCode;
        }

        public delegate void NewUsnRecordEventHandler(object sender, NewUsnRecordEventArgs e);

        public event NewUsnRecordEventHandler NewUsnRecord;

        private void OnNewUsnRecord(NewUsnRecordEventArgs e)
        {
            if (NewUsnRecord != null)
            {
                NewUsnRecord(this, e);
            }
        }

        public Enums.UsnJournalReturnCode
            MonitorUsnJournalEntries(UInt32 reasonMask)
        {
            var state = new Types.USN_JOURNAL_DATA();

            if (_usnJournalRootHandle.ToInt32() == NativeMethods.INVALID_HANDLE_VALUE)
            {
                return Enums.UsnJournalReturnCode.INVALID_HANDLE_VALUE;
            }

            var returnCode = QueryUsnJournal(ref state);

            if (returnCode != Enums.UsnJournalReturnCode.USN_JOURNAL_SUCCESS)
            {
                return returnCode;
            }

            const int pbDataSize = sizeof (UInt64)*0x4000;

            IntPtr pbData = Marshal.AllocHGlobal(pbDataSize);

            NativeMethods.ZeroMemory(pbData, pbDataSize);

            var rujd = new Types.READ_USN_JOURNAL_DATA
            {
                StartUsn = state.NextUsn,
                ReasonMask = reasonMask,
                ReturnOnlyOnClose = 0,
                Timeout = 1,
                bytesToWaitFor = 1,
                UsnJournalId = state.UsnJournalID
            };

            int sizeRujd = Marshal.SizeOf(rujd);

            IntPtr rujdBuffer = Marshal.AllocHGlobal(sizeRujd);

            NativeMethods.ZeroMemory(rujdBuffer, sizeRujd);

            Marshal.StructureToPtr(rujd, rujdBuffer, true);

            _readMore = true;

            // Read USN journal entries
            while (_readMore)
            {
                uint bytesReturned;

                bool returnValue = NativeMethods.DeviceIoControl(
                    _usnJournalRootHandle,
                    NativeMethods.FSCTL_READ_USN_JOURNAL,
                    rujdBuffer,
                    sizeRujd,
                    pbData,
                    pbDataSize,
                    out bytesReturned,
                    IntPtr.Zero);

                if (!returnValue)
                {
                    var lastWin32Error = (Enums.GetLastErrorEnum) Marshal.GetLastWin32Error();

                    Marshal.FreeHGlobal(rujdBuffer);
                    Marshal.FreeHGlobal(pbData);

                    return lastWin32Error == Enums.GetLastErrorEnum.ERROR_HANDLE_EOF
                               ? Enums.UsnJournalReturnCode.USN_JOURNAL_SUCCESS
                               : ConvertWin32ErrorToUsnError(lastWin32Error);
                }

                var usnEntryPointer = new IntPtr(pbData.ToInt32() + sizeof (UInt64));

                // While there are at least one entry in the usn journal
                // XXX: Why magic number 60?
                while (bytesReturned > 60)
                {
                    var usnEntry = new UsnJournalEntry(usnEntryPointer);

                    OnNewUsnRecord(new NewUsnRecordEventArgs(usnEntry));

                    usnEntryPointer = new IntPtr(usnEntryPointer.ToInt32() + usnEntry.UsnRecord.RecordLength);

                    bytesReturned -= usnEntry.UsnRecord.RecordLength;
                }

                var nextUsn = Marshal.ReadInt64(pbData, 0);

                Marshal.WriteInt64(rujdBuffer, nextUsn);
            }

            Marshal.FreeHGlobal(rujdBuffer);
            Marshal.FreeHGlobal(pbData);

            return returnCode;
        }

        /// <summary>
        /// Tests to see if the USN Journal is active on the volume.
        /// </summary>
        /// <returns>true if USN Journal is active
        /// false if no USN Journal on volume</returns>
        public bool
            IsUsnJournalActive()
        {
            if (_usnJournalRootHandle.ToInt32() == NativeMethods.INVALID_HANDLE_VALUE)
            {
                return false;
            }

            var currentState = new Types.USN_JOURNAL_DATA();

            var usnError = QueryUsnJournal(ref currentState);

            return usnError == Enums.UsnJournalReturnCode.USN_JOURNAL_SUCCESS;
        }

        /// <summary>
        /// Rests to see if there is a USN Journal on this volume and if there is 
        /// determines whether any journal entries have been lost.
        /// </summary>
        /// <returns>true if the USN Journal is active and if the JournalId's are the same 
        /// and if all the usn journal entries expected by the previous state are available 
        /// from the current state and false if not</returns>
        public bool
            IsUsnJournalValid(Types.USN_JOURNAL_DATA usnJournalPreviousState)
        {
            if (_usnJournalRootHandle.ToInt32() == NativeMethods.INVALID_HANDLE_VALUE)
            {
                return false;
            }

            var usnJournalState = new Types.USN_JOURNAL_DATA();
            var usnError = QueryUsnJournal(ref usnJournalState);

            if (usnError == Enums.UsnJournalReturnCode.USN_JOURNAL_SUCCESS && 
                usnJournalPreviousState.UsnJournalID == usnJournalState.UsnJournalID &&
                usnJournalPreviousState.NextUsn >= usnJournalState.NextUsn)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Converts a Win32 Error to a UsnJournalReturnCode
        /// </summary>
        /// <param name="win32LastError">The 'last' NativeMethods error.</param>
        private static Enums.UsnJournalReturnCode
            ConvertWin32ErrorToUsnError(Enums.GetLastErrorEnum win32LastError)
        {
            Enums.UsnJournalReturnCode returnCode;

            switch (win32LastError)
            {
                case Enums.GetLastErrorEnum.ERROR_JOURNAL_NOT_ACTIVE:
                    returnCode = Enums.UsnJournalReturnCode.USN_JOURNAL_NOT_ACTIVE;
                    break;
                case Enums.GetLastErrorEnum.ERROR_SUCCESS:
                    returnCode = Enums.UsnJournalReturnCode.USN_JOURNAL_SUCCESS;
                    break;
                case Enums.GetLastErrorEnum.ERROR_HANDLE_EOF:
                    returnCode = Enums.UsnJournalReturnCode.ERROR_HANDLE_EOF;
                    break;
                default:
                    returnCode = Enums.UsnJournalReturnCode.USN_JOURNAL_ERROR;
                    break;
            }

            return returnCode;
        }

        public int 
            GetFileInformation(string path, out Types.BY_HANDLE_FILE_INFORMATION? fileInformation)
        {
            var handle = NativeMethods.CreateFile(path,
                0,
                NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE,
                IntPtr.Zero,
                NativeMethods.OPEN_EXISTING,
                NativeMethods.FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero);

            if (handle.ToInt32() != NativeMethods.INVALID_HANDLE_VALUE)
            {
                Types.BY_HANDLE_FILE_INFORMATION fileInformationTemp;

                bool success = NativeMethods.GetFileInformationByHandle(handle, out fileInformationTemp);

                if (success)
                {
                    NativeMethods.CloseHandle(handle);

                    fileInformation = fileInformationTemp;

                    return 0;
                }
            }

            fileInformation = new Types.BY_HANDLE_FILE_INFORMATION();

            return Marshal.GetLastWin32Error();
        }

        /// <summary>
        /// Gets a Volume Serial Number for the volume represented by driveInfo.
        /// </summary>
        /// <param name="driveInfo">DriveInfo object representing the volume in question.</param>
        /// <param name="volumeSerialNumber">out parameter to hold the volume serial number.</param>
        private Enums.UsnJournalReturnCode
            GetVolumeSerialNumber(DriveInfo driveInfo, out uint volumeSerialNumber)
        {
            volumeSerialNumber = 0;

            var returnCode = Enums.UsnJournalReturnCode.USN_JOURNAL_SUCCESS;

            string pathRoot = string.Concat("\\\\.\\", driveInfo.Name);

            var handle = NativeMethods.CreateFile(pathRoot,
                0,
                NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE,
                IntPtr.Zero,
                NativeMethods.OPEN_EXISTING,
                NativeMethods.FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero);

            if (handle.ToInt32() != NativeMethods.INVALID_HANDLE_VALUE)
            {
                Types.BY_HANDLE_FILE_INFORMATION fileInformation;

                bool result = NativeMethods.GetFileInformationByHandle(handle, out fileInformation);

                if (result)
                {
                    volumeSerialNumber = fileInformation.VolumeSerialNumber;
                }
                else
                {
                    returnCode = (Enums.UsnJournalReturnCode)Marshal.GetLastWin32Error();
                }

                NativeMethods.CloseHandle(handle);
            }
            else
            {
                returnCode = (Enums.UsnJournalReturnCode)Marshal.GetLastWin32Error();
            }

            return returnCode;
        }

        private Enums.UsnJournalReturnCode
            GetRootHandle(out IntPtr rootHandle)
        {
            var returnCode = Enums.UsnJournalReturnCode.USN_JOURNAL_SUCCESS;

            string vol = string.Concat("\\\\.\\", _driveInfo.Name.TrimEnd('\\'));

            rootHandle = NativeMethods.CreateFile(vol,
                 NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE,
                 NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE,
                 IntPtr.Zero,
                 NativeMethods.OPEN_EXISTING,
                 0,
                 IntPtr.Zero);

            if (rootHandle.ToInt32() == NativeMethods.INVALID_HANDLE_VALUE)
            {
                returnCode = (Enums.UsnJournalReturnCode)Marshal.GetLastWin32Error();
            }

            return returnCode;
        }

        /// <summary>
        /// This function queries the usn journal on the volume. 
        /// </summary>
        /// <param name="usnJournalState">the USN_JOURNAL_DATA object that is associated with this volume</param>
        private Enums.UsnJournalReturnCode
            QueryUsnJournal(ref Types.USN_JOURNAL_DATA usnJournalState)
        {
            var usnReturnCode = Enums.UsnJournalReturnCode.USN_JOURNAL_SUCCESS;

            int sizeUsnJournalState = Marshal.SizeOf(usnJournalState);

            UInt32 cb;

            bool result = NativeMethods.DeviceIoControl(
                _usnJournalRootHandle,
                NativeMethods.FSCTL_QUERY_USN_JOURNAL,
                IntPtr.Zero,
                0,
                out usnJournalState,
                sizeUsnJournalState,
                out cb,
                IntPtr.Zero);

            if (!result)
            {
                usnReturnCode = ConvertWin32ErrorToUsnError((Enums.GetLastErrorEnum)Marshal.GetLastWin32Error());
            }

            return usnReturnCode;
        }

        public void Dispose()
        {
            NativeMethods.CloseHandle(_usnJournalRootHandle);
        }
    }
}