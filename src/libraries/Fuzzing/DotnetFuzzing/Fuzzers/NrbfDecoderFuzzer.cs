// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Formats.Nrbf;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace DotnetFuzzing.Fuzzers
{
    internal sealed class NrbfDecoderFuzzer : IFuzzer
    {
        public string[] TargetAssemblies { get; } = ["System.Formats.Nrbf"];

        public string[] TargetCoreLibPrefixes => [];

        public string Dictionary => "nrbfdecoder.dict";

        public void FuzzTarget(ReadOnlySpan<byte> bytes)
        {
            Test(bytes, PoisonPagePlacement.Before);
            Test(bytes, PoisonPagePlacement.After);
        }

        private static void Test(ReadOnlySpan<byte> bytes, PoisonPagePlacement poisonPagePlacement)
        {
            using PooledBoundedMemory<byte> inputPoisoned = PooledBoundedMemory<byte>.Rent(bytes, poisonPagePlacement);

            using MemoryStream seekableStream = new(inputPoisoned.Memory.ToArray());
            Test(inputPoisoned.Span, seekableStream);

            // NrbfDecoder has few code paths dedicated to non-seekable streams, let's test them as well.
            using NonSeekableStream nonSeekableStream = new(inputPoisoned.Memory.ToArray());
            Test(inputPoisoned.Span, nonSeekableStream);
        }

        private static void Test(Span<byte> testSpan, Stream stream)
        {
            if (NrbfDecoder.StartsWithPayloadHeader(testSpan))
            {
                HashSet<SerializationRecordId> visited = new();
                Queue<SerializationRecord> queue = new();
                try
                {
                    SerializationRecord record = NrbfDecoder.Decode(stream, out IReadOnlyDictionary<SerializationRecordId, SerializationRecord> recordMap);

                    Assert.Equal(true, recordMap.ContainsKey(record.Id)); // make sure the loop below includes it
                    foreach (SerializationRecord fromMap in recordMap.Values)
                    {
                        visited.Add(fromMap.Id);
                        queue.Enqueue(fromMap);
                    }
                }
                catch (SerializationException) { /* Reading from the stream encountered invalid NRBF data.*/ }
                catch (NotSupportedException) { /* Reading from the stream encountered unsupported records */ }
                catch (DecoderFallbackException) { /* Reading from the stream encountered an invalid UTF8 sequence. */ }
                catch (EndOfStreamException) { /* The end of the stream was reached before reading SerializationRecordType.MessageEnd record. */ }
                catch (IOException) { /* An I/O error occurred. */ }

                // Lets consume it outside of the try/catch block to not swallow any exceptions by accident.
                Consume(visited, queue);
            }
            else
            {
                try
                {
                    NrbfDecoder.Decode(stream);
                    throw new Exception("Decoding supposed to fail!");
                }
                catch (SerializationException) { /* Everything has to start with a header */ }
                catch (NotSupportedException) { /* Reading from the stream encountered unsupported records */ }
                catch (EndOfStreamException) { /* The end of the stream was reached before reading SerializationRecordType.MessageEnd record. */ }
            }
        }

        private static void Consume(HashSet<SerializationRecordId> visited, Queue<SerializationRecord> queue)
        {
            while (queue.Count > 0)
            {
                SerializationRecord serializationRecord = queue.Dequeue();

                if (serializationRecord is PrimitiveTypeRecord primitiveTypeRecord)
                {
                    ConsumePrimitiveValue(primitiveTypeRecord.Value);
                }
                else if (serializationRecord is ClassRecord classRecord)
                {
                    foreach (string memberName in classRecord.MemberNames)
                    {
                        ConsumePrimitiveValue(memberName);

                        Assert.Equal(true, classRecord.HasMember(memberName));

                        object? rawValue;

                        try
                        {
                            rawValue = classRecord.GetRawValue(memberName);
                        }
                        catch (SerializationException ex) when (ex.Message == "Invalid member reference.")
                        {
                            // It was a reference to a non-existing record, just continue.
                            continue;
                        }
                        
                        if (rawValue is not null)
                        {
                            if (rawValue is SerializationRecord nestedRecord)
                            {
                                TryEnqueue(nestedRecord);
                            }
                            else
                            {
                                ConsumePrimitiveValue(rawValue);
                            }    
                        }
                    }
                }
                else if (serializationRecord is ArrayRecord arrayRecord)
                {
                    Type? type;

                    try
                    {
                        // THIS IS VERY BAD IDEA FOR ANY KIND OF PRODUCT CODE!!
                        // IT'S USED ONLY FOR THE PURPOSE OF TESTING, DO NOT COPY IT.
                        type = Type.GetType(arrayRecord.TypeName.AssemblyQualifiedName, throwOnError: false);
                        if (type is null)
                        {
                            continue;
                        }
                    }
                    catch (Exception) // throwOnError passed to GetType does not prevent from all kinds of exceptions
                    {
                        // It was some type made up by the Fuzzer.
                        // Since it's currently impossible to get the array without providing the type,
                        // we just bail here (in the future we may add an enumerator to ArrayRecord).
                        continue;
                    }

                    Array? array;
                    try
                    {
                        array = arrayRecord.GetArray(type);
                    }
                    catch (SerializationException ex) when (ex.Message == "Invalid member reference.")
                    {
                        // It contained a reference to a non-existing record, just continue.
                        continue;
                    }

                    ReadOnlySpan<int> lengths = arrayRecord.Lengths;
                    long totalElementsCount = 1;
                    for (int i = 0; i < arrayRecord.Rank; i++)
                    {
                        Assert.Equal(lengths[i], array.GetLength(i));
                        totalElementsCount *= lengths[i];
                    }

                    // This array contains indices that are used to get values of multi-dimensional array.
                    // At the beginning, all values are set to 0, so we start from the first element.
                    int[] indices = new int[arrayRecord.Rank];

                    long flatIndex = 0;
                    for (; flatIndex < totalElementsCount; flatIndex++)
                    {
                        object? rawValue = array.GetValue(indices);
                        if (rawValue is not null)
                        {
                            if (rawValue is SerializationRecord record)
                            {
                                TryEnqueue(record);
                            }
                            else
                            {
                                ConsumePrimitiveValue(rawValue);
                            }
                        }

                        // The loop below is responsible for incrementing the multi-dimensional indices.
                        // It finds the dimension and then performs an increment.
                        int dimension = indices.Length - 1;
                        while (dimension >= 0)
                        {
                            indices[dimension]++;
                            if (indices[dimension] < lengths[dimension])
                            {
                                break;
                            }
                            indices[dimension] = 0;
                            dimension--;
                        }
                    }

                    // We track the flat index to ensure that we have enumerated over all elements.
                    Assert.Equal(totalElementsCount, flatIndex);
                }
                else
                {
                    // The map may currently contain it (it may change in the future)
                    Assert.Equal(SerializationRecordType.BinaryLibrary, serializationRecord.RecordType);
                }
            }

            void TryEnqueue(SerializationRecord record)
            {
                if (visited.Add(record.Id)) // avoid unbounded recursion
                {
                    queue.Enqueue(record);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ConsumePrimitiveValue(object value)
        {
            if (value is string text)
                Assert.Equal(text, text.ToString()); // we want to touch all elements to see if memory is not corrupted
            else if (value is bool boolean)
                Assert.Equal(true, Unsafe.BitCast<bool, byte>(boolean) is 1 or 0); // other values are illegal!!
            else if (value is sbyte @sbyte)
                TestNumber(@sbyte);
            else if (value is byte @byte)
                TestNumber(@byte);
            else if (value is char character)
                TestNumber(character);
            else if (value is short @short)
                TestNumber(@short);
            else if (value is ushort @ushort)
                TestNumber(@ushort);
            else if (value is int integer)
                TestNumber(integer);
            else if (value is uint @uint)
                TestNumber(@uint);
            else if (value is long @long)
                TestNumber(@long);
            else if (value is ulong @ulong)
                TestNumber(@ulong);
            else if (value is float @float)
            {
                if (!float.IsNaN(@float) && !float.IsInfinity(@float))
                {
                    TestNumber(@float);
                }
            }
            else if (value is double @double)
            {
                if (!double.IsNaN(@double) && !double.IsInfinity(@double))
                {
                    TestNumber(@double);
                }
            }
            else if (value is decimal @decimal)
                TestNumber(@decimal);
            else if (value is nint @nint)
                TestNumber(@nint);
            else if (value is nuint @nuint)
                TestNumber(@nuint);
            else if (value is DateTime datetime)
                Assert.Equal(true, datetime >= DateTime.MinValue && datetime <= DateTime.MaxValue);
            else if (value is TimeSpan timeSpan)
                Assert.Equal(true, timeSpan >= TimeSpan.MinValue && timeSpan <= TimeSpan.MaxValue);
            else
                throw new InvalidOperationException();

            static void TestNumber<T>(T value) where T : IComparable<T>, IMinMaxValue<T>
            {
                if (value.CompareTo(T.MinValue) < 0)
                {
                    throw new Exception($"Expected {value} to be more or equal {T.MinValue}, {value.CompareTo(T.MinValue)}.");
                }
                if (value.CompareTo(T.MaxValue) > 0)
                {
                    throw new Exception($"Expected {value} to be less or equal {T.MaxValue}, {value.CompareTo(T.MaxValue)}.");
                }
            }
        }

        private sealed class NonSeekableStream : MemoryStream
        {
            public NonSeekableStream(byte[] buffer) : base(buffer) { }
            public override bool CanSeek => false;
        }
    }
}
