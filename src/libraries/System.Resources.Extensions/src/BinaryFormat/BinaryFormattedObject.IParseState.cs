// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization.BinaryFormat;

namespace System.Windows.Forms.BinaryFormat;

internal sealed partial class BinaryFormattedObject
{
    /// <summary>
    ///  Parsing state.
    /// </summary>
    internal interface IParseState
    {
        BinaryReader Reader { get; }
        IReadOnlyDictionary<int, SerializationRecord> RecordMap { get; }
        Options Options { get; }
        ITypeResolver TypeResolver { get; }
    }
}
