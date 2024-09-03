// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tracing.Tests.Common;
using Xunit;

namespace Tracing.Tests
{
    using Xunit;
    using Assert = Tracing.Tests.Common.Assert;
    
    public static class EventActivityIdControlTest
    {
        internal enum ActivityControlCode : uint
        {
            EVENT_ACTIVITY_CTRL_GET_ID = 1,
            EVENT_ACTIVITY_CTRL_SET_ID = 2,
            EVENT_ACTIVITY_CTRL_CREATE_ID = 3,
            EVENT_ACTIVITY_CTRL_GET_SET_ID = 4,
            EVENT_ACTIVITY_CTRL_CREATE_SET_ID = 5
        };

        private const uint NumThreads = 10;
        private const uint NumTasks = 20;

        private static MethodInfo s_EventActivityIdControl;
        private static bool s_FailureEncountered = false;

        [Fact]
        public static int TestEntryPoint()
        {
            if(!Initialize())
            {
                return -1;
            }

            // Run the test on the start-up thread.
            TestThreadProc();

            // Run the test on some background threads.
            Thread[] threads = new Thread[NumThreads];
            for(int i=0; i<NumThreads; i++)
            {
                threads[i] = new Thread(new ThreadStart(TestThreadProc));
                threads[i].Start();
            }

            // Wait for all threads to complete.
            for(int i=0; i<NumThreads; i++)
            {
                threads[i].Join();
            }

            // Run the test on some background tasks.
            Task[] tasks = new Task[NumTasks];
            for(int i=0; i<NumTasks; i++)
            {
                tasks[i] = Task.Run(new Action(TestThreadProc));
            }
            Task.WaitAll(tasks);

            // Return the result.
            return s_FailureEncountered ? 0 : 100;
        }

        private static void TestThreadProc()
        {
            Guid activityId = Guid.Empty;

            try
            {
                // Activity ID starts as Guid.Empty.
                int retCode = EventActivityIdControl(
                    ActivityControlCode.EVENT_ACTIVITY_CTRL_GET_ID,
                    ref activityId);
                Assert.Equal<int>(nameof(retCode), retCode, 0);
                Assert.Equal<Guid>(nameof(activityId), activityId, Guid.Empty);

                // Set the activity ID to a random GUID and then confirm that it was properly set.
                activityId = Guid.NewGuid();
                retCode = EventActivityIdControl(
                    ActivityControlCode.EVENT_ACTIVITY_CTRL_SET_ID,
                    ref activityId);
                Assert.Equal<int>(nameof(retCode), retCode, 0);

                Guid currActivityId = Guid.Empty;
                retCode = EventActivityIdControl(
                    ActivityControlCode.EVENT_ACTIVITY_CTRL_GET_ID,
                    ref currActivityId);
                Assert.Equal<int>(nameof(retCode), retCode, 0);
                Assert.Equal<Guid>(nameof(currActivityId), currActivityId, activityId);

                // Set and get the activity ID in one call.
                activityId = Guid.NewGuid();
                Guid savedActivityId = activityId;
                retCode = EventActivityIdControl(
                    ActivityControlCode.EVENT_ACTIVITY_CTRL_GET_SET_ID,
                    ref activityId);
                Assert.Equal<int>(nameof(retCode), retCode, 0);
                Assert.Equal<Guid>(nameof(currActivityId), currActivityId, activityId);

                // Validate that the value we specified in the previous call is what comes back from a call to Get.
                retCode = EventActivityIdControl(
                    ActivityControlCode.EVENT_ACTIVITY_CTRL_GET_ID,
                    ref activityId);
                Assert.Equal<int>(nameof(retCode), retCode, 0);
                Assert.Equal<Guid>(nameof(savedActivityId), savedActivityId, activityId);

                // Create a new ID but don't change the current value.
                Guid newActivityId = Guid.Empty;
                retCode = EventActivityIdControl(
                    ActivityControlCode.EVENT_ACTIVITY_CTRL_CREATE_ID,
                    ref newActivityId);
                Assert.Equal<int>(nameof(retCode), retCode, 0);
                Assert.NotEqual<Guid>(nameof(newActivityId), newActivityId, Guid.Empty);

                retCode = EventActivityIdControl(
                    ActivityControlCode.EVENT_ACTIVITY_CTRL_GET_ID,
                    ref activityId);
                Assert.Equal<int>(nameof(retCode), retCode, 0);
                Assert.Equal<Guid>(nameof(savedActivityId), savedActivityId, activityId);

                // Create a new ID and set it in one action.
                retCode = EventActivityIdControl(
                    ActivityControlCode.EVENT_ACTIVITY_CTRL_CREATE_SET_ID,
                    ref activityId);
                Assert.Equal<int>(nameof(retCode), retCode, 0);
                Assert.Equal<Guid>(nameof(savedActivityId), savedActivityId, activityId);

                savedActivityId = activityId;
                retCode = EventActivityIdControl(
                    ActivityControlCode.EVENT_ACTIVITY_CTRL_GET_ID,
                    ref activityId);
                Assert.Equal<int>(nameof(retCode), retCode, 0);
                Assert.NotEqual<Guid>(nameof(savedActivityId), savedActivityId, activityId);
                Assert.NotEqual<Guid>(nameof(activityId), activityId, Guid.Empty);

                retCode = EventActivityIdControl(
                    ActivityControlCode.EVENT_ACTIVITY_CTRL_GET_ID,
                    ref newActivityId);
                Assert.Equal<int>(nameof(retCode), retCode, 0);
                Assert.Equal<Guid>(nameof(activityId), activityId, newActivityId);

                // Set the activity ID back to zero.
                activityId = Guid.Empty;
                retCode = EventActivityIdControl(
                    ActivityControlCode.EVENT_ACTIVITY_CTRL_SET_ID,
                    ref activityId);
                Assert.Equal<int>(nameof(retCode), retCode, 0);

                retCode = EventActivityIdControl(
                    ActivityControlCode.EVENT_ACTIVITY_CTRL_GET_ID,
                    ref activityId);
                Assert.Equal<int>(nameof(retCode), retCode, 0);
                Assert.Equal<Guid>(nameof(activityId), activityId, Guid.Empty);

                // Try pass an invalid control code.
                activityId = Guid.NewGuid();
                savedActivityId = activityId;
                retCode = EventActivityIdControl(
                    (ActivityControlCode)10,
                    ref activityId);
                Assert.Equal<int>(nameof(retCode), retCode, 1);
                Assert.Equal<Guid>(nameof(activityId), activityId, savedActivityId);
            }
            catch(Exception ex)
            {
                s_FailureEncountered = true;
                Console.WriteLine(ex.ToString());
            }
        }

        private static int EventActivityIdControl(
            ActivityControlCode controlCode,
            ref Guid activityId)
        {
            object[] parameters = new object[]
            {
                (uint)controlCode,
                activityId
            };

            int retCode = (int) s_EventActivityIdControl.Invoke(
                null,
                parameters);

            // Copy the by ref activityid out of the parameters array.
            activityId = (Guid)parameters[1];
            return retCode;
        }

        private static bool Initialize()
        {
            // Reflect over System.Private.CoreLib and get the EventPipeEventProvider type and EventActivityIdControl method.
            Assembly SPC = typeof(System.Diagnostics.Tracing.EventSource).Assembly;
            if(SPC == null)
            {
                Console.WriteLine("Failed to get System.Private.CoreLib assembly.");
                return false;
            }
            Type eventPipeEventProviderType = SPC.GetType("System.Diagnostics.Tracing.EventPipeEventProvider");
            if(eventPipeEventProviderType == null)
            {
                Console.WriteLine("Failed to get System.Diagnostics.Tracing.EventPipeEventProvider type.");
                return false;
            }
            s_EventActivityIdControl = eventPipeEventProviderType.GetMethod("EventActivityIdControl", BindingFlags.NonPublic | BindingFlags.Static );
            if(s_EventActivityIdControl == null)
            {
                Console.WriteLine("Failed to get EventActivityIdControl method.");
                return false;
            }

            return true;
        }
    }
}
