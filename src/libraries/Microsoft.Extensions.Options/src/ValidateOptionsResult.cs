// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Represents the result of an options validation.
    /// </summary>
    public class ValidateOptionsResult
    {
        /// <summary>
        /// Result when validation was skipped due to name not matching.
        /// </summary>
        public static readonly ValidateOptionsResult Skip = new ValidateOptionsResult() { Skipped = true };

        /// <summary>
        /// Validation was successful.
        /// </summary>
        public static readonly ValidateOptionsResult Success = new ValidateOptionsResult() { Succeeded = true };

        /// <summary>
        /// True if validation was successful.
        /// </summary>
        public bool Succeeded { get; protected set; }

        /// <summary>
        /// True if validation was not run.
        /// </summary>
        public bool Skipped { get; protected set; }

        /// <summary>
        /// True if validation failed.
        /// </summary>
        public bool Failed { get; protected set; }

        /// <summary>
        /// Used to describe why validation failed.
        /// </summary>
        public string FailureMessage { get; protected set; }

        /// <summary>
        /// Full list of failures (can be multiple).
        /// </summary>
        public IEnumerable<string> Failures { get; protected set; }

        /// <summary>
        /// Returns a failure result.
        /// </summary>
        /// <param name="failureMessage">The reason for the failure.</param>
        /// <returns>The failure result.</returns>
        public static ValidateOptionsResult Fail(string failureMessage)
            => new ValidateOptionsResult { Failed = true, FailureMessage = failureMessage, Failures = new string[] { failureMessage } };

        /// <summary>
        /// Returns a failure result.
        /// </summary>
        /// <param name="failures">The reasons for the failure.</param>
        /// <returns>The failure result.</returns>
        public static ValidateOptionsResult Fail(IEnumerable<string> failures)
            => new ValidateOptionsResult { Failed = true, FailureMessage = string.Join("; ", failures), Failures = failures };
    }
}
