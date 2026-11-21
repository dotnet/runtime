// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;

namespace ILCompiler
{
    public partial class CompilerTypeSystemContext
    {
        private sealed class TypeMapGetValueStubHashtable : LockFreeReaderHashtable<(TypeDesc Target, TypeDesc ValueType), TypeMapGetValueStub>
        {
            protected override bool CompareKeyToValue((TypeDesc Target, TypeDesc ValueType) key, TypeMapGetValueStub value) => key.Target == value.Target && key.ValueType == value.ValueType;
            protected override bool CompareValueToValue(TypeMapGetValueStub value1, TypeMapGetValueStub value2) => value1.Target == value2.Target && value1.ValueType == value2.ValueType;
            protected override int GetKeyHashCode((TypeDesc Target, TypeDesc ValueType) key) => HashCode.Combine(key.Target.GetHashCode(), key.ValueType.GetHashCode());
            protected override int GetValueHashCode(TypeMapGetValueStub value) => HashCode.Combine(value.Target.GetHashCode(), value.ValueType.GetHashCode());
            protected override TypeMapGetValueStub CreateValueFromKey((TypeDesc Target, TypeDesc ValueType) key)
                => new TypeMapGetValueStub(((CompilerTypeSystemContext)key.Target.Context).GeneratedAssembly.GetGlobalModuleType(), key.Target, key.ValueType);
        }

        private readonly TypeMapGetValueStubHashtable _typeMapGetValueStubs = new TypeMapGetValueStubHashtable();

        /// <summary>
        /// Gets the stub that returns the value of ITypeMapValue&lt;<paramref name="valueType"/>&gt; of <paramref name="target"/>.
        /// </summary>
        internal MethodDesc GetTypeMapGetValueStub(TypeDesc target, TypeDesc valueType) => _typeMapGetValueStubs.GetOrCreateValue((target, valueType));
    }
}
