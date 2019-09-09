// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using ILCompiler.DependencyAnalysis.ReadyToRun;

using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis
{
    public enum ReadyToRunHelperId
    {
        Invalid,
        NewHelper,
        NewArr1,
        IsInstanceOf,
        CastClass,
        GetNonGCStaticBase,
        GetGCStaticBase,
        GetThreadStaticBase,
        GetThreadNonGcStaticBase,
        CctorTrigger,

        //// The following helpers are used for generic lookups only
        TypeHandle,
        DeclaringTypeHandle,
        MethodHandle,
        FieldHandle,
        MethodDictionary,
        TypeDictionary,
        MethodEntry,
        VirtualDispatchCell,
    }

    public sealed class ReadyToRunSymbolNodeFactory
    {
        private readonly ReadyToRunCodegenNodeFactory _codegenNodeFactory;

        public ReadyToRunSymbolNodeFactory(ReadyToRunCodegenNodeFactory codegenNodeFactory)
        {
            _codegenNodeFactory = codegenNodeFactory;
        }

        private readonly Dictionary<ModuleToken, ISymbolNode> _importStrings = new Dictionary<ModuleToken, ISymbolNode>();

        public ISymbolNode StringLiteral(ModuleToken token, SignatureContext signatureContext)
        {
            if (!_importStrings.TryGetValue(token, out ISymbolNode stringNode))
            {
                stringNode = new StringImport(_codegenNodeFactory.StringImports, token, signatureContext);
                _importStrings.Add(token, stringNode);
            }
            return stringNode;
        }

        private readonly Dictionary<ReadyToRunHelperId, Dictionary<object, ISymbolNode>> _r2rHelpers = new Dictionary<ReadyToRunHelperId, Dictionary<object, ISymbolNode>>();

        public ISymbolNode ReadyToRunHelper(ReadyToRunHelperId id, object target, SignatureContext signatureContext)
        {
            if (!_r2rHelpers.TryGetValue(id, out Dictionary<object, ISymbolNode> helperNodeMap))
            {
                helperNodeMap = new Dictionary<object, ISymbolNode>();
                _r2rHelpers.Add(id, helperNodeMap);
            }

            if (helperNodeMap.TryGetValue(target, out ISymbolNode helperNode))
            {
                return helperNode;
            }

            switch (id)
            {
                case ReadyToRunHelperId.NewHelper:
                    helperNode = CreateNewHelper((TypeDesc)target, signatureContext);
                    break;

                case ReadyToRunHelperId.NewArr1:
                    helperNode = CreateNewArrayHelper((ArrayType)target, signatureContext);
                    break;

                case ReadyToRunHelperId.GetGCStaticBase:
                    helperNode = CreateGCStaticBaseHelper((TypeDesc)target, signatureContext);
                    break;

                case ReadyToRunHelperId.GetNonGCStaticBase:
                    helperNode = CreateNonGCStaticBaseHelper((TypeDesc)target, signatureContext);
                    break;

                case ReadyToRunHelperId.GetThreadStaticBase:
                    helperNode = CreateThreadGcStaticBaseHelper((TypeDesc)target, signatureContext);
                    break;

                case ReadyToRunHelperId.GetThreadNonGcStaticBase:
                    helperNode = CreateThreadNonGcStaticBaseHelper((TypeDesc)target, signatureContext);
                    break;

                case ReadyToRunHelperId.IsInstanceOf:
                    helperNode = CreateIsInstanceOfHelper((TypeDesc)target, signatureContext);
                    break;

                case ReadyToRunHelperId.CastClass:
                    helperNode = CreateCastClassHelper((TypeDesc)target, signatureContext);
                    break;

                case ReadyToRunHelperId.TypeHandle:
                    helperNode = CreateTypeHandleHelper((TypeDesc)target, signatureContext);
                    break;

                case ReadyToRunHelperId.MethodHandle:
                    helperNode = CreateMethodHandleHelper((MethodWithToken)target, signatureContext);
                    break;

                case ReadyToRunHelperId.FieldHandle:
                    helperNode = CreateFieldHandleHelper((FieldDesc)target, signatureContext);
                    break;

                case ReadyToRunHelperId.CctorTrigger:
                    helperNode = CreateCctorTrigger((TypeDesc)target, signatureContext);
                    break;

                case ReadyToRunHelperId.TypeDictionary:
                    helperNode = CreateTypeDictionary((TypeDesc)target, signatureContext);
                    break;

                case ReadyToRunHelperId.MethodDictionary:
                    helperNode = CreateMethodDictionary((MethodWithToken)target, signatureContext);
                    break;

                default:
                    throw new NotImplementedException(id.ToString());
            }

            helperNodeMap.Add(target, helperNode);
            return helperNode;
        }

        private ISymbolNode CreateNewHelper(TypeDesc type, SignatureContext signatureContext)
        {
            return new DelayLoadHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.HelperImports,
                ILCompiler.ReadyToRunHelper.DelayLoad_Helper,
                new NewObjectFixupSignature(type, signatureContext));
        }

        private ISymbolNode CreateNewArrayHelper(ArrayType type, SignatureContext signatureContext)
        {
            return new DelayLoadHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.HelperImports,
                ILCompiler.ReadyToRunHelper.DelayLoad_Helper,
                new NewArrayFixupSignature(type, signatureContext));
        }

        private ISymbolNode CreateGCStaticBaseHelper(TypeDesc type, SignatureContext signatureContext)
        {
            return new DelayLoadHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.HelperImports,
                ILCompiler.ReadyToRunHelper.DelayLoad_Helper,
                _codegenNodeFactory.TypeSignature(ReadyToRunFixupKind.READYTORUN_FIXUP_StaticBaseGC, type, signatureContext));
        }

        private ISymbolNode CreateNonGCStaticBaseHelper(TypeDesc type, SignatureContext signatureContext)
        {
            return new DelayLoadHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.HelperImports,
                ILCompiler.ReadyToRunHelper.DelayLoad_Helper,
                _codegenNodeFactory.TypeSignature(ReadyToRunFixupKind.READYTORUN_FIXUP_StaticBaseNonGC, type, signatureContext));
        }

        private ISymbolNode CreateThreadGcStaticBaseHelper(TypeDesc type, SignatureContext signatureContext)
        {
            return new DelayLoadHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.HelperImports,
                ILCompiler.ReadyToRunHelper.DelayLoad_Helper,
                _codegenNodeFactory.TypeSignature(ReadyToRunFixupKind.READYTORUN_FIXUP_ThreadStaticBaseGC, type, signatureContext));
        }

        private ISymbolNode CreateThreadNonGcStaticBaseHelper(TypeDesc type, SignatureContext signatureContext)
        {
            return new DelayLoadHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.HelperImports,
                ILCompiler.ReadyToRunHelper.DelayLoad_Helper,
                _codegenNodeFactory.TypeSignature(ReadyToRunFixupKind.READYTORUN_FIXUP_ThreadStaticBaseNonGC, type, signatureContext));
        }

        private ISymbolNode CreateIsInstanceOfHelper(TypeDesc type, SignatureContext signatureContext)
        {
            return new DelayLoadHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.HelperImports,
                ILCompiler.ReadyToRunHelper.DelayLoad_Helper,
                _codegenNodeFactory.TypeSignature(ReadyToRunFixupKind.READYTORUN_FIXUP_IsInstanceOf, type, signatureContext));
        }

        private ISymbolNode CreateCastClassHelper(TypeDesc type, SignatureContext signatureContext)
        {
            return new DelayLoadHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.HelperImports,
                ILCompiler.ReadyToRunHelper.DelayLoad_Helper_Obj,
                _codegenNodeFactory.TypeSignature(ReadyToRunFixupKind.READYTORUN_FIXUP_ChkCast, type, signatureContext));
        }

        private ISymbolNode CreateTypeHandleHelper(TypeDesc type, SignatureContext signatureContext)
        {
            return new PrecodeHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.TypeSignature(ReadyToRunFixupKind.READYTORUN_FIXUP_TypeHandle, type, signatureContext));
        }

        private ISymbolNode CreateMethodHandleHelper(MethodWithToken method, SignatureContext signatureContext)
        {
            bool useInstantiatingStub = method.Method.GetCanonMethodTarget(CanonicalFormKind.Specific) != method.Method;

            return new PrecodeHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.MethodSignature(
                    ReadyToRunFixupKind.READYTORUN_FIXUP_MethodHandle,
                    method,
                    isUnboxingStub: false,
                    isInstantiatingStub: useInstantiatingStub,
                    signatureContext));
        }

        private ISymbolNode CreateFieldHandleHelper(FieldDesc field, SignatureContext signatureContext)
        {
            return new PrecodeHelperImport(
                _codegenNodeFactory,
                new FieldFixupSignature(ReadyToRunFixupKind.READYTORUN_FIXUP_FieldHandle, field, signatureContext));
        }

        private ISymbolNode CreateCctorTrigger(TypeDesc type, SignatureContext signatureContext)
        {
            return new DelayLoadHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.HelperImports,
                ILCompiler.ReadyToRunHelper.DelayLoad_Helper,
                _codegenNodeFactory.TypeSignature(ReadyToRunFixupKind.READYTORUN_FIXUP_CctorTrigger, type, signatureContext));
        }

        private ISymbolNode CreateTypeDictionary(TypeDesc type, SignatureContext signatureContext)
        {
            return new PrecodeHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.TypeSignature(
                    ReadyToRunFixupKind.READYTORUN_FIXUP_TypeDictionary,
                    type,
                    signatureContext));
        }

        private ISymbolNode CreateMethodDictionary(MethodWithToken method, SignatureContext signatureContext)
        {
            return new PrecodeHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.MethodSignature(
                    ReadyToRunFixupKind.READYTORUN_FIXUP_MethodDictionary, 
                    method, 
                    isUnboxingStub: false,
                    isInstantiatingStub: true,
                    signatureContext));
        }

        private readonly Dictionary<FieldDesc, ISymbolNode> _fieldAddressCache = new Dictionary<FieldDesc, ISymbolNode>();

        public ISymbolNode FieldAddress(FieldDesc fieldDesc, SignatureContext signatureContext)
        {
            ISymbolNode result;
            if (!_fieldAddressCache.TryGetValue(fieldDesc, out result))
            {
                result = new DelayLoadHelperImport(
                    _codegenNodeFactory,
                    _codegenNodeFactory.HelperImports,
                    ILCompiler.ReadyToRunHelper.DelayLoad_Helper,
                    new FieldFixupSignature(ReadyToRunFixupKind.READYTORUN_FIXUP_FieldAddress, fieldDesc, signatureContext));
                _fieldAddressCache.Add(fieldDesc, result);
            }
            return result;
        }

        private readonly Dictionary<FieldDesc, ISymbolNode> _fieldOffsetCache = new Dictionary<FieldDesc, ISymbolNode>();

        public ISymbolNode FieldOffset(FieldDesc fieldDesc, SignatureContext signatureContext)
        {
            ISymbolNode result;
            if (!_fieldOffsetCache.TryGetValue(fieldDesc, out result))
            {
                result = new PrecodeHelperImport(
                    _codegenNodeFactory,
                    new FieldFixupSignature(ReadyToRunFixupKind.READYTORUN_FIXUP_FieldOffset, fieldDesc, signatureContext));
                _fieldOffsetCache.Add(fieldDesc, result);
            }
            return result;
        }

        private readonly Dictionary<TypeDesc, ISymbolNode> _fieldBaseOffsetCache = new Dictionary<TypeDesc, ISymbolNode>();

        public ISymbolNode FieldBaseOffset(TypeDesc typeDesc, SignatureContext signatureContext)
        {
            ISymbolNode result;
            if (!_fieldBaseOffsetCache.TryGetValue(typeDesc, out result))
            {
                result = new PrecodeHelperImport(
                    _codegenNodeFactory,
                    _codegenNodeFactory.TypeSignature(ReadyToRunFixupKind.READYTORUN_FIXUP_FieldBaseOffset, typeDesc, signatureContext));
                _fieldBaseOffsetCache.Add(typeDesc, result);
            }
            return result;
        }

        private readonly Dictionary<MethodAndCallSite, ISymbolNode> _interfaceDispatchCells = new Dictionary<MethodAndCallSite, ISymbolNode>();

        public ISymbolNode InterfaceDispatchCell(MethodWithToken method, SignatureContext signatureContext, bool isUnboxingStub, string callSite)
        {
            MethodAndCallSite cellKey = new MethodAndCallSite(method, callSite);
            if (!_interfaceDispatchCells.TryGetValue(cellKey, out ISymbolNode dispatchCell))
            {
                dispatchCell = new DelayLoadHelperMethodImport(
                    _codegenNodeFactory,
                    _codegenNodeFactory.DispatchImports,
                    ILCompiler.ReadyToRunHelper.DelayLoad_MethodCall,
                    method,
                    useVirtualCall: true,
                    useInstantiatingStub: false,
                    _codegenNodeFactory.MethodSignature(ReadyToRunFixupKind.READYTORUN_FIXUP_VirtualEntry,
                        method,
                        isUnboxingStub, isInstantiatingStub: false, signatureContext),
                    signatureContext,
                    callSite);

                _interfaceDispatchCells.Add(cellKey, dispatchCell);
            }
            return dispatchCell;
        }

        private readonly Dictionary<TypeAndMethod, ISymbolNode> _delegateCtors = new Dictionary<TypeAndMethod, ISymbolNode>();

        public ISymbolNode DelegateCtor(TypeDesc delegateType, MethodWithToken method, SignatureContext signatureContext)
        {
            TypeAndMethod ctorKey = new TypeAndMethod(delegateType, method, isUnboxingStub: false, isInstantiatingStub: false, isPrecodeImportRequired: false);
            if (!_delegateCtors.TryGetValue(ctorKey, out ISymbolNode ctorNode))
            {
                IMethodNode targetMethodNode = _codegenNodeFactory.MethodEntrypoint(
                    method,
                    isUnboxingStub: false,
                    isInstantiatingStub: false,
                    isPrecodeImportRequired: false,
                    signatureContext: signatureContext);

                ctorNode = new DelayLoadHelperImport(
                    _codegenNodeFactory,
                    _codegenNodeFactory.HelperImports,
                    ILCompiler.ReadyToRunHelper.DelayLoad_Helper,
                    new DelegateCtorSignature(delegateType, targetMethodNode, method.Token, signatureContext));
                _delegateCtors.Add(ctorKey, ctorNode);
            }
            return ctorNode;
        }

        struct MethodAndCallSite : IEquatable<MethodAndCallSite>
        {
            public readonly MethodWithToken Method;
            public readonly string CallSite;

            public MethodAndCallSite(MethodWithToken method, string callSite)
            {
                CallSite = callSite;
                Method = method;
            }

            public bool Equals(MethodAndCallSite other)
            {
                return CallSite == other.CallSite && Method.Equals(other.Method);
            }

            public override bool Equals(object obj)
            {
                return obj is MethodAndCallSite other && Equals(other);
            }

            public override int GetHashCode()
            {
                return (CallSite != null ? CallSite.GetHashCode() : 0) + unchecked(31 * Method.GetHashCode());
            }
        }

        private class GenericLookupKey : IEquatable<GenericLookupKey>
        {
            public readonly CORINFO_RUNTIME_LOOKUP_KIND LookupKind;
            public readonly ReadyToRunFixupKind FixupKind;
            public readonly TypeDesc TypeArgument;
            public readonly MethodWithToken MethodArgument;
            public readonly FieldDesc FieldArgument;
            public readonly GenericContext MethodContext;

            public GenericLookupKey(
                CORINFO_RUNTIME_LOOKUP_KIND lookupKind,
                ReadyToRunFixupKind fixupKind,
                TypeDesc typeArgument,
                MethodWithToken methodArgument,
                FieldDesc fieldArgument,
                GenericContext methodContext)
            {
                LookupKind = lookupKind;
                FixupKind = fixupKind;
                TypeArgument = typeArgument;
                MethodArgument = methodArgument;
                FieldArgument = fieldArgument;
                MethodContext = methodContext;
            }

            public bool Equals(GenericLookupKey other)
            {
                return LookupKind == other.LookupKind &&
                    FixupKind == other.FixupKind &&
                    RuntimeDeterminedTypeHelper.Equals(TypeArgument, other.TypeArgument) &&
                    RuntimeDeterminedTypeHelper.Equals(MethodArgument?.Method ?? null, other.MethodArgument?.Method ?? null) &&
                    RuntimeDeterminedTypeHelper.Equals(FieldArgument, other.FieldArgument) &&
                    MethodContext.Equals(other.MethodContext);
            }

            public override bool Equals(object obj)
            {
                return obj is GenericLookupKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return unchecked(((int)LookupKind << 24) +
                    (int)FixupKind +
                    (TypeArgument != null ? 31 * RuntimeDeterminedTypeHelper.GetHashCode(TypeArgument) : 0) +
                    (MethodArgument != null ? 31 * RuntimeDeterminedTypeHelper.GetHashCode(MethodArgument.Method) : 0) +
                    (FieldArgument != null ? 31 * RuntimeDeterminedTypeHelper.GetHashCode(FieldArgument) : 0) +
                    MethodContext.GetHashCode());
            }
        }

        private Dictionary<GenericLookupKey, ISymbolNode> _genericLookupHelpers = new Dictionary<GenericLookupKey, ISymbolNode>();

        public ISymbolNode GenericLookupHelper(
            CORINFO_RUNTIME_LOOKUP_KIND runtimeLookupKind,
            ReadyToRunHelperId helperId,
            object helperArgument,
            GenericContext methodContext,
            SignatureContext signatureContext)
        {
            switch (helperId)
            {
                case ReadyToRunHelperId.TypeHandle:
                    return GenericLookupTypeHelper(
                        runtimeLookupKind,
                        ReadyToRunFixupKind.READYTORUN_FIXUP_TypeHandle,
                        helperArgument,
                        methodContext,
                        signatureContext);

                case ReadyToRunHelperId.MethodHandle:
                    return GenericLookupMethodHelper(
                        runtimeLookupKind,
                        ReadyToRunFixupKind.READYTORUN_FIXUP_MethodHandle,
                        (MethodWithToken)helperArgument,
                        methodContext,
                        signatureContext);

                case ReadyToRunHelperId.MethodEntry:
                    return GenericLookupMethodHelper(
                        runtimeLookupKind,
                        ReadyToRunFixupKind.READYTORUN_FIXUP_MethodEntry,
                        (MethodWithToken)helperArgument,
                        methodContext,
                        signatureContext);

                case ReadyToRunHelperId.MethodDictionary:
                    return GenericLookupMethodHelper(
                        runtimeLookupKind,
                        ReadyToRunFixupKind.READYTORUN_FIXUP_MethodHandle,
                        (MethodWithToken)helperArgument,
                        methodContext,
                        signatureContext);

                case ReadyToRunHelperId.TypeDictionary:
                    return GenericLookupTypeHelper(
                        runtimeLookupKind,
                        ReadyToRunFixupKind.READYTORUN_FIXUP_TypeDictionary,
                        (TypeDesc)helperArgument,
                        methodContext,
                        signatureContext);

                case ReadyToRunHelperId.VirtualDispatchCell:
                    return GenericLookupMethodHelper(
                        runtimeLookupKind,
                        ReadyToRunFixupKind.READYTORUN_FIXUP_VirtualEntry,
                        (MethodWithToken)helperArgument,
                        methodContext,
                        signatureContext);

                case ReadyToRunHelperId.FieldHandle:
                    return GenericLookupFieldHelper(
                        runtimeLookupKind,
                        ReadyToRunFixupKind.READYTORUN_FIXUP_FieldHandle,
                        (FieldDesc)helperArgument,
                        methodContext,
                        signatureContext);

                default:
                    throw new NotImplementedException(helperId.ToString());
            }
        }

        private ISymbolNode GenericLookupTypeHelper(
            CORINFO_RUNTIME_LOOKUP_KIND runtimeLookupKind,
            ReadyToRunFixupKind fixupKind,
            object helperArgument,
            GenericContext methodContext,
            SignatureContext signatureContext)
        {
            TypeDesc typeArgument;
            if (helperArgument is MethodWithToken methodWithToken)
            {
                typeArgument = methodWithToken.Method.OwningType;
            }
            else if (helperArgument is FieldDesc fieldDesc)
            {
                typeArgument = fieldDesc.OwningType;
            }
            else
            {
                typeArgument = (TypeDesc)helperArgument;
            }

            GenericLookupKey key = new GenericLookupKey(runtimeLookupKind, fixupKind, typeArgument, methodArgument: null, fieldArgument: null, methodContext);
            ISymbolNode node;
            if (!_genericLookupHelpers.TryGetValue(key, out node))
            {
                node = new DelayLoadHelperImport(
                    _codegenNodeFactory,
                    _codegenNodeFactory.HelperImports,
                    ILCompiler.ReadyToRunHelper.DelayLoad_Helper,
                    new GenericLookupSignature(runtimeLookupKind, fixupKind, typeArgument, methodArgument: null, fieldArgument: null, methodContext, signatureContext));
                _genericLookupHelpers.Add(key, node);
            }
            return node;
        }

        private ISymbolNode GenericLookupFieldHelper(
            CORINFO_RUNTIME_LOOKUP_KIND runtimeLookupKind,
            ReadyToRunFixupKind fixupKind,
            FieldDesc fieldArgument,
            GenericContext methodContext,
            SignatureContext signatureContext)
        {
            GenericLookupKey key = new GenericLookupKey(runtimeLookupKind, fixupKind, typeArgument: null, methodArgument: null, fieldArgument: fieldArgument, methodContext);
            ISymbolNode node;
            if (!_genericLookupHelpers.TryGetValue(key, out node))
            {
                node = new DelayLoadHelperImport(
                    _codegenNodeFactory,
                    _codegenNodeFactory.HelperImports,
                    ILCompiler.ReadyToRunHelper.DelayLoad_Helper,
                    new GenericLookupSignature(runtimeLookupKind, fixupKind, typeArgument: null, methodArgument: null, fieldArgument: fieldArgument, methodContext, signatureContext));
                _genericLookupHelpers.Add(key, node);
            }
            return node;
        }

        private ISymbolNode GenericLookupMethodHelper(
            CORINFO_RUNTIME_LOOKUP_KIND runtimeLookupKind,
            ReadyToRunFixupKind fixupKind,
            MethodWithToken methodArgument,
            GenericContext methodContext,
            SignatureContext signatureContext)
        {
            GenericLookupKey key = new GenericLookupKey(runtimeLookupKind, fixupKind, typeArgument: null, methodArgument, fieldArgument: null, methodContext);
            ISymbolNode node;
            if (!_genericLookupHelpers.TryGetValue(key, out node))
            {
                node = new DelayLoadHelperMethodImport(
                    _codegenNodeFactory,
                    _codegenNodeFactory.HelperImports,
                    ILCompiler.ReadyToRunHelper.DelayLoad_Helper,
                    methodArgument,
                    useVirtualCall: false,
                    useInstantiatingStub: false,
                    new GenericLookupSignature(runtimeLookupKind, fixupKind, typeArgument: null, methodArgument, fieldArgument: null, methodContext, signatureContext),
                    signatureContext);
                _genericLookupHelpers.Add(key, node);
            }
            return node;
        }

        private Dictionary<MethodWithToken, ISymbolNode> _indirectPInvokeTargetNodes = new Dictionary<MethodWithToken, ISymbolNode>();

        public ISymbolNode GetIndirectPInvokeTargetNode(MethodWithToken methodWithToken, SignatureContext signatureContext)
        {
            ISymbolNode result;

            if (!_indirectPInvokeTargetNodes.TryGetValue(methodWithToken, out result))
            {
                result = new PrecodeHelperImport(
                    _codegenNodeFactory,
                    _codegenNodeFactory.MethodSignature(
                        ReadyToRunFixupKind.READYTORUN_FIXUP_IndirectPInvokeTarget,
                        methodWithToken,
                        signatureContext: signatureContext,
                        isUnboxingStub: false,
                        isInstantiatingStub: false));

                _indirectPInvokeTargetNodes.Add(methodWithToken, result);
            }

            return result;
        }
    }
}
