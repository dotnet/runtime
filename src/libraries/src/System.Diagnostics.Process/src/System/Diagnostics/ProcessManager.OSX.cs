// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
        /// <summary>Gets the IDs of all processes on the current machine.</summary>
        public static int[] GetProcessIds()
        {
            // TODO: Implement this
            throw NotImplemented.ByDesign;
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private static ProcessInfo CreateProcessInfo(int pid)
        {
            ProcessInfo dummy = new ProcessInfo() // assign to fields to suppress warnings until implemented
            {
                _basePriority = 0,
                _pageFileBytes = 0,
                _handleCount = 0,
                _pageFileBytesPeak = 0,
                _poolNonpagedBytes = 0,
                _poolPagedBytes = 0,
                _privateBytes = 0,
                _processId = pid,
                _processName = null,
                _sessionId = 0,
                _virtualBytes = 0,
                _virtualBytesPeak = 0,
                _workingSet = 0,
                _workingSetPeak = 0
            };
            dummy._threadInfoList.Add(new ThreadInfo()
            {
                _basePriority = 0,
                _currentPriority = 0,
                _processId = pid,
                _startAddress = IntPtr.Zero,
                _threadId = 0,
                _threadState = 0,
                _threadWaitReason = ThreadWaitReason.Unknown
            });

            // TODO: Implement this
            throw NotImplemented.ByDesign;
        }

        // ----------------------------------
        // ---- Unix PAL layer ends here ----
        // ----------------------------------
    }
}
