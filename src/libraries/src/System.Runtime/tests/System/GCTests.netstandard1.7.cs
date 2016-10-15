// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Runtime;
using System.Threading.Tasks;
using Xunit;

namespace System.Tests
{
    public class GCExtendedTests : RemoteExecutorTestBase
    {
        private const int TimeoutMilliseconds = 10 * 30 * 1000; //if full GC is triggered it may take a while

        [Fact]
        [OuterLoop]
        public static void GetGeneration_WeakReference()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.TimeOut = TimeoutMilliseconds;
            RemoteInvoke(() =>
                {

                    Func<WeakReference> getweakref = delegate ()
                    {
                        Version myobj = new Version();
                        var wkref = new WeakReference(myobj);

                        Assert.True(GC.TryStartNoGCRegion(1024));
                        Assert.True(GC.GetGeneration(wkref) >= 0);
                        Assert.Equal(GC.GetGeneration(wkref), GC.GetGeneration(myobj));
                        GC.EndNoGCRegion();

                        myobj = null;
                        return wkref;
                    };

                    WeakReference weakref = getweakref();
                    Assert.True(weakref != null);
#if !DEBUG
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
                    Assert.Throws<ArgumentNullException>(() => GC.GetGeneration(weakref));
#endif
                    return SuccessExitCode;
                }, options).Dispose();

        }

        [Fact]
        public static void GCNotificationNegTests()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GC.RegisterForFullGCNotification(-1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => GC.RegisterForFullGCNotification(100, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => GC.RegisterForFullGCNotification(-1, 100));

            Assert.Throws<ArgumentOutOfRangeException>(() => GC.RegisterForFullGCNotification(10, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => GC.RegisterForFullGCNotification(-1, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => GC.RegisterForFullGCNotification(100, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => GC.RegisterForFullGCNotification(10, 100));


            Assert.Throws<ArgumentOutOfRangeException>(() => GC.WaitForFullGCApproach(-2));
            Assert.Throws<ArgumentOutOfRangeException>(() => GC.WaitForFullGCComplete(-2));
        }

        [Fact]
        [OuterLoop]
        public static void GCNotifiicationTests()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.TimeOut = TimeoutMilliseconds;
            RemoteInvoke(() =>
                {
                    Assert.True(TestWait(true, -1));
                    Assert.True(TestWait(false, -1));
                    Assert.True(TestWait(true, 0));
                    Assert.True(TestWait(false, 0));
                    Assert.True(TestWait(true, 100));
                    Assert.True(TestWait(false, 100));
                    Assert.True(TestWait(true, int.MaxValue));
                    Assert.True(TestWait(false, int.MaxValue));
                    return SuccessExitCode;
                }, options).Dispose();
        }
        [Fact]
        [OuterLoop]
        public static void TryStartNoGCRegionNegTest()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.TimeOut = TimeoutMilliseconds;
            RemoteInvoke(() =>
                {
                    Assert.Throws<InvalidOperationException>(() => GC.EndNoGCRegion());

                    Assert.True(GC.TryStartNoGCRegion(1024));
                    Assert.Throws<InvalidOperationException>(() => GC.TryStartNoGCRegion(1024));

                    Assert.True(GC.TryStartNoGCRegion(1024, true));
                    Assert.Throws<InvalidOperationException>(() => GC.TryStartNoGCRegion(1024, true));

                    Assert.True(GC.TryStartNoGCRegion(1024, 1024));
                    Assert.Throws<InvalidOperationException>(() => GC.TryStartNoGCRegion(1024, 1024));

                    Assert.True(GC.TryStartNoGCRegion(1024, 1024, true));
                    Assert.Throws<InvalidOperationException>(() => GC.TryStartNoGCRegion(1024, 1024, true));
                    Assert.True(GC.TryStartNoGCRegion(1024, true));
                    Assert.Equal(GCSettings.LatencyMode, GCLatencyMode.NoGCRegion);
                    Assert.Throws<InvalidOperationException>(() => GCSettings.LatencyMode = GCLatencyMode.LowLatency);

                    GC.EndNoGCRegion();

                    return SuccessExitCode;

                }, options).Dispose();
        }
        [Fact]
        [OuterLoop]
        public static void TryStartNoGCRegionPosTest()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.TimeOut = TimeoutMilliseconds;
            RemoteInvoke(() =>
                {

                    Assert.True(GC.TryStartNoGCRegion(1024));
                    Assert.Equal(GCSettings.LatencyMode, GCLatencyMode.NoGCRegion);
                    GC.EndNoGCRegion();

                    Assert.True(GC.TryStartNoGCRegion(1024, true));
                    Assert.Equal(GCSettings.LatencyMode, GCLatencyMode.NoGCRegion);
                    GC.EndNoGCRegion();

                    Assert.True(GC.TryStartNoGCRegion(1024, 1024));
                    Assert.Equal(GCSettings.LatencyMode, GCLatencyMode.NoGCRegion);
                    GC.EndNoGCRegion();

                    Assert.True(GC.TryStartNoGCRegion(1024, 1024, true));
                    Assert.Equal(GCSettings.LatencyMode, GCLatencyMode.NoGCRegion);
                    GC.EndNoGCRegion();

                    return SuccessExitCode;

                }, options).Dispose();
        }

        public static bool TestWait(bool approach, int timeout)
        {
            GCNotificationStatus result = GCNotificationStatus.Failed;
            Thread cancelProc = null;

            // Since we need to test an infinite (or very large) wait but the API won't return, spawn off a thread which
            // will cancel the wait after a few seconds
            //
            bool cancelTimeout = (timeout == -1) || (timeout > 10000);

            GC.RegisterForFullGCNotification(20, 20);

            try
            {
                if (cancelTimeout)
                {
                    cancelProc = new Thread(new ThreadStart(CancelProc));
                    cancelProc.Start();
                }

                if (approach)
                    result = GC.WaitForFullGCApproach(timeout);
                else
                    result = GC.WaitForFullGCComplete(timeout);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error - Unexpected exception received: {0}", e.ToString());
                return false;
            }
            finally
            {
                if (cancelProc != null)
                    cancelProc.Join();
            }

            if (cancelTimeout)
            {
                if (result != GCNotificationStatus.Canceled)
                {
                    Console.WriteLine("Error - WaitForFullGCApproach result not Cancelled");
                    return false;
                }
            }
            else
            {
                if (result != GCNotificationStatus.Timeout)
                {
                    Console.WriteLine("Error - WaitForFullGCApproach result not Timeout");
                    return false;
                }
            }

            return true;
        }

        public static void CancelProc()
        {
            Thread.Sleep(500);
            GC.CancelFullGCNotification();
        }
    }
}
