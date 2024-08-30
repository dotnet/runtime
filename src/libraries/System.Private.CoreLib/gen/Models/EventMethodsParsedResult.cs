// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;

namespace System.Private.CoreLib.Generators.Models
{
    internal sealed class EventMethodsParsedResult
    {
        public EventMethodsParsedResult(string? @namespace, string className, ImmutableArray<EventMethod> methods, ImmutableArray<Diagnostic> diagnostics)
        {
            Debug.Assert(methods != null);
            Debug.Assert(diagnostics != null);
            Debug.Assert(className != null);

            Namespace = @namespace;
            ClassName = className;
            Methods = methods;
            Diagnostics = diagnostics;
        }
        /// <summary>
        /// Null is global
        /// </summary>
        public string? Namespace { get; }

        public string ClassName { get; }

        public ImmutableArray<EventMethod> Methods { get; }

        public ImmutableArray<string> ContextClassDeclarations { get; init; } = ImmutableArray<string>.Empty;

        public ImmutableArray<Diagnostic> Diagnostics { get; }
    }
}
