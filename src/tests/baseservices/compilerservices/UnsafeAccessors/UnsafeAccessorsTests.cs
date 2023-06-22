// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Xunit;

static unsafe class UnsafeAccessorsTests
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
        public const string MethodNameAmbiguous = nameof(_Ambiguous);
        public const string MethodPointerName = nameof(_Pointer);
        public const string MethodCdeclCallConvBitName = nameof(_CdeclCallConvBit);
        public const string MethodStdcallCallConvBitName = nameof(_StdcallCallConvBit);
        public const string MethodManagedCallConvBitName = nameof(_ManagedCallConvBit);

        private static string _F = PrivateStatic;
        private string _f;

        public string Value => _f;

        private UserDataClass(string a) { _f = a; }
        private UserDataClass() { _f = Private; Prop = Private; }

        private static string _M(string s, ref string sr, in string si) => s;
        private string _m(string s, ref string sr, in string si) => s;

        private static void _MVV() {}
        private void _mvv() {}

        // The "init" is important to have here - custom modifier test.
        private string Prop { get; init; }

        // Used to validate ambiguity is handled via custom modifiers.
        private string _Ambiguous(delegate* unmanaged[Cdecl, MemberFunction]<void> fptr) => nameof(CallConvCdecl);
        private string _Ambiguous(delegate* unmanaged[Stdcall, MemberFunction]<void> fptr) => nameof(CallConvStdcall);

        // Used to validate pointer values.
        private static string _Pointer(void* ptr) => "void*";

        // Used to validate the embedded callconv bits in
        // ECMA-335 signatures for methods.
        private string _CdeclCallConvBit(delegate* unmanaged[Cdecl]<void> fptr) => nameof(CallConvCdecl);
        private string _StdcallCallConvBit(delegate* unmanaged[Stdcall]<void> fptr) => nameof(CallConvStdcall);
        private string _ManagedCallConvBit(delegate* <void> fptr) => "Managed";
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
    public static void Verify_CallDefaultCtorClass()
    {
        Console.WriteLine($"Running {nameof(Verify_CallDefaultCtorClass)}");

        var local = CallPrivateConstructorClass();
        Assert.Equal(nameof(UserDataClass), local.GetType().Name);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void Verify_CallCtorClass()
    {
        Console.WriteLine($"Running {nameof(Verify_CallCtorClass)}");

        var local = CallPrivateConstructorClass(PrivateArg);
        Assert.Equal(nameof(UserDataClass), local.GetType().Name);
        Assert.Equal(PrivateArg, local.Value);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void Verify_CallCtorValue()
    {
        Console.WriteLine($"Running {nameof(Verify_CallCtorValue)}");

        var local = CallPrivateConstructorValue(PrivateArg);
        Assert.Equal(nameof(UserDataValue), local.GetType().Name);
        Assert.Equal(PrivateArg, local.Value);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void Verify_CallCtorAsMethod()
    {
        Console.WriteLine($"Running {nameof(Verify_CallCtorAsMethod)}");

        UserDataClass ud = (UserDataClass)RuntimeHelpers.GetUninitializedObject(typeof(UserDataClass));
        Assert.Null(ud.Value);

        CallPrivateConstructor(ud, PrivateArg);
        Assert.Equal(PrivateArg, ud.Value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name=".ctor")]
        extern static void CallPrivateConstructor(UserDataClass _this, string a);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void Verify_CallCtorAsMethodValue()
    {
        Console.WriteLine($"Running {nameof(Verify_CallCtorAsMethodValue)}");

        UserDataValue ud = new();
        Assert.Equal(Private, ud.Value);

        CallPrivateConstructor(ref ud, PrivateArg);
        Assert.Equal(PrivateArg, ud.Value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name=".ctor")]
        extern static void CallPrivateConstructor(ref UserDataValue _this, string a);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void Verify_AccessStaticFieldClass()
    {
        Console.WriteLine($"Running {nameof(Verify_AccessStaticFieldClass)}");

        Assert.Equal(PrivateStatic, GetPrivateStaticField((UserDataClass)null));

        [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name=UserDataClass.StaticFieldName)]
        extern static ref string GetPrivateStaticField(UserDataClass d);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void Verify_AccessFieldClass()
    {
        Console.WriteLine($"Running {nameof(Verify_AccessFieldClass)}");

        var local = CallPrivateConstructorClass();
        Assert.Equal(Private, GetPrivateField(local));

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name=UserDataClass.FieldName)]
        extern static ref string GetPrivateField(UserDataClass d);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void Verify_AccessStaticFieldValue()
    {
        Console.WriteLine($"Running {nameof(Verify_AccessStaticFieldValue)}");

        Assert.Equal(PrivateStatic, GetPrivateStaticField(new UserDataValue()));

        [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name=UserDataValue.StaticFieldName)]
        extern static ref string GetPrivateStaticField(UserDataValue d);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void Verify_AccessFieldValue()
    {
        Console.WriteLine($"Running {nameof(Verify_AccessFieldValue)}");

        UserDataValue local = new();
        Assert.Equal(Private, GetPrivateField(ref local));

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name=UserDataValue.FieldName)]
        extern static ref string GetPrivateField(ref UserDataValue d);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void Verify_AccessStaticMethodClass()
    {
        Console.WriteLine($"Running {nameof(Verify_AccessStaticMethodClass)}");

        var sr = string.Empty;
        var si = string.Empty;
        Assert.Equal(PrivateStatic, GetPrivateStaticMethod((UserDataClass)null, PrivateStatic, ref sr, in si));

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name=UserDataClass.StaticMethodName)]
        extern static string GetPrivateStaticMethod(UserDataClass d, string s, ref string sr, in string si);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void Verify_AccessMethodClass()
    {
        Console.WriteLine($"Running {nameof(Verify_AccessMethodClass)}");

        var sr = string.Empty;
        var si = string.Empty;
        var local = CallPrivateConstructorClass();
        Assert.Equal(Private, GetPrivateMethod(local, Private, ref sr, in si));

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name=UserDataClass.MethodName)]
        extern static string GetPrivateMethod(UserDataClass d, string s, ref string sr, in string si);
    }

    // These are defined outside of the test to validate lookup using the name of
    // the declaration as opposed to the Name field.
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
    extern static void _MVV(UserDataClass d);
    [UnsafeAccessor(UnsafeAccessorKind.Method)]
    extern static void _mvv(UserDataClass d);

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void Verify_AccessStaticMethodVoidClass()
    {
        Console.WriteLine($"Running {nameof(Verify_AccessStaticMethodVoidClass)}");

        GetPrivateStaticMethod((UserDataClass)null);
        _MVV((UserDataClass)null);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name=UserDataClass.StaticMethodVoidName)]
        extern static void GetPrivateStaticMethod(UserDataClass d);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void Verify_AccessMethodVoidClass()
    {
        Console.WriteLine($"Running {nameof(Verify_AccessMethodVoidClass)}");

        var local = CallPrivateConstructorClass();
        GetPrivateMethod(local);
        _mvv(local);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name=UserDataClass.MethodVoidName)]
        extern static void GetPrivateMethod(UserDataClass d);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void Verify_AccessStaticMethodValue()
    {
        Console.WriteLine($"Running {nameof(Verify_AccessStaticMethodValue)}");

        var sr = string.Empty;
        var si = string.Empty;
        Assert.Equal(PrivateStatic, GetPrivateStaticMethod(new UserDataValue(), PrivateStatic, ref sr, in si));

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name=UserDataValue.StaticMethodName)]
        extern static string GetPrivateStaticMethod(UserDataValue d, string s, ref string sr, in string si);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void Verify_AccessMethodValue()
    {
        Console.WriteLine($"Running {nameof(Verify_AccessMethodValue)}");

        var sr = string.Empty;
        var si = string.Empty;
        UserDataValue local = new();
        Assert.Equal(Private, GetPrivateMethod(ref local, Private, ref sr, in si));

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name=UserDataValue.MethodName)]
        extern static string GetPrivateMethod(ref UserDataValue d, string s, ref string sr, in string si);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void Verify_IgnoreCustomModifier()
    {
        Console.WriteLine($"Running {nameof(Verify_IgnoreCustomModifier)}");

        var ud = CallPrivateConstructorClass();
        Assert.Equal(Private, CallPrivateGetter(ud));

        const string newValue = "NewPropValue";
        CallPrivateSetter(ud, newValue);
        Assert.Equal(newValue, CallPrivateGetter(ud));

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name="get_Prop")]
        extern static string CallPrivateGetter(UserDataClass d);

        // Private setter used with "init" to validate default "ignore custom modifier" logic
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name="set_Prop")]
        extern static void CallPrivateSetter(UserDataClass d, string v);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void Verify_PreciseMatchCustomModifier()
    {
        Console.WriteLine($"Running {nameof(Verify_PreciseMatchCustomModifier)}");

        var ud = CallPrivateConstructorClass();
        Assert.Equal(nameof(CallConvStdcall), CallPrivateMethod(ud, null));

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name=UserDataClass.MethodNameAmbiguous)]
        extern static string CallPrivateMethod(UserDataClass d, delegate* unmanaged[Stdcall, MemberFunction]<void> fptr);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void Verify_UnmanagedCallConvBitAreTreatedAsCustomModifiersAndIgnored()
    {
        Console.WriteLine($"Running {nameof(Verify_UnmanagedCallConvBitAreTreatedAsCustomModifiersAndIgnored)}");

        var ud = CallPrivateConstructorClass();
        Assert.Equal(nameof(CallConvCdecl), CallCdeclMethod(ud, null));
        Assert.Equal(nameof(CallConvStdcall), CallStdcallMethod(ud, null));

        // The names of the declarations don't match the calling conventions in the function
        // pointer signature, this is by design for this test. The intent here is to validate that
        // calling conventions, when encoded in the ECMA-335 bits, are ignored on the first pass
        // in the same way custom modifiers are ignored.
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name=UserDataClass.MethodCdeclCallConvBitName)]
        extern static string CallCdeclMethod(UserDataClass d, delegate* unmanaged[Stdcall]<void> fptr);

        // See comment above regarding naming.
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name=UserDataClass.MethodStdcallCallConvBitName)]
        extern static string CallStdcallMethod(UserDataClass d, delegate* unmanaged[Cdecl]<void> fptr);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void Verify_ManagedUnmanagedFunctionPointersDontMatch()
    {
        Console.WriteLine($"Running {nameof(Verify_ManagedUnmanagedFunctionPointersDontMatch)}");

        var ud = CallPrivateConstructorClass();
        Assert.Throws<MissingMethodException>(() => CallCdeclMethod(ud, null));
        Assert.Throws<MissingMethodException>(() => CallManagedMethod(ud, null));

        // Managed calling conventions don't match on unmanaged function pointers
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name=UserDataClass.MethodCdeclCallConvBitName)]
        extern static string CallCdeclMethod(UserDataClass d, delegate* <void> fptr);

        // Unmanaged calling conventions don't match on managed function pointers
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name=UserDataClass.MethodManagedCallConvBitName)]
        extern static string CallManagedMethod(UserDataClass d, delegate* unmanaged[Cdecl]<void> fptr);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void Verify_InvalidTargetUnsafeAccessor()
    {
        Console.WriteLine($"Running {nameof(Verify_InvalidTargetUnsafeAccessor)}");

        bool isNativeAot = TestLibrary.Utilities.IsNativeAot;
        const string DoesNotExist = "_DoesNotExist_";
        AssertExtensions.ThrowsMissingMemberException<MissingMethodException>(
            isNativeAot ? null : DoesNotExist,
            () => MethodNotFound(null));
        AssertExtensions.ThrowsMissingMemberException<MissingMethodException>(
            isNativeAot ? null : DoesNotExist,
            () => StaticMethodNotFound(null));

        AssertExtensions.ThrowsMissingMemberException<MissingFieldException>(
            isNativeAot ? null : DoesNotExist,
            () => FieldNotFound(null));
        AssertExtensions.ThrowsMissingMemberException<MissingFieldException>(
            isNativeAot ? null : DoesNotExist,
            () => StaticFieldNotFound(null));

        AssertExtensions.ThrowsMissingMemberException<MissingMethodException>(
            isNativeAot ? null : UserDataClass.MethodPointerName,
            () => CallPointerMethod(null, null));

        Assert.Throws<AmbiguousMatchException>(
            () => CallAmbiguousMethod(CallPrivateConstructorClass(), null));

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name=DoesNotExist)]
        extern static void MethodNotFound(UserDataClass d);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name=DoesNotExist)]
        extern static void StaticMethodNotFound(UserDataClass d);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name=DoesNotExist)]
        extern static ref string FieldNotFound(UserDataClass d);

        [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name=DoesNotExist)]
        extern static ref string StaticFieldNotFound(UserDataClass d);

        // Pointers generally degrade to `void*`, but that isn't true for UnsafeAccessor signature validation.
        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name=UserDataClass.MethodPointerName)]
        extern static string CallPointerMethod(UserDataClass d, delegate* unmanaged[Stdcall]<void> fptr);

        // This is an ambiguous match since there are two methods each with two custom modifiers.
        // Therefore the default "ignore custom modifiers" logic fails. The fallback is for a
        // precise match and that also fails because the custom modifiers don't match precisely.
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name=UserDataClass.MethodNameAmbiguous)]
        extern static string CallAmbiguousMethod(UserDataClass d, delegate* unmanaged[Stdcall, SuppressGCTransition]<void> fptr);
    }

    class Invalid
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name=nameof(ToString))]
        public extern string NonStatic(string a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name=nameof(ToString))]
        public static extern string CallToString<U>(U a);
    }

    class Invalid<T>
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name=nameof(ToString))]
        public static extern string CallToString(T a);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/86040", TestRuntimes.Mono)]
    public static void Verify_InvalidUseUnsafeAccessor()
    {
        Console.WriteLine($"Running {nameof(Verify_InvalidUseUnsafeAccessor)}");

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
        Assert.Throws<BadImageFormatException>(() => InvalidCtorSignatureClass());
        Assert.Throws<BadImageFormatException>(() => InvalidCtorSignatureValue());
        Assert.Throws<BadImageFormatException>(() => InvalidCtorName());
        Assert.Throws<BadImageFormatException>(() => InvalidCtorType());
        Assert.Throws<BadImageFormatException>(() => LookUpFailsOnPointers(null));
        Assert.Throws<BadImageFormatException>(() => LookUpFailsOnFunctionPointers(null));
        Assert.Throws<BadImageFormatException>(() => new Invalid().NonStatic(string.Empty));
        Assert.Throws<BadImageFormatException>(() => Invalid.CallToString<string>(string.Empty));
        Assert.Throws<BadImageFormatException>(() => Invalid<string>.CallToString(string.Empty));

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
        extern static ref UserDataClass InvalidCtorSignatureClass();

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        extern static ref UserDataValue InvalidCtorSignatureValue();

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