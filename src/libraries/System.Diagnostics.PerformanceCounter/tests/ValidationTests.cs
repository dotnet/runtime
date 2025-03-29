// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

using static Interop.Advapi32;

namespace System.Diagnostics.Tests
{
    public static class ValidationTests
    {
        [Theory]
        [MemberData(nameof(InvalidDataBlocksToTest))]
        public static void ValidateDataBlock(byte[] data)
        {
            using (PerformanceCounter counter = GetPerformanceCounterLib(out PerformanceCounterLib lib))
            {
                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => GetCategorySample(lib, data));
                Assert.Contains(nameof(PERF_DATA_BLOCK), ex.Message);
            }
        }

        public static IEnumerable<object[]> InvalidDataBlocksToTest()
        {
            int validSize = PERF_DATA_BLOCK.SizeOf;

            yield return Create(0, validSize);
            yield return Create(1, validSize);
            yield return Create(-1, validSize);
            yield return Create(validSize, 0);
            yield return Create(validSize, 1);
            yield return Create(validSize, -1);
            yield return Create(validSize, validSize + 1);
            yield return Create(validSize - 1, validSize);

            static object[] Create(int totalByteLength, int headerLength)
            {
                PERF_DATA_BLOCK perfDataBlock = new()
                {
                    TotalByteLength = totalByteLength,
                    HeaderLength = headerLength,
                    Signature1 = PERF_DATA_BLOCK.Signature1Int,
                    Signature2 = PERF_DATA_BLOCK.Signature2Int
                };

                return new object[] { StructToByteArray(perfDataBlock) };
            }
        }

        [Theory]
        [MemberData(nameof(InvalidObjectTypesToTest))]
        public static void ValidateObjectType(byte[] data)
        {
            using (PerformanceCounter counter = GetPerformanceCounterLib(out PerformanceCounterLib lib))
            {
                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => GetCategorySample(lib, data));
                Assert.Contains(nameof(PERF_OBJECT_TYPE), ex.Message);
            }
        }

        public static IEnumerable<object[]> InvalidObjectTypesToTest()
        {
            VerifyInitialized();

            int validSize = PERF_OBJECT_TYPE.SizeOf;
            yield return new object[] { Create(0, validSize, validSize) };
            yield return new object[] { Create(1, validSize, validSize) };
            yield return new object[] { Create(-1, validSize, validSize) };
            yield return new object[] { Create(validSize, 0, validSize) };
            yield return new object[] { Create(validSize, 1, validSize) };
            yield return new object[] { Create(validSize, -1, validSize) };
            yield return new object[] { Create(validSize, validSize, 0) };
            yield return new object[] { Create(validSize, validSize, 1) };
            yield return new object[] { Create(validSize, validSize, -1) };
            yield return new object[] { Create(validSize - 1, validSize, validSize) };
            yield return new object[] { Create(validSize, validSize - 1, validSize) };
            yield return new object[] { Create(validSize, validSize + 1, validSize) };
            yield return new object[] { Create(validSize, validSize, validSize - 1) };
            yield return new object[] { Create(validSize, validSize, validSize + 1) };

            static byte[] Create(int totalByteLength, int headerLength, int definitionLength)
            {
                PERF_DATA_BLOCK perfDataBlock = CreatePerfDataBlock();
                perfDataBlock.TotalByteLength = PERF_DATA_BLOCK.SizeOf + PERF_OBJECT_TYPE.SizeOf;

                PERF_OBJECT_TYPE perfObjectType = new()
                {
                    TotalByteLength = totalByteLength,
                    HeaderLength = headerLength,
                    DefinitionLength = definitionLength,
                    ObjectNameTitleIndex = s_ObjectNameTitleIndex
                };

                return StructsToByteArray(perfDataBlock, perfObjectType);
            }
        }

        [Theory]
        [MemberData(nameof(ObjectTypeWithHighCountsToTest))]
        public static void ValidateObjectTypeWithHighCounts(byte[] data)
        {
            using (PerformanceCounter counter = GetPerformanceCounterLib(out PerformanceCounterLib lib))
            {
                Exception ex = Assert.ThrowsAny<Exception>(() => GetCategorySample(lib, data));
                Assert.True(ex is InvalidOperationException || ex is OverflowException, $"Type:{ex.GetType().Name}.");
            }
        }

        public static IEnumerable<object[]> ObjectTypeWithHighCountsToTest()
        {
            VerifyInitialized();

            yield return new object[] { Create(Array.MaxLength, 1) };
            yield return new object[] { Create(1, -1) }; // numInstances with -1 is supported, but numCounters is not. 
            yield return new object[] { Create(1, Array.MaxLength) };
            yield return new object[] { Create(Array.MaxLength / 1000, 1) };
            yield return new object[] { Create(1, Array.MaxLength / 1000) };
            yield return new object[] { Create(Array.MaxLength, Array.MaxLength) };

            static byte[] Create(int numInstances, int numCounters)
            {
                PERF_DATA_BLOCK perfDataBlock = CreatePerfDataBlock();
                PERF_OBJECT_TYPE perfObjectType = CreatePerfObjectType(numInstances, numCounters);

                // Add a single instance definition.
                PERF_COUNTER_DEFINITION perfCounterDefinition = CreatePerfCounterDefinition();

                return StructsToByteArray(perfDataBlock, perfObjectType, perfCounterDefinition);
            }
        }

        [Theory]
        [MemberData(nameof(InvalidCounterDefinitionsToTest))]
        public static void ValidateCounterDefinition(byte[] data)
        {
            using (PerformanceCounter counter = GetPerformanceCounterLib(out PerformanceCounterLib lib))
            {
                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => GetCategorySample(lib, data));
                Assert.Contains(nameof(PERF_COUNTER_DEFINITION), ex.Message);
            }
        }

        public static IEnumerable<object[]> InvalidCounterDefinitionsToTest()
        {
            VerifyInitialized();

            int validSize = PERF_INSTANCE_DEFINITION.SizeOf;

            yield return new object[] { Create(0, 4) };
            yield return new object[] { Create(1, 4) };
            yield return new object[] { Create(-1, 4) };
            yield return new object[] { Create(validSize, -1) };
            yield return new object[] { Create(validSize - 1, 4) };
            yield return new object[] { Create(validSize, 1000) };

            static byte[] Create(int byteLength, int counterOffset)
            {
                PERF_DATA_BLOCK perfDataBlock = CreatePerfDataBlock();
                PERF_OBJECT_TYPE perfObjectType = CreatePerfObjectType(numInstances: 0, numCounters: 1);
                PERF_COUNTER_DEFINITION perfCounterDefinition = new()
                {
                    ByteLength = byteLength,
                    CounterOffset = counterOffset,
                    CounterSize = 4,
                    CounterNameTitleIndex = s_ObjectNameTitleIndex
                };

                return StructsToByteArray(perfDataBlock, perfObjectType, perfCounterDefinition);
            }
        }

        [Theory]
        [MemberData(nameof(InvalidInstancesToTest))]
        public static void ValidateInstanceDefinition(byte[] data)
        {
            using (PerformanceCounter counter = GetPerformanceCounterLib(out PerformanceCounterLib lib))
            {
                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => GetCategorySample(lib, data));
                Assert.Contains(nameof(PERF_INSTANCE_DEFINITION), ex.Message);
            }
        }

        public static IEnumerable<object[]> InvalidInstancesToTest()
        {
            VerifyInitialized();

            int validSize = PERF_INSTANCE_DEFINITION.SizeOf;

            yield return new object[] { Create(0, 0, 0) };
            yield return new object[] { Create(1, 0, 0) };
            yield return new object[] { Create(-1, 0, 0) };
            yield return new object[] { Create(validSize, -1, 0) };
            yield return new object[] { Create(validSize, 0, -1) };
            yield return new object[] { Create(validSize, 1000, 0) };
            yield return new object[] { Create(validSize, 0, 1000) };

            static byte[] Create(int byteLength, int nameOffset, int nameLength)
            {
                PERF_DATA_BLOCK perfDataBlock = CreatePerfDataBlock();
                PERF_OBJECT_TYPE perfObjectType = CreatePerfObjectType(numInstances: 1, numCounters: 1);
                PERF_COUNTER_DEFINITION perfCounterDefinition = CreatePerfCounterDefinition();
                PERF_INSTANCE_DEFINITION perfInstanceDefinition = new()
                {
                    ByteLength = byteLength,
                    NameOffset = nameOffset,
                    NameLength = nameLength,
                };

                return StructsToByteArray(perfDataBlock, perfObjectType, perfCounterDefinition, perfInstanceDefinition);
            }
        }

        [Theory]
        [MemberData(nameof(InvalidCounterBlocksToTest))]
        public static void ValidateCounterBlock(byte[] data)
        {
            using (PerformanceCounter counter = GetPerformanceCounterLib(out PerformanceCounterLib lib))
            {
                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => GetCategorySample(lib, data));
                Assert.Contains(nameof(PERF_COUNTER_BLOCK), ex.Message);
            }
        }

        public static IEnumerable<object[]> InvalidCounterBlocksToTest()
        {
            VerifyInitialized();

            yield return new object[] { Create(-1) };
            yield return new object[] { Create(0) };
            yield return new object[] { Create(1) };

            static byte[] Create(int byteLength)
            {
                PERF_DATA_BLOCK perfDataBlock = CreatePerfDataBlock();
                PERF_OBJECT_TYPE perfObjectType = CreatePerfObjectType(numInstances: 1, numCounters: 1);
                PERF_COUNTER_DEFINITION perfCounterDefinition = CreatePerfCounterDefinition();
                PERF_INSTANCE_DEFINITION perfInstanceDefinition = CreatePerfInstanceDefinition();
                PERF_COUNTER_BLOCK perfCounterBlock = new()
                {
                    ByteLength = byteLength
                };

                int value = 0;

                return StructsToByteArray(perfDataBlock, perfObjectType, perfCounterDefinition, perfInstanceDefinition, perfCounterBlock, value);
            }
        }

        private static byte[] StructToByteArray<T>(T value) where T : struct
        {
            int size = Marshal.SizeOf(value);
            byte[] arr = new byte[size];
            CopyStruct(value, arr, 0, size);
            return arr;
        }

        private static byte[] StructsToByteArray<T1, T2>(T1 value1, T2 value2)
            where T1 : struct where T2 : struct
        {
            int size1 = Marshal.SizeOf(value1);
            int size2 = Marshal.SizeOf(value2);
            byte[] arr = new byte[size1 + size2];
            CopyStruct(value1, arr, 0, size1);
            CopyStruct(value2, arr, size1, size2);
            return arr;
        }

        private static byte[] StructsToByteArray<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
            where T1 : struct where T2 : struct where T3 : struct
        {
            int size1 = Marshal.SizeOf(value1);
            int size2 = Marshal.SizeOf(value2);
            int size3 = Marshal.SizeOf(value3);
            byte[] arr = new byte[size1 + size2 + size3];
            CopyStruct(value1, arr, 0, size1);
            CopyStruct(value2, arr, size1, size2);
            CopyStruct(value3, arr, size1 + size2, size3);
            return arr;
        }

        private static byte[] StructsToByteArray<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct
        {
            int size1 = Marshal.SizeOf(value1);
            int size2 = Marshal.SizeOf(value2);
            int size3 = Marshal.SizeOf(value3);
            int size4 = Marshal.SizeOf(value4);
            byte[] arr = new byte[size1 + size2 + size3 + size4];
            CopyStruct(value1, arr, 0, size1);
            CopyStruct(value2, arr, size1, size2);
            CopyStruct(value3, arr, size1 + size2, size3);
            CopyStruct(value4, arr, size1 + size2 + size3, size4);
            return arr;
        }

        private static byte[] StructsToByteArray<T1, T2, T3, T4, T5, T6>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct
        {
            int size1 = Marshal.SizeOf(value1);
            int size2 = Marshal.SizeOf(value2);
            int size3 = Marshal.SizeOf(value3);
            int size4 = Marshal.SizeOf(value4);
            int size5 = Marshal.SizeOf(value5);
            int size6 = Marshal.SizeOf(value5);
            byte[] arr = new byte[size1 + size2 + size3 + size4 + size5 + size6];
            CopyStruct(value1, arr, 0, size1);
            CopyStruct(value2, arr, size1, size2);
            CopyStruct(value3, arr, size1 + size2, size3);
            CopyStruct(value4, arr, size1 + size2 + size3, size4);
            CopyStruct(value5, arr, size1 + size2 + size3 + size4, size5);
            CopyStruct(value6, arr, size1 + size2 + size3 + size4 + size5, size6);
            return arr;
        }

        private static void CopyStruct<T>(T value, byte[] data, int startIndex, int length) where T : struct
        {
            int size = Marshal.SizeOf(value);
            IntPtr ptr = Marshal.AllocHGlobal(size);

            try
            {
                Marshal.StructureToPtr(value, ptr, true);
                Marshal.Copy(ptr, data, startIndex, length);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        private static PerformanceCounter GetPerformanceCounterLib(out PerformanceCounterLib lib)
        {
            PerformanceCounter counterSample = Helpers.RetryOnAllPlatformsWithClosingResources(() =>
                new PerformanceCounter("Processor", "Interrupts/sec", "0", "."));

            counterSample.BeginInit();
            Assert.NotNull(counterSample);

            FieldInfo fi = typeof(PerformanceCounterLib).GetField("s_libraryTable", BindingFlags.Static | BindingFlags.NonPublic);
            Hashtable libs = (Hashtable)fi.GetValue(null);
            CategoryEntry category = default;

            bool found = false;
            lib = default;
            foreach (string key in libs.Keys)
            {
                lib = (PerformanceCounterLib)libs[key];
                Assert.NotNull(lib);

                category = (CategoryEntry)lib.CategoryTable["Processor"];
                if (category != null)
                {
                    found = true;
                    break;
                }
            }

            Assert.True(found);

            s_ObjectNameTitleIndex = category.NameIndex;

            return counterSample;
        }

        private static CategorySample GetCategorySample(PerformanceCounterLib lib, byte[] data)
        {
            CategoryEntry entry = (CategoryEntry)lib.CategoryTable["Processor"];
            return new CategorySample(data, entry, lib);
        }

        private static int s_ObjectNameTitleIndex { get; set; } = -1;

        private static void VerifyInitialized()
        {
            if (s_ObjectNameTitleIndex == -1)
            {
                using (PerformanceCounter counter = GetPerformanceCounterLib(out PerformanceCounterLib _)) { }
            }

            Assert.True(s_ObjectNameTitleIndex != -1);
        }

        private static PERF_DATA_BLOCK CreatePerfDataBlock() =>
            new PERF_DATA_BLOCK
            {
                TotalByteLength = PERF_DATA_BLOCK.SizeOf + PERF_OBJECT_TYPE.SizeOf + PERF_COUNTER_DEFINITION.SizeOf,
                HeaderLength = PERF_DATA_BLOCK.SizeOf,
                Signature1 = PERF_DATA_BLOCK.Signature1Int,
                Signature2 = PERF_DATA_BLOCK.Signature2Int,
                NumObjectTypes = 1
            };

        private static PERF_OBJECT_TYPE CreatePerfObjectType(int numInstances, int numCounters) =>
            new PERF_OBJECT_TYPE
            {
                TotalByteLength = PERF_OBJECT_TYPE.SizeOf + PERF_COUNTER_DEFINITION.SizeOf,
                HeaderLength = PERF_OBJECT_TYPE.SizeOf,
                DefinitionLength = PERF_OBJECT_TYPE.SizeOf + PERF_COUNTER_DEFINITION.SizeOf,
                ObjectNameTitleIndex = s_ObjectNameTitleIndex,
                NumCounters = numCounters,
                NumInstances = numInstances
            };

        private static PERF_COUNTER_DEFINITION CreatePerfCounterDefinition() =>
            new PERF_COUNTER_DEFINITION
            {
                ByteLength = PERF_COUNTER_DEFINITION.SizeOf,
                CounterOffset = 4,
                CounterSize = 4,
                CounterNameTitleIndex = s_ObjectNameTitleIndex
            };

        private static PERF_INSTANCE_DEFINITION CreatePerfInstanceDefinition() =>
            new()
            {
                ByteLength = PERF_INSTANCE_DEFINITION.SizeOf,
                NameOffset = 0,
                NameLength = 0,

                // Setting this calls GetInstanceNamesFromIndex() which will validate the real (valid) definition.
                ParentObjectTitleIndex = s_ObjectNameTitleIndex 
            };
    }
}
