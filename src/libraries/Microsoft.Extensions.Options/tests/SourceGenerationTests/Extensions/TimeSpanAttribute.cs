// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
// using Microsoft.Shared.Diagnostics;

#pragma warning disable CA1716
namespace Microsoft.Shared.Data.Validation;
#pragma warning restore CA1716

/// <summary>
/// Provides boundary validation for <see cref="TimeSpan"/>.
/// </summary>
#if !SHARED_PROJECT
[ExcludeFromCodeCoverage]
#endif

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
[SuppressMessage("Design", "CA1019:Define accessors for attribute arguments", Justification = "Indirectly we are.")]
internal sealed class TimeSpanAttribute : ValidationAttribute
{
    /// <summary>
    /// Gets the lower bound for time span.
    /// </summary>
    public TimeSpan Minimum => _minMs.HasValue ? TimeSpan.FromMilliseconds((double)_minMs) : TimeSpan.Parse(_min!, CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the upper bound for time span.
    /// </summary>
    public TimeSpan? Maximum
    {
        get
        {
            if (_maxMs.HasValue)
            {
                return TimeSpan.FromMilliseconds((double)_maxMs);
            }
            else
            {
                return _max == null ? null : TimeSpan.Parse(_max, CultureInfo.InvariantCulture);
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the time span validation should exclude the minimum and maximum values.
    /// </summary>
    /// <remarks>
    /// By default the property is set to <c>false</c>.
    /// </remarks>
    public bool Exclusive { get; set; }

    private readonly int? _minMs;
    private readonly int? _maxMs;
    private readonly string? _min;
    private readonly string? _max;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeSpanAttribute"/> class.
    /// </summary>
    /// <param name="minMs">Minimum in milliseconds.</param>
    public TimeSpanAttribute(int minMs)
    {
        _minMs = minMs;
        _maxMs = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeSpanAttribute"/> class.
    /// </summary>
    /// <param name="minMs">Minimum in milliseconds.</param>
    /// <param name="maxMs">Maximum in milliseconds.</param>
    public TimeSpanAttribute(int minMs, int maxMs)
    {
        _minMs = minMs;
        _maxMs = maxMs;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeSpanAttribute"/> class.
    /// </summary>
    /// <param name="min">Minimum represented as time span string.</param>
    public TimeSpanAttribute(string min)
    {
        _ = ThrowHelper.IfNullOrWhitespace(min);

        _min = min;
        _max = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeSpanAttribute"/> class.
    /// </summary>
    /// <param name="min">Minimum represented as time span string.</param>
    /// <param name="max">Maximum represented as time span string.</param>
    public TimeSpanAttribute(string min, string max)
    {
        _ = ThrowHelper.IfNullOrWhitespace(min);
        _ = ThrowHelper.IfNullOrWhitespace(max);

        _min = min;
        _max = max;
    }

    /// <summary>
    /// Validates that a given value represents an in-range TimeSpan value.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="validationContext">Additional context for this validation.</param>
    /// <returns>A value indicating success or failure.</returns>
    protected override ValidationResult IsValid(object? value, ValidationContext? validationContext)
    {
        var min = Minimum;
        var max = Maximum;

        if (min >= max)
        {
            throw new InvalidOperationException($"{nameof(TimeSpanAttribute)} requires that the minimum value be less than the maximum value (see field {validationContext.GetDisplayName()})");
        }

        if (value == null)
        {
            // use the [Required] attribute to force presence
            return ValidationResult.Success!;
        }

        if (value is TimeSpan ts)
        {
            if (Exclusive && ts <= min)
            {
                return new ValidationResult($"The field {validationContext.GetDisplayName()} must be > to {min}.", validationContext.GetMemberName());
            }

            if (ts < min)
            {
                return new ValidationResult($"The field {validationContext.GetDisplayName()} must be >= to {min}.", validationContext.GetMemberName());
            }

            if (max.HasValue)
            {
                if (Exclusive && ts >= max.Value)
                {
                    return new ValidationResult($"The field {validationContext.GetDisplayName()} must be < to {max}.", validationContext.GetMemberName());
                }

                if (ts > max.Value)
                {
                    return new ValidationResult($"The field {validationContext.GetDisplayName()} must be <= to {max}.", validationContext.GetMemberName());
                }
            }

            return ValidationResult.Success!;
        }

        throw new InvalidOperationException($"{nameof(TimeSpanAttribute)} can only be used with fields of type TimeSpan (see field {validationContext.GetDisplayName()})");
    }
}
