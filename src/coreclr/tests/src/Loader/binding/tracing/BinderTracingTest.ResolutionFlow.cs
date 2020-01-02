﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

using TestLibrary;

using ResolutionStage = BinderTracingTests.ResolutionAttempt.ResolutionStage;
using ResolutionResult = BinderTracingTests.ResolutionAttempt.ResolutionResult;

namespace BinderTracingTests
{
    partial class BinderTracingTest
    {
        private static CustomALC alcInstance = new CustomALC("StaticInstance");
        private static Assembly loadedAssembly;

        // Matching assembly in load context:
        //   ResolutionAttempted : FindInLoadContext    (CustomALC) [Success]
        [BinderTest(isolate: true, testSetup: nameof(LoadSubdirectoryAssembly_InstanceALC))]
        public static BindOperation FindInLoadContext_CustomALC()
        {
            var assemblyName = new AssemblyName(SubdirectoryAssemblyName);
            Assembly asm = alcInstance.LoadFromAssemblyName(assemblyName);

            return new BindOperation()
            {
                AssemblyName = assemblyName,
                AssemblyLoadContext = alcInstance.ToString(),
                Success = true,
                ResultAssemblyName = asm.GetName(),
                ResultAssemblyPath = asm.Location,
                Cached = false,
                ResolutionAttempts = new List<ResolutionAttempt>()
                {
                    GetResolutionAttempt(assemblyName, ResolutionStage.FindInLoadContext, alcInstance, ResolutionResult.Success, asm)
                }
            };
        }

        // Matching assembly in load context:
        //   ResolutionAttempted : FindInLoadContext    (DefaultALC)    [Success]
        [BinderTest(isolate: true, testSetup: nameof(UseDependentAssembly))]
        public static BindOperation FindInLoadContext_DefaultALC()
        {
            var assemblyName = new AssemblyName(DependentAssemblyName);
            Assembly asm = AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);

            return new BindOperation()
            {
                AssemblyName = assemblyName,
                AssemblyLoadContext = DefaultALC,
                Success = true,
                ResultAssemblyName = asm.GetName(),
                ResultAssemblyPath = asm.Location,
                Cached = false,
                ResolutionAttempts = new List<ResolutionAttempt>()
                {
                    GetResolutionAttempt(assemblyName, ResolutionStage.FindInLoadContext, AssemblyLoadContext.Default, ResolutionResult.Success, asm)
                }
            };
        }

        // Incompatible version in load context:
        //   ResolutionAttempted : FindInLoadContext                    (CustomALC)     [IncompatibleVersion]
        //   ResolutionAttempted : AssemblyLoadContextLoad              (CustomALC)     [AssemblyNotFound]
        //   ResolutionAttempted : FindInLoadContext                    (DefaultALC)    [AssemblyNotFound]
        //   ResolutionAttempted : ApplicationAssemblies                (DefaultALC)    [AssemblyNotFound]
        //   ResolutionAttempted : DefaultAssemblyLoadContextFallback   (CustomALC)     [AssemblyNotFound]
        //   ResolutionAttempted : AssemblyLoadContextResolvingEvent    (CustomALC)     [AssemblyNotFound]
        //   ResolutionAttempted : AppDomainAssemblyResolveEvent        (CustomALC)     [AssemblyNotFound]
        [BinderTest(isolate: true, testSetup: nameof(LoadSubdirectoryAssembly_InstanceALC))]
        public static BindOperation FindInLoadContext_IncompatibleVersion()
        {
            var assemblyName = new AssemblyName($"{SubdirectoryAssemblyName}, Version=4.3.2.1");
            Assert.Throws<FileNotFoundException>(() => alcInstance.LoadFromAssemblyName(assemblyName));

            return new BindOperation()
            {
                AssemblyName = assemblyName,
                AssemblyLoadContext = alcInstance.ToString(),
                Success = false,
                Cached = false,
                ResolutionAttempts = new List<ResolutionAttempt>()
                {
                    GetResolutionAttempt(assemblyName, ResolutionStage.FindInLoadContext, alcInstance, ResolutionResult.IncompatibleVersion, loadedAssembly),
                    GetResolutionAttempt(assemblyName, ResolutionStage.AssemblyLoadContextLoad, alcInstance, ResolutionResult.AssemblyNotFound),
                    GetResolutionAttempt(assemblyName, ResolutionStage.FindInLoadContext, AssemblyLoadContext.Default, ResolutionResult.AssemblyNotFound),
                    GetResolutionAttempt(assemblyName, ResolutionStage.ApplicationAssemblies, AssemblyLoadContext.Default, ResolutionResult.AssemblyNotFound),
                    GetResolutionAttempt(assemblyName, ResolutionStage.DefaultAssemblyLoadContextFallback, alcInstance, ResolutionResult.AssemblyNotFound),
                    GetResolutionAttempt(assemblyName, ResolutionStage.AssemblyLoadContextResolvingEvent, alcInstance, ResolutionResult.AssemblyNotFound),
                    GetResolutionAttempt(assemblyName, ResolutionStage.AppDomainAssemblyResolveEvent, alcInstance, ResolutionResult.AssemblyNotFound)
                }
            };
        }

        // Successful load through application assemblies search:
        //   ResolutionAttempted : FindInLoadContext        (DefaultALC)    [AssemblyNotFound]
        //   ResolutionAttempted : ApplicationAssemblies    (DefaultALC)    [Success]
        [BinderTest(isolate: true)]
        public static BindOperation ApplicationAssemblies()
        {
            var assemblyName = new AssemblyName(DependentAssemblyName);
            Assembly asm = AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);

            return new BindOperation()
            {
                AssemblyName = assemblyName,
                AssemblyLoadContext = DefaultALC,
                Success = true,
                ResultAssemblyName = asm.GetName(),
                ResultAssemblyPath = asm.Location,
                Cached = false,
                ResolutionAttempts = new List<ResolutionAttempt>()
                {
                    GetResolutionAttempt(assemblyName, ResolutionStage.FindInLoadContext, AssemblyLoadContext.Default, ResolutionResult.AssemblyNotFound),
                    GetResolutionAttempt(assemblyName, ResolutionStage.ApplicationAssemblies, AssemblyLoadContext.Default, ResolutionResult.Success, asm)
                }
            };
        }

        // Mismatched assembly name from platform assemblies:
        //   ResolutionAttempted : FindInLoadContext                    (DefaultALC)    [AssemblyNotFound]
        //   ResolutionAttempted : ApplicationAssemblies                (DefaultALC)    [MismatchedAssemblyName]
        //   ResolutionAttempted : AssemblyLoadContextResolvingEvent    (DefaultALC)    [AssemblyNotFound]
        //   ResolutionAttempted : AppDomainAssemblyResolveEvent        (DefaultALC)    [AssemblyNotFound]
        [BinderTest(isolate: true)]
        public static BindOperation ApplicationAssemblies_MismatchedAssemblyName()
        {
            string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string assemblyPath = Path.Combine(appPath, $"{DependentAssemblyName}_Copy.dll");
            var assemblyName = new AssemblyName($"{DependentAssemblyName}_Copy, Culture=neutral, PublicKeyToken=null");
            try
            {
                File.Copy(Path.Combine(appPath,$"{DependentAssemblyName}.dll"), assemblyPath, true);
                Assert.Throws<FileNotFoundException>(() => AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName));
            }
            finally
            {
                File.Delete(assemblyPath);
            }

            return new BindOperation()
            {
                AssemblyName = assemblyName,
                AssemblyLoadContext = DefaultALC,
                Success = false,
                Cached = false,
                ResolutionAttempts = new List<ResolutionAttempt>()
                {
                    GetResolutionAttempt(assemblyName, ResolutionStage.FindInLoadContext, AssemblyLoadContext.Default, ResolutionResult.AssemblyNotFound),
                    GetResolutionAttempt(assemblyName, ResolutionStage.ApplicationAssemblies, AssemblyLoadContext.Default, ResolutionResult.MismatchedAssemblyName, UseDependentAssembly().GetName(), assemblyPath),
                    GetResolutionAttempt(assemblyName, ResolutionStage.AssemblyLoadContextResolvingEvent, AssemblyLoadContext.Default, ResolutionResult.AssemblyNotFound),
                    GetResolutionAttempt(assemblyName, ResolutionStage.AppDomainAssemblyResolveEvent, AssemblyLoadContext.Default, ResolutionResult.AssemblyNotFound)
                }
            };
        }

        // Successful load through AssemblyLoadContext.Load:
        //   ResolutionAttempted : FindInLoadContext        (CustomALC) [AssemblyNotFound]
        //   ResolutionAttempted : AssemblyLoadContextLoad  (CustomALC) [Success]
        [BinderTest]
        public static BindOperation AssemblyLoadContextLoad()
        {
            var assemblyName = new AssemblyName(SubdirectoryAssemblyName);
            var assemblyPath = Helpers.GetAssemblyInSubdirectoryPath(assemblyName.Name);
            CustomALC alc = new CustomALC(nameof(AssemblyLoadContextLoad));
            alc.EnableLoad(assemblyName.Name, assemblyPath);

            Assembly asm = alc.LoadFromAssemblyName(assemblyName);

            return new BindOperation()
            {
                AssemblyName = assemblyName,
                AssemblyLoadContext = alc.ToString(),
                Success = true,
                ResultAssemblyName = asm.GetName(),
                ResultAssemblyPath = asm.Location,
                Cached = false,
                ResolutionAttempts = new List<ResolutionAttempt>()
                {
                    GetResolutionAttempt(assemblyName, ResolutionStage.FindInLoadContext, alc, ResolutionResult.AssemblyNotFound),
                    GetResolutionAttempt(assemblyName, ResolutionStage.AssemblyLoadContextLoad, alc, ResolutionResult.Success, asm)
                }
            };
        }

        // Exception thrown in AssemblyLoadContext.Load:
        //   ResolutionAttempted : FindInLoadContext        (CustomALC) [AssemblyNotFound]
        //   ResolutionAttempted : AssemblyLoadContextLoad  (CustomALC) [Exception]
        [BinderTest]
        public static BindOperation AssemblyLoadContextLoad_Exception()
        {
            var assemblyName = new AssemblyName(SubdirectoryAssemblyName);
            var assemblyPath = Helpers.GetAssemblyInSubdirectoryPath(assemblyName.Name);
            CustomALC alc = new CustomALC(nameof(AssemblyLoadContextLoad), true /*throwOnLoad*/);

            Assert.Throws<FileLoadException, Exception>(() => alc.LoadFromAssemblyName(assemblyName));

            return new BindOperation()
            {
                AssemblyName = assemblyName,
                AssemblyLoadContext = alc.ToString(),
                Success = false,
                Cached = false,
                ResolutionAttempts = new List<ResolutionAttempt>()
                {
                    GetResolutionAttempt(assemblyName, ResolutionStage.FindInLoadContext, alc, ResolutionResult.AssemblyNotFound),
                    GetResolutionAttempt(assemblyName, ResolutionStage.AssemblyLoadContextLoad, alc, $"Exception on Load in '{alc.ToString()}'")
                }
            };
        }

        // Successful load through default ALC fallback:
        //   ResolutionAttempted : FindInLoadContext                    (CustomALC)     [AssemblyNotFound]
        //   ResolutionAttempted : AssemblyLoadContextLoad              (CustomALC)     [AssemblyNotFound]
        //   ResolutionAttempted : FindInLoadContext                    (DefaultALC)    [AssemblyNotFound]
        //   ResolutionAttempted : ApplicationAssemblies                (DefaultALC)    [Success]
        //   ResolutionAttempted : DefaultAssemblyLoadContextFallback   (CustomALC)     [Success]
        [BinderTest(isolate: true)]
        public static BindOperation DefaultAssemblyLoadContextFallback()
        {
            var assemblyName = new AssemblyName("System.Xml");
            CustomALC alc = new CustomALC(nameof(DefaultAssemblyLoadContextFallback));
            Assembly asm = alc.LoadFromAssemblyName(assemblyName);

            return new BindOperation()
            {
                AssemblyName = assemblyName,
                AssemblyLoadContext = alc.ToString(),
                Success = true,
                ResultAssemblyName = asm.GetName(),
                ResultAssemblyPath = asm.Location,
                Cached = false,
                ResolutionAttempts = new List<ResolutionAttempt>()
                {
                    GetResolutionAttempt(assemblyName, ResolutionStage.FindInLoadContext, alc, ResolutionResult.AssemblyNotFound),
                    GetResolutionAttempt(assemblyName, ResolutionStage.AssemblyLoadContextLoad, alc, ResolutionResult.AssemblyNotFound),
                    GetResolutionAttempt(assemblyName, ResolutionStage.FindInLoadContext, AssemblyLoadContext.Default, ResolutionResult.AssemblyNotFound),
                    GetResolutionAttempt(assemblyName, ResolutionStage.ApplicationAssemblies, AssemblyLoadContext.Default, ResolutionResult.Success, asm),
                    GetResolutionAttempt(assemblyName, ResolutionStage.DefaultAssemblyLoadContextFallback, alc, ResolutionResult.Success, asm)
                }
            };
        }

        // Successful load through AssemblyLoadContext.Resolving event (Custom ALC):
        //   ResolutionAttempted : FindInLoadContext                    (CustomALC)     [AssemblyNotFound]
        //   ResolutionAttempted : AssemblyLoadContextLoad              (CustomALC)     [AssemblyNotFound]
        //   ResolutionAttempted : FindInLoadContext                    (DefaultALC)    [AssemblyNotFound]
        //   ResolutionAttempted : ApplicationAssemblies                (DefaultALC)    [AssemblyNotFound]
        //   ResolutionAttempted : DefaultAssemblyLoadContextFallback   (CustomALC)     [AssemblyNotFound]
        //   ResolutionAttempted : AssemblyLoadContextResolvingEvent    (CustomALC)     [Success]
        [BinderTest(isolate: true)]
        public static BindOperation AssemblyLoadContextResolvingEvent_CustomALC()
        {
            var assemblyName = new AssemblyName(SubdirectoryAssemblyName);
            CustomALC alc = new CustomALC(nameof(AssemblyLoadContextResolvingEvent_CustomALC));
            using (var handlers = new Handlers(HandlerReturn.RequestedAssembly, alc))
            {
                Assembly asm = alc.LoadFromAssemblyName(assemblyName);

                return new BindOperation()
                {
                    AssemblyName = assemblyName,
                    AssemblyLoadContext = alc.ToString(),
                    Success = true,
                    ResultAssemblyName = asm.GetName(),
                    ResultAssemblyPath = asm.Location,
                    Cached = false,
                    ResolutionAttempts = new List<ResolutionAttempt>()
                    {
                        GetResolutionAttempt(assemblyName, ResolutionStage.FindInLoadContext, alc, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.AssemblyLoadContextLoad, alc, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.FindInLoadContext, AssemblyLoadContext.Default, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.ApplicationAssemblies, AssemblyLoadContext.Default, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.DefaultAssemblyLoadContextFallback, alc, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.AssemblyLoadContextResolvingEvent, alc, ResolutionResult.Success, asm)
                    },
                    AssemblyLoadContextResolvingHandlers = handlers.Invocations
                };
            }
        }

        // Successful load through AssemblyLoadContext.Resolving event (default ALC):
        //   ResolutionAttempted : FindInLoadContext                    (DefaultALC)    [AssemblyNotFound]
        //   ResolutionAttempted : ApplicationAssemblies                (DefaultALC)    [AssemblyNotFound]
        //   ResolutionAttempted : AssemblyLoadContextResolvingEvent    (DefaultALC)    [Success]
        [BinderTest(isolate: true)]
        public static BindOperation AssemblyLoadContextResolvingEvent_DefaultALC()
        {
            var assemblyName = new AssemblyName(SubdirectoryAssemblyName);
            using (var handlers = new Handlers(HandlerReturn.RequestedAssembly, AssemblyLoadContext.Default))
            {
                Assembly asm = AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);

                return new BindOperation()
                {
                    AssemblyName = assemblyName,
                    AssemblyLoadContext = DefaultALC,
                    Success = true,
                    ResultAssemblyName = asm.GetName(),
                    ResultAssemblyPath = asm.Location,
                    Cached = false,
                    ResolutionAttempts = new List<ResolutionAttempt>()
                    {
                        GetResolutionAttempt(assemblyName, ResolutionStage.FindInLoadContext, AssemblyLoadContext.Default, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.ApplicationAssemblies, AssemblyLoadContext.Default, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.AssemblyLoadContextResolvingEvent, AssemblyLoadContext.Default, ResolutionResult.Success, asm)
                    },
                    AssemblyLoadContextResolvingHandlers = handlers.Invocations
                };
            }
        }

        // Exception in AssemblyLoadContext.Resolving event handler (Custom ALC):
        //   ResolutionAttempted : FindInLoadContext                    (CustomALC)     [AssemblyNotFound]
        //   ResolutionAttempted : AssemblyLoadContextLoad              (CustomALC)     [AssemblyNotFound]
        //   ResolutionAttempted : FindInLoadContext                    (DefaultALC)    [AssemblyNotFound]
        //   ResolutionAttempted : ApplicationAssemblies                (DefaultALC)    [AssemblyNotFound]
        //   ResolutionAttempted : DefaultAssemblyLoadContextFallback   (CustomALC)     [AssemblyNotFound]
        //   ResolutionAttempted : AssemblyLoadContextResolvingEvent    (CustomALC)     [Exception]
        [BinderTest(isolate: true)]
        public static BindOperation AssemblyLoadContextResolvingEvent_CustomALC_Exception()
        {
            var assemblyName = new AssemblyName(SubdirectoryAssemblyName);
            CustomALC alc = new CustomALC(nameof(AssemblyLoadContextResolvingEvent_CustomALC_Exception));
            using (var handlers = new Handlers(HandlerReturn.Exception, alc))
            {
                Assert.Throws<FileLoadException, Exception>(() => alc.LoadFromAssemblyName(assemblyName));

                return new BindOperation()
                {
                    AssemblyName = assemblyName,
                    AssemblyLoadContext = alc.ToString(),
                    Success = false,
                    Cached = false,
                    ResolutionAttempts = new List<ResolutionAttempt>()
                    {
                        GetResolutionAttempt(assemblyName, ResolutionStage.FindInLoadContext, alc, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.AssemblyLoadContextLoad, alc, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.FindInLoadContext, AssemblyLoadContext.Default, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.ApplicationAssemblies, AssemblyLoadContext.Default, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.DefaultAssemblyLoadContextFallback, alc, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.AssemblyLoadContextResolvingEvent, alc, "Exception in handler for AssemblyLoadContext.Resolving")
                    },
                    AssemblyLoadContextResolvingHandlers = handlers.Invocations
                };
            }
        }

        // Exception in AssemblyLoadContext.Resolving event handler (default ALC):
        //   ResolutionAttempted : FindInLoadContext                    (DefaultALC)    [AssemblyNotFound]
        //   ResolutionAttempted : ApplicationAssemblies                (DefaultALC)    [AssemblyNotFound]
        //   ResolutionAttempted : AssemblyLoadContextResolvingEvent    (DefaultALC)    [Exception]
        [BinderTest(isolate: true)]
        public static BindOperation AssemblyLoadContextResolvingEvent_DefaultALC_Exception()
        {
            var assemblyName = new AssemblyName(SubdirectoryAssemblyName);
            using (var handlers = new Handlers(HandlerReturn.Exception, AssemblyLoadContext.Default))
            {
                Assert.Throws<FileLoadException>(() => AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName));

                return new BindOperation()
                {
                    AssemblyName = assemblyName,
                    AssemblyLoadContext = DefaultALC,
                    Success = false,
                    Cached = false,
                    ResolutionAttempts = new List<ResolutionAttempt>()
                    {
                        GetResolutionAttempt(assemblyName, ResolutionStage.FindInLoadContext, AssemblyLoadContext.Default, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.ApplicationAssemblies, AssemblyLoadContext.Default, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.AssemblyLoadContextResolvingEvent, AssemblyLoadContext.Default, "Exception in handler for AssemblyLoadContext.Resolving")
                    },
                    AssemblyLoadContextResolvingHandlers = handlers.Invocations
                };
            }
        }

        // Successful load through AppDomain.AssemblyResolve event (Custom ALC):
        //   ResolutionAttempted : FindInLoadContext                    (CustomALC)     [AssemblyNotFound]
        //   ResolutionAttempted : AssemblyLoadContextLoad              (CustomALC)     [AssemblyNotFound]
        //   ResolutionAttempted : FindInLoadContext                    (DefaultALC)    [AssemblyNotFound]
        //   ResolutionAttempted : ApplicationAssemblies                (DefaultALC)    [AssemblyNotFound]
        //   ResolutionAttempted : DefaultAssemblyLoadContextFallback   (CustomALC)     [AssemblyNotFound]
        //   ResolutionAttempted : AssemblyLoadContextResolvingEvent    (CustomALC)     [AssemblyNotFound]
        //   ResolutionAttempted : AppDomainAssemblyResolveEvent        (CustomALC)     [Success]
        [BinderTest(isolate: true)]
        public static BindOperation AppDomainAssemblyResolveEvent_CustomALC()
        {
            var assemblyName = new AssemblyName(SubdirectoryAssemblyName);
            CustomALC alc = new CustomALC(nameof(AppDomainAssemblyResolveEvent_CustomALC));
            using (var handlers = new Handlers(HandlerReturn.RequestedAssembly))
            {
                Assembly asm = alc.LoadFromAssemblyName(assemblyName);

                return new BindOperation()
                {
                    AssemblyName = assemblyName,
                    AssemblyLoadContext = alc.ToString(),
                    Success = true,
                    ResultAssemblyName = asm.GetName(),
                    ResultAssemblyPath = asm.Location,
                    Cached = false,
                    ResolutionAttempts = new List<ResolutionAttempt>()
                    {
                        GetResolutionAttempt(assemblyName, ResolutionStage.FindInLoadContext, alc, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.AssemblyLoadContextLoad, alc, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.FindInLoadContext, AssemblyLoadContext.Default, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.ApplicationAssemblies, AssemblyLoadContext.Default, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.DefaultAssemblyLoadContextFallback, alc, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.AssemblyLoadContextResolvingEvent, alc, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.AppDomainAssemblyResolveEvent, alc, ResolutionResult.Success, asm)
                    },
                    AppDomainAssemblyResolveHandlers = handlers.Invocations
                };
            }
        }

        // Successful load through AppDomain.AssemblyResolve event (default ALC):
        //   ResolutionAttempted : FindInLoadContext                    (DefaultALC)    [AssemblyNotFound]
        //   ResolutionAttempted : ApplicationAssemblies                (DefaultALC)    [AssemblyNotFound]
        //   ResolutionAttempted : AssemblyLoadContextResolvingEvent    (DefaultALC)    [AssemblyNotFound]
        //   ResolutionAttempted : AppDomainAssemblyResolveEvent        (DefaultALC)    [Success]
        [BinderTest(isolate: true)]
        public static BindOperation AppDomainAssemblyResolveEvent_DefaultALC()
        {
            var assemblyName = new AssemblyName(SubdirectoryAssemblyName);
            using (var handlers = new Handlers(HandlerReturn.RequestedAssembly))
            {
                Assembly asm = AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);

                return new BindOperation()
                {
                    AssemblyName = assemblyName,
                    AssemblyLoadContext = DefaultALC,
                    Success = true,
                    ResultAssemblyName = asm.GetName(),
                    ResultAssemblyPath = asm.Location,
                    Cached = false,
                    ResolutionAttempts = new List<ResolutionAttempt>()
                    {
                        GetResolutionAttempt(assemblyName, ResolutionStage.FindInLoadContext, AssemblyLoadContext.Default, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.ApplicationAssemblies, AssemblyLoadContext.Default, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.AssemblyLoadContextResolvingEvent, AssemblyLoadContext.Default, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.AppDomainAssemblyResolveEvent, AssemblyLoadContext.Default, ResolutionResult.Success, asm)
                    },
                    AppDomainAssemblyResolveHandlers = handlers.Invocations
                };
            }
        }

        // Exception in AppDomain.AssemblyResolve event handler:
        //   ResolutionAttempted : FindInLoadContext                    (CustomALC)     [AssemblyNotFound]
        //   ResolutionAttempted : AssemblyLoadContextLoad              (CustomALC)     [AssemblyNotFound]
        //   ResolutionAttempted : FindInLoadContext                    (DefaultALC)    [AssemblyNotFound]
        //   ResolutionAttempted : ApplicationAssemblies                (DefaultALC)    [AssemblyNotFound]
        //   ResolutionAttempted : DefaultAssemblyLoadContextFallback   (CustomALC)     [AssemblyNotFound]
        //   ResolutionAttempted : AssemblyLoadContextResolvingEvent    (CustomALC)     [AssemblyNotFound]
        //   ResolutionAttempted : AppDomainAssemblyResolveEvent        (CustomALC)     [Exception]
        [BinderTest(isolate: true)]
        public static BindOperation AppDomainAssemblyResolveEvent_Exception()
        {
            var assemblyName = new AssemblyName(SubdirectoryAssemblyName);
            CustomALC alc = new CustomALC(nameof(AppDomainAssemblyResolveEvent_Exception));
            using (var handlers = new Handlers(HandlerReturn.Exception))
            {
                Assert.Throws<FileLoadException, Exception>(() => alc.LoadFromAssemblyName(assemblyName));

                return new BindOperation()
                {
                    AssemblyName = assemblyName,
                    AssemblyLoadContext = alc.ToString(),
                    Success = false,
                    Cached = false,
                    ResolutionAttempts = new List<ResolutionAttempt>()
                    {
                        GetResolutionAttempt(assemblyName, ResolutionStage.FindInLoadContext, alc, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.AssemblyLoadContextLoad, alc, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.FindInLoadContext, AssemblyLoadContext.Default, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.ApplicationAssemblies, AssemblyLoadContext.Default, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.DefaultAssemblyLoadContextFallback, alc, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.AssemblyLoadContextResolvingEvent, alc, ResolutionResult.AssemblyNotFound),
                        GetResolutionAttempt(assemblyName, ResolutionStage.AppDomainAssemblyResolveEvent, alc, "Exception in handler for AppDomain.AssemblyResolve")
                    },
                    AppDomainAssemblyResolveHandlers = handlers.Invocations
                };
            }
        }

        // Assembly is found in app path when attempted to load through full path:
        //   ResolutionAttempted : FindInLoadContext        (DefaultALC)    [AssemblyNotFound]
        //   ResolutionAttempted : ApplicationAssemblies    (DefaultALC)    [Success]
        [BinderTest(isolate: true)]
        public static BindOperation LoadFromAssemblyPath_FoundInAppPath()
        {
            var assemblyName = new AssemblyName(DependentAssemblyName);
            var assemblyPath = Helpers.GetAssemblyInSubdirectoryPath(assemblyName.Name);

            Assembly asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);

            Assert.AreNotEqual(assemblyPath, asm.Location);
            return new BindOperation()
            {
                AssemblyName = asm.GetName(),
                AssemblyPath = assemblyPath,
                AssemblyLoadContext = DefaultALC,
                RequestingAssembly = CoreLibName,
                RequestingAssemblyLoadContext = DefaultALC,
                Success = true,
                ResultAssemblyName = asm.GetName(),
                ResultAssemblyPath = asm.Location,
                Cached = false,
                ResolutionAttempts = new List<ResolutionAttempt>()
                {
                    GetResolutionAttempt(asm.GetName(), ResolutionStage.FindInLoadContext, AssemblyLoadContext.Default, ResolutionResult.AssemblyNotFound),
                    GetResolutionAttempt(asm.GetName(), ResolutionStage.ApplicationAssemblies, AssemblyLoadContext.Default, ResolutionResult.Success, asm),
                }
            };
        }

        private static ResolutionAttempt GetResolutionAttempt(AssemblyName assemblyName, ResolutionStage stage, AssemblyLoadContext alc, string exceptionMessage)
        {
            return GetResolutionAttempt(assemblyName, stage, alc, ResolutionResult.Exception, null, null, exceptionMessage);
        }

        private static ResolutionAttempt GetResolutionAttempt(AssemblyName assemblyName, ResolutionStage stage, AssemblyLoadContext alc, ResolutionResult result, Assembly resultAssembly = null)
        {
            AssemblyName resultAssemblyName = null;
            string resultAssemblyPath = string.Empty;
            if (resultAssembly != null)
            {
                resultAssemblyName = resultAssembly.GetName();
                resultAssemblyPath = resultAssembly.Location;
            }

            return GetResolutionAttempt(assemblyName, stage, alc, result, resultAssemblyName, resultAssemblyPath);
        }

        private static ResolutionAttempt GetResolutionAttempt(AssemblyName assemblyName, ResolutionStage stage, AssemblyLoadContext alc, ResolutionResult result, AssemblyName resultAssemblyName, string resultAssemblyPath, string exceptionMessage = null)
        {
            var attempt = new ResolutionAttempt()
            {
                AssemblyName = assemblyName,
                Stage = stage,
                AssemblyLoadContext = alc == AssemblyLoadContext.Default ? DefaultALC : alc.ToString(),
                Result = result,
                ResultAssemblyName = resultAssemblyName,
                ResultAssemblyPath = resultAssemblyPath
            };

            if (!string.IsNullOrEmpty(exceptionMessage))
            {
                attempt.ErrorMessage = exceptionMessage;
            }
            else
            {
                switch (result)
                {
                    case ResolutionAttempt.ResolutionResult.AssemblyNotFound:
                        attempt.ErrorMessage = "Could not locate assembly";
                        break;
                    case ResolutionAttempt.ResolutionResult.IncompatibleVersion:
                        attempt.ErrorMessage = $"Requested version {assemblyName.Version} is incompatible with found version {resultAssemblyName.Version}";
                        break;
                    case ResolutionAttempt.ResolutionResult.MismatchedAssemblyName:
                        attempt.ErrorMessage = $"Requested assembly name '{assemblyName.FullName}' does not match found assembly name '{resultAssemblyName.FullName}'";
                        break;
                }
            }

            return attempt;
        }

        private static void LoadSubdirectoryAssembly_InstanceALC()
        {
            string assemblyPath = Helpers.GetAssemblyInSubdirectoryPath(SubdirectoryAssemblyName);
            loadedAssembly = alcInstance.LoadFromAssemblyPath(assemblyPath);
        }
    }
}
