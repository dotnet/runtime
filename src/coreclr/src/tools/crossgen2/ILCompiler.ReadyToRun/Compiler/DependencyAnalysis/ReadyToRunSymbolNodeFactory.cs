// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using ILCompiler.DependencyAnalysis.ReadyToRun;

using Internal.JitInterface;
using Internal.TypeSystem;

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
            CreateNodeCaches();
        }

        private void CreateNodeCaches()
        {
            _importStrings = new NodeCache<ModuleTokenAndSignatureContext, ISymbolNode>(key =>
            {
                return new StringImport(_codegenNodeFactory.StringImports, key.ModuleToken, key.SignatureContext);
            });

            _r2rHelpers = new NodeCache<ReadyToRunHelperKey, ISymbolNode>(CreateReadyToRunHelper);

            _fieldAddressCache = new NodeCache<FieldAndSignatureContext, ISymbolNode>(key =>
            {
                return new DelayLoadHelperImport(
                    _codegenNodeFactory,
                    _codegenNodeFactory.HelperImports,
                    ILCompiler.ReadyToRunHelper.DelayLoad_Helper,
                    new FieldFixupSignature(ReadyToRunFixupKind.READYTORUN_FIXUP_FieldAddress, key.Field, key.SignatureContext)
                );
            });

            _fieldOffsetCache = new NodeCache<FieldAndSignatureContext, ISymbolNode>(key =>
            {
                return new PrecodeHelperImport(
                    _codegenNodeFactory,
                    new FieldFixupSignature(ReadyToRunFixupKind.READYTORUN_FIXUP_FieldOffset, key.Field, key.SignatureContext)
                );
            });

            _fieldBaseOffsetCache = new NodeCache<TypeAndSignatureContext, ISymbolNode>(key =>
            {
                return new PrecodeHelperImport(
                    _codegenNodeFactory,
                    _codegenNodeFactory.TypeSignature(ReadyToRunFixupKind.READYTORUN_FIXUP_FieldBaseOffset, key.Type, key.SignatureContext)
                );
            });

            _interfaceDispatchCells = new NodeCache<MethodAndCallSite, ISymbolNode>(cellKey =>
            {
                return new DelayLoadHelperMethodImport(
                    _codegenNodeFactory,
                    _codegenNodeFactory.DispatchImports,
                    ILCompiler.ReadyToRunHelper.DelayLoad_MethodCall,
                    cellKey.Method,
                    useVirtualCall: true,
                    useInstantiatingStub: false,
                    _codegenNodeFactory.MethodSignature(ReadyToRunFixupKind.READYTORUN_FIXUP_VirtualEntry,
                        cellKey.Method,
                        cellKey.IsUnboxingStub, isInstantiatingStub: false, cellKey.SignatureContext),
                    cellKey.SignatureContext,
                    cellKey.CallSite);
            });

            _delegateCtors = new NodeCache<TypeAndMethod, ISymbolNode>(ctorKey =>
            {
                SignatureContext signatureContext = ctorKey.SignatureContext;
                IMethodNode targetMethodNode = _codegenNodeFactory.MethodEntrypoint(
                    ctorKey.Method,
                    isUnboxingStub: false,
                    isInstantiatingStub: false,
                    isPrecodeImportRequired: false,
                    signatureContext: signatureContext);

                return new DelayLoadHelperImport(
                    _codegenNodeFactory,
                    _codegenNodeFactory.HelperImports,
                    ILCompiler.ReadyToRunHelper.DelayLoad_Helper,
                    new DelegateCtorSignature(ctorKey.Type, targetMethodNode, ctorKey.Method.Token, signatureContext));
            });

            _genericLookupHelpers = new NodeCache<GenericLookupKey, ISymbolNode>(key =>
            {
                return new DelayLoadHelperImport(
                    _codegenNodeFactory,
                    _codegenNodeFactory.HelperImports,
                    ILCompiler.ReadyToRunHelper.DelayLoad_Helper,
                    new GenericLookupSignature(
                        key.LookupKind,
                        key.FixupKind,
                        key.TypeArgument,
                        key.MethodArgument,
                        key.FieldArgument,
                        key.MethodContext,
                        key.SignatureContext));
            });

            _indirectPInvokeTargetNodes = new NodeCache<IndirectPInvokeTargetKey, ISymbolNode>(key =>
            {
                return new PrecodeHelperImport(
                    _codegenNodeFactory,
                    _codegenNodeFactory.MethodSignature(
                        ReadyToRunFixupKind.READYTORUN_FIXUP_IndirectPInvokeTarget,
                        key.MethodWithToken,
                        signatureContext: key.SignatureContext,
                        isUnboxingStub: false,
                        isInstantiatingStub: false));
            });
        }

        private struct ModuleTokenAndSignatureContext : IEquatable<ModuleTokenAndSignatureContext>
        {
            public readonly ModuleToken ModuleToken;
            public readonly SignatureContext SignatureContext;

            public ModuleTokenAndSignatureContext(ModuleToken moduleToken, SignatureContext signatureContext)
            {
                ModuleToken = moduleToken;
                SignatureContext = signatureContext;
            }

            public bool Equals(ModuleTokenAndSignatureContext other)
            {
                return ModuleToken.Equals(other.ModuleToken)
                    && SignatureContext.Equals(other.SignatureContext);
            }

            public override bool Equals(object obj)
            {
                return obj is ModuleTokenAndSignatureContext other && Equals(other);
            }

            public override int GetHashCode()
            {
                return ModuleToken.GetHashCode() ^ (SignatureContext.GetHashCode() * 31);
            }
        }

        private NodeCache<ModuleTokenAndSignatureContext, ISymbolNode> _importStrings;

        public ISymbolNode StringLiteral(ModuleToken moduleToken, SignatureContext signatureContext)
        {
            return _importStrings.GetOrAdd(new ModuleTokenAndSignatureContext(moduleToken, signatureContext));
        }

        private struct ReadyToRunHelperKey
        {
            public readonly ReadyToRunHelperId Id;
            public readonly object Target;
            public readonly SignatureContext SignatureContext;

            public ReadyToRunHelperKey(ReadyToRunHelperId id, object target, SignatureContext signatureContext)
            {
                Id = id;
                Target = target;
                SignatureContext = signatureContext;
            }

            public bool Equals(ReadyToRunHelperKey other)
            {
                return Id == other.Id
                    && Target.Equals(other.Target)
                    && SignatureContext.Equals(other.SignatureContext);
            }

            public override bool Equals(object obj)
            {
                return obj is ReadyToRunHelperKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode() 
                    ^ (Target.GetHashCode() * 23)
                    ^ (SignatureContext.GetHashCode() * 31);
            }
        }

        private NodeCache<ReadyToRunHelperKey, ISymbolNode> _r2rHelpers;

        private ISymbolNode CreateReadyToRunHelper(ReadyToRunHelperKey key)
        {
            switch (key.Id)
            {
                case ReadyToRunHelperId.NewHelper:
                    return CreateNewHelper((TypeDesc)key.Target, key.SignatureContext);

                case ReadyToRunHelperId.NewArr1:
                    return CreateNewArrayHelper((ArrayType)key.Target, key.SignatureContext);

                case ReadyToRunHelperId.GetGCStaticBase:
                    return CreateGCStaticBaseHelper((TypeDesc)key.Target, key.SignatureContext);

                case ReadyToRunHelperId.GetNonGCStaticBase:
                    return CreateNonGCStaticBaseHelper((TypeDesc)key.Target, key.SignatureContext);

                case ReadyToRunHelperId.GetThreadStaticBase:
                    return CreateThreadGcStaticBaseHelper((TypeDesc)key.Target, key.SignatureContext);

                case ReadyToRunHelperId.GetThreadNonGcStaticBase:
                    return CreateThreadNonGcStaticBaseHelper((TypeDesc)key.Target, key.SignatureContext);

                case ReadyToRunHelperId.IsInstanceOf:
                    return CreateIsInstanceOfHelper((TypeDesc)key.Target, key.SignatureContext);

                case ReadyToRunHelperId.CastClass:
                    return CreateCastClassHelper((TypeDesc)key.Target, key.SignatureContext);

                case ReadyToRunHelperId.TypeHandle:
                    return CreateTypeHandleHelper((TypeDesc)key.Target, key.SignatureContext);

                case ReadyToRunHelperId.MethodHandle:
                    return CreateMethodHandleHelper((MethodWithToken)key.Target, key.SignatureContext);

                case ReadyToRunHelperId.FieldHandle:
                    return CreateFieldHandleHelper((FieldDesc)key.Target, key.SignatureContext);

                case ReadyToRunHelperId.CctorTrigger:
                    return CreateCctorTrigger((TypeDesc)key.Target, key.SignatureContext);

                case ReadyToRunHelperId.TypeDictionary:
                    return CreateTypeDictionary((TypeDesc)key.Target, key.SignatureContext);

                case ReadyToRunHelperId.MethodDictionary:
                    return CreateMethodDictionary((MethodWithToken)key.Target, key.SignatureContext);

                default:
                    throw new NotImplementedException(key.Id.ToString());
            }
        }

        public ISymbolNode ReadyToRunHelper(ReadyToRunHelperId id, object target, SignatureContext signatureContext)
        {
            return _r2rHelpers.GetOrAdd(new ReadyToRunHelperKey(id, target, signatureContext));
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
                _codegenNodeFactory.TypeSignature(ReadyToRunFixupKind.READYTORUN_FIXUP_TypeDictionary, type, signatureContext)
            );
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

        private struct FieldAndSignatureContext : IEquatable<FieldAndSignatureContext>
        {
            public readonly FieldDesc Field;
            public readonly SignatureContext SignatureContext;

            public FieldAndSignatureContext(FieldDesc fieldDesc, SignatureContext signatureContext)
            {
                Field = fieldDesc;
                SignatureContext = signatureContext;
            }

            public bool Equals(FieldAndSignatureContext other)
            {
                return Field.Equals(other.Field) &&
                       SignatureContext.Equals(other.SignatureContext);
            }

            public override bool Equals(object obj)
            {
                return obj is FieldAndSignatureContext other && Equals(other);
            }

            public override int GetHashCode()
            {
                return Field.GetHashCode() ^ (SignatureContext.GetHashCode() * 31);
            }
        }

        private NodeCache<FieldAndSignatureContext, ISymbolNode> _fieldAddressCache;

        public ISymbolNode FieldAddress(FieldDesc fieldDesc, SignatureContext signatureContext)
        {
            return _fieldAddressCache.GetOrAdd(new FieldAndSignatureContext(fieldDesc, signatureContext));
        }

        private NodeCache<FieldAndSignatureContext, ISymbolNode> _fieldOffsetCache;

        public ISymbolNode FieldOffset(FieldDesc fieldDesc, SignatureContext signatureContext)
        {
            return _fieldOffsetCache.GetOrAdd(new FieldAndSignatureContext(fieldDesc, signatureContext));
        }

        private struct TypeAndSignatureContext : IEquatable<TypeAndSignatureContext>
        {
            public readonly TypeDesc Type;
            public readonly SignatureContext SignatureContext;

            public TypeAndSignatureContext(TypeDesc typeDesc, SignatureContext signatureContext)
            {
                Type = typeDesc;
                SignatureContext = signatureContext;
            }

            public bool Equals(TypeAndSignatureContext other)
            {
                return Type.Equals(other.Type) &&
                       SignatureContext.Equals(other.SignatureContext);
            }

            public override bool Equals(object obj)
            {
                return obj is FieldAndSignatureContext other && Equals(other);
            }

            public override int GetHashCode()
            {
                return Type.GetHashCode() ^ (SignatureContext.GetHashCode() * 31);
            }
        }

        private NodeCache<TypeAndSignatureContext, ISymbolNode> _fieldBaseOffsetCache;

        public ISymbolNode FieldBaseOffset(TypeDesc typeDesc, SignatureContext signatureContext)
        {
            return _fieldBaseOffsetCache.GetOrAdd(new TypeAndSignatureContext(typeDesc, signatureContext));
        }

        private NodeCache<MethodAndCallSite, ISymbolNode> _interfaceDispatchCells = new NodeCache<MethodAndCallSite, ISymbolNode>();

        public ISymbolNode InterfaceDispatchCell(MethodWithToken method, SignatureContext signatureContext, bool isUnboxingStub, string callSite)
        {
            MethodAndCallSite cellKey = new MethodAndCallSite(method, isUnboxingStub, callSite, signatureContext);
            return _interfaceDispatchCells.GetOrAdd(cellKey);
        }

        private NodeCache<TypeAndMethod, ISymbolNode> _delegateCtors = new NodeCache<TypeAndMethod, ISymbolNode>();

        public ISymbolNode DelegateCtor(TypeDesc delegateType, MethodWithToken method, SignatureContext signatureContext)
        {
            TypeAndMethod ctorKey = new TypeAndMethod(
                delegateType,
                method,
                isUnboxingStub: false,
                isInstantiatingStub: false,
                isPrecodeImportRequired: false,
                signatureContext);
            return _delegateCtors.GetOrAdd(ctorKey);
        }

        struct MethodAndCallSite : IEquatable<MethodAndCallSite>
        {
            public readonly MethodWithToken Method;
            public readonly bool IsUnboxingStub;
            public readonly string CallSite;
            public readonly SignatureContext SignatureContext;

            public MethodAndCallSite(MethodWithToken method, bool isUnboxingStub, string callSite, SignatureContext signatureContext)
            {
                CallSite = callSite;
                IsUnboxingStub = isUnboxingStub;
                Method = method;
                SignatureContext = signatureContext;
            }

            public bool Equals(MethodAndCallSite other)
            {
                return CallSite == other.CallSite 
                    && Method.Equals(other.Method) 
                    && IsUnboxingStub == other.IsUnboxingStub
                    && SignatureContext.Equals(other.SignatureContext);
            }

            public override bool Equals(object obj)
            {
                return obj is MethodAndCallSite other && Equals(other);
            }

            public override int GetHashCode()
            {
                return (CallSite != null ? CallSite.GetHashCode() : 0) 
                    ^ unchecked(31 * Method.GetHashCode())
                    ^ (IsUnboxingStub ? -0x80000000 : 0)
                    ^ (23 * SignatureContext.GetHashCode());
            }
        }

        private struct GenericLookupKey : IEquatable<GenericLookupKey>
        {
            public readonly CORINFO_RUNTIME_LOOKUP_KIND LookupKind;
            public readonly ReadyToRunFixupKind FixupKind;
            public readonly TypeDesc TypeArgument;
            public readonly MethodWithToken MethodArgument;
            public readonly FieldDesc FieldArgument;
            public readonly GenericContext MethodContext;
            public readonly SignatureContext SignatureContext;

            public GenericLookupKey(
                CORINFO_RUNTIME_LOOKUP_KIND lookupKind,
                ReadyToRunFixupKind fixupKind,
                TypeDesc typeArgument,
                MethodWithToken methodArgument,
                FieldDesc fieldArgument,
                GenericContext methodContext,
                SignatureContext signatureContext)
            {
                LookupKind = lookupKind;
                FixupKind = fixupKind;
                TypeArgument = typeArgument;
                MethodArgument = methodArgument;
                FieldArgument = fieldArgument;
                MethodContext = methodContext;
                SignatureContext = signatureContext;
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

        private NodeCache<GenericLookupKey, ISymbolNode> _genericLookupHelpers;

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

            GenericLookupKey key = new GenericLookupKey(runtimeLookupKind, fixupKind, typeArgument, methodArgument: null, fieldArgument: null, methodContext, signatureContext);
            return _genericLookupHelpers.GetOrAdd(key);
        }

        private ISymbolNode GenericLookupFieldHelper(
            CORINFO_RUNTIME_LOOKUP_KIND runtimeLookupKind,
            ReadyToRunFixupKind fixupKind,
            FieldDesc fieldArgument,
            GenericContext methodContext,
            SignatureContext signatureContext)
        {
            GenericLookupKey key = new GenericLookupKey(runtimeLookupKind, fixupKind, typeArgument: null, methodArgument: null, fieldArgument: fieldArgument, methodContext, signatureContext);
            return _genericLookupHelpers.GetOrAdd(key);
        }

        private ISymbolNode GenericLookupMethodHelper(
            CORINFO_RUNTIME_LOOKUP_KIND runtimeLookupKind,
            ReadyToRunFixupKind fixupKind,
            MethodWithToken methodArgument,
            GenericContext methodContext,
            SignatureContext signatureContext)
        {
            GenericLookupKey key = new GenericLookupKey(runtimeLookupKind, fixupKind, typeArgument: null, methodArgument, fieldArgument: null, methodContext, signatureContext);
            return _genericLookupHelpers.GetOrAdd(key);
        }

        private struct IndirectPInvokeTargetKey : IEquatable<IndirectPInvokeTargetKey>
        {
            public readonly MethodWithToken MethodWithToken;
            public readonly SignatureContext SignatureContext;

            public IndirectPInvokeTargetKey(MethodWithToken methodWithToken, SignatureContext signatureContext)
            {
                MethodWithToken = methodWithToken;
                SignatureContext = signatureContext;
            }

            public bool Equals(IndirectPInvokeTargetKey other)
            {
                return MethodWithToken.Equals(other.MethodWithToken)
                    && SignatureContext.Equals(other.SignatureContext);
            }

            public override bool Equals(object obj)
            {
                return obj is IndirectPInvokeTargetKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return MethodWithToken.GetHashCode() ^ (SignatureContext.GetHashCode() * 31);
            }
        }

        private NodeCache<IndirectPInvokeTargetKey, ISymbolNode> _indirectPInvokeTargetNodes = new NodeCache<IndirectPInvokeTargetKey, ISymbolNode>();

        public ISymbolNode GetIndirectPInvokeTargetNode(MethodWithToken methodWithToken, SignatureContext signatureContext)
        {
            return _indirectPInvokeTargetNodes.GetOrAdd(new IndirectPInvokeTargetKey(methodWithToken, signatureContext));
        }
    }
}
