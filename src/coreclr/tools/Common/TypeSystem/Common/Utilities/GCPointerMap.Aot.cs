// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    partial struct GCPointerMap
    {
        /// <summary>
        /// Computes the GC pointer map of the GC static region of the type.
        /// </summary>
        public static GCPointerMap FromStaticLayout(DefType type)
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
                    var fieldDefType = (DefType)fieldType;
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

        /// <summary>
        /// Computes the GC pointer map of the thread static region of the type.
        /// </summary>
        public static GCPointerMap FromThreadStaticLayout(DefType type)
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
                    var fieldDefType = (DefType)fieldType;
                    if (fieldDefType.ContainsGCPointers)
                    {
                        GCPointerMapBuilder innerBuilder =
                            builder.GetInnerBuilder(field.Offset.AsInt, fieldDefType.InstanceByteCount.AsInt);
                        FromInstanceLayoutHelper(ref innerBuilder, fieldDefType);
                    }
                }
            }

            Debug.Assert(builder.ToGCMap().Size * type.Context.Target.PointerSize >= type.ThreadGcStaticFieldSize.AsInt);
            return builder.ToGCMap();
        }
    }
}
