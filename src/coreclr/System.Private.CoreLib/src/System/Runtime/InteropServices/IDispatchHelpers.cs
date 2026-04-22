// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    internal static class IDispatchHelpers
    {
        private const int DispatchExPropertyCanRead = 1;
        private const int DispatchExPropertyCanWrite = 2;

        [UnmanagedCallersOnly]
        [RequiresUnsafe]
        private static unsafe int GetDispatchExPropertyFlags(PropertyInfo* pMemberInfo, Exception* pException)
        {
            try
            {
                int result = 0;
                PropertyInfo property = *pMemberInfo;
                if (property.CanRead)
                {
                    result |= DispatchExPropertyCanRead;
                }

                if (property.CanWrite)
                {
                    result |= DispatchExPropertyCanWrite;
                }

                return result;
            }
            catch (Exception ex)
            {
                *pException = ex;
                return 0;
            }
        }

        [UnmanagedCallersOnly]
        [RequiresUnsafe]
        private static unsafe void GetDispatchParameterInfoName(ParameterInfo* pParameterInfo, string* pResult, Exception* pException)
        {
            try
            {
                *pResult = pParameterInfo->Name;
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        [UnmanagedCallersOnly]
        [RequiresUnsafe]
        private static unsafe void GetDispatchMemberInfoName(MemberInfo* pMemberInfo, string* pResult, Exception* pException)
        {
            try
            {
                *pResult = pMemberInfo->Name;
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        [UnmanagedCallersOnly]
        [RequiresUnsafe]
        private static unsafe int GetDispatchMemberInfoType(MemberInfo* pMemberInfo, Exception* pException)
        {
            try
            {
                return (int)pMemberInfo->MemberType;
            }
            catch (Exception ex)
            {
                *pException = ex;
                return 0;
            }
        }

        [UnmanagedCallersOnly]
        [RequiresUnsafe]
        private static unsafe void HasDispatchCustomAttribute(MemberInfo* pMemberInfo, Type* pAttributeType, bool* pResult, Exception* pException)
        {
            try
            {
                *pResult = pMemberInfo->IsDefined(*pAttributeType, inherit: false);
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        [UnmanagedCallersOnly]
        [RequiresUnsafe]
        private static unsafe void GetDispatchMemberParameters(MemberInfo* pMemberInfo, ParameterInfo[]* pResult, Exception* pException)
        {
            try
            {
                ParameterInfo[]? parameters = *pMemberInfo switch
                {
                    MethodBase methodBase => methodBase.GetParameters(),
                    PropertyInfo propertyInfo => propertyInfo.GetIndexParameters(),
                    _ => null,
                };

                *pResult = parameters;
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        [UnmanagedCallersOnly]
        [RequiresUnsafe]
        private static unsafe void GetDispatchPropertyTokenAndModule(PropertyInfo* pPropertyInfo, int* pToken, RuntimeModule* pModule, Exception* pException)
        {
            try
            {
                *pToken = pPropertyInfo->MetadataToken;
                *pModule = (RuntimeModule)pPropertyInfo->Module;
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        [UnmanagedCallersOnly]
        [RequiresUnsafe]
        private static unsafe void GetDispatchPropertyAccessor(PropertyInfo* pPropertyInfo, bool getter, bool nonPublic, IntPtr* pResult, Exception* pException)
        {
            try
            {
                MethodInfo? accessor = getter ? pPropertyInfo->GetGetMethod(nonPublic) : pPropertyInfo->GetSetMethod(nonPublic);
                *pResult = accessor?.MethodHandle.Value ?? IntPtr.Zero;
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        [UnmanagedCallersOnly]
        [RequiresUnsafe]
        private static unsafe void GetDispatchFieldValue(FieldInfo* pFieldInfo, object* pTarget, object* pResult, Exception* pException)
        {
            try
            {
                *pResult = pFieldInfo->GetValue(*pTarget);
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        [UnmanagedCallersOnly]
        [RequiresUnsafe]
        private static unsafe void SetDispatchFieldValue(FieldInfo* pFieldInfo, object* pTarget, object* pValue, int invokeAttr, Binder* pBinder, Globalization.CultureInfo* pCulture, Exception* pException)
        {
            try
            {
                pFieldInfo->SetValue(
                    *pTarget,
                    *pValue,
                    (BindingFlags)invokeAttr,
                    *pBinder,
                    *pCulture);
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        [UnmanagedCallersOnly]
        [RequiresUnsafe]
        private static unsafe void GetDispatchPropertyValue(PropertyInfo* pPropertyInfo, object* pTarget, int invokeAttr, Binder* pBinder, object[]* pIndexArgs, Globalization.CultureInfo* pCulture, object* pResult, Exception* pException)
        {
            try
            {
                *pResult = pPropertyInfo->GetValue(
                    *pTarget,
                    (BindingFlags)invokeAttr,
                    *pBinder,
                    *pIndexArgs,
                    *pCulture);
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        [UnmanagedCallersOnly]
        [RequiresUnsafe]
        private static unsafe void SetDispatchPropertyValue(PropertyInfo* pPropertyInfo, object* pTarget, object* pValue, int invokeAttr, Binder* pBinder, object[]* pIndexArgs, Globalization.CultureInfo* pCulture, Exception* pException)
        {
            try
            {
                pPropertyInfo->SetValue(
                    *pTarget,
                    *pValue,
                    (BindingFlags)invokeAttr,
                    *pBinder,
                    *pIndexArgs,
                    *pCulture);
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        [UnmanagedCallersOnly]
        [RequiresUnsafe]
        private static unsafe void InvokeDispatchMethodInfo(MethodInfo* pMemberInfo, object* pTarget, int invokeAttr, Binder* pBinder, object[]* pArgs, Globalization.CultureInfo* pCulture, object* pResult, Exception* pException)
        {
            try
            {
                *pResult = pMemberInfo->Invoke(
                    *pTarget,
                    (BindingFlags)invokeAttr,
                    *pBinder,
                    *pArgs,
                    *pCulture);
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        [RequiresUnreferencedCode("The member might be removed")]
        [UnmanagedCallersOnly]
        [RequiresUnsafe]
        private static unsafe void InvokeDispatchReflectMember(IReflect* pReflectionObject, string* pName, int invokeAttr, Binder* pBinder, object* pTarget, object[]* pArgs, ParameterModifier[]* pModifiers, Globalization.CultureInfo* pCulture, string[]* pNamedParams, object* pResult, Exception* pException)
        {
            try
            {
                *pResult = pReflectionObject->InvokeMember(
                    *pName,
                    (BindingFlags)invokeAttr,
                    *pBinder,
                    *pTarget,
                    *pArgs,
                    *pModifiers,
                    *pCulture,
                    *pNamedParams);
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        [RequiresUnreferencedCode("The member might be removed")]
        [UnmanagedCallersOnly]
        [RequiresUnsafe]
        private static unsafe void GetDispatchProperties(IReflect* pReflectionObject, int bindingFlags, PropertyInfo[]* pResult, Exception* pException)
        {
            try
            {
                *pResult = pReflectionObject->GetProperties((BindingFlags)bindingFlags);
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        [RequiresUnreferencedCode("The member might be removed")]
        [UnmanagedCallersOnly]
        [RequiresUnsafe]
        private static unsafe void GetDispatchFields(IReflect* pReflectionObject, int bindingFlags, FieldInfo[]* pResult, Exception* pException)
        {
            try
            {
                *pResult = pReflectionObject->GetFields((BindingFlags)bindingFlags);
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        [RequiresUnreferencedCode("The member might be removed")]
        [UnmanagedCallersOnly]
        [RequiresUnsafe]
        private static unsafe void GetDispatchMethods(IReflect* pReflectionObject, int bindingFlags, MethodInfo[]* pResult, Exception* pException)
        {
            try
            {
                *pResult = pReflectionObject->GetMethods((BindingFlags)bindingFlags);
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }

        [UnmanagedCallersOnly]
        [RequiresUnsafe]
        private static unsafe void GetDispatchInnerException(Exception* pExceptionObject, Exception* pResult, Exception* pException)
        {
            try
            {
                *pResult = pExceptionObject->InnerException;
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }
    }
}
