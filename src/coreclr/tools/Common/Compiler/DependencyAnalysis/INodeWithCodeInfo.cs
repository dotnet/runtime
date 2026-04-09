// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILCompiler.DependencyAnalysis
{
    [Flags]
    public enum FrameInfoFlags
    {
        Handler             = 0x01,
        Filter              = 0x02,

        HasEHInfo           = 0x04,
        ReversePInvoke      = 0x08,
        HasAssociatedData   = 0x10,
    }

    public struct FrameInfo : IEquatable<FrameInfo>
    {
        public readonly FrameInfoFlags Flags;
        public readonly int StartOffset;
        public readonly int EndOffset;
        public readonly byte[] BlobData;

        public FrameInfo(FrameInfoFlags flags, int startOffset, int endOffset, byte[] blobData)
        {
            Flags = flags;
            StartOffset = startOffset;
            EndOffset = endOffset;
            BlobData = blobData;
        }

        public bool Equals(FrameInfo other)
            => Flags == other.Flags
            && StartOffset == other.StartOffset
            && EndOffset == other.EndOffset
            && ((ReadOnlySpan<byte>)BlobData).SequenceEqual(other.BlobData);

        public override bool Equals(object obj) => obj is FrameInfo other && Equals(other);

        public override int GetHashCode()
        {
            HashCode hash = default;
            hash.Add(Flags);
            hash.Add(StartOffset);
            hash.Add(EndOffset);
            hash.AddBytes(BlobData);
            return hash.ToHashCode();
        }
    }

    public struct DebugEHClauseInfo
    {
        public uint TryOffset;
        public uint TryLength;
        public uint HandlerOffset;
        public uint HandlerLength;

        public DebugEHClauseInfo(uint tryOffset, uint tryLength, uint handlerOffset, uint handlerLength)
        {
            TryOffset = tryOffset;
            TryLength = tryLength;
            HandlerOffset = handlerOffset;
            HandlerLength = handlerLength;
        }
    }
}
