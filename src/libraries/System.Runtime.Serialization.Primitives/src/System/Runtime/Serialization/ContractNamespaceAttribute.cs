// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module, Inherited = false, AllowMultiple = true)]
    public sealed class ContractNamespaceAttribute : Attribute
    {
        public ContractNamespaceAttribute(string contractNamespace)
        {
            ContractNamespace = contractNamespace;
        }

        public string ClrNamespace { get; set; }

        public string ContractNamespace { get; }
    }
}
