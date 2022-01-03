// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;

namespace ILCompiler
{
    partial class CompilerTypeSystemContext
    {
        // Functionality related to determinstic ordering of types and members
        partial class CompilerGeneratedType : MetadataType
        {
            protected override int ClassCode => -1036681447;

            protected override int CompareToImpl(TypeDesc other, TypeSystemComparer comparer)
            {
                // Should be a singleton
                throw new NotSupportedException();
            }
        }
    }
}
