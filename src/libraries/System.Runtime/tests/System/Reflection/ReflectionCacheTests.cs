// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Tests;
using Xunit;

namespace System.Reflection.Tests
{
    [Collection(nameof(DisableParallelization))]
    public class ReflectionCacheTests
    {
        private static readonly Type s_type = typeof(ReflectionCacheTests);

        public string Property { get; set; }

        public int Field1;

        private void Method()
        {
            Event1(null, EventArgs.Empty);
        }

        public event EventHandler Event1;

        [Fact]
        public void GetMethod_MultipleCalls_SameObjects()
        {
            MethodInfo mi1 = s_type.GetMethod(nameof(GetMethod_MultipleCalls_SameObjects));
            PropertyInfo pi1 = s_type.GetProperty(nameof(Property));
            FieldInfo fi1 = s_type.GetField(nameof(Field1));
            EventInfo ei1 = s_type.GetEvent(nameof(Event1));
            Assert.NotNull(mi1);
            Assert.NotNull(pi1);
            Assert.NotNull(fi1);

            MethodInfo mi2 = s_type.GetMethod(nameof(GetMethod_MultipleCalls_SameObjects));
            PropertyInfo pi2 = s_type.GetProperty(nameof(Property));
            FieldInfo fi2 = s_type.GetField(nameof(Field1));
            Assert.NotNull(mi2);
            Assert.NotNull(pi2);
            Assert.NotNull(fi2);

            Assert.Same(mi1, mi2);
            Assert.Same(pi1, pi2);
            Assert.Same(fi1, fi2);
            Assert.Equal(mi1, mi2);
            Assert.Equal(pi1, pi2);
            Assert.Equal(fi1, fi2);
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/50978", TestRuntimes.Mono)]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMetadataUpdateSupported))]
        public void InvokeClearCache_NoExceptions()
        {
            Action<Type[]> clearCache = GetClearCacheMethod();
            clearCache(null);
            clearCache(new Type[0]);
            clearCache(new Type[] { typeof(ReflectionCacheTests) });
            clearCache(new Type[] { typeof(string), typeof(int) });
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/50978", TestRuntimes.Mono)]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsMetadataUpdateSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void GetMethod_MultipleCalls_ClearCache_DifferentObjects(bool justSpecificType)
        {
            Action<Type[]> clearCache = GetClearCacheMethod();

            MethodInfo mi1 = s_type.GetMethod(nameof(GetMethod_MultipleCalls_ClearCache_DifferentObjects));
            PropertyInfo pi1 = s_type.GetProperty(nameof(Property));
            FieldInfo fi1 = s_type.GetField(nameof(Field1));
            EventInfo ei1 = s_type.GetEvent(nameof(Event1));
            ConstructorInfo ci1 = s_type.GetConstructor(Type.EmptyTypes);
            ParameterInfo pai1 = mi1.GetParameters()[0];
            int mi1Hash = mi1.GetHashCode();
            int pi1Hash = pi1.GetHashCode();
            int fi1Hash = fi1.GetHashCode();
            int ei1Hash = ei1.GetHashCode();
            int ci1Hash = ci1.GetHashCode();
            int pai1Hash = pai1.GetHashCode();

            Assert.NotNull(mi1);
            Assert.NotNull(pi1);
            Assert.NotNull(fi1);
            Assert.Equal(nameof(GetMethod_MultipleCalls_ClearCache_DifferentObjects), mi1.Name);
            Assert.Equal(nameof(Property), pi1.Name);
            Assert.Equal(nameof(Field1), fi1.Name);

            clearCache(justSpecificType ? new[] { typeof(ReflectionCacheTests) } : null);
            Assert.True(HotReloadDeltaApplied());
            MethodInfo mi2 = s_type.GetMethod(nameof(GetMethod_MultipleCalls_ClearCache_DifferentObjects));
            PropertyInfo pi2 = s_type.GetProperty(nameof(Property));
            FieldInfo fi2 = s_type.GetField(nameof(Field1));
            EventInfo ei2 = s_type.GetEvent(nameof(Event1));
            ConstructorInfo ci2 = s_type.GetConstructor(Type.EmptyTypes);
            ParameterInfo pai2 = mi2.GetParameters()[0];
            int mi2Hash = mi2.GetHashCode();
            int pi2Hash = pi2.GetHashCode();
            int fi2Hash = fi2.GetHashCode();
            int ei2Hash = ei2.GetHashCode();
            int ci2Hash = ci2.GetHashCode();
            int pai2Hash = pai2.GetHashCode();

            Assert.NotNull(mi2);
            Assert.NotNull(pi2);
            Assert.NotNull(fi2);
            Assert.Equal(nameof(GetMethod_MultipleCalls_ClearCache_DifferentObjects), mi2.Name);
            Assert.Equal(nameof(Property), pi2.Name);
            Assert.Equal(nameof(Field1), fi2.Name);

            // After the Cache cleared the references of same member will be diffenet 
            Assert.NotSame(mi1, mi2);
            Assert.NotSame(pi1, pi2);
            Assert.NotSame(fi1, fi2);
            Assert.NotSame(ei1, ei2);
            Assert.NotSame(ci1, ci2);
            Assert.NotSame(pai1, pai2);

            // But they should be evaluated as Equal so that there were no issue using the same member after hot reload
            Assert.Equal(mi1, mi2);
            Assert.Equal(pi1, pi2);
            Assert.Equal(fi1, fi2);
            Assert.Equal(ei1, ei2);
            Assert.Equal(ci1, ci2);
            Assert.Equal(pai1, pai2);

            // And the HashCode of a member before and after hot reload should produce same result 
            Assert.Equal(mi1Hash, mi2Hash);
            Assert.Equal(pi1Hash, pi2Hash);
            Assert.Equal(fi1Hash, fi2Hash);
            Assert.Equal(ei1Hash, ei2Hash);
            Assert.Equal(ci1Hash, ci2Hash);
            Assert.Equal(pai1Hash, pai2Hash);
        }

        private static Action<Type[]> GetClearCacheMethod()
        {
            Type updateHandler = typeof(Type).Assembly.GetType("System.Reflection.Metadata.RuntimeTypeMetadataUpdateHandler", throwOnError: true, ignoreCase: false);
            MethodInfo clearCache = updateHandler.GetMethod("ClearCache", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, new[] { typeof(Type[]) });
            Assert.NotNull(clearCache);
            return clearCache.CreateDelegate<Action<Type[]>>();
        }

        private static bool HotReloadDeltaApplied()
        {
            Type updateHandler = typeof(Type).Assembly.GetType("System.Reflection.Metadata.RuntimeTypeMetadataUpdateHandler", throwOnError: true, ignoreCase: false);
            PropertyInfo hotReloadApplied = updateHandler.GetProperty("HotReloadDeltaApplied", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(hotReloadApplied);
            return (bool)hotReloadApplied.GetValue(null);
        }
    }
}
