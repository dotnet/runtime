// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Xml.Schema;

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

        int ParseNumericCharRef(StringBuilder internalSubsetBuilder);
        int ParseNamedCharRef(bool expand, StringBuilder internalSubsetBuilder);
        void ParsePI(StringBuilder sb);
        void ParseComment(StringBuilder sb);

        bool PushEntity(IDtdEntityInfo entity, out int entityId);

        bool PopEntity(out IDtdEntityInfo oldEntity, out int newEntityId);

        bool PushExternalSubset(string systemId, string publicId);

        void PushInternalDtd(string baseUri, string internalDtd);
        void OnSystemId(string systemId, LineInfo keywordLineInfo, LineInfo systemLiteralLineInfo);
        void OnPublicId(string publicId, LineInfo keywordLineInfo, LineInfo publicLiteralLineInfo);

        void Throw(Exception e);
    }

    internal interface IDtdParserAdapterWithValidation : IDtdParserAdapter
    {
        bool DtdValidation { get; }
        IValidationEventHandling ValidationEventHandling { get; }
    }

    internal interface IDtdParserAdapterV1 : IDtdParserAdapterWithValidation
    {
        bool V1CompatibilityMode { get; }
        bool Normalization { get; }
        bool Namespaces { get; }
    }
}
