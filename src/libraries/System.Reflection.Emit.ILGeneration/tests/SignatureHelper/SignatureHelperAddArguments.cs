// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class SignatureHelperAddArguments
    {
        public static IEnumerable<object[]> AddArguments_TestData()
        {
            yield return new object[] { null, null, 3 };
            yield return new object[] { null, new Type[][] { new Type[] { typeof(string), typeof(int) }, new Type[] { typeof(char), typeof(Module) } }, 11 };
            yield return new object[] { new Type[][] { new Type[] { typeof(string), typeof(int) }, new Type[] { typeof(char), typeof(Module) } }, null, 11 };
            yield return new object[] { new Type[][] { new Type[] { typeof(string), typeof(int) }, new Type[] { typeof(char), typeof(Module) } }, new Type[][] { new Type[] { typeof(string), typeof(int) }, new Type[] { typeof(char), typeof(Module) } }, 19 };
        }

        [Theory]
        [MemberData(nameof(AddArguments_TestData))]
        public void AddArguments(Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers, int expectedLength)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            SignatureHelper helper = SignatureHelper.GetFieldSigHelper(module);
            
            helper.AddArguments(new Type[] { typeof(string), typeof(int) }, requiredCustomModifiers, optionalCustomModifiers);
            Assert.Equal(expectedLength, helper.GetSignature().Length);
        }

        [Fact]
        public void AddArguments_NullObjectInTypeArguments_ThrowsArgumentNullException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            SignatureHelper helper = SignatureHelper.GetFieldSigHelper(module);

            Assert.Throws<ArgumentNullException>(() => { helper.AddArguments(new Type[] { typeof(char), null }, null, null); });
        }

        [Fact]
        public void AddArguments_SignatureFinished_ThrowsArgumentException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            SignatureHelper helper = SignatureHelper.GetFieldSigHelper(module);
            helper.GetSignature();

            AssertExtensions.Throws<ArgumentException>(null, () => helper.AddArguments(new Type[] { typeof(string) }, null, null));
        }

        [Fact]
        public void AddArgument_NullObjectInRequiredCustomModifiers_ThrowsArgumentNullException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            SignatureHelper helper = SignatureHelper.GetFieldSigHelper(module);

            AssertExtensions.Throws<ArgumentNullException>("requiredCustomModifiers", () => { helper.AddArguments(new Type[] { typeof(string) }, new Type[][] { new Type[] { typeof(int), null } }, null); });
        }

        [Fact]
        public void AddArgument_DifferentCountsForCustomModifiers_ThrowsArgumentException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            SignatureHelper helper = SignatureHelper.GetFieldSigHelper(module);

            AssertExtensions.Throws<ArgumentException>("requiredCustomModifiers", () => helper.AddArguments(new Type[] { typeof(string) }, new Type[][] { new Type[] { typeof(int), typeof(int[]) } }, null));
            AssertExtensions.Throws<ArgumentException>(null, () => helper.AddArguments(new Type[] { typeof(string) }, new Type[][] { new Type[] { typeof(int) }, new Type[] { typeof(char) } }, null));

            AssertExtensions.Throws<ArgumentException>("optionalCustomModifiers", () => helper.AddArguments(new Type[] { typeof(string) }, null, new Type[][] { new Type[] { typeof(int), typeof(int[]) } }));
            AssertExtensions.Throws<ArgumentException>(null, () => helper.AddArguments(new Type[] { typeof(string) }, null, new Type[][] { new Type[] { typeof(int) }, new Type[] { typeof(char) } }));
        }

        [Fact]
        public void AddArgument_NullObjectInOptionalCustomModifiers_ThrowsArgumentNullException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            SignatureHelper helper = SignatureHelper.GetFieldSigHelper(module);

            AssertExtensions.Throws<ArgumentNullException>("optionalCustomModifiers", () => helper.AddArguments(new Type[] { typeof(string) }, null, new Type[][] { new Type[] { typeof(int), null } }));
        }
    }
}
