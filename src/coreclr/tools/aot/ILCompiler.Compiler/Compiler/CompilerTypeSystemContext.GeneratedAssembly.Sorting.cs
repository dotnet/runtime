// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;

namespace ILCompiler
{
    public partial class CompilerTypeSystemContext
    {
        // Functionality related to deterministic ordering of types and members
        internal partial class CompilerGeneratedType : MetadataType
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
