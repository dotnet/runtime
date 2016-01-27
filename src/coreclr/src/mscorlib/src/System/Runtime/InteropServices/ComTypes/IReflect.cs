// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*==========================================================================
**
** Interface:  IReflect
**
**
** Purpose: 
** This interface is redefined here since the original IReflect interface 
** has all its methods marked as ecall's since it is a managed standard 
** interface. This interface is used from within the runtime to make a call 
** on the COM server directly when it implements the IReflect interface.
**
** 
==========================================================================*/
namespace System.Runtime.InteropServices.ComTypes
{
    using System;
    using System.Reflection;
    using CultureInfo = System.Globalization.CultureInfo;

    [Guid("AFBF15E5-C37C-11d2-B88E-00A0C9B471B8")]    
    internal interface IReflect
    {
        MethodInfo GetMethod(String name,BindingFlags bindingAttr,Binder binder,
                Type[] types,ParameterModifier[] modifiers);

        MethodInfo GetMethod(String name,BindingFlags bindingAttr);

        MethodInfo[] GetMethods(
                BindingFlags bindingAttr);

        FieldInfo GetField(
                String name,
                BindingFlags bindingAttr);

        FieldInfo[] GetFields(
                BindingFlags bindingAttr);

        PropertyInfo GetProperty(
                String name,
                BindingFlags bindingAttr);

        PropertyInfo GetProperty(
                String name,
                BindingFlags bindingAttr,
                Binder binder,
                Type returnType,
                Type[] types,
                ParameterModifier[] modifiers);

        PropertyInfo[] GetProperties(
                BindingFlags bindingAttr);

        MemberInfo[] GetMember(
                String name,
                BindingFlags bindingAttr);

        MemberInfo[] GetMembers(
                BindingFlags bindingAttr);

        Object InvokeMember(
                String name,
                BindingFlags invokeAttr,
                Binder binder,
                Object target,
                Object[] args,
                ParameterModifier[] modifiers,
                CultureInfo culture,
                String[] namedParameters);

        Type UnderlyingSystemType
        {
            get;
        }
    }
}
