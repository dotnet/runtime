// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public class DynamicAttributeTests
    {
        [Fact]
        public void NullToCtor()
        {
            AssertExtensions.Throws<ArgumentNullException>("transformFlags", () => new DynamicAttribute(null));
        }

        [Fact]
        public void DefaultCtorSingleTrueFlag()
        {
            IList<bool> flags = new DynamicAttribute().TransformFlags;
            Assert.Equal(1, flags.Count);
            Assert.True(flags[0]);
        }

        public static dynamic ParamsAndReturns(
            dynamic d, dynamic[] da, List<dynamic> ld, Dictionary<dynamic, object> dido,
            Dictionary<object, dynamic> diod, Dictionary<dynamic, List<Dictionary<dynamic, int>>> dolddi)
        {
            return null;
        }

        [Fact]
        public void DefaultForDynamicParam()
        {
            IList<bool> flags = ((DynamicAttribute)GetType().GetMethod(nameof(ParamsAndReturns)).GetParameters()[0]
                .GetCustomAttribute(typeof(DynamicAttribute))).TransformFlags;
            Assert.Equal(1, flags.Count);
            Assert.True(flags[0]);
        }


        [Fact]
        public void DefaultForDynamicReturn()
        {
            IList<bool> flags = ((DynamicAttribute)GetType().GetMethod(nameof(ParamsAndReturns)).ReturnParameter
                .GetCustomAttribute(typeof(DynamicAttribute))).TransformFlags;
            Assert.Equal(1, flags.Count);
            Assert.True(flags[0]);
        }

        [Fact]
        public void ArrayOfDynamic()
        {
            IList<bool> flags = ((DynamicAttribute)GetType().GetMethod(nameof(ParamsAndReturns)).GetParameters()[1]
                .GetCustomAttribute(typeof(DynamicAttribute))).TransformFlags;
            Assert.Equal(new[] { false, true }, flags);
        }

        [Fact]
        public void ListOfDynamic()
        {
            IList<bool> flags = ((DynamicAttribute)GetType().GetMethod(nameof(ParamsAndReturns)).GetParameters()[2]
                .GetCustomAttribute(typeof(DynamicAttribute))).TransformFlags;
            Assert.Equal(new[] { false, true }, flags);
        }

        [Fact]
        public void DictionaryWithDynamicKey()
        {
            IList<bool> flags = ((DynamicAttribute)GetType().GetMethod(nameof(ParamsAndReturns)).GetParameters()[3]
                .GetCustomAttribute(typeof(DynamicAttribute))).TransformFlags;
            Assert.Equal(new[] { false, true, false }, flags);
        }

        [Fact]
        public void DictionaryWithDynamicValue()
        {
            IList<bool> flags = ((DynamicAttribute)GetType().GetMethod(nameof(ParamsAndReturns)).GetParameters()[4]
                .GetCustomAttribute(typeof(DynamicAttribute))).TransformFlags;
            Assert.Equal(new[] { false, false, true }, flags);
        }

        [Fact]
        public void ComplexGenericWithDynamic()
        {
            IList<bool> flags = ((DynamicAttribute)GetType().GetMethod(nameof(ParamsAndReturns)).GetParameters()[5]
                .GetCustomAttribute(typeof(DynamicAttribute))).TransformFlags;
            Assert.Equal(new[] { false, true, false, false, true, false }, flags);
        }
    }
}
