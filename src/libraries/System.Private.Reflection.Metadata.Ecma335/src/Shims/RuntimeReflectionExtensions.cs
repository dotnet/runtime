// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace System.Reflection
{
    internal static partial class RuntimeReflectionExtensions
    {
        public static MethodInfo GetRuntimeMethod(this Type type, String name, Type[] parameters)
        {
            if (type == null) 
            {
                throw new ArgumentNullException(nameof(type));
            }
            return type.GetMethod(name, parameters);
        }
    }
}

