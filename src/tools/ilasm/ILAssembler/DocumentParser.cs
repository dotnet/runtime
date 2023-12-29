// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using Antlr4.Runtime;

namespace ILAssembler;
#pragma warning disable IDE0059
#pragma warning disable CA1822
internal sealed class DocumentParser
{
    public void Parse(SourceText document, Func<string, SourceText> includedDocumentLoader, Func<string, byte[]> resourceLocator, Options options)
    {
        var inputSource = new AntlrInputStream(document.Text)
        {
            name = document.Path
        };
        CILLexer lexer = new(inputSource);
        Dictionary<string, SourceText> loadedDocuments = new()
        {
            {document.Path!, document }
        };
        PreprocessedTokenSource preprocessor = new(lexer, path =>
        {
            var includedDocument = includedDocumentLoader(path);

            var includedSource = new AntlrInputStream(document.Text)
            {
                name = document.Path
            };
            loadedDocuments.Add(document.Path, document);
            return new CILLexer(includedSource);
        });
        // TODO Handle preprocessor diagnostics.
        CILParser parser = new(new CommonTokenStream(lexer));
        var result = parser.decls();
        // TODO: Handle parse errors.
        var metadataBuilder = new MetadataBuilder();
        GrammarVisitor visitor = new GrammarVisitor(loadedDocuments, options, metadataBuilder, resourceLocator);
        _ = result.Accept(visitor);
        // TODO: Get result information out of visitor and create MetadataRootBuilder.
    }
}
