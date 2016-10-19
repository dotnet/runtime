// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Xunit;
using Xunit.NetCore.Extensions;

namespace System.Tests
{
    public class AppDomainTests : RemoteExecutorTestBase
    {
        static bool flag = false;
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
            bool _flag = flag;
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
            if(_flag == flag)
                Assert.True(false, "FirstChanceHandler not called");
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
            // TODO Enable when Assembly.Load(AssemblyName assemblyRef) is supported
            //Assert.Equal(105, AppDomain.CurrentDomain.ExecuteAssemblyByName(assembly.GetName(), new string[3] {"50", "25", "25"}));
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
            // TODO
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
            // TODO
        }

        [Fact]
        public void AssemblyLoad()
        {
            // TODO
        }

        [Fact]
        public void AssemblyResolve()
        {
            // TODO
        }

        [Fact]
        public void TypeResolve()
        {
            // TODO
        }

        [Fact]
        public void ResourceResolve ()
        {
            // TODO
        }
       
        /*
        [Fact]
        public void SetThreadPrincipal()
        {
            Assert.Throws<ArgumentNullException>(() => {AppDomain.CurrentDomain.SetThreadPrincipal(null);});
            var identity = new System.Security.Principal.GenericIdentity("NewUser");
            var principal = new System.Security.Principal.GenericPrincipal(identity, null);
            Assert.Throws<PlatformNotSupportedException>(() => {AppDomain.CurrentDomain.SetThreadPrincipal(principle);});
        }*/

    }
}
