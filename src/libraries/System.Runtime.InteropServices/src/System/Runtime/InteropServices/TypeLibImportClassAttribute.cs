// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Interface, Inherited = false)]
    public sealed class TypeLibImportClassAttribute : Attribute
    {
        public TypeLibImportClassAttribute(Type importClass) => Value = importClass.ToString();

        public string Value { get; }
    }
}
