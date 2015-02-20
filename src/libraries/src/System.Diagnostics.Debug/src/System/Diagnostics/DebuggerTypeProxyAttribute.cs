// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;

namespace System.Diagnostics
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class DebuggerTypeProxyAttribute : Attribute
    {
        private string typeName;
        private string targetName;
        private Type target;

        public DebuggerTypeProxyAttribute(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            Contract.EndContractBlock();

            this.typeName = type.AssemblyQualifiedName;
        }

        public DebuggerTypeProxyAttribute(string typeName)
        {
            this.typeName = typeName;
        }
        public string ProxyTypeName
        {
            get { return typeName; }
        }

        public Type Target
        {
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                Contract.EndContractBlock();

                targetName = value.AssemblyQualifiedName;
                target = value;
            }

            get { return target; }
        }

        public string TargetTypeName
        {
            get { return targetName; }
            set { targetName = value; }

        }
    }
}
