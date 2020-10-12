// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Quic.Implementations.Managed.Internal.Frames;
using System.Net.Quic.Implementations.Managed.Internal.Recovery;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace System.Net.Quic.Implementations.Managed.Internal.Tracing
{
    /// <summary>
    ///     Class for tracing QuicConnection events
    /// </summary>
    internal class QuicTrace : IDisposable
    {
        private static readonly Dictionary<PacketType, byte[]> _packetTypeNames = new Dictionary<PacketType, byte[]>
        {
            [PacketType.Initial] = GetBytesHelper("initial"),
            [PacketType.Handshake] = GetBytesHelper("handshake"),
            [PacketType.OneRtt] = GetBytesHelper("onertt"),
            [PacketType.ZeroRtt] = GetBytesHelper("zerortt"),
            [PacketType.Retry] = GetBytesHelper("retry"),
            [PacketType.VersionNegotiation] = GetBytesHelper("version_negotiation")
            // TODO: Stateless reset?
        };

        private static readonly Dictionary<PacketLossTrigger, byte[]> _packetLossTriggerNames =
            new Dictionary<PacketLossTrigger, byte[]>
            {
                [PacketLossTrigger.ReorderingThreshold] = GetBytesHelper("reordering_threshold"),
                [PacketLossTrigger.TimeThreshold] = GetBytesHelper("time_threshold"),
                [PacketLossTrigger.PtoExpired] = GetBytesHelper("pto_expired")
            };

        private static readonly Dictionary<CongestionState, byte[]> _congestionStateNames =
            new Dictionary<CongestionState, byte[]>
            {
                [CongestionState.SlowStart] = GetBytesHelper("slow_start"),
                [CongestionState.CongestionAvoidance] = GetBytesHelper("congestion_avoidance"),
                [CongestionState.ApplicationLimited] = GetBytesHelper("application_limited"),
                [CongestionState.Recovery] = GetBytesHelper("recovery")
            };

        private static readonly Dictionary<KeyUpdateTrigger, byte[]> _keyUpdateTriggerNames =
            new Dictionary<KeyUpdateTrigger, byte[]>
            {
                [KeyUpdateTrigger.Tls] = GetBytesHelper("tls"),
                [KeyUpdateTrigger.LocalUpdate] = GetBytesHelper("local_update"),
                [KeyUpdateTrigger.RemoteUpdate] = GetBytesHelper("remote_update"),
            };

        private static byte[] GetBytesHelper(string s) => Encoding.UTF8.GetBytes(s);

        private static class Category
        {
            internal static readonly byte[] Transport = GetBytesHelper("transport");
            internal static readonly byte[] Security = GetBytesHelper("security");
            internal static readonly byte[] Recovery = GetBytesHelper("recovery");
        }

        private static class Event
        {
            internal static readonly byte[] packet_sent = GetBytesHelper("packet_sent");
            internal static readonly byte[] packet_received = GetBytesHelper("packet_received");
            internal static readonly byte[] parameters_set = GetBytesHelper("parameters_set");
            internal static readonly byte[] key_updated = GetBytesHelper("key_updated");
            internal static readonly byte[] datagrams_received = GetBytesHelper("datagrams_received");
            internal static readonly byte[] datagrams_sent = GetBytesHelper("datagrams_sent");
            internal static readonly byte[] datagrams_dropped = GetBytesHelper("datagrams_dropped");
            internal static readonly byte[] packet_dropped = GetBytesHelper("packet_dropped");
            internal static readonly byte[] packet_lost = GetBytesHelper("packet_lost");
            internal static readonly byte[] metrics_updated = GetBytesHelper("metrics_updated");
            internal static readonly byte[] congestion_state_updated = GetBytesHelper("congestion_state_updated");
        }

        private static class Field
        {
            internal static readonly byte[] qlog_version = GetBytesHelper("qlog_version");
            internal static readonly byte[] title = GetBytesHelper("title");
            internal static readonly byte[] traces = GetBytesHelper("traces");
            internal static readonly byte[] vantage_point = GetBytesHelper("vantage_point");
            internal static readonly byte[] type = GetBytesHelper("type");
            internal static readonly byte[] configuration = GetBytesHelper("configuration");
            internal static readonly byte[] reference_time = GetBytesHelper("reference_time");
            internal static readonly byte[] common_fields = GetBytesHelper("common_fields");
            internal static readonly byte[] protocol_type = GetBytesHelper("protocol_type");
            internal static readonly byte[] group_id = GetBytesHelper("group_id");
            internal static readonly byte[] event_fields = GetBytesHelper("event_fields");
            internal static readonly byte[] delta_time = GetBytesHelper("delta_time");
            internal static readonly byte[] time = GetBytesHelper("time");
            internal static readonly byte[] relative_time = GetBytesHelper("relative_time");
            internal static readonly byte[] category = GetBytesHelper("category");
            internal static readonly byte[] @event = GetBytesHelper("event");
            internal static readonly byte[] data = GetBytesHelper("data");
            internal static readonly byte[] events = GetBytesHelper("events");

            internal static readonly byte[] original_destination_connection_id =
                GetBytesHelper("original_destination_connection_id");

            internal static readonly byte[] original_source_connection_id =
                GetBytesHelper("original_source_connection_id");

            internal static readonly byte[] retry_source_connection_id = GetBytesHelper("retry_source_connection_id");
            internal static readonly byte[] stateless_reset_token = GetBytesHelper("stateless_reset_token");
            internal static readonly byte[] disable_active_migration = GetBytesHelper("disable_active_migration");
            internal static readonly byte[] max_idle_timeout = GetBytesHelper("max_idle_timeout");
            internal static readonly byte[] max_udp_payload_size = GetBytesHelper("max_udp_payload_size");
            internal static readonly byte[] ack_delay_exponent = GetBytesHelper("ack_delay_exponent");
            internal static readonly byte[] max_ack_delay = GetBytesHelper("max_ack_delay");
            internal static readonly byte[] active_connection_id_limit = GetBytesHelper("active_connection_id_limit");
            internal static readonly byte[] initial_max_data = GetBytesHelper("initial_max_data");

            internal static readonly byte[] initial_stream_data_bidi_local =
                GetBytesHelper("initial_stream_data_bidi_local");

            internal static readonly byte[] initial_stream_data_bidi_remote =
                GetBytesHelper("initial_stream_data_bidi_remote");

            internal static readonly byte[] initial_stream_data_uni = GetBytesHelper("initial_stream_data_uni");
            internal static readonly byte[] initial_streams_bidi = GetBytesHelper("initial_streams_bidi");
            internal static readonly byte[] initial_streams_uni = GetBytesHelper("initial_streams_uni");
            internal static readonly byte[] key_type = GetBytesHelper("key_type");
            internal static readonly byte[] @new = GetBytesHelper("new");
            internal static readonly byte[] byte_length = GetBytesHelper("byte_length");
            internal static readonly byte[] header = GetBytesHelper("header");
            internal static readonly byte[] scid = GetBytesHelper("scid");
            internal static readonly byte[] dcid = GetBytesHelper("dcid");
            internal static readonly byte[] packet_size = GetBytesHelper("packet_size");
            internal static readonly byte[] payload_length = GetBytesHelper("payload_length");
            internal static readonly byte[] frames = GetBytesHelper("frames");
            internal static readonly byte[] ack_delay = GetBytesHelper("ack_delay");
            internal static readonly byte[] acked_ranges = GetBytesHelper("acked_ranges");
            internal static readonly byte[] ect1 = GetBytesHelper("ect1");
            internal static readonly byte[] ect0 = GetBytesHelper("ect0");
            internal static readonly byte[] ce = GetBytesHelper("ce");
            internal static readonly byte[] stream_id = GetBytesHelper("stream_id");
            internal static readonly byte[] offset = GetBytesHelper("offset");
            internal static readonly byte[] length = GetBytesHelper("length");
            internal static readonly byte[] fin = GetBytesHelper("fin");
            internal static readonly byte[] raw_frame_type = GetBytesHelper("raw_frame_type");
            internal static readonly byte[] raw_length = GetBytesHelper("raw_length");
            internal static readonly byte[] frame_type = GetBytesHelper("frame_type");
            internal static readonly byte[] packet_type = GetBytesHelper("packet_type");
            internal static readonly byte[] packet_number = GetBytesHelper("packet_number");
            internal static readonly byte[] trigger = GetBytesHelper("trigger");
            internal static readonly byte[] min_rtt = GetBytesHelper("min_rtt");
            internal static readonly byte[] smoothed_rtt = GetBytesHelper("smoothed_rtt");
            internal static readonly byte[] latest_rtt = GetBytesHelper("latest_rtt");
            internal static readonly byte[] rtt_variance = GetBytesHelper("rtt_variance");
            internal static readonly byte[] pto_count = GetBytesHelper("pto_count");
            internal static readonly byte[] congestion_window = GetBytesHelper("congestion_window");
            internal static readonly byte[] bytes_in_flight = GetBytesHelper("bytes_in_flight");
            internal static readonly byte[] ssthresh = GetBytesHelper("ssthresh");
            internal static readonly byte[] packets_in_flight = GetBytesHelper("packets_in_flight");
            internal static readonly byte[] pacing_rate = GetBytesHelper("pacing_rate");
            internal static readonly byte[] reordering_threshold = GetBytesHelper("reordering_threshold");
            internal static readonly byte[] time_threshold = GetBytesHelper("time_threshold");
            internal static readonly byte[] timer_granularity = GetBytesHelper("timer_granularity");
            internal static readonly byte[] initial_rtt = GetBytesHelper("initial_rtt");
            internal static readonly byte[] max_datagram_size = GetBytesHelper("max_datagram_size");
            internal static readonly byte[] initial_congestion_window = GetBytesHelper("initial_congestion_window");
            internal static readonly byte[] minimum_congestion_window = GetBytesHelper("minimum_congestion_window");
            internal static readonly byte[] loss_reduction_factor = GetBytesHelper("loss_reduction_factor");
            internal static readonly byte[] time_units = GetBytesHelper("time_units");

            internal static readonly byte[] persistent_congestion_threshold =
                GetBytesHelper("persistent_congestion_threshold");
        }

        private static class Frame
        {
            internal static readonly byte[] Padding = GetBytesHelper("padding");
            internal static readonly byte[] Ping = GetBytesHelper("ping");
            internal static readonly byte[] Ack = GetBytesHelper("ack");
            internal static readonly byte[] ResetStream = GetBytesHelper("reset_stream");
            internal static readonly byte[] StopSending = GetBytesHelper("stop_sending");
            internal static readonly byte[] Crypto = GetBytesHelper("crypto");
            internal static readonly byte[] NewToken = GetBytesHelper("new_token");
            internal static readonly byte[] Stream = GetBytesHelper("stream");
            internal static readonly byte[] MaxData = GetBytesHelper("max_data");
            internal static readonly byte[] MaxStreamData = GetBytesHelper("max_stream_data");
            internal static readonly byte[] MaxStreams = GetBytesHelper("max_streams");
            internal static readonly byte[] DataBlocked = GetBytesHelper("data_blocked");
            internal static readonly byte[] StreamDataBlocked = GetBytesHelper("stream_data_blocked");
            internal static readonly byte[] StreamsBlocked = GetBytesHelper("streams_blocked");
            internal static readonly byte[] NewConnectionId = GetBytesHelper("new_connection_id");
            internal static readonly byte[] RetireConnectionId = GetBytesHelper("retire_connection_id");
            internal static readonly byte[] PathChallenge = GetBytesHelper("path_challenge");
            internal static readonly byte[] PathResponse = GetBytesHelper("path_response");
            internal static readonly byte[] ConnectionClose = GetBytesHelper("connection_close");
            internal static readonly byte[] HandshakeDone = GetBytesHelper("handshake_done");
            internal static readonly byte[] Unknown = GetBytesHelper("unknown");
        }

        private static class KeyType
        {
            internal static readonly byte[] server_initial_secret = GetBytesHelper("server_initial_secret");
            internal static readonly byte[] client_initial_secret = GetBytesHelper("client_initial_secret");
            internal static readonly byte[] server_handshake_secret = GetBytesHelper("server_handshake_secret");
            internal static readonly byte[] client_handshake_secret = GetBytesHelper("client_handshake_secret");
            internal static readonly byte[] server_1rtt_secret = GetBytesHelper("server_1rtt_secret");
            internal static readonly byte[] client_1rtt_secret = GetBytesHelper("client_1rtt_secret");
        }

        // member is explicitly initialized to its default value
        #pragma warning disable CA1805
        // switches for verbosity logging
        // TODO-RZ: it might be good idea to make this configurable somehow
        private bool _logDatagrams = false;
        private bool _logRecovery = true;
        private bool _logSecurity = true;
        private bool _logTransport = true;
        #pragma warning restore CA1805

        private readonly Utf8JsonWriter _writer;
        private readonly Stream _stream;
        private readonly byte[] _groupId;

        private readonly bool _isServer;

        private bool _inEvent;
        private bool _disposed;

        internal QuicTrace(Stream stream, byte[] groupId, bool isServer)
        {
            _stream = stream;
            _groupId = groupId;
            _isServer = isServer;

            _writer = new Utf8JsonWriter(_stream);

            WriteHeader();
        }

        private void WriteHeader()
        {
            // We want to produce something like this:
            //
            // {
            //   "qlog_version": "draft-01",
            //   "title": "quant 0.0.29/58d01df qlog",
            //   "traces": [
            //     {
            //       "vantage_point": {
            //         "type": <server|client>
            //       },
            //       "configuration": {
            //         "time_units": "us"
            //       },
            //       "common_fields": {
            //         "group_id": <Original connection id>,
            //         "protocol_type": "QUIC_HTTP3"
            //       },
            //       "event_fields": [
            //         "time",
            //         "category",
            //         "event",
            //         "trigger",
            //         "data"
            //       ],
            //       "events": [ <-- end here, events will be written using individual methods

            _writer.WriteStartObject();
            _writer.WriteString(Field.qlog_version, "draft-01");
            _writer.WriteString(Field.title, "Managed .NET log: " + DateTime.Now.ToShortTimeString());
            _writer.WriteStartArray(Field.traces);
            _writer.WriteStartObject();

            _writer.WriteStartObject(Field.vantage_point);
            _writer.WriteString(Field.type, _isServer ? "server" : "client");
            _writer.WriteEndObject();

            _writer.WriteStartObject(Field.configuration);
            _writer.WriteString(Field.time_units, "us");
            _writer.WriteEndObject();

            _writer.WriteStartObject(Field.common_fields);
            _writer.WriteString(Field.protocol_type, "QUIC_HTTP3");
            _writer.WriteHexBytesString(Field.group_id, _groupId);
            _writer.WriteEndObject();

            _writer.WriteStartArray(Field.event_fields);
            _writer.WriteStringValue(Field.time);
            _writer.WriteStringValue(Field.category);
            _writer.WriteStringValue(Field.@event);
            _writer.WriteStringValue(Field.data);
            _writer.WriteEndArray();

            _writer.WriteStartArray(Field.events);
        }

        private void WriteEventProlog(ReadOnlySpan<byte> category, ReadOnlySpan<byte> eventName)
        {
            Debug.Assert(!_inEvent);
            _inEvent = true; // for debug purposes

            // start the event array
            _writer.WriteStartArray();

            _writer.WriteNumberValue(Timestamp.GetMicrosecondsDouble(Timestamp.Now));
            _writer.WriteStringValue(category);
            _writer.WriteStringValue(eventName);

            // start the data object
            _writer.WriteStartObject();
        }

        private void WriteEventEpilog()
        {
            Debug.Assert(_inEvent);
            // end the data object
            _writer.WriteEndObject();
            // end the event array
            _writer.WriteEndArray();

            _inEvent = false;
        }

        internal void OnTransportParametersSet(TransportParameters parameters)
        {
            WriteEventProlog(Category.Transport, Event.parameters_set);

            // _Writer.WriteBoolean(Field.original_destination_connection_id, ??);
            // _Writer.WriteBoolean(Field.original_source_connection_id, ??);
            // _Writer.WriteBoolean(Field.retry_source_connection_id, ??);
            if (parameters.StatelessResetToken.HasValue)
            {
                var token = parameters.StatelessResetToken.Value;
                _writer.WriteHexBytesString(Field.stateless_reset_token,
                    MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref token, 1)));
                _writer.WriteBoolean(Field.disable_active_migration, parameters.DisableActiveMigration);
            }

            _writer.WriteNumber(Field.max_idle_timeout, parameters.MaxIdleTimeout);
            _writer.WriteNumber(Field.max_udp_payload_size, parameters.MaxPacketSize);
            _writer.WriteNumber(Field.ack_delay_exponent, parameters.AckDelayExponent);
            _writer.WriteNumber(Field.max_ack_delay, parameters.MaxAckDelay);
            _writer.WriteNumber(Field.active_connection_id_limit, parameters.ActiveConnectionIdLimit);

            _writer.WriteNumber(Field.initial_max_data, parameters.InitialMaxData);
            _writer.WriteNumber(Field.initial_stream_data_bidi_local, parameters.InitialMaxStreamDataBidiLocal);
            _writer.WriteNumber(Field.initial_stream_data_bidi_remote, parameters.InitialMaxStreamDataBidiRemote);
            _writer.WriteNumber(Field.initial_stream_data_uni, parameters.InitialMaxStreamDataUni);
            _writer.WriteNumber(Field.initial_streams_bidi, parameters.InitialMaxStreamsBidi);
            _writer.WriteNumber(Field.initial_streams_uni, parameters.InitialMaxStreamsUni);

            // TODO: perferred_address

            WriteEventEpilog();
        }

        internal void OnKeyUpdated(ReadOnlySpan<byte> secret, EncryptionLevel level, bool isServer,
            KeyUpdateTrigger trigger, int? generation)
        {
            if (!_logSecurity)
                return;

            WriteEventProlog(Category.Security, Event.key_updated);

            var keyType = (level, isServer) switch
            {
                (EncryptionLevel.Initial, true) => KeyType.server_initial_secret,
                (EncryptionLevel.Initial, false) => KeyType.client_initial_secret,
                (EncryptionLevel.Handshake, true) => KeyType.server_handshake_secret,
                (EncryptionLevel.Handshake, false) => KeyType.client_handshake_secret,
                (EncryptionLevel.Application, true) => KeyType.server_1rtt_secret,
                (EncryptionLevel.Application, false) => KeyType.client_1rtt_secret,
                _ => Array.Empty<byte>()
            };

            _writer.WriteString(Field.key_type, keyType);
            _writer.WriteHexBytesString(Field.@new, secret);
            _writer.WriteString(Field.trigger, _keyUpdateTriggerNames[trigger]);

            WriteEventEpilog();
        }

        internal void OnDatagramReceived(int length)
        {
            if (!_logDatagrams)
                return;

            WriteEventProlog(Category.Transport, Event.datagrams_received);

            _writer.WriteNumber(Field.byte_length, length);

            WriteEventEpilog();
        }

        internal void OnDatagramSent(int length)
        {
            if (!_logDatagrams)
                return;

            WriteEventProlog(Category.Transport, Event.datagrams_sent);

            _writer.WriteNumber(Field.byte_length, length);

            WriteEventEpilog();
        }

        internal void OnDatagramDropped(int length)
        {
            if (!_logDatagrams)
                return;

            WriteEventProlog(Category.Transport, Event.datagrams_dropped);

            _writer.WriteNumber(Field.byte_length, length);

            WriteEventEpilog();
        }

        internal void OnStreamStateUpdated(int length)
        {
            Debug.Assert(!_inEvent);
        }

        internal void OnPacketReceiveStart(ReadOnlySpan<byte> scid, ReadOnlySpan<byte> dcid, PacketType packetType,
            long packetNumber, long payloadLength, long packetSize)
        {
            if (!_logTransport)
                return;

            WriteEventProlog(Category.Transport, Event.packet_received);
            _writer.WriteString(Field.packet_type, _packetTypeNames[packetType]);

            WritePacketHeader(scid, dcid, packetType, packetNumber, payloadLength, packetSize);

            _writer.WriteStartArray(Field.frames);
        }

        internal void OnPacketReceiveEnd()
        {
            if (!_logTransport)
                return;

            // end frames array
            _writer.WriteEndArray();

            WriteEventEpilog();
        }

        internal void OnPacketSendStart()
        {
            if (!_logTransport)
                return;

            WriteEventProlog(Category.Transport, Event.packet_sent);

            _writer.WriteStartArray(Field.frames);
        }

        internal void OnPacketSendEnd(ReadOnlySpan<byte> scid, ReadOnlySpan<byte> dcid,
            PacketType packetType, long packetNumber, long payloadLength, long packetSize)
        {
            if (!_logTransport)
                return;

            // end frames array
            _writer.WriteEndArray();

            // write header now since we finally know all the sizes
            _writer.WriteString(Field.packet_type, _packetTypeNames[packetType]);
            WritePacketHeader(scid, dcid, packetType, packetNumber, payloadLength, packetSize);

            WriteEventEpilog();
        }

        private void WritePacketHeader(ReadOnlySpan<byte> scid, ReadOnlySpan<byte> dcid,
            PacketType packetType, long packetNumber, long payloadLength, long packetSize)
        {
            _writer.WriteStartObject(Field.header);
            if (packetType != PacketType.OneRtt)
            {
                // no need to log connection ids for 1-rtt packets (they can be inferred)
                _writer.WriteHexBytesString(Field.scid, scid);
                _writer.WriteHexBytesString(Field.dcid, dcid);
            }

            _writer.WriteNumber(Field.packet_number, packetNumber);
            _writer.WriteNumber(Field.packet_size, packetSize);
            _writer.WriteNumber(Field.payload_length, payloadLength);

            _writer.WriteEndObject();
        }

        internal void OnPacketDropped(PacketType? type, int packetSize)
        {
            if (!_logTransport)
                return;

            WriteEventProlog(Category.Transport, Event.packet_dropped);

            if (type != null)
                _writer.WriteString(Field.packet_type, _packetTypeNames[type.Value]);
            _writer.WriteNumber(Field.raw_length, packetSize);

            // TODO: trigger?

            WriteEventEpilog();
        }

        internal void OnPaddingFrame(int length)
        {
            if (!_logTransport)
                return;

            WriteFrameProlog(Frame.Padding);
            WriteFrameEpilog();
        }

        internal void OnPingFrame()
        {
            if (!_logTransport)
                return;

            WriteFrameProlog(Frame.Ping);
            WriteFrameEpilog();
        }

        internal void OnAckFrame(in AckFrame frame, long ackDelayMicroseconds)
        {
            if (!_logTransport)
                return;

            WriteFrameProlog(Frame.Ack);

            _writer.WriteNumber(Field.ack_delay, ackDelayMicroseconds / 1000.0);

            Span<RangeSet.Range> ranges = frame.AckRangeCount < 16
                ? stackalloc RangeSet.Range[(int)frame.AckRangeCount + 1]
                : new RangeSet.Range[frame.AckRangeCount + 1];

            if (frame.TryDecodeAckRanges(ranges))
            {
                _writer.WriteStartArray(Field.acked_ranges);

                for (int i = ranges.Length - 1; i >= 0; i--)
                {
                    _writer.WriteStartArray();
                    _writer.WriteNumberValue(ranges[i].Start);
                    _writer.WriteNumberValue(ranges[i].End);
                    _writer.WriteEndArray();
                }

                _writer.WriteEndArray();
            }

            if (frame.HasEcnCounts)
            {
                _writer.WriteNumber(Field.ect1, frame.Ect1Count);
                _writer.WriteNumber(Field.ect0, frame.Ect0Count);
                _writer.WriteNumber(Field.ce, frame.CeCount);
            }

            WriteFrameEpilog();
        }

        internal void OnResetStreamFrame(in ResetStreamFrame frame)
        {
            if (!_logTransport)
                return;

            WriteFrameProlog(Frame.ResetStream);
            WriteFrameEpilog();
        }

        internal void OnStopSendingFrame(in StopSendingFrame frame)
        {
            if (!_logTransport)
                return;

            WriteFrameProlog(Frame.StopSending);
            WriteFrameEpilog();
        }

        internal void OnCryptoFrame(in CryptoFrame frame)
        {
            if (!_logTransport)
                return;

            WriteFrameProlog(Frame.Crypto);

            _writer.WriteNumber(Field.offset, frame.Offset);
            _writer.WriteNumber(Field.length, frame.CryptoData.Length);

            WriteFrameEpilog();
        }

        internal void OnNewTokenFrame(in NewTokenFrame frame)
        {
            if (!_logTransport)
                return;

            WriteFrameProlog(Frame.NewToken);
            WriteFrameEpilog();
        }

        internal void OnStreamFrame(in StreamFrame frame)
        {
            if (!_logTransport)
                return;

            WriteFrameProlog(Frame.Stream);

            _writer.WriteNumber(Field.stream_id, frame.StreamId);
            _writer.WriteNumber(Field.offset, frame.Offset);
            _writer.WriteNumber(Field.length, frame.StreamData.Length);

            if (frame.Fin)
                _writer.WriteBoolean(Field.fin, frame.Fin);

            WriteFrameEpilog();
        }

        internal void OnMaxDataFrame(in MaxDataFrame frame)
        {
            if (!_logTransport)
                return;

            WriteFrameProlog(Frame.MaxData);
            WriteFrameEpilog();
        }

        internal void OnMaxStreamDataFrame(in MaxStreamDataFrame frame)
        {
            if (!_logTransport)
                return;

            WriteFrameProlog(Frame.MaxStreamData);
            WriteFrameEpilog();
        }

        internal void OnMaxStreamsFrame(in MaxStreamsFrame frame)
        {
            if (!_logTransport)
                return;

            WriteFrameProlog(Frame.MaxStreams);
            WriteFrameEpilog();
        }

        internal void OnDataBlockedFrame(in DataBlockedFrame frame)
        {
            if (!_logTransport)
                return;

            WriteFrameProlog(Frame.DataBlocked);
            WriteFrameEpilog();
        }

        internal void OnStreamDataBlockedFrame(in StreamDataBlockedFrame frame)
        {
            if (!_logTransport)
                return;

            WriteFrameProlog(Frame.StreamDataBlocked);
            WriteFrameEpilog();
        }

        internal void OnStreamsBlockedFrame(in StreamsBlockedFrame frame)
        {
            if (!_logTransport)
                return;

            WriteFrameProlog(Frame.StreamsBlocked);
            WriteFrameEpilog();
        }

        internal void OnNewConnectionIdFrame(in NewConnectionIdFrame frame)
        {
            if (!_logTransport)
                return;

            WriteFrameProlog(Frame.NewConnectionId);
            WriteFrameEpilog();
        }

        internal void OnRetireConnectionIdFrame(in RetireConnectionIdFrame frame)
        {
            if (!_logTransport)
                return;

            WriteFrameProlog(Frame.RetireConnectionId);
            WriteFrameEpilog();
        }

        internal void OnPathChallengeFrame(in PathChallengeFrame frame)
        {
            if (!_logTransport)
                return;

            WriteFrameProlog(frame.IsChallenge ? Frame.PathChallenge : Frame.PathResponse);
            WriteFrameEpilog();
        }

        internal void OnConnectionCloseFrame(in ConnectionCloseFrame frame)
        {
            if (!_logTransport)
                return;

            WriteFrameProlog(Frame.ConnectionClose);
            WriteFrameEpilog();
        }

        internal void OnHandshakeDoneFrame()
        {
            if (!_logTransport)
                return;

            WriteFrameProlog(Frame.HandshakeDone);
            WriteFrameEpilog();
        }

        internal void OnUnknownFrame(long frameType, int length)
        {
            if (!_logTransport)
                return;

            WriteFrameProlog(Frame.Unknown);

            _writer.WriteNumber(Field.raw_frame_type, frameType);
            _writer.WriteNumber(Field.raw_length, length);

            WriteFrameEpilog();
        }

        private void WriteFrameProlog(ReadOnlySpan<byte> frameType)
        {
            _writer.WriteStartObject();
            _writer.WriteString(Field.frame_type, frameType);
        }

        private void WriteFrameEpilog()
        {
            _writer.WriteEndObject();
        }

        internal void OnPacketLost(PacketType packetType, long packetNumber, PacketLossTrigger trigger)
        {
            if (!_logRecovery)
                return;

            WriteEventProlog(Category.Recovery, Event.packet_lost);

            _writer.WriteString(Field.packet_type, _packetTypeNames[packetType]);
            _writer.WriteNumber(Field.packet_number, packetNumber);
            _writer.WriteString(Field.trigger, _packetLossTriggerNames[trigger]);

            WriteEventEpilog();
        }

        // copy of the recovery parameters to reduce verbosity of the logs
        private long _minRtt;
        private long _smoothedRtt;
        private long _latestRtt;
        private long _rttVariance;
        private long _ptoCount;
        private long _congestionWindow;
        private long _bytesInFlight;
        private long _sstresh;

        internal void OnRecoveryMetricsUpdated(RecoveryController recovery)
        {
            if (!_logRecovery)
                return;

            WriteEventProlog(Category.Recovery, Event.metrics_updated);

            if (_minRtt != recovery.MinimumRtt)
                _writer.WriteNumber(Field.min_rtt, Timestamp.GetMicroseconds(_minRtt = recovery.MinimumRtt));

            if (_smoothedRtt != recovery.SmoothedRtt)
                _writer.WriteNumber(Field.smoothed_rtt, Timestamp.GetMicroseconds(_smoothedRtt = recovery.SmoothedRtt));

            if (_latestRtt != recovery.LatestRtt)
                _writer.WriteNumber(Field.latest_rtt, Timestamp.GetMicroseconds(_latestRtt = recovery.LatestRtt));

            if (_rttVariance != recovery.RttVariation)
                _writer.WriteNumber(Field.rtt_variance,
                    Timestamp.GetMicroseconds(_rttVariance = recovery.RttVariation));

            if (_ptoCount != recovery.PtoCount)
                _writer.WriteNumber(Field.pto_count, _ptoCount = recovery.PtoCount);

            if (_congestionWindow != recovery.CongestionWindow)
                _writer.WriteNumber(Field.congestion_window, _congestionWindow = recovery.CongestionWindow);

            if (_bytesInFlight != recovery.BytesInFlight)
                _writer.WriteNumber(Field.bytes_in_flight, _bytesInFlight = recovery.BytesInFlight);

            if (_sstresh != recovery.SlowStartThreshold)
                _writer.WriteNumber(Field.ssthresh, _sstresh = recovery.SlowStartThreshold);

            // TODO: total packets in flight?
            // _Writer.WriteNumber(Field.packets_in_flight, ???);

            // TODO: if pacing gets implemented
            // _Writer.WriteNumber(Field.pacing_rate, ???);

            WriteEventEpilog();
        }

        private CongestionState _previousCongestionState = CongestionState.Recovery;

        internal void OnCongestionStateUpdated(CongestionState state)
        {
            if (!_logRecovery)
                return;

            if (_previousCongestionState == state)
                return;
            _previousCongestionState = state;

            WriteEventProlog(Category.Recovery, Event.congestion_state_updated);

            _writer.WriteString(Field.@new, _congestionStateNames[state]);

            WriteEventEpilog();
        }

        internal void OnLossTimerUpdated()
        {
            if (!_logRecovery)
                return;

            // TODO
            // WriteEventProlog(Category.Recovery, Events.loss_timer_updated);
            // WriteEventEpilog();
        }

        internal void OnRecoveryParametersSet(RecoveryController recovery)
        {
            if (!_logRecovery)
                return;

            WriteEventProlog(Category.Recovery, Event.parameters_set);

            _writer.WriteNumber(Field.reordering_threshold, RecoveryController.PacketReorderingThreshold);
            _writer.WriteNumber(Field.time_threshold, RecoveryController.TimeReorderingThreshold);
            _writer.WriteNumber(Field.timer_granularity,
                Timestamp.GetMicroseconds(RecoveryController.TimerGranularity));
            _writer.WriteNumber(Field.initial_rtt, Timestamp.GetMicroseconds(RecoveryController.InitialRtt));

            _writer.WriteNumber(Field.max_datagram_size, RecoveryController.MaxDatagramSize);
            _writer.WriteNumber(Field.initial_congestion_window, RecoveryController.InitialWindowSize);
            _writer.WriteNumber(Field.minimum_congestion_window, RecoveryController.MinimumWindowSize);
            _writer.WriteNumber(Field.loss_reduction_factor, NewRenoCongestionController.LossReductionFactor);

            // TODO:
            // _Writer.WriteNumber(Field.persistent_congestion_threshold, ???);

            WriteEventEpilog();
        }

        private void WriteFooter()
        {
            Debug.Assert(!_inEvent);
            // end 'events' array
            _writer.WriteEndArray();
            // end trace object
            _writer.WriteEndObject();
            // end 'traces' array
            _writer.WriteEndArray();
            // end global object
            _writer.WriteEndObject();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            WriteFooter();

            _writer.Dispose();
            _stream.Dispose();
        }
    }
}
