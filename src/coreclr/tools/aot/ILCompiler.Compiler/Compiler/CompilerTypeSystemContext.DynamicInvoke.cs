// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.IL.Stubs;
using Internal.TypeSystem;

namespace ILCompiler
{
    public partial class CompilerTypeSystemContext
    {
        private sealed class DynamicInvokeThunkHashtable : LockFreeReaderHashtable<MethodSignature, DynamicInvokeMethodThunk>
        {
            protected override bool CompareKeyToValue(MethodSignature key, DynamicInvokeMethodThunk value) => key.Equals(value.TargetSignature);
            protected override bool CompareValueToValue(DynamicInvokeMethodThunk value1, DynamicInvokeMethodThunk value2) => value1.TargetSignature.Equals(value2.TargetSignature) && value1.OwningType == value2.OwningType;
            protected override int GetKeyHashCode(MethodSignature key) => key.GetHashCode();
            protected override int GetValueHashCode(DynamicInvokeMethodThunk value) => value.TargetSignature.GetHashCode();
            protected override DynamicInvokeMethodThunk CreateValueFromKey(MethodSignature key)
            {
                return new DynamicInvokeMethodThunk(((CompilerTypeSystemContext)key.Context).GeneratedAssembly.GetGlobalModuleType(), key);
            }
        }

        private DynamicInvokeThunkHashtable _dynamicInvokeThunks = new DynamicInvokeThunkHashtable();

        public MethodDesc GetDynamicInvokeThunk(MethodSignature signature, bool valueTypeInstanceMethod)
        {
            return _dynamicInvokeThunks.GetOrCreateValue(
                DynamicInvokeMethodThunk.NormalizeSignature(signature, valueTypeInstanceMethod));
        }
    }
}
