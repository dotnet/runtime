// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class DataMemberAttribute : Attribute
    {
        private string _name;
        private bool _isNameSetExplicitly;
        private int _order = -1;

        public DataMemberAttribute()
        {
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                _isNameSetExplicitly = true;
            }
        }

        public bool IsNameSetExplicitly => _isNameSetExplicitly;

        public int Order
        {
            get => _order;
            set
            {
                if (value < 0)
                {
                    throw new InvalidDataContractException(SR.OrderCannotBeNegative);
                }

                _order = value;
            }
        }

        public bool IsRequired { get; set; }

        public bool EmitDefaultValue { get; set; } = true;
    }
}
