// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Runtime.Remoting;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Runtime.Loader.Tests
{
    class AGenericClass<T>
    {
    }

    class MockAssembly : Assembly
    {
        public MockAssembly() {}
    }

    public class ContextualReflectionTestFixture : IContextualReflectionTestFixture
    {
        public AssemblyLoadContext isolatedAlc { get; }
        public Assembly isolatedAlcAssembly { get; }
        public Type isolatedAlcFixtureType { get; }
        public IContextualReflectionTestFixture isolatedAlcFixtureInstance { get; }

        public AssemblyLoadContext defaultAlc { get; }
        public Assembly defaultAlcAssembly { get; }
        public Type defaultAlcFixtureType { get; }

        public Assembly GetExecutingAssembly() { return Assembly.GetExecutingAssembly(); }

        public Assembly AssemblyLoad(AssemblyName name) { return Assembly.Load(name); }

        public Assembly AssemblyLoad(string name) { return Assembly.Load(name); }

#pragma warning disable 618
        public Assembly AssemblyLoadWithPartialName(string name) { return Assembly.LoadWithPartialName(name); }
#pragma warning restore 618

        public Type TypeGetType(string typeName)
        { return Type.GetType(typeName); }

        public Type TypeGetType(string typeName, bool throwOnError)
        { return Type.GetType(typeName, throwOnError); }

        public Type TypeGetType(string typeName, bool throwOnError, bool ignoreCase)
        { return Type.GetType(typeName, throwOnError, ignoreCase); }

        public Type TypeGetType(string typeName, Func<AssemblyName,Assembly> assemblyResolver, Func<Assembly, string, Boolean, Type> typeResolver)
        { return Type.GetType(typeName, assemblyResolver, typeResolver); }

        public Type TypeGetType(string typeName, Func<AssemblyName,Assembly> assemblyResolver, Func<Assembly, string, Boolean, Type> typeResolver, bool throwOnError)
        { return Type.GetType(typeName, assemblyResolver, typeResolver, throwOnError); }

        public Type TypeGetType(string typeName, Func<AssemblyName,Assembly> assemblyResolver, Func<Assembly, string, Boolean, Type> typeResolver, bool throwOnError, bool ignoreCase)
        { return Type.GetType(typeName, assemblyResolver, typeResolver, throwOnError, ignoreCase); }

        public Type AssemblyGetType(Assembly assembly, string typeName)
        { return assembly.GetType(typeName); }

        public Type AssemblyGetType(Assembly assembly, string typeName, bool throwOnError)
        { return assembly.GetType(typeName, throwOnError); }

        public Type AssemblyGetType(Assembly assembly, string typeName, bool throwOnError, bool ignoreCase)
        { return assembly.GetType(typeName, throwOnError, ignoreCase); }

        public ObjectHandle ActivatorCreateInstance(string assemblyName, string typeName)
        { return Activator.CreateInstance(assemblyName, typeName); }

        public ObjectHandle ActivatorCreateInstance(string assemblyName, string typeName, object[] activationAttributes)
        { return Activator.CreateInstance(assemblyName, typeName, activationAttributes); }

        public ObjectHandle ActivatorCreateInstance(string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, object[] args, System.Globalization.CultureInfo culture, object[] activationAttributes)
        { return Activator.CreateInstance(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes); }

        public ContextualReflectionTestFixture()
        {
            SetPreConditions();

            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            AssemblyLoadContext executingAlc = AssemblyLoadContext.GetLoadContext(executingAssembly);

            defaultAlc = AssemblyLoadContext.Default;
            defaultAlcAssembly = AssemblyLoadContext.Default.LoadFromAssemblyName(executingAssembly.GetName());
            defaultAlcFixtureType = defaultAlcAssembly.GetType("System.Runtime.Loader.Tests.ContextualReflectionTestFixture");

            if (executingAlc == AssemblyLoadContext.Default)
            {
                isolatedAlc = new AssemblyLoadContext("Isolated", isCollectible: true);
                isolatedAlcAssembly = isolatedAlc.LoadFromAssemblyPath(defaultAlcAssembly.Location);

                isolatedAlcFixtureType = isolatedAlcAssembly.GetType("System.Runtime.Loader.Tests.ContextualReflectionTestFixture");

                isolatedAlcFixtureInstance = (IContextualReflectionTestFixture) Activator.CreateInstance(isolatedAlcFixtureType);
            }
            else
            {
                isolatedAlc = executingAlc;
                isolatedAlcAssembly = executingAssembly;
                isolatedAlcFixtureType = typeof(ContextualReflectionTestFixture);
                isolatedAlcFixtureInstance = this;
            }
        }

        public void SetPreConditions()
        {
            AssemblyLoadContext.EnterContextualReflection(null);
        }

        public void FixtureSetupAssertions()
        {
            Assert.NotNull(defaultAlc);
            Assert.NotNull(defaultAlcAssembly);
            Assert.NotNull(defaultAlcFixtureType);
            Assert.NotNull(isolatedAlc);
            Assert.NotNull(isolatedAlcAssembly);
            Assert.NotNull(isolatedAlcFixtureType);
            Assert.NotNull(isolatedAlcFixtureInstance);

            Assert.Equal(defaultAlc, isolatedAlcFixtureInstance.defaultAlc);
            Assert.Equal(defaultAlcAssembly, isolatedAlcFixtureInstance.defaultAlcAssembly);
            Assert.Equal(defaultAlcFixtureType, isolatedAlcFixtureInstance.defaultAlcFixtureType);
            Assert.Equal(isolatedAlc, isolatedAlcFixtureInstance.isolatedAlc);
            Assert.Equal(isolatedAlcAssembly, isolatedAlcFixtureInstance.isolatedAlcAssembly);
            Assert.Equal(isolatedAlcFixtureType, isolatedAlcFixtureInstance.isolatedAlcFixtureType);
            Assert.Equal(isolatedAlcFixtureInstance, isolatedAlcFixtureInstance.isolatedAlcFixtureInstance);

            Assert.Equal("Default", defaultAlc.Name);
            Assert.Equal("Isolated", isolatedAlc.Name);

            Assert.NotEqual(defaultAlc, isolatedAlc);
            Assert.NotEqual(defaultAlcAssembly, isolatedAlcAssembly);
            Assert.NotEqual(defaultAlcFixtureType, isolatedAlcFixtureType);
            Assert.NotEqual((IContextualReflectionTestFixture)this, isolatedAlcFixtureInstance);

            Assert.Equal(isolatedAlc, AssemblyLoadContext.GetLoadContext(isolatedAlcFixtureInstance.isolatedAlcAssembly));
        }
    }

    public class ContextualReflectionTest : IClassFixture<ContextualReflectionTestFixture>
    {
        IContextualReflectionTestFixture _fixture;

        public ContextualReflectionTest(ContextualReflectionTestFixture fixture)
        {
            _fixture = fixture;
            _fixture.SetPreConditions();
        }

        void AssertAssemblyEqual(Assembly expected, Assembly actual)
        {
            Assert.Equal(AssemblyLoadContext.GetLoadContext(expected), AssemblyLoadContext.GetLoadContext(actual));
            Assert.Equal(expected, actual);
        }

        [Fact]
        void FixtureIsSetupCorrectly()
        {
            _fixture.FixtureSetupAssertions();
            AssertAssemblyEqual(_fixture.defaultAlcAssembly, _fixture.GetExecutingAssembly());
            AssertAssemblyEqual(_fixture.isolatedAlcAssembly, _fixture.isolatedAlcFixtureInstance.GetExecutingAssembly());
        }

#region EnterContextualReflectionAndDispose
        [Fact]
        public void CurrentContextualReflectionContextInitialValueNull()
        {
            RemoteExecutor.Invoke(() =>
            {
                Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        void InstanceEnterContextualReflectionRaw()
        {
            AssemblyLoadContext context;

            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            context = _fixture.defaultAlc;
            {
                context.EnterContextualReflection();
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);

            context = _fixture.isolatedAlc;
            {
                context.EnterContextualReflection();
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
        }

        [Fact]
        void StaticEnterContextualReflectionRaw()
        {
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            AssemblyLoadContext context = null;
            Assembly assembly = null;
            {
                AssemblyLoadContext.EnterContextualReflection(assembly);
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);

            context = _fixture.defaultAlc;
            assembly = _fixture.defaultAlcAssembly;
            {
                AssemblyLoadContext.EnterContextualReflection(assembly);
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);

            context = _fixture.isolatedAlc;
            assembly = _fixture.isolatedAlcAssembly;
            {
                AssemblyLoadContext.EnterContextualReflection(assembly);
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
        }

        [Fact]
        void InstanceEnterContextualReflectionRawDispose()
        {
            AssemblyLoadContext context;

            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            context = _fixture.defaultAlc;
            {
                var scope = context.EnterContextualReflection();
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);

                scope.Dispose();
                Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            context = _fixture.isolatedAlc;
            {
                var scope = context.EnterContextualReflection();
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);

                scope.Dispose();
                Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
        }

        [Fact]
        void StaticEnterContextualReflectionRawDispose()
        {
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            AssemblyLoadContext context = null;
            Assembly assembly = null;
            {
                var scope = AssemblyLoadContext.EnterContextualReflection(assembly);
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);

                scope.Dispose();
                Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            context = _fixture.defaultAlc;
            assembly = _fixture.defaultAlcAssembly;
            {
                var scope = AssemblyLoadContext.EnterContextualReflection(assembly);
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);

                scope.Dispose();
                Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            context = _fixture.isolatedAlc;
            assembly = _fixture.isolatedAlcAssembly;
            {
                var scope = AssemblyLoadContext.EnterContextualReflection(assembly);
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);

                scope.Dispose();
                Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
        }

        [Fact]
        void InstanceEnterContextualReflectionUsingBasic()
        {
            AssemblyLoadContext context;

            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            context = _fixture.defaultAlc;
            using (context.EnterContextualReflection())
            {
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            context = _fixture.isolatedAlc;
            using (context.EnterContextualReflection())
            {
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
        }

        [Fact]
        void StaticEnterContextualReflectionUsingBasic()
        {
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            AssemblyLoadContext context = null;
            Assembly assembly = null;
            using (AssemblyLoadContext.EnterContextualReflection(assembly))
            {
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            context = _fixture.defaultAlc;
            assembly = _fixture.defaultAlcAssembly;
            using (AssemblyLoadContext.EnterContextualReflection(assembly))
            {
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            context = _fixture.isolatedAlc;
            assembly = _fixture.isolatedAlcAssembly;
            using (AssemblyLoadContext.EnterContextualReflection(assembly))
            {
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
        }

        [Fact]
        void InstanceEnterContextualReflectionUsingNested()
        {
            AssemblyLoadContext context;

            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            context = _fixture.defaultAlc;
            using (context.EnterContextualReflection())
            {
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
                AssemblyLoadContext nestedContext = _fixture.isolatedAlc;
                using (nestedContext.EnterContextualReflection())
                {
                    Assert.Equal(nestedContext, AssemblyLoadContext.CurrentContextualReflectionContext);
                }
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            context = _fixture.isolatedAlc;
            using (context.EnterContextualReflection())
            {
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
                AssemblyLoadContext nestedContext = _fixture.defaultAlc;
                using (nestedContext.EnterContextualReflection())
                {
                    Assert.Equal(nestedContext, AssemblyLoadContext.CurrentContextualReflectionContext);
                }
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
            }

            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
        }

        [Fact]
        void StaticEnterContextualReflectionUsingNested()
        {
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            AssemblyLoadContext context = null;
            Assembly assembly = null;
            using (AssemblyLoadContext.EnterContextualReflection(assembly))
            {
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
                AssemblyLoadContext nestedContext = _fixture.isolatedAlc;
                using (nestedContext.EnterContextualReflection())
                {
                    Assert.Equal(nestedContext, AssemblyLoadContext.CurrentContextualReflectionContext);
                }
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            context = _fixture.defaultAlc;
            assembly = _fixture.defaultAlcAssembly;
            using (AssemblyLoadContext.EnterContextualReflection(assembly))
            {
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
                AssemblyLoadContext nestedContext = _fixture.isolatedAlc;
                using (nestedContext.EnterContextualReflection())
                {
                    Assert.Equal(nestedContext, AssemblyLoadContext.CurrentContextualReflectionContext);
                }
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            context = _fixture.isolatedAlc;
            assembly = _fixture.isolatedAlcAssembly;
            using (AssemblyLoadContext.EnterContextualReflection(assembly))
            {
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
                AssemblyLoadContext nestedContext = _fixture.isolatedAlc;
                using (nestedContext.EnterContextualReflection())
                {
                    Assert.Equal(nestedContext, AssemblyLoadContext.CurrentContextualReflectionContext);
                }
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
        }

        [Fact]
        void EnterContextualReflectionUsingNestedImmutableDispose()
        {
            AssemblyLoadContext context;

            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            context = _fixture.defaultAlc;
            using (var scopeDefault = context.EnterContextualReflection())
            {
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
                AssemblyLoadContext nestedContext = _fixture.isolatedAlc;
                using (var scopeIsolated = nestedContext.EnterContextualReflection())
                {
                    Assert.Equal(nestedContext, AssemblyLoadContext.CurrentContextualReflectionContext);

                    scopeDefault.Dispose();
                    Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
                    scopeIsolated.Dispose();
                    Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
                    scopeDefault.Dispose();
                    Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
                }
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
        }

        [Fact]
        void InstanceEnterContextualReflectionUsingEarlyDispose()
        {
            AssemblyLoadContext context;

            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            context = _fixture.defaultAlc;
            using (var scope = context.EnterContextualReflection())
            {
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);

                scope.Dispose();
                Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            context = _fixture.isolatedAlc;
            using (var scope = context.EnterContextualReflection())
            {
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);

                scope.Dispose();
                Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
        }

        [Fact]
        void StaticEnterContextualReflectionUsingEarlyDispose()
        {
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            AssemblyLoadContext context = null;
            Assembly assembly = null;
            using (var scope = AssemblyLoadContext.EnterContextualReflection(assembly))
            {
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);

                scope.Dispose();
                Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            context = _fixture.defaultAlc;
            assembly = _fixture.defaultAlcAssembly;
            using (var scope = AssemblyLoadContext.EnterContextualReflection(assembly))
            {
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);

                scope.Dispose();
                Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);

            context = _fixture.isolatedAlc;
            assembly = _fixture.isolatedAlcAssembly;
            using (var scope = AssemblyLoadContext.EnterContextualReflection(assembly))
            {
                Assert.Equal(context, AssemblyLoadContext.CurrentContextualReflectionContext);

                scope.Dispose();
                Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
            }
            Assert.Null(AssemblyLoadContext.CurrentContextualReflectionContext);
        }

        [Fact]
        void EnterContextualReflectionWithMockAssemblyThrows()
        {
            AssertExtensions.Throws<ArgumentException>("activating", () => AssemblyLoadContext.EnterContextualReflection(new MockAssembly()));
        }
#endregion

#region Assembly.Load
        void AssemblyLoadTestCase(Func<Assembly> func, Assembly nullExpected, Assembly defaultExpected, Assembly isolatedExpected)
        {
            AssertAssemblyEqual(nullExpected, func());

            using (_fixture.defaultAlc.EnterContextualReflection())
            {
                AssertAssemblyEqual(defaultExpected, func());
            }

            using (_fixture.isolatedAlc.EnterContextualReflection())
            {
                AssertAssemblyEqual(isolatedExpected, func());
            }
        }

        [Fact]
        void AssemblyLoadAssemblyNameMultiLoadedAssemblyDefault()
        {
            AssemblyName name = _fixture.defaultAlcAssembly.GetName();

            AssemblyLoadTestCase(() => _fixture.AssemblyLoad(name),
                                _fixture.defaultAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);
        }

        [Fact]
        void AssemblyLoadAssemblyNameMultiLoadedAssemblyIsolated()
        {
            AssemblyName name = _fixture.defaultAlcAssembly.GetName();

            AssemblyLoadTestCase(() => _fixture.isolatedAlcFixtureInstance.AssemblyLoad(name),
                                _fixture.isolatedAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);
        }

        [Fact]
        void AssemblyLoadAssemblyNameSharedAssemblyDefault()
        {
            Assembly sharedAssembly = typeof(IContextualReflectionTestFixture).Assembly;
            AssemblyName name = sharedAssembly.GetName();

            AssemblyLoadTestCase(() => _fixture.AssemblyLoad(name),
                                sharedAssembly,
                                sharedAssembly,
                                sharedAssembly);
        }

        [Fact]
        void AssemblyLoadAssemblyNameSharedAssemblyIsolated()
        {
            Assembly sharedAssembly = typeof(IContextualReflectionTestFixture).Assembly;
            AssemblyName name = sharedAssembly.GetName();

            AssemblyLoadTestCase(() => _fixture.isolatedAlcFixtureInstance.AssemblyLoad(name),
                                sharedAssembly,
                                sharedAssembly,
                                sharedAssembly);
        }

        [Fact]
        void AssemblyLoadStringMultiLoadedAssemblyDefault()
        {
            string name = _fixture.defaultAlcAssembly.GetName().Name;

            AssemblyLoadTestCase(() => _fixture.AssemblyLoad(name),
                                _fixture.defaultAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);

            AssemblyLoadTestCase(() => _fixture.AssemblyLoadWithPartialName(name),
                                _fixture.defaultAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);
        }

        [Fact]
        void AssemblyLoadStringMultiLoadedAssemblyIsolated()
        {
            string name = _fixture.defaultAlcAssembly.GetName().Name;

            AssemblyLoadTestCase(() => _fixture.isolatedAlcFixtureInstance.AssemblyLoad(name),
                                _fixture.isolatedAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);

            AssemblyLoadTestCase(() => _fixture.isolatedAlcFixtureInstance.AssemblyLoadWithPartialName(name),
                                _fixture.isolatedAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);
        }

        [Fact]
        void AssemblyLoadStringSharedAssemblyDefault()
        {
            Assembly sharedAssembly = typeof(IContextualReflectionTestFixture).Assembly;
            string name = sharedAssembly.GetName().Name;

            AssemblyLoadTestCase(() => _fixture.AssemblyLoad(name),
                                sharedAssembly,
                                sharedAssembly,
                                sharedAssembly);
        }

        [Fact]
        void AssemblyLoadStringSharedAssemblyIsolated()
        {
            Assembly sharedAssembly = typeof(IContextualReflectionTestFixture).Assembly;
            string name = sharedAssembly.GetName().Name;

            AssemblyLoadTestCase(() => _fixture.isolatedAlcFixtureInstance.AssemblyLoad(name),
                                sharedAssembly,
                                sharedAssembly,
                                sharedAssembly);
        }
#endregion

#region Type.GetType
        void TypeGetTypeTestCase(Func<Type> func, Assembly nullExpected, Assembly defaultExpected, Assembly isolatedExpected)
        {
            AssertAssemblyEqual(nullExpected, func().Assembly);

            using (_fixture.defaultAlc.EnterContextualReflection())
            {
                AssertAssemblyEqual(defaultExpected, func().Assembly);
            }

            using (_fixture.isolatedAlc.EnterContextualReflection())
            {
                AssertAssemblyEqual(isolatedExpected, func().Assembly);
            }
        }

        [Fact]
        void TypeGetTypeStringSharedAssemblyDefault()
        {
            Assembly assembly = typeof(IContextualReflectionTestFixture).Assembly;
            string typeName = typeof(IContextualReflectionTestFixture).AssemblyQualifiedName;

            TypeGetTypeTestCase(() => _fixture.TypeGetType(typeName),
                                assembly,
                                assembly,
                                assembly);

            TypeGetTypeTestCase(() => _fixture.TypeGetType(typeName, throwOnError:false),
                                assembly,
                                assembly,
                                assembly);

            TypeGetTypeTestCase(() => _fixture.TypeGetType(typeName, throwOnError:false, ignoreCase:true),
                                assembly,
                                assembly,
                                assembly);

            TypeGetTypeTestCase(() => _fixture.TypeGetType(typeName, null, null),
                                assembly,
                                assembly,
                                assembly);

            TypeGetTypeTestCase(() => _fixture.TypeGetType(typeName, null, null, throwOnError:false),
                                assembly,
                                assembly,
                                assembly);

            TypeGetTypeTestCase(() => _fixture.TypeGetType(typeName, null, null, throwOnError:false, ignoreCase:true),
                                assembly,
                                assembly,
                                assembly);
        }

        [Fact]
        void TypeGetTypeStringSharedAssemblyIsolated()
        {
            Assembly assembly = typeof(IContextualReflectionTestFixture).Assembly;
            string typeName = typeof(IContextualReflectionTestFixture).AssemblyQualifiedName;


            TypeGetTypeTestCase(() => _fixture.isolatedAlcFixtureInstance.TypeGetType(typeName),
                                assembly,
                                assembly,
                                assembly);

            TypeGetTypeTestCase(() => _fixture.isolatedAlcFixtureInstance.TypeGetType(typeName, throwOnError:false),
                                assembly,
                                assembly,
                                assembly);

            TypeGetTypeTestCase(() => _fixture.isolatedAlcFixtureInstance.TypeGetType(typeName, throwOnError:false, ignoreCase:true),
                                assembly,
                                assembly,
                                assembly);

            TypeGetTypeTestCase(() => _fixture.isolatedAlcFixtureInstance.TypeGetType(typeName, null, null),
                                assembly,
                                assembly,
                                assembly);

            TypeGetTypeTestCase(() => _fixture.isolatedAlcFixtureInstance.TypeGetType(typeName, null, null, throwOnError:false),
                                assembly,
                                assembly,
                                assembly);

            TypeGetTypeTestCase(() => _fixture.isolatedAlcFixtureInstance.TypeGetType(typeName, null, null, throwOnError:false, ignoreCase:true),
                                assembly,
                                assembly,
                                assembly);
        }

        [Fact]
        void TypeGetTypeStringMultiLoadedAssemblyDefault()
        {
            string typeName = typeof(ContextualReflectionTestFixture).AssemblyQualifiedName;

            TypeGetTypeTestCase(() => _fixture.TypeGetType(typeName),
                                _fixture.defaultAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);

            TypeGetTypeTestCase(() => _fixture.TypeGetType(typeName, throwOnError:false),
                                _fixture.defaultAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);

            TypeGetTypeTestCase(() => _fixture.TypeGetType(typeName, throwOnError:false, ignoreCase:true),
                                _fixture.defaultAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);

            TypeGetTypeTestCase(() => _fixture.TypeGetType(typeName, null, null),
                                _fixture.defaultAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);

            TypeGetTypeTestCase(() => _fixture.TypeGetType(typeName, null, null, throwOnError:false),
                                _fixture.defaultAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);

            TypeGetTypeTestCase(() => _fixture.TypeGetType(typeName, null, null, throwOnError:false, ignoreCase:true),
                                _fixture.defaultAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);
        }

        [Fact]
        void TypeGetTypeStringMultiLoadedAssemblyIsolated()
        {
            string typeName = typeof(ContextualReflectionTestFixture).AssemblyQualifiedName;

            TypeGetTypeTestCase(() => _fixture.isolatedAlcFixtureInstance.TypeGetType(typeName),
                                _fixture.isolatedAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);

            TypeGetTypeTestCase(() => _fixture.isolatedAlcFixtureInstance.TypeGetType(typeName, throwOnError:false),
                                _fixture.isolatedAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);

            TypeGetTypeTestCase(() => _fixture.isolatedAlcFixtureInstance.TypeGetType(typeName, throwOnError:false, ignoreCase:true),
                                _fixture.isolatedAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);

            TypeGetTypeTestCase(() => _fixture.isolatedAlcFixtureInstance.TypeGetType(typeName, null, null),
                                _fixture.isolatedAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);

            TypeGetTypeTestCase(() => _fixture.isolatedAlcFixtureInstance.TypeGetType(typeName, null, null, throwOnError:false),
                                _fixture.isolatedAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);

            TypeGetTypeTestCase(() => _fixture.isolatedAlcFixtureInstance.TypeGetType(typeName, null, null, throwOnError:false, ignoreCase:true),
                                _fixture.isolatedAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);
        }

#endregion

#region Assembly.GetType
        void AssemblyGetTypeTestCase(Func<Type> func, Assembly nullExpected, Assembly defaultExpected, Assembly isolatedExpected)
        {
            AssertAssemblyEqual(nullExpected, func().GenericTypeArguments?[0].Assembly);

            using (_fixture.defaultAlc.EnterContextualReflection())
            {
                AssertAssemblyEqual(defaultExpected, func().GenericTypeArguments?[0].Assembly);
            }

            using (_fixture.isolatedAlc.EnterContextualReflection())
            {
                AssertAssemblyEqual(isolatedExpected, func().GenericTypeArguments?[0].Assembly);
            }
        }

        [Fact]
        void AssemblyGetTypeStringSharedAssemblyDefault()
        {
            Assembly assembly = typeof(IContextualReflectionTestFixture).Assembly;
            string typeNameAGenericClass = typeof(AGenericClass<>).FullName;
            string typeNameGenericArgument = typeof(IContextualReflectionTestFixture).AssemblyQualifiedName;
            string typeName = $"{typeNameAGenericClass}[[{typeNameGenericArgument}]]";

            AssemblyGetTypeTestCase(() => _fixture.AssemblyGetType(_fixture.defaultAlcAssembly, typeName),
                                    assembly,
                                    assembly,
                                    assembly);
            AssemblyGetTypeTestCase(() => _fixture.AssemblyGetType(_fixture.defaultAlcAssembly, typeName, throwOnError:false),
                                    assembly,
                                    assembly,
                                    assembly);
            AssemblyGetTypeTestCase(() => _fixture.AssemblyGetType(_fixture.defaultAlcAssembly, typeName, throwOnError:false, ignoreCase:true),
                                    assembly,
                                    assembly,
                                    assembly);
        }

        [Fact]
        void AssemblyGetTypeStringSharedAssemblyIsolated()
        {
            Assembly assembly = typeof(IContextualReflectionTestFixture).Assembly;
            string typeNameAGenericClass = typeof(AGenericClass<>).FullName;
            string typeNameGenericArgument = typeof(IContextualReflectionTestFixture).AssemblyQualifiedName;
            string typeName = $"{typeNameAGenericClass}[[{typeNameGenericArgument}]]";

            AssemblyGetTypeTestCase(() => _fixture.isolatedAlcFixtureInstance.AssemblyGetType(_fixture.defaultAlcAssembly, typeName),
                                    assembly,
                                    assembly,
                                    assembly);
            AssemblyGetTypeTestCase(() => _fixture.isolatedAlcFixtureInstance.AssemblyGetType(_fixture.defaultAlcAssembly, typeName, throwOnError:false),
                                    assembly,
                                    assembly,
                                    assembly);
            AssemblyGetTypeTestCase(() => _fixture.isolatedAlcFixtureInstance.AssemblyGetType(_fixture.defaultAlcAssembly, typeName, throwOnError:false, ignoreCase:true),
                                    assembly,
                                    assembly,
                                    assembly);
        }

        [Fact]
        void AssemblyGetTypeStringMultiLoadedAssemblyDefault()
        {
            string typeNameAGenericClass = typeof(AGenericClass<>).FullName;
            string typeNameGenericArgument = typeof(ContextualReflectionTestFixture).AssemblyQualifiedName;
            string typeName = $"{typeNameAGenericClass}[[{typeNameGenericArgument}]]";

            AssemblyGetTypeTestCase(() => _fixture.AssemblyGetType(_fixture.defaultAlcAssembly, typeName),
                                _fixture.defaultAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);

            AssemblyGetTypeTestCase(() => _fixture.AssemblyGetType(_fixture.defaultAlcAssembly, typeName, throwOnError:false),
                                _fixture.defaultAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);

            AssemblyGetTypeTestCase(() => _fixture.AssemblyGetType(_fixture.defaultAlcAssembly, typeName, throwOnError:false, ignoreCase:true),
                                _fixture.defaultAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);
        }

        [Fact]
        void AssemblyGetTypeStringMultiLoadedAssemblyIsolated()
        {
            string typeNameAGenericClass = typeof(AGenericClass<>).FullName;
            string typeNameGenericArgument = typeof(ContextualReflectionTestFixture).AssemblyQualifiedName;
            string typeName = $"{typeNameAGenericClass}[[{typeNameGenericArgument}]]";

            AssemblyGetTypeTestCase(() => _fixture.isolatedAlcFixtureInstance.AssemblyGetType(_fixture.isolatedAlcAssembly, typeName),
                                _fixture.isolatedAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);

            AssemblyGetTypeTestCase(() => _fixture.isolatedAlcFixtureInstance.AssemblyGetType(_fixture.isolatedAlcAssembly, typeName, throwOnError:false),
                                _fixture.isolatedAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);

            AssemblyGetTypeTestCase(() => _fixture.isolatedAlcFixtureInstance.AssemblyGetType(_fixture.isolatedAlcAssembly, typeName, throwOnError:false, ignoreCase:true),
                                _fixture.isolatedAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);
        }
#endregion

#region Activator.CreateInstance
        [Fact]
        void ActivatorCreateInstanceNullMultiLoadedAssemblyDefault()
        {
            string typeName = typeof(ContextualReflectionTestFixture).FullName;

            TypeGetTypeTestCase(() => _fixture.ActivatorCreateInstance(null, typeName).Unwrap().GetType(),
                                _fixture.defaultAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.defaultAlcAssembly);
        }

        [Fact]
        void ActivatorCreateInstanceNullMultiLoadedAssemblyIsolated()
        {
            string typeName = typeof(ContextualReflectionTestFixture).FullName;

            TypeGetTypeTestCase(() => _fixture.isolatedAlcFixtureInstance.ActivatorCreateInstance(null, typeName).Unwrap().GetType(),
                                _fixture.isolatedAlcAssembly,
                                _fixture.isolatedAlcAssembly,
                                _fixture.isolatedAlcAssembly);
        }

        [Fact]
        void ActivatorCreateInstanceNameMultiLoadedAssemblyDefault()
        {
            string typeName = typeof(ContextualReflectionTestFixture).FullName;
            string assemblyName = _fixture.defaultAlcAssembly.GetName().Name;

            TypeGetTypeTestCase(() => _fixture.ActivatorCreateInstance(assemblyName, typeName).Unwrap().GetType(),
                                _fixture.defaultAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);
        }

        [Fact]
        void ActivatorCreateInstanceNameMultiLoadedAssemblyIsolated()
        {
            string typeName = typeof(ContextualReflectionTestFixture).FullName;
            string assemblyName = _fixture.defaultAlcAssembly.GetName().Name;

            TypeGetTypeTestCase(() => _fixture.isolatedAlcFixtureInstance.ActivatorCreateInstance(assemblyName, typeName).Unwrap().GetType(),
                                _fixture.isolatedAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);
        }

        [Fact]
        void ActivatorCreateInstanceNullGenericMultiLoadedAssemblyDefault()
        {
            string typeNameAGenericClass = typeof(AGenericClass<>).FullName;
            string typeNameGenericArgument = typeof(ContextualReflectionTestFixture).AssemblyQualifiedName;
            string typeName = $"{typeNameAGenericClass}[[{typeNameGenericArgument}]]";

            AssemblyGetTypeTestCase(() => _fixture.ActivatorCreateInstance(null, typeName).Unwrap().GetType(),
                                _fixture.defaultAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);
        }

        [Fact]
        void ActivatorCreateInstanceNullGenericMultiLoadedAssemblyIsolated()
        {
            string typeNameAGenericClass = typeof(AGenericClass<>).FullName;
            string typeNameGenericArgument = typeof(ContextualReflectionTestFixture).AssemblyQualifiedName;
            string typeName = $"{typeNameAGenericClass}[[{typeNameGenericArgument}]]";

            AssemblyGetTypeTestCase(() => _fixture.isolatedAlcFixtureInstance.ActivatorCreateInstance(null, typeName).Unwrap().GetType(),
                                _fixture.isolatedAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);
        }

        [Fact]
        void ActivatorCreateInstanceNameGenericMultiLoadedAssemblyDefault()
        {
            string typeNameAGenericClass = typeof(AGenericClass<>).FullName;
            string typeNameGenericArgument = typeof(ContextualReflectionTestFixture).AssemblyQualifiedName;
            string typeName = $"{typeNameAGenericClass}[[{typeNameGenericArgument}]]";
            string assemblyName = _fixture.defaultAlcAssembly.GetName().Name;

            AssemblyGetTypeTestCase(() => _fixture.ActivatorCreateInstance(assemblyName, typeName).Unwrap().GetType(),
                                _fixture.defaultAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);
        }

        [Fact]
        void ActivatorCreateInstanceNameGenericMultiLoadedAssemblyIsolated()
        {
            string typeNameAGenericClass = typeof(AGenericClass<>).FullName;
            string typeNameGenericArgument = typeof(ContextualReflectionTestFixture).AssemblyQualifiedName;
            string typeName = $"{typeNameAGenericClass}[[{typeNameGenericArgument}]]";
            string assemblyName = _fixture.defaultAlcAssembly.GetName().Name;

            AssemblyGetTypeTestCase(() => _fixture.isolatedAlcFixtureInstance.ActivatorCreateInstance(assemblyName, typeName).Unwrap().GetType(),
                                _fixture.isolatedAlcAssembly,
                                _fixture.defaultAlcAssembly,
                                _fixture.isolatedAlcAssembly);
        }
#endregion

    }
}

