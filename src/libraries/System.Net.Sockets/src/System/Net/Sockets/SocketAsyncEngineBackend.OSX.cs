// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    internal struct SocketAsyncEngineBackend
    {
        private readonly int _kqueueFd;

        private SocketAsyncEngineBackend(int kqueueFd)
        {
            _kqueueFd = kqueueFd;
        }

        public static SocketAsyncEngineBackend Create()
        {
            int kqueueFd = Interop.libc.kqueue();
            if (kqueueFd == -1)
            {
                // TODO: throw an appropriate exception
                throw new InternalException();
            }

            return new SocketAsyncEngineBackend(kqueueFd);
        }

        private static SocketAsyncEvents GetSocketAsyncEvents(short filter, ushort flags)
        {
            SocketAsyncEvents events;
            switch (filter)
            {
                case Interop.libc.EVFILT_READ:
                    events = SocketAsyncEvents.Read;
                    if ((flags & Interop.libc.EV_EOF) != 0)
                    {
                        events |= SocketAsyncEvents.ReadClose;
                    }
                    break;

                case Interop.libc.EVFILT_WRITE:
                    events = SocketAsyncEvents.Write;

                    // kqueue does not play well with disconnected connection-oriented sockets, frequently
                    // reporting spurious EOF events. Fortunately, EOF may be handled as an EVFILT_READ |
                    // EVFILT_WRITE event: the usual processing for these events will recognize and
                    // handle the EOF condition.
                    if ((flags & Interop.libc.EV_EOF) != 0)
                    {
                        events |= SocketAsyncEvents.Read;
                    }
                    break;

                default:
                    Debug.Fail("unexpected kqueue filter type");
                    return SocketAsyncEvents.None;
            }

            if ((flags & Interop.libc.EV_ERROR) != 0)
            {
                events |= SocketAsyncEvents.Error;
            }
            return events;
        }

        public unsafe void EventLoop()
        {
            const int EventCount = 64;

            var events = stackalloc Interop.libc.kevent64_s[EventCount];
            for (;;)
            {
                int numEvents = Interop.libc.kevent64(_kqueueFd, null, 0, events, EventCount, 0, null);
                if (numEvents == -1)
                {
                    // TODO: error handling + EINTR?
                    continue;
                }

                // We should never see 0 events. Given an infinite timeout, kevent64 will never return
                // 0 events even if there are no file descriptors registered with the kqueue fd. In
                // that case, the wait will block until a file descriptor is added and an event occurs
                // on the added file descriptor.
                Debug.Assert(numEvents != 0);

                for (int i = 0; i < numEvents; i++)
                {
                    var handle = (GCHandle)(IntPtr)events[i].udata;
                    var context = (SocketAsyncContext)handle.Target;

                    if (context != null)
                    {
                        context.HandleEvents(GetSocketAsyncEvents(events[i].filter, events[i].flags));
                    }
                }
            }
        }

        public unsafe bool TryRegister(int fileDescriptor, SocketAsyncEvents current, SocketAsyncEvents events, GCHandle handle, out Interop.Error error)
        {
            const ushort AddFlags = Interop.libc.EV_ADD | Interop.libc.EV_CLEAR | Interop.libc.EV_RECEIPT;
            const ushort RemoveFlags = Interop.libc.EV_DELETE | Interop.libc.EV_RECEIPT;

            Debug.Assert(current != events);

            SocketAsyncEvents changed = current ^ events;
            bool readChanged = (changed & SocketAsyncEvents.Read) != 0;
            bool writeChanged = (changed & SocketAsyncEvents.Write) != 0;

            int evtCount = (readChanged ? 1 : 0) + (writeChanged ? 1 : 0);
            var kevents = stackalloc Interop.libc.kevent64_s[evtCount];

            int i = 0;
            if (readChanged)
            {
                kevents[0].ident = unchecked((ulong)fileDescriptor);
                kevents[0].filter = Interop.libc.EVFILT_READ;
                kevents[0].flags = (events & SocketAsyncEvents.Read) == 0 ? RemoveFlags : AddFlags;
                kevents[0].fflags = 0;
                kevents[0].data = 0;
                kevents[0].udata = (ulong)(IntPtr)handle;
                i = 1;
            }
            if (writeChanged)
            {
                kevents[i].ident = unchecked((ulong)fileDescriptor);
                kevents[i].filter = Interop.libc.EVFILT_WRITE;
                kevents[i].flags = (events & SocketAsyncEvents.Write) == 0 ? RemoveFlags : AddFlags;
                kevents[i].fflags = 0;
                kevents[i].data = 0;
                kevents[i].udata = (ulong)(IntPtr)handle;
            }

            int err = Interop.libc.kevent64(_kqueueFd, kevents, evtCount, null, 0, 0, null);
            if (err == 0)
            {
                error = Interop.Error.SUCCESS;
                return true;
            }

            error = Interop.Sys.GetLastError();
            return false;
        }
    }
}
