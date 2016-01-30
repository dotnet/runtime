// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Runtime.Serialization
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module, Inherited = false, AllowMultiple = true)]
    public sealed class ContractNamespaceAttribute : Attribute
    {
        private string _clrNamespace;
        private string _contractNamespace;

        public ContractNamespaceAttribute(string contractNamespace)
        {
            _contractNamespace = contractNamespace;
        }

        public string ClrNamespace
        {
            get { return _clrNamespace; }
            set { _clrNamespace = value; }
        }

        public string ContractNamespace
        {
            get { return _contractNamespace; }
        }
    }
}

