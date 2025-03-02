// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Test76531
{
    internal class Test
    {
        private static Dependency.DependencyClass? value;

        static Test()
        {
            value = new Dependency.DependencyClass();
        }
    }

    public class MyObject : IDynamicInterfaceCastable
    {
        public RuntimeTypeHandle GetInterfaceImplementation(RuntimeTypeHandle interfaceType)
            => throw new Exception("My exception");

        public bool IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
            => true;

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void CallMe(MyObject o, Action<IMyInterface> d) => d((IMyInterface)o);
    }

    public interface IMyInterface
    {
        void M();
    }

    public class Program
    {
        [Fact]
        public static void TestExternalMethodFixupWorker()
        {
            File.Delete(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dependencytodelete.dll"));
            Assert.Throws<TargetInvocationException>(() =>
            {
                typeof(TailCallInvoker).GetMethod("Test")!.Invoke(null, null);
            });
        }

        [Fact]
        public static void TestPreStubWorker()
        {
            File.Delete(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dependencytodelete.dll"));
            if (TestLibrary.Utilities.IsMonoRuntime)
            {
                Assert.Throws<TypeLoadException>(() =>
                {
                    Test test = new ();
                });
            }
            else
            {
                // The exception is of different type with issue #76531
                Assert.Throws<TypeInitializationException>(() =>
                {
                    Test test = new ();
                });
            }
        }

        [Fact]
        public static void TestVSD_ResolveWorker()
        {
            Assert.Throws<TargetInvocationException>(() =>
            {
                var d = typeof(IMyInterface).GetMethod("M")!.CreateDelegate<Action<IMyInterface>>();
                typeof(MyObject).GetMethod("CallMe")!.Invoke(null, [new MyObject(), d]);
            });
        }
    }
}
