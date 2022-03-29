// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    public sealed class LocalBuilder : LocalVariableInfo
    {
        internal LocalBuilder()
        {
            // Prevent generating a default constructor
        }

        public override bool IsPinned
        {
            get
            {
                return default;
            }
        }

        public override int LocalIndex
        {
            get
            {
                return default;
            }
        }

        public override Type LocalType
        {
            get
            {
                return default;
            }
        }
    }
}
