// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ModuleBuilderDefineInitializedData
    {
        [Theory]
        [InlineData(FieldAttributes.Static | FieldAttributes.Public)]
        [InlineData(FieldAttributes.Static | FieldAttributes.Private)]
        [InlineData( FieldAttributes.Private)]
        public void TestWithStaticAndPublic(FieldAttributes attributes)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            FieldBuilder field = module.DefineInitializedData("MyField", new byte[] { 01, 00, 01 }, attributes);
            Assert.True(field.IsStatic);
            Assert.Equal((attributes & FieldAttributes.Public) != 0 , field.IsPublic);
            Assert.Equal((attributes & FieldAttributes.Private) != 0, field.IsPrivate);
            Assert.Equal(field.Name, "MyField");
        }

        [Fact]
        public void DefineInitializedData_EmptyName_ThrowsArgumentException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            AssertExtensions.Throws<ArgumentException>("name", () => module.DefineInitializedData("", new byte[] { 1, 0, 1 }, FieldAttributes.Private));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(0x3f0000)]
        public void DefineInitializedData_InvalidDataLength_ThrowsArgumentException(int length)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            AssertExtensions.Throws<ArgumentException>(null, () => module.DefineInitializedData("MyField", new byte[length], FieldAttributes.Public));
        }

        [Fact]
        public void DefineInitializedData_NullName_ThrowsArgumentNullException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            AssertExtensions.Throws<ArgumentNullException>("name", () => module.DefineInitializedData(null, new byte[] { 1, 0, 1 }, FieldAttributes.Public));
        }

        [Fact]
        public void DefineInitializedData_NullData_ThrowsArgumentNullException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            AssertExtensions.Throws<ArgumentNullException>("data", () => module.DefineInitializedData("MyField", null, FieldAttributes.Public));
        }

        [Fact]
        public void DefineInitializedData_CreateGlobalFunctionsCalled_ThrowsInvalidOperationException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            FieldBuilder field = module.DefineInitializedData("MyField", new byte[] { 1, 0, 1 }, FieldAttributes.Public);
            module.CreateGlobalFunctions();
            Assert.Throws<InvalidOperationException>(() => module.DefineInitializedData("MyField2", new byte[] { 1, 0, 1 }, FieldAttributes.Public));
        }
    }
}
