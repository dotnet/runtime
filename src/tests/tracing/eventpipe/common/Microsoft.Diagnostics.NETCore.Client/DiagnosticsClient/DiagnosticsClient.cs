// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.NETCore.Client
{
    /// <summary>
    /// This is a top-level class that contains methods to send various diagnostics command to the runtime.
    /// </summary>
    public sealed class DiagnosticsClient
    {
        private readonly IpcEndpoint _endpoint;

        public DiagnosticsClient(int processId) :
            this(new PidIpcEndpoint(processId))
        {
        }

        internal DiagnosticsClient(IpcEndpointConfig config) :
            this(new DiagnosticPortIpcEndpoint(config))
        {
        }

        internal DiagnosticsClient(IpcEndpoint endpoint)
        {
            _endpoint = endpoint;
        }

        /// <summary>
        /// Wait for an available diagnostic endpoint to the runtime instance.
        /// </summary>
        /// <param name="timeout">The amount of time to wait before cancelling the wait for the connection.</param>
        internal void WaitForConnection(TimeSpan timeout)
        {
            _endpoint.WaitForConnection(timeout);
        }

        /// <summary>
        /// Wait for an available diagnostic endpoint to the runtime instance.
        /// </summary>
        /// <param name="token">The token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task the completes when a diagnostic endpoint to the runtime instance becomes available.
        /// </returns>
        internal Task WaitForConnectionAsync(CancellationToken token)
        {
            return _endpoint.WaitForConnectionAsync(token);
        }

        /// <summary>
        /// Start tracing the application and return an EventPipeSession object
        /// </summary>
        /// <param name="providers">An IEnumerable containing the list of Providers to turn on.</param>
        /// <param name="requestRundown">If true, request rundown events from the runtime</param>
        /// <param name="circularBufferMB">The size of the runtime's buffer for collecting events in MB</param>
        /// <returns>
        /// An EventPipeSession object representing the EventPipe session that just started.
        /// </returns> 
        public EventPipeSession StartEventPipeSession(IEnumerable<EventPipeProvider> providers, bool requestRundown = true, int circularBufferMB = 256)
        {
            return EventPipeSession.Start(_endpoint, providers, requestRundown, circularBufferMB);
        }

        /// <summary>
        /// Start tracing the application and return an EventPipeSession object
        /// </summary>
        /// <param name="provider">An EventPipeProvider to turn on.</param>
        /// <param name="requestRundown">If true, request rundown events from the runtime</param>
        /// <param name="circularBufferMB">The size of the runtime's buffer for collecting events in MB</param>
        /// <returns>
        /// An EventPipeSession object representing the EventPipe session that just started.
        /// </returns> 
        public EventPipeSession StartEventPipeSession(EventPipeProvider provider, bool requestRundown = true, int circularBufferMB = 256)
        {
            return EventPipeSession.Start(_endpoint, new[] { provider }, requestRundown, circularBufferMB);
        }

        /// <summary>
        /// Start tracing the application and return an EventPipeSession object
        /// </summary>
        /// <param name="providers">An IEnumerable containing the list of Providers to turn on.</param>
        /// <param name="requestRundown">If true, request rundown events from the runtime</param>
        /// <param name="circularBufferMB">The size of the runtime's buffer for collecting events in MB</param>
        /// <param name="token">The token to monitor for cancellation requests.</param>
        /// <returns>
        /// An EventPipeSession object representing the EventPipe session that just started.
        /// </returns> 
        internal Task<EventPipeSession> StartEventPipeSessionAsync(IEnumerable<EventPipeProvider> providers, bool requestRundown, int circularBufferMB, CancellationToken token)
        {
            return EventPipeSession.StartAsync(_endpoint, providers, requestRundown, circularBufferMB, token);
        }

        /// <summary>
        /// Start tracing the application and return an EventPipeSession object
        /// </summary>
        /// <param name="provider">An EventPipeProvider to turn on.</param>
        /// <param name="requestRundown">If true, request rundown events from the runtime</param>
        /// <param name="circularBufferMB">The size of the runtime's buffer for collecting events in MB</param>
        /// <param name="token">The token to monitor for cancellation requests.</param>
        /// <returns>
        /// An EventPipeSession object representing the EventPipe session that just started.
        /// </returns>
        internal Task<EventPipeSession> StartEventPipeSessionAsync(EventPipeProvider provider, bool requestRundown, int circularBufferMB, CancellationToken token)
        {
            return EventPipeSession.StartAsync(_endpoint, new[] { provider }, requestRundown, circularBufferMB, token);
        }

        /// <summary>
        /// Trigger a core dump generation.
        /// </summary> 
        /// <param name="dumpType">Type of the dump to be generated</param>
        /// <param name="dumpPath">Full path to the dump to be generated. By default it is /tmp/coredump.{pid}</param>
        /// <param name="logDumpGeneration">When set to true, display the dump generation debug log to the console.</param>
        public void WriteDump(DumpType dumpType, string dumpPath, bool logDumpGeneration = false)
        {
            WriteDump(dumpType, dumpPath, logDumpGeneration ? WriteDumpFlags.LoggingEnabled : WriteDumpFlags.None);
        }

        /// <summary>
        /// Trigger a core dump generation.
        /// </summary> 
        /// <param name="dumpType">Type of the dump to be generated</param>
        /// <param name="dumpPath">Full path to the dump to be generated. By default it is /tmp/coredump.{pid}</param>
        /// <param name="flags">logging and crash report flags. On runtimes less than 6.0, only LoggingEnabled is supported.</param>
        public void WriteDump(DumpType dumpType, string dumpPath, WriteDumpFlags flags)
        {
            IpcMessage request = CreateWriteDumpMessage(DumpCommandId.GenerateCoreDump3, dumpType, dumpPath, flags);
            IpcMessage response = IpcClient.SendMessage(_endpoint, request);
            if (!ValidateResponseMessage(response, "Write dump", ValidateResponseOptions.UnknownCommandReturnsFalse | ValidateResponseOptions.ErrorMessageReturned))
            {
                request = CreateWriteDumpMessage(DumpCommandId.GenerateCoreDump2, dumpType, dumpPath, flags);
                response = IpcClient.SendMessage(_endpoint, request);
                if (!ValidateResponseMessage(response, "Write dump", ValidateResponseOptions.UnknownCommandReturnsFalse))
                {
                    if ((flags & ~WriteDumpFlags.LoggingEnabled) != 0)
                    {
                        throw new ArgumentException($"Only {nameof(WriteDumpFlags.LoggingEnabled)} flag is supported by this runtime version", nameof(flags));
                    }
                    request = CreateWriteDumpMessage(dumpType, dumpPath, logDumpGeneration: (flags & WriteDumpFlags.LoggingEnabled) != 0);
                    response = IpcClient.SendMessage(_endpoint, request);
                    ValidateResponseMessage(response, "Write dump");
                }
            }
        }

        /// <summary>
        /// Trigger a core dump generation.
        /// </summary> 
        /// <param name="dumpType">Type of the dump to be generated</param>
        /// <param name="dumpPath">Full path to the dump to be generated. By default it is /tmp/coredump.{pid}</param>
        /// <param name="logDumpGeneration">When set to true, display the dump generation debug log to the console.</param>
        /// <param name="token">The token to monitor for cancellation requests.</param>
        public Task WriteDumpAsync(DumpType dumpType, string dumpPath, bool logDumpGeneration, CancellationToken token)
        {
            return WriteDumpAsync(dumpType, dumpPath, logDumpGeneration ? WriteDumpFlags.LoggingEnabled : WriteDumpFlags.None, token);
        }

        /// <summary>
        /// Trigger a core dump generation.
        /// </summary> 
        /// <param name="dumpType">Type of the dump to be generated</param>
        /// <param name="dumpPath">Full path to the dump to be generated. By default it is /tmp/coredump.{pid}</param>
        /// <param name="flags">logging and crash report flags. On runtimes less than 6.0, only LoggingEnabled is supported.</param>
        /// <param name="token">The token to monitor for cancellation requests.</param>
        public async Task WriteDumpAsync(DumpType dumpType, string dumpPath, WriteDumpFlags flags, CancellationToken token)
        {
            IpcMessage request = CreateWriteDumpMessage(DumpCommandId.GenerateCoreDump3, dumpType, dumpPath, flags);
            IpcMessage response = await IpcClient.SendMessageAsync(_endpoint, request, token).ConfigureAwait(false);
            if (!ValidateResponseMessage(response, "Write dump", ValidateResponseOptions.UnknownCommandReturnsFalse | ValidateResponseOptions.ErrorMessageReturned))
            {
                request = CreateWriteDumpMessage(DumpCommandId.GenerateCoreDump2, dumpType, dumpPath, flags);
                response = await IpcClient.SendMessageAsync(_endpoint, request, token).ConfigureAwait(false);
                if (!ValidateResponseMessage(response, "Write dump", ValidateResponseOptions.UnknownCommandReturnsFalse))
                {
                    if ((flags & ~WriteDumpFlags.LoggingEnabled) != 0)
                    {
                        throw new ArgumentException($"Only {nameof(WriteDumpFlags.LoggingEnabled)} flag is supported by this runtime version", nameof(flags));
                    }
                    request = CreateWriteDumpMessage(dumpType, dumpPath, logDumpGeneration: (flags & WriteDumpFlags.LoggingEnabled) != 0);
                    response = await IpcClient.SendMessageAsync(_endpoint, request, token).ConfigureAwait(false);
                    ValidateResponseMessage(response, "Write dump");
                }
            }
        }

        /// <summary>
        /// Attach a profiler.
        /// </summary>
        /// <param name="attachTimeout">Timeout for attaching the profiler</param>
        /// <param name="profilerGuid">Guid for the profiler to be attached</param>
        /// <param name="profilerPath">Path to the profiler to be attached</param>
        /// <param name="additionalData">Additional data to be passed to the profiler</param>
        public void AttachProfiler(TimeSpan attachTimeout, Guid profilerGuid, string profilerPath, byte[] additionalData = null)
        {
            IpcMessage request = CreateAttachProfilerMessage(attachTimeout, profilerGuid, profilerPath, additionalData);
            IpcMessage response = IpcClient.SendMessage(_endpoint, request);
            ValidateResponseMessage(response, nameof(AttachProfiler));

            // The call to set up the pipe and send the message operates on a different timeout than attachTimeout, which is for the runtime.
            // We should eventually have a configurable timeout for the message passing, potentially either separately from the 
            // runtime timeout or respect attachTimeout as one total duration.
        }

        internal async Task AttachProfilerAsync(TimeSpan attachTimeout, Guid profilerGuid, string profilerPath, byte[] additionalData, CancellationToken token)
        {
            IpcMessage request = CreateAttachProfilerMessage(attachTimeout, profilerGuid, profilerPath, additionalData);
            IpcMessage response = await IpcClient.SendMessageAsync(_endpoint, request, token).ConfigureAwait(false);
            ValidateResponseMessage(response, nameof(AttachProfilerAsync));
        }

        /// <summary>
        /// Set a profiler as the startup profiler. It is only valid to issue this command
        /// while the runtime is paused at startup.
        /// </summary>
        /// <param name="profilerGuid">Guid for the profiler to be attached</param>
        /// <param name="profilerPath">Path to the profiler to be attached</param>
        public void SetStartupProfiler(Guid profilerGuid, string profilerPath)
        {
            IpcMessage request = CreateSetStartupProfilerMessage(profilerGuid, profilerPath);
            IpcMessage response = IpcClient.SendMessage(_endpoint, request);
            ValidateResponseMessage(response, nameof(SetStartupProfiler), ValidateResponseOptions.InvalidArgumentIsRequiresSuspension);
        }

        internal async Task SetStartupProfilerAsync(Guid profilerGuid, string profilerPath, CancellationToken token)
        {
            IpcMessage request = CreateSetStartupProfilerMessage(profilerGuid, profilerPath);
            IpcMessage response = await IpcClient.SendMessageAsync(_endpoint, request, token).ConfigureAwait(false);
            ValidateResponseMessage(response, nameof(SetStartupProfilerAsync), ValidateResponseOptions.InvalidArgumentIsRequiresSuspension);
        }

        /// <summary>
        /// Tell the runtime to resume execution after being paused at startup.
        /// </summary>
        public void ResumeRuntime()
        {
            IpcMessage request = CreateResumeRuntimeMessage();
            IpcMessage response = IpcClient.SendMessage(_endpoint, request);
            ValidateResponseMessage(response, nameof(ResumeRuntime));
        }

        internal async Task ResumeRuntimeAsync(CancellationToken token)
        {
            IpcMessage request = CreateResumeRuntimeMessage();
            IpcMessage response = await IpcClient.SendMessageAsync(_endpoint, request, token).ConfigureAwait(false);
            ValidateResponseMessage(response, nameof(ResumeRuntimeAsync));
        }

        /// <summary>
        /// Set an environment variable in the target process.
        /// </summary>
        /// <param name="name">The name of the environment variable to set.</param>
        /// <param name="value">The value of the environment variable to set.</param>
        public void SetEnvironmentVariable(string name, string value)
        {
            IpcMessage request = CreateSetEnvironmentVariableMessage(name, value);
            IpcMessage response = IpcClient.SendMessage(_endpoint, request);
            ValidateResponseMessage(response, nameof(SetEnvironmentVariable));
        }

        internal async Task SetEnvironmentVariableAsync(string name, string value, CancellationToken token)
        {
            IpcMessage request = CreateSetEnvironmentVariableMessage(name, value);
            IpcMessage response = await IpcClient.SendMessageAsync(_endpoint, request, token).ConfigureAwait(false);
            ValidateResponseMessage(response, nameof(SetEnvironmentVariableAsync));
        }

        /// <summary>
        /// Gets all environement variables and their values from the target process.
        /// </summary>
        /// <returns>A dictionary containing all of the environment variables defined in the target process.</returns>
        public Dictionary<string, string> GetProcessEnvironment()
        {
            IpcMessage message = CreateProcessEnvironmentMessage();
            using IpcResponse response = IpcClient.SendMessageGetContinuation(_endpoint, message);
            ValidateResponseMessage(response.Message, nameof(GetProcessEnvironmentAsync));

            ProcessEnvironmentHelper helper = ProcessEnvironmentHelper.Parse(response.Message.Payload);
            return helper.ReadEnvironment(response.Continuation);
        }

        internal async Task<Dictionary<string, string>> GetProcessEnvironmentAsync(CancellationToken token)
        {
            IpcMessage message = CreateProcessEnvironmentMessage();
            using IpcResponse response = await IpcClient.SendMessageGetContinuationAsync(_endpoint, message, token).ConfigureAwait(false);
            ValidateResponseMessage(response.Message, nameof(GetProcessEnvironmentAsync));

            ProcessEnvironmentHelper helper = ProcessEnvironmentHelper.Parse(response.Message.Payload);
            return await helper.ReadEnvironmentAsync(response.Continuation, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Get all the active processes that can be attached to.
        /// </summary>
        /// <returns>
        /// IEnumerable of all the active process IDs.
        /// </returns>
        public static IEnumerable<int> GetPublishedProcesses()
        {
            static IEnumerable<int> GetAllPublishedProcesses()
            {
                foreach (var port in Directory.GetFiles(PidIpcEndpoint.IpcRootPath))
                {
                    var fileName = new FileInfo(port).Name;
                    var match = Regex.Match(fileName, PidIpcEndpoint.DiagnosticsPortPattern);
                    if (!match.Success) continue;
                    var group = match.Groups[1].Value;
                    if (!int.TryParse(group, NumberStyles.Integer, CultureInfo.InvariantCulture, out var processId))
                        continue;

                    yield return processId;
                }
            }

            return GetAllPublishedProcesses().Distinct();
        }

        internal ProcessInfo GetProcessInfo()
        {
            // Attempt to get ProcessInfo v2
            ProcessInfo processInfo = TryGetProcessInfo2();
            if (null != processInfo)
            {
                return processInfo;
            }

            IpcMessage request = CreateProcessInfoMessage();
            using IpcResponse response = IpcClient.SendMessageGetContinuation(_endpoint, request);
            return GetProcessInfoFromResponse(response, nameof(GetProcessInfo));
        }

        internal async Task<ProcessInfo> GetProcessInfoAsync(CancellationToken token)
        {
            // Attempt to get ProcessInfo v2
            ProcessInfo processInfo = await TryGetProcessInfo2Async(token);
            if (null != processInfo)
            {
                return processInfo;
            }

            IpcMessage request = CreateProcessInfoMessage();
            using IpcResponse response = await IpcClient.SendMessageGetContinuationAsync(_endpoint, request, token).ConfigureAwait(false);
            return GetProcessInfoFromResponse(response, nameof(GetProcessInfoAsync));
        }

        private ProcessInfo TryGetProcessInfo2()
        {
            IpcMessage request = CreateProcessInfo2Message();
            using IpcResponse response2 = IpcClient.SendMessageGetContinuation(_endpoint, request);
            return TryGetProcessInfo2FromResponse(response2, nameof(GetProcessInfo));
        }

        private async Task<ProcessInfo> TryGetProcessInfo2Async(CancellationToken token)
        {
            IpcMessage request = CreateProcessInfo2Message();
            using IpcResponse response2 = await IpcClient.SendMessageGetContinuationAsync(_endpoint, request, token).ConfigureAwait(false);
            return TryGetProcessInfo2FromResponse(response2, nameof(GetProcessInfoAsync));
        }

        private static byte[] SerializePayload<T>(T arg)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                SerializePayloadArgument(arg, writer);

                writer.Flush();
                return stream.ToArray();
            }
        }

        private static byte[] SerializePayload<T1, T2>(T1 arg1, T2 arg2)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                SerializePayloadArgument(arg1, writer);
                SerializePayloadArgument(arg2, writer);

                writer.Flush();
                return stream.ToArray();
            }
        }

        private static byte[] SerializePayload<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                SerializePayloadArgument(arg1, writer);
                SerializePayloadArgument(arg2, writer);
                SerializePayloadArgument(arg3, writer);

                writer.Flush();
                return stream.ToArray();
            }
        }

        private static byte[] SerializePayload<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                SerializePayloadArgument(arg1, writer);
                SerializePayloadArgument(arg2, writer);
                SerializePayloadArgument(arg3, writer);
                SerializePayloadArgument(arg4, writer);

                writer.Flush();
                return stream.ToArray();
            }
        }

        private static void SerializePayloadArgument<T>(T obj, BinaryWriter writer)
        {
            if (typeof(T) == typeof(string))
            {
                writer.WriteString((string)((object)obj));
            }
            else if (typeof(T) == typeof(int))
            {
                writer.Write((int)((object)obj));
            }
            else if (typeof(T) == typeof(uint))
            {
                writer.Write((uint)((object)obj));
            }
            else if (typeof(T) == typeof(bool))
            {
                bool bValue = (bool)((object)obj);
                uint uiValue = bValue ? (uint)1 : 0;
                writer.Write(uiValue);
            }
            else if (typeof(T) == typeof(Guid))
            {
                Guid guidVal = (Guid)((object)obj);
                writer.Write(guidVal.ToByteArray());
            }
            else if (typeof(T) == typeof(byte[]))
            {
                byte[] byteArray = (byte[])((object)obj);
                uint length = byteArray == null ? 0U : (uint)byteArray.Length;
                writer.Write(length);

                if (length > 0)
                {
                    writer.Write(byteArray);
                }
            }
            else
            {
                throw new ArgumentException($"Type {obj.GetType()} is not supported in SerializePayloadArgument, please add it.");
            }
        }

        private static IpcMessage CreateAttachProfilerMessage(TimeSpan attachTimeout, Guid profilerGuid, string profilerPath, byte[] additionalData)
        {
            if (profilerGuid == null || profilerGuid == Guid.Empty)
            {
                throw new ArgumentException($"{nameof(profilerGuid)} must be a valid Guid");
            }

            if (String.IsNullOrEmpty(profilerPath))
            {
                throw new ArgumentException($"{nameof(profilerPath)} must be non-null");
            }

            byte[] serializedConfiguration = SerializePayload((uint)attachTimeout.TotalSeconds, profilerGuid, profilerPath, additionalData);
            return new IpcMessage(DiagnosticsServerCommandSet.Profiler, (byte)ProfilerCommandId.AttachProfiler, serializedConfiguration);
        }

        private static IpcMessage CreateProcessEnvironmentMessage()
        {
            return new IpcMessage(DiagnosticsServerCommandSet.Process, (byte)ProcessCommandId.GetProcessEnvironment);
        }

        private static IpcMessage CreateProcessInfoMessage()
        {
            return new IpcMessage(DiagnosticsServerCommandSet.Process, (byte)ProcessCommandId.GetProcessInfo);
        }

        private static IpcMessage CreateProcessInfo2Message()
        {
            return new IpcMessage(DiagnosticsServerCommandSet.Process, (byte)ProcessCommandId.GetProcessInfo2);
        }

        private static IpcMessage CreateResumeRuntimeMessage()
        {
            return new IpcMessage(DiagnosticsServerCommandSet.Process, (byte)ProcessCommandId.ResumeRuntime);
        }

        private static IpcMessage CreateSetEnvironmentVariableMessage(string name, string value)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"{nameof(name)} must be non-null.");
            }

            byte[] serializedConfiguration = SerializePayload(name, value);
            return new IpcMessage(DiagnosticsServerCommandSet.Process, (byte)ProcessCommandId.SetEnvironmentVariable, serializedConfiguration);
        }

        private static IpcMessage CreateSetStartupProfilerMessage(Guid profilerGuid, string profilerPath)
        {
            if (profilerGuid == null || profilerGuid == Guid.Empty)
            {
                throw new ArgumentException($"{nameof(profilerGuid)} must be a valid Guid");
            }

            if (String.IsNullOrEmpty(profilerPath))
            {
                throw new ArgumentException($"{nameof(profilerPath)} must be non-null");
            }

            byte[] serializedConfiguration = SerializePayload(profilerGuid, profilerPath);
            return new IpcMessage(DiagnosticsServerCommandSet.Profiler, (byte)ProfilerCommandId.StartupProfiler, serializedConfiguration);
        }

        private static IpcMessage CreateWriteDumpMessage(DumpType dumpType, string dumpPath, bool logDumpGeneration)
        {
            if (string.IsNullOrEmpty(dumpPath))
                throw new ArgumentNullException($"{nameof(dumpPath)} required");

            byte[] payload = SerializePayload(dumpPath, (uint)dumpType, logDumpGeneration);
            return new IpcMessage(DiagnosticsServerCommandSet.Dump, (byte)DumpCommandId.GenerateCoreDump, payload);
        }

        private static IpcMessage CreateWriteDumpMessage(DumpCommandId command, DumpType dumpType, string dumpPath, WriteDumpFlags flags)
        {
            if (string.IsNullOrEmpty(dumpPath))
                throw new ArgumentNullException($"{nameof(dumpPath)} required");

            byte[] payload = SerializePayload(dumpPath, (uint)dumpType, (uint)flags);
            return new IpcMessage(DiagnosticsServerCommandSet.Dump, (byte)command, payload);
        }

        private static ProcessInfo GetProcessInfoFromResponse(IpcResponse response, string operationName)
        {
            ValidateResponseMessage(response.Message, operationName);

            return ProcessInfo.ParseV1(response.Message.Payload);
        }

        private static ProcessInfo TryGetProcessInfo2FromResponse(IpcResponse response, string operationName)
        {
            if (!ValidateResponseMessage(response.Message, operationName, ValidateResponseOptions.UnknownCommandReturnsFalse))
            {
                return null;
            }

            return ProcessInfo.ParseV2(response.Message.Payload);
        }

        internal static bool ValidateResponseMessage(IpcMessage responseMessage, string operationName, ValidateResponseOptions options = ValidateResponseOptions.None)
        {
            switch ((DiagnosticsServerResponseId)responseMessage.Header.CommandId)
            {
                case DiagnosticsServerResponseId.OK:
                    return true;

                case DiagnosticsServerResponseId.Error:
                    uint hr = BitConverter.ToUInt32(responseMessage.Payload, 0);
                    int index = sizeof(uint);
                    string message = null;
                    switch (hr)
                    {
                        case (uint)DiagnosticsIpcError.UnknownCommand:
                            if (options.HasFlag(ValidateResponseOptions.UnknownCommandReturnsFalse))
                            {
                                return false;
                            }
                            throw new UnsupportedCommandException($"{operationName} failed - Command is not supported.");

                        case (uint)DiagnosticsIpcError.ProfilerAlreadyActive:
                            throw new ProfilerAlreadyActiveException($"{operationName} failed - A profiler is already loaded.");

                        case (uint)DiagnosticsIpcError.InvalidArgument:
                            if (options.HasFlag(ValidateResponseOptions.InvalidArgumentIsRequiresSuspension))
                            {
                                throw new ServerErrorException($"{operationName} failed - The runtime must be suspended for this command.");
                            }
                            throw new UnsupportedCommandException($"{operationName} failed - Invalid command argument.");

                        case (uint)DiagnosticsIpcError.NotSupported:
                            message = $"{operationName} - Not supported by this runtime.";
                            break;

                        default:
                            break;
                    }
                    // Check if the command can return an error message and if the payload is big enough to contain the
                    // error code (uint) and the string length (uint).
                    if (options.HasFlag(ValidateResponseOptions.ErrorMessageReturned) && responseMessage.Payload.Length >= (sizeof(uint) * 2))
                    {
                        message = IpcHelpers.ReadString(responseMessage.Payload, ref index);
                    }
                    if (string.IsNullOrWhiteSpace(message))
                    {
                        message = $"{operationName} failed - HRESULT: 0x{hr:X8}.";
                    }
                    throw new ServerErrorException(message);

                default:
                    throw new ServerErrorException($"{operationName} failed - Server responded with unknown response.");
            }
        }

        [Flags]
        internal enum ValidateResponseOptions
        {
            None = 0x0,
            UnknownCommandReturnsFalse = 0x1,
            InvalidArgumentIsRequiresSuspension = 0x2,
            ErrorMessageReturned = 0x4,
        }
    }
}
