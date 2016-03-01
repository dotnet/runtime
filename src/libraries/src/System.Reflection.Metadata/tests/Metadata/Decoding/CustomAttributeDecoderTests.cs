// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata.Tests;
using System.Reflection.PortableExecutable;
using Xunit;

namespace System.Reflection.Metadata.Decoding.Tests
{
    public class CustomAttributeDecoderTests
    {
        [Fact]
        public void TestCustomAttributeDecoder()
        {
            using (FileStream stream = File.OpenRead(typeof(HasAttributes).GetTypeInfo().Assembly.Location))
            using (var peReader = new PEReader(stream))
            {
                MetadataReader reader = peReader.GetMetadataReader();
                var provider = new CustomAttributeTypeProvider();
                TypeDefinitionHandle typeDefHandle = TestMetadataResolver.FindTestType(reader, typeof(HasAttributes));


                int i = 0;
                foreach (CustomAttributeHandle attributeHandle in reader.GetCustomAttributes(typeDefHandle))
                {
                    CustomAttribute attribute = reader.GetCustomAttribute(attributeHandle);
                    CustomAttributeValue<string> value = attribute.DecodeValue(provider);

                    switch (i++)
                    {
                        case 0:
                            Assert.Empty(value.FixedArguments);
                            Assert.Empty(value.NamedArguments);
                            break;

                        case 1:
                            Assert.Equal(3, value.FixedArguments.Length);

                            Assert.Equal("string", value.FixedArguments[0].Type);
                            Assert.Equal("0", value.FixedArguments[0].Value);

                            Assert.Equal("int32", value.FixedArguments[1].Type);
                            Assert.Equal(1, value.FixedArguments[1].Value);

                            Assert.Equal("float64", value.FixedArguments[2].Type);
                            Assert.Equal(2.0, value.FixedArguments[2].Value);

                            Assert.Empty(value.NamedArguments);
                            break;

                        case 2:
                            Assert.Equal(3, value.NamedArguments.Length);

                            Assert.Equal(CustomAttributeNamedArgumentKind.Field, value.NamedArguments[0].Kind);
                            Assert.Equal("StringField", value.NamedArguments[0].Name);
                            Assert.Equal("string", value.NamedArguments[0].Type);
                            Assert.Equal("0", value.NamedArguments[0].Value);

                            Assert.Equal(CustomAttributeNamedArgumentKind.Field, value.NamedArguments[1].Kind);
                            Assert.Equal("Int32Field", value.NamedArguments[1].Name);
                            Assert.Equal("int32", value.NamedArguments[1].Type);
                            Assert.Equal(1, value.NamedArguments[1].Value);

                            Assert.Equal(CustomAttributeNamedArgumentKind.Property, value.NamedArguments[2].Kind);
                            Assert.Equal("SByteEnumArrayProperty", value.NamedArguments[2].Name);
                            Assert.Equal(typeof(SByteEnum).FullName + "[]", value.NamedArguments[2].Type);

                            var array = (ImmutableArray<CustomAttributeTypedArgument<string>>)(value.NamedArguments[2].Value);
                            Assert.Equal(1, array.Length);
                            Assert.Equal(typeof(SByteEnum).FullName, array[0].Type);
                            Assert.Equal((sbyte)SByteEnum.Value, array[0].Value);
                            break;

                        default:
                            // TODO: https://github.com/dotnet/corefx/issues/6534
                            // The other cases are missing corresponding assertions. This needs some refactoring to
                            // be data-driven. A better approach would probably be to generically compare reflection
                            // CustomAttributeData to S.R.M CustomAttributeValue for every test attribute applied.
                            break;
                    }
                }
            }
        }

        // no arguments
        [Test]

        // multiple fixed arguments
        [Test("0", 1, 2.0)]

        // multiple named arguments
        [Test(StringField = "0", Int32Field = 1, SByteEnumArrayProperty = new[] { SByteEnum.Value })]

        // multiple fixed and named arguments
        [Test("0", 1, 2.0, StringField = "0", Int32Field = 1, DoubleField = 2.0)]

        // single fixed null argument
        [Test((object)null)]
        [Test((string)null)]
        [Test((Type)null)]
        [Test((int[])null)]

        // single fixed arguments with strong type
        [Test("string")]
        [Test((sbyte)-1)]
        [Test((short)-2)]
        [Test((int)-4)]
        [Test((long)-8)]
        [Test((sbyte)-1)]
        [Test((short)-2)]
        [Test((int)-4)]
        [Test((long)-8)]
        [Test((byte)1)]
        [Test((ushort)2)]
        [Test((uint)4)]
        [Test((ulong)8)]
        [Test(true)]
        [Test(false)]
        [Test(typeof(string))]
        [Test(SByteEnum.Value)]
        [Test(Int16Enum.Value)]
        [Test(Int32Enum.Value)]
        [Test(Int64Enum.Value)]
        [Test(ByteEnum.Value)]
        [Test(UInt16Enum.Value)]
        [Test(UInt32Enum.Value)]
        [Test(UInt64Enum.Value)]
        [Test(new string[] { })]
        [Test(new string[] { "x", "y", "z", null })]
        [Test(new Int32Enum[] { Int32Enum.Value })]

        // same single fixed arguments as above, typed as object
        [Test((object)("string"))]
        [Test((object)(sbyte)-1)]
        [Test((object)(short)-2)]
        [Test((object)(int)-4)]
        [Test((object)(long)-8)]
        [Test((object)(sbyte)-1)]
        [Test((object)(short)-2)]
        [Test((object)(int)-4)]
        [Test((object)(long)-8)]
        [Test((object)(byte)1)]
        [Test((object)(ushort)2)]
        [Test((object)(uint)4)]
        [Test((object)(true))]
        [Test((object)(false))]
        [Test((object)(typeof(string)))]
        [Test((object)(SByteEnum.Value))]
        [Test((object)(Int16Enum.Value))]
        [Test((object)(Int32Enum.Value))]
        [Test((object)(Int64Enum.Value))]
        [Test((object)(ByteEnum.Value))]
        [Test((object)(UInt16Enum.Value))]
        [Test((object)(UInt32Enum.Value))]
        [Test((object)(UInt64Enum.Value))]
        [Test((object)(new string[] { }))]
        [Test((object)(new string[] { "x", "y", "z", null }))]
        [Test((object)(new Int32Enum[] { Int32Enum.Value }))]

        // same values as above two cases, but put into an object[]
        [Test(new object[] {
            "string",
            (sbyte)-1,
            (short)-2,
            (int)-4,
            (long)-8,
            (sbyte)-1,
            (short)-2,
            (int)-4,
            (long)-8,
            (byte)1,
            (ushort)2,
            (uint)4,
            true,
            false,
            typeof(string),
            SByteEnum.Value,
            Int16Enum.Value,
            Int32Enum.Value,
            Int64Enum.Value,
            SByteEnum.Value,
            Int16Enum.Value,
            Int32Enum.Value,
            Int64Enum.Value,
            new string[] {},
            new string[] { "x", "y", "z", null },
        })]

        // same values as strongly-typed fixed arguments as named arguments
        // single fixed arguments with strong type
        [Test(StringField = "string")]
        [Test(SByteField = -1)]
        [Test(Int16Field = -2)]
        [Test(Int32Field = -4)]
        [Test(Int64Field = -8)]
        [Test(SByteField = -1)]
        [Test(Int16Field = -2)]
        [Test(Int32Field = -4)]
        [Test(Int64Field = -8)]
        [Test(ByteField = 1)]
        [Test(UInt16Field = 2)]
        [Test(UInt32Field = 4)]
        [Test(UInt64Field = 8)]
        [Test(BooleanField = true)]
        [Test(BooleanField = false)]
        [Test(TypeField = typeof(string))]
        [Test(SByteEnumField = SByteEnum.Value)]
        [Test(Int16EnumField = Int16Enum.Value)]
        [Test(Int32EnumField = Int32Enum.Value)]
        [Test(Int64EnumField = Int64Enum.Value)]
        [Test(ByteEnumField = ByteEnum.Value)]
        [Test(UInt16EnumField = UInt16Enum.Value)]
        [Test(UInt32EnumField = UInt32Enum.Value)]
        [Test(UInt64EnumField = UInt64Enum.Value)]
        [Test(new string[] { })]
        [Test(new string[] { "x", "y", "z", null })]
        [Test(new Int32Enum[] { Int32Enum.Value })]

        // null named arguments
        [Test(ObjectField = null)]
        [Test(StringField = null)]

        [Test(Int32ArrayProperty = null)]

        private sealed class HasAttributes { }

        public enum SByteEnum : sbyte { Value = -1 }
        public enum Int16Enum : short { Value = -2 }
        public enum Int32Enum : int { Value = -3 }
        public enum Int64Enum : long { Value = -4 }
        public enum ByteEnum : sbyte { Value = 1 }
        public enum UInt16Enum : ushort { Value = 2 }
        public enum UInt32Enum : uint { Value = 3 }
        public enum UInt64Enum : ulong { Value = 4 }

        [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
        public sealed class TestAttribute : Attribute
        {
            public TestAttribute() { }
            public TestAttribute(string x, int y, double z) { }
            public TestAttribute(string value) { }
            public TestAttribute(object value) { }
            public TestAttribute(sbyte value) { }
            public TestAttribute(short value) { }
            public TestAttribute(int value) { }
            public TestAttribute(long value) { }
            public TestAttribute(byte value) { }
            public TestAttribute(ushort value) { }
            public TestAttribute(uint value) { }
            public TestAttribute(ulong value) { }
            public TestAttribute(bool value) { }
            public TestAttribute(float value) { }
            public TestAttribute(double value) { }
            public TestAttribute(Type value) { }
            public TestAttribute(SByteEnum value) { }
            public TestAttribute(Int16Enum value) { }
            public TestAttribute(Int32Enum value) { }
            public TestAttribute(Int64Enum value) { }
            public TestAttribute(ByteEnum value) { }
            public TestAttribute(UInt16Enum value) { }
            public TestAttribute(UInt32Enum value) { }
            public TestAttribute(UInt64Enum value) { }
            public TestAttribute(string[] value) { }
            public TestAttribute(object[] value) { }
            public TestAttribute(sbyte[] value) { }
            public TestAttribute(short[] value) { }
            public TestAttribute(int[] value) { }
            public TestAttribute(long[] value) { }
            public TestAttribute(byte[] value) { }
            public TestAttribute(ushort[] value) { }
            public TestAttribute(uint[] value) { }
            public TestAttribute(ulong[] value) { }
            public TestAttribute(bool[] value) { }
            public TestAttribute(float[] value) { }
            public TestAttribute(double[] value) { }
            public TestAttribute(Type[] value) { }
            public TestAttribute(SByteEnum[] value) { }
            public TestAttribute(Int16Enum[] value) { }
            public TestAttribute(Int32Enum[] value) { }
            public TestAttribute(Int64Enum[] value) { }
            public TestAttribute(ByteEnum[] value) { }
            public TestAttribute(UInt16Enum[] value) { }
            public TestAttribute(UInt32Enum[] value) { }
            public TestAttribute(UInt64Enum[] value) { }

            public string StringField;
            public object ObjectField;
            public sbyte SByteField;
            public short Int16Field;
            public int Int32Field;
            public long Int64Field;
            public byte ByteField;
            public ushort UInt16Field;
            public uint UInt32Field;
            public ulong UInt64Field;
            public bool BooleanField;
            public float SingleField;
            public double DoubleField;
            public Type TypeField;
            public SByteEnum SByteEnumField;
            public Int16Enum Int16EnumField;
            public Int32Enum Int32EnumField;
            public Int64Enum Int64EnumField;
            public ByteEnum ByteEnumField;
            public UInt16Enum UInt16EnumField;
            public UInt32Enum UInt32EnumField;
            public UInt64Enum UInt64EnumField;

            public string[] StringArrayProperty { get; set; }
            public object[] ObjectArrayProperty { get; set; }
            public sbyte[] SByteArrayProperty { get; set; }
            public short[] Int16ArrayProperty { get; set; }
            public int[] Int32ArrayProperty { get; set; }
            public long[] Int64ArrayProperty { get; set; }
            public byte[] ByteArrayProperty { get; set; }
            public ushort[] UInt16ArrayProperty { get; set; }
            public uint[] UInt32ArrayProperty { get; set; }
            public ulong[] UInt64ArrayProperty { get; set; }
            public bool[] BooleanArrayProperty { get; set; }
            public float[] SingleArrayProperty { get; set; }
            public double[] DoubleArrayProperty { get; set; }
            public Type[] TypeArrayProperty { get; set; }
            public SByteEnum[] SByteEnumArrayProperty { get; set; }
            public Int16Enum[] Int16EnumArrayProperty { get; set; }
            public Int32Enum[] Int32EnumArrayProperty { get; set; }
            public Int64Enum[] Int64EnumArrayProperty { get; set; }
            public ByteEnum[] ByteEnumArrayProperty { get; set; }
            public UInt16Enum[] UInt16EnumArrayProperty { get; set; }
            public UInt32Enum[] UInt32EnumArrayProperty { get; set; }
            public UInt64Enum[] UInt64EnumArrayProperty { get; set; }
        }


        private class CustomAttributeTypeProvider : DisassemblingTypeProvider, ICustomAttributeTypeProvider<string>
        {
            public string GetSystemType()
            {
                return "[System.Runtime]System.Type";
            }

            public bool IsSystemType(string type)
            {
                return type == "[System.Runtime]System.Type"  // encountered as typeref
                    || Type.GetType(type) == typeof(Type);    // encountered as serialized to reflection notation
            }

            public string GetTypeFromSerializedName(string name)
            {
                return name;
            }

            public PrimitiveTypeCode GetUnderlyingEnumType(string type)
            {
                Type runtimeType = Type.GetType(type.Replace('/', '+')); // '/' vs '+' is only difference between ilasm and reflection notation for fixed set below.

                if (runtimeType == typeof(SByteEnum))
                    return PrimitiveTypeCode.SByte;

                if (runtimeType == typeof(Int16Enum))
                    return PrimitiveTypeCode.Int16;

                if (runtimeType == typeof(Int32Enum))
                    return PrimitiveTypeCode.Int32;

                if (runtimeType == typeof(Int64Enum))
                    return PrimitiveTypeCode.Int64;

                if (runtimeType == typeof(ByteEnum))
                    return PrimitiveTypeCode.Byte;

                if (runtimeType == typeof(UInt16Enum))
                    return PrimitiveTypeCode.UInt16;

                if (runtimeType == typeof(UInt32Enum))
                    return PrimitiveTypeCode.UInt32;

                if (runtimeType == typeof(UInt64Enum))
                    return PrimitiveTypeCode.UInt64;

                throw new ArgumentOutOfRangeException();
            }
        }
    }
}
