using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Xunit;

namespace Threading.Tests
{
    public sealed class OSThreadId
    {
        private const int NumThreads = 10;
        private static MethodInfo s_osThreadIdGetMethod;
        private static ManualResetEvent s_resetEvent = new ManualResetEvent(false);
        private static ulong[] s_threadIds = new ulong[NumThreads];

        [Fact]
        public static int TestEntryPoint()
        {
            // The property to be tested is internal.
            Type runtimeThreadType = typeof(object).Assembly.GetType("System.Threading.Thread");
            Assert(runtimeThreadType != null);
            PropertyInfo osThreadIdProperty = runtimeThreadType.GetProperty("CurrentOSThreadId", BindingFlags.NonPublic | BindingFlags.Static);
            Assert(osThreadIdProperty != null);
            s_osThreadIdGetMethod = osThreadIdProperty.GetGetMethod(true);
            Assert(s_osThreadIdGetMethod != null);

            // Test the main thread.
            Assert(GetCurrentThreadId() > 0);

            // Create more threads.
            Thread[] threads = new Thread[NumThreads];
            for (int i = 0; i < NumThreads; i++)
            {
                threads[i] = new Thread(new ParameterizedThreadStart(ThreadProc));
                threads[i].Start(i);
            }

            // Now that all threads have been created, allow them to run.
            s_resetEvent.Set();

            // Wait for all threads to complete.
            for (int i = 0; i < NumThreads; i++)
            {
                threads[i].Join();
            }

            // Check for duplicate thread IDs.
            Array.Sort(s_threadIds);
            ulong previousThreadId = 0;
            for (int i = 0; i < NumThreads; i++)
            {
                if (i == 0)
                {
                    previousThreadId = s_threadIds[i];
                }
                else
                {
                    Assert(s_threadIds[i] > 0);
                    Assert(previousThreadId != s_threadIds[i]);
                    previousThreadId = s_threadIds[i];
                }
            }

            return 100;
        }

        private static ulong GetCurrentThreadId()
        {
            return (ulong)s_osThreadIdGetMethod.Invoke(null, null);
        }

        private static void ThreadProc(object state)
        {
            // Get the thread index.
            int threadIndex = (int)state;
            Assert(threadIndex >= 0 && threadIndex < NumThreads);

            // Wait for all threads to be created.
            s_resetEvent.WaitOne();

            // We now know that all threads were created before GetCurrentThread is called.
            // Thus, no thread IDs can be duplicates.
            ulong threadId = GetCurrentThreadId();

            // Ensure that the thread ID is valid.
            Assert(threadId > 0);

            // Save the thread ID so that it can be checked for duplicates.
            s_threadIds[threadIndex] = threadId;
        }

        private static void Assert(bool condition)
        {
            if (!condition)
            {
                throw new Exception("Assertion failed.");
            }
        }
    }
}
