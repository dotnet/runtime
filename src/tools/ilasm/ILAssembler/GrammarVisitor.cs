// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Xml.XPath;
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
                return (T)(object)(((int)(object)lhs & (~(int)(object)rhs._groupMask)) | (int)(object)rhs.Value);
            }
        }
    }

#pragma warning disable CA1822 // Mark members as static
    internal sealed class GrammarVisitor : ICILVisitor<GrammarResult>
    {
        private const string NodeShouldNeverBeDirectlyVisited = "This node should never be directly visited. It should be directly processed by its parent node.";
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

        public GrammarVisitor(IReadOnlyDictionary<string, SourceText> documents, Options options, Func<string, byte[]> resourceLocator)
        {
            _documents = documents;
            _options = options;
            _resourceLocator = resourceLocator;
        }

        public (ImmutableArray<Diagnostic> Diagnostics, PEBuilder? Image) BuildImage()
        {
            if (_diagnostics.Any(diag => diag.Severity == DiagnosticSeverity.Error))
            {
                return (_diagnostics.ToImmutable(), null);
            }

            BlobBuilder ilStream = new();
            _entityRegistry.WriteContentTo(_metadataBuilder, ilStream);
            MetadataRootBuilder rootBuilder = new(_metadataBuilder);
            PEHeaderBuilder header = new(
                fileAlignment: _alignment,
                imageBase: (ulong)_imageBase,
                subsystem: _subsystem);

            MethodDefinitionHandle entryPoint = default;
            if (_entityRegistry.EntryPoint is not null)
            {
                entryPoint = (MethodDefinitionHandle)_entityRegistry.EntryPoint.Handle;
            }

            ManagedPEBuilder peBuilder = new(
                header,
                rootBuilder,
                ilStream,
                _mappedFieldData,
                _manifestResources,
                flags: CorFlags.ILOnly, entryPoint: entryPoint);

            return (_diagnostics.ToImmutable(), peBuilder);
        }

        public GrammarResult Visit(IParseTree tree) => tree.Accept(this);

        GrammarResult ICILVisitor<GrammarResult>.VisitAlignment(CILParser.AlignmentContext context) => VisitAlignment(context);
        public GrammarResult.Literal<int> VisitAlignment(CILParser.AlignmentContext context)
        {
            return VisitInt32(context.int32());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitAsmAttr(CILParser.AsmAttrContext context) => VisitAsmAttr(context);
        public GrammarResult.Literal<AssemblyFlags> VisitAsmAttr(CILParser.AsmAttrContext context)
            => new(context.asmAttrAny().Select(VisitAsmAttrAny).Aggregate((AssemblyFlags)0, (lhs, rhs) => lhs | rhs));
        GrammarResult ICILVisitor<GrammarResult>.VisitAsmAttrAny(CILParser.AsmAttrAnyContext context) => VisitAsmAttrAny(context);
        public GrammarResult.Flag<AssemblyFlags> VisitAsmAttrAny(CILParser.AsmAttrAnyContext context)
        {
            return context.GetText() switch
            {
                "retargetable" => new(AssemblyFlags.Retargetable),
                "windowsruntime" => new(AssemblyFlags.WindowsRuntime),
                "noplatform" => new((AssemblyFlags)0x70),
                "legacy library" => new(0),
                "cil" => new(GetFlagForArch(ProcessorArchitecture.MSIL), (AssemblyFlags)0xF0),
                "x86" => new(GetFlagForArch(ProcessorArchitecture.X86), (AssemblyFlags)0xF0),
                "amd64" => new(GetFlagForArch(ProcessorArchitecture.Amd64), (AssemblyFlags)0xF0),
                "arm" => new(GetFlagForArch(ProcessorArchitecture.Arm), (AssemblyFlags)0xF0),
                "arm64" => new(GetFlagForArch((ProcessorArchitecture)6), (AssemblyFlags)0xF0),
                _ => throw new InvalidOperationException("unreachable")
            };
        }

        private static AssemblyFlags GetFlagForArch(ProcessorArchitecture arch)
        {
            return (AssemblyFlags)((int)arch << 4);
        }

        private static (ProcessorArchitecture, AssemblyFlags) GetArchAndFlags(AssemblyFlags flags)
        {
            var arch = (ProcessorArchitecture)(((int)flags & 0xF0) >> 4);
            var newFlags = flags & ~((AssemblyFlags)((int)arch << 4));
            return (arch, newFlags);
        }

        private EntityRegistry.AssemblyOrRefEntity? _currentAssemblyOrRef;
        public GrammarResult VisitAsmOrRefDecl(CILParser.AsmOrRefDeclContext context)
        {
            Debug.Assert(_currentAssemblyOrRef is not null);

            if (context.customAttrDecl() is { } attr)
            {
                var customAttr = VisitCustomAttrDecl(attr).Value;
                if (customAttr is not null)
                {
                    customAttr.Owner = _currentAssemblyOrRef;
                }
                return GrammarResult.SentinelValue.Result;
            }

            string decl = context.GetChild(0).GetText();
            if (decl == ".publicKey")
            {
                BlobBuilder blob = new();
                blob.WriteBytes(VisitBytes(context.bytes()).Value);
                _currentAssemblyOrRef!.PublicKeyOrToken = blob;
            }
            else if (decl == ".ver")
            {
                var versionComponents = context.intOrWildcard();
                _currentAssemblyOrRef!.Version = new Version(
                    VisitIntOrWildcard(versionComponents[0]).Value ?? 0,
                    VisitIntOrWildcard(versionComponents[1]).Value ?? 0,
                    VisitIntOrWildcard(versionComponents[2]).Value ?? 0,
                    VisitIntOrWildcard(versionComponents[3]).Value ?? 0);
            }
            else if (decl == ".locale")
            {
                _currentAssemblyOrRef!.Culture = context.compQstring() is { } compQstring
                    ? VisitCompQstring(compQstring).Value
                    : Encoding.Unicode.GetString([.. VisitBytes(context.bytes()).Value]);
            }
            return GrammarResult.SentinelValue.Result;
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitAssemblyBlock(CILParser.AssemblyBlockContext context) => VisitAssemblyBlock(context);
        public GrammarResult VisitAssemblyBlock(CILParser.AssemblyBlockContext context)
        {
            _entityRegistry.Assembly ??= new EntityRegistry.AssemblyEntity(VisitDottedName(context.dottedName()).Value);
            var attr = VisitAsmAttr(context.asmAttr()).Value;
            (_entityRegistry.Assembly.ProcessorArchitecture, _entityRegistry.Assembly.Flags) = GetArchAndFlags(attr);
            foreach (var decl in context.assemblyDecls().assemblyDecl())
            {
                VisitAssemblyDecl(decl);
            }
            return GrammarResult.SentinelValue.Result;
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitAssemblyDecl(CILParser.AssemblyDeclContext context) => VisitAssemblyDecl(context);
        public GrammarResult VisitAssemblyDecl(CILParser.AssemblyDeclContext context)
        {
            if (context.secDecl() is { } secDecl)
            {
                var declarativeSecurity = VisitSecDecl(secDecl);
                if (declarativeSecurity.Value is { } sec)
                {
                    sec.Parent = _entityRegistry.Assembly;
                }
            }
            else if (context.int32() is { } hashAlg)
            {
                _entityRegistry.Assembly!.HashAlgorithm = (AssemblyHashAlgorithm)VisitInt32(hashAlg).Value;
            }
            else if (context.asmOrRefDecl() is { } asmOrRef)
            {
                _currentAssemblyOrRef = _entityRegistry.Assembly;
                VisitAsmOrRefDecl(asmOrRef);
                _currentAssemblyOrRef = null;
            }
            return GrammarResult.SentinelValue.Result;
        }
        public GrammarResult VisitAssemblyDecls(CILParser.AssemblyDeclsContext context) => throw new InvalidOperationException(NodeShouldNeverBeDirectlyVisited);
        public GrammarResult VisitAssemblyRefDecl(CILParser.AssemblyRefDeclContext context)
        {
            if (context.asmOrRefDecl() is { } asmOrRef)
            {
                VisitAsmOrRefDecl(asmOrRef);
            }
            string decl = context.GetChild(0).GetText();
            if (decl == ".hash")
            {
                var blob = new BlobBuilder();
                blob.WriteBytes(VisitBytes(context.bytes()).Value);
                ((EntityRegistry.AssemblyReferenceEntity)_currentAssemblyOrRef!).Hash = blob;
            }
            if (decl == ".publickeytoken")
            {
                var blob = new BlobBuilder();
                blob.WriteBytes(VisitBytes(context.bytes()).Value);
                _currentAssemblyOrRef!.PublicKeyOrToken = blob;
            }
            return GrammarResult.SentinelValue.Result;
        }
        public GrammarResult VisitAssemblyRefDecls(CILParser.AssemblyRefDeclsContext context) => throw new InvalidOperationException(NodeShouldNeverBeDirectlyVisited);
        GrammarResult ICILVisitor<GrammarResult>.VisitAssemblyRefHead(CILParser.AssemblyRefHeadContext context) => VisitAssemblyRefHead(context);
        public GrammarResult.Literal<EntityRegistry.AssemblyReferenceEntity> VisitAssemblyRefHead(CILParser.AssemblyRefHeadContext context)
        {
            var (arch, flags) = GetArchAndFlags(VisitAsmAttr(context.asmAttr()).Value);
            var dottedNames = context.dottedName();
            string name = VisitDottedName(dottedNames[0]).Value;
            string alias = name;
            if (dottedNames.Length > 1)
            {
                alias = VisitDottedName(dottedNames[1]).Value;
            }
            return new(_entityRegistry.GetOrCreateAssemblyReference(alias, asmref =>
            {
                asmref.Name = name;
                asmref.Flags = flags;
                asmref.ProcessorArchitecture = arch;
            }));
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitAtOpt(CILParser.AtOptContext context) => VisitAtOpt(context);
        public static GrammarResult.Literal<string?> VisitAtOpt(CILParser.AtOptContext context) => context.id() is {} id ? new(VisitId(id).Value) : new(null);

        GrammarResult ICILVisitor<GrammarResult>.VisitBoolSeq(CILParser.BoolSeqContext context) => VisitBoolSeq(context);
        public static GrammarResult.FormattedBlob VisitBoolSeq(CILParser.BoolSeqContext context)
        {
            var builder = ImmutableArray.CreateBuilder<bool>();

            foreach (var item in context.truefalse())
            {
                builder.AddRange(VisitTruefalse(item).Value);
            }

            return new(builder.ToImmutable().SerializeSequence());
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitBound(CILParser.BoundContext context) => VisitBound(context);
        public GrammarResult.Literal<(int? Lower, int? Upper)> VisitBound(CILParser.BoundContext context)
        {
            bool hasEllipsis = context.ELLIPSIS() is not null;
            if (context.ChildCount == 0 || (context.ChildCount == 1 && hasEllipsis))
            {
                return new((null, null));
            }

            var indicies = context.int32();

            int firstValue = VisitInt32(indicies[0]).Value;

            return (indicies.Length, hasEllipsis) switch
            {
                (1, false) => new((0, firstValue)),
                (1, true) => new((firstValue, null)),
                (2, false) => new((firstValue, VisitInt32(indicies[1]).Value - firstValue + 1)),
                _ => throw new InvalidOperationException("unreachable")
            };
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitBounds(CILParser.BoundsContext context) => VisitBounds(context);
        public GrammarResult.Sequence<(int? Lower, int? Upper)> VisitBounds(CILParser.BoundsContext context)
        {
            return new(context.bound().Select(bound => VisitBound(bound).Value).ToImmutableArray());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitBytes(CILParser.BytesContext context) => VisitBytes(context);
        public static GrammarResult.Sequence<byte> VisitBytes(CILParser.BytesContext context)
        {
            var builder = ImmutableArray.CreateBuilder<byte>();

            foreach (var item in context.hexbytes())
            {
                builder.AddRange(VisitHexbytes(item).Value);
            }

            return new(builder.ToImmutable());
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitCallConv(CILParser.CallConvContext context) => VisitCallConv(context);
        public GrammarResult.Literal<byte> VisitCallConv(CILParser.CallConvContext context)
        {
            if (context.callKind() is CILParser.CallKindContext callKind)
            {
                return new((byte)VisitCallKind(callKind).Value);
            }
            else if (context.int32() is CILParser.Int32Context int32)
            {
                return new((byte)VisitInt32(int32).Value);
            }
            else if (context.INSTANCE() is not null)
            {
                return new((byte)(VisitCallConv(context.callConv()).Value | (byte)SignatureAttributes.Instance));
            }
            else if (context.EXPLICIT() is not null)
            {
                return new((byte)(VisitCallConv(context.callConv()).Value | (byte)SignatureAttributes.ExplicitThis));
            }
            return new(0);
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitCallKind(CILParser.CallKindContext context) => VisitCallKind(context);
        public GrammarResult.Literal<SignatureCallingConvention> VisitCallKind(CILParser.CallKindContext context)
        {
            int childType = context.GetChild<ITerminalNode>(context.ChildCount - 1).Symbol.Type;
            return new(childType switch
            {
                CILParser.DEFAULT => SignatureCallingConvention.Default,
                CILParser.VARARG => SignatureCallingConvention.VarArgs,
                CILParser.CDECL => SignatureCallingConvention.CDecl,
                CILParser.STDCALL => SignatureCallingConvention.StdCall,
                CILParser.THISCALL => SignatureCallingConvention.ThisCall,
                CILParser.FASTCALL => SignatureCallingConvention.FastCall,
                CILParser.UNMANAGED => SignatureCallingConvention.Unmanaged,
                _ => throw new InvalidOperationException("unreachable")
            });
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitCatchClause(CILParser.CatchClauseContext context) => VisitCatchClause(context);
        public GrammarResult.Literal<EntityRegistry.TypeEntity> VisitCatchClause(CILParser.CatchClauseContext context) => VisitTypeSpec(context.typeSpec());

        GrammarResult ICILVisitor<GrammarResult>.VisitCaValue(CILParser.CaValueContext context) => VisitCaValue(context);
        public GrammarResult.FormattedBlob VisitCaValue(CILParser.CaValueContext context)
        {
            BlobBuilder blob = new();
            if (context.truefalse() is CILParser.TruefalseContext truefalse)
            {
                blob.WriteByte((byte)SerializationTypeCode.Boolean);
                blob.WriteBoolean(VisitTruefalse(truefalse).Value);
            }
            else if (context.compQstring() is CILParser.CompQstringContext str)
            {
                blob.WriteUTF8(VisitCompQstring(str).Value);
                blob.WriteByte(0);
            }
            else if (context.className() is CILParser.ClassNameContext className)
            {
                var name = VisitClassName(className).Value;
                blob.WriteByte((byte)SerializationTypeCode.Enum);
                blob.WriteUTF8((name as EntityRegistry.IHasReflectionNotation)?.ReflectionNotation ?? "");
                blob.WriteByte(0);
                byte size = 4;
                if (context.INT8() is not null)
                {
                    size = 1;
                }
                else if (context.INT16() is not null)
                {
                    size = 2;
                }
                blob.WriteByte(size);
                blob.WriteInt32(VisitInt32(context.int32()).Value);
            }
            else
            {
                blob.WriteByte((byte)SerializationTypeCode.Int32);
                blob.WriteInt32(VisitInt32(context.int32()).Value);
            }
            return new(blob);
        }

        public GrammarResult VisitChildren(IRuleNode node)
        {
            for (int i = 0; i < node.ChildCount; i++)
            {
                node.GetChild(i).Accept(this);
            }
            return GrammarResult.SentinelValue.Result;
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitClassAttr(CILParser.ClassAttrContext context) => VisitClassAttr(context);

        public GrammarResult.Literal<(GrammarResult.Flag<TypeAttributes> Attribute, EntityRegistry.WellKnownBaseType? FallbackBase, bool RequireSealed)> VisitClassAttr(CILParser.ClassAttrContext context)
        {
            if (context.int32() is CILParser.Int32Context int32)
            {
                int value = VisitInt32(int32).Value;
                // COMPAT: The VALUE and ENUM keywords use sentinel values to pass through the fallback base type
                // in ILASM. These sentinel values can be provided through the "pass the value of the flag" feature,
                // so we detect those old flags here and provide the correct fallback type.
                bool requireSealed = false;
                EntityRegistry.WellKnownBaseType? fallbackBase = null;
                if ((value & 0x80000000) != 0)
                {
                    requireSealed = true;
                    fallbackBase = EntityRegistry.WellKnownBaseType.System_ValueType;
                }
                if ((value & 0x40000000) != 0)
                {
                    fallbackBase = EntityRegistry.WellKnownBaseType.System_Enum;
                }
                // Mask off the sentinel bits
                value &= unchecked((int)~0xC0000000);
                // COMPAT: When explicit flags are provided they always supercede previously set flags
                // (other than the sentinel values)
                return new((new((TypeAttributes)value, ShouldAppend: false), fallbackBase, requireSealed));
            }

            if (context.ENUM() is not null)
            {
                // COMPAT: ilasm implies the Sealed flag when using the 'value' keyword in a type declaration
                // even when the 'enum' keyword is used.
                return new((new(context.VALUE() is not null ? TypeAttributes.Sealed : 0), EntityRegistry.WellKnownBaseType.System_Enum, false));
            }
            else if (context.VALUE() is not null)
            {
                // COMPAT: ilasm implies the Sealed flag when using the 'value' keyword in a type declaration
                return new((new(TypeAttributes.Sealed), EntityRegistry.WellKnownBaseType.System_ValueType, true));
            }

            // TODO: We should probably do this based on each token instead of just parsing all values.
            return new((new((TypeAttributes)Enum.Parse(typeof(TypeAttributes), context.GetText(), true)), null, false));
        }

        private sealed class CurrentMethodContext
        {
            public CurrentMethodContext(EntityRegistry.MethodDefinitionEntity definition)
            {
                Definition = definition;
            }

            public EntityRegistry.MethodDefinitionEntity Definition { get; }

            public Dictionary<string, LabelHandle> Labels { get; } = new();

            public Dictionary<string, int> ArgumentNames { get; } = new();

            public List<Dictionary<string, int>> LocalsScopes { get; } = new();

            public List<SignatureArg> AllLocals { get; } = new();
        }

        private CurrentMethodContext? _currentMethod;

        public GrammarResult VisitClassDecl(CILParser.ClassDeclContext context)
        {
            if (context.classHead() is CILParser.ClassHeadContext classHead)
            {
                _currentTypeDefinition.Push(VisitClassHead(classHead).Value);
                VisitClassDecls(context.classDecls());
                _currentTypeDefinition.Pop();
            }
            else if (context.methodHead() is CILParser.MethodHeadContext methodHead)
            {
                _currentMethod = new(VisitMethodHead(methodHead).Value);
                VisitMethodDecls(context.methodDecls());
                _currentMethod = null;
            }
            else if (context.secDecl() is {} secDecl)
            {
                var declarativeSecurity = VisitSecDecl(secDecl).Value;
                if (declarativeSecurity is not null)
                {
                    declarativeSecurity.Parent = _currentTypeDefinition.PeekOrDefault();
                }
            }
            else if (context.fieldDecl() is {} fieldDecl)
            {
                _ = VisitFieldDecl(fieldDecl);
            }

            return GrammarResult.SentinelValue.Result;
        }
        public GrammarResult VisitClassDecls(CILParser.ClassDeclsContext context) => VisitChildren(context);


        GrammarResult ICILVisitor<GrammarResult>.VisitClassHead(CILParser.ClassHeadContext context) => VisitClassHead(context);
        public GrammarResult.Literal<EntityRegistry.TypeDefinitionEntity> VisitClassHead(CILParser.ClassHeadContext context)
        {
            string typeFullName = VisitDottedName(context.dottedName()).Value;
            int typeFullNameLastDot = typeFullName.LastIndexOf('.');
            string typeNS;
            if (_currentTypeDefinition.Count != 0)
            {
                if (typeFullNameLastDot == -1)
                {
                    typeNS = string.Empty;
                }
                else
                {
                    typeNS = typeFullName.Substring(0, typeFullNameLastDot);
                }
            }
            else
            {
                if (typeFullNameLastDot == -1)
                {
                    typeNS = _currentNamespace.PeekOrDefault() ?? string.Empty;
                }
                else
                {
                    typeNS = $"{_currentNamespace.PeekOrDefault()}{typeFullName.Substring(0, typeFullNameLastDot)}";
                }
            }

            bool isNewType = false;

            var typeDefinition = _entityRegistry.GetOrCreateTypeDefinition(
                _currentTypeDefinition.PeekOrDefault(),
                typeNS,
                typeFullNameLastDot != -1
                    ? typeFullName.Substring(typeFullNameLastDot)
                    : typeFullName,
                (newTypeDef) =>
                {
                    isNewType = true;
                    EntityRegistry.WellKnownBaseType? fallbackBase = _options.NoAutoInherit ? null : EntityRegistry.WellKnownBaseType.System_Object;
                    bool requireSealed = false;
                    newTypeDef.Attributes = context.classAttr().Select(VisitClassAttr).Aggregate(
                        (TypeAttributes)0,
                        (acc, result) =>
                        {
                            var (attribute, implicitBase, attrRequireSealed) = result.Value;
                            if (implicitBase is not null)
                            {
                                // COMPAT: Any base type specified by an attribute is ignored if
                                // the user specified an explicit base type in an 'extends' clause.
                                fallbackBase = implicitBase;
                            }
                            // COMPAT: When a flags value is specified as an integer, it overrides
                            // all of the provided flags, including any compat sentinel flags that will require
                            // the sealed modifier to be provided.
                            if (!attribute.ShouldAppend)
                            {
                                requireSealed = attrRequireSealed;
                                return attribute.Value;
                            }
                            requireSealed |= attrRequireSealed;
                            if (TypeAttributes.LayoutMask.HasFlag(attribute.Value))
                            {
                                return (acc & ~TypeAttributes.LayoutMask) | attribute.Value;
                            }
                            if (TypeAttributes.StringFormatMask.HasFlag(attribute.Value))
                            {
                                return (acc & ~TypeAttributes.StringFormatMask) | attribute.Value;
                            }
                            if (TypeAttributes.VisibilityMask.HasFlag(attribute.Value))
                            {
                                return (acc & ~TypeAttributes.VisibilityMask) | attribute.Value;
                            }
                            if (attribute.Value == TypeAttributes.RTSpecialName)
                            {
                                // COMPAT: ILASM ignores the rtspecialname directive on a type.
                                return acc;
                            }
                            if (attribute.Value == TypeAttributes.Interface)
                            {
                                // COMPAT: interface implies abstract
                                return acc | TypeAttributes.Interface | TypeAttributes.Abstract;
                            }

                            return acc | attribute.Value;
                        });


                    for (int i = 0; i < VisitTyparsClause(context.typarsClause()).Value.Length; i++)
                    {
                        EntityRegistry.GenericParameterEntity? param = VisitTyparsClause(context.typarsClause()).Value[i];
                        param.Owner = newTypeDef;
                        param.Index = i;
                        newTypeDef.GenericParameters.Add(param);
                        foreach (var constraint in param.Constraints)
                        {
                            constraint.Owner = param;
                            newTypeDef.GenericParameterConstraints.Add(constraint);
                        }
                    }

                    // Temporarily push the new type as the current type definition so we can resolve type parameters
                    // that are used in the base type and interface types.
                    _currentTypeDefinition.Push(newTypeDef);

                    if (context.extendsClause() is CILParser.ExtendsClauseContext extends)
                    {
                        newTypeDef.BaseType = VisitExtendsClause(context.extendsClause()).Value;
                    }

                    if (context.implClause() is CILParser.ImplClauseContext impl)
                    {
                        newTypeDef.InterfaceImplementations.AddRange(VisitImplClause(context.implClause()).Value);
                    }

                    _currentTypeDefinition.Pop();

                    newTypeDef.BaseType ??= _entityRegistry.ResolveImplicitBaseType(fallbackBase);

                    // When the user has provided a type definition for a type that directly inherits
                    // System.ValueType but has not sealed it, emit a warning and add the 'sealed' modifier.
                    if (!newTypeDef.Attributes.HasFlag(TypeAttributes.Sealed) &&
                        (requireSealed // COMPAT: when both the sentinel values for 'value' and 'enum' are explicitly
                                       // specified, the sealed modifier is required even though
                                       // the base type isn't System.ValueType.
                        || _entityRegistry.SystemValueTypeType.Equals(newTypeDef.BaseType)))
                    {
                        _diagnostics.Add(
                            new Diagnostic(
                                DiagnosticIds.UnsealedValueType,
                                DiagnosticSeverity.Error,
                                string.Format(DiagnosticMessageTemplates.UnsealedValueType, newTypeDef.Name),
                                Location.From(context.dottedName().Stop, _documents)));
                        newTypeDef.Attributes |= TypeAttributes.Sealed;
                    }
                });

            if (!isNewType)
            {
                // COMPAT: Still visit some of the clauses to ensure the provided types are still imported,
                // even if unused.
                _ = context.extendsClause()?.Accept(this);
                _ = context.typarsClause().Accept(this);
            }

            return new(typeDefinition);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitClassName(CILParser.ClassNameContext context) => VisitClassName(context);
        public GrammarResult.Literal<EntityRegistry.TypeEntity> VisitClassName(CILParser.ClassNameContext context)
        {
            if (context.THIS() is not null)
            {
                if (_currentTypeDefinition.Count == 0)
                {
                    // TODO: Report diagnostic.
                    return new(new EntityRegistry.FakeTypeEntity(default(TypeDefinitionHandle)));
                }
                var thisType = _currentTypeDefinition.Peek();
                return new(thisType);
            }
            else if (context.BASE() is not null)
            {
                if (_currentTypeDefinition.Count == 0)
                {
                    // TODO: Report diagnostic.
                    return new(new EntityRegistry.FakeTypeEntity(default(TypeDefinitionHandle)));
                }
                var baseType = _currentTypeDefinition.Peek().BaseType;
                if (baseType is null)
                {
                    // TODO: Report diagnostic.
                    return new(new EntityRegistry.FakeTypeEntity(default(TypeDefinitionHandle)));
                }
                return new(baseType);
            }
            else if (context.NESTER() is not null)
            {
                if (_currentTypeDefinition.Count < 2)
                {
                    // TODO: Report diagnostic.
                    return new(new EntityRegistry.FakeTypeEntity(default(TypeDefinitionHandle)));
                }
                var nesterType = _currentTypeDefinition.Peek().ContainingType!;
                return new(nesterType);
            }
            else if (context.slashedName() is CILParser.SlashedNameContext slashedName)
            {
                EntityRegistry.EntityBase? resolutionContext = null;
                if (context.dottedName() is CILParser.DottedNameContext dottedAssemblyOrModuleName)
                {
                    if (context.MODULE() is not null)
                    {
                        resolutionContext = _entityRegistry.FindModuleReference(VisitDottedName(dottedAssemblyOrModuleName).Value);
                        if (resolutionContext is null)
                        {
                            // TODO: Report diagnostic
                            return new(new EntityRegistry.FakeTypeEntity(default(TypeDefinitionHandle)));
                        }
                    }
                    else
                    {
                        resolutionContext = _entityRegistry.GetOrCreateAssemblyReference(VisitDottedName(dottedAssemblyOrModuleName).Value, newRef => { });
                    }
                }
                else if (context.mdtoken() is CILParser.MdtokenContext typeRefScope)
                {
                    resolutionContext = VisitMdtoken(typeRefScope).Value;
                }
                else if (context.PTR() is not null)
                {
                    resolutionContext = new EntityRegistry.FakeTypeEntity(default(ModuleDefinitionHandle));
                }

                if (resolutionContext is not null)
                {
                    EntityRegistry.TypeReferenceEntity typeRef = _entityRegistry.GetOrCreateTypeReference(resolutionContext, VisitSlashedName(slashedName).Value);
                    return new(typeRef);
                }

                Debug.Assert(resolutionContext is null);

                return new(ResolveTypeDef());

                // Resolve typedef references
                EntityRegistry.TypeEntity ResolveTypeDef()
                {
                    TypeName typeName = VisitSlashedName(slashedName).Value;
                    if (typeName.ContainingTypeName is null)
                    {
                        // TODO: Check for typedef.
                    }
                    Stack<TypeName> containingTypes = new();
                    for (TypeName? containingType = typeName; containingType is not null; containingType = containingType.ContainingTypeName)
                    {
                        containingTypes.Push(containingType);
                    }
                    EntityRegistry.TypeDefinitionEntity? typeDef = null;
                    while (containingTypes.Count != 0)
                    {
                        TypeName containingType = containingTypes.Pop();

                        (string ns, string name) = NameHelpers.SplitDottedNameToNamespaceAndName(containingType.DottedName);

                        typeDef = _entityRegistry.FindTypeDefinition(
                            typeDef,
                            ns,
                            name);

                        if (typeDef is null)
                        {
                            // TODO: Report diagnostic for missing type name
                            return new EntityRegistry.FakeTypeEntity(default(TypeDefinitionHandle));
                        }
                    }

                    return typeDef!;
                }
            }
            else if (context.mdtoken() is CILParser.MdtokenContext typeToken)
            {
                EntityRegistry.EntityBase resolvedToken = VisitMdtoken(typeToken).Value;

                if (resolvedToken is not EntityRegistry.TypeEntity type)
                {
                    return new(new EntityRegistry.FakeTypeEntity(resolvedToken.Handle));
                }
                return new(type);
            }

            throw new InvalidOperationException("unreachable");
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitClassSeq(CILParser.ClassSeqContext context) => VisitClassSeq(context);
        public GrammarResult.FormattedBlob VisitClassSeq(CILParser.ClassSeqContext context)
        {
            // We're going to add all of the elements in the sequence as prefix blobs to this blob.
            BlobBuilder objSeqBlob = new(0);
            foreach (var item in context.classSeqElement())
            {
                objSeqBlob.LinkPrefix(VisitClassSeqElement(item).Value);
            }
            return new(objSeqBlob);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitClassSeqElement(CILParser.ClassSeqElementContext context) => VisitClassSeqElement(context);

        public GrammarResult.FormattedBlob VisitClassSeqElement(CILParser.ClassSeqElementContext context)
        {
            BlobBuilder blob = new();
            if (context.className() is CILParser.ClassNameContext className)
            {
                if (VisitClassName(className).Value is EntityRegistry.IHasReflectionNotation notation)
                {
                    blob.WriteSerializedString(notation.ReflectionNotation);
                }
                else
                {
                    blob.WriteSerializedString("");
                }
                return new(blob);
            }

            blob.WriteSerializedString(context.SQSTRING()?.Symbol.Text);
            return new(blob);
        }
        public GrammarResult VisitCompControl(CILParser.CompControlContext context)
        {
            // All compilation control directives that need special handling will be handled
            // directly in the token stream before parsing.
            // Any that reach here can be ignored.
            return GrammarResult.SentinelValue.Result;
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitCompQstring(CILParser.CompQstringContext context)
        {
            return VisitCompQstring(context);
        }

        private static GrammarResult.String VisitCompQstring(CILParser.CompQstringContext context)
        {
            StringBuilder builder = new();
            foreach (var item in context.QSTRING())
            {
                builder.Append(item.Symbol.Text);
            }
            return new(builder.ToString());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitCorflags(CILParser.CorflagsContext context) => VisitCorflags(context);
        public GrammarResult.Literal<int> VisitCorflags(CILParser.CorflagsContext context) => VisitInt32(context.int32());

        GrammarResult ICILVisitor<GrammarResult>.VisitCustomAttrDecl(CILParser.CustomAttrDeclContext context) => VisitCustomAttrDecl(context);
        public GrammarResult.Literal<EntityRegistry.CustomAttributeEntity?> VisitCustomAttrDecl(CILParser.CustomAttrDeclContext context)
        {
            if (context.dottedName() is {})
            {
                // TODO: typedef
                return new(null);
            }
            if (context.customDescrWithOwner() is {} descrWithOwner)
            {
                // Visit the custom attribute descriptor to record it,
                // but don't return it as it will already have its owner recorded.
                _ = VisitCustomDescrWithOwner(descrWithOwner);
                return new(null);
            }
            if (context.customDescr() is {} descr)
            {
#nullable disable // Disable nullability to work around lack of variance.
                return VisitCustomDescr(descr);
#nullable restore
            }
            throw new InvalidOperationException("unreachable");
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitCustomBlobArgs(CILParser.CustomBlobArgsContext context) => VisitCustomBlobArgs(context);
        public GrammarResult.FormattedBlob VisitCustomBlobArgs(CILParser.CustomBlobArgsContext context)
        {
            BlobBuilder blob = new();
            foreach (var item in context.serInit())
            {
                VisitSerInit(item).Value.WriteContentTo(blob);
            }
            return new(blob);
        }

        private const int CustomAttributeBlobFormatVersion = 1;

        GrammarResult ICILVisitor<GrammarResult>.VisitCustomBlobDescr(CILParser.CustomBlobDescrContext context) => VisitCustomBlobDescr(context);
        public GrammarResult.FormattedBlob VisitCustomBlobDescr(CILParser.CustomBlobDescrContext context)
        {
            var blob = new BlobBuilder();
            blob.WriteInt32(CustomAttributeBlobFormatVersion);
            VisitCustomBlobArgs(context.customBlobArgs()).Value.WriteContentTo(blob);
            VisitCustomBlobNVPairs(context.customBlobNVPairs()).Value.WriteContentTo(blob);
            return new(blob);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitCustomBlobNVPairs(CILParser.CustomBlobNVPairsContext context) => VisitCustomBlobNVPairs(context);
        public GrammarResult.FormattedBlob VisitCustomBlobNVPairs(CILParser.CustomBlobNVPairsContext context)
        {
            var blob = new BlobBuilder();
            var fieldOrProps = context.fieldOrProp();
            var types = context.serializType();
            var names = context.dottedName();
            var values = context.serInit();

            blob.WriteInt16((short)fieldOrProps.Length);

            for (int i = 0; i < fieldOrProps.Length; i++)
            {
                var fieldOrProp = fieldOrProps[i].GetText() == "field" ? CustomAttributeNamedArgumentKind.Field : CustomAttributeNamedArgumentKind.Property;
                var type = VisitSerializType(types[i]).Value;
                var name = VisitDottedName(names[i]).Value;
                var value = VisitSerInit(values[i]).Value;
                blob.WriteByte((byte)fieldOrProp);
                type.WriteContentTo(blob);
                blob.WriteSerializedString(name);
                value.WriteContentTo(blob);
            }
            return new(blob);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitCustomDescr(CILParser.CustomDescrContext context) => VisitCustomDescr(context);
        public GrammarResult.Literal<EntityRegistry.CustomAttributeEntity> VisitCustomDescr(CILParser.CustomDescrContext context)
        {
            var ctor = VisitCustomType(context.customType()).Value;
            BlobBuilder value;
            if (context.customBlobDescr() is {} customBlobDescr)
            {
                value = VisitCustomBlobDescr(customBlobDescr).Value;
            }
            else if (context.bytes() is {} bytes)
            {
                value = new();
                value.WriteBytes(VisitBytes(bytes).Value);
            }
            else if (context.compQstring() is {} str)
            {
                value = new();
                value.WriteUTF8(VisitCompQstring(str).Value);
                // COMPAT: We treat this string as a string-reprensentation of a blob,
                // so we don't emit the null terminator.
            }
            else
            {
                throw new InvalidOperationException("unreachable");
            }

            return new(_entityRegistry.CreateCustomAttribute(ctor, value));
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitCustomDescrWithOwner(CILParser.CustomDescrWithOwnerContext context) => VisitCustomDescrWithOwner(context);
        public GrammarResult.Literal<EntityRegistry.CustomAttributeEntity> VisitCustomDescrWithOwner(CILParser.CustomDescrWithOwnerContext context)
        {
            var ctor = VisitCustomType(context.customType()).Value;
            BlobBuilder value;
            if (context.customBlobDescr() is {} customBlobDescr)
            {
                value = VisitCustomBlobDescr(customBlobDescr).Value;
            }
            else if (context.bytes() is {} bytes)
            {
                value = new();
                value.WriteBytes(VisitBytes(bytes).Value);
            }
            else if (context.compQstring() is {} str)
            {
                value = new();
                value.WriteUTF8(VisitCompQstring(str).Value);
                // COMPAT: We treat this string as a string-reprensentation of a blob,
                // so we don't emit the null terminator.
            }
            else
            {
                throw new InvalidOperationException("unreachable");
            }

            var attr = _entityRegistry.CreateCustomAttribute(ctor, value);

            attr.Owner = VisitOwnerType(context.ownerType()).Value;

            return new(attr);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitCustomType(CILParser.CustomTypeContext context) => VisitCustomType(context);
        public GrammarResult.Literal<EntityRegistry.EntityBase> VisitCustomType(CILParser.CustomTypeContext context) => VisitMethodRef(context.methodRef());

        public GrammarResult VisitDataDecl(CILParser.DataDeclContext context)
        {
            _ = VisitDdHead(context.ddHead());
            _ = VisitDdBody(context.ddBody());
            return GrammarResult.SentinelValue.Result;
        }
        public GrammarResult VisitDdBody(CILParser.DdBodyContext context)
        {
            if (context.ddItemList() is CILParser.DdItemListContext ddItemList)
            {
                _ = VisitDdItemList(ddItemList);
            }
            else
            {
                _ = VisitDdItem(context.ddItem());
            }
            return GrammarResult.SentinelValue.Result;
        }
        public GrammarResult VisitDdHead(CILParser.DdHeadContext context)
        {
            if (context.id() is CILParser.IdContext id)
            {
                string name = VisitId(id).Value;
                if (!_mappedFieldDataNames.ContainsKey(name))
                {
                    _mappedFieldDataNames.Add(name, _mappedFieldData.Count);
                }
            }
            return GrammarResult.SentinelValue.Result;
        }
        public GrammarResult VisitDdItem(CILParser.DdItemContext context)
        {
            if (context.compQstring() is CILParser.CompQstringContext str)
            {
                var value = VisitCompQstring(str).Value;
                _mappedFieldData.WriteUTF16(value);
                return GrammarResult.SentinelValue.Result;
            }
            else if (context.id() is CILParser.IdContext id)
            {
                string name = VisitId(id).Value;
                if (!_mappedFieldDataReferenceFixups.TryGetValue(name, out var fixups))
                {
                    _mappedFieldDataReferenceFixups[name] = fixups = new();
                }

                // TODO: Figure out how to handle relocs correctly
                fixups.Add(_mappedFieldData.ReserveBytes(4));
                return GrammarResult.SentinelValue.Result;
            }
            else if (context.bytes() is CILParser.BytesContext bytes)
            {
                _mappedFieldData.WriteBytes(VisitBytes(bytes).Value);
                return GrammarResult.SentinelValue.Result;
            }

            int itemCount = VisitDdItemCount(context.ddItemCount()).Value;

            if (context.INT8() is not null)
            {
                _mappedFieldData.WriteBytes(context.int32() is CILParser.Int32Context int32 ? (byte)VisitInt32(int32).Value : (byte)0, itemCount);
            }
            else if (context.INT16() is not null)
            {
                for (int i = 0; i < itemCount; i++)
                {
                    _mappedFieldData.WriteInt16(context.int32() is CILParser.Int32Context int32 ? (short)VisitInt32(int32).Value : (short)0);
                }
            }
            else if (context.INT32_() is not null)
            {
                for (int i = 0; i < itemCount; i++)
                {
                    _mappedFieldData.WriteInt32(context.int32() is CILParser.Int32Context int32 ? VisitInt32(int32).Value : 0);
                }
            }
            else if (context.INT64_() is not null)
            {
                for (int i = 0; i < itemCount; i++)
                {
                    _mappedFieldData.WriteInt64(context.int64() is CILParser.Int64Context int64 ? VisitInt64(int64).Value : 0);
                }
            }
            else if (context.FLOAT32() is not null)
            {
                for (int i = 0; i < itemCount; i++)
                {
                    _mappedFieldData.WriteSingle(context.float64() is CILParser.Float64Context float64 ? (float)VisitFloat64(float64).Value : 0);
                }
            }
            else if (context.FLOAT64_() is not null)
            {
                for (int i = 0; i < itemCount; i++)
                {
                    _mappedFieldData.WriteDouble(context.float64() is CILParser.Float64Context float64 ? VisitFloat64(float64).Value : 0);
                }
            }
            return GrammarResult.SentinelValue.Result;
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitDdItemCount(CILParser.DdItemCountContext context) => VisitDdItemCount(context);
        public GrammarResult.Literal<int> VisitDdItemCount(CILParser.DdItemCountContext context) => new(context.int32() is CILParser.Int32Context ? VisitInt32(context.int32()).Value : 1);
        public GrammarResult VisitDdItemList(CILParser.DdItemListContext context)
        {
            foreach (var item in context.ddItem())
            {
                VisitDdItem(item);
            }
            return GrammarResult.SentinelValue.Result;
        }

        private readonly Stack<string> _currentNamespace = new();

        private readonly Stack<EntityRegistry.TypeDefinitionEntity> _currentTypeDefinition = new();

        public GrammarResult VisitDecl(CILParser.DeclContext context)
        {
            if (context.nameSpaceHead() is CILParser.NameSpaceHeadContext ns)
            {
                string namespaceName = VisitNameSpaceHead(ns).Value;
                _currentNamespace.Push($"{_currentNamespace.PeekOrDefault()}.{namespaceName}");
                VisitDecls(context.decls());
                _currentNamespace.Pop();
                return GrammarResult.SentinelValue.Result;
            }
            if (context.classHead() is CILParser.ClassHeadContext classHead)
            {
                _currentTypeDefinition.Push(VisitClassHead(classHead).Value);
                VisitClassDecls(context.classDecls());
                _currentTypeDefinition.Pop();
                return GrammarResult.SentinelValue.Result;
            }
            if (context.methodHead() is CILParser.MethodHeadContext methodHead)
            {
                _currentMethod = new(VisitMethodHead(methodHead).Value);
                VisitMethodDecls(context.methodDecls());
                _currentMethod = null;
                return GrammarResult.SentinelValue.Result;
            }
            if (context.fieldDecl() is { } fieldDecl)
            {
                _ = VisitFieldDecl(fieldDecl);
                return GrammarResult.SentinelValue.Result;
            }
            if (context.dataDecl() is { } dataDecl)
            {
                _ = VisitDataDecl(dataDecl);
                return GrammarResult.SentinelValue.Result;
            }
            if (context.vtableDecl() is { } vtable)
            {
                _ = VisitVtableDecl(vtable);
                return GrammarResult.SentinelValue.Result;
            }
            if (context.vtfixupDecl() is { } vtFixup)
            {
                _ = VisitVtfixupDecl(vtFixup);
                return GrammarResult.SentinelValue.Result;
            }
            if (context.extSourceSpec() is { } extSourceSpec)
            {
                _ = VisitExtSourceSpec(extSourceSpec);
                return GrammarResult.SentinelValue.Result;
            }
            if (context.fileDecl() is { } fileDecl)
            {
                _ = VisitFileDecl(fileDecl);
                return GrammarResult.SentinelValue.Result;
            }
            if (context.assemblyBlock() is { } assemblyBlock)
            {
                _ = VisitAssemblyBlock(assemblyBlock);
                return GrammarResult.SentinelValue.Result;
            }
            if (context.assemblyRefHead() is { } assemblyRef)
            {
                var asmRef = VisitAssemblyRefHead(assemblyRef).Value;
                _currentAssemblyOrRef = asmRef;
                foreach (var decl in context.assemblyRefDecls().assemblyRefDecl())
                {
                    _ = VisitAssemblyRefDecl(decl);
                }
                _currentAssemblyOrRef = null;
            }
            if (context.exptypeHead() is { } exptypeHead)
            {
                var (attrs, dottedName) = VisitExptypeHead(exptypeHead).Value;
                (string typeNamespace, string name) = NameHelpers.SplitDottedNameToNamespaceAndName(dottedName);
                var (impl, typeDefId, customAttrs) = VisitExptypeDecls(context.exptypeDecls()).Value;
                var exp = _entityRegistry.GetOrCreateExportedType(impl, typeNamespace, name, exp =>
                {
                    exp.Attributes = attrs;
                    exp.TypeDefinitionId = typeDefId;
                });
                foreach (var attr in customAttrs)
                {
                    attr.Owner = exp;
                }
                return GrammarResult.SentinelValue.Result;
            }
            if (context.manifestResHead() is { } manifestResHead)
            {
                var (name, alias, flags) = VisitManifestResHead(manifestResHead).Value;
                var (implementation, offset, attrs) = VisitManifestResDecls(context.manifestResDecls()).Value;
                if (implementation is null)
                {
                    offset = (uint)_manifestResources.Count;
                    _manifestResources.WriteBytes(_resourceLocator(alias));
                }
                var res = _entityRegistry.CreateManifestResource(name, offset);
                res.Attributes = flags;
                res.Implementation = implementation;
                foreach (var attr in attrs)
                {
                    attr.Owner = res;
                }
                return GrammarResult.SentinelValue.Result;
            }
            if (context.moduleHead() is { } moduleHead)
            {
                if (moduleHead.dottedName() is null)
                {
                    _entityRegistry.Module.Name = null;
                }
                else if (moduleHead.ChildCount == 2)
                {
                    _entityRegistry.Module.Name = VisitDottedName(moduleHead.dottedName()).Value;
                }
                else
                {
                    var name = VisitDottedName(moduleHead.dottedName()).Value;
                    _entityRegistry.GetOrCreateModuleReference(name, _ => { });
                }
                return GrammarResult.SentinelValue.Result;
            }
            if (context.subsystem() is { } subsystem)
            {
                _subsystem = (Subsystem)VisitSubsystem(subsystem).Value;
            }
            if (context.corflags() is { } corflags)
            {
                _corflags = (CorFlags)VisitCorflags(corflags).Value;
            }
            if (context.alignment() is { } alignment)
            {
                _alignment = VisitAlignment(alignment).Value;
            }
            if (context.imagebase() is { } imagebase)
            {
                _imageBase = VisitImagebase(imagebase).Value;
            }
            if (context.stackreserve() is { } stackreserve)
            {
                _stackReserve = VisitStackreserve(stackreserve).Value;
            }
            if (context.languageDecl() is { } languageDecl)
            {
                VisitLanguageDecl(languageDecl);
            }
            if (context.typedefDecl() is { } typedefDecl)
            {
                VisitTypedefDecl(typedefDecl);
            }
            if (context.typelist() is { } typelist)
            {
                foreach (var name in typelist.className())
                {
                    _ = VisitClassName(name);
                }
            }
            if (context.mscorlib() is { } mscorlib)
            {
                VisitMscorlib(mscorlib);
            }
            return GrammarResult.SentinelValue.Result;
        }

        public GrammarResult VisitDecls(CILParser.DeclsContext context)
        {
            foreach (var decl in context.decl())
            {
                _ = VisitDecl(decl);
            }
            return GrammarResult.SentinelValue.Result;
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitDottedName(CILParser.DottedNameContext context)
        {
            return VisitDottedName(context);
        }

        public static GrammarResult.String VisitDottedName(CILParser.DottedNameContext context)
        {
            return new(context.GetText());
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitElementType(CILParser.ElementTypeContext context) => VisitElementType(context);
        public GrammarResult.FormattedBlob VisitElementType(CILParser.ElementTypeContext context)
        {
            BlobBuilder blob = new(5);
            if (context.OBJECT() is not null)
            {
                blob.WriteByte((byte)SignatureTypeCode.Object);
            }
            else if (context.className() is CILParser.ClassNameContext className)
            {
                EntityRegistry.TypeEntity typeEntity = VisitClassName(className).Value;
                if (context.VALUE() is not null || context.VALUETYPE() is not null)
                {
                    blob.WriteByte((byte)SignatureTypeKind.ValueType);
                    blob.WriteTypeEntity(typeEntity);
                }
                else
                {
                    blob.WriteByte((byte)SignatureTypeKind.Class);
                    blob.WriteTypeEntity(typeEntity);
                }
            }
            else if (context.callConv() is CILParser.CallConvContext callConv)
            {
                // Emit function pointer signature.
                blob.WriteByte((byte)SignatureTypeCode.FunctionPointer);
                byte sigCallConv = VisitCallConv(callConv).Value;
                blob.WriteByte(sigCallConv);
                var signatureArgs = VisitSigArgs(context.sigArgs()).Value;
                int numArgs = signatureArgs.Count(arg => !arg.IsSentinel);
                blob.WriteCompressedInteger(numArgs);
                blob.LinkSuffix(VisitType(context.type()).Value);
                foreach (var arg in signatureArgs)
                {
                    blob.LinkSuffix(arg.SignatureBlob);
                }
            }
            else if (context.ELLIPSIS() is not null)
            {
                blob.WriteByte((byte)SignatureTypeCode.Sentinel);
                blob.LinkSuffix(VisitType(context.type()).Value);
            }
            else if (context.METHOD_TYPE_PARAMETER() is not null)
            {
                if (context.int32() is CILParser.Int32Context int32)
                {
                    // COMPAT: Always write a reference to a generic method parameter by index
                    // even if we aren't in a method or the index is out of range. We want to be able to write invalid IL like this.
                    blob.WriteByte((byte)SignatureTypeCode.GenericMethodParameter);
                    blob.WriteCompressedInteger(VisitInt32(int32).Value);
                }
                else
                {
                    string dottedName = VisitDottedName(context.dottedName()).Value;
                    if (_currentMethod is null)
                    {
                        // TODO: Report diagnostic
                        blob.WriteByte((byte)SignatureTypeCode.GenericMethodParameter);
                        blob.WriteCompressedInteger(0);
                    }
                    else
                    {
                        blob.WriteByte((byte)SignatureTypeCode.GenericMethodParameter);
                        bool foundParameter = false;
                        for (int i = 0; i < _currentMethod.Definition.GenericParameters.Count; i++)
                        {
                            EntityRegistry.GenericParameterEntity? genericParameter = _currentMethod.Definition.GenericParameters[i];
                            if (genericParameter.Name == dottedName)
                            {
                                foundParameter = true;
                                blob.WriteCompressedInteger(i);
                                break;
                            }
                        }
                        if (!foundParameter)
                        {
                            // BREAK-COMPAT: ILASM would silently emit an invalid signature when a method uses an invalid method type parameter but doesn't have method type parameters.
                            // The signature used completely invalid undocumented codes (that were really sentinel values for how ilasm later detected errors due to how the parsing model worked with a YACC-based parser)
                            // and when a method had no type parameters, it didn't run the code to process out these values and emit errors.
                            // This seems like a scenario that doesn't need to be brought forward.
                            // Instead, we'll just emit a reference to "generic method parameter" 0 and report an error.

                            // TODO: Report diagnostic
                            blob.WriteCompressedInteger(0);
                        }
                    }
                }
            }
            else if (context.TYPE_PARAMETER() is not null)
            {
                if (context.int32() is CILParser.Int32Context int32)
                {
                    // COMPAT: Always write a reference to a generic type parameter by index
                    // even if we aren't in a type or the index is out of range. We want to be able to write invalid IL like this.
                    blob.WriteByte((byte)SignatureTypeCode.GenericTypeParameter);
                    blob.WriteCompressedInteger(VisitInt32(int32).Value);
                }
                else
                {
                    string dottedName = VisitDottedName(context.dottedName()).Value;
                    if (_currentTypeDefinition.Count == 0)
                    {
                        // TODO: Report diagnostic
                        blob.WriteByte((byte)SignatureTypeCode.GenericTypeParameter);
                        blob.WriteCompressedInteger(0);
                    }
                    else
                    {
                        blob.WriteByte((byte)SignatureTypeCode.GenericTypeParameter);
                        bool foundParameter = false;
                        for (int i = 0; i < _currentTypeDefinition.Peek().GenericParameters.Count; i++)
                        {
                            EntityRegistry.GenericParameterEntity? genericParameter = _currentTypeDefinition.Peek().GenericParameters[i];
                            if (genericParameter.Name == dottedName)
                            {
                                foundParameter = true;
                                blob.WriteCompressedInteger(i);
                                break;
                            }
                        }
                        if (!foundParameter)
                        {
                            // BREAK-COMPAT: ILASM would silently emit an invalid signature when a type uses an invalid method type parameter but doesn't have any type parameters.
                            // The signature used completely invalid undocumented codes (that were really sentinel values for how ilasm later detected errors due to how the parsing model worked with a YACC-based parser)
                            // and when a method had no type parameters, it didn't run the code to process out these values and emit errors.
                            // This seems like a scenario that doesn't need to be brought forward.
                            // Instead, we'll just emit a reference to "generic method parameter" 0 and report an error.

                            // TODO: Report diagnostic
                            blob.WriteCompressedInteger(0);
                        }
                    }
                }
            }
            else if (context.TYPEDREF() is not null)
            {
                blob.WriteByte((byte)SignatureTypeCode.TypedReference);
            }
            else if (context.VOID() is not null)
            {
                blob.WriteByte((byte)SignatureTypeCode.Void);
            }
            else if (context.NATIVE_INT() is not null)
            {
                blob.WriteByte((byte)SignatureTypeCode.IntPtr);
            }
            else if (context.NATIVE_UINT() is not null)
            {
                blob.WriteByte((byte)SignatureTypeCode.UIntPtr);
            }
            else if (context.simpleType() is CILParser.SimpleTypeContext simpleType)
            {
                blob.WriteByte((byte)VisitSimpleType(simpleType).Value);
            }
            else if (context.dottedName() is CILParser.DottedNameContext dottedName)
            {
                // TODO: typedef
                throw new NotImplementedException();
            }
            else
            {
                throw new InvalidOperationException("unreachable");
            }
            return new(blob);
        }

        public GrammarResult VisitErrorNode(IErrorNode node) => throw new InvalidOperationException(NodeShouldNeverBeDirectlyVisited);
        public GrammarResult VisitEsHead(CILParser.EsHeadContext context) => throw new NotImplementedException("TODO: Symbols");

        GrammarResult ICILVisitor<GrammarResult>.VisitEventAttr(CILParser.EventAttrContext context) => VisitEventAttr(context);
        public GrammarResult.Flag<EventAttributes> VisitEventAttr(CILParser.EventAttrContext context)
        {
            return context.GetText() switch
            {
                "specialname" => new(EventAttributes.SpecialName),
                "rtspecialname" => new(0), // COMPAT: Ignore
                _ => throw new InvalidOperationException("unreachable"),
            };
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitEventDecl(CILParser.EventDeclContext context) => VisitEventDecl(context);
        public GrammarResult.Literal<(MethodSemanticsAttributes, EntityRegistry.EntityBase)?> VisitEventDecl(CILParser.EventDeclContext context)
        {
            if (context.ChildCount != 2)
            {
                return new(null);
            }
            string accessor = context.GetChild(0).GetText();
            EntityRegistry.EntityBase memberReference = VisitMethodRef(context.methodRef()).Value;
            MethodSemanticsAttributes methodSemanticsAttributes = accessor switch
            {
                ".addon" => MethodSemanticsAttributes.Adder,
                ".removeon" => MethodSemanticsAttributes.Remover,
                ".fire" => MethodSemanticsAttributes.Raiser,
                ".other" => MethodSemanticsAttributes.Other,
                _ => throw new InvalidOperationException("unreachable"),
            };
            return new((methodSemanticsAttributes, memberReference));
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitEventDecls(CILParser.EventDeclsContext context) => VisitEventDecls(context);
        public GrammarResult.Sequence<(MethodSemanticsAttributes, EntityRegistry.EntityBase)> VisitEventDecls(CILParser.EventDeclsContext context)
            => new(
                context.eventDecl()
                .Select(decl => VisitEventDecl(decl).Value)
                .Where(decl => decl is not null)
                .Select(decl => decl!.Value).ToImmutableArray());

        GrammarResult ICILVisitor<GrammarResult>.VisitEventHead(CILParser.EventHeadContext context) => VisitEventHead(context);
        public GrammarResult.Literal<EntityRegistry.EventEntity> VisitEventHead(CILParser.EventHeadContext context)
        {
            string name = VisitDottedName(context.dottedName()).Value;
            EventAttributes eventAttributes = context.eventAttr().Select(attr => VisitEventAttr(attr).Value).Aggregate((a, b) => a | b);
            return new(new EntityRegistry.EventEntity(eventAttributes, VisitTypeSpec(context.typeSpec()).Value, name));
        }

        public GrammarResult VisitExportHead(CILParser.ExportHeadContext context) => throw new NotImplementedException("Obsolete syntax");

        private const TypeAttributes TypeAttributesForwarder = (TypeAttributes)0x00200000;

        GrammarResult ICILVisitor<GrammarResult>.VisitExptAttr(CILParser.ExptAttrContext context) => VisitExptAttr(context);
        public static GrammarResult.Flag<TypeAttributes> VisitExptAttr(CILParser.ExptAttrContext context)
        {
            return context.GetText() switch
            {
                "private" => new(TypeAttributes.NotPublic, TypeAttributes.VisibilityMask),
                "public" => new(TypeAttributes.Public, TypeAttributes.VisibilityMask),
                "forwarder" => new(TypeAttributesForwarder),
                "nestedpublic" => new(TypeAttributes.NestedPublic, TypeAttributes.VisibilityMask),
                "nestedprivate" => new(TypeAttributes.NestedPrivate, TypeAttributes.VisibilityMask),
                "nestedfamily" => new(TypeAttributes.NestedFamily, TypeAttributes.VisibilityMask),
                "nestedassembly" => new(TypeAttributes.NestedAssembly, TypeAttributes.VisibilityMask),
                "nestedfamandassem" => new(TypeAttributes.NestedFamANDAssem, TypeAttributes.VisibilityMask),
                "nestedfamorassem" => new(TypeAttributes.NestedFamORAssem, TypeAttributes.VisibilityMask),
                _ => throw new InvalidOperationException("unreachable"),
            };
        }

        // TODO: Implement multimodule type exports and fowarders
        public GrammarResult VisitExptypeDecl(CILParser.ExptypeDeclContext context) => throw new InvalidOperationException(NodeShouldNeverBeDirectlyVisited);

        GrammarResult ICILVisitor<GrammarResult>.VisitExptypeDecls(CILParser.ExptypeDeclsContext context) => VisitExptypeDecls(context);
        public GrammarResult.Literal<(EntityRegistry.EntityBase? implementation, int typedefId, ImmutableArray<EntityRegistry.CustomAttributeEntity> attrs)> VisitExptypeDecls(CILParser.ExptypeDeclsContext context)
        {
            // COMPAT: The following order specifies the precedence of the various export kinds.
            // File, Assembly, Class (enclosing type), invalid token.
            // We'll process through all of the options here and then return the one that is valid.
            // We'll also record custom attributes here.
            EntityRegistry.EntityBase? implementationEntity = null;
            int typedefId = 0;
            var attrs = ImmutableArray.CreateBuilder<EntityRegistry.CustomAttributeEntity>();
            var declarations = context.exptypeDecl();
            for (int i = 0; i < declarations.Length; i++)
            {
                if (declarations[i].customAttrDecl() is { } attr)
                {
                    if (VisitCustomAttrDecl(attr).Value is EntityRegistry.CustomAttributeEntity customAttribute)
                    {
                        attrs.Add(customAttribute);
                    }
                    continue;
                }
                if (declarations[i].mdtoken() is { } mdToken)
                {
                    var entity = VisitMdtoken(mdToken).Value;
                    if (entity is null)
                    {
                        // TODO: Report diagnostic
                    }
                    implementationEntity = ResolveBetterEntity(entity);
                    continue;
                }
                string kind = declarations[i].GetText();
                if (kind == ".file")
                {
                    implementationEntity = _entityRegistry.FindFile(VisitDottedName(declarations[i].dottedName()).Value);
                    if (implementationEntity is null)
                    {
                        // TODO: Report diagnostic
                    }
                }
                else if (kind == ".assembly")
                {
                    implementationEntity = _entityRegistry.FindAssemblyReference(VisitDottedName(declarations[i].dottedName()).Value);
                    if (implementationEntity is null)
                    {
                        // TODO: Report diagnostic
                    }
                }
                else if (kind == ".class")
                {
                    if (declarations[i].int32() is CILParser.Int32Context int32)
                    {
                        typedefId = VisitInt32(int32).Value;
                    }
                    else
                    {
                        _ = VisitSlashedName(declarations[i].slashedName());
                        var containing = ResolveExportedType(declarations[i].slashedName());
                        if (containing is null)
                        {
                            // TODO: Report diagnostic
                        }
                        else
                        {
                            implementationEntity = ResolveBetterEntity(containing);
                        }
                    }
                }
            }

            return new((implementationEntity, typedefId, attrs.ToImmutable()));

            EntityRegistry.EntityBase? ResolveBetterEntity(EntityRegistry.EntityBase? newImplementation)
            {
                return (implementationEntity, newImplementation) switch
                {
                    (null, _) => newImplementation,
                    (_, null) => implementationEntity,
                    (_, EntityRegistry.FileEntity) => newImplementation,
                    (EntityRegistry.FileEntity, _) => implementationEntity,
                    (_, EntityRegistry.AssemblyEntity) => newImplementation,
                    (EntityRegistry.AssemblyEntity, _) => implementationEntity,
                    (_, EntityRegistry.TypeEntity) => newImplementation,
                    (EntityRegistry.TypeEntity, _) => implementationEntity,
                    _ => throw new InvalidOperationException("unreachable"),
                };
            }

            // Resolve ExportedType reference
            EntityRegistry.ExportedTypeEntity? ResolveExportedType(CILParser.SlashedNameContext slashedName)
            {
                TypeName typeName = VisitSlashedName(slashedName).Value;
                if (typeName.ContainingTypeName is null)
                {
                    // TODO: Check for typedef.
                }
                Stack<TypeName> containingTypes = new();
                for (TypeName? containingType = typeName; containingType is not null; containingType = containingType.ContainingTypeName)
                {
                    containingTypes.Push(containingType);
                }
                EntityRegistry.ExportedTypeEntity? exportedType = null;
                while (containingTypes.Count != 0)
                {
                    TypeName containingType = containingTypes.Pop();

                    (string ns, string name) = NameHelpers.SplitDottedNameToNamespaceAndName(containingType.DottedName);

                    exportedType = _entityRegistry.FindExportedType(
                        exportedType,
                        ns,
                        name);

                    if (exportedType is null)
                    {
                        // TODO: Report diagnostic for missing type name
                        return null;
                    }
                }

                return exportedType!;
            }
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitExptypeHead(CILParser.ExptypeHeadContext context) => VisitExptypeHead(context);
        public GrammarResult.Literal<(TypeAttributes attrs, string dottedName)> VisitExptypeHead(CILParser.ExptypeHeadContext context)
        {
            var attrs = context.exptAttr().Select(VisitExptAttr).Aggregate((TypeAttributes)0, (a, b) => a | b);
            return new((attrs, VisitDottedName(context.dottedName()).Value));
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitExtendsClause(CILParser.ExtendsClauseContext context) => VisitExtendsClause(context);

        public GrammarResult.Literal<EntityRegistry.TypeEntity?> VisitExtendsClause(CILParser.ExtendsClauseContext context)
        {
            if (context.typeSpec() is CILParser.TypeSpecContext typeSpec)
            {
                return new(VisitTypeSpec(typeSpec).Value);
            }
            else
            {
                return new(null);
            }
        }

        public GrammarResult VisitExtSourceSpec(CILParser.ExtSourceSpecContext context) => throw new NotImplementedException("TODO: Symbols");

        GrammarResult ICILVisitor<GrammarResult>.VisitF32seq(CILParser.F32seqContext context) => VisitF32seq(context);
        public GrammarResult.FormattedBlob VisitF32seq(CILParser.F32seqContext context)
        {
            var builder = ImmutableArray.CreateBuilder<float>();

            foreach (var item in context.children)
            {
                builder.Add((float)(item switch
                {
                    CILParser.Int32Context int32 => VisitInt32(int32).Value,
                    CILParser.Float64Context float64 => VisitFloat64(float64).Value,
                    _ => throw new InvalidOperationException("unreachable")
                }));
            }
            return new(builder.MoveToImmutable().SerializeSequence());
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitF64seq(CILParser.F64seqContext context) => VisitF64seq(context);
        public GrammarResult.FormattedBlob VisitF64seq(CILParser.F64seqContext context)
        {
            var builder = ImmutableArray.CreateBuilder<double>();

            foreach (var item in context.children)
            {
                builder.Add((double)(item switch
                {
                    CILParser.Int64Context int64 => VisitInt64(int64).Value,
                    CILParser.Float64Context float64 => VisitFloat64(float64).Value,
                    _ => throw new InvalidOperationException("unreachable")
                }));
            }
            return new(builder.MoveToImmutable().SerializeSequence());
        }

        public GrammarResult VisitFaultClause(CILParser.FaultClauseContext context) => throw new InvalidOperationException(NodeShouldNeverBeDirectlyVisited);

        GrammarResult ICILVisitor<GrammarResult>.VisitFieldAttr(CILParser.FieldAttrContext context) => VisitFieldAttr(context);
        public GrammarResult.Flag<FieldAttributes> VisitFieldAttr(CILParser.FieldAttrContext context)
        {
            if (context.int32() is { } int32)
            {
                return new((FieldAttributes)VisitInt32(int32).Value, ShouldAppend: false);
            }

            return context.GetText() switch
            {
                "static" => new(FieldAttributes.Static),
                "public" => new(FieldAttributes.Public, FieldAttributes.FieldAccessMask),
                "private" => new(FieldAttributes.Private, FieldAttributes.FieldAccessMask),
                "family" => new(FieldAttributes.Family, FieldAttributes.FieldAccessMask),
                "initonly" => new(FieldAttributes.InitOnly),
                "rtspecialname" => new(0), // COMPAT: Don't emit rtspecialname
                "specialname" => new(FieldAttributes.SpecialName),
                "assembly" => new(FieldAttributes.Assembly, FieldAttributes.FieldAccessMask),
                "famandassem" => new(FieldAttributes.FamANDAssem, FieldAttributes.FieldAccessMask),
                "famorassem" => new(FieldAttributes.FamORAssem, FieldAttributes.FieldAccessMask),
                "privatescope" => new(FieldAttributes.PrivateScope, FieldAttributes.FieldAccessMask),
                "literal" => new(FieldAttributes.Literal),
                "notserialized" => new(FieldAttributes.NotSerialized),
                _ => throw new InvalidOperationException("unreachable")
            };
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitFieldDecl(CILParser.FieldDeclContext context) => VisitFieldDecl(context);
        public GrammarResult VisitFieldDecl(CILParser.FieldDeclContext context)
        {
            var fieldAttrs = context.fieldAttr().Select(VisitFieldAttr).Aggregate((FieldAttributes)0, (a, b) => a | b);
            var fieldType = VisitType(context.type()).Value;
            var marshalBlobs = context.marshalBlob();
            var marshalBlob = VisitMarshalBlob(marshalBlobs[marshalBlobs.Length - 1]).Value;
            var name = VisitDottedName(context.dottedName()).Value;
            var rvaOffset = VisitAtOpt(context.atOpt()).Value;
            _ = VisitInitOpt(context.initOpt());

            var signature = new BlobEncoder(new BlobBuilder());
            _ = signature.Field();
            fieldType.WriteContentTo(signature.Builder);

            var field = EntityRegistry.CreateUnrecordedFieldDefinition(fieldAttrs, _currentTypeDefinition.PeekOrDefault()!, name, signature.Builder);

            if (field is not null)
            {
                field.MarshallingDescriptor = marshalBlob;
                field.DataDeclarationName = rvaOffset;
            }

            return GrammarResult.SentinelValue.Result;
        }

        public GrammarResult VisitFieldInit(CILParser.FieldInitContext context) => throw new NotImplementedException("TODO-SRM: Need support for an arbitrary byte blob as a constant value");

        public GrammarResult VisitFieldOrProp(CILParser.FieldOrPropContext context) => throw new InvalidOperationException(NodeShouldNeverBeDirectlyVisited);

        GrammarResult ICILVisitor<GrammarResult>.VisitFieldRef(CILParser.FieldRefContext context) => VisitFieldRef(context);
        public GrammarResult.Literal<EntityRegistry.EntityBase> VisitFieldRef(CILParser.FieldRefContext context)
        {
            if (context.type() is not CILParser.TypeContext type)
            {
                // TODO: typedef
                throw new NotImplementedException();
            }

            var fieldTypeSig = VisitType(type).Value;
            EntityRegistry.TypeEntity definingType = _currentTypeDefinition.PeekOrDefault() ?? _entityRegistry.ModuleType;
            if (context.typeSpec() is CILParser.TypeSpecContext typeSpec)
            {
                definingType = VisitTypeSpec(typeSpec).Value;
            }

            var name = VisitDottedName(context.dottedName()).Value;

            var fieldSig = new BlobBuilder(fieldTypeSig.Count + 1);
            fieldSig.WriteByte((byte)SignatureKind.Field);
            fieldTypeSig.WriteContentTo(fieldSig);
            return new(_entityRegistry.CreateLazilyRecordedMemberReference(definingType, name, fieldSig));
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitFieldSerInit(CILParser.FieldSerInitContext context) => VisitFieldSerInit(context);
        public GrammarResult.FormattedBlob VisitFieldSerInit(CILParser.FieldSerInitContext context)
        {
            // The max length for the majority of the blobs is 9 bytes. 1 for the type of blob, 8 for the max 64-bit value.
            // Byte arrays can be larger, so we handle that case separately.
            const int CommonMaxBlobLength = 9;
            BlobBuilder builder;
            var bytesNode = context.bytes();
            if (bytesNode is not null)
            {
                var bytesResult = VisitBytes(bytesNode);
                // Our blob length is the number of bytes in the byte array + the code for the byte array.
                builder = new BlobBuilder(bytesResult.Value.Length + 1);
                builder.WriteByte((byte)SerializationTypeCode.String);
                builder.WriteBytes(bytesResult.Value);
                return new(builder);
            }
            builder = new BlobBuilder(CommonMaxBlobLength);

            int tokenType = ((ITerminalNode)context.GetChild(0)).Symbol.Type;

            builder.WriteByte((byte)GetTypeCodeForToken(tokenType));

            switch (tokenType)
            {
                case CILParser.BOOL:
                    builder.WriteBoolean(VisitTruefalse(context.truefalse()).Value);
                    break;
                case CILParser.INT8:
                case CILParser.UINT8:
                    builder.WriteByte((byte)VisitInt32(context.int32()).Value);
                    break;
                case CILParser.CHAR:
                case CILParser.INT16:
                case CILParser.UINT16:
                    builder.WriteInt16((short)VisitInt32(context.int32()).Value);
                    break;
                case CILParser.INT32:
                case CILParser.UINT32:
                    builder.WriteInt32(VisitInt32(context.int32()).Value);
                    break;
                case CILParser.INT64:
                case CILParser.UINT64:
                    builder.WriteInt64(VisitInt64(context.int64()).Value);
                    break;
                case CILParser.FLOAT32:
                    {
                        if (context.float64() is CILParser.Float64Context float64)
                        {
                            builder.WriteSingle((float)VisitFloat64(float64).Value);
                        }
                        if (context.int32() is CILParser.Int32Context int32)
                        {
                            int value = VisitInt32(int32).Value;
                            builder.WriteSingle(Unsafe.As<int, float>(ref value));
                        }
                        break;
                    }
                case CILParser.FLOAT64:
                    {
                        if (context.float64() is CILParser.Float64Context float64)
                        {
                            builder.WriteDouble(VisitFloat64(float64).Value);
                        }
                        if (context.int64() is CILParser.Int64Context int64)
                        {
                            long value = VisitInt64(int64).Value;
                            builder.WriteDouble(Unsafe.As<long, double>(ref value));
                        }
                        break;
                    }
            }

            return new(builder);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitFileAttr(CILParser.FileAttrContext context) => VisitFileAttr(context);
        public GrammarResult.Literal<bool> VisitFileAttr(CILParser.FileAttrContext context)
            => context.ChildCount != 0 ? new(false) : new(true);
        GrammarResult ICILVisitor<GrammarResult>.VisitFileDecl(CILParser.FileDeclContext context) => VisitFileDecl(context);
        public GrammarResult.Literal<EntityRegistry.FileEntity> VisitFileDecl(CILParser.FileDeclContext context)
        {
            string dottedName = VisitDottedName(context.dottedName()).Value;
            ImmutableArray<byte>? hash = context.HASH() is not null ? VisitBytes(context.bytes()).Value : null;
            var hashBlob = hash is not null ? new BlobBuilder() : null;
            hashBlob?.WriteBytes(hash!.Value);

            bool hasMetadata = context.fileAttr().Aggregate(true, (acc, attr) => acc || VisitFileAttr(attr).Value);
            bool isEntrypoint = context.fileEntry().Aggregate(true, (acc, attr) => acc || VisitFileEntry(attr).Value);
            var entity = _entityRegistry.GetOrCreateFile(dottedName, hasMetadata, hashBlob);
            if (isEntrypoint)
            {
                _entityRegistry.EntryPoint = entity;
            }
            return new(entity);
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitFileEntry(CILParser.FileEntryContext context) => VisitFileEntry(context);
        public GrammarResult.Literal<bool> VisitFileEntry(CILParser.FileEntryContext context)
            => context.ChildCount != 0 ? new(true) : new(false);

        GrammarResult ICILVisitor<GrammarResult>.VisitFilterClause(CILParser.FilterClauseContext context) => VisitFilterClause(context);
        public GrammarResult.Literal<LabelHandle> VisitFilterClause(CILParser.FilterClauseContext context)
        {
            if (context.scopeBlock() is CILParser.ScopeBlockContext scopeBlock)
            {
                LabelHandle start = _currentMethod!.Definition.MethodBody.DefineLabel();
                _currentMethod.Definition.MethodBody.MarkLabel(start);
                _ = VisitScopeBlock(scopeBlock);
                return new(start);
            }
            if (context.id() is CILParser.IdContext id)
            {
                var start = _currentMethod!.Labels.TryGetValue(VisitId(id).Value, out LabelHandle startLabel) ? startLabel : _currentMethod.Labels[VisitId(id).Value] = _currentMethod.Definition.MethodBody.DefineLabel();
                return new(start);
            }
            if (context.int32() is CILParser.Int32Context offset)
            {
                var start = _currentMethod!.Definition.MethodBody.DefineLabel();
                _currentMethod.Definition.MethodBody.MarkLabel(start, VisitInt32(offset).Value);
                return new(start);
            }
            throw new InvalidOperationException("unreachable");
        }

        public GrammarResult VisitFinallyClause(CILParser.FinallyClauseContext context) => throw new InvalidOperationException(NodeShouldNeverBeDirectlyVisited);

        GrammarResult ICILVisitor<GrammarResult>.VisitFloat64(CILParser.Float64Context context) => VisitFloat64(context);
        public GrammarResult.Literal<double> VisitFloat64(CILParser.Float64Context context)
        {
            if (context.FLOAT64() is ITerminalNode float64)
            {
                string text = float64.Symbol.Text;
                bool neg = text.StartsWith("-", StringComparison.InvariantCulture);
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                {
                    result = neg ? double.MaxValue : double.MinValue;
                }
                return new(result);
            }
            else if (context.int32() is CILParser.Int32Context int32)
            {
                int value = VisitInt32(int32).Value;
                return new(Unsafe.As<int, float>(ref value));
            }
            else if (context.int64() is CILParser.Int64Context int64)
            {
                long value = VisitInt64(int64).Value;
                return new(BitConverter.Int64BitsToDouble(value));
            }
            throw new InvalidOperationException("unreachable");
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitGenArity(CILParser.GenArityContext context) => VisitGenArity(context);
        public GrammarResult.Literal<int> VisitGenArity(CILParser.GenArityContext context)
            => context.genArityNotEmpty() is CILParser.GenArityNotEmptyContext genArity ? VisitGenArityNotEmpty(genArity) : new(0);

        GrammarResult ICILVisitor<GrammarResult>.VisitGenArityNotEmpty(CILParser.GenArityNotEmptyContext context) => VisitGenArityNotEmpty(context);
        public GrammarResult.Literal<int> VisitGenArityNotEmpty(CILParser.GenArityNotEmptyContext context) => VisitInt32(context.int32());

        GrammarResult ICILVisitor<GrammarResult>.VisitHandlerBlock(CILParser.HandlerBlockContext context) => VisitHandlerBlock(context);

        public GrammarResult.Literal<(LabelHandle Start, LabelHandle End)> VisitHandlerBlock(CILParser.HandlerBlockContext context)
        {
            if (context.scopeBlock() is CILParser.ScopeBlockContext scopeBlock)
            {
                LabelHandle start = _currentMethod!.Definition.MethodBody.DefineLabel();
                _currentMethod.Definition.MethodBody.MarkLabel(start);
                _ = VisitScopeBlock(scopeBlock);
                LabelHandle end = _currentMethod.Definition.MethodBody.DefineLabel();
                _currentMethod.Definition.MethodBody.MarkLabel(end);
                return new((start, end));
            }
            if (context.id() is CILParser.IdContext[] ids)
            {
                var start = _currentMethod!.Labels.TryGetValue(VisitId(ids[0]).Value, out LabelHandle startLabel) ? startLabel : _currentMethod.Labels[VisitId(ids[0]).Value] = _currentMethod.Definition.MethodBody.DefineLabel();
                var end = _currentMethod!.Labels.TryGetValue(VisitId(ids[1]).Value, out LabelHandle endLabel) ? endLabel : _currentMethod.Labels[VisitId(ids[1]).Value] = _currentMethod.Definition.MethodBody.DefineLabel();
                return new((start, end));
            }
            if (context.int32() is CILParser.Int32Context[] offsets)
            {
                var start = _currentMethod!.Definition.MethodBody.DefineLabel();
                var end = _currentMethod.Definition.MethodBody.DefineLabel();
                _currentMethod.Definition.MethodBody.MarkLabel(start, VisitInt32(offsets[0]).Value);
                _currentMethod.Definition.MethodBody.MarkLabel(end, VisitInt32(offsets[1]).Value);
                return new((start, end));
            }
            throw new InvalidOperationException("unreachable");
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitHexbytes(CILParser.HexbytesContext context)
        {
            return VisitHexbytes(context);
        }

        public static GrammarResult.Sequence<byte> VisitHexbytes(CILParser.HexbytesContext context)
        {
            ITerminalNode[] bytes = context.HEXBYTE();
            var builder = ImmutableArray.CreateBuilder<byte>(bytes.Length);
            foreach (var @byte in bytes)
            {
                builder.Add(byte.Parse(@byte.Symbol.Text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
            }
            return new(builder.MoveToImmutable());
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitI16seq(CILParser.I16seqContext context) => VisitI16seq(context);
        public GrammarResult.FormattedBlob VisitI16seq(CILParser.I16seqContext context)
        {
            var values = context.int32();
            var builder = ImmutableArray.CreateBuilder<short>(values.Length);
            foreach (var value in values)
            {
                builder.Add((short)VisitInt32(value).Value);
            }
            return new(builder.MoveToImmutable().SerializeSequence());
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitI32seq(CILParser.I32seqContext context) => VisitI32seq(context);
        public GrammarResult.FormattedBlob VisitI32seq(CILParser.I32seqContext context)
        {
            var values = context.int32();
            var builder = ImmutableArray.CreateBuilder<int>(values.Length);
            foreach (var value in values)
            {
                builder.Add(VisitInt32(value).Value);
            }
            return new(builder.MoveToImmutable().SerializeSequence());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitI8seq(CILParser.I8seqContext context) => VisitI8seq(context);
        public GrammarResult.FormattedBlob VisitI8seq(CILParser.I8seqContext context)
        {
            var values = context.int32();
            var builder = ImmutableArray.CreateBuilder<byte>(values.Length);
            foreach (var value in values)
            {
                builder.Add((byte)VisitInt32(value).Value);
            }
            return new(builder.MoveToImmutable().SerializeSequence());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitI64seq(CILParser.I64seqContext context) => VisitI64seq(context);
        public GrammarResult.FormattedBlob VisitI64seq(CILParser.I64seqContext context)
        {
            var values = context.int64();
            var builder = ImmutableArray.CreateBuilder<long>(values.Length);
            foreach (var value in values)
            {
                builder.Add(VisitInt64(value).Value);
            }
            return new(builder.MoveToImmutable().SerializeSequence());
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitId(CILParser.IdContext context) => VisitId(context);
        public static GrammarResult.String VisitId(CILParser.IdContext context)
        {
            return new GrammarResult.String((context.ID() ?? context.SQSTRING()).Symbol.Text);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitIidParamIndex(CILParser.IidParamIndexContext context) => VisitIidParamIndex(context);
        public GrammarResult.Literal<int?> VisitIidParamIndex(CILParser.IidParamIndexContext context)
            => context.int32() is CILParser.Int32Context int32 ? new(VisitInt32(int32).Value) : new(null);

        GrammarResult ICILVisitor<GrammarResult>.VisitImagebase(CILParser.ImagebaseContext context) => VisitImagebase(context);
        public GrammarResult.Literal<long> VisitImagebase(CILParser.ImagebaseContext context) => VisitInt64(context.int64());

        GrammarResult ICILVisitor<GrammarResult>.VisitImplAttr(ILAssembler.CILParser.ImplAttrContext context) => VisitImplAttr(context);
        public GrammarResult.Flag<MethodImplAttributes> VisitImplAttr(CILParser.ImplAttrContext context)
        {
            if (context.int32() is CILParser.Int32Context int32)
            {
                return new((MethodImplAttributes)VisitInt32(int32).Value, ShouldAppend: false);
            }
            string attribute = context.GetText();
            return attribute switch
            {
                "native" => new(MethodImplAttributes.Native, MethodImplAttributes.CodeTypeMask),
                "cil" => new(MethodImplAttributes.IL, MethodImplAttributes.CodeTypeMask),
                "optil" => new(MethodImplAttributes.OPTIL, MethodImplAttributes.CodeTypeMask),
                "managed" => new(MethodImplAttributes.Managed, MethodImplAttributes.ManagedMask),
                "unmanaged" => new(MethodImplAttributes.Unmanaged, MethodImplAttributes.ManagedMask),
                "forwardref" => new(MethodImplAttributes.ForwardRef),
                "preservesig" => new(MethodImplAttributes.PreserveSig),
                "runtime" => new(MethodImplAttributes.Runtime, MethodImplAttributes.CodeTypeMask),
                "internalcall" => new(MethodImplAttributes.InternalCall),
                "synchronized" => new(MethodImplAttributes.Synchronized),
                "noinlining" => new(MethodImplAttributes.NoInlining),
                "aggressiveinlining" => new(MethodImplAttributes.AggressiveInlining),
                "nooptimization" => new(MethodImplAttributes.NoOptimization),
                "aggressiveoptimization" => new((MethodImplAttributes)0x0200),
                _ => throw new InvalidOperationException("unreachable"),
            };
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitImplClause(CILParser.ImplClauseContext context) => VisitImplClause(context);
        public GrammarResult.Sequence<EntityRegistry.InterfaceImplementationEntity> VisitImplClause(CILParser.ImplClauseContext context) => context.implList() is {} implList ? VisitImplList(implList) : new(ImmutableArray<EntityRegistry.InterfaceImplementationEntity>.Empty);

        GrammarResult ICILVisitor<GrammarResult>.VisitImplList(CILParser.ImplListContext context) => VisitImplList(context);
        public GrammarResult.Sequence<EntityRegistry.InterfaceImplementationEntity> VisitImplList(CILParser.ImplListContext context)
        {
            var builder = ImmutableArray.CreateBuilder<EntityRegistry.InterfaceImplementationEntity>();
            foreach (var impl in context.typeSpec())
            {
                builder.Add(EntityRegistry.CreateUnrecordedInterfaceImplementation(_currentTypeDefinition.PeekOrDefault()!, VisitTypeSpec(impl).Value));
            }
            return new(builder.ToImmutable());
        }

        public GrammarResult VisitInitOpt(CILParser.InitOptContext context)
        {
            if (context.fieldInit() is {})
            {
                // TODO: Change fieldSerInit to return a parsed System.Object value to construct the constant row entry.
                // TODO-SRM: AddConstant does not support providing an arbitrary byte array as a constant value.
                // Propose MetadataBuilder.AddConstant(EntityHandle parent, PrimitiveTypeCode type, BlobBuilder value) overload?
                throw new NotImplementedException();
            }
            return GrammarResult.SentinelValue.Result;
        }
        public GrammarResult VisitInstr(CILParser.InstrContext context)
        {
            var instrContext = context.GetRuleContext<ParserRuleContext>(0);
            ILOpCode opcode = ((GrammarResult.Literal<ILOpCode>)instrContext.Accept(this)).Value;
            switch (instrContext.RuleIndex)
            {
                case CILParser.RULE_instr_brtarget:
                    {
                        ParserRuleContext argument = context.GetRuleContext<ParserRuleContext>(1);
                        if (argument is CILParser.IdContext id)
                        {
                            string label = VisitId(id).Value;
                            if (!_currentMethod!.Labels.TryGetValue(label, out var handle))
                            {
                                _currentMethod.Labels.Add(label, handle = _currentMethod.Definition.MethodBody.DefineLabel());
                            }
                            _currentMethod.Definition.MethodBody.Branch(opcode, handle);
                        }
                        if (argument is CILParser.Int32Context int32)
                        {
                            int offset = VisitInt32(int32).Value;
                            LabelHandle label = _currentMethod!.Definition.MethodBody.DefineLabel();
                            _currentMethod.Definition.MethodBody.Branch(opcode, label);
                            _currentMethod.Definition.MethodBody.MarkLabel(label, _currentMethod.Definition.MethodBody.Offset + offset);
                        }
                    }
                    break;
                case CILParser.RULE_instr_field:
                    {
                        _currentMethod!.Definition.MethodBody.OpCode(opcode);
                        if (context.mdtoken() is CILParser.MdtokenContext mdtoken)
                        {
                            _currentMethod.Definition.MethodBody.Token(VisitMdtoken(mdtoken).Value.Handle);
                        }
                        else
                        {
                            var fieldRef = VisitFieldRef(context.fieldRef()).Value;
                            if (fieldRef is EntityRegistry.MemberReferenceEntity memberRef)
                            {
                                memberRef.RecordBlobToWriteResolvedHandle(_currentMethod.Definition.MethodBody.CodeBuilder.ReserveBytes(4));
                            }
                            else
                            {
                                _currentMethod.Definition.MethodBody.Token(fieldRef.Handle);
                            }
                        }
                    }
                    break;
                case CILParser.RULE_instr_i:
                    {
                        int arg = VisitInt32(context.int32()).Value;
                        if (opcode == ILOpCode.Ldc_i4 || opcode == ILOpCode.Ldc_i4_s)
                        {
                            _currentMethod!.Definition.MethodBody.LoadConstantI4(arg);
                        }
                        else
                        {
                            _currentMethod!.Definition.MethodBody.OpCode(opcode);
                            _currentMethod.Definition.MethodBody.CodeBuilder.WriteByte((byte)arg);
                        }
                    }
                    break;
                case CILParser.RULE_instr_i8:
                    Debug.Assert(opcode == ILOpCode.Ldc_i8);
                    _currentMethod!.Definition.MethodBody.LoadConstantI8(VisitInt64(context.int64()).Value);
                    break;
                case CILParser.RULE_instr_method:
                    {
                        if (opcode == ILOpCode.Callvirt || opcode == ILOpCode.Newobj)
                        {
                            _expectInstance = true;
                        }
                        _currentMethod!.Definition.MethodBody.OpCode(opcode);
                        var methodRef = VisitMethodRef(context.methodRef()).Value;
                        if (methodRef is EntityRegistry.MemberReferenceEntity memberRef)
                        {
                            memberRef.RecordBlobToWriteResolvedHandle(_currentMethod.Definition.MethodBody.CodeBuilder.ReserveBytes(4));
                        }
                        else
                        {
                            _currentMethod.Definition.MethodBody.Token(methodRef.Handle);
                        }
                        // Reset the instance flag for the next instruction.
                        if (opcode == ILOpCode.Callvirt || opcode == ILOpCode.Newobj)
                        {
                            _expectInstance = false;
                        }
                    }
                    break;
                case CILParser.RULE_instr_none:
                    _currentMethod!.Definition.MethodBody.OpCode(opcode);
                    break;
                case CILParser.RULE_instr_r:
                    {
                        double value;
                        ParserRuleContext argument = context.GetRuleContext<ParserRuleContext>(1);
                        if (argument is CILParser.Float64Context float64)
                        {
                            value = VisitFloat64(float64).Value;
                        }
                        else if (argument is CILParser.Int64Context int64)
                        {
                            long intValue = VisitInt64(int64).Value;
                            value = BitConverter.Int64BitsToDouble(intValue);
                        }
                        else if (argument is CILParser.BytesContext bytesContext)
                        {
                            var bytes = VisitBytes(bytesContext).Value.ToArray();
                            value = bytes switch
                            {
                                { Length: >= 8 } => BitConverter.ToDouble(bytes, 0),
                                { Length: >= 4 } => BitConverter.ToSingle(bytes, 0),
                                // TODO: Report diagnostic on too-short byte array
                                _ => 0.0d
                            };
                        }
                        else
                        {
                            throw new InvalidOperationException("unreachable");
                        }
                        if (opcode == ILOpCode.Ldc_r4)
                        {
                            _currentMethod!.Definition.MethodBody.LoadConstantR4((float)value);
                        }
                        else
                        {
                            _currentMethod!.Definition.MethodBody.LoadConstantR8(value);
                        }
                    }
                    break;
                case CILParser.RULE_instr_sig:
                    {
                        BlobBuilder signature = new();
                        byte callConv = VisitCallConv(context.callConv()).Value;
                        signature.WriteByte(callConv);
                        var args = VisitSigArgs(context.sigArgs()).Value;
                        signature.WriteCompressedInteger(args.Length);
                        // Write return type
                        VisitType(context.type()).Value.WriteContentTo(signature);
                        // Write arg signatures
                        foreach (var arg in args)
                        {
                            arg.SignatureBlob.WriteContentTo(signature);
                        }
                        _currentMethod!.Definition.MethodBody.Token(_entityRegistry.GetOrCreateStandaloneSignature(signature).Handle);
                    }
                    break;
                case CILParser.RULE_instr_string:
                    Debug.Assert(opcode == ILOpCode.Ldstr);
                    string str;
                    if (context.bytes() is CILParser.BytesContext rawBytes)
                    {
                        ReadOnlySpan<byte> bytes = VisitBytes(rawBytes).Value.AsSpan();
                        ReadOnlySpan<char> bytesAsChars = MemoryMarshal.Cast<byte, char>(bytes);
                        if (!BitConverter.IsLittleEndian)
                        {
                            for (int i = 0; i < bytesAsChars.Length; i++)
                            {
                                BinaryPrimitives.ReverseEndianness(bytesAsChars[i]);
                            }
                        }
                        str = bytesAsChars.ToString();
                    }
                    else
                    {
                        var userString = context.compQstring();
                        Debug.Assert(userString is not null);
                        str = VisitCompQstring(userString!).Value;
                        if (context.ANSI() is not null)
                        {
                            unsafe
                            {
                                byte* ansiStrMemory = (byte*)Marshal.StringToCoTaskMemAnsi(str);
                                int strlen = 0;
                                for (; ansiStrMemory[strlen] != 0; strlen++) ;
                                str = new string((char*)ansiStrMemory, 0, (strlen + 1) / 2);
                                Marshal.FreeCoTaskMem((nint)ansiStrMemory);
                            }
                        }
                    }
                    _currentMethod!.Definition.MethodBody.LoadString(_metadataBuilder.GetOrAddUserString(str));
                    break;
                case CILParser.RULE_instr_switch:
                    {
                        var labels = new List<(LabelHandle Label, int? Offset)>();
                        foreach (var label in context.labels().children)
                        {
                            if (label is CILParser.IdContext id)
                            {
                                string labelName = VisitId(id).Value;
                                if (!_currentMethod!.Labels.TryGetValue(labelName, out var handle))
                                {
                                    _currentMethod.Labels.Add(labelName, handle = _currentMethod.Definition.MethodBody.DefineLabel());
                                }
                                labels.Add((handle, null));
                            }
                            else if (label is CILParser.Int32Context int32)
                            {
                                int offset = VisitInt32(int32).Value;
                                LabelHandle labelHandle = _currentMethod!.Definition.MethodBody.DefineLabel();
                                labels.Add((labelHandle, offset));
                            }
                        }
                        var switchEncoder = _currentMethod!.Definition.MethodBody.Switch(labels.Count);
                        foreach (var label in labels)
                        {
                            switchEncoder.Branch(label.Label);
                        }
                        // Now that we've emitted the switch instruction, we can go back and mark the offset-based target labels
                        foreach (var label in labels)
                        {
                            if (label.Offset is int offset)
                            {
                                _currentMethod.Definition.MethodBody.MarkLabel(label.Label, _currentMethod.Definition.MethodBody.Offset + offset);
                            }
                        }
                    }
                    break;
                case CILParser.RULE_instr_tok:
                    var tok = VisitOwnerType(context.ownerType()).Value;
                    _currentMethod!.Definition.MethodBody.OpCode(opcode);
                    _currentMethod.Definition.MethodBody.Token(tok.Handle);
                    break;
                case CILParser.RULE_instr_type:
                    {
                        var arg = VisitTypeSpec(context.typeSpec()).Value;
                        _currentMethod!.Definition.MethodBody.OpCode(opcode);
                        _currentMethod.Definition.MethodBody.Token(arg.Handle);
                    }
                    break;
                case CILParser.RULE_instr_var:
                    {
                        string instrName = opcode.ToString();
                        bool isShortForm = instrName.EndsWith("_s");
                        _currentMethod!.Definition.MethodBody.OpCode(opcode);
                        if (context.int32() is CILParser.Int32Context int32)
                        {
                            int value = VisitInt32(int32).Value;
                            if (isShortForm)
                            {
                                // Emit a byte instead of the int for the short form
                                _currentMethod.Definition.MethodBody.CodeBuilder.WriteByte((byte)value);
                            }
                            else
                            {
                                _currentMethod.Definition.MethodBody.CodeBuilder.WriteInt32(value);
                            }
                        }
                        else
                        {
                            Debug.Assert(context.id() is not null);
                            string varName = VisitId(context.id()!).Value;
                            int? index = null;
                            if (instrName.Contains("arg"))
                            {
                                if (_currentMethod!.ArgumentNames.TryGetValue(varName, out var argIndex))
                                {
                                    index = argIndex;

                                    if (_currentMethod.Definition.SignatureHeader.IsInstance)
                                    {
                                        argIndex++;
                                    }
                                }
                                else
                                {
                                    // TODO: Report diagnostic
                                }
                            }
                            else
                            {
                                for (int i = _currentMethod!.LocalsScopes.Count - 1; i >= 0 ; i--)
                                {
                                    if (_currentMethod.LocalsScopes[i].TryGetValue(varName, out var localIndex))
                                    {
                                        index = localIndex;
                                        break;
                                    }
                                }
                                if (index is null)
                                {
                                    // TODO: diagnostic
                                }
                            }

                            index ??= -1;

                            if (isShortForm)
                            {
                                // Emit a byte instead of the int for the short form
                                _currentMethod.Definition.MethodBody.CodeBuilder.WriteByte((byte)index.Value);
                            }
                            else
                            {
                                _currentMethod.Definition.MethodBody.CodeBuilder.WriteInt32(index.Value);
                            }
                        }
                    }
                    break;
            }
            return GrammarResult.SentinelValue.Result;
        }

        public GrammarResult.Literal<ILOpCode> VisitInstr_brtarget(CILParser.Instr_brtargetContext context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        public GrammarResult.Literal<ILOpCode> VisitInstr_field(CILParser.Instr_fieldContext context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        public GrammarResult.Literal<ILOpCode> VisitInstr_i(CILParser.Instr_iContext context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        public GrammarResult.Literal<ILOpCode> VisitInstr_i8(CILParser.Instr_i8Context context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        public GrammarResult.Literal<ILOpCode> VisitInstr_method(CILParser.Instr_methodContext context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        public GrammarResult.Literal<ILOpCode> VisitInstr_none(CILParser.Instr_noneContext context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        public GrammarResult.Literal<ILOpCode> VisitInstr_r(CILParser.Instr_rContext context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        public GrammarResult.Literal<ILOpCode> VisitInstr_sig(CILParser.Instr_sigContext context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        public GrammarResult.Literal<ILOpCode> VisitInstr_string(CILParser.Instr_stringContext context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        public GrammarResult.Literal<ILOpCode> VisitInstr_switch(CILParser.Instr_switchContext context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        public GrammarResult.Literal<ILOpCode> VisitInstr_tok(CILParser.Instr_tokContext context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        public GrammarResult.Literal<ILOpCode> VisitInstr_type(CILParser.Instr_typeContext context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        public GrammarResult.Literal<ILOpCode> VisitInstr_var(CILParser.Instr_varContext context) => new(ParseOpCodeFromToken(((ITerminalNode)context.children[0]).Symbol));
        private static ILOpCode ParseOpCodeFromToken(IToken token)
        {
            return (ILOpCode)Enum.Parse(typeof(ILOpCode), token.Text.Replace('.', '_'), ignoreCase: true);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitInstr(CILParser.InstrContext context) => VisitInstr(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_brtarget(CILParser.Instr_brtargetContext context) => VisitInstr_brtarget(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_field(CILParser.Instr_fieldContext context) => VisitInstr_field(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_i(CILParser.Instr_iContext context) => VisitInstr_i(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_i8(CILParser.Instr_i8Context context) => VisitInstr_i8(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_method(CILParser.Instr_methodContext context) => VisitInstr_method(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_none(CILParser.Instr_noneContext context) => VisitInstr_none(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_r(CILParser.Instr_rContext context) => VisitInstr_r(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_sig(CILParser.Instr_sigContext context) => VisitInstr_sig(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_string(CILParser.Instr_stringContext context) => VisitInstr_string(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_switch(CILParser.Instr_switchContext context) => VisitInstr_switch(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_tok(CILParser.Instr_tokContext context) => VisitInstr_tok(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_type(CILParser.Instr_typeContext context) => VisitInstr_type(context);
        GrammarResult ICILVisitor<GrammarResult>.VisitInstr_var(CILParser.Instr_varContext context) => VisitInstr_var(context);

        private static bool ParseIntegerValue(ReadOnlySpan<char> value, out long result)
        {
            NumberStyles parseStyle = NumberStyles.None;
            bool negate = false;
            if (value.StartsWith("-".AsSpan()))
            {
                negate = true;
            }

            if (value.StartsWith("0x".AsSpan()))
            {
                parseStyle = NumberStyles.AllowHexSpecifier;
                value = value.Slice(2);
            }
            else if (value.StartsWith("0".AsSpan()))
            {
                // Octal support isn't built-in, so we'll do it manually.
                result = 0;
                for (int i = 0; i < value.Length; i++, result *= 8)
                {
                    int digitValue = value[i] - '0';
                    if (digitValue < 0 || digitValue > 7)
                    {
                        // COMPAT: native ilasm skips invalid digits silently
                        continue;
                    }
                    result += digitValue;
                }
                return true;
            }

            bool success = long.TryParse(value.ToString(), parseStyle, CultureInfo.InvariantCulture, out result);
            if (!success)
            {
                return false;
            }

            result *= negate ? -1 : 1;
            return true;
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitInt32(CILParser.Int32Context context)
        {
            return VisitInt32(context);
        }

        public GrammarResult.Literal<int> VisitInt32(CILParser.Int32Context context)
        {
            IToken node = context.INT32().Symbol;

            ReadOnlySpan<char> value = node.Text.AsSpan();

            if (!ParseIntegerValue(value, out long num))
            {
                _diagnostics.Add(new Diagnostic(
                    DiagnosticIds.LiteralOutOfRange,
                    DiagnosticSeverity.Error,
                    string.Format(DiagnosticMessageTemplates.LiteralOutOfRange, node.Text),
                    Location.From(node, _documents)));
                return new GrammarResult.Literal<int>(0);
            }

            return new GrammarResult.Literal<int>((int)num);
        }


        GrammarResult ICILVisitor<GrammarResult>.VisitInt64(CILParser.Int64Context context)
        {
            return VisitInt64(context);
        }

        public GrammarResult.Literal<long> VisitInt64(CILParser.Int64Context context)
        {
            IToken node = context.GetChild<ITerminalNode>(0).Symbol;

            ReadOnlySpan<char> value = node.Text.AsSpan();

            if (!ParseIntegerValue(value, out long num))
            {
                _diagnostics.Add(new Diagnostic(
                    DiagnosticIds.LiteralOutOfRange,
                    DiagnosticSeverity.Error,
                    string.Format(DiagnosticMessageTemplates.LiteralOutOfRange, node.Text),
                    Location.From(node, _documents)));
                return new GrammarResult.Literal<long>(0);
            }

            return new GrammarResult.Literal<long>(num);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitIntOrWildcard(CILParser.IntOrWildcardContext context) => VisitIntOrWildcard(context);
        public GrammarResult.Literal<int?> VisitIntOrWildcard(CILParser.IntOrWildcardContext context) => context.int32() is {} int32 ? new(VisitInt32(int32).Value) : new(null);
        public GrammarResult VisitLabels(CILParser.LabelsContext context) => throw new InvalidOperationException(NodeShouldNeverBeDirectlyVisited);
        public GrammarResult VisitLanguageDecl(CILParser.LanguageDeclContext context) => throw new NotImplementedException("TODO: Symbols");

        public GrammarResult VisitManifestResDecl(CILParser.ManifestResDeclContext context) => throw new InvalidOperationException(NodeShouldNeverBeDirectlyVisited);
        GrammarResult ICILVisitor<GrammarResult>.VisitManifestResDecls(CILParser.ManifestResDeclsContext context) => VisitManifestResDecls(context);
        public GrammarResult.Literal<(EntityRegistry.EntityBase? implementation, uint offset, ImmutableArray<EntityRegistry.CustomAttributeEntity> attributes)> VisitManifestResDecls(CILParser.ManifestResDeclsContext context)
        {
            EntityRegistry.EntityBase? implementation = null;
            uint offset = 0;
            var attributes = ImmutableArray.CreateBuilder<EntityRegistry.CustomAttributeEntity>();
            // COMPAT: Priority order for implementation is the following
            // AssemblyRef, File, nil
            foreach (var decl in context.manifestResDecl())
            {
                if (decl.customAttrDecl() is CILParser.CustomAttrDeclContext customAttrDecl)
                {
                    if (VisitCustomAttrDecl(customAttrDecl).Value is { } attr)
                    {
                        attributes.Add(attr);
                    }
                }
                string kind = decl.GetChild(0).GetText();
                if (kind == ".file" && implementation is not EntityRegistry.AssemblyReferenceEntity)
                {
                    var file = _entityRegistry.FindFile(VisitDottedName(decl.dottedName()).Value);
                    if (file is null)
                    {
                        // TODO: Report diagnostic
                    }
                    else
                    {
                        implementation = file;
                        offset = (uint)VisitInt32(decl.int32()).Value;
                    }
                }
                else if (kind == ".assembly")
                {
                    var asm = _entityRegistry.FindAssemblyReference(VisitDottedName(decl.dottedName()).Value);
                    if (asm is null)
                    {
                        // TODO: Report diagnostic
                    }
                    else
                    {
                        implementation = asm;
                    }
                }
            }

            return new((implementation, offset, attributes.ToImmutable()));
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitManifestResHead(CILParser.ManifestResHeadContext context) => VisitManifestResHead(context);
        public GrammarResult.Literal<(string name, string alias, ManifestResourceAttributes attr)> VisitManifestResHead(CILParser.ManifestResHeadContext context)
        {
            var dottedNames = context.dottedName();
            string name = VisitDottedName(dottedNames[0]).Value;
            string alias = dottedNames.Length == 2 ? VisitDottedName(dottedNames[1]).Value : name;
            ManifestResourceAttributes attr = 0;
            foreach (var attrContext in context.manresAttr())
            {
                attr |= VisitManresAttr(attrContext).Value;
            }

            return new((name, alias, attr));
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitManresAttr(CILParser.ManresAttrContext context) => VisitManresAttr(context);
        public GrammarResult.Flag<ManifestResourceAttributes> VisitManresAttr(CILParser.ManresAttrContext context)
        {
            return context.GetText() switch
            {
                "public" => new(ManifestResourceAttributes.Public),
                "private" => new(ManifestResourceAttributes.Private),
                _ => throw new InvalidOperationException("unreachable")
            };
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitMarshalBlob(CILParser.MarshalBlobContext context) => VisitMarshalBlob(context);
        public GrammarResult.FormattedBlob VisitMarshalBlob(CILParser.MarshalBlobContext context)
        {
            if (context.hexbytes() is CILParser.HexbytesContext hexBytes)
            {
                var bytes = VisitHexbytes(hexBytes).Value;
                var blob = new BlobBuilder(bytes.Length);
                blob.WriteBytes(bytes);
                return new(blob);
            }

            return VisitNativeType(context.nativeType());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitMarshalClause(CILParser.MarshalClauseContext context) => VisitMarshalClause(context);
        public GrammarResult.FormattedBlob VisitMarshalClause(CILParser.MarshalClauseContext context) => VisitMarshalBlob(context.marshalBlob());

        GrammarResult ICILVisitor<GrammarResult>.VisitMdtoken(ILAssembler.CILParser.MdtokenContext context) => VisitMdtoken(context);
        public GrammarResult.Literal<EntityRegistry.EntityBase> VisitMdtoken(CILParser.MdtokenContext context)
        {
            return new(_entityRegistry.ResolveHandleToEntity(MetadataTokens.EntityHandle(VisitInt32(context.int32()).Value)));
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitMemberRef(CILParser.MemberRefContext context) => VisitMemberRef(context);
        public GrammarResult.Literal<EntityRegistry.EntityBase> VisitMemberRef(CILParser.MemberRefContext context)
        {
            if (context.mdtoken() is CILParser.MdtokenContext mdToken)
            {
                return VisitMdtoken(mdToken);
            }

            if (context.methodRef() is CILParser.MethodRefContext methodRef)
            {
                return VisitMethodRef(methodRef);
            }
            if (context.fieldRef() is CILParser.FieldRefContext fieldRef)
            {
                return VisitFieldRef(fieldRef);
            }

            throw new InvalidOperationException("unreachable");
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitMethAttr(CILParser.MethAttrContext context) => VisitMethAttr(context);
        public GrammarResult.Flag<MethodAttributes> VisitMethAttr(CILParser.MethAttrContext context)
        {
            if (context.int32() is CILParser.Int32Context int32)
            {
                return new((MethodAttributes)VisitInt32(int32).Value, ShouldAppend: false);
            }
            string attribute = context.GetText();
            return attribute switch
            {
                "static" => new(MethodAttributes.Static),
                "public" => new(MethodAttributes.Public, MethodAttributes.MemberAccessMask),
                "private" => new(MethodAttributes.Private, MethodAttributes.MemberAccessMask),
                "family" => new(MethodAttributes.Family, MethodAttributes.MemberAccessMask),
                "final" => new(MethodAttributes.Final),
                "specialname" => new(MethodAttributes.SpecialName),
                "virtual" => new(MethodAttributes.Virtual),
                "strict" => new(MethodAttributes.CheckAccessOnOverride),
                "abstract" => new(MethodAttributes.Abstract),
                "assembly" => new(MethodAttributes.Assembly, MethodAttributes.MemberAccessMask),
                "famandassem" => new(MethodAttributes.FamANDAssem, MethodAttributes.MemberAccessMask),
                "famorassem" => new(MethodAttributes.FamORAssem, MethodAttributes.MemberAccessMask),
                "privatescope" => new(MethodAttributes.PrivateScope, MethodAttributes.MemberAccessMask),
                "hidebysig" => new(MethodAttributes.HideBySig),
                "newslot" => new(MethodAttributes.NewSlot),
                "rtspecialname" => new(0), // COMPAT: Rtspecialname is ignored
                "unmanagedexp" => new(MethodAttributes.UnmanagedExport),
                "reqsecobj" => new(MethodAttributes.RequireSecObject),
                _ => throw new InvalidOperationException("unreachable"),
            };
        }

        public GrammarResult VisitMethodDecl(CILParser.MethodDeclContext context)
        {
            Debug.Assert(_currentMethod is not null);
            var currentMethod = _currentMethod!;
            if (context.EMITBYTE() is not null)
            {
                currentMethod.Definition.MethodBody.CodeBuilder.WriteByte((byte)VisitInt32(context.GetChild<CILParser.Int32Context>(0)).Value);
            }
            else if (context.ENTRYPOINT() is not null)
            {
                _entityRegistry.EntryPoint = currentMethod.Definition;
            }
            else if (context.ZEROINIT() is not null)
            {
                currentMethod.Definition.BodyAttributes = MethodBodyAttributes.InitLocals;
            }
            else if (context.MAXSTACK() is not null)
            {
                currentMethod.Definition.MaxStack = VisitInt32(context.GetChild<CILParser.Int32Context>(0)).Value;
            }
            else if (context.LOCALS() is not null)
            {
                if (context.ChildCount == 3)
                {
                    // init keyword specified
                    currentMethod.Definition.BodyAttributes = MethodBodyAttributes.InitLocals;
                }
                var localsScope = currentMethod.LocalsScopes.Count != 0 ? currentMethod.LocalsScopes[currentMethod.LocalsScopes.Count - 1] : new();
                var newLocals = VisitSigArgs(context.sigArgs()).Value;
                foreach (var loc in newLocals)
                {
                    // BREAK-COMPAT: We don't allow specifying a local's slot via the [in], [out], or [opt] parameter attributes, or the custom int override.
                    // This only worked in ilasm due to how ilasm reused fields.
                    // We're only going to support allowing this tool to determine the slot numbers.
                    // This blocks two different locals in two different scopes from resuing the same slot
                    // but that is a very rare scenario (even using more than one .locals block in a method in IL is quite rare)

                    // If the local is named, add it to our name-lookup dictionary.
                    // Otherwise, it will only be accessible via its index.
                    if (loc.Name is not null)
                    {
                        localsScope.Add(loc.Name, currentMethod.AllLocals.Count);
                    }
                    currentMethod.AllLocals.Add(loc);
                }
            }
            else if (context.ChildCount == 2 && context.GetChild(0) is CILParser.IdContext labelId)
            {
                string labelName = VisitId(labelId).Value;
                if (!currentMethod.Labels.TryGetValue(labelName, out var label))
                {
                    label = currentMethod.Definition.MethodBody.DefineLabel();
                }
                currentMethod.Definition.MethodBody.MarkLabel(label);
            }
            else if (context.EXPORT() is not null)
            {
                // TODO: Need custom ManagedPEBuilder subclass to write the exports directory.
            }
            else if (context.VTENTRY() is not null)
            {
                // TODO: Need custom ManagedPEBuilder subclass to write the exports directory.
            }
            else if (context.OVERRIDE() is not null)
            {
                BlobBuilder signature = currentMethod.Definition.MethodSignature!;
                if (context.callConv() is {} callConv)
                {
                    // We have an explicitly specified signature, so we need to parse it.
                    signature = new();
                    var callConvByte = VisitCallConv(callConv).Value;
                    var arity = VisitGenArity(context.genArity()).Value;
                    if (arity > 0)
                    {
                        callConvByte |= (byte)SignatureAttributes.Generic;
                    }
                    signature.WriteByte(callConvByte);
                    if (arity > 0)
                    {
                        signature.WriteCompressedInteger(arity);
                    }
                    var args = VisitSigArgs(context.sigArgs()).Value;
                    signature.WriteCompressedInteger(args.Length);
                    VisitType(context.type()).Value.WriteContentTo(signature);
                    foreach (var arg in args)
                    {
                        arg.SignatureBlob.WriteContentTo(signature);
                    }
                }

                var ownerType = VisitTypeSpec(context.typeSpec()).Value;
                var methodName = VisitMethodName(context.methodName()).Value;
                var methodRef = _entityRegistry.CreateLazilyRecordedMemberReference(ownerType, methodName, signature);
                _currentTypeDefinition.PeekOrDefault()!.MethodImplementations.Add(EntityRegistry.CreateUnrecordedMethodImplementation(currentMethod.Definition, methodRef));
            }
            else if (context.PARAM() is not null)
            {
                // BREAK-COMPAT: We require attributes on parameters, generic parameters, and constraints
                // to be specified directly after the .param directive, not at any point later in the method.
                // This matches the IL outputted by ILDASM, ILSpy, and other tools in the ecosystem.
                // Attributes not specified directly after the .param directive are applied to the method itself.
                var customAttrDeclarations = context.customAttrDecl();
                if (context.TYPE() is not null)
                {
                    // Type parameters
                    EntityRegistry.GenericParameterEntity? param = null;
                    if (context.int32() is { } int32)
                    {
                        int index = VisitInt32(int32[0]).Value;
                        if (index < 0 || index >= currentMethod.Definition.GenericParameters.Count)
                        {
                            // TODO: Report generic parameter index out of range
                            return GrammarResult.SentinelValue.Result;
                        }
                        param = currentMethod.Definition.GenericParameters[index];
                    }
                    else
                    {
                        string name = VisitDottedName(context.dottedName()).Value;
                        foreach (var genericParam in currentMethod.Definition.GenericParameters)
                        {
                            if (genericParam.Name == name)
                            {
                                param = genericParam;
                                break;
                            }
                        }
                        if (param is null)
                        {
                            // TODO: Report unknown generic parameter
                            return GrammarResult.SentinelValue.Result;
                        }
                    }
                    foreach (var attr in customAttrDeclarations ?? Array.Empty<CILParser.CustomAttrDeclContext>())
                    {
                        var customAttrDecl = VisitCustomAttrDecl(attr).Value;
                        if (customAttrDecl is not null)
                        {
                            customAttrDecl.Owner = param;
                        }
                    }
                }
                else if (context.CONSTRAINT() is not null)
                {
                    // constraints
                    EntityRegistry.GenericParameterEntity? param = null;
                    if (context.int32() is { } int32)
                    {
                        int index = VisitInt32(int32[0]).Value;
                        if (index < 0 || index >= currentMethod.Definition.GenericParameters.Count)
                        {
                            // TODO: Report generic parameter index out of range
                            return GrammarResult.SentinelValue.Result;
                        }
                        param = currentMethod.Definition.GenericParameters[index];
                    }
                    else
                    {
                        string name = VisitDottedName(context.dottedName()).Value;
                        foreach (var genericParam in currentMethod.Definition.GenericParameters)
                        {
                            if (genericParam.Name == name)
                            {
                                param = genericParam;
                                break;
                            }
                        }
                        if (param is null)
                        {
                            // TODO: Report unknown generic parameter
                            return GrammarResult.SentinelValue.Result;
                        }
                    }
                    EntityRegistry.GenericParameterConstraintEntity? constraint = null;
                    var baseType = VisitTypeSpec(context.typeSpec()).Value;
                    foreach (var constraintEntity in param.Constraints)
                    {
                        if (constraintEntity.BaseType == baseType)
                        {
                            constraint = constraintEntity;
                            break;
                        }
                    }
                    if (constraint is null)
                    {
                        constraint = EntityRegistry.CreateGenericConstraint(baseType);
                        constraint.Owner = param;
                        param.Constraints.Add(constraint);
                        currentMethod.Definition.GenericParameterConstraints.Add(constraint);
                    }
                    foreach (var attr in customAttrDeclarations ?? Array.Empty<CILParser.CustomAttrDeclContext>())
                    {
                        var customAttrDecl = VisitCustomAttrDecl(attr).Value;
                        if (customAttrDecl is not null)
                        {
                            customAttrDecl.Owner = constraint;
                        }
                    }
                }
                else
                {
                    // Adding attibutes to parameters.
                    int index = VisitInt32(context.int32()[0]).Value;
                    if (index < 0 || index >= currentMethod.Definition.Parameters.Count)
                    {
                        // TODO: Report parameter index out of range
                        return GrammarResult.SentinelValue.Result;
                    }

                    // TODO: Visit initOpt to get the Constant table entry if a constant value is provided.
                    var param = currentMethod.Definition.Parameters[index];
                    foreach (var attr in customAttrDeclarations ?? Array.Empty<CILParser.CustomAttrDeclContext>())
                    {
                        var customAttrDecl = VisitCustomAttrDecl(attr).Value;
                        if (customAttrDecl is not null)
                        {
                            customAttrDecl.Owner = param;
                            param.HasCustomAttributes = true;
                        }
                    }
                }
            }
            else if (context.secDecl() is {} secDecl)
            {
                var declarativeSecurity = VisitSecDecl(secDecl).Value;
                if (declarativeSecurity is not null)
                {
                    declarativeSecurity.Parent = currentMethod.Definition;
                }
            }
            else if (context.customAttrDecl() is {} customAttr)
            {
                foreach (var attr in customAttr)
                {
                    var customAttrDecl = VisitCustomAttrDecl(attr).Value;
                    if (customAttrDecl is not null)
                    {
                        customAttrDecl.Owner = currentMethod.Definition;
                    }
                }
            }
            else
            {
                _ = context.children[0].Accept(this);
            }
            return GrammarResult.SentinelValue.Result;
        }
        public GrammarResult VisitMethodDecls(CILParser.MethodDeclsContext context)
        {
            foreach (var decl in context.methodDecl())
            {
                VisitMethodDecl(decl);
            }
            return GrammarResult.SentinelValue.Result;
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitMethodHead(CILParser.MethodHeadContext context) => VisitMethodHead(context);
        public GrammarResult.Literal<EntityRegistry.MethodDefinitionEntity> VisitMethodHead(CILParser.MethodHeadContext context)
        {
            string name = VisitMethodName(context.methodName()).Value;
            var containingType = _currentTypeDefinition.PeekOrDefault() ?? _entityRegistry.ModuleType;
            var methodDefinition = EntityRegistry.CreateUnrecordedMethodDefinition(containingType, name);

            BlobBuilder methodSignature = new();
            byte sigHeader = VisitCallConv(context.callConv()).Value;

            // Set the current method for type parameter and signature parsing
            // so we can resolve generic parameters correctly.
            _currentMethod = new(methodDefinition);
            var typeParameters = VisitTyparsClause(context.typarsClause()).Value;
            if (typeParameters.Length != 0)
            {
                sigHeader |= (byte)SignatureAttributes.Generic;
            }
            methodDefinition.MethodAttributes = context.methAttr().Aggregate((MethodAttributes)0, (acc, attr) => acc | VisitMethAttr(attr));

            if (methodDefinition.MethodAttributes.HasFlag(MethodAttributes.Abstract) && !methodDefinition.ContainingType.Attributes.HasFlag(TypeAttributes.Abstract))
            {
                // TODO:Emit error
            }

            (EntityRegistry.ModuleReferenceEntity Module, string? EntryPoint, MethodImportAttributes Attributes)? pInvokeInformation = null;
            foreach (var pInvokeInfo in context.pinvImpl())
            {
                var (moduleName, entryPoint, attributes) = VisitPinvImpl(pInvokeInfo).Value;
                if (moduleName is null)
                {
                    // TODO: Emit error
                    continue;
                }
                pInvokeInformation = (_entityRegistry.GetOrCreateModuleReference(moduleName, _ => { }), entryPoint, attributes);
            }
            methodDefinition.MethodImportInformation = pInvokeInformation;

            SignatureHeader parsedHeader = new(sigHeader);
            if (methodDefinition.MethodAttributes.HasFlag(MethodAttributes.Static) && (parsedHeader.IsInstance || parsedHeader.HasExplicitThis))
            {
                // Error on static + instance.
            }
            if (parsedHeader.HasExplicitThis && !parsedHeader.IsInstance)
            {
                // Warn on explicit-this + non-instance
                parsedHeader = new(sigHeader |= (byte)SignatureAttributes.Instance);
            }
            methodSignature.WriteByte(sigHeader);
            if (typeParameters.Length != 0)
            {
                methodSignature.WriteCompressedInteger(typeParameters.Length);
            }
            for (int i = 0; i < typeParameters.Length; i++)
            {
                EntityRegistry.GenericParameterEntity? param = typeParameters[i];
                param.Owner = methodDefinition;
                param.Index = i;
                methodDefinition.GenericParameters.Add(param);
                foreach (var constraint in param.Constraints)
                {
                    constraint.Owner = param;
                    methodDefinition.GenericParameterConstraints.Add(constraint);
                }
            }

            var args = VisitSigArgs(context.sigArgs()).Value;
            methodSignature.WriteCompressedInteger(args.Length);

            SignatureArg returnValue = new(VisitParamAttr(context.paramAttr()).Value, VisitType(context.type()).Value, VisitMarshalClause(context.marshalClause()).Value, null);

            returnValue.SignatureBlob.WriteContentTo(methodSignature);
            methodDefinition.Parameters.Add(EntityRegistry.CreateParameter(returnValue.Attributes, returnValue.Name, returnValue.MarshallingDescriptor, 0));
            for (int i = 0; i < args.Length; i++)
            {
                SignatureArg? arg = args[i];
                arg.SignatureBlob.WriteContentTo(methodSignature);
                methodDefinition.Parameters.Add(EntityRegistry.CreateParameter(arg.Attributes, arg.Name, arg.MarshallingDescriptor, i + 1));
            }
            // We've parsed all signature information. We can reset the current method now (the caller will handle setting/unsetting it for the method body).
            _currentMethod = null;
            methodDefinition.SignatureHeader = parsedHeader;
            methodDefinition.MethodSignature = methodSignature;

            methodDefinition.ImplementationAttributes = context.implAttr().Aggregate((MethodImplAttributes)0, (acc, attr) => acc | VisitImplAttr(attr));
            if (!EntityRegistry.TryAddMethodDefinitionToContainingType(methodDefinition))
            {
                // TODO: Report duplicate method
            }

            return new(methodDefinition);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitMethodName(CILParser.MethodNameContext context) => VisitMethodName(context);
        public GrammarResult.String VisitMethodName(CILParser.MethodNameContext context)
        {
            IParseTree child = context.GetChild(0);
            if (child is ITerminalNode terminal)
            {
                return new(terminal.Symbol.Text);
            }
            Debug.Assert(child is CILParser.DottedNameContext);
            return (GrammarResult.String)child.Accept(this);
        }

        private bool _expectInstance;
        private Subsystem _subsystem = Subsystem.WindowsCui;
        private CorFlags _corflags = CorFlags.ILOnly;
        private int _alignment = 0x200;
        private long _imageBase = 0x00400000;
        private long _stackReserve;

        GrammarResult ICILVisitor<GrammarResult>.VisitMethodRef(CILParser.MethodRefContext context) => VisitMethodRef(context);
        public GrammarResult.Literal<EntityRegistry.EntityBase> VisitMethodRef(CILParser.MethodRefContext context)
        {
            if (context.mdtoken() is CILParser.MdtokenContext token)
            {
                return new(VisitMdtoken(token).Value);
            }
            if (context.dottedName() is CILParser.DottedNameContext)
            {
                // TODO: typedef
                throw new NotImplementedException();
            }
            BlobBuilder methodRefSignature = new();
            byte callConv = VisitCallConv(context.callConv()).Value;
            EntityRegistry.TypeEntity owner = _currentTypeDefinition.PeekOrDefault() ?? _entityRegistry.ModuleType;
            if (context.typeSpec() is CILParser.TypeSpecContext typeSpec)
            {
                owner = VisitTypeSpec(typeSpec).Value;
            }
            string name = VisitMethodName(context.methodName()).Value;
            BlobBuilder? methodSpecSignature = null;
            int numGenericParameters = 0;
            if (context.typeArgs() is CILParser.TypeArgsContext typeArgs)
            {
                var types = typeArgs.type();
                numGenericParameters = types.Length;
                if (types.Length != 0)
                {
                    methodSpecSignature = new();
                    methodSpecSignature.WriteByte((byte)SignatureKind.MethodSpecification);
                    VisitTypeArgs(typeArgs).Value.WriteContentTo(methodSpecSignature);
                }
            }
            else if (context.genArityNotEmpty() is CILParser.GenArityNotEmptyContext genArityNotEmpty)
            {
                numGenericParameters = VisitGenArityNotEmpty(genArityNotEmpty).Value;
            }
            if (numGenericParameters != 0)
            {
                callConv |= (byte)SignatureAttributes.Generic;
            }
            if (_expectInstance && (callConv & (byte)SignatureAttributes.Instance) == 0)
            {
                // TODO: Warn for missing instance call-conv
                callConv |= (byte)SignatureAttributes.Instance;
            }
            methodRefSignature.WriteByte(callConv);
            if (numGenericParameters != 0)
            {
                methodRefSignature.WriteCompressedInteger(numGenericParameters);
            }
            var args = VisitSigArgs(context.sigArgs()).Value;
            methodRefSignature.WriteCompressedInteger(args.Length);
            // Write return type
            VisitType(context.type()).Value.WriteContentTo(methodRefSignature);
            // Write arg signatures
            foreach (var arg in args)
            {
                arg.SignatureBlob.WriteContentTo(methodRefSignature);
            }

            var memberRef = _entityRegistry.CreateLazilyRecordedMemberReference(owner, name, methodRefSignature);

            if (methodSpecSignature is not null)
            {
                return new(_entityRegistry.GetOrCreateMethodSpecification(memberRef, methodSpecSignature));
            }

            return new(memberRef);
        }
        public GrammarResult VisitModuleHead(CILParser.ModuleHeadContext context)
        {
            if (context.ChildCount > 2)
            {
                _ = _entityRegistry.GetOrCreateModuleReference(VisitDottedName(context.dottedName()).Value, _ => { });
                return GrammarResult.SentinelValue.Result;
            }

            _entityRegistry.Module.Name = VisitDottedName(context.dottedName()).Value;
            return GrammarResult.SentinelValue.Result;
        }
        public GrammarResult VisitMscorlib(CILParser.MscorlibContext context) => GrammarResult.SentinelValue.Result;

        GrammarResult ICILVisitor<GrammarResult>.VisitNameSpaceHead(CILParser.NameSpaceHeadContext context) => VisitNameSpaceHead(context);

        public static GrammarResult.String VisitNameSpaceHead(CILParser.NameSpaceHeadContext context) => VisitDottedName(context.dottedName());

        GrammarResult ICILVisitor<GrammarResult>.VisitNameValPair(CILParser.NameValPairContext context) => VisitNameValPair(context);
        public GrammarResult.Literal<KeyValuePair<string, BlobBuilder>> VisitNameValPair(CILParser.NameValPairContext context)
        {
            return new(new(VisitCompQstring(context.compQstring()).Value, VisitCaValue(context.caValue()).Value));
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitNameValPairs(CILParser.NameValPairsContext context) => VisitNameValPairs(context);
        public GrammarResult.Sequence<KeyValuePair<string, BlobBuilder>> VisitNameValPairs(CILParser.NameValPairsContext context) => new(context.nameValPair().Select(pair => VisitNameValPair(pair).Value).ToImmutableArray());

        GrammarResult ICILVisitor<GrammarResult>.VisitNativeType(CILParser.NativeTypeContext context) => VisitNativeType(context);
        public GrammarResult.FormattedBlob VisitNativeType(CILParser.NativeTypeContext context)
        {
            if (context.nativeTypeArrayPointerInfo() is not CILParser.NativeTypeArrayPointerInfoContext[] arrayPointerInfo)
            {
                return VisitNativeTypeElement(context.nativeTypeElement());
            }
            var prefix = new BlobBuilder(arrayPointerInfo.Length);
            var elementType = VisitNativeTypeElement(context.nativeTypeElement()).Value;
            var suffix = new BlobBuilder();

            for (int i = arrayPointerInfo.Length - 1; i >= 0; i--)
            {
                if (arrayPointerInfo[i] is CILParser.PointerNativeTypeContext)
                {
                    // TODO: warn on deprecated native type
                    const int NATIVE_TYPE_PTR = 0x10;
                    prefix.WriteByte(NATIVE_TYPE_PTR);
                }
                else
                {
                    prefix.WriteByte((byte)UnmanagedType.LPArray);
                    if (elementType.Count == 0)
                    {
                        // We need to have an element type for arrays,
                        // so write the invalid NATIVE_TYPE_MAX value so we have something parsable.
                        const int NATIVE_TYPE_MAX = 0x50;
                        elementType.WriteByte(NATIVE_TYPE_MAX);
                    }
                }
            }

            for (int i = 0; i < arrayPointerInfo.Length; i++)
            {
                if (arrayPointerInfo[i] is CILParser.PointerArrayTypeSizeContext size)
                {
                    suffix.WriteCompressedInteger(VisitInt32(size.int32()).Value);
                }
                else if (arrayPointerInfo[i] is CILParser.PointerArrayTypeSizeParamIndexContext sizeParamIndex)
                {
                    var ints = sizeParamIndex.int32();
                    suffix.WriteCompressedInteger(VisitInt32(ints[1]).Value);
                    suffix.WriteCompressedInteger(VisitInt32(ints[2]).Value);
                    suffix.WriteCompressedInteger(1); // Write that the paramIndex parameter was specified
                }
                else if (arrayPointerInfo[i] is CILParser.PointerArrayTypeParamIndexContext paramIndex)
                {
                    suffix.WriteCompressedInteger(0);
                    suffix.WriteCompressedInteger(VisitInt32(paramIndex.int32()).Value);
                    suffix.WriteCompressedInteger(0); // Write that the paramIndex parameter was not specified
                }
            }

            prefix.LinkSuffix(elementType);
            prefix.LinkSuffix(suffix);
            return new(prefix);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitNativeTypeElement(CILParser.NativeTypeElementContext context) => VisitNativeTypeElement(context);
        public GrammarResult.FormattedBlob VisitNativeTypeElement(CILParser.NativeTypeElementContext context)
        {
            var blob = new BlobBuilder(5);
            if (context.dottedName() is CILParser.DottedNameContext typedef)
            {
                _ = typedef;
                // TODO: typedef
                return new(blob);
            }

            if (context.marshalType is null)
            {
                return new(blob);
            }

            switch (context.marshalType.Type)
            {
                case CILParser.CUSTOM:
                    {
                        blob.WriteByte((byte)UnmanagedType.CustomMarshaler);
                        CILParser.CompQstringContext[] strings = context.compQstring();
                        if (strings.Length == 4)
                        {
                            // TODO: warn on deprecated 4-string form of custom marshaller.
                            blob.WriteSerializedString(VisitCompQstring(strings[0]).Value);
                            blob.WriteSerializedString(VisitCompQstring(strings[1]).Value);
                            blob.WriteSerializedString(VisitCompQstring(strings[2]).Value);
                            blob.WriteSerializedString(VisitCompQstring(strings[3]).Value);
                        }
                        else
                        {
                            Debug.Assert(strings.Length == 2);
                            blob.WriteCompressedInteger(0);
                            blob.WriteCompressedInteger(0);
                            blob.WriteSerializedString(VisitCompQstring(strings[0]).Value);
                            blob.WriteSerializedString(VisitCompQstring(strings[1]).Value);
                        }
                        break;
                    }
                case CILParser.SYSSTRING:
                    blob.WriteByte((byte)UnmanagedType.ByValTStr);
                    blob.WriteCompressedInteger(VisitInt32(context.int32()).Value);
                    break;
                case CILParser.ARRAY:
                    blob.WriteByte((byte)UnmanagedType.ByValArray);
                    blob.WriteCompressedInteger(VisitInt32(context.int32()).Value);
                    blob.LinkSuffix(VisitNativeType(context.nativeType()).Value);
                    break;
                case CILParser.VARIANT:
                    // TODO: warn on deprecated native type
                    const int NATIVE_TYPE_VARIANT = 0xe;
                    blob.WriteByte(NATIVE_TYPE_VARIANT);
                    break;
                case CILParser.CURRENCY:
                    blob.WriteByte((byte)UnmanagedType.Currency);
                    break;
                case CILParser.SYSCHAR:
                    // TODO: warn on deprecated native type
                    const int NATIVE_TYPE_SYSCHAR = 0xd;
                    blob.WriteByte(NATIVE_TYPE_SYSCHAR);
                    break;
                case CILParser.VOID:
                    // TODO: warn on deprecated native type
                    const int NATIVE_TYPE_VOID = 0x1;
                    blob.WriteByte(NATIVE_TYPE_VOID);
                    break;
                case CILParser.BOOL:
                    blob.WriteByte((byte)UnmanagedType.Bool);
                    break;
                case CILParser.INT8:
                    blob.WriteByte((byte)UnmanagedType.I1);
                    break;
                case CILParser.INT16:
                    blob.WriteByte((byte)UnmanagedType.I2);
                    break;
                case CILParser.INT32_:
                    blob.WriteByte((byte)UnmanagedType.I4);
                    break;
                case CILParser.INT64_:
                    blob.WriteByte((byte)UnmanagedType.I8);
                    break;
                case CILParser.FLOAT32:
                    blob.WriteByte((byte)UnmanagedType.R4);
                    break;
                case CILParser.FLOAT64_:
                    blob.WriteByte((byte)UnmanagedType.R8);
                    break;
                case CILParser.ERROR:
                    blob.WriteByte((byte)UnmanagedType.Error);
                    break;
                case CILParser.UINT8:
                    blob.WriteByte((byte)UnmanagedType.U1);
                    break;
                case CILParser.UINT16:
                    blob.WriteByte((byte)UnmanagedType.U2);
                    break;
                case CILParser.UINT32:
                    blob.WriteByte((byte)UnmanagedType.U4);
                    break;
                case CILParser.UINT64:
                    blob.WriteByte((byte)UnmanagedType.U8);
                    break;
                case CILParser.DECIMAL:
                    // TODO: warn on deprecated native type
                    const int NATIVE_TYPE_DECIMAL = 0x11;
                    blob.WriteByte(NATIVE_TYPE_DECIMAL);
                    break;
                case CILParser.DATE:
                    // TODO: warn on deprecated native type
                    const int NATIVE_TYPE_DATE = 0x12;
                    blob.WriteByte(NATIVE_TYPE_DATE);
                    break;
                case CILParser.BSTR:
                    blob.WriteByte((byte)UnmanagedType.BStr);
                    break;
                case CILParser.LPSTR:
                    blob.WriteByte((byte)UnmanagedType.LPStr);
                    break;
                case CILParser.LPWSTR:
                    blob.WriteByte((byte)UnmanagedType.LPWStr);
                    break;
                case CILParser.LPTSTR:
                    blob.WriteByte((byte)UnmanagedType.LPTStr);
                    break;
                case CILParser.OBJECTREF:
                    // TODO: warn on deprecated native type
                    const int NATIVE_TYPE_OBJECTREF = 0x18;
                    blob.WriteByte(NATIVE_TYPE_OBJECTREF);
                    break;
                case CILParser.IUNKNOWN:
                    {
                        blob.WriteByte((byte)UnmanagedType.IUnknown);
                        if (VisitIidParamIndex(context.iidParamIndex()) is { Value: int index })
                        {
                            blob.WriteCompressedInteger(index);
                        }
                        break;
                    }
                case CILParser.IDISPATCH:
                    {
                        blob.WriteByte((byte)UnmanagedType.IDispatch);
                        if (VisitIidParamIndex(context.iidParamIndex()) is { Value: int index })
                        {
                            blob.WriteCompressedInteger(index);
                        }
                        break;
                    }
                case CILParser.STRUCT:
                    blob.WriteByte((byte)UnmanagedType.Struct);
                    break;
                case CILParser.INTERFACE:
                    {
                        blob.WriteByte((byte)UnmanagedType.Interface);
                        if (VisitIidParamIndex(context.iidParamIndex()) is { Value: int index })
                        {
                            blob.WriteCompressedInteger(index);
                        }
                        break;
                    }
                case CILParser.SAFEARRAY:
                    blob.WriteByte((byte)UnmanagedType.SafeArray);
                    blob.WriteCompressedInteger((int)VisitVariantType(context.variantType()).Value);
                    if (context.compQstring() is { Length: 1 } safeArrayCustomType)
                    {
                        string str = VisitCompQstring(safeArrayCustomType[0]).Value;
                        blob.WriteSerializedString(str);
                    }
                    else
                    {
                        blob.WriteCompressedInteger(0);
                    }
                    break;
                case CILParser.INT:
                    blob.WriteByte((byte)UnmanagedType.SysInt);
                    break;
                case CILParser.UINT:
                    blob.WriteByte((byte)UnmanagedType.SysUInt);
                    break;
                case CILParser.NESTEDSTRUCT:
                    // TODO: warn on deprecated native type
                    const int NATIVE_TYPE_NESTEDSTRUCT = 0x21;
                    blob.WriteByte(NATIVE_TYPE_NESTEDSTRUCT);
                    break;
                case CILParser.BYVALSTR:
                    blob.WriteByte((byte)UnmanagedType.VBByRefStr);
                    break;
                case CILParser.ANSIBSTR:
                    blob.WriteByte((byte)UnmanagedType.AnsiBStr);
                    break;
                case CILParser.TBSTR:
                    blob.WriteByte((byte)UnmanagedType.TBStr);
                    break;
                case CILParser.VARIANTBOOL:
                    blob.WriteByte((byte)UnmanagedType.VariantBool);
                    break;
                case CILParser.METHOD:
                    blob.WriteByte((byte)UnmanagedType.FunctionPtr);
                    break;
                case CILParser.LPSTRUCT:
                    blob.WriteByte((byte)UnmanagedType.LPStruct);
                    break;
                case CILParser.ANY:
                    blob.WriteByte((byte)UnmanagedType.AsAny);
                    break;
            }

            throw new NotImplementedException();
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitObjSeq(CILParser.ObjSeqContext context) => VisitObjSeq(context);
        public GrammarResult.FormattedBlob VisitObjSeq(CILParser.ObjSeqContext context)
        {
            // We're going to add all of the elements in the sequence as prefix blobs to this blob.
            BlobBuilder objSeqBlob = new(0);
            foreach (var item in context.serInit())
            {
                objSeqBlob.LinkPrefix(VisitSerInit(item).Value);
            }
            return new(objSeqBlob);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitOwnerType(CILParser.OwnerTypeContext context) => VisitOwnerType(context);
        public GrammarResult.Literal<EntityRegistry.EntityBase> VisitOwnerType(CILParser.OwnerTypeContext context)
        {
            if (context.memberRef() is CILParser.MemberRefContext memberRef)
            {
                return VisitMemberRef(memberRef);
            }
            if (context.typeSpec() is CILParser.TypeSpecContext typeSpec)
            {
                return new(VisitTypeSpec(typeSpec).Value);
            }
            throw new InvalidOperationException("unreachable");
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitParamAttr(CILParser.ParamAttrContext context) => VisitParamAttr(context);
        public GrammarResult.Literal<ParameterAttributes> VisitParamAttr(CILParser.ParamAttrContext context)
        {
            ParameterAttributes attributes = 0;
            foreach (var element in context.paramAttrElement())
            {
                attributes |= VisitParamAttrElement(element);
            }
            return new(attributes);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitParamAttrElement(CILParser.ParamAttrElementContext context) => VisitParamAttrElement(context);
        public GrammarResult.Flag<ParameterAttributes> VisitParamAttrElement(CILParser.ParamAttrElementContext context)
        {
            if (context.int32() is CILParser.Int32Context int32)
            {
                return new((ParameterAttributes)(VisitInt32(int32).Value + 1), ShouldAppend: false);
            }
            return context switch
            {
                { @in: not null } => new(ParameterAttributes.In),
                { @out: not null } => new(ParameterAttributes.Out),
                { opt: not null } => new(ParameterAttributes.Optional),
                _ => throw new InvalidOperationException("unreachable")
            };
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitPinvAttr(CILParser.PinvAttrContext context) => VisitPinvAttr(context);
        public GrammarResult.Flag<MethodImportAttributes> VisitPinvAttr(CILParser.PinvAttrContext context)
        {
            if (context.int32() is CILParser.Int32Context int32)
            {
                return new((MethodImportAttributes)VisitInt32(int32).Value, ShouldAppend: false);
            }
            switch(context.GetText())
            {
                case "nomangle":
                    return new(MethodImportAttributes.ExactSpelling);
                case "ansi":
                    return new(MethodImportAttributes.CharSetAnsi);
                case "unicode":
                    return new(MethodImportAttributes.CharSetUnicode);
                case "autochar":
                    return new(MethodImportAttributes.CharSetAuto);
                case "lasterr":
                    return new(MethodImportAttributes.SetLastError);
                case "winapi":
                    return new(MethodImportAttributes.CallingConventionWinApi);
                case "cdecl":
                    return new(MethodImportAttributes.CallingConventionCDecl);
                case "stdcall":
                    return new(MethodImportAttributes.CallingConventionStdCall);
                case "thiscall":
                    return new(MethodImportAttributes.CallingConventionThisCall);
                case "fastcall":
                    return new(MethodImportAttributes.CallingConventionFastCall);
                case "bestfit:on":
                    return new(MethodImportAttributes.BestFitMappingEnable);
                case "bestfit:off":
                    return new(MethodImportAttributes.BestFitMappingDisable);
                case "charmaperror:on":
                    return new(MethodImportAttributes.ThrowOnUnmappableCharEnable);
                case "charmaperror:off":
                    return new(MethodImportAttributes.ThrowOnUnmappableCharDisable);
                default:
                    throw new InvalidOperationException("unreachable");
            }
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitPinvImpl(CILParser.PinvImplContext context) => VisitPinvImpl(context);
        public GrammarResult.Literal<(string? ModuleName, string? EntryPointName, MethodImportAttributes Attributes)> VisitPinvImpl(CILParser.PinvImplContext context)
        {
            MethodImportAttributes attrs = MethodImportAttributes.None;
            foreach (var attr in context.pinvAttr())
            {
                attrs |= VisitPinvAttr(attr);
            }
            var names = context.compQstring();
            string? moduleName = names.Length > 0 ? VisitCompQstring(names[0]).Value : null;
            string? entryPointName = names.Length > 1 ? VisitCompQstring(names[1]).Value : null;
            return new((moduleName, entryPointName, attrs));
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitPropAttr(CILParser.PropAttrContext context) => VisitPropAttr(context);
        public static GrammarResult.Flag<PropertyAttributes> VisitPropAttr(CILParser.PropAttrContext context)
        {
            return context.GetText() switch
            {
                "specialname" => new(PropertyAttributes.SpecialName),
                "rtspecialname" => new(0), // COMPAT: Ignore
                _ => throw new InvalidOperationException("unreachable"),
            };
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitPropDecl(CILParser.PropDeclContext context) => VisitPropDecl(context);
        public GrammarResult.Literal<(MethodSemanticsAttributes, EntityRegistry.EntityBase)?> VisitPropDecl(CILParser.PropDeclContext context)
        {
            if (context.ChildCount != 2)
            {
                return new(null);
            }
            string accessor = context.GetChild(0).GetText();
            EntityRegistry.EntityBase memberReference = VisitMethodRef(context.methodRef()).Value;
            MethodSemanticsAttributes methodSemanticsAttributes = accessor switch
            {
                ".set" => MethodSemanticsAttributes.Getter,
                ".get" => MethodSemanticsAttributes.Setter,
                ".other" => MethodSemanticsAttributes.Other,
                _ => throw new InvalidOperationException("unreachable"),
            };
            return new((methodSemanticsAttributes, memberReference));
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitPropDecls(CILParser.PropDeclsContext context) => VisitPropDecls(context);
        public GrammarResult.Sequence<(MethodSemanticsAttributes, EntityRegistry.EntityBase)> VisitPropDecls(CILParser.PropDeclsContext context)
            => new(
                context.propDecl()
                .Select(decl => VisitPropDecl(decl).Value)
                .Where(decl => decl is not null)
                .Select(decl => decl!.Value).ToImmutableArray());

        GrammarResult ICILVisitor<GrammarResult>.VisitPropHead(ILAssembler.CILParser.PropHeadContext context) => VisitPropHead(context);
        public GrammarResult.Literal<EntityRegistry.PropertyEntity> VisitPropHead(CILParser.PropHeadContext context)
        {
            var propAttrs = context.propAttr().Select(VisitPropAttr).Aggregate((PropertyAttributes)0, (a, b) => a | b);
            var name = VisitDottedName(context.dottedName()).Value;

            var signature = new BlobBuilder();
            byte callConv = (byte)(VisitCallConv(context.callConv()).Value | (byte)SignatureKind.Property);
            signature.WriteByte(callConv);
            var args = VisitSigArgs(context.sigArgs()).Value;
            signature.WriteCompressedInteger(args.Length);
            VisitType(context.type()).Value.WriteContentTo(signature);
            foreach (var arg in args)
            {
                arg.SignatureBlob.WriteContentTo(signature);
            }

            // TODO: Handle initOpt
            _ = VisitInitOpt(context.initOpt());
            return new(new(propAttrs, signature, name));
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitRepeatOpt(CILParser.RepeatOptContext context) => VisitRepeatOpt(context);
        public GrammarResult.Literal<int?> VisitRepeatOpt(CILParser.RepeatOptContext context) => context.int32() is {} int32 ? new(VisitInt32(int32).Value) : new(null);

        public GrammarResult VisitScopeBlock(CILParser.ScopeBlockContext context)
        {
            int numLocalsScopes = _currentMethod!.LocalsScopes.Count;
            _ = VisitMethodDecls(context.methodDecls());
            _currentMethod.LocalsScopes.RemoveRange(numLocalsScopes, _currentMethod.LocalsScopes.Count - numLocalsScopes);
            return GrammarResult.SentinelValue.Result;
        }

        private static class DeclarativeSecurityActionEx
        {
            public const DeclarativeSecurityAction Request = (DeclarativeSecurityAction)1;
            public const DeclarativeSecurityAction PrejitGrant = (DeclarativeSecurityAction)0xB;
            public const DeclarativeSecurityAction PrejitDeny = (DeclarativeSecurityAction)0xC;
            public const DeclarativeSecurityAction NonCasDemand = (DeclarativeSecurityAction)0xD;
            public const DeclarativeSecurityAction NonCasLinkDemand = (DeclarativeSecurityAction)0xE;
            public const DeclarativeSecurityAction NonCasInheritanceDemand = (DeclarativeSecurityAction)0xF;
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitSecAction(CILParser.SecActionContext context) => VisitSecAction(context);
        public static GrammarResult.Literal<DeclarativeSecurityAction> VisitSecAction(CILParser.SecActionContext context)
        {
            return context.GetText() switch
            {
                "request" => new(DeclarativeSecurityActionEx.Request),
                "demand" => new(DeclarativeSecurityAction.Demand),
                "assert" => new(DeclarativeSecurityAction.Assert),
                "deny" => new(DeclarativeSecurityAction.Deny),
                "permitonly" => new(DeclarativeSecurityAction.PermitOnly),
                "linkcheck" => new(DeclarativeSecurityAction.LinkDemand),
                "inheritcheck" => new(DeclarativeSecurityAction.InheritanceDemand),
                "reqmin" => new(DeclarativeSecurityAction.RequestMinimum),
                "reqopt" => new(DeclarativeSecurityAction.RequestOptional),
                "reqrefuse" => new(DeclarativeSecurityAction.RequestRefuse),
                "prejitgrant" => new(DeclarativeSecurityActionEx.PrejitGrant),
                "prejitdeny" => new(DeclarativeSecurityActionEx.PrejitDeny),
                "noncasdemand" => new(DeclarativeSecurityActionEx.NonCasDemand),
                "noncaslinkdemand" => new(DeclarativeSecurityActionEx.NonCasLinkDemand),
                "noncasinheritance" => new(DeclarativeSecurityActionEx.NonCasInheritanceDemand),
                _ => throw new InvalidOperationException("unreachable")
            };
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitSecAttrBlob(CILParser.SecAttrBlobContext context) => VisitSecAttrBlob(context);
        public GrammarResult.FormattedBlob VisitSecAttrBlob(CILParser.SecAttrBlobContext context)
        {
            var blob = new BlobBuilder();

            string attributeName = string.Empty;

            if (context.typeSpec() is CILParser.TypeSpecContext typeSpec && VisitTypeSpec(typeSpec).Value is EntityRegistry.IHasReflectionNotation reflectionNotation)
            {
                attributeName = reflectionNotation.ReflectionNotation;
            }
            else if (context.SQSTRING() is { } sqstring)
            {
                attributeName = sqstring.GetText();
            }

            blob.WriteSerializedString(attributeName);
            VisitCustomBlobNVPairs(context.customBlobNVPairs()).Value.WriteContentTo(blob);

            return new(blob);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitSecAttrSetBlob(CILParser.SecAttrSetBlobContext context) => VisitSecAttrSetBlob(context);
        public GrammarResult.FormattedBlob VisitSecAttrSetBlob(CILParser.SecAttrSetBlobContext context)
        {
            BlobBuilder blob = new();
            var secAttributes = context.secAttrBlob();
            blob.WriteByte((byte)'.');
            blob.WriteCompressedInteger(secAttributes.Length);
            foreach (var secAttribute in secAttributes)
            {
                VisitSecAttrBlob(secAttribute).Value.WriteContentTo(blob);
            }
            return new(blob);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitSecDecl(CILParser.SecDeclContext context) => VisitSecDecl(context);
        public GrammarResult.Literal<EntityRegistry.DeclarativeSecurityAttributeEntity?> VisitSecDecl(CILParser.SecDeclContext context)
        {
            if (context.PERMISSION() is not null)
            {
                // TODO: Report unsupported error
                // Cannot convert individual SecurityAttribute-based permissions to a PermissionSet without a runtime.
                return new(null);
            }
            DeclarativeSecurityAction action = VisitSecAction(context.secAction()).Value;
            BlobBuilder value;
            if (context.secAttrSetBlob() is CILParser.SecAttrSetBlobContext setBlob)
            {
                value = VisitSecAttrSetBlob(setBlob).Value;
            }
            else if (context.bytes() is CILParser.BytesContext bytes)
            {
                value = new();
                value.WriteBytes(VisitBytes(bytes).Value);
            }
            else if (context.compQstring() is CILParser.CompQstringContext str)
            {
                value = new BlobBuilder();
                value.WriteUTF16(VisitCompQstring(str).Value);
                value.WriteUTF16("\0");
            }
            else
            {
                throw new InvalidOperationException("unreachable");
            }
            return new(_entityRegistry.CreateDeclarativeSecurityAttribute(action, value));
        }

        internal abstract record ExceptionClause(LabelHandle Start, LabelHandle End)
        {
            internal sealed record Catch(EntityRegistry.TypeEntity Type, LabelHandle Start, LabelHandle End) : ExceptionClause(Start, End);

            internal sealed record Filter(LabelHandle FilterStart, LabelHandle Start, LabelHandle End) : ExceptionClause(Start, End);

            internal sealed record Finally(LabelHandle Start, LabelHandle End) : ExceptionClause(Start, End);

            internal sealed record Fault(LabelHandle Start, LabelHandle End) : ExceptionClause(Start, End);
        }

        public GrammarResult VisitSehBlock(CILParser.SehBlockContext context)
        {
            var (tryStart, tryEnd) = VisitTryBlock(context.tryBlock()).Value;
            foreach (var clause in VisitSehClauses(context.sehClauses()).Value)
            {
                switch (clause)
                {
                    case ExceptionClause.Finally finallyClause:
                        _currentMethod!.Definition.MethodBody.ControlFlowBuilder!.AddFinallyRegion(tryStart, tryEnd, finallyClause.Start, finallyClause.End);
                        break;
                    case ExceptionClause.Fault faultClause:
                        _currentMethod!.Definition.MethodBody.ControlFlowBuilder!.AddFaultRegion(tryStart, tryEnd, faultClause.Start, faultClause.End);
                        break;
                    case ExceptionClause.Catch catchClause:
                        _currentMethod!.Definition.MethodBody.ControlFlowBuilder!.AddCatchRegion(tryStart, tryEnd, catchClause.Start, catchClause.End, catchClause.Type.Handle);
                        break;
                    case ExceptionClause.Filter filterClause:
                        _currentMethod!.Definition.MethodBody.ControlFlowBuilder!.AddFilterRegion(tryStart, tryEnd, filterClause.Start, filterClause.End, filterClause.FilterStart);
                        break;
                    default:
                        throw new InvalidOperationException("unreachable");
                }
            }
            return GrammarResult.SentinelValue.Result;
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitSehClause(CILParser.SehClauseContext context) => VisitSehClause(context);
        public GrammarResult.Literal<ExceptionClause> VisitSehClause(CILParser.SehClauseContext context)
        {
            var (start, end) = VisitHandlerBlock(context.handlerBlock()).Value;

            if (context.finallyClause() is not null)
            {
                return new(new ExceptionClause.Finally(start, end));
            }
            if (context.faultClause() is not null)
            {
                return new(new ExceptionClause.Fault(start, end));
            }
            if (context.catchClause() is CILParser.CatchClauseContext catchClause)
            {
                return new(new ExceptionClause.Catch(VisitCatchClause(catchClause).Value, start, end));
            }
            if (context.filterClause() is CILParser.FilterClauseContext filterClause)
            {
                return new(new ExceptionClause.Filter(VisitFilterClause(filterClause).Value, start, end));
            }

            throw new InvalidOperationException("unreachable");
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitSehClauses(CILParser.SehClausesContext context) => VisitSehClauses(context);
        public GrammarResult.Sequence<ExceptionClause> VisitSehClauses(CILParser.SehClausesContext context) => new(context.sehClause().Select(clause => VisitSehClause(clause).Value).ToImmutableArray());

        GrammarResult ICILVisitor<GrammarResult>.VisitSerializType(CILParser.SerializTypeContext context) => VisitSerializType(context);
        public GrammarResult.FormattedBlob VisitSerializType(CILParser.SerializTypeContext context)
        {
            var blob = new BlobBuilder();
            if(context.ARRAY_TYPE_NO_BOUNDS() is not null)
            {
                blob.WriteByte((byte)SerializationTypeCode.SZArray);
            }
            VisitSerializTypeElement(context.serializTypeElement()).Value.WriteContentTo(blob);
            return new(blob);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitSerializTypeElement(CILParser.SerializTypeElementContext context) => VisitSerializTypeElement(context);
        public GrammarResult.FormattedBlob VisitSerializTypeElement(CILParser.SerializTypeElementContext context)
        {
            if (context.simpleType() is CILParser.SimpleTypeContext simpleType)
            {
                BlobBuilder blob = new(1);
                blob.WriteByte((byte)VisitSimpleType(simpleType).Value);
                return new(blob);
            }
            if (context.dottedName() is CILParser.DottedNameContext)
            {
                // TODO: typedef
                throw new NotImplementedException();
            }
            if (context.TYPE() is not null)
            {
                BlobBuilder blob = new BlobBuilder(1);
                blob.WriteByte((byte)SerializationTypeCode.Type);
                return new(blob);
            }
            if (context.OBJECT() is not null)
            {
                BlobBuilder blob = new BlobBuilder(1);
                blob.WriteByte((byte)SerializationTypeCode.TaggedObject);
                return new(blob);
            }
            if (context.ENUM() is not null)
            {
                BlobBuilder blob = new BlobBuilder();
                blob.WriteByte((byte)SerializationTypeCode.Enum);
                if (context.SQSTRING() is ITerminalNode sqString)
                {
                    blob.WriteSerializedString(sqString.GetText());
                }
                else
                {
                    Debug.Assert(context.className() is not null);
                    blob.WriteSerializedString((VisitClassName(context.className()).Value as EntityRegistry.IHasReflectionNotation)?.ReflectionNotation ?? "");
                }
                return new(blob);
            }
            throw new InvalidOperationException("unreachable");
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitSerInit(CILParser.SerInitContext context) => VisitSerInit(context);
        public GrammarResult.FormattedBlob VisitSerInit(CILParser.SerInitContext context)
        {
            if (context.fieldSerInit() is CILParser.FieldSerInitContext fieldSerInit)
            {
                return VisitFieldSerInit(fieldSerInit);
            }

            if (context.serInit() is CILParser.SerInitContext serInit)
            {
                Debug.Assert(context.OBJECT() is not null);
                BlobBuilder taggedObjectBlob = new(1);
                taggedObjectBlob.WriteByte((byte)SerializationTypeCode.TaggedObject);
                taggedObjectBlob.LinkSuffix(VisitSerInit(serInit).Value);
                return new(taggedObjectBlob);
            }

            if (context.int32() is not CILParser.Int32Context arrLength)
            {
                // The only cases where there is no int32 node is when the value is a string or type.
                BlobBuilder blob = new();
                blob.WriteByte((byte)GetTypeCodeForToken(((ITerminalNode)context.GetChild(0)).Symbol.Type));
                if (context.className() is CILParser.ClassNameContext className)
                {
                    blob.WriteSerializedString(VisitClassName(className).Value is EntityRegistry.IHasReflectionNotation reflection ? reflection.ReflectionNotation : string.Empty);
                }
                else
                {
                    blob.WriteSerializedString(context.SQSTRING()?.Symbol.Text);
                }
                return new(blob);
            }

            int tokenType = ((ITerminalNode)context.GetChild(0)).Symbol.Type;

            // 1 byte for ELEMENT_TYPE_SZARRAY, 1 byte for the array element type, 4 bytes for the length.
            BlobBuilder arrayHeader = new(6);
            arrayHeader.WriteByte((byte)SerializationTypeCode.SZArray);
            arrayHeader.WriteByte((byte)GetTypeCodeForToken(tokenType));
            arrayHeader.WriteInt32(VisitInt32(arrLength).Value);
            var sequenceResult = (GrammarResult.FormattedBlob)Visit(context.GetRuleContext<ParserRuleContext>(0));
            arrayHeader.LinkSuffix(sequenceResult.Value);
            return new(arrayHeader);
        }

        private static SerializationTypeCode GetTypeCodeForToken(int tokenType)
        {
            return tokenType switch
            {
                CILParser.INT8 => SerializationTypeCode.SByte,
                CILParser.UINT8 => SerializationTypeCode.Byte,
                CILParser.INT16 => SerializationTypeCode.Int16,
                CILParser.UINT16 => SerializationTypeCode.UInt16,
                CILParser.INT32_ => SerializationTypeCode.Int32,
                CILParser.UINT32 => SerializationTypeCode.UInt32,
                CILParser.INT64_ => SerializationTypeCode.Int64,
                CILParser.UINT64 => SerializationTypeCode.UInt64,
                CILParser.FLOAT32 => SerializationTypeCode.Single,
                CILParser.FLOAT64_ => SerializationTypeCode.Double,
                CILParser.CHAR => SerializationTypeCode.Char,
                CILParser.BOOL => SerializationTypeCode.Boolean,
                CILParser.STRING => SerializationTypeCode.String,
                CILParser.TYPE => SerializationTypeCode.Type,
                CILParser.OBJECT => SerializationTypeCode.TaggedObject,
                _ => throw new InvalidOperationException("unreachable")
            };
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitSigArg(CILParser.SigArgContext context) => VisitSigArg(context);
        public GrammarResult.Literal<SignatureArg> VisitSigArg(CILParser.SigArgContext context)
        {
            if (context.ELLIPSIS() is not null)
            {
                return new(SignatureArg.CreateSentinelArgument());
            }
            return new(new SignatureArg(
                VisitParamAttr(context.paramAttr()).Value,
                VisitType(context.type()).Value,
                VisitMarshalClause(context.marshalClause()).Value,
                context.id() is CILParser.IdContext id ? VisitId(id).Value : null));
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitSigArgs(CILParser.SigArgsContext context) => VisitSigArgs(context);
        public GrammarResult.Sequence<SignatureArg> VisitSigArgs(CILParser.SigArgsContext context) => new(ImmutableArray.CreateRange(context.sigArg().Select(arg => VisitSigArg(arg).Value)));
        GrammarResult ICILVisitor<GrammarResult>.VisitSimpleType(CILParser.SimpleTypeContext context) => VisitSimpleType(context);
        public GrammarResult.Literal<SignatureTypeCode> VisitSimpleType(CILParser.SimpleTypeContext context)
        {
            return new(context.GetChild<ITerminalNode>(0).Symbol.Type switch
            {
                CILParser.CHAR => SignatureTypeCode.Char,
                CILParser.STRING => SignatureTypeCode.String,
                CILParser.BOOL => SignatureTypeCode.Boolean,
                CILParser.INT8 => SignatureTypeCode.SByte,
                CILParser.INT16 => SignatureTypeCode.Int16,
                CILParser.INT32_ => SignatureTypeCode.Int32,
                CILParser.INT64_ => SignatureTypeCode.Int64,
                CILParser.UINT8 => SignatureTypeCode.Byte,
                CILParser.UINT16 => SignatureTypeCode.UInt16,
                CILParser.UINT32 => SignatureTypeCode.UInt32,
                CILParser.UINT64 => SignatureTypeCode.UInt64,
                _ => throw new InvalidOperationException("unreachable")
            });
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitSlashedName(CILParser.SlashedNameContext context)
        {
            return VisitSlashedName(context);
        }

        public static GrammarResult.Literal<TypeName> VisitSlashedName(CILParser.SlashedNameContext context)
        {
            TypeName? currentTypeName = null;
            foreach (var item in context.dottedName())
            {
                currentTypeName = new TypeName(currentTypeName, VisitDottedName(item).Value);
            }
            // We'll always have at least one dottedName, so the value here will be non-null
            return new(currentTypeName!);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitSqstringSeq(CILParser.SqstringSeqContext context) => VisitSqstringSeq(context);

        public static GrammarResult.FormattedBlob VisitSqstringSeq(CILParser.SqstringSeqContext context)
        {
            var strings = ImmutableArray.CreateBuilder<string?>(context.ChildCount);
            foreach (var child in context.children)
            {
                string? str = null;

                if (child is ITerminalNode { Symbol: { Type: CILParser.SQSTRING, Text: string stringValue } })
                {
                    str = stringValue;
                }

                strings.Add(str);
            }
            return new(strings.ToImmutable().SerializeSequence());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitStackreserve(CILParser.StackreserveContext context) => VisitStackreserve(context);
        public GrammarResult.Literal<long> VisitStackreserve(CILParser.StackreserveContext context) => VisitInt64(context.int64());

        GrammarResult ICILVisitor<GrammarResult>.VisitSubsystem(CILParser.SubsystemContext context) => VisitSubsystem(context);
        public GrammarResult.Literal<int> VisitSubsystem(CILParser.SubsystemContext context) => VisitInt32(context.int32());

        public GrammarResult VisitTerminal(ITerminalNode node) => throw new InvalidOperationException("unreachable");
        public GrammarResult VisitTls(CILParser.TlsContext context)
        {
            // TODO-SRM: System.Reflection.Metadata doesn't provide APIs to point a data declaration at a TLS slot or into the IL stream.
            // We have tests for the TLS case (CoreCLR only supports it on Win-x86), but not for the IL case.
            return GrammarResult.SentinelValue.Result;
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitTruefalse(CILParser.TruefalseContext context) => VisitTruefalse(context);

        public static GrammarResult.Literal<bool> VisitTruefalse(CILParser.TruefalseContext context)
        {
            return new(bool.Parse(context.GetText()));
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitTryBlock(CILParser.TryBlockContext context) => VisitTryBlock(context);

        public GrammarResult.Literal<(LabelHandle Start, LabelHandle End)> VisitTryBlock(CILParser.TryBlockContext context)
        {
            if (context.scopeBlock() is CILParser.ScopeBlockContext scopeBlock)
            {
                LabelHandle start = _currentMethod!.Definition.MethodBody.DefineLabel();
                _currentMethod.Definition.MethodBody.MarkLabel(start);
                _ = VisitScopeBlock(scopeBlock);
                LabelHandle end = _currentMethod.Definition.MethodBody.DefineLabel();
                _currentMethod.Definition.MethodBody.MarkLabel(end);
                return new((start, end));
            }
            if (context.id() is CILParser.IdContext[] ids)
            {
                var start = _currentMethod!.Labels.TryGetValue(VisitId(ids[0]).Value, out LabelHandle startLabel) ? startLabel : _currentMethod.Labels[VisitId(ids[0]).Value] = _currentMethod.Definition.MethodBody.DefineLabel();
                var end = _currentMethod!.Labels.TryGetValue(VisitId(ids[1]).Value, out LabelHandle endLabel) ? endLabel : _currentMethod.Labels[VisitId(ids[1]).Value] = _currentMethod.Definition.MethodBody.DefineLabel();
                return new((start, end));
            }
            if (context.int32() is CILParser.Int32Context[] offsets)
            {
                var start = _currentMethod!.Definition.MethodBody.DefineLabel();
                var end = _currentMethod.Definition.MethodBody.DefineLabel();
                _currentMethod.Definition.MethodBody.MarkLabel(start, VisitInt32(offsets[0]).Value);
                _currentMethod.Definition.MethodBody.MarkLabel(end, VisitInt32(offsets[1]).Value);
                return new((start, end));
            }
            throw new InvalidOperationException("unreachable");
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitTyBound(CILParser.TyBoundContext context) => VisitTyBound(context);
        public GrammarResult.Sequence<EntityRegistry.GenericParameterConstraintEntity> VisitTyBound(CILParser.TyBoundContext context)
        {
            return new(VisitTypeList(context.typeList()).Value.Select(EntityRegistry.CreateGenericConstraint).ToImmutableArray());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitTypar(CILParser.TyparContext context) => VisitTypar(context);

        public GrammarResult.Literal<EntityRegistry.GenericParameterEntity> VisitTypar(CILParser.TyparContext context)
        {
            GenericParameterAttributes attributes = VisitTyparAttribs(context.typarAttribs()).Value;
            EntityRegistry.GenericParameterEntity genericParameter = EntityRegistry.CreateGenericParameter(attributes, VisitDottedName(context.dottedName()).Value);

            foreach (var constraint in VisitTyBound(context.tyBound()).Value)
            {
                genericParameter.Constraints.Add(constraint);
            }

            return new(genericParameter);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitTyparAttrib(CILParser.TyparAttribContext context) => VisitTyparAttrib(context);
        public GrammarResult.Flag<GenericParameterAttributes> VisitTyparAttrib(CILParser.TyparAttribContext context)
        {
            return context switch
            {
                { covariant: not null } => new(GenericParameterAttributes.Covariant),
                { contravariant: not null } => new(GenericParameterAttributes.Contravariant),
                { @class: not null } => new(GenericParameterAttributes.ReferenceTypeConstraint),
                { valuetype: not null } => new(GenericParameterAttributes.NotNullableValueTypeConstraint),
                { byrefLike: not null } => new((GenericParameterAttributes)0x0020),
                { ctor: not null } => new(GenericParameterAttributes.DefaultConstructorConstraint),
                { flags: CILParser.Int32Context int32 } => new((GenericParameterAttributes)VisitInt32(int32).Value),
                _ => throw new InvalidOperationException("unreachable")
            };
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitTyparAttribs(CILParser.TyparAttribsContext context) => VisitTyparAttribs(context);

        public GrammarResult.Literal<GenericParameterAttributes> VisitTyparAttribs(CILParser.TyparAttribsContext context) =>
            new(context.typarAttrib()
                .Select(VisitTyparAttrib)
                .Aggregate(
                    (GenericParameterAttributes)0, (agg, attr) => agg | attr));

        GrammarResult ICILVisitor<GrammarResult>.VisitTypars(CILParser.TyparsContext context) => VisitTypars(context);
        public GrammarResult.Sequence<EntityRegistry.GenericParameterEntity> VisitTypars(CILParser.TyparsContext context)
        {
            CILParser.TyparContext[] typeParameters = context.typar();
            ImmutableArray<EntityRegistry.GenericParameterEntity>.Builder builder = ImmutableArray.CreateBuilder<EntityRegistry.GenericParameterEntity>(typeParameters.Length);

            foreach (var typeParameter in typeParameters)
            {
                builder.Add(VisitTypar(typeParameter).Value);
            }
            return new(builder.MoveToImmutable());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitTyparsClause(CILParser.TyparsClauseContext context) => VisitTyparsClause(context);
        public GrammarResult.Sequence<EntityRegistry.GenericParameterEntity> VisitTyparsClause(CILParser.TyparsClauseContext context) => context.typars() is null ? new(ImmutableArray<EntityRegistry.GenericParameterEntity>.Empty) : VisitTypars(context.typars());

        GrammarResult ICILVisitor<GrammarResult>.VisitType(CILParser.TypeContext context) => VisitType(context);
        public GrammarResult.FormattedBlob VisitType(CILParser.TypeContext context)
        {
            // These blobs will likely be very small, so use a smaller default size.
            const int DefaultSignatureElementBlobSize = 10;
            BlobBuilder prefix = new(DefaultSignatureElementBlobSize);
            BlobBuilder suffix = new(DefaultSignatureElementBlobSize);
            BlobBuilder elementType = VisitElementType(context.elementType()).Value;

            // Prefix blob writes outer modifiers first.
            // Suffix blob writes inner modifiers first.
            // Since all blobs are prefix blobs and only some have suffix data,
            // We will go in reverse order to write the prefixes
            // and then go in forward order to write the suffixes.
            CILParser.TypeModifiersContext[] typeModifiers = context.typeModifiers();
            for (int i = typeModifiers.Length - 1; i >= 0; i--)
            {
                CILParser.TypeModifiersContext? modifier = typeModifiers[i];
                switch (modifier)
                {
                    case CILParser.SZArrayModifierContext:
                        prefix.WriteByte((byte)SignatureTypeCode.SZArray);
                        break;
                    case CILParser.ArrayModifierContext:
                        prefix.WriteByte((byte)SignatureTypeCode.Array);
                        break;
                    case CILParser.ByRefModifierContext:
                        prefix.WriteByte((byte)SignatureTypeCode.ByReference);
                        break;
                    case CILParser.PtrModifierContext:
                        prefix.WriteByte((byte)SignatureTypeCode.Pointer);
                        break;
                    case CILParser.PinnedModifierContext:
                        prefix.WriteByte((byte)SignatureTypeCode.Pinned);
                        break;
                    case CILParser.RequiredModifierContext modreq:
                        prefix.WriteByte((byte)SignatureTypeCode.RequiredModifier);
                        prefix.WriteTypeEntity(VisitTypeSpec(modreq.typeSpec()).Value);
                        break;
                    case CILParser.OptionalModifierContext modopt:
                        prefix.WriteByte((byte)SignatureTypeCode.OptionalModifier);
                        prefix.WriteTypeEntity(VisitTypeSpec(modopt.typeSpec()).Value);
                        break;
                    case CILParser.GenericArgumentsModifierContext:
                        prefix.WriteByte((byte)SignatureTypeCode.GenericTypeInstance);
                        break;
                }
            }

            foreach (var modifier in typeModifiers)
            {
                switch (modifier)
                {
                    case CILParser.ArrayModifierContext arr:
                        var bounds = VisitBounds(arr.bounds()).Value;
                        suffix.WriteCompressedInteger(bounds.Length);
                        int lowerBoundsDefined = 0;
                        int upperBoundsDefined = 0;
                        foreach (var bound in bounds)
                        {
                            if (bound.Lower is not null)
                            {
                                lowerBoundsDefined++;
                            }
                            if (bound.Upper is not null)
                            {
                                upperBoundsDefined++;
                            }
                        }
                        suffix.WriteCompressedInteger(upperBoundsDefined);
                        foreach (var bound in bounds)
                        {
                            suffix.WriteCompressedInteger(bound.Upper.GetValueOrDefault());
                        }
                        suffix.WriteCompressedInteger(lowerBoundsDefined);
                        foreach (var bound in bounds)
                        {
                            suffix.WriteCompressedSignedInteger(bound.Lower.GetValueOrDefault());
                        }
                        break;
                    case CILParser.GenericArgumentsModifierContext genericArgs:
                        VisitTypeArgs(genericArgs.typeArgs()).Value.WriteContentTo(suffix);
                        break;
                }
            }

            elementType.LinkSuffix(suffix);
            prefix.LinkSuffix(elementType);
            return new(prefix);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitTypeArgs(CILParser.TypeArgsContext context) => VisitTypeArgs(context);

        public GrammarResult.FormattedBlob VisitTypeArgs(CILParser.TypeArgsContext context)
        {
            BlobBuilder blob = new(4);
            var types = context.type();
            blob.WriteCompressedInteger(types.Length);
            foreach (var type in types)
            {
                blob.LinkSuffix(VisitType(type).Value);
            }
            return new(blob);
        }

        public GrammarResult VisitTypedefDecl(CILParser.TypedefDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitTypelist(CILParser.TypelistContext context)
        {
            foreach (var name in context.className())
            {
                // We don't do anything with the class names here.
                // We just go through the name resolution process to ensure that the names are valid
                // and to provide TypeReference table rows.
                _ = VisitClassName(name);
            }
            return GrammarResult.SentinelValue.Result;
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitTypeList(CILParser.TypeListContext context) => VisitTypeList(context);
        public GrammarResult.Sequence<EntityRegistry.TypeEntity> VisitTypeList(CILParser.TypeListContext context)
        {
            CILParser.TypeSpecContext[] bounds = context.typeSpec();
            ImmutableArray<EntityRegistry.TypeEntity>.Builder builder = ImmutableArray.CreateBuilder<EntityRegistry.TypeEntity>(bounds.Length);
            foreach (var typeSpec in bounds)
            {
                builder.Add(VisitTypeSpec(typeSpec).Value);
            }
            return new(builder.MoveToImmutable());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitTypeSpec(CILParser.TypeSpecContext context) => VisitTypeSpec(context);
        public GrammarResult.Literal<EntityRegistry.TypeEntity> VisitTypeSpec(CILParser.TypeSpecContext context)
        {
            if (context.className() is CILParser.ClassNameContext className)
            {
                return new(VisitClassName(className).Value);
            }
            else if (context.dottedName() is CILParser.DottedNameContext dottedName)
            {
                string nameToResolve = VisitDottedName(dottedName).Value;
                if (context.MODULE() is not null)
                {
                    EntityRegistry.ModuleReferenceEntity? module = _entityRegistry.FindModuleReference(nameToResolve);
                    if (module is null)
                    {
                        // report error
                        return new(new EntityRegistry.FakeTypeEntity(MetadataTokens.ModuleReferenceHandle(0)));
                    }
                    return new(new EntityRegistry.FakeTypeEntity(module.Handle));
                }
                else
                {
                    return new(new EntityRegistry.FakeTypeEntity(
                        _entityRegistry.GetOrCreateAssemblyReference(nameToResolve, newRef =>
                        {
                            // Report warning on implicit assembly reference creation.
                        }).Handle));
                }
            }
            else
            {
                Debug.Assert(context.type() != null);
                return new(_entityRegistry.GetOrCreateTypeSpec(VisitType(context.type()).Value));
            }
        }


        GrammarResult ICILVisitor<GrammarResult>.VisitVariantType(CILParser.VariantTypeContext context) => VisitVariantType(context);
        public GrammarResult.Literal<VarEnum> VisitVariantType(CILParser.VariantTypeContext context)
        {
            VarEnum variant = VisitVariantTypeElement(context.variantTypeElement()).Value;
            // The 0th child is the variant element type.
            for (int i = 1; i < context.ChildCount; i++)
            {
                ITerminalNode childToken = (ITerminalNode)context.children[i];
                if (childToken.Symbol.Type == CILParser.ARRAY_TYPE_NO_BOUNDS)
                {
                    variant |= VarEnum.VT_ARRAY;
                }
                else if (childToken.Symbol.Type == CILParser.VECTOR)
                {
                    variant |= VarEnum.VT_VECTOR;
                }
                else
                {
                    Debug.Assert(childToken.Symbol.Type == CILParser.REF);
                    variant |= VarEnum.VT_BYREF;
                }
            }
            return new(variant);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitVariantTypeElement(CILParser.VariantTypeElementContext context) => VisitVariantTypeElement(context);
        public GrammarResult.Literal<VarEnum> VisitVariantTypeElement(CILParser.VariantTypeElementContext context)
        {
            return new(context.GetChild<ITerminalNode>(0).Symbol.Type switch
            {
                CILParser.VARIANT => VarEnum.VT_VARIANT,
                CILParser.CURRENCY => VarEnum.VT_CY,
                CILParser.VOID => VarEnum.VT_VOID,
                CILParser.BOOL => VarEnum.VT_BOOL,
                CILParser.INT8 => VarEnum.VT_I1,
                CILParser.INT16 => VarEnum.VT_I2,
                CILParser.INT32_ => VarEnum.VT_I4,
                CILParser.INT64_ => VarEnum.VT_I8,
                CILParser.FLOAT32 => VarEnum.VT_R4,
                CILParser.FLOAT64_ => VarEnum.VT_R8,
                CILParser.UINT8 => VarEnum.VT_UI1,
                CILParser.UINT16 => VarEnum.VT_UI2,
                CILParser.UINT32 => VarEnum.VT_UI4,
                CILParser.UINT64 => VarEnum.VT_UI8,
                CILParser.PTR => VarEnum.VT_PTR,
                CILParser.DECIMAL => VarEnum.VT_DECIMAL,
                CILParser.DATE => VarEnum.VT_DATE,
                CILParser.BSTR => VarEnum.VT_BSTR,
                CILParser.LPSTR => VarEnum.VT_LPSTR,
                CILParser.LPWSTR => VarEnum.VT_LPWSTR,
                CILParser.IUNKNOWN => VarEnum.VT_UNKNOWN,
                CILParser.IDISPATCH => VarEnum.VT_DISPATCH,
                CILParser.SAFEARRAY => VarEnum.VT_SAFEARRAY,
                CILParser.INT => VarEnum.VT_INT,
                CILParser.UINT => VarEnum.VT_UINT,
                CILParser.ERROR => VarEnum.VT_ERROR,
                CILParser.HRESULT => VarEnum.VT_HRESULT,
                CILParser.CARRAY => VarEnum.VT_CARRAY,
                CILParser.USERDEFINED => VarEnum.VT_USERDEFINED,
                CILParser.RECORD => VarEnum.VT_RECORD,
                CILParser.FILETIME => VarEnum.VT_FILETIME,
                CILParser.BLOB => VarEnum.VT_BLOB,
                CILParser.STREAM => VarEnum.VT_STREAM,
                CILParser.STORAGE => VarEnum.VT_STORAGE,
                CILParser.STREAMED_OBJECT => VarEnum.VT_STREAMED_OBJECT,
                CILParser.STORED_OBJECT => VarEnum.VT_STORED_OBJECT,
                CILParser.BLOB_OBJECT => VarEnum.VT_BLOB_OBJECT,
                CILParser.CF => VarEnum.VT_CF,
                CILParser.CLSID => VarEnum.VT_CLSID,
                _ => throw new InvalidOperationException("unreachable")
            });
        }

        public GrammarResult VisitVtableDecl(CILParser.VtableDeclContext context)
        {
            // TODO: Need custom ManagedPEBuilder subclass to write the exports directory.
            throw new NotImplementedException("raw vtable fixups blob not supported");
        }

        public GrammarResult VisitVtfixupAttr(CILParser.VtfixupAttrContext context)
        {
            // TODO: Need custom ManagedPEBuilder subclass to write the exports directory.
            throw new NotImplementedException("vtable fixups not supported");
        }
        public GrammarResult VisitVtfixupDecl(CILParser.VtfixupDeclContext context)
        {
            // TODO: Need custom ManagedPEBuilder subclass to write the exports directory.
            throw new NotImplementedException("raw vtable fixups blob not supported");
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitOptionalModifier(CILParser.OptionalModifierContext context) => throw new InvalidOperationException(NodeShouldNeverBeDirectlyVisited);
        GrammarResult ICILVisitor<GrammarResult>.VisitSZArrayModifier(CILParser.SZArrayModifierContext context) => throw new InvalidOperationException(NodeShouldNeverBeDirectlyVisited);
        GrammarResult ICILVisitor<GrammarResult>.VisitRequiredModifier(CILParser.RequiredModifierContext context) => throw new InvalidOperationException(NodeShouldNeverBeDirectlyVisited);
        GrammarResult ICILVisitor<GrammarResult>.VisitPtrModifier(CILParser.PtrModifierContext context) => throw new InvalidOperationException(NodeShouldNeverBeDirectlyVisited);
        GrammarResult ICILVisitor<GrammarResult>.VisitPinnedModifier(CILParser.PinnedModifierContext context) => throw new InvalidOperationException(NodeShouldNeverBeDirectlyVisited);
        GrammarResult ICILVisitor<GrammarResult>.VisitGenericArgumentsModifier(CILParser.GenericArgumentsModifierContext context) => throw new InvalidOperationException(NodeShouldNeverBeDirectlyVisited);
        GrammarResult ICILVisitor<GrammarResult>.VisitByRefModifier(CILParser.ByRefModifierContext context) => throw new InvalidOperationException(NodeShouldNeverBeDirectlyVisited);
        GrammarResult ICILVisitor<GrammarResult>.VisitArrayModifier(CILParser.ArrayModifierContext context) => throw new InvalidOperationException(NodeShouldNeverBeDirectlyVisited);
        GrammarResult ICILVisitor<GrammarResult>.VisitTypeModifiers(CILParser.TypeModifiersContext context) => throw new InvalidOperationException(NodeShouldNeverBeDirectlyVisited);
        public GrammarResult VisitNativeTypeArrayPointerInfo(CILParser.NativeTypeArrayPointerInfoContext context) => throw new InvalidOperationException(NodeShouldNeverBeDirectlyVisited);
        public GrammarResult VisitPointerArrayTypeSize(CILParser.PointerArrayTypeSizeContext context) => throw new InvalidOperationException(NodeShouldNeverBeDirectlyVisited);
        public GrammarResult VisitPointerArrayTypeParamIndex(CILParser.PointerArrayTypeParamIndexContext context) => throw new InvalidOperationException(NodeShouldNeverBeDirectlyVisited);
        public GrammarResult VisitPointerNativeType(CILParser.PointerNativeTypeContext context) => throw new InvalidOperationException(NodeShouldNeverBeDirectlyVisited);
        public GrammarResult VisitPointerArrayTypeSizeParamIndex(CILParser.PointerArrayTypeSizeParamIndexContext context) => throw new InvalidOperationException(NodeShouldNeverBeDirectlyVisited);
        public GrammarResult VisitPointerArrayTypeNoSizeData(CILParser.PointerArrayTypeNoSizeDataContext context) => throw new InvalidOperationException(NodeShouldNeverBeDirectlyVisited);
    }
}
