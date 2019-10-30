// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
    internal partial class TimerQueue
    {
        #region interface to native per-AppDomain timer

        // We use a SafeHandle to ensure that the native timer is destroyed when the AppDomain is unloaded.
        private sealed class AppDomainTimerSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public AppDomainTimerSafeHandle()
                : base(true)
            {
            }

            protected override bool ReleaseHandle()
            {
                return DeleteAppDomainTimer(handle);
            }
        }

        private readonly int _id; // TimerQueues[_id] == this

        private AppDomainTimerSafeHandle? m_appDomainTimer;

        private TimerQueue(int id)
        {
            _id = id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool SetTimer(uint actualDuration)
        {
            if (m_appDomainTimer == null || m_appDomainTimer.IsInvalid)
            {
                Debug.Assert(!_isTimerScheduled);
                Debug.Assert(_id >= 0 && _id < Instances.Length && this == Instances[_id]);

                m_appDomainTimer = CreateAppDomainTimer(actualDuration, _id);
                return !m_appDomainTimer.IsInvalid;
            }
            else
            {
                return ChangeAppDomainTimer(m_appDomainTimer, actualDuration);
            }
        }

        // The VM calls this when a native timer fires.
        internal static void AppDomainTimerCallback(int id)
        {
            Debug.Assert(id >= 0 && id < Instances.Length && Instances[id]._id == id);
            Instances[id].FireNextTimers();
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern AppDomainTimerSafeHandle CreateAppDomainTimer(uint dueTime, int id);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern bool ChangeAppDomainTimer(AppDomainTimerSafeHandle handle, uint dueTime);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern bool DeleteAppDomainTimer(IntPtr handle);

        #endregion
    }
}
