using System;
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

                string path = _journal.GetPathFromFileReference(record.UsnRecord.ParentFileReferenceNumber);

                Trace.WriteLine(string.Format("Name: {0}", record.Name));
                Trace.WriteLine(string.Format("Path: {0}", path));
                Trace.WriteLine(string.Format("FRN: {0}", record.UsnRecord.FileReferenceNumber));
                Trace.WriteLine(string.Format("PFRN: {0}", record.UsnRecord.ParentFileReferenceNumber));

                Trace.WriteLine("");

                count++;
            }
        }

        [TestMethod]
        public void TestGetFileInformation()
        {
            var path = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "Test.txt");

            File.Create(path).Close();

            var fileInformation = UsnJournal.GetFileInformation(path);

            Assert.IsNotNull(fileInformation);

            Trace.WriteLine(fileInformation.LastAccessTime.ToDateTime());
            Trace.WriteLine(fileInformation.LastWriteTime.ToDateTime());
        }

        [TestMethod]
        public void TestGetFilesMatchingFilter()
        {
            int count = 0;

            foreach (var record in _journal.GetVolumeFiles())
            {
                if (count > 25)
                {
                    return;
                }

                Assert.IsNotNull(record);

                string path = _journal.GetPathFromFileReference(record.UsnRecord.ParentFileReferenceNumber);

                Trace.WriteLine(string.Format("Name: {0}", record.Name));
                Trace.WriteLine(string.Format("Path: {0}", path));
                Trace.WriteLine(string.Format("FRN: {0}", record.UsnRecord.FileReferenceNumber));
                Trace.WriteLine(string.Format("PFRN: {0}", record.UsnRecord.ParentFileReferenceNumber));

                Trace.WriteLine("");

                count++;
            }
        }
    }
}