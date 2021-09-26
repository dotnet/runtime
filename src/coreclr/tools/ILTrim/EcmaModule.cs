// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace ILTrim
{
    public class EcmaModule
    {
        public PEReader PEReader { get; }
        public MetadataReader MetadataReader { get; }

        public EcmaModule(PEReader peReader, MetadataReader mdReader)
            => (PEReader, MetadataReader) = (peReader, mdReader);
    }
}
