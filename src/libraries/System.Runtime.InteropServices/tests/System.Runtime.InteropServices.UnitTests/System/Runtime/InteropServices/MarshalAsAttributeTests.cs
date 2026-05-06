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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void SafeArrayParameter_ZeroLengthUserDefinedSubType_DoesNotThrow()
        {
            byte[] peBytes = BuildPEWithSafeArrayMarshalBlob();

            AssemblyLoadContext alc = new(nameof(SafeArrayParameter_ZeroLengthUserDefinedSubType_DoesNotThrow), isCollectible: true);
            try
            {
                using MemoryStream peStream = new(peBytes);
                Assembly asm = alc.LoadFromStream(peStream);
                Type iface = asm.GetType("TestInterface")!;
                MethodInfo method = iface.GetMethod("TestMethod")!;
                ParameterInfo param = method.GetParameters()[0];

                // Must not throw TypeLoadException.
                _ = param.GetCustomAttributes(false);

                MarshalAsAttribute? attr = (MarshalAsAttribute?)Attribute.GetCustomAttribute(param, typeof(MarshalAsAttribute));
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
        /// Creates a PE whose parameter has a FieldMarshal blob that
        /// reproduces the native bug: NATIVE_TYPE_SAFEARRAY (0x1D),
        /// VT_DISPATCH (0x09), zero-length string (0x00), followed by
        /// poison byte 'X' (0x58) and null terminator (0x00).
        /// Without the native fix the FCALL returns a dangling pointer
        /// into the poison byte region causing TypeLoadException.
        /// </summary>
        private static byte[] BuildPEWithSafeArrayMarshalBlob()
        {
            PersistedAssemblyBuilder ab = new(
                new AssemblyName("SafeArrayTestAsm"), typeof(object).Assembly);
            ModuleBuilder mod = ab.DefineDynamicModule("SafeArrayTestAsm.dll");
            TypeBuilder td = mod.DefineType("TestInterface",
                TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.Public);

            MethodBuilder md = td.DefineMethod("TestMethod",
                MethodAttributes.Public | MethodAttributes.Abstract |
                MethodAttributes.Virtual | MethodAttributes.NewSlot |
                MethodAttributes.HideBySig,
                typeof(void), new[] { typeof(object[]) });

            md.DefineParameter(1, ParameterAttributes.HasFieldMarshal, "args");
            td.CreateType();

            MetadataBuilder mdb = ab.GenerateMetadata(out BlobBuilder ilBlob, out _);

            // This is the only parameter defined on the only method, so it
            // occupies row 1 in the Param table. PersistedAssemblyBuilder
            // emits parameters in definition order deterministically.
            const int paramRowNumber = 1;

            // Blob bytes:
            //   0x1D  NATIVE_TYPE_SAFEARRAY
            //   0x09  VT_DISPATCH
            //   0x00  compressed string length 0
            //   0x58  'X' poison (not consumed by parser)
            //   0x00  null terminator
            BlobBuilder marshalBlob = new();
            marshalBlob.WriteByte(0x1D);
            marshalBlob.WriteByte(0x09);
            marshalBlob.WriteByte(0x00);
            marshalBlob.WriteByte(0x58);
            marshalBlob.WriteByte(0x00);
            mdb.AddMarshallingDescriptor(
                MetadataTokens.ParameterHandle(paramRowNumber),
                mdb.GetOrAddBlob(marshalBlob));

            ManagedPEBuilder pe = new(
                PEHeaderBuilder.CreateLibraryHeader(),
                new MetadataRootBuilder(mdb),
                ilBlob,
                flags: CorFlags.ILOnly);

            BlobBuilder output = new();
            pe.Serialize(output);
            return output.ToArray();
        }
    }
}
