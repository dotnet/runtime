// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Resources;
using System.Runtime.ExceptionServices;
using System.Runtime.Remoting;
using System.Security;
using System.Security.Permissions;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Tests
{
    public class AppDomainTests : FileCleanupTestBase
    {
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
        [SkipOnPlatform(TestPlatforms.Browser, "throws pNSE")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49868", TestPlatforms.Android)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36896", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void TargetFrameworkTest()
        {
            const int ExpectedExitCode = 0;
            const string AppName = "TargetFrameworkNameTestApp.dll";
            var psi = new ProcessStartInfo();
            psi.FileName = RemoteExecutor.HostRunner;
            psi.Arguments = $"{AppName} {ExpectedExitCode}";

            using (Process p = Process.Start(psi))
            {
                p.WaitForExit();
                Assert.Equal(ExpectedExitCode, p.ExitCode);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void UnhandledException_Add_Remove()
        {
            RemoteExecutor.Invoke(() => {
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
                AppDomain.CurrentDomain.UnhandledException -= new UnhandledExceptionEventHandler(MyHandler);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void UnhandledException_NotCalled_When_Handled()
        {
            RemoteExecutor.Invoke(() => {
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(NotExpectedToBeCalledHandler);
                try
                {
                    throw new Exception();
                }
                catch
                {
                }
                AppDomain.CurrentDomain.UnhandledException -= new UnhandledExceptionEventHandler(NotExpectedToBeCalledHandler);
            }).Dispose();
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/18984")]
        [SkipOnPlatform(TestPlatforms.OSX, "Unhandled exception on a separate process causes xunit to crash on osx")]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void UnhandledException_Called()
        {
            System.IO.File.Delete("success.txt");
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.CheckExitCode = false;
            RemoteExecutor.Invoke(() =>
            {
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
                throw new Exception("****This Unhandled Exception is Expected****");
#pragma warning disable 0162
                return RemoteExecutor.SuccessExitCode;
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
            File.Create("success.txt");
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
            string expected = Assembly.GetEntryAssembly()?.GetName()?.Name;

            // GetEntryAssembly may be null (i.e. desktop)
            if (expected == null)
                expected = "DefaultDomain";

            Assert.Equal(expected, s);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Id()
        {
            // if running directly on some platforms Xunit may be Id = 1
            RemoteExecutor.Invoke(() => {
                Assert.Equal(1, AppDomain.CurrentDomain.Id);
            }).Dispose();
        }

        [Fact]
        public void IsFullyTrusted()
        {
            Assert.True(AppDomain.CurrentDomain.IsFullyTrusted);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void IsHomogenous()
        {
            Assert.True(AppDomain.CurrentDomain.IsHomogenous);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void FirstChanceException_Add_Remove()
        {
            RemoteExecutor.Invoke(() => {
                EventHandler<FirstChanceExceptionEventArgs> handler = (sender, e) => { };
                AppDomain.CurrentDomain.FirstChanceException += handler;
                AppDomain.CurrentDomain.FirstChanceException -= handler;
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void FirstChanceException_Called()
        {
            RemoteExecutor.Invoke(() => {
                bool flag = false;
                EventHandler<FirstChanceExceptionEventArgs> handler = (sender, e) =>
                {
                    Exception ex = e.Exception;
                    if (ex is FirstChanceTestException)
                    {
                        flag = !flag;
                    }
                };
                AppDomain.CurrentDomain.FirstChanceException += handler;
                try
                {
                    throw new FirstChanceTestException("testing");
                }
                catch
                {
                }
                AppDomain.CurrentDomain.FirstChanceException -= handler;
                Assert.True(flag, "FirstChanceHandler not called");
            }).Dispose();
        }

        class FirstChanceTestException : Exception
        {
            public FirstChanceTestException(string message) : base(message)
            { }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ProcessExit_Add_Remove()
        {
            RemoteExecutor.Invoke(() => {
                EventHandler handler = (sender, e) => { };
                AppDomain.CurrentDomain.ProcessExit += handler;
                AppDomain.CurrentDomain.ProcessExit -= handler;
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ProcessExit_Called()
        {
            string path = GetTestFilePath();
            RemoteExecutor.Invoke((pathToFile) =>
            {
                EventHandler handler = (sender, e) =>
                {
                    Assert.Same(AppDomain.CurrentDomain, sender);
                    File.Create(pathToFile);
                };

                AppDomain.CurrentDomain.ProcessExit += handler;
            }, path).Dispose();

            Assert.True(File.Exists(path));
        }

        [Fact]
        public void ApplyPolicy()
        {
            AssertExtensions.Throws<ArgumentNullException>("assemblyName", () => { AppDomain.CurrentDomain.ApplyPolicy(null); });
            AssertExtensions.Throws<ArgumentException>("assemblyName", null, () => { AppDomain.CurrentDomain.ApplyPolicy(""); });
            string entryAssembly = Assembly.GetEntryAssembly()?.FullName ?? Assembly.GetExecutingAssembly().FullName;
            Assert.Equal(AppDomain.CurrentDomain.ApplyPolicy(entryAssembly), entryAssembly);
        }

#pragma warning disable SYSLIB0024 // Creating and unloading AppDomains is not supported and throws an exception.
        [Fact]
        public void CreateDomainNonNetfx()
        {
            AssertExtensions.Throws<ArgumentNullException>("friendlyName", () => { AppDomain.CreateDomain(null); });
            Assert.Throws<PlatformNotSupportedException>(() => { AppDomain.CreateDomain("test"); });
        }
#pragma warning restore SYSLIB0024

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ExecuteAssemblyByName()
        {
            RemoteExecutor.Invoke(() => {
                string name = "TestApp";
                var assembly = Assembly.Load(name);
                Assert.Equal(5, AppDomain.CurrentDomain.ExecuteAssemblyByName(assembly.FullName));
                Assert.Equal(10, AppDomain.CurrentDomain.ExecuteAssemblyByName(assembly.FullName, new string[2] { "2", "3" }));
                Assert.Throws<FormatException>(() => AppDomain.CurrentDomain.ExecuteAssemblyByName(assembly.FullName, new string[1] { "a" }));
                AssemblyName assemblyName = assembly.GetName();
                assemblyName.CodeBase = null;
                Assert.Equal(105, AppDomain.CurrentDomain.ExecuteAssemblyByName(assemblyName, new string[3] { "50", "25", "25" }));
            }).Dispose();
        }

        [Fact]
        public void ExecuteAssembly()
        {
            CopyTestAssemblies();

            string name = Path.Combine(Environment.CurrentDirectory, "TestAppOutsideOfTPA.exe");
            AssertExtensions.Throws<ArgumentNullException>("assemblyFile", () => AppDomain.CurrentDomain.ExecuteAssembly(null));
            Assert.Throws<FileNotFoundException>(() => AppDomain.CurrentDomain.ExecuteAssembly("NonExistentFile.exe"));

#pragma warning disable SYSLIB0003 // Code Access Security is not supported or honored by the runtime.
            Func<int> executeAssembly = () => AppDomain.CurrentDomain.ExecuteAssembly(name, new string[2] { "2", "3" }, null, Configuration.Assemblies.AssemblyHashAlgorithm.SHA1);
            Assert.Throws<PlatformNotSupportedException>(() => executeAssembly());
#pragma warning restore SYSLIB0003

            Assert.Equal(5, AppDomain.CurrentDomain.ExecuteAssembly(name));
            Assert.Equal(10, AppDomain.CurrentDomain.ExecuteAssembly(name, new string[2] { "2", "3" }));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void GetData_SetData()
        {
            RemoteExecutor.Invoke(() => {
                AssertExtensions.Throws<ArgumentNullException>("name", () => { AppDomain.CurrentDomain.SetData(null, null); });
                AppDomain.CurrentDomain.SetData("", null);
                Assert.Null(AppDomain.CurrentDomain.GetData(""));
                AppDomain.CurrentDomain.SetData("randomkey", 4);
                Assert.Equal(4, AppDomain.CurrentDomain.GetData("randomkey"));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SetData_SameKeyMultipleTimes_ReplacesOldValue()
        {
            RemoteExecutor.Invoke(() => {
                string key = Guid.NewGuid().ToString("N");
                for (int i = 0; i < 3; i++)
                {
                    AppDomain.CurrentDomain.SetData(key, i.ToString());
                    Assert.Equal(i.ToString(), AppDomain.CurrentDomain.GetData(key));
                }
                AppDomain.CurrentDomain.SetData(key, null);
            }).Dispose();
        }

        [Fact]
        public void IsCompatibilitySwitchSet()
        {
            Assert.Throws<ArgumentNullException>(() => { AppDomain.CurrentDomain.IsCompatibilitySwitchSet(null); });
            AssertExtensions.Throws<ArgumentException>("switchName", () => { AppDomain.CurrentDomain.IsCompatibilitySwitchSet("");});
            Assert.Null(AppDomain.CurrentDomain.IsCompatibilitySwitchSet("randomSwitch"));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void IsDefaultAppDomain()
        {
            // Xunit may be default app domain if run directly
            RemoteExecutor.Invoke(() =>
            {
                Assert.True(AppDomain.CurrentDomain.IsDefaultAppDomain());
            }).Dispose();
        }

        [Fact]
        public void IsFinalizingForUnload()
        {
            Assert.False(AppDomain.CurrentDomain.IsFinalizingForUnload());
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void toString()
        {
            // Workaround issue: UWP culture is process wide
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

                string actual = AppDomain.CurrentDomain.ToString();

                string expected = "Name:" + AppDomain.CurrentDomain.FriendlyName + Environment.NewLine + "There are no context policies.";
                Assert.Equal(expected, actual);

            }).Dispose();
        }

#pragma warning disable SYSLIB0024 // Creating and unloading AppDomains is not supported and throws an exception.
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Unload()
        {
            RemoteExecutor.Invoke(() => {
                AssertExtensions.Throws<ArgumentNullException>("domain", () => { AppDomain.Unload(null); });
                Assert.Throws<CannotUnloadAppDomainException>(() => { AppDomain.Unload(AppDomain.CurrentDomain); });
            }).Dispose();
        }
#pragma warning restore SYSLIB0024

        [Fact]
        public void Load()
        {
            AssemblyName assemblyName = typeof(AppDomainTests).Assembly.GetName();
            assemblyName.CodeBase = null;
            Assert.NotNull(AppDomain.CurrentDomain.Load(assemblyName));
            Assert.NotNull(AppDomain.CurrentDomain.Load(typeof(AppDomainTests).Assembly.FullName));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser.")]
        public void LoadBytes()
        {
            Assembly assembly = typeof(AppDomainTests).Assembly;
            byte[] aBytes = System.IO.File.ReadAllBytes(assembly.Location);
            Assert.NotNull(AppDomain.CurrentDomain.Load(aBytes));
        }

        [Fact]
        public void ReflectionOnlyGetAssemblies()
        {
            Assert.Equal(0, AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies().Length);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Throws PNSE")]
        public void MonitoringIsEnabled()
        {
            Assert.True(AppDomain.MonitoringIsEnabled);
            Assert.Throws<ArgumentException>(() => { AppDomain.MonitoringIsEnabled = false; });
            AppDomain.MonitoringIsEnabled = true;

            const int AllocationSize = 1_234_567;
            object o = new byte[AllocationSize];
            GC.Collect();

            Assert.InRange(AppDomain.MonitoringSurvivedProcessMemorySize, AllocationSize, long.MaxValue);
            Assert.InRange(AppDomain.CurrentDomain.MonitoringSurvivedMemorySize, AllocationSize, long.MaxValue);
            Assert.InRange(AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize, AllocationSize, long.MaxValue);

            using (Process p = Process.GetCurrentProcess())
            {
                TimeSpan processTime = p.UserProcessorTime;
                Assert.InRange(processTime, TimeSpan.Zero, TimeSpan.MaxValue);

                TimeSpan monitoringTime = AppDomain.CurrentDomain.MonitoringTotalProcessorTime;
                Assert.InRange(monitoringTime, processTime * 0.95, TimeSpan.MaxValue); // *0.95 for a bit of wiggle room due to precision differences with employed timing mechanisms
            }

            GC.KeepAlive(o);
        }

#pragma warning disable 618
        [Fact]
        public void GetCurrentThreadId()
        {
            Assert.Equal(AppDomain.GetCurrentThreadId(), Environment.CurrentManagedThreadId);
        }

        [Fact]
        public void ShadowCopyFiles()
        {
            Assert.False(AppDomain.CurrentDomain.ShadowCopyFiles);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void AppendPrivatePath()
        {
            RemoteExecutor.Invoke(() => {
                AppDomain.CurrentDomain.AppendPrivatePath("test");
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ClearPrivatePath()
        {
            RemoteExecutor.Invoke(() => {
                AppDomain.CurrentDomain.ClearPrivatePath();
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ClearShadowCopyPath()
        {
            RemoteExecutor.Invoke(() => {
                AppDomain.CurrentDomain.ClearShadowCopyPath();
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SetCachePath()
        {
            RemoteExecutor.Invoke(() => {
                AppDomain.CurrentDomain.SetCachePath("test");
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SetShadowCopyFiles()
        {
            RemoteExecutor.Invoke(() => {
                AppDomain.CurrentDomain.SetShadowCopyFiles();
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SetShadowCopyPath()
        {
            RemoteExecutor.Invoke(() => {
                AppDomain.CurrentDomain.SetShadowCopyPath("test");
            }).Dispose();
        }

#pragma warning restore 618
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void GetAssemblies()
        {
            RemoteExecutor.Invoke(() => {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                Assert.NotNull(assemblies);
                Assert.True(assemblies.Length > 0, "There must be assemblies already loaded in the process");
                AppDomain.CurrentDomain.Load(typeof(AppDomainTests).Assembly.GetName().FullName);
                Assembly[] assemblies1 = AppDomain.CurrentDomain.GetAssemblies();
                // Another thread could have loaded an assembly hence not checking for equality
                Assert.True(assemblies1.Length >= assemblies.Length, "Assembly.Load of an already loaded assembly should not cause another load");
                Type someType = typeof(HttpClient);
                Assembly.LoadFile(someType.Assembly.Location);
                Assembly[] assemblies2 = AppDomain.CurrentDomain.GetAssemblies();
                Assert.True(assemblies2.Length > assemblies.Length, "Assembly.LoadFile should cause an increase in GetAssemblies list");
                int ctr = 0;
                foreach (var a in assemblies2)
                {
                    // Dynamic assemblies do not support Location property.
                    if (!a.IsDynamic)
                    {
                        if (a.Location == someType.Assembly.Location)
                            ctr++;
                    }
                }
                foreach (var a in assemblies)
                {
                    if (!a.IsDynamic)
                    {
                        if (a.Location == someType.Assembly.Location)
                            ctr--;
                    }
                }
                Assert.True(ctr > 0, "Assembly.LoadFile should cause file to be loaded again");
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void AssemblyLoad()
        {
            RemoteExecutor.Invoke(() => {
                bool AssemblyLoadFlag = false;
                AssemblyLoadEventHandler handler = (sender, args) =>
                {
                    Assert.Same(AppDomain.CurrentDomain, sender);
                    Assert.NotNull(args);
                    Assert.NotNull(args.LoadedAssembly);

                    if (args.LoadedAssembly.FullName.Equals(typeof(AppDomainTests).Assembly.FullName))
                    {
                        AssemblyLoadFlag = true;
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
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void AssemblyResolveInvalidAssemblyName()
        {
            RemoteExecutor.Invoke(() => {
                bool AssemblyResolveFlag = false;
                ResolveEventHandler handler = (sender, args) =>
                {
                    Assert.Same(AppDomain.CurrentDomain, sender);
                    Assert.NotNull(args);
                    Assert.NotNull(args.Name);
                    Assert.NotNull(args.RequestingAssembly);
                    AssemblyResolveFlag = true;
                    return null;
                };

                AppDomain.CurrentDomain.AssemblyResolve += handler;

                Type t = Type.GetType("AssemblyResolveTestApp.Class1, InvalidAssemblyName", throwOnError : false);
                Assert.Null(t);
                Assert.True(AssemblyResolveFlag);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void AssemblyResolve()
        {
            CopyTestAssemblies();

            RemoteExecutor.Invoke(() => {
                // bool AssemblyResolveFlag = false;
                ResolveEventHandler handler = (sender, args) =>
                {
                    Assert.Same(AppDomain.CurrentDomain, sender);
                    Assert.NotNull(args);
                    Assert.NotNull(args.Name);
                    Assert.NotNull(args.RequestingAssembly);
                    // AssemblyResolveFlag = true;
                    return Assembly.LoadFile(Path.Combine(Environment.CurrentDirectory, "AssemblyResolveTestApp.dll"));
                };

                AppDomain.CurrentDomain.AssemblyResolve += handler;

                Type t = Type.GetType("AssemblyResolveTestApp.Class1, AssemblyResolveTestApp", true);
                Assert.NotNull(t);
                // https://github.com/dotnet/runtime/issues/29817
                // Assert.True(AssemblyResolveFlag);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void AssemblyResolve_RequestingAssembly()
        {
            CopyTestAssemblies();

            RemoteExecutor.Invoke(() => {
                // bool AssemblyResolveFlag = false;

                Assembly a = Assembly.LoadFile(Path.Combine(Environment.CurrentDirectory, "TestAppOutsideOfTPA.exe"));

                ResolveEventHandler handler = (sender, args) =>
                {
                    Assert.Same(AppDomain.CurrentDomain, sender);
                    Assert.NotNull(args);
                    Assert.NotNull(args.Name);
                    Assert.Same(a, args.RequestingAssembly);
                    // AssemblyResolveFlag = true;
                    return Assembly.LoadFile(Path.Combine(Environment.CurrentDirectory, "AssemblyResolveTestApp.dll"));
                };

                AppDomain.CurrentDomain.AssemblyResolve += handler;
                Type ptype = a.GetType("Program");
                MethodInfo myMethodInfo = ptype.GetMethod("foo");
                object ret = myMethodInfo.Invoke(null, null);
                Assert.NotNull(ret);
                // https://github.com/dotnet/runtime/issues/29817
                // Assert.True(AssemblyResolveFlag);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void AssemblyResolve_IsNotCalledForCoreLibResources()
        {
            RemoteExecutor.Invoke(() =>
            {
                bool assemblyResolveHandlerCalled = false;
                AppDomain.CurrentDomain.AssemblyResolve +=
                    (sender, e) =>
                    {
                        // This implementation violates the contract. AssemblyResolve event handler is supposed to return an assembly
                        // that matches the requested identity and that is not the case here.
                        assemblyResolveHandlerCalled = true;
                        return typeof(AppDomainTests).Assembly;
                    };

                CultureInfo previousUICulture = CultureInfo.CurrentUICulture;
                CultureInfo.CurrentUICulture = new CultureInfo("de-CH");
                try
                {
                    // The resource lookup for NullReferenceException (generally for CoreLib resources) should not raise the
                    // AssemblyResolve event because a misbehaving handler could cause an infinite recursion check and fail-fast to
                    // be triggered when the resource is not found, as the issue would repeat when reporting that error.
                    Assert.Throws<NullReferenceException>(() => ((string)null).Contains("a"));
                    Assert.False(assemblyResolveHandlerCalled);
                }
                finally
                {
                    CultureInfo.CurrentUICulture = previousUICulture;
                }
            }).Dispose();
        }

        class CorrectlyPropagatesException : Exception
        {
            public CorrectlyPropagatesException(string message) : base(message)
            { }
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/43909", TestRuntimes.Mono)]
        [InlineData(true)]
        [InlineData(false)]
        public void AssemblyResolve_ExceptionPropagatesCorrectly(bool throwOnError)
        {
            bool handlerExceptionThrown = false;

            ResolveEventHandler handler = (sender, args) =>
            {
                if (args.Name.StartsWith("Some.Assembly"))
                    throw new CorrectlyPropagatesException("Failure");
                return null;
            };

            AppDomain.CurrentDomain.AssemblyResolve += handler;

            try
            {
                Type.GetType("Some.Assembly.Type, Some.Assembly", throwOnError);
            }
            catch (FileLoadException e)
            {
                Assert.NotNull(e.InnerException);
                Assert.IsAssignableFrom<CorrectlyPropagatesException>(e.InnerException);
                Assert.Equal("Failure", e.InnerException.Message);
                handlerExceptionThrown = true;
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= handler;
            }

            Assert.True(handlerExceptionThrown);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TypeResolve()
        {
            RemoteExecutor.Invoke(() => {
                Assert.Throws<TypeLoadException>(() => Type.GetType("Program", true));

                ResolveEventHandler handler = (sender, args) =>
                {
                    Assert.Same(AppDomain.CurrentDomain, sender);
                    Assert.NotNull(args);
                    Assert.NotNull(args.Name);
                    Assert.NotNull(args.RequestingAssembly);
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
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ResourceResolve()
        {
            RemoteExecutor.Invoke(() => {
                ResourceManager res = new ResourceManager(typeof(FxResources.TestApp.SR));
                Assert.Throws<MissingManifestResourceException>(() => res.GetString("Message"));

                ResolveEventHandler handler = (sender, args) =>
                {
                    Assert.Same(AppDomain.CurrentDomain, sender);
                    Assert.NotNull(args);
                    Assert.NotNull(args.Name);
                    Assert.NotNull(args.RequestingAssembly);
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
                Assert.Equal("Happy Halloween", s);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SetThreadPrincipal()
        {
            RemoteExecutor.Invoke(() => {
                Assert.Throws<ArgumentNullException>(() => { AppDomain.CurrentDomain.SetThreadPrincipal(null); });
                var identity = new System.Security.Principal.GenericIdentity("NewUser");
                var principal = new System.Security.Principal.GenericPrincipal(identity, null);
                AppDomain.CurrentDomain.SetThreadPrincipal(principal);
            }).Dispose();
        }

        private void CopyTestAssemblies()
        {
            string destTestAssemblyPath = Path.Combine(Environment.CurrentDirectory, "AssemblyResolveTestApp", "AssemblyResolveTestApp.dll");
            if (!File.Exists(destTestAssemblyPath) && File.Exists("AssemblyResolveTestApp.dll"))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destTestAssemblyPath));
                File.Copy("AssemblyResolveTestApp.dll", destTestAssemblyPath, false);
            }

            destTestAssemblyPath = Path.Combine(Environment.CurrentDirectory, "TestAppOutsideOfTPA", "TestAppOutsideOfTPA.exe");
            if (!File.Exists(destTestAssemblyPath) && File.Exists("TestAppOutsideOfTPA.exe"))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destTestAssemblyPath));
                File.Copy("TestAppOutsideOfTPA.exe", destTestAssemblyPath, false);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void GetSetupInformation()
        {
            RemoteExecutor.Invoke(() => {
                Assert.Equal(AppContext.BaseDirectory, AppDomain.CurrentDomain.SetupInformation.ApplicationBase);
                Assert.Equal(AppContext.TargetFrameworkName, AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void GetPermissionSet()
        {
#pragma warning disable SYSLIB0003 // Obsolete: CAS
            RemoteExecutor.Invoke(() => {
                Assert.Equal(new PermissionSet(PermissionState.Unrestricted), AppDomain.CurrentDomain.PermissionSet);
            }).Dispose();
#pragma warning restore SYSLIB0003 // Obsolete: CAS
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34030", TestPlatforms.Linux | TestPlatforms.Browser | TestPlatforms.Android, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [MemberData(nameof(TestingCreateInstanceFromObjectHandleData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36896", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public static void TestingCreateInstanceFromObjectHandle(string physicalFileName, string assemblyFile, string type, string returnedFullNameType, Type exceptionType)
        {
            ObjectHandle oh = null;
            object obj = null;

            if (exceptionType != null)
            {
                Assert.Throws(exceptionType, () => AppDomain.CurrentDomain.CreateInstanceFrom(assemblyFile: assemblyFile, typeName: type));
                Assert.Throws(exceptionType, () => AppDomain.CurrentDomain.CreateInstanceFromAndUnwrap(assemblyFile: assemblyFile, typeName: type));
            }
            else
            {
                oh = AppDomain.CurrentDomain.CreateInstanceFrom(assemblyFile: assemblyFile, typeName: type);
                CheckValidity(oh, returnedFullNameType);

                obj = AppDomain.CurrentDomain.CreateInstanceFromAndUnwrap(assemblyFile: assemblyFile, typeName: type);
                CheckValidity(obj, returnedFullNameType);
            }

            if (exceptionType != null)
            {
                Assert.Throws(exceptionType, () => AppDomain.CurrentDomain.CreateInstanceFrom(assemblyFile: assemblyFile, typeName: type, null));
                Assert.Throws(exceptionType, () => AppDomain.CurrentDomain.CreateInstanceFromAndUnwrap(assemblyFile: assemblyFile, typeName: type, null));
            }
            else
            {
                oh = AppDomain.CurrentDomain.CreateInstanceFrom(assemblyFile: assemblyFile, typeName: type, null);
                CheckValidity(oh, returnedFullNameType);

                obj = AppDomain.CurrentDomain.CreateInstanceFromAndUnwrap(assemblyFile: assemblyFile, typeName: type, null);
                CheckValidity(obj, returnedFullNameType);
            }
            Assert.True(File.Exists(physicalFileName));
        }

        public static TheoryData<string, string, string, string, Type> TestingCreateInstanceFromObjectHandleData => new TheoryData<string, string, string, string, Type>
        {
            // string physicalFileName, string assemblyFile, string typeName, returnedFullNameType, expectedException
            { "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.PublicClassSample", "AssemblyResolveTestApp.PublicClassSample", null },
            { "AssemblyResolveTestApp.dll", "assemblyresolvetestapp.dll", "assemblyresolvetestapp.publicclasssample", "AssemblyResolveTestApp.PublicClassSample", typeof(TypeLoadException) },

            { "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.PrivateClassSample", "AssemblyResolveTestApp.PrivateClassSample", null },
            { "AssemblyResolveTestApp.dll", "assemblyresolvetestapp.dll", "assemblyresolvetestapp.privateclasssample", "AssemblyResolveTestApp.PrivateClassSample", typeof(TypeLoadException) },

            { "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.PublicClassNoDefaultConstructorSample", "AssemblyResolveTestApp.PublicClassNoDefaultConstructorSample", typeof(MissingMethodException) },
            { "AssemblyResolveTestApp.dll", "assemblyresolvetestapp.dll", "assemblyresolvetestapp.publicclassnodefaultconstructorsample", "AssemblyResolveTestApp.PublicClassNoDefaultConstructorSample", typeof(TypeLoadException) }
        };

        [Theory]
        [MemberData(nameof(TestingCreateInstanceObjectHandleData))]
        public static void TestingCreateInstanceObjectHandle(string assemblyName, string type, string returnedFullNameType, Type exceptionType)
        {
            ObjectHandle oh = null;
            object obj = null;

            if (exceptionType != null)
            {
                Assert.Throws(exceptionType, () => AppDomain.CurrentDomain.CreateInstance(assemblyName: assemblyName, typeName: type));
                Assert.Throws(exceptionType, () => AppDomain.CurrentDomain.CreateInstanceAndUnwrap(assemblyName: assemblyName, typeName: type));
            }
            else
            {
                oh = AppDomain.CurrentDomain.CreateInstance(assemblyName: assemblyName, typeName: type);
                CheckValidity(oh, returnedFullNameType);

                obj = AppDomain.CurrentDomain.CreateInstanceAndUnwrap(assemblyName: assemblyName, typeName: type);
                CheckValidity(obj, returnedFullNameType);
            }

            if (exceptionType != null)
            {
                Assert.Throws(exceptionType, () => AppDomain.CurrentDomain.CreateInstance(assemblyName: assemblyName, typeName: type, null));
                Assert.Throws(exceptionType, () => AppDomain.CurrentDomain.CreateInstanceAndUnwrap(assemblyName: assemblyName, typeName: type, null));
            }
            else
            {
                oh = AppDomain.CurrentDomain.CreateInstance(assemblyName: assemblyName, typeName: type, null);
                CheckValidity(oh, returnedFullNameType);

                obj = AppDomain.CurrentDomain.CreateInstanceAndUnwrap(assemblyName: assemblyName, typeName: type, null);
                CheckValidity(obj, returnedFullNameType);
            }
        }

        public static TheoryData<string, string, string, Type> TestingCreateInstanceObjectHandleData => new TheoryData<string, string, string, Type>()
        {
            // string assemblyName, string typeName, returnedFullNameType, expectedException
            { "AssemblyResolveTestApp", "AssemblyResolveTestApp.PublicClassSample", "AssemblyResolveTestApp.PublicClassSample", null },
            { "assemblyresolvetestapp", "assemblyresolvetestapp.publicclasssample", "AssemblyResolveTestApp.PublicClassSample", typeof(TypeLoadException) },

            { "AssemblyResolveTestApp", "AssemblyResolveTestApp.PrivateClassSample", "AssemblyResolveTestApp.PrivateClassSample", null },
            { "assemblyresolvetestapp", "assemblyresolvetestapp.privateclasssample", "AssemblyResolveTestApp.PrivateClassSample", typeof(TypeLoadException) },

            { "AssemblyResolveTestApp", "AssemblyResolveTestApp.PublicClassNoDefaultConstructorSample", "AssemblyResolveTestApp.PublicClassNoDefaultConstructorSample", typeof(MissingMethodException) },
            { "assemblyresolvetestapp", "assemblyresolvetestapp.publicclassnodefaultconstructorsample", "AssemblyResolveTestApp.PublicClassNoDefaultConstructorSample", typeof(TypeLoadException) }
        };

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34030", TestPlatforms.Linux | TestPlatforms.Browser | TestPlatforms.Android, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [MemberData(nameof(TestingCreateInstanceFromObjectHandleFullSignatureData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36896", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public static void TestingCreateInstanceFromObjectHandleFullSignature(string physicalFileName, string assemblyFile, string type, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, string returnedFullNameType)
        {
            ObjectHandle oh = AppDomain.CurrentDomain.CreateInstanceFrom(assemblyFile: assemblyFile, typeName: type, ignoreCase: ignoreCase, bindingAttr: bindingAttr, binder: binder, args: args, culture: culture, activationAttributes: activationAttributes);
            CheckValidity(oh, returnedFullNameType);

            object obj = AppDomain.CurrentDomain.CreateInstanceFromAndUnwrap(assemblyFile: assemblyFile, typeName: type, ignoreCase: ignoreCase, bindingAttr: bindingAttr, binder: binder, args: args, culture: culture, activationAttributes: activationAttributes);
            CheckValidity(obj, returnedFullNameType);

            Assert.True(File.Exists(physicalFileName));
        }

        public static IEnumerable<object[]> TestingCreateInstanceFromObjectHandleFullSignatureData()
        {
            // string physicalFileName, string assemblyFile, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, returnedFullNameType
            yield return new object[] { "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.PublicClassSample", false, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PublicClassSample" };
            yield return new object[] { "AssemblyResolveTestApp.dll", "assemblyresolvetestapp.dll", "assemblyresolvetestapp.publicclasssample", true, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PublicClassSample" };
            yield return new object[] { "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.PublicClassSample", false, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PublicClassSample" };
            yield return new object[] { "AssemblyResolveTestApp.dll", "assemblyresolvetestapp.dll", "assemblyresolvetestapp.publicclasssample", true, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PublicClassSample" };

            yield return new object[] { "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.PrivateClassSample", false, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PrivateClassSample" };
            yield return new object[] { "AssemblyResolveTestApp.dll", "assemblyresolvetestapp.dll", "assemblyresolvetestapp.privateclasssample", true, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PrivateClassSample" };
            yield return new object[] { "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.dll", "AssemblyResolveTestApp.PrivateClassSample", false, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PrivateClassSample" };
            yield return new object[] { "AssemblyResolveTestApp.dll", "assemblyresolvetestapp.dll", "assemblyresolvetestapp.privateclasssample", true, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PrivateClassSample" };
        }

        [Theory]
        [MemberData(nameof(TestingCreateInstanceObjectHandleFullSignatureData))]
        public static void TestingCreateInstanceObjectHandleFullSignature(string assemblyName, string type, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, string returnedFullNameType)
        {
            ObjectHandle oh = AppDomain.CurrentDomain.CreateInstance(assemblyName: assemblyName, typeName: type, ignoreCase: ignoreCase, bindingAttr: bindingAttr, binder: binder, args: args, culture: culture, activationAttributes: activationAttributes);
            CheckValidity(oh, returnedFullNameType);

            object obj = AppDomain.CurrentDomain.CreateInstanceAndUnwrap(assemblyName: assemblyName, typeName: type, ignoreCase: ignoreCase, bindingAttr: bindingAttr, binder: binder, args: args, culture: culture, activationAttributes: activationAttributes);
            CheckValidity(obj, returnedFullNameType);
        }

        private static void CheckValidity(object instance, string expected)
        {
            Assert.NotNull(instance);
            Assert.Equal(expected, instance.GetType().FullName);
        }

        private static void CheckValidity(ObjectHandle instance, string expected)
        {
            Assert.NotNull(instance);
            Assert.Equal(expected, instance.Unwrap().GetType().FullName);
        }

        public static IEnumerable<object[]> TestingCreateInstanceObjectHandleFullSignatureData()
        {
            // string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, returnedFullNameType
            yield return new object[] { "AssemblyResolveTestApp", "AssemblyResolveTestApp.PublicClassSample", false, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PublicClassSample" };
            yield return new object[] { "assemblyresolvetestapp", "assemblyresolvetestapp.publicclasssample", true, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PublicClassSample" };
            yield return new object[] { "AssemblyResolveTestApp", "AssemblyResolveTestApp.PublicClassSample", false, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PublicClassSample" };
            yield return new object[] { "assemblyresolvetestapp", "assemblyresolvetestapp.publicclasssample", true, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PublicClassSample" };

            yield return new object[] { "AssemblyResolveTestApp", "AssemblyResolveTestApp.PrivateClassSample", false, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PrivateClassSample" };
            yield return new object[] { "assemblyresolvetestapp", "assemblyresolvetestapp.privateclasssample", true, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PrivateClassSample" };
            yield return new object[] { "AssemblyResolveTestApp", "AssemblyResolveTestApp.PrivateClassSample", false, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PrivateClassSample" };
            yield return new object[] { "assemblyresolvetestapp", "assemblyresolvetestapp.privateclasssample", true, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "AssemblyResolveTestApp.PrivateClassSample" };
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/37871", TestRuntimes.Mono)]
        public void AssemblyResolve_FirstChanceException()
        {
            RemoteExecutor.Invoke(() => {
                Assembly assembly = typeof(AppDomainTests).Assembly;

                Exception firstChanceExceptionThrown = null;

                EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs> firstChanceHandler = (source, args) =>
                {
                    firstChanceExceptionThrown = args.Exception;
                };

                AppDomain.CurrentDomain.FirstChanceException += firstChanceHandler;

                ResolveEventHandler assemblyResolveHandler = (sender, e) =>
                {
                    Assert.Equal(assembly, e.RequestingAssembly);
                    Assert.Null(firstChanceExceptionThrown);
                    return null;
                };

                AppDomain.CurrentDomain.AssemblyResolve += assemblyResolveHandler;

                Func<System.Runtime.Loader.AssemblyLoadContext, AssemblyName, Assembly> resolvingHandler = (context, name) =>
                {
                    return null;
                };

                // The issue resolved by https://github.com/dotnet/coreclr/pull/24450, was only reproduced when there was a Resolving handler present
                System.Runtime.Loader.AssemblyLoadContext.Default.Resolving += resolvingHandler;

                assembly.GetType("System.Tests.AGenericClass`1[[Bogus, BogusAssembly]]", false);
                Assert.Null(firstChanceExceptionThrown);

                Exception thrown = Assert.Throws<FileNotFoundException>(() => assembly.GetType("System.Tests.AGenericClass`1[[Bogus, AnotherBogusAssembly]]", true));
                Assert.Same(firstChanceExceptionThrown, thrown);
            }).Dispose();
        }
    }

    class AGenericClass<T>
    {
    }
}

namespace FxResources.TestApp
{
    class SR { }
}
