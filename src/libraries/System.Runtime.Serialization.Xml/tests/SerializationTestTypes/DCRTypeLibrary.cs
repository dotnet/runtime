// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SerializationTestTypes
{
    [DataContract]
    public class ObjectContainer
    {
        [DataMember]
        private object _data;

        [DataMember]
        private object _data2;

        public ObjectContainer(object input)
        {
            _data = input;
            _data2 = _data;
        }

        public object Data
        {
            get { return _data; }
        }

        public object Data2
        {
            get { return _data2; }
        }
    }

    [DataContract(Name = "EmptyDCType", Namespace = "http://www.Default.com")]
    public class EmptyDCType
    {
    }

    [KnownType(typeof(EmptyDCType))]
    public class POCOObjectContainer
    {
        public POCOObjectContainer() { Data = new EmptyDCType(); }

        public object Data;

        [IgnoreDataMember]
        public object NonSerializedData;

        public POCOObjectContainer(object input)
        {
            Data = input;
        }
    }

    [DataContract]
    public class Alpha
    {
        [DataMember]
        public Person person = new Person();
    }

    [DataContract]
    [KnownType(typeof(CharClass))]
    public class Beta
    {
        [DataMember]
        public object unknown1 = new CharClass();
    }

    [DataContract]
    public class Charlie
    {
        [DataMember]
        public object unknown2 = new SerializationTestTypes.Employee();
    }


    [DataContract(Namespace = "NonExistNamespace")]
    public class Employee
    {
        [DataMember]
        public DateTime dateHired;

        [DataMember]
        public decimal salary;

        [DataMember]
        public Individual individual;
    }

    public class Individual
    {
        public string firstName;

        public string lastName;

        public int age;

        public object data;

        public object data1;

        public Individual() { }
    }

    [DataContract]
    public class Wireformat1
    {
        [DataMember]
        public Alpha alpha = new Alpha();
        [DataMember]
        public Beta beta = new Beta();
        [DataMember]
        public Charlie charlie = new Charlie();
    }

    [DataContract]
    public class Wireformat2
    {
        [DataMember]
        public Beta beta1 = new Beta();
        [DataMember]
        public Charlie charlie = new Charlie();
        [DataMember]
        public Beta beta2 = new Beta();
    }

    [DataContract]
    public class Wireformat3
    {
        [DataMember]
        public Charlie charlie1 = new Charlie();
        [DataMember]
        public Beta beta = new Beta();
        [DataMember]
        public Charlie charlie2 = new Charlie();
    }

    [DataContract]
    public class DCRVariations
    {
        [DataMember]
        public object unknownType1;

        [DataMember]
        public object unknownType2;
    }

    [DataContract]
    public class CustomClass
    {
        [DataMember()]
        private object[] knownTypes;

        [DataMember()]
        private object[] dataContractResolverTypes;

        [DataMember()]
        public virtual object[] KnownTypes
        {
            get { return knownTypes; }
            set { knownTypes = value; }
        }

        [DataMember()]
        public virtual object[] DataContractResolverTypes
        {
            get { return dataContractResolverTypes; }
            set { dataContractResolverTypes = value; }
        }
    }

    [DataContract]
    public class DefaultCollections
    {
        [DataMember]
        private ArrayList _arrayList = new ArrayList() { new Person() };
        [DataMember]
        private Dictionary<int, object> _dictionary = new Dictionary<int, object>() { { 001, new CharClass() } };
        [DataMember]
        private Hashtable _hashtable = new Hashtable() { { "one", new Version1() } };
        [DataMember]
        private object[] _singleDimArray = new object[] { new Employee() };
    }

    [DataContract(Name = "Car", Namespace = "TestingVersionTolerance")]
    public class Version1
    {
        [DataMember]
        public object make;

        public Version1()
        {
            make = "Chevrolet";
        }
    }

    public class TypeNotFound { };

    public class TypeLibraryManager
    {
        private List<Type> _primitiveTypeList = new List<Type>();
        private List<Type> _collectionTypeList = new List<Type>();
        private List<Type> _selfRefAndCyclesTypeList = new List<Type>();
        private List<Type> _iobjectRefTypeList = new List<Type>();
        private List<Type> _sampleTypeList = new List<Type>();
        private List<Type> _isReferenceTypeList = new List<Type>();
        private List<Type> _allTypesList = new List<Type>();
        private List<Type> _fxPrimitivesInCollectionList = new List<Type>();

        private Hashtable _allTypeHashTable = new Hashtable();

        public Hashtable AllTypesHashtable
        {
            get
            {
                if (_allTypeHashTable.Count == 0)
                {
                    _allTypeHashTable = new Hashtable();
                    foreach (Type t in AllTypesList)
                    {
                        _allTypeHashTable.Add(t.FullName, t);
                    }
                }
                return _allTypeHashTable;
            }
        }

        public List<Type> FxPrimitivesInCollectionList
        {
            get
            {
                if (_fxPrimitivesInCollectionList.Count == 0)
                {
                    _fxPrimitivesInCollectionList.Add(typeof(List<bool>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<byte>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<byte[]>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<char>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<System.DateTime>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<System.DBNull>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<decimal>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<double>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<System.Guid>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<short>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<int>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<long>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<System.Nullable<System.Boolean>>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<System.Nullable<System.Byte>>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<System.Nullable<System.Char>>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<System.Nullable<System.DateTime>>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<System.Nullable<System.Decimal>>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<System.Nullable<System.Double>>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<System.Nullable<System.Guid>>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<System.Nullable<System.Int16>>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<System.Nullable<System.Int32>>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<System.Nullable<System.Int64>>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<System.Nullable<System.SByte>>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<System.Nullable<System.Single>>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<System.Nullable<System.TimeSpan>>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<System.Nullable<System.UInt16>>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<System.Nullable<System.UInt32>>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<System.Nullable<System.UInt64>>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<sbyte>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<float>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<string>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<System.TimeSpan>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<ushort>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<uint>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<ulong>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<System.Xml.XmlElement>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<System.Xml.XmlNode[]>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<System.DateTimeOffset>));
                    _fxPrimitivesInCollectionList.Add(typeof(List<System.Nullable<System.DateTimeOffset>>));
                }
                return _fxPrimitivesInCollectionList;
            }
        }

        public List<Type> AllTypesList
        {
            get
            {
                if (_allTypesList.Count == 0)
                {
                    _allTypesList.AddRange(this.FxPrimitivesInCollectionList);
                    _allTypesList.AddRange(this.EnumsStructList);
                    _allTypesList.AddRange(this.SelfRefAndCyclesTypeList);
                    _allTypesList.AddRange(this.IsReferenceTypeList);
                    _allTypesList.AddRange(this.IObjectRefTypeList);
                    _allTypesList.AddRange(this.CollectionsTypeList);
                    _allTypesList.AddRange(this.SampleTypeList);
                }
                return _allTypesList;
            }
        }

        public List<Type> IsReferenceTypeList
        {
            get
            {
                if (_isReferenceTypeList.Count == 0)
                {
                    _isReferenceTypeList.Add(typeof(TestInheritance9));
                    _isReferenceTypeList.Add(typeof(SimpleDC));
                    _isReferenceTypeList.Add(typeof(SimpleDCWithSimpleDMRef));
                    _isReferenceTypeList.Add(typeof(SimpleDCWithRef));
                    _isReferenceTypeList.Add(typeof(ContainsSimpleDCWithRef));
                    _isReferenceTypeList.Add(typeof(SimpleDCWithIsRequiredFalse));
                    _isReferenceTypeList.Add(typeof(Mixed1));
                    _isReferenceTypeList.Add(typeof(SerIser));
                    _isReferenceTypeList.Add(typeof(DCVersioned1));
                    _isReferenceTypeList.Add(typeof(DCVersioned2));
                    _isReferenceTypeList.Add(typeof(DCVersionedContainer1));
                    _isReferenceTypeList.Add(typeof(DCVersionedContainerVersion1));
                    _isReferenceTypeList.Add(typeof(DCVersionedContainerVersion2));
                    _isReferenceTypeList.Add(typeof(DCVersionedContainerVersion3));
                    _isReferenceTypeList.Add(typeof(BaseDC));
                    _isReferenceTypeList.Add(typeof(BaseSerializable));
                    _isReferenceTypeList.Add(typeof(DerivedDC));
                    _isReferenceTypeList.Add(typeof(DerivedSerializable));
                    _isReferenceTypeList.Add(typeof(DerivedDCIsRefBaseSerializable));
                    _isReferenceTypeList.Add(typeof(DerivedDCBaseSerializable));
                    _isReferenceTypeList.Add(typeof(Derived2DC));
                    _isReferenceTypeList.Add(typeof(BaseDCNoIsRef));
                    _isReferenceTypeList.Add(typeof(DerivedPOCOBaseDCNOISRef));
                    _isReferenceTypeList.Add(typeof(DerivedIXmlSerializable_POCOBaseDCNOISRef));
                    _isReferenceTypeList.Add(typeof(DerivedCDCFromBaseDC));
                    _isReferenceTypeList.Add(typeof(Derived2Serializable));
                    _isReferenceTypeList.Add(typeof(Derived2SerializablePositive));
                    _isReferenceTypeList.Add(typeof(Derived2Derived2Serializable));
                    _isReferenceTypeList.Add(typeof(Derived3Derived2Serializable));
                    _isReferenceTypeList.Add(typeof(Derived31Derived2SerializablePOCO));
                    _isReferenceTypeList.Add(typeof(Derived4Derived2Serializable));
                    _isReferenceTypeList.Add(typeof(Derived5Derived2Serializable));
                    _isReferenceTypeList.Add(typeof(Derived6Derived2SerializablePOCO));
                    _isReferenceTypeList.Add(typeof(BaseWithIsRefTrue));
                    _isReferenceTypeList.Add(typeof(DerivedNoIsRef));
                    _isReferenceTypeList.Add(typeof(DerivedNoIsRef2));
                    _isReferenceTypeList.Add(typeof(DerivedNoIsRef3));
                    _isReferenceTypeList.Add(typeof(DerivedNoIsRef4));
                    _isReferenceTypeList.Add(typeof(DerivedNoIsRef5));
                    _isReferenceTypeList.Add(typeof(DerivedNoIsRefWithIsRefTrue6));
                    _isReferenceTypeList.Add(typeof(DerivedWithIsRefFalse));
                    _isReferenceTypeList.Add(typeof(DerivedWithIsRefFalse2));
                    _isReferenceTypeList.Add(typeof(DerivedWithIsRefFalse3));
                    _isReferenceTypeList.Add(typeof(DerivedWithIsRefFalse4));
                    _isReferenceTypeList.Add(typeof(DerivedWithIsRefFalse5));
                    _isReferenceTypeList.Add(typeof(DerivedWithIsRefTrue6));
                    _isReferenceTypeList.Add(typeof(DerivedWithIsRefTrueExplicit));
                    _isReferenceTypeList.Add(typeof(DerivedWithIsRefTrueExplicit2));
                    _isReferenceTypeList.Add(typeof(BaseNoIsRef));
                    _isReferenceTypeList.Add(typeof(DerivedWithIsRefFalseExplicit));
                    _isReferenceTypeList.Add(typeof(TestInheritance));
                    _isReferenceTypeList.Add(typeof(TestInheritance91));
                    _isReferenceTypeList.Add(typeof(TestInheritance5));
                    _isReferenceTypeList.Add(typeof(TestInheritance10));
                    _isReferenceTypeList.Add(typeof(TestInheritance2));
                    _isReferenceTypeList.Add(typeof(TestInheritance11));
                    _isReferenceTypeList.Add(typeof(TestInheritance3));
                    _isReferenceTypeList.Add(typeof(TestInheritance16));
                    _isReferenceTypeList.Add(typeof(TestInheritance4));
                    _isReferenceTypeList.Add(typeof(TestInheritance12));
                    _isReferenceTypeList.Add(typeof(TestInheritance6));
                    _isReferenceTypeList.Add(typeof(TestInheritance7));
                    _isReferenceTypeList.Add(typeof(TestInheritance14));
                    _isReferenceTypeList.Add(typeof(TestInheritance8));
                }
                return _isReferenceTypeList;
            }
        }

        public List<Type> SelfRefAndCyclesTypeList
        {
            get
            {
                if (_selfRefAndCyclesTypeList.Count == 0)
                {
                    _selfRefAndCyclesTypeList.Add(typeof(SelfRef1));
                    _selfRefAndCyclesTypeList.Add(typeof(SelfRef1DoubleDM));
                    _selfRefAndCyclesTypeList.Add(typeof(SelfRef2));
                    _selfRefAndCyclesTypeList.Add(typeof(SelfRef3));
                    _selfRefAndCyclesTypeList.Add(typeof(Cyclic1));
                    _selfRefAndCyclesTypeList.Add(typeof(Cyclic2));
                    _selfRefAndCyclesTypeList.Add(typeof(CyclicA));
                    _selfRefAndCyclesTypeList.Add(typeof(CyclicB));
                    _selfRefAndCyclesTypeList.Add(typeof(CyclicC));
                    _selfRefAndCyclesTypeList.Add(typeof(CyclicD));
                    _selfRefAndCyclesTypeList.Add(typeof(CyclicABCD1));
                    _selfRefAndCyclesTypeList.Add(typeof(CyclicABCD2));
                    _selfRefAndCyclesTypeList.Add(typeof(CyclicABCD3));
                    _selfRefAndCyclesTypeList.Add(typeof(CyclicABCD4));
                    _selfRefAndCyclesTypeList.Add(typeof(CyclicABCD5));
                    _selfRefAndCyclesTypeList.Add(typeof(CyclicABCD6));
                    _selfRefAndCyclesTypeList.Add(typeof(CyclicABCD7));
                    _selfRefAndCyclesTypeList.Add(typeof(CyclicABCD8));
                    _selfRefAndCyclesTypeList.Add(typeof(CyclicABCDNoCycles));
                    _selfRefAndCyclesTypeList.Add(typeof(A1));
                    _selfRefAndCyclesTypeList.Add(typeof(B1));
                    _selfRefAndCyclesTypeList.Add(typeof(C1));
                    _selfRefAndCyclesTypeList.Add(typeof(BB1));
                    _selfRefAndCyclesTypeList.Add(typeof(BBB1));
                }
                return _selfRefAndCyclesTypeList;
            }
        }

        public List<Type> IObjectRefTypeList
        {
            get
            {
                if (_iobjectRefTypeList.Count == 0)
                {
                    _iobjectRefTypeList.Add(typeof(DCExplicitInterfaceIObjRef));
                    _iobjectRefTypeList.Add(typeof(DCIObjRef));
                    _iobjectRefTypeList.Add(typeof(SerExplicitInterfaceIObjRefReturnsPrivate));
                    _iobjectRefTypeList.Add(typeof(SerIObjRefReturnsPrivate));
                    _iobjectRefTypeList.Add(typeof(DCExplicitInterfaceIObjRefReturnsPrivate));
                    _iobjectRefTypeList.Add(typeof(DCIObjRefReturnsPrivate));
                }
                return _iobjectRefTypeList;
            }
        }

        public List<Type> EnumsStructList
        {
            get
            {
                if (_primitiveTypeList.Count == 0)
                {
                    _primitiveTypeList.Add(typeof(Person));
                    _primitiveTypeList.Add(typeof(CharClass));
                    _primitiveTypeList.Add(typeof(AllTypes));
                    _primitiveTypeList.Add(typeof(AllTypes2));
                    _primitiveTypeList.Add(typeof(DictContainer));
                    _primitiveTypeList.Add(typeof(ListContainer));
                    _primitiveTypeList.Add(typeof(ArrayContainer));
                    _primitiveTypeList.Add(typeof(EnumContainer1));
                    _primitiveTypeList.Add(typeof(EnumContainer2));
                    _primitiveTypeList.Add(typeof(EnumContainer3));
                    _primitiveTypeList.Add(typeof(WithStatic));
                    _primitiveTypeList.Add(typeof(DerivedFromPriC));
                    _primitiveTypeList.Add(typeof(EmptyDC));
                    _primitiveTypeList.Add(typeof(Base));
                    _primitiveTypeList.Add(typeof(Derived));
                    _primitiveTypeList.Add(typeof(List));
                    _primitiveTypeList.Add(typeof(Arrays));
                    _primitiveTypeList.Add(typeof(Array3));
                    _primitiveTypeList.Add(typeof(Properties));
                    _primitiveTypeList.Add(typeof(HaveNS));
                    _primitiveTypeList.Add(typeof(OutClass));
                    _primitiveTypeList.Add(typeof(Temp));
                    _primitiveTypeList.Add(typeof(Array22));
                    _primitiveTypeList.Add(typeof(Person2));
                    _primitiveTypeList.Add(typeof(BoxedPrim));
                    _primitiveTypeList.Add(typeof(MyEnum));
                    _primitiveTypeList.Add(typeof(MyPrivateEnum1));
                    _primitiveTypeList.Add(typeof(MyPrivateEnum2));
                    _primitiveTypeList.Add(typeof(MyPrivateEnum3));
                    _primitiveTypeList.Add(typeof(MyEnum1));
                    _primitiveTypeList.Add(typeof(MyEnum2));
                    _primitiveTypeList.Add(typeof(MyEnum3));
                    _primitiveTypeList.Add(typeof(MyEnum4));
                    _primitiveTypeList.Add(typeof(MyEnum7));
                    _primitiveTypeList.Add(typeof(MyEnum8));
                    _primitiveTypeList.Add(typeof(SeasonsEnumContainer));
                }
                return _primitiveTypeList;
            }
        }

        public List<Type> CollectionsTypeList
        {
            get
            {
                if (_collectionTypeList.Count == 0)
                {
                    _collectionTypeList = new List<Type>();
                    _collectionTypeList.Add(typeof(ContainsLinkedList));
                    _collectionTypeList.Add(typeof(SimpleCDC));
                    _collectionTypeList.Add(typeof(SimpleCDC2));
                    _collectionTypeList.Add(typeof(ContainsSimpleCDC));
                    _collectionTypeList.Add(typeof(DMInCollection1));
                    _collectionTypeList.Add(typeof(DMInCollection2));
                    _collectionTypeList.Add(typeof(DMInDict1));
                    _collectionTypeList.Add(typeof(DMWithRefInCollection1));
                    _collectionTypeList.Add(typeof(DMWithRefInCollection2));
                    _collectionTypeList.Add(typeof(DMWithRefInDict1));
                }
                return _collectionTypeList;
            }
        }

        public List<Type> SampleTypeList
        {
            get
            {
                if (_sampleTypeList.Count == 0)
                {
                    _sampleTypeList = new List<Type>();
                    _sampleTypeList.Add(typeof(TypeNotFound));
                    _sampleTypeList.Add(typeof(EmptyDCType));
                    _sampleTypeList.Add(typeof(ObjectContainer));
                    _sampleTypeList.Add(typeof(POCOObjectContainer));
                    _sampleTypeList.Add(typeof(CircularLink));
                    _sampleTypeList.Add(typeof(CircularLinkDerived));
                    _sampleTypeList.Add(typeof(KT1Base));
                    _sampleTypeList.Add(typeof(KT1Derived));
                    _sampleTypeList.Add(typeof(KT2Base));
                    _sampleTypeList.Add(typeof(KT3BaseKTMReturnsPrivateType));
                    _sampleTypeList.Add(typeof(KT2Derived));
                    _sampleTypeList.Add(typeof(CB1));
                    _sampleTypeList.Add(typeof(ArrayListWithCDCFilledPublicTypes));
                    _sampleTypeList.Add(typeof(ArrayListWithCDCFilledWithMixedTypes));
                    _sampleTypeList.Add(typeof(CollectionBaseWithCDCFilledPublicTypes));
                    _sampleTypeList.Add(typeof(CollectionBaseWithCDCFilledWithMixedTypes));
                    _sampleTypeList.Add(typeof(DCHashtableContainerPublic));
                    _sampleTypeList.Add(typeof(DCHashtableContainerMixedTypes));
                    _sampleTypeList.Add(typeof(CustomGenericContainerPrivateType1));
                    _sampleTypeList.Add(typeof(CustomGenericContainerPrivateType2));
                    _sampleTypeList.Add(typeof(CustomGenericContainerPrivateType3));
                    _sampleTypeList.Add(typeof(CustomGenericContainerPrivateType4));
                    _sampleTypeList.Add(typeof(CustomGenericContainerPublicType1));
                    _sampleTypeList.Add(typeof(CustomGenericContainerPublicType2));
                    _sampleTypeList.Add(typeof(CustomGenericContainerPublicType3));
                    _sampleTypeList.Add(typeof(CustomGenericContainerPublicType4));
                    _sampleTypeList.Add(typeof(CustomGeneric1<KT1Base>));
                    _sampleTypeList.Add(typeof(CustomGeneric2<KT1Base, NonDCPerson>));
                    _sampleTypeList.Add(typeof(GenericContainer));
                    _sampleTypeList.Add(typeof(GenericBase<NonDCPerson>));
                    _sampleTypeList.Add(typeof(GenericBase2<KT1Base, NonDCPerson>));
                    _sampleTypeList.Add(typeof(SimpleBase));
                    _sampleTypeList.Add(typeof(SimpleBaseDerived));
                    _sampleTypeList.Add(typeof(SimpleBaseDerived2));
                    _sampleTypeList.Add(typeof(SimpleBaseContainer));
                    _sampleTypeList.Add(typeof(DCListPrivateTContainer2));
                    _sampleTypeList.Add(typeof(DCListPrivateTContainer));
                    _sampleTypeList.Add(typeof(DCListPublicTContainer));
                    _sampleTypeList.Add(typeof(DCListMixedTContainer));
                    _sampleTypeList.Add(typeof(SampleListImplicitWithDC));
                    _sampleTypeList.Add(typeof(SampleListImplicitWithoutDC));
                    _sampleTypeList.Add(typeof(SampleListImplicitWithCDC));
                    _sampleTypeList.Add(typeof(SampleListExplicitWithDC));
                    _sampleTypeList.Add(typeof(SampleListExplicitWithoutDC));
                    _sampleTypeList.Add(typeof(SampleListExplicitWithCDC));
                    _sampleTypeList.Add(typeof(SampleListExplicitWithCDCContainsPrivateDC));
                    _sampleTypeList.Add(typeof(SampleListTImplicitWithDC));
                    _sampleTypeList.Add(typeof(SampleListTImplicitWithoutDC));
                    _sampleTypeList.Add(typeof(SampleListTImplicitWithCDC));
                    _sampleTypeList.Add(typeof(SampleListTExplicitWithDC));
                    _sampleTypeList.Add(typeof(SampleListTExplicitWithoutDC));
                    _sampleTypeList.Add(typeof(SampleListTExplicitWithCDC));
                    _sampleTypeList.Add(typeof(SampleListTExplicitWithCDCContainsPublicDCClassPrivateDM));
                    _sampleTypeList.Add(typeof(SampleListTExplicitWithCDCContainsPrivateDC));
                    _sampleTypeList.Add(typeof(SampleICollectionTImplicitWithDC));
                    _sampleTypeList.Add(typeof(SampleICollectionTImplicitWithoutDC));
                    _sampleTypeList.Add(typeof(SampleICollectionTImplicitWithCDC));
                    _sampleTypeList.Add(typeof(SampleICollectionTExplicitWithDC));
                    _sampleTypeList.Add(typeof(SampleICollectionTExplicitWithoutDC));
                    _sampleTypeList.Add(typeof(SampleICollectionTExplicitWithCDC));
                    _sampleTypeList.Add(typeof(SampleICollectionTExplicitWithCDCContainsPrivateDC));
                    _sampleTypeList.Add(typeof(SampleIEnumerableTImplicitWithDC));
                    _sampleTypeList.Add(typeof(SampleIEnumerableTImplicitWithoutDC));
                    _sampleTypeList.Add(typeof(SampleIEnumerableTImplicitWithCDC));
                    _sampleTypeList.Add(typeof(SampleIEnumerableTExplicitWithDC));
                    _sampleTypeList.Add(typeof(SampleIEnumerableTExplicitWithoutDC));
                    _sampleTypeList.Add(typeof(SampleIEnumerableTExplicitWithCDC));
                    _sampleTypeList.Add(typeof(SampleIEnumerableTExplicitWithCDCContainsPrivateDC));
                    _sampleTypeList.Add(typeof(SampleICollectionImplicitWithDC));
                    _sampleTypeList.Add(typeof(SampleICollectionImplicitWithoutDC));
                    _sampleTypeList.Add(typeof(SampleICollectionImplicitWithCDC));
                    _sampleTypeList.Add(typeof(SampleICollectionExplicitWithDC));
                    _sampleTypeList.Add(typeof(SampleICollectionExplicitWithoutDC));
                    _sampleTypeList.Add(typeof(SampleICollectionExplicitWithCDC));
                    _sampleTypeList.Add(typeof(SampleICollectionExplicitWithCDCContainsPrivateDC));
                    _sampleTypeList.Add(typeof(SampleIEnumerableImplicitWithDC));
                    _sampleTypeList.Add(typeof(SampleIEnumerableImplicitWithoutDC));
                    _sampleTypeList.Add(typeof(SampleIEnumerableImplicitWithCDC));
                    _sampleTypeList.Add(typeof(SampleIEnumerableExplicitWithDC));
                    _sampleTypeList.Add(typeof(SampleIEnumerableExplicitWithoutDC));
                    _sampleTypeList.Add(typeof(SampleIEnumerableExplicitWithCDC));
                    _sampleTypeList.Add(typeof(SampleIEnumerableExplicitWithCDCContainsPrivateDC));
                    _sampleTypeList.Add(typeof(MyIDictionaryContainsPublicDC));
                    _sampleTypeList.Add(typeof(MyIDictionaryContainsPublicDCExplicit));
                    _sampleTypeList.Add(typeof(MyIDictionaryContainsPrivateDC));
                    _sampleTypeList.Add(typeof(MyGenericIDictionaryKVContainsPublicDC));
                    _sampleTypeList.Add(typeof(MyGenericIDictionaryKVContainsPublicDCExplicit));
                    _sampleTypeList.Add(typeof(MyGenericIDictionaryKVContainsPrivateDC));
                    _sampleTypeList.Add(typeof(DCDictionaryPrivateKTContainer));
                    _sampleTypeList.Add(typeof(DCDictionaryPublicKTContainer));
                    _sampleTypeList.Add(typeof(DCDictionaryMixedKTContainer1));
                    _sampleTypeList.Add(typeof(DCDictionaryMixedKTContainer2));
                    _sampleTypeList.Add(typeof(DCDictionaryMixedKTContainer3));
                    _sampleTypeList.Add(typeof(DCDictionaryMixedKTContainer4));
                    _sampleTypeList.Add(typeof(DCDictionaryMixedKTContainer5));
                    _sampleTypeList.Add(typeof(DCDictionaryMixedKTContainer6));
                    _sampleTypeList.Add(typeof(DCDictionaryMixedKTContainer7));
                    _sampleTypeList.Add(typeof(DCDictionaryMixedKTContainer8));
                    _sampleTypeList.Add(typeof(DCDictionaryMixedKTContainer9));
                    _sampleTypeList.Add(typeof(DCDictionaryMixedKTContainer10));
                    _sampleTypeList.Add(typeof(DCDictionaryMixedKTContainer11));
                    _sampleTypeList.Add(typeof(DCDictionaryMixedKTContainer12));
                    _sampleTypeList.Add(typeof(DCDictionaryMixedKTContainer13));
                    _sampleTypeList.Add(typeof(DCDictionaryMixedKTContainer14));
                    _sampleTypeList.Add(typeof(PublicDC));
                    _sampleTypeList.Add(typeof(PublicDCDerivedPublic));
                    _sampleTypeList.Add(typeof(DC));
                    _sampleTypeList.Add(typeof(DCWithReadOnlyField));
                    _sampleTypeList.Add(typeof(IReadWriteXmlWriteBinHex_EqualityDefined));
                    _sampleTypeList.Add(typeof(PrivateDefaultCtorIXmlSerializables));
                    _sampleTypeList.Add(typeof(PublicIXmlSerializablesWithPublicSchemaProvider));
                    _sampleTypeList.Add(typeof(PublicExplicitIXmlSerializablesWithPublicSchemaProvider));
                    _sampleTypeList.Add(typeof(PublicIXmlSerializablesWithPrivateSchemaProvider));
                    _sampleTypeList.Add(typeof(PublicDCClassPublicDM));
                    _sampleTypeList.Add(typeof(PublicDCClassPrivateDM));
                    _sampleTypeList.Add(typeof(PublicDCClassInternalDM));
                    _sampleTypeList.Add(typeof(PublicDCClassMixedDM));
                    _sampleTypeList.Add(typeof(PublicDCClassPublicDM_DerivedDCClassPublic));
                    _sampleTypeList.Add(typeof(PublicDCClassPrivateDM_DerivedDCClassPublic));
                    _sampleTypeList.Add(typeof(PublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM));
                    _sampleTypeList.Add(typeof(Prop_PublicDCClassPublicDM_PublicDCClassPrivateDM));
                    _sampleTypeList.Add(typeof(Prop_SetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM));
                    _sampleTypeList.Add(typeof(Prop_GetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM));
                    _sampleTypeList.Add(typeof(Prop_PublicDCClassPublicDM));
                    _sampleTypeList.Add(typeof(Prop_PublicDCClassPrivateDM));
                    _sampleTypeList.Add(typeof(Prop_PublicDCClassInternalDM));
                    _sampleTypeList.Add(typeof(Prop_PublicDCClassMixedDM));
                    _sampleTypeList.Add(typeof(Prop_PublicDCClassPublicDM_DerivedDCClassPublic));
                    _sampleTypeList.Add(typeof(Prop_PublicDCClassPrivateDM_DerivedDCClassPublic));
                    _sampleTypeList.Add(typeof(Prop_PublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM));
                    _sampleTypeList.Add(typeof(Prop_SetPrivate_PublicDCClassPublicDM));
                    _sampleTypeList.Add(typeof(Prop_GetPrivate_PublicDCClassPublicDM));
                    _sampleTypeList.Add(typeof(Derived_Override_Prop_All_Public));
                    _sampleTypeList.Add(typeof(Derived_Override_Prop_Private));
                    _sampleTypeList.Add(typeof(Derived_Override_Prop_GetPrivate_All_Public));
                    _sampleTypeList.Add(typeof(Derived_Override_Prop_GetPrivate_Private));
                    _sampleTypeList.Add(typeof(DC1_Version1));
                    _sampleTypeList.Add(typeof(DC2_Version1));
                    _sampleTypeList.Add(typeof(DC2_Version4));
                    _sampleTypeList.Add(typeof(DC2_Version5));
                    _sampleTypeList.Add(typeof(DC3_Version1));
                    _sampleTypeList.Add(typeof(DC3_Version2));
                    _sampleTypeList.Add(typeof(DC3_Version3));
                    _sampleTypeList.Add(typeof(CallBackSample_OnSerializing_Public));
                    _sampleTypeList.Add(typeof(CallBackSample_OnSerialized_Public));
                    _sampleTypeList.Add(typeof(CallBackSample_OnDeserializing_Public));
                    _sampleTypeList.Add(typeof(CallBackSample_OnDeserialized_Public));
                    _sampleTypeList.Add(typeof(CallBackSample_OnSerializing));
                    _sampleTypeList.Add(typeof(CallBackSample_OnSerialized));
                    _sampleTypeList.Add(typeof(CallBackSample_OnDeserializing));
                    _sampleTypeList.Add(typeof(CallBackSample_OnDeserialized));
                    _sampleTypeList.Add(typeof(CallBackSample_IDeserializationCallback));
                    _sampleTypeList.Add(typeof(CallBackSample_IDeserializationCallback_Explicit));
                    _sampleTypeList.Add(typeof(CallBackSample_OnDeserialized_Private_Base));
                    _sampleTypeList.Add(typeof(CallBackSample_OnDeserialized_Public_Derived));
                    _sampleTypeList.Add(typeof(CDC_Possitive));
                    _sampleTypeList.Add(typeof(CDC_PrivateAdd));
                    _sampleTypeList.Add(typeof(Base_Possitive_VirtualAdd));
                    _sampleTypeList.Add(typeof(CDC_NewAddToPrivate));
                    _sampleTypeList.Add(typeof(CDC_PrivateDefaultCtor));
                    _sampleTypeList.Add(typeof(NonDCPerson));
                    _sampleTypeList.Add(typeof(PersonSurrogated));
                    _sampleTypeList.Add(typeof(DCSurrogate));
                    _sampleTypeList.Add(typeof(SerSurrogate));
                    _sampleTypeList.Add(typeof(DCSurrogateExplicit));
                    _sampleTypeList.Add(typeof(SerSurrogateExplicit));
                    _sampleTypeList.Add(typeof(DCSurrogateReturnPrivate));
                    _sampleTypeList.Add(typeof(SerSurrogateReturnPrivate));
                    _sampleTypeList.Add(typeof(NullableContainerContainsValue));
                    _sampleTypeList.Add(typeof(NullableContainerContainsNull));
                    _sampleTypeList.Add(typeof(NullablePrivateContainerContainsValue));
                    _sampleTypeList.Add(typeof(NullablePrivateContainerContainsNull));
                    _sampleTypeList.Add(typeof(NullablePrivateDataInDMContainerContainsValue));
                    _sampleTypeList.Add(typeof(NullablePrivateDataInDMContainerContainsNull));
                    _sampleTypeList.Add(typeof(DCPublicDatasetPublic));
                    _sampleTypeList.Add(typeof(DCPublicDatasetPrivate));
                    _sampleTypeList.Add(typeof(SerPublicDatasetPublic));
                    _sampleTypeList.Add(typeof(SerPublicDatasetPrivate));
                    _sampleTypeList.Add(typeof(CustomGeneric2<NonDCPerson>));
                    _sampleTypeList.Add(typeof(DTOContainer));
                }
                return _sampleTypeList;
            }
        }
    }

    public static class DCRUtils
    {
        public static bool CompareIObjectRefTypes(object serialized, object deSerialized)
        {
            Dictionary<DataContract, List<RefData>> alreadyRefdValues = ObjectRefUtil.GetReferenceCounts(serialized);
            Dictionary<DataContract, List<RefData>> alreadyRefdValues2 = ObjectRefUtil.GetReferenceCounts(deSerialized);
            if (!ObjectRefUtil.IsEqual(alreadyRefdValues, alreadyRefdValues2))
            {
                return false;
            }
            return true;
        }
    }

    public class RefData
    {
        public object Data;
        public int RefCount = 0;

        public RefData(object data)
        {
            Data = data;
        }

        public override bool Equals(object obj)
        {
            RefData other = obj as RefData;
            if (other == null) return false;
            return (object.ReferenceEquals(this.Data, other.Data));
        }

        public override int GetHashCode()
        {
            if (Data != null)
            {
                return Data.GetHashCode();
            }
            else
            {
                return base.GetHashCode();
            }
        }
    }

    public class ObjectRefUtil
    {
        public static Dictionary<DataContract, List<RefData>> GetReferenceCounts(object data)
        {
            Dictionary<DataContract, List<RefData>> nonRefdValues = new Dictionary<DataContract, List<RefData>>();
            return GetReferenceCounts(data, ref nonRefdValues);
        }

        public static Dictionary<DataContract, List<RefData>> GetReferenceCounts(object data, ref Dictionary<DataContract, List<RefData>> nonRefdValues)
        {
            Dictionary<DataContract, List<RefData>> alreadyRefdValues = new Dictionary<DataContract, List<RefData>>();
            Type type = data.GetType();
            DataContract dataContract = DataContract.GetDataContract(type, supportCollectionDataContract);
            s_refStack.Clear();
            FindAndAddRefd(data, dataContract, ref alreadyRefdValues, ref nonRefdValues);
            return alreadyRefdValues;
        }

        public static bool IsEqual(Dictionary<DataContract, List<RefData>> alreadyRefdValues1, Dictionary<DataContract, List<RefData>> alreadyRefdValues2)
        {
            if (alreadyRefdValues1.Count != alreadyRefdValues2.Count) return false;
            foreach (KeyValuePair<DataContract, List<RefData>> kp in alreadyRefdValues1)
            {
                if (alreadyRefdValues2.ContainsKey(kp.Key))
                {
                    if (alreadyRefdValues2[kp.Key].Count != kp.Value.Count)
                    {
                        return false;
                    }
                    for (int i = 0; i < kp.Value.Count; i++)
                    {
                        if (alreadyRefdValues2[kp.Key][i].RefCount != kp.Value[i].RefCount)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        private static Stack<RefData> s_refStack = new Stack<RefData>();
        /// <summary>
        /// </summary>
        /// <param name="data"></param>
        /// <param name="dataContract"></param>
        /// <param name="alreadyRefdValues"></param>
        /// <param name="nonRefdValues"></param>
        private static void FindAndAddRefd(object data, DataContract dataContract, ref Dictionary<DataContract, List<RefData>> alreadyRefdValues
                                        , ref Dictionary<DataContract, List<RefData>> nonRefdValues)
        {
            RefData refData = new RefData(data);
            FindRefUpdateRef(refData, dataContract, ref alreadyRefdValues, ref nonRefdValues);
            if (s_refStack.Contains(refData))
            {
                return;
            }
            else
            {
                s_refStack.Push(refData);
            }
            FindRefHandleMembers(data, dataContract, ref alreadyRefdValues, ref nonRefdValues);
            s_refStack.Pop();
        }

        public static bool supportCollectionDataContract = true;
        private static void FindRefUpdateRef(RefData refData, DataContract dataContract, ref Dictionary<DataContract, List<RefData>> alreadyRefdValues, ref Dictionary<DataContract, List<RefData>> nonRefdValues)
        {
            if (dataContract.IsReference)
            {
                if (alreadyRefdValues.ContainsKey(dataContract))
                {
                    if (!alreadyRefdValues[dataContract].Contains(refData))
                    {
                        alreadyRefdValues[dataContract].Add(refData);
                    }
                    else
                    {
                        alreadyRefdValues[dataContract][alreadyRefdValues[dataContract].IndexOf(refData)].RefCount++;
                    }
                }
                else
                {
                    List<RefData> list = new List<RefData>();
                    list.Add(refData);
                    alreadyRefdValues.Add(dataContract, list);
                }
            }
            else if (!(dataContract is PrimitiveDataContract))
            {
                if (nonRefdValues.ContainsKey(dataContract))
                {
                    if (!nonRefdValues[dataContract].Contains(refData))
                    {
                        nonRefdValues[dataContract].Add(refData);
                    }
                    else
                    {
                        nonRefdValues[dataContract][nonRefdValues[dataContract].IndexOf(refData)].RefCount++;
                    }
                }
                else
                {
                    List<RefData> list = new List<RefData>();
                    list.Add(refData);
                    nonRefdValues.Add(dataContract, list);
                }
            }
        }
        private static void FindRefHandleMembers(object data, DataContract dataContract, ref Dictionary<DataContract, List<RefData>> alreadyRefdValues, ref Dictionary<DataContract, List<RefData>> nonRefdValues)
        {
            if (dataContract is ClassDataContract)
            {
                ClassDataContract classContract = dataContract as ClassDataContract;
                foreach (DataMember member in classContract.Members)
                {
                    object memberData = member.GetMemberValue(data);
                    if (memberData != null)
                    {
                        FindAndAddRefd(memberData, DataContract.GetDataContract(memberData.GetType(), supportCollectionDataContract), ref alreadyRefdValues, ref nonRefdValues);
                    }
                }
            }
            else if (dataContract is ArrayDataContract)
            {
                ArrayDataContract arrayContract = dataContract as ArrayDataContract;
                foreach (object obj in (IEnumerable)data)
                {
                    if (obj != null)
                    {
                        FindAndAddRefd(obj, DataContract.GetDataContract(obj.GetType(), supportCollectionDataContract), ref alreadyRefdValues, ref nonRefdValues);
                    }
                }
            }
            else if (dataContract is CollectionDataContract)
            {
                FindRefHandleCollectionDataContractMembers(data, dataContract, ref alreadyRefdValues, ref nonRefdValues);
            }
            else if (dataContract is EnumDataContract || dataContract is PrimitiveDataContract)
            {
                //Nothing to do
            }
            else
            {
                throw new Exception("TestDriver Exception: Type Not Supported");
            }
        }

        private static void FindRefHandleCollectionDataContractMembers(object data, DataContract dataContract, ref Dictionary<DataContract, List<RefData>> alreadyRefdValues, ref Dictionary<DataContract, List<RefData>> nonRefdValues)
        {
            CollectionDataContract collectionContract = dataContract as CollectionDataContract;
            if (!collectionContract.IsDictionary)
            {
                foreach (object obj in (IEnumerable)data)
                {
                    if (obj != null)
                    {
                        FindAndAddRefd(obj, DataContract.GetDataContract(obj.GetType(), supportCollectionDataContract), ref alreadyRefdValues, ref nonRefdValues);
                    }
                }
            }
            else
            {
                IDictionary dictionary = data as IDictionary;
                if (dictionary != null)
                {
                    foreach (object key in dictionary.Keys)
                    {
                        if (key != null)
                        {
                            FindAndAddRefd(key, DataContract.GetDataContract(key.GetType(), supportCollectionDataContract), ref alreadyRefdValues, ref nonRefdValues);
                        }
                    }
                    foreach (object value in dictionary.Values)
                    {
                        if (value != null)
                        {
                            FindAndAddRefd(value, DataContract.GetDataContract(value.GetType(), supportCollectionDataContract), ref alreadyRefdValues, ref nonRefdValues);
                        }
                    }
                }
                else
                {
                    if (collectionContract.GetEnumeratorMethod != null)
                    {
                        object dictEnumObj = null;
                        try
                        {
                            dictEnumObj = collectionContract.GetEnumeratorMethod.Invoke(data, new object[] { });
                        }
                        catch (Exception) { }
                        IDictionaryEnumerator dictEnum = dictEnumObj as IDictionaryEnumerator;
                        if (dictEnum != null)
                        {
                            while (dictEnum.MoveNext())
                            {
                                FindAndAddRefd(dictEnum.Key, DataContract.GetDataContract(dictEnum.Key.GetType(), supportCollectionDataContract), ref alreadyRefdValues, ref nonRefdValues);
                            }
                            dictEnum.Reset();
                            while (dictEnum.MoveNext())
                            {
                                if (dictEnum.Value != null)
                                {
                                    FindAndAddRefd(dictEnum.Value, DataContract.GetDataContract(dictEnum.Value.GetType(), supportCollectionDataContract), ref alreadyRefdValues, ref nonRefdValues);
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("TestDriver Exception: Dictionary CollectionDataCotnract Type Not Supported");
                    }
                }
            }
        }
    }
}
