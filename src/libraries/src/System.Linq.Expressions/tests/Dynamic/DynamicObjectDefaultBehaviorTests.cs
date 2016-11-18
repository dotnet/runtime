// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;
using Xunit;

namespace System.Dynamic.Tests
{
    public class DynamicObjectDefaultBehaviorTests
    {
        private class TypeOnlyKnownHere
        {
        }

        private struct ValueTypeOnlyKnownHere
        {
        }

        private class NopDynamicObject : DynamicObject
        {
            // Adds no functionality.
        }

        [Fact]
        public void TryGetMemberDefaultsToFalseWithNoValidation()
        {
            object result;
            Assert.False(new NopDynamicObject().TryGetMember(null, out result));
            Assert.Null(result);
        }

        [Fact]
        public void TrySetMemberDefaultsToFalseWithNoValidation()
        {
            Assert.False(new NopDynamicObject().TrySetMember(null, null));
        }

        [Fact]
        public void TryDeleteMemberDefaultsToFalseWithNoValidation()
        {
            Assert.False(new NopDynamicObject().TryDeleteMember(null));
        }

        [Fact]
        public void TryInvokeMemberDefaultsToFalseWithNoValidation()
        {
            object result;
            Assert.False(new NopDynamicObject().TryInvokeMember(null, null, out result));
            Assert.Null(result);
        }

        [Fact]
        public void TryConvertDefaultsToFalseWithNoValidation()
        {
            object result;
            Assert.False(new NopDynamicObject().TryConvert(null, out result));
            Assert.Null(result);
        }

        [Fact]
        public void TryCreateInstanceDefaultsToFalseWithNoValidation()
        {
            object result;
            Assert.False(new NopDynamicObject().TryCreateInstance(null, null, out result));
            Assert.Null(result);
        }

        [Fact]
        public void TryInvokeDefaultsToFalseWithNoValidation()
        {
            object result;
            Assert.False(new NopDynamicObject().TryInvoke(null, null, out result));
            Assert.Null(result);
        }

        [Fact]
        public void TryBinaryOperationDefaultsToFalseWithNoValidation()
        {
            object result;
            Assert.False(new NopDynamicObject().TryBinaryOperation(null, null, out result));
            Assert.Null(result);
        }

        [Fact]
        public void TryUnaryOperationDefaultsToFalseWithNoValidation()
        {
            object result;
            Assert.False(new NopDynamicObject().TryUnaryOperation(null, out result));
            Assert.Null(result);
        }

        [Fact]
        public void TryGetIndexDefaultsToFalseWithNoValidation()
        {
            object result;
            Assert.False(new NopDynamicObject().TryGetIndex(null, null, out result));
            Assert.Null(result);
        }

        [Fact]
        public void TrySetIndexDefaultsToFalseWithNoValidation()
        {
            Assert.False(new NopDynamicObject().TrySetIndex(null, null, null));
        }

        [Fact]
        public void TryDeleteIndexDefaultsToFalseWithNoValidation()
        {
            Assert.False(new NopDynamicObject().TryDeleteIndex(null, null));
        }

        [Fact]
        public void GetDynamicMemberNamesDefaultsToEmptyArray()
        {
            Assert.Same(Array.Empty<string>(), new NopDynamicObject().GetDynamicMemberNames());
        }

        [Fact]
        public void DefaultPropertiesForMetaObject()
        {
            Expression exp = Expression.Default(typeof(TypeOnlyKnownHere));
            NopDynamicObject nop = new NopDynamicObject();
            DynamicMetaObject dmo = nop.GetMetaObject(exp);
            Assert.Same(nop, dmo.Value);
            Assert.Same(exp, dmo.Expression);
            Assert.True(dmo.HasValue);
            Assert.Same(BindingRestrictions.Empty, dmo.Restrictions);
        }

        [Fact]
        public void DefaultRuntimeTypeForMetaObjectReferenceType()
        {
            Expression exp = Expression.Default(typeof(TypeOnlyKnownHere));
            NopDynamicObject nop = new NopDynamicObject();
            DynamicMetaObject dmo = nop.GetMetaObject(exp);
            Assert.Same(typeof(NopDynamicObject), dmo.RuntimeType);
            Assert.Same(typeof(NopDynamicObject), dmo.LimitType);
        }

        [Fact]
        public void DefaultRuntimeTypeForMetaObjectValueType()
        {
            Expression exp = Expression.Default(typeof(ValueTypeOnlyKnownHere));
            NopDynamicObject nop = new NopDynamicObject();
            DynamicMetaObject dmo = nop.GetMetaObject(exp);
            Assert.Same(typeof(ValueTypeOnlyKnownHere), dmo.RuntimeType);
            Assert.Same(typeof(ValueTypeOnlyKnownHere), dmo.LimitType);
        }

        [Fact]
        public void MetaObjectNullExpression()
        {
            NopDynamicObject nop = new NopDynamicObject();

            // Ideally the name returned should be "parameter" to match that on GetMetaObject
            // but the only way to change this without making other calls incorrect and without
            // a breaking change to names would be to catch and rethrow, which is more expensive
            // than it's worth.
            Assert.Throws<ArgumentNullException>("expression", () => nop.GetMetaObject(null));
        }
    }
}
