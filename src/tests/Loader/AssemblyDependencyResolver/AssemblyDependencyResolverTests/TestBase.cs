// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AssemblyDependencyResolverTests
{
    public class TestBase
    {
        protected string TestBasePath { get; private set; }
        protected string BinaryBasePath { get; private set; }
        protected string CoreRoot { get; private set; }

        protected virtual void Initialize()
        {
        }

        protected virtual void Cleanup()
        {
        }

        public static int RunTests(params Type[] testTypes)
        {
            int result = 100;
            foreach (Type testType in testTypes)
            {
                int testResult = RunTestsForType(testType);
                if (testResult != 100)
                {
                    result = testResult;
                }
            }

            return result;
        }

        private static int RunTestsForType(Type testType)
        {
            string testBasePath = Path.GetDirectoryName(testType.Assembly.Location);

            TestBase runner = (TestBase)Activator.CreateInstance(testType);
            runner.TestBasePath = testBasePath;
            runner.BinaryBasePath = Path.GetDirectoryName(testBasePath);
            runner.CoreRoot = GetCoreRoot();

            try
            {
                runner.Initialize();

                runner.RunTestsForInstance(runner);
                return runner._retValue;
            }
            finally
            {
                runner.Cleanup();
            }
        }

        private int _retValue = 100;
        private void RunSingleTest(Action test, string testName = null)
        {
            testName = testName ?? test.Method.Name;

            try
            {
                Console.WriteLine($"{testName} Start");
                test();
                Console.WriteLine($"{testName} PASSED.");
            }
            catch (Exception exe)
            {
                Console.WriteLine($"{testName} FAILED:");
                Console.WriteLine(exe.ToString());
                _retValue = -1;
            }
        }

        private void RunTestsForInstance(object testClass)
        {
            foreach (MethodInfo m in testClass.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(m => m.Name.StartsWith("Test") && m.GetParameters().Length == 0))
            {
                RunSingleTest(() => m.Invoke(testClass, new object[0]), m.Name);
            }
        }

        private static string GetCoreRoot()
        {
            string value = Environment.GetEnvironmentVariable("CORE_ROOT");
            if (value == null)
            {
                value = Directory.GetCurrentDirectory();
            }

            return value;
        }
    }
}
