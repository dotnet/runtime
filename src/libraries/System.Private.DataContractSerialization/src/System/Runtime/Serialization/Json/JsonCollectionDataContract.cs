// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Xml;

namespace System.Runtime.Serialization.Json
{
    internal sealed class JsonCollectionDataContract : JsonDataContract
    {
        private readonly JsonCollectionDataContractCriticalHelper _helper;

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public JsonCollectionDataContract(CollectionDataContract traditionalDataContract)
            : base(new JsonCollectionDataContractCriticalHelper(traditionalDataContract))
        {
            _helper = (base.Helper as JsonCollectionDataContractCriticalHelper)!;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private JsonFormatCollectionReaderDelegate CreateJsonFormatReaderDelegate()
        {
            return new ReflectionJsonCollectionReader().ReflectionReadCollection;
        }

        internal JsonFormatCollectionReaderDelegate JsonFormatReaderDelegate
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
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
                            else
                            {
                                tempDelegate = new JsonFormatReaderGenerator().GenerateCollectionReader(TraditionalCollectionDataContract);
                            }

                            Interlocked.MemoryBarrier();
                            _helper.JsonFormatReaderDelegate = tempDelegate;
                        }
                    }
                }
                return _helper.JsonFormatReaderDelegate;
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private JsonFormatGetOnlyCollectionReaderDelegate CreateJsonFormatGetOnlyReaderDelegate()
        {
            return new ReflectionJsonCollectionReader().ReflectionReadGetOnlyCollection;
        }

        internal JsonFormatGetOnlyCollectionReaderDelegate JsonFormatGetOnlyReaderDelegate
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
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
                            else
                            {
                                tempDelegate = new JsonFormatReaderGenerator().GenerateGetOnlyCollectionReader(TraditionalCollectionDataContract);
                            }

                            Interlocked.MemoryBarrier();
                            _helper.JsonFormatGetOnlyReaderDelegate = tempDelegate;
                        }
                    }
                }
                return _helper.JsonFormatGetOnlyReaderDelegate;
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private JsonFormatCollectionWriterDelegate CreateJsonFormatWriterDelegate()
        {
            return new ReflectionJsonFormatWriter().ReflectionWriteCollection;
        }


        internal JsonFormatCollectionWriterDelegate JsonFormatWriterDelegate
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
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
                            else
                            {
                                tempDelegate = new JsonFormatWriterGenerator().GenerateCollectionWriter(TraditionalCollectionDataContract);
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadJsonValueCore(XmlReaderDelegator jsonReader, XmlObjectSerializerReadContextComplexJson? context)
        {
            Debug.Assert(context != null);

            jsonReader.Read();
            object? o = null;
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteJsonValueCore(XmlWriterDelegator jsonWriter, object obj, XmlObjectSerializerWriteContextComplexJson? context, RuntimeTypeHandle declaredTypeHandle)
        {
            Debug.Assert(context != null);
            // IsGetOnlyCollection value has already been used to create current collectiondatacontract, value can now be reset.
            context.IsGetOnlyCollection = false;
            JsonFormatWriterDelegate(jsonWriter, obj, context, TraditionalCollectionDataContract);
        }

        private sealed class JsonCollectionDataContractCriticalHelper : JsonDataContractCriticalHelper
        {
            private JsonFormatCollectionReaderDelegate? _jsonFormatReaderDelegate;
            private JsonFormatGetOnlyCollectionReaderDelegate? _jsonFormatGetOnlyReaderDelegate;
            private JsonFormatCollectionWriterDelegate? _jsonFormatWriterDelegate;
            private readonly CollectionDataContract _traditionalCollectionDataContract;

            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            public JsonCollectionDataContractCriticalHelper(CollectionDataContract traditionalDataContract)
                : base(traditionalDataContract)
            {
                _traditionalCollectionDataContract = traditionalDataContract;
            }

            internal JsonFormatCollectionReaderDelegate? JsonFormatReaderDelegate
            {
                get { return _jsonFormatReaderDelegate; }
                set { _jsonFormatReaderDelegate = value; }
            }

            internal JsonFormatGetOnlyCollectionReaderDelegate? JsonFormatGetOnlyReaderDelegate
            {
                get { return _jsonFormatGetOnlyReaderDelegate; }
                set { _jsonFormatGetOnlyReaderDelegate = value; }
            }

            internal JsonFormatCollectionWriterDelegate? JsonFormatWriterDelegate
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
