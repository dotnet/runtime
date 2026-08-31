// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    public partial class FieldDesc
    {
        /// <summary>
        /// Returns description of how the field should be marshalled to native code.
        /// </summary>
        public virtual MarshalAsDescriptor GetMarshalAsDescriptor()
        {
            return null;
        }
    }

    public partial class FieldForInstantiatedType
    {
        public override MarshalAsDescriptor GetMarshalAsDescriptor()
        {
            return _fieldDef.GetMarshalAsDescriptor();
        }
    }
}
