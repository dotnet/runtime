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

    public class DefaultLoadContextTests
    {
        private static string s_loadFromPath = null;

        // Since the first non-Null returning callback should stop Resolving event processing,
        // this counter is used to assert the same.
        private static int s_NumNonNullResolutions = 0;

        private static Assembly ResolveAssembly(AssemblyLoadContext sender, AssemblyName assembly)
        {
            string assemblyFilename = assembly.Name + ".dll";
            s_NumNonNullResolutions++;

            return sender.LoadFromAssemblyPath(Path.Combine(s_loadFromPath, assemblyFilename));
        }

        private static Assembly ResolveAssemblyAgain(AssemblyLoadContext sender, AssemblyName assembly)
        {
            string assemblyFilename = assembly.Name + ".dll";
            s_NumNonNullResolutions++;

            return sender.LoadFromAssemblyPath(Path.Combine(s_loadFromPath, assemblyFilename));
        }

        private static Assembly ResolveNullAssembly(AssemblyLoadContext sender, AssemblyName assembly)
        {
            return null;
        }

        private static void Init()
        {
            // Delete the assembly from the temp location if it exists.
            var assemblyFilename = "System.Runtime.Loader.Noop.Assembly.dll";
            s_NumNonNullResolutions = 0;

            // Form the dynamic path that would not collide if another instance of this test is running.
            s_loadFromPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            
            // Create the folder
            Directory.CreateDirectory(s_loadFromPath);
            
            var targetPath = Path.Combine(s_loadFromPath, assemblyFilename);

            // Rename the file local to the test folder.
            var sourcePath = Path.Combine(Directory.GetCurrentDirectory(),assemblyFilename);
            var targetRenamedPath = Path.Combine(Directory.GetCurrentDirectory(), "System.Runtime.Loader.Noop.Assembly_test.dll");
            if (File.Exists(sourcePath))
            {
                if (File.Exists(targetRenamedPath))
                {
                    File.Delete(targetRenamedPath);
                }

                File.Move(sourcePath, targetRenamedPath);
            }

            // Finally, copy the file to the temp location from where we expect to load it
            File.Copy(targetRenamedPath, targetPath); 
        }

        [Fact]
        public static void LoadInDefaultContext()
        {
            Init();

            // This will attempt to load an assembly, by path, in the Default Load context via the Resolving event
            var assemblyNameStr = "System.Runtime.Loader.Noop.Assembly";
            var assemblyName = new AssemblyName(assemblyNameStr);

            // By default, the assembly should not be found in DefaultContext at all
            Assert.Throws(typeof(FileNotFoundException), () => Assembly.Load(assemblyName));

            // Create a secondary load context and wireup its resolving event
            SecondaryLoadContext slc = new SecondaryLoadContext();
            slc.Resolving += ResolveAssembly;
            
            // Attempt to load the assembly in secondary load context
            var slcLoadedAssembly = slc.LoadFromAssemblyName(assemblyName);
            
            // We should have successfully loaded the assembly in default context.
            Assert.NotNull(slcLoadedAssembly);

            // And make sure the simple name matches
            Assert.Equal(assemblyNameStr, slcLoadedAssembly.GetName().Name);

            // We should have only invoked non-Null returning handler once
            Assert.Equal(1, s_NumNonNullResolutions);

            slc.Resolving -= ResolveAssembly;

            // Reset the non-Null resolution counter
            s_NumNonNullResolutions = 0;

            // Now, wireup the Resolving event of default context to locate the assembly via multiple handlers
            AssemblyLoadContext.Default.Resolving += ResolveNullAssembly;
            AssemblyLoadContext.Default.Resolving += ResolveAssembly;
            AssemblyLoadContext.Default.Resolving += ResolveAssemblyAgain;
            
            // This will invoke the resolution via VM requiring to bind using the TPA binder
            var assemblyExpectedFromLoad = Assembly.Load(assemblyName);

            // We should have successfully loaded the assembly in default context.
            Assert.NotNull(assemblyExpectedFromLoad);

            // We should have only invoked non-Null returning handler once
            Assert.Equal(1, s_NumNonNullResolutions);

            // And make sure the simple name matches
            Assert.Equal(assemblyNameStr, assemblyExpectedFromLoad.GetName().Name);

            // The assembly loaded in DefaultContext should have a different reference from the one in secondary load context
            Assert.NotEqual(slcLoadedAssembly, assemblyExpectedFromLoad);

            // This will resolve the assembly via event invocation.
            var assemblyExpected = AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
            
            // We should have successfully loaded the assembly in default context.
            Assert.NotNull(assemblyExpected);

            // What we got via Assembly.Load and LoadFromAssemblyName should be the same
            Assert.Equal(assemblyExpected, assemblyExpectedFromLoad);

            // And make sure the simple name matches
            Assert.Equal(assemblyExpected.GetName().Name, assemblyNameStr);

            // Unwire the Resolving event.
            AssemblyLoadContext.Default.Resolving -= ResolveAssemblyAgain;
            AssemblyLoadContext.Default.Resolving -= ResolveAssembly;
            AssemblyLoadContext.Default.Resolving -= ResolveNullAssembly;

            // Unwire the Resolving event and attempt to load the assembly again. This time
            // it should be found in the Default Load Context.
            var assemblyLoaded = Assembly.Load(new AssemblyName(assemblyNameStr));

            // We should have successfully found the assembly in default context.
            Assert.NotNull(assemblyLoaded);

            // Ensure that we got the same assembly reference back.
            Assert.Equal(assemblyExpected, assemblyLoaded);
        }

        [Fact]
        public static void LoadNonExistentInDefaultContext()
        {
            // Now, try to load an assembly that does not exist
            Assert.Throws(typeof(FileNotFoundException), 
                () => AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("System.Runtime.Loader.NonExistent.Assembly")));
        }
    }
}
