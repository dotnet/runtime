// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public partial class CompilerTypeSystemContext
    {
        private sealed class AsyncAwareVirtualMethodResolutionAlgorithm : MetadataVirtualMethodAlgorithm
        {
            private readonly CompilerTypeSystemContext _context;

            public AsyncAwareVirtualMethodResolutionAlgorithm(CompilerTypeSystemContext context)
                => _context = context;

            private MethodDesc DecomposeAsyncVariant(MethodDesc method, out bool isAsyncVariant)
            {
                isAsyncVariant = method.IsAsyncVariant();
                return isAsyncVariant ? _context.GetTargetOfAsyncVariantMethod(method) : method;
            }

            public override MethodDesc FindVirtualFunctionTargetMethodOnObjectType(MethodDesc targetMethod, TypeDesc objectType)
            {
                targetMethod = DecomposeAsyncVariant(targetMethod, out bool isAsyncSlot);
                MethodDesc result = base.FindVirtualFunctionTargetMethodOnObjectType(targetMethod, objectType);
                if (result != null && isAsyncSlot)
                    result = _context.GetAsyncVariantMethod(result);

                return result;
            }

            public override DefaultInterfaceMethodResolution ResolveInterfaceMethodToDefaultImplementationOnType(MethodDesc interfaceMethod, TypeDesc currentType, out MethodDesc impl)
            {
                interfaceMethod = DecomposeAsyncVariant(interfaceMethod, out bool isAsyncSlot);
                DefaultInterfaceMethodResolution result = base.ResolveInterfaceMethodToDefaultImplementationOnType(interfaceMethod, currentType, out impl);
                if (impl != null && isAsyncSlot)
                    impl = _context.GetAsyncVariantMethod(impl);

                return result;
            }

            public override MethodDesc ResolveInterfaceMethodToStaticVirtualMethodOnType(MethodDesc interfaceMethod, TypeDesc currentType)
            {
                interfaceMethod = DecomposeAsyncVariant(interfaceMethod, out bool isAsyncSlot);
                MethodDesc result = base.ResolveInterfaceMethodToStaticVirtualMethodOnType(interfaceMethod, currentType);
                if (result != null && isAsyncSlot)
                    result = _context.GetAsyncVariantMethod(result);

                return result;
            }
            public override MethodDesc ResolveInterfaceMethodToVirtualMethodOnType(MethodDesc interfaceMethod, TypeDesc currentType)
            {
                interfaceMethod = DecomposeAsyncVariant(interfaceMethod, out bool isAsyncSlot);
                MethodDesc result = base.ResolveInterfaceMethodToVirtualMethodOnType(interfaceMethod, currentType);
                if (result != null && isAsyncSlot)
                    result = _context.GetAsyncVariantMethod(result);

                return result;
            }
            public override DefaultInterfaceMethodResolution ResolveVariantInterfaceMethodToDefaultImplementationOnType(MethodDesc interfaceMethod, TypeDesc currentType, out MethodDesc impl)
            {
                interfaceMethod = DecomposeAsyncVariant(interfaceMethod, out bool isAsyncSlot);
                DefaultInterfaceMethodResolution result = base.ResolveVariantInterfaceMethodToDefaultImplementationOnType(interfaceMethod, currentType, out impl);
                if (impl != null && isAsyncSlot)
                    impl = _context.GetAsyncVariantMethod(impl);

                return result;
            }
            public override MethodDesc ResolveVariantInterfaceMethodToStaticVirtualMethodOnType(MethodDesc interfaceMethod, TypeDesc currentType)
            {
                interfaceMethod = DecomposeAsyncVariant(interfaceMethod, out bool isAsyncSlot);
                MethodDesc result = base.ResolveVariantInterfaceMethodToStaticVirtualMethodOnType(interfaceMethod, currentType);
                if (result != null && isAsyncSlot)
                    result = _context.GetAsyncVariantMethod(result);

                return result;
            }
            public override MethodDesc ResolveVariantInterfaceMethodToVirtualMethodOnType(MethodDesc interfaceMethod, TypeDesc currentType)
            {
                interfaceMethod = DecomposeAsyncVariant(interfaceMethod, out bool isAsyncSlot);
                MethodDesc result = base.ResolveVariantInterfaceMethodToVirtualMethodOnType(interfaceMethod, currentType);
                if (result != null && isAsyncSlot)
                    result = _context.GetAsyncVariantMethod(result);

                return result;
            }

            public override IEnumerable<MethodDesc> ComputeAllVirtualSlots(TypeDesc type)
            {
                foreach (MethodDesc method in base.ComputeAllVirtualSlots(type))
                {
                    yield return method;

                    // We create an async variant slot for any Task-returning method, not just runtime-async.
                    // This is not a problem in practice because the slot is still subject to dependency
                    // analysis and if not used, will not be generated.
                    //
                    // The reason why we need it is this:
                    //
                    // interface IFoo
                    // {
                    //     [RuntimeAsyncMethodGeneration(true)]
                    //     Task Method();
                    // }
                    //
                    // class Base
                    // {
                    //     [RuntimeAsyncMethodGeneration(false)]
                    //     public virtual Task Method();
                    // }
                    //
                    // class Derived : Base, IFoo
                    // {
                    //     // Q: The runtime-async implementation for IFoo.Method
                    //     //    comes from Base. However Base was not runtime-async and we
                    //     //    didn't know about IFoo in Base either. Who has the slot?
                    //     // A: Base has the runtime-async slot, despite the method not being runtime-async.
                    // }
                    if (method.GetTypicalMethodDefinition().Signature.ReturnsTaskOrValueTask())
                        yield return _context.GetAsyncVariantMethod(method);
                }
            }
        }

        public MethodDesc GetTargetOfAsyncVariantMethod(MethodDesc asyncVariantMethod)
        {
            var asyncMethodVariantDefinition = (AsyncMethodVariant)asyncVariantMethod.GetTypicalMethodDefinition();
            MethodDesc result = asyncMethodVariantDefinition.Target;

            // If there are generics involved, we need to specialize
            if (asyncVariantMethod != asyncMethodVariantDefinition)
            {
                TypeDesc owningType = asyncVariantMethod.OwningType;
                if (owningType != asyncMethodVariantDefinition.OwningType)
                    result = GetMethodForInstantiatedType(result, (InstantiatedType)owningType);

                if (asyncVariantMethod.HasInstantiation && !asyncVariantMethod.IsMethodDefinition)
                    result = GetInstantiatedMethod(result, asyncVariantMethod.Instantiation);
            }

            return result;
        }

        public MethodDesc GetAsyncVariantMethod(MethodDesc taskReturningMethod)
        {
            Debug.Assert(taskReturningMethod.Signature.ReturnsTaskOrValueTask());
            MethodDesc asyncMetadataMethodDef = taskReturningMethod.GetTypicalMethodDefinition();
            MethodDesc result = _asyncVariantHashtable.GetOrCreateValue((EcmaMethod)asyncMetadataMethodDef);

            if (asyncMetadataMethodDef != taskReturningMethod)
            {
                TypeDesc owningType = taskReturningMethod.OwningType;
                if (owningType.HasInstantiation)
                    result = GetMethodForInstantiatedType(result, (InstantiatedType)owningType);

                if (taskReturningMethod.HasInstantiation)
                    result = GetInstantiatedMethod(result, taskReturningMethod.Instantiation);
            }

            return result;
        }

        private sealed class AsyncVariantHashtable : LockFreeReaderHashtable<EcmaMethod, AsyncMethodVariant>
        {
            protected override int GetKeyHashCode(EcmaMethod key) => key.GetHashCode();
            protected override int GetValueHashCode(AsyncMethodVariant value) => value.Target.GetHashCode();
            protected override bool CompareKeyToValue(EcmaMethod key, AsyncMethodVariant value) => key == value.Target;
            protected override bool CompareValueToValue(AsyncMethodVariant value1, AsyncMethodVariant value2)
                => value1.Target == value2.Target;
            protected override AsyncMethodVariant CreateValueFromKey(EcmaMethod key) => new AsyncMethodVariant(key);
        }
        private AsyncVariantHashtable _asyncVariantHashtable = new AsyncVariantHashtable();

        public MetadataType GetContinuationType(GCPointerMap pointerMap)
        {
            return _continuationTypeHashtable.GetOrCreateValue(pointerMap);
        }

        private sealed class ContinuationTypeHashtable : LockFreeReaderHashtable<GCPointerMap, AsyncContinuationType>
        {
            private readonly CompilerTypeSystemContext _parent;
            private MetadataType _continuationType;

            public ContinuationTypeHashtable(CompilerTypeSystemContext parent)
                => _parent = parent;

            protected override int GetKeyHashCode(GCPointerMap key) => key.GetHashCode();
            protected override int GetValueHashCode(AsyncContinuationType value) => value.PointerMap.GetHashCode();
            protected override bool CompareKeyToValue(GCPointerMap key, AsyncContinuationType value) => key.Equals(value.PointerMap);
            protected override bool CompareValueToValue(AsyncContinuationType value1, AsyncContinuationType value2)
                => value1.PointerMap.Equals(value2.PointerMap);
            protected override AsyncContinuationType CreateValueFromKey(GCPointerMap key)
            {
                _continuationType ??= _parent.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "Continuation"u8);
                return new AsyncContinuationType(_continuationType, key);
            }
        }
        private ContinuationTypeHashtable _continuationTypeHashtable;
    }
}
