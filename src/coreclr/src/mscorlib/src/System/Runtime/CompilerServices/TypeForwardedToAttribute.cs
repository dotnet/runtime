// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Reflection;

namespace System.Runtime.CompilerServices
{
    using System;
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple=true, Inherited=false)]
    public sealed class TypeForwardedToAttribute : Attribute
    {
        private Type _destination;

        public TypeForwardedToAttribute(Type destination)
        {
            _destination = destination;
        }

        public Type Destination
        {
            get {
                return _destination;
            }
        }

        [System.Security.SecurityCritical]
        internal static TypeForwardedToAttribute[] GetCustomAttribute(RuntimeAssembly assembly)
        {
            Type[] types = null;
            RuntimeAssembly.GetForwardedTypes(assembly.GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref types));

            TypeForwardedToAttribute[] attributes = new TypeForwardedToAttribute[types.Length];
            for (int i = 0; i < types.Length; ++i)
                attributes[i] = new TypeForwardedToAttribute(types[i]);

            return attributes;
        }

    }
}


 

