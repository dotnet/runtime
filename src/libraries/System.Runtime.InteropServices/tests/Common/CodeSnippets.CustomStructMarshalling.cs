// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop.UnitTests
{
    internal static partial class CodeSnippets
    {
        public static class CustomStructMarshalling
        {
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
            public static string NonStaticMarshallerEntryPoint => BasicParameterByValue("S")
                + NonBlittableUserDefinedType()
                + NonStatic;

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
                public static string ManagedToNativeOnlyOutParameter => BasicParameterWithByRefModifier("out", "S")
                    + NonBlittableUserDefinedType()
                    + In;

                public static string NativeToManagedOnlyOutParameter => BasicParameterWithByRefModifier("out", "S")
                    + NonBlittableUserDefinedType()
                    + Out;

                public static string NativeToManagedFinallyOnlyOutParameter => BasicParameterWithByRefModifier("out", "S")
                    + NonBlittableUserDefinedType()
                    + OutGuaranteed;

                public static string ManagedToNativeOnlyReturnValue => BasicReturnType("S")
                    + NonBlittableUserDefinedType()
                    + In;

                public static string NativeToManagedOnlyReturnValue => BasicReturnType("S")
                    + NonBlittableUserDefinedType()
                    + Out;

                public static string NativeToManagedFinallyOnlyReturnValue => BasicReturnType("S")
                    + NonBlittableUserDefinedType()
                    + Out;

                public static string NativeToManagedOnlyInParameter => BasicParameterWithByRefModifier("in", "S")
                    + NonBlittableUserDefinedType()
                    + Out;

                public static string ParametersAndModifiers = BasicParametersAndModifiers("S", UsingSystemRuntimeInteropServicesMarshalling)
                    + NonBlittableUserDefinedType(defineNativeMarshalling: true)
                    + Default;

                public static string MarshalUsingParametersAndModifiers = MarshalUsingParametersAndModifiers("S", "Marshaller")
                    + NonBlittableUserDefinedType(defineNativeMarshalling: false)
                    + Default;

                public static string ByValueInParameter => BasicParameterByValue("S")
                    + NonBlittableUserDefinedType()
                    + In;

                public static string StackallocByValueInParameter => BasicParameterByValue("S")
                    + NonBlittableUserDefinedType()
                    + InBuffer;

                public static string PinByValueInParameter => BasicParameterByValue("S")
                    + NonBlittableUserDefinedType()
                    + InPinnable;

                public static string StackallocParametersAndModifiersNoRef = BasicParametersAndModifiersNoRef("S")
                    + NonBlittableUserDefinedType()
                    + InOutBuffer;

                public static string RefParameter = BasicParameterWithByRefModifier("ref", "S")
                    + NonBlittableUserDefinedType()
                    + Ref;

                public static string StackallocOnlyRefParameter = BasicParameterWithByRefModifier("ref", "S")
                    + NonBlittableUserDefinedType()
                    + InOutBuffer;

                public static string OptionalStackallocParametersAndModifiers = BasicParametersAndModifiers("S", UsingSystemRuntimeInteropServicesMarshalling)
                    + NonBlittableUserDefinedType()
                    + DefaultOptionalBuffer;

                public static string DefaultModeByValueInParameter => BasicParameterByValue("S")
                    + NonBlittableUserDefinedType()
                    + DefaultIn;

                public static string DefaultModeReturnValue => BasicReturnType("S")
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
                public static string ManagedToNativeOnlyOutParameter => BasicParameterWithByRefModifier("out", "S")
                    + NonBlittableUserDefinedType()
                    + In;

                public static string NativeToManagedOnlyOutParameter => BasicParameterWithByRefModifier("out", "S")
                    + NonBlittableUserDefinedType()
                    + Out;

                public static string NativeToManagedFinallyOnlyOutParameter => BasicParameterWithByRefModifier("out", "S")
                    + NonBlittableUserDefinedType()
                    + OutGuaranteed;

                public static string ManagedToNativeOnlyReturnValue => BasicReturnType("S")
                    + NonBlittableUserDefinedType()
                    + In;

                public static string NativeToManagedOnlyReturnValue => BasicReturnType("S")
                    + NonBlittableUserDefinedType()
                    + Out;

                public static string NativeToManagedFinallyOnlyReturnValue => BasicReturnType("S")
                    + NonBlittableUserDefinedType()
                    + Out;

                public static string NativeToManagedOnlyInParameter => BasicParameterWithByRefModifier("in", "S")
                    + NonBlittableUserDefinedType()
                    + Out;

                public static string ParametersAndModifiers = BasicParametersAndModifiers("S", UsingSystemRuntimeInteropServicesMarshalling)
                    + NonBlittableUserDefinedType(defineNativeMarshalling: true)
                    + Default;

                public static string ParametersAndModifiersWithFree = BasicParametersAndModifiers("S", UsingSystemRuntimeInteropServicesMarshalling)
                    + NonBlittableUserDefinedType(defineNativeMarshalling: true)
                    + DefaultWithFree;

                public static string ParametersAndModifiersWithOnInvoked = BasicParametersAndModifiers("S", UsingSystemRuntimeInteropServicesMarshalling)
                    + NonBlittableUserDefinedType(defineNativeMarshalling: true)
                    + DefaultWithOnInvoked;

                public static string MarshalUsingParametersAndModifiers = MarshalUsingParametersAndModifiers("S", "Marshaller")
                    + NonBlittableUserDefinedType(defineNativeMarshalling: false)
                    + Default;

                public static string ByValueInParameter => BasicParameterByValue("S")
                    + NonBlittableUserDefinedType()
                    + In;

                public static string StackallocByValueInParameter => BasicParameterByValue("S")
                    + NonBlittableUserDefinedType()
                    + InBuffer;

                public static string PinByValueInParameter => BasicParameterByValue("S")
                    + NonBlittableUserDefinedType()
                    + InStatelessPinnable;

                public static string MarshallerPinByValueInParameter => BasicParameterByValue("S")
                    + NonBlittableUserDefinedType()
                    + InPinnable;

                public static string StackallocParametersAndModifiersNoRef = BasicParametersAndModifiersNoRef("S")
                    + NonBlittableUserDefinedType()
                    + InOutBuffer;

                public static string RefParameter = BasicParameterWithByRefModifier("ref", "S")
                    + NonBlittableUserDefinedType()
                    + Ref;

                public static string StackallocOnlyRefParameter = BasicParameterWithByRefModifier("ref", "S")
                    + NonBlittableUserDefinedType()
                    + InOutBuffer;

                public static string OptionalStackallocParametersAndModifiers = BasicParametersAndModifiers("S", UsingSystemRuntimeInteropServicesMarshalling)
                    + NonBlittableUserDefinedType()
                    + DefaultOptionalBuffer;

                public static string DefaultModeByValueInParameter => BasicParameterByValue("S")
                    + NonBlittableUserDefinedType()
                    + DefaultIn;

                public static string DefaultModeReturnValue => BasicReturnType("S")
                    + NonBlittableUserDefinedType()
                    + DefaultOut;
            }
        }
    }
}
