// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Linq;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Xunit;

namespace TypeSystemTests
{
    public class StaticFieldLayoutTests
    {
        TestTypeSystemContext _context;
        ModuleDesc _testModule;

        public StaticFieldLayoutTests()
        {
            _context = new TestTypeSystemContext(TargetArchitecture.X64);
            var systemModule = _context.CreateModuleForSimpleName("CoreTestAssembly");
            _context.SetSystemModule(systemModule);

            _testModule = systemModule;
        }

        [Fact]
        public void TestNoPointers()
        {
            MetadataType t = _testModule.GetType("StaticFieldLayout", "NoPointers");

            foreach (var field in t.GetFields())
            {
                if (!field.IsStatic)
                    continue;

                switch (field.Name)
                {
                    case "int1":
                        Assert.Equal(0, field.Offset.AsInt);
                        break;
                    case "byte1":
                        Assert.Equal(4, field.Offset.AsInt);
                        break;
                    case "char1":
                        Assert.Equal(6, field.Offset.AsInt);
                        break;
                    default:
                        throw new Exception(field.Name);
                }
            }
        }

        [Fact]
        public void TestStillNoPointers()
        {
            //
            // Test that static offsets ignore instance fields preceeding them
            //

            MetadataType t = _testModule.GetType("StaticFieldLayout", "StillNoPointers");

            foreach (var field in t.GetFields())
            {
                if (!field.IsStatic)
                    continue;

                switch (field.Name)
                {
                    case "bool1":
                        Assert.Equal(0, field.Offset.AsInt);
                        break;
                    default:
                        throw new Exception(field.Name);
                }
            }
        }

        [Fact]
        public void TestClassNoPointers()
        {
            //
            // Ensure classes behave the same as structs when containing statics
            //

            MetadataType t = _testModule.GetType("StaticFieldLayout", "ClassNoPointers");

            foreach (var field in t.GetFields())
            {
                if (!field.IsStatic)
                    continue;

                switch (field.Name)
                {
                    case "int1":
                        Assert.Equal(0, field.Offset.AsInt);
                        break;
                    case "byte1":
                        Assert.Equal(4, field.Offset.AsInt);
                        break;
                    case "char1":
                        Assert.Equal(6, field.Offset.AsInt);
                        break;
                    default:
                        throw new Exception(field.Name);
                }
            }
        }

        [Fact]
        public void TestHasPointers()
        {
            //
            // Test a struct containing static types with pointers
            //

            MetadataType t = _testModule.GetType("StaticFieldLayout", "HasPointers");

            foreach (var field in t.GetFields())
            {
                if (!field.IsStatic)
                    continue;

                switch (field.Name)
                {
                    case "string1":
                        Assert.Equal(8, field.Offset.AsInt);
                        break;
                    case "class1":
                        Assert.Equal(16, field.Offset.AsInt);
                        break;
                    default:
                        throw new Exception(field.Name);
                }
            }
        }

        [Fact]
        public void TestMixPointersAndNonPointers()
        {
            //
            // Test that static fields with GC pointers get separate offsets from non-GC fields
            //

            MetadataType t = _testModule.GetType("StaticFieldLayout", "MixPointersAndNonPointers");

            foreach (var field in t.GetFields())
            {
                if (!field.IsStatic)
                    continue;

                switch (field.Name)
                {
                    case "string1":
                        Assert.Equal(8, field.Offset.AsInt);
                        break;
                    case "int1":
                        Assert.Equal(0, field.Offset.AsInt);
                        break;
                    case "class1":
                        Assert.Equal(16, field.Offset.AsInt);
                        break;
                    case "int2":
                        Assert.Equal(4, field.Offset.AsInt);
                        break;
                    case "string2":
                        Assert.Equal(24, field.Offset.AsInt);
                        break;
                    default:
                        throw new Exception(field.Name);
                }
            }
        }

        [Fact]
        public void TestEnsureInheritanceResetsStaticOffsets()
        {
            //
            // Test that when inheriting a class with static fields, the derived slice's static fields
            // are again offset from 0
            //

            MetadataType t = _testModule.GetType("StaticFieldLayout", "EnsureInheritanceResetsStaticOffsets");

            foreach (var field in t.GetFields())
            {
                if (!field.IsStatic)
                    continue;

                switch (field.Name)
                {
                    case "int3":
                        Assert.Equal(0, field.Offset.AsInt);
                        break;
                    case "string3":
                        Assert.Equal(8, field.Offset.AsInt);
                        break;
                    default:
                        throw new Exception(field.Name);
                }
            }
        }

        [Fact]
        public void TestLiteralFieldsDontAffectLayout()
        {
            //
            // Test that literal fields are not laid out.
            //

            MetadataType t = _testModule.GetType("StaticFieldLayout", "LiteralFieldsDontAffectLayout");

            Assert.Equal(4, t.GetFields().Count());

            foreach (var field in t.GetFields())
            {
                if (!field.IsStatic)
                    continue;

                switch (field.Name)
                {
                    case "IntConstant":
                    case "StringConstant":
                        Assert.True(field.IsStatic);
                        Assert.True(field.IsLiteral);
                        break;
                    case "Int1":
                        Assert.Equal(0, field.Offset.AsInt);
                        break;
                    case "String1":
                        Assert.Equal(8, field.Offset.AsInt);
                        break;
                    default:
                        throw new Exception(field.Name);
                }
            }
        }

        [Fact]
        public void TestStaticSelfRef()
        {
            //
            // Test that we can load a struct which has a static field referencing itself without
            // going into an infinite loop
            //

            MetadataType t = _testModule.GetType("StaticFieldLayout", "StaticSelfRef");

            foreach (var field in t.GetFields())
            {
                if (!field.IsStatic)
                    continue;

                switch (field.Name)
                {
                    case "selfRef1":
                        Assert.Equal(0, field.Offset.AsInt);
                        break;
                    default:
                        throw new Exception(field.Name);
                }
            }
        }

        [Fact]
        public void TestRvaStatics()
        {
            //
            // Test that an RVA mapped field has the right value for the offset.
            //

            var ilModule = _context.GetModuleForSimpleName("ILTestAssembly");
            var t = ilModule.GetType("StaticFieldLayout", "RvaStatics");
            var field = t.GetField("StaticInitedInt");

            Assert.True(field.HasRva);

            byte[] rvaData = ((EcmaField)field).GetFieldRvaData();

            Assert.Equal(4, rvaData.Length);

            int value = BinaryPrimitives.ReadInt32LittleEndian(rvaData);

            Assert.Equal(0x78563412, value);
        }

        [Fact]
        public void TestFunctionPointer()
        {
            //
            // Test layout with a function pointer typed-field.
            //

            var ilModule = _context.GetModuleForSimpleName("ILTestAssembly");
            var t = ilModule.GetType("StaticFieldLayout", "FunctionPointerType");
            var field = t.GetField("StaticMethodField");

            Assert.Equal(8, field.Offset.AsInt);
            Assert.False(field.HasGCStaticBase);
        }
    }
}
