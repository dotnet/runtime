// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Security;
using System.Collections;
using System.IO;
using System.Reflection.Emit;

/*
 * Limitations:
 * The ExceptionDataContract utilizes the ClassDataConract class in order to deserialize Exceptions with the ability to fill private fields.
 * For the ClassDataContract to deserialize private fields, the name of the private field must exactly match the name of the field in xml.
 * However the thick framework does not serialize exceptions using private field names. The thick framework is able to bypass using private field names because it has a mapping for each Exception
 * made possible by implementing ISerializable. The mapping of every single .Net Exception Private Field to the Thick Framework serialized name is too much data for the ExceptionDataContract
 * to hold. 
 * This class creates a mapping for the System.Exception private fields, which are considered the most important fields for an Exception.
 * Therefore the only private fields that are supported for serialization/deserialization are those declared in System.Exception.
 * 
 * For the serialization of classes derived from System.Exception all public properties are included into the xml out.
 * There is no support for private properties other than those in System.Exception.
 * In order for the property to be reset on deserialization it must implement a setter.
 * Author: t-jicamp
*/

namespace System.Runtime.Serialization
{
    internal sealed class ExceptionDataContract : DataContract
    {
        private XmlDictionaryString[] _contractNamespaces;
        private XmlDictionaryString[] _memberNames;
        private XmlDictionaryString[] _memberNamespaces;
        [SecurityCritical]

        private ExceptionDataContractCriticalHelper _helper;

        [SecuritySafeCritical]
        public ExceptionDataContract() : base(new ExceptionDataContractCriticalHelper())
        {
            _helper = base.Helper as ExceptionDataContractCriticalHelper;
            _contractNamespaces = _helper.ContractNamespaces;
            _memberNames = _helper.MemberNames;
            _memberNamespaces = _helper.MemberNamespaces;
        }
        [SecuritySafeCritical]
        public ExceptionDataContract(Type type) : base(new ExceptionDataContractCriticalHelper(type))
        {
            _helper = base.Helper as ExceptionDataContractCriticalHelper;
            _contractNamespaces = _helper.ContractNamespaces;
            _memberNames = _helper.MemberNames;
            _memberNamespaces = _helper.MemberNamespaces;
        }
        public List<DataMember> Members
        {
            [SecuritySafeCritical]
            get
            { return _helper.Members; }
        }
        internal override bool CanContainReferences //inherited as internal
        {
            get { return true; }
        }
        public static Dictionary<string, string> EssentialExceptionFields
        {
            [SecuritySafeCritical]
            get
            { return ExceptionDataContractCriticalHelper.EssentialExceptionFields; }
        }
        public override Dictionary<XmlQualifiedName, DataContract> KnownDataContracts //inherited as internal
        {
            [SecuritySafeCritical]
            get
            { return _helper.KnownDataContracts; }
            [SecurityCritical]
            set
            { _helper.KnownDataContracts = value; }
        }
        public XmlDictionaryString[] ContractNamespaces
        {
            get { return _contractNamespaces; }
            set { _contractNamespaces = value; }
        }
        public XmlDictionaryString[] MemberNames
        {
            get { return _memberNames; }
            set { _memberNames = value; }
        }
        public XmlDictionaryString[] MemberNamespaces
        {
            get { return _memberNamespaces; }
            set { _memberNamespaces = value; }
        }


        [SecurityCritical]
        private class ExceptionDataContractCriticalHelper : DataContract.DataContractCriticalHelper
        {
            private XmlDictionaryString[] _contractNamespaces;
            private XmlDictionaryString[] _memberNames;
            private XmlDictionaryString[] _memberNamespaces;

            private ExceptionDataContract _baseContract;
            private List<DataMember> _members;
            private bool _hasDataContract;
            private Dictionary<XmlQualifiedName, DataContract> _knownDataContracts;

            //Contains the essential fields to serialize an Exception. Not all fields are serialized in an Exception. Some private fields
            //need to be serialized, but then again some need to be left out. This will  keep track of which ones need to be serialized
            //And also their display name for serialization which differs from their declared name.
            private static readonly Dictionary<string, string> s_essentialExceptionFields = CreateExceptionFields();

            /*
             * The ordering of this dictionary is important due to the nature of the ClassDataContract that ExceptionDataContract depends on.
             * In order for the ClassDataContract to correctly set the values for the members of the underlying class, it must have a list
             * of members in the same order as the xml document it is reading. These members are created in the ImportDataMembers() method,
             * and their ordering comes from this dictionary.
             * 
             * This dictionary is in the order that the Full Framework declares System.Exceptions members. This order is established
             * in the Full Framework version of System.Exception's Iserializable interface.
             */
            private static Dictionary<string, string> CreateExceptionFields()
            {
                var essentialExceptionFields = new Dictionary<string, string>(12);
                essentialExceptionFields.Add("_className", "ClassName");
                essentialExceptionFields.Add("_message", "Message");
                essentialExceptionFields.Add("_data", "Data");
                essentialExceptionFields.Add("_innerException", "InnerException");
                essentialExceptionFields.Add("_helpURL", "HelpURL");
                essentialExceptionFields.Add("_stackTraceString", "StackTraceString");
                essentialExceptionFields.Add("_remoteStackTraceString", "RemoteStackTraceString");
                essentialExceptionFields.Add("_remoteStackIndex", "RemoteStackIndex");
                essentialExceptionFields.Add("_exceptionMethodString", "ExceptionMethod");
                essentialExceptionFields.Add("_HResult", "HResult");
                essentialExceptionFields.Add("_source", "Source");
                essentialExceptionFields.Add("_watsonBuckets", "WatsonBuckets");
                return essentialExceptionFields;
            }

            public ExceptionDataContractCriticalHelper()
            {
                IsValueType = false;
            }

            public ExceptionDataContractCriticalHelper(Type type)
                : base(type)
            {
                this.StableName = DataContract.GetStableName(type, out _hasDataContract);
                Type baseType = Globals.TypeOfException;

                this.IsValueType = type.GetTypeInfo().IsValueType;
                if (baseType != null && baseType != Globals.TypeOfObject && type != Globals.TypeOfException)
                {
                    DataContract baseContract = DataContract.GetDataContract(baseType);
                    this.BaseContract = baseContract as ExceptionDataContract;
                }
                else
                {
                    this.BaseContract = null;
                }

                ImportDataMembers();
                ImportKnownTypes();
                XmlDictionary dictionary = new XmlDictionary(2 + Members.Count);
                Name = dictionary.Add(StableName.Name);
                Namespace = dictionary.Add(StableName.Namespace);

                int baseContractCount = 0;
                if (BaseContract == null)
                {
                    _memberNames = new XmlDictionaryString[Members.Count];
                    _memberNamespaces = new XmlDictionaryString[Members.Count];
                    _contractNamespaces = new XmlDictionaryString[1];
                }
                else
                {
                    _memberNames = new XmlDictionaryString[Members.Count];
                    _memberNamespaces = new XmlDictionaryString[Members.Count];
                    baseContractCount = BaseContract._contractNamespaces.Length;
                    _contractNamespaces = new XmlDictionaryString[1 + baseContractCount];
                    Array.Copy(BaseContract._contractNamespaces, 0, _contractNamespaces, 0, baseContractCount);
                }
                _contractNamespaces[baseContractCount] = Namespace;
                for (int i = 0; i < Members.Count; i++)
                {
                    _memberNames[i] = dictionary.Add(Members[i].Name);
                    _memberNamespaces[i] = Namespace;
                }
            }

            public ExceptionDataContract BaseContract
            {
                get { return _baseContract; }
                set { _baseContract = value; }
            }

            public static Dictionary<string, string> EssentialExceptionFields
            {
                get { return s_essentialExceptionFields; }
            }

            internal override Dictionary<XmlQualifiedName, DataContract> KnownDataContracts // inherited as internal
            {
                [SecurityCritical]
                get
                {
                    if (_knownDataContracts == null)
                        _knownDataContracts = new Dictionary<XmlQualifiedName, DataContract>();
                    return _knownDataContracts;
                }
                [SecurityCritical]
                set
                { /* do nothing */ }
            }

            public List<DataMember> Members
            {
                get { return _members; }
            }

            public XmlDictionaryString[] ContractNamespaces
            {
                get { return _contractNamespaces; }
                set { _contractNamespaces = value; }
            }

            public XmlDictionaryString[] MemberNames
            {
                get { return _memberNames; }
                set { _memberNames = value; }
            }

            public XmlDictionaryString[] MemberNamespaces
            {
                get { return _memberNamespaces; }
                set { _memberNamespaces = value; }
            }

            private void ImportDataMembers()
            {
                /*
                 * DataMembers are used for the deserialization of Exceptions.
                 * DataMembers represent the fields and/or settable properties that the underlying Exception has.
                 * The DataMembers are stored and eventually passed to a ClassDataContract created from the underlying Exception.
                 * The ClassDataContract uses the list of DataMembers to set the fields/properties for the newly created Exception.
                 * If a DataMember is a property it must be settable.
                 */

                Type type = this.UnderlyingType;
                if (type == Globals.TypeOfException)
                {
                    ImportSystemExceptionDataMembers(); //System.Exception must be handled specially because it is the only exception that imports private fields.
                    return;
                }

                List<DataMember> tempMembers;
                if (BaseContract != null)
                {
                    tempMembers = new List<DataMember>(BaseContract.Members);  //Don't set tempMembers = BaseContract.Members and then start adding, because this alters the base's reference.
                }
                else
                {
                    tempMembers = new List<DataMember>();
                }
                Dictionary<string, DataMember> memberNamesTable = new Dictionary<string, DataMember>();

                MemberInfo[] memberInfos;
                memberInfos = type.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);

                for (int i = 0; i < memberInfos.Length; i++)
                {
                    MemberInfo member = memberInfos[i];

                    //Public properties with set methods can be deserialized with the ClassDataContract
                    PropertyInfo publicProperty = member as PropertyInfo;
                    if (publicProperty != null && publicProperty.SetMethod != null)
                    {
                        DataMember memberContract = new DataMember(member);

                        memberContract.Name = DataContract.EncodeLocalName(member.Name);
                        memberContract.IsRequired = false;
                        memberContract.IsNullable = DataContract.IsTypeNullable(memberContract.MemberType);
                        if (HasNoConflictWithBaseMembers(memberContract))
                        {
                            CheckAndAddMember(tempMembers, memberContract, memberNamesTable);
                        }
                    }
                }

                Interlocked.MemoryBarrier();
                _members = tempMembers;
            }

            private void ImportSystemExceptionDataMembers()
            {
                /*
                 * The data members imported for System.Exception are private fields. They must be treated specially. 
                 * The EssentialExceptionFields Dictionary keeps track of which private fields needs to be imported, and also the name that they should be serialized with.
                 */

                Type type = this.UnderlyingType;
                List<DataMember> tempMembers;
                tempMembers = new List<DataMember>();
                Dictionary<string, DataMember> memberNamesTable = new Dictionary<string, DataMember>();

                foreach (string fieldInfoName in EssentialExceptionFields.Keys)
                {
                    FieldInfo member = type.GetField(fieldInfoName, BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance);

                    if (CanSerializeMember(member))
                    {
                        DataMember memberContract = new DataMember(member);

                        memberContract.Name = DataContract.EncodeLocalName(member.Name);
                        memberContract.IsRequired = false;
                        memberContract.IsNullable = DataContract.IsTypeNullable(memberContract.MemberType);
                        CheckAndAddMember(tempMembers, memberContract, memberNamesTable);
                    }
                }
                Interlocked.MemoryBarrier();
                _members = tempMembers;
            }

            private void ImportKnownTypes()
            {
                DataContract dataDataContract = GetDataContract(typeof(IDictionary<object, object>));
                this.KnownDataContracts.Add(dataDataContract.StableName, dataDataContract);
            }

            private bool HasNoConflictWithBaseMembers(DataMember memberContract)
            {
                //Don't add redundant members, this can happen if a property overrides it's base class implementation. Because the overriden property will appear as "declared" in that type.
                foreach (DataMember dm in BaseContract.Members)
                {
                    if (dm.Name.Equals(memberContract.Name))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        private static bool CanSerializeMember(FieldInfo field)
        {
            return field != null &&
                   field.FieldType != Globals.TypeOfObject; // Don't really know how to serialize plain System.Object instance
        }

        private void WriteExceptionValue(XmlWriterDelegator writer, object value, XmlObjectSerializerWriteContext context)
        {
            /*
             * Every private field present in System.Exception that is serialized in the thick framework is also serialized in this method.
             * For classes that inherit from System.Exception all public properties are serialized.
             * The reason that the Members property is not used here to get the properties to serialize is because Members contains only the properties with setters.
             */

            Type type = value.GetType();
            writer.WriteXmlnsAttribute("x", new XmlDictionary(1).Add(Globals.SchemaNamespace));
            WriteSystemExceptionRequiredValues(writer, value, context);
            PropertyInfo[] props = type.GetProperties();
            foreach (PropertyInfo prop in props)
            {
                //Properties in System.Exception are handled in the call to WriteSystemExceptionRequiredValues, we don't want to repeat these properties.
                if (PropertyIsSystemExceptionProperty(prop))
                    continue;

                writer.WriteStartElement(prop.Name, "");
                DataContract propDataContract = context.GetDataContract(prop.PropertyType);
                if (prop.GetValue(value) != null && propDataContract != null && !TryCheckIfNoCountIDictionary(prop.PropertyType, prop.GetValue(value)))
                {
                    if (!TryWritePrimitive(prop.PropertyType, prop.GetValue(value), writer, context))
                    {
                        writer.WriteAttributeString(Globals.XsiPrefix, "type", null, "a:" + propDataContract.StableName.Name);
                        writer.WriteXmlnsAttribute("a", new XmlDictionary(1).Add(propDataContract.StableName.Namespace));
                        context.SerializeWithoutXsiType(propDataContract, writer, prop.GetValue(value), prop.PropertyType.TypeHandle);
                    }
                }
                else
                {
                    writer.WriteAttributeString(Globals.XsiPrefix, "nil", null, "true");
                }
                writer.WriteEndElement();
            }
        }

        private void WriteSystemExceptionRequiredValues(XmlWriterDelegator writer, object value, XmlObjectSerializerWriteContext context)
        {
            Dictionary<string, object> exceptionFields = GetExceptionFieldValues((Exception)value);

            object val;
            foreach (string key in exceptionFields.Keys)
            {
                if (!exceptionFields.TryGetValue(key, out val))
                    continue;

                Type fieldType;
                FieldInfo FieldFind = Globals.TypeOfException.GetField(key, BindingFlags.Instance | BindingFlags.NonPublic);
                if (FieldFind == null)
                {
                    val = null; // need to nullify because the private fields that are necessary in Exception have changed. 
                    fieldType = typeof(int); // can be any type, it doesn't matter. field type will be used to recover a contract, but the type won't be utilized.
                }
                else
                    fieldType = FieldFind.FieldType;

                string fieldDisplayName;
                if (EssentialExceptionFields.TryGetValue(key, out fieldDisplayName))
                    writer.WriteStartElement(fieldDisplayName, "");
                else
                    writer.WriteStartElement(key, "");

                DataContract fieldDataContract = context.GetDataContract(fieldType);
                if (val != null && fieldDataContract != null && !TryCheckIfNoCountIDictionary(fieldType, val))
                {
                    if (!TryWritePrimitive(fieldType, val, writer, context))
                    {
                        writer.WriteAttributeString(Globals.XsiPrefix, "type", null, "a:" + fieldDataContract.StableName.Name);
                        writer.WriteXmlnsAttribute("a", new XmlDictionary(1).Add(fieldDataContract.StableName.Namespace));
                        fieldDataContract.WriteXmlValue(writer, val, context);
                    }
                }
                else
                {
                    writer.WriteAttributeString(Globals.XsiPrefix, "nil", null, "true");
                }
                writer.WriteEndElement();
            }
        }

        private bool PropertyIsSystemExceptionProperty(PropertyInfo prop)
        {
            PropertyInfo[] props = Globals.TypeOfException.GetProperties();
            foreach (PropertyInfo propInfo in props)
            {
                if (propInfo.Name.Equals(prop.Name))
                {
                    return true;
                }
            }
            return false;
        }

        private static void CheckAndAddMember(List<DataMember> members, DataMember memberContract, Dictionary<string, DataMember> memberNamesTable)
        {
            DataMember existingMemberContract;
            if (memberNamesTable.TryGetValue(memberContract.Name, out existingMemberContract))
            {
                Type declaringType = memberContract.MemberInfo.DeclaringType;
                DataContract.ThrowInvalidDataContractException(
                    SR.Format(SR.DupMemberName,
                        existingMemberContract.MemberInfo.Name,
                        memberContract.MemberInfo.Name,
                        DataContract.GetClrTypeFullName(declaringType),
                        memberContract.Name),
                    declaringType);
            }
            memberNamesTable.Add(memberContract.Name, memberContract);
            members.Add(memberContract);
        }

        [SecuritySafeCritical]
        private Dictionary<string, object> GetExceptionFieldValues(Exception value)
        {
            // Obtain the unoverrided version of Message
            Type exceptionType = Globals.TypeOfException;
            PropertyInfo messageProperty = exceptionType.GetProperty("Message");
            MethodInfo messageGetter = messageProperty.GetMethod;
#if !NET_NATIVE
            DynamicMethod baseMessageImpl = new DynamicMethod("NonVirtual_Message", typeof(string), new Type[] { Globals.TypeOfException }, Globals.TypeOfException);

            ILGenerator gen = baseMessageImpl.GetILGenerator();
            gen.Emit(OpCodes.Ldarg, messageGetter.GetParameters().Length);
            gen.EmitCall(OpCodes.Call, messageGetter, null);
            gen.Emit(OpCodes.Ret);

            string messageValue = (string)baseMessageImpl.Invoke(null, new object[] { value });
#else
            string messageValue = string.Empty;
#endif

            // Populate the values for the necessary System.Exception private fields.
            Dictionary<string, object> fieldToValueDictionary = new Dictionary<string, object>();

            fieldToValueDictionary.Add("_className", value.GetType().ToString());
            fieldToValueDictionary.Add("_message", messageValue); //Thick framework retrieves the System.Exception implementation of message
            fieldToValueDictionary.Add("_data", value.Data);
            fieldToValueDictionary.Add("_innerException", value.InnerException);
            fieldToValueDictionary.Add("_helpURL", value.HelpLink);
            fieldToValueDictionary.Add("_stackTraceString", value.StackTrace);
            fieldToValueDictionary.Add("_remoteStackTraceString", null);
            fieldToValueDictionary.Add("_remoteStackIndex", 0);
            fieldToValueDictionary.Add("_exceptionMethodString", null);
            fieldToValueDictionary.Add("_HResult", value.HResult);
            fieldToValueDictionary.Add("_source", null); //value.source caused transparency error on build.
            fieldToValueDictionary.Add("_watsonBuckets", null);

            return fieldToValueDictionary;
        }

        public override void WriteXmlValue(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context)
        {
            Type type = obj.GetType();
            if (!(typeof(Exception).IsAssignableFrom(type)))
            {
                throw new InvalidDataContractException("Cannot use ExceptionDataContract to serialize object with type: " + type);
            }

            WriteExceptionValue(xmlWriter, obj, context);
        }

        public override object ReadXmlValue(XmlReaderDelegator xmlReader, XmlObjectSerializerReadContext context)
        {
            XmlReaderDelegator xmlDelegator = ParseReaderString(xmlReader);

            ClassDataContract cdc = new ClassDataContract(this.UnderlyingType);

            // The Class Data Contract created from the underlying exception type uses custom imported members that are 
            // created in this classes constructor. Here we clear out the Class Data Contract's default members and insert our own.
            cdc.Members.Clear();
            foreach (DataMember dm in this.Members)
            {
                cdc.Members.Add(dm);
            }

            cdc.MemberNames = _memberNames;
            cdc.ContractNamespaces = _contractNamespaces;
            cdc.MemberNamespaces = _memberNamespaces;

            object obj = cdc.ReadXmlValue(xmlDelegator, context);

            if (context != null)
                context.AddNewObject(obj);
            return obj;
        }

        private bool TryWritePrimitive(Type type, object value, XmlWriterDelegator writer, XmlObjectSerializerWriteContext context)
        {
            PrimitiveDataContract primitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(type);

            if (primitiveContract == null || primitiveContract.UnderlyingType == Globals.TypeOfObject)
                return false;

            writer.WriteAttributeString(Globals.XsiPrefix, "type", null, "x:" + primitiveContract.StableName.Name);
            primitiveContract.WriteXmlValue(writer, value, context);

            return true;
        }

        private static bool TryCheckIfNoCountIDictionary(Type type, object value)
        {
            if (value == null)
                return true;
            if (type == Globals.TypeOfIDictionary)
            {
                IDictionary tempDictImitation = (IDictionary)value;
                return (tempDictImitation.Count == 0);
            }
            return false;
        }

        /*
         * The reason this function exists is to provide compatibility for ExceptionDataContract to utilize ClassDataContract for deserialization.
         * In order for ExceptionDataContract to deserialize using ClassDataContract the xml elements corresponding to private fields need
         * to have their name replaced with the name of the private field they map to.
         * Example: Message ---replaced to--> _message
         * 
         * Currently this method utilizes a custom created class entitled ExceptionXmlParser that sits alongside the ExceptionDataContract
         * The ExceptionXmlParser reads the xml string passed to it exchanges any element names that are presented in the name map
         * in its constructor.
         * 
         * The ExceptionXmlParser also gives each nested element the proper namespace for the given exception being deserialized.
         * The ClassDataContract needs an exact match on the element name and the namespace in order to deserialize the value, because not all serialization
         * explicitly inserts the xmlnamespace of nested objects, the exception xml parser handles this.
         */
        private XmlReaderDelegator ParseReaderString(XmlReaderDelegator xmlReader)
        {
            //The reference to the xmlReader passed into this method should not be modified.
            //The call to ReadOuterXml advances the xmlReader to the next object if the exception being parsed is a nested object of another class.
            //When the call to ReadXmlValue that called this method returns, it is possible that the 'xmlReader' will still be used by the calling datacontract.
            string EntryXmlString = xmlReader.UnderlyingReader.ReadOuterXml();

            string result = ExceptionXmlParser.ParseExceptionXmlForClassDataContract(ReverseDictionary(EssentialExceptionFields), this.Namespace.ToString(), EntryXmlString);

            byte[] byteBuffer = Encoding.UTF8.GetBytes(result);
            XmlReaderDelegator xmlDelegator = new XmlReaderDelegator(XmlDictionaryReader.CreateTextReader(byteBuffer, XmlDictionaryReaderQuotas.Max));

            xmlDelegator.MoveToContent();

            return xmlDelegator;
        }

        private static Dictionary<string, string> ReverseDictionary(Dictionary<string, string> inDict)
        {
            Dictionary<string, string> mapDict = new Dictionary<string, string>();
            foreach (string key in inDict.Keys)
            {
                string valString;
                valString = inDict[key];
                mapDict.Add(valString, key);
            }
            return mapDict;
        }
    }

    /*
     * This class is necessary to create a parsed xml document to utilize the ClassDataContract.
     * The ExceptionXmlParser inserts necessary xmlns declarations for exception members, the namespace is necessary for ClassDataContract to identify
     * the xml members as members of the Exception object.
     * This ExceptionXmlParser also performs the transformation of the serialized names of exception private fields to the actual field names.
     */
    internal sealed class ExceptionXmlParser : IDisposable
    {
        private XmlReader _reader;
        private MemoryStream _ms;
        private StreamWriter _sw;
        private StringBuilder _sb;
        private string _exceptionNamespace;
        private Dictionary<string, string> _elementNamesToMap;
        private bool _disposed;

        private ExceptionXmlParser(Dictionary<string, string> dictMap, string exceptionNamespace)
        {
            // dictMap passes in the dictionary that is used for mapping field names to their serialized representations.
            if (dictMap == null)
            {
                throw new ArgumentNullException("dictMap");
            }

            if (exceptionNamespace == null)
            {
                throw new ArgumentNullException("exceptionNamespace");
            }

            _elementNamesToMap = dictMap;
            _exceptionNamespace = exceptionNamespace;
            _sb = new StringBuilder();
            _ms = new MemoryStream();
            _sw = new StreamWriter(_ms);
            _disposed = false;
        }

        public static string ParseExceptionXmlForClassDataContract(Dictionary<string, string> dictMap, string exceptionNamespace, string stringToParse)
        {
            using (ExceptionXmlParser parser = new ExceptionXmlParser(dictMap, exceptionNamespace))
            {
                return parser.ParseExceptionXml(stringToParse);
            }
        }

        private string ParseExceptionXml(string stringToParse)
        {
            _sw.Write(stringToParse);
            _sw.Flush();
            _ms.Position = 0;
            _reader = XmlReader.Create(_ms);

            return ParseRootElement();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _ms.Dispose();
                _ms = null;
                _sw.Dispose();
                _sw = null;
                _reader.Dispose();
                _reader = null;
                _disposed = true;
            }
        }

        private string ParseRootElement()
        {
            _reader.MoveToContent();
            string elementName = _reader.Name;
            OpenRootElement(elementName);
            if (_reader.IsEmptyElement)
            {
                _sb.Append("/>"); // close root element.
            }
            else
            {
                HandleRootElementsContents();
                CloseRootElement(elementName);
            }
            return _sb.ToString();
        }

        private void ParseChildElement()
        {
            string elementName = _reader.Name;
            elementName = OpenChildElement(elementName);
            if (_reader.IsEmptyElement)
            {
                InlineCloser();
            }
            else
            {
                _sb.Append(">");
                HandleChildElementContent();
                CloseElement(elementName);
            }
        }

        private string WriteElementNameWithBracketAndMapping(string name)
        {
            _sb.Append("<");
            name = SwitchElementNameIfNecessary(name);
            _sb.Append(name);

            return name;
        }

        private string WriteExactElementNameWithBracket(string name)
        {
            _sb.AppendFormat("<{0}", name);
            return name;
        }

        private string SwitchElementNameIfNecessary(string name)
        {
            string newName;
            if (_elementNamesToMap.TryGetValue(name, out newName))
            {
                return newName;
            }
            return name;
        }

        private void InlineCloser()
        {
            _sb.Append("/>");
            Read();
        }

        private void OpenRootElement(string elementName)
        {
            WriteElementNameWithBracketAndMapping(elementName);
            for (int i = 0; i < _reader.AttributeCount; i++)
            {
                _reader.MoveToNextAttribute();
                WriteAttribute(_reader.Name, _reader.Value);
            }
            _reader.MoveToElement();
        }

        private void CloseRootElement(string ElementName)
        {
            _sb.AppendFormat("</{0}>", ElementName);
        }

        private void HandleRootElementsContents()
        {
            _sb.Append(">");
            Read();
            while (_reader.NodeType == XmlNodeType.Element)
            {
                ParseChildElement();
            }
        }

        private string OpenChildElement(string elementName)
        {
            elementName = WriteElementNameWithBracketAndMapping(elementName);

            // the immediate child elements must have an xmlns attribute with namespace of the parent exception.
            WriteAttribute("xmlns", _exceptionNamespace);
            for (int i = 0; i < _reader.AttributeCount; i++)
            {
                _reader.MoveToNextAttribute();
                if (_reader.Name.Equals("xmlns"))
                    continue;
                WriteAttribute(_reader.Name, _reader.Value);
            }
            _reader.MoveToElement();
            return elementName;
        }

        private void CloseElement(string ElementName)
        {
            _sb.AppendFormat("</{0}>", ElementName);
        }

        private void CopyXmlFromCurrentNode()
        {
            string elementName = _reader.Name;

            // Start with the element
            OpenExactElement(elementName);

            // Handle an empty element
            if (_reader.IsEmptyElement)
            {
                InlineCloser();
                return;
            }
            _sb.Append(">");

            // Append all children elements
            Read();
            while (_reader.NodeType == XmlNodeType.Element)
            {
                CopyXmlFromCurrentNode(); // Recursive call
            }

            if (_reader.NodeType == XmlNodeType.Text)
            {
                _sb.Append(_reader.Value);
                Read();
            }

            // Finish with the element
            CloseExactElement(elementName);
        }

        private void OpenExactElement(string elementName)
        {
            WriteExactElementNameWithBracket(elementName);
            for (int i = 0; i < _reader.AttributeCount; i++)
            {
                _reader.MoveToNextAttribute();
                WriteAttribute(_reader.Name, _reader.Value);
            }
            _reader.MoveToElement();
        }

        private void CloseExactElement(string elementName)
        {
            _sb.AppendFormat("</{0}>", elementName);
            Read();
        }

        private void WriteAttribute(string name, string value)
        {
            _sb.AppendFormat(" {0}=\"{1}\"", name, value);
        }

        private void HandleChildElementContent()
        {
            Read();
            if (_reader.NodeType == XmlNodeType.Text)
            {
                _sb.Append(_reader.Value);
                Read();
                _reader.ReadEndElement();
            }
            else
            {
                while (_reader.NodeType != XmlNodeType.EndElement)
                {
                    CopyXmlFromCurrentNode();
                }
                _reader.ReadEndElement();
            }
        }

        private void Read()
        {
            //There is no reason the reader should return false for properly serialized xml.
            if (!_reader.Read())
            {
                throw new InvalidOperationException();
            }
        }
    }
}
