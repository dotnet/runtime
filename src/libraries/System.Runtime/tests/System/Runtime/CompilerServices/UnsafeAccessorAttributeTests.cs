// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

using Xunit;

// CS0414: The field '<name>' is assigned but its value is never used
#pragma warning disable 0414

namespace System.Runtime.CompilerServices.Tests;

public static class UnsafeAccessorAttributeTests
{
    const string PrivateStatic = nameof(PrivateStatic);
    const string Private = nameof(Private);
    const string PrivateArg = nameof(PrivateArg);

    class UserDataClass
    {
        public const string StaticFieldName = nameof(_F);
        public const string FieldName = nameof(_f);
        public const string StaticMethodName = nameof(_M);
        public const string MethodName = nameof(_m);
        public const string StaticMethodVoidName = nameof(_MVV);
        public const string MethodVoidName = nameof(_mvv);

        private static string _F = PrivateStatic;
        private string _f;

        public string Value => _f;

        private UserDataClass(string a) { _f = a; }
        private UserDataClass() { _f = Private; }

        private static string _M(string s, ref string sr, in string si) => s;
        private string _m(string s, ref string sr, in string si) => s;

        private static void _MVV() {}
        private void _mvv() {}
    }

    [StructLayout(LayoutKind.Sequential)]
    struct UserDataValue
    {
        public const string StaticFieldName = nameof(_F);
        public const string FieldName = nameof(_f);
        public const string StaticMethodName = nameof(_M);
        public const string MethodName = nameof(_m);

        private static string _F = PrivateStatic;
        private string _f;

        public string Value => _f;

        private UserDataValue(string a) { _f = a; }

        // ValueClass are not permitted to define a private default constructor.
        public UserDataValue() { _f = Private; }

        private static string _M(string s, ref string sr, in string si) => s;
        private string _m(string s, ref string sr, in string si) => s;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    extern static UserDataClass CallPrivateConstructorClass();

    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    extern static UserDataClass CallPrivateConstructorClass(string a);

    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    extern static UserDataValue CallPrivateConstructorValue(string a);

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void VerifyCallDefaultCtorClass()
    {
        var local = CallPrivateConstructorClass();
        Assert.Equal(nameof(UserDataClass), local.GetType().Name);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void VerifyCallCtorClass()
    {
        var local = CallPrivateConstructorClass(PrivateArg);
        Assert.Equal(nameof(UserDataClass), local.GetType().Name);
        Assert.Equal(PrivateArg, local.Value);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void VerifyCallCtorValue()
    {
        var local = CallPrivateConstructorValue(PrivateArg);
        Assert.Equal(nameof(UserDataValue), local.GetType().Name);
        Assert.Equal(PrivateArg, local.Value);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void VerifyAccessStaticFieldClass()
    {
        Assert.Equal(PrivateStatic, GetPrivateStaticField((UserDataClass)null));

        [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name=UserDataClass.StaticFieldName)]
        extern static ref string GetPrivateStaticField(UserDataClass d);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void VerifyAccessFieldClass()
    {
        var local = CallPrivateConstructorClass();
        Assert.Equal(Private, GetPrivateField(local));

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name=UserDataClass.FieldName)]
        extern static ref string GetPrivateField(UserDataClass d);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void VerifyAccessStaticFieldValue()
    {
        Assert.Equal(PrivateStatic, GetPrivateStaticField(new UserDataValue()));

        [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name=UserDataValue.StaticFieldName)]
        extern static ref string GetPrivateStaticField(UserDataValue d);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void VerifyAccessFieldValue()
    {
        UserDataValue local = new();
        Assert.Equal(Private, GetPrivateField(ref local));

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name=UserDataValue.FieldName)]
        extern static ref string GetPrivateField(ref UserDataValue d);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void VerifyAccessStaticMethodClass()
    {
        var sr = string.Empty;
        var si = string.Empty;
        Assert.Equal(PrivateStatic, GetPrivateStaticMethod((UserDataClass)null, PrivateStatic, ref sr, in si));

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name=UserDataClass.StaticMethodName)]
        extern static string GetPrivateStaticMethod(UserDataClass d, string s, ref string sr, in string si);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void VerifyAccessMethodClass()
    {
        var sr = string.Empty;
        var si = string.Empty;
        var local = CallPrivateConstructorClass();
        Assert.Equal(Private, GetPrivateMethod(local, Private, ref sr, in si));

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name=UserDataClass.MethodName)]
        extern static string GetPrivateMethod(UserDataClass d, string s, ref string sr, in string si);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void VerifyAccessStaticMethodVoidClass()
    {
        GetPrivateStaticMethod((UserDataClass)null);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name=UserDataClass.StaticMethodVoidName)]
        extern static void GetPrivateStaticMethod(UserDataClass d);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void VerifyAccessMethodVoidClass()
    {
        var local = CallPrivateConstructorClass();
        GetPrivateMethod(local);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name=UserDataClass.MethodVoidName)]
        extern static void GetPrivateMethod(UserDataClass d);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void VerifyAccessStaticMethodValue()
    {
        var sr = string.Empty;
        var si = string.Empty;
        Assert.Equal(PrivateStatic, GetPrivateStaticMethod(new UserDataValue(), PrivateStatic, ref sr, in si));

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name=UserDataValue.StaticMethodName)]
        extern static string GetPrivateStaticMethod(UserDataValue d, string s, ref string sr, in string si);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void VerifyAccessMethodValue()
    {
        var sr = string.Empty;
        var si = string.Empty;
        UserDataValue local = new();
        Assert.Equal(Private, GetPrivateMethod(ref local, Private, ref sr, in si));

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name=UserDataValue.MethodName)]
        extern static string GetPrivateMethod(ref UserDataValue d, string s, ref string sr, in string si);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static unsafe void VerifyInvalidTargetUnsafeAccessor()
    {
        Assert.Throws<MissingMethodException>(() => MethodNotFound(null));
        Assert.Throws<MissingMethodException>(() => StaticMethodNotFound(null));

        Assert.Throws<MissingFieldException>(() => FieldNotFound(null));
        Assert.Throws<MissingFieldException>(() => StaticFieldNotFound(null));

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name="_DoesNotExist_")]
        extern static void MethodNotFound(UserDataClass d);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name="_DoesNotExist_")]
        extern static void StaticMethodNotFound(UserDataClass d);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name="_DoesNotExist_")]
        extern static ref string FieldNotFound(UserDataClass d);

        [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name="_DoesNotExist_")]
        extern static ref string StaticFieldNotFound(UserDataClass d);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static unsafe void VerifyInvalidUseUnsafeAccessor()
    {
        Assert.Throws<BadImageFormatException>(() => FieldReturnMustBeByRefClass((UserDataClass)null));
        Assert.Throws<BadImageFormatException>(() =>
        {
            UserDataValue local = new();
            FieldReturnMustBeByRefValue(ref local);
        });
        Assert.Throws<BadImageFormatException>(() => FieldArgumentMustBeByRef(new UserDataValue()));
        Assert.Throws<BadImageFormatException>(() => FieldMustHaveSingleArgument((UserDataClass)null, 0));
        Assert.Throws<BadImageFormatException>(() => StaticFieldMustHaveSingleArgument((UserDataClass)null, 0));
        Assert.Throws<BadImageFormatException>(() => InvalidKindValue(null));
        Assert.Throws<BadImageFormatException>(() => InvalidCtorSignature());
        Assert.Throws<BadImageFormatException>(() => InvalidCtorName());
        Assert.Throws<BadImageFormatException>(() => InvalidCtorType());
        Assert.Throws<BadImageFormatException>(() => LookUpFailsOnPointers(null));
        Assert.Throws<BadImageFormatException>(() => LookUpFailsOnFunctionPointers(null));

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name=UserDataValue.FieldName)]
        extern static string FieldReturnMustBeByRefClass(UserDataClass d);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name=UserDataValue.FieldName)]
        extern static string FieldReturnMustBeByRefValue(ref UserDataValue d);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name=UserDataValue.FieldName)]
        extern static ref string FieldArgumentMustBeByRef(UserDataValue d);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name=UserDataValue.FieldName)]
        extern static ref string FieldMustHaveSingleArgument(UserDataClass d, int a);

        [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name=UserDataValue.StaticFieldName)]
        extern static ref string StaticFieldMustHaveSingleArgument(UserDataClass d, int a);

        [UnsafeAccessor((UnsafeAccessorKind)100, Name=UserDataClass.StaticMethodVoidName)]
        extern static void InvalidKindValue(UserDataClass d);

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        extern static ref UserDataClass InvalidCtorSignature();

        [UnsafeAccessor(UnsafeAccessorKind.Constructor, Name="_ShouldBeNull_")]
        extern static UserDataClass InvalidCtorName();

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        extern static void InvalidCtorType();

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name=nameof(ToString))]
        extern static string LookUpFailsOnPointers(void* d);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name=nameof(ToString))]
        extern static string LookUpFailsOnFunctionPointers(delegate* <void> fptr);
    }
}