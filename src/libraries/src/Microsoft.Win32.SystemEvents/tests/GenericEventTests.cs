// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Xunit;
using static Interop;

namespace Microsoft.Win32.SystemEventsTests
{
    public abstract class GenericEventTests : SystemEventsTest
    {
        protected abstract int MessageId { get; }

        protected abstract event EventHandler Event;

        private void SendMessage()
        {
            SendMessage(MessageId, IntPtr.Zero, IntPtr.Zero);
        }
        private void SendReflectedMessage()
        {
            SendMessage(User32.WM_REFLECT + MessageId, IntPtr.Zero, IntPtr.Zero);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void SignalsEventsAsynchronouslyOnMessage()
        {
            var signal = new AutoResetEvent(false);
            EventHandler signaledHandler = (o, e) => signal.Set();

            Event += signaledHandler;

            try
            {
                SendMessage();
                Assert.True(signal.WaitOne(PostMessageWait));
            }
            finally
            {
                Event -= signaledHandler;
                signal.Dispose();
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void SignalsEventsSynchronouslyOnReflectedMessage()
        {
            bool signal = false;
            EventHandler signaledHandler = (o, e) => signal = true;

            Event += signaledHandler;

            try
            {
                SendReflectedMessage();
                Assert.True(signal);
            }
            finally
            {
                Event -= signaledHandler;
            }
        }
    }
}
