// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.IL.Stubs;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;
using Interlocked = System.Threading.Interlocked;

namespace Internal.IL
{
    /// <summary>
    /// Represents a delegate and provides access to compiler-generated methods on the delegate type.
    /// </summary>
    public class DelegateInfo
    {
        private readonly TypeDesc _delegateType;
        private readonly DelegateFeature _supportedFeatures;

        private MethodSignature _signature;

        private MethodDesc _getThunkMethod;
        private DelegateThunkCollection _thunks;

        /// <summary>
        /// Gets the synthetic methods that support this delegate type.
        /// </summary>
        public IEnumerable<MethodDesc> Methods
        {
            get
            {
                if (_getThunkMethod == null)
                {
                    Interlocked.CompareExchange(ref _getThunkMethod, new DelegateGetThunkMethodOverride(this), null);
                }

                yield return _getThunkMethod;

                DelegateThunkCollection thunks = Thunks;
                for (DelegateThunkKind kind = 0; kind < DelegateThunkCollection.MaxThunkKind; kind++)
                {
                    MethodDesc thunk = thunks[kind];
                    if (thunk != null)
                        yield return thunk;
                }
            }
        }

        /// <summary>
        /// Gets the collection of delegate invocation thunks.
        /// </summary>
        public DelegateThunkCollection Thunks
        {
            get
            {
                if (_thunks == null)
                {
                    Interlocked.CompareExchange(ref _thunks, new DelegateThunkCollection(this), null);
                }
                return _thunks;
            }
        }

        /// <summary>
        /// Gets the signature of the delegate type.
        /// </summary>
        public MethodSignature Signature
        {
            get
            {
                _signature ??= _delegateType.GetKnownMethod("Invoke", null).Signature;
                return _signature;
            }
        }

        public DelegateFeature SupportedFeatures
        {
            get
            {
                return _supportedFeatures;
            }
        }

        /// <summary>
        /// Gets the type of the delegate.
        /// </summary>
        public TypeDesc Type
        {
            get
            {
                return _delegateType;
            }
        }

        public DelegateInfo(TypeDesc delegateType, DelegateFeature features)
        {
            Debug.Assert(delegateType.IsDelegate);
            Debug.Assert(delegateType.IsTypeDefinition);

            _delegateType = delegateType;
            _supportedFeatures = features;
        }
    }

    /// <summary>
    /// Represents a collection of delegate invocation thunks.
    /// </summary>
    public class DelegateThunkCollection
    {
        public const DelegateThunkKind MaxThunkKind = DelegateThunkKind.ObjectArrayThunk + 1;

        private MethodDesc _openStaticThunk;
        private MethodDesc _multicastThunk;
        private MethodDesc _closedStaticThunk;
        private MethodDesc _closedInstanceOverGeneric;
        private MethodDesc _invokeObjectArrayThunk;
        private MethodDesc _openInstanceThunk;

        internal DelegateThunkCollection(DelegateInfo owningDelegate)
        {
            _openStaticThunk = new DelegateInvokeOpenStaticThunk(owningDelegate);
            _multicastThunk = new DelegateInvokeMulticastThunk(owningDelegate);
            _closedStaticThunk = new DelegateInvokeClosedStaticThunk(owningDelegate);
            _closedInstanceOverGeneric = new DelegateInvokeInstanceClosedOverGenericMethodThunk(owningDelegate);

            // Methods that have a byref-like type in the signature cannot be invoked with the object array thunk.
            // We would need to box the parameter and these can't be boxed.
            // Neither can be methods that have pointers in the signature.
            MethodSignature delegateSignature = owningDelegate.Signature;
            bool generateObjectArrayThunk = true;
            for (int i = 0; i < delegateSignature.Length; i++)
            {
                TypeDesc paramType = delegateSignature[i];
                if (paramType.IsByRef)
                    paramType = ((ByRefType)paramType).ParameterType;
                if (!paramType.IsSignatureVariable && paramType.IsByRefLike)
                {
                    generateObjectArrayThunk = false;
                    break;
                }
                if (paramType.IsPointer || paramType.IsFunctionPointer)
                {
                    generateObjectArrayThunk = false;
                    break;
                }
            }
            TypeDesc returnType = delegateSignature.ReturnType;
            if (returnType.IsByRef)
                generateObjectArrayThunk = false;
            if (!returnType.IsSignatureVariable && returnType.IsByRefLike)
                generateObjectArrayThunk = false;
            if (returnType.IsPointer || returnType.IsFunctionPointer)
                generateObjectArrayThunk = false;

            if ((owningDelegate.SupportedFeatures & DelegateFeature.ObjectArrayThunk) != 0 && generateObjectArrayThunk)
                _invokeObjectArrayThunk = new DelegateInvokeObjectArrayThunk(owningDelegate);

            //
            // Check whether we have an open instance thunk
            //

            if ((owningDelegate.SupportedFeatures & DelegateFeature.OpenInstanceThunk) != 0 && delegateSignature.Length > 0)
            {
                TypeDesc firstParam = delegateSignature[0];

                bool generateOpenInstanceMethod;

                switch (firstParam.Category)
                {
                    case TypeFlags.Pointer:
                    case TypeFlags.FunctionPointer:
                        generateOpenInstanceMethod = false;
                        break;

                    case TypeFlags.ByRef:
                        firstParam = ((ByRefType)firstParam).ParameterType;
                        generateOpenInstanceMethod = firstParam.IsSignatureVariable || firstParam.IsValueType;
                        break;

                    case TypeFlags.Array:
                    case TypeFlags.SzArray:
                    case TypeFlags.SignatureTypeVariable:
                        generateOpenInstanceMethod = true;
                        break;

                    default:
                        Debug.Assert(firstParam.IsDefType);
                        generateOpenInstanceMethod = !firstParam.IsValueType;
                        break;
                }

                if (generateOpenInstanceMethod)
                {
                    _openInstanceThunk = new DelegateInvokeOpenInstanceThunk(owningDelegate);
                }
            }
        }

        public MethodDesc this[DelegateThunkKind kind]
        {
            get
            {
                switch (kind)
                {
                    case DelegateThunkKind.OpenStaticThunk:
                        return _openStaticThunk;
                    case DelegateThunkKind.MulticastThunk:
                        return _multicastThunk;
                    case DelegateThunkKind.ClosedStaticThunk:
                        return _closedStaticThunk;
                    case DelegateThunkKind.ClosedInstanceThunkOverGenericMethod:
                        return _closedInstanceOverGeneric;
                    case DelegateThunkKind.ObjectArrayThunk:
                        return _invokeObjectArrayThunk;
                    case DelegateThunkKind.OpenInstanceThunk:
                        return _openInstanceThunk;
                    default:
                        return null;
                }
            }
        }
    }

    // TODO: Unify with the consts used in Delegate.cs within the class library.
    public enum DelegateThunkKind
    {
        MulticastThunk = 0,
        ClosedStaticThunk = 1,
        OpenStaticThunk = 2,
        ClosedInstanceThunkOverGenericMethod = 3, // This may not exist
        OpenInstanceThunk = 4,        // This may not exist
        ObjectArrayThunk = 5,         // This may not exist
    }

    [Flags]
    public enum DelegateFeature
    {
        ObjectArrayThunk = 0x1,
        OpenInstanceThunk = 0x2,

        All = 0x3,
    }
}
