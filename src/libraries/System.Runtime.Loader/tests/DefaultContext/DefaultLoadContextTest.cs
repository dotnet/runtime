// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace System.Runtime.Loader.Tests
{
    public class SecondaryLoadContext : AssemblyLoadContext
    {
        protected override Assembly Load(AssemblyName assemblyName)
        {
            return null;
        }
    }

    public class FallbackLoadContext : AssemblyLoadContext
    {
        protected override Assembly Load(AssemblyName assemblyName)
        {
            // Return null since we expect the DefaultContext to be used to bind
            // the assembly bind request.
            return null;
        }
    }

    public class OverrideDefaultLoadContext : AssemblyLoadContext
    {
        public bool LoadedFromContext { get; set; } = false;

        protected override Assembly Load(AssemblyName assemblyName)
        {
            // Override the assembly that was loaded in DefaultContext.
            string assemblyPath = Path.Combine(Path.GetDirectoryName(typeof(string).Assembly.Location), assemblyName.Name + ".dll");
            Assembly assembly = LoadFromAssemblyPath(assemblyPath);
            LoadedFromContext = true;
            return assembly;
        }
    }

    [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "AssemblyLoadContext not supported on .Net Native")]
    public class DefaultLoadContextTests
    {
        private const string TestAssemblyName = "System.Runtime.Loader.Noop.Assembly";
        private string _assemblyPath;
        private string _defaultLoadDirectory;

        // Since the first non-Null returning callback should stop Resolving event processing,
        // this counter is used to assert the same.
        private int _numNonNullResolutions = 0;

        public DefaultLoadContextTests()
        {
            _defaultLoadDirectory = GetDefaultAssemblyLoadDirectory();
            _assemblyPath = Path.Combine(_defaultLoadDirectory, "System.Runtime.Loader.Noop.Assembly_test.dll");
        }

        private static string GetDefaultAssemblyLoadDirectory()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        private Assembly ResolveAssembly(AssemblyLoadContext sender, AssemblyName assembly)
        {
            string resolvedAssemblyPath = Path.Combine(_defaultLoadDirectory, assembly.Name + "_test.dll");
            _numNonNullResolutions++;

            return sender.LoadFromAssemblyPath(resolvedAssemblyPath);
        }

        private Assembly ResolveAssemblyAgain(AssemblyLoadContext sender, AssemblyName assembly)
        {
            return ResolveAssembly(sender, assembly);
        }

        private Assembly ResolveNullAssembly(AssemblyLoadContext sender, AssemblyName assembly)
        {
            return null;
        }

        [Fact]
        public void LoadInDefaultContext()
        {
            // This will attempt to load an assembly, by path, in the Default Load context via the Resolving event
            var assemblyName = new AssemblyName(TestAssemblyName);

            // By default, the assembly should not be found in DefaultContext at all
            Assert.Throws(typeof(FileNotFoundException), () => Assembly.Load(assemblyName));

            // Create a secondary load context and wireup its resolving event
            SecondaryLoadContext slc = new SecondaryLoadContext();
            slc.Resolving += ResolveAssembly;
            
            // Attempt to load the assembly in secondary load context
            var slcLoadedAssembly = slc.LoadFromAssemblyName(assemblyName);
            
            // We should have successfully loaded the assembly in secondary load context.
            Assert.NotNull(slcLoadedAssembly);

            // And make sure the simple name matches
            Assert.Equal(TestAssemblyName, slcLoadedAssembly.GetName().Name);

            // We should have only invoked non-Null returning handler once
            Assert.Equal(1, _numNonNullResolutions);

            slc.Resolving -= ResolveAssembly;

            // Reset the non-Null resolution counter
            _numNonNullResolutions = 0;

            // Now, wireup the Resolving event of default context to locate the assembly via multiple handlers
            AssemblyLoadContext.Default.Resolving += ResolveNullAssembly;
            AssemblyLoadContext.Default.Resolving += ResolveAssembly;
            AssemblyLoadContext.Default.Resolving += ResolveAssemblyAgain;
            
            // This will invoke the resolution via VM requiring to bind using the TPA binder
            var assemblyExpectedFromLoad = Assembly.Load(assemblyName);

            // We should have successfully loaded the assembly in default context.
            Assert.NotNull(assemblyExpectedFromLoad);

            // We should have only invoked non-Null returning handler once
            Assert.Equal(1, _numNonNullResolutions);

            // And make sure the simple name matches
            Assert.Equal(TestAssemblyName, assemblyExpectedFromLoad.GetName().Name);

            // The assembly loaded in DefaultContext should have a different reference from the one in secondary load context
            Assert.NotEqual(slcLoadedAssembly, assemblyExpectedFromLoad);

            // Reset the non-Null resolution counter
            _numNonNullResolutions = 0;

            // Since the assembly is already loaded in TPA Binder, we will get that back without invoking any Resolving event handlers
            var assemblyExpected = AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
            Assert.Equal(0, _numNonNullResolutions);

            // We should have successfully loaded the assembly in default context.
            Assert.NotNull(assemblyExpected);

            // What we got via Assembly.Load and LoadFromAssemblyName should be the same
            Assert.Equal(assemblyExpected, assemblyExpectedFromLoad);

            // And make sure the simple name matches
            Assert.Equal(assemblyExpected.GetName().Name, TestAssemblyName);

            // Unwire the Resolving event.
            AssemblyLoadContext.Default.Resolving -= ResolveAssemblyAgain;
            AssemblyLoadContext.Default.Resolving -= ResolveAssembly;
            AssemblyLoadContext.Default.Resolving -= ResolveNullAssembly;

            // Unwire the Resolving event and attempt to load the assembly again. This time
            // it should be found in the Default Load Context.
            var assemblyLoaded = Assembly.Load(new AssemblyName(TestAssemblyName));

            // We should have successfully found the assembly in default context.
            Assert.NotNull(assemblyLoaded);

            // Ensure that we got the same assembly reference back.
            Assert.Equal(assemblyExpected, assemblyLoaded);

            // Run tests for binding from DefaultContext when custom load context does not have TPA overrides.
            DefaultContextFallback();

            // Run tests for overriding DefaultContext when custom load context has TPA overrides.
            DefaultContextOverrideTPA();
        }

        [Fact]
        public static void LoadNonExistentInDefaultContext()
        {
            // Now, try to load an assembly that does not exist
            Assert.Throws(typeof(FileNotFoundException), 
                () => AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("System.Runtime.Loader.NonExistent.Assembly")));
        }

        public void DefaultContextFallback()
        {
            var lcDefault = AssemblyLoadContext.Default;
            
            // Load the assembly in custom load context
            FallbackLoadContext flc = new FallbackLoadContext();
            var asmTargetAsm = flc.LoadFromAssemblyPath(_assemblyPath);
            var loadedContext = AssemblyLoadContext.GetLoadContext(asmTargetAsm);

            // LoadContext of the assembly should be the custom context and not DefaultContext
            Assert.NotEqual(lcDefault, flc);
            Assert.Equal(flc, loadedContext);
            
            // Get reference to the helper method that will load assemblies (actually, resolve them)
            // from DefaultContext
            Type type = asmTargetAsm.GetType("System.Runtime.Loader.Tests.TestClass");
            var method = System.Reflection.TypeExtensions.GetMethod(type, "LoadFromDefaultContext");
            
            // Load System.Runtime - since this is on TPA, it should get resolved from DefaultContext
            // since FallbackLoadContext does not override the Load method to specify its location.
            var assemblyName = "System.Runtime, Version=4.0.0.0";
            Assembly asmLoaded = (Assembly)method.Invoke(null, new object[] {assemblyName});
            loadedContext = AssemblyLoadContext.GetLoadContext(asmLoaded);

            // Confirm assembly Loaded from DefaultContext
            Assert.Equal(lcDefault, loadedContext);
            Assert.NotEqual(flc, loadedContext);

            // Now, do the same from an assembly that we explicitly had loaded in DefaultContext
            // in the caller of this method. We should get it from FallbackLoadContext since we
            // explicitly loaded it there as well.
            assemblyName = TestAssemblyName;
            Assembly asmLoaded2 = (Assembly)method.Invoke(null, new object[] {assemblyName});
            loadedContext = AssemblyLoadContext.GetLoadContext(asmLoaded2);

            Assert.NotEqual(lcDefault, loadedContext);
            Assert.Equal(flc, loadedContext);

            // Attempt to bind an assembly that has not been loaded in DefaultContext and is not 
            // present on TPA as well. Such an assembly will not trigger a load since we only consult 
            // the DefaultContext cache (including assemblies on TPA list) in an attempt to bind.
            assemblyName = "System.Runtime.Loader.Noop.Assembly.NonExistent";
            Exception ex = null;
            try
            {
                method.Invoke(null, new object[] {assemblyName});
            }
            catch(TargetInvocationException tie)
            {
                ex = tie.InnerException;
            }

            Assert.Equal(typeof(FileNotFoundException), ex.GetType());
        }

        public void DefaultContextOverrideTPA()
        {
            var lcDefault = AssemblyLoadContext.Default;
            
            // Load the assembly in custom load context
            OverrideDefaultLoadContext olc = new OverrideDefaultLoadContext();
            var asmTargetAsm = olc.LoadFromAssemblyPath(_assemblyPath);
            var loadedContext = AssemblyLoadContext.GetLoadContext(asmTargetAsm);

            // LoadContext of the assembly should be the custom context and not DefaultContext
            Assert.NotEqual(lcDefault, olc);
            Assert.Equal(olc, loadedContext);
            
            // Get reference to the helper method that will load assemblies (actually, resolve them)
            // from DefaultContext
            Type type = asmTargetAsm.GetType("System.Runtime.Loader.Tests.TestClass", true);
            var method = System.Reflection.TypeExtensions.GetMethod(type, "LoadFromDefaultContext");
            
            // Load System.Runtime - since this is on TPA, it should get resolved from our custom load context
            // since the Load method has been implemented to override TPA assemblies.
            var assemblyName = "System.Runtime, Version=4.0.0.0";
            olc.LoadedFromContext = false;
            Assembly asmLoaded = (Assembly)method.Invoke(null, new object[] {assemblyName});
            loadedContext = AssemblyLoadContext.GetLoadContext(asmLoaded);

            // Confirm assembly did not load from DefaultContext
            Assert.NotEqual(lcDefault, loadedContext);
            Assert.Equal(olc, loadedContext);
            Assert.Equal(true, olc.LoadedFromContext);

            // Now, do the same for an assembly that we explicitly had loaded in DefaultContext
            // in the caller of this method and ALSO loaded in the current load context. We should get it from our LoadContext,
            // without invoking the Load override, since it is already loaded.
            assemblyName = TestAssemblyName;
            olc.LoadedFromContext = false;
            Assembly asmLoaded2 = (Assembly)method.Invoke(null, new object[] {assemblyName});
            loadedContext = AssemblyLoadContext.GetLoadContext(asmLoaded2);

            // Confirm assembly loaded from the intended LoadContext
            Assert.NotEqual(lcDefault, loadedContext);
            Assert.Equal(olc, loadedContext);
            Assert.Equal(false, olc.LoadedFromContext);
        }
    }
}
