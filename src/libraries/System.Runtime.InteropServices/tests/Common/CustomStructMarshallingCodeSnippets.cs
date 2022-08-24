// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop.UnitTests
{
    public static class CustomStructMarshallingCodeSnippets<TSignatureTestProvider>
        where TSignatureTestProvider : ICustomMarshallingSignatureTestProvider
    {
        private static readonly string UsingSystemRuntimeInteropServicesMarshalling = "using System.Runtime.InteropServices.Marshalling;";

        public static string NonBlittableUserDefinedType(bool defineNativeMarshalling = true) => $@"
{(defineNativeMarshalling ? "[NativeMarshalling(typeof(Marshaller))]" : string.Empty)}
public struct S
{{
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    public bool b;
#pragma warning restore CS0649
}}
";
        private static string NonStatic = @"
[CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
public class Marshaller
{
    public struct Native { }

    public static Native ConvertToUnmanaged(S s) => default;
}
";
        public static string NonStaticMarshallerEntryPoint => TSignatureTestProvider.BasicParameterByValue("S")
            + NonBlittableUserDefinedType()
            + NonStatic;

        private static string Struct = @"
[CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
public struct Marshaller
{
    public struct Native { }

    public void FromManaged(S s) {}
    public Native ToUnmanaged() => default;
}
";
        public static string StructMarshallerEntryPoint => TSignatureTestProvider.BasicParameterByValue("S")
            + NonBlittableUserDefinedType()
            + Struct;


        public static class Stateless
        {
            private static string In = @"
[CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
public static class Marshaller
{
    public struct Native { }

    public static Native ConvertToUnmanaged(S s) => default;
}
";
            private static string InBuffer = @"
[CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
public static class Marshaller
{
    public struct Native { }

    public const int BufferSize = 0x100;
    public static Native ConvertToUnmanaged(S s, System.Span<byte> buffer) => default;
}
";

            public static string InPinnable = @"
[CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
public static unsafe class Marshaller
{
    public static byte* ConvertToUnmanaged(S s) => default;
    public static ref byte GetPinnableReference(S s) => throw null;
}
";
            private static string Out = @"
[CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedOut, typeof(Marshaller))]
public static class Marshaller
{
    public struct Native { }

    public static S ConvertToManaged(Native n) => default;
}
";
            private static string OutGuaranteed = @"
[CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedOut, typeof(Marshaller))]
public static class Marshaller
{
    public struct Native { }

    public static S ConvertToManagedFinally(Native n) => default;
}
";
            public static string Ref = @"
[CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedRef, typeof(Marshaller))]
public static class Marshaller
{
    public struct Native { }

    public static Native ConvertToUnmanaged(S s) => default;
    public static S ConvertToManaged(Native n) => default;
}
";
            public static string Default = @"
[CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
public static class Marshaller
{
    public struct Native { }

    public static Native ConvertToUnmanaged(S s) => default;
    public static S ConvertToManaged(Native n) => default;
}
";
            public static string InOutBuffer = @"
[CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
[CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedOut, typeof(Marshaller))]
public static class Marshaller
{
    public struct Native { }

    public const int BufferSize = 0x100;
    public static Native ConvertToUnmanaged(S s, System.Span<byte> buffer) => default;
    public static S ConvertToManaged(Native n) => default;
}
";
            public static string DefaultOptionalBuffer = @"
[CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
public static class Marshaller
{
    public struct Native { }

    public const int BufferSize = 0x100;
    public static Native ConvertToUnmanaged(S s) => default;
    public static Native ConvertToUnmanaged(S s, System.Span<byte> buffer) => default;
    public static S ConvertToManaged(Native n) => default;
}
";
            private static string DefaultIn = @"
[CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
public static class Marshaller
{
    public struct Native { }

    public static Native ConvertToUnmanaged(S s) => default;
}
";
            private static string DefaultOut = @"
[CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedOut, typeof(Marshaller))]
public static class Marshaller
{
    public struct Native { }

    public static S ConvertToManaged(Native n) => default;
}
";
            public static string ManagedToNativeOnlyOutParameter => TSignatureTestProvider.BasicParameterWithByRefModifier("out", "S")
                + NonBlittableUserDefinedType()
                + In;

            public static string NativeToManagedOnlyOutParameter => TSignatureTestProvider.BasicParameterWithByRefModifier("out", "S")
                + NonBlittableUserDefinedType()
                + Out;

            public static string NativeToManagedFinallyOnlyOutParameter => TSignatureTestProvider.BasicParameterWithByRefModifier("out", "S")
                + NonBlittableUserDefinedType()
                + OutGuaranteed;

            public static string ManagedToNativeOnlyReturnValue => TSignatureTestProvider.BasicReturnType("S")
                + NonBlittableUserDefinedType()
                + In;

            public static string NativeToManagedOnlyReturnValue => TSignatureTestProvider.BasicReturnType("S")
                + NonBlittableUserDefinedType()
                + Out;

            public static string NativeToManagedFinallyOnlyReturnValue => TSignatureTestProvider.BasicReturnType("S")
                + NonBlittableUserDefinedType()
                + Out;

            public static string NativeToManagedOnlyInParameter => TSignatureTestProvider.BasicParameterWithByRefModifier("in", "S")
                + NonBlittableUserDefinedType()
                + Out;

            public static string ParametersAndModifiers = TSignatureTestProvider.BasicParametersAndModifiers("S", UsingSystemRuntimeInteropServicesMarshalling)
                + NonBlittableUserDefinedType(defineNativeMarshalling: true)
                + Default;

            public static string MarshalUsingParametersAndModifiers = TSignatureTestProvider.MarshalUsingParametersAndModifiers("S", "Marshaller")
                + NonBlittableUserDefinedType(defineNativeMarshalling: false)
                + Default;

            public static string ByValueInParameter => TSignatureTestProvider.BasicParameterByValue("S")
                + NonBlittableUserDefinedType()
                + In;

            public static string StackallocByValueInParameter => TSignatureTestProvider.BasicParameterByValue("S")
                + NonBlittableUserDefinedType()
                + InBuffer;

            public static string PinByValueInParameter => TSignatureTestProvider.BasicParameterByValue("S")
                + NonBlittableUserDefinedType()
                + InPinnable;

            public static string StackallocParametersAndModifiersNoRef = TSignatureTestProvider.BasicParametersAndModifiersNoRef("S")
                + NonBlittableUserDefinedType()
                + InOutBuffer;

            public static string RefParameter = TSignatureTestProvider.BasicParameterWithByRefModifier("ref", "S")
                + NonBlittableUserDefinedType()
                + Ref;

            public static string StackallocOnlyRefParameter = TSignatureTestProvider.BasicParameterWithByRefModifier("ref", "S")
                + NonBlittableUserDefinedType()
                + InOutBuffer;

            public static string OptionalStackallocParametersAndModifiers = TSignatureTestProvider.BasicParametersAndModifiers("S", UsingSystemRuntimeInteropServicesMarshalling)
                + NonBlittableUserDefinedType()
                + DefaultOptionalBuffer;

            public static string DefaultModeByValueInParameter => TSignatureTestProvider.BasicParameterByValue("S")
                + NonBlittableUserDefinedType()
                + DefaultIn;

            public static string DefaultModeReturnValue => TSignatureTestProvider.BasicReturnType("S")
                + NonBlittableUserDefinedType()
                + DefaultOut;
        }

        public static class Stateful
        {
            private static string In = @"
[CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedIn, typeof(M))]
public static class Marshaller
{
    public struct Native { }

    public struct M
    {
        public void FromManaged(S s) {}
        public Native ToUnmanaged() => default;
    }
}
";

            public static string InStatelessPinnable = @"
[CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedIn, typeof(M))]
public static class Marshaller
{
    public unsafe struct M
    {
        public void FromManaged(S s) {}
        public byte* ToUnmanaged() => default;

        public static ref byte GetPinnableReference(S s) => throw null;
    }
}
";

            public static string InPinnable = @"
[CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedIn, typeof(M))]
public static class Marshaller
{
    public unsafe struct M
    {
        public void FromManaged(S s) {}
        public byte* ToUnmanaged() => default;

        public ref byte GetPinnableReference() => throw null;
    }
}
";

            private static string InBuffer = @"
[CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedIn, typeof(M))]
public static class Marshaller
{
    public struct Native { }

    public struct M
    {
        public const int BufferSize = 0x100;
        public void FromManaged(S s, System.Span<byte> buffer) {}
        public Native ToUnmanaged() => default;
    }
}
";
            private static string Out = @"
[CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedOut, typeof(M))]
public static class Marshaller
{
    public struct Native { }

    public struct M
    {
        public void FromUnmanaged(Native n) {}
        public S ToManaged() => default;
    }
}
";
            private static string OutGuaranteed = @"
[CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedOut, typeof(M))]
public static class Marshaller
{
    public struct Native { }

    public struct M
    {
        public void FromUnmanaged(Native n) {}
        public S ToManagedFinally() => default;
    }
}
";
            public static string Ref = @"
[CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedRef, typeof(M))]
public static class Marshaller
{
    public struct Native { }

    public struct M
    {
        public void FromManaged(S s) {}
        public Native ToUnmanaged() => default;
        public void FromUnmanaged(Native n) {}
        public S ToManaged() => default;
    }
}
";
            public static string Default = @"
[CustomMarshaller(typeof(S), MarshalMode.Default, typeof(M))]
public static class Marshaller
{
    public struct Native { }

    public struct M
    {
        public void FromManaged(S s) {}
        public Native ToUnmanaged() => default;
        public void FromUnmanaged(Native n) {}
        public S ToManaged() => default;
    }
}
";
            public static string DefaultWithFree = @"
[CustomMarshaller(typeof(S), MarshalMode.Default, typeof(M))]
public static class Marshaller
{
    public struct Native { }

    public struct M
    {
        public void FromManaged(S s) {}
        public Native ToUnmanaged() => default;
        public void FromUnmanaged(Native n) {}
        public S ToManaged() => default;
        public void Free() {}
    }
}
";
            public static string DefaultWithOnInvoked = @"
[CustomMarshaller(typeof(S), MarshalMode.Default, typeof(M))]
public static class Marshaller
{
    public struct Native { }

    public struct M
    {
        public void FromManaged(S s) {}
        public Native ToUnmanaged() => default;
        public void FromUnmanaged(Native n) {}
        public S ToManaged() => default;
        public void OnInvoked() {}
    }
}
";
            public static string InOutBuffer = @"
[CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedIn, typeof(M))]
[CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedOut, typeof(M))]
public static class Marshaller
{
    public struct Native { }

    public struct M
    {
        public const int BufferSize = 0x100;
        public void FromManaged(S s, System.Span<byte> buffer) {}
        public Native ToUnmanaged() => default;
        public void FromUnmanaged(Native n) {}
        public S ToManaged() => default;
    }
}
";
            public static string DefaultOptionalBuffer = @"
[CustomMarshaller(typeof(S), MarshalMode.Default, typeof(M))]
public static class Marshaller
{
    public struct Native { }

    public struct M
    {
        public const int BufferSize = 0x100;
        public void FromManaged(S s) {}
        public void FromManaged(S s, System.Span<byte> buffer) {}
        public Native ToUnmanaged() => default;
        public void FromUnmanaged(Native n) {}
        public S ToManaged() => default;
    }
}
";
            private static string DefaultIn = @"
[CustomMarshaller(typeof(S), MarshalMode.Default, typeof(M))]
public static class Marshaller
{
    public struct Native { }

    public struct M
    {
        public void FromManaged(S s) {}
        public Native ToUnmanaged() => default;
    }
}
";
            private static string DefaultOut = @"
[CustomMarshaller(typeof(S), MarshalMode.Default, typeof(M))]
public static class Marshaller
{
    public struct Native { }

    public struct M
    {
        public void FromUnmanaged(Native n) {}
        public S ToManaged() => default;
    }
}
";
            public static string ManagedToNativeOnlyOutParameter => TSignatureTestProvider.BasicParameterWithByRefModifier("out", "S")
                + NonBlittableUserDefinedType()
                + In;

            public static string NativeToManagedOnlyOutParameter => TSignatureTestProvider.BasicParameterWithByRefModifier("out", "S")
                + NonBlittableUserDefinedType()
                + Out;

            public static string NativeToManagedFinallyOnlyOutParameter => TSignatureTestProvider.BasicParameterWithByRefModifier("out", "S")
                + NonBlittableUserDefinedType()
                + OutGuaranteed;

            public static string ManagedToNativeOnlyReturnValue => TSignatureTestProvider.BasicReturnType("S")
                + NonBlittableUserDefinedType()
                + In;

            public static string NativeToManagedOnlyReturnValue => TSignatureTestProvider.BasicReturnType("S")
                + NonBlittableUserDefinedType()
                + Out;

            public static string NativeToManagedFinallyOnlyReturnValue => TSignatureTestProvider.BasicReturnType("S")
                + NonBlittableUserDefinedType()
                + Out;

            public static string NativeToManagedOnlyInParameter => TSignatureTestProvider.BasicParameterWithByRefModifier("in", "S")
                + NonBlittableUserDefinedType()
                + Out;

            public static string ParametersAndModifiers = TSignatureTestProvider.BasicParametersAndModifiers("S", UsingSystemRuntimeInteropServicesMarshalling)
                + NonBlittableUserDefinedType(defineNativeMarshalling: true)
                + Default;

            public static string ParametersAndModifiersWithFree = TSignatureTestProvider.BasicParametersAndModifiers("S", UsingSystemRuntimeInteropServicesMarshalling)
                + NonBlittableUserDefinedType(defineNativeMarshalling: true)
                + DefaultWithFree;

            public static string ParametersAndModifiersWithOnInvoked = TSignatureTestProvider.BasicParametersAndModifiers("S", UsingSystemRuntimeInteropServicesMarshalling)
                + NonBlittableUserDefinedType(defineNativeMarshalling: true)
                + DefaultWithOnInvoked;

            public static string MarshalUsingParametersAndModifiers = TSignatureTestProvider.MarshalUsingParametersAndModifiers("S", "Marshaller")
                + NonBlittableUserDefinedType(defineNativeMarshalling: false)
                + Default;

            public static string ByValueInParameter => TSignatureTestProvider.BasicParameterByValue("S")
                + NonBlittableUserDefinedType()
                + In;

            public static string StackallocByValueInParameter => TSignatureTestProvider.BasicParameterByValue("S")
                + NonBlittableUserDefinedType()
                + InBuffer;

            public static string PinByValueInParameter => TSignatureTestProvider.BasicParameterByValue("S")
                + NonBlittableUserDefinedType()
                + InStatelessPinnable;

            public static string MarshallerPinByValueInParameter => TSignatureTestProvider.BasicParameterByValue("S")
                + NonBlittableUserDefinedType()
                + InPinnable;

            public static string StackallocParametersAndModifiersNoRef = TSignatureTestProvider.BasicParametersAndModifiersNoRef("S")
                + NonBlittableUserDefinedType()
                + InOutBuffer;

            public static string RefParameter = TSignatureTestProvider.BasicParameterWithByRefModifier("ref", "S")
                + NonBlittableUserDefinedType()
                + Ref;

            public static string StackallocOnlyRefParameter = TSignatureTestProvider.BasicParameterWithByRefModifier("ref", "S")
                + NonBlittableUserDefinedType()
                + InOutBuffer;

            public static string OptionalStackallocParametersAndModifiers = TSignatureTestProvider.BasicParametersAndModifiers("S", UsingSystemRuntimeInteropServicesMarshalling)
                + NonBlittableUserDefinedType()
                + DefaultOptionalBuffer;

            public static string DefaultModeByValueInParameter => TSignatureTestProvider.BasicParameterByValue("S")
                + NonBlittableUserDefinedType()
                + DefaultIn;

            public static string DefaultModeReturnValue => TSignatureTestProvider.BasicReturnType("S")
                + NonBlittableUserDefinedType()
                + DefaultOut;
        }
    }
}
