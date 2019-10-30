// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public readonly bool IsUnboxingStub;
        public readonly bool IsInstantiatingStub;
        public readonly bool IsPrecodeImportRequired;
        public readonly SignatureContext SignatureContext;

        public TypeAndMethod(TypeDesc type, MethodWithToken method, bool isUnboxingStub, bool isInstantiatingStub, bool isPrecodeImportRequired, SignatureContext signatureContext)
        {
            Type = type;
            Method = method;
            IsUnboxingStub = isUnboxingStub;
            IsInstantiatingStub = isInstantiatingStub;
            IsPrecodeImportRequired = isPrecodeImportRequired;
            SignatureContext = signatureContext;
        }

        public bool Equals(TypeAndMethod other)
        {
            return Type == other.Type &&
                   Method.Equals(other.Method) &&
                   IsUnboxingStub == other.IsUnboxingStub &&
                   IsInstantiatingStub == other.IsInstantiatingStub &&
                   IsPrecodeImportRequired == other.IsPrecodeImportRequired &&
                   SignatureContext.Equals(other.SignatureContext);
        }

        public override bool Equals(object obj)
        {
            return obj is TypeAndMethod other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Type?.GetHashCode() ?? 0) ^ 
                unchecked(Method.GetHashCode() * 31) ^ 
                (IsUnboxingStub ? -0x80000000 : 0) ^ 
                (IsInstantiatingStub ? 0x40000000 : 0) ^
                (IsPrecodeImportRequired ? 0x20000000 : 0) ^
                (SignatureContext.GetHashCode() * 23);
        }
    }
}
