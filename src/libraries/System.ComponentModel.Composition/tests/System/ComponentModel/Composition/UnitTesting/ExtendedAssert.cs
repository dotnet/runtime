// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.UnitTesting
{
    public static class ExtendedAssert
    {
        public static void EnumsContainSameValues<TEnum1, TEnum2>()
            where TEnum1 : struct
            where TEnum2 : struct
        {
            EnumsContainSameValuesCore<TEnum1, TEnum2>();
            EnumsContainSameValuesCore<TEnum2, TEnum1>();            
        }

        private static void EnumsContainSameValuesCore<TEnum1, TEnum2>()
            where TEnum1 : struct
            where TEnum2 : struct
        {
            var values = TestServices.GetEnumValues<TEnum1>();

            foreach (TEnum1 value in values)
            {
                string name1 = Enum.GetName(typeof(TEnum1), value);
                string name2 = Enum.GetName(typeof(TEnum2), value);

                Assert.Equal(name1, name2);
            }
        }
    }
}
