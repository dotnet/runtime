// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Tests;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Reflection.Tests
{
    [Collection(nameof(DisableParallelization))]
    public class ReflectionCacheTests
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
            ParameterInfo pai1 = mi1.GetParameters()[0];

            AssertMembersAreNotNull(mi1, pi1, fi1, ei1, ci1, pai1);

            MethodInfo mi2 = s_type.GetMethod(nameof(Method));
            PropertyInfo pi2 = s_type.GetProperty(nameof(Property));
            FieldInfo fi2 = s_type.GetField(nameof(Field1));
            EventInfo ei2 = s_type.GetEvent(nameof(Event1));
            ConstructorInfo ci2 = s_type.GetConstructor(Type.EmptyTypes);
            ParameterInfo pai2 = mi2.GetParameters()[0];

            AssertMembersAreNotNull(mi2, pi2, fi2, ei2, ci2, pai2);

            Assert.Same(mi1, mi2);
            Assert.Same(pi1, pi2);
            Assert.Same(fi1, fi2);
            Assert.Same(ei1, ei2);
            Assert.Same(ci1, ci2);
            Assert.Same(pai1, pai2);

            Assert.Equal(mi1, mi2);
            Assert.Equal(pi1, pi2);
            Assert.Equal(fi1, fi2);
            Assert.Equal(ei1, ei2);
            Assert.Equal(ci1, ci2);
            Assert.Equal(pai1, pai2);
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
        public void GetMembers_MultipleCalls_ClearCache_SpecificType()
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
                ParameterInfo pai1 = mi1.GetParameters()[0];

                AssertMembersAreNotNull(mi1, pi1, fi1, ei1, ci1, pai1);

                clearCache(new[] { typeof(ReflectionCacheTests) });

                MethodInfo mi2 = s_type.GetMethod(nameof(Method));
                PropertyInfo pi2 = s_type.GetProperty(nameof(Property));
                FieldInfo fi2 = s_type.GetField(nameof(Field1));
                EventInfo ei2 = s_type.GetEvent(nameof(Event1));
                ConstructorInfo ci2 = s_type.GetConstructor(Type.EmptyTypes);
                ParameterInfo pai2 = mi2.GetParameters()[0];

                AssertMembersAreNotNull(mi2, pi2, fi2, ei2, ci2, pai2);

                // After the Cache cleared the references of the same members of the same type will be different
                // But they should be evaluated as Equal so that there were no issue using the same member after hot reload
                AssertMemberReferencesNotSameButEqual(mi1, pi1, fi1, ei1, ci1, pai1, mi2, pi2, fi2, ei2, ci2, pai2);

                // And the HashCode of a member before and after hot reload should produce same result 
                AssertHashCodesAreEqual(mi1.GetHashCode(), pi1.GetHashCode(), fi1.GetHashCode(), ei1.GetHashCode(), ci1.GetHashCode(), pai1.GetHashCode(),
                    mi2.GetHashCode(), pi2.GetHashCode(), fi2.GetHashCode(), ei2.GetHashCode(), ci2.GetHashCode(), pai2.GetHashCode());
            }, options);
        }

        private static void AssertMembersAreNotNull(MethodInfo mi, PropertyInfo pi, FieldInfo fi, EventInfo ei, ConstructorInfo ci, ParameterInfo pai)
        {
            Assert.NotNull(mi);
            Assert.NotNull(pi);
            Assert.NotNull(fi);
            Assert.NotNull(ei);
            Assert.NotNull(ci);
            Assert.NotNull(pai);
        }

        private static void AssertHashCodesAreEqual(int mi1Hash, int pi1Hash, int fi1Hash, int ei1Hash, int ci1Hash, int pai1Hash,
            int mi2Hash, int pi2Hash, int fi2Hash, int ei2Hash, int ci2Hash, int pai2Hash)
        {
            Assert.Equal(mi1Hash, mi2Hash);
            Assert.Equal(pi1Hash, pi2Hash);
            Assert.Equal(fi1Hash, fi2Hash);
            Assert.Equal(ei1Hash, ei2Hash);
            Assert.Equal(ci1Hash, ci2Hash);
            Assert.Equal(pai1Hash, pai2Hash);
        }

        private static void AssertMemberReferencesNotSameButEqual(MethodInfo mi1, PropertyInfo pi1, FieldInfo fi1, EventInfo ei1, ConstructorInfo ci1, ParameterInfo pai1,
            MethodInfo mi2, PropertyInfo pi2, FieldInfo fi2, EventInfo ei2, ConstructorInfo ci2, ParameterInfo pai2)
        {
            Assert.NotSame(mi1, mi2);
            Assert.NotSame(pi1, pi2);
            Assert.NotSame(fi1, fi2);
            Assert.NotSame(ei1, ei2);
            Assert.NotSame(ci1, ci2);
            Assert.NotSame(pai1, pai2);

            Assert.Equal(mi1, mi2);
            Assert.Equal(pi1, pi2);
            Assert.Equal(fi1, fi2);
            Assert.Equal(ei1, ei2);
            Assert.Equal(ci1, ci2);
            Assert.Equal(pai1, pai2);
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
                ParameterInfo pai1 = mi1.GetParameters()[0];

                AssertMembersAreNotNull(mi1, pi1, fi1, ei1, ci1, pai1);

                clearCache(null);

                MethodInfo mi2 = s_type.GetMethod(nameof(Method));
                PropertyInfo pi2 = s_type.GetProperty(nameof(Property));
                FieldInfo fi2 = s_type.GetField(nameof(Field1));
                EventInfo ei2 = s_type.GetEvent(nameof(Event1));
                ConstructorInfo ci2 = s_type.GetConstructor(Type.EmptyTypes);
                ParameterInfo pai2 = mi2.GetParameters()[0];

                AssertMembersAreNotNull(mi2, pi2, fi2, ei2, ci2, pai2);

                // After the Cache cleared the references of the same members of the same type will be different
                // But they should be evaluated as Equal so that there were no issue using the same member after hot reload
                AssertMemberReferencesNotSameButEqual(mi1, pi1, fi1, ei1, ci1, pai1, mi2, pi2, fi2, ei2, ci2, pai2);

                // And the HashCode of a member before and after hot reload should produce same result 
                AssertHashCodesAreEqual(mi1.GetHashCode(), pi1.GetHashCode(), fi1.GetHashCode(), ei1.GetHashCode(), ci1.GetHashCode(), pai1.GetHashCode(),
                    mi2.GetHashCode(), pi2.GetHashCode(), fi2.GetHashCode(), ei2.GetHashCode(), ci2.GetHashCode(), pai2.GetHashCode());
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
