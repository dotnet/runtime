// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Drawing.Imaging;
using System.Linq;
using System.Resources;
using System.Diagnostics;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

[assembly:NeutralResourcesLanguage("en")]

namespace System.Resources.Tests
{
    namespace Resources
    {
        internal class TestClassWithoutNeutralResources
        {
        }
    }

    public class ResourceManagerTests
    {
        public static bool AllowsCustomResourceTypes => AppContext.TryGetSwitch("System.Resources.ResourceManager.AllowCustomResourceTypes", out bool isEnabled) ? isEnabled : true;

        [Fact]
        public static void ExpectMissingManifestResourceException()
        {
            MissingManifestResourceException e = Assert.Throws<MissingManifestResourceException> (() =>
            {
                Type resourceType = typeof(Resources.TestClassWithoutNeutralResources);
                ResourceManager resourceManager = new ResourceManager(resourceType);
                string actual = resourceManager.GetString("Any");
            });
            Assert.NotNull(e.Message);
        }

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
            Assert.Same(actual, resourceManager.GetString(key));
            Assert.Equal(expectedValue, resourceManager.GetObject(key));
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

        public static IEnumerable<object[]> CultureResourceData()
        {
            yield return new object[] { "OneLoc", "es", "Value-One(es)" };       // Find language specific resource
            yield return new object[] { "OneLoc", "es-ES", "Value-One(es)" };    // Finds parent language of culture specific resource
            yield return new object[] { "OneLoc", "es-MX", "Value-One(es-MX)" }; // Finds culture specific resource
            yield return new object[] { "OneLoc", "fr", "Value-One" };           // Find neutral resource when language resources are absent
            yield return new object[] { "OneLoc", "fr-CA", "Value-One" };        // Find neutral resource when culture and language resources are absent
            yield return new object[] { "OneLoc", "fr-FR", "Value-One(fr-FR)" }; // Finds culture specific resource
            yield return new object[] { "Lang", "es-MX", "es" };                 // Finds lang specific string when key is missing in culture resource
            yield return new object[] { "NeutOnly", "es-MX", "Neutral" };        // Finds neutral string when key is missing in culture and lang resource
        }

        [Theory]
        [MemberData(nameof(CultureResourceData))]
        [ActiveIssue("https://github.com/dotnet/runtimelab/issues/155", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))] // satellite assemblies
        public static void GetString_CultureFallback(string key, string cultureName, string expectedValue)
        {
            Type resourceType = typeof(Resources.TestResx);
            ResourceManager resourceManager = new ResourceManager(resourceType);
            var culture = new CultureInfo(cultureName);
            string actual = resourceManager.GetString(key, culture);
            Assert.Equal(expectedValue, actual);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtimelab/issues/155", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))] //satellite assemblies
        public static void GetString_FromTestClassWithoutNeutralResources()
        {
            // This test is designed to complement the GetString_FromCulutureAndResourceType "fr" & "fr-CA" cases
            // Together these tests cover the case where there exists a satellite assembly for "fr" which has
            // resources for some types, but not all.  This confirms the fallback through a satellite which matches
            // culture but does not match resource file
            Type resourceType = typeof(Resources.TestClassWithoutNeutralResources);
            ResourceManager resourceManager = new ResourceManager(resourceType);
            var culture = new CultureInfo("fr");
            string actual = resourceManager.GetString("One", culture);
            Assert.Equal("Value-One(fr)", actual);
        }

        static int ResourcesAfAZEvents = 0;

#if NETCOREAPP
        static System.Reflection.Assembly AssemblyResolvingEventHandler(System.Runtime.Loader.AssemblyLoadContext alc, System.Reflection.AssemblyName name)
        {
            if (name.FullName.StartsWith("System.Resources.ResourceManager.Tests.resources"))
            {
                if (name.FullName.Contains("Culture=af-ZA"))
                {
                    Assert.Equal(System.Runtime.Loader.AssemblyLoadContext.Default, alc);
                    Assert.Equal("System.Resources.ResourceManager.Tests.resources", name.Name);
                    Assert.Equal("af-ZA", name.CultureName);
                    Assert.Equal(0, ResourcesAfAZEvents);
                    ResourcesAfAZEvents++;
                }
            }

            return null;
        }
#endif

        static System.Reflection.Assembly AssemblyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            string name = args.Name;
            if (name.StartsWith("System.Resources.ResourceManager.Tests.resources"))
            {
                if (name.Contains("Culture=af-ZA"))
                {
#if NETCOREAPP
                    Assert.Equal(1, ResourcesAfAZEvents);
#else
                    Assert.Equal(0, ResourcesAfAZEvents);
#endif
                    ResourcesAfAZEvents++;
                }
            }

            return null;
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void GetString_ExpectEvents()
        {
            RemoteExecutor.Invoke(() =>
            {
                // Events only fire first time.  Remote to make sure test runs in a separate process
                Remote_ExpectEvents();
            }).Dispose();
        }

        private static void Remote_ExpectEvents()
        {
#if NETCOREAPP
            System.Runtime.Loader.AssemblyLoadContext.Default.Resolving += AssemblyResolvingEventHandler;
#endif
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(AssemblyResolveEventHandler);

            ResourcesAfAZEvents = 0;

            Type resourceType = typeof(Resources.TestResx);

            ResourceManager resourceManager = new ResourceManager(resourceType);
            var culture = new CultureInfo("af-ZA");
            string actual = resourceManager.GetString("One", culture);
            Assert.Equal("Value-One", actual);

#if NETCOREAPP
            Assert.Equal(2, ResourcesAfAZEvents);
#else
            Assert.Equal(1, ResourcesAfAZEvents);
#endif
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
            yield return new object[] { "Int", 42, false };
            yield return new object[] { "Float", 3.14159, false };
            yield return new object[] { "Bytes", new byte[] { 41, 42, 43, 44, 192, 168, 1, 1 }, false };
            yield return new object[] { "InvalidKeyName", null, false };

            yield return new object[] { "Point", new Point(50, 60), true };
            yield return new object[] { "Size", new Size(20, 30), true };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
        [MemberData(nameof(EnglishNonStringResourceData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50935", TestPlatforms.Android)]
        public static void GetObject(string key, object expectedValue, bool requiresBinaryFormatter)
        {
            _ = requiresBinaryFormatter;
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

        [ConditionalTheory(nameof(IsDrawingSupportedAndAllowsCustomResourceTypes))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34008", TestPlatforms.Linux | TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [MemberData(nameof(EnglishImageResourceData))]
        public static void GetObject_Images(string key, object expectedValue)
        {
            var manager = new ResourceManager("System.Resources.Tests.Resources.TestResx.netstandard17", typeof(ResourceManagerTests).GetTypeInfo().Assembly);
            Assert.Equal(GetImageData(expectedValue), GetImageData(manager.GetObject(key)));
            Assert.Equal(GetImageData(expectedValue), GetImageData(manager.GetObject(key, new CultureInfo("en-US"))));
        }

        [ConditionalTheory(nameof(IsDrawingSupportedAndAllowsCustomResourceTypes))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34008", TestPlatforms.Linux | TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [MemberData(nameof(EnglishImageResourceData))]
        public static void GetObject_Images_ResourceSet(string key, object expectedValue)
        {
            var manager = new ResourceManager(
                "System.Resources.Tests.Resources.TestResx.netstandard17",
                typeof(ResourceManagerTests).GetTypeInfo().Assembly,
                typeof(ResourceSet));
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
            Assert.Equal(expectedValue, set.GetObject(key));
            Assert.Equal(expectedValue, set.GetString(key));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
        [MemberData(nameof(EnglishNonStringResourceData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50935", TestPlatforms.Android)]
        public static void GetResourceSet_NonStrings(string key, object expectedValue, bool requiresBinaryFormatter)
        {
            _ = requiresBinaryFormatter;
            var manager = new ResourceManager("System.Resources.Tests.Resources.TestResx.netstandard17", typeof(ResourceManagerTests).GetTypeInfo().Assembly);
            var culture = new CultureInfo("en-US");
            ResourceSet set = manager.GetResourceSet(culture, true, true);
            Assert.Equal(expectedValue, set.GetObject(key));
            Assert.Equal(expectedValue, set.GetObject(key));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
        [MemberData(nameof(EnglishNonStringResourceData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50935", TestPlatforms.Android)]
        public static void GetResourceSet_NonStringsIgnoreCase(string key, object expectedValue, bool requiresBinaryFormatter)
        {
            _ = requiresBinaryFormatter;
            var manager = new ResourceManager("System.Resources.Tests.Resources.TestResx.netstandard17", typeof(ResourceManagerTests).GetTypeInfo().Assembly);
            var culture = new CultureInfo("en-US");
            ResourceSet set = manager.GetResourceSet(culture, true, true);
            Assert.Equal(expectedValue, set.GetObject(key.ToLower(), true));
            Assert.Equal(expectedValue, set.GetObject(key.ToLower(), true));
        }

        public static bool IsDrawingSupportedAndAllowsCustomResourceTypes => PlatformDetection.IsDrawingSupported && AllowsCustomResourceTypes;

        [ConditionalTheory(nameof(IsDrawingSupportedAndAllowsCustomResourceTypes))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34008", TestPlatforms.Linux | TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
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
        public static void File_GetObject(string key, object expectedValue, bool requiresBinaryFormatter)
        {
            var manager = ResourceManager.CreateFileBasedResourceManager("TestResx.netstandard17", Directory.GetCurrentDirectory(), null);
            if (requiresBinaryFormatter)
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
        public static void File_GetResourceSet_NonStrings(string key, object expectedValue, bool requiresBinaryFormatter)
        {
            var manager = ResourceManager.CreateFileBasedResourceManager("TestResx.netstandard17", Directory.GetCurrentDirectory(), null);
            var culture = new CultureInfo("en-US");
            ResourceSet set = manager.GetResourceSet(culture, true, true);
            if (requiresBinaryFormatter)
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

        [Fact]
        public static void ConstructorNonRuntimeAssembly()
        {
            MockAssembly assembly = new MockAssembly();
            Assert.Throws<ArgumentException>(() => new ResourceManager("name", assembly));
            Assert.Throws<ArgumentException>(() => new ResourceManager("name", assembly, null));
        }

        [Fact]
        public static void GetStringAfterDispose()
        {
            var manager = new ResourceManager("System.Resources.Tests.Resources.TestResx", typeof(ResourceManagerTests).GetTypeInfo().Assembly);
            var culture = new CultureInfo("en-US");
            ResourceSet set = manager.GetResourceSet(culture, true, true);

            set.GetString("Any");
            ((IDisposable)set).Dispose();
            Assert.Throws<ObjectDisposedException> (() => set.GetString("Any"));
        }

        private class MockAssembly : Assembly
        {
        }
    }
}
