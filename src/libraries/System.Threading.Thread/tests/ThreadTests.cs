// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Threading.Tests;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Threading.Threads.Tests
{
    public class DummyClass
    {
        public static string HostRunnerTest = RemoteExecutor.HostRunner;
    }

    public static class ThreadTests
    {
        private const int UnexpectedTimeoutMilliseconds = ThreadTestHelpers.UnexpectedTimeoutMilliseconds;
        private const int ExpectedTimeoutMilliseconds = ThreadTestHelpers.ExpectedTimeoutMilliseconds;

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void ConstructorTest()
        {
            const int SmallStackSizeBytes = 64 << 10; // 64 KB, currently accepted in all supported platforms, and is the PAL minimum
            const int LargeStackSizeBytes = 16 << 20; // 16 MB

            int pageSizeBytes = Environment.SystemPageSize;

            // Leave some buffer for other data structures that will take some of the allocated stack space
            int stackSizeBufferBytes = pageSizeBytes * 16;

            Action<Thread> startThreadAndJoin =
                t =>
                {
                    t.IsBackground = true;
                    t.Start();
                    Assert.True(t.Join(UnexpectedTimeoutMilliseconds));
                };
            Action<int> verifyStackSize =
                stackSizeBytes =>
                {
                    // Try to stack-allocate an array to verify that close to the expected amount of stack space is actually
                    // available
                    int bufferSizeBytes = Math.Max(SmallStackSizeBytes / 4, stackSizeBytes - stackSizeBufferBytes);
                    unsafe
                    {
                        byte* buffer = stackalloc byte[bufferSizeBytes];
                        for (int i = 0; i < bufferSizeBytes; i += pageSizeBytes)
                        {
                            Volatile.Write(ref buffer[i], 0xff);
                        }
                        Volatile.Write(ref buffer[bufferSizeBytes - 1], 0xff);
                    }
                };
            startThreadAndJoin(new Thread(() => verifyStackSize(0)));
            startThreadAndJoin(new Thread(() => verifyStackSize(0), 0));
            startThreadAndJoin(new Thread(() => verifyStackSize(SmallStackSizeBytes), SmallStackSizeBytes));
            startThreadAndJoin(new Thread(() => verifyStackSize(LargeStackSizeBytes), LargeStackSizeBytes));
            startThreadAndJoin(new Thread(state => verifyStackSize(0)));
            startThreadAndJoin(new Thread(state => verifyStackSize(0), 0));
            startThreadAndJoin(new Thread(state => verifyStackSize(SmallStackSizeBytes), SmallStackSizeBytes));
            startThreadAndJoin(new Thread(state => verifyStackSize(LargeStackSizeBytes), LargeStackSizeBytes));

            Assert.Throws<ArgumentNullException>(() => new Thread((ThreadStart)null));
            Assert.Throws<ArgumentNullException>(() => new Thread((ThreadStart)null, 0));
            Assert.Throws<ArgumentNullException>(() => new Thread((ParameterizedThreadStart)null));
            Assert.Throws<ArgumentNullException>(() => new Thread((ParameterizedThreadStart)null, 0));

            Assert.Throws<ArgumentOutOfRangeException>(() => new Thread(() => { }, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Thread(state => { }, -1));
        }

        public static IEnumerable<object[]> ApartmentStateTest_MemberData()
        {
            yield return
                new object[]
                {
#pragma warning disable 618 // Obsolete members
                    new Func<Thread, ApartmentState>(t => t.ApartmentState),
#pragma warning restore 618 // Obsolete members
                    new Func<Thread, ApartmentState, int>(
                        (t, value) =>
                        {
                            try
                            {
#pragma warning disable 618 // Obsolete members
                                t.ApartmentState = value;
#pragma warning restore 618 // Obsolete members
                                return 0;
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                return 1;
                            }
                            catch (PlatformNotSupportedException)
                            {
                                return 3;
                            }
                        }),
                    0
                };
            yield return
                new object[]
                {
                    new Func<Thread, ApartmentState>(t => t.GetApartmentState()),
                    new Func<Thread, ApartmentState, int>(
                        (t, value) =>
                        {
                            try
                            {
                                t.SetApartmentState(value);
                                return 0;
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                return 1;
                            }
                            catch (InvalidOperationException)
                            {
                                return 2;
                            }
                            catch (PlatformNotSupportedException)
                            {
                                return 3;
                            }
                        }),
                    1
                };
            yield return
                new object[]
                {
                    new Func<Thread, ApartmentState>(t => t.GetApartmentState()),
                    new Func<Thread, ApartmentState, int>(
                        (t, value) =>
                        {
                            try
                            {
                                return t.TrySetApartmentState(value) ? 0 : 2;
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                return 1;
                            }
                            catch (PlatformNotSupportedException)
                            {
                                return 3;
                            }
                        }),
                    2
                };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34543", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [InlineData("STAMain.exe", "GetApartmentStateTest")]
        [InlineData("STAMain.exe", "SetApartmentStateTest")]
        [InlineData("STAMain.exe", "WaitAllNotSupportedOnSta_Test0")]
        [InlineData("STAMain.exe", "WaitAllNotSupportedOnSta_Test1")]
        [InlineData("MTAMain.exe", "GetApartmentStateTest")]
        [InlineData("MTAMain.exe", "SetApartmentStateTest")]
        [InlineData("DefaultApartmentStateMain.exe", "GetApartmentStateTest")]
        [InlineData("DefaultApartmentStateMain.exe", "SetApartmentStateTest")]
        public static void ApartmentState_AttributePresent(string appName, string testName)
        {
            var psi = new ProcessStartInfo();
            psi.FileName = DummyClass.HostRunnerTest;
            psi.Arguments = $"{appName} {testName}";

            using (Process p = Process.Start(psi))
            {
                p.WaitForExit();
                Assert.Equal(PlatformDetection.IsWindows ? 0 : 2, p.ExitCode);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34543", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void ApartmentState_NoAttributePresent_DefaultState_Windows()
        {
            RemoteExecutor.Invoke(() =>
            {
                Assert.Equal(ApartmentState.MTA, Thread.CurrentThread.GetApartmentState());
                Assert.Throws<InvalidOperationException>(() => Thread.CurrentThread.SetApartmentState(ApartmentState.STA));
                Thread.CurrentThread.SetApartmentState(ApartmentState.MTA);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void ApartmentState_NoAttributePresent_DefaultState_Unix()
        {
            RemoteExecutor.Invoke(() =>
            {
                Assert.Equal(ApartmentState.Unknown, Thread.CurrentThread.GetApartmentState());
                Assert.Throws<PlatformNotSupportedException>(() => Thread.CurrentThread.SetApartmentState(ApartmentState.MTA));
            }).Dispose();
        }

        private static bool IsWindowsNanoServerAndRemoteExecutorSupported => PlatformDetection.IsWindowsNanoServer && RemoteExecutor.IsSupported;

        // Thread is always initialized as MTA irrespective of the attribute present.
        [ConditionalFact(nameof(IsWindowsNanoServerAndRemoteExecutorSupported))]
        public static void ApartmentState_NoAttributePresent_DefaultState_Nano()
        {
            RemoteExecutor.Invoke(() =>
            {
                Assert.Throws<InvalidOperationException>(() => Thread.CurrentThread.SetApartmentState(ApartmentState.STA));
                Assert.Equal(ApartmentState.MTA, Thread.CurrentThread.GetApartmentState());
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void ApartmentState_NoAttributePresent_STA_Unix()
        {
            RemoteExecutor.Invoke(() =>
            {
                Assert.Throws<PlatformNotSupportedException>(() => Thread.CurrentThread.SetApartmentState(ApartmentState.STA));
            }).Dispose();
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34543", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [MemberData(nameof(ApartmentStateTest_MemberData))]
        [PlatformSpecific(TestPlatforms.Windows)]  // Expected behavior differs on Unix and Windows
        public static void GetSetApartmentStateTest_ChangeAfterThreadStarted_Windows(
            Func<Thread, ApartmentState> getApartmentState,
            Func<Thread, ApartmentState, int> setApartmentState,
            int setType /* 0 = ApartmentState setter, 1 = SetApartmentState, 2 = TrySetApartmentState */)
        {
            ThreadTestHelpers.RunTestInBackgroundThread(() =>
            {
                var t = Thread.CurrentThread;
                Assert.Equal(1, setApartmentState(t, ApartmentState.STA - 1));
                Assert.Equal(1, setApartmentState(t, ApartmentState.Unknown + 1));

                Assert.Equal(ApartmentState.MTA, getApartmentState(t));
                Assert.Equal(0, setApartmentState(t, ApartmentState.MTA));
                Assert.Equal(ApartmentState.MTA, getApartmentState(t));
                Assert.Equal(setType == 0 ? 0 : 2, setApartmentState(t, ApartmentState.STA)); // cannot be changed after thread is started
                Assert.Equal(ApartmentState.MTA, getApartmentState(t));
            });
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34543", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [MemberData(nameof(ApartmentStateTest_MemberData))]
        [PlatformSpecific(TestPlatforms.Windows)]  // Expected behavior differs on Unix and Windows
        public static void ApartmentStateTest_ChangeBeforeThreadStarted_Windows(
            Func<Thread, ApartmentState> getApartmentState,
            Func<Thread, ApartmentState, int> setApartmentState,
            int setType /* 0 = ApartmentState setter, 1 = SetApartmentState, 2 = TrySetApartmentState */)
        {
            ApartmentState apartmentStateInThread = ApartmentState.Unknown;
            Thread t = null;
            t = new Thread(() => apartmentStateInThread = getApartmentState(t));
            t.IsBackground = true;
            Assert.Equal(0, setApartmentState(t, ApartmentState.STA));
            Assert.Equal(ApartmentState.STA, getApartmentState(t));
            Assert.Equal(setType == 0 ? 0 : 2, setApartmentState(t, ApartmentState.MTA)); // cannot be changed more than once
            Assert.Equal(ApartmentState.STA, getApartmentState(t));
            t.Start();
            Assert.True(t.Join(UnexpectedTimeoutMilliseconds));

            Assert.Equal(ApartmentState.STA, apartmentStateInThread);
        }


        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindowsNanoServer))]
        [MemberData(nameof(ApartmentStateTest_MemberData))]
        public static void ApartmentStateTest_ChangeBeforeThreadStarted_Windows_Nano_Server(
            Func<Thread, ApartmentState> getApartmentState,
            Func<Thread, ApartmentState, int> setApartmentState,
            int setType /* 0 = ApartmentState setter, 1 = SetApartmentState, 2 = TrySetApartmentState */)
        {
            ApartmentState apartmentStateInThread = ApartmentState.Unknown;
            Thread t = null;
            t = new Thread(() => apartmentStateInThread = getApartmentState(t));
            t.IsBackground = true;
            Assert.Equal(0, setApartmentState(t, ApartmentState.STA));
            Assert.Equal(ApartmentState.STA, getApartmentState(t));
            Assert.Equal(setType == 0 ? 0 : 2, setApartmentState(t, ApartmentState.MTA)); // cannot be changed more than once
            Assert.Equal(ApartmentState.STA, getApartmentState(t));

            Assert.Throws<ThreadStartException>(() => t.Start()); // Windows Nano Server does not support starting threads in the STA.
        }



        [Theory]
        [MemberData(nameof(ApartmentStateTest_MemberData))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Expected behavior differs on Unix and Windows
        public static void ApartmentStateTest_Unix(
            Func<Thread, ApartmentState> getApartmentState,
            Func<Thread, ApartmentState, int> setApartmentState,
            int setType /* 0 = ApartmentState setter, 1 = SetApartmentState, 2 = TrySetApartmentState */)
        {
            var t = new Thread(() => { });
            Assert.Equal(ApartmentState.Unknown, getApartmentState(t));
            Assert.Equal(0, setApartmentState(t, ApartmentState.Unknown));

            int expectedFailure;
            switch (setType)
            {
                case 0:
                    expectedFailure = 0; // ApartmentState setter - no exception, but value does not change
                    break;
                case 1:
                    expectedFailure = 3; // SetApartmentState - InvalidOperationException
                    break;
                default:
                    expectedFailure = 2; // TrySetApartmentState - returns false
                    break;
            }
            Assert.Equal(expectedFailure, setApartmentState(t, ApartmentState.STA));
            Assert.Equal(expectedFailure, setApartmentState(t, ApartmentState.MTA));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void CurrentCultureTest_DifferentThread()
        {
            CultureInfo culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            CultureInfo uiCulture = (CultureInfo)CultureInfo.CurrentCulture.Clone();

            ExceptionDispatchInfo exceptionFromThread = null;
            var t = new Thread(() =>
            {
                try
                {
                    Assert.Same(culture, Thread.CurrentThread.CurrentCulture);
                    Assert.Same(uiCulture, Thread.CurrentThread.CurrentUICulture);
                }
                catch (Exception e)
                {
                    exceptionFromThread = ExceptionDispatchInfo.Capture(e);
                }
            });

            // We allow setting thread culture of unstarted threads to ease .NET Framework migration. It is pattern used
            // in real world .NET Framework apps.
            t.CurrentCulture = culture;
            t.CurrentUICulture = uiCulture;

            // Cannot access culture properties on a thread object from a different thread
            Assert.Throws<InvalidOperationException>(() => t.CurrentCulture);
            Assert.Throws<InvalidOperationException>(() => t.CurrentUICulture);

            t.Start();
            // Cannot access culture properties on a thread object from a different thread once the thread is started
            // .NET Framework allowed this, but it did not work reliably. .NET Core throws instead.
            Assert.Throws<InvalidOperationException>(() => { t.CurrentCulture = culture; });
            Assert.Throws<InvalidOperationException>(() => { t.CurrentUICulture = uiCulture; });
            Assert.Throws<InvalidOperationException>(() => t.CurrentCulture);
            Assert.Throws<InvalidOperationException>(() => t.CurrentUICulture);
            t.Join();

            exceptionFromThread?.Throw();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void CurrentCultureTest()
        {
            ThreadTestHelpers.RunTestInBackgroundThread(() =>
            {
                var t = Thread.CurrentThread;
                var originalCulture = CultureInfo.CurrentCulture;
                var originalUICulture = CultureInfo.CurrentUICulture;
                var otherCulture = CultureInfo.InvariantCulture;

                // Culture properties return the same value as those on CultureInfo
                Assert.Equal(originalCulture, t.CurrentCulture);
                Assert.Equal(originalUICulture, t.CurrentUICulture);

                try
                {
                    // Changing culture properties on CultureInfo causes the values of properties on the current thread to change
                    CultureInfo.CurrentCulture = otherCulture;
                    CultureInfo.CurrentUICulture = otherCulture;
                    Assert.Equal(otherCulture, t.CurrentCulture);
                    Assert.Equal(otherCulture, t.CurrentUICulture);

                    // Changing culture properties on the current thread causes new values to be returned, and causes the values of
                    // properties on CultureInfo to change
                    t.CurrentCulture = originalCulture;
                    t.CurrentUICulture = originalUICulture;
                    Assert.Equal(originalCulture, t.CurrentCulture);
                    Assert.Equal(originalUICulture, t.CurrentUICulture);
                    Assert.Equal(originalCulture, CultureInfo.CurrentCulture);
                    Assert.Equal(originalUICulture, CultureInfo.CurrentUICulture);
                }
                finally
                {
                    CultureInfo.CurrentCulture = originalCulture;
                    CultureInfo.CurrentUICulture = originalUICulture;
                }

                Assert.Throws<ArgumentNullException>(() => t.CurrentCulture = null);
                Assert.Throws<ArgumentNullException>(() => t.CurrentUICulture = null);
            });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void CurrentPrincipalTest_SkipOnDesktopFramework()
        {
            ThreadTestHelpers.RunTestInBackgroundThread(() => Assert.Null(Thread.CurrentPrincipal));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void CurrentPrincipalTest()
        {
            ThreadTestHelpers.RunTestInBackgroundThread(() =>
            {
                var originalPrincipal = Thread.CurrentPrincipal;
                var otherPrincipal =
                    new GenericPrincipal(new GenericIdentity(string.Empty, string.Empty), new string[] { string.Empty });

                Thread.CurrentPrincipal = otherPrincipal;
                Assert.Equal(otherPrincipal, Thread.CurrentPrincipal);

                Thread.CurrentPrincipal = originalPrincipal;
                Assert.Equal(originalPrincipal, Thread.CurrentPrincipal);
            });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void CurrentPrincipalContextFlowTest()
        {
            ThreadTestHelpers.RunTestInBackgroundThread(async () =>
            {
                Thread.CurrentPrincipal = new ClaimsPrincipal();

                await Task.Run(async () =>
                {

                    Assert.IsType<ClaimsPrincipal>(Thread.CurrentPrincipal);

                    await Task.Run(async () =>
                    {
                        Assert.IsType<ClaimsPrincipal>(Thread.CurrentPrincipal);

                        Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity("name"), new string[0]);

                        await Task.Run(() =>
                        {
                            Assert.IsType<GenericPrincipal>(Thread.CurrentPrincipal);
                        });

                        Assert.IsType<GenericPrincipal>(Thread.CurrentPrincipal);
                    });

                    Assert.IsType<ClaimsPrincipal>(Thread.CurrentPrincipal);
                });

                Assert.IsType<ClaimsPrincipal>(Thread.CurrentPrincipal);
            });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void CurrentPrincipalContextFlowTest_NotFlow()
        {
            ThreadTestHelpers.RunTestInBackgroundThread(async () =>
            {
                Thread.CurrentPrincipal = new ClaimsPrincipal();

                Task task;
                using (ExecutionContext.SuppressFlow())
                {
                    Assert.True(ExecutionContext.IsFlowSuppressed());

                    task = Task.Run(() =>
                    {
                        Assert.Null(Thread.CurrentPrincipal);
                        Assert.False(ExecutionContext.IsFlowSuppressed());
                    });
                }

                Assert.False(ExecutionContext.IsFlowSuppressed());

                await task;
            });
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void CurrentPrincipal_SetNull()
        {
            // We run test on remote process because we need to set same principal policy
            // On .NET Framework default principal policy is PrincipalPolicy.UnauthenticatedPrincipal
            RemoteExecutor.Invoke(() =>
            {
                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.NoPrincipal);

                Assert.Null(Thread.CurrentPrincipal);

                Thread.CurrentPrincipal = null;
                Assert.Null(Thread.CurrentPrincipal);

                Thread.CurrentPrincipal = new ClaimsPrincipal();
                Assert.IsType<ClaimsPrincipal>(Thread.CurrentPrincipal);

                Thread.CurrentPrincipal = null;
                Assert.Null(Thread.CurrentPrincipal);

                Thread.CurrentPrincipal = new ClaimsPrincipal();
                Assert.IsType<ClaimsPrincipal>(Thread.CurrentPrincipal);
            }).Dispose();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void CurrentThreadTest()
        {
            Thread otherThread = null;
            var t = new Thread(() => otherThread = Thread.CurrentThread);
            t.IsBackground = true;
            t.Start();
            Assert.True(t.Join(UnexpectedTimeoutMilliseconds));

            Assert.Equal(t, otherThread);

            var mainThread = Thread.CurrentThread;
            Assert.NotNull(mainThread);
            Assert.NotEqual(mainThread, otherThread);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void ExecutionContextTest()
        {
            ThreadTestHelpers.RunTestInBackgroundThread(
                () => Assert.Equal(ExecutionContext.Capture(), Thread.CurrentThread.ExecutionContext));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void IsAliveTest()
        {
            var isAliveWhenRunning = false;
            Thread t = null;
            t = new Thread(() => isAliveWhenRunning = t.IsAlive);
            t.IsBackground = true;

            Assert.False(t.IsAlive);
            t.Start();
            Assert.True(t.Join(UnexpectedTimeoutMilliseconds));
            Assert.True(isAliveWhenRunning);
            Assert.False(t.IsAlive);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void IsBackgroundTest()
        {
            var t = new Thread(() => { });
            Assert.False(t.IsBackground);
            t.IsBackground = true;
            Assert.True(t.IsBackground);
            t.Start();
            Assert.True(t.Join(UnexpectedTimeoutMilliseconds));

            // Cannot use this property after the thread is dead
            Assert.Throws<ThreadStateException>(() => t.IsBackground);
            Assert.Throws<ThreadStateException>(() => t.IsBackground = false);
            Assert.Throws<ThreadStateException>(() => t.IsBackground = true);

            // Verify that the test process can shut down gracefully with a hung background thread
            t = new Thread(() => Thread.Sleep(Timeout.Infinite));
            t.IsBackground = true;
            t.Start();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void IsThreadPoolThreadTest()
        {
            var isThreadPoolThread = false;
            Thread t = null;
            t = new Thread(() => { isThreadPoolThread = t.IsThreadPoolThread; });
            t.IsBackground = true;
            Assert.False(t.IsThreadPoolThread);

            t.Start();
            Assert.True(t.Join(UnexpectedTimeoutMilliseconds));
            Assert.False(isThreadPoolThread);

            var e = new ManualResetEvent(false);
            ThreadPool.QueueUserWorkItem(
                state =>
                {
                    isThreadPoolThread = Thread.CurrentThread.IsThreadPoolThread;
                    e.Set();
                });
            e.CheckedWait();
            Assert.True(isThreadPoolThread);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void ManagedThreadIdTest()
        {
            var e = new ManualResetEvent(false);
            Action waitForThread;
            var t = ThreadTestHelpers.CreateGuardedThread(out waitForThread, e.CheckedWait);
            t.IsBackground = true;
            t.Start();
            Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, t.ManagedThreadId);
            e.Set();
            waitForThread();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void NameTest()
        {
            string name = Guid.NewGuid().ToString("N");
            Action waitForThread;
            var t =
                ThreadTestHelpers.CreateGuardedThread(out waitForThread, () =>
                {
                    var ct = Thread.CurrentThread;
                    Assert.Equal(name, ct.Name);
                    Assert.Throws<InvalidOperationException>(() => ct.Name = null);
                    Assert.Throws<InvalidOperationException>(() => ct.Name = name + "b");
                    Assert.Equal(name, ct.Name);
                });
            t.IsBackground = true;
            Assert.Null(t.Name);
            t.Name = null;
            t.Name = null;
            Assert.Null(t.Name);
            t.Name = name;
            Assert.Equal(name, t.Name);
            Assert.Throws<InvalidOperationException>(() => t.Name = null);
            Assert.Throws<InvalidOperationException>(() => t.Name = name + "b");
            Assert.Equal(name, t.Name);
            t.Start();
            waitForThread();

            ThreadTestHelpers.RunTestInBackgroundThread(() =>
            {
                var ct = Thread.CurrentThread;
                Assert.Null(ct.Name);
                ct.Name = null;
                ct.Name = null;
                Assert.Null(ct.Name);
                ct.Name = name;
                Assert.Equal(name, ct.Name);
                Assert.Throws<InvalidOperationException>(() => ct.Name = null);
                Assert.Throws<InvalidOperationException>(() => ct.Name = name + "b");
                Assert.Equal(name, ct.Name);
            });
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void ThreadNameDoesNotAffectProcessName()
        {
            // On Linux, changing the main thread name affects ProcessName.
            // To avoid that, .NET ignores requests to change the main thread name.
            RemoteExecutor.Invoke(() =>
            {
                const string ThreadName = "my-thread";
                Thread.CurrentThread.Name = ThreadName;
                Assert.Equal(ThreadName, Thread.CurrentThread.Name);
                Assert.NotEqual(ThreadName, Process.GetCurrentProcess().ProcessName);
            }).Dispose();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void PriorityTest()
        {
            var e = new ManualResetEvent(false);
            Action waitForThread;
            var t = ThreadTestHelpers.CreateGuardedThread(out waitForThread, e.CheckedWait);
            t.IsBackground = true;
            Assert.Equal(ThreadPriority.Normal, t.Priority);
            t.Priority = ThreadPriority.AboveNormal;
            Assert.Equal(ThreadPriority.AboveNormal, t.Priority);
            t.Start();
            Assert.Equal(ThreadPriority.AboveNormal, t.Priority);
            t.Priority = ThreadPriority.Normal;
            Assert.Equal(ThreadPriority.Normal, t.Priority);
            e.Set();
            waitForThread();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void ThreadStateTest()
        {
            var e0 = new ManualResetEvent(false);
            var e1 = new AutoResetEvent(false);
            Action waitForThread;
            var t =
                ThreadTestHelpers.CreateGuardedThread(out waitForThread, () =>
                {
                    e0.CheckedWait();
                    ThreadTestHelpers.WaitForConditionWithoutBlocking(() => e1.WaitOne(0));
                });
            Assert.Equal(ThreadState.Unstarted, t.ThreadState);
            t.IsBackground = true;
            Assert.Equal(ThreadState.Unstarted | ThreadState.Background, t.ThreadState);

            t.Start();
            ThreadTestHelpers.WaitForCondition(() => t.ThreadState == (ThreadState.WaitSleepJoin | ThreadState.Background));

            e0.Set();
            ThreadTestHelpers.WaitForCondition(() => t.ThreadState == (ThreadState.Running | ThreadState.Background));

            e1.Set();
            waitForThread();
            Assert.Equal(ThreadState.Stopped, t.ThreadState);

            t = ThreadTestHelpers.CreateGuardedThread(
                    out waitForThread,
                    () => ThreadTestHelpers.WaitForConditionWithoutBlocking(() => e1.WaitOne(0)));
            t.Start();
            ThreadTestHelpers.WaitForCondition(() => t.ThreadState == ThreadState.Running);

            e1.Set();
            waitForThread();
            Assert.Equal(ThreadState.Stopped, t.ThreadState);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void AbortSuspendTest()
        {
            var e = new ManualResetEvent(false);
            Action waitForThread;
            var t = ThreadTestHelpers.CreateGuardedThread(out waitForThread, e.CheckedWait);
            t.IsBackground = true;

            Action verify = () =>
            {
#pragma warning disable SYSLIB0006, 618 // Obsolete: Abort, Suspend, Resume
                Assert.Throws<PlatformNotSupportedException>(() => t.Abort());
                Assert.Throws<PlatformNotSupportedException>(() => t.Abort(t));
                Assert.Throws<PlatformNotSupportedException>(() => t.Suspend());
                Assert.Throws<PlatformNotSupportedException>(() => t.Resume());
#pragma warning restore SYSLIB0006, 618 // Obsolete: Abort, Suspend, Resume
            };
            verify();

            t.Start();
            verify();

            e.Set();
            waitForThread();

            Assert.Throws<PlatformNotSupportedException>(() => Thread.ResetAbort());
        }

        private static void VerifyLocalDataSlot(LocalDataStoreSlot slot)
        {
            Assert.NotNull(slot);

            var waitForThreadArray = new Action[2];
            var threadArray = new Thread[2];
            var barrier = new Barrier(threadArray.Length);
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            Func<bool> barrierSignalAndWait =
                () =>
                {
                    try
                    {
                        Assert.True(barrier.SignalAndWait(UnexpectedTimeoutMilliseconds, cancellationToken));
                    }
                    catch (OperationCanceledException)
                    {
                        return false;
                    }
                    return true;
                };

            Action<int> threadMain =
                threadIndex =>
                {
                    try
                    {
                        Assert.Null(Thread.GetData(slot));
                        if (!barrierSignalAndWait())
                        {
                            return;
                        }

                        if (threadIndex == 0)
                        {
                            Thread.SetData(slot, threadIndex);
                        }
                        if (!barrierSignalAndWait())
                        {
                            return;
                        }

                        if (threadIndex == 0)
                        {
                            Assert.Equal(threadIndex, Thread.GetData(slot));
                        }
                        else
                        {
                            Assert.Null(Thread.GetData(slot));
                        }
                        if (!barrierSignalAndWait())
                        {
                            return;
                        }

                        if (threadIndex != 0)
                        {
                            Thread.SetData(slot, threadIndex);
                        }
                        if (!barrierSignalAndWait())
                        {
                            return;
                        }

                        Assert.Equal(threadIndex, Thread.GetData(slot));
                        if (!barrierSignalAndWait())
                        {
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        cancellationTokenSource.Cancel();
                        throw new TargetInvocationException(ex);
                    }
                };

            for (int i = 0; i < threadArray.Length; ++i)
            {
                threadArray[i] = ThreadTestHelpers.CreateGuardedThread(out waitForThreadArray[i], () => threadMain(i));
                threadArray[i].IsBackground = true;
                threadArray[i].Start();
            }

            foreach (var waitForThread in waitForThreadArray)
            {
                waitForThread();
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void LocalDataSlotTest()
        {
            var slot = Thread.AllocateDataSlot();
            var slot2 = Thread.AllocateDataSlot();
            Assert.NotEqual(slot, slot2);
            VerifyLocalDataSlot(slot);
            VerifyLocalDataSlot(slot2);

            var slotName = "System.Threading.Threads.Tests.LocalDataSlotTest";
            var slotName2 = slotName + ".2";
            var invalidSlotName = slotName + ".Invalid";

            try
            {
                // AllocateNamedDataSlot allocates
                slot = Thread.AllocateNamedDataSlot(slotName);
                AssertExtensions.Throws<ArgumentException>(null, () => Thread.AllocateNamedDataSlot(slotName));
                slot2 = Thread.AllocateNamedDataSlot(slotName2);
                Assert.NotEqual(slot, slot2);
                VerifyLocalDataSlot(slot);
                VerifyLocalDataSlot(slot2);

                // Free the same slot twice, should be fine
                Thread.FreeNamedDataSlot(slotName2);
                Thread.FreeNamedDataSlot(slotName2);
            }
            catch (Exception ex)
            {
                Thread.FreeNamedDataSlot(slotName);
                Thread.FreeNamedDataSlot(slotName2);
                throw new TargetInvocationException(ex);
            }

            try
            {
                // GetNamedDataSlot gets or allocates
                var tempSlot = Thread.GetNamedDataSlot(slotName);
                Assert.Equal(slot, tempSlot);
                tempSlot = Thread.GetNamedDataSlot(slotName2);
                Assert.NotEqual(slot2, tempSlot);
                slot2 = tempSlot;
                Assert.NotEqual(slot, slot2);
                VerifyLocalDataSlot(slot);
                VerifyLocalDataSlot(slot2);
            }
            finally
            {
                Thread.FreeNamedDataSlot(slotName);
                Thread.FreeNamedDataSlot(slotName2);
            }

            try
            {
                // A named slot can be used after the name is freed, since only the name is freed, not the slot
                slot = Thread.AllocateNamedDataSlot(slotName);
                Thread.FreeNamedDataSlot(slotName);
                slot2 = Thread.AllocateNamedDataSlot(slotName); // same name
                Thread.FreeNamedDataSlot(slotName);
                Assert.NotEqual(slot, slot2);
                VerifyLocalDataSlot(slot);
                VerifyLocalDataSlot(slot2);
            }
            finally
            {
                Thread.FreeNamedDataSlot(slotName);
                Thread.FreeNamedDataSlot(slotName2);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void InterruptTest()
        {
            // Interrupting a thread that is not blocked does not do anything, but once the thread starts blocking, it gets
            // interrupted and does not auto-reset the signaled event
            var threadReady = new AutoResetEvent(false);
            var continueThread = new AutoResetEvent(false);
            bool continueThreadBool = false;
            Action waitForThread;
            var t =
                ThreadTestHelpers.CreateGuardedThread(out waitForThread, () =>
                {
                    threadReady.Set();
                    ThreadTestHelpers.WaitForConditionWithoutBlocking(() => Volatile.Read(ref continueThreadBool));
                    threadReady.Set();
                    Assert.Throws<ThreadInterruptedException>(() => continueThread.CheckedWait());
                });
            t.IsBackground = true;
            t.Start();
            threadReady.CheckedWait();
            continueThread.Set();
            t.Interrupt();
            Assert.False(threadReady.WaitOne(ExpectedTimeoutMilliseconds));
            Volatile.Write(ref continueThreadBool, true);
            waitForThread();
            Assert.True(continueThread.WaitOne(0));

            // Interrupting a dead thread does nothing
            t.Interrupt();

            // Interrupting an unstarted thread causes the thread to be interrupted after it is started and starts blocking
            t = ThreadTestHelpers.CreateGuardedThread(out waitForThread, () =>
                    Assert.Throws<ThreadInterruptedException>(() => continueThread.CheckedWait()));
            t.IsBackground = true;
            t.Interrupt();
            t.Start();
            waitForThread();

            // A thread that is already blocked on a synchronization primitive unblocks immediately
            continueThread.Reset();
            t = ThreadTestHelpers.CreateGuardedThread(out waitForThread, () =>
                    Assert.Throws<ThreadInterruptedException>(() => continueThread.CheckedWait()));
            t.IsBackground = true;
            t.Start();
            ThreadTestHelpers.WaitForCondition(() => (t.ThreadState & ThreadState.WaitSleepJoin) != 0);
            t.Interrupt();
            waitForThread();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void InterruptInFinallyBlockTest_SkipOnDesktopFramework()
        {
            // A wait in a finally block can be interrupted. The .NET Framework applies the same rules as thread abort, and
            // does not allow thread interrupt in a finally block. There is nothing special about thread interrupt that requires
            // not allowing it in finally blocks, so this behavior has changed in .NET Core.
            var continueThread = new AutoResetEvent(false);
            Action waitForThread;
            Thread t =
                ThreadTestHelpers.CreateGuardedThread(out waitForThread, () =>
                {
                    try
                    {
                    }
                    finally
                    {
                        Assert.Throws<ThreadInterruptedException>(() => continueThread.CheckedWait());
                    }
                });
            t.IsBackground = true;
            t.Start();
            t.Interrupt();
            waitForThread();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void JoinTest()
        {
            var threadReady = new ManualResetEvent(false);
            var continueThread = new ManualResetEvent(false);
            Action waitForThread;
            var t =
                ThreadTestHelpers.CreateGuardedThread(out waitForThread, () =>
                {
                    threadReady.Set();
                    continueThread.CheckedWait();
                    Thread.Sleep(ExpectedTimeoutMilliseconds);
                });
            t.IsBackground = true;

            Assert.Throws<ArgumentOutOfRangeException>(() => t.Join(-2));
            Assert.Throws<ArgumentOutOfRangeException>(() => t.Join(TimeSpan.FromMilliseconds(-2)));
            Assert.Throws<ArgumentOutOfRangeException>(() => t.Join(TimeSpan.FromMilliseconds((double)int.MaxValue + 1)));

            Assert.Throws<ThreadStateException>(() => t.Join());
            Assert.Throws<ThreadStateException>(() => t.Join(UnexpectedTimeoutMilliseconds));
            Assert.Throws<ThreadStateException>(() => t.Join(TimeSpan.FromMilliseconds(UnexpectedTimeoutMilliseconds)));

            t.Start();
            threadReady.CheckedWait();
            Assert.False(t.Join(ExpectedTimeoutMilliseconds));
            Assert.False(t.Join(TimeSpan.FromMilliseconds(ExpectedTimeoutMilliseconds)));
            continueThread.Set();
            waitForThread();

            Assert.True(t.Join(0));
            Assert.True(t.Join(TimeSpan.Zero));
        }

        [Fact]
        public static void SleepTest()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Thread.Sleep(-2));
            Assert.Throws<ArgumentOutOfRangeException>(() => Thread.Sleep(TimeSpan.FromMilliseconds(-2)));
            Assert.Throws<ArgumentOutOfRangeException>(() => Thread.Sleep(TimeSpan.FromMilliseconds((double)int.MaxValue + 1)));

            Thread.Sleep(0);

            var stopwatch = Stopwatch.StartNew();
            Thread.Sleep(500);
            stopwatch.Stop();
            Assert.InRange((int)stopwatch.ElapsedMilliseconds, 100, int.MaxValue);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void StartTest()
        {
            var e = new AutoResetEvent(false);
            Action waitForThread;
            Thread t = null;
            t = ThreadTestHelpers.CreateGuardedThread(out waitForThread, () =>
                {
                    e.CheckedWait();
                    Assert.Same(t, Thread.CurrentThread);
                });
            t.IsBackground = true;
            Assert.Throws<InvalidOperationException>(() => t.Start(null));
            Assert.Throws<InvalidOperationException>(() => t.Start(t));
            t.Start();
            Assert.Throws<ThreadStateException>(() => t.Start());
            e.Set();
            waitForThread();
            Assert.Throws<ThreadStateException>(() => t.Start());

            t = ThreadTestHelpers.CreateGuardedThread(out waitForThread, parameter => e.CheckedWait());
            t.IsBackground = true;
            t.Start();
            Assert.Throws<ThreadStateException>(() => t.Start());
            Assert.Throws<ThreadStateException>(() => t.Start(null));
            Assert.Throws<ThreadStateException>(() => t.Start(t));
            e.Set();
            waitForThread();
            Assert.Throws<ThreadStateException>(() => t.Start());
            Assert.Throws<ThreadStateException>(() => t.Start(null));
            Assert.Throws<ThreadStateException>(() => t.Start(t));

            t = ThreadTestHelpers.CreateGuardedThread(out waitForThread, parameter =>
                {
                    Assert.Null(parameter);
                    Assert.Same(t, Thread.CurrentThread);
                });
            t.IsBackground = true;
            t.Start();
            waitForThread();

            t = ThreadTestHelpers.CreateGuardedThread(out waitForThread, parameter =>
                {
                    Assert.Null(parameter);
                    Assert.Same(t, Thread.CurrentThread);
                });
            t.IsBackground = true;
            t.Start(null);
            waitForThread();

            t = ThreadTestHelpers.CreateGuardedThread(out waitForThread, parameter =>
                {
                    Assert.Same(t, parameter);
                    Assert.Same(t, Thread.CurrentThread);
                });
            t.IsBackground = true;
            t.Start(t);
            waitForThread();
        }

        [Fact]
        public static void MiscellaneousTest()
        {
            Thread.BeginCriticalRegion();
            Thread.EndCriticalRegion();
            Thread.BeginThreadAffinity();
            Thread.EndThreadAffinity();

#pragma warning disable 618 // obsolete members
            Assert.Throws<InvalidOperationException>(() => Thread.CurrentThread.GetCompressedStack());
            Assert.Throws<InvalidOperationException>(() => Thread.CurrentThread.SetCompressedStack(CompressedStack.Capture()));
#pragma warning restore 618 // obsolete members

            Thread.MemoryBarrier();

            var ad = Thread.GetDomain();
            Assert.NotNull(ad);
            Assert.Equal(AppDomain.CurrentDomain, ad);
            Assert.Equal(ad.Id, Thread.GetDomainID());

            Thread.SpinWait(int.MinValue);
            Thread.SpinWait(-1);
            Thread.SpinWait(0);
            Thread.SpinWait(1);
            Thread.Yield();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void WindowsPrincipalPolicyTest_Windows()
        {
            RemoteExecutor.Invoke(() =>
            {
                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
                Assert.Equal(Environment.UserDomainName + @"\" + Environment.UserName, Thread.CurrentPrincipal.Identity.Name);
            }).Dispose();
        }


        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34543", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void WindowsPrincipalPolicyTest_Windows_NewThreads()
        {
            RemoteExecutor.Invoke(() =>
            {
                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

                IPrincipal currentPrincipal = Thread.CurrentPrincipal;

                Assert.NotNull(currentPrincipal);
                Assert.True(currentPrincipal.Identity.IsAuthenticated);

                var first = new Thread(CheckPrincipal);
                first.Start(currentPrincipal);
                first.Join();

                var second = new Thread(CheckPrincipal);
                second.Start(currentPrincipal);
                second.Join();
            }).Dispose();

            static void CheckPrincipal(object principal)
            {
                Assert.True(Thread.CurrentPrincipal.Identity.IsAuthenticated);
                Assert.NotNull(Thread.CurrentPrincipal);
                Assert.Equal((IPrincipal)principal, Thread.CurrentPrincipal);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void NoPrincipalPolicyTest_NewThreads()
        {
            RemoteExecutor.Invoke(() =>
            {
                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.NoPrincipal);

                Assert.Null(Thread.CurrentPrincipal);

                var first = new Thread(() => Assert.Null(Thread.CurrentPrincipal));
                first.Start();
                first.Join();

                var second = new Thread(() => Assert.Null(Thread.CurrentPrincipal));
                second.Start();
                second.Join();
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34543", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void NoPrincipalToWindowsPrincipalPolicyTest_Windows_NewThreads()
        {
            RemoteExecutor.Invoke(() =>
            {
                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.NoPrincipal);

                Assert.Null(Thread.CurrentPrincipal);

                var first = new Thread(() => Assert.Null(Thread.CurrentPrincipal));
                first.Start();
                first.Join();

                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

                var second = new Thread(CheckPrincipal);
                second.Start(Thread.CurrentPrincipal);
                second.Join();
            }).Dispose();

            static void CheckPrincipal(object principal)
            {
                Assert.True(Thread.CurrentPrincipal.Identity.IsAuthenticated);
                Assert.NotNull(Thread.CurrentPrincipal);
                Assert.Equal((IPrincipal)principal, Thread.CurrentPrincipal);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void WindowsPrincipalPolicyTest_Unix()
        {
            RemoteExecutor.Invoke(() =>
            {
                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
                Assert.Throws<PlatformNotSupportedException>(() => Thread.CurrentPrincipal);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void UnauthenticatedPrincipalTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.UnauthenticatedPrincipal);
                Assert.Equal(string.Empty, Thread.CurrentPrincipal.Identity.Name);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void DefaultPrincipalPolicyTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                Assert.Null(Thread.CurrentPrincipal);
            }).Dispose();
        }

        [Fact]
        public static void GetCurrentProcessorId()
        {
            Assert.True(Thread.GetCurrentProcessorId() >= 0);
        }
    }
}
