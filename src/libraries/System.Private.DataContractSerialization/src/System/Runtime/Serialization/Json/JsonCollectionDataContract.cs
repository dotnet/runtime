// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.DataContracts;
using System.Threading;
using System.Xml;

namespace System.Runtime.Serialization.Json
{
    internal sealed class JsonCollectionDataContract : JsonDataContract
    {
        private readonly JsonCollectionDataContractCriticalHelper _helper;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public JsonCollectionDataContract(CollectionDataContract traditionalDataContract)
            : base(new JsonCollectionDataContractCriticalHelper(traditionalDataContract))
        {
            _helper = (base.Helper as JsonCollectionDataContractCriticalHelper)!;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal JsonFormatCollectionReaderDelegate GetJsonFormatReaderDelegate(JsonDictionaryFormat format)
        {
            int index = (int)format;
            JsonFormatCollectionReaderDelegate? read = Volatile.Read(ref _helper.JsonFormatReaders[index]);
            if (read is null)
            {
                lock (this)
                {
                    read = _helper.JsonFormatReaders[index];
                    if (read is null)
                    {
                        if (DataContractSerializer.Option == SerializationOption.ReflectionOnly)
                        {
                            read = new ReflectionJsonCollectionReader(TraditionalCollectionDataContract, null, format is JsonDictionaryFormat.Simple).ReflectionReadCollection;
                        }
                        else
                        {
                            JsonDictionaryFormat boundFormat = format;
                            Lazy<JsonFormatCollectionReaderDelegate> normal = new Lazy<JsonFormatCollectionReaderDelegate>(() =>
                                new JsonFormatReaderGenerator().GenerateCollectionReader(TraditionalCollectionDataContract));
                            read = (reader, context, empty, name, contract) =>
                            {
                                if (context.IsGetOnlyCollection)
                                {
                                    context.IsGetOnlyCollection = false;
                                    GetJsonFormatGetOnlyReaderDelegate(boundFormat)(reader, context, empty, name, contract);
                                    return null!;
                                }
                                return normal.Value(reader, context, empty, name, contract);
                            };
                        }

                        PublishDictionaryFormatPlan(_helper.JsonFormatReaders, index, read);
                    }
                }
            }

            return read;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal JsonFormatGetOnlyCollectionReaderDelegate GetJsonFormatGetOnlyReaderDelegate(JsonDictionaryFormat format)
        {
            int index = (int)format;
            JsonFormatGetOnlyCollectionReaderDelegate? read = Volatile.Read(ref _helper.JsonFormatGetOnlyReaders[index]);
            if (read is null)
            {
                lock (this)
                {
                    read = _helper.JsonFormatGetOnlyReaders[index];
                    if (read is null)
                    {
                        CollectionKind kind = TraditionalCollectionDataContract.Kind;
                        if (TraditionalDataContract.UnderlyingType.IsInterface &&
                            kind is CollectionKind.Enumerable or CollectionKind.Collection or CollectionKind.GenericEnumerable)
                        {
                            throw new InvalidDataContractException(SR.Format(SR.GetOnlyCollectionMustHaveAddMethod, DataContract.GetClrTypeFullName(TraditionalDataContract.UnderlyingType)));
                        }
                        read = DataContractSerializer.Option == SerializationOption.ReflectionOnly
                            ? new ReflectionJsonCollectionReader(TraditionalCollectionDataContract, true, format is JsonDictionaryFormat.Simple).ReflectionReadGetOnlyCollection
                            : new JsonFormatReaderGenerator().GenerateGetOnlyCollectionReader(TraditionalCollectionDataContract);
                        PublishDictionaryFormatPlan(_helper.JsonFormatGetOnlyReaders, index, read);
                    }
                }
            }

            return read;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal JsonFormatCollectionWriterDelegate GetJsonFormatWriterDelegate(JsonDictionaryFormat format)
        {
            int index = (int)format;
            JsonFormatCollectionWriterDelegate? write = Volatile.Read(ref _helper.JsonFormatWriters[index]);
            if (write is null)
            {
                lock (this)
                {
                    write = _helper.JsonFormatWriters[index];
                    if (write is null)
                    {
                        write = DataContractSerializer.Option == SerializationOption.ReflectionOnly
                            ? ReflectionJsonFormatWriter.CreateCollectionWriter(TraditionalCollectionDataContract, format is JsonDictionaryFormat.Simple)
                            : new JsonFormatWriterGenerator().GenerateCollectionWriter(TraditionalCollectionDataContract);
                        PublishDictionaryFormatPlan(_helper.JsonFormatWriters, index, write);
                    }
                }
            }

            return write;
        }

        private CollectionDataContract TraditionalCollectionDataContract => _helper.TraditionalCollectionDataContract;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override object? ReadJsonValueCore(XmlReaderDelegator jsonReader, XmlObjectSerializerReadContextComplexJson? context)
        {
            Debug.Assert(context != null);

            jsonReader.Read();
            object? o = GetJsonFormatReaderDelegate(context.ReflectionDictionaryFormat)(
                jsonReader, context, XmlDictionaryString.Empty, JsonGlobals.itemDictionaryString, TraditionalCollectionDataContract);
            jsonReader.ReadEndElement();
            return o;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public override void WriteJsonValueCore(XmlWriterDelegator jsonWriter, object obj, XmlObjectSerializerWriteContextComplexJson? context, RuntimeTypeHandle declaredTypeHandle)
        {
            Debug.Assert(context != null);
            // IsGetOnlyCollection value has already been used to create current collectiondatacontract, value can now be reset.
            context.IsGetOnlyCollection = false;
            GetJsonFormatWriterDelegate(context.ReflectionDictionaryFormat)(jsonWriter, obj, context, TraditionalCollectionDataContract);
        }

        private sealed class JsonCollectionDataContractCriticalHelper : JsonDataContractCriticalHelper
        {
            private readonly CollectionDataContract _traditionalCollectionDataContract;

            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            public JsonCollectionDataContractCriticalHelper(CollectionDataContract traditionalDataContract)
                : base(traditionalDataContract)
            {
                _traditionalCollectionDataContract = traditionalDataContract;
            }

            internal JsonFormatCollectionReaderDelegate?[] JsonFormatReaders { get; } = new JsonFormatCollectionReaderDelegate?[DictionaryFormatCount];
            internal JsonFormatGetOnlyCollectionReaderDelegate?[] JsonFormatGetOnlyReaders { get; } = new JsonFormatGetOnlyCollectionReaderDelegate?[DictionaryFormatCount];
            internal JsonFormatCollectionWriterDelegate?[] JsonFormatWriters { get; } = new JsonFormatCollectionWriterDelegate?[DictionaryFormatCount];

            internal CollectionDataContract TraditionalCollectionDataContract
            {
                get { return _traditionalCollectionDataContract; }
            }
        }
    }
}
