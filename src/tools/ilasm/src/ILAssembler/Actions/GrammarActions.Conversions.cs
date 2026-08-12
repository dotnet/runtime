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
using Antlr4.Runtime.Tree;

namespace ILAssembler
{
    internal abstract record GrammarResult
    {
        protected GrammarResult() { }

        public sealed record String(string Value) : GrammarResult;

        public sealed record Literal<T>(T Value) : GrammarResult;

        public sealed record Sequence<T>(ImmutableArray<T> Value) : GrammarResult;

        /// <summary>
        /// A formatted blob of bytes.
        /// </summary>
        /// <param name="Value">The bytes of the blob.</param>
        public sealed record FormattedBlob(BlobBuilder Value) : GrammarResult;

        public sealed record SentinelValue
        {
            public static SentinelValue Instance { get; } = new();

            public static Literal<SentinelValue> Result { get; } = new(Instance);
        }

        public sealed record Flag<T>(T Value, bool ShouldAppend = true) : GrammarResult
            where T : struct, Enum
        {
            private readonly T _groupMask;
            public Flag(T value, bool shouldAppend, T groupMask)
                : this(value, shouldAppend)
            {
                _groupMask = groupMask;
            }
            public Flag(T value, T groupMask)
                : this(value)
            {
                _groupMask = groupMask;
            }

            public static T operator |(T lhs, Flag<T> rhs)
            {
                if (!rhs.ShouldAppend)
                {
                    return rhs.Value;
                }
                int lhsInt = Convert.ToInt32(lhs);
                int maskInt = Convert.ToInt32(rhs._groupMask);
                int valueInt = Convert.ToInt32(rhs.Value);
                return (T)Enum.ToObject(typeof(T), (lhsInt & ~maskInt) | valueInt);
            }
        }
    }

#pragma warning disable CA1822 // Mark members as static
    internal sealed partial class GrammarActions : ICILVisitor<GrammarResult>
    {
        private const string NodeShouldNeverBeDirectlyVisited = "This node should never be directly visited. It should be directly processed by its parent node.";
        private const string StructuralNodeIsDrivenByParserActions = "This node is processed incrementally by the parser semantic actions and must never be visited recursively.";
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

        // Typedef aliases - maps alias name to the resolved entity
        private readonly Dictionary<string, TypedefEntry> _typedefs = new();

        // Debug info tracking
        private Guid _currentLanguageGuid = Guid.Empty;
        private Guid _currentLanguageVendorGuid = Guid.Empty;
        private Guid _currentDocumentTypeGuid = Guid.Empty;
        private string? _currentDocumentPath;
        private readonly Dictionary<string, DocumentHandle> _documentHandles = new();
        private readonly MetadataBuilder _pdbBuilder = new();

        // VTable fixup tracking - uses types from VTableFixupSupport
        private readonly List<VTableFixupSupport.VTableFixupEntry> _vtableFixups = new();

        internal GrammarActions(IReadOnlyDictionary<string, SourceText> documents, Options options, Func<string, byte[]> resourceLocator)
        {
            _documents = documents;
            _options = options;
            _resourceLocator = resourceLocator;
        }
        /// <summary>
        /// Represents a typedef alias entry.
        /// </summary>
        private abstract record TypedefEntry
        {
            public sealed record Type(EntityRegistry.TypeEntity Entity) : TypedefEntry;
            public sealed record TypeBlob(BlobBuilder Blob) : TypedefEntry;
            public sealed record Member(EntityRegistry.EntityBase Entity) : TypedefEntry;
            public sealed record CustomAttribute(EntityRegistry.EntityBase Constructor, BlobBuilder Value) : TypedefEntry;
        }

        private void ReportDiagnostic(DiagnosticSeverity severity, string id, string message, Antlr4.Runtime.ParserRuleContext context)
        {
            var location = Location.From(context.Start, _documents);
            _diagnostics.Add(new Diagnostic(id, severity, message, location));
        }

        private void ReportError(string id, string message, Antlr4.Runtime.ParserRuleContext context)
            => ReportDiagnostic(DiagnosticSeverity.Error, id, message, context);

        private void ReportWarning(string id, string message, Antlr4.Runtime.ParserRuleContext context)
            => ReportDiagnostic(DiagnosticSeverity.Warning, id, message, context);

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

        public GrammarResult Visit(IParseTree tree) => tree.Accept(this);

        private EntityRegistry.AssemblyOrRefEntity? _currentAssemblyOrRef;

        public GrammarResult VisitChildren(IRuleNode node)
        {
            for (int i = 0; i < node.ChildCount; i++)
            {
                node.GetChild(i).Accept(this);
            }
            return GrammarResult.SentinelValue.Result;
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

            public HashSet<string> DeclaredLabels { get; } = new();

            public Dictionary<string, ParserRuleContext> UndefinedLabelReferences { get; } = new();

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

        public GrammarResult VisitErrorNode(IErrorNode node) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

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

        public GrammarResult VisitTerminal(ITerminalNode node) => throw new UnreachableException();
    }
}
