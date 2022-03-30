// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
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
                .CreateSyntaxProvider(IsSyntaxTargetForGeneration, GetSemanticTargetForGeneration)
                .Where(x => x is not null);

            context.RegisterSourceOutput(eventSourceClasses, EmitSourceFile);
        }

        private sealed record EventSourceClass(string Namespace, string ClassName, string SourceName, Guid Guid);
    }
}
