// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Security;
using System.Runtime.Serialization;

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
        [System.Security.SecuritySafeCritical]  // auto-generated
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
        public override int GetHashCode(T obj) {
            if (obj == null) return 0;
            return obj.GetHashCode();
        }

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
        public override bool Equals(Object obj){
            GenericEqualityComparer<T> comparer = obj as GenericEqualityComparer<T>;
            return comparer != null;
        }

        public override int GetHashCode() {
            return this.GetType().Name.GetHashCode();
        }
    }

    [Serializable]
    internal class NullableEqualityComparer<T> : EqualityComparer<Nullable<T>> where T : struct, IEquatable<T>
    {
        [Pure]
        public override bool Equals(Nullable<T> x, Nullable<T> y) {
            if (x.HasValue) {
                if (y.HasValue) return x.value.Equals(y.value);
                return false;
            }
            if (y.HasValue) return false;
            return true;
        }

        [Pure]
        public override int GetHashCode(Nullable<T> obj) {
            return obj.GetHashCode();
        }

        internal override int IndexOf(Nullable<T>[] array, Nullable<T> value, int startIndex, int count) {
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

        internal override int LastIndexOf(Nullable<T>[] array, Nullable<T> value, int startIndex, int count) {
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
        public override bool Equals(Object obj){
            NullableEqualityComparer<T> comparer = obj as NullableEqualityComparer<T>;
            return comparer != null;
        }        

        public override int GetHashCode() {
            return this.GetType().Name.GetHashCode();
        }                                
    }

    [Serializable]
    internal class ObjectEqualityComparer<T>: EqualityComparer<T>
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
        public override int GetHashCode(T obj) {
            if (obj == null) return 0;
            return obj.GetHashCode();
        }

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
        public override bool Equals(Object obj){
            ObjectEqualityComparer<T> comparer = obj as ObjectEqualityComparer<T>;
            return comparer != null;
        }        

        public override int GetHashCode() {
            return this.GetType().Name.GetHashCode();
        }                                
    }

#if FEATURE_CORECLR
    // NonRandomizedStringEqualityComparer is the comparer used by default with the Dictionary<string,...> 
    // As the randomized string hashing is turned on by default on coreclr, we need to keep the performance not affected 
    // as much as possible in the main stream scenarios like Dictionary<string,>
    // We use NonRandomizedStringEqualityComparer as default comparer as it doesnt use the randomized string hashing which 
    // keep the perofrmance not affected till we hit collision threshold and then we switch to the comparer which is using 
    // randomized string hashing GenericEqualityComparer<string>

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
#endif // FEATURE_CORECLR

    // Performance of IndexOf on byte array is very important for some scenarios.
    // We will call the C runtime function memchr, which is optimized.
    [Serializable]
    internal class ByteEqualityComparer: EqualityComparer<byte>
    {
        [Pure]
        public override bool Equals(byte x, byte y) {
            return x == y;
        }

        [Pure]
        public override int GetHashCode(byte b) {
            return b.GetHashCode();
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe override int IndexOf(byte[] array, byte value, int startIndex, int count) {
            if (array==null)
                throw new ArgumentNullException("array");
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
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
        public override bool Equals(Object obj){
            ByteEqualityComparer comparer = obj as ByteEqualityComparer;
            return comparer != null;
        }        

        public override int GetHashCode() {
            return this.GetType().Name.GetHashCode();
        }                                
    }

    [Serializable]
    internal class EnumEqualityComparer<T> : EqualityComparer<T>, ISerializable where T : struct
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

        // This is used by the serialization engine.
        protected EnumEqualityComparer(SerializationInfo information, StreamingContext context) { }

        [SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            // For back-compat we need to serialize the comparers for enums with underlying types other than int as ObjectEqualityComparer 
            if (Type.GetTypeCode(Enum.GetUnderlyingType(typeof(T))) != TypeCode.Int32) {
                info.SetType(typeof(ObjectEqualityComparer<T>));
            }
        }

        // Equals method for the comparer itself. 
        public override bool Equals(Object obj){
            EnumEqualityComparer<T> comparer = obj as EnumEqualityComparer<T>;
            return comparer != null;
        }

        public override int GetHashCode() {
            return this.GetType().Name.GetHashCode();
        }
    }

    [Serializable]
    internal sealed class SByteEnumEqualityComparer<T> : EnumEqualityComparer<T>, ISerializable where T : struct
    {
        public SByteEnumEqualityComparer() { }

        // This is used by the serialization engine.
        public SByteEnumEqualityComparer(SerializationInfo information, StreamingContext context) { }

        [Pure]
        public override int GetHashCode(T obj) {
            int x_final = System.Runtime.CompilerServices.JitHelpers.UnsafeEnumCast(obj);
            return ((sbyte)x_final).GetHashCode();
        }
    }

    [Serializable]
    internal sealed class ShortEnumEqualityComparer<T> : EnumEqualityComparer<T>, ISerializable where T : struct
    {
        public ShortEnumEqualityComparer() { }

        // This is used by the serialization engine.
        public ShortEnumEqualityComparer(SerializationInfo information, StreamingContext context) { }

        [Pure]
        public override int GetHashCode(T obj) {
            int x_final = System.Runtime.CompilerServices.JitHelpers.UnsafeEnumCast(obj);
            return ((short)x_final).GetHashCode();
        }
    }

    [Serializable]
    internal sealed class LongEnumEqualityComparer<T> : EqualityComparer<T>, ISerializable where T : struct
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
        public override bool Equals(Object obj){
            LongEnumEqualityComparer<T> comparer = obj as LongEnumEqualityComparer<T>;
            return comparer != null;
        }

        public override int GetHashCode() {
            return this.GetType().Name.GetHashCode();
        }

        public LongEnumEqualityComparer() { }

        // This is used by the serialization engine.
        public LongEnumEqualityComparer(SerializationInfo information, StreamingContext context) { }

        [SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // The LongEnumEqualityComparer does not exist on 4.0 so we need to serialize this comparer as ObjectEqualityComparer
            // to allow for roundtrip between 4.0 and 4.5.
            info.SetType(typeof(ObjectEqualityComparer<T>));
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
        [SecuritySafeCritical]
        public int GetHashCode(String obj) {
            if(obj == null) return 0;
            return String.InternalMarvin32HashString(obj, obj.Length, _entropy);
        }

        [Pure]
        [SecuritySafeCritical]
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
            return (this.GetType().Name.GetHashCode() ^ ((int) (_entropy & 0x7FFFFFFF))); 
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
        [SecuritySafeCritical]
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
            return (this.GetType().Name.GetHashCode() ^ ((int) (_entropy & 0x7FFFFFFF))); 
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

