// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators
{
    [Generator]
    public partial class EventSourceGenerator : IIncrementalGenerator
    {
        // Example input:
        //
        //    [EventSource(Guid = "49592C0F-5A05-516D-AA4B-A64E02026C89", Name = "System.Runtime")]
        //    [EventSourceAutoGenerate]
        //    internal sealed partial class RuntimeEventSource : EventSource
        //
        // Example generated output:
        //
        //     using System;
        //
        //     namespace System.Diagnostics.Tracing
        //     {
        //         partial class RuntimeEventSource
        //         {
        //             private RuntimeEventSource() : base(new Guid(0x49592c0f,0x5a05,0x516d,0xaa,0x4b,0xa6,0x4e,0x02,0x02,0x6c,0x89), "System.Runtime") { }
        //
        //             private protected override ReadOnlySpan<byte> ProviderMetadata => new byte[] { 0x11, 0x0, 0x53, 0x79, 0x73, 0x74, 0x65, 0x6d, 0x2e, 0x52, 0x75, 0x6e, 0x74, 0x69, 0x6d, 0x65, 0x0, };
        //         }
        //     }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<EventSourceClass> eventSourceClasses =
                context.SyntaxProvider
                .CreateSyntaxProvider(static (x, _) => IsSyntaxTargetForGeneration(x), (x, _) => GetSemanticTargetForGeneration(x))
                .Where(x => x is not null)
                .Collect()
                .Combine(context.CompilationProvider)
                // Use a custom comparer that ignores the compilation.
                .WithComparer(ClassDeclarationsEqualityComparer.Instance)
                .SelectMany((x, cancellationToken) => Parser.GetEventSourceClasses(x.Left, x.Right, cancellationToken));

            context.RegisterSourceOutput(eventSourceClasses, static (spc, x) => Emitter.Emit(spc, x));
        }

        private static bool IsSyntaxTargetForGeneration(SyntaxNode node) =>
            node is ClassDeclarationSyntax x && x.AttributeLists.Count > 0;

        private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            // Only add classes annotated [EventSourceAutoGenerate] to reduce busy work.
            const string EventSourceAttribute = "EventSourceAutoGenerateAttribute";
            const string EventSourceAttributeShort = "EventSourceAutoGenerate";

            var classDeclaration = (ClassDeclarationSyntax)context.Node;

            // Check if has EventSource attribute before adding to candidates
            // as we don't want to add every class in the project
            foreach (AttributeListSyntax? cal in classDeclaration.AttributeLists)
            {
                foreach (AttributeSyntax? ca in cal.Attributes)
                {
                    // Check if Span length matches before allocating the string to check more
                    int length = ca.Name.Span.Length;
                    if (length != EventSourceAttribute.Length && length != EventSourceAttributeShort.Length)
                    {
                        continue;
                    }

                    // Possible match, now check the string value
                    string attrName = ca.Name.ToString();
                    if (attrName == EventSourceAttribute || attrName == EventSourceAttributeShort)
                    {
                        return classDeclaration;
                    }
                }
            }

            return null;
        }

        private sealed class ClassDeclarationsEqualityComparer : IEqualityComparer<(ImmutableArray<ClassDeclarationSyntax> Left, Compilation Right)>
        {
            public static readonly ClassDeclarationsEqualityComparer Instance = new();

            private ClassDeclarationsEqualityComparer() { }

            public bool Equals((ImmutableArray<ClassDeclarationSyntax>, Compilation) x, (ImmutableArray<ClassDeclarationSyntax>, Compilation) y)
            {
                var array1 = x.Item1;
                var array2 = y.Item1;

                if (array1.Length != array2.Length)
                    return false;
                for (int i = 0; i < array1.Length; i++)
                {
                    if (!array1[i].Equals(array2[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
            public int GetHashCode((ImmutableArray<ClassDeclarationSyntax>, Compilation) obj)
            {
                // Numbers taken from the FNV prime and offset basis.
                int hash = -2128831035;
                foreach (ClassDeclarationSyntax x in obj.Item1)
                {
                    // The implementation is not correct in the strict
                    // sense, since it needs to process each byte individually.
                    // But never mind, we are combining reference-based hash codes.
                    hash ^= x.GetHashCode();
                    hash *= 16777619;
                }
                return hash;
            }
        }

        private sealed class EventSourceClass
        {
            public string Namespace = string.Empty;
            public string ClassName = string.Empty;
            public string SourceName = string.Empty;
            public Guid Guid = Guid.Empty;
        }
    }
}
