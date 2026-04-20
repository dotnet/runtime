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
    /// By default, the type's simple name is used in the descriptor. Set <see cref="Name"/>
    /// to override the descriptor key (e.g., to use a fully-qualified name or avoid collisions).
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    internal sealed class CdacTypeAttribute : Attribute
    {
        /// <summary>
        /// Optional override for the type name used in the cDAC descriptor JSON.
        /// When null, the type's simple metadata name is used.
        /// </summary>
        public string? Name { get; set; }
    }
}
