// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    internal partial class ModifiedType
    {
        /// <summary>
        /// Holds type signature information about the modified type
        /// It can have two sources for signatures:
        ///     - SignatureHolderType - holds function pointer type signature
        ///     - SignatureHolderInfo - holds field/property/parameter type signature
        /// This comes down of having three different scenarios:
        ///     1. Only SignatureHolderInfo holds signature information, example:
        ///         volatile int intField;
        ///     2. Only SignatureHolderType holds signature information, example:
        ///         delegate* unmanaged[Cdecl]&lt;int&gt; fptrField1;
        ///     3. Both SignatureHolderType and SignatureHolderInfo hold signature information, example:
        ///         volatile delegate* unmanaged[Cdecl]&lt;int&gt; fptrField2;
        ///     NOTE: In scenario 3) the SignatureHolderInfo has higher priority for retrieving field data (like custom modifiers)
        /// </summary>
        internal struct TypeSignature
        {
            internal readonly RuntimeType? SignatureHolderType;
            internal readonly object? SignatureHolderInfo;
            internal int ParameterIndex;

            internal TypeSignature(RuntimeType signatureHolderType, int parameterIndex)
            {
                SignatureHolderType = signatureHolderType;
                SignatureHolderInfo = null;
                ParameterIndex = parameterIndex;
            }

            internal TypeSignature(object signatureHolderInfo, int parameterIndex)
            {
                SignatureHolderType = null;
                SignatureHolderInfo = signatureHolderInfo;
                ParameterIndex = parameterIndex;
            }

            internal TypeSignature(RuntimeType signatureHolderType, object signatureHolderInfo, int parameterIndex)
            {
                SignatureHolderType = signatureHolderType;
                SignatureHolderInfo = signatureHolderInfo;
                ParameterIndex = parameterIndex;
            }

            internal bool TryGetCustomModifiersFromSignatureHolderInfo(bool required, out Type[] modifiers)
            {
                if (SignatureHolderInfo is null)
                {
                    modifiers = Type.EmptyTypes;
                    return false;
                }
                else
                {
                    switch (SignatureHolderInfo)
                    {
                        case RuntimeFieldInfo fieldInfo:
                            modifiers = fieldInfo.GetCustomModifiersFromModifiedType(!required, fieldInfo.FieldType.IsGenericType ? ParameterIndex : -1);
                            break;
                        case RuntimeParameterInfo parameterInfo:
                            modifiers = parameterInfo.GetCustomModifiersFromModifiedType(!required, parameterInfo.ParameterType.IsGenericType ? ParameterIndex : -1);
                            break;
                        case RuntimePropertyInfo propertyInfo:
                            modifiers = propertyInfo.GetCustomModifiersFromModifiedType(!required, propertyInfo.PropertyType.IsGenericType ? ParameterIndex : -1);
                            break;
                        default:
                            throw new Exception($"SignatureHolderInfo: {SignatureHolderInfo} is not recognized");
                    }
                    return true;
                }
            }

            internal bool TryGetCustomModifiersFromSignatureHolderType(bool required, out Type[] modifiers)
            {
                if (SignatureHolderType is null)
                {
                    modifiers = Type.EmptyTypes;
                    return false;
                }
                else
                {
                    modifiers = SignatureHolderType.GetCustomModifiersFromFunctionPointer(ParameterIndex, optional: !required);
                    return true;
                }
            }
        }

        internal static Type Create(Type sourceType, object sourceTypeInfo, int parameterIndex = 0)
        {
            var unmodifiedType = (RuntimeType)sourceType;
            TypeSignature typeSignature;

            if (unmodifiedType.IsFunctionPointer)
                typeSignature = new TypeSignature(unmodifiedType, sourceTypeInfo, parameterIndex);
            else
                typeSignature = new TypeSignature(sourceTypeInfo, parameterIndex);

            return Create(unmodifiedType, typeSignature);
        }

        // If the current unmodifiedType is a function pointer that means that the signature holder for the modified type
        // (and all its children types) becomes the unmodifiedType. At the same time, if the current or parent unmodified
        // types are function pointers, then SignatureHolderInfo becomes irrelevant as there is no more dependency on fetching
        // custom modifiers from parent's field/param/property info.
        // In all other cases, we pass parent's type signature information down the hierarchy.
        internal Type GetTypeParameter(Type unmodifiedType, int index)
        {
            var parentUnmodifiedType = UnmodifiedType;
            var childUnmodifiedType = (RuntimeType)unmodifiedType;
            TypeSignature childTypeSignature;

            if (childUnmodifiedType.IsFunctionPointer)
            {
                childTypeSignature = new TypeSignature(childUnmodifiedType, index);
            }
            else
            {
                if (parentUnmodifiedType.IsFunctionPointer)
                {
                    var parentSignatureHolderType = _typeSignature.SignatureHolderType ??
                        throw new Exception($"Parent's {nameof(_typeSignature.SignatureHolderType)} cannot be null");
                    childTypeSignature = new TypeSignature(parentSignatureHolderType, index);
                }
                else
                {
                    var parentSignatureHolderInfo = _typeSignature.SignatureHolderInfo ??
                        throw new Exception($"Parent's {nameof(_typeSignature.SignatureHolderInfo)} cannot be null");
                    childTypeSignature = new TypeSignature(parentSignatureHolderInfo, index);
                }
            }

            return Create(childUnmodifiedType, childTypeSignature);
        }

        internal SignatureCallingConvention GetCallingConventionFromFunctionPointer()
        {
            if (_typeSignature.SignatureHolderType is null)
                throw new Exception($"{nameof(_typeSignature.SignatureHolderType)} cannot be null when retrieving calling conventions from a function pointer type ");
            return _typeSignature.SignatureHolderType.GetCallingConventionFromFunctionPointer();
        }

        private Type[] GetCustomModifiers(bool required)
        {
            if (_typeSignature.TryGetCustomModifiersFromSignatureHolderInfo(required, out var modifiersFromInfo))
                return modifiersFromInfo;
            else if (_typeSignature.TryGetCustomModifiersFromSignatureHolderType(required, out var modifiersFromType))
                return modifiersFromType;
            else
                throw new Exception($"Failed to retrieve custom modifiers on a modified type: {this}");
        }
    }
}
