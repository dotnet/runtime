// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

            int pointerSize = type.Context.Target.PointerSize;
            GCPointerMapBuilder builder = new GCPointerMapBuilder(type.InstanceByteCount.AsInt, pointerSize);
            FromInstanceLayoutHelper(ref builder, type, pointerSize);

            return builder.ToGCMap();
        }

        private static void FromInstanceLayoutHelper(ref GCPointerMapBuilder builder, MetadataType type, int pointerSize)
        {
            if (!type.IsValueType && type.HasBaseType)
            {
                MetadataType baseType = (MetadataType)type.BaseType;
                GCPointerMapBuilder baseLayoutBuilder = builder.GetInnerBuilder(0, baseType.InstanceByteCount.AsInt);
                FromInstanceLayoutHelper(ref baseLayoutBuilder, baseType, pointerSize);
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
                        builder.MarkGCPointer(field.Offset.AsInt + pointerSize * i);
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
                            FromInstanceLayoutHelper(ref innerBuilder, fieldDefType, pointerSize);
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
                        int pointerSize = type.Context.Target.PointerSize;
                        GCPointerMapBuilder innerBuilder =
                            builder.GetInnerBuilder(field.Offset.AsInt, fieldDefType.InstanceByteCount.AsInt);
                        FromInstanceLayoutHelper(ref innerBuilder, fieldDefType, pointerSize);
                    }
                }
            }

            Debug.Assert(builder.ToGCMap().Size * type.Context.Target.PointerSize >= type.GCStaticFieldSize.AsInt);
            return builder.ToGCMap();
        }

        /// <summary>
        /// Computes the GC pointer map of the thread static region of the type.
        /// </summary>
        public static GCPointerMap FromThreadStaticLayout(MetadataType type)
        {
            GCPointerMapBuilder builder = new GCPointerMapBuilder(type.ThreadGcStaticFieldSize.AsInt, type.Context.Target.PointerSize);

            foreach (FieldDesc field in type.GetFields())
            {
                if (!field.IsStatic || field.HasRva || field.IsLiteral || !field.IsThreadStatic || !field.HasGCStaticBase)
                    continue;

                TypeDesc fieldType = field.FieldType;
                if (fieldType.IsGCPointer)
                {
                    builder.MarkGCPointer(field.Offset.AsInt);
                }
                else if (fieldType.IsValueType)
                {
                    var fieldDefType = (MetadataType)fieldType;
                    if (fieldDefType.ContainsGCPointers)
                    {
                        int pointerSize = type.Context.Target.PointerSize;
                        GCPointerMapBuilder innerBuilder =
                            builder.GetInnerBuilder(field.Offset.AsInt, fieldDefType.InstanceByteCount.AsInt);
                        FromInstanceLayoutHelper(ref innerBuilder, fieldDefType, pointerSize);
                    }
                }
            }

            Debug.Assert(builder.ToGCMap().Size * type.Context.Target.PointerSize >= type.ThreadGcStaticFieldSize.AsInt);
            return builder.ToGCMap();
        }
    }
}
