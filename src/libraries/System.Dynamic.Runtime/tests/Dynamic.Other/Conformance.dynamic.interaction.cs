// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common
{
    // <Title> Dynamic and static interaction utility class </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class Verify
    {
        [ThreadStatic]
        internal static bool FirstCalled = false;

        internal static int Eval(Func<bool> testmethod)
        {
            int result = 0;
            try
            {
                if (!testmethod())
                {
                    result++;
                    //System.Console.WriteLine("Test failed at {0}\n", testmethod.Method.Name);
                }
            }
            catch (Exception e)
            {
                result++;
                //System.Console.WriteLine("Catch an unknown exception when run test {0}, \nexception: {1}", testmethod.Method.Name, e.ToString());
            }

            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commondefretdym.commondefretdym
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    // <Title> Dynamic and static interaction utility class </Title>
    // <Description>
    // Defined types which return dynamic used in test.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    #region FirstClassWithSecondClass
    public class FirstClassReturnDynamicClass
    {
        private static SecondClass s_secClass = new SecondClass();
        public dynamic InstanceMethod()
        {
            Verify.FirstCalled = true;
            return s_secClass;
        }

        public static dynamic StaticMethod()
        {
            Verify.FirstCalled = true;
            return s_secClass;
        }

        public dynamic InstanceProperty
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secClass;
            }
        }

        public static dynamic StaticProperty
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secClass;
            }
        }

        public static dynamic operator +(FirstClassReturnDynamicClass s, int i)
        {
            Verify.FirstCalled = true;
            return s_secClass;
        }

        public dynamic this[int i]
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secClass;
            }
        }
    }

    public class SecondClass
    {
        public bool InstanceMethod()
        {
            return true;
        }

        public bool InstanceProperty
        {
            get
            {
                return true;
            }
        }

        public static bool operator +(SecondClass s, int i)
        {
            return true;
        }

        public bool this[int i]
        {
            get
            {
                return true;
            }
        }
    }

    #endregion
    #region FirstClassWithSecondStruct
    public class FirstClassReturnDynamicStruct
    {
        private static SecondStruct s_secStruct = new SecondStruct();
        public dynamic InstanceMethod()
        {
            Verify.FirstCalled = true;
            return s_secStruct;
        }

        public static dynamic StaticMethod()
        {
            Verify.FirstCalled = true;
            return s_secStruct;
        }

        public dynamic InstanceProperty
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secStruct;
            }
        }

        public static dynamic StaticProperty
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secStruct;
            }
        }

        public static dynamic operator +(FirstClassReturnDynamicStruct s, int i)
        {
            Verify.FirstCalled = true;
            return s_secStruct;
        }

        public dynamic this[int i]
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secStruct;
            }
        }
    }

    public struct SecondStruct
    {
        public bool InstanceMethod()
        {
            return true;
        }

        public bool InstanceProperty
        {
            get
            {
                return true;
            }
        }

        public static bool operator +(SecondStruct s, int i)
        {
            return true;
        }

        public bool this[int i]
        {
            get
            {
                return true;
            }
        }
    }

    #endregion
    #region FirstClassWithSecondInterface
    public class FirstClassReturnDynamicInterface
    {
        private static SecondInterface s_secInterface = new SecondInterfaceImp();
        public dynamic InstanceMethod()
        {
            Verify.FirstCalled = true;
            return s_secInterface;
        }

        public static dynamic StaticMethod()
        {
            Verify.FirstCalled = true;
            return s_secInterface;
        }

        public dynamic InstanceProperty
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secInterface;
            }
        }

        public static dynamic StaticProperty
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secInterface;
            }
        }

        public static dynamic operator +(FirstClassReturnDynamicInterface s, int i)
        {
            Verify.FirstCalled = true;
            return s_secInterface;
        }

        public dynamic this[int i]
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secInterface;
            }
        }
    }

    public interface SecondInterface
    {
        bool InstanceMethod();
        bool InstanceProperty
        {
            get;
        }

        bool this[int i]
        {
            get;
        }
    }

    public class SecondInterfaceImp : SecondInterface
    {
        public bool InstanceMethod()
        {
            return true;
        }

        public bool InstanceProperty
        {
            get
            {
                return true;
            }
        }

        public bool this[int i]
        {
            get
            {
                return true;
            }
        }
    }
    #endregion

    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.statictodynamic001.statictodynamic001
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commondefretdym.commondefretdym;
    // <Title> Static context switch to dynamic context </Title>
    // <Description>
    // First is instance method of class and return dynamic
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class StaticToDynamicOfFirstClassWithInstanceMethod
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicClass();
            if ((!(bool)(s.InstanceMethod().InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicClass();
            if ((!(bool)(s.InstanceMethod().InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondClassOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicClass();

if ( (!(bool)(s.InstanceMethod() + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondClassIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicClass();

if ( (!(bool)(s.InstanceMethod()[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicStruct();
            if ((!(bool)(s.InstanceMethod().InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicStruct();
            if ((!(bool)(s.InstanceMethod().InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondStructOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicStruct();

if ( (!(bool)(s.InstanceMethod() + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondStructIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicStruct();

if ( (!(bool)(s.InstanceMethod()[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicInterface();
            if ((!(bool)(s.InstanceMethod().InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicInterface();
            if ((!(bool)(s.InstanceMethod().InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondInterfaceIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicInterface();

if ( (!(bool)(s.InstanceMethod()[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
#if M3
result += Verify.Eval(CallSecondClassOperator);
result += Verify.Eval(CallSecondClassIndexer);
 #endif
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
#if M3
result += Verify.Eval(CallSecondStructOperator);
result += Verify.Eval(CallSecondStructIndexer);
 #endif
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
#if M3
result += Verify.Eval(CallSecondInterfaceIndexer);
 #endif
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.statictodynamic002.statictodynamic002
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commondefretdym.commondefretdym;
    // <Title> Static context switch to dynamic context </Title>
    // <Description>
    // First is instance property of class and return dynamic
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class StaticToDynamicOfFirstClassWithInstanceProperty
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicClass();
            if ((!(bool)(s.InstanceProperty.InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicClass();
            if ((!(bool)(s.InstanceProperty.InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondClassOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicClass();

if ( (!(bool)(s.InstanceProperty + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondClassIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicClass();

if ( (!(bool)(s.InstanceProperty[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicStruct();
            if ((!(bool)(s.InstanceProperty.InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicStruct();
            if ((!(bool)(s.InstanceProperty.InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondStructOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicStruct();

if ( (!(bool)(s.InstanceProperty + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondStructIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicStruct();

if ( (!(bool)(s.InstanceProperty[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicInterface();
            if ((!(bool)(s.InstanceProperty.InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicInterface();
            if ((!(bool)(s.InstanceProperty.InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondInterfaceIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicInterface();

if ( (!(bool)(s.InstanceProperty[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
#if M3
result += Verify.Eval(CallSecondClassOperator);
result += Verify.Eval(CallSecondClassIndexer);
 #endif
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
#if M3
result += Verify.Eval(CallSecondStructOperator);
result += Verify.Eval(CallSecondStructIndexer);
 #endif
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
#if M3
result += Verify.Eval(CallSecondInterfaceIndexer);
 #endif
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.statictodynamic003.statictodynamic003
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commondefretdym.commondefretdym;
    // <Title> Static context switch to dynamic context </Title>
    // <Description>
    // First is operator of class and return dynamic
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class StaticToDynamicOfFirstClassWithOperator
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicClass();
            if ((!(bool)((s + 1).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicClass();
            if ((!(bool)((s + 1).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondClassOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicClass();

if ( (!(bool)((s + 1) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondClassIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicClass();

if ( (!(bool)((s + 1)[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicStruct();
            if ((!(bool)((s + 1).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicStruct();
            if ((!(bool)((s + 1).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondStructOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicStruct();

if ( (!(bool)((s + 1) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondStructIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicStruct();

if ( (!(bool)((s + 1)[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicInterface();
            if ((!(bool)((s + 1).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicInterface();
            if ((!(bool)((s + 1).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondInterfaceIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicInterface();

if ( (!(bool)((s + 1)[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
#if M3
result += Verify.Eval(CallSecondClassOperator);
result += Verify.Eval(CallSecondClassIndexer);
 #endif
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
#if M3
result += Verify.Eval(CallSecondStructOperator);
result += Verify.Eval(CallSecondStructIndexer);
 #endif
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
#if M3
result += Verify.Eval(CallSecondInterfaceIndexer);
 #endif
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.statictodynamic004.statictodynamic004
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commondefretdym.commondefretdym;
    // <Title> Static context switch to dynamic context </Title>
    // <Description>
    // First is indexer of class and return dynamic
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class StaticToDynamicOfFirstClassWithIndexer
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicClass();
            if ((!(bool)(s[1].InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicClass();
            if ((!(bool)(s[1].InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondClassOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicClass();

if ( (!(bool)(s[1] + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondClassIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicClass();

if ( (!(bool)(s[1][2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicStruct();
            if ((!(bool)(s[1].InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicStruct();
            if ((!(bool)(s[1].InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondStructOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicStruct();

if ( (!(bool)(s[1] + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondStructIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicStruct();

if ( (!(bool)(s[1][2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicInterface();
            if ((!(bool)(s[1].InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicInterface();
            if ((!(bool)(s[1].InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondInterfaceIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicInterface();

if ( (!(bool)(s[1][2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
#if M3
result += Verify.Eval(CallSecondClassOperator);
result += Verify.Eval(CallSecondClassIndexer);
 #endif
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
#if M3
result += Verify.Eval(CallSecondStructOperator);
result += Verify.Eval(CallSecondStructIndexer);
 #endif
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
#if M3
result += Verify.Eval(CallSecondInterfaceIndexer);
 #endif
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commongendefretdym.commongendefretdym
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    // <Title> Dynamic and static interaction utility class </Title>
    // <Description>
    // Defined generic types which return dynamic used in test.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    #region FirstClassWithSecondClass
    public class FirstClassReturnDynamicClass<T>
    {
        private static SecondClass<T> s_secClass = new SecondClass<T>();
        public dynamic InstanceMethod()
        {
            Verify.FirstCalled = true;
            return s_secClass;
        }

        public static dynamic StaticMethod()
        {
            Verify.FirstCalled = true;
            return s_secClass;
        }

        public dynamic InstanceProperty
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secClass;
            }
        }

        public static dynamic StaticProperty
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secClass;
            }
        }

        public static dynamic operator +(FirstClassReturnDynamicClass<T> s, int i)
        {
            Verify.FirstCalled = true;
            return s_secClass;
        }

        public dynamic this[int i]
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secClass;
            }
        }
    }

    public class SecondClass<T>
    {
        public bool InstanceMethod()
        {
            return true;
        }

        public bool InstanceProperty
        {
            get
            {
                return true;
            }
        }

        public static bool operator +(SecondClass<T> s, int i)
        {
            return true;
        }

        public bool this[int i]
        {
            get
            {
                return true;
            }
        }
    }

    #endregion
    #region FirstClassWithSecondStruct
    public class FirstClassReturnDynamicStruct<T>
    {
        private static SecondStruct<T> s_secStruct = new SecondStruct<T>();
        public dynamic InstanceMethod()
        {
            Verify.FirstCalled = true;
            return s_secStruct;
        }

        public static dynamic StaticMethod()
        {
            Verify.FirstCalled = true;
            return s_secStruct;
        }

        public dynamic InstanceProperty
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secStruct;
            }
        }

        public static dynamic StaticProperty
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secStruct;
            }
        }

        public static dynamic operator +(FirstClassReturnDynamicStruct<T> s, int i)
        {
            Verify.FirstCalled = true;
            return s_secStruct;
        }

        public dynamic this[int i]
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secStruct;
            }
        }
    }

    public struct SecondStruct<T>
    {
        public bool InstanceMethod()
        {
            return true;
        }

        public bool InstanceProperty
        {
            get
            {
                return true;
            }
        }

        public static bool operator +(SecondStruct<T> s, int i)
        {
            return true;
        }

        public bool this[int i]
        {
            get
            {
                return true;
            }
        }
    }

    #endregion
    #region FirstClassWithSecondInterface
    public class FirstClassReturnDynamicInterface<T>
    {
        private static SecondInterface<T> s_secInterface = new SecondInterfaceImp<T>();
        public dynamic InstanceMethod()
        {
            Verify.FirstCalled = true;
            return s_secInterface;
        }

        public static dynamic StaticMethod()
        {
            Verify.FirstCalled = true;
            return s_secInterface;
        }

        public dynamic InstanceProperty
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secInterface;
            }
        }

        public static dynamic StaticProperty
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secInterface;
            }
        }

        public static dynamic operator +(FirstClassReturnDynamicInterface<T> s, int i)
        {
            Verify.FirstCalled = true;
            return s_secInterface;
        }

        public dynamic this[int i]
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secInterface;
            }
        }
    }

    public interface SecondInterface<T>
    {
        bool InstanceMethod();
        bool InstanceProperty
        {
            get;
        }

        bool this[int i]
        {
            get;
        }
    }

    public class SecondInterfaceImp<T> : SecondInterface<T>
    {
        public bool InstanceMethod()
        {
            return true;
        }

        public bool InstanceProperty
        {
            get
            {
                return true;
            }
        }

        public bool this[int i]
        {
            get
            {
                return true;
            }
        }
    }
    #endregion

    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.statictodynamic005.statictodynamic005
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commongendefretdym.commongendefretdym;
    // <Title> Static context switch to dynamic context </Title>
    // <Description>
    // First is instance method of generic class and return dynamic
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class StaticToDynamicOfFirstGenericClassWithInstanceMethod
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicClass<int>();
            if ((!(bool)(s.InstanceMethod().InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicClass<int>();
            if ((!(bool)(s.InstanceMethod().InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondClassOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicClass<int>();

if ( (!(bool)(s.InstanceMethod() + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondClassIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicClass<int>();

if ( (!(bool)(s.InstanceMethod()[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicStruct<int>();
            if ((!(bool)(s.InstanceMethod().InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicStruct<int>();
            if ((!(bool)(s.InstanceMethod().InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondStructOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicStruct<int>();

if ( (!(bool)(s.InstanceMethod() + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondStructIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicStruct<int>();

if ( (!(bool)(s.InstanceMethod()[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicInterface<int>();
            if ((!(bool)(s.InstanceMethod().InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicInterface<int>();
            if ((!(bool)(s.InstanceMethod().InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondInterfaceIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicInterface<int>();

if ( (!(bool)(s.InstanceMethod()[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
#if M3
result += Verify.Eval(CallSecondClassOperator);
result += Verify.Eval(CallSecondClassIndexer);
 #endif
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
#if M3
result += Verify.Eval(CallSecondStructOperator);
result += Verify.Eval(CallSecondStructIndexer);
 #endif
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
#if M3
result += Verify.Eval(CallSecondInterfaceIndexer);
 #endif
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.statictodynamic006.statictodynamic006
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commongendefretdym.commongendefretdym;
    // <Title> Static context switch to dynamic context </Title>
    // <Description>
    // First is instance property of generic class and return dynamic
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class StaticToDynamicOfFirstGenericClassWithInstanceProperty
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicClass<int>();
            if ((!(bool)(s.InstanceProperty.InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicClass<int>();
            if ((!(bool)(s.InstanceProperty.InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondClassOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicClass<int>();

if ( (!(bool)(s.InstanceProperty + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondClassIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicClass<int>();

if ( (!(bool)(s.InstanceProperty[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicStruct<int>();
            if ((!(bool)(s.InstanceProperty.InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicStruct<int>();
            if ((!(bool)(s.InstanceProperty.InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondStructOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicStruct<int>();

if ( (!(bool)(s.InstanceProperty + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondStructIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicStruct<int>();

if ( (!(bool)(s.InstanceProperty[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicInterface<int>();
            if ((!(bool)(s.InstanceProperty.InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicInterface<int>();
            if ((!(bool)(s.InstanceProperty.InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondInterfaceIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicInterface<int>();

if ( (!(bool)(s.InstanceProperty[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
#if M3
result += Verify.Eval(CallSecondClassOperator);
result += Verify.Eval(CallSecondClassIndexer);
 #endif
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
#if M3
result += Verify.Eval(CallSecondStructOperator);
result += Verify.Eval(CallSecondStructIndexer);
 #endif
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
#if M3
result += Verify.Eval(CallSecondInterfaceIndexer);
 #endif
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.statictodynamic007.statictodynamic007
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commongendefretdym.commongendefretdym;
    // <Title> Static context switch to dynamic context </Title>
    // <Description>
    // First is operator of generic class and return dynamic
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class StaticToDynamicOfFirstGenericClassWithOperator
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicClass<int>();
            if ((!(bool)((s + 1).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicClass<int>();
            if ((!(bool)((s + 1).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondClassOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicClass<int>();

if ( (!(bool)((s + 1) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondClassIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicClass<int>();

if ( (!(bool)((s + 1)[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicStruct<int>();
            if ((!(bool)((s + 1).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicStruct<int>();
            if ((!(bool)((s + 1).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondStructOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicStruct<int>();

if ( (!(bool)((s + 1) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondStructIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicStruct<int>();

if ( (!(bool)((s + 1)[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicInterface<int>();
            if ((!(bool)((s + 1).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicInterface<int>();
            if ((!(bool)((s + 1).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondInterfaceIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicInterface<int>();

if ( (!(bool)((s + 1)[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
#if M3
result += Verify.Eval(CallSecondClassOperator);
result += Verify.Eval(CallSecondClassIndexer);
 #endif
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
#if M3
result += Verify.Eval(CallSecondStructOperator);
result += Verify.Eval(CallSecondStructIndexer);
 #endif
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
#if M3
result += Verify.Eval(CallSecondInterfaceIndexer);
 #endif
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.statictodynamic008.statictodynamic008
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commongendefretdym.commongendefretdym;
    // <Title> Static context switch to dynamic context </Title>
    // <Description>
    // First is indexer of generic class and return dynamic
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class StaticToDynamicOfFirstGenericClassWithIndexer
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicClass<int>();
            if ((!(bool)(s[1].InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicClass<int>();
            if ((!(bool)(s[1].InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondClassOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicClass<int>();

if ( (!(bool)(s[1] + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondClassIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicClass<int>();

if ( (!(bool)(s[1][2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicStruct<int>();
            if ((!(bool)(s[1].InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicStruct<int>();
            if ((!(bool)(s[1].InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondStructOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicStruct<int>();

if ( (!(bool)(s[1] + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondStructIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicStruct<int>();

if ( (!(bool)(s[1][2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicInterface<int>();
            if ((!(bool)(s[1].InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnDynamicInterface<int>();
            if ((!(bool)(s[1].InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondInterfaceIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnDynamicInterface<int>();

if ( (!(bool)(s[1][2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
#if M3
result += Verify.Eval(CallSecondClassOperator);
result += Verify.Eval(CallSecondClassIndexer);
 #endif
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
#if M3
result += Verify.Eval(CallSecondStructOperator);
result += Verify.Eval(CallSecondStructIndexer);
 #endif
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
#if M3
result += Verify.Eval(CallSecondInterfaceIndexer);
 #endif
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.statictodynamic009.statictodynamic009
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commondefretdym.commondefretdym;
    // <Title> Static context switch to dynamic context </Title>
    // <Description>
    // First is static method of class and return dynamic
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class StaticToDynamicOfFirstClassWithStaticMethod
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(FirstClassReturnDynamicClass.StaticMethod().InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(FirstClassReturnDynamicClass.StaticMethod().InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondClassOperator()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(FirstClassReturnDynamicClass.StaticMethod() + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondClassIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(FirstClassReturnDynamicClass.StaticMethod()[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(FirstClassReturnDynamicStruct.StaticMethod().InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(FirstClassReturnDynamicStruct.StaticMethod().InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondStructOperator()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(FirstClassReturnDynamicStruct.StaticMethod() + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondStructIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(FirstClassReturnDynamicStruct.StaticMethod()[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(FirstClassReturnDynamicInterface.StaticMethod().InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(FirstClassReturnDynamicInterface.StaticMethod().InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondInterfaceIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(FirstClassReturnDynamicInterface.StaticMethod()[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
#if M3
result += Verify.Eval(CallSecondClassOperator);
result += Verify.Eval(CallSecondClassIndexer);
 #endif
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
#if M3
result += Verify.Eval(CallSecondStructOperator);
result += Verify.Eval(CallSecondStructIndexer);
 #endif
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
#if M3
result += Verify.Eval(CallSecondInterfaceIndexer);
 #endif
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.statictodynamic010.statictodynamic010
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commondefretdym.commondefretdym;
    // <Title> Static context switch to dynamic context </Title>
    // <Description>
    // First is static property of class and return dynamic
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class StaticToDynamicOfFirstClassWithStaticProperty
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(FirstClassReturnDynamicClass.StaticProperty.InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(FirstClassReturnDynamicClass.StaticProperty.InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondClassOperator()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(FirstClassReturnDynamicClass.StaticProperty + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondClassIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(FirstClassReturnDynamicClass.StaticProperty[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(FirstClassReturnDynamicStruct.StaticProperty.InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(FirstClassReturnDynamicStruct.StaticProperty.InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondStructOperator()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(FirstClassReturnDynamicStruct.StaticProperty + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondStructIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(FirstClassReturnDynamicStruct.StaticProperty[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(FirstClassReturnDynamicInterface.StaticProperty.InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(FirstClassReturnDynamicInterface.StaticProperty.InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondInterfaceIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(FirstClassReturnDynamicInterface.StaticProperty[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
#if M3
result += Verify.Eval(CallSecondClassOperator);
result += Verify.Eval(CallSecondClassIndexer);
 #endif
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
#if M3
result += Verify.Eval(CallSecondStructOperator);
result += Verify.Eval(CallSecondStructIndexer);
 #endif
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
#if M3
result += Verify.Eval(CallSecondInterfaceIndexer);
 #endif
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.statictodynamic011.statictodynamic011
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commongendefretdym.commongendefretdym;
    // <Title> Static context switch to dynamic context </Title>
    // <Description>
    // First is static method of generic class and return dynamic
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class StaticToDynamicOfFirstGenericClassWithStaticMethod
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(FirstClassReturnDynamicClass<int>.StaticMethod().InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(FirstClassReturnDynamicClass<int>.StaticMethod().InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondClassOperator()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(FirstClassReturnDynamicClass<int>.StaticMethod() + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondClassIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(FirstClassReturnDynamicClass<int>.StaticMethod()[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(FirstClassReturnDynamicStruct<int>.StaticMethod().InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(FirstClassReturnDynamicStruct<int>.StaticMethod().InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondStructOperator()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(FirstClassReturnDynamicStruct<int>.StaticMethod() + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondStructIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(FirstClassReturnDynamicStruct<int>.StaticMethod()[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(FirstClassReturnDynamicInterface<int>.StaticMethod().InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(FirstClassReturnDynamicInterface<int>.StaticMethod().InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondInterfaceIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(FirstClassReturnDynamicInterface<int>.StaticMethod()[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
#if M3
result += Verify.Eval(CallSecondClassOperator);
result += Verify.Eval(CallSecondClassIndexer);
 #endif
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
#if M3
result += Verify.Eval(CallSecondStructOperator);
result += Verify.Eval(CallSecondStructIndexer);
 #endif
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
#if M3
result += Verify.Eval(CallSecondInterfaceIndexer);
 #endif
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.statictodynamic012.statictodynamic012
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commongendefretdym.commongendefretdym;
    // <Title> Static context switch to dynamic context </Title>
    // <Description>
    // First is static property of generic class and return dynamic
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class StaticToDynamicOfFirstGenericClassWithStaticProperty
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(FirstClassReturnDynamicClass<int>.StaticProperty.InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(FirstClassReturnDynamicClass<int>.StaticProperty.InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondClassOperator()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(FirstClassReturnDynamicClass<int>.StaticProperty + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondClassIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(FirstClassReturnDynamicClass<int>.StaticProperty[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(FirstClassReturnDynamicStruct<int>.StaticProperty.InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(FirstClassReturnDynamicStruct<int>.StaticProperty.InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondStructOperator()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(FirstClassReturnDynamicStruct<int>.StaticProperty + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondStructIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(FirstClassReturnDynamicStruct<int>.StaticProperty[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(FirstClassReturnDynamicInterface<int>.StaticProperty.InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(FirstClassReturnDynamicInterface<int>.StaticProperty.InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondInterfaceIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(FirstClassReturnDynamicInterface<int>.StaticProperty[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
#if M3
result += Verify.Eval(CallSecondClassOperator);
result += Verify.Eval(CallSecondClassIndexer);
 #endif
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
#if M3
result += Verify.Eval(CallSecondStructOperator);
result += Verify.Eval(CallSecondStructIndexer);
 #endif
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
#if M3
result += Verify.Eval(CallSecondInterfaceIndexer);
 #endif
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commondef.commondef
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    // <Title> Dynamic and static interaction utility class </Title>
    // <Description>
    // Defined types used in test.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    #region FirstClassWithSecondClass
    public class FirstClassReturnClass
    {
        private static SecondClass s_secClass = new SecondClass();
        public SecondClass InstanceMethod()
        {
            Verify.FirstCalled = true;
            return s_secClass;
        }

        public static SecondClass StaticMethod()
        {
            Verify.FirstCalled = true;
            return s_secClass;
        }

        public SecondClass InstanceProperty
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secClass;
            }
        }

        public static SecondClass StaticProperty
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secClass;
            }
        }

        public static SecondClass operator +(FirstClassReturnClass s, int i)
        {
            Verify.FirstCalled = true;
            return s_secClass;
        }

        public SecondClass this[int i]
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secClass;
            }
        }
    }

    public class SecondClass
    {
        public bool InstanceMethod()
        {
            return true;
        }

        public bool InstanceProperty
        {
            get
            {
                return true;
            }
        }

        public static bool operator +(SecondClass s, int i)
        {
            return true;
        }

        public bool this[int i]
        {
            get
            {
                return true;
            }
        }
    }

    #endregion
    #region FirstClassWithSecondStruct
    public class FirstClassReturnStruct
    {
        private static SecondStruct s_secStruct = new SecondStruct();
        public SecondStruct InstanceMethod()
        {
            Verify.FirstCalled = true;
            return s_secStruct;
        }

        public static SecondStruct StaticMethod()
        {
            Verify.FirstCalled = true;
            return s_secStruct;
        }

        public SecondStruct InstanceProperty
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secStruct;
            }
        }

        public static SecondStruct StaticProperty
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secStruct;
            }
        }

        public static SecondStruct operator +(FirstClassReturnStruct s, int i)
        {
            Verify.FirstCalled = true;
            return s_secStruct;
        }

        public SecondStruct this[int i]
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secStruct;
            }
        }
    }

    public struct SecondStruct
    {
        public bool InstanceMethod()
        {
            return true;
        }

        public bool InstanceProperty
        {
            get
            {
                return true;
            }
        }

        public static bool operator +(SecondStruct s, int i)
        {
            return true;
        }

        public bool this[int i]
        {
            get
            {
                return true;
            }
        }
    }

    #endregion
    #region FirstClassWithSecondInterface
    public class FirstClassReturnInterface
    {
        private static SecondInterface s_secInterface = new SecondInterfaceImp();
        public SecondInterface InstanceMethod()
        {
            Verify.FirstCalled = true;
            return s_secInterface;
        }

        public static SecondInterface StaticMethod()
        {
            Verify.FirstCalled = true;
            return s_secInterface;
        }

        public SecondInterface InstanceProperty
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secInterface;
            }
        }

        public static SecondInterface StaticProperty
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secInterface;
            }
        }

        public static SecondInterface operator +(FirstClassReturnInterface s, int i)
        {
            Verify.FirstCalled = true;
            return s_secInterface;
        }

        public SecondInterface this[int i]
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secInterface;
            }
        }
    }

    public interface SecondInterface
    {
        bool InstanceMethod();
        bool InstanceProperty
        {
            get;
        }

        bool this[int i]
        {
            get;
        }
    }

    public class SecondInterfaceImp : SecondInterface
    {
        public bool InstanceMethod()
        {
            return true;
        }

        public bool InstanceProperty
        {
            get
            {
                return true;
            }
        }

        public bool this[int i]
        {
            get
            {
                return true;
            }
        }
    }
    #endregion

    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.statictodynamic013.statictodynamic013
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commondef.commondef;
    // <Title> Static context switch to dynamic context </Title>
    // <Description>
    // First is instance method of class
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class StaticToDynamicOfFirstClassWithInstanceMethod
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnClass();
            if ((!(bool)(((dynamic)s.InstanceMethod()).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnClass();
            if ((!(bool)(((dynamic)s.InstanceMethod()).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondClassOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnClass();

if ( (!(bool)(((dynamic)s.InstanceMethod()) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondClassIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnClass();

if ( (!(bool)(((dynamic)s.InstanceMethod())[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnStruct();
            if ((!(bool)(((dynamic)s.InstanceMethod()).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnStruct();
            if ((!(bool)(((dynamic)s.InstanceMethod()).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondStructOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnStruct();

if ( (!(bool)(((dynamic)s.InstanceMethod()) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondStructIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnStruct();

if ( (!(bool)(((dynamic)s.InstanceMethod())[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnInterface();
            if ((!(bool)(((dynamic)s.InstanceMethod()).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnInterface();
            if ((!(bool)(((dynamic)s.InstanceMethod()).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondInterfaceIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnInterface();

if ( (!(bool)(((dynamic)s.InstanceMethod())[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
#if M3
result += Verify.Eval(CallSecondClassOperator);
result += Verify.Eval(CallSecondClassIndexer);
 #endif
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
#if M3
result += Verify.Eval(CallSecondStructOperator);
result += Verify.Eval(CallSecondStructIndexer);
 #endif
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
#if M3
result += Verify.Eval(CallSecondInterfaceIndexer);
 #endif
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.statictodynamic014.statictodynamic014
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commondef.commondef;
    // <Title> Static context switch to dynamic context </Title>
    // <Description>
    // First is instance property of class
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class StaticToDynamicOfFirstClassWithInstanceProperty
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnClass();
            if ((!(bool)(((dynamic)s.InstanceProperty).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnClass();
            if ((!(bool)(((dynamic)s.InstanceProperty).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondClassOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnClass();

if ( (!(bool)(((dynamic)s.InstanceProperty) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondClassIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnClass();

if ( (!(bool)(((dynamic)s.InstanceProperty)[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnStruct();
            if ((!(bool)(((dynamic)s.InstanceProperty).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnStruct();
            if ((!(bool)(((dynamic)s.InstanceProperty).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondStructOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnStruct();

if ( (!(bool)(((dynamic)s.InstanceProperty) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondStructIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnStruct();

if ( (!(bool)(((dynamic)s.InstanceProperty)[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnInterface();
            if ((!(bool)(((dynamic)s.InstanceProperty).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnInterface();
            if ((!(bool)(((dynamic)s.InstanceProperty).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondInterfaceIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnInterface();

if ( (!(bool)(((dynamic)s.InstanceProperty)[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
#if M3
result += Verify.Eval(CallSecondClassOperator);
result += Verify.Eval(CallSecondClassIndexer);
 #endif
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
#if M3
result += Verify.Eval(CallSecondStructOperator);
result += Verify.Eval(CallSecondStructIndexer);
 #endif
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
#if M3
result += Verify.Eval(CallSecondInterfaceIndexer);
 #endif
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.statictodynamic015.statictodynamic015
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commondef.commondef;
    // <Title> Static context switch to dynamic context </Title>
    // <Description>
    // First is operator of class
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class StaticToDynamicOfFirstClassWithOperator
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnClass();
            if ((!(bool)(((dynamic)(s + 1)).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnClass();
            if ((!(bool)(((dynamic)(s + 1)).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondClassOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnClass();

if ( (!(bool)(((dynamic)(s + 1)) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondClassIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnClass();

if ( (!(bool)(((dynamic)(s + 1))[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnStruct();
            if ((!(bool)(((dynamic)(s + 1)).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnStruct();
            if ((!(bool)(((dynamic)(s + 1)).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondStructOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnStruct();

if ( (!(bool)(((dynamic)(s + 1)) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondStructIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnStruct();

if ( (!(bool)(((dynamic)(s + 1))[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnInterface();
            if ((!(bool)(((dynamic)(s + 1)).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnInterface();
            if ((!(bool)(((dynamic)(s + 1)).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondInterfaceIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnInterface();

if ( (!(bool)(((dynamic)(s + 1))[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
#if M3
result += Verify.Eval(CallSecondClassOperator);
result += Verify.Eval(CallSecondClassIndexer);
 #endif
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
#if M3
result += Verify.Eval(CallSecondStructOperator);
result += Verify.Eval(CallSecondStructIndexer);
 #endif
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
#if M3
result += Verify.Eval(CallSecondInterfaceIndexer);
 #endif
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.statictodynamic016.statictodynamic016
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commondef.commondef;
    // <Title> Static context switch to dynamic context </Title>
    // <Description>
    // First is indexer of class
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class StaticToDynamicOfFirstClassWithIndexer
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnClass();
            if ((!(bool)(((dynamic)s[1]).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnClass();
            if ((!(bool)(((dynamic)s[1]).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondClassOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnClass();

if ( (!(bool)(((dynamic)s[1]) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondClassIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnClass();

if ( (!(bool)(((dynamic)s[1])[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnStruct();
            if ((!(bool)(((dynamic)s[1]).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnStruct();
            if ((!(bool)(((dynamic)s[1]).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondStructOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnStruct();

if ( (!(bool)(((dynamic)s[1]) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondStructIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnStruct();

if ( (!(bool)(((dynamic)s[1])[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnInterface();
            if ((!(bool)(((dynamic)s[1]).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnInterface();
            if ((!(bool)(((dynamic)s[1]).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondInterfaceIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnInterface();

if ( (!(bool)(((dynamic)s[1])[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
#if M3
result += Verify.Eval(CallSecondClassOperator);
result += Verify.Eval(CallSecondClassIndexer);
 #endif
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
#if M3
result += Verify.Eval(CallSecondStructOperator);
result += Verify.Eval(CallSecondStructIndexer);
 #endif
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
#if M3
result += Verify.Eval(CallSecondInterfaceIndexer);
 #endif
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commongendef.commongendef
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    // <Title> Dynamic and static interaction utility class </Title>
    // <Description>
    // Defined generic types used in test.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    #region FirstClassWithSecondClass
    public class FirstClassReturnClass<T>
    {
        private static SecondClass<T> s_secClass = new SecondClass<T>();
        public SecondClass<T> InstanceMethod()
        {
            Verify.FirstCalled = true;
            return s_secClass;
        }

        public static SecondClass<T> StaticMethod()
        {
            Verify.FirstCalled = true;
            return s_secClass;
        }

        public SecondClass<T> InstanceProperty
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secClass;
            }
        }

        public static SecondClass<T> StaticProperty
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secClass;
            }
        }

        public static SecondClass<T> operator +(FirstClassReturnClass<T> s, int i)
        {
            Verify.FirstCalled = true;
            return s_secClass;
        }

        public SecondClass<T> this[int i]
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secClass;
            }
        }
    }

    public class SecondClass<T>
    {
        public bool InstanceMethod()
        {
            return true;
        }

        public bool InstanceProperty
        {
            get
            {
                return true;
            }
        }

        public static bool operator +(SecondClass<T> s, int i)
        {
            return true;
        }

        public bool this[int i]
        {
            get
            {
                return true;
            }
        }
    }

    #endregion
    #region FirstClassWithSecondStruct
    public class FirstClassReturnStruct<T>
    {
        private static SecondStruct<T> s_secStruct = new SecondStruct<T>();
        public SecondStruct<T> InstanceMethod()
        {
            Verify.FirstCalled = true;
            return s_secStruct;
        }

        public static SecondStruct<T> StaticMethod()
        {
            Verify.FirstCalled = true;
            return s_secStruct;
        }

        public SecondStruct<T> InstanceProperty
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secStruct;
            }
        }

        public static SecondStruct<T> StaticProperty
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secStruct;
            }
        }

        public static SecondStruct<T> operator +(FirstClassReturnStruct<T> s, int i)
        {
            Verify.FirstCalled = true;
            return s_secStruct;
        }

        public SecondStruct<T> this[int i]
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secStruct;
            }
        }
    }

    public struct SecondStruct<T>
    {
        public bool InstanceMethod()
        {
            return true;
        }

        public bool InstanceProperty
        {
            get
            {
                return true;
            }
        }

        public static bool operator +(SecondStruct<T> s, int i)
        {
            return true;
        }

        public bool this[int i]
        {
            get
            {
                return true;
            }
        }
    }

    #endregion
    #region FirstClassWithSecondInterface
    public class FirstClassReturnInterface<T>
    {
        private static SecondInterface<T> s_secInterface = new SecondInterfaceImp<T>();
        public SecondInterface<T> InstanceMethod()
        {
            Verify.FirstCalled = true;
            return s_secInterface;
        }

        public static SecondInterface<T> StaticMethod()
        {
            Verify.FirstCalled = true;
            return s_secInterface;
        }

        public SecondInterface<T> InstanceProperty
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secInterface;
            }
        }

        public static SecondInterface<T> StaticProperty
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secInterface;
            }
        }

        public static SecondInterface<T> operator +(FirstClassReturnInterface<T> s, int i)
        {
            Verify.FirstCalled = true;
            return s_secInterface;
        }

        public SecondInterface<T> this[int i]
        {
            get
            {
                Verify.FirstCalled = true;
                return s_secInterface;
            }
        }
    }

    public interface SecondInterface<T>
    {
        bool InstanceMethod();
        bool InstanceProperty
        {
            get;
        }

        bool this[int i]
        {
            get;
        }
    }

    public class SecondInterfaceImp<T> : SecondInterface<T>
    {
        public bool InstanceMethod()
        {
            return true;
        }

        public bool InstanceProperty
        {
            get
            {
                return true;
            }
        }

        public bool this[int i]
        {
            get
            {
                return true;
            }
        }
    }
    #endregion

    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.statictodynamic017.statictodynamic017
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commongendef.commongendef;
    // <Title> Static context switch to dynamic context </Title>
    // <Description>
    // First is instance method of generic class
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class StaticToDynamicOfFirstGenericClassWithInstanceMethod
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnClass<int>();
            if ((!(bool)(((dynamic)s.InstanceMethod()).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnClass<int>();
            if ((!(bool)(((dynamic)s.InstanceMethod()).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondClassOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnClass<int>();

if ( (!(bool)(((dynamic)s.InstanceMethod()) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondClassIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnClass<int>();

if ( (!(bool)(((dynamic)s.InstanceMethod())[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnStruct<int>();
            if ((!(bool)(((dynamic)s.InstanceMethod()).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnStruct<int>();
            if ((!(bool)(((dynamic)s.InstanceMethod()).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondStructOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnStruct<int>();

if ( (!(bool)(((dynamic)s.InstanceMethod()) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondStructIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnStruct<int>();

if ( (!(bool)(((dynamic)s.InstanceMethod())[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnInterface<int>();
            if ((!(bool)(((dynamic)s.InstanceMethod()).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnInterface<int>();
            if ((!(bool)(((dynamic)s.InstanceMethod()).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondInterfaceIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnInterface<int>();

if ( (!(bool)(((dynamic)s.InstanceMethod())[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
#if M3
result += Verify.Eval(CallSecondClassOperator);
result += Verify.Eval(CallSecondClassIndexer);
 #endif
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
#if M3
result += Verify.Eval(CallSecondStructOperator);
result += Verify.Eval(CallSecondStructIndexer);
 #endif
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
#if M3
result += Verify.Eval(CallSecondInterfaceIndexer);
 #endif
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.statictodynamic018.statictodynamic018
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commongendef.commongendef;
    // <Title> Static context switch to dynamic context </Title>
    // <Description>
    // First is instance property of generic class
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class StaticToDynamicOfFirstGenericClassWithInstanceProperty
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnClass<int>();
            if ((!(bool)(((dynamic)s.InstanceProperty).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnClass<int>();
            if ((!(bool)(((dynamic)s.InstanceProperty).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondClassOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnClass<int>();

if ( (!(bool)(((dynamic)s.InstanceProperty) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondClassIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnClass<int>();

if ( (!(bool)(((dynamic)s.InstanceProperty)[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnStruct<int>();
            if ((!(bool)(((dynamic)s.InstanceProperty).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnStruct<int>();
            if ((!(bool)(((dynamic)s.InstanceProperty).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondStructOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnStruct<int>();

if ( (!(bool)(((dynamic)s.InstanceProperty) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondStructIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnStruct<int>();

if ( (!(bool)(((dynamic)s.InstanceProperty)[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnInterface<int>();
            if ((!(bool)(((dynamic)s.InstanceProperty).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnInterface<int>();
            if ((!(bool)(((dynamic)s.InstanceProperty).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondInterfaceIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnInterface<int>();

if ( (!(bool)(((dynamic)s.InstanceProperty)[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
#if M3
result += Verify.Eval(CallSecondClassOperator);
result += Verify.Eval(CallSecondClassIndexer);
 #endif
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
#if M3
result += Verify.Eval(CallSecondStructOperator);
result += Verify.Eval(CallSecondStructIndexer);
 #endif
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
#if M3
result += Verify.Eval(CallSecondInterfaceIndexer);
 #endif
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.statictodynamic019.statictodynamic019
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commongendef.commongendef;
    // <Title> Static context switch to dynamic context </Title>
    // <Description>
    // First is operator of generic class
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class StaticToDynamicOfFirstGenericClassWithOperator
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnClass<int>();
            if ((!(bool)(((dynamic)(s + 1)).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnClass<int>();
            if ((!(bool)(((dynamic)(s + 1)).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondClassOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnClass<int>();

if ( (!(bool)(((dynamic)(s + 1)) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondClassIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnClass<int>();

if ( (!(bool)(((dynamic)(s + 1))[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnStruct<int>();
            if ((!(bool)(((dynamic)(s + 1)).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnStruct<int>();
            if ((!(bool)(((dynamic)(s + 1)).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondStructOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnStruct<int>();

if ( (!(bool)(((dynamic)(s + 1)) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondStructIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnStruct<int>();

if ( (!(bool)(((dynamic)(s + 1))[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnInterface<int>();
            if ((!(bool)(((dynamic)(s + 1)).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnInterface<int>();
            if ((!(bool)(((dynamic)(s + 1)).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondInterfaceIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnInterface<int>();

if ( (!(bool)(((dynamic)(s + 1))[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
#if M3
result += Verify.Eval(CallSecondClassOperator);
result += Verify.Eval(CallSecondClassIndexer);
 #endif
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
#if M3
result += Verify.Eval(CallSecondStructOperator);
result += Verify.Eval(CallSecondStructIndexer);
 #endif
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
#if M3
result += Verify.Eval(CallSecondInterfaceIndexer);
 #endif
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.statictodynamic020.statictodynamic020
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commongendef.commongendef;
    // <Title> Static context switch to dynamic context </Title>
    // <Description>
    // First is indexer of generic class
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class StaticToDynamicOfFirstGenericClassWithIndexer
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnClass<int>();
            if ((!(bool)(((dynamic)s[1]).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnClass<int>();
            if ((!(bool)(((dynamic)s[1]).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondClassOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnClass<int>();

if ( (!(bool)(((dynamic)s[1]) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondClassIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnClass<int>();

if ( (!(bool)(((dynamic)s[1])[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnStruct<int>();
            if ((!(bool)(((dynamic)s[1]).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnStruct<int>();
            if ((!(bool)(((dynamic)s[1]).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondStructOperator()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnStruct<int>();

if ( (!(bool)(((dynamic)s[1]) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondStructIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnStruct<int>();

if ( (!(bool)(((dynamic)s[1])[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnInterface<int>();
            if ((!(bool)(((dynamic)s[1]).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            var s = new FirstClassReturnInterface<int>();
            if ((!(bool)(((dynamic)s[1]).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondInterfaceIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


var s = new FirstClassReturnInterface<int>();

if ( (!(bool)(((dynamic)s[1])[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
#if M3
result += Verify.Eval(CallSecondClassOperator);
result += Verify.Eval(CallSecondClassIndexer);
 #endif
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
#if M3
result += Verify.Eval(CallSecondStructOperator);
result += Verify.Eval(CallSecondStructIndexer);
 #endif
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
#if M3
result += Verify.Eval(CallSecondInterfaceIndexer);
 #endif
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.statictodynamic021.statictodynamic021
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commondef.commondef;
    // <Title> Static context switch to dynamic context </Title>
    // <Description>
    // First is static method of class
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class StaticToDynamicOfFirstClassWithStaticMethod
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(((dynamic)FirstClassReturnClass.StaticMethod()).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(((dynamic)FirstClassReturnClass.StaticMethod()).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondClassOperator()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(((dynamic)FirstClassReturnClass.StaticMethod()) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondClassIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(((dynamic)FirstClassReturnClass.StaticMethod())[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(((dynamic)FirstClassReturnStruct.StaticMethod()).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(((dynamic)FirstClassReturnStruct.StaticMethod()).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondStructOperator()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(((dynamic)FirstClassReturnStruct.StaticMethod()) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondStructIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(((dynamic)FirstClassReturnStruct.StaticMethod())[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(((dynamic)FirstClassReturnInterface.StaticMethod()).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(((dynamic)FirstClassReturnInterface.StaticMethod()).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondInterfaceIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(((dynamic)FirstClassReturnInterface.StaticMethod())[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
#if M3
result += Verify.Eval(CallSecondClassOperator);
result += Verify.Eval(CallSecondClassIndexer);
 #endif
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
#if M3
result += Verify.Eval(CallSecondStructOperator);
result += Verify.Eval(CallSecondStructIndexer);
 #endif
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
#if M3
result += Verify.Eval(CallSecondInterfaceIndexer);
 #endif
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.statictodynamic022.statictodynamic022
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commondef.commondef;
    // <Title> Static context switch to dynamic context </Title>
    // <Description>
    // First is static property of class
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class StaticToDynamicOfFirstClassWithStaticProperty
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(((dynamic)FirstClassReturnClass.StaticProperty).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(((dynamic)FirstClassReturnClass.StaticProperty).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondClassOperator()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(((dynamic)FirstClassReturnClass.StaticProperty) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondClassIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(((dynamic)FirstClassReturnClass.StaticProperty)[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(((dynamic)FirstClassReturnStruct.StaticProperty).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(((dynamic)FirstClassReturnStruct.StaticProperty).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondStructOperator()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(((dynamic)FirstClassReturnStruct.StaticProperty) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondStructIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(((dynamic)FirstClassReturnStruct.StaticProperty)[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(((dynamic)FirstClassReturnInterface.StaticProperty).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(((dynamic)FirstClassReturnInterface.StaticProperty).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondInterfaceIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(((dynamic)FirstClassReturnInterface.StaticProperty)[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
#if M3
result += Verify.Eval(CallSecondClassOperator);
result += Verify.Eval(CallSecondClassIndexer);
 #endif
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
#if M3
result += Verify.Eval(CallSecondStructOperator);
result += Verify.Eval(CallSecondStructIndexer);
 #endif
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
#if M3
result += Verify.Eval(CallSecondInterfaceIndexer);
 #endif
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.statictodynamic023.statictodynamic023
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commongendef.commongendef;
    // <Title> Static context switch to dynamic context </Title>
    // <Description>
    // First is static method of generic class
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class StaticToDynamicOfFirstGenericClassWithStaticMethod
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(((dynamic)FirstClassReturnClass<int>.StaticMethod()).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(((dynamic)FirstClassReturnClass<int>.StaticMethod()).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondClassOperator()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(((dynamic)FirstClassReturnClass<int>.StaticMethod()) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondClassIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(((dynamic)FirstClassReturnClass<int>.StaticMethod())[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(((dynamic)FirstClassReturnStruct<int>.StaticMethod()).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(((dynamic)FirstClassReturnStruct<int>.StaticMethod()).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondStructOperator()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(((dynamic)FirstClassReturnStruct<int>.StaticMethod()) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondStructIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(((dynamic)FirstClassReturnStruct<int>.StaticMethod())[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(((dynamic)FirstClassReturnInterface<int>.StaticMethod()).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(((dynamic)FirstClassReturnInterface<int>.StaticMethod()).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondInterfaceIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(((dynamic)FirstClassReturnInterface<int>.StaticMethod())[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
#if M3
result += Verify.Eval(CallSecondClassOperator);
result += Verify.Eval(CallSecondClassIndexer);
 #endif
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
#if M3
result += Verify.Eval(CallSecondStructOperator);
result += Verify.Eval(CallSecondStructIndexer);
 #endif
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
#if M3
result += Verify.Eval(CallSecondInterfaceIndexer);
 #endif
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.statictodynamic024.statictodynamic024
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commongendef.commongendef;
    // <Title> Static context switch to dynamic context </Title>
    // <Description>
    // First is static property of generic class
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class StaticToDynamicOfFirstGenericClassWithStaticProperty
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(((dynamic)FirstClassReturnClass<int>.StaticProperty).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(((dynamic)FirstClassReturnClass<int>.StaticProperty).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondClassOperator()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(((dynamic)FirstClassReturnClass<int>.StaticProperty) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondClassIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(((dynamic)FirstClassReturnClass<int>.StaticProperty)[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(((dynamic)FirstClassReturnStruct<int>.StaticProperty).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(((dynamic)FirstClassReturnStruct<int>.StaticProperty).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondStructOperator()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(((dynamic)FirstClassReturnStruct<int>.StaticProperty) + 2)) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}


static bool CallSecondStructIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(((dynamic)FirstClassReturnStruct<int>.StaticProperty)[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(((dynamic)FirstClassReturnInterface<int>.StaticProperty).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            if ((!(bool)(((dynamic)FirstClassReturnInterface<int>.StaticProperty).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

#if M3

static bool CallSecondInterfaceIndexer()
{
int failcount = 0; Verify.FirstCalled = false;


if ( (!(bool)(((dynamic)FirstClassReturnInterface<int>.StaticProperty)[2])) || (!Verify.FirstCalled))
{
failcount++;
System.Console.WriteLine("Test failed at call result");
}

return failcount == 0;
}
#endif
        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
#if M3
result += Verify.Eval(CallSecondClassOperator);
result += Verify.Eval(CallSecondClassIndexer);
 #endif
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
#if M3
result += Verify.Eval(CallSecondStructOperator);
result += Verify.Eval(CallSecondStructIndexer);
 #endif
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
#if M3
result += Verify.Eval(CallSecondInterfaceIndexer);
 #endif
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.dynamictostatic001.dynamictostatic001
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commondef.commondef;
    // <Title> Dynamic context switch to static context </Title>
    // <Description>
    // First is instance method of class
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class DynamicToStaticOfFirstClassWithInstanceMethod
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnClass();
            Verify.FirstCalled = false;
            if ((!(((SecondClass)d.InstanceMethod()).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnClass();
            Verify.FirstCalled = false;
            if ((!(((SecondClass)d.InstanceMethod()).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassOperator()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnClass();
            Verify.FirstCalled = false;
            if ((!(((SecondClass)d.InstanceMethod()) + 2)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassIndexer()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnClass();
            Verify.FirstCalled = false;
            if ((!(((SecondClass)d.InstanceMethod())[2])) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnStruct();
            Verify.FirstCalled = false;
            if ((!(((SecondStruct)d.InstanceMethod()).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnStruct();
            Verify.FirstCalled = false;
            if ((!(((SecondStruct)d.InstanceMethod()).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructOperator()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnStruct();
            Verify.FirstCalled = false;
            if ((!(((SecondStruct)d.InstanceMethod()) + 2)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructIndexer()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnStruct();
            Verify.FirstCalled = false;
            if ((!(((SecondStruct)d.InstanceMethod())[2])) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnInterface();
            Verify.FirstCalled = false;
            if ((!(((SecondInterface)d.InstanceMethod()).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnInterface();
            Verify.FirstCalled = false;
            if ((!(((SecondInterface)d.InstanceMethod()).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceIndexer()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnInterface();
            Verify.FirstCalled = false;
            if ((!(((SecondInterface)d.InstanceMethod())[2])) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
            result += Verify.Eval(CallSecondClassOperator);
            result += Verify.Eval(CallSecondClassIndexer);
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
            result += Verify.Eval(CallSecondStructOperator);
            result += Verify.Eval(CallSecondStructIndexer);
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
            result += Verify.Eval(CallSecondInterfaceIndexer);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.dynamictostatic002.dynamictostatic002
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commondef.commondef;
    // <Title> Dynamic context switch to static context </Title>
    // <Description>
    // First is instance property of class
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class DynamicToStaticOfFirstClassWithInstanceProperty
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnClass();
            Verify.FirstCalled = false;
            if ((!(((SecondClass)d.InstanceProperty).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnClass();
            Verify.FirstCalled = false;
            if ((!(((SecondClass)d.InstanceProperty).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassOperator()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnClass();
            Verify.FirstCalled = false;
            if ((!(((SecondClass)d.InstanceProperty) + 2)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassIndexer()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnClass();
            Verify.FirstCalled = false;
            if ((!(((SecondClass)d.InstanceProperty)[2])) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnStruct();
            Verify.FirstCalled = false;
            if ((!(((SecondStruct)d.InstanceProperty).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnStruct();
            Verify.FirstCalled = false;
            if ((!(((SecondStruct)d.InstanceProperty).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructOperator()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnStruct();
            Verify.FirstCalled = false;
            if ((!(((SecondStruct)d.InstanceProperty) + 2)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructIndexer()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnStruct();
            Verify.FirstCalled = false;
            if ((!(((SecondStruct)d.InstanceProperty)[2])) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnInterface();
            Verify.FirstCalled = false;
            if ((!(((SecondInterface)d.InstanceProperty).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnInterface();
            Verify.FirstCalled = false;
            if ((!(((SecondInterface)d.InstanceProperty).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceIndexer()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnInterface();
            Verify.FirstCalled = false;
            if ((!(((SecondInterface)d.InstanceProperty)[2])) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
            result += Verify.Eval(CallSecondClassOperator);
            result += Verify.Eval(CallSecondClassIndexer);
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
            result += Verify.Eval(CallSecondStructOperator);
            result += Verify.Eval(CallSecondStructIndexer);
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
            result += Verify.Eval(CallSecondInterfaceIndexer);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.dynamictostatic003.dynamictostatic003
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commondef.commondef;
    // <Title> Dynamic context switch to static context </Title>
    // <Description>
    // First is operator of class
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class DynamicToStaticOfFirstClassWithOperator
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnClass();
            Verify.FirstCalled = false;
            if ((!(((d + 1)).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnClass();
            Verify.FirstCalled = false;
            if ((!(((d + 1)).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassOperator()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnClass();
            Verify.FirstCalled = false;
            if ((!(((d + 1)) + 2)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassIndexer()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnClass();
            Verify.FirstCalled = false;
            if ((!(((d + 1))[2])) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnStruct();
            Verify.FirstCalled = false;
            if ((!(((d + 1)).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnStruct();
            Verify.FirstCalled = false;
            if ((!(((d + 1)).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructOperator()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnStruct();
            Verify.FirstCalled = false;
            if ((!(((SecondStruct)(d + 1)) + 2)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructIndexer()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnStruct();
            Verify.FirstCalled = false;
            if ((!(((d + 1))[2])) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnInterface();
            Verify.FirstCalled = false;
            if ((!(((d + 1)).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnInterface();
            Verify.FirstCalled = false;
            if ((!(((d + 1)).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceIndexer()
        {
            int failcount = 0;
            dynamic d = new FirstClassReturnInterface();
            Verify.FirstCalled = false;
            if ((!(((d + 1))[2])) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
            result += Verify.Eval(CallSecondClassOperator);
            result += Verify.Eval(CallSecondClassIndexer);
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
            result += Verify.Eval(CallSecondStructOperator);
            result += Verify.Eval(CallSecondStructIndexer);
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
            result += Verify.Eval(CallSecondInterfaceIndexer);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.dynamictostatic004.dynamictostatic004
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commondef.commondef;
    // <Title> Dynamic context switch to static context </Title>
    // <Description>
    // First is indexer of class
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class DynamicToStaticOfFirstClassWithIndexer
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnClass();
            if ((!(((SecondClass)d[1]).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnClass();
            if ((!(((SecondClass)d[1]).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassOperator()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnClass();
            if ((!(((SecondClass)d[1]) + 2)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassIndexer()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnClass();
            if ((!(((SecondClass)d[1])[2])) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnStruct();
            if ((!(((SecondStruct)d[1]).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnStruct();
            if ((!(((SecondStruct)d[1]).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructOperator()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnStruct();
            if ((!(((SecondStruct)d[1]) + 2)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructIndexer()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnStruct();
            if ((!(((SecondStruct)d[1])[2])) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnInterface();
            if ((!(((SecondInterface)d[1]).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnInterface();
            if ((!(((SecondInterface)d[1]).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceIndexer()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnInterface();
            if ((!(((SecondInterface)d[1])[2])) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
            result += Verify.Eval(CallSecondClassOperator);
            result += Verify.Eval(CallSecondClassIndexer);
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
            result += Verify.Eval(CallSecondStructOperator);
            result += Verify.Eval(CallSecondStructIndexer);
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
            result += Verify.Eval(CallSecondInterfaceIndexer);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.dynamictostatic005.dynamictostatic005
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commongendef.commongendef;
    // <Title> Dynamic context switch to static context </Title>
    // <Description>
    // First is instance method of generic class
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class DynamicToStaticOfFirstGenericClassWithInstanceMethod
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnClass<int>();
            if ((!(((SecondClass<int>)d.InstanceMethod()).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnClass<int>();
            if ((!(((SecondClass<int>)d.InstanceMethod()).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassOperator()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnClass<int>();
            if ((!(((SecondClass<int>)d.InstanceMethod()) + 2)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassIndexer()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnClass<int>();
            if ((!(((SecondClass<int>)d.InstanceMethod())[2])) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnStruct<int>();
            if ((!(((SecondStruct<int>)d.InstanceMethod()).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnStruct<int>();
            if ((!(((SecondStruct<int>)d.InstanceMethod()).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructOperator()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnStruct<int>();
            if ((!(((SecondStruct<int>)d.InstanceMethod()) + 2)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructIndexer()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnStruct<int>();
            if ((!(((SecondStruct<int>)d.InstanceMethod())[2])) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnInterface<int>();
            if ((!(((SecondInterface<int>)d.InstanceMethod()).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnInterface<int>();
            if ((!(((SecondInterface<int>)d.InstanceMethod()).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceIndexer()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnInterface<int>();
            if ((!(((SecondInterface<int>)d.InstanceMethod())[2])) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
            result += Verify.Eval(CallSecondClassOperator);
            result += Verify.Eval(CallSecondClassIndexer);
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
            result += Verify.Eval(CallSecondStructOperator);
            result += Verify.Eval(CallSecondStructIndexer);
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
            result += Verify.Eval(CallSecondInterfaceIndexer);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.dynamictostatic006.dynamictostatic006
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commongendef.commongendef;
    // <Title> Dynamic context switch to static context </Title>
    // <Description>
    // First is instance property of generic class
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class DynamicToStaticOfFirstGenericClassWithInstanceProperty
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnClass<int>();
            if ((!(((SecondClass<int>)d.InstanceProperty).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnClass<int>();
            if ((!(((SecondClass<int>)d.InstanceProperty).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassOperator()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnClass<int>();
            if ((!(((SecondClass<int>)d.InstanceProperty) + 2)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassIndexer()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnClass<int>();
            if ((!(((SecondClass<int>)d.InstanceProperty)[2])) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnStruct<int>();
            if ((!(((SecondStruct<int>)d.InstanceProperty).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnStruct<int>();
            if ((!(((SecondStruct<int>)d.InstanceProperty).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructOperator()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnStruct<int>();
            if ((!(((SecondStruct<int>)d.InstanceProperty) + 2)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructIndexer()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnStruct<int>();
            if ((!(((SecondStruct<int>)d.InstanceProperty)[2])) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnInterface<int>();
            if ((!(((SecondInterface<int>)d.InstanceProperty).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnInterface<int>();
            if ((!(((SecondInterface<int>)d.InstanceProperty).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceIndexer()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnInterface<int>();
            if ((!(((SecondInterface<int>)d.InstanceProperty)[2])) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
            result += Verify.Eval(CallSecondClassOperator);
            result += Verify.Eval(CallSecondClassIndexer);
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
            result += Verify.Eval(CallSecondStructOperator);
            result += Verify.Eval(CallSecondStructIndexer);
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
            result += Verify.Eval(CallSecondInterfaceIndexer);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.dynamictostatic007.dynamictostatic007
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commongendef.commongendef;
    // <Title> Dynamic context switch to static context </Title>
    // <Description>
    // First is operator of generic class
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class DynamicToStaticOfFirstGenericClassWithOperator
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnClass<int>();
            if ((!(((d + 1)).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnClass<int>();
            if ((!(((d + 1)).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassOperator()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnClass<int>();
            if ((!(((d + 1)) + 2)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassIndexer()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnClass<int>();
            if ((!(((d + 1))[2])) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnStruct<int>();
            if ((!(((d + 1)).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnStruct<int>();
            if ((!(((d + 1)).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructOperator()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnStruct<int>();
            if ((!(((d + 1)) + 2)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructIndexer()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnStruct<int>();
            if ((!(((d + 1))[2])) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnInterface<int>();
            if ((!(((d + 1)).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnInterface<int>();
            if ((!(((d + 1)).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceIndexer()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnInterface<int>();
            if ((!(((d + 1))[2])) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
            result += Verify.Eval(CallSecondClassOperator);
            result += Verify.Eval(CallSecondClassIndexer);
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
            result += Verify.Eval(CallSecondStructOperator);
            result += Verify.Eval(CallSecondStructIndexer);
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
            result += Verify.Eval(CallSecondInterfaceIndexer);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.dynamictostatic008.dynamictostatic008
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.common.common;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.commongendef.commongendef;
    // <Title> Dynamic context switch to static context </Title>
    // <Description>
    // First is indexer of generic class
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class DynamicToStaticOfFirstGenericClassWithIndexer
    {
        #region Second is class
        private static bool CallSecondClassInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnClass<int>();
            if ((!(((SecondClass<int>)d[1]).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnClass<int>();
            if ((!(((SecondClass<int>)d[1]).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassOperator()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnClass<int>();
            if ((!(((SecondClass<int>)d[1]) + 2)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondClassIndexer()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnClass<int>();
            if ((!(((SecondClass<int>)d[1])[2])) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        #endregion
        #region Second is struct
        private static bool CallSecondStructInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnStruct<int>();
            if ((!(((SecondStruct<int>)d[1]).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnStruct<int>();
            if ((!(((SecondStruct<int>)d[1]).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructOperator()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnStruct<int>();
            if ((!(((SecondStruct<int>)d[1]) + 2)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondStructIndexer()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnStruct<int>();
            if ((!(((SecondStruct<int>)d[1])[2])) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        #endregion
        #region Second is interface
        private static bool CallSecondInterfaceInstanceMethod()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnInterface<int>();
            if ((!(((SecondInterface<int>)d[1]).InstanceMethod())) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceInstanceProperty()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnInterface<int>();
            if ((!(((SecondInterface<int>)d[1]).InstanceProperty)) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        private static bool CallSecondInterfaceIndexer()
        {
            int failcount = 0;
            Verify.FirstCalled = false;
            dynamic d = new FirstClassReturnInterface<int>();
            if ((!(((SecondInterface<int>)d[1])[2])) || (!Verify.FirstCalled))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount == 0;
        }

        #endregion
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(CallSecondClassInstanceMethod);
            result += Verify.Eval(CallSecondClassInstanceProperty);
            result += Verify.Eval(CallSecondClassOperator);
            result += Verify.Eval(CallSecondClassIndexer);
            result += Verify.Eval(CallSecondStructInstanceMethod);
            result += Verify.Eval(CallSecondStructInstanceProperty);
            result += Verify.Eval(CallSecondStructOperator);
            result += Verify.Eval(CallSecondStructIndexer);
            result += Verify.Eval(CallSecondInterfaceInstanceMethod);
            result += Verify.Eval(CallSecondInterfaceInstanceProperty);
            result += Verify.Eval(CallSecondInterfaceIndexer);
            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.combined001.combined001
{
    // <Title> Combined with dynamic context and static context </Title>
    // <Description>
    // One loop "( (S1)d.M1() ).M2().M3().M4()"
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class First
    {
        public Second M1()
        {
            TestCount.CallCount++;
            return new Second();
        }
    }

    public class Second
    {
        public Third M2()
        {
            TestCount.CallCount++;
            return new Third();
        }
    }

    public class Third
    {
        public dynamic M3()
        {
            TestCount.CallCount++;
            return new Fourth();
        }
    }

    public class Fourth
    {
        public bool M4()
        {
            TestCount.CallCount++;
            return true;
        }
    }

    public class TestCount
    {
        internal static int CallCount = 0;
    }

    public class CombinedDynamicAndStaticOneLoop
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int failcount = 0;
            dynamic d = new First();
            if ((!(bool)(((Second)d.M1()).M2().M3().M4())) || (TestCount.CallCount != 4))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.combined002.combined002
{
    // <Title> Combined with dynamic context and static context </Title>
    // <Description>
    // One loop "((dynamic)( ( (S1)d.M1() ).M2().M3() )).M4()"
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class First
    {
        public Second M1()
        {
            TestCount.CallCount++;
            return new Second();
        }
    }

    public class Second
    {
        public Third M2()
        {
            TestCount.CallCount++;
            return new Third();
        }
    }

    public class Third
    {
        public Fourth M3()
        {
            TestCount.CallCount++;
            return new Fourth();
        }
    }

    public class Fourth
    {
        public bool M4()
        {
            TestCount.CallCount++;
            return true;
        }
    }

    public class TestCount
    {
        internal static int CallCount = 0;
    }

    public class CombinedDynamicAndStaticOneLoop
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int failcount = 0;
            dynamic d = new First();
            if ((!(bool)(((dynamic)(((Second)d.M1()).M2().M3())).M4())) || (TestCount.CallCount != 4))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.combined003.combined003
{
    // <Title> Combined with dynamic context and static context </Title>
    // <Description>
    // One loop "( ( (S1)d.P1 ) + 2 ).M3()[4]"
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class First
    {
        public Second P1
        {
            get
            {
                TestCount.CallCount++;
                return new Second();
            }
        }
    }

    public class Second
    {
        public static Third operator +(Second s, int i)
        {
            TestCount.CallCount++;
            return new Third();
        }
    }

    public class Third
    {
        public dynamic M3()
        {
            TestCount.CallCount++;
            return new Fourth();
        }
    }

    public class Fourth
    {
        public bool this[int i]
        {
            get
            {
                TestCount.CallCount++;
                return true;
            }
        }
    }

    public class TestCount
    {
        internal static int CallCount = 0;
    }

    public class CombinedDynamicAndStaticOneLoop
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int failcount = 0;
            dynamic d = new First();
            if ((!(bool)((((Second)d.P1) + 2).M3()[4])) || (TestCount.CallCount != 4))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.combined004.combined004
{
    // <Title> Combined with dynamic context and static context </Title>
    // <Description>
    // One loop "((dynamic)( ( (S1)d.M1() ).P2 + 3 ))[4]"
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class First
    {
        public Second M1()
        {
            TestCount.CallCount++;
            return new Second();
        }
    }

    public class Second
    {
        public Third P2
        {
            get
            {
                TestCount.CallCount++;
                return new Third();
            }
        }
    }

    public class Third
    {
        public static Fourth operator +(Third t, int i)
        {
            TestCount.CallCount++;
            return new Fourth();
        }
    }

    public class Fourth
    {
        public bool this[int i]
        {
            get
            {
                TestCount.CallCount++;
                return true;
            }
        }
    }

    public class TestCount
    {
        internal static int CallCount = 0;
    }

    public class CombinedDynamicAndStaticOneLoop
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int failcount = 0;
            dynamic d = new First();
            if ((!(bool)(((dynamic)(((Second)d.M1()).P2 + 3))[4])) || (TestCount.CallCount != 4))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.combined005.combined005
{
    // <Title> Combined with dynamic context and static context </Title>
    // <Description>
    // Two loop "( (S2)(( (S1)d.M1() ).M2().M3().M4())).M5().M6().M7()"
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class C1
    {
        public C2 M1()
        {
            TestCount.CallCount++;
            return new C2();
        }
    }

    public class C2
    {
        public C3 M2()
        {
            TestCount.CallCount++;
            return new C3();
        }
    }

    public class C3
    {
        public dynamic M3()
        {
            TestCount.CallCount++;
            return new C4();
        }
    }

    public class C4
    {
        public C5 M4()
        {
            TestCount.CallCount++;
            return new C5();
        }
    }

    public class C5
    {
        public C6 M5()
        {
            TestCount.CallCount++;
            return new C6();
        }
    }

    public class C6
    {
        public dynamic M6()
        {
            TestCount.CallCount++;
            return new C7();
        }
    }

    public class C7
    {
        public bool M7()
        {
            TestCount.CallCount++;
            return true;
        }
    }

    public class TestCount
    {
        internal static int CallCount = 0;
    }

    public class CombinedDynamicAndStaticTwoLoop
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int failcount = 0;
            dynamic d = new C1();
            if ((!(bool)(((C5)(((C2)d.M1()).M2().M3().M4())).M5().M6().M7())) || (TestCount.CallCount != 7))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.combined006.combined006
{
    // <Title> Combined with dynamic context and static context </Title>
    // <Description>
    // Two loop "( (S2)((dynamic)( ( (S1)d.M1() ).M2().M3() )).M4() ).M5().M6().M7()"
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class C1
    {
        public C2 M1()
        {
            TestCount.CallCount++;
            return new C2();
        }
    }

    public class C2
    {
        public C3 M2()
        {
            TestCount.CallCount++;
            return new C3();
        }
    }

    public class C3
    {
        public C4 M3()
        {
            TestCount.CallCount++;
            return new C4();
        }
    }

    public class C4
    {
        public C5 M4()
        {
            TestCount.CallCount++;
            return new C5();
        }
    }

    public class C5
    {
        public C6 M5()
        {
            TestCount.CallCount++;
            return new C6();
        }
    }

    public class C6
    {
        public dynamic M6()
        {
            TestCount.CallCount++;
            return new C7();
        }
    }

    public class C7
    {
        public bool M7()
        {
            TestCount.CallCount++;
            return true;
        }
    }

    public class TestCount
    {
        internal static int CallCount = 0;
    }

    public class CombinedDynamicAndStaticTwoLoop
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int failcount = 0;
            dynamic d = new C1();
            if ((!(bool)(((C5)((dynamic)(((C2)d.M1()).M2().M3())).M4()).M5().M6().M7())) || (TestCount.CallCount != 7))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.combined007.combined007
{
    // <Title> Combined with dynamic context and static context </Title>
    // <Description>
    // Two loop "(( (S2)((( (S1)d[1] ) + 2).P3.M4()))[5] + 6).P7"
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class C1
    {
        public C2 this[int i]
        {
            get
            {
                TestCount.CallCount++;
                return new C2();
            }
        }
    }

    public class C2
    {
        public static C3 operator +(C2 c, int i)
        {
            TestCount.CallCount++;
            return new C3();
        }
    }

    public class C3
    {
        public dynamic P3
        {
            get
            {
                TestCount.CallCount++;
                return new C4();
            }
        }
    }

    public class C4
    {
        public C5 M4()
        {
            TestCount.CallCount++;
            return new C5();
        }
    }

    public class C5
    {
        public C6 this[int i]
        {
            get
            {
                TestCount.CallCount++;
                return new C6();
            }
        }
    }

    public class C6
    {
        public static dynamic operator +(C6 c, int i)
        {
            TestCount.CallCount++;
            return new C7();
        }
    }

    public class C7
    {
        public bool P7
        {
            get
            {
                TestCount.CallCount++;
                return true;
            }
        }
    }

    public class TestCount
    {
        internal static int CallCount = 0;
    }

    public class CombinedDynamicAndStaticTwoLoop
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int failcount = 0;
            dynamic d = new C1();
            if ((!(bool)((((C5)((((C2)d[1]) + 2).P3.M4()))[5] + 6).P7)) || (TestCount.CallCount != 7))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.integeregereraction.combined008.combined008
{
    // <Title> Combined with dynamic context and static context </Title>
    // <Description>
    // Two loop "( (S2)((dynamic)( ( (S1)d.M1() ).P2 + 3 ))[4] ).P5[6] + 7"
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class C1
    {
        public C2 M1()
        {
            TestCount.CallCount++;
            return new C2();
        }
    }

    public class C2
    {
        public C3 P2
        {
            get
            {
                TestCount.CallCount++;
                return new C3();
            }
        }
    }

    public class C3
    {
        public static C4 operator +(C3 c, int i)
        {
            TestCount.CallCount++;
            return new C4();
        }
    }

    public class C4
    {
        public C5 this[int i]
        {
            get
            {
                TestCount.CallCount++;
                return new C5();
            }
        }
    }

    public class C5
    {
        public C6 P5
        {
            get
            {
                TestCount.CallCount++;
                return new C6();
            }
        }
    }

    public class C6
    {
        public dynamic this[int i]
        {
            get
            {
                TestCount.CallCount++;
                return new C7();
            }
        }
    }

    public class C7
    {
        public static bool operator +(C7 c, int i)
        {
            TestCount.CallCount++;
            return true;
        }
    }

    public class TestCount
    {
        internal static int CallCount = 0;
    }

    public class CombinedDynamicAndStaticTwoLoop
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int failcount = 0;
            dynamic d = new C1();
            if ((!(bool)(((C5)((dynamic)(((C2)d.M1()).P2 + 3))[4]).P5[6] + 7)) || (TestCount.CallCount != 7))
            {
                failcount++;
                System.Console.WriteLine("Test failed at call result");
            }

            return failcount;
        }
    }
    // </Code>
}
