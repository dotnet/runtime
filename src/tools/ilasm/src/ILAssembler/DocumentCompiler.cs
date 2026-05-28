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
public sealed class DocumentCompiler
{
    public (ImmutableArray<Diagnostic>, PEBuilder?) Compile(SourceText document, Func<string, SourceText> includedDocumentLoader, Func<string, byte[]> resourceLocator, Options options)
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

            var includedSource = new AntlrInputStream(includedDocument.Text)
            {
                name = includedDocument.Path
            };
            loadedDocuments.Add(includedDocument.Path, includedDocument);
            return new CILLexer(includedSource);
        });

        ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        preprocessor.OnPreprocessorSyntaxError += (source, start, length, msg) =>
        {
            diagnostics.Add(new Diagnostic("Preprocessor", DiagnosticSeverity.Error, msg, new Location(new(start, length), loadedDocuments[source])));
        };

        // Note: Parser must use the preprocessor token stream (not the raw lexer)
        // to properly handle #include, #define, and other preprocessor directives.
        CILParser parser = new(new CommonTokenStream(preprocessor));
        var result = parser.decls();
        GrammarVisitor visitor = new GrammarVisitor(loadedDocuments, options, resourceLocator);
        _ = result.Accept(visitor);

        var image = visitor.BuildImage();

        bool anyErrors = diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        anyErrors |= image.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        diagnostics.AddRange(image.Diagnostics);

        // In error-tolerant mode, return image even with errors
        bool returnImage = !anyErrors || options.ErrorTolerant;
        return (diagnostics.ToImmutable(), returnImage ? image.Image : null);
    }
}
