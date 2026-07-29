// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// When applied to a type, indicates that ILC should include its field layout in the
    /// managed cDAC data descriptor so diagnostic tools can inspect instances without
    /// runtime metadata or symbols. When applied to a field of such a type, indicates ILC should
    /// include that field's offset in the descriptor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field, Inherited = false)]
    internal sealed class DataContractAttribute : Attribute
    {
    }
}
