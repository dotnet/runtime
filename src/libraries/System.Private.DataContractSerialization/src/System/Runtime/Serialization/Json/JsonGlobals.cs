// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Xml;
using System.Security;
using System.Reflection;
using System.Text;


namespace System.Runtime.Serialization.Json
{
    internal static class JsonGlobals
    {
        public const char QuoteChar = '"';
        public const string KeyString = "Key";
        public const string ValueString = "Value";
        public const string ServerTypeString = "__type";
        public static readonly int DataContractXsdBaseNamespaceLength = Globals.DataContractXsdBaseNamespace.Length;
        public static readonly long unixEpochTicks = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
        public static readonly SecurityException SecurityException = new SecurityException();
        public static readonly UnicodeEncoding ValidatingBEUTF16 = new UnicodeEncoding(true, false, true);
        public static readonly UnicodeEncoding ValidatingUTF16 = new UnicodeEncoding(false, false, true);
        public static readonly UTF8Encoding ValidatingUTF8 = new UTF8Encoding(false, true);
        public const string PositiveInf = "INF";
        public const string NegativeInf = "-INF";
        public const string typeString = "type";
        public const string nullString = "null";
        public const string arrayString = "array";
        public const string booleanString = "boolean";
        public const string stringString = "string";
        public const string numberString = "number";
        public const string objectString = "object";
        public const string itemString = "item";
        public const string rootString = "root";
        public const string NameValueSeparatorString = ":";
        public const string DateTimeEndGuardReader = ")/";
        public const string DateTimeEndGuardWriter = ")\\/";
        public const string DateTimeStartGuardReader = "/Date(";
        public const string DateTimeStartGuardWriter = "\\/Date(";
        public const string xmlnsPrefix = "xmlns";
        public const string xmlPrefix = "xml";
        public const byte EndCollectionByte = (byte)EndCollectionChar;
        public const char EndCollectionChar = ']';
        public const byte EndObjectByte = (byte)EndObjectChar;
        public const char EndObjectChar = '}';
        public const byte MemberSeparatorByte = (byte)MemberSeparatorChar;
        public const char MemberSeparatorChar = ',';
        public const byte NameValueSeparatorByte = (byte)NameValueSeparatorChar;
        public const char NameValueSeparatorChar = ':';
        public const byte QuoteByte = (byte)QuoteChar;
        public const byte ObjectByte = (byte)ObjectChar;
        public const char ObjectChar = '{';
        public const byte CollectionByte = (byte)CollectionChar;
        public const char CollectionChar = '[';
        public const char WhitespaceChar = ' ';
        public const string serverTypeString = "__type";
        public const int maxScopeSize = 25;
        public static readonly XmlDictionaryString itemDictionaryString = new XmlDictionary().Add("item");
        public static readonly XmlDictionaryString rootDictionaryString = new XmlDictionary().Add("root");
    }
}
