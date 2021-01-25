// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    // Custom attribute to indicating a TypeDef is a discardable attribute.
    [AttributeUsage(AttributeTargets.All)]
    public class DiscardableAttribute : Attribute
    {
        public DiscardableAttribute() { }
    }
}
