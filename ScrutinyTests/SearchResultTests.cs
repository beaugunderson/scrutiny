using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using NTFS;

using Scrutiny.Models;
using Scrutiny.WPF;

namespace ScrutinyTests
{
    [TestClass]
    public class SearchResultTests
    {
        private UsnJournal _journal;

        public TestContext TestContext
        {
            get;
            set;
        }

        [TestInitialize]
        public void TestSetup()
        {
            _journal = new UsnJournal(new DriveInfo("C"));

            Assert.IsNotNull(_journal);
        }

        [TestMethod]
        public void TestNewSearchResult()
        {
            int count = 0;

            foreach (var usnEntry in _journal.GetFilesMatchingFilter("*"))
            {
                if (count > 5)
                {
                    return;
                }

                Assert.IsNotNull(usnEntry);

                var searchResult = new SearchResult(usnEntry, _journal);

                Assert.IsNotNull(searchResult.PathAndName);

                Assert.IsNotNull(searchResult.Size);
                Assert.IsNotNull(searchResult.LastModified);

                Trace.WriteLine(searchResult.PathAndName);

                Trace.WriteLine(searchResult.Size);
                Trace.WriteLine(searchResult.LastModified);

                count++;
            } 
        }

        [TestMethod]
        public void TestDefaultSearchResultSpeedParallel()
        {
            TestSearchResultSpeed();
        }

        [TestMethod]
        public void TestTenThreadSearchResultSpeedParallel()
        {
            TestSearchResultSpeed(10);
        }

        [Ignore]
        private void TestSearchResultSpeed(int threads = 1)
        {
            var context = new SynchronizationContext();
            var searchResults = new ThreadSafeObservableCollection<SearchResult>(synchronizationContext: context);

            TestContext.BeginTimer("Reading journal");

            _journal.GetFilesMatchingFilter("*")
                .AsParallel()
                .WithDegreeOfParallelism(threads)
                .Take(1000)
                .ForAll(entry => searchResults.Add(new SearchResult(entry, _journal)));

            TestContext.EndTimer("Reading journal");

            Trace.WriteLine(string.Format("Read and added {0} objects.", searchResults.Count));
        }
    }
}