// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Security;
using System.Xml.Schema;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.Versioning;

using System.Threading.Tasks;

namespace System.Xml
{
    internal partial class XmlTextReaderImpl
    {
        //
        // DtdParserProxy: IDtdParserAdapter proxy for XmlTextReaderImpl
        //
        internal partial class DtdParserProxy : IDtdParserAdapterV1
        {
            Task<int> IDtdParserAdapter.ReadDataAsync()
            {
                return _reader.DtdParserProxy_ReadDataAsync();
            }

            Task<int> IDtdParserAdapter.ParseNumericCharRefAsync(StringBuilder internalSubsetBuilder)
            {
                return _reader.DtdParserProxy_ParseNumericCharRefAsync(internalSubsetBuilder);
            }

            Task<int> IDtdParserAdapter.ParseNamedCharRefAsync(bool expand, StringBuilder internalSubsetBuilder)
            {
                return _reader.DtdParserProxy_ParseNamedCharRefAsync(expand, internalSubsetBuilder);
            }

            Task IDtdParserAdapter.ParsePIAsync(StringBuilder sb)
            {
                return _reader.DtdParserProxy_ParsePIAsync(sb);
            }

            Task IDtdParserAdapter.ParseCommentAsync(StringBuilder sb)
            {
                return _reader.DtdParserProxy_ParseCommentAsync(sb);
            }

            Task<Tuple<int, bool>> IDtdParserAdapter.PushEntityAsync(IDtdEntityInfo entity)
            {
                return _reader.DtdParserProxy_PushEntityAsync(entity);
            }

            Task<bool> IDtdParserAdapter.PushExternalSubsetAsync(string systemId, string publicId)
            {
                return _reader.DtdParserProxy_PushExternalSubsetAsync(systemId, publicId);
            }
        }
    }
}
