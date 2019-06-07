// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace System.Resources.Extensions.Tests
{
    public class PreserializedResourceWriterTests
    {
        [Fact]
        public static void ExceptionforNullStream()
        {
            Assert.Throws<ArgumentNullException>("stream", () => new PreserializedResourceWriter((Stream)null));
        }

        [Fact]
        public static void ExceptionforNullFile()
        {
            Assert.Throws<ArgumentNullException>("fileName", () => new PreserializedResourceWriter((string)null));
        }

        [Fact]
        public static void ExceptionforReadOnlyStream()
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                using (var readOnlyStream = new MemoryStream(new byte[1], false))
                {
                    new PreserializedResourceWriter(readOnlyStream);
                }
            });
        }

        [Fact]
        public static void ExceptionforNullResourceId()
        {
            using (var writer = new PreserializedResourceWriter(new MemoryStream()))
            {
                Assert.Throws<ArgumentNullException>("name", () => writer.AddResource(null, "value"));
                Assert.Throws<ArgumentNullException>("name", () => writer.AddResource(null, new object()));
                Assert.Throws<ArgumentNullException>("name", () => writer.AddResource(null, new byte[0]));

                using (var stream = new MemoryStream())
                {
                    Assert.Throws<ArgumentNullException>("name", () => writer.AddResource(null, stream));
                    Assert.Throws<ArgumentNullException>("name", () => writer.AddResource(null, stream, true));
                    Assert.Throws<ArgumentNullException>("name", () => writer.AddActivatorResource(null, stream, "System.DayOfWeek", false));
                }
                
                Assert.Throws<ArgumentNullException>("name", () => writer.AddBinaryFormattedResource(null, new byte[1], "System.DayOfWeek"));
                Assert.Throws<ArgumentNullException>("name", () => writer.AddTypeConverterResource(null, new byte[1], "System.DayOfWeek"));
                Assert.Throws<ArgumentNullException>("name", () => writer.AddResource(null, "Monday", "System.DayOfWeek"));
            }
        }

        [Fact]
        public static void ExceptionforDuplicateKey()
        {
            using (var writer = new PreserializedResourceWriter(new MemoryStream()))
            {
                writer.AddResource("duplicate", "value");

                Assert.Throws<ArgumentException>(null, () => writer.AddResource("duplicate", "value"));
                Assert.Throws<ArgumentException>(null, () => writer.AddResource("duplicate", new object()));
                Assert.Throws<ArgumentException>(null, () => writer.AddResource("duplicate", new byte[0]));

                using (var stream = new MemoryStream())
                {
                    Assert.Throws<ArgumentException>(null, () => writer.AddResource("duplicate", stream));
                    Assert.Throws<ArgumentException>(null, () => writer.AddResource("duplicate", stream, true));
                    Assert.Throws<ArgumentException>(null, () => writer.AddActivatorResource("duplicate", stream, "System.DayOfWeek", false));
                }

                Assert.Throws<ArgumentException>(null, () => writer.AddBinaryFormattedResource("duplicate", new byte[1], "System.DayOfWeek"));
                Assert.Throws<ArgumentException>(null, () => writer.AddTypeConverterResource("duplicate", new byte[1], "System.DayOfWeek"));
                Assert.Throws<ArgumentException>(null, () => writer.AddResource("duplicate", "Monday", "System.DayOfWeek"));


                Assert.Throws<ArgumentException>(null, () => writer.AddResource("Duplicate", "value"));
                Assert.Throws<ArgumentException>(null, () => writer.AddResource("dUplicate", new object()));
                Assert.Throws<ArgumentException>(null, () => writer.AddResource("duPlicate", new byte[0]));

                using (var stream = new MemoryStream())
                {
                    Assert.Throws<ArgumentException>(null, () => writer.AddResource("dupLicate", stream));
                    Assert.Throws<ArgumentException>(null, () => writer.AddResource("duplIcate", stream, true));
                    Assert.Throws<ArgumentException>(null, () => writer.AddActivatorResource("dupliCate", stream, "System.DayOfWeek", false));
                }

                Assert.Throws<ArgumentException>(null, () => writer.AddBinaryFormattedResource("duplicAte", new byte[1], "System.DayOfWeek"));
                Assert.Throws<ArgumentException>(null, () => writer.AddTypeConverterResource("duplicaTe", new byte[1], "System.DayOfWeek"));
                Assert.Throws<ArgumentException>(null, () => writer.AddResource("duplicatE", "Monday", "System.DayOfWeek"));
            }
        }

        [Fact]
        public static void ExceptionForAddAfterGenerate()
        {
            using (var writer = new PreserializedResourceWriter(new MemoryStream()))
            {
                writer.AddResource("duplicate", "value");

                writer.Generate();

                Assert.Throws<InvalidOperationException>(() => writer.AddResource("duplicate", "value"));
                Assert.Throws<InvalidOperationException>(() => writer.AddResource("duplicate", new object()));
                Assert.Throws<InvalidOperationException>(() => writer.AddResource("duplicate", new byte[0]));

                using (var stream = new MemoryStream())
                {
                    Assert.Throws<InvalidOperationException>(() => writer.AddResource("duplicate", stream));
                    Assert.Throws<InvalidOperationException>(() => writer.AddResource("duplicate", stream, true));
                    Assert.Throws<InvalidOperationException>(() => writer.AddActivatorResource("duplicate", stream, "System.DayOfWeek", false));
                }

                Assert.Throws<InvalidOperationException>(() => writer.AddBinaryFormattedResource("duplicate", new byte[1], "System.DayOfWeek"));
                Assert.Throws<InvalidOperationException>(() => writer.AddTypeConverterResource("duplicate", new byte[1], "System.DayOfWeek"));
                Assert.Throws<InvalidOperationException>(() => writer.AddResource("duplicate", "Monday", "System.DayOfWeek"));
            }
        }

        [Fact]
        public static void EmptyResources()
        {
            byte[] writerBuffer, binaryWriterBuffer;
            using (MemoryStream ms = new MemoryStream())
            using (ResourceWriter writer = new ResourceWriter(ms))
            {
                writer.Generate();
                writerBuffer = ms.ToArray();
            }

            using (MemoryStream ms = new MemoryStream())
            using (PreserializedResourceWriter writer = new PreserializedResourceWriter(ms))
            {
                writer.Generate();
                binaryWriterBuffer = ms.ToArray();
            }

            Assert.Equal(writerBuffer, binaryWriterBuffer);
        }

        
        [Fact]
        public static void PrimitiveResources()
        {
            IReadOnlyDictionary<string,object> values = TestData.Primitive;
            Action<IResourceWriter> addData = (writer) =>
            {
                foreach (var pair in values)
                {
                    writer.AddResource(pair.Key, pair.Value);
                }
            };

            byte[] writerBuffer, binaryWriterBuffer;
            using (MemoryStream ms = new MemoryStream())
            using (ResourceWriter writer = new ResourceWriter(ms))
            {
                addData(writer);
                writer.Generate();
                writerBuffer = ms.ToArray();
            }

            using (MemoryStream ms = new MemoryStream())
            using (PreserializedResourceWriter writer = new PreserializedResourceWriter(ms))
            {
                addData(writer);
                writer.Generate();
                binaryWriterBuffer = ms.ToArray();
            }

            // PreserializedResourceWriter should write ResourceWriter/ResourceReader format
            Assert.Equal(writerBuffer, binaryWriterBuffer);

            using (MemoryStream ms = new MemoryStream(binaryWriterBuffer, false))
            using (ResourceReader reader = new ResourceReader(ms))
            {
                IDictionaryEnumerator dictEnum = reader.GetEnumerator();

                while (dictEnum.MoveNext())
                {
                    Assert.Equal(values[(string)dictEnum.Key], dictEnum.Value);
                }
            }

            // DeserializingResourceReader can read ResourceReader format
            using (MemoryStream ms = new MemoryStream(binaryWriterBuffer, false))
            using (DeserializingResourceReader reader = new DeserializingResourceReader(ms))
            {
                IDictionaryEnumerator dictEnum = reader.GetEnumerator();

                while (dictEnum.MoveNext())
                {
                    Assert.Equal(values[(string)dictEnum.Key], dictEnum.Value);
                }
            }
        }

        [Fact]
        public static void PrimitiveResourcesAsStrings()
        {
            IReadOnlyDictionary<string, object> values = TestData.Primitive;

            byte[] writerBuffer, binaryWriterBuffer;
            using (MemoryStream ms = new MemoryStream())
            using (ResourceWriter writer = new ResourceWriter(ms))
            {
                foreach (var pair in values)
                {
                    writer.AddResource(pair.Key, pair.Value);
                }

                writer.Generate();
                writerBuffer = ms.ToArray();
            }

            using (MemoryStream ms = new MemoryStream())
            using (PreserializedResourceWriter writer = new PreserializedResourceWriter(ms))
            {
                foreach (var pair in values)
                {
                    writer.AddResource(pair.Key, TestData.GetStringValue(pair.Value), TestData.GetSerializationTypeName(pair.Value.GetType()));
                }
                writer.Generate();
                binaryWriterBuffer = ms.ToArray();
            }

            // PreserializedResourceWriter should write ResourceWriter/ResourceReader format
            Assert.Equal(writerBuffer, binaryWriterBuffer);

            using (MemoryStream ms = new MemoryStream(binaryWriterBuffer, false))
            using (ResourceReader reader = new ResourceReader(ms))
            {
                IDictionaryEnumerator dictEnum = reader.GetEnumerator();

                while (dictEnum.MoveNext())
                {
                    Assert.Equal(values[(string)dictEnum.Key], dictEnum.Value);
                }
            }

            // DeserializingResourceReader can read ResourceReader format
            using (MemoryStream ms = new MemoryStream(binaryWriterBuffer, false))
            using (DeserializingResourceReader reader = new DeserializingResourceReader(ms))
            {
                IDictionaryEnumerator dictEnum = reader.GetEnumerator();

                while (dictEnum.MoveNext())
                {
                    Assert.Equal(values[(string)dictEnum.Key], dictEnum.Value);
                }
            }
        }

        [Fact]
        public static void BinaryFormattedResources()
        {
            var values = TestData.BinaryFormatted;
            byte[] writerBuffer, binaryWriterBuffer;
            using (MemoryStream ms = new MemoryStream())
            using (ResourceWriter writer = new ResourceWriter(ms))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();

                foreach (var pair in values)
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        binaryFormatter.Serialize(memoryStream, pair.Value);
                        writer.AddResourceData(pair.Key, TestData.GetSerializationTypeName(pair.Value.GetType()), memoryStream.ToArray());
                    }
                }
                writer.Generate();
                writerBuffer = ms.ToArray();
            }

            using (MemoryStream ms = new MemoryStream())
            using (PreserializedResourceWriter writer = new PreserializedResourceWriter(ms))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();

                foreach (var pair in values)
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        binaryFormatter.Serialize(memoryStream, pair.Value);
                        writer.AddBinaryFormattedResource(pair.Key, memoryStream.ToArray(), TestData.GetSerializationTypeName(pair.Value.GetType()));
                    }
                }
                writer.Generate();
                binaryWriterBuffer = ms.ToArray();
            }

            // PreserializedResourceWriter should write ResourceWriter/ResourceReader format
            Assert.Equal(writerBuffer, binaryWriterBuffer);

            using (MemoryStream ms = new MemoryStream(writerBuffer, false))
            using (ResourceReader reader = new ResourceReader(ms))
            {
                typeof(ResourceReader).GetField("_permitDeserialization", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(reader, true);

                IDictionaryEnumerator dictEnum = reader.GetEnumerator();

                while (dictEnum.MoveNext())
                {
                    ResourceValueEquals(values[(string)dictEnum.Key], dictEnum.Value);
                }
            }

            // DeserializingResourceReader can read ResourceReader format
            using (MemoryStream ms = new MemoryStream(writerBuffer, false))
            using (DeserializingResourceReader reader = new DeserializingResourceReader(ms))
            {
                IDictionaryEnumerator dictEnum = reader.GetEnumerator();

                while (dictEnum.MoveNext())
                {
                    ResourceValueEquals(values[(string)dictEnum.Key], dictEnum.Value);
                }
            }
        }

        [Fact]
        public static void BinaryFormattedResourcesWithoutTypeName()
        {
            var values = TestData.BinaryFormatted;
            byte[] binaryWriterBuffer;

            using (MemoryStream ms = new MemoryStream())
            using (PreserializedResourceWriter writer = new PreserializedResourceWriter(ms))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();

                foreach (var pair in values)
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        binaryFormatter.Serialize(memoryStream, pair.Value);
                        writer.AddBinaryFormattedResource(pair.Key, memoryStream.ToArray());
                    }
                }
                writer.Generate();
                binaryWriterBuffer = ms.ToArray();
            }

            // DeserializingResourceReader can read ResourceReader format
            using (MemoryStream ms = new MemoryStream(binaryWriterBuffer, false))
            using (DeserializingResourceReader reader = new DeserializingResourceReader(ms))
            {
                IDictionaryEnumerator dictEnum = reader.GetEnumerator();

                while (dictEnum.MoveNext())
                {
                    ResourceValueEquals(values[(string)dictEnum.Key], dictEnum.Value);
                }
            }
        }
        [Fact]
        public static void TypeConverterByteArrayResources()
        {
            var values = TestData.ByteArrayConverter;

            byte[] binaryWriterBuffer;

            using (MemoryStream ms = new MemoryStream())
            using (PreserializedResourceWriter writer = new PreserializedResourceWriter(ms))
            {
                foreach (var pair in values)
                {
                    TypeConverter converter = TypeDescriptor.GetConverter(pair.Value.GetType());
                    byte[] buffer = (byte[])converter.ConvertTo(pair.Value, typeof(byte[]));
                    writer.AddTypeConverterResource(pair.Key, buffer, TestData.GetSerializationTypeName(pair.Value.GetType()));
                }
                writer.Generate();
                binaryWriterBuffer = ms.ToArray();
            }

            using (MemoryStream ms = new MemoryStream(binaryWriterBuffer, false))
            using (DeserializingResourceReader reader = new DeserializingResourceReader(ms))
            {
                IDictionaryEnumerator dictEnum = reader.GetEnumerator();

                while (dictEnum.MoveNext())
                {
                    ResourceValueEquals(values[(string)dictEnum.Key], dictEnum.Value);
                }
            }
        }

        [Fact]
        public static void TypeConverterStringResources()
        {
            var values = TestData.StringConverter;

            byte[] binaryWriterBuffer;

            using (MemoryStream ms = new MemoryStream())
            using (PreserializedResourceWriter writer = new PreserializedResourceWriter(ms))
            {
                foreach (var pair in values)
                {
                    writer.AddResource(pair.Key, TestData.GetStringValue(pair.Value), TestData.GetSerializationTypeName(pair.Value.GetType()));
                }
                writer.Generate();
                binaryWriterBuffer = ms.ToArray();
            }

            using (MemoryStream ms = new MemoryStream(binaryWriterBuffer, false))
            using (DeserializingResourceReader reader = new DeserializingResourceReader(ms))
            {
                IDictionaryEnumerator dictEnum = reader.GetEnumerator();

                while (dictEnum.MoveNext())
                {
                    ResourceValueEquals(values[(string)dictEnum.Key], dictEnum.Value);
                }
            }
        }

        [Fact]
        public static void StreamResources()
        {
            var values = TestData.Activator;

            byte[] binaryWriterBuffer;

            using (MemoryStream ms = new MemoryStream())
            using (PreserializedResourceWriter writer = new PreserializedResourceWriter(ms))
            {
                foreach (var pair in values)
                {
                    pair.Value.stream.Seek(0, SeekOrigin.Begin);
                    writer.AddActivatorResource(pair.Key, pair.Value.stream, TestData.GetSerializationTypeName(pair.Value.type), false);
                }
                writer.Generate();
                binaryWriterBuffer = ms.ToArray();
            }

            using (MemoryStream ms = new MemoryStream(binaryWriterBuffer, false))
            using (DeserializingResourceReader reader = new DeserializingResourceReader(ms))
            {
                IDictionaryEnumerator dictEnum = reader.GetEnumerator();

                while (dictEnum.MoveNext())
                {
                    var expectedTuple = values[(string)dictEnum.Key];
                    expectedTuple.stream.Seek(0, SeekOrigin.Begin);
                    object expected = Activator.CreateInstance(expectedTuple.type, new object[] { expectedTuple.stream });
                    ResourceValueEquals(expected, dictEnum.Value);
                }
            }
        }
        
        [Fact]
        public static void CanReadViaResourceManager()
        {
            ResourceManager resourceManager = new ResourceManager(typeof(TestData));

            IEnumerable<KeyValuePair<string, object>> objectPairs = TestData.Primitive
                .Concat(TestData.PrimitiveAsString)
                .Concat(TestData.BinaryFormattedWithoutDrawing)
                .Concat(TestData.BinaryFormattedWithoutDrawingNoType)
                .Concat(TestData.ByteArrayConverterWithoutDrawing)
                .Concat(TestData.StringConverterWithoutDrawing);

            foreach(KeyValuePair<string, object> pair in objectPairs) 
            {
                var actualValue = resourceManager.GetObject(pair.Key);

                Assert.Equal(pair.Value, actualValue);
            }

            foreach(KeyValuePair<string, (Type type, Stream stream)> pair in TestData.ActivatorWithoutDrawing)
            {
                pair.Value.stream.Seek(0, SeekOrigin.Begin);
                var expectedValue = Activator.CreateInstance(pair.Value.type, pair.Value.stream);
                var actualValue = resourceManager.GetObject(pair.Key);

                Assert.Equal(expectedValue, actualValue);
            }
        }

        [Fact]
        public static void ResourceManagerLoadsCorrectReader()
        {
            ResourceManager resourceManager = new ResourceManager(typeof(TestData));
            ResourceSet resSet = resourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, true);
            IResourceReader reader = (IResourceReader)resSet.GetType().GetProperty("Reader", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(resSet);
            Assert.IsType<DeserializingResourceReader>(reader);
        }

        [Fact]
        public static void EmbeddedResourcesAreUpToDate()
        {
            // this is meant to catch a case where our embedded test resources are out of date with respect to the current writer.
            // that could be intentional, or accidental.  Regardless we want to know.
            using (Stream resourcesStream = typeof(TestData).Assembly.GetManifestResourceStream("System.Resources.Extensions.Tests.TestData.resources"))
            using (MemoryStream actualData = new MemoryStream(), expectedData = new MemoryStream())
            {
                TestData.WriteResourcesStream(actualData);
                resourcesStream.CopyTo(expectedData);
                Assert.Equal(expectedData.ToArray(), actualData.ToArray());
            }
        }

        private static void ResourceValueEquals(object expected, object actual)
        {
            if (actual is Bitmap bitmap)
            {
                BitmapEquals((Bitmap)expected, bitmap);
            }
            else if (actual is Icon icon)
            {
                BitmapEquals(((Icon)expected).ToBitmap(), icon.ToBitmap());
            }
            else if (actual is Font font)
            {
                Font expectedFont = (Font)expected;
                Assert.Equal(expectedFont.FontFamily, font.FontFamily);
                Assert.Equal(expectedFont.Size, font.Size);
                Assert.Equal(expectedFont.Style, font.Style);
                Assert.Equal(expectedFont.Unit, font.Unit);
            }
            else
            {
                Assert.Equal(expected, actual);
            }
        }

        private static void BitmapEquals(Bitmap left, Bitmap right)
        {
            Assert.Equal(left.Size, right.Size);

            for (int x = 0; x < left.Width; ++x)
            {
                for (int y = 0; y < left.Height; ++y)
                {
                    Assert.Equal(left.GetPixel(x, y), right.GetPixel(x, y));
                }
            }
        }
    }

}
