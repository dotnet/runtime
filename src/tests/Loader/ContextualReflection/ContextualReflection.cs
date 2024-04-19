// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Runtime.Remoting;
using System.Threading.Tasks;
using Xunit;

namespace ContextualReflectionTest
{
    class AGenericClass<T>
    {
    }

    class MockAssembly : Assembly
    {
        public MockAssembly() {}
    }

    public class Program : IProgram
    {
        public AssemblyLoadContext alc { get; set; }
        public Assembly alcAssembly { get; set; }
        public Type alcProgramType { get; set; }
        public IProgram alcProgramInstance { get; set; }
        public Assembly defaultAssembly { get; set; }

        [Fact]
        public static void TestEntryPoint()
        {
            Program program = new Program(isolated:false);

            program.RunTests();

            Console.WriteLine("Success");
        }

        public Program()
        {
            InitializeIsolation(true);
        }

        public Program(bool isolated)
        {
            InitializeIsolation(isolated);
        }

        public void InitializeIsolation(bool isolated)
        {
            if (isolated == false)
            {
                alc = new AssemblyLoadContext("Isolated", isCollectible: true);
                defaultAssembly = Assembly.GetExecutingAssembly();
                alcAssembly = alc.LoadFromAssemblyPath(defaultAssembly.Location);

                Assert.Equal(alcAssembly, alc.LoadFromAssemblyName(alcAssembly.GetName()));

                alcProgramType = alcAssembly.GetType("ContextualReflectionTest.Program");

                AssemblyLoadContext.Default.Resolving += TestResolve.ResolvingTestDefault;
                alc.Resolving += TestResolve.ResolvingTestIsolated;

                alcProgramInstance = (IProgram) Activator.CreateInstance(alcProgramType);
            }
            else
            {
                alcAssembly = Assembly.GetExecutingAssembly();
                alc = AssemblyLoadContext.GetLoadContext(alcAssembly);
                alcProgramType = typeof(Program);
                alcProgramInstance = this;
                defaultAssembly = AssemblyLoadContext.Default.LoadFromAssemblyName(alcAssembly.GetName());
            }
        }

        void VerifyIsolationDefault()
        {
            VerifyIsolation();
            Assert.Equal(defaultAssembly, Assembly.GetExecutingAssembly());
            Assert.Equal(AssemblyLoadContext.Default, AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()));
            Assert.NotEqual(typeof(Program), alcProgramType);
            Assert.NotEqual((object)alcProgramInstance, (object)this);
        }

        void VerifyIsolationAlc()
        {
            VerifyIsolation();
            Assert.Equal(alcAssembly, Assembly.GetExecutingAssembly());
            Assert.Equal(alc, AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()));
            Assert.Equal(typeof(Program), alcProgramType);
            Assert.Equal((object)alcProgramInstance, (object)this);
        }

        void VerifyIsolation()
        {
            Assert.Equal("Default", AssemblyLoadContext.Default.Name);

            Assert.NotNull(alc);
            Assert.NotNull(alcAssembly);
            Assert.NotNull(alcProgramType);
            Assert.NotNull(alcProgramInstance);

            Assert.Equal("Isolated", alc.Name);

            Assert.NotEqual(defaultAssembly, alcAssembly);
            Assert.NotEqual(alc, AssemblyLoadContext.Default);

            Assert.Equal(alc, AssemblyLoadContext.GetLoadContext(alcProgramInstance.alcAssembly));
            Assert.Equal(alcAssembly, alcProgramInstance.alcAssembly);
            Assert.Equal(alcProgramType, alcProgramInstance.alcProgramType);
            Assert.Equal(alcProgramInstance, alcProgramInstance.alcProgramInstance);
        }

        void VerifyTestResolve()
        {
            TestResolve.Assert(ResolveEvents.ExpectedEvent, () => AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("TestDefaultLoad")));
            TestResolve.Assert(ResolveEvents.NoEvent, () => AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("TestIsolatedLoad")));
            TestResolve.Assert(ResolveEvents.ExpectedEvent, () => alc.LoadFromAssemblyName(new AssemblyName("TestIsolatedLoad")));
            TestResolve.Assert(ResolveEvents.ExpectedEvent, () => alc.LoadFromAssemblyName(new AssemblyName("TestDefaultLoad")));

            // Make sure failure is not cached
            TestResolve.Assert(ResolveEvents.ExpectedEvent, () => AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("TestDefaultLoad")));
            TestResolve.Assert(ResolveEvents.NoEvent, () => AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("TestIsolatedLoad")));
            TestResolve.Assert(ResolveEvents.ExpectedEvent, () => alc.LoadFromAssemblyName(new AssemblyName("TestIsolatedLoad")));
            TestResolve.Assert(ResolveEvents.ExpectedEvent, () => alc.LoadFromAssemblyName(new AssemblyName("TestDefaultLoad")));
        }

        void VerifyContextualReflectionProxy()
        {
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            using (alc.EnterContextualReflection())
            {
                Assert.Equal(alc, AssemblyLoadContext.CurrentContextualReflectionContext);
                using (AssemblyLoadContext.Default.EnterContextualReflection())
                {
                    Assert.Equal(AssemblyLoadContext.Default, AssemblyLoadContext.CurrentContextualReflectionContext);
                    using (AssemblyLoadContext.EnterContextualReflection(null))
                    {
                        Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
                        using (AssemblyLoadContext.EnterContextualReflection(alcAssembly))
                        {
                            Assert.Equal(alc, AssemblyLoadContext.CurrentContextualReflectionContext);
                        }
                        Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
                    }
                    Assert.Equal(AssemblyLoadContext.Default, AssemblyLoadContext.CurrentContextualReflectionContext);
                }
                Assert.Equal(alc, AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
        }

        void VerifyUsingStatementContextualReflectionUsage()
        {
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            {
                using IDisposable alcScope = alc.EnterContextualReflection();
                Assert.Equal(alc, AssemblyLoadContext.CurrentContextualReflectionContext);
            }

            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            {
                using IDisposable alcScope = alc.EnterContextualReflection();
                Assert.Equal(alc, AssemblyLoadContext.CurrentContextualReflectionContext);
                alcScope.Dispose();
                Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
            }

            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            {
                using IDisposable alcScope = alc.EnterContextualReflection();
                Assert.Equal(alc, AssemblyLoadContext.CurrentContextualReflectionContext);
                alcScope.Dispose();
                Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
                alcScope.Dispose();
            }

            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            {
                using IDisposable alcScope = alc.EnterContextualReflection();
                Assert.Equal(alc, AssemblyLoadContext.CurrentContextualReflectionContext);
                {
                    using IDisposable defaultScope = AssemblyLoadContext.Default.EnterContextualReflection();
                    Assert.Equal(AssemblyLoadContext.Default, AssemblyLoadContext.CurrentContextualReflectionContext);

                }
                Assert.Equal(alc, AssemblyLoadContext.CurrentContextualReflectionContext);
            }

            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            {
                using IDisposable alcScope = alc.EnterContextualReflection();
                Assert.Equal(alc, AssemblyLoadContext.CurrentContextualReflectionContext);
                try
                {
                    using IDisposable defaultScope = AssemblyLoadContext.Default.EnterContextualReflection();
                    Assert.Equal(AssemblyLoadContext.Default, AssemblyLoadContext.CurrentContextualReflectionContext);

                    throw new InvalidOperationException();
                }
                catch
                {
                }
                Assert.Equal(alc, AssemblyLoadContext.CurrentContextualReflectionContext);
            }

            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            {
                using IDisposable alcScope = alc.EnterContextualReflection();
                Assert.Equal(alc, AssemblyLoadContext.CurrentContextualReflectionContext);
                using IDisposable defaultScope = AssemblyLoadContext.Default.EnterContextualReflection();
                Assert.Equal(AssemblyLoadContext.Default, AssemblyLoadContext.CurrentContextualReflectionContext);
                defaultScope.Dispose();
                Assert.Equal(alc, AssemblyLoadContext.CurrentContextualReflectionContext);
                alcScope.Dispose();
            }

            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
        }

        void VerifyBadContextualReflectionUsage()
        {
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            {
                IDisposable alcScope = alc.EnterContextualReflection();
                Assert.Equal(alc, AssemblyLoadContext.CurrentContextualReflectionContext);
                alcScope.Dispose();
            }

            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            {
                IDisposable alcScope = alc.EnterContextualReflection();
                Assert.Equal(alc, AssemblyLoadContext.CurrentContextualReflectionContext);
                alcScope.Dispose();
                alcScope.Dispose();
            }

            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            {
                IDisposable alcScope = alc.EnterContextualReflection();
                Assert.Equal(alc, AssemblyLoadContext.CurrentContextualReflectionContext);
                IDisposable defaultScope = AssemblyLoadContext.Default.EnterContextualReflection();
                Assert.Equal(AssemblyLoadContext.Default, AssemblyLoadContext.CurrentContextualReflectionContext);
                defaultScope.Dispose();
                Assert.Equal(alc, AssemblyLoadContext.CurrentContextualReflectionContext);
                alcScope.Dispose();
            }

            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            {
                IDisposable alcScope = alc.EnterContextualReflection();
                Assert.Equal(alc, AssemblyLoadContext.CurrentContextualReflectionContext);
                IDisposable defaultScope = AssemblyLoadContext.Default.EnterContextualReflection();
                Assert.Equal(AssemblyLoadContext.Default, AssemblyLoadContext.CurrentContextualReflectionContext);

                alcScope.Dispose();
                Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

                defaultScope.Dispose();
                Assert.Equal(alc, AssemblyLoadContext.CurrentContextualReflectionContext);
                alcScope.Dispose();
            }

            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            {
                IDisposable alcScope = alc.EnterContextualReflection();
                Assert.Equal(alc, AssemblyLoadContext.CurrentContextualReflectionContext);
                try
                {
                    IDisposable defaultScope = AssemblyLoadContext.EnterContextualReflection(null);
                    Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

                    throw new InvalidOperationException();
                }
                catch
                {
                }
            }

            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
        }

        void TestResolveMissingAssembly(bool isolated, Action<string> action, bool skipNullIsolated = false)
        {
            using (AssemblyLoadContext.EnterContextualReflection(null))
            {
                TestResolve.Assert(ResolveEvents.ExpectedEvent, () => action("TestDefaultLoad"));
                if (!skipNullIsolated)
                    TestResolve.Assert(isolated ? ResolveEvents.ExpectedEvent : ResolveEvents.NoEvent, () => action("TestIsolatedLoad"));
            }
            using (AssemblyLoadContext.Default.EnterContextualReflection())
            {
                TestResolve.Assert(ResolveEvents.ExpectedEvent, () => action("TestDefaultLoad"));
                TestResolve.Assert(ResolveEvents.NoEvent, () => action("TestIsolatedLoad"));
            }
            using (alc.EnterContextualReflection())
            {
                TestResolve.Assert(ResolveEvents.ExpectedEvent, () => action("TestDefaultLoad"));
                TestResolve.Assert(ResolveEvents.ExpectedEvent, () => action("TestIsolatedLoad"));
            }
        }

        void TestAssemblyLoad(bool isolated)
        {
            TestAssemblyLoad(isolated, (string assemblyName) => Assembly.Load(assemblyName));
            TestAssemblyLoad(isolated, (string assemblyName) => Assembly.Load(new AssemblyName(assemblyName)));
#pragma warning disable 618
            TestAssemblyLoad(isolated, (string assemblyName) => Assembly.LoadWithPartialName(assemblyName));
#pragma warning restore 618
        }

        void TestAssemblyLoad(bool isolated, Func<string, Assembly> assemblyLoad)
        {
            TestResolveMissingAssembly(isolated, (string assemblyName) => assemblyLoad(assemblyName));

            using (AssemblyLoadContext.EnterContextualReflection(null))
            {
                Assembly assembly = assemblyLoad("ContextualReflection");

                Assert.Equal(isolated ? alc : AssemblyLoadContext.Default, AssemblyLoadContext.GetLoadContext(assembly));

                Assembly depends = assemblyLoad("ContextualReflectionDependency");

                Assert.Equal(AssemblyLoadContext.Default, AssemblyLoadContext.GetLoadContext(depends));
            }
            using (AssemblyLoadContext.Default.EnterContextualReflection())
            {
                Assembly assembly = assemblyLoad("ContextualReflection");

                Assert.Equal(AssemblyLoadContext.Default, AssemblyLoadContext.GetLoadContext(assembly));

                Assembly depends = assemblyLoad("ContextualReflectionDependency");

                Assert.Equal(AssemblyLoadContext.Default, AssemblyLoadContext.GetLoadContext(depends));
            }
            using (alc.EnterContextualReflection())
            {
                Assembly assembly = assemblyLoad("ContextualReflection");

                Assert.Equal(alc, AssemblyLoadContext.GetLoadContext(assembly));

                Assembly depends = assemblyLoad("ContextualReflectionDependency");

                Assert.Equal(AssemblyLoadContext.Default, AssemblyLoadContext.GetLoadContext(depends));
            }
        }

        void TestTypeGetType(bool isolated)
        {
            TestTypeGetType(isolated, (string typeName) => Type.GetType(typeName));
            TestTypeGetType(isolated, (string typeName) => Type.GetType(typeName, throwOnError : false));
            TestTypeGetType(isolated, (string typeName) => Type.GetType(typeName, throwOnError : false, ignoreCase : false));
            TestTypeGetType(isolated, (string typeName) => Type.GetType(typeName, assemblyResolver : null, typeResolver : null));
            TestTypeGetType(isolated, (string typeName) => Type.GetType(typeName, assemblyResolver : null, typeResolver : null, throwOnError : false));
            TestTypeGetType(isolated, (string typeName) => Type.GetType(typeName, assemblyResolver : null, typeResolver : null, throwOnError : false, ignoreCase : false));
        }

        void TestTypeGetType(bool isolated, Func<string, System.Type> typeGetType)
        {
            TestResolveMissingAssembly(isolated, (string assemblyName) => typeGetType(string.Format("MyType, {0}", assemblyName)));

            using (AssemblyLoadContext.EnterContextualReflection(null))
            {
                {
                    Type p = typeGetType("ContextualReflectionTest.Program");

                    Assembly expectedAssembly = Assembly.GetExecutingAssembly();

                    Assert.NotNull(p);
                    Assert.Equal(expectedAssembly, p.Assembly);
                    Assert.Equal(typeof (Program), p);
                }
                {
                    Type p = typeGetType("ContextualReflectionTest.Program, ContextualReflection");

                    Assembly expectedAssembly = Assembly.GetExecutingAssembly();

                    Assert.NotNull(p);
                    Assert.Equal(expectedAssembly, p.Assembly);
                    Assert.Equal(typeof (Program), p);
                }
                {
                    Type g = typeGetType("ContextualReflectionTest.AGenericClass`1[[ContextualReflectionTest.Program, ContextualReflection]], ContextualReflection");

                    Assembly expectedAssembly = Assembly.GetExecutingAssembly();

                    Assert.NotNull(g);
                    Assert.Equal(expectedAssembly, g.Assembly);
                    Assert.Equal(expectedAssembly, g.GenericTypeArguments[0].Assembly);
                    Assert.Equal(typeof (Program), g.GenericTypeArguments[0]);
                    Assert.Equal(isolated ? alc : AssemblyLoadContext.Default, AssemblyLoadContext.GetLoadContext(g.GenericTypeArguments[0].Assembly));
                }
            }
            using (AssemblyLoadContext.Default.EnterContextualReflection())
            {
                {
                    Type p = typeGetType("ContextualReflectionTest.Program");

                    Assembly expectedAssembly = Assembly.GetExecutingAssembly();

                    Assert.NotNull(p);
                    Assert.Equal(expectedAssembly, p.Assembly);
                    Assert.Equal(typeof (Program), p);
                }
                {
                    Type p = typeGetType("ContextualReflectionTest.Program, ContextualReflection");

                    Assembly expectedAssembly = defaultAssembly;

                    Assert.NotNull(p);
                    Assert.Equal(expectedAssembly, p.Assembly);
                    Assert.Equal(AssemblyLoadContext.Default, AssemblyLoadContext.GetLoadContext(p.Assembly));
                }
                {
                    Type g = typeGetType("ContextualReflectionTest.AGenericClass`1[[ContextualReflectionTest.Program, ContextualReflection]], ContextualReflection");

                    Assembly expectedAssembly = defaultAssembly;

                    Assert.NotNull(g);
                    Assert.Equal(expectedAssembly, g.Assembly);
                    Assert.Equal(expectedAssembly, g.GenericTypeArguments[0].Assembly);
                    Assert.Equal(AssemblyLoadContext.Default, AssemblyLoadContext.GetLoadContext(g.Assembly));
                    Assert.Equal(AssemblyLoadContext.Default, AssemblyLoadContext.GetLoadContext(g.GenericTypeArguments[0].Assembly));
                }
            }
            using (alc.EnterContextualReflection())
            {
                {
                    Type p = typeGetType("ContextualReflectionTest.Program");

                    Assembly expectedAssembly = Assembly.GetExecutingAssembly();

                    Assert.NotNull(p);
                    Assert.Equal(expectedAssembly, p.Assembly);
                    Assert.Equal(typeof (Program), p);
                }
                {
                    Type p = typeGetType("ContextualReflectionTest.Program, ContextualReflection");

                    Assembly expectedAssembly = alcAssembly;

                    Assert.NotNull(p);
                    Assert.Equal(expectedAssembly, p.Assembly);
                    Assert.Equal(alc, AssemblyLoadContext.GetLoadContext(p.Assembly));
                }
                {
                    Type g = typeGetType("ContextualReflectionTest.AGenericClass`1[[ContextualReflectionTest.Program, ContextualReflection]], ContextualReflection");

                    Assembly expectedAssembly = alcAssembly;

                    Assert.NotNull(g);
                    Assert.Equal(expectedAssembly, g.Assembly);
                    Assert.Equal(expectedAssembly, g.GenericTypeArguments[0].Assembly);
                    Assert.Equal(alc, AssemblyLoadContext.GetLoadContext(g.Assembly));
                    Assert.Equal(alc, AssemblyLoadContext.GetLoadContext(g.GenericTypeArguments[0].Assembly));
                }
            }
        }

        void TestAssemblyGetType(bool isolated)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            TestResolveMissingAssembly(isolated,
                (string assemblyName) => assembly.GetType(string.Format("ContextualReflectionTest.AGenericClass`1[[MyType, {0}]]", assemblyName)));

            using (AssemblyLoadContext.EnterContextualReflection(null))
            {
                {
                    Type g = assembly.GetType("ContextualReflectionTest.AGenericClass`1[[ContextualReflectionTest.Program]]", throwOnError : false);

                    Assert.NotNull(g);
                    Assert.Equal(assembly, g.Assembly);
                    Assert.Equal(assembly, g.GenericTypeArguments[0].Assembly);
                    Assert.Equal(Assembly.GetExecutingAssembly(), g.GenericTypeArguments[0].Assembly);
                    Assert.Equal(typeof (Program), g.GenericTypeArguments[0]);
                }
                {
                    Type g = assembly.GetType("ContextualReflectionTest.AGenericClass`1[[ContextualReflectionTest.Program, ContextualReflection]]", throwOnError : false);

                    Assert.NotNull(g);
                    Assert.Equal(assembly, g.Assembly);
                    Assert.Equal(assembly, g.GenericTypeArguments[0].Assembly);
                    Assert.Equal(Assembly.GetExecutingAssembly(), g.GenericTypeArguments[0].Assembly);
                    Assert.Equal(typeof (Program), g.GenericTypeArguments[0]);
                }
                {
                    Assembly mscorlib = typeof (System.Collections.Generic.List<string>).Assembly;

                    Type m = mscorlib.GetType("System.Collections.Generic.List`1[[ContextualReflectionTest.Program, ContextualReflection]]", throwOnError : false);

                    Assembly expectedAssembly = mscorlib;

                    Assert.NotNull(m);
                    Assert.Equal(expectedAssembly, m.Assembly);
                    Assert.Equal(AssemblyLoadContext.Default, AssemblyLoadContext.GetLoadContext(m.GenericTypeArguments[0].Assembly));
                }
            }
            using (AssemblyLoadContext.Default.EnterContextualReflection())
            {
                {
                    Type g = assembly.GetType("ContextualReflectionTest.AGenericClass`1[[ContextualReflectionTest.Program]]", throwOnError : false);

                    Assert.NotNull(g);
                    Assert.Equal(assembly, g.Assembly);
                    Assert.Equal(assembly, g.GenericTypeArguments[0].Assembly);
                    Assert.Equal(Assembly.GetExecutingAssembly(), g.GenericTypeArguments[0].Assembly);
                    Assert.Equal(typeof (Program), g.GenericTypeArguments[0]);
                }
                {
                    Type g = assembly.GetType("ContextualReflectionTest.AGenericClass`1[[ContextualReflectionTest.Program, ContextualReflection]]", throwOnError : false);

                    Assembly expectedAssembly = defaultAssembly;

                    Assert.NotNull(g);
                    Assert.Equal(assembly, g.Assembly);
                    Assert.Equal(expectedAssembly, g.GenericTypeArguments[0].Assembly);
                }
                {
                    Assembly mscorlib = typeof (System.Collections.Generic.List<string>).Assembly;

                    Type m = mscorlib.GetType("System.Collections.Generic.List`1[[ContextualReflectionTest.Program, ContextualReflection]]", throwOnError : false);

                    Assembly expectedAssembly = mscorlib;

                    Assert.NotNull(m);
                    Assert.Equal(expectedAssembly, m.Assembly);
                    Assert.Equal(AssemblyLoadContext.Default, AssemblyLoadContext.GetLoadContext(m.GenericTypeArguments[0].Assembly));
                }
            }
            using (alc.EnterContextualReflection())
            {
                {
                    Type g = assembly.GetType("ContextualReflectionTest.AGenericClass`1[[ContextualReflectionTest.Program]]", throwOnError : false);

                    Assert.NotNull(g);
                    Assert.Equal(assembly, g.Assembly);
                    Assert.Equal(assembly, g.GenericTypeArguments[0].Assembly);
                    Assert.Equal(Assembly.GetExecutingAssembly(), g.GenericTypeArguments[0].Assembly);
                    Assert.Equal(typeof (Program), g.GenericTypeArguments[0]);
                }
                {
                    Type g = assembly.GetType("ContextualReflectionTest.AGenericClass`1[[ContextualReflectionTest.Program, ContextualReflection]]", throwOnError : false);

                    Assembly expectedAssembly = alcAssembly;

                    Assert.NotNull(g);
                    Assert.Equal(assembly, g.Assembly);
                    Assert.Equal(expectedAssembly, g.GenericTypeArguments[0].Assembly);
                }
                {
                    Assembly mscorlib = typeof (System.Collections.Generic.List<string>).Assembly;

                    Type m = mscorlib.GetType("System.Collections.Generic.List`1[[ContextualReflectionTest.Program, ContextualReflection]]", throwOnError : false);

                    Assembly expectedAssembly = mscorlib;

                    Assert.NotNull(m);
                    Assert.Equal(expectedAssembly, m.Assembly);
                    Assert.Equal(alc, AssemblyLoadContext.GetLoadContext(m.GenericTypeArguments[0].Assembly));
                }
            }
        }

        void TestActivatorCreateInstance(bool isolated)
        {
            TestResolveMissingAssembly(isolated, (string assemblyName) => Activator.CreateInstance(assemblyName, "MyType"));
            TestResolveMissingAssembly(isolated,
                (string assemblyName) => Activator.CreateInstance("System.Private.CoreLib", string.Format("System.Collections.Generic.List`1[[MyType, {0}]]", assemblyName)),
                skipNullIsolated : true);

            TestResolveMissingAssembly(isolated,
                (string assemblyName) => Activator.CreateInstance("ContextualReflection", string.Format("ContextualReflectionTest.AGenericClass`1[[MyType, {0}]]", assemblyName)));

            Assembly assembly = Assembly.GetExecutingAssembly();

            using (AssemblyLoadContext.EnterContextualReflection(null))
            {
                {
                    ObjectHandle objectHandle = Activator.CreateInstance(null, "ContextualReflectionTest.AGenericClass`1[[ContextualReflectionTest.Program]]");
                    Type g = objectHandle.Unwrap().GetType();

                    Assert.NotNull(g);
                    Assert.Equal(assembly, g.Assembly);
                    Assert.Equal(assembly, g.GenericTypeArguments[0].Assembly);
                    Assert.Equal(Assembly.GetExecutingAssembly(), g.GenericTypeArguments[0].Assembly);
                    Assert.Equal(typeof (Program), g.GenericTypeArguments[0]);
                }
                {
                    ObjectHandle objectHandle = Activator.CreateInstance(null, "ContextualReflectionTest.AGenericClass`1[[ContextualReflectionTest.Program, ContextualReflection]]");
                    Type g = objectHandle.Unwrap().GetType();

                    Assert.NotNull(g);
                    Assert.Equal(assembly, g.Assembly);
                    Assert.Equal(assembly, g.GenericTypeArguments[0].Assembly);
                    Assert.Equal(Assembly.GetExecutingAssembly(), g.GenericTypeArguments[0].Assembly);
                    Assert.Equal(typeof (Program), g.GenericTypeArguments[0]);
                }
                {
                    ObjectHandle objectHandle = Activator.CreateInstance("ContextualReflection" , "ContextualReflectionTest.AGenericClass`1[[ContextualReflectionTest.Program, ContextualReflection]]");
                    Type g = objectHandle.Unwrap().GetType();

                    Assembly expectedAssembly = assembly;

                    Assert.NotNull(g);
                    Assert.Equal(expectedAssembly, g.Assembly);
                    Assert.Equal(expectedAssembly, g.GenericTypeArguments[0].Assembly);
                }
                {
                    Assembly expectedAssembly = alcAssembly;

                    Assembly mscorlib = typeof (System.Collections.Generic.List<string>).Assembly;

                    ObjectHandle objectHandle = Activator.CreateInstance(mscorlib.GetName().Name, "System.Collections.Generic.List`1[[ContextualReflectionTest.Program, ContextualReflection]]");
                    Type m = objectHandle.Unwrap().GetType();

                    Assert.NotNull(m);
                    Assert.Equal(mscorlib, m.Assembly);
                    Assert.Equal(AssemblyLoadContext.Default, AssemblyLoadContext.GetLoadContext(m.GenericTypeArguments[0].Assembly));
                }
            }
            using (AssemblyLoadContext.Default.EnterContextualReflection())
            {
                {
                    ObjectHandle objectHandle = Activator.CreateInstance(null, "ContextualReflectionTest.AGenericClass`1[[ContextualReflectionTest.Program]]");
                    Type g = objectHandle.Unwrap().GetType();

                    Assert.NotNull(g);
                    Assert.Equal(assembly, g.Assembly);
                    Assert.Equal(assembly, g.GenericTypeArguments[0].Assembly);
                    Assert.Equal(Assembly.GetExecutingAssembly(), g.GenericTypeArguments[0].Assembly);
                    Assert.Equal(typeof (Program), g.GenericTypeArguments[0]);
                }
                {
                    ObjectHandle objectHandle = Activator.CreateInstance(null, "ContextualReflectionTest.AGenericClass`1[[ContextualReflectionTest.Program, ContextualReflection]]");
                    Type g = objectHandle.Unwrap().GetType();

                    Assembly expectedAssembly = defaultAssembly;

                    Assert.NotNull(g);
                    Assert.Equal(assembly, g.Assembly);
                    Assert.Equal(expectedAssembly, g.GenericTypeArguments[0].Assembly);
                }
                {
                    ObjectHandle objectHandle = Activator.CreateInstance("ContextualReflection" , "ContextualReflectionTest.AGenericClass`1[[ContextualReflectionTest.Program, ContextualReflection]]");
                    Type g = objectHandle.Unwrap().GetType();

                    Assembly expectedAssembly = defaultAssembly;

                    Assert.NotNull(g);
                    Assert.Equal(expectedAssembly, g.Assembly);
                    Assert.Equal(expectedAssembly, g.GenericTypeArguments[0].Assembly);
                }
                {
                    Assembly mscorlib = typeof (System.Collections.Generic.List<string>).Assembly;

                    ObjectHandle objectHandle = Activator.CreateInstance(mscorlib.GetName().Name, "System.Collections.Generic.List`1[[ContextualReflectionTest.Program, ContextualReflection]]");
                    Type m = objectHandle.Unwrap().GetType();

                    Assembly expectedAssembly = mscorlib;

                    Assert.NotNull(m);
                    Assert.Equal(expectedAssembly, m.Assembly);
                    Assert.Equal(AssemblyLoadContext.Default, AssemblyLoadContext.GetLoadContext(m.GenericTypeArguments[0].Assembly));
                }
            }
            using (alc.EnterContextualReflection())
            {
                {
                    ObjectHandle objectHandle = Activator.CreateInstance(null, "ContextualReflectionTest.AGenericClass`1[[ContextualReflectionTest.Program]]");
                    Type g = objectHandle.Unwrap().GetType();

                    Assert.NotNull(g);
                    Assert.Equal(assembly, g.Assembly);
                    Assert.Equal(assembly, g.GenericTypeArguments[0].Assembly);
                    Assert.Equal(Assembly.GetExecutingAssembly(), g.GenericTypeArguments[0].Assembly);
                    Assert.Equal(typeof (Program), g.GenericTypeArguments[0]);
                }
                {
                    ObjectHandle objectHandle = Activator.CreateInstance(null, "ContextualReflectionTest.AGenericClass`1[[ContextualReflectionTest.Program, ContextualReflection]]");
                    Type g = objectHandle.Unwrap().GetType();

                    Assembly expectedAssembly = alcAssembly;

                    Assert.NotNull(g);
                    Assert.Equal(assembly, g.Assembly);
                    Assert.Equal(expectedAssembly, g.GenericTypeArguments[0].Assembly);
                }
                {
                    ObjectHandle objectHandle = Activator.CreateInstance("ContextualReflection" , "ContextualReflectionTest.AGenericClass`1[[ContextualReflectionTest.Program, ContextualReflection]]");
                    Type g = objectHandle.Unwrap().GetType();

                    Assembly expectedAssembly = alcAssembly;

                    Assert.NotNull(g);
                    Assert.Equal(expectedAssembly, g.Assembly);
                    Assert.Equal(expectedAssembly, g.GenericTypeArguments[0].Assembly);
                }
                {
                    Assembly mscorlib = typeof (System.Collections.Generic.List<string>).Assembly;

                    ObjectHandle objectHandle = Activator.CreateInstance(mscorlib.GetName().Name, "System.Collections.Generic.List`1[[ContextualReflectionTest.Program, ContextualReflection]]");
                    Type m = objectHandle.Unwrap().GetType();

                    Assert.NotNull(m);
                    Assert.Equal(mscorlib, m.Assembly);
                    Assert.Equal(alc, AssemblyLoadContext.GetLoadContext(m.GenericTypeArguments[0].Assembly));
                }
            }
        }

        void TestDefineDynamicAssembly(bool collectibleContext, AssemblyBuilderAccess assemblyBuilderAccess)
        {
            AssemblyLoadContext assemblyLoadContext = collectibleContext ? new AssemblyLoadContext("DynamicAssembly Collectable context", true) : AssemblyLoadContext.Default;
            AssemblyBuilder assemblyBuilder;

            using (assemblyLoadContext.EnterContextualReflection())
            {
                assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"DynamicAssembly_{Guid.NewGuid():N}"), assemblyBuilderAccess);
            }

            AssemblyLoadContext context = AssemblyLoadContext.GetLoadContext(assemblyBuilder);
            Assert.Equal(assemblyLoadContext, context);
            Assert.Contains(assemblyLoadContext.Assemblies, a => AssemblyName.ReferenceMatchesDefinition(a.GetName(), assemblyBuilder.GetName()));
        }

        void TestMockAssemblyThrows()
        {
            Exception e = AssertExtensions.ThrowsArgumentException("activating", () => AssemblyLoadContext.EnterContextualReflection(new MockAssembly()));
        }

        public void RunTests()
        {
            VerifyIsolationDefault();
            VerifyTestResolve();
            VerifyContextualReflectionProxy();
            VerifyUsingStatementContextualReflectionUsage();
            VerifyBadContextualReflectionUsage();

            TestDynamicAssembly(true);
            TestDynamicAssembly(false);

            RunTests(isolated : false);
            alcProgramInstance.RunTestsIsolated();
        }

        public void RunTests(bool isolated)
        {
            TestAssemblyLoad(isolated);
            TestTypeGetType(isolated);
            TestAssemblyGetType(isolated);
            TestActivatorCreateInstance(isolated);
            TestMockAssemblyThrows();
        }

        public void RunTestsIsolated()
        {
            VerifyIsolationAlc();
            RunTests(isolated : true);
        }

        public void TestDynamicAssembly(bool collectibleContext)
        {
            TestDefineDynamicAssembly(collectibleContext, AssemblyBuilderAccess.Run);
            TestDefineDynamicAssembly(collectibleContext, AssemblyBuilderAccess.RunAndCollect);
        }
    }
}

