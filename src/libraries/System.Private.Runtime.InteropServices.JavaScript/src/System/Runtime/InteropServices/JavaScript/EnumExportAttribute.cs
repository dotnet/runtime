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
    public class EnumExportAttribute : Attribute
    {
        public EnumExportAttribute()
        { }

        public EnumExportAttribute(string contractName)
        {
            ContractName = contractName;
        }

        public string? ContractName { get; }
        public ConvertEnum EnumValue { get; set; }
    }
}
