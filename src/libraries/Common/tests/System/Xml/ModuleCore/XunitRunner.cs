// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace OLEDB.Test.ModuleCore
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class XmlTestsAttribute : DataAttribute
    {
        private delegate CTestModule ModuleGenerator();

        private string _methodName;

        public XmlTestsAttribute(string methodName)
        {
            _methodName = methodName;
        }

        public static Func<CTestModule> GetGenerator(Type type, string methodName)
        {
            ModuleGenerator moduleGenerator = (ModuleGenerator)type.GetMethod(methodName).CreateDelegate(typeof(ModuleGenerator));
            return new Func<CTestModule>(moduleGenerator);
        }

        public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(MethodInfo testMethod, DisposalTracker disposalTracker)
        {
            Func<CTestModule> moduleGenerator = GetGenerator(testMethod.DeclaringType, _methodName);
            var testCases = new List<ITheoryDataRow>();
            foreach (object[] testCase in GenerateTestCases(moduleGenerator))
            {
                testCases.Add(new TheoryDataRow(testCase));
            }
            return new ValueTask<IReadOnlyCollection<ITheoryDataRow>>(testCases);
        }

        public static IEnumerable<object[]> GenerateTestCases(Func<CTestModule> moduleGenerator)
        {
            CModInfo.CommandLine = "";
            foreach (object[] testCase in GenerateTestCasesForModule(moduleGenerator()))
            {
                yield return testCase;
            }

            CModInfo.CommandLine = "/async";
            foreach (object[] testCase in GenerateTestCasesForModule(moduleGenerator()))
            {
                yield return testCase;
            }
        }

        private static IEnumerable<object[]> GenerateTestCasesForModule(CTestModule module)
        {
            foreach (OLEDB.Test.ModuleCore.XunitTestCase testCase in module.TestCases())
            {
                yield return new object[] { testCase };
            }
        }

        public override bool SupportsDiscoveryEnumeration() => true;
    }
}
