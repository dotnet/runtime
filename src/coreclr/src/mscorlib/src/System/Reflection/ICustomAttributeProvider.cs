// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// ICustomAttributeProvider is an interface that is implemented by reflection
// 
//    objects which support custom attributes.
//
//
namespace System.Reflection {
    
    using System;

    // Interface does not need to be marked with the serializable attribute
[System.Runtime.InteropServices.ComVisible(true)]
    public interface ICustomAttributeProvider
    {

        // Return an array of custom attributes identified by Type
        Object[] GetCustomAttributes(Type attributeType, bool inherit);


        // Return an array of all of the custom attributes (named attributes are not included)
        Object[] GetCustomAttributes(bool inherit);

    
        // Returns true if one or more instance of attributeType is defined on this member. 
        bool IsDefined (Type attributeType, bool inherit);
    
    }
}
