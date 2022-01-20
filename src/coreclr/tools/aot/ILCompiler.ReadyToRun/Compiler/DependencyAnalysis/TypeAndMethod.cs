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
        public readonly bool IsJumpableImportRequired;

        public TypeAndMethod(TypeDesc type, MethodWithToken method, bool isInstantiatingStub, bool isPrecodeImportRequired, bool isJumpableImportRequired)
        {
            Type = type;
            Method = method;
            IsInstantiatingStub = isInstantiatingStub;
            IsPrecodeImportRequired = isPrecodeImportRequired;
            IsJumpableImportRequired = isJumpableImportRequired;
        }

        public bool Equals(TypeAndMethod other)
        {
            return Type == other.Type &&
                   Method.Equals(other.Method) &&
                   IsInstantiatingStub == other.IsInstantiatingStub &&
                   IsPrecodeImportRequired == other.IsPrecodeImportRequired &&
                   IsJumpableImportRequired == other.IsJumpableImportRequired;
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
                (IsPrecodeImportRequired ? 0x20000000 : 0) ^
                (IsJumpableImportRequired ? 0x10000000 : 0);
        }
    }
}
