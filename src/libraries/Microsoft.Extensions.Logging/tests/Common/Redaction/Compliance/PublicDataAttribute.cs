// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Compliance.Testing;

/// <summary>
/// Public data.
/// </summary>
public sealed class PublicDataAttribute : DataClassificationAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PublicDataAttribute"/> class.
    /// </summary>
    public PublicDataAttribute()
        : base(SimpleClassifications.PublicData)
    {
    }
}
