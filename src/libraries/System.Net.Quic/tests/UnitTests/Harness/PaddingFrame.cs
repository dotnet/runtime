// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Quic.Implementations.Managed.Internal;

namespace System.Net.Quic.Tests.Harness
{
    internal class PaddingFrame : FrameBase
    {
        internal override FrameType FrameType => FrameType.Padding;

        internal int Length;

        protected override string GetAdditionalInfo() => $"[{Length}]";

        internal override void Serialize(QuicWriter writer)
        {
            writer.GetWritableSpan(Length).Clear();
        }

        internal override bool Deserialize(QuicReader reader)
        {
            Length = 0;
            while (reader.BytesLeft > 0 && reader.Peek() == 0)
            {
                Length++;
                reader.ReadUInt8();
            }

            return Length > 0;
        }
    }
}
