// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


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


 

