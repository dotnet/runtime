// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;

namespace System.Diagnostics
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class DebuggerDisplayAttribute : Attribute
    {
        private readonly string _value;
        
        private string _name;
        private string _type;
        private string _targetName;
        private Type _target;

        public DebuggerDisplayAttribute(string value)
        {
            if (value == null)
            {
                this._value = "";
            }
            else
            {
                this._value = value;
            }
            _name = "";
            _type = "";
        }

        public string Value
        {
            get { return this._value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Type
        {
            get { return _type; }
            set { _type = value; }
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

                _targetName = value.AssemblyQualifiedName;
                _target = value;
            }
            get { return _target; }
        }

        public string TargetTypeName
        {
            get { return _targetName; }
            set { _targetName = value; }
        }
    }
}
