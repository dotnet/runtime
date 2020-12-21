// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Interface, Inherited = false)]
    public sealed class CoClassAttribute : Attribute
    {
        public CoClassAttribute(Type coClass)
        {
            CoClass = coClass;
        }

        public Type CoClass { get; }
    }
}
