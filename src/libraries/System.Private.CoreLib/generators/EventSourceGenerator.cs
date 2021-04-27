// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators
{
    [Generator]
    public partial class EventSourceGenerator : ISourceGenerator
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

        public void Initialize(GeneratorInitializationContext context)
            => context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());

        public void Execute(GeneratorExecutionContext context)
        {
            SyntaxReceiver? receiver = context.SyntaxReceiver as SyntaxReceiver;
            if ((receiver?.CandidateClasses?.Count ?? 0) == 0)
            {
                // nothing to do yet
                return;
            }

            Parser? p = new Parser(context.Compilation, context.ReportDiagnostic, context.CancellationToken);
            EventSourceClass[]? eventSources = p.GetEventSourceClasses(receiver.CandidateClasses);

            if (eventSources?.Length > 0)
            {
                Emitter? e = new Emitter(context);
                e.Emit(eventSources, context.CancellationToken);
            }
        }

        private sealed class SyntaxReceiver : ISyntaxReceiver
        {
            private List<ClassDeclarationSyntax>? _candidateClasses;

            public List<ClassDeclarationSyntax>? CandidateClasses => _candidateClasses;

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // Only add classes annotated [EventSourceAutoGenerate] to reduce busy work.
                const string EventSourceAttribute = "EventSourceAutoGenerateAttribute";
                const string EventSourceAttributeShort = "EventSourceAutoGenerate";

                // Only clasess
                if (syntaxNode is ClassDeclarationSyntax classDeclaration)
                {
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
                                // Match add to candidates
                                _candidateClasses ??= new List<ClassDeclarationSyntax>();
                                _candidateClasses.Add(classDeclaration);
                                return;
                            }
                        }
                    }
                }
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
