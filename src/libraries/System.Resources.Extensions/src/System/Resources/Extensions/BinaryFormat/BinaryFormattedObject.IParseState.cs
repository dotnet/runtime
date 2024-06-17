// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Formats.Nrbf;

namespace System.Resources.Extensions.BinaryFormat;

internal sealed partial class BinaryFormattedObject
{
    /// <summary>
    ///  Parsing state.
    /// </summary>
    internal interface IParseState
    {
        BinaryReader Reader { get; }
        IReadOnlyDictionary<SerializationRecordId, SerializationRecord> RecordMap { get; }
        Options Options { get; }
        ITypeResolver TypeResolver { get; }
    }
}
