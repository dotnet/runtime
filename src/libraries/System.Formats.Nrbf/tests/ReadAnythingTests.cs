using System.Collections.Generic;
using System.IO;
using Xunit;
using System.Linq;
using System.Text.Json;
using System.Reflection;

namespace System.Formats.Nrbf.Tests
{
    public class ReadAnythingTests : ReadTests
    {
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Fails with ValueTuple is not marked as serializable, but only in this repo")]
        public void UserCanReadAnyValidInputAndCheckTypesUsingStronglyTypedTypeInstances()
        {
            Dictionary<string, object> input = new()
            {
                { "exception", new Exception("test") },
                { "struct", new ValueTuple<bool, int>(true, 123) },
                { "generic", new List<int>(){ 1, 2, 3, 4 } }
            };

            using MemoryStream stream = Serialize(input);

            SerializationRecord topLevel = NrbfDecoder.Decode(stream);

            Assert.IsAssignableFrom<ClassRecord>(topLevel);
            ClassRecord dictionaryRecord = (ClassRecord)topLevel;
            // this innocent line tests type forwards support ;)
            Assert.True(dictionaryRecord.TypeNameMatches(typeof(Dictionary<string, object>)));

            ClassRecord comparerRecord = dictionaryRecord.GetClassRecord(nameof(input.Comparer))!;
            Assert.True(comparerRecord.TypeNameMatches(input.Comparer.GetType()));

            SZArrayRecord<ClassRecord> arrayRecord = (SZArrayRecord<ClassRecord>)dictionaryRecord.GetSerializationRecord("KeyValuePairs")!;
            ClassRecord[] keyValuePairs = arrayRecord.GetArray()!;
            Assert.True(keyValuePairs[0].TypeNameMatches(typeof(KeyValuePair<string, object>)));

            ClassRecord exceptionPair = Find(keyValuePairs, "exception");
            ClassRecord exceptionValue = exceptionPair.GetClassRecord("value")!;
            Assert.True(exceptionValue.TypeNameMatches(typeof(Exception)));
            Assert.Equal("test", exceptionValue.GetString(nameof(Exception.Message)));

            ClassRecord structPair = Find(keyValuePairs, "struct");
            ClassRecord structValue = structPair.GetClassRecord("value")!;
            Assert.True(structValue.TypeNameMatches(typeof(ValueTuple<bool, int>)));
            Assert.True(structValue.GetBoolean("Item1"));
            Assert.Equal(123, structValue.GetInt32("Item2"));

            ClassRecord genericPair = Find(keyValuePairs, "generic");
            ClassRecord genericValue = genericPair.GetClassRecord("value")!;
            Assert.True(genericValue.TypeNameMatches(typeof(List<int>)));
            Assert.Equal(4, genericValue.GetInt32("_size"));
            Assert.Equal(new int[] { 1, 2, 3, 4 }, ((SZArrayRecord<int>)genericValue.GetArrayRecord("_items")).GetArray());

            static ClassRecord Find(ClassRecord[] keyValuePairs, string key)
                => keyValuePairs.Where(pair => pair.GetString("key") == key).Single();
        }

        public static IEnumerable<object[]> GetAllInputTypes()
        {
            yield return new object[] { "string" };
            yield return new object[] { true };
            yield return new object[] { byte.MaxValue };
            yield return new object[] { sbyte.MaxValue };
            yield return new object[] { short.MaxValue };
            yield return new object[] { ushort.MaxValue };
            yield return new object[] { int.MaxValue };
            yield return new object[] { uint.MaxValue };
            yield return new object[] { long.MaxValue };
            yield return new object[] { ulong.MaxValue };
            yield return new object[] { float.MaxValue };
            yield return new object[] { double.MaxValue };
            yield return new object[] { decimal.MaxValue };
            yield return new object[] { TimeSpan.MaxValue };
            yield return new object[] { new DateTime(2000, 01, 01) };
            yield return new object[] { new Exception("SystemType") };
            yield return new object[] { new[] { "string" } };
            yield return new object[] { new[] { true } };
            yield return new object[] { new[] { byte.MaxValue } };
            yield return new object[] { new[] { sbyte.MaxValue } };
            yield return new object[] { new[] { short.MaxValue } };
            yield return new object[] { new[] { ushort.MaxValue } };
            yield return new object[] { new[] { int.MaxValue } };
            yield return new object[] { new[] { uint.MaxValue } };
            yield return new object[] { new[] { long.MaxValue } };
            yield return new object[] { new[] { ulong.MaxValue } };
            yield return new object[] { new[] { float.MaxValue } };
            yield return new object[] { new[] { double.MaxValue } };
            yield return new object[] { new[] { decimal.MaxValue } };
            yield return new object[] { new[] { TimeSpan.MaxValue } };
            yield return new object[] { new[] { new DateTime(2000, 01, 01) } };
            yield return new object[] { new[] { new Exception("SystemType") } };
            // BinaryArrayRecord with BinaryType.SystemClass item that contains BinaryLibraryRecord
            yield return new object[] { new Dictionary<int, NonSystemPoint>()
            {
                { 1, new NonSystemPoint(1, 1) },
                { 2, new NonSystemPoint(2, 2) }
            }};
            // ClassWithMembersAndTypesRecord that contains MemberPrimitiveTypedRecord
            yield return new object[] { new JsonException("message", path: "path", lineNumber: 1, bytePositionInLine: 2) };
            // More than one BinaryArrayRecord in a row
            yield return new object[] { new Dictionary<NonSystemPoint, JsonException>()
            {
                { new NonSystemPoint(1, 1), new JsonException("message") },
                { new NonSystemPoint(2, 2), new JsonException("message") }
            }};
            yield return new object[] { new int?[] { 1, 2, 3, null } };
            // Class with no members
            yield return new object[] { new EmptyClass() };
            // Empty arrays of class with no members
            yield return new object[] { new EmptyClass[0] };
            yield return new object[] { new EmptyClass[0, 0] };
            yield return new object[] { new EmptyClass[0][] };
            yield return new object[] { new EmptyClass[0][,] };
        }

        [Theory]
        [MemberData(nameof(GetAllInputTypes))]
        public void UserCanReadEveryPossibleSerializationRecord(object input)
        {
            SerializationRecord root = NrbfDecoder.Decode(Serialize(input));

            switch(root)
            {
                // primitive types
                case PrimitiveTypeRecord<string> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<bool> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<byte> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<sbyte> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<char> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<short> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<ushort> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<int> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<uint> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<long> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<ulong> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<float> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<double> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<decimal> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<DateTime> record:
                    Assert.Equal(input, record.Value);
                    break;
                case PrimitiveTypeRecord<TimeSpan> record:
                    Assert.Equal(input, record.Value);
                    break;
                // arrays of primitive types
                case SZArrayRecord<string> record:
                    Assert.Equal(input, record.GetArray());
                    break;
                case SZArrayRecord<bool> record:
                    Assert.Equal(input, record.GetArray());
                    break;
                case SZArrayRecord<byte> record:
                    Assert.Equal(input, record.GetArray());
                    break;
                case SZArrayRecord<sbyte> record:
                    Assert.Equal(input, record.GetArray());
                    break;
                case SZArrayRecord<char> record:
                    Assert.Equal(input, record.GetArray());
                    break;
                case SZArrayRecord<short> record:
                    Assert.Equal(input, record.GetArray());
                    break;
                case SZArrayRecord<ushort> record:
                    Assert.Equal(input, record.GetArray());
                    break;
                case SZArrayRecord<int> record:
                    Assert.Equal(input, record.GetArray());
                    break;
                case SZArrayRecord<uint> record:
                    Assert.Equal(input, record.GetArray());
                    break;
                case SZArrayRecord<long> record:
                    Assert.Equal(input, record.GetArray());
                    break;
                case SZArrayRecord<ulong> record:
                    Assert.Equal(input, record.GetArray());
                    break;
                case SZArrayRecord<float> record:
                    Assert.Equal(input, record.GetArray());
                    break;
                case SZArrayRecord<double> record:
                    Assert.Equal(input, record.GetArray());
                    break;
                case SZArrayRecord<decimal> record:
                    Assert.Equal(input, record.GetArray());
                    break;
                case SZArrayRecord<DateTime> record:
                    Assert.Equal(input, record.GetArray());
                    break;
                case SZArrayRecord<TimeSpan> record:
                    Assert.Equal(input, record.GetArray());
                    break;
                // class records
                case ClassRecord record when record.TypeNameMatches(typeof(Exception)):
                    Assert.Equal(((Exception)input).Message, record.GetString("Message"));
                    break;
                case SZArrayRecord<ClassRecord> record when record.TypeNameMatches(typeof(Exception[])):
                    Assert.Equal(((Exception[])input)[0].Message, record.GetArray()[0]!.GetString("Message"));
                    break;
                case ClassRecord record when record.TypeNameMatches(typeof(JsonException)):
                    Assert.Equal(((JsonException)input).Message, record.GetString("Message"));
                    break;
                case ClassRecord record when record.TypeNameMatches(typeof(Dictionary<int, NonSystemPoint>)):
                    VerifyDictionary<int, NonSystemPoint>(record);
                    break;
                case ClassRecord record when record.TypeNameMatches(typeof(Dictionary<NonSystemPoint, JsonException>)):
                    VerifyDictionary<NonSystemPoint, JsonException>(record);
                    break;
                case ClassRecord record when record.TypeNameMatches(typeof(EmptyClass)):
                    Assert.Empty(record.MemberNames);
                    break;
                case ArrayRecord arrayRecord when arrayRecord.TypeNameMatches(typeof(int?[])):
                    Assert.Equal(input, arrayRecord.GetArray(typeof(int?[])));
                    break;
                case ArrayRecord arrayRecord when arrayRecord.TypeNameMatches(typeof(EmptyClass[])):
                    Assert.Equal(0, arrayRecord.Lengths.ToArray().Single());
                    break;
                case ArrayRecord arrayRecord when arrayRecord.TypeNameMatches(typeof(EmptyClass[,])):
                    Assert.Equal(new int[2] { 0, 0 }, arrayRecord.Lengths.ToArray());
                    break;
                case ArrayRecord arrayRecord when arrayRecord.TypeNameMatches(typeof(EmptyClass[][])):
                    Assert.Equal(0, arrayRecord.Lengths.ToArray().Single());
                    break;
                case ArrayRecord arrayRecord when arrayRecord.TypeNameMatches(typeof(EmptyClass[][,])):
                    Assert.Equal(0, arrayRecord.Lengths.ToArray().Single());
                    break;
                default:
                    Assert.Fail($"All cases should be handled! Record was {root.GetType()}, input was {input.GetType()}");
                    break;
            }

            static void VerifyDictionary<TKey, TValue>(ClassRecord record)
            {
                SZArrayRecord<ClassRecord> arrayRecord = (SZArrayRecord<ClassRecord>)record.GetSerializationRecord("KeyValuePairs")!;
                ClassRecord[] keyValuePairs = arrayRecord.GetArray()!;
                Assert.True(keyValuePairs[0].TypeNameMatches(typeof(KeyValuePair<TKey, TValue>)));
            }
        }
    }

    [Serializable]
    public class NonSystemPoint : IComparable<NonSystemPoint>, IEquatable<NonSystemPoint>
    {
        public int X;
        public int Y;

        public NonSystemPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int CompareTo(object obj)
        {

            return CompareTo(obj as NonSystemPoint);
        }

        public int CompareTo(NonSystemPoint other)
        {
            return other is null ? 1 : 0;
        }

        public override bool Equals(object obj) => Equals(obj as NonSystemPoint);

        public bool Equals(NonSystemPoint other)
        {
            return other is not null &&
                X == other.X &&
                Y == other.Y;
        }

        public override int GetHashCode() => 1;
    }

    [Serializable]
    public class EmptyClass
    {
    }
}
