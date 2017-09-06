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

        public virtual IEnumerable<object[]> GetData(IAttributeInfo dataAttribute, IMethodInfo testMethod)
        {
            string methodName = (string)dataAttribute.GetConstructorArguments().Single();
            Func<CTestModule> moduleGenerator = XmlTestsAttribute.GetGenerator(testMethod.ToRuntimeMethod().DeclaringType, methodName);
            return GenerateTestCases(moduleGenerator);
        }

        public virtual bool SupportsDiscoveryEnumeration(IAttributeInfo dataAttribute, IMethodInfo testMethod)
        {
            return true;
        }
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
