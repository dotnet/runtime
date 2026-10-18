// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace ILAssembler;

public record Location(SourceSpan Span, SourceText Source)
{
    internal static Location From(Antlr4.Runtime.IToken token, IReadOnlyDictionary<string, SourceText> sourceDocuments)
    {
        string sourceName =
            token.TokenSource?.InputStream?.SourceName ??
            token.TokenSource?.SourceName ??
            string.Empty;
        SourceText source = sourceDocuments.TryGetValue(sourceName, out SourceText? sourceDocument)
            ? sourceDocument
            : new SourceText(string.Empty, sourceName);
        return new Location(GetSourceSpan(token), source);
    }

    internal static SourceSpan GetSourceSpan(Antlr4.Runtime.IToken? token)
    {
        if (token is null)
        {
            return new SourceSpan(0, 0);
        }

        int start = Math.Max(token.StartIndex, 0);
        int stop = Math.Max(token.StopIndex, start - 1);
        int length = stop < start
            ? 0
            : (int)Math.Min((long)stop - start + 1, int.MaxValue);
        return new SourceSpan(start, length);
    }
}
