// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace ILAssembler;

/// <summary>Represents a compiled portable executable image.</summary>
public sealed class CompilationResult
{
    private readonly PEBuilder _peBuilder;
    private readonly Blob _mvidFixup;

    internal CompilationResult(PEBuilder peBuilder, Blob mvidFixup)
    {
        _peBuilder = peBuilder;
        _mvidFixup = mvidFixup;
    }

    /// <summary>Serializes the compiled image into the specified builder.</summary>
    /// <param name="builder">The builder that receives the serialized image.</param>
    /// <returns>The content identifier of the serialized image.</returns>
    public BlobContentId Serialize(BlobBuilder builder)
    {
        BlobContentId contentId = _peBuilder.Serialize(builder);
        if (!_mvidFixup.IsDefault)
        {
            new BlobWriter(_mvidFixup).WriteGuid(contentId.Guid);
        }

        return contentId;
    }
}
