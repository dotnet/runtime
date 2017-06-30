// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace SampleDynamicTests
{
    public class Animal
    {
    }

    public class Tiger : Animal
    {
    }

    public class VarianceTest
    {
        private delegate T Create<out T>();

        [Fact]
        public static void VarianceTest_RunTest()
        {
            dynamic d1 = (Create<Tiger>)(() => new Tiger());
            Create<Animal> d2 = d1;
            Animal a1 = d2();

            Create<Tiger> d3 = () => new Tiger();
            dynamic d4 = (Create<Animal>)d3;
            Animal a2 = d4();
        }
    }
}
