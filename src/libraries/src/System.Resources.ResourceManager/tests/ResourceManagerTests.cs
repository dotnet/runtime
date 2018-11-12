// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Drawing.Imaging;
using System.Linq;

namespace System.Resources.Tests
{
    public static partial class ResourceManagerTests
    {
        public static IEnumerable<object[]> EnglishResourceData()
        {
            yield return new object[] { "One", "Value-One" };
            yield return new object[] { "Two", "Value-Two" };
            yield return new object[] { "Three", "Value-Three" };
            yield return new object[] { "Empty", "" };
            yield return new object[] { "InvalidKeyName", null };
        }

        [Theory]
        [MemberData(nameof(EnglishResourceData))]
        public static void GetString_Basic(string key, string expectedValue)
        {
            ResourceManager resourceManager = new ResourceManager("System.Resources.Tests.Resources.TestResx", typeof(ResourceManagerTests).GetTypeInfo().Assembly);
            string actual = resourceManager.GetString(key);
            Assert.Equal(expectedValue, actual);
        }

        [Theory]
        [MemberData(nameof(EnglishResourceData))]
        public static void GetString_FromResourceType(string key, string expectedValue)
        {
            Type resourceType = typeof(Resources.TestResx);
            ResourceManager resourceManager = new ResourceManager(resourceType);
            string actual = resourceManager.GetString(key);
            Assert.Equal(expectedValue, actual);
        }

        [Fact]
        public static void HeaderVersionNumber()
        {
            Assert.Equal(1, ResourceManager.HeaderVersionNumber);
        }

        [Fact]
        public static void MagicNumber()
        {
            Assert.Equal(unchecked((int)0xBEEFCACE), ResourceManager.MagicNumber);
        }

        [Fact]
        public static void UsingResourceSet()
        {
            var resourceManager = new ResourceManager("System.Resources.Tests.Resources.TestResx", typeof(ResourceManagerTests).GetTypeInfo().Assembly, typeof(ResourceSet));
            Assert.Equal(typeof(ResourceSet), resourceManager.ResourceSetType);
        }

        [Fact]
        public static void BaseName()
        {
            var manager = new ResourceManager("System.Resources.Tests.Resources.TestResx", typeof(ResourceManagerTests).GetTypeInfo().Assembly);
            Assert.Equal("System.Resources.Tests.Resources.TestResx", manager.BaseName);
        }

        [Theory]
        [MemberData(nameof(EnglishResourceData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapNotUapAot, "When getting resources from PRI file the casing doesn't matter, if it is there it will always find and return the resource")]
        public static void IgnoreCase(string key, string expectedValue)
        {
            var manager = new ResourceManager("System.Resources.Tests.Resources.TestResx", typeof(ResourceManagerTests).GetTypeInfo().Assembly);
            var culture = new CultureInfo("en-US");
            Assert.False(manager.IgnoreCase);
            Assert.Equal(expectedValue, manager.GetString(key, culture));
            Assert.Null(manager.GetString(key.ToLower(), culture));
            manager.IgnoreCase = true;
            Assert.Equal(expectedValue, manager.GetString(key, culture));
            Assert.Equal(expectedValue, manager.GetString(key.ToLower(), culture));
        }


        public static IEnumerable<object[]> EnglishNonStringResourceData()
        {
            yield return new object[] { "Int", 42 };
            yield return new object[] { "Float", 3.14159 };
            yield return new object[] { "Bytes", new byte[] { 41, 42, 43, 44, 192, 168, 1, 1 } };
            yield return new object[] { "InvalidKeyName", null };
            yield return new object[] { "Point", new Point(50, 60), true };
            yield return new object[] { "Size", new Size(20, 30), true };
        }

        [Theory]
        [MemberData(nameof(EnglishNonStringResourceData))]
        public static void GetObject(string key, object expectedValue, bool requiresBinaryFormatter = false)
        {
            var manager = new ResourceManager("System.Resources.Tests.Resources.TestResx.netstandard17", typeof(ResourceManagerTests).GetTypeInfo().Assembly);
            Assert.Equal(expectedValue, manager.GetObject(key));
            Assert.Equal(expectedValue, manager.GetObject(key, new CultureInfo("en-US")));
        }


        private static byte[] GetImageData(object obj)
        {
            using (var stream = new MemoryStream())
            {
                switch (obj)
                {
                    case Image image:
                        image.Save(stream, ImageFormat.Bmp);
                        break;
                    case Icon icon:
                        icon.Save(stream);
                        break;
                    default:
                        throw new NotSupportedException();
                }

                return stream.ToArray();
            }
        }


        public static IEnumerable<object[]> EnglishImageResourceData()
        {
            yield return new object[] { "Bitmap", new Bitmap("bitmap.bmp") };
            yield return new object[] { "Icon", new Icon("icon.ico") };
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap)]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(EnglishImageResourceData))]
        public static void GetObject_Images(string key, object expectedValue)
        {
            var manager = new ResourceManager("System.Resources.Tests.Resources.TestResx.netstandard17", typeof(ResourceManagerTests).GetTypeInfo().Assembly);
            Assert.Equal(GetImageData(expectedValue), GetImageData(manager.GetObject(key)));
            Assert.Equal(GetImageData(expectedValue), GetImageData(manager.GetObject(key, new CultureInfo("en-US"))));
        }

        [Theory]
        [MemberData(nameof(EnglishResourceData))]
        public static void GetResourceSet_Strings(string key, string expectedValue)
        {
            var manager = new ResourceManager("System.Resources.Tests.Resources.TestResx", typeof(ResourceManagerTests).GetTypeInfo().Assembly);
            var culture = new CultureInfo("en-US");
            ResourceSet set = manager.GetResourceSet(culture, true, true);
            Assert.Equal(expectedValue, set.GetString(key));
        }

        [Theory]
        [MemberData(nameof(EnglishNonStringResourceData))]
        public static void GetResourceSet_NonStrings(string key, object expectedValue, bool requiresBinaryFormatter = false)
        {
            var manager = new ResourceManager("System.Resources.Tests.Resources.TestResx.netstandard17", typeof(ResourceManagerTests).GetTypeInfo().Assembly);
            var culture = new CultureInfo("en-US");
            ResourceSet set = manager.GetResourceSet(culture, true, true);
            Assert.Equal(expectedValue, set.GetObject(key));
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap)]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(EnglishImageResourceData))]
        public static void GetResourceSet_Images(string key, object expectedValue)
        {
            var manager = new ResourceManager("System.Resources.Tests.Resources.TestResx.netstandard17", typeof(ResourceManagerTests).GetTypeInfo().Assembly);
            var culture = new CultureInfo("en-US");
            ResourceSet set = manager.GetResourceSet(culture, true, true);
            Assert.Equal(GetImageData(expectedValue), GetImageData(set.GetObject(key)));
        }

        [Theory]
        [MemberData(nameof(EnglishNonStringResourceData))]
        public static void File_GetObject(string key, object expectedValue, bool requiresBinaryFormatter = false)
        {
            var manager = ResourceManager.CreateFileBasedResourceManager("TestResx.netstandard17", Directory.GetCurrentDirectory(), null);
            if (requiresBinaryFormatter && !PlatformDetection.IsFullFramework)
            {
                Assert.Throws<NotSupportedException>(() => manager.GetObject(key));
                Assert.Throws<NotSupportedException>(() => manager.GetObject(key, new CultureInfo("en-US")));
            }
            else
            {
                Assert.Equal(expectedValue, manager.GetObject(key));
                Assert.Equal(expectedValue, manager.GetObject(key, new CultureInfo("en-US")));
            }
        }

        [Theory]
        [MemberData(nameof(EnglishNonStringResourceData))]
        public static void File_GetResourceSet_NonStrings(string key, object expectedValue, bool requiresBinaryFormatter = false)
        {
            var manager = ResourceManager.CreateFileBasedResourceManager("TestResx.netstandard17", Directory.GetCurrentDirectory(), null);
            var culture = new CultureInfo("en-US");
            ResourceSet set = manager.GetResourceSet(culture, true, true);
            if (requiresBinaryFormatter && !PlatformDetection.IsFullFramework)
            {
                Assert.Throws<NotSupportedException>(() => set.GetObject(key));
            }
            else
            {
                Assert.Equal(expectedValue, set.GetObject(key));
            }
        }

        [Fact]
        public static void GetStream()
        {
            var manager = new ResourceManager("System.Resources.Tests.Resources.TestResx.netstandard17", typeof(ResourceManagerTests).GetTypeInfo().Assembly);
            var culture = new CultureInfo("en-US");
            var expectedBytes = new byte[] { 41, 42, 43, 44, 192, 168, 1, 1 };
            using (Stream stream = manager.GetStream("ByteStream"))
            {
                foreach (byte b in expectedBytes)
                {
                    Assert.Equal(b, stream.ReadByte());
                }
            }
            using (Stream stream = manager.GetStream("ByteStream", culture))
            {
                foreach (byte b in expectedBytes)
                {
                    Assert.Equal(b, stream.ReadByte());
                }
            }
        }
    }
}
