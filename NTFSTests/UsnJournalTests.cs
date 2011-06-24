using System.Diagnostics;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using NTFS;

namespace NTFSTests
{
    [TestClass]
    public class UsnJournalTests
    {
        private UsnJournal _journal;

        [TestInitialize]
        public void TestSetup()
        {
            _journal = new UsnJournal(new DriveInfo("C"));

            Assert.IsNotNull(_journal);
        }

        [TestMethod]
        public void TestGetFileInformation()
        {
            NativeMethods.BY_HANDLE_FILE_INFORMATION? fileInformation;

            // TODO: Create the file ourselves
            int result = _journal.GetFileInformation(@"C:\Test.txt", out fileInformation);

            Assert.IsTrue(result == 0);

            Assert.IsNotNull(fileInformation);

            Trace.WriteLine(NativeMethods.FiletimeToDateTime(fileInformation.Value.LastAccessTime));
            Trace.WriteLine(NativeMethods.FiletimeToDateTime(fileInformation.Value.LastWriteTime));
        }

        [TestMethod]
        public void TestGetFilesMatchingFilter()
        {
            int count = 0;

            foreach (var usnEntry in _journal.GetFilesMatchingFilter("*"))
            {
                if (count > 5)
                {
                    return;
                }

                Assert.IsNotNull(usnEntry);

                Trace.WriteLine(usnEntry.ParentFileReferenceNumber);

                string path = _journal.GetPathFromFileReference(usnEntry.ParentFileReferenceNumber);

                Assert.IsNotNull(path);

                Trace.WriteLine(path);

                count++;
            }
        }
    }
}