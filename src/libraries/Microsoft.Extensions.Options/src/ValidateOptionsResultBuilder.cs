// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Builds <see cref="ValidateOptionsResult"/> with support for multiple error messages.
    /// </summary>
    [DebuggerDisplay("{ErrorsCount} errors")]
    public class ValidateOptionsResultBuilder
    {
        private const string MemberSeparatorString = ", ";

        private List<string>? _errors;

        /// <summary>
        /// Creates new instance of the <see cref="ValidateOptionsResultBuilder"/> class.
        /// </summary>
        public ValidateOptionsResultBuilder() { }

        /// <summary>
        /// Adds a new validation error to the builder.
        /// </summary>
        /// <param name="error">Content of error message.</param>
        /// <param name="propertyName">The property in the option object which contains an error.</param>
        public void AddError(string error, string? propertyName = null)
        {
            ThrowHelper.ThrowIfNull(error);
            Errors.Add(propertyName is null ? error : $"Property {propertyName}: {error}");
        }

        /// <summary>
        /// Adds any validation error carried by the <see cref="ValidationResult"/> instance to this instance.
        /// </summary>
        /// <param name="result">The instance to append the error from.</param>
        public void AddResult(ValidationResult? result)
        {
            if (result?.ErrorMessage is not null)
            {
                string joinedMembers = string.Join(MemberSeparatorString, result.MemberNames);
                Errors.Add(joinedMembers.Length != 0
                    ? $"{joinedMembers}: {result.ErrorMessage}"
                    : result.ErrorMessage);
            }
        }

        /// <summary>
        /// Adds any validation error carried by the enumeration of <see cref="ValidationResult"/> instances to this instance.
        /// </summary>
        /// <param name="results">The enumeration to consume the errors from.</param>
        public void AddResults(IEnumerable<ValidationResult?>? results)
        {
            if (results != null)
            {
                foreach (ValidationResult? result in results)
                {
                    AddResult(result);
                }
            }
        }

        /// <summary>
        /// Adds any validation errors carried by the <see cref="ValidateOptionsResult"/> instance to this instance.
        /// </summary>
        /// <param name="result">The instance to consume the errors from.</param>
        public void AddResult(ValidateOptionsResult result)
        {
            ThrowHelper.ThrowIfNull(result);

            if (result.Failed)
            {
                if (result.Failures is null)
                {
                    Errors.Add(result.FailureMessage);
                }
                else
                {
                    // We are adding each failure separately to have the right failures count in _errors list.
                    // Otherwise we could add result.FailureMessage as one failure containing all result failures.
                    foreach (var failure in result.Failures)
                    {
                        if (failure is not null)
                        {
                            Errors.Add(failure);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Builds <see cref="ValidateOptionsResult"/> based on provided data.
        /// </summary>
        /// <returns>New instance of <see cref="ValidateOptionsResult"/>.</returns>
        public ValidateOptionsResult Build()
        {
            if (_errors?.Count > 0)
            {
                return ValidateOptionsResult.Fail(_errors);
            }

            return ValidateOptionsResult.Success;
        }

        /// <summary>
        /// Reset the builder to the empty state
        /// </summary>
        public void Clear() => _errors?.Clear();

        private int ErrorsCount => _errors is null ? 0 : _errors.Count;

        private List<string> Errors => _errors ??= new();
    }
}
