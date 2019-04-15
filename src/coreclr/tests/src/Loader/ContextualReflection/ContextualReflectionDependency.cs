// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;

namespace ContextualReflectionTest
{
    public interface IProgram
    {
        AssemblyLoadContext alc { get; }
        Assembly alcAssembly { get; }
        Type alcProgramType { get; }
        IProgram alcProgramInstance { get; }
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        void RunTestsIsolated();
    }

    public class ContextualReflectionProxy
    {
        public static AssemblyLoadContext CurrentContextualReflectionContext
        {
            get
            {
#if AssemblyLoadContextContextualReflectionFacade
                return AssemblyLoadContext.CurrentContextualReflectionContext;
#else
                Type t = typeof (AssemblyLoadContext);

                try
                {
                    object result = t.InvokeMember("CurrentContextualReflectionContext",
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty,
                        null,
                        null,
                        new object [] {});

                    return (AssemblyLoadContext) result;
                }
                catch(Exception ex)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                }
                return null;
#endif
            }
        }

        static public IDisposable EnterContextualReflection(AssemblyLoadContext alc)
        {
#if AssemblyLoadContextContextualReflectionFacade
            return alc.EnterContextualReflection();
#else
            Type t = typeof (AssemblyLoadContext);

            try
            {
                object result = t.InvokeMember("EnterContextualReflection",
                    BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Instance,
                    null,
                    alc,
                    new object [] {});

                return (IDisposable) result;
            }
            catch(Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
            return null;
#endif
        }

        static public IDisposable EnterContextualReflection(Assembly activating)
        {
#if AssemblyLoadContextContextualReflectionFacade
            return AssemblyLoadContext.EnterContextualReflection(activating);
#else
            Type t = typeof (AssemblyLoadContext);

            try
            {
                object result = t.InvokeMember("EnterContextualReflection",
                    BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Static,
                    null,
                    null,
                    new object [] {activating});
                return (IDisposable) result;
            }
            catch(Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
            return null;
#endif
        }
    }

    public enum ResolveEvents
    {
        NoEvent,
        ExpectedEvent,
    };

    public class TestResolve
    {
        static public ResolveEvents ResolveEvent { get; set;}

        static public Assembly ResolvingTestDefault(AssemblyLoadContext alc, AssemblyName assemblyName)
        {
            if (assemblyName.Name.Contains("TestDefaultLoad") && (ResolveEvent == ResolveEvents.NoEvent))
            {
                ResolveEvent = ResolveEvents.ExpectedEvent;
            }
            return null;
        }

        static public Assembly ResolvingTestIsolated(AssemblyLoadContext alc, AssemblyName assemblyName)
        {
            if (assemblyName.Name.Contains("TestIsolatedLoad") && (ResolveEvent == ResolveEvents.NoEvent))
            {
                ResolveEvent = ResolveEvents.ExpectedEvent;
            }
            return null;
        }

        static public void Assert(ResolveEvents expected, Action action)
        {
            ResolveEvent = ResolveEvents.NoEvent;
            try
            {
                action();
            }
            catch
            {
            }
            finally
            {
                TestLibrary.Assert.AreEqual(expected, ResolveEvent);
            }
        }
    }
}
