// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// TaskPropertiesTest.cs
//
// Test class using UnitTestDriver that test the ID and CreationOptions property of a Task
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace System.Threading.Tasks.Tests
{
    public sealed class TaskPropertiesTests
    {
        /// <summary>
        /// Test to ensure that the task and task scheduler IDs are unique when tasks are started
        /// </summary>
        [Fact]
        public static void TaskIDTest()
        {
            // Read Parameters
            int taskCount = 100; // default to 1 task
            int[] taskIDs = new int[2 * taskCount];
            int[] tmIDs = new int[2 * taskCount];

            // Create Tasks under different taskScheduler
            for (int i = 0; i < taskCount; i++)
            {
                int iCopy = 2 * i;
                Task t1 = Task.Factory.StartNew(() => { tmIDs[iCopy] = TaskScheduler.Current.Id; }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
                taskIDs[iCopy] = t1.Id;
                t1.Wait();

                Task t2 = Task.Factory.StartNew(() => { tmIDs[iCopy + 1] = TaskScheduler.Current.Id; }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
                taskIDs[iCopy + 1] = t2.Id;
                t2.Wait();
            }

            // Verification -- Verify that task IDs are unique and equal to the number of tasks created
            for (int i = 0; i < taskIDs.Length; i++)
            {
                var id = taskIDs[i];
                for (int j = 0; j < taskIDs.Length; j++)
                {
                    var testID = taskIDs[j];
                    if (id.Equals(testID) && i != j)
                        Assert.True(false, string.Format("Found matching Task.ID for different tasks at index i [" + i + "] and j [" + j + "].  ID: " + id));
                }
            }
        }

        //Use the Start method to start the task
        [Fact]
        public static void TaskOptionsTestAsync()
        {
            TaskOptionTest(false);
        }

        //Use the RunSynchronously method to start the task
        [Fact]
        public static void TaskOptionsTestSync()
        {
            TaskOptionTest(true);
        }

        /// <summary>
        /// Create as many tasks as the options combination allow and start them and check to ensure that the options are correct
        /// </summary>
        internal static void TaskOptionTest(bool runSync)
        {
            // Read Parameters

            //Get the number of combinations of all options to try, currently there are four options and this relies on the product implementation
            //that each option is bit field
            int allOptions = 1;
            int y = Enum.GetNames(typeof(TaskCreationOptions)).Length - 1;

            // calculating 2^y
            for (int i = 1; i < y; i++)
            {
                allOptions = allOptions * 2;
            }
            Debug.WriteLine("Exhaustively testing {0} values for TaskCreationOptions", allOptions);

            Task[] tasks = new Task[allOptions];
            for (int i = 0; i < allOptions; i++)
            {
                TaskCreationOptions options = (TaskCreationOptions)i;

                tasks[i] = new Task(() => { }, options);
                if (runSync)
                    tasks[i].RunSynchronously();
                else
                    tasks[i].Start();
            }

            // Verification
            for (int i = 0; i < allOptions; i++)
            {
                if ((int)(tasks[i].CreationOptions) != i)
                {
                    Assert.True(false, string.Format("Task.CreationOptions failed at Option = {0}", i));
                }
            }
        }
    }
}
