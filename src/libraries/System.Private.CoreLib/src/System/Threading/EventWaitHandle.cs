// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;

namespace System.Threading
{
    public partial class EventWaitHandle : WaitHandle
    {
        public EventWaitHandle(bool initialState, EventResetMode mode) :
            this(initialState, mode, null, out _)
        {
        }

        public EventWaitHandle(bool initialState, EventResetMode mode, string? name) :
            this(initialState, mode, name, out _)
        {
        }

        public EventWaitHandle(bool initialState, EventResetMode mode, string? name, out bool createdNew)
        {
            if (mode != EventResetMode.AutoReset && mode != EventResetMode.ManualReset)
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(mode));

            CreateEventCore(initialState, mode, name, out createdNew);
        }

        [SupportedOSPlatform("windows")]
        public static EventWaitHandle OpenExisting(string name)
        {
            switch (OpenExistingWorker(name, out EventWaitHandle? result))
            {
                case OpenExistingResult.NameNotFound:
                    throw new WaitHandleCannotBeOpenedException();
                case OpenExistingResult.NameInvalid:
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));
                case OpenExistingResult.PathNotFound:
                    throw new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, name));
                default:
                    Debug.Assert(result != null, "result should be non-null on success");
                    return result;
            }
        }

        [SupportedOSPlatform("windows")]
        public static bool TryOpenExisting(string name, [NotNullWhen(true)] out EventWaitHandle? result) =>
            OpenExistingWorker(name, out result!) == OpenExistingResult.Success;
    }
}
