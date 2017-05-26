// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Xunit;

namespace System.Runtime.Serialization.Formatters.Tests
{
    public partial class BinaryFormatterTests : RemoteExecutorTestBase
    {
        public static IEnumerable<object[]> ValidateBasicObjectsRoundtrip_MemberData()
        {
            foreach (object[] obj in SerializableObjects())
            {
                foreach (FormatterAssemblyStyle assemblyFormat in new[] { FormatterAssemblyStyle.Full, FormatterAssemblyStyle.Simple })
                {
                    foreach (TypeFilterLevel filterLevel in new[] { TypeFilterLevel.Full, TypeFilterLevel.Low })
                    {
                        foreach (FormatterTypeStyle typeFormat in new[] { FormatterTypeStyle.TypesAlways, FormatterTypeStyle.TypesWhenNeeded, FormatterTypeStyle.XsdString })
                        {
                            yield return new object[] { obj[0], assemblyFormat, filterLevel, typeFormat };
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(ValidateBasicObjectsRoundtrip_MemberData))]
        public void ValidateBasicObjectsRoundtrip(object obj, FormatterAssemblyStyle assemblyFormat, TypeFilterLevel filterLevel, FormatterTypeStyle typeFormat)
        {
            object clone = FormatterClone(obj, null, assemblyFormat, filterLevel, typeFormat);
            if (!ReferenceEquals(obj, string.Empty)) // "" is interned and will roundtrip as the same object
            {
                Assert.NotSame(obj, clone);
            }

            CheckForAnyEquals(obj, clone);
        }

        //[Fact]
        //public void Serialize()
        //{
        //    IEnumerable<object[]> objs = SerializableObjects();
        //    List<string> serializedHashes = new List<string>();
        //    for (int i = 0; i < objs.Count(); i++)
        //    {
        //        var obj = objs.ElementAt(i)[0];
        //        BinaryFormatter bf = new BinaryFormatter();
        //        using (MemoryStream ms = new MemoryStream())
        //        {
        //            bf.Serialize(ms, obj);
        //            string serializedHash = Convert.ToBase64String(ms.ToArray());
        //            serializedHashes.Add(serializedHash);
        //        }
        //    }

        //    File.WriteAllLines("serializables.txt", serializedHashes);
        //}

        private static string SerializeObjectToHash(object original)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, original);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        private static object DeserializeObjectHash(object original, string base64Str)
        {
            var binaryFormatter = new BinaryFormatter();
            byte[] serializedObj = Convert.FromBase64String(base64Str);
            using (var serializedStream = new MemoryStream(serializedObj))
            {
                try
                {
                    return binaryFormatter.Deserialize(serializedStream);
                }
                catch
                {
                    Console.WriteLine($"Deserialization of type {original.GetType().FullName} failed");
                    throw;
                }
            }
        }

        [Fact]
        public void Seri()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            object obj = TimeZone.CurrentTimeZone;
#pragma warning restore CS0618 // Type or member is obsolete
            string base64 = null;

            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                base64 = Convert.ToBase64String(ms.ToArray());
            }

            byte[] serializedObj = Convert.FromBase64String(base64);
            using (var serializedStream = new MemoryStream(serializedObj))
            {
                var x = bf.Deserialize(serializedStream);
            }
        }

        [Theory]
        [MemberData(nameof(SerializableObjects))]
        public void ValidateTfmHashes(object original, string[] tfmBase64Hashes)
        {
            if (tfmBase64Hashes == null || tfmBase64Hashes.Length < 2)
            {
                throw new InvalidOperationException($"Type {original} has no base64 hashes to deserialize and test equality against. " +
                    $"Hash for object is: " + SerializeObjectToHash(original));
            }

            foreach (string tfmBase64Hash in tfmBase64Hashes)
            {
                if (!string.IsNullOrWhiteSpace(tfmBase64Hash))
                {
                    CheckForAnyEquals(original, DeserializeObjectHash(original, tfmBase64Hash));
                }
            }
        }

        private void CheckForAnyEquals(object original, object clone)
        {
            if (original != null && clone != null)
            {
                object result = null;
                Type originalType = original.GetType();

                // Check if custom equality extension method is available
                MethodInfo customEqualityCheck = GetExtensionMethod(typeof(EqualityExtensions).Assembly, originalType);
                if (customEqualityCheck != null)
                {
                    result = customEqualityCheck.Invoke(original, new object[] { original, clone });
                }
                else
                {
                    // Check if object.Equals(object) is overridden and if not check if there is a more concrete equality check implementation
                    bool equalsNotOverridden = originalType.GetMethod("Equals", new Type[] { typeof(object) }).DeclaringType == typeof(object);
                    if (equalsNotOverridden)
                    {
                        // If type doesn't override Equals(object) method then check if there is a more concrete implementation
                        // e.g. if type implements IEquatable<T>.
                        MethodInfo equalsMethod = originalType.GetMethod("Equals", new Type[] { originalType });
                        if (equalsMethod.DeclaringType != typeof(object))
                        {
                            result = equalsMethod.Invoke(original, new object[] { clone });
                        }
                    }
                }

                if (result != null)
                {
                    Assert.True((bool)result, "Error during equality check of type " + originalType.FullName);
                    return;
                }
            }

            try
            {
                Assert.Equal(original, clone);
            }
            catch (Exception)
            {
                Console.WriteLine("Error during equality check of type " + original?.GetType()?.FullName);
                throw;
            }
        }

        private static MethodInfo GetExtensionMethod(Assembly assembly, Type extendedType)
        {
            if (extendedType.IsGenericType)
            {
                return typeof(EqualityExtensions).GetMethods()
                    ?.SingleOrDefault(m => m.Name == "IsEqual" && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType.Name == extendedType.Name)
                    ?.MakeGenericMethod(extendedType.GenericTypeArguments[0]);
            }
            else
            {
                return typeof(EqualityExtensions).GetMethod("IsEqual", new[] { extendedType, extendedType });
            }
        }

        [Fact]
        public void RoundtripManyObjectsInOneStream()
        {
            object[][] objects = SerializableObjects().ToArray();
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

        public static IEnumerable<object> SerializableExceptions()
        {
            var exception = new Exception("Exception message", new Exception("Inner exception message"));
            yield return new object[] { new AggregateException("Aggregate exception message", exception) };
            yield return new object[] { exception };
        }

        [Theory]
        [MemberData(nameof(SerializableExceptions))]
        public void Roundtrip_Exceptions(Exception expected)
        {
            BinaryFormatterHelpers.AssertRoundtrips(expected);
        }

        public static IEnumerable<object[]> SerializableObjectsWithFuncOfObjectToCompare()
        {
            object target = 42;
            yield return new object[] { new Random(), new Func<object, object>(o => ((Random)o).Next()) };
        }

        [Theory]
        [MemberData(nameof(SerializableObjectsWithFuncOfObjectToCompare))]
        public void Roundtrip_ObjectsWithComparers(object obj, Func<object, object> getResult)
        {
            object actual = FormatterClone(obj);
            Assert.Equal(getResult(obj), getResult(actual));
        }

        public static IEnumerable<object[]> ValidateNonSerializableTypes_MemberData()
        {
            foreach (object obj in NonSerializableObjects())
            {
                foreach (FormatterAssemblyStyle assemblyFormat in new[] { FormatterAssemblyStyle.Full, FormatterAssemblyStyle.Simple })
                {
                    foreach (TypeFilterLevel filterLevel in new[] { TypeFilterLevel.Full, TypeFilterLevel.Low })
                    {
                        foreach (FormatterTypeStyle typeFormat in new[] { FormatterTypeStyle.TypesAlways, FormatterTypeStyle.TypesWhenNeeded, FormatterTypeStyle.XsdString })
                        {
                            yield return new object[] { obj, assemblyFormat, filterLevel, typeFormat };
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(ValidateNonSerializableTypes_MemberData))]
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

        public static IEnumerable<object[]> Deserialize_FuzzInput_MemberData()
        {
            var rand = new Random(42);
            foreach (object obj in SerializableObjects())
            {
                const int FuzzingsPerObject = 3;
                for (int i = 0; i < FuzzingsPerObject; i++)
                {
                    yield return new object[] { obj, rand, i };
                }
            }
        }

        [OuterLoop]
        [Theory]
        [MemberData(nameof(Deserialize_FuzzInput_MemberData))]
        public void Deserialize_FuzzInput(object obj, Random rand, int fuzzTrial)
        {
            // Get the serialized data for the object
            var f = new BinaryFormatter();
            var s = new MemoryStream();
            f.Serialize(s, obj);

            // Make some "random" changes to it
            byte[] data = s.ToArray();
            for (int i = 1; i < rand.Next(1, 100); i++)
            {
                data[rand.Next(data.Length)] = (byte)rand.Next(256);
            }

            // Try to deserialize that.
            try
            {
                f.Deserialize(new MemoryStream(data));
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

        public static IEnumerable<object[]> Roundtrip_CrossProcess_MemberData()
        {
            // Just a few objects to verify we can roundtrip out of process memory
            yield return new object[] { "test" };
            yield return new object[] { new List<int> { 1, 2, 3, 4, 5 } };
            yield return new object[] { new Tree<int>(1, new Tree<int>(2, new Tree<int>(3, null, null), new Tree<int>(4, null, null)), new Tree<int>(5, null, null)) };
        }

        [Theory]
        [MemberData(nameof(Roundtrip_CrossProcess_MemberData))]
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
            }, $"\"{outputPath}\"", $"\"{inputPath}\"").Dispose();

            // Deserialize what the other process serialized and compare it to the original
            using (FileStream fs = File.OpenRead(inputPath))
            {
                object deserialized = new BinaryFormatter().Deserialize(fs);
                Assert.Equal(obj, deserialized);
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework fails when serializing arrays with non-zero lower bounds")]
        public void Roundtrip_ArrayContainingArrayAtNonZeroLowerBound()
        {
            FormatterClone(Array.CreateInstance(typeof(uint[]), new[] { 5 }, new[] { 1 }));
        }

        private class DelegateBinder : SerializationBinder
        {
            public Func<string, string, Type> BindToTypeDelegate = null;
            public override Type BindToType(string assemblyName, string typeName) => BindToTypeDelegate?.Invoke(assemblyName, typeName);
        }

        private static T FormatterClone<T>(
            T obj,
            ISerializationSurrogate surrogate = null,
            FormatterAssemblyStyle assemblyFormat = FormatterAssemblyStyle.Full,
            TypeFilterLevel filterLevel = TypeFilterLevel.Full,
            FormatterTypeStyle typeFormat = FormatterTypeStyle.TypesAlways)
        {
            BinaryFormatter f;
            if (surrogate == null)
            {
                f = new BinaryFormatter();
            }
            else
            {
                var c = new StreamingContext();
                var s = new SurrogateSelector();
                s.AddSurrogate(obj.GetType(), c, surrogate);
                f = new BinaryFormatter(s, c);
            }
            f.AssemblyFormat = assemblyFormat;
            f.FilterLevel = filterLevel;
            f.TypeFormat = typeFormat;

            using (var s = new MemoryStream())
            {
                f.Serialize(s, obj);
                Assert.NotEqual(0, s.Position);
                s.Position = 0;
                return (T)(f.Deserialize(s));
            }
        }
    }
}
