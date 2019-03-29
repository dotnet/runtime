// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Runtime.Serialization.Formatters.Tests
{
    public partial class BinaryFormatterTests : FileCleanupTestBase
    {
        // On 32-bit we can't test these high inputs as they cause OutOfMemoryExceptions.
        [ConditionalTheory(typeof(Environment), nameof(Environment.Is64BitProcess))]
        [InlineData(2 * 6_584_983 - 2)] // previous limit
        [InlineData(2 * 7_199_369 - 2)] // last pre-computed prime number
        public void SerializeHugeObjectGraphs(int limit)
        {
            Point[] pointArr = Enumerable.Range(0, limit)
                .Select(i => new Point(i, i + 1))
                .ToArray();

            // This should not throw a SerializationException as we removed the artifical limit in the ObjectIDGenerator.
            // Instead of round tripping we only serialize to minimize test time.
            // This will throw on .NET Framework as the artificial limit is still enabled.
            var bf = new BinaryFormatter();
            AssertExtensions.ThrowsIf<SerializationException>(PlatformDetection.IsFullFramework, () =>
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    bf.Serialize(ms, pointArr);
                }
            });
        }

        [Theory]
        [MemberData(nameof(BasicObjectsRoundtrip_MemberData))]
        public void ValidateBasicObjectsRoundtrip(object obj, FormatterAssemblyStyle assemblyFormat, TypeFilterLevel filterLevel, FormatterTypeStyle typeFormat)
        {
            object clone = BinaryFormatterHelpers.Clone(obj, null, assemblyFormat, filterLevel, typeFormat);
            // string.Empty and DBNull are both singletons
            if (!ReferenceEquals(obj, string.Empty) && !(obj is DBNull))
            {
                Assert.NotSame(obj, clone);
            }

            EqualityExtensions.CheckEquals(obj, clone, isSamePlatform: true);
        }

        // Used for updating blobs in BinaryFormatterTestData.cs
        //[Fact]
        public void UpdateBlobs()
        {
            string testDataFilePath = GetTestDataFilePath();
            string[] coreTypeBlobs = SerializableEqualityComparers_MemberData()
                .Concat(SerializableObjects_MemberData())
                .Select(record => BinaryFormatterHelpers.ToBase64String(record[0]))
                .ToArray();

            var (numberOfBlobs, numberOfFoundBlobs, numberOfUpdatedBlobs) = UpdateCoreTypeBlobs(testDataFilePath, coreTypeBlobs);
            Console.WriteLine($"{numberOfBlobs} existing blobs" +
                $"{Environment.NewLine}{numberOfFoundBlobs} found blobs with regex search" +
                $"{Environment.NewLine}{numberOfUpdatedBlobs} updated blobs with regex replace");
        }

        [Theory]
        [MemberData(nameof(SerializableObjects_MemberData))]
        public void ValidateAgainstBlobs(object obj, TypeSerializableValue[] blobs)
            => ValidateAndRoundtrip(obj, blobs, false);

        [Theory]
        [MemberData(nameof(SerializableEqualityComparers_MemberData))]
        public void ValidateEqualityComparersAgainstBlobs(object obj, TypeSerializableValue[] blobs)
            => ValidateAndRoundtrip(obj, blobs, true);

        private static void ValidateAndRoundtrip(object obj, TypeSerializableValue[] blobs, bool isEqualityComparer)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("The serializable object must not be null", nameof(obj));
            }

            if (blobs == null || blobs.Length == 0)
            {
                throw new ArgumentOutOfRangeException($"Type {obj} has no blobs to deserialize and test equality against. Blob: " +
                    BinaryFormatterHelpers.ToBase64String(obj, FormatterAssemblyStyle.Full));
            }

            // Check if the passed in value in a serialization entry is assignable by the passed in type.
            if (obj is ISerializable customSerializableObj && HasObjectTypeIntegrity(customSerializableObj))
            {
                CheckObjectTypeIntegrity(customSerializableObj);
            }

            SanityCheckBlob(obj, blobs);

            // SqlException, ReflectionTypeLoadException and LicenseException aren't deserializable from Desktop --> Core.
            // Therefore we remove the second blob which is the one from Desktop.
            if (!PlatformDetection.IsFullFramework && (obj is SqlException || obj is ReflectionTypeLoadException || obj is LicenseException))
            {
                var tmpList = new List<TypeSerializableValue>(blobs);
                tmpList.RemoveAt(1);

                int index = tmpList.FindIndex(b => b.Platform.IsNetfxPlatform());
                if (index >= 0)
                    tmpList.RemoveAt(index);

                blobs = tmpList.ToArray();
            }

            // We store our framework blobs in index 1
            int platformBlobIndex = blobs.GetPlatformIndex();
            for (int i = 0; i < blobs.Length; i++)
            {
                // Check if the current blob is from the current running platform.
                bool isSamePlatform = i == platformBlobIndex;

                if (isEqualityComparer)
                {
                    ValidateEqualityComparer(BinaryFormatterHelpers.FromBase64String(blobs[i].Base64Blob, FormatterAssemblyStyle.Simple));
                    ValidateEqualityComparer(BinaryFormatterHelpers.FromBase64String(blobs[i].Base64Blob, FormatterAssemblyStyle.Full));
                }
                else
                {
                    EqualityExtensions.CheckEquals(obj, BinaryFormatterHelpers.FromBase64String(blobs[i].Base64Blob, FormatterAssemblyStyle.Simple), isSamePlatform);
                    EqualityExtensions.CheckEquals(obj, BinaryFormatterHelpers.FromBase64String(blobs[i].Base64Blob, FormatterAssemblyStyle.Full), isSamePlatform);
                }
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void RegexExceptionSerializable()
        {
            try
            {
#pragma warning disable RE0001 // Regex issue: {0}
                new Regex("*"); // parsing "*" - Quantifier {x,y} following nothing.
#pragma warning restore RE0001 // Regex issue: {0}
            }
            catch (ArgumentException ex)
            {
                Assert.Equal(ex.GetType().Name, "RegexParseException");
                ArgumentException clone = BinaryFormatterHelpers.Clone(ex);
                Assert.IsType<ArgumentException>(clone);
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
            EqualityExtensions.CheckEquals(obj, BinaryFormatterHelpers.FromBase64String(corefxBlob, FormatterAssemblyStyle.Full), isSamePlatform: true);
            EqualityExtensions.CheckEquals(obj, BinaryFormatterHelpers.FromBase64String(netfxBlob, FormatterAssemblyStyle.Full), isSamePlatform: true);
        }

        [Fact]
        public void ValidateDeserializationOfObjectWithDifferentAssemblyVersion()
        {
            // To generate this properly, change AssemblyVersion to a value which is unlikely to happen in production and generate base64(serialized-data)
            // For this test 9.98.7.987 is being used
            var obj = new SomeType() { SomeField = 7 };
            string serializedObj = @"AAEAAAD/////AQAAAAAAAAAMAgAAAHNTeXN0ZW0uUnVudGltZS5TZXJpYWxpemF0aW9uLkZvcm1hdHRlcnMuVGVzdHMsIFZlcnNpb249OS45OC43Ljk4NywgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj05ZDc3Y2M3YWQzOWI2OGViBQEAAAA2U3lzdGVtLlJ1bnRpbWUuU2VyaWFsaXphdGlvbi5Gb3JtYXR0ZXJzLlRlc3RzLlNvbWVUeXBlAQAAAAlTb21lRmllbGQACAIAAAAHAAAACw==";

            var deserialized = (SomeType)BinaryFormatterHelpers.FromBase64String(serializedObj, FormatterAssemblyStyle.Simple);
            Assert.Equal(obj, deserialized);
        }

        [Fact]
        public void ValidateDeserializationOfObjectWithGenericTypeWhichGenericArgumentHasDifferentAssemblyVersion()
        {
            // To generate this properly, change AssemblyVersion to a value which is unlikely to happen in production and generate base64(serialized-data)
            // For this test 9.98.7.987 is being used
            var obj = new GenericTypeWithArg<SomeType>() { Test = new SomeType() { SomeField = 9 } };
            string serializedObj = @"AAEAAAD/////AQAAAAAAAAAMAgAAAHNTeXN0ZW0uUnVudGltZS5TZXJpYWxpemF0aW9uLkZvcm1hdHRlcnMuVGVzdHMsIFZlcnNpb249OS45OC43Ljk4NywgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj05ZDc3Y2M3YWQzOWI2OGViBQEAAADxAVN5c3RlbS5SdW50aW1lLlNlcmlhbGl6YXRpb24uRm9ybWF0dGVycy5UZXN0cy5HZW5lcmljVHlwZVdpdGhBcmdgMVtbU3lzdGVtLlJ1bnRpbWUuU2VyaWFsaXphdGlvbi5Gb3JtYXR0ZXJzLlRlc3RzLlNvbWVUeXBlLCBTeXN0ZW0uUnVudGltZS5TZXJpYWxpemF0aW9uLkZvcm1hdHRlcnMuVGVzdHMsIFZlcnNpb249OS45OC43Ljk4NywgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj05ZDc3Y2M3YWQzOWI2OGViXV0BAAAABFRlc3QENlN5c3RlbS5SdW50aW1lLlNlcmlhbGl6YXRpb24uRm9ybWF0dGVycy5UZXN0cy5Tb21lVHlwZQIAAAACAAAACQMAAAAFAwAAADZTeXN0ZW0uUnVudGltZS5TZXJpYWxpemF0aW9uLkZvcm1hdHRlcnMuVGVzdHMuU29tZVR5cGUBAAAACVNvbWVGaWVsZAAIAgAAAAkAAAAL";

            var deserialized = (GenericTypeWithArg<SomeType>)BinaryFormatterHelpers.FromBase64String(serializedObj, FormatterAssemblyStyle.Simple);
            Assert.Equal(obj, deserialized);
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
                EqualityExtensions.CheckEquals(obj[0], clone, isSamePlatform: true);
            }
        }

        [Fact]
        public void SameObjectRepeatedInArray()
        {
            object o = new object();
            object[] arr = new[] { o, o, o, o, o };
            object[] result = BinaryFormatterHelpers.Clone(arr);

            Assert.Equal(arr.Length, result.Length);
            Assert.NotSame(arr, result);
            Assert.NotSame(arr[0], result[0]);
            for (int i = 1; i < result.Length; i++)
            {
                Assert.Same(result[0], result[i]);
            }
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
            Assert.Throws<SerializationException>(() => BinaryFormatterHelpers.Clone(p));

            NonSerializablePair<int, string> result = BinaryFormatterHelpers.Clone(p, new NonSerializablePairSurrogate());
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
            object real = BinaryFormatterHelpers.Clone<object>(obj);
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
            byte[] data = BinaryFormatterHelpers.ToByteArray(obj, FormatterAssemblyStyle.Simple);

            // Make some "random" changes to it
            for (int i = 1; i < rand.Next(1, 100); i++)
            {
                data[rand.Next(data.Length)] = (byte)rand.Next(256);
            }

            // Try to deserialize that.
            try
            {
                BinaryFormatterHelpers.FromByteArray(data, FormatterAssemblyStyle.Simple);
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
                byte[] data = new byte[i];
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
            RemoteExecutor.Invoke((remoteInput, remoteOutput) =>
            {
                Assert.False(File.Exists(remoteOutput));
                using (FileStream input = File.OpenRead(remoteInput))
                using (FileStream output = File.OpenWrite(remoteOutput))
                {
                    var b = new BinaryFormatter();
                    b.Serialize(output, b.Deserialize(input));
                    return RemoteExecutor.SuccessExitCode;
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
            BinaryFormatterHelpers.Clone(Array.CreateInstance(typeof(uint[]), new[] { 5 }, new[] { 1 }));
        }

        private static void ValidateEqualityComparer(object obj)
        {
            Type objType = obj.GetType();
            Assert.True(objType.IsGenericType, $"Type `{objType.FullName}` must be generic.");
            Assert.Equal("System.Collections.Generic.ObjectEqualityComparer`1", objType.GetGenericTypeDefinition().FullName);
            Assert.Equal(obj.GetType().GetGenericArguments()[0], objType.GetGenericArguments()[0]);
        }

        private static bool HasObjectTypeIntegrity(ISerializable serializable)
        {
            return !PlatformDetection.IsFullFramework ||
                !(serializable is NotFiniteNumberException);
        }

        private static void CheckObjectTypeIntegrity(ISerializable serializable)
        {
            SerializationInfo testData = new SerializationInfo(serializable.GetType(), new FormatterConverter());
            serializable.GetObjectData(testData, new StreamingContext(StreamingContextStates.Other));

            foreach (SerializationEntry entry in testData)
            {
                if (entry.Value != null)
                {
                    Assert.IsAssignableFrom(entry.ObjectType, entry.Value);
                }
            }
        }

        private static void SanityCheckBlob(object obj, TypeSerializableValue[] blobs)
        {
            // These types are unstable during serialization and produce different blobs.
            if (obj is WeakReference<Point> ||
                obj is Collections.Specialized.HybridDictionary ||
                obj is Color ||
                obj.GetType().FullName == "System.Collections.SortedList+SyncSortedList")
            {
                return;
            }

            // The blobs aren't identical because of different implementations on Unix vs. Windows.
            if (obj is Bitmap ||
                obj is Icon ||
                obj is Metafile)
            {
                return;
            }

            // In most cases exceptions in Core have a different layout than in Desktop,
            // therefore we are skipping the string comparison of the blobs.
            if (obj is Exception)
            {
                return;
            }

            // Check if runtime generated blob is the same as the stored one
            int frameworkBlobNumber = blobs.GetPlatformIndex();
            if (frameworkBlobNumber < blobs.Length)
            {
                string runtimeBlob = BinaryFormatterHelpers.ToBase64String(obj, FormatterAssemblyStyle.Full);

                string storedComparableBlob = CreateComparableBlobInfo(blobs[frameworkBlobNumber].Base64Blob);
                string runtimeComparableBlob = CreateComparableBlobInfo(runtimeBlob);

                Assert.True(storedComparableBlob == runtimeComparableBlob,
                    $"The stored blob for type {obj.GetType().FullName} is outdated and needs to be updated.{Environment.NewLine}{Environment.NewLine}" +
                    $"-------------------- Stored blob ---------------------{Environment.NewLine}" +
                    $"Encoded: {blobs[frameworkBlobNumber].Base64Blob}{Environment.NewLine}" +
                    $"Decoded: {storedComparableBlob}{Environment.NewLine}{Environment.NewLine}" +
                    $"--------------- Runtime generated blob ---------------{Environment.NewLine}" +
                    $"Encoded: {runtimeBlob}{Environment.NewLine}" +
                    $"Decoded: {runtimeComparableBlob}");
            }
        }

        private static string GetTestDataFilePath()
        {
            string GetRepoRootPath()
            {
                var exeFile = new FileInfo(Assembly.GetExecutingAssembly().Location);

                DirectoryInfo root = exeFile.Directory;
                while (!Directory.Exists(Path.Combine(root.FullName, ".git")))
                {
                    if (root.Parent == null)
                        return null;

                    root = root.Parent;
                }

                return root.FullName;
            }

            // Get path to binary formatter test data
            string repositoryRootPath = GetRepoRootPath();
            Assert.NotNull(repositoryRootPath);
            string testDataFilePath = Path.Combine(repositoryRootPath, "src", "System.Runtime.Serialization.Formatters", "tests", "BinaryFormatterTestData.cs");
            Assert.True(File.Exists(testDataFilePath));

            return testDataFilePath;
        }

        private static string CreateComparableBlobInfo(string base64Blob)
        {
            string lineSeparator = ((char)0x2028).ToString();
            string paragraphSeparator = ((char)0x2029).ToString();

            byte[] data = Convert.FromBase64String(base64Blob);
            base64Blob = Encoding.UTF8.GetString(data);

            return Regex.Replace(base64Blob, @"Version=\d.\d.\d.\d.", "Version=0.0.0.0", RegexOptions.Multiline)
                // Ignore the old Test key and Open public keys.
                .Replace("PublicKeyToken=cc7b13ffcd2ddd51", "PublicKeyToken=null")
                .Replace("PublicKeyToken=9d77cc7ad39b68eb", "PublicKeyToken=null")
                .Replace("\r\n", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("\r", string.Empty)
                .Replace(lineSeparator, string.Empty)
                .Replace(paragraphSeparator, string.Empty);
        }

        private static (int blobs, int foundBlobs, int updatedBlobs) UpdateCoreTypeBlobs(string testDataFilePath, string[] blobs)
        {
            // Replace existing test data blobs with updated ones
            string[] testDataLines = File.ReadAllLines(testDataFilePath);
            List<string> updatedTestDataLines = new List<string>();
            int numberOfBlobs = 0;
            int numberOfFoundBlobs = 0;
            int numberOfUpdatedBlobs = 0;

            for (int i = 0; i < testDataLines.Length; i++)
            {
                string testDataLine = testDataLines[i];
                if (!testDataLine.Trim().StartsWith("yield") || numberOfBlobs >= blobs.Length)
                {
                    updatedTestDataLines.Add(testDataLine);
                    continue;
                }

                string pattern = null;
                string replacement = null;
                if (PlatformDetection.IsFullFramework)
                {
                    pattern = ", \"AAEAAAD[^\"]+\"(?!,)";
                    replacement = ", \"" + blobs[numberOfBlobs] + "\"";
                }
                else
                {
                    pattern = "\"AAEAAAD[^\"]+\",";
                    replacement = "\"" + blobs[numberOfBlobs] + "\",";
                }

                Regex regex = new Regex(pattern);
                Match match = regex.Match(testDataLine);
                if (match.Success)
                {
                    numberOfFoundBlobs++;
                }
                string updatedLine = regex.Replace(testDataLine, replacement);
                if (testDataLine != updatedLine)
                {
                    numberOfUpdatedBlobs++;
                }
                testDataLine = updatedLine;

                updatedTestDataLines.Add(testDataLine);
                numberOfBlobs++;
            }

            // Check if all blobs were recognized and write updates to file
            Assert.Equal(numberOfBlobs, blobs.Length);
            File.WriteAllLines(testDataFilePath, updatedTestDataLines);

            return (numberOfBlobs, numberOfFoundBlobs, numberOfUpdatedBlobs);
        }

        private class DelegateBinder : SerializationBinder
        {
            public Func<string, string, Type> BindToTypeDelegate = null;
            public override Type BindToType(string assemblyName, string typeName) => BindToTypeDelegate?.Invoke(assemblyName, typeName);
        }
    }
}
