// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;

namespace System.Diagnostics
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class DebuggerTypeProxyAttribute : Attribute
    {
        private readonly string _typeName;
        private string _targetName;
        private Type _target;

        public DebuggerTypeProxyAttribute(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            Contract.EndContractBlock();

            this._typeName = type.AssemblyQualifiedName;
        }

        public DebuggerTypeProxyAttribute(string typeName)
        {
            this._typeName = typeName;
        }
        public string ProxyTypeName
        {
            get { return _typeName; }
        }

        public Type Target
        {
            get { return _target; }
            
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                Contract.EndContractBlock();

                _targetName = value.AssemblyQualifiedName;
                _target = value;
            }
        }

        public string TargetTypeName
        {
            get { return _targetName; }
            set { _targetName = value; }

        }
    }
}
