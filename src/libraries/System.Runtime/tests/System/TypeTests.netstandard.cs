// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Xunit;

namespace System.Tests
{
    public class TypeTestsExtended : RemoteExecutorTestBase
    {
        public class ContextBoundClass : ContextBoundObject
        {
            public string Value = "The Value property.";
        }

        static string sourceTestAssemblyPath = Path.Combine(Environment.CurrentDirectory, "TestLoadAssembly.dll");
        static string destTestAssemblyPath = Path.Combine(Environment.CurrentDirectory, "TestLoadAssembly", "TestLoadAssembly.dll");
        static string testtype = "System.Collections.Generic.Dictionary`2[[Program, Foo], [Program, Foo]]";
        static TypeTestsExtended()
        {
            // Move TestLoadAssembly.dll to subfolder TestLoadAssembly
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destTestAssemblyPath));
                File.Move(sourceTestAssemblyPath, destTestAssemblyPath);
            }
            catch (System.Exception) { }
            finally
            {
                File.Delete(sourceTestAssemblyPath);
            }
        }

        private static Func<AssemblyName, Assembly> assemblyloader = (aName) => aName.Name == "TestLoadAssembly" ?
                           Assembly.LoadFrom(@".\TestLoadAssembly\TestLoadAssembly.dll") :
                           null;
        private static Func<Assembly, String, Boolean, Type> typeloader = (assem, name, ignore) => assem == null ?
                             Type.GetType(name, false, ignore) :
                                 assem.GetType(name, false, ignore);
        [Fact]
        public static void GetTypeByName()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            RemoteInvoke(() =>
               {
                   string test1 = testtype;
                   Type t1 = Type.GetType(test1,
                             (aName) => aName.Name == "Foo" ?
                                   Assembly.LoadFrom(destTestAssemblyPath) : null,
                             typeloader,
                             true
                     );

                   Assert.NotNull(t1);

                   string test2 = "System.Collections.Generic.Dictionary`2[[Program, TestLoadAssembly], [Program, TestLoadAssembly]]";
                   Type t2 = Type.GetType(test2, assemblyloader, typeloader, true);

                   Assert.NotNull(t2);
                   Assert.Equal(t1, t2);

                   return SuccessExitCode;
               }, options).Dispose();
        }

        [Fact]
        public static void GetTypeByNameTypeloadFailure()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            RemoteInvoke(() =>
               {
                   string test1 = testtype;
                   //Loading from the wrong path
                   Assert.Throws<System.IO.FileNotFoundException>(() =>
                   Type.GetType(test1,
                     (aName) => aName.Name == "Foo" ?
                         Assembly.LoadFrom(@".\TestLoadAssembly.dll") : null,
                     typeloader,
                     true
                  ));

                   //Type specified 'Program2' does not exst
                   string test2 = "System.Collections.Generic.Dictionary`2[[Program, TestLoadAssembly], [Program2, TestLoadAssembly]]";
                   Assert.Throws<TypeLoadException>(() => Type.GetType(test2, assemblyloader, typeloader, true));

                   //Api does not throw
                   Type t1 = Type.GetType(test2,
                                          assemblyloader,
                                          typeloader,
                                          false      //no throw
                    );

                   Assert.Null(t1);

                   return SuccessExitCode;
               }, options).Dispose();
        }

        [Fact]
        public static void GetTypeByNameCaseSensitiveTypeloadFailure()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            RemoteInvoke(() =>
               {
                   //Type load failure due to case sensitive search of type Ptogram
                   string test3 = "System.Collections.Generic.Dictionary`2[[Program, TestLoadAssembly], [program, TestLoadAssembly]]";
                   Assert.Throws<TypeLoadException>(() =>
                                Type.GetType(test3,
                                            assemblyloader,
                                            typeloader,
                                            true,
                                            false     //case sensitive
                   ));

                   //non throwing version
                   Type t2 = Type.GetType(test3,
                                          assemblyloader,
                                          typeloader,
                                          false,  //no throw
                                          false
                  );

                   Assert.Null(t2);

                   return SuccessExitCode;
               }, options).Dispose();
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void IsContextful()
        {
            Assert.True(!typeof(TypeTestsExtended).IsContextful);
            Assert.True(!typeof(ContextBoundClass).IsContextful);
        }
    }
}
