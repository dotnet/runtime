// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor | AttributeTargets.Field,
        AllowMultiple = true,
        Inherited = false)]
    public class LogDoesNotContainAttribute : EnableLoggerAttribute
    {
        public LogDoesNotContainAttribute(string message, bool regexMatch = false)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Value cannot be null or empty.", nameof(message));
        }

        /// <summary>
        /// Property used by the result checkers of trimmer and analyzers to determine whether
        /// the tool should have produced the specified warning on the annotated member.
        /// </summary>
        public ProducedBy ProducedBy { get; set; } = ProducedBy.TrimmerAnalyzerAndNativeAot;
    }
}
