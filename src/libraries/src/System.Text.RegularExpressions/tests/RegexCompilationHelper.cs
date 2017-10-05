// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexCompilationHelper
    {
        /// <summary>
        /// Adds RegexOptions.Compiled to the RegexOptions of each item in a given array of test data.
        /// </summary>
        /// <param name="regexOptionsArrayIndex">The index in the object array of the CompilationOptions enum.</param>
        /// <returns></returns>
        public static IEnumerable<object[]> TransformRegexOptions(string testDataMethodName, int regexOptionsArrayIndex)
        {
            // On Uap or NetNative the compiled feature isn't currently enabled,
            // therefore we don't need the additional test data.
            if (PlatformDetection.IsUap)
            {
                return Enumerable.Empty<object[]>();
            }

            IEnumerable<Type> types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Namespace == typeof(RegexCompilationHelper).Namespace);
            foreach (Type type in types)
            {
                IEnumerable<object[]> result = InvokeTransform(type, testDataMethodName, regexOptionsArrayIndex);
                if (result != null)
                {
                    return result;
                }
            }
            
            throw new Exception($"Test method '{testDataMethodName}' not found");
        }

        private static IEnumerable<object[]> InvokeTransform(Type type, string methodName, int regexOptionsArrayIndex)
        {
            MethodInfo methodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            var data = methodInfo?.Invoke(null, null) as IEnumerable<object[]>;

            return data?.Select(obj =>
            {
                obj[regexOptionsArrayIndex] = (RegexOptions)obj[regexOptionsArrayIndex] | RegexOptions.Compiled;
                return obj;
            });
        }
    }
}
