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

            private MethodDesc WrapAsAsyncVariant(MethodDesc originalMethod, MethodDesc resolvedMethod)
            {
                Debug.Assert(originalMethod.Signature.ReturnsTaskOrValueTask());
                Debug.Assert(resolvedMethod.Signature.ReturnsTaskOrValueTask());

                MethodDesc asyncVariant = _context.GetAsyncVariantMethod(resolvedMethod);

                // Check if the slot is void-returning but the resolved
                // async variant is T-returning (base has Task, derived has Task<T>).
                if (!originalMethod.Signature.ReturnType.HasInstantiation
                    && !asyncVariant.Signature.ReturnType.IsVoid)
                {
                    asyncVariant = _context.GetReturnDroppingAsyncVariantMethod(asyncVariant);
                }

                return asyncVariant;
            }

            public override MethodDesc FindVirtualFunctionTargetMethodOnObjectType(MethodDesc targetMethod, TypeDesc objectType)
            {
                targetMethod = DecomposeAsyncVariant(targetMethod, out bool isAsyncSlot);
                MethodDesc result = base.FindVirtualFunctionTargetMethodOnObjectType(targetMethod, objectType);
                if (result != null && isAsyncSlot)
                    result = WrapAsAsyncVariant(targetMethod, result);

                return result;
            }

            public override DefaultInterfaceMethodResolution ResolveInterfaceMethodToDefaultImplementationOnType(MethodDesc interfaceMethod, TypeDesc currentType, out MethodDesc impl)
            {
                interfaceMethod = DecomposeAsyncVariant(interfaceMethod, out bool isAsyncSlot);
                DefaultInterfaceMethodResolution result = base.ResolveInterfaceMethodToDefaultImplementationOnType(interfaceMethod, currentType, out impl);
                if (impl != null && isAsyncSlot)
                    impl = WrapAsAsyncVariant(interfaceMethod, impl);

                return result;
            }

            public override MethodDesc ResolveInterfaceMethodToStaticVirtualMethodOnType(MethodDesc interfaceMethod, TypeDesc currentType)
            {
                interfaceMethod = DecomposeAsyncVariant(interfaceMethod, out bool isAsyncSlot);
                MethodDesc result = base.ResolveInterfaceMethodToStaticVirtualMethodOnType(interfaceMethod, currentType);
                if (result != null && isAsyncSlot)
                    result = WrapAsAsyncVariant(interfaceMethod, result);

                return result;
            }
            public override MethodDesc ResolveInterfaceMethodToVirtualMethodOnType(MethodDesc interfaceMethod, TypeDesc currentType)
            {
                interfaceMethod = DecomposeAsyncVariant(interfaceMethod, out bool isAsyncSlot);
                MethodDesc result = base.ResolveInterfaceMethodToVirtualMethodOnType(interfaceMethod, currentType);
                if (result != null && isAsyncSlot)
                    result = WrapAsAsyncVariant(interfaceMethod, result);

                return result;
            }
            public override DefaultInterfaceMethodResolution ResolveVariantInterfaceMethodToDefaultImplementationOnType(MethodDesc interfaceMethod, TypeDesc currentType, out MethodDesc impl)
            {
                interfaceMethod = DecomposeAsyncVariant(interfaceMethod, out bool isAsyncSlot);
                DefaultInterfaceMethodResolution result = base.ResolveVariantInterfaceMethodToDefaultImplementationOnType(interfaceMethod, currentType, out impl);
                if (impl != null && isAsyncSlot)
                    impl = WrapAsAsyncVariant(interfaceMethod, impl);

                return result;
            }
            public override MethodDesc ResolveVariantInterfaceMethodToStaticVirtualMethodOnType(MethodDesc interfaceMethod, TypeDesc currentType)
            {
                interfaceMethod = DecomposeAsyncVariant(interfaceMethod, out bool isAsyncSlot);
                MethodDesc result = base.ResolveVariantInterfaceMethodToStaticVirtualMethodOnType(interfaceMethod, currentType);
                if (result != null && isAsyncSlot)
                    result = WrapAsAsyncVariant(interfaceMethod, result);

                return result;
            }
            public override MethodDesc ResolveVariantInterfaceMethodToVirtualMethodOnType(MethodDesc interfaceMethod, TypeDesc currentType)
            {
                interfaceMethod = DecomposeAsyncVariant(interfaceMethod, out bool isAsyncSlot);
                MethodDesc result = base.ResolveVariantInterfaceMethodToVirtualMethodOnType(interfaceMethod, currentType);
                if (result != null && isAsyncSlot)
                    result = WrapAsAsyncVariant(interfaceMethod, result);

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

        public MethodDesc GetReturnDroppingAsyncVariantMethod(MethodDesc asyncVariantMethod)
        {
            Debug.Assert(asyncVariantMethod.IsAsyncVariant());
            Debug.Assert(!asyncVariantMethod.Signature.ReturnType.IsVoid);

            MethodDesc typicalMethodDefinition = asyncVariantMethod.GetTypicalMethodDefinition();
            MethodDesc result = _returnDroppingHashtable.GetOrCreateValue((AsyncMethodVariant)typicalMethodDefinition);

            if (typicalMethodDefinition != asyncVariantMethod)
            {
                TypeDesc owningType = asyncVariantMethod.OwningType;
                if (owningType != typicalMethodDefinition.OwningType)
                    result = GetMethodForInstantiatedType(result, (InstantiatedType)owningType);

                if (asyncVariantMethod.HasInstantiation && !asyncVariantMethod.IsMethodDefinition)
                    result = GetInstantiatedMethod(result, asyncVariantMethod.Instantiation);
            }

            return result;
        }

        private sealed class ReturnDroppingHashtable : LockFreeReaderHashtable<AsyncMethodVariant, ReturnDroppingAsyncThunk>
        {
            protected override int GetKeyHashCode(AsyncMethodVariant key) => key.GetHashCode();
            protected override int GetValueHashCode(ReturnDroppingAsyncThunk value) => value.AsyncVariantTarget.GetHashCode();
            protected override bool CompareKeyToValue(AsyncMethodVariant key, ReturnDroppingAsyncThunk value) => key == value.AsyncVariantTarget;
            protected override bool CompareValueToValue(ReturnDroppingAsyncThunk value1, ReturnDroppingAsyncThunk value2)
                => value1.AsyncVariantTarget == value2.AsyncVariantTarget;
            protected override ReturnDroppingAsyncThunk CreateValueFromKey(AsyncMethodVariant key) => new ReturnDroppingAsyncThunk(key);
        }
        private ReturnDroppingHashtable _returnDroppingHashtable = new ReturnDroppingHashtable();

        public AsyncResumptionStub GetAsyncResumptionStub(MethodDesc targetMethod, TypeDesc owningType)
        {
            return _asyncResumptionStubHashtable.GetOrCreateValue(new AsyncResumptionStubKey(targetMethod, owningType));
        }

        private readonly struct AsyncResumptionStubKey : System.IEquatable<AsyncResumptionStubKey>
        {
            public readonly MethodDesc TargetMethod;
            public readonly TypeDesc OwningType;

            public AsyncResumptionStubKey(MethodDesc targetMethod, TypeDesc owningType)
            {
                TargetMethod = targetMethod;
                OwningType = owningType;
            }

            public bool Equals(AsyncResumptionStubKey other)
                => TargetMethod == other.TargetMethod;

            public override bool Equals(object obj)
                => obj is AsyncResumptionStubKey other && Equals(other);

            public override int GetHashCode()
                => TargetMethod.GetHashCode();
        }

        private sealed class AsyncResumptionStubHashtable : LockFreeReaderHashtable<AsyncResumptionStubKey, AsyncResumptionStub>
        {
            protected override int GetKeyHashCode(AsyncResumptionStubKey key) => key.GetHashCode();
            protected override int GetValueHashCode(AsyncResumptionStub value) => value.TargetMethod.GetHashCode();
            protected override bool CompareKeyToValue(AsyncResumptionStubKey key, AsyncResumptionStub value)
                => key.TargetMethod == value.TargetMethod;
            protected override bool CompareValueToValue(AsyncResumptionStub value1, AsyncResumptionStub value2)
                => value1.TargetMethod == value2.TargetMethod;
            protected override AsyncResumptionStub CreateValueFromKey(AsyncResumptionStubKey key)
                => new AsyncResumptionStub(key.TargetMethod, key.OwningType);
        }
        private AsyncResumptionStubHashtable _asyncResumptionStubHashtable = new AsyncResumptionStubHashtable();

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
                var cont = new AsyncContinuationType(_parent.ContinuationType, key);
                // Short circuit loadability checks for this type
                _parent._validTypes.TryAdd(cont);
                return cont;
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
