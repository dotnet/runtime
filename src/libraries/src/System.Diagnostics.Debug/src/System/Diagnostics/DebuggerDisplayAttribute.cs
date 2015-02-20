// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;

namespace System.Diagnostics
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class DebuggerDisplayAttribute : Attribute
    {
        private string name;
        private string value;
        private string type;
        private string targetName;
        private Type target;

        public DebuggerDisplayAttribute(string value)
        {
            if (value == null)
            {
                this.value = "";
            }
            else
            {
                this.value = value;
            }
            name = "";
            type = "";
        }

        public string Value
        {
            get { return this.value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string Type
        {
            get { return type; }
            set { type = value; }
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
