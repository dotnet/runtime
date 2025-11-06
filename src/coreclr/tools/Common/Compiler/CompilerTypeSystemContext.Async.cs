// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public partial class CompilerTypeSystemContext
    {
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
