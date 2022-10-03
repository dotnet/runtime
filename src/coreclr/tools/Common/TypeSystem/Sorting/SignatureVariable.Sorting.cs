// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Internal.TypeSystem
{
    // Functionality related to deterministic ordering of types
    partial class SignatureVariable
    {
        protected internal sealed override int CompareToImpl(TypeDesc other, TypeSystemComparer comparer)
        {
            return ((SignatureVariable)other).Index - Index;
        }
    }

    partial class SignatureTypeVariable
    {
        protected internal override int ClassCode
        {
            get
            {
                return 1042124696;
            }
        }
    }

    partial class SignatureMethodVariable
    {
        protected internal override int ClassCode
        {
            get
            {
                return 144542889;
            }
        }

    }
}
