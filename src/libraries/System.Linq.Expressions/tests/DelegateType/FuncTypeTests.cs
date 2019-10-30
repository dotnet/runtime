// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class FuncTypeTests : DelegateCreationTests
    {
        [Fact]
        public void NullTypeList()
        {
            AssertExtensions.Throws<ArgumentNullException>("typeArgs", () => Expression.GetFuncType(default(Type[])));
        }

        [Fact]
        public void NullInTypeList()
        {
            AssertExtensions.Throws<ArgumentNullException>("typeArgs", () => Expression.GetFuncType(typeof(int), null));
        }

        [Theory, MemberData(nameof(ValidTypeArgs), true)]
        public void SuccessfulGetFuncType(Type[] typeArgs)
        {
            Type funcType = Expression.GetFuncType(typeArgs);
            Assert.StartsWith("System.Func`" + typeArgs.Length, funcType.FullName);
            Assert.Equal(typeArgs, funcType.GetGenericArguments());
        }

        [Theory, MemberData(nameof(ValidTypeArgs), true)]
        public void SuccessfulTryGetFuncType(Type[] typeArgs)
        {
            Type funcType;
            Assert.True(Expression.TryGetFuncType(typeArgs, out funcType));
            Assert.StartsWith("System.Func`" + typeArgs.Length, funcType.FullName);
            Assert.Equal(typeArgs, funcType.GetGenericArguments());
        }

        // Open generic type args aren't useful directly with Expressions, but creating them is allowed.
        [Theory, MemberData(nameof(OpenGenericTypeArgs), false)]
        public void SuccessfulGetFuncTypeOpenGeneric(Type[] typeArgs)
        {
            Type funcType = Expression.GetFuncType(typeArgs);
            Assert.Equal("Func`" + typeArgs.Length, funcType.Name);
            Assert.Null(funcType.FullName);
            Assert.Equal(typeArgs, funcType.GetGenericArguments());
        }

        [Theory, MemberData(nameof(OpenGenericTypeArgs), false)]
        public void SuccessfulTryGetFuncTypeWithOpenGeneric(Type[] typeArgs)
        {
            Type funcType;
            Assert.True(Expression.TryGetFuncType(typeArgs, out funcType));
            Assert.Equal("Func`" + typeArgs.Length, funcType.Name);
            Assert.Null(funcType.FullName);
            Assert.Equal(typeArgs, funcType.GetGenericArguments());
        }

        [Theory]
        [MemberData(nameof(EmptyTypeArgs))]
        [MemberData(nameof(ExcessiveLengthTypeArgs))]
        [MemberData(nameof(ByRefTypeArgs))]
        [MemberData(nameof(PointerTypeArgs))]
        [MemberData(nameof(ManagedPointerTypeArgs))]
        [MemberData(nameof(VoidTypeArgs), true)]
        public void UnsuccessfulGetFuncType(Type[] typeArgs)
        {
            string paramName = typeArgs.Any(t => t == typeof(void)) || typeArgs.Count(t => t.IsPointer) == 1 ? null : "typeArgs";
            AssertExtensions.Throws<ArgumentException>(paramName, () => Expression.GetFuncType(typeArgs));
        }

        [Theory]
        [MemberData(nameof(EmptyTypeArgs))]
        [MemberData(nameof(ExcessiveLengthTypeArgs))]
        [MemberData(nameof(ByRefTypeArgs))]
        [MemberData(nameof(PointerTypeArgs))]
        [MemberData(nameof(ManagedPointerTypeArgs))]
        [MemberData(nameof(VoidTypeArgs), true)]
        public void UnsuccessfulTryGetFuncType(Type[] typeArgs)
        {
            Type funcType;
            Assert.False(Expression.TryGetFuncType(typeArgs, out funcType));
        }
    }
}
