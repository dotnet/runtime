// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// <Title></Title>
// <Description>
// </Description>
// <RelatedBugs></RelatedBugs>
//<Expects Status=success></Expects>
// <Code>
using System.Dynamic;
using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.IDynamicObject.regr001.regr001
{
    public class myIDO : DynamicObject
    {
        public double this[decimal d]
        {
            get
            {
                return 4;
            }

            set
            {
                Program.Value = 0;
            }
        }
    }

    public class Program
    {
        public static int Value = 1;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new myIDO();
            int dec33 = 4;
            d[dec33] = 4;
            return Program.Value;
        }
    }
    // </Code>
}
