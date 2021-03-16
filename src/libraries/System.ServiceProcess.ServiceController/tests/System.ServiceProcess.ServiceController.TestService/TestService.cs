// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace System.ServiceProcess.Tests
{
    public class TestService : ServiceBase
    {
        // To view tracing, use DbgView from sysinternals.com;
        // run it elevated, check "Capture>Global Win32" and "Capture>Win32",
        // and filter to just messages beginning with "##"
        internal const bool DebugTracing = false;

        private bool _disposed;
        private Task _waitClientConnect;
        private NamedPipeServerStream _serverStream;
        private readonly Exception _exception;

        public TestService(string serviceName, Exception throwException = null)
        {
            DebugTrace("TestService " + ServiceName + ": Ctor");
            this.ServiceName = serviceName;

            // Enable all the events
            this.CanPauseAndContinue = true;
            this.CanStop = true;
            this.CanShutdown = true;

            // We cannot easily test these so disable the events
            this.CanHandleSessionChangeEvent = false;
            this.CanHandlePowerEvent = false;
            this._exception = throwException;
            this._serverStream = new NamedPipeServerStream(serviceName);
            _waitClientConnect = this._serverStream.WaitForConnectionAsync();
            _waitClientConnect = _waitClientConnect.ContinueWith(_ => DebugTrace("TestService " + ServiceName + ": Connected"));
            _waitClientConnect.ContinueWith(t => WriteStreamAsync(PipeMessageByteCode.Connected));
            DebugTrace("TestService " + ServiceName + ": Ctor completed");
        }

        protected override void OnContinue()
        {
            base.OnContinue();
            WriteStreamAsync(PipeMessageByteCode.Continue).Wait();
        }

        protected override void OnCustomCommand(int command)
        {
            base.OnCustomCommand(command);

            if (Environment.UserInteractive) // see ServiceBaseTests.TestOnExecuteCustomCommand()
                command++;

            WriteStreamAsync(PipeMessageByteCode.OnCustomCommand, command).Wait();
        }

        protected override void OnPause()
        {
            base.OnPause();
            WriteStreamAsync(PipeMessageByteCode.Pause).Wait();
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            base.OnSessionChange(changeDescription);
        }

        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            return base.OnPowerEvent(powerStatus);
        }

        protected override void OnShutdown()
        {
            base.OnShutdown();
        }

        protected override void OnStart(string[] args)
        {
            DebugTrace("TestService " + ServiceName + ": OnStart");
            base.OnStart(args);
            if (_exception != null)
            {
                throw _exception;
            }

            if (args.Length == 4 && args[0] == "StartWithArguments")
            {
                Debug.Assert(args[1] == "a");
                Debug.Assert(args[2] == "b");
                Debug.Assert(args[3] == "c");
                WriteStreamAsync(PipeMessageByteCode.Start).Wait();
            }
        }

        protected override void OnStop()
        {
            DebugTrace("TestService " + ServiceName + ": OnStop");
            base.OnStop();
            WriteStreamAsync(PipeMessageByteCode.Stop).Wait();
        }

        public async Task WriteStreamAsync(PipeMessageByteCode code, int command = 0)
        {
            DebugTrace("TestService " + ServiceName + ": WriteStreamAsync writing " + code.ToString());

            var toWrite = (code == PipeMessageByteCode.OnCustomCommand) ? new byte[] { (byte)command } : new byte[] { (byte)code };

            // Wait for the client connection before writing to the pipe.
            // Exception: if it's a Stop message, it may be because the test has completed and we're cleaning up the test service so there's no client at all.
            // Tests that verify "Stop" itself should ensure the client connection has completed before calling stop, by waiting on some other message from the pipe first.
            if (code != PipeMessageByteCode.Stop)
            {
                await _waitClientConnect;
            }
            await _serverStream.WriteAsync(toWrite, 0, 1).WaitAsync(TimeSpan.FromSeconds(60)).ConfigureAwait(false);
            DebugTrace("TestService " + ServiceName + ": WriteStreamAsync completed");
        }

        /// <summary>
        /// This is called from <see cref="ServiceBase.Run(ServiceBase[])"/> when all services in the process
        /// have entered the SERVICE_STOPPED state. It disposes the named pipe stream.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                DebugTrace("TestService " + ServiceName + ": Disposing");
                _serverStream.Dispose();
                _disposed = true;
                base.Dispose();
            }
        }

        internal static void DebugTrace(string message)
        {
            if (DebugTracing)
            {
#pragma warning disable CS0162 // unreachable code
                Debug.WriteLine("## " + message);
#pragma warning restore
            }
        }
    }
}
