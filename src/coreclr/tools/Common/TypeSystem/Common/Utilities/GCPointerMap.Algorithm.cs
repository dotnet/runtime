// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    partial struct GCPointerMap
    {
        /// <summary>
        /// Computes the GC pointer map for the instance fields of <paramref name="type"/>.
        /// </summary>
        public static GCPointerMap FromInstanceLayout(DefType type)
        {
            Debug.Assert(type.ContainsGCPointers);

            int pointerSize = type.Context.Target.PointerSize;
            GCPointerMapBuilder builder = new GCPointerMapBuilder(type.InstanceByteCount.AsInt, pointerSize);
            FromInstanceLayoutHelper(ref builder, type, pointerSize);

            return builder.ToGCMap();
        }

        private static void FromInstanceLayoutHelper(ref GCPointerMapBuilder builder, DefType type, int pointerSize)
        {
            if (!type.IsValueType && type.HasBaseType)
            {
                DefType baseType = type.BaseType;
                GCPointerMapBuilder baseLayoutBuilder = builder.GetInnerBuilder(0, baseType.InstanceByteCount.AsInt);
                FromInstanceLayoutHelper(ref baseLayoutBuilder, baseType, pointerSize);
            }

            int repeat = 1;
            if (type is InstantiatedType it)
            {
                if (it.Name == "ValueArray`2" && it.Namespace == "System")
                {
                    if (it.Instantiation[1] is ArrayType arr)
                    {
                        repeat = arr.Rank;
                    }
                }
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
                    var fieldDefType = (DefType)fieldType;
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
    }
}
