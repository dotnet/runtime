// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace SerializationTestTypes
{
    [DataContract(IsReference = true)]
    public class SelfRef1
    {
        [DataMember]
        public SelfRef1 Data;

        public SelfRef1() { }
        public SelfRef1(bool init)
        {
            Data = this;
        }
    }

    [DataContract(IsReference = true)]
    public class SelfRef1DoubleDM
    {
        [DataMember]
        public SelfRef1DoubleDM Data;

        [DataMember]
        public SelfRef1DoubleDM Data2;

        public SelfRef1DoubleDM() { }
        public SelfRef1DoubleDM(bool init)
        {
            Data = this;
            Data2 = Data;
        }
    }

    [DataContract(IsReference = true)]
    public class SelfRef2
    {
        [DataMember]
        public SelfRef1 Data;

        [DataMember]
        public SelfRef1 RefData;

        public SelfRef2() { }
        public SelfRef2(bool init)
        {
            Data = new SelfRef1(true);
            RefData = Data;
        }
    }

    [DataContract(IsReference = true)]
    public class SelfRef3
    {
        [DataMember]
        public SelfRef2 Data;

        [DataMember]
        public SelfRef2 RefData;

        public SelfRef3() { }
        public SelfRef3(bool init)
        {
            Data = new SelfRef2(true);
            RefData = Data;
        }
    }

    [DataContract(IsReference = true)]
    public class Cyclic1
    {
        [DataMember]
        public Cyclic2 Data;
        public Cyclic1() { }
        public Cyclic1(bool init)
        {
            Data = new Cyclic2(true);
        }
    }

    [DataContract(IsReference = true)]
    public class Cyclic2
    {
        [DataMember]
        public Cyclic1 Data;
        public Cyclic2() { }
        public Cyclic2(bool init)
        {
            Data = new Cyclic1();
            Data.Data = this;
        }
    }

    [DataContract(IsReference = true)]
    public class CyclicA
    {
        [DataMember]
        public CyclicB Data;
        public CyclicA() { }
        public CyclicA(bool init)
        {
            Data = new CyclicB(true);
            Data.Data.Data.Data = this;
        }
    }

    [DataContract(IsReference = true)]
    public class CyclicB
    {
        [DataMember]
        public CyclicC Data;
        public CyclicB() { }
        public CyclicB(bool init)
        {
            Data = new CyclicC(true);
            Data.Data.Data.Data = this;
        }
    }

    [DataContract(IsReference = true)]
    public class CyclicC
    {
        [DataMember]
        public CyclicD Data;
        public CyclicC() { }
        public CyclicC(bool init)
        {
            Data = new CyclicD(true);
            Data.Data.Data.Data = this;
        }
    }

    [DataContract(IsReference = true)]
    public class CyclicD
    {
        [DataMember]
        public CyclicA Data;
        public CyclicD() { }
        public CyclicD(bool init)
        {
            Data = new CyclicA();
            Data.Data = new CyclicB();
            Data.Data.Data = new CyclicC();
            Data.Data.Data.Data = this;
        }
    }

    [DataContract(IsReference = true)]
    public class CyclicABCD1
    {
        [DataMember]
        public CyclicA Data;
        public CyclicABCD1() { }
        public CyclicABCD1(bool init)
        {
            Data = new CyclicA(true);
            Data.Data.Data.Data.Data = Data;
        }
    }

    [DataContract(IsReference = true)]
    public class CyclicABCD2
    {
        [DataMember]
        public CyclicA Data;
        public CyclicABCD2() { }
        public CyclicABCD2(bool init)
        {
            Data = new CyclicA(true);
            Data.Data.Data.Data.Data = null;
        }
    }

    [DataContract(IsReference = true)]
    public class CyclicABCD3
    {
        [DataMember]
        public CyclicA Data;
        public CyclicABCD3() { }
        public CyclicABCD3(bool init)
        {
            Data = new CyclicA(true);
            Data.Data.Data = new CyclicC(true);
            Data.Data.Data.Data.Data = new CyclicA(true);
        }
    }

    [DataContract(IsReference = true)]
    public class CyclicABCD4
    {
        [DataMember]
        public CyclicA Data;
        public CyclicABCD4() { }
        public CyclicABCD4(bool init)
        {
            Data = new CyclicA(true);
            Data.Data.Data = new CyclicC(true);
            Data.Data.Data.Data.Data = new CyclicA(true);
            Data.Data.Data.Data.Data.Data.Data.Data.Data.Data = new CyclicB(true);
        }
    }

    [DataContract(IsReference = true)]
    public class CyclicABCD5
    {
        [DataMember]
        public CyclicA Data;

        [DataMember]
        public CyclicA Data2;

        public CyclicABCD5() { }
        public CyclicABCD5(bool init)
        {
            Data = new CyclicA(true);
            Data2 = new CyclicA(true);
            Data.Data.Data.Data.Data = Data2;
            Data2.Data.Data.Data.Data = Data;
        }
    }

    [DataContract(IsReference = true)]
    public class CyclicABCD6
    {
        [DataMember]
        public CyclicA Data;

        [DataMember]
        public CyclicA Data2;

        [DataMember]
        public CyclicB Data3;

        public CyclicABCD6() { }
        public CyclicABCD6(bool init)
        {
            Data = new CyclicA(true);
            Data2 = new CyclicA(true);
            Data.Data.Data.Data.Data = Data2;
            Data2.Data.Data.Data.Data = Data;
            Data3 = Data.Data;
        }
    }

    [DataContract(IsReference = true)]
    public class CyclicABCD7
    {
        [DataMember]
        public CyclicA Data;

        [DataMember]
        public CyclicA Data2;

        [DataMember]
        public CyclicB Data3;

        [DataMember]
        public CyclicC Data4;

        public CyclicABCD7() { }
        public CyclicABCD7(bool init)
        {
            Data = new CyclicA(true);
            Data2 = new CyclicA(true);
            Data4 = new CyclicC(true);
            Data4.Data.Data = Data2;
            Data2.Data.Data.Data.Data = Data;
            Data3 = Data.Data;
        }
    }

    [DataContract(IsReference = true)]
    public class CyclicABCD8
    {
        [DataMember]
        public CyclicA Data;

        [DataMember]
        public CyclicA Data2;

        [DataMember]
        public CyclicB Data3;

        [DataMember]
        public CyclicC Data4;

        [DataMember]
        public CyclicD Data5;

        public CyclicABCD8() { }
        public CyclicABCD8(bool init)
        {
            Data = new CyclicA(true);
            Data2 = new CyclicA(true);
            Data3 = new CyclicB(true);
            Data4 = new CyclicC(true);
            Data5 = new CyclicD(true);
            Data2.Data = Data3;
            Data3.Data.Data.Data = Data2;
            Data4.Data = Data5;
            Data5.Data = Data;
        }
    }

    [DataContract(IsReference = true)]
    public class CyclicABCDNoCycles
    {
        [DataMember]
        public CyclicA Data;
        public CyclicABCDNoCycles() { }
        public CyclicABCDNoCycles(bool init)
        {
            Data = new CyclicA(true);
            Data.Data = new CyclicB(true);
            Data.Data.Data = new CyclicC(true);
            Data.Data.Data.Data = new CyclicD(true);
            Data.Data.Data.Data.Data = new CyclicA(true);
        }
    }

    [DataContract(IsReference = true)]
    public class A1
    {
        [DataMember]
        public B1 Data;
        public A1() { }
        public A1(bool init)
        {
            Data = new B1(true);
        }
    }

    [DataContract(IsReference = true)]
    public class B1
    {
        [DataMember]
        public BB1 Data;

        [DataMember]
        public C1 Data2;

        public B1() { }
        public B1(bool init)
        {
            Data = new BB1(true);
            Data2 = new C1(true);
        }
    }

    [DataContract(IsReference = true)]
    public class C1
    {
        [DataMember]
        public B1 Data;
        public C1() { }
        public C1(bool init)
        {
            Data = new B1();
            Data.Data2 = new C1();
            Data.Data = new BB1(true);
        }
    }

    [DataContract(IsReference = true)]
    public class BB1
    {
        [DataMember]
        public BBB1 Data;
        public BB1() { }
        public BB1(bool init)
        {
            Data = new BBB1(true);
        }
    }

    [DataContract(IsReference = true)]
    public class BBB1
    {
        [DataMember]
        public A1 Data;
        public BBB1() { }
        public BBB1(bool init)
        {
            Data = new A1();
            Data.Data = new B1();
            Data.Data.Data = new BB1();
            Data.Data.Data2 = new C1();
            Data.Data.Data2.Data = Data.Data;
            Data.Data.Data.Data = this;
        }
    }
}
