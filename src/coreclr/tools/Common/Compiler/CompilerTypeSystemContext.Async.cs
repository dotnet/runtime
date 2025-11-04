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
            MethodDesc result = _asyncVariantImplHashtable.GetOrCreateValue((EcmaMethod)asyncMetadataMethodDef);

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

        private sealed class AsyncVariantImplHashtable : LockFreeReaderHashtable<EcmaMethod, AsyncMethodVariant>
        {
            protected override int GetKeyHashCode(EcmaMethod key) => key.GetHashCode();
            protected override int GetValueHashCode(AsyncMethodVariant value) => value.Target.GetHashCode();
            protected override bool CompareKeyToValue(EcmaMethod key, AsyncMethodVariant value) => key == value.Target;
            protected override bool CompareValueToValue(AsyncMethodVariant value1, AsyncMethodVariant value2)
                => value1.Target == value2.Target;
            protected override AsyncMethodVariant CreateValueFromKey(EcmaMethod key) => new AsyncMethodVariant(key);
        }
        private AsyncVariantImplHashtable _asyncVariantImplHashtable = new AsyncVariantImplHashtable();
    }
}
