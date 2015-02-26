// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;

using BufferBuilder = System.Xml.BufferBuilder;

namespace System.Xml
{
    internal partial interface IDtdParserAdapter
    {
        XmlNameTable NameTable { get; }
        IXmlNamespaceResolver NamespaceResolver { get; }

        Uri BaseUri { get; }
        char[] ParsingBuffer { get; }
        int ParsingBufferLength { get; }
        int CurrentPosition { get; set; }
        int LineNo { get; }
        int LineStartPosition { get; }
        bool IsEof { get; }
        int EntityStackLength { get; }
        bool IsEntityEolNormalized { get; }

        int ReadData();

        void OnNewLine(int pos);

        int ParseNumericCharRef(BufferBuilder internalSubsetBuilder);
        int ParseNamedCharRef(bool expand, BufferBuilder internalSubsetBuilder);
        void ParsePI(BufferBuilder sb);
        void ParseComment(BufferBuilder sb);

        bool PushEntity(IDtdEntityInfo entity, out int entityId);

        bool PopEntity(out IDtdEntityInfo oldEntity, out int newEntityId);

        bool PushExternalSubset(string systemId, string publicId);

        void PushInternalDtd(string baseUri, string internalDtd);
        void OnSystemId(string systemId, LineInfo keywordLineInfo, LineInfo systemLiteralLineInfo);
        void OnPublicId(string publicId, LineInfo keywordLineInfo, LineInfo publicLiteralLineInfo);

        void Throw(Exception e);
    }
}
