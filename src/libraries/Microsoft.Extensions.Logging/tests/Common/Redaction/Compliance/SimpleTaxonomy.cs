// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Compliance.Testing;

/// <summary>
/// Classes of data used for simple scenarios.
/// </summary>
[Flags]
public enum SimpleTaxonomy : ulong
{
    /// <summary>
    /// No data classification.
    /// </summary>
    None = DataClassification.NoneTaxonomyValue,

    /// <summary>
    /// This is public data.
    /// </summary>
    PublicData = 1 << 0,

    /// <summary>
    /// This is private data.
    /// </summary>
    PrivateData = 1 << 1,

    /// <summary>
    /// Unknown data classification, handle with care.
    /// </summary>
    Unknown = DataClassification.UnknownTaxonomyValue,
}
