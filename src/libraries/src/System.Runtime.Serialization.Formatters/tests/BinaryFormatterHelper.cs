// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace System.Runtime.Serialization.Formatters.Tests
{
    public partial class BinaryFormatterTests
    {
        private static void CheckForAnyEquals(object obj, object deserializedObj)
        {
            Assert.True(CheckEquals(obj, deserializedObj), "Error during equality check of type " + obj?.GetType()?.FullName);
        }

        public static bool CheckEquals(object objA, object objB)
        {
            if (objA == null && objB == null)
                return true;

            if (objA != null && objB != null)
            {
                object equalityResult = null;
                Type objType = objA.GetType();

                // Check if custom equality extension method is available
                MethodInfo customEqualityCheck = GetExtensionMethod(objType);
                if (customEqualityCheck != null)
                {
                    equalityResult = customEqualityCheck.Invoke(objA, new object[] { objA, objB });
                }
                else
                {
                    // Check if object.Equals(object) is overridden and if not check if there is a more concrete equality check implementation
                    bool equalsNotOverridden = objType.GetMethod("Equals", new Type[] { typeof(object) }).DeclaringType == typeof(object);
                    if (equalsNotOverridden)
                    {
                        // If type doesn't override Equals(object) method then check if there is a more concrete implementation
                        // e.g. if type implements IEquatable<T>.
                        MethodInfo equalsMethod = objType.GetMethod("Equals", new Type[] { objType });
                        if (equalsMethod.DeclaringType != typeof(object))
                        {
                            equalityResult = equalsMethod.Invoke(objA, new object[] { objB });
                        }
                    }
                }

                if (equalityResult != null)
                {
                    return (bool)equalityResult;
                }
            }

            if (objA is IEnumerable objAEnumerable && objB is IEnumerable objBEnumerable)
            {
                return CheckSequenceEquals(objAEnumerable, objBEnumerable);
            }

            return objA.Equals(objB);
        }

        public static bool CheckSequenceEquals(IEnumerable a, IEnumerable b)
        {
            if (a == null || b == null)
                return a == b;

            if (a.GetType() != b.GetType())
                return false;

            IEnumerator eA = null;
            IEnumerator eB = null;

            try
            {
                eA = (a as IEnumerable).GetEnumerator();
                eB = (a as IEnumerable).GetEnumerator();
                while (true)
                {
                    bool moved = eA.MoveNext();
                    if (moved != eB.MoveNext())
                        return false;
                    if (!moved)
                        return true;
                    if (eA.Current == null && eB.Current == null)
                        return true;
                    if (!CheckEquals(eA.Current, eB.Current))
                        return true;
                }
            }
            finally
            {
                (eA as IDisposable)?.Dispose();
                (eB as IDisposable)?.Dispose();
            }
        }

        private static MethodInfo GetExtensionMethod(Type extendedType)
        {
            if (extendedType.IsGenericType)
            {
                MethodInfo method = typeof(EqualityExtensions).GetMethods()
                        ?.SingleOrDefault(m =>
                            m.Name == "IsEqual" &&
                            m.GetParameters().Length == 2 &&
                            m.GetParameters()[0].ParameterType.Name == extendedType.Name &&
                            m.IsGenericMethodDefinition);
                if (method != null)
                    return method.MakeGenericMethod(extendedType.GenericTypeArguments[0]);
            }

            return typeof(EqualityExtensions).GetMethod("IsEqual", new[] { extendedType, extendedType });
        }

        public static void ValidateEqualityComparer(object obj)
        {
            Type objType = obj.GetType();
            Assert.True(objType.IsGenericType, $"Type `{objType.FullName}` must be generic.");
            Assert.Equal("System.Collections.Generic.ObjectEqualityComparer`1", objType.GetGenericTypeDefinition().FullName);
            Assert.Equal(obj.GetType().GetGenericArguments()[0], objType.GetGenericArguments()[0]);
        }

        private static void SanityCheckBlob(object obj, string[] blobs)
        {
            // Check if runtime generated blob is the same as the stored one
            int frameworkBlobNumber = PlatformDetection.IsFullFramework ? 1 : 0;
            if (frameworkBlobNumber < blobs.Length &&
                // WeakReference<Point> and HybridDictionary with default constructor are generating
                // different blobs at runtime for some obscure reason. Excluding those from the check.
                !(obj is WeakReference<Point>) &&
                !(obj is Collections.Specialized.HybridDictionary) &&
                !(obj is TimeZoneInfo.AdjustmentRule))
            {
                string runtimeBlob = SerializeObjectToBlob(obj, FormatterAssemblyStyle.Full);

                string storedComparableBlob = CreateComparableBlobInfo(blobs[frameworkBlobNumber]);
                string runtimeComparableBlob = CreateComparableBlobInfo(runtimeBlob);

                Assert.True(storedComparableBlob == runtimeComparableBlob,
                    $"The stored blob for type {obj.GetType().FullName} is outdated and needs to be updated.{Environment.NewLine}Stored blob: {blobs[frameworkBlobNumber]}{Environment.NewLine}Generated runtime blob: {runtimeBlob}");
            }
        }

        public static string GetTestDataFilePath()
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

        public static IEnumerable<object[]> GetCoreTypeRecords()
        {
            // Define all core type records here
            return SerializableEqualityComparers_MemberData()
                .Concat(SerializableObjects_MemberData());
        }

        public static IEnumerable<string> GetCoreTypeBlobs(IEnumerable<object[]> records, FormatterAssemblyStyle assemblyStyle)
        {
            foreach (object[] record in records)
            {
                yield return SerializeObjectToBlob(record[0], assemblyStyle);
            }
        }

        public static string CreateComparableBlobInfo(string base64Blob)
        {
            string lineSeparator = ((char)0x2028).ToString();
            string paragraphSeparator = ((char)0x2029).ToString();

            byte[] data = Convert.FromBase64String(base64Blob);
            base64Blob = Encoding.UTF8.GetString(data);

            return Regex.Replace(base64Blob, @"Version=\d.\d.\d.\d.", "Version=0.0.0.0", RegexOptions.Multiline)
                .Replace("\r\n", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("\r", string.Empty)
                .Replace(lineSeparator, string.Empty)
                .Replace(paragraphSeparator, string.Empty);
        }

        public static (int blobs, int foundBlobs, int updatedBlobs) UpdateCoreTypeBlobs(string testDataFilePath, string[] blobs)
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

        public static byte[] SerializeObjectToRaw(object obj, FormatterAssemblyStyle assemblyStyle)
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.AssemblyFormat = assemblyStyle;
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public static string SerializeObjectToBlob(object obj, FormatterAssemblyStyle assemblyStyle)
        {
            byte[] raw = SerializeObjectToRaw(obj, assemblyStyle);
            return Convert.ToBase64String(raw);
        }

        public static object DeserializeRawToObject(byte[] raw, FormatterAssemblyStyle assemblyStyle)
        {
            var binaryFormatter = new BinaryFormatter();
            binaryFormatter.AssemblyFormat = assemblyStyle;
            using (var serializedStream = new MemoryStream(raw))
            {
                return binaryFormatter.Deserialize(serializedStream);
            }
        }

        public static object DeserializeBlobToObject(string base64Str, FormatterAssemblyStyle assemblyStyle)
        {
            byte[] raw = Convert.FromBase64String(base64Str);
            return DeserializeRawToObject(raw, assemblyStyle);
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

        private class DelegateBinder : SerializationBinder
        {
            public Func<string, string, Type> BindToTypeDelegate = null;
            public override Type BindToType(string assemblyName, string typeName) => BindToTypeDelegate?.Invoke(assemblyName, typeName);
        }
    }
}
