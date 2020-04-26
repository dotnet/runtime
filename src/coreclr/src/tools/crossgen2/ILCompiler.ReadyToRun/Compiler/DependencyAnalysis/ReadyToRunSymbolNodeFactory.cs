// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using ILCompiler.DependencyAnalysis.ReadyToRun;

using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.ReadyToRunConstants;

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
        private readonly NodeFactory _codegenNodeFactory;

        public ReadyToRunSymbolNodeFactory(NodeFactory codegenNodeFactory)
        {
            _codegenNodeFactory = codegenNodeFactory;
            CreateNodeCaches();
        }

        private void CreateNodeCaches()
        {
            _importStrings = new NodeCache<ModuleToken, ISymbolNode>(key =>
            {
                return new StringImport(_codegenNodeFactory.StringImports, key);
            });

            _r2rHelpers = new NodeCache<ReadyToRunHelperKey, ISymbolNode>(CreateReadyToRunHelper);

            _instructionSetSupportFixups = new NodeCache<string, ISymbolNode>(key =>
            {
                return new PrecodeHelperImport(
                    _codegenNodeFactory,
                    new ReadyToRunInstructionSetSupportSignature(key));
            });

            _fieldAddressCache = new NodeCache<FieldDesc, ISymbolNode>(key =>
            {
                return new DelayLoadHelperImport(
                    _codegenNodeFactory,
                    _codegenNodeFactory.HelperImports,
                    ReadyToRunHelper.DelayLoad_Helper,
                    new FieldFixupSignature(ReadyToRunFixupKind.FieldAddress, key)
                );
            });

            _fieldOffsetCache = new NodeCache<FieldDesc, ISymbolNode>(key =>
            {
                return new PrecodeHelperImport(
                    _codegenNodeFactory,
                    new FieldFixupSignature(ReadyToRunFixupKind.FieldOffset, key)
                );
            });

            _fieldBaseOffsetCache = new NodeCache<TypeDesc, ISymbolNode>(key =>
            {
                return new PrecodeHelperImport(
                    _codegenNodeFactory,
                    _codegenNodeFactory.TypeSignature(ReadyToRunFixupKind.FieldBaseOffset, key)
                );
            });

            _checkFieldOffsetCache = new NodeCache<FieldDesc, ISymbolNode>(key =>
            {
                return new PrecodeHelperImport(
                    _codegenNodeFactory,
                    new FieldFixupSignature(ReadyToRunFixupKind.Check_FieldOffset, key)
                );
            });

            _interfaceDispatchCells = new NodeCache<MethodAndCallSite, ISymbolNode>(cellKey =>
            {
                return new DelayLoadHelperMethodImport(
                    _codegenNodeFactory,
                    _codegenNodeFactory.DispatchImports,
                    ReadyToRunHelper.DelayLoad_MethodCall,
                    cellKey.Method,
                    useVirtualCall: true,
                    useInstantiatingStub: false,
                    _codegenNodeFactory.MethodSignature(ReadyToRunFixupKind.VirtualEntry,
                        cellKey.Method,
                        cellKey.IsUnboxingStub, isInstantiatingStub: false),
                    cellKey.CallSite);
            });

            _delegateCtors = new NodeCache<TypeAndMethod, ISymbolNode>(ctorKey =>
            {
                IMethodNode targetMethodNode = _codegenNodeFactory.MethodEntrypoint(
                    ctorKey.Method,
                    isUnboxingStub: false,
                    isInstantiatingStub: ctorKey.Method.Method.HasInstantiation,
                    isPrecodeImportRequired: false);

                return new DelayLoadHelperImport(
                    _codegenNodeFactory,
                    _codegenNodeFactory.HelperImports,
                    ReadyToRunHelper.DelayLoad_Helper_ObjObj,
                    new DelegateCtorSignature(ctorKey.Type, targetMethodNode, ctorKey.Method.Token));
            });

            _checkTypeLayoutCache = new NodeCache<TypeDesc, ISymbolNode>(key =>
            {
                return new PrecodeHelperImport(
                    _codegenNodeFactory,
                    _codegenNodeFactory.TypeSignature(ReadyToRunFixupKind.Check_TypeLayout, key)
                );
            });

            _genericLookupHelpers = new NodeCache<GenericLookupKey, ISymbolNode>(key =>
            {
                return new DelayLoadHelperImport(
                    _codegenNodeFactory,
                    _codegenNodeFactory.HelperImports,
                    ReadyToRunHelper.DelayLoad_Helper,
                    new GenericLookupSignature(
                        key.LookupKind,
                        key.FixupKind,
                        key.TypeArgument,
                        key.MethodArgument,
                        key.FieldArgument,
                        key.MethodContext));
            });

            _pInvokeTargetNodes = new NodeCache<PInvokeTargetKey, ISymbolNode>(key =>
            {
                return new PrecodeHelperImport(
                    _codegenNodeFactory,
                    _codegenNodeFactory.MethodSignature(
                        key.IsIndirect ? ReadyToRunFixupKind.IndirectPInvokeTarget : ReadyToRunFixupKind.PInvokeTarget,
                        key.MethodWithToken,
                        isUnboxingStub: false,
                        isInstantiatingStub: false));
            });
        }

        private NodeCache<ModuleToken, ISymbolNode> _importStrings;

        public ISymbolNode StringLiteral(ModuleToken moduleToken)
        {
            return _importStrings.GetOrAdd(moduleToken);
        }

        private struct ReadyToRunHelperKey
        {
            public readonly ReadyToRunHelperId Id;
            public readonly object Target;

            public ReadyToRunHelperKey(ReadyToRunHelperId id, object target)
            {
                Id = id;
                Target = target;
            }

            public bool Equals(ReadyToRunHelperKey other)
            {
                return Id == other.Id && Target.Equals(other.Target);
            }

            public override bool Equals(object obj)
            {
                return obj is ReadyToRunHelperKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode() ^ (Target.GetHashCode() * 23);
            }
        }

        private NodeCache<ReadyToRunHelperKey, ISymbolNode> _r2rHelpers;

        private ISymbolNode CreateReadyToRunHelper(ReadyToRunHelperKey key)
        {
            switch (key.Id)
            {
                case ReadyToRunHelperId.NewHelper:
                    return CreateNewHelper((TypeDesc)key.Target);

                case ReadyToRunHelperId.NewArr1:
                    return CreateNewArrayHelper((ArrayType)key.Target);

                case ReadyToRunHelperId.GetGCStaticBase:
                    return CreateGCStaticBaseHelper((TypeDesc)key.Target);

                case ReadyToRunHelperId.GetNonGCStaticBase:
                    return CreateNonGCStaticBaseHelper((TypeDesc)key.Target);

                case ReadyToRunHelperId.GetThreadStaticBase:
                    return CreateThreadGcStaticBaseHelper((TypeDesc)key.Target);

                case ReadyToRunHelperId.GetThreadNonGcStaticBase:
                    return CreateThreadNonGcStaticBaseHelper((TypeDesc)key.Target);

                case ReadyToRunHelperId.IsInstanceOf:
                    return CreateIsInstanceOfHelper((TypeDesc)key.Target);

                case ReadyToRunHelperId.CastClass:
                    return CreateCastClassHelper((TypeDesc)key.Target);

                case ReadyToRunHelperId.TypeHandle:
                    return CreateTypeHandleHelper((TypeDesc)key.Target);

                case ReadyToRunHelperId.MethodHandle:
                    return CreateMethodHandleHelper((MethodWithToken)key.Target);

                case ReadyToRunHelperId.FieldHandle:
                    return CreateFieldHandleHelper((FieldDesc)key.Target);

                case ReadyToRunHelperId.CctorTrigger:
                    return CreateCctorTrigger((TypeDesc)key.Target);

                case ReadyToRunHelperId.TypeDictionary:
                    return CreateTypeDictionary((TypeDesc)key.Target);

                case ReadyToRunHelperId.MethodDictionary:
                    return CreateMethodDictionary((MethodWithToken)key.Target);

                default:
                    throw new NotImplementedException(key.Id.ToString());
            }
        }

        public ISymbolNode CreateReadyToRunHelper(ReadyToRunHelperId id, object target)
        {
            return _r2rHelpers.GetOrAdd(new ReadyToRunHelperKey(id, target));
        }

        private NodeCache<string, ISymbolNode> _instructionSetSupportFixups;

        public ISymbolNode PerMethodInstructionSetSupportFixup(InstructionSetSupport instructionSetSupport)
        {
            string key = ReadyToRunInstructionSetSupportSignature.ToInstructionSetSupportString(instructionSetSupport);
            return _instructionSetSupportFixups.GetOrAdd(key);
        }

        private ISymbolNode CreateNewHelper(TypeDesc type)
        {
            return new DelayLoadHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.HelperImports,
                ReadyToRunHelper.DelayLoad_Helper,
                new NewObjectFixupSignature(type));
        }

        private ISymbolNode CreateNewArrayHelper(ArrayType type)
        {
            return new DelayLoadHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.HelperImports,
                ReadyToRunHelper.DelayLoad_Helper,
                new NewArrayFixupSignature(type));
        }

        private ISymbolNode CreateGCStaticBaseHelper(TypeDesc type)
        {
            return new DelayLoadHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.HelperImports,
                ReadyToRunHelper.DelayLoad_Helper,
                _codegenNodeFactory.TypeSignature(ReadyToRunFixupKind.StaticBaseGC, type));
        }

        private ISymbolNode CreateNonGCStaticBaseHelper(TypeDesc type)
        {
            return new DelayLoadHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.HelperImports,
                ReadyToRunHelper.DelayLoad_Helper,
                _codegenNodeFactory.TypeSignature(ReadyToRunFixupKind.StaticBaseNonGC, type));
        }

        private ISymbolNode CreateThreadGcStaticBaseHelper(TypeDesc type)
        {
            return new DelayLoadHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.HelperImports,
                ReadyToRunHelper.DelayLoad_Helper,
                _codegenNodeFactory.TypeSignature(ReadyToRunFixupKind.ThreadStaticBaseGC, type));
        }

        private ISymbolNode CreateThreadNonGcStaticBaseHelper(TypeDesc type)
        {
            return new DelayLoadHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.HelperImports,
                ReadyToRunHelper.DelayLoad_Helper,
                _codegenNodeFactory.TypeSignature(ReadyToRunFixupKind.ThreadStaticBaseNonGC, type));
        }

        private ISymbolNode CreateIsInstanceOfHelper(TypeDesc type)
        {
            return new DelayLoadHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.HelperImports,
                ReadyToRunHelper.DelayLoad_Helper_Obj,
                _codegenNodeFactory.TypeSignature(ReadyToRunFixupKind.IsInstanceOf, type));
        }

        private ISymbolNode CreateCastClassHelper(TypeDesc type)
        {
            return new DelayLoadHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.HelperImports,
                ReadyToRunHelper.DelayLoad_Helper_Obj,
                _codegenNodeFactory.TypeSignature(ReadyToRunFixupKind.ChkCast, type));
        }

        private ISymbolNode CreateTypeHandleHelper(TypeDesc type)
        {
            return new PrecodeHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.TypeSignature(ReadyToRunFixupKind.TypeHandle, type));
        }

        private ISymbolNode CreateMethodHandleHelper(MethodWithToken method)
        {
            bool useUnboxingStub = method.Method.IsUnboxingThunk();
            if (useUnboxingStub)
            {
                method = new MethodWithToken(method.Method.GetUnboxedMethod(), method.Token, method.ConstrainedType);
            }

            bool useInstantiatingStub = method.Method.GetCanonMethodTarget(CanonicalFormKind.Specific) != method.Method;

            return new PrecodeHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.MethodSignature(
                    ReadyToRunFixupKind.MethodHandle,
                    method,
                    isUnboxingStub: useUnboxingStub,
                    isInstantiatingStub: useInstantiatingStub));
        }

        private ISymbolNode CreateFieldHandleHelper(FieldDesc field)
        {
            return new PrecodeHelperImport(
                _codegenNodeFactory,
                new FieldFixupSignature(ReadyToRunFixupKind.FieldHandle, field));
        }

        private ISymbolNode CreateCctorTrigger(TypeDesc type)
        {
            return new DelayLoadHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.HelperImports,
                ReadyToRunHelper.DelayLoad_Helper,
                _codegenNodeFactory.TypeSignature(ReadyToRunFixupKind.CctorTrigger, type));
        }

        private ISymbolNode CreateTypeDictionary(TypeDesc type)
        {
            return new PrecodeHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.TypeSignature(ReadyToRunFixupKind.TypeDictionary, type)
            );
        }

        private ISymbolNode CreateMethodDictionary(MethodWithToken method)
        {
            return new PrecodeHelperImport(
                _codegenNodeFactory,
                _codegenNodeFactory.MethodSignature(
                    ReadyToRunFixupKind.MethodDictionary, 
                    method, 
                    isUnboxingStub: false,
                    isInstantiatingStub: true));
        }

        private NodeCache<FieldDesc, ISymbolNode> _fieldAddressCache;

        public ISymbolNode FieldAddress(FieldDesc fieldDesc)
        {
            return _fieldAddressCache.GetOrAdd(fieldDesc);
        }

        private NodeCache<FieldDesc, ISymbolNode> _fieldOffsetCache;

        public ISymbolNode FieldOffset(FieldDesc fieldDesc)
        {
            return _fieldOffsetCache.GetOrAdd(fieldDesc);
        }

        private NodeCache<FieldDesc, ISymbolNode> _checkFieldOffsetCache;

        public ISymbolNode CheckFieldOffset(FieldDesc fieldDesc)
        {
            return _checkFieldOffsetCache.GetOrAdd(fieldDesc);
        }

        private NodeCache<TypeDesc, ISymbolNode> _fieldBaseOffsetCache;

        public ISymbolNode FieldBaseOffset(TypeDesc typeDesc)
        {
            return _fieldBaseOffsetCache.GetOrAdd(typeDesc);
        }

        private NodeCache<MethodAndCallSite, ISymbolNode> _interfaceDispatchCells = new NodeCache<MethodAndCallSite, ISymbolNode>();

        public ISymbolNode InterfaceDispatchCell(MethodWithToken method, bool isUnboxingStub, string callSite)
        {
            MethodAndCallSite cellKey = new MethodAndCallSite(method, isUnboxingStub, callSite);
            return _interfaceDispatchCells.GetOrAdd(cellKey);
        }

        private NodeCache<TypeAndMethod, ISymbolNode> _delegateCtors = new NodeCache<TypeAndMethod, ISymbolNode>();

        public ISymbolNode DelegateCtor(TypeDesc delegateType, MethodWithToken method)
        {
            TypeAndMethod ctorKey = new TypeAndMethod(
                delegateType,
                method,
                isUnboxingStub: false,
                isInstantiatingStub: false,
                isPrecodeImportRequired: false);
            return _delegateCtors.GetOrAdd(ctorKey);
        }

        private NodeCache<TypeDesc, ISymbolNode> _checkTypeLayoutCache;

        public ISymbolNode CheckTypeLayout(TypeDesc type)
        {
            return _checkTypeLayoutCache.GetOrAdd(type);
        }

        struct MethodAndCallSite : IEquatable<MethodAndCallSite>
        {
            public readonly MethodWithToken Method;
            public readonly bool IsUnboxingStub;
            public readonly string CallSite;

            public MethodAndCallSite(MethodWithToken method, bool isUnboxingStub, string callSite)
            {
                CallSite = callSite;
                IsUnboxingStub = isUnboxingStub;
                Method = method;
            }

            public bool Equals(MethodAndCallSite other)
            {
                return CallSite == other.CallSite && Method.Equals(other.Method) && IsUnboxingStub == other.IsUnboxingStub;
            }

            public override bool Equals(object obj)
            {
                return obj is MethodAndCallSite other && Equals(other);
            }

            public override int GetHashCode()
            {
                return (CallSite != null ? CallSite.GetHashCode() : 0)
                    ^ unchecked(31 * Method.GetHashCode())
                    ^ (IsUnboxingStub ? -0x80000000 : 0);
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

        private NodeCache<GenericLookupKey, ISymbolNode> _genericLookupHelpers;

        public ISymbolNode GenericLookupHelper(
            CORINFO_RUNTIME_LOOKUP_KIND runtimeLookupKind,
            ReadyToRunHelperId helperId,
            object helperArgument,
            GenericContext methodContext)
        {
            switch (helperId)
            {
                case ReadyToRunHelperId.TypeHandle:
                    return GenericLookupTypeHelper(
                        runtimeLookupKind,
                        ReadyToRunFixupKind.TypeHandle,
                        helperArgument,
                        methodContext);

                case ReadyToRunHelperId.MethodHandle:
                    return GenericLookupMethodHelper(
                        runtimeLookupKind,
                        ReadyToRunFixupKind.MethodHandle,
                        (MethodWithToken)helperArgument,
                        methodContext);

                case ReadyToRunHelperId.MethodEntry:
                    return GenericLookupMethodHelper(
                        runtimeLookupKind,
                        ReadyToRunFixupKind.MethodEntry,
                        (MethodWithToken)helperArgument,
                        methodContext);

                case ReadyToRunHelperId.MethodDictionary:
                    return GenericLookupMethodHelper(
                        runtimeLookupKind,
                        ReadyToRunFixupKind.MethodHandle,
                        (MethodWithToken)helperArgument,
                        methodContext);

                case ReadyToRunHelperId.TypeDictionary:
                    return GenericLookupTypeHelper(
                        runtimeLookupKind,
                        ReadyToRunFixupKind.TypeDictionary,
                        (TypeDesc)helperArgument,
                        methodContext);

                case ReadyToRunHelperId.VirtualDispatchCell:
                    return GenericLookupMethodHelper(
                        runtimeLookupKind,
                        ReadyToRunFixupKind.VirtualEntry,
                        (MethodWithToken)helperArgument,
                        methodContext);

                case ReadyToRunHelperId.FieldHandle:
                    return GenericLookupFieldHelper(
                        runtimeLookupKind,
                        ReadyToRunFixupKind.FieldHandle,
                        (FieldDesc)helperArgument,
                        methodContext);

                default:
                    throw new NotImplementedException(helperId.ToString());
            }
        }

        private ISymbolNode GenericLookupTypeHelper(
            CORINFO_RUNTIME_LOOKUP_KIND runtimeLookupKind,
            ReadyToRunFixupKind fixupKind,
            object helperArgument,
            GenericContext methodContext)
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
            return _genericLookupHelpers.GetOrAdd(key);
        }

        private ISymbolNode GenericLookupFieldHelper(
            CORINFO_RUNTIME_LOOKUP_KIND runtimeLookupKind,
            ReadyToRunFixupKind fixupKind,
            FieldDesc fieldArgument,
            GenericContext methodContext)
        {
            GenericLookupKey key = new GenericLookupKey(runtimeLookupKind, fixupKind, typeArgument: null, methodArgument: null, fieldArgument: fieldArgument, methodContext);
            return _genericLookupHelpers.GetOrAdd(key);
        }

        private ISymbolNode GenericLookupMethodHelper(
            CORINFO_RUNTIME_LOOKUP_KIND runtimeLookupKind,
            ReadyToRunFixupKind fixupKind,
            MethodWithToken methodArgument,
            GenericContext methodContext)
        {
            GenericLookupKey key = new GenericLookupKey(runtimeLookupKind, fixupKind, typeArgument: null, methodArgument, fieldArgument: null, methodContext);
            return _genericLookupHelpers.GetOrAdd(key);
        }

        private struct PInvokeTargetKey : IEquatable<PInvokeTargetKey>
        {
            public readonly MethodWithToken MethodWithToken;
            public readonly bool IsIndirect;

            public PInvokeTargetKey(MethodWithToken methodWithToken, bool isIndirect)
            {
                MethodWithToken = methodWithToken;
                IsIndirect = isIndirect;
            }

            public bool Equals(PInvokeTargetKey other)
            {
                return IsIndirect.Equals(other.IsIndirect) && MethodWithToken.Equals(other.MethodWithToken);
            }

            public override bool Equals(object obj)
            {
                return obj is PInvokeTargetKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return IsIndirect.GetHashCode() ^ (MethodWithToken.GetHashCode() * 23);
            }
        }

        private NodeCache<PInvokeTargetKey, ISymbolNode> _pInvokeTargetNodes = new NodeCache<PInvokeTargetKey, ISymbolNode>();

        public ISymbolNode GetIndirectPInvokeTargetNode(MethodWithToken methodWithToken)
        {
            return _pInvokeTargetNodes.GetOrAdd(new PInvokeTargetKey(methodWithToken, isIndirect: true));
        }

        public ISymbolNode GetPInvokeTargetNode(MethodWithToken methodWithToken)
        {
            return _pInvokeTargetNodes.GetOrAdd(new PInvokeTargetKey(methodWithToken, isIndirect: false));
        }
    }
}
