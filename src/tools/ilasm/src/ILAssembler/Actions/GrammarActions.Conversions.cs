// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace ILAssembler
{
    internal abstract record GrammarResult
    {
        protected GrammarResult() { }

        public sealed record String(string Value) : GrammarResult;

        public sealed record Literal<T>(T Value) : GrammarResult;

        public sealed record SentinelValue
        {
            public static SentinelValue Instance { get; } = new();

            public static Literal<SentinelValue> Result { get; } = new(Instance);
        }
    }

#pragma warning disable CA1822 // Mark members as static
    internal sealed partial class GrammarActions
    {
        private readonly ImmutableArray<Diagnostic>.Builder _diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        private readonly EntityRegistry _entityRegistry = new();
        private readonly IReadOnlyDictionary<string, SourceText> _documents;
        private readonly Options _options;
        private readonly MetadataBuilder _metadataBuilder = new();
        private readonly Func<string, byte[]> _resourceLocator;

        // Record the mapped field data directly into the blob to ensure we preserve ordering
        private readonly BlobBuilder _mappedFieldData = new();
        private readonly Dictionary<string, int> _mappedFieldDataNames = new();
        private readonly Dictionary<string, List<Blob>> _mappedFieldDataReferenceFixups = new();
        private readonly BlobBuilder _manifestResources = new();
        private readonly Stack<SemanticRootFrame> _semanticRootFrames = new();
        private int _syntaxErrorCount;

        // Debug info tracking
        private Guid _currentLanguageGuid = Guid.Empty;
        private Guid _currentLanguageVendorGuid = Guid.Empty;
        private Guid _currentDocumentTypeGuid = Guid.Empty;
        private string? _currentDocumentPath;
        private readonly Dictionary<string, DocumentHandle> _documentHandles = new();
        private readonly MetadataBuilder _pdbBuilder = new();

        internal GrammarActions(IReadOnlyDictionary<string, SourceText> documents, Options options, Func<string, byte[]> resourceLocator)
        {
            _documents = documents;
            _options = options;
            _resourceLocator = resourceLocator;
        }
        private void ReportDiagnostic(DiagnosticSeverity severity, string id, string message, Antlr4.Runtime.ParserRuleContext context)
        {
            var location = Location.From(context.Start, _documents);
            _diagnostics.Add(new Diagnostic(id, severity, message, location));
        }

        private void ReportError(string id, string message, Antlr4.Runtime.ParserRuleContext context)
            => ReportDiagnostic(DiagnosticSeverity.Error, id, message, context);

        private void ReportError(string id, string message, IToken token)
        {
            _diagnostics.Add(new Diagnostic(
                id,
                DiagnosticSeverity.Error,
                message,
                Location.From(token, _documents)));
        }

        private void ReportWarning(string id, string message, Antlr4.Runtime.ParserRuleContext context)
            => ReportDiagnostic(DiagnosticSeverity.Warning, id, message, context);

        private void ReportWarning(string id, string message, IToken token)
        {
            _diagnostics.Add(new Diagnostic(
                id,
                DiagnosticSeverity.Warning,
                message,
                Location.From(token, _documents)));
        }

        private sealed record SemanticRootFrame(ParserRuleContext Owner, int InitialSyntaxErrorCount);

        internal void RecordSyntaxError() => _syntaxErrorCount++;

        internal void BeginSemanticRoot(ParserRuleContext context)
            => _semanticRootFrames.Push(new(context, _syntaxErrorCount));

        internal bool EndSemanticRoot(ParserRuleContext context)
        {
            Debug.Assert(_semanticRootFrames.Count > 0);
            SemanticRootFrame frame = _semanticRootFrames.Pop();
            Debug.Assert(ReferenceEquals(frame.Owner, context));
            return frame.InitialSyntaxErrorCount != _syntaxErrorCount || context.exception is not null;
        }

        private static bool IsRecoverableError(string diagnosticId)
        {
            // Method body and signature diagnostics are recoverable - we emit the assembly but report the error.
            // This matches native ilasm behavior where errors during method/field emission don't prevent
            // the assembly from being written when the /ERR (OnErrGo) flag is set.
            return diagnosticId is DiagnosticIds.ByteArrayTooShort
                or DiagnosticIds.ArgumentNotFound
                or DiagnosticIds.LocalNotFound
                or DiagnosticIds.LabelNotFound
                or DiagnosticIds.GenericParameterIndexOutOfRange
                or DiagnosticIds.ParameterIndexOutOfRange
                or DiagnosticIds.GenericParameterNotFound
                or DiagnosticIds.UnknownGenericParameter
                or DiagnosticIds.MissingInstanceCallConv;
        }

        private sealed class CurrentMethodContext
        {
            public CurrentMethodContext(EntityRegistry.MethodDefinitionEntity definition)
            {
                Definition = definition;
                // Populate argument names from the method's parameter definitions
                foreach (var param in definition.Parameters)
                {
                    if (param.Name is not null && param.Sequence > 0)
                    {
                        ArgumentNames[param.Name] = param.Sequence - 1;
                    }
                }
            }

            public EntityRegistry.MethodDefinitionEntity Definition { get; }

            public Dictionary<string, LabelHandle> Labels { get; } = new();

            public Dictionary<string, IToken> UndefinedLabelReferences { get; } = new();

            public Dictionary<string, int> ArgumentNames { get; } = new();

            public List<Dictionary<string, int>> LocalsScopes { get; } = new();

            public List<SignatureArg> AllLocals { get; } = new();
        }

        private CurrentMethodContext? _currentMethod;
        private EntityRegistry.FieldDefinitionEntity? _lastFieldDefinition;
        private EntityRegistry.EntityBase? _pendingClassCustomAttributeOwner;

        private const ushort CustomAttributeBlobFormatVersion = 1;

        private readonly Stack<string> _currentNamespace = new();

        private readonly Stack<EntityRegistry.TypeDefinitionEntity> _currentTypeDefinition = new();

        // Sentinel to distinguish "no constant" from "constant is null"
        private sealed class NoConstantSentinel
        {
            public static readonly NoConstantSentinel Instance = new();
            private NoConstantSentinel() { }
        }

        private bool _expectInstance;
        private Subsystem _subsystem = Subsystem.WindowsCui;
        private CorFlags _corflags = CorFlags.ILOnly;
        private int _alignment = 0x200;
        private long _imageBase = 0x00400000;
        private long _stackReserve;

    }
}
