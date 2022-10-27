// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
    /// <summary>
    /// The diagnostic context may be entirely disabled or some kinds of warnings may be suppressed.
    /// The suppressions are determined based on the <paramref name="id"/>.
    /// Typically the suppressions will be based on diagnostic category <see cref="DiagnosticCategory"/>:
    ///  - Trimmer warnings (suppressed by RequiresUnreferencedCodeAttribute)
    ///  - AOT warnings (suppressed by RequiresDynamicCodeAttribute)
    ///  - Single-file warnings (suppressed by RequiresAssemblyFilesAttribute)
    /// Note that not all categories are used/supported by all tools, for example the ILLink only handles trimmer warnings and ignores the rest.
    /// </summary>
    readonly partial struct DiagnosticContext
    {
        /// <param name="id">The diagnostic ID, this will be used to determine the category of diagnostic (trimmer, AOT, single-file)</param>
        /// <param name="args">The arguments for diagnostic message.</param>
        public partial void AddDiagnostic(DiagnosticId id, params string[] args);

        /// <param name="id">The diagnostic ID, this will be used to determine the category of diagnostic (trimmer, AOT, single-file)</param>
        /// <param name="actualValue">The value for the source of the diagnostic</param>
        /// <param name="expectedAnnotationsValue">The value for the symbol causing the diagnostic</param>
        /// <param name="args">The arguments for diagnostic message.</param>
        public partial void AddDiagnostic(DiagnosticId id, ValueWithDynamicallyAccessedMembers actualValue, ValueWithDynamicallyAccessedMembers expectedAnnotationsValue, params string[] args);
    }
}
