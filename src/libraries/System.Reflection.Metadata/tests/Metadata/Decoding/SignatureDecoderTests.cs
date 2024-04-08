// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata.Tests;
using System.Reflection.PortableExecutable;
using Xunit;

namespace System.Reflection.Metadata.Decoding.Tests
{
    public partial class SignatureDecoderTests
    {
        [Fact]
        public unsafe void VerifyMultipleOptionalModifiers()
        {
            // Type 1: int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) modopt([mscorlib]System.Runtime.CompilerServices.CallConvCdecl)
            // Type 2: char*
            // Type 3: uint32
            // Type 4: char modopt([mscorlib]System.Runtime.CompilerServices.IsConst)*
            var testSignature = new byte[] { 0x20, 0x45, 0x20, 0x69, 0x08, 0x0F, 0x03, 0x09, 0x0F, 0x20, 0x55, 0x03 };
            var types = new string[]
            {
                "int32 modopt(100001A) modopt(1000011)",
                "char*",
                "uint32",
                "char modopt(1000015)*"
            };

            fixed (byte* testSignaturePtr = &testSignature[0])
            {
                var signatureBlob = new BlobReader(testSignaturePtr, testSignature.Length);
                var provider = new OpaqueTokenTypeProvider();
                var decoder = new SignatureDecoder<string, DisassemblingGenericContext>(provider, metadataReader: null, genericContext: null);

                foreach (string typeString in types)
                {
                    // Verify that each type is decoded as expected
                    Assert.Equal(typeString, decoder.DecodeType(ref signatureBlob));
                }
                // And that nothing is left over to decode
                Assert.True(signatureBlob.RemainingBytes == 0);
                Assert.Throws<BadImageFormatException>(() => decoder.DecodeType(ref signatureBlob));
            }
        }

        [Theory]
        [InlineData(new string[] { "int32", "string" }, new byte[] { 0x0A /*GENERICINST*/, 2  /*count*/, 0x8 /*I4*/, 0xE /*STRING*/ })]
        public unsafe void DecodeValidMethodSpecificationSignature(string[] expectedTypes, byte[] testSignature)
        {
            fixed (byte* testSignaturePtr = &testSignature[0])
            {
                var signatureBlob = new BlobReader(testSignaturePtr, testSignature.Length);
                var provider = new OpaqueTokenTypeProvider();
                var decoder = new SignatureDecoder<string, DisassemblingGenericContext>(provider, metadataReader: null, genericContext: null);

                IEnumerable<string> actualTypes = decoder.DecodeMethodSpecificationSignature(ref signatureBlob);
                Assert.Equal(expectedTypes, actualTypes);
                Assert.True(signatureBlob.RemainingBytes == 0);
                Assert.Throws<BadImageFormatException>(() => decoder.DecodeType(ref signatureBlob));
            }
        }

        [Theory]
        [InlineData(new byte[] { 0 })]  // bad header
        [InlineData(new byte[] { 0x0A /*GENERICINST*/, 0 /*count*/ })] // no type parameters
        public unsafe void DecodeInvalidMethodSpecificationSignature(byte[] testSignature)
        {
            fixed (byte* testSignaturePtr = &testSignature[0])
            {
                var signatureBlob = new BlobReader(testSignaturePtr, testSignature.Length);
                var provider = new OpaqueTokenTypeProvider();
                var decoder = new SignatureDecoder<string, DisassemblingGenericContext>(provider, metadataReader: null, genericContext: null);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        public void DecodeVarArgsDefAndRef()
        {
            using (FileStream stream = File.OpenRead(AssemblyPathHelper.GetAssemblyLocation(typeof(VarArgsToDecode).GetTypeInfo().Assembly)))
            using (var peReader = new PEReader(stream))
            {
                MetadataReader metadataReader = peReader.GetMetadataReader();
                TypeDefinitionHandle typeDefHandle = TestMetadataResolver.FindTestType(metadataReader, typeof(VarArgsToDecode));
                TypeDefinition typeDef = metadataReader.GetTypeDefinition(typeDefHandle);
                MethodDefinition methodDef = metadataReader.GetMethodDefinition(typeDef.GetMethods().First());

                Assert.Equal("VarArgsCallee", metadataReader.GetString(methodDef.Name));
                var provider = new OpaqueTokenTypeProvider();

                MethodSignature<string> defSignature = methodDef.DecodeSignature(provider, null);
                Assert.Equal(SignatureCallingConvention.VarArgs, defSignature.Header.CallingConvention);
                Assert.Equal(1, defSignature.RequiredParameterCount);
                Assert.Equal(new[] { "int32" }, defSignature.ParameterTypes);

                int refCount = 0;
                foreach (MemberReferenceHandle memberRefHandle in metadataReader.MemberReferences)
                {
                    MemberReference memberRef = metadataReader.GetMemberReference(memberRefHandle);

                    if (metadataReader.StringComparer.Equals(memberRef.Name, "VarArgsCallee"))
                    {
                        Assert.Equal(MemberReferenceKind.Method, memberRef.GetKind());
                        MethodSignature<string> refSignature = memberRef.DecodeMethodSignature(provider, null);
                        Assert.Equal(SignatureCallingConvention.VarArgs, refSignature.Header.CallingConvention);
                        Assert.Equal(1, refSignature.RequiredParameterCount);
                        Assert.Equal(new[] { "int32", "bool", "string", "float64" }, refSignature.ParameterTypes);
                        refCount++;
                    }
                }

                Assert.Equal(1, refCount);
            }
        }

        private static class VarArgsToDecode
        {
            public static void VarArgsCallee(int i, __arglist)
            {
            }

            public static void VarArgsCaller()
            {
                VarArgsCallee(1, __arglist(true, "hello", 0.42));
            }
        }

        // Test as much as we can with simple C# examples inline below.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        public void SimpleSignatureProviderCoverage()
        {
            using (FileStream stream = File.OpenRead(AssemblyPathHelper.GetAssemblyLocation(typeof(SignaturesToDecode<>).GetTypeInfo().Assembly)))
            using (var peReader = new PEReader(stream))
            {
                MetadataReader reader = peReader.GetMetadataReader();
                var provider = new DisassemblingTypeProvider();
                TypeDefinitionHandle typeHandle = TestMetadataResolver.FindTestType(reader, typeof(SignaturesToDecode<>));
                Assert.Equal("System.Reflection.Metadata.Decoding.Tests.SignatureDecoderTests/SignaturesToDecode`1", provider.GetTypeFromHandle(reader, genericContext: null, handle: typeHandle));

                TypeDefinition type = reader.GetTypeDefinition(typeHandle);
                Dictionary<string, string> expectedFields = GetExpectedFieldSignatures();
                ImmutableArray<string> genericTypeParameters = type.GetGenericParameters().Select(h => reader.GetString(reader.GetGenericParameter(h).Name)).ToImmutableArray();

                var genericTypeContext = new DisassemblingGenericContext(genericTypeParameters, ImmutableArray<string>.Empty);

                foreach (var fieldHandle in type.GetFields())
                {
                    FieldDefinition field = reader.GetFieldDefinition(fieldHandle);
                    string fieldName = reader.GetString(field.Name);
                    string expected;
                    Assert.True(expectedFields.TryGetValue(fieldName, out expected), "Unexpected field: " + fieldName);
                    Assert.Equal(expected, field.DecodeSignature(provider, genericTypeContext));
                }

                Dictionary<string, string> expectedMethods = GetExpectedMethodSignatures();
                foreach (var methodHandle in type.GetMethods())
                {
                    MethodDefinition method = reader.GetMethodDefinition(methodHandle);

                    ImmutableArray<string> genericMethodParameters = method.GetGenericParameters().Select(h => reader.GetString(reader.GetGenericParameter(h).Name)).ToImmutableArray();
                    var genericMethodContext = new DisassemblingGenericContext(genericTypeParameters, genericMethodParameters);

                    string methodName = reader.GetString(method.Name);
                    string expected;
                    Assert.True(expectedMethods.TryGetValue(methodName, out expected), "Unexpected method: " + methodName);
                    MethodSignature<string> signature = method.DecodeSignature(provider, genericMethodContext);
                    Assert.True(signature.Header.Kind == SignatureKind.Method);

                    if (methodName.StartsWith("Generic"))
                    {
                        Assert.Equal(1, signature.GenericParameterCount);
                    }
                    else
                    {
                        Assert.Equal(0, signature.GenericParameterCount);
                    }

                    Assert.True(signature.GenericParameterCount <= 1 && (methodName != "GenericMethodParameter" || signature.GenericParameterCount == 1));
                    Assert.Equal(expected, provider.GetFunctionPointerType(signature));
                }

                Dictionary<string, string> expectedProperties = GetExpectedPropertySignatures();
                foreach (var propertyHandle in type.GetProperties())
                {
                    PropertyDefinition property = reader.GetPropertyDefinition(propertyHandle);
                    string propertyName = reader.GetString(property.Name);
                    string expected;
                    Assert.True(expectedProperties.TryGetValue(propertyName, out expected), "Unexpected property: " + propertyName);
                    MethodSignature<string> signature = property.DecodeSignature(provider, genericTypeContext);
                    Assert.True(signature.Header.Kind == SignatureKind.Property);
                    Assert.Equal(expected, provider.GetFunctionPointerType(signature));
                }

                Dictionary<string, string> expectedEvents = GetExpectedEventSignatures();
                foreach (var eventHandle in type.GetEvents())
                {
                    EventDefinition @event = reader.GetEventDefinition(eventHandle);
                    string eventName = reader.GetString(@event.Name);
                    string expected;
                    Assert.True(expectedEvents.TryGetValue(eventName, out expected), "Unexpected event: " + eventName);

                    Assert.Equal(expected, provider.GetTypeFromHandle(reader, genericTypeContext, @event.Type));
                }

                Assert.Equal($"[{MetadataReaderTestHelpers.CollectionsAssemblyName}]System.Collections.Generic.List`1<!T>", provider.GetTypeFromHandle(reader, genericTypeContext, handle: type.BaseType));
            }
        }

        public unsafe class SignaturesToDecode<T> : List<T>
        {
            public sbyte SByte;
            public byte Byte;
            public short Int16;
            public ushort UInt16;
            public int Int32;
            public uint UInt32;
            public long Int64;
            public ulong UInt64;
            public string String;
            public object Object;
            public float Single;
            public double Double;
            public IntPtr IntPtr;
            public UIntPtr UIntPtr;
            public bool Boolean;
            public char Char;
            public volatile int ModifiedType;
            public int* Pointer;
            public int[] SZArray;
            public int[,] Array;
            public void ByReference(ref int i) { }
            public T GenericTypeParameter;
            public U GenericMethodParameter<U>() { throw null; }
            public List<int> GenericInstantiation;
            public struct Nested { }
            public Nested Property { get { throw null; } }
            public event EventHandler<EventArgs> Event { add { } remove { } }
            public delegate* managed<int, bool> ManagedFunctionPointer;
            public delegate* unmanaged[Stdcall]<int, bool> NativeFunctionPointer;
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        public void PinnedAndUnpinnedLocals()
        {
            using (FileStream stream = File.OpenRead(AssemblyPathHelper.GetAssemblyLocation(typeof(PinnedAndUnpinnedLocalsToDecode).GetTypeInfo().Assembly)))
            using (var peReader = new PEReader(stream))
            {
                MetadataReader reader = peReader.GetMetadataReader();
                var provider = new DisassemblingTypeProvider();

                TypeDefinitionHandle typeDefHandle = TestMetadataResolver.FindTestType(reader, typeof(PinnedAndUnpinnedLocalsToDecode));
                TypeDefinition typeDef = reader.GetTypeDefinition(typeDefHandle);
                MethodDefinition methodDef = reader.GetMethodDefinition(typeDef.GetMethods().First());

                Assert.Equal("DoSomething", reader.GetString(methodDef.Name));

                MethodBodyBlock body = peReader.GetMethodBody(methodDef.RelativeVirtualAddress);
                var il = body.GetILBytes();
                // ILStrip replaces method body with the 'ret' IL opcode i.e. 0x2a
                if (!(il?.Length == 1 && il[0] == 0x2a)) {
                    StandaloneSignature localSignature = reader.GetStandaloneSignature(body.LocalSignature);
                    ImmutableArray<string> localTypes = localSignature.DecodeLocalSignature(provider, genericContext: null);

                    // Compiler can generate temporaries or re-order so just check the ones we expect are there.
                    // (They could get optimized away too. If that happens in practice, change this test to use hard-coded signatures.)
                    Assert.Contains("uint8[] pinned", localTypes);
                    Assert.Contains("uint8[]", localTypes);
                }
            }
        }

        public static class PinnedAndUnpinnedLocalsToDecode
        {
            public static unsafe int DoSomething()
            {
                byte[] bytes = new byte[] { 1, 2, 3 };
                fixed (byte* bytePtr = bytes)
                {
                    return *bytePtr;
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        public void WrongSignatureType()
        {
            using (FileStream stream = File.OpenRead(AssemblyPathHelper.GetAssemblyLocation(typeof(VarArgsToDecode).GetTypeInfo().Assembly)))
            using (var peReader = new PEReader(stream))
            {
                MetadataReader reader = peReader.GetMetadataReader();
                var provider = new DisassemblingTypeProvider();
                var decoder = new SignatureDecoder<string, DisassemblingGenericContext>(provider, reader, genericContext: null);

                BlobReader fieldSignature = reader.GetBlobReader(reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1)).Signature);
                BlobReader methodSignature = reader.GetBlobReader(reader.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(1)).Signature);
                BlobReader propertySignature = reader.GetBlobReader(reader.GetPropertyDefinition(MetadataTokens.PropertyDefinitionHandle(1)).Signature);

                Assert.Throws<BadImageFormatException>(() => decoder.DecodeMethodSignature(ref fieldSignature));
                Assert.Throws<BadImageFormatException>(() => decoder.DecodeFieldSignature(ref methodSignature));
                Assert.Throws<BadImageFormatException>(() => decoder.DecodeLocalSignature(ref propertySignature));
            }
        }

        private static Dictionary<string, string> GetExpectedFieldSignatures()
        {
            // Field name -> signature
            return new Dictionary<string, string>()
            {
                { "SByte", "int8" },
                { "Byte", "uint8" },
                { "Int16", "int16" },
                { "UInt16", "uint16" },
                { "Int32", "int32" },
                { "UInt32", "uint32" },
                { "Int64", "int64" },
                { "UInt64", "uint64" },
                { "String", "string" },
                { "Object", "object" },
                { "Single", "float32" },
                { "Double", "float64" },
                { "IntPtr", "native int" },
                { "UIntPtr", "native uint" },
                { "Boolean", "bool" },
                { "Char", "char" },
                { "ModifiedType", $"int32 modreq([{MetadataReaderTestHelpers.RuntimeAssemblyName}]System.Runtime.CompilerServices.IsVolatile)" },
                { "Pointer", "int32*"  },
                { "SZArray", "int32[]" },
                { "Array", "int32[0...,0...]" },
                { "GenericTypeParameter", "!T" },
                { "GenericInstantiation", $"[{MetadataReaderTestHelpers.CollectionsAssemblyName}]System.Collections.Generic.List`1<int32>" },
                { "ManagedFunctionPointer", "method bool *(int32)" },
                { "NativeFunctionPointer", "method bool *(int32)" },
            };
        }

        private static Dictionary<string, string> GetExpectedMethodSignatures()
        {
            // method name -> signature
            return new Dictionary<string, string>()
            {
                { "ByReference", "method void *(int32&)" },
                { "GenericMethodParameter", "method !!U *()" },
                { ".ctor", "method void *()" },
                { "get_Property", "method System.Reflection.Metadata.Decoding.Tests.SignatureDecoderTests/SignaturesToDecode`1/Nested<!T> *()"  },
                { "add_Event",  $"method void *([{MetadataReaderTestHelpers.RuntimeAssemblyName}]System.EventHandler`1<[{MetadataReaderTestHelpers.RuntimeAssemblyName}]System.EventArgs>)" },
                { "remove_Event", $"method void *([{MetadataReaderTestHelpers.RuntimeAssemblyName}]System.EventHandler`1<[{MetadataReaderTestHelpers.RuntimeAssemblyName}]System.EventArgs>)" },
            };
        }

        private static Dictionary<string, string> GetExpectedPropertySignatures()
        {
            // field name -> signature
            return new Dictionary<string, string>()
            {
                { "Property", "method System.Reflection.Metadata.Decoding.Tests.SignatureDecoderTests/SignaturesToDecode`1/Nested<!T> *()" },
            };
        }

        private static Dictionary<string, string> GetExpectedEventSignatures()
        {
            // event name -> signature
            return new Dictionary<string, string>()
            {
                { "Event", $"[{MetadataReaderTestHelpers.RuntimeAssemblyName}]System.EventHandler`1<[{MetadataReaderTestHelpers.RuntimeAssemblyName}]System.EventArgs>" },
            };
        }

        [Theory]
        [InlineData(new byte[] { 0x12 /*CLASS*/, 0x06 /*encoded type spec*/ })] // not def or ref
        [InlineData(new byte[] { 0x11 /*VALUETYPE*/, 0x06 /*encoded type spec*/})] // not def or ref
        [InlineData(new byte[] { 0x60 })] // Bad type code
        public unsafe void BadTypeSignature(byte[] signature)
        {
            fixed (byte* bytes = signature)
            {
                BlobReader reader = new BlobReader(bytes, signature.Length);
                Assert.Throws<BadImageFormatException>(() => new SignatureDecoder<string, DisassemblingGenericContext>(new OpaqueTokenTypeProvider(), metadataReader: null, genericContext: null).DecodeType(ref reader));
            }
        }

        [Theory]
        [InlineData("method void *()", new byte[] { 0x1B /*FNPTR*/, 0 /*default calling convention*/, 0 /*parameters count*/, 0x1 /* return type (VOID)*/ })]
        [InlineData("int32[...]", new byte[] { 0x14 /*ARRAY*/, 0x8 /*I4*/, 1 /*rank*/, 0 /*sizes*/, 0 /*lower bounds*/ })]
        [InlineData("int32[...,...,...]", new byte[] { 0x14 /*ARRAY*/, 0x8 /*I4*/, 3 /*rank*/, 0 /*sizes*/, 0/*lower bounds*/  })]
        [InlineData("int32[-1...1]", new byte[] { 0x14 /*ARRAY*/, 0x8 /*I4*/, 1 /*rank*/, 1 /*sizes*/, 3 /*size*/, 1 /*lower bounds*/, 0x7F /*lower bound (compressed -1)*/ })]
        [InlineData("int32[1...]", new byte[] { 0x14 /*ARRAY*/, 0x8 /*I4*/, 1 /*rank*/, 0 /*sizes*/, 1 /*lower bounds*/, 2 /*lower bound (compressed +1)*/ })]
        public unsafe void ExoticTypeSignature(string expected, byte[] signature)
        {
            fixed (byte* bytes = signature)
            {
                BlobReader reader = new BlobReader(bytes, signature.Length);
                Assert.Equal(expected, new SignatureDecoder<string, DisassemblingGenericContext>(new OpaqueTokenTypeProvider(), metadataReader: null, genericContext: null).DecodeType(ref reader));
            }
        }

        [Fact]
        public void ProviderCannotBeNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("provider", () => new SignatureDecoder<int, object>(provider: null, metadataReader: null, genericContext: null));
        }
    }
}
