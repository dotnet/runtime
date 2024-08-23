// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata.Tests;
using System.Reflection.PortableExecutable;
using Xunit;

namespace System.Reflection.Metadata.Decoding.Tests
{
    public class CustomAttributeDecoderTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles), nameof(PlatformDetection.IsMonoRuntime))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60579", TestPlatforms.iOS | TestPlatforms.tvOS)]
        public void TestCustomAttributeDecoder()
        {
            using (FileStream stream = File.OpenRead(AssemblyPathHelper.GetAssemblyLocation(typeof(HasAttributes).GetTypeInfo().Assembly)))
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
                            // TODO: https://github.com/dotnet/runtime/issues/73593
                            // This method only tests first 3 attriubtes because the complete test 'TestCustomAttributeDecoderUsingReflection' fails on mono
                            // Leaving this hard coded test only for mono, until the issue fixed on mono
                            break;
                    }
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/73593", TestRuntimes.Mono)]
        public void TestCustomAttributeDecoderUsingReflection()
        {
            Type type = typeof(HasAttributes);
            using (FileStream stream = File.OpenRead(AssemblyPathHelper.GetAssemblyLocation(type.GetTypeInfo().Assembly)))
            using (PEReader peReader = new PEReader(stream))
            {
                MetadataReader reader = peReader.GetMetadataReader();
                CustomAttributeTypeProvider provider = new CustomAttributeTypeProvider();
                TypeDefinitionHandle typeDefHandle = TestMetadataResolver.FindTestType(reader, type);

                IList<CustomAttributeData> attributes = type.GetCustomAttributesData();

                int i = 0;
                foreach (CustomAttributeHandle attributeHandle in reader.GetCustomAttributes(typeDefHandle))
                {
                    CustomAttribute attribute = reader.GetCustomAttribute(attributeHandle);
                    CustomAttributeValue<string> value = attribute.DecodeValue(provider);
                    CustomAttributeData reflectionAttribute = attributes[i++];

                    Assert.Equal(reflectionAttribute.ConstructorArguments.Count, value.FixedArguments.Length);
                    Assert.Equal(reflectionAttribute.NamedArguments.Count, value.NamedArguments.Length);

                    int j = 0;
                    foreach (CustomAttributeTypedArgument<string> arguments in value.FixedArguments)
                    {
                        Type t = reflectionAttribute.ConstructorArguments[j].ArgumentType;
                        Assert.Equal(TypeToString(t), arguments.Type);
                        if (t.IsArray && arguments.Value is not null)
                        {   
                            ImmutableArray<CustomAttributeTypedArgument<string>> array = (ImmutableArray<CustomAttributeTypedArgument<string>>)(arguments.Value);
                            IList<CustomAttributeTypedArgument> refArray = (IList<CustomAttributeTypedArgument>)reflectionAttribute.ConstructorArguments[j].Value;
                            int k = 0;
                            foreach (CustomAttributeTypedArgument<string> element in array)
                            {
                                if (refArray[k].ArgumentType.IsArray)
                                {
                                    ImmutableArray<CustomAttributeTypedArgument<string>> innerArray = (ImmutableArray<CustomAttributeTypedArgument<string>>)(element.Value);
                                    IList<CustomAttributeTypedArgument> refInnerArray = (IList<CustomAttributeTypedArgument>)refArray[k].Value;
                                    int a = 0;
                                    foreach (CustomAttributeTypedArgument<string> el in innerArray)
                                    {
                                        if (refInnerArray[a].Value?.ToString() != el.Value?.ToString())
                                        {
                                            Assert.Equal(refInnerArray[a].Value, el.Value);
                                        }
                                        a++;
                                    }
                                }
                                else if (refArray[k].Value?.ToString() != element.Value?.ToString())
                                {
                                    if (refArray[k].ArgumentType == typeof(Type)) // TODO: check if it is expected
                                    {
                                        Assert.Contains(refArray[k].Value.ToString(), element.Value.ToString());
                                    }
                                    else
                                    {
                                        Assert.Equal(refArray[k].Value, element.Value);
                                    }
                                }
                                k++;
                            }
                        }
                        else if (reflectionAttribute.ConstructorArguments[j].Value?.ToString() != arguments.Value?.ToString())
                        {
                            if (reflectionAttribute.ConstructorArguments[j].ArgumentType == typeof(Type))
                            {
                                Assert.Contains(reflectionAttribute.ConstructorArguments[j].Value.ToString(), arguments.Value.ToString());
                            }
                            else
                            {
                                Assert.Equal(reflectionAttribute.ConstructorArguments[j].Value, arguments.Value);
                            }
                        }
                        j++;
                    }
                    j = 0;
                    foreach (CustomAttributeNamedArgument<string> arguments in value.NamedArguments)
                    {
                        Type t = reflectionAttribute.NamedArguments[j].TypedValue.ArgumentType;
                        Assert.Equal(TypeToString(t), arguments.Type);
                        if (t.IsArray && arguments.Value is not null)
                        {
                            ImmutableArray<CustomAttributeTypedArgument<string>> array = (ImmutableArray<CustomAttributeTypedArgument<string>>)(arguments.Value);
                            IList<CustomAttributeTypedArgument> refArray = (IList<CustomAttributeTypedArgument>)reflectionAttribute.NamedArguments[j].TypedValue.Value;
                            int k = 0;
                            foreach (CustomAttributeTypedArgument<string> element in array)
                            {
                                if (refArray[k].Value?.ToString() != element.Value?.ToString())
                                {
                                    Assert.Equal(refArray[k].Value, element.Value);
                                }
                                k++;
                            }
                        }
                        else if (reflectionAttribute.NamedArguments[j].TypedValue.Value?.ToString() != arguments.Value?.ToString())
                        {
                            if (reflectionAttribute.NamedArguments[j].TypedValue.ArgumentType == typeof(Type)) // typeof operator used for named parameter, like [Test(TypeField = typeof(string))], check if it is expected
                            {
                                Assert.Contains(reflectionAttribute.NamedArguments[j].TypedValue.Value.ToString(), arguments.Value.ToString());
                            }
                            else
                            {
                                Assert.Equal(reflectionAttribute.NamedArguments[j].TypedValue.Value, arguments.Value);
                            }
                        }
                        j++;
                    }
                }
            }
        }

#if NET && !TARGET_BROWSER // Generic attribute is not supported on .NET Framework.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60579", TestPlatforms.iOS | TestPlatforms.tvOS)]
        public void TestCustomAttributeDecoderGenericUsingReflection()
        {
            Type type = typeof(HasGenericAttributes);
            using (FileStream stream = File.OpenRead(AssemblyPathHelper.GetAssemblyLocation(type.GetTypeInfo().Assembly)))
            using (PEReader peReader = new PEReader(stream))
            {
                MetadataReader reader = peReader.GetMetadataReader();
                CustomAttributeTypeProvider provider = new CustomAttributeTypeProvider();
                TypeDefinitionHandle typeDefHandle = TestMetadataResolver.FindTestType(reader, type);

                IList<CustomAttributeData> attributes= type.GetCustomAttributesData();

                int i = 0;
                foreach (CustomAttributeHandle attributeHandle in reader.GetCustomAttributes(typeDefHandle))
                {
                    CustomAttribute attribute = reader.GetCustomAttribute(attributeHandle);
                    CustomAttributeValue<string> value = attribute.DecodeValue(provider);
                    CustomAttributeData reflectionAttribute = attributes[i++];

                    Assert.Equal(reflectionAttribute.ConstructorArguments.Count, value.FixedArguments.Length);
                    Assert.Equal(reflectionAttribute.NamedArguments.Count, value.NamedArguments.Length);

                    int j = 0;
                    foreach (CustomAttributeTypedArgument<string> arguments in value.FixedArguments)
                    {
                        Assert.Equal(TypeToString(reflectionAttribute.ConstructorArguments[j].ArgumentType), arguments.Type);
                        if (reflectionAttribute.ConstructorArguments[j].Value.ToString() != arguments.Value.ToString())
                        {
                            Assert.Equal(reflectionAttribute.ConstructorArguments[j].Value, arguments.Value);
                        }
                        j++;
                    }
                    j = 0;
                    foreach (CustomAttributeNamedArgument<string> arguments in value.NamedArguments)
                    {
                        Assert.Equal(TypeToString(reflectionAttribute.NamedArguments[j].TypedValue.ArgumentType), arguments.Type);
                        if (reflectionAttribute.NamedArguments[j].TypedValue.Value.ToString() != arguments.Value.ToString())
                        {
                            Assert.Equal(reflectionAttribute.NamedArguments[j].TypedValue.Value, arguments.Value);
                        }
                        j++;
                    }
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60579", TestPlatforms.iOS | TestPlatforms.tvOS)]
        public void TestCustomAttributeDecoderGenericArray()
        {
            Type type = typeof(HasGenericArrayAttributes);
            using (FileStream stream = File.OpenRead(AssemblyPathHelper.GetAssemblyLocation(type.GetTypeInfo().Assembly)))
            using (PEReader peReader = new PEReader(stream))
            {
                MetadataReader reader = peReader.GetMetadataReader();
                CustomAttributeTypeProvider provider = new CustomAttributeTypeProvider();
                TypeDefinitionHandle typeDefHandle = TestMetadataResolver.FindTestType(reader, type);

                IList<CustomAttributeData> attributes = type.GetCustomAttributesData();

                foreach (CustomAttributeHandle attributeHandle in reader.GetCustomAttributes(typeDefHandle))
                {
                    CustomAttribute attribute = reader.GetCustomAttribute(attributeHandle);
                    CustomAttributeValue<string> value = attribute.DecodeValue(provider);

                    if (value.FixedArguments.Length == 2)
                    {
                        Assert.Equal(2, value.FixedArguments.Length);
                        ImmutableArray<CustomAttributeTypedArgument<string>> array1 = (ImmutableArray<CustomAttributeTypedArgument<string>>)(value.FixedArguments[0].Value);
                        Assert.Equal("int32[]", value.FixedArguments[0].Type);
                        Assert.Equal(1, array1[0].Value);
                        Assert.Equal(3, array1[2].Value);
                        ImmutableArray<CustomAttributeTypedArgument<string>> array2 = (ImmutableArray<CustomAttributeTypedArgument<string>>)(value.FixedArguments[1].Value);
                        Assert.Equal("uint8[]", value.FixedArguments[1].Type);
                        Assert.Equal((byte)4, array2[0].Value);
                        Assert.Equal((byte)5, array2[1].Value);

                        Assert.Empty(value.NamedArguments);
                    }
                    else
                    {
                        Assert.Equal(1, value.FixedArguments.Length);

                        Assert.Equal("uint8", value.FixedArguments[0].Type);
                        Assert.Equal((byte)1, value.FixedArguments[0].Value);

                        Assert.Equal(2, value.NamedArguments.Length);

                        Assert.Equal("uint8", value.NamedArguments[0].Type);
                        Assert.Equal((byte)2, value.NamedArguments[0].Value);

                        ImmutableArray<CustomAttributeTypedArgument<string>> array = (ImmutableArray<CustomAttributeTypedArgument<string>>)(value.NamedArguments[1].Value);
                        Assert.Equal("uint8[]", value.NamedArguments[1].Type);
                        Assert.Equal((byte)3, array[0].Value);
                    }
                }
            }
        }

        [GenericAttribute<bool>]
        [GenericAttribute<string>("Hello")]
        [GenericAttribute<int>(12)]
        [GenericAttribute<string>("Hello", 12, TProperty = "Bye")]
        [GenericAttribute<byte>(1, TProperty = 2)]
        [GenericAttribute2<bool, int>(true, 13)]
        // [GenericAttribute<MyEnum>(MyEnum.Property)] TODO: https://github.com/dotnet/runtime/issues/16552
        [GenericAttribute<Type>(typeof(HasAttributes))]
        [GenericAttribute<Type>(TProperty = typeof(HasAttributes))]
        public class HasGenericAttributes { }

        [GenericAttribute2<int[], byte[]>(new int[] { 1, 2, 3 }, new byte[] { 4, 5 })]
        [GenericAttribute<byte>(1, TProperty = 2, TArrayProperty = new byte[] { 3, 4 })]
        public class HasGenericArrayAttributes { }

        [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
        internal class GenericAttribute<T> : Attribute
        {
            public GenericAttribute() { }
            public GenericAttribute(T value)
            {
                Field = value;
            }
            public GenericAttribute(T value, int count)
            {
                Field = value;
            }
            public T TProperty { get; set; }
            public T[] TArrayProperty { get; set; }
            public T Field;
        }

        [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
        internal class GenericAttribute2<K, V> : Attribute
        {
            public GenericAttribute2() { }
            public GenericAttribute2(K key) { }
            public GenericAttribute2(K key, V value) { }
            public K Key { get; set; }
            public V Value { get; set; }
            public K[] ArrayProperty { get; set; }
        }
#endif

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
        /* [Test(SByteEnum.Value)] // The FullName is (System.Reflection.Metadata.Decoding.Tests.CustomAttributeDecoderTests+SByteEnum) 
        [Test(Int16Enum.Value)] // but some enums '+' is replaced with '/' and causing inconsistency
        [Test(Int32Enum.Value)] // Updaated https://github.com/dotnet/runtime/issues/16552 to resolve this scenario later
        [Test(Int64Enum.Value)]
        [Test(ByteEnum.Value)]
        [Test(UInt16Enum.Value)]
        [Test(UInt32Enum.Value)]
        [Test(UInt64Enum.Value)]*/
        [Test(new string[] { })]
        [Test(new string[] { "x", "y", "z", null })]
        // [Test(new Int32Enum[] { Int32Enum.Value })] TODO: https://github.com/dotnet/runtime/issues/16552

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
            typeof(string), // check if the produced value is expected
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
        // [Test(new Int32Enum[] { Int32Enum.Value })] TODO: https://github.com/dotnet/runtime/issues/16552

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

        private string TypeToString(Type type)
        {
            if (type == typeof(Type))
                return $"[{MetadataReaderTestHelpers.RuntimeAssemblyName}]System.Type";

            if (type.IsArray)
            {
                if (type.GetElementType().IsEnum)
                {
                    Type el = type.GetElementType();
                    return type.FullName;
                }
                return GetPrimitiveType(type.GetElementType()) + "[]";
            }

            if (type.IsEnum)
                return type.FullName;

            return GetPrimitiveType(type);
        }

        private static string GetPrimitiveType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return "bool";

                case TypeCode.Byte:
                    return "uint8";

                case TypeCode.Char:
                    return "char";

                case TypeCode.Double:
                    return "float64";

                case TypeCode.Int16:
                    return "int16";

                case TypeCode.Int32:
                    return "int32";

                case TypeCode.Int64:
                    return "int64";

                case TypeCode.Object:
                    return "object";

                case TypeCode.SByte:
                    return "int8";

                case TypeCode.Single:
                    return "float32";

                case TypeCode.String:
                    return "string";

                case TypeCode.UInt16:
                    return "uint16";

                case TypeCode.UInt32:
                    return "uint32";

                case TypeCode.UInt64:
                    return "uint64";

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        public enum MyEnum
        {
            Ctor,
            Property
        }

        private class CustomAttributeTypeProvider : DisassemblingTypeProvider, ICustomAttributeTypeProvider<string>
        {
            public string GetSystemType()
            {
                return $"[{MetadataReaderTestHelpers.RuntimeAssemblyName}]System.Type";
            }

            public bool IsSystemType(string type)
            {
                return type == $"[{MetadataReaderTestHelpers.RuntimeAssemblyName}]System.Type"  // encountered as typeref
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

                if (runtimeType == typeof(MyEnum))
                    return PrimitiveTypeCode.Byte;

                throw new ArgumentOutOfRangeException();
            }
        }
    }
}
