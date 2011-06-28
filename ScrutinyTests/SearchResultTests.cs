using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using NTFS;
using NTFS.Extensions;

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

            foreach (var usnEntry in _journal.GetVolumeFiles())
            {
                if (count > 25)
                {
                    return;
                }

                Assert.IsNotNull(usnEntry);

                var searchResult = new SearchResult(usnEntry, _journal);

                Assert.IsNotNull(searchResult.PathAndName);

                Assert.IsNotNull(searchResult.SizeInBytes);
                Assert.IsNotNull(searchResult.LastModified);

                Trace.WriteLine(searchResult.PathAndName);

                Trace.WriteLine(searchResult.SizeInBytes);
                Trace.WriteLine(searchResult.LastModified);

                count++;
            } 
        }

        [TestMethod]
        public void TestSequentialQuicksort()
        {
            TestContext.BeginTimer("Reading journal");

            var array = _journal.GetVolumeFiles().ToArray();
            
            TestContext.EndTimer("Reading journal");

            TestContext.BeginTimer("Sequential quicksort");

            array.SequentialQuickSort();

            TestContext.EndTimer("Sequential quicksort");
        }

        [TestMethod]
        public void TestParallelQuicksort()
        {
            TestContext.BeginTimer("Reading journal");

            var array = _journal.GetVolumeFiles().ToArray();
            
            TestContext.EndTimer("Reading journal");

            TestContext.BeginTimer("Parallel quicksort");

            array.ParallelQuickSort();

            TestContext.EndTimer("Parallel quicksort");
        }

        [TestMethod]
        public void TestDefaultSearchResultSpeedParallel()
        {
            TestSearchResultSpeed();
        }

        [TestMethod]
        public void TestTwoThreadSearchResultSpeedParallel()
        {
            TestSearchResultSpeed(2);
        }

        [Ignore]
        private void TestSearchResultSpeed(int threads = 1)
        {
            var context = new SynchronizationContext();
            var searchResults = new ThreadSafeObservableCollection<SearchResult>(synchronizationContext: context);

            TestContext.BeginTimer("Reading journal");

            _journal.GetVolumeFiles()
                .AsParallel()
                .WithDegreeOfParallelism(threads)
                .Take(1000)
                .ForAll(entry => searchResults.Add(new SearchResult(entry, _journal)));

            TestContext.EndTimer("Reading journal");

            // XXX: Sleep to allow the threads to finish
            Thread.Sleep(5000);

            Assert.AreEqual(1000, searchResults.Count);

            Trace.WriteLine(string.Format("Read and added {0} objects.", searchResults.Count));
        }
    }
}