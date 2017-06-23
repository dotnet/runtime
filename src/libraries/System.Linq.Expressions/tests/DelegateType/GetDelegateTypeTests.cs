// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class GetDelegateTypeTests : DelegateCreationTests
    {
        [Fact]
        public void NullTypeList()
        {
            AssertExtensions.Throws<ArgumentNullException>("typeArgs", () => Expression.GetDelegateType(default(Type[])));
        }

        [Fact]
        public void NullInTypeList()
        {
            AssertExtensions.Throws<ArgumentNullException>("typeArgs[1]", () => Expression.GetDelegateType(typeof(int), null));
        }

        [Fact]
        public void EmptyArgs()
        {
            AssertExtensions.Throws<ArgumentException>("typeArgs", () => Expression.GetDelegateType());
        }

        [Theory, MemberData(nameof(ValidTypeArgs), true)]
        public void SuccessfulGetFuncType(Type[] typeArgs)
        {
            Type funcType = Expression.GetDelegateType(typeArgs);
            Assert.StartsWith("System.Func`" + typeArgs.Length, funcType.FullName);
            Assert.Equal(typeArgs, funcType.GetGenericArguments());
        }

        [Theory, MemberData(nameof(ValidTypeArgs), false)]
        public void SuccessfulGetActionType(Type[] typeArgs)
        {
            Type funcType = Expression.GetDelegateType(typeArgs.Append(typeof(void)).ToArray());
            Assert.StartsWith("System.Action`" + typeArgs.Length, funcType.FullName);
            Assert.Equal(typeArgs, funcType.GetGenericArguments());
        }

        [Fact]
        public void GetNullaryAction()
        {
            Assert.Equal(typeof(Action), Expression.GetDelegateType(typeof(void)));
        }

        [Theory]
        [MemberData(nameof(ExcessiveLengthTypeArgs))]
        [MemberData(nameof(ExcessiveLengthOpenGenericTypeArgs))]
        [MemberData(nameof(ByRefTypeArgs))]
        [MemberData(nameof(PointerTypeArgs))]
        [MemberData(nameof(ManagedPointerTypeArgs))]
        public void CantBeFunc(Type[] typeArgs)
        {
#if !FEATURE_COMPILE
            Assert.Throws<PlatformNotSupportedException>(() => Expression.GetDelegateType(typeArgs));
#else
            Type delType = Expression.GetDelegateType(typeArgs);
            Assert.True(typeof(MulticastDelegate).IsAssignableFrom(delType));
            Assert.DoesNotMatch(new Regex(@"System\.Action"), delType.FullName);
            Assert.DoesNotMatch(new Regex(@"System\.Func"), delType.FullName);
            Reflection.MethodInfo method = delType.GetMethod("Invoke");
            Assert.Equal(typeArgs.Last(), method.ReturnType);
            Assert.Equal(typeArgs.Take(typeArgs.Length - 1), method.GetParameters().Select(p => p.ParameterType));
#endif
        }

        [Theory]
        [MemberData(nameof(ExcessiveLengthTypeArgs))]
        [MemberData(nameof(ExcessiveLengthOpenGenericTypeArgs))]
        [MemberData(nameof(ByRefTypeArgs))]
        [MemberData(nameof(PointerTypeArgs))]
        [MemberData(nameof(ManagedPointerTypeArgs))]
        public void CantBeAction(Type[] typeArgs)
        {
            Type[] delegateArgs = typeArgs.Append(typeof(void)).ToArray();
#if !FEATURE_COMPILE
            Assert.Throws<PlatformNotSupportedException>(() => Expression.GetDelegateType(delegateArgs));
#else
            Type delType = Expression.GetDelegateType(delegateArgs);
            Assert.True(typeof(MulticastDelegate).IsAssignableFrom(delType));
            Assert.DoesNotMatch(new Regex(@"System\.Action"), delType.FullName);
            Assert.DoesNotMatch(new Regex(@"System\.Func"), delType.FullName);
            Reflection.MethodInfo method = delType.GetMethod("Invoke");
            Assert.Equal(typeof(void), method.ReturnType);
            Assert.Equal(typeArgs, method.GetParameters().Select(p => p.ParameterType));
#endif
        }

        // Open generic type args aren't useful directly with Expressions, but creating them is allowed.
        [Theory, MemberData(nameof(OpenGenericTypeArgs), false)]
        public void SuccessfulGetFuncTypeOpenGeneric(Type[] typeArgs)
        {
            Type funcType = Expression.GetDelegateType(typeArgs);
            Assert.Equal("Func`" + typeArgs.Length, funcType.Name);
            Assert.Null(funcType.FullName);
            Assert.Equal(typeArgs, funcType.GetGenericArguments());
        }

        [Theory, MemberData(nameof(OpenGenericTypeArgs), false)]
        public void SuccessfulGetActionTypeOpenGeneric(Type[] typeArgs)
        {
            Type funcType = Expression.GetDelegateType(typeArgs.Append(typeof(void)).ToArray());
            Assert.Equal("Action`" + typeArgs.Length, funcType.Name);
            Assert.Null(funcType.FullName);
            Assert.Equal(typeArgs, funcType.GetGenericArguments());
        }

        [Theory, MemberData(nameof(VoidTypeArgs), false)]
        public void VoidArgToFuncTypeDelegate(Type[] typeArgs)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.GetDelegateType(typeArgs));
        }

        [Theory, MemberData(nameof(VoidTypeArgs), false)]
        public void VoidArgToActionTypeDelegate(Type[] typeArgs)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.GetDelegateType(typeArgs.Append(typeof(void)).ToArray()));
        }
    }
}
