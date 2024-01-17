// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using Xunit;
#if NETCOREAPP
using System.Runtime.CompilerServices;
#endif

namespace System.Tests
{
    public class ValueTupleTests
    {
        private class ValueTupleTestDriver<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
        {
            private int _nItems;

            private readonly object valueTuple;
            private readonly ValueTuple valueTuple0;
            private readonly ValueTuple<T1> valueTuple1;
            private readonly ValueTuple<T1, T2> valueTuple2;
            private readonly ValueTuple<T1, T2, T3> valueTuple3;
            private readonly ValueTuple<T1, T2, T3, T4> valueTuple4;
            private readonly ValueTuple<T1, T2, T3, T4, T5> valueTuple5;
            private readonly ValueTuple<T1, T2, T3, T4, T5, T6> valueTuple6;
            private readonly ValueTuple<T1, T2, T3, T4, T5, T6, T7> valueTuple7;
            private readonly ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>> valueTuple8;
            private readonly ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9>> valueTuple9;
            private readonly ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10>> valueTuple10;

            internal ValueTupleTestDriver(params object[] values)
            {
                if (values.Length > 10)
                    throw new ArgumentOutOfRangeException("values", "You must provide at most 10 values");

                _nItems = values.Length;
                switch (_nItems)
                {
                    case 0:
                        valueTuple0 = ValueTuple.Create();
                        valueTuple = valueTuple0;
                        break;
                    case 1:
                        valueTuple1 = ValueTuple.Create((T1)values[0]);
                        valueTuple = valueTuple1;
                        break;
                    case 2:
                        valueTuple2 = ValueTuple.Create((T1)values[0], (T2)values[1]);
                        valueTuple = valueTuple2;
                        break;
                    case 3:
                        valueTuple3 = ValueTuple.Create((T1)values[0], (T2)values[1], (T3)values[2]);
                        valueTuple = valueTuple3;
                        break;
                    case 4:
                        valueTuple4 = ValueTuple.Create((T1)values[0], (T2)values[1], (T3)values[2], (T4)values[3]);
                        valueTuple = valueTuple4;
                        break;
                    case 5:
                        valueTuple5 = ValueTuple.Create((T1)values[0], (T2)values[1], (T3)values[2], (T4)values[3], (T5)values[4]);
                        valueTuple = valueTuple5;
                        break;
                    case 6:
                        valueTuple6 = ValueTuple.Create((T1)values[0], (T2)values[1], (T3)values[2], (T4)values[3],
                            (T5)values[4], (T6)values[5]);
                        valueTuple = valueTuple6;
                        break;
                    case 7:
                        valueTuple7 = ValueTuple.Create((T1)values[0], (T2)values[1], (T3)values[2], (T4)values[3],
                            (T5)values[4], (T6)values[5], (T7)values[6]);
                        valueTuple = valueTuple7;
                        break;
                    case 8:
                        valueTuple8 = ValueTuple.Create((T1)values[0], (T2)values[1], (T3)values[2], (T4)values[3],
                            (T5)values[4], (T6)values[5], (T7)values[6], (T8)values[7]);
                        valueTuple = valueTuple8;
                        break;
                    case 9:
                        valueTuple9 = new ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9>>((T1)values[0], (T2)values[1], (T3)values[2], (T4)values[3], (T5)values[4], (T6)values[5], (T7)values[6], new ValueTuple<T8, T9>((T8)values[7], (T9)values[8]));
                        valueTuple = valueTuple9;
                        break;
                    case 10:
                        valueTuple10 = new ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10>>((T1)values[0], (T2)values[1], (T3)values[2], (T4)values[3], (T5)values[4], (T6)values[5], (T7)values[6], new ValueTuple<T8, T9, T10>((T8)values[7], (T9)values[8], (T10)values[9]));
                        valueTuple = valueTuple10;
                        break;
                }
            }

            private void VerifyItem(int itemPos, object Item1, object Item2)
            {
                Assert.True(object.Equals(Item1, Item2));
            }

            public void TestConstructor(params object[] expectedValue)
            {
                if (expectedValue.Length != _nItems)
                    throw new ArgumentOutOfRangeException("expectedValues", "You must provide " + _nItems + " expectedvalues");

                switch (_nItems)
                {
                    case 0:
                        break;
                    case 1:
                        VerifyItem(1, valueTuple1.Item1, expectedValue[0]);
                        break;
                    case 2:
                        VerifyItem(1, valueTuple2.Item1, expectedValue[0]);
                        VerifyItem(2, valueTuple2.Item2, expectedValue[1]);
                        break;
                    case 3:
                        VerifyItem(1, valueTuple3.Item1, expectedValue[0]);
                        VerifyItem(2, valueTuple3.Item2, expectedValue[1]);
                        VerifyItem(3, valueTuple3.Item3, expectedValue[2]);
                        break;
                    case 4:
                        VerifyItem(1, valueTuple4.Item1, expectedValue[0]);
                        VerifyItem(2, valueTuple4.Item2, expectedValue[1]);
                        VerifyItem(3, valueTuple4.Item3, expectedValue[2]);
                        VerifyItem(4, valueTuple4.Item4, expectedValue[3]);
                        break;
                    case 5:
                        VerifyItem(1, valueTuple5.Item1, expectedValue[0]);
                        VerifyItem(2, valueTuple5.Item2, expectedValue[1]);
                        VerifyItem(3, valueTuple5.Item3, expectedValue[2]);
                        VerifyItem(4, valueTuple5.Item4, expectedValue[3]);
                        VerifyItem(5, valueTuple5.Item5, expectedValue[4]);
                        break;
                    case 6:
                        VerifyItem(1, valueTuple6.Item1, expectedValue[0]);
                        VerifyItem(2, valueTuple6.Item2, expectedValue[1]);
                        VerifyItem(3, valueTuple6.Item3, expectedValue[2]);
                        VerifyItem(4, valueTuple6.Item4, expectedValue[3]);
                        VerifyItem(5, valueTuple6.Item5, expectedValue[4]);
                        VerifyItem(6, valueTuple6.Item6, expectedValue[5]);
                        break;
                    case 7:
                        VerifyItem(1, valueTuple7.Item1, expectedValue[0]);
                        VerifyItem(2, valueTuple7.Item2, expectedValue[1]);
                        VerifyItem(3, valueTuple7.Item3, expectedValue[2]);
                        VerifyItem(4, valueTuple7.Item4, expectedValue[3]);
                        VerifyItem(5, valueTuple7.Item5, expectedValue[4]);
                        VerifyItem(6, valueTuple7.Item6, expectedValue[5]);
                        VerifyItem(7, valueTuple7.Item7, expectedValue[6]);
                        break;
                    case 8: // Extended ValueTuple
                        VerifyItem(1, valueTuple8.Item1, expectedValue[0]);
                        VerifyItem(2, valueTuple8.Item2, expectedValue[1]);
                        VerifyItem(3, valueTuple8.Item3, expectedValue[2]);
                        VerifyItem(4, valueTuple8.Item4, expectedValue[3]);
                        VerifyItem(5, valueTuple8.Item5, expectedValue[4]);
                        VerifyItem(6, valueTuple8.Item6, expectedValue[5]);
                        VerifyItem(7, valueTuple8.Item7, expectedValue[6]);
                        VerifyItem(8, valueTuple8.Rest.Item1, expectedValue[7]);
                        break;
                    case 9: // Extended ValueTuple
                        VerifyItem(1, valueTuple9.Item1, expectedValue[0]);
                        VerifyItem(2, valueTuple9.Item2, expectedValue[1]);
                        VerifyItem(3, valueTuple9.Item3, expectedValue[2]);
                        VerifyItem(4, valueTuple9.Item4, expectedValue[3]);
                        VerifyItem(5, valueTuple9.Item5, expectedValue[4]);
                        VerifyItem(6, valueTuple9.Item6, expectedValue[5]);
                        VerifyItem(7, valueTuple9.Item7, expectedValue[6]);
                        VerifyItem(8, valueTuple9.Rest.Item1, expectedValue[7]);
                        VerifyItem(9, valueTuple9.Rest.Item2, expectedValue[8]);
                        break;
                    case 10: // Extended ValueTuple
                        VerifyItem(1, valueTuple10.Item1, expectedValue[0]);
                        VerifyItem(2, valueTuple10.Item2, expectedValue[1]);
                        VerifyItem(3, valueTuple10.Item3, expectedValue[2]);
                        VerifyItem(4, valueTuple10.Item4, expectedValue[3]);
                        VerifyItem(5, valueTuple10.Item5, expectedValue[4]);
                        VerifyItem(6, valueTuple10.Item6, expectedValue[5]);
                        VerifyItem(7, valueTuple10.Item7, expectedValue[6]);
                        VerifyItem(8, valueTuple10.Rest.Item1, expectedValue[7]);
                        VerifyItem(9, valueTuple10.Rest.Item2, expectedValue[8]);
                        VerifyItem(10, valueTuple10.Rest.Item3, expectedValue[9]);
                        break;
                    default:
                        throw new ArgumentException("Must specify between 0 and 10 expected values (inclusive).");
                }
            }

            public void TestToString(string expected)
            {
                Assert.Equal(expected, valueTuple.ToString());
            }

            public void TestEquals_GetHashCode(ValueTupleTestDriver<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> other, bool expectEqual, bool expectStructuallyEqual)
            {
                if (expectEqual)
                {
                    Assert.True(valueTuple.Equals(other.valueTuple));
                    Assert.Equal(valueTuple.GetHashCode(), other.valueTuple.GetHashCode());
                }
                else
                {
                    Assert.False(valueTuple.Equals(other.valueTuple));
                    Assert.NotEqual(valueTuple.GetHashCode(), other.valueTuple.GetHashCode());
                }

                if (expectStructuallyEqual)
                {
                    var equatable = ((IStructuralEquatable)valueTuple);
                    var otherEquatable = ((IStructuralEquatable)other.valueTuple);
                    Assert.True(equatable.Equals(other.valueTuple, TestEqualityComparer.Instance));
                    Assert.Equal(equatable.GetHashCode(TestEqualityComparer.Instance), otherEquatable.GetHashCode(TestEqualityComparer.Instance));
                }
                else
                {
                    var equatable = ((IStructuralEquatable)valueTuple);
                    var otherEquatable = ((IStructuralEquatable)other.valueTuple);
                    Assert.False(equatable.Equals(other.valueTuple, TestEqualityComparer.Instance));
                    Assert.NotEqual(equatable.GetHashCode(TestEqualityComparer.Instance), otherEquatable.GetHashCode(TestEqualityComparer.Instance));
                }

                Assert.False(valueTuple.Equals(null));
                Assert.False(((IStructuralEquatable)valueTuple).Equals(null));
                IStructuralEquatable_Equals_NullComparer_ThrowsNullReferenceException();
                IStructuralEquatable_GetHashCode_NullComparer_ThrowsNullReferenceException();
            }

            public void IStructuralEquatable_Equals_NullComparer_ThrowsNullReferenceException()
            {
                // This was not fixed in order to be compatible with the .NET Framework and Xamarin.
                // https://github.com/dotnet/runtime/issues/19275
                IStructuralEquatable equatable = (IStructuralEquatable)valueTuple;
                if (valueTuple is ValueTuple)
                {
                    Assert.True(equatable.Equals(valueTuple, null));
                }
                else
                {
                    Assert.Throws<NullReferenceException>(() => equatable.Equals(valueTuple, null));
                }
            }

            public void IStructuralEquatable_GetHashCode_NullComparer_ThrowsNullReferenceException()
            {
                // This was not fixed in order to be compatible with the .NET Framework and Xamarin.
                // https://github.com/dotnet/runtime/issues/19275
                IStructuralEquatable equatable = (IStructuralEquatable)valueTuple;
                if (valueTuple is ValueTuple)
                {
                    Assert.Equal(0, valueTuple.GetHashCode());
                }
                else
                {
                    Assert.Throws<NullReferenceException>(() => equatable.GetHashCode(null));
                }
            }

            public void TestCompareTo(ValueTupleTestDriver<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> other, int expectedResult, int expectedStructuralResult)
            {
                Assert.Equal(expectedResult, ((IComparable)valueTuple).CompareTo(other.valueTuple));
                Assert.Equal(expectedStructuralResult, ((IStructuralComparable)valueTuple).CompareTo(other.valueTuple, DummyTestComparer.Instance));
                Assert.Equal(1, ((IComparable)valueTuple).CompareTo(null));

                IStructuralComparable_NullComparer_ThrowsNullReferenceException();
            }

            public void IStructuralComparable_NullComparer_ThrowsNullReferenceException()
            {
                // This was not fixed in order to be compatible with the .NET Framework and Xamarin.
                // https://github.com/dotnet/runtime/issues/19275
                IStructuralComparable comparable = (IStructuralComparable)valueTuple;
                if (valueTuple is ValueTuple)
                {
                    Assert.Equal(0, comparable.CompareTo(valueTuple, null));
                }
                else
                {
                    Assert.Throws<NullReferenceException>(() => comparable.CompareTo(valueTuple, null));
                }
            }

            public void TestNotEqual()
            {
                ValueTuple<int> ValueTupleB = new ValueTuple<int>((int)10000);
                Assert.NotEqual(valueTuple, ValueTupleB);
            }

            internal void TestCompareToThrows()
            {
                ValueTuple<int> ValueTupleB = new ValueTuple<int>((int)10000);
                AssertExtensions.Throws<ArgumentException>("other", () => ((IComparable)valueTuple).CompareTo(ValueTupleB));
            }
        }

        [Fact]
        public static void TestConstructor()
        {
            ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan> ValueTupleDriverA;
            //ValueTuple-0
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>();
            ValueTupleDriverA.TestConstructor();

            //ValueTuple-1
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>(short.MaxValue);
            ValueTupleDriverA.TestConstructor(short.MaxValue);

            //ValueTuple-2
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>(short.MinValue, int.MaxValue);
            ValueTupleDriverA.TestConstructor(short.MinValue, int.MaxValue);

            //ValueTuple-3
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)0, (int)0, long.MaxValue);
            ValueTupleDriverA.TestConstructor((short)0, (int)0, long.MaxValue);

            //ValueTuple-4
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)1, (int)1, long.MinValue, "This");
            ValueTupleDriverA.TestConstructor((short)1, (int)1, long.MinValue, "This");

            //ValueTuple-5
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)(-1), (int)(-1), (long)0, "is", 'A');
            ValueTupleDriverA.TestConstructor((short)(-1), (int)(-1), (long)0, "is", 'A');

            //ValueTuple-6
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)10, (int)100, (long)1, "testing", 'Z', float.MaxValue);
            ValueTupleDriverA.TestConstructor((short)10, (int)100, (long)1, "testing", 'Z', float.MaxValue);

            //ValueTuple-7
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)(-100), (int)(-1000), (long)(-1), "ValueTuples", ' ', float.MinValue, double.MaxValue);
            ValueTupleDriverA.TestConstructor((short)(-100), (int)(-1000), (long)(-1), "ValueTuples", ' ', float.MinValue, double.MaxValue);

            object myObj = new object();
            //ValueTuple-10
            DateTime now = DateTime.Now;
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)10000, (int)1000000, (long)10000000, "2008?7?2?", '0', (float)0.0001, (double)0.0000001, now, ValueTuple.Create(false, myObj), TimeSpan.Zero);
            ValueTupleDriverA.TestConstructor((short)10000, (int)1000000, (long)10000000, "2008?7?2?", '0', (float)0.0001, (double)0.0000001, now, ValueTuple.Create(false, myObj), TimeSpan.Zero);
        }

        [Fact]
        public static void TestToString()
        {
            ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan> ValueTupleDriverA;
            //ValueTuple-0
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>();
            ValueTupleDriverA.TestToString("()");

            //ValueTuple-1
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>(short.MaxValue);
            ValueTupleDriverA.TestToString("(" + short.MaxValue + ")");

            //ValueTuple-2
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>(short.MinValue, int.MaxValue);
            ValueTupleDriverA.TestToString("(" + short.MinValue + ", " + int.MaxValue + ")");

            //ValueTuple-3
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)0, (int)0, long.MaxValue);
            ValueTupleDriverA.TestToString("(" + ((short)0) + ", " + ((int)0) + ", " + long.MaxValue + ")");

            //ValueTuple-4
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)1, (int)1, long.MinValue, "This");
            ValueTupleDriverA.TestConstructor((short)1, (int)1, long.MinValue, "This");
            ValueTupleDriverA.TestToString("(" + ((short)1) + ", " + ((int)1) + ", " + long.MinValue + ", This)");

            //ValueTuple-5
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)(-1), (int)(-1), (long)0, "is", 'A');
            ValueTupleDriverA.TestToString("(" + ((short)(-1)) + ", " + ((int)(-1)) + ", " + ((long)0) + ", is, A)");

            //ValueTuple-6
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)10, (int)100, (long)1, "testing", 'Z', float.MaxValue);
            ValueTupleDriverA.TestToString("(" + ((short)10) + ", " + ((int)100) + ", " + ((long)1) + ", testing, Z, " + float.MaxValue + ")");

            //ValueTuple-7
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)(-100), (int)(-1000), (long)(-1), "ValueTuples", ' ', float.MinValue, double.MaxValue);
            ValueTupleDriverA.TestToString("(" + ((short)(-100)) + ", " + ((int)(-1000)) + ", " + ((long)(-1)) + ", ValueTuples,  , " + float.MinValue + ", " + double.MaxValue + ")");

            object myObj = new object();
            //ValueTuple-10
            DateTime now = DateTime.Now;

            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)10000, (int)1000000, (long)10000000, "2008?7?2?", '0', (float)0.0001, (double)0.0000001, now, ValueTuple.Create(false, myObj), TimeSpan.Zero);
            // .NET Native bug 438149 - object.ToString in incorrect
            ValueTupleDriverA.TestToString("(" + ((short)10000) + ", " + ((int)1000000) + ", " + ((long)10000000) + ", 2008?7?2?, 0, " + ((float)0.0001) + ", " + ((double)0.0000001) + ", " + now + ", (False, System.Object), " + TimeSpan.Zero + ")");
        }

        [Fact]
        public static void TestEquals_GetHashCode()
        {
            ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan> ValueTupleDriverA, ValueTupleDriverB, ValueTupleDriverC, ValueTupleDriverD;
            //ValueTuple-0
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>();
            ValueTupleDriverB = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>();
            ValueTupleDriverD = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>(short.MaxValue, int.MaxValue);
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverB, true, true);
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverD, false, false);

            //ValueTuple-1
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>(short.MaxValue);
            ValueTupleDriverB = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>(short.MaxValue);
            ValueTupleDriverC = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>(short.MinValue);
            ValueTupleDriverD = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>(short.MaxValue, int.MaxValue);
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverB, true, true);
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverC, false, false);
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverD, false, false);

            //ValueTuple-2
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>(short.MinValue, int.MaxValue);
            ValueTupleDriverB = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>(short.MinValue, int.MaxValue);
            ValueTupleDriverC = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>(short.MinValue, int.MinValue);
            ValueTupleDriverD = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)(-1), (int)(-1), long.MinValue);
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverB, true, true);
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverC, false, false);
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverD, false, false);

            //ValueTuple-3
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)0, (int)0, long.MaxValue);
            ValueTupleDriverB = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)0, (int)0, long.MaxValue);
            ValueTupleDriverC = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)(-1), (int)(-1), long.MinValue);
            ValueTupleDriverD = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)1, (int)1, long.MinValue, "this");
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverB, true, true);
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverC, false, false);
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverD, false, false);

            //ValueTuple-4
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)1, (int)1, long.MinValue, "This");
            ValueTupleDriverB = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)1, (int)1, long.MinValue, "This");
            ValueTupleDriverC = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)1, (int)1, long.MinValue, "this");
            ValueTupleDriverD = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)0, (int)0, (long)1, "IS", 'a');
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverB, true, true);
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverC, false, false);
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverD, false, false);

            //ValueTuple-5
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)(-1), (int)(-1), (long)0, "is", 'A');
            ValueTupleDriverB = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)(-1), (int)(-1), (long)0, "is", 'A');
            ValueTupleDriverC = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)0, (int)0, (long)1, "IS", 'a');
            ValueTupleDriverD = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)10, (int)100, (long)1, "testing", 'Z', float.MinValue);
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverB, true, true);
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverC, false, false);
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverD, false, false);

            //ValueTuple-6
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)10, (int)100, (long)1, "testing", 'Z', float.MaxValue);
            ValueTupleDriverB = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)10, (int)100, (long)1, "testing", 'Z', float.MaxValue);
            ValueTupleDriverC = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)10, (int)100, (long)1, "testing", 'Z', float.MinValue);
            ValueTupleDriverD = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)(-101), (int)(-1001), (long)(-2), "ValueTuples", ' ', float.MinValue, (double)0.0);
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverB, true, true);
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverC, false, false);
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverD, false, false);

            //ValueTuple-7
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)(-100), (int)(-1000), (long)(-1), "ValueTuples", ' ', float.MinValue, double.MaxValue);
            ValueTupleDriverB = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)(-100), (int)(-1000), (long)(-1), "ValueTuples", ' ', float.MinValue, double.MaxValue);
            ValueTupleDriverC = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)(-101), (int)(-1001), (long)(-2), "ValueTuples", ' ', float.MinValue, (double)0.0);
            ValueTupleDriverD = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)10001, (int)1000001, (long)10000001, "2008?7?3?", '1', (float)0.0002, (double)0.0000002, DateTime.Now.AddMilliseconds(1));
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverB, true, true);
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverC, false, false);
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverD, false, false);

            //ValueTuple-8
            DateTime now = DateTime.Now;
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)(-100), (int)(-1000), (long)(-1), "ValueTuples", ' ', float.MinValue, double.MaxValue, now);
            ValueTupleDriverB = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)(-100), (int)(-1000), (long)(-1), "ValueTuples", ' ', float.MinValue, double.MaxValue, now);
            ValueTupleDriverC = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)(-101), (int)(-1001), (long)(-2), "ValueTuples", ' ', float.MinValue, (double)0.0, now.AddMilliseconds(1));
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverB, true, true);
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverC, false, false);

            object myObj = new object();
            //ValueTuple-10
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)10000, (int)1000000, (long)10000000, "2008?7?2?", '0', (float)0.0001, (double)0.0000001, now, ValueTuple.Create(false, myObj), TimeSpan.Zero);
            ValueTupleDriverB = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)10000, (int)1000000, (long)10000000, "2008?7?2?", '0', (float)0.0001, (double)0.0000001, now, ValueTuple.Create(false, myObj), TimeSpan.Zero);
            ValueTupleDriverC = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)10001, (int)1000001, (long)10000001, "2008?7?3?", '1', (float)0.0002, (double)0.0000002, now.AddMilliseconds(1), ValueTuple.Create(true, myObj), TimeSpan.MaxValue);
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverB, true, true);
            ValueTupleDriverA.TestEquals_GetHashCode(ValueTupleDriverC, false, false);
        }

        [Fact]
        public static void TestCompareTo()
        {
            ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan> ValueTupleDriverA, ValueTupleDriverB, ValueTupleDriverC;
            //ValueTuple-0
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>();
            ValueTupleDriverB = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>();
            ValueTupleDriverA.TestCompareTo(ValueTupleDriverB, 0, 0);

            //ValueTuple-1
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>(short.MaxValue);
            ValueTupleDriverB = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>(short.MaxValue);
            ValueTupleDriverC = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>(short.MinValue);
            ValueTupleDriverA.TestCompareTo(ValueTupleDriverB, 0, 5);
            ValueTupleDriverA.TestCompareTo(ValueTupleDriverC, 65535, 5);
            //ValueTuple-2
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>(short.MinValue, int.MaxValue);
            ValueTupleDriverB = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>(short.MinValue, int.MaxValue);
            ValueTupleDriverC = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>(short.MinValue, int.MinValue);
            ValueTupleDriverA.TestCompareTo(ValueTupleDriverB, 0, 5);
            ValueTupleDriverA.TestCompareTo(ValueTupleDriverC, 1, 5);
            //ValueTuple-3
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)0, (int)0, long.MaxValue);
            ValueTupleDriverB = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)0, (int)0, long.MaxValue);
            ValueTupleDriverC = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)(-1), (int)(-1), long.MinValue);
            ValueTupleDriverA.TestCompareTo(ValueTupleDriverB, 0, 5);
            ValueTupleDriverA.TestCompareTo(ValueTupleDriverC, 1, 5);
            //ValueTuple-4
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)1, (int)1, long.MinValue, "This");
            ValueTupleDriverB = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)1, (int)1, long.MinValue, "This");
            ValueTupleDriverC = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)1, (int)1, long.MinValue, "this");
            ValueTupleDriverA.TestCompareTo(ValueTupleDriverB, 0, 5);
            ValueTupleDriverA.TestCompareTo(ValueTupleDriverC, 1, 5);
            //ValueTuple-5
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)(-1), (int)(-1), (long)0, "is", 'A');
            ValueTupleDriverB = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)(-1), (int)(-1), (long)0, "is", 'A');
            ValueTupleDriverC = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)0, (int)0, (long)1, "IS", 'a');
            ValueTupleDriverA.TestCompareTo(ValueTupleDriverB, 0, 5);
            ValueTupleDriverA.TestCompareTo(ValueTupleDriverC, -1, 5);
            //ValueTuple-6
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)10, (int)100, (long)1, "testing", 'Z', float.MaxValue);
            ValueTupleDriverB = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)10, (int)100, (long)1, "testing", 'Z', float.MaxValue);
            ValueTupleDriverC = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)10, (int)100, (long)1, "testing", 'Z', float.MinValue);
            ValueTupleDriverA.TestCompareTo(ValueTupleDriverB, 0, 5);
            ValueTupleDriverA.TestCompareTo(ValueTupleDriverC, 1, 5);
            //ValueTuple-7
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)(-100), (int)(-1000), (long)(-1), "ValueTuples", ' ', float.MinValue, double.MaxValue);
            ValueTupleDriverB = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)(-100), (int)(-1000), (long)(-1), "ValueTuples", ' ', float.MinValue, double.MaxValue);
            ValueTupleDriverC = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)(-101), (int)(-1001), (long)(-2), "ValueTuples", ' ', float.MinValue, (double)0.0);
            ValueTupleDriverA.TestCompareTo(ValueTupleDriverB, 0, 5);
            ValueTupleDriverA.TestCompareTo(ValueTupleDriverC, 1, 5);

            object myObj = new object();
            //ValueTuple-10
            DateTime now = DateTime.Now;

            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)10000, (int)1000000, (long)10000000, "2008?7?2?", '0', (float)0.0001, (double)0.0000001, now, ValueTuple.Create(false, myObj), TimeSpan.Zero);
            ValueTupleDriverB = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)10000, (int)1000000, (long)10000000, "2008?7?2?", '0', (float)0.0001, (double)0.0000001, now, ValueTuple.Create(false, myObj), TimeSpan.Zero);
            ValueTupleDriverC = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, ValueTuple<bool, object>, TimeSpan>((short)10001, (int)1000001, (long)10000001, "2008?7?3?", '1', (float)0.0002, (double)0.0000002, now.AddMilliseconds(1), ValueTuple.Create(true, myObj), TimeSpan.MaxValue);
            ValueTupleDriverA.TestCompareTo(ValueTupleDriverB, 0, 5);
            ValueTupleDriverA.TestCompareTo(ValueTupleDriverC, -1, 5);
        }

        [Fact]
        public static void TestNotEqual()
        {
            ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, bool, TimeSpan> ValueTupleDriverA;
            //ValueTuple-0
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, bool, TimeSpan>();
            ValueTupleDriverA.TestNotEqual();

            //ValueTuple-1
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, bool, TimeSpan>((short)10000);
            ValueTupleDriverA.TestNotEqual();

            // This is for code coverage purposes
            //ValueTuple-2
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, bool, TimeSpan>((short)10000, (int)1000000);
            ValueTupleDriverA.TestNotEqual();

            //ValueTuple-3
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, bool, TimeSpan>((short)10000, (int)1000000, (long)10000000);
            ValueTupleDriverA.TestNotEqual();

            //ValueTuple-4
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, bool, TimeSpan>((short)10000, (int)1000000, (long)10000000, "2008?7?2?");
            ValueTupleDriverA.TestNotEqual();

            //ValueTuple-5
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, bool, TimeSpan>((short)10000, (int)1000000, (long)10000000, "2008?7?2?", '0');
            ValueTupleDriverA.TestNotEqual();

            //ValueTuple-6
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, bool, TimeSpan>((short)10000, (int)1000000, (long)10000000, "2008?7?2?", '0', float.NaN);
            ValueTupleDriverA.TestNotEqual();

            //ValueTuple-7
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, bool, TimeSpan>((short)10000, (int)1000000, (long)10000000, "2008?7?2?", '0', float.NaN, double.NegativeInfinity);
            ValueTupleDriverA.TestNotEqual();

            //ValueTuple-8, extended ValueTuple
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, bool, TimeSpan>((short)10000, (int)1000000, (long)10000000, "2008?7?2?", '0', float.NaN, double.NegativeInfinity, DateTime.Now);
            ValueTupleDriverA.TestNotEqual();

            //ValueTuple-9 and ValueTuple-10 are not necessary because they use the same code path as ValueTuple-8
        }

        [Fact]
        public static void IncomparableTypes()
        {
            ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, bool, TimeSpan> ValueTupleDriverA;

            //ValueTuple-0
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, bool, TimeSpan>();
            ValueTupleDriverA.TestCompareToThrows();

            //ValueTuple-1
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, bool, TimeSpan>((short)10000);
            ValueTupleDriverA.TestCompareToThrows();

            // This is for code coverage purposes
            //ValueTuple-2
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, bool, TimeSpan>((short)10000, (int)1000000);
            ValueTupleDriverA.TestCompareToThrows();

            //ValueTuple-3
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, bool, TimeSpan>((short)10000, (int)1000000, (long)10000000);
            ValueTupleDriverA.TestCompareToThrows();

            //ValueTuple-4
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, bool, TimeSpan>((short)10000, (int)1000000, (long)10000000, "2008?7?2?");
            ValueTupleDriverA.TestCompareToThrows();

            //ValueTuple-5
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, bool, TimeSpan>((short)10000, (int)1000000, (long)10000000, "2008?7?2?", '0');
            ValueTupleDriverA.TestCompareToThrows();

            //ValueTuple-6
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, bool, TimeSpan>((short)10000, (int)1000000, (long)10000000, "2008?7?2?", '0', float.NaN);
            ValueTupleDriverA.TestCompareToThrows();

            //ValueTuple-7
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, bool, TimeSpan>((short)10000, (int)1000000, (long)10000000, "2008?7?2?", '0', float.NaN, double.NegativeInfinity);
            ValueTupleDriverA.TestCompareToThrows();

            //ValueTuple-8, extended ValueTuple
            ValueTupleDriverA = new ValueTupleTestDriver<short, int, long, string, char, float, double, DateTime, bool, TimeSpan>((short)10000, (int)1000000, (long)10000000, "2008?7?2?", '0', float.NaN, double.NegativeInfinity, DateTime.Now);
            ValueTupleDriverA.TestCompareToThrows();

            //ValueTuple-9 and ValueTuple-10 are not necessary because they use the same code path as ValueTuple-8
        }

        [Fact]
        public static void FloatingPointNaNCases()
        {
            var a = ValueTuple.Create(double.MinValue, double.NaN, float.MinValue, float.NaN);
            var b = ValueTuple.Create(double.MinValue, double.NaN, float.MinValue, float.NaN);

            Assert.True(a.Equals(b));
            Assert.Equal(0, ((IComparable)a).CompareTo(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
            Assert.True(((IStructuralEquatable)a).Equals(b, TestEqualityComparer.Instance));
            Assert.Equal(5, ((IStructuralComparable)a).CompareTo(b, DummyTestComparer.Instance));
            Assert.Equal(
                ((IStructuralEquatable)a).GetHashCode(TestEqualityComparer.Instance),
                ((IStructuralEquatable)b).GetHashCode(TestEqualityComparer.Instance));
        }

        [Fact]
        public static void TestCustomTypeParameter1()
        {
            // Special case of ValueTuple<T1> where T1 is a custom type
            var testClass = new TestClass();
            var a = ValueTuple.Create(testClass);
            var b = ValueTuple.Create(testClass);

            Assert.True(a.Equals(b));
            Assert.Equal(0, ((IComparable)a).CompareTo(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
            Assert.True(((IStructuralEquatable)a).Equals(b, TestEqualityComparer.Instance));
            Assert.Equal(5, ((IStructuralComparable)a).CompareTo(b, DummyTestComparer.Instance));
            Assert.Equal(
                ((IStructuralEquatable)a).GetHashCode(TestEqualityComparer.Instance),
                ((IStructuralEquatable)b).GetHashCode(TestEqualityComparer.Instance));
        }

        [Fact]
        public static void TestCustomTypeParameter2()
        {
            // Special case of ValueTuple<T1, T2> where T2 is a custom type
            var testClass = new TestClass(1);
            var a = ValueTuple.Create(1, testClass);
            var b = ValueTuple.Create(1, testClass);

            Assert.True(a.Equals(b));
            Assert.Equal(0, ((IComparable)a).CompareTo(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
            Assert.True(((IStructuralEquatable)a).Equals(b, TestEqualityComparer.Instance));
            Assert.Equal(5, ((IStructuralComparable)a).CompareTo(b, DummyTestComparer.Instance));
            Assert.Equal(
                ((IStructuralEquatable)a).GetHashCode(TestEqualityComparer.Instance),
                ((IStructuralEquatable)b).GetHashCode(TestEqualityComparer.Instance));
        }

        [Fact]
        public static void TestCustomTypeParameter3()
        {
            // Special case of ValueTuple<T1, T2> where T1 and T2 are custom types
            var testClassA = new TestClass(100);
            var testClassB = new TestClass(101);
            var a = ValueTuple.Create(testClassA, testClassB);
            var b = ValueTuple.Create(testClassB, testClassA);

            Assert.False(a.Equals(b));
            Assert.Equal(-1, ((IComparable)a).CompareTo(b));
            Assert.False(((IStructuralEquatable)a).Equals(b, TestEqualityComparer.Instance));
            Assert.Equal(5, ((IStructuralComparable)a).CompareTo(b, DummyTestComparer.Instance));
            // Equals(IEqualityComparer) is false, ignore hash code
        }

        [Fact]
        public static void TestCustomTypeParameter4()
        {
            // Special case of ValueTuple<T1, T2> where T1 and T2 are custom types
            var testClassA = new TestClass(100);
            var testClassB = new TestClass(101);
            var a = ValueTuple.Create(testClassA, testClassB);
            var b = ValueTuple.Create(testClassA, testClassA);

            Assert.False(a.Equals(b));
            Assert.Equal(1, ((IComparable)a).CompareTo(b));
            Assert.False(((IStructuralEquatable)a).Equals(b, TestEqualityComparer.Instance));
            Assert.Equal(5, ((IStructuralComparable)a).CompareTo(b, DummyTestComparer.Instance));
            // Equals(IEqualityComparer) is false, ignore hash code
        }

        [Fact]
        public static void NestedValueTuples1()
        {
            var a = ValueTuple.Create(1, 2, ValueTuple.Create(31, 32), 4, 5, 6, 7, ValueTuple.Create(8, 9));
            var b = ValueTuple.Create(1, 2, ValueTuple.Create(31, 32), 4, 5, 6, 7, ValueTuple.Create(8, 9));

            Assert.True(a.Equals(b));
            Assert.Equal(0, ((IComparable)a).CompareTo(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
            Assert.True(((IStructuralEquatable)a).Equals(b, TestEqualityComparer.Instance));
            Assert.Equal(5, ((IStructuralComparable)a).CompareTo(b, DummyTestComparer.Instance));
            Assert.Equal(
                ((IStructuralEquatable)a).GetHashCode(TestEqualityComparer.Instance),
                ((IStructuralEquatable)b).GetHashCode(TestEqualityComparer.Instance));
            Assert.Equal("(1, 2, (31, 32), 4, 5, 6, 7, (8, 9))", a.ToString());
            Assert.Equal("(31, 32)", a.Item3.ToString());
            Assert.Equal("((8, 9))", a.Rest.ToString());
        }

        [Fact]
        public static void NestedValueTuples2()
        {
            var a = ValueTuple.Create(0, 1, 2, 3, 4, 5, 6, ValueTuple.Create(7, 8, 9, 10, 11, 12, 13, ValueTuple.Create(14, 15)));
            var b = ValueTuple.Create(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8, 9, 10, 11, 12, 13, 14, ValueTuple.Create(15, 16)));

            Assert.False(a.Equals(b));
            Assert.Equal(-1, ((IComparable)a).CompareTo(b));
            Assert.False(((IStructuralEquatable)a).Equals(b, TestEqualityComparer.Instance));
            Assert.Equal(5, ((IStructuralComparable)a).CompareTo(b, DummyTestComparer.Instance));

            Assert.Equal("(0, 1, 2, 3, 4, 5, 6, (7, 8, 9, 10, 11, 12, 13, (14, 15)))", a.ToString());
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, (8, 9, 10, 11, 12, 13, 14, (15, 16)))", b.ToString());
            Assert.Equal("((7, 8, 9, 10, 11, 12, 13, (14, 15)))", a.Rest.ToString());

            var a2 = Tuple.Create(0, 1, 2, 3, 4, 5, 6, Tuple.Create(7, 8, 9, 10, 11, 12, 13, Tuple.Create(14, 15)));
            var b2 = Tuple.Create(1, 2, 3, 4, 5, 6, 7, Tuple.Create(8, 9, 10, 11, 12, 13, 14, Tuple.Create(15, 16)));

            Assert.Equal(a2.ToString(), a.ToString());
            Assert.Equal(b2.ToString(), b.ToString());
            Assert.Equal(a2.Rest.ToString(), a.Rest.ToString());
        }

        [Fact]
        public static void IncomparableTypesSpecialCase()
        {
            // Special case when T does not implement IComparable
            var testClassA = new TestClass2(100);
            var testClassB = new TestClass2(100);
            var a = ValueTuple.Create(testClassA);
            var b = ValueTuple.Create(testClassB);

            Assert.True(a.Equals(b));
            AssertExtensions.Throws<ArgumentException>(null, () => ((IComparable)a).CompareTo(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
            Assert.True(((IStructuralEquatable)a).Equals(b, TestEqualityComparer.Instance));
            Assert.Equal(5, ((IStructuralComparable)a).CompareTo(b, DummyTestComparer.Instance));
            Assert.Equal(
                ((IStructuralEquatable)a).GetHashCode(TestEqualityComparer.Instance),
                ((IStructuralEquatable)b).GetHashCode(TestEqualityComparer.Instance));
            Assert.Equal("([100])", a.ToString());
        }

        [Fact]
        public static void ZeroTuples()
        {
            var a = ValueTuple.Create();
            Assert.True(a.Equals(new ValueTuple()));
            Assert.Equal(0, a.CompareTo(new ValueTuple()));

            Assert.Equal(1, ((IStructuralComparable)a).CompareTo(null, DummyTestComparer.Instance));
            AssertExtensions.Throws<ArgumentException>("other", () => ((IStructuralComparable)a).CompareTo("string", DummyTestComparer.Instance));

            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, )", CreateLong(1, 2, 3, 4, 5, 6, 7, new ValueTuple()).ToString());
#if NETCOREAPP
            ITuple it = ValueTuple.Create();
            Assert.Throws<IndexOutOfRangeException>(() => it[-1].ToString());
            Assert.Throws<IndexOutOfRangeException>(() => it[0].ToString());
            Assert.Throws<IndexOutOfRangeException>(() => it[1].ToString());
#endif
        }

        [Fact]
        public static void OneTuples()
        {
            IComparable c = ValueTuple.Create(1);

            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(3)));

            IStructuralComparable sc = (IStructuralComparable)c;

            Assert.Equal(1, sc.CompareTo(null, DummyTestComparer.Instance));
            AssertExtensions.Throws<ArgumentException>("other", () => ((IStructuralComparable)sc).CompareTo("string", DummyTestComparer.Instance));

            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(3), TestComparer.Instance));

            Assert.False(((IStructuralEquatable)sc).Equals(sc, DummyTestEqualityComparer.Instance));

            Assert.True(ValueTuple.Create(1).Equals(ValueTuple.Create(1)));
            Assert.False(ValueTuple.Create(1).Equals(ValueTuple.Create(0)));

            Assert.True(((IStructuralEquatable)ValueTuple.Create(1)).Equals(ValueTuple.Create(1), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1)).Equals(ValueTuple.Create(0), TestEqualityComparer.Instance));

            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 1)", CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(1)).ToString());

            var vtWithNull = new ValueTuple<string>(null);
            var tupleWithNull = new Tuple<string>(null);
            Assert.Equal("()", vtWithNull.ToString());
            Assert.Equal(tupleWithNull.ToString(), vtWithNull.ToString());

#if NETCOREAPP
            ITuple it = ValueTuple.Create(1);
            Assert.Throws<IndexOutOfRangeException>(() => it[-1].ToString());
            Assert.Equal(1, it[0]);
            Assert.Throws<IndexOutOfRangeException>(() => it[1].ToString());
#endif
        }

        [Fact]
        public static void TwoTuples()
        {
            IComparable c = ValueTuple.Create(1, 1);

            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(3, 1)));
            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(1, 3)));

            IStructuralComparable sc = (IStructuralComparable)c;

            Assert.Equal(1, sc.CompareTo(null, DummyTestComparer.Instance));
            AssertExtensions.Throws<ArgumentException>("other", () => ((IStructuralComparable)sc).CompareTo("string", DummyTestComparer.Instance));

            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(1, 3), TestComparer.Instance));

            Assert.False(((IStructuralEquatable)sc).Equals(sc, DummyTestEqualityComparer.Instance));

            Assert.True(ValueTuple.Create(1, 2).Equals(ValueTuple.Create(1, 2)));
            Assert.False(ValueTuple.Create(1, 2).Equals(ValueTuple.Create(0, 2)));
            Assert.False(ValueTuple.Create(1, 2).Equals(ValueTuple.Create(1, 0)));

            Assert.True(((IStructuralEquatable)ValueTuple.Create(1, 2)).Equals(ValueTuple.Create(1, 2), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2)).Equals(ValueTuple.Create(0, 2), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2)).Equals(ValueTuple.Create(1, 0), TestEqualityComparer.Instance));

            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 1, 2)", CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(1, 2)).ToString());

            var vtWithNull = new ValueTuple<string, string>(null, null);
            var tupleWithNull = new Tuple<string, string>(null, null);
            Assert.Equal("(, )", vtWithNull.ToString());
            Assert.Equal(tupleWithNull.ToString(), vtWithNull.ToString());

#if NETCOREAPP
            ITuple it = ValueTuple.Create(1, 2);
            Assert.Throws<IndexOutOfRangeException>(() => it[-1].ToString());
            Assert.Equal(1, it[0]);
            Assert.Equal(2, it[1]);
            Assert.Throws<IndexOutOfRangeException>(() => it[2].ToString());
#endif
        }

        [Fact]
        public static void ThreeTuples()
        {
            IComparable c = ValueTuple.Create(1, 1, 1);

            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(3, 1, 1)));
            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(1, 3, 1)));
            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(1, 1, 3)));

            IStructuralComparable sc = (IStructuralComparable)c;

            Assert.Equal(1, sc.CompareTo(null, DummyTestComparer.Instance));
            AssertExtensions.Throws<ArgumentException>("other", () => ((IStructuralComparable)sc).CompareTo("string", DummyTestComparer.Instance));

            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(1, 3, 1), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(1, 1, 3), TestComparer.Instance));

            Assert.False(((IStructuralEquatable)sc).Equals(sc, DummyTestEqualityComparer.Instance));

            Assert.True(ValueTuple.Create(1, 2, 3).Equals(ValueTuple.Create(1, 2, 3)));
            Assert.False(ValueTuple.Create(1, 2, 3).Equals(ValueTuple.Create(0, 2, 3)));
            Assert.False(ValueTuple.Create(1, 2, 3).Equals(ValueTuple.Create(1, 0, 3)));
            Assert.False(ValueTuple.Create(1, 2, 3).Equals(ValueTuple.Create(1, 2, 0)));

            Assert.True(((IStructuralEquatable)ValueTuple.Create(1, 2, 3)).Equals(ValueTuple.Create(1, 2, 3), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3)).Equals(ValueTuple.Create(0, 2, 3), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3)).Equals(ValueTuple.Create(1, 0, 3), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3)).Equals(ValueTuple.Create(1, 2, 0), TestEqualityComparer.Instance));

            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 1, 2, 3)", CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(1, 2, 3)).ToString());

            var vtWithNull = new ValueTuple<string, string, string>(null, null, null);
            var tupleWithNull = new Tuple<string, string, string>(null, null, null);
            Assert.Equal("(, , )", vtWithNull.ToString());
            Assert.Equal(tupleWithNull.ToString(), vtWithNull.ToString());

#if NETCOREAPP
            ITuple it = ValueTuple.Create(1, 2, 3);
            Assert.Throws<IndexOutOfRangeException>(() => it[-1].ToString());
            Assert.Equal(1, it[0]);
            Assert.Equal(2, it[1]);
            Assert.Equal(3, it[2]);
            Assert.Throws<IndexOutOfRangeException>(() => it[3].ToString());
#endif
        }

        [Fact]
        public static void FourTuples()
        {
            IComparable c = ValueTuple.Create(1, 1, 1, 1);

            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(3, 1, 1, 1)));
            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(1, 3, 1, 1)));
            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(1, 1, 3, 1)));
            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(1, 1, 1, 3)));

            IStructuralComparable sc = (IStructuralComparable)c;

            Assert.Equal(1, sc.CompareTo(null, DummyTestComparer.Instance));
            AssertExtensions.Throws<ArgumentException>("other", () => ((IStructuralComparable)sc).CompareTo("string", DummyTestComparer.Instance));

            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(3, 1, 1, 1), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(1, 3, 1, 1), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(1, 1, 3, 1), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(1, 1, 1, 3), TestComparer.Instance));

            Assert.False(((IStructuralEquatable)sc).Equals(sc, DummyTestEqualityComparer.Instance));

            Assert.True(ValueTuple.Create(1, 2, 3, 4).Equals(ValueTuple.Create(1, 2, 3, 4)));
            Assert.False(ValueTuple.Create(1, 2, 3, 4).Equals(ValueTuple.Create(0, 2, 3, 4)));
            Assert.False(ValueTuple.Create(1, 2, 3, 4).Equals(ValueTuple.Create(1, 0, 3, 4)));
            Assert.False(ValueTuple.Create(1, 2, 3, 4).Equals(ValueTuple.Create(1, 2, 0, 4)));
            Assert.False(ValueTuple.Create(1, 2, 3, 4).Equals(ValueTuple.Create(1, 2, 3, 0)));

            Assert.True(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4)).Equals(ValueTuple.Create(1, 2, 3, 4), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4)).Equals(ValueTuple.Create(0, 2, 3, 4), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4)).Equals(ValueTuple.Create(1, 0, 3, 4), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4)).Equals(ValueTuple.Create(1, 2, 0, 4), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4)).Equals(ValueTuple.Create(1, 2, 3, 0), TestEqualityComparer.Instance));

            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 1, 2, 3, 4)", CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(1, 2, 3, 4)).ToString());

            var vtWithNull = new ValueTuple<string, string, string, string>(null, null, null, null);
            var tupleWithNull = new Tuple<string, string, string, string>(null, null, null, null);
            Assert.Equal("(, , , )", vtWithNull.ToString());
            Assert.Equal(tupleWithNull.ToString(), vtWithNull.ToString());

#if NETCOREAPP
            ITuple it = ValueTuple.Create(1, 2, 3, 4);
            Assert.Throws<IndexOutOfRangeException>(() => it[-1].ToString());
            Assert.Equal(1, it[0]);
            Assert.Equal(2, it[1]);
            Assert.Equal(3, it[2]);
            Assert.Equal(4, it[3]);
            Assert.Throws<IndexOutOfRangeException>(() => it[4].ToString());
#endif
        }

        [Fact]
        public static void FiveTuples()
        {
            IComparable c = ValueTuple.Create(1, 1, 1, 1, 1);

            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(3, 1, 1, 1, 1)));
            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(1, 3, 1, 1, 1)));
            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(1, 1, 3, 1, 1)));
            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(1, 1, 1, 3, 1)));
            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(1, 1, 1, 1, 3)));

            IStructuralComparable sc = (IStructuralComparable)c;

            Assert.Equal(1, sc.CompareTo(null, DummyTestComparer.Instance));
            AssertExtensions.Throws<ArgumentException>("other", () => ((IStructuralComparable)sc).CompareTo("string", DummyTestComparer.Instance));

            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(3, 1, 1, 1, 1), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(1, 3, 1, 1, 1), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(1, 1, 3, 1, 1), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(1, 1, 1, 3, 1), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(1, 1, 1, 1, 3), TestComparer.Instance));

            Assert.False(((IStructuralEquatable)sc).Equals(sc, DummyTestEqualityComparer.Instance));

            Assert.True(ValueTuple.Create(1, 2, 3, 4, 5).Equals(ValueTuple.Create(1, 2, 3, 4, 5)));
            Assert.False(ValueTuple.Create(1, 2, 3, 4, 5).Equals(ValueTuple.Create(0, 2, 3, 4, 5)));
            Assert.False(ValueTuple.Create(1, 2, 3, 4, 5).Equals(ValueTuple.Create(1, 0, 3, 4, 5)));
            Assert.False(ValueTuple.Create(1, 2, 3, 4, 5).Equals(ValueTuple.Create(1, 2, 0, 4, 5)));
            Assert.False(ValueTuple.Create(1, 2, 3, 4, 5).Equals(ValueTuple.Create(1, 2, 3, 0, 5)));
            Assert.False(ValueTuple.Create(1, 2, 3, 4, 5).Equals(ValueTuple.Create(1, 2, 3, 4, 0)));

            Assert.True(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4, 5)).Equals(ValueTuple.Create(1, 2, 3, 4, 5), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4, 5)).Equals(ValueTuple.Create(0, 2, 3, 4, 5), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4, 5)).Equals(ValueTuple.Create(1, 0, 3, 4, 5), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4, 5)).Equals(ValueTuple.Create(1, 2, 0, 4, 5), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4, 5)).Equals(ValueTuple.Create(1, 2, 3, 0, 5), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4, 5)).Equals(ValueTuple.Create(1, 2, 3, 4, 0), TestEqualityComparer.Instance));

            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 1, 2, 3, 4, 5)", CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(1, 2, 3, 4, 5)).ToString());

            var vtWithNull = new ValueTuple<string, string, string, string, string>(null, null, null, null, null);
            var tupleWithNull = new Tuple<string, string, string, string, string>(null, null, null, null, null);
            Assert.Equal("(, , , , )", vtWithNull.ToString());
            Assert.Equal(tupleWithNull.ToString(), vtWithNull.ToString());

#if NETCOREAPP
            ITuple it = ValueTuple.Create(1, 2, 3, 4, 5);
            Assert.Throws<IndexOutOfRangeException>(() => it[-1].ToString());
            Assert.Equal(1, it[0]);
            Assert.Equal(2, it[1]);
            Assert.Equal(3, it[2]);
            Assert.Equal(4, it[3]);
            Assert.Equal(5, it[4]);
            Assert.Throws<IndexOutOfRangeException>(() => it[5].ToString());
#endif
        }

        [Fact]
        public static void SixTuples()
        {
            IComparable c = ValueTuple.Create(1, 1, 1, 1, 1, 1);

            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(3, 1, 1, 1, 1, 1)));
            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(1, 3, 1, 1, 1, 1)));
            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(1, 1, 3, 1, 1, 1)));
            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(1, 1, 1, 3, 1, 1)));
            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(1, 1, 1, 1, 3, 1)));
            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(1, 1, 1, 1, 1, 3)));

            IStructuralComparable sc = (IStructuralComparable)c;

            Assert.Equal(1, sc.CompareTo(null, DummyTestComparer.Instance));
            AssertExtensions.Throws<ArgumentException>("other", () => ((IStructuralComparable)sc).CompareTo("string", DummyTestComparer.Instance));

            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(3, 1, 1, 1, 1, 1), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(1, 3, 1, 1, 1, 1), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(1, 1, 3, 1, 1, 1), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(1, 1, 1, 3, 1, 1), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(1, 1, 1, 1, 3, 1), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(1, 1, 1, 1, 1, 3), TestComparer.Instance));

            Assert.False(((IStructuralEquatable)sc).Equals(sc, DummyTestEqualityComparer.Instance));

            Assert.True(ValueTuple.Create(1, 2, 3, 4, 5, 6).Equals(ValueTuple.Create(1, 2, 3, 4, 5, 6)));
            Assert.False(ValueTuple.Create(1, 2, 3, 4, 5, 6).Equals(ValueTuple.Create(0, 2, 3, 4, 5, 6)));
            Assert.False(ValueTuple.Create(1, 2, 3, 4, 5, 6).Equals(ValueTuple.Create(1, 0, 3, 4, 5, 6)));
            Assert.False(ValueTuple.Create(1, 2, 3, 4, 5, 6).Equals(ValueTuple.Create(1, 2, 0, 4, 5, 6)));
            Assert.False(ValueTuple.Create(1, 2, 3, 4, 5, 6).Equals(ValueTuple.Create(1, 2, 3, 0, 5, 6)));
            Assert.False(ValueTuple.Create(1, 2, 3, 4, 5, 6).Equals(ValueTuple.Create(1, 2, 3, 4, 0, 6)));
            Assert.False(ValueTuple.Create(1, 2, 3, 4, 5, 6).Equals(ValueTuple.Create(1, 2, 3, 4, 5, 0)));

            Assert.True(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4, 5, 6)).Equals(ValueTuple.Create(1, 2, 3, 4, 5, 6), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4, 5, 6)).Equals(ValueTuple.Create(0, 2, 3, 4, 5, 6), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4, 5, 6)).Equals(ValueTuple.Create(1, 0, 3, 4, 5, 6), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4, 5, 6)).Equals(ValueTuple.Create(1, 2, 0, 4, 5, 6), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4, 5, 6)).Equals(ValueTuple.Create(1, 2, 3, 0, 5, 6), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4, 5, 6)).Equals(ValueTuple.Create(1, 2, 3, 4, 0, 6), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4, 5, 6)).Equals(ValueTuple.Create(1, 2, 3, 4, 5, 0), TestEqualityComparer.Instance));

            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 1, 2, 3, 4, 5, 6)", CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(1, 2, 3, 4, 5, 6)).ToString());

            var vtWithNull = new ValueTuple<string, string, string, string, string, string>(null, null, null, null, null, null);
            var tupleWithNull = new Tuple<string, string, string, string, string, string>(null, null, null, null, null, null);
            Assert.Equal("(, , , , , )", vtWithNull.ToString());
            Assert.Equal(tupleWithNull.ToString(), vtWithNull.ToString());

#if NETCOREAPP
            ITuple it = ValueTuple.Create(1, 2, 3, 4, 5, 6);
            Assert.Throws<IndexOutOfRangeException>(() => it[-1].ToString());
            Assert.Equal(1, it[0]);
            Assert.Equal(2, it[1]);
            Assert.Equal(3, it[2]);
            Assert.Equal(4, it[3]);
            Assert.Equal(5, it[4]);
            Assert.Equal(6, it[5]);
            Assert.Throws<IndexOutOfRangeException>(() => it[6].ToString());
#endif
        }

        [Fact]
        public static void SevenTuples()
        {
            IComparable c = ValueTuple.Create(1, 1, 1, 1, 1, 1, 1);

            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(3, 1, 1, 1, 1, 1, 1)));
            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(1, 3, 1, 1, 1, 1, 1)));
            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(1, 1, 3, 1, 1, 1, 1)));
            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(1, 1, 1, 3, 1, 1, 1)));
            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(1, 1, 1, 1, 3, 1, 1)));
            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(1, 1, 1, 1, 1, 3, 1)));
            Assert.Equal(-1, c.CompareTo(ValueTuple.Create(1, 1, 1, 1, 1, 1, 3)));

            IStructuralComparable sc = (IStructuralComparable)c;

            Assert.Equal(1, sc.CompareTo(null, DummyTestComparer.Instance));
            AssertExtensions.Throws<ArgumentException>("other", () => ((IStructuralComparable)sc).CompareTo("string", DummyTestComparer.Instance));

            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(3, 1, 1, 1, 1, 1, 1), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(1, 3, 1, 1, 1, 1, 1), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(1, 1, 3, 1, 1, 1, 1), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(1, 1, 1, 3, 1, 1, 1), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(1, 1, 1, 1, 3, 1, 1), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(1, 1, 1, 1, 1, 3, 1), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(ValueTuple.Create(1, 1, 1, 1, 1, 1, 3), TestComparer.Instance));

            Assert.False(((IStructuralEquatable)sc).Equals(sc, DummyTestEqualityComparer.Instance));

            Assert.True(ValueTuple.Create(1, 2, 3, 4, 5, 6, 7).Equals(ValueTuple.Create(1, 2, 3, 4, 5, 6, 7)));
            Assert.False(ValueTuple.Create(1, 2, 3, 4, 5, 6, 7).Equals(ValueTuple.Create(0, 2, 3, 4, 5, 6, 7)));
            Assert.False(ValueTuple.Create(1, 2, 3, 4, 5, 6, 7).Equals(ValueTuple.Create(1, 0, 3, 4, 5, 6, 7)));
            Assert.False(ValueTuple.Create(1, 2, 3, 4, 5, 6, 7).Equals(ValueTuple.Create(1, 2, 0, 4, 5, 6, 7)));
            Assert.False(ValueTuple.Create(1, 2, 3, 4, 5, 6, 7).Equals(ValueTuple.Create(1, 2, 3, 0, 5, 6, 7)));
            Assert.False(ValueTuple.Create(1, 2, 3, 4, 5, 6, 7).Equals(ValueTuple.Create(1, 2, 3, 4, 0, 6, 7)));
            Assert.False(ValueTuple.Create(1, 2, 3, 4, 5, 6, 7).Equals(ValueTuple.Create(1, 2, 3, 4, 5, 0, 7)));
            Assert.False(ValueTuple.Create(1, 2, 3, 4, 5, 6, 7).Equals(ValueTuple.Create(1, 2, 3, 4, 5, 6, 0)));

            Assert.True(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4, 5, 6, 7)).Equals(ValueTuple.Create(1, 2, 3, 4, 5, 6, 7), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4, 5, 6, 7)).Equals(ValueTuple.Create(0, 2, 3, 4, 5, 6, 7), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4, 5, 6, 7)).Equals(ValueTuple.Create(1, 0, 3, 4, 5, 6, 7), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4, 5, 6, 7)).Equals(ValueTuple.Create(1, 2, 0, 4, 5, 6, 7), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4, 5, 6, 7)).Equals(ValueTuple.Create(1, 2, 3, 0, 5, 6, 7), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4, 5, 6, 7)).Equals(ValueTuple.Create(1, 2, 3, 4, 0, 6, 7), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4, 5, 6, 7)).Equals(ValueTuple.Create(1, 2, 3, 4, 5, 0, 7), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)ValueTuple.Create(1, 2, 3, 4, 5, 6, 7)).Equals(ValueTuple.Create(1, 2, 3, 4, 5, 6, 0), TestEqualityComparer.Instance));

            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 1, 2, 3, 4, 5, 6, 7)", CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(1, 2, 3, 4, 5, 6, 7)).ToString());

            var vtWithNull = new ValueTuple<string, string, string, string, string, string, string>(null, null, null, null, null, null, null);
            var tupleWithNull = new Tuple<string, string, string, string, string, string, string>(null, null, null, null, null, null, null);
            Assert.Equal("(, , , , , , )", vtWithNull.ToString());
            Assert.Equal(tupleWithNull.ToString(), vtWithNull.ToString());

#if NETCOREAPP
            ITuple it = ValueTuple.Create(1, 2, 3, 4, 5, 6, 7);
            Assert.Throws<IndexOutOfRangeException>(() => it[-1].ToString());
            Assert.Equal(1, it[0]);
            Assert.Equal(2, it[1]);
            Assert.Equal(3, it[2]);
            Assert.Equal(4, it[3]);
            Assert.Equal(5, it[4]);
            Assert.Equal(6, it[5]);
            Assert.Equal(7, it[6]);
            Assert.Throws<IndexOutOfRangeException>(() => it[7].ToString());
#endif
        }

        public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> CreateLong<T1, T2, T3, T4, T5, T6, T7, TRest>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest) where TRest : struct
        {
            return new ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1, item2, item3, item4, item5, item6, item7, rest);
        }

        public static Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> CreateLongRef<T1, T2, T3, T4, T5, T6, T7, TRest>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest)
        {
            return new Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1, item2, item3, item4, item5, item6, item7, rest);
        }

        [Fact]
        public static void EightTuples()
        {
            var x = new Tuple<int, int, int, int, int, int, int, Tuple<string, string>>(1, 2, 3, 4, 5, 6, 7, new Tuple<string, string>("alice", "bob"));
            var y = new ValueTuple<int, int, int, int, int, int, int, ValueTuple<string, string>>(1, 2, 3, 4, 5, 6, 7, new ValueTuple<string, string>("alice", "bob"));
            Assert.Equal(x.ToString(), y.ToString());

            var t = CreateLong(1, 1, 1, 1, 1, 1, 1, ValueTuple.Create(1));

            IStructuralEquatable se = t;
            Assert.False(se.Equals(null, TestEqualityComparer.Instance));
            Assert.False(se.Equals("string", TestEqualityComparer.Instance));
            Assert.False(se.Equals(new ValueTuple(), TestEqualityComparer.Instance));

            IComparable c = t;
            Assert.Equal(-1, c.CompareTo(CreateLong(3, 1, 1, 1, 1, 1, 1, ValueTuple.Create(1))));
            Assert.Equal(-1, c.CompareTo(CreateLong(1, 3, 1, 1, 1, 1, 1, ValueTuple.Create(1))));
            Assert.Equal(-1, c.CompareTo(CreateLong(1, 1, 3, 1, 1, 1, 1, ValueTuple.Create(1))));
            Assert.Equal(-1, c.CompareTo(CreateLong(1, 1, 1, 3, 1, 1, 1, ValueTuple.Create(1))));
            Assert.Equal(-1, c.CompareTo(CreateLong(1, 1, 1, 1, 3, 1, 1, ValueTuple.Create(1))));
            Assert.Equal(-1, c.CompareTo(CreateLong(1, 1, 1, 1, 1, 3, 1, ValueTuple.Create(1))));
            Assert.Equal(-1, c.CompareTo(CreateLong(1, 1, 1, 1, 1, 1, 3, ValueTuple.Create(1))));

            IStructuralComparable sc = t;
            Assert.Equal(1, sc.CompareTo(null, DummyTestComparer.Instance));
            AssertExtensions.Throws<ArgumentException>("other", () => sc.CompareTo("string", DummyTestComparer.Instance));

            Assert.Equal(1, sc.CompareTo(CreateLong(3, 1, 1, 1, 1, 1, 1, ValueTuple.Create(1)), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(CreateLong(1, 3, 1, 1, 1, 1, 1, ValueTuple.Create(1)), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(CreateLong(1, 1, 3, 1, 1, 1, 1, ValueTuple.Create(1)), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(CreateLong(1, 1, 1, 3, 1, 1, 1, ValueTuple.Create(1)), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(CreateLong(1, 1, 1, 1, 3, 1, 1, ValueTuple.Create(1)), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(CreateLong(1, 1, 1, 1, 1, 3, 1, ValueTuple.Create(1)), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(CreateLong(1, 1, 1, 1, 1, 1, 3, ValueTuple.Create(1)), TestComparer.Instance));
            Assert.Equal(1, sc.CompareTo(CreateLong(1, 1, 1, 1, 1, 1, 1, ValueTuple.Create(3)), TestComparer.Instance));

            Assert.False(se.Equals(t, DummyTestEqualityComparer.Instance));

            Assert.True(CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8)).Equals(CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8))));
            Assert.False(CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8)).Equals(CreateLong(0, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8))));
            Assert.False(CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8)).Equals(CreateLong(1, 0, 3, 4, 5, 6, 7, ValueTuple.Create(8))));
            Assert.False(CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8)).Equals(CreateLong(1, 2, 0, 4, 5, 6, 7, ValueTuple.Create(8))));
            Assert.False(CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8)).Equals(CreateLong(1, 2, 3, 0, 5, 6, 7, ValueTuple.Create(8))));
            Assert.False(CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8)).Equals(CreateLong(1, 2, 3, 4, 0, 6, 7, ValueTuple.Create(8))));
            Assert.False(CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8)).Equals(CreateLong(1, 2, 3, 4, 5, 0, 7, ValueTuple.Create(8))));
            Assert.False(CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8)).Equals(CreateLong(1, 2, 3, 4, 5, 6, 0, ValueTuple.Create(8))));
            Assert.False(CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8)).Equals(CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(0))));

            Assert.True(((IStructuralEquatable)CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8))).Equals(CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8)), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8))).Equals(CreateLong(0, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8)), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8))).Equals(CreateLong(1, 0, 3, 4, 5, 6, 7, ValueTuple.Create(8)), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8))).Equals(CreateLong(1, 2, 0, 4, 5, 6, 7, ValueTuple.Create(8)), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8))).Equals(CreateLong(1, 2, 3, 0, 5, 6, 7, ValueTuple.Create(8)), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8))).Equals(CreateLong(1, 2, 3, 4, 0, 6, 7, ValueTuple.Create(8)), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8))).Equals(CreateLong(1, 2, 3, 4, 5, 0, 7, ValueTuple.Create(8)), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8))).Equals(CreateLong(1, 2, 3, 4, 5, 6, 0, ValueTuple.Create(8)), TestEqualityComparer.Instance));
            Assert.False(((IStructuralEquatable)CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8))).Equals(CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(0)), TestEqualityComparer.Instance));

            // Notice that 0-tuple prints as empty position
            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 1, 2, 3, 4, 5, 6, 7, )", CreateLong(1, 2, 3, 4, 5, 6, 7, CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create())).ToString());

            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 1, 2, 3, 4, 5, 6, 7, 1)", CreateLong(1, 2, 3, 4, 5, 6, 7, CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(1))).ToString());

            var vtWithNull = new ValueTuple<string, string, string, string, string, string, string, ValueTuple<string>>(null, null, null, null, null, null, null, new ValueTuple<string>(null));
            var tupleWithNull = new Tuple<string, string, string, string, string, string, string, Tuple<string>>(null, null, null, null, null, null, null, new Tuple<string>(null));
            Assert.Equal("(, , , , , , , )", vtWithNull.ToString());
            Assert.Equal(tupleWithNull.ToString(), vtWithNull.ToString());

#if NETCOREAPP
            ITuple it = CreateLong(1, 2, 3, 4, 5, 6, 7, ValueTuple.Create(8));
            Assert.Throws<IndexOutOfRangeException>(() => it[-1].ToString());
            Assert.Equal(1, it[0]);
            Assert.Equal(2, it[1]);
            Assert.Equal(3, it[2]);
            Assert.Equal(4, it[3]);
            Assert.Equal(5, it[4]);
            Assert.Equal(6, it[5]);
            Assert.Equal(7, it[6]);
            Assert.Equal(8, it[7]);
            Assert.Throws<IndexOutOfRangeException>(() => it[8].ToString());
#endif
        }

        [Fact]
        public static void LongTuplesWithNull()
        {
            {
                var vtWithNull = CreateLong(1, 2, 3, 4, 5, 6, 7, new ValueTuple<string>(null));
                var tupleWithNull = CreateLongRef(1, 2, 3, 4, 5, 6, 7, new Tuple<string>(null));
                Assert.Equal("(1, 2, 3, 4, 5, 6, 7, )", vtWithNull.ToString());
                Assert.Equal(tupleWithNull.ToString(), vtWithNull.ToString());
            }

            {
                var vtWithNull = CreateLong(1, 2, 3, 4, 5, 6, 7, new ValueTuple<string>(null));
                var tupleWithNull = CreateLongRef(1, 2, 3, 4, 5, 6, 7, new Tuple<string>(null));
                Assert.Equal("(1, 2, 3, 4, 5, 6, 7, )", vtWithNull.ToString());
                Assert.Equal(tupleWithNull.ToString(), vtWithNull.ToString());
            }

            {
                var vtWithNull = CreateLong(1, 2, 3, 4, 5, 6, 7, new ValueTuple<string, string>(null, null));
                var tupleWithNull = CreateLongRef(1, 2, 3, 4, 5, 6, 7, new Tuple<string, string>(null, null));
                Assert.Equal("(1, 2, 3, 4, 5, 6, 7, , )", vtWithNull.ToString());
                Assert.Equal(tupleWithNull.ToString(), vtWithNull.ToString());
            }

            {
                var vtWithNull = CreateLong(1, 2, 3, 4, 5, 6, 7, new ValueTuple<string, string, string>(null, null, null));
                var tupleWithNull = CreateLongRef(1, 2, 3, 4, 5, 6, 7, new Tuple<string, string, string>(null, null, null));
                Assert.Equal("(1, 2, 3, 4, 5, 6, 7, , , )", vtWithNull.ToString());
                Assert.Equal(tupleWithNull.ToString(), vtWithNull.ToString());
            }

            {
                var vtWithNull = CreateLong(1, 2, 3, 4, 5, 6, 7, new ValueTuple<string, string, string, string>(null, null, null, null));
                var tupleWithNull = CreateLongRef(1, 2, 3, 4, 5, 6, 7, new Tuple<string, string, string, string>(null, null, null, null));
                Assert.Equal("(1, 2, 3, 4, 5, 6, 7, , , , )", vtWithNull.ToString());
                Assert.Equal(tupleWithNull.ToString(), vtWithNull.ToString());
            }

            {
                var vtWithNull = CreateLong(1, 2, 3, 4, 5, 6, 7, new ValueTuple<string, string, string, string, string>(null, null, null, null, null));
                var tupleWithNull = CreateLongRef(1, 2, 3, 4, 5, 6, 7, new Tuple<string, string, string, string, string>(null, null, null, null, null));
                Assert.Equal("(1, 2, 3, 4, 5, 6, 7, , , , , )", vtWithNull.ToString());
                Assert.Equal(tupleWithNull.ToString(), vtWithNull.ToString());
            }

            {
                var vtWithNull = CreateLong(1, 2, 3, 4, 5, 6, 7, new ValueTuple<string, string, string, string, string, string>(null, null, null, null, null, null));
                var tupleWithNull = CreateLongRef(1, 2, 3, 4, 5, 6, 7, new Tuple<string, string, string, string, string, string>(null, null, null, null, null, null));
                Assert.Equal("(1, 2, 3, 4, 5, 6, 7, , , , , , )", vtWithNull.ToString());
                Assert.Equal(tupleWithNull.ToString(), vtWithNull.ToString());
            }

            {
                var vtWithNull = CreateLong(1, 2, 3, 4, 5, 6, 7, new ValueTuple<string, string, string, string, string, string, string>(null, null, null, null, null, null, null));
                var tupleWithNull = CreateLongRef(1, 2, 3, 4, 5, 6, 7, new Tuple<string, string, string, string, string, string, string>(null, null, null, null, null, null, null));
                Assert.Equal("(1, 2, 3, 4, 5, 6, 7, , , , , , , )", vtWithNull.ToString());
                Assert.Equal(tupleWithNull.ToString(), vtWithNull.ToString());
            }
        }

        [Fact]
        public static void EightTuplesWithBadRest()
        {
            var d = default(ValueTuple<int, int, int, int, int, int, int, int>);
            d.Item1 = 1;
            d.Rest = 42;
            Assert.Equal("(1, 0, 0, 0, 0, 0, 0, 42)", d.ToString());

            // GetHashCode only tries to hash the first 7 elements when rest is not ITupleInternal
            Assert.Equal(ValueTuple.Create(1, 0, 0, 0, 0, 0, 0).GetHashCode(), d.GetHashCode());
            Assert.Equal(((IStructuralEquatable)ValueTuple.Create(1, 0, 0, 0, 0, 0, 0)).GetHashCode(TestEqualityComparer.Instance), ((IStructuralEquatable)d).GetHashCode(TestEqualityComparer.Instance));

            Assert.Equal("(1, 2, 3, 4, 5, 6, 7, 1, 0, 0, 0, 0, 0, 0, 42)", CreateLong(1, 2, 3, 4, 5, 6, 7, d).ToString());

#if NETCOREAPP
            ITuple it = d;
            Assert.Throws<IndexOutOfRangeException>(() => it[-1].ToString());
            Assert.Equal(1, it[0]);
            Assert.Equal(0, it[1]);
            Assert.Equal(0, it[2]);
            Assert.Equal(0, it[3]);
            Assert.Equal(0, it[4]);
            Assert.Equal(0, it[5]);
            Assert.Equal(0, it[6]);
            Assert.Equal(42, it[7]);
            Assert.Throws<IndexOutOfRangeException>(() => it[8].ToString());
#endif
        }

        private class TestClass : IComparable
        {
            private readonly int _value;

            internal TestClass()
                : this(0)
            { }

            internal TestClass(int value)
            {
                this._value = value;
            }

            public override string ToString()
            {
                return "{" + _value.ToString() + "}";
            }

            public int CompareTo(object x)
            {
                TestClass tmp = x as TestClass;
                if (tmp != null)
                    return this._value.CompareTo(tmp._value);
                else
                    return 1;
            }
        }

        private class TestClass2
        {
            private readonly int _value;

            internal TestClass2()
                : this(0)
            { }

            internal TestClass2(int value)
            {
                this._value = value;
            }

            public override string ToString()
            {
                return "[" + _value.ToString() + "]";
            }

            public override bool Equals(object x)
            {
                TestClass2 tmp = x as TestClass2;
                if (tmp != null)
                    return _value.Equals(tmp._value);
                else
                    return false;
            }

            public override int GetHashCode()
            {
                return _value.GetHashCode();
            }
        }

        private class DummyTestComparer : IComparer
        {
            public static readonly DummyTestComparer Instance = new DummyTestComparer();

            public int Compare(object x, object y)
            {
                return 5;
            }
        }

        private class TestComparer : IComparer
        {
            public static readonly TestComparer Instance = new TestComparer();

            public int Compare(object x, object y)
            {
                return x.Equals(y) ? 0 : 1;
            }
        }

        private class DummyTestEqualityComparer : IEqualityComparer
        {
            public static readonly DummyTestEqualityComparer Instance = new DummyTestEqualityComparer();

            public new bool Equals(object x, object y)
            {
                return false;
            }

            public int GetHashCode(object x)
            {
                return x.GetHashCode();
            }
        }

        private class TestEqualityComparer : IEqualityComparer
        {
            public static readonly TestEqualityComparer Instance = new TestEqualityComparer();

            public new bool Equals(object x, object y)
            {
                return x.Equals(y);
            }

            public int GetHashCode(object x)
            {
                return x.GetHashCode();
            }
        }
    }
}
