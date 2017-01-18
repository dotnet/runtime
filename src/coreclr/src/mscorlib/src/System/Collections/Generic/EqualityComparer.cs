// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Security;

namespace System.Collections.Generic
{
    using System.Globalization;
    using System.Runtime;
    using System.Runtime.CompilerServices;
    using System.Diagnostics.Contracts;
    
    [Serializable]
    [TypeDependencyAttribute("System.Collections.Generic.ObjectEqualityComparer`1")]
    public abstract class EqualityComparer<T> : IEqualityComparer, IEqualityComparer<T>
    {
        static readonly EqualityComparer<T> defaultComparer = CreateComparer();

        public static EqualityComparer<T> Default {
            get {
                Contract.Ensures(Contract.Result<EqualityComparer<T>>() != null);
                return defaultComparer;
            }
        }

        //
        // Note that logic in this method is replicated in vm\compile.cpp to ensure that NGen
        // saves the right instantiations
        //
        private static EqualityComparer<T> CreateComparer()
        {
            Contract.Ensures(Contract.Result<EqualityComparer<T>>() != null);
            
            object result = null;
            RuntimeType t = (RuntimeType)typeof(T);
            
            // Specialize type byte for performance reasons
            if (t == typeof(byte)) {
                result = new ByteEqualityComparer();
            }
            // If T implements IEquatable<T> return a GenericEqualityComparer<T>
            else if (typeof(IEquatable<T>).IsAssignableFrom(t))
            {
                result = RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(GenericEqualityComparer<int>), t);
            }
            else if (default(T) == null) // Reference type/Nullable
            {
                // If T is a Nullable<U> where U implements IEquatable<U> return a NullableEqualityComparer<U>
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                    RuntimeType u = (RuntimeType)t.GetGenericArguments()[0];
                    if (typeof(IEquatable<>).MakeGenericType(u).IsAssignableFrom(u)) {
                        result = RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(NullableEqualityComparer<int>), u);
                    }
                }
            }
            // See the METHOD__JIT_HELPERS__UNSAFE_ENUM_CAST and METHOD__JIT_HELPERS__UNSAFE_ENUM_CAST_LONG cases in getILIntrinsicImplementation
            else if (t.IsEnum) {
                TypeCode underlyingTypeCode = Type.GetTypeCode(Enum.GetUnderlyingType(t));

                // Depending on the enum type, we need to special case the comparers so that we avoid boxing
                // Note: We have different comparers for Short and SByte because for those types we need to make sure we call GetHashCode on the actual underlying type as the 
                // implementation of GetHashCode is more complex than for the other types.
                switch (underlyingTypeCode) {
                    case TypeCode.Int16: // short
                        result = RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(ShortEnumEqualityComparer<short>), t);
                        break;
                    case TypeCode.SByte:
                        result = RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(SByteEnumEqualityComparer<sbyte>), t);
                        break;
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Byte:
                    case TypeCode.UInt16: //ushort
                        result = RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(EnumEqualityComparer<int>), t);
                        break;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        result = RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(LongEnumEqualityComparer<long>), t);
                        break;
                }
            }
            
            return result != null ?
                (EqualityComparer<T>)result :
                new ObjectEqualityComparer<T>(); // Fallback to ObjectEqualityComparer, which uses boxing
        }

        [Pure]
        public abstract bool Equals(T x, T y);
        [Pure]
        public abstract int GetHashCode(T obj);

        internal virtual int IndexOf(T[] array, T value, int startIndex, int count) {
            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++) {
                if (Equals(array[i], value)) return i;
            }
            return -1;
        }

        internal virtual int LastIndexOf(T[] array, T value, int startIndex, int count) {
            int endIndex = startIndex - count + 1;
            for (int i = startIndex; i >= endIndex; i--) {
                if (Equals(array[i], value)) return i;
            }
            return -1;
        }

        int IEqualityComparer.GetHashCode(object obj) {
            if (obj == null) return 0;
            if (obj is T) return GetHashCode((T)obj);
            ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArgumentForComparison);
            return 0;            
        }                        

        bool IEqualityComparer.Equals(object x, object y) {
            if (x == y) return true;
            if (x == null || y == null) return false;
            if ((x is T) && (y is T)) return Equals((T)x, (T)y);
            ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArgumentForComparison);
            return false;
        }
    }

    // The methods in this class look identical to the inherited methods, but the calls
    // to Equal bind to IEquatable<T>.Equals(T) instead of Object.Equals(Object)
    [Serializable]
    internal class GenericEqualityComparer<T>: EqualityComparer<T> where T: IEquatable<T>
    {
        [Pure]
        public override bool Equals(T x, T y) {
            if (x != null) {
                if (y != null) return x.Equals(y);
                return false;
            }
            if (y != null) return false;
            return true;
        }

        [Pure]
        public override int GetHashCode(T obj) => obj?.GetHashCode() ?? 0;

        internal override int IndexOf(T[] array, T value, int startIndex, int count) {
            int endIndex = startIndex + count;
            if (value == null) {
                for (int i = startIndex; i < endIndex; i++) {
                    if (array[i] == null) return i;
                }
            }
            else {
                for (int i = startIndex; i < endIndex; i++) {
                    if (array[i] != null && array[i].Equals(value)) return i;
                }
            }
            return -1;
        }

        internal override int LastIndexOf(T[] array, T value, int startIndex, int count) {
            int endIndex = startIndex - count + 1;
            if (value == null) {
                for (int i = startIndex; i >= endIndex; i--) {
                    if (array[i] == null) return i;
                }
            }
            else {
                for (int i = startIndex; i >= endIndex; i--) {
                    if (array[i] != null && array[i].Equals(value)) return i;
                }
            }
            return -1;
        }

        // Equals method for the comparer itself.
        // If in the future this type is made sealed, change the is check to obj != null && GetType() == obj.GetType().
        public override bool Equals(Object obj) =>
            obj is GenericEqualityComparer<T>;

        // If in the future this type is made sealed, change typeof(...) to GetType().
        public override int GetHashCode() =>
            typeof(GenericEqualityComparer<T>).GetHashCode();
    }

    [Serializable]
    internal sealed class NullableEqualityComparer<T> : EqualityComparer<T?> where T : struct, IEquatable<T>
    {
        [Pure]
        public override bool Equals(T? x, T? y) {
            if (x.HasValue) {
                if (y.HasValue) return x.value.Equals(y.value);
                return false;
            }
            if (y.HasValue) return false;
            return true;
        }

        [Pure]
        public override int GetHashCode(T? obj) => obj.GetHashCode();

        internal override int IndexOf(T?[] array, T? value, int startIndex, int count) {
            int endIndex = startIndex + count;
            if (!value.HasValue) {
                for (int i = startIndex; i < endIndex; i++) {
                    if (!array[i].HasValue) return i;
                }
            }
            else {
                for (int i = startIndex; i < endIndex; i++) {
                    if (array[i].HasValue && array[i].value.Equals(value.value)) return i;
                }
            }
            return -1;
        }

        internal override int LastIndexOf(T?[] array, T? value, int startIndex, int count) {
            int endIndex = startIndex - count + 1;
            if (!value.HasValue) {
                for (int i = startIndex; i >= endIndex; i--) {
                    if (!array[i].HasValue) return i;
                }
            }
            else {
                for (int i = startIndex; i >= endIndex; i--) {
                    if (array[i].HasValue && array[i].value.Equals(value.value)) return i;
                }
            }
            return -1;
        }

        // Equals method for the comparer itself.
        public override bool Equals(Object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();
    }

    [Serializable]
    internal sealed class ObjectEqualityComparer<T>: EqualityComparer<T>
    {
        [Pure]
        public override bool Equals(T x, T y) {
            if (x != null) {
                if (y != null) return x.Equals(y);
                return false;
            }
            if (y != null) return false;
            return true;
        }

        [Pure]
        public override int GetHashCode(T obj) => obj?.GetHashCode() ?? 0;

        internal override int IndexOf(T[] array, T value, int startIndex, int count) {
            int endIndex = startIndex + count;
            if (value == null) {
                for (int i = startIndex; i < endIndex; i++) {
                    if (array[i] == null) return i;
                }
            }
            else {
                for (int i = startIndex; i < endIndex; i++) {
                    if (array[i] != null && array[i].Equals(value)) return i;
                }
            }
            return -1;
        }

        internal override int LastIndexOf(T[] array, T value, int startIndex, int count) {
            int endIndex = startIndex - count + 1;
            if (value == null) {
                for (int i = startIndex; i >= endIndex; i--) {
                    if (array[i] == null) return i;
                }
            }
            else {
                for (int i = startIndex; i >= endIndex; i--) {
                    if (array[i] != null && array[i].Equals(value)) return i;
                }
            }
            return -1;
        }

        // Equals method for the comparer itself.
        public override bool Equals(Object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();
    }

    // NonRandomizedStringEqualityComparer is the comparer used by default with the Dictionary<string,...> 
    // As the randomized string hashing is turned on by default on coreclr, we need to keep the performance not affected 
    // as much as possible in the main stream scenarios like Dictionary<string,>
    // We use NonRandomizedStringEqualityComparer as default comparer as it doesnt use the randomized string hashing which 
    // keep the perofrmance not affected till we hit collision threshold and then we switch to the comparer which is using 
    // randomized string hashing GenericEqualityComparer<string>
    [Serializable]
    internal class NonRandomizedStringEqualityComparer : GenericEqualityComparer<string> {
        static IEqualityComparer<string> s_nonRandomizedComparer;
        
        internal static new IEqualityComparer<string> Default {
            get  {
                    if (s_nonRandomizedComparer == null) {
                        s_nonRandomizedComparer = new NonRandomizedStringEqualityComparer();
                    }
                    return s_nonRandomizedComparer;
            }
        }

        [Pure]
        public override int GetHashCode(string obj)  {
            if (obj == null) return 0;
            return obj.GetLegacyNonRandomizedHashCode();
        }
    }

    // Performance of IndexOf on byte array is very important for some scenarios.
    // We will call the C runtime function memchr, which is optimized.
    [Serializable]
    internal sealed class ByteEqualityComparer: EqualityComparer<byte>
    {
        [Pure]
        public override bool Equals(byte x, byte y) {
            return x == y;
        }

        [Pure]
        public override int GetHashCode(byte b) {
            return b.GetHashCode();
        }

        internal unsafe override int IndexOf(byte[] array, byte value, int startIndex, int count) {
            if (array==null)
                throw new ArgumentNullException(nameof(array));
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), Environment.GetResourceString("ArgumentOutOfRange_Count"));
            if (count > array.Length - startIndex)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            if (count == 0) return -1;
            fixed (byte* pbytes = array) {
                return Buffer.IndexOfByte(pbytes, value, startIndex, count);
            }
        }

        internal override int LastIndexOf(byte[] array, byte value, int startIndex, int count) {
            int endIndex = startIndex - count + 1;
            for (int i = startIndex; i >= endIndex; i--) {
                if (array[i] == value) return i;
            }
            return -1;
        }

        // Equals method for the comparer itself.
        public override bool Equals(Object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();      
    }

    [Serializable]
    internal class EnumEqualityComparer<T> : EqualityComparer<T> where T : struct
    {
        [Pure]
        public override bool Equals(T x, T y) {
            int x_final = System.Runtime.CompilerServices.JitHelpers.UnsafeEnumCast(x);
            int y_final = System.Runtime.CompilerServices.JitHelpers.UnsafeEnumCast(y);
            return x_final == y_final;
        }

        [Pure]
        public override int GetHashCode(T obj) {
            int x_final = System.Runtime.CompilerServices.JitHelpers.UnsafeEnumCast(obj);
            return x_final.GetHashCode();
        }

        public EnumEqualityComparer() { }

        // Equals method for the comparer itself.
        public override bool Equals(Object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();

        internal override int IndexOf(T[] array, T value, int startIndex, int count)
        {
            int toFind = JitHelpers.UnsafeEnumCast(value);
            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
            {
                int current = JitHelpers.UnsafeEnumCast(array[i]);
                if (toFind == current) return i;
            }
            return -1;
        }

        internal override int LastIndexOf(T[] array, T value, int startIndex, int count)
        {
            int toFind = JitHelpers.UnsafeEnumCast(value);
            int endIndex = startIndex - count + 1;
            for (int i = startIndex; i >= endIndex; i--)
            {
                int current = JitHelpers.UnsafeEnumCast(array[i]);
                if (toFind == current) return i;
            }
            return -1;
        }
    }

    [Serializable]
    internal sealed class SByteEnumEqualityComparer<T> : EnumEqualityComparer<T> where T : struct
    {
        public SByteEnumEqualityComparer() { }

        [Pure]
        public override int GetHashCode(T obj) {
            int x_final = System.Runtime.CompilerServices.JitHelpers.UnsafeEnumCast(obj);
            return ((sbyte)x_final).GetHashCode();
        }
    }

    [Serializable]
    internal sealed class ShortEnumEqualityComparer<T> : EnumEqualityComparer<T> where T : struct
    {
        public ShortEnumEqualityComparer() { }

        [Pure]
        public override int GetHashCode(T obj) {
            int x_final = System.Runtime.CompilerServices.JitHelpers.UnsafeEnumCast(obj);
            return ((short)x_final).GetHashCode();
        }
    }

    [Serializable]
    internal sealed class LongEnumEqualityComparer<T> : EqualityComparer<T> where T : struct
    {
        [Pure]
        public override bool Equals(T x, T y) {
            long x_final = System.Runtime.CompilerServices.JitHelpers.UnsafeEnumCastLong(x);
            long y_final = System.Runtime.CompilerServices.JitHelpers.UnsafeEnumCastLong(y);
            return x_final == y_final;
        }

        [Pure]
        public override int GetHashCode(T obj) {
            long x_final = System.Runtime.CompilerServices.JitHelpers.UnsafeEnumCastLong(obj);
            return x_final.GetHashCode();
        }

        // Equals method for the comparer itself.
        public override bool Equals(Object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();

        public LongEnumEqualityComparer() { }

        internal override int IndexOf(T[] array, T value, int startIndex, int count)
        {
            long toFind = JitHelpers.UnsafeEnumCastLong(value);
            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
            {
                long current = JitHelpers.UnsafeEnumCastLong(array[i]);
                if (toFind == current) return i;
            }
            return -1;
        }

        internal override int LastIndexOf(T[] array, T value, int startIndex, int count)
        {
            long toFind = JitHelpers.UnsafeEnumCastLong(value);
            int endIndex = startIndex - count + 1;
            for (int i = startIndex; i >= endIndex; i--)
            {
                long current = JitHelpers.UnsafeEnumCastLong(array[i]);
                if (toFind == current) return i;
            }
            return -1;
        }
    }

#if FEATURE_RANDOMIZED_STRING_HASHING
    // This type is not serializeable by design.  It does not exist in previous versions and will be removed 
    // Once we move the framework to using secure hashing by default.
    internal sealed class RandomizedStringEqualityComparer : IEqualityComparer<String>, IEqualityComparer, IWellKnownStringEqualityComparer
    {
        private long _entropy;

        public RandomizedStringEqualityComparer() {
            _entropy = HashHelpers.GetEntropy();
        }

        public new bool Equals(object x, object y) {
            if (x == y) return true;
            if (x == null || y == null) return false;
            if ((x is string) && (y is string)) return Equals((string)x, (string)y);
            ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArgumentForComparison);
            return false;
        }

        [Pure]
        public bool Equals(string x, string y) {
            if (x != null) {
                if (y != null) return x.Equals(y);
                return false;
            }
            if (y != null) return false;
            return true;
        }

        [Pure]
        public int GetHashCode(String obj) {
            if(obj == null) return 0;
            return String.InternalMarvin32HashString(obj, obj.Length, _entropy);
        }

        [Pure]
        public int GetHashCode(Object obj) {
            if(obj == null) return 0;

            string sObj = obj as string;
            if(sObj != null) return  String.InternalMarvin32HashString(sObj, sObj.Length, _entropy);

            return obj.GetHashCode(); 
        }

        // Equals method for the comparer itself. 
        public override bool Equals(Object obj) {
            RandomizedStringEqualityComparer comparer = obj as RandomizedStringEqualityComparer; 
            return (comparer != null) && (this._entropy == comparer._entropy);
        }

        public override int GetHashCode() {
            return (this.GetType().GetHashCode() ^ ((int) (_entropy & 0x7FFFFFFF))); 
        }


        IEqualityComparer IWellKnownStringEqualityComparer.GetRandomizedEqualityComparer() {
            return new RandomizedStringEqualityComparer();
        }

        // We want to serialize the old comparer.
        IEqualityComparer IWellKnownStringEqualityComparer.GetEqualityComparerForSerialization() {
            return EqualityComparer<string>.Default;
        } 
    }

    // This type is not serializeable by design.  It does not exist in previous versions and will be removed 
    // Once we move the framework to using secure hashing by default.
    internal sealed class RandomizedObjectEqualityComparer : IEqualityComparer, IWellKnownStringEqualityComparer
    {
        private long _entropy;

        public RandomizedObjectEqualityComparer() {
            _entropy = HashHelpers.GetEntropy();
        }

        [Pure]
        public new bool Equals(Object x, Object y) {
            if (x != null) {
                if (y != null) return x.Equals(y);
                return false;
            }
            if (y != null) return false;
            return true;
        }

        [Pure]
        public int GetHashCode(Object obj) {
            if(obj == null) return 0;

            string sObj = obj as string;
            if(sObj != null) return  String.InternalMarvin32HashString(sObj, sObj.Length, _entropy);

            return obj.GetHashCode();           
        }

        // Equals method for the comparer itself. 
        public override bool Equals(Object obj){
            RandomizedObjectEqualityComparer comparer = obj as RandomizedObjectEqualityComparer; 
            return (comparer != null) && (this._entropy == comparer._entropy);
        }

        public override int GetHashCode() {
            return (this.GetType().GetHashCode() ^ ((int) (_entropy & 0x7FFFFFFF))); 
        }

        IEqualityComparer IWellKnownStringEqualityComparer.GetRandomizedEqualityComparer() {
            return new RandomizedObjectEqualityComparer();
        }

        // We want to serialize the old comparer, which in this case was null.
        IEqualityComparer IWellKnownStringEqualityComparer.GetEqualityComparerForSerialization() {
            return null;
        }   
    }
#endif
}

