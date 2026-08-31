// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes
{
    /// <summary>
    /// Anonymous pipe server stream
    /// </summary>
    public sealed partial class AnonymousPipeServerStream : PipeStream
    {
        internal AnonymousPipeServerStream(PipeDirection direction, HandleInheritability inheritability, int bufferSize, PipeSecurity? pipeSecurity)
            : base(direction, bufferSize)
        {
            if (direction == PipeDirection.InOut)
            {
                throw new NotSupportedException(SR.NotSupported_AnonymousPipeUnidirectional);
            }

            if (inheritability < HandleInheritability.None || inheritability > HandleInheritability.Inheritable)
            {
                throw new ArgumentOutOfRangeException(nameof(inheritability), SR.ArgumentOutOfRange_HandleInheritabilityNoneOrInheritable);
            }

            Create(direction, inheritability, bufferSize, pipeSecurity);
        }

        private void Create(PipeDirection direction, HandleInheritability inheritability, int bufferSize)
        {
            Create(direction, inheritability, bufferSize, null);
        }

        private void Create(PipeDirection direction, HandleInheritability inheritability, int bufferSize, PipeSecurity? pipeSecurity)
        {
            Debug.Assert(direction != PipeDirection.InOut, "Anonymous pipe direction shouldn't be InOut");
            Debug.Assert(bufferSize >= 0, "bufferSize is negative");

            bool bSuccess;
            SafePipeHandle serverHandle, clientHandle;
            SafePipeHandle newServerHandle;

            // Create the two pipe handles that make up the anonymous pipe.
            GCHandle pinningHandle = default;
            try
            {
                Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = PipeStream.GetSecAttrs(inheritability, pipeSecurity, ref pinningHandle);

                if (direction == PipeDirection.In)
                {
                    bSuccess = Interop.Kernel32.CreatePipe(out serverHandle, out clientHandle, ref secAttrs, bufferSize);
                }
                else
                {
                    bSuccess = Interop.Kernel32.CreatePipe(out clientHandle, out serverHandle, ref secAttrs, bufferSize);
                }
            }
            finally
            {
                if (pinningHandle.IsAllocated)
                {
                    pinningHandle.Free();
                }
            }

            if (!bSuccess)
            {
                Exception e = Win32Marshal.GetExceptionForLastWin32Error();

                serverHandle.Dispose();
                clientHandle.Dispose();

                throw e;
            }

            // Duplicate the server handle to make it not inheritable.  Note: We need to do this so that the child
            // process doesn't end up getting another copy of the server handle.  If it were to get a copy, the
            // OS wouldn't be able to inform the child that the server has closed its handle because it will see
            // that there is still one server handle that is open.
            bSuccess = Interop.Kernel32.DuplicateHandle(Interop.Kernel32.GetCurrentProcess(), serverHandle, Interop.Kernel32.GetCurrentProcess(),
                out newServerHandle, 0, false, Interop.Kernel32.HandleOptions.DUPLICATE_SAME_ACCESS);

            if (!bSuccess)
            {
                Exception e = Win32Marshal.GetExceptionForLastWin32Error();

                serverHandle.Dispose();
                newServerHandle.Dispose();
                clientHandle.Dispose();

                throw e;
            }

            // Close the inheritable server handle.
            serverHandle.Dispose();

            _clientHandle = clientHandle;
            InitializeHandle(newServerHandle, false, false);

            State = PipeState.Connected;
        }
    }
}
