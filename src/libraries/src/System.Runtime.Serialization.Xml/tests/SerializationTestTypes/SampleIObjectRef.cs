// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace SerializationTestTypes
{
    [DataContract(IsReference = false)]
    [KnownType(typeof(PublicDC))]
    public class DCExplicitInterfaceIObjRef : IObjectReference
    {
        [DataMember]
        public SelfRef1 data;

        [NonSerialized]
        static public SelfRef1 containedData = new SelfRef1();

        public DCExplicitInterfaceIObjRef() { }
        public DCExplicitInterfaceIObjRef(bool init)
        {
            data = new SelfRef1(true);
        }

        object IObjectReference.GetRealObject(StreamingContext context)
        {
            return containedData;
        }

        public override bool Equals(object obj)
        {
            return DCRUtils.CompareIObjectRefTypes(containedData, obj);
        }

        public override int GetHashCode()
        {
            return containedData.GetHashCode();
        }
    }

    [DataContract(IsReference = false)]
    [KnownType(typeof(PublicDC))]
    public class DCIObjRef : IObjectReference
    {
        [DataMember]
        public SimpleDCWithRef data;

        [NonSerialized]
        private static SimpleDCWithRef s_containedData = new SimpleDCWithRef(true);

        public DCIObjRef() { }
        public DCIObjRef(bool init) { }

        public object GetRealObject(StreamingContext context)
        {
            return s_containedData;
        }

        public override bool Equals(object obj)
        {
            return DCRUtils.CompareIObjectRefTypes(s_containedData, obj);
        }

        public override int GetHashCode()
        {
            return s_containedData.GetHashCode();
        }
    }

    [Serializable]
    [KnownType(typeof(PrivateDC))]
    public class SerExplicitInterfaceIObjRefReturnsPrivate : IObjectReference
    {
        [NonSerialized]
        private static PrivateDC s_containedData = new PrivateDC();

        object IObjectReference.GetRealObject(StreamingContext context)
        {
            return s_containedData;
        }

        public override bool Equals(object obj)
        {
            return DCRUtils.CompareIObjectRefTypes(s_containedData, obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    [Serializable]
    [KnownType(typeof(PrivateDC))]
    public class SerIObjRefReturnsPrivate : IObjectReference
    {
        [NonSerialized]
        private static PrivateDC s_containedData = new PrivateDC();

        object IObjectReference.GetRealObject(StreamingContext context)
        {
            return s_containedData;
        }

        public override bool Equals(object obj)
        {
            return DCRUtils.CompareIObjectRefTypes(s_containedData, obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    [DataContract(IsReference = false)]
    [KnownType(typeof(PrivateDC))]
    public class DCExplicitInterfaceIObjRefReturnsPrivate : IObjectReference
    {
        [DataMember]
        private PrivateDC _data = new PrivateDC();

        [NonSerialized]
        private static PrivateDC s_containedData = new PrivateDC();

        object IObjectReference.GetRealObject(StreamingContext context)
        {
            return s_containedData;
        }

        public override bool Equals(object obj)
        {
            return DCRUtils.CompareIObjectRefTypes(s_containedData, obj);
        }

        public override int GetHashCode()
        {
            return s_containedData.GetHashCode();
        }
    }

    [DataContract(IsReference = false)]
    [KnownType(typeof(PrivateDC))]
    public class DCIObjRefReturnsPrivate : IObjectReference
    {
        [DataMember]
        private PrivateDC _data = new PrivateDC();

        [NonSerialized]
        private static PrivateDC s_containedData = new PrivateDC();

        public object GetRealObject(StreamingContext context)
        {
            return s_containedData;
        }

        public override bool Equals(object obj)
        {
            return DCRUtils.CompareIObjectRefTypes(s_containedData, obj);
        }

        public override int GetHashCode()
        {
            return s_containedData.GetHashCode();
        }
    }
}
