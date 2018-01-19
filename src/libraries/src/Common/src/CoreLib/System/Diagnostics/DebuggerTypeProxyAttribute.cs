// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class DebuggerTypeProxyAttribute : Attribute
    {
        private Type _target;

        public DebuggerTypeProxyAttribute(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            ProxyTypeName = type.AssemblyQualifiedName;
        }

        public DebuggerTypeProxyAttribute(string typeName)
        {
            ProxyTypeName = typeName;
        }

        public string ProxyTypeName { get; }

        public Type Target
        {
            get => _target;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                TargetTypeName = value.AssemblyQualifiedName;
                _target = value;
            }        
        }

        public string TargetTypeName { get; set; }
    }
}
