// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace System.Xml
{
    internal sealed partial class XmlTextReaderImpl
    {
        //
        // DtdParserProxy: IDtdParserAdapter proxy for XmlTextReaderImpl
        //
        internal sealed partial class DtdParserProxy : IDtdParserAdapterV1
        {
            Task<int> IDtdParserAdapter.ReadDataAsync()
            {
                return _reader.DtdParserProxy_ReadDataAsync();
            }

            Task<int> IDtdParserAdapter.ParseNumericCharRefAsync(StringBuilder? internalSubsetBuilder)
            {
                return _reader.DtdParserProxy_ParseNumericCharRefAsync(internalSubsetBuilder);
            }

            Task<int> IDtdParserAdapter.ParseNamedCharRefAsync(bool expand, StringBuilder? internalSubsetBuilder)
            {
                return _reader.DtdParserProxy_ParseNamedCharRefAsync(expand, internalSubsetBuilder);
            }

            Task IDtdParserAdapter.ParsePIAsync(StringBuilder? sb)
            {
                return _reader.DtdParserProxy_ParsePIAsync(sb);
            }

            Task IDtdParserAdapter.ParseCommentAsync(StringBuilder? sb)
            {
                return _reader.DtdParserProxy_ParseCommentAsync(sb);
            }

            Task<(int, bool)> IDtdParserAdapter.PushEntityAsync(IDtdEntityInfo entity)
            {
                return _reader.DtdParserProxy_PushEntityAsync(entity);
            }

            Task<bool> IDtdParserAdapter.PushExternalSubsetAsync(string? systemId, string? publicId)
            {
                return _reader.DtdParserProxy_PushExternalSubsetAsync(systemId, publicId);
            }
        }
    }
}
