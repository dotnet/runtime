// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Internal.TypeSystem
{
    // Api extensions for fields that allow keeping track of field layout

    public partial class FieldDesc
    {
        private LayoutInt _offset = FieldAndOffset.InvalidOffset;

        public LayoutInt Offset
        {
            get
            {
                if (_offset == FieldAndOffset.InvalidOffset)
                {
                    if (IsStatic)
                        OwningType.ComputeStaticFieldLayout(StaticLayoutKind.StaticRegionSizesAndFields);
                    else
                        OwningType.ComputeInstanceLayout(InstanceLayoutKind.TypeAndFields);

                    // If the offset still wasn't computed, this must be a field that doesn't participate in layout
                    // (either literal or RVA mapped). We shouldn't be asking for the offset.
                    Debug.Assert(_offset != FieldAndOffset.InvalidOffset);
                }
                return _offset;
            }
        }

        /// <summary>
        /// For static fields, represents whether or not the field is held in the GC or non GC statics region.
        /// </summary>
        public bool HasGCStaticBase
        {
            get
            {
                Debug.Assert(IsStatic);
                return Context.ComputeHasGCStaticBase(this);
            }
        }

        internal void InitializeOffset(LayoutInt offset)
        {
            Debug.Assert(_offset == FieldAndOffset.InvalidOffset || _offset == offset);
            _offset = offset;
        }
    }
}
