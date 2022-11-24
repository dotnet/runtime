// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace ILAssembler;

public record Location(SourceSpan Span, SourceText Source)
{
    internal static Location From(Antlr4.Runtime.IToken token, SourceText source)
    {
        SourceSpan span = new(token.StartIndex, token.StopIndex - token.StartIndex + 1);
        return new Location(span, source);
    }
}
