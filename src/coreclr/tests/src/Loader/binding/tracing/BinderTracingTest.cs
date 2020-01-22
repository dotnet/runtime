// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using TestLibrary;

namespace BinderTracingTests
{
    [AttributeUsage(System.AttributeTargets.Method)]
    class BinderTestAttribute : Attribute
    {
        public bool Isolate { get; private set; }
        public string TestSetup { get; private set; }
        public BinderTestAttribute(bool isolate = false, string testSetup = null)
        {
            Isolate = isolate;
            TestSetup = testSetup;
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

        private const int S_OK = unchecked((int)0);
        private const int COR_E_FILENOTFOUND = unchecked((int)0x80070002);

        private static readonly AssemblyName CoreLibName = typeof(object).Assembly.GetName();

        [BinderTest]
        public static BindOperation LoadFile()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            Assembly asm = Assembly.LoadFile(executingAssembly.Location);

            return new BindOperation()
            {
                AssemblyName = executingAssembly.GetName(),
                AssemblyPath = executingAssembly.Location,
                AssemblyLoadContext = AssemblyLoadContext.GetLoadContext(asm).ToString(),
                RequestingAssembly = CoreLibName,
                RequestingAssemblyLoadContext = DefaultALC,
                Success = true,
                ResultAssemblyName = asm.GetName(),
                ResultAssemblyPath = asm.Location,
                Cached = false
            };
        }

        [BinderTest]
        public static BindOperation LoadBytes()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            Byte[] bytes = File.ReadAllBytes(executingAssembly.Location);
            Assembly asm = Assembly.Load(bytes);

            return new BindOperation()
            {
                AssemblyName = executingAssembly.GetName(),
                AssemblyLoadContext = AssemblyLoadContext.GetLoadContext(asm).ToString(),
                RequestingAssembly = CoreLibName,
                RequestingAssemblyLoadContext = DefaultALC,
                Success = true,
                ResultAssemblyName = asm.GetName(),
                ResultAssemblyPath = asm.Location,
                Cached = false
            };
        }

        [BinderTest]
        public static BindOperation LoadFromStream()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            Stream stream = File.OpenRead(executingAssembly.Location);
            CustomALC alc = new CustomALC(nameof(LoadFromStream));
            Assembly asm = alc.LoadFromStream(stream);

            return new BindOperation()
            {
                AssemblyName = executingAssembly.GetName(),
                AssemblyLoadContext = alc.ToString(),
                RequestingAssembly = CoreLibName,
                RequestingAssemblyLoadContext = DefaultALC,
                Success = true,
                ResultAssemblyName = asm.GetName(),
                ResultAssemblyPath = asm.Location,
                Cached = false
            };
        }

        [BinderTest]
        public static BindOperation LoadFromAssemblyPath()
        {
            CustomALC alc = new CustomALC(nameof(LoadFromAssemblyPath));
            var executingAssembly = Assembly.GetExecutingAssembly();
            Assembly asm = alc.LoadFromAssemblyPath(executingAssembly.Location);

            return new BindOperation()
            {
                AssemblyName = executingAssembly.GetName(),
                AssemblyPath = executingAssembly.Location,
                AssemblyLoadContext = alc.ToString(),
                RequestingAssembly = CoreLibName,
                RequestingAssemblyLoadContext = DefaultALC,
                Success = true,
                ResultAssemblyName = asm.GetName(),
                ResultAssemblyPath = asm.Location,
                Cached = false
            };
        }

        [BinderTest(isolate: true)]
        public static BindOperation LoadFromAssemblyName()
        {
            AssemblyName assemblyName = new AssemblyName("System.Xml");
            CustomALC alc = new CustomALC(nameof(LoadFromAssemblyName));
            Assembly asm = alc.LoadFromAssemblyName(assemblyName);

            return new BindOperation()
            {
                AssemblyName = assemblyName,
                AssemblyLoadContext = alc.ToString(),
                Success = true,
                ResultAssemblyName = asm.GetName(),
                ResultAssemblyPath = asm.Location,
                Cached = false
            };
        }

        [BinderTest(isolate: true)]
        public static BindOperation LoadFrom()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            Assembly asm = Assembly.LoadFrom(executingAssembly.Location);

            return new BindOperation()
            {
                AssemblyName = executingAssembly.GetName(),
                AssemblyPath = executingAssembly.Location,
                AssemblyLoadContext = DefaultALC,
                RequestingAssembly = CoreLibName,
                RequestingAssemblyLoadContext = DefaultALC,
                Success = true,
                ResultAssemblyName = asm.GetName(),
                ResultAssemblyPath = asm.Location,
                Cached = false
            };
        }

        [BinderTest(isolate: true)]
        public static BindOperation PlatformAssembly()
        {
            var assemblyName = new AssemblyName("System.Xml");
            Assembly asm = Assembly.Load(assemblyName);

            return new BindOperation()
            {
                AssemblyName = assemblyName,
                AssemblyLoadContext = DefaultALC,
                RequestingAssembly = Assembly.GetExecutingAssembly().GetName(),
                RequestingAssemblyLoadContext = DefaultALC,
                Success = true,
                ResultAssemblyName = asm.GetName(),
                ResultAssemblyPath = asm.Location,
                Cached = false,
                ProbedPaths = new List<ProbedPath>()
                {
                    new ProbedPath()
                    {
                        FilePath = asm.Location,
                        Source = ProbedPath.PathSource.ApplicationAssemblies,
                        Result = S_OK
                    }
                }
            };
        }

        [BinderTest(isolate: true, testSetup: nameof(PlatformAssembly))]
        public static BindOperation PlatformAssembly_Cached()
        {
            BindOperation bind = PlatformAssembly();
            bind.Cached = true;
            bind.ProbedPaths.Clear();
            return bind;
        }

        [BinderTest(isolate: true)]
        public static BindOperation Reflection()
        {
            var t = GetDependentAssemblyType();

            return new BindOperation()
            {
                AssemblyName = new AssemblyName(DependentAssemblyName),
                AssemblyLoadContext = DefaultALC,
                RequestingAssembly = Assembly.GetExecutingAssembly().GetName(),
                RequestingAssemblyLoadContext = DefaultALC,
                Success = true,
                ResultAssemblyName = t.Assembly.GetName(),
                ResultAssemblyPath = t.Assembly.Location,
                Cached = false,
            };
        }

        [BinderTest(isolate: true, testSetup: nameof(Reflection))]
        public static BindOperation Reflection_Cached()
        {
            BindOperation bind = Reflection();
            bind.Cached = true;
            return bind;
        }

        [BinderTest(isolate: true)]
        public static BindOperation Reflection_CustomALC()
        {
            CustomALC alc = new CustomALC(nameof(Reflection_CustomALC));
            Type testClass = LoadTestClassInALC(alc);
            MethodInfo method = testClass.GetMethod(nameof(GetDependentAssemblyType), BindingFlags.NonPublic | BindingFlags.Static);
            Type t = (Type)method.Invoke(null, new object[0]);

            return new BindOperation()
            {
                AssemblyName = new AssemblyName(DependentAssemblyName),
                AssemblyLoadContext = alc.ToString(),
                RequestingAssembly = testClass.Assembly.GetName(),
                RequestingAssemblyLoadContext = alc.ToString(),
                Success = true,
                ResultAssemblyName = t.Assembly.GetName(),
                ResultAssemblyPath = t.Assembly.Location,
                Cached = false,
            };
        }

        [BinderTest(isolate: true)]
        public static BindOperation ContextualReflection_DefaultToCustomALC()
        {
            Type t;
            CustomALC alc = new CustomALC(nameof(ContextualReflection_DefaultToCustomALC));
            using (alc.EnterContextualReflection())
            {
                t = GetDependentAssemblyType();
            }

            return new BindOperation()
            {
                AssemblyName = new AssemblyName(DependentAssemblyName),
                AssemblyLoadContext = alc.ToString(),
                RequestingAssembly = Assembly.GetExecutingAssembly().GetName(),
                RequestingAssemblyLoadContext = DefaultALC,
                Success = true,
                ResultAssemblyName = t.Assembly.GetName(),
                ResultAssemblyPath = t.Assembly.Location,
                Cached = false,
            };
        }

        [BinderTest(isolate: true)]
        public static BindOperation ContextualReflection_CustomToDefaultALC()
        {
            CustomALC alc = new CustomALC(nameof(ContextualReflection_CustomToDefaultALC));
            Type testClass = LoadTestClassInALC(alc);
            MethodInfo method = testClass.GetMethod(nameof(GetDependentAssemblyType), BindingFlags.NonPublic | BindingFlags.Static);

            Type t;
            using (AssemblyLoadContext.Default.EnterContextualReflection())
            {
                t = (Type)method.Invoke(null, new object[0]);
            }

            return new BindOperation()
            {
                AssemblyName = new AssemblyName(DependentAssemblyName),
                AssemblyLoadContext = DefaultALC,
                RequestingAssembly = testClass.Assembly.GetName(),
                RequestingAssemblyLoadContext = alc.ToString(),
                Success = true,
                ResultAssemblyName = t.Assembly.GetName(),
                ResultAssemblyPath = t.Assembly.Location,
                Cached = false,
            };
        }

        [BinderTest(isolate: true)]
        public static BindOperation JITLoad()
        {
            Assembly asm = UseDependentAssembly();

            return new BindOperation()
            {
                AssemblyName = asm.GetName(),
                AssemblyLoadContext = DefaultALC,
                RequestingAssembly = Assembly.GetExecutingAssembly().GetName(),
                RequestingAssemblyLoadContext = DefaultALC,
                Success = true,
                ResultAssemblyName = asm.GetName(),
                ResultAssemblyPath = asm.Location,
                Cached = false,
            };
        }

        [BinderTest(isolate: true)]
        public static BindOperation JITLoad_CustomALC()
        {
            CustomALC alc = new CustomALC(nameof(JITLoad_CustomALC));
            Type testClass= LoadTestClassInALC(alc);
            MethodInfo method = testClass.GetMethod(nameof(UseDependentAssembly), BindingFlags.NonPublic | BindingFlags.Static);
            Assembly asm = (Assembly)method.Invoke(null, new object[0]);

            return new BindOperation()
            {
                AssemblyName = asm.GetName(),
                AssemblyLoadContext = alc.ToString(),
                RequestingAssembly = testClass.Assembly.GetName(),
                RequestingAssemblyLoadContext = alc.ToString(),
                Success = true,
                ResultAssemblyName = asm.GetName(),
                ResultAssemblyPath = asm.Location,
                Cached = false
            };
        }

        public static bool RunAllTests()
        {
            MethodInfo[] methods = typeof(BinderTracingTest)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.GetCustomAttribute<BinderTestAttribute>() != null && m.ReturnType == typeof(BindOperation))
                .ToArray();

            foreach (var method in methods)
            {
                BinderTestAttribute attribute = method.GetCustomAttribute<BinderTestAttribute>();
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
                    Assert.IsTrue(method != null && method.GetCustomAttribute<BinderTestAttribute>() != null && method.ReturnType == typeof(BindOperation), "Invalid test method specified");
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
            Console.WriteLine($"Running {method.Name}...");
            try
            {
                BinderTestAttribute attribute = method.GetCustomAttribute<BinderTestAttribute>();
                if (!string.IsNullOrEmpty(attribute.TestSetup))
                {
                    MethodInfo setupMethod = method.DeclaringType
                        .GetMethod(attribute.TestSetup, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    Assert.IsTrue(setupMethod != null);
                    setupMethod.Invoke(null, new object[0]);
                }

                Func<BindOperation> func = (Func<BindOperation>)method.CreateDelegate(typeof(Func<BindOperation>));
                using (var listener = new BinderEventListener())
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
            var startInfo = new ProcessStartInfo()
            {
                FileName = Process.GetCurrentProcess().MainModule.FileName,
                Arguments = $"{Assembly.GetExecutingAssembly().Location} {method.Name}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Console.WriteLine($"Launching process for {method.Name}...");
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
            Assert.IsTrue(binds.Length == 1, $"Bind event count for {assemblyName} - expected: 1, actual: {binds.Length}");
            BindOperation actual = binds[0];

            Helpers.ValidateBindOperation(expected, actual);
        }
    }
}
