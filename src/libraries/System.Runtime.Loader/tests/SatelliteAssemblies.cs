// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Globalization;
using Xunit;
using ReferencedClassLib;
using ReferencedClassLibNeutralIsSatellite;

namespace System.Runtime.Loader.Tests
{
    public class SatelliteAssembliesTestsFixture
    {
        public Dictionary<string, AssemblyLoadContext> contexts = new Dictionary<string, AssemblyLoadContext>();
        public SatelliteAssembliesTestsFixture()
        {
            AssemblyLoadContext satelliteAssembliesTests = new AssemblyLoadContext("SatelliteAssembliesTests");
            var satelliteAssembliesTestsPath = AssemblyPathHelper.GetAssemblyLocation(typeof(SatelliteAssembliesTests).Assembly);
            satelliteAssembliesTests.LoadFromAssemblyPath(satelliteAssembliesTestsPath);

            AssemblyLoadContext referencedClassLib = new AssemblyLoadContext("ReferencedClassLib");
            var referencedClassLibPath = AssemblyPathHelper.GetAssemblyLocation(typeof(ReferencedClassLib.Program).Assembly);
            referencedClassLib.LoadFromAssemblyPath(referencedClassLibPath);

            AssemblyLoadContext referencedClassLibNeutralIsSatellite = new AssemblyLoadContext("ReferencedClassLibNeutralIsSatellite");
            var referencedClassLibNeutralIsSatellitePath = AssemblyPathHelper.GetAssemblyLocation(typeof(ReferencedClassLibNeutralIsSatellite.Program).Assembly);
            referencedClassLibNeutralIsSatellite.LoadFromAssemblyPath(referencedClassLibNeutralIsSatellitePath);

            new AssemblyLoadContext("Empty");

            try
            {
                Assembly assembly = Assembly.LoadFile(satelliteAssembliesTestsPath);
                contexts["LoadFile"] = AssemblyLoadContext.GetLoadContext(assembly);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            foreach (var alc in AssemblyLoadContext.All)
            {
                if (alc.Name != null)
                    contexts[alc.Name] = alc;
            }
        }
    }

    public class SatelliteAssembliesTests : IClassFixture<SatelliteAssembliesTestsFixture>
    {
        Dictionary<string, AssemblyLoadContext> contexts;
        public SatelliteAssembliesTests(SatelliteAssembliesTestsFixture fixture)
        {
            contexts = fixture.contexts;
        }

#region DescribeTests

        public static IEnumerable<object[]> MainResources_TestData()
        {
            yield return new object[] { "", "Neutral language Main description 1.0.0" };

            if (PlatformDetection.IsNotInvariantGlobalization)
            {
                yield return new object[] { "en", "English language Main description 1.0.0" };
                yield return new object[] { "en-US", "English language Main description 1.0.0" };
                yield return new object[] { "es", "Neutral language Main description 1.0.0" };
                yield return new object[] { "es-MX", "Spanish (Mexico) language Main description 1.0.0" };
                yield return new object[] { "fr", "Neutral language Main description 1.0.0" };
                yield return new object[] { "fr-FR", "Neutral language Main description 1.0.0" };
            }
        }

        [Theory]
        [MemberData(nameof(MainResources_TestData))]
        public static void mainResources(string lang, string expected)
        {
            Assert.Equal(expected, Describe(lang));
        }

        public static string Describe(string lang)
        {
            ResourceManager rm = new ResourceManager("System.Runtime.Loader.Tests.MainStrings", typeof(SatelliteAssembliesTests).Assembly);

            CultureInfo ci = CultureInfo.CreateSpecificCulture(lang);

            return rm.GetString("Describe", ci);
        }

        public static IEnumerable<object[]> DescribeLib_TestData()
        {
            yield return new object[] { "Default", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "",      "Neutral language Main description 1.0.0" };
            yield return new object[] { "Default", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "en",    "English language Main description 1.0.0" };
            yield return new object[] { "Default", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "en-US", "English language Main description 1.0.0" };
            yield return new object[] { "Default", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "es",    "Neutral language Main description 1.0.0" };
            yield return new object[] { "Default", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "es-MX", "Spanish (Mexico) language Main description 1.0.0" };
            yield return new object[] { "Default", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "fr",    "Neutral language Main description 1.0.0" };
            yield return new object[] { "Default", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "fr-FR", "Neutral language Main description 1.0.0" };
            yield return new object[] { "SatelliteAssembliesTests", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "",      "Neutral language Main description 1.0.0" };
            yield return new object[] { "SatelliteAssembliesTests", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "en",    "English language Main description 1.0.0" };
            yield return new object[] { "SatelliteAssembliesTests", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "en-US", "English language Main description 1.0.0" };
            yield return new object[] { "SatelliteAssembliesTests", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "es",    "Neutral language Main description 1.0.0" };
            yield return new object[] { "SatelliteAssembliesTests", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "es-MX", "Spanish (Mexico) language Main description 1.0.0" };
            yield return new object[] { "SatelliteAssembliesTests", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "fr",    "Neutral language Main description 1.0.0" };
            yield return new object[] { "SatelliteAssembliesTests", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "fr-FR", "Neutral language Main description 1.0.0" };
            yield return new object[] { "LoadFile", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "",      "Neutral language Main description 1.0.0" };
            yield return new object[] { "LoadFile", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "en",    "English language Main description 1.0.0" };
            yield return new object[] { "LoadFile", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "en-US", "English language Main description 1.0.0" };
            yield return new object[] { "LoadFile", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "es",    "Neutral language Main description 1.0.0" };
            yield return new object[] { "LoadFile", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "es-MX", "Spanish (Mexico) language Main description 1.0.0" };
            yield return new object[] { "LoadFile", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "fr",    "Neutral language Main description 1.0.0" };
            yield return new object[] { "LoadFile", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "fr-FR", "Neutral language Main description 1.0.0" };
            yield return new object[] { "Default", "ReferencedClassLib.Program, ReferencedClassLib", "",        "Neutral language ReferencedClassLib description 1.0.0" };
            yield return new object[] { "Default", "ReferencedClassLib.Program, ReferencedClassLib", "en",      "English language ReferencedClassLib description 1.0.0" };
            yield return new object[] { "Default", "ReferencedClassLib.Program, ReferencedClassLib", "en-US",   "English language ReferencedClassLib description 1.0.0" };
            yield return new object[] { "Default", "ReferencedClassLib.Program, ReferencedClassLib", "es",      "Neutral language ReferencedClassLib description 1.0.0" };
            yield return new object[] { "Default", "ReferencedClassLibNeutralIsSatellite.Program, ReferencedClassLibNeutralIsSatellite", "",        "Neutral (es) language ReferencedClassLibNeutralIsSatellite description 1.0.0" };
            yield return new object[] { "Default", "ReferencedClassLibNeutralIsSatellite.Program, ReferencedClassLibNeutralIsSatellite", "en",      "English language ReferencedClassLibNeutralIsSatellite description 1.0.0" };
            yield return new object[] { "Default", "ReferencedClassLibNeutralIsSatellite.Program, ReferencedClassLibNeutralIsSatellite", "en-US",   "English language ReferencedClassLibNeutralIsSatellite description 1.0.0" };
            yield return new object[] { "Default", "ReferencedClassLibNeutralIsSatellite.Program, ReferencedClassLibNeutralIsSatellite", "es",      "Neutral (es) language ReferencedClassLibNeutralIsSatellite description 1.0.0" };
            yield return new object[] { "ReferencedClassLib", "ReferencedClassLib.Program, ReferencedClassLib", "",        "Neutral language ReferencedClassLib description 1.0.0" };
            yield return new object[] { "ReferencedClassLib", "ReferencedClassLib.Program, ReferencedClassLib", "en",      "English language ReferencedClassLib description 1.0.0" };
            yield return new object[] { "ReferencedClassLib", "ReferencedClassLib.Program, ReferencedClassLib", "en-US",   "English language ReferencedClassLib description 1.0.0" };
            yield return new object[] { "ReferencedClassLib", "ReferencedClassLib.Program, ReferencedClassLib", "es",      "Neutral language ReferencedClassLib description 1.0.0" };
            yield return new object[] { "ReferencedClassLibNeutralIsSatellite", "ReferencedClassLibNeutralIsSatellite.Program, ReferencedClassLibNeutralIsSatellite", "",        "Neutral (es) language ReferencedClassLibNeutralIsSatellite description 1.0.0" };
            yield return new object[] { "ReferencedClassLibNeutralIsSatellite", "ReferencedClassLibNeutralIsSatellite.Program, ReferencedClassLibNeutralIsSatellite", "en",      "English language ReferencedClassLibNeutralIsSatellite description 1.0.0" };
            yield return new object[] { "ReferencedClassLibNeutralIsSatellite", "ReferencedClassLibNeutralIsSatellite.Program, ReferencedClassLibNeutralIsSatellite", "en-US",   "English language ReferencedClassLibNeutralIsSatellite description 1.0.0" };
            yield return new object[] { "ReferencedClassLibNeutralIsSatellite", "ReferencedClassLibNeutralIsSatellite.Program, ReferencedClassLibNeutralIsSatellite", "es",      "Neutral (es) language ReferencedClassLibNeutralIsSatellite description 1.0.0" };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        [MemberData(nameof(DescribeLib_TestData))]
        public void describeLib(string alc, string type, string culture, string expected)
        {
            string result = "Oops";
            try
            {
                using (contexts[alc].EnterContextualReflection())
                {
                    Type describeType = Type.GetType(type);

                    result = (String)describeType.InvokeMember("Describe", BindingFlags.InvokeMethod, null, null, new object[] { culture });
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                result = "threw:";
            }

            Assert.Equal(expected, result);
        }
#endregion

        public static IEnumerable<object[]> SatelliteLoadsCorrectly_TestData()
        {
            yield return new object[] { "Default", "System.Runtime.Loader.Tests", "en" };
            yield return new object[] { "Default", "System.Runtime.Loader.Tests", "es-MX" };
            yield return new object[] { "Empty", "System.Runtime.Loader.Tests", "en" };
            yield return new object[] { "Empty", "System.Runtime.Loader.Tests", "es-MX" };
            yield return new object[] { "ReferencedClassLib", "System.Runtime.Loader.Tests", "en" };
            yield return new object[] { "ReferencedClassLib", "System.Runtime.Loader.Tests", "es-MX" };
            yield return new object[] { "ReferencedClassLibNeutralIsSatellite", "System.Runtime.Loader.Tests", "en" };
            yield return new object[] { "ReferencedClassLibNeutralIsSatellite", "System.Runtime.Loader.Tests", "es-MX" };
            yield return new object[] { "SatelliteAssembliesTests", "System.Runtime.Loader.Tests", "en" };
            yield return new object[] { "SatelliteAssembliesTests", "System.Runtime.Loader.Tests", "es-MX" };
            yield return new object[] { "LoadFile", "System.Runtime.Loader.Tests", "en" };
            yield return new object[] { "LoadFile", "System.Runtime.Loader.Tests", "es-MX" };
            yield return new object[] { "Default", "ReferencedClassLib", "en" };
            yield return new object[] { "Default", "ReferencedClassLibNeutralIsSatellite", "en" };
            yield return new object[] { "Default", "ReferencedClassLibNeutralIsSatellite", "es" };
            yield return new object[] { "Empty", "ReferencedClassLib", "en" };
            yield return new object[] { "Empty", "ReferencedClassLibNeutralIsSatellite", "en" };
            yield return new object[] { "Empty", "ReferencedClassLibNeutralIsSatellite", "es" };
            yield return new object[] { "LoadFile", "ReferencedClassLib", "en" };
            yield return new object[] { "LoadFile", "ReferencedClassLibNeutralIsSatellite", "en" };
            yield return new object[] { "LoadFile", "ReferencedClassLibNeutralIsSatellite", "es" };
            yield return new object[] { "ReferencedClassLibNeutralIsSatellite", "ReferencedClassLib", "en" };
            yield return new object[] { "ReferencedClassLib", "ReferencedClassLibNeutralIsSatellite", "en" };
            yield return new object[] { "ReferencedClassLib", "ReferencedClassLibNeutralIsSatellite", "es" };
            yield return new object[] { "ReferencedClassLib", "ReferencedClassLib", "en" };
            yield return new object[] { "ReferencedClassLibNeutralIsSatellite", "ReferencedClassLibNeutralIsSatellite", "en" };
            yield return new object[] { "ReferencedClassLibNeutralIsSatellite", "ReferencedClassLibNeutralIsSatellite", "es" };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        [MemberData(nameof(SatelliteLoadsCorrectly_TestData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/39379", TestPlatforms.Browser)]
        public void SatelliteLoadsCorrectly(string alc, string assemblyName, string culture)
        {
            AssemblyName satelliteAssemblyName = new AssemblyName(assemblyName + ".resources");
            satelliteAssemblyName.CultureInfo = new CultureInfo(culture);

            AssemblyLoadContext assemblyLoadContext = contexts[alc];

            Assembly satelliteAssembly = assemblyLoadContext.LoadFromAssemblyName(satelliteAssemblyName);

            Assert.NotNull(satelliteAssembly);

            AssemblyName parentAssemblyName = new AssemblyName(assemblyName);
            Assembly parentAssembly = assemblyLoadContext.LoadFromAssemblyName(parentAssemblyName);

            Assert.Equal(AssemblyLoadContext.GetLoadContext(parentAssembly), AssemblyLoadContext.GetLoadContext(satelliteAssembly));
        }
    }
}
