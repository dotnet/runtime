// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace SourceGenerators.Tests
{
    internal static class GeneratorTestHelpers
    {
        /// <summary>
        /// Asserts for structural equality, returning a path to the mismatching data when not equal.
        /// </summary>
        public static void AssertStructurallyEqual<T>(T expected, T actual)
        {
            CheckAreEqualCore(expected, actual, new());
            static void CheckAreEqualCore(object expected, object actual, Stack<string> path)
            {
                if (expected is null || actual is null)
                {
                    if (expected is not null || actual is not null)
                    {
                        FailNotEqual();
                    }

                    return;
                }

                Type type = expected.GetType();
                if (type != actual.GetType())
                {
                    FailNotEqual();
                    return;
                }

                if (expected is IEnumerable leftCollection)
                {
                    if (actual is not IEnumerable rightCollection)
                    {
                        FailNotEqual();
                        return;
                    }

                    object?[] expectedValues = leftCollection.Cast<object?>().ToArray();
                    object?[] actualValues = rightCollection.Cast<object?>().ToArray();

                    for (int i = 0; i < Math.Max(expectedValues.Length, actualValues.Length); i++)
                    {
                        object? expectedElement = i < expectedValues.Length ? expectedValues[i] : "<end of collection>";
                        object? actualElement = i < actualValues.Length ? actualValues[i] : "<end of collection>";

                        path.Push($"[{i}]");
                        CheckAreEqualCore(expectedElement, actualElement, path);
                        path.Pop();
                    }
                }

                if (type.GetProperty("EqualityContract", BindingFlags.Instance | BindingFlags.NonPublic, null, returnType: typeof(Type), types: Array.Empty<Type>(), null) != null)
                {
                    // Type is a C# record, run pointwise equality comparison.
                    foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        path.Push("." + property.Name);
                        CheckAreEqualCore(property.GetValue(expected), property.GetValue(actual), path);
                        path.Pop();
                    }

                    return;
                }

                if (!expected.Equals(actual))
                {
                    FailNotEqual();
                }

                void FailNotEqual() => Assert.Fail($"Value not equal in ${string.Join("", path.Reverse())}: expected {expected}, but was {actual}.");
            }
        }
    }
}
