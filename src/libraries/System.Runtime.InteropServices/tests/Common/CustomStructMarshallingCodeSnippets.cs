// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop.UnitTests
{
    public class CustomStructMarshallingCodeSnippets
    {
        readonly ICustomMarshallingSignatureTestProvider _provider;
        public StatelessSnippets Stateless { get; }
        public StatefulSnippets Stateful { get; }
        public CustomStructMarshallingCodeSnippets(ICustomMarshallingSignatureTestProvider provider)
        {
            _provider = provider;
            Stateless = new StatelessSnippets(provider);
            Stateful = new StatefulSnippets(provider);
        }

        private static readonly string UsingSystemRuntimeInteropServicesMarshalling = "using System.Runtime.InteropServices.Marshalling;";

        public static string NonBlittableUserDefinedType(bool defineNativeMarshalling = true) => $$"""
            {{(defineNativeMarshalling ? "[NativeMarshalling(typeof(Marshaller))]" : string.Empty)}}
            public struct S
            {
            #pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
                public bool b;
            #pragma warning restore CS0649
            }
            """;
        private static string NonStatic = """
            [CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
            public class Marshaller
            {
                public struct Native { }

                public static Native ConvertToUnmanaged(S s) => default;
            }
            """;
        public string NonStaticMarshallerEntryPoint => _provider.BasicParameterByValue("S")
            + NonBlittableUserDefinedType()
            + NonStatic;

        private static string Struct = """
            [CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
            public struct Marshaller
            {
                public struct Native { }

                public void FromManaged(S s) {}
                public Native ToUnmanaged() => default;
            }
            """;
        public string StructMarshallerEntryPoint => _provider.BasicParameterByValue("S")
            + NonBlittableUserDefinedType()
            + Struct;


        public class StatelessSnippets
        {
            public readonly ICustomMarshallingSignatureTestProvider _provider;
            public StatelessSnippets(ICustomMarshallingSignatureTestProvider provider)
            {
                this._provider = provider;
            }

            private static string In = """
                [CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
                [CustomMarshaller(typeof(S), MarshalMode.UnmanagedToManagedOut, typeof(Marshaller))]
                public static class Marshaller
                {
                    public struct Native { }

                    public static Native ConvertToUnmanaged(S s) => default;
                }
                """;
            private static string InBuffer = """
                [CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
                public static class Marshaller
                {
                    public struct Native { }

                    public const int BufferSize = 0x100;
                    public static Native ConvertToUnmanaged(S s, System.Span<byte> buffer) => default;
                }
                """;

            public static string InPinnable = """
                [CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
                [CustomMarshaller(typeof(S), MarshalMode.UnmanagedToManagedOut, typeof(Marshaller))]
                public static unsafe class Marshaller
                {
                    public static byte* ConvertToUnmanaged(S s) => default;
                    public static ref byte GetPinnableReference(S s) => throw null;
                }
                """;
            private static string Out = """
                [CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedOut, typeof(Marshaller))]
                [CustomMarshaller(typeof(S), MarshalMode.UnmanagedToManagedIn, typeof(Marshaller))]
                public static class Marshaller
                {
                    public struct Native { }

                    public static S ConvertToManaged(Native n) => default;
                }
                """;
            private static string OutGuaranteed = """
                [CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedOut, typeof(Marshaller))]
                [CustomMarshaller(typeof(S), MarshalMode.UnmanagedToManagedIn, typeof(Marshaller))]
                public static class Marshaller
                {
                    public struct Native { }

                    public static S ConvertToManagedFinally(Native n) => default;
                }
                """;
            public static string Ref = """
                [CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedRef, typeof(Marshaller))]
                [CustomMarshaller(typeof(S), MarshalMode.UnmanagedToManagedRef, typeof(Marshaller))]
                public static class Marshaller
                {
                    public struct Native { }

                    public static Native ConvertToUnmanaged(S s) => default;
                    public static S ConvertToManaged(Native n) => default;
                }
                """;
            public static string Default = """
                [CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
                public static class Marshaller
                {
                    public struct Native { }

                    public static Native ConvertToUnmanaged(S s) => default;
                    public static S ConvertToManaged(Native n) => default;
                }
                """;
            public static string InOutBuffer = """
                [CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
                [CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedOut, typeof(Marshaller))]
                [CustomMarshaller(typeof(S), MarshalMode.UnmanagedToManagedIn, typeof(Marshaller))]
                public static class Marshaller
                {
                    public struct Native { }

                    public const int BufferSize = 0x100;
                    public static Native ConvertToUnmanaged(S s, System.Span<byte> buffer) => default;
                    public static S ConvertToManaged(Native n) => default;
                }
                """;
            public static string DefaultOptionalBuffer = """
                [CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
                public static class Marshaller
                {
                    public struct Native { }

                    public const int BufferSize = 0x100;
                    public static Native ConvertToUnmanaged(S s) => default;
                    public static Native ConvertToUnmanaged(S s, System.Span<byte> buffer) => default;
                    public static S ConvertToManaged(Native n) => default;
                }
                """;
            private static string DefaultIn = """
                [CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
                public static class Marshaller
                {
                    public struct Native { }

                    public static Native ConvertToUnmanaged(S s) => default;
                }
                """;
            private static string DefaultOut = """
                [CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
                public static class Marshaller
                {
                    public struct Native { }

                    public static S ConvertToManaged(Native n) => default;
                }
                """;
            public string ManagedToNativeOnlyOutParameter => _provider.BasicParameterWithByRefModifier("out", "S")
                + NonBlittableUserDefinedType()
                + In;

            public string NativeToManagedOnlyOutParameter => _provider.BasicParameterWithByRefModifier("out", "S")
                + NonBlittableUserDefinedType()
                + Out;

            public string NativeToManagedFinallyOnlyOutParameter => _provider.BasicParameterWithByRefModifier("out", "S")
                + NonBlittableUserDefinedType()
                + OutGuaranteed;

            public string ManagedToNativeOnlyReturnValue => _provider.BasicReturnType("S")
                + NonBlittableUserDefinedType()
                + In;

            public string NativeToManagedOnlyReturnValue => _provider.BasicReturnType("S")
                + NonBlittableUserDefinedType()
                + Out;

            public string NativeToManagedFinallyOnlyReturnValue => _provider.BasicReturnType("S")
                + NonBlittableUserDefinedType()
                + Out;

            public string NativeToManagedOnlyInParameter => _provider.BasicParameterWithByRefModifier("in", "S")
                + NonBlittableUserDefinedType()
                + Out;

            public string NativeToManagedFinallyOnlyInParameter => _provider.BasicParameterWithByRefModifier("in", "S")
                + NonBlittableUserDefinedType()
                + OutGuaranteed;

            public string ParametersAndModifiers => _provider.BasicParametersAndModifiers("S", UsingSystemRuntimeInteropServicesMarshalling)
                + NonBlittableUserDefinedType(defineNativeMarshalling: true)
                + Default;

            public string MarshalUsingParametersAndModifiers => _provider.MarshalUsingParametersAndModifiers("S", "Marshaller")
                + NonBlittableUserDefinedType(defineNativeMarshalling: false)
                + Default;

            public string ByValueInParameter => _provider.BasicParameterByValue("S")
                + NonBlittableUserDefinedType()
                + In;

            public string ByValueOutParameter => _provider.BasicParameterByValue("S")
                + NonBlittableUserDefinedType()
                + Out;

            public string StackallocByValueInParameter => _provider.BasicParameterByValue("S")
                + NonBlittableUserDefinedType()
                + InBuffer;

            public string PinByValueInParameter => _provider.BasicParameterByValue("S")
                + NonBlittableUserDefinedType()
                + InPinnable;

            public string StackallocParametersAndModifiersNoRef => _provider.BasicParametersAndModifiersNoRef("S")
                + NonBlittableUserDefinedType()
                + InOutBuffer;

            public string RefParameter => _provider.BasicParameterWithByRefModifier("ref", "S")
                + NonBlittableUserDefinedType()
                + Ref;

                public string StackallocOnlyRefParameter => _provider.BasicParameterWithByRefModifier("ref", "S")
                + NonBlittableUserDefinedType()
                + InOutBuffer;

            public string OptionalStackallocParametersAndModifiers => _provider.BasicParametersAndModifiers("S", UsingSystemRuntimeInteropServicesMarshalling)
                + NonBlittableUserDefinedType()
                + DefaultOptionalBuffer;

            public string DefaultModeByValueInParameter => _provider.BasicParameterByValue("S")
                + NonBlittableUserDefinedType()
                + DefaultIn;

            public string DefaultModeReturnValue => _provider.BasicReturnType("S")
                + NonBlittableUserDefinedType()
                + DefaultOut;
        }

        public class StatefulSnippets
        {
            private readonly ICustomMarshallingSignatureTestProvider _provider;
            public StatefulSnippets (ICustomMarshallingSignatureTestProvider provider)
            {
                _provider = provider;
            }
            private static string In = """
                [CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedIn, typeof(M))]
                [CustomMarshaller(typeof(S), MarshalMode.UnmanagedToManagedOut, typeof(M))]
                public static class Marshaller
                {
                    public struct Native { }

                    public struct M
                    {
                        public void FromManaged(S s) {}
                        public Native ToUnmanaged() => default;
                    }
                }
                """;

            public static string InStatelessPinnable = """
                [CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedIn, typeof(M))]
                [CustomMarshaller(typeof(S), MarshalMode.UnmanagedToManagedOut, typeof(M))]
                public static class Marshaller
                {
                    public unsafe struct M
                    {
                        public void FromManaged(S s) {}
                        public byte* ToUnmanaged() => default;

                        public static ref byte GetPinnableReference(S s) => throw null;
                    }
                }
                """;

            public static string InPinnable = """
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
                """;

            private static string InBuffer = """
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
                """;
            private static string Out = """
                [CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedOut, typeof(M))]
                [CustomMarshaller(typeof(S), MarshalMode.UnmanagedToManagedIn, typeof(M))]
                public static class Marshaller
                {
                    public struct Native { }

                    public struct M
                    {
                        public void FromUnmanaged(Native n) {}
                        public S ToManaged() => default;
                    }
                }
                """;
            private static string OutGuaranteed = """
                [CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedOut, typeof(M))]
                [CustomMarshaller(typeof(S), MarshalMode.UnmanagedToManagedIn, typeof(M))]
                public static class Marshaller
                {
                    public struct Native { }

                    public struct M
                    {
                        public void FromUnmanaged(Native n) {}
                        public S ToManagedFinally() => default;
                    }
                }
                """;
            public static string Ref = """
                [CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedRef, typeof(M))]
                [CustomMarshaller(typeof(S), MarshalMode.UnmanagedToManagedRef, typeof(M))]
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
                """;
            public static string Default = """
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
                """;
            public static string DefaultWithFree = """
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
                """;
            public static string DefaultWithOnInvoked = """
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
                """;
            public static string InOutBuffer = """
                [CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedIn, typeof(M))]
                [CustomMarshaller(typeof(S), MarshalMode.ManagedToUnmanagedOut, typeof(M))]
                [CustomMarshaller(typeof(S), MarshalMode.UnmanagedToManagedIn, typeof(M))]
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
                """;
            public static string DefaultOptionalBuffer = """
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
                """;
            private static string DefaultIn = """
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
                """;
            private static string DefaultOut = """
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
                """;
            public string ManagedToNativeOnlyOutParameter => _provider.BasicParameterWithByRefModifier("out", "S")
                + NonBlittableUserDefinedType()
                + In;

            public string NativeToManagedOnlyOutParameter => _provider.BasicParameterWithByRefModifier("out", "S")
                + NonBlittableUserDefinedType()
                + Out;

            public string NativeToManagedFinallyOnlyOutParameter => _provider.BasicParameterWithByRefModifier("out", "S")
                + NonBlittableUserDefinedType()
                + OutGuaranteed;

            public string ManagedToNativeOnlyReturnValue => _provider.BasicReturnType("S")
                + NonBlittableUserDefinedType()
                + In;

            public string NativeToManagedOnlyReturnValue => _provider.BasicReturnType("S")
                + NonBlittableUserDefinedType()
                + Out;

            public string NativeToManagedFinallyOnlyReturnValue => _provider.BasicReturnType("S")
                + NonBlittableUserDefinedType()
                + Out;

            public string NativeToManagedOnlyInParameter => _provider.BasicParameterWithByRefModifier("in", "S")
                + NonBlittableUserDefinedType()
                + Out;

            public string NativeToManagedFinallyOnlyInParameter => _provider.BasicParameterWithByRefModifier("in", "S")
                + NonBlittableUserDefinedType()
                + OutGuaranteed;

            public string ParametersAndModifiers => _provider.BasicParametersAndModifiers("S", UsingSystemRuntimeInteropServicesMarshalling)
                + NonBlittableUserDefinedType(defineNativeMarshalling: true)
                + Default;

            public string ParametersAndModifiersWithFree => _provider.BasicParametersAndModifiers("S", UsingSystemRuntimeInteropServicesMarshalling)
                + NonBlittableUserDefinedType(defineNativeMarshalling: true)
                + DefaultWithFree;

            public string ParametersAndModifiersWithOnInvoked => _provider.BasicParametersAndModifiers("S", UsingSystemRuntimeInteropServicesMarshalling)
                + NonBlittableUserDefinedType(defineNativeMarshalling: true)
                + DefaultWithOnInvoked;

            public string MarshalUsingParametersAndModifiers => _provider.MarshalUsingParametersAndModifiers("S", "Marshaller")
                + NonBlittableUserDefinedType(defineNativeMarshalling: false)
                + Default;

            public string ByValueInParameter => _provider.BasicParameterByValue("S")
                + NonBlittableUserDefinedType()
                + In;

            public string ByValueOutParameter => _provider.BasicParameterByValue("S")
                + NonBlittableUserDefinedType()
                + Out;

            public string StackallocByValueInParameter => _provider.BasicParameterByValue("S")
                + NonBlittableUserDefinedType()
                + InBuffer;

            public string PinByValueInParameter => _provider.BasicParameterByValue("S")
                + NonBlittableUserDefinedType()
                + InStatelessPinnable;

            public string MarshallerPinByValueInParameter => _provider.BasicParameterByValue("S")
                + NonBlittableUserDefinedType()
                + InPinnable;

            public string StackallocParametersAndModifiersNoRef => _provider.BasicParametersAndModifiersNoRef("S")
                + NonBlittableUserDefinedType()
                + InOutBuffer;

            public string RefParameter => _provider.BasicParameterWithByRefModifier("ref", "S")
                + NonBlittableUserDefinedType()
                + Ref;

            public string StackallocOnlyRefParameter => _provider.BasicParameterWithByRefModifier("ref", "S")
                + NonBlittableUserDefinedType()
                + InOutBuffer;

            public string OptionalStackallocParametersAndModifiers => _provider.BasicParametersAndModifiers("S", UsingSystemRuntimeInteropServicesMarshalling)
                + NonBlittableUserDefinedType()
                + DefaultOptionalBuffer;

            public string DefaultModeByValueInParameter => _provider.BasicParameterByValue("S")
                + NonBlittableUserDefinedType()
                + DefaultIn;

            public string DefaultModeReturnValue => _provider.BasicReturnType("S")
                + NonBlittableUserDefinedType()
                + DefaultOut;
        }
    }
}
