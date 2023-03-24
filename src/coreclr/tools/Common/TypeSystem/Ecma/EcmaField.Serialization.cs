// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem.Ecma
{
    public partial class EcmaField
    {
        public override bool IsNotSerialized
        {
            get
            {
                return (GetFieldFlags(FieldFlags.BasicMetadataCache | FieldFlags.NotSerialized) & FieldFlags.NotSerialized) != 0;
            }
        }
    }
}
