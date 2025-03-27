// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    public sealed partial class FieldForInstantiatedType : FieldDesc
    {
        public override LayoutInt MetadataOffset
        {
            get
            {
                return _fieldDef.MetadataOffset;
            }
        }
    }
}
