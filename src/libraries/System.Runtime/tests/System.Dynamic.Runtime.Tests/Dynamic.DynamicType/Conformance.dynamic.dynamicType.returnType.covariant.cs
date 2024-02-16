// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.returnType.covariant.indexer001.indexer001
{
    // <Title>Covariant return type for dynamic/object</Title>
    // <Description> Tests simple indexers changing the return type from dynamic to object and vice versa
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public abstract class BaseObject
    {
        public virtual object this[int i]
        {
            get
            {
                Test.Called = "base";
                return new object();
            }
        }

        protected abstract object this[string s]
        {
            get;
        }

        internal virtual List<object> this[bool b]
        {
            get
            {
                Test.Called = "base";
                return new List<object>();
            }
        }

        public abstract Dictionary<object, Dictionary<string, List<object>>> this[char c]
        {
            get;
        }

        public virtual object[] this[float f]
        {
            get
            {
                Test.Called = "base";
                return new object[]
                {
                }

                ;
            }
        }

        public virtual object this[decimal d]
        {
            get
            {
                return new object();
            }
        }
    }

    public class Derive1Dynamic : BaseObject
    {
        public override dynamic[] this[float f]
        {
            get
            {
                Test.Called = "1derivedget";
                return null;
            }
        }

        public override dynamic this[int i]
        {
            get
            {
                Test.Called = "1derivedget";
                return null;
            }
        }

        protected override dynamic this[string s]
        {
            get
            {
                Test.Called = "1derivedget";
                return null;
            }
        }

        internal override List<dynamic> this[bool b]
        {
            get
            {
                Test.Called = "1derivedget";
                return null;
            }
        }

        public override Dictionary<dynamic, Dictionary<string, List<dynamic>>> this[char c]
        {
            get
            {
                Test.Called = "1derivedget";
                return null;
            }
        }

        public new dynamic this[decimal d]
        {
            get
            {
                Test.Called = "1derivedget";
                return null;
            }
        }

        public virtual bool TestSimpleCall()
        {
            bool rez = true;
            var r1 = this[3];
            try
            {
                r1.Foo(); //this will fail at runtime, validating that the return type for the method is dynamic
            }
            catch
            {
                if (Test.Called != "1derivedget")
                    rez = false;
            }

            var r2 = this['a'];
            try
            {
                r2["aa"]["aa"][0].Foo(); //this will fail at runtime, validating that the return type for the method is dynamic
            }
            catch
            {
                if (Test.Called != "1derivedget")
                    rez = false;
            }

            var r3 = this[true];
            try
            {
                r3[0].Foo(); //this will fail at runtime, validating that the return type for the method is dynamic
            }
            catch
            {
                if (Test.Called != "1derivedget")
                    rez = false;
            }

            var r4 = this[(decimal)2];
            try
            {
                r4.Foo();
            }
            catch
            {
                if (Test.Called != "1derivedget")
                    rez = false;
            }

            var r5 = this["adfa"];
            try
            {
                r5.Foo();
            }
            catch
            {
                if (Test.Called != "1derivedget")
                    rez = false;
            }

            var r6 = this[4f];
            try
            {
                r6[0].Foo();
            }
            catch
            {
                if (Test.Called != "1derivedget")
                    rez = false;
            }

            return rez;
        }
    }

    public class Derive2Object : Derive1Dynamic
    {
        public override object[] this[float f]
        {
            get
            {
                Test.Called = "2derivedget";
                return new object[]
                {
                3, 4
                }

                ;
            }
        }

        public override object this[int i]
        {
            get
            {
                Test.Called = "2derivedget";
                return new object();
            }
        }

        protected override object this[string s]
        {
            get
            {
                Test.Called = "2derivedget";
                return new object();
            }
        }

        internal override List<object> this[bool b]
        {
            get
            {
                Test.Called = "2derivedget";
                return new List<object>();
            }
        }

        public override Dictionary<object, Dictionary<string, List<object>>> this[char c]
        {
            get
            {
                Test.Called = "2derivedget";
                return new Dictionary<object, Dictionary<string, List<object>>>();
            }
        }

        public new object this[decimal d]
        {
            get
            {
                Test.Called = "2derivedget";
                return new object();
            }
        }

        public override bool TestSimpleCall()
        {
            bool rez = true;
            var r1 = this[3];
            if (Test.Called != "2derivedget" && r1 != null)
                rez = false;
            var r2 = this['d'];
            if (Test.Called != "2derivedget" && r2 != null)
                rez = false;
            var r3 = this["asdfas"];
            if (Test.Called != "2derivedget" && r3 != null)
                rez = false;
            var r4 = this[(decimal)43];
            if (Test.Called != "2derivedget" && r4 != null)
                rez = false;
            var r5 = this[true];
            if (Test.Called != "2derivedget" && r5 != null)
                rez = false;
            var r6 = this[4f];
            if (Test.Called != "2derivedget" && r6 != null)
                rez = false;
            return rez;
        }
    }

    public class Test
    {
        public static string Called;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int test = 0, success = 0;
            Derive1Dynamic der1 = new Derive1Dynamic();
            Derive2Object der2 = new Derive2Object();
            /// =============================================
            /// calling the methods on the first derived class
            /// =============================================
            test++;
            var r1 = der1[3];
            try
            {
                r1.Foo(); //this will fail at runtime, validating that the return type for the method is dynamic
            }
            catch
            {
                if (Test.Called == "1derivedget")
                    success++;
            }

            test++;
            var r2 = der1['a'];
            try
            {
                r2["aa"]["aa"][0].Foo(); //this will fail at runtime, validating that the return type for the method is dynamic
            }
            catch
            {
                if (Test.Called == "1derivedget")
                    success++;
            }

            test++;
            var r3 = der1[true];
            try
            {
                r3[0].Foo(); //this will fail at runtime, validating that the return type for the method is dynamic
            }
            catch
            {
                if (Test.Called == "1derivedget")
                    success++;
            }

            test++;
            var r4 = der1[decimal.MaxValue];
            try
            {
                r4.Foo();
            }
            catch
            {
                if (Test.Called == "1derivedget")
                    success++;
            }

            test++;
            var r9 = der1[4f];
            try
            {
                r9[0].Foo();
            }
            catch
            {
                if (Test.Called == "1derivedget")
                    success++;
            }

            test++;
            if (der1.TestSimpleCall())
                success++;
            /// =============================================
            /// calling the methods on the second derived class
            /// =============================================
            test++;
            var r5 = der2[3];
            if (Test.Called == "2derivedget" && r5 != null)
                success++;
            test++;
            var r6 = der2['a'];
            if (Test.Called == "2derivedget" && r6 != null)
                success++;
            test++;
            var r7 = der2[false];
            if (Test.Called == "2derivedget" && r7 != null)
                success++;
            test++;
            var r8 = der2[decimal.MinusOne];
            if (Test.Called == "2derivedget" && r8 != null)
                success++;
            test++;
            if (der2.TestSimpleCall())
                success++;
            return test == success ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.returnType.covariant.indexer003.indexer003
{
    // <Title>Covariant return type for dynamic/object</Title>
    // <Description> Tests generic indexer changing the return type from dynamic to object and vice versa
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public abstract class BaseObject<T>
    {
        protected T storage;
        public virtual T this[int x]
        {
            get
            {
                Test.Called = "baseGet";
                return storage;
            }

            set
            {
                Test.Called = "baseSet";
                storage = value;
            }
        }
    }

    public class Derive1Dynamic : BaseObject<object>
    {
        public override dynamic this[int x]
        {
            get
            {
                Test.Called = "DerGet";
                return storage;
            }

            set
            {
                Test.Called = "DerSet";
                storage = value;
            }
        }
    }

    public class Derive2Object : Derive1Dynamic
    {
        public override object this[int x]
        {
            get
            {
                Test.Called = "Der2Get";
                return storage;
            }

            set
            {
                Test.Called = "Der2Set";
                storage = value;
            }
        }
    }

    public class Test
    {
        public static string Called;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int test = 0, success = 0;
            Derive1Dynamic der = new Derive1Dynamic();
            test++;
            //test the right method was called
            der[3] = 1;
            var x = der[3];
            try
            {
                x.Foo();
            }
            catch
            {
                if (x == 1 && Test.Called == "DerGet")
                    success++;
            }

            test++;
            //we should be calling the most derived method
            der = new Derive2Object();
            der[5] = 3;
            var y = der[5];
            try
            {
                y.Foo();
            }
            catch
            {
                if (y == 3 && Test.Called == "Der2Get")
                    success++;
            }

            return test == success ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.returnType.covariant.integeregererface001.integeregererface001
{
    // <Title>Covariant return type for dynamic/object</Title>
    // <Description> Tests interface implementation of methods
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public interface IRetObj
    {
        //methods
        dynamic RetObj();
        List<dynamic> RetListObj();
        //properties
        dynamic Prop1
        {
            get;
            set;
        }

        Dictionary<string, dynamic> Prop2
        {
            get;
            set;
        }

        //indexers
        dynamic this[float f]
        {
            get;
            set;
        }

        Stack<dynamic> this[string s]
        {
            get;
            set;
        }
    }

    public class RetObjBase : IRetObj
    {
        public virtual object RetObj()
        {
            Test.Called = "baseRetObj";
            return 1;
        }

        public virtual List<object> RetListObj()
        {
            Test.Called = "baseRetListObj";
            return new List<object>()
            {
            2
            }

            ;
        }

        public virtual object Prop1
        {
            get
            {
                Test.Called = "baseProp1Get";
                return 1;
            }

            set
            {
                Test.Called = "baseProp1Set";
            }
        }

        public virtual Dictionary<string, object> Prop2
        {
            get
            {
                Test.Called = "baseProp2Get";
                return new Dictionary<string, object>();
            }

            set
            {
                Test.Called = "baseProp2Set";
            }
        }

        public virtual object this[float f]
        {
            get
            {
                Test.Called = "baseindex1Get";
                return "";
            }

            set
            {
                Test.Called = "baseindex1Set";
            }
        }

        public virtual Stack<object> this[string s]
        {
            get
            {
                Test.Called = "baseindex2Get";
                return new Stack<object>();
            }

            set
            {
                Test.Called = "baseindex2Set";
            }
        }
    }

    public class RetObjDerived : RetObjBase
    {
        public override dynamic RetObj()
        {
            Test.Called = "derivedRetObj";
            return 2;
        }

        public override List<dynamic> RetListObj()
        {
            Test.Called = "derivedRetListObj";
            return new List<dynamic>()
            {
            3
            }

            ;
        }

        public override dynamic Prop1
        {
            get
            {
                Test.Called = "derivedProp1Get";
                return 1;
            }

            set
            {
                Test.Called = "derivedProp1Set";
            }
        }

        public override Dictionary<string, dynamic> Prop2
        {
            get
            {
                Test.Called = "derivedProp2Get";
                return new Dictionary<string, dynamic>();
            }

            set
            {
                Test.Called = "derivedProp2Set";
            }
        }

        public override dynamic this[float f]
        {
            get
            {
                Test.Called = "derivedindex1Get";
                return "";
            }

            set
            {
                Test.Called = "derivedindex1Set";
            }
        }

        public override Stack<dynamic> this[string s]
        {
            get
            {
                Test.Called = "derivedindex2Get";
                var ss = new Stack<dynamic>();
                ss.Push(1);
                return ss;
            }

            set
            {
                Test.Called = "derivedindex2Set";
            }
        }
    }

    public class Test
    {
        public static string Called;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int test = 0, success = 0;
            ///calling off the class object
            RetObjDerived der = new RetObjDerived();
            //---
            //Methods
            //---
            test++;
            Test.Called = string.Empty;
            var r1 = der.RetObj();
            try
            {
                r1.Foo();
            }
            catch
            {
                if (Test.Called == "derivedRetObj")
                    success++;
            }

            test++;
            Test.Called = string.Empty;
            var r2 = der.RetListObj();
            try
            {
                r2[0].Foo();
            }
            catch
            {
                if (Test.Called == "derivedRetListObj")
                    success++;
            }

            //---
            //Properties
            //---
            test++;
            Test.Called = string.Empty;
            var r3 = der.Prop1;
            try
            {
                r3.Foo();
            }
            catch
            {
                if (Test.Called == "derivedProp1Get")
                    success++;
            }

            test++;
            Test.Called = string.Empty;
            var r4 = der.Prop2;
            try
            {
                r4[""].Foo();
            }
            catch
            {
                if (Test.Called == "derivedProp2Get")
                    success++;
            }

            //---
            //Indexers
            //---
            test++;
            Test.Called = string.Empty;
            var r5 = der[34f];
            try
            {
                r5.Foo();
            }
            catch
            {
                if (Test.Called == "derivedindex1Get")
                    success++;
            }

            test++;
            Test.Called = string.Empty;
            var r6 = der[""];
            try
            {
                r6.Pop().Foo();
            }
            catch
            {
                if (Test.Called == "derivedindex2Get")
                    success++;
            }

            //=====================================================================================
            //calling off the interface object
            IRetObj interf = der;
            //---
            //Methods
            //---
            test++;
            Test.Called = string.Empty;
            var r7 = interf.RetObj();
            try
            {
                r7.Foo();
            }
            catch
            {
                if (Test.Called == "derivedRetObj")
                    success++;
            }

            test++;
            Test.Called = string.Empty;
            var r8 = interf.RetListObj();
            try
            {
                r8[0].Foo();
            }
            catch
            {
                if (Test.Called == "derivedRetListObj")
                    success++;
            }

            //---
            //Properties
            //---
            test++;
            Test.Called = string.Empty;
            var r9 = interf.Prop1;
            try
            {
                r9.Foo();
            }
            catch
            {
                if (Test.Called == "derivedProp1Get")
                    success++;
            }

            test++;
            Test.Called = string.Empty;
            var r10 = interf.Prop2;
            try
            {
                r10[""].Foo();
            }
            catch
            {
                if (Test.Called == "derivedProp2Get")
                    success++;
            }

            //---
            //Indexers
            //---
            test++;
            Test.Called = string.Empty;
            var r11 = interf[34f];
            try
            {
                r11.Foo();
            }
            catch
            {
                if (Test.Called == "derivedindex1Get")
                    success++;
            }

            test++;
            Test.Called = string.Empty;
            var r12 = interf[""];
            try
            {
                r12.Pop().Foo();
            }
            catch
            {
                if (Test.Called == "derivedindex2Get")
                    success++;
            }

            return test == success ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.returnType.covariant.integeregererface003.integeregererface003
{
    // <Title>Covariant return type for dynamic/object</Title>
    // <Description> Tests interface explicit implementation
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public interface IRetObj
    {
        //methods
        dynamic RetObj();
        List<dynamic> RetListObj();
        //properties
        dynamic Prop1
        {
            get;
            set;
        }

        Dictionary<string, dynamic> Prop2
        {
            get;
            set;
        }

        //indexers
        dynamic this[float f]
        {
            get;
            set;
        }

        Stack<dynamic> this[string s]
        {
            get;
            set;
        }
    }

    public class RetObjBase : IRetObj
    {
        object IRetObj.RetObj()
        {
            Test.Called = "baseRetObj";
            return 1;
        }

        List<object> IRetObj.RetListObj()
        {
            Test.Called = "baseRetListObj";
            return new List<object>()
            {
            2
            }

            ;
        }

        object IRetObj.Prop1
        {
            get
            {
                Test.Called = "baseProp1Get";
                return 1;
            }

            set
            {
                Test.Called = "baseProp1Set";
            }
        }

        Dictionary<string, object> IRetObj.Prop2
        {
            get
            {
                Test.Called = "baseProp2Get";
                return new Dictionary<string, object>();
            }

            set
            {
                Test.Called = "baseProp2Set";
            }
        }

        object IRetObj.this[float f]
        {
            get
            {
                Test.Called = "baseindex1Get";
                return "";
            }

            set
            {
                Test.Called = "baseindex1Set";
            }
        }

        Stack<object> IRetObj.this[string s]
        {
            get
            {
                Test.Called = "baseindex2Get";
                return new Stack<object>();
            }

            set
            {
                Test.Called = "baseindex2Set";
            }
        }
    }

    public class Test
    {
        public static string Called;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int test = 0, success = 0;
            ///calling off the class object
            RetObjBase bas = new RetObjBase();
            //calling off the interface object
            IRetObj interf = bas;
            //---
            //Methods
            //---
            test++;
            Test.Called = string.Empty;
            var r7 = interf.RetObj();
            try
            {
                r7.Foo();
            }
            catch
            {
                if (Test.Called == "baseRetObj")
                    success++;
            }

            test++;
            Test.Called = string.Empty;
            var r8 = interf.RetListObj();
            try
            {
                r8[0].Foo();
            }
            catch
            {
                if (Test.Called == "baseRetListObj")
                    success++;
            }

            //---
            //Properties
            //---
            test++;
            Test.Called = string.Empty;
            var r9 = interf.Prop1;
            try
            {
                r9.Foo();
            }
            catch
            {
                if (Test.Called == "baseProp1Get")
                    success++;
            }

            test++;
            Test.Called = string.Empty;
            var r10 = interf.Prop2;
            try
            {
                r10[""].Foo();
            }
            catch
            {
                if (Test.Called == "baseProp2Get")
                    success++;
            }

            //---
            //Indexers
            //---
            test++;
            Test.Called = string.Empty;
            var r11 = interf[34f];
            try
            {
                r11.Foo();
            }
            catch
            {
                if (Test.Called == "baseindex1Get")
                    success++;
            }

            test++;
            Test.Called = string.Empty;
            var r12 = interf[""];
            try
            {
                r12.Pop().Foo();
            }
            catch
            {
                if (Test.Called == "baseindex2Get")
                    success++;
            }

            return test == success ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.returnType.covariant.integeregererface004.integeregererface004
{
    // <Title>Covariant return type for dynamic/object</Title>
    // <Description> Tests interface implementation
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public interface IFoo
    {
        object Method();
    }

    public interface IFoo2
    {
        dynamic Method();
    }

    public class Base : IFoo, IFoo2
    {
        public virtual object Method()
        {
            return 1;
        }

        object IFoo2.Method()
        {
            return 2;
        }
    }

    public class Derived : Base, IFoo2
    {
        public override dynamic Method()
        {
            return 3;
        }

        dynamic IFoo2.Method()
        {
            return 5;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int test = 0, success = 0;
            Derived der = new Derived();
            test++;
            var x = der.Method();
            try
            {
                x.Foo();
            }
            catch
            {
                //the override in the derived class
                if (x == 3)
                    success++;
            }

            test++;
            IFoo2 interf = der;
            var y = interf.Method();
            try
            {
                y.Foo();
            }
            catch
            {
                //the explicit implementation in the derived class
                if (y == 5)
                    success++;
            }

            return test == success ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.returnType.covariant.method001.method001
{
    // <Title>Covariant return type for dynamic/object</Title>
    // <Description> Tests simple method changing the return type from dynamic to object and vice versa
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public abstract class BaseObject
    {
        public virtual object RetObject()
        {
            Test.Called = "base";
            return new object();
        }

        protected abstract object RetObject2();
        internal virtual List<object> RetListObject()
        {
            Test.Called = "base";
            return new List<object>();
        }

        public abstract Dictionary<object, Dictionary<string, List<object>>> RetComplex();
        public virtual object[] RetArray()
        {
            return new object[]
            {
            1
            }

            ;
        }

        public virtual object NewMethod()
        {
            return new object();
        }
    }

    public class Derive1Dynamic : BaseObject
    {
        public override dynamic[] RetArray()
        {
            Test.Called = "1derived";
            return null;
        }

        public override dynamic RetObject()
        {
            Test.Called = "1derived";
            return null;
        }

        protected override dynamic RetObject2()
        {
            Test.Called = "1derived";
            return null;
        }

        internal override List<dynamic> RetListObject()
        {
            Test.Called = "1derived";
            return null;
        }

        public override Dictionary<dynamic, Dictionary<string, List<dynamic>>> RetComplex()
        {
            Test.Called = "1derived";
            return null;
        }

        public new dynamic NewMethod()
        {
            Test.Called = "1derived";
            return null;
        }

        public virtual bool TestSimpleCall()
        {
            bool rez = true;
            var r1 = RetObject();
            try
            {
                r1.Foo(); //this will fail at runtime, validating that the return type for the method is dynamic
            }
            catch
            {
                if (Test.Called != "1derived")
                    rez = false;
            }

            var r2 = RetComplex();
            try
            {
                r2["aa"]["aa"][0].Foo(); //this will fail at runtime, validating that the return type for the method is dynamic
            }
            catch
            {
                if (Test.Called != "1derived")
                    rez = false;
            }

            var r3 = RetListObject();
            try
            {
                r3[0].Foo(); //this will fail at runtime, validating that the return type for the method is dynamic
            }
            catch
            {
                if (Test.Called != "1derived")
                    rez = false;
            }

            var r4 = NewMethod();
            try
            {
                r4.Foo();
            }
            catch
            {
                if (Test.Called != "1derived")
                    rez = false;
            }

            var r5 = RetObject2();
            try
            {
                r5.Foo();
            }
            catch
            {
                if (Test.Called != "1derived")
                    rez = false;
            }

            var r6 = RetArray();
            try
            {
                r6[0].Foo();
            }
            catch
            {
                if (Test.Called != "1derived")
                    rez = false;
            }

            return rez;
        }
    }

    public class Derive2Object : Derive1Dynamic
    {
        public override object[] RetArray()
        {
            Test.Called = "2derived";
            return new object[]
            {
            1, 2, 3
            }

            ;
        }

        public override object RetObject()
        {
            Test.Called = "2derived";
            return new object();
        }

        protected override object RetObject2()
        {
            Test.Called = "2derived";
            return new object();
        }

        internal override List<object> RetListObject()
        {
            Test.Called = "2derived";
            return new List<object>();
        }

        public override Dictionary<object, Dictionary<string, List<object>>> RetComplex()
        {
            Test.Called = "2derived";
            return new Dictionary<object, Dictionary<string, List<object>>>();
        }

        public new object NewMethod()
        {
            Test.Called = "2derived";
            return new object();
        }

        public override bool TestSimpleCall()
        {
            bool rez = true;
            var r1 = RetObject();
            if (Test.Called != "2derived" && r1 != null)
                rez = false;
            var r2 = RetComplex();
            if (Test.Called != "2derived" && r2 != null)
                rez = false;
            var r3 = RetListObject();
            if (Test.Called != "2derived" && r3 != null)
                rez = false;
            var r4 = NewMethod();
            if (Test.Called != "2derived" && r4 != null)
                rez = false;
            var r5 = RetObject2();
            if (Test.Called != "2derived" && r5 != null)
                rez = false;
            var r6 = RetArray();
            if (Test.Called != "2derived" && r6 != null)
                rez = false;
            return rez;
        }
    }

    public class Test
    {
        public static string Called;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int test = 0, success = 0;
            Derive1Dynamic der1 = new Derive1Dynamic();
            Derive2Object der2 = new Derive2Object();
            /// =============================================
            /// calling the methods on the first derived class
            /// =============================================
            test++;
            var r1 = der1.RetObject();
            try
            {
                r1.Foo(); //this will fail at runtime, validating that the return type for the method is dynamic
            }
            catch
            {
                if (Test.Called == "1derived")
                    success++;
            }

            test++;
            var r2 = der1.RetComplex();
            try
            {
                r2["aa"]["aa"][0].Foo(); //this will fail at runtime, validating that the return type for the method is dynamic
            }
            catch
            {
                if (Test.Called == "1derived")
                    success++;
            }

            test++;
            var r3 = der1.RetListObject();
            try
            {
                r3[0].Foo(); //this will fail at runtime, validating that the return type for the method is dynamic
            }
            catch
            {
                if (Test.Called == "1derived")
                    success++;
            }

            test++;
            var r4 = der1.NewMethod();
            try
            {
                r4.Foo();
            }
            catch
            {
                if (Test.Called == "1derived")
                    success++;
            }

            test++;
            var r9 = der1.RetArray();
            try
            {
                r9[0].Foo();
            }
            catch
            {
                if (Test.Called == "1derived")
                    success++;
            }

            test++;
            if (der1.TestSimpleCall())
                success++;
            /// =============================================
            /// calling the methods on the second derived class
            /// =============================================
            test++;
            var r5 = der2.RetObject();
            if (Test.Called == "2derived" && r5 != null)
                success++;
            test++;
            var r6 = der2.RetComplex();
            if (Test.Called == "2derived" && r6 != null)
                success++;
            test++;
            var r7 = der2.RetListObject();
            if (Test.Called == "2derived" && r7 != null)
                success++;
            test++;
            var r8 = der2.NewMethod();
            if (Test.Called == "2derived" && r8 != null)
                success++;
            test++;
            if (der2.TestSimpleCall())
                success++;
            return test == success ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.returnType.covariant.method003.method003
{
    // <Title>Covariant return type for dynamic/object</Title>
    // <Description> Tests generic methods changing the return type from dynamic to object and vice versa
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public abstract class BaseObject<T>
    {
        public virtual T RetObject()
        {
            Test.Called = "base";
            return default(T);
        }
    }

    public class Derive1Dynamic : BaseObject<object>
    {
        public override dynamic RetObject()
        {
            Test.Called = "derived";
            return 1;
        }
    }

    public class Derive2Object : Derive1Dynamic
    {
        public override object RetObject()
        {
            Test.Called = "derived2";
            return 3;
        }
    }

    public class Test
    {
        public static string Called;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int test = 0, success = 0;
            Derive1Dynamic der = new Derive1Dynamic();
            test++;
            //test the right method was called
            var x = der.RetObject();
            try
            {
                x.Foo();
            }
            catch
            {
                if (x == 1 && Test.Called == "derived")
                    success++;
            }

            test++;
            //we should be calling the most derived method
            der = new Derive2Object();
            var y = der.RetObject();
            try
            {
                y.Foo();
            }
            catch
            {
                if (y == 3 && Test.Called == "derived2")
                    success++;
            }

            return test == success ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.returnType.covariant.method004.method004
{
    // <Title>Covariant return type for dynamic/object</Title>
    // <Description> Tests generic method changing the return type from dynamic to object.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public abstract class BaseObject<T>
    {
        public virtual T RetObject<U>()
        {
            Test.Called = "base";
            return default(T);
        }

        public virtual T RetObjectConstr<U>(U u) where U : T
        {
            Test.Called = "baseConstr";
            return default(U);
        }

        internal virtual List<T> RetListObject<U>()
        {
            return new List<T>();
        }

        public abstract Dictionary<object, Dictionary<U, List<object>>> RetComplex<U>();
    }

    public class DerivedDynamic : BaseObject<object>
    {
        public override dynamic RetObject<U>()
        {
            Test.Called = "der1";
            return default(U);
        }

        public override dynamic RetObjectConstr<U>(U u)
        {
            Test.Called = "der1";
            return 3 as dynamic;
        }

        internal override List<dynamic> RetListObject<U>()
        {
            Test.Called = "der1";
            return new List<dynamic>()
            {
            1
            }

            ;
        }

        public override Dictionary<dynamic, Dictionary<U, List<dynamic>>> RetComplex<U>()
        {
            Test.Called = "der1";
            return new Dictionary<dynamic, Dictionary<U, List<dynamic>>>();
        }
    }

    public class DerivedObject : DerivedDynamic
    {
        public override object RetObject<U>()
        {
            Test.Called = "der2";
            return default(U);
        }

        public override object RetObjectConstr<U>(U u)
        {
            Test.Called = "der2";
            return 3 as object;
        }

        internal override List<object> RetListObject<U>()
        {
            Test.Called = "der2";
            return new List<object>()
            {
            1
            }

            ;
        }

        public override Dictionary<object, Dictionary<U, List<object>>> RetComplex<U>()
        {
            Test.Called = "der2";
            return new Dictionary<object, Dictionary<U, List<object>>>();
        }
    }

    public class Test
    {
        public static string Called;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int test = 0, success = 0;
            // -----
            // Instantiate the dynamic class
            // ---=
            DerivedDynamic der = new DerivedDynamic();
            test++;
            Test.Called = string.Empty;
            var r1 = der.RetComplex<char>();
            try
            {
                r1[""]['a'][0].Foo(); //this will fail at runtime, validating that the return type for the method is dynamic
            }
            catch
            {
                if (Test.Called == "der1")
                    success++;
            }

            test++;
            Test.Called = string.Empty;
            var r2 = der.RetListObject<int>();
            try
            {
                r2[0].Foo(); //this will fail at runtime, validating that the return type for the method is dynamic
            }
            catch
            {
                if (Test.Called == "der1")
                    success++;
            }

            test++;
            Test.Called = string.Empty;
            var r3 = der.RetObject<dynamic>();
            try
            {
                r3.Foo(); //this will fail at runtime, validating that the return type for the method is dynamic
            }
            catch
            {
                if (Test.Called == "der1")
                    success++;
            }

            test++;
            Test.Called = string.Empty;
            var r4 = der.RetObjectConstr<string>("a");
            try
            {
                r4.Foo(); //this will fail at runtime, validating that the return type for the method is dynamic
            }
            catch
            {
                if (Test.Called == "der1")
                    success++;
            }

            // -----
            // Instantiate the object  class and stick in into the dynamic one
            // Make sure we use the type of the most derived class, up to the type of the variable
            // ---=
            der = new DerivedObject();
            test++;
            Test.Called = string.Empty;
            var r5 = der.RetComplex<char>();
            try
            {
                r5[""]['a'][0].Foo(); //this will fail at runtime, validating that the return type for the method is dynamic
            }
            catch
            {
                if (Test.Called == "der2")
                    success++;
            }

            test++;
            Test.Called = string.Empty;
            var r7 = der.RetObject<dynamic>();
            try
            {
                r7.Foo(); //this will fail at runtime, validating that the return type for the method is dynamic
            }
            catch
            {
                if (Test.Called == "der2")
                    success++;
            }

            return test == success ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.returnType.covariant.property001.property001
{
    // <Title>Covariant return type for dynamic/object</Title>
    // <Description> Tests simple indexers changing the return type from dynamic to object and vice versa
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public abstract class BaseObject
    {
        public virtual object Prop1
        {
            get
            {
                Test.Called = "base";
                return new object();
            }
        }

        protected abstract object Prop2
        {
            get;
        }

        internal virtual List<object> Prop3
        {
            get
            {
                Test.Called = "base";
                return new List<object>();
            }
        }

        public abstract Dictionary<object, Dictionary<string, List<object>>> Prop4
        {
            get;
        }

        public virtual object Prop5
        {
            get
            {
                return new object();
            }
        }

        public virtual object[] Prop6
        {
            get
            {
                return new object[]
                {
                1, 2, 3
                }

                ;
            }
        }
    }

    public class Derive1Dynamic : BaseObject
    {
        public override dynamic[] Prop6
        {
            get
            {
                Test.Called = "1derivedget";
                return null;
            }
        }

        public override dynamic Prop1
        {
            get
            {
                Test.Called = "1derivedget";
                return null;
            }
        }

        protected override dynamic Prop2
        {
            get
            {
                Test.Called = "1derivedget";
                return null;
            }
        }

        internal override List<dynamic> Prop3
        {
            get
            {
                Test.Called = "1derivedget";
                return null;
            }
        }

        public override Dictionary<dynamic, Dictionary<string, List<dynamic>>> Prop4
        {
            get
            {
                Test.Called = "1derivedget";
                return null;
            }
        }

        public new dynamic Prop5
        {
            get
            {
                Test.Called = "1derivedget";
                return null;
            }
        }

        public virtual bool TestSimpleCall()
        {
            bool rez = true;
            var r1 = Prop1;
            try
            {
                r1.Foo(); //this will fail at runtime, validating that the return type for the method is dynamic
            }
            catch
            {
                if (Test.Called != "1derivedget")
                    rez = false;
            }

            var r2 = Prop2;
            try
            {
                r2["aa"]["aa"][0].Foo(); //this will fail at runtime, validating that the return type for the method is dynamic
            }
            catch
            {
                if (Test.Called != "1derivedget")
                    rez = false;
            }

            var r3 = Prop3;
            try
            {
                r3[0].Foo(); //this will fail at runtime, validating that the return type for the method is dynamic
            }
            catch
            {
                if (Test.Called != "1derivedget")
                    rez = false;
            }

            var r4 = Prop2;
            try
            {
                r4.Foo();
            }
            catch
            {
                if (Test.Called != "1derivedget")
                    rez = false;
            }

            var r5 = Prop5;
            try
            {
                r5.Foo();
            }
            catch
            {
                if (Test.Called != "1derivedget")
                    rez = false;
            }

            var r6 = Prop6;
            try
            {
                r6[0].Foo();
            }
            catch
            {
                if (Test.Called != "1derivedget")
                    rez = false;
            }

            return rez;
        }
    }

    public class Derive2Object : Derive1Dynamic
    {
        public override object[] Prop6
        {
            get
            {
                Test.Called = "2derivedget";
                return new object[]
                {
                1, 2
                }

                ;
            }
        }

        public override object Prop1
        {
            get
            {
                Test.Called = "2derivedget";
                return new object();
            }
        }

        protected override object Prop2
        {
            get
            {
                Test.Called = "2derivedget";
                return new object();
            }
        }

        internal override List<object> Prop3
        {
            get
            {
                Test.Called = "2derivedget";
                return new List<object>();
            }
        }

        public override Dictionary<object, Dictionary<string, List<object>>> Prop4
        {
            get
            {
                Test.Called = "2derivedget";
                return new Dictionary<object, Dictionary<string, List<object>>>();
            }
        }

        public new object Prop5
        {
            get
            {
                Test.Called = "2derivedget";
                return new object();
            }
        }

        public override bool TestSimpleCall()
        {
            bool rez = true;
            var r1 = Prop1;
            if (Test.Called != "2derivedget" && r1 != null)
                rez = false;
            var r2 = Prop2;
            if (Test.Called != "2derivedget" && r2 != null)
                rez = false;
            var r3 = Prop3;
            if (Test.Called != "2derivedget" && r3 != null)
                rez = false;
            var r4 = Prop4;
            if (Test.Called != "2derivedget" && r4 != null)
                rez = false;
            var r5 = Prop5;
            if (Test.Called != "2derivedget" && r5 != null)
                rez = false;
            var r6 = Prop6;
            if (Test.Called != "2derivedget" && r6 != null)
                rez = false;
            return rez;
        }
    }

    public class Test
    {
        public static string Called;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int test = 0, success = 0;
            Derive1Dynamic der1 = new Derive1Dynamic();
            Derive2Object der2 = new Derive2Object();
            /// =============================================
            /// calling the methods on the first derived class
            /// =============================================
            test++;
            var r1 = der1.Prop1;
            try
            {
                r1.Foo(); //this will fail at runtime, validating that the return type for the method is dynamic
            }
            catch
            {
                if (Test.Called == "1derivedget")
                    success++;
            }

            test++;
            var r2 = der1.Prop4;
            try
            {
                r2["aa"]["aa"][0].Foo(); //this will fail at runtime, validating that the return type for the method is dynamic
            }
            catch
            {
                if (Test.Called == "1derivedget")
                    success++;
            }

            test++;
            var r3 = der1.Prop3;
            try
            {
                r3[0].Foo(); //this will fail at runtime, validating that the return type for the method is dynamic
            }
            catch
            {
                if (Test.Called == "1derivedget")
                    success++;
            }

            test++;
            var r4 = der1.Prop5;
            try
            {
                r4.Foo();
            }
            catch
            {
                if (Test.Called == "1derivedget")
                    success++;
            }

            test++;
            var r9 = der1.Prop6;
            try
            {
                r9[0].Foo();
            }
            catch
            {
                if (Test.Called == "1derivedget")
                    success++;
            }

            test++;
            if (der1.TestSimpleCall())
                success++;
            /// =============================================
            /// calling the methods on the second derived class
            /// =============================================
            test++;
            var r5 = der2.Prop1;
            if (Test.Called == "2derivedget" && r5 != null)
                success++;
            test++;
            var r6 = der2.Prop3;
            if (Test.Called == "2derivedget" && r6 != null)
                success++;
            test++;
            var r7 = der2.Prop4;
            if (Test.Called == "2derivedget" && r7 != null)
                success++;
            test++;
            var r8 = der2.Prop5;
            if (Test.Called == "2derivedget" && r8 != null)
                success++;
            test++;
            if (der2.TestSimpleCall())
                success++;
            return test == success ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.returnType.covariant.property003.property003
{
    // <Title>Covariant return type for dynamic/object</Title>
    // <Description> Tests generic indexer changing the return type from dynamic to object and vice versa
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public abstract class BaseObject<T>
    {
        protected T storage;
        public virtual T Prop
        {
            get
            {
                Test.Called = "baseGet";
                return storage;
            }

            set
            {
                Test.Called = "baseSet";
                storage = value;
            }
        }
    }

    public class Derive1Dynamic : BaseObject<object>
    {
        public override dynamic Prop
        {
            get
            {
                Test.Called = "DerGet";
                return storage;
            }

            set
            {
                Test.Called = "DerSet";
                storage = value;
            }
        }
    }

    public class Derive2Object : Derive1Dynamic
    {
        public override object Prop
        {
            get
            {
                Test.Called = "Der2Get";
                return storage;
            }

            set
            {
                Test.Called = "Der2Set";
                storage = value;
            }
        }
    }

    public class Test
    {
        public static string Called;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int test = 0, success = 0;
            Derive1Dynamic der = new Derive1Dynamic();
            test++;
            //test the right method was called
            der.Prop = 1;
            var x = der.Prop;
            try
            {
                x.Foo();
            }
            catch
            {
                if (x == 1 && Test.Called == "DerGet")
                    success++;
            }

            test++;
            //we should be calling the most derived method
            der = new Derive2Object();
            der.Prop = 3;
            var y = der.Prop;
            try
            {
                y.Foo();
            }
            catch
            {
                if (y == 3 && Test.Called == "Der2Get")
                    success++;
            }

            return test == success ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.returnType.covariant.usage001.usage001
{
    // <Title>Covariant return type for dynamic/object</Title>
    // <Description> Tests simple method changing the return type from dynamic to object and vice versa
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class Base
    {
        public virtual object Method()
        {
            Test.Called += "BaseM";
            return new Base();
        }

        public virtual List<object> MethodGeneric()
        {
            Test.Called += "BaseML";
            return new List<object>()
            {
            new Base()}

            ;
        }

        public virtual List<object> this[int x]
        {
            get
            {
                Test.Called += "BaseGetIL";
                return new List<object>()
                {
                new Base()}

                ;
            }

            set
            {
                Test.Called += "BaseSetIL";
            }
        }

        public virtual object[] Prop
        {
            get
            {
                Test.Called += "BaseGetP";
                return new object[]
                {
                new Base()}

                ;
            }

            set
            {
                Test.Called += "BaseSetP";
            }
        }

        public virtual object Prop2
        {
            get
            {
                Test.Called += "BaseGetP2";
                return new Base();
            }

            set
            {
                Test.Called += "BaseSetP2";
            }
        }
    }

    public class Derived : Base
    {
        public override dynamic Method()
        {
            Test.Called += "DerivedM";
            return new Derived();
        }

        public override List<dynamic> MethodGeneric()
        {
            Test.Called += "DerivedML";
            return new List<dynamic>()
            {
            new Derived()}

            ;
        }

        public override List<dynamic> this[int x]
        {
            get
            {
                Test.Called += "DerivedGetIL";
                return new List<dynamic>()
                {
                new Derived()}

                ;
            }

            set
            {
                Test.Called += "DerivedSetIL";
            }
        }

        public override dynamic[] Prop
        {
            get
            {
                Test.Called += "DerivedGetP";
                return new dynamic[]
                {
                new Derived()}

                ;
            }

            set
            {
                Test.Called += "DerivedSetP";
            }
        }

        public override dynamic Prop2
        {
            get
            {
                Test.Called += "DerivedGetP2";
                return new Derived();
            }

            set
            {
                Test.Called += "DerivedSetP2";
            }
        }
    }

    public class Test
    {
        public static string Called;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int test = 0, success = 0;
            Derived der = new Derived();
            test++;
            Test.Called = string.Empty;
            der.Method().Method().Method();
            if (Test.Called == "DerivedMDerivedMDerivedM")
                success++;
            test++;
            Test.Called = string.Empty;
            der.MethodGeneric()[0].Method().MethodGeneric()[0].Method();
            if (Test.Called == "DerivedMLDerivedMDerivedMLDerivedM")
                success++;
            test++;
            Test.Called = string.Empty;
            der[0][0][0][0].Method().MethodGeneric()[0].Method();
            if (Test.Called == "DerivedGetILDerivedGetILDerivedMDerivedMLDerivedM")
                success++;
            test++;
            Test.Called = string.Empty;
            der.Prop[0][0][0].Method(); //(der.Prop[0] -> dynamic [0] -> List<dynamic> [0] -> dynamic .Method() -> dynamic
            if (Test.Called == "DerivedGetPDerivedGetILDerivedM")
                success++;
            test++;
            Test.Called = string.Empty;
            der.Prop2[0][0].Method(); //
            if (Test.Called == "DerivedGetP2DerivedGetILDerivedM")
                success++;
            test++;
            Test.Called = string.Empty;
            der[0][0].Prop2[0][0].Method(); //
            if (Test.Called == "DerivedGetILDerivedGetP2DerivedGetILDerivedM")
                success++;
            //ternary operator
            test++;
            Test.Called = string.Empty;
            Base bas = new Base();
            bool condition = true;
            var r1 = condition ? der.Method() : bas.Method();
            r1.Method();
            if (Test.Called == "DerivedMDerivedM")
                success++;
            test++;
            Test.Called = string.Empty;
            condition = false;
            var r2 = condition ? der.Method() : bas.Method();
            r2.Method();
            if (Test.Called == "BaseMBaseM")
                success++;
            //null coalescing operator
            test++;
            Test.Called = string.Empty;
            object obj = null;
            var r3 = obj ?? der.Method();
            r3.Method();
            if (Test.Called == "DerivedMDerivedM")
                success++;
            test++;
            Test.Called = string.Empty;
            obj = der;
            var r4 = obj ?? der.Method();
            r4.Method();
            if (Test.Called == "DerivedM")
                success++;
            return test == success ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.returnType.covariant.usage003.usage003
{
    // <Title>Covariant return type for dynamic/object</Title>
    // <Description> implicitly typed arrays, anon types, collection init
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Base
    {
        public virtual object Prop
        {
            get
            {
                Test.Called += "BaseGetP";
                return new Base();
            }

            set
            {
                Test.Called += "BaseSetP";
            }
        }
    }

    public class Derived : Base
    {
        public override dynamic Prop
        {
            get
            {
                Test.Called += "DerivedGetP";
                return new Derived();
            }

            set
            {
                Test.Called += "DerivedSetP";
            }
        }
    }

    public class Test
    {
        public static string Called;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int test = 0, success = 0;
            Derived d = new Derived();
            //implicitly typed array initializers
            test++;
            Test.Called = string.Empty;
            var x = new[]
            {
            d.Prop
            }

            ;
            var y = x[0].Prop;
            if (Test.Called == "DerivedGetPDerivedGetP")
                success++;
            test++;
            Base b = new Base();
            Test.Called = string.Empty;
            var z = new[]
            {
            d.Prop, b.Prop
            }

            ;
            var t = x[0].Prop;
            if (Test.Called == "DerivedGetPBaseGetPDerivedGetP")
                success++;
            //member initializer of anon type
            var a1 = new
            {
                d = d.Prop,
                b = b.Prop
            }

            ;
            test++;
            Test.Called = string.Empty;
            var r1 = a1.d.Prop;
            if (Test.Called == "DerivedGetP")
                success++;
            return test == success ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.returnType.covariant.usage004.usage004
{
    // <Title>Covariant return type for dynamic/object</Title>
    // <Description> is/as
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(57,6\).*CS1981</Expects>
    //<Expects Status=warning>\(74,6\).*CS1981</Expects>

    public class Base
    {
        public virtual object this[decimal d]
        {
            get
            {
                Test.Called += "BaseGetI";
                return new Base();
            }

            set
            {
                Test.Called += "BaseSetI";
            }
        }
    }

    public class Derived : Base
    {
        public override dynamic this[decimal d]
        {
            get
            {
                Test.Called += "DerivedGetI";
                return new Derived();
            }

            set
            {
                Test.Called += "DerivedSetI";
            }
        }
    }

    public class Test
    {
        public static string Called;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int test = 0, success = 0;
            Derived d = new Derived();
            //is
            test++;
            Test.Called = string.Empty;
            if (d[4] is dynamic & Test.Called == "DerivedGetI")
                success++;
            //is
            Base b = new Derived();
            test++;
            Test.Called = string.Empty;
            if (b[4] is object & Test.Called == "DerivedGetI")
                success++;
            //as
            test++;
            b = new Base();
            Test.Called = string.Empty;
            var x = (b[4] as dynamic)[0];
            if (x is dynamic & Test.Called == "BaseGetIBaseGetI")
                success++;
            return test == success ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.returnType.covariant.usage005.usage005
{
    // <Title>Covariant return type for dynamic/object</Title>
    // <Description> is/as
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public static class Ext
    {
        public static void Foo(this object o)
        {
            Test.Called = "Ext";
        }
    }

    public class Base
    {
        public virtual object this[decimal d]
        {
            get
            {
                Test.Called += "BaseGetI";
                return new Base();
            }

            set
            {
                Test.Called += "BaseSetI";
            }
        }
    }

    public class Derived : Base
    {
        public override dynamic this[decimal d]
        {
            get
            {
                Test.Called += "DerivedGetI";
                return new Derived();
            }

            set
            {
                Test.Called += "DerivedSetI";
            }
        }
    }

    public class Test
    {
        public static string Called;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int test = 0, success = 0;
            Base b = new Derived();
            //ext method on object
            test++;
            Test.Called = string.Empty;
            b[4].Foo();
            if (Test.Called == "Ext")
                success++;
            //ext method on dynamic
            test++;
            Derived d = new Derived();
            try
            {
                d[4].Foo();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                success++;
            }

            return test == success ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.returnType.covariant.usage006.usage006
{
    // <Title>Covariant return type for dynamic/object</Title>
    // <Description> is/as
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public abstract class Base
    {
        public abstract object Foo();
    }

    public class Derived : Base
    {
        public override dynamic Foo()
        {
            Test.Called += "DerivedM";
            return new Derived();
        }

        public static implicit operator int (Derived d)
        {
            return 10;
        }
    }

    public class Test
    {
        public static string Called;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int test = 0, success = 0;
            Derived d = new Derived();
            //Conversion is applied
            test++;
            Test.Called = string.Empty;
            int x = d.Foo();
            if (x == 10)
                success++;
            return test == success ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.returnType.covariant.errorverifier.errorverifier
{
    public enum ErrorElementId
    {
        None,
        SK_METHOD, // method
        SK_CLASS, // type
        SK_NAMESPACE, // namespace
        SK_FIELD, // field
        SK_PROPERTY, // property
        SK_UNKNOWN, // element
        SK_VARIABLE, // variable
        SK_EVENT, // event
        SK_TYVAR, // type parameter
        SK_ALIAS, // using alias
        ERRORSYM, // <error>
        NULL, // <null>
        GlobalNamespace, // <global namespace>
        MethodGroup, // method group
        AnonMethod, // anonymous method
        Lambda, // lambda expression
        AnonymousType, // anonymous type
    }

    public enum ErrorMessageId
    {
        None,
        BadBinaryOps, // Operator '{0}' cannot be applied to operands of type '{1}' and '{2}'
        IntDivByZero, // Division by constant zero
        BadIndexLHS, // Cannot apply indexing with [] to an expression of type '{0}'
        BadIndexCount, // Wrong number of indices inside []; expected '{0}'
        BadUnaryOp, // Operator '{0}' cannot be applied to operand of type '{1}'
        NoImplicitConv, // Cannot implicitly convert type '{0}' to '{1}'
        NoExplicitConv, // Cannot convert type '{0}' to '{1}'
        ConstOutOfRange, // Constant value '{0}' cannot be converted to a '{1}'
        AmbigBinaryOps, // Operator '{0}' is ambiguous on operands of type '{1}' and '{2}'
        AmbigUnaryOp, // Operator '{0}' is ambiguous on an operand of type '{1}'
        ValueCantBeNull, // Cannot convert null to '{0}' because it is a non-nullable value type
        WrongNestedThis, // Cannot access a non-static member of outer type '{0}' via nested type '{1}'
        NoSuchMember, // '{0}' does not contain a definition for '{1}'
        ObjectRequired, // An object reference is required for the non-static field, method, or property '{0}'
        AmbigCall, // The call is ambiguous between the following methods or properties: '{0}' and '{1}'
        BadAccess, // '{0}' is inaccessible due to its protection level
        MethDelegateMismatch, // No overload for '{0}' matches delegate '{1}'
        AssgLvalueExpected, // The left-hand side of an assignment must be a variable, property or indexer
        NoConstructors, // The type '{0}' has no constructors defined
        BadDelegateConstructor, // The delegate '{0}' does not have a valid constructor
        PropertyLacksGet, // The property or indexer '{0}' cannot be used in this context because it lacks the get accessor
        ObjectProhibited, // Member '{0}' cannot be accessed with an instance reference; qualify it with a type name instead
        AssgReadonly, // A readonly field cannot be assigned to (except in a constructor or a variable initializer)
        RefReadonly, // A readonly field cannot be passed ref or out (except in a constructor)
        AssgReadonlyStatic, // A static readonly field cannot be assigned to (except in a static constructor or a variable initializer)
        RefReadonlyStatic, // A static readonly field cannot be passed ref or out (except in a static constructor)
        AssgReadonlyProp, // Property or indexer '{0}' cannot be assigned to -- it is read only
        AbstractBaseCall, // Cannot call an abstract base member: '{0}'
        RefProperty, // A property or indexer may not be passed as an out or ref parameter
        ManagedAddr, // Cannot take the address of, get the size of, or declare a pointer to a managed type ('{0}')
        FixedNotNeeded, // You cannot use the fixed statement to take the address of an already fixed expression
        UnsafeNeeded, // Dynamic calls cannot be used in conjunction with pointers
        BadBoolOp, // In order to be applicable as a short circuit operator a user-defined logical operator ('{0}') must have the same return type as the type of its 2 parameters
        MustHaveOpTF, // The type ('{0}') must contain declarations of operator true and operator false
        CheckedOverflow, // The operation overflows at compile time in checked mode
        ConstOutOfRangeChecked, // Constant value '{0}' cannot be converted to a '{1}' (use 'unchecked' syntax to override)
        AmbigMember, // Ambiguity between '{0}' and '{1}'
        SizeofUnsafe, // '{0}' does not have a predefined size, therefore sizeof can only be used in an unsafe context (consider using System.Runtime.InteropServices.Marshal.SizeOf)
        FieldInitRefNonstatic, // A field initializer cannot reference the non-static field, method, or property '{0}'
        CallingFinalizeDeprecated, // Destructors and object.Finalize cannot be called directly. Consider calling IDisposable.Dispose if available.
        CallingBaseFinalizeDeprecated, // Do not directly call your base class Finalize method. It is called automatically from your destructor.
        BadCastInFixed, // The right hand side of a fixed statement assignment may not be a cast expression
        NoImplicitConvCast, // Cannot implicitly convert type '{0}' to '{1}'. An explicit conversion exists (are you missing a cast?)
        InaccessibleGetter, // The property or indexer '{0}' cannot be used in this context because the get accessor is inaccessible
        InaccessibleSetter, // The property or indexer '{0}' cannot be used in this context because the set accessor is inaccessible
        BadArity, // Using the generic {1} '{0}' requires '{2}' type arguments
        BadTypeArgument, // The type '{0}' may not be used as a type argument
        TypeArgsNotAllowed, // The {1} '{0}' cannot be used with type arguments
        HasNoTypeVars, // The non-generic {1} '{0}' cannot be used with type arguments
        NewConstraintNotSatisfied, // '{2}' must be a non-abstract type with a public parameterless constructor in order to use it as parameter '{1}' in the generic type or method '{0}'
        GenericConstraintNotSatisfiedRefType, // The type '{3}' cannot be used as type parameter '{2}' in the generic type or method '{0}'. There is no implicit reference conversion from '{3}' to '{1}'.
        GenericConstraintNotSatisfiedNullableEnum, // The type '{3}' cannot be used as type parameter '{2}' in the generic type or method '{0}'. The nullable type '{3}' does not satisfy the constraint of '{1}'.
        GenericConstraintNotSatisfiedNullableInterface, // The type '{3}' cannot be used as type parameter '{2}' in the generic type or method '{0}'. The nullable type '{3}' does not satisfy the constraint of '{1}'. Nullable types can not satisfy any interface constraints.
        GenericConstraintNotSatisfiedTyVar, // The type '{3}' cannot be used as type parameter '{2}' in the generic type or method '{0}'. There is no boxing conversion or type parameter conversion from '{3}' to '{1}'.
        GenericConstraintNotSatisfiedValType, // The type '{3}' cannot be used as type parameter '{2}' in the generic type or method '{0}'. There is no boxing conversion from '{3}' to '{1}'.
        TypeVarCantBeNull, // Cannot convert null to type parameter '{0}' because it could be a non-nullable value type. Consider using 'default({0})' instead.
        BadRetType, // '{1} {0}' has the wrong return type
        CantInferMethTypeArgs, // The type arguments for method '{0}' cannot be inferred from the usage. Try specifying the type arguments explicitly.
        MethGrpToNonDel, // Cannot convert method group '{0}' to non-delegate type '{1}'. Did you intend to invoke the method?
        RefConstraintNotSatisfied, // The type '{2}' must be a reference type in order to use it as parameter '{1}' in the generic type or method '{0}'
        ValConstraintNotSatisfied, // The type '{2}' must be a non-nullable value type in order to use it as parameter '{1}' in the generic type or method '{0}'
        CircularConstraint, // Circular constraint dependency involving '{0}' and '{1}'
        BaseConstraintConflict, // Type parameter '{0}' inherits conflicting constraints '{1}' and '{2}'
        ConWithValCon, // Type parameter '{1}' has the 'struct' constraint so '{1}' cannot be used as a constraint for '{0}'
        AmbigUDConv, // Ambiguous user defined conversions '{0}' and '{1}' when converting from '{2}' to '{3}'
        PredefinedTypeNotFound, // Predefined type '{0}' is not defined or imported
        PredefinedTypeBadType, // Predefined type '{0}' is declared incorrectly
        BindToBogus, // '{0}' is not supported by the language
        CantCallSpecialMethod, // '{0}': cannot explicitly call operator or accessor
        BogusType, // '{0}' is a type not supported by the language
        MissingPredefinedMember, // Missing compiler required member '{0}.{1}'
        LiteralDoubleCast, // Literal of type double cannot be implicitly converted to type '{1}'; use an '{0}' suffix to create a literal of this type
        UnifyingInterfaceInstantiations, // '{0}' cannot implement both '{1}' and '{2}' because they may unify for some type parameter substitutions
        ConvertToStaticClass, // Cannot convert to static type '{0}'
        GenericArgIsStaticClass, // '{0}': static types cannot be used as type arguments
        PartialMethodToDelegate, // Cannot create delegate from method '{0}' because it is a partial method without an implementing declaration
        IncrementLvalueExpected, // The operand of an increment or decrement operator must be a variable, property or indexer
        NoSuchMemberOrExtension, // '{0}' does not contain a definition for '{1}' and no extension method '{1}' accepting a first argument of type '{0}' could be found (are you missing a using directive or an assembly reference?)
        ValueTypeExtDelegate, // Extension methods '{0}' defined on value type '{1}' cannot be used to create delegates
        BadArgCount, // No overload for method '{0}' takes '{1}' arguments
        BadArgTypes, // The best overloaded method match for '{0}' has some invalid arguments
        BadArgType, // Argument '{0}': cannot convert from '{1}' to '{2}'
        RefLvalueExpected, // A ref or out argument must be an assignable variable
        BadProtectedAccess, // Cannot access protected member '{0}' via a qualifier of type '{1}'; the qualifier must be of type '{2}' (or derived from it)
        BindToBogusProp2, // Property, indexer, or event '{0}' is not supported by the language; try directly calling accessor methods '{1}' or '{2}'
        BindToBogusProp1, // Property, indexer, or event '{0}' is not supported by the language; try directly calling accessor method '{1}'
        BadDelArgCount, // Delegate '{0}' does not take '{1}' arguments
        BadDelArgTypes, // Delegate '{0}' has some invalid arguments
        AssgReadonlyLocal, // Cannot assign to '{0}' because it is read-only
        RefReadonlyLocal, // Cannot pass '{0}' as a ref or out argument because it is read-only
        ReturnNotLValue, // Cannot modify the return value of '{0}' because it is not a variable
        BadArgExtraRef, // Argument '{0}' should not be passed with the '{1}' keyword
        // DelegateOnConditional, // Cannot create delegate with '{0}' because it has a Conditional attribute (REMOVED)
        BadArgRef, // Argument '{0}' must be passed with the '{1}' keyword
        AssgReadonly2, // Members of readonly field '{0}' cannot be modified (except in a constructor or a variable initializer)
        RefReadonly2, // Members of readonly field '{0}' cannot be passed ref or out (except in a constructor)
        AssgReadonlyStatic2, // Fields of static readonly field '{0}' cannot be assigned to (except in a static constructor or a variable initializer)
        RefReadonlyStatic2, // Fields of static readonly field '{0}' cannot be passed ref or out (except in a static constructor)
        AssgReadonlyLocalCause, // Cannot assign to '{0}' because it is a '{1}'
        RefReadonlyLocalCause, // Cannot pass '{0}' as a ref or out argument because it is a '{1}'
        ThisStructNotInAnonMeth, // Anonymous methods, lambda expressions, and query expressions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression or query expression and using the local instead.
        DelegateOnNullable, // Cannot bind delegate to '{0}' because it is a member of 'System.Nullable<T>'
        BadCtorArgCount, // '{0}' does not contain a constructor that takes '{1}' arguments
        BadExtensionArgTypes, // '{0}' does not contain a definition for '{1}' and the best extension method overload '{2}' has some invalid arguments
        BadInstanceArgType, // Instance argument: cannot convert from '{0}' to '{1}'
        BadArgTypesForCollectionAdd, // The best overloaded Add method '{0}' for the collection initializer has some invalid arguments
        InitializerAddHasParamModifiers, // The best overloaded method match '{0}' for the collection initializer element cannot be used. Collection initializer 'Add' methods cannot have ref or out parameters.
        NonInvocableMemberCalled, // Non-invocable member '{0}' cannot be used like a method.
        NamedArgumentSpecificationBeforeFixedArgument, // Named argument specifications must appear after all fixed arguments have been specified
        BadNamedArgument, // The best overload for '{0}' does not have a parameter named '{1}'
        BadNamedArgumentForDelegateInvoke, // The delegate '{0}' does not have a parameter named '{1}'
        DuplicateNamedArgument, // Named argument '{0}' cannot be specified multiple times
        NamedArgumentUsedInPositional, // Named argument '{0}' specifies a parameter for which a positional argument has already been given
    }

    public enum RuntimeErrorId
    {
        None,
        // RuntimeBinderInternalCompilerException
        InternalCompilerError, // An unexpected exception occurred while binding a dynamic operation
        // ArgumentException
        BindRequireArguments, // Cannot bind call with no calling object
        // RuntimeBinderException
        BindCallFailedOverloadResolution, // Overload resolution failed
        // ArgumentException
        BindBinaryOperatorRequireTwoArguments, // Binary operators must be invoked with two arguments
        // ArgumentException
        BindUnaryOperatorRequireOneArgument, // Unary operators must be invoked with one argument
        // RuntimeBinderException
        BindPropertyFailedMethodGroup, // The name '{0}' is bound to a method and cannot be used like a property
        // RuntimeBinderException
        BindPropertyFailedEvent, // The event '{0}' can only appear on the left hand side of += or -=
        // RuntimeBinderException
        BindInvokeFailedNonDelegate, // Cannot invoke a non-delegate type
        // ArgumentException
        BindImplicitConversionRequireOneArgument, // Implicit conversion takes exactly one argument
        // ArgumentException
        BindExplicitConversionRequireOneArgument, // Explicit conversion takes exactly one argument
        // ArgumentException
        BindBinaryAssignmentRequireTwoArguments, // Binary operators cannot be invoked with one argument
        // RuntimeBinderException
        BindBinaryAssignmentFailedNullReference, // Cannot perform member assignment on a null reference
        // RuntimeBinderException
        NullReferenceOnMemberException, // Cannot perform runtime binding on a null reference
        // RuntimeBinderException
        BindCallToConditionalMethod, // Cannot dynamically invoke method '{0}' because it has a Conditional attribute
        // RuntimeBinderException
        BindToVoidMethodButExpectResult, // Cannot implicitly convert type 'void' to 'object'
        // EE?
        EmptyDynamicView, // No further information on this object could be discovered
        // MissingMemberException
        GetValueonWriteOnlyProperty, // Write Only properties are not supported
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.returnType.covariant.usage007.usage007
{
    // <Title>Covariant return type for dynamic/object</Title>
    // <Description> return type and compound operators
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Base
    {
        protected int storage = 4;
        public virtual object M()
        {
            return 1;
        }

        public virtual object P
        {
            get
            {
                return storage * 2;
            }

            set
            {
                storage = (int)value * 2;
            }
        }

        public virtual object P2
        {
            get;
            set;
        }

        public virtual object this[int x]
        {
            get
            {
                return null;
            }

            set
            {
            }
        }
    }

    public class Derived : Base
    {
        public override dynamic M()
        {
            return 2;
        }

        public override dynamic P
        {
            get
            {
                return storage;
            }

            set
            {
                storage = (int)value;
            }
        }

        private dynamic _P2_Storage;
        public override dynamic P2
        {
            get
            {
                return _P2_Storage;
            }

            set
            {
                _P2_Storage = value;
            }
        }

        public override dynamic this[int x]
        {
            get
            {
                return x;
            }

            set
            {
            }
        }

        //Some operators
        public static dynamic operator +(Derived d, int x)
        {
            return new Derived()
            {
                P = d.P + x
            }

            ;
        }

        public static Derived operator ++(Derived d)
        {
            return new Derived()
            {
                P = d.P + 1
            }

            ;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Derived d = new Derived();
            try
            {
                d.P2++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.returnType.covariant.usage008.usage008
{
    // <Title>Covariant return type for dynamic/object</Title>
    // <Description> return type and compound operators
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Base
    {
        protected int storage = 4;
        public virtual object M()
        {
            return 1;
        }

        public virtual object P
        {
            get
            {
                return storage * 2;
            }

            set
            {
                storage = (int)value * 2;
            }
        }

        public virtual object P2
        {
            get;
            set;
        }

        public virtual object this[int x]
        {
            get
            {
                return null;
            }

            set
            {
            }
        }

        public static object operator &(Base b, object o)
        {
            return "base";
        }
    }

    public class Derived : Base
    {
        public override dynamic M()
        {
            return 2;
        }

        public override dynamic P
        {
            get
            {
                return storage;
            }

            set
            {
                storage = (int)value;
            }
        }

        private dynamic _P2_Storage;
        public override dynamic P2
        {
            get
            {
                return _P2_Storage;
            }

            set
            {
                _P2_Storage = value;
            }
        }

        public override dynamic this[int x]
        {
            get
            {
                return x;
            }

            set
            {
            }
        }

        //Some operators
        public static dynamic operator +(Derived d, int x)
        {
            return new Derived()
            {
                P = d.P + x
            }

            ;
        }

        public static Derived operator ++(Derived d)
        {
            return new Derived()
            {
                P = d.P + 1
            }

            ;
        }

        public static bool operator ==(Derived d, dynamic x)
        {
            return true;
        }

        public static bool operator !=(Derived d, dynamic x)
        {
            return true;
        }

        public static bool operator true(Derived d)
        {
            return true;
        }

        public static bool operator false(Derived d)
        {
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object o)
        {
            return base.Equals(o);
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int test = 0, success = 0;
            Derived d = new Derived();
            //---- increments the result of a method call
            test++;
            var x = d.M();
            x++;
            if (x == 3)
                success++;
            test++;
            d.P++;
            if (d.P == 5)
                success++;
            test++;
            ++d.P;
            if (d.P == 6)
                success++;
            //---- uses the += on a property returning dynamic in a checked context
            test++;
            try
            {
                d.P = int.MaxValue;
                checked
                {
                    d.P += 2;
                }
            }
            catch (System.OverflowException)
            {
                success++;
            }

            //---- uses the *= on a property returning dynamic
            test++;
            d.P = 3;
            d.P *= 3;
            if (d.P == 9)
                success++;
            //---- uses the & operator defined on the base class
            test++;
            var t = d & d;
            if ((string)t == "base")
                success++;
            //---- multiple operators
            test++;
            var y = d[0] + 5 * d[5];
            if (y == 25)
                success++;
            //---- the == operator
            test++;
            if (d == 3)
                success++;
            //---- the != operator
            test++;
            if (d != 4)
                success++;
            //---- the true operator
            test++;
            if (d)
                success++;
            return test == success ? 0 : 1;
        }
    }
    // </Code>
}
