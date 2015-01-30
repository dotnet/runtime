// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
** 
**
**
** Purpose: For Assembly-related stuff.
**
**
=============================================================================*/

namespace System.Reflection
{
    using System;

    public abstract class ReflectionContext
    {
        protected ReflectionContext() { }

        public abstract Assembly MapAssembly(Assembly assembly);

        public abstract TypeInfo MapType(TypeInfo type);

        public virtual TypeInfo GetTypeForObject(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return MapType(value.GetType().GetTypeInfo());
        }
    }
}
