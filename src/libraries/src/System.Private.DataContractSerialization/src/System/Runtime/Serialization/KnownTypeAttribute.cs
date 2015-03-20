// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------
//------------------------------------------------------------

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
        }

        public Type Type
        {
            get { return _type; }
        }
    }
}

