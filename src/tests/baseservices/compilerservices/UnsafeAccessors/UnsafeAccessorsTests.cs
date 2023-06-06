// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

using Xunit;

// These runtime tests are a minimum set to help validate
// runtime related test passes (e.g., CrossGen, IL round trip, etc).
// A complete set of testing can be found in the
// libraries System.Runtime.CompilerServices test suite.
static class UnsafeAccessorsTests
{
    class UserData
    {
        public const string FieldName = nameof(_f);
        public const string MethodName = nameof(GetF);
        public const string FieldValue = "Field";

        private string _f;
        private string GetF() => _f;
        private UserData()
        {
            _f = FieldValue;
        }
    }

    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    extern static UserData CallPrivateConstructor();

    [Fact]
    public static void ValidateUnsafeAccess_Constructor()
    {
        Console.WriteLine($"Running {nameof(ValidateUnsafeAccess_Constructor)}");

        var ud = CallPrivateConstructor();
        Assert.Equal(typeof(UserData), ud.GetType());
    }

    [Fact]
    public static void ValidateUnsafeAccess_Field()
    {
        Console.WriteLine($"Running {nameof(ValidateUnsafeAccess_Field)}");

        var ud = CallPrivateConstructor();
        Assert.Equal(UserData.FieldValue, AccessPrivateField(ud));

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name=UserData.FieldName)]
        extern static ref string AccessPrivateField(UserData d);
    }

    [Fact]
    public static void ValidateUnsafeAccess_Method()
    {
        Console.WriteLine($"Running {nameof(ValidateUnsafeAccess_Method)}");

        var ud = CallPrivateConstructor();
        Assert.Equal(UserData.FieldValue, CallPrivateMethod(ud));

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name=UserData.MethodName)]
        extern static string CallPrivateMethod(UserData d);
    }
}