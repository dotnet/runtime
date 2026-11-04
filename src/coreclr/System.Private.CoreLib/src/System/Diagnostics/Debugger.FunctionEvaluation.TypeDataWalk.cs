// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Diagnostics;

public static partial class Debugger
{
    internal sealed unsafe partial class FunctionEvaluation
    {
        // DebuggerIPCE_TypeArgData uses fixed-width, little-endian debugger cookies, not native pointers.
        // corelib.h verifies this layout against the native record using the reader's size and offsets.
        private const int TypeArgumentDataSize = 48;
        private const int TypeArgumentElementTypeOffset = 0;
        private const int TypeArgumentTokenOffset = 8;
        private const int TypeArgumentAssemblyOffset = 16;
        private const int TypeArgumentArrayRankOffset = 32;
        private const int TypeArgumentChildCountOffset = 40;

        // Preserve the unused slots in the flattened native union, including padding on 32-bit targets.
        [StructLayout(LayoutKind.Sequential)]
        internal struct NativeTypeArgument
        {
            internal uint ElementType;
            internal uint ElementTypePadding;
            internal uint MetadataToken;
            internal uint MetadataTokenPadding;
            internal ulong Assembly;
            internal ulong TypeHandle;
            internal uint ArrayRank;
            internal uint ArrayRankPadding;
            internal uint ChildCount;
            internal uint ChildCountPadding;
        }

        [DebuggerHidden]
        internal static RuntimeType[] ReadTypeArguments(ReadOnlySpan<byte> typeData, int count)
        {
            TypeDataWalk walk = new(typeData);
            RuntimeType[] types = walk.ReadTypeHandles(count);
            if (!walk.IsFinished)
            {
                throw new ArgumentException(SR.Argument_InvalidGenericArg);
            }

            return types;
        }

        private static ReadOnlySpan<byte> GetTypeData(byte* data, uint nodeCount) =>
            new(data, checked((int)nodeCount * TypeArgumentDataSize));

        private static RuntimeModule GetModule(nint nativeAssembly)
        {
            if (nativeAssembly == 0)
            {
                throw new ArgumentException(SR.Argument_InvalidGenericArg);
            }

            return (RuntimeModule)RuntimeAssembly.GetExposedObject(nativeAssembly).ManifestModule;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The debugger supplies tokens from the target's actual loaded metadata.")]
        private static RuntimeType ReadInstantiation(RuntimeModule module, int token, RuntimeType[] arguments)
        {
            RuntimeType type = (RuntimeType)module.ResolveType(token);
            if (arguments.Length != 0)
            {
                if (!type.IsGenericTypeDefinition || type.GetGenericArguments().Length != arguments.Length)
                {
                    throw new ArgumentException(SR.Argument_InvalidGenericArg);
                }

                type = type.TypeHandle.Instantiate(arguments);
            }

            return type;
        }

        [StackTraceHidden]
        private ref struct TypeDataWalk
        {
            private ReadOnlySpan<byte> _remaining;

            internal TypeDataWalk(ReadOnlySpan<byte> data)
            {
                if (data.Length % TypeArgumentDataSize != 0)
                {
                    throw new ArgumentException(SR.Argument_InvalidGenericArg);
                }

                _remaining = data;
            }

            internal readonly bool IsFinished => _remaining.IsEmpty;

            internal RuntimeType[] ReadTypeHandles(int count)
            {
                if ((uint)count > (uint)(_remaining.Length / TypeArgumentDataSize))
                {
                    throw new ArgumentException(SR.Argument_InvalidGenericArg);
                }

                RuntimeType[] types = count == 0 ? [] : new RuntimeType[count];
                for (int i = 0; i < types.Length; i++)
                {
                    types[i] = ReadTypeHandle();
                }

                return types;
            }

            private RuntimeType ReadTypeHandle()
            {
                if (_remaining.IsEmpty)
                {
                    throw new ArgumentException(SR.Argument_InvalidGenericArg);
                }

                ReadOnlySpan<byte> node = _remaining[..TypeArgumentDataSize];
                _remaining = _remaining[TypeArgumentDataSize..];
                uint elementValue = BinaryPrimitives.ReadUInt32LittleEndian(
                    node[TypeArgumentElementTypeOffset..]);
                if (elementValue > byte.MaxValue)
                {
                    throw new ArgumentException(SR.Argument_InvalidGenericArg);
                }

                CorElementType elementType = (CorElementType)elementValue;
                uint childCount = BinaryPrimitives.ReadUInt32LittleEndian(node[TypeArgumentChildCountOffset..]);
                if (childCount > (uint)(_remaining.Length / TypeArgumentDataSize))
                {
                    throw new ArgumentException(SR.Argument_InvalidGenericArg);
                }

                switch (elementType)
                {
                    case CorElementType.ELEMENT_TYPE_ARRAY:
                    case CorElementType.ELEMENT_TYPE_SZARRAY:
                    case CorElementType.ELEMENT_TYPE_PTR:
                    case CorElementType.ELEMENT_TYPE_BYREF:
                        if (childCount != 1)
                        {
                            throw new ArgumentException(SR.Argument_InvalidGenericArg);
                        }

                        RuntimeType element = ReadTypeHandle();
                        return elementType switch
                        {
                            CorElementType.ELEMENT_TYPE_ARRAY => (RuntimeType)element.MakeArrayType(
                                checked((int)BinaryPrimitives.ReadUInt32LittleEndian(node[TypeArgumentArrayRankOffset..]))),
                            CorElementType.ELEMENT_TYPE_SZARRAY => (RuntimeType)element.MakeArrayType(),
                            CorElementType.ELEMENT_TYPE_PTR => (RuntimeType)element.MakePointerType(),
                            _ => (RuntimeType)element.MakeByRefType(),
                        };

                    case CorElementType.ELEMENT_TYPE_CLASS:
                    case CorElementType.ELEMENT_TYPE_VALUETYPE:
                        nint assembly = (nint)checked((nuint)BinaryPrimitives.ReadUInt64LittleEndian(
                            node[TypeArgumentAssemblyOffset..]));
                        RuntimeModule module = GetModule(assembly);
                        int token = BinaryPrimitives.ReadInt32LittleEndian(node[TypeArgumentTokenOffset..]);
                        return ReadInstantiation(module, token, ReadTypeHandles((int)childCount));

                    case CorElementType.ELEMENT_TYPE_FNPTR:
                        if (childCount == 0)
                        {
                            throw new ArgumentException(SR.Argument_InvalidGenericArg);
                        }

                        RuntimeType[] signature = ReadTypeHandles((int)childCount);
                        return signature[0].TypeHandle.MakeFunctionPointer(signature.AsSpan(1).ToArray(), isUnmanaged: false);

                    default:
                        if (childCount != 0)
                        {
                            throw new ArgumentException(SR.Argument_InvalidGenericArg);
                        }

                        return (RuntimeType)(elementType switch
                        {
                            CorElementType.ELEMENT_TYPE_VOID => typeof(void),
                            CorElementType.ELEMENT_TYPE_BOOLEAN => typeof(bool),
                            CorElementType.ELEMENT_TYPE_CHAR => typeof(char),
                            CorElementType.ELEMENT_TYPE_I1 => typeof(sbyte),
                            CorElementType.ELEMENT_TYPE_U1 => typeof(byte),
                            CorElementType.ELEMENT_TYPE_I2 => typeof(short),
                            CorElementType.ELEMENT_TYPE_U2 => typeof(ushort),
                            CorElementType.ELEMENT_TYPE_I4 => typeof(int),
                            CorElementType.ELEMENT_TYPE_U4 => typeof(uint),
                            CorElementType.ELEMENT_TYPE_I8 => typeof(long),
                            CorElementType.ELEMENT_TYPE_U8 => typeof(ulong),
                            CorElementType.ELEMENT_TYPE_R4 => typeof(float),
                            CorElementType.ELEMENT_TYPE_R8 => typeof(double),
                            CorElementType.ELEMENT_TYPE_STRING => typeof(string),
                            CorElementType.ELEMENT_TYPE_TYPEDBYREF => typeof(TypedReference),
                            CorElementType.ELEMENT_TYPE_I => typeof(nint),
                            CorElementType.ELEMENT_TYPE_U => typeof(nuint),
                            CorElementType.ELEMENT_TYPE_OBJECT => typeof(object),
                            _ => throw new ArgumentException(SR.Argument_InvalidGenericArg),
                        });
                }
            }
        }
    }
}
