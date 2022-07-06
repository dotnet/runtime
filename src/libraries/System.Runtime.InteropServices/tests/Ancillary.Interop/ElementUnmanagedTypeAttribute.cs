// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Specifies that a particular generic parameter is the collection element's unmanaged type.
    /// </summary>
    /// <remarks>
    /// If this attribute is provided on a generic parameter of a marshaller, then the generator will assume
    /// that it is a linear collection marshaller.
    /// </remarks>
    [AttributeUsage(AttributeTargets.GenericParameter)]
    public sealed class ElementUnmanagedTypeAttribute : Attribute
    {
    }
}
