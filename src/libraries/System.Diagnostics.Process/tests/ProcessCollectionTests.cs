// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Linq;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class ProcessCollectionTests : ProcessTestBase
    {
        [Fact]
        public void TestModuleCollectionBehavior()
        {
            ProcessModule[] mArray = Process.GetCurrentProcess().Modules.Cast<ProcessModule>().ToArray();

            // Constructor
            ProcessModuleCollection moduleCollection = new ProcessModuleCollection(mArray);

            // Count
            Assert.Equal(mArray.Count(), moduleCollection.Count);

            // get_item, Contains, IndexOf
            for (int i = 0; i < mArray.Count(); i++)
            {
                Assert.Equal(mArray[i], moduleCollection[i]);
                Assert.True(moduleCollection.Contains(mArray[i]));
                Assert.Equal(i, moduleCollection.IndexOf(mArray[i]));
            }

            // CopyTo
            ProcessModule[] moduleArray = new ProcessModule[moduleCollection.Count + 1];
            moduleCollection.CopyTo(moduleArray, 1);
            for (int i = 0; i < mArray.Count(); i++)
            {
                Assert.Equal(mArray[i], moduleArray[i + 1]);
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => moduleCollection.CopyTo(moduleArray, -1));

            // Explicit interface implementations
            Assert.False(((ICollection)moduleCollection).IsSynchronized);
            Assert.NotNull(((ICollection)moduleCollection).SyncRoot);

            moduleArray = new ProcessModule[moduleCollection.Count];
            ((ICollection)moduleCollection).CopyTo(moduleArray, 0);
            Assert.Equal(moduleCollection.Cast<ProcessModule>().ToArray(), moduleArray);

            // GetEnumerator
            IEnumerator enumerator = moduleCollection.GetEnumerator();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            for (int i = 0; i < moduleCollection.Count; i++)
            {
                enumerator.MoveNext();
                Assert.Equal(moduleCollection[i], enumerator.Current);
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Retrieving information about local processes is not supported on uap")]
        public void TestThreadCollectionBehavior()
        {
            CreateDefaultProcess();
            
            ProcessThread[] tArray = _process.Threads.Cast<ProcessThread>().ToArray();
            int countOfTArray = tArray.Count();

            // constructor
            ProcessThreadCollection threadCollection = new ProcessThreadCollection(tArray);

            // Count
            Assert.Equal(countOfTArray, threadCollection.Count);

            // get_item, Contains, IndexOf
            for (int i = 0; i < countOfTArray; i++)
            {
                Assert.Equal(tArray[i], threadCollection[i]);
                Assert.True(threadCollection.Contains(tArray[i]));
                Assert.Equal(i, threadCollection.IndexOf(tArray[i]));
            }

            // CopyTo
            ProcessThread[] threadArray = new ProcessThread[threadCollection.Count + 1];
            threadCollection.CopyTo(threadArray, 1);
            for (int i = 0; i < countOfTArray; i++)
            {
                Assert.Equal(tArray[i], threadArray[i + 1]);
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => threadCollection.CopyTo(threadArray, -1));

            // Remove
            threadCollection.Remove(tArray[0]);
            Assert.Equal(-1, threadCollection.IndexOf(tArray[0]));
            Assert.False(threadCollection.Contains(tArray[0]));
            // Try remove non existent member
            threadCollection.Remove(tArray[0]);
            // Cleanup after remove
            threadCollection.Insert(0, tArray[0]);

            // Add
            threadCollection.Add(default(ProcessThread));
            Assert.Equal(threadCollection.Count - 1, threadCollection.IndexOf(default(ProcessThread)));
            // Add same member again
            threadCollection.Add(default(ProcessThread));
            Assert.Equal(threadCollection.Count - 2, threadCollection.IndexOf(default(ProcessThread)));
            Assert.Equal(default(ProcessThread), threadCollection[threadCollection.Count - 1]);
            // Cleanup after Add.
            threadCollection.Remove(default(ProcessThread));
            threadCollection.Remove(default(ProcessThread));
            Assert.False(threadCollection.Contains(default(ProcessThread)));

            // Insert
            int index = threadCollection.Count / 2;
            int initialCount = threadCollection.Count;
            threadCollection.Insert(index, null);
            Assert.Equal(index, threadCollection.IndexOf(null));
            Assert.Equal(initialCount + 1, threadCollection.Count);
            // Insert at invalid index
            Assert.Throws<ArgumentOutOfRangeException>(() => threadCollection.Insert(-1, tArray[0]));

            // Explicit interface implementations
            Assert.False(((ICollection)threadCollection).IsSynchronized);
            Assert.NotNull(((ICollection)threadCollection).SyncRoot);

            threadArray = new ProcessThread[threadCollection.Count];
            ((ICollection)threadCollection).CopyTo(threadArray, 0);
            Assert.Equal(threadCollection.Cast<ProcessThread>().ToArray(), threadArray);

            // GetEnumerator
            IEnumerator enumerator = threadCollection.GetEnumerator();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            for (int i = 0; i < threadCollection.Count; i++)
            {
                enumerator.MoveNext();
                Assert.Equal(threadCollection[i], enumerator.Current);
            }
        }
    }
}
