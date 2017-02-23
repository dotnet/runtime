// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Reflection;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class TypeForwardedToAttribute : Attribute
    {
        private Type _destination;

        public TypeForwardedToAttribute(Type destination)
        {
            _destination = destination;
        }

        public Type Destination
        {
            get
            {
                return _destination;
            }
        }
    }
}




