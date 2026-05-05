// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

namespace System.Xml
{
    internal partial class XmlWrappingReader : XmlReader, IXmlLineInfo
    {
        public override async Task<string> GetValueAsync()
        {
            return await reader.GetValueAsync().ConfigureAwait(false);
        }

        public override async Task<bool> ReadAsync()
        {
            return await reader.ReadAsync().ConfigureAwait(false);
        }

        public override async Task SkipAsync()
        {
            await reader.SkipAsync().ConfigureAwait(false);
        }
    }
}
