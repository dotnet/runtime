using System.Runtime.Serialization;
using System.Xml.Serialization;

[assembly: ContractNamespace("http://special1.tempuri.org", ClrNamespace = "System.Runtime.Serialization.Schema.Tests.DataContracts")]

namespace System.Runtime.Serialization.Schema.Tests.DataContracts
{
    [DataContract(Namespace = "http://basic")]
    public class Point
    {
        [DataMember]
        public int X = 42;
        [DataMember]
        public int Y = 43;
    }

    [DataContract(Namespace = "http://shapes")]
    public class Circle
    {
        [DataMember]
        public Point Center = new Point();
        [DataMember]
        public int Radius = 5;
    }

    [DataContract(Namespace = "http://shapes")]
    public class Square
    {
        [DataMember]
        public Point BottomLeft = new Point();
        [DataMember]
        public int Side = 5;
    }

    public struct NonAttributedPersonStruct
    {
        public string firstName;
        public string lastName;
    }

    public class NonAttributedPersonClass
    {
        public string firstName = "John";
        public string lastName = "Smith";

        internal NonAttributedPersonClass()
        {
        }
    }

    public class ExtendedSquare : Square
    {
        public string lineColor = "black";
    }

    [DataContract(Name = "AnotherValidType", Namespace = "http://schemas.datacontract.org/2004/07/barNs")]
    public class AnotherValidType
    {
        [DataMember]
        public string member;
    }

    [DataContract(Name = "AnotherValidType", Namespace = "http://schemas.datacontract.org/2004/07/barNs")]
    public class ConflictingAnotherValidType
    {
        [DataMember]
        public string member;
    }

    public class NonAttributedType
    {
        public NonAttributedSquare Length;
    }

    public class NonAttributedSquare
    {
        public int Length;
    }

    [DataContract(IsReference = true)]
    public class RefType1
    {
    }

    [DataContract]
    public class NonRefType
    {
    }

    [Serializable]
    public class ISerializableFormatClass : ISerializable
    {
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
        }
    }

#pragma warning disable CS0169, IDE0051, IDE1006
    #region SurrogateTests
    [XmlRoot("XmlSerializerPersonElement")]
    public class XmlSerializerPerson
    {
        public XmlSerializerPerson() { }
        [XmlAttribute]
        public string Name;
        [XmlAttribute]
        public int Age;
    }

    [DataContract]
    public class CircleContainer
    {
        [DataMember]
        public SerializableCircle Circle { get { return null; } set { } }
        [DataMember]
        SerializableCircle[] circles;
    }

    [Serializable]
    public class SerializableCircle
    {
        public int Radius;
    }

    [Serializable]
    public class SerializableSquare
    {
        public int Side;
    }

    public class Node
    {
        Node next;
    }

    [Serializable]
    public class SerializableNode
    {
        SerializableNode next;
    }
    #endregion

    [DataContract]
    public class SerializableClass
    {
        [DataMember]
        string member;

        [DataMember(Order = 3)]
        string v3member;
    }

    [DataContract]
    public class DerivedClass : SerializableClass
    {
        [DataMember]
        SerializableClass[] derivedMember;
    }
#pragma warning restore CS0169, IDE0051, IDE1006
}
