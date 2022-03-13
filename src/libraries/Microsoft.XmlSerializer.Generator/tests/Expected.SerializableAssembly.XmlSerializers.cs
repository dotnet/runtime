[assembly:System.Security.AllowPartiallyTrustedCallers()]
[assembly:System.Security.SecurityTransparent()]
[assembly:System.Security.SecurityRules(System.Security.SecurityRuleSet.Level1)]
[assembly:System.Xml.Serialization.XmlSerializerVersionAttribute(ParentAssemblyId=@"%%ParentAssemblyId%%", Version=@"1.0.0.0")]
namespace Microsoft.Xml.Serialization.GeneratedAssembly {

    public class XmlSerializationWriter1 : System.Xml.Serialization.XmlSerializationWriter {

        public void Write104_TypeWithXmlElementProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithXmlElementProperty", @"");
                return;
            }
            TopLevelElement();
            Write2_TypeWithXmlElementProperty(@"TypeWithXmlElementProperty", @"", ((global::TypeWithXmlElementProperty)o), true, false);
        }

        public void Write105_TypeWithXmlDocumentProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithXmlDocumentProperty", @"");
                return;
            }
            TopLevelElement();
            Write3_TypeWithXmlDocumentProperty(@"TypeWithXmlDocumentProperty", @"", ((global::TypeWithXmlDocumentProperty)o), true, false);
        }

        public void Write106_TypeWithBinaryProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithBinaryProperty", @"");
                return;
            }
            TopLevelElement();
            Write4_TypeWithBinaryProperty(@"TypeWithBinaryProperty", @"", ((global::TypeWithBinaryProperty)o), true, false);
        }

        public void Write107_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithDateTimeOffsetProperties", @"");
                return;
            }
            TopLevelElement();
            Write5_Item(@"TypeWithDateTimeOffsetProperties", @"", ((global::TypeWithDateTimeOffsetProperties)o), true, false);
        }

        public void Write108_TypeWithTimeSpanProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithTimeSpanProperty", @"");
                return;
            }
            TopLevelElement();
            Write6_TypeWithTimeSpanProperty(@"TypeWithTimeSpanProperty", @"", ((global::TypeWithTimeSpanProperty)o), true, false);
        }

        public void Write109_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithDefaultTimeSpanProperty", @"");
                return;
            }
            TopLevelElement();
            Write7_Item(@"TypeWithDefaultTimeSpanProperty", @"", ((global::TypeWithDefaultTimeSpanProperty)o), true, false);
        }

        public void Write110_TypeWithByteProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithByteProperty", @"");
                return;
            }
            TopLevelElement();
            Write8_TypeWithByteProperty(@"TypeWithByteProperty", @"", ((global::TypeWithByteProperty)o), true, false);
        }

        public void Write111_TypeWithXmlNodeArrayProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithXmlNodeArrayProperty", @"");
                return;
            }
            TopLevelElement();
            Write9_TypeWithXmlNodeArrayProperty(@"TypeWithXmlNodeArrayProperty", @"", ((global::TypeWithXmlNodeArrayProperty)o), true, false);
        }

        public void Write112_Animal(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Animal", @"");
                return;
            }
            TopLevelElement();
            Write10_Animal(@"Animal", @"", ((global::Animal)o), true, false);
        }

        public void Write113_Dog(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Dog", @"");
                return;
            }
            TopLevelElement();
            Write12_Dog(@"Dog", @"", ((global::Dog)o), true, false);
        }

        public void Write114_DogBreed(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"DogBreed", @"");
                return;
            }
            WriteElementString(@"DogBreed", @"", Write11_DogBreed(((global::DogBreed)o)));
        }

        public void Write115_Group(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Group", @"");
                return;
            }
            TopLevelElement();
            Write14_Group(@"Group", @"", ((global::Group)o), true, false);
        }

        public void Write116_Vehicle(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Vehicle", @"");
                return;
            }
            TopLevelElement();
            Write13_Vehicle(@"Vehicle", @"", ((global::Vehicle)o), true, false);
        }

        public void Write117_Employee(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Employee", @"");
                return;
            }
            TopLevelElement();
            Write15_Employee(@"Employee", @"", ((global::Employee)o), true, false);
        }

        public void Write118_BaseClass(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"BaseClass", @"");
                return;
            }
            TopLevelElement();
            Write17_BaseClass(@"BaseClass", @"", ((global::BaseClass)o), true, false);
        }

        public void Write119_DerivedClass(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DerivedClass", @"");
                return;
            }
            TopLevelElement();
            Write16_DerivedClass(@"DerivedClass", @"", ((global::DerivedClass)o), true, false);
        }

        public void Write120_PurchaseOrder(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"PurchaseOrder", @"http://www.contoso1.com");
                return;
            }
            TopLevelElement();
            Write20_PurchaseOrder(@"PurchaseOrder", @"http://www.contoso1.com", ((global::PurchaseOrder)o), false, false);
        }

        public void Write121_Address(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Address", @"");
                return;
            }
            TopLevelElement();
            Write21_Address(@"Address", @"", ((global::Address)o), true, false);
        }

        public void Write122_OrderedItem(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"OrderedItem", @"");
                return;
            }
            TopLevelElement();
            Write22_OrderedItem(@"OrderedItem", @"", ((global::OrderedItem)o), true, false);
        }

        public void Write123_AliasedTestType(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"AliasedTestType", @"");
                return;
            }
            TopLevelElement();
            Write23_AliasedTestType(@"AliasedTestType", @"", ((global::AliasedTestType)o), true, false);
        }

        public void Write124_BaseClass1(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"BaseClass1", @"");
                return;
            }
            TopLevelElement();
            Write24_BaseClass1(@"BaseClass1", @"", ((global::BaseClass1)o), true, false);
        }

        public void Write125_DerivedClass1(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DerivedClass1", @"");
                return;
            }
            TopLevelElement();
            Write25_DerivedClass1(@"DerivedClass1", @"", ((global::DerivedClass1)o), true, false);
        }

        public void Write126_ArrayOfDateTime(object o) {
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

        public void Write127_Orchestra(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Orchestra", @"");
                return;
            }
            TopLevelElement();
            Write27_Orchestra(@"Orchestra", @"", ((global::Orchestra)o), true, false);
        }

        public void Write128_Instrument(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Instrument", @"");
                return;
            }
            TopLevelElement();
            Write26_Instrument(@"Instrument", @"", ((global::Instrument)o), true, false);
        }

        public void Write129_Brass(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Brass", @"");
                return;
            }
            TopLevelElement();
            Write28_Brass(@"Brass", @"", ((global::Brass)o), true, false);
        }

        public void Write130_Trumpet(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Trumpet", @"");
                return;
            }
            TopLevelElement();
            Write29_Trumpet(@"Trumpet", @"", ((global::Trumpet)o), true, false);
        }

        public void Write131_Pet(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Pet", @"");
                return;
            }
            TopLevelElement();
            Write30_Pet(@"Pet", @"", ((global::Pet)o), true, false);
        }

        public void Write132_DefaultValuesSetToNaN(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DefaultValuesSetToNaN", @"");
                return;
            }
            TopLevelElement();
            Write31_DefaultValuesSetToNaN(@"DefaultValuesSetToNaN", @"", ((global::DefaultValuesSetToNaN)o), true, false);
        }

        public void Write133_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DefaultValuesSetToPositiveInfinity", @"");
                return;
            }
            TopLevelElement();
            Write32_Item(@"DefaultValuesSetToPositiveInfinity", @"", ((global::DefaultValuesSetToPositiveInfinity)o), true, false);
        }

        public void Write134_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DefaultValuesSetToNegativeInfinity", @"");
                return;
            }
            TopLevelElement();
            Write33_Item(@"DefaultValuesSetToNegativeInfinity", @"", ((global::DefaultValuesSetToNegativeInfinity)o), true, false);
        }

        public void Write135_RootElement(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"RootElement", @"");
                return;
            }
            TopLevelElement();
            Write34_Item(@"RootElement", @"", ((global::TypeWithMismatchBetweenAttributeAndPropertyType)o), true, false);
        }

        public void Write136_TypeWithLinkedProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithLinkedProperty", @"");
                return;
            }
            TopLevelElement();
            Write35_TypeWithLinkedProperty(@"TypeWithLinkedProperty", @"", ((global::TypeWithLinkedProperty)o), true, false);
        }

        public void Write137_Document(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Document", @"http://example.com");
                return;
            }
            TopLevelElement();
            Write36_MsgDocumentType(@"Document", @"http://example.com", ((global::MsgDocumentType)o), true, false);
        }

        public void Write138_RootClass(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"RootClass", @"");
                return;
            }
            TopLevelElement();
            Write39_RootClass(@"RootClass", @"", ((global::RootClass)o), true, false);
        }

        public void Write139_Parameter(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Parameter", @"");
                return;
            }
            TopLevelElement();
            Write38_Parameter(@"Parameter", @"", ((global::Parameter)o), true, false);
        }

        public void Write140_TypeWithDateTimeStringProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithDateTimeStringProperty", @"");
                return;
            }
            TopLevelElement();
            Write40_TypeWithDateTimeStringProperty(@"TypeWithDateTimeStringProperty", @"", ((global::SerializationTypes.TypeWithDateTimeStringProperty)o), true, false);
        }

        public void Write141_SimpleType(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"SimpleType", @"");
                return;
            }
            TopLevelElement();
            Write41_SimpleType(@"SimpleType", @"", ((global::SerializationTypes.SimpleType)o), true, false);
        }

        public void Write142_TypeWithGetSetArrayMembers(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithGetSetArrayMembers", @"");
                return;
            }
            TopLevelElement();
            Write42_TypeWithGetSetArrayMembers(@"TypeWithGetSetArrayMembers", @"", ((global::SerializationTypes.TypeWithGetSetArrayMembers)o), true, false);
        }

        public void Write143_TypeWithGetOnlyArrayProperties(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithGetOnlyArrayProperties", @"");
                return;
            }
            TopLevelElement();
            Write43_TypeWithGetOnlyArrayProperties(@"TypeWithGetOnlyArrayProperties", @"", ((global::SerializationTypes.TypeWithGetOnlyArrayProperties)o), true, false);
        }

        public void Write144_StructNotSerializable(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"StructNotSerializable", @"");
                return;
            }
            Write44_StructNotSerializable(@"StructNotSerializable", @"", ((global::SerializationTypes.StructNotSerializable)o), false);
        }

        public void Write145_TypeWithMyCollectionField(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithMyCollectionField", @"");
                return;
            }
            TopLevelElement();
            Write45_TypeWithMyCollectionField(@"TypeWithMyCollectionField", @"", ((global::SerializationTypes.TypeWithMyCollectionField)o), true, false);
        }

        public void Write146_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithReadOnlyMyCollectionProperty", @"");
                return;
            }
            TopLevelElement();
            Write46_Item(@"TypeWithReadOnlyMyCollectionProperty", @"", ((global::SerializationTypes.TypeWithReadOnlyMyCollectionProperty)o), true, false);
        }

        public void Write147_ArrayOfAnyType(object o) {
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

        public void Write148_MyEnum(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"MyEnum", @"");
                return;
            }
            WriteElementString(@"MyEnum", @"", Write47_MyEnum(((global::SerializationTypes.MyEnum)o)));
        }

        public void Write149_TypeWithEnumMembers(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithEnumMembers", @"");
                return;
            }
            TopLevelElement();
            Write48_TypeWithEnumMembers(@"TypeWithEnumMembers", @"", ((global::SerializationTypes.TypeWithEnumMembers)o), true, false);
        }

        public void Write150_DCStruct(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"DCStruct", @"");
                return;
            }
            Write49_DCStruct(@"DCStruct", @"", ((global::SerializationTypes.DCStruct)o), false);
        }

        public void Write151_DCClassWithEnumAndStruct(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DCClassWithEnumAndStruct", @"");
                return;
            }
            TopLevelElement();
            Write50_DCClassWithEnumAndStruct(@"DCClassWithEnumAndStruct", @"", ((global::SerializationTypes.DCClassWithEnumAndStruct)o), true, false);
        }

        public void Write152_BuiltInTypes(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"BuiltInTypes", @"");
                return;
            }
            TopLevelElement();
            Write51_BuiltInTypes(@"BuiltInTypes", @"", ((global::SerializationTypes.BuiltInTypes)o), true, false);
        }

        public void Write153_TypeA(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeA", @"");
                return;
            }
            TopLevelElement();
            Write52_TypeA(@"TypeA", @"", ((global::SerializationTypes.TypeA)o), true, false);
        }

        public void Write154_TypeB(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeB", @"");
                return;
            }
            TopLevelElement();
            Write53_TypeB(@"TypeB", @"", ((global::SerializationTypes.TypeB)o), true, false);
        }

        public void Write155_TypeHasArrayOfASerializedAsB(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeHasArrayOfASerializedAsB", @"");
                return;
            }
            TopLevelElement();
            Write54_TypeHasArrayOfASerializedAsB(@"TypeHasArrayOfASerializedAsB", @"", ((global::SerializationTypes.TypeHasArrayOfASerializedAsB)o), true, false);
        }

        public void Write156_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"__TypeNameWithSpecialCharacters漢ñ", @"");
                return;
            }
            TopLevelElement();
            Write55_Item(@"__TypeNameWithSpecialCharacters漢ñ", @"", ((global::SerializationTypes.@__TypeNameWithSpecialCharacters漢ñ)o), true, false);
        }

        public void Write157_BaseClassWithSamePropertyName(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"BaseClassWithSamePropertyName", @"");
                return;
            }
            TopLevelElement();
            Write56_BaseClassWithSamePropertyName(@"BaseClassWithSamePropertyName", @"", ((global::SerializationTypes.BaseClassWithSamePropertyName)o), true, false);
        }

        public void Write158_DerivedClassWithSameProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DerivedClassWithSameProperty", @"");
                return;
            }
            TopLevelElement();
            Write57_DerivedClassWithSameProperty(@"DerivedClassWithSameProperty", @"", ((global::SerializationTypes.DerivedClassWithSameProperty)o), true, false);
        }

        public void Write159_DerivedClassWithSameProperty2(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DerivedClassWithSameProperty2", @"");
                return;
            }
            TopLevelElement();
            Write58_DerivedClassWithSameProperty2(@"DerivedClassWithSameProperty2", @"", ((global::SerializationTypes.DerivedClassWithSameProperty2)o), true, false);
        }

        public void Write160_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithDateTimePropertyAsXmlTime", @"");
                return;
            }
            TopLevelElement();
            Write59_Item(@"TypeWithDateTimePropertyAsXmlTime", @"", ((global::SerializationTypes.TypeWithDateTimePropertyAsXmlTime)o), true, false);
        }

        public void Write161_TypeWithByteArrayAsXmlText(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithByteArrayAsXmlText", @"");
                return;
            }
            TopLevelElement();
            Write60_TypeWithByteArrayAsXmlText(@"TypeWithByteArrayAsXmlText", @"", ((global::SerializationTypes.TypeWithByteArrayAsXmlText)o), true, false);
        }

        public void Write162_SimpleDC(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"SimpleDC", @"");
                return;
            }
            TopLevelElement();
            Write61_SimpleDC(@"SimpleDC", @"", ((global::SerializationTypes.SimpleDC)o), true, false);
        }

        public void Write163_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"TypeWithXmlTextAttributeOnArray", @"http://schemas.xmlsoap.org/ws/2005/04/discovery");
                return;
            }
            TopLevelElement();
            Write62_Item(@"TypeWithXmlTextAttributeOnArray", @"http://schemas.xmlsoap.org/ws/2005/04/discovery", ((global::SerializationTypes.TypeWithXmlTextAttributeOnArray)o), false, false);
        }

        public void Write164_EnumFlags(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"EnumFlags", @"");
                return;
            }
            WriteElementString(@"EnumFlags", @"", Write63_EnumFlags(((global::SerializationTypes.EnumFlags)o)));
        }

        public void Write165_ClassImplementsInterface(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"ClassImplementsInterface", @"");
                return;
            }
            TopLevelElement();
            Write64_ClassImplementsInterface(@"ClassImplementsInterface", @"", ((global::SerializationTypes.ClassImplementsInterface)o), true, false);
        }

        public void Write166_WithStruct(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"WithStruct", @"");
                return;
            }
            TopLevelElement();
            Write66_WithStruct(@"WithStruct", @"", ((global::SerializationTypes.WithStruct)o), true, false);
        }

        public void Write167_SomeStruct(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"SomeStruct", @"");
                return;
            }
            Write65_SomeStruct(@"SomeStruct", @"", ((global::SerializationTypes.SomeStruct)o), false);
        }

        public void Write168_WithEnums(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"WithEnums", @"");
                return;
            }
            TopLevelElement();
            Write69_WithEnums(@"WithEnums", @"", ((global::SerializationTypes.WithEnums)o), true, false);
        }

        public void Write169_WithNullables(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"WithNullables", @"");
                return;
            }
            TopLevelElement();
            Write70_WithNullables(@"WithNullables", @"", ((global::SerializationTypes.WithNullables)o), true, false);
        }

        public void Write170_ByteEnum(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"ByteEnum", @"");
                return;
            }
            WriteElementString(@"ByteEnum", @"", Write71_ByteEnum(((global::SerializationTypes.ByteEnum)o)));
        }

        public void Write171_SByteEnum(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"SByteEnum", @"");
                return;
            }
            WriteElementString(@"SByteEnum", @"", Write72_SByteEnum(((global::SerializationTypes.SByteEnum)o)));
        }

        public void Write172_ShortEnum(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"ShortEnum", @"");
                return;
            }
            WriteElementString(@"ShortEnum", @"", Write68_ShortEnum(((global::SerializationTypes.ShortEnum)o)));
        }

        public void Write173_IntEnum(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"IntEnum", @"");
                return;
            }
            WriteElementString(@"IntEnum", @"", Write67_IntEnum(((global::SerializationTypes.IntEnum)o)));
        }

        public void Write174_UIntEnum(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"UIntEnum", @"");
                return;
            }
            WriteElementString(@"UIntEnum", @"", Write73_UIntEnum(((global::SerializationTypes.UIntEnum)o)));
        }

        public void Write175_LongEnum(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"LongEnum", @"");
                return;
            }
            WriteElementString(@"LongEnum", @"", Write74_LongEnum(((global::SerializationTypes.LongEnum)o)));
        }

        public void Write176_ULongEnum(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"ULongEnum", @"");
                return;
            }
            WriteElementString(@"ULongEnum", @"", Write75_ULongEnum(((global::SerializationTypes.ULongEnum)o)));
        }

        public void Write177_AttributeTesting(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"AttributeTesting", @"");
                return;
            }
            TopLevelElement();
            Write77_XmlSerializerAttributes(@"AttributeTesting", @"", ((global::SerializationTypes.XmlSerializerAttributes)o), false, false);
        }

        public void Write178_ItemChoiceType(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"ItemChoiceType", @"");
                return;
            }
            WriteElementString(@"ItemChoiceType", @"", Write76_ItemChoiceType(((global::SerializationTypes.ItemChoiceType)o)));
        }

        public void Write179_TypeWithAnyAttribute(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithAnyAttribute", @"");
                return;
            }
            TopLevelElement();
            Write78_TypeWithAnyAttribute(@"TypeWithAnyAttribute", @"", ((global::SerializationTypes.TypeWithAnyAttribute)o), true, false);
        }

        public void Write180_KnownTypesThroughConstructor(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"KnownTypesThroughConstructor", @"");
                return;
            }
            TopLevelElement();
            Write79_KnownTypesThroughConstructor(@"KnownTypesThroughConstructor", @"", ((global::SerializationTypes.KnownTypesThroughConstructor)o), true, false);
        }

        public void Write181_SimpleKnownTypeValue(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"SimpleKnownTypeValue", @"");
                return;
            }
            TopLevelElement();
            Write80_SimpleKnownTypeValue(@"SimpleKnownTypeValue", @"", ((global::SerializationTypes.SimpleKnownTypeValue)o), true, false);
        }

        public void Write182_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"ClassImplementingIXmlSerialiable", @"");
                return;
            }
            TopLevelElement();
            WriteSerializable((System.Xml.Serialization.IXmlSerializable)((global::SerializationTypes.ClassImplementingIXmlSerialiable)o), @"ClassImplementingIXmlSerialiable", @"", true, true);
        }

        public void Write183_TypeWithPropertyNameSpecified(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithPropertyNameSpecified", @"");
                return;
            }
            TopLevelElement();
            Write81_TypeWithPropertyNameSpecified(@"TypeWithPropertyNameSpecified", @"", ((global::SerializationTypes.TypeWithPropertyNameSpecified)o), true, false);
        }

        public void Write184_TypeWithXmlSchemaFormAttribute(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithXmlSchemaFormAttribute", @"");
                return;
            }
            TopLevelElement();
            Write82_TypeWithXmlSchemaFormAttribute(@"TypeWithXmlSchemaFormAttribute", @"", ((global::SerializationTypes.TypeWithXmlSchemaFormAttribute)o), true, false);
        }

        public void Write185_MyXmlType(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"MyXmlType", @"");
                return;
            }
            TopLevelElement();
            Write83_Item(@"MyXmlType", @"", ((global::SerializationTypes.TypeWithTypeNameInXmlTypeAttribute)o), true, false);
        }

        public void Write186_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithSchemaFormInXmlAttribute", @"");
                return;
            }
            TopLevelElement();
            Write84_Item(@"TypeWithSchemaFormInXmlAttribute", @"", ((global::SerializationTypes.TypeWithSchemaFormInXmlAttribute)o), true, false);
        }

        public void Write187_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithNonPublicDefaultConstructor", @"");
                return;
            }
            TopLevelElement();
            Write85_Item(@"TypeWithNonPublicDefaultConstructor", @"", ((global::SerializationTypes.TypeWithNonPublicDefaultConstructor)o), true, false);
        }

        public void Write188_ServerSettings(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"ServerSettings", @"");
                return;
            }
            TopLevelElement();
            Write86_ServerSettings(@"ServerSettings", @"", ((global::SerializationTypes.ServerSettings)o), true, false);
        }

        public void Write189_TypeWithXmlQualifiedName(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithXmlQualifiedName", @"");
                return;
            }
            TopLevelElement();
            Write87_TypeWithXmlQualifiedName(@"TypeWithXmlQualifiedName", @"", ((global::SerializationTypes.TypeWithXmlQualifiedName)o), true, false);
        }

        public void Write190_TypeWith2DArrayProperty2(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWith2DArrayProperty2", @"");
                return;
            }
            TopLevelElement();
            Write88_TypeWith2DArrayProperty2(@"TypeWith2DArrayProperty2", @"", ((global::SerializationTypes.TypeWith2DArrayProperty2)o), true, false);
        }

        public void Write191_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithPropertiesHavingDefaultValue", @"");
                return;
            }
            TopLevelElement();
            Write89_Item(@"TypeWithPropertiesHavingDefaultValue", @"", ((global::SerializationTypes.TypeWithPropertiesHavingDefaultValue)o), true, false);
        }

        public void Write192_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithEnumPropertyHavingDefaultValue", @"");
                return;
            }
            TopLevelElement();
            Write90_Item(@"TypeWithEnumPropertyHavingDefaultValue", @"", ((global::SerializationTypes.TypeWithEnumPropertyHavingDefaultValue)o), true, false);
        }

        public void Write193_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithEnumFlagPropertyHavingDefaultValue", @"");
                return;
            }
            TopLevelElement();
            Write91_Item(@"TypeWithEnumFlagPropertyHavingDefaultValue", @"", ((global::SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue)o), true, false);
        }

        public void Write194_TypeWithShouldSerializeMethod(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithShouldSerializeMethod", @"");
                return;
            }
            TopLevelElement();
            Write92_TypeWithShouldSerializeMethod(@"TypeWithShouldSerializeMethod", @"", ((global::SerializationTypes.TypeWithShouldSerializeMethod)o), true, false);
        }

        public void Write195_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"KnownTypesThroughConstructorWithArrayProperties", @"");
                return;
            }
            TopLevelElement();
            Write93_Item(@"KnownTypesThroughConstructorWithArrayProperties", @"", ((global::SerializationTypes.KnownTypesThroughConstructorWithArrayProperties)o), true, false);
        }

        public void Write196_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"KnownTypesThroughConstructorWithValue", @"");
                return;
            }
            TopLevelElement();
            Write94_Item(@"KnownTypesThroughConstructorWithValue", @"", ((global::SerializationTypes.KnownTypesThroughConstructorWithValue)o), true, false);
        }

        public void Write197_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithTypesHavingCustomFormatter", @"");
                return;
            }
            TopLevelElement();
            Write95_Item(@"TypeWithTypesHavingCustomFormatter", @"", ((global::SerializationTypes.TypeWithTypesHavingCustomFormatter)o), true, false);
        }

        public void Write198_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithArrayPropertyHavingChoice", @"");
                return;
            }
            TopLevelElement();
            Write97_Item(@"TypeWithArrayPropertyHavingChoice", @"", ((global::SerializationTypes.TypeWithArrayPropertyHavingChoice)o), true, false);
        }

        public void Write199_MoreChoices(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"MoreChoices", @"");
                return;
            }
            WriteElementString(@"MoreChoices", @"", Write96_MoreChoices(((global::SerializationTypes.MoreChoices)o)));
        }

        public void Write200_TypeWithFieldsOrdered(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithFieldsOrdered", @"");
                return;
            }
            TopLevelElement();
            Write98_TypeWithFieldsOrdered(@"TypeWithFieldsOrdered", @"", ((global::SerializationTypes.TypeWithFieldsOrdered)o), true, false);
        }

        public void Write201_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithKnownTypesOfCollectionsWithConflictingXmlName", @"");
                return;
            }
            TopLevelElement();
            Write99_Item(@"TypeWithKnownTypesOfCollectionsWithConflictingXmlName", @"", ((global::SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName)o), true, false);
        }

        public void Write202_Root(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Root", @"");
                return;
            }
            TopLevelElement();
            Write102_Item(@"Root", @"", ((global::SerializationTypes.NamespaceTypeNameClashContainer)o), true, false);
        }

        public void Write203_TypeClashB(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeClashB", @"");
                return;
            }
            TopLevelElement();
            Write101_TypeNameClash(@"TypeClashB", @"", ((global::SerializationTypes.TypeNameClashB.TypeNameClash)o), true, false);
        }

        public void Write204_TypeClashA(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeClashA", @"");
                return;
            }
            TopLevelElement();
            Write100_TypeNameClash(@"TypeClashA", @"", ((global::SerializationTypes.TypeNameClashA.TypeNameClash)o), true, false);
        }

        public void Write205_Person(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Person", @"");
                return;
            }
            TopLevelElement();
            Write103_Person(@"Person", @"", ((global::Outer.Person)o), true, false);
        }

        void Write103_Person(string n, string ns, global::Outer.Person o, bool isNullable, bool needType) {
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

        void Write100_TypeNameClash(string n, string ns, global::SerializationTypes.TypeNameClashA.TypeNameClash o, bool isNullable, bool needType) {
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

        void Write101_TypeNameClash(string n, string ns, global::SerializationTypes.TypeNameClashB.TypeNameClash o, bool isNullable, bool needType) {
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

        void Write102_Item(string n, string ns, global::SerializationTypes.NamespaceTypeNameClashContainer o, bool isNullable, bool needType) {
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
                        Write100_TypeNameClash(@"A", @"", ((global::SerializationTypes.TypeNameClashA.TypeNameClash)a[ia]), false, false);
                    }
                }
            }
            {
                global::SerializationTypes.TypeNameClashB.TypeNameClash[] a = (global::SerializationTypes.TypeNameClashB.TypeNameClash[])o.@B;
                if (a != null) {
                    for (int ia = 0; ia < a.Length; ia++) {
                        Write101_TypeNameClash(@"B", @"", ((global::SerializationTypes.TypeNameClashB.TypeNameClash)a[ia]), false, false);
                    }
                }
            }
            WriteEndElement(o);
        }

        void Write99_Item(string n, string ns, global::SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName o, bool isNullable, bool needType) {
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
                        Write103_Person(n, ns,(global::Outer.Person)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.NamespaceTypeNameClashContainer)) {
                        Write102_Item(n, ns,(global::SerializationTypes.NamespaceTypeNameClashContainer)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeNameClashB.TypeNameClash)) {
                        Write101_TypeNameClash(n, ns,(global::SerializationTypes.TypeNameClashB.TypeNameClash)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeNameClashA.TypeNameClash)) {
                        Write100_TypeNameClash(n, ns,(global::SerializationTypes.TypeNameClashA.TypeNameClash)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName)) {
                        Write99_Item(n, ns,(global::SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithFieldsOrdered)) {
                        Write98_TypeWithFieldsOrdered(n, ns,(global::SerializationTypes.TypeWithFieldsOrdered)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithArrayPropertyHavingChoice)) {
                        Write97_Item(n, ns,(global::SerializationTypes.TypeWithArrayPropertyHavingChoice)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithTypesHavingCustomFormatter)) {
                        Write95_Item(n, ns,(global::SerializationTypes.TypeWithTypesHavingCustomFormatter)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.KnownTypesThroughConstructorWithValue)) {
                        Write94_Item(n, ns,(global::SerializationTypes.KnownTypesThroughConstructorWithValue)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.KnownTypesThroughConstructorWithArrayProperties)) {
                        Write93_Item(n, ns,(global::SerializationTypes.KnownTypesThroughConstructorWithArrayProperties)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithShouldSerializeMethod)) {
                        Write92_TypeWithShouldSerializeMethod(n, ns,(global::SerializationTypes.TypeWithShouldSerializeMethod)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue)) {
                        Write91_Item(n, ns,(global::SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithEnumPropertyHavingDefaultValue)) {
                        Write90_Item(n, ns,(global::SerializationTypes.TypeWithEnumPropertyHavingDefaultValue)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithPropertiesHavingDefaultValue)) {
                        Write89_Item(n, ns,(global::SerializationTypes.TypeWithPropertiesHavingDefaultValue)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWith2DArrayProperty2)) {
                        Write88_TypeWith2DArrayProperty2(n, ns,(global::SerializationTypes.TypeWith2DArrayProperty2)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithXmlQualifiedName)) {
                        Write87_TypeWithXmlQualifiedName(n, ns,(global::SerializationTypes.TypeWithXmlQualifiedName)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.ServerSettings)) {
                        Write86_ServerSettings(n, ns,(global::SerializationTypes.ServerSettings)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithNonPublicDefaultConstructor)) {
                        Write85_Item(n, ns,(global::SerializationTypes.TypeWithNonPublicDefaultConstructor)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithTypeNameInXmlTypeAttribute)) {
                        Write83_Item(n, ns,(global::SerializationTypes.TypeWithTypeNameInXmlTypeAttribute)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithXmlSchemaFormAttribute)) {
                        Write82_TypeWithXmlSchemaFormAttribute(n, ns,(global::SerializationTypes.TypeWithXmlSchemaFormAttribute)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithPropertyNameSpecified)) {
                        Write81_TypeWithPropertyNameSpecified(n, ns,(global::SerializationTypes.TypeWithPropertyNameSpecified)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.SimpleKnownTypeValue)) {
                        Write80_SimpleKnownTypeValue(n, ns,(global::SerializationTypes.SimpleKnownTypeValue)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.KnownTypesThroughConstructor)) {
                        Write79_KnownTypesThroughConstructor(n, ns,(global::SerializationTypes.KnownTypesThroughConstructor)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithAnyAttribute)) {
                        Write78_TypeWithAnyAttribute(n, ns,(global::SerializationTypes.TypeWithAnyAttribute)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.XmlSerializerAttributes)) {
                        Write77_XmlSerializerAttributes(n, ns,(global::SerializationTypes.XmlSerializerAttributes)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.WithNullables)) {
                        Write70_WithNullables(n, ns,(global::SerializationTypes.WithNullables)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.WithEnums)) {
                        Write69_WithEnums(n, ns,(global::SerializationTypes.WithEnums)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.WithStruct)) {
                        Write66_WithStruct(n, ns,(global::SerializationTypes.WithStruct)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.SomeStruct)) {
                        Write65_SomeStruct(n, ns,(global::SerializationTypes.SomeStruct)o, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.ClassImplementsInterface)) {
                        Write64_ClassImplementsInterface(n, ns,(global::SerializationTypes.ClassImplementsInterface)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithXmlTextAttributeOnArray)) {
                        Write62_Item(n, ns,(global::SerializationTypes.TypeWithXmlTextAttributeOnArray)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.SimpleDC)) {
                        Write61_SimpleDC(n, ns,(global::SerializationTypes.SimpleDC)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithByteArrayAsXmlText)) {
                        Write60_TypeWithByteArrayAsXmlText(n, ns,(global::SerializationTypes.TypeWithByteArrayAsXmlText)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithDateTimePropertyAsXmlTime)) {
                        Write59_Item(n, ns,(global::SerializationTypes.TypeWithDateTimePropertyAsXmlTime)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.BaseClassWithSamePropertyName)) {
                        Write56_BaseClassWithSamePropertyName(n, ns,(global::SerializationTypes.BaseClassWithSamePropertyName)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.DerivedClassWithSameProperty)) {
                        Write57_DerivedClassWithSameProperty(n, ns,(global::SerializationTypes.DerivedClassWithSameProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.DerivedClassWithSameProperty2)) {
                        Write58_DerivedClassWithSameProperty2(n, ns,(global::SerializationTypes.DerivedClassWithSameProperty2)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.@__TypeNameWithSpecialCharacters漢ñ)) {
                        Write55_Item(n, ns,(global::SerializationTypes.@__TypeNameWithSpecialCharacters漢ñ)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeHasArrayOfASerializedAsB)) {
                        Write54_TypeHasArrayOfASerializedAsB(n, ns,(global::SerializationTypes.TypeHasArrayOfASerializedAsB)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeB)) {
                        Write53_TypeB(n, ns,(global::SerializationTypes.TypeB)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeA)) {
                        Write52_TypeA(n, ns,(global::SerializationTypes.TypeA)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.BuiltInTypes)) {
                        Write51_BuiltInTypes(n, ns,(global::SerializationTypes.BuiltInTypes)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.DCClassWithEnumAndStruct)) {
                        Write50_DCClassWithEnumAndStruct(n, ns,(global::SerializationTypes.DCClassWithEnumAndStruct)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.DCStruct)) {
                        Write49_DCStruct(n, ns,(global::SerializationTypes.DCStruct)o, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithEnumMembers)) {
                        Write48_TypeWithEnumMembers(n, ns,(global::SerializationTypes.TypeWithEnumMembers)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithReadOnlyMyCollectionProperty)) {
                        Write46_Item(n, ns,(global::SerializationTypes.TypeWithReadOnlyMyCollectionProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithMyCollectionField)) {
                        Write45_TypeWithMyCollectionField(n, ns,(global::SerializationTypes.TypeWithMyCollectionField)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.StructNotSerializable)) {
                        Write44_StructNotSerializable(n, ns,(global::SerializationTypes.StructNotSerializable)o, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithGetOnlyArrayProperties)) {
                        Write43_TypeWithGetOnlyArrayProperties(n, ns,(global::SerializationTypes.TypeWithGetOnlyArrayProperties)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithGetSetArrayMembers)) {
                        Write42_TypeWithGetSetArrayMembers(n, ns,(global::SerializationTypes.TypeWithGetSetArrayMembers)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.SimpleType)) {
                        Write41_SimpleType(n, ns,(global::SerializationTypes.SimpleType)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithDateTimeStringProperty)) {
                        Write40_TypeWithDateTimeStringProperty(n, ns,(global::SerializationTypes.TypeWithDateTimeStringProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::RootClass)) {
                        Write39_RootClass(n, ns,(global::RootClass)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Parameter)) {
                        Write38_Parameter(n, ns,(global::Parameter)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Parameter<global::System.String>)) {
                        Write37_ParameterOfString(n, ns,(global::Parameter<global::System.String>)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::MsgDocumentType)) {
                        Write36_MsgDocumentType(n, ns,(global::MsgDocumentType)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::TypeWithLinkedProperty)) {
                        Write35_TypeWithLinkedProperty(n, ns,(global::TypeWithLinkedProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::TypeWithMismatchBetweenAttributeAndPropertyType)) {
                        Write34_Item(n, ns,(global::TypeWithMismatchBetweenAttributeAndPropertyType)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::DefaultValuesSetToNegativeInfinity)) {
                        Write33_Item(n, ns,(global::DefaultValuesSetToNegativeInfinity)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::DefaultValuesSetToPositiveInfinity)) {
                        Write32_Item(n, ns,(global::DefaultValuesSetToPositiveInfinity)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::DefaultValuesSetToNaN)) {
                        Write31_DefaultValuesSetToNaN(n, ns,(global::DefaultValuesSetToNaN)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Pet)) {
                        Write30_Pet(n, ns,(global::Pet)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Orchestra)) {
                        Write27_Orchestra(n, ns,(global::Orchestra)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Instrument)) {
                        Write26_Instrument(n, ns,(global::Instrument)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Brass)) {
                        Write28_Brass(n, ns,(global::Brass)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Trumpet)) {
                        Write29_Trumpet(n, ns,(global::Trumpet)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::BaseClass1)) {
                        Write24_BaseClass1(n, ns,(global::BaseClass1)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::DerivedClass1)) {
                        Write25_DerivedClass1(n, ns,(global::DerivedClass1)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::AliasedTestType)) {
                        Write23_AliasedTestType(n, ns,(global::AliasedTestType)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::OrderedItem)) {
                        Write22_OrderedItem(n, ns,(global::OrderedItem)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Address)) {
                        Write21_Address(n, ns,(global::Address)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::PurchaseOrder)) {
                        Write20_PurchaseOrder(n, ns,(global::PurchaseOrder)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::OrderedItem)) {
                        Write19_OrderedItem(n, ns,(global::OrderedItem)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Address)) {
                        Write18_Address(n, ns,(global::Address)o, isNullable, true);
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
                                    Write19_OrderedItem(@"OrderedItem", @"http://www.contoso1.com", ((global::OrderedItem)a[ia]), true, false);
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
                                    Write26_Instrument(@"Instrument", @"", ((global::Instrument)a[ia]), true, false);
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
                                    Write35_TypeWithLinkedProperty(@"TypeWithLinkedProperty", @"", ((global::TypeWithLinkedProperty)a[ia]), true, false);
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
                                    Write38_Parameter(@"Parameter", @"", ((global::Parameter)a[ia]), true, false);
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
                                    Write41_SimpleType(@"SimpleType", @"", ((global::SerializationTypes.SimpleType)a[ia]), true, false);
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
                        Writer.WriteString(Write47_MyEnum((global::SerializationTypes.MyEnum)o));
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
                                    Write52_TypeA(@"TypeA", @"", ((global::SerializationTypes.TypeA)a[ia]), true, false);
                                }
                            }
                        }
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.EnumFlags)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"EnumFlags", @"");
                        Writer.WriteString(Write63_EnumFlags((global::SerializationTypes.EnumFlags)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.IntEnum)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"IntEnum", @"");
                        Writer.WriteString(Write67_IntEnum((global::SerializationTypes.IntEnum)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.ShortEnum)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ShortEnum", @"");
                        Writer.WriteString(Write68_ShortEnum((global::SerializationTypes.ShortEnum)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.ByteEnum)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ByteEnum", @"");
                        Writer.WriteString(Write71_ByteEnum((global::SerializationTypes.ByteEnum)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.SByteEnum)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"SByteEnum", @"");
                        Writer.WriteString(Write72_SByteEnum((global::SerializationTypes.SByteEnum)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.UIntEnum)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"UIntEnum", @"");
                        Writer.WriteString(Write73_UIntEnum((global::SerializationTypes.UIntEnum)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.LongEnum)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"LongEnum", @"");
                        Writer.WriteString(Write74_LongEnum((global::SerializationTypes.LongEnum)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.ULongEnum)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ULongEnum", @"");
                        Writer.WriteString(Write75_ULongEnum((global::SerializationTypes.ULongEnum)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.ItemChoiceType)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ItemChoiceType", @"");
                        Writer.WriteString(Write76_ItemChoiceType((global::SerializationTypes.ItemChoiceType)o));
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
                                    WriteElementString(@"ItemChoiceType", @"", Write76_ItemChoiceType(((global::SerializationTypes.ItemChoiceType)a[ia])));
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
                                    Write41_SimpleType(@"SimpleType", @"", ((global::SerializationTypes.SimpleType)a[ia]), true, false);
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
                        Writer.WriteString(Write96_MoreChoices((global::SerializationTypes.MoreChoices)o));
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

        string Write96_MoreChoices(global::SerializationTypes.MoreChoices v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.MoreChoices.@None: s = @"None"; break;
                case global::SerializationTypes.MoreChoices.@Item: s = @"Item"; break;
                case global::SerializationTypes.MoreChoices.@Amount: s = @"Amount"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.MoreChoices");
            }
            return s;
        }

        void Write41_SimpleType(string n, string ns, global::SerializationTypes.SimpleType o, bool isNullable, bool needType) {
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

        string Write76_ItemChoiceType(global::SerializationTypes.ItemChoiceType v) {
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

        string Write75_ULongEnum(global::SerializationTypes.ULongEnum v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.ULongEnum.@Option0: s = @"Option0"; break;
                case global::SerializationTypes.ULongEnum.@Option1: s = @"Option1"; break;
                case global::SerializationTypes.ULongEnum.@Option2: s = @"Option2"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.ULongEnum");
            }
            return s;
        }

        string Write74_LongEnum(global::SerializationTypes.LongEnum v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.LongEnum.@Option0: s = @"Option0"; break;
                case global::SerializationTypes.LongEnum.@Option1: s = @"Option1"; break;
                case global::SerializationTypes.LongEnum.@Option2: s = @"Option2"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.LongEnum");
            }
            return s;
        }

        string Write73_UIntEnum(global::SerializationTypes.UIntEnum v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.UIntEnum.@Option0: s = @"Option0"; break;
                case global::SerializationTypes.UIntEnum.@Option1: s = @"Option1"; break;
                case global::SerializationTypes.UIntEnum.@Option2: s = @"Option2"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.UIntEnum");
            }
            return s;
        }

        string Write72_SByteEnum(global::SerializationTypes.SByteEnum v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.SByteEnum.@Option0: s = @"Option0"; break;
                case global::SerializationTypes.SByteEnum.@Option1: s = @"Option1"; break;
                case global::SerializationTypes.SByteEnum.@Option2: s = @"Option2"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.SByteEnum");
            }
            return s;
        }

        string Write71_ByteEnum(global::SerializationTypes.ByteEnum v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.ByteEnum.@Option0: s = @"Option0"; break;
                case global::SerializationTypes.ByteEnum.@Option1: s = @"Option1"; break;
                case global::SerializationTypes.ByteEnum.@Option2: s = @"Option2"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.ByteEnum");
            }
            return s;
        }

        string Write68_ShortEnum(global::SerializationTypes.ShortEnum v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.ShortEnum.@Option0: s = @"Option0"; break;
                case global::SerializationTypes.ShortEnum.@Option1: s = @"Option1"; break;
                case global::SerializationTypes.ShortEnum.@Option2: s = @"Option2"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.ShortEnum");
            }
            return s;
        }

        string Write67_IntEnum(global::SerializationTypes.IntEnum v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.IntEnum.@Option0: s = @"Option0"; break;
                case global::SerializationTypes.IntEnum.@Option1: s = @"Option1"; break;
                case global::SerializationTypes.IntEnum.@Option2: s = @"Option2"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.IntEnum");
            }
            return s;
        }

        string Write63_EnumFlags(global::SerializationTypes.EnumFlags v) {
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

        void Write52_TypeA(string n, string ns, global::SerializationTypes.TypeA o, bool isNullable, bool needType) {
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

        string Write47_MyEnum(global::SerializationTypes.MyEnum v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.MyEnum.@One: s = @"One"; break;
                case global::SerializationTypes.MyEnum.@Two: s = @"Two"; break;
                case global::SerializationTypes.MyEnum.@Three: s = @"Three"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.MyEnum");
            }
            return s;
        }

        void Write38_Parameter(string n, string ns, global::Parameter o, bool isNullable, bool needType) {
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
                        Write37_ParameterOfString(n, ns,(global::Parameter<global::System.String>)o, isNullable, true);
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

        void Write37_ParameterOfString(string n, string ns, global::Parameter<global::System.String> o, bool isNullable, bool needType) {
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

        void Write35_TypeWithLinkedProperty(string n, string ns, global::TypeWithLinkedProperty o, bool isNullable, bool needType) {
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
            Write35_TypeWithLinkedProperty(@"Child", @"", ((global::TypeWithLinkedProperty)o.@Child), false, false);
            {
                global::System.Collections.Generic.List<global::TypeWithLinkedProperty> a = (global::System.Collections.Generic.List<global::TypeWithLinkedProperty>)((global::System.Collections.Generic.List<global::TypeWithLinkedProperty>)o.@Children);
                if (a != null){
                    WriteStartElement(@"Children", @"", null, false);
                    for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                        Write35_TypeWithLinkedProperty(@"TypeWithLinkedProperty", @"", ((global::TypeWithLinkedProperty)a[ia]), true, false);
                    }
                    WriteEndElement();
                }
            }
            WriteEndElement(o);
        }

        void Write26_Instrument(string n, string ns, global::Instrument o, bool isNullable, bool needType) {
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
                        Write28_Brass(n, ns,(global::Brass)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Trumpet)) {
                        Write29_Trumpet(n, ns,(global::Trumpet)o, isNullable, true);
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

        void Write29_Trumpet(string n, string ns, global::Trumpet o, bool isNullable, bool needType) {
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

        void Write28_Brass(string n, string ns, global::Brass o, bool isNullable, bool needType) {
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
                        Write29_Trumpet(n, ns,(global::Trumpet)o, isNullable, true);
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

        void Write19_OrderedItem(string n, string ns, global::OrderedItem o, bool isNullable, bool needType) {
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

        void Write18_Address(string n, string ns, global::Address o, bool isNullable, bool needType) {
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

        void Write20_PurchaseOrder(string n, string ns, global::PurchaseOrder o, bool isNullable, bool needType) {
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
            Write18_Address(@"ShipTo", @"http://www.contoso1.com", ((global::Address)o.@ShipTo), false, false);
            WriteElementString(@"OrderDate", @"http://www.contoso1.com", ((global::System.String)o.@OrderDate));
            {
                global::OrderedItem[] a = (global::OrderedItem[])((global::OrderedItem[])o.@OrderedItems);
                if (a != null){
                    WriteStartElement(@"Items", @"http://www.contoso1.com", null, false);
                    for (int ia = 0; ia < a.Length; ia++) {
                        Write19_OrderedItem(@"OrderedItem", @"http://www.contoso1.com", ((global::OrderedItem)a[ia]), true, false);
                    }
                    WriteEndElement();
                }
            }
            WriteElementStringRaw(@"SubTotal", @"http://www.contoso1.com", System.Xml.XmlConvert.ToString((global::System.Decimal)((global::System.Decimal)o.@SubTotal)));
            WriteElementStringRaw(@"ShipCost", @"http://www.contoso1.com", System.Xml.XmlConvert.ToString((global::System.Decimal)((global::System.Decimal)o.@ShipCost)));
            WriteElementStringRaw(@"TotalCost", @"http://www.contoso1.com", System.Xml.XmlConvert.ToString((global::System.Decimal)((global::System.Decimal)o.@TotalCost)));
            WriteEndElement(o);
        }

        void Write21_Address(string n, string ns, global::Address o, bool isNullable, bool needType) {
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

        void Write22_OrderedItem(string n, string ns, global::OrderedItem o, bool isNullable, bool needType) {
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

        void Write23_AliasedTestType(string n, string ns, global::AliasedTestType o, bool isNullable, bool needType) {
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

        void Write25_DerivedClass1(string n, string ns, global::DerivedClass1 o, bool isNullable, bool needType) {
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

        void Write24_BaseClass1(string n, string ns, global::BaseClass1 o, bool isNullable, bool needType) {
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
                        Write25_DerivedClass1(n, ns,(global::DerivedClass1)o, isNullable, true);
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

        void Write27_Orchestra(string n, string ns, global::Orchestra o, bool isNullable, bool needType) {
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
                        Write26_Instrument(@"Instrument", @"", ((global::Instrument)a[ia]), true, false);
                    }
                    WriteEndElement();
                }
            }
            WriteEndElement(o);
        }

        void Write30_Pet(string n, string ns, global::Pet o, bool isNullable, bool needType) {
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

        void Write31_DefaultValuesSetToNaN(string n, string ns, global::DefaultValuesSetToNaN o, bool isNullable, bool needType) {
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

        void Write32_Item(string n, string ns, global::DefaultValuesSetToPositiveInfinity o, bool isNullable, bool needType) {
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

        void Write33_Item(string n, string ns, global::DefaultValuesSetToNegativeInfinity o, bool isNullable, bool needType) {
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

        void Write34_Item(string n, string ns, global::TypeWithMismatchBetweenAttributeAndPropertyType o, bool isNullable, bool needType) {
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

        void Write36_MsgDocumentType(string n, string ns, global::MsgDocumentType o, bool isNullable, bool needType) {
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

        void Write39_RootClass(string n, string ns, global::RootClass o, bool isNullable, bool needType) {
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
                        Write38_Parameter(@"Parameter", @"", ((global::Parameter)a[ia]), true, false);
                    }
                    WriteEndElement();
                }
            }
            WriteEndElement(o);
        }

        void Write40_TypeWithDateTimeStringProperty(string n, string ns, global::SerializationTypes.TypeWithDateTimeStringProperty o, bool isNullable, bool needType) {
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

        void Write42_TypeWithGetSetArrayMembers(string n, string ns, global::SerializationTypes.TypeWithGetSetArrayMembers o, bool isNullable, bool needType) {
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
                        Write41_SimpleType(@"SimpleType", @"", ((global::SerializationTypes.SimpleType)a[ia]), true, false);
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
                        Write41_SimpleType(@"SimpleType", @"", ((global::SerializationTypes.SimpleType)a[ia]), true, false);
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

        void Write43_TypeWithGetOnlyArrayProperties(string n, string ns, global::SerializationTypes.TypeWithGetOnlyArrayProperties o, bool isNullable, bool needType) {
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

        void Write44_StructNotSerializable(string n, string ns, global::SerializationTypes.StructNotSerializable o, bool needType) {
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

        void Write45_TypeWithMyCollectionField(string n, string ns, global::SerializationTypes.TypeWithMyCollectionField o, bool isNullable, bool needType) {
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

        void Write46_Item(string n, string ns, global::SerializationTypes.TypeWithReadOnlyMyCollectionProperty o, bool isNullable, bool needType) {
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

        void Write48_TypeWithEnumMembers(string n, string ns, global::SerializationTypes.TypeWithEnumMembers o, bool isNullable, bool needType) {
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
            WriteElementString(@"F1", @"", Write47_MyEnum(((global::SerializationTypes.MyEnum)o.@F1)));
            WriteElementString(@"P1", @"", Write47_MyEnum(((global::SerializationTypes.MyEnum)o.@P1)));
            WriteEndElement(o);
        }

        void Write49_DCStruct(string n, string ns, global::SerializationTypes.DCStruct o, bool needType) {
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

        void Write50_DCClassWithEnumAndStruct(string n, string ns, global::SerializationTypes.DCClassWithEnumAndStruct o, bool isNullable, bool needType) {
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
            Write49_DCStruct(@"MyStruct", @"", ((global::SerializationTypes.DCStruct)o.@MyStruct), false);
            WriteElementString(@"MyEnum1", @"", Write47_MyEnum(((global::SerializationTypes.MyEnum)o.@MyEnum1)));
            WriteEndElement(o);
        }

        void Write51_BuiltInTypes(string n, string ns, global::SerializationTypes.BuiltInTypes o, bool isNullable, bool needType) {
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

        void Write53_TypeB(string n, string ns, global::SerializationTypes.TypeB o, bool isNullable, bool needType) {
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

        void Write54_TypeHasArrayOfASerializedAsB(string n, string ns, global::SerializationTypes.TypeHasArrayOfASerializedAsB o, bool isNullable, bool needType) {
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
                        Write52_TypeA(@"TypeA", @"", ((global::SerializationTypes.TypeA)a[ia]), true, false);
                    }
                    WriteEndElement();
                }
            }
            WriteEndElement(o);
        }

        void Write55_Item(string n, string ns, global::SerializationTypes.@__TypeNameWithSpecialCharacters漢ñ o, bool isNullable, bool needType) {
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

        void Write58_DerivedClassWithSameProperty2(string n, string ns, global::SerializationTypes.DerivedClassWithSameProperty2 o, bool isNullable, bool needType) {
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

        void Write57_DerivedClassWithSameProperty(string n, string ns, global::SerializationTypes.DerivedClassWithSameProperty o, bool isNullable, bool needType) {
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
                        Write58_DerivedClassWithSameProperty2(n, ns,(global::SerializationTypes.DerivedClassWithSameProperty2)o, isNullable, true);
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

        void Write56_BaseClassWithSamePropertyName(string n, string ns, global::SerializationTypes.BaseClassWithSamePropertyName o, bool isNullable, bool needType) {
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
                        Write57_DerivedClassWithSameProperty(n, ns,(global::SerializationTypes.DerivedClassWithSameProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.DerivedClassWithSameProperty2)) {
                        Write58_DerivedClassWithSameProperty2(n, ns,(global::SerializationTypes.DerivedClassWithSameProperty2)o, isNullable, true);
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

        void Write59_Item(string n, string ns, global::SerializationTypes.TypeWithDateTimePropertyAsXmlTime o, bool isNullable, bool needType) {
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

        void Write60_TypeWithByteArrayAsXmlText(string n, string ns, global::SerializationTypes.TypeWithByteArrayAsXmlText o, bool isNullable, bool needType) {
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

        void Write61_SimpleDC(string n, string ns, global::SerializationTypes.SimpleDC o, bool isNullable, bool needType) {
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

        void Write62_Item(string n, string ns, global::SerializationTypes.TypeWithXmlTextAttributeOnArray o, bool isNullable, bool needType) {
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

        void Write64_ClassImplementsInterface(string n, string ns, global::SerializationTypes.ClassImplementsInterface o, bool isNullable, bool needType) {
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

        void Write65_SomeStruct(string n, string ns, global::SerializationTypes.SomeStruct o, bool needType) {
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

        void Write66_WithStruct(string n, string ns, global::SerializationTypes.WithStruct o, bool isNullable, bool needType) {
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
            Write65_SomeStruct(@"Some", @"", ((global::SerializationTypes.SomeStruct)o.@Some), false);
            WriteEndElement(o);
        }

        void Write69_WithEnums(string n, string ns, global::SerializationTypes.WithEnums o, bool isNullable, bool needType) {
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
            WriteElementString(@"Int", @"", Write67_IntEnum(((global::SerializationTypes.IntEnum)o.@Int)));
            WriteElementString(@"Short", @"", Write68_ShortEnum(((global::SerializationTypes.ShortEnum)o.@Short)));
            WriteEndElement(o);
        }

        void Write70_WithNullables(string n, string ns, global::SerializationTypes.WithNullables o, bool isNullable, bool needType) {
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
                WriteElementString(@"Optional", @"", Write67_IntEnum(((global::SerializationTypes.IntEnum)o.@Optional)));
            }
            else {
                WriteNullTagLiteral(@"Optional", @"");
            }
            if (o.@Optionull != null) {
                WriteElementString(@"Optionull", @"", Write67_IntEnum(((global::SerializationTypes.IntEnum)o.@Optionull)));
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
                Write65_SomeStruct(@"Struct1", @"", ((global::SerializationTypes.SomeStruct)o.@Struct1), false);
            }
            else {
                WriteNullTagLiteral(@"Struct1", @"");
            }
            if (o.@Struct2 != null) {
                Write65_SomeStruct(@"Struct2", @"", ((global::SerializationTypes.SomeStruct)o.@Struct2), false);
            }
            else {
                WriteNullTagLiteral(@"Struct2", @"");
            }
            WriteEndElement(o);
        }

        void Write77_XmlSerializerAttributes(string n, string ns, global::SerializationTypes.XmlSerializerAttributes o, bool isNullable, bool needType) {
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
                        WriteElementString(@"ItemChoiceType", @"", Write76_ItemChoiceType(((global::SerializationTypes.ItemChoiceType)a[ia])));
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

        void Write78_TypeWithAnyAttribute(string n, string ns, global::SerializationTypes.TypeWithAnyAttribute o, bool isNullable, bool needType) {
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

        void Write79_KnownTypesThroughConstructor(string n, string ns, global::SerializationTypes.KnownTypesThroughConstructor o, bool isNullable, bool needType) {
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

        void Write80_SimpleKnownTypeValue(string n, string ns, global::SerializationTypes.SimpleKnownTypeValue o, bool isNullable, bool needType) {
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

        void Write81_TypeWithPropertyNameSpecified(string n, string ns, global::SerializationTypes.TypeWithPropertyNameSpecified o, bool isNullable, bool needType) {
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

        void Write82_TypeWithXmlSchemaFormAttribute(string n, string ns, global::SerializationTypes.TypeWithXmlSchemaFormAttribute o, bool isNullable, bool needType) {
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

        void Write83_Item(string n, string ns, global::SerializationTypes.TypeWithTypeNameInXmlTypeAttribute o, bool isNullable, bool needType) {
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

        void Write85_Item(string n, string ns, global::SerializationTypes.TypeWithNonPublicDefaultConstructor o, bool isNullable, bool needType) {
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

        void Write86_ServerSettings(string n, string ns, global::SerializationTypes.ServerSettings o, bool isNullable, bool needType) {
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

        void Write87_TypeWithXmlQualifiedName(string n, string ns, global::SerializationTypes.TypeWithXmlQualifiedName o, bool isNullable, bool needType) {
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

        void Write88_TypeWith2DArrayProperty2(string n, string ns, global::SerializationTypes.TypeWith2DArrayProperty2 o, bool isNullable, bool needType) {
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
                                    Write41_SimpleType(@"SimpleType", @"", ((global::SerializationTypes.SimpleType)aa[iaa]), true, false);
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

        void Write89_Item(string n, string ns, global::SerializationTypes.TypeWithPropertiesHavingDefaultValue o, bool isNullable, bool needType) {
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

        void Write90_Item(string n, string ns, global::SerializationTypes.TypeWithEnumPropertyHavingDefaultValue o, bool isNullable, bool needType) {
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
                WriteElementString(@"EnumProperty", @"", Write67_IntEnum(((global::SerializationTypes.IntEnum)o.@EnumProperty)));
            }
            WriteEndElement(o);
        }

        void Write91_Item(string n, string ns, global::SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue o, bool isNullable, bool needType) {
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
                WriteElementString(@"EnumProperty", @"", Write63_EnumFlags(((global::SerializationTypes.EnumFlags)o.@EnumProperty)));
            }
            WriteEndElement(o);
        }

        void Write92_TypeWithShouldSerializeMethod(string n, string ns, global::SerializationTypes.TypeWithShouldSerializeMethod o, bool isNullable, bool needType) {
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

        void Write93_Item(string n, string ns, global::SerializationTypes.KnownTypesThroughConstructorWithArrayProperties o, bool isNullable, bool needType) {
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

        void Write94_Item(string n, string ns, global::SerializationTypes.KnownTypesThroughConstructorWithValue o, bool isNullable, bool needType) {
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

        void Write95_Item(string n, string ns, global::SerializationTypes.TypeWithTypesHavingCustomFormatter o, bool isNullable, bool needType) {
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

        void Write97_Item(string n, string ns, global::SerializationTypes.TypeWithArrayPropertyHavingChoice o, bool isNullable, bool needType) {
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

        void Write98_TypeWithFieldsOrdered(string n, string ns, global::SerializationTypes.TypeWithFieldsOrdered o, bool isNullable, bool needType) {
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
            WriteElementStringRaw(@"IntField1", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)o.@IntField1)));
            WriteElementStringRaw(@"IntField2", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)o.@IntField2)));
            WriteElementString(@"StringField2", @"", ((global::System.String)o.@StringField2));
            WriteElementString(@"StringField1", @"", ((global::System.String)o.@StringField1));
            WriteEndElement(o);
        }

        void Write84_Item(string n, string ns, global::SerializationTypes.TypeWithSchemaFormInXmlAttribute o, bool isNullable, bool needType) {
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

        public object Read108_TypeWithXmlElementProperty() {
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

        public object Read109_TypeWithXmlDocumentProperty() {
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

        public object Read110_TypeWithBinaryProperty() {
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

        public object Read111_Item() {
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

        public object Read112_TypeWithTimeSpanProperty() {
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

        public object Read113_Item() {
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

        public object Read114_TypeWithByteProperty() {
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

        public object Read115_TypeWithXmlNodeArrayProperty() {
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

        public object Read116_Animal() {
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

        public object Read117_Dog() {
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

        public object Read118_DogBreed() {
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

        public object Read119_Group() {
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

        public object Read120_Vehicle() {
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

        public object Read121_Employee() {
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

        public object Read122_BaseClass() {
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

        public object Read123_DerivedClass() {
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

        public object Read124_PurchaseOrder() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id18_PurchaseOrder && (object) Reader.NamespaceURI == (object)id19_httpwwwcontoso1com)) {
                        o = Read21_PurchaseOrder(false, true);
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

        public object Read125_Address() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id20_Address && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read22_Address(true, true);
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

        public object Read126_OrderedItem() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id21_OrderedItem && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read23_OrderedItem(true, true);
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

        public object Read127_AliasedTestType() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id22_AliasedTestType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read24_AliasedTestType(true, true);
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

        public object Read128_BaseClass1() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id23_BaseClass1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read25_BaseClass1(true, true);
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

        public object Read129_DerivedClass1() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id24_DerivedClass1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read26_DerivedClass1(true, true);
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

        public object Read130_ArrayOfDateTime() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id25_ArrayOfDateTime && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id26_dateTime && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        public object Read131_Orchestra() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id27_Orchestra && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read28_Orchestra(true, true);
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

        public object Read132_Instrument() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id28_Instrument && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read27_Instrument(true, true);
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

        public object Read133_Brass() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id29_Brass && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read29_Brass(true, true);
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

        public object Read134_Trumpet() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id30_Trumpet && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read30_Trumpet(true, true);
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

        public object Read135_Pet() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id31_Pet && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read31_Pet(true, true);
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

        public object Read136_DefaultValuesSetToNaN() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id32_DefaultValuesSetToNaN && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read32_DefaultValuesSetToNaN(true, true);
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

        public object Read137_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id33_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read33_Item(true, true);
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

        public object Read138_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id34_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read34_Item(true, true);
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

        public object Read139_RootElement() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id35_RootElement && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read35_Item(true, true);
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

        public object Read140_TypeWithLinkedProperty() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id36_TypeWithLinkedProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read36_TypeWithLinkedProperty(true, true);
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

        public object Read141_Document() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id37_Document && (object) Reader.NamespaceURI == (object)id38_httpexamplecom)) {
                        o = Read37_MsgDocumentType(true, true);
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

        public object Read142_RootClass() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id39_RootClass && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read40_RootClass(true, true);
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

        public object Read143_Parameter() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id40_Parameter && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read39_Parameter(true, true);
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

        public object Read144_TypeWithDateTimeStringProperty() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id41_TypeWithDateTimeStringProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read41_TypeWithDateTimeStringProperty(true, true);
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

        public object Read145_SimpleType() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id42_SimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read42_SimpleType(true, true);
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

        public object Read146_TypeWithGetSetArrayMembers() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id43_TypeWithGetSetArrayMembers && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read43_TypeWithGetSetArrayMembers(true, true);
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

        public object Read147_TypeWithGetOnlyArrayProperties() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id44_TypeWithGetOnlyArrayProperties && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read44_TypeWithGetOnlyArrayProperties(true, true);
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

        public object Read148_StructNotSerializable() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id45_StructNotSerializable && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read45_StructNotSerializable(true);
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

        public object Read149_TypeWithMyCollectionField() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id46_TypeWithMyCollectionField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read46_TypeWithMyCollectionField(true, true);
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

        public object Read150_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id47_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read47_Item(true, true);
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

        public object Read151_ArrayOfAnyType() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id48_ArrayOfAnyType && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id49_anyType && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        public object Read152_MyEnum() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id50_MyEnum && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read48_MyEnum(Reader.ReadElementString());
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

        public object Read153_TypeWithEnumMembers() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id51_TypeWithEnumMembers && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read49_TypeWithEnumMembers(true, true);
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

        public object Read154_DCStruct() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id52_DCStruct && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read50_DCStruct(true);
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

        public object Read155_DCClassWithEnumAndStruct() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id53_DCClassWithEnumAndStruct && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read51_DCClassWithEnumAndStruct(true, true);
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

        public object Read156_BuiltInTypes() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id54_BuiltInTypes && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read52_BuiltInTypes(true, true);
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

        public object Read157_TypeA() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id55_TypeA && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read53_TypeA(true, true);
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

        public object Read158_TypeB() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id56_TypeB && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read54_TypeB(true, true);
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

        public object Read159_TypeHasArrayOfASerializedAsB() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id57_TypeHasArrayOfASerializedAsB && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read55_TypeHasArrayOfASerializedAsB(true, true);
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

        public object Read160_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id58_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read56_Item(true, true);
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

        public object Read161_BaseClassWithSamePropertyName() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id59_BaseClassWithSamePropertyName && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read57_BaseClassWithSamePropertyName(true, true);
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

        public object Read162_DerivedClassWithSameProperty() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id60_DerivedClassWithSameProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read58_DerivedClassWithSameProperty(true, true);
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

        public object Read163_DerivedClassWithSameProperty2() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id61_DerivedClassWithSameProperty2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read59_DerivedClassWithSameProperty2(true, true);
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

        public object Read164_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id62_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read60_Item(true, true);
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

        public object Read165_TypeWithByteArrayAsXmlText() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id63_TypeWithByteArrayAsXmlText && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read61_TypeWithByteArrayAsXmlText(true, true);
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

        public object Read166_SimpleDC() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id64_SimpleDC && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read62_SimpleDC(true, true);
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

        public object Read167_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id65_Item && (object) Reader.NamespaceURI == (object)id66_Item)) {
                        o = Read63_Item(false, true);
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

        public object Read168_EnumFlags() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id67_EnumFlags && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read64_EnumFlags(Reader.ReadElementString());
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

        public object Read169_ClassImplementsInterface() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id68_ClassImplementsInterface && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read65_ClassImplementsInterface(true, true);
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

        public object Read170_WithStruct() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id69_WithStruct && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read67_WithStruct(true, true);
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

        public object Read171_SomeStruct() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id70_SomeStruct && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read66_SomeStruct(true);
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

        public object Read172_WithEnums() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id71_WithEnums && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read70_WithEnums(true, true);
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

        public object Read173_WithNullables() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id72_WithNullables && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read74_WithNullables(true, true);
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

        public object Read174_ByteEnum() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id73_ByteEnum && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read75_ByteEnum(Reader.ReadElementString());
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

        public object Read175_SByteEnum() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id74_SByteEnum && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read76_SByteEnum(Reader.ReadElementString());
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

        public object Read176_ShortEnum() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id75_ShortEnum && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read69_ShortEnum(Reader.ReadElementString());
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

        public object Read177_IntEnum() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id76_IntEnum && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read68_IntEnum(Reader.ReadElementString());
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

        public object Read178_UIntEnum() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id77_UIntEnum && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read77_UIntEnum(Reader.ReadElementString());
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

        public object Read179_LongEnum() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id78_LongEnum && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read78_LongEnum(Reader.ReadElementString());
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

        public object Read180_ULongEnum() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id79_ULongEnum && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read79_ULongEnum(Reader.ReadElementString());
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

        public object Read181_AttributeTesting() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id80_AttributeTesting && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read81_XmlSerializerAttributes(false, true);
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

        public object Read182_ItemChoiceType() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id81_ItemChoiceType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read80_ItemChoiceType(Reader.ReadElementString());
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

        public object Read183_TypeWithAnyAttribute() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id82_TypeWithAnyAttribute && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read82_TypeWithAnyAttribute(true, true);
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

        public object Read184_KnownTypesThroughConstructor() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id83_KnownTypesThroughConstructor && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read83_KnownTypesThroughConstructor(true, true);
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

        public object Read185_SimpleKnownTypeValue() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id84_SimpleKnownTypeValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read84_SimpleKnownTypeValue(true, true);
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

        public object Read186_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id85_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = (global::SerializationTypes.ClassImplementingIXmlSerialiable)ReadSerializable(( System.Xml.Serialization.IXmlSerializable)new global::SerializationTypes.ClassImplementingIXmlSerialiable());
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":ClassImplementingIXmlSerialiable");
            }
            return (object)o;
        }

        public object Read187_TypeWithPropertyNameSpecified() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id86_TypeWithPropertyNameSpecified && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read85_TypeWithPropertyNameSpecified(true, true);
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

        public object Read188_TypeWithXmlSchemaFormAttribute() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id87_TypeWithXmlSchemaFormAttribute && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read86_TypeWithXmlSchemaFormAttribute(true, true);
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

        public object Read189_MyXmlType() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id88_MyXmlType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read87_Item(true, true);
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

        public object Read190_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id89_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read88_Item(true, true);
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

        public object Read191_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id90_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read89_Item(true, true);
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

        public object Read192_ServerSettings() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id91_ServerSettings && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read90_ServerSettings(true, true);
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

        public object Read193_TypeWithXmlQualifiedName() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id92_TypeWithXmlQualifiedName && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read91_TypeWithXmlQualifiedName(true, true);
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

        public object Read194_TypeWith2DArrayProperty2() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id93_TypeWith2DArrayProperty2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read92_TypeWith2DArrayProperty2(true, true);
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

        public object Read195_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id94_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read93_Item(true, true);
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

        public object Read196_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id95_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read94_Item(true, true);
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

        public object Read197_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id96_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read95_Item(true, true);
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

        public object Read198_TypeWithShouldSerializeMethod() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id97_TypeWithShouldSerializeMethod && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read96_TypeWithShouldSerializeMethod(true, true);
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

        public object Read199_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id98_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read97_Item(true, true);
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

        public object Read200_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id99_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read98_Item(true, true);
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

        public object Read201_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id100_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read99_Item(true, true);
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

        public object Read202_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id101_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read101_Item(true, true);
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

        public object Read203_MoreChoices() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id102_MoreChoices && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read100_MoreChoices(Reader.ReadElementString());
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

        public object Read204_TypeWithFieldsOrdered() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id103_TypeWithFieldsOrdered && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read102_TypeWithFieldsOrdered(true, true);
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

        public object Read205_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id104_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read103_Item(true, true);
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

        public object Read206_Root() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id105_Root && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read106_Item(true, true);
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

        public object Read207_TypeClashB() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id106_TypeClashB && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read105_TypeNameClash(true, true);
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

        public object Read208_TypeClashA() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id107_TypeClashA && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read104_TypeNameClash(true, true);
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

        public object Read209_Person() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id108_Person && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read107_Person(true, true);
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

        global::Outer.Person Read107_Person(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id108_Person && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Outer.Person o;
            o = new global::Outer.Person();
            bool[] paramsRead = new bool[3];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id109_FirstName && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@FirstName = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id110_MiddleName && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@MiddleName = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id111_LastName && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeNameClashA.TypeNameClash Read104_TypeNameClash(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id107_TypeClashA && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeNameClashA.TypeNameClash o;
            o = new global::SerializationTypes.TypeNameClashA.TypeNameClash();
            bool[] paramsRead = new bool[1];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id112_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeNameClashB.TypeNameClash Read105_TypeNameClash(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id106_TypeClashB && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeNameClashB.TypeNameClash o;
            o = new global::SerializationTypes.TypeNameClashB.TypeNameClash();
            bool[] paramsRead = new bool[1];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id112_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.NamespaceTypeNameClashContainer Read106_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id113_ContainerType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[2];
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
                        if (((object) Reader.LocalName == (object)id114_A && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            a_0 = (global::SerializationTypes.TypeNameClashA.TypeNameClash[])EnsureArrayIndex(a_0, ca_0, typeof(global::SerializationTypes.TypeNameClashA.TypeNameClash));a_0[ca_0++] = Read104_TypeNameClash(false, true);
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id115_B && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            a_1 = (global::SerializationTypes.TypeNameClashB.TypeNameClash[])EnsureArrayIndex(a_1, ca_1, typeof(global::SerializationTypes.TypeNameClashB.TypeNameClash));a_1[ca_1++] = Read105_TypeNameClash(false, true);
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

        global::SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName Read103_Item(bool isNullable, bool checkType) {
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
            global::SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName o;
            o = new global::SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName();
            bool[] paramsRead = new bool[2];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id116_Value1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Value1 = Read1_Object(false, true);
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id117_Value2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id108_Person && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read107_Person(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id113_ContainerType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read106_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id106_TypeClashB && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read105_TypeNameClash(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id107_TypeClashA && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read104_TypeNameClash(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id104_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read103_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id103_TypeWithFieldsOrdered && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read102_TypeWithFieldsOrdered(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id101_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read101_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id100_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read99_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id99_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read98_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id98_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read97_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id97_TypeWithShouldSerializeMethod && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read96_TypeWithShouldSerializeMethod(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id96_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read95_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id95_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read94_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id94_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read93_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id93_TypeWith2DArrayProperty2 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read92_TypeWith2DArrayProperty2(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id92_TypeWithXmlQualifiedName && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read91_TypeWithXmlQualifiedName(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id91_ServerSettings && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read90_ServerSettings(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id90_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read89_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id88_MyXmlType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read87_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id87_TypeWithXmlSchemaFormAttribute && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read86_TypeWithXmlSchemaFormAttribute(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id86_TypeWithPropertyNameSpecified && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read85_TypeWithPropertyNameSpecified(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id84_SimpleKnownTypeValue && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read84_SimpleKnownTypeValue(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id83_KnownTypesThroughConstructor && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read83_KnownTypesThroughConstructor(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id82_TypeWithAnyAttribute && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read82_TypeWithAnyAttribute(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id118_XmlSerializerAttributes && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read81_XmlSerializerAttributes(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id72_WithNullables && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read74_WithNullables(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id71_WithEnums && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read70_WithEnums(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id69_WithStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read67_WithStruct(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id70_SomeStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read66_SomeStruct(false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id68_ClassImplementsInterface && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read65_ClassImplementsInterface(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id65_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id66_Item))
                        return Read63_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id64_SimpleDC && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read62_SimpleDC(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id63_TypeWithByteArrayAsXmlText && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read61_TypeWithByteArrayAsXmlText(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id62_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read60_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id59_BaseClassWithSamePropertyName && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read57_BaseClassWithSamePropertyName(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id60_DerivedClassWithSameProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read58_DerivedClassWithSameProperty(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id61_DerivedClassWithSameProperty2 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read59_DerivedClassWithSameProperty2(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id58_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read56_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id57_TypeHasArrayOfASerializedAsB && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read55_TypeHasArrayOfASerializedAsB(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id56_TypeB && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read54_TypeB(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id55_TypeA && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read53_TypeA(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id54_BuiltInTypes && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read52_BuiltInTypes(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id53_DCClassWithEnumAndStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read51_DCClassWithEnumAndStruct(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id52_DCStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read50_DCStruct(false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id51_TypeWithEnumMembers && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read49_TypeWithEnumMembers(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id47_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read47_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id46_TypeWithMyCollectionField && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read46_TypeWithMyCollectionField(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id45_StructNotSerializable && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read45_StructNotSerializable(false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id44_TypeWithGetOnlyArrayProperties && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read44_TypeWithGetOnlyArrayProperties(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id43_TypeWithGetSetArrayMembers && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read43_TypeWithGetSetArrayMembers(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id42_SimpleType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read42_SimpleType(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id41_TypeWithDateTimeStringProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read41_TypeWithDateTimeStringProperty(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id39_RootClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read40_RootClass(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id40_Parameter && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read39_Parameter(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id119_ParameterOfString && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read38_ParameterOfString(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id120_MsgDocumentType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id38_httpexamplecom))
                        return Read37_MsgDocumentType(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id36_TypeWithLinkedProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read36_TypeWithLinkedProperty(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id121_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read35_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id34_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read34_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id33_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read33_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id32_DefaultValuesSetToNaN && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read32_DefaultValuesSetToNaN(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id31_Pet && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read31_Pet(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id27_Orchestra && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read28_Orchestra(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id28_Instrument && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read27_Instrument(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id29_Brass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read29_Brass(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id30_Trumpet && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read30_Trumpet(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id23_BaseClass1 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read25_BaseClass1(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id24_DerivedClass1 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read26_DerivedClass1(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id22_AliasedTestType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read24_AliasedTestType(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id21_OrderedItem && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read23_OrderedItem(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id20_Address && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read22_Address(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id18_PurchaseOrder && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id19_httpwwwcontoso1com))
                        return Read21_PurchaseOrder(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id21_OrderedItem && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id19_httpwwwcontoso1com))
                        return Read20_OrderedItem(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id20_Address && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id19_httpwwwcontoso1com))
                        return Read19_Address(isNullable, false);
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id122_ArrayOfOrderedItem && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id19_httpwwwcontoso1com)) {
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
                                            if (((object) Reader.LocalName == (object)id21_OrderedItem && (object) Reader.NamespaceURI == (object)id19_httpwwwcontoso1com)) {
                                                z_0_0 = (global::OrderedItem[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::OrderedItem));z_0_0[cz_0_0++] = Read20_OrderedItem(true, true);
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id123_ArrayOfInt && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id124_int && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id125_ArrayOfString && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id126_string && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id127_ArrayOfDouble && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id128_double && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id25_ArrayOfDateTime && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id26_dateTime && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id129_ArrayOfInstrument && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id28_Instrument && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                z_0_0 = (global::Instrument[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::Instrument));z_0_0[cz_0_0++] = Read27_Instrument(true, true);
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id130_ArrayOfTypeWithLinkedProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id36_TypeWithLinkedProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                if ((object)(z_0_0) == null) Reader.Skip(); else z_0_0.Add(Read36_TypeWithLinkedProperty(true, true));
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id131_ArrayOfParameter && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id40_Parameter && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                if ((object)(z_0_0) == null) Reader.Skip(); else z_0_0.Add(Read39_Parameter(true, true));
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id132_ArrayOfSimpleType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id42_SimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                z_0_0 = (global::SerializationTypes.SimpleType[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::SerializationTypes.SimpleType));z_0_0[cz_0_0++] = Read42_SimpleType(true, true);
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id48_ArrayOfAnyType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id49_anyType && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id50_MyEnum && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read48_MyEnum(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id133_ArrayOfTypeA && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id55_TypeA && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                z_0_0 = (global::SerializationTypes.TypeA[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::SerializationTypes.TypeA));z_0_0[cz_0_0++] = Read53_TypeA(true, true);
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id67_EnumFlags && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read64_EnumFlags(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id76_IntEnum && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read68_IntEnum(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id75_ShortEnum && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read69_ShortEnum(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id73_ByteEnum && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read75_ByteEnum(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id74_SByteEnum && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read76_SByteEnum(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id77_UIntEnum && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read77_UIntEnum(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id78_LongEnum && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read78_LongEnum(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id79_ULongEnum && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read79_ULongEnum(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id81_ItemChoiceType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read80_ItemChoiceType(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id134_ArrayOfItemChoiceType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id81_ItemChoiceType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                {
                                                    z_0_0 = (global::SerializationTypes.ItemChoiceType[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::SerializationTypes.ItemChoiceType));z_0_0[cz_0_0++] = Read80_ItemChoiceType(Reader.ReadElementString());
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id125_ArrayOfString && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id135_httpmynamespace)) {
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
                                            if (((object) Reader.LocalName == (object)id126_string && (object) Reader.NamespaceURI == (object)id135_httpmynamespace)) {
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id136_ArrayOfString1 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id137_NoneParameter && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id138_ArrayOfBoolean && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id139_QualifiedParameter && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id140_ArrayOfArrayOfSimpleType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id42_SimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                                    if (((object) Reader.LocalName == (object)id42_SimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                                        z_0_0_0 = (global::SerializationTypes.SimpleType[])EnsureArrayIndex(z_0_0_0, cz_0_0_0, typeof(global::SerializationTypes.SimpleType));z_0_0_0[cz_0_0_0++] = Read42_SimpleType(true, true);
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id102_MoreChoices && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read100_MoreChoices(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    return ReadTypedPrimitive((System.Xml.XmlQualifiedName)xsiType);
                }
            }
            if (isNull) return null;
            global::System.Object o;
            o = new global::System.Object();
            bool[] paramsRead = new bool[0];
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

        global::SerializationTypes.MoreChoices Read100_MoreChoices(string s) {
            switch (s) {
                case @"None": return global::SerializationTypes.MoreChoices.@None;
                case @"Item": return global::SerializationTypes.MoreChoices.@Item;
                case @"Amount": return global::SerializationTypes.MoreChoices.@Amount;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.MoreChoices));
            }
        }

        global::SerializationTypes.SimpleType Read42_SimpleType(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id42_SimpleType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.SimpleType o;
            o = new global::SerializationTypes.SimpleType();
            bool[] paramsRead = new bool[2];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id141_P1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@P1 = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id142_P2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.ItemChoiceType Read80_ItemChoiceType(string s) {
            switch (s) {
                case @"None": return global::SerializationTypes.ItemChoiceType.@None;
                case @"Word": return global::SerializationTypes.ItemChoiceType.@Word;
                case @"Number": return global::SerializationTypes.ItemChoiceType.@Number;
                case @"DecimalNumber": return global::SerializationTypes.ItemChoiceType.@DecimalNumber;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.ItemChoiceType));
            }
        }

        global::SerializationTypes.ULongEnum Read79_ULongEnum(string s) {
            switch (s) {
                case @"Option0": return global::SerializationTypes.ULongEnum.@Option0;
                case @"Option1": return global::SerializationTypes.ULongEnum.@Option1;
                case @"Option2": return global::SerializationTypes.ULongEnum.@Option2;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.ULongEnum));
            }
        }

        global::SerializationTypes.LongEnum Read78_LongEnum(string s) {
            switch (s) {
                case @"Option0": return global::SerializationTypes.LongEnum.@Option0;
                case @"Option1": return global::SerializationTypes.LongEnum.@Option1;
                case @"Option2": return global::SerializationTypes.LongEnum.@Option2;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.LongEnum));
            }
        }

        global::SerializationTypes.UIntEnum Read77_UIntEnum(string s) {
            switch (s) {
                case @"Option0": return global::SerializationTypes.UIntEnum.@Option0;
                case @"Option1": return global::SerializationTypes.UIntEnum.@Option1;
                case @"Option2": return global::SerializationTypes.UIntEnum.@Option2;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.UIntEnum));
            }
        }

        global::SerializationTypes.SByteEnum Read76_SByteEnum(string s) {
            switch (s) {
                case @"Option0": return global::SerializationTypes.SByteEnum.@Option0;
                case @"Option1": return global::SerializationTypes.SByteEnum.@Option1;
                case @"Option2": return global::SerializationTypes.SByteEnum.@Option2;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.SByteEnum));
            }
        }

        global::SerializationTypes.ByteEnum Read75_ByteEnum(string s) {
            switch (s) {
                case @"Option0": return global::SerializationTypes.ByteEnum.@Option0;
                case @"Option1": return global::SerializationTypes.ByteEnum.@Option1;
                case @"Option2": return global::SerializationTypes.ByteEnum.@Option2;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.ByteEnum));
            }
        }

        global::SerializationTypes.ShortEnum Read69_ShortEnum(string s) {
            switch (s) {
                case @"Option0": return global::SerializationTypes.ShortEnum.@Option0;
                case @"Option1": return global::SerializationTypes.ShortEnum.@Option1;
                case @"Option2": return global::SerializationTypes.ShortEnum.@Option2;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.ShortEnum));
            }
        }

        global::SerializationTypes.IntEnum Read68_IntEnum(string s) {
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

        global::SerializationTypes.EnumFlags Read64_EnumFlags(string s) {
            return (global::SerializationTypes.EnumFlags)ToEnum(s, EnumFlagsValues, @"global::SerializationTypes.EnumFlags");
        }

        global::SerializationTypes.TypeA Read53_TypeA(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id55_TypeA && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeA o;
            o = new global::SerializationTypes.TypeA();
            bool[] paramsRead = new bool[1];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id112_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.MyEnum Read48_MyEnum(string s) {
            switch (s) {
                case @"One": return global::SerializationTypes.MyEnum.@One;
                case @"Two": return global::SerializationTypes.MyEnum.@Two;
                case @"Three": return global::SerializationTypes.MyEnum.@Three;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.MyEnum));
            }
        }

        global::Parameter Read39_Parameter(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id40_Parameter && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id119_ParameterOfString && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read38_ParameterOfString(isNullable, false);
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Parameter o;
            o = new global::Parameter();
            bool[] paramsRead = new bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id112_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::Parameter<global::System.String> Read38_ParameterOfString(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id119_ParameterOfString && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Parameter<global::System.String> o;
            o = new global::Parameter<global::System.String>();
            bool[] paramsRead = new bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id112_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id143_Value && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::TypeWithLinkedProperty Read36_TypeWithLinkedProperty(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id36_TypeWithLinkedProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[2];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id144_Child && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Child = Read36_TypeWithLinkedProperty(false, true);
                            paramsRead[0] = true;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id145_Children && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id36_TypeWithLinkedProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    if ((object)(a_1_0) == null) Reader.Skip(); else a_1_0.Add(Read36_TypeWithLinkedProperty(true, true));
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

        global::Instrument Read27_Instrument(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id28_Instrument && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id29_Brass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read29_Brass(isNullable, false);
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id30_Trumpet && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read30_Trumpet(isNullable, false);
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Instrument o;
            o = new global::Instrument();
            bool[] paramsRead = new bool[1];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id112_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::Trumpet Read30_Trumpet(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id30_Trumpet && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Trumpet o;
            o = new global::Trumpet();
            bool[] paramsRead = new bool[3];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id112_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Name = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id146_IsValved && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@IsValved = System.Xml.XmlConvert.ToBoolean(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id147_Modulation && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::Brass Read29_Brass(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id29_Brass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id30_Trumpet && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read30_Trumpet(isNullable, false);
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Brass o;
            o = new global::Brass();
            bool[] paramsRead = new bool[2];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id112_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Name = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id146_IsValved && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::OrderedItem Read20_OrderedItem(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id21_OrderedItem && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id19_httpwwwcontoso1com)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::OrderedItem o;
            o = new global::OrderedItem();
            bool[] paramsRead = new bool[5];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id148_ItemName && (object) Reader.NamespaceURI == (object)id19_httpwwwcontoso1com)) {
                            {
                                o.@ItemName = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id149_Description && (object) Reader.NamespaceURI == (object)id19_httpwwwcontoso1com)) {
                            {
                                o.@Description = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id150_UnitPrice && (object) Reader.NamespaceURI == (object)id19_httpwwwcontoso1com)) {
                            {
                                o.@UnitPrice = System.Xml.XmlConvert.ToDecimal(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id151_Quantity && (object) Reader.NamespaceURI == (object)id19_httpwwwcontoso1com)) {
                            {
                                o.@Quantity = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id152_LineTotal && (object) Reader.NamespaceURI == (object)id19_httpwwwcontoso1com)) {
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
            bool[] paramsRead = new bool[1];
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
            bool[] paramsRead = new bool[1];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id37_Document && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[2];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id153_BinaryHexContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@BinaryHexContent = ToByteArrayHex(false);
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id154_Base64Content && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[5];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id155_DTO && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id156_DTO2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id157_DefaultDTO && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id158_NullableDTO && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@NullableDTO = Read5_NullableOfDateTimeOffset(true);
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id159_NullableDefaultDTO && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[1];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id160_TimeSpanProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[2];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id160_TimeSpanProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id161_TimeSpanProperty2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[1];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id162_ByteProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[1];
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
            bool[] paramsRead = new bool[3];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id163_Age && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Age = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id112_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Name = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id164_Breed && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[2];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id163_Age && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Age = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id112_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[1];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id165_LicenseNumber && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[2];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id166_GroupName && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@GroupName = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id167_GroupVehicle && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[1];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id168_EmployeeName && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[2];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id143_Value && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Value = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id169_value && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[2];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id143_Value && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Value = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id169_value && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::Address Read19_Address(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id20_Address && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id19_httpwwwcontoso1com)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Address o;
            o = new global::Address();
            bool[] paramsRead = new bool[5];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id112_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id170_Line1 && (object) Reader.NamespaceURI == (object)id19_httpwwwcontoso1com)) {
                            {
                                o.@Line1 = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id171_City && (object) Reader.NamespaceURI == (object)id19_httpwwwcontoso1com)) {
                            {
                                o.@City = Reader.ReadElementString();
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id172_State && (object) Reader.NamespaceURI == (object)id19_httpwwwcontoso1com)) {
                            {
                                o.@State = Reader.ReadElementString();
                            }
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id173_Zip && (object) Reader.NamespaceURI == (object)id19_httpwwwcontoso1com)) {
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

        global::PurchaseOrder Read21_PurchaseOrder(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id18_PurchaseOrder && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id19_httpwwwcontoso1com)) {
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
            bool[] paramsRead = new bool[6];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id174_ShipTo && (object) Reader.NamespaceURI == (object)id19_httpwwwcontoso1com)) {
                            o.@ShipTo = Read19_Address(false, true);
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id175_OrderDate && (object) Reader.NamespaceURI == (object)id19_httpwwwcontoso1com)) {
                            {
                                o.@OrderDate = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id176_Items && (object) Reader.NamespaceURI == (object)id19_httpwwwcontoso1com)) {
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
                                                if (((object) Reader.LocalName == (object)id21_OrderedItem && (object) Reader.NamespaceURI == (object)id19_httpwwwcontoso1com)) {
                                                    a_2_0 = (global::OrderedItem[])EnsureArrayIndex(a_2_0, ca_2_0, typeof(global::OrderedItem));a_2_0[ca_2_0++] = Read20_OrderedItem(true, true);
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
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id177_SubTotal && (object) Reader.NamespaceURI == (object)id19_httpwwwcontoso1com)) {
                            {
                                o.@SubTotal = System.Xml.XmlConvert.ToDecimal(Reader.ReadElementString());
                            }
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id178_ShipCost && (object) Reader.NamespaceURI == (object)id19_httpwwwcontoso1com)) {
                            {
                                o.@ShipCost = System.Xml.XmlConvert.ToDecimal(Reader.ReadElementString());
                            }
                            paramsRead[4] = true;
                            break;
                        }
                        if (!paramsRead[5] && ((object) Reader.LocalName == (object)id179_TotalCost && (object) Reader.NamespaceURI == (object)id19_httpwwwcontoso1com)) {
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

        global::Address Read22_Address(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id20_Address && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Address o;
            o = new global::Address();
            bool[] paramsRead = new bool[5];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id112_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id170_Line1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Line1 = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id171_City && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@City = Reader.ReadElementString();
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id172_State && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@State = Reader.ReadElementString();
                            }
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id173_Zip && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::OrderedItem Read23_OrderedItem(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id21_OrderedItem && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::OrderedItem o;
            o = new global::OrderedItem();
            bool[] paramsRead = new bool[5];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id148_ItemName && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@ItemName = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id149_Description && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Description = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id150_UnitPrice && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@UnitPrice = System.Xml.XmlConvert.ToDecimal(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id151_Quantity && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Quantity = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id152_LineTotal && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::AliasedTestType Read24_AliasedTestType(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id22_AliasedTestType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::AliasedTestType o;
            o = new global::AliasedTestType();
            bool[] paramsRead = new bool[1];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id180_X && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id124_int && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id181_Y && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id126_string && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id182_Z && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id128_double && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::DerivedClass1 Read26_DerivedClass1(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id24_DerivedClass1 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[1];
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
                        if (((object) Reader.LocalName == (object)id183_Prop && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::BaseClass1 Read25_BaseClass1(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id23_BaseClass1 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id24_DerivedClass1 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read26_DerivedClass1(isNullable, false);
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::BaseClass1 o;
            o = new global::BaseClass1();
            if ((object)(o.@Prop) == null) o.@Prop = new global::MyCollection1();
            global::MyCollection1 a_0 = (global::MyCollection1)o.@Prop;
            bool[] paramsRead = new bool[1];
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
                        if (((object) Reader.LocalName == (object)id183_Prop && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::Orchestra Read28_Orchestra(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id27_Orchestra && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[1];
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
                        if (((object) Reader.LocalName == (object)id184_Instruments && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id28_Instrument && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    a_0_0 = (global::Instrument[])EnsureArrayIndex(a_0_0, ca_0_0, typeof(global::Instrument));a_0_0[ca_0_0++] = Read27_Instrument(true, true);
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

        global::Pet Read31_Pet(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id31_Pet && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Pet o;
            o = new global::Pet();
            bool[] paramsRead = new bool[2];
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
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id185_Comment2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::DefaultValuesSetToNaN Read32_DefaultValuesSetToNaN(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id32_DefaultValuesSetToNaN && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::DefaultValuesSetToNaN o;
            o = new global::DefaultValuesSetToNaN();
            bool[] paramsRead = new bool[4];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id186_DoubleField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@DoubleField = System.Xml.XmlConvert.ToDouble(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id187_SingleField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@SingleField = System.Xml.XmlConvert.ToSingle(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id188_DoubleProp && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@DoubleProp = System.Xml.XmlConvert.ToDouble(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id189_FloatProp && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::DefaultValuesSetToPositiveInfinity Read33_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id33_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::DefaultValuesSetToPositiveInfinity o;
            o = new global::DefaultValuesSetToPositiveInfinity();
            bool[] paramsRead = new bool[4];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id186_DoubleField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@DoubleField = System.Xml.XmlConvert.ToDouble(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id187_SingleField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@SingleField = System.Xml.XmlConvert.ToSingle(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id188_DoubleProp && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@DoubleProp = System.Xml.XmlConvert.ToDouble(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id189_FloatProp && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::DefaultValuesSetToNegativeInfinity Read34_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id34_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::DefaultValuesSetToNegativeInfinity o;
            o = new global::DefaultValuesSetToNegativeInfinity();
            bool[] paramsRead = new bool[4];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id186_DoubleField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@DoubleField = System.Xml.XmlConvert.ToDouble(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id187_SingleField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@SingleField = System.Xml.XmlConvert.ToSingle(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id188_DoubleProp && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@DoubleProp = System.Xml.XmlConvert.ToDouble(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id189_FloatProp && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::TypeWithMismatchBetweenAttributeAndPropertyType Read35_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id121_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::TypeWithMismatchBetweenAttributeAndPropertyType o;
            o = new global::TypeWithMismatchBetweenAttributeAndPropertyType();
            bool[] paramsRead = new bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id190_IntValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::MsgDocumentType Read37_MsgDocumentType(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id120_MsgDocumentType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id38_httpexamplecom)) {
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
            bool[] paramsRead = new bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id191_id && (object) Reader.NamespaceURI == (object)id2_Item)) {
                    o.@Id = CollapseWhitespace(Reader.Value);
                    paramsRead[0] = true;
                }
                else if (((object) Reader.LocalName == (object)id192_refs && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::RootClass Read40_RootClass(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id39_RootClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[1];
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
                        if (((object) Reader.LocalName == (object)id193_Parameters && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id40_Parameter && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    if ((object)(a_0_0) == null) Reader.Skip(); else a_0_0.Add(Read39_Parameter(true, true));
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

        global::SerializationTypes.TypeWithDateTimeStringProperty Read41_TypeWithDateTimeStringProperty(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id41_TypeWithDateTimeStringProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithDateTimeStringProperty o;
            o = new global::SerializationTypes.TypeWithDateTimeStringProperty();
            bool[] paramsRead = new bool[2];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id194_DateTimeString && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@DateTimeString = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id195_CurrentDateTime && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithGetSetArrayMembers Read43_TypeWithGetSetArrayMembers(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id43_TypeWithGetSetArrayMembers && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[4];
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
                        if (((object) Reader.LocalName == (object)id196_F1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id42_SimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    a_0_0 = (global::SerializationTypes.SimpleType[])EnsureArrayIndex(a_0_0, ca_0_0, typeof(global::SerializationTypes.SimpleType));a_0_0[ca_0_0++] = Read42_SimpleType(true, true);
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
                        if (((object) Reader.LocalName == (object)id197_F2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id124_int && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (((object) Reader.LocalName == (object)id141_P1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id42_SimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    a_2_0 = (global::SerializationTypes.SimpleType[])EnsureArrayIndex(a_2_0, ca_2_0, typeof(global::SerializationTypes.SimpleType));a_2_0[ca_2_0++] = Read42_SimpleType(true, true);
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
                        if (((object) Reader.LocalName == (object)id142_P2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id124_int && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithGetOnlyArrayProperties Read44_TypeWithGetOnlyArrayProperties(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id44_TypeWithGetOnlyArrayProperties && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithGetOnlyArrayProperties o;
            o = new global::SerializationTypes.TypeWithGetOnlyArrayProperties();
            bool[] paramsRead = new bool[0];
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

        global::SerializationTypes.StructNotSerializable Read45_StructNotSerializable(bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id45_StructNotSerializable && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[1];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id169_value && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithMyCollectionField Read46_TypeWithMyCollectionField(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id46_TypeWithMyCollectionField && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[1];
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
                        if (((object) Reader.LocalName == (object)id198_Collection && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id126_string && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithReadOnlyMyCollectionProperty Read47_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id47_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithReadOnlyMyCollectionProperty o;
            o = new global::SerializationTypes.TypeWithReadOnlyMyCollectionProperty();
            global::SerializationTypes.MyCollection<global::System.String> a_0 = (global::SerializationTypes.MyCollection<global::System.String>)o.@Collection;
            bool[] paramsRead = new bool[1];
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
                        if (((object) Reader.LocalName == (object)id198_Collection && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id126_string && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithEnumMembers Read49_TypeWithEnumMembers(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id51_TypeWithEnumMembers && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithEnumMembers o;
            o = new global::SerializationTypes.TypeWithEnumMembers();
            bool[] paramsRead = new bool[2];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id196_F1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@F1 = Read48_MyEnum(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id141_P1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@P1 = Read48_MyEnum(Reader.ReadElementString());
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

        global::SerializationTypes.DCStruct Read50_DCStruct(bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id52_DCStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[1];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id199_Data && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.DCClassWithEnumAndStruct Read51_DCClassWithEnumAndStruct(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id53_DCClassWithEnumAndStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.DCClassWithEnumAndStruct o;
            o = new global::SerializationTypes.DCClassWithEnumAndStruct();
            bool[] paramsRead = new bool[2];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id200_MyStruct && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@MyStruct = Read50_DCStruct(true);
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id201_MyEnum1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@MyEnum1 = Read48_MyEnum(Reader.ReadElementString());
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

        global::SerializationTypes.BuiltInTypes Read52_BuiltInTypes(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id54_BuiltInTypes && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.BuiltInTypes o;
            o = new global::SerializationTypes.BuiltInTypes();
            bool[] paramsRead = new bool[1];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id202_ByteArray && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeB Read54_TypeB(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id56_TypeB && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeB o;
            o = new global::SerializationTypes.TypeB();
            bool[] paramsRead = new bool[1];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id112_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeHasArrayOfASerializedAsB Read55_TypeHasArrayOfASerializedAsB(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id57_TypeHasArrayOfASerializedAsB && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[1];
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
                        if (((object) Reader.LocalName == (object)id176_Items && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id55_TypeA && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    a_0_0 = (global::SerializationTypes.TypeA[])EnsureArrayIndex(a_0_0, ca_0_0, typeof(global::SerializationTypes.TypeA));a_0_0[ca_0_0++] = Read53_TypeA(true, true);
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

        global::SerializationTypes.@__TypeNameWithSpecialCharacters漢ñ Read56_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id58_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.@__TypeNameWithSpecialCharacters漢ñ o;
            o = new global::SerializationTypes.@__TypeNameWithSpecialCharacters漢ñ();
            bool[] paramsRead = new bool[1];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id203_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.DerivedClassWithSameProperty2 Read59_DerivedClassWithSameProperty2(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id61_DerivedClassWithSameProperty2 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[4];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id204_StringProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@StringProperty = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id205_IntProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@IntProperty = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id206_DateTimeProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@DateTimeProperty = ToDateTime(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id207_ListProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id126_string && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.DerivedClassWithSameProperty Read58_DerivedClassWithSameProperty(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id60_DerivedClassWithSameProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id61_DerivedClassWithSameProperty2 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read59_DerivedClassWithSameProperty2(isNullable, false);
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.DerivedClassWithSameProperty o;
            o = new global::SerializationTypes.DerivedClassWithSameProperty();
            if ((object)(o.@ListProperty) == null) o.@ListProperty = new global::System.Collections.Generic.List<global::System.String>();
            global::System.Collections.Generic.List<global::System.String> a_3 = (global::System.Collections.Generic.List<global::System.String>)o.@ListProperty;
            bool[] paramsRead = new bool[4];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id204_StringProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@StringProperty = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id205_IntProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@IntProperty = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id206_DateTimeProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@DateTimeProperty = ToDateTime(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id207_ListProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id126_string && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.BaseClassWithSamePropertyName Read57_BaseClassWithSamePropertyName(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id59_BaseClassWithSamePropertyName && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id60_DerivedClassWithSameProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read58_DerivedClassWithSameProperty(isNullable, false);
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id61_DerivedClassWithSameProperty2 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read59_DerivedClassWithSameProperty2(isNullable, false);
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.BaseClassWithSamePropertyName o;
            o = new global::SerializationTypes.BaseClassWithSamePropertyName();
            if ((object)(o.@ListProperty) == null) o.@ListProperty = new global::System.Collections.Generic.List<global::System.String>();
            global::System.Collections.Generic.List<global::System.String> a_3 = (global::System.Collections.Generic.List<global::System.String>)o.@ListProperty;
            bool[] paramsRead = new bool[4];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id204_StringProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@StringProperty = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id205_IntProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@IntProperty = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id206_DateTimeProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@DateTimeProperty = ToDateTime(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id207_ListProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id126_string && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithDateTimePropertyAsXmlTime Read60_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id62_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithDateTimePropertyAsXmlTime o;
            o = new global::SerializationTypes.TypeWithDateTimePropertyAsXmlTime();
            bool[] paramsRead = new bool[1];
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

        global::SerializationTypes.TypeWithByteArrayAsXmlText Read61_TypeWithByteArrayAsXmlText(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id63_TypeWithByteArrayAsXmlText && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithByteArrayAsXmlText o;
            o = new global::SerializationTypes.TypeWithByteArrayAsXmlText();
            bool[] paramsRead = new bool[1];
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

        global::SerializationTypes.SimpleDC Read62_SimpleDC(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id64_SimpleDC && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.SimpleDC o;
            o = new global::SerializationTypes.SimpleDC();
            bool[] paramsRead = new bool[1];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id199_Data && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithXmlTextAttributeOnArray Read63_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id65_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id66_Item)) {
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
            bool[] paramsRead = new bool[1];
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

        global::SerializationTypes.ClassImplementsInterface Read65_ClassImplementsInterface(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id68_ClassImplementsInterface && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.ClassImplementsInterface o;
            o = new global::SerializationTypes.ClassImplementsInterface();
            bool[] paramsRead = new bool[4];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id208_ClassID && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@ClassID = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id209_DisplayName && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@DisplayName = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id210_Id && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Id = Reader.ReadElementString();
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id211_IsLoaded && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.SomeStruct Read66_SomeStruct(bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id70_SomeStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[2];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id114_A && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@A = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id115_B && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.WithStruct Read67_WithStruct(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id69_WithStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.WithStruct o;
            o = new global::SerializationTypes.WithStruct();
            bool[] paramsRead = new bool[1];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id212_Some && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Some = Read66_SomeStruct(true);
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

        global::SerializationTypes.WithEnums Read70_WithEnums(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id71_WithEnums && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.WithEnums o;
            o = new global::SerializationTypes.WithEnums();
            bool[] paramsRead = new bool[2];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id213_Int && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Int = Read68_IntEnum(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id214_Short && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Short = Read69_ShortEnum(Reader.ReadElementString());
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

        global::SerializationTypes.WithNullables Read74_WithNullables(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id72_WithNullables && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.WithNullables o;
            o = new global::SerializationTypes.WithNullables();
            bool[] paramsRead = new bool[6];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id215_Optional && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Optional = Read71_NullableOfIntEnum(true);
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id216_Optionull && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Optionull = Read71_NullableOfIntEnum(true);
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id217_OptionalInt && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@OptionalInt = Read72_NullableOfInt32(true);
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id218_OptionullInt && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@OptionullInt = Read72_NullableOfInt32(true);
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id219_Struct1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Struct1 = Read73_NullableOfSomeStruct(true);
                            paramsRead[4] = true;
                            break;
                        }
                        if (!paramsRead[5] && ((object) Reader.LocalName == (object)id220_Struct2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Struct2 = Read73_NullableOfSomeStruct(true);
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

        global::System.Nullable<global::SerializationTypes.SomeStruct> Read73_NullableOfSomeStruct(bool checkType) {
            global::System.Nullable<global::SerializationTypes.SomeStruct> o = default(global::System.Nullable<global::SerializationTypes.SomeStruct>);
            if (ReadNull())
                return o;
            o = Read66_SomeStruct(true);
            return o;
        }

        global::System.Nullable<global::System.Int32> Read72_NullableOfInt32(bool checkType) {
            global::System.Nullable<global::System.Int32> o = default(global::System.Nullable<global::System.Int32>);
            if (ReadNull())
                return o;
            {
                o = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
            }
            return o;
        }

        global::System.Nullable<global::SerializationTypes.IntEnum> Read71_NullableOfIntEnum(bool checkType) {
            global::System.Nullable<global::SerializationTypes.IntEnum> o = default(global::System.Nullable<global::SerializationTypes.IntEnum>);
            if (ReadNull())
                return o;
            {
                o = Read68_IntEnum(Reader.ReadElementString());
            }
            return o;
        }

        global::SerializationTypes.XmlSerializerAttributes Read81_XmlSerializerAttributes(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id118_XmlSerializerAttributes && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[8];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[6] && ((object) Reader.LocalName == (object)id221_XmlAttributeName && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id222_Word && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@MyChoice = Reader.ReadElementString();
                            }
                            o.@EnumType = global::SerializationTypes.ItemChoiceType.@Word;
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id223_Number && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@MyChoice = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            o.@EnumType = global::SerializationTypes.ItemChoiceType.@Number;
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id224_DecimalNumber && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@MyChoice = System.Xml.XmlConvert.ToDouble(Reader.ReadElementString());
                            }
                            o.@EnumType = global::SerializationTypes.ItemChoiceType.@DecimalNumber;
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id225_XmlIncludeProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@XmlIncludeProperty = Read1_Object(false, true);
                            paramsRead[1] = true;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id226_XmlEnumProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id81_ItemChoiceType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    {
                                                        a_2_0 = (global::SerializationTypes.ItemChoiceType[])EnsureArrayIndex(a_2_0, ca_2_0, typeof(global::SerializationTypes.ItemChoiceType));a_2_0[ca_2_0++] = Read80_ItemChoiceType(Reader.ReadElementString());
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
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id227_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@XmlNamespaceDeclarationsProperty = Reader.ReadElementString();
                            }
                            paramsRead[4] = true;
                            break;
                        }
                        if (!paramsRead[5] && ((object) Reader.LocalName == (object)id228_XmlElementPropertyNode && (object) Reader.NamespaceURI == (object)id229_httpelement)) {
                            {
                                o.@XmlElementProperty = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[5] = true;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id230_CustomXmlArrayProperty && (object) Reader.NamespaceURI == (object)id135_httpmynamespace)) {
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
                                                if (((object) Reader.LocalName == (object)id126_string && (object) Reader.NamespaceURI == (object)id135_httpmynamespace)) {
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

        global::SerializationTypes.TypeWithAnyAttribute Read82_TypeWithAnyAttribute(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id82_TypeWithAnyAttribute && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[3];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[1] && ((object) Reader.LocalName == (object)id205_IntProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id112_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.KnownTypesThroughConstructor Read83_KnownTypesThroughConstructor(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id83_KnownTypesThroughConstructor && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.KnownTypesThroughConstructor o;
            o = new global::SerializationTypes.KnownTypesThroughConstructor();
            bool[] paramsRead = new bool[2];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id231_EnumValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@EnumValue = Read1_Object(false, true);
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id232_SimpleTypeValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.SimpleKnownTypeValue Read84_SimpleKnownTypeValue(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id84_SimpleKnownTypeValue && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.SimpleKnownTypeValue o;
            o = new global::SerializationTypes.SimpleKnownTypeValue();
            bool[] paramsRead = new bool[1];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id233_StrProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithPropertyNameSpecified Read85_TypeWithPropertyNameSpecified(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id86_TypeWithPropertyNameSpecified && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithPropertyNameSpecified o;
            o = new global::SerializationTypes.TypeWithPropertyNameSpecified();
            bool[] paramsRead = new bool[2];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id234_MyField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@MyFieldSpecified = true;
                            {
                                o.@MyField = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id235_MyFieldIgnored && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithXmlSchemaFormAttribute Read86_TypeWithXmlSchemaFormAttribute(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id87_TypeWithXmlSchemaFormAttribute && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[3];
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
                        if (((object) Reader.LocalName == (object)id236_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id124_int && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (((object) Reader.LocalName == (object)id237_NoneSchemaFormListProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id137_NoneParameter && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (((object) Reader.LocalName == (object)id238_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id139_QualifiedParameter && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithTypeNameInXmlTypeAttribute Read87_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id88_MyXmlType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithTypeNameInXmlTypeAttribute o;
            o = new global::SerializationTypes.TypeWithTypeNameInXmlTypeAttribute();
            bool[] paramsRead = new bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id239_XmlAttributeForm && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithNonPublicDefaultConstructor Read89_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id90_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[1];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id112_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.ServerSettings Read90_ServerSettings(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id91_ServerSettings && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.ServerSettings o;
            o = new global::SerializationTypes.ServerSettings();
            bool[] paramsRead = new bool[2];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id240_DS2Root && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@DS2Root = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id241_MetricConfigUrl && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithXmlQualifiedName Read91_TypeWithXmlQualifiedName(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id92_TypeWithXmlQualifiedName && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithXmlQualifiedName o;
            o = new global::SerializationTypes.TypeWithXmlQualifiedName();
            bool[] paramsRead = new bool[1];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id143_Value && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWith2DArrayProperty2 Read92_TypeWith2DArrayProperty2(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id93_TypeWith2DArrayProperty2 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[1];
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
                        if (((object) Reader.LocalName == (object)id242_TwoDArrayOfSimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id42_SimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                                        if (((object) Reader.LocalName == (object)id42_SimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                                            a_0_0_0 = (global::SerializationTypes.SimpleType[])EnsureArrayIndex(a_0_0_0, ca_0_0_0, typeof(global::SerializationTypes.SimpleType));a_0_0_0[ca_0_0_0++] = Read42_SimpleType(true, true);
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

        global::SerializationTypes.TypeWithPropertiesHavingDefaultValue Read93_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id94_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithPropertiesHavingDefaultValue o;
            o = new global::SerializationTypes.TypeWithPropertiesHavingDefaultValue();
            bool[] paramsRead = new bool[4];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id243_EmptyStringProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@EmptyStringProperty = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id204_StringProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@StringProperty = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id205_IntProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@IntProperty = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id244_CharProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithEnumPropertyHavingDefaultValue Read94_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id95_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithEnumPropertyHavingDefaultValue o;
            o = new global::SerializationTypes.TypeWithEnumPropertyHavingDefaultValue();
            bool[] paramsRead = new bool[1];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id245_EnumProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@EnumProperty = Read68_IntEnum(Reader.ReadElementString());
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

        global::SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue Read95_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id96_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue o;
            o = new global::SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue();
            bool[] paramsRead = new bool[1];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id245_EnumProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@EnumProperty = Read64_EnumFlags(Reader.ReadElementString());
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

        global::SerializationTypes.TypeWithShouldSerializeMethod Read96_TypeWithShouldSerializeMethod(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id97_TypeWithShouldSerializeMethod && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithShouldSerializeMethod o;
            o = new global::SerializationTypes.TypeWithShouldSerializeMethod();
            bool[] paramsRead = new bool[1];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id246_Foo && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.KnownTypesThroughConstructorWithArrayProperties Read97_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id98_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.KnownTypesThroughConstructorWithArrayProperties o;
            o = new global::SerializationTypes.KnownTypesThroughConstructorWithArrayProperties();
            bool[] paramsRead = new bool[2];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id247_StringArrayValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@StringArrayValue = Read1_Object(false, true);
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id248_IntArrayValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.KnownTypesThroughConstructorWithValue Read98_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id99_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.KnownTypesThroughConstructorWithValue o;
            o = new global::SerializationTypes.KnownTypesThroughConstructorWithValue();
            bool[] paramsRead = new bool[1];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id143_Value && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithTypesHavingCustomFormatter Read99_Item(bool isNullable, bool checkType) {
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
            global::SerializationTypes.TypeWithTypesHavingCustomFormatter o;
            o = new global::SerializationTypes.TypeWithTypesHavingCustomFormatter();
            bool[] paramsRead = new bool[9];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id249_DateTimeContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@DateTimeContent = ToDateTime(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id250_QNameContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@QNameContent = ReadElementQualifiedName();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id251_DateContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@DateContent = ToDate(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id252_NameContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@NameContent = ToXmlName(Reader.ReadElementString());
                            }
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id253_NCNameContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@NCNameContent = ToXmlNCName(Reader.ReadElementString());
                            }
                            paramsRead[4] = true;
                            break;
                        }
                        if (!paramsRead[5] && ((object) Reader.LocalName == (object)id254_NMTOKENContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@NMTOKENContent = ToXmlNmToken(Reader.ReadElementString());
                            }
                            paramsRead[5] = true;
                            break;
                        }
                        if (!paramsRead[6] && ((object) Reader.LocalName == (object)id255_NMTOKENSContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@NMTOKENSContent = ToXmlNmTokens(Reader.ReadElementString());
                            }
                            paramsRead[6] = true;
                            break;
                        }
                        if (!paramsRead[7] && ((object) Reader.LocalName == (object)id256_Base64BinaryContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Base64BinaryContent = ToByteArrayBase64(false);
                            }
                            paramsRead[7] = true;
                            break;
                        }
                        if (!paramsRead[8] && ((object) Reader.LocalName == (object)id257_HexBinaryContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithArrayPropertyHavingChoice Read101_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id101_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
            bool[] paramsRead = new bool[1];
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
                        if (((object) Reader.LocalName == (object)id258_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                a_0 = (global::System.Object[])EnsureArrayIndex(a_0, ca_0, typeof(global::System.Object));a_0[ca_0++] = Reader.ReadElementString();
                            }
                            choice_a_0 = (global::SerializationTypes.MoreChoices[])EnsureArrayIndex(choice_a_0, cchoice_a_0, typeof(global::SerializationTypes.MoreChoices));choice_a_0[cchoice_a_0++] = global::SerializationTypes.MoreChoices.@Item;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id259_Amount && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithFieldsOrdered Read102_TypeWithFieldsOrdered(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id103_TypeWithFieldsOrdered && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithFieldsOrdered o;
            o = new global::SerializationTypes.TypeWithFieldsOrdered();
            bool[] paramsRead = new bool[4];
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
                        if (((object) Reader.LocalName == (object)id260_IntField1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@IntField1 = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                        }
                        state = 1;
                        break;
                    case 1:
                        if (((object) Reader.LocalName == (object)id261_IntField2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@IntField2 = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                        }
                        state = 2;
                        break;
                    case 2:
                        if (((object) Reader.LocalName == (object)id262_StringField2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@StringField2 = Reader.ReadElementString();
                            }
                        }
                        state = 3;
                        break;
                    case 3:
                        if (((object) Reader.LocalName == (object)id263_StringField1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithSchemaFormInXmlAttribute Read88_Item(bool isNullable, bool checkType) {
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
            bool[] paramsRead = new bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id264_TestProperty && (object) Reader.NamespaceURI == (object)id265_httptestcom)) {
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

        string id152_LineTotal;
        string id141_P1;
        string id77_UIntEnum;
        string id233_StrProperty;
        string id14_Vehicle;
        string id81_ItemChoiceType;
        string id124_int;
        string id232_SimpleTypeValue;
        string id108_Person;
        string id259_Amount;
        string id239_XmlAttributeForm;
        string id117_Value2;
        string id145_Children;
        string id228_XmlElementPropertyNode;
        string id31_Pet;
        string id178_ShipCost;
        string id10_Animal;
        string id91_ServerSettings;
        string id175_OrderDate;
        string id202_ByteArray;
        string id41_TypeWithDateTimeStringProperty;
        string id2_Item;
        string id153_BinaryHexContent;
        string id68_ClassImplementsInterface;
        string id54_BuiltInTypes;
        string id48_ArrayOfAnyType;
        string id64_SimpleDC;
        string id189_FloatProp;
        string id149_Description;
        string id217_OptionalInt;
        string id247_StringArrayValue;
        string id88_MyXmlType;
        string id106_TypeClashB;
        string id16_BaseClass;
        string id171_City;
        string id5_Item;
        string id197_F2;
        string id172_State;
        string id118_XmlSerializerAttributes;
        string id83_KnownTypesThroughConstructor;
        string id62_Item;
        string id24_DerivedClass1;
        string id104_Item;
        string id121_Item;
        string id180_X;
        string id187_SingleField;
        string id32_DefaultValuesSetToNaN;
        string id33_Item;
        string id241_MetricConfigUrl;
        string id196_F1;
        string id69_WithStruct;
        string id50_MyEnum;
        string id82_TypeWithAnyAttribute;
        string id132_ArrayOfSimpleType;
        string id130_ArrayOfTypeWithLinkedProperty;
        string id156_DTO2;
        string id40_Parameter;
        string id142_P2;
        string id159_NullableDefaultDTO;
        string id157_DefaultDTO;
        string id242_TwoDArrayOfSimpleType;
        string id220_Struct2;
        string id30_Trumpet;
        string id207_ListProperty;
        string id37_Document;
        string id169_value;
        string id4_TypeWithBinaryProperty;
        string id56_TypeB;
        string id55_TypeA;
        string id136_ArrayOfString1;
        string id244_CharProperty;
        string id9_TypeWithXmlNodeArrayProperty;
        string id150_UnitPrice;
        string id72_WithNullables;
        string id173_Zip;
        string id260_IntField1;
        string id139_QualifiedParameter;
        string id92_TypeWithXmlQualifiedName;
        string id177_SubTotal;
        string id114_A;
        string id116_Value1;
        string id186_DoubleField;
        string id255_NMTOKENSContent;
        string id112_Name;
        string id161_TimeSpanProperty2;
        string id12_DogBreed;
        string id194_DateTimeString;
        string id225_XmlIncludeProperty;
        string id78_LongEnum;
        string id127_ArrayOfDouble;
        string id134_ArrayOfItemChoiceType;
        string id115_B;
        string id192_refs;
        string id131_ArrayOfParameter;
        string id87_TypeWithXmlSchemaFormAttribute;
        string id231_EnumValue;
        string id162_ByteProperty;
        string id84_SimpleKnownTypeValue;
        string id22_AliasedTestType;
        string id265_httptestcom;
        string id179_TotalCost;
        string id96_Item;
        string id205_IntProperty;
        string id65_Item;
        string id45_StructNotSerializable;
        string id34_Item;
        string id39_RootClass;
        string id47_Item;
        string id120_MsgDocumentType;
        string id140_ArrayOfArrayOfSimpleType;
        string id256_Base64BinaryContent;
        string id86_TypeWithPropertyNameSpecified;
        string id222_Word;
        string id176_Items;
        string id183_Prop;
        string id46_TypeWithMyCollectionField;
        string id90_Item;
        string id105_Root;
        string id138_ArrayOfBoolean;
        string id70_SomeStruct;
        string id240_DS2Root;
        string id94_Item;
        string id264_TestProperty;
        string id42_SimpleType;
        string id199_Data;
        string id236_Item;
        string id257_HexBinaryContent;
        string id165_LicenseNumber;
        string id248_IntArrayValue;
        string id20_Address;
        string id246_Foo;
        string id201_MyEnum1;
        string id110_MiddleName;
        string id234_MyField;
        string id238_Item;
        string id102_MoreChoices;
        string id89_Item;
        string id230_CustomXmlArrayProperty;
        string id43_TypeWithGetSetArrayMembers;
        string id15_Employee;
        string id107_TypeClashA;
        string id249_DateTimeContent;
        string id227_Item;
        string id174_ShipTo;
        string id200_MyStruct;
        string id215_Optional;
        string id190_IntValue;
        string id193_Parameters;
        string id184_Instruments;
        string id258_Item;
        string id27_Orchestra;
        string id210_Id;
        string id137_NoneParameter;
        string id25_ArrayOfDateTime;
        string id18_PurchaseOrder;
        string id181_Y;
        string id29_Brass;
        string id52_DCStruct;
        string id206_DateTimeProperty;
        string id73_ByteEnum;
        string id251_DateContent;
        string id229_httpelement;
        string id226_XmlEnumProperty;
        string id263_StringField1;
        string id198_Collection;
        string id63_TypeWithByteArrayAsXmlText;
        string id60_DerivedClassWithSameProperty;
        string id128_double;
        string id49_anyType;
        string id100_Item;
        string id119_ParameterOfString;
        string id66_Item;
        string id97_TypeWithShouldSerializeMethod;
        string id17_DerivedClass;
        string id101_Item;
        string id19_httpwwwcontoso1com;
        string id93_TypeWith2DArrayProperty2;
        string id213_Int;
        string id166_GroupName;
        string id109_FirstName;
        string id235_MyFieldIgnored;
        string id1_TypeWithXmlElementProperty;
        string id125_ArrayOfString;
        string id203_Item;
        string id59_BaseClassWithSamePropertyName;
        string id204_StringProperty;
        string id76_IntEnum;
        string id188_DoubleProp;
        string id147_Modulation;
        string id53_DCClassWithEnumAndStruct;
        string id144_Child;
        string id26_dateTime;
        string id151_Quantity;
        string id209_DisplayName;
        string id103_TypeWithFieldsOrdered;
        string id98_Item;
        string id261_IntField2;
        string id79_ULongEnum;
        string id44_TypeWithGetOnlyArrayProperties;
        string id191_id;
        string id218_OptionullInt;
        string id71_WithEnums;
        string id3_TypeWithXmlDocumentProperty;
        string id51_TypeWithEnumMembers;
        string id170_Line1;
        string id122_ArrayOfOrderedItem;
        string id154_Base64Content;
        string id195_CurrentDateTime;
        string id212_Some;
        string id113_ContainerType;
        string id85_Item;
        string id163_Age;
        string id216_Optionull;
        string id35_RootElement;
        string id224_DecimalNumber;
        string id38_httpexamplecom;
        string id61_DerivedClassWithSameProperty2;
        string id143_Value;
        string id133_ArrayOfTypeA;
        string id185_Comment2;
        string id11_Dog;
        string id7_Item;
        string id252_NameContent;
        string id8_TypeWithByteProperty;
        string id221_XmlAttributeName;
        string id211_IsLoaded;
        string id111_LastName;
        string id208_ClassID;
        string id80_AttributeTesting;
        string id155_DTO;
        string id182_Z;
        string id262_StringField2;
        string id28_Instrument;
        string id126_string;
        string id58_Item;
        string id75_ShortEnum;
        string id237_NoneSchemaFormListProperty;
        string id243_EmptyStringProperty;
        string id254_NMTOKENContent;
        string id6_TypeWithTimeSpanProperty;
        string id21_OrderedItem;
        string id57_TypeHasArrayOfASerializedAsB;
        string id129_ArrayOfInstrument;
        string id36_TypeWithLinkedProperty;
        string id219_Struct1;
        string id164_Breed;
        string id148_ItemName;
        string id146_IsValved;
        string id250_QNameContent;
        string id158_NullableDTO;
        string id13_Group;
        string id253_NCNameContent;
        string id168_EmployeeName;
        string id95_Item;
        string id223_Number;
        string id160_TimeSpanProperty;
        string id67_EnumFlags;
        string id23_BaseClass1;
        string id99_Item;
        string id167_GroupVehicle;
        string id214_Short;
        string id245_EnumProperty;
        string id74_SByteEnum;
        string id123_ArrayOfInt;
        string id135_httpmynamespace;

        protected override void InitIDs() {
            id152_LineTotal = Reader.NameTable.Add(@"LineTotal");
            id141_P1 = Reader.NameTable.Add(@"P1");
            id77_UIntEnum = Reader.NameTable.Add(@"UIntEnum");
            id233_StrProperty = Reader.NameTable.Add(@"StrProperty");
            id14_Vehicle = Reader.NameTable.Add(@"Vehicle");
            id81_ItemChoiceType = Reader.NameTable.Add(@"ItemChoiceType");
            id124_int = Reader.NameTable.Add(@"int");
            id232_SimpleTypeValue = Reader.NameTable.Add(@"SimpleTypeValue");
            id108_Person = Reader.NameTable.Add(@"Person");
            id259_Amount = Reader.NameTable.Add(@"Amount");
            id239_XmlAttributeForm = Reader.NameTable.Add(@"XmlAttributeForm");
            id117_Value2 = Reader.NameTable.Add(@"Value2");
            id145_Children = Reader.NameTable.Add(@"Children");
            id228_XmlElementPropertyNode = Reader.NameTable.Add(@"XmlElementPropertyNode");
            id31_Pet = Reader.NameTable.Add(@"Pet");
            id178_ShipCost = Reader.NameTable.Add(@"ShipCost");
            id10_Animal = Reader.NameTable.Add(@"Animal");
            id91_ServerSettings = Reader.NameTable.Add(@"ServerSettings");
            id175_OrderDate = Reader.NameTable.Add(@"OrderDate");
            id202_ByteArray = Reader.NameTable.Add(@"ByteArray");
            id41_TypeWithDateTimeStringProperty = Reader.NameTable.Add(@"TypeWithDateTimeStringProperty");
            id2_Item = Reader.NameTable.Add(@"");
            id153_BinaryHexContent = Reader.NameTable.Add(@"BinaryHexContent");
            id68_ClassImplementsInterface = Reader.NameTable.Add(@"ClassImplementsInterface");
            id54_BuiltInTypes = Reader.NameTable.Add(@"BuiltInTypes");
            id48_ArrayOfAnyType = Reader.NameTable.Add(@"ArrayOfAnyType");
            id64_SimpleDC = Reader.NameTable.Add(@"SimpleDC");
            id189_FloatProp = Reader.NameTable.Add(@"FloatProp");
            id149_Description = Reader.NameTable.Add(@"Description");
            id217_OptionalInt = Reader.NameTable.Add(@"OptionalInt");
            id247_StringArrayValue = Reader.NameTable.Add(@"StringArrayValue");
            id88_MyXmlType = Reader.NameTable.Add(@"MyXmlType");
            id106_TypeClashB = Reader.NameTable.Add(@"TypeClashB");
            id16_BaseClass = Reader.NameTable.Add(@"BaseClass");
            id171_City = Reader.NameTable.Add(@"City");
            id5_Item = Reader.NameTable.Add(@"TypeWithDateTimeOffsetProperties");
            id197_F2 = Reader.NameTable.Add(@"F2");
            id172_State = Reader.NameTable.Add(@"State");
            id118_XmlSerializerAttributes = Reader.NameTable.Add(@"XmlSerializerAttributes");
            id83_KnownTypesThroughConstructor = Reader.NameTable.Add(@"KnownTypesThroughConstructor");
            id62_Item = Reader.NameTable.Add(@"TypeWithDateTimePropertyAsXmlTime");
            id24_DerivedClass1 = Reader.NameTable.Add(@"DerivedClass1");
            id104_Item = Reader.NameTable.Add(@"TypeWithKnownTypesOfCollectionsWithConflictingXmlName");
            id121_Item = Reader.NameTable.Add(@"TypeWithMismatchBetweenAttributeAndPropertyType");
            id180_X = Reader.NameTable.Add(@"X");
            id187_SingleField = Reader.NameTable.Add(@"SingleField");
            id32_DefaultValuesSetToNaN = Reader.NameTable.Add(@"DefaultValuesSetToNaN");
            id33_Item = Reader.NameTable.Add(@"DefaultValuesSetToPositiveInfinity");
            id241_MetricConfigUrl = Reader.NameTable.Add(@"MetricConfigUrl");
            id196_F1 = Reader.NameTable.Add(@"F1");
            id69_WithStruct = Reader.NameTable.Add(@"WithStruct");
            id50_MyEnum = Reader.NameTable.Add(@"MyEnum");
            id82_TypeWithAnyAttribute = Reader.NameTable.Add(@"TypeWithAnyAttribute");
            id132_ArrayOfSimpleType = Reader.NameTable.Add(@"ArrayOfSimpleType");
            id130_ArrayOfTypeWithLinkedProperty = Reader.NameTable.Add(@"ArrayOfTypeWithLinkedProperty");
            id156_DTO2 = Reader.NameTable.Add(@"DTO2");
            id40_Parameter = Reader.NameTable.Add(@"Parameter");
            id142_P2 = Reader.NameTable.Add(@"P2");
            id159_NullableDefaultDTO = Reader.NameTable.Add(@"NullableDefaultDTO");
            id157_DefaultDTO = Reader.NameTable.Add(@"DefaultDTO");
            id242_TwoDArrayOfSimpleType = Reader.NameTable.Add(@"TwoDArrayOfSimpleType");
            id220_Struct2 = Reader.NameTable.Add(@"Struct2");
            id30_Trumpet = Reader.NameTable.Add(@"Trumpet");
            id207_ListProperty = Reader.NameTable.Add(@"ListProperty");
            id37_Document = Reader.NameTable.Add(@"Document");
            id169_value = Reader.NameTable.Add(@"value");
            id4_TypeWithBinaryProperty = Reader.NameTable.Add(@"TypeWithBinaryProperty");
            id56_TypeB = Reader.NameTable.Add(@"TypeB");
            id55_TypeA = Reader.NameTable.Add(@"TypeA");
            id136_ArrayOfString1 = Reader.NameTable.Add(@"ArrayOfString1");
            id244_CharProperty = Reader.NameTable.Add(@"CharProperty");
            id9_TypeWithXmlNodeArrayProperty = Reader.NameTable.Add(@"TypeWithXmlNodeArrayProperty");
            id150_UnitPrice = Reader.NameTable.Add(@"UnitPrice");
            id72_WithNullables = Reader.NameTable.Add(@"WithNullables");
            id173_Zip = Reader.NameTable.Add(@"Zip");
            id260_IntField1 = Reader.NameTable.Add(@"IntField1");
            id139_QualifiedParameter = Reader.NameTable.Add(@"QualifiedParameter");
            id92_TypeWithXmlQualifiedName = Reader.NameTable.Add(@"TypeWithXmlQualifiedName");
            id177_SubTotal = Reader.NameTable.Add(@"SubTotal");
            id114_A = Reader.NameTable.Add(@"A");
            id116_Value1 = Reader.NameTable.Add(@"Value1");
            id186_DoubleField = Reader.NameTable.Add(@"DoubleField");
            id255_NMTOKENSContent = Reader.NameTable.Add(@"NMTOKENSContent");
            id112_Name = Reader.NameTable.Add(@"Name");
            id161_TimeSpanProperty2 = Reader.NameTable.Add(@"TimeSpanProperty2");
            id12_DogBreed = Reader.NameTable.Add(@"DogBreed");
            id194_DateTimeString = Reader.NameTable.Add(@"DateTimeString");
            id225_XmlIncludeProperty = Reader.NameTable.Add(@"XmlIncludeProperty");
            id78_LongEnum = Reader.NameTable.Add(@"LongEnum");
            id127_ArrayOfDouble = Reader.NameTable.Add(@"ArrayOfDouble");
            id134_ArrayOfItemChoiceType = Reader.NameTable.Add(@"ArrayOfItemChoiceType");
            id115_B = Reader.NameTable.Add(@"B");
            id192_refs = Reader.NameTable.Add(@"refs");
            id131_ArrayOfParameter = Reader.NameTable.Add(@"ArrayOfParameter");
            id87_TypeWithXmlSchemaFormAttribute = Reader.NameTable.Add(@"TypeWithXmlSchemaFormAttribute");
            id231_EnumValue = Reader.NameTable.Add(@"EnumValue");
            id162_ByteProperty = Reader.NameTable.Add(@"ByteProperty");
            id84_SimpleKnownTypeValue = Reader.NameTable.Add(@"SimpleKnownTypeValue");
            id22_AliasedTestType = Reader.NameTable.Add(@"AliasedTestType");
            id265_httptestcom = Reader.NameTable.Add(@"http://test.com");
            id179_TotalCost = Reader.NameTable.Add(@"TotalCost");
            id96_Item = Reader.NameTable.Add(@"TypeWithEnumFlagPropertyHavingDefaultValue");
            id205_IntProperty = Reader.NameTable.Add(@"IntProperty");
            id65_Item = Reader.NameTable.Add(@"TypeWithXmlTextAttributeOnArray");
            id45_StructNotSerializable = Reader.NameTable.Add(@"StructNotSerializable");
            id34_Item = Reader.NameTable.Add(@"DefaultValuesSetToNegativeInfinity");
            id39_RootClass = Reader.NameTable.Add(@"RootClass");
            id47_Item = Reader.NameTable.Add(@"TypeWithReadOnlyMyCollectionProperty");
            id120_MsgDocumentType = Reader.NameTable.Add(@"MsgDocumentType");
            id140_ArrayOfArrayOfSimpleType = Reader.NameTable.Add(@"ArrayOfArrayOfSimpleType");
            id256_Base64BinaryContent = Reader.NameTable.Add(@"Base64BinaryContent");
            id86_TypeWithPropertyNameSpecified = Reader.NameTable.Add(@"TypeWithPropertyNameSpecified");
            id222_Word = Reader.NameTable.Add(@"Word");
            id176_Items = Reader.NameTable.Add(@"Items");
            id183_Prop = Reader.NameTable.Add(@"Prop");
            id46_TypeWithMyCollectionField = Reader.NameTable.Add(@"TypeWithMyCollectionField");
            id90_Item = Reader.NameTable.Add(@"TypeWithNonPublicDefaultConstructor");
            id105_Root = Reader.NameTable.Add(@"Root");
            id138_ArrayOfBoolean = Reader.NameTable.Add(@"ArrayOfBoolean");
            id70_SomeStruct = Reader.NameTable.Add(@"SomeStruct");
            id240_DS2Root = Reader.NameTable.Add(@"DS2Root");
            id94_Item = Reader.NameTable.Add(@"TypeWithPropertiesHavingDefaultValue");
            id264_TestProperty = Reader.NameTable.Add(@"TestProperty");
            id42_SimpleType = Reader.NameTable.Add(@"SimpleType");
            id199_Data = Reader.NameTable.Add(@"Data");
            id236_Item = Reader.NameTable.Add(@"UnqualifiedSchemaFormListProperty");
            id257_HexBinaryContent = Reader.NameTable.Add(@"HexBinaryContent");
            id165_LicenseNumber = Reader.NameTable.Add(@"LicenseNumber");
            id248_IntArrayValue = Reader.NameTable.Add(@"IntArrayValue");
            id20_Address = Reader.NameTable.Add(@"Address");
            id246_Foo = Reader.NameTable.Add(@"Foo");
            id201_MyEnum1 = Reader.NameTable.Add(@"MyEnum1");
            id110_MiddleName = Reader.NameTable.Add(@"MiddleName");
            id234_MyField = Reader.NameTable.Add(@"MyField");
            id238_Item = Reader.NameTable.Add(@"QualifiedSchemaFormListProperty");
            id102_MoreChoices = Reader.NameTable.Add(@"MoreChoices");
            id89_Item = Reader.NameTable.Add(@"TypeWithSchemaFormInXmlAttribute");
            id230_CustomXmlArrayProperty = Reader.NameTable.Add(@"CustomXmlArrayProperty");
            id43_TypeWithGetSetArrayMembers = Reader.NameTable.Add(@"TypeWithGetSetArrayMembers");
            id15_Employee = Reader.NameTable.Add(@"Employee");
            id107_TypeClashA = Reader.NameTable.Add(@"TypeClashA");
            id249_DateTimeContent = Reader.NameTable.Add(@"DateTimeContent");
            id227_Item = Reader.NameTable.Add(@"XmlNamespaceDeclarationsProperty");
            id174_ShipTo = Reader.NameTable.Add(@"ShipTo");
            id200_MyStruct = Reader.NameTable.Add(@"MyStruct");
            id215_Optional = Reader.NameTable.Add(@"Optional");
            id190_IntValue = Reader.NameTable.Add(@"IntValue");
            id193_Parameters = Reader.NameTable.Add(@"Parameters");
            id184_Instruments = Reader.NameTable.Add(@"Instruments");
            id258_Item = Reader.NameTable.Add(@"Item");
            id27_Orchestra = Reader.NameTable.Add(@"Orchestra");
            id210_Id = Reader.NameTable.Add(@"Id");
            id137_NoneParameter = Reader.NameTable.Add(@"NoneParameter");
            id25_ArrayOfDateTime = Reader.NameTable.Add(@"ArrayOfDateTime");
            id18_PurchaseOrder = Reader.NameTable.Add(@"PurchaseOrder");
            id181_Y = Reader.NameTable.Add(@"Y");
            id29_Brass = Reader.NameTable.Add(@"Brass");
            id52_DCStruct = Reader.NameTable.Add(@"DCStruct");
            id206_DateTimeProperty = Reader.NameTable.Add(@"DateTimeProperty");
            id73_ByteEnum = Reader.NameTable.Add(@"ByteEnum");
            id251_DateContent = Reader.NameTable.Add(@"DateContent");
            id229_httpelement = Reader.NameTable.Add(@"http://element");
            id226_XmlEnumProperty = Reader.NameTable.Add(@"XmlEnumProperty");
            id263_StringField1 = Reader.NameTable.Add(@"StringField1");
            id198_Collection = Reader.NameTable.Add(@"Collection");
            id63_TypeWithByteArrayAsXmlText = Reader.NameTable.Add(@"TypeWithByteArrayAsXmlText");
            id60_DerivedClassWithSameProperty = Reader.NameTable.Add(@"DerivedClassWithSameProperty");
            id128_double = Reader.NameTable.Add(@"double");
            id49_anyType = Reader.NameTable.Add(@"anyType");
            id100_Item = Reader.NameTable.Add(@"TypeWithTypesHavingCustomFormatter");
            id119_ParameterOfString = Reader.NameTable.Add(@"ParameterOfString");
            id66_Item = Reader.NameTable.Add(@"http://schemas.xmlsoap.org/ws/2005/04/discovery");
            id97_TypeWithShouldSerializeMethod = Reader.NameTable.Add(@"TypeWithShouldSerializeMethod");
            id17_DerivedClass = Reader.NameTable.Add(@"DerivedClass");
            id101_Item = Reader.NameTable.Add(@"TypeWithArrayPropertyHavingChoice");
            id19_httpwwwcontoso1com = Reader.NameTable.Add(@"http://www.contoso1.com");
            id93_TypeWith2DArrayProperty2 = Reader.NameTable.Add(@"TypeWith2DArrayProperty2");
            id213_Int = Reader.NameTable.Add(@"Int");
            id166_GroupName = Reader.NameTable.Add(@"GroupName");
            id109_FirstName = Reader.NameTable.Add(@"FirstName");
            id235_MyFieldIgnored = Reader.NameTable.Add(@"MyFieldIgnored");
            id1_TypeWithXmlElementProperty = Reader.NameTable.Add(@"TypeWithXmlElementProperty");
            id125_ArrayOfString = Reader.NameTable.Add(@"ArrayOfString");
            id203_Item = Reader.NameTable.Add(@"PropertyNameWithSpecialCharacters漢ñ");
            id59_BaseClassWithSamePropertyName = Reader.NameTable.Add(@"BaseClassWithSamePropertyName");
            id204_StringProperty = Reader.NameTable.Add(@"StringProperty");
            id76_IntEnum = Reader.NameTable.Add(@"IntEnum");
            id188_DoubleProp = Reader.NameTable.Add(@"DoubleProp");
            id147_Modulation = Reader.NameTable.Add(@"Modulation");
            id53_DCClassWithEnumAndStruct = Reader.NameTable.Add(@"DCClassWithEnumAndStruct");
            id144_Child = Reader.NameTable.Add(@"Child");
            id26_dateTime = Reader.NameTable.Add(@"dateTime");
            id151_Quantity = Reader.NameTable.Add(@"Quantity");
            id209_DisplayName = Reader.NameTable.Add(@"DisplayName");
            id103_TypeWithFieldsOrdered = Reader.NameTable.Add(@"TypeWithFieldsOrdered");
            id98_Item = Reader.NameTable.Add(@"KnownTypesThroughConstructorWithArrayProperties");
            id261_IntField2 = Reader.NameTable.Add(@"IntField2");
            id79_ULongEnum = Reader.NameTable.Add(@"ULongEnum");
            id44_TypeWithGetOnlyArrayProperties = Reader.NameTable.Add(@"TypeWithGetOnlyArrayProperties");
            id191_id = Reader.NameTable.Add(@"id");
            id218_OptionullInt = Reader.NameTable.Add(@"OptionullInt");
            id71_WithEnums = Reader.NameTable.Add(@"WithEnums");
            id3_TypeWithXmlDocumentProperty = Reader.NameTable.Add(@"TypeWithXmlDocumentProperty");
            id51_TypeWithEnumMembers = Reader.NameTable.Add(@"TypeWithEnumMembers");
            id170_Line1 = Reader.NameTable.Add(@"Line1");
            id122_ArrayOfOrderedItem = Reader.NameTable.Add(@"ArrayOfOrderedItem");
            id154_Base64Content = Reader.NameTable.Add(@"Base64Content");
            id195_CurrentDateTime = Reader.NameTable.Add(@"CurrentDateTime");
            id212_Some = Reader.NameTable.Add(@"Some");
            id113_ContainerType = Reader.NameTable.Add(@"ContainerType");
            id85_Item = Reader.NameTable.Add(@"ClassImplementingIXmlSerialiable");
            id163_Age = Reader.NameTable.Add(@"Age");
            id216_Optionull = Reader.NameTable.Add(@"Optionull");
            id35_RootElement = Reader.NameTable.Add(@"RootElement");
            id224_DecimalNumber = Reader.NameTable.Add(@"DecimalNumber");
            id38_httpexamplecom = Reader.NameTable.Add(@"http://example.com");
            id61_DerivedClassWithSameProperty2 = Reader.NameTable.Add(@"DerivedClassWithSameProperty2");
            id143_Value = Reader.NameTable.Add(@"Value");
            id133_ArrayOfTypeA = Reader.NameTable.Add(@"ArrayOfTypeA");
            id185_Comment2 = Reader.NameTable.Add(@"Comment2");
            id11_Dog = Reader.NameTable.Add(@"Dog");
            id7_Item = Reader.NameTable.Add(@"TypeWithDefaultTimeSpanProperty");
            id252_NameContent = Reader.NameTable.Add(@"NameContent");
            id8_TypeWithByteProperty = Reader.NameTable.Add(@"TypeWithByteProperty");
            id221_XmlAttributeName = Reader.NameTable.Add(@"XmlAttributeName");
            id211_IsLoaded = Reader.NameTable.Add(@"IsLoaded");
            id111_LastName = Reader.NameTable.Add(@"LastName");
            id208_ClassID = Reader.NameTable.Add(@"ClassID");
            id80_AttributeTesting = Reader.NameTable.Add(@"AttributeTesting");
            id155_DTO = Reader.NameTable.Add(@"DTO");
            id182_Z = Reader.NameTable.Add(@"Z");
            id262_StringField2 = Reader.NameTable.Add(@"StringField2");
            id28_Instrument = Reader.NameTable.Add(@"Instrument");
            id126_string = Reader.NameTable.Add(@"string");
            id58_Item = Reader.NameTable.Add(@"__TypeNameWithSpecialCharacters漢ñ");
            id75_ShortEnum = Reader.NameTable.Add(@"ShortEnum");
            id237_NoneSchemaFormListProperty = Reader.NameTable.Add(@"NoneSchemaFormListProperty");
            id243_EmptyStringProperty = Reader.NameTable.Add(@"EmptyStringProperty");
            id254_NMTOKENContent = Reader.NameTable.Add(@"NMTOKENContent");
            id6_TypeWithTimeSpanProperty = Reader.NameTable.Add(@"TypeWithTimeSpanProperty");
            id21_OrderedItem = Reader.NameTable.Add(@"OrderedItem");
            id57_TypeHasArrayOfASerializedAsB = Reader.NameTable.Add(@"TypeHasArrayOfASerializedAsB");
            id129_ArrayOfInstrument = Reader.NameTable.Add(@"ArrayOfInstrument");
            id36_TypeWithLinkedProperty = Reader.NameTable.Add(@"TypeWithLinkedProperty");
            id219_Struct1 = Reader.NameTable.Add(@"Struct1");
            id164_Breed = Reader.NameTable.Add(@"Breed");
            id148_ItemName = Reader.NameTable.Add(@"ItemName");
            id146_IsValved = Reader.NameTable.Add(@"IsValved");
            id250_QNameContent = Reader.NameTable.Add(@"QNameContent");
            id158_NullableDTO = Reader.NameTable.Add(@"NullableDTO");
            id13_Group = Reader.NameTable.Add(@"Group");
            id253_NCNameContent = Reader.NameTable.Add(@"NCNameContent");
            id168_EmployeeName = Reader.NameTable.Add(@"EmployeeName");
            id95_Item = Reader.NameTable.Add(@"TypeWithEnumPropertyHavingDefaultValue");
            id223_Number = Reader.NameTable.Add(@"Number");
            id160_TimeSpanProperty = Reader.NameTable.Add(@"TimeSpanProperty");
            id67_EnumFlags = Reader.NameTable.Add(@"EnumFlags");
            id23_BaseClass1 = Reader.NameTable.Add(@"BaseClass1");
            id99_Item = Reader.NameTable.Add(@"KnownTypesThroughConstructorWithValue");
            id167_GroupVehicle = Reader.NameTable.Add(@"GroupVehicle");
            id214_Short = Reader.NameTable.Add(@"Short");
            id245_EnumProperty = Reader.NameTable.Add(@"EnumProperty");
            id74_SByteEnum = Reader.NameTable.Add(@"SByteEnum");
            id123_ArrayOfInt = Reader.NameTable.Add(@"ArrayOfInt");
            id135_httpmynamespace = Reader.NameTable.Add(@"http://mynamespace");
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
            ((XmlSerializationWriter1)writer).Write104_TypeWithXmlElementProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read108_TypeWithXmlElementProperty();
        }
    }

    public sealed class TypeWithXmlDocumentPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithXmlDocumentProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write105_TypeWithXmlDocumentProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read109_TypeWithXmlDocumentProperty();
        }
    }

    public sealed class TypeWithBinaryPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithBinaryProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write106_TypeWithBinaryProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read110_TypeWithBinaryProperty();
        }
    }

    public sealed class TypeWithDateTimeOffsetPropertiesSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithDateTimeOffsetProperties", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write107_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read111_Item();
        }
    }

    public sealed class TypeWithTimeSpanPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithTimeSpanProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write108_TypeWithTimeSpanProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read112_TypeWithTimeSpanProperty();
        }
    }

    public sealed class TypeWithDefaultTimeSpanPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithDefaultTimeSpanProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write109_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read113_Item();
        }
    }

    public sealed class TypeWithBytePropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithByteProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write110_TypeWithByteProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read114_TypeWithByteProperty();
        }
    }

    public sealed class TypeWithXmlNodeArrayPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithXmlNodeArrayProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write111_TypeWithXmlNodeArrayProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read115_TypeWithXmlNodeArrayProperty();
        }
    }

    public sealed class AnimalSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Animal", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write112_Animal(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read116_Animal();
        }
    }

    public sealed class DogSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Dog", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write113_Dog(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read117_Dog();
        }
    }

    public sealed class DogBreedSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DogBreed", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write114_DogBreed(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read118_DogBreed();
        }
    }

    public sealed class GroupSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Group", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write115_Group(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read119_Group();
        }
    }

    public sealed class VehicleSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Vehicle", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write116_Vehicle(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read120_Vehicle();
        }
    }

    public sealed class EmployeeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Employee", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write117_Employee(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read121_Employee();
        }
    }

    public sealed class BaseClassSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"BaseClass", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write118_BaseClass(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read122_BaseClass();
        }
    }

    public sealed class DerivedClassSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DerivedClass", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write119_DerivedClass(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read123_DerivedClass();
        }
    }

    public sealed class PurchaseOrderSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"PurchaseOrder", @"http://www.contoso1.com");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write120_PurchaseOrder(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read124_PurchaseOrder();
        }
    }

    public sealed class AddressSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Address", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write121_Address(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read125_Address();
        }
    }

    public sealed class OrderedItemSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"OrderedItem", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write122_OrderedItem(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read126_OrderedItem();
        }
    }

    public sealed class AliasedTestTypeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"AliasedTestType", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write123_AliasedTestType(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read127_AliasedTestType();
        }
    }

    public sealed class BaseClass1Serializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"BaseClass1", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write124_BaseClass1(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read128_BaseClass1();
        }
    }

    public sealed class DerivedClass1Serializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DerivedClass1", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write125_DerivedClass1(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read129_DerivedClass1();
        }
    }

    public sealed class MyCollection1Serializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ArrayOfDateTime", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write126_ArrayOfDateTime(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read130_ArrayOfDateTime();
        }
    }

    public sealed class OrchestraSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Orchestra", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write127_Orchestra(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read131_Orchestra();
        }
    }

    public sealed class InstrumentSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Instrument", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write128_Instrument(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read132_Instrument();
        }
    }

    public sealed class BrassSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Brass", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write129_Brass(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read133_Brass();
        }
    }

    public sealed class TrumpetSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Trumpet", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write130_Trumpet(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read134_Trumpet();
        }
    }

    public sealed class PetSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Pet", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write131_Pet(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read135_Pet();
        }
    }

    public sealed class DefaultValuesSetToNaNSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DefaultValuesSetToNaN", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write132_DefaultValuesSetToNaN(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read136_DefaultValuesSetToNaN();
        }
    }

    public sealed class DefaultValuesSetToPositiveInfinitySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DefaultValuesSetToPositiveInfinity", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write133_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read137_Item();
        }
    }

    public sealed class DefaultValuesSetToNegativeInfinitySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DefaultValuesSetToNegativeInfinity", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write134_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read138_Item();
        }
    }

    public sealed class TypeWithMismatchBetweenAttributeAndPropertyTypeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"RootElement", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write135_RootElement(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read139_RootElement();
        }
    }

    public sealed class TypeWithLinkedPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithLinkedProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write136_TypeWithLinkedProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read140_TypeWithLinkedProperty();
        }
    }

    public sealed class MsgDocumentTypeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Document", @"http://example.com");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write137_Document(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read141_Document();
        }
    }

    public sealed class RootClassSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"RootClass", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write138_RootClass(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read142_RootClass();
        }
    }

    public sealed class ParameterSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Parameter", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write139_Parameter(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read143_Parameter();
        }
    }

    public sealed class TypeWithDateTimeStringPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithDateTimeStringProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write140_TypeWithDateTimeStringProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read144_TypeWithDateTimeStringProperty();
        }
    }

    public sealed class SimpleTypeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"SimpleType", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write141_SimpleType(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read145_SimpleType();
        }
    }

    public sealed class TypeWithGetSetArrayMembersSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithGetSetArrayMembers", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write142_TypeWithGetSetArrayMembers(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read146_TypeWithGetSetArrayMembers();
        }
    }

    public sealed class TypeWithGetOnlyArrayPropertiesSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithGetOnlyArrayProperties", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write143_TypeWithGetOnlyArrayProperties(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read147_TypeWithGetOnlyArrayProperties();
        }
    }

    public sealed class StructNotSerializableSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"StructNotSerializable", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write144_StructNotSerializable(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read148_StructNotSerializable();
        }
    }

    public sealed class TypeWithMyCollectionFieldSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithMyCollectionField", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write145_TypeWithMyCollectionField(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read149_TypeWithMyCollectionField();
        }
    }

    public sealed class TypeWithReadOnlyMyCollectionPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithReadOnlyMyCollectionProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write146_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read150_Item();
        }
    }

    public sealed class MyListSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ArrayOfAnyType", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write147_ArrayOfAnyType(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read151_ArrayOfAnyType();
        }
    }

    public sealed class MyEnumSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"MyEnum", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write148_MyEnum(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read152_MyEnum();
        }
    }

    public sealed class TypeWithEnumMembersSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithEnumMembers", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write149_TypeWithEnumMembers(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read153_TypeWithEnumMembers();
        }
    }

    public sealed class DCStructSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DCStruct", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write150_DCStruct(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read154_DCStruct();
        }
    }

    public sealed class DCClassWithEnumAndStructSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DCClassWithEnumAndStruct", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write151_DCClassWithEnumAndStruct(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read155_DCClassWithEnumAndStruct();
        }
    }

    public sealed class BuiltInTypesSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"BuiltInTypes", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write152_BuiltInTypes(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read156_BuiltInTypes();
        }
    }

    public sealed class TypeASerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeA", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write153_TypeA(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read157_TypeA();
        }
    }

    public sealed class TypeBSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeB", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write154_TypeB(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read158_TypeB();
        }
    }

    public sealed class TypeHasArrayOfASerializedAsBSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeHasArrayOfASerializedAsB", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write155_TypeHasArrayOfASerializedAsB(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read159_TypeHasArrayOfASerializedAsB();
        }
    }

    public sealed class @__TypeNameWithSpecialCharacters漢ñSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"__TypeNameWithSpecialCharacters漢ñ", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write156_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read160_Item();
        }
    }

    public sealed class BaseClassWithSamePropertyNameSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"BaseClassWithSamePropertyName", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write157_BaseClassWithSamePropertyName(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read161_BaseClassWithSamePropertyName();
        }
    }

    public sealed class DerivedClassWithSamePropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DerivedClassWithSameProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write158_DerivedClassWithSameProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read162_DerivedClassWithSameProperty();
        }
    }

    public sealed class DerivedClassWithSameProperty2Serializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DerivedClassWithSameProperty2", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write159_DerivedClassWithSameProperty2(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read163_DerivedClassWithSameProperty2();
        }
    }

    public sealed class TypeWithDateTimePropertyAsXmlTimeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithDateTimePropertyAsXmlTime", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write160_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read164_Item();
        }
    }

    public sealed class TypeWithByteArrayAsXmlTextSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithByteArrayAsXmlText", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write161_TypeWithByteArrayAsXmlText(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read165_TypeWithByteArrayAsXmlText();
        }
    }

    public sealed class SimpleDCSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"SimpleDC", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write162_SimpleDC(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read166_SimpleDC();
        }
    }

    public sealed class TypeWithXmlTextAttributeOnArraySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithXmlTextAttributeOnArray", @"http://schemas.xmlsoap.org/ws/2005/04/discovery");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write163_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read167_Item();
        }
    }

    public sealed class EnumFlagsSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"EnumFlags", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write164_EnumFlags(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read168_EnumFlags();
        }
    }

    public sealed class ClassImplementsInterfaceSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ClassImplementsInterface", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write165_ClassImplementsInterface(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read169_ClassImplementsInterface();
        }
    }

    public sealed class WithStructSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"WithStruct", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write166_WithStruct(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read170_WithStruct();
        }
    }

    public sealed class SomeStructSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"SomeStruct", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write167_SomeStruct(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read171_SomeStruct();
        }
    }

    public sealed class WithEnumsSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"WithEnums", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write168_WithEnums(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read172_WithEnums();
        }
    }

    public sealed class WithNullablesSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"WithNullables", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write169_WithNullables(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read173_WithNullables();
        }
    }

    public sealed class ByteEnumSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ByteEnum", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write170_ByteEnum(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read174_ByteEnum();
        }
    }

    public sealed class SByteEnumSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"SByteEnum", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write171_SByteEnum(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read175_SByteEnum();
        }
    }

    public sealed class ShortEnumSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ShortEnum", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write172_ShortEnum(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read176_ShortEnum();
        }
    }

    public sealed class IntEnumSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"IntEnum", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write173_IntEnum(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read177_IntEnum();
        }
    }

    public sealed class UIntEnumSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"UIntEnum", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write174_UIntEnum(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read178_UIntEnum();
        }
    }

    public sealed class LongEnumSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"LongEnum", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write175_LongEnum(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read179_LongEnum();
        }
    }

    public sealed class ULongEnumSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ULongEnum", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write176_ULongEnum(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read180_ULongEnum();
        }
    }

    public sealed class XmlSerializerAttributesSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"AttributeTesting", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write177_AttributeTesting(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read181_AttributeTesting();
        }
    }

    public sealed class ItemChoiceTypeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ItemChoiceType", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write178_ItemChoiceType(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read182_ItemChoiceType();
        }
    }

    public sealed class TypeWithAnyAttributeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithAnyAttribute", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write179_TypeWithAnyAttribute(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read183_TypeWithAnyAttribute();
        }
    }

    public sealed class KnownTypesThroughConstructorSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"KnownTypesThroughConstructor", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write180_KnownTypesThroughConstructor(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read184_KnownTypesThroughConstructor();
        }
    }

    public sealed class SimpleKnownTypeValueSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"SimpleKnownTypeValue", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write181_SimpleKnownTypeValue(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read185_SimpleKnownTypeValue();
        }
    }

    public sealed class ClassImplementingIXmlSerialiableSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ClassImplementingIXmlSerialiable", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write182_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read186_Item();
        }
    }

    public sealed class TypeWithPropertyNameSpecifiedSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithPropertyNameSpecified", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write183_TypeWithPropertyNameSpecified(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read187_TypeWithPropertyNameSpecified();
        }
    }

    public sealed class TypeWithXmlSchemaFormAttributeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithXmlSchemaFormAttribute", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write184_TypeWithXmlSchemaFormAttribute(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read188_TypeWithXmlSchemaFormAttribute();
        }
    }

    public sealed class TypeWithTypeNameInXmlTypeAttributeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"MyXmlType", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write185_MyXmlType(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read189_MyXmlType();
        }
    }

    public sealed class TypeWithSchemaFormInXmlAttributeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithSchemaFormInXmlAttribute", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write186_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read190_Item();
        }
    }

    public sealed class TypeWithNonPublicDefaultConstructorSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithNonPublicDefaultConstructor", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write187_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read191_Item();
        }
    }

    public sealed class ServerSettingsSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ServerSettings", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write188_ServerSettings(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read192_ServerSettings();
        }
    }

    public sealed class TypeWithXmlQualifiedNameSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithXmlQualifiedName", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write189_TypeWithXmlQualifiedName(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read193_TypeWithXmlQualifiedName();
        }
    }

    public sealed class TypeWith2DArrayProperty2Serializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWith2DArrayProperty2", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write190_TypeWith2DArrayProperty2(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read194_TypeWith2DArrayProperty2();
        }
    }

    public sealed class TypeWithPropertiesHavingDefaultValueSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithPropertiesHavingDefaultValue", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write191_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read195_Item();
        }
    }

    public sealed class TypeWithEnumPropertyHavingDefaultValueSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithEnumPropertyHavingDefaultValue", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write192_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read196_Item();
        }
    }

    public sealed class TypeWithEnumFlagPropertyHavingDefaultValueSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithEnumFlagPropertyHavingDefaultValue", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write193_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read197_Item();
        }
    }

    public sealed class TypeWithShouldSerializeMethodSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithShouldSerializeMethod", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write194_TypeWithShouldSerializeMethod(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read198_TypeWithShouldSerializeMethod();
        }
    }

    public sealed class KnownTypesThroughConstructorWithArrayPropertiesSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"KnownTypesThroughConstructorWithArrayProperties", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write195_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read199_Item();
        }
    }

    public sealed class KnownTypesThroughConstructorWithValueSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"KnownTypesThroughConstructorWithValue", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write196_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read200_Item();
        }
    }

    public sealed class TypeWithTypesHavingCustomFormatterSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithTypesHavingCustomFormatter", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write197_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read201_Item();
        }
    }

    public sealed class TypeWithArrayPropertyHavingChoiceSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithArrayPropertyHavingChoice", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write198_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read202_Item();
        }
    }

    public sealed class MoreChoicesSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"MoreChoices", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write199_MoreChoices(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read203_MoreChoices();
        }
    }

    public sealed class TypeWithFieldsOrderedSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithFieldsOrdered", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write200_TypeWithFieldsOrdered(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read204_TypeWithFieldsOrdered();
        }
    }

    public sealed class TypeWithKnownTypesOfCollectionsWithConflictingXmlNameSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithKnownTypesOfCollectionsWithConflictingXmlName", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write201_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read205_Item();
        }
    }

    public sealed class NamespaceTypeNameClashContainerSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Root", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write202_Root(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read206_Root();
        }
    }

    public sealed class TypeNameClashSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeClashB", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write203_TypeClashB(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read207_TypeClashB();
        }
    }

    public sealed class TypeNameClashSerializer1 : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeClashA", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write204_TypeClashA(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read208_TypeClashA();
        }
    }

    public sealed class PersonSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Person", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write205_Person(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read209_Person();
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
                    _tmp[@"TypeWithXmlElementProperty::"] = @"Read108_TypeWithXmlElementProperty";
                    _tmp[@"TypeWithXmlDocumentProperty::"] = @"Read109_TypeWithXmlDocumentProperty";
                    _tmp[@"TypeWithBinaryProperty::"] = @"Read110_TypeWithBinaryProperty";
                    _tmp[@"TypeWithDateTimeOffsetProperties::"] = @"Read111_Item";
                    _tmp[@"TypeWithTimeSpanProperty::"] = @"Read112_TypeWithTimeSpanProperty";
                    _tmp[@"TypeWithDefaultTimeSpanProperty::"] = @"Read113_Item";
                    _tmp[@"TypeWithByteProperty::"] = @"Read114_TypeWithByteProperty";
                    _tmp[@"TypeWithXmlNodeArrayProperty:::True:"] = @"Read115_TypeWithXmlNodeArrayProperty";
                    _tmp[@"Animal::"] = @"Read116_Animal";
                    _tmp[@"Dog::"] = @"Read117_Dog";
                    _tmp[@"DogBreed::"] = @"Read118_DogBreed";
                    _tmp[@"Group::"] = @"Read119_Group";
                    _tmp[@"Vehicle::"] = @"Read120_Vehicle";
                    _tmp[@"Employee::"] = @"Read121_Employee";
                    _tmp[@"BaseClass::"] = @"Read122_BaseClass";
                    _tmp[@"DerivedClass::"] = @"Read123_DerivedClass";
                    _tmp[@"PurchaseOrder:http://www.contoso1.com:PurchaseOrder:False:"] = @"Read124_PurchaseOrder";
                    _tmp[@"Address::"] = @"Read125_Address";
                    _tmp[@"OrderedItem::"] = @"Read126_OrderedItem";
                    _tmp[@"AliasedTestType::"] = @"Read127_AliasedTestType";
                    _tmp[@"BaseClass1::"] = @"Read128_BaseClass1";
                    _tmp[@"DerivedClass1::"] = @"Read129_DerivedClass1";
                    _tmp[@"MyCollection1::"] = @"Read130_ArrayOfDateTime";
                    _tmp[@"Orchestra::"] = @"Read131_Orchestra";
                    _tmp[@"Instrument::"] = @"Read132_Instrument";
                    _tmp[@"Brass::"] = @"Read133_Brass";
                    _tmp[@"Trumpet::"] = @"Read134_Trumpet";
                    _tmp[@"Pet::"] = @"Read135_Pet";
                    _tmp[@"DefaultValuesSetToNaN::"] = @"Read136_DefaultValuesSetToNaN";
                    _tmp[@"DefaultValuesSetToPositiveInfinity::"] = @"Read137_Item";
                    _tmp[@"DefaultValuesSetToNegativeInfinity::"] = @"Read138_Item";
                    _tmp[@"TypeWithMismatchBetweenAttributeAndPropertyType::RootElement:True:"] = @"Read139_RootElement";
                    _tmp[@"TypeWithLinkedProperty::"] = @"Read140_TypeWithLinkedProperty";
                    _tmp[@"MsgDocumentType:http://example.com:Document:True:"] = @"Read141_Document";
                    _tmp[@"RootClass::"] = @"Read142_RootClass";
                    _tmp[@"Parameter::"] = @"Read143_Parameter";
                    _tmp[@"SerializationTypes.TypeWithDateTimeStringProperty::"] = @"Read144_TypeWithDateTimeStringProperty";
                    _tmp[@"SerializationTypes.SimpleType::"] = @"Read145_SimpleType";
                    _tmp[@"SerializationTypes.TypeWithGetSetArrayMembers::"] = @"Read146_TypeWithGetSetArrayMembers";
                    _tmp[@"SerializationTypes.TypeWithGetOnlyArrayProperties::"] = @"Read147_TypeWithGetOnlyArrayProperties";
                    _tmp[@"SerializationTypes.StructNotSerializable::"] = @"Read148_StructNotSerializable";
                    _tmp[@"SerializationTypes.TypeWithMyCollectionField::"] = @"Read149_TypeWithMyCollectionField";
                    _tmp[@"SerializationTypes.TypeWithReadOnlyMyCollectionProperty::"] = @"Read150_Item";
                    _tmp[@"SerializationTypes.MyList::"] = @"Read151_ArrayOfAnyType";
                    _tmp[@"SerializationTypes.MyEnum::"] = @"Read152_MyEnum";
                    _tmp[@"SerializationTypes.TypeWithEnumMembers::"] = @"Read153_TypeWithEnumMembers";
                    _tmp[@"SerializationTypes.DCStruct::"] = @"Read154_DCStruct";
                    _tmp[@"SerializationTypes.DCClassWithEnumAndStruct::"] = @"Read155_DCClassWithEnumAndStruct";
                    _tmp[@"SerializationTypes.BuiltInTypes::"] = @"Read156_BuiltInTypes";
                    _tmp[@"SerializationTypes.TypeA::"] = @"Read157_TypeA";
                    _tmp[@"SerializationTypes.TypeB::"] = @"Read158_TypeB";
                    _tmp[@"SerializationTypes.TypeHasArrayOfASerializedAsB::"] = @"Read159_TypeHasArrayOfASerializedAsB";
                    _tmp[@"SerializationTypes.__TypeNameWithSpecialCharacters漢ñ::"] = @"Read160_Item";
                    _tmp[@"SerializationTypes.BaseClassWithSamePropertyName::"] = @"Read161_BaseClassWithSamePropertyName";
                    _tmp[@"SerializationTypes.DerivedClassWithSameProperty::"] = @"Read162_DerivedClassWithSameProperty";
                    _tmp[@"SerializationTypes.DerivedClassWithSameProperty2::"] = @"Read163_DerivedClassWithSameProperty2";
                    _tmp[@"SerializationTypes.TypeWithDateTimePropertyAsXmlTime::"] = @"Read164_Item";
                    _tmp[@"SerializationTypes.TypeWithByteArrayAsXmlText::"] = @"Read165_TypeWithByteArrayAsXmlText";
                    _tmp[@"SerializationTypes.SimpleDC::"] = @"Read166_SimpleDC";
                    _tmp[@"SerializationTypes.TypeWithXmlTextAttributeOnArray:http://schemas.xmlsoap.org/ws/2005/04/discovery::False:"] = @"Read167_Item";
                    _tmp[@"SerializationTypes.EnumFlags::"] = @"Read168_EnumFlags";
                    _tmp[@"SerializationTypes.ClassImplementsInterface::"] = @"Read169_ClassImplementsInterface";
                    _tmp[@"SerializationTypes.WithStruct::"] = @"Read170_WithStruct";
                    _tmp[@"SerializationTypes.SomeStruct::"] = @"Read171_SomeStruct";
                    _tmp[@"SerializationTypes.WithEnums::"] = @"Read172_WithEnums";
                    _tmp[@"SerializationTypes.WithNullables::"] = @"Read173_WithNullables";
                    _tmp[@"SerializationTypes.ByteEnum::"] = @"Read174_ByteEnum";
                    _tmp[@"SerializationTypes.SByteEnum::"] = @"Read175_SByteEnum";
                    _tmp[@"SerializationTypes.ShortEnum::"] = @"Read176_ShortEnum";
                    _tmp[@"SerializationTypes.IntEnum::"] = @"Read177_IntEnum";
                    _tmp[@"SerializationTypes.UIntEnum::"] = @"Read178_UIntEnum";
                    _tmp[@"SerializationTypes.LongEnum::"] = @"Read179_LongEnum";
                    _tmp[@"SerializationTypes.ULongEnum::"] = @"Read180_ULongEnum";
                    _tmp[@"SerializationTypes.XmlSerializerAttributes::AttributeTesting:False:"] = @"Read181_AttributeTesting";
                    _tmp[@"SerializationTypes.ItemChoiceType::"] = @"Read182_ItemChoiceType";
                    _tmp[@"SerializationTypes.TypeWithAnyAttribute::"] = @"Read183_TypeWithAnyAttribute";
                    _tmp[@"SerializationTypes.KnownTypesThroughConstructor::"] = @"Read184_KnownTypesThroughConstructor";
                    _tmp[@"SerializationTypes.SimpleKnownTypeValue::"] = @"Read185_SimpleKnownTypeValue";
                    _tmp[@"SerializationTypes.ClassImplementingIXmlSerialiable::"] = @"Read186_Item";
                    _tmp[@"SerializationTypes.TypeWithPropertyNameSpecified::"] = @"Read187_TypeWithPropertyNameSpecified";
                    _tmp[@"SerializationTypes.TypeWithXmlSchemaFormAttribute:::True:"] = @"Read188_TypeWithXmlSchemaFormAttribute";
                    _tmp[@"SerializationTypes.TypeWithTypeNameInXmlTypeAttribute::"] = @"Read189_MyXmlType";
                    _tmp[@"SerializationTypes.TypeWithSchemaFormInXmlAttribute::"] = @"Read190_Item";
                    _tmp[@"SerializationTypes.TypeWithNonPublicDefaultConstructor::"] = @"Read191_Item";
                    _tmp[@"SerializationTypes.ServerSettings::"] = @"Read192_ServerSettings";
                    _tmp[@"SerializationTypes.TypeWithXmlQualifiedName::"] = @"Read193_TypeWithXmlQualifiedName";
                    _tmp[@"SerializationTypes.TypeWith2DArrayProperty2::"] = @"Read194_TypeWith2DArrayProperty2";
                    _tmp[@"SerializationTypes.TypeWithPropertiesHavingDefaultValue::"] = @"Read195_Item";
                    _tmp[@"SerializationTypes.TypeWithEnumPropertyHavingDefaultValue::"] = @"Read196_Item";
                    _tmp[@"SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue::"] = @"Read197_Item";
                    _tmp[@"SerializationTypes.TypeWithShouldSerializeMethod::"] = @"Read198_TypeWithShouldSerializeMethod";
                    _tmp[@"SerializationTypes.KnownTypesThroughConstructorWithArrayProperties::"] = @"Read199_Item";
                    _tmp[@"SerializationTypes.KnownTypesThroughConstructorWithValue::"] = @"Read200_Item";
                    _tmp[@"SerializationTypes.TypeWithTypesHavingCustomFormatter::"] = @"Read201_Item";
                    _tmp[@"SerializationTypes.TypeWithArrayPropertyHavingChoice::"] = @"Read202_Item";
                    _tmp[@"SerializationTypes.MoreChoices::"] = @"Read203_MoreChoices";
                    _tmp[@"SerializationTypes.TypeWithFieldsOrdered::"] = @"Read204_TypeWithFieldsOrdered";
                    _tmp[@"SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName::"] = @"Read205_Item";
                    _tmp[@"SerializationTypes.NamespaceTypeNameClashContainer::Root:True:"] = @"Read206_Root";
                    _tmp[@"SerializationTypes.TypeNameClashB.TypeNameClash::"] = @"Read207_TypeClashB";
                    _tmp[@"SerializationTypes.TypeNameClashA.TypeNameClash::"] = @"Read208_TypeClashA";
                    _tmp[@"Outer+Person::"] = @"Read209_Person";
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
                    _tmp[@"TypeWithXmlElementProperty::"] = @"Write104_TypeWithXmlElementProperty";
                    _tmp[@"TypeWithXmlDocumentProperty::"] = @"Write105_TypeWithXmlDocumentProperty";
                    _tmp[@"TypeWithBinaryProperty::"] = @"Write106_TypeWithBinaryProperty";
                    _tmp[@"TypeWithDateTimeOffsetProperties::"] = @"Write107_Item";
                    _tmp[@"TypeWithTimeSpanProperty::"] = @"Write108_TypeWithTimeSpanProperty";
                    _tmp[@"TypeWithDefaultTimeSpanProperty::"] = @"Write109_Item";
                    _tmp[@"TypeWithByteProperty::"] = @"Write110_TypeWithByteProperty";
                    _tmp[@"TypeWithXmlNodeArrayProperty:::True:"] = @"Write111_TypeWithXmlNodeArrayProperty";
                    _tmp[@"Animal::"] = @"Write112_Animal";
                    _tmp[@"Dog::"] = @"Write113_Dog";
                    _tmp[@"DogBreed::"] = @"Write114_DogBreed";
                    _tmp[@"Group::"] = @"Write115_Group";
                    _tmp[@"Vehicle::"] = @"Write116_Vehicle";
                    _tmp[@"Employee::"] = @"Write117_Employee";
                    _tmp[@"BaseClass::"] = @"Write118_BaseClass";
                    _tmp[@"DerivedClass::"] = @"Write119_DerivedClass";
                    _tmp[@"PurchaseOrder:http://www.contoso1.com:PurchaseOrder:False:"] = @"Write120_PurchaseOrder";
                    _tmp[@"Address::"] = @"Write121_Address";
                    _tmp[@"OrderedItem::"] = @"Write122_OrderedItem";
                    _tmp[@"AliasedTestType::"] = @"Write123_AliasedTestType";
                    _tmp[@"BaseClass1::"] = @"Write124_BaseClass1";
                    _tmp[@"DerivedClass1::"] = @"Write125_DerivedClass1";
                    _tmp[@"MyCollection1::"] = @"Write126_ArrayOfDateTime";
                    _tmp[@"Orchestra::"] = @"Write127_Orchestra";
                    _tmp[@"Instrument::"] = @"Write128_Instrument";
                    _tmp[@"Brass::"] = @"Write129_Brass";
                    _tmp[@"Trumpet::"] = @"Write130_Trumpet";
                    _tmp[@"Pet::"] = @"Write131_Pet";
                    _tmp[@"DefaultValuesSetToNaN::"] = @"Write132_DefaultValuesSetToNaN";
                    _tmp[@"DefaultValuesSetToPositiveInfinity::"] = @"Write133_Item";
                    _tmp[@"DefaultValuesSetToNegativeInfinity::"] = @"Write134_Item";
                    _tmp[@"TypeWithMismatchBetweenAttributeAndPropertyType::RootElement:True:"] = @"Write135_RootElement";
                    _tmp[@"TypeWithLinkedProperty::"] = @"Write136_TypeWithLinkedProperty";
                    _tmp[@"MsgDocumentType:http://example.com:Document:True:"] = @"Write137_Document";
                    _tmp[@"RootClass::"] = @"Write138_RootClass";
                    _tmp[@"Parameter::"] = @"Write139_Parameter";
                    _tmp[@"SerializationTypes.TypeWithDateTimeStringProperty::"] = @"Write140_TypeWithDateTimeStringProperty";
                    _tmp[@"SerializationTypes.SimpleType::"] = @"Write141_SimpleType";
                    _tmp[@"SerializationTypes.TypeWithGetSetArrayMembers::"] = @"Write142_TypeWithGetSetArrayMembers";
                    _tmp[@"SerializationTypes.TypeWithGetOnlyArrayProperties::"] = @"Write143_TypeWithGetOnlyArrayProperties";
                    _tmp[@"SerializationTypes.StructNotSerializable::"] = @"Write144_StructNotSerializable";
                    _tmp[@"SerializationTypes.TypeWithMyCollectionField::"] = @"Write145_TypeWithMyCollectionField";
                    _tmp[@"SerializationTypes.TypeWithReadOnlyMyCollectionProperty::"] = @"Write146_Item";
                    _tmp[@"SerializationTypes.MyList::"] = @"Write147_ArrayOfAnyType";
                    _tmp[@"SerializationTypes.MyEnum::"] = @"Write148_MyEnum";
                    _tmp[@"SerializationTypes.TypeWithEnumMembers::"] = @"Write149_TypeWithEnumMembers";
                    _tmp[@"SerializationTypes.DCStruct::"] = @"Write150_DCStruct";
                    _tmp[@"SerializationTypes.DCClassWithEnumAndStruct::"] = @"Write151_DCClassWithEnumAndStruct";
                    _tmp[@"SerializationTypes.BuiltInTypes::"] = @"Write152_BuiltInTypes";
                    _tmp[@"SerializationTypes.TypeA::"] = @"Write153_TypeA";
                    _tmp[@"SerializationTypes.TypeB::"] = @"Write154_TypeB";
                    _tmp[@"SerializationTypes.TypeHasArrayOfASerializedAsB::"] = @"Write155_TypeHasArrayOfASerializedAsB";
                    _tmp[@"SerializationTypes.__TypeNameWithSpecialCharacters漢ñ::"] = @"Write156_Item";
                    _tmp[@"SerializationTypes.BaseClassWithSamePropertyName::"] = @"Write157_BaseClassWithSamePropertyName";
                    _tmp[@"SerializationTypes.DerivedClassWithSameProperty::"] = @"Write158_DerivedClassWithSameProperty";
                    _tmp[@"SerializationTypes.DerivedClassWithSameProperty2::"] = @"Write159_DerivedClassWithSameProperty2";
                    _tmp[@"SerializationTypes.TypeWithDateTimePropertyAsXmlTime::"] = @"Write160_Item";
                    _tmp[@"SerializationTypes.TypeWithByteArrayAsXmlText::"] = @"Write161_TypeWithByteArrayAsXmlText";
                    _tmp[@"SerializationTypes.SimpleDC::"] = @"Write162_SimpleDC";
                    _tmp[@"SerializationTypes.TypeWithXmlTextAttributeOnArray:http://schemas.xmlsoap.org/ws/2005/04/discovery::False:"] = @"Write163_Item";
                    _tmp[@"SerializationTypes.EnumFlags::"] = @"Write164_EnumFlags";
                    _tmp[@"SerializationTypes.ClassImplementsInterface::"] = @"Write165_ClassImplementsInterface";
                    _tmp[@"SerializationTypes.WithStruct::"] = @"Write166_WithStruct";
                    _tmp[@"SerializationTypes.SomeStruct::"] = @"Write167_SomeStruct";
                    _tmp[@"SerializationTypes.WithEnums::"] = @"Write168_WithEnums";
                    _tmp[@"SerializationTypes.WithNullables::"] = @"Write169_WithNullables";
                    _tmp[@"SerializationTypes.ByteEnum::"] = @"Write170_ByteEnum";
                    _tmp[@"SerializationTypes.SByteEnum::"] = @"Write171_SByteEnum";
                    _tmp[@"SerializationTypes.ShortEnum::"] = @"Write172_ShortEnum";
                    _tmp[@"SerializationTypes.IntEnum::"] = @"Write173_IntEnum";
                    _tmp[@"SerializationTypes.UIntEnum::"] = @"Write174_UIntEnum";
                    _tmp[@"SerializationTypes.LongEnum::"] = @"Write175_LongEnum";
                    _tmp[@"SerializationTypes.ULongEnum::"] = @"Write176_ULongEnum";
                    _tmp[@"SerializationTypes.XmlSerializerAttributes::AttributeTesting:False:"] = @"Write177_AttributeTesting";
                    _tmp[@"SerializationTypes.ItemChoiceType::"] = @"Write178_ItemChoiceType";
                    _tmp[@"SerializationTypes.TypeWithAnyAttribute::"] = @"Write179_TypeWithAnyAttribute";
                    _tmp[@"SerializationTypes.KnownTypesThroughConstructor::"] = @"Write180_KnownTypesThroughConstructor";
                    _tmp[@"SerializationTypes.SimpleKnownTypeValue::"] = @"Write181_SimpleKnownTypeValue";
                    _tmp[@"SerializationTypes.ClassImplementingIXmlSerialiable::"] = @"Write182_Item";
                    _tmp[@"SerializationTypes.TypeWithPropertyNameSpecified::"] = @"Write183_TypeWithPropertyNameSpecified";
                    _tmp[@"SerializationTypes.TypeWithXmlSchemaFormAttribute:::True:"] = @"Write184_TypeWithXmlSchemaFormAttribute";
                    _tmp[@"SerializationTypes.TypeWithTypeNameInXmlTypeAttribute::"] = @"Write185_MyXmlType";
                    _tmp[@"SerializationTypes.TypeWithSchemaFormInXmlAttribute::"] = @"Write186_Item";
                    _tmp[@"SerializationTypes.TypeWithNonPublicDefaultConstructor::"] = @"Write187_Item";
                    _tmp[@"SerializationTypes.ServerSettings::"] = @"Write188_ServerSettings";
                    _tmp[@"SerializationTypes.TypeWithXmlQualifiedName::"] = @"Write189_TypeWithXmlQualifiedName";
                    _tmp[@"SerializationTypes.TypeWith2DArrayProperty2::"] = @"Write190_TypeWith2DArrayProperty2";
                    _tmp[@"SerializationTypes.TypeWithPropertiesHavingDefaultValue::"] = @"Write191_Item";
                    _tmp[@"SerializationTypes.TypeWithEnumPropertyHavingDefaultValue::"] = @"Write192_Item";
                    _tmp[@"SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue::"] = @"Write193_Item";
                    _tmp[@"SerializationTypes.TypeWithShouldSerializeMethod::"] = @"Write194_TypeWithShouldSerializeMethod";
                    _tmp[@"SerializationTypes.KnownTypesThroughConstructorWithArrayProperties::"] = @"Write195_Item";
                    _tmp[@"SerializationTypes.KnownTypesThroughConstructorWithValue::"] = @"Write196_Item";
                    _tmp[@"SerializationTypes.TypeWithTypesHavingCustomFormatter::"] = @"Write197_Item";
                    _tmp[@"SerializationTypes.TypeWithArrayPropertyHavingChoice::"] = @"Write198_Item";
                    _tmp[@"SerializationTypes.MoreChoices::"] = @"Write199_MoreChoices";
                    _tmp[@"SerializationTypes.TypeWithFieldsOrdered::"] = @"Write200_TypeWithFieldsOrdered";
                    _tmp[@"SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName::"] = @"Write201_Item";
                    _tmp[@"SerializationTypes.NamespaceTypeNameClashContainer::Root:True:"] = @"Write202_Root";
                    _tmp[@"SerializationTypes.TypeNameClashB.TypeNameClash::"] = @"Write203_TypeClashB";
                    _tmp[@"SerializationTypes.TypeNameClashA.TypeNameClash::"] = @"Write204_TypeClashA";
                    _tmp[@"Outer+Person::"] = @"Write205_Person";
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
                    _tmp.Add(@"DefaultValuesSetToPositiveInfinity::", new DefaultValuesSetToPositiveInfinitySerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithFieldsOrdered::", new TypeWithFieldsOrderedSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithByteArrayAsXmlText::", new TypeWithByteArrayAsXmlTextSerializer());
                    _tmp.Add(@"Brass::", new BrassSerializer());
                    _tmp.Add(@"BaseClass1::", new BaseClass1Serializer());
                    _tmp.Add(@"DerivedClass::", new DerivedClassSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithAnyAttribute::", new TypeWithAnyAttributeSerializer());
                    _tmp.Add(@"SerializationTypes.ULongEnum::", new ULongEnumSerializer());
                    _tmp.Add(@"Vehicle::", new VehicleSerializer());
                    _tmp.Add(@"Orchestra::", new OrchestraSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue::", new TypeWithEnumFlagPropertyHavingDefaultValueSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithXmlQualifiedName::", new TypeWithXmlQualifiedNameSerializer());
                    _tmp.Add(@"SerializationTypes.WithNullables::", new WithNullablesSerializer());
                    _tmp.Add(@"RootClass::", new RootClassSerializer());
                    _tmp.Add(@"SerializationTypes.BaseClassWithSamePropertyName::", new BaseClassWithSamePropertyNameSerializer());
                    _tmp.Add(@"DefaultValuesSetToNegativeInfinity::", new DefaultValuesSetToNegativeInfinitySerializer());
                    _tmp.Add(@"SerializationTypes.TypeNameClashA.TypeNameClash::", new TypeNameClashSerializer1());
                    _tmp.Add(@"SerializationTypes.TypeWithTypeNameInXmlTypeAttribute::", new TypeWithTypeNameInXmlTypeAttributeSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithPropertyNameSpecified::", new TypeWithPropertyNameSpecifiedSerializer());
                    _tmp.Add(@"TypeWithDateTimeOffsetProperties::", new TypeWithDateTimeOffsetPropertiesSerializer());
                    _tmp.Add(@"TypeWithByteProperty::", new TypeWithBytePropertySerializer());
                    _tmp.Add(@"TypeWithLinkedProperty::", new TypeWithLinkedPropertySerializer());
                    _tmp.Add(@"TypeWithXmlDocumentProperty::", new TypeWithXmlDocumentPropertySerializer());
                    _tmp.Add(@"Trumpet::", new TrumpetSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithGetSetArrayMembers::", new TypeWithGetSetArrayMembersSerializer());
                    _tmp.Add(@"SerializationTypes.ClassImplementsInterface::", new ClassImplementsInterfaceSerializer());
                    _tmp.Add(@"SerializationTypes.XmlSerializerAttributes::AttributeTesting:False:", new XmlSerializerAttributesSerializer());
                    _tmp.Add(@"SerializationTypes.StructNotSerializable::", new StructNotSerializableSerializer());
                    _tmp.Add(@"SerializationTypes.KnownTypesThroughConstructorWithValue::", new KnownTypesThroughConstructorWithValueSerializer());
                    _tmp.Add(@"SerializationTypes.WithStruct::", new WithStructSerializer());
                    _tmp.Add(@"TypeWithBinaryProperty::", new TypeWithBinaryPropertySerializer());
                    _tmp.Add(@"TypeWithMismatchBetweenAttributeAndPropertyType::RootElement:True:", new TypeWithMismatchBetweenAttributeAndPropertyTypeSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithTypesHavingCustomFormatter::", new TypeWithTypesHavingCustomFormatterSerializer());
                    _tmp.Add(@"Address::", new AddressSerializer());
                    _tmp.Add(@"OrderedItem::", new OrderedItemSerializer());
                    _tmp.Add(@"TypeWithXmlNodeArrayProperty:::True:", new TypeWithXmlNodeArrayPropertySerializer());
                    _tmp.Add(@"SerializationTypes.TypeWith2DArrayProperty2::", new TypeWith2DArrayProperty2Serializer());
                    _tmp.Add(@"SerializationTypes.DCClassWithEnumAndStruct::", new DCClassWithEnumAndStructSerializer());
                    _tmp.Add(@"SerializationTypes.ItemChoiceType::", new ItemChoiceTypeSerializer());
                    _tmp.Add(@"SerializationTypes.ShortEnum::", new ShortEnumSerializer());
                    _tmp.Add(@"TypeWithDefaultTimeSpanProperty::", new TypeWithDefaultTimeSpanPropertySerializer());
                    _tmp.Add(@"SerializationTypes.LongEnum::", new LongEnumSerializer());
                    _tmp.Add(@"SerializationTypes.TypeHasArrayOfASerializedAsB::", new TypeHasArrayOfASerializedAsBSerializer());
                    _tmp.Add(@"SerializationTypes.WithEnums::", new WithEnumsSerializer());
                    _tmp.Add(@"SerializationTypes.ServerSettings::", new ServerSettingsSerializer());
                    _tmp.Add(@"SerializationTypes.SimpleKnownTypeValue::", new SimpleKnownTypeValueSerializer());
                    _tmp.Add(@"Outer+Person::", new PersonSerializer());
                    _tmp.Add(@"MyCollection1::", new MyCollection1Serializer());
                    _tmp.Add(@"Pet::", new PetSerializer());
                    _tmp.Add(@"TypeWithTimeSpanProperty::", new TypeWithTimeSpanPropertySerializer());
                    _tmp.Add(@"Animal::", new AnimalSerializer());
                    _tmp.Add(@"SerializationTypes.__TypeNameWithSpecialCharacters漢ñ::", new __TypeNameWithSpecialCharacters漢ñSerializer());
                    _tmp.Add(@"TypeWithXmlElementProperty::", new TypeWithXmlElementPropertySerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithShouldSerializeMethod::", new TypeWithShouldSerializeMethodSerializer());
                    _tmp.Add(@"SerializationTypes.KnownTypesThroughConstructor::", new KnownTypesThroughConstructorSerializer());
                    _tmp.Add(@"SerializationTypes.EnumFlags::", new EnumFlagsSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithXmlTextAttributeOnArray:http://schemas.xmlsoap.org/ws/2005/04/discovery::False:", new TypeWithXmlTextAttributeOnArraySerializer());
                    _tmp.Add(@"SerializationTypes.SimpleDC::", new SimpleDCSerializer());
                    _tmp.Add(@"Employee::", new EmployeeSerializer());
                    _tmp.Add(@"SerializationTypes.SomeStruct::", new SomeStructSerializer());
                    _tmp.Add(@"Dog::", new DogSerializer());
                    _tmp.Add(@"SerializationTypes.KnownTypesThroughConstructorWithArrayProperties::", new KnownTypesThroughConstructorWithArrayPropertiesSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithNonPublicDefaultConstructor::", new TypeWithNonPublicDefaultConstructorSerializer());
                    _tmp.Add(@"PurchaseOrder:http://www.contoso1.com:PurchaseOrder:False:", new PurchaseOrderSerializer());
                    _tmp.Add(@"SerializationTypes.TypeB::", new TypeBSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithSchemaFormInXmlAttribute::", new TypeWithSchemaFormInXmlAttributeSerializer());
                    _tmp.Add(@"Instrument::", new InstrumentSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithXmlSchemaFormAttribute:::True:", new TypeWithXmlSchemaFormAttributeSerializer());
                    _tmp.Add(@"MsgDocumentType:http://example.com:Document:True:", new MsgDocumentTypeSerializer());
                    _tmp.Add(@"Group::", new GroupSerializer());
                    _tmp.Add(@"SerializationTypes.MoreChoices::", new MoreChoicesSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithDateTimePropertyAsXmlTime::", new TypeWithDateTimePropertyAsXmlTimeSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithEnumMembers::", new TypeWithEnumMembersSerializer());
                    _tmp.Add(@"SerializationTypes.TypeNameClashB.TypeNameClash::", new TypeNameClashSerializer());
                    _tmp.Add(@"SerializationTypes.UIntEnum::", new UIntEnumSerializer());
                    _tmp.Add(@"SerializationTypes.TypeA::", new TypeASerializer());
                    _tmp.Add(@"BaseClass::", new BaseClassSerializer());
                    _tmp.Add(@"SerializationTypes.ByteEnum::", new ByteEnumSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithGetOnlyArrayProperties::", new TypeWithGetOnlyArrayPropertiesSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName::", new TypeWithKnownTypesOfCollectionsWithConflictingXmlNameSerializer());
                    _tmp.Add(@"DogBreed::", new DogBreedSerializer());
                    _tmp.Add(@"SerializationTypes.IntEnum::", new IntEnumSerializer());
                    _tmp.Add(@"SerializationTypes.MyEnum::", new MyEnumSerializer());
                    _tmp.Add(@"SerializationTypes.SimpleType::", new SimpleTypeSerializer());
                    _tmp.Add(@"SerializationTypes.DerivedClassWithSameProperty::", new DerivedClassWithSamePropertySerializer());
                    _tmp.Add(@"SerializationTypes.DerivedClassWithSameProperty2::", new DerivedClassWithSameProperty2Serializer());
                    _tmp.Add(@"SerializationTypes.TypeWithMyCollectionField::", new TypeWithMyCollectionFieldSerializer());
                    _tmp.Add(@"SerializationTypes.BuiltInTypes::", new BuiltInTypesSerializer());
                    _tmp.Add(@"AliasedTestType::", new AliasedTestTypeSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithDateTimeStringProperty::", new TypeWithDateTimeStringPropertySerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithReadOnlyMyCollectionProperty::", new TypeWithReadOnlyMyCollectionPropertySerializer());
                    _tmp.Add(@"SerializationTypes.ClassImplementingIXmlSerialiable::", new ClassImplementingIXmlSerialiableSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithEnumPropertyHavingDefaultValue::", new TypeWithEnumPropertyHavingDefaultValueSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithArrayPropertyHavingChoice::", new TypeWithArrayPropertyHavingChoiceSerializer());
                    _tmp.Add(@"SerializationTypes.SByteEnum::", new SByteEnumSerializer());
                    _tmp.Add(@"DerivedClass1::", new DerivedClass1Serializer());
                    _tmp.Add(@"SerializationTypes.DCStruct::", new DCStructSerializer());
                    _tmp.Add(@"SerializationTypes.MyList::", new MyListSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithPropertiesHavingDefaultValue::", new TypeWithPropertiesHavingDefaultValueSerializer());
                    _tmp.Add(@"Parameter::", new ParameterSerializer());
                    _tmp.Add(@"SerializationTypes.NamespaceTypeNameClashContainer::Root:True:", new NamespaceTypeNameClashContainerSerializer());
                    _tmp.Add(@"DefaultValuesSetToNaN::", new DefaultValuesSetToNaNSerializer());
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
            if (type == typeof(global::SerializationTypes.ClassImplementingIXmlSerialiable)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithPropertyNameSpecified)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithXmlSchemaFormAttribute)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithTypeNameInXmlTypeAttribute)) return true;
            if (type == typeof(global::SerializationTypes.TypeWithSchemaFormInXmlAttribute)) return true;
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
            if (type == typeof(global::SerializationTypes.ClassImplementingIXmlSerialiable)) return new ClassImplementingIXmlSerialiableSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithPropertyNameSpecified)) return new TypeWithPropertyNameSpecifiedSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithXmlSchemaFormAttribute)) return new TypeWithXmlSchemaFormAttributeSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithTypeNameInXmlTypeAttribute)) return new TypeWithTypeNameInXmlTypeAttributeSerializer();
            if (type == typeof(global::SerializationTypes.TypeWithSchemaFormInXmlAttribute)) return new TypeWithSchemaFormInXmlAttributeSerializer();
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
