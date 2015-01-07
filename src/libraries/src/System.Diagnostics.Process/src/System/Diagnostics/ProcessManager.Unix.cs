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
                basePriority = 0,
                processId = 0,
                handleCount = 0,
                poolPagedBytes = 0,
                poolNonpagedBytes = 0,
                virtualBytes = 0,
                virtualBytesPeak = 0,
                workingSetPeak = 0,
                workingSet = 0,
                pageFileBytesPeak = 0,
                pageFileBytes = 0,
                privateBytes = 0,
                sessionId = 0,
                processName = null
            };
            pi.threadInfoList.Add(new ThreadInfo // dummy code using fields just to suppress warnings for now
            {
                threadId = 0,
                processId = 0,
                basePriority = 0,
                currentPriority = 0,
                startAddress = IntPtr.Zero,
                threadState = default(ThreadState),
                threadWaitReason = default(ThreadWaitReason)
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
            mi.baseName = null;
            mi.fileName = null;
            mi.baseOfDll = IntPtr.Zero;
            mi.entryPoint = IntPtr.Zero;
            mi.sizeOfImage = 0;

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