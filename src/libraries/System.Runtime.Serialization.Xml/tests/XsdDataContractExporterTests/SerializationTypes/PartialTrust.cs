using System;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;

[assembly:AllowPartiallyTrustedCallers]
#if UseSeparateAssemblyNamespace
namespace SerializableTypes.XsdDataContractExporterTests
#else
namespace System.Runtime.Serialization.Xml.XsdDataContractExporterTests
#endif
{
    public class PartialTrust
    {
        [Serializable]
        //[SerializationPermissionNotRequired]
        public class SafePoint
        {
            int x = 42, y = 43;
        }

        [Serializable]
        //[SerializationPermissionNotRequired]
        public class SafePoint3D : SafePoint
        {
            int z = 44;
            DateTimeOffset dateCreated = new DateTimeOffset(new DateTime(1997, 03, 11, 07, 15, 30), new TimeSpan(1,2,60));
        }

        [Serializable]
        //[SerializationPermissionNotRequired]
        public class SafeCube
        {
            SafePoint3D topLeftBehind = new SafePoint3D();
            SafePoint3D bottomRightFront = new SafePoint3D();
        }

        [Serializable]
        public class UnsafePoint
        {
            int x = 42, y = 43;
        }

        [Serializable]
        //[SerializationPermissionNotRequired]
        public class UnsafePoint3D : UnsafePoint
        {
            int z = 44;
        }
        
        [Serializable]
        //[SerializationPermissionNotRequired]
        public class UnsafeCube
        {
            UnsafePoint3D topLeftBehind = new UnsafePoint3D();
            UnsafePoint3D bottomRightFront = new UnsafePoint3D();
        }

        //[SerializationPermissionNotRequired]
        public class AttributeOnlyIXmlSerializable : IXmlSerializable
        {
            public AttributeOnlyIXmlSerializable()
            {
                // This was not commented in NetFx. It clutters output though and seems unnecessary for our needs.
                //Console.WriteLine("Default Ctor");
            }

            public AttributeOnlyIXmlSerializable(string init)
            {

            }

            public XmlSchema GetSchema()
            {
                return null;
            }

            public void ReadXml(XmlReader reader)
            {
                Console.WriteLine(reader.NodeType + " " + reader.Name);
                Console.WriteLine("Value1 = " + reader.GetAttribute("myAttribute1"));
                Console.WriteLine("Value2 = " + reader.GetAttribute("myAttribute2"));
            }

            public void WriteXml(XmlWriter writer)
            {
                writer.WriteAttributeString("myAttribute1", "", "myAttribute1Value");
                writer.WriteAttributeString("myAttribute2", "", "myAttribute2Value");
            }
        }

        public class UnsafeAttributeOnlyIXmlSerializable : AttributeOnlyIXmlSerializable
        {
            public UnsafeAttributeOnlyIXmlSerializable()
            {
                //may be called to invoke GetSchema() method
            }

            public UnsafeAttributeOnlyIXmlSerializable(string init)
            {

            }

        }
    }
}

