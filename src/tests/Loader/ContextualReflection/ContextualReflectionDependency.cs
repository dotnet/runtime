// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
        void RunTestsIsolated();
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
