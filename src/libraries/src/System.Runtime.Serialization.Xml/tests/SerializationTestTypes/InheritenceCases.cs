// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace SerializationTestTypes
{
    [DataContract(IsReference = true)]
    public class BaseWithIsRefTrue
    {
        [DataMember]
        public SimpleDC Data;
        public BaseWithIsRefTrue() { }
        public BaseWithIsRefTrue(bool init)
        {
            Data = new SimpleDC(true);
        }
    }

    [DataContract]
    public class DerivedNoIsRef : BaseWithIsRefTrue
    {
        [DataMember]
        public SimpleDC RefData;
        public DerivedNoIsRef() { }
        public DerivedNoIsRef(bool init)
            : base(init)
        {
            RefData = Data;
        }
    }

    [DataContract]
    public class DerivedNoIsRef2 : DerivedNoIsRef
    {
        [DataMember]
        public SimpleDC RefData2;
        public DerivedNoIsRef2() { }
        public DerivedNoIsRef2(bool init)
            : base(init)
        {
            RefData2 = RefData;
        }
    }

    [DataContract]
    public class DerivedNoIsRef3 : DerivedNoIsRef2
    {
        [DataMember]
        public SimpleDC RefData3;
        public DerivedNoIsRef3() { }
        public DerivedNoIsRef3(bool init)
            : base(init)
        {
            RefData3 = RefData2;
        }
    }

    [DataContract]
    public class DerivedNoIsRef4 : DerivedNoIsRef3
    {
        [DataMember]
        public SimpleDC RefData4;
        public DerivedNoIsRef4() { }
        public DerivedNoIsRef4(bool init)
            : base(init)
        {
            RefData4 = RefData3;
        }
    }

    [DataContract]
    public class DerivedNoIsRef5 : DerivedNoIsRef4
    {
        [DataMember]
        public SimpleDC RefData5;
        public DerivedNoIsRef5() { }
        public DerivedNoIsRef5(bool init)
            : base(init)
        {
            RefData5 = RefData4;
        }
    }

    [DataContract(IsReference = true)]
    public class DerivedNoIsRefWithIsRefTrue6 : DerivedNoIsRef5
    {
        [DataMember]
        public SimpleDC RefData6;
        public DerivedNoIsRefWithIsRefTrue6() { }
        public DerivedNoIsRefWithIsRefTrue6(bool init)
            : base(init)
        {
            RefData6 = RefData5;
        }
    }

    [DataContract]
    public class DerivedWithIsRefFalse : BaseWithIsRefTrue
    {
        [DataMember]
        public SimpleDC RefData;
        public DerivedWithIsRefFalse() { }
        public DerivedWithIsRefFalse(bool init)
            : base(init)
        {
            RefData = Data;
        }
    }

    [DataContract]
    public class DerivedWithIsRefFalse2 : DerivedWithIsRefFalse
    {
        [DataMember]
        public SimpleDC RefData2;
        public DerivedWithIsRefFalse2() { }
        public DerivedWithIsRefFalse2(bool init)
            : base(init)
        {
            RefData2 = RefData;
        }
    }

    [DataContract]
    public class DerivedWithIsRefFalse3 : DerivedWithIsRefFalse2
    {
        [DataMember]
        public SimpleDC RefData3;
        public DerivedWithIsRefFalse3() { }
        public DerivedWithIsRefFalse3(bool init)
            : base(init)
        {
            RefData3 = RefData2;
        }
    }

    [DataContract]
    public class DerivedWithIsRefFalse4 : DerivedWithIsRefFalse3
    {
        [DataMember]
        public SimpleDC RefData4;
        public DerivedWithIsRefFalse4() { }
        public DerivedWithIsRefFalse4(bool init)
            : base(init)
        {
            RefData4 = RefData3;
        }
    }

    [DataContract]
    public class DerivedWithIsRefFalse5 : DerivedWithIsRefFalse4
    {
        [DataMember]
        public SimpleDC RefData5;
        public DerivedWithIsRefFalse5() { }
        public DerivedWithIsRefFalse5(bool init)
            : base(init)
        {
            RefData5 = RefData4;
        }
    }

    [DataContract(IsReference = true)]
    public class DerivedWithIsRefTrue6 : DerivedWithIsRefFalse5
    {
        [DataMember]
        public SimpleDC RefData6;
        public DerivedWithIsRefTrue6() { }
        public DerivedWithIsRefTrue6(bool init)
            : base(init)
        {
            RefData6 = RefData5;
        }
    }

    [DataContract(IsReference = true)]
    public class DerivedWithIsRefTrueExplicit : BaseWithIsRefTrue
    {
        [DataMember]
        public SimpleDC RefData;
        public DerivedWithIsRefTrueExplicit() { }
        public DerivedWithIsRefTrueExplicit(bool init)
            : base(init)
        {
            RefData = Data;
        }
    }

    [DataContract(IsReference = true)]
    public class DerivedWithIsRefTrueExplicit2 : DerivedWithIsRefTrueExplicit
    {
        [DataMember]
        public SimpleDC RefData2;
        public DerivedWithIsRefTrueExplicit2() { }
        public DerivedWithIsRefTrueExplicit2(bool init)
            : base(init)
        {
            RefData2 = Data;
        }
    }

    [DataContract()]
    public class BaseNoIsRef
    {
        [DataMember]
        public SimpleDC Data;
        public BaseNoIsRef() { }
        public BaseNoIsRef(bool init)
        {
            Data = new SimpleDC(true);
        }
    }

    [DataContract(IsReference = true)]
    public class DerivedWithIsRefTrue : BaseNoIsRef
    {
        [DataMember]
        public SimpleDC RefData;
        public DerivedWithIsRefTrue() { }
        public DerivedWithIsRefTrue(bool init)
            : base(true)
        {
            RefData = Data;
        }
    }

    [DataContract]
    public class DerivedWithIsRefFalseExplicit : BaseNoIsRef
    {
        [DataMember]
        public SimpleDC RefData;
        public DerivedWithIsRefFalseExplicit() { }
        public DerivedWithIsRefFalseExplicit(bool init)
            : base(true)
        {
            RefData = Data;
        }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(DerivedDC))]
    public class TestInheritence
    {
        private BaseDC _b;
        private DerivedDC _d;
        [DataMember]
        public BaseDC baseDC
        {
            get
            {
                return _b;
            }
            set
            {
                _b = value;
            }
        }

        [DataMember]
        public DerivedDC derivedDC
        {
            get
            {
                return _d;
            }
            set
            {
                _d = value;
            }
        }

        public TestInheritence()
        {
        }

        public TestInheritence(bool init)
        {
            derivedDC = new DerivedDC(true);
            baseDC = derivedDC;
        }
    }

    [DataContract]
    [KnownType(typeof(DerivedSerializable))]
    [KnownType(typeof(Derived2Serializable))]

    public class TestInheritence9
    {
        [DataMember]
        public BaseSerializable baseDC;
        [DataMember]
        public DerivedSerializable derivedDC;
        [DataMember]
        public BaseDC base1;
        [DataMember]
        public Derived2Serializable derived2;

        public TestInheritence9()
        {
        }

        public TestInheritence9(bool init)
        {
            derivedDC = new DerivedSerializable(true);
            baseDC = derivedDC;
            base1 = new Derived2Serializable(true);
            derived2 = (Derived2Serializable)base1;
        }
    }

    [DataContract]
    [KnownType(typeof(DerivedSerializable))]
    [KnownType(typeof(Derived2Serializable))]
    [KnownType(typeof(Derived3Derived2Serializable))]
    public class TestInheritence91
    {
        [DataMember]
        public BaseSerializable baseDC;
        [DataMember]
        public DerivedSerializable derivedDC;
        [DataMember]
        public BaseDC base1;
        [DataMember]
        public Derived2Serializable derived2;
        [DataMember]
        public Derived3Derived2Serializable derived3;

        public TestInheritence91()
        {
        }

        public TestInheritence91(bool init)
        {
            derivedDC = new DerivedSerializable(true);
            baseDC = derivedDC;
            base1 = new Derived2Serializable(true);
            derived3 = new Derived3Derived2Serializable(true);
            derived2 = derived3;
        }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(DerivedDC))]
    public class TestInheritence5
    {
        [DataMember]
        public BaseDC baseDC = null;

        [DataMember]
        public DerivedDC derivedDC = null;

        public TestInheritence5()
        {
        }

        public TestInheritence5(bool init)
        {
            baseDC = derivedDC;
        }
    }

    [DataContract]
    public class TestInheritence10
    {
        public BaseSerializable baseDC = null;
        public DerivedSerializable derivedDC = null;

        public TestInheritence10()
        {
        }

        public TestInheritence10(bool init)
        {
            baseDC = derivedDC;
        }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(DerivedDC))]
    public class TestInheritence2
    {
        [DataMember]
        public BaseDC baseDC;

        [DataMember]
        public DerivedDC derivedDC;

        public TestInheritence2()
        {
        }

        public TestInheritence2(bool init)
        {
            derivedDC = new DerivedDC(true);
            baseDC = new BaseDC(true);
            baseDC.data = derivedDC.data;
            derivedDC.Data = "String1";
            baseDC.Data = derivedDC.Data;
            baseDC.data = derivedDC.data1;
            derivedDC.Data1 = "String2";
            baseDC.Data = derivedDC.Data1;
            baseDC.data = baseDC.data2;
            baseDC.Data2 = "String3";
            baseDC.Data = baseDC.Data2;
        }
    }

    [DataContract]
    public class TestInheritence11
    {
        [DataMember]
        public BaseSerializable baseDC;
        [DataMember]
        public DerivedSerializable derivedDC;

        public TestInheritence11()
        {
        }

        public TestInheritence11(bool init)
        {
            derivedDC = new DerivedSerializable(true);
            baseDC = new BaseSerializable(true);
            baseDC.data = derivedDC.data;
            derivedDC.Data = "String1";
            baseDC.Data = derivedDC.Data;
            baseDC.data = derivedDC.data1;
            derivedDC.Data1 = "String2";
            baseDC.Data = derivedDC.Data1;
            baseDC.data = baseDC.data2;
            baseDC.Data2 = "String3";
            baseDC.Data = baseDC.Data2;
        }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(DerivedDC))]
    public class TestInheritence3
    {
        [DataMember]
        public BaseDC baseDC;

        [DataMember]
        public DerivedDC derivedDC;

        public TestInheritence3()
        {
        }

        public TestInheritence3(bool init)
        {
            derivedDC = new DerivedDC(true);
            baseDC = new BaseDC(true);
            derivedDC.data = baseDC.data;
            baseDC.Data = "String1";
            derivedDC.Data = baseDC.Data;
            derivedDC.data = baseDC.data2;
            baseDC.Data2 = "String2";
            baseDC.Data2 = baseDC.Data2;
            derivedDC.data = derivedDC.data1;
            derivedDC.Data1 = "String3";
            derivedDC.Data = derivedDC.Data1;
        }
    }

    [DataContract]
    public class TestInheritence16
    {
        [DataMember]
        public BaseSerializable baseDC;
        [DataMember]
        public DerivedSerializable derivedDC;

        public TestInheritence16()
        {
        }

        public TestInheritence16(bool init)
        {
            derivedDC = new DerivedSerializable(true);
            baseDC = new BaseSerializable(true);
            derivedDC.data = baseDC.data;
            baseDC.Data = "String1";
            derivedDC.Data = baseDC.Data;
            derivedDC.data = baseDC.data2;
            baseDC.Data2 = "String2";
            baseDC.Data2 = baseDC.Data2;
            derivedDC.data = derivedDC.data1;
            derivedDC.Data1 = "String3";
            derivedDC.Data = derivedDC.Data1;
        }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(DerivedDC))]
    public class TestInheritence4
    {
        [DataMember]
        public BaseDC baseDC;

        [DataMember]
        public DerivedDC derivedDC;

        public TestInheritence4()
        {
        }

        public TestInheritence4(bool init)
        {
            derivedDC = new DerivedDC(true);
            baseDC = new BaseDC(true);
            derivedDC.Data = "String1";
            ((BaseDC)derivedDC).Data = derivedDC.Data;
            ((BaseDC)derivedDC).data = derivedDC.data1;
            derivedDC.Data1 = "String2";
            ((BaseDC)derivedDC).Data = derivedDC.Data1;
            ((BaseDC)derivedDC).data = ((BaseDC)derivedDC).data2;
            ((BaseDC)derivedDC).Data2 = "String3";
            ((BaseDC)derivedDC).Data = ((BaseDC)derivedDC).Data2;
        }
    }

    [DataContract]
    public class TestInheritence12
    {
        [DataMember]
        public BaseSerializable baseDC;
        [DataMember]
        public DerivedSerializable derivedDC;

        public TestInheritence12()
        {
        }

        public TestInheritence12(bool init)
        {
            derivedDC = new DerivedSerializable(true);
            baseDC = new BaseSerializable(true);
            derivedDC.Data = "String1";
            ((BaseSerializable)derivedDC).Data = derivedDC.Data;
            ((BaseSerializable)derivedDC).data = derivedDC.data1;
            derivedDC.Data1 = "String2";
            ((BaseSerializable)derivedDC).Data = derivedDC.Data1;
            ((BaseSerializable)derivedDC).data = ((BaseSerializable)derivedDC).data2;
            ((BaseSerializable)derivedDC).Data2 = "String3";
            ((BaseSerializable)derivedDC).Data = ((BaseSerializable)derivedDC).Data2;
        }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(DerivedDC))]
    [KnownType(typeof(Derived2DC))]
    public class TestInheritence6
    {
        [DataMember]
        public BaseDC baseDC;

        [DataMember]
        public DerivedDC derivedDC;

        [DataMember]
        public Derived2DC derived2DC;

        public TestInheritence6()
        {
        }

        public TestInheritence6(bool init)
        {
            baseDC = new BaseDC(true);
            derivedDC = new DerivedDC(true);
            derived2DC = new Derived2DC(true);
            derived2DC.data = derivedDC.data;
            derivedDC.Data = "String1";
            derived2DC.Data = derivedDC.Data;
            derived2DC.data = derivedDC.data3;
            derivedDC.Data3 = "String2";
            derived2DC.Data = derivedDC.Data3;
            derived2DC.data4 = derivedDC.data1;
            derivedDC.Data1 = "String3";
            derived2DC.Data4 = derivedDC.Data1;
            derived2DC.data1 = derived2DC.data2;
            derived2DC.Data2 = "String4";
            derived2DC.Data1 = derived2DC.Data2;
        }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(Derived2DC))]
    public class TestInheritence7
    {
        [DataMember]
        public BaseDC baseDC;

        [DataMember]
        public Derived2DC derived2DC;
        
        public TestInheritence7()
        {
        }

        public TestInheritence7(bool init)
        {
            baseDC = new BaseDC(true);
            derived2DC = new Derived2DC(true);
            derived2DC.data = baseDC.data;
            baseDC.Data = "String1";
            derived2DC.Data = baseDC.Data;
            derived2DC.data = baseDC.data2;
            baseDC.Data2 = "String2";
            derived2DC.Data = baseDC.Data2;
        }
    }

    [DataContract]
    public class TestInheritence14
    {
        [DataMember]
        public BaseSerializable baseDC;
        [DataMember]
        public Derived2Serializable derived2DC;

        public TestInheritence14()
        {
        }

        public TestInheritence14(bool init)
        {
            baseDC = new BaseSerializable(true);
            derived2DC = new Derived2Serializable(true);
            derived2DC.data = baseDC.data;
            baseDC.Data = "String1";
            derived2DC.Data = baseDC.Data;
            derived2DC.data = baseDC.data2;
            baseDC.Data2 = "String2";
            derived2DC.Data = baseDC.Data2;
        }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(Derived2DC))]
    public class TestInheritence8
    {
        [DataMember]
        public BaseDC baseDC;

        [DataMember]
        public Derived2DC derived2DC;

        public TestInheritence8()
        {
        }

        public TestInheritence8(bool init)
        {
            derived2DC = new Derived2DC(true);
            baseDC = new BaseDC(true);
            baseDC.data = derived2DC.data;
            derived2DC.Data = "String1";
            baseDC.Data = derived2DC.Data;
            baseDC.data = derived2DC.data1;
            derived2DC.Data1 = "String2";
            baseDC.Data = derived2DC.Data1;
        }
    }
}
