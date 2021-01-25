// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Metadata.Ecma335
{
    /// <summary>
    /// Encodes method body stream.
    /// </summary>
    public readonly struct MethodBodyStreamEncoder
    {
        public BlobBuilder Builder { get; }

        public MethodBodyStreamEncoder(BlobBuilder builder)
        {
            if (builder == null)
            {
                Throw.BuilderArgumentNull();
            }

            // Fat methods are 4-byte aligned. We calculate the alignment relative to the start of the ILStream.
            //
            // See ECMA-335 paragraph 25.4.5, Method data section:
            // "At the next 4-byte boundary following the method body can be extra method data sections."
            if ((builder.Count % 4) != 0)
            {
                throw new ArgumentException(SR.BuilderMustAligned, nameof(builder));
            }

            Builder = builder;
        }

        /// <summary>
        /// Encodes a method body and adds it to the method body stream.
        /// </summary>
        /// <param name="codeSize">Number of bytes to be reserved for instructions.</param>
        /// <param name="maxStack">Max stack.</param>
        /// <param name="exceptionRegionCount">Number of exception regions.</param>
        /// <param name="hasSmallExceptionRegions">True if the exception regions should be encoded in 'small' format.</param>
        /// <param name="localVariablesSignature">Local variables signature handle.</param>
        /// <param name="attributes">Attributes.</param>
        /// <returns>The offset of the encoded body within the method body stream.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="codeSize"/>, <paramref name="exceptionRegionCount"/>, or <paramref name="maxStack"/> is out of allowed range.
        /// </exception>
        public MethodBody AddMethodBody(
            int codeSize,
            int maxStack,
            int exceptionRegionCount,
            bool hasSmallExceptionRegions,
            StandaloneSignatureHandle localVariablesSignature,
            MethodBodyAttributes attributes)
            => AddMethodBody(codeSize, maxStack, exceptionRegionCount, hasSmallExceptionRegions, localVariablesSignature, attributes, hasDynamicStackAllocation: false);

        /// <summary>
        /// Encodes a method body and adds it to the method body stream.
        /// </summary>
        /// <param name="codeSize">Number of bytes to be reserved for instructions.</param>
        /// <param name="maxStack">Max stack.</param>
        /// <param name="exceptionRegionCount">Number of exception regions.</param>
        /// <param name="hasSmallExceptionRegions">True if the exception regions should be encoded in 'small' format.</param>
        /// <param name="localVariablesSignature">Local variables signature handle.</param>
        /// <param name="attributes">Attributes.</param>
        /// <param name="hasDynamicStackAllocation">True if the method allocates from dynamic local memory pool (<c>localloc</c> instruction).</param>
        /// <returns>The offset of the encoded body within the method body stream.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="codeSize"/>, <paramref name="exceptionRegionCount"/>, or <paramref name="maxStack"/> is out of allowed range.
        /// </exception>
        public MethodBody AddMethodBody(
            int codeSize,
            int maxStack = 8,
            int exceptionRegionCount = 0,
            bool hasSmallExceptionRegions = true,
            StandaloneSignatureHandle localVariablesSignature = default,
            MethodBodyAttributes attributes = MethodBodyAttributes.InitLocals,
            bool hasDynamicStackAllocation = false)
        {
            if (codeSize < 0)
            {
                Throw.ArgumentOutOfRange(nameof(codeSize));
            }

            if (unchecked((uint)maxStack) > ushort.MaxValue)
            {
                Throw.ArgumentOutOfRange(nameof(maxStack));
            }

            if (!ExceptionRegionEncoder.IsExceptionRegionCountInBounds(exceptionRegionCount))
            {
                Throw.ArgumentOutOfRange(nameof(exceptionRegionCount));
            }

            int bodyOffset = SerializeHeader(codeSize, (ushort)maxStack, exceptionRegionCount, attributes, localVariablesSignature, hasDynamicStackAllocation);
            var instructions = Builder.ReserveBytes(codeSize);

            var regionEncoder = (exceptionRegionCount > 0) ?
                ExceptionRegionEncoder.SerializeTableHeader(Builder, exceptionRegionCount, hasSmallExceptionRegions) : default;

            return new MethodBody(bodyOffset, instructions, regionEncoder);
        }

        public readonly struct MethodBody
        {
            /// <summary>
            /// Offset of the encoded method body in method body stream.
            /// </summary>
            public int Offset { get; }

            /// <summary>
            /// Blob reserved for instructions.
            /// </summary>
            public Blob Instructions { get; }

            /// <summary>
            /// Use to encode exception regions to the method body.
            /// </summary>
            public ExceptionRegionEncoder ExceptionRegions { get; }

            internal MethodBody(int bodyOffset, Blob instructions, ExceptionRegionEncoder exceptionRegions)
            {
                Offset = bodyOffset;
                Instructions = instructions;
                ExceptionRegions = exceptionRegions;
            }
        }

        /// <summary>
        /// Encodes a method body and adds it to the method body stream.
        /// </summary>
        /// <param name="instructionEncoder">Instruction encoder.</param>
        /// <param name="maxStack">Max stack.</param>
        /// <param name="localVariablesSignature">Local variables signature handle.</param>
        /// <param name="attributes">Attributes.</param>
        /// <returns>The offset of the encoded body within the method body stream.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="instructionEncoder"/> has default value.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxStack"/> is out of range [0, <see cref="ushort.MaxValue"/>].</exception>
        /// <exception cref="InvalidOperationException">
        /// A label targeted by a branch in the instruction stream has not been marked,
        /// or the distance between a branch instruction and the target label doesn't fit the size of the instruction operand.
        /// </exception>
        public int AddMethodBody(
            InstructionEncoder instructionEncoder,
            int maxStack,
            StandaloneSignatureHandle localVariablesSignature,
            MethodBodyAttributes attributes)
            => AddMethodBody(instructionEncoder, maxStack, localVariablesSignature, attributes, hasDynamicStackAllocation: false);

        /// <summary>
        /// Encodes a method body and adds it to the method body stream.
        /// </summary>
        /// <param name="instructionEncoder">Instruction encoder.</param>
        /// <param name="maxStack">Max stack.</param>
        /// <param name="localVariablesSignature">Local variables signature handle.</param>
        /// <param name="attributes">Attributes.</param>
        /// <param name="hasDynamicStackAllocation">True if the method allocates from dynamic local memory pool (the IL contains <c>localloc</c> instruction).
        /// </param>
        /// <returns>The offset of the encoded body within the method body stream.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="instructionEncoder"/> has default value.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxStack"/> is out of range [0, <see cref="ushort.MaxValue"/>].</exception>
        /// <exception cref="InvalidOperationException">
        /// A label targeted by a branch in the instruction stream has not been marked,
        /// or the distance between a branch instruction and the target label doesn't fit the size of the instruction operand.
        /// </exception>
        public int AddMethodBody(
            InstructionEncoder instructionEncoder,
            int maxStack = 8,
            StandaloneSignatureHandle localVariablesSignature = default,
            MethodBodyAttributes attributes = MethodBodyAttributes.InitLocals,
            bool hasDynamicStackAllocation = false)
        {
            if (unchecked((uint)maxStack) > ushort.MaxValue)
            {
                Throw.ArgumentOutOfRange(nameof(maxStack));
            }

            // The branch fixup code expects the operands of branch instructions in the code builder to be contiguous.
            // That's true when we emit thru InstructionEncoder. Taking it as a parameter instead of separate
            // code and flow builder parameters ensures they match each other.
            var codeBuilder = instructionEncoder.CodeBuilder;
            var flowBuilder = instructionEncoder.ControlFlowBuilder;

            if (codeBuilder == null)
            {
                Throw.ArgumentNull(nameof(instructionEncoder));
            }

            int exceptionRegionCount = flowBuilder?.ExceptionHandlerCount ?? 0;
            if (!ExceptionRegionEncoder.IsExceptionRegionCountInBounds(exceptionRegionCount))
            {
                Throw.ArgumentOutOfRange(nameof(instructionEncoder), SR.TooManyExceptionRegions);
            }

            // Note (see also https://github.com/dotnet/runtime/issues/24948)
            //
            // We could potentially automatically determine whether a tiny method with no variables and InitLocals flag set
            // has localloc instruction and thus needs a fat header. We could parse the IL stored in codeBuilder.
            // However, it would unnecessarily slow down emit of virtually all tiny methods, which do not use localloc
            // and would only address uninitialized memory issues in very rare scenarios when the pointer returned by
            // localloc is not stored in a local variable but passed to a method call or stored in a field.
            //
            // Since emitting code with localloc is already a pretty advanced scenario that emits unsafe code
            // that can be potentially incorrect in many other ways we decide that it's not worth the complexity
            // and a perf regression to do so. Instead we rely on the caller to let us know if there is a localloc
            // in the code they emitted.

            int bodyOffset = SerializeHeader(codeBuilder.Count, (ushort)maxStack, exceptionRegionCount, attributes, localVariablesSignature, hasDynamicStackAllocation);

            if (flowBuilder?.BranchCount > 0)
            {
                flowBuilder.CopyCodeAndFixupBranches(codeBuilder, Builder);
            }
            else
            {
                codeBuilder.WriteContentTo(Builder);
            }

            flowBuilder?.SerializeExceptionTable(Builder);

            return bodyOffset;
        }

        private int SerializeHeader(
            int codeSize,
            ushort maxStack,
            int exceptionRegionCount,
            MethodBodyAttributes attributes,
            StandaloneSignatureHandle localVariablesSignature,
            bool hasDynamicStackAllocation)
        {
            const int TinyFormat = 2;
            const int FatFormat = 3;
            const int MoreSections = 8;
            const byte InitLocals = 0x10;

            bool initLocals = (attributes & MethodBodyAttributes.InitLocals) != 0;

            bool isTiny = codeSize < 64 &&
                          maxStack <= 8 &&
                          localVariablesSignature.IsNil && (!hasDynamicStackAllocation || !initLocals) &&
                          exceptionRegionCount == 0;

            int offset;
            if (isTiny)
            {
                offset = Builder.Count;
                Builder.WriteByte((byte)((codeSize << 2) | TinyFormat));
            }
            else
            {
                Builder.Align(4);

                offset = Builder.Count;

                ushort flags = (3 << 12) | FatFormat;
                if (exceptionRegionCount > 0)
                {
                    flags |= MoreSections;
                }

                if (initLocals)
                {
                    flags |= InitLocals;
                }

                Builder.WriteUInt16((ushort)((int)attributes | flags));
                Builder.WriteUInt16(maxStack);
                Builder.WriteInt32(codeSize);
                Builder.WriteInt32(localVariablesSignature.IsNil ? 0 : MetadataTokens.GetToken(localVariablesSignature));
            }

            return offset;
        }
    }
}
