// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using SampleMetadata;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Tests
{
    public static partial class TypeTests
    {
        public static IEnumerable<object[]> ImplementedInterfacesTestTheoryData => ImplementedInterfacesTestTypeData.Wrap();

        public static IEnumerable<object[]> ImplementedInterfacesTestTypeData
        {
            get
            {
                yield return new object[] {
                    typeof(CInterfaceImplementerI1I2).Project(),
                    new Type[] {
                        typeof(Interface1).Project(),
                        typeof(Interface2).Project() } };

                yield return new object[] {
                    typeof(CInterfaceImplementerC12I2I3).Project(),
                    new Type[] {
                        typeof(Interface1).Project(),
                        typeof(Interface2).Project(),
                        typeof(Interface3).Project() } };

                yield return new object[] {
                    typeof(CInterfaceImplementerI123).Project(),
                    new Type[] {
                        typeof(Interface123).Project(),
                        typeof(Interface1).Project(),
                        typeof(Interface2).Project(),
                        typeof(Interface3).Project() } };

                yield return new object[] {
                    typeof(CInterfaceImplementerII5).Project(),
                    new Type[] {
                        typeof(InterfaceII5).Project(),
                        typeof(InterfaceI5).Project(),
                        typeof(Interface5).Project() } };

                yield return new object[] {
                    typeof(GenericClass4<int, string>).Project(),
                    new Type[] {
                        typeof(IGeneric1<int>).Project(),
                        typeof(IGeneric2<string>).Project() } };

                yield return new object[] {
                    typeof(GenericClassWithQuirkyConstraints1<,>).Project().GetTypeInfo().GenericTypeParameters[0],
                    new Type[] {
                        typeof(IConstrained1).Project() } };

                yield return new object[] {
                    typeof(int[]).Project(),
                    new Type[] {
                    typeof(IEnumerable).Project(),
                    typeof(IEnumerable<int>).Project(),
                    typeof(ICollection<int>).Project(),
                    typeof(IList<int>).Project(),
                    typeof(IReadOnlyList<int>).Project() } };

            }
        }

        [Theory]
        [MemberData(nameof(ImplementedInterfacesTestTheoryData))]
        public static void TestGetInterfaces(TypeWrapper tw, Type[] expectedInterfacesArray)
        {
            Type t = tw?.Type;
            HashSet<Type> expectedInterfaces = new HashSet<Type>();
            foreach (Type expectedIfc in expectedInterfacesArray)
            {
                expectedInterfaces.Add(expectedIfc);
            }

            if (!t.IsInterface)
            {
                // We don't want tests baking in assumptions about what interfaces framework types implement.
                foreach (Type expectedIfc in typeof(object).Project().GetInterfaces())
                {
                    expectedInterfaces.Add(expectedIfc);
                }
            }

            if (t.IsArray)
            {
                foreach (Type expectedIfc in typeof(Array).Project().GetInterfaces())
                {
                    expectedInterfaces.Add(expectedIfc);
                }
            }

            HashSet<Type> seenInterfaces = new HashSet<Type>();
            Type[] actualIfcs = t.GetInterfaces();
            foreach (Type actualIfc in actualIfcs)
            {
                bool notSeenBefore = seenInterfaces.Add(actualIfc);
                Assert.True(notSeenBefore, "Interface " + actualIfc + " appears twice in list.");

                if (!expectedInterfaces.Contains(actualIfc))
                {
                    if (!t.IsArray)  // Don't bake in assumptions about what else arrays might expose
                    {
                        Assert.True(false, "Unexpected interface found: " + actualIfc);
                    }
                }

                if (expectedInterfaces.Contains(actualIfc))
                {
                    expectedInterfaces.Remove(actualIfc);
                }
            }

            foreach (Type leftOver in expectedInterfaces)
            {
                Assert.True(false, "Expected interface not found: " + leftOver);
            }
        }
    }
}
