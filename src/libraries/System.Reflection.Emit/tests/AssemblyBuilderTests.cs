// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class BoolAllAttribute : Attribute
    {
        private bool _s;
        public BoolAllAttribute(bool s) { _s = s; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class IntClassAttribute : Attribute
    {
        public int i;
        public IntClassAttribute(int i) { this.i = i; }
    }

    public class AssemblyTests
    {
        // The ECMA replacement key for the Microsoft implementation of the CLR.
        private static readonly byte[] TheKey =
        {
            0x00,0x24,0x00,0x00,0x04,0x80,0x00,0x00,0x94,0x00,0x00,0x00,0x06,0x02,0x00,0x00,
            0x00,0x24,0x00,0x00,0x52,0x53,0x41,0x31,0x00,0x04,0x00,0x00,0x01,0x00,0x01,0x00,
            0x07,0xd1,0xfa,0x57,0xc4,0xae,0xd9,0xf0,0xa3,0x2e,0x84,0xaa,0x0f,0xae,0xfd,0x0d,
            0xe9,0xe8,0xfd,0x6a,0xec,0x8f,0x87,0xfb,0x03,0x76,0x6c,0x83,0x4c,0x99,0x92,0x1e,
            0xb2,0x3b,0xe7,0x9a,0xd9,0xd5,0xdc,0xc1,0xdd,0x9a,0xd2,0x36,0x13,0x21,0x02,0x90,
            0x0b,0x72,0x3c,0xf9,0x80,0x95,0x7f,0xc4,0xe1,0x77,0x10,0x8f,0xc6,0x07,0x77,0x4f,
            0x29,0xe8,0x32,0x0e,0x92,0xea,0x05,0xec,0xe4,0xe8,0x21,0xc0,0xa5,0xef,0xe8,0xf1,
            0x64,0x5c,0x4c,0x0c,0x93,0xc1,0xab,0x99,0x28,0x5d,0x62,0x2c,0xaa,0x65,0x2c,0x1d,
            0xfa,0xd6,0x3d,0x74,0x5d,0x6f,0x2d,0xe5,0xf1,0x7e,0x5e,0xaf,0x0f,0xc4,0x96,0x3d,
            0x26,0x1c,0x8a,0x12,0x43,0x65,0x18,0x20,0x6d,0xc0,0x93,0x34,0x4d,0x5a,0xd2,0x93
        };

        public static IEnumerable<object[]> DefineDynamicAssembly_TestData()
        {
            foreach (AssemblyBuilderAccess access in new AssemblyBuilderAccess[] { AssemblyBuilderAccess.Run, AssemblyBuilderAccess.RunAndCollect })
            {
                yield return new object[] { new AssemblyName("TestName") { Version = new Version(0, 0, 0, 0) }, access };
                yield return new object[] { new AssemblyName("testname") { Version = new Version(1, 2, 3, 4) }, access };
                yield return new object[] { new AssemblyName("class") { Version = new Version(0, 0, 0, 0) }, access };
                yield return new object[] { new AssemblyName("\uD800\uDC00") { Version = new Version(0, 0, 0, 0) }, access };

                AssemblyName testPublicKey = new AssemblyName("TestPublicKey") { Version = new Version(0, 0, 0, 0) };
                testPublicKey.CultureInfo = CultureInfo.InvariantCulture;
                testPublicKey.SetPublicKey(TheKey);
                yield return new object[] { testPublicKey, access };
            }
        }

        [Theory]
        [MemberData(nameof(DefineDynamicAssembly_TestData))]
        public void DefineDynamicAssembly_AssemblyName_AssemblyBuilderAccess(AssemblyName name, AssemblyBuilderAccess access)
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(name, access);
            VerifyAssemblyBuilder(assembly, name, new CustomAttributeBuilder[0]);
        }

        public static IEnumerable<object[]> DefineDynamicAssembly_CustomAttributes_TestData()
        {
            foreach (object[] data in DefineDynamicAssembly_TestData())
            {
                yield return new object[] { data[0], data[1], null };
                yield return new object[] { data[0], data[1], new CustomAttributeBuilder[0] };

                ConstructorInfo constructor = typeof(IntClassAttribute).GetConstructor(new Type[] { typeof(int) });
                yield return new object[] { data[0], data[1], new CustomAttributeBuilder[] { new CustomAttributeBuilder(constructor, new object[] { 10 }) } };
            }
        }

        [Theory]
        [MemberData(nameof(DefineDynamicAssembly_CustomAttributes_TestData))]
        public void DefineDynamicAssembly_AssemblyName_AssemblyBuilderAccess_CustomAttributeBuilder(AssemblyName name, AssemblyBuilderAccess access, IEnumerable<CustomAttributeBuilder> attributes)
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(name, access, attributes);
            VerifyAssemblyBuilder(assembly, name, attributes);
        }

        [Fact]
        public void DefineDynamicAssembly_NullName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("name", () => AssemblyBuilder.DefineDynamicAssembly(null, AssemblyBuilderAccess.Run));
            AssertExtensions.Throws<ArgumentNullException>("name", () => AssemblyBuilder.DefineDynamicAssembly(null, AssemblyBuilderAccess.Run, new CustomAttributeBuilder[0]));
        }

        [Theory]
        [InlineData((AssemblyBuilderAccess)2)] // Save (not supported)
        [InlineData((AssemblyBuilderAccess)2 | AssemblyBuilderAccess.Run)] // RunAndSave (not supported)
        [InlineData((AssemblyBuilderAccess)6)] // ReflectionOnly (not supported)
        public void DefineDynamicAssembly_CoreclrNotSupportedAccess_ThrowsArgumentException(AssemblyBuilderAccess access)
        {
            DefineDynamicAssembly_InvalidAccess_ThrowsArgumentException(access);
        }

        [Theory]
        [InlineData((AssemblyBuilderAccess)(-1))]
        [InlineData((AssemblyBuilderAccess)0)]
        [InlineData((AssemblyBuilderAccess)10)]
        [InlineData((AssemblyBuilderAccess)int.MaxValue)]
        public void DefineDynamicAssembly_InvalidAccess_ThrowsArgumentException(AssemblyBuilderAccess access)
        {
            AssertExtensions.Throws<ArgumentException>("access", () => AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), access));
            AssertExtensions.Throws<ArgumentException>("access", () => AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), access, new CustomAttributeBuilder[0]));
        }

        [Fact]
        public void DefineDynamicAssembly_NameIsCopy()
        {
            AssemblyName name = new AssemblyName("Name") { Version = new Version(0, 0, 0, 0) };
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
            Assert.StartsWith(name.ToString(), assembly.FullName);

            name.Name = "NewName";
            Assert.False(assembly.FullName.StartsWith(name.ToString()));
        }

        public static IEnumerable<object[]> DefineDynamicModule_TestData()
        {
            yield return new object[] { "Module" };
            yield return new object[] { "module" };
            yield return new object[] { "class" };
            yield return new object[] { "\uD800\uDC00" };
            yield return new object[] { new string('a', 259) };
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        [MemberData(nameof(DefineDynamicModule_TestData))]
        public void DefineDynamicModule(string name)
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            ModuleBuilder module = assembly.DefineDynamicModule(name);

            Assert.Equal(assembly, module.Assembly);
            Assert.Empty(module.CustomAttributes);

            Assert.Equal("<In Memory Module>", module.Name);

            // The coreclr ignores the name passed to AssemblyBuilder.DefineDynamicModule
            Assert.Equal("RefEmit_InMemoryManifestModule", module.FullyQualifiedName);

            Assert.Equal(module, assembly.GetDynamicModule(module.FullyQualifiedName));
        }

        [Fact]
        public void DefineDynamicModule_NullName_ThrowsArgumentNullException()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            AssertExtensions.Throws<ArgumentNullException>("name", () => assembly.DefineDynamicModule(null));
        }

        [Theory]
        [InlineData("")]
        [InlineData("\0test")]
        public void DefineDynamicModule_InvalidName_ThrowsArgumentException(string name)
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            AssertExtensions.Throws<ArgumentException>("name", () => assembly.DefineDynamicModule(name));
        }

        [Fact]
        public void DefineDynamicModule_ModuleAlreadyDefined_ThrowsInvalidOperationException()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            assembly.DefineDynamicModule("module1");
            Assert.Throws<InvalidOperationException>(() => assembly.DefineDynamicModule("module1"));
            Assert.Throws<InvalidOperationException>(() => assembly.DefineDynamicModule("module2"));
        }

        [Fact]
        public void GetManifestResourceNames_ThrowsNotSupportedException()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            Assert.Throws<NotSupportedException>(() => assembly.GetManifestResourceNames());
        }

        [Fact]
        public void GetManifestResourceStream_ThrowsNotSupportedException()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            Assert.Throws<NotSupportedException>(() => assembly.GetManifestResourceStream(""));
        }

        [Fact]
        public void GetManifestResourceInfo_ThrowsNotSupportedException()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            Assert.Throws<NotSupportedException>(() => assembly.GetManifestResourceInfo(""));
        }

        [Fact]
        public void ExportedTypes_ThrowsNotSupportedException()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            Assert.Throws<NotSupportedException>(() => assembly.ExportedTypes);
            Assert.Throws<NotSupportedException>(() => assembly.GetExportedTypes());
        }

        [Theory]
        [InlineData("testmodule")]
        [InlineData("\0test")]
        public void GetDynamicModule_NoSuchModule_ReturnsNull(string name)
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            assembly.DefineDynamicModule("TestModule");

            Assert.Null(assembly.GetDynamicModule(name));
        }

        [Fact]
        public void GetDynamicModule_InvalidName_ThrowsArgumentException()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            AssertExtensions.Throws<ArgumentNullException>("name", () => assembly.GetDynamicModule(null));
            AssertExtensions.Throws<ArgumentException>("name", () => assembly.GetDynamicModule(""));
        }

        [Theory]
        [InlineData(AssemblyBuilderAccess.Run)]
        [InlineData(AssemblyBuilderAccess.RunAndCollect)]
        public void SetCustomAttribute_ConstructorBuilder_ByteArray(AssemblyBuilderAccess access)
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly(access: access);
            ConstructorInfo constructor = typeof(BoolAllAttribute).GetConstructor(new Type[] { typeof(bool) });
            assembly.SetCustomAttribute(constructor, new byte[] { 1, 0, 1 });

            IEnumerable<Attribute> attributes = assembly.GetCustomAttributes();
            Assert.IsType<BoolAllAttribute>(attributes.First());
        }

        [Fact]
        public void SetCustomAttribute_ConstructorBuildler_ByteArray_NullConstructorBuilder_ThrowsArgumentNullException()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            AssertExtensions.Throws<ArgumentNullException>("con", () => assembly.SetCustomAttribute(null, new byte[0]));
        }

        [Fact]
        public void SetCustomAttribute_ConstructorBuildler_ByteArray_NullByteArray_ThrowsArgumentNullException()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            ConstructorInfo constructor = typeof(IntAllAttribute).GetConstructor(new Type[] { typeof(int) });
            AssertExtensions.Throws<ArgumentNullException>("binaryAttribute", () => assembly.SetCustomAttribute(constructor, null));
        }

        public static IEnumerable<object[]> MalformedIntBlob_TestData()
        {
            // Truncated prolog: completely empty blob.
            yield return new object[] { Array.Empty<byte>(), typeof(CustomAttributeFormatException) };
            // Truncated prolog: only 1 of the required 2 bytes.
            yield return new object[] { new byte[] { 0x01 }, typeof(CustomAttributeFormatException) };
            // Invalid prolog value (0x0000 instead of the required 0x0001).
            yield return new object[] { CustomAttributeBlob.Concat(new byte[] { 0x00, 0x00 }, CustomAttributeBlob.I4(5), CustomAttributeBlob.U2(0)), typeof(CustomAttributeFormatException) };
            // Truncated fixed Int32 argument: only 2 of the required 4 bytes.
            yield return new object[] { CustomAttributeBlob.Concat(CustomAttributeBlob.Prolog, new byte[] { 0x05, 0x00 }), typeof(CustomAttributeFormatException) };
            // 1 stray byte remains after the fixed argument: not enough for the 2-byte named-argument count.
            yield return new object[] { CustomAttributeBlob.Concat(CustomAttributeBlob.Prolog, CustomAttributeBlob.I4(5), new byte[] { 0xFF }), typeof(CustomAttributeFormatException) };
            // Named count says 1, but the blob ends before the member-kind tag byte.
            yield return new object[] { CustomAttributeBlob.Concat(CustomAttributeBlob.Prolog, CustomAttributeBlob.I4(5), CustomAttributeBlob.U2(1)), typeof(CustomAttributeFormatException) };
            // Invalid member-kind tag: neither Field (0x53) nor Property (0x54).
            yield return new object[] { CustomAttributeBlob.Concat(CustomAttributeBlob.Prolog, CustomAttributeBlob.I4(5), CustomAttributeBlob.U2(1), new byte[] { 0x00 }), typeof(CustomAttributeFormatException) };
            // Invalid value-type tag for an otherwise well-formed named Field entry.
            yield return new object[] { CustomAttributeBlob.Concat(CustomAttributeBlob.Prolog, CustomAttributeBlob.I4(5), CustomAttributeBlob.U2(1), new byte[] { CustomAttributeBlob.TagField, 0xAB }), typeof(CustomAttributeFormatException) };
            // Extra trailing byte after an otherwise well-formed, zero-named-argument blob.
            yield return new object[] { CustomAttributeBlob.Concat(CustomAttributeBlob.Prolog, CustomAttributeBlob.I4(5), CustomAttributeBlob.U2(0), new byte[] { 0x00 }), typeof(CustomAttributeFormatException) };
        }

        [Theory]
        [MemberData(nameof(MalformedIntBlob_TestData))]
        public void SetCustomAttribute_ConstructorInfo_ByteArray_MalformedBlob_Throws(byte[] blob, Type expectedExceptionType)
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            ConstructorInfo constructor = typeof(IntAllAttribute).GetConstructor(new Type[] { typeof(int) });
            assembly.SetCustomAttribute(constructor, blob);

            Assert.Throws(expectedExceptionType, () => assembly.GetCustomAttributes().ToArray());
        }

        public static IEnumerable<object[]> MalformedTaggedObjectBlob_TestData()
        {
            // Invalid tag byte for the tagged `object` argument itself.
            yield return new object[] { CustomAttributeBlob.Concat(CustomAttributeBlob.Prolog, new byte[] { 0xAB }, CustomAttributeBlob.U2(0)), typeof(CustomAttributeFormatException) };
            // An enum tag must resolve to an enum, not just a loadable type.
            yield return new object[] { CustomAttributeBlob.Concat(CustomAttributeBlob.Prolog, new byte[] { CustomAttributeBlob.TagEnum }, CustomAttributeBlob.PackedString("System.String"), CustomAttributeBlob.U2(0)), typeof(CustomAttributeFormatException) };
            // Type tag with an empty (zero-length) packed type name.
            yield return new object[] { CustomAttributeBlob.Concat(CustomAttributeBlob.Prolog, new byte[] { CustomAttributeBlob.TagType }, CustomAttributeBlob.PackedString(""), CustomAttributeBlob.U2(0)), typeof(CustomAttributeFormatException) };
            // Type tag whose packed name starts with an embedded NUL, truncating it to empty.
            yield return new object[] { CustomAttributeBlob.Concat(CustomAttributeBlob.Prolog, new byte[] { CustomAttributeBlob.TagType }, CustomAttributeBlob.PackedString("\0System.Int32"), CustomAttributeBlob.U2(0)), typeof(TypeLoadException) };
            // Type tag, assembly-qualified name pointing at a nonexistent assembly.
            yield return new object[] { CustomAttributeBlob.Concat(CustomAttributeBlob.Prolog, new byte[] { CustomAttributeBlob.TagType }, CustomAttributeBlob.PackedString("Ca.Managed.Test.NoSuchType, Ca_Managed_Test_NoSuchAssembly_12345"), CustomAttributeBlob.U2(0)), typeof(FileNotFoundException) };
            // Type tag, unqualified name that does not resolve against the requesting assembly or corelib.
            yield return new object[] { CustomAttributeBlob.Concat(CustomAttributeBlob.Prolog, new byte[] { CustomAttributeBlob.TagType }, CustomAttributeBlob.PackedString("Ca_Managed_Test_NoSuchNamespace.Ca_Managed_Test_NoSuchType_12345"), CustomAttributeBlob.U2(0)), typeof(TypeLoadException) };
            // Array tag with a negative-but-not-(-1) length.
            yield return new object[] { CustomAttributeBlob.Concat(CustomAttributeBlob.Prolog, new byte[] { CustomAttributeBlob.TagArray, CustomAttributeBlob.TagInt32 }, CustomAttributeBlob.I4(-5), CustomAttributeBlob.U2(0)), typeof(OverflowException) };
            // Array tag (primitive elements) whose declared length overflows the remaining bytes.
            yield return new object[] { CustomAttributeBlob.Concat(CustomAttributeBlob.Prolog, new byte[] { CustomAttributeBlob.TagArray, CustomAttributeBlob.TagInt32 }, CustomAttributeBlob.I4(5), CustomAttributeBlob.U2(0)), typeof(CustomAttributeFormatException) };
            // Array tag (reference elements) whose declared length overflows the remaining bytes.
            yield return new object[] { CustomAttributeBlob.Concat(CustomAttributeBlob.Prolog, new byte[] { CustomAttributeBlob.TagArray, CustomAttributeBlob.TagString }, CustomAttributeBlob.I4(5), CustomAttributeBlob.U2(0)), typeof(CustomAttributeFormatException) };
            // Invalid packed-string-length prefix byte (matches none of the 1/2/4-byte encodings, nor the 0xFF null marker).
            yield return new object[] { CustomAttributeBlob.Concat(CustomAttributeBlob.Prolog, new byte[] { CustomAttributeBlob.TagString, 0xE0 }), typeof(CustomAttributeFormatException) };
            // Packed-string length prefix signals the 2-byte form, but the blob ends before its continuation byte.
            yield return new object[] { CustomAttributeBlob.Concat(CustomAttributeBlob.Prolog, new byte[] { CustomAttributeBlob.TagString, 0x80 }), typeof(CustomAttributeFormatException) };
            // Packed string declares 10 UTF8 bytes, but only 3 remain in the blob.
            yield return new object[] { CustomAttributeBlob.Concat(CustomAttributeBlob.Prolog, new byte[] { CustomAttributeBlob.TagString, 0x0A }, new byte[] { (byte)'a', (byte)'b', (byte)'c' }), typeof(CustomAttributeFormatException) };
        }

        [Theory]
        [MemberData(nameof(MalformedTaggedObjectBlob_TestData))]
        public void SetCustomAttribute_ConstructorInfo_ByteArray_MalformedTaggedObjectBlob_Throws(byte[] blob, Type expectedExceptionType)
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            ConstructorInfo constructor = typeof(ObjectAllAttribute).GetConstructor(new Type[] { typeof(object) });
            assembly.SetCustomAttribute(constructor, blob);

            Assert.Throws(expectedExceptionType, () => assembly.GetCustomAttributes().ToArray());
        }

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray_DuplicateNamedField_LastAssignmentWins()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            ConstructorInfo constructor = typeof(IntAllAttribute).GetConstructor(new Type[] { typeof(int) });

            // Two named "Field _i" entries with different values. C# cannot express a duplicate named
            // attribute argument (CS0643 at compile time), so this can only be built as a raw blob.
            byte[] blob = CustomAttributeBlob.Concat(
                CustomAttributeBlob.Prolog,
                CustomAttributeBlob.I4(1),
                CustomAttributeBlob.U2(2),
                new byte[] { CustomAttributeBlob.TagField, CustomAttributeBlob.TagInt32 }, CustomAttributeBlob.PackedString("_i"), CustomAttributeBlob.I4(10),
                new byte[] { CustomAttributeBlob.TagField, CustomAttributeBlob.TagInt32 }, CustomAttributeBlob.PackedString("_i"), CustomAttributeBlob.I4(20));
            assembly.SetCustomAttribute(constructor, blob);

            IntAllAttribute attribute = (IntAllAttribute)assembly.GetCustomAttributes().Single();
            Assert.Equal(20, attribute._i);
        }

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray_NamedFieldTypeMismatch_WrapsArgumentException()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            ConstructorInfo constructor = typeof(IntAllAttribute).GetConstructor(new Type[] { typeof(int) });

            // "_i" is declared int, but the named entry tags its value as a string. FieldInfo.SetValue
            // throws a plain ArgumentException here - unlike a property setter, there is no
            // TargetInvocationException wrapping layer first.
            byte[] blob = CustomAttributeBlob.Concat(
                CustomAttributeBlob.Prolog,
                CustomAttributeBlob.I4(1),
                CustomAttributeBlob.U2(1),
                new byte[] { CustomAttributeBlob.TagField, CustomAttributeBlob.TagString }, CustomAttributeBlob.PackedString("_i"), CustomAttributeBlob.PackedString("oops"));
            assembly.SetCustomAttribute(constructor, blob);

            CustomAttributeFormatException exception = Assert.Throws<CustomAttributeFormatException>(() => assembly.GetCustomAttributes().ToArray());
            Assert.IsType<ArgumentException>(exception.InnerException);
        }

        public static IEnumerable<object[]> MalformedParameterlessBlob_TestData()
        {
            // A non-empty blob for a parameterless attribute constructor must contain at least the 2-byte
            // prolog plus the 2-byte named-argument count (4 bytes total); shorter non-empty blobs must be
            // rejected with a bounded read, not read past the end of the blob. Only a truly empty (0-byte)
            // blob is treated as "zero named arguments" per the ECMA spec; that positive case is already
            // covered by existing SetCustomAttribute(ConstructorInfo, byte[]) fixtures using empty blobs.
            yield return new object[] { new byte[] { 0x01 } };
            yield return new object[] { new byte[] { 0x01, 0x00 } };
            yield return new object[] { new byte[] { 0x01, 0x00, 0x00 } };
        }

        [Theory]
        [MemberData(nameof(MalformedParameterlessBlob_TestData))]
        public void SetCustomAttribute_ConstructorInfo_ByteArray_MalformedParameterlessBlob_Throws(byte[] blob)
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            ConstructorInfo constructor = typeof(EmptyAttribute).GetConstructor(Type.EmptyTypes);
            assembly.SetCustomAttribute(constructor, blob);

            Assert.Throws<CustomAttributeFormatException>(() => assembly.GetCustomAttributes().ToArray());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(127)]
        [InlineData(128)]
        [InlineData(16383)]
        [InlineData(16384)]
        public void SetCustomAttribute_PackedStringBoundaries(int length)
        {
            string expected = new string('x', length);
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            assembly.SetCustomAttribute(typeof(ObjectAllAttribute).GetConstructor(new[] { typeof(object) }),
                CustomAttributeBlob.Concat(CustomAttributeBlob.Prolog, new[] { CustomAttributeBlob.TagString },
                    CustomAttributeBlob.PackedString(expected), CustomAttributeBlob.U2(0)));

            ObjectAllAttribute attribute = Assert.IsType<ObjectAllAttribute>(assembly.GetCustomAttributes().Single());
            Assert.Equal(expected, attribute._o);
        }

        [Theory]
        [InlineData(true, 0x80000000L)]
        [InlineData(true, 0x7fc01234L)]
        [InlineData(true, 0x7f800000L)]
        [InlineData(false, long.MinValue)]
        [InlineData(false, 0x7ff8000000001234L)]
        [InlineData(false, 0x7ff0000000000000L)]
        public void SetCustomAttribute_FloatingPointBits(bool single, long bits)
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            byte tag = single ? CustomAttributeBlob.TagFloat : CustomAttributeBlob.TagDouble;
            byte[] value = single ? CustomAttributeBlob.I4(unchecked((int)bits)) : CustomAttributeBlob.U8(unchecked((ulong)bits));
            assembly.SetCustomAttribute(typeof(ObjectAllAttribute).GetConstructor(new[] { typeof(object) }),
                CustomAttributeBlob.Concat(CustomAttributeBlob.Prolog, new[] { tag }, value, CustomAttributeBlob.U2(0)));

            object actual = Assert.IsType<ObjectAllAttribute>(assembly.GetCustomAttributes().Single())._o;
            if (single)
            {
                Assert.Equal(unchecked((int)bits), BitConverter.SingleToInt32Bits(Assert.IsType<float>(actual)));
            }
            else
            {
                Assert.Equal(bits, BitConverter.DoubleToInt64Bits(Assert.IsType<double>(actual)));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SetCustomAttribute_InvalidUtf8(bool typeName)
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            assembly.SetCustomAttribute(typeof(ObjectAllAttribute).GetConstructor(new[] { typeof(object) }),
                CustomAttributeBlob.Concat(CustomAttributeBlob.Prolog,
                    new[] { typeName ? CustomAttributeBlob.TagType : CustomAttributeBlob.TagString, (byte)1, (byte)0xff },
                    CustomAttributeBlob.U2(0)));

            if (typeName)
            {
                Assert.Throws<TypeLoadException>(() => assembly.GetCustomAttributes().ToArray());
            }
            else
            {
                ObjectAllAttribute attribute = Assert.IsType<ObjectAllAttribute>(assembly.GetCustomAttributes().Single());
                Assert.Equal("\ufffd", attribute._o);
            }
        }

        [Theory]
        [InlineData(AssemblyBuilderAccess.Run)]
        [InlineData(AssemblyBuilderAccess.RunAndCollect)]
        public void SetCustomAttribute_CustomAttributeBuilder(AssemblyBuilderAccess access)
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly(access: access);
            ConstructorInfo constructor = typeof(IntClassAttribute).GetConstructor(new Type[] { typeof(int) });
            CustomAttributeBuilder attributeBuilder = new CustomAttributeBuilder(constructor, new object[] { 5 });
            assembly.SetCustomAttribute(attributeBuilder);

            IEnumerable<Attribute> attributes = assembly.GetCustomAttributes();
            Assert.IsType<IntClassAttribute>(attributes.First());
        }

        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder_NullAttributeBuilder_ThrowsArgumentNullException()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            AssertExtensions.Throws<ArgumentNullException>("customBuilder", () => assembly.SetCustomAttribute(null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void SetCustomAttribute_GetCustomAttributesData_NonPublicVisibility_DefinedInternally()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            ModuleBuilder module = assembly.DefineDynamicModule("DynamicModule");

            TypeBuilder internalAttributeType = module.DefineType("DynamicInternalAttribute", TypeAttributes.NotPublic, typeof(Attribute));
            ConstructorBuilder internalAttributeCtor = internalAttributeType.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);
            internalAttributeCtor.GetILGenerator().Emit(OpCodes.Ret);

            ConstructorInfo internalAttributeCtorInfo = internalAttributeType.CreateTypeInfo().GetConstructor(Array.Empty<Type>());
            CustomAttributeBuilder customAttribute = new CustomAttributeBuilder(internalAttributeCtorInfo, Array.Empty<object>());

            assembly.SetCustomAttribute(customAttribute);
            Assert.Single(assembly.GetCustomAttributesData());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void SetCustomAttribute_GetCustomAttributes_NonPublicVisibility_DefinedInternally()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            ModuleBuilder module = assembly.DefineDynamicModule("DynamicModule");

            TypeBuilder internalAttributeType = module.DefineType("DynamicInternalAttribute", TypeAttributes.NotPublic, typeof(Attribute));
            ConstructorBuilder internalAttributeCtor = internalAttributeType.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);
            internalAttributeCtor.GetILGenerator().Emit(OpCodes.Ret);

            ConstructorInfo internalAttributeCtorInfo = internalAttributeType.CreateTypeInfo().GetConstructor(Array.Empty<Type>());
            CustomAttributeBuilder customAttribute = new CustomAttributeBuilder(internalAttributeCtorInfo, Array.Empty<object>());

            assembly.SetCustomAttribute(customAttribute);
            Assert.Single(assembly.GetCustomAttributes(false));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void SetCustomAttribute_GetCustomAttributesData_NonPublicVisibility_DefinedExternally()
        {
            AssemblyBuilder assembly1 = Helpers.DynamicAssembly();
            ModuleBuilder module1 = assembly1.DefineDynamicModule("DynamicModule");
            AssemblyBuilder assembly2 = Helpers.DynamicAssembly("AnotherTestAssembly");

            TypeBuilder internalAttributeType = module1.DefineType("DynamicInternalAttribute", TypeAttributes.NotPublic, typeof(Attribute));
            ConstructorBuilder internalAttributeCtor = internalAttributeType.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);
            internalAttributeCtor.GetILGenerator().Emit(OpCodes.Ret);

            ConstructorInfo internalAttributeCtorInfo = internalAttributeType.CreateTypeInfo().GetConstructor(Array.Empty<Type>());
            CustomAttributeBuilder customAttribute = new CustomAttributeBuilder(internalAttributeCtorInfo, Array.Empty<object>());

            assembly2.SetCustomAttribute(customAttribute);
            Assert.Single(assembly2.GetCustomAttributesData());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void SetCustomAttribute_GetCustomAttributes_NonPublicVisibility_DefinedExternally()
        {
            AssemblyBuilder assembly1 = Helpers.DynamicAssembly();
            ModuleBuilder module1 = assembly1.DefineDynamicModule("DynamicModule");
            AssemblyBuilder assembly2 = Helpers.DynamicAssembly("AnotherTestAssembly");

            TypeBuilder internalAttributeType = module1.DefineType("DynamicInternalAttribute", TypeAttributes.NotPublic, typeof(Attribute));
            ConstructorBuilder internalAttributeCtor = internalAttributeType.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);
            internalAttributeCtor.GetILGenerator().Emit(OpCodes.Ret);

            ConstructorInfo internalAttributeCtorInfo = internalAttributeType.CreateTypeInfo().GetConstructor(Array.Empty<Type>());
            CustomAttributeBuilder customAttribute = new CustomAttributeBuilder(internalAttributeCtorInfo, Array.Empty<object>());

            assembly2.SetCustomAttribute(customAttribute);
            Assert.Empty(assembly2.GetCustomAttributes(false));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly(name: "Name1");
            yield return new object[] { assembly, assembly, true };
            yield return new object[] { assembly, Helpers.DynamicAssembly("Name1"), false };
            yield return new object[] { assembly, Helpers.DynamicAssembly("Name2"), false };
            yield return new object[] { assembly, Helpers.DynamicAssembly("Name1", access: AssemblyBuilderAccess.RunAndCollect), false };

            yield return new object[] { assembly, new object(), false };
            yield return new object[] { assembly, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void EqualsTest(AssemblyBuilder assembly, object obj, bool expected)
        {
            Assert.Equal(expected, assembly.Equals(obj));
            if (obj is AssemblyBuilder)
            {
                Assert.Equal(expected, assembly.GetHashCode().Equals(obj.GetHashCode()));
            }
        }

        public class CustomAttribute : Attribute
        {
            public CustomAttribute()
            {
            }
        }

        [Fact]
        public void GetReferencedAssemblies()
        {
            // Create an assembly tagged with a custom attribute
            var cattr_asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("custom_attr_assembly"), AssemblyBuilderAccess.Run);

            ConstructorInfo classCtorInfo = typeof(CustomAttribute).GetConstructor(new Type[] { });
            CustomAttributeBuilder cattr = new CustomAttributeBuilder(
                        classCtorInfo,
                        new object[] { });

            Assert.Equal(0, cattr_asm.GetReferencedAssemblies().Length);

            cattr_asm.SetCustomAttribute(cattr);

            // Should now have a single reference, to this assembly
            Assert.Equal(1, cattr_asm.GetReferencedAssemblies().Length);
            Assert.Equal(typeof(CustomAttribute).Assembly.GetName().Name, cattr_asm.GetReferencedAssemblies()[0].Name);
            Assert.Equal(typeof(CustomAttribute).Assembly.GetName().GetPublicKeyToken(), cattr_asm.GetReferencedAssemblies()[0].GetPublicKeyToken());
        }

        private static void VerifyAssemblyBuilder(AssemblyBuilder assembly, AssemblyName name, IEnumerable<CustomAttributeBuilder> attributes)
        {
            Assert.StartsWith(name.ToString(), assembly.FullName);
            Assert.StartsWith(name.ToString(), assembly.GetName().ToString());

            Assert.True(assembly.IsDynamic);

            Assert.Equal(attributes?.Count() ?? 0, assembly.CustomAttributes.Count());

            Assert.Equal(1, assembly.Modules.Count());
            Module module = assembly.Modules.First();
            Assert.NotEmpty(module.Name);
            Assert.Equal(assembly.Modules, assembly.GetModules());

            Assert.Empty(assembly.DefinedTypes);
            Assert.Empty(assembly.GetTypes());
        }

        private static void SamplePrivateMethod()
        {
        }

        internal static void SampleInternalMethod()
        {
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        void Invoke_Private_CrossAssembly_ThrowsMethodAccessException()
        {
            TypeBuilder tb = Helpers.DynamicType(TypeAttributes.Public);
            var mb = tb.DefineMethod("MyMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new Type[] { });

            var ilg = mb.GetILGenerator();

            var callee = typeof(AssemblyTests).GetMethod("SamplePrivateMethod", BindingFlags.Static | BindingFlags.NonPublic);

            ilg.Emit(OpCodes.Call, callee);
            ilg.Emit(OpCodes.Ret);

            var ty = tb.CreateType();

            var mi = ty.GetMethod("MyMethod", BindingFlags.Static | BindingFlags.Public);

            var d = (Action)mi.CreateDelegate(typeof(Action));

            Assert.Throws<MethodAccessException>(() => d());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        void Invoke_Internal_CrossAssembly_ThrowsMethodAccessException()
        {
            TypeBuilder tb = Helpers.DynamicType(TypeAttributes.Public);
            var mb = tb.DefineMethod("MyMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new Type[] { });

            var ilg = mb.GetILGenerator();

            var callee = typeof(AssemblyTests).GetMethod("SampleInternalMethod", BindingFlags.Static | BindingFlags.NonPublic);

            ilg.Emit(OpCodes.Call, callee);
            ilg.Emit(OpCodes.Ret);

            var ty = tb.CreateType();

            var mi = ty.GetMethod("MyMethod", BindingFlags.Static | BindingFlags.Public);

            var d = (Action)mi.CreateDelegate(typeof(Action));

            Assert.Throws<MethodAccessException>(() => d());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        void Invoke_Private_SameAssembly_ThrowsMethodAccessException()
        {
            ModuleBuilder modb = Helpers.DynamicModule();

            string calleeName = "PrivateMethod";

            TypeBuilder tbCalled = modb.DefineType("CalledClass", TypeAttributes.Public);
            var mbCalled = tbCalled.DefineMethod(calleeName, MethodAttributes.Private | MethodAttributes.Static);
            mbCalled.GetILGenerator().Emit(OpCodes.Ret);

            var tyCalled = tbCalled.CreateType();
            var callee = tyCalled.GetMethod(calleeName, BindingFlags.NonPublic | BindingFlags.Static);

            TypeBuilder tb = modb.DefineType("CallerClass", TypeAttributes.Public);
            var mb = tb.DefineMethod("MyMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new Type[] { });

            var ilg = mb.GetILGenerator();

            ilg.Emit(OpCodes.Call, callee);
            ilg.Emit(OpCodes.Ret);

            var ty = tb.CreateType();

            var mi = ty.GetMethod("MyMethod", BindingFlags.Static | BindingFlags.Public);

            var d = (Action)mi.CreateDelegate(typeof(Action));

            Assert.Throws<MethodAccessException>(() => d());
        }

        [Fact]
        public void DefineDynamicAssembly_AssemblyBuilderLocationIsEmpty_InternalAssemblyBuilderLocationIsEmpty()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly(nameof(DefineDynamicAssembly_AssemblyBuilderLocationIsEmpty_InternalAssemblyBuilderLocationIsEmpty));
            Assembly internalAssemblyBuilder = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.FullName == assembly.FullName);

            Assert.Empty(assembly.Location);
            Assert.NotNull(internalAssemblyBuilder);
            Assert.Empty(internalAssemblyBuilder.Location);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void ThrowsWhenDynamicCodeNotSupported()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions.Add("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported", false.ToString());

            using RemoteInvokeHandle remoteHandle = RemoteExecutor.Invoke(static () =>
            {
                var assemblyName = new AssemblyName("TestName");
                AssemblyBuilderAccess access = AssemblyBuilderAccess.Run;

                Assert.Throws<PlatformNotSupportedException>(() => AssemblyBuilder.DefineDynamicAssembly(assemblyName, access));
                Assert.Throws<PlatformNotSupportedException>(() => AssemblyBuilder.DefineDynamicAssembly(assemblyName, access, assemblyAttributes: null));
            }, options);
        }
    }
}
