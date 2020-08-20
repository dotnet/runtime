// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.InteropServices.JavaScript
{
    public enum ConvertEnum
    {
        Default,
        ToLower,
        ToUpper,
        Numeric
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field,
            AllowMultiple = true, Inherited = false)]
    public class ExportAttribute : Attribute
    {
        public ExportAttribute() : this(null, null)
        {
        }

        public ExportAttribute(Type contractType) : this(null, contractType)
        {
        }

        public ExportAttribute(string contractName) : this(contractName, null)
        {
        }

        public ExportAttribute(string? contractName, Type? contractType)
        {
            ContractName = contractName;
            ContractType = contractType;
        }

        public string? ContractName { get; }

        public Type? ContractType { get; }
        public ConvertEnum EnumValue { get; set; }
    }
}
