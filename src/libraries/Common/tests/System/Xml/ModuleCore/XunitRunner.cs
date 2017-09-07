// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace OLEDB.Test.ModuleCore
{
    public class XmlInlineDataDiscoverer : IDataDiscoverer
    {
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

        private static Type ToRuntimeType(ITypeInfo typeInfo)
        {
            var reflectionTypeInfo = typeInfo as IReflectionTypeInfo;
            if (reflectionTypeInfo != null)
                return reflectionTypeInfo.Type;

            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == typeInfo.Assembly.Name);
            if (assembly != null)
            {
                return assembly.GetType(typeInfo.Name);
            }

            throw new Exception($"Could not find runtime type `{typeInfo.Name}`");
        }

        private static Type GetDeclaringType(IMethodInfo methodInfo)
        {
            var reflectionMethodInfo = methodInfo as IReflectionMethodInfo;
            if (reflectionMethodInfo != null)
                return reflectionMethodInfo.MethodInfo.DeclaringType;

            return ToRuntimeType(methodInfo.Type);
        }

        public virtual IEnumerable<object[]> GetData(IAttributeInfo dataAttribute, IMethodInfo testMethod)
        {
            string methodName = (string)dataAttribute.GetConstructorArguments().Single();
            Func<CTestModule> moduleGenerator = XmlTestsAttribute.GetGenerator(GetDeclaringType(testMethod), methodName);
            return GenerateTestCases(moduleGenerator);
        }

        public virtual bool SupportsDiscoveryEnumeration(IAttributeInfo dataAttribute, IMethodInfo testMethod) => true;
    }

    [DataDiscoverer("OLEDB.Test.ModuleCore.XmlInlineDataDiscoverer", "ModuleCore")]
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

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            Func<CTestModule> moduleGenerator = GetGenerator(testMethod.DeclaringType, _methodName);
            return XmlInlineDataDiscoverer.GenerateTestCases(moduleGenerator);
        }
    }
}
