// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Xml;

namespace System.Runtime.Serialization.Json
{
    internal class JsonCollectionDataContract : JsonDataContract
    {
        private JsonCollectionDataContractCriticalHelper _helper;

        public JsonCollectionDataContract(CollectionDataContract traditionalDataContract)
            : base(new JsonCollectionDataContractCriticalHelper(traditionalDataContract))
        {
            _helper = base.Helper as JsonCollectionDataContractCriticalHelper;
        }

#if uapaot
        [RemovableFeature(ReflectionBasedSerializationFeature.Name)]
#endif
        private JsonFormatCollectionReaderDelegate CreateJsonFormatReaderDelegate()
        {
            return new ReflectionJsonCollectionReader().ReflectionReadCollection;
        }

        internal JsonFormatCollectionReaderDelegate JsonFormatReaderDelegate
        {
            get
            {
                if (_helper.JsonFormatReaderDelegate == null)
                {
                    lock (this)
                    {
                        if (_helper.JsonFormatReaderDelegate == null)
                        {
                            JsonFormatCollectionReaderDelegate tempDelegate;
                            if (DataContractSerializer.Option == SerializationOption.ReflectionOnly)
                            {
                                tempDelegate = CreateJsonFormatReaderDelegate();
                            }
#if uapaot
                            else if (DataContractSerializer.Option == SerializationOption.ReflectionAsBackup)
                            {
                                tempDelegate = JsonDataContract.TryGetReadWriteDelegatesFromGeneratedAssembly(TraditionalCollectionDataContract)?.CollectionReaderDelegate;
                                tempDelegate = tempDelegate ?? CreateJsonFormatReaderDelegate();

                                if (tempDelegate == null)
                                    throw new InvalidDataContractException(SR.Format(SR.SerializationCodeIsMissingForType, TraditionalCollectionDataContract.UnderlyingType));
                            }
#endif
                            else 
                            {
#if uapaot
                                tempDelegate = JsonDataContract.GetReadWriteDelegatesFromGeneratedAssembly(TraditionalCollectionDataContract).CollectionReaderDelegate;
#else   
                                tempDelegate = new JsonFormatReaderGenerator().GenerateCollectionReader(TraditionalCollectionDataContract);
#endif
                            }

                            Interlocked.MemoryBarrier();
                            _helper.JsonFormatReaderDelegate = tempDelegate;
                        }
                    }
                }
                return _helper.JsonFormatReaderDelegate;
            }
        }

#if uapaot
        [RemovableFeature(ReflectionBasedSerializationFeature.Name)]
#endif
        private JsonFormatGetOnlyCollectionReaderDelegate CreateJsonFormatGetOnlyReaderDelegate()
        {
            return new ReflectionJsonCollectionReader().ReflectionReadGetOnlyCollection;
        }

        internal JsonFormatGetOnlyCollectionReaderDelegate JsonFormatGetOnlyReaderDelegate
        {
            get
            {
                if (_helper.JsonFormatGetOnlyReaderDelegate == null)
                {
                    lock (this)
                    {
                        if (_helper.JsonFormatGetOnlyReaderDelegate == null)
                        {
                            CollectionKind kind = this.TraditionalCollectionDataContract.Kind;
                            if (this.TraditionalDataContract.UnderlyingType.IsInterface && (kind == CollectionKind.Enumerable || kind == CollectionKind.Collection || kind == CollectionKind.GenericEnumerable))
                            {
                                throw new InvalidDataContractException(SR.Format(SR.GetOnlyCollectionMustHaveAddMethod, DataContract.GetClrTypeFullName(this.TraditionalDataContract.UnderlyingType)));
                            }

                            JsonFormatGetOnlyCollectionReaderDelegate tempDelegate;
                            if (DataContractSerializer.Option == SerializationOption.ReflectionOnly)
                            {
                                tempDelegate = CreateJsonFormatGetOnlyReaderDelegate();
                            }
#if uapaot
                            else if (DataContractSerializer.Option == SerializationOption.ReflectionAsBackup)
                            {
                                tempDelegate = JsonDataContract.TryGetReadWriteDelegatesFromGeneratedAssembly(TraditionalCollectionDataContract)?.GetOnlyCollectionReaderDelegate;
                                tempDelegate = tempDelegate ?? CreateJsonFormatGetOnlyReaderDelegate();

                                if (tempDelegate == null)
                                    throw new InvalidDataContractException(SR.Format(SR.SerializationCodeIsMissingForType, TraditionalCollectionDataContract.UnderlyingType));
                            }
#endif
                            else
                            {
#if uapaot
                                tempDelegate = JsonDataContract.GetReadWriteDelegatesFromGeneratedAssembly(TraditionalCollectionDataContract).GetOnlyCollectionReaderDelegate;
#else   
                                tempDelegate =  new JsonFormatReaderGenerator().GenerateGetOnlyCollectionReader(TraditionalCollectionDataContract);
#endif
                            }

                            Interlocked.MemoryBarrier();
                            _helper.JsonFormatGetOnlyReaderDelegate = tempDelegate;
                        }
                    }
                }
                return _helper.JsonFormatGetOnlyReaderDelegate;
            }
        }

#if uapaot
        [RemovableFeature(ReflectionBasedSerializationFeature.Name)]
#endif
        private JsonFormatCollectionWriterDelegate CreateJsonFormatWriterDelegate()
        {
            return new ReflectionJsonFormatWriter().ReflectionWriteCollection;
        }


        internal JsonFormatCollectionWriterDelegate JsonFormatWriterDelegate
        {
            get
            {
                if (_helper.JsonFormatWriterDelegate == null)
                {
                    lock (this)
                    {
                        if (_helper.JsonFormatWriterDelegate == null)
                        {
                            JsonFormatCollectionWriterDelegate tempDelegate;
                            if (DataContractSerializer.Option == SerializationOption.ReflectionOnly)
                            {
                                tempDelegate = CreateJsonFormatWriterDelegate();
                            }
#if uapaot
                            else if (DataContractSerializer.Option == SerializationOption.ReflectionAsBackup)
                            {
                                tempDelegate = JsonDataContract.TryGetReadWriteDelegatesFromGeneratedAssembly(TraditionalCollectionDataContract)?.CollectionWriterDelegate;
                                tempDelegate = tempDelegate ?? CreateJsonFormatWriterDelegate();

                                if (tempDelegate == null)
                                    throw new InvalidDataContractException(SR.Format(SR.SerializationCodeIsMissingForType, TraditionalCollectionDataContract.UnderlyingType));
                            }
#endif
                            else
                            {
#if uapaot
                                tempDelegate = JsonDataContract.GetReadWriteDelegatesFromGeneratedAssembly(TraditionalCollectionDataContract).CollectionWriterDelegate;
#else   
                                tempDelegate = new JsonFormatWriterGenerator().GenerateCollectionWriter(TraditionalCollectionDataContract);
#endif
                            }

                            Interlocked.MemoryBarrier();
                            _helper.JsonFormatWriterDelegate = tempDelegate;
                        }
                    }
                }
                return _helper.JsonFormatWriterDelegate;
            }
        }

        private CollectionDataContract TraditionalCollectionDataContract => _helper.TraditionalCollectionDataContract;

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
