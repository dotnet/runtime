// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Xml;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security;

namespace System.Runtime.Serialization.Json
{
    internal class JsonClassDataContract : JsonDataContract
    {
        [SecurityCritical]
        private JsonClassDataContractCriticalHelper _helper;

        [SecuritySafeCritical]
        public JsonClassDataContract(ClassDataContract traditionalDataContract)
            : base(new JsonClassDataContractCriticalHelper(traditionalDataContract))
        {
            _helper = base.Helper as JsonClassDataContractCriticalHelper;
        }

        internal JsonFormatClassReaderDelegate JsonFormatReaderDelegate
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
#if MERGE_DCJS
                            JsonFormatClassReaderDelegate tempDelegate = new JsonFormatReaderGenerator().GenerateClassReader(TraditionalClassDataContract);
                            Interlocked.MemoryBarrier();
#else
                            JsonFormatClassReaderDelegate tempDelegate = JsonDataContract.GetReadWriteDelegatesFromGeneratedAssembly(TraditionalClassDataContract).ClassReaderDelegate;
#endif
                            _helper.JsonFormatReaderDelegate = tempDelegate;
                        }
                    }
                }
                return _helper.JsonFormatReaderDelegate;
            }
        }

        internal JsonFormatClassWriterDelegate JsonFormatWriterDelegate
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
#if MERGE_DCJS
                            JsonFormatClassWriterDelegate tempDelegate = new JsonFormatWriterGenerator().GenerateClassWriter(TraditionalClassDataContract);
                            Interlocked.MemoryBarrier();
#else
                            JsonFormatClassWriterDelegate tempDelegate = JsonDataContract.GetReadWriteDelegatesFromGeneratedAssembly(TraditionalClassDataContract).ClassWriterDelegate;
#endif
                            _helper.JsonFormatWriterDelegate = tempDelegate;
                        }
                    }
                }
                return _helper.JsonFormatWriterDelegate;
            }
        }

        internal XmlDictionaryString[] MemberNames
        {
            [SecuritySafeCritical]
            get
            { return _helper.MemberNames; }
        }

        internal override string TypeName
        {
            [SecuritySafeCritical]
            get
            { return _helper.TypeName; }
        }


        private ClassDataContract TraditionalClassDataContract
        {
            [SecuritySafeCritical]
            get
            { return _helper.TraditionalClassDataContract; }
        }

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
