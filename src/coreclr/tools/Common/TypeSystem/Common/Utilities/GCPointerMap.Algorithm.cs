// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    public partial struct GCPointerMap
    {
        /// <summary>
        /// Computes the GC pointer map for the instance fields of <paramref name="type"/>.
        /// </summary>
        public static GCPointerMap FromInstanceLayout(MetadataType type)
        {
            Debug.Assert(type.ContainsGCPointers);

            GCPointerMapBuilder builder = new GCPointerMapBuilder(type.InstanceByteCount.AsInt, type.Context.Target.PointerSize);
            FromInstanceLayoutHelper(ref builder, type);

            return builder.ToGCMap();
        }

        private static void FromInstanceLayoutHelper(ref GCPointerMapBuilder builder, MetadataType type)
        {
            if (!type.IsValueType && type.HasBaseType)
            {
                MetadataType baseType = (MetadataType)type.BaseType;
                GCPointerMapBuilder baseLayoutBuilder = builder.GetInnerBuilder(0, baseType.InstanceByteCount.AsInt);
                FromInstanceLayoutHelper(ref baseLayoutBuilder, baseType);
            }

            int repeat = 1;
            if (type.IsInlineArray)
            {
                repeat = ((MetadataType)type).GetInlineArrayLength();
            }

            foreach (FieldDesc field in type.GetFields())
            {
                if (field.IsStatic)
                    continue;

                TypeDesc fieldType = field.FieldType;
                if (fieldType.IsGCPointer)
                {
                    for (int i = 0; i < repeat; i++)
                    {
                        builder.MarkGCPointer(field.Offset.AsInt + type.Context.Target.PointerSize * i);
                    }
                }
                else if (fieldType.IsValueType)
                {
                    var fieldDefType = (MetadataType)fieldType;
                    if (fieldDefType.ContainsGCPointers)
                    {
                        for (int i = 0; i < repeat; i++)
                        {
                            int fieldSize = fieldDefType.InstanceByteCount.AsInt;
                            GCPointerMapBuilder innerBuilder =
                                builder.GetInnerBuilder(field.Offset.AsInt + fieldSize * i, fieldSize);
                            FromInstanceLayoutHelper(ref innerBuilder, fieldDefType);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Computes the GC pointer map of the GC static region of the type.
        /// </summary>
        public static GCPointerMap FromStaticLayout(MetadataType type)
        {
            GCPointerMapBuilder builder = new GCPointerMapBuilder(type.GCStaticFieldSize.AsInt, type.Context.Target.PointerSize);

            foreach (FieldDesc field in type.GetFields())
            {
                if (!field.IsStatic || field.HasRva || field.IsLiteral
                    || field.IsThreadStatic || !field.HasGCStaticBase)
                    continue;

                TypeDesc fieldType = field.FieldType;
                if (fieldType.IsGCPointer)
                {
                    builder.MarkGCPointer(field.Offset.AsInt);
                }
                else
                {
                    Debug.Assert(fieldType.IsValueType);
                    var fieldDefType = (MetadataType)fieldType;
                    if (fieldDefType.ContainsGCPointers)
                    {
                        GCPointerMapBuilder innerBuilder =
                            builder.GetInnerBuilder(field.Offset.AsInt, fieldDefType.InstanceByteCount.AsInt);
                        FromInstanceLayoutHelper(ref innerBuilder, fieldDefType);
                    }
                }
            }

            Debug.Assert(builder.ToGCMap().Size * type.Context.Target.PointerSize >= type.GCStaticFieldSize.AsInt);
            return builder.ToGCMap();
        }

        private static void MapThreadStaticsForType(GCPointerMapBuilder builder, MetadataType type, int baseOffset)
        {
            foreach (FieldDesc field in type.GetFields())
            {
                if (!field.IsStatic || field.HasRva || field.IsLiteral || !field.IsThreadStatic || !field.HasGCStaticBase)
                    continue;

                TypeDesc fieldType = field.FieldType;
                if (fieldType.IsGCPointer)
                {
                    builder.MarkGCPointer(field.Offset.AsInt + baseOffset);
                }
                else if (fieldType.IsValueType)
                {
                    var fieldDefType = (MetadataType)fieldType;
                    if (fieldDefType.ContainsGCPointers)
                    {
                        GCPointerMapBuilder innerBuilder =
                            builder.GetInnerBuilder(field.Offset.AsInt + baseOffset, fieldDefType.InstanceByteCount.AsInt);
                        FromInstanceLayoutHelper(ref innerBuilder, fieldDefType);
                    }
                }
            }
        }

        /// <summary>
        /// Computes the GC pointer map of the thread static region of the type.
        /// </summary>
        public static GCPointerMap FromThreadStaticLayout(MetadataType type)
        {
            GCPointerMapBuilder builder = new GCPointerMapBuilder(type.ThreadGcStaticFieldSize.AsInt, type.Context.Target.PointerSize);

            MapThreadStaticsForType(builder, type, baseOffset: 0);

            Debug.Assert(builder.ToGCMap().Size * type.Context.Target.PointerSize >= type.ThreadGcStaticFieldSize.AsInt);
            return builder.ToGCMap();
        }

        public static GCPointerMap FromInlinedThreadStatics(
            List<MetadataType> types,
            Dictionary<MetadataType, int> offsets,
            int threadStaticSize,
            int pointerSize)
        {
            GCPointerMapBuilder builder = new GCPointerMapBuilder(threadStaticSize, pointerSize);
            foreach (var type in types)
            {
                MapThreadStaticsForType(builder, type, offsets[type]);
            }

            return builder.ToGCMap();
        }
    }
}
