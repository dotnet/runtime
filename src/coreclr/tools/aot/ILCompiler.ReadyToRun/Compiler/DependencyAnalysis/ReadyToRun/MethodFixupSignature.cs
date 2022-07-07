// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.JitInterface;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.CorConstants;
using Internal.ReadyToRunConstants;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class MethodFixupSignature : Signature
    {
        private readonly ReadyToRunFixupKind _fixupKind;

        private readonly MethodWithToken _method;

        public MethodFixupSignature(
            ReadyToRunFixupKind fixupKind, 
            MethodWithToken method,
            bool isInstantiatingStub)
        {
            _fixupKind = fixupKind;
            _method = method;
            IsInstantiatingStub = isInstantiatingStub;

            // Ensure types in signature are loadable and resolvable, otherwise we'll fail later while emitting the signature
            CompilerTypeSystemContext compilerContext = (CompilerTypeSystemContext)method.Method.Context;
            compilerContext.EnsureLoadableMethod(method.Method);
            compilerContext.EnsureLoadableType(_method.OwningType);

            if (method.ConstrainedType != null)
                compilerContext.EnsureLoadableType(method.ConstrainedType);
        }

        public MethodDesc Method => _method.Method;
        public bool IsInstantiatingStub { get; }

        public override int ClassCode => 150063499;

        public bool IsUnboxingStub => _method.Unboxing;

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList list = base.ComputeNonRelocationBasedDependencies(factory);
            if (_fixupKind == ReadyToRunFixupKind.VirtualEntry &&
                !Method.IsAbstract &&
                !Method.HasInstantiation &&
                Method.GetCanonMethodTarget(CanonicalFormKind.Specific) is var canonMethod &&
                !factory.CompilationModuleGroup.VersionsWithMethodBody(canonMethod) &&
                factory.CompilationModuleGroup.CrossModuleCompileable(canonMethod) &&
                factory.CompilationModuleGroup.ContainsMethodBody(canonMethod, false))
            {
                list = list ?? new DependencyAnalysisFramework.DependencyNodeCore<NodeFactory>.DependencyList();
                list.Add(factory.CompiledMethodNode(canonMethod), "Virtual function dependency on cross module inlineable method");
            }

            return list;
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            if (relocsOnly)
            {
                // Method fixup signature doesn't contain any direct relocs
                return new ObjectData(data: Array.Empty<byte>(), relocs: null, alignment: 0, definedSymbols: null);
            }

            ObjectDataSignatureBuilder dataBuilder = new ObjectDataSignatureBuilder();
            dataBuilder.AddSymbol(this);

            // Optimize some of the fixups into a more compact form
            ReadyToRunFixupKind fixupKind = _fixupKind;
            bool optimized = false;
            if (!_method.Unboxing && !IsInstantiatingStub && _method.ConstrainedType == null &&
                fixupKind == ReadyToRunFixupKind.MethodEntry)
            {
                if (!_method.Method.HasInstantiation && !_method.Method.OwningType.HasInstantiation && !_method.Method.OwningType.IsArray)
                {
                    if (_method.Token.TokenType == CorTokenType.mdtMethodDef)
                    {
                        fixupKind = ReadyToRunFixupKind.MethodEntry_DefToken;
                        optimized = true;
                    }
                    else if (_method.Token.TokenType == CorTokenType.mdtMemberRef)
                    {
                        if (!_method.OwningTypeNotDerivedFromToken)
                        {
                            fixupKind = ReadyToRunFixupKind.MethodEntry_RefToken;
                            optimized = true;
                        }
                    }
                }
            }

            MethodWithToken method = _method;
            
            if (factory.CompilationModuleGroup.VersionsWithMethodBody(method.Method))
            {
                if (method.Token.TokenType == CorTokenType.mdtMethodSpec)
                {
                    method = new MethodWithToken(method.Method, factory.SignatureContext.GetModuleTokenForMethod(method.Method), method.ConstrainedType, unboxing: _method.Unboxing, null);
                }
                else if (!optimized && (method.Token.TokenType == CorTokenType.mdtMemberRef))
                {
                    if (method.Method.OwningType.GetTypeDefinition() is EcmaType)
                    {
                        method = new MethodWithToken(method.Method, factory.SignatureContext.GetModuleTokenForMethod(method.Method), method.ConstrainedType, unboxing: _method.Unboxing, null);
                    }
                }
            }

            SignatureContext innerContext = dataBuilder.EmitFixup(factory, fixupKind, method.Token.Module, factory.SignatureContext);

            if (optimized && method.Token.TokenType == CorTokenType.mdtMethodDef)
            {
                dataBuilder.EmitMethodDefToken(method.Token);
            }
            else if (optimized && method.Token.TokenType == CorTokenType.mdtMemberRef)
            {
                dataBuilder.EmitMethodRefToken(method.Token);
            }
            else
            {
                dataBuilder.EmitMethodSignature(method, enforceDefEncoding: false, enforceOwningType: false, innerContext, IsInstantiatingStub);
            }

            return dataBuilder.ToObjectData();
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append($@"MethodFixupSignature(");
            sb.Append(_fixupKind.ToString());
            if (IsInstantiatingStub)
            {
                sb.Append(" [INST]");
            }
            sb.Append(": ");
            _method.AppendMangledName(nameMangler, sb);
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            MethodFixupSignature otherNode = (MethodFixupSignature)other;
            int result = ((int)_fixupKind).CompareTo((int)otherNode._fixupKind);
            if (result != 0)
                return result;

            result = IsInstantiatingStub.CompareTo(otherNode.IsInstantiatingStub);
            if (result != 0)
                return result;

            return _method.CompareTo(otherNode._method, comparer);
        }
    }
}
