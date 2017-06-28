// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Xunit;

namespace System.Runtime.Serialization.Formatters.Tests
{
    public partial class BinaryFormatterTests : RemoteExecutorTestBase
    {
        [Theory]
        [MemberData(nameof(BasicObjectsRoundtrip_MemberData))]
        public void ValidateBasicObjectsRoundtrip(object obj, FormatterAssemblyStyle assemblyFormat, TypeFilterLevel filterLevel, FormatterTypeStyle typeFormat)
        {
            object clone = FormatterClone(obj, null, assemblyFormat, filterLevel, typeFormat);
            if (!ReferenceEquals(obj, string.Empty)) // "" is interned and will roundtrip as the same object
            {
                Assert.NotSame(obj, clone);
            }

            CheckForAnyEquals(obj, clone);
        }

        // Used for updating blobs in BinaryFormatterTestData.cs
        //[Fact]
        public void UpdateBlobs()
        {
            string testDataFilePath = GetTestDataFilePath();
            IEnumerable<object[]> coreTypeRecords = GetCoreTypeRecords();
            string[] coreTypeBlobs = GetCoreTypeBlobs(coreTypeRecords, FormatterAssemblyStyle.Full).ToArray();

            var (numberOfBlobs, numberOfFoundBlobs, numberOfUpdatedBlobs) = UpdateCoreTypeBlobs(testDataFilePath, coreTypeBlobs);
            Console.WriteLine($"{numberOfBlobs} existing blobs" +
                $"{Environment.NewLine}{numberOfFoundBlobs} found blobs with regex search" +
                $"{Environment.NewLine}{numberOfUpdatedBlobs} updated blobs with regex replace");
        }

        [Theory]
        [MemberData(nameof(SerializableObjects_MemberData))]
        public void ValidateAgainstBlobs(object obj, string[] blobs)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("The serializable object must not be null", nameof(obj));
            }

            if (blobs == null || blobs.Length == 0)
            {
                throw new ArgumentOutOfRangeException($"Type {obj} has no blobs to deserialize and test equality against. Blob: " +
                    SerializeObjectToBlob(obj, FormatterAssemblyStyle.Full));
            }

            SanityCheckBlob(obj, blobs);

            foreach (string blob in blobs)
            {
                CheckForAnyEquals(obj, DeserializeBlobToObject(blob, FormatterAssemblyStyle.Simple));
                CheckForAnyEquals(obj, DeserializeBlobToObject(blob, FormatterAssemblyStyle.Full));
            }
        }

        [Fact]
        public void ArraySegmentDefaultCtor()
        {
            // This is workaround for Xunit bug which tries to pretty print test case name and enumerate this object.
            // When inner array is not initialized it throws an exception when this happens.
            object obj = new ArraySegment<int>();
            string corefxBlob = "AAEAAAD/////AQAAAAAAAAAEAQAAAHJTeXN0ZW0uQXJyYXlTZWdtZW50YDFbW1N5c3RlbS5JbnQzMiwgbXNjb3JsaWIsIFZlcnNpb249NC4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1iNzdhNWM1NjE5MzRlMDg5XV0DAAAABl9hcnJheQdfb2Zmc2V0Bl9jb3VudAcAAAgICAoAAAAAAAAAAAs=";
            string netfxBlob = "AAEAAAD/////AQAAAAAAAAAEAQAAAHJTeXN0ZW0uQXJyYXlTZWdtZW50YDFbW1N5c3RlbS5JbnQzMiwgbXNjb3JsaWIsIFZlcnNpb249NC4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1iNzdhNWM1NjE5MzRlMDg5XV0DAAAABl9hcnJheQdfb2Zmc2V0Bl9jb3VudAcAAAgICAoAAAAAAAAAAAs=";
            CheckForAnyEquals(obj, DeserializeBlobToObject(corefxBlob, FormatterAssemblyStyle.Full));
            CheckForAnyEquals(obj, DeserializeBlobToObject(netfxBlob, FormatterAssemblyStyle.Full));
        }

        [Fact]
        public void ValidateDeserializationOfObjectWithDifferentAssemblyVersion()
        {
            // To generate this properly, change AssemblyVersion to a value which is unlikely to happen in production and generate base64(serialized-data)
            // For this test 9.98.7.987 is being used
            var obj = new SomeType() { SomeField = 7 };
            string serializedObj = @"AAEAAAD/////AQAAAAAAAAAMAgAAAHNTeXN0ZW0uUnVudGltZS5TZXJpYWxpemF0aW9uLkZvcm1hdHRlcnMuVGVzdHMsIFZlcnNpb249OS45OC43Ljk4NywgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj05ZDc3Y2M3YWQzOWI2OGViBQEAAAA2U3lzdGVtLlJ1bnRpbWUuU2VyaWFsaXphdGlvbi5Gb3JtYXR0ZXJzLlRlc3RzLlNvbWVUeXBlAQAAAAlTb21lRmllbGQACAIAAAAHAAAACw==";

            var deserialized = (SomeType)DeserializeBlobToObject(serializedObj, FormatterAssemblyStyle.Simple);
            Assert.Equal(obj, deserialized);
        }

        [Fact]
        public void ValidateDeserializationOfObjectWithGenericTypeWhichGenericArgumentHasDifferentAssemblyVersion()
        {
            // To generate this properly, change AssemblyVersion to a value which is unlikely to happen in production and generate base64(serialized-data)
            // For this test 9.98.7.987 is being used
            var obj = new GenericTypeWithArg<SomeType>() { Test = new SomeType() { SomeField = 9 } };
            string serializedObj = @"AAEAAAD/////AQAAAAAAAAAMAgAAAHNTeXN0ZW0uUnVudGltZS5TZXJpYWxpemF0aW9uLkZvcm1hdHRlcnMuVGVzdHMsIFZlcnNpb249OS45OC43Ljk4NywgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj05ZDc3Y2M3YWQzOWI2OGViBQEAAADxAVN5c3RlbS5SdW50aW1lLlNlcmlhbGl6YXRpb24uRm9ybWF0dGVycy5UZXN0cy5HZW5lcmljVHlwZVdpdGhBcmdgMVtbU3lzdGVtLlJ1bnRpbWUuU2VyaWFsaXphdGlvbi5Gb3JtYXR0ZXJzLlRlc3RzLlNvbWVUeXBlLCBTeXN0ZW0uUnVudGltZS5TZXJpYWxpemF0aW9uLkZvcm1hdHRlcnMuVGVzdHMsIFZlcnNpb249OS45OC43Ljk4NywgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj05ZDc3Y2M3YWQzOWI2OGViXV0BAAAABFRlc3QENlN5c3RlbS5SdW50aW1lLlNlcmlhbGl6YXRpb24uRm9ybWF0dGVycy5UZXN0cy5Tb21lVHlwZQIAAAACAAAACQMAAAAFAwAAADZTeXN0ZW0uUnVudGltZS5TZXJpYWxpemF0aW9uLkZvcm1hdHRlcnMuVGVzdHMuU29tZVR5cGUBAAAACVNvbWVGaWVsZAAIAgAAAAkAAAAL";

            var deserialized = (GenericTypeWithArg<SomeType>)DeserializeBlobToObject(serializedObj, FormatterAssemblyStyle.Simple);
            Assert.Equal(obj, deserialized);
        }

        [Theory]
        [MemberData(nameof(SerializableEqualityComparers_MemberData))]
        public void ValidateEqualityComparersAgainstBlobs(object obj, string[] blobs)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("The serializable object must not be null", nameof(obj));
            }

            if (blobs == null || blobs.Length == 0)
            {
                throw new ArgumentOutOfRangeException($"Type {obj} has no blobs to deserialize and test equality against. Blob: " +
                    SerializeObjectToBlob(obj, FormatterAssemblyStyle.Full));
            }

            SanityCheckBlob(obj, blobs);

            foreach (string blob in blobs)
            {
                ValidateEqualityComparer(DeserializeBlobToObject(blob, FormatterAssemblyStyle.Simple));
                ValidateEqualityComparer(DeserializeBlobToObject(blob, FormatterAssemblyStyle.Full));
            }
        }

        [Fact]
        public void RoundtripManyObjectsInOneStream()
        {
            object[][] objects = SerializableObjects_MemberData().ToArray();
            var s = new MemoryStream();
            var f = new BinaryFormatter();

            foreach (object[] obj in objects)
            {
                f.Serialize(s, obj[0]);
            }
            s.Position = 0;
            foreach (object[] obj in objects)
            {
                object clone = f.Deserialize(s);
                CheckForAnyEquals(obj[0], clone);
            }
        }

        [Fact]
        public void SameObjectRepeatedInArray()
        {
            object o = new object();
            object[] arr = new[] { o, o, o, o, o };
            object[] result = FormatterClone(arr);

            Assert.Equal(arr.Length, result.Length);
            Assert.NotSame(arr, result);
            Assert.NotSame(arr[0], result[0]);
            for (int i = 1; i < result.Length; i++)
            {
                Assert.Same(result[0], result[i]);
            }
        }

        [Theory]
        [MemberData(nameof(SerializableExceptions_MemberData))]
        public void Roundtrip_Exceptions(Exception expected)
        {
            BinaryFormatterHelpers.AssertRoundtrips(expected);
        }

        [Theory]
        [MemberData(nameof(NonSerializableTypes_MemberData))]
        public void ValidateNonSerializableTypes(object obj, FormatterAssemblyStyle assemblyFormat, TypeFilterLevel filterLevel, FormatterTypeStyle typeFormat)
        {
            var f = new BinaryFormatter()
            {
                AssemblyFormat = assemblyFormat,
                FilterLevel = filterLevel,
                TypeFormat = typeFormat
            };
            using (var s = new MemoryStream())
            {
                Assert.Throws<SerializationException>(() => f.Serialize(s, obj));
            }
        }

        [Fact]
        public void SerializeNonSerializableTypeWithSurrogate()
        {
            var p = new NonSerializablePair<int, string>() { Value1 = 1, Value2 = "2" };
            Assert.False(p.GetType().IsSerializable);
            Assert.Throws<SerializationException>(() => FormatterClone(p));

            NonSerializablePair<int, string> result = FormatterClone(p, new NonSerializablePairSurrogate());
            Assert.NotSame(p, result);
            Assert.Equal(p.Value1, result.Value1);
            Assert.Equal(p.Value2, result.Value2);
        }

        [Fact]
        public void SerializationEvents_FireAsExpected()
        {
            var f = new BinaryFormatter();

            var obj = new IncrementCountsDuringRoundtrip(null);

            Assert.Equal(0, obj.IncrementedDuringOnSerializingMethod);
            Assert.Equal(0, obj.IncrementedDuringOnSerializedMethod);
            Assert.Equal(0, obj.IncrementedDuringOnDeserializingMethod);
            Assert.Equal(0, obj.IncrementedDuringOnDeserializedMethod);

            using (var s = new MemoryStream())
            {
                f.Serialize(s, obj);
                s.Position = 0;

                Assert.Equal(1, obj.IncrementedDuringOnSerializingMethod);
                Assert.Equal(1, obj.IncrementedDuringOnSerializedMethod);
                Assert.Equal(0, obj.IncrementedDuringOnDeserializingMethod);
                Assert.Equal(0, obj.IncrementedDuringOnDeserializedMethod);

                var result = (IncrementCountsDuringRoundtrip)f.Deserialize(s);

                Assert.Equal(1, obj.IncrementedDuringOnSerializingMethod);
                Assert.Equal(1, obj.IncrementedDuringOnSerializedMethod);
                Assert.Equal(0, obj.IncrementedDuringOnDeserializingMethod);
                Assert.Equal(0, obj.IncrementedDuringOnDeserializedMethod);

                Assert.Equal(1, result.IncrementedDuringOnSerializingMethod);
                Assert.Equal(0, result.IncrementedDuringOnSerializedMethod);
                Assert.Equal(1, result.IncrementedDuringOnDeserializingMethod);
                Assert.Equal(1, result.IncrementedDuringOnDeserializedMethod);
            }
        }

        [Fact]
        public void SerializationEvents_DerivedTypeWithEvents_FireAsExpected()
        {
            var f = new BinaryFormatter();

            var obj = new DerivedIncrementCountsDuringRoundtrip(null);

            Assert.Equal(0, obj.IncrementedDuringOnSerializingMethod);
            Assert.Equal(0, obj.IncrementedDuringOnSerializedMethod);
            Assert.Equal(0, obj.IncrementedDuringOnDeserializingMethod);
            Assert.Equal(0, obj.IncrementedDuringOnDeserializedMethod);
            Assert.Equal(0, obj.DerivedIncrementedDuringOnSerializingMethod);
            Assert.Equal(0, obj.DerivedIncrementedDuringOnSerializedMethod);
            Assert.Equal(0, obj.DerivedIncrementedDuringOnDeserializingMethod);
            Assert.Equal(0, obj.DerivedIncrementedDuringOnDeserializedMethod);

            using (var s = new MemoryStream())
            {
                f.Serialize(s, obj);
                s.Position = 0;

                Assert.Equal(1, obj.IncrementedDuringOnSerializingMethod);
                Assert.Equal(1, obj.IncrementedDuringOnSerializedMethod);
                Assert.Equal(0, obj.IncrementedDuringOnDeserializingMethod);
                Assert.Equal(0, obj.IncrementedDuringOnDeserializedMethod);
                Assert.Equal(1, obj.DerivedIncrementedDuringOnSerializingMethod);
                Assert.Equal(1, obj.DerivedIncrementedDuringOnSerializedMethod);
                Assert.Equal(0, obj.DerivedIncrementedDuringOnDeserializingMethod);
                Assert.Equal(0, obj.DerivedIncrementedDuringOnDeserializedMethod);

                var result = (DerivedIncrementCountsDuringRoundtrip)f.Deserialize(s);

                Assert.Equal(1, obj.IncrementedDuringOnSerializingMethod);
                Assert.Equal(1, obj.IncrementedDuringOnSerializedMethod);
                Assert.Equal(0, obj.IncrementedDuringOnDeserializingMethod);
                Assert.Equal(0, obj.IncrementedDuringOnDeserializedMethod);
                Assert.Equal(1, obj.DerivedIncrementedDuringOnSerializingMethod);
                Assert.Equal(1, obj.DerivedIncrementedDuringOnSerializedMethod);
                Assert.Equal(0, obj.DerivedIncrementedDuringOnDeserializingMethod);
                Assert.Equal(0, obj.DerivedIncrementedDuringOnDeserializedMethod);

                Assert.Equal(1, result.IncrementedDuringOnSerializingMethod);
                Assert.Equal(0, result.IncrementedDuringOnSerializedMethod);
                Assert.Equal(1, result.IncrementedDuringOnDeserializingMethod);
                Assert.Equal(1, result.IncrementedDuringOnDeserializedMethod);
                Assert.Equal(1, result.DerivedIncrementedDuringOnSerializingMethod);
                Assert.Equal(0, result.DerivedIncrementedDuringOnSerializedMethod);
                Assert.Equal(1, result.DerivedIncrementedDuringOnDeserializingMethod);
                Assert.Equal(1, result.DerivedIncrementedDuringOnDeserializedMethod);
            }
        }

        [Fact]
        public void Properties_Roundtrip()
        {
            var f = new BinaryFormatter();

            Assert.Null(f.Binder);
            var binder = new DelegateBinder();
            f.Binder = binder;
            Assert.Same(binder, f.Binder);

            Assert.NotNull(f.Context);
            Assert.Null(f.Context.Context);
            Assert.Equal(StreamingContextStates.All, f.Context.State);
            var context = new StreamingContext(StreamingContextStates.Clone);
            f.Context = context;
            Assert.Equal(StreamingContextStates.Clone, f.Context.State);

            Assert.Null(f.SurrogateSelector);
            var selector = new SurrogateSelector();
            f.SurrogateSelector = selector;
            Assert.Same(selector, f.SurrogateSelector);

            Assert.Equal(FormatterAssemblyStyle.Simple, f.AssemblyFormat);
            f.AssemblyFormat = FormatterAssemblyStyle.Full;
            Assert.Equal(FormatterAssemblyStyle.Full, f.AssemblyFormat);

            Assert.Equal(TypeFilterLevel.Full, f.FilterLevel);
            f.FilterLevel = TypeFilterLevel.Low;
            Assert.Equal(TypeFilterLevel.Low, f.FilterLevel);

            Assert.Equal(FormatterTypeStyle.TypesAlways, f.TypeFormat);
            f.TypeFormat = FormatterTypeStyle.XsdString;
            Assert.Equal(FormatterTypeStyle.XsdString, f.TypeFormat);
        }

        [Fact]
        public void SerializeDeserialize_InvalidArguments_ThrowsException()
        {
            var f = new BinaryFormatter();
            AssertExtensions.Throws<ArgumentNullException>("serializationStream", () => f.Serialize(null, new object()));
            AssertExtensions.Throws<ArgumentNullException>("serializationStream", () => f.Deserialize(null));
            Assert.Throws<SerializationException>(() => f.Deserialize(new MemoryStream())); // seekable, 0-length
        }

        [Theory]
        [InlineData(FormatterAssemblyStyle.Simple, false)]
        [InlineData(FormatterAssemblyStyle.Full, true)]
        public void MissingField_FailsWithAppropriateStyle(FormatterAssemblyStyle style, bool exceptionExpected)
        {
            var f = new BinaryFormatter();
            var s = new MemoryStream();
            f.Serialize(s, new Version1ClassWithoutField());
            s.Position = 0;

            f = new BinaryFormatter() { AssemblyFormat = style };
            f.Binder = new DelegateBinder { BindToTypeDelegate = (_, __) => typeof(Version2ClassWithoutOptionalField) };
            if (exceptionExpected)
            {
                Assert.Throws<SerializationException>(() => f.Deserialize(s));
            }
            else
            {
                var result = (Version2ClassWithoutOptionalField)f.Deserialize(s);
                Assert.NotNull(result);
                Assert.Equal(null, result.Value);
            }
        }

        [Theory]
        [InlineData(FormatterAssemblyStyle.Simple)]
        [InlineData(FormatterAssemblyStyle.Full)]
        public void OptionalField_Missing_Success(FormatterAssemblyStyle style)
        {
            var f = new BinaryFormatter();
            var s = new MemoryStream();
            f.Serialize(s, new Version1ClassWithoutField());
            s.Position = 0;

            f = new BinaryFormatter() { AssemblyFormat = style };
            f.Binder = new DelegateBinder { BindToTypeDelegate = (_, __) => typeof(Version2ClassWithOptionalField) };
            var result = (Version2ClassWithOptionalField)f.Deserialize(s);
            Assert.NotNull(result);
            Assert.Equal(null, result.Value);
        }

        [Fact]
        public void ObjectReference_RealObjectSerialized()
        {
            var obj = new ObjRefReturnsObj { Real = 42 };
            object real = FormatterClone<object>(obj);
            Assert.Equal(42, real);
        }

        // Test is disabled becaues it can cause improbable memory allocations leading to interminable paging.
        // We're keeping the code because it could be useful to a dev making local changes to binary formatter code.
        //[OuterLoop]
        //[Theory]
        //[MemberData(nameof(FuzzInputs_MemberData))]
        public void Deserialize_FuzzInput(object obj, Random rand)
        {
            // Get the serialized data for the object
            byte[] data = SerializeObjectToRaw(obj, FormatterAssemblyStyle.Simple);

            // Make some "random" changes to it
            for (int i = 1; i < rand.Next(1, 100); i++)
            {
                data[rand.Next(data.Length)] = (byte)rand.Next(256);
            }

            // Try to deserialize that.
            try
            {
                DeserializeRawToObject(data, FormatterAssemblyStyle.Simple);
                // Since there's no checksum, it's possible we changed data that didn't corrupt the instance
            }
            catch (ArgumentOutOfRangeException) { }
            catch (ArrayTypeMismatchException) { }
            catch (DecoderFallbackException) { }
            catch (FormatException) { }
            catch (IndexOutOfRangeException) { }
            catch (InvalidCastException) { }
            catch (OutOfMemoryException) { }
            catch (OverflowException) { }
            catch (NullReferenceException) { }
            catch (SerializationException) { }
            catch (TargetInvocationException) { }
            catch (ArgumentException) { }
            catch (FileLoadException) { }
        }

        [Fact]
        public void Deserialize_EndOfStream_ThrowsException()
        {
            var f = new BinaryFormatter();
            var s = new MemoryStream();
            f.Serialize(s, 1024);

            for (long i = s.Length - 1; i >= 0; i--)
            {
                s.Position = 0;
                var data = new byte[i];
                Assert.Equal(data.Length, s.Read(data, 0, data.Length));
                Assert.Throws<SerializationException>(() => f.Deserialize(new MemoryStream(data)));
            }
        }

        [Theory]
        [MemberData(nameof(CrossProcessObjects_MemberData))]
        public void Roundtrip_CrossProcess(object obj)
        {
            string outputPath = GetTestFilePath();
            string inputPath = GetTestFilePath();

            // Serialize out to a file
            using (FileStream fs = File.OpenWrite(outputPath))
            {
                new BinaryFormatter().Serialize(fs, obj);
            }

            // In another process, deserialize from that file and serialize to another
            RemoteInvoke((remoteInput, remoteOutput) =>
            {
                Assert.False(File.Exists(remoteOutput));
                using (FileStream input = File.OpenRead(remoteInput))
                using (FileStream output = File.OpenWrite(remoteOutput))
                {
                    var b = new BinaryFormatter();
                    b.Serialize(output, b.Deserialize(input));
                    return SuccessExitCode;
                }
            }, outputPath, inputPath).Dispose();

            // Deserialize what the other process serialized and compare it to the original
            using (FileStream fs = File.OpenRead(inputPath))
            {
                object deserialized = new BinaryFormatter().Deserialize(fs);
                Assert.Equal(obj, deserialized);
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework fails when serializing arrays with non-zero lower bounds")]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "UAPAOT does not support non-zero lower bounds")]
        public void Roundtrip_ArrayContainingArrayAtNonZeroLowerBound()
        {
            FormatterClone(Array.CreateInstance(typeof(uint[]), new[] { 5 }, new[] { 1 }));
        }
    }
}
