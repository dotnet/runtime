// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

using Internal.JitInterface;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class GenericLookupSignature : Signature
    {
        private readonly CORINFO_RUNTIME_LOOKUP_KIND _runtimeLookupKind;

        private readonly ReadyToRunFixupKind _fixupKind;

        private readonly TypeDesc _typeArgument;

        private readonly MethodWithToken _methodArgument;

        private readonly FieldDesc _fieldArgument;

        private readonly GenericContext _methodContext;

        private readonly SignatureContext _signatureContext;

        public GenericLookupSignature(
            CORINFO_RUNTIME_LOOKUP_KIND runtimeLookupKind,
            ReadyToRunFixupKind fixupKind,
            TypeDesc typeArgument,
            MethodWithToken methodArgument,
            FieldDesc fieldArgument,
            GenericContext methodContext,
            SignatureContext signatureContext)
        {
            Debug.Assert(typeArgument != null || methodArgument != null || fieldArgument != null);
            _runtimeLookupKind = runtimeLookupKind;
            _fixupKind = fixupKind;
            _typeArgument = typeArgument;
            _methodArgument = methodArgument;
            _fieldArgument = fieldArgument;
            _methodContext = methodContext;
            _signatureContext = signatureContext;
        }

        public override int ClassCode => 258608008;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            if (relocsOnly)
            {
                return new ObjectData(Array.Empty<byte>(), null, 1, null);
            }

            ReadyToRunCodegenNodeFactory r2rFactory = (ReadyToRunCodegenNodeFactory)factory;

            // Determine the need for module override
            EcmaModule targetModule;
            if (_methodArgument != null)
            {
                targetModule = _methodArgument.Token.Module;
            }
            else if (_typeArgument != null)
            {
                targetModule = _signatureContext.GetTargetModule(_typeArgument);
            }
            else if (_fieldArgument != null)
            {
                targetModule = _signatureContext.GetTargetModule(_fieldArgument);
            }
            else
            {
                throw new NotImplementedException();
            }

            ReadyToRunFixupKind fixupToEmit;
            TypeDesc contextTypeToEmit = null;

            switch (_runtimeLookupKind)
            {
                case CORINFO_RUNTIME_LOOKUP_KIND.CORINFO_LOOKUP_CLASSPARAM:
                    fixupToEmit = ReadyToRunFixupKind.READYTORUN_FIXUP_TypeDictionaryLookup;
                    break;

                case CORINFO_RUNTIME_LOOKUP_KIND.CORINFO_LOOKUP_METHODPARAM:
                    fixupToEmit = ReadyToRunFixupKind.READYTORUN_FIXUP_MethodDictionaryLookup;
                    break;

                case CORINFO_RUNTIME_LOOKUP_KIND.CORINFO_LOOKUP_THISOBJ:
                    fixupToEmit = ReadyToRunFixupKind.READYTORUN_FIXUP_ThisObjDictionaryLookup;
                    contextTypeToEmit = _methodContext.ContextType;
                    break;

                default:
                    throw new NotImplementedException();
            }

            ObjectDataSignatureBuilder dataBuilder = new ObjectDataSignatureBuilder();
            dataBuilder.AddSymbol(this);

            SignatureContext innerContext = dataBuilder.EmitFixup(r2rFactory, fixupToEmit, targetModule, _signatureContext);
            if (contextTypeToEmit != null)
            {
                dataBuilder.EmitTypeSignature(contextTypeToEmit, innerContext);
            }

            dataBuilder.EmitByte((byte)_fixupKind);
            if (_methodArgument != null)
            {
                dataBuilder.EmitMethodSignature(
                    _methodArgument,
                    enforceDefEncoding: false,
                    enforceOwningType: false,
                    context: innerContext,
                    isUnboxingStub: false,
                    isInstantiatingStub: true);
            }
            else if (_typeArgument != null)
            {
                dataBuilder.EmitTypeSignature(_typeArgument, innerContext);
            }
            else if (_fieldArgument != null)
            {
                dataBuilder.EmitFieldSignature(_fieldArgument, innerContext);
            }
            else
            {
                throw new NotImplementedException();
            }

            return dataBuilder.ToObjectData();
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("GenericLookupSignature(");
            sb.Append(_runtimeLookupKind.ToString());
            sb.Append(" / ");
            sb.Append(_fixupKind.ToString());
            sb.Append(": ");
            if (_methodArgument != null)
            {
                sb.Append(nameMangler.GetMangledMethodName(_methodArgument.Method));
                if (!_methodArgument.Token.IsNull)
                {
                    sb.Append(" [");
                    sb.Append(_methodArgument.Token.MetadataReader.GetString(_methodArgument.Token.MetadataReader.GetAssemblyDefinition().Name));
                    sb.Append(":"); ;
                    sb.Append(((uint)_methodArgument.Token.Token).ToString("X8"));
                    sb.Append("]");
                }
            }
            if (_typeArgument != null)
            {
                sb.Append(nameMangler.GetMangledTypeName(_typeArgument));
            }
            if (_fieldArgument != null)
            {
                sb.Append(nameMangler.GetMangledFieldName(_fieldArgument));
            }
            sb.Append(" (");
            _methodContext.AppendMangledName(nameMangler, sb);
            sb.Append(")");
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            throw new NotImplementedException();
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencies = new DependencyList();
            if (_typeArgument != null && !_typeArgument.IsRuntimeDeterminedSubtype)
            {
                dependencies.Add(factory.NecessaryTypeSymbol(_typeArgument), "Type referenced in a generic lookup signature");
            }
            return dependencies;
        }
    }
}
