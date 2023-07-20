// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json
{
    internal static partial class JsonTestHelper
    {
#if NETCOREAPP
        public const string DoubleFormatString = null;
        public const string SingleFormatString = null;
#else
        public const string DoubleFormatString = "G17";
        public const string SingleFormatString = "G9";
#endif

#if NETCOREAPP
        public static Half NextHalf(Random random)
        {
            double mantissa = (random.NextDouble() * 2.0) - 1.0;
            double exponent = Math.Pow(2.0, random.Next(-15, 16));
            Half value = (Half)(mantissa * exponent);
            return value;
        }
#endif

        public static float NextFloat(Random random)
        {
            double mantissa = (random.NextDouble() * 2.0) - 1.0;
            double exponent = Math.Pow(2.0, random.Next(-126, 128));
            float value = (float)(mantissa * exponent);
            return value;
        }

        public static double NextDouble(Random random, double minValue, double maxValue)
        {
            double value = random.NextDouble() * (maxValue - minValue) + minValue;
            return value;
        }

        public static decimal NextDecimal(Random random, double minValue, double maxValue)
        {
            double value = random.NextDouble() * (maxValue - minValue) + minValue;
            return (decimal)value;
        }

        public static void AssertJsonEqual(string expected, string actual)
        {
            using JsonDocument expectedDom = JsonDocument.Parse(expected);
            using JsonDocument actualDom = JsonDocument.Parse(actual);
            AssertJsonEqual(expectedDom.RootElement, actualDom.RootElement);
        }

        public static void AssertJsonEqual(JsonElement expected, JsonElement actual)
        {
            AssertJsonEqualCore(expected, actual, new());
        }

        private static void AssertJsonEqualCore(JsonElement expected, JsonElement actual, Stack<object> path)
        {
            JsonValueKind valueKind = expected.ValueKind;
            AssertTrue(passCondition: valueKind == actual.ValueKind);

            switch (valueKind)
            {
                case JsonValueKind.Object:
                    var expectedProperties = new List<string>();
                    foreach (JsonProperty property in expected.EnumerateObject())
                    {
                        expectedProperties.Add(property.Name);
                    }

                    var actualProperties = new List<string>();
                    foreach (JsonProperty property in actual.EnumerateObject())
                    {
                        actualProperties.Add(property.Name);
                    }

                    foreach (var property in expectedProperties.Except(actualProperties))
                    {
                        AssertTrue(passCondition: false, $"Property \"{property}\" missing from actual object.");
                    }

                    foreach (var property in actualProperties.Except(expectedProperties))
                    {
                        AssertTrue(passCondition: false, $"Actual object defines additional property \"{property}\".");
                    }

                    foreach (string name in expectedProperties)
                    {
                        path.Push(name);
                        AssertJsonEqualCore(expected.GetProperty(name), actual.GetProperty(name), path);
                        path.Pop();
                    }
                    break;
                case JsonValueKind.Array:
                    JsonElement.ArrayEnumerator expectedEnumerator = expected.EnumerateArray();
                    JsonElement.ArrayEnumerator actualEnumerator = actual.EnumerateArray();

                    int i = 0;
                    while (expectedEnumerator.MoveNext())
                    {
                        AssertTrue(passCondition: actualEnumerator.MoveNext(), "Actual array contains fewer elements.");
                        path.Push(i++);
                        AssertJsonEqualCore(expectedEnumerator.Current, actualEnumerator.Current, path);
                        path.Pop();
                    }

                    AssertTrue(passCondition: !actualEnumerator.MoveNext(), "Actual array contains additional elements.");
                    break;
                case JsonValueKind.String:
                    AssertTrue(passCondition: expected.GetString() == actual.GetString());
                    break;
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                    AssertTrue(passCondition: expected.GetRawText() == actual.GetRawText());
                    break;
                default:
                    Debug.Fail($"Unexpected JsonValueKind: JsonValueKind.{valueKind}.");
                    break;
            }

            void AssertTrue(bool passCondition, string? message = null)
            {
                if (!passCondition)
                {
                    message ??= "Expected JSON does not match actual value";
                    Assert.Fail($"{message}\nExpected JSON: {expected}\n  Actual JSON: {actual}\n  in JsonPath: {BuildJsonPath(path)}");
                }

                // TODO replace with JsonPath implementation for JsonElement
                // cf. https://github.com/dotnet/runtime/issues/31068
                static string BuildJsonPath(Stack<object> path)
                {
                    var sb = new StringBuilder("$");
                    foreach (object node in path.Reverse())
                    {
                        string pathNode = node is string propertyName
                            ? "." + propertyName
                            : $"[{(int)node}]";

                        sb.Append(pathNode);
                    }
                    return sb.ToString();
                }
            }
        }

        public static void AssertOptionsEqual(JsonSerializerOptions expected, JsonSerializerOptions actual)
        {
            foreach (PropertyInfo property in typeof(JsonSerializerOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Type propertyType = property.PropertyType;

                if (property.Name == nameof(JsonSerializerOptions.IsReadOnly))
                {
                    continue; // readonly-ness is not a structural property of JsonSerializerOptions.
                }
                else if (propertyType == typeof(IList<JsonConverter>))
                {
                    var expectedConverters = (IList<JsonConverter>)property.GetValue(expected);
                    var actualConverters = (IList<JsonConverter>)property.GetValue(actual);
                    Assert.Equal(expectedConverters.Count, actualConverters.Count);
                    for (int i = 0; i < actualConverters.Count; i++)
                    {
                        Assert.IsType(expectedConverters[i].GetType(), actualConverters[i]);
                    }
                }
                else if (propertyType == typeof(IList<IJsonTypeInfoResolver>))
                {
                    var list1 = (IList<IJsonTypeInfoResolver>)property.GetValue(expected);
                    var list2 = (IList<IJsonTypeInfoResolver>)property.GetValue(actual);
                    Assert.Equal(list1, list2);
                }
                else
                {
                    Assert.Equal(property.GetValue(expected), property.GetValue(actual));
                }
            }
        }

        /// <summary>
        /// Linq Cartesian product
        /// </summary>
        public static IEnumerable<(TFirst First, TSecond Second)> CrossJoin<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second)
        {
            TSecond[]? secondCached = null;
            foreach (TFirst f in first)
            {
                secondCached ??= second.ToArray();
                foreach (TSecond s in secondCached)
                {
                    yield return (f, s);
                }
            }
        }

        /// <summary>
        /// Linq Cartesian product
        /// </summary>
        public static IEnumerable<(TFirst First, TSecond Second, TThird Third)> CrossJoin<TFirst, TSecond, TThird>(
            this IEnumerable<TFirst> first,
            IEnumerable<TSecond> second,
            IEnumerable<TThird> third)
        {
            TSecond[]? secondCached = null;
            TThird[]? thirdCached = null;

            foreach (TFirst f in first)
            {
                secondCached ??= second.ToArray();
                foreach (TSecond s in secondCached)
                {
                    thirdCached ??= third.ToArray();
                    foreach (TThird t in thirdCached)
                    {
                        yield return (f, s, t);
                    }
                }
            }
        }

        /// <summary>
        /// Linq Cartesian product
        /// </summary>
        public static IEnumerable<TResult> CrossJoin<TFirst, TSecond, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
            => first.CrossJoin(second).Select(tuple => resultSelector(tuple.First, tuple.Second));

        /// <summary>
        /// Linq Cartesian product
        /// </summary>
        public static IEnumerable<TResult> CrossJoin<TFirst, TSecond, TThird, TResult>(
            this IEnumerable<TFirst> first,
            IEnumerable<TSecond> second,
            IEnumerable<TThird> third,
            Func<TFirst, TSecond, TThird, TResult> resultSelector)
            => first.CrossJoin(second, third).Select(tuple => resultSelector(tuple.First, tuple.Second, tuple.Third));

        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
        {
            var list = new List<T>();
            await foreach (T item in source)
            {
                list.Add(item);
            }
            return list;
        }

        private static readonly Regex s_stripWhitespace = new Regex(@"\s+", RegexOptions.Compiled);

        public static string StripWhitespace(this string value)
            => s_stripWhitespace.Replace(value, string.Empty);
    }

    /// <summary>
    /// Generic visitor pattern used for safely invoking generic methods in AOT.
    /// </summary>
    public abstract class TypeWitness
    {
        public abstract TResult Accept<TState, TResult>(ITypeVisitor<TState, TResult> visitor, TState state);
    }

    /// <summary>
    /// Generic visitor pattern used for safely invoking generic methods in AOT.
    /// </summary>
    public sealed class TypeWitness<T> : TypeWitness
    {
        public override TResult Accept<TState, TResult>(ITypeVisitor<TState, TResult> visitor, TState state) => visitor.Visit<T>(state);
    }

    /// <summary>
    /// Generic visitor pattern used for safely invoking generic methods in AOT.
    /// </summary>
    public interface ITypeVisitor<TState, TResult>
    {
        public TResult Visit<T>(TState state);
    }
}
