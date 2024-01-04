// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Antlr4.Runtime;

namespace ILAssembler;
#pragma warning disable CA1822
internal sealed class DocumentParser
{
    public (ImmutableArray<Diagnostic>, PEBuilder?) Parse(SourceText document, Func<string, SourceText> includedDocumentLoader, Func<string, byte[]> resourceLocator, Options options)
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

        ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        preprocessor.OnPreprocessorSyntaxError += (source, start, length, msg) =>
        {
            diagnostics.Add(new Diagnostic("Preprocessor", DiagnosticSeverity.Error, msg, new Location(new(start, length), loadedDocuments[source])));
        };

        CILParser parser = new(new CommonTokenStream(lexer));
        var result = parser.decls();
        GrammarVisitor visitor = new GrammarVisitor(loadedDocuments, options, resourceLocator);
        _ = result.Accept(visitor);

        var image = visitor.BuildImage();

        bool anyErrors = diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        diagnostics.AddRange(image.Diagnostics);

        return (diagnostics.ToImmutable(), anyErrors ? null : image.Image);
    }
}
