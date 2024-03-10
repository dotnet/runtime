// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.IL;
using Internal.TypeSystem;
using Internal.Text;
using ILCompiler.DependencyAnalysis;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    /// <summary>
    /// Captures information required to generate a ReadyToRun helper to create a delegate type instance
    /// pointing to a specific target method.
    /// </summary>
    public sealed class DelegateCreationInfo
    {
        private enum TargetKind
        {
            CanonicalEntrypoint,
            ExactCallableAddress,
            InterfaceDispatch,
            VTableLookup,
            MethodHandle,
            ConstrainedMethod,
        }

        private TargetKind _targetKind;

        private TypeDesc _constrainedType;
        private MethodDesc _targetMethod;

        /// <summary>
        /// Gets the node corresponding to the method that initializes the delegate.
        /// </summary>
        public IMethodNode Constructor
        {
            get;
        }

        public MethodDesc TargetMethod
        {
            get
            {
                Debug.Assert(_constrainedType == null);
                return _targetMethod;
            }
        }

        // The target method might be constrained if this was a "constrained ldftn" IL instruction.
        // The real target can be computed after resolving the constraint.
        public MethodDesc PossiblyUnresolvedTargetMethod
        {
            get
            {
                return _targetMethod;
            }
        }

        private bool TargetMethodIsUnboxingThunk
        {
            get
            {
                return TargetMethod.OwningType.IsValueType && !TargetMethod.Signature.IsStatic;
            }
        }

        public bool TargetNeedsVTableLookup => _targetKind == TargetKind.VTableLookup;

        public bool NeedsVirtualMethodUseTracking
        {
            get
            {
                return _targetKind == TargetKind.VTableLookup || _targetKind == TargetKind.InterfaceDispatch;
            }
        }

        public bool NeedsRuntimeLookup
        {
            get
            {
                switch (_targetKind)
                {
                    case TargetKind.VTableLookup:
                        return false;

                    case TargetKind.CanonicalEntrypoint:
                    case TargetKind.ExactCallableAddress:
                    case TargetKind.InterfaceDispatch:
                    case TargetKind.MethodHandle:
                        return TargetMethod.IsRuntimeDeterminedExactMethod;

                    case TargetKind.ConstrainedMethod:
                        Debug.Assert(_targetMethod.IsRuntimeDeterminedExactMethod || _constrainedType.IsRuntimeDeterminedSubtype);
                        return true;

                    default:
                        Debug.Assert(false);
                        return false;
                }
            }
        }

        // None of the data structures that support shared generics have been ported to the JIT
        // codebase which makes this a huge PITA. Not including the method for JIT since nobody
        // uses it in that mode anyway.
#if !SUPPORT_JIT
        public GenericLookupResult GetLookupKind(NodeFactory factory)
        {
            Debug.Assert(NeedsRuntimeLookup);
            switch (_targetKind)
            {
                case TargetKind.ExactCallableAddress:
                    return factory.GenericLookup.MethodEntry(TargetMethod, TargetMethodIsUnboxingThunk);

                case TargetKind.InterfaceDispatch:
                    return factory.GenericLookup.VirtualDispatchCell(TargetMethod);

                case TargetKind.MethodHandle:
                    return factory.GenericLookup.MethodHandle(TargetMethod);

                case TargetKind.ConstrainedMethod:
                    return factory.GenericLookup.ConstrainedMethodUse(_targetMethod, _constrainedType, directCall: !_targetMethod.HasInstantiation);

                default:
                    Debug.Assert(false);
                    return null;
            }
        }
#endif

        /// <summary>
        /// Gets the node representing the target method of the delegate if no runtime lookup is needed.
        /// </summary>
        public ISymbolNode GetTargetNode(NodeFactory factory)
        {
            Debug.Assert(!NeedsRuntimeLookup);
            switch (_targetKind)
            {
                case TargetKind.CanonicalEntrypoint:
                    return factory.CanonicalEntrypoint(TargetMethod, TargetMethodIsUnboxingThunk);

                case TargetKind.ExactCallableAddress:
                    return factory.ExactCallableAddress(TargetMethod, TargetMethodIsUnboxingThunk);

                case TargetKind.InterfaceDispatch:
                    return factory.InterfaceDispatchCell(TargetMethod);

                case TargetKind.MethodHandle:
                    return factory.RuntimeMethodHandle(TargetMethod);

                case TargetKind.VTableLookup:
                    Debug.Fail("Need to do runtime lookup");
                    return null;

                default:
                    Debug.Assert(false);
                    return null;
            }
        }

        /// <summary>
        /// Gets an optional node passed as an additional argument to the constructor.
        /// </summary>
        public IMethodNode Thunk
        {
            get;
        }

        public TypeDesc DelegateType
        {
            get;
        }

        private DelegateCreationInfo(TypeDesc delegateType, IMethodNode constructor, MethodDesc targetMethod, TypeDesc constrainedType, TargetKind targetKind, IMethodNode thunk = null)
        {
            Debug.Assert(targetKind != TargetKind.VTableLookup
                || MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(targetMethod) == targetMethod);
            DelegateType = delegateType;
            Constructor = constructor;
            _targetMethod = targetMethod;
            _constrainedType = constrainedType;
            _targetKind = targetKind;
            Thunk = thunk;
        }

        /// <summary>
        /// Constructs a new instance of <see cref="DelegateCreationInfo"/> set up to construct a delegate of type
        /// '<paramref name="delegateType"/>' pointing to '<paramref name="targetMethod"/>'.
        /// </summary>
        public static DelegateCreationInfo Create(TypeDesc delegateType, MethodDesc targetMethod, TypeDesc constrainedType, NodeFactory factory, bool followVirtualDispatch)
        {
            CompilerTypeSystemContext context = factory.TypeSystemContext;
            DefType systemDelegate = context.GetWellKnownType(WellKnownType.MulticastDelegate).BaseType;

            int paramCountTargetMethod = targetMethod.Signature.Length;
            if (!targetMethod.Signature.IsStatic)
            {
                paramCountTargetMethod++;
            }

            DelegateInfo delegateInfo = context.GetDelegateInfo(delegateType.GetTypeDefinition());
            int paramCountDelegateClosed = delegateInfo.Signature.Length + 1;
            bool closed = false;
            if (paramCountDelegateClosed == paramCountTargetMethod)
            {
                closed = true;
            }
            else
            {
                Debug.Assert(paramCountDelegateClosed == paramCountTargetMethod + 1);
            }

            if (targetMethod.Signature.IsStatic)
            {
                MethodDesc invokeThunk;
                MethodDesc initMethod;

                if (!closed)
                {
                    initMethod = systemDelegate.GetKnownMethod("InitializeOpenStaticThunk", null);
                    invokeThunk = delegateInfo.Thunks[DelegateThunkKind.OpenStaticThunk];
                }
                else
                {
                    // Closed delegate to a static method (i.e. delegate to an extension method that locks the first parameter)
                    invokeThunk = delegateInfo.Thunks[DelegateThunkKind.ClosedStaticThunk];
                    initMethod = systemDelegate.GetKnownMethod("InitializeClosedStaticThunk", null);
                }

                var instantiatedDelegateType = delegateType as InstantiatedType;
                if (instantiatedDelegateType != null)
                    invokeThunk = context.GetMethodForInstantiatedType(invokeThunk, instantiatedDelegateType);

                return new DelegateCreationInfo(
                    delegateType,
                    factory.MethodEntrypoint(initMethod),
                    targetMethod,
                    constrainedType,
                    constrainedType == null ? TargetKind.ExactCallableAddress : TargetKind.ConstrainedMethod,
                    factory.MethodEntrypoint(invokeThunk));
            }
            else
            {
                if (!closed)
                    throw new NotImplementedException("Open instance delegates");

                string initializeMethodName = "InitializeClosedInstance";
                MethodDesc targetCanonMethod = targetMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);
                TargetKind kind;
                if (targetMethod.HasInstantiation)
                {
                    if (followVirtualDispatch && targetMethod.IsVirtual)
                    {
                        initializeMethodName = "InitializeClosedInstanceWithGVMResolution";
                        kind = TargetKind.MethodHandle;
                    }
                    else
                    {
                        if (targetMethod != targetCanonMethod)
                        {
                            // Closed delegates to generic instance methods need to be constructed through a slow helper that
                            // checks for the fat function pointer case (function pointer + instantiation argument in a single
                            // pointer) and injects an invocation thunk to unwrap the fat function pointer as part of
                            // the invocation if necessary.
                            initializeMethodName = "InitializeClosedInstanceSlow";
                        }

                        kind = TargetKind.ExactCallableAddress;
                    }
                }
                else
                {
                    if (followVirtualDispatch && targetMethod.IsVirtual)
                    {
                        if (targetMethod.OwningType.IsInterface)
                        {
                            kind = TargetKind.InterfaceDispatch;
                            initializeMethodName = "InitializeClosedInstanceToInterface";
                        }
                        else
                        {
                            kind = TargetKind.VTableLookup;
                            targetMethod = targetMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);
                        }
                    }
                    else
                    {
                        kind = TargetKind.CanonicalEntrypoint;
                        targetMethod = targetMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);
                    }
                }

                Debug.Assert(constrainedType == null);
                return new DelegateCreationInfo(
                    delegateType,
                    factory.MethodEntrypoint(systemDelegate.GetKnownMethod(initializeMethodName, null)),
                    targetMethod,
                    constrainedType,
                    kind);
            }
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("__DelegateCtor_");
            if (TargetNeedsVTableLookup)
                sb.Append("FromVtbl_");
            Constructor.AppendMangledName(nameMangler, sb);
            sb.Append("__");
            sb.Append(nameMangler.GetMangledMethodName(_targetMethod));
            if (_constrainedType != null)
            {
                sb.Append("__");
                nameMangler.GetMangledTypeName(_constrainedType);
            }
            if (Thunk != null)
            {
                sb.Append("__");
                Thunk.AppendMangledName(nameMangler, sb);
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj as DelegateCreationInfo;
            return other != null
                && Constructor == other.Constructor
                && _targetMethod == other._targetMethod
                && _constrainedType == other._constrainedType
                && _targetKind == other._targetKind
                && Thunk == other.Thunk;
        }

        public override int GetHashCode()
        {
            return Constructor.GetHashCode() ^ _targetMethod.GetHashCode();
        }

#if !SUPPORT_JIT
        internal int CompareTo(DelegateCreationInfo other, TypeSystemComparer comparer)
        {
            var compare = _targetKind - other._targetKind;
            if (compare != 0)
                return compare;

            compare = comparer.Compare(TargetMethod, other.TargetMethod);
            if (compare != 0)
                return compare;

            compare = comparer.Compare(Constructor.Method, other.Constructor.Method);
            if (compare != 0)
                return compare;

            if (_constrainedType != null && other._constrainedType != null)
            {
                compare = comparer.Compare(_constrainedType, other._constrainedType);
                if (compare != 0)
                    return compare;
            }
            else
            {
                if (_constrainedType != null)
                    return 1;
                if (other._constrainedType != null)
                    return -1;
            }

            if (Thunk == other.Thunk)
                return 0;

            if (Thunk == null)
                return -1;

            if (other.Thunk == null)
                return 1;

            return comparer.Compare(Thunk.Method, other.Thunk.Method);
        }
#endif
    }
}
