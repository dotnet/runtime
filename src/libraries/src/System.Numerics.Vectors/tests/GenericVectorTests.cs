// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Numerics.Tests
{
    /// <summary>
    ///  Vector{T} tests that use random number generation and a unified generic test structure
    /// </summary>
    public class GenericVectorTests
    {
        // Static constructor in top-level class\
        static System.Numerics.Vector<float> dummy;
        static GenericVectorTests()
        {
            dummy = System.Numerics.Vector<float>.One;
        }

        #region Constructor Tests

        [Fact]
        public void ConstructorByte() { TestConstructor<Byte>(); }
        [Fact]
        public void ConstructorSByte() { TestConstructor<SByte>(); }
        [Fact]
        public void ConstructorUInt16() { TestConstructor<UInt16>(); }
        [Fact]
        public void ConstructorInt16() { TestConstructor<Int16>(); }
        [Fact]
        public void ConstructorUInt32() { TestConstructor<UInt32>(); }
        [Fact]
        public void ConstructorInt32() { TestConstructor<Int32>(); }
        [Fact]
        public void ConstructorUInt64() { TestConstructor<UInt64>(); }
        [Fact]
        public void ConstructorInt64() { TestConstructor<Int64>(); }
        [Fact]
        public void ConstructorSingle() { TestConstructor<Single>(); }
        [Fact]
        public void ConstructorDouble() { TestConstructor<Double>(); }

        private void TestConstructor<T>() where T : struct
        {
            Assert.Throws<NullReferenceException>(() => new Vector<T>((T[])null));

            T[] values = GenerateRandomValuesForVector<T>();
            var vector = new Vector<T>(values);
            ValidateVector(
                vector,
                (index, val) =>
                {
                    Assert.Equal(values[index], val);
                });
        }

        [Fact]
        public void ConstructorWithOffsetByte() { TestConstructorWithOffset<Byte>(); }
        [Fact]
        public void ConstructorWithOffsetSByte() { TestConstructorWithOffset<SByte>(); }
        [Fact]
        public void ConstructorWithOffsetUInt16() { TestConstructorWithOffset<UInt16>(); }
        [Fact]
        public void ConstructorWithOffsetInt16() { TestConstructorWithOffset<Int16>(); }
        [Fact]
        public void ConstructorWithOffsetUInt32() { TestConstructorWithOffset<UInt32>(); }
        [Fact]
        public void ConstructorWithOffsetInt32() { TestConstructorWithOffset<Int32>(); }
        [Fact]
        public void ConstructorWithOffsetUInt64() { TestConstructorWithOffset<UInt64>(); }
        [Fact]
        public void ConstructorWithOffsetInt64() { TestConstructorWithOffset<Int64>(); }
        [Fact]
        public void ConstructorWithOffsetSingle() { TestConstructorWithOffset<Single>(); }
        [Fact]
        public void ConstructorWithOffsetDouble() { TestConstructorWithOffset<Double>(); }
        private void TestConstructorWithOffset<T>() where T : struct
        {
            Assert.Throws<NullReferenceException>(() => new Vector<T>((T[])null, 0));

            int offsetAmount = Util.GenerateSingleValue<int>(2, 250);
            T[] values = new T[offsetAmount].Concat(GenerateRandomValuesForVector<T>()).ToArray();
            var vector = new Vector<T>(values, offsetAmount);
            ValidateVector(vector,
                (index, val) =>
                {
                    Assert.Equal(values[index + offsetAmount], val);
                });
        }

        [Fact]
        public void ConstructorConstantValueByte() { TestConstructorConstantValue<Byte>(); }
        [Fact]
        public void ConstructorConstantValueSByte() { TestConstructorConstantValue<SByte>(); }
        [Fact]
        public void ConstructorConstantValueUInt16() { TestConstructorConstantValue<UInt16>(); }
        [Fact]
        public void ConstructorConstantValueInt16() { TestConstructorConstantValue<Int16>(); }
        [Fact]
        public void ConstructorConstantValueUInt32() { TestConstructorConstantValue<UInt32>(); }
        [Fact]
        public void ConstructorConstantValueInt32() { TestConstructorConstantValue<Int32>(); }
        [Fact]
        public void ConstructorConstantValueUInt64() { TestConstructorConstantValue<UInt64>(); }
        [Fact]
        public void ConstructorConstantValueInt64() { TestConstructorConstantValue<Int64>(); }
        [Fact]
        public void ConstructorConstantValueSingle() { TestConstructorConstantValue<Single>(); }
        [Fact]
        public void ConstructorConstantValueDouble() { TestConstructorConstantValue<Double>(); }
        private void TestConstructorConstantValue<T>() where T : struct
        {
            T constantValue = Util.GenerateSingleValue<T>(GetMinValue<T>(), GetMaxValue<T>());
            var vector = new Vector<T>(constantValue);
            ValidateVector(vector,
                (index, val) =>
                {
                    Assert.Equal(val, constantValue);
                });
        }

        [Fact]
        public void ConstructorDefaultByte() { TestConstructorDefault<Byte>(); }
        [Fact]
        public void ConstructorDefaultSByte() { TestConstructorDefault<SByte>(); }
        [Fact]
        public void ConstructorDefaultUInt16() { TestConstructorDefault<UInt16>(); }
        [Fact]
        public void ConstructorDefaultInt16() { TestConstructorDefault<Int16>(); }
        [Fact]
        public void ConstructorDefaultUInt32() { TestConstructorDefault<UInt32>(); }
        [Fact]
        public void ConstructorDefaultInt32() { TestConstructorDefault<Int32>(); }
        [Fact]
        public void ConstructorDefaultUInt64() { TestConstructorDefault<UInt64>(); }
        [Fact]
        public void ConstructorDefaultInt64() { TestConstructorDefault<Int64>(); }
        [Fact]
        public void ConstructorDefaultSingle() { TestConstructorDefault<Single>(); }
        [Fact]
        public void ConstructorDefaultDouble() { TestConstructorDefault<Double>(); }
        private void TestConstructorDefault<T>() where T : struct
        {
            var vector = new Vector<T>();
            ValidateVector(vector,
                (index, val) =>
                {
                    Assert.Equal(val, (T)(dynamic)0);
                });
        }

        [Fact]
        public void ConstructorExceptionByte() { TestConstructorArrayTooSmallException<Byte>(); }
        [Fact]
        public void ConstructorExceptionSByte() { TestConstructorArrayTooSmallException<SByte>(); }
        [Fact]
        public void ConstructorExceptionUInt16() { TestConstructorArrayTooSmallException<UInt16>(); }
        [Fact]
        public void ConstructorExceptionInt16() { TestConstructorArrayTooSmallException<Int16>(); }
        [Fact]
        public void ConstructorExceptionUInt32() { TestConstructorArrayTooSmallException<UInt32>(); }
        [Fact]
        public void ConstructorExceptionInt32() { TestConstructorArrayTooSmallException<Int32>(); }
        [Fact]
        public void ConstructorExceptionUInt64() { TestConstructorArrayTooSmallException<UInt64>(); }
        [Fact]
        public void ConstructorExceptionInt64() { TestConstructorArrayTooSmallException<Int64>(); }
        [Fact]
        public void ConstructorExceptionSingle() { TestConstructorArrayTooSmallException<Single>(); }
        [Fact]
        public void ConstructorExceptionDouble() { TestConstructorArrayTooSmallException<Double>(); }
        private void TestConstructorArrayTooSmallException<T>() where T : struct
        {
            T[] values = GenerateRandomValuesForVector<T>().Skip(1).ToArray();
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var vector = new Vector<T>(values);
            });
        }
        #endregion Constructor Tests

        #region Indexer Tests

        [Fact]
        public void IndexerOutOfRangeByte() { TestIndexerOutOfRange<Byte>(); }
        [Fact]
        public void IndexerOutOfRangeSByte() { TestIndexerOutOfRange<SByte>(); }
        [Fact]
        public void IndexerOutOfRangeUInt16() { TestIndexerOutOfRange<UInt16>(); }
        [Fact]
        public void IndexerOutOfRangeInt16() { TestIndexerOutOfRange<Int16>(); }
        [Fact]
        public void IndexerOutOfRangeUInt32() { TestIndexerOutOfRange<UInt32>(); }
        [Fact]
        public void IndexerOutOfRangeInt32() { TestIndexerOutOfRange<Int32>(); }
        [Fact]
        public void IndexerOutOfRangeUInt64() { TestIndexerOutOfRange<UInt64>(); }
        [Fact]
        public void IndexerOutOfRangeInt64() { TestIndexerOutOfRange<Int64>(); }
        [Fact]
        public void IndexerOutOfRangeSingle() { TestIndexerOutOfRange<Single>(); }
        [Fact]
        public void IndexerOutOfRangeDouble() { TestIndexerOutOfRange<Double>(); }
        private void TestIndexerOutOfRange<T>() where T : struct
        {
            Vector<T> vector = Vector<T>.One;
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                T value = vector[Vector<T>.Count];
            });
        }
        #endregion

        #region Static Member Tests
        [Fact]
        public void StaticOneVectorByte() { TestStaticOneVector<Byte>(); }
        [Fact]
        public void StaticOneVectorSByte() { TestStaticOneVector<SByte>(); }
        [Fact]
        public void StaticOneVectorUInt16() { TestStaticOneVector<UInt16>(); }
        [Fact]
        public void StaticOneVectorInt16() { TestStaticOneVector<Int16>(); }
        [Fact]
        public void StaticOneVectorUInt32() { TestStaticOneVector<UInt32>(); }
        [Fact]
        public void StaticOneVectorInt32() { TestStaticOneVector<Int32>(); }
        [Fact]
        public void StaticOneVectorUInt64() { TestStaticOneVector<UInt64>(); }
        [Fact]
        public void StaticOneVectorInt64() { TestStaticOneVector<Int64>(); }
        [Fact]
        public void StaticOneVectorSingle() { TestStaticOneVector<Single>(); }
        [Fact]
        public void StaticOneVectorDouble() { TestStaticOneVector<Double>(); }
        private void TestStaticOneVector<T>() where T : struct
        {
            Vector<T> vector = Vector<T>.One;
            T oneValue = Util.One<T>();
            ValidateVector(vector,
                (index, val) =>
                {
                    Assert.Equal(oneValue, val);
                });
        }

        [Fact]
        public void StaticZeroVectorByte() { TestStaticZeroVector<Byte>(); }
        [Fact]
        public void StaticZeroVectorSByte() { TestStaticZeroVector<SByte>(); }
        [Fact]
        public void StaticZeroVectorUInt16() { TestStaticZeroVector<UInt16>(); }
        [Fact]
        public void StaticZeroVectorInt16() { TestStaticZeroVector<Int16>(); }
        [Fact]
        public void StaticZeroVectorUInt32() { TestStaticZeroVector<UInt32>(); }
        [Fact]
        public void StaticZeroVectorInt32() { TestStaticZeroVector<Int32>(); }
        [Fact]
        public void StaticZeroVectorUInt64() { TestStaticZeroVector<UInt64>(); }
        [Fact]
        public void StaticZeroVectorInt64() { TestStaticZeroVector<Int64>(); }
        [Fact]
        public void StaticZeroVectorSingle() { TestStaticZeroVector<Single>(); }
        [Fact]
        public void StaticZeroVectorDouble() { TestStaticZeroVector<Double>(); }
        private void TestStaticZeroVector<T>() where T : struct
        {
            Vector<T> vector = Vector<T>.Zero;
            T zeroValue = Util.Zero<T>();
            ValidateVector(vector,
                (index, val) =>
                {
                    Assert.Equal(zeroValue, val);
                });
        }
        #endregion

        #region CopyTo (array) Tests
        [Fact]
        public void CopyToByte() { TestCopyTo<Byte>(); }
        [Fact]
        public void CopyToSByte() { TestCopyTo<SByte>(); }
        [Fact]
        public void CopyToUInt16() { TestCopyTo<UInt16>(); }
        [Fact]
        public void CopyToInt16() { TestCopyTo<Int16>(); }
        [Fact]
        public void CopyToUInt32() { TestCopyTo<UInt32>(); }
        [Fact]
        public void CopyToInt32() { TestCopyTo<Int32>(); }
        [Fact]
        public void CopyToUInt64() { TestCopyTo<UInt64>(); }
        [Fact]
        public void CopyToInt64() { TestCopyTo<Int64>(); }
        [Fact]
        public void CopyToSingle() { TestCopyTo<Single>(); }
        [Fact]
        public void CopyToDouble() { TestCopyTo<Double>(); }
        private void TestCopyTo<T>() where T : struct
        {
            var initialValues = GenerateRandomValuesForVector<T>();
            var vector = new Vector<T>(initialValues);
            T[] array = new T[Vector<T>.Count];

            Assert.Throws<NullReferenceException>(() => vector.CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => vector.CopyTo(array, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => vector.CopyTo(array, array.Length));
            Assert.Throws<ArgumentException>(() => vector.CopyTo(array, array.Length - 1));

            vector.CopyTo(array);
            for (int g = 0; g < array.Length; g++)
            {
                Assert.Equal(initialValues[g], array[g]);
                Assert.Equal(vector[g], array[g]);
            }
        }

        [Fact]
        public void CopyToWithOffsetByte() { TestCopyToWithOffset<Byte>(); }
        [Fact]
        public void CopyToWithOffsetSByte() { TestCopyToWithOffset<SByte>(); }
        [Fact]
        public void CopyToWithOffsetUInt16() { TestCopyToWithOffset<UInt16>(); }
        [Fact]
        public void CopyToWithOffsetInt16() { TestCopyToWithOffset<Int16>(); }
        [Fact]
        public void CopyToWithOffsetUInt32() { TestCopyToWithOffset<UInt32>(); }
        [Fact]
        public void CopyToWithOffsetInt32() { TestCopyToWithOffset<Int32>(); }
        [Fact]
        public void CopyToWithOffsetUInt64() { TestCopyToWithOffset<UInt64>(); }
        [Fact]
        public void CopyToWithOffsetInt64() { TestCopyToWithOffset<Int64>(); }
        [Fact]
        public void CopyToWithOffsetSingle() { TestCopyToWithOffset<Single>(); }
        [Fact]
        public void CopyToWithOffsetDouble() { TestCopyToWithOffset<Double>(); }
        private void TestCopyToWithOffset<T>() where T : struct
        {
            int offset = Util.GenerateSingleValue<int>(5, 500);
            var initialValues = GenerateRandomValuesForVector<T>();
            var vector = new Vector<T>(initialValues);
            T[] array = new T[Vector<T>.Count + offset];
            vector.CopyTo(array, offset);
            for (int g = 0; g < initialValues.Length; g++)
            {
                Assert.Equal(initialValues[g], array[g + offset]);
                Assert.Equal(vector[g], array[g + offset]);
            }
        }
        #endregion

        #region EqualsTests
        [Fact]
        public void EqualsObjectByte() { TestEqualsObject<Byte>(); }
        [Fact]
        public void EqualsObjectSByte() { TestEqualsObject<SByte>(); }
        [Fact]
        public void EqualsObjectUInt16() { TestEqualsObject<UInt16>(); }
        [Fact]
        public void EqualsObjectInt16() { TestEqualsObject<Int16>(); }
        [Fact]
        public void EqualsObjectUInt32() { TestEqualsObject<UInt32>(); }
        [Fact]
        public void EqualsObjectInt32() { TestEqualsObject<Int32>(); }
        [Fact]
        public void EqualsObjectUInt64() { TestEqualsObject<UInt64>(); }
        [Fact]
        public void EqualsObjectInt64() { TestEqualsObject<Int64>(); }
        [Fact]
        public void EqualsObjectSingle() { TestEqualsObject<Single>(); }
        [Fact]
        public void EqualsObjectDouble() { TestEqualsObject<Double>(); }
        private void TestEqualsObject<T>() where T : struct
        {
            T[] values = GenerateRandomValuesForVector<T>();
            Vector<T> vector1 = new Vector<T>(values);

            const string stringObject = "This is not a Vector<T> object.";
            DateTime dateTimeObject = DateTime.UtcNow;

            Assert.False(vector1.Equals(stringObject));
            Assert.False(vector1.Equals(dateTimeObject));
            Assert.True(vector1.Equals((object)vector1));

            if (typeof(T) != typeof(Int32))
            {
                Vector<Int32> intVector = new Vector<Int32>(GenerateRandomValuesForVector<Int32>());
                Assert.False(vector1.Equals(intVector));
                Assert.False(intVector.Equals(vector1));
            }
            else
            {
                Vector<Single> floatVector = new Vector<Single>(GenerateRandomValuesForVector<Single>());
                Assert.False(vector1.Equals(floatVector));
                Assert.False(floatVector.Equals(vector1));
            }
        }

        [Fact]
        public void EqualsVectorByte() { TestEqualsVector<Byte>(); }
        [Fact]
        public void EqualsVectorSByte() { TestEqualsVector<SByte>(); }
        [Fact]
        public void EqualsVectorUInt16() { TestEqualsVector<UInt16>(); }
        [Fact]
        public void EqualsVectorInt16() { TestEqualsVector<Int16>(); }
        [Fact]
        public void EqualsVectorUInt32() { TestEqualsVector<UInt32>(); }
        [Fact]
        public void EqualsVectorInt32() { TestEqualsVector<Int32>(); }
        [Fact]
        public void EqualsVectorUInt64() { TestEqualsVector<UInt64>(); }
        [Fact]
        public void EqualsVectorInt64() { TestEqualsVector<Int64>(); }
        [Fact]
        public void EqualsVectorSingle() { TestEqualsVector<Single>(); }
        [Fact]
        public void EqualsVectorDouble() { TestEqualsVector<Double>(); }
        private void TestEqualsVector<T>() where T : struct
        {
            T[] values = GenerateRandomValuesForVector<T>();
            Vector<T> vector1 = new Vector<T>(values);
            Vector<T> vector2 = new Vector<T>(values);

            Assert.True(vector1.Equals(vector2));
            Assert.True(vector2.Equals(vector1));

            Assert.True(Vector<T>.Zero.Equals(Vector<T>.Zero));
            Assert.True(Vector<T>.One.Equals(Vector<T>.One));

            Assert.True(Vector<T>.Zero.Equals(new Vector<T>(Util.Zero<T>())));
            Assert.True(Vector<T>.One.Equals(new Vector<T>(Util.One<T>())));

            Assert.False(Vector<T>.Zero.Equals(Vector<T>.One));
            Assert.False(Vector<T>.Zero.Equals(new Vector<T>(Util.One<T>())));
        }
        #endregion

        #region System.Object Overloads
        [Fact]
        public void GetHashCodeByte() { TestGetHashCode<Byte>(); }
        [Fact]
        public void GetHashCodeSByte() { TestGetHashCode<SByte>(); }
        [Fact]
        public void GetHashCodeUInt16() { TestGetHashCode<UInt16>(); }
        [Fact]
        public void GetHashCodeInt16() { TestGetHashCode<Int16>(); }
        [Fact]
        public void GetHashCodeUInt32() { TestGetHashCode<UInt32>(); }
        [Fact]
        public void GetHashCodeInt32() { TestGetHashCode<Int32>(); }
        [Fact]
        public void GetHashCodeUInt64() { TestGetHashCode<UInt64>(); }
        [Fact]
        public void GetHashCodeInt64() { TestGetHashCode<Int64>(); }
        [Fact]
        public void GetHashCodeSingle() { TestGetHashCode<Single>(); }
        [Fact]
        public void GetHashCodeDouble() { TestGetHashCode<Double>(); }
        private void TestGetHashCode<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            Vector<T> v1 = new Vector<T>(values1);
            int hash = v1.GetHashCode();

            int expected = 0;
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                unchecked
                {
                    uint shift5 = ((uint)expected << 5) | ((uint)expected >> 27);
                    expected = ((int)shift5 + expected) ^ v1[g].GetHashCode();
                }
            }

            Assert.Equal(expected, hash);
        }

        [Fact]
        public void ToStringGeneralByte() { TestToString<Byte>("G", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringGeneralSByte() { TestToString<SByte>("G", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringGeneralUInt16() { TestToString<UInt16>("G", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringGeneralInt16() { TestToString<Int16>("G", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringGeneralUInt32() { TestToString<UInt32>("G", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringGeneralInt32() { TestToString<Int32>("G", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringGeneralUInt64() { TestToString<UInt64>("G", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringGeneralInt64() { TestToString<Int64>("G", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringGeneralSingle() { TestToString<Single>("G", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringGeneralDouble() { TestToString<Double>("G", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringCurrencyByte() { TestToString<Byte>("c", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringCurrencySByte() { TestToString<SByte>("c", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringCurrencyUInt16() { TestToString<UInt16>("c", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringCurrencyInt16() { TestToString<Int16>("c", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringCurrencyUInt32() { TestToString<UInt32>("c", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringCurrencyInt32() { TestToString<Int32>("c", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringCurrencyUInt64() { TestToString<UInt64>("c", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringCurrencyInt64() { TestToString<Int64>("c", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringCurrencySingle() { TestToString<Single>("c", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringCurrencyDouble() { TestToString<Double>("c", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringExponentialByte() { TestToString<Byte>("E3", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringExponentialSByte() { TestToString<SByte>("E3", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringExponentialUInt16() { TestToString<UInt16>("E3", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringExponentialInt16() { TestToString<Int16>("E3", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringExponentialUInt32() { TestToString<UInt32>("E3", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringExponentialInt32() { TestToString<Int32>("E3", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringExponentialUInt64() { TestToString<UInt64>("E3", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringExponentialInt64() { TestToString<Int64>("E3", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringExponentialSingle() { TestToString<Single>("E3", CultureInfo.CurrentCulture); }
        [Fact]
        public void ToStringExponentialDouble() { TestToString<Double>("E3", CultureInfo.CurrentCulture); }

        private void TestToString<T>(string format, IFormatProvider provider) where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            Vector<T> v1 = new Vector<T>(values1);
            string result = v1.ToString(format, provider);
            string cultureSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator + " ";

            string expected = "<";
            for (int g = 0; g < Vector<T>.Count - 1; g++)
            {
                expected += ((IFormattable)v1[g]).ToString(format, provider);
                expected += cultureSeparator;
            }
            expected += ((IFormattable)v1[Vector<T>.Count - 1]).ToString(format, provider);
            expected += ">";
            Assert.Equal(expected, result);
        }
        #endregion System.Object Overloads

        #region Arithmetic Operator Tests
        [Fact]
        public void AdditionByte() { TestAddition<Byte>(); }
        [Fact]
        public void AdditionSByte() { TestAddition<SByte>(); }
        [Fact]
        public void AdditionUInt16() { TestAddition<UInt16>(); }
        [Fact]
        public void AdditionInt16() { TestAddition<Int16>(); }
        [Fact]
        public void AdditionUInt32() { TestAddition<UInt32>(); }
        [Fact]
        public void AdditionInt32() { TestAddition<Int32>(); }
        [Fact]
        public void AdditionUInt64() { TestAddition<UInt64>(); }
        [Fact]
        public void AdditionInt64() { TestAddition<Int64>(); }
        [Fact]
        public void AdditionSingle() { TestAddition<Single>(); }
        [Fact]
        public void AdditionDouble() { TestAddition<Double>(); }
        private void TestAddition<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            var v1 = new Vector<T>(values1);
            var v2 = new Vector<T>(values2);
            var sum = v1 + v2;
            ValidateVector(sum,
                (index, val) =>
                {
                    Assert.Equal(Util.Add(values1[index], values2[index]), val);
                });
        }

        [Fact]
        public void AdditionOverflowByte() { TestAdditionOverflow<Byte>(); }
        [Fact]
        public void AdditionOverflowSByte() { TestAdditionOverflow<SByte>(); }
        [Fact]
        public void AdditionOverflowUInt16() { TestAdditionOverflow<UInt16>(); }
        [Fact]
        public void AdditionOverflowInt16() { TestAdditionOverflow<Int16>(); }
        [Fact]
        public void AdditionOverflowUInt32() { TestAdditionOverflow<UInt32>(); }
        [Fact]
        public void AdditionOverflowInt32() { TestAdditionOverflow<Int32>(); }
        [Fact]
        public void AdditionOverflowUInt64() { TestAdditionOverflow<UInt64>(); }
        [Fact]
        public void AdditionOverflowInt64() { TestAdditionOverflow<Int64>(); }
        private void TestAdditionOverflow<T>() where T : struct
        {
            T maxValue = (T)(dynamic)typeof(T).GetRuntimeField("MaxValue").GetValue(null);
            Vector<T> maxValueVector = new Vector<T>(maxValue);
            Vector<T> secondVector = new Vector<T>(GenerateRandomValuesForVector<T>());
            Vector<T> sum = maxValueVector + secondVector;

            T minValue = (T)(dynamic)typeof(T).GetRuntimeField("MinValue").GetValue(null);
            ValidateVector(sum,
                (index, val) =>
                {
                    Assert.Equal(Util.Subtract(Util.Add(secondVector[index], minValue), (T)(dynamic)1), sum[index]);
                });
        }

        [Fact]
        public void SubtractionByte() { TestSubtraction<Byte>(); }
        [Fact]
        public void SubtractionSByte() { TestSubtraction<SByte>(); }
        [Fact]
        public void SubtractionUInt16() { TestSubtraction<UInt16>(); }
        [Fact]
        public void SubtractionInt16() { TestSubtraction<Int16>(); }
        [Fact]
        public void SubtractionUInt32() { TestSubtraction<UInt32>(); }
        [Fact]
        public void SubtractionInt32() { TestSubtraction<Int32>(); }
        [Fact]
        public void SubtractionUInt64() { TestSubtraction<UInt64>(); }
        [Fact]
        public void SubtractionInt64() { TestSubtraction<Int64>(); }
        [Fact]
        public void SubtractionSingle() { TestSubtraction<Single>(); }
        [Fact]
        public void SubtractionDouble() { TestSubtraction<Double>(); }
        private void TestSubtraction<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            var v1 = new Vector<T>(values1);
            var v2 = new Vector<T>(values2);
            var sum = v1 - v2;
            ValidateVector(sum,
                (index, val) =>
                {
                    Assert.Equal(Util.Subtract(values1[index], values2[index]), val);
                });
        }

        [Fact]
        public void SubtractionOverflowByte() { TestSubtractionOverflow<Byte>(); }
        [Fact]
        public void SubtractionOverflowSByte() { TestSubtractionOverflow<SByte>(); }
        [Fact]
        public void SubtractionOverflowUInt16() { TestSubtractionOverflow<UInt16>(); }
        [Fact]
        public void SubtractionOverflowInt16() { TestSubtractionOverflow<Int16>(); }
        [Fact]
        public void SubtractionOverflowUInt32() { TestSubtractionOverflow<UInt32>(); }
        [Fact]
        public void SubtractionOverflowInt32() { TestSubtractionOverflow<Int32>(); }
        [Fact]
        public void SubtractionOverflowUInt64() { TestSubtractionOverflow<UInt64>(); }
        [Fact]
        public void SubtractionOverflowInt64() { TestSubtractionOverflow<Int64>(); }
        private void TestSubtractionOverflow<T>() where T : struct
        {
            T minValue = (T)(dynamic)typeof(T).GetRuntimeField("MinValue").GetValue(null);
            Vector<T> minValueVector = new Vector<T>(minValue);
            Vector<T> secondVector = new Vector<T>(GenerateRandomValuesForVector<T>());
            Vector<T> difference = minValueVector - secondVector;

            T maxValue = (T)(dynamic)typeof(T).GetRuntimeField("MaxValue").GetValue(null);
            ValidateVector(difference,
                (index, val) =>
                {
                    Assert.Equal(Util.Add(Util.Subtract(maxValue, secondVector[index]), (T)(dynamic)1), val);
                });
        }

        [Fact]
        public void MultiplicationByte() { TestMultiplication<Byte>(); }
        [Fact]
        public void MultiplicationSByte() { TestMultiplication<SByte>(); }
        [Fact]
        public void MultiplicationUInt16() { TestMultiplication<UInt16>(); }
        [Fact]
        public void MultiplicationInt16() { TestMultiplication<Int16>(); }
        [Fact]
        public void MultiplicationUInt32() { TestMultiplication<UInt32>(); }
        [Fact]
        public void MultiplicationInt32() { TestMultiplication<Int32>(); }
        [Fact]
        public void MultiplicationUInt64() { TestMultiplication<UInt64>(); }
        [Fact]
        public void MultiplicationInt64() { TestMultiplication<Int64>(); }
        [Fact]
        public void MultiplicationSingle() { TestMultiplication<Single>(); }
        [Fact]
        public void MultiplicationDouble() { TestMultiplication<Double>(); }
        private void TestMultiplication<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            var v1 = new Vector<T>(values1);
            var v2 = new Vector<T>(values2);
            var sum = v1 * v2;
            ValidateVector(sum,
                (index, val) =>
                {
                    Assert.Equal(Util.Multiply(values1[index], values2[index]), val);
                });
        }

        [Fact]
        public void MultiplicationWithScalarByte() { TestMultiplicationWithScalar<Byte>(); }
        [Fact]
        public void MultiplicationWithScalarSByte() { TestMultiplicationWithScalar<SByte>(); }
        [Fact]
        public void MultiplicationWithScalarUInt16() { TestMultiplicationWithScalar<UInt16>(); }
        [Fact]
        public void MultiplicationWithScalarInt16() { TestMultiplicationWithScalar<Int16>(); }
        [Fact]
        public void MultiplicationWithScalarUInt32() { TestMultiplicationWithScalar<UInt32>(); }
        [Fact]
        public void MultiplicationWithScalarInt32() { TestMultiplicationWithScalar<Int32>(); }
        [Fact]
        public void MultiplicationWithScalarUInt64() { TestMultiplicationWithScalar<UInt64>(); }
        [Fact]
        public void MultiplicationWithScalarInt64() { TestMultiplicationWithScalar<Int64>(); }
        [Fact]
        public void MultiplicationWithScalarSingle() { TestMultiplicationWithScalar<Single>(); }
        [Fact]
        public void MultiplicationWithScalarDouble() { TestMultiplicationWithScalar<Double>(); }
        private void TestMultiplicationWithScalar<T>() where T : struct
        {
            T[] values = GenerateRandomValuesForVector<T>();
            T factor = Util.GenerateSingleValue<T>(GetMinValue<T>(), GetMaxValue<T>());
            var vector = new Vector<T>(values);
            var product1 = vector * factor;
            ValidateVector(product1,
                (index, val) =>
                {
                    T expected = Util.Multiply(values[index], factor);
                    Assert.Equal(expected, val);
                });

            var product2 = factor * vector;
            ValidateVector(product2,
                (index, val) =>
                {
                    T expected = Util.Multiply(values[index], factor);
                    Assert.Equal(expected, val);
                });
        }

        [Fact]
        public void DivisionByte() { TestDivision<Byte>(); }
        [Fact]
        public void DivisionSByte() { TestDivision<SByte>(); }
        [Fact]
        public void DivisionUInt16() { TestDivision<UInt16>(); }
        [Fact]
        public void DivisionInt16() { TestDivision<Int16>(); }
        [Fact]
        public void DivisionUInt32() { TestDivision<UInt32>(); }
        [Fact]
        public void DivisionInt32() { TestDivision<Int32>(); }
        [Fact]
        public void DivisionUInt64() { TestDivision<UInt64>(); }
        [Fact]
        public void DivisionInt64() { TestDivision<Int64>(); }
        [Fact]
        public void DivisionSingle() { TestDivision<Single>(); }
        [Fact]
        public void DivisionDouble() { TestDivision<Double>(); }
        private void TestDivision<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            values1 = values1.Select(val => val.Equals(Util.Zero<T>()) ? Util.One<T>() : val).ToArray(); // Avoid divide-by-zero
            T[] values2 = GenerateRandomValuesForVector<T>();
            values2 = values2.Select(val => val.Equals(Util.Zero<T>()) ? Util.One<T>() : val).ToArray(); // Avoid divide-by-zero
            // I replace all Zero's with One's above to avoid Divide-by-zero.

            var v1 = new Vector<T>(values1);
            var v2 = new Vector<T>(values2);
            var sum = v1 / v2;
            ValidateVector(sum,
                (index, val) =>
                {
                    Assert.Equal(Util.Divide(values1[index], values2[index]), val);
                });
        }

        [Fact]
        public void DivisionByZeroExceptionByte() { TestDivisionByZeroException<Byte>(); }
        [Fact]
        public void DivisionByZeroExceptionSByte() { TestDivisionByZeroException<SByte>(); }
        [Fact]
        public void DivisionByZeroExceptionUInt16() { TestDivisionByZeroException<UInt16>(); }
        [Fact]
        public void DivisionByZeroExceptionInt16() { TestDivisionByZeroException<Int16>(); }
        [Fact]
        public void DivisionByZeroExceptionInt32() { TestDivisionByZeroException<Int32>(); }
        [Fact]
        public void DivisionByZeroExceptionInt64() { TestDivisionByZeroException<Int64>(); }
        private void TestDivisionByZeroException<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            Vector<T> vector = new Vector<T>(values1);
            Assert.Throws<DivideByZeroException>(() =>
            {
                var result = vector / Vector<T>.Zero;
            });
        }

        [Fact]
        public void UnaryMinusByte() { TestUnaryMinus<Byte>(); }
        [Fact]
        public void UnaryMinusSByte() { TestUnaryMinus<SByte>(); }
        [Fact]
        public void UnaryMinusUInt16() { TestUnaryMinus<UInt16>(); }
        [Fact]
        public void UnaryMinusInt16() { TestUnaryMinus<Int16>(); }
        [Fact]
        public void UnaryMinusUInt32() { TestUnaryMinus<UInt32>(); }
        [Fact]
        public void UnaryMinusInt32() { TestUnaryMinus<Int32>(); }
        [Fact]
        public void UnaryMinusUInt64() { TestUnaryMinus<UInt64>(); }
        [Fact]
        public void UnaryMinusInt64() { TestUnaryMinus<Int64>(); }
        [Fact]
        public void UnaryMinusSingle() { TestUnaryMinus<Single>(); }
        [Fact]
        public void UnaryMinusDouble() { TestUnaryMinus<Double>(); }
        private void TestUnaryMinus<T>() where T : struct
        {
            T[] values = GenerateRandomValuesForVector<T>();
            Vector<T> vector = new Vector<T>(values);
            var negated = -vector;
            ValidateVector(negated,
                (index, value) =>
                {
                    T expected = Util.Subtract(Util.Zero<T>(), values[index]);
                    Assert.Equal(expected, value);
                });
        }
        #endregion

        #region Bitwise Operator Tests
        [Fact]
        public void BitwiseAndOperatorByte() { TestBitwiseAndOperator<Byte>(); }
        [Fact]
        public void BitwiseAndOperatorSByte() { TestBitwiseAndOperator<SByte>(); }
        [Fact]
        public void BitwiseAndOperatorUInt16() { TestBitwiseAndOperator<UInt16>(); }
        [Fact]
        public void BitwiseAndOperatorInt16() { TestBitwiseAndOperator<Int16>(); }
        [Fact]
        public void BitwiseAndOperatorUInt32() { TestBitwiseAndOperator<UInt32>(); }
        [Fact]
        public void BitwiseAndOperatorInt32() { TestBitwiseAndOperator<Int32>(); }
        [Fact]
        public void BitwiseAndOperatorUInt64() { TestBitwiseAndOperator<UInt64>(); }
        [Fact]
        public void BitwiseAndOperatorInt64() { TestBitwiseAndOperator<Int64>(); }
        [Fact]
        public void BitwiseAndOperatorSingle() { TestBitwiseAndOperator<Single>(); }
        [Fact]
        public void BitwiseAndOperatorDouble() { TestBitwiseAndOperator<Double>(); }
        private void TestBitwiseAndOperator<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            Vector<T> randomVector = new Vector<T>(values1);
            Vector<T> zeroVector = Vector<T>.Zero;

            Vector<T> selfAnd = randomVector & randomVector;
            Assert.Equal(randomVector, selfAnd);

            Vector<T> zeroAnd = randomVector & zeroVector;
            Assert.Equal(zeroVector, zeroAnd);
        }

        [Fact]
        public void BitwiseOrOperatorByte() { TestBitwiseOrOperator<Byte>(); }
        [Fact]
        public void BitwiseOrOperatorSByte() { TestBitwiseOrOperator<SByte>(); }
        [Fact]
        public void BitwiseOrOperatorUInt16() { TestBitwiseOrOperator<UInt16>(); }
        [Fact]
        public void BitwiseOrOperatorInt16() { TestBitwiseOrOperator<Int16>(); }
        [Fact]
        public void BitwiseOrOperatorUInt32() { TestBitwiseOrOperator<UInt32>(); }
        [Fact]
        public void BitwiseOrOperatorInt32() { TestBitwiseOrOperator<Int32>(); }
        [Fact]
        public void BitwiseOrOperatorUInt64() { TestBitwiseOrOperator<UInt64>(); }
        [Fact]
        public void BitwiseOrOperatorInt64() { TestBitwiseOrOperator<Int64>(); }
        private void TestBitwiseOrOperator<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            Vector<T> randomVector = new Vector<T>(values1);
            Vector<T> zeroVector = Vector<T>.Zero;

            Vector<T> selfOr = randomVector | randomVector;
            Assert.Equal(randomVector, selfOr);

            Vector<T> zeroOr = randomVector | zeroVector;
            Assert.Equal(randomVector, zeroOr);

            Vector<T> allOnesVector = new Vector<T>(GetValueWithAllOnesSet<T>());
            Vector<T> allOnesOrZero = zeroVector | allOnesVector;
            Assert.Equal(allOnesVector, allOnesOrZero);
        }

        [Fact]
        public void BitwiseXorOperatorByte() { TestBitwiseXorOperator<Byte>(); }
        [Fact]
        public void BitwiseXorOperatorSByte() { TestBitwiseXorOperator<SByte>(); }
        [Fact]
        public void BitwiseXorOperatorUInt16() { TestBitwiseXorOperator<UInt16>(); }
        [Fact]
        public void BitwiseXorOperatorInt16() { TestBitwiseXorOperator<Int16>(); }
        [Fact]
        public void BitwiseXorOperatorUInt32() { TestBitwiseXorOperator<UInt32>(); }
        [Fact]
        public void BitwiseXorOperatorInt32() { TestBitwiseXorOperator<Int32>(); }
        [Fact]
        public void BitwiseXorOperatorUInt64() { TestBitwiseXorOperator<UInt64>(); }
        [Fact]
        public void BitwiseXorOperatorInt64() { TestBitwiseXorOperator<Int64>(); }
        private void TestBitwiseXorOperator<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            Vector<T> randomVector1 = new Vector<T>(values1);
            Vector<T> randomVector2 = new Vector<T>(values2);

            Vector<T> result = randomVector1 ^ randomVector2;
            ValidateVector(result,
                (index, val) =>
                {
                    T expected = Util.Xor(values1[index], values2[index]);
                    Assert.Equal(expected, val);
                });
        }

        [Fact]
        public void BitwiseOnesComplementOperatorByte() { TestBitwiseOnesComplementOperator<Byte>(); }
        [Fact]
        public void BitwiseOnesComplementOperatorSByte() { TestBitwiseOnesComplementOperator<SByte>(); }
        [Fact]
        public void BitwiseOnesComplementOperatorUInt16() { TestBitwiseOnesComplementOperator<UInt16>(); }
        [Fact]
        public void BitwiseOnesComplementOperatorInt16() { TestBitwiseOnesComplementOperator<Int16>(); }
        [Fact]
        public void BitwiseOnesComplementOperatorUInt32() { TestBitwiseOnesComplementOperator<UInt32>(); }
        [Fact]
        public void BitwiseOnesComplementOperatorInt32() { TestBitwiseOnesComplementOperator<Int32>(); }
        [Fact]
        public void BitwiseOnesComplementOperatorUInt64() { TestBitwiseOnesComplementOperator<UInt64>(); }
        [Fact]
        public void BitwiseOnesComplementOperatorInt64() { TestBitwiseOnesComplementOperator<Int64>(); }
        private void TestBitwiseOnesComplementOperator<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            Vector<T> randomVector1 = new Vector<T>(values1);

            Vector<T> result = ~randomVector1;
            ValidateVector(result,
                (index, val) =>
                {
                    T expected = Util.OnesComplement(values1[index]);
                    Assert.Equal(expected, val);
                });
        }

        [Fact]
        public void BitwiseAndNotByte() { TestBitwiseAndNot<Byte>(); }
        [Fact]
        public void BitwiseAndNotSByte() { TestBitwiseAndNot<SByte>(); }
        [Fact]
        public void BitwiseAndNotUInt16() { TestBitwiseAndNot<UInt16>(); }
        [Fact]
        public void BitwiseAndNotInt16() { TestBitwiseAndNot<Int16>(); }
        [Fact]
        public void BitwiseAndNotUInt32() { TestBitwiseAndNot<UInt32>(); }
        [Fact]
        public void BitwiseAndNotInt32() { TestBitwiseAndNot<Int32>(); }
        [Fact]
        public void BitwiseAndNotUInt64() { TestBitwiseAndNot<UInt64>(); }
        [Fact]
        public void BitwiseAndNotInt64() { TestBitwiseAndNot<Int64>(); }
        private void TestBitwiseAndNot<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            Vector<T> randomVector1 = new Vector<T>(values1);
            Vector<T> randomVector2 = new Vector<T>(values2);

            Vector<T> result = Vector.AndNot(randomVector1, randomVector2);
            Vector<T> result2 = randomVector1 & ~randomVector2;
            ValidateVector(result,
                (index, val) =>
                {
                    T expected = Util.AndNot(values1[index], values2[index]);
                    Assert.Equal(expected, val);
                    Assert.Equal(expected, result2[index]);
                    Assert.Equal(result2[index], val);
                });
        }
        #endregion

        #region Comparison Tests
        [Fact]
        public void VectorGreaterThanByte() { TestVectorGreaterThan<Byte>(); }
        [Fact]
        public void VectorGreaterThanSByte() { TestVectorGreaterThan<SByte>(); }
        [Fact]
        public void VectorGreaterThanUInt16() { TestVectorGreaterThan<UInt16>(); }
        [Fact]
        public void VectorGreaterThanInt16() { TestVectorGreaterThan<Int16>(); }
        [Fact]
        public void VectorGreaterThanUInt32() { TestVectorGreaterThan<UInt32>(); }
        [Fact]
        public void VectorGreaterThanInt32() { TestVectorGreaterThan<Int32>(); }
        [Fact]
        public void VectorGreaterThanUInt64() { TestVectorGreaterThan<UInt64>(); }
        [Fact]
        public void VectorGreaterThanInt64() { TestVectorGreaterThan<Int64>(); }
        [Fact]
        public void VectorGreaterThanSingle() { TestVectorGreaterThan<Single>(); }
        [Fact]
        public void VectorGreaterThanDouble() { TestVectorGreaterThan<Double>(); }
        private void TestVectorGreaterThan<T>() where T : struct
        {
            var values1 = GenerateRandomValuesForVector<T>();
            var values2 = GenerateRandomValuesForVector<T>();
            var vec1 = new Vector<T>(values1);
            var vec2 = new Vector<T>(values2);

            var result = Vector.GreaterThan<T>(vec1, vec2);
            ValidateVector(result,
                (index, val) =>
                {
                    bool isGreater = Util.GreaterThan(values1[index], values2[index]);
                    T expected = isGreater ? GetValueWithAllOnesSet<T>() : Util.Zero<T>();
                    Assert.Equal(expected, result[index]);
                });
        }

        [Fact]
        public void GreaterThanOrEqualByte() { TestVectorGreaterThanOrEqual<Byte>(); }
        [Fact]
        public void GreaterThanOrEqualSByte() { TestVectorGreaterThanOrEqual<SByte>(); }
        [Fact]
        public void GreaterThanOrEqualUInt16() { TestVectorGreaterThanOrEqual<UInt16>(); }
        [Fact]
        public void GreaterThanOrEqualInt16() { TestVectorGreaterThanOrEqual<Int16>(); }
        [Fact]
        public void GreaterThanOrEqualUInt32() { TestVectorGreaterThanOrEqual<UInt32>(); }
        [Fact]
        public void GreaterThanOrEqualInt32() { TestVectorGreaterThanOrEqual<Int32>(); }
        [Fact]
        public void GreaterThanOrEqualUInt64() { TestVectorGreaterThanOrEqual<UInt64>(); }
        [Fact]
        public void GreaterThanOrEqualInt64() { TestVectorGreaterThanOrEqual<Int64>(); }
        [Fact]
        public void GreaterThanOrEqualSingle() { TestVectorGreaterThanOrEqual<Single>(); }
        [Fact]
        public void GreaterThanOrEqualDouble() { TestVectorGreaterThanOrEqual<Double>(); }
        private void TestVectorGreaterThanOrEqual<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            Vector<T> vec1 = new Vector<T>(values1);
            Vector<T> vec2 = new Vector<T>(values2);

            Vector<T> result = Vector.GreaterThanOrEqual<T>(vec1, vec2);
            ValidateVector(result,
                (index, val) =>
                {
                    bool isGreaterOrEqual = Util.GreaterThanOrEqual(values1[index], values2[index]);
                    T expected = isGreaterOrEqual ? GetValueWithAllOnesSet<T>() : Util.Zero<T>();
                    Assert.Equal(expected, result[index]);
                });
        }

        [Fact]
        public void GreaterThanAnyByte() { TestVectorGreaterThanAny<Byte>(); }
        [Fact]
        public void GreaterThanAnySByte() { TestVectorGreaterThanAny<SByte>(); }
        [Fact]
        public void GreaterThanAnyUInt16() { TestVectorGreaterThanAny<UInt16>(); }
        [Fact]
        public void GreaterThanAnyInt16() { TestVectorGreaterThanAny<Int16>(); }
        [Fact]
        public void GreaterThanAnyUInt32() { TestVectorGreaterThanAny<UInt32>(); }
        [Fact]
        public void GreaterThanAnyInt32() { TestVectorGreaterThanAny<Int32>(); }
        [Fact]
        public void GreaterThanAnyUInt64() { TestVectorGreaterThanAny<UInt64>(); }
        [Fact]
        public void GreaterThanAnyInt64() { TestVectorGreaterThanAny<Int64>(); }
        [Fact]
        public void GreaterThanAnySingle() { TestVectorGreaterThanAny<Single>(); }
        [Fact]
        public void GreaterThanAnyDouble() { TestVectorGreaterThanAny<Double>(); }
        private void TestVectorGreaterThanAny<T>() where T : struct
        {
            T[] values1 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values1[g] = (T)(dynamic)(g + 10);
            }
            Vector<T> vec1 = new Vector<T>(values1);

            T[] values2 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values2[g] = unchecked((T)(dynamic)(g * 5 + 9));
            }
            Vector<T> vec2 = new Vector<T>(values2);

            T[] values3 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values3[g] = (T)(dynamic)(g + 12);
            }
            Vector<T> vec3 = new Vector<T>(values3);

            Assert.True(Vector.GreaterThanAny(vec1, vec2));
            Assert.True(Vector.GreaterThanAny(vec2, vec1));
            Assert.True(Vector.GreaterThanAny(vec3, vec1));
            Assert.True(Vector.GreaterThanAny(vec2, vec3));
            Assert.False(Vector.GreaterThanAny(vec1, vec3));
        }

        [Fact]
        public void GreaterThanAllByte() { TestVectorGreaterThanAll<Byte>(); }
        [Fact]
        public void GreaterThanAllSByte() { TestVectorGreaterThanAll<SByte>(); }
        [Fact]
        public void GreaterThanAllUInt16() { TestVectorGreaterThanAll<UInt16>(); }
        [Fact]
        public void GreaterThanAllInt16() { TestVectorGreaterThanAll<Int16>(); }
        [Fact]
        public void GreaterThanAllUInt32() { TestVectorGreaterThanAll<UInt32>(); }
        [Fact]
        public void GreaterThanAllInt32() { TestVectorGreaterThanAll<Int32>(); }
        [Fact]
        public void GreaterThanAllUInt64() { TestVectorGreaterThanAll<UInt64>(); }
        [Fact]
        public void GreaterThanAllInt64() { TestVectorGreaterThanAll<Int64>(); }
        [Fact]
        public void GreaterThanAllSingle() { TestVectorGreaterThanAll<Single>(); }
        [Fact]
        public void GreaterThanAllDouble() { TestVectorGreaterThanAll<Double>(); }
        private void TestVectorGreaterThanAll<T>() where T : struct
        {
            T[] values1 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values1[g] = (T)(dynamic)(g + 10);
            }
            Vector<T> vec1 = new Vector<T>(values1);

            T[] values2 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values2[g] = unchecked((T)(dynamic)(g * 5 + 9));
            }
            Vector<T> vec2 = new Vector<T>(values2);

            T[] values3 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values3[g] = (T)(dynamic)(g + 12);
            }
            Vector<T> vec3 = new Vector<T>(values3);

            Assert.False(Vector.GreaterThanAll(vec1, vec2));
            Assert.False(Vector.GreaterThanAll(vec2, vec1));
            Assert.True(Vector.GreaterThanAll(vec3, vec1));
            Assert.False(Vector.GreaterThanAll(vec1, vec3));
        }

        [Fact]
        public void GreaterThanOrEqualAnyByte() { TestVectorGreaterThanOrEqualAny<Byte>(); }
        [Fact]
        public void GreaterThanOrEqualAnySByte() { TestVectorGreaterThanOrEqualAny<SByte>(); }
        [Fact]
        public void GreaterThanOrEqualAnyUInt16() { TestVectorGreaterThanOrEqualAny<UInt16>(); }
        [Fact]
        public void GreaterThanOrEqualAnyInt16() { TestVectorGreaterThanOrEqualAny<Int16>(); }
        [Fact]
        public void GreaterThanOrEqualAnyUInt32() { TestVectorGreaterThanOrEqualAny<UInt32>(); }
        [Fact]
        public void GreaterThanOrEqualAnyInt32() { TestVectorGreaterThanOrEqualAny<Int32>(); }
        [Fact]
        public void GreaterThanOrEqualAnyUInt64() { TestVectorGreaterThanOrEqualAny<UInt64>(); }
        [Fact]
        public void GreaterThanOrEqualAnyInt64() { TestVectorGreaterThanOrEqualAny<Int64>(); }
        [Fact]
        public void GreaterThanOrEqualAnySingle() { TestVectorGreaterThanOrEqualAny<Single>(); }
        [Fact]
        public void GreaterThanOrEqualAnyDouble() { TestVectorGreaterThanOrEqualAny<Double>(); }
        private void TestVectorGreaterThanOrEqualAny<T>() where T : struct
        {
            int maxT = GetMaxValue<T>();
            double maxStep = (double)maxT / (double)Vector<T>.Count;
            double halfStep = maxStep / 2;

            T[] values1 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values1[g] = (T)(dynamic)(g * halfStep);
            }
            Vector<T> vec1 = new Vector<T>(values1);

            T[] values2 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values2[g] = (T)(dynamic)(g * maxStep);
            }
            Vector<T> vec2 = new Vector<T>(values2);

            T[] values3 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values3[g] = (T)(dynamic)((g + 1) * maxStep);
            }
            Vector<T> vec3 = new Vector<T>(values3);

            Assert.True(Vector.GreaterThanOrEqualAny(vec1, vec2));
            Assert.True(Vector.GreaterThanOrEqualAny(vec2, vec1));
            Assert.True(Vector.GreaterThanOrEqualAny(vec3, vec1));
            Assert.True(Vector.GreaterThanOrEqualAny(vec3, vec2));
            Assert.False(Vector.GreaterThanOrEqualAny(vec1, vec3));
            Assert.False(Vector.GreaterThanOrEqualAny(vec2, vec3));

            Assert.True(Vector.GreaterThanOrEqualAny(vec1, vec1));
            Assert.True(Vector.GreaterThanOrEqualAny(vec2, vec2));
            Assert.True(Vector.GreaterThanOrEqualAny(vec3, vec3));
        }

        [Fact]
        public void GreaterThanOrEqualAllByte() { TestVectorGreaterThanOrEqualAll<Byte>(); }
        [Fact]
        public void GreaterThanOrEqualAllSByte() { TestVectorGreaterThanOrEqualAll<SByte>(); }
        [Fact]
        public void GreaterThanOrEqualAllUInt16() { TestVectorGreaterThanOrEqualAll<UInt16>(); }
        [Fact]
        public void GreaterThanOrEqualAllInt16() { TestVectorGreaterThanOrEqualAll<Int16>(); }
        [Fact]
        public void GreaterThanOrEqualAllUInt32() { TestVectorGreaterThanOrEqualAll<UInt32>(); }
        [Fact]
        public void GreaterThanOrEqualAllInt32() { TestVectorGreaterThanOrEqualAll<Int32>(); }
        [Fact]
        public void GreaterThanOrEqualAllUInt64() { TestVectorGreaterThanOrEqualAll<UInt64>(); }
        [Fact]
        public void GreaterThanOrEqualAllInt64() { TestVectorGreaterThanOrEqualAll<Int64>(); }
        [Fact]
        public void GreaterThanOrEqualAllSingle() { TestVectorGreaterThanOrEqualAll<Single>(); }
        [Fact]
        public void GreaterThanOrEqualAllDouble() { TestVectorGreaterThanOrEqualAll<Double>(); }
        private void TestVectorGreaterThanOrEqualAll<T>() where T : struct
        {
            int maxT = GetMaxValue<T>();
            double maxStep = (double)maxT / (double)Vector<T>.Count;
            double halfStep = maxStep / 2;

            T[] values1 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values1[g] = (T)(dynamic)(g * halfStep);
            }
            Vector<T> vec1 = new Vector<T>(values1);

            T[] values2 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values2[g] = (T)(dynamic)(g * maxStep);
            }
            Vector<T> vec2 = new Vector<T>(values2);

            T[] values3 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values3[g] = (T)(dynamic)((g + 1) * maxStep);
            }
            Vector<T> vec3 = new Vector<T>(values3);

            Assert.False(Vector.GreaterThanOrEqualAll(vec1, vec2));
            Assert.True(Vector.GreaterThanOrEqualAll(vec2, vec1));
            Assert.True(Vector.GreaterThanOrEqualAll(vec3, vec1));
            Assert.True(Vector.GreaterThanOrEqualAll(vec3, vec2));
            Assert.False(Vector.GreaterThanOrEqualAll(vec1, vec3));

            Assert.True(Vector.GreaterThanOrEqualAll(vec1, vec1));
            Assert.True(Vector.GreaterThanOrEqualAll(vec2, vec2));
            Assert.True(Vector.GreaterThanOrEqualAll(vec3, vec3));
        }

        [Fact]
        public void LessThanByte() { TestVectorLessThan<Byte>(); }
        [Fact]
        public void LessThanSByte() { TestVectorLessThan<SByte>(); }
        [Fact]
        public void LessThanUInt16() { TestVectorLessThan<UInt16>(); }
        [Fact]
        public void LessThanInt16() { TestVectorLessThan<Int16>(); }
        [Fact]
        public void LessThanUInt32() { TestVectorLessThan<UInt32>(); }
        [Fact]
        public void LessThanInt32() { TestVectorLessThan<Int32>(); }
        [Fact]
        public void LessThanUInt64() { TestVectorLessThan<UInt64>(); }
        [Fact]
        public void LessThanInt64() { TestVectorLessThan<Int64>(); }
        [Fact]
        public void LessThanSingle() { TestVectorLessThan<Single>(); }
        [Fact]
        public void LessThanDouble() { TestVectorLessThan<Double>(); }
        private void TestVectorLessThan<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            Vector<T> vec1 = new Vector<T>(values1);
            Vector<T> vec2 = new Vector<T>(values2);

            var result = Vector.LessThan<T>(vec1, vec2);
            ValidateVector(result,
                (index, val) =>
                {
                    bool isLess = Util.LessThan(values1[index], values2[index]);
                    T expected = isLess ? GetValueWithAllOnesSet<T>() : Util.Zero<T>();
                    Assert.Equal(expected, result[index]);
                });
        }

        [Fact]
        public void LessThanOrEqualByte() { TestVectorLessThanOrEqual<Byte>(); }
        [Fact]
        public void LessThanOrEqualSByte() { TestVectorLessThanOrEqual<SByte>(); }
        [Fact]
        public void LessThanOrEqualUInt16() { TestVectorLessThanOrEqual<UInt16>(); }
        [Fact]
        public void LessThanOrEqualInt16() { TestVectorLessThanOrEqual<Int16>(); }
        [Fact]
        public void LessThanOrEqualUInt32() { TestVectorLessThanOrEqual<UInt32>(); }
        [Fact]
        public void LessThanOrEqualInt32() { TestVectorLessThanOrEqual<Int32>(); }
        [Fact]
        public void LessThanOrEqualUInt64() { TestVectorLessThanOrEqual<UInt64>(); }
        [Fact]
        public void LessThanOrEqualInt64() { TestVectorLessThanOrEqual<Int64>(); }
        [Fact]
        public void LessThanOrEqualSingle() { TestVectorLessThanOrEqual<Single>(); }
        [Fact]
        public void LessThanOrEqualDouble() { TestVectorLessThanOrEqual<Double>(); }
        private void TestVectorLessThanOrEqual<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            Vector<T> vec1 = new Vector<T>(values1);
            Vector<T> vec2 = new Vector<T>(values2);

            var result = Vector.LessThanOrEqual<T>(vec1, vec2);
            ValidateVector(result,
                (index, val) =>
                {
                    bool isLessOrEqual = Util.LessThanOrEqual(values1[index], values2[index]);
                    T expected = isLessOrEqual ? GetValueWithAllOnesSet<T>() : Util.Zero<T>();
                    Assert.Equal(expected, result[index]);
                });
        }

        [Fact]
        public void LessThanAnyByte() { TestVectorLessThanAny<Byte>(); }
        [Fact]
        public void LessThanAnySByte() { TestVectorLessThanAny<SByte>(); }
        [Fact]
        public void LessThanAnyUInt16() { TestVectorLessThanAny<UInt16>(); }
        [Fact]
        public void LessThanAnyInt16() { TestVectorLessThanAny<Int16>(); }
        [Fact]
        public void LessThanAnyUInt32() { TestVectorLessThanAny<UInt32>(); }
        [Fact]
        public void LessThanAnyInt32() { TestVectorLessThanAny<Int32>(); }
        [Fact]
        public void LessThanAnyUInt64() { TestVectorLessThanAny<UInt64>(); }
        [Fact]
        public void LessThanAnyInt64() { TestVectorLessThanAny<Int64>(); }
        [Fact]
        public void LessThanAnySingle() { TestVectorLessThanAny<Single>(); }
        [Fact]
        public void LessThanAnyDouble() { TestVectorLessThanAny<Double>(); }
        private void TestVectorLessThanAny<T>() where T : struct
        {
            T[] values1 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values1[g] = (T)(dynamic)g;
            }
            Vector<T> vec1 = new Vector<T>(values1);
            values1[0] = Util.Add(values1[0], Util.One<T>());
            Vector<T> vec2 = new Vector<T>(values1);

            Assert.False(Vector.LessThanAny(vec1, vec1));
            Assert.True(Vector.LessThanAny(vec1, vec2));
        }

        [Fact]
        public void LessThanAllByte() { TestVectorLessThanAll<Byte>(); }
        [Fact]
        public void LessThanAllSByte() { TestVectorLessThanAll<SByte>(); }
        [Fact]
        public void LessThanAllUInt16() { TestVectorLessThanAll<UInt16>(); }
        [Fact]
        public void LessThanAllInt16() { TestVectorLessThanAll<Int16>(); }
        [Fact]
        public void LessThanAllUInt32() { TestVectorLessThanAll<UInt32>(); }
        [Fact]
        public void LessThanAllInt32() { TestVectorLessThanAll<Int32>(); }
        [Fact]
        public void LessThanAllUInt64() { TestVectorLessThanAll<UInt64>(); }
        [Fact]
        public void LessThanAllInt64() { TestVectorLessThanAll<Int64>(); }
        [Fact]
        public void LessThanAllSingle() { TestVectorLessThanAll<Single>(); }
        [Fact]
        public void LessThanAllDouble() { TestVectorLessThanAll<Double>(); }
        private void TestVectorLessThanAll<T>() where T : struct
        {
            T[] values1 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values1[g] = (T)(dynamic)g;
            }
            Vector<T> vec1 = new Vector<T>(values1);

            T[] values2 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values2[g] = (T)(dynamic)(g + 25);
            }
            Vector<T> vec2 = new Vector<T>(values2);

            Assert.True(Vector.LessThanAll(vec1, vec2));
            Assert.True(Vector.LessThanAll(Vector<T>.Zero, Vector<T>.One));

            T[] values3 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values3[g] = (g < Vector<T>.Count / 2) ? Util.Zero<T>() : Util.One<T>();
            }
            Vector<T> vec3 = new Vector<T>(values3);
            Assert.False(Vector.LessThanAll(vec3, Vector<T>.One));
        }

        [Fact]
        public void LessThanOrEqualAnyByte() { TestVectorLessThanOrEqualAny<Byte>(); }
        [Fact]
        public void LessThanOrEqualAnySByte() { TestVectorLessThanOrEqualAny<SByte>(); }
        [Fact]
        public void LessThanOrEqualAnyUInt16() { TestVectorLessThanOrEqualAny<UInt16>(); }
        [Fact]
        public void LessThanOrEqualAnyInt16() { TestVectorLessThanOrEqualAny<Int16>(); }
        [Fact]
        public void LessThanOrEqualAnyUInt32() { TestVectorLessThanOrEqualAny<UInt32>(); }
        [Fact]
        public void LessThanOrEqualAnyInt32() { TestVectorLessThanOrEqualAny<Int32>(); }
        [Fact]
        public void LessThanOrEqualAnyUInt64() { TestVectorLessThanOrEqualAny<UInt64>(); }
        [Fact]
        public void LessThanOrEqualAnyInt64() { TestVectorLessThanOrEqualAny<Int64>(); }
        [Fact]
        public void LessThanOrEqualAnySingle() { TestVectorLessThanOrEqualAny<Single>(); }
        [Fact]
        public void LessThanOrEqualAnyDouble() { TestVectorLessThanOrEqualAny<Double>(); }
        private void TestVectorLessThanOrEqualAny<T>() where T : struct
        {
            T[] values1 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values1[g] = (T)(dynamic)g;
            }
            Vector<T> vec1 = new Vector<T>(values1);

            T[] values2 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values2[g] = (T)(dynamic)(g * 2);
            }
            Vector<T> vec2 = new Vector<T>(values2);

            T[] values3 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values3[g] = (T)(dynamic)(g + 2);
            }
            Vector<T> vec3 = new Vector<T>(values3);

            Assert.True(Vector.LessThanOrEqualAny(vec1, vec2));
            Assert.True(Vector.LessThanOrEqualAny(vec2, vec1));

            Assert.False(Vector.LessThanOrEqualAny(vec3, vec1));
            Assert.True(Vector.LessThanOrEqualAny(vec1, vec3));
            Assert.True(Vector.LessThanOrEqualAny(vec2, vec3));

            Assert.True(Vector.LessThanOrEqualAny(vec1, vec1));
            Assert.True(Vector.LessThanOrEqualAny(vec2, vec2));
        }

        [Fact]
        public void LessThanOrEqualAllByte() { TestVectorLessThanOrEqualAll<Byte>(); }
        [Fact]
        public void LessThanOrEqualAllSByte() { TestVectorLessThanOrEqualAll<SByte>(); }
        [Fact]
        public void LessThanOrEqualAllUInt16() { TestVectorLessThanOrEqualAll<UInt16>(); }
        [Fact]
        public void LessThanOrEqualAllInt16() { TestVectorLessThanOrEqualAll<Int16>(); }
        [Fact]
        public void LessThanOrEqualAllUInt32() { TestVectorLessThanOrEqualAll<UInt32>(); }
        [Fact]
        public void LessThanOrEqualAllInt32() { TestVectorLessThanOrEqualAll<Int32>(); }
        [Fact]
        public void LessThanOrEqualAllUInt64() { TestVectorLessThanOrEqualAll<UInt64>(); }
        [Fact]
        public void LessThanOrEqualAllInt64() { TestVectorLessThanOrEqualAll<Int64>(); }
        [Fact]
        public void LessThanOrEqualAllSingle() { TestVectorLessThanOrEqualAll<Single>(); }
        [Fact]
        public void LessThanOrEqualAllDouble() { TestVectorLessThanOrEqualAll<Double>(); }
        private void TestVectorLessThanOrEqualAll<T>() where T : struct
        {
            T[] values1 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values1[g] = (T)(dynamic)g;
            }
            Vector<T> vec1 = new Vector<T>(values1);

            T[] values2 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values2[g] = (T)(dynamic)(g * 2);
            }
            Vector<T> vec2 = new Vector<T>(values2);

            T[] values3 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values3[g] = (T)(dynamic)(g + 2);
            }
            Vector<T> vec3 = new Vector<T>(values3);

            Assert.True(Vector.LessThanOrEqualAll(vec1, vec2));
            Assert.False(Vector.LessThanOrEqualAll(vec2, vec1));

            Assert.False(Vector.LessThanOrEqualAll(vec3, vec1));
            Assert.True(Vector.LessThanOrEqualAll(vec1, vec3));

            Assert.True(Vector.LessThanOrEqualAll(vec1, vec1));
            Assert.True(Vector.LessThanOrEqualAll(vec2, vec2));
        }

        [Fact]
        public void VectorEqualsByte() { TestVectorEquals<Byte>(); }
        [Fact]
        public void VectorEqualsSByte() { TestVectorEquals<SByte>(); }
        [Fact]
        public void VectorEqualsUInt16() { TestVectorEquals<UInt16>(); }
        [Fact]
        public void VectorEqualsInt16() { TestVectorEquals<Int16>(); }
        [Fact]
        public void VectorEqualsUInt32() { TestVectorEquals<UInt32>(); }
        [Fact]
        public void VectorEqualsInt32() { TestVectorEquals<Int32>(); }
        [Fact]
        public void VectorEqualsUInt64() { TestVectorEquals<UInt64>(); }
        [Fact]
        public void VectorEqualsInt64() { TestVectorEquals<Int64>(); }
        [Fact]
        public void VectorEqualsSingle() { TestVectorEquals<Single>(); }
        [Fact]
        public void VectorEqualsDouble() { TestVectorEquals<Double>(); }
        private void TestVectorEquals<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2;
            do
            {
                values2 = GenerateRandomValuesForVector<T>();
            }
            while (Util.AnyEqual(values1, values2));

            Array.Copy(values1, 0, values2, 0, Vector<T>.Count / 2);
            Vector<T> vec1 = new Vector<T>(values1);
            Vector<T> vec2 = new Vector<T>(values2);

            Vector<T> result = Vector.Equals(vec1, vec2);
            for (int g = 0; g < Vector<T>.Count / 2; g++)
            {
                Assert.Equal(GetValueWithAllOnesSet<T>(), result[g]);
            }
            for (int g = Vector<T>.Count / 2; g < Vector<T>.Count; g++)
            {
                Assert.Equal((T)(dynamic)0, result[g]);
            }
        }

        [Fact]
        public void VectorEqualsAnyByte() { TestVectorEqualsAny<Byte>(); }
        [Fact]
        public void VectorEqualsAnySByte() { TestVectorEqualsAny<SByte>(); }
        [Fact]
        public void VectorEqualsAnyUInt16() { TestVectorEqualsAny<UInt16>(); }
        [Fact]
        public void VectorEqualsAnyInt16() { TestVectorEqualsAny<Int16>(); }
        [Fact]
        public void VectorEqualsAnyUInt32() { TestVectorEqualsAny<UInt32>(); }
        [Fact]
        public void VectorEqualsAnyInt32() { TestVectorEqualsAny<Int32>(); }
        [Fact]
        public void VectorEqualsAnyUInt64() { TestVectorEqualsAny<UInt64>(); }
        [Fact]
        public void VectorEqualsAnyInt64() { TestVectorEqualsAny<Int64>(); }
        [Fact]
        public void VectorEqualsAnySingle() { TestVectorEqualsAny<Single>(); }
        [Fact]
        public void VectorEqualsAnyDouble() { TestVectorEqualsAny<Double>(); }
        private void TestVectorEqualsAny<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2;
            do
            {
                values2 = GenerateRandomValuesForVector<T>();
            }
            while (Util.AnyEqual(values1, values2));

            Array.Copy(values1, 0, values2, 0, Vector<T>.Count / 2);
            Vector<T> vec1 = new Vector<T>(values1);
            Vector<T> vec2 = new Vector<T>(values2);

            bool result = Vector.EqualsAny(vec1, vec2);
            Assert.True(result);

            do
            {
                values2 = GenerateRandomValuesForVector<T>();
            }
            while (Util.AnyEqual(values1, values2));

            vec2 = new Vector<T>(values2);
            result = Vector.EqualsAny(vec1, vec2);
            Assert.False(result);
        }

        [Fact]
        public void VectorEqualsAllByte() { TestVectorEqualsAll<Byte>(); }
        [Fact]
        public void VectorEqualsAllSByte() { TestVectorEqualsAll<SByte>(); }
        [Fact]
        public void VectorEqualsAllUInt16() { TestVectorEqualsAll<UInt16>(); }
        [Fact]
        public void VectorEqualsAllInt16() { TestVectorEqualsAll<Int16>(); }
        [Fact]
        public void VectorEqualsAllUInt32() { TestVectorEqualsAll<UInt32>(); }
        [Fact]
        public void VectorEqualsAllInt32() { TestVectorEqualsAll<Int32>(); }
        [Fact]
        public void VectorEqualsAllUInt64() { TestVectorEqualsAll<UInt64>(); }
        [Fact]
        public void VectorEqualsAllInt64() { TestVectorEqualsAll<Int64>(); }
        [Fact]
        public void VectorEqualsAllSingle() { TestVectorEqualsAll<Single>(); }
        [Fact]
        public void VectorEqualsAllDouble() { TestVectorEqualsAll<Double>(); }
        private void TestVectorEqualsAll<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2;
            do
            {
                values2 = GenerateRandomValuesForVector<T>();
            }
            while (Util.AnyEqual(values1, values2));

            Array.Copy(values1, 0, values2, 0, Vector<T>.Count / 2);
            Vector<T> vec1 = new Vector<T>(values1);
            Vector<T> vec2 = new Vector<T>(values2);

            bool result = Vector.EqualsAll(vec1, vec2);
            Assert.False(result);

            result = Vector.EqualsAny(vec1, vec1);
            Assert.True(result);
        }
        #endregion

        #region Selection Tests
        [Fact]
        public void ConditionalSelectByte() { TestConditionalSelect<Byte>(); }
        [Fact]
        public void ConditionalSelectSByte() { TestConditionalSelect<SByte>(); }
        [Fact]
        public void ConditionalSelectUInt16() { TestConditionalSelect<UInt16>(); }
        [Fact]
        public void ConditionalSelectInt16() { TestConditionalSelect<Int16>(); }
        [Fact]
        public void ConditionalSelectUInt32() { TestConditionalSelect<UInt32>(); }
        [Fact]
        public void ConditionalSelectInt32() { TestConditionalSelect<Int32>(); }
        [Fact]
        public void ConditionalSelectUInt64() { TestConditionalSelect<UInt64>(); }
        [Fact]
        public void ConditionalSelectInt64() { TestConditionalSelect<Int64>(); }
        [Fact]
        public void ConditionalSelectSingle() { TestConditionalSelect<Single>(); }
        [Fact]
        public void ConditionalSelectDouble() { TestConditionalSelect<Double>(); }
        private void TestConditionalSelect<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            Vector<T> vec1 = new Vector<T>(values1);
            Vector<T> vec2 = new Vector<T>(values2);

            // Using Greater Than mask
            Vector<T> mask = Vector.GreaterThan(vec1, vec2);
            Vector<T> result = Vector.ConditionalSelect(mask, vec1, vec2);
            ValidateVector(result,
                (index, val) =>
                {
                    bool isGreater = Util.GreaterThan(values1[index], values2[index]);
                    T expected = isGreater ? values1[index] : values2[index];
                    Assert.Equal(expected, val);
                });

            // Using Less Than Or Equal mask
            Vector<T> mask2 = Vector.LessThanOrEqual(vec1, vec2);
            Vector<T> result2 = Vector.ConditionalSelect(mask2, vec1, vec2);
            ValidateVector(result2,
                (index, val) =>
                {
                    bool isLessOrEqual = Util.LessThanOrEqual(values1[index], values2[index]);
                    T expected = isLessOrEqual ? values1[index] : values2[index];
                    Assert.Equal(expected, val);
                });
        }
        #endregion

        #region Vector Tests
        [Fact]
        public void DotProductByte() { TestDotProduct<Byte>(); }
        [Fact]
        public void DotProductSByte() { TestDotProduct<SByte>(); }
        [Fact]
        public void DotProductUInt16() { TestDotProduct<UInt16>(); }
        [Fact]
        public void DotProductInt16() { TestDotProduct<Int16>(); }
        [Fact]
        public void DotProductUInt32() { TestDotProduct<UInt32>(); }
        [Fact]
        public void DotProductInt32() { TestDotProduct<Int32>(); }
        [Fact]
        public void DotProductUInt64() { TestDotProduct<UInt64>(); }
        [Fact]
        public void DotProductInt64() { TestDotProduct<Int64>(); }
        [Fact]
        public void DotProductSingle() { TestDotProduct<Single>(); }
        [Fact]
        public void DotProductDouble() { TestDotProduct<Double>(); }
        private void TestDotProduct<T>() where T : struct
        {
            T[] values1 = Util.GenerateRandomValues<T>(Vector<T>.Count);
            T[] values2 = Util.GenerateRandomValues<T>(Vector<T>.Count);
            Vector<T> vector1 = new Vector<T>(values1);
            Vector<T> vector2 = new Vector<T>(values2);

            T dotProduct = Vector.Dot(vector1, vector2);
            T expected = Util.Zero<T>();
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                expected = Util.Add(expected, Util.Multiply(values1[g], values2[g]));
            }
            Assert.Equal(expected, dotProduct);
        }

        [Fact]
        public void MaxByte() { TestMax<Byte>(); }
        [Fact]
        public void MaxSByte() { TestMax<SByte>(); }
        [Fact]
        public void MaxUInt16() { TestMax<UInt16>(); }
        [Fact]
        public void MaxInt16() { TestMax<Int16>(); }
        [Fact]
        public void MaxUInt32() { TestMax<UInt32>(); }
        [Fact]
        public void MaxInt32() { TestMax<Int32>(); }
        [Fact]
        public void MaxUInt64() { TestMax<UInt64>(); }
        [Fact]
        public void MaxInt64() { TestMax<Int64>(); }
        [Fact]
        public void MaxSingle() { TestMax<Single>(); }
        [Fact]
        public void MaxDouble() { TestMax<Double>(); }
        private void TestMax<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            Vector<T> vector1 = new Vector<T>(values1);
            Vector<T> vector2 = new Vector<T>(values2);

            Vector<T> maxVector = Vector.Max(vector1, vector2);
            ValidateVector(maxVector,
                (index, val) =>
                {
                    T expected = Util.GreaterThan(values1[index], values2[index]) ? values1[index] : values2[index];
                    Assert.Equal(expected, val);
                });
        }

        [Fact]
        public void MinByte() { TestMin<Byte>(); }
        [Fact]
        public void MinSByte() { TestMin<SByte>(); }
        [Fact]
        public void MinUInt16() { TestMin<UInt16>(); }
        [Fact]
        public void MinInt16() { TestMin<Int16>(); }
        [Fact]
        public void MinUInt32() { TestMin<UInt32>(); }
        [Fact]
        public void MinInt32() { TestMin<Int32>(); }
        [Fact]
        public void MinUInt64() { TestMin<UInt64>(); }
        [Fact]
        public void MinInt64() { TestMin<Int64>(); }
        [Fact]
        public void MinSingle() { TestMin<Single>(); }
        [Fact]
        public void MinDouble() { TestMin<Double>(); }
        private void TestMin<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            Vector<T> vector1 = new Vector<T>(values1);
            Vector<T> vector2 = new Vector<T>(values2);

            Vector<T> minVector = Vector.Min(vector1, vector2);
            ValidateVector(minVector,
                (index, val) =>
                {
                    T expected = Util.LessThan(values1[index], values2[index]) ? values1[index] : values2[index];
                    Assert.Equal(expected, val);
                });
        }

        [Fact]
        public void SquareRootByte() { TestSquareRoot<Byte>(); }
        [Fact]
        public void SquareRootSByte() { TestSquareRoot<SByte>(); }
        [Fact]
        public void SquareRootUInt16() { TestSquareRoot<UInt16>(); }
        [Fact]
        public void SquareRootInt16() { TestSquareRoot<Int16>(); }
        [Fact]
        public void SquareRootUInt32() { TestSquareRoot<UInt32>(); }
        [Fact]
        public void SquareRootInt32() { TestSquareRoot<Int32>(); }
        [Fact]
        public void SquareRootUInt64() { TestSquareRoot<UInt64>(); }
        [Fact]
        public void SquareRootInt64() { TestSquareRoot<Int64>(); }
        [Fact]
        public void SquareRootSingle() { TestSquareRoot<Single>(); }
        [Fact]
        public void SquareRootDouble() { TestSquareRoot<Double>(); }
        private void TestSquareRoot<T>() where T : struct
        {
            T[] values = GenerateRandomValuesForVector<T>();
            Vector<T> vector = new Vector<T>(values);

            Vector<T> SquareRootVector = Vector.SquareRoot(vector);
            ValidateVector(SquareRootVector,
                (index, val) =>
                {
                    T expected = Util.Sqrt(values[index]);
                    Assert.Equal(expected, val);
                });
        }

        [Fact]
        public void AbsByte() { TestAbs<Byte>(); }
        [Fact]
        public void AbsSByte() { TestAbs<SByte>(); }
        [Fact]
        public void AbsUInt16() { TestAbs<UInt16>(); }
        [Fact]
        public void AbsInt16() { TestAbs<Int16>(); }
        [Fact]
        public void AbsUInt32() { TestAbs<UInt32>(); }
        [Fact]
        public void AbsInt32() { TestAbs<Int32>(); }
        [Fact]
        public void AbsUInt64() { TestAbs<UInt64>(); }
        [Fact]
        public void AbsInt64() { TestAbs<Int64>(); }
        [Fact]
        public void AbsSingle() { TestAbs<Single>(); }
        [Fact]
        public void AbsDouble() { TestAbs<Double>(); }
        private void TestAbs<T>() where T : struct
        {
            T[] values = Util.GenerateRandomValues<T>(Vector<T>.Count, GetMinValue<T>() + 1, GetMaxValue<T>());
            Vector<T> vector = new Vector<T>(values);
            Vector<T> AbsVector = Vector.Abs(vector);
            ValidateVector(AbsVector,
                (index, val) =>
                {
                    T expected = Util.Abs(values[index]);
                    Assert.Equal(expected, val);
                });
        }

        #endregion

        #region Reflection Tests
        // These tests ensure that, when invoked through reflection, methods behave as expected. There are potential
        // oddities when intrinsic methods are invoked through reflection which could have unexpected effects for the developer.
        [Fact]
        public void MultiplicationReflectionByte() { TestMultiplicationReflection<Byte>(); }
        [Fact]
        public void MultiplicationReflectionSByte() { TestMultiplicationReflection<SByte>(); }
        [Fact]
        public void MultiplicationReflectionUInt16() { TestMultiplicationReflection<UInt16>(); }
        [Fact]
        public void MultiplicationReflectionInt16() { TestMultiplicationReflection<Int16>(); }
        [Fact]
        public void MultiplicationReflectionUInt32() { TestMultiplicationReflection<UInt32>(); }
        [Fact]
        public void MultiplicationReflectionInt32() { TestMultiplicationReflection<Int32>(); }
        [Fact]
        public void MultiplicationReflectionUInt64() { TestMultiplicationReflection<UInt64>(); }
        [Fact]
        public void MultiplicationReflectionInt64() { TestMultiplicationReflection<Int64>(); }
        [Fact]
        public void MultiplicationReflectionSingle() { TestMultiplicationReflection<Single>(); }
        [Fact]
        public void MultiplicationReflectionDouble() { TestMultiplicationReflection<Double>(); }
        private void TestMultiplicationReflection<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            var v1 = new Vector<T>(values1);
            var v2 = new Vector<T>(values2);
            var multOperatorMethod = typeof(Vector<T>).GetTypeInfo().GetDeclaredMethods("op_Multiply")
                .Where(mi => mi.GetParameters().Select(pi => pi.ParameterType).SequenceEqual(new Type[] { typeof(Vector<T>), typeof(Vector<T>) }))
                .Single();
            Vector<T> sum = (Vector<T>)multOperatorMethod.Invoke(null, new object[] { v1, v2 });
            ValidateVector(sum,
                (index, val) =>
                {
                    Assert.Equal(Util.Multiply(values1[index], values2[index]), val);
                });
        }

        [Fact]
        public void AdditionReflectionByte() { TestAdditionReflection<Byte>(); }
        [Fact]
        public void AdditionReflectionSByte() { TestAdditionReflection<SByte>(); }
        [Fact]
        public void AdditionReflectionUInt16() { TestAdditionReflection<UInt16>(); }
        [Fact]
        public void AdditionReflectionInt16() { TestAdditionReflection<Int16>(); }
        [Fact]
        public void AdditionReflectionUInt32() { TestAdditionReflection<UInt32>(); }
        [Fact]
        public void AdditionReflectionInt32() { TestAdditionReflection<Int32>(); }
        [Fact]
        public void AdditionReflectionUInt64() { TestAdditionReflection<UInt64>(); }
        [Fact]
        public void AdditionReflectionInt64() { TestAdditionReflection<Int64>(); }
        [Fact]
        public void AdditionReflectionSingle() { TestAdditionReflection<Single>(); }
        [Fact]
        public void AdditionReflectionDouble() { TestAdditionReflection<Double>(); }
        private void TestAdditionReflection<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            T[] values2 = GenerateRandomValuesForVector<T>();
            var v1 = new Vector<T>(values1);
            var v2 = new Vector<T>(values2);
            var addOperatorMethod = typeof(Vector<T>).GetTypeInfo().GetDeclaredMethods("op_Addition")
                .Where(mi => mi.GetParameters().Select(pi => pi.ParameterType).SequenceEqual(new Type[] { typeof(Vector<T>), typeof(Vector<T>) }))
                .Single();
            Vector<T> sum = (Vector<T>)addOperatorMethod.Invoke(null, new object[] { v1, v2 });
            ValidateVector(sum,
                (index, val) =>
                {
                    Assert.Equal(Util.Add(values1[index], values2[index]), val);
                });
        }

        [Fact]
        public void DivisionReflectionByte() { TestDivisionReflection<Byte>(); }
        [Fact]
        public void DivisionReflectionSByte() { TestDivisionReflection<SByte>(); }
        [Fact]
        public void DivisionReflectionUInt16() { TestDivisionReflection<UInt16>(); }
        [Fact]
        public void DivisionReflectionInt16() { TestDivisionReflection<Int16>(); }
        [Fact]
        public void DivisionReflectionUInt32() { TestDivisionReflection<UInt32>(); }
        [Fact]
        public void DivisionReflectionInt32() { TestDivisionReflection<Int32>(); }
        [Fact]
        public void DivisionReflectionUInt64() { TestDivisionReflection<UInt64>(); }
        [Fact]
        public void DivisionReflectionInt64() { TestDivisionReflection<Int64>(); }
        [Fact]
        public void DivisionReflectionSingle() { TestDivisionReflection<Single>(); }
        [Fact]
        public void DivisionReflectionDouble() { TestDivisionReflection<Double>(); }
        private void TestDivisionReflection<T>() where T : struct
        {
            T[] values1 = GenerateRandomValuesForVector<T>();
            values1 = values1.Select(val => val.Equals(Util.Zero<T>()) ? Util.One<T>() : val).ToArray(); // Avoid divide-by-zero
            T[] values2 = GenerateRandomValuesForVector<T>();
            values2 = values2.Select(val => val.Equals(Util.Zero<T>()) ? Util.One<T>() : val).ToArray(); // Avoid divide-by-zero
            // I replace all Zero's with One's above to avoid Divide-by-zero.

            var v1 = new Vector<T>(values1);
            var v2 = new Vector<T>(values2);
            var divideOperatorMethod = typeof(Vector<T>).GetTypeInfo().GetDeclaredMethods("op_Division")
                .Where(mi => mi.GetParameters().Select(pi => pi.ParameterType).SequenceEqual(new Type[] { typeof(Vector<T>), typeof(Vector<T>) }))
                .Single();
            Vector<T> sum = (Vector<T>)divideOperatorMethod.Invoke(null, new object[] { v1, v2 });
            ValidateVector(sum,
                (index, val) =>
                {
                    Assert.Equal(Util.Divide(values1[index], values2[index]), val);
                });
        }

        [Fact]
        public void ConstructorSingleValueReflectionByte() { TestConstructorSingleValueReflection<Byte>(); }
        [Fact]
        public void ConstructorSingleValueReflectionSByte() { TestConstructorSingleValueReflection<SByte>(); }
        [Fact]
        public void ConstructorSingleValueReflectionUInt16() { TestConstructorSingleValueReflection<UInt16>(); }
        [Fact]
        public void ConstructorSingleValueReflectionInt16() { TestConstructorSingleValueReflection<Int16>(); }
        [Fact]
        public void ConstructorSingleValueReflectionUInt32() { TestConstructorSingleValueReflection<UInt32>(); }
        [Fact]
        public void ConstructorSingleValueReflectionInt32() { TestConstructorSingleValueReflection<Int32>(); }
        [Fact]
        public void ConstructorSingleValueReflectionUInt64() { TestConstructorSingleValueReflection<UInt64>(); }
        [Fact]
        public void ConstructorSingleValueReflectionInt64() { TestConstructorSingleValueReflection<Int64>(); }
        [Fact]
        public void ConstructorSingleValueReflectionSingle() { TestConstructorSingleValueReflection<Single>(); }
        [Fact]
        public void ConstructorSingleValueReflectionDouble() { TestConstructorSingleValueReflection<Double>(); }
        private void TestConstructorSingleValueReflection<T>() where T : struct
        {
            ConstructorInfo constructor = typeof(Vector<T>).GetTypeInfo().DeclaredConstructors
                .Where(ci => ci.GetParameters().Select(pi => pi.ParameterType).SequenceEqual(new Type[] { typeof(T) }))
                .Single();
            T constantValue = Util.GenerateSingleValue<T>();
            Vector<T> vec = (Vector<T>)constructor.Invoke(new object[] { constantValue });
            ValidateVector(vec, (index, value) =>
                {
                    for (int g = 0; g < Vector<T>.Count; g++)
                    {
                        Assert.Equal(constantValue, vec[g]);
                    }
                });
        }

        [Fact]
        public void ConstructorArrayReflectionByte() { TestConstructorArrayReflection<Byte>(); }
        [Fact]
        public void ConstructorArrayReflectionSByte() { TestConstructorArrayReflection<SByte>(); }
        [Fact]
        public void ConstructorArrayReflectionUInt16() { TestConstructorArrayReflection<UInt16>(); }
        [Fact]
        public void ConstructorArrayReflectionInt16() { TestConstructorArrayReflection<Int16>(); }
        [Fact]
        public void ConstructorArrayReflectionUInt32() { TestConstructorArrayReflection<UInt32>(); }
        [Fact]
        public void ConstructorArrayReflectionInt32() { TestConstructorArrayReflection<Int32>(); }
        [Fact]
        public void ConstructorArrayReflectionUInt64() { TestConstructorArrayReflection<UInt64>(); }
        [Fact]
        public void ConstructorArrayReflectionInt64() { TestConstructorArrayReflection<Int64>(); }
        [Fact]
        public void ConstructorArrayReflectionSingle() { TestConstructorArrayReflection<Single>(); }
        [Fact]
        public void ConstructorArrayReflectionDouble() { TestConstructorArrayReflection<Double>(); }
        private void TestConstructorArrayReflection<T>() where T : struct
        {
            ConstructorInfo constructor = typeof(Vector<T>).GetTypeInfo().DeclaredConstructors
                .Where(ci => ci.GetParameters().Select(pi => pi.ParameterType).SequenceEqual(new Type[] { typeof(T[]) }))
                .Single();
            T[] values = GenerateRandomValuesForVector<T>();
            Vector<T> vec = (Vector<T>)constructor.Invoke(new object[] { values });
            ValidateVector(vec, (index, value) =>
                {
                    for (int g = 0; g < Vector<T>.Count; g++)
                    {
                        Assert.Equal(values[g], vec[g]);
                    }
                });
        }

        [Fact]
        public void CopyToReflectionByte() { TestCopyToReflection<Byte>(); }
        [Fact]
        public void CopyToReflectionSByte() { TestCopyToReflection<SByte>(); }
        [Fact]
        public void CopyToReflectionUInt16() { TestCopyToReflection<UInt16>(); }
        [Fact]
        public void CopyToReflectionInt16() { TestCopyToReflection<Int16>(); }
        [Fact]
        public void CopyToReflectionUInt32() { TestCopyToReflection<UInt32>(); }
        [Fact]
        public void CopyToReflectionInt32() { TestCopyToReflection<Int32>(); }
        [Fact]
        public void CopyToReflectionUInt64() { TestCopyToReflection<UInt64>(); }
        [Fact]
        public void CopyToReflectionInt64() { TestCopyToReflection<Int64>(); }
        [Fact]
        public void CopyToReflectionSingle() { TestCopyToReflection<Single>(); }
        [Fact]
        public void CopyToReflectionDouble() { TestCopyToReflection<Double>(); }
        private void TestCopyToReflection<T>() where T : struct
        {
            MethodInfo copyToMethod = typeof(Vector<T>).GetTypeInfo().GetDeclaredMethods("CopyTo")
                .Where(mi => mi.GetParameters().Select(pi => pi.ParameterType).SequenceEqual(new Type[] { typeof(T[]) }))
                .Single();
            T[] values = GenerateRandomValuesForVector<T>();
            Vector<T> vector = new Vector<T>(values);
            T[] array = new T[Vector<T>.Count];
            copyToMethod.Invoke(vector, new object[] { array });
            for (int g = 0; g < array.Length; g++)
            {
                Assert.Equal(values[g], array[g]);
                Assert.Equal(vector[g], array[g]);
            }
        }

        [Fact]
        public void CopyToWithOffsetReflectionByte() { TestCopyToWithOffsetReflection<Byte>(); }
        [Fact]
        public void CopyToWithOffsetReflectionSByte() { TestCopyToWithOffsetReflection<SByte>(); }
        [Fact]
        public void CopyToWithOffsetReflectionUInt16() { TestCopyToWithOffsetReflection<UInt16>(); }
        [Fact]
        public void CopyToWithOffsetReflectionInt16() { TestCopyToWithOffsetReflection<Int16>(); }
        [Fact]
        public void CopyToWithOffsetReflectionUInt32() { TestCopyToWithOffsetReflection<UInt32>(); }
        [Fact]
        public void CopyToWithOffsetReflectionInt32() { TestCopyToWithOffsetReflection<Int32>(); }
        [Fact]
        public void CopyToWithOffsetReflectionUInt64() { TestCopyToWithOffsetReflection<UInt64>(); }
        [Fact]
        public void CopyToWithOffsetReflectionInt64() { TestCopyToWithOffsetReflection<Int64>(); }
        [Fact]
        public void CopyToWithOffsetReflectionSingle() { TestCopyToWithOffsetReflection<Single>(); }
        [Fact]
        public void CopyToWithOffsetReflectionDouble() { TestCopyToWithOffsetReflection<Double>(); }
        private void TestCopyToWithOffsetReflection<T>() where T : struct
        {
            MethodInfo copyToMethod = typeof(Vector<T>).GetTypeInfo().GetDeclaredMethods("CopyTo")
                .Where(mi => mi.GetParameters().Select(pi => pi.ParameterType).SequenceEqual(new Type[] { typeof(T[]), typeof(int) }))
                .Single();
            T[] values = GenerateRandomValuesForVector<T>();
            Vector<T> vector = new Vector<T>(values);
            int offset = Util.GenerateSingleValue<int>();
            T[] array = new T[Vector<T>.Count + offset];
            copyToMethod.Invoke(vector, new object[] { array, offset });
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                Assert.Equal(values[g], array[g + offset]);
                Assert.Equal(vector[g], array[g + offset]);
            }
        }

        [Fact]
        public void CountViaReflectionConsistencyByte() { TestCountViaReflectionConsistency<Byte>(); }
        [Fact]
        public void CountViaReflectionConsistencySByte() { TestCountViaReflectionConsistency<SByte>(); }
        [Fact]
        public void CountViaReflectionConsistencyUInt16() { TestCountViaReflectionConsistency<UInt16>(); }
        [Fact]
        public void CountViaReflectionConsistencyInt16() { TestCountViaReflectionConsistency<Int16>(); }
        [Fact]
        public void CountViaReflectionConsistencyUInt32() { TestCountViaReflectionConsistency<UInt32>(); }
        [Fact]
        public void CountViaReflectionConsistencyInt32() { TestCountViaReflectionConsistency<Int32>(); }
        [Fact]
        public void CountViaReflectionConsistencyUInt64() { TestCountViaReflectionConsistency<UInt64>(); }
        [Fact]
        public void CountViaReflectionConsistencyInt64() { TestCountViaReflectionConsistency<Int64>(); }
        [Fact]
        public void CountViaReflectionConsistencySingle() { TestCountViaReflectionConsistency<Single>(); }
        [Fact]
        public void CountViaReflectionConsistencyDouble() { TestCountViaReflectionConsistency<Double>(); }
        private void TestCountViaReflectionConsistency<T>() where T : struct
        {
            MethodInfo countMethod = typeof(Vector<T>).GetTypeInfo().GetDeclaredProperty("Count").GetMethod;
            int valueFromReflection = (int)countMethod.Invoke(null, null);
            int valueFromNormalCall = Vector<T>.Count;
            Assert.Equal(valueFromNormalCall, valueFromReflection);
        }
        #endregion Reflection Tests

        #region Same-Size Conversions
        [Fact]
        public void ConvertInt32ToSingle()
        {
            Int32[] source = GenerateRandomValuesForVector<Int32>();
            Vector<Int32> sourceVec = new Vector<Int32>(source);
            Vector<Single> targetVec = Vector.ConvertToSingle(sourceVec);
            for (int i = 0; i < Vector<Single>.Count; i++)
            {
                Assert.Equal((Single)source[i], targetVec[i]);
            }
        }

        [Fact]
        public void ConvertUInt32ToSingle()
        {
            UInt32[] source = GenerateRandomValuesForVector<UInt32>();
            Vector<UInt32> sourceVec = new Vector<UInt32>(source);
            Vector<Single> targetVec = Vector.ConvertToSingle(sourceVec);
            for (int i = 0; i < Vector<Single>.Count; i++)
            {
                Assert.Equal((Single)source[i], targetVec[i]);
            }
        }

        [Fact]
        public void ConvertInt64ToDouble()
        {
            Int64[] source = GenerateRandomValuesForVector<Int64>();
            Vector<Int64> sourceVec = new Vector<Int64>(source);
            Vector<Double> targetVec = Vector.ConvertToDouble(sourceVec);
            for (int i = 0; i < Vector<Double>.Count; i++)
            {
                Assert.Equal((Double)source[i], targetVec[i]);
            }
        }

        [Fact]
        public void ConvertUInt64ToDouble()
        {
            UInt64[] source = GenerateRandomValuesForVector<UInt64>();
            Vector<UInt64> sourceVec = new Vector<UInt64>(source);
            Vector<Double> targetVec = Vector.ConvertToDouble(sourceVec);
            for (int i = 0; i < Vector<Double>.Count; i++)
            {
                Assert.Equal((Double)source[i], targetVec[i]);
            }
        }

        [Fact]
        public void ConvertSingleToInt32()
        {
            Single[] source = GenerateRandomValuesForVector<Single>();
            Vector<Single> sourceVec = new Vector<Single>(source);
            Vector<Int32> targetVec = Vector.ConvertToInt32(sourceVec);
            for (int i = 0; i < Vector<Int32>.Count; i++)
            {
                Assert.Equal((Int32)source[i], targetVec[i]);
            }
        }

        [Fact]
        public void ConvertSingleToUInt32()
        {
            Single[] source = GenerateRandomValuesForVector<Single>();
            Vector<Single> sourceVec = new Vector<Single>(source);
            Vector<UInt32> targetVec = Vector.ConvertToUInt32(sourceVec);
            for (int i = 0; i < Vector<UInt32>.Count; i++)
            {
                Assert.Equal((UInt32)source[i], targetVec[i]);
            }
        }

        [Fact]
        public void ConvertDoubleToInt64()
        {
            Double[] source = GenerateRandomValuesForVector<Double>();
            Vector<Double> sourceVec = new Vector<Double>(source);
            Vector<Int64> targetVec = Vector.ConvertToInt64(sourceVec);
            for (int i = 0; i < Vector<Int64>.Count; i++)
            {
                Assert.Equal((Int64)source[i], targetVec[i]);
            }
        }

        [Fact]
        public void ConvertDoubleToUInt64()
        {
            Double[] source = GenerateRandomValuesForVector<Double>();
            Vector<Double> sourceVec = new Vector<Double>(source);
            Vector<UInt64> targetVec = Vector.ConvertToUInt64(sourceVec);
            for (int i = 0; i < Vector<UInt64>.Count; i++)
            {
                Assert.Equal((UInt64)source[i], targetVec[i]);
            }
        }

        #endregion Same-Size Conversions

        #region Narrow / Widen
        [Fact]
        public void WidenByte()
        {
            Byte[] source = GenerateRandomValuesForVector<Byte>();
            Vector<Byte> sourceVec = new Vector<Byte>(source);
            Vector<UInt16> dest1;
            Vector<UInt16> dest2;
            Vector.Widen(sourceVec, out dest1, out dest2);
            ValidateVector(dest1, (index, val) =>
            {
                Assert.Equal((UInt16)source[index], val);
            });

            ValidateVector(dest2, (index, val) =>
            {
                Assert.Equal((UInt16)source[index + Vector<UInt16>.Count], val);
            });
        }

        [Fact]
        public void WidenUInt16()
        {
            UInt16[] source = GenerateRandomValuesForVector<UInt16>();
            Vector<UInt16> sourceVec = new Vector<UInt16>(source);
            Vector<UInt32> dest1;
            Vector<UInt32> dest2;
            Vector.Widen(sourceVec, out dest1, out dest2);
            ValidateVector(dest1, (index, val) =>
            {
                Assert.Equal((UInt32)source[index], val);
            });

            ValidateVector(dest2, (index, val) =>
            {
                Assert.Equal((UInt32)source[index + Vector<UInt32>.Count], val);
            });
        }

        [Fact]
        public void WidenUInt32()
        {
            UInt32[] source = GenerateRandomValuesForVector<UInt32>();
            Vector<UInt32> sourceVec = new Vector<UInt32>(source);
            Vector<UInt64> dest1;
            Vector<UInt64> dest2;
            Vector.Widen(sourceVec, out dest1, out dest2);
            ValidateVector(dest1, (index, val) =>
            {
                Assert.Equal((UInt64)source[index], val);
            });

            ValidateVector(dest2, (index, val) =>
            {
                Assert.Equal((UInt64)source[index + Vector<UInt64>.Count], val);
            });
        }

        [Fact]
        public void WidenSByte()
        {
            SByte[] source = GenerateRandomValuesForVector<SByte>();
            Vector<SByte> sourceVec = new Vector<SByte>(source);
            Vector<Int16> dest1;
            Vector<Int16> dest2;
            Vector.Widen(sourceVec, out dest1, out dest2);
            ValidateVector(dest1, (index, val) =>
            {
                Assert.Equal((Int16)source[index], val);
            });

            ValidateVector(dest2, (index, val) =>
            {
                Assert.Equal((Int16)source[index + Vector<Int16>.Count], val);
            });
        }

        [Fact]
        public void WidenInt16()
        {
            Int16[] source = GenerateRandomValuesForVector<Int16>();
            Vector<Int16> sourceVec = new Vector<Int16>(source);
            Vector<Int32> dest1;
            Vector<Int32> dest2;
            Vector.Widen(sourceVec, out dest1, out dest2);
            ValidateVector(dest1, (index, val) =>
            {
                Assert.Equal((Int32)source[index], val);
            });

            ValidateVector(dest2, (index, val) =>
            {
                Assert.Equal((Int32)source[index + Vector<Int32>.Count], val);
            });
        }

        [Fact]
        public void WidenInt32()
        {
            Int32[] source = GenerateRandomValuesForVector<Int32>();
            Vector<Int32> sourceVec = new Vector<Int32>(source);
            Vector<Int64> dest1;
            Vector<Int64> dest2;
            Vector.Widen(sourceVec, out dest1, out dest2);
            ValidateVector(dest1, (index, val) =>
            {
                Assert.Equal((Int64)source[index], val);
            });

            ValidateVector(dest2, (index, val) =>
            {
                Assert.Equal((Int64)source[index + Vector<Int64>.Count], val);
            });
        }

        [Fact]
        public void WidenSingle()
        {
            Single[] source = GenerateRandomValuesForVector<Single>();
            Vector<Single> sourceVec = new Vector<Single>(source);
            Vector<Double> dest1;
            Vector<Double> dest2;
            Vector.Widen(sourceVec, out dest1, out dest2);
            ValidateVector(dest1, (index, val) =>
            {
                Assert.Equal((Double)source[index], val);
            });

            ValidateVector(dest2, (index, val) =>
            {
                Assert.Equal((Double)source[index + Vector<Double>.Count], val);
            });
        }


        [Fact]
        public void NarrowUInt16()
        {
            UInt16[] source1 = GenerateRandomValuesForVector<UInt16>();
            UInt16[] source2 = GenerateRandomValuesForVector<UInt16>();
            Vector<UInt16> sourceVec1 = new Vector<UInt16>(source1);
            Vector<UInt16> sourceVec2 = new Vector<UInt16>(source2);
            Vector<Byte> dest = Vector.Narrow(sourceVec1, sourceVec2);

            for (int i = 0; i < Vector<UInt16>.Count; i++)
            {
                Assert.Equal((Byte)source1[i], dest[i]);
            }
            for (int i = 0; i < Vector<UInt16>.Count; i++)
            {
                Assert.Equal((Byte)source2[i], dest[i + Vector<UInt16>.Count]);
            }
        }

        [Fact]
        public void NarrowUInt32()
        {
            UInt32[] source1 = GenerateRandomValuesForVector<UInt32>();
            UInt32[] source2 = GenerateRandomValuesForVector<UInt32>();
            Vector<UInt32> sourceVec1 = new Vector<UInt32>(source1);
            Vector<UInt32> sourceVec2 = new Vector<UInt32>(source2);
            Vector<UInt16> dest = Vector.Narrow(sourceVec1, sourceVec2);

            for (int i = 0; i < Vector<UInt32>.Count; i++)
            {
                Assert.Equal((UInt16)source1[i], dest[i]);
            }
            for (int i = 0; i < Vector<UInt32>.Count; i++)
            {
                Assert.Equal((UInt16)source2[i], dest[i + Vector<UInt32>.Count]);
            }
        }

        [Fact]
        public void NarrowUInt64()
        {
            UInt64[] source1 = GenerateRandomValuesForVector<UInt64>();
            UInt64[] source2 = GenerateRandomValuesForVector<UInt64>();
            Vector<UInt64> sourceVec1 = new Vector<UInt64>(source1);
            Vector<UInt64> sourceVec2 = new Vector<UInt64>(source2);
            Vector<UInt32> dest = Vector.Narrow(sourceVec1, sourceVec2);

            for (int i = 0; i < Vector<UInt64>.Count; i++)
            {
                Assert.Equal((UInt32)source1[i], dest[i]);
            }
            for (int i = 0; i < Vector<UInt64>.Count; i++)
            {
                Assert.Equal((UInt32)source2[i], dest[i + Vector<UInt64>.Count]);
            }
        }

        [Fact]
        public void NarrowInt16()
        {
            Int16[] source1 = GenerateRandomValuesForVector<Int16>();
            Int16[] source2 = GenerateRandomValuesForVector<Int16>();
            Vector<Int16> sourceVec1 = new Vector<Int16>(source1);
            Vector<Int16> sourceVec2 = new Vector<Int16>(source2);
            Vector<SByte> dest = Vector.Narrow(sourceVec1, sourceVec2);

            for (int i = 0; i < Vector<Int16>.Count; i++)
            {
                Assert.Equal((SByte)source1[i], dest[i]);
            }
            for (int i = 0; i < Vector<Int16>.Count; i++)
            {
                Assert.Equal((SByte)source2[i], dest[i + Vector<Int16>.Count]);
            }
        }

        [Fact]
        public void NarrowInt32()
        {
            Int32[] source1 = GenerateRandomValuesForVector<Int32>();
            Int32[] source2 = GenerateRandomValuesForVector<Int32>();
            Vector<Int32> sourceVec1 = new Vector<Int32>(source1);
            Vector<Int32> sourceVec2 = new Vector<Int32>(source2);
            Vector<Int16> dest = Vector.Narrow(sourceVec1, sourceVec2);

            for (int i = 0; i < Vector<Int32>.Count; i++)
            {
                Assert.Equal((Int16)source1[i], dest[i]);
            }
            for (int i = 0; i < Vector<Int32>.Count; i++)
            {
                Assert.Equal((Int16)source2[i], dest[i + Vector<Int32>.Count]);
            }
        }

        [Fact]
        public void NarrowInt64()
        {
            Int64[] source1 = GenerateRandomValuesForVector<Int64>();
            Int64[] source2 = GenerateRandomValuesForVector<Int64>();
            Vector<Int64> sourceVec1 = new Vector<Int64>(source1);
            Vector<Int64> sourceVec2 = new Vector<Int64>(source2);
            Vector<Int32> dest = Vector.Narrow(sourceVec1, sourceVec2);

            for (int i = 0; i < Vector<Int64>.Count; i++)
            {
                Assert.Equal((Int32)source1[i], dest[i]);
            }
            for (int i = 0; i < Vector<Int64>.Count; i++)
            {
                Assert.Equal((Int32)source2[i], dest[i + Vector<Int64>.Count]);
            }
        }

        [Fact]
        public void NarrowDouble()
        {
            Double[] source1 = GenerateRandomValuesForVector<Double>();
            Double[] source2 = GenerateRandomValuesForVector<Double>();
            Vector<Double> sourceVec1 = new Vector<Double>(source1);
            Vector<Double> sourceVec2 = new Vector<Double>(source2);
            Vector<Single> dest = Vector.Narrow(sourceVec1, sourceVec2);

            for (int i = 0; i < Vector<Double>.Count; i++)
            {
                Assert.Equal((Single)source1[i], dest[i]);
            }
            for (int i = 0; i < Vector<Double>.Count; i++)
            {
                Assert.Equal((Single)source2[i], dest[i + Vector<Double>.Count]);
            }
        }

        #endregion Narrow / Widen

        #region Helper Methods
        private static void ValidateVector<T>(Vector<T> vector, Action<int, T> indexValidationFunc) where T : struct
        {
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                indexValidationFunc(g, vector[g]);
            }
        }

        internal static T[] GenerateRandomValuesForVector<T>() where T : struct
        {
            int minValue = GetMinValue<T>();
            int maxValue = GetMaxValue<T>();
            return Util.GenerateRandomValues<T>(Vector<T>.Count, minValue, maxValue);
        }

        internal static int GetMinValue<T>() where T : struct
        {
            if (typeof(T) == typeof(Int64) || typeof(T) == typeof(Single) || typeof(T) == typeof(Double) || typeof(T) == typeof(UInt32) || typeof(T) == typeof(UInt64))
            {
                return int.MinValue;
            }
            var typeInfo = typeof(T).GetTypeInfo();
            var field = typeInfo.GetDeclaredField("MinValue");
            var value = field.GetValue(null);
            return (int)(dynamic)value;
        }

        internal static int GetMaxValue<T>() where T : struct
        {
            if (typeof(T) == typeof(Int64) || typeof(T) == typeof(Single) || typeof(T) == typeof(Double) || typeof(T) == typeof(UInt32) || typeof(T) == typeof(UInt64))
            {
                return int.MaxValue;
            }
            var typeInfo = typeof(T).GetTypeInfo();
            var field = typeInfo.GetDeclaredField("MaxValue");
            var value = field.GetValue(null);
            return (int)(dynamic)value;
        }

        internal static T GetValueWithAllOnesSet<T>() where T : struct
        {
            if (typeof(T) == typeof(Byte))
            {
                return (T)(object)ConstantHelper.GetByteWithAllBitsSet();
            }
            else if (typeof(T) == typeof(SByte))
            {
                return (T)(object)ConstantHelper.GetSByteWithAllBitsSet();
            }
            else if (typeof(T) == typeof(UInt16))
            {
                return (T)(object)ConstantHelper.GetUInt16WithAllBitsSet();
            }
            else if (typeof(T) == typeof(Int16))
            {
                return (T)(object)ConstantHelper.GetInt16WithAllBitsSet();
            }
            else if (typeof(T) == typeof(Int32))
            {
                return (T)(object)ConstantHelper.GetInt32WithAllBitsSet();
            }
            else if (typeof(T) == typeof(Int64))
            {
                return (T)(object)ConstantHelper.GetInt64WithAllBitsSet();
            }
            else if (typeof(T) == typeof(Single))
            {
                return (T)(object)ConstantHelper.GetSingleWithAllBitsSet();
            }
            else if (typeof(T) == typeof(Double))
            {
                return (T)(object)ConstantHelper.GetDoubleWithAllBitsSet();
            }
            else if (typeof(T) == typeof(UInt32))
            {
                return (T)(object)ConstantHelper.GetUInt32WithAllBitsSet();
            }
            else if (typeof(T) == typeof(UInt64))
            {
                return (T)(object)ConstantHelper.GetUInt64WithAllBitsSet();
            }
            throw new NotSupportedException();
        }
        #endregion
    }
}