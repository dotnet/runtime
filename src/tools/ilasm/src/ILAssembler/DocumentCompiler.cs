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
        return Compile([document], includedDocumentLoader, resourceLocator, options);
    }

    public (ImmutableArray<Diagnostic>, PEBuilder?) Compile(ImmutableArray<SourceText> documents, Func<string, SourceText> includedDocumentLoader, Func<string, byte[]> resourceLocator, Options options)
    {
        Dictionary<string, SourceText> loadedDocuments = new();
        ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        GrammarVisitor? visitor = null;
        IReadOnlyDictionary<string, string?>? definedVariables = null;

        foreach (var document in documents)
        {
            loadedDocuments[document.Path!] = document;

            var inputSource = new AntlrInputStream(document.Text)
            {
                name = document.Path
            };
            CILLexer lexer = new(inputSource);
            PreprocessedTokenSource preprocessor = new(lexer, path =>
            {
                var includedDocument = includedDocumentLoader(path);
                var includedSource = new AntlrInputStream(includedDocument.Text)
                {
                    name = includedDocument.Path
                };
                loadedDocuments[includedDocument.Path!] = includedDocument;
                return new CILLexer(includedSource);
            }, text => new CILLexer(new AntlrInputStream(text)), definedVariables);

            preprocessor.OnPreprocessorSyntaxError += (source, start, length, msg) =>
            {
                if (loadedDocuments.TryGetValue(source, out var sourceText))
                {
                    diagnostics.Add(new Diagnostic("Preprocessor", DiagnosticSeverity.Error, msg, new Location(new(start, length), sourceText)));
                }
                else
                {
                    diagnostics.Add(new Diagnostic("Preprocessor", DiagnosticSeverity.Error, msg, new Location(new(start, length), new SourceText("", source))));
                }
            };

            CILParser parser = new(new CommonTokenStream(preprocessor));
            var result = parser.decls();

            visitor ??= new GrammarVisitor(loadedDocuments, options, resourceLocator);

            _ = result.Accept(visitor);

            // Transfer defined constants to the next document
            definedVariables = preprocessor.DefinedVariables;
        }

        if (visitor is null)
        {
            return (diagnostics.ToImmutable(), null);
        }

        var image = visitor.BuildImage();

        bool anyErrors = diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        anyErrors |= image.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        diagnostics.AddRange(image.Diagnostics);

        // In error-tolerant mode, return image even with errors
        bool returnImage = !anyErrors || options.ErrorTolerant;
        return (diagnostics.ToImmutable(), returnImage ? image.Image : null);
    }
}
