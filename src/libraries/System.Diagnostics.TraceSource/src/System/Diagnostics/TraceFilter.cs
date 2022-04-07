// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics
{
    public abstract class TraceFilter
    {
        public abstract bool ShouldTrace(TraceEventCache? cache, string source, TraceEventType eventType, int id, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string? formatOrMessage,
                                         object?[]? args, object? data1, object?[]? data);

        internal bool ShouldTrace(TraceEventCache? cache, string source, TraceEventType eventType, int id, string? formatOrMessage)
        {
            return ShouldTrace(cache, source, eventType, id, formatOrMessage, null, null, null);
        }

        internal bool ShouldTrace(TraceEventCache? cache, string source, TraceEventType eventType, int id, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string? formatOrMessage, object?[]? args)
        {
            return ShouldTrace(cache, source, eventType, id, formatOrMessage, args, null, null);
        }

        internal bool ShouldTrace(TraceEventCache? cache, string source, TraceEventType eventType, int id, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string? formatOrMessage, object?[]? args, object? data1)
        {
            return ShouldTrace(cache, source, eventType, id, formatOrMessage, args, data1, null);
        }
    }
}
