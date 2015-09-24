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
        private readonly int _epollFd;

        private SocketAsyncEngineBackend(int epollFd)
        {
            _epollFd = epollFd;
        }

        public static SocketAsyncEngineBackend Create()
        {
            int epollFd = Interop.libc.epoll_create1(Interop.libc.EPOLL_CLOEXEC);
            if (epollFd == -1)
            {
                // TODO: throw an appropriate exception
                throw new InternalException();
            }

            return new SocketAsyncEngineBackend(epollFd);
        }

        private static SocketAsyncEvents GetSocketAsyncEvents(uint events)
        {
            return
                (((events & Interop.libc.EPOLLIN) != 0) ? SocketAsyncEvents.Read : 0) |
                (((events & Interop.libc.EPOLLOUT) != 0) ? SocketAsyncEvents.Write : 0) |
                (((events & Interop.libc.EPOLLRDHUP) != 0) ? SocketAsyncEvents.ReadClose : 0) |
                (((events & Interop.libc.EPOLLHUP) != 0) ? SocketAsyncEvents.Close : 0) |
                (((events & Interop.libc.EPOLLERR) != 0) ? SocketAsyncEvents.Error : 0);
        }

        private static uint GetEPollEvents(SocketAsyncEvents events)
        {
            return
                (((events & SocketAsyncEvents.Read) != 0) ? Interop.libc.EPOLLIN : 0) |
                (((events & SocketAsyncEvents.Write) != 0) ? Interop.libc.EPOLLOUT : 0) |
                (((events & SocketAsyncEvents.ReadClose) != 0) ? Interop.libc.EPOLLRDHUP : 0) |
                (((events & SocketAsyncEvents.Close) != 0) ? Interop.libc.EPOLLHUP : 0) |
                (((events & SocketAsyncEvents.Error) != 0) ? Interop.libc.EPOLLERR : 0);
        }

        public unsafe void EventLoop()
        {
            const int EventCount = 64;

            var events = stackalloc Interop.libc.epoll_event[EventCount];
            for (;;)
            {
                int numEvents = Interop.libc.epoll_wait(_epollFd, events, EventCount, -1);
                if (numEvents == -1)
                {
                    // TODO: error handling + EINTR?
                    continue;
                }

                // We should never see 0 events. Given an infinite timeout, epoll_ctl will never return
                // 0 events even if there are no file descriptors registered with the epoll fd. In
                // that case, the wait will block until a file descriptor is added and an event occurs
                // on the added file descriptor.
                Debug.Assert(numEvents != 0);

                for (int i = 0; i < numEvents; i++)
                {
                    uint evts = events[i].events;

                    // epoll does not play well with disconnected connection-oriented sockets, frequently
                    // reporting spurious EPOLLHUP events. Fortunately, EPOLLHUP may be handled as an
                    // EPOLLIN | EPOLLOUT event: the usual processing for these events will recognize and
                    // handle the HUP condition.
                    if ((evts & Interop.libc.EPOLLHUP) != 0)
                    {
                        evts = (evts & ~Interop.libc.EPOLLHUP) | Interop.libc.EPOLLIN | Interop.libc.EPOLLOUT;
                    }

                    var handle = (GCHandle)events[i].data;
                    var context = (SocketAsyncContext)handle.Target;
                    if (context != null)
                    {
                        context.HandleEvents(GetSocketAsyncEvents(evts));
                    }
                }
            }
        }

        public unsafe bool TryRegister(int fileDescriptor, SocketAsyncEvents current, SocketAsyncEvents events, GCHandle handle, out Interop.Error error)
        {
            Debug.Assert(current != events);

            int op = Interop.libc.EPOLL_CTL_MOD;
            if (current == SocketAsyncEvents.None)
            {
                // This context was not listening for events, add it
                op = Interop.libc.EPOLL_CTL_ADD;
            }
            else if (events == SocketAsyncEvents.None)
            {
                // This context will no longer be listening for events, remove it
                op = Interop.libc.EPOLL_CTL_DEL;
            }

            // Register events
            var evt = new Interop.libc.epoll_event { events = GetEPollEvents(events) | Interop.libc.EPOLLET, data = (IntPtr)handle };
            int err = Interop.libc.epoll_ctl(_epollFd, op, fileDescriptor, &evt);
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
