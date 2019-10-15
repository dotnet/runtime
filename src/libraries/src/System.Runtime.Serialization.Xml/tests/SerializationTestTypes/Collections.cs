// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SerializationTestTypes
{
    public class ContainsLinkedList
    {
        public LinkedList<SimpleDCWithRef> Data;

        public ContainsLinkedList() { }
        public ContainsLinkedList(bool init)
        {
            this.Data = new LinkedList<SimpleDCWithRef>();
            SimpleDCWithRef d1 = new SimpleDCWithRef(true);
            SimpleDCWithRef d2 = new SimpleDCWithRef(true);
            d2.Data.Data = d1.RefData.Data;
            Data.AddLast(d1);
            Data.AddLast(d2);
            Data.AddLast(d2);
            Data.AddLast(d1);
            SimpleDCWithRef d3 = new SimpleDCWithRef(true);
            SimpleDCWithRef d4 = new SimpleDCWithRef(true);
            d4.Data = d3.RefData;
            Data.AddLast(d4);
            Data.AddLast(d3);
            SimpleDCWithRef d5 = new SimpleDCWithRef(true);
            SimpleDCWithRef d6 = new SimpleDCWithRef(true);
            SimpleDCWithRef d7 = new SimpleDCWithRef(true);
            d6.Data = d5.Data;
            d7.Data = d5.RefData;
            d7.RefData = d6.RefData;
            Data.AddLast(d7);
        }
    }

    #region Simple CDC

    [CollectionDataContract(Name = "SimpleCDC", ItemName = "Item", IsReference = true)]
    public class SimpleCDC : ICollection<string>
    {
        private List<string> _data = new List<string>();
        public SimpleCDC() { }
        public SimpleCDC(bool init)
        {
            _data.Add("One");
            _data.Add("Two");
            _data.Add("two");
        }

        #region ICollection<string> Members

        public void Add(string item)
        {
            _data.Add(item);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public bool Contains(string item)
        {
            return _data.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            _data.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _data.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(string item)
        {
            return _data.Remove(item);
        }

        #endregion

        #region IEnumerable<string> Members

        public IEnumerator<string> GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        #endregion
    }

    [CollectionDataContract(Name = "SimpleCDC2", ItemName = "Item", IsReference = true)]
    public class SimpleCDC2 : ICollection<string>
    {
        private List<string> _data = new List<string>();
        public SimpleCDC2() { }
        public SimpleCDC2(bool init)
        {
            _data.Add("One");
            _data.Add("Two");
            _data.Add("two");
        }

        #region ICollection<string> Members

        public void Add(string item)
        {
            _data.Add(item);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public bool Contains(string item)
        {
            return _data.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            _data.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _data.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(string item)
        {
            return _data.Remove(item);
        }

        #endregion

        #region IEnumerable<string> Members

        public IEnumerator<string> GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        #endregion
    }

    [DataContract(IsReference = true)]
    public class ContainsSimpleCDC
    {
        [DataMember]
        public SimpleCDC data1;

        [DataMember]
        public SimpleCDC2 data2;

        public ContainsSimpleCDC() { }
        public ContainsSimpleCDC(bool init)
        {
            data1 = new SimpleCDC(true);
            data2 = new SimpleCDC2(true);
        }
    }
    #endregion

    #region DMs in Collections

    [DataContract(IsReference = true)]
    public class DMInCollection1
    {
        [DataMember]
        public SimpleDC Data1;

        [DataMember]
        public List<SimpleDC> List1;

        public DMInCollection1() { }
        public DMInCollection1(bool init)
        {
            Data1 = new SimpleDC(true);
            List1 = new List<SimpleDC>();
            List1.Add(Data1);
            List1.Add(new SimpleDC(true));
            List1.Add(List1[1]);
        }
    }

    [DataContract(IsReference = true)]
    public class DMInCollection2
    {
        [DataMember]
        public IEnumerable<SimpleDC> List4;

        [DataMember]
        public ICollection<SimpleDC> List3;

        [DataMember]
        public IList<SimpleDC> List2;

        [DataMember]
        public List<SimpleDC> List1;

        [DataMember]
        public SimpleDC Data;

        [DataMember]
        public string InnerContent;

        [DataMember]
        public string InnerInnerContent;

        public DMInCollection2() { }
        public DMInCollection2(bool init)
        {
            List1 = new List<SimpleDC>();
            List2 = new List<SimpleDC>();
            List1.Add(new SimpleDC(true));
            List1.Add(new SimpleDC(true));
            List1.Add(new SimpleDC(true));
            List1.Add(new SimpleDC(true));
            List2.Add(List1[0]);
            List2.Add(new SimpleDC(true));
            List1.Add(List2[1]);
            List3 = List1;
            List4 = List2;
            Data = List2[0];
            InnerContent = Data.Data;
            InnerInnerContent = List1[3].Data;
        }
    }

    [DataContract(IsReference = true)]
    public class DMInDict1
    {
        [DataMember]
        public string InnerInnerData1;

        [DataMember]
        public Dictionary<SimpleDC, SimpleDC> Dict1;

        [DataMember]
        public IDictionary<SimpleDC, SimpleDC> Dict2;

        [DataMember]
        public KeyValuePair<SimpleDC, SimpleDC> Kvp1;

        [DataMember]
        public SimpleDC Data1;

        [DataMember]
        public SimpleDC Data2;

        [DataMember]
        public string InnerData1;

        public DMInDict1() { }
        public DMInDict1(bool init)
        {
            Dict1 = new Dictionary<SimpleDC, SimpleDC>();
            Data1 = new SimpleDC(true);
            Data2 = new SimpleDC(true);
            InnerInnerData1 = new Guid("cd4f6d1f-db5e-49c9-bb43-13e73508a549").ToString();
            InnerData1 = Data1.Data;

            SimpleDC t1 = new SimpleDC(true);
            SimpleDC t2 = new SimpleDC(true);
            t2.Data = InnerInnerData1;
            Dict1.Add(t1, t2);
            Dict1.Add(Data1, Data2);
            Dict1.Add(new SimpleDC(true), t2);
            Dict1.Add(t2, new SimpleDC(true));
            Dict2 = Dict1;
            foreach (KeyValuePair<SimpleDC, SimpleDC> kvp in Dict1)
            {
                Kvp1 = kvp;
            }
        }
    }

    #endregion

    #region DMs in Collection where DMs are of DCs with Ref

    [DataContract(IsReference = true)]
    public class DMWithRefInCollection1
    {
        [DataMember]
        public SimpleDCWithSimpleDMRef Data1;

        [DataMember]
        public List<SimpleDCWithSimpleDMRef> List1;

        [DataMember]
        public string InnerData1;

        public DMWithRefInCollection1() { }
        public DMWithRefInCollection1(bool init)
        {
            InnerData1 = new Guid("a6d053ed-f7d4-42fb-8e56-e4b425f26fa9").ToString();
            Data1 = new SimpleDCWithSimpleDMRef(true);
            List1 = new List<SimpleDCWithSimpleDMRef>();
            List1.Add(Data1);
            List1.Add(new SimpleDCWithSimpleDMRef(true));
            List1.Add(List1[1]);
            List1[1].Data = InnerData1;
        }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(SimpleDC))]
    [KnownType(typeof(SimpleDCWithRef))]
    [KnownType(typeof(SimpleDCWithSimpleDMRef))]
    [KnownType(typeof(List<SimpleDC>))]
    [KnownType(typeof(List<SimpleDCWithRef>))]
    [KnownType(typeof(List<SimpleDCWithSimpleDMRef>))]
    public class DMWithRefInCollection2
    {
        [DataMember]
        public IEnumerable<SimpleDCWithRef> List4;

        [DataMember]
        public ICollection<SimpleDCWithRef> List3;

        [DataMember]
        public IList<SimpleDCWithRef> List2;

        [DataMember]
        public List<SimpleDCWithRef> List1;

        [DataMember]
        public List<SimpleDC> List5;

        [DataMember]
        public List<object> List6;

        [DataMember]
        public SimpleDC Data;

        [DataMember]
        public string InnerContent;

        [DataMember]
        public string InnerInnerContent;

        public DMWithRefInCollection2() { }
        public DMWithRefInCollection2(bool init)
        {
            List1 = new List<SimpleDCWithRef>();
            List2 = new List<SimpleDCWithRef>();
            List1.Add(new SimpleDCWithRef(true));
            List1.Add(new SimpleDCWithRef(true));
            List1.Add(new SimpleDCWithRef(true));
            List1.Add(new SimpleDCWithRef(true));
            List2.Add(List1[0]);
            List2.Add(new SimpleDCWithRef(true));
            List1.Add(List2[1]);
            List3 = List1;
            List4 = List2;
            Data = List2[0].Data;
            InnerContent = Data.Data;
            InnerInnerContent = List1[3].Data.Data;
            List5 = new List<SimpleDC>();
            List5.Add(Data);
            List5.Add(new SimpleDC(true));
            List5.Add(Data);
            List6 = new List<object>();
            List6.Add(new SimpleDC(true));
            List6.Add(new SimpleDCWithRef(true));
            List6.Add(new SimpleDCWithSimpleDMRef(true));
            List6.Add(new List<SimpleDC>());
            List6.Add(new List<SimpleDCWithRef>());
            List6.Add(new List<SimpleDCWithSimpleDMRef>());
            List6.Add(List1);
            List6.Add(List2);
            List6.Add(List3);
            List6.Add(List4);
            List6.Add(List5);
            List6.Add(Data);
            List6.Add(InnerContent);
            List6.Add(InnerInnerContent);
        }
    }

    [DataContract(IsReference = true)]
    public class DMWithRefInDict1
    {
        [DataMember]
        public string InnerInnerData1;

        [DataMember]
        public Dictionary<SimpleDCWithRef, SimpleDCWithRef> Dict1;

        [DataMember]
        public IDictionary<SimpleDCWithRef, SimpleDCWithRef> Dict2;

        [DataMember]
        public KeyValuePair<SimpleDCWithRef, SimpleDCWithRef> Kvp1;

        [DataMember]
        public SimpleDCWithRef Data1;

        [DataMember]
        public SimpleDCWithRef Data2;

        [DataMember]
        public SimpleDC InnerData1;

        public DMWithRefInDict1() { }
        public DMWithRefInDict1(bool init)
        {
            Dict1 = new Dictionary<SimpleDCWithRef, SimpleDCWithRef>();
            Data1 = new SimpleDCWithRef(true);
            Data2 = new SimpleDCWithRef(true);
            InnerInnerData1 = new Guid("6d807157-536f-4794-a157-e463a11029aa").ToString();
            InnerData1 = Data1.Data;

            SimpleDCWithRef t1 = new SimpleDCWithRef(true);
            SimpleDCWithRef t2 = new SimpleDCWithRef(true);
            t2.Data.Data = InnerInnerData1;
            Dict1.Add(t1, t2);
            Dict1.Add(Data1, Data2);
            Dict1.Add(new SimpleDCWithRef(true), t2);
            Dict1.Add(t2, new SimpleDCWithRef(true));
            Dict2 = Dict1;
            foreach (KeyValuePair<SimpleDCWithRef, SimpleDCWithRef> kvp in Dict1)
            {
                Kvp1 = kvp;
            }
        }
    }

    #endregion
}
