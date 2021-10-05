// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    [System.AttributeUsageAttribute(System.AttributeTargets.Method | System.AttributeTargets.Class | System.AttributeTargets.Struct | System.AttributeTargets.Delegate | System.AttributeTargets.Enum, AllowMultiple = true, Inherited=false)]
    public sealed partial class GenericParameterSupportsAnyTypeAttribute : System.Attribute
    {
        public GenericParameterSupportsAnyTypeAttribute(int index) { Index = index; }
        public int Index { get; }
    }
}
