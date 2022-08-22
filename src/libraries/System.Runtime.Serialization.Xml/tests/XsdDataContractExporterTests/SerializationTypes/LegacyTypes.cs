using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Security;
using System.Runtime.Serialization;

#if UseSeparateAssemblyNamespace
namespace SerializableTypes.XsdDataContractExporterTests
#else
namespace System.Runtime.Serialization.Xml.XsdDataContractExporterTests
#endif
{
    [Serializable]
    [SecuritySafeCritical]
#if UseSeparateAssemblyNamespace
    public unsafe class LegacyTypes
#else
    public class LegacyTypes
#endif
    {
        Hashtable h;
        List<int> lInt;
        public IList list;
        public IList<string> stringList;
        public ICollection collection;
        public ICollection<string> stringCollection;
        public IEnumerable enumerable;
        public IEnumerable<string> stringEnumerable;
        public IDictionary dictionary;
        public IDictionary<string, int> dictionaryOfStringToInt;
        Dictionary<Exception, Version> dExVer;
#if !HideTypesWithoutSerializableAttribute
        float* f;
#endif
        IntPtr iPtr;
        DBNull dbNull;
    }

}


