// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.IO;

namespace System.Reflection.Tests
{
    internal static partial class TestUtils
    {
        // Given a runtime Type, load up the equivalent in the Test MetataLoadContext. This is for test-writing convenience so
        // that tests can write "typeof(TestClass).Project()" and get the benefits of compile-time typename checking and Intellisense.
        // It also opens the possibility of sharing Reflection tests between different type providers with minimal fuss.
        public static Type Project(this Type type)
        {
            if (type == null)
                return null;

            Assembly assembly = type.Assembly;
            string location = assembly.Location;
            if (PlatformDetection.IsNotBrowser && (location == null || location == string.Empty))
            {
                throw new Exception("Could not find the IL for assembly " + type.Assembly + " on disk. The most likely cause " +
                    "is that you built the tests for a Jitted runtime but are running them on an AoT runtime.");
            }

            Assembly projectedAssembly = s_assemblyDict.GetOrAdd(assembly,
                delegate (Assembly a)
                {
                    // The core assembly we're using might not be the one powering the runtime. Make sure we project to the core assembly the MetataLoadContext
                    // is using.
                    if (a == typeof(object).Assembly)
                    {
                        TestMetadataLoadContext.LoadFromStream(CreateStreamForCoreAssembly());
                    }

                    return TestMetadataLoadContext.LoadFromAssemblyPath(AssemblyPathHelper.GetAssemblyLocation(a));
                });

            Type projectedType = s_typeDict.GetOrAdd(type, (t) => projectedAssembly.GetType(t.FullName, throwOnError: true, ignoreCase: false));

            if (s_useRuntimeTypesForTests.Value)
                return type;

            return projectedType;
        }

        private static readonly ConcurrentDictionary<Assembly, Assembly> s_assemblyDict = new ConcurrentDictionary<Assembly, Assembly>();
        private static readonly ConcurrentDictionary<Type, Type> s_typeDict = new ConcurrentDictionary<Type, Type>();

        public static Stream CreateStreamForCoreAssembly()
        {
            // We need a core assembly in IL form. Since this version of this code is for Jitted platforms, the System.Private.Corelib
            // of the underlying runtime will do just fine.
            if (PlatformDetection.IsNotBrowser)
            {
                string assumedLocationOfCoreLibrary = typeof(object).Assembly.Location;
                if (assumedLocationOfCoreLibrary == null || assumedLocationOfCoreLibrary == string.Empty)
                {
                    throw new Exception("Could not find a core assembly to use for tests as 'typeof(object).Assembly.Location` returned " +
                        "a null or empty value. The most likely cause is that you built the tests for a Jitted runtime but are running them " +
                        "on an AoT runtime.");
                }
            }

            return File.OpenRead(GetPathToCoreAssembly());
        }

        public static string GetPathToCoreAssembly()
        {
            return AssemblyPathHelper.GetAssemblyLocation(typeof(object).Assembly);
        }

        public static string GetNameOfCoreAssembly()
        {
            return typeof(object).Assembly.GetName().Name;
        }
    }
}
