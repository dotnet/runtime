// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    internal sealed unsafe class SocketAsyncEngine
    {
        private const int EventBufferCount = 64;

        private static SocketAsyncEngine _engine;
        private static readonly object _initLock = new object();

        private readonly int _port;
        private readonly Interop.Sys.SocketEvent* _buffer;

        public static SocketAsyncEngine Instance
        {
            get
            {
                if (Volatile.Read(ref _engine) == null)
                {
                    lock (_initLock)
                    {
                        if (_engine == null)
                        {
                            int port;
                            Interop.Error err = Interop.Sys.CreateSocketEventPort(&port);
                            if (err != Interop.Error.SUCCESS)
                            {
                                throw new InternalException();
                            }

                            Interop.Sys.SocketEvent* buffer;
                            err = Interop.Sys.CreateSocketEventBuffer(EventBufferCount, &buffer);
                            if (err != Interop.Error.SUCCESS)
                            {
                                Interop.Sys.CloseSocketEventPort(port);
                                throw new InternalException();
                            }

                            var engine = new SocketAsyncEngine(port, buffer);
                            Task.Factory.StartNew(o =>
                            {
                                var eng = (SocketAsyncEngine)o;
                                for (;;)
                                {
                                    try
                                    {
                                        eng.EventLoop();
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.Fail(string.Format("Exception thrown from event loop: {0}", e.Message));
                                    }
                                }
                            }, engine, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                            Volatile.Write(ref _engine, engine);
                        }
                    }
                }

                return _engine;
            }
        }

        private SocketAsyncEngine(int port, Interop.Sys.SocketEvent* buffer)
        {
            _port = port;
            _buffer = buffer;
        }

        private void EventLoop()
        {
            for (;;)
            {
                int numEvents = EventBufferCount;
                Interop.Error err = Interop.Sys.WaitForSocketEvents(_port, _buffer, &numEvents);
                if (err != Interop.Error.SUCCESS)
                {
                    throw new InternalException();
                }

                // The native shim is responsible for ensuring this condition.
                Debug.Assert(numEvents > 0);

                for (int i = 0; i < numEvents; i++)
                {
                    var handle = (GCHandle)(IntPtr)_buffer[i].Data;
                    var context = (SocketAsyncContext)handle.Target;

                    if (context != null)
                    {
                        context.HandleEvents(_buffer[i].Events);
                    }
                }
            }
        }

        public bool TryRegister(SafeCloseSocket socket, Interop.Sys.SocketEvents current, Interop.Sys.SocketEvents events, GCHandle handle, out Interop.Error error)
        {
            if (current == events)
            {
                error = Interop.Error.SUCCESS;
                return true;
            }

            //
            // @TODO: work out a better way to handle this.  For now, just do the "dangerous" thing; this is called
            // from SafeCloseSocket.ReleaseHandle, so it can't access the file descriptor in the normal way.
            // 
            int fd = (int)socket.DangerousGetHandle();

            error = Interop.Sys.DangerousTryChangeSocketEventRegistration(_port, fd, current, events, (IntPtr)handle);
            return error == Interop.Error.SUCCESS;
        }
    }
}
