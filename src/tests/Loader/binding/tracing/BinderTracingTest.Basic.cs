// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using Xunit;

namespace BinderTracingTests
{
    partial class BinderTracingTest
    {
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

        [BinderTest(isolate: true, additionalLoadsToTrack: new string[] { "System.Xml" })]
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

        [BinderTest(isolate: true, additionalLoadsToTrack: new string[] { "System.Xml" })]
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

        [BinderTest(isolate: true, testSetup: nameof(PlatformAssembly), additionalLoadsToTrack: new string[] { "System.Xml" })]
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
    }
}
