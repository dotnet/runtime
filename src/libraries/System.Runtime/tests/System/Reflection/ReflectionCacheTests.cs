// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Tests;
using Xunit;

namespace System.Reflection.Tests
{
    [Collection(nameof(NoParallelTests))]
    public class ReflectionCacheTests
    {
        [Fact]
        public void GetMethod_MultipleCalls_SameObjects()
        {
            MethodInfo mi1 = typeof(ReflectionCacheTests).GetMethod(nameof(GetMethod_MultipleCalls_SameObjects));
            Assert.NotNull(mi1);

            MethodInfo mi2 = typeof(ReflectionCacheTests).GetMethod(nameof(GetMethod_MultipleCalls_SameObjects));
            Assert.NotNull(mi2);

            Assert.Same(mi1, mi2);
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/50978", TestRuntimes.Mono)]
        [Fact]
        public void InvokeBeforeUpdate_NoExceptions()
        {
            Action<Type[]> beforeUpdate = GetBeforeUpdateMethod();
            beforeUpdate(null);
            beforeUpdate(new Type[0]);
            beforeUpdate(new Type[] { typeof(ReflectionCacheTests) });
            beforeUpdate(new Type[] { typeof(string), typeof(int) });
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/50978", TestRuntimes.Mono)]
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetMethod_MultipleCalls_ClearCache_DifferentObjects(bool justSpecificType)
        {
            Action<Type[]> beforeUpdate = GetBeforeUpdateMethod();

            MethodInfo mi1 = typeof(ReflectionCacheTests).GetMethod(nameof(GetMethod_MultipleCalls_ClearCache_DifferentObjects));
            Assert.NotNull(mi1);
            Assert.Equal(nameof(GetMethod_MultipleCalls_ClearCache_DifferentObjects), mi1.Name);

            beforeUpdate(justSpecificType ? new[] { typeof(ReflectionCacheTests) } : null);

            MethodInfo mi2 = typeof(ReflectionCacheTests).GetMethod(nameof(GetMethod_MultipleCalls_ClearCache_DifferentObjects));
            Assert.NotNull(mi2);
            Assert.Equal(nameof(GetMethod_MultipleCalls_ClearCache_DifferentObjects), mi2.Name);

            Assert.NotSame(mi1, mi2);
        }

        private static Action<Type[]> GetBeforeUpdateMethod()
        {
            Type updateHandler = typeof(Type).Assembly.GetType("System.Reflection.Metadata.RuntimeTypeMetadataUpdateHandler", throwOnError: true, ignoreCase: false);
            MethodInfo beforeUpdate = updateHandler.GetMethod("BeforeUpdate", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, new[] { typeof(Type[]) });
            Assert.NotNull(beforeUpdate);
            return beforeUpdate.CreateDelegate<Action<Type[]>>();
        }
    }
}
