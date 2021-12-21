// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    public sealed class SignatureHelper
    {
        internal SignatureHelper()
        {
            // Prevent generating a default constructor
        }

        public void AddArgument(Type clsArgument)
        {
        }

        public void AddArgument(Type argument, bool pinned)
        {
        }

        public void AddArgument(Type argument, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers)
        {
        }

        public void AddArguments(Type[] arguments, Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers)
        {
        }

        public void AddSentinel()
        {
        }

        public override bool Equals(object? obj)
        {
            return default;
        }

        public static SignatureHelper GetFieldSigHelper(Module mod)
        {
            ReflectionEmitThrower.ThrowPlatformNotSupportedException();
            return default;
        }

        public override int GetHashCode()
        {
            return default;
        }

        public static SignatureHelper GetLocalVarSigHelper()
        {
            ReflectionEmitThrower.ThrowPlatformNotSupportedException();
            return default;
        }

        public static SignatureHelper GetLocalVarSigHelper(Module mod)
        {
            ReflectionEmitThrower.ThrowPlatformNotSupportedException();
            return default;
        }

        public static SignatureHelper GetMethodSigHelper(CallingConventions callingConvention, Type returnType)
        {
            ReflectionEmitThrower.ThrowPlatformNotSupportedException();
            return default;
        }

        public static SignatureHelper GetMethodSigHelper(Module mod, CallingConventions callingConvention, Type returnType)
        {
            ReflectionEmitThrower.ThrowPlatformNotSupportedException();
            return default;
        }

        public static SignatureHelper GetMethodSigHelper(Module mod, Type returnType, Type[] parameterTypes)
        {
            ReflectionEmitThrower.ThrowPlatformNotSupportedException();
            return default;
        }

        public static SignatureHelper GetPropertySigHelper(Module mod, CallingConventions callingConvention, Type returnType, Type[] requiredReturnTypeCustomModifiers, Type[] optionalReturnTypeCustomModifiers, Type[] parameterTypes, Type[][] requiredParameterTypeCustomModifiers, Type[][] optionalParameterTypeCustomModifiers)
        {
            ReflectionEmitThrower.ThrowPlatformNotSupportedException();
            return default;
        }

        public static SignatureHelper GetPropertySigHelper(Module mod, Type returnType, Type[] parameterTypes)
        {
            ReflectionEmitThrower.ThrowPlatformNotSupportedException();
            return default;
        }

        public static SignatureHelper GetPropertySigHelper(Module mod, Type returnType, Type[] requiredReturnTypeCustomModifiers, Type[] optionalReturnTypeCustomModifiers, Type[] parameterTypes, Type[][] requiredParameterTypeCustomModifiers, Type[][] optionalParameterTypeCustomModifiers)
        {
            ReflectionEmitThrower.ThrowPlatformNotSupportedException();
            return default;
        }

        public byte[] GetSignature()
        {
            return default;
        }

        public override string ToString()
        {
            return default;
        }
    }
}
