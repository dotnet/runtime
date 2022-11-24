// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.IO;
using System.Reflection;
using Antlr4.Runtime;

namespace ILAssembler;
#pragma warning disable IDE0059
#pragma warning disable CA1822
internal sealed class DocumentParser
{
    public void Parse(Stream contents)
    {
        CILLexer lexer = new(new AntlrInputStream(contents));
        CILParser parser = new(new CommonTokenStream(lexer));
        var result = parser.decls();

    }
}
