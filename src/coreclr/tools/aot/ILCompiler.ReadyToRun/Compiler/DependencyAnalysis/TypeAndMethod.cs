// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.JitInterface;
using Internal.TypeSystem;

using ILCompiler.DependencyAnalysis.ReadyToRun;

namespace ILCompiler.DependencyAnalysis
{
    internal struct TypeAndMethod : IEquatable<TypeAndMethod>
    {
        public readonly TypeDesc Type;
        public readonly MethodWithToken Method;
        public readonly bool IsInstantiatingStub;
        public readonly bool IsPrecodeImportRequired;

        public TypeAndMethod(TypeDesc type, MethodWithToken method, bool isInstantiatingStub, bool isPrecodeImportRequired)
        {
            Type = type;
            Method = method;
            IsInstantiatingStub = isInstantiatingStub;
            IsPrecodeImportRequired = isPrecodeImportRequired;
        }

        public bool Equals(TypeAndMethod other)
        {
            return Type == other.Type &&
                   Method.Equals(other.Method) &&
                   IsInstantiatingStub == other.IsInstantiatingStub &&
                   IsPrecodeImportRequired == other.IsPrecodeImportRequired;
        }

        public override bool Equals(object obj)
        {
            return obj is TypeAndMethod other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Type?.GetHashCode() ?? 0) ^
                unchecked(Method.GetHashCode() * 31) ^
                (IsInstantiatingStub ? 0x40000000 : 0) ^
                (IsPrecodeImportRequired ? 0x20000000 : 0);
        }
    }
}
