using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;

class XMLSchemaExamples
{
    public static int Main()
    {

        XmlSchema schema = new XmlSchema();

        // <xs:element name="cat" type="xs:string"/>
        XmlSchemaElement elementCat = new XmlSchemaElement();
        schema.Items.Add(elementCat);
        elementCat.Name = "cat";
        elementCat.SchemaTypeName = new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema");

        // <xs:element name="dog" type="xs:string"/>
        XmlSchemaElement elementDog = new XmlSchemaElement();
        schema.Items.Add(elementDog);
        elementDog.Name = "dog";
        elementDog.SchemaTypeName = new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema");

        // <xs:element name="redDog" substitutionGroup="dog" />
        XmlSchemaElement elementRedDog = new XmlSchemaElement();
        schema.Items.Add(elementRedDog);
        elementRedDog.Name = "redDog";
        elementRedDog.SubstitutionGroup = new XmlQualifiedName("dog");

        // <xs:element name="brownDog" substitutionGroup ="dog" />
        XmlSchemaElement elementBrownDog = new XmlSchemaElement();
        schema.Items.Add(elementBrownDog);
        elementBrownDog.Name = "brownDog";
        elementBrownDog.SubstitutionGroup = new XmlQualifiedName("dog");

        // <xs:element name="pets">
        XmlSchemaElement elementPets = new XmlSchemaElement();
        schema.Items.Add(elementPets);
        elementPets.Name = "pets";

        using (var stream = new MemoryStream())
        using (var writer = XmlWriter.Create(stream))
        {
            schema.Write(writer, null);
        }

        return 100;
    }
}