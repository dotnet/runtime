// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using LibraryImportGenerator.UnitTests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Interop.Analyzers;
using static Microsoft.Interop.Analyzers.CustomMarshallerAttributeAnalyzer;

namespace LibraryImportGenerator.UnitTests
{
    internal class CustomMarshallerAttributeFixerTest : CSharpCodeFixVerifier<CustomMarshallerAttributeAnalyzer, CustomMarshallerAttributeFixer>.Test
    {
        // Sort the diagnostics in a deterministic order even when they only differ by diagnostic message.
        // In particular, sort the equivalent subgroups by their diagnostic descriptor in the order that the fixer's fix-all provider
        // will add the methods.
        // This ensures that the iterative code-fix test will produce the same (deterministic) output as the fix-all tests.
        protected override ImmutableArray<(Project project, Diagnostic diagnostic)> SortDistinctDiagnostics(IEnumerable<(Project project, Diagnostic diagnostic)> diagnostics)
            => diagnostics.OrderBy(d => d.diagnostic.Location.GetLineSpan().Path, StringComparer.Ordinal)
                .ThenBy(d => d.diagnostic.Location.SourceSpan.Start)
                .ThenBy(d => d.diagnostic.Location.SourceSpan.End)
                .ThenBy(d => d.diagnostic.Id)
                .ThenBy(d => d.diagnostic.Descriptor, Comparer.Instance)
                .ToImmutableArray();

        private class Comparer : IComparer<DiagnosticDescriptor>
        {
            public static readonly Comparer Instance = new();

            /// <summary>
            /// Checks if the provided descriptor matches the expected descriptor or the default marshal mode equivalent of the expected descriptor.
            /// </summary>
            /// <param name="descriptor"></param>
            /// <param name="expected"></param>
            /// <returns></returns>
            private static bool IsEquivalentDescriptor(DiagnosticDescriptor descriptor, DiagnosticDescriptor expected)
            {
                return descriptor.Equals(expected) || descriptor.Equals(DefaultMarshalModeDiagnostics.GetDefaultMarshalModeDiagnostic(expected));
            }

            private static int GetOrderIndexFromDescriptor(DiagnosticDescriptor descriptor)
            {
                // We'll order the descriptors in the following order for testing:
                // - FromManaged/ConvertToUnmanaged
                // - ToUnmanaged
                // - FromUnmanaged/ConvertToManaged
                // - ToManaged
                // - BufferSize
                // - AllocateContainerForUnmanagedElements
                // - AllocateContainerForManagedElements
                // - GetManagedValuesSource/GetUnmanagedValuesDestination
                // - GetUnmanagedValuesSource/GetManagedValuesDestination
                // - Free
                // This order corresponds to the order that the fix-all provider will add the methods.

                if (IsEquivalentDescriptor(descriptor, StatefulMarshallerRequiresFromManagedRule)
                    || IsEquivalentDescriptor(descriptor, StatelessValueInRequiresConvertToUnmanagedRule)
                    || IsEquivalentDescriptor(descriptor, StatelessLinearCollectionRequiresTwoParameterAllocateContainerForUnmanagedElementsRule))
                {
                    return 0;
                }
                if (IsEquivalentDescriptor(descriptor, StatefulMarshallerRequiresToUnmanagedRule))
                {
                    return 1;
                }
                if (IsEquivalentDescriptor(descriptor, StatefulMarshallerRequiresFromUnmanagedRule)
                    || IsEquivalentDescriptor(descriptor, StatelessRequiresConvertToManagedRule)
                    || IsEquivalentDescriptor(descriptor, StatelessLinearCollectionRequiresTwoParameterAllocateContainerForManagedElementsRule))
                {
                    return 2;
                }
                if (IsEquivalentDescriptor(descriptor, StatefulMarshallerRequiresToManagedRule))
                {
                    return 3;
                }
                if (IsEquivalentDescriptor(descriptor, CallerAllocFromManagedMustHaveBufferSizeRule)
                    || IsEquivalentDescriptor(descriptor, StatelessLinearCollectionCallerAllocFromManagedMustHaveBufferSizeRule))
                {
                    return 4;
                }
                if (IsEquivalentDescriptor(descriptor, StatelessLinearCollectionRequiresTwoParameterAllocateContainerForUnmanagedElementsRule))
                {
                    return 5;
                }
                if (IsEquivalentDescriptor(descriptor, StatelessLinearCollectionRequiresTwoParameterAllocateContainerForManagedElementsRule))
                {
                    return 6;
                }
                if (IsEquivalentDescriptor(descriptor, LinearCollectionInRequiresCollectionMethodsRule)
                    || IsEquivalentDescriptor(descriptor, StatelessLinearCollectionInRequiresCollectionMethodsRule))
                {
                    return 7;
                }
                if (IsEquivalentDescriptor(descriptor, LinearCollectionOutRequiresCollectionMethodsRule)
                    || IsEquivalentDescriptor(descriptor, StatelessLinearCollectionOutRequiresCollectionMethodsRule))
                {
                    return 8;
                }
                if (IsEquivalentDescriptor(descriptor, StatefulMarshallerRequiresFreeRule))
                {
                    return 9;
                }
                // Sort all unknown diagnostic descriptors later.
                return 10;
            }

            public int Compare(DiagnosticDescriptor? x, DiagnosticDescriptor? y)
            {
                // Sort null as less than non-null.
                if (x is null)
                {
                    return y is null ? 0 : -1;
                }
                if (y is null)
                {
                    return 1;
                }

                return GetOrderIndexFromDescriptor(x) - GetOrderIndexFromDescriptor(y);
            }
        }

        public static async Task VerifyCodeFixAsync(string source, string fixedSource, params DiagnosticResult[] diagnostics)
        {
            CustomMarshallerAttributeFixerTest test = new()
            {
                TestCode = source,
                FixedCode = fixedSource,
            };

            test.ExpectedDiagnostics.AddRange(diagnostics);

            await test.RunAsync();
        }
    }
}
