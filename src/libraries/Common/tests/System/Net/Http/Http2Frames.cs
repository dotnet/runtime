// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace System.Net.Test.Common
{
    public enum FrameType : byte
    {
        Data = 0,
        Headers = 1,
        Priority = 2,
        RstStream = 3,
        Settings = 4,
        PushPromise = 5,
        Ping = 6,
        GoAway = 7,
        WindowUpdate = 8,
        Continuation = 9,
        AltSvc = 10
    }

    [Flags]
    public enum FrameFlags : byte
    {
        None = 0,

        // Some frame types define bits differently.  Define them all here for simplicity.
        EndStream =     0b00000001,
        Ack =           0b00000001,
        EndHeaders =    0b00000100,
        Padded =        0b00001000,
        Priority =      0b00100000,

        ValidBits =     0b00101101
    }

    public enum SettingId : ushort
    {
        HeaderTableSize = 0x1,
        EnablePush = 0x2,
        MaxConcurrentStreams = 0x3,
        InitialWindowSize = 0x4,
        MaxFrameSize = 0x5,
        MaxHeaderListSize = 0x6,
        EnableConnect = 0x8
    }

    public class Frame
    {
        public int Length;
        public FrameType Type;
        public FrameFlags Flags;
        public int StreamId;

        public const int FrameHeaderLength = 9;
        public const int MaxFrameLength = 16384;

        public Frame(int length, FrameType type, FrameFlags flags, int streamId)
        {
            Length = length;
            Type = type;
            Flags = flags;
            StreamId = streamId;
        }

        public bool PaddedFlag => (Flags & FrameFlags.Padded) != 0;
        public bool AckFlag => (Flags & FrameFlags.Ack) != 0;
        public bool EndHeadersFlag => (Flags & FrameFlags.EndHeaders) != 0;
        public bool EndStreamFlag => (Flags & FrameFlags.EndStream) != 0;
        public bool PriorityFlag => (Flags & FrameFlags.Priority) != 0;

        public static Frame ReadFrom(ReadOnlySpan<byte> buffer)
        {
            return new Frame(
                (buffer[0] << 16) | (buffer[1] << 8) | buffer[2],
                (FrameType)buffer[3],
                (FrameFlags)buffer[4],
                (int)((uint)((buffer[5] << 24) | (buffer[6] << 16) | (buffer[7] << 8) | buffer[8]) & 0x7FFFFFFF));
        }

        public virtual void WriteTo(Span<byte> buffer)
        {
            buffer[0] = (byte)((Length & 0x00FF0000) >> 16);
            buffer[1] = (byte)((Length & 0x0000FF00) >> 8);
            buffer[2] = (byte)(Length & 0x000000FF);

            buffer[3] = (byte)Type;
            buffer[4] = (byte)Flags;

            buffer[5] = (byte)((StreamId & 0xFF000000) >> 24);
            buffer[6] = (byte)((StreamId & 0x00FF0000) >> 16);
            buffer[7] = (byte)((StreamId & 0x0000FF00) >> 8);
            buffer[8] = (byte)(StreamId & 0x000000FF);
        }

        public override string ToString()
        {
            return $"Length: {Length}\nType: {Type}\nFlags: {Flags}\nStreamId: {StreamId}";
        }
    }

    public class DataFrame : Frame
    {
        public byte PadLength;
        public ReadOnlyMemory<byte> Data;

        public DataFrame(ReadOnlyMemory<byte> data, FrameFlags flags, byte padLength, int streamId) :
            base(0, FrameType.Data, flags, streamId)
        {
            Length = (flags & FrameFlags.Padded) == 0 ? data.Length : data.Length + padLength + 1;

            Data = data;
            PadLength = padLength;
        }

        public static DataFrame ReadFrom(Frame header, ReadOnlySpan<byte> buffer)
        {
            int idx = 0;

            byte padLength = (byte)(header.PaddedFlag ? buffer[idx++] : 0);
            byte[] data = buffer.Slice(idx).ToArray();

            return new DataFrame(data, header.Flags, padLength, header.StreamId);
        }

        public override void WriteTo(Span<byte> buffer)
        {
            base.WriteTo(buffer);

            if (PaddedFlag)
            {
                buffer[Frame.FrameHeaderLength] = PadLength;
                Data.Span.CopyTo(buffer.Slice(Frame.FrameHeaderLength + 1));
            }
            else
            {
                Data.Span.CopyTo(buffer.Slice(Frame.FrameHeaderLength));
            }
        }

        public override string ToString()
        {
            return base.ToString() + $"\nPadding: {PadLength}";
        }
    }

    // TODO: Add helpers to construct simple header data.
    public class HeadersFrame : Frame
    {
        public byte PadLength = 0;
        public int StreamDependency = 0;
        public byte Weight = 0;
        public Memory<byte> Data;

        public HeadersFrame(Memory<byte> data, FrameFlags flags, byte padLength, int streamDependency, byte weight, int streamId) :
            base(0, FrameType.Headers, flags, streamId)
        {
            Length = data.Length + (PaddedFlag ? padLength + 1 : 0) + (PriorityFlag ? 5 : 0);

            Data = data;
            PadLength = padLength;
            StreamDependency = streamDependency;
            Weight = weight;
        }

        public static HeadersFrame ReadFrom(Frame header, ReadOnlySpan<byte> buffer)
        {
            int idx = 0;

            byte padLength = (byte)(header.PaddedFlag ? buffer[idx++] : 0);
            int streamDependency = header.PriorityFlag ? (int)((uint)((buffer[idx++] << 24) | (buffer[idx++] << 16) | (buffer[idx++] << idx++) | buffer[idx++]) & 0x7FFFFFFF) : 0;
            byte weight = (byte)(header.PaddedFlag ? buffer[idx++] : 0);

            byte[] data = buffer.Slice(idx).ToArray();

            return new HeadersFrame(data, header.Flags, padLength, streamDependency, weight, header.StreamId);
        }

        public override void WriteTo(Span<byte> buffer)
        {
            base.WriteTo(buffer);

            int idx = Frame.FrameHeaderLength;
            if (PaddedFlag)
            {
                buffer[idx++] = PadLength;
            }

            if (PriorityFlag)
            {
                buffer[idx++] = (byte)((StreamDependency & 0xFF000000) >> 24);
                buffer[idx++] = (byte)((StreamDependency & 0x00FF0000) >> 16);
                buffer[idx++] = (byte)((StreamDependency & 0x0000FF00) >> 8);
                buffer[idx++] = (byte)(StreamDependency & 0x000000FF);

                buffer[idx++] = Weight;
            }
            Data.Span.CopyTo(buffer.Slice(idx));
        }

        public override string ToString()
        {
            return base.ToString() + $"\nPadding: {PadLength}\nStream Dependency: {StreamDependency}\nWeight: {Weight}";
        }
    }

    public class ContinuationFrame : Frame
    {
        public byte PadLength = 0;
        public int StreamDependency = 0;
        public byte Weight = 0;
        public Memory<byte> Data;

        public ContinuationFrame(Memory<byte> data, FrameFlags flags, byte padLength, int streamDependency, byte weight, int streamId) :
            base(0, FrameType.Continuation, flags, streamId)
        {
            Length = data.Length + (PaddedFlag ? padLength + 1 : 0) + (PriorityFlag ? 5 : 0);

            Data = data;
            PadLength = padLength;
            StreamDependency = streamDependency;
            Weight = weight;
        }

        public static ContinuationFrame ReadFrom(Frame header, ReadOnlySpan<byte> buffer)
        {
            int idx = 0;

            byte padLength = (byte)(header.PaddedFlag ? buffer[idx++] : 0);
            int streamDependency = header.PriorityFlag ? (int)((uint)((buffer[idx++] << 24) | (buffer[idx++] << 16) | (buffer[idx++] << idx++) | buffer[idx++]) & 0x7FFFFFFF) : 0;
            byte weight = (byte)(header.PaddedFlag ? buffer[idx++] : 0);

            byte[] data = buffer.Slice(idx).ToArray();

            return new ContinuationFrame(data, header.Flags, padLength, streamDependency, weight, header.StreamId);
        }

        public override void WriteTo(Span<byte> buffer)
        {
            base.WriteTo(buffer);

            int idx = Frame.FrameHeaderLength;
            if (PaddedFlag)
            {
                buffer[idx++] = PadLength;
            }

            if (PriorityFlag)
            {
                buffer[idx++] = (byte)((StreamDependency & 0xFF000000) >> 24);
                buffer[idx++] = (byte)((StreamDependency & 0x00FF0000) >> 16);
                buffer[idx++] = (byte)((StreamDependency & 0x0000FF00) >> 8);
                buffer[idx++] = (byte)(StreamDependency & 0x000000FF);

                buffer[idx++] = Weight;
            }
            Data.Span.CopyTo(buffer.Slice(idx));
        }

        public override string ToString()
        {
            return base.ToString() + $"\nPadding: {PadLength}\nStream Dependency: {StreamDependency}\nWeight: {Weight}";
        }
    }

    public class PriorityFrame : Frame
    {
        public int StreamDependency = 0;
        public byte Weight = 0;

        public PriorityFrame(FrameFlags flags, int streamDependency, byte weight, int streamId) :
            base(Frame.FrameHeaderLength + 5, FrameType.Priority, flags, streamId)
        {
            StreamDependency = streamDependency;
            Weight = weight;
        }

        public static PriorityFrame ReadFrom(Frame header, ReadOnlySpan<byte> buffer)
        {
            int idx = Frame.FrameHeaderLength;
            int streamDependency = (int)((uint)((buffer[idx++] << 24) | (buffer[idx++] << 16) | (buffer[idx++] << idx++) | buffer[idx++]) & 0x7FFFFFFF);
            byte weight = (byte)buffer[idx++];

            return new PriorityFrame(header.Flags, streamDependency, weight, header.StreamId);
        }

        public override void WriteTo(Span<byte> buffer)
        {
            base.WriteTo(buffer);

            int idx = Frame.FrameHeaderLength;

            buffer[idx++] = (byte)((StreamDependency & 0xFF000000) >> 24);
            buffer[idx++] = (byte)((StreamDependency & 0x00FF0000) >> 16);
            buffer[idx++] = (byte)((StreamDependency & 0x0000FF00) >> 8);
            buffer[idx++] = (byte)(StreamDependency & 0x000000FF);

            buffer[idx++] = Weight;
        }

        public override string ToString()
        {
            return base.ToString() + $"\nStream Dependency: {StreamDependency}\nWeight: {Weight}";
        }
    }

    public class RstStreamFrame : Frame
    {
        public int ErrorCode = 0;

        public RstStreamFrame(FrameFlags flags, int errorCode, int streamId) :
            base(4, FrameType.RstStream, flags, streamId)
        {
            ErrorCode = errorCode;
        }

        public static RstStreamFrame ReadFrom(Frame header, ReadOnlySpan<byte> buffer)
        {
            int idx = 0;
            int errorCode = (int)((uint)((buffer[idx++] << 24) | (buffer[idx++] << 16) | (buffer[idx++] << 8) | buffer[idx++]) & 0x7FFFFFFF);

            return new RstStreamFrame(header.Flags, errorCode, header.StreamId);
        }

        public override void WriteTo(Span<byte> buffer)
        {
            base.WriteTo(buffer);

            int idx = Frame.FrameHeaderLength;

            buffer[idx++] = (byte)((ErrorCode & 0xFF000000) >> 24);
            buffer[idx++] = (byte)((ErrorCode & 0x00FF0000) >> 16);
            buffer[idx++] = (byte)((ErrorCode & 0x0000FF00) >> 8);
            buffer[idx++] = (byte)(ErrorCode & 0x000000FF);
        }

        public override string ToString()
        {
            return base.ToString() + $"\nError Code: {ErrorCode}";
        }
    }

    public class PingFrame : Frame
    {
        public long Data;

        public PingFrame(long data, FrameFlags flags, int streamId) :
            base(8, FrameType.Ping, flags, streamId)
        {
            Data = data;
        }

        public static PingFrame ReadFrom(Frame header, ReadOnlySpan<byte> buffer)
        {
            long data = BinaryPrimitives.ReadInt64BigEndian(buffer);

            return new PingFrame(data, header.Flags, header.StreamId);
        }

        public override void WriteTo(Span<byte> buffer)
        {
            base.WriteTo(buffer);
            buffer = buffer.Slice(Frame.FrameHeaderLength, 8);

            BinaryPrimitives.WriteInt64BigEndian(buffer, Data);
        }

        public override string ToString()
        {
            return base.ToString() + $"\nOpaque Data: {Data:X16}";
        }
    }

    public class WindowUpdateFrame : Frame
    {
        public int UpdateSize;

        public WindowUpdateFrame(int updateSize, int streamId) :
            base(4, FrameType.WindowUpdate, FrameFlags.None, streamId)
        {
            UpdateSize = updateSize;
        }

        public static WindowUpdateFrame ReadFrom(Frame header, ReadOnlySpan<byte> buffer)
        {
            int updateSize = BinaryPrimitives.ReadInt32BigEndian(buffer);

            return new WindowUpdateFrame(updateSize, header.StreamId);
        }

        public override void WriteTo(Span<byte> buffer)
        {
            base.WriteTo(buffer);

            BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(Frame.FrameHeaderLength), UpdateSize);
        }

        public override string ToString()
        {
            return base.ToString() + $"\nUpdateSize: {UpdateSize}";
        }
    }

    public struct SettingsEntry
    {
        public SettingId SettingId;
        public uint Value;
    }

    public class SettingsFrame : Frame
    {
        const int RfcDefaultDynamicTableSize = 4096;

        public List<SettingsEntry> Entries;

        public SettingsFrame(FrameFlags flags, SettingsEntry[] entries) :
            base(entries.Length * 6, FrameType.Settings, flags, 0)
        {
            Entries = new List<SettingsEntry>(entries);
        }

        public SettingsFrame(params SettingsEntry[] entries) :
            this(FrameFlags.None, entries)
        {
        }

        public static SettingsFrame ReadFrom(Frame header, ReadOnlySpan<byte> buffer)
        {
            var entries = new List<SettingsEntry>();

            while (buffer.Length > 0)
            {
                SettingId id = (SettingId)BinaryPrimitives.ReadUInt16BigEndian(buffer);
                buffer = buffer.Slice(2);
                uint value = BinaryPrimitives.ReadUInt32BigEndian(buffer);
                buffer = buffer.Slice(4);

                entries.Add(new SettingsEntry { SettingId = id, Value = value });
            }

            return new SettingsFrame(header.Flags, entries.ToArray());
        }

        public override void WriteTo(Span<byte> buffer)
        {
            base.WriteTo(buffer);
            buffer = buffer.Slice(Frame.FrameHeaderLength);

            foreach (SettingsEntry entry in Entries)
            {
                BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)entry.SettingId);
                buffer = buffer.Slice(2);
                BinaryPrimitives.WriteUInt32BigEndian(buffer, entry.Value);
                buffer = buffer.Slice(4);
            }
        }

        public override string ToString()
        {
            return base.ToString() + $"\nEntry Count: {Entries.Count}";
        }

        public uint GetSettingOrDefault(SettingId settingId, uint defaultValue)
        {
            foreach (SettingsEntry setting in Entries)
            {
                if (setting.SettingId == settingId)
                {
                    defaultValue = setting.Value;
                }
            }

            return defaultValue;
        }

        public int GetHeaderTableSize() => (int)GetSettingOrDefault(SettingId.HeaderTableSize, RfcDefaultDynamicTableSize);
    }

    public class GoAwayFrame : Frame
    {
        public int LastStreamId;
        public int ErrorCode;
        public byte[] AdditionalDebugData;

        public GoAwayFrame(int lastStreamId, int errorCode, byte[] additionalDebugData, int streamId) :
            base(additionalDebugData.Length + 8, FrameType.GoAway, FrameFlags.None, streamId)
        {
            LastStreamId = lastStreamId;
            ErrorCode = errorCode;
            AdditionalDebugData = additionalDebugData;
        }

        public static GoAwayFrame ReadFrom(Frame header, ReadOnlySpan<byte> buffer)
        {
            int lastStreamId = BinaryPrimitives.ReadInt32BigEndian(buffer);
            int errorCode = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(4));
            byte[] additionalDebugData = buffer.Slice(8).ToArray();

            return new GoAwayFrame(lastStreamId, errorCode, additionalDebugData, header.StreamId);
        }

        public override void WriteTo(Span<byte> buffer)
        {
            base.WriteTo(buffer);

            BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(Frame.FrameHeaderLength), LastStreamId);
            BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(Frame.FrameHeaderLength + 4), ErrorCode);
            AdditionalDebugData.CopyTo(buffer.Slice(Frame.FrameHeaderLength + 8));
        }

        public override string ToString()
        {
            return base.ToString() + $"\nLastStreamId: {LastStreamId}\nErrorCode: {ErrorCode}";
        }
    }

    public class AltSvcFrame : Frame
    {
        public string Origin;
        public string AltSvc;

        /// <summary>
        /// ALTSVC frame, defined in https://tools.ietf.org/html/rfc7838#section-4
        /// </summary>
        /// <param name="origin">
        /// Origin in format &lt;scheme&gt;://&lt;hostname&gt;:&lt;port&gt;.
        /// Defined by https://tools.ietf.org/html/rfc6454#section-6.2.
        /// </param>
        /// <param name="altSvc">
        /// An Alt-Svc header value as defined by https://tools.ietf.org/html/rfc7838#section-3
        /// E.g. "h3=contoso.com:12345"
        /// </param>
        public AltSvcFrame(string origin, string altSvc, int streamId)
            : base(2 + origin.Length + altSvc.Length, FrameType.AltSvc, FrameFlags.None, streamId)
        {
            Origin = origin;
            AltSvc = altSvc;
        }

        public override void WriteTo(Span<byte> buffer)
        {
            base.WriteTo(buffer);
            buffer = buffer.Slice(Frame.FrameHeaderLength);

            BinaryPrimitives.WriteUInt16BigEndian(buffer, checked((ushort)Origin.Length));
            buffer = buffer.Slice(2);

            var tmpBuffer = Encoding.ASCII.GetBytes(Origin);
            tmpBuffer.CopyTo(buffer);
            buffer = buffer.Slice(Origin.Length);

            tmpBuffer = Encoding.ASCII.GetBytes(AltSvc);
            tmpBuffer.CopyTo(buffer);
        }

        public override string ToString() => $"{base.ToString()}\n{nameof(Origin)}: {Origin}\n{nameof(AltSvc)}: {AltSvc}";
    }
}
