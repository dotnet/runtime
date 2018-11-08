// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using SampleMetadata;
using Xunit;

namespace System.Reflection.Tests
{
    public static partial class FieldTests
    {
        [Fact]
        public unsafe static void TestFields1()
        {
            TestField1Worker(typeof(ClassWithFields1<>).Project());
            TestField1Worker(typeof(ClassWithFields1<int>).Project());
            TestField1Worker(typeof(ClassWithFields1<string>).Project());
        }

        private static void TestField1Worker(Type t)
        {
            const BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            FieldInfo[] fields = t.GetFields(bf);

            foreach (FieldInfo f in fields)
            {
                Assert.Equal(t, f.DeclaringType);
                Assert.Equal(t, f.ReflectedType);
                Assert.Equal(t.Module, f.Module);
            }

            Type theT = t.GetGenericArguments()[0];

            {
                FieldInfo f = fields.Single(f1 => f1.Name == "ConstField1");
                Assert.Equal(FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault, f.Attributes);
                Assert.Equal(typeof(int).Project(), f.FieldType);
            }
            {
                FieldInfo f = fields.Single(f1 => f1.Name == "Field1");
                Assert.Equal(FieldAttributes.Public, f.Attributes);
                Assert.Equal(typeof(int).Project(), f.FieldType);
            }
            {
                FieldInfo f = fields.Single(f1 => f1.Name == "Field2");
                Assert.Equal(FieldAttributes.Family, f.Attributes);
                Assert.Equal(typeof(GenericClass2<int, string>).Project(), f.FieldType);
            }
            {
                FieldInfo f = fields.Single(f1 => f1.Name == "Field3");
                Assert.Equal(FieldAttributes.Private, f.Attributes);
                Assert.Equal(typeof(Interface1).Project(), f.FieldType);
            }
            {
                FieldInfo f = fields.Single(f1 => f1.Name == "Field4");
                Assert.Equal(FieldAttributes.Assembly, f.Attributes);
                Assert.Equal(typeof(int[]).Project(), f.FieldType);
            }
            {
                FieldInfo f = fields.Single(f1 => f1.Name == "Field5");
                Assert.Equal(FieldAttributes.FamORAssem, f.Attributes);
                Assert.Equal(typeof(int*).Project(), f.FieldType);
            }
            {
                FieldInfo f = fields.Single(f1 => f1.Name == "Field6");
                Assert.Equal(FieldAttributes.FamANDAssem, f.Attributes);
                Assert.Equal(theT, f.FieldType);
            }
            {
                FieldInfo f = fields.Single(f1 => f1.Name == "ReadOnlyField1");
                Assert.Equal(FieldAttributes.Private | FieldAttributes.InitOnly, f.Attributes);
                Assert.Equal(typeof(IEnumerable<int>).Project(), f.FieldType);
            }
            {
                FieldInfo f = fields.Single(f1 => f1.Name == "SField1");
                Assert.Equal(FieldAttributes.Static | FieldAttributes.Public, f.Attributes);
                Assert.Equal(typeof(byte).Project(), f.FieldType);
            }
            {
                FieldInfo f = fields.Single(f1 => f1.Name == "SField2");
                Assert.Equal(FieldAttributes.Static | FieldAttributes.Family, f.Attributes);
                Assert.Equal(typeof(GenericClass2<,>).Project().MakeGenericType(typeof(int).Project(), theT), f.FieldType);
            }
            {
                FieldInfo f = fields.Single(f1 => f1.Name == "SField3");
                Assert.Equal(FieldAttributes.Static | FieldAttributes.Private, f.Attributes);
                Assert.Equal(typeof(Interface1).Project(), f.FieldType);
            }
            {
                FieldInfo f = fields.Single(f1 => f1.Name == "SField4");
                Assert.Equal(FieldAttributes.Static | FieldAttributes.Assembly, f.Attributes);
                Assert.Equal(typeof(int[,]).Project(), f.FieldType);
            }
            {
                FieldInfo f = fields.Single(f1 => f1.Name == "SField5");
                Assert.Equal(FieldAttributes.Static | FieldAttributes.FamORAssem, f.Attributes);
                Assert.Equal(typeof(int*).Project(), f.FieldType);
            }
            {
                FieldInfo f = fields.Single(f1 => f1.Name == "SField6");
                Assert.Equal(FieldAttributes.Static | FieldAttributes.FamANDAssem, f.Attributes);
                Assert.Equal(theT, f.FieldType);
            }
            {
                FieldInfo f = fields.Single(f1 => f1.Name == "VolatileField1");
                Assert.Equal(FieldAttributes.Public, f.Attributes);
                Assert.Equal(typeof(int).Project(), f.FieldType);
            }
        }

        [Fact]
        public unsafe static void TestLiteralFields1()
        {
            Type t = typeof(ClassWithLiteralFields).Project();
            FieldInfo[] fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.NotLiteral));
                Assert.Throws<InvalidOperationException>(() => f.GetRawConstantValue());
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.NotLiteralJustReadOnly));
                Assert.Throws<InvalidOperationException>(() => f.GetRawConstantValue());
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitBool1));
                object o = f.GetRawConstantValue();
                Assert.True(o is bool);
                Assert.Equal(ClassWithLiteralFields.LitBool1, (bool)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitBool2));
                object o = f.GetRawConstantValue();
                Assert.True(o is bool);
                Assert.Equal(ClassWithLiteralFields.LitBool2, (bool)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitChar1));
                object o = f.GetRawConstantValue();
                Assert.True(o is char);
                Assert.Equal(ClassWithLiteralFields.LitChar1, (char)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitChar2));
                object o = f.GetRawConstantValue();
                Assert.True(o is char);
                Assert.Equal(ClassWithLiteralFields.LitChar2, (char)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitChar3));
                object o = f.GetRawConstantValue();
                Assert.True(o is char);
                Assert.Equal(ClassWithLiteralFields.LitChar3, (char)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitByte1));
                object o = f.GetRawConstantValue();
                Assert.True(o is byte);
                Assert.Equal(ClassWithLiteralFields.LitByte1, (byte)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitByte2));
                object o = f.GetRawConstantValue();
                Assert.True(o is byte);
                Assert.Equal(ClassWithLiteralFields.LitByte2, (byte)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitByte3));
                object o = f.GetRawConstantValue();
                Assert.True(o is byte);
                Assert.Equal(ClassWithLiteralFields.LitByte3, (byte)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitSByte1));
                object o = f.GetRawConstantValue();
                Assert.True(o is sbyte);
                Assert.Equal(ClassWithLiteralFields.LitSByte1, (sbyte)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitSByte2));
                object o = f.GetRawConstantValue();
                Assert.True(o is sbyte);
                Assert.Equal(ClassWithLiteralFields.LitSByte2, (sbyte)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitSByte3));
                object o = f.GetRawConstantValue();
                Assert.True(o is sbyte);
                Assert.Equal(ClassWithLiteralFields.LitSByte3, (sbyte)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitShort1));
                object o = f.GetRawConstantValue();
                Assert.True(o is short);
                Assert.Equal(ClassWithLiteralFields.LitShort1, (short)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitShort2));
                object o = f.GetRawConstantValue();
                Assert.True(o is short);
                Assert.Equal(ClassWithLiteralFields.LitShort2, (short)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitShort3));
                object o = f.GetRawConstantValue();
                Assert.True(o is short);
                Assert.Equal(ClassWithLiteralFields.LitShort3, (short)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitUShort1));
                object o = f.GetRawConstantValue();
                Assert.True(o is ushort);
                Assert.Equal(ClassWithLiteralFields.LitUShort1, (ushort)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitUShort2));
                object o = f.GetRawConstantValue();
                Assert.True(o is ushort);
                Assert.Equal(ClassWithLiteralFields.LitUShort2, (ushort)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitUShort3));
                object o = f.GetRawConstantValue();
                Assert.True(o is ushort);
                Assert.Equal(ClassWithLiteralFields.LitUShort3, (ushort)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitInt1));
                object o = f.GetRawConstantValue();
                Assert.True(o is int);
                Assert.Equal(ClassWithLiteralFields.LitInt1, (int)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitInt2));
                object o = f.GetRawConstantValue();
                Assert.True(o is int);
                Assert.Equal(ClassWithLiteralFields.LitInt2, (int)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitInt3));
                object o = f.GetRawConstantValue();
                Assert.True(o is int);
                Assert.Equal(ClassWithLiteralFields.LitInt3, (int)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitUInt1));
                object o = f.GetRawConstantValue();
                Assert.True(o is uint);
                Assert.Equal(ClassWithLiteralFields.LitUInt1, (uint)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitUInt2));
                object o = f.GetRawConstantValue();
                Assert.True(o is uint);
                Assert.Equal(ClassWithLiteralFields.LitUInt2, (uint)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitUInt3));
                object o = f.GetRawConstantValue();
                Assert.True(o is uint);
                Assert.Equal(ClassWithLiteralFields.LitUInt3, (uint)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitLong1));
                object o = f.GetRawConstantValue();
                Assert.True(o is long);
                Assert.Equal(ClassWithLiteralFields.LitLong1, (long)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitLong2));
                object o = f.GetRawConstantValue();
                Assert.True(o is long);
                Assert.Equal(ClassWithLiteralFields.LitLong2, (long)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitLong3));
                object o = f.GetRawConstantValue();
                Assert.True(o is long);
                Assert.Equal(ClassWithLiteralFields.LitLong3, (long)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitULong1));
                object o = f.GetRawConstantValue();
                Assert.True(o is ulong);
                Assert.Equal(ClassWithLiteralFields.LitULong1, (ulong)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitULong2));
                object o = f.GetRawConstantValue();
                Assert.True(o is ulong);
                Assert.Equal(ClassWithLiteralFields.LitULong2, (ulong)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitULong3));
                object o = f.GetRawConstantValue();
                Assert.True(o is ulong);
                Assert.Equal(ClassWithLiteralFields.LitULong3, (ulong)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitSingle1));
                object o = f.GetRawConstantValue();
                Assert.True(o is float);
                Assert.Equal(ClassWithLiteralFields.LitSingle1, (float)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitSingle2));
                object o = f.GetRawConstantValue();
                Assert.True(o is float);
                Assert.Equal(ClassWithLiteralFields.LitSingle2, (float)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitDouble1));
                object o = f.GetRawConstantValue();
                Assert.True(o is double);
                Assert.Equal(ClassWithLiteralFields.LitDouble1, (double)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitDouble2));
                object o = f.GetRawConstantValue();
                Assert.True(o is double);
                Assert.Equal(ClassWithLiteralFields.LitDouble2, (double)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitString1));
                object o = f.GetRawConstantValue();
                Assert.True(o is string);
                Assert.Equal(ClassWithLiteralFields.LitString1, (string)o);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitString2));
                object o = f.GetRawConstantValue();
                Assert.True(o == null);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitObject));
                object o = f.GetRawConstantValue();
                Assert.True(o == null);
            }

            {
                FieldInfo f = fields.Single(f1 => f1.Name == nameof(ClassWithLiteralFields.LitMyColor1));
                object o = f.GetRawConstantValue();
                Assert.True(o is int);
                Assert.Equal((int)(ClassWithLiteralFields.LitMyColor1), (int)o);
            }
        }

        [Fact]
        public unsafe static void TestCustomModifiers1()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(
                new FuncMetadataAssemblyResolver(
                    delegate (MetadataLoadContext context, AssemblyName name)
                    {
                        if (name.Name == "mscorlib")
                            return context.LoadFromStream(TestUtils.CreateStreamForCoreAssembly());
                        return null;
                    }),
                    "mscorlib"))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_CustomModifiersImage);
                Type t = a.GetType("N", throwOnError: true);
                Type reqA = a.GetType("ReqA", throwOnError: true);
                Type reqB = a.GetType("ReqB", throwOnError: true);
                Type reqC = a.GetType("ReqC", throwOnError: true);
                Type optA = a.GetType("OptA", throwOnError: true);
                Type optB = a.GetType("OptB", throwOnError: true);
                Type optC = a.GetType("OptC", throwOnError: true);

                FieldInfo f = t.GetField("MyField");
                Type[] req = f.GetRequiredCustomModifiers();
                Type[] opt = f.GetOptionalCustomModifiers();

                Assert.Equal<Type>(new Type[] { reqA, reqB, reqC }, req);
                Assert.Equal<Type>(new Type[] { optA, optB, optC }, opt);

                TestUtils.AssertNewObjectReturnedEachTime(() => f.GetRequiredCustomModifiers());
                TestUtils.AssertNewObjectReturnedEachTime(() => f.GetOptionalCustomModifiers());
            }
        }

        [Fact]
        public unsafe static void TestCustomModifiers2()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new CoreMetadataAssemblyResolver(), "mscorlib"))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_CustomModifiersImage);
                Type t = a.GetType("N", throwOnError: true);
                Type reqA = a.GetType("ReqA", throwOnError: true);
                Type reqB = a.GetType("ReqB", throwOnError: true);
                Type reqC = a.GetType("ReqC", throwOnError: true);
                Type optA = a.GetType("OptA", throwOnError: true);
                Type optB = a.GetType("OptB", throwOnError: true);
                Type optC = a.GetType("OptC", throwOnError: true);

                FieldInfo f = t.GetField("MyArrayField");
                Type[] req = f.GetRequiredCustomModifiers();
                Type[] opt = f.GetOptionalCustomModifiers();

                Assert.Equal<Type>(new Type[] { reqB }, req);
                Assert.Equal<Type>(new Type[] { }, opt);

                TestUtils.AssertNewObjectReturnedEachTime(() => f.GetRequiredCustomModifiers());
                TestUtils.AssertNewObjectReturnedEachTime(() => f.GetOptionalCustomModifiers());
            }
        }
    }
}
