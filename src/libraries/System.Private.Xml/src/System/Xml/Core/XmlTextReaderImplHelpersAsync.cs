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
            async Task<int> IDtdParserAdapter.ReadDataAsync()
            {
                return await _reader.DtdParserProxy_ReadDataAsync().ConfigureAwait(false);
            }

            async
                        Task<int> IDtdParserAdapter.ParseNumericCharRefAsync(StringBuilder? internalSubsetBuilder)
            {
                return await _reader.DtdParserProxy_ParseNumericCharRefAsync(internalSubsetBuilder).ConfigureAwait(false);
            }

            async
                        Task<int> IDtdParserAdapter.ParseNamedCharRefAsync(bool expand, StringBuilder? internalSubsetBuilder)
            {
                return await _reader.DtdParserProxy_ParseNamedCharRefAsync(expand, internalSubsetBuilder).ConfigureAwait(false);
            }

            async
                        Task IDtdParserAdapter.ParsePIAsync(StringBuilder? sb)
            {
                await _reader.DtdParserProxy_ParsePIAsync(sb).ConfigureAwait(false);
            }

            async
                        Task IDtdParserAdapter.ParseCommentAsync(StringBuilder? sb)
            {
                await _reader.DtdParserProxy_ParseCommentAsync(sb).ConfigureAwait(false);
            }

            async
                        Task<(int, bool)> IDtdParserAdapter.PushEntityAsync(IDtdEntityInfo entity)
            {
                return await _reader.DtdParserProxy_PushEntityAsync(entity).ConfigureAwait(false);
            }

            async
                        Task<bool> IDtdParserAdapter.PushExternalSubsetAsync(string? systemId, string? publicId)
            {
                return await _reader.DtdParserProxy_PushExternalSubsetAsync(systemId, publicId).ConfigureAwait(false);
            }
        }
    }
}
