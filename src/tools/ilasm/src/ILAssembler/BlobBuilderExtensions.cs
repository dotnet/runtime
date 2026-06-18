// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace ILAssembler
{
    internal static class BlobBuilderExtensions
    {
        public static BlobBuilder SerializeSequence<T>(this ImmutableArray<T> sequence)
        {
            BlobBuilder builder = new BlobBuilder();
            builder.WriteSerializedSequence(sequence);
            return builder;
        }

        public static void WriteSerializedSequence<T>(this BlobBuilder writer, ImmutableArray<T> sequence)
        {
            foreach (T value in sequence)
            {
                WriteSerializedValue(writer, value);
            }
        }

        public static void WriteSerializedValue<T>(this BlobBuilder writer, T value)
        {
            if (typeof(T) == typeof(bool))
            {
                writer.WriteBoolean((bool)(object)value!);
            }
            else if (typeof(T) == typeof(int))
            {
                writer.WriteInt32((int)(object)value!);
            }
            else if (typeof(T) == typeof(byte))
            {
                writer.WriteByte((byte)(object)value!);
            }
            else if (typeof(T) == typeof(char))
            {
                writer.WriteUInt16((char)(object)value!);
            }
            else if (typeof(T) == typeof(double))
            {
                writer.WriteDouble((double)(object)value!);
            }
            else if (typeof(T) == typeof(short))
            {
                writer.WriteInt16((short)(object)value!);
            }
            else if (typeof(T) == typeof(long))
            {
                writer.WriteInt64((long)(object)value!);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                writer.WriteSByte((sbyte)(object)value!);
            }
            else if (typeof(T) == typeof(float))
            {
                writer.WriteSingle((float)(object)value!);
            }
            else if (typeof(T) == typeof(ushort))
            {
                writer.WriteUInt16((ushort)(object)value!);
            }
            else if (typeof(T) == typeof(uint))
            {
                writer.WriteUInt32((uint)(object)value!);
            }
            else if (typeof(T) == typeof(ulong))
            {
                writer.WriteUInt64((ulong)(object)value!);
            }
            else if (typeof(T) == typeof(string))
            {
                writer.WriteSerializedString((string?)(object?)value);
            }
        }

        public static void WriteTypeEntity(this BlobBuilder builder, EntityRegistry.TypeEntity entity)
        {
            if (entity is EntityRegistry.FakeTypeEntity fakeEntity)
            {
                builder.WriteCompressedInteger(CodedIndex.TypeDefOrRefOrSpec(fakeEntity.TypeSignatureHandle));
            }
            else
            {
                builder.WriteCompressedInteger(CodedIndex.TypeDefOrRefOrSpec(entity.Handle));
            }
        }
    }
}
