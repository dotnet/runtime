// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.IL.Stubs;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    partial class CompilerTypeSystemContext
    {
        private class DynamicInvokeThunkHashtable : LockFreeReaderHashtable<DynamicInvokeMethodSignature, DynamicInvokeMethodThunk>
        {
            protected override bool CompareKeyToValue(DynamicInvokeMethodSignature key, DynamicInvokeMethodThunk value) => key.Equals(value.TargetSignature);
            protected override bool CompareValueToValue(DynamicInvokeMethodThunk value1, DynamicInvokeMethodThunk value2) => value1.TargetSignature.Equals(value2.TargetSignature) && value1.OwningType == value2.OwningType;
            protected override int GetKeyHashCode(DynamicInvokeMethodSignature key) => key.GetHashCode();
            protected override int GetValueHashCode(DynamicInvokeMethodThunk value) => value.TargetSignature.GetHashCode();
            protected override DynamicInvokeMethodThunk CreateValueFromKey(DynamicInvokeMethodSignature key)
            {
                return new DynamicInvokeMethodThunk(((CompilerTypeSystemContext)key.Context).GeneratedAssembly.GetGlobalModuleType(), key);
            }
        }
        DynamicInvokeThunkHashtable _dynamicInvokeThunks = new DynamicInvokeThunkHashtable();

        public MethodDesc GetDynamicInvokeThunk(DynamicInvokeMethodSignature signature)
        {
            return _dynamicInvokeThunks.GetOrCreateValue(signature);
        }
    }
}
