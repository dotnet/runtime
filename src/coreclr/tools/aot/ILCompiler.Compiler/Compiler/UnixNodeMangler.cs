// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Globalization;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler
{
    public sealed class UnixNodeMangler : NodeMangler
    {
        // Mangled name of boxed version of a type
        public sealed override Utf8String MangledBoxedTypeName(TypeDesc type)
        {
            Debug.Assert(type.IsValueType);
            return Utf8String.Concat("Boxed_"u8, NameMangler.GetMangledTypeName(type).AsSpan());
        }

        public sealed override Utf8String MethodTable(TypeDesc type)
        {
            Utf8String mangledJustTypeName = type.IsValueType
                ? MangledBoxedTypeName(type)
                : NameMangler.GetMangledTypeName(type);

            Span<byte> typeNameLengthStr = stackalloc byte[16];
            mangledJustTypeName.Length.TryFormat(typeNameLengthStr, out int written);

            return Utf8String.Concat("_ZTV"u8, typeNameLengthStr.Slice(0, written), mangledJustTypeName.AsSpan());
        }

        public sealed override Utf8String GCStatics(TypeDesc type)
        {
            return Utf8String.Concat("__GCSTATICS"u8, NameMangler.GetMangledTypeName(type).AsSpan());
        }

        public sealed override Utf8String NonGCStatics(TypeDesc type)
        {
            return Utf8String.Concat("__NONGCSTATICS"u8, NameMangler.GetMangledTypeName(type).AsSpan());
        }

        public sealed override Utf8String ThreadStatics(TypeDesc type)
        {
            return Utf8String.Concat(NameMangler.CompilationUnitPrefix.AsSpan(), "__THREADSTATICS"u8, NameMangler.GetMangledTypeName(type).AsSpan());
        }

        public sealed override Utf8String ThreadStaticsIndex(TypeDesc type)
        {
            return Utf8String.Concat("__TypeThreadStaticIndex"u8, NameMangler.GetMangledTypeName(type).AsSpan());
        }

        public sealed override Utf8String TypeGenericDictionary(TypeDesc type)
        {
            return Utf8String.Concat(GenericDictionaryNamePrefix, NameMangler.GetMangledTypeName(type));
        }

        public sealed override Utf8String MethodGenericDictionary(MethodDesc method)
        {
            return Utf8String.Concat(GenericDictionaryNamePrefix, NameMangler.GetMangledMethodName(method));
        }

        public sealed override Utf8String ExternMethod(Utf8String unmangledName, MethodDesc method)
        {
            return unmangledName;
        }

        public sealed override Utf8String ExternVariable(Utf8String unmangledName)
        {
            return unmangledName;
        }
    }
}
