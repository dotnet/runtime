// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
** 
**
**
** Purpose: go from type to type info
**
**
=============================================================================*/

namespace System.Reflection
{
    using System.Reflection;

    public static class IntrospectionExtensions
    {
	    public static TypeInfo GetTypeInfo(this Type type){
            if(type == null){
                throw new ArgumentNullException("type");
            }
            var rcType=(IReflectableType)type;
            if(rcType==null){
                return null;
            }else{
                return rcType.GetTypeInfo();
            }
        }   
    }
}

