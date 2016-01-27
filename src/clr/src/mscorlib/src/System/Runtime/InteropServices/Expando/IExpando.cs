// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// IExpando is an interface which allows Objects implemeningt this interface 
//    support the ability to modify the object by adding and removing members, 
//    represented by MemberInfo objects.
//
//
// The IExpando Interface.
namespace System.Runtime.InteropServices.Expando {
    
    using System;
    using System.Reflection;

    [Guid("AFBF15E6-C37C-11d2-B88E-00A0C9B471B8")]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface IExpando : IReflect
    {
        // Add a new Field to the reflection object.  The field has
        // name as its name.
        FieldInfo AddField(String name);

        // Add a new Property to the reflection object.  The property has
        // name as its name.
        PropertyInfo AddProperty(String name);

        // Add a new Method to the reflection object.  The method has 
        // name as its name and method is a delegate
        // to the method.  
        MethodInfo AddMethod(String name, Delegate method);

        // Removes the specified member.
        void RemoveMember(MemberInfo m);
    }
}
