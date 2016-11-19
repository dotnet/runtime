// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Xml;
using System.Diagnostics;
using System.Collections.Generic;

namespace System.Runtime.Serialization.Json
{
    internal class JsonClassDataContract : JsonDataContract
    {
        private JsonClassDataContractCriticalHelper _helper;

        public JsonClassDataContract(ClassDataContract traditionalDataContract)
            : base(new JsonClassDataContractCriticalHelper(traditionalDataContract))
        {
            _helper = base.Helper as JsonClassDataContractCriticalHelper;
        }

        internal JsonFormatClassReaderDelegate JsonFormatReaderDelegate
        {
            get
            {
                if (_helper.JsonFormatReaderDelegate == null)
                {
                    lock (this)
                    {
                        if (_helper.JsonFormatReaderDelegate == null)
                        {
                            JsonFormatClassReaderDelegate tempDelegate;
                            if (DataContractSerializer.Option == SerializationOption.ReflectionOnly)
                            {
                                tempDelegate = new ReflectionJsonClassReader(TraditionalClassDataContract).ReflectionReadClass;
                            }
#if NET_NATIVE
                            else if (DataContractSerializer.Option == SerializationOption.ReflectionAsBackup)
                            {
                                tempDelegate = JsonDataContract.TryGetReadWriteDelegatesFromGeneratedAssembly(TraditionalClassDataContract).ClassReaderDelegate;
                                tempDelegate = tempDelegate ?? new ReflectionJsonClassReader(TraditionalClassDataContract).ReflectionReadClass;

                                if (tempDelegate == null)
                                    throw new InvalidDataContractException(SR.Format(SR.SerializationCodeIsMissingForType, TraditionalClassDataContract.UnderlyingType.ToString()));
                            }
#endif
                            else 
                            {
#if NET_NATIVE
                                tempDelegate = JsonDataContract.GetReadWriteDelegatesFromGeneratedAssembly(TraditionalClassDataContract).ClassReaderDelegate;
#else   
                                tempDelegate = new JsonFormatReaderGenerator().GenerateClassReader(TraditionalClassDataContract);
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

        internal JsonFormatClassWriterDelegate JsonFormatWriterDelegate
        {
            get
            {
                if (_helper.JsonFormatWriterDelegate == null)
                {
                    lock (this)
                    {
                        if (_helper.JsonFormatWriterDelegate == null)
                        {
                            JsonFormatClassWriterDelegate tempDelegate;
                            if (DataContractSerializer.Option == SerializationOption.ReflectionOnly)
                            {
                                tempDelegate = new ReflectionJsonFormatWriter().ReflectionWriteClass;
                            }
#if NET_NATIVE
                            else if (DataContractSerializer.Option == SerializationOption.ReflectionAsBackup)
                            {
                                tempDelegate = JsonDataContract.TryGetReadWriteDelegatesFromGeneratedAssembly(TraditionalClassDataContract)?.ClassWriterDelegate;
                                tempDelegate = tempDelegate ?? new ReflectionJsonFormatWriter().ReflectionWriteClass;

                                if (tempDelegate == null)
                                    throw new InvalidDataContractException(SR.Format(SR.SerializationCodeIsMissingForType, TraditionalClassDataContract.UnderlyingType.ToString()));
                            }
#endif
                            else 
                            {
#if NET_NATIVE
                                tempDelegate = JsonDataContract.GetReadWriteDelegatesFromGeneratedAssembly(TraditionalClassDataContract).ClassWriterDelegate;
#else   
                                tempDelegate = new JsonFormatWriterGenerator().GenerateClassWriter(TraditionalClassDataContract);
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

        internal XmlDictionaryString[] MemberNames => _helper.MemberNames;

        internal override string TypeName => _helper.TypeName;

        private ClassDataContract TraditionalClassDataContract => _helper.TraditionalClassDataContract;

        public override object ReadJsonValueCore(XmlReaderDelegator jsonReader, XmlObjectSerializerReadContextComplexJson context)
        {
            jsonReader.Read();
            object o = JsonFormatReaderDelegate(jsonReader, context, XmlDictionaryString.Empty, MemberNames);
            jsonReader.ReadEndElement();
            return o;
        }

        public override void WriteJsonValueCore(XmlWriterDelegator jsonWriter, object obj, XmlObjectSerializerWriteContextComplexJson context, RuntimeTypeHandle declaredTypeHandle)
        {
            jsonWriter.WriteAttributeString(null, JsonGlobals.typeString, null, JsonGlobals.objectString);
            JsonFormatWriterDelegate(jsonWriter, obj, context, TraditionalClassDataContract, MemberNames);
        }

        private class JsonClassDataContractCriticalHelper : JsonDataContractCriticalHelper
        {
            private JsonFormatClassReaderDelegate _jsonFormatReaderDelegate;
            private JsonFormatClassWriterDelegate _jsonFormatWriterDelegate;
            private XmlDictionaryString[] _memberNames;
            private ClassDataContract _traditionalClassDataContract;
            private string _typeName;

            public JsonClassDataContractCriticalHelper(ClassDataContract traditionalDataContract)
                : base(traditionalDataContract)
            {
                _typeName = string.IsNullOrEmpty(traditionalDataContract.Namespace.Value) ? traditionalDataContract.Name.Value : string.Concat(traditionalDataContract.Name.Value, JsonGlobals.NameValueSeparatorString, XmlObjectSerializerWriteContextComplexJson.TruncateDefaultDataContractNamespace(traditionalDataContract.Namespace.Value));
                _traditionalClassDataContract = traditionalDataContract;
                CopyMembersAndCheckDuplicateNames();
            }

            internal JsonFormatClassReaderDelegate JsonFormatReaderDelegate
            {
                get { return _jsonFormatReaderDelegate; }
                set { _jsonFormatReaderDelegate = value; }
            }

            internal JsonFormatClassWriterDelegate JsonFormatWriterDelegate
            {
                get { return _jsonFormatWriterDelegate; }
                set { _jsonFormatWriterDelegate = value; }
            }

            internal XmlDictionaryString[] MemberNames
            {
                get { return _memberNames; }
            }

            internal ClassDataContract TraditionalClassDataContract
            {
                get { return _traditionalClassDataContract; }
            }

            private void CopyMembersAndCheckDuplicateNames()
            {
                if (_traditionalClassDataContract.MemberNames != null)
                {
                    int memberCount = _traditionalClassDataContract.MemberNames.Length;
                    Dictionary<string, object> memberTable = new Dictionary<string, object>(memberCount);
                    XmlDictionaryString[] decodedMemberNames = new XmlDictionaryString[memberCount];
                    for (int i = 0; i < memberCount; i++)
                    {
                        if (memberTable.ContainsKey(_traditionalClassDataContract.MemberNames[i].Value))
                        {
                            throw new SerializationException(SR.Format(SR.JsonDuplicateMemberNames, DataContract.GetClrTypeFullName(_traditionalClassDataContract.UnderlyingType), _traditionalClassDataContract.MemberNames[i].Value));
                        }
                        else
                        {
                            memberTable.Add(_traditionalClassDataContract.MemberNames[i].Value, null);
                            decodedMemberNames[i] = DataContractJsonSerializerImpl.ConvertXmlNameToJsonName(_traditionalClassDataContract.MemberNames[i]);
                        }
                    }
                    _memberNames = decodedMemberNames;
                }
            }
        }
    }
}
