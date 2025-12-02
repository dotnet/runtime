// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Formats.Nrbf;

namespace System.Resources.Extensions.BinaryFormat;

internal sealed partial class BinaryFormattedObject
{
    /// <summary>
    ///  Parsing state for <see cref="BinaryFormattedObject"/>.
    /// </summary>
    internal sealed class ParseState : IParseState
    {
        private readonly BinaryFormattedObject _format;

        public ParseState(BinaryReader reader, BinaryFormattedObject format)
        {
            Reader = reader;
            _format = format;
        }

        public BinaryReader Reader { get; }
        public IReadOnlyDictionary<SerializationRecordId, SerializationRecord> RecordMap => _format.RecordMap;
        public Options Options => _format._options;
        public ITypeResolver TypeResolver => _format.TypeResolver;
    }
}
