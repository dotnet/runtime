// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;
using TestLibrary;

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
        [ActiveIssue("Assembly.GetExecutingAssembly().Location returns NULL on WASM", TestPlatforms.Browser)]
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void TestExternalMethodFixupWorker(bool useMethodInvoker)
        {
            File.Delete(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dependencytodelete.dll"));
            MethodInfo method = typeof(TailCallInvoker).GetMethod(nameof(TailCallInvoker.Test))!;
            if (useMethodInvoker)
            {
                Assert.Throws<FileNotFoundException>(() => MethodInvoker.Create(method).Invoke(null));
            }
            else
            {
                TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, null));
                Assert.IsType<FileNotFoundException>(exception.InnerException);
            }
        }

        [ActiveIssue("Assembly.GetExecutingAssembly().Location returns NULL on WASM", TestPlatforms.Browser)]
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

        [ActiveIssue("Assembly.GetExecutingAssembly().Location returns NULL on WASM", TestPlatforms.Browser)]
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
