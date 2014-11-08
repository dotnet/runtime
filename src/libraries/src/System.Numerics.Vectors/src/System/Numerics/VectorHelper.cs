// -----------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------
namespace System.Numerics {
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Text;
    /// <summary>
    /// This class contains some helper methods for generic and fixed type vectors.
    /// The intent of this class is to allow for code sharing and in cases where it is not trivial to keep
    ///  similar implementations of the same method localized (for generic and fixed type vectors).
    /// </summary>
    internal static class VectorHelper {

        public static string FixedVectorToString(string format, IFormatProvider formatProvider, params Single[] components) {
            StringBuilder ret = new StringBuilder();
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator + " ";
            ret.Append("<");
            for (int i = 0; i < components.Length; i++) {
                ret.Append(components[i].ToString(format, formatProvider));
                if (i != components.Length - 1)
                    ret.Append(separator);
            }
            ret.Append(">");
            return ret.ToString();
        }

        public static string GenericVectorToString<T>(string format, IFormatProvider formatProvider, Vector<T> values) where T : struct {
            StringBuilder ret = new StringBuilder();
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator + " ";
            ret.Append("<");
            for (int i = 0; i < Vector<T>.Length; i++) {
                ret.Append(((IFormattable)values[i]).ToString(format, formatProvider));
                if (i != Vector<T>.Length - 1)
                    ret.Append(separator);
            }
            ret.Append(">");
            return ret.ToString();
        }

        public static int FixedVectorHashCode(params Single[] components) {
            int hash = 0;
            for (int i = 0; i < components.Length; i++) {
                hash = CombineHashCodes(hash, components[i].GetHashCode());
            }
            return hash;
        }

        public static int GenericVectorHashCode<T>(Vector<T> values) where T : struct {
            int hash = 0;
            for (int i = 0; i < Vector<T>.Length; i++) {
                hash = CombineHashCodes(hash, values[i].GetHashCode());
            }
            return hash;
        }

        private static int CombineHashCodes(int h1, int h2) {
            return (((h1 << 5) + h1) ^ h2);
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static Byte GetByteWithAllBitsSet()
        {
            Byte f = 0;
            unsafe
            {
                unchecked
                {
                    *((byte*)&f) = 0xff;
                }
            }
            return f;
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static SByte GetSByteWithAllBitsSet()
        {
            SByte f = 0;
            unsafe
            {
                unchecked
                {
                    *((byte*)&f) = 0xff;
                }
            }
            return f;
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static UInt16 GetUInt16WithAllBitsSet()
        {
            UInt16 f = 0;
            unsafe
            {
                unchecked
                {
                    *((UInt16*)&f) = (UInt16)0xffff;
                }
            }
            return f;
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static Int16 GetInt16WithAllBitsSet()
        {
            Int16 f = 0;
            unsafe
            {
                unchecked
                {
                    *((UInt16*)&f) = (UInt16)0xffff;
                }
            }
            return f;
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static Single GetSingleWithAllBitsSet() {
            Single f = 0.0f;
            unsafe {
                unchecked {
                    *((int*)&f) = (int)0xffffffff;
                }
            }
            return f;
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static Int32 GetInt32WithAllBitsSet() {
            Int32 f = 0;
            unsafe {
                unchecked {
                    *((int*)&f) = (int)0xffffffff;
                }
            }
            return f;
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static Int64 GetInt64WithAllBitsSet() {
            Int64 f = 0L;
            unsafe {
                unchecked {
                    *((Int64*)&f) = (Int64)(0xffffffffffffffff);
                }
            }
            return f;
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static Double GetDoubleWithAllBitsSet() {
            Double f = 0.0;
            unsafe {
                unchecked {
                    *((Int64*)&f) = (Int64)(0xffffffffffffffff);
                }
            }
            return f;
        }
    }
}
