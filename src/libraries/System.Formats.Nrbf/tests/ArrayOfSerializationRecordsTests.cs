// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Formats.Nrbf.Tests
{
    public class ArrayOfSerializationRecordsTests : ReadTests
    {
        [Fact]
        public void CanReadBinaryArrayThatContainsStringRecord_SZ()
        {
            const string Text = "hello";
            IEnumerable[] input = [Text];

            SZArrayRecord<SerializationRecord> arrayRecord = (SZArrayRecord<SerializationRecord>)NrbfDecoder.Decode(Serialize(input));
            SerializationRecord[] array = arrayRecord.GetArray();

            PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)array[0];
            Assert.Equal(Text, stringRecord.Value);
        }

        [Fact]
        public void CanReadBinaryArrayThatContainsStringRecord_MD()
        {
            const string Text = "hello";
            IEnumerable[,] input = new IEnumerable[1, 1];
            input[0, 0] = Text;

            ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input));
            SerializationRecord[,] array = (SerializationRecord[,])arrayRecord.GetArray(input.GetType());

            Assert.Equal(input.GetLength(0), array.GetLength(0));
            Assert.Equal(input.GetLength(1), array.GetLength(1));
            PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)array[0, 0];
            Assert.Equal(Text, stringRecord.Value);
        }

        [Fact]
        public void CanReadBinaryArrayThatContainsStringRecord_Jagged()
        {
            const string Text = "hello";
            IEnumerable[][] input = [[Text]];

            ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input));
            SerializationRecord[][] array = (SerializationRecord[][])arrayRecord.GetArray(input.GetType());

            PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)array[0][0];
            Assert.Equal(Text, stringRecord.Value);
        }

        [Theory]
        [InlineData(1)] // ObjectNullRecord
        [InlineData(200)] // ObjectNullMultiple256Record
        [InlineData(1_000)] // ObjectNullMultipleRecord
        public void CanReadBinaryArrayThatContainsNullRecords_SZ(int nullCount)
        {
            const string Text = "notNull";
            IEnumerable[] input = new IEnumerable[nullCount + 1];
            input[nullCount] = Text;

            SZArrayRecord<SerializationRecord> arrayRecord = (SZArrayRecord<SerializationRecord>)NrbfDecoder.Decode(Serialize(input));
            SerializationRecord?[] array = arrayRecord.GetArray();

            Assert.Equal(input.Length, array.Length);
            Assert.All(array.Take(nullCount), Assert.Null);
            PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)array[nullCount];
            Assert.Equal(Text, stringRecord.Value);
        }

        [Theory]
        [InlineData(1)] // ObjectNullRecord
        [InlineData(200)] // ObjectNullMultiple256Record
        [InlineData(1_000)] // ObjectNullMultipleRecord
        public void CanReadBinaryArrayThatContainsNullRecords_MD(int nullCount)
        {
            const string Text = "notNull";
            IEnumerable[,] input = new IEnumerable[1, nullCount + 1];
            input[0, nullCount] = Text;

            ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input));
            SerializationRecord[,] array = (SerializationRecord[,])arrayRecord.GetArray(input.GetType());

            Assert.Equal(input.GetLength(0), array.GetLength(0));
            Assert.Equal(input.GetLength(1), array.GetLength(1));
            for (int i = 0; i < nullCount; i++)
            {
                Assert.Null(array[0, i]);
            }
            PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)array[0, nullCount];
            Assert.Equal(Text, stringRecord.Value);
        }

        [Theory]
        [InlineData(1)] // ObjectNullRecord
        [InlineData(200)] // ObjectNullMultiple256Record
        [InlineData(1_000)] // ObjectNullMultipleRecord
        public void CanReadBinaryArrayThatContainsNullRecords_Jagged(int nullCount)
        {
            const string Text = "notNull";
            IEnumerable[][] input = [new IEnumerable[nullCount + 1]];
            input[0][nullCount] = Text;

            ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input));
            SerializationRecord[][] array = (SerializationRecord[][])arrayRecord.GetArray(input.GetType());

            Assert.Equal(input.Length, array.Length);
            Assert.Equal(input[0].Length, array[0].Length);
            Assert.All(array[0].Take(nullCount), Assert.Null);
            PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)array[0][nullCount];
            Assert.Equal(Text, stringRecord.Value);
        }

        [Fact]
        public void CanReadBinaryArrayThatContainsArrayRecord_SZ()
        {
            int[] intArray = [1, 2, 3];

#if RELEASE // Let's test both the generic and non-generic abstractions.
            IEnumerable[] input = [intArray];
#else
            IList<int>[] input = [intArray];
#endif
            SZArrayRecord<SerializationRecord> arrayRecord = (SZArrayRecord<SerializationRecord>)NrbfDecoder.Decode(Serialize(input));
            SerializationRecord[] array = arrayRecord.GetArray();

            SZArrayRecord<int> intArrayRecord = (SZArrayRecord<int>)array[0];
            Assert.Equal(intArray, intArrayRecord.GetArray());
        }

        [Fact]
        public void CanReadBinaryArrayThatContainsArrayRecord_MD()
        {
            int[] intArray = [1, 2, 3];
#if RELEASE // Let's test both the generic and non-generic abstractions.
            IEnumerable[,] input = new IEnumerable[1, 1];
#else
            IList<int>[,] input = new IList<int>[1, 1];
#endif
            input[0, 0] = intArray;

            ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input));
            SerializationRecord[,] array = (SerializationRecord[,])arrayRecord.GetArray(input.GetType());

            Assert.Equal(input.GetLength(0), array.GetLength(0));
            Assert.Equal(input.GetLength(1), array.GetLength(1));
            SZArrayRecord<int> intArrayRecord = (SZArrayRecord<int>)array[0, 0];
            Assert.Equal(intArray, intArrayRecord.GetArray());
        }

        [Fact]
        public void CanReadBinaryArrayThatContainsArrayRecord_Jagged()
        {
            int[] intArray = [1, 2, 3];
#if RELEASE // Let's test both the generic and non-generic abstractions.
            IEnumerable[][] input = [[intArray]];
#else
            IList<int>[][] input = [[intArray]];
#endif

            ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input));
            SerializationRecord[][] array = (SerializationRecord[][])arrayRecord.GetArray(input.GetType());

            SZArrayRecord<int> intArrayRecord = (SZArrayRecord<int>)array[0][0];
            Assert.Equal(intArray, intArrayRecord.GetArray());
        }

        [Fact]
        public void CanReadBinaryArrayThatContainsAllRecordTypes_SZ()
        {
            const string Text = "hello";
            int[] intArray = [1, 2, 3];
            CustomClassThatImplementsIEnumerable classThatImplementsIEnumerable = new() { Field = 456 };

            IEnumerable[] input = [
                Text, // BinaryObjectStringRecord
                intArray, // ArraySinglePrimitiveRecord
                classThatImplementsIEnumerable, // ClassWithMembersAndTypesRecord,
                null // ObjectNullRecord
            ];

            SZArrayRecord<SerializationRecord> arrayRecord = (SZArrayRecord<SerializationRecord>)NrbfDecoder.Decode(Serialize(input));
            SerializationRecord[] array = arrayRecord.GetArray();

            Assert.Equal(input.Length, array.Length);
            PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)array[0];
            Assert.Equal(Text, stringRecord.Value);
            SZArrayRecord<int> intArrayRecord = (SZArrayRecord<int>)array[1];
            Assert.Equal(intArray, intArrayRecord.GetArray());
            ClassRecord classRecord = (ClassRecord)array[2];
            Assert.Equal(classThatImplementsIEnumerable.Field, classRecord.GetInt32(nameof(CustomClassThatImplementsIEnumerable.Field)));
            Assert.Null(input[3]);
        }

        [Fact]
        public void CanReadBinaryArrayThatContainsAllRecordTypes_MD()
        {
            const string Text = "hello";
            int[] intArray = [1, 2, 3];
            CustomClassThatImplementsIEnumerable classThatImplementsIEnumerable = new() { Field = 456 };

            IEnumerable[,] input = new IEnumerable[1, 4];
            input[0, 0] = Text; // BinaryObjectStringRecord
            input[0, 1] = intArray; // ArraySinglePrimitiveRecord
            input[0, 2] = classThatImplementsIEnumerable; // ClassWithMembersAndTypesRecord
            input[0, 3] = null; // ObjectNullRecord

            ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input));
            SerializationRecord[,] array = (SerializationRecord[,])arrayRecord.GetArray(input.GetType());

            Assert.Equal(input.GetLength(0), array.GetLength(0));
            Assert.Equal(input.GetLength(1), array.GetLength(1));
            PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)array[0, 0];
            Assert.Equal(Text, stringRecord.Value);
            SZArrayRecord<int> intArrayRecord = (SZArrayRecord<int>)array[0, 1];
            Assert.Equal(intArray, intArrayRecord.GetArray());
            ClassRecord classRecord = (ClassRecord)array[0, 2];
            Assert.Equal(classThatImplementsIEnumerable.Field, classRecord.GetInt32(nameof(CustomClassThatImplementsIEnumerable.Field)));
            Assert.Null(array[0, 3]);
        }

        [Fact]
        public void CanReadBinaryArrayThatContainsAllRecordTypes_Jagged()
        {
            const string Text = "hello";
            int[] intArray = [1, 2, 3];
            CustomClassThatImplementsIEnumerable classThatImplementsIEnumerable = new() { Field = 456 };

            IEnumerable[][] input = [new IEnumerable[4]];
            input[0][0] = Text; // BinaryObjectStringRecord
            input[0][1] = intArray; // ArraySinglePrimitiveRecord
            input[0][2] = classThatImplementsIEnumerable; // ClassWithMembersAndTypesRecord
            input[0][3] = null; // ObjectNullRecord

            ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input));
            SerializationRecord[][] array = (SerializationRecord[][])arrayRecord.GetArray(input.GetType());

            Assert.Equal(input.Length, array.Length);
            Assert.Equal(input[0].Length, array[0].Length);
            PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)array[0][0];
            Assert.Equal(Text, stringRecord.Value);
            SZArrayRecord<int> intArrayRecord = (SZArrayRecord<int>)array[0][1];
            Assert.Equal(intArray, intArrayRecord.GetArray());
            ClassRecord classRecord = (ClassRecord)array[0][2];
            Assert.Equal(classThatImplementsIEnumerable.Field, classRecord.GetInt32(nameof(CustomClassThatImplementsIEnumerable.Field)));
            Assert.Null(array[0][3]);
        }

        [Serializable]
        public class CustomClassThatImplementsIEnumerable : IEnumerable
        {
            public int Field;

            public IEnumerator GetEnumerator() => Array.Empty<int>().GetEnumerator();
        }
    }
}
