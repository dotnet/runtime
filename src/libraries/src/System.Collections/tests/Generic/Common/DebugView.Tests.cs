// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Collections.Tests
{
    public class DebugView_Tests
    {
        public static IEnumerable<object[]> TestDebuggerAttributes_Inputs()
        {
            yield return new object[] { new Dictionary<int, string>() };
            yield return new object[] { new HashSet<string>() };
            yield return new object[] { new LinkedList<object>() };
            yield return new object[] { new List<int>() };
            yield return new object[] { new Queue<double>() };
            yield return new object[] { new SortedDictionary<string, int>() };
            yield return new object[] { new SortedList<int, string>() };
            yield return new object[] { new SortedSet<int>() };
            yield return new object[] { new Stack<object>() };

            yield return new object[] { new Dictionary<double, float>().Keys };
            yield return new object[] { new Dictionary<float, double>().Values };
            yield return new object[] { new SortedDictionary<Guid, string>().Keys };
            yield return new object[] { new SortedDictionary<long, Guid>().Values };
            yield return new object[] { new SortedList<string, int>().Keys };
            yield return new object[] { new SortedList<float, long>().Values };
        }

        [Theory]
        [MemberData("TestDebuggerAttributes_Inputs")]
        public static void TestDebuggerAttributes(object obj)
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(obj);
            DebuggerAttributes.ValidateDebuggerTypeProxyProperties(obj);
        }

        [Theory]
        [MemberData("TestDebuggerAttributes_Inputs")]
        public static void TestDebuggerAttributes_Null(object obj)
        {
            // Get the DebuggerTypeProxyAttibute for obj
            var attrs =
                obj.GetType().GetTypeInfo().CustomAttributes
                .Where(a => a.AttributeType == typeof(DebuggerTypeProxyAttribute))
                .ToArray();
            if (attrs.Length != 1)
            {
                throw new InvalidOperationException(
                    string.Format("Expected one DebuggerTypeProxyAttribute on {0}.", obj));
            }
            var cad = (CustomAttributeData)attrs[0];

            // Get the proxy type.  As written, this only works if the proxy and the target type
            // have the same generic parameters, e.g. Dictionary<TKey,TValue> and Proxy<TKey,TValue>.
            // It will not work with, for example, Dictionary<TKey,TValue>.Keys and Proxy<TKey>,
            // as the former has two generic parameters and the latter only one.
            Type proxyType = cad.ConstructorArguments[0].ArgumentType == typeof(Type) ?
                (Type)cad.ConstructorArguments[0].Value :
                Type.GetType((string)cad.ConstructorArguments[0].Value);
            var genericArguments = obj.GetType().GenericTypeArguments;
            if (genericArguments.Length > 0)
            {
                proxyType = proxyType.MakeGenericType(genericArguments);
            }

            // Create an instance of the proxy type, and make sure we can access all of the instance properties 
            // on the type without exception
            Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(proxyType, (object)null));
        }
    }
}
