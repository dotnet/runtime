// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Class:  SafeProcessHandle
**
** A wrapper for a process handle
**
**
===========================================================*/

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal static readonly SafeProcessHandle InvalidHandle = new SafeProcessHandle();

        internal SafeFileHandle? _parentInputPipeHandle;
        internal SafeFileHandle? _parentOutputPipeHandle;
        internal SafeFileHandle? _parentErrorPipeHandle;

        private static int s_cachedSerializationSwitch;

        /// <summary>
        /// Creates a <see cref="T:Microsoft.Win32.SafeHandles.SafeHandle" />.
        /// </summary>
        public SafeProcessHandle()
            : this(IntPtr.Zero)
        {
        }

        internal SafeProcessHandle(IntPtr handle)
            : this(handle, true)
        {
        }

        /// <summary>
        /// Creates a <see cref="T:Microsoft.Win32.SafeHandles.SafeHandle" /> around a process handle.
        /// </summary>
        /// <param name="existingHandle">Handle to wrap</param>
        /// <param name="ownsHandle">Whether to control the handle lifetime</param>
        public SafeProcessHandle(IntPtr existingHandle, bool ownsHandle)
            : base(ownsHandle)
        {
            SetHandle(existingHandle);
        }

        /// <summary>
        /// Starts the process specified by the given start information, returning a handle to the process.
        /// </summary>
        /// <param name="startInfo">The <see cref="System.Diagnostics.ProcessStartInfo"/> that contains the information that is used to start the process.</param>
        /// <returns>
        /// A <see cref="SafeProcessHandle"/> that wraps the handle to the started process,
        /// or an invalid handle if the process was not started (for example, when using shell execute and the process was launched but no process handle was returned).
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="startInfo"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">No file name was specified in the <paramref name="startInfo"/> parameter's <see cref="System.Diagnostics.ProcessStartInfo.FileName"/> property.</exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public static SafeProcessHandle Start(System.Diagnostics.ProcessStartInfo startInfo)
        {
            ArgumentNullException.ThrowIfNull(startInfo);

            if (startInfo.FileName.Length == 0)
            {
                throw new InvalidOperationException(SR.FileNameMissing);
            }
            if (startInfo.StandardInputEncoding != null && !startInfo.RedirectStandardInput)
            {
                throw new InvalidOperationException(SR.StandardInputEncodingNotAllowed);
            }
            if (startInfo.StandardOutputEncoding != null && !startInfo.RedirectStandardOutput)
            {
                throw new InvalidOperationException(SR.StandardOutputEncodingNotAllowed);
            }
            if (startInfo.StandardErrorEncoding != null && !startInfo.RedirectStandardError)
            {
                throw new InvalidOperationException(SR.StandardErrorEncodingNotAllowed);
            }
            if (!string.IsNullOrEmpty(startInfo.Arguments) && startInfo.HasArgumentList)
            {
                throw new InvalidOperationException(SR.ArgumentAndArgumentListInitialized);
            }
            if (startInfo.HasArgumentList)
            {
                int argumentCount = startInfo.ArgumentList.Count;
                for (int i = 0; i < argumentCount; i++)
                {
                    if (startInfo.ArgumentList[i] is null)
                    {
                        throw new ArgumentNullException("item", SR.ArgumentListMayNotContainNull);
                    }
                }
            }

            bool anyRedirection = startInfo.RedirectStandardInput || startInfo.RedirectStandardOutput || startInfo.RedirectStandardError;
            bool anyHandle = startInfo.StandardInputHandle is not null || startInfo.StandardOutputHandle is not null || startInfo.StandardErrorHandle is not null;
            if (startInfo.UseShellExecute && (anyRedirection || anyHandle))
            {
                throw new InvalidOperationException(SR.CantRedirectStreams);
            }

            if (anyHandle)
            {
                if (startInfo.StandardInputHandle is not null && startInfo.RedirectStandardInput)
                {
                    throw new InvalidOperationException(SR.CantSetHandleAndRedirect);
                }
                if (startInfo.StandardOutputHandle is not null && startInfo.RedirectStandardOutput)
                {
                    throw new InvalidOperationException(SR.CantSetHandleAndRedirect);
                }
                if (startInfo.StandardErrorHandle is not null && startInfo.RedirectStandardError)
                {
                    throw new InvalidOperationException(SR.CantSetHandleAndRedirect);
                }

                ProcessUtils.ValidateHandle(startInfo.StandardInputHandle, nameof(startInfo.StandardInputHandle));
                ProcessUtils.ValidateHandle(startInfo.StandardOutputHandle, nameof(startInfo.StandardOutputHandle));
                ProcessUtils.ValidateHandle(startInfo.StandardErrorHandle, nameof(startInfo.StandardErrorHandle));
            }

            SerializationGuard.ThrowIfDeserializationInProgress("AllowProcessCreation", ref s_cachedSerializationSwitch);

            SafeFileHandle? parentInputPipeHandle = null;
            SafeFileHandle? parentOutputPipeHandle = null;
            SafeFileHandle? parentErrorPipeHandle = null;

            SafeFileHandle? childInputPipeHandle = null;
            SafeFileHandle? childOutputPipeHandle = null;
            SafeFileHandle? childErrorPipeHandle = null;

            SafeProcessHandle? processHandle = null;

            try
            {
                if (!startInfo.UseShellExecute)
                {
                    // Windows supports creating non-inheritable pipe in atomic way.
                    // When it comes to Unixes, it depends whether they support pipe2 sys-call or not.
                    // If they don't, the pipe is created as inheritable and made non-inheritable with another sys-call.
                    // Some process could be started in the meantime, so in order to prevent accidental handle inheritance,
                    // a writer lock is used around the pipe creation code.

                    bool requiresLock = anyRedirection && !ProcessUtils.SupportsAtomicNonInheritablePipeCreation;

                    if (requiresLock)
                    {
                        ProcessUtils.s_processStartLock.EnterWriteLock();
                    }

                    try
                    {
                        if (startInfo.StandardInputHandle is not null)
                        {
                            childInputPipeHandle = startInfo.StandardInputHandle;
                        }
                        else if (startInfo.RedirectStandardInput)
                        {
                            SafeFileHandle.CreateAnonymousPipe(out childInputPipeHandle, out parentInputPipeHandle);
                        }
                        else if (!OperatingSystem.IsAndroid())
                        {
                            childInputPipeHandle = Console.OpenStandardInputHandle();
                        }

                        if (startInfo.StandardOutputHandle is not null)
                        {
                            childOutputPipeHandle = startInfo.StandardOutputHandle;
                        }
                        else if (startInfo.RedirectStandardOutput)
                        {
                            SafeFileHandle.CreateAnonymousPipe(out parentOutputPipeHandle, out childOutputPipeHandle, asyncRead: OperatingSystem.IsWindows());
                        }
                        else if (!OperatingSystem.IsAndroid())
                        {
                            childOutputPipeHandle = Console.OpenStandardOutputHandle();
                        }

                        if (startInfo.StandardErrorHandle is not null)
                        {
                            childErrorPipeHandle = startInfo.StandardErrorHandle;
                        }
                        else if (startInfo.RedirectStandardError)
                        {
                            SafeFileHandle.CreateAnonymousPipe(out parentErrorPipeHandle, out childErrorPipeHandle, asyncRead: OperatingSystem.IsWindows());
                        }
                        else if (!OperatingSystem.IsAndroid())
                        {
                            childErrorPipeHandle = Console.OpenStandardErrorHandle();
                        }
                    }
                    finally
                    {
                        if (requiresLock)
                        {
                            ProcessUtils.s_processStartLock.ExitWriteLock();
                        }
                    }
                }

                processHandle = StartCore(startInfo, childInputPipeHandle, childOutputPipeHandle, childErrorPipeHandle);
                if (processHandle.IsInvalid)
                {
                    return processHandle;
                }

                processHandle._parentInputPipeHandle = parentInputPipeHandle;
                processHandle._parentOutputPipeHandle = parentOutputPipeHandle;
                processHandle._parentErrorPipeHandle = parentErrorPipeHandle;

                parentInputPipeHandle = null;
                parentOutputPipeHandle = null;
                parentErrorPipeHandle = null;

                return processHandle;
            }
            catch
            {
                processHandle?.Dispose();
                parentInputPipeHandle?.Dispose();
                parentOutputPipeHandle?.Dispose();
                parentErrorPipeHandle?.Dispose();

                throw;
            }
            finally
            {
                // We MUST close the child handles, otherwise the parent
                // process will not receive EOF when the child process closes its handles.
                // It's OK to do it for handles returned by Console.OpenStandard*Handle APIs,
                // because these handles are not owned and won't be closed by Dispose.
                // We don't dispose handles that were passed in
                // by the caller via StartInfo.StandardInputHandle/OutputHandle/ErrorHandle.
                if (startInfo.StandardInputHandle is null)
                {
                    childInputPipeHandle?.Dispose();
                }
                if (startInfo.StandardOutputHandle is null)
                {
                    childOutputPipeHandle?.Dispose();
                }
                if (startInfo.StandardErrorHandle is null)
                {
                    childErrorPipeHandle?.Dispose();
                }
            }
        }
    }
}
