// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Internal.TypeSystem
{
    // Additional members of FieldDesc related to code generation.
    partial class FieldDesc
    {
        /// <summary>
        /// Gets a value indicating whether this is a field that needs to be treated
        /// specially.
        /// </summary>
        public virtual bool IsIntrinsic
        {
            get
            {
                return false;
            }
        }
    }

    partial class FieldForInstantiatedType
    {
        public override bool IsIntrinsic
        {
            get
            {
                return _fieldDef.IsIntrinsic;
            }
        }
    }
}
