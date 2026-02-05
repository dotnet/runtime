// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    internal static class CompilerTypeSystemContextAsyncExtensions
    {
        public static IEnumerable<MethodDesc> GetAllMethodsAndAsyncVariants(this TypeDesc type)
        {
            return CompilerTypeSystemContext.WithAsyncVariants((CompilerTypeSystemContext)type.Context, type.GetAllMethods());
        }
    }

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
                return WithAsyncVariants(_context, base.ComputeAllVirtualSlots(type));
            }
        }

        internal static IEnumerable<MethodDesc> WithAsyncVariants(CompilerTypeSystemContext context, IEnumerable<MethodDesc> methods)
        {
            foreach (MethodDesc method in methods)
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
                //
                // The other reason is that when the method is awaited, RyuJIT will prefer the AsyncCallable
                // variant, no matter if the method is async.
                //
                // We restrict this to EcmaMethod since AsyncVariantMethod cannot deal with non-ECMA methods
                // and we shouldn't be awaiting compiler-generated methods (delegate thunks, etc.) anyway.
                if (method.GetTypicalMethodDefinition() is EcmaMethod ecmaMethod
                    && ecmaMethod.Signature.ReturnsTaskOrValueTask())
                {
                    yield return context.GetAsyncVariantMethod(method);
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
            MethodDesc taskReturningMethodDefinition = taskReturningMethod.GetTypicalMethodDefinition();
            MethodDesc result = _asyncVariantHashtable.GetOrCreateValue((EcmaMethod)taskReturningMethodDefinition);

            if (taskReturningMethodDefinition != taskReturningMethod)
            {
                TypeDesc owningType = taskReturningMethod.OwningType;
                if (owningType != taskReturningMethodDefinition.OwningType)
                    result = GetMethodForInstantiatedType(result, (InstantiatedType)owningType);

                if (taskReturningMethod.HasInstantiation && !taskReturningMethod.IsMethodDefinition)
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

            public ContinuationTypeHashtable(CompilerTypeSystemContext parent)
                => _parent = parent;

            protected override int GetKeyHashCode(GCPointerMap key) => key.GetHashCode();
            protected override int GetValueHashCode(AsyncContinuationType value) => value.PointerMap.GetHashCode();
            protected override bool CompareKeyToValue(GCPointerMap key, AsyncContinuationType value) => key.Equals(value.PointerMap);
            protected override bool CompareValueToValue(AsyncContinuationType value1, AsyncContinuationType value2)
                => value1.PointerMap.Equals(value2.PointerMap);
            protected override AsyncContinuationType CreateValueFromKey(GCPointerMap key)
            {
                return new AsyncContinuationType(_parent.ContinuationType, key);
            }
        }
        private ContinuationTypeHashtable _continuationTypeHashtable;

        private MetadataType _continuationType;

        /// <summary>
        /// Gets the base type for async continuations.
        /// </summary>
        public MetadataType ContinuationType
        {
            get
            {
                return _continuationType ??= SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "Continuation"u8);
            }
        }
    }
}
