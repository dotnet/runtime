// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class FileSystemWatcherFilterListTests : FileSystemWatcherTest
    {
        [Fact]
        public void DefaultFiltersValue()
        {
            var watcher = new FileSystemWatcher();
            Assert.Equal(0, watcher.Filters.Count);
            Assert.Empty(watcher.Filters);
            Assert.NotNull(watcher.Filters);
            Assert.Equal(new string[] { }, watcher.Filters);
        }

        [Fact]
        public void AddFilterToFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");
            Assert.Equal(2, watcher.Filters.Count);
            Assert.Equal(new string[] { "*.pdb", "*.dll" }, watcher.Filters);

            string[] copied = new string[2];
            watcher.Filters.CopyTo(copied, 0);
            Assert.Equal(new string[] { "*.pdb", "*.dll" }, copied);
        }

        [Fact]
        public void FiltersCaseSensitive()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("foo");
            Assert.Equal("foo", watcher.Filters[0]);
            watcher.Filters[0] = "Foo";
            Assert.Equal("Foo", watcher.Filters[0]);
        }

        [Fact]
        public void RemoveFilterFromFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");

            watcher.Filters.Remove("*.pdb");
            Assert.DoesNotContain(watcher.Filters, t => t == "*.pdb");
            Assert.Equal(new string[] { "*.dll" }, watcher.Filters);

            // No Exception is thrown while removing an item which is not present in the list.
            watcher.Filters.Remove("*.pdb");
        }

        [Fact]
        public void AddEmptyStringToFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");

            watcher.Filters.Add(string.Empty);
            Assert.Equal(3, watcher.Filters.Count);
            Assert.Equal(new string[] { "*.pdb", "*.dll", "*" }, watcher.Filters);
        }

        [Fact]
        public void AddNullToFilters()
        {
            var watcher = new FileSystemWatcher();

            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");

            watcher.Filters.Add(null);
            Assert.Equal(3, watcher.Filters.Count);
            Assert.Equal(new string[] { "*.pdb", "*.dll", "*" }, watcher.Filters);
        }

        [Fact]
        public void SetEmptyStringToFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");

            watcher.Filters[0] = string.Empty;
            Assert.Equal(2, watcher.Filters.Count);
            Assert.Equal("*", watcher.Filters[0]);
            Assert.Equal(new string[] { "*", "*.dll"}, watcher.Filters);
        }

        [Fact]
        public void RemoveEmptyStringToFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");
            watcher.Filters.Add(string.Empty);

            Assert.Equal(3, watcher.Filters.Count);
            watcher.Filters.Remove(string.Empty);
            Assert.Equal(3, watcher.Filters.Count);
            Assert.Equal(new string[] { "*.pdb", "*.dll", "*" }, watcher.Filters);
        }

        [Fact]
        public void RemoveAtFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");
            
            watcher.Filters.RemoveAt(0);
            Assert.Equal(1, watcher.Filters.Count);
            Assert.Equal("*.dll", watcher.Filter);
            Assert.Equal(new string[] {"*.dll" }, watcher.Filters);
        }

        [Fact]
        public void RemoveAtEmptyFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");

            watcher.Filters.RemoveAt(0);
            Assert.Equal(0, watcher.Filters.Count);
            Assert.Equal("*", watcher.Filter);
            Assert.Equal(new string[] { }, watcher.Filters);
        }

        [Fact]
        public void SetNullToFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");

            watcher.Filters[0] = null;
            Assert.Equal(2, watcher.Filters.Count);
            Assert.Equal("*", watcher.Filters[0]);
            Assert.Equal(new string[] { "*", "*.dll" }, watcher.Filters);
        }

        [Fact]
        public void ContainsEmptyStringFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");
            watcher.Filters.Add(string.Empty);

            Assert.False(watcher.Filters.Contains(string.Empty));
            Assert.True(watcher.Filters.Contains("*"));
        }

        [Fact]
        public void ContainsNullFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");
            watcher.Filters.Add(null);

            Assert.False(watcher.Filters.Contains(null));
            Assert.True(watcher.Filters.Contains("*"));
        }

        [Fact]
        public void ContainsFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");
            
            Assert.True(watcher.Filters.Contains("*.pdb"));
        }

        [Fact]
        public void InsertEmptyStringFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");
            watcher.Filters.Insert(1, string.Empty);

            Assert.Equal("*", watcher.Filters[1]);
            Assert.Equal(3, watcher.Filters.Count);
            Assert.Equal(new string[] { "*.pdb", "*", "*.dll" }, watcher.Filters);
        }

        [Fact]
        public void InsertNullFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");
            watcher.Filters.Insert(1, null);

            Assert.Equal("*", watcher.Filters[1]);
            Assert.Equal(3, watcher.Filters.Count);
            Assert.Equal(new string[] { "*.pdb", "*", "*.dll" }, watcher.Filters);
        }

        [Fact]
        public void InsertFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");
            watcher.Filters.Insert(1, "foo");

            Assert.Equal("foo", watcher.Filters[1]);
            Assert.Equal(3, watcher.Filters.Count);
            Assert.Equal(new string[] { "*.pdb", "foo", "*.dll" }, watcher.Filters);
        }

        [Fact]
        public void InsertAtZero()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");
            watcher.Filters.Insert(0, "foo");

            Assert.Equal("foo", watcher.Filters[0]);
            Assert.Equal("foo", watcher.Filter);
            Assert.Equal(3, watcher.Filters.Count);
            Assert.Equal(new string[] { "foo", "*.pdb", "*.dll" }, watcher.Filters);
        }
        [Fact]
        public void IndexOfEmptyStringFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");
            watcher.Filters.Add(string.Empty);

            Assert.Equal(-1, watcher.Filters.IndexOf(string.Empty));
        }

        [Fact]
        public void IndexOfNullFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");
            watcher.Filters.Add(null);

            Assert.Equal(-1, watcher.Filters.IndexOf(null));
        }

        [Fact]
        public void IndexOfFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");
            
            Assert.Equal(-1, watcher.Filters.IndexOf("foo"));
            Assert.Equal(0, watcher.Filters.IndexOf("*.pdb"));
        }

        [Fact]
        public void GetTypeFilters()
        {
            var watcher = new FileSystemWatcher();
            Assert.IsAssignableFrom<Collection<string>>(watcher.Filters);
        }

        [Fact]
        public void ClearFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");

            watcher.Filters.Clear();
            Assert.Equal(0, watcher.Filters.Count);
            Assert.Equal(new string[] { }, watcher.Filters) ;
        }

        [Fact]
        public void GetFilterAfterFiltersClear()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            {
                var watcher = new FileSystemWatcher(testDirectory.Path);
                watcher.Filters.Add("*.pdb");
                watcher.Filters.Add("*.dll");

                watcher.Filters.Clear();
                Assert.Equal("*", watcher.Filter);
                Assert.Equal(new string[] { }, watcher.Filters);
            }
        }

        [Fact]
        public void GetFiltersAfterFiltersClear()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            {
                var watcher = new FileSystemWatcher(testDirectory.Path);
                watcher.Filters.Add("*.pdb");
                watcher.Filters.Add("*.dll");

                watcher.Filters.Clear();
                Assert.Throws<ArgumentOutOfRangeException>(() => watcher.Filters[0]);
                Assert.Equal(0, watcher.Filters.Count);
                Assert.Empty(watcher.Filters);
                Assert.NotNull(watcher.Filters);
            }
        }

        [Fact]
        public void InvalidOperationsOnFilters()
        {
            var watcher = new FileSystemWatcher();
            watcher.Filters.Add("*.pdb");
            watcher.Filters.Add("*.dll");

            Assert.Throws<ArgumentOutOfRangeException>(() => watcher.Filters.Insert(4, "*"));            
            watcher.Filters.Clear();
            Assert.Throws<ArgumentOutOfRangeException>(() => watcher.Filters[0]);
        }

        [Fact]
        public void SetAndGetFilterProperty()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            {
                var watcher = new FileSystemWatcher(testDirectory.Path, "*.pdb");
                watcher.Filters.Add("foo");
                Assert.Equal(2, watcher.Filters.Count);
                Assert.Equal(new string[] { "*.pdb", "foo" }, watcher.Filters);

                watcher.Filter = "*.doc";
                Assert.Equal(1, watcher.Filters.Count);
                Assert.Equal("*.doc", watcher.Filter);
                Assert.Equal("*.doc", watcher.Filters[0]);
                Assert.Equal(new string[] { "*.doc" }, watcher.Filters);

                watcher.Filters.Clear();
                Assert.Equal("*", watcher.Filter);
            }
        }

        [Fact]
        public void SetAndGetFiltersProperty()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            {
                var watcher = new FileSystemWatcher(testDirectory.Path, "*.pdb");
                watcher.Filters.Add("foo");
                Assert.Equal(new string[] { "*.pdb", "foo" }, watcher.Filters);
            }
        }

        [Fact]
        public void FileSystemWatcher_File_Delete_MultipleFilters()
        {
            // Check delete events against multiple filters

            DirectoryInfo directory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo fileOne = new FileInfo(Path.Combine(directory.FullName, GetTestFileName()));
            FileInfo fileTwo = new FileInfo(Path.Combine(directory.FullName, GetTestFileName()));
            FileInfo fileThree = new FileInfo(Path.Combine(directory.FullName, GetTestFileName()));
            fileOne.Create().Dispose();
            fileTwo.Create().Dispose();
            fileThree.Create().Dispose();

            using (var watcher = new FileSystemWatcher(directory.FullName))
            {
                watcher.Filters.Add(fileOne.Name);
                watcher.Filters.Add(fileTwo.Name);

                ExpectEvent(watcher, WatcherChangeTypes.Deleted, () => fileOne.Delete(), cleanup: null, expectedPath : fileOne.FullName);
                ExpectEvent(watcher, WatcherChangeTypes.Deleted, () => fileTwo.Delete(), cleanup: null, expectedPath: fileTwo.FullName );
                ExpectNoEvent(watcher, WatcherChangeTypes.Deleted, () => fileThree.Delete(), cleanup: null, expectedPath: fileThree.FullName);
            }
        }

        [Fact]
        public void FileSystemWatcher_Directory_Create_MultipleFilters()
        {
            // Check create events against multiple filters

            DirectoryInfo directory = Directory.CreateDirectory(GetTestFilePath());
            string directoryOne = Path.Combine(directory.FullName, GetTestFileName());
            string directoryTwo = Path.Combine(directory.FullName, GetTestFileName());
            string directoryThree = Path.Combine(directory.FullName, GetTestFileName());

            using (var watcher = new FileSystemWatcher(directory.FullName))
            {
                watcher.Filters.Add(Path.GetFileName(directoryOne));
                watcher.Filters.Add(Path.GetFileName(directoryTwo));

                ExpectEvent(watcher, WatcherChangeTypes.Created, () => Directory.CreateDirectory(directoryOne), cleanup: null, expectedPath: directoryOne);
                ExpectEvent(watcher, WatcherChangeTypes.Created, () => Directory.CreateDirectory(directoryTwo), cleanup: null, expectedPath: directoryTwo);
                ExpectNoEvent(watcher, WatcherChangeTypes.Created, () => Directory.CreateDirectory(directoryThree), cleanup: null, expectedPath: directoryThree);
            }
        }

        [Fact]
        public void FileSystemWatcher_Directory_Create_Filter_Ctor()
        {
            // Check create events against multiple filters

            DirectoryInfo directory = Directory.CreateDirectory(GetTestFilePath());
            string directoryOne = Path.Combine(directory.FullName, GetTestFileName());
            string directoryTwo = Path.Combine(directory.FullName, GetTestFileName());
            string directoryThree = Path.Combine(directory.FullName, GetTestFileName());

            using (var watcher = new FileSystemWatcher(directory.FullName, Path.GetFileName(directoryOne)))
            {
                watcher.Filters.Add(Path.GetFileName(directoryTwo));

                ExpectEvent(watcher, WatcherChangeTypes.Created, () => Directory.CreateDirectory(directoryOne), cleanup: null, expectedPath: directoryOne);
                ExpectEvent(watcher, WatcherChangeTypes.Created, () => Directory.CreateDirectory(directoryTwo), cleanup: null, expectedPath: directoryTwo);
                ExpectNoEvent(watcher, WatcherChangeTypes.Created, () => Directory.CreateDirectory(directoryThree), cleanup: null, expectedPath: directoryThree);
            }
        }

        [Fact]
        public void FileSystemWatcher_Directory_Delete_MultipleFilters()
        {
            DirectoryInfo directory = Directory.CreateDirectory(GetTestFilePath());
            DirectoryInfo directoryOne = Directory.CreateDirectory(Path.Combine(directory.FullName, GetTestFileName()));
            DirectoryInfo directoryTwo = Directory.CreateDirectory(Path.Combine(directory.FullName, GetTestFileName()));
            DirectoryInfo directoryThree = Directory.CreateDirectory(Path.Combine(directory.FullName, GetTestFileName()));

            using (var watcher = new FileSystemWatcher(directory.FullName))
            {
                watcher.Filters.Add(Path.GetFileName(directoryOne.FullName));
                watcher.Filters.Add(Path.GetFileName(directoryTwo.FullName));

                ExpectEvent(watcher, WatcherChangeTypes.Deleted, () => directoryOne.Delete(), cleanup: null, expectedPath: directoryOne.FullName);
                ExpectEvent(watcher, WatcherChangeTypes.Deleted, () => directoryTwo.Delete(), cleanup: null, expectedPath: directoryTwo.FullName);
                ExpectNoEvent(watcher, WatcherChangeTypes.Deleted, () => directoryThree.Delete(), cleanup: null, expectedPath: directoryThree.FullName);
            }
        }

        [Fact]
        public void FileSystemWatcher_File_Create_MultipleFilters()
        {
            DirectoryInfo directory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo fileOne = new FileInfo(Path.Combine(directory.FullName, GetTestFileName()));
            FileInfo fileTwo = new FileInfo(Path.Combine(directory.FullName, GetTestFileName()));
            FileInfo fileThree = new FileInfo(Path.Combine(directory.FullName, GetTestFileName()));

            using (var watcher = new FileSystemWatcher(directory.FullName))
            {
                watcher.Filters.Add(fileOne.Name);
                watcher.Filters.Add(fileTwo.Name);

                ExpectEvent(watcher, WatcherChangeTypes.Created, () => fileOne.Create().Dispose(), cleanup: null, expectedPath: fileOne.FullName);
                ExpectEvent(watcher, WatcherChangeTypes.Created, () => fileTwo.Create().Dispose(), cleanup: null, expectedPath: fileTwo.FullName);
                ExpectNoEvent(watcher, WatcherChangeTypes.Created, () => fileThree.Create().Dispose(), cleanup: null, expectedPath: fileThree.FullName);
            }
        }

        [Fact]
        public void FileSystemWatcher_ModifyFiltersConcurrentWithEvents()
        {
            DirectoryInfo directory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo fileOne = new FileInfo(Path.Combine(directory.FullName, GetTestFileName()));
            FileInfo fileTwo = new FileInfo(Path.Combine(directory.FullName, GetTestFileName()));
            FileInfo fileThree = new FileInfo(Path.Combine(directory.FullName, GetTestFileName()));

            using (var watcher = new FileSystemWatcher(directory.FullName))
            {
                watcher.Filters.Add(fileOne.Name);
                watcher.Filters.Add(fileTwo.Name);

                var cts = new CancellationTokenSource();
                Task modifier = Task.Run(() =>
                {
                    string otherFilter = Guid.NewGuid().ToString("N");
                    while (!cts.IsCancellationRequested)
                    {
                        watcher.Filters.Add(otherFilter);
                        watcher.Filters.RemoveAt(2);
                    }
                });

                ExpectEvent(watcher, WatcherChangeTypes.Created, () => fileOne.Create().Dispose(), cleanup: null, expectedPath: fileOne.FullName);
                ExpectEvent(watcher, WatcherChangeTypes.Created, () => fileTwo.Create().Dispose(), cleanup: null, expectedPath: fileTwo.FullName);
                ExpectNoEvent(watcher, WatcherChangeTypes.Created, () => fileThree.Create().Dispose(), cleanup: null, expectedPath: fileThree.FullName);

                cts.Cancel();
                modifier.Wait();
            }
        }
    }
}
