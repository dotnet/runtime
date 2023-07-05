using System;
using System.Runtime.Serialization;
using System.Security;

#if UseSeparateAssemblyNamespace
namespace SerializableTypes.XsdDataContractExporterTests
#else
namespace System.Runtime.Serialization.Xml.XsdDataContractExporterTests
#endif
{
    [DataContract]
    public struct Point
    {

        Nullable<int> x;
        Nullable<int> y;

        [DataMember]
        public Nullable<int> X { get { return x; } set { x = value; } }
        [DataMember]
        public Nullable<int> Y { get { return y; } set { y = value; } }

    }

    [DataContract]
    public struct Rectangle
    {
        [DataMember]
        public Nullable<Point> TopLeft;
        [DataMember]
        public Point? BottomRight;
    }

    [Serializable]
    [KnownType(typeof(Point))]
    public struct Polygon : ISerializable
    {
        Nullable<Point>[] points;

        [SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
        }
    }

    [Serializable]
    [KnownType(typeof(Polygon))]
    [KnownType(typeof(Nullable<Polygon>))]
    [KnownType(typeof(Container))]
    public class Container
    {
        Polygon polygon;
        Rectangle? excessivelyNullableRectangle; //not excessively anymore
        Nullable<int>[] nullableInts;
        Nullable<long>[][] nullableLongss;
    }

    [DataContract]
    public struct NullableDateTimeOffset
    {
        Nullable<DateTimeOffset> nullableDTO;

        [DataMember]
        public Nullable<DateTimeOffset> NullableDTO { get { return nullableDTO; } set { nullableDTO = value; } }
    }
}
