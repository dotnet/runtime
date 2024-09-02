// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Private.CoreLib.Generators.Models;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators
{
    [Generator]
    public partial class EventSourceEventGenerator : IIncrementalGenerator
    {

#if false

        //Writing code...

        [EventSourceEventGenerate]
        internal sealed unsafe /*It's mandatory or in method write unsafe*/ partial class RuntimeEventSource : EventSource
        {
            [Event(1)]
            public partial void GoHome(string address,double usedTime);
        }


        //Will generated
        public void GoHome(string address,double usedTime)
        {
            global::System.Diagnostics.Tracing.EventSource.EventData* datas = stackalloc global::System.Diagnostics.Tracing.EventSource.EventData[2];
            datas[0] = new global::System.Diagnostics.Tracing.EventSource.EventData
            {
                //Use unsafe method because it is faster than fixed, and easily to generate
                DataPointer = address == null ? global::System.IntPtr.Zero : (nint)global::System.Runtime.CompilerServices.Unsafe.AsPointer(ref global::System.Runtime.InteropServices.MemoryMarshal.GetReference(global::System.MemoryExtensions.AsSpan(address))),
                Size = address == null ? 0 : checked((address.Length + 1) * sizeof(char))
            };
            datas[1] = new global::System.Diagnostics.Tracing.EventSource.EventData
            {
                DataPointer = (nint)(&usedTime),
                Size = sizeof(double)
            };
            WriteEventWithRelatedActivityIdCore(1, null, 2, datas);
            OnGoHome(i, address);
        }

        [global::System.Diagnostics.Tracing.NonEventAttribute]
        partial void OnGoHome(string address,double usedTime);

#endif

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<EventMethodsParsedResult> eventSourceClasses =
                context.SyntaxProvider.ForAttributeWithMetadataName(
                    KnowsAttributeNames.EventSourceEventGenerateAttribute,
                    (node, _) => node is ClassDeclarationSyntax,
                    (context, token) =>
                    {
                        return Parser.Parse((ClassDeclarationSyntax)context.TargetNode, context.SemanticModel, token);
                    })
                .Where(x => x is not null);

            context.RegisterSourceOutput(eventSourceClasses, (ctx, source) =>
            {
                var code = new StringBuilder();
                Emiitter.Emit(source, code);
                if (code.Length != 0)
                {
                    ctx.AddSource($"{source.ClassName}.Events.g.cs", code.ToString());
                }
            });
        }
    }
}
