// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Test
{
    /// <summary>The class that contains the unit tests of the BlockingCollection.</summary>
    public class BlockingCollectionTests
    {
        [Fact]
        public static void TestBlockingCollectionBasic()
        {
            BlockingCollection<int> bc = new BlockingCollection<int>(3);
            Task[] tks = new Task[2];
            // A simple blocking consumer with no cancellation.
            int expect = 1;
            tks[0] = Task.Factory.StartNew(() =>
            {
                while (!bc.IsCompleted)
                {
                    try
                    {
                        int data = bc.Take();
                        Assert.Equal(expect, data);
                        expect++;
                    }
                    catch (InvalidOperationException) { } // throw when CompleteAdding called
                }
            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            // A simple blocking producer with no cancellation.
            tks[1] = Task.Factory.StartNew(() =>
            {
                bc.Add(1);
                bc.Add(2);
                bc.Add(3);
                // Let consumer know we are done.
                bc.CompleteAdding();
            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            Task.WaitAll(tks);
        }
    }
}
