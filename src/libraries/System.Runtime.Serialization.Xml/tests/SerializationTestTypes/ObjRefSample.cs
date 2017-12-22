// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace SerializationTestTypes
{
    [DataContract(IsReference = true)]
    public class SimpleDC
    {
        [DataMember]
        public string Data;
        public SimpleDC() { }
        public SimpleDC(bool init)
        {
            Data = "This is a string";
        }
    }

    [DataContract(IsReference = true)]
    public class SimpleDCWithSimpleDMRef
    {
        [DataMember]
        public string Data;

        [DataMember]
        public string RefData;

        public SimpleDCWithSimpleDMRef() { }
        public SimpleDCWithSimpleDMRef(bool init)
        {
            Data = "This is a string";
            RefData = Data;
        }
    }

    [DataContract(IsReference = true)]
    public class SimpleDCWithRef
    {
        [DataMember]
        public SimpleDC Data;

        [DataMember]
        public SimpleDC RefData;

        public SimpleDCWithRef() { }
        public SimpleDCWithRef(bool init)
        {
            Data = new SimpleDC(true);
            RefData = Data;
        }
    }

    [DataContract(IsReference = true)]
    public class ContainsSimpleDCWithRef
    {
        [DataMember]
        public SimpleDCWithRef Data;

        [DataMember]
        public SimpleDCWithRef RefData;

        public ContainsSimpleDCWithRef() { }
        public ContainsSimpleDCWithRef(bool init)
        {
            Data = new SimpleDCWithRef(true);
            RefData = Data;
        }
    }

    [DataContract]
    public struct SimpleStructDC
    {
        [DataMember]
        public string Data;

        public SimpleStructDC(bool init)
        {
            Data = "This is a string";
        }
    }

    [DataContract(IsReference = true)]
    public class SimpleDCWithIsRequiredFalse
    {
        [DataMember(IsRequired = false)]
        public string Data;
        public SimpleDCWithIsRequiredFalse() { }
        public SimpleDCWithIsRequiredFalse(bool init)
        {
            Data = "This is a string";
        }
    }
    
    [DataContract]
    public struct SimpleStructDCWithRef
    {
        [DataMember]
        public SimpleStructDC Data;

        [DataMember]
        public SimpleStructDC RefData;
        public SimpleStructDCWithRef(bool init)
        {
            Data = new SimpleStructDC(true);
            RefData = Data;
        }
    }

    [DataContract(IsReference = true)]
    public class Mixed1
    {
        [DataMember]
        public SimpleDC Data1;

        [DataMember]
        public SimpleDCWithRef Data2;

        [DataMember]
        public SimpleDCWithSimpleDMRef Data3;

        [DataMember]
        public SimpleStructDC Data4;

        [DataMember]
        public SimpleStructDCWithRef Data5;

        public Mixed1() { }
        public Mixed1(bool init)
        {
            Data1 = new SimpleDC(true);
            Data2 = new SimpleDCWithRef(true);
            Data3 = new SimpleDCWithSimpleDMRef(true);
            Data4 = new SimpleStructDC(true);
            Data5 = new SimpleStructDCWithRef(true);
        }
    }

    [DataContract(IsReference = true, Name = "DCVersioned", Namespace = "SerializationTestTypes.ExtensionData")]
    public class DCVersioned1 : IExtensibleDataObject
    {
        [DataMember]
        public SimpleDC Data;

        [DataMember]
        public SimpleDC RefData;
        public DCVersioned1() { }
        public DCVersioned1(bool init)
        {
            this.Data = new SimpleDC(true);
            this.RefData = this.Data;
        }

        private ExtensionDataObject _extensionData;

        public ExtensionDataObject ExtensionData
        {
            get
            {
                return _extensionData;
            }
            set
            {
                _extensionData = value;
            }
        }
    }

    [DataContract(IsReference = true, Name = "DCVersioned2", Namespace = "SerializationTestTypes.ExtensionData")]
    public class DCVersioned2 : IExtensibleDataObject
    {
        [DataMember]
        public SimpleDC Data;

        public DCVersioned2() { }
        public DCVersioned2(bool init)
        {
            this.Data = new SimpleDC(true);
        }

        private ExtensionDataObject _extensionData;

        public ExtensionDataObject ExtensionData
        {
            get
            {
                return _extensionData;
            }
            set
            {
                _extensionData = value;
            }
        }
    }

    [DataContract(IsReference = true, Name = "DCVersionedContainer", Namespace = "SerializationTestTypes.ExtensionData")]
    public class DCVersionedContainer1 : IExtensibleDataObject
    {
        [DataMember]
        public DCVersioned1 DataVersion1;

        public DCVersionedContainer1() { }
        public DCVersionedContainer1(bool init)
        {
            this.DataVersion1 = new DCVersioned1(true);
        }

        private ExtensionDataObject _extensionData;

        public ExtensionDataObject ExtensionData
        {
            get
            {
                return _extensionData;
            }
            set
            {
                _extensionData = value;
            }
        }
    }

    [DataContract(IsReference = true, Name = "DCVersionedContainerV1", Namespace = "SerializationTestTypes.ExtensionData")]
    public class DCVersionedContainerVersion1 : IExtensibleDataObject
    {
        [DataMember]
        public DCVersioned1 DataVersion1;

        [DataMember]
        public DCVersioned2 DataVersion2;

        [DataMember]
        public DCVersioned1 RefDataVersion1;

        [DataMember]
        public DCVersioned2 RefDataVersion2;


        public DCVersionedContainerVersion1() { }
        public DCVersionedContainerVersion1(bool init)
        {
            this.DataVersion1 = new DCVersioned1(true);
            this.DataVersion2 = new DCVersioned2(true);

            this.RefDataVersion1 = this.DataVersion1;
            this.RefDataVersion2 = this.DataVersion2;
        }

        private ExtensionDataObject _extensionData;

        public ExtensionDataObject ExtensionData
        {
            get
            {
                return _extensionData;
            }
            set
            {
                _extensionData = value;
            }
        }
    }

    [DataContract(IsReference = true, Name = "DCVersionedContainerV2", Namespace = "SerializationTestTypes.ExtensionData")]
    public class DCVersionedContainerVersion2 : IExtensibleDataObject
    {
        [DataMember]
        public DCVersioned1 DataVersion1;

        [DataMember]
        public DCVersioned2 DataVersion2;

        public DCVersionedContainerVersion2() { }
        public DCVersionedContainerVersion2(bool init)
        {
            this.DataVersion1 = new DCVersioned1(true);
            this.DataVersion2 = new DCVersioned2(true);
        }

        private ExtensionDataObject _extensionData;

        public ExtensionDataObject ExtensionData
        {
            get
            {
                return _extensionData;
            }
            set
            {
                _extensionData = value;
            }
        }
    }

    [DataContract(IsReference = true, Name = "DCVersionedContainerV3", Namespace = "SerializationTestTypes.ExtensionData")]
    [KnownType(typeof(DCVersioned1))]
    public class DCVersionedContainerVersion3 : IExtensibleDataObject
    {
        [DataMember(Name = "DCVersioned1")]
        public object DataVersion1;

        [DataMember]
        public object RefDataVersion2;

        [DataMember]
        public DCVersioned1 NewRefDataVersion1;

        public DCVersioned2 NewDataVersion2;

        public DCVersionedContainerVersion3() { }
        public DCVersionedContainerVersion3(bool init)
        {
            this.DataVersion1 = new DCVersioned1(true);
            this.NewRefDataVersion1 = (DCVersioned1)this.DataVersion1;
            this.RefDataVersion2 = this.NewDataVersion2;
        }

        private ExtensionDataObject _extensionData;

        public ExtensionDataObject ExtensionData
        {
            get
            {
                return _extensionData;
            }
            set
            {
                _extensionData = value;
            }
        }
    }

    [Serializable]
    [KnownType(typeof(PublicDC))]
    public class SerIser : ISerializable
    {
        [NonSerialized]
        public PublicDC containedData = new PublicDC();

        public SerIser() { }

        public SerIser(SerializationInfo info, StreamingContext context)
        {
            containedData = (PublicDC)info.GetValue("containedData", typeof(PublicDC));
        }

        public override bool Equals(object obj)
        {
            return obj.Equals(containedData);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("containedData", containedData);
        }
    }

    public class IgnoreMemberAttribute : Attribute
    {
    }
}
