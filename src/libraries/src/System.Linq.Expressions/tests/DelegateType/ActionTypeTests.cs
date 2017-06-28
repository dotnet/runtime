// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class ActionTypeTests : DelegateCreationTests
    {
        [Fact]
        public void NullTypeList()
        {
            AssertExtensions.Throws<ArgumentNullException>("typeArgs", () => Expression.GetActionType(default(Type[])));
        }

        [Fact]
        public void NullInTypeList()
        {
            AssertExtensions.Throws<ArgumentNullException>("typeArgs", () => Expression.GetActionType(typeof(int), null));
        }

        [Theory]
        [MemberData(nameof(EmptyTypeArgs))]
        [MemberData(nameof(ValidTypeArgs), false)]
        public void SuccessfulGetActionType(Type[] typeArgs)
        {
            Type funcType = Expression.GetActionType(typeArgs);
            if (typeArgs.Length == 0)
            {
                Assert.Equal(typeof(Action), funcType);
            }
            else
            {
                Assert.StartsWith("System.Action`" + typeArgs.Length, funcType.FullName);
                Assert.Equal(typeArgs, funcType.GetGenericArguments());
            }
        }

        [Theory]
        [MemberData(nameof(EmptyTypeArgs))]
        [MemberData(nameof(ValidTypeArgs), false)]
        public void SuccessfulTryGetActionType(Type[] typeArgs)
        {
            Type funcType;
            Assert.True(Expression.TryGetActionType(typeArgs, out funcType));
            if (typeArgs.Length == 0)
            {
                Assert.Equal(typeof(Action), funcType);
            }
            else
            {
                Assert.StartsWith("System.Action`" + typeArgs.Length, funcType.FullName);
                Assert.Equal(typeArgs, funcType.GetGenericArguments());
            }
        }

        // Open generic type args aren't useful directly with Expressions, but creating them is allowed.
        [Theory, MemberData(nameof(OpenGenericTypeArgs), false)]
        public void SuccessfulGetActionTypeOpenGeneric(Type[] typeArgs)
        {
            Type funcType = Expression.GetActionType(typeArgs);
            Assert.Equal("Action`" + typeArgs.Length, funcType.Name);
            Assert.Null(funcType.FullName);
            Assert.Equal(typeArgs, funcType.GetGenericArguments());
        }

        [Theory, MemberData(nameof(OpenGenericTypeArgs), false)]
        public void SuccessfulTryGetActionTypeWithOpenGeneric(Type[] typeArgs)
        {
            Type funcType;
            Assert.True(Expression.TryGetActionType(typeArgs, out funcType));
            Assert.Equal("Action`" + typeArgs.Length, funcType.Name);
            Assert.Null(funcType.FullName);
            Assert.Equal(typeArgs, funcType.GetGenericArguments());
        }

        [Theory]
        [MemberData(nameof(ExcessiveLengthTypeArgs))]
        [MemberData(nameof(ExcessiveLengthOpenGenericTypeArgs))]
        [MemberData(nameof(ByRefTypeArgs))]
        [MemberData(nameof(PointerTypeArgs))]
        [MemberData(nameof(ManagedPointerTypeArgs))]
        [MemberData(nameof(VoidTypeArgs), true)]
        public void UnsuccessfulGetActionType(Type[] typeArgs)
        {
            string paramName = typeArgs.Any(t => t == typeof(void)) || typeArgs.Count(t => t.IsPointer) == 1 ? null : "typeArgs";
            AssertExtensions.Throws<ArgumentException>(paramName, () => Expression.GetActionType(typeArgs));
        }

        [Theory]
        [MemberData(nameof(ExcessiveLengthTypeArgs))]
        [MemberData(nameof(ExcessiveLengthOpenGenericTypeArgs))]
        [MemberData(nameof(ByRefTypeArgs))]
        [MemberData(nameof(PointerTypeArgs))]
        [MemberData(nameof(ManagedPointerTypeArgs))]
        [MemberData(nameof(VoidTypeArgs), true)]
        public void UnsuccessfulTryGetActionType(Type[] typeArgs)
        {
            Type funcType;
            Assert.False(Expression.TryGetActionType(typeArgs, out funcType));
        }
    }
}
