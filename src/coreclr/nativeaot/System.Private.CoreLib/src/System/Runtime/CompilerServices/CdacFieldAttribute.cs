// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// When applied to an instance field of a type annotated with <see cref="CdacTypeAttribute"/>,
    /// indicates that ILC should include this field in the managed cDAC data descriptor.
    /// The field's actual declared name is used in the descriptor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    internal sealed class CdacFieldAttribute : Attribute
    {
    }
}
