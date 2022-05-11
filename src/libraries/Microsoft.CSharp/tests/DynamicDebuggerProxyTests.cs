// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class DynamicDebuggerProxyTests
    {
        private static readonly Type _debugViewType = GetType("DynamicMetaObjectProviderDebugView");

        // DynamicDebugViewEmptyException.Empty is used by the debugger EE
        // as the message displayed when a dynamic object has no members.
        [Fact]
        public void Items_Empty()
        {
            var obj = new DynamicTestObject(new Dictionary<string, object>());
            var debugView = CreateDebugView(obj);

            var exceptionType = GetType("DynamicMetaObjectProviderDebugView+DynamicDebugViewEmptyException");
            var exception = Assert.Throws(exceptionType, () => GetItems(debugView));

            // Get resource string.
            var itemValue = (string)exceptionType.GetMethod("get_Empty", BindingFlags.Public | BindingFlags.Instance).Invoke(exception, new object[0]);
            Assert.NotNull(itemValue);
        }

        [Fact]
        public void Items()
        {
            var obj = new DynamicTestObject(new Dictionary<string, object>() { { "F", 1 }, { "G", "" } });
            var debugView = CreateDebugView(obj);
            var items = GetItems(debugView);
            Assert.Equal(2, items.Length);
        }

        [Fact]
        public void TryGetMemberValue()
        {
            var obj = new DynamicTestObject(new Dictionary<string, object>() { { "F", 1 } });
            Assert.Equal(1, TryGetMemberValueImpl(obj, "F", ignoreException: false));
        }

        [Fact]
        public void TryGetMemberValue_BindingFailed()
        {
            var obj = new DynamicTestObject(new Dictionary<string, object>());

            // ignoreException: true
            Assert.Null(TryGetMemberValueImpl(obj, "F", ignoreException: true));

            // ignoreException: false
            var exceptionType = GetType("DynamicBindingFailedException");
            Assert.Throws(exceptionType, () => GetItems(TryGetMemberValueImpl(obj, "F", ignoreException: false)));
        }

        [Fact]
        public void TryGetMemberValue_MissingMember()
        {
            var obj = new DynamicTestObject(new Dictionary<string, object>(), throwIfMissing: true);

            // ignoreException: true
            string message = (string)TryGetMemberValueImpl(obj, "F", ignoreException: true);
            Assert.NotNull(message);

            // ignoreException: false
            Assert.Throws<MissingMemberException>(() => GetItems(TryGetMemberValueImpl(obj, "F", ignoreException: false)));
        }

        private static Type GetType(string typeName)
        {
            return typeof(Binder).Assembly.GetType("Microsoft.CSharp.RuntimeBinder." + typeName, throwOnError: true);
        }

        private static object CreateDebugView(object arg)
        {
            return Activator.CreateInstance(_debugViewType, new object[] { arg });
        }

        private static object[] GetItems(object debugView)
        {
            var method = _debugViewType.GetMethod("get_Items", BindingFlags.NonPublic | BindingFlags.Instance);
            return (object[])InvokeAndUnwrapException(method, debugView, new object[0]);
        }

        private static object TryGetMemberValueImpl(object obj, string name, bool ignoreException)
        {
            var method = _debugViewType.GetMethod("TryGetMemberValue", BindingFlags.NonPublic | BindingFlags.Static);
            return InvokeAndUnwrapException(method, null, new object[] { obj, name, ignoreException });
        }

        private static object InvokeAndUnwrapException(MethodInfo method, object obj, object[] args)
        {
            try
            {
                return method.Invoke(obj, args);
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException;
            }
        }

        private sealed class DynamicTestObject : DynamicObject
        {
            private readonly Dictionary<string, object> _members;
            private readonly bool _throwIfMissing;

            internal DynamicTestObject(Dictionary<string, object> members, bool throwIfMissing = false)
            {
                _members = members;
                _throwIfMissing = throwIfMissing;
            }

            public override IEnumerable<string> GetDynamicMemberNames()
            {
                return _members.Keys;
            }

            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                if (_members.TryGetValue(binder.Name, out result))
                {
                    return true;
                }
                if (_throwIfMissing)
                {
                    throw new MissingMemberException();
                }
                return false;
            }
        }
    }
}
