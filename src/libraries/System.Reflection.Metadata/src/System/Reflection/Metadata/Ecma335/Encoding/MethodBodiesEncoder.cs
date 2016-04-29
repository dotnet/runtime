// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Metadata.Ecma335
{
    public struct MethodBodiesEncoder
    {
        public BlobBuilder Builder { get; }

        public MethodBodiesEncoder(BlobBuilder builder = null)
        {
            if (builder == null)
            {
                builder = new BlobBuilder();
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

        public MethodBodyEncoder AddMethodBody(
            int maxStack = 8,
            int exceptionRegionCount = 0,
            StandaloneSignatureHandle localVariablesSignature = default(StandaloneSignatureHandle),
            MethodBodyAttributes attributes = MethodBodyAttributes.InitLocals)
        {
            if (unchecked((ushort)maxStack) > ushort.MaxValue)
            {
                Throw.ArgumentOutOfRange(nameof(maxStack));
            }

            if (exceptionRegionCount < 0)
            {
                Throw.ArgumentOutOfRange(nameof(exceptionRegionCount));
            }

            return new MethodBodyEncoder(Builder, (ushort)maxStack, exceptionRegionCount, localVariablesSignature, attributes);
        }
    }
}
