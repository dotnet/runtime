// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using System.Security;

#if NET_NATIVE || MERGE_DCJS
namespace System.Runtime.Serialization.Json
{
    internal class JsonEnumDataContract : JsonDataContract
    {
        [SecurityCritical]
        private JsonEnumDataContractCriticalHelper _helper;

        [SecuritySafeCritical]
        public JsonEnumDataContract(EnumDataContract traditionalDataContract)
            : base(new JsonEnumDataContractCriticalHelper(traditionalDataContract))
        {
            _helper = base.Helper as JsonEnumDataContractCriticalHelper;
        }

        public bool IsULong
        {
            [SecuritySafeCritical]
            get
            { return _helper.IsULong; }
        }

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
            private bool _isULong;

            public JsonEnumDataContractCriticalHelper(EnumDataContract traditionalEnumDataContract)
                : base(traditionalEnumDataContract)
            {
                _isULong = traditionalEnumDataContract.IsULong;
            }

            public bool IsULong
            {
                get { return _isULong; }
            }
        }
    }
}
#endif
