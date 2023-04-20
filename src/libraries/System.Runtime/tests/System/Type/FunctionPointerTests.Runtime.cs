// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Tests.Types
{
    // These are runtime-specific tests not shared with MetadataLoadContext.
    // Using arrays in the manner below allows for use of the "is" keyword.
    // The use of 'object' will call into the runtime to compare but using a strongly-typed
    // function pointer without 'object' causes C# to hard-code the result.
    public partial class FunctionPointerTests
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void CompileTimeIdentity_Managed()
        {
            object obj = new delegate*<int>[1];
            Assert.True(obj is delegate*<int>[]);
            Assert.False(obj is delegate*<bool>[]);

            var fn = new delegate*<int>[1];
            Assert.True(fn is delegate*<int>[]);
#pragma warning disable CS0184 // 'is' expression's given expression is never of the provided type
            Assert.False(fn is delegate*<bool>[]);
#pragma warning restore CS0184
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void CompileTimeIdentity_ManagedWithMods()
        {
            object obj = new delegate*<ref int, void>[1];
            Assert.True(obj is delegate*<out int, void>[]);
            Assert.True(obj is delegate*<in int, void>[]);

            var fn = new delegate*<ref int, void>[1];
#pragma warning disable CS0184
            Assert.False(fn is delegate*<out int, void>[]);
            Assert.False(fn is delegate*<in int, void>[]);
#pragma warning restore CS0184
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void CompileTimeIdentity_Unmanaged()
        {
            object obj = new delegate* unmanaged[MemberFunction]<void>[1];
            Assert.True(obj is delegate* unmanaged<void>[]);
            Assert.True(obj is delegate* unmanaged[SuppressGCTransition]<void>[]);
            Assert.True(obj is delegate* unmanaged[MemberFunction, SuppressGCTransition]<void>[]);

            var fn = new delegate* unmanaged[MemberFunction]<void>[1];
#pragma warning disable CS0184
            Assert.False(fn is delegate* unmanaged<void>[]);
            Assert.False(fn is delegate* unmanaged[SuppressGCTransition]<void>[]);
            Assert.False(fn is delegate* unmanaged[MemberFunction, SuppressGCTransition]<void>[]);
#pragma warning restore CS0184
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void CompileTimeIdentity_UnmanagedIsPartOfIdentity()
        {
            object obj = new delegate* unmanaged[MemberFunction]<void>[1];
            Assert.False(obj is delegate*<void>[]);

            var fn = new delegate* unmanaged[MemberFunction]<void>[1];
#pragma warning disable CS0184
            Assert.False(fn is delegate*<void>[]);
#pragma warning restore CS0184

            object obj2 = new delegate* unmanaged[Cdecl]<void>[1];
            Assert.False(obj2 is delegate*<void>[]);

            var fn2 = new delegate* unmanaged[Cdecl]<void>[1];
#pragma warning disable CS0184
            Assert.False(fn2 is delegate*<void>[]);
#pragma warning restore CS0184
        }
    }
}


