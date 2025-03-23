// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Formats.Nrbf.Tests
{
    public class ArrayOfSerializationRecordsTests : ReadTests
    {
        public enum ElementType
        {
            Object,
            NonGeneric,
            Generic
        }

        [Serializable]
        public class CustomClassThatImplementsIEnumerable : IEnumerable
        {
            public int Field;

            public IEnumerator GetEnumerator() => Array.Empty<int>().GetEnumerator();
        }

        [Theory]
        [InlineData(ElementType.Object)]
        [InlineData(ElementType.NonGeneric)]
        [InlineData(ElementType.Generic)]
        public void CanReadArrayThatContainsStringRecord_SZ(ElementType elementType)
        {
            const string Text = "hello";
            Array input = elementType switch
            {
                ElementType.Object => new object[] { Text },
                ElementType.NonGeneric => new IEnumerable[] { Text },
                ElementType.Generic => new IEnumerable<char>[] { Text },
                _ => throw new InvalidOperationException()
            };

            SZArrayRecord<SerializationRecord> arrayRecord = (SZArrayRecord<SerializationRecord>)NrbfDecoder.Decode(Serialize(input), out var recordMap);
            SerializationRecord[] output = arrayRecord.GetArray();

            Verify(input, arrayRecord, output, recordMap);
            PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)output.Single();
            Assert.Equal(Text, stringRecord.Value);
        }

        [Theory]
        [InlineData(ElementType.Object)]
        [InlineData(ElementType.NonGeneric)]
        [InlineData(ElementType.Generic)]
        public void CanReadArrayThatContainsStringRecord_MD(ElementType elementType)
        {
            const string Text = "hello";
            Array input = elementType switch
            {
                ElementType.Object => new object[1, 1],
                ElementType.NonGeneric => new IEnumerable[1, 1],
                ElementType.Generic => new IEnumerable<char>[1, 1],
                _ => throw new InvalidOperationException()
            };
            input.SetValue(Text, 0, 0);

            ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input), out var recordMap);
            SerializationRecord[,] output = (SerializationRecord[,])arrayRecord.GetArray(input.GetType());

            Verify(input, arrayRecord, output, recordMap);
            PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)output[0, 0];
            Assert.Equal(Text, stringRecord.Value);
        }

        [Theory]
        [InlineData(ElementType.Object)]
        [InlineData(ElementType.NonGeneric)]
        [InlineData(ElementType.Generic)]
        public void CanReadArrayThatContainsStringRecord_Jagged(ElementType elementType)
        {
            const string Text = "hello";
            Array input = elementType switch
            {
                ElementType.Object => new object[1][] { [Text] },
                ElementType.NonGeneric => new IEnumerable[1][] { [Text] },
                ElementType.Generic => new IEnumerable<char>[1][] { [Text] },
                _ => throw new InvalidOperationException()
            };

            ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input), out var recordMap);
            ArrayRecord[] output = (ArrayRecord[])arrayRecord.GetArray(input.GetType());

            Verify(input, arrayRecord, output, recordMap);

            SZArrayRecord<SerializationRecord> contained = (SZArrayRecord<SerializationRecord>)output.Single();
            PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)contained.GetArray().Single();
            Assert.Equal(Text, stringRecord.Value);
        }

        [ConditionalTheory]
        [InlineData(ElementType.Object)]
        [InlineData(ElementType.NonGeneric)]
        [InlineData(ElementType.Generic)]
        public void CanReadArrayThatContainsMemberPrimitiveTypedRecord_SZ(ElementType elementType)
        {
            if (elementType != ElementType.Object && !IsPatched)
            {
                throw new SkipTestException("Current machine has not been patched with the most recent BinaryFormatter fix.");
            }

            const int Integer = 123;
            Array input = elementType switch
            {
                ElementType.Object => new object[] { Integer },
                ElementType.NonGeneric => new IComparable[] { Integer },
                ElementType.Generic => new IComparable<int>[] { Integer },
                _ => throw new InvalidOperationException()
            };

            SZArrayRecord<SerializationRecord> arrayRecord = (SZArrayRecord<SerializationRecord>)NrbfDecoder.Decode(Serialize(input), out var recordMap);
            SerializationRecord[] output = arrayRecord.GetArray();

            Verify(input, arrayRecord, output, recordMap);
            PrimitiveTypeRecord<int> intRecord = (PrimitiveTypeRecord<int>)output.Single();
            Assert.Equal(Integer, intRecord.Value);
        }

        [ConditionalTheory]
        [InlineData(ElementType.Object)]
        [InlineData(ElementType.NonGeneric)]
        [InlineData(ElementType.Generic)]
        public void CanReadArrayThatContainsMemberPrimitiveTypedRecord_MD(ElementType elementType)
        {
            if (elementType != ElementType.Object && !IsPatched)
            {
                throw new SkipTestException("Current machine has not been patched with the most recent BinaryFormatter fix.");
            }

            const int Integer = 123;
            Array input = elementType switch
            {
                ElementType.Object => new object[1, 1],
                ElementType.NonGeneric => new IComparable[1, 1],
                ElementType.Generic => new IComparable<int>[1, 1],
                _ => throw new InvalidOperationException()
            };
            input.SetValue(Integer, 0, 0);

            ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input), out var recordMap);
            SerializationRecord[,] output = (SerializationRecord[,])arrayRecord.GetArray(input.GetType());

            Verify(input, arrayRecord, output, recordMap);
            PrimitiveTypeRecord<int> intRecord = (PrimitiveTypeRecord<int>)output[0, 0];
            Assert.Equal(Integer, intRecord.Value);
        }

        [ConditionalTheory]
        [InlineData(ElementType.Object)]
        [InlineData(ElementType.NonGeneric)]
        [InlineData(ElementType.Generic)]
        public void CanReadArrayThatContainsMemberPrimitiveTypedRecord_Jagged(ElementType elementType)
        {
            if (elementType != ElementType.Object && !IsPatched)
            {
                throw new SkipTestException("Current machine has not been patched with the most recent BinaryFormatter fix.");
            }

            const int Integer = 123;
            Array input = elementType switch
            {
                ElementType.Object => new object[1][] { [Integer] },
                ElementType.NonGeneric => new IComparable[1][] { [Integer] },
                ElementType.Generic => new IComparable<int>[1][] { [Integer] },
                _ => throw new InvalidOperationException()
            };

            ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input), out var recordMap);
            ArrayRecord[] output = (ArrayRecord[])arrayRecord.GetArray(input.GetType());

            Verify(input, arrayRecord, output, recordMap);
            SZArrayRecord<SerializationRecord> contained = (SZArrayRecord<SerializationRecord>)output.Single();
            PrimitiveTypeRecord<int> intRecord = (PrimitiveTypeRecord<int>)contained.GetArray().Single();
            Assert.Equal(Integer, intRecord.Value);
        }

        public static IEnumerable<object[]> NullAndArrayPermutations()
        {
            foreach (ElementType elementType in Enum.GetValues(typeof(ElementType)))
            {
                yield return new object[] { elementType, 1 }; // ObjectNullRecord
                yield return new object[] { elementType, 200 }; // ObjectNullMultiple256Record
                yield return new object[] { elementType, 1_000 }; // ObjectNullMultipleRecord
            }
        }

        [Theory]
        [MemberData(nameof(NullAndArrayPermutations))]
        public void CanReadArrayThatContainsNullRecords_SZ(ElementType elementType, int nullCount)
        {
            const string Text = "notNull";
            Array input = elementType switch
            {
                ElementType.Object => new object[nullCount + 1],
                ElementType.NonGeneric => new IEnumerable[nullCount + 1],
                ElementType.Generic => new IEnumerable<char>[nullCount + 1],
                _ => throw new InvalidOperationException()
            };
            input.SetValue(Text, nullCount);

            SZArrayRecord<SerializationRecord> arrayRecord = (SZArrayRecord<SerializationRecord>)NrbfDecoder.Decode(Serialize(input), out var recordMap);
            SerializationRecord?[] output = arrayRecord.GetArray();

            Verify(input, arrayRecord, output, recordMap);
            Assert.All(output.Take(nullCount), Assert.Null);
            PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)output[nullCount];
            Assert.Equal(Text, stringRecord.Value);
        }

        [Theory]
        [MemberData(nameof(NullAndArrayPermutations))]
        public void CanReadArrayThatContainsNullRecords_MD(ElementType elementType, int nullCount)
        {
            const string Text = "notNull";
            Array input = elementType switch
            {
                ElementType.Object => new object[1, nullCount + 1],
                ElementType.NonGeneric => new IEnumerable[1, nullCount + 1],
                ElementType.Generic => new IEnumerable<char>[1, nullCount + 1],
                _ => throw new InvalidOperationException()
            };
            input.SetValue(Text, 0, nullCount);

            ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input), out var recordMap);
            SerializationRecord[,] output = (SerializationRecord[,])arrayRecord.GetArray(input.GetType());

            Verify(input, arrayRecord, output, recordMap);
            for (int i = 0; i < nullCount; i++)
            {
                Assert.Null(output[0, i]);
            }
            PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)output[0, nullCount];
            Assert.Equal(Text, stringRecord.Value);
        }

        [Theory]
        [MemberData(nameof(NullAndArrayPermutations))]
        public void CanReadArrayThatContainsNullRecords_Jagged(ElementType elementType, int nullCount)
        {
            const string Text = "notNull";
            Array input = elementType switch
            {
                ElementType.Object => new object[1][] { new object[nullCount + 1] },
                ElementType.NonGeneric => new IEnumerable[1][]  { new IEnumerable[nullCount + 1] },
                ElementType.Generic => new IEnumerable<char>[1][] { new IEnumerable<char>[nullCount + 1] },
                _ => throw new InvalidOperationException()
            };
            ((Array)input.GetValue(0)).SetValue(Text, nullCount);

            ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input), out var recordMap);
            ArrayRecord[] output = (ArrayRecord[])arrayRecord.GetArray(input.GetType());

            Verify(input, arrayRecord, output, recordMap);
            SZArrayRecord<SerializationRecord> contained = (SZArrayRecord<SerializationRecord>)output.Single();
            Assert.All(contained.GetArray().Take(nullCount), Assert.Null);
            PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)contained.GetArray()[nullCount];
            Assert.Equal(Text, stringRecord.Value);
        }

        [Theory]
        [InlineData(ElementType.Object)]
        [InlineData(ElementType.NonGeneric)]
        [InlineData(ElementType.Generic)]
        public void CanReadArrayThatContainsArrayRecord_SZ(ElementType elementType)
        {
            int[] intArray = [1, 2, 3];
            Array input = elementType switch
            {
                ElementType.Object => new object[] { intArray },
                ElementType.NonGeneric => new IEnumerable[] { intArray },
                ElementType.Generic => new IEnumerable<int>[] { intArray },
                _ => throw new InvalidOperationException()
            };

            SZArrayRecord<SerializationRecord> arrayRecord = (SZArrayRecord<SerializationRecord>)NrbfDecoder.Decode(Serialize(input), out var recordMap);
            SerializationRecord[] output = arrayRecord.GetArray();

            Verify(input, arrayRecord, output, recordMap);
            SZArrayRecord<int> intArrayRecord = (SZArrayRecord<int>)output.Single();
            Assert.Equal(intArray, intArrayRecord.GetArray());
        }

        [Theory]
        [InlineData(ElementType.Object)]
        [InlineData(ElementType.NonGeneric)]
        [InlineData(ElementType.Generic)]
        public void CanReadArrayThatContainsArrayRecord_MD(ElementType elementType)
        {
            int[] intArray = [1, 2, 3];
            Array input = elementType switch
            {
                ElementType.Object => new object[1, 1],
                ElementType.NonGeneric => new IEnumerable[1, 1],
                ElementType.Generic => new IEnumerable<int>[1, 1],
                _ => throw new InvalidOperationException()
            };
            input.SetValue(intArray, 0, 0);

            ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input), out var recordMap);
            SerializationRecord[,] output = (SerializationRecord[,])arrayRecord.GetArray(input.GetType());

            Verify(input, arrayRecord, output, recordMap);
            SZArrayRecord<int> intArrayRecord = (SZArrayRecord<int>)output[0, 0];
            Assert.Equal(intArray, intArrayRecord.GetArray());
        }

        [Theory]
        [InlineData(ElementType.Object)]
        [InlineData(ElementType.NonGeneric)]
        [InlineData(ElementType.Generic)]
        public void CanReadArrayThatContainsArrayRecord_Jagged(ElementType elementType)
        {
            int[] intArray = [1, 2, 3];
            Array input = elementType switch
            {
                ElementType.Object => new object[1][] { [intArray] },
                ElementType.NonGeneric => new IEnumerable[1][] { [intArray] },
                ElementType.Generic => new IEnumerable<int>[1][] { [intArray] },
                _ => throw new InvalidOperationException()
            };

            ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input), out var recordMap);
            ArrayRecord[] output = (ArrayRecord[])arrayRecord.GetArray(input.GetType());

            Verify(input, arrayRecord, output, recordMap);
            SZArrayRecord<SerializationRecord> contained = (SZArrayRecord<SerializationRecord>)output.Single();
            SZArrayRecord<int> intArrayRecord = (SZArrayRecord<int>)contained.GetArray().Single();
            Assert.Equal(intArray, intArrayRecord.GetArray());
        }

        [Theory]
        [InlineData(ElementType.Object)]
        [InlineData(ElementType.NonGeneric)]
        public void CanReadArrayThatContainsAllRecordTypes_SZ(ElementType elementType)
        {
            const string Text = "hello";
            int[] intArray = [1, 2, 3];
            CustomClassThatImplementsIEnumerable classThatImplementsIEnumerable = new() { Field = 456 };
            Array input = elementType switch
            {
                ElementType.Object => new object[]
                {
                    Text, // BinaryObjectStringRecord
                    intArray, // ArraySinglePrimitiveRecord
                    classThatImplementsIEnumerable, // ClassWithMembersAndTypesRecord,
                    null // ObjectNullRecord
                },
                ElementType.NonGeneric => new IEnumerable[] { Text, intArray, classThatImplementsIEnumerable, null },
                _ => throw new InvalidOperationException()
            };

            SZArrayRecord<SerializationRecord> arrayRecord = (SZArrayRecord<SerializationRecord>)NrbfDecoder.Decode(Serialize(input), out var recordMap);
            SerializationRecord[] output = arrayRecord.GetArray();

            Verify(input, arrayRecord, output, recordMap);
            PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)output[0];
            Assert.Equal(Text, stringRecord.Value);
            SZArrayRecord<int> intArrayRecord = (SZArrayRecord<int>)output[1];
            Assert.Equal(intArray, intArrayRecord.GetArray());
            ClassRecord classRecord = (ClassRecord)output[2];
            Assert.Equal(classThatImplementsIEnumerable.Field, classRecord.GetInt32(nameof(CustomClassThatImplementsIEnumerable.Field)));
            Assert.Null(output[3]);
        }

        [Theory]
        [InlineData(ElementType.Object)]
        [InlineData(ElementType.NonGeneric)]
        public void CanReadArrayThatContainsAllRecordTypes_MD(ElementType elementType)
        {
            const string Text = "hello";
            int[] intArray = [1, 2, 3];
            CustomClassThatImplementsIEnumerable classThatImplementsIEnumerable = new() { Field = 456 };

            Array input = elementType switch
            {
                ElementType.Object => new object[1, 4],
                ElementType.NonGeneric => new IEnumerable[1, 4],
                _ => throw new InvalidOperationException()
            };
            input.SetValue(Text, 0, 0);
            input.SetValue(intArray, 0, 1);
            input.SetValue(classThatImplementsIEnumerable, 0, 2);
            input.SetValue(null, 0, 3);

            ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input), out var recordMap);
            SerializationRecord[,] output = (SerializationRecord[,])arrayRecord.GetArray(input.GetType());

            Verify(input, arrayRecord, output, recordMap);
            PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)output[0, 0];
            Assert.Equal(Text, stringRecord.Value);
            SZArrayRecord<int> intArrayRecord = (SZArrayRecord<int>)output[0, 1];
            Assert.Equal(intArray, intArrayRecord.GetArray());
            ClassRecord classRecord = (ClassRecord)output[0, 2];
            Assert.Equal(classThatImplementsIEnumerable.Field, classRecord.GetInt32(nameof(CustomClassThatImplementsIEnumerable.Field)));
            Assert.Null(output[0, 3]);
        }

        [Theory]
        [InlineData(ElementType.Object)]
        [InlineData(ElementType.NonGeneric)]
        public void CanReadArrayThatContainsAllRecordTypes_Jagged(ElementType elementType)
        {
            const string Text = "hello";
            int[] intArray = [1, 2, 3];
            CustomClassThatImplementsIEnumerable classThatImplementsIEnumerable = new() { Field = 456 };

            Array input = elementType switch
            {
                ElementType.Object => new object[1][] { [Text, intArray, classThatImplementsIEnumerable, null] },
                ElementType.NonGeneric => new IEnumerable[1][] { [Text, intArray, classThatImplementsIEnumerable, null] },
                _ => throw new InvalidOperationException()
            };

            ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input), out var recordMap);
            ArrayRecord[] output = (ArrayRecord[])arrayRecord.GetArray(input.GetType());

            Verify(input, arrayRecord, output, recordMap);
            SZArrayRecord<SerializationRecord> contained = (SZArrayRecord<SerializationRecord>)output.Single();
            SerializationRecord[] records = contained.GetArray();
            PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)records[0];
            Assert.Equal(Text, stringRecord.Value);
            SZArrayRecord<int> intArrayRecord = (SZArrayRecord<int>)records[1];
            Assert.Equal(intArray, intArrayRecord.GetArray());
            ClassRecord classRecord = (ClassRecord)records[2];
            Assert.Equal(classThatImplementsIEnumerable.Field, classRecord.GetInt32(nameof(CustomClassThatImplementsIEnumerable.Field)));
            Assert.Null(records[3]);
        }

        [Theory]
        [InlineData(ElementType.Object)]
        [InlineData(ElementType.NonGeneric)]
        public void CanReadArrayThatContainsAllRecordTypes_Jagged_MD(ElementType elementType)
        {
            const string Text = "hello";
            int[] intArray = [1, 2, 3];
            CustomClassThatImplementsIEnumerable classThatImplementsIEnumerable = new() { Field = 456 };

            Array input = elementType switch
            {
                ElementType.Object => new object[1, 1][,],
                ElementType.NonGeneric => new IEnumerable[1, 1][,],
                _ => throw new InvalidOperationException()
            };
            Array contained = elementType switch
            {
                ElementType.Object => new object[2, 2],
                ElementType.NonGeneric => new IEnumerable[2, 2],
                _ => throw new InvalidOperationException()
            };
            contained.SetValue(Text, 0, 0);
            contained.SetValue(intArray, 0, 1);
            contained.SetValue(classThatImplementsIEnumerable, 1, 0);
            input.SetValue(contained, 0, 0);

            ArrayRecord arrayRecord = (ArrayRecord)NrbfDecoder.Decode(Serialize(input), out var recordMap);
            ArrayRecord[,] output = (ArrayRecord[,])arrayRecord.GetArray(input.GetType());

            Verify(input, arrayRecord, output, recordMap);
            SerializationRecord[,] records = (SerializationRecord[,])output[0, 0].GetArray(contained.GetType());
            PrimitiveTypeRecord<string> stringRecord = (PrimitiveTypeRecord<string>)records[0, 0];
            Assert.Equal(Text, stringRecord.Value);
            SZArrayRecord<int> intArrayRecord = (SZArrayRecord<int>)records[0, 1];
            Assert.Equal(intArray, intArrayRecord.GetArray());
            ClassRecord classRecord = (ClassRecord)records[1, 0];
            Assert.Equal(classThatImplementsIEnumerable.Field, classRecord.GetInt32(nameof(CustomClassThatImplementsIEnumerable.Field)));
            Assert.Null(records[1, 1]);
        }

        [Fact]
        public void TypeMismatch()
        {
            // An array of strings that contains non-string.
            byte[] bytes = Convert.FromBase64String("AAEAAAD/////AQAAAAAAAAAHAQAAAAICAAAAAQAAAAEAAAABCQEAAAAL");

            ArrayRecord arrRecord = (ArrayRecord)NrbfDecoder.Decode(new MemoryStream(bytes));

            Assert.Throws<SerializationException>(() => arrRecord.GetArray(typeof(string[,])));
        }

        private static void Verify(Array input, ArrayRecord arrayRecord, Array output,
            IReadOnlyDictionary<SerializationRecordId, SerializationRecord> recordMap)
        {
            Assert.Equal(input.Rank, arrayRecord.Rank);
            Assert.Equal(input.Rank, output.Rank);

            for (int i = 0; i < input.Rank; i++)
            {
                Assert.Equal(input.GetLength(i), arrayRecord.Lengths[i]);
                Assert.Equal(input.GetLength(i), output.GetLength(i));
            }

            foreach (object? recordOrNull in output)
            {
                if (recordOrNull is SerializationRecord record && !record.Id.Equals(default))
                {
                    // An array of abstractions always uses SystemClassWithMembersAndTypesRecord to represent primitive values.
                    // This requires some non-trivial mapping and we need to ensure that it's reflected not only in what
                    // has been stored in the array, but also in the record map.
                    Assert.Same(record, recordMap[record.Id]);
                }
            }
        }
    }
}
