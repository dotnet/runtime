[assembly:System.Security.AllowPartiallyTrustedCallers()]
[assembly:System.Security.SecurityTransparent()]
[assembly:System.Security.SecurityRules(System.Security.SecurityRuleSet.Level1)]
[assembly:System.Xml.Serialization.XmlSerializerVersionAttribute(ParentAssemblyId=@"%%ParentAssemblyId%%", Version=@"1.0.0.0")]
namespace Microsoft.Xml.Serialization.GeneratedAssembly {

    public class XmlSerializationWriter1 : System.Xml.Serialization.XmlSerializationWriter {

        public void Write111_TypeWithXmlElementProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithXmlElementProperty", @"");
                return;
            }
            TopLevelElement();
            Write2_TypeWithXmlElementProperty(@"TypeWithXmlElementProperty", @"", ((global::TypeWithXmlElementProperty)o), true, false);
        }

        public void Write112_TypeWithXmlDocumentProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithXmlDocumentProperty", @"");
                return;
            }
            TopLevelElement();
            Write3_TypeWithXmlDocumentProperty(@"TypeWithXmlDocumentProperty", @"", ((global::TypeWithXmlDocumentProperty)o), true, false);
        }

        public void Write113_TypeWithBinaryProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithBinaryProperty", @"");
                return;
            }
            TopLevelElement();
            Write4_TypeWithBinaryProperty(@"TypeWithBinaryProperty", @"", ((global::TypeWithBinaryProperty)o), true, false);
        }

        public void Write114_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithDateTimeOffsetProperties", @"");
                return;
            }
            TopLevelElement();
            Write5_Item(@"TypeWithDateTimeOffsetProperties", @"", ((global::TypeWithDateTimeOffsetProperties)o), true, false);
        }

        public void Write115_TypeWithTimeSpanProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithTimeSpanProperty", @"");
                return;
            }
            TopLevelElement();
            Write6_TypeWithTimeSpanProperty(@"TypeWithTimeSpanProperty", @"", ((global::TypeWithTimeSpanProperty)o), true, false);
        }

        public void Write116_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithDefaultTimeSpanProperty", @"");
                return;
            }
            TopLevelElement();
            Write7_Item(@"TypeWithDefaultTimeSpanProperty", @"", ((global::TypeWithDefaultTimeSpanProperty)o), true, false);
        }

        public void Write117_TypeWithByteProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithByteProperty", @"");
                return;
            }
            TopLevelElement();
            Write8_TypeWithByteProperty(@"TypeWithByteProperty", @"", ((global::TypeWithByteProperty)o), true, false);
        }

        public void Write118_TypeWithXmlNodeArrayProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithXmlNodeArrayProperty", @"");
                return;
            }
            TopLevelElement();
            Write9_TypeWithXmlNodeArrayProperty(@"TypeWithXmlNodeArrayProperty", @"", ((global::TypeWithXmlNodeArrayProperty)o), true, false);
        }

        public void Write119_Animal(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Animal", @"");
                return;
            }
            TopLevelElement();
            Write10_Animal(@"Animal", @"", ((global::Animal)o), true, false);
        }

        public void Write120_Dog(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Dog", @"");
                return;
            }
            TopLevelElement();
            Write12_Dog(@"Dog", @"", ((global::Dog)o), true, false);
        }

        public void Write121_DogBreed(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"DogBreed", @"");
                return;
            }
            WriteElementString(@"DogBreed", @"", Write11_DogBreed(((global::DogBreed)o)));
        }

        public void Write122_Group(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Group", @"");
                return;
            }
            TopLevelElement();
            Write14_Group(@"Group", @"", ((global::Group)o), true, false);
        }

        public void Write123_Vehicle(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Vehicle", @"");
                return;
            }
            TopLevelElement();
            Write13_Vehicle(@"Vehicle", @"", ((global::Vehicle)o), true, false);
        }

        public void Write124_Employee(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Employee", @"");
                return;
            }
            TopLevelElement();
            Write15_Employee(@"Employee", @"", ((global::Employee)o), true, false);
        }

        public void Write125_BaseClass(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"BaseClass", @"");
                return;
            }
            TopLevelElement();
            Write17_BaseClass(@"BaseClass", @"", ((global::BaseClass)o), true, false);
        }

        public void Write126_DerivedClass(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DerivedClass", @"");
                return;
            }
            TopLevelElement();
            Write16_DerivedClass(@"DerivedClass", @"", ((global::DerivedClass)o), true, false);
        }

        public void Write127_SimpleBaseClass(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"SimpleBaseClass", @"");
                return;
            }
            TopLevelElement();
            Write19_SimpleBaseClass(@"SimpleBaseClass", @"", ((global::SimpleBaseClass)o), true, false);
        }

        public void Write128_SimpleDerivedClass(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"SimpleDerivedClass", @"");
                return;
            }
            TopLevelElement();
            Write18_SimpleDerivedClass(@"SimpleDerivedClass", @"", ((global::SimpleDerivedClass)o), true, false);
        }

        public void Write129_BaseIXmlSerializable(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"BaseIXmlSerializable", @"http://example.com/serializer-test-namespace");
                return;
            }
            TopLevelElement();
            WriteSerializable((System.Xml.Serialization.IXmlSerializable)((global::XmlSerializableBaseClass)o), @"BaseIXmlSerializable", @"http://example.com/serializer-test-namespace", true, true);
        }

        public void Write130_DerivedIXmlSerializable(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DerivedIXmlSerializable", @"");
                return;
            }
            TopLevelElement();
            WriteSerializable((System.Xml.Serialization.IXmlSerializable)((global::XmlSerializableDerivedClass)o), @"DerivedIXmlSerializable", @"", true, true);
        }

        public void Write131_PurchaseOrder(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"PurchaseOrder", @"http://www.contoso1.com");
                return;
            }
            TopLevelElement();
            Write22_PurchaseOrder(@"PurchaseOrder", @"http://www.contoso1.com", ((global::PurchaseOrder)o), false, false);
        }

        public void Write132_Address(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Address", @"");
                return;
            }
            TopLevelElement();
            Write23_Address(@"Address", @"", ((global::Address)o), true, false);
        }

        public void Write133_OrderedItem(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"OrderedItem", @"");
                return;
            }
            TopLevelElement();
            Write24_OrderedItem(@"OrderedItem", @"", ((global::OrderedItem)o), true, false);
        }

        public void Write134_AliasedTestType(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"AliasedTestType", @"");
                return;
            }
            TopLevelElement();
            Write25_AliasedTestType(@"AliasedTestType", @"", ((global::AliasedTestType)o), true, false);
        }

        public void Write135_BaseClass1(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"BaseClass1", @"");
                return;
            }
            TopLevelElement();
            Write26_BaseClass1(@"BaseClass1", @"", ((global::BaseClass1)o), true, false);
        }

        public void Write136_DerivedClass1(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DerivedClass1", @"");
                return;
            }
            TopLevelElement();
            Write27_DerivedClass1(@"DerivedClass1", @"", ((global::DerivedClass1)o), true, false);
        }

        public void Write137_ArrayOfDateTime(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"ArrayOfDateTime", @"");
                return;
            }
            TopLevelElement();
            {
                global::MyCollection1 a = (global::MyCollection1)((global::MyCollection1)o);
                if ((object)(a) == null) {
                    WriteNullTagLiteral(@"ArrayOfDateTime", @"");
                }
                else {
                    WriteStartElement(@"ArrayOfDateTime", @"", null, false);
                    System.Collections.IEnumerator e = ((System.Collections.Generic.IEnumerable<global::System.DateTime>)a).GetEnumerator();
                    if (e != null)
                    while (e.MoveNext()) {
                        global::System.DateTime ai = (global::System.DateTime)e.Current;
                        WriteElementStringRaw(@"dateTime", @"", FromDateTime(((global::System.DateTime)ai)));
                    }
                    WriteEndElement();
                }
            }
        }

        public void Write138_Orchestra(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Orchestra", @"");
                return;
            }
            TopLevelElement();
            Write29_Orchestra(@"Orchestra", @"", ((global::Orchestra)o), true, false);
        }

        public void Write139_Instrument(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Instrument", @"");
                return;
            }
            TopLevelElement();
            Write28_Instrument(@"Instrument", @"", ((global::Instrument)o), true, false);
        }

        public void Write140_Brass(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Brass", @"");
                return;
            }
            TopLevelElement();
            Write30_Brass(@"Brass", @"", ((global::Brass)o), true, false);
        }

        public void Write141_Trumpet(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Trumpet", @"");
                return;
            }
            TopLevelElement();
            Write31_Trumpet(@"Trumpet", @"", ((global::Trumpet)o), true, false);
        }

        public void Write142_Pet(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Pet", @"");
                return;
            }
            TopLevelElement();
            Write32_Pet(@"Pet", @"", ((global::Pet)o), true, false);
        }

        public void Write143_DefaultValuesSetToNaN(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DefaultValuesSetToNaN", @"");
                return;
            }
            TopLevelElement();
            Write33_DefaultValuesSetToNaN(@"DefaultValuesSetToNaN", @"", ((global::DefaultValuesSetToNaN)o), true, false);
        }

        public void Write144_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DefaultValuesSetToPositiveInfinity", @"");
                return;
            }
            TopLevelElement();
            Write34_Item(@"DefaultValuesSetToPositiveInfinity", @"", ((global::DefaultValuesSetToPositiveInfinity)o), true, false);
        }

        public void Write145_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DefaultValuesSetToNegativeInfinity", @"");
                return;
            }
            TopLevelElement();
            Write35_Item(@"DefaultValuesSetToNegativeInfinity", @"", ((global::DefaultValuesSetToNegativeInfinity)o), true, false);
        }

        public void Write146_RootElement(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"RootElement", @"");
                return;
            }
            TopLevelElement();
            Write36_Item(@"RootElement", @"", ((global::TypeWithMismatchBetweenAttributeAndPropertyType)o), true, false);
        }

        public void Write147_TypeWithLinkedProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithLinkedProperty", @"");
                return;
            }
            TopLevelElement();
            Write37_TypeWithLinkedProperty(@"TypeWithLinkedProperty", @"", ((global::TypeWithLinkedProperty)o), true, false);
        }

        public void Write148_Document(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Document", @"http://example.com");
                return;
            }
            TopLevelElement();
            Write38_MsgDocumentType(@"Document", @"http://example.com", ((global::MsgDocumentType)o), true, false);
        }

        public void Write149_RootClass(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"RootClass", @"");
                return;
            }
            TopLevelElement();
            Write41_RootClass(@"RootClass", @"", ((global::RootClass)o), true, false);
        }

        public void Write150_Parameter(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Parameter", @"");
                return;
            }
            TopLevelElement();
            Write40_Parameter(@"Parameter", @"", ((global::Parameter)o), true, false);
        }

        public void Write151_XElementWrapper(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"XElementWrapper", @"");
                return;
            }
            TopLevelElement();
            Write42_XElementWrapper(@"XElementWrapper", @"", ((global::XElementWrapper)o), true, false);
        }

        public void Write152_XElementStruct(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"XElementStruct", @"");
                return;
            }
            Write43_XElementStruct(@"XElementStruct", @"", ((global::XElementStruct)o), false);
        }

        public void Write153_XElementArrayWrapper(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"XElementArrayWrapper", @"");
                return;
            }
            TopLevelElement();
            Write44_XElementArrayWrapper(@"XElementArrayWrapper", @"", ((global::XElementArrayWrapper)o), true, false);
        }

        public void Write154_TypeWithDateTimeStringProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithDateTimeStringProperty", @"");
                return;
            }
            TopLevelElement();
            Write45_TypeWithDateTimeStringProperty(@"TypeWithDateTimeStringProperty", @"", ((global::SerializationTypes.TypeWithDateTimeStringProperty)o), true, false);
        }

        public void Write155_SimpleType(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"SimpleType", @"");
                return;
            }
            TopLevelElement();
            Write46_SimpleType(@"SimpleType", @"", ((global::SerializationTypes.SimpleType)o), true, false);
        }

        public void Write156_TypeWithGetSetArrayMembers(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithGetSetArrayMembers", @"");
                return;
            }
            TopLevelElement();
            Write47_TypeWithGetSetArrayMembers(@"TypeWithGetSetArrayMembers", @"", ((global::SerializationTypes.TypeWithGetSetArrayMembers)o), true, false);
        }

        public void Write157_TypeWithGetOnlyArrayProperties(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithGetOnlyArrayProperties", @"");
                return;
            }
            TopLevelElement();
            Write48_TypeWithGetOnlyArrayProperties(@"TypeWithGetOnlyArrayProperties", @"", ((global::SerializationTypes.TypeWithGetOnlyArrayProperties)o), true, false);
        }

        public void Write158_StructNotSerializable(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"StructNotSerializable", @"");
                return;
            }
            Write49_StructNotSerializable(@"StructNotSerializable", @"", ((global::SerializationTypes.StructNotSerializable)o), false);
        }

        public void Write159_TypeWithMyCollectionField(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithMyCollectionField", @"");
                return;
            }
            TopLevelElement();
            Write50_TypeWithMyCollectionField(@"TypeWithMyCollectionField", @"", ((global::SerializationTypes.TypeWithMyCollectionField)o), true, false);
        }

        public void Write160_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithReadOnlyMyCollectionProperty", @"");
                return;
            }
            TopLevelElement();
            Write51_Item(@"TypeWithReadOnlyMyCollectionProperty", @"", ((global::SerializationTypes.TypeWithReadOnlyMyCollectionProperty)o), true, false);
        }

        public void Write161_ArrayOfAnyType(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"ArrayOfAnyType", @"");
                return;
            }
            TopLevelElement();
            {
                global::SerializationTypes.MyList a = (global::SerializationTypes.MyList)((global::SerializationTypes.MyList)o);
                if ((object)(a) == null) {
                    WriteNullTagLiteral(@"ArrayOfAnyType", @"");
                }
                else {
                    WriteStartElement(@"ArrayOfAnyType", @"", null, false);
                    for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                        Write1_Object(@"anyType", @"", ((global::System.Object)a[ia]), true, false);
                    }
                    WriteEndElement();
                }
            }
        }

        public void Write162_MyEnum(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"MyEnum", @"");
                return;
            }
            WriteElementString(@"MyEnum", @"", Write52_MyEnum(((global::SerializationTypes.MyEnum)o)));
        }

        public void Write163_TypeWithEnumMembers(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithEnumMembers", @"");
                return;
            }
            TopLevelElement();
            Write53_TypeWithEnumMembers(@"TypeWithEnumMembers", @"", ((global::SerializationTypes.TypeWithEnumMembers)o), true, false);
        }

        public void Write164_DCStruct(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"DCStruct", @"");
                return;
            }
            Write54_DCStruct(@"DCStruct", @"", ((global::SerializationTypes.DCStruct)o), false);
        }

        public void Write165_DCClassWithEnumAndStruct(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DCClassWithEnumAndStruct", @"");
                return;
            }
            TopLevelElement();
            Write55_DCClassWithEnumAndStruct(@"DCClassWithEnumAndStruct", @"", ((global::SerializationTypes.DCClassWithEnumAndStruct)o), true, false);
        }

        public void Write166_BuiltInTypes(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"BuiltInTypes", @"");
                return;
            }
            TopLevelElement();
            Write56_BuiltInTypes(@"BuiltInTypes", @"", ((global::SerializationTypes.BuiltInTypes)o), true, false);
        }

        public void Write167_TypeA(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeA", @"");
                return;
            }
            TopLevelElement();
            Write57_TypeA(@"TypeA", @"", ((global::SerializationTypes.TypeA)o), true, false);
        }

        public void Write168_TypeB(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeB", @"");
                return;
            }
            TopLevelElement();
            Write58_TypeB(@"TypeB", @"", ((global::SerializationTypes.TypeB)o), true, false);
        }

        public void Write169_TypeHasArrayOfASerializedAsB(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeHasArrayOfASerializedAsB", @"");
                return;
            }
            TopLevelElement();
            Write59_TypeHasArrayOfASerializedAsB(@"TypeHasArrayOfASerializedAsB", @"", ((global::SerializationTypes.TypeHasArrayOfASerializedAsB)o), true, false);
        }

        public void Write170_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"__TypeNameWithSpecialCharacters漢ñ", @"");
                return;
            }
            TopLevelElement();
            Write60_Item(@"__TypeNameWithSpecialCharacters漢ñ", @"", ((global::SerializationTypes.@__TypeNameWithSpecialCharacters漢ñ)o), true, false);
        }

        public void Write171_BaseClassWithSamePropertyName(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"BaseClassWithSamePropertyName", @"");
                return;
            }
            TopLevelElement();
            Write61_BaseClassWithSamePropertyName(@"BaseClassWithSamePropertyName", @"", ((global::SerializationTypes.BaseClassWithSamePropertyName)o), true, false);
        }

        public void Write172_DerivedClassWithSameProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DerivedClassWithSameProperty", @"");
                return;
            }
            TopLevelElement();
            Write62_DerivedClassWithSameProperty(@"DerivedClassWithSameProperty", @"", ((global::SerializationTypes.DerivedClassWithSameProperty)o), true, false);
        }

        public void Write173_DerivedClassWithSameProperty2(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DerivedClassWithSameProperty2", @"");
                return;
            }
            TopLevelElement();
            Write63_DerivedClassWithSameProperty2(@"DerivedClassWithSameProperty2", @"", ((global::SerializationTypes.DerivedClassWithSameProperty2)o), true, false);
        }

        public void Write174_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithDateTimePropertyAsXmlTime", @"");
                return;
            }
            TopLevelElement();
            Write64_Item(@"TypeWithDateTimePropertyAsXmlTime", @"", ((global::SerializationTypes.TypeWithDateTimePropertyAsXmlTime)o), true, false);
        }

        public void Write175_TypeWithByteArrayAsXmlText(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithByteArrayAsXmlText", @"");
                return;
            }
            TopLevelElement();
            Write65_TypeWithByteArrayAsXmlText(@"TypeWithByteArrayAsXmlText", @"", ((global::SerializationTypes.TypeWithByteArrayAsXmlText)o), true, false);
        }

        public void Write176_SimpleDC(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"SimpleDC", @"");
                return;
            }
            TopLevelElement();
            Write66_SimpleDC(@"SimpleDC", @"", ((global::SerializationTypes.SimpleDC)o), true, false);
        }

        public void Write177_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"TypeWithXmlTextAttributeOnArray", @"http://schemas.xmlsoap.org/ws/2005/04/discovery");
                return;
            }
            TopLevelElement();
            Write67_Item(@"TypeWithXmlTextAttributeOnArray", @"http://schemas.xmlsoap.org/ws/2005/04/discovery", ((global::SerializationTypes.TypeWithXmlTextAttributeOnArray)o), false, false);
        }

        public void Write178_EnumFlags(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"EnumFlags", @"");
                return;
            }
            WriteElementString(@"EnumFlags", @"", Write68_EnumFlags(((global::SerializationTypes.EnumFlags)o)));
        }

        public void Write179_ClassImplementsInterface(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"ClassImplementsInterface", @"");
                return;
            }
            TopLevelElement();
            Write69_ClassImplementsInterface(@"ClassImplementsInterface", @"", ((global::SerializationTypes.ClassImplementsInterface)o), true, false);
        }

        public void Write180_WithStruct(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"WithStruct", @"");
                return;
            }
            TopLevelElement();
            Write71_WithStruct(@"WithStruct", @"", ((global::SerializationTypes.WithStruct)o), true, false);
        }

        public void Write181_SomeStruct(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"SomeStruct", @"");
                return;
            }
            Write70_SomeStruct(@"SomeStruct", @"", ((global::SerializationTypes.SomeStruct)o), false);
        }

        public void Write182_WithEnums(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"WithEnums", @"");
                return;
            }
            TopLevelElement();
            Write74_WithEnums(@"WithEnums", @"", ((global::SerializationTypes.WithEnums)o), true, false);
        }

        public void Write183_WithNullables(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"WithNullables", @"");
                return;
            }
            TopLevelElement();
            Write75_WithNullables(@"WithNullables", @"", ((global::SerializationTypes.WithNullables)o), true, false);
        }

        public void Write184_ByteEnum(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"ByteEnum", @"");
                return;
            }
            WriteElementString(@"ByteEnum", @"", Write76_ByteEnum(((global::SerializationTypes.ByteEnum)o)));
        }

        public void Write185_SByteEnum(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"SByteEnum", @"");
                return;
            }
            WriteElementString(@"SByteEnum", @"", Write77_SByteEnum(((global::SerializationTypes.SByteEnum)o)));
        }

        public void Write186_ShortEnum(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"ShortEnum", @"");
                return;
            }
            WriteElementString(@"ShortEnum", @"", Write73_ShortEnum(((global::SerializationTypes.ShortEnum)o)));
        }

        public void Write187_IntEnum(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"IntEnum", @"");
                return;
            }
            WriteElementString(@"IntEnum", @"", Write72_IntEnum(((global::SerializationTypes.IntEnum)o)));
        }

        public void Write188_UIntEnum(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"UIntEnum", @"");
                return;
            }
            WriteElementString(@"UIntEnum", @"", Write78_UIntEnum(((global::SerializationTypes.UIntEnum)o)));
        }

        public void Write189_LongEnum(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"LongEnum", @"");
                return;
            }
            WriteElementString(@"LongEnum", @"", Write79_LongEnum(((global::SerializationTypes.LongEnum)o)));
        }

        public void Write190_ULongEnum(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"ULongEnum", @"");
                return;
            }
            WriteElementString(@"ULongEnum", @"", Write80_ULongEnum(((global::SerializationTypes.ULongEnum)o)));
        }

        public void Write191_AttributeTesting(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"AttributeTesting", @"");
                return;
            }
            TopLevelElement();
            Write82_XmlSerializerAttributes(@"AttributeTesting", @"", ((global::SerializationTypes.XmlSerializerAttributes)o), false, false);
        }

        public void Write192_ItemChoiceType(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"ItemChoiceType", @"");
                return;
            }
            WriteElementString(@"ItemChoiceType", @"", Write81_ItemChoiceType(((global::SerializationTypes.ItemChoiceType)o)));
        }

        public void Write193_TypeWithAnyAttribute(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithAnyAttribute", @"");
                return;
            }
            TopLevelElement();
            Write83_TypeWithAnyAttribute(@"TypeWithAnyAttribute", @"", ((global::SerializationTypes.TypeWithAnyAttribute)o), true, false);
        }

        public void Write194_KnownTypesThroughConstructor(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"KnownTypesThroughConstructor", @"");
                return;
            }
            TopLevelElement();
            Write84_KnownTypesThroughConstructor(@"KnownTypesThroughConstructor", @"", ((global::SerializationTypes.KnownTypesThroughConstructor)o), true, false);
        }

        public void Write195_SimpleKnownTypeValue(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"SimpleKnownTypeValue", @"");
                return;
            }
            TopLevelElement();
            Write85_SimpleKnownTypeValue(@"SimpleKnownTypeValue", @"", ((global::SerializationTypes.SimpleKnownTypeValue)o), true, false);
        }

        public void Write196_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"ClassImplementingIXmlSerializable", @"");
                return;
            }
            TopLevelElement();
            WriteSerializable((System.Xml.Serialization.IXmlSerializable)((global::SerializationTypes.ClassImplementingIXmlSerializable)o), @"ClassImplementingIXmlSerializable", @"", true, true);
        }

        public void Write197_TypeWithPropertyNameSpecified(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithPropertyNameSpecified", @"");
                return;
            }
            TopLevelElement();
            Write86_TypeWithPropertyNameSpecified(@"TypeWithPropertyNameSpecified", @"", ((global::SerializationTypes.TypeWithPropertyNameSpecified)o), true, false);
        }

        public void Write198_TypeWithXmlSchemaFormAttribute(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithXmlSchemaFormAttribute", @"");
                return;
            }
            TopLevelElement();
            Write87_TypeWithXmlSchemaFormAttribute(@"TypeWithXmlSchemaFormAttribute", @"", ((global::SerializationTypes.TypeWithXmlSchemaFormAttribute)o), true, false);
        }

        public void Write199_MyXmlType(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"MyXmlType", @"");
                return;
            }
            TopLevelElement();
            Write88_Item(@"MyXmlType", @"", ((global::SerializationTypes.TypeWithTypeNameInXmlTypeAttribute)o), true, false);
        }

        public void Write200_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithSchemaFormInXmlAttribute", @"");
                return;
            }
            TopLevelElement();
            Write89_Item(@"TypeWithSchemaFormInXmlAttribute", @"", ((global::SerializationTypes.TypeWithSchemaFormInXmlAttribute)o), true, false);
        }

        public void Write201_CustomDocument(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"CustomDocument", @"");
                return;
            }
            TopLevelElement();
            Write91_CustomDocument(@"CustomDocument", @"", ((global::SerializationTypes.CustomDocument)o), true, false);
        }

        public void Write202_CustomElement(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"CustomElement", @"");
                return;
            }
            TopLevelElement();
            Write90_CustomElement(@"CustomElement", @"", ((global::SerializationTypes.CustomElement)o), true, false);
        }

        public void Write203_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"", null);
                return;
            }
            TopLevelElement();
            if ((o) is System.Xml.XmlNode || o == null) {
                WriteElementLiteral((System.Xml.XmlNode)o, @"", null, true, true);
            }
            else {
                throw CreateInvalidAnyTypeException(o);
            }
        }

        public void Write204_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithNonPublicDefaultConstructor", @"");
                return;
            }
            TopLevelElement();
            Write92_Item(@"TypeWithNonPublicDefaultConstructor", @"", ((global::SerializationTypes.TypeWithNonPublicDefaultConstructor)o), true, false);
        }

        public void Write205_ServerSettings(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"ServerSettings", @"");
                return;
            }
            TopLevelElement();
            Write93_ServerSettings(@"ServerSettings", @"", ((global::SerializationTypes.ServerSettings)o), true, false);
        }

        public void Write206_TypeWithXmlQualifiedName(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithXmlQualifiedName", @"");
                return;
            }
            TopLevelElement();
            Write94_TypeWithXmlQualifiedName(@"TypeWithXmlQualifiedName", @"", ((global::SerializationTypes.TypeWithXmlQualifiedName)o), true, false);
        }

        public void Write207_TypeWith2DArrayProperty2(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWith2DArrayProperty2", @"");
                return;
            }
            TopLevelElement();
            Write95_TypeWith2DArrayProperty2(@"TypeWith2DArrayProperty2", @"", ((global::SerializationTypes.TypeWith2DArrayProperty2)o), true, false);
        }

        public void Write208_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithPropertiesHavingDefaultValue", @"");
                return;
            }
            TopLevelElement();
            Write96_Item(@"TypeWithPropertiesHavingDefaultValue", @"", ((global::SerializationTypes.TypeWithPropertiesHavingDefaultValue)o), true, false);
        }

        public void Write209_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithEnumPropertyHavingDefaultValue", @"");
                return;
            }
            TopLevelElement();
            Write97_Item(@"TypeWithEnumPropertyHavingDefaultValue", @"", ((global::SerializationTypes.TypeWithEnumPropertyHavingDefaultValue)o), true, false);
        }

        public void Write210_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithEnumFlagPropertyHavingDefaultValue", @"");
                return;
            }
            TopLevelElement();
            Write98_Item(@"TypeWithEnumFlagPropertyHavingDefaultValue", @"", ((global::SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue)o), true, false);
        }

        public void Write211_TypeWithShouldSerializeMethod(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithShouldSerializeMethod", @"");
                return;
            }
            TopLevelElement();
            Write99_TypeWithShouldSerializeMethod(@"TypeWithShouldSerializeMethod", @"", ((global::SerializationTypes.TypeWithShouldSerializeMethod)o), true, false);
        }

        public void Write212_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"KnownTypesThroughConstructorWithArrayProperties", @"");
                return;
            }
            TopLevelElement();
            Write100_Item(@"KnownTypesThroughConstructorWithArrayProperties", @"", ((global::SerializationTypes.KnownTypesThroughConstructorWithArrayProperties)o), true, false);
        }

        public void Write213_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"KnownTypesThroughConstructorWithValue", @"");
                return;
            }
            TopLevelElement();
            Write101_Item(@"KnownTypesThroughConstructorWithValue", @"", ((global::SerializationTypes.KnownTypesThroughConstructorWithValue)o), true, false);
        }

        public void Write214_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithTypesHavingCustomFormatter", @"");
                return;
            }
            TopLevelElement();
            Write102_Item(@"TypeWithTypesHavingCustomFormatter", @"", ((global::SerializationTypes.TypeWithTypesHavingCustomFormatter)o), true, false);
        }

        public void Write215_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithArrayPropertyHavingChoice", @"");
                return;
            }
            TopLevelElement();
            Write104_Item(@"TypeWithArrayPropertyHavingChoice", @"", ((global::SerializationTypes.TypeWithArrayPropertyHavingChoice)o), true, false);
        }

        public void Write216_MoreChoices(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"MoreChoices", @"");
                return;
            }
            WriteElementString(@"MoreChoices", @"", Write103_MoreChoices(((global::SerializationTypes.MoreChoices)o)));
        }

        public void Write217_TypeWithFieldsOrdered(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithFieldsOrdered", @"");
                return;
            }
            TopLevelElement();
            Write105_TypeWithFieldsOrdered(@"TypeWithFieldsOrdered", @"", ((global::SerializationTypes.TypeWithFieldsOrdered)o), true, false);
        }

        public void Write218_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithKnownTypesOfCollectionsWithConflictingXmlName", @"");
                return;
            }
            TopLevelElement();
            Write106_Item(@"TypeWithKnownTypesOfCollectionsWithConflictingXmlName", @"", ((global::SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName)o), true, false);
        }

        public void Write219_Root(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Root", @"");
                return;
            }
            TopLevelElement();
            Write109_Item(@"Root", @"", ((global::SerializationTypes.NamespaceTypeNameClashContainer)o), true, false);
        }

        public void Write220_TypeClashB(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeClashB", @"");
                return;
            }
            TopLevelElement();
            Write108_TypeNameClash(@"TypeClashB", @"", ((global::SerializationTypes.TypeNameClashB.TypeNameClash)o), true, false);
        }

        public void Write221_TypeClashA(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeClashA", @"");
                return;
            }
            TopLevelElement();
            Write107_TypeNameClash(@"TypeClashA", @"", ((global::SerializationTypes.TypeNameClashA.TypeNameClash)o), true, false);
        }

        public void Write222_Person(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Person", @"");
                return;
            }
            TopLevelElement();
            Write110_Person(@"Person", @"", ((global::Outer.Person)o), true, false);
        }

        void Write110_Person(string n, string ns, global::Outer.Person o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::Outer.Person)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"Person", @"");
            WriteElementString(@"FirstName", @"", ((global::System.String)o.@FirstName));
            WriteElementString(@"MiddleName", @"", ((global::System.String)o.@MiddleName));
            WriteElementString(@"LastName", @"", ((global::System.String)o.@LastName));
            WriteEndElement(o);
        }

        void Write107_TypeNameClash(string n, string ns, global::SerializationTypes.TypeNameClashA.TypeNameClash o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeNameClashA.TypeNameClash)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeClashA", @"");
            WriteElementString(@"Name", @"", ((global::System.String)o.@Name));
            WriteEndElement(o);
        }

        void Write108_TypeNameClash(string n, string ns, global::SerializationTypes.TypeNameClashB.TypeNameClash o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeNameClashB.TypeNameClash)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeClashB", @"");
            WriteElementString(@"Name", @"", ((global::System.String)o.@Name));
            WriteEndElement(o);
        }

        void Write109_Item(string n, string ns, global::SerializationTypes.NamespaceTypeNameClashContainer o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.NamespaceTypeNameClashContainer)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"ContainerType", @"");
            {
                global::SerializationTypes.TypeNameClashA.TypeNameClash[] a = (global::SerializationTypes.TypeNameClashA.TypeNameClash[])o.@A;
                if (a != null) {
                    for (int ia = 0; ia < a.Length; ia++) {
                        Write107_TypeNameClash(@"A", @"", ((global::SerializationTypes.TypeNameClashA.TypeNameClash)a[ia]), false, false);
                    }
                }
            }
            {
                global::SerializationTypes.TypeNameClashB.TypeNameClash[] a = (global::SerializationTypes.TypeNameClashB.TypeNameClash[])o.@B;
                if (a != null) {
                    for (int ia = 0; ia < a.Length; ia++) {
                        Write108_TypeNameClash(@"B", @"", ((global::SerializationTypes.TypeNameClashB.TypeNameClash)a[ia]), false, false);
                    }
                }
            }
            WriteEndElement(o);
        }

        void Write106_Item(string n, string ns, global::SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithKnownTypesOfCollectionsWithConflictingXmlName", @"");
            Write1_Object(@"Value1", @"", ((global::System.Object)o.@Value1), false, false);
            Write1_Object(@"Value2", @"", ((global::System.Object)o.@Value2), false, false);
            WriteEndElement(o);
        }

        void Write1_Object(string n, string ns, global::System.Object o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::System.Object)) {
                }
                else {
                    if (t == typeof(global::Outer.Person)) {
                        Write110_Person(n, ns,(global::Outer.Person)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.NamespaceTypeNameClashContainer)) {
                        Write109_Item(n, ns,(global::SerializationTypes.NamespaceTypeNameClashContainer)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeNameClashB.TypeNameClash)) {
                        Write108_TypeNameClash(n, ns,(global::SerializationTypes.TypeNameClashB.TypeNameClash)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeNameClashA.TypeNameClash)) {
                        Write107_TypeNameClash(n, ns,(global::SerializationTypes.TypeNameClashA.TypeNameClash)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName)) {
                        Write106_Item(n, ns,(global::SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithFieldsOrdered)) {
                        Write105_TypeWithFieldsOrdered(n, ns,(global::SerializationTypes.TypeWithFieldsOrdered)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithArrayPropertyHavingChoice)) {
                        Write104_Item(n, ns,(global::SerializationTypes.TypeWithArrayPropertyHavingChoice)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithTypesHavingCustomFormatter)) {
                        Write102_Item(n, ns,(global::SerializationTypes.TypeWithTypesHavingCustomFormatter)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.KnownTypesThroughConstructorWithValue)) {
                        Write101_Item(n, ns,(global::SerializationTypes.KnownTypesThroughConstructorWithValue)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.KnownTypesThroughConstructorWithArrayProperties)) {
                        Write100_Item(n, ns,(global::SerializationTypes.KnownTypesThroughConstructorWithArrayProperties)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithShouldSerializeMethod)) {
                        Write99_TypeWithShouldSerializeMethod(n, ns,(global::SerializationTypes.TypeWithShouldSerializeMethod)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue)) {
                        Write98_Item(n, ns,(global::SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithEnumPropertyHavingDefaultValue)) {
                        Write97_Item(n, ns,(global::SerializationTypes.TypeWithEnumPropertyHavingDefaultValue)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithPropertiesHavingDefaultValue)) {
                        Write96_Item(n, ns,(global::SerializationTypes.TypeWithPropertiesHavingDefaultValue)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWith2DArrayProperty2)) {
                        Write95_TypeWith2DArrayProperty2(n, ns,(global::SerializationTypes.TypeWith2DArrayProperty2)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithXmlQualifiedName)) {
                        Write94_TypeWithXmlQualifiedName(n, ns,(global::SerializationTypes.TypeWithXmlQualifiedName)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.ServerSettings)) {
                        Write93_ServerSettings(n, ns,(global::SerializationTypes.ServerSettings)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithNonPublicDefaultConstructor)) {
                        Write92_Item(n, ns,(global::SerializationTypes.TypeWithNonPublicDefaultConstructor)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.CustomDocument)) {
                        Write91_CustomDocument(n, ns,(global::SerializationTypes.CustomDocument)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.CustomElement)) {
                        Write90_CustomElement(n, ns,(global::SerializationTypes.CustomElement)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithTypeNameInXmlTypeAttribute)) {
                        Write88_Item(n, ns,(global::SerializationTypes.TypeWithTypeNameInXmlTypeAttribute)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithXmlSchemaFormAttribute)) {
                        Write87_TypeWithXmlSchemaFormAttribute(n, ns,(global::SerializationTypes.TypeWithXmlSchemaFormAttribute)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithPropertyNameSpecified)) {
                        Write86_TypeWithPropertyNameSpecified(n, ns,(global::SerializationTypes.TypeWithPropertyNameSpecified)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.SimpleKnownTypeValue)) {
                        Write85_SimpleKnownTypeValue(n, ns,(global::SerializationTypes.SimpleKnownTypeValue)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.KnownTypesThroughConstructor)) {
                        Write84_KnownTypesThroughConstructor(n, ns,(global::SerializationTypes.KnownTypesThroughConstructor)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithAnyAttribute)) {
                        Write83_TypeWithAnyAttribute(n, ns,(global::SerializationTypes.TypeWithAnyAttribute)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.XmlSerializerAttributes)) {
                        Write82_XmlSerializerAttributes(n, ns,(global::SerializationTypes.XmlSerializerAttributes)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.WithNullables)) {
                        Write75_WithNullables(n, ns,(global::SerializationTypes.WithNullables)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.WithEnums)) {
                        Write74_WithEnums(n, ns,(global::SerializationTypes.WithEnums)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.WithStruct)) {
                        Write71_WithStruct(n, ns,(global::SerializationTypes.WithStruct)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.SomeStruct)) {
                        Write70_SomeStruct(n, ns,(global::SerializationTypes.SomeStruct)o, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.ClassImplementsInterface)) {
                        Write69_ClassImplementsInterface(n, ns,(global::SerializationTypes.ClassImplementsInterface)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithXmlTextAttributeOnArray)) {
                        Write67_Item(n, ns,(global::SerializationTypes.TypeWithXmlTextAttributeOnArray)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.SimpleDC)) {
                        Write66_SimpleDC(n, ns,(global::SerializationTypes.SimpleDC)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithByteArrayAsXmlText)) {
                        Write65_TypeWithByteArrayAsXmlText(n, ns,(global::SerializationTypes.TypeWithByteArrayAsXmlText)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithDateTimePropertyAsXmlTime)) {
                        Write64_Item(n, ns,(global::SerializationTypes.TypeWithDateTimePropertyAsXmlTime)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.BaseClassWithSamePropertyName)) {
                        Write61_BaseClassWithSamePropertyName(n, ns,(global::SerializationTypes.BaseClassWithSamePropertyName)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.DerivedClassWithSameProperty)) {
                        Write62_DerivedClassWithSameProperty(n, ns,(global::SerializationTypes.DerivedClassWithSameProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.DerivedClassWithSameProperty2)) {
                        Write63_DerivedClassWithSameProperty2(n, ns,(global::SerializationTypes.DerivedClassWithSameProperty2)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.@__TypeNameWithSpecialCharacters漢ñ)) {
                        Write60_Item(n, ns,(global::SerializationTypes.@__TypeNameWithSpecialCharacters漢ñ)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeHasArrayOfASerializedAsB)) {
                        Write59_TypeHasArrayOfASerializedAsB(n, ns,(global::SerializationTypes.TypeHasArrayOfASerializedAsB)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeB)) {
                        Write58_TypeB(n, ns,(global::SerializationTypes.TypeB)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeA)) {
                        Write57_TypeA(n, ns,(global::SerializationTypes.TypeA)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.BuiltInTypes)) {
                        Write56_BuiltInTypes(n, ns,(global::SerializationTypes.BuiltInTypes)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.DCClassWithEnumAndStruct)) {
                        Write55_DCClassWithEnumAndStruct(n, ns,(global::SerializationTypes.DCClassWithEnumAndStruct)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.DCStruct)) {
                        Write54_DCStruct(n, ns,(global::SerializationTypes.DCStruct)o, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithEnumMembers)) {
                        Write53_TypeWithEnumMembers(n, ns,(global::SerializationTypes.TypeWithEnumMembers)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithReadOnlyMyCollectionProperty)) {
                        Write51_Item(n, ns,(global::SerializationTypes.TypeWithReadOnlyMyCollectionProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithMyCollectionField)) {
                        Write50_TypeWithMyCollectionField(n, ns,(global::SerializationTypes.TypeWithMyCollectionField)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.StructNotSerializable)) {
                        Write49_StructNotSerializable(n, ns,(global::SerializationTypes.StructNotSerializable)o, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithGetOnlyArrayProperties)) {
                        Write48_TypeWithGetOnlyArrayProperties(n, ns,(global::SerializationTypes.TypeWithGetOnlyArrayProperties)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithGetSetArrayMembers)) {
                        Write47_TypeWithGetSetArrayMembers(n, ns,(global::SerializationTypes.TypeWithGetSetArrayMembers)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.SimpleType)) {
                        Write46_SimpleType(n, ns,(global::SerializationTypes.SimpleType)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithDateTimeStringProperty)) {
                        Write45_TypeWithDateTimeStringProperty(n, ns,(global::SerializationTypes.TypeWithDateTimeStringProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::XElementArrayWrapper)) {
                        Write44_XElementArrayWrapper(n, ns,(global::XElementArrayWrapper)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::XElementStruct)) {
                        Write43_XElementStruct(n, ns,(global::XElementStruct)o, true);
                        return;
                    }
                    if (t == typeof(global::XElementWrapper)) {
                        Write42_XElementWrapper(n, ns,(global::XElementWrapper)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::RootClass)) {
                        Write41_RootClass(n, ns,(global::RootClass)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Parameter)) {
                        Write40_Parameter(n, ns,(global::Parameter)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Parameter<global::System.String>)) {
                        Write39_ParameterOfString(n, ns,(global::Parameter<global::System.String>)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::MsgDocumentType)) {
                        Write38_MsgDocumentType(n, ns,(global::MsgDocumentType)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::TypeWithLinkedProperty)) {
                        Write37_TypeWithLinkedProperty(n, ns,(global::TypeWithLinkedProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::TypeWithMismatchBetweenAttributeAndPropertyType)) {
                        Write36_Item(n, ns,(global::TypeWithMismatchBetweenAttributeAndPropertyType)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::DefaultValuesSetToNegativeInfinity)) {
                        Write35_Item(n, ns,(global::DefaultValuesSetToNegativeInfinity)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::DefaultValuesSetToPositiveInfinity)) {
                        Write34_Item(n, ns,(global::DefaultValuesSetToPositiveInfinity)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::DefaultValuesSetToNaN)) {
                        Write33_DefaultValuesSetToNaN(n, ns,(global::DefaultValuesSetToNaN)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Pet)) {
                        Write32_Pet(n, ns,(global::Pet)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Orchestra)) {
                        Write29_Orchestra(n, ns,(global::Orchestra)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Instrument)) {
                        Write28_Instrument(n, ns,(global::Instrument)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Brass)) {
                        Write30_Brass(n, ns,(global::Brass)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Trumpet)) {
                        Write31_Trumpet(n, ns,(global::Trumpet)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::BaseClass1)) {
                        Write26_BaseClass1(n, ns,(global::BaseClass1)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::DerivedClass1)) {
                        Write27_DerivedClass1(n, ns,(global::DerivedClass1)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::AliasedTestType)) {
                        Write25_AliasedTestType(n, ns,(global::AliasedTestType)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::OrderedItem)) {
                        Write24_OrderedItem(n, ns,(global::OrderedItem)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Address)) {
                        Write23_Address(n, ns,(global::Address)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::PurchaseOrder)) {
                        Write22_PurchaseOrder(n, ns,(global::PurchaseOrder)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::OrderedItem)) {
                        Write21_OrderedItem(n, ns,(global::OrderedItem)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Address)) {
                        Write20_Address(n, ns,(global::Address)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SimpleBaseClass)) {
                        Write19_SimpleBaseClass(n, ns,(global::SimpleBaseClass)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SimpleDerivedClass)) {
                        Write18_SimpleDerivedClass(n, ns,(global::SimpleDerivedClass)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::BaseClass)) {
                        Write17_BaseClass(n, ns,(global::BaseClass)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::DerivedClass)) {
                        Write16_DerivedClass(n, ns,(global::DerivedClass)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Employee)) {
                        Write15_Employee(n, ns,(global::Employee)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Group)) {
                        Write14_Group(n, ns,(global::Group)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Vehicle)) {
                        Write13_Vehicle(n, ns,(global::Vehicle)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Animal)) {
                        Write10_Animal(n, ns,(global::Animal)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Dog)) {
                        Write12_Dog(n, ns,(global::Dog)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::TypeWithXmlNodeArrayProperty)) {
                        Write9_TypeWithXmlNodeArrayProperty(n, ns,(global::TypeWithXmlNodeArrayProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::TypeWithByteProperty)) {
                        Write8_TypeWithByteProperty(n, ns,(global::TypeWithByteProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::TypeWithDefaultTimeSpanProperty)) {
                        Write7_Item(n, ns,(global::TypeWithDefaultTimeSpanProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::TypeWithTimeSpanProperty)) {
                        Write6_TypeWithTimeSpanProperty(n, ns,(global::TypeWithTimeSpanProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::TypeWithDateTimeOffsetProperties)) {
                        Write5_Item(n, ns,(global::TypeWithDateTimeOffsetProperties)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::TypeWithBinaryProperty)) {
                        Write4_TypeWithBinaryProperty(n, ns,(global::TypeWithBinaryProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::TypeWithXmlDocumentProperty)) {
                        Write3_TypeWithXmlDocumentProperty(n, ns,(global::TypeWithXmlDocumentProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::TypeWithXmlElementProperty)) {
                        Write2_TypeWithXmlElementProperty(n, ns,(global::TypeWithXmlElementProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::DogBreed)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"DogBreed", @"");
                        Writer.WriteString(Write11_DogBreed((global::DogBreed)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::OrderedItem[])) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ArrayOfOrderedItem", @"http://www.contoso1.com");
                        {
                            global::OrderedItem[] a = (global::OrderedItem[])o;
                            if (a != null) {
                                for (int ia = 0; ia < a.Length; ia++) {
                                    Write21_OrderedItem(@"OrderedItem", @"http://www.contoso1.com", ((global::OrderedItem)a[ia]), true, false);
                                }
                            }
                        }
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::System.Collections.Generic.List<global::System.Int32>)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ArrayOfInt", @"");
                        {
                            global::System.Collections.Generic.List<global::System.Int32> a = (global::System.Collections.Generic.List<global::System.Int32>)o;
                            if (a != null) {
                                for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                                    WriteElementStringRaw(@"int", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)a[ia])));
                                }
                            }
                        }
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::System.Collections.Generic.List<global::System.String>)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ArrayOfString", @"");
                        {
                            global::System.Collections.Generic.List<global::System.String> a = (global::System.Collections.Generic.List<global::System.String>)o;
                            if (a != null) {
                                for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                                    WriteNullableStringLiteral(@"string", @"", ((global::System.String)a[ia]));
                                }
                            }
                        }
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::System.Collections.Generic.List<global::System.Double>)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ArrayOfDouble", @"");
                        {
                            global::System.Collections.Generic.List<global::System.Double> a = (global::System.Collections.Generic.List<global::System.Double>)o;
                            if (a != null) {
                                for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                                    WriteElementStringRaw(@"double", @"", System.Xml.XmlConvert.ToString((global::System.Double)((global::System.Double)a[ia])));
                                }
                            }
                        }
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::MyCollection1)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ArrayOfDateTime", @"");
                        {
                            global::MyCollection1 a = (global::MyCollection1)o;
                            if (a != null) {
                                System.Collections.IEnumerator e = ((System.Collections.Generic.IEnumerable<global::System.DateTime>)a).GetEnumerator();
                                if (e != null)
                                while (e.MoveNext()) {
                                    global::System.DateTime ai = (global::System.DateTime)e.Current;
                                    WriteElementStringRaw(@"dateTime", @"", FromDateTime(((global::System.DateTime)ai)));
                                }
                            }
                        }
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::Instrument[])) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ArrayOfInstrument", @"");
                        {
                            global::Instrument[] a = (global::Instrument[])o;
                            if (a != null) {
                                for (int ia = 0; ia < a.Length; ia++) {
                                    Write28_Instrument(@"Instrument", @"", ((global::Instrument)a[ia]), true, false);
                                }
                            }
                        }
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::System.Collections.Generic.List<global::TypeWithLinkedProperty>)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ArrayOfTypeWithLinkedProperty", @"");
                        {
                            global::System.Collections.Generic.List<global::TypeWithLinkedProperty> a = (global::System.Collections.Generic.List<global::TypeWithLinkedProperty>)o;
                            if (a != null) {
                                for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                                    Write37_TypeWithLinkedProperty(@"TypeWithLinkedProperty", @"", ((global::TypeWithLinkedProperty)a[ia]), true, false);
                                }
                            }
                        }
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::System.Collections.Generic.List<global::Parameter>)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ArrayOfParameter", @"");
                        {
                            global::System.Collections.Generic.List<global::Parameter> a = (global::System.Collections.Generic.List<global::Parameter>)o;
                            if (a != null) {
                                for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                                    Write40_Parameter(@"Parameter", @"", ((global::Parameter)a[ia]), true, false);
                                }
                            }
                        }
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::System.Xml.Linq.XElement[])) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ArrayOfXElement", @"");
                        {
                            global::System.Xml.Linq.XElement[] a = (global::System.Xml.Linq.XElement[])o;
                            if (a != null) {
                                for (int ia = 0; ia < a.Length; ia++) {
                                    WriteSerializable((System.Xml.Serialization.IXmlSerializable)((global::System.Xml.Linq.XElement)a[ia]), @"XElement", @"", true, true);
                                }
                            }
                        }
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.SimpleType[])) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ArrayOfSimpleType", @"");
                        {
                            global::SerializationTypes.SimpleType[] a = (global::SerializationTypes.SimpleType[])o;
                            if (a != null) {
                                for (int ia = 0; ia < a.Length; ia++) {
                                    Write46_SimpleType(@"SimpleType", @"", ((global::SerializationTypes.SimpleType)a[ia]), true, false);
                                }
                            }
                        }
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.MyList)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ArrayOfAnyType", @"");
                        {
                            global::SerializationTypes.MyList a = (global::SerializationTypes.MyList)o;
                            if (a != null) {
                                for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                                    Write1_Object(@"anyType", @"", ((global::System.Object)a[ia]), true, false);
                                }
                            }
                        }
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.MyEnum)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"MyEnum", @"");
                        Writer.WriteString(Write52_MyEnum((global::SerializationTypes.MyEnum)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeA[])) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ArrayOfTypeA", @"");
                        {
                            global::SerializationTypes.TypeA[] a = (global::SerializationTypes.TypeA[])o;
                            if (a != null) {
                                for (int ia = 0; ia < a.Length; ia++) {
                                    Write57_TypeA(@"TypeA", @"", ((global::SerializationTypes.TypeA)a[ia]), true, false);
                                }
                            }
                        }
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.EnumFlags)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"EnumFlags", @"");
                        Writer.WriteString(Write68_EnumFlags((global::SerializationTypes.EnumFlags)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.IntEnum)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"IntEnum", @"");
                        Writer.WriteString(Write72_IntEnum((global::SerializationTypes.IntEnum)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.ShortEnum)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ShortEnum", @"");
                        Writer.WriteString(Write73_ShortEnum((global::SerializationTypes.ShortEnum)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.ByteEnum)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ByteEnum", @"");
                        Writer.WriteString(Write76_ByteEnum((global::SerializationTypes.ByteEnum)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.SByteEnum)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"SByteEnum", @"");
                        Writer.WriteString(Write77_SByteEnum((global::SerializationTypes.SByteEnum)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.UIntEnum)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"UIntEnum", @"");
                        Writer.WriteString(Write78_UIntEnum((global::SerializationTypes.UIntEnum)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.LongEnum)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"LongEnum", @"");
                        Writer.WriteString(Write79_LongEnum((global::SerializationTypes.LongEnum)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.ULongEnum)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ULongEnum", @"");
                        Writer.WriteString(Write80_ULongEnum((global::SerializationTypes.ULongEnum)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.ItemChoiceType)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ItemChoiceType", @"");
                        Writer.WriteString(Write81_ItemChoiceType((global::SerializationTypes.ItemChoiceType)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.ItemChoiceType[])) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ArrayOfItemChoiceType", @"");
                        {
                            global::SerializationTypes.ItemChoiceType[] a = (global::SerializationTypes.ItemChoiceType[])o;
                            if (a != null) {
                                for (int ia = 0; ia < a.Length; ia++) {
                                    WriteElementString(@"ItemChoiceType", @"", Write81_ItemChoiceType(((global::SerializationTypes.ItemChoiceType)a[ia])));
                                }
                            }
                        }
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::System.Object[])) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ArrayOfString", @"http://mynamespace");
                        {
                            global::System.Object[] a = (global::System.Object[])o;
                            if (a != null) {
                                for (int ia = 0; ia < a.Length; ia++) {
                                    WriteNullableStringLiteral(@"string", @"http://mynamespace", ((global::System.String)a[ia]));
                                }
                            }
                        }
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::System.Collections.Generic.List<global::System.String>)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ArrayOfString1", @"");
                        {
                            global::System.Collections.Generic.List<global::System.String> a = (global::System.Collections.Generic.List<global::System.String>)o;
                            if (a != null) {
                                for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                                    WriteElementString(@"NoneParameter", @"", ((global::System.String)a[ia]));
                                }
                            }
                        }
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::System.Collections.Generic.List<global::System.Boolean>)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ArrayOfBoolean", @"");
                        {
                            global::System.Collections.Generic.List<global::System.Boolean> a = (global::System.Collections.Generic.List<global::System.Boolean>)o;
                            if (a != null) {
                                for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                                    WriteElementStringRaw(@"QualifiedParameter", @"", System.Xml.XmlConvert.ToString((global::System.Boolean)((global::System.Boolean)a[ia])));
                                }
                            }
                        }
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.SimpleType[][])) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ArrayOfArrayOfSimpleType", @"");
                        {
                            global::SerializationTypes.SimpleType[] a = (global::SerializationTypes.SimpleType[])((global::SerializationTypes.SimpleType[])o);
                            if (a != null){
                                WriteStartElement(@"SimpleType", @"", null, false);
                                for (int ia = 0; ia < a.Length; ia++) {
                                    Write46_SimpleType(@"SimpleType", @"", ((global::SerializationTypes.SimpleType)a[ia]), true, false);
                                }
                                WriteEndElement();
                            }
                        }
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.MoreChoices)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"MoreChoices", @"");
                        Writer.WriteString(Write103_MoreChoices((global::SerializationTypes.MoreChoices)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    WriteTypedPrimitive(n, ns, o, true);
                    return;
                }
            }
            WriteStartElement(n, ns, o, false, null);
            WriteEndElement(o);
        }

        string Write103_MoreChoices(global::SerializationTypes.MoreChoices v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.MoreChoices.@None: s = @"None"; break;
                case global::SerializationTypes.MoreChoices.@Item: s = @"Item"; break;
                case global::SerializationTypes.MoreChoices.@Amount: s = @"Amount"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.MoreChoices");
            }
            return s;
        }

        void Write46_SimpleType(string n, string ns, global::SerializationTypes.SimpleType o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.SimpleType)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"SimpleType", @"");
            WriteElementString(@"P1", @"", ((global::System.String)o.@P1));
            WriteElementStringRaw(@"P2", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)o.@P2)));
            WriteEndElement(o);
        }

        string Write81_ItemChoiceType(global::SerializationTypes.ItemChoiceType v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.ItemChoiceType.@None: s = @"None"; break;
                case global::SerializationTypes.ItemChoiceType.@Word: s = @"Word"; break;
                case global::SerializationTypes.ItemChoiceType.@Number: s = @"Number"; break;
                case global::SerializationTypes.ItemChoiceType.@DecimalNumber: s = @"DecimalNumber"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.ItemChoiceType");
            }
            return s;
        }

        string Write80_ULongEnum(global::SerializationTypes.ULongEnum v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.ULongEnum.@Option0: s = @"Option0"; break;
                case global::SerializationTypes.ULongEnum.@Option1: s = @"Option1"; break;
                case global::SerializationTypes.ULongEnum.@Option2: s = @"Option2"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.ULongEnum");
            }
            return s;
        }

        string Write79_LongEnum(global::SerializationTypes.LongEnum v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.LongEnum.@Option0: s = @"Option0"; break;
                case global::SerializationTypes.LongEnum.@Option1: s = @"Option1"; break;
                case global::SerializationTypes.LongEnum.@Option2: s = @"Option2"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.LongEnum");
            }
            return s;
        }

        string Write78_UIntEnum(global::SerializationTypes.UIntEnum v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.UIntEnum.@Option0: s = @"Option0"; break;
                case global::SerializationTypes.UIntEnum.@Option1: s = @"Option1"; break;
                case global::SerializationTypes.UIntEnum.@Option2: s = @"Option2"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.UIntEnum");
            }
            return s;
        }

        string Write77_SByteEnum(global::SerializationTypes.SByteEnum v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.SByteEnum.@Option0: s = @"Option0"; break;
                case global::SerializationTypes.SByteEnum.@Option1: s = @"Option1"; break;
                case global::SerializationTypes.SByteEnum.@Option2: s = @"Option2"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.SByteEnum");
            }
            return s;
        }

        string Write76_ByteEnum(global::SerializationTypes.ByteEnum v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.ByteEnum.@Option0: s = @"Option0"; break;
                case global::SerializationTypes.ByteEnum.@Option1: s = @"Option1"; break;
                case global::SerializationTypes.ByteEnum.@Option2: s = @"Option2"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.ByteEnum");
            }
            return s;
        }

        string Write73_ShortEnum(global::SerializationTypes.ShortEnum v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.ShortEnum.@Option0: s = @"Option0"; break;
                case global::SerializationTypes.ShortEnum.@Option1: s = @"Option1"; break;
                case global::SerializationTypes.ShortEnum.@Option2: s = @"Option2"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.ShortEnum");
            }
            return s;
        }

        string Write72_IntEnum(global::SerializationTypes.IntEnum v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.IntEnum.@Option0: s = @"Option0"; break;
                case global::SerializationTypes.IntEnum.@Option1: s = @"Option1"; break;
                case global::SerializationTypes.IntEnum.@Option2: s = @"Option2"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.IntEnum");
            }
            return s;
        }

        string Write68_EnumFlags(global::SerializationTypes.EnumFlags v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.EnumFlags.@One: s = @"One"; break;
                case global::SerializationTypes.EnumFlags.@Two: s = @"Two"; break;
                case global::SerializationTypes.EnumFlags.@Three: s = @"Three"; break;
                case global::SerializationTypes.EnumFlags.@Four: s = @"Four"; break;
                default: s = FromEnum(((System.Int64)v), new string[] {@"One", 
                    @"Two", 
                    @"Three", 
                    @"Four"}, new System.Int64[] {(long)global::SerializationTypes.EnumFlags.@One, 
                    (long)global::SerializationTypes.EnumFlags.@Two, 
                    (long)global::SerializationTypes.EnumFlags.@Three, 
                    (long)global::SerializationTypes.EnumFlags.@Four}, @"SerializationTypes.EnumFlags"); break;
            }
            return s;
        }

        void Write57_TypeA(string n, string ns, global::SerializationTypes.TypeA o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeA)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeA", @"");
            WriteElementString(@"Name", @"", ((global::System.String)o.@Name));
            WriteEndElement(o);
        }

        string Write52_MyEnum(global::SerializationTypes.MyEnum v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.MyEnum.@One: s = @"One"; break;
                case global::SerializationTypes.MyEnum.@Two: s = @"Two"; break;
                case global::SerializationTypes.MyEnum.@Three: s = @"Three"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.MyEnum");
            }
            return s;
        }

        void Write40_Parameter(string n, string ns, global::Parameter o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::Parameter)) {
                }
                else {
                    if (t == typeof(global::Parameter<global::System.String>)) {
                        Write39_ParameterOfString(n, ns,(global::Parameter<global::System.String>)o, isNullable, true);
                        return;
                    }
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"Parameter", @"");
            WriteAttribute(@"Name", @"", ((global::System.String)o.@Name));
            WriteEndElement(o);
        }

        void Write39_ParameterOfString(string n, string ns, global::Parameter<global::System.String> o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::Parameter<global::System.String>)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"ParameterOfString", @"");
            WriteAttribute(@"Name", @"", ((global::System.String)o.@Name));
            WriteElementString(@"Value", @"", ((global::System.String)o.@Value));
            WriteEndElement(o);
        }

        void Write37_TypeWithLinkedProperty(string n, string ns, global::TypeWithLinkedProperty o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::TypeWithLinkedProperty)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithLinkedProperty", @"");
            Write37_TypeWithLinkedProperty(@"Child", @"", ((global::TypeWithLinkedProperty)o.@Child), false, false);
            {
                global::System.Collections.Generic.List<global::TypeWithLinkedProperty> a = (global::System.Collections.Generic.List<global::TypeWithLinkedProperty>)((global::System.Collections.Generic.List<global::TypeWithLinkedProperty>)o.@Children);
                if (a != null){
                    WriteStartElement(@"Children", @"", null, false);
                    for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                        Write37_TypeWithLinkedProperty(@"TypeWithLinkedProperty", @"", ((global::TypeWithLinkedProperty)a[ia]), true, false);
                    }
                    WriteEndElement();
                }
            }
            WriteEndElement(o);
        }

        void Write28_Instrument(string n, string ns, global::Instrument o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::Instrument)) {
                }
                else {
                    if (t == typeof(global::Brass)) {
                        Write30_Brass(n, ns,(global::Brass)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Trumpet)) {
                        Write31_Trumpet(n, ns,(global::Trumpet)o, isNullable, true);
                        return;
                    }
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"Instrument", @"");
            WriteElementString(@"Name", @"", ((global::System.String)o.@Name));
            WriteEndElement(o);
        }

        void Write31_Trumpet(string n, string ns, global::Trumpet o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::Trumpet)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"Trumpet", @"");
            WriteElementString(@"Name", @"", ((global::System.String)o.@Name));
            WriteElementStringRaw(@"IsValved", @"", System.Xml.XmlConvert.ToString((global::System.Boolean)((global::System.Boolean)o.@IsValved)));
            WriteElementString(@"Modulation", @"", FromChar(((global::System.Char)o.@Modulation)));
            WriteEndElement(o);
        }

        void Write30_Brass(string n, string ns, global::Brass o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::Brass)) {
                }
                else {
                    if (t == typeof(global::Trumpet)) {
                        Write31_Trumpet(n, ns,(global::Trumpet)o, isNullable, true);
                        return;
                    }
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"Brass", @"");
            WriteElementString(@"Name", @"", ((global::System.String)o.@Name));
            WriteElementStringRaw(@"IsValved", @"", System.Xml.XmlConvert.ToString((global::System.Boolean)((global::System.Boolean)o.@IsValved)));
            WriteEndElement(o);
        }

        void Write21_OrderedItem(string n, string ns, global::OrderedItem o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::OrderedItem)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"OrderedItem", @"http://www.contoso1.com");
            WriteElementString(@"ItemName", @"http://www.contoso1.com", ((global::System.String)o.@ItemName));
            WriteElementString(@"Description", @"http://www.contoso1.com", ((global::System.String)o.@Description));
            WriteElementStringRaw(@"UnitPrice", @"http://www.contoso1.com", System.Xml.XmlConvert.ToString((global::System.Decimal)((global::System.Decimal)o.@UnitPrice)));
            WriteElementStringRaw(@"Quantity", @"http://www.contoso1.com", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)o.@Quantity)));
            WriteElementStringRaw(@"LineTotal", @"http://www.contoso1.com", System.Xml.XmlConvert.ToString((global::System.Decimal)((global::System.Decimal)o.@LineTotal)));
            WriteEndElement(o);
        }

        string Write11_DogBreed(global::DogBreed v) {
            string s = null;
            switch (v) {
                case global::DogBreed.@GermanShepherd: s = @"GermanShepherd"; break;
                case global::DogBreed.@LabradorRetriever: s = @"LabradorRetriever"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"DogBreed");
            }
            return s;
        }

        void Write2_TypeWithXmlElementProperty(string n, string ns, global::TypeWithXmlElementProperty o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::TypeWithXmlElementProperty)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithXmlElementProperty", @"");
            {
                global::System.Xml.XmlElement[] a = (global::System.Xml.XmlElement[])o.@Elements;
                if (a != null) {
                    for (int ia = 0; ia < a.Length; ia++) {
                        if ((a[ia]) is System.Xml.XmlNode || a[ia] == null) {
                            WriteElementLiteral((System.Xml.XmlNode)a[ia], @"", null, false, true);
                        }
                        else {
                            throw CreateInvalidAnyTypeException(a[ia]);
                        }
                    }
                }
            }
            WriteEndElement(o);
        }

        void Write3_TypeWithXmlDocumentProperty(string n, string ns, global::TypeWithXmlDocumentProperty o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::TypeWithXmlDocumentProperty)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithXmlDocumentProperty", @"");
            if ((((global::System.Xml.XmlDocument)o.@Document)) is System.Xml.XmlNode || ((global::System.Xml.XmlDocument)o.@Document) == null) {
                WriteElementLiteral((System.Xml.XmlNode)((global::System.Xml.XmlDocument)o.@Document), @"Document", @"", false, false);
            }
            else {
                throw CreateInvalidAnyTypeException(((global::System.Xml.XmlDocument)o.@Document));
            }
            WriteEndElement(o);
        }

        void Write4_TypeWithBinaryProperty(string n, string ns, global::TypeWithBinaryProperty o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::TypeWithBinaryProperty)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithBinaryProperty", @"");
            WriteElementStringRaw(@"BinaryHexContent", @"", FromByteArrayHex(((global::System.Byte[])o.@BinaryHexContent)));
            WriteElementStringRaw(@"Base64Content", @"", FromByteArrayBase64(((global::System.Byte[])o.@Base64Content)));
            WriteEndElement(o);
        }

        void Write5_Item(string n, string ns, global::TypeWithDateTimeOffsetProperties o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::TypeWithDateTimeOffsetProperties)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithDateTimeOffsetProperties", @"");
            WriteElementStringRaw(@"DTO", @"", System.Xml.XmlConvert.ToString((global::System.DateTimeOffset)((global::System.DateTimeOffset)o.@DTO)));
            WriteElementStringRaw(@"DTO2", @"", System.Xml.XmlConvert.ToString((global::System.DateTimeOffset)((global::System.DateTimeOffset)o.@DTO2)));
            if (((global::System.DateTimeOffset)o.@DTOWithDefault) !=  new System.DateTimeOffset(0, new System.TimeSpan(0))) {
                WriteElementStringRaw(@"DefaultDTO", @"", System.Xml.XmlConvert.ToString((global::System.DateTimeOffset)((global::System.DateTimeOffset)o.@DTOWithDefault)));
            }
            if (o.@NullableDTO != null) {
                WriteNullableStringLiteralRaw(@"NullableDTO", @"", System.Xml.XmlConvert.ToString((global::System.DateTimeOffset)((global::System.DateTimeOffset)o.@NullableDTO)));
            }
            else {
                WriteNullTagLiteral(@"NullableDTO", @"");
            }
            if (o.@NullableDTOWithDefault != null) {
                WriteNullableStringLiteralRaw(@"NullableDefaultDTO", @"", System.Xml.XmlConvert.ToString((global::System.DateTimeOffset)((global::System.DateTimeOffset)o.@NullableDTOWithDefault)));
            }
            else {
                WriteNullTagLiteral(@"NullableDefaultDTO", @"");
            }
            WriteEndElement(o);
        }

        void Write6_TypeWithTimeSpanProperty(string n, string ns, global::TypeWithTimeSpanProperty o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::TypeWithTimeSpanProperty)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithTimeSpanProperty", @"");
            WriteElementStringRaw(@"TimeSpanProperty", @"", System.Xml.XmlConvert.ToString((global::System.TimeSpan)((global::System.TimeSpan)o.@TimeSpanProperty)));
            WriteEndElement(o);
        }

        void Write7_Item(string n, string ns, global::TypeWithDefaultTimeSpanProperty o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::TypeWithDefaultTimeSpanProperty)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithDefaultTimeSpanProperty", @"");
            if (((global::System.TimeSpan)o.@TimeSpanProperty) !=  new System.TimeSpan(600000000)) {
                WriteElementStringRaw(@"TimeSpanProperty", @"", System.Xml.XmlConvert.ToString((global::System.TimeSpan)((global::System.TimeSpan)o.@TimeSpanProperty)));
            }
            if (((global::System.TimeSpan)o.@TimeSpanProperty2) !=  new System.TimeSpan(10000000)) {
                WriteElementStringRaw(@"TimeSpanProperty2", @"", System.Xml.XmlConvert.ToString((global::System.TimeSpan)((global::System.TimeSpan)o.@TimeSpanProperty2)));
            }
            WriteEndElement(o);
        }

        void Write8_TypeWithByteProperty(string n, string ns, global::TypeWithByteProperty o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::TypeWithByteProperty)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithByteProperty", @"");
            WriteElementStringRaw(@"ByteProperty", @"", System.Xml.XmlConvert.ToString((global::System.Byte)((global::System.Byte)o.@ByteProperty)));
            WriteEndElement(o);
        }

        void Write9_TypeWithXmlNodeArrayProperty(string n, string ns, global::TypeWithXmlNodeArrayProperty o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::TypeWithXmlNodeArrayProperty)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithXmlNodeArrayProperty", @"");
            {
                global::System.Xml.XmlNode[] a = (global::System.Xml.XmlNode[])o.@CDATA;
                if (a != null) {
                    for (int ia = 0; ia < a.Length; ia++) {
                        if ((object)(a[ia]) != null){
                            ((global::System.Xml.XmlNode)a[ia]).WriteTo(Writer);
                        }
                    }
                }
            }
            WriteEndElement(o);
        }

        void Write12_Dog(string n, string ns, global::Dog o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::Dog)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"Dog", @"");
            WriteElementStringRaw(@"Age", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)o.@Age)));
            WriteElementString(@"Name", @"", ((global::System.String)o.@Name));
            WriteElementString(@"Breed", @"", Write11_DogBreed(((global::DogBreed)o.@Breed)));
            WriteEndElement(o);
        }

        void Write10_Animal(string n, string ns, global::Animal o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::Animal)) {
                }
                else {
                    if (t == typeof(global::Dog)) {
                        Write12_Dog(n, ns,(global::Dog)o, isNullable, true);
                        return;
                    }
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"Animal", @"");
            WriteElementStringRaw(@"Age", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)o.@Age)));
            WriteElementString(@"Name", @"", ((global::System.String)o.@Name));
            WriteEndElement(o);
        }

        void Write13_Vehicle(string n, string ns, global::Vehicle o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::Vehicle)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"Vehicle", @"");
            WriteElementString(@"LicenseNumber", @"", ((global::System.String)o.@LicenseNumber));
            WriteEndElement(o);
        }

        void Write14_Group(string n, string ns, global::Group o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::Group)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"Group", @"");
            WriteElementString(@"GroupName", @"", ((global::System.String)o.@GroupName));
            Write13_Vehicle(@"GroupVehicle", @"", ((global::Vehicle)o.@GroupVehicle), false, false);
            WriteEndElement(o);
        }

        void Write15_Employee(string n, string ns, global::Employee o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::Employee)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"Employee", @"");
            WriteElementString(@"EmployeeName", @"", ((global::System.String)o.@EmployeeName));
            WriteEndElement(o);
        }

        void Write16_DerivedClass(string n, string ns, global::DerivedClass o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::DerivedClass)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"DerivedClass", @"");
            WriteElementString(@"Value", @"", ((global::System.String)o.@Value));
            WriteElementString(@"value", @"", ((global::System.String)o.@value));
            WriteEndElement(o);
        }

        void Write17_BaseClass(string n, string ns, global::BaseClass o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::BaseClass)) {
                }
                else {
                    if (t == typeof(global::DerivedClass)) {
                        Write16_DerivedClass(n, ns,(global::DerivedClass)o, isNullable, true);
                        return;
                    }
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"BaseClass", @"");
            WriteElementString(@"Value", @"", ((global::System.String)o.@Value));
            WriteElementString(@"value", @"", ((global::System.String)o.@value));
            WriteEndElement(o);
        }

        void Write18_SimpleDerivedClass(string n, string ns, global::SimpleDerivedClass o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SimpleDerivedClass)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"SimpleDerivedClass", @"");
            WriteAttribute(@"AttributeString", @"", ((global::System.String)o.@AttributeString));
            WriteAttribute(@"DateTimeValue", @"", FromDateTime(((global::System.DateTime)o.@DateTimeValue)));
            WriteAttribute(@"BoolValue", @"", System.Xml.XmlConvert.ToString((global::System.Boolean)((global::System.Boolean)o.@BoolValue)));
            WriteEndElement(o);
        }

        void Write19_SimpleBaseClass(string n, string ns, global::SimpleBaseClass o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SimpleBaseClass)) {
                }
                else {
                    if (t == typeof(global::SimpleDerivedClass)) {
                        Write18_SimpleDerivedClass(n, ns,(global::SimpleDerivedClass)o, isNullable, true);
                        return;
                    }
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"SimpleBaseClass", @"");
            WriteAttribute(@"AttributeString", @"", ((global::System.String)o.@AttributeString));
            WriteAttribute(@"DateTimeValue", @"", FromDateTime(((global::System.DateTime)o.@DateTimeValue)));
            WriteEndElement(o);
        }

        void Write20_Address(string n, string ns, global::Address o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::Address)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"Address", @"http://www.contoso1.com");
            WriteAttribute(@"Name", @"", ((global::System.String)o.@Name));
            WriteElementString(@"Line1", @"http://www.contoso1.com", ((global::System.String)o.@Line1));
            WriteElementString(@"City", @"http://www.contoso1.com", ((global::System.String)o.@City));
            WriteElementString(@"State", @"http://www.contoso1.com", ((global::System.String)o.@State));
            WriteElementString(@"Zip", @"http://www.contoso1.com", ((global::System.String)o.@Zip));
            WriteEndElement(o);
        }

        void Write22_PurchaseOrder(string n, string ns, global::PurchaseOrder o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::PurchaseOrder)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"PurchaseOrder", @"http://www.contoso1.com");
            Write20_Address(@"ShipTo", @"http://www.contoso1.com", ((global::Address)o.@ShipTo), false, false);
            WriteElementString(@"OrderDate", @"http://www.contoso1.com", ((global::System.String)o.@OrderDate));
            {
                global::OrderedItem[] a = (global::OrderedItem[])((global::OrderedItem[])o.@OrderedItems);
                if (a != null){
                    WriteStartElement(@"Items", @"http://www.contoso1.com", null, false);
                    for (int ia = 0; ia < a.Length; ia++) {
                        Write21_OrderedItem(@"OrderedItem", @"http://www.contoso1.com", ((global::OrderedItem)a[ia]), true, false);
                    }
                    WriteEndElement();
                }
            }
            WriteElementStringRaw(@"SubTotal", @"http://www.contoso1.com", System.Xml.XmlConvert.ToString((global::System.Decimal)((global::System.Decimal)o.@SubTotal)));
            WriteElementStringRaw(@"ShipCost", @"http://www.contoso1.com", System.Xml.XmlConvert.ToString((global::System.Decimal)((global::System.Decimal)o.@ShipCost)));
            WriteElementStringRaw(@"TotalCost", @"http://www.contoso1.com", System.Xml.XmlConvert.ToString((global::System.Decimal)((global::System.Decimal)o.@TotalCost)));
            WriteEndElement(o);
        }

        void Write23_Address(string n, string ns, global::Address o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::Address)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"Address", @"");
            WriteAttribute(@"Name", @"", ((global::System.String)o.@Name));
            WriteElementString(@"Line1", @"", ((global::System.String)o.@Line1));
            WriteElementString(@"City", @"", ((global::System.String)o.@City));
            WriteElementString(@"State", @"", ((global::System.String)o.@State));
            WriteElementString(@"Zip", @"", ((global::System.String)o.@Zip));
            WriteEndElement(o);
        }

        void Write24_OrderedItem(string n, string ns, global::OrderedItem o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::OrderedItem)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"OrderedItem", @"");
            WriteElementString(@"ItemName", @"", ((global::System.String)o.@ItemName));
            WriteElementString(@"Description", @"", ((global::System.String)o.@Description));
            WriteElementStringRaw(@"UnitPrice", @"", System.Xml.XmlConvert.ToString((global::System.Decimal)((global::System.Decimal)o.@UnitPrice)));
            WriteElementStringRaw(@"Quantity", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)o.@Quantity)));
            WriteElementStringRaw(@"LineTotal", @"", System.Xml.XmlConvert.ToString((global::System.Decimal)((global::System.Decimal)o.@LineTotal)));
            WriteEndElement(o);
        }

        void Write25_AliasedTestType(string n, string ns, global::AliasedTestType o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::AliasedTestType)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"AliasedTestType", @"");
            if ((object)(o.@Aliased) != null){
                if (o.@Aliased is global::System.Collections.Generic.List<global::System.Int32>) {
                    {
                        global::System.Collections.Generic.List<global::System.Int32> a = (global::System.Collections.Generic.List<global::System.Int32>)((global::System.Collections.Generic.List<global::System.Int32>)o.@Aliased);
                        if (a != null){
                            WriteStartElement(@"X", @"", null, false);
                            for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                                WriteElementStringRaw(@"int", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)a[ia])));
                            }
                            WriteEndElement();
                        }
                    }
                }
                else if (o.@Aliased is global::System.Collections.Generic.List<global::System.String>) {
                    {
                        global::System.Collections.Generic.List<global::System.String> a = (global::System.Collections.Generic.List<global::System.String>)((global::System.Collections.Generic.List<global::System.String>)o.@Aliased);
                        if (a != null){
                            WriteStartElement(@"Y", @"", null, false);
                            for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                                WriteNullableStringLiteral(@"string", @"", ((global::System.String)a[ia]));
                            }
                            WriteEndElement();
                        }
                    }
                }
                else if (o.@Aliased is global::System.Collections.Generic.List<global::System.Double>) {
                    {
                        global::System.Collections.Generic.List<global::System.Double> a = (global::System.Collections.Generic.List<global::System.Double>)((global::System.Collections.Generic.List<global::System.Double>)o.@Aliased);
                        if (a != null){
                            WriteStartElement(@"Z", @"", null, false);
                            for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                                WriteElementStringRaw(@"double", @"", System.Xml.XmlConvert.ToString((global::System.Double)((global::System.Double)a[ia])));
                            }
                            WriteEndElement();
                        }
                    }
                }
                else  if ((object)(o.@Aliased) != null){
                    throw CreateUnknownTypeException(o.@Aliased);
                }
            }
            WriteEndElement(o);
        }

        void Write27_DerivedClass1(string n, string ns, global::DerivedClass1 o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::DerivedClass1)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"DerivedClass1", @"");
            {
                global::MyCollection1 a = (global::MyCollection1)o.@Prop;
                if (a != null) {
                    System.Collections.IEnumerator e = ((System.Collections.Generic.IEnumerable<global::System.DateTime>)a).GetEnumerator();
                    if (e != null)
                    while (e.MoveNext()) {
                        global::System.DateTime ai = (global::System.DateTime)e.Current;
                        WriteElementStringRaw(@"Prop", @"", FromDateTime(((global::System.DateTime)ai)));
                    }
                }
            }
            WriteEndElement(o);
        }

        void Write26_BaseClass1(string n, string ns, global::BaseClass1 o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::BaseClass1)) {
                }
                else {
                    if (t == typeof(global::DerivedClass1)) {
                        Write27_DerivedClass1(n, ns,(global::DerivedClass1)o, isNullable, true);
                        return;
                    }
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"BaseClass1", @"");
            {
                global::MyCollection1 a = (global::MyCollection1)o.@Prop;
                if (a != null) {
                    System.Collections.IEnumerator e = ((System.Collections.Generic.IEnumerable<global::System.DateTime>)a).GetEnumerator();
                    if (e != null)
                    while (e.MoveNext()) {
                        global::System.DateTime ai = (global::System.DateTime)e.Current;
                        WriteElementStringRaw(@"Prop", @"", FromDateTime(((global::System.DateTime)ai)));
                    }
                }
            }
            WriteEndElement(o);
        }

        void Write29_Orchestra(string n, string ns, global::Orchestra o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::Orchestra)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"Orchestra", @"");
            {
                global::Instrument[] a = (global::Instrument[])((global::Instrument[])o.@Instruments);
                if (a != null){
                    WriteStartElement(@"Instruments", @"", null, false);
                    for (int ia = 0; ia < a.Length; ia++) {
                        Write28_Instrument(@"Instrument", @"", ((global::Instrument)a[ia]), true, false);
                    }
                    WriteEndElement();
                }
            }
            WriteEndElement(o);
        }

        void Write32_Pet(string n, string ns, global::Pet o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::Pet)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"Pet", @"");
            if (((global::System.String)o.@Animal) != @"Dog") {
                WriteElementString(@"Animal", @"", ((global::System.String)o.@Animal));
            }
            WriteElementString(@"Comment2", @"", ((global::System.String)o.@Comment2));
            WriteEndElement(o);
        }

        void Write33_DefaultValuesSetToNaN(string n, string ns, global::DefaultValuesSetToNaN o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::DefaultValuesSetToNaN)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"DefaultValuesSetToNaN", @"");
            if (!((global::System.Double)o.@DoubleField).Equals(System.Double.NaN)) {
                WriteElementStringRaw(@"DoubleField", @"", System.Xml.XmlConvert.ToString((global::System.Double)((global::System.Double)o.@DoubleField)));
            }
            if (!((global::System.Single)o.@SingleField).Equals(System.Single.NaN)) {
                WriteElementStringRaw(@"SingleField", @"", System.Xml.XmlConvert.ToString((global::System.Single)((global::System.Single)o.@SingleField)));
            }
            if (!((global::System.Double)o.@DoubleProp).Equals(System.Double.NaN)) {
                WriteElementStringRaw(@"DoubleProp", @"", System.Xml.XmlConvert.ToString((global::System.Double)((global::System.Double)o.@DoubleProp)));
            }
            if (!((global::System.Single)o.@FloatProp).Equals(System.Single.NaN)) {
                WriteElementStringRaw(@"FloatProp", @"", System.Xml.XmlConvert.ToString((global::System.Single)((global::System.Single)o.@FloatProp)));
            }
            WriteEndElement(o);
        }

        void Write34_Item(string n, string ns, global::DefaultValuesSetToPositiveInfinity o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::DefaultValuesSetToPositiveInfinity)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"DefaultValuesSetToPositiveInfinity", @"");
            if (!((global::System.Double)o.@DoubleField).Equals(System.Double.PositiveInfinity)) {
                WriteElementStringRaw(@"DoubleField", @"", System.Xml.XmlConvert.ToString((global::System.Double)((global::System.Double)o.@DoubleField)));
            }
            if (!((global::System.Single)o.@SingleField).Equals(System.Single.PositiveInfinity)) {
                WriteElementStringRaw(@"SingleField", @"", System.Xml.XmlConvert.ToString((global::System.Single)((global::System.Single)o.@SingleField)));
            }
            if (!((global::System.Double)o.@DoubleProp).Equals(System.Double.PositiveInfinity)) {
                WriteElementStringRaw(@"DoubleProp", @"", System.Xml.XmlConvert.ToString((global::System.Double)((global::System.Double)o.@DoubleProp)));
            }
            if (!((global::System.Single)o.@FloatProp).Equals(System.Single.PositiveInfinity)) {
                WriteElementStringRaw(@"FloatProp", @"", System.Xml.XmlConvert.ToString((global::System.Single)((global::System.Single)o.@FloatProp)));
            }
            WriteEndElement(o);
        }

        void Write35_Item(string n, string ns, global::DefaultValuesSetToNegativeInfinity o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::DefaultValuesSetToNegativeInfinity)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"DefaultValuesSetToNegativeInfinity", @"");
            if (!((global::System.Double)o.@DoubleField).Equals(System.Double.NegativeInfinity)) {
                WriteElementStringRaw(@"DoubleField", @"", System.Xml.XmlConvert.ToString((global::System.Double)((global::System.Double)o.@DoubleField)));
            }
            if (!((global::System.Single)o.@SingleField).Equals(System.Single.NegativeInfinity)) {
                WriteElementStringRaw(@"SingleField", @"", System.Xml.XmlConvert.ToString((global::System.Single)((global::System.Single)o.@SingleField)));
            }
            if (!((global::System.Double)o.@DoubleProp).Equals(System.Double.NegativeInfinity)) {
                WriteElementStringRaw(@"DoubleProp", @"", System.Xml.XmlConvert.ToString((global::System.Double)((global::System.Double)o.@DoubleProp)));
            }
            if (!((global::System.Single)o.@FloatProp).Equals(System.Single.NegativeInfinity)) {
                WriteElementStringRaw(@"FloatProp", @"", System.Xml.XmlConvert.ToString((global::System.Single)((global::System.Single)o.@FloatProp)));
            }
            WriteEndElement(o);
        }

        void Write36_Item(string n, string ns, global::TypeWithMismatchBetweenAttributeAndPropertyType o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::TypeWithMismatchBetweenAttributeAndPropertyType)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithMismatchBetweenAttributeAndPropertyType", @"");
            if (((global::System.Int32)o.@IntValue) != 1) {
                WriteAttribute(@"IntValue", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)o.@IntValue)));
            }
            WriteEndElement(o);
        }

        void Write38_MsgDocumentType(string n, string ns, global::MsgDocumentType o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::MsgDocumentType)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"MsgDocumentType", @"http://example.com");
            WriteAttribute(@"id", @"", ((global::System.String)o.@Id));
            {
                global::System.String[] a = (global::System.String[])o.@Refs;
                if (a != null) {
                    Writer.WriteStartAttribute(null, @"refs", @"");
                    for (int i = 0; i < a.Length; i++) {
                        global::System.String ai = (global::System.String)a[i];
                        if (i != 0) Writer.WriteString(" ");
                        WriteValue(ai);
                    }
                    Writer.WriteEndAttribute();
                }
            }
            WriteEndElement(o);
        }

        void Write41_RootClass(string n, string ns, global::RootClass o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::RootClass)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"RootClass", @"");
            {
                global::System.Collections.Generic.List<global::Parameter> a = (global::System.Collections.Generic.List<global::Parameter>)((global::System.Collections.Generic.List<global::Parameter>)o.@Parameters);
                if (a != null){
                    WriteStartElement(@"Parameters", @"", null, false);
                    for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                        Write40_Parameter(@"Parameter", @"", ((global::Parameter)a[ia]), true, false);
                    }
                    WriteEndElement();
                }
            }
            WriteEndElement(o);
        }

        void Write42_XElementWrapper(string n, string ns, global::XElementWrapper o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::XElementWrapper)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"XElementWrapper", @"");
            WriteSerializable((System.Xml.Serialization.IXmlSerializable)((global::System.Xml.Linq.XElement)o.@Value), @"Value", @"", false, true);
            WriteEndElement(o);
        }

        void Write43_XElementStruct(string n, string ns, global::XElementStruct o, bool needType) {
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::XElementStruct)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"XElementStruct", @"");
            WriteSerializable((System.Xml.Serialization.IXmlSerializable)((global::System.Xml.Linq.XElement)o.@xelement), @"xelement", @"", false, true);
            WriteEndElement(o);
        }

        void Write44_XElementArrayWrapper(string n, string ns, global::XElementArrayWrapper o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::XElementArrayWrapper)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"XElementArrayWrapper", @"");
            {
                global::System.Xml.Linq.XElement[] a = (global::System.Xml.Linq.XElement[])((global::System.Xml.Linq.XElement[])o.@xelements);
                if (a != null){
                    WriteStartElement(@"xelements", @"", null, false);
                    for (int ia = 0; ia < a.Length; ia++) {
                        WriteSerializable((System.Xml.Serialization.IXmlSerializable)((global::System.Xml.Linq.XElement)a[ia]), @"XElement", @"", true, true);
                    }
                    WriteEndElement();
                }
            }
            WriteEndElement(o);
        }

        void Write45_TypeWithDateTimeStringProperty(string n, string ns, global::SerializationTypes.TypeWithDateTimeStringProperty o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithDateTimeStringProperty)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithDateTimeStringProperty", @"");
            WriteElementString(@"DateTimeString", @"", ((global::System.String)o.@DateTimeString));
            WriteElementStringRaw(@"CurrentDateTime", @"", FromDateTime(((global::System.DateTime)o.@CurrentDateTime)));
            WriteEndElement(o);
        }

        void Write47_TypeWithGetSetArrayMembers(string n, string ns, global::SerializationTypes.TypeWithGetSetArrayMembers o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithGetSetArrayMembers)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithGetSetArrayMembers", @"");
            {
                global::SerializationTypes.SimpleType[] a = (global::SerializationTypes.SimpleType[])((global::SerializationTypes.SimpleType[])o.@F1);
                if (a != null){
                    WriteStartElement(@"F1", @"", null, false);
                    for (int ia = 0; ia < a.Length; ia++) {
                        Write46_SimpleType(@"SimpleType", @"", ((global::SerializationTypes.SimpleType)a[ia]), true, false);
                    }
                    WriteEndElement();
                }
            }
            {
                global::System.Int32[] a = (global::System.Int32[])((global::System.Int32[])o.@F2);
                if (a != null){
                    WriteStartElement(@"F2", @"", null, false);
                    for (int ia = 0; ia < a.Length; ia++) {
                        WriteElementStringRaw(@"int", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)a[ia])));
                    }
                    WriteEndElement();
                }
            }
            {
                global::SerializationTypes.SimpleType[] a = (global::SerializationTypes.SimpleType[])((global::SerializationTypes.SimpleType[])o.@P1);
                if (a != null){
                    WriteStartElement(@"P1", @"", null, false);
                    for (int ia = 0; ia < a.Length; ia++) {
                        Write46_SimpleType(@"SimpleType", @"", ((global::SerializationTypes.SimpleType)a[ia]), true, false);
                    }
                    WriteEndElement();
                }
            }
            {
                global::System.Int32[] a = (global::System.Int32[])((global::System.Int32[])o.@P2);
                if (a != null){
                    WriteStartElement(@"P2", @"", null, false);
                    for (int ia = 0; ia < a.Length; ia++) {
                        WriteElementStringRaw(@"int", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)a[ia])));
                    }
                    WriteEndElement();
                }
            }
            WriteEndElement(o);
        }

        void Write48_TypeWithGetOnlyArrayProperties(string n, string ns, global::SerializationTypes.TypeWithGetOnlyArrayProperties o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithGetOnlyArrayProperties)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithGetOnlyArrayProperties", @"");
            WriteEndElement(o);
        }

        void Write49_StructNotSerializable(string n, string ns, global::SerializationTypes.StructNotSerializable o, bool needType) {
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.StructNotSerializable)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"StructNotSerializable", @"");
            WriteElementStringRaw(@"value", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)o.@value)));
            WriteEndElement(o);
        }

        void Write50_TypeWithMyCollectionField(string n, string ns, global::SerializationTypes.TypeWithMyCollectionField o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithMyCollectionField)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithMyCollectionField", @"");
            {
                global::SerializationTypes.MyCollection<global::System.String> a = (global::SerializationTypes.MyCollection<global::System.String>)((global::SerializationTypes.MyCollection<global::System.String>)o.@Collection);
                if (a != null){
                    WriteStartElement(@"Collection", @"", null, false);
                    System.Collections.IEnumerator e = a.@GetEnumerator();
                    if (e != null)
                    while (e.MoveNext()) {
                        global::System.String ai = (global::System.String)e.Current;
                        WriteNullableStringLiteral(@"string", @"", ((global::System.String)ai));
                    }
                    WriteEndElement();
                }
            }
            WriteEndElement(o);
        }

        void Write51_Item(string n, string ns, global::SerializationTypes.TypeWithReadOnlyMyCollectionProperty o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithReadOnlyMyCollectionProperty)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithReadOnlyMyCollectionProperty", @"");
            {
                global::SerializationTypes.MyCollection<global::System.String> a = (global::SerializationTypes.MyCollection<global::System.String>)((global::SerializationTypes.MyCollection<global::System.String>)o.@Collection);
                if (a != null){
                    WriteStartElement(@"Collection", @"", null, false);
                    System.Collections.IEnumerator e = a.@GetEnumerator();
                    if (e != null)
                    while (e.MoveNext()) {
                        global::System.String ai = (global::System.String)e.Current;
                        WriteNullableStringLiteral(@"string", @"", ((global::System.String)ai));
                    }
                    WriteEndElement();
                }
            }
            WriteEndElement(o);
        }

        void Write53_TypeWithEnumMembers(string n, string ns, global::SerializationTypes.TypeWithEnumMembers o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithEnumMembers)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithEnumMembers", @"");
            WriteElementString(@"F1", @"", Write52_MyEnum(((global::SerializationTypes.MyEnum)o.@F1)));
            WriteElementString(@"P1", @"", Write52_MyEnum(((global::SerializationTypes.MyEnum)o.@P1)));
            WriteEndElement(o);
        }

        void Write54_DCStruct(string n, string ns, global::SerializationTypes.DCStruct o, bool needType) {
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.DCStruct)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"DCStruct", @"");
            WriteElementString(@"Data", @"", ((global::System.String)o.@Data));
            WriteEndElement(o);
        }

        void Write55_DCClassWithEnumAndStruct(string n, string ns, global::SerializationTypes.DCClassWithEnumAndStruct o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.DCClassWithEnumAndStruct)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"DCClassWithEnumAndStruct", @"");
            Write54_DCStruct(@"MyStruct", @"", ((global::SerializationTypes.DCStruct)o.@MyStruct), false);
            WriteElementString(@"MyEnum1", @"", Write52_MyEnum(((global::SerializationTypes.MyEnum)o.@MyEnum1)));
            WriteEndElement(o);
        }

        void Write56_BuiltInTypes(string n, string ns, global::SerializationTypes.BuiltInTypes o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.BuiltInTypes)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"BuiltInTypes", @"");
            WriteElementStringRaw(@"ByteArray", @"", FromByteArrayBase64(((global::System.Byte[])o.@ByteArray)));
            WriteEndElement(o);
        }

        void Write58_TypeB(string n, string ns, global::SerializationTypes.TypeB o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeB)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeB", @"");
            WriteElementString(@"Name", @"", ((global::System.String)o.@Name));
            WriteEndElement(o);
        }

        void Write59_TypeHasArrayOfASerializedAsB(string n, string ns, global::SerializationTypes.TypeHasArrayOfASerializedAsB o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeHasArrayOfASerializedAsB)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeHasArrayOfASerializedAsB", @"");
            {
                global::SerializationTypes.TypeA[] a = (global::SerializationTypes.TypeA[])((global::SerializationTypes.TypeA[])o.@Items);
                if (a != null){
                    WriteStartElement(@"Items", @"", null, false);
                    for (int ia = 0; ia < a.Length; ia++) {
                        Write57_TypeA(@"TypeA", @"", ((global::SerializationTypes.TypeA)a[ia]), true, false);
                    }
                    WriteEndElement();
                }
            }
            WriteEndElement(o);
        }

        void Write60_Item(string n, string ns, global::SerializationTypes.@__TypeNameWithSpecialCharacters漢ñ o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.@__TypeNameWithSpecialCharacters漢ñ)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"__TypeNameWithSpecialCharacters漢ñ", @"");
            WriteElementString(@"PropertyNameWithSpecialCharacters漢ñ", @"", ((global::System.String)o.@PropertyNameWithSpecialCharacters漢ñ));
            WriteEndElement(o);
        }

        void Write63_DerivedClassWithSameProperty2(string n, string ns, global::SerializationTypes.DerivedClassWithSameProperty2 o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.DerivedClassWithSameProperty2)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"DerivedClassWithSameProperty2", @"");
            WriteElementString(@"StringProperty", @"", ((global::System.String)o.@StringProperty));
            WriteElementStringRaw(@"IntProperty", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)o.@IntProperty)));
            WriteElementStringRaw(@"DateTimeProperty", @"", FromDateTime(((global::System.DateTime)o.@DateTimeProperty)));
            {
                global::System.Collections.Generic.List<global::System.String> a = (global::System.Collections.Generic.List<global::System.String>)((global::System.Collections.Generic.List<global::System.String>)o.@ListProperty);
                if (a != null){
                    WriteStartElement(@"ListProperty", @"", null, false);
                    for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                        WriteNullableStringLiteral(@"string", @"", ((global::System.String)a[ia]));
                    }
                    WriteEndElement();
                }
            }
            WriteEndElement(o);
        }

        void Write62_DerivedClassWithSameProperty(string n, string ns, global::SerializationTypes.DerivedClassWithSameProperty o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.DerivedClassWithSameProperty)) {
                }
                else {
                    if (t == typeof(global::SerializationTypes.DerivedClassWithSameProperty2)) {
                        Write63_DerivedClassWithSameProperty2(n, ns,(global::SerializationTypes.DerivedClassWithSameProperty2)o, isNullable, true);
                        return;
                    }
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"DerivedClassWithSameProperty", @"");
            WriteElementString(@"StringProperty", @"", ((global::System.String)o.@StringProperty));
            WriteElementStringRaw(@"IntProperty", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)o.@IntProperty)));
            WriteElementStringRaw(@"DateTimeProperty", @"", FromDateTime(((global::System.DateTime)o.@DateTimeProperty)));
            {
                global::System.Collections.Generic.List<global::System.String> a = (global::System.Collections.Generic.List<global::System.String>)((global::System.Collections.Generic.List<global::System.String>)o.@ListProperty);
                if (a != null){
                    WriteStartElement(@"ListProperty", @"", null, false);
                    for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                        WriteNullableStringLiteral(@"string", @"", ((global::System.String)a[ia]));
                    }
                    WriteEndElement();
                }
            }
            WriteEndElement(o);
        }

        void Write61_BaseClassWithSamePropertyName(string n, string ns, global::SerializationTypes.BaseClassWithSamePropertyName o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.BaseClassWithSamePropertyName)) {
                }
                else {
                    if (t == typeof(global::SerializationTypes.DerivedClassWithSameProperty)) {
                        Write62_DerivedClassWithSameProperty(n, ns,(global::SerializationTypes.DerivedClassWithSameProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.DerivedClassWithSameProperty2)) {
                        Write63_DerivedClassWithSameProperty2(n, ns,(global::SerializationTypes.DerivedClassWithSameProperty2)o, isNullable, true);
                        return;
                    }
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"BaseClassWithSamePropertyName", @"");
            WriteElementString(@"StringProperty", @"", ((global::System.String)o.@StringProperty));
            WriteElementStringRaw(@"IntProperty", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)o.@IntProperty)));
            WriteElementStringRaw(@"DateTimeProperty", @"", FromDateTime(((global::System.DateTime)o.@DateTimeProperty)));
            {
                global::System.Collections.Generic.List<global::System.String> a = (global::System.Collections.Generic.List<global::System.String>)((global::System.Collections.Generic.List<global::System.String>)o.@ListProperty);
                if (a != null){
                    WriteStartElement(@"ListProperty", @"", null, false);
                    for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                        WriteNullableStringLiteral(@"string", @"", ((global::System.String)a[ia]));
                    }
                    WriteEndElement();
                }
            }
            WriteEndElement(o);
        }

        void Write64_Item(string n, string ns, global::SerializationTypes.TypeWithDateTimePropertyAsXmlTime o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithDateTimePropertyAsXmlTime)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithDateTimePropertyAsXmlTime", @"");
            {
                WriteValue(FromTime(((global::System.DateTime)o.@Value)));
            }
            WriteEndElement(o);
        }

        void Write65_TypeWithByteArrayAsXmlText(string n, string ns, global::SerializationTypes.TypeWithByteArrayAsXmlText o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithByteArrayAsXmlText)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithByteArrayAsXmlText", @"");
            if ((object)(o.@Value) != null){
                WriteValue(FromByteArrayBase64(((global::System.Byte[])o.@Value)));
            }
            WriteEndElement(o);
        }

        void Write66_SimpleDC(string n, string ns, global::SerializationTypes.SimpleDC o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.SimpleDC)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"SimpleDC", @"");
            WriteElementString(@"Data", @"", ((global::System.String)o.@Data));
            WriteEndElement(o);
        }

        void Write67_Item(string n, string ns, global::SerializationTypes.TypeWithXmlTextAttributeOnArray o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithXmlTextAttributeOnArray)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithXmlTextAttributeOnArray", @"http://schemas.xmlsoap.org/ws/2005/04/discovery");
            {
                global::System.String[] a = (global::System.String[])o.@Text;
                if (a != null) {
                    for (int ia = 0; ia < a.Length; ia++) {
                        if ((object)(a[ia]) != null){
                            WriteValue(((global::System.String)a[ia]));
                        }
                    }
                }
            }
            WriteEndElement(o);
        }

        void Write69_ClassImplementsInterface(string n, string ns, global::SerializationTypes.ClassImplementsInterface o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.ClassImplementsInterface)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"ClassImplementsInterface", @"");
            WriteElementString(@"ClassID", @"", ((global::System.String)o.@ClassID));
            WriteElementString(@"DisplayName", @"", ((global::System.String)o.@DisplayName));
            WriteElementString(@"Id", @"", ((global::System.String)o.@Id));
            WriteElementStringRaw(@"IsLoaded", @"", System.Xml.XmlConvert.ToString((global::System.Boolean)((global::System.Boolean)o.@IsLoaded)));
            WriteEndElement(o);
        }

        void Write70_SomeStruct(string n, string ns, global::SerializationTypes.SomeStruct o, bool needType) {
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.SomeStruct)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"SomeStruct", @"");
            WriteElementStringRaw(@"A", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)o.@A)));
            WriteElementStringRaw(@"B", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)o.@B)));
            WriteEndElement(o);
        }

        void Write71_WithStruct(string n, string ns, global::SerializationTypes.WithStruct o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.WithStruct)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"WithStruct", @"");
            Write70_SomeStruct(@"Some", @"", ((global::SerializationTypes.SomeStruct)o.@Some), false);
            WriteEndElement(o);
        }

        void Write74_WithEnums(string n, string ns, global::SerializationTypes.WithEnums o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.WithEnums)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"WithEnums", @"");
            WriteElementString(@"Int", @"", Write72_IntEnum(((global::SerializationTypes.IntEnum)o.@Int)));
            WriteElementString(@"Short", @"", Write73_ShortEnum(((global::SerializationTypes.ShortEnum)o.@Short)));
            WriteEndElement(o);
        }

        void Write75_WithNullables(string n, string ns, global::SerializationTypes.WithNullables o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.WithNullables)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"WithNullables", @"");
            if (o.@Optional != null) {
                WriteElementString(@"Optional", @"", Write72_IntEnum(((global::SerializationTypes.IntEnum)o.@Optional)));
            }
            else {
                WriteNullTagLiteral(@"Optional", @"");
            }
            if (o.@Optionull != null) {
                WriteElementString(@"Optionull", @"", Write72_IntEnum(((global::SerializationTypes.IntEnum)o.@Optionull)));
            }
            else {
                WriteNullTagLiteral(@"Optionull", @"");
            }
            if (o.@OptionalInt != null) {
                WriteNullableStringLiteralRaw(@"OptionalInt", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)o.@OptionalInt)));
            }
            else {
                WriteNullTagLiteral(@"OptionalInt", @"");
            }
            if (o.@OptionullInt != null) {
                WriteNullableStringLiteralRaw(@"OptionullInt", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)o.@OptionullInt)));
            }
            else {
                WriteNullTagLiteral(@"OptionullInt", @"");
            }
            if (o.@Struct1 != null) {
                Write70_SomeStruct(@"Struct1", @"", ((global::SerializationTypes.SomeStruct)o.@Struct1), false);
            }
            else {
                WriteNullTagLiteral(@"Struct1", @"");
            }
            if (o.@Struct2 != null) {
                Write70_SomeStruct(@"Struct2", @"", ((global::SerializationTypes.SomeStruct)o.@Struct2), false);
            }
            else {
                WriteNullTagLiteral(@"Struct2", @"");
            }
            WriteEndElement(o);
        }

        void Write82_XmlSerializerAttributes(string n, string ns, global::SerializationTypes.XmlSerializerAttributes o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.XmlSerializerAttributes)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"XmlSerializerAttributes", @"");
            WriteAttribute(@"XmlAttributeName", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)o.@XmlAttributeProperty)));
            {
                if (o.@EnumType == SerializationTypes.ItemChoiceType.@Word && ((object)(o.@MyChoice) != null)) {
                    if (((object)o.@MyChoice) != null && !(o.@MyChoice is global::System.String)) throw CreateMismatchChoiceException(@"System.String", @"EnumType", @"SerializationTypes.ItemChoiceType.@Word");
                    WriteElementString(@"Word", @"", ((global::System.String)o.@MyChoice));
                }
                else if (o.@EnumType == SerializationTypes.ItemChoiceType.@Number && ((object)(o.@MyChoice) != null)) {
                    if (((object)o.@MyChoice) != null && !(o.@MyChoice is global::System.Int32)) throw CreateMismatchChoiceException(@"System.Int32", @"EnumType", @"SerializationTypes.ItemChoiceType.@Number");
                    WriteElementStringRaw(@"Number", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)o.@MyChoice)));
                }
                else if (o.@EnumType == SerializationTypes.ItemChoiceType.@DecimalNumber && ((object)(o.@MyChoice) != null)) {
                    if (((object)o.@MyChoice) != null && !(o.@MyChoice is global::System.Double)) throw CreateMismatchChoiceException(@"System.Double", @"EnumType", @"SerializationTypes.ItemChoiceType.@DecimalNumber");
                    WriteElementStringRaw(@"DecimalNumber", @"", System.Xml.XmlConvert.ToString((global::System.Double)((global::System.Double)o.@MyChoice)));
                }
                else  if ((object)(o.@MyChoice) != null){
                    throw CreateUnknownTypeException(o.@MyChoice);
                }
            }
            Write1_Object(@"XmlIncludeProperty", @"", ((global::System.Object)o.@XmlIncludeProperty), false, false);
            {
                global::SerializationTypes.ItemChoiceType[] a = (global::SerializationTypes.ItemChoiceType[])((global::SerializationTypes.ItemChoiceType[])o.@XmlEnumProperty);
                if (a != null){
                    WriteStartElement(@"XmlEnumProperty", @"", null, false);
                    for (int ia = 0; ia < a.Length; ia++) {
                        WriteElementString(@"ItemChoiceType", @"", Write81_ItemChoiceType(((global::SerializationTypes.ItemChoiceType)a[ia])));
                    }
                    WriteEndElement();
                }
            }
            if ((object)(o.@XmlTextProperty) != null){
                WriteValue(((global::System.String)o.@XmlTextProperty));
            }
            WriteElementString(@"XmlNamespaceDeclarationsProperty", @"", ((global::System.String)o.@XmlNamespaceDeclarationsProperty));
            WriteElementStringRaw(@"XmlElementPropertyNode", @"http://element", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)o.@XmlElementProperty)));
            {
                global::System.Object[] a = (global::System.Object[])((global::System.Object[])o.@XmlArrayProperty);
                if (a != null){
                    WriteStartElement(@"CustomXmlArrayProperty", @"http://mynamespace", null, false);
                    for (int ia = 0; ia < a.Length; ia++) {
                        WriteNullableStringLiteral(@"string", @"http://mynamespace", ((global::System.String)a[ia]));
                    }
                    WriteEndElement();
                }
            }
            WriteEndElement(o);
        }

        void Write83_TypeWithAnyAttribute(string n, string ns, global::SerializationTypes.TypeWithAnyAttribute o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithAnyAttribute)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithAnyAttribute", @"");
            WriteAttribute(@"IntProperty", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)o.@IntProperty)));
            {
                global::System.Xml.XmlAttribute[] a = (global::System.Xml.XmlAttribute[])o.@Attributes;
                if (a != null) {
                    for (int i = 0; i < a.Length; i++) {
                        global::System.Xml.XmlAttribute ai = (global::System.Xml.XmlAttribute)a[i];
                        WriteXmlAttribute(ai, o);
                    }
                }
            }
            WriteElementString(@"Name", @"", ((global::System.String)o.@Name));
            WriteEndElement(o);
        }

        void Write84_KnownTypesThroughConstructor(string n, string ns, global::SerializationTypes.KnownTypesThroughConstructor o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.KnownTypesThroughConstructor)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"KnownTypesThroughConstructor", @"");
            Write1_Object(@"EnumValue", @"", ((global::System.Object)o.@EnumValue), false, false);
            Write1_Object(@"SimpleTypeValue", @"", ((global::System.Object)o.@SimpleTypeValue), false, false);
            WriteEndElement(o);
        }

        void Write85_SimpleKnownTypeValue(string n, string ns, global::SerializationTypes.SimpleKnownTypeValue o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.SimpleKnownTypeValue)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"SimpleKnownTypeValue", @"");
            WriteElementString(@"StrProperty", @"", ((global::System.String)o.@StrProperty));
            WriteEndElement(o);
        }

        void Write86_TypeWithPropertyNameSpecified(string n, string ns, global::SerializationTypes.TypeWithPropertyNameSpecified o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithPropertyNameSpecified)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithPropertyNameSpecified", @"");
            if (o.@MyFieldSpecified) {
                WriteElementString(@"MyField", @"", ((global::System.String)o.@MyField));
            }
            if (o.@MyFieldIgnoredSpecified) {
                WriteElementStringRaw(@"MyFieldIgnored", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)o.@MyFieldIgnored)));
            }
            WriteEndElement(o);
        }

        void Write87_TypeWithXmlSchemaFormAttribute(string n, string ns, global::SerializationTypes.TypeWithXmlSchemaFormAttribute o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithXmlSchemaFormAttribute)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithXmlSchemaFormAttribute", @"");
            {
                global::System.Collections.Generic.List<global::System.Int32> a = (global::System.Collections.Generic.List<global::System.Int32>)((global::System.Collections.Generic.List<global::System.Int32>)o.@UnqualifiedSchemaFormListProperty);
                if (a != null){
                    WriteStartElement(@"UnqualifiedSchemaFormListProperty", @"", null, false);
                    for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                        WriteElementStringRaw(@"int", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)a[ia])));
                    }
                    WriteEndElement();
                }
            }
            {
                global::System.Collections.Generic.List<global::System.String> a = (global::System.Collections.Generic.List<global::System.String>)((global::System.Collections.Generic.List<global::System.String>)o.@NoneSchemaFormListProperty);
                if (a != null){
                    WriteStartElement(@"NoneSchemaFormListProperty", @"", null, false);
                    for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                        WriteElementString(@"NoneParameter", @"", ((global::System.String)a[ia]));
                    }
                    WriteEndElement();
                }
            }
            {
                global::System.Collections.Generic.List<global::System.Boolean> a = (global::System.Collections.Generic.List<global::System.Boolean>)((global::System.Collections.Generic.List<global::System.Boolean>)o.@QualifiedSchemaFormListProperty);
                if (a != null){
                    WriteStartElement(@"QualifiedSchemaFormListProperty", @"", null, false);
                    for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                        WriteElementStringRaw(@"QualifiedParameter", @"", System.Xml.XmlConvert.ToString((global::System.Boolean)((global::System.Boolean)a[ia])));
                    }
                    WriteEndElement();
                }
            }
            WriteEndElement(o);
        }

        void Write88_Item(string n, string ns, global::SerializationTypes.TypeWithTypeNameInXmlTypeAttribute o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithTypeNameInXmlTypeAttribute)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"MyXmlType", @"");
            WriteAttribute(@"XmlAttributeForm", @"", ((global::System.String)o.@XmlAttributeForm));
            WriteEndElement(o);
        }

        void Write90_CustomElement(string n, string ns, global::SerializationTypes.CustomElement o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.CustomElement)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"CustomElement", @"");
            WriteAttribute(@"name", @"", ((global::System.String)o.@Name));
            {
                global::System.Xml.XmlAttribute[] a = (global::System.Xml.XmlAttribute[])o.@Attributes;
                if (a != null) {
                    for (int i = 0; i < a.Length; i++) {
                        global::System.Xml.XmlAttribute ai = (global::System.Xml.XmlAttribute)a[i];
                        WriteXmlAttribute(ai, o);
                    }
                }
            }
            {
                global::System.Xml.XmlNode[] a = (global::System.Xml.XmlNode[])o.@CustomAttributes;
                if (a != null) {
                    for (int i = 0; i < a.Length; i++) {
                        global::System.Xml.XmlNode ai = (global::System.Xml.XmlNode)a[i];
                        WriteXmlAttribute(ai, o);
                    }
                }
            }
            WriteEndElement(o);
        }

        void Write91_CustomDocument(string n, string ns, global::SerializationTypes.CustomDocument o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.CustomDocument)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"CustomDocument", @"");
            {
                global::System.Collections.Generic.List<global::SerializationTypes.CustomElement> a = (global::System.Collections.Generic.List<global::SerializationTypes.CustomElement>)o.@CustomItems;
                if (a != null) {
                    for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                        Write90_CustomElement(@"customElement", @"", ((global::SerializationTypes.CustomElement)a[ia]), false, false);
                    }
                }
            }
            {
                global::System.Xml.XmlNode[] a = (global::System.Xml.XmlNode[])o.@Items;
                if (a != null) {
                    for (int ia = 0; ia < a.Length; ia++) {
                        if ((a[ia]) is System.Xml.XmlNode || a[ia] == null) {
                            WriteElementLiteral((System.Xml.XmlNode)a[ia], @"", null, false, true);
                        }
                        else {
                            throw CreateInvalidAnyTypeException(a[ia]);
                        }
                    }
                }
            }
            WriteEndElement(o);
        }

        void Write92_Item(string n, string ns, global::SerializationTypes.TypeWithNonPublicDefaultConstructor o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithNonPublicDefaultConstructor)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithNonPublicDefaultConstructor", @"");
            WriteElementString(@"Name", @"", ((global::System.String)o.@Name));
            WriteEndElement(o);
        }

        void Write93_ServerSettings(string n, string ns, global::SerializationTypes.ServerSettings o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.ServerSettings)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"ServerSettings", @"");
            WriteElementString(@"DS2Root", @"", ((global::System.String)o.@DS2Root));
            WriteElementString(@"MetricConfigUrl", @"", ((global::System.String)o.@MetricConfigUrl));
            WriteEndElement(o);
        }

        void Write94_TypeWithXmlQualifiedName(string n, string ns, global::SerializationTypes.TypeWithXmlQualifiedName o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithXmlQualifiedName)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithXmlQualifiedName", @"");
            WriteElementQualifiedName(@"Value", @"", ((global::System.Xml.XmlQualifiedName)o.@Value));
            WriteEndElement(o);
        }

        void Write95_TypeWith2DArrayProperty2(string n, string ns, global::SerializationTypes.TypeWith2DArrayProperty2 o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWith2DArrayProperty2)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWith2DArrayProperty2", @"");
            {
                global::SerializationTypes.SimpleType[][] a = (global::SerializationTypes.SimpleType[][])((global::SerializationTypes.SimpleType[][])o.@TwoDArrayOfSimpleType);
                if (a != null){
                    WriteStartElement(@"TwoDArrayOfSimpleType", @"", null, false);
                    for (int ia = 0; ia < a.Length; ia++) {
                        {
                            global::SerializationTypes.SimpleType[] aa = (global::SerializationTypes.SimpleType[])((global::SerializationTypes.SimpleType[])a[ia]);
                            if (aa != null){
                                WriteStartElement(@"SimpleType", @"", null, false);
                                for (int iaa = 0; iaa < aa.Length; iaa++) {
                                    Write46_SimpleType(@"SimpleType", @"", ((global::SerializationTypes.SimpleType)aa[iaa]), true, false);
                                }
                                WriteEndElement();
                            }
                        }
                    }
                    WriteEndElement();
                }
            }
            WriteEndElement(o);
        }

        void Write96_Item(string n, string ns, global::SerializationTypes.TypeWithPropertiesHavingDefaultValue o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithPropertiesHavingDefaultValue)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithPropertiesHavingDefaultValue", @"");
            if ((((global::System.String)o.@EmptyStringProperty) != null) && (((global::System.String)o.@EmptyStringProperty).Length != 0)) {
                WriteElementString(@"EmptyStringProperty", @"", ((global::System.String)o.@EmptyStringProperty));
            }
            if (((global::System.String)o.@StringProperty) != @"DefaultString") {
                WriteElementString(@"StringProperty", @"", ((global::System.String)o.@StringProperty));
            }
            if (((global::System.Int32)o.@IntProperty) != 11) {
                WriteElementStringRaw(@"IntProperty", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)o.@IntProperty)));
            }
            WriteElementString(@"CharProperty", @"", FromChar(((global::System.Char)o.@CharProperty)));
            WriteEndElement(o);
        }

        void Write97_Item(string n, string ns, global::SerializationTypes.TypeWithEnumPropertyHavingDefaultValue o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithEnumPropertyHavingDefaultValue)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithEnumPropertyHavingDefaultValue", @"");
            if (((global::SerializationTypes.IntEnum)o.@EnumProperty) != global::SerializationTypes.IntEnum.@Option1) {
                WriteElementString(@"EnumProperty", @"", Write72_IntEnum(((global::SerializationTypes.IntEnum)o.@EnumProperty)));
            }
            WriteEndElement(o);
        }

        void Write98_Item(string n, string ns, global::SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithEnumFlagPropertyHavingDefaultValue", @"");
            if (((global::SerializationTypes.EnumFlags)o.@EnumProperty) != (global::SerializationTypes.EnumFlags.@One | 
            global::SerializationTypes.EnumFlags.@Four)) {
                WriteElementString(@"EnumProperty", @"", Write68_EnumFlags(((global::SerializationTypes.EnumFlags)o.@EnumProperty)));
            }
            WriteEndElement(o);
        }

        void Write99_TypeWithShouldSerializeMethod(string n, string ns, global::SerializationTypes.TypeWithShouldSerializeMethod o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithShouldSerializeMethod)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithShouldSerializeMethod", @"");
            if (o.@ShouldSerializeFoo()) {
                WriteElementString(@"Foo", @"", ((global::System.String)o.@Foo));
            }
            WriteEndElement(o);
        }

        void Write100_Item(string n, string ns, global::SerializationTypes.KnownTypesThroughConstructorWithArrayProperties o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.KnownTypesThroughConstructorWithArrayProperties)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"KnownTypesThroughConstructorWithArrayProperties", @"");
            Write1_Object(@"StringArrayValue", @"", ((global::System.Object)o.@StringArrayValue), false, false);
            Write1_Object(@"IntArrayValue", @"", ((global::System.Object)o.@IntArrayValue), false, false);
            WriteEndElement(o);
        }

        void Write101_Item(string n, string ns, global::SerializationTypes.KnownTypesThroughConstructorWithValue o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.KnownTypesThroughConstructorWithValue)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"KnownTypesThroughConstructorWithValue", @"");
            Write1_Object(@"Value", @"", ((global::System.Object)o.@Value), false, false);
            WriteEndElement(o);
        }

        void Write102_Item(string n, string ns, global::SerializationTypes.TypeWithTypesHavingCustomFormatter o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithTypesHavingCustomFormatter)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithTypesHavingCustomFormatter", @"");
            WriteElementStringRaw(@"DateTimeContent", @"", FromDateTime(((global::System.DateTime)o.@DateTimeContent)));
            WriteElementQualifiedName(@"QNameContent", @"", ((global::System.Xml.XmlQualifiedName)o.@QNameContent));
            WriteElementStringRaw(@"DateContent", @"", FromDate(((global::System.DateTime)o.@DateContent)));
            WriteElementString(@"NameContent", @"", FromXmlName(((global::System.String)o.@NameContent)));
            WriteElementString(@"NCNameContent", @"", FromXmlNCName(((global::System.String)o.@NCNameContent)));
            WriteElementString(@"NMTOKENContent", @"", FromXmlNmToken(((global::System.String)o.@NMTOKENContent)));
            WriteElementString(@"NMTOKENSContent", @"", FromXmlNmTokens(((global::System.String)o.@NMTOKENSContent)));
            WriteElementStringRaw(@"Base64BinaryContent", @"", FromByteArrayBase64(((global::System.Byte[])o.@Base64BinaryContent)));
            WriteElementStringRaw(@"HexBinaryContent", @"", FromByteArrayHex(((global::System.Byte[])o.@HexBinaryContent)));
            WriteEndElement(o);
        }

        void Write104_Item(string n, string ns, global::SerializationTypes.TypeWithArrayPropertyHavingChoice o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithArrayPropertyHavingChoice)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithArrayPropertyHavingChoice", @"");
            {
                global::System.Object[] a = (global::System.Object[])o.@ManyChoices;
                if (a != null) {
                    global::SerializationTypes.MoreChoices[] c = (global::SerializationTypes.MoreChoices[])o.@ChoiceArray;
                    if (c == null || c.Length < a.Length) {
                        throw CreateInvalidChoiceIdentifierValueException(@"SerializationTypes.MoreChoices", @"ChoiceArray");}
                    for (int ia = 0; ia < a.Length; ia++) {
                        global::System.Object ai = (global::System.Object)a[ia];
                        global::SerializationTypes.MoreChoices ci = (global::SerializationTypes.MoreChoices)c[ia];
                        {
                            if (ci == SerializationTypes.MoreChoices.@Item && ((object)(ai) != null)) {
                                if (((object)ai) != null && !(ai is global::System.String)) throw CreateMismatchChoiceException(@"System.String", @"ChoiceArray", @"SerializationTypes.MoreChoices.@Item");
                                WriteElementString(@"Item", @"", ((global::System.String)ai));
                            }
                            else if (ci == SerializationTypes.MoreChoices.@Amount && ((object)(ai) != null)) {
                                if (((object)ai) != null && !(ai is global::System.Int32)) throw CreateMismatchChoiceException(@"System.Int32", @"ChoiceArray", @"SerializationTypes.MoreChoices.@Amount");
                                WriteElementStringRaw(@"Amount", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)ai)));
                            }
                            else  if ((object)(ai) != null){
                                throw CreateUnknownTypeException(ai);
                            }
                        }
                    }
                }
            }
            WriteEndElement(o);
        }

        void Write105_TypeWithFieldsOrdered(string n, string ns, global::SerializationTypes.TypeWithFieldsOrdered o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithFieldsOrdered)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithFieldsOrdered", @"");
            WriteElementStringRaw(@"IntField2", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)o.@IntField2)));
            WriteElementStringRaw(@"IntField1", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)o.@IntField1)));
            WriteElementString(@"strfld", @"", ((global::System.String)o.@StringField2));
            WriteElementString(@"strfld", @"", ((global::System.String)o.@StringField1));
            WriteEndElement(o);
        }

        void Write89_Item(string n, string ns, global::SerializationTypes.TypeWithSchemaFormInXmlAttribute o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithSchemaFormInXmlAttribute)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(null, @"");
            WriteAttribute(@"TestProperty", @"http://test.com", ((global::System.String)o.@TestProperty));
            WriteEndElement(o);
        }

        protected override void InitCallbacks() {
        }
    }

    public class XmlSerializationReader1 : System.Xml.Serialization.XmlSerializationReader {

        public object Read115_TypeWithXmlElementProperty() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id1_TypeWithXmlElementProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read2_TypeWithXmlElementProperty(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithXmlElementProperty");
            }
            return (object)o;
        }

        public object Read116_TypeWithXmlDocumentProperty() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id3_TypeWithXmlDocumentProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read3_TypeWithXmlDocumentProperty(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithXmlDocumentProperty");
            }
            return (object)o;
        }

        public object Read117_TypeWithBinaryProperty() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id4_TypeWithBinaryProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read4_TypeWithBinaryProperty(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithBinaryProperty");
            }
            return (object)o;
        }

        public object Read118_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id5_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read6_Item(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithDateTimeOffsetProperties");
            }
            return (object)o;
        }

        public object Read119_TypeWithTimeSpanProperty() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id6_TypeWithTimeSpanProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read7_TypeWithTimeSpanProperty(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithTimeSpanProperty");
            }
            return (object)o;
        }

        public object Read120_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id7_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read8_Item(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithDefaultTimeSpanProperty");
            }
            return (object)o;
        }

        public object Read121_TypeWithByteProperty() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id8_TypeWithByteProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read9_TypeWithByteProperty(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithByteProperty");
            }
            return (object)o;
        }

        public object Read122_TypeWithXmlNodeArrayProperty() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id9_TypeWithXmlNodeArrayProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read10_TypeWithXmlNodeArrayProperty(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithXmlNodeArrayProperty");
            }
            return (object)o;
        }

        public object Read123_Animal() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id10_Animal && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read11_Animal(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":Animal");
            }
            return (object)o;
        }

        public object Read124_Dog() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id11_Dog && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read13_Dog(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":Dog");
            }
            return (object)o;
        }

        public object Read125_DogBreed() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id12_DogBreed && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read12_DogBreed(Reader.ReadElementString());
                        }
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":DogBreed");
            }
            return (object)o;
        }

        public object Read126_Group() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id13_Group && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read15_Group(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":Group");
            }
            return (object)o;
        }

        public object Read127_Vehicle() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id14_Vehicle && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read14_Vehicle(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":Vehicle");
            }
            return (object)o;
        }

        public object Read128_Employee() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id15_Employee && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read16_Employee(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":Employee");
            }
            return (object)o;
        }

        public object Read129_BaseClass() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id16_BaseClass && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read18_BaseClass(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":BaseClass");
            }
            return (object)o;
        }

        public object Read130_DerivedClass() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id17_DerivedClass && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read17_DerivedClass(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":DerivedClass");
            }
            return (object)o;
        }

        public object Read131_SimpleBaseClass() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id18_SimpleBaseClass && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read20_SimpleBaseClass(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":SimpleBaseClass");
            }
            return (object)o;
        }

        public object Read132_SimpleDerivedClass() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id19_SimpleDerivedClass && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read19_SimpleDerivedClass(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":SimpleDerivedClass");
            }
            return (object)o;
        }

        public object Read133_BaseIXmlSerializable() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id20_BaseIXmlSerializable && (object) Reader.NamespaceURI == (object)id21_Item)) {
                        System.Xml.XmlQualifiedName tser = GetXsiType();
                        if (tser == null || ((object) ((System.Xml.XmlQualifiedName)tser).Name == (object)id20_BaseIXmlSerializable && (object) ((System.Xml.XmlQualifiedName)tser).Namespace == (object)id21_Item)) {
                            o = (global::XmlSerializableBaseClass)ReadSerializable(( System.Xml.Serialization.IXmlSerializable)new global::XmlSerializableBaseClass());
                        }
                        else if (tser == null || ((object) ((System.Xml.XmlQualifiedName)tser).Name == (object)id22_DerivedIXmlSerializable && (object) ((System.Xml.XmlQualifiedName)tser).Namespace == (object)id21_Item)) {
                            o = (global::XmlSerializableBaseClass)ReadSerializable(( System.Xml.Serialization.IXmlSerializable)new global::XmlSerializableDerivedClass());
                        }
                        else {
                            UnknownNode(null);
                        }
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @"http://example.com/serializer-test-namespace:BaseIXmlSerializable");
            }
            return (object)o;
        }

        public object Read134_DerivedIXmlSerializable() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id22_DerivedIXmlSerializable && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = (global::XmlSerializableDerivedClass)ReadSerializable(( System.Xml.Serialization.IXmlSerializable)new global::XmlSerializableDerivedClass());
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":DerivedIXmlSerializable");
            }
            return (object)o;
        }

        public object Read135_PurchaseOrder() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id23_PurchaseOrder && (object) Reader.NamespaceURI == (object)id24_httpwwwcontoso1com)) {
                        o = Read23_PurchaseOrder(false, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @"http://www.contoso1.com:PurchaseOrder");
            }
            return (object)o;
        }

        public object Read136_Address() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id25_Address && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read24_Address(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":Address");
            }
            return (object)o;
        }

        public object Read137_OrderedItem() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id26_OrderedItem && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read25_OrderedItem(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":OrderedItem");
            }
            return (object)o;
        }

        public object Read138_AliasedTestType() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id27_AliasedTestType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read26_AliasedTestType(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":AliasedTestType");
            }
            return (object)o;
        }

        public object Read139_BaseClass1() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id28_BaseClass1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read27_BaseClass1(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":BaseClass1");
            }
            return (object)o;
        }

        public object Read140_DerivedClass1() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id29_DerivedClass1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read28_DerivedClass1(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":DerivedClass1");
            }
            return (object)o;
        }

        public object Read141_ArrayOfDateTime() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id30_ArrayOfDateTime && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        if (!ReadNull()) {
                            if ((object)(o) == null) o = new global::MyCollection1();
                            global::MyCollection1 a_0_0 = (global::MyCollection1)o;
                            if ((Reader.IsEmptyElement)) {
                                Reader.Skip();
                            }
                            else {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                        do {
                                            if (((object) Reader.LocalName == (object)id31_dateTime && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                {
                                                    a_0_0.Add(ToDateTime(Reader.ReadElementString()));
                                                }
                                                break;
                                            }
                                            UnknownNode(null, @":dateTime");
                                        } while (false);
                                    }
                                    else {
                                        UnknownNode(null, @":dateTime");
                                    }
                                    Reader.MoveToContent();
                                }
                            ReadEndElement();
                            }
                        }
                        else {
                            if ((object)(o) == null) o = new global::MyCollection1();
                            global::MyCollection1 a_0_0 = (global::MyCollection1)o;
                        }
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":ArrayOfDateTime");
            }
            return (object)o;
        }

        public object Read142_Orchestra() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id32_Orchestra && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read30_Orchestra(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":Orchestra");
            }
            return (object)o;
        }

        public object Read143_Instrument() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id33_Instrument && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read29_Instrument(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":Instrument");
            }
            return (object)o;
        }

        public object Read144_Brass() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id34_Brass && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read31_Brass(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":Brass");
            }
            return (object)o;
        }

        public object Read145_Trumpet() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id35_Trumpet && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read32_Trumpet(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":Trumpet");
            }
            return (object)o;
        }

        public object Read146_Pet() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id36_Pet && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read33_Pet(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":Pet");
            }
            return (object)o;
        }

        public object Read147_DefaultValuesSetToNaN() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id37_DefaultValuesSetToNaN && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read34_DefaultValuesSetToNaN(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":DefaultValuesSetToNaN");
            }
            return (object)o;
        }

        public object Read148_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id38_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read35_Item(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":DefaultValuesSetToPositiveInfinity");
            }
            return (object)o;
        }

        public object Read149_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id39_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read36_Item(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":DefaultValuesSetToNegativeInfinity");
            }
            return (object)o;
        }

        public object Read150_RootElement() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id40_RootElement && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read37_Item(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":RootElement");
            }
            return (object)o;
        }

        public object Read151_TypeWithLinkedProperty() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id41_TypeWithLinkedProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read38_TypeWithLinkedProperty(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithLinkedProperty");
            }
            return (object)o;
        }

        public object Read152_Document() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id42_Document && (object) Reader.NamespaceURI == (object)id43_httpexamplecom)) {
                        o = Read39_MsgDocumentType(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @"http://example.com:Document");
            }
            return (object)o;
        }

        public object Read153_RootClass() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id44_RootClass && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read42_RootClass(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":RootClass");
            }
            return (object)o;
        }

        public object Read154_Parameter() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id45_Parameter && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read41_Parameter(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":Parameter");
            }
            return (object)o;
        }

        public object Read155_XElementWrapper() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id46_XElementWrapper && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read43_XElementWrapper(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":XElementWrapper");
            }
            return (object)o;
        }

        public object Read156_XElementStruct() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id47_XElementStruct && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read44_XElementStruct(true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":XElementStruct");
            }
            return (object)o;
        }

        public object Read157_XElementArrayWrapper() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id48_XElementArrayWrapper && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read45_XElementArrayWrapper(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":XElementArrayWrapper");
            }
            return (object)o;
        }

        public object Read158_TypeWithDateTimeStringProperty() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id49_TypeWithDateTimeStringProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read46_TypeWithDateTimeStringProperty(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithDateTimeStringProperty");
            }
            return (object)o;
        }

        public object Read159_SimpleType() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id50_SimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read47_SimpleType(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":SimpleType");
            }
            return (object)o;
        }

        public object Read160_TypeWithGetSetArrayMembers() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id51_TypeWithGetSetArrayMembers && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read48_TypeWithGetSetArrayMembers(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithGetSetArrayMembers");
            }
            return (object)o;
        }

        public object Read161_TypeWithGetOnlyArrayProperties() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id52_TypeWithGetOnlyArrayProperties && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read49_TypeWithGetOnlyArrayProperties(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithGetOnlyArrayProperties");
            }
            return (object)o;
        }

        public object Read162_StructNotSerializable() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id53_StructNotSerializable && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read50_StructNotSerializable(true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":StructNotSerializable");
            }
            return (object)o;
        }

        public object Read163_TypeWithMyCollectionField() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id54_TypeWithMyCollectionField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read51_TypeWithMyCollectionField(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithMyCollectionField");
            }
            return (object)o;
        }

        public object Read164_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id55_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read52_Item(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithReadOnlyMyCollectionProperty");
            }
            return (object)o;
        }

        public object Read165_ArrayOfAnyType() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id56_ArrayOfAnyType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        if (!ReadNull()) {
                            if ((object)(o) == null) o = new global::SerializationTypes.MyList();
                            global::SerializationTypes.MyList a_0_0 = (global::SerializationTypes.MyList)o;
                            if ((Reader.IsEmptyElement)) {
                                Reader.Skip();
                            }
                            else {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                        do {
                                            if (((object) Reader.LocalName == (object)id57_anyType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                if ((object)(a_0_0) == null) Reader.Skip(); else a_0_0.Add(Read1_Object(true, true));
                                                break;
                                            }
                                            UnknownNode(null, @":anyType");
                                        } while (false);
                                    }
                                    else {
                                        UnknownNode(null, @":anyType");
                                    }
                                    Reader.MoveToContent();
                                }
                            ReadEndElement();
                            }
                        }
                        else {
                            if ((object)(o) == null) o = new global::SerializationTypes.MyList();
                            global::SerializationTypes.MyList a_0_0 = (global::SerializationTypes.MyList)o;
                        }
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":ArrayOfAnyType");
            }
            return (object)o;
        }

        public object Read166_MyEnum() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id58_MyEnum && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read53_MyEnum(Reader.ReadElementString());
                        }
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":MyEnum");
            }
            return (object)o;
        }

        public object Read167_TypeWithEnumMembers() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id59_TypeWithEnumMembers && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read54_TypeWithEnumMembers(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithEnumMembers");
            }
            return (object)o;
        }

        public object Read168_DCStruct() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id60_DCStruct && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read55_DCStruct(true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":DCStruct");
            }
            return (object)o;
        }

        public object Read169_DCClassWithEnumAndStruct() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id61_DCClassWithEnumAndStruct && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read56_DCClassWithEnumAndStruct(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":DCClassWithEnumAndStruct");
            }
            return (object)o;
        }

        public object Read170_BuiltInTypes() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id62_BuiltInTypes && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read57_BuiltInTypes(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":BuiltInTypes");
            }
            return (object)o;
        }

        public object Read171_TypeA() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id63_TypeA && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read58_TypeA(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeA");
            }
            return (object)o;
        }

        public object Read172_TypeB() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id64_TypeB && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read59_TypeB(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeB");
            }
            return (object)o;
        }

        public object Read173_TypeHasArrayOfASerializedAsB() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id65_TypeHasArrayOfASerializedAsB && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read60_TypeHasArrayOfASerializedAsB(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeHasArrayOfASerializedAsB");
            }
            return (object)o;
        }

        public object Read174_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id66_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read61_Item(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":__TypeNameWithSpecialCharacters漢ñ");
            }
            return (object)o;
        }

        public object Read175_BaseClassWithSamePropertyName() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id67_BaseClassWithSamePropertyName && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read62_BaseClassWithSamePropertyName(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":BaseClassWithSamePropertyName");
            }
            return (object)o;
        }

        public object Read176_DerivedClassWithSameProperty() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id68_DerivedClassWithSameProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read63_DerivedClassWithSameProperty(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":DerivedClassWithSameProperty");
            }
            return (object)o;
        }

        public object Read177_DerivedClassWithSameProperty2() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id69_DerivedClassWithSameProperty2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read64_DerivedClassWithSameProperty2(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":DerivedClassWithSameProperty2");
            }
            return (object)o;
        }

        public object Read178_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id70_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read65_Item(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithDateTimePropertyAsXmlTime");
            }
            return (object)o;
        }

        public object Read179_TypeWithByteArrayAsXmlText() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id71_TypeWithByteArrayAsXmlText && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read66_TypeWithByteArrayAsXmlText(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithByteArrayAsXmlText");
            }
            return (object)o;
        }

        public object Read180_SimpleDC() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id72_SimpleDC && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read67_SimpleDC(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":SimpleDC");
            }
            return (object)o;
        }

        public object Read181_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id73_Item && (object) Reader.NamespaceURI == (object)id74_Item)) {
                        o = Read68_Item(false, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @"http://schemas.xmlsoap.org/ws/2005/04/discovery:TypeWithXmlTextAttributeOnArray");
            }
            return (object)o;
        }

        public object Read182_EnumFlags() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id75_EnumFlags && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read69_EnumFlags(Reader.ReadElementString());
                        }
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":EnumFlags");
            }
            return (object)o;
        }

        public object Read183_ClassImplementsInterface() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id76_ClassImplementsInterface && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read70_ClassImplementsInterface(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":ClassImplementsInterface");
            }
            return (object)o;
        }

        public object Read184_WithStruct() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id77_WithStruct && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read72_WithStruct(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":WithStruct");
            }
            return (object)o;
        }

        public object Read185_SomeStruct() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id78_SomeStruct && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read71_SomeStruct(true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":SomeStruct");
            }
            return (object)o;
        }

        public object Read186_WithEnums() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id79_WithEnums && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read75_WithEnums(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":WithEnums");
            }
            return (object)o;
        }

        public object Read187_WithNullables() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id80_WithNullables && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read79_WithNullables(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":WithNullables");
            }
            return (object)o;
        }

        public object Read188_ByteEnum() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id81_ByteEnum && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read80_ByteEnum(Reader.ReadElementString());
                        }
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":ByteEnum");
            }
            return (object)o;
        }

        public object Read189_SByteEnum() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id82_SByteEnum && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read81_SByteEnum(Reader.ReadElementString());
                        }
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":SByteEnum");
            }
            return (object)o;
        }

        public object Read190_ShortEnum() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id83_ShortEnum && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read74_ShortEnum(Reader.ReadElementString());
                        }
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":ShortEnum");
            }
            return (object)o;
        }

        public object Read191_IntEnum() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id84_IntEnum && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read73_IntEnum(Reader.ReadElementString());
                        }
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":IntEnum");
            }
            return (object)o;
        }

        public object Read192_UIntEnum() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id85_UIntEnum && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read82_UIntEnum(Reader.ReadElementString());
                        }
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":UIntEnum");
            }
            return (object)o;
        }

        public object Read193_LongEnum() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id86_LongEnum && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read83_LongEnum(Reader.ReadElementString());
                        }
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":LongEnum");
            }
            return (object)o;
        }

        public object Read194_ULongEnum() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id87_ULongEnum && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read84_ULongEnum(Reader.ReadElementString());
                        }
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":ULongEnum");
            }
            return (object)o;
        }

        public object Read195_AttributeTesting() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id88_AttributeTesting && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read86_XmlSerializerAttributes(false, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":AttributeTesting");
            }
            return (object)o;
        }

        public object Read196_ItemChoiceType() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id89_ItemChoiceType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read85_ItemChoiceType(Reader.ReadElementString());
                        }
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":ItemChoiceType");
            }
            return (object)o;
        }

        public object Read197_TypeWithAnyAttribute() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id90_TypeWithAnyAttribute && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read87_TypeWithAnyAttribute(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithAnyAttribute");
            }
            return (object)o;
        }

        public object Read198_KnownTypesThroughConstructor() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id91_KnownTypesThroughConstructor && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read88_KnownTypesThroughConstructor(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":KnownTypesThroughConstructor");
            }
            return (object)o;
        }

        public object Read199_SimpleKnownTypeValue() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id92_SimpleKnownTypeValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read89_SimpleKnownTypeValue(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":SimpleKnownTypeValue");
            }
            return (object)o;
        }

        public object Read200_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id93_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = (global::SerializationTypes.ClassImplementingIXmlSerializable)ReadSerializable(( System.Xml.Serialization.IXmlSerializable)new global::SerializationTypes.ClassImplementingIXmlSerializable());
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":ClassImplementingIXmlSerializable");
            }
            return (object)o;
        }

        public object Read201_TypeWithPropertyNameSpecified() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id94_TypeWithPropertyNameSpecified && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read90_TypeWithPropertyNameSpecified(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithPropertyNameSpecified");
            }
            return (object)o;
        }

        public object Read202_TypeWithXmlSchemaFormAttribute() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id95_TypeWithXmlSchemaFormAttribute && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read91_TypeWithXmlSchemaFormAttribute(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithXmlSchemaFormAttribute");
            }
            return (object)o;
        }

        public object Read203_MyXmlType() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id96_MyXmlType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read92_Item(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":MyXmlType");
            }
            return (object)o;
        }

        public object Read204_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id97_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read93_Item(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithSchemaFormInXmlAttribute");
            }
            return (object)o;
        }

        public object Read205_CustomDocument() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id98_CustomDocument && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read95_CustomDocument(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":CustomDocument");
            }
            return (object)o;
        }

        public object Read206_CustomElement() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id99_CustomElement && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read94_CustomElement(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":CustomElement");
            }
            return (object)o;
        }

        public object Read207_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                o = (global::SerializationTypes.CustomAttribute)ReadXmlNode(false);
            }
            else {
                UnknownNode(null, @"");
            }
            return (object)o;
        }

        public object Read208_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id100_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read96_Item(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithNonPublicDefaultConstructor");
            }
            return (object)o;
        }

        public object Read209_ServerSettings() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id101_ServerSettings && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read97_ServerSettings(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":ServerSettings");
            }
            return (object)o;
        }

        public object Read210_TypeWithXmlQualifiedName() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id102_TypeWithXmlQualifiedName && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read98_TypeWithXmlQualifiedName(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithXmlQualifiedName");
            }
            return (object)o;
        }

        public object Read211_TypeWith2DArrayProperty2() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id103_TypeWith2DArrayProperty2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read99_TypeWith2DArrayProperty2(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWith2DArrayProperty2");
            }
            return (object)o;
        }

        public object Read212_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id104_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read100_Item(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithPropertiesHavingDefaultValue");
            }
            return (object)o;
        }

        public object Read213_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id105_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read101_Item(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithEnumPropertyHavingDefaultValue");
            }
            return (object)o;
        }

        public object Read214_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id106_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read102_Item(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithEnumFlagPropertyHavingDefaultValue");
            }
            return (object)o;
        }

        public object Read215_TypeWithShouldSerializeMethod() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id107_TypeWithShouldSerializeMethod && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read103_TypeWithShouldSerializeMethod(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithShouldSerializeMethod");
            }
            return (object)o;
        }

        public object Read216_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id108_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read104_Item(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":KnownTypesThroughConstructorWithArrayProperties");
            }
            return (object)o;
        }

        public object Read217_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id109_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read105_Item(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":KnownTypesThroughConstructorWithValue");
            }
            return (object)o;
        }

        public object Read218_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id110_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read106_Item(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithTypesHavingCustomFormatter");
            }
            return (object)o;
        }

        public object Read219_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id111_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read108_Item(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithArrayPropertyHavingChoice");
            }
            return (object)o;
        }

        public object Read220_MoreChoices() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id112_MoreChoices && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read107_MoreChoices(Reader.ReadElementString());
                        }
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":MoreChoices");
            }
            return (object)o;
        }

        public object Read221_TypeWithFieldsOrdered() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id113_TypeWithFieldsOrdered && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read109_TypeWithFieldsOrdered(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithFieldsOrdered");
            }
            return (object)o;
        }

        public object Read222_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id114_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read110_Item(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithKnownTypesOfCollectionsWithConflictingXmlName");
            }
            return (object)o;
        }

        public object Read223_Root() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id115_Root && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read113_Item(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":Root");
            }
            return (object)o;
        }

        public object Read224_TypeClashB() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id116_TypeClashB && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read112_TypeNameClash(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeClashB");
            }
            return (object)o;
        }

        public object Read225_TypeClashA() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id117_TypeClashA && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read111_TypeNameClash(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeClashA");
            }
            return (object)o;
        }

        public object Read226_Person() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id118_Person && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read114_Person(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":Person");
            }
            return (object)o;
        }

        global::Outer.Person Read114_Person(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id118_Person && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Outer.Person o;
            o = new global::Outer.Person();
            System.Span<bool> paramsRead = stackalloc bool[3];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id119_FirstName && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@FirstName = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id120_MiddleName && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@MiddleName = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id121_LastName && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@LastName = Reader.ReadElementString();
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        UnknownNode((object)o, @":FirstName, :MiddleName, :LastName");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":FirstName, :MiddleName, :LastName");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeNameClashA.TypeNameClash Read111_TypeNameClash(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id117_TypeClashA && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeNameClashA.TypeNameClash o;
            o = new global::SerializationTypes.TypeNameClashA.TypeNameClash();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id122_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Name = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Name");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Name");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeNameClashB.TypeNameClash Read112_TypeNameClash(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id116_TypeClashB && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeNameClashB.TypeNameClash o;
            o = new global::SerializationTypes.TypeNameClashB.TypeNameClash();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id122_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Name = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Name");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Name");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.NamespaceTypeNameClashContainer Read113_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id123_ContainerType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.NamespaceTypeNameClashContainer o;
            o = new global::SerializationTypes.NamespaceTypeNameClashContainer();
            global::SerializationTypes.TypeNameClashA.TypeNameClash[] a_0 = null;
            int ca_0 = 0;
            global::SerializationTypes.TypeNameClashB.TypeNameClash[] a_1 = null;
            int ca_1 = 0;
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                o.@A = (global::SerializationTypes.TypeNameClashA.TypeNameClash[])ShrinkArray(a_0, ca_0, typeof(global::SerializationTypes.TypeNameClashA.TypeNameClash), true);
                o.@B = (global::SerializationTypes.TypeNameClashB.TypeNameClash[])ShrinkArray(a_1, ca_1, typeof(global::SerializationTypes.TypeNameClashB.TypeNameClash), true);
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (((object) Reader.LocalName == (object)id124_A && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            a_0 = (global::SerializationTypes.TypeNameClashA.TypeNameClash[])EnsureArrayIndex(a_0, ca_0, typeof(global::SerializationTypes.TypeNameClashA.TypeNameClash));a_0[ca_0++] = Read111_TypeNameClash(false, true);
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id125_B && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            a_1 = (global::SerializationTypes.TypeNameClashB.TypeNameClash[])EnsureArrayIndex(a_1, ca_1, typeof(global::SerializationTypes.TypeNameClashB.TypeNameClash));a_1[ca_1++] = Read112_TypeNameClash(false, true);
                            break;
                        }
                        UnknownNode((object)o, @":A, :B");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":A, :B");
                }
                Reader.MoveToContent();
            }
            o.@A = (global::SerializationTypes.TypeNameClashA.TypeNameClash[])ShrinkArray(a_0, ca_0, typeof(global::SerializationTypes.TypeNameClashA.TypeNameClash), true);
            o.@B = (global::SerializationTypes.TypeNameClashB.TypeNameClash[])ShrinkArray(a_1, ca_1, typeof(global::SerializationTypes.TypeNameClashB.TypeNameClash), true);
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName Read110_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id114_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName o;
            o = new global::SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName();
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id126_Value1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Value1 = Read1_Object(false, true);
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id127_Value2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Value2 = Read1_Object(false, true);
                            paramsRead[1] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Value1, :Value2");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Value1, :Value2");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::System.Object Read1_Object(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
                if (isNull) {
                    if (xsiType != null) return (global::System.Object)ReadTypedNull(xsiType);
                    else return null;
                }
                if (xsiType == null) {
                    return ReadTypedPrimitive(new System.Xml.XmlQualifiedName("anyType", "http://www.w3.org/2001/XMLSchema"));
                }
                else {
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id118_Person && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read114_Person(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id123_ContainerType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read113_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id116_TypeClashB && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read112_TypeNameClash(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id117_TypeClashA && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read111_TypeNameClash(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id114_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read110_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id113_TypeWithFieldsOrdered && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read109_TypeWithFieldsOrdered(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id111_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read108_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id110_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read106_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id109_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read105_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id108_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read104_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id107_TypeWithShouldSerializeMethod && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read103_TypeWithShouldSerializeMethod(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id106_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read102_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id105_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read101_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id104_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read100_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id103_TypeWith2DArrayProperty2 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read99_TypeWith2DArrayProperty2(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id102_TypeWithXmlQualifiedName && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read98_TypeWithXmlQualifiedName(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id101_ServerSettings && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read97_ServerSettings(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id100_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read96_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id98_CustomDocument && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read95_CustomDocument(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id99_CustomElement && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read94_CustomElement(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id96_MyXmlType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read92_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id95_TypeWithXmlSchemaFormAttribute && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read91_TypeWithXmlSchemaFormAttribute(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id94_TypeWithPropertyNameSpecified && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read90_TypeWithPropertyNameSpecified(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id92_SimpleKnownTypeValue && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read89_SimpleKnownTypeValue(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id91_KnownTypesThroughConstructor && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read88_KnownTypesThroughConstructor(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id90_TypeWithAnyAttribute && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read87_TypeWithAnyAttribute(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id128_XmlSerializerAttributes && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read86_XmlSerializerAttributes(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id80_WithNullables && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read79_WithNullables(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id79_WithEnums && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read75_WithEnums(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id77_WithStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read72_WithStruct(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id78_SomeStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read71_SomeStruct(false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id76_ClassImplementsInterface && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read70_ClassImplementsInterface(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id73_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id74_Item))
                        return Read68_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id72_SimpleDC && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read67_SimpleDC(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id71_TypeWithByteArrayAsXmlText && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read66_TypeWithByteArrayAsXmlText(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id70_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read65_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id67_BaseClassWithSamePropertyName && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read62_BaseClassWithSamePropertyName(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id68_DerivedClassWithSameProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read63_DerivedClassWithSameProperty(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id69_DerivedClassWithSameProperty2 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read64_DerivedClassWithSameProperty2(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id66_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read61_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id65_TypeHasArrayOfASerializedAsB && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read60_TypeHasArrayOfASerializedAsB(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id64_TypeB && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read59_TypeB(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id63_TypeA && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read58_TypeA(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id62_BuiltInTypes && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read57_BuiltInTypes(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id61_DCClassWithEnumAndStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read56_DCClassWithEnumAndStruct(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id60_DCStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read55_DCStruct(false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id59_TypeWithEnumMembers && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read54_TypeWithEnumMembers(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id55_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read52_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id54_TypeWithMyCollectionField && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read51_TypeWithMyCollectionField(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id53_StructNotSerializable && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read50_StructNotSerializable(false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id52_TypeWithGetOnlyArrayProperties && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read49_TypeWithGetOnlyArrayProperties(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id51_TypeWithGetSetArrayMembers && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read48_TypeWithGetSetArrayMembers(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id50_SimpleType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read47_SimpleType(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id49_TypeWithDateTimeStringProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read46_TypeWithDateTimeStringProperty(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id48_XElementArrayWrapper && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read45_XElementArrayWrapper(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id47_XElementStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read44_XElementStruct(false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id46_XElementWrapper && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read43_XElementWrapper(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id44_RootClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read42_RootClass(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id45_Parameter && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read41_Parameter(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id129_ParameterOfString && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read40_ParameterOfString(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id130_MsgDocumentType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id43_httpexamplecom))
                        return Read39_MsgDocumentType(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id41_TypeWithLinkedProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read38_TypeWithLinkedProperty(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id131_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read37_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id39_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read36_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id38_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read35_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id37_DefaultValuesSetToNaN && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read34_DefaultValuesSetToNaN(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id36_Pet && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read33_Pet(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id32_Orchestra && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read30_Orchestra(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id33_Instrument && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read29_Instrument(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id34_Brass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read31_Brass(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id35_Trumpet && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read32_Trumpet(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id28_BaseClass1 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read27_BaseClass1(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id29_DerivedClass1 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read28_DerivedClass1(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id27_AliasedTestType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read26_AliasedTestType(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id26_OrderedItem && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read25_OrderedItem(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id25_Address && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read24_Address(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id23_PurchaseOrder && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id24_httpwwwcontoso1com))
                        return Read23_PurchaseOrder(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id26_OrderedItem && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id24_httpwwwcontoso1com))
                        return Read22_OrderedItem(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id25_Address && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id24_httpwwwcontoso1com))
                        return Read21_Address(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id18_SimpleBaseClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read20_SimpleBaseClass(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id19_SimpleDerivedClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read19_SimpleDerivedClass(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id16_BaseClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read18_BaseClass(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id17_DerivedClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read17_DerivedClass(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id15_Employee && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read16_Employee(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id13_Group && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read15_Group(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id14_Vehicle && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read14_Vehicle(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id10_Animal && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read11_Animal(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id11_Dog && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read13_Dog(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id9_TypeWithXmlNodeArrayProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read10_TypeWithXmlNodeArrayProperty(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id8_TypeWithByteProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read9_TypeWithByteProperty(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id7_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read8_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id6_TypeWithTimeSpanProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read7_TypeWithTimeSpanProperty(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id5_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read6_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id4_TypeWithBinaryProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read4_TypeWithBinaryProperty(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id3_TypeWithXmlDocumentProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read3_TypeWithXmlDocumentProperty(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id1_TypeWithXmlElementProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read2_TypeWithXmlElementProperty(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id12_DogBreed && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read12_DogBreed(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id132_ArrayOfOrderedItem && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id24_httpwwwcontoso1com)) {
                        global::OrderedItem[] a = null;
                        if (!ReadNull()) {
                            global::OrderedItem[] z_0_0 = null;
                            int cz_0_0 = 0;
                            if ((Reader.IsEmptyElement)) {
                                Reader.Skip();
                            }
                            else {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                        do {
                                            if (((object) Reader.LocalName == (object)id26_OrderedItem && (object) Reader.NamespaceURI == (object)id24_httpwwwcontoso1com)) {
                                                z_0_0 = (global::OrderedItem[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::OrderedItem));z_0_0[cz_0_0++] = Read22_OrderedItem(true, true);
                                                break;
                                            }
                                            UnknownNode(null, @"http://www.contoso1.com:OrderedItem");
                                        } while (false);
                                    }
                                    else {
                                        UnknownNode(null, @"http://www.contoso1.com:OrderedItem");
                                    }
                                    Reader.MoveToContent();
                                }
                            ReadEndElement();
                            }
                            a = (global::OrderedItem[])ShrinkArray(z_0_0, cz_0_0, typeof(global::OrderedItem), false);
                        }
                        return a;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id133_ArrayOfInt && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        global::System.Collections.Generic.List<global::System.Int32> a = null;
                        if (!ReadNull()) {
                            if ((object)(a) == null) a = new global::System.Collections.Generic.List<global::System.Int32>();
                            global::System.Collections.Generic.List<global::System.Int32> z_0_0 = (global::System.Collections.Generic.List<global::System.Int32>)a;
                            if ((Reader.IsEmptyElement)) {
                                Reader.Skip();
                            }
                            else {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                        do {
                                            if (((object) Reader.LocalName == (object)id134_int && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                {
                                                    z_0_0.Add(System.Xml.XmlConvert.ToInt32(Reader.ReadElementString()));
                                                }
                                                break;
                                            }
                                            UnknownNode(null, @":int");
                                        } while (false);
                                    }
                                    else {
                                        UnknownNode(null, @":int");
                                    }
                                    Reader.MoveToContent();
                                }
                            ReadEndElement();
                            }
                        }
                        return a;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id135_ArrayOfString && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        global::System.Collections.Generic.List<global::System.String> a = null;
                        if (!ReadNull()) {
                            if ((object)(a) == null) a = new global::System.Collections.Generic.List<global::System.String>();
                            global::System.Collections.Generic.List<global::System.String> z_0_0 = (global::System.Collections.Generic.List<global::System.String>)a;
                            if ((Reader.IsEmptyElement)) {
                                Reader.Skip();
                            }
                            else {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                        do {
                                            if (((object) Reader.LocalName == (object)id136_string && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                if (ReadNull()) {
                                                    z_0_0.Add(null);
                                                }
                                                else {
                                                    z_0_0.Add(Reader.ReadElementString());
                                                }
                                                break;
                                            }
                                            UnknownNode(null, @":string");
                                        } while (false);
                                    }
                                    else {
                                        UnknownNode(null, @":string");
                                    }
                                    Reader.MoveToContent();
                                }
                            ReadEndElement();
                            }
                        }
                        return a;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id137_ArrayOfDouble && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        global::System.Collections.Generic.List<global::System.Double> a = null;
                        if (!ReadNull()) {
                            if ((object)(a) == null) a = new global::System.Collections.Generic.List<global::System.Double>();
                            global::System.Collections.Generic.List<global::System.Double> z_0_0 = (global::System.Collections.Generic.List<global::System.Double>)a;
                            if ((Reader.IsEmptyElement)) {
                                Reader.Skip();
                            }
                            else {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                        do {
                                            if (((object) Reader.LocalName == (object)id138_double && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                {
                                                    z_0_0.Add(System.Xml.XmlConvert.ToDouble(Reader.ReadElementString()));
                                                }
                                                break;
                                            }
                                            UnknownNode(null, @":double");
                                        } while (false);
                                    }
                                    else {
                                        UnknownNode(null, @":double");
                                    }
                                    Reader.MoveToContent();
                                }
                            ReadEndElement();
                            }
                        }
                        return a;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id30_ArrayOfDateTime && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        global::MyCollection1 a = null;
                        if (!ReadNull()) {
                            if ((object)(a) == null) a = new global::MyCollection1();
                            global::MyCollection1 z_0_0 = (global::MyCollection1)a;
                            if ((Reader.IsEmptyElement)) {
                                Reader.Skip();
                            }
                            else {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                        do {
                                            if (((object) Reader.LocalName == (object)id31_dateTime && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                {
                                                    z_0_0.Add(ToDateTime(Reader.ReadElementString()));
                                                }
                                                break;
                                            }
                                            UnknownNode(null, @":dateTime");
                                        } while (false);
                                    }
                                    else {
                                        UnknownNode(null, @":dateTime");
                                    }
                                    Reader.MoveToContent();
                                }
                            ReadEndElement();
                            }
                        }
                        return a;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id139_ArrayOfInstrument && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        global::Instrument[] a = null;
                        if (!ReadNull()) {
                            global::Instrument[] z_0_0 = null;
                            int cz_0_0 = 0;
                            if ((Reader.IsEmptyElement)) {
                                Reader.Skip();
                            }
                            else {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                        do {
                                            if (((object) Reader.LocalName == (object)id33_Instrument && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                z_0_0 = (global::Instrument[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::Instrument));z_0_0[cz_0_0++] = Read29_Instrument(true, true);
                                                break;
                                            }
                                            UnknownNode(null, @":Instrument");
                                        } while (false);
                                    }
                                    else {
                                        UnknownNode(null, @":Instrument");
                                    }
                                    Reader.MoveToContent();
                                }
                            ReadEndElement();
                            }
                            a = (global::Instrument[])ShrinkArray(z_0_0, cz_0_0, typeof(global::Instrument), false);
                        }
                        return a;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id140_ArrayOfTypeWithLinkedProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        global::System.Collections.Generic.List<global::TypeWithLinkedProperty> a = null;
                        if (!ReadNull()) {
                            if ((object)(a) == null) a = new global::System.Collections.Generic.List<global::TypeWithLinkedProperty>();
                            global::System.Collections.Generic.List<global::TypeWithLinkedProperty> z_0_0 = (global::System.Collections.Generic.List<global::TypeWithLinkedProperty>)a;
                            if ((Reader.IsEmptyElement)) {
                                Reader.Skip();
                            }
                            else {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                        do {
                                            if (((object) Reader.LocalName == (object)id41_TypeWithLinkedProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                if ((object)(z_0_0) == null) Reader.Skip(); else z_0_0.Add(Read38_TypeWithLinkedProperty(true, true));
                                                break;
                                            }
                                            UnknownNode(null, @":TypeWithLinkedProperty");
                                        } while (false);
                                    }
                                    else {
                                        UnknownNode(null, @":TypeWithLinkedProperty");
                                    }
                                    Reader.MoveToContent();
                                }
                            ReadEndElement();
                            }
                        }
                        return a;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id141_ArrayOfParameter && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        global::System.Collections.Generic.List<global::Parameter> a = null;
                        if (!ReadNull()) {
                            if ((object)(a) == null) a = new global::System.Collections.Generic.List<global::Parameter>();
                            global::System.Collections.Generic.List<global::Parameter> z_0_0 = (global::System.Collections.Generic.List<global::Parameter>)a;
                            if ((Reader.IsEmptyElement)) {
                                Reader.Skip();
                            }
                            else {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                        do {
                                            if (((object) Reader.LocalName == (object)id45_Parameter && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                if ((object)(z_0_0) == null) Reader.Skip(); else z_0_0.Add(Read41_Parameter(true, true));
                                                break;
                                            }
                                            UnknownNode(null, @":Parameter");
                                        } while (false);
                                    }
                                    else {
                                        UnknownNode(null, @":Parameter");
                                    }
                                    Reader.MoveToContent();
                                }
                            ReadEndElement();
                            }
                        }
                        return a;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id142_ArrayOfXElement && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        global::System.Xml.Linq.XElement[] a = null;
                        if (!ReadNull()) {
                            global::System.Xml.Linq.XElement[] z_0_0 = null;
                            int cz_0_0 = 0;
                            if ((Reader.IsEmptyElement)) {
                                Reader.Skip();
                            }
                            else {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                        do {
                                            if (((object) Reader.LocalName == (object)id143_XElement && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                z_0_0 = (global::System.Xml.Linq.XElement[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::System.Xml.Linq.XElement));z_0_0[cz_0_0++] = (global::System.Xml.Linq.XElement)ReadSerializable(( System.Xml.Serialization.IXmlSerializable)new global::System.Xml.Linq.XElement("default"), true
                                                );
                                                break;
                                            }
                                            UnknownNode(null, @":XElement");
                                        } while (false);
                                    }
                                    else {
                                        UnknownNode(null, @":XElement");
                                    }
                                    Reader.MoveToContent();
                                }
                            ReadEndElement();
                            }
                            a = (global::System.Xml.Linq.XElement[])ShrinkArray(z_0_0, cz_0_0, typeof(global::System.Xml.Linq.XElement), false);
                        }
                        return a;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id144_ArrayOfSimpleType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        global::SerializationTypes.SimpleType[] a = null;
                        if (!ReadNull()) {
                            global::SerializationTypes.SimpleType[] z_0_0 = null;
                            int cz_0_0 = 0;
                            if ((Reader.IsEmptyElement)) {
                                Reader.Skip();
                            }
                            else {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                        do {
                                            if (((object) Reader.LocalName == (object)id50_SimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                z_0_0 = (global::SerializationTypes.SimpleType[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::SerializationTypes.SimpleType));z_0_0[cz_0_0++] = Read47_SimpleType(true, true);
                                                break;
                                            }
                                            UnknownNode(null, @":SimpleType");
                                        } while (false);
                                    }
                                    else {
                                        UnknownNode(null, @":SimpleType");
                                    }
                                    Reader.MoveToContent();
                                }
                            ReadEndElement();
                            }
                            a = (global::SerializationTypes.SimpleType[])ShrinkArray(z_0_0, cz_0_0, typeof(global::SerializationTypes.SimpleType), false);
                        }
                        return a;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id56_ArrayOfAnyType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        global::SerializationTypes.MyList a = null;
                        if (!ReadNull()) {
                            if ((object)(a) == null) a = new global::SerializationTypes.MyList();
                            global::SerializationTypes.MyList z_0_0 = (global::SerializationTypes.MyList)a;
                            if ((Reader.IsEmptyElement)) {
                                Reader.Skip();
                            }
                            else {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                        do {
                                            if (((object) Reader.LocalName == (object)id57_anyType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                if ((object)(z_0_0) == null) Reader.Skip(); else z_0_0.Add(Read1_Object(true, true));
                                                break;
                                            }
                                            UnknownNode(null, @":anyType");
                                        } while (false);
                                    }
                                    else {
                                        UnknownNode(null, @":anyType");
                                    }
                                    Reader.MoveToContent();
                                }
                            ReadEndElement();
                            }
                        }
                        return a;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id58_MyEnum && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read53_MyEnum(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id145_ArrayOfTypeA && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        global::SerializationTypes.TypeA[] a = null;
                        if (!ReadNull()) {
                            global::SerializationTypes.TypeA[] z_0_0 = null;
                            int cz_0_0 = 0;
                            if ((Reader.IsEmptyElement)) {
                                Reader.Skip();
                            }
                            else {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                        do {
                                            if (((object) Reader.LocalName == (object)id63_TypeA && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                z_0_0 = (global::SerializationTypes.TypeA[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::SerializationTypes.TypeA));z_0_0[cz_0_0++] = Read58_TypeA(true, true);
                                                break;
                                            }
                                            UnknownNode(null, @":TypeA");
                                        } while (false);
                                    }
                                    else {
                                        UnknownNode(null, @":TypeA");
                                    }
                                    Reader.MoveToContent();
                                }
                            ReadEndElement();
                            }
                            a = (global::SerializationTypes.TypeA[])ShrinkArray(z_0_0, cz_0_0, typeof(global::SerializationTypes.TypeA), false);
                        }
                        return a;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id75_EnumFlags && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read69_EnumFlags(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id84_IntEnum && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read73_IntEnum(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id83_ShortEnum && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read74_ShortEnum(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id81_ByteEnum && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read80_ByteEnum(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id82_SByteEnum && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read81_SByteEnum(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id85_UIntEnum && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read82_UIntEnum(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id86_LongEnum && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read83_LongEnum(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id87_ULongEnum && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read84_ULongEnum(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id89_ItemChoiceType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read85_ItemChoiceType(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id146_ArrayOfItemChoiceType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        global::SerializationTypes.ItemChoiceType[] a = null;
                        if (!ReadNull()) {
                            global::SerializationTypes.ItemChoiceType[] z_0_0 = null;
                            int cz_0_0 = 0;
                            if ((Reader.IsEmptyElement)) {
                                Reader.Skip();
                            }
                            else {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                        do {
                                            if (((object) Reader.LocalName == (object)id89_ItemChoiceType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                {
                                                    z_0_0 = (global::SerializationTypes.ItemChoiceType[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::SerializationTypes.ItemChoiceType));z_0_0[cz_0_0++] = Read85_ItemChoiceType(Reader.ReadElementString());
                                                }
                                                break;
                                            }
                                            UnknownNode(null, @":ItemChoiceType");
                                        } while (false);
                                    }
                                    else {
                                        UnknownNode(null, @":ItemChoiceType");
                                    }
                                    Reader.MoveToContent();
                                }
                            ReadEndElement();
                            }
                            a = (global::SerializationTypes.ItemChoiceType[])ShrinkArray(z_0_0, cz_0_0, typeof(global::SerializationTypes.ItemChoiceType), false);
                        }
                        return a;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id135_ArrayOfString && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id147_httpmynamespace)) {
                        global::System.Object[] a = null;
                        if (!ReadNull()) {
                            global::System.Object[] z_0_0 = null;
                            int cz_0_0 = 0;
                            if ((Reader.IsEmptyElement)) {
                                Reader.Skip();
                            }
                            else {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                        do {
                                            if (((object) Reader.LocalName == (object)id136_string && (object) Reader.NamespaceURI == (object)id147_httpmynamespace)) {
                                                if (ReadNull()) {
                                                    z_0_0 = (global::System.Object[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::System.Object));z_0_0[cz_0_0++] = null;
                                                }
                                                else {
                                                    z_0_0 = (global::System.Object[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::System.Object));z_0_0[cz_0_0++] = Reader.ReadElementString();
                                                }
                                                break;
                                            }
                                            UnknownNode(null, @"http://mynamespace:string");
                                        } while (false);
                                    }
                                    else {
                                        UnknownNode(null, @"http://mynamespace:string");
                                    }
                                    Reader.MoveToContent();
                                }
                            ReadEndElement();
                            }
                            a = (global::System.Object[])ShrinkArray(z_0_0, cz_0_0, typeof(global::System.Object), false);
                        }
                        return a;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id148_ArrayOfString1 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        global::System.Collections.Generic.List<global::System.String> a = null;
                        if (!ReadNull()) {
                            if ((object)(a) == null) a = new global::System.Collections.Generic.List<global::System.String>();
                            global::System.Collections.Generic.List<global::System.String> z_0_0 = (global::System.Collections.Generic.List<global::System.String>)a;
                            if ((Reader.IsEmptyElement)) {
                                Reader.Skip();
                            }
                            else {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                        do {
                                            if (((object) Reader.LocalName == (object)id149_NoneParameter && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                {
                                                    z_0_0.Add(Reader.ReadElementString());
                                                }
                                                break;
                                            }
                                            UnknownNode(null, @":NoneParameter");
                                        } while (false);
                                    }
                                    else {
                                        UnknownNode(null, @":NoneParameter");
                                    }
                                    Reader.MoveToContent();
                                }
                            ReadEndElement();
                            }
                        }
                        return a;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id150_ArrayOfBoolean && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        global::System.Collections.Generic.List<global::System.Boolean> a = null;
                        if (!ReadNull()) {
                            if ((object)(a) == null) a = new global::System.Collections.Generic.List<global::System.Boolean>();
                            global::System.Collections.Generic.List<global::System.Boolean> z_0_0 = (global::System.Collections.Generic.List<global::System.Boolean>)a;
                            if ((Reader.IsEmptyElement)) {
                                Reader.Skip();
                            }
                            else {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                        do {
                                            if (((object) Reader.LocalName == (object)id151_QualifiedParameter && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                {
                                                    z_0_0.Add(System.Xml.XmlConvert.ToBoolean(Reader.ReadElementString()));
                                                }
                                                break;
                                            }
                                            UnknownNode(null, @":QualifiedParameter");
                                        } while (false);
                                    }
                                    else {
                                        UnknownNode(null, @":QualifiedParameter");
                                    }
                                    Reader.MoveToContent();
                                }
                            ReadEndElement();
                            }
                        }
                        return a;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id152_ArrayOfArrayOfSimpleType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        global::SerializationTypes.SimpleType[][] a = null;
                        if (!ReadNull()) {
                            global::SerializationTypes.SimpleType[][] z_0_0 = null;
                            int cz_0_0 = 0;
                            if ((Reader.IsEmptyElement)) {
                                Reader.Skip();
                            }
                            else {
                                Reader.ReadStartElement();
                                Reader.MoveToContent();
                                while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                    if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                        do {
                                            if (((object) Reader.LocalName == (object)id50_SimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                if (!ReadNull()) {
                                                    global::SerializationTypes.SimpleType[] z_0_0_0 = null;
                                                    int cz_0_0_0 = 0;
                                                    if ((Reader.IsEmptyElement)) {
                                                        Reader.Skip();
                                                    }
                                                    else {
                                                        Reader.ReadStartElement();
                                                        Reader.MoveToContent();
                                                        while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                                            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                                                do {
                                                                    if (((object) Reader.LocalName == (object)id50_SimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                                        z_0_0_0 = (global::SerializationTypes.SimpleType[])EnsureArrayIndex(z_0_0_0, cz_0_0_0, typeof(global::SerializationTypes.SimpleType));z_0_0_0[cz_0_0_0++] = Read47_SimpleType(true, true);
                                                                        break;
                                                                    }
                                                                    UnknownNode(null, @":SimpleType");
                                                                } while (false);
                                                            }
                                                            else {
                                                                UnknownNode(null, @":SimpleType");
                                                            }
                                                            Reader.MoveToContent();
                                                        }
                                                    ReadEndElement();
                                                    }
                                                    z_0_0 = (global::SerializationTypes.SimpleType[][])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::SerializationTypes.SimpleType[]));z_0_0[cz_0_0++] = (global::SerializationTypes.SimpleType[])ShrinkArray(z_0_0_0, cz_0_0_0, typeof(global::SerializationTypes.SimpleType), false);
                                                }
                                                break;
                                            }
                                            UnknownNode(null, @":SimpleType");
                                        } while (false);
                                    }
                                    else {
                                        UnknownNode(null, @":SimpleType");
                                    }
                                    Reader.MoveToContent();
                                }
                            ReadEndElement();
                            }
                            a = (global::SerializationTypes.SimpleType[][])ShrinkArray(z_0_0, cz_0_0, typeof(global::SerializationTypes.SimpleType[]), false);
                        }
                        return a;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id112_MoreChoices && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read107_MoreChoices(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    return ReadTypedPrimitive((System.Xml.XmlQualifiedName)xsiType);
                }
            }
            if (isNull) return null;
            global::System.Object o;
            o = new global::System.Object();
            System.Span<bool> paramsRead = stackalloc bool[0];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    UnknownNode((object)o, @"");
                }
                else {
                    UnknownNode((object)o, @"");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.MoreChoices Read107_MoreChoices(string s) {
            switch (s) {
                case @"None": return global::SerializationTypes.MoreChoices.@None;
                case @"Item": return global::SerializationTypes.MoreChoices.@Item;
                case @"Amount": return global::SerializationTypes.MoreChoices.@Amount;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.MoreChoices));
            }
        }

        global::SerializationTypes.SimpleType Read47_SimpleType(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id50_SimpleType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.SimpleType o;
            o = new global::SerializationTypes.SimpleType();
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id153_P1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@P1 = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id154_P2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@P2 = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        UnknownNode((object)o, @":P1, :P2");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":P1, :P2");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.ItemChoiceType Read85_ItemChoiceType(string s) {
            switch (s) {
                case @"None": return global::SerializationTypes.ItemChoiceType.@None;
                case @"Word": return global::SerializationTypes.ItemChoiceType.@Word;
                case @"Number": return global::SerializationTypes.ItemChoiceType.@Number;
                case @"DecimalNumber": return global::SerializationTypes.ItemChoiceType.@DecimalNumber;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.ItemChoiceType));
            }
        }

        global::SerializationTypes.ULongEnum Read84_ULongEnum(string s) {
            switch (s) {
                case @"Option0": return global::SerializationTypes.ULongEnum.@Option0;
                case @"Option1": return global::SerializationTypes.ULongEnum.@Option1;
                case @"Option2": return global::SerializationTypes.ULongEnum.@Option2;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.ULongEnum));
            }
        }

        global::SerializationTypes.LongEnum Read83_LongEnum(string s) {
            switch (s) {
                case @"Option0": return global::SerializationTypes.LongEnum.@Option0;
                case @"Option1": return global::SerializationTypes.LongEnum.@Option1;
                case @"Option2": return global::SerializationTypes.LongEnum.@Option2;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.LongEnum));
            }
        }

        global::SerializationTypes.UIntEnum Read82_UIntEnum(string s) {
            switch (s) {
                case @"Option0": return global::SerializationTypes.UIntEnum.@Option0;
                case @"Option1": return global::SerializationTypes.UIntEnum.@Option1;
                case @"Option2": return global::SerializationTypes.UIntEnum.@Option2;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.UIntEnum));
            }
        }

        global::SerializationTypes.SByteEnum Read81_SByteEnum(string s) {
            switch (s) {
                case @"Option0": return global::SerializationTypes.SByteEnum.@Option0;
                case @"Option1": return global::SerializationTypes.SByteEnum.@Option1;
                case @"Option2": return global::SerializationTypes.SByteEnum.@Option2;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.SByteEnum));
            }
        }

        global::SerializationTypes.ByteEnum Read80_ByteEnum(string s) {
            switch (s) {
                case @"Option0": return global::SerializationTypes.ByteEnum.@Option0;
                case @"Option1": return global::SerializationTypes.ByteEnum.@Option1;
                case @"Option2": return global::SerializationTypes.ByteEnum.@Option2;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.ByteEnum));
            }
        }

        global::SerializationTypes.ShortEnum Read74_ShortEnum(string s) {
            switch (s) {
                case @"Option0": return global::SerializationTypes.ShortEnum.@Option0;
                case @"Option1": return global::SerializationTypes.ShortEnum.@Option1;
                case @"Option2": return global::SerializationTypes.ShortEnum.@Option2;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.ShortEnum));
            }
        }

        global::SerializationTypes.IntEnum Read73_IntEnum(string s) {
            switch (s) {
                case @"Option0": return global::SerializationTypes.IntEnum.@Option0;
                case @"Option1": return global::SerializationTypes.IntEnum.@Option1;
                case @"Option2": return global::SerializationTypes.IntEnum.@Option2;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.IntEnum));
            }
        }

        System.Collections.Hashtable _EnumFlagsValues;

        internal System.Collections.Hashtable EnumFlagsValues {
            get {
                if ((object)_EnumFlagsValues == null) {
                    System.Collections.Hashtable h = new System.Collections.Hashtable();
                    h.Add(@"One", (long)global::SerializationTypes.EnumFlags.@One);
                    h.Add(@"Two", (long)global::SerializationTypes.EnumFlags.@Two);
                    h.Add(@"Three", (long)global::SerializationTypes.EnumFlags.@Three);
                    h.Add(@"Four", (long)global::SerializationTypes.EnumFlags.@Four);
                    _EnumFlagsValues = h;
                }
                return _EnumFlagsValues;
            }
        }

        global::SerializationTypes.EnumFlags Read69_EnumFlags(string s) {
            return (global::SerializationTypes.EnumFlags)ToEnum(s, EnumFlagsValues, @"global::SerializationTypes.EnumFlags");
        }

        global::SerializationTypes.TypeA Read58_TypeA(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id63_TypeA && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeA o;
            o = new global::SerializationTypes.TypeA();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id122_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Name = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Name");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Name");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.MyEnum Read53_MyEnum(string s) {
            switch (s) {
                case @"One": return global::SerializationTypes.MyEnum.@One;
                case @"Two": return global::SerializationTypes.MyEnum.@Two;
                case @"Three": return global::SerializationTypes.MyEnum.@Three;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.MyEnum));
            }
        }

        global::Parameter Read41_Parameter(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id45_Parameter && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id129_ParameterOfString && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read40_ParameterOfString(isNullable, false);
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Parameter o;
            o = new global::Parameter();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id122_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
                    o.@Name = Reader.Value;
                    paramsRead[0] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o, @":Name");
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    UnknownNode((object)o, @"");
                }
                else {
                    UnknownNode((object)o, @"");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::Parameter<global::System.String> Read40_ParameterOfString(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id129_ParameterOfString && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Parameter<global::System.String> o;
            o = new global::Parameter<global::System.String>();
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id122_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
                    o.@Name = Reader.Value;
                    paramsRead[0] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o, @":Name");
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id155_Value && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Value = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Value");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Value");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::TypeWithLinkedProperty Read38_TypeWithLinkedProperty(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id41_TypeWithLinkedProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::TypeWithLinkedProperty o;
            o = new global::TypeWithLinkedProperty();
            if ((object)(o.@Children) == null) o.@Children = new global::System.Collections.Generic.List<global::TypeWithLinkedProperty>();
            global::System.Collections.Generic.List<global::TypeWithLinkedProperty> a_1 = (global::System.Collections.Generic.List<global::TypeWithLinkedProperty>)o.@Children;
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id156_Child && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Child = Read38_TypeWithLinkedProperty(false, true);
                            paramsRead[0] = true;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id157_Children && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                if ((object)(o.@Children) == null) o.@Children = new global::System.Collections.Generic.List<global::TypeWithLinkedProperty>();
                                global::System.Collections.Generic.List<global::TypeWithLinkedProperty> a_1_0 = (global::System.Collections.Generic.List<global::TypeWithLinkedProperty>)o.@Children;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id41_TypeWithLinkedProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    if ((object)(a_1_0) == null) Reader.Skip(); else a_1_0.Add(Read38_TypeWithLinkedProperty(true, true));
                                                    break;
                                                }
                                                UnknownNode(null, @":TypeWithLinkedProperty");
                                            } while (false);
                                        }
                                        else {
                                            UnknownNode(null, @":TypeWithLinkedProperty");
                                        }
                                        Reader.MoveToContent();
                                    }
                                ReadEndElement();
                                }
                            }
                            break;
                        }
                        UnknownNode((object)o, @":Child, :Children");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Child, :Children");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::Instrument Read29_Instrument(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id33_Instrument && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id34_Brass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read31_Brass(isNullable, false);
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id35_Trumpet && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read32_Trumpet(isNullable, false);
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Instrument o;
            o = new global::Instrument();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id122_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Name = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Name");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Name");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::Trumpet Read32_Trumpet(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id35_Trumpet && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Trumpet o;
            o = new global::Trumpet();
            System.Span<bool> paramsRead = stackalloc bool[3];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id122_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Name = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id158_IsValved && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@IsValved = System.Xml.XmlConvert.ToBoolean(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id159_Modulation && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Modulation = ToChar(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Name, :IsValved, :Modulation");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Name, :IsValved, :Modulation");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::Brass Read31_Brass(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id34_Brass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id35_Trumpet && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read32_Trumpet(isNullable, false);
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Brass o;
            o = new global::Brass();
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id122_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Name = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id158_IsValved && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@IsValved = System.Xml.XmlConvert.ToBoolean(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Name, :IsValved");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Name, :IsValved");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::OrderedItem Read22_OrderedItem(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id26_OrderedItem && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id24_httpwwwcontoso1com)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::OrderedItem o;
            o = new global::OrderedItem();
            System.Span<bool> paramsRead = stackalloc bool[5];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id160_ItemName && (object) Reader.NamespaceURI == (object)id24_httpwwwcontoso1com)) {
                            {
                                o.@ItemName = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id161_Description && (object) Reader.NamespaceURI == (object)id24_httpwwwcontoso1com)) {
                            {
                                o.@Description = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id162_UnitPrice && (object) Reader.NamespaceURI == (object)id24_httpwwwcontoso1com)) {
                            {
                                o.@UnitPrice = System.Xml.XmlConvert.ToDecimal(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id163_Quantity && (object) Reader.NamespaceURI == (object)id24_httpwwwcontoso1com)) {
                            {
                                o.@Quantity = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id164_LineTotal && (object) Reader.NamespaceURI == (object)id24_httpwwwcontoso1com)) {
                            {
                                o.@LineTotal = System.Xml.XmlConvert.ToDecimal(Reader.ReadElementString());
                            }
                            paramsRead[4] = true;
                            break;
                        }
                        UnknownNode((object)o, @"http://www.contoso1.com:ItemName, http://www.contoso1.com:Description, http://www.contoso1.com:UnitPrice, http://www.contoso1.com:Quantity, http://www.contoso1.com:LineTotal");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @"http://www.contoso1.com:ItemName, http://www.contoso1.com:Description, http://www.contoso1.com:UnitPrice, http://www.contoso1.com:Quantity, http://www.contoso1.com:LineTotal");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::DogBreed Read12_DogBreed(string s) {
            switch (s) {
                case @"GermanShepherd": return global::DogBreed.@GermanShepherd;
                case @"LabradorRetriever": return global::DogBreed.@LabradorRetriever;
                default: throw CreateUnknownConstantException(s, typeof(global::DogBreed));
            }
        }

        global::TypeWithXmlElementProperty Read2_TypeWithXmlElementProperty(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id1_TypeWithXmlElementProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::TypeWithXmlElementProperty o;
            o = new global::TypeWithXmlElementProperty();
            global::System.Xml.XmlElement[] a_0 = null;
            int ca_0 = 0;
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                o.@Elements = (global::System.Xml.XmlElement[])ShrinkArray(a_0, ca_0, typeof(global::System.Xml.XmlElement), true);
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    a_0 = (global::System.Xml.XmlElement[])EnsureArrayIndex(a_0, ca_0, typeof(global::System.Xml.XmlElement));a_0[ca_0++] = (global::System.Xml.XmlElement)ReadXmlNode(false);
                }
                else {
                    UnknownNode((object)o, @"");
                }
                Reader.MoveToContent();
            }
            o.@Elements = (global::System.Xml.XmlElement[])ShrinkArray(a_0, ca_0, typeof(global::System.Xml.XmlElement), true);
            ReadEndElement();
            return o;
        }

        global::TypeWithXmlDocumentProperty Read3_TypeWithXmlDocumentProperty(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id3_TypeWithXmlDocumentProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::TypeWithXmlDocumentProperty o;
            o = new global::TypeWithXmlDocumentProperty();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id42_Document && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Document = (global::System.Xml.XmlDocument)ReadXmlDocument(true);
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Document");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Document");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::TypeWithBinaryProperty Read4_TypeWithBinaryProperty(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id4_TypeWithBinaryProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::TypeWithBinaryProperty o;
            o = new global::TypeWithBinaryProperty();
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id165_BinaryHexContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@BinaryHexContent = ToByteArrayHex(false);
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id166_Base64Content && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Base64Content = ToByteArrayBase64(false);
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        UnknownNode((object)o, @":BinaryHexContent, :Base64Content");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":BinaryHexContent, :Base64Content");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::TypeWithDateTimeOffsetProperties Read6_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id5_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::TypeWithDateTimeOffsetProperties o;
            o = new global::TypeWithDateTimeOffsetProperties();
            System.Span<bool> paramsRead = stackalloc bool[5];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id167_DTO && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                if (Reader.IsEmptyElement) {
                                    Reader.Skip();
                                    o.@DTO = default(System.DateTimeOffset);
                                }
                                else {
                                    o.@DTO = System.Xml.XmlConvert.ToDateTimeOffset(Reader.ReadElementString());
                                }
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id168_DTO2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                if (Reader.IsEmptyElement) {
                                    Reader.Skip();
                                    o.@DTO2 = default(System.DateTimeOffset);
                                }
                                else {
                                    o.@DTO2 = System.Xml.XmlConvert.ToDateTimeOffset(Reader.ReadElementString());
                                }
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id169_DefaultDTO && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                if (Reader.IsEmptyElement) {
                                    Reader.Skip();
                                    o.@DTOWithDefault = default(System.DateTimeOffset);
                                }
                                else {
                                    o.@DTOWithDefault = System.Xml.XmlConvert.ToDateTimeOffset(Reader.ReadElementString());
                                }
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id170_NullableDTO && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@NullableDTO = Read5_NullableOfDateTimeOffset(true);
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id171_NullableDefaultDTO && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@NullableDTOWithDefault = Read5_NullableOfDateTimeOffset(true);
                            paramsRead[4] = true;
                            break;
                        }
                        UnknownNode((object)o, @":DTO, :DTO2, :DefaultDTO, :NullableDTO, :NullableDefaultDTO");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":DTO, :DTO2, :DefaultDTO, :NullableDTO, :NullableDefaultDTO");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::System.Nullable<global::System.DateTimeOffset> Read5_NullableOfDateTimeOffset(bool checkType) {
            global::System.Nullable<global::System.DateTimeOffset> o = default(global::System.Nullable<global::System.DateTimeOffset>);
            if (ReadNull())
                return o;
            {
                if (Reader.IsEmptyElement) {
                    Reader.Skip();
                    o = default(System.DateTimeOffset);
                }
                else {
                    o = System.Xml.XmlConvert.ToDateTimeOffset(Reader.ReadElementString());
                }
            }
            return o;
        }

        global::TypeWithTimeSpanProperty Read7_TypeWithTimeSpanProperty(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id6_TypeWithTimeSpanProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::TypeWithTimeSpanProperty o;
            o = new global::TypeWithTimeSpanProperty();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id172_TimeSpanProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                if (Reader.IsEmptyElement) {
                                    Reader.Skip();
                                    o.@TimeSpanProperty = default(System.TimeSpan);
                                }
                                else {
                                    o.@TimeSpanProperty = System.Xml.XmlConvert.ToTimeSpan(Reader.ReadElementString());
                                }
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":TimeSpanProperty");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":TimeSpanProperty");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::TypeWithDefaultTimeSpanProperty Read8_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id7_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::TypeWithDefaultTimeSpanProperty o;
            o = new global::TypeWithDefaultTimeSpanProperty();
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id172_TimeSpanProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                if (Reader.IsEmptyElement) {
                                    Reader.Skip();
                                    o.@TimeSpanProperty = default(System.TimeSpan);
                                }
                                else {
                                    o.@TimeSpanProperty = System.Xml.XmlConvert.ToTimeSpan(Reader.ReadElementString());
                                }
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id173_TimeSpanProperty2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                if (Reader.IsEmptyElement) {
                                    Reader.Skip();
                                    o.@TimeSpanProperty2 = default(System.TimeSpan);
                                }
                                else {
                                    o.@TimeSpanProperty2 = System.Xml.XmlConvert.ToTimeSpan(Reader.ReadElementString());
                                }
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        UnknownNode((object)o, @":TimeSpanProperty, :TimeSpanProperty2");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":TimeSpanProperty, :TimeSpanProperty2");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::TypeWithByteProperty Read9_TypeWithByteProperty(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id8_TypeWithByteProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::TypeWithByteProperty o;
            o = new global::TypeWithByteProperty();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id174_ByteProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@ByteProperty = System.Xml.XmlConvert.ToByte(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":ByteProperty");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":ByteProperty");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::TypeWithXmlNodeArrayProperty Read10_TypeWithXmlNodeArrayProperty(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id9_TypeWithXmlNodeArrayProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::TypeWithXmlNodeArrayProperty o;
            o = new global::TypeWithXmlNodeArrayProperty();
            global::System.Xml.XmlNode[] a_0 = null;
            int ca_0 = 0;
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                o.@CDATA = (global::System.Xml.XmlNode[])ShrinkArray(a_0, ca_0, typeof(global::System.Xml.XmlNode), true);
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                string tmp = null;
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    UnknownNode((object)o, @"");
                }
                else if (Reader.NodeType == System.Xml.XmlNodeType.Text || 
                Reader.NodeType == System.Xml.XmlNodeType.CDATA || 
                Reader.NodeType == System.Xml.XmlNodeType.Whitespace || 
                Reader.NodeType == System.Xml.XmlNodeType.SignificantWhitespace) {
                    a_0 = (global::System.Xml.XmlNode[])EnsureArrayIndex(a_0, ca_0, typeof(global::System.Xml.XmlNode));a_0[ca_0++] = (global::System.Xml.XmlNode)Document.CreateTextNode(Reader.ReadString());
                }
                else {
                    UnknownNode((object)o, @"");
                }
                Reader.MoveToContent();
            }
            o.@CDATA = (global::System.Xml.XmlNode[])ShrinkArray(a_0, ca_0, typeof(global::System.Xml.XmlNode), true);
            ReadEndElement();
            return o;
        }

        global::Dog Read13_Dog(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id11_Dog && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Dog o;
            o = new global::Dog();
            System.Span<bool> paramsRead = stackalloc bool[3];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id175_Age && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Age = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id122_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Name = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id176_Breed && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Breed = Read12_DogBreed(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Age, :Name, :Breed");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Age, :Name, :Breed");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::Animal Read11_Animal(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id10_Animal && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id11_Dog && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read13_Dog(isNullable, false);
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Animal o;
            o = new global::Animal();
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id175_Age && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Age = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id122_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Name = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Age, :Name");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Age, :Name");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::Vehicle Read14_Vehicle(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id14_Vehicle && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Vehicle o;
            o = new global::Vehicle();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id177_LicenseNumber && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@LicenseNumber = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":LicenseNumber");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":LicenseNumber");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::Group Read15_Group(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id13_Group && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Group o;
            o = new global::Group();
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id178_GroupName && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@GroupName = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id179_GroupVehicle && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@GroupVehicle = Read14_Vehicle(false, true);
                            paramsRead[1] = true;
                            break;
                        }
                        UnknownNode((object)o, @":GroupName, :GroupVehicle");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":GroupName, :GroupVehicle");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::Employee Read16_Employee(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id15_Employee && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Employee o;
            o = new global::Employee();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id180_EmployeeName && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@EmployeeName = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":EmployeeName");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":EmployeeName");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::DerivedClass Read17_DerivedClass(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id17_DerivedClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::DerivedClass o;
            o = new global::DerivedClass();
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id155_Value && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Value = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id181_value && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@value = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Value, :value");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Value, :value");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::BaseClass Read18_BaseClass(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id16_BaseClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id17_DerivedClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read17_DerivedClass(isNullable, false);
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::BaseClass o;
            o = new global::BaseClass();
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id155_Value && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Value = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id181_value && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@value = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Value, :value");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Value, :value");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SimpleDerivedClass Read19_SimpleDerivedClass(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id19_SimpleDerivedClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SimpleDerivedClass o;
            o = new global::SimpleDerivedClass();
            System.Span<bool> paramsRead = stackalloc bool[3];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id182_AttributeString && (object) Reader.NamespaceURI == (object)id2_Item)) {
                    o.@AttributeString = Reader.Value;
                    paramsRead[0] = true;
                }
                else if (!paramsRead[1] && ((object) Reader.LocalName == (object)id183_DateTimeValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
                    o.@DateTimeValue = ToDateTime(Reader.Value);
                    paramsRead[1] = true;
                }
                else if (!paramsRead[2] && ((object) Reader.LocalName == (object)id184_BoolValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
                    o.@BoolValue = System.Xml.XmlConvert.ToBoolean(Reader.Value);
                    paramsRead[2] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o, @":AttributeString, :DateTimeValue, :BoolValue");
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    UnknownNode((object)o, @"");
                }
                else {
                    UnknownNode((object)o, @"");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SimpleBaseClass Read20_SimpleBaseClass(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id18_SimpleBaseClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id19_SimpleDerivedClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read19_SimpleDerivedClass(isNullable, false);
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SimpleBaseClass o;
            o = new global::SimpleBaseClass();
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id182_AttributeString && (object) Reader.NamespaceURI == (object)id2_Item)) {
                    o.@AttributeString = Reader.Value;
                    paramsRead[0] = true;
                }
                else if (!paramsRead[1] && ((object) Reader.LocalName == (object)id183_DateTimeValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
                    o.@DateTimeValue = ToDateTime(Reader.Value);
                    paramsRead[1] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o, @":AttributeString, :DateTimeValue");
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    UnknownNode((object)o, @"");
                }
                else {
                    UnknownNode((object)o, @"");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::Address Read21_Address(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id25_Address && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id24_httpwwwcontoso1com)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Address o;
            o = new global::Address();
            System.Span<bool> paramsRead = stackalloc bool[5];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id122_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
                    o.@Name = Reader.Value;
                    paramsRead[0] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o, @":Name");
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id185_Line1 && (object) Reader.NamespaceURI == (object)id24_httpwwwcontoso1com)) {
                            {
                                o.@Line1 = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id186_City && (object) Reader.NamespaceURI == (object)id24_httpwwwcontoso1com)) {
                            {
                                o.@City = Reader.ReadElementString();
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id187_State && (object) Reader.NamespaceURI == (object)id24_httpwwwcontoso1com)) {
                            {
                                o.@State = Reader.ReadElementString();
                            }
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id188_Zip && (object) Reader.NamespaceURI == (object)id24_httpwwwcontoso1com)) {
                            {
                                o.@Zip = Reader.ReadElementString();
                            }
                            paramsRead[4] = true;
                            break;
                        }
                        UnknownNode((object)o, @"http://www.contoso1.com:Line1, http://www.contoso1.com:City, http://www.contoso1.com:State, http://www.contoso1.com:Zip");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @"http://www.contoso1.com:Line1, http://www.contoso1.com:City, http://www.contoso1.com:State, http://www.contoso1.com:Zip");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::PurchaseOrder Read23_PurchaseOrder(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id23_PurchaseOrder && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id24_httpwwwcontoso1com)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::PurchaseOrder o;
            o = new global::PurchaseOrder();
            global::OrderedItem[] a_2 = null;
            int ca_2 = 0;
            System.Span<bool> paramsRead = stackalloc bool[6];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id189_ShipTo && (object) Reader.NamespaceURI == (object)id24_httpwwwcontoso1com)) {
                            o.@ShipTo = Read21_Address(false, true);
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id190_OrderDate && (object) Reader.NamespaceURI == (object)id24_httpwwwcontoso1com)) {
                            {
                                o.@OrderDate = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id191_Items && (object) Reader.NamespaceURI == (object)id24_httpwwwcontoso1com)) {
                            if (!ReadNull()) {
                                global::OrderedItem[] a_2_0 = null;
                                int ca_2_0 = 0;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id26_OrderedItem && (object) Reader.NamespaceURI == (object)id24_httpwwwcontoso1com)) {
                                                    a_2_0 = (global::OrderedItem[])EnsureArrayIndex(a_2_0, ca_2_0, typeof(global::OrderedItem));a_2_0[ca_2_0++] = Read22_OrderedItem(true, true);
                                                    break;
                                                }
                                                UnknownNode(null, @"http://www.contoso1.com:OrderedItem");
                                            } while (false);
                                        }
                                        else {
                                            UnknownNode(null, @"http://www.contoso1.com:OrderedItem");
                                        }
                                        Reader.MoveToContent();
                                    }
                                ReadEndElement();
                                }
                                o.@OrderedItems = (global::OrderedItem[])ShrinkArray(a_2_0, ca_2_0, typeof(global::OrderedItem), false);
                            }
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id192_SubTotal && (object) Reader.NamespaceURI == (object)id24_httpwwwcontoso1com)) {
                            {
                                o.@SubTotal = System.Xml.XmlConvert.ToDecimal(Reader.ReadElementString());
                            }
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id193_ShipCost && (object) Reader.NamespaceURI == (object)id24_httpwwwcontoso1com)) {
                            {
                                o.@ShipCost = System.Xml.XmlConvert.ToDecimal(Reader.ReadElementString());
                            }
                            paramsRead[4] = true;
                            break;
                        }
                        if (!paramsRead[5] && ((object) Reader.LocalName == (object)id194_TotalCost && (object) Reader.NamespaceURI == (object)id24_httpwwwcontoso1com)) {
                            {
                                o.@TotalCost = System.Xml.XmlConvert.ToDecimal(Reader.ReadElementString());
                            }
                            paramsRead[5] = true;
                            break;
                        }
                        UnknownNode((object)o, @"http://www.contoso1.com:ShipTo, http://www.contoso1.com:OrderDate, http://www.contoso1.com:Items, http://www.contoso1.com:SubTotal, http://www.contoso1.com:ShipCost, http://www.contoso1.com:TotalCost");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @"http://www.contoso1.com:ShipTo, http://www.contoso1.com:OrderDate, http://www.contoso1.com:Items, http://www.contoso1.com:SubTotal, http://www.contoso1.com:ShipCost, http://www.contoso1.com:TotalCost");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::Address Read24_Address(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id25_Address && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Address o;
            o = new global::Address();
            System.Span<bool> paramsRead = stackalloc bool[5];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id122_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
                    o.@Name = Reader.Value;
                    paramsRead[0] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o, @":Name");
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id185_Line1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Line1 = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id186_City && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@City = Reader.ReadElementString();
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id187_State && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@State = Reader.ReadElementString();
                            }
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id188_Zip && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Zip = Reader.ReadElementString();
                            }
                            paramsRead[4] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Line1, :City, :State, :Zip");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Line1, :City, :State, :Zip");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::OrderedItem Read25_OrderedItem(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id26_OrderedItem && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::OrderedItem o;
            o = new global::OrderedItem();
            System.Span<bool> paramsRead = stackalloc bool[5];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id160_ItemName && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@ItemName = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id161_Description && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Description = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id162_UnitPrice && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@UnitPrice = System.Xml.XmlConvert.ToDecimal(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id163_Quantity && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Quantity = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id164_LineTotal && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@LineTotal = System.Xml.XmlConvert.ToDecimal(Reader.ReadElementString());
                            }
                            paramsRead[4] = true;
                            break;
                        }
                        UnknownNode((object)o, @":ItemName, :Description, :UnitPrice, :Quantity, :LineTotal");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":ItemName, :Description, :UnitPrice, :Quantity, :LineTotal");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::AliasedTestType Read26_AliasedTestType(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id27_AliasedTestType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::AliasedTestType o;
            o = new global::AliasedTestType();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id195_X && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                if ((object)(o.@Aliased) == null) o.@Aliased = new global::System.Collections.Generic.List<global::System.Int32>();
                                global::System.Collections.Generic.List<global::System.Int32> a_0_0 = (global::System.Collections.Generic.List<global::System.Int32>)o.@Aliased;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id134_int && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    {
                                                        a_0_0.Add(System.Xml.XmlConvert.ToInt32(Reader.ReadElementString()));
                                                    }
                                                    break;
                                                }
                                                UnknownNode(null, @":int");
                                            } while (false);
                                        }
                                        else {
                                            UnknownNode(null, @":int");
                                        }
                                        Reader.MoveToContent();
                                    }
                                ReadEndElement();
                                }
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id196_Y && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                if ((object)(o.@Aliased) == null) o.@Aliased = new global::System.Collections.Generic.List<global::System.String>();
                                global::System.Collections.Generic.List<global::System.String> a_0_0 = (global::System.Collections.Generic.List<global::System.String>)o.@Aliased;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id136_string && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    if (ReadNull()) {
                                                        a_0_0.Add(null);
                                                    }
                                                    else {
                                                        a_0_0.Add(Reader.ReadElementString());
                                                    }
                                                    break;
                                                }
                                                UnknownNode(null, @":string");
                                            } while (false);
                                        }
                                        else {
                                            UnknownNode(null, @":string");
                                        }
                                        Reader.MoveToContent();
                                    }
                                ReadEndElement();
                                }
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id197_Z && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                if ((object)(o.@Aliased) == null) o.@Aliased = new global::System.Collections.Generic.List<global::System.Double>();
                                global::System.Collections.Generic.List<global::System.Double> a_0_0 = (global::System.Collections.Generic.List<global::System.Double>)o.@Aliased;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id138_double && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    {
                                                        a_0_0.Add(System.Xml.XmlConvert.ToDouble(Reader.ReadElementString()));
                                                    }
                                                    break;
                                                }
                                                UnknownNode(null, @":double");
                                            } while (false);
                                        }
                                        else {
                                            UnknownNode(null, @":double");
                                        }
                                        Reader.MoveToContent();
                                    }
                                ReadEndElement();
                                }
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":X, :Y, :Z");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":X, :Y, :Z");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::DerivedClass1 Read28_DerivedClass1(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id29_DerivedClass1 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::DerivedClass1 o;
            o = new global::DerivedClass1();
            if ((object)(o.@Prop) == null) o.@Prop = new global::MyCollection1();
            global::MyCollection1 a_0 = (global::MyCollection1)o.@Prop;
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (((object) Reader.LocalName == (object)id198_Prop && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                a_0.Add(ToDateTime(Reader.ReadElementString()));
                            }
                            break;
                        }
                        UnknownNode((object)o, @":Prop");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Prop");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::BaseClass1 Read27_BaseClass1(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id28_BaseClass1 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id29_DerivedClass1 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read28_DerivedClass1(isNullable, false);
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::BaseClass1 o;
            o = new global::BaseClass1();
            if ((object)(o.@Prop) == null) o.@Prop = new global::MyCollection1();
            global::MyCollection1 a_0 = (global::MyCollection1)o.@Prop;
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (((object) Reader.LocalName == (object)id198_Prop && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                a_0.Add(ToDateTime(Reader.ReadElementString()));
                            }
                            break;
                        }
                        UnknownNode((object)o, @":Prop");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Prop");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::Orchestra Read30_Orchestra(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id32_Orchestra && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Orchestra o;
            o = new global::Orchestra();
            global::Instrument[] a_0 = null;
            int ca_0 = 0;
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (((object) Reader.LocalName == (object)id199_Instruments && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                global::Instrument[] a_0_0 = null;
                                int ca_0_0 = 0;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id33_Instrument && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    a_0_0 = (global::Instrument[])EnsureArrayIndex(a_0_0, ca_0_0, typeof(global::Instrument));a_0_0[ca_0_0++] = Read29_Instrument(true, true);
                                                    break;
                                                }
                                                UnknownNode(null, @":Instrument");
                                            } while (false);
                                        }
                                        else {
                                            UnknownNode(null, @":Instrument");
                                        }
                                        Reader.MoveToContent();
                                    }
                                ReadEndElement();
                                }
                                o.@Instruments = (global::Instrument[])ShrinkArray(a_0_0, ca_0_0, typeof(global::Instrument), false);
                            }
                            break;
                        }
                        UnknownNode((object)o, @":Instruments");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Instruments");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::Pet Read33_Pet(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id36_Pet && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Pet o;
            o = new global::Pet();
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id10_Animal && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Animal = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id200_Comment2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Comment2 = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Animal, :Comment2");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Animal, :Comment2");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::DefaultValuesSetToNaN Read34_DefaultValuesSetToNaN(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id37_DefaultValuesSetToNaN && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::DefaultValuesSetToNaN o;
            o = new global::DefaultValuesSetToNaN();
            System.Span<bool> paramsRead = stackalloc bool[4];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id201_DoubleField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@DoubleField = System.Xml.XmlConvert.ToDouble(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id202_SingleField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@SingleField = System.Xml.XmlConvert.ToSingle(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id203_DoubleProp && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@DoubleProp = System.Xml.XmlConvert.ToDouble(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id204_FloatProp && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@FloatProp = System.Xml.XmlConvert.ToSingle(Reader.ReadElementString());
                            }
                            paramsRead[3] = true;
                            break;
                        }
                        UnknownNode((object)o, @":DoubleField, :SingleField, :DoubleProp, :FloatProp");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":DoubleField, :SingleField, :DoubleProp, :FloatProp");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::DefaultValuesSetToPositiveInfinity Read35_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id38_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::DefaultValuesSetToPositiveInfinity o;
            o = new global::DefaultValuesSetToPositiveInfinity();
            System.Span<bool> paramsRead = stackalloc bool[4];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id201_DoubleField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@DoubleField = System.Xml.XmlConvert.ToDouble(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id202_SingleField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@SingleField = System.Xml.XmlConvert.ToSingle(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id203_DoubleProp && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@DoubleProp = System.Xml.XmlConvert.ToDouble(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id204_FloatProp && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@FloatProp = System.Xml.XmlConvert.ToSingle(Reader.ReadElementString());
                            }
                            paramsRead[3] = true;
                            break;
                        }
                        UnknownNode((object)o, @":DoubleField, :SingleField, :DoubleProp, :FloatProp");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":DoubleField, :SingleField, :DoubleProp, :FloatProp");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::DefaultValuesSetToNegativeInfinity Read36_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id39_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::DefaultValuesSetToNegativeInfinity o;
            o = new global::DefaultValuesSetToNegativeInfinity();
            System.Span<bool> paramsRead = stackalloc bool[4];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id201_DoubleField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@DoubleField = System.Xml.XmlConvert.ToDouble(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id202_SingleField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@SingleField = System.Xml.XmlConvert.ToSingle(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id203_DoubleProp && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@DoubleProp = System.Xml.XmlConvert.ToDouble(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id204_FloatProp && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@FloatProp = System.Xml.XmlConvert.ToSingle(Reader.ReadElementString());
                            }
                            paramsRead[3] = true;
                            break;
                        }
                        UnknownNode((object)o, @":DoubleField, :SingleField, :DoubleProp, :FloatProp");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":DoubleField, :SingleField, :DoubleProp, :FloatProp");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::TypeWithMismatchBetweenAttributeAndPropertyType Read37_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id131_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::TypeWithMismatchBetweenAttributeAndPropertyType o;
            o = new global::TypeWithMismatchBetweenAttributeAndPropertyType();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id205_IntValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
                    o.@IntValue = System.Xml.XmlConvert.ToInt32(Reader.Value);
                    paramsRead[0] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o, @":IntValue");
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    UnknownNode((object)o, @"");
                }
                else {
                    UnknownNode((object)o, @"");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::MsgDocumentType Read39_MsgDocumentType(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id130_MsgDocumentType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id43_httpexamplecom)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::MsgDocumentType o;
            o = new global::MsgDocumentType();
            global::System.String[] a_1 = null;
            int ca_1 = 0;
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id206_id && (object) Reader.NamespaceURI == (object)id2_Item)) {
                    o.@Id = CollapseWhitespace(Reader.Value);
                    paramsRead[0] = true;
                }
                else if (((object) Reader.LocalName == (object)id207_refs && (object) Reader.NamespaceURI == (object)id2_Item)) {
                    string listValues = Reader.Value;
                    string[] vals = listValues.Split(null);
                    for (int i = 0; i < vals.Length; i++) {
                        a_1 = (global::System.String[])EnsureArrayIndex(a_1, ca_1, typeof(global::System.String));a_1[ca_1++] = CollapseWhitespace(vals[i]);
                    }
                }
                else if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o, @":id, :refs");
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                o.@Refs = (global::System.String[])ShrinkArray(a_1, ca_1, typeof(global::System.String), true);
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    UnknownNode((object)o, @"");
                }
                else {
                    UnknownNode((object)o, @"");
                }
                Reader.MoveToContent();
            }
            o.@Refs = (global::System.String[])ShrinkArray(a_1, ca_1, typeof(global::System.String), true);
            ReadEndElement();
            return o;
        }

        global::RootClass Read42_RootClass(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id44_RootClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::RootClass o;
            o = new global::RootClass();
            if ((object)(o.@Parameters) == null) o.@Parameters = new global::System.Collections.Generic.List<global::Parameter>();
            global::System.Collections.Generic.List<global::Parameter> a_0 = (global::System.Collections.Generic.List<global::Parameter>)o.@Parameters;
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (((object) Reader.LocalName == (object)id208_Parameters && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                if ((object)(o.@Parameters) == null) o.@Parameters = new global::System.Collections.Generic.List<global::Parameter>();
                                global::System.Collections.Generic.List<global::Parameter> a_0_0 = (global::System.Collections.Generic.List<global::Parameter>)o.@Parameters;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id45_Parameter && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    if ((object)(a_0_0) == null) Reader.Skip(); else a_0_0.Add(Read41_Parameter(true, true));
                                                    break;
                                                }
                                                UnknownNode(null, @":Parameter");
                                            } while (false);
                                        }
                                        else {
                                            UnknownNode(null, @":Parameter");
                                        }
                                        Reader.MoveToContent();
                                    }
                                ReadEndElement();
                                }
                            }
                            break;
                        }
                        UnknownNode((object)o, @":Parameters");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Parameters");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::XElementWrapper Read43_XElementWrapper(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id46_XElementWrapper && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::XElementWrapper o;
            o = new global::XElementWrapper();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id155_Value && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Value = (global::System.Xml.Linq.XElement)ReadSerializable(( System.Xml.Serialization.IXmlSerializable)new global::System.Xml.Linq.XElement("default"), true
                            );
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Value");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Value");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::XElementStruct Read44_XElementStruct(bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id47_XElementStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            global::XElementStruct o;
            try {
                o = (global::XElementStruct)System.Activator.CreateInstance(typeof(global::XElementStruct), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.CreateInstance | System.Reflection.BindingFlags.NonPublic, null, new object[0], null);
            }
            catch (System.MissingMethodException) {
                throw CreateInaccessibleConstructorException(@"global::XElementStruct");
            }
            catch (System.Security.SecurityException) {
                throw CreateCtorHasSecurityException(@"global::XElementStruct");
            }
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id209_xelement && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@xelement = (global::System.Xml.Linq.XElement)ReadSerializable(( System.Xml.Serialization.IXmlSerializable)new global::System.Xml.Linq.XElement("default"), true
                            );
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":xelement");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":xelement");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::XElementArrayWrapper Read45_XElementArrayWrapper(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id48_XElementArrayWrapper && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::XElementArrayWrapper o;
            o = new global::XElementArrayWrapper();
            global::System.Xml.Linq.XElement[] a_0 = null;
            int ca_0 = 0;
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (((object) Reader.LocalName == (object)id210_xelements && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                global::System.Xml.Linq.XElement[] a_0_0 = null;
                                int ca_0_0 = 0;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id143_XElement && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    a_0_0 = (global::System.Xml.Linq.XElement[])EnsureArrayIndex(a_0_0, ca_0_0, typeof(global::System.Xml.Linq.XElement));a_0_0[ca_0_0++] = (global::System.Xml.Linq.XElement)ReadSerializable(( System.Xml.Serialization.IXmlSerializable)new global::System.Xml.Linq.XElement("default"), true
                                                    );
                                                    break;
                                                }
                                                UnknownNode(null, @":XElement");
                                            } while (false);
                                        }
                                        else {
                                            UnknownNode(null, @":XElement");
                                        }
                                        Reader.MoveToContent();
                                    }
                                ReadEndElement();
                                }
                                o.@xelements = (global::System.Xml.Linq.XElement[])ShrinkArray(a_0_0, ca_0_0, typeof(global::System.Xml.Linq.XElement), false);
                            }
                            break;
                        }
                        UnknownNode((object)o, @":xelements");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":xelements");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWithDateTimeStringProperty Read46_TypeWithDateTimeStringProperty(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id49_TypeWithDateTimeStringProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithDateTimeStringProperty o;
            o = new global::SerializationTypes.TypeWithDateTimeStringProperty();
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id211_DateTimeString && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@DateTimeString = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id212_CurrentDateTime && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@CurrentDateTime = ToDateTime(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        UnknownNode((object)o, @":DateTimeString, :CurrentDateTime");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":DateTimeString, :CurrentDateTime");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWithGetSetArrayMembers Read48_TypeWithGetSetArrayMembers(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id51_TypeWithGetSetArrayMembers && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithGetSetArrayMembers o;
            o = new global::SerializationTypes.TypeWithGetSetArrayMembers();
            global::SerializationTypes.SimpleType[] a_0 = null;
            int ca_0 = 0;
            global::System.Int32[] a_1 = null;
            int ca_1 = 0;
            global::SerializationTypes.SimpleType[] a_2 = null;
            int ca_2 = 0;
            global::System.Int32[] a_3 = null;
            int ca_3 = 0;
            System.Span<bool> paramsRead = stackalloc bool[4];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (((object) Reader.LocalName == (object)id213_F1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                global::SerializationTypes.SimpleType[] a_0_0 = null;
                                int ca_0_0 = 0;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id50_SimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    a_0_0 = (global::SerializationTypes.SimpleType[])EnsureArrayIndex(a_0_0, ca_0_0, typeof(global::SerializationTypes.SimpleType));a_0_0[ca_0_0++] = Read47_SimpleType(true, true);
                                                    break;
                                                }
                                                UnknownNode(null, @":SimpleType");
                                            } while (false);
                                        }
                                        else {
                                            UnknownNode(null, @":SimpleType");
                                        }
                                        Reader.MoveToContent();
                                    }
                                ReadEndElement();
                                }
                                o.@F1 = (global::SerializationTypes.SimpleType[])ShrinkArray(a_0_0, ca_0_0, typeof(global::SerializationTypes.SimpleType), false);
                            }
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id214_F2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                global::System.Int32[] a_1_0 = null;
                                int ca_1_0 = 0;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id134_int && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    {
                                                        a_1_0 = (global::System.Int32[])EnsureArrayIndex(a_1_0, ca_1_0, typeof(global::System.Int32));a_1_0[ca_1_0++] = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                                                    }
                                                    break;
                                                }
                                                UnknownNode(null, @":int");
                                            } while (false);
                                        }
                                        else {
                                            UnknownNode(null, @":int");
                                        }
                                        Reader.MoveToContent();
                                    }
                                ReadEndElement();
                                }
                                o.@F2 = (global::System.Int32[])ShrinkArray(a_1_0, ca_1_0, typeof(global::System.Int32), false);
                            }
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id153_P1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                global::SerializationTypes.SimpleType[] a_2_0 = null;
                                int ca_2_0 = 0;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id50_SimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    a_2_0 = (global::SerializationTypes.SimpleType[])EnsureArrayIndex(a_2_0, ca_2_0, typeof(global::SerializationTypes.SimpleType));a_2_0[ca_2_0++] = Read47_SimpleType(true, true);
                                                    break;
                                                }
                                                UnknownNode(null, @":SimpleType");
                                            } while (false);
                                        }
                                        else {
                                            UnknownNode(null, @":SimpleType");
                                        }
                                        Reader.MoveToContent();
                                    }
                                ReadEndElement();
                                }
                                o.@P1 = (global::SerializationTypes.SimpleType[])ShrinkArray(a_2_0, ca_2_0, typeof(global::SerializationTypes.SimpleType), false);
                            }
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id154_P2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                global::System.Int32[] a_3_0 = null;
                                int ca_3_0 = 0;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id134_int && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    {
                                                        a_3_0 = (global::System.Int32[])EnsureArrayIndex(a_3_0, ca_3_0, typeof(global::System.Int32));a_3_0[ca_3_0++] = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                                                    }
                                                    break;
                                                }
                                                UnknownNode(null, @":int");
                                            } while (false);
                                        }
                                        else {
                                            UnknownNode(null, @":int");
                                        }
                                        Reader.MoveToContent();
                                    }
                                ReadEndElement();
                                }
                                o.@P2 = (global::System.Int32[])ShrinkArray(a_3_0, ca_3_0, typeof(global::System.Int32), false);
                            }
                            break;
                        }
                        UnknownNode((object)o, @":F1, :F2, :P1, :P2");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":F1, :F2, :P1, :P2");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWithGetOnlyArrayProperties Read49_TypeWithGetOnlyArrayProperties(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id52_TypeWithGetOnlyArrayProperties && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithGetOnlyArrayProperties o;
            o = new global::SerializationTypes.TypeWithGetOnlyArrayProperties();
            System.Span<bool> paramsRead = stackalloc bool[0];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    UnknownNode((object)o, @"");
                }
                else {
                    UnknownNode((object)o, @"");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.StructNotSerializable Read50_StructNotSerializable(bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id53_StructNotSerializable && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            global::SerializationTypes.StructNotSerializable o;
            try {
                o = (global::SerializationTypes.StructNotSerializable)System.Activator.CreateInstance(typeof(global::SerializationTypes.StructNotSerializable), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.CreateInstance | System.Reflection.BindingFlags.NonPublic, null, new object[0], null);
            }
            catch (System.MissingMethodException) {
                throw CreateInaccessibleConstructorException(@"global::SerializationTypes.StructNotSerializable");
            }
            catch (System.Security.SecurityException) {
                throw CreateCtorHasSecurityException(@"global::SerializationTypes.StructNotSerializable");
            }
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id181_value && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@value = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":value");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":value");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWithMyCollectionField Read51_TypeWithMyCollectionField(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id54_TypeWithMyCollectionField && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithMyCollectionField o;
            o = new global::SerializationTypes.TypeWithMyCollectionField();
            if ((object)(o.@Collection) == null) o.@Collection = new global::SerializationTypes.MyCollection<global::System.String>();
            global::SerializationTypes.MyCollection<global::System.String> a_0 = (global::SerializationTypes.MyCollection<global::System.String>)o.@Collection;
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (((object) Reader.LocalName == (object)id215_Collection && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                if ((object)(o.@Collection) == null) o.@Collection = new global::SerializationTypes.MyCollection<global::System.String>();
                                global::SerializationTypes.MyCollection<global::System.String> a_0_0 = (global::SerializationTypes.MyCollection<global::System.String>)o.@Collection;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id136_string && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    if (ReadNull()) {
                                                        a_0_0.Add(null);
                                                    }
                                                    else {
                                                        a_0_0.Add(Reader.ReadElementString());
                                                    }
                                                    break;
                                                }
                                                UnknownNode(null, @":string");
                                            } while (false);
                                        }
                                        else {
                                            UnknownNode(null, @":string");
                                        }
                                        Reader.MoveToContent();
                                    }
                                ReadEndElement();
                                }
                            }
                            break;
                        }
                        UnknownNode((object)o, @":Collection");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Collection");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWithReadOnlyMyCollectionProperty Read52_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id55_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithReadOnlyMyCollectionProperty o;
            o = new global::SerializationTypes.TypeWithReadOnlyMyCollectionProperty();
            global::SerializationTypes.MyCollection<global::System.String> a_0 = (global::SerializationTypes.MyCollection<global::System.String>)o.@Collection;
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (((object) Reader.LocalName == (object)id215_Collection && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                global::SerializationTypes.MyCollection<global::System.String> a_0_0 = (global::SerializationTypes.MyCollection<global::System.String>)o.@Collection;
                                if (((object)(a_0_0) == null) || (Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id136_string && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    if (ReadNull()) {
                                                        a_0_0.Add(null);
                                                    }
                                                    else {
                                                        a_0_0.Add(Reader.ReadElementString());
                                                    }
                                                    break;
                                                }
                                                UnknownNode(null, @":string");
                                            } while (false);
                                        }
                                        else {
                                            UnknownNode(null, @":string");
                                        }
                                        Reader.MoveToContent();
                                    }
                                ReadEndElement();
                                }
                            }
                            break;
                        }
                        UnknownNode((object)o, @":Collection");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Collection");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWithEnumMembers Read54_TypeWithEnumMembers(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id59_TypeWithEnumMembers && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithEnumMembers o;
            o = new global::SerializationTypes.TypeWithEnumMembers();
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id213_F1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@F1 = Read53_MyEnum(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id153_P1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@P1 = Read53_MyEnum(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        UnknownNode((object)o, @":F1, :P1");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":F1, :P1");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.DCStruct Read55_DCStruct(bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id60_DCStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            global::SerializationTypes.DCStruct o;
            try {
                o = (global::SerializationTypes.DCStruct)System.Activator.CreateInstance(typeof(global::SerializationTypes.DCStruct), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.CreateInstance | System.Reflection.BindingFlags.NonPublic, null, new object[0], null);
            }
            catch (System.MissingMethodException) {
                throw CreateInaccessibleConstructorException(@"global::SerializationTypes.DCStruct");
            }
            catch (System.Security.SecurityException) {
                throw CreateCtorHasSecurityException(@"global::SerializationTypes.DCStruct");
            }
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id216_Data && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Data = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Data");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Data");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.DCClassWithEnumAndStruct Read56_DCClassWithEnumAndStruct(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id61_DCClassWithEnumAndStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.DCClassWithEnumAndStruct o;
            o = new global::SerializationTypes.DCClassWithEnumAndStruct();
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id217_MyStruct && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@MyStruct = Read55_DCStruct(true);
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id218_MyEnum1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@MyEnum1 = Read53_MyEnum(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        UnknownNode((object)o, @":MyStruct, :MyEnum1");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":MyStruct, :MyEnum1");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.BuiltInTypes Read57_BuiltInTypes(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id62_BuiltInTypes && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.BuiltInTypes o;
            o = new global::SerializationTypes.BuiltInTypes();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id219_ByteArray && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@ByteArray = ToByteArrayBase64(false);
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":ByteArray");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":ByteArray");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeB Read59_TypeB(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id64_TypeB && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeB o;
            o = new global::SerializationTypes.TypeB();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id122_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Name = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Name");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Name");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeHasArrayOfASerializedAsB Read60_TypeHasArrayOfASerializedAsB(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id65_TypeHasArrayOfASerializedAsB && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeHasArrayOfASerializedAsB o;
            o = new global::SerializationTypes.TypeHasArrayOfASerializedAsB();
            global::SerializationTypes.TypeA[] a_0 = null;
            int ca_0 = 0;
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (((object) Reader.LocalName == (object)id191_Items && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                global::SerializationTypes.TypeA[] a_0_0 = null;
                                int ca_0_0 = 0;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id63_TypeA && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    a_0_0 = (global::SerializationTypes.TypeA[])EnsureArrayIndex(a_0_0, ca_0_0, typeof(global::SerializationTypes.TypeA));a_0_0[ca_0_0++] = Read58_TypeA(true, true);
                                                    break;
                                                }
                                                UnknownNode(null, @":TypeA");
                                            } while (false);
                                        }
                                        else {
                                            UnknownNode(null, @":TypeA");
                                        }
                                        Reader.MoveToContent();
                                    }
                                ReadEndElement();
                                }
                                o.@Items = (global::SerializationTypes.TypeA[])ShrinkArray(a_0_0, ca_0_0, typeof(global::SerializationTypes.TypeA), false);
                            }
                            break;
                        }
                        UnknownNode((object)o, @":Items");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Items");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.@__TypeNameWithSpecialCharacters漢ñ Read61_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id66_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.@__TypeNameWithSpecialCharacters漢ñ o;
            o = new global::SerializationTypes.@__TypeNameWithSpecialCharacters漢ñ();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id220_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@PropertyNameWithSpecialCharacters漢ñ = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":PropertyNameWithSpecialCharacters漢ñ");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":PropertyNameWithSpecialCharacters漢ñ");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.DerivedClassWithSameProperty2 Read64_DerivedClassWithSameProperty2(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id69_DerivedClassWithSameProperty2 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.DerivedClassWithSameProperty2 o;
            o = new global::SerializationTypes.DerivedClassWithSameProperty2();
            if ((object)(o.@ListProperty) == null) o.@ListProperty = new global::System.Collections.Generic.List<global::System.String>();
            global::System.Collections.Generic.List<global::System.String> a_3 = (global::System.Collections.Generic.List<global::System.String>)o.@ListProperty;
            System.Span<bool> paramsRead = stackalloc bool[4];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id221_StringProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@StringProperty = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id222_IntProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@IntProperty = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id223_DateTimeProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@DateTimeProperty = ToDateTime(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id224_ListProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                if ((object)(o.@ListProperty) == null) o.@ListProperty = new global::System.Collections.Generic.List<global::System.String>();
                                global::System.Collections.Generic.List<global::System.String> a_3_0 = (global::System.Collections.Generic.List<global::System.String>)o.@ListProperty;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id136_string && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    if (ReadNull()) {
                                                        a_3_0.Add(null);
                                                    }
                                                    else {
                                                        a_3_0.Add(Reader.ReadElementString());
                                                    }
                                                    break;
                                                }
                                                UnknownNode(null, @":string");
                                            } while (false);
                                        }
                                        else {
                                            UnknownNode(null, @":string");
                                        }
                                        Reader.MoveToContent();
                                    }
                                ReadEndElement();
                                }
                            }
                            break;
                        }
                        UnknownNode((object)o, @":StringProperty, :IntProperty, :DateTimeProperty, :ListProperty");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":StringProperty, :IntProperty, :DateTimeProperty, :ListProperty");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.DerivedClassWithSameProperty Read63_DerivedClassWithSameProperty(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id68_DerivedClassWithSameProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id69_DerivedClassWithSameProperty2 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read64_DerivedClassWithSameProperty2(isNullable, false);
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.DerivedClassWithSameProperty o;
            o = new global::SerializationTypes.DerivedClassWithSameProperty();
            if ((object)(o.@ListProperty) == null) o.@ListProperty = new global::System.Collections.Generic.List<global::System.String>();
            global::System.Collections.Generic.List<global::System.String> a_3 = (global::System.Collections.Generic.List<global::System.String>)o.@ListProperty;
            System.Span<bool> paramsRead = stackalloc bool[4];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id221_StringProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@StringProperty = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id222_IntProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@IntProperty = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id223_DateTimeProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@DateTimeProperty = ToDateTime(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id224_ListProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                if ((object)(o.@ListProperty) == null) o.@ListProperty = new global::System.Collections.Generic.List<global::System.String>();
                                global::System.Collections.Generic.List<global::System.String> a_3_0 = (global::System.Collections.Generic.List<global::System.String>)o.@ListProperty;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id136_string && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    if (ReadNull()) {
                                                        a_3_0.Add(null);
                                                    }
                                                    else {
                                                        a_3_0.Add(Reader.ReadElementString());
                                                    }
                                                    break;
                                                }
                                                UnknownNode(null, @":string");
                                            } while (false);
                                        }
                                        else {
                                            UnknownNode(null, @":string");
                                        }
                                        Reader.MoveToContent();
                                    }
                                ReadEndElement();
                                }
                            }
                            break;
                        }
                        UnknownNode((object)o, @":StringProperty, :IntProperty, :DateTimeProperty, :ListProperty");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":StringProperty, :IntProperty, :DateTimeProperty, :ListProperty");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.BaseClassWithSamePropertyName Read62_BaseClassWithSamePropertyName(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id67_BaseClassWithSamePropertyName && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id68_DerivedClassWithSameProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read63_DerivedClassWithSameProperty(isNullable, false);
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id69_DerivedClassWithSameProperty2 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read64_DerivedClassWithSameProperty2(isNullable, false);
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.BaseClassWithSamePropertyName o;
            o = new global::SerializationTypes.BaseClassWithSamePropertyName();
            if ((object)(o.@ListProperty) == null) o.@ListProperty = new global::System.Collections.Generic.List<global::System.String>();
            global::System.Collections.Generic.List<global::System.String> a_3 = (global::System.Collections.Generic.List<global::System.String>)o.@ListProperty;
            System.Span<bool> paramsRead = stackalloc bool[4];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id221_StringProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@StringProperty = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id222_IntProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@IntProperty = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id223_DateTimeProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@DateTimeProperty = ToDateTime(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id224_ListProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                if ((object)(o.@ListProperty) == null) o.@ListProperty = new global::System.Collections.Generic.List<global::System.String>();
                                global::System.Collections.Generic.List<global::System.String> a_3_0 = (global::System.Collections.Generic.List<global::System.String>)o.@ListProperty;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id136_string && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    if (ReadNull()) {
                                                        a_3_0.Add(null);
                                                    }
                                                    else {
                                                        a_3_0.Add(Reader.ReadElementString());
                                                    }
                                                    break;
                                                }
                                                UnknownNode(null, @":string");
                                            } while (false);
                                        }
                                        else {
                                            UnknownNode(null, @":string");
                                        }
                                        Reader.MoveToContent();
                                    }
                                ReadEndElement();
                                }
                            }
                            break;
                        }
                        UnknownNode((object)o, @":StringProperty, :IntProperty, :DateTimeProperty, :ListProperty");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":StringProperty, :IntProperty, :DateTimeProperty, :ListProperty");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWithDateTimePropertyAsXmlTime Read65_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id70_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithDateTimePropertyAsXmlTime o;
            o = new global::SerializationTypes.TypeWithDateTimePropertyAsXmlTime();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                string tmp = null;
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    UnknownNode((object)o, @"");
                }
                else if (Reader.NodeType == System.Xml.XmlNodeType.Text || 
                Reader.NodeType == System.Xml.XmlNodeType.CDATA || 
                Reader.NodeType == System.Xml.XmlNodeType.Whitespace || 
                Reader.NodeType == System.Xml.XmlNodeType.SignificantWhitespace) {
                    o.@Value = ToTime(Reader.ReadString());
                }
                else {
                    UnknownNode((object)o, @"");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWithByteArrayAsXmlText Read66_TypeWithByteArrayAsXmlText(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id71_TypeWithByteArrayAsXmlText && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithByteArrayAsXmlText o;
            o = new global::SerializationTypes.TypeWithByteArrayAsXmlText();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                string tmp = null;
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    UnknownNode((object)o, @"");
                }
                else if (Reader.NodeType == System.Xml.XmlNodeType.Text || 
                Reader.NodeType == System.Xml.XmlNodeType.CDATA || 
                Reader.NodeType == System.Xml.XmlNodeType.Whitespace || 
                Reader.NodeType == System.Xml.XmlNodeType.SignificantWhitespace) {
                    o.@Value = ToByteArrayBase64(Reader.ReadString());
                }
                else {
                    UnknownNode((object)o, @"");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.SimpleDC Read67_SimpleDC(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id72_SimpleDC && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.SimpleDC o;
            o = new global::SerializationTypes.SimpleDC();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id216_Data && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Data = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Data");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Data");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWithXmlTextAttributeOnArray Read68_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id73_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id74_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithXmlTextAttributeOnArray o;
            o = new global::SerializationTypes.TypeWithXmlTextAttributeOnArray();
            global::System.String[] a_0 = null;
            int ca_0 = 0;
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                o.@Text = (global::System.String[])ShrinkArray(a_0, ca_0, typeof(global::System.String), true);
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                string tmp = null;
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    UnknownNode((object)o, @"");
                }
                else if (Reader.NodeType == System.Xml.XmlNodeType.Text || 
                Reader.NodeType == System.Xml.XmlNodeType.CDATA || 
                Reader.NodeType == System.Xml.XmlNodeType.Whitespace || 
                Reader.NodeType == System.Xml.XmlNodeType.SignificantWhitespace) {
                    a_0 = (global::System.String[])EnsureArrayIndex(a_0, ca_0, typeof(global::System.String));a_0[ca_0++] = Reader.ReadString();
                }
                else {
                    UnknownNode((object)o, @"");
                }
                Reader.MoveToContent();
            }
            o.@Text = (global::System.String[])ShrinkArray(a_0, ca_0, typeof(global::System.String), true);
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.ClassImplementsInterface Read70_ClassImplementsInterface(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id76_ClassImplementsInterface && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.ClassImplementsInterface o;
            o = new global::SerializationTypes.ClassImplementsInterface();
            System.Span<bool> paramsRead = stackalloc bool[4];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id225_ClassID && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@ClassID = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id226_DisplayName && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@DisplayName = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id227_Id && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Id = Reader.ReadElementString();
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id228_IsLoaded && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@IsLoaded = System.Xml.XmlConvert.ToBoolean(Reader.ReadElementString());
                            }
                            paramsRead[3] = true;
                            break;
                        }
                        UnknownNode((object)o, @":ClassID, :DisplayName, :Id, :IsLoaded");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":ClassID, :DisplayName, :Id, :IsLoaded");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.SomeStruct Read71_SomeStruct(bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id78_SomeStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            global::SerializationTypes.SomeStruct o;
            try {
                o = (global::SerializationTypes.SomeStruct)System.Activator.CreateInstance(typeof(global::SerializationTypes.SomeStruct), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.CreateInstance | System.Reflection.BindingFlags.NonPublic, null, new object[0], null);
            }
            catch (System.MissingMethodException) {
                throw CreateInaccessibleConstructorException(@"global::SerializationTypes.SomeStruct");
            }
            catch (System.Security.SecurityException) {
                throw CreateCtorHasSecurityException(@"global::SerializationTypes.SomeStruct");
            }
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id124_A && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@A = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id125_B && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@B = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        UnknownNode((object)o, @":A, :B");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":A, :B");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.WithStruct Read72_WithStruct(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id77_WithStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.WithStruct o;
            o = new global::SerializationTypes.WithStruct();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id229_Some && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Some = Read71_SomeStruct(true);
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Some");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Some");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.WithEnums Read75_WithEnums(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id79_WithEnums && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.WithEnums o;
            o = new global::SerializationTypes.WithEnums();
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id230_Int && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Int = Read73_IntEnum(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id231_Short && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Short = Read74_ShortEnum(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Int, :Short");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Int, :Short");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.WithNullables Read79_WithNullables(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id80_WithNullables && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.WithNullables o;
            o = new global::SerializationTypes.WithNullables();
            System.Span<bool> paramsRead = stackalloc bool[6];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id232_Optional && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Optional = Read76_NullableOfIntEnum(true);
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id233_Optionull && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Optionull = Read76_NullableOfIntEnum(true);
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id234_OptionalInt && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@OptionalInt = Read77_NullableOfInt32(true);
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id235_OptionullInt && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@OptionullInt = Read77_NullableOfInt32(true);
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id236_Struct1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Struct1 = Read78_NullableOfSomeStruct(true);
                            paramsRead[4] = true;
                            break;
                        }
                        if (!paramsRead[5] && ((object) Reader.LocalName == (object)id237_Struct2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Struct2 = Read78_NullableOfSomeStruct(true);
                            paramsRead[5] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Optional, :Optionull, :OptionalInt, :OptionullInt, :Struct1, :Struct2");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Optional, :Optionull, :OptionalInt, :OptionullInt, :Struct1, :Struct2");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::System.Nullable<global::SerializationTypes.SomeStruct> Read78_NullableOfSomeStruct(bool checkType) {
            global::System.Nullable<global::SerializationTypes.SomeStruct> o = default(global::System.Nullable<global::SerializationTypes.SomeStruct>);
            if (ReadNull())
                return o;
            o = Read71_SomeStruct(true);
            return o;
        }

        global::System.Nullable<global::System.Int32> Read77_NullableOfInt32(bool checkType) {
            global::System.Nullable<global::System.Int32> o = default(global::System.Nullable<global::System.Int32>);
            if (ReadNull())
                return o;
            {
                o = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
            }
            return o;
        }

        global::System.Nullable<global::SerializationTypes.IntEnum> Read76_NullableOfIntEnum(bool checkType) {
            global::System.Nullable<global::SerializationTypes.IntEnum> o = default(global::System.Nullable<global::SerializationTypes.IntEnum>);
            if (ReadNull())
                return o;
            {
                o = Read73_IntEnum(Reader.ReadElementString());
            }
            return o;
        }

        global::SerializationTypes.XmlSerializerAttributes Read86_XmlSerializerAttributes(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id128_XmlSerializerAttributes && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.XmlSerializerAttributes o;
            o = new global::SerializationTypes.XmlSerializerAttributes();
            global::SerializationTypes.ItemChoiceType[] a_2 = null;
            int ca_2 = 0;
            global::System.Object[] a_7 = null;
            int ca_7 = 0;
            System.Span<bool> paramsRead = stackalloc bool[8];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[6] && ((object) Reader.LocalName == (object)id238_XmlAttributeName && (object) Reader.NamespaceURI == (object)id2_Item)) {
                    o.@XmlAttributeProperty = System.Xml.XmlConvert.ToInt32(Reader.Value);
                    paramsRead[6] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o, @":XmlAttributeName");
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                string tmp = null;
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id239_Word && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@MyChoice = Reader.ReadElementString();
                            }
                            o.@EnumType = global::SerializationTypes.ItemChoiceType.@Word;
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id240_Number && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@MyChoice = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            o.@EnumType = global::SerializationTypes.ItemChoiceType.@Number;
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id241_DecimalNumber && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@MyChoice = System.Xml.XmlConvert.ToDouble(Reader.ReadElementString());
                            }
                            o.@EnumType = global::SerializationTypes.ItemChoiceType.@DecimalNumber;
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id242_XmlIncludeProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@XmlIncludeProperty = Read1_Object(false, true);
                            paramsRead[1] = true;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id243_XmlEnumProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                global::SerializationTypes.ItemChoiceType[] a_2_0 = null;
                                int ca_2_0 = 0;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id89_ItemChoiceType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    {
                                                        a_2_0 = (global::SerializationTypes.ItemChoiceType[])EnsureArrayIndex(a_2_0, ca_2_0, typeof(global::SerializationTypes.ItemChoiceType));a_2_0[ca_2_0++] = Read85_ItemChoiceType(Reader.ReadElementString());
                                                    }
                                                    break;
                                                }
                                                UnknownNode(null, @":ItemChoiceType");
                                            } while (false);
                                        }
                                        else {
                                            UnknownNode(null, @":ItemChoiceType");
                                        }
                                        Reader.MoveToContent();
                                    }
                                ReadEndElement();
                                }
                                o.@XmlEnumProperty = (global::SerializationTypes.ItemChoiceType[])ShrinkArray(a_2_0, ca_2_0, typeof(global::SerializationTypes.ItemChoiceType), false);
                            }
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id244_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@XmlNamespaceDeclarationsProperty = Reader.ReadElementString();
                            }
                            paramsRead[4] = true;
                            break;
                        }
                        if (!paramsRead[5] && ((object) Reader.LocalName == (object)id245_XmlElementPropertyNode && (object) Reader.NamespaceURI == (object)id246_httpelement)) {
                            {
                                o.@XmlElementProperty = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[5] = true;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id247_CustomXmlArrayProperty && (object) Reader.NamespaceURI == (object)id147_httpmynamespace)) {
                            if (!ReadNull()) {
                                global::System.Object[] a_7_0 = null;
                                int ca_7_0 = 0;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id136_string && (object) Reader.NamespaceURI == (object)id147_httpmynamespace)) {
                                                    if (ReadNull()) {
                                                        a_7_0 = (global::System.Object[])EnsureArrayIndex(a_7_0, ca_7_0, typeof(global::System.Object));a_7_0[ca_7_0++] = null;
                                                    }
                                                    else {
                                                        a_7_0 = (global::System.Object[])EnsureArrayIndex(a_7_0, ca_7_0, typeof(global::System.Object));a_7_0[ca_7_0++] = Reader.ReadElementString();
                                                    }
                                                    break;
                                                }
                                                UnknownNode(null, @"http://mynamespace:string");
                                            } while (false);
                                        }
                                        else {
                                            UnknownNode(null, @"http://mynamespace:string");
                                        }
                                        Reader.MoveToContent();
                                    }
                                ReadEndElement();
                                }
                                o.@XmlArrayProperty = (global::System.Object[])ShrinkArray(a_7_0, ca_7_0, typeof(global::System.Object), false);
                            }
                            break;
                        }
                        UnknownNode((object)o, @":Word, :Number, :DecimalNumber, :XmlIncludeProperty, :XmlEnumProperty, :XmlNamespaceDeclarationsProperty, http://element:XmlElementPropertyNode, http://mynamespace:CustomXmlArrayProperty");
                    } while (false);
                }
                else if (Reader.NodeType == System.Xml.XmlNodeType.Text || 
                Reader.NodeType == System.Xml.XmlNodeType.CDATA || 
                Reader.NodeType == System.Xml.XmlNodeType.Whitespace || 
                Reader.NodeType == System.Xml.XmlNodeType.SignificantWhitespace) {
                    tmp = ReadString(tmp, false);
                    o.@XmlTextProperty = tmp;
                }
                else {
                    UnknownNode((object)o, @":Word, :Number, :DecimalNumber, :XmlIncludeProperty, :XmlEnumProperty, :XmlNamespaceDeclarationsProperty, http://element:XmlElementPropertyNode, http://mynamespace:CustomXmlArrayProperty");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWithAnyAttribute Read87_TypeWithAnyAttribute(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id90_TypeWithAnyAttribute && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithAnyAttribute o;
            o = new global::SerializationTypes.TypeWithAnyAttribute();
            global::System.Xml.XmlAttribute[] a_2 = null;
            int ca_2 = 0;
            System.Span<bool> paramsRead = stackalloc bool[3];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[1] && ((object) Reader.LocalName == (object)id222_IntProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                    o.@IntProperty = System.Xml.XmlConvert.ToInt32(Reader.Value);
                    paramsRead[1] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name)) {
                    System.Xml.XmlAttribute attr = (System.Xml.XmlAttribute) Document.ReadNode(Reader);
                    ParseWsdlArrayType(attr);
                    a_2 = (global::System.Xml.XmlAttribute[])EnsureArrayIndex(a_2, ca_2, typeof(global::System.Xml.XmlAttribute));a_2[ca_2++] = attr;
                }
            }
            o.@Attributes = (global::System.Xml.XmlAttribute[])ShrinkArray(a_2, ca_2, typeof(global::System.Xml.XmlAttribute), true);
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                o.@Attributes = (global::System.Xml.XmlAttribute[])ShrinkArray(a_2, ca_2, typeof(global::System.Xml.XmlAttribute), true);
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id122_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Name = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Name");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Name");
                }
                Reader.MoveToContent();
            }
            o.@Attributes = (global::System.Xml.XmlAttribute[])ShrinkArray(a_2, ca_2, typeof(global::System.Xml.XmlAttribute), true);
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.KnownTypesThroughConstructor Read88_KnownTypesThroughConstructor(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id91_KnownTypesThroughConstructor && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.KnownTypesThroughConstructor o;
            o = new global::SerializationTypes.KnownTypesThroughConstructor();
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id248_EnumValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@EnumValue = Read1_Object(false, true);
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id249_SimpleTypeValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@SimpleTypeValue = Read1_Object(false, true);
                            paramsRead[1] = true;
                            break;
                        }
                        UnknownNode((object)o, @":EnumValue, :SimpleTypeValue");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":EnumValue, :SimpleTypeValue");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.SimpleKnownTypeValue Read89_SimpleKnownTypeValue(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id92_SimpleKnownTypeValue && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.SimpleKnownTypeValue o;
            o = new global::SerializationTypes.SimpleKnownTypeValue();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id250_StrProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@StrProperty = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":StrProperty");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":StrProperty");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWithPropertyNameSpecified Read90_TypeWithPropertyNameSpecified(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id94_TypeWithPropertyNameSpecified && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithPropertyNameSpecified o;
            o = new global::SerializationTypes.TypeWithPropertyNameSpecified();
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id251_MyField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@MyFieldSpecified = true;
                            {
                                o.@MyField = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id252_MyFieldIgnored && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@MyFieldIgnoredSpecified = true;
                            {
                                o.@MyFieldIgnored = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        UnknownNode((object)o, @":MyField, :MyFieldIgnored");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":MyField, :MyFieldIgnored");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWithXmlSchemaFormAttribute Read91_TypeWithXmlSchemaFormAttribute(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id95_TypeWithXmlSchemaFormAttribute && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithXmlSchemaFormAttribute o;
            o = new global::SerializationTypes.TypeWithXmlSchemaFormAttribute();
            if ((object)(o.@UnqualifiedSchemaFormListProperty) == null) o.@UnqualifiedSchemaFormListProperty = new global::System.Collections.Generic.List<global::System.Int32>();
            global::System.Collections.Generic.List<global::System.Int32> a_0 = (global::System.Collections.Generic.List<global::System.Int32>)o.@UnqualifiedSchemaFormListProperty;
            if ((object)(o.@NoneSchemaFormListProperty) == null) o.@NoneSchemaFormListProperty = new global::System.Collections.Generic.List<global::System.String>();
            global::System.Collections.Generic.List<global::System.String> a_1 = (global::System.Collections.Generic.List<global::System.String>)o.@NoneSchemaFormListProperty;
            if ((object)(o.@QualifiedSchemaFormListProperty) == null) o.@QualifiedSchemaFormListProperty = new global::System.Collections.Generic.List<global::System.Boolean>();
            global::System.Collections.Generic.List<global::System.Boolean> a_2 = (global::System.Collections.Generic.List<global::System.Boolean>)o.@QualifiedSchemaFormListProperty;
            System.Span<bool> paramsRead = stackalloc bool[3];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (((object) Reader.LocalName == (object)id253_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                if ((object)(o.@UnqualifiedSchemaFormListProperty) == null) o.@UnqualifiedSchemaFormListProperty = new global::System.Collections.Generic.List<global::System.Int32>();
                                global::System.Collections.Generic.List<global::System.Int32> a_0_0 = (global::System.Collections.Generic.List<global::System.Int32>)o.@UnqualifiedSchemaFormListProperty;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id134_int && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    {
                                                        a_0_0.Add(System.Xml.XmlConvert.ToInt32(Reader.ReadElementString()));
                                                    }
                                                    break;
                                                }
                                                UnknownNode(null, @":int");
                                            } while (false);
                                        }
                                        else {
                                            UnknownNode(null, @":int");
                                        }
                                        Reader.MoveToContent();
                                    }
                                ReadEndElement();
                                }
                            }
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id254_NoneSchemaFormListProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                if ((object)(o.@NoneSchemaFormListProperty) == null) o.@NoneSchemaFormListProperty = new global::System.Collections.Generic.List<global::System.String>();
                                global::System.Collections.Generic.List<global::System.String> a_1_0 = (global::System.Collections.Generic.List<global::System.String>)o.@NoneSchemaFormListProperty;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id149_NoneParameter && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    {
                                                        a_1_0.Add(Reader.ReadElementString());
                                                    }
                                                    break;
                                                }
                                                UnknownNode(null, @":NoneParameter");
                                            } while (false);
                                        }
                                        else {
                                            UnknownNode(null, @":NoneParameter");
                                        }
                                        Reader.MoveToContent();
                                    }
                                ReadEndElement();
                                }
                            }
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id255_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                if ((object)(o.@QualifiedSchemaFormListProperty) == null) o.@QualifiedSchemaFormListProperty = new global::System.Collections.Generic.List<global::System.Boolean>();
                                global::System.Collections.Generic.List<global::System.Boolean> a_2_0 = (global::System.Collections.Generic.List<global::System.Boolean>)o.@QualifiedSchemaFormListProperty;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id151_QualifiedParameter && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    {
                                                        a_2_0.Add(System.Xml.XmlConvert.ToBoolean(Reader.ReadElementString()));
                                                    }
                                                    break;
                                                }
                                                UnknownNode(null, @":QualifiedParameter");
                                            } while (false);
                                        }
                                        else {
                                            UnknownNode(null, @":QualifiedParameter");
                                        }
                                        Reader.MoveToContent();
                                    }
                                ReadEndElement();
                                }
                            }
                            break;
                        }
                        UnknownNode((object)o, @":UnqualifiedSchemaFormListProperty, :NoneSchemaFormListProperty, :QualifiedSchemaFormListProperty");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":UnqualifiedSchemaFormListProperty, :NoneSchemaFormListProperty, :QualifiedSchemaFormListProperty");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWithTypeNameInXmlTypeAttribute Read92_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id96_MyXmlType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithTypeNameInXmlTypeAttribute o;
            o = new global::SerializationTypes.TypeWithTypeNameInXmlTypeAttribute();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id256_XmlAttributeForm && (object) Reader.NamespaceURI == (object)id2_Item)) {
                    o.@XmlAttributeForm = Reader.Value;
                    paramsRead[0] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o, @":XmlAttributeForm");
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    UnknownNode((object)o, @"");
                }
                else {
                    UnknownNode((object)o, @"");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.CustomElement Read94_CustomElement(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id99_CustomElement && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.CustomElement o;
            o = new global::SerializationTypes.CustomElement();
            global::System.Xml.XmlAttribute[] a_1 = null;
            int ca_1 = 0;
            global::System.Xml.XmlNode[] a_2 = null;
            int ca_2 = 0;
            System.Span<bool> paramsRead = stackalloc bool[3];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id257_name && (object) Reader.NamespaceURI == (object)id2_Item)) {
                    o.@Name = Reader.Value;
                    paramsRead[0] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name)) {
                    System.Xml.XmlAttribute attr = (System.Xml.XmlAttribute) Document.ReadNode(Reader);
                    ParseWsdlArrayType(attr);
                    if (attr is System.Xml.XmlAttribute) {
                        a_2 = (global::System.Xml.XmlNode[])EnsureArrayIndex(a_2, ca_2, typeof(global::System.Xml.XmlNode));a_2[ca_2++] = (System.Xml.XmlAttribute)attr;
                    }
                }
            }
            o.@Attributes = (global::System.Xml.XmlAttribute[])ShrinkArray(a_1, ca_1, typeof(global::System.Xml.XmlAttribute), true);
            o.@CustomAttributes = (global::System.Xml.XmlNode[])ShrinkArray(a_2, ca_2, typeof(global::System.Xml.XmlNode), true);
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                o.@Attributes = (global::System.Xml.XmlAttribute[])ShrinkArray(a_1, ca_1, typeof(global::System.Xml.XmlAttribute), true);
                o.@CustomAttributes = (global::System.Xml.XmlNode[])ShrinkArray(a_2, ca_2, typeof(global::System.Xml.XmlNode), true);
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    UnknownNode((object)o, @"");
                }
                else {
                    UnknownNode((object)o, @"");
                }
                Reader.MoveToContent();
            }
            o.@Attributes = (global::System.Xml.XmlAttribute[])ShrinkArray(a_1, ca_1, typeof(global::System.Xml.XmlAttribute), true);
            o.@CustomAttributes = (global::System.Xml.XmlNode[])ShrinkArray(a_2, ca_2, typeof(global::System.Xml.XmlNode), true);
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.CustomDocument Read95_CustomDocument(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id98_CustomDocument && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.CustomDocument o;
            o = new global::SerializationTypes.CustomDocument();
            if ((object)(o.@CustomItems) == null) o.@CustomItems = new global::System.Collections.Generic.List<global::SerializationTypes.CustomElement>();
            global::System.Collections.Generic.List<global::SerializationTypes.CustomElement> a_0 = (global::System.Collections.Generic.List<global::SerializationTypes.CustomElement>)o.@CustomItems;
            global::System.Xml.XmlNode[] a_1 = null;
            int ca_1 = 0;
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                o.@Items = (global::System.Xml.XmlNode[])ShrinkArray(a_1, ca_1, typeof(global::System.Xml.XmlNode), true);
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (((object) Reader.LocalName == (object)id258_customElement && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if ((object)(a_0) == null) Reader.Skip(); else a_0.Add(Read94_CustomElement(false, true));
                            break;
                        }
                        a_1 = (global::System.Xml.XmlNode[])EnsureArrayIndex(a_1, ca_1, typeof(global::System.Xml.XmlNode));a_1[ca_1++] = (global::System.Xml.XmlNode)ReadXmlNode(false);
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":customElement");
                }
                Reader.MoveToContent();
            }
            o.@Items = (global::System.Xml.XmlNode[])ShrinkArray(a_1, ca_1, typeof(global::System.Xml.XmlNode), true);
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWithNonPublicDefaultConstructor Read96_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id100_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithNonPublicDefaultConstructor o;
            try {
                o = (global::SerializationTypes.TypeWithNonPublicDefaultConstructor)System.Activator.CreateInstance(typeof(global::SerializationTypes.TypeWithNonPublicDefaultConstructor), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.CreateInstance | System.Reflection.BindingFlags.NonPublic, null, new object[0], null);
            }
            catch (System.MissingMethodException) {
                throw CreateInaccessibleConstructorException(@"global::SerializationTypes.TypeWithNonPublicDefaultConstructor");
            }
            catch (System.Security.SecurityException) {
                throw CreateCtorHasSecurityException(@"global::SerializationTypes.TypeWithNonPublicDefaultConstructor");
            }
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id122_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Name = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Name");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Name");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.ServerSettings Read97_ServerSettings(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id101_ServerSettings && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.ServerSettings o;
            o = new global::SerializationTypes.ServerSettings();
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id259_DS2Root && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@DS2Root = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id260_MetricConfigUrl && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@MetricConfigUrl = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        UnknownNode((object)o, @":DS2Root, :MetricConfigUrl");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":DS2Root, :MetricConfigUrl");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWithXmlQualifiedName Read98_TypeWithXmlQualifiedName(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id102_TypeWithXmlQualifiedName && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithXmlQualifiedName o;
            o = new global::SerializationTypes.TypeWithXmlQualifiedName();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id155_Value && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Value = ReadElementQualifiedName();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Value");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Value");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWith2DArrayProperty2 Read99_TypeWith2DArrayProperty2(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id103_TypeWith2DArrayProperty2 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWith2DArrayProperty2 o;
            o = new global::SerializationTypes.TypeWith2DArrayProperty2();
            global::SerializationTypes.SimpleType[][] a_0 = null;
            int ca_0 = 0;
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (((object) Reader.LocalName == (object)id261_TwoDArrayOfSimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                global::SerializationTypes.SimpleType[][] a_0_0 = null;
                                int ca_0_0 = 0;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id50_SimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    if (!ReadNull()) {
                                                        global::SerializationTypes.SimpleType[] a_0_0_0 = null;
                                                        int ca_0_0_0 = 0;
                                                        if ((Reader.IsEmptyElement)) {
                                                            Reader.Skip();
                                                        }
                                                        else {
                                                            Reader.ReadStartElement();
                                                            Reader.MoveToContent();
                                                            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                                                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                                                    do {
                                                                        if (((object) Reader.LocalName == (object)id50_SimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                                            a_0_0_0 = (global::SerializationTypes.SimpleType[])EnsureArrayIndex(a_0_0_0, ca_0_0_0, typeof(global::SerializationTypes.SimpleType));a_0_0_0[ca_0_0_0++] = Read47_SimpleType(true, true);
                                                                            break;
                                                                        }
                                                                        UnknownNode(null, @":SimpleType");
                                                                    } while (false);
                                                                }
                                                                else {
                                                                    UnknownNode(null, @":SimpleType");
                                                                }
                                                                Reader.MoveToContent();
                                                            }
                                                        ReadEndElement();
                                                        }
                                                        a_0_0 = (global::SerializationTypes.SimpleType[][])EnsureArrayIndex(a_0_0, ca_0_0, typeof(global::SerializationTypes.SimpleType[]));a_0_0[ca_0_0++] = (global::SerializationTypes.SimpleType[])ShrinkArray(a_0_0_0, ca_0_0_0, typeof(global::SerializationTypes.SimpleType), false);
                                                    }
                                                    break;
                                                }
                                                UnknownNode(null, @":SimpleType");
                                            } while (false);
                                        }
                                        else {
                                            UnknownNode(null, @":SimpleType");
                                        }
                                        Reader.MoveToContent();
                                    }
                                ReadEndElement();
                                }
                                o.@TwoDArrayOfSimpleType = (global::SerializationTypes.SimpleType[][])ShrinkArray(a_0_0, ca_0_0, typeof(global::SerializationTypes.SimpleType[]), false);
                            }
                            break;
                        }
                        UnknownNode((object)o, @":TwoDArrayOfSimpleType");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":TwoDArrayOfSimpleType");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWithPropertiesHavingDefaultValue Read100_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id104_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithPropertiesHavingDefaultValue o;
            o = new global::SerializationTypes.TypeWithPropertiesHavingDefaultValue();
            System.Span<bool> paramsRead = stackalloc bool[4];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id262_EmptyStringProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@EmptyStringProperty = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id221_StringProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@StringProperty = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id222_IntProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@IntProperty = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id263_CharProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@CharProperty = ToChar(Reader.ReadElementString());
                            }
                            paramsRead[3] = true;
                            break;
                        }
                        UnknownNode((object)o, @":EmptyStringProperty, :StringProperty, :IntProperty, :CharProperty");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":EmptyStringProperty, :StringProperty, :IntProperty, :CharProperty");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWithEnumPropertyHavingDefaultValue Read101_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id105_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithEnumPropertyHavingDefaultValue o;
            o = new global::SerializationTypes.TypeWithEnumPropertyHavingDefaultValue();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id264_EnumProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@EnumProperty = Read73_IntEnum(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":EnumProperty");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":EnumProperty");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue Read102_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id106_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue o;
            o = new global::SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id264_EnumProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@EnumProperty = Read69_EnumFlags(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":EnumProperty");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":EnumProperty");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWithShouldSerializeMethod Read103_TypeWithShouldSerializeMethod(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id107_TypeWithShouldSerializeMethod && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithShouldSerializeMethod o;
            o = new global::SerializationTypes.TypeWithShouldSerializeMethod();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id265_Foo && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Foo = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Foo");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Foo");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.KnownTypesThroughConstructorWithArrayProperties Read104_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id108_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.KnownTypesThroughConstructorWithArrayProperties o;
            o = new global::SerializationTypes.KnownTypesThroughConstructorWithArrayProperties();
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id266_StringArrayValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@StringArrayValue = Read1_Object(false, true);
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id267_IntArrayValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@IntArrayValue = Read1_Object(false, true);
                            paramsRead[1] = true;
                            break;
                        }
                        UnknownNode((object)o, @":StringArrayValue, :IntArrayValue");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":StringArrayValue, :IntArrayValue");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.KnownTypesThroughConstructorWithValue Read105_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id109_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.KnownTypesThroughConstructorWithValue o;
            o = new global::SerializationTypes.KnownTypesThroughConstructorWithValue();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id155_Value && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Value = Read1_Object(false, true);
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Value");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Value");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWithTypesHavingCustomFormatter Read106_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id110_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithTypesHavingCustomFormatter o;
            o = new global::SerializationTypes.TypeWithTypesHavingCustomFormatter();
            System.Span<bool> paramsRead = stackalloc bool[9];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id268_DateTimeContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@DateTimeContent = ToDateTime(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id269_QNameContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@QNameContent = ReadElementQualifiedName();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id270_DateContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@DateContent = ToDate(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id271_NameContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@NameContent = ToXmlName(Reader.ReadElementString());
                            }
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id272_NCNameContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@NCNameContent = ToXmlNCName(Reader.ReadElementString());
                            }
                            paramsRead[4] = true;
                            break;
                        }
                        if (!paramsRead[5] && ((object) Reader.LocalName == (object)id273_NMTOKENContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@NMTOKENContent = ToXmlNmToken(Reader.ReadElementString());
                            }
                            paramsRead[5] = true;
                            break;
                        }
                        if (!paramsRead[6] && ((object) Reader.LocalName == (object)id274_NMTOKENSContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@NMTOKENSContent = ToXmlNmTokens(Reader.ReadElementString());
                            }
                            paramsRead[6] = true;
                            break;
                        }
                        if (!paramsRead[7] && ((object) Reader.LocalName == (object)id275_Base64BinaryContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Base64BinaryContent = ToByteArrayBase64(false);
                            }
                            paramsRead[7] = true;
                            break;
                        }
                        if (!paramsRead[8] && ((object) Reader.LocalName == (object)id276_HexBinaryContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@HexBinaryContent = ToByteArrayHex(false);
                            }
                            paramsRead[8] = true;
                            break;
                        }
                        UnknownNode((object)o, @":DateTimeContent, :QNameContent, :DateContent, :NameContent, :NCNameContent, :NMTOKENContent, :NMTOKENSContent, :Base64BinaryContent, :HexBinaryContent");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":DateTimeContent, :QNameContent, :DateContent, :NameContent, :NCNameContent, :NMTOKENContent, :NMTOKENSContent, :Base64BinaryContent, :HexBinaryContent");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWithArrayPropertyHavingChoice Read108_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id111_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithArrayPropertyHavingChoice o;
            o = new global::SerializationTypes.TypeWithArrayPropertyHavingChoice();
            global::System.Object[] a_0 = null;
            int ca_0 = 0;
            global::SerializationTypes.MoreChoices[] choice_a_0 = null;
            int cchoice_a_0 = 0;
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                o.@ManyChoices = (global::System.Object[])ShrinkArray(a_0, ca_0, typeof(global::System.Object), true);
                o.@ChoiceArray = (global::SerializationTypes.MoreChoices[])ShrinkArray(choice_a_0, cchoice_a_0, typeof(global::SerializationTypes.MoreChoices), true);
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    do {
                        if (((object) Reader.LocalName == (object)id277_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                a_0 = (global::System.Object[])EnsureArrayIndex(a_0, ca_0, typeof(global::System.Object));a_0[ca_0++] = Reader.ReadElementString();
                            }
                            choice_a_0 = (global::SerializationTypes.MoreChoices[])EnsureArrayIndex(choice_a_0, cchoice_a_0, typeof(global::SerializationTypes.MoreChoices));choice_a_0[cchoice_a_0++] = global::SerializationTypes.MoreChoices.@Item;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id278_Amount && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                a_0 = (global::System.Object[])EnsureArrayIndex(a_0, ca_0, typeof(global::System.Object));a_0[ca_0++] = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            choice_a_0 = (global::SerializationTypes.MoreChoices[])EnsureArrayIndex(choice_a_0, cchoice_a_0, typeof(global::SerializationTypes.MoreChoices));choice_a_0[cchoice_a_0++] = global::SerializationTypes.MoreChoices.@Amount;
                            break;
                        }
                        UnknownNode((object)o, @":Item, :Amount");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Item, :Amount");
                }
                Reader.MoveToContent();
            }
            o.@ManyChoices = (global::System.Object[])ShrinkArray(a_0, ca_0, typeof(global::System.Object), true);
            o.@ChoiceArray = (global::SerializationTypes.MoreChoices[])ShrinkArray(choice_a_0, cchoice_a_0, typeof(global::SerializationTypes.MoreChoices), true);
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWithFieldsOrdered Read109_TypeWithFieldsOrdered(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id113_TypeWithFieldsOrdered && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithFieldsOrdered o;
            o = new global::SerializationTypes.TypeWithFieldsOrdered();
            System.Span<bool> paramsRead = stackalloc bool[4];
            while (Reader.MoveToNextAttribute()) {
                if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o);
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            int state = 0;
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    switch (state) {
                    case 0:
                        if (((object) Reader.LocalName == (object)id280_IntField2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@IntField2 = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                        }
                        state = 1;
                        break;
                    case 1:
                        if (((object) Reader.LocalName == (object)id279_IntField1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@IntField1 = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                        }
                        state = 2;
                        break;
                    case 2:
                        if (((object) Reader.LocalName == (object)id281_strfld && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@StringField2 = Reader.ReadElementString();
                            }
                        }
                        state = 3;
                        break;
                    case 3:
                        if (((object) Reader.LocalName == (object)id281_strfld && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@StringField1 = Reader.ReadElementString();
                            }
                        }
                        state = 4;
                        break;
                    default:
                        UnknownNode((object)o, null);
                        break;
                    }
                }
                else {
                    UnknownNode((object)o, null);
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.TypeWithSchemaFormInXmlAttribute Read93_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id2_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithSchemaFormInXmlAttribute o;
            o = new global::SerializationTypes.TypeWithSchemaFormInXmlAttribute();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id282_TestProperty && (object) Reader.NamespaceURI == (object)id283_httptestcom)) {
                    o.@TestProperty = Reader.Value;
                    paramsRead[0] = true;
                }
                else if (!IsXmlnsAttribute(Reader.Name)) {
                    UnknownNode((object)o, @"http://test.com:TestProperty");
                }
            }
            Reader.MoveToElement();
            if (Reader.IsEmptyElement) {
                Reader.Skip();
                return o;
            }
            Reader.ReadStartElement();
            Reader.MoveToContent();
            while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                    UnknownNode((object)o, @"");
                }
                else {
                    UnknownNode((object)o, @"");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        protected override void InitCallbacks() {
        }

        string id243_XmlEnumProperty;
        string id56_ArrayOfAnyType;
        string id245_XmlElementPropertyNode;
        string id193_ShipCost;
        string id79_WithEnums;
        string id197_Z;
        string id48_XElementArrayWrapper;
        string id124_A;
        string id155_Value;
        string id225_ClassID;
        string id7_Item;
        string id212_CurrentDateTime;
        string id141_ArrayOfParameter;
        string id105_Item;
        string id251_MyField;
        string id237_Struct2;
        string id12_DogBreed;
        string id267_IntArrayValue;
        string id74_Item;
        string id256_XmlAttributeForm;
        string id88_AttributeTesting;
        string id259_DS2Root;
        string id275_Base64BinaryContent;
        string id119_FirstName;
        string id160_ItemName;
        string id13_Group;
        string id53_StructNotSerializable;
        string id35_Trumpet;
        string id46_XElementWrapper;
        string id146_ArrayOfItemChoiceType;
        string id201_DoubleField;
        string id117_TypeClashA;
        string id223_DateTimeProperty;
        string id126_Value1;
        string id121_LastName;
        string id258_customElement;
        string id33_Instrument;
        string id255_Item;
        string id45_Parameter;
        string id174_ByteProperty;
        string id176_Breed;
        string id266_StringArrayValue;
        string id184_BoolValue;
        string id28_BaseClass1;
        string id44_RootClass;
        string id190_OrderDate;
        string id249_SimpleTypeValue;
        string id228_IsLoaded;
        string id80_WithNullables;
        string id233_Optionull;
        string id57_anyType;
        string id163_Quantity;
        string id139_ArrayOfInstrument;
        string id159_Modulation;
        string id58_MyEnum;
        string id77_WithStruct;
        string id55_Item;
        string id264_EnumProperty;
        string id265_Foo;
        string id200_Comment2;
        string id24_httpwwwcontoso1com;
        string id102_TypeWithXmlQualifiedName;
        string id99_CustomElement;
        string id34_Brass;
        string id209_xelement;
        string id111_Item;
        string id213_F1;
        string id272_NCNameContent;
        string id98_CustomDocument;
        string id281_strfld;
        string id63_TypeA;
        string id131_Item;
        string id109_Item;
        string id279_IntField1;
        string id181_value;
        string id185_Line1;
        string id123_ContainerType;
        string id90_TypeWithAnyAttribute;
        string id217_MyStruct;
        string id169_DefaultDTO;
        string id166_Base64Content;
        string id23_PurchaseOrder;
        string id8_TypeWithByteProperty;
        string id182_AttributeString;
        string id140_ArrayOfTypeWithLinkedProperty;
        string id161_Description;
        string id16_BaseClass;
        string id268_DateTimeContent;
        string id199_Instruments;
        string id78_SomeStruct;
        string id250_StrProperty;
        string id238_XmlAttributeName;
        string id145_ArrayOfTypeA;
        string id64_TypeB;
        string id42_Document;
        string id273_NMTOKENContent;
        string id191_Items;
        string id59_TypeWithEnumMembers;
        string id49_TypeWithDateTimeStringProperty;
        string id196_Y;
        string id231_Short;
        string id128_XmlSerializerAttributes;
        string id83_ShortEnum;
        string id151_QualifiedParameter;
        string id120_MiddleName;
        string id89_ItemChoiceType;
        string id26_OrderedItem;
        string id172_TimeSpanProperty;
        string id133_ArrayOfInt;
        string id219_ByteArray;
        string id252_MyFieldIgnored;
        string id202_SingleField;
        string id207_refs;
        string id113_TypeWithFieldsOrdered;
        string id39_Item;
        string id142_ArrayOfXElement;
        string id108_Item;
        string id278_Amount;
        string id112_MoreChoices;
        string id138_double;
        string id43_httpexamplecom;
        string id229_Some;
        string id20_BaseIXmlSerializable;
        string id110_Item;
        string id81_ByteEnum;
        string id198_Prop;
        string id82_SByteEnum;
        string id192_SubTotal;
        string id239_Word;
        string id17_DerivedClass;
        string id18_SimpleBaseClass;
        string id47_XElementStruct;
        string id205_IntValue;
        string id246_httpelement;
        string id76_ClassImplementsInterface;
        string id54_TypeWithMyCollectionField;
        string id85_UIntEnum;
        string id189_ShipTo;
        string id127_Value2;
        string id253_Item;
        string id62_BuiltInTypes;
        string id260_MetricConfigUrl;
        string id115_Root;
        string id283_httptestcom;
        string id107_TypeWithShouldSerializeMethod;
        string id274_NMTOKENSContent;
        string id271_NameContent;
        string id95_TypeWithXmlSchemaFormAttribute;
        string id66_Item;
        string id136_string;
        string id149_NoneParameter;
        string id214_F2;
        string id234_OptionalInt;
        string id242_XmlIncludeProperty;
        string id73_Item;
        string id257_name;
        string id31_dateTime;
        string id270_DateContent;
        string id240_Number;
        string id254_NoneSchemaFormListProperty;
        string id104_Item;
        string id41_TypeWithLinkedProperty;
        string id5_Item;
        string id179_GroupVehicle;
        string id27_AliasedTestType;
        string id215_Collection;
        string id168_DTO2;
        string id210_xelements;
        string id222_IntProperty;
        string id180_EmployeeName;
        string id22_DerivedIXmlSerializable;
        string id277_Item;
        string id232_Optional;
        string id276_HexBinaryContent;
        string id152_ArrayOfArrayOfSimpleType;
        string id144_ArrayOfSimpleType;
        string id93_Item;
        string id230_Int;
        string id97_Item;
        string id211_DateTimeString;
        string id280_IntField2;
        string id147_httpmynamespace;
        string id11_Dog;
        string id52_TypeWithGetOnlyArrayProperties;
        string id61_DCClassWithEnumAndStruct;
        string id175_Age;
        string id114_Item;
        string id71_TypeWithByteArrayAsXmlText;
        string id4_TypeWithBinaryProperty;
        string id118_Person;
        string id261_TwoDArrayOfSimpleType;
        string id32_Orchestra;
        string id220_Item;
        string id216_Data;
        string id60_DCStruct;
        string id148_ArrayOfString1;
        string id165_BinaryHexContent;
        string id91_KnownTypesThroughConstructor;
        string id224_ListProperty;
        string id195_X;
        string id6_TypeWithTimeSpanProperty;
        string id203_DoubleProp;
        string id106_Item;
        string id103_TypeWith2DArrayProperty2;
        string id156_Child;
        string id86_LongEnum;
        string id9_TypeWithXmlNodeArrayProperty;
        string id25_Address;
        string id162_UnitPrice;
        string id125_B;
        string id68_DerivedClassWithSameProperty;
        string id51_TypeWithGetSetArrayMembers;
        string id236_Struct1;
        string id150_ArrayOfBoolean;
        string id177_LicenseNumber;
        string id72_SimpleDC;
        string id94_TypeWithPropertyNameSpecified;
        string id96_MyXmlType;
        string id101_ServerSettings;
        string id226_DisplayName;
        string id19_SimpleDerivedClass;
        string id153_P1;
        string id183_DateTimeValue;
        string id154_P2;
        string id2_Item;
        string id269_QNameContent;
        string id1_TypeWithXmlElementProperty;
        string id3_TypeWithXmlDocumentProperty;
        string id263_CharProperty;
        string id178_GroupName;
        string id134_int;
        string id186_City;
        string id69_DerivedClassWithSameProperty2;
        string id75_EnumFlags;
        string id235_OptionullInt;
        string id158_IsValved;
        string id157_Children;
        string id92_SimpleKnownTypeValue;
        string id262_EmptyStringProperty;
        string id194_TotalCost;
        string id171_NullableDefaultDTO;
        string id100_Item;
        string id132_ArrayOfOrderedItem;
        string id143_XElement;
        string id15_Employee;
        string id187_State;
        string id173_TimeSpanProperty2;
        string id188_Zip;
        string id122_Name;
        string id241_DecimalNumber;
        string id221_StringProperty;
        string id204_FloatProp;
        string id218_MyEnum1;
        string id30_ArrayOfDateTime;
        string id65_TypeHasArrayOfASerializedAsB;
        string id87_ULongEnum;
        string id84_IntEnum;
        string id248_EnumValue;
        string id50_SimpleType;
        string id206_id;
        string id247_CustomXmlArrayProperty;
        string id38_Item;
        string id36_Pet;
        string id208_Parameters;
        string id67_BaseClassWithSamePropertyName;
        string id116_TypeClashB;
        string id21_Item;
        string id282_TestProperty;
        string id129_ParameterOfString;
        string id14_Vehicle;
        string id130_MsgDocumentType;
        string id10_Animal;
        string id29_DerivedClass1;
        string id135_ArrayOfString;
        string id170_NullableDTO;
        string id244_Item;
        string id70_Item;
        string id37_DefaultValuesSetToNaN;
        string id40_RootElement;
        string id227_Id;
        string id137_ArrayOfDouble;
        string id167_DTO;
        string id164_LineTotal;

        protected override void InitIDs() {
            id243_XmlEnumProperty = Reader.NameTable.Add(@"XmlEnumProperty");
            id56_ArrayOfAnyType = Reader.NameTable.Add(@"ArrayOfAnyType");
            id245_XmlElementPropertyNode = Reader.NameTable.Add(@"XmlElementPropertyNode");
            id193_ShipCost = Reader.NameTable.Add(@"ShipCost");
            id79_WithEnums = Reader.NameTable.Add(@"WithEnums");
            id197_Z = Reader.NameTable.Add(@"Z");
            id48_XElementArrayWrapper = Reader.NameTable.Add(@"XElementArrayWrapper");
            id124_A = Reader.NameTable.Add(@"A");
            id155_Value = Reader.NameTable.Add(@"Value");
            id225_ClassID = Reader.NameTable.Add(@"ClassID");
            id7_Item = Reader.NameTable.Add(@"TypeWithDefaultTimeSpanProperty");
            id212_CurrentDateTime = Reader.NameTable.Add(@"CurrentDateTime");
            id141_ArrayOfParameter = Reader.NameTable.Add(@"ArrayOfParameter");
            id105_Item = Reader.NameTable.Add(@"TypeWithEnumPropertyHavingDefaultValue");
            id251_MyField = Reader.NameTable.Add(@"MyField");
            id237_Struct2 = Reader.NameTable.Add(@"Struct2");
            id12_DogBreed = Reader.NameTable.Add(@"DogBreed");
            id267_IntArrayValue = Reader.NameTable.Add(@"IntArrayValue");
            id74_Item = Reader.NameTable.Add(@"http://schemas.xmlsoap.org/ws/2005/04/discovery");
            id256_XmlAttributeForm = Reader.NameTable.Add(@"XmlAttributeForm");
            id88_AttributeTesting = Reader.NameTable.Add(@"AttributeTesting");
            id259_DS2Root = Reader.NameTable.Add(@"DS2Root");
            id275_Base64BinaryContent = Reader.NameTable.Add(@"Base64BinaryContent");
            id119_FirstName = Reader.NameTable.Add(@"FirstName");
            id160_ItemName = Reader.NameTable.Add(@"ItemName");
            id13_Group = Reader.NameTable.Add(@"Group");
            id53_StructNotSerializable = Reader.NameTable.Add(@"StructNotSerializable");
            id35_Trumpet = Reader.NameTable.Add(@"Trumpet");
            id46_XElementWrapper = Reader.NameTable.Add(@"XElementWrapper");
            id146_ArrayOfItemChoiceType = Reader.NameTable.Add(@"ArrayOfItemChoiceType");
            id201_DoubleField = Reader.NameTable.Add(@"DoubleField");
            id117_TypeClashA = Reader.NameTable.Add(@"TypeClashA");
            id223_DateTimeProperty = Reader.NameTable.Add(@"DateTimeProperty");
            id126_Value1 = Reader.NameTable.Add(@"Value1");
            id121_LastName = Reader.NameTable.Add(@"LastName");
            id258_customElement = Reader.NameTable.Add(@"customElement");
            id33_Instrument = Reader.NameTable.Add(@"Instrument");
            id255_Item = Reader.NameTable.Add(@"QualifiedSchemaFormListProperty");
            id45_Parameter = Reader.NameTable.Add(@"Parameter");
            id174_ByteProperty = Reader.NameTable.Add(@"ByteProperty");
            id176_Breed = Reader.NameTable.Add(@"Breed");
            id266_StringArrayValue = Reader.NameTable.Add(@"StringArrayValue");
            id184_BoolValue = Reader.NameTable.Add(@"BoolValue");
            id28_BaseClass1 = Reader.NameTable.Add(@"BaseClass1");
            id44_RootClass = Reader.NameTable.Add(@"RootClass");
            id190_OrderDate = Reader.NameTable.Add(@"OrderDate");
            id249_SimpleTypeValue = Reader.NameTable.Add(@"SimpleTypeValue");
            id228_IsLoaded = Reader.NameTable.Add(@"IsLoaded");
            id80_WithNullables = Reader.NameTable.Add(@"WithNullables");
            id233_Optionull = Reader.NameTable.Add(@"Optionull");
            id57_anyType = Reader.NameTable.Add(@"anyType");
            id163_Quantity = Reader.NameTable.Add(@"Quantity");
            id139_ArrayOfInstrument = Reader.NameTable.Add(@"ArrayOfInstrument");
            id159_Modulation = Reader.NameTable.Add(@"Modulation");
            id58_MyEnum = Reader.NameTable.Add(@"MyEnum");
            id77_WithStruct = Reader.NameTable.Add(@"WithStruct");
            id55_Item = Reader.NameTable.Add(@"TypeWithReadOnlyMyCollectionProperty");
            id264_EnumProperty = Reader.NameTable.Add(@"EnumProperty");
            id265_Foo = Reader.NameTable.Add(@"Foo");
            id200_Comment2 = Reader.NameTable.Add(@"Comment2");
            id24_httpwwwcontoso1com = Reader.NameTable.Add(@"http://www.contoso1.com");
            id102_TypeWithXmlQualifiedName = Reader.NameTable.Add(@"TypeWithXmlQualifiedName");
            id99_CustomElement = Reader.NameTable.Add(@"CustomElement");
            id34_Brass = Reader.NameTable.Add(@"Brass");
            id209_xelement = Reader.NameTable.Add(@"xelement");
            id111_Item = Reader.NameTable.Add(@"TypeWithArrayPropertyHavingChoice");
            id213_F1 = Reader.NameTable.Add(@"F1");
            id272_NCNameContent = Reader.NameTable.Add(@"NCNameContent");
            id98_CustomDocument = Reader.NameTable.Add(@"CustomDocument");
            id281_strfld = Reader.NameTable.Add(@"strfld");
            id63_TypeA = Reader.NameTable.Add(@"TypeA");
            id131_Item = Reader.NameTable.Add(@"TypeWithMismatchBetweenAttributeAndPropertyType");
            id109_Item = Reader.NameTable.Add(@"KnownTypesThroughConstructorWithValue");
            id279_IntField1 = Reader.NameTable.Add(@"IntField1");
            id181_value = Reader.NameTable.Add(@"value");
            id185_Line1 = Reader.NameTable.Add(@"Line1");
            id123_ContainerType = Reader.NameTable.Add(@"ContainerType");
            id90_TypeWithAnyAttribute = Reader.NameTable.Add(@"TypeWithAnyAttribute");
            id217_MyStruct = Reader.NameTable.Add(@"MyStruct");
            id169_DefaultDTO = Reader.NameTable.Add(@"DefaultDTO");
            id166_Base64Content = Reader.NameTable.Add(@"Base64Content");
            id23_PurchaseOrder = Reader.NameTable.Add(@"PurchaseOrder");
            id8_TypeWithByteProperty = Reader.NameTable.Add(@"TypeWithByteProperty");
            id182_AttributeString = Reader.NameTable.Add(@"AttributeString");
            id140_ArrayOfTypeWithLinkedProperty = Reader.NameTable.Add(@"ArrayOfTypeWithLinkedProperty");
            id161_Description = Reader.NameTable.Add(@"Description");
            id16_BaseClass = Reader.NameTable.Add(@"BaseClass");
            id268_DateTimeContent = Reader.NameTable.Add(@"DateTimeContent");
            id199_Instruments = Reader.NameTable.Add(@"Instruments");
            id78_SomeStruct = Reader.NameTable.Add(@"SomeStruct");
            id250_StrProperty = Reader.NameTable.Add(@"StrProperty");
            id238_XmlAttributeName = Reader.NameTable.Add(@"XmlAttributeName");
            id145_ArrayOfTypeA = Reader.NameTable.Add(@"ArrayOfTypeA");
            id64_TypeB = Reader.NameTable.Add(@"TypeB");
            id42_Document = Reader.NameTable.Add(@"Document");
            id273_NMTOKENContent = Reader.NameTable.Add(@"NMTOKENContent");
            id191_Items = Reader.NameTable.Add(@"Items");
            id59_TypeWithEnumMembers = Reader.NameTable.Add(@"TypeWithEnumMembers");
            id49_TypeWithDateTimeStringProperty = Reader.NameTable.Add(@"TypeWithDateTimeStringProperty");
            id196_Y = Reader.NameTable.Add(@"Y");
            id231_Short = Reader.NameTable.Add(@"Short");
            id128_XmlSerializerAttributes = Reader.NameTable.Add(@"XmlSerializerAttributes");
            id83_ShortEnum = Reader.NameTable.Add(@"ShortEnum");
            id151_QualifiedParameter = Reader.NameTable.Add(@"QualifiedParameter");
            id120_MiddleName = Reader.NameTable.Add(@"MiddleName");
            id89_ItemChoiceType = Reader.NameTable.Add(@"ItemChoiceType");
            id26_OrderedItem = Reader.NameTable.Add(@"OrderedItem");
            id172_TimeSpanProperty = Reader.NameTable.Add(@"TimeSpanProperty");
            id133_ArrayOfInt = Reader.NameTable.Add(@"ArrayOfInt");
            id219_ByteArray = Reader.NameTable.Add(@"ByteArray");
            id252_MyFieldIgnored = Reader.NameTable.Add(@"MyFieldIgnored");
            id202_SingleField = Reader.NameTable.Add(@"SingleField");
            id207_refs = Reader.NameTable.Add(@"refs");
            id113_TypeWithFieldsOrdered = Reader.NameTable.Add(@"TypeWithFieldsOrdered");
            id39_Item = Reader.NameTable.Add(@"DefaultValuesSetToNegativeInfinity");
            id142_ArrayOfXElement = Reader.NameTable.Add(@"ArrayOfXElement");
            id108_Item = Reader.NameTable.Add(@"KnownTypesThroughConstructorWithArrayProperties");
            id278_Amount = Reader.NameTable.Add(@"Amount");
            id112_MoreChoices = Reader.NameTable.Add(@"MoreChoices");
            id138_double = Reader.NameTable.Add(@"double");
            id43_httpexamplecom = Reader.NameTable.Add(@"http://example.com");
            id229_Some = Reader.NameTable.Add(@"Some");
            id20_BaseIXmlSerializable = Reader.NameTable.Add(@"BaseIXmlSerializable");
            id110_Item = Reader.NameTable.Add(@"TypeWithTypesHavingCustomFormatter");
            id81_ByteEnum = Reader.NameTable.Add(@"ByteEnum");
            id198_Prop = Reader.NameTable.Add(@"Prop");
            id82_SByteEnum = Reader.NameTable.Add(@"SByteEnum");
            id192_SubTotal = Reader.NameTable.Add(@"SubTotal");
            id239_Word = Reader.NameTable.Add(@"Word");
            id17_DerivedClass = Reader.NameTable.Add(@"DerivedClass");
            id18_SimpleBaseClass = Reader.NameTable.Add(@"SimpleBaseClass");
            id47_XElementStruct = Reader.NameTable.Add(@"XElementStruct");
            id205_IntValue = Reader.NameTable.Add(@"IntValue");
            id246_httpelement = Reader.NameTable.Add(@"http://element");
            id76_ClassImplementsInterface = Reader.NameTable.Add(@"ClassImplementsInterface");
            id54_TypeWithMyCollectionField = Reader.NameTable.Add(@"TypeWithMyCollectionField");
            id85_UIntEnum = Reader.NameTable.Add(@"UIntEnum");
            id189_ShipTo = Reader.NameTable.Add(@"ShipTo");
            id127_Value2 = Reader.NameTable.Add(@"Value2");
            id253_Item = Reader.NameTable.Add(@"UnqualifiedSchemaFormListProperty");
            id62_BuiltInTypes = Reader.NameTable.Add(@"BuiltInTypes");
            id260_MetricConfigUrl = Reader.NameTable.Add(@"MetricConfigUrl");
            id115_Root = Reader.NameTable.Add(@"Root");
            id283_httptestcom = Reader.NameTable.Add(@"http://test.com");
            id107_TypeWithShouldSerializeMethod = Reader.NameTable.Add(@"TypeWithShouldSerializeMethod");
            id274_NMTOKENSContent = Reader.NameTable.Add(@"NMTOKENSContent");
            id271_NameContent = Reader.NameTable.Add(@"NameContent");
            id95_TypeWithXmlSchemaFormAttribute = Reader.NameTable.Add(@"TypeWithXmlSchemaFormAttribute");
            id66_Item = Reader.NameTable.Add(@"__TypeNameWithSpecialCharacters漢ñ");
            id136_string = Reader.NameTable.Add(@"string");
            id149_NoneParameter = Reader.NameTable.Add(@"NoneParameter");
            id214_F2 = Reader.NameTable.Add(@"F2");
            id234_OptionalInt = Reader.NameTable.Add(@"OptionalInt");
            id242_XmlIncludeProperty = Reader.NameTable.Add(@"XmlIncludeProperty");
            id73_Item = Reader.NameTable.Add(@"TypeWithXmlTextAttributeOnArray");
            id257_name = Reader.NameTable.Add(@"name");
            id31_dateTime = Reader.NameTable.Add(@"dateTime");
            id270_DateContent = Reader.NameTable.Add(@"DateContent");
            id240_Number = Reader.NameTable.Add(@"Number");
            id254_NoneSchemaFormListProperty = Reader.NameTable.Add(@"NoneSchemaFormListProperty");
            id104_Item = Reader.NameTable.Add(@"TypeWithPropertiesHavingDefaultValue");
            id41_TypeWithLinkedProperty = Reader.NameTable.Add(@"TypeWithLinkedProperty");
            id5_Item = Reader.NameTable.Add(@"TypeWithDateTimeOffsetProperties");
            id179_GroupVehicle = Reader.NameTable.Add(@"GroupVehicle");
            id27_AliasedTestType = Reader.NameTable.Add(@"AliasedTestType");
            id215_Collection = Reader.NameTable.Add(@"Collection");
            id168_DTO2 = Reader.NameTable.Add(@"DTO2");
            id210_xelements = Reader.NameTable.Add(@"xelements");
            id222_IntProperty = Reader.NameTable.Add(@"IntProperty");
            id180_EmployeeName = Reader.NameTable.Add(@"EmployeeName");
            id22_DerivedIXmlSerializable = Reader.NameTable.Add(@"DerivedIXmlSerializable");
            id277_Item = Reader.NameTable.Add(@"Item");
            id232_Optional = Reader.NameTable.Add(@"Optional");
            id276_HexBinaryContent = Reader.NameTable.Add(@"HexBinaryContent");
            id152_ArrayOfArrayOfSimpleType = Reader.NameTable.Add(@"ArrayOfArrayOfSimpleType");
            id144_ArrayOfSimpleType = Reader.NameTable.Add(@"ArrayOfSimpleType");
            id93_Item = Reader.NameTable.Add(@"ClassImplementingIXmlSerializable");
            id230_Int = Reader.NameTable.Add(@"Int");
            id97_Item = Reader.NameTable.Add(@"TypeWithSchemaFormInXmlAttribute");
            id211_DateTimeString = Reader.NameTable.Add(@"DateTimeString");
            id280_IntField2 = Reader.NameTable.Add(@"IntField2");
            id147_httpmynamespace = Reader.NameTable.Add(@"http://mynamespace");
            id11_Dog = Reader.NameTable.Add(@"Dog");
            id52_TypeWithGetOnlyArrayProperties = Reader.NameTable.Add(@"TypeWithGetOnlyArrayProperties");
            id61_DCClassWithEnumAndStruct = Reader.NameTable.Add(@"DCClassWithEnumAndStruct");
            id175_Age = Reader.NameTable.Add(@"Age");
            id114_Item = Reader.NameTable.Add(@"TypeWithKnownTypesOfCollectionsWithConflictingXmlName");
            id71_TypeWithByteArrayAsXmlText = Reader.NameTable.Add(@"TypeWithByteArrayAsXmlText");
            id4_TypeWithBinaryProperty = Reader.NameTable.Add(@"TypeWithBinaryProperty");
            id118_Person = Reader.NameTable.Add(@"Person");
            id261_TwoDArrayOfSimpleType = Reader.NameTable.Add(@"TwoDArrayOfSimpleType");
            id32_Orchestra = Reader.NameTable.Add(@"Orchestra");
            id220_Item = Reader.NameTable.Add(@"PropertyNameWithSpecialCharacters漢ñ");
            id216_Data = Reader.NameTable.Add(@"Data");
            id60_DCStruct = Reader.NameTable.Add(@"DCStruct");
            id148_ArrayOfString1 = Reader.NameTable.Add(@"ArrayOfString1");
            id165_BinaryHexContent = Reader.NameTable.Add(@"BinaryHexContent");
            id91_KnownTypesThroughConstructor = Reader.NameTable.Add(@"KnownTypesThroughConstructor");
            id224_ListProperty = Reader.NameTable.Add(@"ListProperty");
            id195_X = Reader.NameTable.Add(@"X");
            id6_TypeWithTimeSpanProperty = Reader.NameTable.Add(@"TypeWithTimeSpanProperty");
            id203_DoubleProp = Reader.NameTable.Add(@"DoubleProp");
            id106_Item = Reader.NameTable.Add(@"TypeWithEnumFlagPropertyHavingDefaultValue");
            id103_TypeWith2DArrayProperty2 = Reader.NameTable.Add(@"TypeWith2DArrayProperty2");
            id156_Child = Reader.NameTable.Add(@"Child");
            id86_LongEnum = Reader.NameTable.Add(@"LongEnum");
            id9_TypeWithXmlNodeArrayProperty = Reader.NameTable.Add(@"TypeWithXmlNodeArrayProperty");
            id25_Address = Reader.NameTable.Add(@"Address");
            id162_UnitPrice = Reader.NameTable.Add(@"UnitPrice");
            id125_B = Reader.NameTable.Add(@"B");
            id68_DerivedClassWithSameProperty = Reader.NameTable.Add(@"DerivedClassWithSameProperty");
            id51_TypeWithGetSetArrayMembers = Reader.NameTable.Add(@"TypeWithGetSetArrayMembers");
            id236_Struct1 = Reader.NameTable.Add(@"Struct1");
            id150_ArrayOfBoolean = Reader.NameTable.Add(@"ArrayOfBoolean");
            id177_LicenseNumber = Reader.NameTable.Add(@"LicenseNumber");
            id72_SimpleDC = Reader.NameTable.Add(@"SimpleDC");
            id94_TypeWithPropertyNameSpecified = Reader.NameTable.Add(@"TypeWithPropertyNameSpecified");
            id96_MyXmlType = Reader.NameTable.Add(@"MyXmlType");
            id101_ServerSettings = Reader.NameTable.Add(@"ServerSettings");
            id226_DisplayName = Reader.NameTable.Add(@"DisplayName");
            id19_SimpleDerivedClass = Reader.NameTable.Add(@"SimpleDerivedClass");
            id153_P1 = Reader.NameTable.Add(@"P1");
            id183_DateTimeValue = Reader.NameTable.Add(@"DateTimeValue");
            id154_P2 = Reader.NameTable.Add(@"P2");
            id2_Item = Reader.NameTable.Add(@"");
            id269_QNameContent = Reader.NameTable.Add(@"QNameContent");
            id1_TypeWithXmlElementProperty = Reader.NameTable.Add(@"TypeWithXmlElementProperty");
            id3_TypeWithXmlDocumentProperty = Reader.NameTable.Add(@"TypeWithXmlDocumentProperty");
            id263_CharProperty = Reader.NameTable.Add(@"CharProperty");
            id178_GroupName = Reader.NameTable.Add(@"GroupName");
            id134_int = Reader.NameTable.Add(@"int");
            id186_City = Reader.NameTable.Add(@"City");
            id69_DerivedClassWithSameProperty2 = Reader.NameTable.Add(@"DerivedClassWithSameProperty2");
            id75_EnumFlags = Reader.NameTable.Add(@"EnumFlags");
            id235_OptionullInt = Reader.NameTable.Add(@"OptionullInt");
            id158_IsValved = Reader.NameTable.Add(@"IsValved");
            id157_Children = Reader.NameTable.Add(@"Children");
            id92_SimpleKnownTypeValue = Reader.NameTable.Add(@"SimpleKnownTypeValue");
            id262_EmptyStringProperty = Reader.NameTable.Add(@"EmptyStringProperty");
            id194_TotalCost = Reader.NameTable.Add(@"TotalCost");
            id171_NullableDefaultDTO = Reader.NameTable.Add(@"NullableDefaultDTO");
            id100_Item = Reader.NameTable.Add(@"TypeWithNonPublicDefaultConstructor");
            id132_ArrayOfOrderedItem = Reader.NameTable.Add(@"ArrayOfOrderedItem");
            id143_XElement = Reader.NameTable.Add(@"XElement");
            id15_Employee = Reader.NameTable.Add(@"Employee");
            id187_State = Reader.NameTable.Add(@"State");
            id173_TimeSpanProperty2 = Reader.NameTable.Add(@"TimeSpanProperty2");
            id188_Zip = Reader.NameTable.Add(@"Zip");
            id122_Name = Reader.NameTable.Add(@"Name");
            id241_DecimalNumber = Reader.NameTable.Add(@"DecimalNumber");
            id221_StringProperty = Reader.NameTable.Add(@"StringProperty");
            id204_FloatProp = Reader.NameTable.Add(@"FloatProp");
            id218_MyEnum1 = Reader.NameTable.Add(@"MyEnum1");
            id30_ArrayOfDateTime = Reader.NameTable.Add(@"ArrayOfDateTime");
            id65_TypeHasArrayOfASerializedAsB = Reader.NameTable.Add(@"TypeHasArrayOfASerializedAsB");
            id87_ULongEnum = Reader.NameTable.Add(@"ULongEnum");
            id84_IntEnum = Reader.NameTable.Add(@"IntEnum");
            id248_EnumValue = Reader.NameTable.Add(@"EnumValue");
            id50_SimpleType = Reader.NameTable.Add(@"SimpleType");
            id206_id = Reader.NameTable.Add(@"id");
            id247_CustomXmlArrayProperty = Reader.NameTable.Add(@"CustomXmlArrayProperty");
            id38_Item = Reader.NameTable.Add(@"DefaultValuesSetToPositiveInfinity");
            id36_Pet = Reader.NameTable.Add(@"Pet");
            id208_Parameters = Reader.NameTable.Add(@"Parameters");
            id67_BaseClassWithSamePropertyName = Reader.NameTable.Add(@"BaseClassWithSamePropertyName");
            id116_TypeClashB = Reader.NameTable.Add(@"TypeClashB");
            id21_Item = Reader.NameTable.Add(@"http://example.com/serializer-test-namespace");
            id282_TestProperty = Reader.NameTable.Add(@"TestProperty");
            id129_ParameterOfString = Reader.NameTable.Add(@"ParameterOfString");
            id14_Vehicle = Reader.NameTable.Add(@"Vehicle");
            id130_MsgDocumentType = Reader.NameTable.Add(@"MsgDocumentType");
            id10_Animal = Reader.NameTable.Add(@"Animal");
            id29_DerivedClass1 = Reader.NameTable.Add(@"DerivedClass1");
            id135_ArrayOfString = Reader.NameTable.Add(@"ArrayOfString");
            id170_NullableDTO = Reader.NameTable.Add(@"NullableDTO");
            id244_Item = Reader.NameTable.Add(@"XmlNamespaceDeclarationsProperty");
            id70_Item = Reader.NameTable.Add(@"TypeWithDateTimePropertyAsXmlTime");
            id37_DefaultValuesSetToNaN = Reader.NameTable.Add(@"DefaultValuesSetToNaN");
            id40_RootElement = Reader.NameTable.Add(@"RootElement");
            id227_Id = Reader.NameTable.Add(@"Id");
            id137_ArrayOfDouble = Reader.NameTable.Add(@"ArrayOfDouble");
            id167_DTO = Reader.NameTable.Add(@"DTO");
            id164_LineTotal = Reader.NameTable.Add(@"LineTotal");
        }
    }

    public abstract class XmlSerializer1 : System.Xml.Serialization.XmlSerializer {
        protected override System.Xml.Serialization.XmlSerializationReader CreateReader() {
            return new XmlSerializationReader1();
        }
        protected override System.Xml.Serialization.XmlSerializationWriter CreateWriter() {
            return new XmlSerializationWriter1();
        }
    }

    public sealed class TypeWithXmlElementPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithXmlElementProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write111_TypeWithXmlElementProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read115_TypeWithXmlElementProperty();
        }
    }

    public sealed class TypeWithXmlDocumentPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithXmlDocumentProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write112_TypeWithXmlDocumentProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read116_TypeWithXmlDocumentProperty();
        }
    }

    public sealed class TypeWithBinaryPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithBinaryProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write113_TypeWithBinaryProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read117_TypeWithBinaryProperty();
        }
    }

    public sealed class TypeWithDateTimeOffsetPropertiesSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithDateTimeOffsetProperties", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write114_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read118_Item();
        }
    }

    public sealed class TypeWithTimeSpanPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithTimeSpanProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write115_TypeWithTimeSpanProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read119_TypeWithTimeSpanProperty();
        }
    }

    public sealed class TypeWithDefaultTimeSpanPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithDefaultTimeSpanProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write116_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read120_Item();
        }
    }

    public sealed class TypeWithBytePropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithByteProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write117_TypeWithByteProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read121_TypeWithByteProperty();
        }
    }

    public sealed class TypeWithXmlNodeArrayPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithXmlNodeArrayProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write118_TypeWithXmlNodeArrayProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read122_TypeWithXmlNodeArrayProperty();
        }
    }

    public sealed class AnimalSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Animal", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write119_Animal(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read123_Animal();
        }
    }

    public sealed class DogSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Dog", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write120_Dog(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read124_Dog();
        }
    }

    public sealed class DogBreedSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DogBreed", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write121_DogBreed(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read125_DogBreed();
        }
    }

    public sealed class GroupSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Group", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write122_Group(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read126_Group();
        }
    }

    public sealed class VehicleSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Vehicle", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write123_Vehicle(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read127_Vehicle();
        }
    }

    public sealed class EmployeeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Employee", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write124_Employee(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read128_Employee();
        }
    }

    public sealed class BaseClassSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"BaseClass", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write125_BaseClass(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read129_BaseClass();
        }
    }

    public sealed class DerivedClassSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DerivedClass", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write126_DerivedClass(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read130_DerivedClass();
        }
    }

    public sealed class SimpleBaseClassSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"SimpleBaseClass", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write127_SimpleBaseClass(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read131_SimpleBaseClass();
        }
    }

    public sealed class SimpleDerivedClassSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"SimpleDerivedClass", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write128_SimpleDerivedClass(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read132_SimpleDerivedClass();
        }
    }

    public sealed class XmlSerializableBaseClassSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"BaseIXmlSerializable", @"http://example.com/serializer-test-namespace");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write129_BaseIXmlSerializable(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read133_BaseIXmlSerializable();
        }
    }

    public sealed class XmlSerializableDerivedClassSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DerivedIXmlSerializable", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write130_DerivedIXmlSerializable(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read134_DerivedIXmlSerializable();
        }
    }

    public sealed class PurchaseOrderSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"PurchaseOrder", @"http://www.contoso1.com");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write131_PurchaseOrder(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read135_PurchaseOrder();
        }
    }

    public sealed class AddressSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Address", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write132_Address(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read136_Address();
        }
    }

    public sealed class OrderedItemSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"OrderedItem", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write133_OrderedItem(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read137_OrderedItem();
        }
    }

    public sealed class AliasedTestTypeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"AliasedTestType", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write134_AliasedTestType(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read138_AliasedTestType();
        }
    }

    public sealed class BaseClass1Serializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"BaseClass1", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write135_BaseClass1(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read139_BaseClass1();
        }
    }

    public sealed class DerivedClass1Serializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DerivedClass1", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write136_DerivedClass1(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read140_DerivedClass1();
        }
    }

    public sealed class MyCollection1Serializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ArrayOfDateTime", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write137_ArrayOfDateTime(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read141_ArrayOfDateTime();
        }
    }

    public sealed class OrchestraSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Orchestra", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write138_Orchestra(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read142_Orchestra();
        }
    }

    public sealed class InstrumentSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Instrument", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write139_Instrument(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read143_Instrument();
        }
    }

    public sealed class BrassSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Brass", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write140_Brass(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read144_Brass();
        }
    }

    public sealed class TrumpetSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Trumpet", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write141_Trumpet(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read145_Trumpet();
        }
    }

    public sealed class PetSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Pet", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write142_Pet(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read146_Pet();
        }
    }

    public sealed class DefaultValuesSetToNaNSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DefaultValuesSetToNaN", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write143_DefaultValuesSetToNaN(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read147_DefaultValuesSetToNaN();
        }
    }

    public sealed class DefaultValuesSetToPositiveInfinitySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DefaultValuesSetToPositiveInfinity", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write144_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read148_Item();
        }
    }

    public sealed class DefaultValuesSetToNegativeInfinitySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DefaultValuesSetToNegativeInfinity", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write145_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read149_Item();
        }
    }

    public sealed class TypeWithMismatchBetweenAttributeAndPropertyTypeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"RootElement", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write146_RootElement(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read150_RootElement();
        }
    }

    public sealed class TypeWithLinkedPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithLinkedProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write147_TypeWithLinkedProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read151_TypeWithLinkedProperty();
        }
    }

    public sealed class MsgDocumentTypeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Document", @"http://example.com");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write148_Document(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read152_Document();
        }
    }

    public sealed class RootClassSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"RootClass", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write149_RootClass(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read153_RootClass();
        }
    }

    public sealed class ParameterSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Parameter", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write150_Parameter(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read154_Parameter();
        }
    }

    public sealed class XElementWrapperSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"XElementWrapper", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write151_XElementWrapper(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read155_XElementWrapper();
        }
    }

    public sealed class XElementStructSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"XElementStruct", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write152_XElementStruct(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read156_XElementStruct();
        }
    }

    public sealed class XElementArrayWrapperSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"XElementArrayWrapper", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write153_XElementArrayWrapper(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read157_XElementArrayWrapper();
        }
    }

    public sealed class TypeWithDateTimeStringPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithDateTimeStringProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write154_TypeWithDateTimeStringProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read158_TypeWithDateTimeStringProperty();
        }
    }

    public sealed class SimpleTypeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"SimpleType", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write155_SimpleType(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read159_SimpleType();
        }
    }

    public sealed class TypeWithGetSetArrayMembersSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithGetSetArrayMembers", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write156_TypeWithGetSetArrayMembers(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read160_TypeWithGetSetArrayMembers();
        }
    }

    public sealed class TypeWithGetOnlyArrayPropertiesSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithGetOnlyArrayProperties", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write157_TypeWithGetOnlyArrayProperties(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read161_TypeWithGetOnlyArrayProperties();
        }
    }

    public sealed class StructNotSerializableSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"StructNotSerializable", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write158_StructNotSerializable(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read162_StructNotSerializable();
        }
    }

    public sealed class TypeWithMyCollectionFieldSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithMyCollectionField", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write159_TypeWithMyCollectionField(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read163_TypeWithMyCollectionField();
        }
    }

    public sealed class TypeWithReadOnlyMyCollectionPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithReadOnlyMyCollectionProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write160_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read164_Item();
        }
    }

    public sealed class MyListSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ArrayOfAnyType", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write161_ArrayOfAnyType(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read165_ArrayOfAnyType();
        }
    }

    public sealed class MyEnumSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"MyEnum", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write162_MyEnum(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read166_MyEnum();
        }
    }

    public sealed class TypeWithEnumMembersSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithEnumMembers", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write163_TypeWithEnumMembers(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read167_TypeWithEnumMembers();
        }
    }

    public sealed class DCStructSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DCStruct", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write164_DCStruct(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read168_DCStruct();
        }
    }

    public sealed class DCClassWithEnumAndStructSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DCClassWithEnumAndStruct", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write165_DCClassWithEnumAndStruct(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read169_DCClassWithEnumAndStruct();
        }
    }

    public sealed class BuiltInTypesSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"BuiltInTypes", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write166_BuiltInTypes(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read170_BuiltInTypes();
        }
    }

    public sealed class TypeASerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeA", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write167_TypeA(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read171_TypeA();
        }
    }

    public sealed class TypeBSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeB", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write168_TypeB(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read172_TypeB();
        }
    }

    public sealed class TypeHasArrayOfASerializedAsBSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeHasArrayOfASerializedAsB", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write169_TypeHasArrayOfASerializedAsB(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read173_TypeHasArrayOfASerializedAsB();
        }
    }

    public sealed class @__TypeNameWithSpecialCharacters漢ñSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"__TypeNameWithSpecialCharacters漢ñ", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write170_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read174_Item();
        }
    }

    public sealed class BaseClassWithSamePropertyNameSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"BaseClassWithSamePropertyName", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write171_BaseClassWithSamePropertyName(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read175_BaseClassWithSamePropertyName();
        }
    }

    public sealed class DerivedClassWithSamePropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DerivedClassWithSameProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write172_DerivedClassWithSameProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read176_DerivedClassWithSameProperty();
        }
    }

    public sealed class DerivedClassWithSameProperty2Serializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DerivedClassWithSameProperty2", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write173_DerivedClassWithSameProperty2(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read177_DerivedClassWithSameProperty2();
        }
    }

    public sealed class TypeWithDateTimePropertyAsXmlTimeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithDateTimePropertyAsXmlTime", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write174_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read178_Item();
        }
    }

    public sealed class TypeWithByteArrayAsXmlTextSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithByteArrayAsXmlText", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write175_TypeWithByteArrayAsXmlText(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read179_TypeWithByteArrayAsXmlText();
        }
    }

    public sealed class SimpleDCSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"SimpleDC", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write176_SimpleDC(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read180_SimpleDC();
        }
    }

    public sealed class TypeWithXmlTextAttributeOnArraySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithXmlTextAttributeOnArray", @"http://schemas.xmlsoap.org/ws/2005/04/discovery");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write177_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read181_Item();
        }
    }

    public sealed class EnumFlagsSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"EnumFlags", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write178_EnumFlags(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read182_EnumFlags();
        }
    }

    public sealed class ClassImplementsInterfaceSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ClassImplementsInterface", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write179_ClassImplementsInterface(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read183_ClassImplementsInterface();
        }
    }

    public sealed class WithStructSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"WithStruct", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write180_WithStruct(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read184_WithStruct();
        }
    }

    public sealed class SomeStructSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"SomeStruct", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write181_SomeStruct(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read185_SomeStruct();
        }
    }

    public sealed class WithEnumsSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"WithEnums", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write182_WithEnums(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read186_WithEnums();
        }
    }

    public sealed class WithNullablesSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"WithNullables", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write183_WithNullables(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read187_WithNullables();
        }
    }

    public sealed class ByteEnumSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ByteEnum", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write184_ByteEnum(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read188_ByteEnum();
        }
    }

    public sealed class SByteEnumSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"SByteEnum", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write185_SByteEnum(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read189_SByteEnum();
        }
    }

    public sealed class ShortEnumSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ShortEnum", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write186_ShortEnum(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read190_ShortEnum();
        }
    }

    public sealed class IntEnumSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"IntEnum", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write187_IntEnum(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read191_IntEnum();
        }
    }

    public sealed class UIntEnumSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"UIntEnum", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write188_UIntEnum(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read192_UIntEnum();
        }
    }

    public sealed class LongEnumSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"LongEnum", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write189_LongEnum(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read193_LongEnum();
        }
    }

    public sealed class ULongEnumSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ULongEnum", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write190_ULongEnum(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read194_ULongEnum();
        }
    }

    public sealed class XmlSerializerAttributesSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"AttributeTesting", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write191_AttributeTesting(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read195_AttributeTesting();
        }
    }

    public sealed class ItemChoiceTypeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ItemChoiceType", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write192_ItemChoiceType(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read196_ItemChoiceType();
        }
    }

    public sealed class TypeWithAnyAttributeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithAnyAttribute", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write193_TypeWithAnyAttribute(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read197_TypeWithAnyAttribute();
        }
    }

    public sealed class KnownTypesThroughConstructorSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"KnownTypesThroughConstructor", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write194_KnownTypesThroughConstructor(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read198_KnownTypesThroughConstructor();
        }
    }

    public sealed class SimpleKnownTypeValueSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"SimpleKnownTypeValue", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write195_SimpleKnownTypeValue(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read199_SimpleKnownTypeValue();
        }
    }

    public sealed class ClassImplementingIXmlSerializableSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ClassImplementingIXmlSerializable", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write196_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read200_Item();
        }
    }

    public sealed class TypeWithPropertyNameSpecifiedSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithPropertyNameSpecified", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write197_TypeWithPropertyNameSpecified(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read201_TypeWithPropertyNameSpecified();
        }
    }

    public sealed class TypeWithXmlSchemaFormAttributeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithXmlSchemaFormAttribute", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write198_TypeWithXmlSchemaFormAttribute(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read202_TypeWithXmlSchemaFormAttribute();
        }
    }

    public sealed class TypeWithTypeNameInXmlTypeAttributeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"MyXmlType", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write199_MyXmlType(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read203_MyXmlType();
        }
    }

    public sealed class TypeWithSchemaFormInXmlAttributeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithSchemaFormInXmlAttribute", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write200_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read204_Item();
        }
    }

    public sealed class CustomDocumentSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"CustomDocument", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write201_CustomDocument(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read205_CustomDocument();
        }
    }

    public sealed class CustomElementSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"CustomElement", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write202_CustomElement(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read206_CustomElement();
        }
    }

    public sealed class CustomAttributeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return true;
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write203_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read207_Item();
        }
    }

    public sealed class TypeWithNonPublicDefaultConstructorSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithNonPublicDefaultConstructor", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write204_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read208_Item();
        }
    }

    public sealed class ServerSettingsSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ServerSettings", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write205_ServerSettings(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read209_ServerSettings();
        }
    }

    public sealed class TypeWithXmlQualifiedNameSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithXmlQualifiedName", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write206_TypeWithXmlQualifiedName(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read210_TypeWithXmlQualifiedName();
        }
    }

    public sealed class TypeWith2DArrayProperty2Serializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWith2DArrayProperty2", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write207_TypeWith2DArrayProperty2(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read211_TypeWith2DArrayProperty2();
        }
    }

    public sealed class TypeWithPropertiesHavingDefaultValueSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithPropertiesHavingDefaultValue", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write208_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read212_Item();
        }
    }

    public sealed class TypeWithEnumPropertyHavingDefaultValueSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithEnumPropertyHavingDefaultValue", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write209_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read213_Item();
        }
    }

    public sealed class TypeWithEnumFlagPropertyHavingDefaultValueSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithEnumFlagPropertyHavingDefaultValue", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write210_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read214_Item();
        }
    }

    public sealed class TypeWithShouldSerializeMethodSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithShouldSerializeMethod", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write211_TypeWithShouldSerializeMethod(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read215_TypeWithShouldSerializeMethod();
        }
    }

    public sealed class KnownTypesThroughConstructorWithArrayPropertiesSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"KnownTypesThroughConstructorWithArrayProperties", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write212_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read216_Item();
        }
    }

    public sealed class KnownTypesThroughConstructorWithValueSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"KnownTypesThroughConstructorWithValue", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write213_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read217_Item();
        }
    }

    public sealed class TypeWithTypesHavingCustomFormatterSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithTypesHavingCustomFormatter", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write214_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read218_Item();
        }
    }

    public sealed class TypeWithArrayPropertyHavingChoiceSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithArrayPropertyHavingChoice", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write215_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read219_Item();
        }
    }

    public sealed class MoreChoicesSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"MoreChoices", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write216_MoreChoices(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read220_MoreChoices();
        }
    }

    public sealed class TypeWithFieldsOrderedSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithFieldsOrdered", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write217_TypeWithFieldsOrdered(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read221_TypeWithFieldsOrdered();
        }
    }

    public sealed class TypeWithKnownTypesOfCollectionsWithConflictingXmlNameSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithKnownTypesOfCollectionsWithConflictingXmlName", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write218_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read222_Item();
        }
    }

    public sealed class NamespaceTypeNameClashContainerSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Root", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write219_Root(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read223_Root();
        }
    }

    public sealed class TypeNameClashSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeClashB", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write220_TypeClashB(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read224_TypeClashB();
        }
    }

    public sealed class TypeNameClashSerializer1 : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeClashA", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write221_TypeClashA(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read225_TypeClashA();
        }
    }

    public sealed class PersonSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Person", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write222_Person(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read226_Person();
        }
    }

    public class XmlSerializerContract : global::System.Xml.Serialization.XmlSerializerImplementation {
        public override global::System.Xml.Serialization.XmlSerializationReader Reader { get { return new XmlSerializationReader1(); } }
        public override global::System.Xml.Serialization.XmlSerializationWriter Writer { get { return new XmlSerializationWriter1(); } }
        System.Collections.Hashtable readMethods = null;
        public override System.Collections.Hashtable ReadMethods {
            get {
                if (readMethods == null) {
                    System.Collections.Hashtable _tmp = new System.Collections.Hashtable();
                    _tmp[@"TypeWithXmlElementProperty::"] = @"Read115_TypeWithXmlElementProperty";
                    _tmp[@"TypeWithXmlDocumentProperty::"] = @"Read116_TypeWithXmlDocumentProperty";
                    _tmp[@"TypeWithBinaryProperty::"] = @"Read117_TypeWithBinaryProperty";
                    _tmp[@"TypeWithDateTimeOffsetProperties::"] = @"Read118_Item";
                    _tmp[@"TypeWithTimeSpanProperty::"] = @"Read119_TypeWithTimeSpanProperty";
                    _tmp[@"TypeWithDefaultTimeSpanProperty::"] = @"Read120_Item";
                    _tmp[@"TypeWithByteProperty::"] = @"Read121_TypeWithByteProperty";
                    _tmp[@"TypeWithXmlNodeArrayProperty:::True:"] = @"Read122_TypeWithXmlNodeArrayProperty";
                    _tmp[@"Animal::"] = @"Read123_Animal";
                    _tmp[@"Dog::"] = @"Read124_Dog";
                    _tmp[@"DogBreed::"] = @"Read125_DogBreed";
                    _tmp[@"Group::"] = @"Read126_Group";
                    _tmp[@"Vehicle::"] = @"Read127_Vehicle";
                    _tmp[@"Employee::"] = @"Read128_Employee";
                    _tmp[@"BaseClass::"] = @"Read129_BaseClass";
                    _tmp[@"DerivedClass::"] = @"Read130_DerivedClass";
                    _tmp[@"SimpleBaseClass::"] = @"Read131_SimpleBaseClass";
                    _tmp[@"SimpleDerivedClass::"] = @"Read132_SimpleDerivedClass";
                    _tmp[@"XmlSerializableBaseClass:http://example.com/serializer-test-namespace::True:"] = @"Read133_BaseIXmlSerializable";
                    _tmp[@"XmlSerializableDerivedClass::"] = @"Read134_DerivedIXmlSerializable";
                    _tmp[@"PurchaseOrder:http://www.contoso1.com:PurchaseOrder:False:"] = @"Read135_PurchaseOrder";
                    _tmp[@"Address::"] = @"Read136_Address";
                    _tmp[@"OrderedItem::"] = @"Read137_OrderedItem";
                    _tmp[@"AliasedTestType::"] = @"Read138_AliasedTestType";
                    _tmp[@"BaseClass1::"] = @"Read139_BaseClass1";
                    _tmp[@"DerivedClass1::"] = @"Read140_DerivedClass1";
                    _tmp[@"MyCollection1::"] = @"Read141_ArrayOfDateTime";
                    _tmp[@"Orchestra::"] = @"Read142_Orchestra";
                    _tmp[@"Instrument::"] = @"Read143_Instrument";
                    _tmp[@"Brass::"] = @"Read144_Brass";
                    _tmp[@"Trumpet::"] = @"Read145_Trumpet";
                    _tmp[@"Pet::"] = @"Read146_Pet";
                    _tmp[@"DefaultValuesSetToNaN::"] = @"Read147_DefaultValuesSetToNaN";
                    _tmp[@"DefaultValuesSetToPositiveInfinity::"] = @"Read148_Item";
                    _tmp[@"DefaultValuesSetToNegativeInfinity::"] = @"Read149_Item";
                    _tmp[@"TypeWithMismatchBetweenAttributeAndPropertyType::RootElement:True:"] = @"Read150_RootElement";
                    _tmp[@"TypeWithLinkedProperty::"] = @"Read151_TypeWithLinkedProperty";
                    _tmp[@"MsgDocumentType:http://example.com:Document:True:"] = @"Read152_Document";
                    _tmp[@"RootClass::"] = @"Read153_RootClass";
                    _tmp[@"Parameter::"] = @"Read154_Parameter";
                    _tmp[@"XElementWrapper::"] = @"Read155_XElementWrapper";
                    _tmp[@"XElementStruct::"] = @"Read156_XElementStruct";
                    _tmp[@"XElementArrayWrapper::"] = @"Read157_XElementArrayWrapper";
                    _tmp[@"SerializationTypes.TypeWithDateTimeStringProperty::"] = @"Read158_TypeWithDateTimeStringProperty";
                    _tmp[@"SerializationTypes.SimpleType::"] = @"Read159_SimpleType";
                    _tmp[@"SerializationTypes.TypeWithGetSetArrayMembers::"] = @"Read160_TypeWithGetSetArrayMembers";
                    _tmp[@"SerializationTypes.TypeWithGetOnlyArrayProperties::"] = @"Read161_TypeWithGetOnlyArrayProperties";
                    _tmp[@"SerializationTypes.StructNotSerializable::"] = @"Read162_StructNotSerializable";
                    _tmp[@"SerializationTypes.TypeWithMyCollectionField::"] = @"Read163_TypeWithMyCollectionField";
                    _tmp[@"SerializationTypes.TypeWithReadOnlyMyCollectionProperty::"] = @"Read164_Item";
                    _tmp[@"SerializationTypes.MyList::"] = @"Read165_ArrayOfAnyType";
                    _tmp[@"SerializationTypes.MyEnum::"] = @"Read166_MyEnum";
                    _tmp[@"SerializationTypes.TypeWithEnumMembers::"] = @"Read167_TypeWithEnumMembers";
                    _tmp[@"SerializationTypes.DCStruct::"] = @"Read168_DCStruct";
                    _tmp[@"SerializationTypes.DCClassWithEnumAndStruct::"] = @"Read169_DCClassWithEnumAndStruct";
                    _tmp[@"SerializationTypes.BuiltInTypes::"] = @"Read170_BuiltInTypes";
                    _tmp[@"SerializationTypes.TypeA::"] = @"Read171_TypeA";
                    _tmp[@"SerializationTypes.TypeB::"] = @"Read172_TypeB";
                    _tmp[@"SerializationTypes.TypeHasArrayOfASerializedAsB::"] = @"Read173_TypeHasArrayOfASerializedAsB";
                    _tmp[@"SerializationTypes.__TypeNameWithSpecialCharacters漢ñ::"] = @"Read174_Item";
                    _tmp[@"SerializationTypes.BaseClassWithSamePropertyName::"] = @"Read175_BaseClassWithSamePropertyName";
                    _tmp[@"SerializationTypes.DerivedClassWithSameProperty::"] = @"Read176_DerivedClassWithSameProperty";
                    _tmp[@"SerializationTypes.DerivedClassWithSameProperty2::"] = @"Read177_DerivedClassWithSameProperty2";
                    _tmp[@"SerializationTypes.TypeWithDateTimePropertyAsXmlTime::"] = @"Read178_Item";
                    _tmp[@"SerializationTypes.TypeWithByteArrayAsXmlText::"] = @"Read179_TypeWithByteArrayAsXmlText";
                    _tmp[@"SerializationTypes.SimpleDC::"] = @"Read180_SimpleDC";
                    _tmp[@"SerializationTypes.TypeWithXmlTextAttributeOnArray:http://schemas.xmlsoap.org/ws/2005/04/discovery::False:"] = @"Read181_Item";
                    _tmp[@"SerializationTypes.EnumFlags::"] = @"Read182_EnumFlags";
                    _tmp[@"SerializationTypes.ClassImplementsInterface::"] = @"Read183_ClassImplementsInterface";
                    _tmp[@"SerializationTypes.WithStruct::"] = @"Read184_WithStruct";
                    _tmp[@"SerializationTypes.SomeStruct::"] = @"Read185_SomeStruct";
                    _tmp[@"SerializationTypes.WithEnums::"] = @"Read186_WithEnums";
                    _tmp[@"SerializationTypes.WithNullables::"] = @"Read187_WithNullables";
                    _tmp[@"SerializationTypes.ByteEnum::"] = @"Read188_ByteEnum";
                    _tmp[@"SerializationTypes.SByteEnum::"] = @"Read189_SByteEnum";
                    _tmp[@"SerializationTypes.ShortEnum::"] = @"Read190_ShortEnum";
                    _tmp[@"SerializationTypes.IntEnum::"] = @"Read191_IntEnum";
                    _tmp[@"SerializationTypes.UIntEnum::"] = @"Read192_UIntEnum";
                    _tmp[@"SerializationTypes.LongEnum::"] = @"Read193_LongEnum";
                    _tmp[@"SerializationTypes.ULongEnum::"] = @"Read194_ULongEnum";
                    _tmp[@"SerializationTypes.XmlSerializerAttributes::AttributeTesting:False:"] = @"Read195_AttributeTesting";
                    _tmp[@"SerializationTypes.ItemChoiceType::"] = @"Read196_ItemChoiceType";
                    _tmp[@"SerializationTypes.TypeWithAnyAttribute::"] = @"Read197_TypeWithAnyAttribute";
                    _tmp[@"SerializationTypes.KnownTypesThroughConstructor::"] = @"Read198_KnownTypesThroughConstructor";
                    _tmp[@"SerializationTypes.SimpleKnownTypeValue::"] = @"Read199_SimpleKnownTypeValue";
                    _tmp[@"SerializationTypes.ClassImplementingIXmlSerializable::"] = @"Read200_Item";
                    _tmp[@"SerializationTypes.TypeWithPropertyNameSpecified::"] = @"Read201_TypeWithPropertyNameSpecified";
                    _tmp[@"SerializationTypes.TypeWithXmlSchemaFormAttribute:::True:"] = @"Read202_TypeWithXmlSchemaFormAttribute";
                    _tmp[@"SerializationTypes.TypeWithTypeNameInXmlTypeAttribute::"] = @"Read203_MyXmlType";
                    _tmp[@"SerializationTypes.TypeWithSchemaFormInXmlAttribute::"] = @"Read204_Item";
                    _tmp[@"SerializationTypes.CustomDocument::"] = @"Read205_CustomDocument";
                    _tmp[@"SerializationTypes.CustomElement::"] = @"Read206_CustomElement";
                    _tmp[@"SerializationTypes.CustomAttribute::"] = @"Read207_Item";
                    _tmp[@"SerializationTypes.TypeWithNonPublicDefaultConstructor::"] = @"Read208_Item";
                    _tmp[@"SerializationTypes.ServerSettings::"] = @"Read209_ServerSettings";
                    _tmp[@"SerializationTypes.TypeWithXmlQualifiedName::"] = @"Read210_TypeWithXmlQualifiedName";
                    _tmp[@"SerializationTypes.TypeWith2DArrayProperty2::"] = @"Read211_TypeWith2DArrayProperty2";
                    _tmp[@"SerializationTypes.TypeWithPropertiesHavingDefaultValue::"] = @"Read212_Item";
                    _tmp[@"SerializationTypes.TypeWithEnumPropertyHavingDefaultValue::"] = @"Read213_Item";
                    _tmp[@"SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue::"] = @"Read214_Item";
                    _tmp[@"SerializationTypes.TypeWithShouldSerializeMethod::"] = @"Read215_TypeWithShouldSerializeMethod";
                    _tmp[@"SerializationTypes.KnownTypesThroughConstructorWithArrayProperties::"] = @"Read216_Item";
                    _tmp[@"SerializationTypes.KnownTypesThroughConstructorWithValue::"] = @"Read217_Item";
                    _tmp[@"SerializationTypes.TypeWithTypesHavingCustomFormatter::"] = @"Read218_Item";
                    _tmp[@"SerializationTypes.TypeWithArrayPropertyHavingChoice::"] = @"Read219_Item";
                    _tmp[@"SerializationTypes.MoreChoices::"] = @"Read220_MoreChoices";
                    _tmp[@"SerializationTypes.TypeWithFieldsOrdered::"] = @"Read221_TypeWithFieldsOrdered";
                    _tmp[@"SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName::"] = @"Read222_Item";
                    _tmp[@"SerializationTypes.NamespaceTypeNameClashContainer::Root:True:"] = @"Read223_Root";
                    _tmp[@"SerializationTypes.TypeNameClashB.TypeNameClash::"] = @"Read224_TypeClashB";
                    _tmp[@"SerializationTypes.TypeNameClashA.TypeNameClash::"] = @"Read225_TypeClashA";
                    _tmp[@"Outer+Person::"] = @"Read226_Person";
                    if (readMethods == null) readMethods = _tmp;
                }
                return readMethods;
            }
        }
        System.Collections.Hashtable writeMethods = null;
        public override System.Collections.Hashtable WriteMethods {
            get {
                if (writeMethods == null) {
                    System.Collections.Hashtable _tmp = new System.Collections.Hashtable();
                    _tmp[@"TypeWithXmlElementProperty::"] = @"Write111_TypeWithXmlElementProperty";
                    _tmp[@"TypeWithXmlDocumentProperty::"] = @"Write112_TypeWithXmlDocumentProperty";
                    _tmp[@"TypeWithBinaryProperty::"] = @"Write113_TypeWithBinaryProperty";
                    _tmp[@"TypeWithDateTimeOffsetProperties::"] = @"Write114_Item";
                    _tmp[@"TypeWithTimeSpanProperty::"] = @"Write115_TypeWithTimeSpanProperty";
                    _tmp[@"TypeWithDefaultTimeSpanProperty::"] = @"Write116_Item";
                    _tmp[@"TypeWithByteProperty::"] = @"Write117_TypeWithByteProperty";
                    _tmp[@"TypeWithXmlNodeArrayProperty:::True:"] = @"Write118_TypeWithXmlNodeArrayProperty";
                    _tmp[@"Animal::"] = @"Write119_Animal";
                    _tmp[@"Dog::"] = @"Write120_Dog";
                    _tmp[@"DogBreed::"] = @"Write121_DogBreed";
                    _tmp[@"Group::"] = @"Write122_Group";
                    _tmp[@"Vehicle::"] = @"Write123_Vehicle";
                    _tmp[@"Employee::"] = @"Write124_Employee";
                    _tmp[@"BaseClass::"] = @"Write125_BaseClass";
                    _tmp[@"DerivedClass::"] = @"Write126_DerivedClass";
                    _tmp[@"SimpleBaseClass::"] = @"Write127_SimpleBaseClass";
                    _tmp[@"SimpleDerivedClass::"] = @"Write128_SimpleDerivedClass";
                    _tmp[@"XmlSerializableBaseClass:http://example.com/serializer-test-namespace::True:"] = @"Write129_BaseIXmlSerializable";
                    _tmp[@"XmlSerializableDerivedClass::"] = @"Write130_DerivedIXmlSerializable";
                    _tmp[@"PurchaseOrder:http://www.contoso1.com:PurchaseOrder:False:"] = @"Write131_PurchaseOrder";
                    _tmp[@"Address::"] = @"Write132_Address";
                    _tmp[@"OrderedItem::"] = @"Write133_OrderedItem";
                    _tmp[@"AliasedTestType::"] = @"Write134_AliasedTestType";
                    _tmp[@"BaseClass1::"] = @"Write135_BaseClass1";
                    _tmp[@"DerivedClass1::"] = @"Write136_DerivedClass1";
                    _tmp[@"MyCollection1::"] = @"Write137_ArrayOfDateTime";
                    _tmp[@"Orchestra::"] = @"Write138_Orchestra";
                    _tmp[@"Instrument::"] = @"Write139_Instrument";
                    _tmp[@"Brass::"] = @"Write140_Brass";
                    _tmp[@"Trumpet::"] = @"Write141_Trumpet";
                    _tmp[@"Pet::"] = @"Write142_Pet";
                    _tmp[@"DefaultValuesSetToNaN::"] = @"Write143_DefaultValuesSetToNaN";
                    _tmp[@"DefaultValuesSetToPositiveInfinity::"] = @"Write144_Item";
                    _tmp[@"DefaultValuesSetToNegativeInfinity::"] = @"Write145_Item";
                    _tmp[@"TypeWithMismatchBetweenAttributeAndPropertyType::RootElement:True:"] = @"Write146_RootElement";
                    _tmp[@"TypeWithLinkedProperty::"] = @"Write147_TypeWithLinkedProperty";
                    _tmp[@"MsgDocumentType:http://example.com:Document:True:"] = @"Write148_Document";
                    _tmp[@"RootClass::"] = @"Write149_RootClass";
                    _tmp[@"Parameter::"] = @"Write150_Parameter";
                    _tmp[@"XElementWrapper::"] = @"Write151_XElementWrapper";
                    _tmp[@"XElementStruct::"] = @"Write152_XElementStruct";
                    _tmp[@"XElementArrayWrapper::"] = @"Write153_XElementArrayWrapper";
                    _tmp[@"SerializationTypes.TypeWithDateTimeStringProperty::"] = @"Write154_TypeWithDateTimeStringProperty";
                    _tmp[@"SerializationTypes.SimpleType::"] = @"Write155_SimpleType";
                    _tmp[@"SerializationTypes.TypeWithGetSetArrayMembers::"] = @"Write156_TypeWithGetSetArrayMembers";
                    _tmp[@"SerializationTypes.TypeWithGetOnlyArrayProperties::"] = @"Write157_TypeWithGetOnlyArrayProperties";
                    _tmp[@"SerializationTypes.StructNotSerializable::"] = @"Write158_StructNotSerializable";
                    _tmp[@"SerializationTypes.TypeWithMyCollectionField::"] = @"Write159_TypeWithMyCollectionField";
                    _tmp[@"SerializationTypes.TypeWithReadOnlyMyCollectionProperty::"] = @"Write160_Item";
                    _tmp[@"SerializationTypes.MyList::"] = @"Write161_ArrayOfAnyType";
                    _tmp[@"SerializationTypes.MyEnum::"] = @"Write162_MyEnum";
                    _tmp[@"SerializationTypes.TypeWithEnumMembers::"] = @"Write163_TypeWithEnumMembers";
                    _tmp[@"SerializationTypes.DCStruct::"] = @"Write164_DCStruct";
                    _tmp[@"SerializationTypes.DCClassWithEnumAndStruct::"] = @"Write165_DCClassWithEnumAndStruct";
                    _tmp[@"SerializationTypes.BuiltInTypes::"] = @"Write166_BuiltInTypes";
                    _tmp[@"SerializationTypes.TypeA::"] = @"Write167_TypeA";
                    _tmp[@"SerializationTypes.TypeB::"] = @"Write168_TypeB";
                    _tmp[@"SerializationTypes.TypeHasArrayOfASerializedAsB::"] = @"Write169_TypeHasArrayOfASerializedAsB";
                    _tmp[@"SerializationTypes.__TypeNameWithSpecialCharacters漢ñ::"] = @"Write170_Item";
                    _tmp[@"SerializationTypes.BaseClassWithSamePropertyName::"] = @"Write171_BaseClassWithSamePropertyName";
                    _tmp[@"SerializationTypes.DerivedClassWithSameProperty::"] = @"Write172_DerivedClassWithSameProperty";
                    _tmp[@"SerializationTypes.DerivedClassWithSameProperty2::"] = @"Write173_DerivedClassWithSameProperty2";
                    _tmp[@"SerializationTypes.TypeWithDateTimePropertyAsXmlTime::"] = @"Write174_Item";
                    _tmp[@"SerializationTypes.TypeWithByteArrayAsXmlText::"] = @"Write175_TypeWithByteArrayAsXmlText";
                    _tmp[@"SerializationTypes.SimpleDC::"] = @"Write176_SimpleDC";
                    _tmp[@"SerializationTypes.TypeWithXmlTextAttributeOnArray:http://schemas.xmlsoap.org/ws/2005/04/discovery::False:"] = @"Write177_Item";
                    _tmp[@"SerializationTypes.EnumFlags::"] = @"Write178_EnumFlags";
                    _tmp[@"SerializationTypes.ClassImplementsInterface::"] = @"Write179_ClassImplementsInterface";
                    _tmp[@"SerializationTypes.WithStruct::"] = @"Write180_WithStruct";
                    _tmp[@"SerializationTypes.SomeStruct::"] = @"Write181_SomeStruct";
                    _tmp[@"SerializationTypes.WithEnums::"] = @"Write182_WithEnums";
                    _tmp[@"SerializationTypes.WithNullables::"] = @"Write183_WithNullables";
                    _tmp[@"SerializationTypes.ByteEnum::"] = @"Write184_ByteEnum";
                    _tmp[@"SerializationTypes.SByteEnum::"] = @"Write185_SByteEnum";
                    _tmp[@"SerializationTypes.ShortEnum::"] = @"Write186_ShortEnum";
                    _tmp[@"SerializationTypes.IntEnum::"] = @"Write187_IntEnum";
                    _tmp[@"SerializationTypes.UIntEnum::"] = @"Write188_UIntEnum";
                    _tmp[@"SerializationTypes.LongEnum::"] = @"Write189_LongEnum";
                    _tmp[@"SerializationTypes.ULongEnum::"] = @"Write190_ULongEnum";
                    _tmp[@"SerializationTypes.XmlSerializerAttributes::AttributeTesting:False:"] = @"Write191_AttributeTesting";
                    _tmp[@"SerializationTypes.ItemChoiceType::"] = @"Write192_ItemChoiceType";
                    _tmp[@"SerializationTypes.TypeWithAnyAttribute::"] = @"Write193_TypeWithAnyAttribute";
                    _tmp[@"SerializationTypes.KnownTypesThroughConstructor::"] = @"Write194_KnownTypesThroughConstructor";
                    _tmp[@"SerializationTypes.SimpleKnownTypeValue::"] = @"Write195_SimpleKnownTypeValue";
                    _tmp[@"SerializationTypes.ClassImplementingIXmlSerializable::"] = @"Write196_Item";
                    _tmp[@"SerializationTypes.TypeWithPropertyNameSpecified::"] = @"Write197_TypeWithPropertyNameSpecified";
                    _tmp[@"SerializationTypes.TypeWithXmlSchemaFormAttribute:::True:"] = @"Write198_TypeWithXmlSchemaFormAttribute";
                    _tmp[@"SerializationTypes.TypeWithTypeNameInXmlTypeAttribute::"] = @"Write199_MyXmlType";
                    _tmp[@"SerializationTypes.TypeWithSchemaFormInXmlAttribute::"] = @"Write200_Item";
                    _tmp[@"SerializationTypes.CustomDocument::"] = @"Write201_CustomDocument";
                    _tmp[@"SerializationTypes.CustomElement::"] = @"Write202_CustomElement";
                    _tmp[@"SerializationTypes.CustomAttribute::"] = @"Write203_Item";
                    _tmp[@"SerializationTypes.TypeWithNonPublicDefaultConstructor::"] = @"Write204_Item";
                    _tmp[@"SerializationTypes.ServerSettings::"] = @"Write205_ServerSettings";
                    _tmp[@"SerializationTypes.TypeWithXmlQualifiedName::"] = @"Write206_TypeWithXmlQualifiedName";
                    _tmp[@"SerializationTypes.TypeWith2DArrayProperty2::"] = @"Write207_TypeWith2DArrayProperty2";
                    _tmp[@"SerializationTypes.TypeWithPropertiesHavingDefaultValue::"] = @"Write208_Item";
                    _tmp[@"SerializationTypes.TypeWithEnumPropertyHavingDefaultValue::"] = @"Write209_Item";
                    _tmp[@"SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue::"] = @"Write210_Item";
                    _tmp[@"SerializationTypes.TypeWithShouldSerializeMethod::"] = @"Write211_TypeWithShouldSerializeMethod";
                    _tmp[@"SerializationTypes.KnownTypesThroughConstructorWithArrayProperties::"] = @"Write212_Item";
                    _tmp[@"SerializationTypes.KnownTypesThroughConstructorWithValue::"] = @"Write213_Item";
                    _tmp[@"SerializationTypes.TypeWithTypesHavingCustomFormatter::"] = @"Write214_Item";
                    _tmp[@"SerializationTypes.TypeWithArrayPropertyHavingChoice::"] = @"Write215_Item";
                    _tmp[@"SerializationTypes.MoreChoices::"] = @"Write216_MoreChoices";
                    _tmp[@"SerializationTypes.TypeWithFieldsOrdered::"] = @"Write217_TypeWithFieldsOrdered";
                    _tmp[@"SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName::"] = @"Write218_Item";
                    _tmp[@"SerializationTypes.NamespaceTypeNameClashContainer::Root:True:"] = @"Write219_Root";
                    _tmp[@"SerializationTypes.TypeNameClashB.TypeNameClash::"] = @"Write220_TypeClashB";
                    _tmp[@"SerializationTypes.TypeNameClashA.TypeNameClash::"] = @"Write221_TypeClashA";
                    _tmp[@"Outer+Person::"] = @"Write222_Person";
                    if (writeMethods == null) writeMethods = _tmp;
                }
                return writeMethods;
            }
        }
        System.Collections.Hashtable typedSerializers = null;
        public override System.Collections.Hashtable TypedSerializers {
            get {
                if (typedSerializers == null) {
                    System.Collections.Hashtable _tmp = new System.Collections.Hashtable();
                    _tmp.Add(@"DerivedClass::", new DerivedClassSerializer());
                    _tmp.Add(@"XElementStruct::", new XElementStructSerializer());
                    _tmp.Add(@"Orchestra::", new OrchestraSerializer());
                    _tmp.Add(@"SerializationTypes.KnownTypesThroughConstructorWithArrayProperties::", new KnownTypesThroughConstructorWithArrayPropertiesSerializer());
                    _tmp.Add(@"DefaultValuesSetToNaN::", new DefaultValuesSetToNaNSerializer());
                    _tmp.Add(@"SerializationTypes.CustomAttribute::", new CustomAttributeSerializer());
                    _tmp.Add(@"SerializationTypes.WithEnums::", new WithEnumsSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithGetOnlyArrayProperties::", new TypeWithGetOnlyArrayPropertiesSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName::", new TypeWithKnownTypesOfCollectionsWithConflictingXmlNameSerializer());
                    _tmp.Add(@"XmlSerializableBaseClass:http://example.com/serializer-test-namespace::True:", new XmlSerializableBaseClassSerializer());
                    _tmp.Add(@"SerializationTypes.KnownTypesThroughConstructorWithValue::", new KnownTypesThroughConstructorWithValueSerializer());
                    _tmp.Add(@"SerializationTypes.ItemChoiceType::", new ItemChoiceTypeSerializer());
                    _tmp.Add(@"TypeWithDateTimeOffsetProperties::", new TypeWithDateTimeOffsetPropertiesSerializer());
                    _tmp.Add(@"TypeWithXmlElementProperty::", new TypeWithXmlElementPropertySerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithByteArrayAsXmlText::", new TypeWithByteArrayAsXmlTextSerializer());
                    _tmp.Add(@"Vehicle::", new VehicleSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithXmlTextAttributeOnArray:http://schemas.xmlsoap.org/ws/2005/04/discovery::False:", new TypeWithXmlTextAttributeOnArraySerializer());
                    _tmp.Add(@"SerializationTypes.__TypeNameWithSpecialCharacters漢ñ::", new __TypeNameWithSpecialCharacters漢ñSerializer());
                    _tmp.Add(@"SerializationTypes.WithStruct::", new WithStructSerializer());
                    _tmp.Add(@"SerializationTypes.SimpleKnownTypeValue::", new SimpleKnownTypeValueSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithEnumPropertyHavingDefaultValue::", new TypeWithEnumPropertyHavingDefaultValueSerializer());
                    _tmp.Add(@"SimpleBaseClass::", new SimpleBaseClassSerializer());
                    _tmp.Add(@"SerializationTypes.ShortEnum::", new ShortEnumSerializer());
                    _tmp.Add(@"BaseClass1::", new BaseClass1Serializer());
                    _tmp.Add(@"XElementArrayWrapper::", new XElementArrayWrapperSerializer());
                    _tmp.Add(@"Instrument::", new InstrumentSerializer());
                    _tmp.Add(@"DerivedClass1::", new DerivedClass1Serializer());
                    _tmp.Add(@"SerializationTypes.TypeWithMyCollectionField::", new TypeWithMyCollectionFieldSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithSchemaFormInXmlAttribute::", new TypeWithSchemaFormInXmlAttributeSerializer());
                    _tmp.Add(@"SerializationTypes.CustomDocument::", new CustomDocumentSerializer());
                    _tmp.Add(@"SerializationTypes.NamespaceTypeNameClashContainer::Root:True:", new NamespaceTypeNameClashContainerSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithReadOnlyMyCollectionProperty::", new TypeWithReadOnlyMyCollectionPropertySerializer());
                    _tmp.Add(@"SerializationTypes.ULongEnum::", new ULongEnumSerializer());
                    _tmp.Add(@"SerializationTypes.TypeA::", new TypeASerializer());
                    _tmp.Add(@"SerializationTypes.SByteEnum::", new SByteEnumSerializer());
                    _tmp.Add(@"SerializationTypes.DCClassWithEnumAndStruct::", new DCClassWithEnumAndStructSerializer());
                    _tmp.Add(@"SerializationTypes.KnownTypesThroughConstructor::", new KnownTypesThroughConstructorSerializer());
                    _tmp.Add(@"SerializationTypes.TypeNameClashA.TypeNameClash::", new TypeNameClashSerializer1());
                    _tmp.Add(@"BaseClass::", new BaseClassSerializer());
                    _tmp.Add(@"SerializationTypes.SimpleDC::", new SimpleDCSerializer());
                    _tmp.Add(@"SerializationTypes.StructNotSerializable::", new StructNotSerializableSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithDateTimePropertyAsXmlTime::", new TypeWithDateTimePropertyAsXmlTimeSerializer());
                    _tmp.Add(@"MsgDocumentType:http://example.com:Document:True:", new MsgDocumentTypeSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithXmlSchemaFormAttribute:::True:", new TypeWithXmlSchemaFormAttributeSerializer());
                    _tmp.Add(@"SerializationTypes.TypeNameClashB.TypeNameClash::", new TypeNameClashSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithDateTimeStringProperty::", new TypeWithDateTimeStringPropertySerializer());
                    _tmp.Add(@"OrderedItem::", new OrderedItemSerializer());
                    _tmp.Add(@"XElementWrapper::", new XElementWrapperSerializer());
                    _tmp.Add(@"TypeWithLinkedProperty::", new TypeWithLinkedPropertySerializer());
                    _tmp.Add(@"Pet::", new PetSerializer());
                    _tmp.Add(@"SerializationTypes.TypeB::", new TypeBSerializer());
                    _tmp.Add(@"XmlSerializableDerivedClass::", new XmlSerializableDerivedClassSerializer());
                    _tmp.Add(@"SerializationTypes.BuiltInTypes::", new BuiltInTypesSerializer());
                    _tmp.Add(@"DefaultValuesSetToNegativeInfinity::", new DefaultValuesSetToNegativeInfinitySerializer());
                    _tmp.Add(@"SerializationTypes.EnumFlags::", new EnumFlagsSerializer());
                    _tmp.Add(@"TypeWithBinaryProperty::", new TypeWithBinaryPropertySerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithNonPublicDefaultConstructor::", new TypeWithNonPublicDefaultConstructorSerializer());
                    _tmp.Add(@"DefaultValuesSetToPositiveInfinity::", new DefaultValuesSetToPositiveInfinitySerializer());
                    _tmp.Add(@"Trumpet::", new TrumpetSerializer());
                    _tmp.Add(@"SerializationTypes.ClassImplementingIXmlSerializable::", new ClassImplementingIXmlSerializableSerializer());
                    _tmp.Add(@"SerializationTypes.BaseClassWithSamePropertyName::", new BaseClassWithSamePropertyNameSerializer());
                    _tmp.Add(@"RootClass::", new RootClassSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithXmlQualifiedName::", new TypeWithXmlQualifiedNameSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithTypeNameInXmlTypeAttribute::", new TypeWithTypeNameInXmlTypeAttributeSerializer());
                    _tmp.Add(@"SerializationTypes.WithNullables::", new WithNullablesSerializer());
                    _tmp.Add(@"Animal::", new AnimalSerializer());
                    _tmp.Add(@"SerializationTypes.SomeStruct::", new SomeStructSerializer());
                    _tmp.Add(@"Parameter::", new ParameterSerializer());
                    _tmp.Add(@"TypeWithMismatchBetweenAttributeAndPropertyType::RootElement:True:", new TypeWithMismatchBetweenAttributeAndPropertyTypeSerializer());
                    _tmp.Add(@"SerializationTypes.DerivedClassWithSameProperty::", new DerivedClassWithSamePropertySerializer());
                    _tmp.Add(@"DogBreed::", new DogBreedSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithPropertiesHavingDefaultValue::", new TypeWithPropertiesHavingDefaultValueSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithAnyAttribute::", new TypeWithAnyAttributeSerializer());
                    _tmp.Add(@"Employee::", new EmployeeSerializer());
                    _tmp.Add(@"Outer+Person::", new PersonSerializer());
                    _tmp.Add(@"SimpleDerivedClass::", new SimpleDerivedClassSerializer());
                    _tmp.Add(@"TypeWithXmlNodeArrayProperty:::True:", new TypeWithXmlNodeArrayPropertySerializer());
                    _tmp.Add(@"AliasedTestType::", new AliasedTestTypeSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue::", new TypeWithEnumFlagPropertyHavingDefaultValueSerializer());
                    _tmp.Add(@"SerializationTypes.SimpleType::", new SimpleTypeSerializer());
                    _tmp.Add(@"SerializationTypes.LongEnum::", new LongEnumSerializer());
                    _tmp.Add(@"MyCollection1::", new MyCollection1Serializer());
                    _tmp.Add(@"TypeWithByteProperty::", new TypeWithBytePropertySerializer());
                    _tmp.Add(@"SerializationTypes.ServerSettings::", new ServerSettingsSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithArrayPropertyHavingChoice::", new TypeWithArrayPropertyHavingChoiceSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWith2DArrayProperty2::", new TypeWith2DArrayProperty2Serializer());
                    _tmp.Add(@"SerializationTypes.ByteEnum::", new ByteEnumSerializer());
                    _tmp.Add(@"Group::", new GroupSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithGetSetArrayMembers::", new TypeWithGetSetArrayMembersSerializer());
                    _tmp.Add(@"SerializationTypes.MyEnum::", new MyEnumSerializer());
                    _tmp.Add(@"Dog::", new DogSerializer());
                    _tmp.Add(@"SerializationTypes.TypeHasArrayOfASerializedAsB::", new TypeHasArrayOfASerializedAsBSerializer());
                    _tmp.Add(@"SerializationTypes.DerivedClassWithSameProperty2::", new DerivedClassWithSameProperty2Serializer());
                    _tmp.Add(@"TypeWithXmlDocumentProperty::", new TypeWithXmlDocumentPropertySerializer());
                    _tmp.Add(@"SerializationTypes.ClassImplementsInterface::", new ClassImplementsInterfaceSerializer());
                    _tmp.Add(@"Address::", new AddressSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithEnumMembers::", new TypeWithEnumMembersSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithShouldSerializeMethod::", new TypeWithShouldSerializeMethodSerializer());
                    _tmp.Add(@"SerializationTypes.MoreChoices::", new MoreChoicesSerializer());
                    _tmp.Add(@"SerializationTypes.XmlSerializerAttributes::AttributeTesting:False:", new XmlSerializerAttributesSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithPropertyNameSpecified::", new TypeWithPropertyNameSpecifiedSerializer());
                    _tmp.Add(@"SerializationTypes.UIntEnum::", new UIntEnumSerializer());
                    _tmp.Add(@"TypeWithTimeSpanProperty::", new TypeWithTimeSpanPropertySerializer());
                    _tmp.Add(@"SerializationTypes.MyList::", new MyListSerializer());
                    _tmp.Add(@"Brass::", new BrassSerializer());
                    _tmp.Add(@"SerializationTypes.CustomElement::", new CustomElementSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithTypesHavingCustomFormatter::", new TypeWithTypesHavingCustomFormatterSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithFieldsOrdered::", new TypeWithFieldsOrderedSerializer());
                    _tmp.Add(@"PurchaseOrder:http://www.contoso1.com:PurchaseOrder:False:", new PurchaseOrderSerializer());
                    _tmp.Add(@"SerializationTypes.IntEnum::", new IntEnumSerializer());
                    _tmp.Add(@"SerializationTypes.DCStruct::", new DCStructSerializer());
                    _tmp.Add(@"TypeWithDefaultTimeSpanProperty::", new TypeWithDefaultTimeSpanPropertySerializer());
                    if (typedSerializers == null) typedSerializers = _tmp;
                }
                return typedSerializers;
            }
        }
        public override System.Boolean CanSerialize(System.Type type) {
            if (type == typeof(global::TypeWithXmlElementProperty)) return true;
            if (type == typeof(global::TypeWithXmlDocumentProperty)) return true;
            if (type == typeof(global::TypeWithBinaryProperty)) return true;
            if (type == typeof(global::TypeWithDateTimeOffsetProperties)) return true;
            if (type == typeof(global::TypeWithTimeSpanProperty)) return true;
            if (type == typeof(global::TypeWithDefaultTimeSpanProperty)) return true;
            if (type == typeof(global::TypeWithByteProperty)) return true;
            if (type == typeof(global::TypeWithXmlNodeArrayProperty)) return true;
            if (type == typeof(global::Animal)) return true;
            if (type == typeof(global::Dog)) return true;
            if (type == typeof(global::DogBreed)) return true;
            if (type == typeof(global::Group)) return true;
            if (type == typeof(global::Vehicle)) return true;
            if (type == typeof(global::Employee)) return true;
            if (type == typeof(global::BaseClass)) return true;
            if (type == typeof(global::DerivedClass)) return true;
            if (type == typeof(global::SimpleBaseClass)) return true;
            if (type == typeof(global::SimpleDerivedClass)) return true;
            if (type == typeof(global::XmlSerializableBaseClass)) return true;
            if (type == typeof(global::XmlSerializableDerivedClass)) return true;
            if (type == typeof(global::PurchaseOrder)) return true;
            if (type == typeof(global::Address)) return true;
            if (type == typeof(global::OrderedItem)) return true;
            if (type == typeof(global::AliasedTestType)) return true;
            if (type == typeof(global::BaseClass1)) return true;
            if (type == typeof(global::DerivedClass1)) return true;
            if (type == typeof(global::MyCollection1)) return true;
            if (type == typeof(global::Orchestra)) return true;
            if (type == typeof(global::Instrument)) return true;
            if (type == typeof(global::Brass)) return true;
            if (type == typeof(global::Trumpet)) return true;
            if (type == typeof(global::Pet)) return true;
            if (type == typeof(global::DefaultValuesSetToNaN)) return true;
            if (type == typeof(global::DefaultValuesSetToPositiveInfinity)) return true;
            if (type == typeof(global::DefaultValuesSetToNegativeInfinity)) return true;
            if (type == typeof(global::TypeWithMismatchBetweenAttributeAndPropertyType)) return true;
            if (type == typeof(global::TypeWithLinkedProperty)) return true;
            if (type == typeof(global::MsgDocumentType)) return true;
            if (type == typeof(global::RootClass)) return true;
            if (type == typeof(global::Parameter)) return true;
            if (type == typeof(global::XElementWrapper)) return true;
            if (type == typeof(global::XElementStruct)) return true;
            if (type == typeof(global::XElementArrayWrapper)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithDateTimeStringProperty)) return true;
            if (type == typeof(global::SerializationTypes.SimpleType)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithGetSetArrayMembers)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithGetOnlyArrayProperties)) return true;
            if (type == typeof(global::SerializationTypes.StructNotSerializable)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithMyCollectionField)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithReadOnlyMyCollectionProperty)) return true;
            if (type == typeof(global::SerializationTypes.MyList)) return true;
            if (type == typeof(global::SerializationTypes.MyEnum)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithEnumMembers)) return true;
            if (type == typeof(global::SerializationTypes.DCStruct)) return true;
            if (type == typeof(global::SerializationTypes.DCClassWithEnumAndStruct)) return true;
            if (type == typeof(global::SerializationTypes.BuiltInTypes)) return true;
            if (type == typeof(global::SerializationTypes.TypeA)) return true;
            if (type == typeof(global::SerializationTypes.TypeB)) return true;
            if (type == typeof(global::SerializationTypes.TypeHasArrayOfASerializedAsB)) return true;
            if (type == typeof(global::SerializationTypes.@__TypeNameWithSpecialCharacters漢ñ)) return true;
            if (type == typeof(global::SerializationTypes.BaseClassWithSamePropertyName)) return true;
            if (type == typeof(global::SerializationTypes.DerivedClassWithSameProperty)) return true;
            if (type == typeof(global::SerializationTypes.DerivedClassWithSameProperty2)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithDateTimePropertyAsXmlTime)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithByteArrayAsXmlText)) return true;
            if (type == typeof(global::SerializationTypes.SimpleDC)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithXmlTextAttributeOnArray)) return true;
            if (type == typeof(global::SerializationTypes.EnumFlags)) return true;
            if (type == typeof(global::SerializationTypes.ClassImplementsInterface)) return true;
            if (type == typeof(global::SerializationTypes.WithStruct)) return true;
            if (type == typeof(global::SerializationTypes.SomeStruct)) return true;
            if (type == typeof(global::SerializationTypes.WithEnums)) return true;
            if (type == typeof(global::SerializationTypes.WithNullables)) return true;
            if (type == typeof(global::SerializationTypes.ByteEnum)) return true;
            if (type == typeof(global::SerializationTypes.SByteEnum)) return true;
            if (type == typeof(global::SerializationTypes.ShortEnum)) return true;
            if (type == typeof(global::SerializationTypes.IntEnum)) return true;
            if (type == typeof(global::SerializationTypes.UIntEnum)) return true;
            if (type == typeof(global::SerializationTypes.LongEnum)) return true;
            if (type == typeof(global::SerializationTypes.ULongEnum)) return true;
            if (type == typeof(global::SerializationTypes.XmlSerializerAttributes)) return true;
            if (type == typeof(global::SerializationTypes.ItemChoiceType)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithAnyAttribute)) return true;
            if (type == typeof(global::SerializationTypes.KnownTypesThroughConstructor)) return true;
            if (type == typeof(global::SerializationTypes.SimpleKnownTypeValue)) return true;
            if (type == typeof(global::SerializationTypes.ClassImplementingIXmlSerializable)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithPropertyNameSpecified)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithXmlSchemaFormAttribute)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithTypeNameInXmlTypeAttribute)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithSchemaFormInXmlAttribute)) return true;
            if (type == typeof(global::SerializationTypes.CustomDocument)) return true;
            if (type == typeof(global::SerializationTypes.CustomElement)) return true;
            if (type == typeof(global::SerializationTypes.CustomAttribute)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithNonPublicDefaultConstructor)) return true;
            if (type == typeof(global::SerializationTypes.ServerSettings)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithXmlQualifiedName)) return true;
            if (type == typeof(global::SerializationTypes.TypeWith2DArrayProperty2)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithPropertiesHavingDefaultValue)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithEnumPropertyHavingDefaultValue)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithShouldSerializeMethod)) return true;
            if (type == typeof(global::SerializationTypes.KnownTypesThroughConstructorWithArrayProperties)) return true;
            if (type == typeof(global::SerializationTypes.KnownTypesThroughConstructorWithValue)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithTypesHavingCustomFormatter)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithArrayPropertyHavingChoice)) return true;
            if (type == typeof(global::SerializationTypes.MoreChoices)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithFieldsOrdered)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName)) return true;
            if (type == typeof(global::SerializationTypes.NamespaceTypeNameClashContainer)) return true;
            if (type == typeof(global::SerializationTypes.TypeNameClashB.TypeNameClash)) return true;
            if (type == typeof(global::SerializationTypes.TypeNameClashA.TypeNameClash)) return true;
            if (type == typeof(global::Outer.Person)) return true;
            return false;
        }
        public override System.Xml.Serialization.XmlSerializer GetSerializer(System.Type type) {
            if (type == typeof(global::TypeWithXmlElementProperty)) return new TypeWithXmlElementPropertySerializer();
            if (type == typeof(global::TypeWithXmlDocumentProperty)) return new TypeWithXmlDocumentPropertySerializer();
            if (type == typeof(global::TypeWithBinaryProperty)) return new TypeWithBinaryPropertySerializer();
            if (type == typeof(global::TypeWithDateTimeOffsetProperties)) return new TypeWithDateTimeOffsetPropertiesSerializer();
            if (type == typeof(global::TypeWithTimeSpanProperty)) return new TypeWithTimeSpanPropertySerializer();
            if (type == typeof(global::TypeWithDefaultTimeSpanProperty)) return new TypeWithDefaultTimeSpanPropertySerializer();
            if (type == typeof(global::TypeWithByteProperty)) return new TypeWithBytePropertySerializer();
            if (type == typeof(global::TypeWithXmlNodeArrayProperty)) return new TypeWithXmlNodeArrayPropertySerializer();
            if (type == typeof(global::Animal)) return new AnimalSerializer();
            if (type == typeof(global::Dog)) return new DogSerializer();
            if (type == typeof(global::DogBreed)) return new DogBreedSerializer();
            if (type == typeof(global::Group)) return new GroupSerializer();
            if (type == typeof(global::Vehicle)) return new VehicleSerializer();
            if (type == typeof(global::Employee)) return new EmployeeSerializer();
            if (type == typeof(global::BaseClass)) return new BaseClassSerializer();
            if (type == typeof(global::DerivedClass)) return new DerivedClassSerializer();
            if (type == typeof(global::SimpleBaseClass)) return new SimpleBaseClassSerializer();
            if (type == typeof(global::SimpleDerivedClass)) return new SimpleDerivedClassSerializer();
            if (type == typeof(global::XmlSerializableBaseClass)) return new XmlSerializableBaseClassSerializer();
            if (type == typeof(global::XmlSerializableDerivedClass)) return new XmlSerializableDerivedClassSerializer();
            if (type == typeof(global::PurchaseOrder)) return new PurchaseOrderSerializer();
            if (type == typeof(global::Address)) return new AddressSerializer();
            if (type == typeof(global::OrderedItem)) return new OrderedItemSerializer();
            if (type == typeof(global::AliasedTestType)) return new AliasedTestTypeSerializer();
            if (type == typeof(global::BaseClass1)) return new BaseClass1Serializer();
            if (type == typeof(global::DerivedClass1)) return new DerivedClass1Serializer();
            if (type == typeof(global::MyCollection1)) return new MyCollection1Serializer();
            if (type == typeof(global::Orchestra)) return new OrchestraSerializer();
            if (type == typeof(global::Instrument)) return new InstrumentSerializer();
            if (type == typeof(global::Brass)) return new BrassSerializer();
            if (type == typeof(global::Trumpet)) return new TrumpetSerializer();
            if (type == typeof(global::Pet)) return new PetSerializer();
            if (type == typeof(global::DefaultValuesSetToNaN)) return new DefaultValuesSetToNaNSerializer();
            if (type == typeof(global::DefaultValuesSetToPositiveInfinity)) return new DefaultValuesSetToPositiveInfinitySerializer();
            if (type == typeof(global::DefaultValuesSetToNegativeInfinity)) return new DefaultValuesSetToNegativeInfinitySerializer();
            if (type == typeof(global::TypeWithMismatchBetweenAttributeAndPropertyType)) return new TypeWithMismatchBetweenAttributeAndPropertyTypeSerializer();
            if (type == typeof(global::TypeWithLinkedProperty)) return new TypeWithLinkedPropertySerializer();
            if (type == typeof(global::MsgDocumentType)) return new MsgDocumentTypeSerializer();
            if (type == typeof(global::RootClass)) return new RootClassSerializer();
            if (type == typeof(global::Parameter)) return new ParameterSerializer();
            if (type == typeof(global::XElementWrapper)) return new XElementWrapperSerializer();
            if (type == typeof(global::XElementStruct)) return new XElementStructSerializer();
            if (type == typeof(global::XElementArrayWrapper)) return new XElementArrayWrapperSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithDateTimeStringProperty)) return new TypeWithDateTimeStringPropertySerializer();
            if (type == typeof(global::SerializationTypes.SimpleType)) return new SimpleTypeSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithGetSetArrayMembers)) return new TypeWithGetSetArrayMembersSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithGetOnlyArrayProperties)) return new TypeWithGetOnlyArrayPropertiesSerializer();
            if (type == typeof(global::SerializationTypes.StructNotSerializable)) return new StructNotSerializableSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithMyCollectionField)) return new TypeWithMyCollectionFieldSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithReadOnlyMyCollectionProperty)) return new TypeWithReadOnlyMyCollectionPropertySerializer();
            if (type == typeof(global::SerializationTypes.MyList)) return new MyListSerializer();
            if (type == typeof(global::SerializationTypes.MyEnum)) return new MyEnumSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithEnumMembers)) return new TypeWithEnumMembersSerializer();
            if (type == typeof(global::SerializationTypes.DCStruct)) return new DCStructSerializer();
            if (type == typeof(global::SerializationTypes.DCClassWithEnumAndStruct)) return new DCClassWithEnumAndStructSerializer();
            if (type == typeof(global::SerializationTypes.BuiltInTypes)) return new BuiltInTypesSerializer();
            if (type == typeof(global::SerializationTypes.TypeA)) return new TypeASerializer();
            if (type == typeof(global::SerializationTypes.TypeB)) return new TypeBSerializer();
            if (type == typeof(global::SerializationTypes.TypeHasArrayOfASerializedAsB)) return new TypeHasArrayOfASerializedAsBSerializer();
            if (type == typeof(global::SerializationTypes.@__TypeNameWithSpecialCharacters漢ñ)) return new __TypeNameWithSpecialCharacters漢ñSerializer();
            if (type == typeof(global::SerializationTypes.BaseClassWithSamePropertyName)) return new BaseClassWithSamePropertyNameSerializer();
            if (type == typeof(global::SerializationTypes.DerivedClassWithSameProperty)) return new DerivedClassWithSamePropertySerializer();
            if (type == typeof(global::SerializationTypes.DerivedClassWithSameProperty2)) return new DerivedClassWithSameProperty2Serializer();
            if (type == typeof(global::SerializationTypes.TypeWithDateTimePropertyAsXmlTime)) return new TypeWithDateTimePropertyAsXmlTimeSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithByteArrayAsXmlText)) return new TypeWithByteArrayAsXmlTextSerializer();
            if (type == typeof(global::SerializationTypes.SimpleDC)) return new SimpleDCSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithXmlTextAttributeOnArray)) return new TypeWithXmlTextAttributeOnArraySerializer();
            if (type == typeof(global::SerializationTypes.EnumFlags)) return new EnumFlagsSerializer();
            if (type == typeof(global::SerializationTypes.ClassImplementsInterface)) return new ClassImplementsInterfaceSerializer();
            if (type == typeof(global::SerializationTypes.WithStruct)) return new WithStructSerializer();
            if (type == typeof(global::SerializationTypes.SomeStruct)) return new SomeStructSerializer();
            if (type == typeof(global::SerializationTypes.WithEnums)) return new WithEnumsSerializer();
            if (type == typeof(global::SerializationTypes.WithNullables)) return new WithNullablesSerializer();
            if (type == typeof(global::SerializationTypes.ByteEnum)) return new ByteEnumSerializer();
            if (type == typeof(global::SerializationTypes.SByteEnum)) return new SByteEnumSerializer();
            if (type == typeof(global::SerializationTypes.ShortEnum)) return new ShortEnumSerializer();
            if (type == typeof(global::SerializationTypes.IntEnum)) return new IntEnumSerializer();
            if (type == typeof(global::SerializationTypes.UIntEnum)) return new UIntEnumSerializer();
            if (type == typeof(global::SerializationTypes.LongEnum)) return new LongEnumSerializer();
            if (type == typeof(global::SerializationTypes.ULongEnum)) return new ULongEnumSerializer();
            if (type == typeof(global::SerializationTypes.XmlSerializerAttributes)) return new XmlSerializerAttributesSerializer();
            if (type == typeof(global::SerializationTypes.ItemChoiceType)) return new ItemChoiceTypeSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithAnyAttribute)) return new TypeWithAnyAttributeSerializer();
            if (type == typeof(global::SerializationTypes.KnownTypesThroughConstructor)) return new KnownTypesThroughConstructorSerializer();
            if (type == typeof(global::SerializationTypes.SimpleKnownTypeValue)) return new SimpleKnownTypeValueSerializer();
            if (type == typeof(global::SerializationTypes.ClassImplementingIXmlSerializable)) return new ClassImplementingIXmlSerializableSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithPropertyNameSpecified)) return new TypeWithPropertyNameSpecifiedSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithXmlSchemaFormAttribute)) return new TypeWithXmlSchemaFormAttributeSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithTypeNameInXmlTypeAttribute)) return new TypeWithTypeNameInXmlTypeAttributeSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithSchemaFormInXmlAttribute)) return new TypeWithSchemaFormInXmlAttributeSerializer();
            if (type == typeof(global::SerializationTypes.CustomDocument)) return new CustomDocumentSerializer();
            if (type == typeof(global::SerializationTypes.CustomElement)) return new CustomElementSerializer();
            if (type == typeof(global::SerializationTypes.CustomAttribute)) return new CustomAttributeSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithNonPublicDefaultConstructor)) return new TypeWithNonPublicDefaultConstructorSerializer();
            if (type == typeof(global::SerializationTypes.ServerSettings)) return new ServerSettingsSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithXmlQualifiedName)) return new TypeWithXmlQualifiedNameSerializer();
            if (type == typeof(global::SerializationTypes.TypeWith2DArrayProperty2)) return new TypeWith2DArrayProperty2Serializer();
            if (type == typeof(global::SerializationTypes.TypeWithPropertiesHavingDefaultValue)) return new TypeWithPropertiesHavingDefaultValueSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithEnumPropertyHavingDefaultValue)) return new TypeWithEnumPropertyHavingDefaultValueSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue)) return new TypeWithEnumFlagPropertyHavingDefaultValueSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithShouldSerializeMethod)) return new TypeWithShouldSerializeMethodSerializer();
            if (type == typeof(global::SerializationTypes.KnownTypesThroughConstructorWithArrayProperties)) return new KnownTypesThroughConstructorWithArrayPropertiesSerializer();
            if (type == typeof(global::SerializationTypes.KnownTypesThroughConstructorWithValue)) return new KnownTypesThroughConstructorWithValueSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithTypesHavingCustomFormatter)) return new TypeWithTypesHavingCustomFormatterSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithArrayPropertyHavingChoice)) return new TypeWithArrayPropertyHavingChoiceSerializer();
            if (type == typeof(global::SerializationTypes.MoreChoices)) return new MoreChoicesSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithFieldsOrdered)) return new TypeWithFieldsOrderedSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName)) return new TypeWithKnownTypesOfCollectionsWithConflictingXmlNameSerializer();
            if (type == typeof(global::SerializationTypes.NamespaceTypeNameClashContainer)) return new NamespaceTypeNameClashContainerSerializer();
            if (type == typeof(global::SerializationTypes.TypeNameClashB.TypeNameClash)) return new TypeNameClashSerializer();
            if (type == typeof(global::SerializationTypes.TypeNameClashA.TypeNameClash)) return new TypeNameClashSerializer1();
            if (type == typeof(global::Outer.Person)) return new PersonSerializer();
            return null;
        }
    }
}
