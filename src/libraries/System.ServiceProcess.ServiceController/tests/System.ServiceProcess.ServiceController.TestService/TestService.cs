// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace System.ServiceProcess.Tests
{
    public class TestService : ServiceBase
    {
        private bool _disposed;
        private Task _waitClientConnect;
        private NamedPipeServerStream _serverStream;
        private readonly Exception _exception;

        public TestService(string serviceName, Exception throwException = null)
        {
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
            _waitClientConnect.ContinueWith((t) => WriteStreamAsync(PipeMessageByteCode.Connected));
        }

        protected override void OnContinue()
        {
            base.OnContinue();
            WriteStreamAsync(PipeMessageByteCode.Continue).Wait();
        }

        protected override void OnCustomCommand(int command)
        {
            base.OnCustomCommand(command);
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
            base.OnStop();
            WriteStreamAsync(PipeMessageByteCode.Stop).Wait();
        }

        public async Task WriteStreamAsync(PipeMessageByteCode code, int command = 0)
        {
            if (_waitClientConnect.IsCompleted)
            {
                Task writeCompleted;
                const int WriteTimeout = 60000;
                if (code == PipeMessageByteCode.OnCustomCommand)
                {
                    writeCompleted = _serverStream.WriteAsync(new byte[] { (byte)command }, 0, 1);
                }
                else
                {
                    writeCompleted = _serverStream.WriteAsync(new byte[] { (byte)code }, 0, 1);
                }
                await writeCompleted.TimeoutAfter(WriteTimeout).ConfigureAwait(false);
            }
            else
            {
                // We get here if the service is getting torn down before a client ever connected.
                // some tests do this.
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _serverStream.Dispose();
                _disposed = true;
                base.Dispose();
            }
        }
    }
}
