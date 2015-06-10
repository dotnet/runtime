// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Xml;
using System.Security;

#if NET_NATIVE
namespace System.Runtime.Serialization.Json
{
    internal class JsonCollectionDataContract : JsonDataContract
    {
        [SecurityCritical]
        private JsonCollectionDataContractCriticalHelper _helper;

        [SecuritySafeCritical]
        public JsonCollectionDataContract(CollectionDataContract traditionalDataContract)
            : base(new JsonCollectionDataContractCriticalHelper(traditionalDataContract))
        {
            _helper = base.Helper as JsonCollectionDataContractCriticalHelper;
        }

        internal JsonFormatCollectionReaderDelegate JsonFormatReaderDelegate
        {
            [SecuritySafeCritical]
            get
            {
                if (_helper.JsonFormatReaderDelegate == null)
                {
                    lock (this)
                    {
                        if (_helper.JsonFormatReaderDelegate == null)
                        {
                            JsonFormatCollectionReaderDelegate tempDelegate = JsonDataContract.GetReadWriteDelegatesFromGeneratedAssembly(TraditionalCollectionDataContract).CollectionReaderDelegate;
                            _helper.JsonFormatReaderDelegate = tempDelegate;
                        }
                    }
                }
                return _helper.JsonFormatReaderDelegate;
            }
        }

        internal JsonFormatGetOnlyCollectionReaderDelegate JsonFormatGetOnlyReaderDelegate
        {
            [SecuritySafeCritical]
            get
            {
                if (_helper.JsonFormatGetOnlyReaderDelegate == null)
                {
                    lock (this)
                    {
                        if (_helper.JsonFormatGetOnlyReaderDelegate == null)
                        {
                            CollectionKind kind = this.TraditionalCollectionDataContract.Kind;
                            if (this.TraditionalDataContract.TypeIsInterface && (kind == CollectionKind.Enumerable || kind == CollectionKind.Collection || kind == CollectionKind.GenericEnumerable))
                            {
                                throw new InvalidDataContractException(SR.Format(SR.GetOnlyCollectionMustHaveAddMethod, DataContract.GetClrTypeFullName(this.TraditionalDataContract.UnderlyingType)));
                            }
                            JsonFormatGetOnlyCollectionReaderDelegate tempDelegate = JsonDataContract.GetReadWriteDelegatesFromGeneratedAssembly(TraditionalCollectionDataContract).GetOnlyCollectionReaderDelegate;
                            _helper.JsonFormatGetOnlyReaderDelegate = tempDelegate;
                        }
                    }
                }
                return _helper.JsonFormatGetOnlyReaderDelegate;
            }
        }

        internal JsonFormatCollectionWriterDelegate JsonFormatWriterDelegate
        {
            [SecuritySafeCritical]
            get
            {
                if (_helper.JsonFormatWriterDelegate == null)
                {
                    lock (this)
                    {
                        if (_helper.JsonFormatWriterDelegate == null)
                        {
                            JsonFormatCollectionWriterDelegate tempDelegate = JsonDataContract.GetReadWriteDelegatesFromGeneratedAssembly(TraditionalCollectionDataContract).CollectionWriterDelegate;
                            _helper.JsonFormatWriterDelegate = tempDelegate;
                        }
                    }
                }
                return _helper.JsonFormatWriterDelegate;
            }
        }

        private CollectionDataContract TraditionalCollectionDataContract
        {
            [SecuritySafeCritical]
            get
            { return _helper.TraditionalCollectionDataContract; }
        }

        public override object ReadJsonValueCore(XmlReaderDelegator jsonReader, XmlObjectSerializerReadContextComplexJson context)
        {
            jsonReader.Read();
            object o = null;
            if (context.IsGetOnlyCollection)
            {
                // IsGetOnlyCollection value has already been used to create current collectiondatacontract, value can now be reset. 
                context.IsGetOnlyCollection = false;
                JsonFormatGetOnlyReaderDelegate(jsonReader, context, XmlDictionaryString.Empty, JsonGlobals.itemDictionaryString, TraditionalCollectionDataContract);
            }
            else
            {
                o = JsonFormatReaderDelegate(jsonReader, context, XmlDictionaryString.Empty, JsonGlobals.itemDictionaryString, TraditionalCollectionDataContract);
            }
            jsonReader.ReadEndElement();
            return o;
        }

        public override void WriteJsonValueCore(XmlWriterDelegator jsonWriter, object obj, XmlObjectSerializerWriteContextComplexJson context, RuntimeTypeHandle declaredTypeHandle)
        {
            // IsGetOnlyCollection value has already been used to create current collectiondatacontract, value can now be reset. 
            context.IsGetOnlyCollection = false;
            JsonFormatWriterDelegate(jsonWriter, obj, context, TraditionalCollectionDataContract);
        }

        private class JsonCollectionDataContractCriticalHelper : JsonDataContractCriticalHelper
        {
            private JsonFormatCollectionReaderDelegate _jsonFormatReaderDelegate;
            private JsonFormatGetOnlyCollectionReaderDelegate _jsonFormatGetOnlyReaderDelegate;
            private JsonFormatCollectionWriterDelegate _jsonFormatWriterDelegate;
            private CollectionDataContract _traditionalCollectionDataContract;

            public JsonCollectionDataContractCriticalHelper(CollectionDataContract traditionalDataContract)
                : base(traditionalDataContract)
            {
                _traditionalCollectionDataContract = traditionalDataContract;
            }

            internal JsonFormatCollectionReaderDelegate JsonFormatReaderDelegate
            {
                get { return _jsonFormatReaderDelegate; }
                set { _jsonFormatReaderDelegate = value; }
            }

            internal JsonFormatGetOnlyCollectionReaderDelegate JsonFormatGetOnlyReaderDelegate
            {
                get { return _jsonFormatGetOnlyReaderDelegate; }
                set { _jsonFormatGetOnlyReaderDelegate = value; }
            }

            internal JsonFormatCollectionWriterDelegate JsonFormatWriterDelegate
            {
                get { return _jsonFormatWriterDelegate; }
                set { _jsonFormatWriterDelegate = value; }
            }

            internal CollectionDataContract TraditionalCollectionDataContract
            {
                get { return _traditionalCollectionDataContract; }
            }
        }
    }
}
#endif
