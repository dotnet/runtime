// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.InteropServices.JavaScript
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public class EnumExportAttribute : Attribute
    {
        public EnumExportAttribute()
        { }

        public EnumExportAttribute(string contractName)
        {
            ContractName = contractName;
        }

        public string? ContractName { get; }
    }
}
