// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    [AttributeUsage(
        AttributeTargets.Assembly | AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Event,
        AllowMultiple = true,
        Inherited = false)]
    public class ExpectedWarningAttribute : EnableLoggerAttribute
    {
        public ExpectedWarningAttribute(string warningCode, params string[] messageContains)
        {
        }

        public string FileName { get; set; }
        public int SourceLine { get; set; }
        public int SourceColumn { get; set; }

        /// <summary>
        /// Property used by the result checkers of trimmer and analyzers to determine whether
        /// the tool should have produced the specified warning on the annotated member.
        /// </summary>
        public ProducedBy ProducedBy { get; set; } = ProducedBy.TrimmerAnalyzerAndNativeAot;

        public bool CompilerGeneratedCode { get; set; }
    }
}
