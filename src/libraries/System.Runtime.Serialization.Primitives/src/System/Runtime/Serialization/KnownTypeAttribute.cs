// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = true)]
    public sealed class KnownTypeAttribute : Attribute
    {
        private string _methodName;
        private Type _type;

        private KnownTypeAttribute()
        {
            // Disallow default constructor
        }

        public KnownTypeAttribute(Type type)
        {
            _type = type;
        }

        public KnownTypeAttribute(string methodName)
        {
            _methodName = methodName;
        }

        public string MethodName
        {
            get { return _methodName; }
            //set { methodName = value; }
        }

        public Type Type
        {
            get { return _type; }
            //set { type = value; }
        }
    }
}

