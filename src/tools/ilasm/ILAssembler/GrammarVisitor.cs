// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
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

    internal sealed class GrammarVisitor : ICILVisitor<GrammarResult>
    {
        private const string NodeShouldNeverBeDirectlyVisited = "This node should never be directly visited. It should be directly processed by its parent node.";
        private readonly ImmutableArray<Diagnostic>.Builder _diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        private readonly EntityRegistry _entityRegistry = new();
        private readonly IReadOnlyDictionary<string, SourceText> _documents;
        private readonly Options _options;

        public GrammarVisitor(IReadOnlyDictionary<string, SourceText> documents, Options options)
        {
            _documents = documents;
            _options = options;
        }

        public GrammarResult Visit(IParseTree tree) => tree.Accept(this);
        public GrammarResult VisitAlignment(CILParser.AlignmentContext context) => throw new NotImplementedException();
        public GrammarResult VisitAsmAttr(CILParser.AsmAttrContext context) => throw new NotImplementedException();
        public GrammarResult VisitAsmAttrAny(CILParser.AsmAttrAnyContext context) => throw new NotImplementedException();
        public GrammarResult VisitAsmOrRefDecl(CILParser.AsmOrRefDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitAssemblyBlock(CILParser.AssemblyBlockContext context) => throw new NotImplementedException();
        public GrammarResult VisitAssemblyDecl(CILParser.AssemblyDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitAssemblyDecls(CILParser.AssemblyDeclsContext context) => throw new NotImplementedException();
        public GrammarResult VisitAssemblyRefDecl(CILParser.AssemblyRefDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitAssemblyRefDecls(CILParser.AssemblyRefDeclsContext context) => throw new NotImplementedException();
        public GrammarResult VisitAssemblyRefHead(CILParser.AssemblyRefHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitAtOpt(CILParser.AtOptContext context) => throw new NotImplementedException();
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
#pragma warning disable CA1822 // Mark members as static
        public GrammarResult.Literal<SignatureCallingConvention> VisitCallKind(CILParser.CallKindContext context)
#pragma warning restore CA1822 // Mark members as static
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

        public GrammarResult VisitCatchClause(CILParser.CatchClauseContext context) => throw new NotImplementedException();
        public GrammarResult VisitCaValue(CILParser.CaValueContext context) => throw new NotImplementedException();
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

            public sealed record ExternalSource(string FileName, int StartLine, int StartColumn, int? EndLine, int? EndColumn);
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
                if (typeFullNameLastDot != -1)
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
                if (typeFullNameLastDot != -1)
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

                    // TODO: Handle interface implementation
                    if (context.implClause() is CILParser.ImplClauseContext impl)
                    {
                        _ = impl;
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
        public static GrammarResult.FormattedBlob VisitClassSeq(CILParser.ClassSeqContext context)
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

        public static GrammarResult.FormattedBlob VisitClassSeqElement(CILParser.ClassSeqElementContext context)
        {
            if (context.className() is CILParser.ClassNameContext className)
            {
                // TODO: convert className to a reflection-notation string.
                _ = className;
                throw new NotImplementedException();
            }

            BlobBuilder blob = new();
            blob.WriteSerializedString(context.SQSTRING()?.Symbol.Text);
            return new(blob);
        }
        public GrammarResult VisitCompControl(CILParser.CompControlContext context)
        {
            throw new NotImplementedException("Compilation control directives should be handled by a custom token stream.");
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
        public GrammarResult VisitCustomAttrDecl(CILParser.CustomAttrDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitCustomBlobArgs(CILParser.CustomBlobArgsContext context) => throw new NotImplementedException();
        public GrammarResult VisitCustomBlobDescr(CILParser.CustomBlobDescrContext context) => throw new NotImplementedException();
        public GrammarResult VisitCustomBlobNVPairs(CILParser.CustomBlobNVPairsContext context) => throw new NotImplementedException();
        public GrammarResult VisitCustomDescr(CILParser.CustomDescrContext context) => throw new NotImplementedException();
        public GrammarResult VisitCustomDescrWithOwner(CILParser.CustomDescrWithOwnerContext context) => throw new NotImplementedException();
        public GrammarResult VisitCustomType(CILParser.CustomTypeContext context) => throw new NotImplementedException();
        public GrammarResult VisitDataDecl(CILParser.DataDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitDdBody(CILParser.DdBodyContext context) => throw new NotImplementedException();
        public GrammarResult VisitDdHead(CILParser.DdHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitDdItem(CILParser.DdItemContext context) => throw new NotImplementedException();
        public GrammarResult VisitDdItemCount(CILParser.DdItemCountContext context) => throw new NotImplementedException();
        public GrammarResult VisitDdItemList(CILParser.DdItemListContext context) => throw new NotImplementedException();

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
                _currentMethod = VisitMethodHead(methodHead).Value;
                VisitMethodDecls(context.methodDecls());
                _currentMethod = null;
            }
            throw new NotImplementedException();
        }

        public GrammarResult VisitDecls(CILParser.DeclsContext context) => throw new NotImplementedException();

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
                        for (int i = 0; i < _currentMethod.GenericParameters.Count; i++)
                        {
                            EntityRegistry.GenericParameterEntity? genericParameter = _currentMethod.GenericParameters[i];
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

        public GrammarResult VisitErrorNode(IErrorNode node) => throw new NotImplementedException();
        public GrammarResult VisitEsHead(CILParser.EsHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitEventAttr(CILParser.EventAttrContext context) => throw new NotImplementedException();
        public GrammarResult VisitEventDecl(CILParser.EventDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitEventDecls(CILParser.EventDeclsContext context) => throw new NotImplementedException();
        public GrammarResult VisitEventHead(CILParser.EventHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitExportHead(CILParser.ExportHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitExptAttr(CILParser.ExptAttrContext context) => throw new NotImplementedException();
        public GrammarResult VisitExptypeDecl(CILParser.ExptypeDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitExptypeDecls(CILParser.ExptypeDeclsContext context) => throw new NotImplementedException();
        public GrammarResult VisitExptypeHead(CILParser.ExptypeHeadContext context) => throw new NotImplementedException();
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

        public GrammarResult VisitExtSourceSpec(CILParser.ExtSourceSpecContext context) => throw new NotImplementedException();
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
        public GrammarResult VisitFaultClause(CILParser.FaultClauseContext context) => throw new NotImplementedException();
        public GrammarResult VisitFieldAttr(CILParser.FieldAttrContext context) => throw new NotImplementedException();
        public GrammarResult VisitFieldDecl(CILParser.FieldDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitFieldInit(CILParser.FieldInitContext context) => throw new NotImplementedException();
        public GrammarResult VisitFieldOrProp(CILParser.FieldOrPropContext context) => throw new NotImplementedException();

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

        public GrammarResult VisitFileAttr(CILParser.FileAttrContext context) => throw new NotImplementedException();
        public GrammarResult VisitFileDecl(CILParser.FileDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitFileEntry(CILParser.FileEntryContext context) => throw new NotImplementedException();
        public GrammarResult VisitFilterClause(CILParser.FilterClauseContext context) => throw new NotImplementedException();
        public GrammarResult VisitFilterHead(CILParser.FilterHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitFinallyClause(CILParser.FinallyClauseContext context) => throw new NotImplementedException();

        GrammarResult ICILVisitor<GrammarResult>.VisitFloat64(CILParser.Float64Context context) => throw new NotImplementedException();
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
                return new(Unsafe.As<long, double>(ref value));
            }
            throw new InvalidOperationException("unreachable");
        }
        public GrammarResult VisitGenArity(CILParser.GenArityContext context) => throw new NotImplementedException();
        public GrammarResult VisitGenArityNotEmpty(CILParser.GenArityNotEmptyContext context) => throw new NotImplementedException();
        public GrammarResult VisitHandlerBlock(CILParser.HandlerBlockContext context) => throw new NotImplementedException();

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

        public GrammarResult VisitImplClause(CILParser.ImplClauseContext context) => throw new NotImplementedException();
        public GrammarResult VisitImplList(CILParser.ImplListContext context) => throw new NotImplementedException();
        public GrammarResult VisitInitOpt(CILParser.InitOptContext context) => throw new NotImplementedException();
#pragma warning disable CA1822 // Mark members as static
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
                    }
                    break;
                case CILParser.RULE_instr_field:
                    break;
                case CILParser.RULE_instr_i:
                    break;
                case CILParser.RULE_instr_i8:
                    break;
                case CILParser.RULE_instr_method:
                    break;
                case CILParser.RULE_instr_none:
                    break;
                case CILParser.RULE_instr_r:
                    break;
                case CILParser.RULE_instr_sig:
                    break;
                case CILParser.RULE_instr_string:
                    break;
                case CILParser.RULE_instr_switch:
                    break;
                case CILParser.RULE_instr_tok:
                    break;
                case CILParser.RULE_instr_type:
                    break;
                case CILParser.RULE_instr_var:
                    break;
            }
            throw new NotImplementedException();
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
#pragma warning restore CA1822 // Mark members as static
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

        public GrammarResult VisitIntOrWildcard(CILParser.IntOrWildcardContext context) => throw new NotImplementedException();
        public GrammarResult VisitLabels(CILParser.LabelsContext context) => throw new NotImplementedException();
        public GrammarResult VisitLanguageDecl(CILParser.LanguageDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitLocalsHead(CILParser.LocalsHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitManifestResDecl(CILParser.ManifestResDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitManifestResDecls(CILParser.ManifestResDeclsContext context) => throw new NotImplementedException();
        public GrammarResult VisitManifestResHead(CILParser.ManifestResHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitManresAttr(CILParser.ManresAttrContext context) => throw new NotImplementedException();

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

        public GrammarResult VisitMemberRef(CILParser.MemberRefContext context) => throw new NotImplementedException();
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
            else if (context.ZEROINIT() is not null)
            {
                currentMethod.Definition.BodyAttributes = MethodBodyAttributes.InitLocals;
            }
            else if (context.MAXSTACK() is not null)
            {
                currentMethod.Definition.MaxStack = VisitInt32(context.GetChild<CILParser.Int32Context>(0)).Value;
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
                sigHeader |= (byte)SignatureAttributes.Instance;
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
            foreach (var arg in args)
            {
                arg.SignatureBlob.WriteContentTo(methodSignature);
            }
            // We've parsed all signature information. We can reset the current method now (the caller will handle setting/unsetting it for the method body).
            _currentMethod = null;
            methodDefinition.MethodSignature = methodSignature;

            methodDefinition.ImplementationAttributes = context.implAttr().Aggregate((MethodImplAttributes)0, (acc, attr) => acc | VisitImplAttr(attr));
            if (!_entityRegistry.TryRecordMethodDefinition(methodDefinition))
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
        public GrammarResult VisitMethodRef(CILParser.MethodRefContext context) => throw new NotImplementedException();
        public GrammarResult VisitMethodSpec(CILParser.MethodSpecContext context) => throw new NotImplementedException();
        public GrammarResult VisitModuleHead(CILParser.ModuleHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitMscorlib(CILParser.MscorlibContext context) => throw new NotImplementedException();

        GrammarResult ICILVisitor<GrammarResult>.VisitNameSpaceHead(ILAssembler.CILParser.NameSpaceHeadContext context) => VisitNameSpaceHead(context);

        public static GrammarResult.String VisitNameSpaceHead(CILParser.NameSpaceHeadContext context) => VisitDottedName(context.dottedName());

        public GrammarResult VisitNameValPair(CILParser.NameValPairContext context) => throw new NotImplementedException();
        public GrammarResult VisitNameValPairs(CILParser.NameValPairsContext context) => throw new NotImplementedException();

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

        public GrammarResult VisitOwnerType(CILParser.OwnerTypeContext context) => throw new NotImplementedException();
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

        public GrammarResult VisitPropAttr(CILParser.PropAttrContext context) => throw new NotImplementedException();
        public GrammarResult VisitPropDecl(CILParser.PropDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitPropDecls(CILParser.PropDeclsContext context) => throw new NotImplementedException();
        public GrammarResult VisitPropHead(CILParser.PropHeadContext context) => throw new NotImplementedException();
        public GrammarResult VisitRepeatOpt(CILParser.RepeatOptContext context) => throw new NotImplementedException();
        public GrammarResult VisitScopeBlock(CILParser.ScopeBlockContext context) => throw new NotImplementedException();
        public GrammarResult VisitSecAction(CILParser.SecActionContext context) => throw new NotImplementedException();
        public GrammarResult VisitSecAttrBlob(CILParser.SecAttrBlobContext context) => throw new NotImplementedException();
        public GrammarResult VisitSecAttrSetBlob(CILParser.SecAttrSetBlobContext context) => throw new NotImplementedException();
        public GrammarResult VisitSecDecl(CILParser.SecDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitSehBlock(CILParser.SehBlockContext context) => throw new NotImplementedException();
        public GrammarResult VisitSehClause(CILParser.SehClauseContext context) => throw new NotImplementedException();
        public GrammarResult VisitSehClauses(CILParser.SehClausesContext context) => throw new NotImplementedException();
        public GrammarResult VisitSeralizType(CILParser.SeralizTypeContext context) => throw new NotImplementedException();
        public GrammarResult VisitSeralizTypeElement(CILParser.SeralizTypeElementContext context) => throw new NotImplementedException();
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
#pragma warning disable CA1822 // Mark members as static
        public GrammarResult.Literal<SignatureTypeCode> VisitSimpleType(CILParser.SimpleTypeContext context)
#pragma warning restore CA1822 // Mark members as static
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

        public GrammarResult VisitTerminal(ITerminalNode node) => throw new NotImplementedException();
        public GrammarResult VisitTls(CILParser.TlsContext context) => throw new NotImplementedException();

        GrammarResult ICILVisitor<GrammarResult>.VisitTruefalse(CILParser.TruefalseContext context) => VisitTruefalse(context);

        public static GrammarResult.Literal<bool> VisitTruefalse(CILParser.TruefalseContext context)
        {
            return new(bool.Parse(context.GetText()));
        }

        public GrammarResult VisitTryBlock(CILParser.TryBlockContext context) => throw new NotImplementedException();
        public GrammarResult VisitTryHead(CILParser.TryHeadContext context) => throw new NotImplementedException();
        GrammarResult ICILVisitor<GrammarResult>.VisitTyBound(CILParser.TyBoundContext context) => VisitTyBound(context);
        public GrammarResult.Sequence<EntityRegistry.GenericParameterConstraintEntity> VisitTyBound(CILParser.TyBoundContext context)
        {
            return new(VisitTypeList(context.typeList()).Value.Select(_entityRegistry.CreateGenericConstraint).ToImmutableArray());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitTypar(CILParser.TyparContext context) => VisitTypar(context);

        public GrammarResult.Literal<EntityRegistry.GenericParameterEntity> VisitTypar(CILParser.TyparContext context)
        {
            GenericParameterAttributes attributes = VisitTyparAttribs(context.typarAttribs()).Value;
            EntityRegistry.GenericParameterEntity genericParameter = _entityRegistry.CreateGenericParameter(attributes, VisitDottedName(context.dottedName()).Value);

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
        public GrammarResult VisitTypelist(CILParser.TypelistContext context) => throw new NotImplementedException();
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
#pragma warning disable CA1822 // Mark members as static
        public GrammarResult.Literal<VarEnum> VisitVariantTypeElement(CILParser.VariantTypeElementContext context)
#pragma warning restore CA1822 // Mark members as static
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

        public GrammarResult VisitVtableDecl(CILParser.VtableDeclContext context) => throw new NotImplementedException();
        public GrammarResult VisitVtfixupAttr(CILParser.VtfixupAttrContext context) => throw new NotImplementedException();
        public GrammarResult VisitVtfixupDecl(CILParser.VtfixupDeclContext context) => throw new NotImplementedException();

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
