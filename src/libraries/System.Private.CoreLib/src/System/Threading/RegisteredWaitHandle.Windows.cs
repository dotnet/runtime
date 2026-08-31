// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    public sealed partial class RegisteredWaitHandle : MarshalByRefObject
    {
        private SafeWaitHandle? _waitHandle;
        private readonly _ThreadPoolWaitOrTimerCallback? _callbackHelper;
        private readonly uint _millisecondsTimeout;
        private readonly int _signedMillisecondsTimeout;
        private bool _repeating;

        /// <summary>
        /// The callback to execute when the wait on <see cref="Handle"/> either times out or completes.
        /// </summary>
        internal _ThreadPoolWaitOrTimerCallback? Callback
        {
            get => _callbackHelper;
        }

        /// <summary>
        /// The <see cref="SafeWaitHandle"/> that was registered.
        /// </summary>
        internal SafeWaitHandle Handle
        {
            get
            {
                Debug.Assert(_waitHandle != null);
                return _waitHandle;
            }
        }

        /// <summary>
        /// The time this handle times out at in ms.
        /// </summary>
        internal int TimeoutTimeMs { get; private set; }

        internal int TimeoutDurationMs
        {
            get => _signedMillisecondsTimeout;
        }

        internal bool IsInfiniteTimeout => TimeoutDurationMs == -1;

        /// <summary>
        /// Whether or not the wait is a repeating wait.
        /// </summary>
        internal bool Repeating
        {
            get => _repeating;
        }

        public bool Unregister(WaitHandle waitObject) =>
            ThreadPool.UseWindowsThreadPool ?
            UnregisterWindowsThreadPool(waitObject) :
            UnregisterPortableCore(waitObject);

        /// <summary>
        /// Perform the registered callback if the <see cref="UserUnregisterWaitHandle"/> has not been signaled.
        /// </summary>
        /// <param name="timedOut">Whether or not the wait timed out.</param>
        internal void PerformCallback(bool timedOut)
        {
            if (ThreadPool.UseWindowsThreadPool)
            {
                PerformCallbackWindowsThreadPool(timedOut);
            }
            else
            {
                PerformCallbackPortableCore(timedOut);
            }
        }
    }
}
