// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    [Flags]
    internal enum SocketAsyncEvents
    {
        None = 0,
        Read = 1,
        Write = 2,
        ReadClose = 4,
        Close = 8,
        Error = 16
    }

    internal sealed class SocketAsyncEngine
    {
        private static SocketAsyncEngine _engine;
        private static readonly object _initLock = new object();

        private readonly SocketAsyncEngineBackend _backend;

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
                            var engine = new SocketAsyncEngine(SocketAsyncEngineBackend.Create());
                            Task.Factory.StartNew(o =>
                            {
                                SocketAsyncEngineBackend backend = ((SocketAsyncEngine)o)._backend;
                                for (;;)
                                {
                                    try
                                    {
                                        backend.EventLoop();
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

        private SocketAsyncEngine(SocketAsyncEngineBackend backend)
        {
            _backend = backend;
        }

        public bool TryRegister(int fileDescriptor, SocketAsyncEvents current, SocketAsyncEvents events, GCHandle handle, out Interop.Error error)
        {
            if (current == events)
            {
                error = Interop.Error.SUCCESS;
                return true;
            }
            return _backend.TryRegister(fileDescriptor, current, events, handle, out error);
        }
    }
}
