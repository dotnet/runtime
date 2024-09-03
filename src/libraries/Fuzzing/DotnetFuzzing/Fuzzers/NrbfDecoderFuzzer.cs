// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Formats.Nrbf;
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
            using PooledBoundedMemory<byte> inputPoisoned = PooledBoundedMemory<byte>.Rent(bytes, PoisonPagePlacement.After);
            using MemoryStream stream = new MemoryStream(inputPoisoned.Memory.ToArray());

            if (NrbfDecoder.StartsWithPayloadHeader(inputPoisoned.Span))
            {
                try
                {
                    SerializationRecord record = NrbfDecoder.Decode(stream, out IReadOnlyDictionary<SerializationRecordId, SerializationRecord> recordMap);
                    switch (record.RecordType)
                    {
                        case SerializationRecordType.ArraySingleObject:
                            SZArrayRecord<object?> arrayObj = (SZArrayRecord<object?>)record;
                            object?[] objArray = arrayObj.GetArray();
                            Assert.Equal(arrayObj.Length, objArray.Length);
                            Assert.Equal(1, arrayObj.Rank);
                            break;
                        case SerializationRecordType.ArraySingleString:
                            SZArrayRecord<string?> arrayString = (SZArrayRecord<string?>)record;
                            string?[] array = arrayString.GetArray();
                            Assert.Equal(arrayString.Length, array.Length);
                            Assert.Equal(1, arrayString.Rank);
                            Assert.Equal(true, arrayString.TypeNameMatches(typeof(string[])));
                            break;
                        case SerializationRecordType.ArraySinglePrimitive:
                        case SerializationRecordType.BinaryArray:
                            ArrayRecord arrayBinary = (ArrayRecord)record;
                            Assert.NotNull(arrayBinary.TypeName);
                            break;
                        case SerializationRecordType.BinaryObjectString:
                            _ = ((PrimitiveTypeRecord<string>)record).Value;
                            break;
                        case SerializationRecordType.ClassWithId:
                        case SerializationRecordType.ClassWithMembersAndTypes:
                        case SerializationRecordType.SystemClassWithMembersAndTypes:
                        {
                            ClassRecord classRecord = (ClassRecord)record;
                            Assert.NotNull(classRecord.TypeName);

                            foreach (string name in classRecord.MemberNames)
                            {
                                Assert.Equal(true, classRecord.HasMember(name));
                            }
                        } break;
                        case SerializationRecordType.MemberPrimitiveTyped:
                            PrimitiveTypeRecord primitiveType = (PrimitiveTypeRecord)record;
                            Assert.NotNull(primitiveType.Value);
                            break;
                        case SerializationRecordType.MemberReference:
                            Assert.NotNull(record.TypeName);
                            break;
                        case SerializationRecordType.BinaryLibrary:
                        case SerializationRecordType.ObjectNull:
                        case SerializationRecordType.ObjectNullMultiple:
                        case SerializationRecordType.ObjectNullMultiple256:
                            Assert.Equal(default, record.Id);
                            break;
                        case SerializationRecordType.MessageEnd:
                        case SerializationRecordType.SerializedStreamHeader:
                        // case SerializationRecordType.ClassWithMembers: will cause NotSupportedException
                        // case SerializationRecordType.SystemClassWithMembers: will cause NotSupportedException
                        default:
                            throw new Exception("Unexpected RecordType");
                    }
                }
                catch (SerializationException) { /* Reading from the stream encountered invalid NRBF data.*/ }
                catch (NotSupportedException) { /* Reading from the stream encountered unsupported records */ }
                catch (DecoderFallbackException) { /* Reading from the stream encountered an invalid UTF8 sequence. */ }
                catch (EndOfStreamException) { /* The end of the stream was reached before reading SerializationRecordType.MessageEnd record. */ }
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
                catch (DecoderFallbackException) { /* Reading from the stream encountered an invalid UTF8 sequence. */ }
                // below exceptions are not expected
                catch (FormatException) { /* Temporarily catch this until its fixed */ }
                catch (IOException) { /* Temporarily catch this until its fixed */ }
            }
        }
    }
}
