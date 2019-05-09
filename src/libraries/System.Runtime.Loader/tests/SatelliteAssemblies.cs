// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "UWP does not use satellite assemblies in most cases")]
    public class SatelliteAssembliesTestsFixture
    {
        public Dictionary<string, AssemblyLoadContext> contexts = new Dictionary<string, AssemblyLoadContext>();
        public SatelliteAssembliesTestsFixture()
        {
            AssemblyLoadContext satelliteAssembliesTests = new AssemblyLoadContext("SatelliteAssembliesTests");
            satelliteAssembliesTests.LoadFromAssemblyPath(typeof(SatelliteAssembliesTests).Assembly.Location);

            AssemblyLoadContext referencedClassLib = new AssemblyLoadContext("ReferencedClassLib");
            referencedClassLib.LoadFromAssemblyPath(typeof(ReferencedClassLib.Program).Assembly.Location);

            AssemblyLoadContext referencedClassLibNeutralIsSatellite = new AssemblyLoadContext("ReferencedClassLibNeutralIsSatellite");
            referencedClassLibNeutralIsSatellite.LoadFromAssemblyPath(typeof(ReferencedClassLibNeutralIsSatellite.Program).Assembly.Location);

            new AssemblyLoadContext("Empty");

            try
            {
                Assembly assembly = Assembly.LoadFile(typeof(SatelliteAssembliesTests).Assembly.Location);
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

    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "UWP does not use satellite assemblies in most cases")]
    public class SatelliteAssembliesTests : IClassFixture<SatelliteAssembliesTestsFixture>
    {
        Dictionary<string, AssemblyLoadContext> contexts;
        public SatelliteAssembliesTests(SatelliteAssembliesTestsFixture fixture)
        {
            contexts = fixture.contexts;
        }

#region DescribeTests
        [Theory]
        [InlineData("", "Neutral language Main description 1.0.0")]
        [InlineData("en", "English language Main description 1.0.0")]
        [InlineData("en-US", "English language Main description 1.0.0")]
        [InlineData("es", "Neutral language Main description 1.0.0")]
        [InlineData("es-MX", "Spanish (Mexico) language Main description 1.0.0")]
        [InlineData("fr", "Neutral language Main description 1.0.0")]
        [InlineData("fr-FR", "Neutral language Main description 1.0.0")]
        static public void mainResources(string lang, string expected)
        {
            Assert.Equal(expected, Describe(lang));
        }

        public static string Describe(string lang)
        {
            ResourceManager rm = new ResourceManager("System.Runtime.Loader.Tests.MainStrings", typeof(SatelliteAssembliesTests).Assembly);

            CultureInfo ci = CultureInfo.CreateSpecificCulture(lang);

            return rm.GetString("Describe", ci);
        }

        [Theory]
        [InlineData("Default", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "",      "Neutral language Main description 1.0.0")]
        [InlineData("Default", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "en",    "English language Main description 1.0.0")]
        [InlineData("Default", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "en-US", "English language Main description 1.0.0")]
        [InlineData("Default", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "es",    "Neutral language Main description 1.0.0")]
        [InlineData("Default", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "es-MX", "Spanish (Mexico) language Main description 1.0.0")]
        [InlineData("Default", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "fr",    "Neutral language Main description 1.0.0")]
        [InlineData("Default", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "fr-FR", "Neutral language Main description 1.0.0")]
        [InlineData("SatelliteAssembliesTests", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "",      "Neutral language Main description 1.0.0")]
        [InlineData("SatelliteAssembliesTests", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "en",    "English language Main description 1.0.0")]
        [InlineData("SatelliteAssembliesTests", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "en-US", "English language Main description 1.0.0")]
        [InlineData("SatelliteAssembliesTests", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "es",    "Neutral language Main description 1.0.0")]
        [InlineData("SatelliteAssembliesTests", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "es-MX", "Spanish (Mexico) language Main description 1.0.0")]
        [InlineData("SatelliteAssembliesTests", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "fr",    "Neutral language Main description 1.0.0")]
        [InlineData("SatelliteAssembliesTests", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "fr-FR", "Neutral language Main description 1.0.0")]
        [InlineData("LoadFile", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "",      "Neutral language Main description 1.0.0")]
        [InlineData("LoadFile", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "en",    "English language Main description 1.0.0")]
        [InlineData("LoadFile", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "en-US", "English language Main description 1.0.0")]
        [InlineData("LoadFile", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "es",    "Neutral language Main description 1.0.0")]
        [InlineData("LoadFile", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "es-MX", "Spanish (Mexico) language Main description 1.0.0")]
        [InlineData("LoadFile", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "fr",    "Neutral language Main description 1.0.0")]
        [InlineData("LoadFile", "System.Runtime.Loader.Tests.SatelliteAssembliesTests", "fr-FR", "Neutral language Main description 1.0.0")]
        [InlineData("Default", "ReferencedClassLib.Program, ReferencedClassLib", "",        "Neutral language ReferencedClassLib description 1.0.0")]
        [InlineData("Default", "ReferencedClassLib.Program, ReferencedClassLib", "en",      "English language ReferencedClassLib description 1.0.0")]
        [InlineData("Default", "ReferencedClassLib.Program, ReferencedClassLib", "en-US",   "English language ReferencedClassLib description 1.0.0")]
        [InlineData("Default", "ReferencedClassLib.Program, ReferencedClassLib", "es",      "Neutral language ReferencedClassLib description 1.0.0")]
        [InlineData("Default", "ReferencedClassLibNeutralIsSatellite.Program, ReferencedClassLibNeutralIsSatellite", "",        "Neutral (es) language ReferencedClassLibNeutralIsSatellite description 1.0.0")]
        [InlineData("Default", "ReferencedClassLibNeutralIsSatellite.Program, ReferencedClassLibNeutralIsSatellite", "en",      "English language ReferencedClassLibNeutralIsSatellite description 1.0.0")]
        [InlineData("Default", "ReferencedClassLibNeutralIsSatellite.Program, ReferencedClassLibNeutralIsSatellite", "en-US",   "English language ReferencedClassLibNeutralIsSatellite description 1.0.0")]
        [InlineData("Default", "ReferencedClassLibNeutralIsSatellite.Program, ReferencedClassLibNeutralIsSatellite", "es",      "Neutral (es) language ReferencedClassLibNeutralIsSatellite description 1.0.0")]
        [InlineData("ReferencedClassLib", "ReferencedClassLib.Program, ReferencedClassLib", "",        "Neutral language ReferencedClassLib description 1.0.0")]
        [InlineData("ReferencedClassLib", "ReferencedClassLib.Program, ReferencedClassLib", "en",      "English language ReferencedClassLib description 1.0.0")]
        [InlineData("ReferencedClassLib", "ReferencedClassLib.Program, ReferencedClassLib", "en-US",   "English language ReferencedClassLib description 1.0.0")]
        [InlineData("ReferencedClassLib", "ReferencedClassLib.Program, ReferencedClassLib", "es",      "Neutral language ReferencedClassLib description 1.0.0")]
        [InlineData("ReferencedClassLibNeutralIsSatellite", "ReferencedClassLibNeutralIsSatellite.Program, ReferencedClassLibNeutralIsSatellite", "",        "Neutral (es) language ReferencedClassLibNeutralIsSatellite description 1.0.0")]
        [InlineData("ReferencedClassLibNeutralIsSatellite", "ReferencedClassLibNeutralIsSatellite.Program, ReferencedClassLibNeutralIsSatellite", "en",      "English language ReferencedClassLibNeutralIsSatellite description 1.0.0")]
        [InlineData("ReferencedClassLibNeutralIsSatellite", "ReferencedClassLibNeutralIsSatellite.Program, ReferencedClassLibNeutralIsSatellite", "en-US",   "English language ReferencedClassLibNeutralIsSatellite description 1.0.0")]
        [InlineData("ReferencedClassLibNeutralIsSatellite", "ReferencedClassLibNeutralIsSatellite.Program, ReferencedClassLibNeutralIsSatellite", "es",      "Neutral (es) language ReferencedClassLibNeutralIsSatellite description 1.0.0")]
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

        [Theory]
        [InlineData("Default", "System.Runtime.Loader.Tests", "en")]
        [InlineData("Default", "System.Runtime.Loader.Tests", "es-MX")]
        [InlineData("Empty", "System.Runtime.Loader.Tests", "en")]
        [InlineData("Empty", "System.Runtime.Loader.Tests", "es-MX")]
        [InlineData("ReferencedClassLib", "System.Runtime.Loader.Tests", "en")]
        [InlineData("ReferencedClassLib", "System.Runtime.Loader.Tests", "es-MX")]
        [InlineData("ReferencedClassLibNeutralIsSatellite", "System.Runtime.Loader.Tests", "en")]
        [InlineData("ReferencedClassLibNeutralIsSatellite", "System.Runtime.Loader.Tests", "es-MX")]
        [InlineData("SatelliteAssembliesTests", "System.Runtime.Loader.Tests", "en")]
        [InlineData("SatelliteAssembliesTests", "System.Runtime.Loader.Tests", "es-MX")]
        [InlineData("LoadFile", "System.Runtime.Loader.Tests", "en")]
        [InlineData("LoadFile", "System.Runtime.Loader.Tests", "es-MX")]
        [InlineData("Default", "ReferencedClassLib", "en")]
        [InlineData("Default", "ReferencedClassLibNeutralIsSatellite", "en")]
        [InlineData("Default", "ReferencedClassLibNeutralIsSatellite", "es")]
        [InlineData("Empty", "ReferencedClassLib", "en")]
        [InlineData("Empty", "ReferencedClassLibNeutralIsSatellite", "en")]
        [InlineData("Empty", "ReferencedClassLibNeutralIsSatellite", "es")]
        [InlineData("LoadFile", "ReferencedClassLib", "en")]
        [InlineData("LoadFile", "ReferencedClassLibNeutralIsSatellite", "en")]
        [InlineData("LoadFile", "ReferencedClassLibNeutralIsSatellite", "es")]
        [InlineData("ReferencedClassLibNeutralIsSatellite", "ReferencedClassLib", "en")]
        [InlineData("ReferencedClassLib", "ReferencedClassLibNeutralIsSatellite", "en")]
        [InlineData("ReferencedClassLib", "ReferencedClassLibNeutralIsSatellite", "es")]
        [InlineData("ReferencedClassLib", "ReferencedClassLib", "en")]
        [InlineData("ReferencedClassLibNeutralIsSatellite", "ReferencedClassLibNeutralIsSatellite", "en")]
        [InlineData("ReferencedClassLibNeutralIsSatellite", "ReferencedClassLibNeutralIsSatellite", "es")]
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

