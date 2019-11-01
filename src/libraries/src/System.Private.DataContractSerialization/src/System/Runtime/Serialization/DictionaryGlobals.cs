// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;

namespace System.Runtime.Serialization
{
#if USE_REFEMIT
    public static class DictionaryGlobals
#else
    internal static class DictionaryGlobals
#endif
    {
        // Update array size when adding new strings or templates
        private static readonly XmlDictionary s_dictionary = new XmlDictionary(61);

        // 0
        public static readonly XmlDictionaryString SchemaInstanceNamespace = s_dictionary.Add(Globals.SchemaInstanceNamespace);
        public static readonly XmlDictionaryString SerializationNamespace = s_dictionary.Add(Globals.SerializationNamespace);
        public static readonly XmlDictionaryString SchemaNamespace = s_dictionary.Add(Globals.SchemaNamespace);
        public static readonly XmlDictionaryString XsiTypeLocalName = s_dictionary.Add(Globals.XsiTypeLocalName);
        public static readonly XmlDictionaryString XsiNilLocalName = s_dictionary.Add(Globals.XsiNilLocalName);

        // 5
        public static readonly XmlDictionaryString IdLocalName = s_dictionary.Add(Globals.IdLocalName);
        public static readonly XmlDictionaryString RefLocalName = s_dictionary.Add(Globals.RefLocalName);
        public static readonly XmlDictionaryString ArraySizeLocalName = s_dictionary.Add(Globals.ArraySizeLocalName);
        public static readonly XmlDictionaryString EmptyString = s_dictionary.Add(string.Empty);
        public static readonly XmlDictionaryString ISerializableFactoryTypeLocalName = s_dictionary.Add(Globals.ISerializableFactoryTypeLocalName);

        // 10
        public static readonly XmlDictionaryString XmlnsNamespace = s_dictionary.Add(Globals.XmlnsNamespace);
        public static readonly XmlDictionaryString CharLocalName = s_dictionary.Add("char");
        public static readonly XmlDictionaryString BooleanLocalName = s_dictionary.Add("boolean");
        public static readonly XmlDictionaryString SignedByteLocalName = s_dictionary.Add("byte");
        public static readonly XmlDictionaryString UnsignedByteLocalName = s_dictionary.Add("unsignedByte");

        // 15
        public static readonly XmlDictionaryString ShortLocalName = s_dictionary.Add("short");
        public static readonly XmlDictionaryString UnsignedShortLocalName = s_dictionary.Add("unsignedShort");
        public static readonly XmlDictionaryString IntLocalName = s_dictionary.Add("int");
        public static readonly XmlDictionaryString UnsignedIntLocalName = s_dictionary.Add("unsignedInt");
        public static readonly XmlDictionaryString LongLocalName = s_dictionary.Add("long");

        // 20
        public static readonly XmlDictionaryString UnsignedLongLocalName = s_dictionary.Add("unsignedLong");
        public static readonly XmlDictionaryString FloatLocalName = s_dictionary.Add("float");
        public static readonly XmlDictionaryString DoubleLocalName = s_dictionary.Add("double");
        public static readonly XmlDictionaryString DecimalLocalName = s_dictionary.Add("decimal");
        public static readonly XmlDictionaryString DateTimeLocalName = s_dictionary.Add("dateTime");

        // 25
        public static readonly XmlDictionaryString StringLocalName = s_dictionary.Add("string");
        public static readonly XmlDictionaryString ByteArrayLocalName = s_dictionary.Add("base64Binary");
        public static readonly XmlDictionaryString ObjectLocalName = s_dictionary.Add("anyType");
        public static readonly XmlDictionaryString TimeSpanLocalName = s_dictionary.Add("duration");
        public static readonly XmlDictionaryString GuidLocalName = s_dictionary.Add("guid");

        // 30
        public static readonly XmlDictionaryString UriLocalName = s_dictionary.Add("anyURI");
        public static readonly XmlDictionaryString QNameLocalName = s_dictionary.Add("QName");
        public static readonly XmlDictionaryString ClrTypeLocalName = s_dictionary.Add(Globals.ClrTypeLocalName);
        public static readonly XmlDictionaryString ClrAssemblyLocalName = s_dictionary.Add(Globals.ClrAssemblyLocalName);
        public static readonly XmlDictionaryString Space = s_dictionary.Add(Globals.Space);

        // 35
        public static readonly XmlDictionaryString timeLocalName = s_dictionary.Add("time");
        public static readonly XmlDictionaryString dateLocalName = s_dictionary.Add("date");
        public static readonly XmlDictionaryString hexBinaryLocalName = s_dictionary.Add("hexBinary");
        public static readonly XmlDictionaryString gYearMonthLocalName = s_dictionary.Add("gYearMonth");
        public static readonly XmlDictionaryString gYearLocalName = s_dictionary.Add("gYear");

        // 40
        public static readonly XmlDictionaryString gMonthDayLocalName = s_dictionary.Add("gMonthDay");
        public static readonly XmlDictionaryString gDayLocalName = s_dictionary.Add("gDay");
        public static readonly XmlDictionaryString gMonthLocalName = s_dictionary.Add("gMonth");
        public static readonly XmlDictionaryString integerLocalName = s_dictionary.Add("integer");
        public static readonly XmlDictionaryString positiveIntegerLocalName = s_dictionary.Add("positiveInteger");

        // 45
        public static readonly XmlDictionaryString negativeIntegerLocalName = s_dictionary.Add("negativeInteger");
        public static readonly XmlDictionaryString nonPositiveIntegerLocalName = s_dictionary.Add("nonPositiveInteger");
        public static readonly XmlDictionaryString nonNegativeIntegerLocalName = s_dictionary.Add("nonNegativeInteger");
        public static readonly XmlDictionaryString normalizedStringLocalName = s_dictionary.Add("normalizedString");
        public static readonly XmlDictionaryString tokenLocalName = s_dictionary.Add("token");

        // 50
        public static readonly XmlDictionaryString languageLocalName = s_dictionary.Add("language");
        public static readonly XmlDictionaryString NameLocalName = s_dictionary.Add("Name");
        public static readonly XmlDictionaryString NCNameLocalName = s_dictionary.Add("NCName");
        public static readonly XmlDictionaryString XSDIDLocalName = s_dictionary.Add("ID");
        public static readonly XmlDictionaryString IDREFLocalName = s_dictionary.Add("IDREF");

        // 55
        public static readonly XmlDictionaryString IDREFSLocalName = s_dictionary.Add("IDREFS");
        public static readonly XmlDictionaryString ENTITYLocalName = s_dictionary.Add("ENTITY");
        public static readonly XmlDictionaryString ENTITIESLocalName = s_dictionary.Add("ENTITIES");
        public static readonly XmlDictionaryString NMTOKENLocalName = s_dictionary.Add("NMTOKEN");
        public static readonly XmlDictionaryString NMTOKENSLocalName = s_dictionary.Add("NMTOKENS");

        // 60
        public static readonly XmlDictionaryString AsmxTypesNamespace = s_dictionary.Add("http://microsoft.com/wsdl/types/");
    }
}
