// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.NETCore.Client
{
    public class EventPipeSession : IDisposable
    {
        //! This is CoreCLR specific keywords for native ETW events (ending up in event pipe).
        //! The keywords below seems to correspond to:
        //!  GCKeyword                          (0x00000001)
        //!  LoaderKeyword                      (0x00000008)
        //!  JitKeyword                         (0x00000010)
        //!  NgenKeyword                        (0x00000020)
        //!  unused_keyword                     (0x00000100)
        //!  JittedMethodILToNativeMapKeyword   (0x00020000)
        //!  ThreadTransferKeyword              (0x80000000)
        internal const long DefaultRundownKeyword = 0x80020139;

        private ulong _sessionId;
        private IpcEndpoint _endpoint;
        private bool _disposedValue; // To detect redundant calls
        private bool _stopped; // To detect redundant calls
        private readonly IpcResponse _response;

        private EventPipeSession(IpcEndpoint endpoint, IpcResponse response, ulong sessionId)
        {
            _endpoint = endpoint;
            _response = response;
            _sessionId = sessionId;
        }

        public Stream EventStream => _response.Continuation;

        internal static EventPipeSession Start(IpcEndpoint endpoint, EventPipeSessionConfiguration config)
        {
            IpcMessage requestMessage = CreateStartMessage(config);
            IpcResponse? response = IpcClient.SendMessageGetContinuation(endpoint, requestMessage);
            return CreateSessionFromResponse(endpoint, ref response, nameof(Start));
        }

        internal static async Task<EventPipeSession> StartAsync(IpcEndpoint endpoint, EventPipeSessionConfiguration config, CancellationToken cancellationToken)
        {
            IpcMessage requestMessage = CreateStartMessage(config);
            IpcResponse? response = await IpcClient.SendMessageGetContinuationAsync(endpoint, requestMessage, cancellationToken).ConfigureAwait(false);
            return CreateSessionFromResponse(endpoint, ref response, nameof(StartAsync));
        }

        ///<summary>
        /// Stops the given session
        ///</summary>
        public void Stop()
        {
            if (TryCreateStopMessage(out IpcMessage requestMessage))
            {
                try
                {
                    IpcMessage response = IpcClient.SendMessage(_endpoint, requestMessage);

                    DiagnosticsClient.ValidateResponseMessage(response, nameof(Stop));
                }
                // On non-abrupt exits (i.e. the target process has already exited and pipe is gone, sending Stop command will fail).
                catch (IOException)
                {
                    throw new ServerNotAvailableException("Could not send Stop command. The target process may have exited.");
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (TryCreateStopMessage(out IpcMessage requestMessage))
            {
                try
                {
                    IpcMessage response = await IpcClient.SendMessageAsync(_endpoint, requestMessage, cancellationToken).ConfigureAwait(false);

                    DiagnosticsClient.ValidateResponseMessage(response, nameof(StopAsync));
                }
                // On non-abrupt exits (i.e. the target process has already exited and pipe is gone, sending Stop command will fail).
                catch (IOException)
                {
                    throw new ServerNotAvailableException("Could not send Stop command. The target process may have exited.");
                }
            }
        }

        // Internal for unit testing of the version/command selection logic.
        internal static IpcMessage CreateStartMessage(EventPipeSessionConfiguration config)
        {
            // To keep backward compatibility with older runtimes we only use newer serialization format when needed
            EventPipeCommandId command;
            byte[] payload;
            if (config.BufferingMode != EventPipeBufferingMode.Drop)
            {
                // V6 adds an opt-in session buffering mode
                command = EventPipeCommandId.CollectTracing6;
                payload = config.SerializeV6();
            }
            else if (HasEventFilter(config))
            {
                // V5 adds a per-provider event-id filter (and a session-type prefix)
                command = EventPipeCommandId.CollectTracing5;
                payload = config.SerializeV5();
            }
            else if (config.RundownKeyword != DefaultRundownKeyword && config.RundownKeyword != 0)
            {
                // V4 has added support to specify rundown keyword
                command = EventPipeCommandId.CollectTracing4;
                payload = config.SerializeV4();
            }
            else if (!config.RequestStackwalk)
            {
                // V3 has added support to disable the stacktraces
                command = EventPipeCommandId.CollectTracing3;
                payload = config.SerializeV3();
            }
            else
            {
                command = EventPipeCommandId.CollectTracing2;
                payload = config.SerializeV2();
            }

            return new IpcMessage(DiagnosticsServerCommandSet.EventPipe, (byte)command, payload);
        }

        // A per-provider Event ID filter is available on CollectTracing5 and later.
        private static bool HasEventFilter(EventPipeSessionConfiguration config)
        {
            foreach (EventPipeProvider provider in config.Providers)
            {
                if (provider.EventFilter != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static EventPipeSession CreateSessionFromResponse(IpcEndpoint endpoint, ref IpcResponse? response, string operationName)
        {
            try
            {
                DiagnosticsClient.ValidateResponseMessage(response.Value.Message, operationName);

                ulong sessionId = BinaryPrimitives.ReadUInt64LittleEndian(new ReadOnlySpan<byte>(response.Value.Message.Payload, 0, 8));

                EventPipeSession session = new(endpoint, response.Value, sessionId);
                response = null;
                return session;
            }
            finally
            {
                response?.Dispose();
            }
        }

        private bool TryCreateStopMessage(out IpcMessage stopMessage)
        {
            Debug.Assert(_sessionId > 0);

            // Do not issue another Stop command if it has already been issued for this session instance.
            if (_stopped)
            {
                stopMessage = null;
                return false;
            }
            else
            {
                _stopped = true;
            }

            byte[] payload = BitConverter.GetBytes(_sessionId);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(payload);
            }

            stopMessage = new IpcMessage(DiagnosticsServerCommandSet.EventPipe, (byte)EventPipeCommandId.StopTracing, payload);

            return true;
        }

        protected virtual void Dispose(bool disposing)
        {
            // Do not call Stop() here. Trying to do so now might block indefinitely if the runtime is unresponsive and we don't want blocking behavior in Dispose().
            // If the caller wants to ensure that all rundown events are captured they should call Stop() first, then process the EventStream until it is complete, then call Dispose().

            if (!_disposedValue)
            {
                if (disposing)
                {
                    _response.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
