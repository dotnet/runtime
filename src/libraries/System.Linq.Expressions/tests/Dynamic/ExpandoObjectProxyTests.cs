// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Dynamic.Tests
{
    public class ExpandoObjectProxyTests
    {
        private static Type GetDebugViewType(Type type)
        {
            var att =
                (DebuggerTypeProxyAttribute)
                    type.GetCustomAttributes().SingleOrDefault(at => at.TypeId.Equals(typeof(DebuggerTypeProxyAttribute)));
            if (att == null)
            {
                return null;
            }
            string proxyName = att.ProxyTypeName;
            proxyName = proxyName.Substring(0, proxyName.IndexOf(','));
            return type.GetTypeInfo().Assembly.GetType(proxyName);
        }

        private static object GetDebugViewObject(object obj)
            => GetDebugViewType(obj.GetType())?.GetConstructors().Single().Invoke(new[] {obj});

        private static IEnumerable<IDictionary<string, object>> TestExpandos()
        {
            yield return new ExpandoObject();

            dynamic dyn0 = new ExpandoObject();
            dyn0.X = 2;
            yield return dyn0;

            dynamic dyn1 = new ExpandoObject();
            dyn1.X = "hello";
            yield return dyn1;

            dynamic dyn2 = new ExpandoObject();
            dyn2.X = "hello";
            dyn2.Y = 42;
            dyn2.Greet = (Func<string>)(() => $"{dyn2.Salutation} {dyn2.Recipient}!");
            yield return dyn2;

            dynamic dyn3 = new ExpandoObject();
            dyn3.X = "hello";
            dyn3.Y = 42;
            dyn3.Greet = (Func<string>)(() => $"{dyn3.Salutation} {dyn3.Recipient}!");
            dyn3.Salutation = "Hello";
            dyn3.Recipient = "World";
            yield return dyn3;

            dynamic dyn4 = new ExpandoObject();
            dyn4.X = "hello";
            dyn4.Y = 42;
            dyn4.Greet = (Func<string>)(() => $"{dyn4.Salutation} {dyn4.Recipient}!");
            dyn4.Salutation = "Hello";
            dyn4.Recipient = "World";
            ((IDictionary<string, object>)dyn4).Remove("Y");
            yield return dyn4;
        }

        public static IEnumerable<object[]> KeyCollections() => TestExpandos().Select(dict => new object[] {dict.Keys});

        public static IEnumerable<object[]> ValueCollections()
            => TestExpandos().Select(dict => new object[] {dict.Values});

        public static IEnumerable<object[]> OneOfEachCollection() =>
            KeyCollections().Take(1).Concat(ValueCollections().Take(1));

        private static void AssertSameCollectionIgnoreOrder<T>(ICollection<T> expected, ICollection<T> actual)
        {
            Assert.Equal(actual.Count, expected.Count);
            foreach(T item in actual)
                Assert.Contains(item, expected);
        }

        [ConditionalTheory]
        [MemberData(nameof(KeyCollections))]
        [MemberData(nameof(ValueCollections))]
        public void ItemsAreRootHidden(object eo)
        {
            object view = GetDebugViewObject(eo);
            if (view == null)
            {
                throw new SkipTestException($"Didn't find DebuggerTypeProxyAttribute on {eo}.");
            }
            PropertyInfo itemsProp = view.GetType().GetProperty("Items");
            var browsable = (DebuggerBrowsableAttribute)itemsProp.GetCustomAttribute(typeof(DebuggerBrowsableAttribute));
            Assert.Equal(DebuggerBrowsableState.RootHidden, browsable.State);
        }

        [ConditionalTheory, MemberData(nameof(KeyCollections))]
        public void KeyCollectionCorrectlyViewed(ICollection<string> keys)
        {
            object view = GetDebugViewObject(keys);
            if (view == null)
            {
                throw new SkipTestException($"Didn't find DebuggerTypeProxyAttribute on {keys}.");
            }
            PropertyInfo itemsProp = view.GetType().GetProperty("Items");
            string[] items = (string[])itemsProp.GetValue(view);
            AssertSameCollectionIgnoreOrder(keys, items);
        }

        [ConditionalTheory, MemberData(nameof(ValueCollections))]
        public void ValueCollectionCorrectlyViewed(ICollection<object> keys)
        {
            object view = GetDebugViewObject(keys);
            if (view == null)
            {
                throw new SkipTestException($"Didn't find DebuggerTypeProxyAttribute on {keys}.");
            }
            PropertyInfo itemsProp = view.GetType().GetProperty("Items");
            object[] items = (object[])itemsProp.GetValue(view);
            AssertSameCollectionIgnoreOrder(keys, items);
        }

        [ConditionalTheory, MemberData(nameof(OneOfEachCollection))]
        public void ViewTypeThrowsOnNull(object collection)
        {
            Type debugViewType = GetDebugViewType(collection.GetType());
            if (debugViewType == null)
            {
                throw new SkipTestException($"Didn't find DebuggerTypeProxyAttribute on {collection.GetType()}.");
            }
            ConstructorInfo constructor = debugViewType.GetConstructors().Single();
            TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => constructor.Invoke(new object[] {null}));
            var ane = (ArgumentNullException)tie.InnerException;
            Assert.Equal("collection", ane.ParamName);
        }
    }
}
