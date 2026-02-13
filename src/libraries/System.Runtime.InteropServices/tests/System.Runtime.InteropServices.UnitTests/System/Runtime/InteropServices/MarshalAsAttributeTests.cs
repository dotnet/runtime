// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class MarshalAsAttributeTests
    {
        [Theory]
        [InlineData((UnmanagedType)(-1))]
        [InlineData(UnmanagedType.HString)]
        [InlineData((UnmanagedType)int.MaxValue)]
        public void Ctor_UnmanagedType(UnmanagedType unmanagedType)
        {
            var attribute = new MarshalAsAttribute(unmanagedType);
            Assert.Equal(unmanagedType, attribute.Value);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(47)]
        [InlineData(short.MaxValue)]
        public void Ctor_ShortUnmanagedType(short umanagedType)
        {
            var attribute = new MarshalAsAttribute(umanagedType);
            Assert.Equal((UnmanagedType)umanagedType, attribute.Value);
        }

        [Fact]
        public void SafeArrayParameter_ZeroLengthUserDefinedSubType_DoesNotThrow()
        {
            // Build a PE with a FieldMarshal blob that encodes
            // NATIVE_TYPE_SAFEARRAY + VT_DISPATCH + compressed-string-length-0.
            // tlbimp produces this format when there is no user-defined sub type name,
            // and it previously caused TypeLoadException because the native code returned
            // a non-null pointer past the blob boundary for the zero-length string.
            byte[] peImage = CreatePEWithSafeArrayFieldMarshal();

            AssemblyLoadContext alc = new(nameof(SafeArrayParameter_ZeroLengthUserDefinedSubType_DoesNotThrow), isCollectible: true);
            try
            {
                Assembly asm = alc.LoadFromStream(new MemoryStream(peImage));
                Type iface = asm.GetType("TestInterface");
                MethodInfo method = iface.GetMethod("TestMethod");
                ParameterInfo param = method.GetParameters()[0];

                // Accessing custom attributes must not throw TypeLoadException.
                _ = param.GetCustomAttributes(false);

                MarshalAsAttribute attr = (MarshalAsAttribute)Attribute.GetCustomAttribute(param, typeof(MarshalAsAttribute));
                Assert.NotNull(attr);
                Assert.Equal(UnmanagedType.SafeArray, attr.Value);
                Assert.Null(attr.SafeArrayUserDefinedSubType);
            }
            finally
            {
                alc.Unload();
            }
        }

        /// <summary>
        /// Builds a minimal PE containing an interface with a method whose parameter
        /// has a FieldMarshal blob matching what tlbimp generates for SafeArray
        /// without a user-defined sub type:
        ///   byte 0x1D (NATIVE_TYPE_SAFEARRAY), 0x09 (VT_DISPATCH), 0x00 (string len 0)
        /// The trailing zero-length string distinguishes this from blobs the C# compiler
        /// produces (which omit the length byte entirely).
        /// </summary>
        private static byte[] CreatePEWithSafeArrayFieldMarshal()
        {
            PersistedAssemblyBuilder ab = new(new AssemblyName("SafeArrayTestAsm"), typeof(object).Assembly);
            ModuleBuilder moduleDef = ab.DefineDynamicModule("SafeArrayTestAsm.dll");
            TypeBuilder typeDef = moduleDef.DefineType("TestInterface",
                TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.Public);

            MethodBuilder methodDef = typeDef.DefineMethod("TestMethod",
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual |
                MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                typeof(void), new[] { typeof(object[]) });

            methodDef.DefineParameter(1, ParameterAttributes.HasFieldMarshal, "args");
            typeDef.CreateType();

            MetadataBuilder mdBuilder = ab.GenerateMetadata(out BlobBuilder ilStream, out _);

            // Inject the problematic FieldMarshal descriptor directly:
            //   0x1D  NATIVE_TYPE_SAFEARRAY
            //   0x09  VT_DISPATCH (compressed uint)
            //   0x00  compressed string length = 0 (empty user-defined type name)
            BlobBuilder marshalDescriptor = new();
            marshalDescriptor.WriteBytes(new byte[] { 0x1D, 0x09, 0x00 });
            mdBuilder.AddMarshallingDescriptor(
                MetadataTokens.ParameterHandle(1),
                mdBuilder.GetOrAddBlob(marshalDescriptor));

            ManagedPEBuilder peAssembler = new(
                PEHeaderBuilder.CreateLibraryHeader(),
                new MetadataRootBuilder(mdBuilder),
                ilStream,
                flags: CorFlags.ILOnly);

            BlobBuilder peOutput = new();
            peAssembler.Serialize(peOutput);
            return peOutput.ToArray();
        }
    }
}
