// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection.Metadata.Ecma335
{
    public readonly struct ExceptionRegionEncoder
    {
        private const int TableHeaderSize = 4;

        private const int SmallRegionSize =
            sizeof(short) +  // Flags
            sizeof(short) +  // TryOffset
            sizeof(byte) +   // TryLength
            sizeof(short) +  // HandlerOffset
            sizeof(byte) +   // HandleLength
            sizeof(int);     // ClassToken | FilterOffset

        private const int FatRegionSize =
            sizeof(int) +    // Flags
            sizeof(int) +    // TryOffset
            sizeof(int) +    // TryLength
            sizeof(int) +    // HandlerOffset
            sizeof(int) +    // HandleLength
            sizeof(int);     // ClassToken | FilterOffset

        private const int ThreeBytesMaxValue = 0xffffff;
        internal const int MaxSmallExceptionRegions = (byte.MaxValue - TableHeaderSize) / SmallRegionSize;
        internal const int MaxExceptionRegions = (ThreeBytesMaxValue - TableHeaderSize) / FatRegionSize;

        /// <summary>
        /// The underlying builder.
        /// </summary>
        public BlobBuilder Builder { get; }

        /// <summary>
        /// True if the encoder uses small format.
        /// </summary>
        public bool HasSmallFormat { get; }

        internal ExceptionRegionEncoder(BlobBuilder builder, bool hasSmallFormat)
        {
            Builder = builder;
            HasSmallFormat = hasSmallFormat;
        }

        /// <summary>
        /// Returns true if the number of exception regions first small format.
        /// </summary>
        /// <param name="exceptionRegionCount">Number of exception regions.</param>
        public static bool IsSmallRegionCount(int exceptionRegionCount) =>
            unchecked((uint)exceptionRegionCount) <= MaxSmallExceptionRegions;

        /// <summary>
        /// Returns true if the region fits small format.
        /// </summary>
        /// <param name="startOffset">Start offset of the region.</param>
        /// <param name="length">Length of the region.</param>
        public static bool IsSmallExceptionRegion(int startOffset, int length) => 
            unchecked((uint)startOffset) <= ushort.MaxValue && unchecked((uint)length) <= byte.MaxValue;

        internal static bool IsSmallExceptionRegionFromBounds(int startOffset, int endOffset) => 
            IsSmallExceptionRegion(startOffset, endOffset - startOffset);

        internal static int GetExceptionTableSize(int exceptionRegionCount, bool isSmallFormat) => 
            TableHeaderSize + exceptionRegionCount * (isSmallFormat ? SmallRegionSize : FatRegionSize);

        internal static bool IsExceptionRegionCountInBounds(int exceptionRegionCount) => 
            unchecked((uint)exceptionRegionCount) <= MaxExceptionRegions;

        internal static bool IsValidCatchTypeHandle(EntityHandle catchType)
        {
            return !catchType.IsNil &&
                   (catchType.Kind == HandleKind.TypeDefinition ||
                    catchType.Kind == HandleKind.TypeSpecification ||
                    catchType.Kind == HandleKind.TypeReference);
        }

        internal static ExceptionRegionEncoder SerializeTableHeader(BlobBuilder builder, int exceptionRegionCount, bool hasSmallRegions)
        {
            Debug.Assert(exceptionRegionCount > 0);

            const byte EHTableFlag = 0x01;
            const byte FatFormatFlag = 0x40;

            bool hasSmallFormat = hasSmallRegions && IsSmallRegionCount(exceptionRegionCount);
            int dataSize = GetExceptionTableSize(exceptionRegionCount, hasSmallFormat);

            builder.Align(4);
            if (hasSmallFormat)
            {
                builder.WriteByte(EHTableFlag);
                builder.WriteByte(unchecked((byte)dataSize));
                builder.WriteInt16(0);
            }
            else
            {
                Debug.Assert(dataSize <= 0x00ffffff);
                builder.WriteByte(EHTableFlag | FatFormatFlag);
                builder.WriteByte(unchecked((byte)dataSize));
                builder.WriteUInt16(unchecked((ushort)(dataSize >> 8)));
            }

            return new ExceptionRegionEncoder(builder, hasSmallFormat);
        }

        /// <summary>
        /// Adds a finally clause.
        /// </summary>
        /// <param name="tryOffset">Try block start offset.</param>
        /// <param name="tryLength">Try block length.</param>
        /// <param name="handlerOffset">Handler start offset.</param>
        /// <param name="handlerLength">Handler length.</param>
        /// <returns>Encoder for the next clause.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="tryOffset"/>, <paramref name="tryLength"/>, <paramref name="handlerOffset"/> or <paramref name="handlerLength"/> is out of range.
        /// </exception>
        /// <exception cref="InvalidOperationException">Method body was not declared to have exception regions.</exception>
        public ExceptionRegionEncoder AddFinally(int tryOffset, int tryLength, int handlerOffset, int handlerLength)
        {
            return Add(ExceptionRegionKind.Finally, tryOffset, tryLength, handlerOffset, handlerLength, default(EntityHandle), 0);
        }

        /// <summary>
        /// Adds a fault clause.
        /// </summary>
        /// <param name="tryOffset">Try block start offset.</param>
        /// <param name="tryLength">Try block length.</param>
        /// <param name="handlerOffset">Handler start offset.</param>
        /// <param name="handlerLength">Handler length.</param>
        /// <returns>Encoder for the next clause.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="tryOffset"/>, <paramref name="tryLength"/>, <paramref name="handlerOffset"/> or <paramref name="handlerLength"/> is out of range.
        /// </exception>
        /// <exception cref="InvalidOperationException">Method body was not declared to have exception regions.</exception>
        public ExceptionRegionEncoder AddFault(int tryOffset, int tryLength, int handlerOffset, int handlerLength)
        {
            return Add(ExceptionRegionKind.Fault, tryOffset, tryLength, handlerOffset, handlerLength, default(EntityHandle), 0);
        }

        /// <summary>
        /// Adds a fault clause.
        /// </summary>
        /// <param name="tryOffset">Try block start offset.</param>
        /// <param name="tryLength">Try block length.</param>
        /// <param name="handlerOffset">Handler start offset.</param>
        /// <param name="handlerLength">Handler length.</param>
        /// <param name="catchType">
        /// <see cref="TypeDefinitionHandle"/>, <see cref="TypeReferenceHandle"/> or <see cref="TypeSpecificationHandle"/>.
        /// </param>
        /// <returns>Encoder for the next clause.</returns>
        /// <exception cref="ArgumentException"><paramref name="catchType"/> is invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="tryOffset"/>, <paramref name="tryLength"/>, <paramref name="handlerOffset"/> or <paramref name="handlerLength"/> is out of range.
        /// </exception>
        /// <exception cref="InvalidOperationException">Method body was not declared to have exception regions.</exception>
        public ExceptionRegionEncoder AddCatch(int tryOffset, int tryLength, int handlerOffset, int handlerLength, EntityHandle catchType)
        {
            return Add(ExceptionRegionKind.Catch, tryOffset, tryLength, handlerOffset, handlerLength, catchType, 0);
        }

        /// <summary>
        /// Adds a fault clause.
        /// </summary>
        /// <param name="tryOffset">Try block start offset.</param>
        /// <param name="tryLength">Try block length.</param>
        /// <param name="handlerOffset">Handler start offset.</param>
        /// <param name="handlerLength">Handler length.</param>
        /// <param name="filterOffset">Offset of the filter block.</param>
        /// <returns>Encoder for the next clause.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="tryOffset"/>, <paramref name="tryLength"/>, <paramref name="handlerOffset"/> or <paramref name="handlerLength"/> is out of range.
        /// </exception>
        /// <exception cref="InvalidOperationException">Method body was not declared to have exception regions.</exception>
        public ExceptionRegionEncoder AddFilter(int tryOffset, int tryLength, int handlerOffset, int handlerLength, int filterOffset)
        {
            return Add(ExceptionRegionKind.Filter, tryOffset, tryLength, handlerOffset, handlerLength, default(EntityHandle), filterOffset);
        }

        /// <summary>
        /// Adds an exception clause.
        /// </summary>
        /// <param name="kind">Clause kind.</param>
        /// <param name="tryOffset">Try block start offset.</param>
        /// <param name="tryLength">Try block length.</param>
        /// <param name="handlerOffset">Handler start offset.</param>
        /// <param name="handlerLength">Handler length.</param>
        /// <param name="catchType">
        /// <see cref="TypeDefinitionHandle"/>, <see cref="TypeReferenceHandle"/> or <see cref="TypeSpecificationHandle"/>, 
        /// or nil if <paramref name="kind"/> is not <see cref="ExceptionRegionKind.Catch"/>
        /// </param>
        /// <param name="filterOffset">
        /// Offset of the filter block, or 0 if the <paramref name="kind"/> is not <see cref="ExceptionRegionKind.Filter"/>.
        /// </param>
        /// <returns>Encoder for the next clause.</returns>
        /// <exception cref="ArgumentException"><paramref name="catchType"/> is invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> has invalid value.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="tryOffset"/>, <paramref name="tryLength"/>, <paramref name="handlerOffset"/> or <paramref name="handlerLength"/> is out of range.
        /// </exception>
        /// <exception cref="InvalidOperationException">Method body was not declared to have exception regions.</exception>
        public ExceptionRegionEncoder Add(
            ExceptionRegionKind kind,
            int tryOffset,
            int tryLength,
            int handlerOffset,
            int handlerLength,
            EntityHandle catchType = default(EntityHandle),
            int filterOffset = 0)
        {
            if (Builder == null)
            {
                Throw.InvalidOperation(SR.MethodHasNoExceptionRegions);
            }

            if (HasSmallFormat)
            {
                if (unchecked((ushort)tryOffset) != tryOffset) Throw.ArgumentOutOfRange(nameof(tryOffset));
                if (unchecked((byte)tryLength) != tryLength) Throw.ArgumentOutOfRange(nameof(tryLength));
                if (unchecked((ushort)handlerOffset) != handlerOffset) Throw.ArgumentOutOfRange(nameof(handlerOffset));
                if (unchecked((byte)handlerLength) != handlerLength) Throw.ArgumentOutOfRange(nameof(handlerLength));
            }
            else
            {
                if (tryOffset < 0) Throw.ArgumentOutOfRange(nameof(tryOffset));
                if (tryLength < 0) Throw.ArgumentOutOfRange(nameof(tryLength));
                if (handlerOffset < 0) Throw.ArgumentOutOfRange(nameof(handlerOffset));
                if (handlerLength < 0) Throw.ArgumentOutOfRange(nameof(handlerLength));
            }

            int catchTokenOrOffset;
            switch (kind)
            {
                case ExceptionRegionKind.Catch:
                    if (!IsValidCatchTypeHandle(catchType))
                    {
                        Throw.InvalidArgument_Handle(nameof(catchType));
                    }

                    catchTokenOrOffset = MetadataTokens.GetToken(catchType);
                    break;

                case ExceptionRegionKind.Filter:
                    if (filterOffset < 0)
                    {
                        Throw.ArgumentOutOfRange(nameof(filterOffset));
                    }

                    catchTokenOrOffset = filterOffset;
                    break;

                case ExceptionRegionKind.Finally:
                case ExceptionRegionKind.Fault:
                    catchTokenOrOffset = 0;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }

            AddUnchecked(kind, tryOffset, tryLength, handlerOffset, handlerLength, catchTokenOrOffset);
            return this;
        }

        internal void AddUnchecked(
            ExceptionRegionKind kind,
            int tryOffset,
            int tryLength,
            int handlerOffset,
            int handlerLength,
            int catchTokenOrOffset)
        {
            if (HasSmallFormat)
            {
                Builder.WriteUInt16((ushort)kind);
                Builder.WriteUInt16((ushort)tryOffset);
                Builder.WriteByte((byte)tryLength);
                Builder.WriteUInt16((ushort)handlerOffset);
                Builder.WriteByte((byte)handlerLength);
            }
            else
            {
                Builder.WriteInt32((int)kind);
                Builder.WriteInt32(tryOffset);
                Builder.WriteInt32(tryLength);
                Builder.WriteInt32(handlerOffset);
                Builder.WriteInt32(handlerLength);
            }

            Builder.WriteInt32(catchTokenOrOffset);
        }
    }
}
