// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
    public sealed class DataContractAttribute : Attribute
    {
        private string _name;
        private string _ns;
        private bool _isNameSetExplicitly;
        private bool _isNamespaceSetExplicitly;
        private bool _isReference;
        private bool _isReferenceSetExplicitly;

        public DataContractAttribute()
        {
        }

        public bool IsReference
        {
            get => _isReference;
            set
            {
                _isReference = value;
                _isReferenceSetExplicitly = true;
            }
        }

        public bool IsReferenceSetExplicitly => _isReferenceSetExplicitly;

        public string Namespace
        {
            get => _ns;
            set
            {
                _ns = value;
                _isNamespaceSetExplicitly = true;
            }
        }

        public bool IsNamespaceSetExplicitly => _isNamespaceSetExplicitly;

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
    }
}
