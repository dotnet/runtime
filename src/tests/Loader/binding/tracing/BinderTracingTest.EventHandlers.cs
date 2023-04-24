// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using Xunit;

namespace BinderTracingTests
{
    public class BinderTestException : Exception
    {
        public BinderTestException(string message)
                : base(message)
        {
        }

    }
    partial class BinderTracingTest
    {
        private const string AssemblyLoadFromHandlerName = "LoadFromResolveHandler";

        [BinderTest]
        public static BindOperation AssemblyLoadContextResolving_ReturnNull()
        {
            var assemblyName = new AssemblyName(SubdirectoryAssemblyName);
            using (var handlers = new Handlers(HandlerReturn.Null, AssemblyLoadContext.Default))
            {
                try
                {
                    Assembly.Load(assemblyName);
                }
                catch { }

                Assert.Equal(1, handlers.Invocations.Count);
                Assert.Equal(0, handlers.Binds.Count);
                return new BindOperation()
                {
                    AssemblyName = assemblyName,
                    AssemblyLoadContext = DefaultALC,
                    RequestingAssembly = Assembly.GetExecutingAssembly().GetName(),
                    RequestingAssemblyLoadContext = DefaultALC,
                    Success = false,
                    Cached = false,
                    AssemblyLoadContextResolvingHandlers = handlers.Invocations,
                    NestedBinds = handlers.Binds
                };
            }
        }

        [BinderTest]
        public static BindOperation AssemblyLoadContextResolving_LoadAssembly()
        {
            var assemblyName = new AssemblyName(SubdirectoryAssemblyName);
            CustomALC alc = new CustomALC(nameof(AssemblyLoadContextResolving_LoadAssembly));
            using (var handlers = new Handlers(HandlerReturn.RequestedAssembly, alc))
            {
                Assembly asm = alc.LoadFromAssemblyName(assemblyName);

                Assert.Equal(1, handlers.Invocations.Count);
                Assert.Equal(1, handlers.Binds.Count);
                return new BindOperation()
                {
                    AssemblyName = assemblyName,
                    AssemblyLoadContext = alc.ToString(),
                    Success = true,
                    ResultAssemblyName = asm.GetName(),
                    ResultAssemblyPath = asm.Location,
                    Cached = false,
                    AssemblyLoadContextResolvingHandlers = handlers.Invocations,
                    NestedBinds = handlers.Binds
                };
            }
        }

        [BinderTest(additionalLoadsToTrack: new string[] { SubdirectoryAssemblyName + "Mismatch" })]
        public static BindOperation AssemblyLoadContextResolving_NameMismatch()
        {
            var assemblyName = new AssemblyName(SubdirectoryAssemblyName);
            CustomALC alc = new CustomALC(nameof(AssemblyLoadContextResolving_NameMismatch));
            using (var handlers = new Handlers(HandlerReturn.NameMismatch, alc))
            {
                Assert.Throws<FileLoadException>(() => alc.LoadFromAssemblyName(assemblyName));

                Assert.Equal(1, handlers.Invocations.Count);
                Assert.Equal(1, handlers.Binds.Count);
                return new BindOperation()
                {
                    AssemblyName = assemblyName,
                    AssemblyLoadContext = alc.ToString(),
                    Success = false,
                    Cached = false,
                    AssemblyLoadContextResolvingHandlers = handlers.Invocations,
                    NestedBinds = handlers.Binds
                };
            }
        }

        [BinderTest]
        public static BindOperation AssemblyLoadContextResolving_MultipleHandlers()
        {
            var assemblyName = new AssemblyName(SubdirectoryAssemblyName);
            CustomALC alc = new CustomALC(nameof(AssemblyLoadContextResolving_MultipleHandlers));
            using (var handlerNull = new Handlers(HandlerReturn.Null, alc))
            using (var handlerLoad = new Handlers(HandlerReturn.RequestedAssembly, alc))
            {
                Assembly asm = alc.LoadFromAssemblyName(assemblyName);

                Assert.Equal(1, handlerNull.Invocations.Count);
                Assert.Equal(0, handlerNull.Binds.Count);
                Assert.Equal(1, handlerLoad.Invocations.Count);
                Assert.Equal(1, handlerLoad.Binds.Count);
                return new BindOperation()
                {
                    AssemblyName = assemblyName,
                    AssemblyLoadContext = alc.ToString(),
                    Success = true,
                    ResultAssemblyName = asm.GetName(),
                    ResultAssemblyPath = asm.Location,
                    Cached = false,
                    AssemblyLoadContextResolvingHandlers = handlerNull.Invocations.Concat(handlerLoad.Invocations).ToList(),
                    NestedBinds = handlerNull.Binds.Concat(handlerLoad.Binds).ToList()
                };
            }
        }

        [BinderTest]
        public static BindOperation AppDomainAssemblyResolve_ReturnNull()
        {
            var assemblyName = new AssemblyName(SubdirectoryAssemblyName);
            using (var handlers = new Handlers(HandlerReturn.Null))
            {
                try
                {
                    Assembly.Load(assemblyName);
                }
                catch { }

                Assert.Equal(1, handlers.Invocations.Count);
                Assert.Equal(0, handlers.Binds.Count);
                return new BindOperation()
                {
                    AssemblyName = assemblyName,
                    AssemblyLoadContext = DefaultALC,
                    RequestingAssembly = Assembly.GetExecutingAssembly().GetName(),
                    RequestingAssemblyLoadContext = DefaultALC,
                    Success = false,
                    Cached = false,
                    AppDomainAssemblyResolveHandlers = handlers.Invocations,
                    NestedBinds = handlers.Binds
                };
            }
        }

        [BinderTest]
        public static BindOperation AppDomainAssemblyResolve_LoadAssembly()
        {
            var assemblyName = new AssemblyName(SubdirectoryAssemblyName);
            CustomALC alc = new CustomALC(nameof(AppDomainAssemblyResolve_LoadAssembly));
            using (var handlers = new Handlers(HandlerReturn.RequestedAssembly))
            {
                Assembly asm = alc.LoadFromAssemblyName(assemblyName);

                Assert.Equal(1, handlers.Invocations.Count);
                Assert.Equal(1, handlers.Binds.Count);
                return new BindOperation()
                {
                    AssemblyName = assemblyName,
                    AssemblyLoadContext = alc.ToString(),
                    Success = true,
                    ResultAssemblyName = asm.GetName(),
                    ResultAssemblyPath = asm.Location,
                    Cached = false,
                    AppDomainAssemblyResolveHandlers = handlers.Invocations,
                    NestedBinds = handlers.Binds
                };
            }
        }

        [BinderTest(additionalLoadsToTrack: new string[] { SubdirectoryAssemblyName + "Mismatch" })]
        public static BindOperation AppDomainAssemblyResolve_NameMismatch()
        {
            var assemblyName = new AssemblyName(SubdirectoryAssemblyName);
            CustomALC alc = new CustomALC(nameof(AppDomainAssemblyResolve_NameMismatch));
            using (var handlers = new Handlers(HandlerReturn.NameMismatch))
            {
                // Result of AssemblyResolve event does not get checked for name mismatch
                Assembly asm = alc.LoadFromAssemblyName(assemblyName);

                Assert.Equal(1, handlers.Invocations.Count);
                Assert.Equal(1, handlers.Binds.Count);
                return new BindOperation()
                {
                    AssemblyName = assemblyName,
                    AssemblyLoadContext = alc.ToString(),
                    Success = true,
                    ResultAssemblyName = asm.GetName(),
                    ResultAssemblyPath = asm.Location,
                    Cached = false,
                    AppDomainAssemblyResolveHandlers = handlers.Invocations,
                    NestedBinds = handlers.Binds
                };
            }
        }

        [BinderTest]
        public static BindOperation AppDomainAssemblyResolve_MultipleHandlers()
        {
            var assemblyName = new AssemblyName(SubdirectoryAssemblyName);
            CustomALC alc = new CustomALC(nameof(AppDomainAssemblyResolve_LoadAssembly));
            using (var handlerNull = new Handlers(HandlerReturn.Null))
            using (var handlerLoad = new Handlers(HandlerReturn.RequestedAssembly))
            {
                Assembly asm = alc.LoadFromAssemblyName(assemblyName);

                Assert.Equal(1, handlerNull.Invocations.Count);
                Assert.Equal(0, handlerNull.Binds.Count);
                Assert.Equal(1, handlerLoad.Invocations.Count);
                Assert.Equal(1, handlerLoad.Binds.Count);
                return new BindOperation()
                {
                    AssemblyName = assemblyName,
                    AssemblyLoadContext = alc.ToString(),
                    Success = true,
                    ResultAssemblyName = asm.GetName(),
                    ResultAssemblyPath = asm.Location,
                    Cached = false,
                    AppDomainAssemblyResolveHandlers = handlerNull.Invocations.Concat(handlerLoad.Invocations).ToList(),
                    NestedBinds = handlerNull.Binds.Concat(handlerLoad.Binds).ToList()
                };
            }
        }

        [BinderTest(isolate: true, additionalLoadsToTrack: new string[] { "AssemblyToLoadDependency" })]
        public static BindOperation AssemblyLoadFromResolveHandler_LoadDependency()
        {
            string assemblyPath = Helpers.GetAssemblyInSubdirectoryPath(SubdirectoryAssemblyName);
            Assembly asm = Assembly.LoadFrom(assemblyPath);
            Type t = asm.GetType(DependentAssemblyTypeName);
            MethodInfo method = t.GetMethod("UseDependentAssembly", BindingFlags.Public | BindingFlags.Static);
            Assembly asmDependency = (Assembly)method.Invoke(null, new object[0]);

            return new BindOperation()
            {
                AssemblyName = asmDependency.GetName(),
                AssemblyLoadContext = DefaultALC,
                RequestingAssembly = asm.GetName(),
                RequestingAssemblyLoadContext = DefaultALC,
                Success = true,
                ResultAssemblyName = asmDependency.GetName(),
                ResultAssemblyPath = asmDependency.Location,
                Cached = false,
                AppDomainAssemblyResolveHandlers = new List<HandlerInvocation>()
                {
                    new HandlerInvocation()
                    {
                        AssemblyName = asmDependency.GetName(),
                        HandlerName = AssemblyLoadFromHandlerName,
                        ResultAssemblyName = asmDependency.GetName(),
                        ResultAssemblyPath = asmDependency.Location
                    }
                },
                AssemblyLoadFromHandler = new LoadFromHandlerInvocation()
                {
                    AssemblyName = asmDependency.GetName(),
                    IsTrackedLoad = true,
                    RequestingAssemblyPath = asm.Location,
                    ComputedRequestedAssemblyPath = asmDependency.Location
                }
            };
        }

        [BinderTest(isolate: true,
            additionalLoadsToTrack: new string[] { "AssemblyToLoadDependency" },
            activeIssue: "https://github.com/dotnet/runtime/issues/68521")] // Emit-based Invoke causes an extra load.
        public static BindOperation AssemblyLoadFromResolveHandler_MissingDependency()
        {
            string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string assemblyPath = Path.Combine(appPath, $"{DependentAssemblyName}.dll");
            Assembly asm = Assembly.LoadFrom(assemblyPath);
            Type t = asm.GetType(DependentAssemblyTypeName);
            MethodInfo method = t.GetMethod("UseDependentAssembly", BindingFlags.Public | BindingFlags.Static);
            AssertExtensions.ThrowsWithInnerException<TargetInvocationException, FileNotFoundException>(() => method.Invoke(null, new object[0]));

            var assemblyName = new AssemblyName(asm.FullName);
            assemblyName.Name = "AssemblyToLoadDependency";
            var expectedPath = Path.Combine(appPath, $"{assemblyName.Name}.dll");
            return new BindOperation()
            {
                AssemblyName = assemblyName,
                AssemblyLoadContext = DefaultALC,
                RequestingAssembly = asm.GetName(),
                RequestingAssemblyLoadContext = DefaultALC,
                Success = false,
                Cached = false,
                AppDomainAssemblyResolveHandlers = new List<HandlerInvocation>()
                {
                    new HandlerInvocation()
                    {
                        AssemblyName = assemblyName,
                        HandlerName = AssemblyLoadFromHandlerName,
                    }
                },
                AssemblyLoadFromHandler = new LoadFromHandlerInvocation()
                {
                    AssemblyName = assemblyName,
                    IsTrackedLoad = true,
                    RequestingAssemblyPath = asm.Location,
                    ComputedRequestedAssemblyPath = expectedPath
                }
            };
        }

        [BinderTest(isolate: true)]
        public static BindOperation AssemblyLoadFromResolveHandler_NotTracked()
        {
            string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string assemblyPath = Path.Combine(appPath, $"{DependentAssemblyName}.dll");
            Assembly.LoadFrom(assemblyPath);

            var assemblyName = new AssemblyName(SubdirectoryAssemblyName);
            Assert.Throws<FileNotFoundException>(() => Assembly.Load(SubdirectoryAssemblyName));

            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            return new BindOperation()
            {
                AssemblyName = assemblyName,
                AssemblyLoadContext = DefaultALC,
                RequestingAssembly = executingAssembly.GetName(),
                RequestingAssemblyLoadContext = DefaultALC,
                Success = false,
                Cached = false,
                AppDomainAssemblyResolveHandlers = new List<HandlerInvocation>()
                {
                    new HandlerInvocation()
                    {
                        AssemblyName = assemblyName,
                        HandlerName = AssemblyLoadFromHandlerName,
                    }
                },
                AssemblyLoadFromHandler = new LoadFromHandlerInvocation()
                {
                    AssemblyName = assemblyName,
                    IsTrackedLoad = false,
                    RequestingAssemblyPath = executingAssembly.Location
                }
            };
        }

        private enum HandlerReturn
        {
            Null,
            RequestedAssembly,
            NameMismatch,
            Exception
        }

        private class Handlers : IDisposable
        {
            private HandlerReturn handlerReturn;
            private AssemblyLoadContext alc;

            internal readonly List<HandlerInvocation> Invocations = new List<HandlerInvocation>();
            internal readonly List<BindOperation> Binds = new List<BindOperation>();

            public Handlers(HandlerReturn handlerReturn)
            {
                this.handlerReturn = handlerReturn;
                AppDomain.CurrentDomain.AssemblyResolve += OnAppDomainAssemblyResolve;
            }

            public Handlers(HandlerReturn handlerReturn, AssemblyLoadContext alc)
            {
                this.handlerReturn = handlerReturn;
                this.alc = alc;
                this.alc.Resolving += OnAssemblyLoadContextResolving;
            }

            public void Dispose()
            {
                AppDomain.CurrentDomain.AssemblyResolve -= OnAppDomainAssemblyResolve;
                if (alc != null)
                    alc.Resolving -= OnAssemblyLoadContextResolving;
            }

            private Assembly OnAssemblyLoadContextResolving(AssemblyLoadContext context, AssemblyName assemblyName)
            {
                if (handlerReturn == HandlerReturn.Exception)
                    throw new BinderTestException("Exception in handler for AssemblyLoadContext.Resolving");

                Assembly asm = ResolveAssembly(context, assemblyName);
                var invocation = new HandlerInvocation()
                {
                    AssemblyName = assemblyName,
                    HandlerName = nameof(OnAssemblyLoadContextResolving),
                    AssemblyLoadContext = context == AssemblyLoadContext.Default ? context.Name : context.ToString(),
                };
                if (asm != null)
                {
                    invocation.ResultAssemblyName = asm.GetName();
                    invocation.ResultAssemblyPath = asm.Location;
                }

                Invocations.Add(invocation);
                return asm;
            }

            private Assembly OnAppDomainAssemblyResolve(object sender, ResolveEventArgs args)
            {
                if (handlerReturn == HandlerReturn.Exception)
                    throw new BinderTestException("Exception in handler for AppDomain.AssemblyResolve");

                var assemblyName = new AssemblyName(args.Name);
                var customContext = new CustomALC(nameof(OnAppDomainAssemblyResolve));
                Assembly asm = ResolveAssembly(customContext, assemblyName);
                var invocation = new HandlerInvocation()
                {
                    AssemblyName = assemblyName,
                    HandlerName = nameof(OnAppDomainAssemblyResolve),
                };
                if (asm != null)
                {
                    invocation.ResultAssemblyName = asm.GetName();
                    invocation.ResultAssemblyPath = asm.Location;
                }

                Invocations.Add(invocation);
                return asm;
            }

            private Assembly ResolveAssembly(AssemblyLoadContext context, AssemblyName assemblyName)
            {
                if (handlerReturn == HandlerReturn.Null)
                    return null;

                string name = handlerReturn == HandlerReturn.RequestedAssembly ? assemblyName.Name : $"{assemblyName.Name}Mismatch";
                string assemblyPath = Helpers.GetAssemblyInSubdirectoryPath(name);

                if (!File.Exists(assemblyPath))
                    return null;

                Assembly asm = context.LoadFromAssemblyPath(assemblyPath);
                var bind = new BindOperation()
                {
                    AssemblyName = asm.GetName(),
                    AssemblyPath = assemblyPath,
                    AssemblyLoadContext = context == AssemblyLoadContext.Default ? context.Name : context.ToString(),
                    RequestingAssembly = CoreLibName,
                    RequestingAssemblyLoadContext = DefaultALC,
                    Success = true,
                    ResultAssemblyName = asm.GetName(),
                    ResultAssemblyPath = asm.Location,
                    Cached = false
                };
                Binds.Add(bind);
                return asm;
            }
        }
    }
}
