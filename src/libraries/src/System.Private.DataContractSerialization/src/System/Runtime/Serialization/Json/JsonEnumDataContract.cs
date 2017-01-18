// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization.Json
{
    internal class JsonEnumDataContract : JsonDataContract
    {
        private JsonEnumDataContractCriticalHelper _helper;

        public JsonEnumDataContract(EnumDataContract traditionalDataContract)
            : base(new JsonEnumDataContractCriticalHelper(traditionalDataContract))
        {
            _helper = base.Helper as JsonEnumDataContractCriticalHelper;
        }

        public bool IsULong => _helper.IsULong;

        public override object ReadJsonValueCore(XmlReaderDelegator jsonReader, XmlObjectSerializerReadContextComplexJson context)
        {
            object enumValue;
            if (IsULong)
            {
                enumValue = Enum.ToObject(TraditionalDataContract.UnderlyingType, jsonReader.ReadElementContentAsUnsignedLong());
            }
            else
            {
                enumValue = Enum.ToObject(TraditionalDataContract.UnderlyingType, jsonReader.ReadElementContentAsLong());
            }

            if (context != null)
            {
                context.AddNewObject(enumValue);
            }
            return enumValue;
        }

        public override void WriteJsonValueCore(XmlWriterDelegator jsonWriter, object obj, XmlObjectSerializerWriteContextComplexJson context, RuntimeTypeHandle declaredTypeHandle)
        {
            if (IsULong)
            {
                jsonWriter.WriteUnsignedLong(Convert.ToUInt64(obj));
            }
            else
            {
                jsonWriter.WriteLong(Convert.ToInt64(obj));
            }
        }

        private class JsonEnumDataContractCriticalHelper : JsonDataContractCriticalHelper
        {
            private readonly bool _isULong;

            public JsonEnumDataContractCriticalHelper(EnumDataContract traditionalEnumDataContract)
                : base(traditionalEnumDataContract)
            {
                _isULong = traditionalEnumDataContract.IsULong;
            }

            public bool IsULong => _isULong;
        }
    }
}
