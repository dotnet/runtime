// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;

namespace Internal.TypeSystem.Ecma
{
    /// <summary>
    /// Interface implemented by TypeSystemContext to provide MetadataStringDecoder
    /// instance for MetadataReaders created by the type system.
    /// </summary>
    public interface IMetadataStringDecoderProvider
    {
        MetadataStringDecoder GetMetadataStringDecoder();
    }
}
