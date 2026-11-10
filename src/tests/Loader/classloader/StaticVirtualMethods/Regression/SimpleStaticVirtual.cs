// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace SimpleStaticVirtual
{
    public class Tests
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/122863", TestRuntimes.Mono)]
        public static void TestEntryPoint()
        {
            Assert.Equal(nameof(IGetter), Get<IGetter>());
            Assert.Equal("Object", GetVariant<IGetter<object>, string>());
        }

        static string Get<T>() where T : IGetter => T.Get();
        static string GetVariant<T, U>() where T : IGetter<U> => T.Get();
    }

    interface IGetter
    {
        static virtual string Get() => nameof(IGetter);
    }

    interface IGetter<in T>
    {
        static virtual string Get() => typeof(T).Name;
    }
}
