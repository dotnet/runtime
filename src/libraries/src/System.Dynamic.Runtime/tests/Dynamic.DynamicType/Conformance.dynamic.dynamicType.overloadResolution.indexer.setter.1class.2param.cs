// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.overloadResolution.indexer.setter.Oneclass.Twoparam.accessibility001.accessibility001
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.overloadResolution.indexer.setter.Oneclass.Twoparam.accessibility001.accessibility001;

    public class Test
    {
        private int this[int x]
        {
            set
            {
                Status = 0;
            }
        }

        public static int Status = -1;
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Test b = new Test();
            dynamic x = int.MaxValue;
            b[x] = 1;
            return Status;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.overloadResolution.indexer.setter.Oneclass.Twoparam.accessibility002.accessibility002
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.overloadResolution.indexer.setter.Oneclass.Twoparam.accessibility002.accessibility002;

    public class Test
    {
        protected int this[int x]
        {
            set
            {
                Status = 0;
            }
        }

        public static int Status = -1;

        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            var b = new Test();
            dynamic x = int.MaxValue;
            b[x] = 1;
            return Status;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.overloadResolution.indexer.setter.Oneclass.Twoparam.accessibility005.accessibility005
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.overloadResolution.indexer.setter.Oneclass.Twoparam.accessibility005.accessibility005;

    public class Test
    {
        private class Base
        {
            public int this[int x]
            {
                set
                {
                    Test.Status = 1;
                }
            }
        }

        public static int Status;
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Base b = new Base();
            dynamic x = int.MaxValue;
            b[x] = 1;
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.overloadResolution.indexer.setter.Oneclass.Twoparam.accessibility006.accessibility006
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.overloadResolution.indexer.setter.Oneclass.Twoparam.accessibility006.accessibility006;

    public class Test
    {
        protected class Base
        {
            public int this[int x]
            {
                set
                {
                    Test.Status = 1;
                }
            }
        }

        public static int Status;
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Base b = new Base();
            dynamic x = int.MaxValue;
            b[x] = 1;
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.overloadResolution.indexer.setter.Oneclass.Twoparam.accessibility007.accessibility007
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.overloadResolution.indexer.setter.Oneclass.Twoparam.accessibility007.accessibility007;

    public class Test
    {
        internal class Base
        {
            public int this[int x]
            {
                set
                {
                    Test.Status = 1;
                }
            }
        }

        public static int Status;
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Base b = new Base();
            dynamic x = int.MaxValue;
            b[x] = 1;
            if (Test.Status == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.overloadResolution.indexer.setter.Oneclass.Twoparam.accessibility011.accessibility011
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.overloadResolution.indexer.setter.Oneclass.Twoparam.accessibility011.accessibility011;

    public class Test
    {
        public static int Status;
        public class Higher
        {
            private class Base
            {
                public int this[int x]
                {
                    set
                    {
                        Test.Status = 1;
                    }
                }
            }

            
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod(null));
            }

            public static int MainMethod(string[] args)
            {
                Base b = new Base();
                dynamic x = int.MaxValue;
                b[x] = 1;
                if (Test.Status == 1)
                    return 0;
                return 1;
            }
        }
    }
    // </Code>
}
