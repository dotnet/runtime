// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Reflection;
using System.IO;
using System.Runtime.CompilerServices;
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

    public class Program
    {
	[MethodImpl(MethodImplOptions.NoInlining)]
        static void TestMethod()
        {
            try
            {
                Test test = new ();
            }
            catch (TypeInitializationException)
            {
                // This catch fails with issue #76531
            }
        }

        [Fact]
        public static void TestEntryPoint()
        {
            File.Delete(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dependencytodelete.dll"));
            TestMethod();
        }
    }
}
