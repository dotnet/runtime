// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using TestLibrary;

namespace BinderTracingTests
{
    partial class BinderTracingTest
    {
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

                Assert.AreEqual(1, handlers.Invocations.Count);
                Assert.AreEqual(0, handlers.Binds.Count);
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

                Assert.AreEqual(1, handlers.Invocations.Count);
                Assert.AreEqual(1, handlers.Binds.Count);
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

        [BinderTest]
        public static BindOperation AssemblyLoadContextResolving_NameMismatch()
        {
            var assemblyName = new AssemblyName(SubdirectoryAssemblyName);
            CustomALC alc = new CustomALC(nameof(AssemblyLoadContextResolving_NameMismatch));
            using (var handlers = new Handlers(HandlerReturn.NameMismatch, alc))
            {
                Assert.Throws<FileLoadException>(() => alc.LoadFromAssemblyName(assemblyName));

                Assert.AreEqual(1, handlers.Invocations.Count);
                Assert.AreEqual(1, handlers.Binds.Count);
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
            CustomALC alc = new CustomALC(nameof(AssemblyLoadContextResolving_NameMismatch));
            using (var handlerNull = new Handlers(HandlerReturn.Null, alc))
            using (var handlerLoad = new Handlers(HandlerReturn.RequestedAssembly, alc))
            {
                Assembly asm = alc.LoadFromAssemblyName(assemblyName);

                Assert.AreEqual(1, handlerNull.Invocations.Count);
                Assert.AreEqual(0, handlerNull.Binds.Count);
                Assert.AreEqual(1, handlerLoad.Invocations.Count);
                Assert.AreEqual(1, handlerLoad.Binds.Count);
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

                Assert.AreEqual(1, handlers.Invocations.Count);
                Assert.AreEqual(0, handlers.Binds.Count);
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

                Assert.AreEqual(1, handlers.Invocations.Count);
                Assert.AreEqual(1, handlers.Binds.Count);
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
        public static BindOperation AppDomainAssemblyResolve_NameMismatch()
        {
            var assemblyName = new AssemblyName(SubdirectoryAssemblyName);
            CustomALC alc = new CustomALC(nameof(AppDomainAssemblyResolve_NameMismatch));
            using (var handlers = new Handlers(HandlerReturn.NameMismatch))
            {
                // Result of AssemblyResolve event does not get checked for name mismatch
                Assembly asm = alc.LoadFromAssemblyName(assemblyName);

                Assert.AreEqual(1, handlers.Invocations.Count);
                Assert.AreEqual(1, handlers.Binds.Count);
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

                Assert.AreEqual(1, handlerNull.Invocations.Count);
                Assert.AreEqual(0, handlerNull.Binds.Count);
                Assert.AreEqual(1, handlerLoad.Invocations.Count);
                Assert.AreEqual(1, handlerLoad.Binds.Count);
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

        private enum HandlerReturn
        {
            Null,
            RequestedAssembly,
            NameMismatch
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

                string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string fileName = handlerReturn == HandlerReturn.RequestedAssembly ? $"{assemblyName.Name}.dll" : $"{assemblyName.Name}Mismatch.dll";
                string assemblyPath = Path.Combine(appPath, "DependentAssemblies", fileName);

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
