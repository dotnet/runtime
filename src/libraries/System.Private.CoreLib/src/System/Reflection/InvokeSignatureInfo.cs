// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using static System.Reflection.MethodBase;

namespace System.Reflection
{
    internal sealed class InvokeSignatureInfo
    {
        internal readonly Type _declaringType;
        internal readonly Type[] _parameterTypes;
        internal readonly Type _returnType;
        internal readonly bool _isStatic;

        public static InvokeSignatureInfo Create(in InvokeSignatureInfoKey InvokeSignatureInfoKey)
        {
            return new InvokeSignatureInfo(
                InvokeSignatureInfoKey._declaringType,
                InvokeSignatureInfoKey._parameterTypes,
                InvokeSignatureInfoKey._returnType,
                InvokeSignatureInfoKey._isStatic);
        }

        public InvokeSignatureInfo(Type declaringType, Type[] parameterTypes, Type returnType, bool isStatic)
        {
            _declaringType = declaringType;
            _parameterTypes = parameterTypes;
            _returnType = returnType;
            _isStatic = isStatic;
        }

        // Must be the same as InvokeSignatureInfoKey.Comparer.Equals().
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

        public override int GetHashCode() => GetHashCode(_declaringType, _parameterTypes, _returnType);

        public static int GetHashCode(Type declaringType, Type[] parameterTypes, Type returnType)
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
    }

    /// <summary>
    /// Provide a zero-alloc ref struct to wrap member signature properties needed for both cache lookup and emit.
    /// </summary>
    internal readonly ref struct InvokeSignatureInfoKey
    {
        internal readonly Type _declaringType;
        internal readonly Type[] _parameterTypes;
        internal readonly Type _returnType;
        internal readonly bool _isStatic;

        public static InvokeSignatureInfoKey CreateNormalized(Type declaringType, Type[] parameterTypes, Type returnType, bool isStatic)
        {
            return new InvokeSignatureInfoKey(
                isStatic ? typeof(void) : NormalizeType(declaringType),
                GetNormalizedParameterTypes(parameterTypes),
                NormalizeType(returnType),
                isStatic);
        }

        public InvokeSignatureInfoKey(Type declaringType, Type[] parameterTypes, Type returnType, bool isStatic)
        {
            _declaringType = declaringType;
            _parameterTypes = parameterTypes;
            _returnType = returnType;
            _isStatic = isStatic;
        }

        public Type DeclaringType => _declaringType;
        public Type[] ParameterTypes => _parameterTypes;
        public Type ReturnType => _returnType;
        public bool IsStatic => _isStatic;

        public static bool AlternativeEquals(in InvokeSignatureInfoKey @this, InvokeSignatureInfo signatureInfo)
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
            _declaringType,
            _parameterTypes,
            _returnType);

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
                if (!IsNormalized(parameterTypes[i]))
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
        private static Type NormalizeType(Type type) => IsNormalized(type) ? type : typeof(object);

        private static bool IsNormalized(Type type) =>
            type == typeof(object) ||
            type == typeof(void) ||
            type.IsValueType ||
            type.IsByRef ||
            type.IsPointer ||
            type.IsFunctionPointer;
    }
}
