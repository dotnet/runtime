// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;
using static DisabledRuntimeMarshallingNative;

namespace DisabledRuntimeMarshalling.PInvokeAssemblyMarshallingDisabled;

[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
public class PInvokes
{

    [Fact]
    public static void StructWithDefaultNonBlittableFields_MarshalAsInfo()
    {
        short s = 41;
        bool b = true;

        Assert.True(DisabledRuntimeMarshallingNative.CheckStructWithShortAndBool(new StructWithShortAndBoolWithMarshalAs(s, b), s, b));

        // We use a the "green check mark" character so that we use both bytes and
        // have a value that can't be accidentally round-tripped.
        char c = '\u2705';
        Assert.True(DisabledRuntimeMarshallingNative.CheckStructWithWCharAndShort(new StructWithWCharAndShortWithMarshalAs(s, c), s, c));
    }

    [Fact]
    public static void Strings_NotSupported()
    {
        Assert.Throws<MarshalDirectiveException>(() => DisabledRuntimeMarshallingNative.CheckStringWithAnsiCharSet(""));
        Assert.Throws<MarshalDirectiveException>(() => DisabledRuntimeMarshallingNative.CheckStringWithUnicodeCharSet(""));
        Assert.Throws<MarshalDirectiveException>(() => DisabledRuntimeMarshallingNative.CheckStructWithStructWithString(new StructWithString("")));
        Assert.Throws<MarshalDirectiveException>(() => DisabledRuntimeMarshallingNative.GetStringWithUnicodeCharSet());
    }

    [Fact]
    public static void LayoutClass_NotSupported()
    {
        Assert.Throws<MarshalDirectiveException>(() => DisabledRuntimeMarshallingNative.CheckLayoutClass(new LayoutClass()));
    }

    [Fact]
    public static void SetLastError_NotSupported()
    {
        Assert.Throws<MarshalDirectiveException>(() => DisabledRuntimeMarshallingNative.CallWithSetLastError());
    }

    [Fact]
    [SkipOnMono("Mono does not support LCIDs at all and it is not worth the effort to add support just to make it throw an exception.")]
    public static void LCID_NotSupported()
    {
        Assert.Throws<MarshalDirectiveException>(() => DisabledRuntimeMarshallingNative.CallWithLCID());
    }

    [Fact]
    [SkipOnMono("Mono does not support PreserveSig=False in P/Invokes and it is not worth the effort to add support just to make it throw an exception.")]
    public static void PreserveSig_False_NotSupported()
    {
        Assert.Throws<MarshalDirectiveException>(() => DisabledRuntimeMarshallingNative.CallWithHResultSwap());
    }

    [Fact]
    public static void Varargs_NotSupported()
    {
        AssertThrowsCorrectException(() => DisabledRuntimeMarshallingNative.CallWithVarargs(__arglist(1, 2, 3)));

        static void AssertThrowsCorrectException(Action testCode)
        {
            try
            {
                testCode();
                return;
            }
#pragma warning disable CS0618 // ExecutionEngineException has an ObsoleteAttribute
            catch (Exception ex) when(ex is MarshalDirectiveException or InvalidProgramException or ExecutionEngineException)
#pragma warning restore CS0618
            {
                // CoreCLR is expected to throw MarshalDirectiveException
                // JIT-enabled Mono is expected to throw an InvalidProgramException
                // AOT-only Mono throws an ExecutionEngineException when attempting to JIT the callback that would throw the InvalidProgramException.
                return;
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected either a MarshalDirectiveException, InvalidProgramException, or ExecutionEngineException, but received a '{ex.GetType().FullName}' exception: '{ex.ToString()}'");
            }
            Assert.Fail($"Expected either a MarshalDirectiveException, InvalidProgramException, or ExecutionEngineException, but received no exception.");
        }
    }

    [Fact]
    public static void ByRef_Args_Not_Supported()
    {
        Assert.Throws<MarshalDirectiveException>(() =>
        {
           int i = 0;
           DisabledRuntimeMarshallingNative.CallWithByRef(ref i);
        });
    }

    [Fact]
    public static void NoBooleanNormalization()
    {
        byte byteVal = 42;
        bool boolVal = DisabledRuntimeMarshallingNative.GetByteAsBool(byteVal);
        Assert.Equal(byteVal, Unsafe.As<bool, byte>(ref Unsafe.AsRef(in boolVal)));
    }

    [Fact]
    public static void StructWithDefaultNonBlittableFields_DoesNotDoMarshalling()
    {
        short s = 42;
        bool b = true;

        Assert.True(DisabledRuntimeMarshallingNative.CheckStructWithShortAndBool(new StructWithShortAndBool(s, b), s, b));

        // We use a the "green check mark" character so that we use both bytes and
        // have a value that can't be accidentally round-tripped.
        char c = '\u2705';
        Assert.True(DisabledRuntimeMarshallingNative.CheckStructWithWCharAndShort(new StructWithWCharAndShort(s, c), s, c));

        Assert.False(DisabledRuntimeMarshallingNative.CheckStructWithShortAndBoolWithVariantBool(new StructWithShortAndBool(s, b), s, b));
    }

    [Fact]
    public static void StructWithNonBlittableGenericInstantiation()
    {
        short s = 42;
        // We use a the "green check mark" character so that we use both bytes and
        // have a value that can't be accidentally round-tripped.
        char c = '\u2705';
        Assert.True(DisabledRuntimeMarshallingNative.CheckStructWithWCharAndShort(new StructWithShortAndGeneric<char>(s, c), s, c));
    }

    [Fact]
    public static void StructWithBlittableGenericInstantiation()
    {
        short s = 42;
        // We use a the "green check mark" character so that we use both bytes and
        // have a value that can't be accidentally round-tripped.
        char c = '\u2705';
        Assert.True(DisabledRuntimeMarshallingNative.CheckStructWithWCharAndShort(new StructWithShortAndGeneric<short>(s, (short)c), s, (short)c));
    }

    [Fact]
    public static void CanUseEnumsWithDisabledMarshalling()
    {
        Assert.Equal((byte)ByteEnum.Value, DisabledRuntimeMarshallingNative.GetEnumUnderlyingValue(ByteEnum.Value));
    }

    [Fact]
    [SkipOnMono("Blocking this on CoreCLR should be good enough.")]
    public static void UInt128_Int128_NotSupported()
    {
        Assert.Throws<MarshalDirectiveException>(() => DisabledRuntimeMarshallingNative.CallWithInt128(default(Int128)));
        Assert.Throws<MarshalDirectiveException>(() => DisabledRuntimeMarshallingNative.CallWithUInt128(default(UInt128)));
    }
}
