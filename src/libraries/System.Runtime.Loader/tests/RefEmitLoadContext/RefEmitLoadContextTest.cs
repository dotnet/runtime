// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace System.Runtime.Loader.Tests
{
    public class RefEmitLoadContext : AssemblyLoadContext
    {
        protected override Assembly Load(AssemblyName assemblyName)
        {
            // We implement Load override since the assembly being loaded (this assembly) is already loaded in the DefaultContext
            // and we want to load a different copy of it in the custom load context.
            var loadPath = Path.Combine(RefEmitLoadContextTests.s_loadFromPath, assemblyName.Name+".dll");
            Assembly loadedAssembly = null;
            if (File.Exists(loadPath))
            {
                loadedAssembly = LoadFromAssemblyPath(loadPath);
            }

            return loadedAssembly;
        }
    }

    public class RefEmitLoadContextTests
    {
        public static string s_loadFromPath = null;

        private static void Init()
        {
            var assemblyFilename = "System.Runtime.Loader.Noop.Assembly.dll";
            
            // Form the dynamic path that would not collide if another instance of this test is running.
            s_loadFromPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            
            // Create the folder
            Directory.CreateDirectory(s_loadFromPath);
            
            var targetPath = Path.Combine(s_loadFromPath, assemblyFilename);

            // Rename the file local to the test folder.
            var sourcePath = Path.Combine(Directory.GetCurrentDirectory(),assemblyFilename);

            // Finally, copy the file to the temp location from where we expect to load it
            File.Copy(sourcePath, targetPath); 

            // Copy the current assembly to the target location as well since we will load it in the custom load context via the
            // RefEmitted assembly.
            var asmCurrentAssembly = typeof(RefEmitLoadContext).GetTypeInfo().Assembly.GetName();
            var pathCurrentAssembly = typeof(RefEmitLoadContext).GetTypeInfo().Assembly.Location;

            targetPath = Path.Combine(s_loadFromPath, asmCurrentAssembly.Name+".dll");

            File.Copy(pathCurrentAssembly, targetPath);
        }

        private static void DeleteDirectory()
        {
            try { Directory.Delete(s_loadFromPath, recursive: true); }
            catch { } 
        }

        [Fact]
        public static void LoadRefEmitAssembly()
        {
            Init();

            // Scenario 1 - Generate a non-collectible dynamic assembly that triggers load of a static assembly
            RefEmitLoadContext refEmitLCRun = new RefEmitLoadContext();
            LoadRefEmitAssemblyInLoadContext(refEmitLCRun, AssemblyBuilderAccess.Run);

            // Scenario 2 - Generate a collectible dynamic assembly that triggers load of a static assembly
            RefEmitLoadContext refEmitLCRunAndCollect = new RefEmitLoadContext();
            LoadRefEmitAssemblyInLoadContext(refEmitLCRunAndCollect, AssemblyBuilderAccess.RunAndCollect);
            DeleteDirectory();
        }

        public static void LoadRefEmitAssemblyInLoadContext(AssemblyLoadContext loadContext, AssemblyBuilderAccess builderType)
        {
            // Load this assembly in custom LoadContext
            var assemblyNameStr = "System.Runtime.Loader.Noop.Assembly.dll";
            
            // Load the assembly in the specified load context
            var asmTargetAsm = loadContext.LoadFromAssemblyPath(Path.Combine(s_loadFromPath, assemblyNameStr));
            var creatorLoadContext = AssemblyLoadContext.GetLoadContext(asmTargetAsm);
            Assert.Equal(loadContext, creatorLoadContext);

            // Get reference to the helper method that will RefEmit an assembly and return reference to it.
            Type type = asmTargetAsm.GetType("System.Runtime.Loader.Tests.TestClass");
            var method = System.Reflection.TypeExtensions.GetMethod(type, "GetRefEmitAssembly");
            
            // Use the helper to generate an assembly
            var assemblyNameRefEmit = "RefEmitTestAssembly";
            var asmRefEmitLoaded = (Assembly)method.Invoke(null, new object[] {assemblyNameRefEmit, builderType});
            Assert.NotNull(asmRefEmitLoaded);

            // Assert that Dynamically emitted assemblies load context is the same as that of the assembly
            // that created them.
            var loadContextRefEmitAssembly = AssemblyLoadContext.GetLoadContext(asmRefEmitLoaded);
            Assert.Equal(creatorLoadContext, loadContextRefEmitAssembly);

            // Invoke the method that will trigger a static load in the dynamically generated assembly.
            Type typeRefEmit = asmRefEmitLoaded.GetType("RefEmitTestType");
            method = System.Reflection.TypeExtensions.GetMethod(typeRefEmit, "LoadStaticAssembly");
            Assert.NotNull(method);

            // Invoke the method to load the current assembly from the temp location
            var assemblyStaticToLoad = typeof(RefEmitLoadContext).GetTypeInfo().Assembly.GetName().Name;
            var asmRefEmitLoadedStatic = method.Invoke(null, new object[] {assemblyStaticToLoad});
            Assert.NotNull(asmRefEmitLoadedStatic);

            // Load context of the statically loaded assembly is the custom load context in which dynamic assembly was created
            Assert.Equal(loadContextRefEmitAssembly, AssemblyLoadContext.GetLoadContext((Assembly)asmRefEmitLoadedStatic));

            // Enumerate the assemblies in the AppDomain and confirm that the Dynamically generated assembly is present.
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            bool fDynamicAssemblyFound = false;
            foreach (Assembly asm in loadedAssemblies)
            {
                if (asmRefEmitLoaded == asm)
                {
                    if (asm.FullName == asmRefEmitLoaded.FullName)    
                    {
                        fDynamicAssemblyFound = true;
                        break;
                    }
                }
            }

            Assert.Equal(true, fDynamicAssemblyFound);
        }
    }
}
