using System;
using System.Runtime.Serialization;

#if UseSeparateAssemblyNamespace
namespace SerializableTypes.XsdDataContractExporterTests
#else
namespace System.Runtime.Serialization.Xml.XsdDataContractExporterTests
#endif
{
    public class ConflictingNameTypes
    {
        [DataContract]
        public class ConflictBase
        {
            [DataMember(IsRequired=true)]
            int a;
        }
        
        [DataContract]
        public class ConflictDerived1 : ConflictBase
        {
            [DataMember(IsRequired = true)]
            int a;
        }
        
        [DataContract]
        public class ConflictDerived2 : ConflictBase
        {
            [DataMember(IsRequired = true)]
            string[] a;
        }
        
        [DataContract]
        public class ConflictDerived11 : ConflictDerived1
        {
            [DataMember(IsRequired = true)]
            int a;
        }
        
        [DataContract]
        public class ConflictDerived12 : ConflictDerived1
        {
            [DataMember(IsRequired = true)]
            string a;
        }

        [DataContract]
        public class NoConflictBase
        {
            [DataMember(IsRequired = true)]
            int a;
        }
        
        [DataContract]
        public class NoConflictDerived1 : NoConflictBase
        {
            [DataMember(IsRequired = true)]
            int a;
        }
        
        [DataContract(Namespace="http://www.tempuri.org/")]
        public class NoConflictDerived2 : NoConflictBase
        {
            [DataMember(IsRequired = true)]
            string a;
        }
        
    }
}



