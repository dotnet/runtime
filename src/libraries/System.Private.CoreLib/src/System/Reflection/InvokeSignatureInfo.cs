// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Reflection.Emit
{
    internal sealed class InvokeSignatureInfo
    {
        private readonly Type _declaringType;
        private readonly Type[] _parameterTypes;
        private readonly Type _returnType;
        private readonly bool _isStatic;

        public static InvokeSignatureInfo Create(MethodBase method, Type[] parameterTypes)
        {
            if (method is RuntimeMethodInfo rmi)
            {
                return new InvokeSignatureInfo(declaringType: rmi.DeclaringType!, returnType: rmi.ReturnType, parameterTypes, rmi.IsStatic);
            }

            if (method is RuntimeConstructorInfo rci)
            {
                Debug.Assert(rci.GetReturnType() == typeof(void));
                Debug.Assert(!rci.IsStatic);

                return new InvokeSignatureInfo(declaringType: rci.DeclaringType!, returnType: typeof(void), parameterTypes, isStatic: false);
            }

            DynamicMethod dm = (DynamicMethod)method;
            return new InvokeSignatureInfo(declaringType: dm.DeclaringType!, returnType: dm.ReturnType, parameterTypes, dm.IsStatic);
        }

        public static InvokeSignatureInfo Create(in NormalizedLookupKey normalizedLookupKey)
        {
            return new InvokeSignatureInfo(
                declaringType: normalizedLookupKey._declaringType,
                returnType: normalizedLookupKey._returnType,
                parameterTypes: normalizedLookupKey._parameterTypes,
                isStatic: normalizedLookupKey._isStatic);
        }

        public InvokeSignatureInfo(Type declaringType, Type returnType, Type[] parameterTypes, bool isStatic)
        {
            _declaringType = isStatic ? typeof(void) : declaringType;
            _returnType = returnType;
            _parameterTypes = parameterTypes;
            _isStatic = isStatic;
        }

        public Type DeclaringType => _declaringType;
        public bool IsStatic => _isStatic;
        public ReadOnlySpan<Type> ParameterTypes => _parameterTypes;
        public Type ReturnType => _returnType;

        // Must be the same as NormalizedLookupKey.Comparer.Equals().
        public override bool Equals(object? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is not InvokeSignatureInfo otherSig)
            {
                return false;
            }

            if (!ReferenceEquals(_declaringType, otherSig._declaringType) ||
                !ReferenceEquals(_returnType, otherSig._returnType) ||
                _isStatic != otherSig._isStatic ||
                _parameterTypes.Length != otherSig._parameterTypes.Length)
            {
                return false;
            }

            for (int i = 0; i < _parameterTypes.Length; i++)
            {
                if (!ReferenceEquals(_parameterTypes[i], otherSig._parameterTypes[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode() => GetHashCode(
            declaringType: _declaringType,
            returnType: _returnType,
            _parameterTypes);

        public static int GetHashCode(Type declaringType, Type returnType, Type[] parameterTypes)
        {
            int hashcode = declaringType.GetHashCode();
            hashcode = int.RotateLeft(hashcode, 5);

            hashcode ^= returnType.GetHashCode();
            hashcode = int.RotateLeft(hashcode, 5);

            for (int i = 0; i < parameterTypes.Length; i++)
            {
                hashcode ^= parameterTypes[i].GetHashCode();
                hashcode = int.RotateLeft(hashcode, 5);
            }

            // We don't include _isStatic in the hashcode because it is already included with _declaringType==typeof(void).

            return hashcode;
        }

        /// <summary>
        /// Return an array of normalized types for a calli signature.
        /// </summary>
        /// <param name="parameterTypes"></param>
        /// <returns></returns>
        private static Type[] GetNormalizedParameterTypes(Type[] parameterTypes)
        {
            if (parameterTypes.Length == 0)
            {
                return parameterTypes;
            }

            Type[]? normalizedParameterTypes = null;

            // Check if we can re-use the existing array if it is already normalized.
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                if (!InvokeSignatureInfo.IsNormalized(parameterTypes[i]))
                {
                    normalizedParameterTypes = new Type[parameterTypes.Length];
                    break;
                }
            }

            if (normalizedParameterTypes is null)
            {
                return parameterTypes;
            }

            for (int i = 0; i < parameterTypes.Length; i++)
            {
                normalizedParameterTypes[i] = NormalizeType(parameterTypes[i]);
            }

            return normalizedParameterTypes;
        }

        /// <summary>
        /// Normalize the type for a calli signature.
        /// </summary>
        public static Type NormalizeType(Type type) => IsNormalized(type) ? type : typeof(object);

        public static bool IsNormalized(Type type) =>
            type == typeof(object) ||
            type == typeof(void) ||
            type.IsValueType ||
            type.IsByRef ||
            type.IsPointer ||
            type.IsFunctionPointer;

        /// <summary>
        /// Provide a zero-alloc ref struct for lookup with normalized types for a Calli signature.
        /// </summary>
        internal readonly ref struct NormalizedLookupKey
        {
            internal readonly Type _declaringType;
            internal readonly Type[] _parameterTypes;
            internal readonly Type _returnType;
            internal readonly bool _isStatic;

            public NormalizedLookupKey(Type declaringType, Type returnType, Type[] parameterTypes, bool isStatic)
            {
                _declaringType = isStatic ? typeof(void) : NormalizeType(declaringType);
                _parameterTypes = GetNormalizedParameterTypes(parameterTypes);
                _returnType = NormalizeType(returnType);
                _isStatic = isStatic;
            }

            public static bool AlternativeEquals(in NormalizedLookupKey @this, InvokeSignatureInfo signatureInfo)
            {
                if (!ReferenceEquals(@this._declaringType, signatureInfo._declaringType) ||
                    !ReferenceEquals(@this._returnType, signatureInfo._returnType) ||
                    @this._isStatic != signatureInfo._isStatic ||
                    @this._parameterTypes.Length != signatureInfo._parameterTypes.Length)
                {
                    return false;
                }

                for (int i = 0; i < @this._parameterTypes.Length; i++)
                {
                    if (!ReferenceEquals(@this._parameterTypes[i], signatureInfo._parameterTypes[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            public int AlternativeGetHashCode() => InvokeSignatureInfo.GetHashCode(
                declaringType: _declaringType,
                returnType: _returnType,
                _parameterTypes);
        }
    }
}
