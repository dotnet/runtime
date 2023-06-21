// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Compliance.Classification;

/// <summary>
/// Base attribute for data classification.
/// </summary>
[AttributeUsage(
    AttributeTargets.Field
    | AttributeTargets.Property
    | AttributeTargets.Parameter
    | AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Interface
    | AttributeTargets.ReturnValue
    | AttributeTargets.GenericParameter,
    AllowMultiple = true)]
#pragma warning disable CA1813 // Avoid unsealed attributes
public class DataClassificationAttribute : Attribute
#pragma warning restore CA1813 // Avoid unsealed attributes
{
    /// <summary>
    /// Gets or sets the notes.
    /// </summary>
    /// <remarks>Optional free-form text to provide context during a privacy audit.</remarks>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Gets the data class represented by this attribute.
    /// </summary>
    public DataClassification Classification { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataClassificationAttribute"/> class.
    /// </summary>
    /// <param name="classification">The data classification to apply.</param>
    protected DataClassificationAttribute(DataClassification classification)
    {
        Classification = classification;
    }
}
