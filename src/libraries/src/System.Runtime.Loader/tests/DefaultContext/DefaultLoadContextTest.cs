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
    public class DefaultLoadContextTests
    {
        private static Assembly ResolveAssembly(AssemblyLoadContext sender, AssemblyName assembly)
        {
            string assemblyFilename = assembly.Name + ".dll";
            
            return sender.LoadFromAssemblyPath(Path.Combine(Path.GetTempPath(), assemblyFilename));
        }

        private static void Init()
        {
            // Delete the assembly from the temp location if it exists.
            var assemblyFilename = "System.Runtime.Loader.Noop.Assembly.dll";
            var targetPath = Path.Combine(Path.GetTempPath(), assemblyFilename);
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

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
           var assemblyName = "System.Runtime.Loader.Noop.Assembly";

            AssemblyLoadContext.Default.Resolving += ResolveAssembly;
            var assemblyExpected = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(assemblyName));
            
            // We should have successfully loaded the assembly in default context.
            Assert.NotNull(assemblyExpected);

            // And make sure the simplename matches
            Assert.Equal(assemblyExpected.GetName().Name, assemblyName);

            // Unwire the Resolving event.
            AssemblyLoadContext.Default.Resolving -= ResolveAssembly;

            // Unwire the Resolving event and attempt to load the assembly again. This time
            // it should be found in the Default Load Context.
            var assemblyLoaded = Assembly.Load(new AssemblyName(assemblyName));

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
