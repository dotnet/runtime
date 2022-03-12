// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal partial class FieldAccessor
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object? InvokeGetter(object? obj)
        {
            // Todo: add strategy for calling IL Emit-based version
            return InvokeGetterNonEmit(obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvokeSetter(object? obj, object? value)
        {
            // Todo: add strategy for calling IL Emit-based version
            InvokeSetterNonEmit(obj, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal object? InvokeGetterNonEmit(object? obj)
        {
            RuntimeType? declaringType = _fieldInfo.DeclaringType as RuntimeType;
            RuntimeType fieldType = (RuntimeType)_fieldInfo.FieldType;
            bool domainInitialized = false;

            if (declaringType == null)
            {
                return RuntimeFieldHandle.GetValue(_fieldInfo, obj, fieldType, null, ref domainInitialized);
            }
            else
            {
                domainInitialized = declaringType.DomainInitialized;
                object? retVal = RuntimeFieldHandle.GetValue(_fieldInfo, obj, fieldType, declaringType, ref domainInitialized);
                declaringType.DomainInitialized = domainInitialized;
                return retVal;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void InvokeSetterNonEmit(object? obj, object? value)
        {
            RuntimeType? declaringType = _fieldInfo.DeclaringType as RuntimeType;
            RuntimeType fieldType = (RuntimeType)_fieldInfo.FieldType;
            bool domainInitialized = false;

            if (declaringType == null)
            {
                RuntimeFieldHandle.SetValue(
                    _fieldInfo,
                    obj,
                    value,
                    fieldType,
                    _fieldInfo.Attributes,
                    declaringType: null,
                    ref domainInitialized);
            }
            else
            {
                domainInitialized = declaringType.DomainInitialized;

                RuntimeFieldHandle.SetValue(
                    _fieldInfo,
                    obj,
                    value,
                    fieldType,
                    _fieldInfo.Attributes,
                    declaringType,
                    ref domainInitialized);

                declaringType.DomainInitialized = domainInitialized;
            }
        }

    }
}
