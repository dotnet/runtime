// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Diagnostics.NETCore.Client
{
    internal enum EventPipeSerializationFormat
    {
        NetPerf,
        NetTrace
    }

    // Session type as encoded on the CollectTracing5+ wire. This is NOT the runtime's internal
    // EventPipeSessionType; the IPC collect command maps wire 0 => IpcStream, 1 => UserEvents.
    internal enum EventPipeSessionType : uint
    {
        IpcStream = 0,

        // This client does not support starting/tracing a user_events session (that path needs an
        // out-of-band file descriptor, see the IPC protocol docs); the value exists only for wire
        // correctness when describing the CollectTracing5+ session-type field.
        UserEvents = 1
    }

    /// <summary>
    /// Controls how the runtime's per-session event buffer behaves when it fills faster than the
    /// session is drained.
    /// </summary>
    public enum EventPipeBufferingMode
    {
        /// <summary>
        /// The runtime default: a circular buffer that drops events when it overflows (lossy).
        /// </summary>
        Drop = 0,

        /// <summary>
        /// Non-lossy: producers block until the reader frees buffer capacity rather than dropping
        /// events. Available on .NET 11+; useful for collections that must be complete (e.g. a heap
        /// snapshot on a large heap).
        /// </summary>
        Block = 1
    }

    public sealed class EventPipeSessionConfiguration
    {
        /// <summary>
        /// Creates a new configuration object for the EventPipeSession.
        /// For details, see the documentation of each property of this object.
        /// </summary>
        /// <param name="providers">An IEnumerable containing the list of Providers to turn on.</param>
        /// <param name="circularBufferSizeMB">The size of the runtime's buffer for collecting events in MB</param>
        /// <param name="requestRundown">If true, request rundown events from the runtime.</param>
        /// <param name="requestStackwalk">If true, record a stacktrace for every emitted event.</param>
        public EventPipeSessionConfiguration(
            IEnumerable<EventPipeProvider> providers,
            int circularBufferSizeMB = 256,
            bool requestRundown = true,
            bool requestStackwalk = true) : this(circularBufferSizeMB, EventPipeSerializationFormat.NetTrace, providers, requestStackwalk, (requestRundown ? EventPipeSession.DefaultRundownKeyword : 0))
        {}

        /// <summary>
        /// Creates a new configuration object for the EventPipeSession.
        /// For details, see the documentation of each property of this object.
        /// </summary>
        /// <param name="providers">An IEnumerable containing the list of Providers to turn on.</param>
        /// <param name="circularBufferSizeMB">The size of the runtime's buffer for collecting events in MB</param>
        /// <param name="rundownKeyword">The keyword for rundown events.</param>
        /// <param name="requestStackwalk">If true, record a stacktrace for every emitted event.</param>
        public EventPipeSessionConfiguration(
            IEnumerable<EventPipeProvider> providers,
            int circularBufferSizeMB,
            long rundownKeyword,
            bool requestStackwalk = true) : this(circularBufferSizeMB, EventPipeSerializationFormat.NetTrace, providers, requestStackwalk, rundownKeyword)
        {}

        /// <summary>
        /// Creates a new configuration object for the EventPipeSession with a specific rundown keyword and
        /// buffering mode. For details, see the documentation of each property of this object.
        /// </summary>
        /// <param name="providers">An IEnumerable containing the list of Providers to turn on.</param>
        /// <param name="circularBufferSizeMB">The size of the runtime's buffer for collecting events in MB</param>
        /// <param name="rundownKeyword">The keyword for rundown events.</param>
        /// <param name="requestStackwalk">If true, record a stacktrace for every emitted event.</param>
        /// <param name="bufferingMode">The session buffering mode; Block requests non-lossy collection (CollectTracing6, .NET 11+).</param>
        public EventPipeSessionConfiguration(
            IEnumerable<EventPipeProvider> providers,
            int circularBufferSizeMB,
            long rundownKeyword,
            bool requestStackwalk,
            EventPipeBufferingMode bufferingMode) : this(circularBufferSizeMB, EventPipeSerializationFormat.NetTrace, providers, requestStackwalk, rundownKeyword, bufferingMode)
        {}

        private EventPipeSessionConfiguration(
            int circularBufferSizeMB,
            EventPipeSerializationFormat format,
            IEnumerable<EventPipeProvider> providers,
            bool requestStackwalk,
            long rundownKeyword,
            EventPipeBufferingMode bufferingMode = EventPipeBufferingMode.Drop)
        {
            if (circularBufferSizeMB == 0)
            {
                throw new ArgumentException($"Buffer size cannot be zero.");
            }

            if (format is not EventPipeSerializationFormat.NetPerf and not EventPipeSerializationFormat.NetTrace)
            {
                throw new ArgumentException("Unrecognized format");
            }

            if (providers is null)
            {
                throw new ArgumentNullException(nameof(providers));
            };

            _providers = new List<EventPipeProvider>(providers);
            if (_providers.Count == 0)
            {
                throw new ArgumentException("At least one provider must be specified.");
            };

            CircularBufferSizeInMB = circularBufferSizeMB;
            Format = format;
            RequestStackwalk = requestStackwalk;
            RundownKeyword = rundownKeyword;
            BufferingMode = bufferingMode;
        }

        /// <summary>
        /// If true, request rundown events from the runtime.
        /// <list type="bullet">
        /// <item>Rundown events are needed to correctly decode the stacktrace information for dynamically generated methods.</item>
        /// <item>Rundown happens at the end of the session. It increases the time needed to finish the session and, for large applications, may have important impact on the final trace file size.</item>
        /// <item>Consider to set this parameter to false if you don't need stacktrace information or if you're analyzing events on the fly.</item>
        /// </list>
        /// </summary>
        public bool RequestRundown => this.RundownKeyword != 0;

        /// <summary>
        /// The size of the runtime's buffer for collecting events in MB.
        /// If the buffer size is too small to accommodate all in-flight events some events may be lost.
        /// </summary>
        public int CircularBufferSizeInMB { get; }

        /// <summary>
        /// If true, record a stacktrace for every emitted event.
        /// <list type="bullet">
        /// <item>The support of this parameter only comes with NET 9. Before, the stackwalk is always enabled and if this property is set to false the connection attempt will fail.</item>
        /// <item>Disabling the stackwalk makes event collection overhead considerably less</item>
        /// <item>Note that some events may choose to omit the stacktrace regardless of this parameter, specifically the events emitted from the native runtime code.</item>
        /// <item>If the stacktrace collection is disabled application-wide (using the env variable <c>DOTNET_EventPipeEnableStackwalk</c>) this parameter is ignored.</item>
        /// </list>
        /// </summary>
        public bool RequestStackwalk { get; }

        /// <summary>
        /// The keywords enabled for the rundown provider.
        /// </summary>
        public long RundownKeyword { get; internal set; }

        /// <summary>
        /// Buffering mode for the session. <see cref="EventPipeBufferingMode.Block"/> requests non-lossy
        /// collection (sent as CollectTracing6); the default keeps the runtime's lossy circular buffer.
        /// </summary>
        public EventPipeBufferingMode BufferingMode { get; }

        /// <summary>
        /// Providers to enable for this session.
        /// </summary>
        public IReadOnlyCollection<EventPipeProvider> Providers => _providers.AsReadOnly();

        private readonly List<EventPipeProvider> _providers;

        internal EventPipeSerializationFormat Format { get; }
    }

    internal static class EventPipeSessionConfigurationExtensions
    {
        public static byte[] SerializeV2(this EventPipeSessionConfiguration config)
        {
            byte[] serializedData = null;
            using (MemoryStream stream = new())
            using (BinaryWriter writer = new(stream))
            {
                writer.Write(config.CircularBufferSizeInMB);
                writer.Write((uint)config.Format);
                writer.Write(config.RequestRundown);

                SerializeProviders(config, writer);

                writer.Flush();
                serializedData = stream.ToArray();
            }

            return serializedData;
        }

        public static byte[] SerializeV3(this EventPipeSessionConfiguration config)
        {
            byte[] serializedData = null;
            using (MemoryStream stream = new())
            using (BinaryWriter writer = new(stream))
            {
                writer.Write(config.CircularBufferSizeInMB);
                writer.Write((uint)config.Format);
                writer.Write(config.RequestRundown);
                writer.Write(config.RequestStackwalk);

                SerializeProviders(config, writer);

                writer.Flush();
                serializedData = stream.ToArray();
            }

            return serializedData;
        }

        public static byte[] SerializeV4(this EventPipeSessionConfiguration config)
        {
            byte[] serializedData = null;
            using (MemoryStream stream = new())
            using (BinaryWriter writer = new(stream))
            {
                writer.Write(config.CircularBufferSizeInMB);
                writer.Write((uint)config.Format);
                writer.Write(config.RundownKeyword);
                writer.Write(config.RequestStackwalk);

                SerializeProviders(config, writer);

                writer.Flush();
                serializedData = stream.ToArray();
            }

            return serializedData;
        }

        public static byte[] SerializeV5(this EventPipeSessionConfiguration config)
        {
            byte[] serializedData = null;
            using (MemoryStream stream = new())
            using (BinaryWriter writer = new(stream))
            {
                // This client only creates streaming (IpcStream) sessions.
                writer.Write((uint)EventPipeSessionType.IpcStream);
                writer.Write(config.CircularBufferSizeInMB);
                writer.Write((uint)config.Format);
                writer.Write(config.RundownKeyword);
                writer.Write(config.RequestStackwalk);

                SerializeProvidersV5(config, writer);

                writer.Flush();
                serializedData = stream.ToArray();
            }

            return serializedData;
        }

        public static byte[] SerializeV6(this EventPipeSessionConfiguration config)
        {
            byte[] serializedData = null;
            using (MemoryStream stream = new())
            using (BinaryWriter writer = new(stream))
            {
                writer.Write((uint)EventPipeSessionType.IpcStream);
                writer.Write(config.CircularBufferSizeInMB);
                writer.Write((uint)config.Format);
                writer.Write(config.RundownKeyword);
                writer.Write(config.RequestStackwalk);

                SerializeProvidersV5(config, writer);

                writer.Write((uint)config.BufferingMode);

                writer.Flush();
                serializedData = stream.ToArray();
            }

            return serializedData;
        }

        private static void SerializeProviders(EventPipeSessionConfiguration config, BinaryWriter writer)
        {
            writer.Write(config.Providers.Count);
            foreach (EventPipeProvider provider in config.Providers)
            {
                writer.Write(unchecked((ulong)provider.Keywords));
                writer.Write((uint)provider.EventLevel);
                writer.WriteString(provider.Name);
                writer.WriteString(provider.GetArgumentString());
            }
        }

        // CollectTracing5+ per-provider layout: the V4 fields plus a trailing event filter.
        private static void SerializeProvidersV5(EventPipeSessionConfiguration config, BinaryWriter writer)
        {
            writer.Write(config.Providers.Count);
            foreach (EventPipeProvider provider in config.Providers)
            {
                writer.Write(unchecked((ulong)provider.Keywords));
                writer.Write((uint)provider.EventLevel);
                writer.WriteString(provider.Name);
                writer.WriteString(provider.GetArgumentString());
                SerializeEventFilter(writer, provider.EventFilter);
            }
        }

        // Serializes a provider's CollectTracing5+ event filter. A null filter (no explicit Event ID
        // filter) is written as a disabled, empty filter (enable=false, count=0), which the runtime
        // interprets as "allow all events".
        private static void SerializeEventFilter(BinaryWriter writer, EventPipeProviderEventFilter filter)
        {
            if (filter == null)
            {
                writer.Write(false);
                writer.Write(0u);
                return;
            }

            writer.Write(filter.Enable);
            writer.Write((uint)filter.EventIds.Count);
            foreach (uint eventId in filter.EventIds)
            {
                writer.Write(eventId);
            }
        }
    }
}
