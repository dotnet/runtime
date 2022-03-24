// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler
{
    // Functionality related to determinstic ordering of types and members
    partial class CompilerTypeSystemContext
    {
        partial class BoxedValueType
        {
            protected override int ClassCode => 1062019524;

            protected override int CompareToImpl(TypeDesc other, TypeSystemComparer comparer)
            {
                return comparer.Compare(ValueTypeRepresented, ((BoxedValueType)other).ValueTypeRepresented);
            }
        }

        partial class GenericUnboxingThunk
        {
            protected override int ClassCode => -247515475;

            protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
            {
                var otherMethod = (GenericUnboxingThunk)other;
                return comparer.Compare(_targetMethod, otherMethod._targetMethod);
            }
        }

        partial class UnboxingThunk
        {
            protected override int ClassCode => 446545583;

            protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
            {
                var otherMethod = (UnboxingThunk)other;
                return comparer.Compare(_targetMethod, otherMethod._targetMethod);
            }
        }

        partial class ValueTypeInstanceMethodWithHiddenParameter
        {
            protected override int ClassCode => 2131875345;

            protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
            {
                var otherMethod = (ValueTypeInstanceMethodWithHiddenParameter)other;
                return comparer.Compare(_methodRepresented, otherMethod._methodRepresented);
            }
        }

        partial class DefaultInterfaceMethodImplementationInstantiationThunk
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

        partial class DefaultInterfaceMethodImplementationWithHiddenParameter
        {
            protected override int ClassCode => 4903209;

            protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
            {
                var otherMethod = (DefaultInterfaceMethodImplementationWithHiddenParameter)other;
                return comparer.Compare(_methodRepresented, otherMethod._methodRepresented);
            }
        }
    }
}
