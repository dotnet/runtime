// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

