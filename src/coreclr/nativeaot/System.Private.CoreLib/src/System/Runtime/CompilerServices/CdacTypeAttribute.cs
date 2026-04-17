// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// When applied to a type, indicates that ILC should include its field layout in the
    /// managed cDAC data descriptor. The cDAC reader merges this information as a
    /// sub-descriptor so diagnostic tools can inspect managed type instances without
    /// runtime metadata (critical for NativeAOT where metadata may be stripped).
    /// </summary>
    /// <remarks>
    /// Fields to include must be individually annotated with <see cref="CdacFieldAttribute"/>.
    /// The type and field names used in the descriptor match the actual managed names.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    internal sealed class CdacTypeAttribute : Attribute
    {
    }
}
