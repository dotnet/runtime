// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Threading;

namespace System.IO.Pipes
{
    /// <summary>
    /// Named pipe client. Use this to open the client end of a named pipes created with 
    /// NamedPipeServerStream.
    /// </summary>
    public sealed partial class NamedPipeClientStream : PipeStream
    {
        private static string GetPipePath(string serverName, string pipeName)
        {
            string normalizedPipePath = Path.GetFullPath(@"\\" + serverName + @"\pipe\" + pipeName);
            if (String.Equals(normalizedPipePath, @"\\.\pipe\anonymous", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentOutOfRangeException("pipeName", SR.ArgumentOutOfRange_AnonymousReserved);
            }
            return normalizedPipePath;
        }

        // Waits for a pipe instance to become available. This method may return before WaitForConnection is called
        // on the server end, but WaitForConnection will not return until we have returned.  Any data writen to the
        // pipe by us after we have connected but before the server has called WaitForConnection will be available
        // to the server after it calls WaitForConnection. 
        [SecurityCritical]
        private void ConnectInternal(int timeout, CancellationToken cancellationToken, int startTime)
        {
            Interop.SECURITY_ATTRIBUTES secAttrs = PipeStream.GetSecAttrs(_inheritability);

            int _pipeFlags = (int)_pipeOptions;
            if (_impersonationLevel != TokenImpersonationLevel.None)
            {
                _pipeFlags |= Interop.SECURITY_SQOS_PRESENT;
                _pipeFlags |= (((int)_impersonationLevel - 1) << 16);
            }

            // This is the main connection loop. It will loop until the timeout expires.  Most of the 
            // time, we will be waiting in the WaitNamedPipe win32 blocking function; however, there are
            // cases when we will need to loop: 1) The server is not created (WaitNamedPipe returns 
            // straight away in such cases), and 2) when another client connects to our server in between 
            // our WaitNamedPipe and CreateFile calls.
            int elapsed = 0;
            do
            {
                // We want any other exception and and success to have priority over cancellation.
                cancellationToken.ThrowIfCancellationRequested();

                // Wait for pipe to become free (this will block unless the pipe does not exist).
                int timeLeft = timeout - elapsed;
                int waitTime;
                if (cancellationToken.CanBeCanceled)
                {
                    waitTime = Math.Min(CancellationCheckInterval, timeLeft);
                }
                else
                {
                    waitTime = timeLeft;
                }

                if (!Interop.mincore.WaitNamedPipe(_normalizedPipePath, waitTime))
                {
                    int errorCode = Marshal.GetLastWin32Error();

                    // Server is not yet created so let's keep looping.
                    if (errorCode == Interop.ERROR_FILE_NOT_FOUND)
                    {
                        continue;
                    }

                    // The timeout has expired.
                    if (errorCode == Interop.ERROR_SUCCESS)
                    {
                        if (cancellationToken.CanBeCanceled)
                        {
                            // It may not be real timeout and only checking for cancellation
                            // let the while condition check it and decide
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }

                    throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                }

                // Pipe server should be free.  Let's try to connect to it.
                int access = 0;
                if ((PipeDirection.In & _direction) != 0)
                {
                    access |= Interop.GENERIC_READ;
                }
                if ((PipeDirection.Out & _direction) != 0)
                {
                    access |= Interop.GENERIC_WRITE;
                }
                SafePipeHandle handle = Interop.mincore.CreateNamedPipeClient(_normalizedPipePath,
                                            access,           // read and write access
                                            0,                  // sharing: none
                                            ref secAttrs,           // security attributes
                                            FileMode.Open,      // open existing 
                                            _pipeFlags,         // impersonation flags
                                            Interop.NULL);  // template file: null

                if (handle.IsInvalid)
                {
                    int errorCode = Marshal.GetLastWin32Error();

                    // Handle the possible race condition of someone else connecting to the server 
                    // between our calls to WaitNamedPipe & CreateFile.
                    if (errorCode == Interop.ERROR_PIPE_BUSY)
                    {
                        continue;
                    }

                    throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                }

                // Success! 
                InitializeHandle(handle, false, (_pipeOptions & PipeOptions.Asynchronous) != 0);
                State = PipeState.Connected;

                return;
            }
            while (timeout == Timeout.Infinite || (elapsed = unchecked(Environment.TickCount - startTime)) < timeout);
            // BUGBUG: SerialPort does not use unchecked arithmetic when calculating elapsed times.  This is needed
            //         because Environment.TickCount can overflow (though only every 49.7 days).

            throw new TimeoutException();
        }

        public int NumberOfServerInstances
        {
            [SecurityCritical]
            [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Security model of pipes: demand at creation but no subsequent demands")]
            get
            {
                CheckPipePropertyOperations();

                // NOTE: MSDN says that GetNamedPipeHandleState requires that the pipe handle has 
                // GENERIC_READ access, but we don't check for that because sometimes it works without
                // GERERIC_READ access. [Edit: Seems like CreateFile slaps on a READ_ATTRIBUTES 
                // access request before calling NTCreateFile, so all NamedPipeClientStreams can read
                // this if they are created (on WinXP SP2 at least)] 
                int numInstances;
                if (!Interop.mincore.GetNamedPipeHandleState(InternalHandle, Interop.NULL, out numInstances,
                    Interop.NULL, Interop.NULL, Interop.NULL, 0))
                {
                    WinIOError(Marshal.GetLastWin32Error());
                }

                return numInstances;
            }
        }

    }
}
