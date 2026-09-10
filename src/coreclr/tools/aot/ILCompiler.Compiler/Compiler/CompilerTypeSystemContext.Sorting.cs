// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler
{
    // Functionality related to deterministic ordering of types and members
    public partial class CompilerTypeSystemContext
    {
        private partial class DefaultInterfaceMethodImplementationInstantiationThunk
        {
            protected override int ClassCode => -789598;

            protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
            {
                var otherMethod = (DefaultInterfaceMethodImplementationInstantiationThunk)other;

                int result = System.Collections.Generic.Comparer<int>.Default.Compare(_interfaceIndex, otherMethod._interfaceIndex);
                if (result != 0)
                    return result;

                return comparer.Compare(_targetMethod, otherMethod._targetMethod);
            }
        }
    }
}
