// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
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

            SerializationRecord[] array = (SerializationRecord[])arrayRecord.GetArray(input.GetType());
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
            PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)array[0, 0];
            Assert.Equal(Text, stringRecord.Value);
        }

        [Fact]
        public void CanReadBinaryArrayThatContainsArrayRecord_SZ()
        {
            int[] intArray = [1, 2, 3];
            IEnumerable[] input = [intArray];

            SZArrayRecord<SerializationRecord> arrayRecord = (SZArrayRecord<SerializationRecord>)NrbfDecoder.Decode(Serialize(input));

            SerializationRecord[] array = (SerializationRecord[])arrayRecord.GetArray(input.GetType());
            SZArrayRecord<int> intArrayRecord = (SZArrayRecord<int>)array[0];
            Assert.Equal(intArray, intArrayRecord.GetArray());
        }

        [Fact]
        public void CanReadBinaryArrayThatContainsArrayRecord_MD()
        {
            int[] intArray = [1, 2, 3];
            IEnumerable[,] input = new IEnumerable[1, 1];
            input[0, 0] = intArray;

            ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input));

            SerializationRecord[,] array = (SerializationRecord[,])arrayRecord.GetArray(input.GetType());
            SZArrayRecord<int> intArrayRecord = (SZArrayRecord<int>)array[0, 0];
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
                classThatImplementsIEnumerable // ClassWithMembersAndTypesRecord
            ];

            SZArrayRecord<SerializationRecord> arrayRecord = (SZArrayRecord<SerializationRecord>)NrbfDecoder.Decode(Serialize(input));

            SerializationRecord[] array = (SerializationRecord[])arrayRecord.GetArray(input.GetType());
            PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)array[0];
            Assert.Equal(Text, stringRecord.Value);
            SZArrayRecord<int> intArrayRecord = (SZArrayRecord<int>)array[1];
            Assert.Equal(intArray, intArrayRecord.GetArray());
            ClassRecord classRecord = (ClassRecord)array[2];
            Assert.Equal(classThatImplementsIEnumerable.Field, classRecord.GetInt32(nameof(CustomClassThatImplementsIEnumerable.Field)));
        }

        [Fact]
        public void CanReadBinaryArrayThatContainsAllRecordTypes_MD()
        {
            const string Text = "hello";
            int[] intArray = [1, 2, 3];
            CustomClassThatImplementsIEnumerable classThatImplementsIEnumerable = new() { Field = 456 };

            IEnumerable[,] input = new IEnumerable[1, 3];
            input[0, 0] = Text; // BinaryObjectStringRecord
            input[0, 1] = intArray; // ArraySinglePrimitiveRecord
            input[0, 2] = classThatImplementsIEnumerable; // ClassWithMembersAndTypesRecord

            ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input));

            SerializationRecord[,] array = (SerializationRecord[,])arrayRecord.GetArray(input.GetType());
            PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)array[0, 0];
            Assert.Equal(Text, stringRecord.Value);
            SZArrayRecord<int> intArrayRecord = (SZArrayRecord<int>)array[0, 1];
            Assert.Equal(intArray, intArrayRecord.GetArray());
            ClassRecord classRecord = (ClassRecord)array[0, 2];
            Assert.Equal(classThatImplementsIEnumerable.Field, classRecord.GetInt32(nameof(CustomClassThatImplementsIEnumerable.Field)));
        }

        [Serializable]
        public class CustomClassThatImplementsIEnumerable : IEnumerable
        {
            public int Field;

            public IEnumerator GetEnumerator() => Array.Empty<int>().GetEnumerator();
        }
    }
}
