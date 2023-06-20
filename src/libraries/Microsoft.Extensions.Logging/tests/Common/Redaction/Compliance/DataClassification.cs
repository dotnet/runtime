// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Compliance.Classification;

/// <summary>
/// Represents a set of data classes as a part of a data taxonomy.
/// </summary>
public readonly struct DataClassification : IEquatable<DataClassification>
{
    /// <summary>
    /// Represents unclassified data.
    /// </summary>
    public const ulong NoneTaxonomyValue = 0UL;

    /// <summary>
    /// Represents the unknown classification.
    /// </summary>
    public const ulong UnknownTaxonomyValue = 1UL << 63;

    /// <summary>
    /// Gets the value to represent data with no defined classification.
    /// </summary>
    public static DataClassification None => new(NoneTaxonomyValue);

    /// <summary>
    /// Gets the value to represent data with an unknown classification.
    /// </summary>
    public static DataClassification Unknown => new(UnknownTaxonomyValue);

    /// <summary>
    /// Gets the name of the taxonomy that recognizes this classification.
    /// </summary>
    public string TaxonomyName { get; }

    /// <summary>
    /// Gets the bit mask representing the data classes.
    /// </summary>
    public ulong Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataClassification"/> struct.
    /// </summary>
    /// <param name="taxonomyName">The name of the taxonomy this classification belongs to.</param>
    /// <param name="value">The taxonomy-specific bit vector representing the data classes.</param>
    /// <exception cref="ArgumentException">Bit 63, which corresponds to <see cref="UnknownTaxonomyValue"/>, is set in the <paramref name="value"/> value.</exception>
    public DataClassification(string taxonomyName, ulong value)
    {
        if (string.IsNullOrEmpty(taxonomyName))
        {
            throw new ArgumentNullException(nameof(taxonomyName));
        }

        TaxonomyName = taxonomyName;
        Value = value;

        if (((value & UnknownTaxonomyValue) != 0) || (value == NoneTaxonomyValue))
        {
            throw new ArgumentException($"Cannot create a classification with a value of 0x{value:x}.", nameof(value));
        }
    }

    private DataClassification(ulong taxonomyValue)
    {
        TaxonomyName = string.Empty;
        Value = taxonomyValue;
    }

    /// <summary>
    /// Checks if object is equal to this instance of <see cref="DataClassification"/>.
    /// </summary>
    /// <param name="obj">Object to check for equality.</param>
    /// <returns><see langword="true" /> if object instances are equal <see langword="false" /> otherwise.</returns>
    public override bool Equals(object? obj) => (obj is DataClassification dc) && Equals(dc);

    /// <summary>
    /// Checks if object is equal to this instance of <see cref="DataClassification"/>.
    /// </summary>
    /// <param name="other">Instance of <see cref="DataClassification"/> to check for equality.</param>
    /// <returns><see langword="true" /> if object instances are equal <see langword="false" /> otherwise.</returns>
    public bool Equals(DataClassification other) => other.TaxonomyName == TaxonomyName && other.Value == Value;

    /// <summary>
    /// Get the hash code the current instance.
    /// </summary>
    /// <returns>Hash code.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(TaxonomyName, Value);
    }

    /// <summary>
    /// Check if two instances are equal.
    /// </summary>
    /// <param name="left">Left argument of the comparison.</param>
    /// <param name="right">Right argument of the comparison.</param>
    /// <returns><see langword="true" /> if object instances are equal, or <see langword="false" /> otherwise.</returns>
    public static bool operator ==(DataClassification left, DataClassification right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Check if two instances are not equal.
    /// </summary>
    /// <param name="left">Left argument of the comparison.</param>
    /// <param name="right">Right argument of the comparison.</param>
    /// <returns><see langword="false" /> if object instances are equal, or <see langword="true" /> otherwise.</returns>
    public static bool operator !=(DataClassification left, DataClassification right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Combines together two data classifications.
    /// </summary>
    /// <param name="left">The first classification to combine.</param>
    /// <param name="right">The second classification to combine.</param>
    /// <returns>A new classification object representing the combination of the two input classifications.</returns>
    /// <exception cref="ArgumentException">The two classifications aren't part of the same taxonomy.</exception>
    public static DataClassification Combine(DataClassification left, DataClassification right)
    {
        if (string.IsNullOrEmpty(left.TaxonomyName))
        {
            return (left.Value == NoneTaxonomyValue) ? right : Unknown;
        }
        else if (string.IsNullOrEmpty(right.TaxonomyName))
        {
            return (right.Value == NoneTaxonomyValue) ? left : Unknown;
        }

        if (left.TaxonomyName != right.TaxonomyName)
        {
            throw new ArgumentException($"Mismatched data taxonomies: {left.TaxonomyName} and {right.TaxonomyName} cannot be combined", nameof(right));
        }

        return new(left.TaxonomyName, left.Value | right.Value);
    }

    /// <summary>
    /// Combines together two data classifications.
    /// </summary>
    /// <param name="left">The first classification to combine.</param>
    /// <param name="right">The second classification to combine.</param>
    /// <returns>A new classification object representing the combination of the two input classifications.</returns>
    /// <exception cref="ArgumentException">The two classifications aren't part of the same taxonomy.</exception>
    [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "It's called Combine")]
    public static DataClassification operator |(DataClassification left, DataClassification right)
    {
        return Combine(left, right);
    }
}
