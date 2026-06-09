// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Represents the result of an <see cref="IServiceCollectionValidator"/> validation.
    /// </summary>
    public readonly struct ValidationResult
    {
        private static readonly string[] s_empty = Array.Empty<string>();

        private readonly string[]? _errors;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResult"/> struct with the specified errors.
        /// </summary>
        /// <param name="errors">The list of validation errors.</param>
        public ValidationResult(IReadOnlyList<string> errors)
        {
            ArgumentNullException.ThrowIfNull(errors);

            if (errors.Count == 0)
            {
                _errors = null;
            }
            else
            {
                _errors = new string[errors.Count];
                for (int i = 0; i < errors.Count; i++)
                {
                    _errors[i] = errors[i];
                }
            }
        }

        private ValidationResult(string[] errors)
        {
            _errors = errors.Length == 0 ? null : errors;
        }

        /// <summary>
        /// Gets a <see cref="ValidationResult"/> that represents a successful validation.
        /// </summary>
        public static ValidationResult Success { get; } = new ValidationResult(s_empty);

        /// <summary>
        /// Gets the list of validation errors.
        /// </summary>
        /// <value>
        /// A read-only list of error messages, or an empty list when validation succeeded.
        /// </value>
        public IReadOnlyList<string> Errors => _errors ?? s_empty;

        /// <summary>
        /// Gets a value that indicates whether the validation succeeded.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if validation succeeded and there are no errors; otherwise, <see langword="false" />.
        /// </value>
        public bool IsSuccess => _errors is null;

        /// <summary>
        /// Creates a <see cref="ValidationResult"/> that represents a failure with the specified error message.
        /// </summary>
        /// <param name="error">The validation error message.</param>
        /// <returns>A <see cref="ValidationResult"/> with the specified error.</returns>
        public static ValidationResult Fail(string error)
        {
            ArgumentNullException.ThrowIfNull(error);
            return new ValidationResult(new[] { error });
        }

        /// <summary>
        /// Creates a <see cref="ValidationResult"/> that represents a failure with the specified error messages.
        /// </summary>
        /// <param name="errors">The list of validation error messages.</param>
        /// <returns>A <see cref="ValidationResult"/> with the specified errors.</returns>
        public static ValidationResult Fail(IReadOnlyList<string> errors)
        {
            ArgumentNullException.ThrowIfNull(errors);
            if (errors.Count == 0)
            {
                return Success;
            }

            var copy = new string[errors.Count];
            for (int i = 0; i < errors.Count; i++)
            {
                copy[i] = errors[i];
            }

            return new ValidationResult(copy);
        }

        /// <summary>
        /// Combines two <see cref="ValidationResult"/> instances by aggregating their errors.
        /// </summary>
        /// <param name="left">The first <see cref="ValidationResult"/>.</param>
        /// <param name="right">The second <see cref="ValidationResult"/>.</param>
        /// <returns>
        /// A new <see cref="ValidationResult"/> whose <see cref="Errors"/> contains the errors from both operands.
        /// </returns>
        /// <remarks>
        /// Each application of this operator allocates a new array to hold the combined errors.
        /// When aggregating many <see cref="ValidationResult"/> values, prefer collecting all errors
        /// manually and constructing a single <see cref="ValidationResult"/> via
        /// <see cref="Fail(IReadOnlyList{string})"/> to avoid repeated allocations.
        /// </remarks>
        public static ValidationResult operator +(ValidationResult left, ValidationResult right)
        {
            if (left.IsSuccess)
            {
                return right;
            }

            if (right.IsSuccess)
            {
                return left;
            }

            var errors = new string[left._errors!.Length + right._errors!.Length];
            Array.Copy(left._errors, errors, left._errors.Length);
            Array.Copy(right._errors, 0, errors, left._errors.Length, right._errors.Length);
            return new ValidationResult(errors);
        }
    }
}
