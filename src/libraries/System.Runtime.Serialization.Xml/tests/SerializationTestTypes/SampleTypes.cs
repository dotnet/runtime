// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SerializationTestTypes
{
    [DataContract]
    public class CircularLinkOne
    {
        [DataMember]
        public CircularLinkOne Link;

        [DataMember]
        public CircularLinkOne RandomHangingLink;

        public CircularLinkOne() { }

        public CircularLinkOne(bool init)
        {
            Link = new CircularLinkOne();
            Link.Link = new CircularLinkOne();
            Link.Link.Link = this;
            RandomHangingLink = new CircularLinkOne();
            RandomHangingLink.Link = new CircularLinkOne();
            RandomHangingLink.Link.Link = new CircularLinkOneDerived();
            RandomHangingLink.Link.Link.Link = RandomHangingLink;
        }
    }

    [DataContract]
    public class CircularLinkOneDerived : CircularLinkOne
    {
        public CircularLinkOneDerived() { }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(CircularLinkDerived))]
    public class CircularLink
    {
        [DataMember]
        public CircularLink Link;

        [DataMember]
        public CircularLink RandomHangingLink;

        public CircularLink() { }

        public CircularLink(bool init)
        {
            Link = new CircularLink();
            Link.Link = new CircularLink();
            Link.Link.Link = this;
            RandomHangingLink = new CircularLink();
            RandomHangingLink.Link = new CircularLink();
            RandomHangingLink.Link.Link = new CircularLinkDerived();
            RandomHangingLink.Link.Link.Link = RandomHangingLink;
        }
    }

    [DataContract(IsReference = true)]
    public class CircularLinkDerived : CircularLink
    {
        public CircularLinkDerived() { }
        public CircularLinkDerived(bool inti) : base() { }
    }

    [DataContract(IsReference = true)]
    [KnownType("GetKT")]
    public class KT1Base
    {
        [DataMember]
        public KT1Base BData;

        public KT1Base()
        { }
        public KT1Base(bool init)
        {
            BData = new KT1Derived();
        }

        private static Type[] GetKT()
        {
            return new Type[] { typeof(KT1Derived) };
        }

        public override bool Equals(object obj)
        {
            KT1Base kt1 = obj as KT1Base;
            if (kt1 == null) return false;
            if (this.BData == null || kt1.BData == null)
            {
                if (this.BData == null && kt1.BData == null) { return true; }
                else { return false; }
            }
            else
            {
                return this.BData.Equals(kt1.BData);
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    [DataContract(IsReference = true)]
    public class KT1Derived : KT1Base
    {
        [DataMember]
        public string DData;

        public KT1Derived()
        {
            this.DData = "TestData";
        }

        public override bool Equals(object obj)
        {
            KT1Derived kt1 = obj as KT1Derived;
            if (kt1 == null) return false;
            if (this.DData != kt1.DData) return false;
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    [DataContract(IsReference = true)]
    [KnownType("GetKT")]
    public class KT2Base
    {
        [DataMember]
        public KT2Base BData;

        public KT2Base()
        { }
        public KT2Base(bool init)
        {
            BData = new KT2Derived();
        }

        public static Type[] GetKT()
        {
            return new Type[] { typeof(KT2Derived) };
        }

        public override bool Equals(object obj)
        {
            KT2Base kt1 = obj as KT2Base;
            if (kt1 == null) return false;
            if (this.BData == null || kt1.BData == null)
            {
                if (this.BData == null && kt1.BData == null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return this.BData.Equals(kt1.BData);
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    [DataContract(IsReference = true)]
    [KnownType("GetKT")]
    public class KT3BaseKTMReturnsPrivateType
    {
        [DataMember]
        public KT3BaseKTMReturnsPrivateType BData;

        public KT3BaseKTMReturnsPrivateType()
        { }
        public KT3BaseKTMReturnsPrivateType(bool init)
        {
            BData = new KT3DerivedPrivate();
        }

        public static Type[] GetKT()
        {
            return new Type[] { typeof(KT3DerivedPrivate) };
        }

        public override bool Equals(object obj)
        {
            KT3BaseKTMReturnsPrivateType kt1 = obj as KT3BaseKTMReturnsPrivateType;
            if (kt1 == null) return false;
            if (this.BData == null || kt1.BData == null)
            {
                if (this.BData == null && kt1.BData == null) { return true; }
                else { return false; }
            }
            else
            {
                return this.BData.Equals(kt1.BData);
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    [DataContract(IsReference = true)]
    internal class KT3DerivedPrivate : KT3BaseKTMReturnsPrivateType
    {
        [DataMember]
        public string DData;

        public KT3DerivedPrivate()
        {
            this.DData = "TestData";
        }

        public override bool Equals(object obj)
        {
            KT3DerivedPrivate kt1 = obj as KT3DerivedPrivate;
            if (kt1 == null) return false;
            if (this.DData != kt1.DData) return false;
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    [DataContract(IsReference = true)]
    public class KT2Derived : KT2Base
    {
        [DataMember]
        public string DData;

        public KT2Derived()
        {
            this.DData = "TestData";
        }

        public override bool Equals(object obj)
        {
            KT2Derived kt1 = obj as KT2Derived;
            if (kt1 == null) return false;
            if (this.DData != kt1.DData) return false;
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    [CollectionDataContract(IsReference = true)]
    [KnownType(typeof(PublicDC))]
    [KnownType(typeof(PublicDCClassPublicDM))]
    [KnownType(typeof(PublicDCClassPrivateDM_DerivedDCClassPublic))]
    public class CB1 : CollectionBase
    {
        public CB1() { }

        public CB1(bool init)
        {
            this.List.Add(new PublicDC());
            this.List.Add(new PublicDCClassPublicDM());
            this.List.Add(new PublicDCClassPrivateDM_DerivedDCClassPublic());
        }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(PublicDC))]
    [KnownType(typeof(PublicDCClassPublicDM))]
    public class ArrayListWithCDCFilledPublicTypes
    {
        [DataMember]
        public ArrayList List = new ArrayList();

        public ArrayListWithCDCFilledPublicTypes() { }

        public ArrayListWithCDCFilledPublicTypes(bool init)
        {
            this.List.Add(new PublicDC());
            this.List.Add(new PublicDCClassPublicDM(true));
        }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(PublicDC))]
    [KnownType(typeof(PublicDCClassPublicDM))]
    [KnownType(typeof(PublicDCClassPrivateDM_DerivedDCClassPublic))]
    [KnownType(typeof(PrivateDCClassPublicDM_DerivedDCClassPrivate))]
    [KnownType(typeof(PrivateDCClassPrivateDM))]
    [KnownType(typeof(PrivateCallBackSample_IDeserializationCallback))]
    [KnownType(typeof(PrivateCallBackSample_OnDeserialized))]
    [KnownType(typeof(PrivateCallBackSample_OnSerialized))]
    [KnownType(typeof(PrivateDCStruct))]
    [KnownType(typeof(PrivateDefaultCtorIXmlSerializables))]
    [KnownType(typeof(PrivateIXmlSerializables))]
    [KnownType(typeof(Derived_Override_Prop_GetPrivate_Private))]
    [KnownType(typeof(DerivedFromPriC))]
    public class ArrayListWithCDCFilledWithMixedTypes
    {
        [DataMember]
        public ArrayList List = new ArrayList();
        public ArrayListWithCDCFilledWithMixedTypes() { }

        public ArrayListWithCDCFilledWithMixedTypes(bool init)
        {
            this.List.Add(new PublicDC());
            this.List.Add(new PublicDCClassPublicDM(true));
            this.List.Add(new PublicDCClassPrivateDM_DerivedDCClassPublic());

            this.List.Add(new PrivateDCClassPublicDM_DerivedDCClassPrivate());
            this.List.Add(new PrivateDCClassPrivateDM(true));
            this.List.Add(new PrivateCallBackSample_IDeserializationCallback());
            this.List.Add(new PrivateCallBackSample_OnDeserialized());
            this.List.Add(new PrivateCallBackSample_OnSerialized());
            this.List.Add(new PrivateDCStruct(true));
            this.List.Add(new PrivateDefaultCtorIXmlSerializables(true));
            this.List.Add(new PrivateIXmlSerializables());
            this.List.Add(new Derived_Override_Prop_GetPrivate_Private(true));
            this.List.Add(new DerivedFromPriC(100));
        }
    }

    [CollectionDataContract(IsReference = true)]
    [KnownType(typeof(PublicDC))]
    [KnownType(typeof(PublicDCClassPublicDM))]
    [KnownType(typeof(PublicDCClassPrivateDM_DerivedDCClassPublic))]
    public class CollectionBaseWithCDCFilledPublicTypes : CollectionBase
    {
        public CollectionBaseWithCDCFilledPublicTypes() { }

        public CollectionBaseWithCDCFilledPublicTypes(bool init)
        {
            this.List.Add(new PublicDC());
            this.List.Add(new PublicDCClassPublicDM(true));
        }
    }

    [CollectionDataContract(IsReference = true)]
    [KnownType(typeof(PublicDC))]
    [KnownType(typeof(PublicDCClassPublicDM))]
    [KnownType(typeof(PublicDCClassPrivateDM_DerivedDCClassPublic))]
    [KnownType(typeof(PrivateDCClassPublicDM_DerivedDCClassPrivate))]
    [KnownType(typeof(PrivateDCClassPrivateDM))]
    [KnownType(typeof(PrivateCallBackSample_IDeserializationCallback))]
    [KnownType(typeof(PrivateCallBackSample_OnDeserialized))]
    [KnownType(typeof(PrivateCallBackSample_OnSerialized))]
    [KnownType(typeof(PrivateDCStruct))]
    [KnownType(typeof(PrivateDefaultCtorIXmlSerializables))]
    [KnownType(typeof(PrivateIXmlSerializables))]
    [KnownType(typeof(Derived_Override_Prop_GetPrivate_Private))]
    [KnownType(typeof(DerivedFromPriC))]
    public class CollectionBaseWithCDCFilledWithMixedTypes : CollectionBase
    {
        public CollectionBaseWithCDCFilledWithMixedTypes() { }

        public CollectionBaseWithCDCFilledWithMixedTypes(bool init)
        {
            this.List.Add(new PublicDC());
            this.List.Add(new PublicDCClassPublicDM(true));
            this.List.Add(new PublicDCClassPrivateDM_DerivedDCClassPublic());

            this.List.Add(new PrivateDCClassPublicDM_DerivedDCClassPrivate());
            this.List.Add(new PrivateDCClassPrivateDM(true));
            this.List.Add(new PrivateCallBackSample_IDeserializationCallback());
            this.List.Add(new PrivateCallBackSample_OnDeserialized());
            this.List.Add(new PrivateCallBackSample_OnSerialized());
            this.List.Add(new PrivateDCStruct(true));
            this.List.Add(new PrivateDefaultCtorIXmlSerializables(true));
            this.List.Add(new PrivateIXmlSerializables());
            this.List.Add(new Derived_Override_Prop_GetPrivate_Private(true));
            this.List.Add(new DerivedFromPriC(100));
        }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(PublicDC))]
    [KnownType(typeof(PublicDCClassPublicDM))]
    [KnownType(typeof(PublicDCClassPrivateDM_DerivedDCClassPublic))]
    [KnownType(typeof(Guid))]
    [KnownType(typeof(AllTypes))]
    [KnownType(typeof(DateTimeOffset))]
    public class DCHashtableContainerPublic
    {
        [DataMember]
        public Hashtable List = new Hashtable();

        public DCHashtableContainerPublic() { }
        public DCHashtableContainerPublic(bool init)
        {
            this.List.Add(new Guid("2838c886-08d4-4cb4-9995-45f79b4359fe"), new PublicDC());
            this.List.Add(new Guid("881d2d4c-1342-48ee-8403-7ca8ca5b3d18"), new PublicDCClassPublicDM(true));
            this.List.Add(int.MaxValue, int.MinValue);
            this.List.Add("null", null);
            this.List.Add(DateTime.MinValue, DateTime.MaxValue);
            this.List.Add(DateTimeOffset.MaxValue, DateTimeOffset.MinValue);
            this.List.Add(new Guid("26f71fea-e5df-4b55-97a6-0419ec4d2c7e"), new AllTypes());
        }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(PublicDC))]
    [KnownType(typeof(PublicDCClassPublicDM))]
    [KnownType(typeof(PublicDCClassPrivateDM_DerivedDCClassPublic))]
    [KnownType(typeof(PrivateDCClassPublicDM_DerivedDCClassPrivate))]
    [KnownType(typeof(PrivateDCClassPrivateDM))]
    [KnownType(typeof(PrivateCallBackSample_IDeserializationCallback))]
    [KnownType(typeof(PrivateCallBackSample_OnDeserialized))]
    [KnownType(typeof(PrivateCallBackSample_OnSerialized))]
    [KnownType(typeof(PrivateDCStruct))]
    [KnownType(typeof(PrivateDefaultCtorIXmlSerializables))]
    [KnownType(typeof(PrivateIXmlSerializables))]
    [KnownType(typeof(Derived_Override_Prop_GetPrivate_Private))]
    [KnownType(typeof(DerivedFromPriC))]
    [KnownType(typeof(Guid))]
    [KnownType(typeof(AllTypes))]
    [KnownType(typeof(DateTimeOffset))]
    public class DCHashtableContainerMixedTypes
    {
        [DataMember]
        public Hashtable List = new Hashtable();

        public DCHashtableContainerMixedTypes() { }
        public DCHashtableContainerMixedTypes(bool init)
        {
            this.List.Add(new Guid("00000000-0000-0000-0000-000000000000"), new PublicDC());
            this.List.Add(new Guid("00000000-0000-0000-0000-000000000001"), new PublicDCClassPublicDM(true));
            this.List.Add(new Guid("00000000-0000-0000-0000-000000000002"), new PublicDCClassPrivateDM_DerivedDCClassPublic());

            this.List.Add(new Guid("00000000-0000-0000-0000-000000000003"), new PrivateDCClassPublicDM_DerivedDCClassPrivate());
            this.List.Add(new Guid("00000000-0000-0000-0000-000000000004"), new PrivateDCClassPrivateDM(true));
            this.List.Add(new Guid("00000000-0000-0000-0000-000000000005"), new PrivateCallBackSample_IDeserializationCallback());
            this.List.Add(new Guid("00000000-0000-0000-0000-000000000006"), new PrivateCallBackSample_OnDeserialized());
            this.List.Add(new Guid("00000000-0000-0000-0000-000000000007"), new PrivateCallBackSample_OnSerialized());
            this.List.Add(new Guid("00000000-0000-0000-0000-000000000008"), new PrivateDCStruct(true));
            this.List.Add(new Guid("00000000-0000-0000-0000-000000000009"), new PrivateDefaultCtorIXmlSerializables(true));
            this.List.Add(new Guid("00000000-0000-0000-0000-000000000010"), new PrivateIXmlSerializables());
            this.List.Add(new Guid("00000000-0000-0000-0000-000000000011"), new Derived_Override_Prop_GetPrivate_Private(true));
            this.List.Add(new Guid("00000000-0000-0000-0000-000000000012"), new DerivedFromPriC(100));

            this.List.Add(string.Empty, string.Empty);
            this.List.Add("null", null);
            this.List.Add(double.MaxValue, double.MinValue);
            this.List.Add(new DateTime(), DateTime.MaxValue);
            this.List.Add(DateTimeOffset.MaxValue, DateTimeOffset.MinValue);
            this.List.Add(new Guid("0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb"), new AllTypes());
        }
    }

    [DataContract(IsReference = true)]
    public class CustomGenericContainerPrivateType1
    {
        [DataMember]
        private CustomGeneric1<PrivateDC> _data1 = new CustomGeneric1<PrivateDC>();
    }

    [DataContract(IsReference = true)]
    public class CustomGenericContainerPrivateType2
    {
        [DataMember]
        private CustomGeneric2<PrivateDC, PrivateDC> _data1 = new CustomGeneric2<PrivateDC, PrivateDC>();
    }

    [DataContract(IsReference = true)]
    public class CustomGenericContainerPrivateType3
    {
        [DataMember]
        private CustomGeneric2<PublicDC, PublicDCClassPublicDM_DerivedDCClassPrivate> _data1 = new CustomGeneric2<PublicDC, PublicDCClassPublicDM_DerivedDCClassPrivate>();
    }

    [DataContract(IsReference = true)]
    public class CustomGenericContainerPrivateType4
    {
        [DataMember]
        private CustomGeneric2<PublicDC, PublicDCClassPrivateDM> _data1 = new CustomGeneric2<PublicDC, PublicDCClassPrivateDM>();
    }

    [DataContract(IsReference = true)]
    public class CustomGenericContainerPublicType1
    {
        [DataMember]
        public CustomGeneric1<PublicDC> data1 = new CustomGeneric1<PublicDC>();
    }

    [DataContract(IsReference = true)]
    public class CustomGenericContainerPublicType2
    {
        [DataMember]
        public CustomGeneric2<PublicDC, PublicDC> data1 = new CustomGeneric2<PublicDC, PublicDC>();
    }

    [DataContract(IsReference = true)]
    public class CustomGenericContainerPublicType3
    {
        [DataMember]
        public CustomGeneric2<PublicDC, PublicDCClassPublicDM_DerivedDCClassPublic> data1 = new CustomGeneric2<PublicDC, PublicDCClassPublicDM_DerivedDCClassPublic>();
    }

    [DataContract(IsReference = true)]
    public class CustomGenericContainerPublicType4
    {
        [DataMember]
        public CustomGeneric2<PublicDC, PublicDCClassPublicDM> data1 = new CustomGeneric2<PublicDC, PublicDCClassPublicDM>();
    }

    [DataContract(IsReference = true)]
    public class CustomGeneric1<T> where T : new()
    {
        [DataMember]
        public T t = new T();
    }

    [DataContract(IsReference = true)]
    public class CustomGeneric2<T, K>
        where T : new()
        where K : new()
    {
        [DataMember]
        public T t = new T();

        [DataMember]
        public K k = new K();
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(GenericBase<SimpleBaseContainer>))]
    [KnownType(typeof(SimpleBaseContainer))]
    public class GenericContainer
    {
        [DataMember]
        public object GenericData;
        public GenericContainer()
        {
        }
        public GenericContainer(bool init)
        {
            GenericData = new GenericBase<SimpleBaseContainer>();
        }
    }

    [DataContract(IsReference = true)]
    public class GenericBase<T> where T : new()
    {
        [DataMember]
        public object genericData = new T();
    }

    [DataContract(IsReference = true)]
    public class GenericBase2<T, K>
        where T : new()
        where K : new()
    {
        [DataMember]
        public T genericData1 = new T();

        [DataMember]
        public K genericData2 = new K();
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(SimpleBaseDerived))]
    public class SimpleBase
    {
        [DataMember]
        public string BaseData = string.Empty;
    }

    [DataContract(IsReference = true)]
    public class SimpleBaseDerived : SimpleBase
    {
        [DataMember]
        public string DerivedData = string.Empty;
    }

    [DataContract(IsReference = true)]
    public class SimpleBaseDerived2 : SimpleBase
    {
        [DataMember]
        public string DerivedData = string.Empty;
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(SimpleBaseDerived2))]
    public class SimpleBaseContainer
    {
        [DataMember]
        public SimpleBase Base1;

        [DataMember]
        public object Base2;

        public SimpleBaseContainer()
        { }
        public SimpleBaseContainer(bool init)
        {
            Base1 = new SimpleBaseDerived();
            Base2 = new SimpleBaseDerived2();
        }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(PrivateDC))]
    public class DCListPrivateTContainer2
    {
        [DataMember]
        public List<object> ListData = new List<object>();
        public DCListPrivateTContainer2() { ListData.Add(new PrivateDC()); }
    }

    [DataContract(IsReference = true)]
    public class DCListPrivateTContainer
    {
        [DataMember]
        private List<PrivateDC> _listData = new List<PrivateDC>();
        public DCListPrivateTContainer() { _listData.Add(new PrivateDC()); }
    }

    [DataContract(IsReference = true)]
    public class DCListPublicTContainer
    {
        [DataMember]
        public List<PublicDC> ListData = new List<PublicDC>();
        public DCListPublicTContainer() { ListData.Add(new PublicDC()); }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(PrivateDC))]
    [KnownType(typeof(PublicDC))]
    public class DCListMixedTContainer
    {
        [DataMember]
        private List<object> _listData = new List<object>();
        public DCListMixedTContainer()
        {
            _listData.Add(new PublicDC());
            _listData.Add(new PrivateDC());
        }
    }

    [CollectionDataContract(Name = "SampleListImplicitWithDC")]
    [KnownType(typeof(SimpleDCWithRef))]
    public class SampleListImplicitWithDC : IList
    {
        private List<object> _internalList = new List<object>();

        public SampleListImplicitWithDC() { }

        public SampleListImplicitWithDC(bool init)
        {
            _internalList.Add(new DateTime());
            _internalList.Add(TimeSpan.MaxValue);
            _internalList.Add(string.Empty);
            _internalList.Add(double.MaxValue);
            _internalList.Add(double.NegativeInfinity);
            _internalList.Add(new Guid("0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb"));
            _internalList.Add(new SimpleDCWithRef(true));
        }

        public int Add(object value)
        {
            _internalList.Add(value);
            return _internalList.Count;
        }

        public void Clear()
        {
            _internalList.Clear();
        }

        public bool Contains(object value)
        {
            return _internalList.Contains(value);
        }

        public int IndexOf(object value)
        {
            return _internalList.IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            _internalList.Insert(index, value);
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void Remove(object value)
        {
            _internalList.Remove(value);
        }

        public void RemoveAt(int index)
        {
            _internalList.Remove(index);
        }

        public object this[int index]
        {
            get
            {
                return _internalList[index];
            }
            set
            {
                _internalList[index] = value;
            }
        }

        public void CopyTo(Array array, int index)
        {
        }

        public int Count
        {
            get { return _internalList.Count; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public IEnumerator GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(Name = "SampleListImplicitWithoutDC")]
    public class SampleListImplicitWithoutDC : IList
    {
        private List<object> _internalList = new List<object>();

        public SampleListImplicitWithoutDC() { }

        public SampleListImplicitWithoutDC(bool init)
        {
            _internalList.Add(new DateTime());
            _internalList.Add(TimeSpan.MaxValue);
            _internalList.Add(string.Empty);
            _internalList.Add(double.MaxValue);
            _internalList.Add(double.NegativeInfinity);
            _internalList.Add(new Guid("899288c9-8bee-41c1-a6d4-13c477ec1b29"));
        }

        public int Add(object value)
        {
            _internalList.Add(value);
            return _internalList.Count;
        }

        public void Clear()
        {
            _internalList.Clear();
        }

        public bool Contains(object value)
        {
            return _internalList.Contains(value);
        }

        public int IndexOf(object value)
        {
            return _internalList.IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            _internalList.Insert(index, value);
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void Remove(object value)
        {
            _internalList.Remove(value);
        }

        public void RemoveAt(int index)
        {
            _internalList.Remove(index);
        }

        public object this[int index]
        {
            get
            {
                return _internalList[index];
            }
            set
            {
                _internalList[index] = value;
            }
        }

        public void CopyTo(Array array, int index)
        {
        }

        public int Count
        {
            get { return _internalList.Count; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public IEnumerator GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(IsReference = true, Name = "SampleListImplicitWithCDC1", Namespace = "Test", ItemName = "Item")]
    public class SampleListImplicitWithCDC : IList
    {
        private List<object> _internalList = new List<object>();

        public SampleListImplicitWithCDC() { }

        public SampleListImplicitWithCDC(bool init)
        {
            _internalList.Add(new DateTime());
            _internalList.Add(TimeSpan.MaxValue);
            _internalList.Add(string.Empty);
            _internalList.Add(double.MaxValue);
            _internalList.Add(double.NegativeInfinity);
            _internalList.Add(new Guid("0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb"));
        }

        public int Add(object value)
        {
            _internalList.Add(value);
            return _internalList.Count;
        }

        public void Clear()
        {
            _internalList.Clear();
        }

        public bool Contains(object value)
        {
            return _internalList.Contains(value);
        }

        public int IndexOf(object value)
        {
            return _internalList.IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            _internalList.Insert(index, value);
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void Remove(object value)
        {
            _internalList.Remove(value);
        }

        public void RemoveAt(int index)
        {
            _internalList.Remove(index);
        }

        public object this[int index]
        {
            get
            {
                return _internalList[index];
            }
            set
            {
                _internalList[index] = value;
            }
        }

        public void CopyTo(Array array, int index)
        {
        }

        public int Count
        {
            get { return _internalList.Count; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public IEnumerator GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [DataContract(IsReference = true)]
    public class SampleListExplicitWithDC : IList
    {
        private List<object> _internalList = new List<object>();
        public SampleListExplicitWithDC() { }
        public SampleListExplicitWithDC(bool init)
        {
            _internalList.Add(new DateTime());
            _internalList.Add(TimeSpan.MaxValue);
            _internalList.Add(string.Empty);
            _internalList.Add(double.MaxValue);
            _internalList.Add(double.NegativeInfinity);
            _internalList.Add(new Guid("0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb"));
        }

        int IList.Add(object value)
        {
            _internalList.Add(value);
            return _internalList.Count;
        }

        void IList.Clear()
        {
            _internalList.Clear();
        }

        bool IList.Contains(object value)
        {
            return _internalList.Contains(value);
        }

        int IList.IndexOf(object value)
        {
            return _internalList.IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            _internalList.Insert(index, value);
        }

        bool IList.IsFixedSize
        {
            get { return false; }
        }

        bool IList.IsReadOnly
        {
            get { return false; }
        }

        void IList.Remove(object value)
        {
            _internalList.Remove(value);
        }

        void IList.RemoveAt(int index)
        {
            _internalList.Remove(index);
        }

        object IList.this[int index]
        {
            get
            {
                return _internalList[index];
            }
            set
            {
                _internalList[index] = value;
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
        }

        int ICollection.Count
        {
            get { return _internalList.Count; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(Name = "SampleListExplicitWithoutDC")]
    public class SampleListExplicitWithoutDC : IList
    {
        private List<object> _internalList = new List<object>();
        public SampleListExplicitWithoutDC() { }

        public SampleListExplicitWithoutDC(bool init)
        {
            _internalList.Add(new DateTime());
            _internalList.Add(TimeSpan.MaxValue);
            _internalList.Add(string.Empty);
            _internalList.Add(double.MaxValue);
            _internalList.Add(double.NegativeInfinity);
            _internalList.Add(new Guid("0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb"));
        }

        int IList.Add(object value)
        {
            _internalList.Add(value);
            return _internalList.Count;
        }

        void IList.Clear()
        {
            _internalList.Clear();
        }

        bool IList.Contains(object value)
        {
            return _internalList.Contains(value);
        }

        int IList.IndexOf(object value)
        {
            return _internalList.IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            _internalList.Insert(index, value);
        }

        bool IList.IsFixedSize
        {
            get { return false; }
        }

        bool IList.IsReadOnly
        {
            get { return false; }
        }

        void IList.Remove(object value)
        {
            _internalList.Remove(value);
        }

        void IList.RemoveAt(int index)
        {
            _internalList.Remove(index);
        }

        object IList.this[int index]
        {
            get
            {
                return _internalList[index];
            }
            set
            {
                _internalList[index] = value;
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
        }

        int ICollection.Count
        {
            get { return _internalList.Count; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(IsReference = true, Name = "SampleListExplicitWithCDC2", Namespace = "Test", ItemName = "Item")]
    public class SampleListExplicitWithCDC : IList
    {
        private List<object> _internalList = new List<object>();
        public SampleListExplicitWithCDC() { }
        public SampleListExplicitWithCDC(bool init)
        {
            _internalList.Add(new DateTime());
            _internalList.Add(TimeSpan.MaxValue);
            _internalList.Add(string.Empty);
            _internalList.Add(double.MaxValue);
            _internalList.Add(double.NegativeInfinity);
            _internalList.Add(new Guid("0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb"));
        }

        int IList.Add(object value)
        {
            _internalList.Add(value);
            return _internalList.Count;
        }

        void IList.Clear()
        {
            _internalList.Clear();
        }

        bool IList.Contains(object value)
        {
            return _internalList.Contains(value);
        }

        int IList.IndexOf(object value)
        {
            return _internalList.IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            _internalList.Insert(index, value);
        }

        bool IList.IsFixedSize
        {
            get { return false; }
        }

        bool IList.IsReadOnly
        {
            get { return false; }
        }

        void IList.Remove(object value)
        {
            _internalList.Remove(value);
        }

        void IList.RemoveAt(int index)
        {
            _internalList.Remove(index);
        }

        object IList.this[int index]
        {
            get
            {
                return _internalList[index];
            }
            set
            {
                _internalList[index] = value;
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
        }

        int ICollection.Count
        {
            get { return _internalList.Count; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(IsReference = true, Name = "SampleListExplicitWithCDCContainsPrivateDC", Namespace = "Test", ItemName = "Item")]
    [KnownType(typeof(PrivateDC))]
    public class SampleListExplicitWithCDCContainsPrivateDC : IList
    {
        private List<object> _internalList = new List<object>();
        public SampleListExplicitWithCDCContainsPrivateDC() { }
        public SampleListExplicitWithCDCContainsPrivateDC(bool init)
        {
            _internalList.Add(new DateTime());
            _internalList.Add(TimeSpan.MaxValue);
            _internalList.Add(string.Empty);
            _internalList.Add(double.MaxValue);
            _internalList.Add(double.NegativeInfinity);
            _internalList.Add(new Guid("0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb"));
            _internalList.Add(new PrivateDC());
        }

        int IList.Add(object value)
        {
            _internalList.Add(value);
            return _internalList.Count;
        }

        void IList.Clear()
        {
            _internalList.Clear();
        }

        bool IList.Contains(object value)
        {
            return _internalList.Contains(value);
        }

        int IList.IndexOf(object value)
        {
            return _internalList.IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            _internalList.Insert(index, value);
        }

        bool IList.IsFixedSize
        {
            get { return false; }
        }

        bool IList.IsReadOnly
        {
            get { return false; }
        }

        void IList.Remove(object value)
        {
            _internalList.Remove(value);
        }

        void IList.RemoveAt(int index)
        {
            _internalList.Remove(index);
        }

        object IList.this[int index]
        {
            get
            {
                return _internalList[index];
            }
            set
            {
                _internalList[index] = value;
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
        }

        int ICollection.Count
        {
            get { return _internalList.Count; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [DataContract(IsReference = true)]
    public class SampleListTImplicitWithDC : IList<DC>
    {
        private List<DC> _internalList = new List<DC>();
        public SampleListTImplicitWithDC() { }

        public SampleListTImplicitWithDC(bool init)
        {
            DC dc1 = new DC();
            _internalList.Add(dc1);
            _internalList.Add(new DC());
            _internalList.Add(dc1);
        }

        public int IndexOf(DC item)
        {
            return _internalList.IndexOf(item);
        }

        public void Insert(int index, DC item)
        {
            _internalList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _internalList.RemoveAt(index);
        }

        public DC this[int index]
        {
            get
            {
                return _internalList[index];
            }
            set
            {
                _internalList[index] = value;
            }
        }

        public void Add(DC item)
        {
            _internalList.Add(item);
        }

        public void Clear()
        {
            _internalList.Clear();
        }

        public bool Contains(DC item)
        {
            return _internalList.Contains(item);
        }

        public void CopyTo(DC[] array, int arrayIndex)
        {
            _internalList.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _internalList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(DC item)
        {
            return _internalList.Remove(item);
        }

        public IEnumerator<DC> GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(Name = "SampleListTImplicitWithoutDC")]
    public class SampleListTImplicitWithoutDC : IList<DC>
    {
        private List<DC> _internalList = new List<DC>();
        public SampleListTImplicitWithoutDC() { }
        public SampleListTImplicitWithoutDC(bool init)
        {
            DC dc1 = new DC();
            _internalList.Add(dc1);
            _internalList.Add(new DC());
            _internalList.Add(dc1);
        }

        public int IndexOf(DC item)
        {
            return _internalList.IndexOf(item);
        }

        public void Insert(int index, DC item)
        {
            _internalList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _internalList.RemoveAt(index);
        }

        public DC this[int index]
        {
            get
            {
                return _internalList[index];
            }
            set
            {
                _internalList[index] = value;
            }
        }

        public void Add(DC item)
        {
            _internalList.Add(item);
        }

        public void Clear()
        {
            _internalList.Clear();
        }

        public bool Contains(DC item)
        {
            return _internalList.Contains(item);
        }

        public void CopyTo(DC[] array, int arrayIndex)
        {
            _internalList.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _internalList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(DC item)
        {
            return _internalList.Remove(item);
        }

        public IEnumerator<DC> GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(IsReference = true, Name = "SampleListTImplicitWithCDC", Namespace = "Test", ItemName = "Item")]
    public class SampleListTImplicitWithCDC : IList<DC>
    {
        private List<DC> _internalList = new List<DC>();
        public SampleListTImplicitWithCDC() { }
        public SampleListTImplicitWithCDC(bool init)
        {
            DC dc1 = new DC();
            _internalList.Add(dc1);
            _internalList.Add(new DC());
            _internalList.Add(dc1);
        }

        public int IndexOf(DC item)
        {
            return _internalList.IndexOf(item);
        }

        public void Insert(int index, DC item)
        {
            _internalList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _internalList.RemoveAt(index);
        }

        public DC this[int index]
        {
            get
            {
                return _internalList[index];
            }
            set
            {
                _internalList[index] = value;
            }
        }

        public void Add(DC item)
        {
            _internalList.Add(item);
        }

        public void Clear()
        {
            _internalList.Clear();
        }

        public bool Contains(DC item)
        {
            return _internalList.Contains(item);
        }

        public void CopyTo(DC[] array, int arrayIndex)
        {
            _internalList.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _internalList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(DC item)
        {
            return _internalList.Remove(item);
        }

        public IEnumerator<DC> GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [DataContract(IsReference = true)]
    public class SampleListTExplicitWithDC : IList<DC>
    {
        private List<DC> _internalList = new List<DC>();
        public SampleListTExplicitWithDC() { }
        public SampleListTExplicitWithDC(bool init)
        {
            DC dc1 = new DC();
            _internalList.Add(dc1);
            _internalList.Add(new DC());
            _internalList.Add(dc1);
        }

        int IList<DC>.IndexOf(DC item)
        {
            return _internalList.IndexOf(item);
        }

        void IList<DC>.Insert(int index, DC item)
        {
            _internalList.Insert(index, item);
        }

        void IList<DC>.RemoveAt(int index)
        {
            _internalList.RemoveAt(index);
        }

        DC IList<DC>.this[int index]
        {
            get
            {
                return _internalList[index];
            }
            set
            {
                _internalList[index] = value;
            }
        }

        void ICollection<DC>.Add(DC item)
        {
            _internalList.Add(item);
        }

        void ICollection<DC>.Clear()
        {
            _internalList.Clear();
        }

        bool ICollection<DC>.Contains(DC item)
        {
            return _internalList.Contains(item);
        }

        void ICollection<DC>.CopyTo(DC[] array, int arrayIndex)
        {
            _internalList.CopyTo(array, arrayIndex);
        }

        int ICollection<DC>.Count
        {
            get { return _internalList.Count; }
        }

        bool ICollection<DC>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<DC>.Remove(DC item)
        {
            return _internalList.Remove(item);
        }

        IEnumerator<DC> IEnumerable<DC>.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(Name = "SampleListTExplicitWithoutDC")]
    public class SampleListTExplicitWithoutDC : IList<DC>
    {
        private List<DC> _internalList = new List<DC>();
        public SampleListTExplicitWithoutDC() { }
        public SampleListTExplicitWithoutDC(bool init)
        {
            DC dc1 = new DC();
            _internalList.Add(dc1);
            _internalList.Add(new DC());
            _internalList.Add(dc1);
        }

        int IList<DC>.IndexOf(DC item)
        {
            return _internalList.IndexOf(item);
        }

        void IList<DC>.Insert(int index, DC item)
        {
            _internalList.Insert(index, item);
        }

        void IList<DC>.RemoveAt(int index)
        {
            _internalList.RemoveAt(index);
        }

        DC IList<DC>.this[int index]
        {
            get
            {
                return _internalList[index];
            }
            set
            {
                _internalList[index] = value;
            }
        }

        void ICollection<DC>.Add(DC item)
        {
            _internalList.Add(item);
        }

        void ICollection<DC>.Clear()
        {
            _internalList.Clear();
        }

        bool ICollection<DC>.Contains(DC item)
        {
            return _internalList.Contains(item);
        }

        void ICollection<DC>.CopyTo(DC[] array, int arrayIndex)
        {
            _internalList.CopyTo(array, arrayIndex);
        }

        int ICollection<DC>.Count
        {
            get { return _internalList.Count; }
        }

        bool ICollection<DC>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<DC>.Remove(DC item)
        {
            return _internalList.Remove(item);
        }

        IEnumerator<DC> IEnumerable<DC>.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(IsReference = true, Name = "SampleListTExplicitWithCDC", Namespace = "Test", ItemName = "Item")]
    public class SampleListTExplicitWithCDC : IList<DC>
    {
        private List<DC> _internalList = new List<DC>();
        public SampleListTExplicitWithCDC() { }
        public SampleListTExplicitWithCDC(bool init)
        {
            DC dc1 = new DC();
            _internalList.Add(dc1);
            _internalList.Add(new DC());
            _internalList.Add(dc1);
        }

        int IList<DC>.IndexOf(DC item)
        {
            return _internalList.IndexOf(item);
        }

        void IList<DC>.Insert(int index, DC item)
        {
            _internalList.Insert(index, item);
        }

        void IList<DC>.RemoveAt(int index)
        {
            _internalList.RemoveAt(index);
        }

        DC IList<DC>.this[int index]
        {
            get
            {
                return _internalList[index];
            }
            set
            {
                _internalList[index] = value;
            }
        }

        void ICollection<DC>.Add(DC item)
        {
            _internalList.Add(item);
        }

        void ICollection<DC>.Clear()
        {
            _internalList.Clear();
        }

        bool ICollection<DC>.Contains(DC item)
        {
            return _internalList.Contains(item);
        }

        void ICollection<DC>.CopyTo(DC[] array, int arrayIndex)
        {
            _internalList.CopyTo(array, arrayIndex);
        }

        int ICollection<DC>.Count
        {
            get { return _internalList.Count; }
        }

        bool ICollection<DC>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<DC>.Remove(DC item)
        {
            return _internalList.Remove(item);
        }

        IEnumerator<DC> IEnumerable<DC>.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(IsReference = true, Name = "SampleListTExplicitWithCDCContainsPublicDCClassPrivateDM", Namespace = "Test", ItemName = "Item")]
    public class SampleListTExplicitWithCDCContainsPublicDCClassPrivateDM : IList<PublicDCClassPrivateDM>
    {
        private List<PublicDCClassPrivateDM> _internalList = new List<PublicDCClassPrivateDM>();
        public SampleListTExplicitWithCDCContainsPublicDCClassPrivateDM() { }
        public SampleListTExplicitWithCDCContainsPublicDCClassPrivateDM(bool init)
        {
            PublicDCClassPrivateDM dc1 = new PublicDCClassPrivateDM();
            _internalList.Add(dc1);
            _internalList.Add(new PublicDCClassPrivateDM());
            _internalList.Add(dc1);
        }

        int IList<PublicDCClassPrivateDM>.IndexOf(PublicDCClassPrivateDM item)
        {
            return _internalList.IndexOf(item);
        }

        void IList<PublicDCClassPrivateDM>.Insert(int index, PublicDCClassPrivateDM item)
        {
            _internalList.Insert(index, item);
        }

        void IList<PublicDCClassPrivateDM>.RemoveAt(int index)
        {
            _internalList.RemoveAt(index);
        }

        PublicDCClassPrivateDM IList<PublicDCClassPrivateDM>.this[int index]
        {
            get
            {
                return _internalList[index];
            }
            set
            {
                _internalList[index] = value;
            }
        }

        void ICollection<PublicDCClassPrivateDM>.Add(PublicDCClassPrivateDM item)
        {
            _internalList.Add(item);
        }

        void ICollection<PublicDCClassPrivateDM>.Clear()
        {
            _internalList.Clear();
        }

        bool ICollection<PublicDCClassPrivateDM>.Contains(PublicDCClassPrivateDM item)
        {
            return _internalList.Contains(item);
        }

        void ICollection<PublicDCClassPrivateDM>.CopyTo(PublicDCClassPrivateDM[] array, int arrayIndex)
        {
            _internalList.CopyTo(array, arrayIndex);
        }

        int ICollection<PublicDCClassPrivateDM>.Count
        {
            get { return _internalList.Count; }
        }

        bool ICollection<PublicDCClassPrivateDM>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<PublicDCClassPrivateDM>.Remove(PublicDCClassPrivateDM item)
        {
            return _internalList.Remove(item);
        }

        IEnumerator<PublicDCClassPrivateDM> IEnumerable<PublicDCClassPrivateDM>.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(IsReference = true, Name = "SampleListTExplicitWithCDCContainsPrivateDC", Namespace = "Test", ItemName = "Item")]
    public class SampleListTExplicitWithCDCContainsPrivateDC : IList<PrivateDC>
    {
        private List<PrivateDC> _internalList = new List<PrivateDC>();
        public SampleListTExplicitWithCDCContainsPrivateDC() { }
        public SampleListTExplicitWithCDCContainsPrivateDC(bool init)
        {
            PrivateDC dc1 = new PrivateDC();
            _internalList.Add(dc1);
            _internalList.Add(new PrivateDC());
            _internalList.Add(dc1);
        }

        int IList<PrivateDC>.IndexOf(PrivateDC item)
        {
            return _internalList.IndexOf(item);
        }

        void IList<PrivateDC>.Insert(int index, PrivateDC item)
        {
            _internalList.Insert(index, item);
        }

        void IList<PrivateDC>.RemoveAt(int index)
        {
            _internalList.RemoveAt(index);
        }

        PrivateDC IList<PrivateDC>.this[int index]
        {
            get
            {
                return _internalList[index];
            }
            set
            {
                _internalList[index] = value;
            }
        }

        void ICollection<PrivateDC>.Add(PrivateDC item)
        {
            _internalList.Add(item);
        }

        void ICollection<PrivateDC>.Clear()
        {
            _internalList.Clear();
        }

        bool ICollection<PrivateDC>.Contains(PrivateDC item)
        {
            return _internalList.Contains(item);
        }

        void ICollection<PrivateDC>.CopyTo(PrivateDC[] array, int arrayIndex)
        {
            _internalList.CopyTo(array, arrayIndex);
        }

        int ICollection<PrivateDC>.Count
        {
            get { return _internalList.Count; }
        }

        bool ICollection<PrivateDC>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<PrivateDC>.Remove(PrivateDC item)
        {
            return _internalList.Remove(item);
        }

        IEnumerator<PrivateDC> IEnumerable<PrivateDC>.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [DataContract(IsReference = true)]
    public class SampleICollectionTImplicitWithDC : ICollection<DC>
    {
        private List<DC> _internalList = new List<DC>();
        public SampleICollectionTImplicitWithDC() { }

        public SampleICollectionTImplicitWithDC(bool init)
        {
            DC dc1 = new DC();
            _internalList.Add(dc1);
            _internalList.Add(new DC());
            _internalList.Add(dc1);
        }

        public void Add(DC item)
        {
            _internalList.Add(item);
        }

        public void Clear()
        {
            _internalList.Clear();
        }

        public bool Contains(DC item)
        {
            return _internalList.Contains(item);
        }

        public void CopyTo(DC[] array, int arrayIndex)
        {
            _internalList.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _internalList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(DC item)
        {
            return _internalList.Remove(item);
        }

        public IEnumerator<DC> GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(Name = "SampleICollectionTImplicitWithoutDC")]
    public class SampleICollectionTImplicitWithoutDC : ICollection<DC>
    {
        private List<DC> _internalList = new List<DC>();
        public SampleICollectionTImplicitWithoutDC() { }
        public SampleICollectionTImplicitWithoutDC(bool init)
        {
            DC dc1 = new DC();
            _internalList.Add(dc1);
            _internalList.Add(new DC());
            _internalList.Add(dc1);
        }

        public void Add(DC item)
        {
            _internalList.Add(item);
        }

        public void Clear()
        {
            _internalList.Clear();
        }

        public bool Contains(DC item)
        {
            return _internalList.Contains(item);
        }

        public void CopyTo(DC[] array, int arrayIndex)
        {
            _internalList.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _internalList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(DC item)
        {
            return _internalList.Remove(item);
        }

        public IEnumerator<DC> GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(IsReference = true, Name = "SampleICollectionTImplicitWithCDC", Namespace = "Test", ItemName = "Item")]
    public class SampleICollectionTImplicitWithCDC : ICollection<DC>
    {
        private List<DC> _internalList = new List<DC>();
        public SampleICollectionTImplicitWithCDC() { }
        public SampleICollectionTImplicitWithCDC(bool init)
        {
            DC dc1 = new DC();
            _internalList.Add(dc1);
            _internalList.Add(new DC());
            _internalList.Add(dc1);
        }

        public void Add(DC item)
        {
            _internalList.Add(item);
        }

        public void Clear()
        {
            _internalList.Clear();
        }

        public bool Contains(DC item)
        {
            return _internalList.Contains(item);
        }

        public void CopyTo(DC[] array, int arrayIndex)
        {
            _internalList.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _internalList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(DC item)
        {
            return _internalList.Remove(item);
        }

        public IEnumerator<DC> GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [DataContract(IsReference = true)]
    public class SampleICollectionTExplicitWithDC : ICollection<DC>
    {
        private List<DC> _internalList = new List<DC>();
        public SampleICollectionTExplicitWithDC() { }
        public SampleICollectionTExplicitWithDC(bool init)
        {
            DC dc1 = new DC();
            _internalList.Add(dc1);
            _internalList.Add(new DC());
            _internalList.Add(dc1);
        }

        void ICollection<DC>.Add(DC item)
        {
            _internalList.Add(item);
        }

        void ICollection<DC>.Clear()
        {
            _internalList.Clear();
        }

        bool ICollection<DC>.Contains(DC item)
        {
            return _internalList.Contains(item);
        }

        void ICollection<DC>.CopyTo(DC[] array, int arrayIndex)
        {
            _internalList.CopyTo(array, arrayIndex);
        }

        int ICollection<DC>.Count
        {
            get { return _internalList.Count; }
        }

        bool ICollection<DC>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<DC>.Remove(DC item)
        {
            return _internalList.Remove(item);
        }

        IEnumerator<DC> IEnumerable<DC>.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(Name = "SampleICollectionTExplicitWithoutDC")]
    public class SampleICollectionTExplicitWithoutDC : ICollection<DC>
    {
        private List<DC> _internalList = new List<DC>();
        public SampleICollectionTExplicitWithoutDC() { }
        public SampleICollectionTExplicitWithoutDC(bool init)
        {
            DC dc1 = new DC();
            _internalList.Add(dc1);
            _internalList.Add(new DC());
            _internalList.Add(dc1);
        }

        void ICollection<DC>.Add(DC item)
        {
            _internalList.Add(item);
        }

        void ICollection<DC>.Clear()
        {
            _internalList.Clear();
        }

        bool ICollection<DC>.Contains(DC item)
        {
            return _internalList.Contains(item);
        }

        void ICollection<DC>.CopyTo(DC[] array, int arrayIndex)
        {
            _internalList.CopyTo(array, arrayIndex);
        }

        int ICollection<DC>.Count
        {
            get { return _internalList.Count; }
        }

        bool ICollection<DC>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<DC>.Remove(DC item)
        {
            return _internalList.Remove(item);
        }

        IEnumerator<DC> IEnumerable<DC>.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(IsReference = true, Name = "SampleICollectionTExplicitWithCDC", Namespace = "Test", ItemName = "Item")]
    public class SampleICollectionTExplicitWithCDC : ICollection<DC>
    {
        private List<DC> _internalList = new List<DC>();
        public SampleICollectionTExplicitWithCDC() { }
        public SampleICollectionTExplicitWithCDC(bool init)
        {
            DC dc1 = new DC();
            _internalList.Add(dc1);
            _internalList.Add(new DC());
            _internalList.Add(dc1);
        }

        void ICollection<DC>.Add(DC item)
        {
            _internalList.Add(item);
        }

        void ICollection<DC>.Clear()
        {
            _internalList.Clear();
        }

        bool ICollection<DC>.Contains(DC item)
        {
            return _internalList.Contains(item);
        }

        void ICollection<DC>.CopyTo(DC[] array, int arrayIndex)
        {
            _internalList.CopyTo(array, arrayIndex);
        }

        int ICollection<DC>.Count
        {
            get { return _internalList.Count; }
        }

        bool ICollection<DC>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<DC>.Remove(DC item)
        {
            return _internalList.Remove(item);
        }

        IEnumerator<DC> IEnumerable<DC>.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(IsReference = true, Name = "SampleICollectionTExplicitWithCDCContainsPrivateDC", Namespace = "Test", ItemName = "Item")]
    public class SampleICollectionTExplicitWithCDCContainsPrivateDC : ICollection<PrivateDC>
    {
        private List<PrivateDC> _internalList = new List<PrivateDC>();
        public SampleICollectionTExplicitWithCDCContainsPrivateDC() { }
        public SampleICollectionTExplicitWithCDCContainsPrivateDC(bool init)
        {
            PrivateDC dc1 = new PrivateDC();
            _internalList.Add(dc1);
            _internalList.Add(new PrivateDC());
            _internalList.Add(dc1);
        }

        void ICollection<PrivateDC>.Add(PrivateDC item)
        {
            _internalList.Add(item);
        }

        void ICollection<PrivateDC>.Clear()
        {
            _internalList.Clear();
        }

        bool ICollection<PrivateDC>.Contains(PrivateDC item)
        {
            return _internalList.Contains(item);
        }

        void ICollection<PrivateDC>.CopyTo(PrivateDC[] array, int arrayIndex)
        {
            _internalList.CopyTo(array, arrayIndex);
        }

        int ICollection<PrivateDC>.Count
        {
            get { return _internalList.Count; }
        }

        bool ICollection<PrivateDC>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<PrivateDC>.Remove(PrivateDC item)
        {
            return _internalList.Remove(item);
        }

        IEnumerator<PrivateDC> IEnumerable<PrivateDC>.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [DataContract(IsReference = true)]
    public class SampleIEnumerableTImplicitWithDC : IEnumerable<DC>
    {
        private List<DC> _internalList = new List<DC>();
        public SampleIEnumerableTImplicitWithDC() { }

        public SampleIEnumerableTImplicitWithDC(bool init)
        {
            DC dc1 = new DC();
            _internalList.Add(dc1);
            _internalList.Add(new DC());
            _internalList.Add(dc1);
        }

        public IEnumerator<DC> GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(Name = "SampleIEnumerableTImplicitWithoutDC")]
    public class SampleIEnumerableTImplicitWithoutDC : IEnumerable<DC>
    {
        private List<DC> _internalList = new List<DC>();
        public SampleIEnumerableTImplicitWithoutDC() { }
        public SampleIEnumerableTImplicitWithoutDC(bool init)
        {
            DC dc1 = new DC();
            _internalList.Add(dc1);
            _internalList.Add(new DC());
            _internalList.Add(dc1);
        }

        public void Add(DC dc)
        {
            _internalList.Add(dc);
        }

        public IEnumerator<DC> GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(IsReference = true, Name = "SampleIEnumerableTImplicitWithCDC", Namespace = "Test", ItemName = "Item")]
    public class SampleIEnumerableTImplicitWithCDC : IEnumerable<DC>
    {
        private List<DC> _internalList = new List<DC>();
        public SampleIEnumerableTImplicitWithCDC() { }
        public SampleIEnumerableTImplicitWithCDC(bool init)
        {
            DC dc1 = new DC();
            _internalList.Add(dc1);
            _internalList.Add(new DC());
            _internalList.Add(dc1);
        }

        public void Add(DC item)
        {
            _internalList.Add(item);
        }

        public IEnumerator<DC> GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [DataContract(IsReference = true)]
    public class SampleIEnumerableTExplicitWithDC : IEnumerable<DC>
    {
        private List<DC> _internalList = new List<DC>();
        public SampleIEnumerableTExplicitWithDC() { }
        public SampleIEnumerableTExplicitWithDC(bool init)
        {
            DC dc1 = new DC();
            _internalList.Add(dc1);
            _internalList.Add(new DC());
            _internalList.Add(dc1);
        }

        IEnumerator<DC> IEnumerable<DC>.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(Name = "SampleIEnumerableTExplicitWithoutDC")]
    public class SampleIEnumerableTExplicitWithoutDC : IEnumerable<DC>
    {
        private List<DC> _internalList = new List<DC>();
        public SampleIEnumerableTExplicitWithoutDC() { }
        public SampleIEnumerableTExplicitWithoutDC(bool init)
        {
            DC dc1 = new DC();
            _internalList.Add(dc1);
            _internalList.Add(new DC());
            _internalList.Add(dc1);
        }

        public void Add(DC dc)
        {
            _internalList.Add(dc);
        }

        IEnumerator<DC> IEnumerable<DC>.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(IsReference = true, Name = "SampleIEnumerableTExplicitWithCDC", Namespace = "Test", ItemName = "Item")]
    public class SampleIEnumerableTExplicitWithCDC : IEnumerable<DC>
    {
        private List<DC> _internalList = new List<DC>();
        public SampleIEnumerableTExplicitWithCDC() { }
        public SampleIEnumerableTExplicitWithCDC(bool init)
        {
            DC dc1 = new DC();
            _internalList.Add(dc1);
            _internalList.Add(new DC());
            _internalList.Add(dc1);
        }

        public void Add(DC dc)
        {
            _internalList.Add(dc);
        }

        IEnumerator<DC> IEnumerable<DC>.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(IsReference = true, Name = "SampleIEnumerableTExplicitWithCDCContainsPrivateDC", Namespace = "Test", ItemName = "Item")]
    public class SampleIEnumerableTExplicitWithCDCContainsPrivateDC : IEnumerable<PrivateDC>
    {
        private List<PrivateDC> _internalList = new List<PrivateDC>();
        public SampleIEnumerableTExplicitWithCDCContainsPrivateDC() { }
        public SampleIEnumerableTExplicitWithCDCContainsPrivateDC(bool init)
        {
            PrivateDC dc1 = new PrivateDC();
            _internalList.Add(dc1);
            _internalList.Add(new PrivateDC());
            _internalList.Add(dc1);
        }

        public void Add(object PrivateDC)
        {
            _internalList.Add((PrivateDC)PrivateDC);
        }

        IEnumerator<PrivateDC> IEnumerable<PrivateDC>.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [DataContract(IsReference = true)]
    public class SampleICollectionImplicitWithDC : ICollection
    {
        private List<object> _internalList = new List<object>();

        public SampleICollectionImplicitWithDC() { }

        public SampleICollectionImplicitWithDC(bool init)
        {
            _internalList.Add(new DateTime());
            _internalList.Add(TimeSpan.MaxValue);
            _internalList.Add(string.Empty);
            _internalList.Add(double.MaxValue);
            _internalList.Add(double.NegativeInfinity);
            _internalList.Add(new Guid("0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb"));
        }
        public int Add(object value)
        {
            _internalList.Add(value);
            return _internalList.Count;
        }

        public void CopyTo(Array array, int index)
        {
        }

        public int Count
        {
            get { return _internalList.Count; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public IEnumerator GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(Name = "SampleICollectionImplicitWithoutDC")]
    public class SampleICollectionImplicitWithoutDC : ICollection
    {
        private List<object> _internalList = new List<object>();
        public SampleICollectionImplicitWithoutDC() { }

        public SampleICollectionImplicitWithoutDC(bool init)
        {
            _internalList.Add(new DateTime());
            _internalList.Add(TimeSpan.MaxValue);
            _internalList.Add(string.Empty);
            _internalList.Add(double.MaxValue);
            _internalList.Add(double.NegativeInfinity);
            _internalList.Add(new Guid("0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb"));
        }
        public int Add(object value)
        {
            _internalList.Add(value);
            return _internalList.Count;
        }

        public void CopyTo(Array array, int index)
        {
        }

        public int Count
        {
            get { return _internalList.Count; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public IEnumerator GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(IsReference = true, Name = "SampleListImplicitWithCDC2", Namespace = "Test", ItemName = "Item")]
    public class SampleICollectionImplicitWithCDC : ICollection
    {
        private List<object> _internalList = new List<object>();
        public SampleICollectionImplicitWithCDC() { }

        public SampleICollectionImplicitWithCDC(bool init)
        {
            _internalList.Add(new DateTime());
            _internalList.Add(TimeSpan.MaxValue);
            _internalList.Add(string.Empty);
            _internalList.Add(double.MaxValue);
            _internalList.Add(double.NegativeInfinity);
            _internalList.Add(new Guid("0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb"));
        }
        public int Add(object value)
        {
            _internalList.Add(value);
            return _internalList.Count;
        }

        public void CopyTo(Array array, int index)
        {
        }

        public int Count
        {
            get { return _internalList.Count; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public IEnumerator GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [DataContract(IsReference = true)]
    public class SampleICollectionExplicitWithDC : ICollection
    {
        private List<object> _internalList = new List<object>();
        public SampleICollectionExplicitWithDC() { }
        public SampleICollectionExplicitWithDC(bool init)
        {
            _internalList.Add(new DateTime());
            _internalList.Add(TimeSpan.MaxValue);
            _internalList.Add(string.Empty);
            _internalList.Add(double.MaxValue);
            _internalList.Add(double.NegativeInfinity);
            _internalList.Add(new Guid("0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb"));
        }
        public int Add(object value)
        {
            _internalList.Add(value);
            return _internalList.Count;
        }

        void ICollection.CopyTo(Array array, int index)
        {
        }

        int ICollection.Count
        {
            get { return _internalList.Count; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(Name = "SampleICollectionExplicitWithoutDC")]
    public class SampleICollectionExplicitWithoutDC : ICollection
    {
        private List<object> _internalList = new List<object>();
        public SampleICollectionExplicitWithoutDC() { }

        public SampleICollectionExplicitWithoutDC(bool init)
        {
            _internalList.Add(new DateTime());
            _internalList.Add(TimeSpan.MaxValue);
            _internalList.Add(string.Empty);
            _internalList.Add(double.MaxValue);
            _internalList.Add(double.NegativeInfinity);
            _internalList.Add(new Guid("0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb"));
        }

        public int Add(object value)
        {
            _internalList.Add(value);
            return _internalList.Count;
        }

        void ICollection.CopyTo(Array array, int index)
        {
        }

        int ICollection.Count
        {
            get { return _internalList.Count; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(IsReference = true, Name = "SampleListExplicitWithCDC1", Namespace = "Test", ItemName = "Item")]
    public class SampleICollectionExplicitWithCDC : ICollection
    {
        private List<object> _internalList = new List<object>();
        public SampleICollectionExplicitWithCDC() { }
        public SampleICollectionExplicitWithCDC(bool init)
        {
            _internalList.Add(new DateTime());
            _internalList.Add(TimeSpan.MaxValue);
            _internalList.Add(string.Empty);
            _internalList.Add(double.MaxValue);
            _internalList.Add(double.NegativeInfinity);
            _internalList.Add(new Guid("0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb"));
        }

        public int Add(object value)
        {
            _internalList.Add(value);
            return _internalList.Count;
        }

        void ICollection.CopyTo(Array array, int index)
        {
        }

        int ICollection.Count
        {
            get { return _internalList.Count; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(IsReference = true, Name = "SampleICollectionExplicitWithCDCContainsPrivateDC", Namespace = "Test", ItemName = "Item")]
    [KnownType(typeof(PrivateDC))]
    public class SampleICollectionExplicitWithCDCContainsPrivateDC : ICollection
    {
        private List<object> _internalList = new List<object>();
        public SampleICollectionExplicitWithCDCContainsPrivateDC()
        {
        }

        public SampleICollectionExplicitWithCDCContainsPrivateDC(bool init)
        {
            _internalList.Add(new DateTime());
            _internalList.Add(TimeSpan.MaxValue);
            _internalList.Add(string.Empty);
            _internalList.Add(double.MaxValue);
            _internalList.Add(double.NegativeInfinity);
            _internalList.Add(new Guid("0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb"));
            _internalList.Add(new PrivateDC());
        }

        public int Add(object value)
        {
            _internalList.Add(value);
            return _internalList.Count;
        }

        void ICollection.CopyTo(Array array, int index)
        {
        }

        int ICollection.Count
        {
            get { return _internalList.Count; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [DataContract(IsReference = true)]
    public class SampleIEnumerableImplicitWithDC : IEnumerable
    {
        private List<object> _internalList = new List<object>();

        public SampleIEnumerableImplicitWithDC() { }

        public SampleIEnumerableImplicitWithDC(bool init)
        {
            _internalList.Add(new DateTime());
            _internalList.Add(TimeSpan.MaxValue);
            _internalList.Add(string.Empty);
            _internalList.Add(double.MaxValue);
            _internalList.Add(double.NegativeInfinity);
            _internalList.Add(new Guid("0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb"));
        }
        public int Add(object value)
        {
            _internalList.Add(value);
            return _internalList.Count;
        }

        public IEnumerator GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(Name = "SampleIEnumerableImplicitWithoutDC")]
    public class SampleIEnumerableImplicitWithoutDC : IEnumerable
    {
        private List<object> _internalList = new List<object>();

        public SampleIEnumerableImplicitWithoutDC() { }

        public SampleIEnumerableImplicitWithoutDC(bool init)
        {
            _internalList.Add(new DateTime());
            _internalList.Add(TimeSpan.MaxValue);
            _internalList.Add(string.Empty);
            _internalList.Add(double.MaxValue);
            _internalList.Add(double.NegativeInfinity);
            _internalList.Add(new Guid("0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb"));
        }
        public int Add(object value)
        {
            _internalList.Add(value);
            return _internalList.Count;
        }

        public IEnumerator GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(IsReference = true, Name = "SampleListImplicitWithCDC3", Namespace = "Test", ItemName = "Item")]
    public class SampleIEnumerableImplicitWithCDC : IEnumerable
    {
        private List<object> _internalList = new List<object>();
        public SampleIEnumerableImplicitWithCDC() { }

        public SampleIEnumerableImplicitWithCDC(bool init)
        {
            _internalList.Add(new DateTime());
            _internalList.Add(TimeSpan.MaxValue);
            _internalList.Add(string.Empty);
            _internalList.Add(double.MaxValue);
            _internalList.Add(double.NegativeInfinity);
            _internalList.Add(new Guid("0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb"));
        }

        public int Add(object value)
        {
            _internalList.Add(value);
            return _internalList.Count;
        }

        public IEnumerator GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [DataContract(IsReference = true)]
    public class SampleIEnumerableExplicitWithDC : IEnumerable
    {
        private List<object> _internalList = new List<object>();
        public SampleIEnumerableExplicitWithDC() { }
        public SampleIEnumerableExplicitWithDC(bool init)
        {
            _internalList.Add(new DateTime());
            _internalList.Add(TimeSpan.MaxValue);
            _internalList.Add(string.Empty);
            _internalList.Add(double.MaxValue);
            _internalList.Add(double.NegativeInfinity);
            _internalList.Add(new Guid("0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb"));
        }

        public void Add(object value)
        {
            _internalList.Add(value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(Name = "SampleIEnumerableExplicitWithoutDC")]
    public class SampleIEnumerableExplicitWithoutDC : IEnumerable
    {
        private List<object> _internalList = new List<object>();
        public SampleIEnumerableExplicitWithoutDC() { }

        public SampleIEnumerableExplicitWithoutDC(bool init)
        {
            _internalList.Add(new DateTime());
            _internalList.Add(TimeSpan.MaxValue);
            _internalList.Add(string.Empty);
            _internalList.Add(double.MaxValue);
            _internalList.Add(double.NegativeInfinity);
            _internalList.Add(new Guid("0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb"));
        }

        public int Add(object value)
        {
            _internalList.Add(value);
            return _internalList.Count;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(IsReference = true, Name = "SampleIEnumerableExplicitWithCDC", Namespace = "Test", ItemName = "Item")]
    public class SampleIEnumerableExplicitWithCDC : IEnumerable
    {
        private List<object> _internalList = new List<object>();
        public SampleIEnumerableExplicitWithCDC() { }
        public SampleIEnumerableExplicitWithCDC(bool init)
        {
            _internalList.Add(new DateTime());
            _internalList.Add(TimeSpan.MaxValue);
            _internalList.Add(string.Empty);
            _internalList.Add(double.MaxValue);
            _internalList.Add(double.NegativeInfinity);
            _internalList.Add(new Guid("0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb"));
        }

        public int Add(object value)
        {
            _internalList.Add(value);
            return _internalList.Count;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(IsReference = true, Name = "SampleIEnumerableExplicitWithCDCContainsPrivateDC", Namespace = "Test", ItemName = "Item")]
    [KnownType(typeof(PrivateDC))]
    public class SampleIEnumerableExplicitWithCDCContainsPrivateDC : IEnumerable
    {
        private List<object> _internalList = new List<object>();
        public SampleIEnumerableExplicitWithCDCContainsPrivateDC() { }
        public SampleIEnumerableExplicitWithCDCContainsPrivateDC(bool init)
        {
            _internalList.Add(new DateTime());
            _internalList.Add(TimeSpan.MaxValue);
            _internalList.Add(string.Empty);
            _internalList.Add(double.MaxValue);
            _internalList.Add(double.NegativeInfinity);
            _internalList.Add(new Guid("0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb"));
            _internalList.Add(new PrivateDC());
        }

        public int Add(object value)
        {
            _internalList.Add(value);
            return _internalList.Count;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    [CollectionDataContract(IsReference = true, ItemName = "DictItem", KeyName = "DictKey", Name = "MyIDictionary4", Namespace = "MyDictNS1", ValueName = "DictValue")]
    [KnownType(typeof(PublicDC))]
    public class MyIDictionaryContainsPublicDC : IDictionary
    {
        private Dictionary<object, object> _data = new Dictionary<object, object>();

        public MyIDictionaryContainsPublicDC()
        {
        }
        public MyIDictionaryContainsPublicDC(bool init)
        {
            _data.Add(new PublicDC(), new PublicDC());
        }

        public void Add(object key, object value)
        {
            _data.Add(key, value);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public bool Contains(object key)
        {
            return _data.ContainsKey(key);
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public ICollection Keys
        {
            get { return _data.Keys; }
        }

        public void Remove(object key)
        {
            _data.Remove(key);
        }

        public ICollection Values
        {
            get { return _data.Keys; }
        }

        public object this[object key]
        {
            get
            {
                return _data[key];
            }
            set
            {
                _data[key] = value;
            }
        }

        public void CopyTo(Array array, int index)
        {
            throw new Exception("TEST EXCEPTION!!!!: CopyTo method or operation is not implemented.");
        }

        public int Count
        {
            get { return _data.Count; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return this; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }
    }

    [CollectionDataContract(IsReference = true, ItemName = "DictItem", KeyName = "DictKey", Name = "MyIDictionary2", Namespace = "MyDictNS1", ValueName = "DictValue")]
    [KnownType(typeof(PublicDC))]
    public class MyIDictionaryContainsPublicDCExplicit : IDictionary
    {
        private Dictionary<object, object> _data = new Dictionary<object, object>();

        public MyIDictionaryContainsPublicDCExplicit()
        {
        }

        public MyIDictionaryContainsPublicDCExplicit(bool init)
        {
            _data.Add(new PublicDC(), new PublicDC());
        }

        void IDictionary.Add(object key, object value)
        {
            _data.Add(key, value);
        }

        void IDictionary.Clear()
        {
            _data.Clear();
        }

        bool IDictionary.Contains(object key)
        {
            return _data.ContainsKey(key);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        bool IDictionary.IsFixedSize
        {
            get { return false; }
        }

        bool IDictionary.IsReadOnly
        {
            get { return true; }
        }

        ICollection IDictionary.Keys
        {
            get { return _data.Keys; }
        }

        void IDictionary.Remove(object key)
        {
            _data.Remove(key);
        }

        ICollection IDictionary.Values
        {
            get { return _data.Keys; }
        }

        object IDictionary.this[object key]
        {
            get
            {
                return _data[key];
            }
            set
            {
                _data[key] = value;
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            throw new Exception("TEST EXCEPTION!!!!: CopyTo method or operation is not implemented.");
        }

        int ICollection.Count
        {
            get { return _data.Count; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { return this; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }
    }

    [CollectionDataContract(IsReference = true, ItemName = "DictItem", KeyName = "DictKey", Name = "MyIDictionary3", Namespace = "MyDictNS2", ValueName = "DictValue")]
    [KnownType(typeof(PrivateDC))]
    [KnownType(typeof(PublicDCClassPrivateDM))]
    public class MyIDictionaryContainsPrivateDC : IDictionary
    {
        private Dictionary<object, object> _data = new Dictionary<object, object>();

        public MyIDictionaryContainsPrivateDC()
        { }
        public MyIDictionaryContainsPrivateDC(bool init)
        {
            _data.Add(new PrivateDC(), new PrivateDC());
            _data.Add(new PublicDCClassPrivateDM(), new PublicDCClassPrivateDM());
        }

        public void Add(object key, object value)
        {
            _data.Add(key, value);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public bool Contains(object key)
        {
            return _data.ContainsKey(key);
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public ICollection Keys
        {
            get { return _data.Keys; }
        }

        public void Remove(object key)
        {
            _data.Remove(key);
        }

        public ICollection Values
        {
            get { return _data.Keys; }
        }

        public object this[object key]
        {
            get
            {
                return _data[key];
            }
            set
            {
                _data[key] = value;
            }
        }

        public void CopyTo(Array array, int index)
        {
            throw new Exception("TEST EXCEPTION!!!!: CopyTo method or operation is not implemented.");
        }

        public int Count
        {
            get { return _data.Count; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return this; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }
    }

    [CollectionDataContract(Name = "MyGenericIDictionaryKVContainsPublicDC")]
    public class MyGenericIDictionaryKVContainsPublicDC : IDictionary<PublicDC, PublicDC>
    {
        private Dictionary<PublicDC, PublicDC> _data = new Dictionary<PublicDC, PublicDC>();

        public MyGenericIDictionaryKVContainsPublicDC()
        { }
        public MyGenericIDictionaryKVContainsPublicDC(bool init)
        {
            _data.Add(new PublicDC(), new PublicDC());
        }

        public void Add(PublicDC key, PublicDC value)
        {
            _data.Add(key, value);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public bool Contains(PublicDC key)
        {
            return _data.ContainsKey(key);
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public ICollection Keys
        {
            get { return _data.Keys; }
        }

        public void Remove(PublicDC key)
        {
            _data.Remove(key);
        }

        public ICollection Values
        {
            get { return _data.Keys; }
        }

        public PublicDC this[PublicDC key]
        {
            get
            {
                return _data[key];
            }
            set
            {
                _data[key] = value;
            }
        }

        public void CopyTo(Array array, int index)
        {
            throw new Exception("TEST EXCEPTION!!!!: CopyTo method or operation is not implemented.");
        }

        public int Count
        {
            get { return _data.Count; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return this; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        public bool ContainsKey(PublicDC key)
        {
            return _data.ContainsKey(key);
        }

        ICollection<PublicDC> IDictionary<PublicDC, PublicDC>.Keys
        {
            get { return _data.Keys; }
        }

        bool IDictionary<PublicDC, PublicDC>.Remove(PublicDC key)
        {
            return _data.Remove(key);
        }

        public bool TryGetValue(PublicDC key, out PublicDC value)
        {
            return _data.TryGetValue(key, out value);
        }

        ICollection<PublicDC> IDictionary<PublicDC, PublicDC>.Values
        {
            get { return _data.Values; }
        }

        PublicDC IDictionary<PublicDC, PublicDC>.this[PublicDC key]
        {
            get
            {
                return _data[key];
            }
            set
            {
                _data[key] = value;
            }
        }

        public void Add(KeyValuePair<PublicDC, PublicDC> item)
        {
            _data.Add(item.Key, item.Value);
        }

        public bool Contains(KeyValuePair<PublicDC, PublicDC> item)
        {
            return _data.ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<PublicDC, PublicDC>[] array, int arrayIndex)
        {
            throw new Exception("TEST EXCEPTION!!: CopyTO: method or operation is not implemented.");
        }

        public bool Remove(KeyValuePair<PublicDC, PublicDC> item)
        {
            return _data.Remove(item.Key);
        }

        IEnumerator<KeyValuePair<PublicDC, PublicDC>> IEnumerable<KeyValuePair<PublicDC, PublicDC>>.GetEnumerator()
        {
            return _data.GetEnumerator();
        }
    }

    [CollectionDataContract(Name = "MyGenericIDictionaryKVContainsPublicDCExplicit")]
    public class MyGenericIDictionaryKVContainsPublicDCExplicit : IDictionary<PublicDC, PublicDC>
    {
        private Dictionary<PublicDC, PublicDC> _data = new Dictionary<PublicDC, PublicDC>();

        public MyGenericIDictionaryKVContainsPublicDCExplicit()
        {
        }
        public MyGenericIDictionaryKVContainsPublicDCExplicit(bool init)
        {
            _data.Add(new PublicDC(), new PublicDC());
        }

        void IDictionary<PublicDC, PublicDC>.Add(PublicDC key, PublicDC value)
        {
            _data.Add(key, value);
        }

        void ICollection<KeyValuePair<PublicDC, PublicDC>>.Clear()
        {
            _data.Clear();
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public ICollection Keys
        {
            get { return _data.Keys; }
        }

        public void Remove(PublicDC key)
        {
            _data.Remove(key);
        }

        public ICollection Values
        {
            get { return _data.Keys; }
        }

        public PublicDC this[PublicDC key]
        {
            get
            {
                return _data[key];
            }
            set
            {
                _data[key] = value;
            }
        }

        public void CopyTo(Array array, int index)
        {
            throw new Exception("TEST EXCEPTION!!!!: CopyTo method or operation is not implemented.");
        }

        int ICollection<KeyValuePair<PublicDC, PublicDC>>.Count
        {
            get { return _data.Count; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return this; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        bool IDictionary<PublicDC, PublicDC>.ContainsKey(PublicDC key)
        {
            return _data.ContainsKey(key);
        }

        ICollection<PublicDC> IDictionary<PublicDC, PublicDC>.Keys
        {
            get { return _data.Keys; }
        }

        bool IDictionary<PublicDC, PublicDC>.Remove(PublicDC key)
        {
            return _data.Remove(key);
        }

        bool IDictionary<PublicDC, PublicDC>.TryGetValue(PublicDC key, out PublicDC value)
        {
            return _data.TryGetValue(key, out value);
        }

        ICollection<PublicDC> IDictionary<PublicDC, PublicDC>.Values
        {
            get { return _data.Values; }
        }

        PublicDC IDictionary<PublicDC, PublicDC>.this[PublicDC key]
        {
            get
            {
                return _data[key];
            }
            set
            {
                _data[key] = value;
            }
        }

        void ICollection<KeyValuePair<PublicDC, PublicDC>>.Add(KeyValuePair<PublicDC, PublicDC> item)
        {
            _data.Add(item.Key, item.Value);
        }

        bool ICollection<KeyValuePair<PublicDC, PublicDC>>.Contains(KeyValuePair<PublicDC, PublicDC> item)
        {
            return _data.ContainsKey(item.Key);
        }

        void ICollection<KeyValuePair<PublicDC, PublicDC>>.CopyTo(KeyValuePair<PublicDC, PublicDC>[] array, int arrayIndex)
        {
            throw new Exception("TEST EXCEPTION!!: CopyTO: method or operation is not implemented.");
        }

        bool ICollection<KeyValuePair<PublicDC, PublicDC>>.Remove(KeyValuePair<PublicDC, PublicDC> item)
        {
            return _data.Remove(item.Key);
        }

        IEnumerator<KeyValuePair<PublicDC, PublicDC>> IEnumerable<KeyValuePair<PublicDC, PublicDC>>.GetEnumerator()
        {
            return _data.GetEnumerator();
        }
    }

    [CollectionDataContract(IsReference = true, ItemName = "DictItem", KeyName = "DictKey", Name = "MyGenericIDictionaryKVContainsPrivateDC", Namespace = "MyDictNS", ValueName = "DictValue")]
    [KnownType(typeof(PrivateDC))]
    public class MyGenericIDictionaryKVContainsPrivateDC : IDictionary<object, object>
    {
        private Dictionary<object, object> _data = new Dictionary<object, object>();

        public MyGenericIDictionaryKVContainsPrivateDC()
        {
        }

        public MyGenericIDictionaryKVContainsPrivateDC(bool init)
        {
            _data.Add(new PrivateDC(), new PrivateDC());
        }

        public void Add(object key, object value)
        {
            _data.Add(key, value);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public bool Contains(object key)
        {
            return _data.ContainsKey(key);
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public ICollection Keys
        {
            get { return _data.Keys; }
        }

        public void Remove(object key)
        {
            _data.Remove(key);
        }

        public ICollection Values
        {
            get { return _data.Keys; }
        }

        public object this[object key]
        {
            get
            {
                return _data[key];
            }
            set
            {
                _data[key] = value;
            }
        }

        public void CopyTo(Array array, int index)
        {
            throw new Exception("TEST EXCEPTION!!!!: CopyTo method or operation is not implemented.");
        }

        public int Count
        {
            get { return _data.Count; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return this; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        public bool ContainsKey(object key)
        {
            return _data.ContainsKey(key);
        }

        ICollection<object> IDictionary<object, object>.Keys
        {
            get { return _data.Keys; }
        }

        bool IDictionary<object, object>.Remove(object key)
        {
            return _data.Remove(key);
        }

        public bool TryGetValue(object key, out object value)
        {
            return _data.TryGetValue(key, out value);
        }

        ICollection<object> IDictionary<object, object>.Values
        {
            get { return _data.Values; }
        }

        object IDictionary<object, object>.this[object key]
        {
            get
            {
                return _data[key];
            }
            set
            {
                _data[key] = value;
            }
        }

        public void Add(KeyValuePair<object, object> item)
        {
            _data.Add(item.Key, item.Value);
        }

        public bool Contains(KeyValuePair<object, object> item)
        {
            return _data.ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<object, object>[] array, int arrayIndex)
        {
            throw new Exception("TEST EXCEPTION!!: CopyTO: method or operation is not implemented.");
        }

        public bool Remove(KeyValuePair<object, object> item)
        {
            return _data.Remove(item.Key);
        }

        IEnumerator<KeyValuePair<object, object>> IEnumerable<KeyValuePair<object, object>>.GetEnumerator()
        {
            return _data.GetEnumerator();
        }
    }

    [DataContract(IsReference = true)]
    public class DCDictionaryPrivateKTContainer
    {
        [DataMember]
        private Dictionary<PrivateDC, PrivateDC> _dictData = new Dictionary<PrivateDC, PrivateDC>();
        public DCDictionaryPrivateKTContainer() { _dictData.Add(new PrivateDC(), new PrivateDC()); }
    }

    [DataContract(IsReference = true)]
    public class DCDictionaryPublicKTContainer
    {
        [DataMember]
        public Dictionary<PublicDC, PublicDC> DictData = new Dictionary<PublicDC, PublicDC>();
        public DCDictionaryPublicKTContainer() { DictData.Add(new PublicDC(), new PublicDC()); }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(PrivateDC))]
    [KnownType(typeof(PublicDC))]
    [KnownType(typeof(PublicDCDerivedPublic))]
    [KnownType(typeof(PublicDCDerivedPrivate))]
    public class DCDictionaryMixedKTContainer1
    {
        [DataMember]
        public Dictionary<object, object> DictData = new Dictionary<object, object>();

        public DCDictionaryMixedKTContainer1()
        {
            DictData.Add(new PrivateDC(), new PublicDC());
            DictData.Add(new PublicDC(), new PrivateDC());
        }

        public static DCDictionaryMixedKTContainer1 CreateInstance()
        {
            DCDictionaryMixedKTContainer1 dict = new DCDictionaryMixedKTContainer1();
            return dict;
        }
    }

    [DataContract(IsReference = true)]
    public class DCDictionaryMixedKTContainer2
    {
        [DataMember]
        private Dictionary<PublicDC, PrivateDC> _dictData = new Dictionary<PublicDC, PrivateDC>();
        public DCDictionaryMixedKTContainer2() { _dictData.Add(new PublicDC(), new PrivateDC()); }
    }

    [DataContract(IsReference = true)]
    public class DCDictionaryMixedKTContainer3
    {
        [DataMember]
        private Dictionary<PrivateDC, PublicDC> _dictData = new Dictionary<PrivateDC, PublicDC>();
        public DCDictionaryMixedKTContainer3() { _dictData.Add(new PrivateDC(), new PublicDC()); }
    }

    [DataContract(IsReference = true)]
    public class DCDictionaryMixedKTContainer4
    {
        [DataMember]
        public Dictionary<PublicDCDerivedPublic, PublicDC> DictData = new Dictionary<PublicDCDerivedPublic, PublicDC>();
        public DCDictionaryMixedKTContainer4() { DictData.Add(new PublicDCDerivedPublic(), new PublicDC()); }
    }

    [DataContract(IsReference = true)]
    public class DCDictionaryMixedKTContainer5
    {
        [DataMember]
        private Dictionary<PublicDCDerivedPrivate, PublicDC> _dictData = new Dictionary<PublicDCDerivedPrivate, PublicDC>();
        public DCDictionaryMixedKTContainer5() { _dictData.Add(new PublicDCDerivedPrivate(), new PublicDC()); }
    }

    [DataContract(IsReference = true)]
    public class DCDictionaryMixedKTContainer6
    {
        [DataMember]
        private Dictionary<PublicDCDerivedPublic, PrivateDC> _dictData = new Dictionary<PublicDCDerivedPublic, PrivateDC>();
        public DCDictionaryMixedKTContainer6() { _dictData.Add(new PublicDCDerivedPublic(), new PrivateDC()); }
    }

    [DataContract(IsReference = true)]
    public class DCDictionaryMixedKTContainer7
    {
        [DataMember]
        private Dictionary<PublicDCDerivedPrivate, PrivateDC> _dictData = new Dictionary<PublicDCDerivedPrivate, PrivateDC>();
        public DCDictionaryMixedKTContainer7() { _dictData.Add(new PublicDCDerivedPrivate(), new PrivateDC()); }
    }


    [DataContract(IsReference = true)]
    public class DCDictionaryMixedKTContainer8
    {
        [DataMember]
        public Dictionary<PublicDCDerivedPublic, PublicDCDerivedPublic> DictData = new Dictionary<PublicDCDerivedPublic, PublicDCDerivedPublic>();
        public DCDictionaryMixedKTContainer8() { DictData.Add(new PublicDCDerivedPublic(), new PublicDCDerivedPublic()); }
    }

    [DataContract(IsReference = true)]
    public class DCDictionaryMixedKTContainer9
    {
        [DataMember]
        private Dictionary<PublicDCDerivedPrivate, PublicDCDerivedPrivate> _dictData = new Dictionary<PublicDCDerivedPrivate, PublicDCDerivedPrivate>();
        public DCDictionaryMixedKTContainer9() { _dictData.Add(new PublicDCDerivedPrivate(), new PublicDCDerivedPrivate()); }
    }

    [DataContract(IsReference = true)]
    public class DCDictionaryMixedKTContainer10
    {
        [DataMember]
        private Dictionary<PublicDCDerivedPublic, PublicDCDerivedPrivate> _dictData = new Dictionary<PublicDCDerivedPublic, PublicDCDerivedPrivate>();
        public DCDictionaryMixedKTContainer10() { _dictData.Add(new PublicDCDerivedPublic(), new PublicDCDerivedPrivate()); }
    }

    [DataContract(IsReference = true)]
    public class DCDictionaryMixedKTContainer11
    {
        [DataMember]
        private Dictionary<PublicDCDerivedPrivate, PublicDCDerivedPublic> _dictData = new Dictionary<PublicDCDerivedPrivate, PublicDCDerivedPublic>();
        public DCDictionaryMixedKTContainer11() { _dictData.Add(new PublicDCDerivedPrivate(), new PublicDCDerivedPublic()); }
    }

    [DataContract(IsReference = true)]
    public class DCDictionaryMixedKTContainer12
    {
        [DataMember]
        private Dictionary<PublicDCClassPrivateDM, PublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM> _dictData = new Dictionary<PublicDCClassPrivateDM, PublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM>();
        public DCDictionaryMixedKTContainer12() { _dictData.Add(new PublicDCClassPrivateDM(), new PublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM()); }
    }

    [DataContract(IsReference = true)]
    public class DCDictionaryMixedKTContainer13
    {
        [DataMember]
        private Dictionary<KT1Base, KT2Base> _dictData = new Dictionary<KT1Base, KT2Base>();
        public DCDictionaryMixedKTContainer13() { _dictData.Add(new KT1Base(), new KT2Base()); }
    }

    [DataContract(IsReference = true)]
    public class DCDictionaryMixedKTContainer14
    {
        private Dictionary<PrivateIXmlSerializables, PrivateDefaultCtorIXmlSerializables> _dictData = new Dictionary<PrivateIXmlSerializables, PrivateDefaultCtorIXmlSerializables>();
        public DCDictionaryMixedKTContainer14() { _dictData.Add(new PrivateIXmlSerializables(), new PrivateDefaultCtorIXmlSerializables(true)); }
    }

    [DataContract(IsReference = true)]
    internal class PrivateDC
    {
        [DataMember]
        public string Data = new Guid("7b4ac88f-972b-43e5-8f6a-5ae64480eaad").ToString();

        public override bool Equals(object obj)
        {
            PrivateDC other = obj as PrivateDC;
            if (other == null) return false;
            if (string.IsNullOrEmpty(other.Data) && string.IsNullOrEmpty(Data)) { return true; }
            return other.Data.Equals(Data);
        }
        public override int GetHashCode()
        {
            return Data.GetHashCode();
        }
    }

    [DataContract(IsReference = true)]
    public class PublicDC
    {
        [DataMember]
        [IgnoreMember]
        public string Data = new Guid("55cb1688-dec7-4106-a6d8-7e57590cb20a").ToString();

        public override bool Equals(object obj)
        {
            PublicDC other = obj as PublicDC;
            if (other == null) return false;
            if (string.IsNullOrEmpty(other.Data) && string.IsNullOrEmpty(Data)) { return true; }
            return other.Data.Equals(Data);
        }
        public override int GetHashCode()
        {
            return Data.GetHashCode();
        }
    }

    [DataContract(IsReference = true)]
    internal class PublicDCDerivedPrivate : PublicDC
    {
    }

    [DataContract(IsReference = true)]
    public class PublicDCDerivedPublic : PublicDC
    {
    }

    [DataContract(IsReference = true)]
    public class DC
    {
        [DataMember]
        public string Data = "TestData";

        [DataMember]
        public DC Next;
    }

    [DataContract(IsReference = true)]
    public class DCWithReadOnlyField
    {
        [DataMember]
        public readonly string Data;
    }

    public class IReadWriteXmlWriteBinHex_EqualityDefined : IXmlSerializable
    {
        private byte[] _bits = System.Text.Encoding.UTF8.GetBytes("hello world");

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }

        public virtual void ReadXml(System.Xml.XmlReader reader)
        {
            byte[] readBits = new byte[_bits.Length];
            reader.Read();
            int readLen = reader.ReadContentAsBinHex(readBits, 0, readBits.Length);

            if (_bits.Length != readLen)
            {
                throw new Exception("Test Code Exception: read content length didnt match expected in IReadWriteXmlWriteBinHex.ReadXml. Read: " + readLen + " Original: " + _bits.Length);
            }

            for (int i = 0; i < readLen; i++)
            {
                if (readBits[i] != _bits[i])
                {
                    throw new Exception("Test Code Exception: read xml content is not correct in IReadWriteXmlWriteBinHex.ReadXml. Read: " + readBits[i] + " Original: " + _bits[i]);
                }
            }
        }

        public virtual void WriteXml(System.Xml.XmlWriter writer)
        {
            byte[] bits = System.Text.Encoding.UTF8.GetBytes("hello world");
            writer.WriteBinHex(bits, 0, bits.Length);
        }

        public override bool Equals(object obj)
        {
            IReadWriteXmlWriteBinHex_EqualityDefined other = obj as IReadWriteXmlWriteBinHex_EqualityDefined;
            if (other == null) { return false; }
            for (int i = 0; i < _bits.Length; i++)
            {
                if (other._bits[i] != _bits[i])
                {
                    return false;
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            return _bits.GetHashCode();
        }
    }

    internal class PrivateIXmlSerializables : IXmlSerializable
    {
        private byte[] _bits = System.Text.Encoding.UTF8.GetBytes("hello world");

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }

        public virtual void ReadXml(System.Xml.XmlReader reader)
        {
            byte[] readBits = new byte[_bits.Length];
            reader.Read();
            int readLen = reader.ReadContentAsBinHex(readBits, 0, readBits.Length);

            if (_bits.Length != readLen)
            {
                throw new Exception("Test Code Exception: read content length didnt match expected in IReadWriteXmlWriteBinHex.ReadXml. Read: " + readLen + " Original: " + _bits.Length);
            }

            for (int i = 0; i < readLen; i++)
            {
                if (readBits[i] != _bits[i])
                {
                    throw new Exception("Test Code Exception: read xml content is not correct in IReadWriteXmlWriteBinHex.ReadXml. Read: " + readBits[i] + " Original: " + _bits[i]);
                }
            }
        }

        public virtual void WriteXml(System.Xml.XmlWriter writer)
        {
            byte[] bits = System.Text.Encoding.UTF8.GetBytes("hello world");
            writer.WriteBinHex(bits, 0, bits.Length);
        }
    }

    public class PrivateDefaultCtorIXmlSerializables : IXmlSerializable
    {
        private PrivateDefaultCtorIXmlSerializables() { }
        public PrivateDefaultCtorIXmlSerializables(bool init) { }

        private byte[] _bits = System.Text.Encoding.UTF8.GetBytes("hello world");

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }

        public virtual void ReadXml(System.Xml.XmlReader reader)
        {
            byte[] readBits = new byte[_bits.Length];
            reader.Read();
            int readLen = reader.ReadContentAsBinHex(readBits, 0, readBits.Length);

            if (_bits.Length != readLen)
            {
                throw new Exception("Test Code Exception: read content length didnt match expected in IReadWriteXmlWriteBinHex.ReadXml. Read: " + readLen + " Original: " + _bits.Length);
            }

            for (int i = 0; i < readLen; i++)
            {
                if (readBits[i] != _bits[i])
                {
                    throw new Exception("Test Code Exception: read xml content is not correct in IReadWriteXmlWriteBinHex.ReadXml. Read: " + readBits[i] + " Original: " + _bits[i]);
                }
            }
        }

        public virtual void WriteXml(System.Xml.XmlWriter writer)
        {
            byte[] bits = System.Text.Encoding.UTF8.GetBytes("hello world");
            writer.WriteBinHex(bits, 0, bits.Length);
        }
    }

    [XmlSchemaProvider("MySchema")]
    public class PublicIXmlSerializablesWithPublicSchemaProvider : IXmlSerializable
    {
        private byte[] _bits = System.Text.Encoding.UTF8.GetBytes("hello world");

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }

        public static XmlQualifiedName MySchema(XmlSchemaSet xs)
        {
            return new XmlQualifiedName("MyData", "MyNameSpace");
        }

        public virtual void ReadXml(System.Xml.XmlReader reader)
        {
            byte[] readBits = new byte[_bits.Length];
            reader.Read();
            int readLen = reader.ReadContentAsBinHex(readBits, 0, readBits.Length);

            if (_bits.Length != readLen)
            {
                throw new Exception("Test Code Exception: read content length didnt match expected in IReadWriteXmlWriteBinHex.ReadXml. Read: " + readLen + " Original: " + _bits.Length);
            }

            for (int i = 0; i < readLen; i++)
            {
                if (readBits[i] != _bits[i])
                {
                    throw new Exception("Test Code Exception: read xml content is not correct in IReadWriteXmlWriteBinHex.ReadXml. Read: " + readBits[i] + " Original: " + _bits[i]);
                }
            }
        }

        public virtual void WriteXml(System.Xml.XmlWriter writer)
        {
            byte[] bits = System.Text.Encoding.UTF8.GetBytes("hello world");
            writer.WriteBinHex(bits, 0, bits.Length);
        }
    }

    [XmlSchemaProvider("MySchema")]
    public class PublicExplicitIXmlSerializablesWithPublicSchemaProvider : IXmlSerializable
    {
        private byte[] _bits = System.Text.Encoding.UTF8.GetBytes("hello world");

        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema()
        {
            throw new NotImplementedException();
        }

        public static XmlQualifiedName MySchema(XmlSchemaSet xs)
        {
            return new XmlQualifiedName("MyData2", "MyNameSpace");
        }

        void IXmlSerializable.ReadXml(System.Xml.XmlReader reader)
        {
            byte[] readBits = new byte[_bits.Length];
            reader.Read();
            int readLen = reader.ReadContentAsBinHex(readBits, 0, readBits.Length);

            if (_bits.Length != readLen)
            {
                throw new Exception("Test Code Exception: read content length didnt match expected in IReadWriteXmlWriteBinHex.ReadXml. Read: " + readLen + " Original: " + _bits.Length);
            }

            for (int i = 0; i < readLen; i++)
            {
                if (readBits[i] != _bits[i])
                {
                    throw new Exception("Test Code Exception: read xml content is not correct in IReadWriteXmlWriteBinHex.ReadXml. Read: " + readBits[i] + " Original: " + _bits[i]);
                }
            }
        }

        void IXmlSerializable.WriteXml(System.Xml.XmlWriter writer)
        {
            byte[] bits = System.Text.Encoding.UTF8.GetBytes("hello world");
            writer.WriteBinHex(bits, 0, bits.Length);
        }
    }

    [XmlSchemaProvider("MySchema")]
    public class PublicIXmlSerializablesWithPrivateSchemaProvider : IXmlSerializable
    {
        private byte[] _bits = System.Text.Encoding.UTF8.GetBytes("hello world");

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }

        private static XmlQualifiedName MySchema(XmlSchemaSet xs)
        {
            return new XmlQualifiedName("MyData3", "MyNameSpace");
        }

        public virtual void ReadXml(System.Xml.XmlReader reader)
        {
            byte[] readBits = new byte[_bits.Length];
            reader.Read();
            int readLen = reader.ReadContentAsBinHex(readBits, 0, readBits.Length);

            if (_bits.Length != readLen)
            {
                throw new Exception("Test Code Exception: read content length didnt match expected in IReadWriteXmlWriteBinHex.ReadXml. Read: " + readLen + " Original: " + _bits.Length);
            }

            for (int i = 0; i < readLen; i++)
            {
                if (readBits[i] != _bits[i])
                {
                    throw new Exception("Test Code Exception: read xml content is not correct in IReadWriteXmlWriteBinHex.ReadXml. Read: " + readBits[i] + " Original: " + _bits[i]);
                }
            }
        }

        public virtual void WriteXml(System.Xml.XmlWriter writer)
        {
            byte[] bits = System.Text.Encoding.UTF8.GetBytes("hello world");
            writer.WriteBinHex(bits, 0, bits.Length);
        }
    }

    [DataContract(IsReference = true)]
    internal class PrivateDCClassPublicDM
    {
        [DataMember]
        public string Data = "Data";

        public PrivateDCClassPublicDM() { }
        public PrivateDCClassPublicDM(bool init) { Data = "No change"; }
    }

    [DataContract(IsReference = true)]
    internal class PrivateDCClassPrivateDM
    {
        [DataMember]
        private string _data;

        public PrivateDCClassPrivateDM() { _data = string.Empty; }
        public PrivateDCClassPrivateDM(bool init) { _data = "No change"; }
    }

    [DataContract(IsReference = true)]
    public class PublicDCClassPublicDM
    {
        [DataMember]
        public string Data;

        public PublicDCClassPublicDM() { }
        public PublicDCClassPublicDM(bool init) { Data = "No change"; }
    }

    [DataContract(IsReference = true)]
    public class PublicDCClassPrivateDM
    {
        [DataMember]
        private string _data;

        public PublicDCClassPrivateDM() { _data = string.Empty; }
        public PublicDCClassPrivateDM(bool init) { _data = "No change"; }
    }

    [DataContract(IsReference = true)]
    public class PublicDCClassInternalDM
    {
        [DataMember]
        internal string Data;

        public PublicDCClassInternalDM() { }
        public PublicDCClassInternalDM(bool init) { Data = "No change"; }
    }

    [DataContract(IsReference = true)]
    public class PublicDCClassMixedDM
    {
        [DataMember]
        public string Data1 = string.Empty;

        [DataMember]
        private string _data2 = string.Empty;

        [DataMember]
        internal string Data3 = string.Empty;

        public PublicDCClassMixedDM() { }
        public PublicDCClassMixedDM(bool init) { Data1 = "No change"; }
    }

    [DataContract(IsReference = true)]
    internal class PublicDCClassPublicDM_DerivedDCClassPrivate : PublicDCClassPublicDM
    {
    }

    [DataContract(IsReference = true)]
    public class PublicDCClassPublicDM_DerivedDCClassPublic : PublicDCClassPublicDM
    {
    }

    [DataContract(IsReference = true)]
    public class PublicDCClassPrivateDM_DerivedDCClassPublic : PublicDCClassPrivateDM
    {
    }

    [DataContract(IsReference = true)]
    internal class PrivateDCClassPublicDM_DerivedDCClassPrivate : PrivateDCClassPublicDM
    {
    }

    [DataContract(IsReference = true)]
    public class PublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM : PublicDCClassPublicDM
    {
        [DataMember]
        private string _derivedData1;

        [DataMember]
        public string DerivedData2;

        public PublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM()
        {
            _derivedData1 = string.Empty;
        }
    }

    [DataContract(IsReference = true)]
    public class Prop_PublicDCClassPublicDM_PublicDCClassPrivateDM
    {
        private PublicDCClassPrivateDM _data;
        [DataMember]
        public PublicDCClassPrivateDM Data
        {
            get { return _data; }
            set { _data = value; }
        }

        public Prop_PublicDCClassPublicDM_PublicDCClassPrivateDM() { }
        public Prop_PublicDCClassPublicDM_PublicDCClassPrivateDM(bool init) { Data = new PublicDCClassPrivateDM(); }
    }

    [DataContract(IsReference = true)]
    public class Prop_SetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM
    {
        internal PublicDCClassPrivateDM _data;
        [DataMember]
        public virtual PublicDCClassPrivateDM Data
        {
            get { return _data; }
            private set { _data = value; }
        }

        public Prop_SetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM() { }
        public Prop_SetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM(bool init) { Data = new PublicDCClassPrivateDM(); }
    }

    [DataContract(IsReference = true)]
    public class Prop_GetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM
    {
        internal PublicDCClassPrivateDM _data;
        [DataMember]
        public PublicDCClassPrivateDM Data
        {
            private get { return _data; }
            set { _data = value; }
        }

        public Prop_GetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM() { }
        public Prop_GetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM(bool init) { Data = new PublicDCClassPrivateDM(); }
    }

    [DataContract(IsReference = true)]
    internal class Prop_PrivateDCClassPublicDM
    {
        private string _data;

        [DataMember]
        public string Data
        {
            get { return _data; }
            set { _data = value; }
        }

        public Prop_PrivateDCClassPublicDM() { }
        public Prop_PrivateDCClassPublicDM(bool init) { Data = "No change"; }
    }

    [DataContract(IsReference = true)]
    internal class Prop_PrivateDCClassPrivateDM
    {
        private string _data;
        [DataMember]
        private string Data
        {
            get { return _data; }
            set { _data = value; }
        }

        public Prop_PrivateDCClassPrivateDM() { }
        public Prop_PrivateDCClassPrivateDM(bool init) { Data = "No change"; }
    }

    [DataContract(IsReference = true)]
    public class Prop_PublicDCClassPublicDM
    {
        private string _data;
        [DataMember]
        public string Data
        {
            get { return _data; }
            set { _data = value; }
        }

        public Prop_PublicDCClassPublicDM() { }
        public Prop_PublicDCClassPublicDM(bool init) { Data = "No change"; }
    }

    [DataContract(IsReference = true)]
    public class Prop_PublicDCClassPrivateDM
    {
        private string _data;
        [DataMember]
        private string Data
        {
            get { return _data; }
            set { _data = value; }
        }

        public Prop_PublicDCClassPrivateDM() { }
        public Prop_PublicDCClassPrivateDM(bool init) { Data = "No change"; }
    }

    [DataContract(IsReference = true)]
    public class Prop_PublicDCClassInternalDM
    {
        private string _data;
        [DataMember]
        internal string Data
        {
            get { return _data; }
            set { _data = value; }
        }

        public Prop_PublicDCClassInternalDM() { }
        public Prop_PublicDCClassInternalDM(bool init) { Data = "No change"; }
    }

    [DataContract(IsReference = true)]
    public class Prop_PublicDCClassMixedDM
    {
        private string _data1;
        private string _data2;
        private string _data3;

        [DataMember]
        public string Data1
        {
            get { return _data1; }
            set { _data1 = value; }
        }

        [DataMember]
        private string Data2
        {
            get { return _data2; }
            set { _data2 = value; }
        }

        [DataMember]
        internal string Data3
        {
            get { return _data3; }
            set { _data3 = value; }
        }

        public Prop_PublicDCClassMixedDM() { }
        public Prop_PublicDCClassMixedDM(bool init) { Data1 = "No change"; }
    }

    [DataContract(IsReference = true)]
    public class Prop_PublicDCClassPublicDM_DerivedDCClassPublic : Prop_PublicDCClassPublicDM
    {
    }

    [DataContract(IsReference = true)]
    public class Prop_PublicDCClassPrivateDM_DerivedDCClassPublic : Prop_PublicDCClassPrivateDM
    {
    }

    [DataContract(IsReference = true)]
    public class Prop_PublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM : Prop_PublicDCClassPublicDM
    {
        [DataMember]
        private string _derivedData1 = string.Empty;

        [DataMember]
        public string DerivedData2;
    }

    [DataContract(IsReference = true)]
    public class Prop_SetPrivate_PublicDCClassPublicDM
    {
        internal string _data;
        [DataMember]
        public virtual string Data
        {
            get { return _data; }
            private set { _data = value; }
        }

        public Prop_SetPrivate_PublicDCClassPublicDM() { }
        public Prop_SetPrivate_PublicDCClassPublicDM(bool init) { Data = "No change"; }
    }

    [DataContract(IsReference = true)]
    public class Prop_GetPrivate_PublicDCClassPublicDM
    {
        internal string _data;
        [DataMember]
        public string Data
        {
            private get { return _data; }
            set { _data = value; }
        }

        public Prop_GetPrivate_PublicDCClassPublicDM() { }
        public Prop_GetPrivate_PublicDCClassPublicDM(bool init) { Data = "No change"; }
    }

    [DataContract(IsReference = true)]
    public class Derived_Override_Prop_All_Public : Prop_SetPrivate_PublicDCClassPublicDM
    {
        [DataMember]
        public new string Data
        {
            get { return _data; }
            private set { _data = value; }
        }

        public Derived_Override_Prop_All_Public() { }
        public Derived_Override_Prop_All_Public(bool init) { Data = "No change"; }
    }

    [DataContract(IsReference = true)]
    public class Derived_Override_Prop_Private : Prop_SetPrivate_PublicDCClassPublicDM
    {
        [DataMember]
        public new string Data
        {
            private get { return _data; }
            set { _data = value; }
        }

        public Derived_Override_Prop_Private() { }
        public Derived_Override_Prop_Private(bool init) { Data = "No change"; }
    }

    [DataContract(IsReference = true)]
    public class Derived_Override_Prop_GetPrivate_All_Public : Prop_GetPrivate_PublicDCClassPublicDM
    {
        [DataMember]
        public new string Data
        {
            get { return _data; }
            private set { _data = value; }
        }

        public Derived_Override_Prop_GetPrivate_All_Public() { }
        public Derived_Override_Prop_GetPrivate_All_Public(bool init) { Data = "No change"; }
    }

    [DataContract(IsReference = true)]
    public class Derived_Override_Prop_GetPrivate_Private : Prop_GetPrivate_PublicDCClassPublicDM
    {
        [DataMember]
        public new string Data
        {
            private get { return _data; }
            set { _data = value; }
        }

        public Derived_Override_Prop_GetPrivate_Private() { }
        public Derived_Override_Prop_GetPrivate_Private(bool init) { Data = "No change"; }
    }

    [DataContract(Name = "DC1_Version")]
    public class DC1_Version1
    {
    }

    [DataContract(Name = "DC2_Version1")]
    public class DC2_Version1
    {
        [DataMember]
        public string Data;
    }

    [DataContract(Name = "DC2_Version4")]
    public class DC2_Version4
    {
        [DataMember]
        private string _data = string.Empty;
    }

    [DataContract(Name = "DC2_Version5")]
    public class DC2_Version5
    {
        private string _data;

        [DataMember]
        public string Data
        {
            get { return _data; }
            private set { _data = value; }
        }
    }

    [DataContract(Name = "DC3_Version1")]
    public class DC3_Version1
    {
        [DataMember]
        public string Data1;
    }

    [DataContract(Name = "DC3_Version2")]
    public class DC3_Version2 : IExtensibleDataObject
    {
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

    [DataContract(Name = "DC3_Version3")]
    public class DC3_Version3 : IExtensibleDataObject
    {
        private ExtensionDataObject _extensionData;

        ExtensionDataObject IExtensibleDataObject.ExtensionData
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

    [DataContract(IsReference = true)]
    public class CallBackSample_OnSerializing_Public
    {
        [DataMember]
        public string Data;

        [OnSerializing]
        public void OnSerializing(System.Runtime.Serialization.StreamingContext context) { }
    }

    [DataContract(IsReference = true)]
    public class CallBackSample_OnSerialized_Public
    {
        [DataMember]
        public string Data;

        [OnSerialized]
        public void OnSerialized(System.Runtime.Serialization.StreamingContext context) { }
    }

    [DataContract(IsReference = true)]
    public class CallBackSample_OnDeserializing_Public
    {
        [DataMember]
        public string Data;

        [OnDeserializing]
        public void OnDeserializing(System.Runtime.Serialization.StreamingContext context) { }
    }

    [DataContract(IsReference = true)]
    public class CallBackSample_OnDeserialized_Public
    {
        [DataMember]
        public string Data;

        [OnDeserialized]
        public void OnDeserialized(System.Runtime.Serialization.StreamingContext context) { }
    }

    [DataContract(IsReference = true)]
    public class CallBackSample_OnSerializing
    {
        [DataMember]
        public string Data;

        [OnSerializing]
        private void OnSerializing(System.Runtime.Serialization.StreamingContext context) { }
    }

    [DataContract(IsReference = true)]
    public class CallBackSample_OnSerialized
    {
        [DataMember]
        public string Data;

        [OnSerialized]
        internal void OnSerialized(System.Runtime.Serialization.StreamingContext context) { }
    }

    [DataContract(IsReference = true)]
    public class CallBackSample_OnDeserializing
    {
        [DataMember]
        public string Data;

        [OnDeserializing]
        private void OnDeserializing(System.Runtime.Serialization.StreamingContext context) { }
    }

    [DataContract(IsReference = true)]
    public class CallBackSample_OnDeserialized
    {
        [DataMember]
        public string Data;

        [OnDeserialized]
        protected internal void OnDeserialized(System.Runtime.Serialization.StreamingContext context) { }
    }

    [DataContract(IsReference = true)]
    public class CallBackSample_IDeserializationCallback : IDeserializationCallback
    {
        [DataMember]
        public string Data;

        public void OnDeserialization(object sender)
        {
        }
    }

    [DataContract(IsReference = true)]
    public class CallBackSample_IDeserializationCallback_Explicit : IDeserializationCallback
    {
        [DataMember]
        public string Data;

        void IDeserializationCallback.OnDeserialization(object sender)
        {
        }
    }

    [DataContract(IsReference = true)]
    internal class PrivateCallBackSample_OnSerialized
    {
        [DataMember]
        public string Data = "Data";

        [OnSerialized]
        internal void OnSerialized(System.Runtime.Serialization.StreamingContext context) { }
    }

    [DataContract(IsReference = true)]
    internal class PrivateCallBackSample_OnDeserialized
    {
        [DataMember]
        public string Data = "Data";

        [OnDeserialized]
        protected internal void OnDeserialized(System.Runtime.Serialization.StreamingContext context) { }
    }

    [DataContract(IsReference = true)]
    internal class PrivateCallBackSample_IDeserializationCallback : IDeserializationCallback
    {
        [DataMember]
        public string Data = "Data";

        public void OnDeserialization(object sender)
        {
        }
    }

    [DataContract(IsReference = true)]
    public class CallBackSample_OnDeserialized_Private_Base
    {
        [DataMember]
        public string Data = "string";

        [OnDeserialized]
        private void OnDeserialized(System.Runtime.Serialization.StreamingContext context)
        {
        }
    }

    [DataContract(IsReference = true)]
    public class CallBackSample_OnDeserialized_Public_Derived : CallBackSample_OnDeserialized_Private_Base
    {
        [DataMember]
        public string Data2 = "string";

        public CallBackSample_OnDeserialized_Public_Derived() { }
        [OnDeserialized]
        public void OnDeserialized(System.Runtime.Serialization.StreamingContext context)
        {
        }
    }

    [CollectionDataContract(IsReference = true)]
    public class CDC_Possitive : IList<string>
    {
        private List<string> _innerList = new List<string>();

        public int IndexOf(string item)
        {
            return _innerList.IndexOf(item);
        }

        public void Insert(int index, string item)
        {
            _innerList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _innerList.RemoveAt(index);
        }

        public string this[int index]
        {
            get
            {
                return _innerList[index];
            }
            set
            {
                _innerList[index] = value;
            }
        }

        public void Add(string item)
        {
            _innerList.Add(item);
        }

        public void Clear()
        {
            _innerList.Clear();
        }

        public bool Contains(string item)
        {
            return _innerList.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            _innerList.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _innerList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(string item)
        {
            return _innerList.Remove(item);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _innerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _innerList.GetEnumerator();
        }

        public static CDC_Possitive CreateInstance()
        {
            CDC_Possitive list = new CDC_Possitive();
            list.Add("112");
            return list;
        }
    }

    [CollectionDataContract(IsReference = true)]
    public class CDC_PrivateAdd : IEnumerable<string>
    {
        private List<string> _innerList = new List<string>();

        public int IndexOf(string item)
        {
            return _innerList.IndexOf(item);
        }

        public void Insert(int index, string item)
        {
            _innerList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _innerList.RemoveAt(index);
        }

        public string this[int index]
        {
            get
            {
                return _innerList[index];
            }
            set
            {
                _innerList[index] = value;
            }
        }

        private void Add(string item)
        {
            _innerList.Add(item.ToString());
        }

        public void Clear()
        {
            _innerList.Clear();
        }

        public bool Contains(string item)
        {
            return _innerList.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            _innerList.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _innerList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(string item)
        {
            return _innerList.Remove(item);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _innerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _innerList.GetEnumerator();
        }

        public static CDC_PrivateAdd CreateInstance()
        {
            CDC_PrivateAdd list = new CDC_PrivateAdd();
            list.Insert(0, "222323");
            list.Insert(1, "222323");
            return list;
        }
    }

    [CollectionDataContract(IsReference = true)]
    public class CDC_PrivateDefaultCtor : IList<string>
    {
        private List<string> _innerList = new List<string>();

        private CDC_PrivateDefaultCtor()
        {
        }

        public CDC_PrivateDefaultCtor(bool init)
        {
        }

        public int IndexOf(string item)
        {
            return _innerList.IndexOf(item);
        }

        public void Insert(int index, string item)
        {
            _innerList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _innerList.RemoveAt(index);
        }

        public string this[int index]
        {
            get
            {
                return _innerList[index];
            }
            set
            {
                _innerList[index] = value;
            }
        }

        public void Add(string item)
        {
            _innerList.Add(item);
        }

        public void Clear()
        {
            _innerList.Clear();
        }

        public bool Contains(string item)
        {
            return _innerList.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            _innerList.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _innerList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(string item)
        {
            return _innerList.Remove(item);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _innerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _innerList.GetEnumerator();
        }
    }

    [CollectionDataContract(IsReference = true)]
    public class Base_Possitive_VirtualAdd : IEnumerable<string>
    {
        private List<string> _innerList = new List<string>();

        public int IndexOf(string item)
        {
            return _innerList.IndexOf(item);
        }

        public void Insert(int index, string item)
        {
            _innerList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _innerList.RemoveAt(index);
        }

        public string this[int index]
        {
            get
            {
                return _innerList[index];
            }
            set
            {
                _innerList[index] = value;
            }
        }

        public virtual void Add(string item)
        {
            _innerList.Add(item.ToString());
        }

        public void Clear()
        {
            _innerList.Clear();
        }

        public bool Contains(string item)
        {
            return _innerList.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            _innerList.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _innerList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(string item)
        {
            return _innerList.Remove(item);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _innerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _innerList.GetEnumerator();
        }

        public static Base_Possitive_VirtualAdd CreateInstance()
        {
            Base_Possitive_VirtualAdd list = new Base_Possitive_VirtualAdd();
            list.Insert(0, "222323");
            list.Insert(1, "222323");
            return list;
        }
    }

    [CollectionDataContract(IsReference = true)]
    public class CDC_NewAddToPrivate : Base_Possitive_VirtualAdd
    {
        private new void Add(string item)
        {
            base.Add(item.ToString());
        }

        public static new CDC_NewAddToPrivate CreateInstance()
        {
            CDC_NewAddToPrivate list = new CDC_NewAddToPrivate();
            list.Insert(0, "223213");
            list.Insert(0, "223213");
            return list;
        }
    }

    public class NonDCPerson
    {
        public string Name = "jeff";
        public int Age = 20;

        public NonDCPerson() { }

        public NonDCPerson(PersonSurrogated nonDCPerson)
        {
            this.Name = nonDCPerson.Name;
            this.Age = nonDCPerson.Age;
        }
    }

    [DataContract(IsReference = true)]
    public class PersonSurrogated
    {
        [DataMember]
        public string Name = "Jeffery";

        [DataMember]
        public int Age = 30;

        public PersonSurrogated() { }

        public PersonSurrogated(NonDCPerson nonDCPerson)
        {
            this.Name = nonDCPerson.Name;
            this.Age = nonDCPerson.Age;
        }
    }

    public class DCSurrogate
    {
        public object GetCustomDataToExport(Type clrType, Type dataContractType)
        {
            throw new NotImplementedException();
        }

        public object GetCustomDataToExport(System.Reflection.MemberInfo memberInfo, Type dataContractType)
        {
            throw new NotImplementedException();
        }

        public Type GetDataContractType(Type type)
        {
            if (typeof(NonDCPerson).IsAssignableFrom(type))
            {
                return typeof(PersonSurrogated);
            }
            return type;
        }

        public object GetDeserializedObject(object obj, Type memberType)
        {
            if (obj is PersonSurrogated)
            {
                return new NonDCPerson((PersonSurrogated)obj);
            }
            return obj;
        }

        public void GetKnownCustomDataTypes(Collection<Type> customDataTypes)
        {
        }

        public object GetObjectToSerialize(object obj, Type membertype)
        {
            if (obj is NonDCPerson)
            {
                return new PersonSurrogated((NonDCPerson)obj);
            }
            return obj;
        }

        public Type GetReferencedTypeOnImport(string typeName, string typeNamespace, object customData)
        {
            throw new NotImplementedException();
        }

        public System.CodeDom.CodeTypeDeclaration ProcessImportedType(System.CodeDom.CodeTypeDeclaration typeDeclaration, System.CodeDom.CodeCompileUnit compileUnit)
        {
            throw new NotImplementedException();
        }
    }

    public class SerSurrogate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            if (obj is NonDCPerson)
            {
                info.AddValue("data", new PersonSurrogated(obj as NonDCPerson));
            }
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            if (obj is PersonSurrogated)
            {
                return new NonDCPerson(obj as PersonSurrogated);
            }
            else if (obj is NonDCPerson)
            {
                return new NonDCPerson(info.GetValue("data", typeof(PersonSurrogated)) as PersonSurrogated);
            }
            else
            {
                return obj;
            }
        }
    }

    public class DCSurrogateExplicit
    {
        private object GetCustomDataToExport(Type clrType, Type dataContractType)
        {
            throw new NotImplementedException();
        }

        private object GetCustomDataToExport(System.Reflection.MemberInfo memberInfo, Type dataContractType)
        {
            throw new NotImplementedException();
        }

        private Type GetDataContractType(Type type)
        {
            if (typeof(NonDCPerson).IsAssignableFrom(type))
            {
                return typeof(PersonSurrogated);
            }
            return type;
        }

        private object GetDeserializedObject(object obj, Type memberType)
        {
            if (obj is PersonSurrogated)
            {
                return new NonDCPerson((PersonSurrogated)obj);
            }
            return obj;
        }

        private void GetKnownCustomDataTypes(Collection<Type> customDataTypes)
        {
        }

        private object GetObjectToSerialize(object obj, Type membertype)
        {
            if (obj is NonDCPerson)
            {
                return new PersonSurrogated((NonDCPerson)obj);
            }
            return obj;
        }

        private Type GetReferencedTypeOnImport(string typeName, string typeNamespace, object customData)
        {
            throw new NotImplementedException();
        }

        private System.CodeDom.CodeTypeDeclaration ProcessImportedType(System.CodeDom.CodeTypeDeclaration typeDeclaration, System.CodeDom.CodeCompileUnit compileUnit)
        {
            throw new NotImplementedException();
        }
    }

    public class SerSurrogateExplicit : ISerializationSurrogate
    {
        void ISerializationSurrogate.GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            if (obj is NonDCPerson)
            {
                info.AddValue("data", new PersonSurrogated(obj as NonDCPerson));
            }
        }

        object ISerializationSurrogate.SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            if (obj is PersonSurrogated)
            {
                return new NonDCPerson(obj as PersonSurrogated);
            }
            else if (obj is NonDCPerson)
            {
                return new NonDCPerson(info.GetValue("data", typeof(PersonSurrogated)) as PersonSurrogated);
            }
            else
            {
                return obj;
            }
        }
    }

    public class DCSurrogateReturnPrivate
    {
        public object GetCustomDataToExport(Type clrType, Type dataContractType)
        {
            throw new NotImplementedException();
        }

        public object GetCustomDataToExport(System.Reflection.MemberInfo memberInfo, Type dataContractType)
        {
            throw new NotImplementedException();
        }

        public Type GetDataContractType(Type type)
        {
            if (typeof(NonDCPerson).IsAssignableFrom(type))
            {
                return typeof(PrivateDC);
            }
            return type;
        }

        public object GetDeserializedObject(object obj, Type memberType)
        {
            if (obj is PrivateDC)
            {
                return new NonDCPerson();
            }
            return obj;
        }

        public void GetKnownCustomDataTypes(Collection<Type> customDataTypes)
        {
        }

        public object GetObjectToSerialize(object obj, Type membertype)
        {
            if (obj is NonDCPerson)
            {
                return new PrivateDC();
            }
            return obj;
        }

        public Type GetReferencedTypeOnImport(string typeName, string typeNamespace, object customData)
        {
            throw new NotImplementedException();
        }

        public System.CodeDom.CodeTypeDeclaration ProcessImportedType(System.CodeDom.CodeTypeDeclaration typeDeclaration, System.CodeDom.CodeCompileUnit compileUnit)
        {
            throw new NotImplementedException();
        }
    }

    public class SerSurrogateReturnPrivate : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            if (obj is NonDCPerson)
            {
                info.AddValue("data", new PrivateDC());
            }
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            if (obj is PrivateDC)
            {
                return new NonDCPerson();
            }
            else
            {
                return obj;
            }
        }
    }

    [DataContract(IsReference = true)]
    public class NullableContainerContainsValue
    {
        [DataMember]
        public Nullable<PublicDCStruct> Data = new PublicDCStruct(true);

        public NullableContainerContainsValue()
        {
        }
    }

    [DataContract(IsReference = true)]
    public class NullableContainerContainsNull
    {
        [DataMember]
        public Nullable<PublicDCStruct> Data = null;

        public NullableContainerContainsNull()
        {
        }
    }

    [DataContract]
    public struct PublicDCStruct
    {
        [DataMember]
        public string Data;
        public PublicDCStruct(bool init)
        {
            Data = "Data";
        }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(Nullable<PrivateDCStruct>))]
    public class NullablePrivateContainerContainsValue
    {
        [DataMember]
        public object Data = new Nullable<PrivateDCStruct>(new PrivateDCStruct(true));

        public NullablePrivateContainerContainsValue()
        {
        }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(Nullable<PrivateDCStruct>))]
    public class NullablePrivateContainerContainsNull
    {
        [DataMember]
        public object Data = new Nullable<PrivateDCStruct>(new PrivateDCStruct(true));

        public NullablePrivateContainerContainsNull()
        {
        }
    }

    [DataContract]
    internal struct PrivateDCStruct
    {
        [DataMember]
        public int Data;

        public PrivateDCStruct(bool init)
        {
            Data = int.MaxValue;
        }
    }

    [DataContract(IsReference = true)]
    public class NullablePrivateDataInDMContainerContainsValue
    {
        [DataMember]
        public Nullable<PublicDCStructContainsPrivateDataInDM> Data = new PublicDCStructContainsPrivateDataInDM(true);

        public NullablePrivateDataInDMContainerContainsValue()
        {
        }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(Nullable<PublicDCStructContainsPrivateDataInDM>))]
    public class NullablePrivateDataInDMContainerContainsNull
    {
        [DataMember]
        public object Data = new Nullable<PublicDCStructContainsPrivateDataInDM>(new PublicDCStructContainsPrivateDataInDM(true));

        public NullablePrivateDataInDMContainerContainsNull()
        {
        }
    }

    [DataContract]
    public struct PublicDCStructContainsPrivateDataInDM
    {
        [DataMember]
        public PublicDCClassPrivateDM Data;

        public PublicDCStructContainsPrivateDataInDM(bool init)
        {
            Data = new PublicDCClassPrivateDM(true);
        }
    }

    [DataContract(IsReference = true)]
    public class DCPublicDatasetPublic
    {
        [DataMember]
        public DataSet dataSet;

        [DataMember]
        public DataSet dataSet2;

        [DataMember]
        public DataTable dataTable;

        [DataMember]
        public DataTable dataTable2;

        public DCPublicDatasetPublic()
        {
        }
        public DCPublicDatasetPublic(bool init)
        {
            dataSet = new DataSet("MyData");
            dataTable = new DataTable("MyTable");
            DataColumn dc1 = new DataColumn("Data", typeof(string));
            dataTable.Columns.Add(dc1);
            DataRow row1 = dataTable.NewRow();
            row1[dc1] = "10";
            dataTable.Rows.Add(row1);
            dataSet.Tables.Add(dataTable);
            dataSet2 = dataSet;
            dataTable2 = dataTable;
        }
    }

    [DataContract(IsReference = true)]
    public class DCPublicDatasetPrivate
    {
        [DataMember]
        private DataSet _dataSet;

        [DataMember]
        public DataTable dataTable;

        public DCPublicDatasetPrivate()
        {
        }

        public DCPublicDatasetPrivate(bool init)
        {
            _dataSet = new DataSet("MyData");
            dataTable = new DataTable("MyTable");
            DataColumn dc1 = new DataColumn("Data", typeof(string));
            dataTable.Columns.Add(dc1);
            DataRow row1 = dataTable.NewRow();
            row1[dc1] = "20";
            dataTable.Rows.Add(row1);
            _dataSet.Tables.Add(dataTable);
        }
    }

    [Serializable]
    public class SerPublicDatasetPublic
    {
        public DataSet dataSet;

        public DataTable dataTable;

        public SerPublicDatasetPublic()
        {
        }

        public SerPublicDatasetPublic(bool init)
        {
            dataSet = new DataSet("MyData");
            DataTable dt = new DataTable("MyTable");
            DataColumn dc1 = new DataColumn("Data", typeof(string));
            dt.Columns.Add(dc1);
            DataRow row1 = dt.NewRow();
            row1[dc1] = "Testing";
            dt.Rows.Add(row1);
            dataSet.Tables.Add(dt);
            dataTable = new DataTable("MyTable");
            DataColumn dc2 = new DataColumn("Data", typeof(string));
            dataTable.Columns.Add(dc2);
            DataRow row2 = dataTable.NewRow();
            row2[dc2] = "Testing";
            dataTable.Rows.Add(row2);
        }
    }

    [Serializable]
    public class SerPublicDatasetPrivate
    {
        private DataSet _dataSet;

        [DataMember]
        public DataTable dataTable;

        public SerPublicDatasetPrivate()
        {
        }

        public SerPublicDatasetPrivate(bool init)
        {
            _dataSet = new DataSet("MyData");
            DataTable dt = new DataTable("MyTable");
            DataColumn dc1 = new DataColumn("Data", typeof(string));
            dt.Columns.Add(dc1);
            DataRow row1 = dt.NewRow();
            row1[dc1] = "Testing";
            dt.Rows.Add(row1);
            _dataSet.Tables.Add(dt);
            dataTable = new DataTable("MyTable");
            DataColumn dc2 = new DataColumn("Data", typeof(string));
            dataTable.Columns.Add(dc2);
            DataRow row2 = dataTable.NewRow();
            row2[dc2] = "Testing";
            dataTable.Rows.Add(row2);
        }
    }

    [System.Runtime.Serialization.DataContract(IsReference = true)]
    public class CustomGeneric2<T> where T : new()
    {
        [System.Runtime.Serialization.DataMember]
        public string Data = "data";

        public T t = new T();
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(DateTimeOffset))]
    [KnownType(typeof(MyEnum1))]
    public class DTOContainer
    {
        [DataMember]
        [IgnoreMember]
        public Enum enumBase1 = MyEnum1.red;

        [DataMember]
        public Array array1 = new object[] { new object(), DateTimeOffset.MinValue, new object() };

        [DataMember]
        public ValueType valType = DateTimeOffset.MinValue;

        [DataMember]
        public Nullable<DateTimeOffset> nDTO = DateTimeOffset.MaxValue;

        [DataMember]
        public List<DateTimeOffset> lDTO = new List<DateTimeOffset>();

        [DataMember]
        public Dictionary<DateTimeOffset, DateTimeOffset> dictDTO = new Dictionary<DateTimeOffset, DateTimeOffset>();

        [DataMember]
        public DateTimeOffset[] arrayDTO;

        public DTOContainer() { }
        public DTOContainer(bool init)
        {
            lDTO.Add(DateTimeOffset.MaxValue);
            lDTO.Add(DateTimeOffset.MinValue);
            dictDTO.Add(DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
            dictDTO.Add(DateTimeOffset.MaxValue, DateTimeOffset.MinValue);

            arrayDTO = new DateTimeOffset[] { DateTimeOffset.MinValue, DateTimeOffset.MaxValue };
        }
    }
}
