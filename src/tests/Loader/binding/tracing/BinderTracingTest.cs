// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using Xunit;

namespace BinderTracingTests
{
    [AttributeUsage(System.AttributeTargets.Method)]
    class BinderTestAttribute : Attribute
    {
        public bool Isolate { get; private set; }
        public string ActiveIssue { get; private set; }
        public string TestSetup { get; private set; }
        public string[] AdditionalLoadsToTrack { get; private set; }
        public BinderTestAttribute(bool isolate = false, string testSetup = null, string[] additionalLoadsToTrack = null, string activeIssue = null)
        {
            Isolate = isolate;
            TestSetup = testSetup;
            AdditionalLoadsToTrack = additionalLoadsToTrack;
            ActiveIssue = activeIssue;
        }
    }

    partial class BinderTracingTest
    {
        public class CustomALC : AssemblyLoadContext
        {
            private string assemblyNameToLoad;
            private string assemblyPathToLoad;
            private bool throwOnLoad;

            public CustomALC(string name, bool throwOnLoad = false) : base(name)
            {
                this.throwOnLoad = throwOnLoad;
            }

            public void EnableLoad(string assemblyName, string path)
            {
                assemblyNameToLoad = assemblyName;
                assemblyPathToLoad = path;
            }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                if (throwOnLoad)
                    throw new Exception($"Exception on Load in '{ToString()}'");

                if (!string.IsNullOrEmpty(assemblyNameToLoad) && assemblyName.Name == assemblyNameToLoad)
                    return LoadFromAssemblyPath(assemblyPathToLoad);

                return null;
            }
        }

        private const string DefaultALC = "Default";
        private const string DependentAssemblyName = "AssemblyToLoad";
        private const string DependentAssemblyTypeName = "AssemblyToLoad.Program";
        private const string SubdirectoryAssemblyName = "AssemblyToLoad_Subdirectory";

        private static CultureInfo SatelliteCulture = CultureInfo.CreateSpecificCulture("fr-FR");

        private const int S_OK = unchecked((int)0);
        private const int COR_E_FILENOTFOUND = unchecked((int)0x80070002);

        private static readonly AssemblyName CoreLibName = typeof(object).Assembly.GetName();

        public static bool RunAllTests()
        {
            MethodInfo[] methods = typeof(BinderTracingTest)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.GetCustomAttribute<BinderTestAttribute>() != null &&
                    m.ReturnType == typeof(BindOperation) &&
                    m.GetCustomAttribute<BinderTestAttribute>().ActiveIssue == null)
                .ToArray();

            foreach (var method in methods)
            {
                BinderTestAttribute attribute = method.GetCustomAttribute<BinderTestAttribute>();
                if (attribute.Isolate && Environment.GetEnvironmentVariable("DOTNET_GCStress") != null)
                    continue;

                bool success = attribute.Isolate
                    ? RunTestInSeparateProcess(method)
                    : RunSingleTest(method);
                if (!success)
                {
                    return false;
                }
            }

            return true;
        }

        public static int Main(string[] args)
        {
            bool success;
            try
            {
                if (args.Length == 0)
                {
                    success = RunAllTests();
                }
                else
                {
                    // Run specific test - first argument should be the test method name
                    MethodInfo method = typeof(BinderTracingTest)
                        .GetMethod(args[0], BindingFlags.Public | BindingFlags.Static);
                    Assert.True(method != null &&
                        method.GetCustomAttribute<BinderTestAttribute>() != null &&
                        method.ReturnType == typeof(BindOperation) &&
                        method.GetCustomAttribute<BinderTestAttribute>().ActiveIssue == null);
                    success = RunSingleTest(method);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Test Failure: {e}");
                return 101;
            }

            return success ? 100 : 101;
        }

        private static Assembly UseDependentAssembly()
        {
            var p = new AssemblyToLoad.Program();
            return Assembly.GetAssembly(p.GetType());
        }

        private static Type GetDependentAssemblyType()
        {
            return Type.GetType($"{DependentAssemblyTypeName}, {DependentAssemblyName}");
        }

        private static Type LoadTestClassInALC(AssemblyLoadContext alc)
        {
            Assembly asm = alc.LoadFromAssemblyPath(Assembly.GetExecutingAssembly().Location);
            return asm.GetType(typeof(BinderTracingTest).FullName);
        }

        private static bool RunSingleTest(MethodInfo method)
        {
            Console.WriteLine($"[{DateTime.Now:T}] Running {method.Name}...");
            try
            {
                BinderTestAttribute attribute = method.GetCustomAttribute<BinderTestAttribute>();
                if (!string.IsNullOrEmpty(attribute.TestSetup))
                {
                    MethodInfo setupMethod = method.DeclaringType
                        .GetMethod(attribute.TestSetup, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    Assert.True(setupMethod != null);
                    setupMethod.Invoke(null, new object[0]);
                }

                var loadsToTrack = new string[]
                {
                    Assembly.GetExecutingAssembly().GetName().Name,
                    DependentAssemblyName,
                    $"{DependentAssemblyName}.resources",
                    SubdirectoryAssemblyName,
                    $"{SubdirectoryAssemblyName}.resources",
                };
                if (attribute.AdditionalLoadsToTrack != null)
                    loadsToTrack = loadsToTrack.Union(attribute.AdditionalLoadsToTrack).ToArray();

                Func<BindOperation> func = (Func<BindOperation>)method.CreateDelegate(typeof(Func<BindOperation>));
                using (var listener = new BinderEventListener(loadsToTrack))
                {
                    BindOperation expected = func();
                    ValidateSingleBind(listener, expected.AssemblyName, expected);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Test {method.Name} failed: {e}");
                return false;
            }

            return true;
        }

        private static bool RunTestInSeparateProcess(MethodInfo method)
        {
            var startInfo = new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName, new[] { Assembly.GetExecutingAssembly().Location, method.Name })
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Console.WriteLine($"[{DateTime.Now:T}] Launching process for {method.Name}...");
            using (Process p = Process.Start(startInfo))
            {
                p.OutputDataReceived += (_, args) => Console.WriteLine(args.Data);
                p.BeginOutputReadLine();

                p.ErrorDataReceived += (_, args) => Console.Error.WriteLine(args.Data);
                p.BeginErrorReadLine();

                p.WaitForExit();
                return p.ExitCode == 100;
            }
        }

        private static void ValidateSingleBind(BinderEventListener listener, AssemblyName assemblyName, BindOperation expected)
        {
            BindOperation[] binds = listener.WaitAndGetEventsForAssembly(assemblyName);
            Assert.True(binds.Length == 1, $"Bind event count for {assemblyName} - expected: 1, actual: {binds.Length}");
            BindOperation actual = binds[0];

            Helpers.ValidateBindOperation(expected, actual);
        }
    }
}
