// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.TypeSystem;

using Xunit;

namespace TypeSystemTests
{
    public class InstanceFieldLayoutTests
    {
        TestTypeSystemContext _context;
        ModuleDesc _testModule;
        ModuleDesc _ilTestModule;

        public InstanceFieldLayoutTests()
        {
            _context = new TestTypeSystemContext(TargetArchitecture.X64);
            var systemModule = _context.CreateModuleForSimpleName("CoreTestAssembly");
            _context.SetSystemModule(systemModule);

            _testModule = systemModule;
            _ilTestModule = _context.CreateModuleForSimpleName("ILTestAssembly");
        }

        [Fact]
        public void TestExplicitLayout()
        {
            MetadataType t = _testModule.GetType("Explicit", "Class1");

            // With 64bit, there should be 8 bytes for the System.Object EE data pointer +
            // 10 bytes up until the offset of the char field + the char size of 2 + we 
            // round up the whole instance size to the next pointer size (+4) = 24
            Assert.Equal(24, t.InstanceByteCount.AsInt);

            foreach (var field in t.GetFields())
            {
                if (field.IsStatic)
                    continue;

                if (field.Name == "Bar")
                {
                    // Bar has explicit offset 4 and is in a class (with S.O size overhead of <pointer size>)
                    // Therefore it should have offset 4 + 8 = 12
                  Assert.Equal(12, field.Offset.AsInt);
                }
                else if (field.Name == "Baz")
                {
                    // Baz has explicit offset 10. 10 + 8 = 18
                    Assert.Equal(18, field.Offset.AsInt);
                }
                else
                {
                    Assert.True(false);
                }
            }
        }

        [Fact]
        public void TestExplicitLayoutThatIsEmpty()
        {
            var explicitEmptyClassType = _testModule.GetType("Explicit", "ExplicitEmptyClass");

            // ExplicitEmpty class has 8 from System.Object overhead = 8
            Assert.Equal(8, explicitEmptyClassType.InstanceByteCount.AsInt);

            var explicitEmptyStructType = _testModule.GetType("Explicit", "ExplicitEmptyStruct");

            // ExplicitEmpty class has 0 bytes in it... so instance field size gets pushed up to 1.
            Assert.Equal(1, explicitEmptyStructType.InstanceFieldSize.AsInt);
        }

        [Fact]
        public void TestExplicitTypeLayoutWithSize()
        {
            var explicitSizeType = _testModule.GetType("Explicit", "ExplicitSize");
            Assert.Equal(48, explicitSizeType.InstanceByteCount.AsInt);
        }

        [Fact]
        public void TestExplicitTypeLayoutWithInheritance()
        {
            MetadataType class2Type = _testModule.GetType("Explicit", "Class2");

            // Class1 has size 24 which Class2 inherits from.  Class2 adds a byte at offset 20, so + 21
            // = 45, rounding up to the next pointer size = 48
            Assert.Equal(48, class2Type.InstanceByteCount.AsInt);

            foreach (var f in class2Type.GetFields())
            {
                if (f.IsStatic)
                    continue;

                if (f.Name == "Lol")
                {
                    // First field after base class, with offset 0 so it should lie on the byte count of 
                    // the base class = 20
                    Assert.Equal(20, f.Offset.AsInt);
                }
                else if (f.Name == "Omg")
                {
                    // Offset 20 from base class byte count = 40
                    Assert.Equal(40, f.Offset.AsInt);
                }
                else
                {
                    Assert.True(false);
                }
            }
        }

        [Fact]
        public void TestInvalidExplicitTypeLayout()
        {
            {
                DefType type = _testModule.GetType("Explicit", "MisalignedPointer");
                Assert.Throws<TypeSystemException.TypeLoadException>(() => type.ComputeInstanceLayout(InstanceLayoutKind.TypeAndFields));
            }

            {
                DefType type = _testModule.GetType("Explicit", "MisalignedByRef");
                Assert.Throws<TypeSystemException.TypeLoadException>(() => type.ComputeInstanceLayout(InstanceLayoutKind.TypeAndFields));
            }
        }

        [Fact]
        public void TestSequentialTypeLayout()
        {
            MetadataType class1Type = _testModule.GetType("Sequential", "Class1");

            // Byte count
            // Base Class       8
            // MyInt            4
            // MyBool           1 + 1 padding
            // MyChar           2
            // MyString         8
            // MyByteArray      8
            // MyClass1SelfRef  8
            // -------------------
            //                  40 (0x28)
            Assert.Equal(0x28, class1Type.InstanceByteCount.AsInt);

            foreach (var f in class1Type.GetFields())
            {
                if (f.IsStatic)
                    continue;

                switch (f.Name)
                {
                    case "MyInt":
                        Assert.Equal(0x8, f.Offset.AsInt);
                        break;
                    case "MyBool":
                        Assert.Equal(0xC, f.Offset.AsInt);
                        break;
                    case "MyChar":
                        Assert.Equal(0xE, f.Offset.AsInt);
                        break;
                    case "MyString":
                        Assert.Equal(0x10, f.Offset.AsInt);
                        break;
                    case "MyByteArray":
                        Assert.Equal(0x18, f.Offset.AsInt);
                        break;
                    case "MyClass1SelfRef":
                        Assert.Equal(0x20, f.Offset.AsInt);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void TestSequentialTypeLayoutInheritance()
        {
            MetadataType class2Type = _testModule.GetType("Sequential", "Class2");

            // Byte count
            // Base Class       40
            // MyInt2           4 + 4 byte padding to make class size % pointer size == 0
            // -------------------
            //                  48 (0x30)
            Assert.Equal(0x30, class2Type.InstanceByteCount.AsInt);

            foreach (var f in class2Type.GetFields())
            {
                if (f.IsStatic)
                    continue;

                switch (f.Name)
                {
                    case "MyInt2":
                        Assert.Equal(0x28, f.Offset.AsInt);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void TestSequentialTypeLayoutStruct()
        {
            MetadataType struct0Type = _testModule.GetType("Sequential", "Struct0");

            // Byte count
            // bool     b1      1
            // bool     b2      1
            // bool     b3      1 + 1 padding for int alignment
            // int      i1      4
            // string   s1      8
            // -------------------
            //                  16 (0x10)
            Assert.Equal(0x10, struct0Type.InstanceByteCount.AsInt);

            foreach (var f in struct0Type.GetFields())
            {
                if (f.IsStatic)
                    continue;

                switch (f.Name)
                {
                    case "b1":
                        Assert.Equal(0x0, f.Offset.AsInt);
                        break;
                    case "b2":
                        Assert.Equal(0x1, f.Offset.AsInt);
                        break;
                    case "b3":
                        Assert.Equal(0x2, f.Offset.AsInt);
                        break;
                    case "i1":
                        Assert.Equal(0x4, f.Offset.AsInt);
                        break;
                    case "s1":
                        Assert.Equal(0x8, f.Offset.AsInt);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        // Test that when a struct is used as a field, we use its instance byte size as the size (ie, treat it
        // as a value type) and not a pointer size.
        public void TestSequentialTypeLayoutStructEmbedded()
        {
            MetadataType struct1Type = _testModule.GetType("Sequential", "Struct1");

            // Byte count
            // struct   MyStruct0   16
            // bool     MyBool      1
            // -----------------------
            //                      24 (0x18)
            Assert.Equal(0x18, struct1Type.InstanceByteCount.AsInt);

            foreach (var f in struct1Type.GetFields())
            {
                if (f.IsStatic)
                    continue;

                switch (f.Name)
                {
                    case "MyStruct0":
                        Assert.Equal(0x0, f.Offset.AsInt);
                        break;
                    case "MyBool":
                        Assert.Equal(0x10, f.Offset.AsInt);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void TestAutoLayoutStruct()
        {
            MetadataType structWithIntCharType = _testModule.GetType("Auto", "StructWithIntChar");

            // Byte count
            // MyStructInt       4
            // MyStructChar      2
            // -------------------
            //                   8 (0x08)
            Assert.Equal(0x08, structWithIntCharType.InstanceByteCount.AsInt);

            foreach (var f in structWithIntCharType.GetFields())
            {
                if (f.IsStatic)
                    continue;

                switch (f.Name)
                {
                    case "MyStructInt":
                        Assert.Equal(0x00, f.Offset.AsInt);
                        break;
                    case "MyStructChar":
                        Assert.Equal(0x04, f.Offset.AsInt);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void TestAutoTypeLayoutClassContainingStructs()
        {
            MetadataType classContainingStructsType = _testModule.GetType("Auto", "ClassContainingStructs");

            // Byte count
            // Base Class           8
            // MyByteArray          8
            // MyString1            8
            // MyDouble             8
            // MyLong               8
            // MyInt                4
            // MyChar1              2
            // MyBool1              1
            // MyBool2              1
            // MyStructWithBool     1 + 3 to align up to the next multiple of 4 after placing a value class
            //                      4 byte padding to make offset % pointer size == 0 before placing the next value class
            // MyStructWithIntChar  6 + 2 to align up to the next multiple of 4 after placing a value class
            // MyStructWithChar     2 + 2 to align up to the next multiple of 4 after placing a value class + 4 byte padding to make class size % pointer size == 0
            // -------------------
            //                  72 (0x48)
            Assert.Equal(0x48, classContainingStructsType.InstanceByteCount.AsInt);

            foreach (var f in classContainingStructsType.GetFields())
            {
                if (f.IsStatic)
                    continue;

                switch (f.Name)
                {
                    case "MyByteArray":
                        Assert.Equal(0x08, f.Offset.AsInt);
                        break;
                    case "MyString1":
                        Assert.Equal(0x10, f.Offset.AsInt);
                        break;
                    case "MyDouble":
                        Assert.Equal(0x18, f.Offset.AsInt);
                        break;
                    case "MyLong":
                        Assert.Equal(0x20, f.Offset.AsInt);
                        break;
                    case "MyInt":
                        Assert.Equal(0x28, f.Offset.AsInt);
                        break;
                    case "MyChar1":
                        Assert.Equal(0x2C, f.Offset.AsInt);
                        break;
                    case "MyBool1":
                        Assert.Equal(0x2E, f.Offset.AsInt);
                        break;
                    case "MyBool2":
                        Assert.Equal(0x2F, f.Offset.AsInt);
                        break;
                    case "MyStructWithBool":
                        Assert.Equal(0x30, f.Offset.AsInt);
                        break;
                    case "MyStructWithIntChar":
                        Assert.Equal(0x38, f.Offset.AsInt);
                        break;
                    case "MyStructWithChar":
                        Assert.Equal(0x40, f.Offset.AsInt);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void TestAutoTypeLayoutBaseClass7BytesRemaining()
        {
            MetadataType baseClass7BytesRemainingType = _testModule.GetType("Auto", "BaseClass7BytesRemaining");

            // Byte count
            // Base Class       8
            // MyByteArray1     8
            // MyString1        8
            // MyDouble1        8
            // MyLong1          8
            // MyBool1          1 + 7 byte padding to make class size % pointer size == 0
            // -------------------
            //                  48 (0x30)
            Assert.Equal(0x30, baseClass7BytesRemainingType.InstanceByteCount.AsInt);

            foreach (var f in baseClass7BytesRemainingType.GetFields())
            {
                if (f.IsStatic)
                    continue;

                switch (f.Name)
                {
                    case "MyByteArray1":
                        Assert.Equal(0x08, f.Offset.AsInt);
                        break;
                    case "MyString1":
                        Assert.Equal(0x10, f.Offset.AsInt);
                        break;
                    case "MyDouble1":
                        Assert.Equal(0x18, f.Offset.AsInt);
                        break;
                    case "MyLong1":
                        Assert.Equal(0x20, f.Offset.AsInt);
                        break;
                    case "MyBool1":
                        Assert.Equal(0x28, f.Offset.AsInt);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void TestAutoTypeLayoutBaseClass4BytesRemaining()
        {
            MetadataType baseClass4BytesRemainingType = _testModule.GetType("Auto", "BaseClass4BytesRemaining");

            // Byte count
            // Base Class       8
            // MyLong1          8
            // MyUint1          4 + 4 byte padding to make class size % pointer size == 0
            // -------------------
            //                  24 (0x18)
            Assert.Equal(0x18, baseClass4BytesRemainingType.InstanceByteCount.AsInt);

            foreach (var f in baseClass4BytesRemainingType.GetFields())
            {
                if (f.IsStatic)
                    continue;

                switch (f.Name)
                {
                    case "MyLong1":
                        Assert.Equal(0x08, f.Offset.AsInt);
                        break;
                    case "MyUint1":
                        Assert.Equal(0x10, f.Offset.AsInt);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void TestAutoTypeLayoutBaseClass3BytesRemaining()
        {
            MetadataType baseClass3BytesRemainingType = _testModule.GetType("Auto", "BaseClass3BytesRemaining");

            // Byte count
            // Base Class       8
            // MyString1        8
            // MyInt1           4
            // MyBool1          1 + 3 byte padding to make class size % pointer size == 0
            // -------------------
            //                  24 (0x18)
            Assert.Equal(0x18, baseClass3BytesRemainingType.InstanceByteCount.AsInt);

            foreach (var f in baseClass3BytesRemainingType.GetFields())
            {
                if (f.IsStatic)
                    continue;

                switch (f.Name)
                {
                    case "MyString1":
                        Assert.Equal(0x08, f.Offset.AsInt);
                        break;
                    case "MyInt1":
                        Assert.Equal(0x10, f.Offset.AsInt);
                        break;
                    case "MyBool1":
                        Assert.Equal(0x14, f.Offset.AsInt);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void TestAutoTypeLayoutOptimizePartial()
        {
            MetadataType optimizePartialType = _testModule.GetType("Auto", "OptimizePartial");

            // Byte count
            // Base Class       41 (unaligned)
            // OptBool          1
            // OptChar          2 + 4 byte padding to make class size % pointer size == 0
            // NoOptString      8
            // NoOptLong        8
            // -------------------
            //                  64 (0x40)
            Assert.Equal(0x40, optimizePartialType.InstanceByteCount.AsInt);

            foreach (var f in optimizePartialType.GetFields())
            {
                if (f.IsStatic)
                    continue;

                switch (f.Name)
                {
                    case "OptBool":
                        Assert.Equal(0x29, f.Offset.AsInt);
                        break;
                    case "OptChar":
                        Assert.Equal(0x2A, f.Offset.AsInt);
                        break;
                    case "NoOptString":
                        Assert.Equal(0x30, f.Offset.AsInt);
                        break;
                    case "NoOptLong":
                        Assert.Equal(0x38, f.Offset.AsInt);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void TestAutoTypeLayoutOptimize7Bools()
        {
            MetadataType optimize7BoolsType = _testModule.GetType("Auto", "Optimize7Bools");

            // Byte count
            // Base Class       41 (unaligned)
            // OptBool1         1
            // OptBool2         1
            // OptBool3         1
            // OptBool4         1
            // OptBool5         1
            // OptBool6         1
            // OptBool7         1
            // NoOptString      8
            // NoOptBool8       1 + 7 byte padding to make class size % pointer size == 0
            // -------------------
            //                  64 (0x40)
            Assert.Equal(0x40, optimize7BoolsType.InstanceByteCount.AsInt);

            foreach (var f in optimize7BoolsType.GetFields())
            {
                if (f.IsStatic)
                    continue;

                switch (f.Name)
                {
                    case "OptBool1":
                        Assert.Equal(0x29, f.Offset.AsInt);
                        break;
                    case "OptBool2":
                        Assert.Equal(0x2A, f.Offset.AsInt);
                        break;
                    case "OptBool3":
                        Assert.Equal(0x2B, f.Offset.AsInt);
                        break;
                    case "OptBool4":
                        Assert.Equal(0x2C, f.Offset.AsInt);
                        break;
                    case "OptBool5":
                        Assert.Equal(0x2D, f.Offset.AsInt);
                        break;
                    case "OptBool6":
                        Assert.Equal(0x2E, f.Offset.AsInt);
                        break;
                    case "OptBool7":
                        Assert.Equal(0x2F, f.Offset.AsInt);
                        break;
                    case "NoOptString":
                        Assert.Equal(0x30, f.Offset.AsInt);
                        break;
                    case "NoOptBool8":
                        Assert.Equal(0x38, f.Offset.AsInt);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void TestAutoTypeLayoutOptimizeAlignedFields()
        {
            MetadataType optimizeAlignedFieldsType = _testModule.GetType("Auto", "OptimizeAlignedFields");

            // Byte count
            // Base Class       41 (unaligned)
            // OptBool1         1
            // OptChar1         2
            // OptChar2         2
            // OptBool2         1
            // OptBool3         1
            // NoOptString      8
            // NoOptBool4       1 + 7 byte padding to make class size % pointer size == 0
            // -------------------
            //                  64 (0x40)
            Assert.Equal(0x40, optimizeAlignedFieldsType.InstanceByteCount.AsInt);

            foreach (var f in optimizeAlignedFieldsType.GetFields())
            {
                if (f.IsStatic)
                    continue;

                switch (f.Name)
                {
                    case "OptBool1":
                        Assert.Equal(0x29, f.Offset.AsInt);
                        break;
                    case "OptChar1":
                        Assert.Equal(0x2A, f.Offset.AsInt);
                        break;
                    case "OptChar2":
                        Assert.Equal(0x2C, f.Offset.AsInt);
                        break;
                    case "OptBool2":
                        Assert.Equal(0x2E, f.Offset.AsInt);
                        break;
                    case "OptBool3":
                        Assert.Equal(0x2F, f.Offset.AsInt);
                        break;
                    case "NoOptString":
                        Assert.Equal(0x30, f.Offset.AsInt);
                        break;
                    case "NoOptBool4":
                        Assert.Equal(0x38, f.Offset.AsInt);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void TestAutoTypeLayoutOptimizeLargestField()
        {
            MetadataType optimizeLargestFieldType = _testModule.GetType("Auto", "OptimizeLargestField");

            // Byte count
            // Base Class       20 (unaligned)
            // OptInt           4
            // NoOptString      8
            // NoOptChar        2
            // NoOptBool        1 + 5 byte padding to make class size % pointer size == 0
            // -------------------
            //                  40 (0x28)
            Assert.Equal(0x28, optimizeLargestFieldType.InstanceByteCount.AsInt);

            foreach (var f in optimizeLargestFieldType.GetFields())
            {
                if (f.IsStatic)
                    continue;

                switch (f.Name)
                {
                    case "OptInt":
                        Assert.Equal(0x14, f.Offset.AsInt);
                        break;
                    case "NoOptString":
                        Assert.Equal(0x18, f.Offset.AsInt);
                        break;
                    case "NoOptChar":
                        Assert.Equal(0x20, f.Offset.AsInt);
                        break;
                    case "NoOptBool":
                        Assert.Equal(0x22, f.Offset.AsInt);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void TestAutoTypeLayoutNoOptimizeMisaligned()
        {
            MetadataType noOptimizeMisalignedType = _testModule.GetType("Auto", "NoOptimizeMisaligned");

            // Byte count
            // Base Class       21 (unaligned) + 3 byte padding to make class size % pointer size == 0
            // NoOptString      8
            // NoOptInt         4
            // NoOptChar        2 + 2 byte padding to make class size % pointer size == 0
            // -------------------
            //                  40 (0x28)
            Assert.Equal(0x28, noOptimizeMisalignedType.InstanceByteCount.AsInt);

            foreach (var f in noOptimizeMisalignedType.GetFields())
            {
                if (f.IsStatic)
                    continue;

                switch (f.Name)
                {
                    case "NoOptString":
                        Assert.Equal(0x18, f.Offset.AsInt);
                        break;
                    case "NoOptInt":
                        Assert.Equal(0x20, f.Offset.AsInt);
                        break;
                    case "NoOptChar":
                        Assert.Equal(0x24, f.Offset.AsInt);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void TestAutoTypeLayoutNoOptimizeCharAtSize2Alignment()
        {
            MetadataType noOptimizeCharAtSize2AlignmentType = _testModule.GetType("Auto", "NoOptimizeCharAtSize2Alignment");

            // Byte count
            // Base Class       21 (unaligned) + 1 byte padding to align char
            // NoOptChar        2
            // -------------------
            //                  24 (0x18)
            Assert.Equal(0x18, noOptimizeCharAtSize2AlignmentType.InstanceByteCount.AsInt);

            foreach (var f in noOptimizeCharAtSize2AlignmentType.GetFields())
            {
                if (f.IsStatic)
                    continue;

                switch (f.Name)
                {
                    case "NoOptChar":
                        Assert.Equal(0x16, f.Offset.AsInt);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        public static IEnumerable<object[]> AutoTypeLayoutMinPackingData()
        {
            yield return new object[] { WellKnownType.Boolean, 2 };
            yield return new object[] { WellKnownType.Byte, 2 };
            yield return new object[] { WellKnownType.Char, 4 };
            yield return new object[] { WellKnownType.Double, 16 };
            yield return new object[] { WellKnownType.Int16, 4 };
            yield return new object[] { WellKnownType.Int32, 8 };
            yield return new object[] { WellKnownType.Int64, 16 };
            yield return new object[] { WellKnownType.IntPtr, 16 };
            yield return new object[] { WellKnownType.Single, 8 };
        }

        [Theory]
        [MemberData(nameof(AutoTypeLayoutMinPackingData))]
        public void TestAutoTypeLayoutMinPacking(WellKnownType type, int expectedSize)
        {
            MetadataType minPackingType = _testModule.GetType("Auto", "MinPacking`1");
            InstantiatedType inst = minPackingType.MakeInstantiatedType(_context.GetWellKnownType(type));
            Assert.Equal(expectedSize, inst.InstanceFieldSize.AsInt);
        }

        [Fact]
        public void TestTypeContainsGCPointers()
        {
            MetadataType type = _testModule.GetType("ContainsGCPointers", "NoPointers");
            Assert.False(type.ContainsGCPointers);

            type = _testModule.GetType("ContainsGCPointers", "StillNoPointers");
            Assert.False(type.ContainsGCPointers);

            type = _testModule.GetType("ContainsGCPointers", "ClassNoPointers");
            Assert.False(type.ContainsGCPointers);

            type = _testModule.GetType("ContainsGCPointers", "HasPointers");
            Assert.True(type.ContainsGCPointers);

            type = _testModule.GetType("ContainsGCPointers", "FieldHasPointers");
            Assert.True(type.ContainsGCPointers);

            type = _testModule.GetType("ContainsGCPointers", "ClassHasPointers");
            Assert.True(type.ContainsGCPointers);

            type = _testModule.GetType("ContainsGCPointers", "BaseClassHasPointers");
            Assert.True(type.ContainsGCPointers);

            type = _testModule.GetType("ContainsGCPointers", "ClassHasIntArray");
            Assert.True(type.ContainsGCPointers);

            type = _testModule.GetType("ContainsGCPointers", "ClassHasArrayOfClassType");
            Assert.True(type.ContainsGCPointers);
        }

        [Fact]
        public void TestByRefLikeTypes()
        {
            {
                DefType type = _context.GetWellKnownType(WellKnownType.TypedReference);
                Assert.True(type.IsByRefLike);
            }

            {
                DefType type = _context.GetWellKnownType(WellKnownType.ByReferenceOfT);
                Assert.True(type.IsByRefLike);
            }

            {
                DefType type = _testModule.GetType("IsByRefLike", "ByRefLikeStruct");
                Assert.True(type.IsByRefLike);
            }

            {
                DefType type = _testModule.GetType("IsByRefLike", "NotByRefLike");
                Assert.False(type.IsByRefLike);
            }
        }

        [Fact]
        public void TestInvalidByRefLikeTypes()
        {
            {
                DefType type = _ilTestModule.GetType("IsByRefLike", "InvalidClass1");
                Assert.Throws<TypeSystemException.TypeLoadException>(() => type.ComputeInstanceLayout(InstanceLayoutKind.TypeAndFields));
            }

            {
                DefType type = _ilTestModule.GetType("IsByRefLike", "InvalidClass2");
                Assert.Throws<TypeSystemException.TypeLoadException>(() => type.ComputeInstanceLayout(InstanceLayoutKind.TypeAndFields));
            }

            {
                DefType type = _ilTestModule.GetType("IsByRefLike", "InvalidStruct");
                Assert.Throws<TypeSystemException.TypeLoadException>(() => type.ComputeInstanceLayout(InstanceLayoutKind.TypeAndFields));
            }
        }
    }
}
