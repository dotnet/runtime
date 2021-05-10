// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Types in this file are used for generated p/invokes (docs/design/features/source-generator-pinvokes.md).
// See the DllImportGenerator experiment in https://github.com/dotnet/runtimelab.
//
namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    internal class GeneratedMarshallingAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Struct)]
    internal class BlittableTypeAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    internal class NativeMarshallingAttribute : Attribute
    {
        public NativeMarshallingAttribute(Type nativeType)
        {
            NativeType = nativeType;
        }

        public Type NativeType { get; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Field)]
    internal class MarshalUsingAttribute : Attribute
    {
        public MarshalUsingAttribute(Type nativeType)
        {
            NativeType = nativeType;
        }

        public Type NativeType { get; }
    }
}
