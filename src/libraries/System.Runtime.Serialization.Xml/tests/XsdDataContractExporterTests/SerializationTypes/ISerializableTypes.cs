using System;
using System.Collections;
using System.Runtime.Serialization;
using System.Security;

#if UseSeparateAssemblyNamespace
namespace SerializableTypes.XsdDataContractExporterTests
#else
namespace System.Runtime.Serialization.Xml.XsdDataContractExporterTests
#endif
{
    [Serializable]
    public class BaseISerializable : ISerializable
    {
        protected BaseISerializable()
        {
        }

        protected BaseISerializable(SerializationInfo info, StreamingContext context)
        {
        }

        public string street;
        int zip;
        [NonSerialized]
        float privateData;

        [SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
        }
    }

    [Serializable]
    public class DerivedISerializable : BaseISerializable
    {
        DerivedISerializable(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

    }

    [Serializable]
    public class MyUri : Uri
    {
        protected MyUri(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class MyDerivedUri : MyUri
    {
        protected MyDerivedUri(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public struct StructISerializable : ISerializable
    {
        public StructISerializable(SerializationInfo serInfo, StreamingContext context)
        {
        }

        [SecurityCritical]
        public void GetObjectData(SerializationInfo serInfo, StreamingContext context)
        {
        }

    }

    [DataContract]
    public class UseISerializable
    {
        [DataMember]
        Hashtable hashtable;

        [DataMember]
        InvalidOperationException exception;

#if !HideTypesWithoutSerializableAttribute
        [DataMember]
        System.Reflection.Assembly assembly;

        [DataMember]
        System.IO.DirectoryInfo directoryInfo;
#endif
        [DataMember]
        StructISerializable structISerMember;

        [DataMember]
        BaseISerializable classISerMember;
    }

    [Serializable]
    public class ISerializableDerivingDC : DataContractTypes.Address, ISerializable
    {
        public ISerializableDerivingDC(SerializationInfo serInfo, StreamingContext context)
        {
        }

        [SecurityCritical]
        void ISerializable.GetObjectData(SerializationInfo serInfo, StreamingContext context)
        {
        }
    }

}

