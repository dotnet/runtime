// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    internal sealed unsafe class SocketAsyncEngine
    {
        //
        // Encapsulates a particular SocketAsyncContext object's access to a SocketAsyncEngine.  
        //
        public struct Token
        {
            private readonly SocketAsyncEngine _engine;
            private readonly IntPtr _handle;

            public Token(SocketAsyncContext context)
            {
                AllocateToken(context, out _engine, out _handle);
            }

            public bool IsAllocated
            {
                get { return _engine != null; }
            }

            public void Free()
            {
                if (IsAllocated)
                    _engine.FreeHandle(_handle);
            }

            public bool TryRegister(SafeCloseSocket socket, Interop.Sys.SocketEvents current, Interop.Sys.SocketEvents events, out Interop.Error error)
            {
                Debug.Assert(IsAllocated);
                return _engine.TryRegister(socket, current, events, _handle, out error);
            }
        }

        private const int EventBufferCount = 64;

        private static readonly object _lock = new object();

        //
        // The current engine.  We replace this with a new engine when we run out of "handle" values for the current
        // engine.
        // Must be accessed under _lock.
        //
        private static SocketAsyncEngine _currentEngine;

        private readonly int _port;
        private readonly Interop.Sys.SocketEvent* _buffer;

        //
        // The read and write ends of a native pipe, used to signal that this instance's event loop should stop 
        // processing events.
        // 
        private readonly int _shutdownReadPipe;
        private readonly int _shutdownWritePipe;

        //
        // Each SocketAsyncContext is associated with a particular "handle" value, used to identify that 
        // SocketAsyncContext when events are raised.  These handle values are never resused, because we do not have
        // a way to ensure that we will never see an event for a socket/handle that has been freed.  Instead, we
        // allocate monotonically increasing handle values up to some limit; when we would exceed that limit,
        // we allocate a new SocketAsyncEngine (and thus a new event port) and start the handle values over at zero.
        // Thus we can uniquely identify a given SocketAsyncContext by the *pair* {SocketAsyncEngine, handle},
        // and avoid any issues with misidentifying the target of an event we read from the port.
        //
#if DEBUG
        //
        // In debug builds, force rollover to new SocketAsyncEngine instances so that code doesn't go untested, since
        // it's very unlikely that the "real" limits will ever be reached in test code.
        //
        private readonly IntPtr MaxHandles = (IntPtr)(EventBufferCount * 2);
#else
        //
        // In release builds, we use *very* high limits.  No 64-bit process running on release builds should ever
        // reach the handle limit for a single event port, and even 32-bit processes should see this only very rarely.
        //
        private readonly IntPtr MaxHandles = IntPtr.Size == 4 ? (IntPtr)int.MaxValue : (IntPtr)long.MaxValue;
#endif

        //
        // Sentinel handle value to identify events from the "shutdown pipe," used to signal an event loop to stop
        // processing events.
        //
        private readonly IntPtr ShutdownHandle = (IntPtr)(-1);

        //
        // The next handle value to be allocated for this event port.
        // Must be accessed under _lock.
        //
        private IntPtr _nextHandle;

        //
        // Count of handles that have been allocated for this event port, but not yet freed.
        // Must be accessed under _lock.
        // 
        private IntPtr _outstandingHandles;

        //
        // Maps handle values to SocketAsyncContext instances.
        // Must be accessed under _lock.
        //
        private readonly Dictionary<IntPtr, SocketAsyncContext> _handleToContextMap = new Dictionary<IntPtr, SocketAsyncContext>();

        //
        // True if we've reached the handle value limit for this event port, and thus must allocate a new event port
        // on the next handle allocation.
        //
        private bool IsFull { get { return _nextHandle == MaxHandles; } }

        //
        // Allocates a new {SocketAsyncEngine, handle} pair.
        //
        private static void AllocateToken(SocketAsyncContext context, out SocketAsyncEngine engine, out IntPtr handle)
        {
            lock (_lock)
            {
                if (_currentEngine == null || _currentEngine.IsFull)
                {
                    _currentEngine = new SocketAsyncEngine();
                }

                engine = _currentEngine;
                handle = _currentEngine.AllocateHandle(context);
            }
        }

        private IntPtr AllocateHandle(SocketAsyncContext context)
                    {
            Debug.Assert(Monitor.IsEntered(_lock));
            Debug.Assert(!IsFull);

            IntPtr handle = _nextHandle;
            _handleToContextMap.Add(handle, context);

            _nextHandle = IntPtr.Add(_nextHandle, 1);
            _outstandingHandles = IntPtr.Add(_outstandingHandles, 1);

            Debug.Assert(handle != ShutdownHandle);
            return handle;
        }

        private void FreeHandle(IntPtr handle)
                        {
            Debug.Assert(handle != ShutdownHandle);

            bool shutdownNeeded = false;

            lock (_lock)
                            {
                if (_handleToContextMap.Remove(handle))
                {
                    _outstandingHandles = IntPtr.Subtract(_outstandingHandles, 1);

                    Debug.Assert(_outstandingHandles.ToInt64() >= 0);

                    //
                    // If we've allocated all possible handles for this instance, and freed them all, then 
                    // we don't need the event loop any more, and can reclaim resources.
                    //
                    if (IsFull && _outstandingHandles == IntPtr.Zero)
                        shutdownNeeded = true;
                }
                            }

            //
            // Signal shutdown outside of the lock to reduce contention.  
            //
            if (shutdownNeeded)
                            {
                BeginShutdown();
            }
                            }

        private SocketAsyncContext GetContextFromHandle(IntPtr handle)
        {
            Debug.Assert(handle != ShutdownHandle);
            Debug.Assert(handle.ToInt64() < MaxHandles.ToInt64());
            lock (_lock)
                            {
                SocketAsyncContext context;
                _handleToContextMap.TryGetValue(handle, out context);
                return context;
            }
        }

        private SocketAsyncEngine()
                                {
            _port = -1;
            _shutdownReadPipe = -1;
            _shutdownWritePipe = -1;
                                    try
                                    {
                //
                // Create the event port and buffer
                //
                if (Interop.Sys.CreateSocketEventPort(out _port) != Interop.Error.SUCCESS)
                {
                    throw new InternalException();
                                    }
                if (Interop.Sys.CreateSocketEventBuffer(EventBufferCount, out _buffer) != Interop.Error.SUCCESS)
                                    {
                    throw new InternalException();
                                    }

                //
                // Create the pipe for signalling shutdown, and register for "read" events for the pipe.  Now writing
                // to the pipe will send an event to the event loop.
                //
                int* pipeFds = stackalloc int[2];
                if (Interop.Sys.Pipe(pipeFds, Interop.Sys.PipeFlags.O_CLOEXEC) != 0)
                {
                    throw new InternalException();
                                }
                _shutdownReadPipe = pipeFds[Interop.Sys.ReadEndOfPipe];
                _shutdownWritePipe = pipeFds[Interop.Sys.WriteEndOfPipe];

                if (Interop.Sys.DangerousTryChangeSocketEventRegistration(_port, _shutdownReadPipe, Interop.Sys.SocketEvents.None, Interop.Sys.SocketEvents.Read, ShutdownHandle) != Interop.Error.SUCCESS)
                {
                    throw new InternalException();
                        }

                //
                // Start the event loop on its own thread.
                //
                Task.Factory.StartNew(
                    EventLoop,
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
                    }
            catch
            {
                if (_shutdownReadPipe != -1)
                {
                    Interop.Sys.Close((IntPtr)_shutdownReadPipe);
                }
                if (_shutdownWritePipe != -1)
                {
                    Interop.Sys.Close((IntPtr)_shutdownWritePipe);
            }
                if (_buffer != null)
                {
                    Interop.Sys.FreeSocketEventBuffer(_buffer);
        }
                if (_port != -1)
        {
                    Interop.Sys.CloseSocketEventPort(_port);
                }
                throw;
            }
        }

        private void EventLoop()
        {
            try
            {
                bool shutdown = false;
                while (!shutdown)
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
                        IntPtr handle = _buffer[i].Data;
                        if (handle == ShutdownHandle)
                        {
                            shutdown = true;
                        }
                        else
                        {
                            SocketAsyncContext context = GetContextFromHandle(handle);
                    if (context != null)
                    {
                        context.HandleEvents(_buffer[i].Events);
                    }
                        }
                    }
                }

                EndShutdown();
            }
            catch (Exception e)
            {
                Environment.FailFast("Exception thrown from SocketAsyncEngine event loop", e);
            }
        }

        private void BeginShutdown()
        {
            //
            // Write to the pipe, which will wake up the event loop and cause it to exit.
            //
            byte* buf = stackalloc byte[1];
            int bytesWritten = Interop.Sys.Write(_shutdownWritePipe, buf, 1);
            if (bytesWritten != 1)
            {
                throw new InternalException();
                }
            }

        private void EndShutdown()
        {
            //
            // The event loop is exiting, so we can free all native resources.
            //
            Interop.Sys.Close((IntPtr)_shutdownReadPipe);
            Interop.Sys.Close((IntPtr)_shutdownWritePipe);
            Interop.Sys.CloseSocketEventPort(_port);
            Interop.Sys.FreeSocketEventBuffer(_buffer);
        }

        private bool TryRegister(SafeCloseSocket socket, Interop.Sys.SocketEvents current, Interop.Sys.SocketEvents events, IntPtr handle, out Interop.Error error)
        {
            if (current == events)
            {
                error = Interop.Error.SUCCESS;
                return true;
            }

            error = Interop.Sys.TryChangeSocketEventRegistration(_port, socket, current, events, handle);
            return error == Interop.Error.SUCCESS;
        }
    }
}
