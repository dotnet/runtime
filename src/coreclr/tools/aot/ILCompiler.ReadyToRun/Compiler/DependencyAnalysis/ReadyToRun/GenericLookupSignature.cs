// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.JitInterface;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.ReadyToRunConstants;

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

        public GenericLookupSignature(
            CORINFO_RUNTIME_LOOKUP_KIND runtimeLookupKind,
            ReadyToRunFixupKind fixupKind,
            TypeDesc typeArgument,
            MethodWithToken methodArgument,
            FieldDesc fieldArgument,
            GenericContext methodContext)
        {
            Debug.Assert(typeArgument != null || methodArgument != null || fieldArgument != null);
            _runtimeLookupKind = runtimeLookupKind;
            _fixupKind = fixupKind;
            _typeArgument = typeArgument;
            _methodArgument = methodArgument;
            _fieldArgument = fieldArgument;
            _methodContext = methodContext;
        }

        public override int ClassCode => 258608008;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            if (relocsOnly)
            {
                return new ObjectData(Array.Empty<byte>(), null, 1, null);
            }

            // Determine the need for module override
            EcmaModule targetModule;
            if (_methodArgument != null)
            {
                targetModule = _methodArgument.Token.Module;
            }
            else if (_typeArgument != null)
            {
                targetModule = factory.SignatureContext.GetTargetModule(_typeArgument);
            }
            else if (_fieldArgument != null)
            {
                targetModule = factory.SignatureContext.GetTargetModule(_fieldArgument);
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
                    fixupToEmit = ReadyToRunFixupKind.TypeDictionaryLookup;
                    break;

                case CORINFO_RUNTIME_LOOKUP_KIND.CORINFO_LOOKUP_METHODPARAM:
                    fixupToEmit = ReadyToRunFixupKind.MethodDictionaryLookup;
                    break;

                case CORINFO_RUNTIME_LOOKUP_KIND.CORINFO_LOOKUP_THISOBJ:
                    fixupToEmit = ReadyToRunFixupKind.ThisObjDictionaryLookup;
                    contextTypeToEmit = _methodContext.ContextType;
                    break;

                default:
                    throw new NotImplementedException();
            }

            ObjectDataSignatureBuilder dataBuilder = new ObjectDataSignatureBuilder();
            dataBuilder.AddSymbol(this);

            SignatureContext innerContext = dataBuilder.EmitFixup(factory, fixupToEmit, targetModule, factory.SignatureContext);
            if (contextTypeToEmit != null)
            {
                dataBuilder.EmitTypeSignature(contextTypeToEmit, innerContext);
            }

            dataBuilder.EmitByte((byte)_fixupKind);
            if (_methodArgument != null)
            {
                Debug.Assert(_methodArgument.Unboxing == false);

                dataBuilder.EmitMethodSignature(
                    _methodArgument,
                    enforceDefEncoding: false,
                    enforceOwningType: false,
                    context: innerContext,
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
                sb.Append(nameMangler.GetMangledTypeName(_methodArgument.OwningType));
                sb.Append("::");
                sb.Append(nameMangler.GetMangledMethodName(_methodArgument.Method));
                if (_methodArgument.ConstrainedType != null)
                {
                    sb.Append("@");
                    sb.Append(nameMangler.GetMangledTypeName(_methodArgument.ConstrainedType));
                }
                if (!_methodArgument.Token.IsNull)
                {
                    sb.Append(" [");
                    sb.Append(_methodArgument.Token.MetadataReader.GetString(_methodArgument.Token.MetadataReader.GetAssemblyDefinition().Name));
                    sb.Append(":");
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
            GenericLookupSignature otherNode = (GenericLookupSignature)other;
            int result = ((int)_runtimeLookupKind).CompareTo((int)otherNode._runtimeLookupKind);
            if (result != 0)
                return result;

            result = ((int)_fixupKind).CompareTo((int)otherNode._fixupKind);
            if (result != 0)
                return result;

            if (_typeArgument != null || otherNode._typeArgument != null)
            {
                if (_typeArgument == null)
                    return -1;
                if (otherNode._typeArgument == null)
                    return 1;

                result = comparer.Compare(_typeArgument, otherNode._typeArgument);
                if (result != 0)
                    return result;
            }

            if (_fieldArgument != null || otherNode._fieldArgument != null)
            {
                if (_fieldArgument == null)
                    return -1;
                if (otherNode._fieldArgument == null)
                    return 1;

                result = comparer.Compare(_fieldArgument, otherNode._fieldArgument);
                if (result != 0)
                    return result;
            }

            if (_methodArgument != null || otherNode._methodArgument != null)
            {
                if (_methodArgument == null)
                    return -1;
                if (otherNode._methodArgument == null)
                    return 1;

                result = _methodArgument.CompareTo(otherNode._methodArgument, comparer);
                if (result != 0)
                    return result;
            }

            var contextAsMethod = _methodContext.Context as MethodDesc;
            var otherContextAsMethod = otherNode._methodContext.Context as MethodDesc;
            if (contextAsMethod != null || otherContextAsMethod != null)
            {
                if (contextAsMethod == null)
                    return -1;
                if (otherContextAsMethod == null)
                    return 1;

                return comparer.Compare(contextAsMethod, otherContextAsMethod);
            }
            else
            {
                return comparer.Compare(_methodContext.ContextType, otherNode._methodContext.ContextType);
            }
        }
    }
}
