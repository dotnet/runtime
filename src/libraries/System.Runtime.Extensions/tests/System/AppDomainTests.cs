// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.ExceptionServices;
using Xunit;
using Xunit.NetCore.Extensions;

namespace System.Tests
{
    public class AppDomainTests : RemoteExecutorTestBase
    {
        public AppDomainTests()
        {
            string sourceTestAssemblyPath = Path.Combine(Environment.CurrentDirectory, "AssemblyResolveTests.dll");
            string destTestAssemblyPath = Path.Combine(Environment.CurrentDirectory, "AssemblyResolveTests", "AssemblyResolveTests.dll");
            if (File.Exists(sourceTestAssemblyPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destTestAssemblyPath));
                File.Copy(sourceTestAssemblyPath, destTestAssemblyPath, true);
                File.Delete(sourceTestAssemblyPath);
            }

            sourceTestAssemblyPath = Path.Combine(Environment.CurrentDirectory, "TestAppOutsideOfTPA.exe");
            destTestAssemblyPath = Path.Combine(Environment.CurrentDirectory, "TestAppOutsideOfTPA", "TestAppOutsideOfTPA.exe");
            if (File.Exists(sourceTestAssemblyPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destTestAssemblyPath));
                File.Copy(sourceTestAssemblyPath, destTestAssemblyPath, true);
                File.Delete(sourceTestAssemblyPath);
            }
        }

        [Fact]
        public void CurrentDomain_Not_Null()
        {
            Assert.NotNull(AppDomain.CurrentDomain);
        }

        [Fact]
        public void CurrentDomain_Idempotent()
        {
            Assert.Equal(AppDomain.CurrentDomain, AppDomain.CurrentDomain);
        }

        [Fact]
        public void BaseDirectory_Same_As_AppContext()
        {
            Assert.Equal(AppDomain.CurrentDomain.BaseDirectory, AppContext.BaseDirectory);
        } 

        [Fact]
        public void RelativeSearchPath_Is_Null()
        {
            Assert.Null(AppDomain.CurrentDomain.RelativeSearchPath);
        } 


        [Fact]
        public void UnhandledException_Add_Remove()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
            AppDomain.CurrentDomain.UnhandledException -= new UnhandledExceptionEventHandler(MyHandler);
        }

        [Fact]
        public void UnhandledException_NotCalled_When_Handled()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(NotExpectedToBeCalledHandler);
            try {
                throw new Exception();
            }
            catch
            {
            }
            AppDomain.CurrentDomain.UnhandledException -= new UnhandledExceptionEventHandler(NotExpectedToBeCalledHandler);
        }

        [ActiveIssue(12716)]
        [PlatformSpecific(~TestPlatforms.OSX)] // Unhandled exception on a separate process causes xunit to crash on osx
        [Fact]
        public void UnhandledException_Called()
        {
            System.IO.File.Delete("success.txt");
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.CheckExitCode = false;
            RemoteInvoke(() =>
            {
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
                throw new Exception("****This Unhandled Exception is Expected****");
#pragma warning disable 0162
                return SuccessExitCode;
#pragma warning restore 0162
            }, options).Dispose();

            Assert.True(System.IO.File.Exists("success.txt"));
        }

        static void NotExpectedToBeCalledHandler(object sender, UnhandledExceptionEventArgs args) 
        {
            Assert.True(false, "UnhandledException handler not expected to be called");
        }

        static void MyHandler(object sender, UnhandledExceptionEventArgs args) 
        {
            System.IO.File.Create("success.txt");
        }

        [Fact]
        public void DynamicDirectory_Null()
        {
            Assert.Null(AppDomain.CurrentDomain.DynamicDirectory);
        }

        [Fact]
        public void FriendlyName()
        {
            string s = AppDomain.CurrentDomain.FriendlyName;
            Assert.NotNull(s);
            string expected = Assembly.GetEntryAssembly().GetName().Name;
            Assert.Equal(expected, s);
        }        

        [Fact]
        public void Id()
        {
            Assert.Equal(1, AppDomain.CurrentDomain.Id);
        }        

        [Fact]
        public void IsFullyTrusted()
        {
            Assert.True(AppDomain.CurrentDomain.IsFullyTrusted);
        }        

        [Fact]
        public void IsHomogenous()
        {
            Assert.True(AppDomain.CurrentDomain.IsHomogenous);
        }        

        [Fact]
        public void FirstChanceException_Add_Remove()
        {
            EventHandler<FirstChanceExceptionEventArgs> handler = (sender, e) =>
            {
            };
            AppDomain.CurrentDomain.FirstChanceException += handler;
            AppDomain.CurrentDomain.FirstChanceException -= handler;
        }

        [Fact]
        public void FirstChanceException_Called()
        {
            bool flag = false;
            EventHandler<FirstChanceExceptionEventArgs> handler = (sender, e) =>
            {
                Exception ex = (Exception) e.Exception;
                if (ex is FirstChanceTestException)
                {
                    flag = !flag;
                }
            };
            AppDomain.CurrentDomain.FirstChanceException += handler;
            try {
                throw new FirstChanceTestException("testing");
            }
            catch
            {
            }
            AppDomain.CurrentDomain.FirstChanceException -= handler;
            Assert.True(flag, "FirstChanceHandler not called");
        }

        class FirstChanceTestException : Exception
        {
            public FirstChanceTestException(string message) : base(message) 
            { }
        }

        [Fact]
        public void ProcessExit_Add_Remove()
        {
            EventHandler handler = (sender, e) =>
            {
            };
            AppDomain.CurrentDomain.ProcessExit += handler;
            AppDomain.CurrentDomain.ProcessExit -= handler;
        }

        [Fact]
        public void ProcessExit_Called()
        {
            System.IO.File.Delete("success.txt");
            RemoteInvoke(() =>
            {
                EventHandler handler = (sender, e) => 
                {
                    System.IO.File.Create("success.txt");
                };

                AppDomain.CurrentDomain.ProcessExit += handler;
                return SuccessExitCode;
            }).Dispose();

            Assert.True(System.IO.File.Exists("success.txt"));
        }

        [Fact]
        public void ApplyPolicy()
        {
            Assert.Throws<ArgumentNullException>("assemblyName", () => { AppDomain.CurrentDomain.ApplyPolicy(null); });
            Assert.Throws<ArgumentException>(() => { AppDomain.CurrentDomain.ApplyPolicy(""); });
            Assert.Equal(AppDomain.CurrentDomain.ApplyPolicy(Assembly.GetEntryAssembly().FullName), Assembly.GetEntryAssembly().FullName);
        }

        [Fact]
        public void CreateDomain()
        {
            Assert.Throws<ArgumentNullException>("friendlyName", () => { AppDomain.CreateDomain(null); });
            Assert.Throws<PlatformNotSupportedException>(() => { AppDomain.CreateDomain("test"); });
        }

        [Fact]
        public void ExecuteAssemblyByName()
        {
            string name = "TestApp";
            var assembly = Assembly.Load(name);
            Assert.Equal(5,  AppDomain.CurrentDomain.ExecuteAssemblyByName(assembly.FullName));
            Assert.Equal(10, AppDomain.CurrentDomain.ExecuteAssemblyByName(assembly.FullName, new string[2] {"2", "3"}));
            Assert.Throws<FormatException>(() => AppDomain.CurrentDomain.ExecuteAssemblyByName(assembly.FullName, new string[1] {"a"}));
            AssemblyName assemblyName = assembly.GetName();
            assemblyName.CodeBase = null;
            Assert.Equal(105, AppDomain.CurrentDomain.ExecuteAssemblyByName(assemblyName, new string[3] {"50", "25", "25"}));
        }

        [Fact]
        public void ExecuteAssembly()
        {
            string name = Path.Combine(Environment.CurrentDirectory, "TestAppOutsideOfTPA", "TestAppOutsideOfTPA.exe");
            Assert.Throws<ArgumentNullException>("assemblyFile", () => AppDomain.CurrentDomain.ExecuteAssembly(null));
            Assert.Throws<FileNotFoundException>(() => AppDomain.CurrentDomain.ExecuteAssembly("NonExistentFile.exe"));
            Assert.Throws<PlatformNotSupportedException>(() => AppDomain.CurrentDomain.ExecuteAssembly(name, new string[2] {"2", "3"}, null, Configuration.Assemblies.AssemblyHashAlgorithm.SHA1));
            Assert.Equal(5, AppDomain.CurrentDomain.ExecuteAssembly(name));
            Assert.Equal(10, AppDomain.CurrentDomain.ExecuteAssembly(name, new string[2] { "2", "3" }));
        }        

        [Fact]
        public void GetData_SetData()
        {
            Assert.Throws<ArgumentNullException>("name", () => { AppDomain.CurrentDomain.SetData(null, null); });
            AppDomain.CurrentDomain.SetData("", null);
            Assert.Null(AppDomain.CurrentDomain.GetData(""));  
            AppDomain.CurrentDomain.SetData("randomkey", 4);
            Assert.Equal(4, AppDomain.CurrentDomain.GetData("randomkey"));
        }

        [Fact]
        public void IsCompatibilitySwitchSet()
        {
            Assert.Throws<ArgumentNullException>(() => { AppDomain.CurrentDomain.IsCompatibilitySwitchSet(null); });
            Assert.Throws<ArgumentException>(() => { AppDomain.CurrentDomain.IsCompatibilitySwitchSet("");});
            Assert.Null(AppDomain.CurrentDomain.IsCompatibilitySwitchSet("randomSwitch"));
        }

        [Fact]
        public void IsDefaultAppDomain()
        {
            Assert.True(AppDomain.CurrentDomain.IsDefaultAppDomain());
        }

        [Fact]
        public void IsFinalizingForUnload()
        {
            Assert.False(AppDomain.CurrentDomain.IsFinalizingForUnload());
        }

        [Fact]
        public void toString()
        {
            string actual = AppDomain.CurrentDomain.ToString();
            string expected = "Name:" + AppDomain.CurrentDomain.FriendlyName + Environment.NewLine + "There are no context policies.";
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Unload()
        {
            Assert.Throws<ArgumentNullException>("domain", () => { AppDomain.Unload(null);});
            Assert.Throws<CannotUnloadAppDomainException>(() => { AppDomain.Unload(AppDomain.CurrentDomain); });
        }

        [Fact]
        public void Load()
        {
            AssemblyName assemblyName = typeof(AppDomainTests).Assembly.GetName();
            assemblyName.CodeBase = null;
            Assert.NotNull(AppDomain.CurrentDomain.Load(assemblyName));
            Assert.NotNull(AppDomain.CurrentDomain.Load(typeof(AppDomainTests).Assembly.FullName));

            Assembly assembly = typeof(AppDomainTests).Assembly;
            byte[] aBytes = System.IO.File.ReadAllBytes(assembly.Location);
            Assert.NotNull(AppDomain.CurrentDomain.Load(aBytes));
        }

        [Fact]
        public void ReflectionOnlyGetAssemblies()
        {
            Assert.Equal(Array.Empty<Assembly>(), AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies());
        }

        [Fact]
        public void MonitoringIsEnabled()
        {
            Assert.False(AppDomain.MonitoringIsEnabled);
            Assert.Throws<ArgumentException>(() => {AppDomain.MonitoringIsEnabled = false;});
            Assert.Throws<PlatformNotSupportedException>(() => {AppDomain.MonitoringIsEnabled = true;});
        }

        [Fact]
        public void MonitoringSurvivedMemorySize()
        {
            Assert.Throws<InvalidOperationException>(() => { var t = AppDomain.CurrentDomain.MonitoringSurvivedMemorySize; });
        }

        [Fact]
        public void MonitoringSurvivedProcessMemorySize()
        {
            Assert.Throws<InvalidOperationException>(() => { var t = AppDomain.MonitoringSurvivedProcessMemorySize; });
        }

        [Fact]
        public void MonitoringTotalAllocatedMemorySize()
        {
            Assert.Throws<InvalidOperationException>(() => { var t = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize; } );
        }

        [Fact]
        public void MonitoringTotalProcessorTime()
        {
            Assert.Throws<InvalidOperationException>(() => { var t = AppDomain.CurrentDomain.MonitoringTotalProcessorTime; } );
        }

#pragma warning disable 618
        [Fact]
        public void GetCurrentThreadId()
        {
            Assert.True(AppDomain.GetCurrentThreadId() == Environment.CurrentManagedThreadId);
        }

        [Fact]
        public void ShadowCopyFiles()
        {
            Assert.False(AppDomain.CurrentDomain.ShadowCopyFiles);
        }

        [Fact]
        public void AppendPrivatePath()
        {
            AppDomain.CurrentDomain.AppendPrivatePath("test");
        }

        [Fact]
        public void ClearPrivatePath()
        {
            AppDomain.CurrentDomain.ClearPrivatePath();
        }

        [Fact]
        public void ClearShadowCopyPath()
        {
            AppDomain.CurrentDomain.ClearShadowCopyPath();
        }

        [Fact]
        public void SetCachePath()
        {
            AppDomain.CurrentDomain.SetCachePath("test");
        }

        [Fact]
        public void SetShadowCopyFiles()
        {
            AppDomain.CurrentDomain.SetShadowCopyFiles();
        }

        [Fact]
        public void SetShadowCopyPath()
        {
            AppDomain.CurrentDomain.SetShadowCopyPath("test");
        }
#pragma warning restore 618

        [Fact]
        public void GetAssemblies()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Assert.NotNull(assemblies);
            Assert.True(assemblies.Length > 0, "There must be assemblies already loaded in the process");
            AppDomain.CurrentDomain.Load(typeof(AppDomainTests).Assembly.GetName().FullName);
            Assembly[] assemblies1 = AppDomain.CurrentDomain.GetAssemblies();
            // Another thread could have loaded an assembly hence not checking for equality
            Assert.True(assemblies1.Length >= assemblies.Length, "Assembly.Load of an already loaded assembly should not cause another load");
            Assembly.LoadFile(typeof(AppDomain).Assembly.Location);
            Assembly[] assemblies2 = AppDomain.CurrentDomain.GetAssemblies();
            Assert.True(assemblies2.Length > assemblies.Length, "Assembly.LoadFile should cause an increase in GetAssemblies list");
            int ctr = 0;
            foreach (var a in assemblies2)
            {
                if (a.Location == typeof(AppDomain).Assembly.Location)
                    ctr++;
            }
            foreach (var a in assemblies)
            {
                if (a.Location == typeof(AppDomain).Assembly.Location)
                    ctr--;
            }
            Assert.True(ctr > 0, "Assembly.LoadFile should cause file to be loaded again");
        }

        [Fact]
        public void AssemblyLoad()
        {
            bool AssemblyLoadFlag = false;
            AssemblyLoadEventHandler handler = (sender, args) =>
            {
                if (args.LoadedAssembly.FullName.Equals(typeof(AppDomainTests).Assembly.FullName))
                {
                    AssemblyLoadFlag = !AssemblyLoadFlag;
                }
            };

            AppDomain.CurrentDomain.AssemblyLoad += handler;

            try
            {
                Assembly.LoadFile(typeof(AppDomainTests).Assembly.Location);
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyLoad -= handler;
            }
            Assert.True(AssemblyLoadFlag);
        }

        [Fact]
        public void AssemblyResolve()
        {
            RemoteInvoke(() =>
            {
                ResolveEventHandler handler = (sender, e) =>
                {
                    return Assembly.LoadFile(Path.Combine(Environment.CurrentDirectory, "AssemblyResolveTests", "AssemblyResolveTests.dll"));
                };

                AppDomain.CurrentDomain.AssemblyResolve += handler;

                Type t = Type.GetType("AssemblyResolveTests.Class1, AssemblyResolveTests", true);
                Assert.NotNull(t);
                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void AssemblyResolve_RequestingAssembly()
        {
            RemoteInvoke(() =>
            {
                Assembly a = Assembly.LoadFile(Path.Combine(Environment.CurrentDirectory, "TestAppOutsideOfTPA", "TestAppOutsideOfTPA.exe"));

                ResolveEventHandler handler = (sender, e) =>
                {
                    Assert.Equal(e.RequestingAssembly, a);
                    return Assembly.LoadFile(Path.Combine(Environment.CurrentDirectory, "AssemblyResolveTests", "AssemblyResolveTests.dll"));
                };

                AppDomain.CurrentDomain.AssemblyResolve += handler;
                Type ptype = a.GetType("Program");
                MethodInfo myMethodInfo = ptype.GetMethod("foo");
                object ret = myMethodInfo.Invoke(null, null);
                Assert.NotNull(ret);
                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void TypeResolve()
        {
            Assert.Throws<TypeLoadException>(() => Type.GetType("Program", true));

            ResolveEventHandler handler = (sender, args) =>
            {
                return Assembly.Load("TestApp");
            };

            AppDomain.CurrentDomain.TypeResolve += handler;

            Type t;
            try
            {
                t = Type.GetType("Program", true);
            }
            finally
            {
                AppDomain.CurrentDomain.TypeResolve -= handler;
            }
            Assert.NotNull(t);
        }

        [Fact]
        public void ResourceResolve()
        {
            ResourceManager res = new ResourceManager(typeof(FxResources.TestApp.SR));
            Assert.Throws<MissingManifestResourceException>(() => res.GetString("Message"));

            ResolveEventHandler handler = (sender, args) =>
            {
                return Assembly.Load("TestApp");
            };

            AppDomain.CurrentDomain.ResourceResolve += handler;

            String s;
            try
            {
                s = res.GetString("Message");
            }
            finally
            {
                AppDomain.CurrentDomain.ResourceResolve -= handler;
            }
            Assert.Equal(s, "Happy Halloween");
        }

        [Fact]       
        public void SetThreadPrincipal()
        {
            Assert.Throws<ArgumentNullException>(() => {AppDomain.CurrentDomain.SetThreadPrincipal(null);});
            var identity = new System.Security.Principal.GenericIdentity("NewUser");
            var principal = new System.Security.Principal.GenericPrincipal(identity, null);
            AppDomain.CurrentDomain.SetThreadPrincipal(principal);
        }
    }
}

namespace FxResources.TestApp
{
    class SR { }
}
