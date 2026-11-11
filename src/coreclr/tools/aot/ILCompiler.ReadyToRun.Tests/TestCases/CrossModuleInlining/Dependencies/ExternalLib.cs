using System;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Runtime.Versioning
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
    internal sealed class NonVersionableAttribute : Attribute
    {
    }
}

public static class ExternalLib
{
    public static int ExternalValue => 99;

    public class ExternalType
    {
        public int Value { get; set; }
    }

    public struct ExternalStruct
    {
        public int Value;
    }

    public class ExternalGenericBase<T>
    {
        public int BaseValue { get; set; }
    }

    public class Outer
    {
        public class Inner
        {
            public static int NestedValue => 77;
        }
    }

    [NonVersionable]
    internal struct UnreferencedInternalStruct<T>
    {
        public int Value;
    }

    [NonVersionable]
    public struct PublicNonVersionableHolder<T>
    {
        internal UnreferencedInternalStruct<T>[] _derived;

        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetDerived(bool flag)
        {
            UnreferencedInternalStruct<T>[] local = null;
            if (flag)
            {
                local = _derived;
            }

            return local;
        }
    }
}
