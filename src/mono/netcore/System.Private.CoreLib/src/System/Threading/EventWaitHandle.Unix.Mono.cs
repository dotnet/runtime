// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    public partial class EventWaitHandle
    {
        public bool Set()
        {
            SafeWaitHandle handle = ValidateHandle(out bool release);

            try
            {
                return SetEventInternal(handle.DangerousGetHandle());
            }
            finally
            {
                if (release)
                    handle.DangerousRelease();
            }
        }

        public bool Reset()
        {
            SafeWaitHandle handle = ValidateHandle(out bool release);

            try
            {
                return ResetEventInternal(handle.DangerousGetHandle());
            }
            finally
            {
                if (release)
                    handle.DangerousRelease();
            }
        }

        private unsafe void CreateEventCore(bool initialState, EventResetMode mode, string? name, out bool createdNew)
        {
            if (name != null)
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_NamedSynchronizationPrimitives);

            SafeWaitHandle handle = new SafeWaitHandle(CreateEventInternal(mode == EventResetMode.ManualReset, initialState, null, 0, out int errorCode), ownsHandle: true);
            if (errorCode != 0)
                throw new NotImplementedException("errorCode");
            SafeWaitHandle = handle;

            createdNew = true;
        }

        private static OpenExistingResult OpenExistingWorker(string name, out EventWaitHandle result)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_NamedSynchronizationPrimitives);
        }

        internal static bool Set(SafeWaitHandle waitHandle)
        {
            bool release = false;
            try
            {
                waitHandle.DangerousAddRef(ref release);
                return SetEventInternal(waitHandle.DangerousGetHandle());
            }
            finally
            {
                if (release)
                    waitHandle.DangerousRelease();
            }
        }

        private SafeWaitHandle ValidateHandle(out bool success)
        {
            // The field value is modifiable via the public <see cref="WaitHandle.SafeWaitHandle"/> property, save it locally
            // to ensure that one instance is used in all places in this method
            SafeWaitHandle waitHandle = SafeWaitHandle;
            if (waitHandle.IsInvalid)
            {
                throw new InvalidOperationException();
            }

            success = false;
            waitHandle.DangerousAddRef(ref success);
            return waitHandle;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe IntPtr CreateEventInternal(bool manual, bool initialState, char* name, int name_length, out int errorCode);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool ResetEventInternal(IntPtr handle);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool SetEventInternal(IntPtr handle);

    }
}
