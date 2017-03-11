// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.overloadResolution.indexer.getter.Oneclass.Twoparam.accessibility005.accessibility005
{
    public class Test
    {
        private class Base
        {
            public int this[int x]
            {
                get
                {
                    return 1;
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
            var rez = b[x];
            if (rez == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.overloadResolution.indexer.getter.Oneclass.Twoparam.accessibility006.accessibility006
{
    public class Test
    {
        protected class Base
        {
            public int this[int x]
            {
                get
                {
                    return 1;
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
            var rez = b[x];
            if (rez == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.overloadResolution.indexer.getter.Oneclass.Twoparam.accessibility007.accessibility007
{
    public class Test
    {
        internal class Base
        {
            public int this[int x]
            {
                get
                {
                    return 1;
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
            var rez = b[x];
            if (rez == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.overloadResolution.indexer.getter.Oneclass.Twoparam.accessibility011.accessibility011
{
    public class Test
    {
        public class Higher
        {
            private class Base
            {
                public int this[int x]
                {
                    get
                    {
                        return 1;
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
                var rez = b[x];
                if (rez == 1)
                    return 0;
                return 1;
            }
        }
    }
    // </Code>
}
