// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ILCompiler.DependencyAnalysis
{
    public readonly struct CodeInfo : IEquatable<CodeInfo>
    {
        public CodeInfo(
            byte[] gcInfo,
            IReadOnlyList<FrameInfo> frameInfos,
            IReadOnlyList<FrameInfo> coldFrameInfos,
            IReadOnlyList<ISymbolNode> fixups)
        {
            GCInfo = gcInfo;
            FrameInfos = frameInfos;
            ColdFrameInfos = coldFrameInfos;
            Fixups = fixups;
        }

        public byte[] GCInfo { get; }
        public IReadOnlyList<FrameInfo> FrameInfos { get; }
        public IReadOnlyList<FrameInfo> ColdFrameInfos { get; }
        public IReadOnlyList<ISymbolNode> Fixups { get; }

        public bool Equals(CodeInfo other) =>
            BytesEqual(GCInfo, other.GCInfo)
            && FrameInfosEqual(FrameInfos, other.FrameInfos)
            && FrameInfosEqual(ColdFrameInfos, other.ColdFrameInfos)
            && FixupsEqual(Fixups, other.Fixups);

        public override bool Equals(object obj) => obj is CodeInfo other && Equals(other);

        public override int GetHashCode()
        {
            HashCode hash = default;

            if (GCInfo is not null)
            {
                hash.AddBytes(GCInfo);
            }

            AddFrameInfos(ref hash, FrameInfos);
            AddFrameInfos(ref hash, ColdFrameInfos);

            if (Fixups is not null)
            {
                for (int i = 0; i < Fixups.Count; i++)
                {
                    hash.Add(RuntimeHelpers.GetHashCode(Fixups[i]));
                }
            }

            return hash.ToHashCode();
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            ReadOnlySpan<byte> leftSpan = left;
            ReadOnlySpan<byte> rightSpan = right;
            return leftSpan.SequenceEqual(rightSpan);
        }

        private static bool FrameInfosEqual(
            IReadOnlyList<FrameInfo> left,
            IReadOnlyList<FrameInfo> right)
        {
            int leftCount = left?.Count ?? 0;
            int rightCount = right?.Count ?? 0;
            if (leftCount != rightCount)
            {
                return false;
            }

            for (int i = 0; i < leftCount; i++)
            {
                if (!left[i].Equals(right[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool FixupsEqual(
            IReadOnlyList<ISymbolNode> left,
            IReadOnlyList<ISymbolNode> right)
        {
            int leftCount = left?.Count ?? 0;
            int rightCount = right?.Count ?? 0;
            if (leftCount != rightCount)
            {
                return false;
            }

            for (int i = 0; i < leftCount; i++)
            {
                if (!ReferenceEquals(left[i], right[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static void AddFrameInfos(ref HashCode hash, IReadOnlyList<FrameInfo> frameInfos)
        {
            if (frameInfos is null)
            {
                return;
            }

            for (int i = 0; i < frameInfos.Count; i++)
            {
                hash.Add(frameInfos[i]);
            }
        }
    }

    public partial interface INodeWithCodeInfo
    {
        bool IsShareableCode { get; }

        CodeInfo CodeInfo { get; }
    }

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
