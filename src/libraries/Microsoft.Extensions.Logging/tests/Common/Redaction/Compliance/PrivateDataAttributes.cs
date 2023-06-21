// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Compliance.Testing;

/// <summary>
/// Private data.
/// </summary>
public sealed class PrivateDataAttribute : DataClassificationAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PrivateDataAttribute"/> class.
    /// </summary>
    public PrivateDataAttribute()
        : base(SimpleClassifications.PrivateData)
    {
    }
}
