// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
        /// <summary>Gets process infos for each process on the specified machine.</summary>
        /// <param name="machineName">The target machine.</param>
        /// <returns>An array of process infos, one per found process.</returns>
        public static ProcessInfo[] GetProcessInfos(string machineName)
        {
            if (IsRemoteMachine(machineName))
            {
                throw new PlatformNotSupportedException();
            }

            var pi = new ProcessInfo // dummy code using fields just to suppress warnings for now
            {
                _basePriority = 0,
                _processId = 0,
                _handleCount = 0,
                _poolPagedBytes = 0,
                _poolNonpagedBytes = 0,
                _virtualBytes = 0,
                _virtualBytesPeak = 0,
                _workingSetPeak = 0,
                _workingSet = 0,
                _pageFileBytesPeak = 0,
                _pageFileBytes = 0,
                _privateBytes = 0,
                _sessionId = 0,
                _processName = null
            };
            pi._threadInfoList.Add(new ThreadInfo // dummy code using fields just to suppress warnings for now
            {
                _threadId = 0,
                _processId = 0,
                _basePriority = 0,
                _currentPriority = 0,
                _startAddress = IntPtr.Zero,
                _threadState = default(ThreadState),
                _threadWaitReason = default(ThreadWaitReason)
            });

            throw NotImplemented.ByDesign; // TODO: Implement this
        }

        /// <summary>Gets the IDs of all processes on the specified machine.</summary>
        /// <param name="machineName">The machine to examine.</param>
        /// <returns>An array of process IDs from the specified machine.</returns>
        public static int[] GetProcessIds(string machineName)
        {
            if (IsRemoteMachine(machineName))
            {
                throw new PlatformNotSupportedException();
            }
            return GetProcessIds();
        }

        /// <summary>Gets the IDs of all processes on the current machine.</summary>
        public static int[] GetProcessIds()
        {
            throw NotImplemented.ByDesign; // TODO: Implement this
        }

        /// <summary>Gets the ID of a process from a handle to the process.</summary>
        /// <param name="processHandle">The handle.</param>
        /// <returns>The process ID.</returns>
        public static int GetProcessIdFromHandle(SafeProcessHandle processHandle)
        {
            throw NotImplemented.ByDesign; // TODO: Implement this
        }

        /// <summary>Gets an array of module infos for the specified process.</summary>
        /// <param name="processId">The ID of the process whose modules should be enumerated.</param>
        /// <returns>The array of modules.</returns>
        public static ModuleInfo[] GetModuleInfos(int processId)
        {
            ModuleInfo mi = new ModuleInfo(); // dummy code using fields just to suppress warnings for now
            mi._baseName = null;
            mi._fileName = null;
            mi._baseOfDll = IntPtr.Zero;
            mi._entryPoint = IntPtr.Zero;
            mi._sizeOfImage = 0;

            throw NotImplemented.ByDesign; // TODO: Implement this
        }

        /// <summary>Gets whether the named machine is remote or local.</summary>
        /// <param name="machineName">The machine name.</param>
        /// <returns>true if the machine is remote; false if it's local.</returns>
        public static bool IsRemoteMachine(string machineName)
        {
            throw NotImplemented.ByDesign; // TODO: Implement this
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

    }
}