// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

// Usage: [assembly: TestCaseOrderer("Microsoft.DotNet.XUnitExtensions.BenchmarkFilter", "System.Drawing.Common.PerformanceTests")]

namespace Microsoft.DotNet.XUnitExtensions
{
    public class BenchmarkFilter : ITestCaseOrderer
    {
        private static MethodInfo LookupConditionalMethod(Type t, string name)
        {
            if (t == null || name == null)
                return null;

            TypeInfo ti = t.GetTypeInfo();

            MethodInfo mi = ti.GetDeclaredMethod(name);
            if (mi != null && mi.IsStatic && mi.GetParameters().Length == 0 && mi.ReturnType == typeof(bool))
                return mi;

            PropertyInfo pi = ti.GetDeclaredProperty(name);
            if (pi != null && pi.PropertyType == typeof(bool) && pi.GetMethod != null && pi.GetMethod.IsStatic && pi.GetMethod.GetParameters().Length == 0)
                return pi.GetMethod;

            return LookupConditionalMethod(ti.BaseType, name);
        }

        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
            where TTestCase : ITestCase
        {
            foreach (TTestCase testCase in testCases)
            {
                bool success = true;

                IEnumerable<IAttributeInfo> attributes = testCase.TestMethod.Method.GetCustomAttributes(typeof(ConditionalBenchmarkAttribute).AssemblyQualifiedName);
                foreach (IAttributeInfo attributeInfo in attributes)
                {
                    Type calleeType = attributeInfo.GetNamedArgument<Type>("CalleeType");
                    string[] conditionMemberNames = attributeInfo.GetNamedArgument<string[]>("ConditionMemberNames");
                    foreach (string conditionMemberName in conditionMemberNames)
                    {
                        MethodInfo conditionMethodInfo = LookupConditionalMethod(calleeType, conditionMemberName);
                        if (conditionMethodInfo == null)
                            throw new Exception($"Conditional method with name '{conditionMemberName}' not found");

                        if (!(bool)conditionMethodInfo.Invoke(null, null))
                            success = false;
                    }
                }

                if (success)
                    yield return testCase;
            }
        }
    }

    public class ConditionalBenchmarkAttribute : Attribute
    {
        public ConditionalBenchmarkAttribute(Type calleeType, params string[] conditionMemberNames)
        {
            CalleeType = calleeType;
            ConditionMemberNames = conditionMemberNames;
        }

        public Type CalleeType { get; }

        public string[] ConditionMemberNames { get; }
    }
}
