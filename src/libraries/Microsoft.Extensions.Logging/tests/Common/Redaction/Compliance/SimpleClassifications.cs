// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Compliance.Testing;

/// <summary>
/// Simple data classifications.
/// </summary>
public static class SimpleClassifications
{
    /// <summary>
    /// Gets the name of this classification taxonomy.
    /// </summary>
    public static string TaxonomyName => typeof(SimpleTaxonomy).FullName!;

    /// <summary>
    /// Gets the private data classification.
    /// </summary>
    public static DataClassification PrivateData => new(TaxonomyName, (ulong)SimpleTaxonomy.PrivateData);

    /// <summary>
    /// Gets the public data classification.
    /// </summary>
    public static DataClassification PublicData => new(TaxonomyName, (ulong)SimpleTaxonomy.PublicData);
}
