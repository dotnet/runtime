// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Xunit;
using static Microsoft.Diagnostics.DataContractReader.Tests.TestHelpers;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests for IXCLRDataValue methods (GetSize, GetFlags,
/// GetBytes, GetNumLocations, GetLocationByIndex) on values returned by
/// ClrDataFrame.GetArgumentByIndex and GetLocalVariableByIndex.
/// </summary>
public unsafe class IXCLRDataValueDumpTests : DumpTestBase
{
    protected override string DebuggeeName => "LocalVariables";

    // ========== GetSize ==========

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetSize_ReturnsExpectedSizes(TestConfiguration config)
    {
        InitializeDumpTest(config);
        int pointerSize = Target.PointerSize;

        // --- Primitives: size matches the ECMA-335 element type ---
        // PrimitiveVars(int=42, double=3.14, bool=true, char='Z', byte=0xFF, short=-1, long=123456789, float=2.5)
        var primitiveArgs = GetArgumentValues("PrimitiveVars");
        AssertEach(primitiveArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["intArg"] = (v, d) => AssertSize(v, 4, d),
            ["doubleArg"] = (v, d) => AssertSize(v, 8, d),
            ["boolArg"] = (v, d) => AssertSize(v, 1, d),
            ["charArg"] = (v, d) => AssertSize(v, 2, d),
            ["byteArg"] = (v, d) => AssertSize(v, 1, d),
            ["shortArg"] = (v, d) => AssertSize(v, 2, d),
            ["longArg"] = (v, d) => AssertSize(v, 8, d),
            ["floatArg"] = (v, d) => AssertSize(v, 4, d),
        });

        // --- Native ints: pointer-sized primitives ---
        // NativeIntVars(nint=0x1234, nuint=0x5678)
        var nativeIntArgs = GetArgumentValues("NativeIntVars");
        AssertEach(nativeIntArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["nintArg"] = (v, d) => AssertSize(v, pointerSize, d),
            ["nuintArg"] = (v, d) => AssertSize(v, pointerSize, d),
        });

        // --- Generics: method-level (MVAR) and class-level (VAR) resolve to actual type ---
        // MethodGenericVars<int, string>(300, "generic")
        // arg1 is !!0 → int → 4 bytes; arg2 is !!1 → string → pointer-sized
        var methodGenericArgs = GetArgumentValues("MethodGenericVars");
        AssertEach(methodGenericArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["arg1"] = (v, d) => AssertSize(v, 4, d),
            ["arg2"] = (v, d) => AssertSize(v, pointerSize, d),
        });

        // GenericContainer<int>.ClassGenericVars(int value)
        // 'this' is IS_REFERENCE → pointer-sized; 'value' is !0 → int → 4 bytes
        var classGenericArgs = GetArgumentValues("ClassGenericVars");
        AssertEach(classGenericArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["this"] = (v, d) => AssertSize(v, pointerSize, d),
            ["value"] = (v, d) => AssertSize(v, 4, d),
        });

        // --- ByRef and GenericInst: pointer-sized ---
        // GenericInstAndByRefVars(List<int> listArg, KeyValuePair<int, string> kvpArg, ref int refArg)
        var byRefArgs = GetArgumentValues("GenericInstAndByRefVars");
        AssertEach(byRefArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["listArg"] = (v, d) => AssertSize(v, pointerSize, d),
            ["refArg"] = (v, d) => AssertSize(v, pointerSize, d),
        });

        // --- Structs: always pointer-sized (JIT storage size, not logical type size) ---
        // StructVars(TinyStruct tinyArg, SmallStruct smallArg, LargeStruct largeArg)
        // NativeVarLocations sets size=sizeof(SIZE_T) for all location types.
        // The native DAC only adjusts size downward for primitives, never for value types.
        var structArgs = GetArgumentValues("StructVars");
        foreach (var (name, value) in structArgs)
        {
            AssertSize(value, pointerSize, name);
        }

        // --- References, enum, object, array: all pointer-sized ---
        // ReferenceTypeVars(string stringArg, SimpleClass classArg, Color enumArg, object boxedArg, int[] arrayArg)
        var refArgs = GetArgumentValues("ReferenceTypeVars");
        AssertEach(refArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["stringArg"] = (v, d) => AssertSize(v, pointerSize, d),
            ["classArg"] = (v, d) => AssertSize(v, pointerSize, d),
            ["enumArg"] = (v, d) => AssertSize(v, pointerSize, d),
            ["boxedArg"] = (v, d) => AssertSize(v, pointerSize, d),
            ["arrayArg"] = (v, d) => AssertSize(v, pointerSize, d),
        });

        // --- GenericInst and ByRef: pointer-sized ---
        // GenericInstAndByRefVars(List<int> listArg, KeyValuePair<int,string> kvpArg, ref int refArg)
        var genericInstArgs = GetArgumentValues("GenericInstAndByRefVars");
        AssertEach(genericInstArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["listArg"] = (v, d) => AssertSize(v, pointerSize, d),
            ["kvpArg"] = (v, d) => AssertSize(v, pointerSize, d),
            ["refArg"] = (v, d) => AssertSize(v, pointerSize, d),
        });

        // --- Instance method with struct parameter ---
        // InstanceMethodOnStruct(SmallStruct s) — struct arg → pointer-sized
        var instanceStructArgs = GetArgumentValues("InstanceMethodOnStruct");
        AssertEach(instanceStructArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["s"] = (v, d) => AssertSize(v, pointerSize, d),
        });

        // --- Locals: non-zero (IL slot order is not stable across builds) ---
        var locals = GetLocalValues("PrimitiveVars");
        Assert.True(locals.Count >= 8, $"Expected at least 8 locals, got {locals.Count}");
        foreach (var (idx, value) in locals)
        {
            ulong size;
            int hr = value.GetSize(&size);
            Assert.True(hr >= 0, $"local[{idx}]: GetSize failed with 0x{hr:X8}");
            Assert.True(size > 0, $"local[{idx}]: expected non-zero size, got {size}");
        }

        // --- Single-local frames: exact size verification ---
        // InstanceMethodVars has 1 local: int localSum → 4 bytes
        var instanceLocals = GetLocalValues("InstanceMethodVars");
        Assert.True(instanceLocals.ContainsKey(0), "InstanceMethodVars local[0] not found");
        AssertSize(instanceLocals[0], 4, "InstanceMethodVars local[0]");

        // ClassGenericVars has 1 local: T localCopy where T=int → 4 bytes
        var classGenericLocals = GetLocalValues("ClassGenericVars");
        Assert.True(classGenericLocals.ContainsKey(0), "ClassGenericVars local[0] not found");
        AssertSize(classGenericLocals[0], 4, "ClassGenericVars local[0]");

        // --- Single-dim array: pointer-sized ---
        // SingleDimArrayVars(int[] arrayArg)
        var singleDimArgs = GetArgumentValues("SingleDimArrayVars");
        AssertEach(singleDimArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["arrayArg"] = (v, d) => AssertSize(v, pointerSize, d),
        });

        // SingleDimArrayVars has 1 local: int[] localArray → pointer-sized
        var singleDimLocals = GetLocalValues("SingleDimArrayVars");
        Assert.True(singleDimLocals.ContainsKey(0), "SingleDimArrayVars local[0] not found");
        AssertSize(singleDimLocals[0], pointerSize, "SingleDimArrayVars local[0]");

        // --- Multi-dim array: pointer-sized ---
        // MultiDimArrayVars(int[,] multiDimArg)
        var multiDimArgs = GetArgumentValues("MultiDimArrayVars");
        AssertEach(multiDimArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["multiDimArg"] = (v, d) => AssertSize(v, pointerSize, d),
        });

        // MultiDimArrayVars has 1 local: int[,] localMultiDim → pointer-sized
        var multiDimLocals = GetLocalValues("MultiDimArrayVars");
        Assert.True(multiDimLocals.ContainsKey(0), "MultiDimArrayVars local[0] not found");
        AssertSize(multiDimLocals[0], pointerSize, "MultiDimArrayVars local[0]");

        // --- Pointer and function pointer: all pointer-sized ---
        // PointerVars(int* ptrArg, delegate*<int,int> funcPtrArg)
        var ptrArgs = GetArgumentValues("PointerVars");
        AssertEach(ptrArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["ptrArg"] = (v, d) => AssertSize(v, pointerSize, d),
            ["funcPtrArg"] = (v, d) => AssertSize(v, pointerSize, d),
        });
    }

    // ========== GetFlags ==========

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetFlags_ReturnsExpectedFlags(TestConfiguration config)
    {
        InitializeDumpTest(config);

        // --- Primitives: IS_PRIMITIVE ---
        // PrimitiveVars(int, double, bool, char, byte, short, long, float)
        var primitiveArgs = GetArgumentValues("PrimitiveVars");
        AssertEach(primitiveArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["intArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_PRIMITIVE, d),
            ["doubleArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_PRIMITIVE, d),
            ["boolArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_PRIMITIVE, d),
            ["charArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_PRIMITIVE, d),
            ["byteArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_PRIMITIVE, d),
            ["shortArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_PRIMITIVE, d),
            ["longArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_PRIMITIVE, d),
            ["floatArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_PRIMITIVE, d),
        });

        // --- Native ints: IS_PRIMITIVE ---
        // NativeIntVars(nint nintArg, nuint nuintArg)
        var nativeIntArgs = GetArgumentValues("NativeIntVars");
        AssertEach(nativeIntArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["nintArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_PRIMITIVE, d),
            ["nuintArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_PRIMITIVE, d),
        });

        // --- Structs: IS_VALUE_TYPE ---
        // StructVars(TinyStruct tinyArg, SmallStruct smallArg, LargeStruct largeArg)
        var structArgs = GetArgumentValues("StructVars");
        AssertEach(structArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["tinyArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_VALUE_TYPE, d),
            ["smallArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_VALUE_TYPE, d),
            ["largeArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_VALUE_TYPE, d),
        });

        // --- Reference types, enum, object, array ---
        // ReferenceTypeVars(string stringArg, SimpleClass classArg, Color enumArg, object boxedArg, int[] arrayArg)
        // Native DAC's GetTypeFieldValueFlags checks IsObjRef first (before IsArray/IsString),
        // so all GC-reference types (string, class, object, arrays) return IS_REFERENCE.
        // IS_ENUM is reachable because enums use GetInternalCorElementType → underlying primitive
        // type which is not IsObjRef.
        var refArgs = GetArgumentValues("ReferenceTypeVars");
        AssertEach(refArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["stringArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_REFERENCE, d),
            ["classArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_REFERENCE, d),
            ["enumArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_ENUM, d),
            ["boxedArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_REFERENCE, d),
            ["arrayArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_REFERENCE, d),
        });

        // --- GenericInst and ByRef ---
        // GenericInstAndByRefVars(List<int> listArg, KeyValuePair<int,string> kvpArg, ref int refArg)
        // Native DAC passes ByRef TypeHandle directly to GetTypeFieldValueFlags which
        // returns DEFAULT (ELEMENT_TYPE_BYREF is not IsObjRef, not primitive, etc.).
        var genericInstArgs = GetArgumentValues("GenericInstAndByRefVars");
        AssertEach(genericInstArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["listArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_REFERENCE, d),
            ["kvpArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_VALUE_TYPE, d),
            ["refArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.DEFAULT, d),
        });

        // --- Instance method: 'this' is IS_REFERENCE, extra (int) is IS_PRIMITIVE ---
        // InstanceWrapper.InstanceMethodVars(int extra)
        var instanceArgs = GetArgumentValues("InstanceMethodVars");
        AssertEach(instanceArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["this"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_REFERENCE, d),
            ["extra"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_PRIMITIVE, d),
        });

        // --- Class generic (VAR) and method generic (MVAR) ---
        // GenericContainer<int>.ClassGenericVars(T value) — T=int → IS_PRIMITIVE
        var classGenericArgs = GetArgumentValues("ClassGenericVars");
        AssertEach(classGenericArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["this"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_REFERENCE, d),
            ["value"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_PRIMITIVE, d),
        });

        // MethodGenericVars<int, string>(arg1, arg2) — T1=int → IS_PRIMITIVE, T2=string → IS_REFERENCE
        var methodGenericArgs = GetArgumentValues("MethodGenericVars");
        AssertEach(methodGenericArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["arg1"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_PRIMITIVE, d),
            ["arg2"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_REFERENCE, d),
        });

        // --- Single-local frames: exact flags verification ---
        // InstanceMethodVars has 1 local: int localSum → IS_PRIMITIVE
        var instanceLocals = GetLocalValues("InstanceMethodVars");
        Assert.True(instanceLocals.ContainsKey(0), "InstanceMethodVars local[0] not found");
        AssertFlags(instanceLocals[0], ClrDataValueFlag.IS_PRIMITIVE, "InstanceMethodVars local[0]");

        // ClassGenericVars has 1 local: T localCopy where T=int → IS_PRIMITIVE
        var classGenericLocals = GetLocalValues("ClassGenericVars");
        Assert.True(classGenericLocals.ContainsKey(0), "ClassGenericVars local[0] not found");
        AssertFlags(classGenericLocals[0], ClrDataValueFlag.IS_PRIMITIVE, "ClassGenericVars local[0]");

        // --- Single-dim array ---
        // SingleDimArrayVars(int[] arrayArg)
        //
        // Native DAC's GetTypeFieldValueFlags (inspect.cpp):
        //   int[] → ELEMENT_TYPE_SZARRAY → IsObjRef=true → IS_REFERENCE
        var singleDimArgs = GetArgumentValues("SingleDimArrayVars");
        AssertEach(singleDimArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["arrayArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_REFERENCE, d),
        });

        // SingleDimArrayVars has 1 local: int[] localArray → IS_REFERENCE
        var singleDimLocals = GetLocalValues("SingleDimArrayVars");
        Assert.True(singleDimLocals.ContainsKey(0), "SingleDimArrayVars local[0] not found");
        AssertFlags(singleDimLocals[0], ClrDataValueFlag.IS_REFERENCE, "SingleDimArrayVars local[0]");

        // --- Multi-dim array ---
        // MultiDimArrayVars(int[,] multiDimArg)
        //
        // Native DAC's GetTypeFieldValueFlags (inspect.cpp):
        //   int[,] → ELEMENT_TYPE_ARRAY → IsObjRef=true → IS_REFERENCE
        var multiDimArgs = GetArgumentValues("MultiDimArrayVars");
        AssertEach(multiDimArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["multiDimArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_REFERENCE, d),
        });

        // MultiDimArrayVars has 1 local: int[,] localMultiDim → IS_REFERENCE
        var multiDimLocals = GetLocalValues("MultiDimArrayVars");
        Assert.True(multiDimLocals.ContainsKey(0), "MultiDimArrayVars local[0] not found");
        AssertFlags(multiDimLocals[0], ClrDataValueFlag.IS_REFERENCE, "MultiDimArrayVars local[0]");

        // --- Pointer and function pointer ---
        // PointerVars(int* ptrArg, delegate*<int,int> funcPtrArg)
        //
        // Native DAC's GetTypeFieldValueFlags (inspect.cpp):
        //   int*                -> ELEMENT_TYPE_PTR    -> IS_POINTER
        //   delegate*<int,int>  -> mapped to IntPtr in DAC build (siginfo.cpp, DACCESS_COMPILE)
        //                       -> ELEMENT_TYPE_I      -> IsPrimitiveType -> IS_PRIMITIVE
        //
        // Raw signature parsing reads the leading type code directly from the method
        // signature blob, avoiding the cross-module TypeDesc resolution issue.
        var ptrArgs = GetArgumentValues("PointerVars");
        AssertEach(ptrArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["ptrArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_POINTER, d),
            ["funcPtrArg"] = (v, d) => AssertFlags(v, ClrDataValueFlag.IS_PRIMITIVE, d),
        });
    }

    // ========== GetBytes ==========

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetBytes_ReturnsExpectedValues(TestConfiguration config)
    {
        InitializeDumpTest(config);

        // --- Primitives: exact byte values ---
        // PrimitiveVars(42, 3.14, true, 'Z', 0xFF, -1, 123456789L, 2.5f)
        var primitiveArgs = GetArgumentValues("PrimitiveVars");
        AssertEach(primitiveArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["intArg"] = (v, d) => AssertBytes(v, BitConverter.GetBytes(42), d),
            ["doubleArg"] = (v, d) => AssertBytes(v, BitConverter.GetBytes(3.14), d),
            ["boolArg"] = (v, d) => AssertBytes(v, [(byte)1], d),
            ["charArg"] = (v, d) => AssertBytes(v, BitConverter.GetBytes((short)'Z'), d),
            ["byteArg"] = (v, d) => AssertBytes(v, [0xFF], d),
            ["shortArg"] = (v, d) => AssertBytes(v, BitConverter.GetBytes((short)-1), d),
            ["longArg"] = (v, d) => AssertBytes(v, BitConverter.GetBytes(123456789L), d),
            ["floatArg"] = (v, d) => AssertBytes(v, BitConverter.GetBytes(2.5f), d),
        });

        // --- Enum: Color.Blue = 2 ---
        // ReferenceTypeVars(string, SimpleClass, Color.Blue)
        var enumArgs = GetArgumentValues("ReferenceTypeVars");
        AssertEach(enumArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["enumArg"] = (v, d) => AssertBytes(v, BitConverter.GetBytes(2), d),
        });

        // --- Struct: SmallStruct { X=100, Y=200 } ---
        // InstanceMethodOnStruct(SmallStruct s)
        // SmallStruct is 8 bytes: two ints packed contiguously (X=100, Y=200)
        var structArgs = GetArgumentValues("InstanceMethodOnStruct");
        AssertEach(structArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["s"] = (v, d) =>
            {
                byte[] expected = new byte[8];
                BitConverter.GetBytes(100).CopyTo(expected, 0);
                BitConverter.GetBytes(200).CopyTo(expected, 4);
                AssertBytes(v, expected, d);
            },
        });

        // --- Instance method: extra = s.Y = 200 ---
        // InstanceWrapper.InstanceMethodVars(extra) where extra = 200
        var instanceArgs = GetArgumentValues("InstanceMethodVars");
        AssertEach(instanceArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["extra"] = (v, d) => { AssertSize(v, 4, d); AssertBytes(v, BitConverter.GetBytes(200), d); },
        });

        // --- Generic method: arg1 = 300 (int via MVAR resolution) ---
        // MethodGenericVars<int, string>(300, "generic")
        var genericArgs = GetArgumentValues("MethodGenericVars");
        AssertEach(genericArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["arg1"] = (v, d) => AssertBytes(v, BitConverter.GetBytes(300), d),
        });

        // --- Native ints: exact pointer values ---
        // NativeIntVars(nint=0x1234, nuint=0x5678)
        var nativeIntArgs = GetArgumentValues("NativeIntVars");
        int pointerSize = Target.PointerSize;
        AssertEach(nativeIntArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["nintArg"] = (v, d) =>
            {
                byte[] expected = pointerSize == 8 ? BitConverter.GetBytes((long)0x1234) : BitConverter.GetBytes(0x1234);
                AssertBytes(v, expected, d);
            },
            ["nuintArg"] = (v, d) =>
            {
                byte[] expected = pointerSize == 8 ? BitConverter.GetBytes((long)0x5678) : BitConverter.GetBytes(0x5678);
                AssertBytes(v, expected, d);
            },
        });

        // --- Reference types: read the pointer, then use cDAC contracts to inspect the object ---
        // ReferenceTypeVars(string stringArg="test", SimpleClass classArg, Color enumArg,
        //                   object boxedArg=(object)42, int[] arrayArg={1,2,3})
        var refArgs = GetArgumentValues("ReferenceTypeVars");

        // String: GetBytes → pointer → IObject.GetStringValue → "test"
        AssertEach(refArgs, new Dictionary<string, Action<IXCLRDataValue, string>>
        {
            ["stringArg"] = (v, d) =>
            {
                TargetPointer ptr = ReadPointerFromValue(v, d);
                string actual = Target.Contracts.Object.GetStringValue(ptr);
                Assert.Equal("test", actual);
            },
            // Array: GetBytes → pointer → IObject.GetArrayData → count=3
            ["arrayArg"] = (v, d) =>
            {
                TargetPointer ptr = ReadPointerFromValue(v, d);
                Target.Contracts.Object.GetArrayData(ptr, out uint count, out _, out _);
                Assert.Equal(3u, count);
            },
        });

        // --- Single-local frames: exact byte verification ---
        // InstanceMethodVars has 1 local: int localSum = _value(100) + extra(200) = 300
        var instanceLocals = GetLocalValues("InstanceMethodVars");
        Assert.True(instanceLocals.ContainsKey(0), "InstanceMethodVars local[0] not found");
        AssertBytes(instanceLocals[0], BitConverter.GetBytes(300), "InstanceMethodVars local[0]");

        // ClassGenericVars has 1 local: T localCopy = value = 300
        var classGenericLocals = GetLocalValues("ClassGenericVars");
        Assert.True(classGenericLocals.ContainsKey(0), "ClassGenericVars local[0] not found");
        AssertBytes(classGenericLocals[0], BitConverter.GetBytes(300), "ClassGenericVars local[0]");
    }

    // ========== GetNumLocations ==========

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetNumLocations_ReturnsValidCount(TestConfiguration config)
    {
        InitializeDumpTest(config);

        // On 64-bit platforms, each variable has at most 1 location (no split values).
        // On 32-bit, a 64-bit value could be split across 2 locations.
        uint maxLocations = Target.PointerSize >= 8 ? 1u : 2u;

        // --- Arguments ---
        var args = GetArgumentValues("PrimitiveVars");
        foreach (var (name, value) in args)
        {
            uint numLocs;
            int hr = value.GetNumLocations(&numLocs);
            Assert.True(hr >= 0, $"{name}: GetNumLocations failed with 0x{hr:X8}");
            Assert.True(numLocs <= maxLocations, $"{name}: expected at most {maxLocations} locations, got {numLocs}");
        }

        // --- Locals ---
        var locals = GetLocalValues("PrimitiveVars");
        foreach (var (idx, value) in locals)
        {
            uint numLocs;
            int hr = value.GetNumLocations(&numLocs);
            Assert.True(hr >= 0, $"local[{idx}]: GetNumLocations failed with 0x{hr:X8}");
            Assert.True(numLocs <= maxLocations, $"local[{idx}]: expected at most {maxLocations} locations, got {numLocs}");
        }
    }

    // ========== GetLocationByIndex ==========

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetLocationByIndex_ValidAndOutOfRange(TestConfiguration config)
    {
        InitializeDumpTest(config);
        var locals = GetLocalValues("PrimitiveVars");

        Assert.True(locals.Count > 0, "No locals found");
        IXCLRDataValue local0 = locals.Values.First();

        uint numLocs;
        int hr = local0.GetNumLocations(&numLocs);
        Assert.True(hr >= 0, $"GetNumLocations failed with 0x{hr:X8}");

        // Valid indices should succeed
        for (uint i = 0; i < numLocs; i++)
        {
            uint flags;
            ClrDataAddress addr;
            hr = local0.GetLocationByIndex(i, &flags, &addr);
            Assert.True(hr >= 0, $"GetLocationByIndex({i}) failed with 0x{hr:X8}");
        }

        // Out-of-range index should fail
        uint oobFlags;
        ClrDataAddress oobAddr;
        hr = local0.GetLocationByIndex(numLocs, &oobFlags, &oobAddr);
        Assert.True(hr < 0, $"Expected failure for out-of-range location index {numLocs}");
    }

    // ========== Comprehensive: all frames ==========

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetSize_GetFlags_AllFrames_Succeeds(TestConfiguration config)
    {
        InitializeDumpTest(config);

        // Walk every frame on the crashing thread and call GetSize/GetFlags on
        // every argument and local. Verifies no unexpected exceptions are thrown
        // across all type varieties in the debuggee's deep call stack.
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);
        int totalValues = 0;

        foreach (IStackDataFrameHandle dataFrame in stackWalk.CreateStackWalk(crashingThread))
        {
            TargetPointer md = stackWalk.GetMethodDescPtr(dataFrame);
            if (md == TargetPointer.Null)
                continue;

            ClrDataFrame frame = new ClrDataFrame(Target, dataFrame, legacyImpl: null);
            IXCLRDataFrame xclrFrame = frame;

            uint numArgs;
            if (xclrFrame.GetNumArguments(&numArgs) >= 0)
            {
                for (uint i = 0; i < numArgs; i++)
                {
                    DacComNullableByRef<IXCLRDataValue> argOut = new(isNullRef: false);
                    if (xclrFrame.GetArgumentByIndex(i, argOut, 0, null, null) >= 0 && argOut.Interface is not null)
                    {
                        totalValues++;
                        uint flags;
                        argOut.Interface.GetFlags(&flags);
                        ulong size;
                        argOut.Interface.GetSize(&size);
                    }
                }
            }

            uint numLocals;
            if (xclrFrame.GetNumLocalVariables(&numLocals) >= 0)
            {
                for (uint i = 0; i < numLocals; i++)
                {
                    DacComNullableByRef<IXCLRDataValue> localOut = new(isNullRef: false);
                    if (xclrFrame.GetLocalVariableByIndex(i, localOut, 0, null, null) >= 0 && localOut.Interface is not null)
                    {
                        totalValues++;
                        uint flags;
                        localOut.Interface.GetFlags(&flags);
                        ulong size;
                        localOut.Interface.GetSize(&size);
                    }
                }
            }
        }

        Assert.True(totalValues > 20, $"Expected many values across all frames, got {totalValues}");
    }

    // ========== Helpers ==========

    /// <summary>
    /// Gets all arguments for a method as a dictionary of name -> IXCLRDataValue.
    /// </summary>
    private Dictionary<string, IXCLRDataValue> GetArgumentValues(string methodName)
    {
        (ClrDataFrame frame, _) = FindFrameByMethodName(methodName);
        IXCLRDataFrame xclrFrame = frame;

        uint numArgs;
        int hr = xclrFrame.GetNumArguments(&numArgs);
        AssertHResult(HResults.S_OK, hr);

        Dictionary<string, IXCLRDataValue> result = new();
        char* nameBuf = stackalloc char[256];
        for (uint i = 0; i < numArgs; i++)
        {
            uint nameLen;
            DacComNullableByRef<IXCLRDataValue> argOut = new(isNullRef: false);
            hr = xclrFrame.GetArgumentByIndex(i, argOut, 256, &nameLen, nameBuf);
            if (hr >= 0 && argOut.Interface is not null)
            {
                // nameLen includes the null terminator; construct the string without it.
                string name = nameLen > 1 ? new string(nameBuf, 0, (int)(nameLen - 1)) : string.Empty;
                result[name] = argOut.Interface;
            }
        }

        return result;
    }

    /// <summary>
    /// Gets all locals for a method as a dictionary of index -> IXCLRDataValue.
    /// </summary>
    private Dictionary<uint, IXCLRDataValue> GetLocalValues(string methodName)
    {
        (ClrDataFrame frame, _) = FindFrameByMethodName(methodName);
        IXCLRDataFrame xclrFrame = frame;

        uint numLocals;
        int hr = xclrFrame.GetNumLocalVariables(&numLocals);
        AssertHResult(HResults.S_OK, hr);

        Dictionary<uint, IXCLRDataValue> result = new();
        for (uint i = 0; i < numLocals; i++)
        {
            DacComNullableByRef<IXCLRDataValue> localOut = new(isNullRef: false);
            hr = xclrFrame.GetLocalVariableByIndex(i, localOut, 0, null, null);
            if (hr >= 0 && localOut.Interface is not null)
            {
                result[i] = localOut.Interface;
            }
        }

        return result;
    }

    private static void AssertSize(IXCLRDataValue value, int expectedSize, string description)
    {
        ulong size;
        int hr = value.GetSize(&size);
        Assert.True(hr >= 0, $"{description}: GetSize failed with 0x{hr:X8}");
        Assert.Equal((ulong)expectedSize, size);
    }

    /// <summary>
    /// Reads the raw bytes from a value and interprets them as a target pointer.
    /// Used to get the object address from reference-type arguments for further inspection.
    /// </summary>
    private TargetPointer ReadPointerFromValue(IXCLRDataValue value, string description)
    {
        ulong size;
        int hr = value.GetSize(&size);
        Assert.True(hr >= 0 && size > 0, $"{description}: GetSize failed or returned 0");

        byte[] bytes = new byte[size];
        uint dataSize;
        fixed (byte* pBuf = bytes)
        {
            hr = value.GetBytes((uint)size, &dataSize, pBuf);
        }

        Assert.True(hr >= 0, $"{description}: GetBytes failed with 0x{hr:X8}");

        ulong ptr = Target.PointerSize == 8
            ? BitConverter.ToUInt64(bytes, 0)
            : BitConverter.ToUInt32(bytes, 0);
        Assert.True(ptr != 0, $"{description}: expected non-null pointer");

        return new TargetPointer(ptr);
    }

    private static void AssertFlags(IXCLRDataValue value, ClrDataValueFlag expectedFlags, string description)
    {
        uint flags = AssertGetFlags(value, description);
        Assert.Equal((uint)expectedFlags, flags);
    }

    private static uint AssertGetFlags(IXCLRDataValue value, string description)
    {
        uint flags;
        int hr = value.GetFlags(&flags);
        Assert.True(hr >= 0, $"{description}: GetFlags failed with 0x{hr:X8}");

        return flags;
    }

    private static void AssertBytes(IXCLRDataValue value, byte[] expectedBytes, string description)
    {
        ulong size;
        int hr = value.GetSize(&size);
        Assert.True(hr >= 0 && size > 0, $"{description}: GetSize failed or returned 0");

        byte[] actual = new byte[size];
        uint dataSize;
        fixed (byte* pBuf = actual)
        {
            hr = value.GetBytes((uint)size, &dataSize, pBuf);
        }

        Assert.True(hr >= 0, $"{description}: GetBytes failed with 0x{hr:X8}");

        int compareLen = Math.Min(expectedBytes.Length, (int)dataSize);
        Assert.True(
            actual.AsSpan(0, compareLen).SequenceEqual(expectedBytes.AsSpan(0, compareLen)),
            $"{description}: bytes mismatch. " +
            $"Expected: [{string.Join(",", expectedBytes.Take(compareLen).Select(b => $"0x{b:X2}"))}], " +
            $"Actual: [{string.Join(",", actual.Take(compareLen).Select(b => $"0x{b:X2}"))}]");
    }

    private static void AssertEach<TKey>(Dictionary<TKey, IXCLRDataValue> values, Dictionary<TKey, Action<IXCLRDataValue, string>> assertions)
        where TKey : notnull
    {
        foreach (var (key, assert) in assertions)
        {
            Assert.True(values.ContainsKey(key), $"Value '{key}' not found");
            assert(values[key], $"'{key}'");
        }
    }

    private (ClrDataFrame Frame, IStackDataFrameHandle DataFrame) FindFrameByMethodName(string methodName)
    {
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        foreach (IStackDataFrameHandle dataFrame in stackWalk.CreateStackWalk(crashingThread))
        {
            TargetPointer md = stackWalk.GetMethodDescPtr(dataFrame);
            if (md == TargetPointer.Null)
                continue;

            string? name = DumpTestHelpers.GetMethodName(Target, md);
            if (name == methodName)
            {
                ClrDataFrame frame = new ClrDataFrame(Target, dataFrame, legacyImpl: null);

                return (frame, dataFrame);
            }
        }

        Assert.Fail($"{methodName} not found on the crashing thread's stack");
        throw new InvalidOperationException("Unreachable");
    }
}
