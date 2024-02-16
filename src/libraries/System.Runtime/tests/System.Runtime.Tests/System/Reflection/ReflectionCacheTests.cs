// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Tests;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Reflection.Tests
{
    public class A
    {
        public string P { get; set; }
        public int F;
#pragma warning disable CS0067
        public event EventHandler E;
#pragma warning restore CS0067
        public void M() { }
    }

    [Collection(nameof(DisableParallelization))]
    public class ReflectionCacheTests : A
    {
        private static bool IsMetadataUpdateAndRemoteExecutorSupported => PlatformDetection.IsMetadataUpdateSupported && RemoteExecutor.IsSupported;

        private static readonly Type s_type = typeof(ReflectionCacheTests);

        public string Property { get; set; }

        public int Field1;

#pragma warning disable xUnit1013 // Public method should be marked as test
        public void Method(bool param)
#pragma warning restore xUnit1013 // Public method should be marked as test
        {
            Event1(null, EventArgs.Empty);
        }

        public event EventHandler Event1;

        [Fact]
        public void GetMembers_MultipleCalls_SameObjects()
        {
            MethodInfo mi1 = s_type.GetMethod(nameof(Method));
            PropertyInfo pi1 = s_type.GetProperty(nameof(Property));
            FieldInfo fi1 = s_type.GetField(nameof(Field1));
            EventInfo ei1 = s_type.GetEvent(nameof(Event1));
            ConstructorInfo ci1 = s_type.GetConstructor(Type.EmptyTypes);

            MethodInfo mi2 = s_type.GetMethod(nameof(Method));
            PropertyInfo pi2 = s_type.GetProperty(nameof(Property));
            FieldInfo fi2 = s_type.GetField(nameof(Field1));
            EventInfo ei2 = s_type.GetEvent(nameof(Event1));
            ConstructorInfo ci2 = s_type.GetConstructor(Type.EmptyTypes);

            AssertSameEqualAndHashCodeEqual(mi1, mi2);
            AssertSameEqualAndHashCodeEqual(pi1, pi2);
            AssertSameEqualAndHashCodeEqual(fi1, fi2);
            AssertSameEqualAndHashCodeEqual(ei1, ei2);
            AssertSameEqualAndHashCodeEqual(ci1, ci2);

            PropertyInfo parentProperty = typeof(A).GetProperty("P");
            PropertyInfo childProperty = s_type.GetProperty("P");
            Assert.NotNull(parentProperty);
            Assert.NotNull(childProperty);
            Assert.NotEqual(parentProperty, childProperty);
        }

        void AssertSameEqualAndHashCodeEqual(object o1, object o2)
        {
            // When cache not cleared the references of the same members are Same and Equal, and Hashcodes Equal.
            Assert.NotNull(o1);
            Assert.NotNull(o2);
            Assert.Same(o1, o2);
            Assert.Equal(o1, o2);
            Assert.Equal(o1.GetHashCode(), o2.GetHashCode());
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
        [ConditionalFact(typeof(ReflectionCacheTests), nameof(IsMetadataUpdateAndRemoteExecutorSupported))]
        public void GetMembers_MultipleCalls_ClearCache_ReflectionCacheTestsType()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables.Add("DOTNET_MODIFIABLE_ASSEMBLIES", "debug");

            using RemoteInvokeHandle remoteHandle = RemoteExecutor.Invoke(() =>
            {
                Action<Type[]> clearCache = GetClearCacheMethod();

                MethodInfo mi1 = s_type.GetMethod(nameof(Method));
                PropertyInfo pi1 = s_type.GetProperty(nameof(Property));
                FieldInfo fi1 = s_type.GetField(nameof(Field1));
                EventInfo ei1 = s_type.GetEvent(nameof(Event1));
                ConstructorInfo ci1 = s_type.GetConstructor(Type.EmptyTypes);

                PropertyInfo parentProperty = typeof(A).GetProperty("P");
                PropertyInfo childProperty = s_type.GetProperty("P");
                FieldInfo parentField = typeof(A).GetField("F");
                FieldInfo childField = s_type.GetField("F");
                MethodInfo parentMethod = typeof(A).GetMethod("M");
                MethodInfo childMethod = s_type.GetMethod("M");
                EventInfo parentEvent = typeof(A).GetEvent("E");
                EventInfo childEvent = s_type.GetEvent("E");

                Assert.NotEqual(parentProperty, childProperty);
                Assert.NotEqual(parentField, childField);
                Assert.NotEqual(parentMethod, childMethod);
                Assert.NotEqual(parentEvent, childEvent);

                clearCache(new[] { typeof(ReflectionCacheTests) });

                MethodInfo mi2 = s_type.GetMethod(nameof(Method));
                PropertyInfo pi2 = s_type.GetProperty(nameof(Property));
                FieldInfo fi2 = s_type.GetField(nameof(Field1));
                EventInfo ei2 = s_type.GetEvent(nameof(Event1));
                ConstructorInfo ci2 = s_type.GetConstructor(Type.EmptyTypes);

                Assert.NotEqual(parentProperty, childProperty);
                Assert.NotEqual(parentField, childField);
                Assert.NotEqual(parentMethod, childMethod);
                Assert.NotEqual(parentEvent, childEvent);

                AssertNotSameSameButEqualAndHashCodeEqual(mi1, mi2);
                AssertNotSameSameButEqualAndHashCodeEqual(pi1, pi2);
                AssertNotSameSameButEqualAndHashCodeEqual(fi1, fi2);
                AssertNotSameSameButEqualAndHashCodeEqual(ci1, ci2);
                AssertNotSameSameButEqualAndHashCodeEqual(ei1, ei2);
            }, options);
        }

        private static void AssertNotSameSameButEqualAndHashCodeEqual(object o1, object o2)
        {
            // After the cache cleared the references of the same members will be Not Same.
            // But they should be evaluated as Equal so that there were no issue using the same member after hot reload.
            // And the member HashCode before and after hot reload should produce the same result.

            Assert.NotNull(o1);
            Assert.NotNull(o2);
            Assert.NotSame(o1, o2);
            Assert.Equal(o1, o2);
            Assert.Equal(o1.GetHashCode(), o2.GetHashCode());
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/50978", TestRuntimes.Mono)]
        [ConditionalFact(typeof(ReflectionCacheTests), nameof(IsMetadataUpdateAndRemoteExecutorSupported))]
        public void GetMembers_MultipleCalls_ClearCache_All()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables.Add("DOTNET_MODIFIABLE_ASSEMBLIES", "debug");

            using RemoteInvokeHandle remoteHandle = RemoteExecutor.Invoke(() =>
            {
                Action<Type[]> clearCache = GetClearCacheMethod();

                MethodInfo mi1 = s_type.GetMethod(nameof(Method));
                PropertyInfo pi1 = s_type.GetProperty(nameof(Property));
                FieldInfo fi1 = s_type.GetField(nameof(Field1));
                EventInfo ei1 = s_type.GetEvent(nameof(Event1));
                ConstructorInfo ci1 = s_type.GetConstructor(Type.EmptyTypes);

                clearCache(null);

                MethodInfo mi2 = s_type.GetMethod(nameof(Method));
                PropertyInfo pi2 = s_type.GetProperty(nameof(Property));
                FieldInfo fi2 = s_type.GetField(nameof(Field1));
                EventInfo ei2 = s_type.GetEvent(nameof(Event1));
                ConstructorInfo ci2 = s_type.GetConstructor(Type.EmptyTypes);

                AssertNotSameSameButEqualAndHashCodeEqual(mi1, mi2);
                AssertNotSameSameButEqualAndHashCodeEqual(pi1, pi2);
                AssertNotSameSameButEqualAndHashCodeEqual(fi1, fi2);
                AssertNotSameSameButEqualAndHashCodeEqual(ci1, ci2);
                AssertNotSameSameButEqualAndHashCodeEqual(ei1, ei2);
            }, options);
        }

        private static Action<Type[]> GetClearCacheMethod()
        {
            Type updateHandler = typeof(Type).Assembly.GetType("System.Reflection.Metadata.RuntimeTypeMetadataUpdateHandler", throwOnError: true, ignoreCase: false);
            MethodInfo clearCache = updateHandler.GetMethod("ClearCache", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, new[] { typeof(Type[]) });
            Assert.NotNull(clearCache);
            return clearCache.CreateDelegate<Action<Type[]>>();
        }
    }
}
