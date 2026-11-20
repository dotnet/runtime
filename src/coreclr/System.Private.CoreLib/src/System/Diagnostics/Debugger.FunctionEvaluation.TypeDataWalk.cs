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
        // corelib.h verifies this layout against the native record.
        [ExtendedLayout(ExtendedLayoutKind.CStruct)]
        internal struct NativeTypeArgument
        {
            internal NativeExpandedTypeData Data;
            internal uint ChildCount;
        }

        [ExtendedLayout(ExtendedLayoutKind.CStruct)]
        internal struct NativeExpandedTypeData
        {
            internal uint ElementType;
            internal NativeTypeDataUnion TypeData;
        }

        [ExtendedLayout(ExtendedLayoutKind.CUnion)]
        internal struct NativeTypeDataUnion
        {
            internal NativeClassTypeData ClassTypeData;
            internal NativeUnaryTypeData UnaryTypeData;
            internal NativeArrayTypeData ArrayTypeData;
            internal NativeNaryTypeData NaryTypeData;
        }

        [ExtendedLayout(ExtendedLayoutKind.CStruct)]
        internal struct NativeBasicTypeData
        {
            internal uint ElementType;
            internal uint MetadataToken;
            internal ulong Assembly;
            internal ulong TypeHandle;
        }

        [ExtendedLayout(ExtendedLayoutKind.CStruct)]
        internal struct NativeClassTypeData
        {
            internal uint MetadataToken;
            internal ulong Assembly;
            internal ulong TypeHandle;
        }

        [ExtendedLayout(ExtendedLayoutKind.CStruct)]
        internal struct NativeUnaryTypeData
        {
            internal NativeBasicTypeData UnaryTypeArgument;
        }

        [ExtendedLayout(ExtendedLayoutKind.CStruct)]
        internal struct NativeArrayTypeData
        {
            internal NativeBasicTypeData ArrayTypeArgument;
            internal uint ArrayRank;
        }

        [ExtendedLayout(ExtendedLayoutKind.CStruct)]
        internal struct NativeNaryTypeData
        {
            internal ulong TypeHandle;
        }

        [DebuggerHidden]
        internal static RuntimeType[] ReadTypeArguments(ReadOnlySpan<byte> typeData, int count)
        {
            if (typeData.Length % sizeof(NativeTypeArgument) != 0)
            {
                throw new ArgumentException();
            }

            TypeDataWalk walk = new(MemoryMarshal.Cast<byte, NativeTypeArgument>(typeData));
            RuntimeType[] types = walk.ReadTypeHandles(count);
            if (!walk.IsFinished)
            {
                throw new ArgumentException();
            }

            return types;
        }

        private static ReadOnlySpan<byte> GetTypeData(byte* data, uint nodeCount) =>
            new(data, checked((int)nodeCount * sizeof(NativeTypeArgument)));

        private static RuntimeModule GetModule(nint nativeAssembly)
        {
            if (nativeAssembly == 0)
            {
                throw new ArgumentException();
            }

            return (RuntimeModule)RuntimeAssembly.GetAssemblyFromHandle(nativeAssembly).ManifestModule;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The debugger supplies tokens from the target's actual loaded metadata.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern",
            Justification = "The debugger supplies the target's actual runtime generic arguments.")]
        private static RuntimeType ReadInstantiation(
            RuntimeModule module, int token, RuntimeType[] arguments, bool allowUninstantiated = false)
        {
            RuntimeType type = (RuntimeType)module.ResolveType(token);
            if (arguments.Length != 0 || (!allowUninstantiated && type.IsGenericTypeDefinition))
            {
                type = (RuntimeType)type.MakeGenericType(arguments);
            }

            return type;
        }

        [StackTraceHidden]
        private ref struct TypeDataWalk
        {
            private ReadOnlySpan<NativeTypeArgument> _remaining;

            internal TypeDataWalk(ReadOnlySpan<NativeTypeArgument> data)
            {
                _remaining = data;
            }

            internal readonly bool IsFinished => _remaining.IsEmpty;

            internal RuntimeType[] ReadTypeHandles(int count)
            {
                if ((uint)count > (uint)_remaining.Length)
                {
                    throw new ArgumentException();
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
                    throw new ArgumentException();
                }

                NativeTypeArgument node = _remaining[0];
                _remaining = _remaining[1..];
                if (!BitConverter.IsLittleEndian)
                {
                    node.Data.ElementType = BinaryPrimitives.ReverseEndianness(node.Data.ElementType);
                    node.Data.TypeData.ClassTypeData.MetadataToken =
                        BinaryPrimitives.ReverseEndianness(node.Data.TypeData.ClassTypeData.MetadataToken);
                    node.Data.TypeData.ClassTypeData.Assembly =
                        BinaryPrimitives.ReverseEndianness(node.Data.TypeData.ClassTypeData.Assembly);
                    node.Data.TypeData.ArrayTypeData.ArrayRank =
                        BinaryPrimitives.ReverseEndianness(node.Data.TypeData.ArrayTypeData.ArrayRank);
                    node.ChildCount = BinaryPrimitives.ReverseEndianness(node.ChildCount);
                }

                uint elementValue = node.Data.ElementType;
                if (elementValue > byte.MaxValue)
                {
                    throw new ArgumentException();
                }

                CorElementType elementType = (CorElementType)elementValue;
                uint childCount = node.ChildCount;
                if (childCount > (uint)_remaining.Length)
                {
                    throw new ArgumentException();
                }

                switch (elementType)
                {
                    case CorElementType.ELEMENT_TYPE_ARRAY:
                    case CorElementType.ELEMENT_TYPE_SZARRAY:
                    case CorElementType.ELEMENT_TYPE_PTR:
                    case CorElementType.ELEMENT_TYPE_BYREF:
                        if (childCount != 1)
                        {
                            throw new ArgumentException();
                        }

                        RuntimeType element = ReadTypeHandle();
                        return elementType switch
                        {
                            CorElementType.ELEMENT_TYPE_ARRAY => (RuntimeType)element.MakeArrayType(
                                unchecked((int)node.Data.TypeData.ArrayTypeData.ArrayRank)),
                            CorElementType.ELEMENT_TYPE_SZARRAY => (RuntimeType)element.MakeArrayType(),
                            CorElementType.ELEMENT_TYPE_PTR => (RuntimeType)element.MakePointerType(),
                            _ => (RuntimeType)element.MakeByRefType(),
                        };

                    case CorElementType.ELEMENT_TYPE_CLASS:
                    case CorElementType.ELEMENT_TYPE_VALUETYPE:
                        nint assembly = (nint)checked((nuint)node.Data.TypeData.ClassTypeData.Assembly);
                        RuntimeModule module = GetModule(assembly);
                        int token = unchecked((int)node.Data.TypeData.ClassTypeData.MetadataToken);
                        return ReadInstantiation(module, token, ReadTypeHandles((int)childCount));

                    case CorElementType.ELEMENT_TYPE_FNPTR:
                        if (childCount == 0)
                        {
                            throw new ArgumentException();
                        }

                        RuntimeType[] signature = ReadTypeHandles((int)childCount);
                        return signature[0].TypeHandle.MakeFunctionPointer(signature.AsSpan(1).ToArray(), isUnmanaged: false);

                    default:
                        if (childCount != 0)
                        {
                            throw new ArgumentException();
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
                            _ => throw new ArgumentException(),
                        });
                }
            }
        }
    }
}
