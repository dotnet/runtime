// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Runtime;

namespace Internal.Reflection.Execution.FieldAccessors
{
    internal abstract class RegularStaticFieldAccessor : WritableStaticFieldAccessor
    {
        protected RegularStaticFieldAccessor(IntPtr cctorContext, IntPtr staticsBase, int fieldOffset, FieldTableFlags fieldBase, RuntimeTypeHandle fieldTypeHandle)
            : base(cctorContext, fieldTypeHandle)
        {
            StaticsBase = staticsBase;
            _fieldFlags = fieldBase;
            FieldOffset = fieldOffset;
        }

        protected IntPtr StaticsBase { get; }
        private readonly FieldTableFlags _fieldFlags;
        protected int FieldOffset { get; }
        protected FieldTableFlags FieldBase => _fieldFlags & FieldTableFlags.StorageClass;
        protected sealed override bool IsFieldInitOnly => (_fieldFlags & FieldTableFlags.IsInitOnly) == FieldTableFlags.IsInitOnly;
    }
}
