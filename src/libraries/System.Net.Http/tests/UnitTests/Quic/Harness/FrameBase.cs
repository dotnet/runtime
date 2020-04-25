using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace System.Net.Quic.Tests.Harness
{
    internal abstract class FrameBase
    {
        internal abstract FrameType FrameType { get; }

        internal abstract void Serialize(QuicWriter writer);

        internal abstract bool Deserialize(QuicReader reader);

        public override string ToString() => $"{FrameType.ToString()}{GetAdditionalInfo()}";

        protected virtual string GetAdditionalInfo() => "";

        private static T Parse<T>(QuicReader reader) where T : FrameBase, new()
        {
            var frame = new T();
            Assert.True(frame.Deserialize(reader), "Unable to deserialize frame");
            return frame;
        }

        internal static FrameBase Parse(QuicReader reader)
        {
            FrameType type = reader.PeekFrameType();
            switch (type)
            {
                case FrameType.Padding:
                    return Parse<PaddingFrame>(reader);
                case FrameType.Ping:
                    return Parse<PingFrame>(reader);
                case FrameType.Ack:
                case FrameType.AckWithEcn:
                    return Parse<AckFrame>(reader);
                case FrameType.ResetStream:
                    return Parse<ResetStreamFrame>(reader);
                case FrameType.StopSending:
                    return Parse<StopSendingFrame>(reader);
                case FrameType.Crypto:
                    return Parse<CryptoFrame>(reader);
                case FrameType.NewToken:
                    return Parse<NewTokenFrame>(reader);
                case FrameType.MaxData:
                    return Parse<MaxDataFrame>(reader);
                case FrameType.MaxStreamData:
                    return Parse<MaxStreamDataFrame>(reader);
                case FrameType.MaxStreamsBidirectional:
                case FrameType.MaxStreamsUnidirectional:
                    return Parse<MaxStreamsFrame>(reader);
                case FrameType.DataBlocked:
                    return Parse<DataBlockedFrame>(reader);
                case FrameType.StreamDataBlocked:
                    return Parse<StreamDataBlockedFrame>(reader);
                case FrameType.StreamsBlockedBidirectional:
                case FrameType.StreamsBlockedUnidirectional:
                    return Parse<StreamsBlockedFrame>(reader);
                case FrameType.NewConnectionId:
                    return Parse<NewConnectionIdFrame>(reader);
                case FrameType.RetireConnectionId:
                    return Parse<RetireConnectionIdFrame>(reader);
                case FrameType.PathChallenge:
                case FrameType.PathResponse:
                    return Parse<PathChallengeFrame>(reader);
                case FrameType.ConnectionCloseQuic:
                case FrameType.ConnectionCloseApplication:
                    return Parse<ConnectionCloseFrame>(reader);
                case FrameType.HandshakeDone:
                    return Parse<HandshakeDoneFrame>(reader);
                default:
                    if ((type & FrameType.StreamMask) == type)
                        return Parse<StreamFrame>(reader);

                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
