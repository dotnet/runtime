// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class SignatureHelperAddArgument
    {
        [Fact]
        public void AddArgument_Type()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            SignatureHelper helper = SignatureHelper.GetFieldSigHelper(module);
            
            helper.AddArgument(typeof(string));
            Assert.Equal(2, helper.GetSignature().Length);
        }

        [Theory]
        [InlineData(true, 3)]
        [InlineData(false, 2)]
        public void AddArgument_Type_Bool(bool pinned, int expectedLength)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            SignatureHelper helper = SignatureHelper.GetFieldSigHelper(module);

            helper.AddArgument(typeof(string), pinned);
            Assert.Equal(expectedLength, helper.GetSignature().Length);
        }

        [Theory]
        [InlineData(null, null, 2)]
        [InlineData(new Type[] { typeof(int) }, null, 4)]
        [InlineData(null, new Type[] { typeof(Type) }, 4)]
        [InlineData(new Type[] { typeof(int) }, new Type[] { typeof(Type) }, 6)]
        public void AddArgument_Type_TypeArray_TypeArray(Type[] requiredCustomModifiers, Type[] optionalCustomModifiers, int expectedLength)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            SignatureHelper helper = SignatureHelper.GetFieldSigHelper(module);

            helper.AddArgument(typeof(string), requiredCustomModifiers, optionalCustomModifiers);
            Assert.Equal(expectedLength, helper.GetSignature().Length);
        }

        [Fact]
        public void AddArgument_NullType_ThrowsArgumentNullException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            SignatureHelper helper = SignatureHelper.GetFieldSigHelper(module);

            AssertExtensions.Throws<ArgumentNullException>("argument", () => helper.AddArgument(null));
            AssertExtensions.Throws<ArgumentNullException>("argument", () => helper.AddArgument(null, true));
            AssertExtensions.Throws<ArgumentNullException>("argument", () => helper.AddArgument(null, null, null));
        }

        [Fact]
        public void AddArgument_SignatureFinished_ThrowsArgumentException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            SignatureHelper helper = SignatureHelper.GetFieldSigHelper(module);
            helper.GetSignature();

            AssertExtensions.Throws<ArgumentException>(null, () => helper.AddArgument(typeof(string)));
            AssertExtensions.Throws<ArgumentException>(null, () => helper.AddArgument(typeof(string), null, null));
        }

        [Fact]
        public void AddArgument_DifferentCountsForCustomModifiers_ThrowsArgumentException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            SignatureHelper helper = SignatureHelper.GetFieldSigHelper(module);
            helper.GetSignature();

            AssertExtensions.Throws<ArgumentException>(null, () => helper.AddArgument(typeof(string), new Type[] { typeof(int) }, null));
            AssertExtensions.Throws<ArgumentException>(null, () => helper.AddArgument(typeof(string), null, new Type[] { typeof(int) }));
        }
    }
}
