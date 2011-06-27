using System.Diagnostics;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using NTFS;
using NTFS.Extensions;

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
        public void TestUsnJournalRecord()
        {
            int count = 0;

            foreach (var record in _journal.TestGetFiles())
            {
                if (count > 25)
                {
                    return;
                }

                Trace.WriteLine(string.Format("Name: {0}", record.Name));
                Trace.WriteLine(string.Format("FRN: {0}", record.UsnRecord.FileReferenceNumber));

                count++;
            }
        }

        [TestMethod]
        public void TestGetFileInformation()
        {
            Types.BY_HANDLE_FILE_INFORMATION? fileInformation;

            // TODO: Create the file ourselves
            int result = UsnJournal.GetFileInformation(@"C:\Test.txt", out fileInformation);

            Assert.IsTrue(result == 0);

            Assert.IsNotNull(fileInformation);

            Trace.WriteLine(fileInformation.Value.LastAccessTime.ToDateTime());
            Trace.WriteLine(fileInformation.Value.LastWriteTime.ToDateTime());
        }

        [TestMethod]
        public void TestGetFilesMatchingFilter()
        {
            int count = 0;

            foreach (var usnEntry in _journal.GetFilesMatchingFilter("*"))
            {
                if (count > 25)
                {
                    return;
                }

                Assert.IsNotNull(usnEntry);

                Trace.WriteLine(usnEntry.UsnRecord.ParentFileReferenceNumber);

                string path = _journal.GetPathFromFileReference(usnEntry.UsnRecord.ParentFileReferenceNumber);

                Trace.WriteLine(string.Format("Path: {0}", path));
                Trace.WriteLine(string.Format("FRN: {0}", usnEntry.UsnRecord.FileReferenceNumber));

                count++;
            }
        }
    }
}