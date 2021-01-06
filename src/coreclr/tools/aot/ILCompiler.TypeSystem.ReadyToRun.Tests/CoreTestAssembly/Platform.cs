// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable 649
#pragma warning disable 169

namespace System
{
    // Dummy core types to allow us compiling this assembly as a core library so that the type
    // system tests don't have a dependency on a real core library.

    public class Object
    {
        internal IntPtr m_pEEType;

        public virtual bool Equals(object other)
        {
            return false;
        }

        public virtual int GetHashCode()
        {
            return 0;
        }

        public virtual string ToString() { return null; }

        ~Object()
        {
        }
    }

    public struct Void { }
    public struct Boolean { }
    public struct Char { }
    public struct SByte { }
    public struct Byte { }
    public struct Int16 { }
    public struct UInt16 { }
    public struct Int32 { }
    public struct UInt32 { }
    public struct Int64 { }
    public struct UInt64 { }
    public struct IntPtr { }
    public struct UIntPtr { }
    public struct Single { }
    public struct Double { }
    public abstract class ValueType { }
    public abstract class Enum : ValueType { }
    public struct Nullable<T> where T : struct { }
    
    public sealed class String { }
    public abstract class Array : System.Collections.IList { }
    public abstract class Delegate { }
    public abstract class MulticastDelegate : Delegate { }

    public struct RuntimeTypeHandle { }
    public struct RuntimeMethodHandle { }
    public struct RuntimeFieldHandle { }

    public class Attribute { }

    public class ThreadStaticAttribute : Attribute { }

    public class Array<T> : Array, System.Collections.Generic.IList<T> { }

    public class Exception { }

    public ref struct TypedReference
    {
        private readonly ByReference<byte> _value;
        private readonly RuntimeTypeHandle _typeHandle;
    }

    public ref struct ByReference<T> { }
}

namespace System.Collections
{
    interface IEnumerable { }

    interface ICollection : IEnumerable { }

    interface IList : ICollection { }
}

namespace System.Collections.Generic
{
    interface IEnumerable<out T> { }

    interface ICollection<T> : IEnumerable<T> { }

    interface IList<T> : ICollection<T> { }
}

namespace System.Runtime.InteropServices
{
    public enum LayoutKind
    {
        Sequential = 0, // 0x00000008,
        Explicit = 2, // 0x00000010,
        Auto = 3, // 0x00000000,
    }

    public sealed class StructLayoutAttribute : Attribute
    {
        internal LayoutKind _val;

        public StructLayoutAttribute(LayoutKind layoutKind)
        {
            _val = layoutKind;
        }

        public LayoutKind Value { get { return _val; } }
        public int Pack;
        public int Size;
    }

    public sealed class FieldOffsetAttribute : Attribute
    {
        private int _val;
        public FieldOffsetAttribute(int offset)
        {
            _val = offset;
        }
        public int Value { get { return _val; } }
    }
}

namespace System.Runtime.CompilerServices
{
    public sealed class IsByRefLikeAttribute : Attribute
    {
    }
}
