// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;

namespace System.Reflection.Runtime.Tracing
{
    [EventSource(Guid = "55B578AE-32B0-48F8-822F-B3245E6FA59C", Name = "System.Reflection.Runtime.Tracing")]
    internal sealed class ReflectionEventSource : EventSource
    {
        // Defines the singleton instance for the Resources ETW provider
        public static readonly ReflectionEventSource Log = new ReflectionEventSource();

        public static bool IsInitialized
        {
            get
            {
                return Log != null;
            }
        }

        private ReflectionEventSource() { }


        #region Reflection Event Handlers
        [Event(1)]
        public void TypeInfo_CustomAttributes(string typeName)
        {
            WriteEvent(1, typeName);
        }

        [Event(2)]
        public void TypeInfo_Name(string typeName)
        {
            WriteEvent(2, typeName);
        }

        [Event(3)]
        public void TypeInfo_BaseType(string typeName)
        {
            WriteEvent(3, typeName);
        }

        [Event(4)]
        public void TypeInfo_DeclaredConstructors(string typeName)
        {
            WriteEvent(4, typeName);
        }

        [Event(5)]
        public void TypeInfo_DeclaredEvents(string typeName)
        {
            WriteEvent(5, typeName);
        }

        [Event(6)]
        public void TypeInfo_DeclaredFields(string typeName)
        {
            WriteEvent(6, typeName);
        }

        [Event(7)]
        public void TypeInfo_DeclaredMembers(string typeName)
        {
            WriteEvent(7, typeName);
        }

        [Event(8)]
        public void TypeInfo_DeclaredMethods(string typeName)
        {
            WriteEvent(8, typeName);
        }

        [Event(9)]
        public void TypeInfo_DeclaredNestedTypes(string typeName)
        {
            WriteEvent(9, typeName);
        }

        [Event(10)]
        public void TypeInfo_DeclaredProperties(string typeName)
        {
            WriteEvent(10, typeName);
        }

        [Event(11)]
        public void TypeInfo_DeclaringMethod(string typeName)
        {
            WriteEvent(11, typeName);
        }

        [Event(12)]
        public void TypeInfo_FullName(string typeName)
        {
            WriteEvent(12, typeName);
        }

        [Event(13)]
        public void TypeInfo_Namespace(string typeName)
        {
            WriteEvent(13, typeName);
        }

        [Event(14)]
        public void TypeInfo_GetDeclaredEvent(string typeName, string eventName)
        {
            WriteEvent(14, typeName, eventName);
        }

        [Event(15)]
        public void TypeInfo_GetDeclaredField(string typeName, string fieldName)
        {
            WriteEvent(15, typeName, fieldName);
        }

        [Event(16)]
        public void TypeInfo_GetDeclaredMethod(string typeName, string methodName)
        {
            WriteEvent(16, typeName, methodName);
        }

        [Event(17)]
        public void TypeInfo_GetDeclaredProperty(string typeName, string propertyName)
        {
            WriteEvent(17, typeName, propertyName);
        }

        [Event(18)]
        public void TypeInfo_MakeArrayType(string typeName)
        {
            WriteEvent(18, typeName);
        }

        [Event(19)]
        public void TypeInfo_MakeByRefType(string typeName)
        {
            WriteEvent(19, typeName);
        }

        [Event(20)]
        public void TypeInfo_MakeGenericType(string typeName, string typeArguments)
        {
            WriteEvent(20, typeName, typeArguments);
        }

        [Event(21)]
        public void TypeInfo_MakePointerType(string typeName)
        {
            WriteEvent(21, typeName);
        }

        [Event(22)]
        public void Assembly_DefinedTypes(string assemblyName)
        {
            WriteEvent(22, assemblyName);
        }

        [Event(23)]
        public void Assembly_GetType(string assemblyName, string typeName)
        {
            WriteEvent(23, assemblyName, typeName);
        }

        [Event(24)]
        public void Assembly_CustomAttributes(string assemblyName)
        {
            WriteEvent(24, assemblyName);
        }

        [Event(25)]
        public void Assembly_FullName(string assemblyName)
        {
            WriteEvent(25, assemblyName);
        }

        [Event(26)]
        public void Assembly_GetName(string assemblyName)
        {
            WriteEvent(26, assemblyName);
        }

        [Event(27)]
        public void CustomAttributeData_ConstructorArguments(string caName)
        {
            WriteEvent(27, caName);
        }

        [Event(28)]
        public void CustomAttributeData_NamedArguments(string caName)
        {
            WriteEvent(28, caName);
        }

        [Event(29)]
        public void EventInfo_AddMethod(string typeName, string eventName)
        {
            WriteEvent(29, typeName, eventName);
        }

        [Event(30)]
        public void EventInfo_RaiseMethod(string typeName, string eventName)
        {
            WriteEvent(30, typeName, eventName);
        }

        [Event(31)]
        public void EventInfo_RemoveMethod(string typeName, string eventName)
        {
            WriteEvent(31, typeName, eventName);
        }

        [Event(32)]
        public void EventInfo_CustomAttributes(string typeName, string eventName)
        {
            WriteEvent(32, typeName, eventName);
        }

        [Event(33)]
        public void EventInfo_Name(string typeName, string eventName)
        {
            WriteEvent(33, typeName, eventName);
        }

        [Event(34)]
        public void EventInfo_DeclaringType(string typeName, string eventName)
        {
            WriteEvent(34, typeName, eventName);
        }

        [Event(35)]
        public void FieldInfo_SetValue(string typeName, string fieldName)
        {
            WriteEvent(35, typeName, fieldName);
        }

        [Event(36)]
        public void FieldInfo_GetValue(string typeName, string fieldName)
        {
            WriteEvent(36, typeName, fieldName);
        }

        [Event(37)]
        public void FieldInfo_CustomAttributes(string typeName, string fieldName)
        {
            WriteEvent(37, typeName, fieldName);
        }

        [Event(38)]
        public void FieldInfo_Name(string typeName, string fieldName)
        {
            WriteEvent(38, typeName, fieldName);
        }

        [Event(39)]
        public void FieldInfo_DeclaringType(string typeName, string fieldName)
        {
            WriteEvent(39, typeName, fieldName);
        }

        [Event(40)]
        public void MethodBase_CustomAttributes(string typeName, string methodName)
        {
            WriteEvent(40, typeName, methodName);
        }

        [Event(41)]
        public void MethodBase_Name(string typeName, string methodName)
        {
            WriteEvent(41, typeName, methodName);
        }

        [Event(42)]
        public void MethodBase_DeclaringType(string typeName, string methodName)
        {
            WriteEvent(42, typeName, methodName);
        }

        [Event(43)]
        public void MethodBase_GetParameters(string typeName, string methodName)
        {
            WriteEvent(43, typeName, methodName);
        }

        [Event(44)]
        public void MethodBase_Invoke(string typeName, string methodName)
        {
            WriteEvent(44, typeName, methodName);
        }

        [Event(45)]
        public void MethodInfo_ReturnParameter(string typeName, string methodName)
        {
            WriteEvent(45, typeName, methodName);
        }

        [Event(46)]
        public void MethodInfo_ReturnType(string typeName, string methodName)
        {
            WriteEvent(46, typeName, methodName);
        }

        [Event(47)]
        public void MethodInfo_MakeGenericMethod(string typeName, string methodName, string typeArguments)
        {
            WriteEvent(47, typeName, methodName, typeArguments);
        }

        [Event(48)]
        public void MethodInfo_CreateDelegate(string typeName, string methodName, string delegateTypeName)
        {
            WriteEvent(48, typeName, methodName, delegateTypeName);
        }

        [Event(49)]
        public void PropertyInfo_GetValue(string typeName, string propertyName)
        {
            WriteEvent(49, typeName, propertyName);
        }

        [Event(50)]
        public void PropertyInfo_SetValue(string typeName, string propertyName)
        {
            WriteEvent(50, typeName, propertyName);
        }

        [Event(51)]
        public void PropertyInfo_GetMethod(string typeName, string propertyName)
        {
            WriteEvent(51, typeName, propertyName);
        }

        [Event(52)]
        public void PropertyInfo_SetMethod(string typeName, string propertyName)
        {
            WriteEvent(52, typeName, propertyName);
        }

        [Event(53)]
        public void PropertyInfo_GetConstantValue(string typeName, string propertyName)
        {
            WriteEvent(53, typeName, propertyName);
        }

        [Event(54)]
        public void PropertyInfo_PropertyType(string typeName, string propertyName)
        {
            WriteEvent(54, typeName, propertyName);
        }

        [Event(55)]
        public void PropertyInfo_CustomAttributes(string typeName, string propertyName)
        {
            WriteEvent(55, typeName, propertyName);
        }

        [Event(56)]
        public void PropertyInfo_Name(string typeName, string propertyName)
        {
            WriteEvent(56, typeName, propertyName);
        }

        [Event(57)]
        public void PropertyInfo_DeclaringType(string typeName, string propertyName)
        {
            WriteEvent(57, typeName, propertyName);
        }

        [Event(58)]
        public void TypeInfo_AssemblyQualifiedName(string typeName)
        {
            WriteEvent(58, typeName);
        }
        #endregion
    }
}
