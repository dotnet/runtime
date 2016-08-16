// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
//using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security;
using System.Runtime.Serialization;

namespace System.Collections.Generic
{    
    [Serializable]
    [TypeDependencyAttribute("System.Collections.Generic.ObjectComparer`1")] 
    public abstract class Comparer<T> : IComparer, IComparer<T>
    {
        static readonly Comparer<T> defaultComparer = CreateComparer();

        public static Comparer<T> Default {
            get {
                Contract.Ensures(Contract.Result<Comparer<T>>() != null);
                return defaultComparer;
            }
        }

        public static Comparer<T> Create(Comparison<T> comparison)
        {
            Contract.Ensures(Contract.Result<Comparer<T>>() != null);

            if (comparison == null)
                throw new ArgumentNullException("comparison");

            return new ComparisonComparer<T>(comparison);
        }

        //
        // Note that logic in this method is replicated in vm\compile.cpp to ensure that NGen
        // saves the right instantiations
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        private static Comparer<T> CreateComparer()
        {
            object result = null;
            RuntimeType t = (RuntimeType)typeof(T);

            // If T implements IComparable<T> return a GenericComparer<T>
            if (typeof(IComparable<T>).IsAssignableFrom(t))
            {
                result = RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(GenericComparer<int>), t);
            }
            else if (default(T) == null)
            {
                // If T is a Nullable<U> where U implements IComparable<U> return a NullableComparer<U>
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                    RuntimeType u = (RuntimeType)t.GetGenericArguments()[0];
                    if (typeof(IComparable<>).MakeGenericType(u).IsAssignableFrom(u)) {
                        result = RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(NullableComparer<int>), u);
                    }
                }
            }
            else if (t.IsEnum)
            {
                // Explicitly call Enum.GetUnderlyingType here. Although GetTypeCode
                // ends up doing this anyway, we end up avoiding an unnecessary P/Invoke
                // and virtual method call.
                TypeCode underlyingTypeCode = Type.GetTypeCode(Enum.GetUnderlyingType(t));
                
                // Depending on the enum type, we need to special case the comparers so that we avoid boxing
                // Specialize differently for signed/unsigned types so we avoid problems with large numbers
                switch (underlyingTypeCode)
                {
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                        result = RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(Int32EnumComparer<int>), t);
                        break;
                    case TypeCode.Byte:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                        result = RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(UInt32EnumComparer<uint>), t);
                        break;
                    // 64-bit enums: use UnsafeEnumCastLong
                    case TypeCode.Int64:
                        result = RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(Int64EnumComparer<long>), t);
                        break;
                    case TypeCode.UInt64:
                        result = RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(UInt64EnumComparer<ulong>), t);
                        break;
                }
            }
            
            return result != null ?
                (Comparer<T>)result :
                new ObjectComparer<T>(); // Fallback to ObjectComparer, which uses boxing
        }

        public abstract int Compare(T x, T y);

        int IComparer.Compare(object x, object y) {
            if (x == null) return y == null ? 0 : -1;
            if (y == null) return 1;
            if (x is T && y is T) return Compare((T)x, (T)y);
            ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArgumentForComparison);
            return 0;
        }
    }
    
    // Note: although there is a lot of shared code in the following
    // comparers, we do not incorporate it into a base class for perf
    // reasons. Adding another base class (even one with no fields)
    // means another generic instantiation, which can be costly esp.
    // for value types.
    
    [Serializable]
    internal sealed class GenericComparer<T> : Comparer<T> where T : IComparable<T>
    {    
        public override int Compare(T x, T y) {
            if (x != null) {
                if (y != null) return x.CompareTo(y);
                return 1;
            }
            if (y != null) return -1;
            return 0;
        }

        // Equals method for the comparer itself. 
        public override bool Equals(Object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();
    }

    [Serializable]
    internal sealed class NullableComparer<T> : Comparer<T?> where T : struct, IComparable<T>
    {
        public override int Compare(Nullable<T> x, Nullable<T> y) {
            if (x.HasValue) {
                if (y.HasValue) return x.value.CompareTo(y.value);
                return 1;
            }
            if (y.HasValue) return -1;
            return 0;
        }

        // Equals method for the comparer itself. 
        public override bool Equals(Object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();
    }

    [Serializable]
    internal sealed class ObjectComparer<T> : Comparer<T>
    {
        public override int Compare(T x, T y) {
            return System.Collections.Comparer.Default.Compare(x, y);
        }

        // Equals method for the comparer itself. 
        public override bool Equals(Object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();
    }

    [Serializable]
    internal sealed class ComparisonComparer<T> : Comparer<T>
    {
        private readonly Comparison<T> _comparison;

        public ComparisonComparer(Comparison<T> comparison) {
            _comparison = comparison;
        }

        public override int Compare(T x, T y) {
            return _comparison(x, y);
        }
    }

    // Enum comparers (specialized to avoid boxing)
    // NOTE: Each of these needs to implement ISerializable
    // and have a SerializationInfo/StreamingContext ctor,
    // since we want to serialize as ObjectComparer for
    // back-compat reasons (see below).

    [Serializable]
    internal sealed class Int32EnumComparer<T> : Comparer<T>, ISerializable where T : struct
    {
        public Int32EnumComparer()
        {
            Contract.Assert(typeof(T).IsEnum, "This type is only intended to be used to compare enums!");
        }
        
        // Used by the serialization engine.
        private Int32EnumComparer(SerializationInfo info, StreamingContext context) { }
        
        public override int Compare(T x, T y)
        {
            int ix = JitHelpers.UnsafeEnumCast(x);
            int iy = JitHelpers.UnsafeEnumCast(y);
            return ix.CompareTo(iy);
        }

        // Equals method for the comparer itself. 
        public override bool Equals(Object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();

        [SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Previously Comparer<T> was not specialized for enums,
            // and instead fell back to ObjectComparer which uses boxing.
            // Set the type as ObjectComparer here so code that serializes
            // Comparer for enums will not break.
            info.SetType(typeof(ObjectComparer<T>));
        }
    }

    [Serializable]
    internal sealed class UInt32EnumComparer<T> : Comparer<T>, ISerializable where T : struct
    {
        public UInt32EnumComparer()
        {
            Contract.Assert(typeof(T).IsEnum, "This type is only intended to be used to compare enums!");
        }
        
        // Used by the serialization engine.
        private UInt32EnumComparer(SerializationInfo info, StreamingContext context) { }
        
        public override int Compare(T x, T y)
        {
            uint ix = (uint)JitHelpers.UnsafeEnumCast(x);
            uint iy = (uint)JitHelpers.UnsafeEnumCast(y);
            return ix.CompareTo(iy);
        }

        // Equals method for the comparer itself. 
        public override bool Equals(Object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();

        [SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.SetType(typeof(ObjectComparer<T>));
        }
    }

    [Serializable]
    internal sealed class Int64EnumComparer<T> : Comparer<T>, ISerializable where T : struct
    {
        public Int64EnumComparer()
        {
            Contract.Assert(typeof(T).IsEnum, "This type is only intended to be used to compare enums!");
        }
        
        // Used by the serialization engine.
        private Int64EnumComparer(SerializationInfo info, StreamingContext context) { }
        
        public override int Compare(T x, T y)
        {
            long lx = JitHelpers.UnsafeEnumCastLong(x);
            long ly = JitHelpers.UnsafeEnumCastLong(y);
            return lx.CompareTo(ly);
        }

        // Equals method for the comparer itself. 
        public override bool Equals(Object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();

        [SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.SetType(typeof(ObjectComparer<T>));
        }
    }

    [Serializable]
    internal sealed class UInt64EnumComparer<T> : Comparer<T>, ISerializable where T : struct
    {
        public UInt64EnumComparer()
        {
            Contract.Assert(typeof(T).IsEnum, "This type is only intended to be used to compare enums!");
        }
        
        // Used by the serialization engine.
        private UInt64EnumComparer(SerializationInfo info, StreamingContext context) { }
        
        public override int Compare(T x, T y)
        {
            ulong lx = (ulong)JitHelpers.UnsafeEnumCastLong(x);
            ulong ly = (ulong)JitHelpers.UnsafeEnumCastLong(y);
            return lx.CompareTo(ly);
        }

        // Equals method for the comparer itself. 
        public override bool Equals(Object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();

        [SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.SetType(typeof(ObjectComparer<T>));
        }
    }
}
