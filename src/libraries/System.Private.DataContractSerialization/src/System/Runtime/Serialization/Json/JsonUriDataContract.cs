// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization.Json
{
    internal sealed class JsonUriDataContract : JsonDataContract
    {
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public JsonUriDataContract(UriDataContract traditionalUriDataContract)
            : base(traditionalUriDataContract)
        {
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadJsonValueCore(XmlReaderDelegator jsonReader, XmlObjectSerializerReadContextComplexJson? context)
        {
            if (context == null)
            {
                return TryReadNullAtTopLevel(jsonReader) ? null : jsonReader.ReadElementContentAsUri();
            }
            else
            {
                return HandleReadValue(jsonReader.ReadElementContentAsUri(), context);
            }
        }
    }
}
