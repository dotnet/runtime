[assembly:System.Security.AllowPartiallyTrustedCallers()]
[assembly:System.Security.SecurityTransparent()]
[assembly:System.Security.SecurityRules(System.Security.SecurityRuleSet.Level1)]
[assembly:System.Xml.Serialization.XmlSerializerVersionAttribute(ParentAssemblyId=@"%%ParentAssemblyId%%", Version=@"1.0.0.0")]
namespace Microsoft.Xml.Serialization.GeneratedAssembly {

    public class XmlSerializationWriter1 : System.Xml.Serialization.XmlSerializationWriter {

        public void Write121_TypeWithXmlElementProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithXmlElementProperty", @"");
                return;
            }
            TopLevelElement();
            Write2_TypeWithXmlElementProperty(@"TypeWithXmlElementProperty", @"", ((global::TypeWithXmlElementProperty)o), true, false);
        }

        public void Write122_TypeWithXmlDocumentProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithXmlDocumentProperty", @"");
                return;
            }
            TopLevelElement();
            Write3_TypeWithXmlDocumentProperty(@"TypeWithXmlDocumentProperty", @"", ((global::TypeWithXmlDocumentProperty)o), true, false);
        }

        public void Write123_TypeWithBinaryProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithBinaryProperty", @"");
                return;
            }
            TopLevelElement();
            Write4_TypeWithBinaryProperty(@"TypeWithBinaryProperty", @"", ((global::TypeWithBinaryProperty)o), true, false);
        }

        public void Write124_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithDateTimeOffsetProperties", @"");
                return;
            }
            TopLevelElement();
            Write5_Item(@"TypeWithDateTimeOffsetProperties", @"", ((global::TypeWithDateTimeOffsetProperties)o), true, false);
        }

        public void Write125_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithDateAndTimeOnlyProperties", @"");
                return;
            }
            TopLevelElement();
            Write6_Item(@"TypeWithDateAndTimeOnlyProperties", @"", ((global::TypeWithDateAndTimeOnlyProperties)o), true, false);
        }

        public void Write126_DateOnlyWrapper(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DateOnlyWrapper", @"");
                return;
            }
            TopLevelElement();
            Write7_DateOnlyWrapper(@"DateOnlyWrapper", @"", ((global::DateOnlyWrapper)o), true, false);
        }

        public void Write127_TimeOnlyWrapper(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TimeOnlyWrapper", @"");
                return;
            }
            TopLevelElement();
            Write8_TimeOnlyWrapper(@"TimeOnlyWrapper", @"", ((global::TimeOnlyWrapper)o), true, false);
        }

        public void Write128_TimeOnlyAsXsdTimeWrapper(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TimeOnlyAsXsdTimeWrapper", @"");
                return;
            }
            TopLevelElement();
            Write9_TimeOnlyAsXsdTimeWrapper(@"TimeOnlyAsXsdTimeWrapper", @"", ((global::TimeOnlyAsXsdTimeWrapper)o), true, false);
        }

        public void Write129_DateTimeDateWrapper(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DateTimeDateWrapper", @"");
                return;
            }
            TopLevelElement();
            Write10_DateTimeDateWrapper(@"DateTimeDateWrapper", @"", ((global::DateTimeDateWrapper)o), true, false);
        }

        public void Write130_DateTimeTimeWrapper(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DateTimeTimeWrapper", @"");
                return;
            }
            TopLevelElement();
            Write11_DateTimeTimeWrapper(@"DateTimeTimeWrapper", @"", ((global::DateTimeTimeWrapper)o), true, false);
        }

        public void Write131_TypeWithTimeSpanProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithTimeSpanProperty", @"");
                return;
            }
            TopLevelElement();
            Write12_TypeWithTimeSpanProperty(@"TypeWithTimeSpanProperty", @"", ((global::TypeWithTimeSpanProperty)o), true, false);
        }

        public void Write132_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithDefaultTimeSpanProperty", @"");
                return;
            }
            TopLevelElement();
            Write13_Item(@"TypeWithDefaultTimeSpanProperty", @"", ((global::TypeWithDefaultTimeSpanProperty)o), true, false);
        }

        public void Write133_TypeWithByteProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithByteProperty", @"");
                return;
            }
            TopLevelElement();
            Write14_TypeWithByteProperty(@"TypeWithByteProperty", @"", ((global::TypeWithByteProperty)o), true, false);
        }

        public void Write134_TypeWithXmlNodeArrayProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithXmlNodeArrayProperty", @"");
                return;
            }
            TopLevelElement();
            Write15_TypeWithXmlNodeArrayProperty(@"TypeWithXmlNodeArrayProperty", @"", ((global::TypeWithXmlNodeArrayProperty)o), true, false);
        }

        public void Write135_Animal(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Animal", @"");
                return;
            }
            TopLevelElement();
            Write16_Animal(@"Animal", @"", ((global::Animal)o), true, false);
        }

        public void Write136_Dog(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Dog", @"");
                return;
            }
            TopLevelElement();
            Write18_Dog(@"Dog", @"", ((global::Dog)o), true, false);
        }

        public void Write137_DogBreed(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"DogBreed", @"");
                return;
            }
            WriteElementString(@"DogBreed", @"", Write17_DogBreed(((global::DogBreed)o)));
        }

        public void Write138_Group(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Group", @"");
                return;
            }
            TopLevelElement();
            Write20_Group(@"Group", @"", ((global::Group)o), true, false);
        }

        public void Write139_Vehicle(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Vehicle", @"");
                return;
            }
            TopLevelElement();
            Write19_Vehicle(@"Vehicle", @"", ((global::Vehicle)o), true, false);
        }

        public void Write140_Employee(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Employee", @"");
                return;
            }
            TopLevelElement();
            Write21_Employee(@"Employee", @"", ((global::Employee)o), true, false);
        }

        public void Write141_BaseClass(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"BaseClass", @"");
                return;
            }
            TopLevelElement();
            Write23_BaseClass(@"BaseClass", @"", ((global::BaseClass)o), true, false);
        }

        public void Write142_DerivedClass(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DerivedClass", @"");
                return;
            }
            TopLevelElement();
            Write22_DerivedClass(@"DerivedClass", @"", ((global::DerivedClass)o), true, false);
        }

        public void Write143_SimpleBaseClass(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"SimpleBaseClass", @"");
                return;
            }
            TopLevelElement();
            Write25_SimpleBaseClass(@"SimpleBaseClass", @"", ((global::SimpleBaseClass)o), true, false);
        }

        public void Write144_SimpleDerivedClass(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"SimpleDerivedClass", @"");
                return;
            }
            TopLevelElement();
            Write24_SimpleDerivedClass(@"SimpleDerivedClass", @"", ((global::SimpleDerivedClass)o), true, false);
        }

        public void Write145_BaseIXmlSerializable(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"BaseIXmlSerializable", @"http://example.com/serializer-test-namespace");
                return;
            }
            TopLevelElement();
            WriteSerializable((System.Xml.Serialization.IXmlSerializable)((global::XmlSerializableBaseClass)o), @"BaseIXmlSerializable", @"http://example.com/serializer-test-namespace", true, true);
        }

        public void Write146_DerivedIXmlSerializable(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DerivedIXmlSerializable", @"");
                return;
            }
            TopLevelElement();
            WriteSerializable((System.Xml.Serialization.IXmlSerializable)((global::XmlSerializableDerivedClass)o), @"DerivedIXmlSerializable", @"", true, true);
        }

        public void Write147_PurchaseOrder(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"PurchaseOrder", @"http://www.contoso1.com");
                return;
            }
            TopLevelElement();
            Write28_PurchaseOrder(@"PurchaseOrder", @"http://www.contoso1.com", ((global::PurchaseOrder)o), false, false);
        }

        public void Write148_Address(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Address", @"");
                return;
            }
            TopLevelElement();
            Write29_Address(@"Address", @"", ((global::Address)o), true, false);
        }

        public void Write149_OrderedItem(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"OrderedItem", @"");
                return;
            }
            TopLevelElement();
            Write30_OrderedItem(@"OrderedItem", @"", ((global::OrderedItem)o), true, false);
        }

        public void Write150_AliasedTestType(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"AliasedTestType", @"");
                return;
            }
            TopLevelElement();
            Write31_AliasedTestType(@"AliasedTestType", @"", ((global::AliasedTestType)o), true, false);
        }

        public void Write151_BaseClass1(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"BaseClass1", @"");
                return;
            }
            TopLevelElement();
            Write32_BaseClass1(@"BaseClass1", @"", ((global::BaseClass1)o), true, false);
        }

        public void Write152_DerivedClass1(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DerivedClass1", @"");
                return;
            }
            TopLevelElement();
            Write33_DerivedClass1(@"DerivedClass1", @"", ((global::DerivedClass1)o), true, false);
        }

        public void Write153_ArrayOfDateTime(object o) {
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

        public void Write154_Orchestra(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Orchestra", @"");
                return;
            }
            TopLevelElement();
            Write35_Orchestra(@"Orchestra", @"", ((global::Orchestra)o), true, false);
        }

        public void Write155_Instrument(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Instrument", @"");
                return;
            }
            TopLevelElement();
            Write34_Instrument(@"Instrument", @"", ((global::Instrument)o), true, false);
        }

        public void Write156_Brass(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Brass", @"");
                return;
            }
            TopLevelElement();
            Write36_Brass(@"Brass", @"", ((global::Brass)o), true, false);
        }

        public void Write157_Trumpet(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Trumpet", @"");
                return;
            }
            TopLevelElement();
            Write37_Trumpet(@"Trumpet", @"", ((global::Trumpet)o), true, false);
        }

        public void Write158_Pet(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Pet", @"");
                return;
            }
            TopLevelElement();
            Write38_Pet(@"Pet", @"", ((global::Pet)o), true, false);
        }

        public void Write159_DefaultValuesSetToNaN(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DefaultValuesSetToNaN", @"");
                return;
            }
            TopLevelElement();
            Write39_DefaultValuesSetToNaN(@"DefaultValuesSetToNaN", @"", ((global::DefaultValuesSetToNaN)o), true, false);
        }

        public void Write160_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DefaultValuesSetToPositiveInfinity", @"");
                return;
            }
            TopLevelElement();
            Write40_Item(@"DefaultValuesSetToPositiveInfinity", @"", ((global::DefaultValuesSetToPositiveInfinity)o), true, false);
        }

        public void Write161_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DefaultValuesSetToNegativeInfinity", @"");
                return;
            }
            TopLevelElement();
            Write41_Item(@"DefaultValuesSetToNegativeInfinity", @"", ((global::DefaultValuesSetToNegativeInfinity)o), true, false);
        }

        public void Write162_RootElement(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"RootElement", @"");
                return;
            }
            TopLevelElement();
            Write42_Item(@"RootElement", @"", ((global::TypeWithMismatchBetweenAttributeAndPropertyType)o), true, false);
        }

        public void Write163_TypeWithLinkedProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithLinkedProperty", @"");
                return;
            }
            TopLevelElement();
            Write43_TypeWithLinkedProperty(@"TypeWithLinkedProperty", @"", ((global::TypeWithLinkedProperty)o), true, false);
        }

        public void Write164_Document(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Document", @"http://example.com");
                return;
            }
            TopLevelElement();
            Write44_MsgDocumentType(@"Document", @"http://example.com", ((global::MsgDocumentType)o), true, false);
        }

        public void Write165_RootClass(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"RootClass", @"");
                return;
            }
            TopLevelElement();
            Write47_RootClass(@"RootClass", @"", ((global::RootClass)o), true, false);
        }

        public void Write166_Parameter(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Parameter", @"");
                return;
            }
            TopLevelElement();
            Write46_Parameter(@"Parameter", @"", ((global::Parameter)o), true, false);
        }

        public void Write167_XElementWrapper(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"XElementWrapper", @"");
                return;
            }
            TopLevelElement();
            Write48_XElementWrapper(@"XElementWrapper", @"", ((global::XElementWrapper)o), true, false);
        }

        public void Write168_XElementStruct(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"XElementStruct", @"");
                return;
            }
            Write49_XElementStruct(@"XElementStruct", @"", ((global::XElementStruct)o), false);
        }

        public void Write169_XElementArrayWrapper(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"XElementArrayWrapper", @"");
                return;
            }
            TopLevelElement();
            Write50_XElementArrayWrapper(@"XElementArrayWrapper", @"", ((global::XElementArrayWrapper)o), true, false);
        }

        public void Write170_TypeWithDateTimeStringProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithDateTimeStringProperty", @"");
                return;
            }
            TopLevelElement();
            Write51_TypeWithDateTimeStringProperty(@"TypeWithDateTimeStringProperty", @"", ((global::SerializationTypes.TypeWithDateTimeStringProperty)o), true, false);
        }

        public void Write171_SimpleType(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"SimpleType", @"");
                return;
            }
            TopLevelElement();
            Write52_SimpleType(@"SimpleType", @"", ((global::SerializationTypes.SimpleType)o), true, false);
        }

        public void Write172_TypeWithGetSetArrayMembers(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithGetSetArrayMembers", @"");
                return;
            }
            TopLevelElement();
            Write53_TypeWithGetSetArrayMembers(@"TypeWithGetSetArrayMembers", @"", ((global::SerializationTypes.TypeWithGetSetArrayMembers)o), true, false);
        }

        public void Write173_TypeWithGetOnlyArrayProperties(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithGetOnlyArrayProperties", @"");
                return;
            }
            TopLevelElement();
            Write54_TypeWithGetOnlyArrayProperties(@"TypeWithGetOnlyArrayProperties", @"", ((global::SerializationTypes.TypeWithGetOnlyArrayProperties)o), true, false);
        }

        public void Write174_TypeWithArraylikeMembers(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithArraylikeMembers", @"");
                return;
            }
            TopLevelElement();
            Write55_TypeWithArraylikeMembers(@"TypeWithArraylikeMembers", @"", ((global::SerializationTypes.TypeWithArraylikeMembers)o), true, false);
        }

        public void Write175_StructNotSerializable(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"StructNotSerializable", @"");
                return;
            }
            Write56_StructNotSerializable(@"StructNotSerializable", @"", ((global::SerializationTypes.StructNotSerializable)o), false);
        }

        public void Write176_TypeWithMyCollectionField(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithMyCollectionField", @"");
                return;
            }
            TopLevelElement();
            Write57_TypeWithMyCollectionField(@"TypeWithMyCollectionField", @"", ((global::SerializationTypes.TypeWithMyCollectionField)o), true, false);
        }

        public void Write177_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithReadOnlyMyCollectionProperty", @"");
                return;
            }
            TopLevelElement();
            Write58_Item(@"TypeWithReadOnlyMyCollectionProperty", @"", ((global::SerializationTypes.TypeWithReadOnlyMyCollectionProperty)o), true, false);
        }

        public void Write178_ArrayOfAnyType(object o) {
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

        public void Write179_MyEnum(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"MyEnum", @"");
                return;
            }
            WriteElementString(@"MyEnum", @"", Write59_MyEnum(((global::SerializationTypes.MyEnum)o)));
        }

        public void Write180_TypeWithEnumMembers(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithEnumMembers", @"");
                return;
            }
            TopLevelElement();
            Write60_TypeWithEnumMembers(@"TypeWithEnumMembers", @"", ((global::SerializationTypes.TypeWithEnumMembers)o), true, false);
        }

        public void Write181_DCStruct(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"DCStruct", @"");
                return;
            }
            Write61_DCStruct(@"DCStruct", @"", ((global::SerializationTypes.DCStruct)o), false);
        }

        public void Write182_DCClassWithEnumAndStruct(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DCClassWithEnumAndStruct", @"");
                return;
            }
            TopLevelElement();
            Write62_DCClassWithEnumAndStruct(@"DCClassWithEnumAndStruct", @"", ((global::SerializationTypes.DCClassWithEnumAndStruct)o), true, false);
        }

        public void Write183_BuiltInTypes(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"BuiltInTypes", @"");
                return;
            }
            TopLevelElement();
            Write63_BuiltInTypes(@"BuiltInTypes", @"", ((global::SerializationTypes.BuiltInTypes)o), true, false);
        }

        public void Write184_TypeA(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeA", @"");
                return;
            }
            TopLevelElement();
            Write64_TypeA(@"TypeA", @"", ((global::SerializationTypes.TypeA)o), true, false);
        }

        public void Write185_TypeB(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeB", @"");
                return;
            }
            TopLevelElement();
            Write65_TypeB(@"TypeB", @"", ((global::SerializationTypes.TypeB)o), true, false);
        }

        public void Write186_TypeHasArrayOfASerializedAsB(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeHasArrayOfASerializedAsB", @"");
                return;
            }
            TopLevelElement();
            Write66_TypeHasArrayOfASerializedAsB(@"TypeHasArrayOfASerializedAsB", @"", ((global::SerializationTypes.TypeHasArrayOfASerializedAsB)o), true, false);
        }

        public void Write187_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"__TypeNameWithSpecialCharacters漢ñ", @"");
                return;
            }
            TopLevelElement();
            Write67_Item(@"__TypeNameWithSpecialCharacters漢ñ", @"", ((global::SerializationTypes.@__TypeNameWithSpecialCharacters漢ñ)o), true, false);
        }

        public void Write188_BaseClassWithSamePropertyName(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"BaseClassWithSamePropertyName", @"");
                return;
            }
            TopLevelElement();
            Write68_BaseClassWithSamePropertyName(@"BaseClassWithSamePropertyName", @"", ((global::SerializationTypes.BaseClassWithSamePropertyName)o), true, false);
        }

        public void Write189_DerivedClassWithSameProperty(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DerivedClassWithSameProperty", @"");
                return;
            }
            TopLevelElement();
            Write69_DerivedClassWithSameProperty(@"DerivedClassWithSameProperty", @"", ((global::SerializationTypes.DerivedClassWithSameProperty)o), true, false);
        }

        public void Write190_DerivedClassWithSameProperty2(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"DerivedClassWithSameProperty2", @"");
                return;
            }
            TopLevelElement();
            Write70_DerivedClassWithSameProperty2(@"DerivedClassWithSameProperty2", @"", ((global::SerializationTypes.DerivedClassWithSameProperty2)o), true, false);
        }

        public void Write191_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithDateTimePropertyAsXmlTime", @"");
                return;
            }
            TopLevelElement();
            Write71_Item(@"TypeWithDateTimePropertyAsXmlTime", @"", ((global::SerializationTypes.TypeWithDateTimePropertyAsXmlTime)o), true, false);
        }

        public void Write192_TypeWithByteArrayAsXmlText(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithByteArrayAsXmlText", @"");
                return;
            }
            TopLevelElement();
            Write72_TypeWithByteArrayAsXmlText(@"TypeWithByteArrayAsXmlText", @"", ((global::SerializationTypes.TypeWithByteArrayAsXmlText)o), true, false);
        }

        public void Write193_SimpleDC(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"SimpleDC", @"");
                return;
            }
            TopLevelElement();
            Write73_SimpleDC(@"SimpleDC", @"", ((global::SerializationTypes.SimpleDC)o), true, false);
        }

        public void Write194_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"TypeWithXmlTextAttributeOnArray", @"http://schemas.xmlsoap.org/ws/2005/04/discovery");
                return;
            }
            TopLevelElement();
            Write74_Item(@"TypeWithXmlTextAttributeOnArray", @"http://schemas.xmlsoap.org/ws/2005/04/discovery", ((global::SerializationTypes.TypeWithXmlTextAttributeOnArray)o), false, false);
        }

        public void Write195_EnumFlags(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"EnumFlags", @"");
                return;
            }
            WriteElementString(@"EnumFlags", @"", Write75_EnumFlags(((global::SerializationTypes.EnumFlags)o)));
        }

        public void Write196_ClassImplementsInterface(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"ClassImplementsInterface", @"");
                return;
            }
            TopLevelElement();
            Write76_ClassImplementsInterface(@"ClassImplementsInterface", @"", ((global::SerializationTypes.ClassImplementsInterface)o), true, false);
        }

        public void Write197_WithStruct(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"WithStruct", @"");
                return;
            }
            TopLevelElement();
            Write78_WithStruct(@"WithStruct", @"", ((global::SerializationTypes.WithStruct)o), true, false);
        }

        public void Write198_SomeStruct(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"SomeStruct", @"");
                return;
            }
            Write77_SomeStruct(@"SomeStruct", @"", ((global::SerializationTypes.SomeStruct)o), false);
        }

        public void Write199_WithEnums(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"WithEnums", @"");
                return;
            }
            TopLevelElement();
            Write81_WithEnums(@"WithEnums", @"", ((global::SerializationTypes.WithEnums)o), true, false);
        }

        public void Write200_WithNullables(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"WithNullables", @"");
                return;
            }
            TopLevelElement();
            Write82_WithNullables(@"WithNullables", @"", ((global::SerializationTypes.WithNullables)o), true, false);
        }

        public void Write201_ByteEnum(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"ByteEnum", @"");
                return;
            }
            WriteElementString(@"ByteEnum", @"", Write83_ByteEnum(((global::SerializationTypes.ByteEnum)o)));
        }

        public void Write202_SByteEnum(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"SByteEnum", @"");
                return;
            }
            WriteElementString(@"SByteEnum", @"", Write84_SByteEnum(((global::SerializationTypes.SByteEnum)o)));
        }

        public void Write203_ShortEnum(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"ShortEnum", @"");
                return;
            }
            WriteElementString(@"ShortEnum", @"", Write80_ShortEnum(((global::SerializationTypes.ShortEnum)o)));
        }

        public void Write204_IntEnum(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"IntEnum", @"");
                return;
            }
            WriteElementString(@"IntEnum", @"", Write79_IntEnum(((global::SerializationTypes.IntEnum)o)));
        }

        public void Write205_UIntEnum(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"UIntEnum", @"");
                return;
            }
            WriteElementString(@"UIntEnum", @"", Write85_UIntEnum(((global::SerializationTypes.UIntEnum)o)));
        }

        public void Write206_LongEnum(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"LongEnum", @"");
                return;
            }
            WriteElementString(@"LongEnum", @"", Write86_LongEnum(((global::SerializationTypes.LongEnum)o)));
        }

        public void Write207_ULongEnum(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"ULongEnum", @"");
                return;
            }
            WriteElementString(@"ULongEnum", @"", Write87_ULongEnum(((global::SerializationTypes.ULongEnum)o)));
        }

        public void Write208_AttributeTesting(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"AttributeTesting", @"");
                return;
            }
            TopLevelElement();
            Write89_XmlSerializerAttributes(@"AttributeTesting", @"", ((global::SerializationTypes.XmlSerializerAttributes)o), false, false);
        }

        public void Write209_ItemChoiceType(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"ItemChoiceType", @"");
                return;
            }
            WriteElementString(@"ItemChoiceType", @"", Write88_ItemChoiceType(((global::SerializationTypes.ItemChoiceType)o)));
        }

        public void Write210_TypeWithAnyAttribute(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithAnyAttribute", @"");
                return;
            }
            TopLevelElement();
            Write90_TypeWithAnyAttribute(@"TypeWithAnyAttribute", @"", ((global::SerializationTypes.TypeWithAnyAttribute)o), true, false);
        }

        public void Write211_KnownTypesThroughConstructor(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"KnownTypesThroughConstructor", @"");
                return;
            }
            TopLevelElement();
            Write91_KnownTypesThroughConstructor(@"KnownTypesThroughConstructor", @"", ((global::SerializationTypes.KnownTypesThroughConstructor)o), true, false);
        }

        public void Write212_SimpleKnownTypeValue(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"SimpleKnownTypeValue", @"");
                return;
            }
            TopLevelElement();
            Write92_SimpleKnownTypeValue(@"SimpleKnownTypeValue", @"", ((global::SerializationTypes.SimpleKnownTypeValue)o), true, false);
        }

        public void Write213_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"ClassImplementingIXmlSerializable", @"");
                return;
            }
            TopLevelElement();
            WriteSerializable((System.Xml.Serialization.IXmlSerializable)((global::SerializationTypes.ClassImplementingIXmlSerializable)o), @"ClassImplementingIXmlSerializable", @"", true, true);
        }

        public void Write214_TypeWithPropertyNameSpecified(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithPropertyNameSpecified", @"");
                return;
            }
            TopLevelElement();
            Write93_TypeWithPropertyNameSpecified(@"TypeWithPropertyNameSpecified", @"", ((global::SerializationTypes.TypeWithPropertyNameSpecified)o), true, false);
        }

        public void Write215_TypeWithXmlSchemaFormAttribute(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithXmlSchemaFormAttribute", @"");
                return;
            }
            TopLevelElement();
            Write94_TypeWithXmlSchemaFormAttribute(@"TypeWithXmlSchemaFormAttribute", @"", ((global::SerializationTypes.TypeWithXmlSchemaFormAttribute)o), true, false);
        }

        public void Write216_MyXmlType(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"MyXmlType", @"");
                return;
            }
            TopLevelElement();
            Write95_Item(@"MyXmlType", @"", ((global::SerializationTypes.TypeWithTypeNameInXmlTypeAttribute)o), true, false);
        }

        public void Write217_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithSchemaFormInXmlAttribute", @"");
                return;
            }
            TopLevelElement();
            Write96_Item(@"TypeWithSchemaFormInXmlAttribute", @"", ((global::SerializationTypes.TypeWithSchemaFormInXmlAttribute)o), true, false);
        }

        public void Write218_CustomDocument(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"CustomDocument", @"");
                return;
            }
            TopLevelElement();
            Write98_CustomDocument(@"CustomDocument", @"", ((global::SerializationTypes.CustomDocument)o), true, false);
        }

        public void Write219_CustomElement(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"CustomElement", @"");
                return;
            }
            TopLevelElement();
            Write97_CustomElement(@"CustomElement", @"", ((global::SerializationTypes.CustomElement)o), true, false);
        }

        public void Write220_Item(object o) {
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

        public void Write221_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithNonPublicDefaultConstructor", @"");
                return;
            }
            TopLevelElement();
            Write99_Item(@"TypeWithNonPublicDefaultConstructor", @"", ((global::SerializationTypes.TypeWithNonPublicDefaultConstructor)o), true, false);
        }

        public void Write222_ServerSettings(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"ServerSettings", @"");
                return;
            }
            TopLevelElement();
            Write100_ServerSettings(@"ServerSettings", @"", ((global::SerializationTypes.ServerSettings)o), true, false);
        }

        public void Write223_TypeWithXmlQualifiedName(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithXmlQualifiedName", @"");
                return;
            }
            TopLevelElement();
            Write101_TypeWithXmlQualifiedName(@"TypeWithXmlQualifiedName", @"", ((global::SerializationTypes.TypeWithXmlQualifiedName)o), true, false);
        }

        public void Write224_TypeWith2DArrayProperty2(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWith2DArrayProperty2", @"");
                return;
            }
            TopLevelElement();
            Write102_TypeWith2DArrayProperty2(@"TypeWith2DArrayProperty2", @"", ((global::SerializationTypes.TypeWith2DArrayProperty2)o), true, false);
        }

        public void Write225_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithPropertiesHavingDefaultValue", @"");
                return;
            }
            TopLevelElement();
            Write103_Item(@"TypeWithPropertiesHavingDefaultValue", @"", ((global::SerializationTypes.TypeWithPropertiesHavingDefaultValue)o), true, false);
        }

        public void Write226_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithEnumPropertyHavingDefaultValue", @"");
                return;
            }
            TopLevelElement();
            Write104_Item(@"TypeWithEnumPropertyHavingDefaultValue", @"", ((global::SerializationTypes.TypeWithEnumPropertyHavingDefaultValue)o), true, false);
        }

        public void Write227_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithEnumFlagPropertyHavingDefaultValue", @"");
                return;
            }
            TopLevelElement();
            Write105_Item(@"TypeWithEnumFlagPropertyHavingDefaultValue", @"", ((global::SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue)o), true, false);
        }

        public void Write228_TypeWithShouldSerializeMethod(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithShouldSerializeMethod", @"");
                return;
            }
            TopLevelElement();
            Write106_TypeWithShouldSerializeMethod(@"TypeWithShouldSerializeMethod", @"", ((global::SerializationTypes.TypeWithShouldSerializeMethod)o), true, false);
        }

        public void Write229_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"KnownTypesThroughConstructorWithArrayProperties", @"");
                return;
            }
            TopLevelElement();
            Write107_Item(@"KnownTypesThroughConstructorWithArrayProperties", @"", ((global::SerializationTypes.KnownTypesThroughConstructorWithArrayProperties)o), true, false);
        }

        public void Write230_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"KnownTypesThroughConstructorWithValue", @"");
                return;
            }
            TopLevelElement();
            Write108_Item(@"KnownTypesThroughConstructorWithValue", @"", ((global::SerializationTypes.KnownTypesThroughConstructorWithValue)o), true, false);
        }

        public void Write231_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithTypesHavingCustomFormatter", @"");
                return;
            }
            TopLevelElement();
            Write109_Item(@"TypeWithTypesHavingCustomFormatter", @"", ((global::SerializationTypes.TypeWithTypesHavingCustomFormatter)o), true, false);
        }

        public void Write232_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithArrayPropertyHavingChoice", @"");
                return;
            }
            TopLevelElement();
            Write111_Item(@"TypeWithArrayPropertyHavingChoice", @"", ((global::SerializationTypes.TypeWithArrayPropertyHavingChoice)o), true, false);
        }

        public void Write233_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithPropertyHavingComplexChoice", @"");
                return;
            }
            TopLevelElement();
            Write114_Item(@"TypeWithPropertyHavingComplexChoice", @"", ((global::SerializationTypes.TypeWithPropertyHavingComplexChoice)o), true, false);
        }

        public void Write234_MoreChoices(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteEmptyTag(@"MoreChoices", @"");
                return;
            }
            WriteElementString(@"MoreChoices", @"", Write110_MoreChoices(((global::SerializationTypes.MoreChoices)o)));
        }

        public void Write235_ComplexChoiceA(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"ComplexChoiceA", @"");
                return;
            }
            TopLevelElement();
            Write113_ComplexChoiceA(@"ComplexChoiceA", @"", ((global::SerializationTypes.ComplexChoiceA)o), true, false);
        }

        public void Write236_ComplexChoiceB(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"ComplexChoiceB", @"");
                return;
            }
            TopLevelElement();
            Write112_ComplexChoiceB(@"ComplexChoiceB", @"", ((global::SerializationTypes.ComplexChoiceB)o), true, false);
        }

        public void Write237_TypeWithFieldsOrdered(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithFieldsOrdered", @"");
                return;
            }
            TopLevelElement();
            Write115_TypeWithFieldsOrdered(@"TypeWithFieldsOrdered", @"", ((global::SerializationTypes.TypeWithFieldsOrdered)o), true, false);
        }

        public void Write238_Item(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeWithKnownTypesOfCollectionsWithConflictingXmlName", @"");
                return;
            }
            TopLevelElement();
            Write116_Item(@"TypeWithKnownTypesOfCollectionsWithConflictingXmlName", @"", ((global::SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName)o), true, false);
        }

        public void Write239_Root(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Root", @"");
                return;
            }
            TopLevelElement();
            Write119_Item(@"Root", @"", ((global::SerializationTypes.NamespaceTypeNameClashContainer)o), true, false);
        }

        public void Write240_TypeClashB(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeClashB", @"");
                return;
            }
            TopLevelElement();
            Write118_TypeNameClash(@"TypeClashB", @"", ((global::SerializationTypes.TypeNameClashB.TypeNameClash)o), true, false);
        }

        public void Write241_TypeClashA(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"TypeClashA", @"");
                return;
            }
            TopLevelElement();
            Write117_TypeNameClash(@"TypeClashA", @"", ((global::SerializationTypes.TypeNameClashA.TypeNameClash)o), true, false);
        }

        public void Write242_Person(object o) {
            WriteStartDocument();
            if (o == null) {
                WriteNullTagLiteral(@"Person", @"");
                return;
            }
            TopLevelElement();
            Write120_Person(@"Person", @"", ((global::Outer.Person)o), true, false);
        }

        void Write120_Person(string n, string ns, global::Outer.Person o, bool isNullable, bool needType) {
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

        void Write117_TypeNameClash(string n, string ns, global::SerializationTypes.TypeNameClashA.TypeNameClash o, bool isNullable, bool needType) {
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

        void Write118_TypeNameClash(string n, string ns, global::SerializationTypes.TypeNameClashB.TypeNameClash o, bool isNullable, bool needType) {
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

        void Write119_Item(string n, string ns, global::SerializationTypes.NamespaceTypeNameClashContainer o, bool isNullable, bool needType) {
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
                        Write117_TypeNameClash(@"A", @"", ((global::SerializationTypes.TypeNameClashA.TypeNameClash)a[ia]), false, false);
                    }
                }
            }
            {
                global::SerializationTypes.TypeNameClashB.TypeNameClash[] a = (global::SerializationTypes.TypeNameClashB.TypeNameClash[])o.@B;
                if (a != null) {
                    for (int ia = 0; ia < a.Length; ia++) {
                        Write118_TypeNameClash(@"B", @"", ((global::SerializationTypes.TypeNameClashB.TypeNameClash)a[ia]), false, false);
                    }
                }
            }
            WriteEndElement(o);
        }

        void Write116_Item(string n, string ns, global::SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName o, bool isNullable, bool needType) {
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
                        Write120_Person(n, ns,(global::Outer.Person)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.NamespaceTypeNameClashContainer)) {
                        Write119_Item(n, ns,(global::SerializationTypes.NamespaceTypeNameClashContainer)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeNameClashB.TypeNameClash)) {
                        Write118_TypeNameClash(n, ns,(global::SerializationTypes.TypeNameClashB.TypeNameClash)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeNameClashA.TypeNameClash)) {
                        Write117_TypeNameClash(n, ns,(global::SerializationTypes.TypeNameClashA.TypeNameClash)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName)) {
                        Write116_Item(n, ns,(global::SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithFieldsOrdered)) {
                        Write115_TypeWithFieldsOrdered(n, ns,(global::SerializationTypes.TypeWithFieldsOrdered)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithPropertyHavingComplexChoice)) {
                        Write114_Item(n, ns,(global::SerializationTypes.TypeWithPropertyHavingComplexChoice)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.ComplexChoiceA)) {
                        Write113_ComplexChoiceA(n, ns,(global::SerializationTypes.ComplexChoiceA)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.ComplexChoiceB)) {
                        Write112_ComplexChoiceB(n, ns,(global::SerializationTypes.ComplexChoiceB)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithArrayPropertyHavingChoice)) {
                        Write111_Item(n, ns,(global::SerializationTypes.TypeWithArrayPropertyHavingChoice)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithTypesHavingCustomFormatter)) {
                        Write109_Item(n, ns,(global::SerializationTypes.TypeWithTypesHavingCustomFormatter)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.KnownTypesThroughConstructorWithValue)) {
                        Write108_Item(n, ns,(global::SerializationTypes.KnownTypesThroughConstructorWithValue)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.KnownTypesThroughConstructorWithArrayProperties)) {
                        Write107_Item(n, ns,(global::SerializationTypes.KnownTypesThroughConstructorWithArrayProperties)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithShouldSerializeMethod)) {
                        Write106_TypeWithShouldSerializeMethod(n, ns,(global::SerializationTypes.TypeWithShouldSerializeMethod)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue)) {
                        Write105_Item(n, ns,(global::SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithEnumPropertyHavingDefaultValue)) {
                        Write104_Item(n, ns,(global::SerializationTypes.TypeWithEnumPropertyHavingDefaultValue)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithPropertiesHavingDefaultValue)) {
                        Write103_Item(n, ns,(global::SerializationTypes.TypeWithPropertiesHavingDefaultValue)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWith2DArrayProperty2)) {
                        Write102_TypeWith2DArrayProperty2(n, ns,(global::SerializationTypes.TypeWith2DArrayProperty2)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithXmlQualifiedName)) {
                        Write101_TypeWithXmlQualifiedName(n, ns,(global::SerializationTypes.TypeWithXmlQualifiedName)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.ServerSettings)) {
                        Write100_ServerSettings(n, ns,(global::SerializationTypes.ServerSettings)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithNonPublicDefaultConstructor)) {
                        Write99_Item(n, ns,(global::SerializationTypes.TypeWithNonPublicDefaultConstructor)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.CustomDocument)) {
                        Write98_CustomDocument(n, ns,(global::SerializationTypes.CustomDocument)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.CustomElement)) {
                        Write97_CustomElement(n, ns,(global::SerializationTypes.CustomElement)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithTypeNameInXmlTypeAttribute)) {
                        Write95_Item(n, ns,(global::SerializationTypes.TypeWithTypeNameInXmlTypeAttribute)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithXmlSchemaFormAttribute)) {
                        Write94_TypeWithXmlSchemaFormAttribute(n, ns,(global::SerializationTypes.TypeWithXmlSchemaFormAttribute)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithPropertyNameSpecified)) {
                        Write93_TypeWithPropertyNameSpecified(n, ns,(global::SerializationTypes.TypeWithPropertyNameSpecified)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.SimpleKnownTypeValue)) {
                        Write92_SimpleKnownTypeValue(n, ns,(global::SerializationTypes.SimpleKnownTypeValue)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.KnownTypesThroughConstructor)) {
                        Write91_KnownTypesThroughConstructor(n, ns,(global::SerializationTypes.KnownTypesThroughConstructor)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithAnyAttribute)) {
                        Write90_TypeWithAnyAttribute(n, ns,(global::SerializationTypes.TypeWithAnyAttribute)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.XmlSerializerAttributes)) {
                        Write89_XmlSerializerAttributes(n, ns,(global::SerializationTypes.XmlSerializerAttributes)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.WithNullables)) {
                        Write82_WithNullables(n, ns,(global::SerializationTypes.WithNullables)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.WithEnums)) {
                        Write81_WithEnums(n, ns,(global::SerializationTypes.WithEnums)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.WithStruct)) {
                        Write78_WithStruct(n, ns,(global::SerializationTypes.WithStruct)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.SomeStruct)) {
                        Write77_SomeStruct(n, ns,(global::SerializationTypes.SomeStruct)o, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.ClassImplementsInterface)) {
                        Write76_ClassImplementsInterface(n, ns,(global::SerializationTypes.ClassImplementsInterface)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithXmlTextAttributeOnArray)) {
                        Write74_Item(n, ns,(global::SerializationTypes.TypeWithXmlTextAttributeOnArray)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.SimpleDC)) {
                        Write73_SimpleDC(n, ns,(global::SerializationTypes.SimpleDC)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithByteArrayAsXmlText)) {
                        Write72_TypeWithByteArrayAsXmlText(n, ns,(global::SerializationTypes.TypeWithByteArrayAsXmlText)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithDateTimePropertyAsXmlTime)) {
                        Write71_Item(n, ns,(global::SerializationTypes.TypeWithDateTimePropertyAsXmlTime)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.BaseClassWithSamePropertyName)) {
                        Write68_BaseClassWithSamePropertyName(n, ns,(global::SerializationTypes.BaseClassWithSamePropertyName)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.DerivedClassWithSameProperty)) {
                        Write69_DerivedClassWithSameProperty(n, ns,(global::SerializationTypes.DerivedClassWithSameProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.DerivedClassWithSameProperty2)) {
                        Write70_DerivedClassWithSameProperty2(n, ns,(global::SerializationTypes.DerivedClassWithSameProperty2)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.@__TypeNameWithSpecialCharacters漢ñ)) {
                        Write67_Item(n, ns,(global::SerializationTypes.@__TypeNameWithSpecialCharacters漢ñ)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeHasArrayOfASerializedAsB)) {
                        Write66_TypeHasArrayOfASerializedAsB(n, ns,(global::SerializationTypes.TypeHasArrayOfASerializedAsB)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeB)) {
                        Write65_TypeB(n, ns,(global::SerializationTypes.TypeB)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeA)) {
                        Write64_TypeA(n, ns,(global::SerializationTypes.TypeA)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.BuiltInTypes)) {
                        Write63_BuiltInTypes(n, ns,(global::SerializationTypes.BuiltInTypes)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.DCClassWithEnumAndStruct)) {
                        Write62_DCClassWithEnumAndStruct(n, ns,(global::SerializationTypes.DCClassWithEnumAndStruct)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.DCStruct)) {
                        Write61_DCStruct(n, ns,(global::SerializationTypes.DCStruct)o, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithEnumMembers)) {
                        Write60_TypeWithEnumMembers(n, ns,(global::SerializationTypes.TypeWithEnumMembers)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithReadOnlyMyCollectionProperty)) {
                        Write58_Item(n, ns,(global::SerializationTypes.TypeWithReadOnlyMyCollectionProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithMyCollectionField)) {
                        Write57_TypeWithMyCollectionField(n, ns,(global::SerializationTypes.TypeWithMyCollectionField)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.StructNotSerializable)) {
                        Write56_StructNotSerializable(n, ns,(global::SerializationTypes.StructNotSerializable)o, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithArraylikeMembers)) {
                        Write55_TypeWithArraylikeMembers(n, ns,(global::SerializationTypes.TypeWithArraylikeMembers)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithGetOnlyArrayProperties)) {
                        Write54_TypeWithGetOnlyArrayProperties(n, ns,(global::SerializationTypes.TypeWithGetOnlyArrayProperties)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithGetSetArrayMembers)) {
                        Write53_TypeWithGetSetArrayMembers(n, ns,(global::SerializationTypes.TypeWithGetSetArrayMembers)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.SimpleType)) {
                        Write52_SimpleType(n, ns,(global::SerializationTypes.SimpleType)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.TypeWithDateTimeStringProperty)) {
                        Write51_TypeWithDateTimeStringProperty(n, ns,(global::SerializationTypes.TypeWithDateTimeStringProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::XElementArrayWrapper)) {
                        Write50_XElementArrayWrapper(n, ns,(global::XElementArrayWrapper)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::XElementStruct)) {
                        Write49_XElementStruct(n, ns,(global::XElementStruct)o, true);
                        return;
                    }
                    if (t == typeof(global::XElementWrapper)) {
                        Write48_XElementWrapper(n, ns,(global::XElementWrapper)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::RootClass)) {
                        Write47_RootClass(n, ns,(global::RootClass)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Parameter)) {
                        Write46_Parameter(n, ns,(global::Parameter)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Parameter<global::System.String>)) {
                        Write45_ParameterOfString(n, ns,(global::Parameter<global::System.String>)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::MsgDocumentType)) {
                        Write44_MsgDocumentType(n, ns,(global::MsgDocumentType)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::TypeWithLinkedProperty)) {
                        Write43_TypeWithLinkedProperty(n, ns,(global::TypeWithLinkedProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::TypeWithMismatchBetweenAttributeAndPropertyType)) {
                        Write42_Item(n, ns,(global::TypeWithMismatchBetweenAttributeAndPropertyType)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::DefaultValuesSetToNegativeInfinity)) {
                        Write41_Item(n, ns,(global::DefaultValuesSetToNegativeInfinity)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::DefaultValuesSetToPositiveInfinity)) {
                        Write40_Item(n, ns,(global::DefaultValuesSetToPositiveInfinity)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::DefaultValuesSetToNaN)) {
                        Write39_DefaultValuesSetToNaN(n, ns,(global::DefaultValuesSetToNaN)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Pet)) {
                        Write38_Pet(n, ns,(global::Pet)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Orchestra)) {
                        Write35_Orchestra(n, ns,(global::Orchestra)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Instrument)) {
                        Write34_Instrument(n, ns,(global::Instrument)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Brass)) {
                        Write36_Brass(n, ns,(global::Brass)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Trumpet)) {
                        Write37_Trumpet(n, ns,(global::Trumpet)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::BaseClass1)) {
                        Write32_BaseClass1(n, ns,(global::BaseClass1)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::DerivedClass1)) {
                        Write33_DerivedClass1(n, ns,(global::DerivedClass1)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::AliasedTestType)) {
                        Write31_AliasedTestType(n, ns,(global::AliasedTestType)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::OrderedItem)) {
                        Write30_OrderedItem(n, ns,(global::OrderedItem)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Address)) {
                        Write29_Address(n, ns,(global::Address)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::PurchaseOrder)) {
                        Write28_PurchaseOrder(n, ns,(global::PurchaseOrder)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::OrderedItem)) {
                        Write27_OrderedItem(n, ns,(global::OrderedItem)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Address)) {
                        Write26_Address(n, ns,(global::Address)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SimpleBaseClass)) {
                        Write25_SimpleBaseClass(n, ns,(global::SimpleBaseClass)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SimpleDerivedClass)) {
                        Write24_SimpleDerivedClass(n, ns,(global::SimpleDerivedClass)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::BaseClass)) {
                        Write23_BaseClass(n, ns,(global::BaseClass)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::DerivedClass)) {
                        Write22_DerivedClass(n, ns,(global::DerivedClass)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Employee)) {
                        Write21_Employee(n, ns,(global::Employee)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Group)) {
                        Write20_Group(n, ns,(global::Group)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Vehicle)) {
                        Write19_Vehicle(n, ns,(global::Vehicle)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Animal)) {
                        Write16_Animal(n, ns,(global::Animal)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Dog)) {
                        Write18_Dog(n, ns,(global::Dog)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::TypeWithXmlNodeArrayProperty)) {
                        Write15_TypeWithXmlNodeArrayProperty(n, ns,(global::TypeWithXmlNodeArrayProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::TypeWithByteProperty)) {
                        Write14_TypeWithByteProperty(n, ns,(global::TypeWithByteProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::TypeWithDefaultTimeSpanProperty)) {
                        Write13_Item(n, ns,(global::TypeWithDefaultTimeSpanProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::TypeWithTimeSpanProperty)) {
                        Write12_TypeWithTimeSpanProperty(n, ns,(global::TypeWithTimeSpanProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::DateTimeTimeWrapper)) {
                        Write11_DateTimeTimeWrapper(n, ns,(global::DateTimeTimeWrapper)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::DateTimeDateWrapper)) {
                        Write10_DateTimeDateWrapper(n, ns,(global::DateTimeDateWrapper)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::TimeOnlyAsXsdTimeWrapper)) {
                        Write9_TimeOnlyAsXsdTimeWrapper(n, ns,(global::TimeOnlyAsXsdTimeWrapper)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::TimeOnlyWrapper)) {
                        Write8_TimeOnlyWrapper(n, ns,(global::TimeOnlyWrapper)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::DateOnlyWrapper)) {
                        Write7_DateOnlyWrapper(n, ns,(global::DateOnlyWrapper)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::TypeWithDateAndTimeOnlyProperties)) {
                        Write6_Item(n, ns,(global::TypeWithDateAndTimeOnlyProperties)o, isNullable, true);
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
                        Writer.WriteString(Write17_DogBreed((global::DogBreed)o));
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
                                    Write27_OrderedItem(@"OrderedItem", @"http://www.contoso1.com", ((global::OrderedItem)a[ia]), true, false);
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
                                    Write34_Instrument(@"Instrument", @"", ((global::Instrument)a[ia]), true, false);
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
                                    Write43_TypeWithLinkedProperty(@"TypeWithLinkedProperty", @"", ((global::TypeWithLinkedProperty)a[ia]), true, false);
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
                                    Write46_Parameter(@"Parameter", @"", ((global::Parameter)a[ia]), true, false);
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
                                    Write52_SimpleType(@"SimpleType", @"", ((global::SerializationTypes.SimpleType)a[ia]), true, false);
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
                        Writer.WriteString(Write59_MyEnum((global::SerializationTypes.MyEnum)o));
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
                                    Write64_TypeA(@"TypeA", @"", ((global::SerializationTypes.TypeA)a[ia]), true, false);
                                }
                            }
                        }
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.EnumFlags)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"EnumFlags", @"");
                        Writer.WriteString(Write75_EnumFlags((global::SerializationTypes.EnumFlags)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.IntEnum)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"IntEnum", @"");
                        Writer.WriteString(Write79_IntEnum((global::SerializationTypes.IntEnum)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.ShortEnum)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ShortEnum", @"");
                        Writer.WriteString(Write80_ShortEnum((global::SerializationTypes.ShortEnum)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.ByteEnum)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ByteEnum", @"");
                        Writer.WriteString(Write83_ByteEnum((global::SerializationTypes.ByteEnum)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.SByteEnum)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"SByteEnum", @"");
                        Writer.WriteString(Write84_SByteEnum((global::SerializationTypes.SByteEnum)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.UIntEnum)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"UIntEnum", @"");
                        Writer.WriteString(Write85_UIntEnum((global::SerializationTypes.UIntEnum)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.LongEnum)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"LongEnum", @"");
                        Writer.WriteString(Write86_LongEnum((global::SerializationTypes.LongEnum)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.ULongEnum)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ULongEnum", @"");
                        Writer.WriteString(Write87_ULongEnum((global::SerializationTypes.ULongEnum)o));
                        Writer.WriteEndElement();
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.ItemChoiceType)) {
                        Writer.WriteStartElement(n, ns);
                        WriteXsiType(@"ItemChoiceType", @"");
                        Writer.WriteString(Write88_ItemChoiceType((global::SerializationTypes.ItemChoiceType)o));
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
                                    WriteElementString(@"ItemChoiceType", @"", Write88_ItemChoiceType(((global::SerializationTypes.ItemChoiceType)a[ia])));
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
                                    Write52_SimpleType(@"SimpleType", @"", ((global::SerializationTypes.SimpleType)a[ia]), true, false);
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
                        Writer.WriteString(Write110_MoreChoices((global::SerializationTypes.MoreChoices)o));
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

        string Write110_MoreChoices(global::SerializationTypes.MoreChoices v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.MoreChoices.@None: s = @"None"; break;
                case global::SerializationTypes.MoreChoices.@Item: s = @"Item"; break;
                case global::SerializationTypes.MoreChoices.@Amount: s = @"Amount"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.MoreChoices");
            }
            return s;
        }

        void Write52_SimpleType(string n, string ns, global::SerializationTypes.SimpleType o, bool isNullable, bool needType) {
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

        string Write88_ItemChoiceType(global::SerializationTypes.ItemChoiceType v) {
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

        string Write87_ULongEnum(global::SerializationTypes.ULongEnum v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.ULongEnum.@Option0: s = @"Option0"; break;
                case global::SerializationTypes.ULongEnum.@Option1: s = @"Option1"; break;
                case global::SerializationTypes.ULongEnum.@Option2: s = @"Option2"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.ULongEnum");
            }
            return s;
        }

        string Write86_LongEnum(global::SerializationTypes.LongEnum v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.LongEnum.@Option0: s = @"Option0"; break;
                case global::SerializationTypes.LongEnum.@Option1: s = @"Option1"; break;
                case global::SerializationTypes.LongEnum.@Option2: s = @"Option2"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.LongEnum");
            }
            return s;
        }

        string Write85_UIntEnum(global::SerializationTypes.UIntEnum v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.UIntEnum.@Option0: s = @"Option0"; break;
                case global::SerializationTypes.UIntEnum.@Option1: s = @"Option1"; break;
                case global::SerializationTypes.UIntEnum.@Option2: s = @"Option2"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.UIntEnum");
            }
            return s;
        }

        string Write84_SByteEnum(global::SerializationTypes.SByteEnum v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.SByteEnum.@Option0: s = @"Option0"; break;
                case global::SerializationTypes.SByteEnum.@Option1: s = @"Option1"; break;
                case global::SerializationTypes.SByteEnum.@Option2: s = @"Option2"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.SByteEnum");
            }
            return s;
        }

        string Write83_ByteEnum(global::SerializationTypes.ByteEnum v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.ByteEnum.@Option0: s = @"Option0"; break;
                case global::SerializationTypes.ByteEnum.@Option1: s = @"Option1"; break;
                case global::SerializationTypes.ByteEnum.@Option2: s = @"Option2"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.ByteEnum");
            }
            return s;
        }

        string Write80_ShortEnum(global::SerializationTypes.ShortEnum v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.ShortEnum.@Option0: s = @"Option0"; break;
                case global::SerializationTypes.ShortEnum.@Option1: s = @"Option1"; break;
                case global::SerializationTypes.ShortEnum.@Option2: s = @"Option2"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.ShortEnum");
            }
            return s;
        }

        string Write79_IntEnum(global::SerializationTypes.IntEnum v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.IntEnum.@Option0: s = @"Option0"; break;
                case global::SerializationTypes.IntEnum.@Option1: s = @"Option1"; break;
                case global::SerializationTypes.IntEnum.@Option2: s = @"Option2"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.IntEnum");
            }
            return s;
        }

        string Write75_EnumFlags(global::SerializationTypes.EnumFlags v) {
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

        void Write64_TypeA(string n, string ns, global::SerializationTypes.TypeA o, bool isNullable, bool needType) {
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

        string Write59_MyEnum(global::SerializationTypes.MyEnum v) {
            string s = null;
            switch (v) {
                case global::SerializationTypes.MyEnum.@One: s = @"One"; break;
                case global::SerializationTypes.MyEnum.@Two: s = @"Two"; break;
                case global::SerializationTypes.MyEnum.@Three: s = @"Three"; break;
                default: throw CreateInvalidEnumValueException(((System.Int64)v).ToString(System.Globalization.CultureInfo.InvariantCulture), @"SerializationTypes.MyEnum");
            }
            return s;
        }

        void Write46_Parameter(string n, string ns, global::Parameter o, bool isNullable, bool needType) {
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
                        Write45_ParameterOfString(n, ns,(global::Parameter<global::System.String>)o, isNullable, true);
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

        void Write45_ParameterOfString(string n, string ns, global::Parameter<global::System.String> o, bool isNullable, bool needType) {
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

        void Write43_TypeWithLinkedProperty(string n, string ns, global::TypeWithLinkedProperty o, bool isNullable, bool needType) {
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
            Write43_TypeWithLinkedProperty(@"Child", @"", ((global::TypeWithLinkedProperty)o.@Child), false, false);
            {
                global::System.Collections.Generic.List<global::TypeWithLinkedProperty> a = (global::System.Collections.Generic.List<global::TypeWithLinkedProperty>)((global::System.Collections.Generic.List<global::TypeWithLinkedProperty>)o.@Children);
                if (a != null){
                    WriteStartElement(@"Children", @"", null, false);
                    for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                        Write43_TypeWithLinkedProperty(@"TypeWithLinkedProperty", @"", ((global::TypeWithLinkedProperty)a[ia]), true, false);
                    }
                    WriteEndElement();
                }
            }
            WriteEndElement(o);
        }

        void Write34_Instrument(string n, string ns, global::Instrument o, bool isNullable, bool needType) {
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
                        Write36_Brass(n, ns,(global::Brass)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::Trumpet)) {
                        Write37_Trumpet(n, ns,(global::Trumpet)o, isNullable, true);
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

        void Write37_Trumpet(string n, string ns, global::Trumpet o, bool isNullable, bool needType) {
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

        void Write36_Brass(string n, string ns, global::Brass o, bool isNullable, bool needType) {
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
                        Write37_Trumpet(n, ns,(global::Trumpet)o, isNullable, true);
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

        void Write27_OrderedItem(string n, string ns, global::OrderedItem o, bool isNullable, bool needType) {
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

        string Write17_DogBreed(global::DogBreed v) {
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

        void Write6_Item(string n, string ns, global::TypeWithDateAndTimeOnlyProperties o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::TypeWithDateAndTimeOnlyProperties)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithDateAndTimeOnlyProperties", @"");
            WriteElementStringRaw(@"Today", @"", FromDateOnly(((global::System.DateOnly)o.@Today)));
            WriteElementStringRaw(@"MyDate", @"", FromDateOnly(((global::System.DateOnly)o.@CustomDate)));
            if (((global::System.DateOnly)o.@DefaultDate) !=  new System.DateOnly(1969, 7, 20)) {
                WriteElementStringRaw(@"DefaultDate", @"", FromDateOnly(((global::System.DateOnly)o.@DefaultDate)));
            }
            if (o.@NullableDate != null) {
                WriteNullableStringLiteralRaw(@"NullableDate", @"", FromDateOnly(((global::System.DateOnly)o.@NullableDate)));
            }
            else {
                WriteNullTagLiteral(@"NullableDate", @"");
            }
            if (o.@NullableDateWithValue != null) {
                WriteNullableStringLiteralRaw(@"NullableDateWithValue", @"", FromDateOnly(((global::System.DateOnly)o.@NullableDateWithValue)));
            }
            else {
                WriteNullTagLiteral(@"NullableDateWithValue", @"");
            }
            if (o.@NullableDefaultDate != null) {
                WriteNullableStringLiteralRaw(@"NullableDefaultDate", @"", FromDateOnly(((global::System.DateOnly)o.@NullableDefaultDate)));
            }
            else {
                WriteNullTagLiteral(@"NullableDefaultDate", @"");
            }
            WriteElementStringRaw(@"Now", @"", FromTimeOnly(((global::System.TimeOnly)o.@Now)));
            WriteElementStringRaw(@"MyTime", @"", FromTimeOnly(((global::System.TimeOnly)o.@CustomTime)));
            if (((global::System.TimeOnly)o.@DefaultTime) !=  new System.TimeOnly(730600000000)) {
                WriteElementStringRaw(@"DefaultTime", @"", FromTimeOnly(((global::System.TimeOnly)o.@DefaultTime)));
            }
            if (o.@NullableTime != null) {
                WriteNullableStringLiteralRaw(@"NullableTime", @"", FromTimeOnly(((global::System.TimeOnly)o.@NullableTime)));
            }
            else {
                WriteNullTagLiteral(@"NullableTime", @"");
            }
            if (o.@NullableTimeWithValue != null) {
                WriteNullableStringLiteralRaw(@"NullableTimeWithValue", @"", FromTimeOnly(((global::System.TimeOnly)o.@NullableTimeWithValue)));
            }
            else {
                WriteNullTagLiteral(@"NullableTimeWithValue", @"");
            }
            if (o.@NullableDefaultTime != null) {
                WriteNullableStringLiteralRaw(@"NullableDefaultTime", @"", FromTimeOnly(((global::System.TimeOnly)o.@NullableDefaultTime)));
            }
            else {
                WriteNullTagLiteral(@"NullableDefaultTime", @"");
            }
            WriteEndElement(o);
        }

        void Write7_DateOnlyWrapper(string n, string ns, global::DateOnlyWrapper o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::DateOnlyWrapper)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"DateOnlyWrapper", @"");
            WriteElementStringRaw(@"TestValue", @"", FromDateOnly(((global::System.DateOnly)o.@TestValue)));
            WriteEndElement(o);
        }

        void Write8_TimeOnlyWrapper(string n, string ns, global::TimeOnlyWrapper o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::TimeOnlyWrapper)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TimeOnlyWrapper", @"");
            WriteElementStringRaw(@"TestValue", @"", FromTimeOnly(((global::System.TimeOnly)o.@TestValue)));
            WriteEndElement(o);
        }

        void Write9_TimeOnlyAsXsdTimeWrapper(string n, string ns, global::TimeOnlyAsXsdTimeWrapper o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::TimeOnlyAsXsdTimeWrapper)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TimeOnlyAsXsdTimeWrapper", @"");
            WriteElementStringRaw(@"TestValue", @"", FromTimeOnlyIgnoreOffset(((global::System.TimeOnly)o.@TestValue)));
            WriteEndElement(o);
        }

        void Write10_DateTimeDateWrapper(string n, string ns, global::DateTimeDateWrapper o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::DateTimeDateWrapper)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"DateTimeDateWrapper", @"");
            WriteElementStringRaw(@"TestValue", @"", FromDate(((global::System.DateTime)o.@TestValue)));
            WriteEndElement(o);
        }

        void Write11_DateTimeTimeWrapper(string n, string ns, global::DateTimeTimeWrapper o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::DateTimeTimeWrapper)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"DateTimeTimeWrapper", @"");
            WriteElementStringRaw(@"TestValue", @"", FromTime(((global::System.DateTime)o.@TestValue)));
            WriteEndElement(o);
        }

        void Write12_TypeWithTimeSpanProperty(string n, string ns, global::TypeWithTimeSpanProperty o, bool isNullable, bool needType) {
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

        void Write13_Item(string n, string ns, global::TypeWithDefaultTimeSpanProperty o, bool isNullable, bool needType) {
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

        void Write14_TypeWithByteProperty(string n, string ns, global::TypeWithByteProperty o, bool isNullable, bool needType) {
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

        void Write15_TypeWithXmlNodeArrayProperty(string n, string ns, global::TypeWithXmlNodeArrayProperty o, bool isNullable, bool needType) {
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

        void Write18_Dog(string n, string ns, global::Dog o, bool isNullable, bool needType) {
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
            WriteElementString(@"Breed", @"", Write17_DogBreed(((global::DogBreed)o.@Breed)));
            WriteEndElement(o);
        }

        void Write16_Animal(string n, string ns, global::Animal o, bool isNullable, bool needType) {
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
                        Write18_Dog(n, ns,(global::Dog)o, isNullable, true);
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

        void Write19_Vehicle(string n, string ns, global::Vehicle o, bool isNullable, bool needType) {
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

        void Write20_Group(string n, string ns, global::Group o, bool isNullable, bool needType) {
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
            Write19_Vehicle(@"GroupVehicle", @"", ((global::Vehicle)o.@GroupVehicle), false, false);
            WriteEndElement(o);
        }

        void Write21_Employee(string n, string ns, global::Employee o, bool isNullable, bool needType) {
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

        void Write22_DerivedClass(string n, string ns, global::DerivedClass o, bool isNullable, bool needType) {
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

        void Write23_BaseClass(string n, string ns, global::BaseClass o, bool isNullable, bool needType) {
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
                        Write22_DerivedClass(n, ns,(global::DerivedClass)o, isNullable, true);
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

        void Write24_SimpleDerivedClass(string n, string ns, global::SimpleDerivedClass o, bool isNullable, bool needType) {
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

        void Write25_SimpleBaseClass(string n, string ns, global::SimpleBaseClass o, bool isNullable, bool needType) {
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
                        Write24_SimpleDerivedClass(n, ns,(global::SimpleDerivedClass)o, isNullable, true);
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

        void Write26_Address(string n, string ns, global::Address o, bool isNullable, bool needType) {
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

        void Write28_PurchaseOrder(string n, string ns, global::PurchaseOrder o, bool isNullable, bool needType) {
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
            Write26_Address(@"ShipTo", @"http://www.contoso1.com", ((global::Address)o.@ShipTo), false, false);
            WriteElementString(@"OrderDate", @"http://www.contoso1.com", ((global::System.String)o.@OrderDate));
            {
                global::OrderedItem[] a = (global::OrderedItem[])((global::OrderedItem[])o.@OrderedItems);
                if (a != null){
                    WriteStartElement(@"Items", @"http://www.contoso1.com", null, false);
                    for (int ia = 0; ia < a.Length; ia++) {
                        Write27_OrderedItem(@"OrderedItem", @"http://www.contoso1.com", ((global::OrderedItem)a[ia]), true, false);
                    }
                    WriteEndElement();
                }
            }
            WriteElementStringRaw(@"SubTotal", @"http://www.contoso1.com", System.Xml.XmlConvert.ToString((global::System.Decimal)((global::System.Decimal)o.@SubTotal)));
            WriteElementStringRaw(@"ShipCost", @"http://www.contoso1.com", System.Xml.XmlConvert.ToString((global::System.Decimal)((global::System.Decimal)o.@ShipCost)));
            WriteElementStringRaw(@"TotalCost", @"http://www.contoso1.com", System.Xml.XmlConvert.ToString((global::System.Decimal)((global::System.Decimal)o.@TotalCost)));
            WriteEndElement(o);
        }

        void Write29_Address(string n, string ns, global::Address o, bool isNullable, bool needType) {
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

        void Write30_OrderedItem(string n, string ns, global::OrderedItem o, bool isNullable, bool needType) {
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

        void Write31_AliasedTestType(string n, string ns, global::AliasedTestType o, bool isNullable, bool needType) {
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

        void Write33_DerivedClass1(string n, string ns, global::DerivedClass1 o, bool isNullable, bool needType) {
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

        void Write32_BaseClass1(string n, string ns, global::BaseClass1 o, bool isNullable, bool needType) {
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
                        Write33_DerivedClass1(n, ns,(global::DerivedClass1)o, isNullable, true);
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

        void Write35_Orchestra(string n, string ns, global::Orchestra o, bool isNullable, bool needType) {
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
                        Write34_Instrument(@"Instrument", @"", ((global::Instrument)a[ia]), true, false);
                    }
                    WriteEndElement();
                }
            }
            WriteEndElement(o);
        }

        void Write38_Pet(string n, string ns, global::Pet o, bool isNullable, bool needType) {
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

        void Write39_DefaultValuesSetToNaN(string n, string ns, global::DefaultValuesSetToNaN o, bool isNullable, bool needType) {
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

        void Write40_Item(string n, string ns, global::DefaultValuesSetToPositiveInfinity o, bool isNullable, bool needType) {
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

        void Write41_Item(string n, string ns, global::DefaultValuesSetToNegativeInfinity o, bool isNullable, bool needType) {
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

        void Write42_Item(string n, string ns, global::TypeWithMismatchBetweenAttributeAndPropertyType o, bool isNullable, bool needType) {
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

        void Write44_MsgDocumentType(string n, string ns, global::MsgDocumentType o, bool isNullable, bool needType) {
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

        void Write47_RootClass(string n, string ns, global::RootClass o, bool isNullable, bool needType) {
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
                        Write46_Parameter(@"Parameter", @"", ((global::Parameter)a[ia]), true, false);
                    }
                    WriteEndElement();
                }
            }
            WriteEndElement(o);
        }

        void Write48_XElementWrapper(string n, string ns, global::XElementWrapper o, bool isNullable, bool needType) {
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

        void Write49_XElementStruct(string n, string ns, global::XElementStruct o, bool needType) {
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

        void Write50_XElementArrayWrapper(string n, string ns, global::XElementArrayWrapper o, bool isNullable, bool needType) {
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

        void Write51_TypeWithDateTimeStringProperty(string n, string ns, global::SerializationTypes.TypeWithDateTimeStringProperty o, bool isNullable, bool needType) {
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

        void Write53_TypeWithGetSetArrayMembers(string n, string ns, global::SerializationTypes.TypeWithGetSetArrayMembers o, bool isNullable, bool needType) {
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
                        Write52_SimpleType(@"SimpleType", @"", ((global::SerializationTypes.SimpleType)a[ia]), true, false);
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
                        Write52_SimpleType(@"SimpleType", @"", ((global::SerializationTypes.SimpleType)a[ia]), true, false);
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

        void Write54_TypeWithGetOnlyArrayProperties(string n, string ns, global::SerializationTypes.TypeWithGetOnlyArrayProperties o, bool isNullable, bool needType) {
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

        void Write55_TypeWithArraylikeMembers(string n, string ns, global::SerializationTypes.TypeWithArraylikeMembers o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithArraylikeMembers)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithArraylikeMembers", @"");
            {
                global::System.Int32[] a = (global::System.Int32[])((global::System.Int32[])o.@IntAField);
                if (a != null){
                    WriteStartElement(@"IntAField", @"", null, false);
                    for (int ia = 0; ia < a.Length; ia++) {
                        WriteElementStringRaw(@"int", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)a[ia])));
                    }
                    WriteEndElement();
                }
            }
            {
                global::System.Int32[] a = (global::System.Int32[])((global::System.Int32[])o.@NIntAField);
                if (a != null){
                    WriteStartElement(@"NIntAField", @"", null, false);
                    for (int ia = 0; ia < a.Length; ia++) {
                        WriteElementStringRaw(@"int", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)a[ia])));
                    }
                    WriteEndElement();
                }
            }
            {
                global::System.Collections.Generic.List<global::System.Int32> a = (global::System.Collections.Generic.List<global::System.Int32>)((global::System.Collections.Generic.List<global::System.Int32>)o.@IntLField);
                if (a != null){
                    WriteStartElement(@"IntLField", @"", null, false);
                    for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                        WriteElementStringRaw(@"int", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)a[ia])));
                    }
                    WriteEndElement();
                }
            }
            {
                global::System.Collections.Generic.List<global::System.Int32> a = (global::System.Collections.Generic.List<global::System.Int32>)((global::System.Collections.Generic.List<global::System.Int32>)o.@NIntLField);
                if ((object)(a) == null) {
                    WriteNullTagLiteral(@"NIntLField", @"");
                }
                else {
                    WriteStartElement(@"NIntLField", @"", null, false);
                    for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                        WriteElementStringRaw(@"int", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)a[ia])));
                    }
                    WriteEndElement();
                }
            }
            {
                global::System.Int32[] a = (global::System.Int32[])((global::System.Int32[])o.@IntAProp);
                if (a != null){
                    WriteStartElement(@"IntAProp", @"", null, false);
                    for (int ia = 0; ia < a.Length; ia++) {
                        WriteElementStringRaw(@"int", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)a[ia])));
                    }
                    WriteEndElement();
                }
            }
            {
                global::System.Int32[] a = (global::System.Int32[])((global::System.Int32[])o.@NIntAProp);
                if ((object)(a) == null) {
                    WriteNullTagLiteral(@"NIntAProp", @"");
                }
                else {
                    WriteStartElement(@"NIntAProp", @"", null, false);
                    for (int ia = 0; ia < a.Length; ia++) {
                        WriteElementStringRaw(@"int", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)a[ia])));
                    }
                    WriteEndElement();
                }
            }
            {
                global::System.Collections.Generic.List<global::System.Int32> a = (global::System.Collections.Generic.List<global::System.Int32>)((global::System.Collections.Generic.List<global::System.Int32>)o.@IntLProp);
                if (a != null){
                    WriteStartElement(@"IntLProp", @"", null, false);
                    for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                        WriteElementStringRaw(@"int", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)a[ia])));
                    }
                    WriteEndElement();
                }
            }
            {
                global::System.Collections.Generic.List<global::System.Int32> a = (global::System.Collections.Generic.List<global::System.Int32>)((global::System.Collections.Generic.List<global::System.Int32>)o.@NIntLProp);
                if (a != null){
                    WriteStartElement(@"NIntLProp", @"", null, false);
                    for (int ia = 0; ia < ((System.Collections.ICollection)a).Count; ia++) {
                        WriteElementStringRaw(@"int", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)a[ia])));
                    }
                    WriteEndElement();
                }
            }
            WriteEndElement(o);
        }

        void Write56_StructNotSerializable(string n, string ns, global::SerializationTypes.StructNotSerializable o, bool needType) {
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

        void Write57_TypeWithMyCollectionField(string n, string ns, global::SerializationTypes.TypeWithMyCollectionField o, bool isNullable, bool needType) {
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

        void Write58_Item(string n, string ns, global::SerializationTypes.TypeWithReadOnlyMyCollectionProperty o, bool isNullable, bool needType) {
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

        void Write60_TypeWithEnumMembers(string n, string ns, global::SerializationTypes.TypeWithEnumMembers o, bool isNullable, bool needType) {
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
            WriteElementString(@"F1", @"", Write59_MyEnum(((global::SerializationTypes.MyEnum)o.@F1)));
            WriteElementString(@"P1", @"", Write59_MyEnum(((global::SerializationTypes.MyEnum)o.@P1)));
            WriteEndElement(o);
        }

        void Write61_DCStruct(string n, string ns, global::SerializationTypes.DCStruct o, bool needType) {
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

        void Write62_DCClassWithEnumAndStruct(string n, string ns, global::SerializationTypes.DCClassWithEnumAndStruct o, bool isNullable, bool needType) {
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
            Write61_DCStruct(@"MyStruct", @"", ((global::SerializationTypes.DCStruct)o.@MyStruct), false);
            WriteElementString(@"MyEnum1", @"", Write59_MyEnum(((global::SerializationTypes.MyEnum)o.@MyEnum1)));
            WriteEndElement(o);
        }

        void Write63_BuiltInTypes(string n, string ns, global::SerializationTypes.BuiltInTypes o, bool isNullable, bool needType) {
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

        void Write65_TypeB(string n, string ns, global::SerializationTypes.TypeB o, bool isNullable, bool needType) {
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

        void Write66_TypeHasArrayOfASerializedAsB(string n, string ns, global::SerializationTypes.TypeHasArrayOfASerializedAsB o, bool isNullable, bool needType) {
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
                        Write64_TypeA(@"TypeA", @"", ((global::SerializationTypes.TypeA)a[ia]), true, false);
                    }
                    WriteEndElement();
                }
            }
            WriteEndElement(o);
        }

        void Write67_Item(string n, string ns, global::SerializationTypes.@__TypeNameWithSpecialCharacters漢ñ o, bool isNullable, bool needType) {
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

        void Write70_DerivedClassWithSameProperty2(string n, string ns, global::SerializationTypes.DerivedClassWithSameProperty2 o, bool isNullable, bool needType) {
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

        void Write69_DerivedClassWithSameProperty(string n, string ns, global::SerializationTypes.DerivedClassWithSameProperty o, bool isNullable, bool needType) {
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
                        Write70_DerivedClassWithSameProperty2(n, ns,(global::SerializationTypes.DerivedClassWithSameProperty2)o, isNullable, true);
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

        void Write68_BaseClassWithSamePropertyName(string n, string ns, global::SerializationTypes.BaseClassWithSamePropertyName o, bool isNullable, bool needType) {
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
                        Write69_DerivedClassWithSameProperty(n, ns,(global::SerializationTypes.DerivedClassWithSameProperty)o, isNullable, true);
                        return;
                    }
                    if (t == typeof(global::SerializationTypes.DerivedClassWithSameProperty2)) {
                        Write70_DerivedClassWithSameProperty2(n, ns,(global::SerializationTypes.DerivedClassWithSameProperty2)o, isNullable, true);
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

        void Write71_Item(string n, string ns, global::SerializationTypes.TypeWithDateTimePropertyAsXmlTime o, bool isNullable, bool needType) {
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

        void Write72_TypeWithByteArrayAsXmlText(string n, string ns, global::SerializationTypes.TypeWithByteArrayAsXmlText o, bool isNullable, bool needType) {
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

        void Write73_SimpleDC(string n, string ns, global::SerializationTypes.SimpleDC o, bool isNullable, bool needType) {
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

        void Write74_Item(string n, string ns, global::SerializationTypes.TypeWithXmlTextAttributeOnArray o, bool isNullable, bool needType) {
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

        void Write76_ClassImplementsInterface(string n, string ns, global::SerializationTypes.ClassImplementsInterface o, bool isNullable, bool needType) {
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

        void Write77_SomeStruct(string n, string ns, global::SerializationTypes.SomeStruct o, bool needType) {
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

        void Write78_WithStruct(string n, string ns, global::SerializationTypes.WithStruct o, bool isNullable, bool needType) {
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
            Write77_SomeStruct(@"Some", @"", ((global::SerializationTypes.SomeStruct)o.@Some), false);
            WriteEndElement(o);
        }

        void Write81_WithEnums(string n, string ns, global::SerializationTypes.WithEnums o, bool isNullable, bool needType) {
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
            WriteElementString(@"Int", @"", Write79_IntEnum(((global::SerializationTypes.IntEnum)o.@Int)));
            WriteElementString(@"Short", @"", Write80_ShortEnum(((global::SerializationTypes.ShortEnum)o.@Short)));
            WriteEndElement(o);
        }

        void Write82_WithNullables(string n, string ns, global::SerializationTypes.WithNullables o, bool isNullable, bool needType) {
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
                WriteElementString(@"Optional", @"", Write79_IntEnum(((global::SerializationTypes.IntEnum)o.@Optional)));
            }
            else {
                WriteNullTagLiteral(@"Optional", @"");
            }
            if (o.@Optionull != null) {
                WriteElementString(@"Optionull", @"", Write79_IntEnum(((global::SerializationTypes.IntEnum)o.@Optionull)));
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
                Write77_SomeStruct(@"Struct1", @"", ((global::SerializationTypes.SomeStruct)o.@Struct1), false);
            }
            else {
                WriteNullTagLiteral(@"Struct1", @"");
            }
            if (o.@Struct2 != null) {
                Write77_SomeStruct(@"Struct2", @"", ((global::SerializationTypes.SomeStruct)o.@Struct2), false);
            }
            else {
                WriteNullTagLiteral(@"Struct2", @"");
            }
            WriteEndElement(o);
        }

        void Write89_XmlSerializerAttributes(string n, string ns, global::SerializationTypes.XmlSerializerAttributes o, bool isNullable, bool needType) {
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
                        WriteElementString(@"ItemChoiceType", @"", Write88_ItemChoiceType(((global::SerializationTypes.ItemChoiceType)a[ia])));
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

        void Write90_TypeWithAnyAttribute(string n, string ns, global::SerializationTypes.TypeWithAnyAttribute o, bool isNullable, bool needType) {
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

        void Write91_KnownTypesThroughConstructor(string n, string ns, global::SerializationTypes.KnownTypesThroughConstructor o, bool isNullable, bool needType) {
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

        void Write92_SimpleKnownTypeValue(string n, string ns, global::SerializationTypes.SimpleKnownTypeValue o, bool isNullable, bool needType) {
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

        void Write93_TypeWithPropertyNameSpecified(string n, string ns, global::SerializationTypes.TypeWithPropertyNameSpecified o, bool isNullable, bool needType) {
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

        void Write94_TypeWithXmlSchemaFormAttribute(string n, string ns, global::SerializationTypes.TypeWithXmlSchemaFormAttribute o, bool isNullable, bool needType) {
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

        void Write95_Item(string n, string ns, global::SerializationTypes.TypeWithTypeNameInXmlTypeAttribute o, bool isNullable, bool needType) {
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

        void Write97_CustomElement(string n, string ns, global::SerializationTypes.CustomElement o, bool isNullable, bool needType) {
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

        void Write98_CustomDocument(string n, string ns, global::SerializationTypes.CustomDocument o, bool isNullable, bool needType) {
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
                        Write97_CustomElement(@"customElement", @"", ((global::SerializationTypes.CustomElement)a[ia]), false, false);
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

        void Write99_Item(string n, string ns, global::SerializationTypes.TypeWithNonPublicDefaultConstructor o, bool isNullable, bool needType) {
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

        void Write100_ServerSettings(string n, string ns, global::SerializationTypes.ServerSettings o, bool isNullable, bool needType) {
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

        void Write101_TypeWithXmlQualifiedName(string n, string ns, global::SerializationTypes.TypeWithXmlQualifiedName o, bool isNullable, bool needType) {
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

        void Write102_TypeWith2DArrayProperty2(string n, string ns, global::SerializationTypes.TypeWith2DArrayProperty2 o, bool isNullable, bool needType) {
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
                                    Write52_SimpleType(@"SimpleType", @"", ((global::SerializationTypes.SimpleType)aa[iaa]), true, false);
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

        void Write103_Item(string n, string ns, global::SerializationTypes.TypeWithPropertiesHavingDefaultValue o, bool isNullable, bool needType) {
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

        void Write104_Item(string n, string ns, global::SerializationTypes.TypeWithEnumPropertyHavingDefaultValue o, bool isNullable, bool needType) {
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
                WriteElementString(@"EnumProperty", @"", Write79_IntEnum(((global::SerializationTypes.IntEnum)o.@EnumProperty)));
            }
            WriteEndElement(o);
        }

        void Write105_Item(string n, string ns, global::SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue o, bool isNullable, bool needType) {
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
                WriteElementString(@"EnumProperty", @"", Write75_EnumFlags(((global::SerializationTypes.EnumFlags)o.@EnumProperty)));
            }
            WriteEndElement(o);
        }

        void Write106_TypeWithShouldSerializeMethod(string n, string ns, global::SerializationTypes.TypeWithShouldSerializeMethod o, bool isNullable, bool needType) {
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

        void Write107_Item(string n, string ns, global::SerializationTypes.KnownTypesThroughConstructorWithArrayProperties o, bool isNullable, bool needType) {
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

        void Write108_Item(string n, string ns, global::SerializationTypes.KnownTypesThroughConstructorWithValue o, bool isNullable, bool needType) {
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

        void Write109_Item(string n, string ns, global::SerializationTypes.TypeWithTypesHavingCustomFormatter o, bool isNullable, bool needType) {
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

        void Write111_Item(string n, string ns, global::SerializationTypes.TypeWithArrayPropertyHavingChoice o, bool isNullable, bool needType) {
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

        void Write112_ComplexChoiceB(string n, string ns, global::SerializationTypes.ComplexChoiceB o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.ComplexChoiceB)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"ComplexChoiceB", @"");
            WriteElementString(@"Name", @"", ((global::System.String)o.@Name));
            WriteEndElement(o);
        }

        void Write113_ComplexChoiceA(string n, string ns, global::SerializationTypes.ComplexChoiceA o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.ComplexChoiceA)) {
                }
                else {
                    if (t == typeof(global::SerializationTypes.ComplexChoiceB)) {
                        Write112_ComplexChoiceB(n, ns,(global::SerializationTypes.ComplexChoiceB)o, isNullable, true);
                        return;
                    }
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"ComplexChoiceA", @"");
            WriteElementString(@"Name", @"", ((global::System.String)o.@Name));
            WriteEndElement(o);
        }

        void Write114_Item(string n, string ns, global::SerializationTypes.TypeWithPropertyHavingComplexChoice o, bool isNullable, bool needType) {
            if ((object)o == null) {
                if (isNullable) WriteNullTagLiteral(n, ns);
                return;
            }
            if (!needType) {
                System.Type t = o.GetType();
                if (t == typeof(global::SerializationTypes.TypeWithPropertyHavingComplexChoice)) {
                }
                else {
                    throw CreateUnknownTypeException(o);
                }
            }
            WriteStartElement(n, ns, o, false, null);
            if (needType) WriteXsiType(@"TypeWithPropertyHavingComplexChoice", @"");
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
                            if (ci == SerializationTypes.MoreChoices.@Amount && ((object)(ai) != null)) {
                                if (((object)ai) != null && !(ai is global::System.Int32)) throw CreateMismatchChoiceException(@"System.Int32", @"ChoiceArray", @"SerializationTypes.MoreChoices.@Amount");
                                WriteElementStringRaw(@"Amount", @"", System.Xml.XmlConvert.ToString((global::System.Int32)((global::System.Int32)ai)));
                            }
                            else if (ci == SerializationTypes.MoreChoices.@Item && ((object)(ai) != null)) {
                                if (((object)ai) != null && !(ai is global::SerializationTypes.ComplexChoiceA)) throw CreateMismatchChoiceException(@"SerializationTypes.ComplexChoiceA", @"ChoiceArray", @"SerializationTypes.MoreChoices.@Item");
                                Write113_ComplexChoiceA(@"Item", @"", ((global::SerializationTypes.ComplexChoiceA)ai), false, false);
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

        void Write115_TypeWithFieldsOrdered(string n, string ns, global::SerializationTypes.TypeWithFieldsOrdered o, bool isNullable, bool needType) {
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

        void Write96_Item(string n, string ns, global::SerializationTypes.TypeWithSchemaFormInXmlAttribute o, bool isNullable, bool needType) {
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

        public object Read127_TypeWithXmlElementProperty() {
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

        public object Read128_TypeWithXmlDocumentProperty() {
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

        public object Read129_TypeWithBinaryProperty() {
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

        public object Read130_Item() {
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

        public object Read131_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id6_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read9_Item(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithDateAndTimeOnlyProperties");
            }
            return (object)o;
        }

        public object Read132_DateOnlyWrapper() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id7_DateOnlyWrapper && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read10_DateOnlyWrapper(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":DateOnlyWrapper");
            }
            return (object)o;
        }

        public object Read133_TimeOnlyWrapper() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id8_TimeOnlyWrapper && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read11_TimeOnlyWrapper(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TimeOnlyWrapper");
            }
            return (object)o;
        }

        public object Read134_TimeOnlyAsXsdTimeWrapper() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id9_TimeOnlyAsXsdTimeWrapper && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read12_TimeOnlyAsXsdTimeWrapper(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TimeOnlyAsXsdTimeWrapper");
            }
            return (object)o;
        }

        public object Read135_DateTimeDateWrapper() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id10_DateTimeDateWrapper && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read13_DateTimeDateWrapper(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":DateTimeDateWrapper");
            }
            return (object)o;
        }

        public object Read136_DateTimeTimeWrapper() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id11_DateTimeTimeWrapper && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read14_DateTimeTimeWrapper(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":DateTimeTimeWrapper");
            }
            return (object)o;
        }

        public object Read137_TypeWithTimeSpanProperty() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id12_TypeWithTimeSpanProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read15_TypeWithTimeSpanProperty(true, true);
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

        public object Read138_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id13_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read16_Item(true, true);
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

        public object Read139_TypeWithByteProperty() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id14_TypeWithByteProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read17_TypeWithByteProperty(true, true);
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

        public object Read140_TypeWithXmlNodeArrayProperty() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id15_TypeWithXmlNodeArrayProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read18_TypeWithXmlNodeArrayProperty(true, true);
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

        public object Read141_Animal() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id16_Animal && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read19_Animal(true, true);
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

        public object Read142_Dog() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id17_Dog && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read21_Dog(true, true);
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

        public object Read143_DogBreed() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id18_DogBreed && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read20_DogBreed(Reader.ReadElementString());
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

        public object Read144_Group() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id19_Group && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read23_Group(true, true);
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

        public object Read145_Vehicle() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id20_Vehicle && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read22_Vehicle(true, true);
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

        public object Read146_Employee() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id21_Employee && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read24_Employee(true, true);
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

        public object Read147_BaseClass() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id22_BaseClass && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read26_BaseClass(true, true);
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

        public object Read148_DerivedClass() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id23_DerivedClass && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read25_DerivedClass(true, true);
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

        public object Read149_SimpleBaseClass() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id24_SimpleBaseClass && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read28_SimpleBaseClass(true, true);
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

        public object Read150_SimpleDerivedClass() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id25_SimpleDerivedClass && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read27_SimpleDerivedClass(true, true);
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

        public object Read151_BaseIXmlSerializable() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id26_BaseIXmlSerializable && (object) Reader.NamespaceURI == (object)id27_Item)) {
                        System.Xml.XmlQualifiedName tser = GetXsiType();
                        if (tser == null || ((object) ((System.Xml.XmlQualifiedName)tser).Name == (object)id26_BaseIXmlSerializable && (object) ((System.Xml.XmlQualifiedName)tser).Namespace == (object)id27_Item)) {
                            o = (global::XmlSerializableBaseClass)ReadSerializable(( System.Xml.Serialization.IXmlSerializable)new global::XmlSerializableBaseClass());
                        }
                        else if (tser == null || ((object) ((System.Xml.XmlQualifiedName)tser).Name == (object)id28_DerivedIXmlSerializable && (object) ((System.Xml.XmlQualifiedName)tser).Namespace == (object)id27_Item)) {
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

        public object Read152_DerivedIXmlSerializable() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id28_DerivedIXmlSerializable && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        public object Read153_PurchaseOrder() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id29_PurchaseOrder && (object) Reader.NamespaceURI == (object)id30_httpwwwcontoso1com)) {
                        o = Read31_PurchaseOrder(false, true);
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

        public object Read154_Address() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id31_Address && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read32_Address(true, true);
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

        public object Read155_OrderedItem() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id32_OrderedItem && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read33_OrderedItem(true, true);
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

        public object Read156_AliasedTestType() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id33_AliasedTestType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read34_AliasedTestType(true, true);
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

        public object Read157_BaseClass1() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id34_BaseClass1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read35_BaseClass1(true, true);
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

        public object Read158_DerivedClass1() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id35_DerivedClass1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read36_DerivedClass1(true, true);
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

        public object Read159_ArrayOfDateTime() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id36_ArrayOfDateTime && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id37_dateTime && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        public object Read160_Orchestra() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id38_Orchestra && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read38_Orchestra(true, true);
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

        public object Read161_Instrument() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id39_Instrument && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read37_Instrument(true, true);
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

        public object Read162_Brass() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id40_Brass && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read39_Brass(true, true);
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

        public object Read163_Trumpet() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id41_Trumpet && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read40_Trumpet(true, true);
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

        public object Read164_Pet() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id42_Pet && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read41_Pet(true, true);
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

        public object Read165_DefaultValuesSetToNaN() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id43_DefaultValuesSetToNaN && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read42_DefaultValuesSetToNaN(true, true);
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

        public object Read166_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id44_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read43_Item(true, true);
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

        public object Read167_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id45_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read44_Item(true, true);
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

        public object Read168_RootElement() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id46_RootElement && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read45_Item(true, true);
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

        public object Read169_TypeWithLinkedProperty() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id47_TypeWithLinkedProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read46_TypeWithLinkedProperty(true, true);
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

        public object Read170_Document() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id48_Document && (object) Reader.NamespaceURI == (object)id49_httpexamplecom)) {
                        o = Read47_MsgDocumentType(true, true);
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

        public object Read171_RootClass() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id50_RootClass && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read50_RootClass(true, true);
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

        public object Read172_Parameter() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id51_Parameter && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read49_Parameter(true, true);
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

        public object Read173_XElementWrapper() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id52_XElementWrapper && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read51_XElementWrapper(true, true);
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

        public object Read174_XElementStruct() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id53_XElementStruct && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read52_XElementStruct(true);
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

        public object Read175_XElementArrayWrapper() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id54_XElementArrayWrapper && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read53_XElementArrayWrapper(true, true);
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

        public object Read176_TypeWithDateTimeStringProperty() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id55_TypeWithDateTimeStringProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read54_TypeWithDateTimeStringProperty(true, true);
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

        public object Read177_SimpleType() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id56_SimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read55_SimpleType(true, true);
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

        public object Read178_TypeWithGetSetArrayMembers() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id57_TypeWithGetSetArrayMembers && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read56_TypeWithGetSetArrayMembers(true, true);
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

        public object Read179_TypeWithGetOnlyArrayProperties() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id58_TypeWithGetOnlyArrayProperties && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read57_TypeWithGetOnlyArrayProperties(true, true);
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

        public object Read180_TypeWithArraylikeMembers() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id59_TypeWithArraylikeMembers && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read58_TypeWithArraylikeMembers(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithArraylikeMembers");
            }
            return (object)o;
        }

        public object Read181_StructNotSerializable() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id60_StructNotSerializable && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read59_StructNotSerializable(true);
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

        public object Read182_TypeWithMyCollectionField() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id61_TypeWithMyCollectionField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read60_TypeWithMyCollectionField(true, true);
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

        public object Read183_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id62_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read61_Item(true, true);
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

        public object Read184_ArrayOfAnyType() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id63_ArrayOfAnyType && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id64_anyType && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        public object Read185_MyEnum() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id65_MyEnum && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read62_MyEnum(Reader.ReadElementString());
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

        public object Read186_TypeWithEnumMembers() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id66_TypeWithEnumMembers && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read63_TypeWithEnumMembers(true, true);
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

        public object Read187_DCStruct() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id67_DCStruct && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read64_DCStruct(true);
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

        public object Read188_DCClassWithEnumAndStruct() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id68_DCClassWithEnumAndStruct && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read65_DCClassWithEnumAndStruct(true, true);
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

        public object Read189_BuiltInTypes() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id69_BuiltInTypes && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read66_BuiltInTypes(true, true);
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

        public object Read190_TypeA() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id70_TypeA && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read67_TypeA(true, true);
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

        public object Read191_TypeB() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id71_TypeB && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read68_TypeB(true, true);
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

        public object Read192_TypeHasArrayOfASerializedAsB() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id72_TypeHasArrayOfASerializedAsB && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read69_TypeHasArrayOfASerializedAsB(true, true);
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

        public object Read193_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id73_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read70_Item(true, true);
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

        public object Read194_BaseClassWithSamePropertyName() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id74_BaseClassWithSamePropertyName && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read71_BaseClassWithSamePropertyName(true, true);
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

        public object Read195_DerivedClassWithSameProperty() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id75_DerivedClassWithSameProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read72_DerivedClassWithSameProperty(true, true);
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

        public object Read196_DerivedClassWithSameProperty2() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id76_DerivedClassWithSameProperty2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read73_DerivedClassWithSameProperty2(true, true);
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

        public object Read197_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id77_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read74_Item(true, true);
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

        public object Read198_TypeWithByteArrayAsXmlText() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id78_TypeWithByteArrayAsXmlText && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read75_TypeWithByteArrayAsXmlText(true, true);
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

        public object Read199_SimpleDC() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id79_SimpleDC && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read76_SimpleDC(true, true);
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

        public object Read200_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id80_Item && (object) Reader.NamespaceURI == (object)id81_Item)) {
                        o = Read77_Item(false, true);
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

        public object Read201_EnumFlags() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id82_EnumFlags && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read78_EnumFlags(Reader.ReadElementString());
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

        public object Read202_ClassImplementsInterface() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id83_ClassImplementsInterface && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read79_ClassImplementsInterface(true, true);
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

        public object Read203_WithStruct() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id84_WithStruct && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read81_WithStruct(true, true);
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

        public object Read204_SomeStruct() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id85_SomeStruct && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read80_SomeStruct(true);
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

        public object Read205_WithEnums() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id86_WithEnums && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read84_WithEnums(true, true);
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

        public object Read206_WithNullables() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id87_WithNullables && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read88_WithNullables(true, true);
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

        public object Read207_ByteEnum() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id88_ByteEnum && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read89_ByteEnum(Reader.ReadElementString());
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

        public object Read208_SByteEnum() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id89_SByteEnum && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read90_SByteEnum(Reader.ReadElementString());
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

        public object Read209_ShortEnum() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id90_ShortEnum && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read83_ShortEnum(Reader.ReadElementString());
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

        public object Read210_IntEnum() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id91_IntEnum && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read82_IntEnum(Reader.ReadElementString());
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

        public object Read211_UIntEnum() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id92_UIntEnum && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read91_UIntEnum(Reader.ReadElementString());
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

        public object Read212_LongEnum() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id93_LongEnum && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read92_LongEnum(Reader.ReadElementString());
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

        public object Read213_ULongEnum() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id94_ULongEnum && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read93_ULongEnum(Reader.ReadElementString());
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

        public object Read214_AttributeTesting() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id95_AttributeTesting && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read95_XmlSerializerAttributes(false, true);
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

        public object Read215_ItemChoiceType() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id96_ItemChoiceType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read94_ItemChoiceType(Reader.ReadElementString());
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

        public object Read216_TypeWithAnyAttribute() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id97_TypeWithAnyAttribute && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read96_TypeWithAnyAttribute(true, true);
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

        public object Read217_KnownTypesThroughConstructor() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id98_KnownTypesThroughConstructor && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read97_KnownTypesThroughConstructor(true, true);
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

        public object Read218_SimpleKnownTypeValue() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id99_SimpleKnownTypeValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read98_SimpleKnownTypeValue(true, true);
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

        public object Read219_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id100_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        public object Read220_TypeWithPropertyNameSpecified() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id101_TypeWithPropertyNameSpecified && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read99_TypeWithPropertyNameSpecified(true, true);
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

        public object Read221_TypeWithXmlSchemaFormAttribute() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id102_TypeWithXmlSchemaFormAttribute && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read100_TypeWithXmlSchemaFormAttribute(true, true);
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

        public object Read222_MyXmlType() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id103_MyXmlType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read101_Item(true, true);
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

        public object Read223_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id104_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read102_Item(true, true);
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

        public object Read224_CustomDocument() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id105_CustomDocument && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read104_CustomDocument(true, true);
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

        public object Read225_CustomElement() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id106_CustomElement && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read103_CustomElement(true, true);
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

        public object Read226_Item() {
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

        public object Read227_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id107_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read105_Item(true, true);
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

        public object Read228_ServerSettings() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id108_ServerSettings && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read106_ServerSettings(true, true);
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

        public object Read229_TypeWithXmlQualifiedName() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id109_TypeWithXmlQualifiedName && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read107_TypeWithXmlQualifiedName(true, true);
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

        public object Read230_TypeWith2DArrayProperty2() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id110_TypeWith2DArrayProperty2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read108_TypeWith2DArrayProperty2(true, true);
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

        public object Read231_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id111_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read109_Item(true, true);
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

        public object Read232_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id112_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read110_Item(true, true);
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

        public object Read233_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id113_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read111_Item(true, true);
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

        public object Read234_TypeWithShouldSerializeMethod() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id114_TypeWithShouldSerializeMethod && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read112_TypeWithShouldSerializeMethod(true, true);
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

        public object Read235_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id115_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read113_Item(true, true);
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

        public object Read236_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id116_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read114_Item(true, true);
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

        public object Read237_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id117_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read115_Item(true, true);
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

        public object Read238_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id118_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read117_Item(true, true);
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

        public object Read239_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id119_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read120_Item(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":TypeWithPropertyHavingComplexChoice");
            }
            return (object)o;
        }

        public object Read240_MoreChoices() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id120_MoreChoices && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        {
                            o = Read116_MoreChoices(Reader.ReadElementString());
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

        public object Read241_ComplexChoiceA() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id121_ComplexChoiceA && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read119_ComplexChoiceA(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":ComplexChoiceA");
            }
            return (object)o;
        }

        public object Read242_ComplexChoiceB() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id122_ComplexChoiceB && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read118_ComplexChoiceB(true, true);
                        break;
                    }
                    throw CreateUnknownNodeException();
                } while (false);
            }
            else {
                UnknownNode(null, @":ComplexChoiceB");
            }
            return (object)o;
        }

        public object Read243_TypeWithFieldsOrdered() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id123_TypeWithFieldsOrdered && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read121_TypeWithFieldsOrdered(true, true);
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

        public object Read244_Item() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id124_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read122_Item(true, true);
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

        public object Read245_Root() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id125_Root && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read125_Item(true, true);
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

        public object Read246_TypeClashB() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id126_TypeClashB && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read124_TypeNameClash(true, true);
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

        public object Read247_TypeClashA() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id127_TypeClashA && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read123_TypeNameClash(true, true);
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

        public object Read248_Person() {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                do {
                    if (((object) Reader.LocalName == (object)id128_Person && (object) Reader.NamespaceURI == (object)id2_Item)) {
                        o = Read126_Person(true, true);
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

        global::Outer.Person Read126_Person(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id128_Person && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id129_FirstName && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@FirstName = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id130_MiddleName && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@MiddleName = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id131_LastName && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeNameClashA.TypeNameClash Read123_TypeNameClash(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id127_TypeClashA && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id132_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeNameClashB.TypeNameClash Read124_TypeNameClash(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id126_TypeClashB && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id132_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.NamespaceTypeNameClashContainer Read125_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id133_ContainerType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (((object) Reader.LocalName == (object)id134_A && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            a_0 = (global::SerializationTypes.TypeNameClashA.TypeNameClash[])EnsureArrayIndex(a_0, ca_0, typeof(global::SerializationTypes.TypeNameClashA.TypeNameClash));a_0[ca_0++] = Read123_TypeNameClash(false, true);
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id135_B && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            a_1 = (global::SerializationTypes.TypeNameClashB.TypeNameClash[])EnsureArrayIndex(a_1, ca_1, typeof(global::SerializationTypes.TypeNameClashB.TypeNameClash));a_1[ca_1++] = Read124_TypeNameClash(false, true);
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

        global::SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName Read122_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id124_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id136_Value1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Value1 = Read1_Object(false, true);
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id137_Value2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id128_Person && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read126_Person(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id133_ContainerType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read125_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id126_TypeClashB && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read124_TypeNameClash(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id127_TypeClashA && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read123_TypeNameClash(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id124_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read122_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id123_TypeWithFieldsOrdered && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read121_TypeWithFieldsOrdered(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id119_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read120_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id121_ComplexChoiceA && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read119_ComplexChoiceA(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id122_ComplexChoiceB && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read118_ComplexChoiceB(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id118_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read117_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id117_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read115_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id116_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read114_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id115_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read113_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id114_TypeWithShouldSerializeMethod && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read112_TypeWithShouldSerializeMethod(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id113_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read111_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id112_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read110_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id111_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read109_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id110_TypeWith2DArrayProperty2 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read108_TypeWith2DArrayProperty2(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id109_TypeWithXmlQualifiedName && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read107_TypeWithXmlQualifiedName(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id108_ServerSettings && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read106_ServerSettings(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id107_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read105_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id105_CustomDocument && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read104_CustomDocument(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id106_CustomElement && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read103_CustomElement(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id103_MyXmlType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read101_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id102_TypeWithXmlSchemaFormAttribute && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read100_TypeWithXmlSchemaFormAttribute(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id101_TypeWithPropertyNameSpecified && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read99_TypeWithPropertyNameSpecified(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id99_SimpleKnownTypeValue && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read98_SimpleKnownTypeValue(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id98_KnownTypesThroughConstructor && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read97_KnownTypesThroughConstructor(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id97_TypeWithAnyAttribute && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read96_TypeWithAnyAttribute(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id138_XmlSerializerAttributes && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read95_XmlSerializerAttributes(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id87_WithNullables && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read88_WithNullables(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id86_WithEnums && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read84_WithEnums(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id84_WithStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read81_WithStruct(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id85_SomeStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read80_SomeStruct(false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id83_ClassImplementsInterface && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read79_ClassImplementsInterface(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id80_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id81_Item))
                        return Read77_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id79_SimpleDC && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read76_SimpleDC(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id78_TypeWithByteArrayAsXmlText && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read75_TypeWithByteArrayAsXmlText(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id77_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read74_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id74_BaseClassWithSamePropertyName && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read71_BaseClassWithSamePropertyName(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id75_DerivedClassWithSameProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read72_DerivedClassWithSameProperty(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id76_DerivedClassWithSameProperty2 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read73_DerivedClassWithSameProperty2(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id73_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read70_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id72_TypeHasArrayOfASerializedAsB && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read69_TypeHasArrayOfASerializedAsB(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id71_TypeB && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read68_TypeB(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id70_TypeA && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read67_TypeA(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id69_BuiltInTypes && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read66_BuiltInTypes(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id68_DCClassWithEnumAndStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read65_DCClassWithEnumAndStruct(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id67_DCStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read64_DCStruct(false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id66_TypeWithEnumMembers && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read63_TypeWithEnumMembers(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id62_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read61_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id61_TypeWithMyCollectionField && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read60_TypeWithMyCollectionField(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id60_StructNotSerializable && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read59_StructNotSerializable(false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id59_TypeWithArraylikeMembers && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read58_TypeWithArraylikeMembers(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id58_TypeWithGetOnlyArrayProperties && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read57_TypeWithGetOnlyArrayProperties(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id57_TypeWithGetSetArrayMembers && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read56_TypeWithGetSetArrayMembers(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id56_SimpleType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read55_SimpleType(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id55_TypeWithDateTimeStringProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read54_TypeWithDateTimeStringProperty(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id54_XElementArrayWrapper && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read53_XElementArrayWrapper(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id53_XElementStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read52_XElementStruct(false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id52_XElementWrapper && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read51_XElementWrapper(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id50_RootClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read50_RootClass(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id51_Parameter && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read49_Parameter(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id139_ParameterOfString && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read48_ParameterOfString(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id140_MsgDocumentType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id49_httpexamplecom))
                        return Read47_MsgDocumentType(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id47_TypeWithLinkedProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read46_TypeWithLinkedProperty(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id141_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read45_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id45_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read44_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id44_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read43_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id43_DefaultValuesSetToNaN && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read42_DefaultValuesSetToNaN(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id42_Pet && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read41_Pet(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id38_Orchestra && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read38_Orchestra(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id39_Instrument && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read37_Instrument(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id40_Brass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read39_Brass(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id41_Trumpet && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read40_Trumpet(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id34_BaseClass1 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read35_BaseClass1(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id35_DerivedClass1 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read36_DerivedClass1(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id33_AliasedTestType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read34_AliasedTestType(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id32_OrderedItem && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read33_OrderedItem(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id31_Address && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read32_Address(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id29_PurchaseOrder && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id30_httpwwwcontoso1com))
                        return Read31_PurchaseOrder(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id32_OrderedItem && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id30_httpwwwcontoso1com))
                        return Read30_OrderedItem(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id31_Address && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id30_httpwwwcontoso1com))
                        return Read29_Address(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id24_SimpleBaseClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read28_SimpleBaseClass(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id25_SimpleDerivedClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read27_SimpleDerivedClass(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id22_BaseClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read26_BaseClass(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id23_DerivedClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read25_DerivedClass(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id21_Employee && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read24_Employee(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id19_Group && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read23_Group(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id20_Vehicle && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read22_Vehicle(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id16_Animal && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read19_Animal(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id17_Dog && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read21_Dog(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id15_TypeWithXmlNodeArrayProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read18_TypeWithXmlNodeArrayProperty(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id14_TypeWithByteProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read17_TypeWithByteProperty(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id13_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read16_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id12_TypeWithTimeSpanProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read15_TypeWithTimeSpanProperty(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id11_DateTimeTimeWrapper && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read14_DateTimeTimeWrapper(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id10_DateTimeDateWrapper && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read13_DateTimeDateWrapper(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id9_TimeOnlyAsXsdTimeWrapper && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read12_TimeOnlyAsXsdTimeWrapper(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id8_TimeOnlyWrapper && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read11_TimeOnlyWrapper(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id7_DateOnlyWrapper && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read10_DateOnlyWrapper(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id6_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read9_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id5_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read6_Item(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id4_TypeWithBinaryProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read4_TypeWithBinaryProperty(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id3_TypeWithXmlDocumentProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read3_TypeWithXmlDocumentProperty(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id1_TypeWithXmlElementProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                        return Read2_TypeWithXmlElementProperty(isNullable, false);
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id18_DogBreed && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read20_DogBreed(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id142_ArrayOfOrderedItem && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id30_httpwwwcontoso1com)) {
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
                                            if (((object) Reader.LocalName == (object)id32_OrderedItem && (object) Reader.NamespaceURI == (object)id30_httpwwwcontoso1com)) {
                                                z_0_0 = (global::OrderedItem[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::OrderedItem));z_0_0[cz_0_0++] = Read30_OrderedItem(true, true);
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id143_ArrayOfInt && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id144_int && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id145_ArrayOfString && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id146_string && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id147_ArrayOfDouble && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id148_double && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id36_ArrayOfDateTime && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id37_dateTime && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id149_ArrayOfInstrument && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id39_Instrument && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                z_0_0 = (global::Instrument[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::Instrument));z_0_0[cz_0_0++] = Read37_Instrument(true, true);
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id150_ArrayOfTypeWithLinkedProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id47_TypeWithLinkedProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                if ((object)(z_0_0) == null) Reader.Skip(); else z_0_0.Add(Read46_TypeWithLinkedProperty(true, true));
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id151_ArrayOfParameter && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id51_Parameter && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                if ((object)(z_0_0) == null) Reader.Skip(); else z_0_0.Add(Read49_Parameter(true, true));
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id152_ArrayOfXElement && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id153_XElement && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id154_ArrayOfSimpleType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id56_SimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                z_0_0 = (global::SerializationTypes.SimpleType[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::SerializationTypes.SimpleType));z_0_0[cz_0_0++] = Read55_SimpleType(true, true);
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id63_ArrayOfAnyType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id64_anyType && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id65_MyEnum && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read62_MyEnum(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id155_ArrayOfTypeA && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id70_TypeA && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                z_0_0 = (global::SerializationTypes.TypeA[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::SerializationTypes.TypeA));z_0_0[cz_0_0++] = Read67_TypeA(true, true);
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id82_EnumFlags && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read78_EnumFlags(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id91_IntEnum && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read82_IntEnum(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id90_ShortEnum && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read83_ShortEnum(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id88_ByteEnum && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read89_ByteEnum(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id89_SByteEnum && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read90_SByteEnum(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id92_UIntEnum && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read91_UIntEnum(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id93_LongEnum && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read92_LongEnum(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id94_ULongEnum && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read93_ULongEnum(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id96_ItemChoiceType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read94_ItemChoiceType(CollapseWhitespace(Reader.ReadString()));
                        ReadEndElement();
                        return e;
                    }
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id156_ArrayOfItemChoiceType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id96_ItemChoiceType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                {
                                                    z_0_0 = (global::SerializationTypes.ItemChoiceType[])EnsureArrayIndex(z_0_0, cz_0_0, typeof(global::SerializationTypes.ItemChoiceType));z_0_0[cz_0_0++] = Read94_ItemChoiceType(Reader.ReadElementString());
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id145_ArrayOfString && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id157_httpmynamespace)) {
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
                                            if (((object) Reader.LocalName == (object)id146_string && (object) Reader.NamespaceURI == (object)id157_httpmynamespace)) {
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id158_ArrayOfString1 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id159_NoneParameter && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id160_ArrayOfBoolean && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id161_QualifiedParameter && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id162_ArrayOfArrayOfSimpleType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                                            if (((object) Reader.LocalName == (object)id56_SimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                                    if (((object) Reader.LocalName == (object)id56_SimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                                        z_0_0_0 = (global::SerializationTypes.SimpleType[])EnsureArrayIndex(z_0_0_0, cz_0_0_0, typeof(global::SerializationTypes.SimpleType));z_0_0_0[cz_0_0_0++] = Read55_SimpleType(true, true);
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
                    if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id120_MoreChoices && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
                        Reader.ReadStartElement();
                        object e = Read116_MoreChoices(CollapseWhitespace(Reader.ReadString()));
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

        global::SerializationTypes.MoreChoices Read116_MoreChoices(string s) {
            switch (s) {
                case @"None": return global::SerializationTypes.MoreChoices.@None;
                case @"Item": return global::SerializationTypes.MoreChoices.@Item;
                case @"Amount": return global::SerializationTypes.MoreChoices.@Amount;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.MoreChoices));
            }
        }

        global::SerializationTypes.SimpleType Read55_SimpleType(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id56_SimpleType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id163_P1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@P1 = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id164_P2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.ItemChoiceType Read94_ItemChoiceType(string s) {
            switch (s) {
                case @"None": return global::SerializationTypes.ItemChoiceType.@None;
                case @"Word": return global::SerializationTypes.ItemChoiceType.@Word;
                case @"Number": return global::SerializationTypes.ItemChoiceType.@Number;
                case @"DecimalNumber": return global::SerializationTypes.ItemChoiceType.@DecimalNumber;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.ItemChoiceType));
            }
        }

        global::SerializationTypes.ULongEnum Read93_ULongEnum(string s) {
            switch (s) {
                case @"Option0": return global::SerializationTypes.ULongEnum.@Option0;
                case @"Option1": return global::SerializationTypes.ULongEnum.@Option1;
                case @"Option2": return global::SerializationTypes.ULongEnum.@Option2;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.ULongEnum));
            }
        }

        global::SerializationTypes.LongEnum Read92_LongEnum(string s) {
            switch (s) {
                case @"Option0": return global::SerializationTypes.LongEnum.@Option0;
                case @"Option1": return global::SerializationTypes.LongEnum.@Option1;
                case @"Option2": return global::SerializationTypes.LongEnum.@Option2;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.LongEnum));
            }
        }

        global::SerializationTypes.UIntEnum Read91_UIntEnum(string s) {
            switch (s) {
                case @"Option0": return global::SerializationTypes.UIntEnum.@Option0;
                case @"Option1": return global::SerializationTypes.UIntEnum.@Option1;
                case @"Option2": return global::SerializationTypes.UIntEnum.@Option2;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.UIntEnum));
            }
        }

        global::SerializationTypes.SByteEnum Read90_SByteEnum(string s) {
            switch (s) {
                case @"Option0": return global::SerializationTypes.SByteEnum.@Option0;
                case @"Option1": return global::SerializationTypes.SByteEnum.@Option1;
                case @"Option2": return global::SerializationTypes.SByteEnum.@Option2;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.SByteEnum));
            }
        }

        global::SerializationTypes.ByteEnum Read89_ByteEnum(string s) {
            switch (s) {
                case @"Option0": return global::SerializationTypes.ByteEnum.@Option0;
                case @"Option1": return global::SerializationTypes.ByteEnum.@Option1;
                case @"Option2": return global::SerializationTypes.ByteEnum.@Option2;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.ByteEnum));
            }
        }

        global::SerializationTypes.ShortEnum Read83_ShortEnum(string s) {
            switch (s) {
                case @"Option0": return global::SerializationTypes.ShortEnum.@Option0;
                case @"Option1": return global::SerializationTypes.ShortEnum.@Option1;
                case @"Option2": return global::SerializationTypes.ShortEnum.@Option2;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.ShortEnum));
            }
        }

        global::SerializationTypes.IntEnum Read82_IntEnum(string s) {
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

        global::SerializationTypes.EnumFlags Read78_EnumFlags(string s) {
            return (global::SerializationTypes.EnumFlags)ToEnum(s, EnumFlagsValues, @"global::SerializationTypes.EnumFlags");
        }

        global::SerializationTypes.TypeA Read67_TypeA(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id70_TypeA && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id132_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.MyEnum Read62_MyEnum(string s) {
            switch (s) {
                case @"One": return global::SerializationTypes.MyEnum.@One;
                case @"Two": return global::SerializationTypes.MyEnum.@Two;
                case @"Three": return global::SerializationTypes.MyEnum.@Three;
                default: throw CreateUnknownConstantException(s, typeof(global::SerializationTypes.MyEnum));
            }
        }

        global::Parameter Read49_Parameter(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id51_Parameter && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id139_ParameterOfString && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read48_ParameterOfString(isNullable, false);
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::Parameter o;
            o = new global::Parameter();
            System.Span<bool> paramsRead = stackalloc bool[1];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id132_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::Parameter<global::System.String> Read48_ParameterOfString(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id139_ParameterOfString && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id132_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id165_Value && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::TypeWithLinkedProperty Read46_TypeWithLinkedProperty(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id47_TypeWithLinkedProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id166_Child && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Child = Read46_TypeWithLinkedProperty(false, true);
                            paramsRead[0] = true;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id167_Children && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id47_TypeWithLinkedProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    if ((object)(a_1_0) == null) Reader.Skip(); else a_1_0.Add(Read46_TypeWithLinkedProperty(true, true));
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

        global::Instrument Read37_Instrument(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id39_Instrument && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id40_Brass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read39_Brass(isNullable, false);
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id41_Trumpet && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read40_Trumpet(isNullable, false);
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id132_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::Trumpet Read40_Trumpet(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id41_Trumpet && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id132_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Name = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id168_IsValved && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@IsValved = System.Xml.XmlConvert.ToBoolean(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id169_Modulation && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::Brass Read39_Brass(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id40_Brass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id41_Trumpet && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read40_Trumpet(isNullable, false);
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id132_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Name = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id168_IsValved && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::OrderedItem Read30_OrderedItem(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id32_OrderedItem && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id30_httpwwwcontoso1com)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id170_ItemName && (object) Reader.NamespaceURI == (object)id30_httpwwwcontoso1com)) {
                            {
                                o.@ItemName = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id171_Description && (object) Reader.NamespaceURI == (object)id30_httpwwwcontoso1com)) {
                            {
                                o.@Description = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id172_UnitPrice && (object) Reader.NamespaceURI == (object)id30_httpwwwcontoso1com)) {
                            {
                                o.@UnitPrice = System.Xml.XmlConvert.ToDecimal(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id173_Quantity && (object) Reader.NamespaceURI == (object)id30_httpwwwcontoso1com)) {
                            {
                                o.@Quantity = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id174_LineTotal && (object) Reader.NamespaceURI == (object)id30_httpwwwcontoso1com)) {
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

        global::DogBreed Read20_DogBreed(string s) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id48_Document && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id175_BinaryHexContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@BinaryHexContent = ToByteArrayHex(false);
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id176_Base64Content && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id177_DTO && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id178_DTO2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id179_DefaultDTO && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id180_NullableDTO && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@NullableDTO = Read5_NullableOfDateTimeOffset(true);
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id181_NullableDefaultDTO && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::TypeWithDateAndTimeOnlyProperties Read9_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id6_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::TypeWithDateAndTimeOnlyProperties o;
            o = new global::TypeWithDateAndTimeOnlyProperties();
            System.Span<bool> paramsRead = stackalloc bool[12];
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id182_Today && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                if (Reader.IsEmptyElement) {
                                    Reader.Skip();
                                    o.@Today = default(System.DateOnly);
                                }
                                else {
                                    o.@Today = ToDateOnly(Reader.ReadElementString());
                                }
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id183_MyDate && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                if (Reader.IsEmptyElement) {
                                    Reader.Skip();
                                    o.@CustomDate = default(System.DateOnly);
                                }
                                else {
                                    o.@CustomDate = ToDateOnly(Reader.ReadElementString());
                                }
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id184_DefaultDate && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                if (Reader.IsEmptyElement) {
                                    Reader.Skip();
                                    o.@DefaultDate = default(System.DateOnly);
                                }
                                else {
                                    o.@DefaultDate = ToDateOnly(Reader.ReadElementString());
                                }
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id185_NullableDate && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@NullableDate = Read7_NullableOfDateOnly(true);
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id186_NullableDateWithValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@NullableDateWithValue = Read7_NullableOfDateOnly(true);
                            paramsRead[4] = true;
                            break;
                        }
                        if (!paramsRead[5] && ((object) Reader.LocalName == (object)id187_NullableDefaultDate && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@NullableDefaultDate = Read7_NullableOfDateOnly(true);
                            paramsRead[5] = true;
                            break;
                        }
                        if (!paramsRead[6] && ((object) Reader.LocalName == (object)id188_Now && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                if (Reader.IsEmptyElement) {
                                    Reader.Skip();
                                    o.@Now = default(System.TimeOnly);
                                }
                                else {
                                    o.@Now = ToTimeOnly(Reader.ReadElementString());
                                }
                            }
                            paramsRead[6] = true;
                            break;
                        }
                        if (!paramsRead[7] && ((object) Reader.LocalName == (object)id189_MyTime && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                if (Reader.IsEmptyElement) {
                                    Reader.Skip();
                                    o.@CustomTime = default(System.TimeOnly);
                                }
                                else {
                                    o.@CustomTime = ToTimeOnly(Reader.ReadElementString());
                                }
                            }
                            paramsRead[7] = true;
                            break;
                        }
                        if (!paramsRead[8] && ((object) Reader.LocalName == (object)id190_DefaultTime && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                if (Reader.IsEmptyElement) {
                                    Reader.Skip();
                                    o.@DefaultTime = default(System.TimeOnly);
                                }
                                else {
                                    o.@DefaultTime = ToTimeOnly(Reader.ReadElementString());
                                }
                            }
                            paramsRead[8] = true;
                            break;
                        }
                        if (!paramsRead[9] && ((object) Reader.LocalName == (object)id191_NullableTime && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@NullableTime = Read8_NullableOfTimeOnly(true);
                            paramsRead[9] = true;
                            break;
                        }
                        if (!paramsRead[10] && ((object) Reader.LocalName == (object)id192_NullableTimeWithValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@NullableTimeWithValue = Read8_NullableOfTimeOnly(true);
                            paramsRead[10] = true;
                            break;
                        }
                        if (!paramsRead[11] && ((object) Reader.LocalName == (object)id193_NullableDefaultTime && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@NullableDefaultTime = Read8_NullableOfTimeOnly(true);
                            paramsRead[11] = true;
                            break;
                        }
                        UnknownNode((object)o, @":Today, :MyDate, :DefaultDate, :NullableDate, :NullableDateWithValue, :NullableDefaultDate, :Now, :MyTime, :DefaultTime, :NullableTime, :NullableTimeWithValue, :NullableDefaultTime");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":Today, :MyDate, :DefaultDate, :NullableDate, :NullableDateWithValue, :NullableDefaultDate, :Now, :MyTime, :DefaultTime, :NullableTime, :NullableTimeWithValue, :NullableDefaultTime");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::System.Nullable<global::System.TimeOnly> Read8_NullableOfTimeOnly(bool checkType) {
            global::System.Nullable<global::System.TimeOnly> o = default(global::System.Nullable<global::System.TimeOnly>);
            if (ReadNull())
                return o;
            {
                if (Reader.IsEmptyElement) {
                    Reader.Skip();
                    o = default(System.TimeOnly);
                }
                else {
                    o = ToTimeOnly(Reader.ReadElementString());
                }
            }
            return o;
        }

        global::System.Nullable<global::System.DateOnly> Read7_NullableOfDateOnly(bool checkType) {
            global::System.Nullable<global::System.DateOnly> o = default(global::System.Nullable<global::System.DateOnly>);
            if (ReadNull())
                return o;
            {
                if (Reader.IsEmptyElement) {
                    Reader.Skip();
                    o = default(System.DateOnly);
                }
                else {
                    o = ToDateOnly(Reader.ReadElementString());
                }
            }
            return o;
        }

        global::DateOnlyWrapper Read10_DateOnlyWrapper(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id7_DateOnlyWrapper && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::DateOnlyWrapper o;
            o = new global::DateOnlyWrapper();
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id194_TestValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                if (Reader.IsEmptyElement) {
                                    Reader.Skip();
                                    o.@TestValue = default(System.DateOnly);
                                }
                                else {
                                    o.@TestValue = ToDateOnly(Reader.ReadElementString());
                                }
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":TestValue");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":TestValue");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::TimeOnlyWrapper Read11_TimeOnlyWrapper(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id8_TimeOnlyWrapper && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::TimeOnlyWrapper o;
            o = new global::TimeOnlyWrapper();
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id194_TestValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                if (Reader.IsEmptyElement) {
                                    Reader.Skip();
                                    o.@TestValue = default(System.TimeOnly);
                                }
                                else {
                                    o.@TestValue = ToTimeOnly(Reader.ReadElementString());
                                }
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":TestValue");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":TestValue");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::TimeOnlyAsXsdTimeWrapper Read12_TimeOnlyAsXsdTimeWrapper(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id9_TimeOnlyAsXsdTimeWrapper && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::TimeOnlyAsXsdTimeWrapper o;
            o = new global::TimeOnlyAsXsdTimeWrapper();
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id194_TestValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                if (Reader.IsEmptyElement) {
                                    Reader.Skip();
                                    o.@TestValue = default(System.TimeOnly);
                                }
                                else {
                                    o.@TestValue = ToTimeOnlyIgnoreOffset(Reader.ReadElementString());
                                }
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":TestValue");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":TestValue");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::DateTimeDateWrapper Read13_DateTimeDateWrapper(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id10_DateTimeDateWrapper && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::DateTimeDateWrapper o;
            o = new global::DateTimeDateWrapper();
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id194_TestValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@TestValue = ToDate(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":TestValue");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":TestValue");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::DateTimeTimeWrapper Read14_DateTimeTimeWrapper(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id11_DateTimeTimeWrapper && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::DateTimeTimeWrapper o;
            o = new global::DateTimeTimeWrapper();
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id194_TestValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@TestValue = ToTime(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        UnknownNode((object)o, @":TestValue");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":TestValue");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::TypeWithTimeSpanProperty Read15_TypeWithTimeSpanProperty(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id12_TypeWithTimeSpanProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id195_TimeSpanProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::TypeWithDefaultTimeSpanProperty Read16_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id13_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id195_TimeSpanProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id196_TimeSpanProperty2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::TypeWithByteProperty Read17_TypeWithByteProperty(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id14_TypeWithByteProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id197_ByteProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::TypeWithXmlNodeArrayProperty Read18_TypeWithXmlNodeArrayProperty(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id15_TypeWithXmlNodeArrayProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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

        global::Dog Read21_Dog(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id17_Dog && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id198_Age && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Age = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id132_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Name = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id199_Breed && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Breed = Read20_DogBreed(Reader.ReadElementString());
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

        global::Animal Read19_Animal(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id16_Animal && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id17_Dog && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read21_Dog(isNullable, false);
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id198_Age && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Age = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id132_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::Vehicle Read22_Vehicle(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id20_Vehicle && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id200_LicenseNumber && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::Group Read23_Group(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id19_Group && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id201_GroupName && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@GroupName = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id202_GroupVehicle && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@GroupVehicle = Read22_Vehicle(false, true);
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

        global::Employee Read24_Employee(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id21_Employee && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id203_EmployeeName && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::DerivedClass Read25_DerivedClass(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id23_DerivedClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id165_Value && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Value = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id204_value && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::BaseClass Read26_BaseClass(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id22_BaseClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id23_DerivedClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read25_DerivedClass(isNullable, false);
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id165_Value && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Value = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id204_value && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SimpleDerivedClass Read27_SimpleDerivedClass(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id25_SimpleDerivedClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id205_AttributeString && (object) Reader.NamespaceURI == (object)id2_Item)) {
                    o.@AttributeString = Reader.Value;
                    paramsRead[0] = true;
                }
                else if (!paramsRead[1] && ((object) Reader.LocalName == (object)id206_DateTimeValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
                    o.@DateTimeValue = ToDateTime(Reader.Value);
                    paramsRead[1] = true;
                }
                else if (!paramsRead[2] && ((object) Reader.LocalName == (object)id207_BoolValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SimpleBaseClass Read28_SimpleBaseClass(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id24_SimpleBaseClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id25_SimpleDerivedClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read27_SimpleDerivedClass(isNullable, false);
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SimpleBaseClass o;
            o = new global::SimpleBaseClass();
            System.Span<bool> paramsRead = stackalloc bool[2];
            while (Reader.MoveToNextAttribute()) {
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id205_AttributeString && (object) Reader.NamespaceURI == (object)id2_Item)) {
                    o.@AttributeString = Reader.Value;
                    paramsRead[0] = true;
                }
                else if (!paramsRead[1] && ((object) Reader.LocalName == (object)id206_DateTimeValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::Address Read29_Address(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id31_Address && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id30_httpwwwcontoso1com)) {
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
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id132_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id208_Line1 && (object) Reader.NamespaceURI == (object)id30_httpwwwcontoso1com)) {
                            {
                                o.@Line1 = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id209_City && (object) Reader.NamespaceURI == (object)id30_httpwwwcontoso1com)) {
                            {
                                o.@City = Reader.ReadElementString();
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id210_State && (object) Reader.NamespaceURI == (object)id30_httpwwwcontoso1com)) {
                            {
                                o.@State = Reader.ReadElementString();
                            }
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id211_Zip && (object) Reader.NamespaceURI == (object)id30_httpwwwcontoso1com)) {
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

        global::PurchaseOrder Read31_PurchaseOrder(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id29_PurchaseOrder && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id30_httpwwwcontoso1com)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id212_ShipTo && (object) Reader.NamespaceURI == (object)id30_httpwwwcontoso1com)) {
                            o.@ShipTo = Read29_Address(false, true);
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id213_OrderDate && (object) Reader.NamespaceURI == (object)id30_httpwwwcontoso1com)) {
                            {
                                o.@OrderDate = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id214_Items && (object) Reader.NamespaceURI == (object)id30_httpwwwcontoso1com)) {
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
                                                if (((object) Reader.LocalName == (object)id32_OrderedItem && (object) Reader.NamespaceURI == (object)id30_httpwwwcontoso1com)) {
                                                    a_2_0 = (global::OrderedItem[])EnsureArrayIndex(a_2_0, ca_2_0, typeof(global::OrderedItem));a_2_0[ca_2_0++] = Read30_OrderedItem(true, true);
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
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id215_SubTotal && (object) Reader.NamespaceURI == (object)id30_httpwwwcontoso1com)) {
                            {
                                o.@SubTotal = System.Xml.XmlConvert.ToDecimal(Reader.ReadElementString());
                            }
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id216_ShipCost && (object) Reader.NamespaceURI == (object)id30_httpwwwcontoso1com)) {
                            {
                                o.@ShipCost = System.Xml.XmlConvert.ToDecimal(Reader.ReadElementString());
                            }
                            paramsRead[4] = true;
                            break;
                        }
                        if (!paramsRead[5] && ((object) Reader.LocalName == (object)id217_TotalCost && (object) Reader.NamespaceURI == (object)id30_httpwwwcontoso1com)) {
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

        global::Address Read32_Address(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id31_Address && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id132_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id208_Line1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Line1 = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id209_City && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@City = Reader.ReadElementString();
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id210_State && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@State = Reader.ReadElementString();
                            }
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id211_Zip && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::OrderedItem Read33_OrderedItem(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id32_OrderedItem && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id170_ItemName && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@ItemName = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id171_Description && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Description = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id172_UnitPrice && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@UnitPrice = System.Xml.XmlConvert.ToDecimal(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id173_Quantity && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Quantity = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id174_LineTotal && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::AliasedTestType Read34_AliasedTestType(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id33_AliasedTestType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id218_X && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id144_int && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id219_Y && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id146_string && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id220_Z && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id148_double && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::DerivedClass1 Read36_DerivedClass1(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id35_DerivedClass1 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (((object) Reader.LocalName == (object)id221_Prop && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::BaseClass1 Read35_BaseClass1(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id34_BaseClass1 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id35_DerivedClass1 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read36_DerivedClass1(isNullable, false);
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
                        if (((object) Reader.LocalName == (object)id221_Prop && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::Orchestra Read38_Orchestra(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id38_Orchestra && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (((object) Reader.LocalName == (object)id222_Instruments && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id39_Instrument && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    a_0_0 = (global::Instrument[])EnsureArrayIndex(a_0_0, ca_0_0, typeof(global::Instrument));a_0_0[ca_0_0++] = Read37_Instrument(true, true);
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

        global::Pet Read41_Pet(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id42_Pet && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id16_Animal && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Animal = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id223_Comment2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::DefaultValuesSetToNaN Read42_DefaultValuesSetToNaN(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id43_DefaultValuesSetToNaN && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id224_DoubleField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@DoubleField = System.Xml.XmlConvert.ToDouble(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id225_SingleField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@SingleField = System.Xml.XmlConvert.ToSingle(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id226_DoubleProp && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@DoubleProp = System.Xml.XmlConvert.ToDouble(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id227_FloatProp && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::DefaultValuesSetToPositiveInfinity Read43_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id44_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id224_DoubleField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@DoubleField = System.Xml.XmlConvert.ToDouble(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id225_SingleField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@SingleField = System.Xml.XmlConvert.ToSingle(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id226_DoubleProp && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@DoubleProp = System.Xml.XmlConvert.ToDouble(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id227_FloatProp && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::DefaultValuesSetToNegativeInfinity Read44_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id45_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id224_DoubleField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@DoubleField = System.Xml.XmlConvert.ToDouble(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id225_SingleField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@SingleField = System.Xml.XmlConvert.ToSingle(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id226_DoubleProp && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@DoubleProp = System.Xml.XmlConvert.ToDouble(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id227_FloatProp && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::TypeWithMismatchBetweenAttributeAndPropertyType Read45_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id141_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id228_IntValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::MsgDocumentType Read47_MsgDocumentType(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id140_MsgDocumentType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id49_httpexamplecom)) {
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
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id229_id && (object) Reader.NamespaceURI == (object)id2_Item)) {
                    o.@Id = CollapseWhitespace(Reader.Value);
                    paramsRead[0] = true;
                }
                else if (((object) Reader.LocalName == (object)id230_refs && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::RootClass Read50_RootClass(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id50_RootClass && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (((object) Reader.LocalName == (object)id231_Parameters && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id51_Parameter && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    if ((object)(a_0_0) == null) Reader.Skip(); else a_0_0.Add(Read49_Parameter(true, true));
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

        global::XElementWrapper Read51_XElementWrapper(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id52_XElementWrapper && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id165_Value && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::XElementStruct Read52_XElementStruct(bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id53_XElementStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id232_xelement && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::XElementArrayWrapper Read53_XElementArrayWrapper(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id54_XElementArrayWrapper && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (((object) Reader.LocalName == (object)id233_xelements && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id153_XElement && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithDateTimeStringProperty Read54_TypeWithDateTimeStringProperty(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id55_TypeWithDateTimeStringProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id234_DateTimeString && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@DateTimeString = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id235_CurrentDateTime && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithGetSetArrayMembers Read56_TypeWithGetSetArrayMembers(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id57_TypeWithGetSetArrayMembers && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (((object) Reader.LocalName == (object)id236_F1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id56_SimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    a_0_0 = (global::SerializationTypes.SimpleType[])EnsureArrayIndex(a_0_0, ca_0_0, typeof(global::SerializationTypes.SimpleType));a_0_0[ca_0_0++] = Read55_SimpleType(true, true);
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
                        if (((object) Reader.LocalName == (object)id237_F2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id144_int && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (((object) Reader.LocalName == (object)id163_P1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id56_SimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    a_2_0 = (global::SerializationTypes.SimpleType[])EnsureArrayIndex(a_2_0, ca_2_0, typeof(global::SerializationTypes.SimpleType));a_2_0[ca_2_0++] = Read55_SimpleType(true, true);
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
                        if (((object) Reader.LocalName == (object)id164_P2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id144_int && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithGetOnlyArrayProperties Read57_TypeWithGetOnlyArrayProperties(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id58_TypeWithGetOnlyArrayProperties && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithArraylikeMembers Read58_TypeWithArraylikeMembers(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id59_TypeWithArraylikeMembers && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithArraylikeMembers o;
            o = new global::SerializationTypes.TypeWithArraylikeMembers();
            global::System.Int32[] a_0 = null;
            int ca_0 = 0;
            global::System.Int32[] a_1 = null;
            int ca_1 = 0;
            if ((object)(o.@IntLField) == null) o.@IntLField = new global::System.Collections.Generic.List<global::System.Int32>();
            global::System.Collections.Generic.List<global::System.Int32> a_2 = (global::System.Collections.Generic.List<global::System.Int32>)o.@IntLField;
            if ((object)(o.@NIntLField) == null) o.@NIntLField = new global::System.Collections.Generic.List<global::System.Int32>();
            global::System.Collections.Generic.List<global::System.Int32> a_3 = (global::System.Collections.Generic.List<global::System.Int32>)o.@NIntLField;
            global::System.Int32[] a_4 = null;
            int ca_4 = 0;
            global::System.Int32[] a_5 = null;
            int ca_5 = 0;
            if ((object)(o.@IntLProp) == null) o.@IntLProp = new global::System.Collections.Generic.List<global::System.Int32>();
            global::System.Collections.Generic.List<global::System.Int32> a_6 = (global::System.Collections.Generic.List<global::System.Int32>)o.@IntLProp;
            if ((object)(o.@NIntLProp) == null) o.@NIntLProp = new global::System.Collections.Generic.List<global::System.Int32>();
            global::System.Collections.Generic.List<global::System.Int32> a_7 = (global::System.Collections.Generic.List<global::System.Int32>)o.@NIntLProp;
            System.Span<bool> paramsRead = stackalloc bool[8];
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
                        if (((object) Reader.LocalName == (object)id238_IntAField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                global::System.Int32[] a_0_0 = null;
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
                                                if (((object) Reader.LocalName == (object)id144_int && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    {
                                                        a_0_0 = (global::System.Int32[])EnsureArrayIndex(a_0_0, ca_0_0, typeof(global::System.Int32));a_0_0[ca_0_0++] = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
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
                                o.@IntAField = (global::System.Int32[])ShrinkArray(a_0_0, ca_0_0, typeof(global::System.Int32), false);
                            }
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id239_NIntAField && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id144_int && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                o.@NIntAField = (global::System.Int32[])ShrinkArray(a_1_0, ca_1_0, typeof(global::System.Int32), false);
                            }
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id240_IntLField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                if ((object)(o.@IntLField) == null) o.@IntLField = new global::System.Collections.Generic.List<global::System.Int32>();
                                global::System.Collections.Generic.List<global::System.Int32> a_2_0 = (global::System.Collections.Generic.List<global::System.Int32>)o.@IntLField;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id144_int && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    {
                                                        a_2_0.Add(System.Xml.XmlConvert.ToInt32(Reader.ReadElementString()));
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
                        if (((object) Reader.LocalName == (object)id241_NIntLField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                if ((object)(o.@NIntLField) == null) o.@NIntLField = new global::System.Collections.Generic.List<global::System.Int32>();
                                global::System.Collections.Generic.List<global::System.Int32> a_3_0 = (global::System.Collections.Generic.List<global::System.Int32>)o.@NIntLField;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id144_int && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    {
                                                        a_3_0.Add(System.Xml.XmlConvert.ToInt32(Reader.ReadElementString()));
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
                            else {
                                if ((object)(o.@NIntLField) == null) o.@NIntLField = new global::System.Collections.Generic.List<global::System.Int32>();
                                global::System.Collections.Generic.List<global::System.Int32> a_3_0 = (global::System.Collections.Generic.List<global::System.Int32>)o.@NIntLField;
                            }
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id242_IntAProp && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                global::System.Int32[] a_4_0 = null;
                                int ca_4_0 = 0;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id144_int && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    {
                                                        a_4_0 = (global::System.Int32[])EnsureArrayIndex(a_4_0, ca_4_0, typeof(global::System.Int32));a_4_0[ca_4_0++] = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
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
                                o.@IntAProp = (global::System.Int32[])ShrinkArray(a_4_0, ca_4_0, typeof(global::System.Int32), false);
                            }
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id243_NIntAProp && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                global::System.Int32[] a_5_0 = null;
                                int ca_5_0 = 0;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id144_int && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    {
                                                        a_5_0 = (global::System.Int32[])EnsureArrayIndex(a_5_0, ca_5_0, typeof(global::System.Int32));a_5_0[ca_5_0++] = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
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
                                o.@NIntAProp = (global::System.Int32[])ShrinkArray(a_5_0, ca_5_0, typeof(global::System.Int32), false);
                            }
                            else {
                                global::System.Int32[] a_5_0 = null;
                                int ca_5_0 = 0;
                                o.@NIntAProp = (global::System.Int32[])ShrinkArray(a_5_0, ca_5_0, typeof(global::System.Int32), true);
                            }
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id244_IntLProp && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                if ((object)(o.@IntLProp) == null) o.@IntLProp = new global::System.Collections.Generic.List<global::System.Int32>();
                                global::System.Collections.Generic.List<global::System.Int32> a_6_0 = (global::System.Collections.Generic.List<global::System.Int32>)o.@IntLProp;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id144_int && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    {
                                                        a_6_0.Add(System.Xml.XmlConvert.ToInt32(Reader.ReadElementString()));
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
                        if (((object) Reader.LocalName == (object)id245_NIntLProp && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (!ReadNull()) {
                                if ((object)(o.@NIntLProp) == null) o.@NIntLProp = new global::System.Collections.Generic.List<global::System.Int32>();
                                global::System.Collections.Generic.List<global::System.Int32> a_7_0 = (global::System.Collections.Generic.List<global::System.Int32>)o.@NIntLProp;
                                if ((Reader.IsEmptyElement)) {
                                    Reader.Skip();
                                }
                                else {
                                    Reader.ReadStartElement();
                                    Reader.MoveToContent();
                                    while (Reader.NodeType != System.Xml.XmlNodeType.EndElement && Reader.NodeType != System.Xml.XmlNodeType.None) {
                                        if (Reader.NodeType == System.Xml.XmlNodeType.Element) {
                                            do {
                                                if (((object) Reader.LocalName == (object)id144_int && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    {
                                                        a_7_0.Add(System.Xml.XmlConvert.ToInt32(Reader.ReadElementString()));
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
                        UnknownNode((object)o, @":IntAField, :NIntAField, :IntLField, :NIntLField, :IntAProp, :NIntAProp, :IntLProp, :NIntLProp");
                    } while (false);
                }
                else {
                    UnknownNode((object)o, @":IntAField, :NIntAField, :IntLField, :NIntLField, :IntAProp, :NIntAProp, :IntLProp, :NIntLProp");
                }
                Reader.MoveToContent();
            }
            ReadEndElement();
            return o;
        }

        global::SerializationTypes.StructNotSerializable Read59_StructNotSerializable(bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id60_StructNotSerializable && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id204_value && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithMyCollectionField Read60_TypeWithMyCollectionField(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id61_TypeWithMyCollectionField && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (((object) Reader.LocalName == (object)id246_Collection && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id146_string && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithReadOnlyMyCollectionProperty Read61_Item(bool isNullable, bool checkType) {
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
                        if (((object) Reader.LocalName == (object)id246_Collection && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id146_string && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithEnumMembers Read63_TypeWithEnumMembers(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id66_TypeWithEnumMembers && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id236_F1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@F1 = Read62_MyEnum(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id163_P1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@P1 = Read62_MyEnum(Reader.ReadElementString());
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

        global::SerializationTypes.DCStruct Read64_DCStruct(bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id67_DCStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id247_Data && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.DCClassWithEnumAndStruct Read65_DCClassWithEnumAndStruct(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id68_DCClassWithEnumAndStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id248_MyStruct && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@MyStruct = Read64_DCStruct(true);
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id249_MyEnum1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@MyEnum1 = Read62_MyEnum(Reader.ReadElementString());
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

        global::SerializationTypes.BuiltInTypes Read66_BuiltInTypes(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id69_BuiltInTypes && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id250_ByteArray && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeB Read68_TypeB(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id71_TypeB && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id132_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeHasArrayOfASerializedAsB Read69_TypeHasArrayOfASerializedAsB(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id72_TypeHasArrayOfASerializedAsB && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (((object) Reader.LocalName == (object)id214_Items && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id70_TypeA && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    a_0_0 = (global::SerializationTypes.TypeA[])EnsureArrayIndex(a_0_0, ca_0_0, typeof(global::SerializationTypes.TypeA));a_0_0[ca_0_0++] = Read67_TypeA(true, true);
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

        global::SerializationTypes.@__TypeNameWithSpecialCharacters漢ñ Read70_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id73_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id251_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.DerivedClassWithSameProperty2 Read73_DerivedClassWithSameProperty2(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id76_DerivedClassWithSameProperty2 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id252_StringProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@StringProperty = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id253_IntProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@IntProperty = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id254_DateTimeProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@DateTimeProperty = ToDateTime(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id255_ListProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id146_string && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.DerivedClassWithSameProperty Read72_DerivedClassWithSameProperty(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id75_DerivedClassWithSameProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id76_DerivedClassWithSameProperty2 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read73_DerivedClassWithSameProperty2(isNullable, false);
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id252_StringProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@StringProperty = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id253_IntProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@IntProperty = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id254_DateTimeProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@DateTimeProperty = ToDateTime(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id255_ListProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id146_string && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.BaseClassWithSamePropertyName Read71_BaseClassWithSamePropertyName(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id74_BaseClassWithSamePropertyName && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id75_DerivedClassWithSameProperty && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read72_DerivedClassWithSameProperty(isNullable, false);
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id76_DerivedClassWithSameProperty2 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read73_DerivedClassWithSameProperty2(isNullable, false);
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id252_StringProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@StringProperty = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id253_IntProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@IntProperty = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id254_DateTimeProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@DateTimeProperty = ToDateTime(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id255_ListProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id146_string && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithDateTimePropertyAsXmlTime Read74_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id77_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithByteArrayAsXmlText Read75_TypeWithByteArrayAsXmlText(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id78_TypeWithByteArrayAsXmlText && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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

        global::SerializationTypes.SimpleDC Read76_SimpleDC(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id79_SimpleDC && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id247_Data && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithXmlTextAttributeOnArray Read77_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id80_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id81_Item)) {
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

        global::SerializationTypes.ClassImplementsInterface Read79_ClassImplementsInterface(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id83_ClassImplementsInterface && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id256_ClassID && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@ClassID = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id257_DisplayName && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@DisplayName = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id258_Id && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Id = Reader.ReadElementString();
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id259_IsLoaded && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.SomeStruct Read80_SomeStruct(bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id85_SomeStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id134_A && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@A = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id135_B && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.WithStruct Read81_WithStruct(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id84_WithStruct && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id260_Some && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Some = Read80_SomeStruct(true);
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

        global::SerializationTypes.WithEnums Read84_WithEnums(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id86_WithEnums && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id261_Int && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Int = Read82_IntEnum(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id262_Short && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Short = Read83_ShortEnum(Reader.ReadElementString());
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

        global::SerializationTypes.WithNullables Read88_WithNullables(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id87_WithNullables && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id263_Optional && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Optional = Read85_NullableOfIntEnum(true);
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id264_Optionull && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Optionull = Read85_NullableOfIntEnum(true);
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id265_OptionalInt && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@OptionalInt = Read86_NullableOfInt32(true);
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id266_OptionullInt && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@OptionullInt = Read86_NullableOfInt32(true);
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id267_Struct1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Struct1 = Read87_NullableOfSomeStruct(true);
                            paramsRead[4] = true;
                            break;
                        }
                        if (!paramsRead[5] && ((object) Reader.LocalName == (object)id268_Struct2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@Struct2 = Read87_NullableOfSomeStruct(true);
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

        global::System.Nullable<global::SerializationTypes.SomeStruct> Read87_NullableOfSomeStruct(bool checkType) {
            global::System.Nullable<global::SerializationTypes.SomeStruct> o = default(global::System.Nullable<global::SerializationTypes.SomeStruct>);
            if (ReadNull())
                return o;
            o = Read80_SomeStruct(true);
            return o;
        }

        global::System.Nullable<global::System.Int32> Read86_NullableOfInt32(bool checkType) {
            global::System.Nullable<global::System.Int32> o = default(global::System.Nullable<global::System.Int32>);
            if (ReadNull())
                return o;
            {
                o = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
            }
            return o;
        }

        global::System.Nullable<global::SerializationTypes.IntEnum> Read85_NullableOfIntEnum(bool checkType) {
            global::System.Nullable<global::SerializationTypes.IntEnum> o = default(global::System.Nullable<global::SerializationTypes.IntEnum>);
            if (ReadNull())
                return o;
            {
                o = Read82_IntEnum(Reader.ReadElementString());
            }
            return o;
        }

        global::SerializationTypes.XmlSerializerAttributes Read95_XmlSerializerAttributes(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id138_XmlSerializerAttributes && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                if (!paramsRead[6] && ((object) Reader.LocalName == (object)id269_XmlAttributeName && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id270_Word && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@MyChoice = Reader.ReadElementString();
                            }
                            o.@EnumType = global::SerializationTypes.ItemChoiceType.@Word;
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id271_Number && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@MyChoice = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            o.@EnumType = global::SerializationTypes.ItemChoiceType.@Number;
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id272_DecimalNumber && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@MyChoice = System.Xml.XmlConvert.ToDouble(Reader.ReadElementString());
                            }
                            o.@EnumType = global::SerializationTypes.ItemChoiceType.@DecimalNumber;
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id273_XmlIncludeProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@XmlIncludeProperty = Read1_Object(false, true);
                            paramsRead[1] = true;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id274_XmlEnumProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id96_ItemChoiceType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                    {
                                                        a_2_0 = (global::SerializationTypes.ItemChoiceType[])EnsureArrayIndex(a_2_0, ca_2_0, typeof(global::SerializationTypes.ItemChoiceType));a_2_0[ca_2_0++] = Read94_ItemChoiceType(Reader.ReadElementString());
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
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id275_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@XmlNamespaceDeclarationsProperty = Reader.ReadElementString();
                            }
                            paramsRead[4] = true;
                            break;
                        }
                        if (!paramsRead[5] && ((object) Reader.LocalName == (object)id276_XmlElementPropertyNode && (object) Reader.NamespaceURI == (object)id277_httpelement)) {
                            {
                                o.@XmlElementProperty = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[5] = true;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id278_CustomXmlArrayProperty && (object) Reader.NamespaceURI == (object)id157_httpmynamespace)) {
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
                                                if (((object) Reader.LocalName == (object)id146_string && (object) Reader.NamespaceURI == (object)id157_httpmynamespace)) {
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

        global::SerializationTypes.TypeWithAnyAttribute Read96_TypeWithAnyAttribute(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id97_TypeWithAnyAttribute && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                if (!paramsRead[1] && ((object) Reader.LocalName == (object)id253_IntProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id132_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.KnownTypesThroughConstructor Read97_KnownTypesThroughConstructor(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id98_KnownTypesThroughConstructor && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id279_EnumValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@EnumValue = Read1_Object(false, true);
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id280_SimpleTypeValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.SimpleKnownTypeValue Read98_SimpleKnownTypeValue(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id99_SimpleKnownTypeValue && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id281_StrProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithPropertyNameSpecified Read99_TypeWithPropertyNameSpecified(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id101_TypeWithPropertyNameSpecified && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id282_MyField && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@MyFieldSpecified = true;
                            {
                                o.@MyField = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id283_MyFieldIgnored && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithXmlSchemaFormAttribute Read100_TypeWithXmlSchemaFormAttribute(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id102_TypeWithXmlSchemaFormAttribute && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (((object) Reader.LocalName == (object)id284_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id144_int && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (((object) Reader.LocalName == (object)id285_NoneSchemaFormListProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id159_NoneParameter && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                        if (((object) Reader.LocalName == (object)id286_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id161_QualifiedParameter && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithTypeNameInXmlTypeAttribute Read101_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id103_MyXmlType && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id287_XmlAttributeForm && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.CustomElement Read103_CustomElement(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id106_CustomElement && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id288_name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.CustomDocument Read104_CustomDocument(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id105_CustomDocument && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (((object) Reader.LocalName == (object)id289_customElement && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if ((object)(a_0) == null) Reader.Skip(); else a_0.Add(Read103_CustomElement(false, true));
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

        global::SerializationTypes.TypeWithNonPublicDefaultConstructor Read105_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id107_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id132_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.ServerSettings Read106_ServerSettings(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id108_ServerSettings && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id290_DS2Root && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@DS2Root = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id291_MetricConfigUrl && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithXmlQualifiedName Read107_TypeWithXmlQualifiedName(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id109_TypeWithXmlQualifiedName && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id165_Value && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWith2DArrayProperty2 Read108_TypeWith2DArrayProperty2(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id110_TypeWith2DArrayProperty2 && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (((object) Reader.LocalName == (object)id292_TwoDArrayOfSimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                if (((object) Reader.LocalName == (object)id56_SimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
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
                                                                        if (((object) Reader.LocalName == (object)id56_SimpleType && (object) Reader.NamespaceURI == (object)id2_Item)) {
                                                                            a_0_0_0 = (global::SerializationTypes.SimpleType[])EnsureArrayIndex(a_0_0_0, ca_0_0_0, typeof(global::SerializationTypes.SimpleType));a_0_0_0[ca_0_0_0++] = Read55_SimpleType(true, true);
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

        global::SerializationTypes.TypeWithPropertiesHavingDefaultValue Read109_Item(bool isNullable, bool checkType) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id293_EmptyStringProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@EmptyStringProperty = Reader.ReadElementString();
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id252_StringProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@StringProperty = Reader.ReadElementString();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id253_IntProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@IntProperty = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id294_CharProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithEnumPropertyHavingDefaultValue Read110_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id112_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id295_EnumProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@EnumProperty = Read82_IntEnum(Reader.ReadElementString());
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

        global::SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue Read111_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id113_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id295_EnumProperty && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            if (Reader.IsEmptyElement) {
                                Reader.Skip();
                            }
                            else {
                                o.@EnumProperty = Read78_EnumFlags(Reader.ReadElementString());
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

        global::SerializationTypes.TypeWithShouldSerializeMethod Read112_TypeWithShouldSerializeMethod(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id114_TypeWithShouldSerializeMethod && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id296_Foo && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.KnownTypesThroughConstructorWithArrayProperties Read113_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id115_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id297_StringArrayValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            o.@StringArrayValue = Read1_Object(false, true);
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id298_IntArrayValue && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.KnownTypesThroughConstructorWithValue Read114_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id116_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id165_Value && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithTypesHavingCustomFormatter Read115_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id117_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id299_DateTimeContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@DateTimeContent = ToDateTime(Reader.ReadElementString());
                            }
                            paramsRead[0] = true;
                            break;
                        }
                        if (!paramsRead[1] && ((object) Reader.LocalName == (object)id300_QNameContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@QNameContent = ReadElementQualifiedName();
                            }
                            paramsRead[1] = true;
                            break;
                        }
                        if (!paramsRead[2] && ((object) Reader.LocalName == (object)id301_DateContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@DateContent = ToDate(Reader.ReadElementString());
                            }
                            paramsRead[2] = true;
                            break;
                        }
                        if (!paramsRead[3] && ((object) Reader.LocalName == (object)id302_NameContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@NameContent = ToXmlName(Reader.ReadElementString());
                            }
                            paramsRead[3] = true;
                            break;
                        }
                        if (!paramsRead[4] && ((object) Reader.LocalName == (object)id303_NCNameContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@NCNameContent = ToXmlNCName(Reader.ReadElementString());
                            }
                            paramsRead[4] = true;
                            break;
                        }
                        if (!paramsRead[5] && ((object) Reader.LocalName == (object)id304_NMTOKENContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@NMTOKENContent = ToXmlNmToken(Reader.ReadElementString());
                            }
                            paramsRead[5] = true;
                            break;
                        }
                        if (!paramsRead[6] && ((object) Reader.LocalName == (object)id305_NMTOKENSContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@NMTOKENSContent = ToXmlNmTokens(Reader.ReadElementString());
                            }
                            paramsRead[6] = true;
                            break;
                        }
                        if (!paramsRead[7] && ((object) Reader.LocalName == (object)id306_Base64BinaryContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@Base64BinaryContent = ToByteArrayBase64(false);
                            }
                            paramsRead[7] = true;
                            break;
                        }
                        if (!paramsRead[8] && ((object) Reader.LocalName == (object)id307_HexBinaryContent && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithArrayPropertyHavingChoice Read117_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id118_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (((object) Reader.LocalName == (object)id308_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                a_0 = (global::System.Object[])EnsureArrayIndex(a_0, ca_0, typeof(global::System.Object));a_0[ca_0++] = Reader.ReadElementString();
                            }
                            choice_a_0 = (global::SerializationTypes.MoreChoices[])EnsureArrayIndex(choice_a_0, cchoice_a_0, typeof(global::SerializationTypes.MoreChoices));choice_a_0[cchoice_a_0++] = global::SerializationTypes.MoreChoices.@Item;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id309_Amount && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.ComplexChoiceB Read118_ComplexChoiceB(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id122_ComplexChoiceB && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.ComplexChoiceB o;
            o = new global::SerializationTypes.ComplexChoiceB();
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id132_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.ComplexChoiceA Read119_ComplexChoiceA(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id121_ComplexChoiceA && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                if (((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id122_ComplexChoiceB && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item))
                    return Read118_ComplexChoiceB(isNullable, false);
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.ComplexChoiceA o;
            o = new global::SerializationTypes.ComplexChoiceA();
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
                        if (!paramsRead[0] && ((object) Reader.LocalName == (object)id132_Name && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithPropertyHavingComplexChoice Read120_Item(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id119_Item && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
            }
            else {
                throw CreateUnknownTypeException((System.Xml.XmlQualifiedName)xsiType);
            }
            }
            if (isNull) return null;
            global::SerializationTypes.TypeWithPropertyHavingComplexChoice o;
            o = new global::SerializationTypes.TypeWithPropertyHavingComplexChoice();
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
                        if (((object) Reader.LocalName == (object)id308_Item && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            a_0 = (global::System.Object[])EnsureArrayIndex(a_0, ca_0, typeof(global::System.Object));a_0[ca_0++] = Read119_ComplexChoiceA(false, true);
                            choice_a_0 = (global::SerializationTypes.MoreChoices[])EnsureArrayIndex(choice_a_0, cchoice_a_0, typeof(global::SerializationTypes.MoreChoices));choice_a_0[cchoice_a_0++] = global::SerializationTypes.MoreChoices.@Item;
                            break;
                        }
                        if (((object) Reader.LocalName == (object)id309_Amount && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithFieldsOrdered Read121_TypeWithFieldsOrdered(bool isNullable, bool checkType) {
            System.Xml.XmlQualifiedName xsiType = checkType ? GetXsiType() : null;
            bool isNull = false;
            if (isNullable) isNull = ReadNull();
            if (checkType) {
            if (xsiType == null || ((object) ((System.Xml.XmlQualifiedName)xsiType).Name == (object)id123_TypeWithFieldsOrdered && (object) ((System.Xml.XmlQualifiedName)xsiType).Namespace == (object)id2_Item)) {
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
                        if (((object) Reader.LocalName == (object)id310_IntField2 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@IntField2 = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                        }
                        state = 1;
                        break;
                    case 1:
                        if (((object) Reader.LocalName == (object)id311_IntField1 && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@IntField1 = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                            }
                        }
                        state = 2;
                        break;
                    case 2:
                        if (((object) Reader.LocalName == (object)id312_strfld && (object) Reader.NamespaceURI == (object)id2_Item)) {
                            {
                                o.@StringField2 = Reader.ReadElementString();
                            }
                        }
                        state = 3;
                        break;
                    case 3:
                        if (((object) Reader.LocalName == (object)id312_strfld && (object) Reader.NamespaceURI == (object)id2_Item)) {
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

        global::SerializationTypes.TypeWithSchemaFormInXmlAttribute Read102_Item(bool isNullable, bool checkType) {
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
                if (!paramsRead[0] && ((object) Reader.LocalName == (object)id313_TestProperty && (object) Reader.NamespaceURI == (object)id314_httptestcom)) {
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

        string id9_TimeOnlyAsXsdTimeWrapper;
        string id65_MyEnum;
        string id205_AttributeString;
        string id14_TypeWithByteProperty;
        string id218_X;
        string id10_DateTimeDateWrapper;
        string id197_ByteProperty;
        string id136_Value1;
        string id118_Item;
        string id297_StringArrayValue;
        string id51_Parameter;
        string id156_ArrayOfItemChoiceType;
        string id93_LongEnum;
        string id37_dateTime;
        string id192_NullableTimeWithValue;
        string id103_MyXmlType;
        string id49_httpexamplecom;
        string id113_Item;
        string id287_XmlAttributeForm;
        string id149_ArrayOfInstrument;
        string id41_Trumpet;
        string id89_SByteEnum;
        string id63_ArrayOfAnyType;
        string id117_Item;
        string id144_int;
        string id121_ComplexChoiceA;
        string id255_ListProperty;
        string id243_NIntAProp;
        string id162_ArrayOfArrayOfSimpleType;
        string id236_F1;
        string id171_Description;
        string id311_IntField1;
        string id96_ItemChoiceType;
        string id215_SubTotal;
        string id44_Item;
        string id179_DefaultDTO;
        string id290_DS2Root;
        string id222_Instruments;
        string id61_TypeWithMyCollectionField;
        string id235_CurrentDateTime;
        string id246_Collection;
        string id45_Item;
        string id74_BaseClassWithSamePropertyName;
        string id253_IntProperty;
        string id275_Item;
        string id277_httpelement;
        string id294_CharProperty;
        string id265_OptionalInt;
        string id67_DCStruct;
        string id150_ArrayOfTypeWithLinkedProperty;
        string id288_name;
        string id234_DateTimeString;
        string id230_refs;
        string id148_double;
        string id102_TypeWithXmlSchemaFormAttribute;
        string id71_TypeB;
        string id228_IntValue;
        string id30_httpwwwcontoso1com;
        string id86_WithEnums;
        string id186_NullableDateWithValue;
        string id83_ClassImplementsInterface;
        string id106_CustomElement;
        string id266_OptionullInt;
        string id168_IsValved;
        string id151_ArrayOfParameter;
        string id204_value;
        string id175_BinaryHexContent;
        string id302_NameContent;
        string id165_Value;
        string id155_ArrayOfTypeA;
        string id124_Item;
        string id55_TypeWithDateTimeStringProperty;
        string id25_SimpleDerivedClass;
        string id249_MyEnum1;
        string id11_DateTimeTimeWrapper;
        string id122_ComplexChoiceB;
        string id16_Animal;
        string id178_DTO2;
        string id213_OrderDate;
        string id32_OrderedItem;
        string id141_Item;
        string id111_Item;
        string id57_TypeWithGetSetArrayMembers;
        string id164_P2;
        string id160_ArrayOfBoolean;
        string id108_ServerSettings;
        string id69_BuiltInTypes;
        string id289_customElement;
        string id130_MiddleName;
        string id120_MoreChoices;
        string id147_ArrayOfDouble;
        string id101_TypeWithPropertyNameSpecified;
        string id21_Employee;
        string id78_TypeWithByteArrayAsXmlText;
        string id226_DoubleProp;
        string id308_Item;
        string id312_strfld;
        string id60_StructNotSerializable;
        string id95_AttributeTesting;
        string id87_WithNullables;
        string id18_DogBreed;
        string id24_SimpleBaseClass;
        string id159_NoneParameter;
        string id8_TimeOnlyWrapper;
        string id128_Person;
        string id48_Document;
        string id58_TypeWithGetOnlyArrayProperties;
        string id62_Item;
        string id258_Id;
        string id143_ArrayOfInt;
        string id242_IntAProp;
        string id23_DerivedClass;
        string id70_TypeA;
        string id35_DerivedClass1;
        string id203_EmployeeName;
        string id47_TypeWithLinkedProperty;
        string id99_SimpleKnownTypeValue;
        string id251_Item;
        string id138_XmlSerializerAttributes;
        string id31_Address;
        string id92_UIntEnum;
        string id131_LastName;
        string id97_TypeWithAnyAttribute;
        string id210_State;
        string id188_Now;
        string id256_ClassID;
        string id193_NullableDefaultTime;
        string id185_NullableDate;
        string id183_MyDate;
        string id276_XmlElementPropertyNode;
        string id77_Item;
        string id46_RootElement;
        string id217_TotalCost;
        string id248_MyStruct;
        string id81_Item;
        string id180_NullableDTO;
        string id269_XmlAttributeName;
        string id296_Foo;
        string id134_A;
        string id64_anyType;
        string id142_ArrayOfOrderedItem;
        string id145_ArrayOfString;
        string id181_NullableDefaultDTO;
        string id212_ShipTo;
        string id293_EmptyStringProperty;
        string id73_Item;
        string id211_Zip;
        string id56_SimpleType;
        string id262_Short;
        string id33_AliasedTestType;
        string id154_ArrayOfSimpleType;
        string id152_ArrayOfXElement;
        string id229_id;
        string id82_EnumFlags;
        string id206_DateTimeValue;
        string id196_TimeSpanProperty2;
        string id271_Number;
        string id305_NMTOKENSContent;
        string id75_DerivedClassWithSameProperty;
        string id284_Item;
        string id114_TypeWithShouldSerializeMethod;
        string id198_Age;
        string id307_HexBinaryContent;
        string id126_TypeClashB;
        string id295_EnumProperty;
        string id146_string;
        string id250_ByteArray;
        string id109_TypeWithXmlQualifiedName;
        string id6_Item;
        string id170_ItemName;
        string id119_Item;
        string id28_DerivedIXmlSerializable;
        string id298_IntArrayValue;
        string id190_DefaultTime;
        string id129_FirstName;
        string id261_Int;
        string id220_Z;
        string id105_CustomDocument;
        string id176_Base64Content;
        string id202_GroupVehicle;
        string id223_Comment2;
        string id1_TypeWithXmlElementProperty;
        string id153_XElement;
        string id303_NCNameContent;
        string id19_Group;
        string id268_Struct2;
        string id252_StringProperty;
        string id20_Vehicle;
        string id201_GroupName;
        string id3_TypeWithXmlDocumentProperty;
        string id85_SomeStruct;
        string id209_City;
        string id133_ContainerType;
        string id301_DateContent;
        string id140_MsgDocumentType;
        string id278_CustomXmlArrayProperty;
        string id72_TypeHasArrayOfASerializedAsB;
        string id116_Item;
        string id264_Optionull;
        string id194_TestValue;
        string id237_F2;
        string id98_KnownTypesThroughConstructor;
        string id76_DerivedClassWithSameProperty2;
        string id283_MyFieldIgnored;
        string id263_Optional;
        string id214_Items;
        string id79_SimpleDC;
        string id306_Base64BinaryContent;
        string id158_ArrayOfString1;
        string id43_DefaultValuesSetToNaN;
        string id314_httptestcom;
        string id292_TwoDArrayOfSimpleType;
        string id310_IntField2;
        string id104_Item;
        string id257_DisplayName;
        string id100_Item;
        string id12_TypeWithTimeSpanProperty;
        string id191_NullableTime;
        string id26_BaseIXmlSerializable;
        string id132_Name;
        string id225_SingleField;
        string id221_Prop;
        string id286_Item;
        string id167_Children;
        string id27_Item;
        string id174_LineTotal;
        string id40_Brass;
        string id231_Parameters;
        string id4_TypeWithBinaryProperty;
        string id17_Dog;
        string id182_Today;
        string id2_Item;
        string id195_TimeSpanProperty;
        string id219_Y;
        string id184_DefaultDate;
        string id273_XmlIncludeProperty;
        string id50_RootClass;
        string id123_TypeWithFieldsOrdered;
        string id259_IsLoaded;
        string id285_NoneSchemaFormListProperty;
        string id172_UnitPrice;
        string id42_Pet;
        string id80_Item;
        string id260_Some;
        string id157_httpmynamespace;
        string id281_StrProperty;
        string id125_Root;
        string id208_Line1;
        string id227_FloatProp;
        string id304_NMTOKENContent;
        string id280_SimpleTypeValue;
        string id187_NullableDefaultDate;
        string id189_MyTime;
        string id135_B;
        string id238_IntAField;
        string id161_QualifiedParameter;
        string id169_Modulation;
        string id54_XElementArrayWrapper;
        string id173_Quantity;
        string id267_Struct1;
        string id163_P1;
        string id232_xelement;
        string id199_Breed;
        string id247_Data;
        string id254_DateTimeProperty;
        string id233_xelements;
        string id137_Value2;
        string id53_XElementStruct;
        string id177_DTO;
        string id139_ParameterOfString;
        string id90_ShortEnum;
        string id34_BaseClass1;
        string id52_XElementWrapper;
        string id7_DateOnlyWrapper;
        string id59_TypeWithArraylikeMembers;
        string id66_TypeWithEnumMembers;
        string id68_DCClassWithEnumAndStruct;
        string id15_TypeWithXmlNodeArrayProperty;
        string id22_BaseClass;
        string id272_DecimalNumber;
        string id299_DateTimeContent;
        string id91_IntEnum;
        string id112_Item;
        string id239_NIntAField;
        string id110_TypeWith2DArrayProperty2;
        string id240_IntLField;
        string id5_Item;
        string id274_XmlEnumProperty;
        string id291_MetricConfigUrl;
        string id244_IntLProp;
        string id29_PurchaseOrder;
        string id115_Item;
        string id36_ArrayOfDateTime;
        string id313_TestProperty;
        string id88_ByteEnum;
        string id84_WithStruct;
        string id282_MyField;
        string id216_ShipCost;
        string id245_NIntLProp;
        string id279_EnumValue;
        string id127_TypeClashA;
        string id38_Orchestra;
        string id309_Amount;
        string id13_Item;
        string id224_DoubleField;
        string id39_Instrument;
        string id94_ULongEnum;
        string id200_LicenseNumber;
        string id300_QNameContent;
        string id107_Item;
        string id241_NIntLField;
        string id166_Child;
        string id270_Word;
        string id207_BoolValue;

        protected override void InitIDs() {
            id9_TimeOnlyAsXsdTimeWrapper = Reader.NameTable.Add(@"TimeOnlyAsXsdTimeWrapper");
            id65_MyEnum = Reader.NameTable.Add(@"MyEnum");
            id205_AttributeString = Reader.NameTable.Add(@"AttributeString");
            id14_TypeWithByteProperty = Reader.NameTable.Add(@"TypeWithByteProperty");
            id218_X = Reader.NameTable.Add(@"X");
            id10_DateTimeDateWrapper = Reader.NameTable.Add(@"DateTimeDateWrapper");
            id197_ByteProperty = Reader.NameTable.Add(@"ByteProperty");
            id136_Value1 = Reader.NameTable.Add(@"Value1");
            id118_Item = Reader.NameTable.Add(@"TypeWithArrayPropertyHavingChoice");
            id297_StringArrayValue = Reader.NameTable.Add(@"StringArrayValue");
            id51_Parameter = Reader.NameTable.Add(@"Parameter");
            id156_ArrayOfItemChoiceType = Reader.NameTable.Add(@"ArrayOfItemChoiceType");
            id93_LongEnum = Reader.NameTable.Add(@"LongEnum");
            id37_dateTime = Reader.NameTable.Add(@"dateTime");
            id192_NullableTimeWithValue = Reader.NameTable.Add(@"NullableTimeWithValue");
            id103_MyXmlType = Reader.NameTable.Add(@"MyXmlType");
            id49_httpexamplecom = Reader.NameTable.Add(@"http://example.com");
            id113_Item = Reader.NameTable.Add(@"TypeWithEnumFlagPropertyHavingDefaultValue");
            id287_XmlAttributeForm = Reader.NameTable.Add(@"XmlAttributeForm");
            id149_ArrayOfInstrument = Reader.NameTable.Add(@"ArrayOfInstrument");
            id41_Trumpet = Reader.NameTable.Add(@"Trumpet");
            id89_SByteEnum = Reader.NameTable.Add(@"SByteEnum");
            id63_ArrayOfAnyType = Reader.NameTable.Add(@"ArrayOfAnyType");
            id117_Item = Reader.NameTable.Add(@"TypeWithTypesHavingCustomFormatter");
            id144_int = Reader.NameTable.Add(@"int");
            id121_ComplexChoiceA = Reader.NameTable.Add(@"ComplexChoiceA");
            id255_ListProperty = Reader.NameTable.Add(@"ListProperty");
            id243_NIntAProp = Reader.NameTable.Add(@"NIntAProp");
            id162_ArrayOfArrayOfSimpleType = Reader.NameTable.Add(@"ArrayOfArrayOfSimpleType");
            id236_F1 = Reader.NameTable.Add(@"F1");
            id171_Description = Reader.NameTable.Add(@"Description");
            id311_IntField1 = Reader.NameTable.Add(@"IntField1");
            id96_ItemChoiceType = Reader.NameTable.Add(@"ItemChoiceType");
            id215_SubTotal = Reader.NameTable.Add(@"SubTotal");
            id44_Item = Reader.NameTable.Add(@"DefaultValuesSetToPositiveInfinity");
            id179_DefaultDTO = Reader.NameTable.Add(@"DefaultDTO");
            id290_DS2Root = Reader.NameTable.Add(@"DS2Root");
            id222_Instruments = Reader.NameTable.Add(@"Instruments");
            id61_TypeWithMyCollectionField = Reader.NameTable.Add(@"TypeWithMyCollectionField");
            id235_CurrentDateTime = Reader.NameTable.Add(@"CurrentDateTime");
            id246_Collection = Reader.NameTable.Add(@"Collection");
            id45_Item = Reader.NameTable.Add(@"DefaultValuesSetToNegativeInfinity");
            id74_BaseClassWithSamePropertyName = Reader.NameTable.Add(@"BaseClassWithSamePropertyName");
            id253_IntProperty = Reader.NameTable.Add(@"IntProperty");
            id275_Item = Reader.NameTable.Add(@"XmlNamespaceDeclarationsProperty");
            id277_httpelement = Reader.NameTable.Add(@"http://element");
            id294_CharProperty = Reader.NameTable.Add(@"CharProperty");
            id265_OptionalInt = Reader.NameTable.Add(@"OptionalInt");
            id67_DCStruct = Reader.NameTable.Add(@"DCStruct");
            id150_ArrayOfTypeWithLinkedProperty = Reader.NameTable.Add(@"ArrayOfTypeWithLinkedProperty");
            id288_name = Reader.NameTable.Add(@"name");
            id234_DateTimeString = Reader.NameTable.Add(@"DateTimeString");
            id230_refs = Reader.NameTable.Add(@"refs");
            id148_double = Reader.NameTable.Add(@"double");
            id102_TypeWithXmlSchemaFormAttribute = Reader.NameTable.Add(@"TypeWithXmlSchemaFormAttribute");
            id71_TypeB = Reader.NameTable.Add(@"TypeB");
            id228_IntValue = Reader.NameTable.Add(@"IntValue");
            id30_httpwwwcontoso1com = Reader.NameTable.Add(@"http://www.contoso1.com");
            id86_WithEnums = Reader.NameTable.Add(@"WithEnums");
            id186_NullableDateWithValue = Reader.NameTable.Add(@"NullableDateWithValue");
            id83_ClassImplementsInterface = Reader.NameTable.Add(@"ClassImplementsInterface");
            id106_CustomElement = Reader.NameTable.Add(@"CustomElement");
            id266_OptionullInt = Reader.NameTable.Add(@"OptionullInt");
            id168_IsValved = Reader.NameTable.Add(@"IsValved");
            id151_ArrayOfParameter = Reader.NameTable.Add(@"ArrayOfParameter");
            id204_value = Reader.NameTable.Add(@"value");
            id175_BinaryHexContent = Reader.NameTable.Add(@"BinaryHexContent");
            id302_NameContent = Reader.NameTable.Add(@"NameContent");
            id165_Value = Reader.NameTable.Add(@"Value");
            id155_ArrayOfTypeA = Reader.NameTable.Add(@"ArrayOfTypeA");
            id124_Item = Reader.NameTable.Add(@"TypeWithKnownTypesOfCollectionsWithConflictingXmlName");
            id55_TypeWithDateTimeStringProperty = Reader.NameTable.Add(@"TypeWithDateTimeStringProperty");
            id25_SimpleDerivedClass = Reader.NameTable.Add(@"SimpleDerivedClass");
            id249_MyEnum1 = Reader.NameTable.Add(@"MyEnum1");
            id11_DateTimeTimeWrapper = Reader.NameTable.Add(@"DateTimeTimeWrapper");
            id122_ComplexChoiceB = Reader.NameTable.Add(@"ComplexChoiceB");
            id16_Animal = Reader.NameTable.Add(@"Animal");
            id178_DTO2 = Reader.NameTable.Add(@"DTO2");
            id213_OrderDate = Reader.NameTable.Add(@"OrderDate");
            id32_OrderedItem = Reader.NameTable.Add(@"OrderedItem");
            id141_Item = Reader.NameTable.Add(@"TypeWithMismatchBetweenAttributeAndPropertyType");
            id111_Item = Reader.NameTable.Add(@"TypeWithPropertiesHavingDefaultValue");
            id57_TypeWithGetSetArrayMembers = Reader.NameTable.Add(@"TypeWithGetSetArrayMembers");
            id164_P2 = Reader.NameTable.Add(@"P2");
            id160_ArrayOfBoolean = Reader.NameTable.Add(@"ArrayOfBoolean");
            id108_ServerSettings = Reader.NameTable.Add(@"ServerSettings");
            id69_BuiltInTypes = Reader.NameTable.Add(@"BuiltInTypes");
            id289_customElement = Reader.NameTable.Add(@"customElement");
            id130_MiddleName = Reader.NameTable.Add(@"MiddleName");
            id120_MoreChoices = Reader.NameTable.Add(@"MoreChoices");
            id147_ArrayOfDouble = Reader.NameTable.Add(@"ArrayOfDouble");
            id101_TypeWithPropertyNameSpecified = Reader.NameTable.Add(@"TypeWithPropertyNameSpecified");
            id21_Employee = Reader.NameTable.Add(@"Employee");
            id78_TypeWithByteArrayAsXmlText = Reader.NameTable.Add(@"TypeWithByteArrayAsXmlText");
            id226_DoubleProp = Reader.NameTable.Add(@"DoubleProp");
            id308_Item = Reader.NameTable.Add(@"Item");
            id312_strfld = Reader.NameTable.Add(@"strfld");
            id60_StructNotSerializable = Reader.NameTable.Add(@"StructNotSerializable");
            id95_AttributeTesting = Reader.NameTable.Add(@"AttributeTesting");
            id87_WithNullables = Reader.NameTable.Add(@"WithNullables");
            id18_DogBreed = Reader.NameTable.Add(@"DogBreed");
            id24_SimpleBaseClass = Reader.NameTable.Add(@"SimpleBaseClass");
            id159_NoneParameter = Reader.NameTable.Add(@"NoneParameter");
            id8_TimeOnlyWrapper = Reader.NameTable.Add(@"TimeOnlyWrapper");
            id128_Person = Reader.NameTable.Add(@"Person");
            id48_Document = Reader.NameTable.Add(@"Document");
            id58_TypeWithGetOnlyArrayProperties = Reader.NameTable.Add(@"TypeWithGetOnlyArrayProperties");
            id62_Item = Reader.NameTable.Add(@"TypeWithReadOnlyMyCollectionProperty");
            id258_Id = Reader.NameTable.Add(@"Id");
            id143_ArrayOfInt = Reader.NameTable.Add(@"ArrayOfInt");
            id242_IntAProp = Reader.NameTable.Add(@"IntAProp");
            id23_DerivedClass = Reader.NameTable.Add(@"DerivedClass");
            id70_TypeA = Reader.NameTable.Add(@"TypeA");
            id35_DerivedClass1 = Reader.NameTable.Add(@"DerivedClass1");
            id203_EmployeeName = Reader.NameTable.Add(@"EmployeeName");
            id47_TypeWithLinkedProperty = Reader.NameTable.Add(@"TypeWithLinkedProperty");
            id99_SimpleKnownTypeValue = Reader.NameTable.Add(@"SimpleKnownTypeValue");
            id251_Item = Reader.NameTable.Add(@"PropertyNameWithSpecialCharacters漢ñ");
            id138_XmlSerializerAttributes = Reader.NameTable.Add(@"XmlSerializerAttributes");
            id31_Address = Reader.NameTable.Add(@"Address");
            id92_UIntEnum = Reader.NameTable.Add(@"UIntEnum");
            id131_LastName = Reader.NameTable.Add(@"LastName");
            id97_TypeWithAnyAttribute = Reader.NameTable.Add(@"TypeWithAnyAttribute");
            id210_State = Reader.NameTable.Add(@"State");
            id188_Now = Reader.NameTable.Add(@"Now");
            id256_ClassID = Reader.NameTable.Add(@"ClassID");
            id193_NullableDefaultTime = Reader.NameTable.Add(@"NullableDefaultTime");
            id185_NullableDate = Reader.NameTable.Add(@"NullableDate");
            id183_MyDate = Reader.NameTable.Add(@"MyDate");
            id276_XmlElementPropertyNode = Reader.NameTable.Add(@"XmlElementPropertyNode");
            id77_Item = Reader.NameTable.Add(@"TypeWithDateTimePropertyAsXmlTime");
            id46_RootElement = Reader.NameTable.Add(@"RootElement");
            id217_TotalCost = Reader.NameTable.Add(@"TotalCost");
            id248_MyStruct = Reader.NameTable.Add(@"MyStruct");
            id81_Item = Reader.NameTable.Add(@"http://schemas.xmlsoap.org/ws/2005/04/discovery");
            id180_NullableDTO = Reader.NameTable.Add(@"NullableDTO");
            id269_XmlAttributeName = Reader.NameTable.Add(@"XmlAttributeName");
            id296_Foo = Reader.NameTable.Add(@"Foo");
            id134_A = Reader.NameTable.Add(@"A");
            id64_anyType = Reader.NameTable.Add(@"anyType");
            id142_ArrayOfOrderedItem = Reader.NameTable.Add(@"ArrayOfOrderedItem");
            id145_ArrayOfString = Reader.NameTable.Add(@"ArrayOfString");
            id181_NullableDefaultDTO = Reader.NameTable.Add(@"NullableDefaultDTO");
            id212_ShipTo = Reader.NameTable.Add(@"ShipTo");
            id293_EmptyStringProperty = Reader.NameTable.Add(@"EmptyStringProperty");
            id73_Item = Reader.NameTable.Add(@"__TypeNameWithSpecialCharacters漢ñ");
            id211_Zip = Reader.NameTable.Add(@"Zip");
            id56_SimpleType = Reader.NameTable.Add(@"SimpleType");
            id262_Short = Reader.NameTable.Add(@"Short");
            id33_AliasedTestType = Reader.NameTable.Add(@"AliasedTestType");
            id154_ArrayOfSimpleType = Reader.NameTable.Add(@"ArrayOfSimpleType");
            id152_ArrayOfXElement = Reader.NameTable.Add(@"ArrayOfXElement");
            id229_id = Reader.NameTable.Add(@"id");
            id82_EnumFlags = Reader.NameTable.Add(@"EnumFlags");
            id206_DateTimeValue = Reader.NameTable.Add(@"DateTimeValue");
            id196_TimeSpanProperty2 = Reader.NameTable.Add(@"TimeSpanProperty2");
            id271_Number = Reader.NameTable.Add(@"Number");
            id305_NMTOKENSContent = Reader.NameTable.Add(@"NMTOKENSContent");
            id75_DerivedClassWithSameProperty = Reader.NameTable.Add(@"DerivedClassWithSameProperty");
            id284_Item = Reader.NameTable.Add(@"UnqualifiedSchemaFormListProperty");
            id114_TypeWithShouldSerializeMethod = Reader.NameTable.Add(@"TypeWithShouldSerializeMethod");
            id198_Age = Reader.NameTable.Add(@"Age");
            id307_HexBinaryContent = Reader.NameTable.Add(@"HexBinaryContent");
            id126_TypeClashB = Reader.NameTable.Add(@"TypeClashB");
            id295_EnumProperty = Reader.NameTable.Add(@"EnumProperty");
            id146_string = Reader.NameTable.Add(@"string");
            id250_ByteArray = Reader.NameTable.Add(@"ByteArray");
            id109_TypeWithXmlQualifiedName = Reader.NameTable.Add(@"TypeWithXmlQualifiedName");
            id6_Item = Reader.NameTable.Add(@"TypeWithDateAndTimeOnlyProperties");
            id170_ItemName = Reader.NameTable.Add(@"ItemName");
            id119_Item = Reader.NameTable.Add(@"TypeWithPropertyHavingComplexChoice");
            id28_DerivedIXmlSerializable = Reader.NameTable.Add(@"DerivedIXmlSerializable");
            id298_IntArrayValue = Reader.NameTable.Add(@"IntArrayValue");
            id190_DefaultTime = Reader.NameTable.Add(@"DefaultTime");
            id129_FirstName = Reader.NameTable.Add(@"FirstName");
            id261_Int = Reader.NameTable.Add(@"Int");
            id220_Z = Reader.NameTable.Add(@"Z");
            id105_CustomDocument = Reader.NameTable.Add(@"CustomDocument");
            id176_Base64Content = Reader.NameTable.Add(@"Base64Content");
            id202_GroupVehicle = Reader.NameTable.Add(@"GroupVehicle");
            id223_Comment2 = Reader.NameTable.Add(@"Comment2");
            id1_TypeWithXmlElementProperty = Reader.NameTable.Add(@"TypeWithXmlElementProperty");
            id153_XElement = Reader.NameTable.Add(@"XElement");
            id303_NCNameContent = Reader.NameTable.Add(@"NCNameContent");
            id19_Group = Reader.NameTable.Add(@"Group");
            id268_Struct2 = Reader.NameTable.Add(@"Struct2");
            id252_StringProperty = Reader.NameTable.Add(@"StringProperty");
            id20_Vehicle = Reader.NameTable.Add(@"Vehicle");
            id201_GroupName = Reader.NameTable.Add(@"GroupName");
            id3_TypeWithXmlDocumentProperty = Reader.NameTable.Add(@"TypeWithXmlDocumentProperty");
            id85_SomeStruct = Reader.NameTable.Add(@"SomeStruct");
            id209_City = Reader.NameTable.Add(@"City");
            id133_ContainerType = Reader.NameTable.Add(@"ContainerType");
            id301_DateContent = Reader.NameTable.Add(@"DateContent");
            id140_MsgDocumentType = Reader.NameTable.Add(@"MsgDocumentType");
            id278_CustomXmlArrayProperty = Reader.NameTable.Add(@"CustomXmlArrayProperty");
            id72_TypeHasArrayOfASerializedAsB = Reader.NameTable.Add(@"TypeHasArrayOfASerializedAsB");
            id116_Item = Reader.NameTable.Add(@"KnownTypesThroughConstructorWithValue");
            id264_Optionull = Reader.NameTable.Add(@"Optionull");
            id194_TestValue = Reader.NameTable.Add(@"TestValue");
            id237_F2 = Reader.NameTable.Add(@"F2");
            id98_KnownTypesThroughConstructor = Reader.NameTable.Add(@"KnownTypesThroughConstructor");
            id76_DerivedClassWithSameProperty2 = Reader.NameTable.Add(@"DerivedClassWithSameProperty2");
            id283_MyFieldIgnored = Reader.NameTable.Add(@"MyFieldIgnored");
            id263_Optional = Reader.NameTable.Add(@"Optional");
            id214_Items = Reader.NameTable.Add(@"Items");
            id79_SimpleDC = Reader.NameTable.Add(@"SimpleDC");
            id306_Base64BinaryContent = Reader.NameTable.Add(@"Base64BinaryContent");
            id158_ArrayOfString1 = Reader.NameTable.Add(@"ArrayOfString1");
            id43_DefaultValuesSetToNaN = Reader.NameTable.Add(@"DefaultValuesSetToNaN");
            id314_httptestcom = Reader.NameTable.Add(@"http://test.com");
            id292_TwoDArrayOfSimpleType = Reader.NameTable.Add(@"TwoDArrayOfSimpleType");
            id310_IntField2 = Reader.NameTable.Add(@"IntField2");
            id104_Item = Reader.NameTable.Add(@"TypeWithSchemaFormInXmlAttribute");
            id257_DisplayName = Reader.NameTable.Add(@"DisplayName");
            id100_Item = Reader.NameTable.Add(@"ClassImplementingIXmlSerializable");
            id12_TypeWithTimeSpanProperty = Reader.NameTable.Add(@"TypeWithTimeSpanProperty");
            id191_NullableTime = Reader.NameTable.Add(@"NullableTime");
            id26_BaseIXmlSerializable = Reader.NameTable.Add(@"BaseIXmlSerializable");
            id132_Name = Reader.NameTable.Add(@"Name");
            id225_SingleField = Reader.NameTable.Add(@"SingleField");
            id221_Prop = Reader.NameTable.Add(@"Prop");
            id286_Item = Reader.NameTable.Add(@"QualifiedSchemaFormListProperty");
            id167_Children = Reader.NameTable.Add(@"Children");
            id27_Item = Reader.NameTable.Add(@"http://example.com/serializer-test-namespace");
            id174_LineTotal = Reader.NameTable.Add(@"LineTotal");
            id40_Brass = Reader.NameTable.Add(@"Brass");
            id231_Parameters = Reader.NameTable.Add(@"Parameters");
            id4_TypeWithBinaryProperty = Reader.NameTable.Add(@"TypeWithBinaryProperty");
            id17_Dog = Reader.NameTable.Add(@"Dog");
            id182_Today = Reader.NameTable.Add(@"Today");
            id2_Item = Reader.NameTable.Add(@"");
            id195_TimeSpanProperty = Reader.NameTable.Add(@"TimeSpanProperty");
            id219_Y = Reader.NameTable.Add(@"Y");
            id184_DefaultDate = Reader.NameTable.Add(@"DefaultDate");
            id273_XmlIncludeProperty = Reader.NameTable.Add(@"XmlIncludeProperty");
            id50_RootClass = Reader.NameTable.Add(@"RootClass");
            id123_TypeWithFieldsOrdered = Reader.NameTable.Add(@"TypeWithFieldsOrdered");
            id259_IsLoaded = Reader.NameTable.Add(@"IsLoaded");
            id285_NoneSchemaFormListProperty = Reader.NameTable.Add(@"NoneSchemaFormListProperty");
            id172_UnitPrice = Reader.NameTable.Add(@"UnitPrice");
            id42_Pet = Reader.NameTable.Add(@"Pet");
            id80_Item = Reader.NameTable.Add(@"TypeWithXmlTextAttributeOnArray");
            id260_Some = Reader.NameTable.Add(@"Some");
            id157_httpmynamespace = Reader.NameTable.Add(@"http://mynamespace");
            id281_StrProperty = Reader.NameTable.Add(@"StrProperty");
            id125_Root = Reader.NameTable.Add(@"Root");
            id208_Line1 = Reader.NameTable.Add(@"Line1");
            id227_FloatProp = Reader.NameTable.Add(@"FloatProp");
            id304_NMTOKENContent = Reader.NameTable.Add(@"NMTOKENContent");
            id280_SimpleTypeValue = Reader.NameTable.Add(@"SimpleTypeValue");
            id187_NullableDefaultDate = Reader.NameTable.Add(@"NullableDefaultDate");
            id189_MyTime = Reader.NameTable.Add(@"MyTime");
            id135_B = Reader.NameTable.Add(@"B");
            id238_IntAField = Reader.NameTable.Add(@"IntAField");
            id161_QualifiedParameter = Reader.NameTable.Add(@"QualifiedParameter");
            id169_Modulation = Reader.NameTable.Add(@"Modulation");
            id54_XElementArrayWrapper = Reader.NameTable.Add(@"XElementArrayWrapper");
            id173_Quantity = Reader.NameTable.Add(@"Quantity");
            id267_Struct1 = Reader.NameTable.Add(@"Struct1");
            id163_P1 = Reader.NameTable.Add(@"P1");
            id232_xelement = Reader.NameTable.Add(@"xelement");
            id199_Breed = Reader.NameTable.Add(@"Breed");
            id247_Data = Reader.NameTable.Add(@"Data");
            id254_DateTimeProperty = Reader.NameTable.Add(@"DateTimeProperty");
            id233_xelements = Reader.NameTable.Add(@"xelements");
            id137_Value2 = Reader.NameTable.Add(@"Value2");
            id53_XElementStruct = Reader.NameTable.Add(@"XElementStruct");
            id177_DTO = Reader.NameTable.Add(@"DTO");
            id139_ParameterOfString = Reader.NameTable.Add(@"ParameterOfString");
            id90_ShortEnum = Reader.NameTable.Add(@"ShortEnum");
            id34_BaseClass1 = Reader.NameTable.Add(@"BaseClass1");
            id52_XElementWrapper = Reader.NameTable.Add(@"XElementWrapper");
            id7_DateOnlyWrapper = Reader.NameTable.Add(@"DateOnlyWrapper");
            id59_TypeWithArraylikeMembers = Reader.NameTable.Add(@"TypeWithArraylikeMembers");
            id66_TypeWithEnumMembers = Reader.NameTable.Add(@"TypeWithEnumMembers");
            id68_DCClassWithEnumAndStruct = Reader.NameTable.Add(@"DCClassWithEnumAndStruct");
            id15_TypeWithXmlNodeArrayProperty = Reader.NameTable.Add(@"TypeWithXmlNodeArrayProperty");
            id22_BaseClass = Reader.NameTable.Add(@"BaseClass");
            id272_DecimalNumber = Reader.NameTable.Add(@"DecimalNumber");
            id299_DateTimeContent = Reader.NameTable.Add(@"DateTimeContent");
            id91_IntEnum = Reader.NameTable.Add(@"IntEnum");
            id112_Item = Reader.NameTable.Add(@"TypeWithEnumPropertyHavingDefaultValue");
            id239_NIntAField = Reader.NameTable.Add(@"NIntAField");
            id110_TypeWith2DArrayProperty2 = Reader.NameTable.Add(@"TypeWith2DArrayProperty2");
            id240_IntLField = Reader.NameTable.Add(@"IntLField");
            id5_Item = Reader.NameTable.Add(@"TypeWithDateTimeOffsetProperties");
            id274_XmlEnumProperty = Reader.NameTable.Add(@"XmlEnumProperty");
            id291_MetricConfigUrl = Reader.NameTable.Add(@"MetricConfigUrl");
            id244_IntLProp = Reader.NameTable.Add(@"IntLProp");
            id29_PurchaseOrder = Reader.NameTable.Add(@"PurchaseOrder");
            id115_Item = Reader.NameTable.Add(@"KnownTypesThroughConstructorWithArrayProperties");
            id36_ArrayOfDateTime = Reader.NameTable.Add(@"ArrayOfDateTime");
            id313_TestProperty = Reader.NameTable.Add(@"TestProperty");
            id88_ByteEnum = Reader.NameTable.Add(@"ByteEnum");
            id84_WithStruct = Reader.NameTable.Add(@"WithStruct");
            id282_MyField = Reader.NameTable.Add(@"MyField");
            id216_ShipCost = Reader.NameTable.Add(@"ShipCost");
            id245_NIntLProp = Reader.NameTable.Add(@"NIntLProp");
            id279_EnumValue = Reader.NameTable.Add(@"EnumValue");
            id127_TypeClashA = Reader.NameTable.Add(@"TypeClashA");
            id38_Orchestra = Reader.NameTable.Add(@"Orchestra");
            id309_Amount = Reader.NameTable.Add(@"Amount");
            id13_Item = Reader.NameTable.Add(@"TypeWithDefaultTimeSpanProperty");
            id224_DoubleField = Reader.NameTable.Add(@"DoubleField");
            id39_Instrument = Reader.NameTable.Add(@"Instrument");
            id94_ULongEnum = Reader.NameTable.Add(@"ULongEnum");
            id200_LicenseNumber = Reader.NameTable.Add(@"LicenseNumber");
            id300_QNameContent = Reader.NameTable.Add(@"QNameContent");
            id107_Item = Reader.NameTable.Add(@"TypeWithNonPublicDefaultConstructor");
            id241_NIntLField = Reader.NameTable.Add(@"NIntLField");
            id166_Child = Reader.NameTable.Add(@"Child");
            id270_Word = Reader.NameTable.Add(@"Word");
            id207_BoolValue = Reader.NameTable.Add(@"BoolValue");
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
            ((XmlSerializationWriter1)writer).Write121_TypeWithXmlElementProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read127_TypeWithXmlElementProperty();
        }
    }

    public sealed class TypeWithXmlDocumentPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithXmlDocumentProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write122_TypeWithXmlDocumentProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read128_TypeWithXmlDocumentProperty();
        }
    }

    public sealed class TypeWithBinaryPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithBinaryProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write123_TypeWithBinaryProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read129_TypeWithBinaryProperty();
        }
    }

    public sealed class TypeWithDateTimeOffsetPropertiesSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithDateTimeOffsetProperties", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write124_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read130_Item();
        }
    }

    public sealed class TypeWithDateAndTimeOnlyPropertiesSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithDateAndTimeOnlyProperties", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write125_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read131_Item();
        }
    }

    public sealed class DateOnlyWrapperSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DateOnlyWrapper", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write126_DateOnlyWrapper(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read132_DateOnlyWrapper();
        }
    }

    public sealed class TimeOnlyWrapperSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TimeOnlyWrapper", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write127_TimeOnlyWrapper(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read133_TimeOnlyWrapper();
        }
    }

    public sealed class TimeOnlyAsXsdTimeWrapperSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TimeOnlyAsXsdTimeWrapper", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write128_TimeOnlyAsXsdTimeWrapper(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read134_TimeOnlyAsXsdTimeWrapper();
        }
    }

    public sealed class DateTimeDateWrapperSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DateTimeDateWrapper", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write129_DateTimeDateWrapper(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read135_DateTimeDateWrapper();
        }
    }

    public sealed class DateTimeTimeWrapperSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DateTimeTimeWrapper", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write130_DateTimeTimeWrapper(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read136_DateTimeTimeWrapper();
        }
    }

    public sealed class TypeWithTimeSpanPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithTimeSpanProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write131_TypeWithTimeSpanProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read137_TypeWithTimeSpanProperty();
        }
    }

    public sealed class TypeWithDefaultTimeSpanPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithDefaultTimeSpanProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write132_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read138_Item();
        }
    }

    public sealed class TypeWithBytePropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithByteProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write133_TypeWithByteProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read139_TypeWithByteProperty();
        }
    }

    public sealed class TypeWithXmlNodeArrayPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithXmlNodeArrayProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write134_TypeWithXmlNodeArrayProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read140_TypeWithXmlNodeArrayProperty();
        }
    }

    public sealed class AnimalSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Animal", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write135_Animal(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read141_Animal();
        }
    }

    public sealed class DogSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Dog", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write136_Dog(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read142_Dog();
        }
    }

    public sealed class DogBreedSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DogBreed", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write137_DogBreed(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read143_DogBreed();
        }
    }

    public sealed class GroupSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Group", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write138_Group(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read144_Group();
        }
    }

    public sealed class VehicleSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Vehicle", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write139_Vehicle(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read145_Vehicle();
        }
    }

    public sealed class EmployeeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Employee", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write140_Employee(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read146_Employee();
        }
    }

    public sealed class BaseClassSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"BaseClass", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write141_BaseClass(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read147_BaseClass();
        }
    }

    public sealed class DerivedClassSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DerivedClass", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write142_DerivedClass(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read148_DerivedClass();
        }
    }

    public sealed class SimpleBaseClassSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"SimpleBaseClass", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write143_SimpleBaseClass(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read149_SimpleBaseClass();
        }
    }

    public sealed class SimpleDerivedClassSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"SimpleDerivedClass", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write144_SimpleDerivedClass(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read150_SimpleDerivedClass();
        }
    }

    public sealed class XmlSerializableBaseClassSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"BaseIXmlSerializable", @"http://example.com/serializer-test-namespace");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write145_BaseIXmlSerializable(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read151_BaseIXmlSerializable();
        }
    }

    public sealed class XmlSerializableDerivedClassSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DerivedIXmlSerializable", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write146_DerivedIXmlSerializable(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read152_DerivedIXmlSerializable();
        }
    }

    public sealed class PurchaseOrderSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"PurchaseOrder", @"http://www.contoso1.com");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write147_PurchaseOrder(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read153_PurchaseOrder();
        }
    }

    public sealed class AddressSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Address", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write148_Address(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read154_Address();
        }
    }

    public sealed class OrderedItemSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"OrderedItem", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write149_OrderedItem(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read155_OrderedItem();
        }
    }

    public sealed class AliasedTestTypeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"AliasedTestType", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write150_AliasedTestType(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read156_AliasedTestType();
        }
    }

    public sealed class BaseClass1Serializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"BaseClass1", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write151_BaseClass1(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read157_BaseClass1();
        }
    }

    public sealed class DerivedClass1Serializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DerivedClass1", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write152_DerivedClass1(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read158_DerivedClass1();
        }
    }

    public sealed class MyCollection1Serializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ArrayOfDateTime", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write153_ArrayOfDateTime(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read159_ArrayOfDateTime();
        }
    }

    public sealed class OrchestraSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Orchestra", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write154_Orchestra(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read160_Orchestra();
        }
    }

    public sealed class InstrumentSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Instrument", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write155_Instrument(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read161_Instrument();
        }
    }

    public sealed class BrassSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Brass", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write156_Brass(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read162_Brass();
        }
    }

    public sealed class TrumpetSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Trumpet", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write157_Trumpet(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read163_Trumpet();
        }
    }

    public sealed class PetSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Pet", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write158_Pet(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read164_Pet();
        }
    }

    public sealed class DefaultValuesSetToNaNSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DefaultValuesSetToNaN", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write159_DefaultValuesSetToNaN(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read165_DefaultValuesSetToNaN();
        }
    }

    public sealed class DefaultValuesSetToPositiveInfinitySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DefaultValuesSetToPositiveInfinity", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write160_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read166_Item();
        }
    }

    public sealed class DefaultValuesSetToNegativeInfinitySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DefaultValuesSetToNegativeInfinity", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write161_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read167_Item();
        }
    }

    public sealed class TypeWithMismatchBetweenAttributeAndPropertyTypeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"RootElement", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write162_RootElement(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read168_RootElement();
        }
    }

    public sealed class TypeWithLinkedPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithLinkedProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write163_TypeWithLinkedProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read169_TypeWithLinkedProperty();
        }
    }

    public sealed class MsgDocumentTypeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Document", @"http://example.com");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write164_Document(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read170_Document();
        }
    }

    public sealed class RootClassSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"RootClass", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write165_RootClass(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read171_RootClass();
        }
    }

    public sealed class ParameterSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Parameter", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write166_Parameter(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read172_Parameter();
        }
    }

    public sealed class XElementWrapperSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"XElementWrapper", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write167_XElementWrapper(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read173_XElementWrapper();
        }
    }

    public sealed class XElementStructSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"XElementStruct", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write168_XElementStruct(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read174_XElementStruct();
        }
    }

    public sealed class XElementArrayWrapperSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"XElementArrayWrapper", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write169_XElementArrayWrapper(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read175_XElementArrayWrapper();
        }
    }

    public sealed class TypeWithDateTimeStringPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithDateTimeStringProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write170_TypeWithDateTimeStringProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read176_TypeWithDateTimeStringProperty();
        }
    }

    public sealed class SimpleTypeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"SimpleType", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write171_SimpleType(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read177_SimpleType();
        }
    }

    public sealed class TypeWithGetSetArrayMembersSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithGetSetArrayMembers", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write172_TypeWithGetSetArrayMembers(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read178_TypeWithGetSetArrayMembers();
        }
    }

    public sealed class TypeWithGetOnlyArrayPropertiesSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithGetOnlyArrayProperties", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write173_TypeWithGetOnlyArrayProperties(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read179_TypeWithGetOnlyArrayProperties();
        }
    }

    public sealed class TypeWithArraylikeMembersSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithArraylikeMembers", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write174_TypeWithArraylikeMembers(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read180_TypeWithArraylikeMembers();
        }
    }

    public sealed class StructNotSerializableSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"StructNotSerializable", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write175_StructNotSerializable(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read181_StructNotSerializable();
        }
    }

    public sealed class TypeWithMyCollectionFieldSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithMyCollectionField", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write176_TypeWithMyCollectionField(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read182_TypeWithMyCollectionField();
        }
    }

    public sealed class TypeWithReadOnlyMyCollectionPropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithReadOnlyMyCollectionProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write177_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read183_Item();
        }
    }

    public sealed class MyListSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ArrayOfAnyType", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write178_ArrayOfAnyType(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read184_ArrayOfAnyType();
        }
    }

    public sealed class MyEnumSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"MyEnum", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write179_MyEnum(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read185_MyEnum();
        }
    }

    public sealed class TypeWithEnumMembersSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithEnumMembers", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write180_TypeWithEnumMembers(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read186_TypeWithEnumMembers();
        }
    }

    public sealed class DCStructSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DCStruct", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write181_DCStruct(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read187_DCStruct();
        }
    }

    public sealed class DCClassWithEnumAndStructSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DCClassWithEnumAndStruct", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write182_DCClassWithEnumAndStruct(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read188_DCClassWithEnumAndStruct();
        }
    }

    public sealed class BuiltInTypesSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"BuiltInTypes", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write183_BuiltInTypes(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read189_BuiltInTypes();
        }
    }

    public sealed class TypeASerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeA", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write184_TypeA(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read190_TypeA();
        }
    }

    public sealed class TypeBSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeB", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write185_TypeB(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read191_TypeB();
        }
    }

    public sealed class TypeHasArrayOfASerializedAsBSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeHasArrayOfASerializedAsB", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write186_TypeHasArrayOfASerializedAsB(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read192_TypeHasArrayOfASerializedAsB();
        }
    }

    public sealed class @__TypeNameWithSpecialCharacters漢ñSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"__TypeNameWithSpecialCharacters漢ñ", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write187_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read193_Item();
        }
    }

    public sealed class BaseClassWithSamePropertyNameSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"BaseClassWithSamePropertyName", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write188_BaseClassWithSamePropertyName(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read194_BaseClassWithSamePropertyName();
        }
    }

    public sealed class DerivedClassWithSamePropertySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DerivedClassWithSameProperty", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write189_DerivedClassWithSameProperty(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read195_DerivedClassWithSameProperty();
        }
    }

    public sealed class DerivedClassWithSameProperty2Serializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"DerivedClassWithSameProperty2", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write190_DerivedClassWithSameProperty2(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read196_DerivedClassWithSameProperty2();
        }
    }

    public sealed class TypeWithDateTimePropertyAsXmlTimeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithDateTimePropertyAsXmlTime", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write191_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read197_Item();
        }
    }

    public sealed class TypeWithByteArrayAsXmlTextSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithByteArrayAsXmlText", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write192_TypeWithByteArrayAsXmlText(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read198_TypeWithByteArrayAsXmlText();
        }
    }

    public sealed class SimpleDCSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"SimpleDC", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write193_SimpleDC(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read199_SimpleDC();
        }
    }

    public sealed class TypeWithXmlTextAttributeOnArraySerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithXmlTextAttributeOnArray", @"http://schemas.xmlsoap.org/ws/2005/04/discovery");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write194_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read200_Item();
        }
    }

    public sealed class EnumFlagsSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"EnumFlags", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write195_EnumFlags(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read201_EnumFlags();
        }
    }

    public sealed class ClassImplementsInterfaceSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ClassImplementsInterface", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write196_ClassImplementsInterface(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read202_ClassImplementsInterface();
        }
    }

    public sealed class WithStructSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"WithStruct", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write197_WithStruct(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read203_WithStruct();
        }
    }

    public sealed class SomeStructSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"SomeStruct", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write198_SomeStruct(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read204_SomeStruct();
        }
    }

    public sealed class WithEnumsSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"WithEnums", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write199_WithEnums(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read205_WithEnums();
        }
    }

    public sealed class WithNullablesSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"WithNullables", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write200_WithNullables(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read206_WithNullables();
        }
    }

    public sealed class ByteEnumSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ByteEnum", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write201_ByteEnum(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read207_ByteEnum();
        }
    }

    public sealed class SByteEnumSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"SByteEnum", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write202_SByteEnum(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read208_SByteEnum();
        }
    }

    public sealed class ShortEnumSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ShortEnum", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write203_ShortEnum(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read209_ShortEnum();
        }
    }

    public sealed class IntEnumSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"IntEnum", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write204_IntEnum(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read210_IntEnum();
        }
    }

    public sealed class UIntEnumSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"UIntEnum", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write205_UIntEnum(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read211_UIntEnum();
        }
    }

    public sealed class LongEnumSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"LongEnum", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write206_LongEnum(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read212_LongEnum();
        }
    }

    public sealed class ULongEnumSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ULongEnum", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write207_ULongEnum(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read213_ULongEnum();
        }
    }

    public sealed class XmlSerializerAttributesSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"AttributeTesting", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write208_AttributeTesting(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read214_AttributeTesting();
        }
    }

    public sealed class ItemChoiceTypeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ItemChoiceType", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write209_ItemChoiceType(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read215_ItemChoiceType();
        }
    }

    public sealed class TypeWithAnyAttributeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithAnyAttribute", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write210_TypeWithAnyAttribute(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read216_TypeWithAnyAttribute();
        }
    }

    public sealed class KnownTypesThroughConstructorSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"KnownTypesThroughConstructor", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write211_KnownTypesThroughConstructor(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read217_KnownTypesThroughConstructor();
        }
    }

    public sealed class SimpleKnownTypeValueSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"SimpleKnownTypeValue", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write212_SimpleKnownTypeValue(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read218_SimpleKnownTypeValue();
        }
    }

    public sealed class ClassImplementingIXmlSerializableSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ClassImplementingIXmlSerializable", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write213_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read219_Item();
        }
    }

    public sealed class TypeWithPropertyNameSpecifiedSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithPropertyNameSpecified", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write214_TypeWithPropertyNameSpecified(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read220_TypeWithPropertyNameSpecified();
        }
    }

    public sealed class TypeWithXmlSchemaFormAttributeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithXmlSchemaFormAttribute", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write215_TypeWithXmlSchemaFormAttribute(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read221_TypeWithXmlSchemaFormAttribute();
        }
    }

    public sealed class TypeWithTypeNameInXmlTypeAttributeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"MyXmlType", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write216_MyXmlType(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read222_MyXmlType();
        }
    }

    public sealed class TypeWithSchemaFormInXmlAttributeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithSchemaFormInXmlAttribute", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write217_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read223_Item();
        }
    }

    public sealed class CustomDocumentSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"CustomDocument", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write218_CustomDocument(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read224_CustomDocument();
        }
    }

    public sealed class CustomElementSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"CustomElement", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write219_CustomElement(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read225_CustomElement();
        }
    }

    public sealed class CustomAttributeSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return true;
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write220_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read226_Item();
        }
    }

    public sealed class TypeWithNonPublicDefaultConstructorSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithNonPublicDefaultConstructor", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write221_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read227_Item();
        }
    }

    public sealed class ServerSettingsSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ServerSettings", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write222_ServerSettings(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read228_ServerSettings();
        }
    }

    public sealed class TypeWithXmlQualifiedNameSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithXmlQualifiedName", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write223_TypeWithXmlQualifiedName(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read229_TypeWithXmlQualifiedName();
        }
    }

    public sealed class TypeWith2DArrayProperty2Serializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWith2DArrayProperty2", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write224_TypeWith2DArrayProperty2(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read230_TypeWith2DArrayProperty2();
        }
    }

    public sealed class TypeWithPropertiesHavingDefaultValueSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithPropertiesHavingDefaultValue", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write225_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read231_Item();
        }
    }

    public sealed class TypeWithEnumPropertyHavingDefaultValueSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithEnumPropertyHavingDefaultValue", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write226_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read232_Item();
        }
    }

    public sealed class TypeWithEnumFlagPropertyHavingDefaultValueSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithEnumFlagPropertyHavingDefaultValue", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write227_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read233_Item();
        }
    }

    public sealed class TypeWithShouldSerializeMethodSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithShouldSerializeMethod", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write228_TypeWithShouldSerializeMethod(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read234_TypeWithShouldSerializeMethod();
        }
    }

    public sealed class KnownTypesThroughConstructorWithArrayPropertiesSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"KnownTypesThroughConstructorWithArrayProperties", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write229_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read235_Item();
        }
    }

    public sealed class KnownTypesThroughConstructorWithValueSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"KnownTypesThroughConstructorWithValue", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write230_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read236_Item();
        }
    }

    public sealed class TypeWithTypesHavingCustomFormatterSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithTypesHavingCustomFormatter", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write231_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read237_Item();
        }
    }

    public sealed class TypeWithArrayPropertyHavingChoiceSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithArrayPropertyHavingChoice", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write232_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read238_Item();
        }
    }

    public sealed class TypeWithPropertyHavingComplexChoiceSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithPropertyHavingComplexChoice", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write233_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read239_Item();
        }
    }

    public sealed class MoreChoicesSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"MoreChoices", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write234_MoreChoices(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read240_MoreChoices();
        }
    }

    public sealed class ComplexChoiceASerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ComplexChoiceA", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write235_ComplexChoiceA(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read241_ComplexChoiceA();
        }
    }

    public sealed class ComplexChoiceBSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"ComplexChoiceB", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write236_ComplexChoiceB(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read242_ComplexChoiceB();
        }
    }

    public sealed class TypeWithFieldsOrderedSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithFieldsOrdered", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write237_TypeWithFieldsOrdered(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read243_TypeWithFieldsOrdered();
        }
    }

    public sealed class TypeWithKnownTypesOfCollectionsWithConflictingXmlNameSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeWithKnownTypesOfCollectionsWithConflictingXmlName", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write238_Item(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read244_Item();
        }
    }

    public sealed class NamespaceTypeNameClashContainerSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Root", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write239_Root(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read245_Root();
        }
    }

    public sealed class TypeNameClashSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeClashB", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write240_TypeClashB(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read246_TypeClashB();
        }
    }

    public sealed class TypeNameClashSerializer1 : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"TypeClashA", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write241_TypeClashA(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read247_TypeClashA();
        }
    }

    public sealed class PersonSerializer : XmlSerializer1 {

        public override System.Boolean CanDeserialize(System.Xml.XmlReader xmlReader) {
            return xmlReader.IsStartElement(@"Person", @"");
        }

        protected override void Serialize(object objectToSerialize, System.Xml.Serialization.XmlSerializationWriter writer) {
            ((XmlSerializationWriter1)writer).Write242_Person(objectToSerialize);
        }

        protected override object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) {
            return ((XmlSerializationReader1)reader).Read248_Person();
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
                    _tmp[@"TypeWithXmlElementProperty::"] = @"Read127_TypeWithXmlElementProperty";
                    _tmp[@"TypeWithXmlDocumentProperty::"] = @"Read128_TypeWithXmlDocumentProperty";
                    _tmp[@"TypeWithBinaryProperty::"] = @"Read129_TypeWithBinaryProperty";
                    _tmp[@"TypeWithDateTimeOffsetProperties::"] = @"Read130_Item";
                    _tmp[@"TypeWithDateAndTimeOnlyProperties::"] = @"Read131_Item";
                    _tmp[@"DateOnlyWrapper::"] = @"Read132_DateOnlyWrapper";
                    _tmp[@"TimeOnlyWrapper::"] = @"Read133_TimeOnlyWrapper";
                    _tmp[@"TimeOnlyAsXsdTimeWrapper::"] = @"Read134_TimeOnlyAsXsdTimeWrapper";
                    _tmp[@"DateTimeDateWrapper::"] = @"Read135_DateTimeDateWrapper";
                    _tmp[@"DateTimeTimeWrapper::"] = @"Read136_DateTimeTimeWrapper";
                    _tmp[@"TypeWithTimeSpanProperty::"] = @"Read137_TypeWithTimeSpanProperty";
                    _tmp[@"TypeWithDefaultTimeSpanProperty::"] = @"Read138_Item";
                    _tmp[@"TypeWithByteProperty::"] = @"Read139_TypeWithByteProperty";
                    _tmp[@"TypeWithXmlNodeArrayProperty:::True:"] = @"Read140_TypeWithXmlNodeArrayProperty";
                    _tmp[@"Animal::"] = @"Read141_Animal";
                    _tmp[@"Dog::"] = @"Read142_Dog";
                    _tmp[@"DogBreed::"] = @"Read143_DogBreed";
                    _tmp[@"Group::"] = @"Read144_Group";
                    _tmp[@"Vehicle::"] = @"Read145_Vehicle";
                    _tmp[@"Employee::"] = @"Read146_Employee";
                    _tmp[@"BaseClass::"] = @"Read147_BaseClass";
                    _tmp[@"DerivedClass::"] = @"Read148_DerivedClass";
                    _tmp[@"SimpleBaseClass::"] = @"Read149_SimpleBaseClass";
                    _tmp[@"SimpleDerivedClass::"] = @"Read150_SimpleDerivedClass";
                    _tmp[@"XmlSerializableBaseClass:http://example.com/serializer-test-namespace::True:"] = @"Read151_BaseIXmlSerializable";
                    _tmp[@"XmlSerializableDerivedClass::"] = @"Read152_DerivedIXmlSerializable";
                    _tmp[@"PurchaseOrder:http://www.contoso1.com:PurchaseOrder:False:"] = @"Read153_PurchaseOrder";
                    _tmp[@"Address::"] = @"Read154_Address";
                    _tmp[@"OrderedItem::"] = @"Read155_OrderedItem";
                    _tmp[@"AliasedTestType::"] = @"Read156_AliasedTestType";
                    _tmp[@"BaseClass1::"] = @"Read157_BaseClass1";
                    _tmp[@"DerivedClass1::"] = @"Read158_DerivedClass1";
                    _tmp[@"MyCollection1::"] = @"Read159_ArrayOfDateTime";
                    _tmp[@"Orchestra::"] = @"Read160_Orchestra";
                    _tmp[@"Instrument::"] = @"Read161_Instrument";
                    _tmp[@"Brass::"] = @"Read162_Brass";
                    _tmp[@"Trumpet::"] = @"Read163_Trumpet";
                    _tmp[@"Pet::"] = @"Read164_Pet";
                    _tmp[@"DefaultValuesSetToNaN::"] = @"Read165_DefaultValuesSetToNaN";
                    _tmp[@"DefaultValuesSetToPositiveInfinity::"] = @"Read166_Item";
                    _tmp[@"DefaultValuesSetToNegativeInfinity::"] = @"Read167_Item";
                    _tmp[@"TypeWithMismatchBetweenAttributeAndPropertyType::RootElement:True:"] = @"Read168_RootElement";
                    _tmp[@"TypeWithLinkedProperty::"] = @"Read169_TypeWithLinkedProperty";
                    _tmp[@"MsgDocumentType:http://example.com:Document:True:"] = @"Read170_Document";
                    _tmp[@"RootClass::"] = @"Read171_RootClass";
                    _tmp[@"Parameter::"] = @"Read172_Parameter";
                    _tmp[@"XElementWrapper::"] = @"Read173_XElementWrapper";
                    _tmp[@"XElementStruct::"] = @"Read174_XElementStruct";
                    _tmp[@"XElementArrayWrapper::"] = @"Read175_XElementArrayWrapper";
                    _tmp[@"SerializationTypes.TypeWithDateTimeStringProperty::"] = @"Read176_TypeWithDateTimeStringProperty";
                    _tmp[@"SerializationTypes.SimpleType::"] = @"Read177_SimpleType";
                    _tmp[@"SerializationTypes.TypeWithGetSetArrayMembers::"] = @"Read178_TypeWithGetSetArrayMembers";
                    _tmp[@"SerializationTypes.TypeWithGetOnlyArrayProperties::"] = @"Read179_TypeWithGetOnlyArrayProperties";
                    _tmp[@"SerializationTypes.TypeWithArraylikeMembers::"] = @"Read180_TypeWithArraylikeMembers";
                    _tmp[@"SerializationTypes.StructNotSerializable::"] = @"Read181_StructNotSerializable";
                    _tmp[@"SerializationTypes.TypeWithMyCollectionField::"] = @"Read182_TypeWithMyCollectionField";
                    _tmp[@"SerializationTypes.TypeWithReadOnlyMyCollectionProperty::"] = @"Read183_Item";
                    _tmp[@"SerializationTypes.MyList::"] = @"Read184_ArrayOfAnyType";
                    _tmp[@"SerializationTypes.MyEnum::"] = @"Read185_MyEnum";
                    _tmp[@"SerializationTypes.TypeWithEnumMembers::"] = @"Read186_TypeWithEnumMembers";
                    _tmp[@"SerializationTypes.DCStruct::"] = @"Read187_DCStruct";
                    _tmp[@"SerializationTypes.DCClassWithEnumAndStruct::"] = @"Read188_DCClassWithEnumAndStruct";
                    _tmp[@"SerializationTypes.BuiltInTypes::"] = @"Read189_BuiltInTypes";
                    _tmp[@"SerializationTypes.TypeA::"] = @"Read190_TypeA";
                    _tmp[@"SerializationTypes.TypeB::"] = @"Read191_TypeB";
                    _tmp[@"SerializationTypes.TypeHasArrayOfASerializedAsB::"] = @"Read192_TypeHasArrayOfASerializedAsB";
                    _tmp[@"SerializationTypes.__TypeNameWithSpecialCharacters漢ñ::"] = @"Read193_Item";
                    _tmp[@"SerializationTypes.BaseClassWithSamePropertyName::"] = @"Read194_BaseClassWithSamePropertyName";
                    _tmp[@"SerializationTypes.DerivedClassWithSameProperty::"] = @"Read195_DerivedClassWithSameProperty";
                    _tmp[@"SerializationTypes.DerivedClassWithSameProperty2::"] = @"Read196_DerivedClassWithSameProperty2";
                    _tmp[@"SerializationTypes.TypeWithDateTimePropertyAsXmlTime::"] = @"Read197_Item";
                    _tmp[@"SerializationTypes.TypeWithByteArrayAsXmlText::"] = @"Read198_TypeWithByteArrayAsXmlText";
                    _tmp[@"SerializationTypes.SimpleDC::"] = @"Read199_SimpleDC";
                    _tmp[@"SerializationTypes.TypeWithXmlTextAttributeOnArray:http://schemas.xmlsoap.org/ws/2005/04/discovery::False:"] = @"Read200_Item";
                    _tmp[@"SerializationTypes.EnumFlags::"] = @"Read201_EnumFlags";
                    _tmp[@"SerializationTypes.ClassImplementsInterface::"] = @"Read202_ClassImplementsInterface";
                    _tmp[@"SerializationTypes.WithStruct::"] = @"Read203_WithStruct";
                    _tmp[@"SerializationTypes.SomeStruct::"] = @"Read204_SomeStruct";
                    _tmp[@"SerializationTypes.WithEnums::"] = @"Read205_WithEnums";
                    _tmp[@"SerializationTypes.WithNullables::"] = @"Read206_WithNullables";
                    _tmp[@"SerializationTypes.ByteEnum::"] = @"Read207_ByteEnum";
                    _tmp[@"SerializationTypes.SByteEnum::"] = @"Read208_SByteEnum";
                    _tmp[@"SerializationTypes.ShortEnum::"] = @"Read209_ShortEnum";
                    _tmp[@"SerializationTypes.IntEnum::"] = @"Read210_IntEnum";
                    _tmp[@"SerializationTypes.UIntEnum::"] = @"Read211_UIntEnum";
                    _tmp[@"SerializationTypes.LongEnum::"] = @"Read212_LongEnum";
                    _tmp[@"SerializationTypes.ULongEnum::"] = @"Read213_ULongEnum";
                    _tmp[@"SerializationTypes.XmlSerializerAttributes::AttributeTesting:False:"] = @"Read214_AttributeTesting";
                    _tmp[@"SerializationTypes.ItemChoiceType::"] = @"Read215_ItemChoiceType";
                    _tmp[@"SerializationTypes.TypeWithAnyAttribute::"] = @"Read216_TypeWithAnyAttribute";
                    _tmp[@"SerializationTypes.KnownTypesThroughConstructor::"] = @"Read217_KnownTypesThroughConstructor";
                    _tmp[@"SerializationTypes.SimpleKnownTypeValue::"] = @"Read218_SimpleKnownTypeValue";
                    _tmp[@"SerializationTypes.ClassImplementingIXmlSerializable::"] = @"Read219_Item";
                    _tmp[@"SerializationTypes.TypeWithPropertyNameSpecified::"] = @"Read220_TypeWithPropertyNameSpecified";
                    _tmp[@"SerializationTypes.TypeWithXmlSchemaFormAttribute:::True:"] = @"Read221_TypeWithXmlSchemaFormAttribute";
                    _tmp[@"SerializationTypes.TypeWithTypeNameInXmlTypeAttribute::"] = @"Read222_MyXmlType";
                    _tmp[@"SerializationTypes.TypeWithSchemaFormInXmlAttribute::"] = @"Read223_Item";
                    _tmp[@"SerializationTypes.CustomDocument::"] = @"Read224_CustomDocument";
                    _tmp[@"SerializationTypes.CustomElement::"] = @"Read225_CustomElement";
                    _tmp[@"SerializationTypes.CustomAttribute::"] = @"Read226_Item";
                    _tmp[@"SerializationTypes.TypeWithNonPublicDefaultConstructor::"] = @"Read227_Item";
                    _tmp[@"SerializationTypes.ServerSettings::"] = @"Read228_ServerSettings";
                    _tmp[@"SerializationTypes.TypeWithXmlQualifiedName::"] = @"Read229_TypeWithXmlQualifiedName";
                    _tmp[@"SerializationTypes.TypeWith2DArrayProperty2::"] = @"Read230_TypeWith2DArrayProperty2";
                    _tmp[@"SerializationTypes.TypeWithPropertiesHavingDefaultValue::"] = @"Read231_Item";
                    _tmp[@"SerializationTypes.TypeWithEnumPropertyHavingDefaultValue::"] = @"Read232_Item";
                    _tmp[@"SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue::"] = @"Read233_Item";
                    _tmp[@"SerializationTypes.TypeWithShouldSerializeMethod::"] = @"Read234_TypeWithShouldSerializeMethod";
                    _tmp[@"SerializationTypes.KnownTypesThroughConstructorWithArrayProperties::"] = @"Read235_Item";
                    _tmp[@"SerializationTypes.KnownTypesThroughConstructorWithValue::"] = @"Read236_Item";
                    _tmp[@"SerializationTypes.TypeWithTypesHavingCustomFormatter::"] = @"Read237_Item";
                    _tmp[@"SerializationTypes.TypeWithArrayPropertyHavingChoice::"] = @"Read238_Item";
                    _tmp[@"SerializationTypes.TypeWithPropertyHavingComplexChoice::"] = @"Read239_Item";
                    _tmp[@"SerializationTypes.MoreChoices::"] = @"Read240_MoreChoices";
                    _tmp[@"SerializationTypes.ComplexChoiceA::"] = @"Read241_ComplexChoiceA";
                    _tmp[@"SerializationTypes.ComplexChoiceB::"] = @"Read242_ComplexChoiceB";
                    _tmp[@"SerializationTypes.TypeWithFieldsOrdered::"] = @"Read243_TypeWithFieldsOrdered";
                    _tmp[@"SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName::"] = @"Read244_Item";
                    _tmp[@"SerializationTypes.NamespaceTypeNameClashContainer::Root:True:"] = @"Read245_Root";
                    _tmp[@"SerializationTypes.TypeNameClashB.TypeNameClash::"] = @"Read246_TypeClashB";
                    _tmp[@"SerializationTypes.TypeNameClashA.TypeNameClash::"] = @"Read247_TypeClashA";
                    _tmp[@"Outer+Person::"] = @"Read248_Person";
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
                    _tmp[@"TypeWithXmlElementProperty::"] = @"Write121_TypeWithXmlElementProperty";
                    _tmp[@"TypeWithXmlDocumentProperty::"] = @"Write122_TypeWithXmlDocumentProperty";
                    _tmp[@"TypeWithBinaryProperty::"] = @"Write123_TypeWithBinaryProperty";
                    _tmp[@"TypeWithDateTimeOffsetProperties::"] = @"Write124_Item";
                    _tmp[@"TypeWithDateAndTimeOnlyProperties::"] = @"Write125_Item";
                    _tmp[@"DateOnlyWrapper::"] = @"Write126_DateOnlyWrapper";
                    _tmp[@"TimeOnlyWrapper::"] = @"Write127_TimeOnlyWrapper";
                    _tmp[@"TimeOnlyAsXsdTimeWrapper::"] = @"Write128_TimeOnlyAsXsdTimeWrapper";
                    _tmp[@"DateTimeDateWrapper::"] = @"Write129_DateTimeDateWrapper";
                    _tmp[@"DateTimeTimeWrapper::"] = @"Write130_DateTimeTimeWrapper";
                    _tmp[@"TypeWithTimeSpanProperty::"] = @"Write131_TypeWithTimeSpanProperty";
                    _tmp[@"TypeWithDefaultTimeSpanProperty::"] = @"Write132_Item";
                    _tmp[@"TypeWithByteProperty::"] = @"Write133_TypeWithByteProperty";
                    _tmp[@"TypeWithXmlNodeArrayProperty:::True:"] = @"Write134_TypeWithXmlNodeArrayProperty";
                    _tmp[@"Animal::"] = @"Write135_Animal";
                    _tmp[@"Dog::"] = @"Write136_Dog";
                    _tmp[@"DogBreed::"] = @"Write137_DogBreed";
                    _tmp[@"Group::"] = @"Write138_Group";
                    _tmp[@"Vehicle::"] = @"Write139_Vehicle";
                    _tmp[@"Employee::"] = @"Write140_Employee";
                    _tmp[@"BaseClass::"] = @"Write141_BaseClass";
                    _tmp[@"DerivedClass::"] = @"Write142_DerivedClass";
                    _tmp[@"SimpleBaseClass::"] = @"Write143_SimpleBaseClass";
                    _tmp[@"SimpleDerivedClass::"] = @"Write144_SimpleDerivedClass";
                    _tmp[@"XmlSerializableBaseClass:http://example.com/serializer-test-namespace::True:"] = @"Write145_BaseIXmlSerializable";
                    _tmp[@"XmlSerializableDerivedClass::"] = @"Write146_DerivedIXmlSerializable";
                    _tmp[@"PurchaseOrder:http://www.contoso1.com:PurchaseOrder:False:"] = @"Write147_PurchaseOrder";
                    _tmp[@"Address::"] = @"Write148_Address";
                    _tmp[@"OrderedItem::"] = @"Write149_OrderedItem";
                    _tmp[@"AliasedTestType::"] = @"Write150_AliasedTestType";
                    _tmp[@"BaseClass1::"] = @"Write151_BaseClass1";
                    _tmp[@"DerivedClass1::"] = @"Write152_DerivedClass1";
                    _tmp[@"MyCollection1::"] = @"Write153_ArrayOfDateTime";
                    _tmp[@"Orchestra::"] = @"Write154_Orchestra";
                    _tmp[@"Instrument::"] = @"Write155_Instrument";
                    _tmp[@"Brass::"] = @"Write156_Brass";
                    _tmp[@"Trumpet::"] = @"Write157_Trumpet";
                    _tmp[@"Pet::"] = @"Write158_Pet";
                    _tmp[@"DefaultValuesSetToNaN::"] = @"Write159_DefaultValuesSetToNaN";
                    _tmp[@"DefaultValuesSetToPositiveInfinity::"] = @"Write160_Item";
                    _tmp[@"DefaultValuesSetToNegativeInfinity::"] = @"Write161_Item";
                    _tmp[@"TypeWithMismatchBetweenAttributeAndPropertyType::RootElement:True:"] = @"Write162_RootElement";
                    _tmp[@"TypeWithLinkedProperty::"] = @"Write163_TypeWithLinkedProperty";
                    _tmp[@"MsgDocumentType:http://example.com:Document:True:"] = @"Write164_Document";
                    _tmp[@"RootClass::"] = @"Write165_RootClass";
                    _tmp[@"Parameter::"] = @"Write166_Parameter";
                    _tmp[@"XElementWrapper::"] = @"Write167_XElementWrapper";
                    _tmp[@"XElementStruct::"] = @"Write168_XElementStruct";
                    _tmp[@"XElementArrayWrapper::"] = @"Write169_XElementArrayWrapper";
                    _tmp[@"SerializationTypes.TypeWithDateTimeStringProperty::"] = @"Write170_TypeWithDateTimeStringProperty";
                    _tmp[@"SerializationTypes.SimpleType::"] = @"Write171_SimpleType";
                    _tmp[@"SerializationTypes.TypeWithGetSetArrayMembers::"] = @"Write172_TypeWithGetSetArrayMembers";
                    _tmp[@"SerializationTypes.TypeWithGetOnlyArrayProperties::"] = @"Write173_TypeWithGetOnlyArrayProperties";
                    _tmp[@"SerializationTypes.TypeWithArraylikeMembers::"] = @"Write174_TypeWithArraylikeMembers";
                    _tmp[@"SerializationTypes.StructNotSerializable::"] = @"Write175_StructNotSerializable";
                    _tmp[@"SerializationTypes.TypeWithMyCollectionField::"] = @"Write176_TypeWithMyCollectionField";
                    _tmp[@"SerializationTypes.TypeWithReadOnlyMyCollectionProperty::"] = @"Write177_Item";
                    _tmp[@"SerializationTypes.MyList::"] = @"Write178_ArrayOfAnyType";
                    _tmp[@"SerializationTypes.MyEnum::"] = @"Write179_MyEnum";
                    _tmp[@"SerializationTypes.TypeWithEnumMembers::"] = @"Write180_TypeWithEnumMembers";
                    _tmp[@"SerializationTypes.DCStruct::"] = @"Write181_DCStruct";
                    _tmp[@"SerializationTypes.DCClassWithEnumAndStruct::"] = @"Write182_DCClassWithEnumAndStruct";
                    _tmp[@"SerializationTypes.BuiltInTypes::"] = @"Write183_BuiltInTypes";
                    _tmp[@"SerializationTypes.TypeA::"] = @"Write184_TypeA";
                    _tmp[@"SerializationTypes.TypeB::"] = @"Write185_TypeB";
                    _tmp[@"SerializationTypes.TypeHasArrayOfASerializedAsB::"] = @"Write186_TypeHasArrayOfASerializedAsB";
                    _tmp[@"SerializationTypes.__TypeNameWithSpecialCharacters漢ñ::"] = @"Write187_Item";
                    _tmp[@"SerializationTypes.BaseClassWithSamePropertyName::"] = @"Write188_BaseClassWithSamePropertyName";
                    _tmp[@"SerializationTypes.DerivedClassWithSameProperty::"] = @"Write189_DerivedClassWithSameProperty";
                    _tmp[@"SerializationTypes.DerivedClassWithSameProperty2::"] = @"Write190_DerivedClassWithSameProperty2";
                    _tmp[@"SerializationTypes.TypeWithDateTimePropertyAsXmlTime::"] = @"Write191_Item";
                    _tmp[@"SerializationTypes.TypeWithByteArrayAsXmlText::"] = @"Write192_TypeWithByteArrayAsXmlText";
                    _tmp[@"SerializationTypes.SimpleDC::"] = @"Write193_SimpleDC";
                    _tmp[@"SerializationTypes.TypeWithXmlTextAttributeOnArray:http://schemas.xmlsoap.org/ws/2005/04/discovery::False:"] = @"Write194_Item";
                    _tmp[@"SerializationTypes.EnumFlags::"] = @"Write195_EnumFlags";
                    _tmp[@"SerializationTypes.ClassImplementsInterface::"] = @"Write196_ClassImplementsInterface";
                    _tmp[@"SerializationTypes.WithStruct::"] = @"Write197_WithStruct";
                    _tmp[@"SerializationTypes.SomeStruct::"] = @"Write198_SomeStruct";
                    _tmp[@"SerializationTypes.WithEnums::"] = @"Write199_WithEnums";
                    _tmp[@"SerializationTypes.WithNullables::"] = @"Write200_WithNullables";
                    _tmp[@"SerializationTypes.ByteEnum::"] = @"Write201_ByteEnum";
                    _tmp[@"SerializationTypes.SByteEnum::"] = @"Write202_SByteEnum";
                    _tmp[@"SerializationTypes.ShortEnum::"] = @"Write203_ShortEnum";
                    _tmp[@"SerializationTypes.IntEnum::"] = @"Write204_IntEnum";
                    _tmp[@"SerializationTypes.UIntEnum::"] = @"Write205_UIntEnum";
                    _tmp[@"SerializationTypes.LongEnum::"] = @"Write206_LongEnum";
                    _tmp[@"SerializationTypes.ULongEnum::"] = @"Write207_ULongEnum";
                    _tmp[@"SerializationTypes.XmlSerializerAttributes::AttributeTesting:False:"] = @"Write208_AttributeTesting";
                    _tmp[@"SerializationTypes.ItemChoiceType::"] = @"Write209_ItemChoiceType";
                    _tmp[@"SerializationTypes.TypeWithAnyAttribute::"] = @"Write210_TypeWithAnyAttribute";
                    _tmp[@"SerializationTypes.KnownTypesThroughConstructor::"] = @"Write211_KnownTypesThroughConstructor";
                    _tmp[@"SerializationTypes.SimpleKnownTypeValue::"] = @"Write212_SimpleKnownTypeValue";
                    _tmp[@"SerializationTypes.ClassImplementingIXmlSerializable::"] = @"Write213_Item";
                    _tmp[@"SerializationTypes.TypeWithPropertyNameSpecified::"] = @"Write214_TypeWithPropertyNameSpecified";
                    _tmp[@"SerializationTypes.TypeWithXmlSchemaFormAttribute:::True:"] = @"Write215_TypeWithXmlSchemaFormAttribute";
                    _tmp[@"SerializationTypes.TypeWithTypeNameInXmlTypeAttribute::"] = @"Write216_MyXmlType";
                    _tmp[@"SerializationTypes.TypeWithSchemaFormInXmlAttribute::"] = @"Write217_Item";
                    _tmp[@"SerializationTypes.CustomDocument::"] = @"Write218_CustomDocument";
                    _tmp[@"SerializationTypes.CustomElement::"] = @"Write219_CustomElement";
                    _tmp[@"SerializationTypes.CustomAttribute::"] = @"Write220_Item";
                    _tmp[@"SerializationTypes.TypeWithNonPublicDefaultConstructor::"] = @"Write221_Item";
                    _tmp[@"SerializationTypes.ServerSettings::"] = @"Write222_ServerSettings";
                    _tmp[@"SerializationTypes.TypeWithXmlQualifiedName::"] = @"Write223_TypeWithXmlQualifiedName";
                    _tmp[@"SerializationTypes.TypeWith2DArrayProperty2::"] = @"Write224_TypeWith2DArrayProperty2";
                    _tmp[@"SerializationTypes.TypeWithPropertiesHavingDefaultValue::"] = @"Write225_Item";
                    _tmp[@"SerializationTypes.TypeWithEnumPropertyHavingDefaultValue::"] = @"Write226_Item";
                    _tmp[@"SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue::"] = @"Write227_Item";
                    _tmp[@"SerializationTypes.TypeWithShouldSerializeMethod::"] = @"Write228_TypeWithShouldSerializeMethod";
                    _tmp[@"SerializationTypes.KnownTypesThroughConstructorWithArrayProperties::"] = @"Write229_Item";
                    _tmp[@"SerializationTypes.KnownTypesThroughConstructorWithValue::"] = @"Write230_Item";
                    _tmp[@"SerializationTypes.TypeWithTypesHavingCustomFormatter::"] = @"Write231_Item";
                    _tmp[@"SerializationTypes.TypeWithArrayPropertyHavingChoice::"] = @"Write232_Item";
                    _tmp[@"SerializationTypes.TypeWithPropertyHavingComplexChoice::"] = @"Write233_Item";
                    _tmp[@"SerializationTypes.MoreChoices::"] = @"Write234_MoreChoices";
                    _tmp[@"SerializationTypes.ComplexChoiceA::"] = @"Write235_ComplexChoiceA";
                    _tmp[@"SerializationTypes.ComplexChoiceB::"] = @"Write236_ComplexChoiceB";
                    _tmp[@"SerializationTypes.TypeWithFieldsOrdered::"] = @"Write237_TypeWithFieldsOrdered";
                    _tmp[@"SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName::"] = @"Write238_Item";
                    _tmp[@"SerializationTypes.NamespaceTypeNameClashContainer::Root:True:"] = @"Write239_Root";
                    _tmp[@"SerializationTypes.TypeNameClashB.TypeNameClash::"] = @"Write240_TypeClashB";
                    _tmp[@"SerializationTypes.TypeNameClashA.TypeNameClash::"] = @"Write241_TypeClashA";
                    _tmp[@"Outer+Person::"] = @"Write242_Person";
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
                    _tmp.Add(@"SerializationTypes.ClassImplementingIXmlSerializable::", new ClassImplementingIXmlSerializableSerializer());
                    _tmp.Add(@"SerializationTypes.TypeNameClashB.TypeNameClash::", new TypeNameClashSerializer());
                    _tmp.Add(@"DefaultValuesSetToNaN::", new DefaultValuesSetToNaNSerializer());
                    _tmp.Add(@"SerializationTypes.StructNotSerializable::", new StructNotSerializableSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithTypesHavingCustomFormatter::", new TypeWithTypesHavingCustomFormatterSerializer());
                    _tmp.Add(@"Trumpet::", new TrumpetSerializer());
                    _tmp.Add(@"Pet::", new PetSerializer());
                    _tmp.Add(@"SerializationTypes.CustomDocument::", new CustomDocumentSerializer());
                    _tmp.Add(@"SerializationTypes.NamespaceTypeNameClashContainer::Root:True:", new NamespaceTypeNameClashContainerSerializer());
                    _tmp.Add(@"SerializationTypes.DCClassWithEnumAndStruct::", new DCClassWithEnumAndStructSerializer());
                    _tmp.Add(@"SerializationTypes.EnumFlags::", new EnumFlagsSerializer());
                    _tmp.Add(@"SerializationTypes.ShortEnum::", new ShortEnumSerializer());
                    _tmp.Add(@"DerivedClass1::", new DerivedClass1Serializer());
                    _tmp.Add(@"MyCollection1::", new MyCollection1Serializer());
                    _tmp.Add(@"SerializationTypes.TypeWithXmlSchemaFormAttribute:::True:", new TypeWithXmlSchemaFormAttributeSerializer());
                    _tmp.Add(@"SerializationTypes.WithEnums::", new WithEnumsSerializer());
                    _tmp.Add(@"Brass::", new BrassSerializer());
                    _tmp.Add(@"SerializationTypes.ByteEnum::", new ByteEnumSerializer());
                    _tmp.Add(@"SerializationTypes.DerivedClassWithSameProperty::", new DerivedClassWithSamePropertySerializer());
                    _tmp.Add(@"SerializationTypes.WithStruct::", new WithStructSerializer());
                    _tmp.Add(@"SerializationTypes.UIntEnum::", new UIntEnumSerializer());
                    _tmp.Add(@"OrderedItem::", new OrderedItemSerializer());
                    _tmp.Add(@"TypeWithDateTimeOffsetProperties::", new TypeWithDateTimeOffsetPropertiesSerializer());
                    _tmp.Add(@"SerializationTypes.__TypeNameWithSpecialCharacters漢ñ::", new __TypeNameWithSpecialCharacters漢ñSerializer());
                    _tmp.Add(@"SerializationTypes.TypeNameClashA.TypeNameClash::", new TypeNameClashSerializer1());
                    _tmp.Add(@"XElementWrapper::", new XElementWrapperSerializer());
                    _tmp.Add(@"Animal::", new AnimalSerializer());
                    _tmp.Add(@"TypeWithMismatchBetweenAttributeAndPropertyType::RootElement:True:", new TypeWithMismatchBetweenAttributeAndPropertyTypeSerializer());
                    _tmp.Add(@"Group::", new GroupSerializer());
                    _tmp.Add(@"BaseClass1::", new BaseClass1Serializer());
                    _tmp.Add(@"SerializationTypes.XmlSerializerAttributes::AttributeTesting:False:", new XmlSerializerAttributesSerializer());
                    _tmp.Add(@"SimpleDerivedClass::", new SimpleDerivedClassSerializer());
                    _tmp.Add(@"SerializationTypes.ULongEnum::", new ULongEnumSerializer());
                    _tmp.Add(@"SerializationTypes.IntEnum::", new IntEnumSerializer());
                    _tmp.Add(@"SerializationTypes.KnownTypesThroughConstructorWithArrayProperties::", new KnownTypesThroughConstructorWithArrayPropertiesSerializer());
                    _tmp.Add(@"TypeWithLinkedProperty::", new TypeWithLinkedPropertySerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithGetOnlyArrayProperties::", new TypeWithGetOnlyArrayPropertiesSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithAnyAttribute::", new TypeWithAnyAttributeSerializer());
                    _tmp.Add(@"SerializationTypes.SomeStruct::", new SomeStructSerializer());
                    _tmp.Add(@"Dog::", new DogSerializer());
                    _tmp.Add(@"Vehicle::", new VehicleSerializer());
                    _tmp.Add(@"DerivedClass::", new DerivedClassSerializer());
                    _tmp.Add(@"XmlSerializableBaseClass:http://example.com/serializer-test-namespace::True:", new XmlSerializableBaseClassSerializer());
                    _tmp.Add(@"SerializationTypes.CustomElement::", new CustomElementSerializer());
                    _tmp.Add(@"TypeWithBinaryProperty::", new TypeWithBinaryPropertySerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithPropertyHavingComplexChoice::", new TypeWithPropertyHavingComplexChoiceSerializer());
                    _tmp.Add(@"TimeOnlyAsXsdTimeWrapper::", new TimeOnlyAsXsdTimeWrapperSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithXmlTextAttributeOnArray:http://schemas.xmlsoap.org/ws/2005/04/discovery::False:", new TypeWithXmlTextAttributeOnArraySerializer());
                    _tmp.Add(@"TypeWithDateAndTimeOnlyProperties::", new TypeWithDateAndTimeOnlyPropertiesSerializer());
                    _tmp.Add(@"TypeWithTimeSpanProperty::", new TypeWithTimeSpanPropertySerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithTypeNameInXmlTypeAttribute::", new TypeWithTypeNameInXmlTypeAttributeSerializer());
                    _tmp.Add(@"TypeWithXmlElementProperty::", new TypeWithXmlElementPropertySerializer());
                    _tmp.Add(@"SerializationTypes.TypeA::", new TypeASerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithMyCollectionField::", new TypeWithMyCollectionFieldSerializer());
                    _tmp.Add(@"Employee::", new EmployeeSerializer());
                    _tmp.Add(@"DateTimeTimeWrapper::", new DateTimeTimeWrapperSerializer());
                    _tmp.Add(@"SerializationTypes.KnownTypesThroughConstructor::", new KnownTypesThroughConstructorSerializer());
                    _tmp.Add(@"SerializationTypes.SimpleType::", new SimpleTypeSerializer());
                    _tmp.Add(@"TypeWithDefaultTimeSpanProperty::", new TypeWithDefaultTimeSpanPropertySerializer());
                    _tmp.Add(@"SerializationTypes.TypeHasArrayOfASerializedAsB::", new TypeHasArrayOfASerializedAsBSerializer());
                    _tmp.Add(@"BaseClass::", new BaseClassSerializer());
                    _tmp.Add(@"XmlSerializableDerivedClass::", new XmlSerializableDerivedClassSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWith2DArrayProperty2::", new TypeWith2DArrayProperty2Serializer());
                    _tmp.Add(@"SerializationTypes.WithNullables::", new WithNullablesSerializer());
                    _tmp.Add(@"XElementArrayWrapper::", new XElementArrayWrapperSerializer());
                    _tmp.Add(@"SerializationTypes.SimpleKnownTypeValue::", new SimpleKnownTypeValueSerializer());
                    _tmp.Add(@"SerializationTypes.SByteEnum::", new SByteEnumSerializer());
                    _tmp.Add(@"DefaultValuesSetToPositiveInfinity::", new DefaultValuesSetToPositiveInfinitySerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithShouldSerializeMethod::", new TypeWithShouldSerializeMethodSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithDateTimePropertyAsXmlTime::", new TypeWithDateTimePropertyAsXmlTimeSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithEnumPropertyHavingDefaultValue::", new TypeWithEnumPropertyHavingDefaultValueSerializer());
                    _tmp.Add(@"SerializationTypes.ComplexChoiceA::", new ComplexChoiceASerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithSchemaFormInXmlAttribute::", new TypeWithSchemaFormInXmlAttributeSerializer());
                    _tmp.Add(@"SerializationTypes.KnownTypesThroughConstructorWithValue::", new KnownTypesThroughConstructorWithValueSerializer());
                    _tmp.Add(@"SerializationTypes.ComplexChoiceB::", new ComplexChoiceBSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithFieldsOrdered::", new TypeWithFieldsOrderedSerializer());
                    _tmp.Add(@"Address::", new AddressSerializer());
                    _tmp.Add(@"SerializationTypes.DCStruct::", new DCStructSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithArraylikeMembers::", new TypeWithArraylikeMembersSerializer());
                    _tmp.Add(@"TypeWithXmlNodeArrayProperty:::True:", new TypeWithXmlNodeArrayPropertySerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithArrayPropertyHavingChoice::", new TypeWithArrayPropertyHavingChoiceSerializer());
                    _tmp.Add(@"MsgDocumentType:http://example.com:Document:True:", new MsgDocumentTypeSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithGetSetArrayMembers::", new TypeWithGetSetArrayMembersSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithPropertiesHavingDefaultValue::", new TypeWithPropertiesHavingDefaultValueSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithPropertyNameSpecified::", new TypeWithPropertyNameSpecifiedSerializer());
                    _tmp.Add(@"AliasedTestType::", new AliasedTestTypeSerializer());
                    _tmp.Add(@"SerializationTypes.MyEnum::", new MyEnumSerializer());
                    _tmp.Add(@"SerializationTypes.MyList::", new MyListSerializer());
                    _tmp.Add(@"DogBreed::", new DogBreedSerializer());
                    _tmp.Add(@"Parameter::", new ParameterSerializer());
                    _tmp.Add(@"SerializationTypes.CustomAttribute::", new CustomAttributeSerializer());
                    _tmp.Add(@"DateOnlyWrapper::", new DateOnlyWrapperSerializer());
                    _tmp.Add(@"SerializationTypes.SimpleDC::", new SimpleDCSerializer());
                    _tmp.Add(@"SimpleBaseClass::", new SimpleBaseClassSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithXmlQualifiedName::", new TypeWithXmlQualifiedNameSerializer());
                    _tmp.Add(@"SerializationTypes.BuiltInTypes::", new BuiltInTypesSerializer());
                    _tmp.Add(@"SerializationTypes.TypeB::", new TypeBSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithKnownTypesOfCollectionsWithConflictingXmlName::", new TypeWithKnownTypesOfCollectionsWithConflictingXmlNameSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithByteArrayAsXmlText::", new TypeWithByteArrayAsXmlTextSerializer());
                    _tmp.Add(@"Instrument::", new InstrumentSerializer());
                    _tmp.Add(@"Orchestra::", new OrchestraSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithDateTimeStringProperty::", new TypeWithDateTimeStringPropertySerializer());
                    _tmp.Add(@"SerializationTypes.DerivedClassWithSameProperty2::", new DerivedClassWithSameProperty2Serializer());
                    _tmp.Add(@"SerializationTypes.MoreChoices::", new MoreChoicesSerializer());
                    _tmp.Add(@"TimeOnlyWrapper::", new TimeOnlyWrapperSerializer());
                    _tmp.Add(@"RootClass::", new RootClassSerializer());
                    _tmp.Add(@"PurchaseOrder:http://www.contoso1.com:PurchaseOrder:False:", new PurchaseOrderSerializer());
                    _tmp.Add(@"SerializationTypes.BaseClassWithSamePropertyName::", new BaseClassWithSamePropertyNameSerializer());
                    _tmp.Add(@"DefaultValuesSetToNegativeInfinity::", new DefaultValuesSetToNegativeInfinitySerializer());
                    _tmp.Add(@"SerializationTypes.ClassImplementsInterface::", new ClassImplementsInterfaceSerializer());
                    _tmp.Add(@"SerializationTypes.LongEnum::", new LongEnumSerializer());
                    _tmp.Add(@"TypeWithXmlDocumentProperty::", new TypeWithXmlDocumentPropertySerializer());
                    _tmp.Add(@"Outer+Person::", new PersonSerializer());
                    _tmp.Add(@"XElementStruct::", new XElementStructSerializer());
                    _tmp.Add(@"SerializationTypes.ServerSettings::", new ServerSettingsSerializer());
                    _tmp.Add(@"SerializationTypes.ItemChoiceType::", new ItemChoiceTypeSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithEnumFlagPropertyHavingDefaultValue::", new TypeWithEnumFlagPropertyHavingDefaultValueSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithEnumMembers::", new TypeWithEnumMembersSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithNonPublicDefaultConstructor::", new TypeWithNonPublicDefaultConstructorSerializer());
                    _tmp.Add(@"TypeWithByteProperty::", new TypeWithBytePropertySerializer());
                    _tmp.Add(@"DateTimeDateWrapper::", new DateTimeDateWrapperSerializer());
                    _tmp.Add(@"SerializationTypes.TypeWithReadOnlyMyCollectionProperty::", new TypeWithReadOnlyMyCollectionPropertySerializer());
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
            if (type == typeof(global::TypeWithDateAndTimeOnlyProperties)) return true;
            if (type == typeof(global::DateOnlyWrapper)) return true;
            if (type == typeof(global::TimeOnlyWrapper)) return true;
            if (type == typeof(global::TimeOnlyAsXsdTimeWrapper)) return true;
            if (type == typeof(global::DateTimeDateWrapper)) return true;
            if (type == typeof(global::DateTimeTimeWrapper)) return true;
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
            if (type == typeof(global::SerializationTypes.TypeWithArraylikeMembers)) return true;
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
            if (type == typeof(global::SerializationTypes.TypeWithPropertyHavingComplexChoice)) return true;
            if (type == typeof(global::SerializationTypes.MoreChoices)) return true;
            if (type == typeof(global::SerializationTypes.ComplexChoiceA)) return true;
            if (type == typeof(global::SerializationTypes.ComplexChoiceB)) return true;
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
            if (type == typeof(global::TypeWithDateAndTimeOnlyProperties)) return new TypeWithDateAndTimeOnlyPropertiesSerializer();
            if (type == typeof(global::DateOnlyWrapper)) return new DateOnlyWrapperSerializer();
            if (type == typeof(global::TimeOnlyWrapper)) return new TimeOnlyWrapperSerializer();
            if (type == typeof(global::TimeOnlyAsXsdTimeWrapper)) return new TimeOnlyAsXsdTimeWrapperSerializer();
            if (type == typeof(global::DateTimeDateWrapper)) return new DateTimeDateWrapperSerializer();
            if (type == typeof(global::DateTimeTimeWrapper)) return new DateTimeTimeWrapperSerializer();
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
            if (type == typeof(global::SerializationTypes.TypeWithArraylikeMembers)) return new TypeWithArraylikeMembersSerializer();
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
            if (type == typeof(global::SerializationTypes.TypeWithPropertyHavingComplexChoice)) return new TypeWithPropertyHavingComplexChoiceSerializer();
            if (type == typeof(global::SerializationTypes.MoreChoices)) return new MoreChoicesSerializer();
            if (type == typeof(global::SerializationTypes.ComplexChoiceA)) return new ComplexChoiceASerializer();
            if (type == typeof(global::SerializationTypes.ComplexChoiceB)) return new ComplexChoiceBSerializer();
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
