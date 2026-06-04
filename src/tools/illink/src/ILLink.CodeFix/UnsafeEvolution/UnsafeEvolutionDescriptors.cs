// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Microsoft.CodeAnalysis;

namespace ILLink.CodeFix.UnsafeEvolution
{
    /// <summary>
    /// Diagnostic identifiers consumed and produced by the unsafe-evolution analyzer and code fixers.
    /// </summary>
    /// <remarks>
    /// The <c>CS****</c> identifiers come from the C# compiler when the
    /// <c>updated-memory-safety-rules</c> feature is enabled. The <c>IL5***</c>
    /// identifiers are emitted by <see cref="UnsafeEvolutionAnalyzer"/> for cases the
    /// compiler does not flag on its own (such as unsafe modifiers on signatures that
    /// no longer need them).
    /// </remarks>
    public static class UnsafeEvolutionDescriptors
    {
        public const string Category = "MemorySafety";

        // ---- Compiler diagnostics consumed by IntroduceUnsafeBlockCodeFixProvider ----

        /// <summary>Legacy: pointer used outside an <c>unsafe</c> context.</summary>
        public const string PointersAndFixedBuffersUnsafe = "CS0214";

        /// <summary>Operation requires unsafe context (e.g. pointer dereference).</summary>
        public const string UnsafeOperation = "CS9360";

        /// <summary>Uninitialized <c>stackalloc</c> -&gt; <c>Span&lt;T&gt;</c> with <c>SkipLocalsInit</c>.</summary>
        public const string UnsafeUninitializedStackAlloc = "CS9361";

        /// <summary>Call of a <em>requires-unsafe</em> member outside an unsafe context.</summary>
        public const string UnsafeMemberOperation = "CS9362";

        /// <summary>Compat-mode call of a member that contains pointers in its signature.</summary>
        public const string UnsafeMemberOperationCompat = "CS9363";

        // ---- Compiler diagnostics consumed by RemoveUnsafeModifierCodeFixProvider ----

        /// <summary>Warning emitted by the compiler for meaningless <c>unsafe</c> modifiers.</summary>
        public const string UnsafeMeaningless = "CS9377";

        // ---- ILLink analyzer diagnostics ----

        /// <summary>
        /// Meaningless <c>unsafe</c> modifier on a type, static constructor, destructor or delegate declaration.
        /// </summary>
        public const string MeaninglessUnsafeModifierId = "IL5005";

        /// <summary>
        /// Probably-unnecessary <c>unsafe</c> modifier on a member whose signature contains no pointer types.
        /// </summary>
        public const string UnnecessaryUnsafeModifierId = "IL5006";

        public static readonly DiagnosticDescriptor MeaninglessUnsafeModifier = new(
            id: MeaninglessUnsafeModifierId,
            title: "The 'unsafe' modifier has no effect on this declaration",
            messageFormat: "The 'unsafe' modifier has no effect on {0} '{1}' and should be removed",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description:
                "Under the updated memory-safety rules, the 'unsafe' modifier is meaningless on type, " +
                "delegate, static constructor and destructor declarations. Remove it.");

        public static readonly DiagnosticDescriptor UnnecessaryUnsafeModifier = new(
            id: UnnecessaryUnsafeModifierId,
            title: "The 'unsafe' modifier on this signature is probably unnecessary",
            messageFormat: "The 'unsafe' modifier on '{0}' is probably unnecessary; its signature contains no pointer types",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description:
                "Under the updated memory-safety rules, marking a member as 'unsafe' makes it requires-unsafe; " +
                "callers must use an 'unsafe' context. If the signature does not expose pointer types, the modifier " +
                "is probably not needed. Review and remove it if appropriate.");

        // ---- Wording inserted by IntroduceUnsafeBlockCodeFixProvider ----

        public const string SafetyTodoCommentText = "// SAFETY-TODO: Audit";
    }
}
#endif
