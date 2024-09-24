// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    public partial class Process
    {
        private readonly struct ProcessSnapshot : IDisposable
        {
            static readonly IntPtr CurrentProcessHandle = Interop.Kernel32.GetCurrentProcess();
            
            public readonly Interop.Kernel32.HPSS Handle;

            public ProcessSnapshot(SafeProcessHandle process, Interop.Kernel32.PSS_CAPTURE_FLAGS captureFlags, int threadContextFlags = 0)
            {
                ThrowIfFailure(Interop.Kernel32.PssCaptureSnapshot(process, captureFlags, threadContextFlags, out var handle));
                Handle = handle;
            }
            public void Dispose()
            {
                if (Handle.IsValid)
                {
                    ThrowIfFailure(Interop.Kernel32.PssFreeSnapshot(CurrentProcessHandle, Handle));
                }
            }
        }
        private readonly struct ProcessSnapshotWalkMarker : IDisposable
        {
            public readonly Interop.Kernel32.HPSSWALK Handle;

            public ProcessSnapshotWalkMarker()
            {
                ThrowIfFailure(Interop.Kernel32.PssWalkMarkerCreate(IntPtr.Zero, out var hWalk));
                Handle = hWalk;
            }
            public void Dispose()
            {
                if (Handle.IsValid)
                {
                    ThrowIfFailure(Interop.Kernel32.PssWalkMarkerFree(Handle));
                }
            }
        }
        private static void ThrowIfFailure(int errorCode)
        {
            if (errorCode != Interop.Errors.ERROR_SUCCESS)
            {
                ThrowHelper_Win32Exception(errorCode);
            }
        }

        [DoesNotReturn]
        private static void ThrowHelper_Win32Exception(int errorCode)
        {
            throw new Win32Exception(errorCode);
        }

        private unsafe ProcessThreadCollection? TryEnumerateThreadsBySnapshot()
        {
            try
            {
                using SafeProcessHandle hProcess = GetProcessHandle(Interop.Advapi32.ProcessOptions.PROCESS_QUERY_INFORMATION, true);
                using var hSnapshot = new ProcessSnapshot(hProcess, Interop.Kernel32.PSS_CAPTURE_FLAGS.PSS_CAPTURE_THREADS);

                // get length of thread array. can be omitted if we are inserting into a List<>
                Interop.Kernel32.PSS_THREAD_INFORMATION info;
                ThrowIfFailure(Interop.Kernel32.PssQuerySnapshot(hSnapshot.Handle, Interop.Kernel32.PSS_QUERY_INFORMATION_CLASS.PSS_QUERY_THREAD_INFORMATION,
                    &info, sizeof(Interop.Kernel32.PSS_THREAD_INFORMATION)));

                var processThreads = new ProcessThread[info.ThreadsCaptured];
                int index = 0;

                using var maker = new ProcessSnapshotWalkMarker();
                Interop.Kernel32.PSS_THREAD_ENTRY thread;
                while (Interop.Kernel32.PssWalkSnapshot(hSnapshot.Handle, Interop.Kernel32.PSS_WALK_INFORMATION_CLASS.PSS_WALK_THREADS,
                    maker.Handle, &thread, sizeof(Interop.Kernel32.PSS_THREAD_ENTRY)) == Interop.Errors.ERROR_SUCCESS)
                {
                    var threadInfo = new ThreadInfo
                    {
                        _basePriority = thread.BasePriority,
                        _currentPriority = thread.Priority,
                        _processId = thread.ProcessId,
                        _startAddress = thread.Win32StartAddress.ToPointer(),
                        _threadId = thread.ThreadId
                    };

                    processThreads[index] = new ProcessThread(false, _processId, threadInfo)
                    {
                        StateRequiresLazyEvaluation = true
                    };

                    ++index;
                }

                return new ProcessThreadCollection(processThreads);
            }
            catch
#if DEBUG
            (Exception ex)
#endif
            {
#if DEBUG
                Debug.Assert(false, $"Throws during enumerating threads by process snapshot: {ex}");
#endif
                // We did something wrong. Revert to the original method for compatibility.
                return null;
            }
        }

        private ProcessThreadCollection EnumerateThreadsCore()
        {
            // Although Pss APIs are supported from 8.1, the API to effortlessly retrieve ThreadState and WaitReason
            // is unavailable until 10. To save some effort, we raise the API version here to 10.
            // See EnsureStateAndWaitReason() in ProcessThread.Windows.cs for more information.

            return (!_isRemoteMachine && OperatingSystem.IsWindowsVersionAtLeast(10, 0) && TryEnumerateThreadsBySnapshot() is { } collection) ?
                collection :
                EnumerateThreadsCoreFallback();
        }
    }
}
