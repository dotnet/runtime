// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using size_t = System.IntPtr;
using libc = Interop.libc;
using libcurl = Interop.libcurl;
using PollFlags = Interop.libc.PollFlags;

internal static partial class Interop
{
    internal static partial class libcurl
    {
        internal sealed class SafeCurlHandle : SafeHandle
        {
            public SafeCurlHandle() : base(IntPtr.Zero, true)
            {
            }

            public override bool IsInvalid
            {
                get { return this.handle == IntPtr.Zero; }
            }

            public static void DisposeAndClearHandle(ref SafeCurlHandle curlHandle)
            {
                if (curlHandle != null)
                {
                    curlHandle.Dispose();
                    curlHandle = null;
                }
            }

            protected override bool ReleaseHandle()
            {
                libcurl.curl_easy_cleanup(this.handle);
                return true;
            }
        }

        internal sealed class SafeCurlMultiHandle : SafeHandle
        {
            private bool _pollCancelled = true;
            private readonly int[] _specialFds = new int[2];
            private readonly HashSet<int> _fdSet = new HashSet<int>();
            private int _requestCount = 0;
            private Timer _timer;

            internal bool PollCancelled
            {
                get { return _pollCancelled; }
                set { _pollCancelled = value; }
            }

            internal int RequestCount
            {
                get { return _requestCount; }
                set { _requestCount = value; }
            }

            internal Timer Timer
            {
                get { return _timer; }
                set { _timer = value; }
            }


            public SafeCurlMultiHandle()
                : base(IntPtr.Zero, true)
            {
                unsafe
                {
                    fixed(int* fds = _specialFds)
                    {
                        while (Interop.CheckIo(libc.pipe(fds)));
                    }
                }
            }

            public override bool IsInvalid
            {
                get { return this.handle == IntPtr.Zero; }
            }

            public static void DisposeAndClearHandle(ref SafeCurlMultiHandle curlHandle)
            {
                if (curlHandle != null)
                {
                    curlHandle.Dispose();
                    curlHandle = null;
                }
            }

            internal void PollFds(List<libc.pollfd> readyFds)
            {
                int count;
                libc.pollfd[] pollFds;

                readyFds.Clear();

                lock (this)
                {
                    // TODO: Avoid the allocation when count is in 100s
                    count = _fdSet.Count + 1;
                    pollFds = new libc.pollfd[count];

                    // Always include special fd in the poll set. This is used to
                    // return from the poll in case any fds have been added or
                    // removed to the set of fds being polled. This prevents starvation
                    // in case current set of fds have no activity but the new fd
                    // is ready for a read/write. The special fd is the read end of a pipe
                    // Whenever an fd is added/removed in _fdSet, a write happens to the
                    // write end of the pipe thus causing the poll to return.
                    pollFds[0].fd = _specialFds[libc.ReadEndOfPipe];
                    pollFds[0].events = PollFlags.POLLIN;
                    int i = 1;
                    foreach (int fd in _fdSet)
                    {
                        pollFds[i].fd = fd;
                        pollFds[i].events = PollFlags.POLLIN | PollFlags.POLLOUT;
                        i++;
                    }
                }

                unsafe
                {
                    fixed (libc.pollfd* fds = pollFds)
                    {
                        int numFds = libc.poll(fds, (uint)count, -1);
                        if (numFds <= 0)
                        {
                            Debug.Assert(numFds != 0); // Since timeout is infinite

                            // TODO: How to handle errors?
                            throw new InvalidOperationException("Poll failure: " + Marshal.GetLastWin32Error());
                        }

                        lock (this)
                        {
                            if (0 == _requestCount)
                            {
                                return;
                            }
                        }

                        // Check for any fdset changes
                        if (fds[0].revents != 0)
                        {
                            if (ReadSpecialFd(fds[0].revents) < 0)
                            {
                                // TODO: How to handle errors?
                                throw new InvalidOperationException("Cannot read data: " + Marshal.GetLastWin32Error());
                            }
                            numFds--;
                        }

                        // Now check for events on the remaining fds
                        for (int i = 1; i < count && numFds > 0; i++)
                        {
                            if (fds[i].revents == 0)
                            {
                                continue;
                            }
                            readyFds.Add(fds[i]);
                            numFds--;
                        }
                    }
                }
            }

            internal void SignalFdSetChange(int fd, bool isRemove)
            {
                Debug.Assert(Monitor.IsEntered(this));
                bool changed = isRemove ? _fdSet.Remove(fd) : _fdSet.Add(fd);
                if (!changed)
                {
                    return;
                }

                unsafe
                {
                    // Write to special fd
                    byte* dummyBytes = stackalloc byte[1];
                    if ((int)libc.write(_specialFds[libc.WriteEndOfPipe], dummyBytes, (size_t)1) <= 0)
                    {
                        // TODO: How to handle errors?
                        throw new InvalidOperationException("Cannot write data: " + Marshal.GetLastWin32Error());
                    }
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (null != _timer)
                    {
                        _timer.Dispose();
                    }
                }
                base.Dispose(disposing);
            }

            protected override bool ReleaseHandle()
            {
                Debug.Assert(0 == _fdSet.Count);
                Debug.Assert(0 == _requestCount);
                Debug.Assert(_pollCancelled);

                Interop.Sys.Close(_specialFds[libc.ReadEndOfPipe]);
                Interop.Sys.Close(_specialFds[libc.WriteEndOfPipe]);
                libcurl.curl_multi_cleanup(this.handle);

                return true;
            }

            private int ReadSpecialFd(PollFlags revents)
            {
                PollFlags badEvents = PollFlags.POLLERR | PollFlags.POLLHUP | PollFlags.POLLNVAL;
                if ((revents & badEvents) != 0)
                {
                    return -1;
                }
                Debug.Assert((revents & PollFlags.POLLIN) != 0);
                int pipeReadFd = _specialFds[libc.ReadEndOfPipe];
                int bytesRead = 0;
                unsafe
                {
                    do
                    {
                        // Read available data from the pipe
                        int bufferLength = 1024;
                        byte* dummyBytes = stackalloc byte[bufferLength];
                        int numBytes = (int)libc.read(pipeReadFd, dummyBytes, (size_t)bufferLength);
                        if (numBytes <= 0)
                        {
                            return -1;
                        }
                        bytesRead += numBytes;

                        // Check if more data is available
                        PollFlags outFlags;
                        int retVal = libc.poll(pipeReadFd, PollFlags.POLLIN, 0, out outFlags);
                        if (retVal < 0)
                        {
                            return -1;
                        }
                        else if (0 == retVal)
                        {
                            break;
                        }
                    }
                    while (true);
                }
                return bytesRead;
            }
        }

        internal sealed class SafeCurlSlistHandle : SafeHandle
        {
            public SafeCurlSlistHandle()
                : base(IntPtr.Zero, true)
            {
            }

            public override bool IsInvalid
            {
                get { return this.handle == IntPtr.Zero; }
            }

            public new void SetHandle(IntPtr handle)
            {
                base.SetHandle(handle);
            }

            public static void DisposeAndClearHandle(ref SafeCurlSlistHandle curlHandle)
            {
                if (curlHandle != null)
                {
                    curlHandle.Dispose();
                    curlHandle = null;
                }
            }

            protected override bool ReleaseHandle()
            {
                libcurl.curl_slist_free_all(this.handle);
                return true;
            }
        }
    }
}
