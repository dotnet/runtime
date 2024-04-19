// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace System.Xml
{
    internal partial interface IDtdParserAdapter
    {
        Task<int> ReadDataAsync();

        Task<int> ParseNumericCharRefAsync(StringBuilder? internalSubsetBuilder);
        Task<int> ParseNamedCharRefAsync(bool expand, StringBuilder? internalSubsetBuilder);
        Task ParsePIAsync(StringBuilder? sb);
        Task ParseCommentAsync(StringBuilder? sb);

        Task<(int, bool)> PushEntityAsync(IDtdEntityInfo entity);

        Task<bool> PushExternalSubsetAsync(string? systemId, string? publicId);
    }
}
