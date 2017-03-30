// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.Globalization;

namespace System.Xml.Tests
{
    /// <summary>
    ///     XmlConvert type conversion functions
    /// </summary>
    public class ToTypeTests : CTestCase
    {
        #region Public Methods and Operators

        public override void AddChildren()
        {
            AddChild(new CVariation(ToType1) { Attribute = new Variation("ToBoolean - valid cases") });
            AddChild(new CVariation(ToType2) { Attribute = new Variation("ToBoolean - invalid cases") });
            AddChild(new CVariation(ToType3) { Attribute = new Variation("ToByte - valid cases") });
            AddChild(new CVariation(ToType4) { Attribute = new Variation("ToByte - invalid cases") });
            AddChild(new CVariation(ToType5) { Attribute = new Variation("ToChar - valid cases") });
            AddChild(new CVariation(ToType6) { Attribute = new Variation("ToChar - invalid cases") });
            AddChild(new CVariation(ToType9) { Attribute = new Variation("ToDecimal - valid cases") });
            AddChild(new CVariation(ToType10) { Attribute = new Variation("ToDecimal - invalid cases") });
            AddChild(new CVariation(ToType11) { Attribute = new Variation("ToDouble - valid cases") });
            AddChild(new CVariation(ToType12) { Attribute = new Variation("ToDouble - invalid cases") });
            AddChild(new CVariation(ToType13) { Attribute = new Variation("ToInt16 - valid cases") });
            AddChild(new CVariation(ToType14) { Attribute = new Variation("ToInt16 - invalid cases") });
            AddChild(new CVariation(ToType15) { Attribute = new Variation("ToInt32 - valid cases") });
            AddChild(new CVariation(ToType16) { Attribute = new Variation("ToInt32 - invalid cases") });
            AddChild(new CVariation(ToType17) { Attribute = new Variation("ToInt64 - valid cases") });
            AddChild(new CVariation(ToType18) { Attribute = new Variation("ToInt64 - invalid cases") });
            AddChild(new CVariation(ToType19) { Attribute = new Variation("ToSingle - valid cases") });
            AddChild(new CVariation(ToType20) { Attribute = new Variation("ToSingle - invalid cases") });
            AddChild(new CVariation(ToType21) { Attribute = new Variation("ToSByte - valid cases") });
            AddChild(new CVariation(ToType22) { Attribute = new Variation("ToSByte - invalid cases") });
            AddChild(new CVariation(ToType23) { Attribute = new Variation("ToTimeSpan - valid cases") });
            AddChild(new CVariation(ToType24) { Attribute = new Variation("ToTimeSpan - invalid cases") });
            AddChild(new CVariation(ToType25) { Attribute = new Variation("ToUInt16 - valid cases") });
            AddChild(new CVariation(ToType26) { Attribute = new Variation("ToUInt16 - invalid cases") });
            AddChild(new CVariation(ToType27) { Attribute = new Variation("ToUInt32 - valid cases") });
            AddChild(new CVariation(ToType28) { Attribute = new Variation("ToUInt32 - invalid cases") });
            AddChild(new CVariation(ToType29) { Attribute = new Variation("ToUInt64 - valid cases") });
            AddChild(new CVariation(ToType30) { Attribute = new Variation("ToUInt64 - invalid cases") });
            AddChild(new CVariation(ToType31) { Attribute = new Variation("ToGuid - valid cases") });
            AddChild(new CVariation(ToType32) { Attribute = new Variation("ToGuid - invalid cases") });
            AddChild(new CVariation(ToType33) { Attribute = new Variation("ToString(integral type) - valid cases") });
            AddChild(new CVariation(ToType34) { Attribute = new Variation("ToString(float) - valid cases") });
            AddChild(new CVariation(ToType35) { Attribute = new Variation("ToString(double) - valid cases") });
            AddChild(new CVariation(ToType36) { Attribute = new Variation("ToString(decimal) - valid cases") });
            AddChild(new CVariation(ToType37) { Attribute = new Variation("ToString(byte, bool, char) - valid cases") });
            AddChild(new CVariation(ToType38) { Attribute = new Variation("ToString(TimeSpan) - valid cases") });
            AddChild(new CVariation(ToType39) { Attribute = new Variation("ToString(Guid) - valid cases") });
            AddChild(new CVariation(ToType40a) { Attribute = new Variation("ToString(int16, in32, int64, uint32, uint64) - valid cases") });
            AddChild(new CVariation(ToType42) { Attribute = new Variation("PositiveZero and NegativeZero double, float, bug regression 76523") });
            AddChild(new CVariation(ToType52) { Attribute = new Variation("ToDateTimeOffset(String s, String[] formats) - valid cases") { Param = "datetimeOffset.formats" } });
            AddChild(new CVariation(ToType52) { Attribute = new Variation("ToDateTimeOffset(String s, String format) - valid cases") { Param = "datetimeOffset.format" } });
            AddChild(new CVariation(ToType52) { Attribute = new Variation("ToDateTimeOffset(String s) - valid cases") { Param = "datetimeOffset" } });
            AddChild(new CVariation(ToType53) { Attribute = new Variation("ToDateTimeOffset(String s) with offset = hh:60 - valid") { Param = "datetimeOffset" } });
            AddChild(new CVariation(ToType54) { Attribute = new Variation("ToDateTimeOffset(String s, String format) with offset = hh:60 - invalid") { Param = "datetimeOffset.format" } });
            AddChild(new CVariation(ToType55) { Attribute = new Variation("ToDateTimeOffset(String s, String format) - invalid cases") { Param = "datetimeOffset.format" } });
            AddChild(new CVariation(ToType55) { Attribute = new Variation("ToDateTimeOffset(String s) - invalid cases") { Param = "datetimeOffset" } });
            AddChild(new CVariation(ToType55) { Attribute = new Variation("ToDateTimeOffset(String s, String[] formats) - invalid cases") { Param = "datetimeOffset.formats" } });
            AddChild(new CVariation(ToType56) { Attribute = new Variation("ToString(DateTimeOffset) - valid cases") });
            AddChild(new CVariation(ToType57) { Attribute = new Variation("ToString(DateTimeOffset, format) - valid cases") });
            AddChild(new CVariation(ToType58) { Attribute = new Variation("4. Roundtrip: DateTimeOffset-String-DateTimeOffset") { Param = 4 } });
            AddChild(new CVariation(ToType58) { Attribute = new Variation("1. Roundtrip: DateTimeOffset-String-DateTimeOffset") { Param = 1 } });
            AddChild(new CVariation(ToType58) { Attribute = new Variation("5. Roundtrip: DateTimeOffset-String-DateTimeOffset") { Param = 5 } });
            AddChild(new CVariation(ToType58) { Attribute = new Variation("2. Roundtrip: DateTimeOffset-String-DateTimeOffset") { Param = 2 } });
            AddChild(new CVariation(ToType58) { Attribute = new Variation("3. Roundtrip: DateTimeOffset-String-DateTimeOffset") { Param = 3 } });
            AddChild(new CVariation(ToType59) { Attribute = new Variation("5. Roundtrip: String-DateTimeOffset-String") { Param = "9999-12-31T23:59:59Z" } });
            AddChild(new CVariation(ToType59) { Attribute = new Variation("6. Roundtrip: String-DateTimeOffset-String") { Param = "2000-02-29T23:59:59.9999999+13:59" } });
            AddChild(new CVariation(ToType59) { Attribute = new Variation("2. Roundtrip: String-DateTimeOffset-String") { Param = "0001-01-01T00:00:00-14:00" } });
            AddChild(new CVariation(ToType59) { Attribute = new Variation("3. Roundtrip: String-DateTimeOffset-String") { Param = "9998-12-31T12:59:59-14:00" } });
            AddChild(new CVariation(ToType59) { Attribute = new Variation("4. Roundtrip: String-DateTimeOffset-String") { Param = "9999-12-31T23:59:59+14:00" } });
            AddChild(new CVariation(ToType59) { Attribute = new Variation("1. Roundtrip: String-DateTimeOffset-String") { Param = "0001-01-01T00:00:00Z" } });
            AddChild(new CVariation(ToType62) { Attribute = new Variation("null params DateTime, DateTimeOffset - invalid cases") });
        }

        public int TestInvalid(string[] array, string type)
        {
            string[] format = { "yyyy-MM-ddTHH:mm:sszzzzzz" };
            return TestInvalid(array, type, format);
        }

        public int TestInvalid(string[] array, string type, string[] format)
        {
            string[] formats = { "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-dd", "HH:mm:sszzzzzz", null };
            for (int i = 0; i < array.Length; i++)
            {
                try
                {
                    switch (type)
                    {
                        case "bool":
                            XmlConvert.ToBoolean(array[i]);
                            break;
                        case "byte":
                            XmlConvert.ToByte(array[i]);
                            break;
                        case "char":
                            XmlConvert.ToChar(array[i]);
                            break;
                        case "decimal":
                            XmlConvert.ToDecimal(array[i]);
                            break;
                        case "double":
                            XmlConvert.ToDouble(array[i]);
                            break;
                        case "Int16":
                            XmlConvert.ToInt16(array[i]);
                            break;
                        case "Int32":
                            XmlConvert.ToInt32(array[i]);
                            break;
                        case "Int64":
                            XmlConvert.ToInt64(array[i]);
                            break;
                        case "single":
                            XmlConvert.ToSingle(array[i]);
                            break;
                        case "sbyte":
                            XmlConvert.ToSByte(array[i]);
                            break;
                        case "timespan":
                            XmlConvert.ToTimeSpan(array[i]);
                            break;
                        case "uint16":
                            XmlConvert.ToUInt16(array[i]);
                            break;
                        case "uint32":
                            XmlConvert.ToUInt32(array[i]);
                            break;
                        case "uint64":
                            XmlConvert.ToUInt64(array[i]);
                            break;
                        case "guid":
                            XmlConvert.ToGuid(array[i]);
                            break;
                        case "datetimeOffset":
                            XmlConvert.ToDateTimeOffset(array[i]);
                            break;
                        case "datetimeOffset.format":
                            XmlConvert.ToDateTimeOffset(array[i], format[i]);
                            break;
                        case "datetimeOffset.formats":
                            XmlConvert.ToDateTimeOffset(array[i], formats);
                            break;
                        default:
                            throw new Exception("Unhandled type " + type);
                    }
                    CError.WriteLine(array[i]);
                    return TEST_FAIL;
                }
                catch (FormatException e)
                {
                    CError.WriteLine(e.Message);
                }
                catch (OverflowException e)
                {
                    CError.WriteLine(e.Message);
                }
                catch (ArgumentNullException e)
                {
                    CError.WriteLine(e.Message);
                }
                catch (ArgumentOutOfRangeException e)
                {
                    CError.WriteLine(e.Message);
                }
                catch (NullReferenceException e)
                {
                    CError.WriteLine(e.Message);
                }
            }
            return TEST_PASS;
        }

        public int TestValid(object[] array0, object[] array1, string type)
        {
            string[] format = { "yyyy-MM-ddTHH:mm:sszzzzzz" };
            return TestValid(array0, array1, type, format);
        }

        public int TestValid(object[] array0, object[] array1, string type, string[] format)
        {
            object actual;
            object expect;
            string[] formats = { "yyyy-MM-ddzzzzzz", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ", "yyyy-MM-ddTHH:mm:ss.Zzzzzz", "yyyyZ", "yyyy", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-dd", "HH:mm:ss", "HH:mm:ssZ", "yyyy-MM-ddZ", "yyyy-MM", "HH:mm:sszzzzzz", "yyyy-MM-dd-HH:mm" };
            for (int i = 0; i < array0.Length; i++)
            {
                CError.WriteLine(array0[i]);
                switch (type)
                {
                    case "bool":
                        actual = XmlConvert.ToBoolean((string)array0[i]);
                        expect = (bool)array1[i];
                        break;
                    case "byte":
                        actual = XmlConvert.ToByte((string)array0[i]);
                        expect = (byte)array1[i];
                        break;
                    case "char":
                        actual = XmlConvert.ToChar((string)array0[i]);
                        expect = (char)array1[i];
                        break;
                    case "datetime.Roundtrip":
                        actual = XmlConvert.ToDateTime((string)array0[i], XmlDateTimeSerializationMode.RoundtripKind);
                        expect = (DateTime)array1[i];
                        break;
                    case "datetime.Local":
                        actual = XmlConvert.ToDateTime((string)array0[i], XmlDateTimeSerializationMode.Local);
                        expect = (DateTime)array1[i];
                        break;
                    case "datetime.Utc":
                        actual = XmlConvert.ToDateTime((string)array0[i], XmlDateTimeSerializationMode.Utc);
                        expect = (DateTime)array1[i];
                        break;
                    case "datetime.Unspecified":
                        actual = XmlConvert.ToDateTime((string)array0[i], XmlDateTimeSerializationMode.Unspecified);
                        expect = (DateTime)array1[i];
                        break;
                    case "decimal":
                        actual = XmlConvert.ToDecimal((string)array0[i]);
                        expect = (decimal)array1[i];
                        break;
                    case "double":
                        actual = XmlConvert.ToDouble((string)array0[i]);
                        expect = (double)array1[i];
                        break;
                    case "Int16":
                        actual = XmlConvert.ToInt16((string)array0[i]);
                        expect = (Int16)array1[i];
                        break;
                    case "Int32":
                        actual = XmlConvert.ToInt32((string)array0[i]);
                        expect = (Int32)array1[i];
                        break;
                    case "Int64":
                        actual = XmlConvert.ToInt64((string)array0[i]);
                        expect = (Int64)array1[i];
                        break;
                    case "single":
                        actual = XmlConvert.ToSingle((string)array0[i]);
                        expect = (float)array1[i];
                        break;
                    case "sbyte":
                        actual = XmlConvert.ToSByte((string)array0[i]);
                        expect = (SByte)array1[i];
                        break;
                    case "timespan":
                        actual = XmlConvert.ToTimeSpan((string)array0[i]);
                        expect = (TimeSpan)array1[i];
                        break;
                    case "uint16":
                        actual = XmlConvert.ToUInt16((string)array0[i]);
                        expect = (UInt16)array1[i];
                        break;
                    case "uint32":
                        actual = XmlConvert.ToUInt32((string)array0[i]);
                        expect = (UInt32)array1[i];
                        break;
                    case "uint64":
                        actual = XmlConvert.ToUInt64((string)array0[i]);
                        expect = (UInt64)array1[i];
                        break;
                    case "guid":
                        actual = XmlConvert.ToGuid((string)array0[i]);
                        expect = (Guid)array1[i];
                        break;
                    case "datetimeOffset":
                        actual = XmlConvert.ToDateTimeOffset((string)array0[i]);
                        expect = array1[i];
                        break;
                    case "datetimeOffset.format":
                        actual = XmlConvert.ToDateTimeOffset((string)array0[i], format[i]);
                        expect = array1[i];
                        break;
                    case "datetimeOffset.formats":
                        actual = XmlConvert.ToDateTimeOffset((string)array0[i], formats);
                        expect = array1[i];
                        break;
                    default:
                        throw new Exception("Unknown type " + type);
                }
                CError.Compare(actual, expect, (string)array0[i] + " Object Compare");
            }
            return TEST_PASS;
        }

        //[Variation("ToBoolean - valid cases")]
        public int ToType1()
        {
            object[] array0 = { "true", "false", "1", "0" };
            object[] array1 = { true, false, true, false };
            return TestValid(array0, array1, "bool");
        }

        //[Variation("ToBoolean - invalid cases")]

        //[Variation("ToDecimal - invalid cases")]
        public int ToType10()
        {
            string[] array = { "54,55", "12.3E2", "34A5.00", "+++32.0", "---32.0", "INF", null };

            return TestInvalid(array, "decimal");
        }

        //[Variation("ToDouble - valid cases")]
        public int ToType11()
        {
            object[] array0 = { "145896.2334", "12.3E+2", "  -458.238", "INF", "-INF", "278.62452469864978e-23", "0", "-0", "NaN" };
            object[] array1 = { 145896.2334, (double)1230, -458.238, Double.PositiveInfinity, Double.NegativeInfinity, 278.62452469864978e-23, (double)0, (double)0, Double.NaN };

            return TestValid(array0, array1, "double");
        }

        //[Variation("ToDouble - invalid cases")]
        public int ToType12()
        {
            string[] array = { "54,55", "12.3E+-2", "34A5.00", "+++32.0", "---32.0", null, "\uD812", "\uDF20" };

            return TestInvalid(array, "double");
        }

        //[Variation("ToInt16 - valid cases")]
        public int ToType13()
        {
            object[] array0 = { "5896", " -458" };
            object[] array1 = { (Int16)5896, (Int16)(-458) };

            return TestValid(array0, array1, "Int16");
        }

        //[Variation("ToInt16 - invalid cases")]
        public int ToType14()
        {
            string[] array = { "54.0", "12,24", "34A5", "+++32", "---32", null, "\uD812", "\uDF20" };

            return TestInvalid(array, "Int16");
        }

        //[Variation("ToInt32 - valid cases")]
        public int ToType15()
        {
            object[] array0 = { "5896", " -458" };
            object[] array1 = { 5896, -458 };
            return TestValid(array0, array1, "Int32");
        }

        //[Variation("ToInt32 - invalid cases")]
        public int ToType16()
        {
            string[] array = { "54.0", "12,24", "34A5", "+++32", "---32", null, "\uD812", "\uDF20" };

            return TestInvalid(array, "Int32");
        }

        //[Variation("ToInt64 - valid cases")]
        public int ToType17()
        {
            object[] array0 = { "5896", " -458" };
            object[] array1 = { (Int64)5896, (Int64)(-458) };
            return TestValid(array0, array1, "Int64");
        }

        //[Variation("ToInt64 - invalid cases")]
        public int ToType18()
        {
            string[] array = { "54.0", "12,24", "34A5", "+++32", "---32", null, "\uD812", "\uDF20" };

            return TestInvalid(array, "Int64");
        }

        //[Variation("ToSingle - valid cases")]
        public int ToType19()
        {
            object[] array0 = { "145896.2334", "12.3E+2", " -458.238", "INF", "-INF", "2.646978e-23", "0", "-0", "NaN" };
            object[] array1 = { (float)145896.2334, (float)1230, (float)-458.238, Single.PositiveInfinity, Single.NegativeInfinity, (float)2.646978e-23, (float)0, (float)0, Single.NaN };
            return TestValid(array0, array1, "single");
        }

        public int ToType2()
        {
            string[] array = { "trues", "falSe ", "11", "00", null, "\uD812", "\uDF20" };

            return TestInvalid(array, "bool");
        }

        //[Variation("ToSingle - invalid cases")]
        public int ToType20()
        {
            string[] array = { "54,55", "12.3E+-2", "34A5.00", "+++32.0", "---32.0", null, "\uD812", "\uDF20" };

            return TestInvalid(array, "single");
        }

        //[Variation("ToSByte - valid cases")]
        public int ToType21()
        {
            object[] array0 = { "-1", "124", "127", "0", " -128" };
            object[] array1 = { (SByte)(-1), (SByte)124, (SByte)127, (SByte)0, (SByte)(-128) };
            return TestValid(array0, array1, "sbyte");
        }

        //[Variation("ToSByte - invalid cases")]
        public int ToType22()
        {
            string[] array = { "1 2 ", "0x45", "128", "0.0", "1,0", "-129", null, "\uD812", "\uDF20" };

            return TestInvalid(array, "sbyte");
        }

        //[Variation("ToTimeSpan - valid cases")]
        public int ToType23()
        {
            object[] array0 = { "P6DT07H45M58.45S", "P1Y1M6DT07H45M58S", " PT00H07M45S", "-P6DT07H45M58.45S", "P6DT07H45M58.4521547S", "P100Y250M362DT455H2542M1245.4521547S", "P1D", "P1M", "P1YT1S", "P0Y", "P0M", "P0D", "P0DT0S", "PT0.0S" };
            object[] array1 = { new TimeSpan(6, 7, 45, 58, 450), new TimeSpan(401, 7, 45, 58), new TimeSpan(0, 7, 45), -new TimeSpan(6, 7, 45, 58, 450), (new TimeSpan(6, 7, 45, 58)).Add(new TimeSpan(4521547)), (new TimeSpan(44482, 17, 42, 45)).Add(new TimeSpan(4521547)), new TimeSpan(1, 0, 0, 0), new TimeSpan(30, 0, 0, 0), new TimeSpan(365, 0, 0, 1), new TimeSpan(0, 0, 0, 0), new TimeSpan(0, 0, 0, 0), new TimeSpan(0, 0, 0, 0), new TimeSpan(0, 0, 0, 0), new TimeSpan(0, 0, 0, 0) };
            return TestValid(array0, array1, "timespan");
        }

        //[Variation("ToTimeSpan - invalid cases")]
        public int ToType24()
        {
            string[] array = { "6.07/45/58.45", "6.25:45:58", "-6.25:45:58", "6.07:45:58.45", "P", "P1YT", "P-1347M", null, "\uD812", "\uDF20" };

            return TestInvalid(array, "timespan");
        }

        //[Variation("ToUInt16 - valid cases")]
        public int ToType25()
        {
            object[] array0 = { "5896", " 458" };
            object[] array1 = { (UInt16)5896, (UInt16)458 };

            return TestValid(array0, array1, "uint16");
        }

        //[Variation("ToUInt16 - invalid cases")]
        public int ToType26()
        {
            string[] array = { "54.0", "12,24", "34A5", "+++32", "-25", "--25", null, "\uD812", "\uDF20" };

            return TestInvalid(array, "uint16");
        }

        //[Variation("ToUInt32 - valid cases")]
        public int ToType27()
        {
            object[] array0 = { "5896", " 458" };
            object[] array1 = { (UInt32)5896, (UInt32)458 };

            return TestValid(array0, array1, "uint32");
        }

        //[Variation("ToUInt32 - invalid cases")]
        public int ToType28()
        {
            string[] array = { "54.0", "12,24", "34A5", "+++32", "-25", "--25", null, "\uD812", "\uDF20" };

            return TestInvalid(array, "uint32");
        }

        //[Variation("ToUInt64 - valid cases")]
        public int ToType29()
        {
            object[] array0 = { "5896", " 458" };
            object[] array1 = { (UInt64)5896, (UInt64)458 };
            return TestValid(array0, array1, "uint64");
        }

        public int ToType3()
        {
            object[] array0 = { "124", "255", "0" };
            object[] array1 = { (byte)124, (byte)255, (byte)0 };
            return TestValid(array0, array1, "byte");
        }

        //[Variation("ToUInt64 - invalid cases")]
        public int ToType30()
        {
            string[] array = { "54.0", "12,24", "34A5", "+++32", "-25", "--25", null, "\uD812", "\uDF20" };

            return TestInvalid(array, "uint64");
        }

        //[Variation("ToGuid - valid cases")]
        public int ToType31()
        {
            object[] array0 = { "0000000a-000b-000c-0001-020304050607", " 0000000a-000b-000c-0001-020304050607" };
            object[] array1 = { new Guid(0xa, 0xb, 0xc, 0, 1, 2, 3, 4, 5, 6, 7), new Guid(0xa, 0xb, 0xc, 0, 1, 2, 3, 4, 5, 6, 7) };
            return TestValid(array0, array1, "guid");
        }

        //[Variation("ToGuid - invalid cases")]
        public int ToType32()
        {
            string[] array = { "0000000a-", "0000000Z-000b-000c-0001-020304050607", "0000000a-000b-000c-0001-0203040506070", null, "\uD812", "\uDF20" };

            return TestInvalid(array, "guid");
        }

        //[Variation("ToString(integral type) - valid cases")]
        public int ToType33()
        {
            CError.Compare(XmlConvert.ToString((ushort)4582), "4582", "ushort");
            CError.Compare(XmlConvert.ToString((ulong)4582), "4582", "ulong");
            CError.Compare(XmlConvert.ToString((long)(-4582)), "-4582", "long");
            CError.Compare(XmlConvert.ToString(-4582), "-4582", "int");
            CError.Compare(XmlConvert.ToString((short)(-4582)), "-4582", "short");
            return TEST_PASS;
        }

        //[Variation("ToString(float) - valid cases")]
        public int ToType34()
        {
            CError.Compare(XmlConvert.ToString((float)(-4582.24)), "-4582.24", "float");
            CError.Compare(XmlConvert.ToString(Single.PositiveInfinity), "INF", "float");
            CError.Compare(XmlConvert.ToString(Single.NegativeInfinity), "-INF", "float");
            CError.Compare(XmlConvert.ToString((float)2.646978e-23), "2.646978E-23", "float");
            CError.Compare(XmlConvert.ToString((float)0), "0", "float");
            CError.Compare(XmlConvert.ToString(Single.NaN), "NaN", "float");
            CError.Compare(XmlConvert.ToString((float)-0), "0", "float");
            return TEST_PASS;
        }

        //[Variation("ToString(double) - valid cases")]
        public int ToType35()
        {
            // double
            CError.Compare(XmlConvert.ToString(-4582.24), "-4582.24", "double");
            CError.Compare(XmlConvert.ToString(Double.PositiveInfinity), "INF", "double");
            CError.Compare(XmlConvert.ToString(Double.NegativeInfinity), "-INF", "double");
            CError.Compare(XmlConvert.ToString(243.657745094698e-23), "2.43657745094698E-21", "double");
            CError.Compare(XmlConvert.ToString((double)0), "0", "double");
            CError.Compare(XmlConvert.ToString(Double.NaN), "NaN", "double");
            CError.Compare(XmlConvert.ToString((double)-0), "0", "double");
            return TEST_PASS;
        }

        //[Variation("ToString(decimal) - valid cases")]
        public int ToType36()
        {
            // decimal
            CError.Compare(XmlConvert.ToString((decimal)(-4582.24)), "-4582.24", "decimal");
            CError.Compare(XmlConvert.ToString((decimal)0.00005), "0.00005", "decimal");
            CError.Compare(XmlConvert.ToString((decimal)0.0000000000000000000000001), "0.0000000000000000000000001", "decimal");
            CError.Compare(XmlConvert.ToString((decimal)145896.233422154), "145896.233422154", "decimal");
            CError.Compare(XmlConvert.ToString(Convert.ToDecimal("0.123456789012345678", CultureInfo.InvariantCulture)), "0.123456789012345678", "decimal");
            CError.Compare(XmlConvert.ToString((decimal)1234567890123456789), (1234567890123456789M).ToString(), "decimal");
            CError.Compare(XmlConvert.ToString((decimal)-0), "0", "decimal");
            return TEST_PASS;
        }

        //[Variation("ToString(byte, bool, char) - valid cases")]
        public int ToType37()
        {
            CError.Compare(XmlConvert.ToString((byte)58), "58", "byte");
            CError.Compare(XmlConvert.ToString((sbyte)(-58)), "-58", "sbyte");
            CError.Compare(XmlConvert.ToString(true), "true", "bool true");
            CError.Compare(XmlConvert.ToString(false), "false", "bool false");
            CError.Compare(XmlConvert.ToString('B'), "B", "char");
            return TEST_PASS;
        }

        //[Variation("ToString(TimeSpan) - valid cases")]
        public int ToType38()
        {
            // TimeSpan
            CError.Compare(XmlConvert.ToString(new TimeSpan(6, 7, 45, 58, 450)), "P6DT7H45M58.45S", "timespan");
            CError.Compare(XmlConvert.ToString(-new TimeSpan(6, 7, 45, 58, 450)), "-P6DT7H45M58.45S", "timespan");
            TimeSpan ts = (new TimeSpan(6, 7, 45, 58)).Add(new TimeSpan(4521547));
            CError.Compare(XmlConvert.ToString(ts), "P6DT7H45M58.4521547S", "timespan");
            CError.Compare(XmlConvert.ToString(new TimeSpan(1, 0, 0, 0)), "P1D", "timespan");
            CError.Compare(XmlConvert.ToString(new TimeSpan(365, 0, 0, 1)), "P365DT1S", "timespan");
            CError.Compare(XmlConvert.ToString(new TimeSpan(0, 0, 0, 0)), "PT0S", "timespan");
            return TEST_PASS;
        }

        //[Variation("ToString(Guid) - valid cases")]
        public int ToType39()
        {
            CError.Compare(XmlConvert.ToString(new Guid(0xa, 0xb, 0xc, 0, 1, 2, 3, 4, 5, 6, 7)), "0000000a-000b-000c-0001-020304050607", "guid");
            return TEST_PASS;
        }

        public int ToType4()
        {
            string[] array = { "-1", "1 2 ", "0x45", "256", "0.0", "1,0", null, "\uD812", "\uDF20" };

            return TestInvalid(array, "byte");
        }

        //[Variation("ToString(int16, in32, int64, uint32, uint64) - valid cases")]
        public int ToType40a()
        {
            CError.Compare(XmlConvert.ToString(UInt64.MaxValue), "18446744073709551615", "UInt64.MaxValue");
            CError.Compare(XmlConvert.ToString(UInt32.MaxValue), "4294967295", "UInt32.MaxValue");
            CError.Compare(XmlConvert.ToString(Int64.MaxValue), "9223372036854775807", "Int64.MaxValue");
            CError.Compare(XmlConvert.ToString(Int32.MaxValue), "2147483647", "Int32.MaxValue");
            CError.Compare(XmlConvert.ToString(Int16.MaxValue), "32767", "Int16.MaxValue");
            return TEST_PASS;
        }

        //[Variation("PositiveZero and NegativeZero double, float, bug regression 76523")]
        public int ToType42()
        {
            float negZeroFloat = -0f;
            double negZeroDouble = -0d;

            CError.Compare(XmlConvert.ToString(negZeroFloat), "-0", "float - negative Zero");
            CError.Compare(XmlConvert.ToString(negZeroDouble), "-0", "double - negative Zero");
            return TEST_PASS;
        }

        public int ToType5()
        {
            object[] array0 = { "1", "A", "\t", "\0", "\uD812", "\uDF20" };
            object[] array1 = { '1', 'A', '\t', '\0', '\uD812', '\uDF20' };
            return TestValid(array0, array1, "char");
        }

        //[Variation("ToString(DateTime,XmlDateTimeSerializationMode.Local) - valid cases")]
        public int ToType43()
        {
            DateTime dtLocal = new DateTime();
            // DateTime
            DateTime dt = new DateTime(2002, 12, 30, 23, 15, 55, 100);
            string expDateTime = (TimeZoneInfo.Local.GetUtcOffset(dt).Hours < 0) ?
                String.Format("2002-12-30T23:15:55.1-0{0}:00", Math.Abs(TimeZoneInfo.Local.GetUtcOffset(dt).Hours)) :
                String.Format("2002-12-30T23:15:55.1+0{0}:00", Math.Abs(TimeZoneInfo.Local.GetUtcOffset(dt).Hours));
            CError.Equals(XmlConvert.ToString(dt, XmlDateTimeSerializationMode.Local), expDateTime, "datetime1");
            dt = new DateTime(1, 1, 1, 23, 59, 59);
            dt = dt.AddTicks(9999999);
            expDateTime = (TimeZoneInfo.Local.GetUtcOffset(dt).Hours < 0) ?
                String.Format("0001-01-01T23:59:59.9999999-0{0}:00", Math.Abs(TimeZoneInfo.Local.GetUtcOffset(dt).Hours)) :
                String.Format("0001-01-01T23:59:59.9999999+0{0}:00", Math.Abs(TimeZoneInfo.Local.GetUtcOffset(dt).Hours));
            CError.Equals(XmlConvert.ToString(dt, XmlDateTimeSerializationMode.Local), expDateTime, "millisecs");

            // new 05/2002
            dt = new DateTime(2002, 12, 30, 23, 15, 55);
            expDateTime = (TimeZoneInfo.Local.GetUtcOffset(dt).Hours < 0) ?
                String.Format("2002-12-30T23:15:55-0{0}:00", Math.Abs(TimeZoneInfo.Local.GetUtcOffset(dt).Hours)) :
                String.Format("2002-12-30T23:15:55+0{0}:00", Math.Abs(TimeZoneInfo.Local.GetUtcOffset(dt).Hours));
            CError.Equals(XmlConvert.ToString(dt, XmlDateTimeSerializationMode.Local), expDateTime, "datetime2");

            // Read in Universal Time 
            dt = new DateTime();
            dtLocal = XmlConvert.ToDateTime("2002-12-31T07:15:55.0000000Z", XmlDateTimeSerializationMode.Local); // this is just to get the offset
            dt = XmlConvert.ToDateTime("2002-12-31T07:15:55.0000000Z", XmlDateTimeSerializationMode.RoundtripKind);
            if (TimeZoneInfo.Local.GetUtcOffset(dt).Hours == -8 || TimeZoneInfo.Local.GetUtcOffset(dt).Hours == 9)
            {   // only -08:00 and 09:00 works
                expDateTime = (TimeZoneInfo.Local.GetUtcOffset(dt).Hours < 0) ?
                    String.Format("2002-12-30T23:15:55-0{0}:00", Math.Abs(TimeZoneInfo.Local.GetUtcOffset(dtLocal).Hours)) :
                    String.Format("2002-12-31T16:15:55+0{0}:00", Math.Abs(TimeZoneInfo.Local.GetUtcOffset(dtLocal).Hours));
                CError.Equals(XmlConvert.ToString(dt, XmlDateTimeSerializationMode.Local), expDateTime, "Read in Universal Time");
            }

            // Read in Local Time 
            dt = new DateTime();
            dt = XmlConvert.ToDateTime("2002-12-31T07:15:55.0000000-00:00", XmlDateTimeSerializationMode.RoundtripKind);
            if (TimeZoneInfo.Local.GetUtcOffset(dt).Hours == -8 || TimeZoneInfo.Local.GetUtcOffset(dt).Hours == 9)
            {   // only -08:00 and 09:00 works
                expDateTime = (TimeZoneInfo.Local.GetUtcOffset(dt).Hours < 0) ?
                    String.Format("2002-12-30T23:15:55-0{0}:00", Math.Abs(TimeZoneInfo.Local.GetUtcOffset(dt).Hours)) :
                    String.Format("2002-12-31T16:15:55+0{0}:00", Math.Abs(TimeZoneInfo.Local.GetUtcOffset(dt).Hours));
                CError.Equals(XmlConvert.ToString(dt, XmlDateTimeSerializationMode.Local), expDateTime, "Read in Local Time");
            }
            // Read in Unspecified Time 
            dt = new DateTime();
            dt = XmlConvert.ToDateTime("2002-12-31T07:15:55.0000000", XmlDateTimeSerializationMode.RoundtripKind);
            expDateTime = (TimeZoneInfo.Local.GetUtcOffset(dt).Hours < 0) ?
                String.Format("2002-12-31T07:15:55-0{0}:00", Math.Abs(TimeZoneInfo.Local.GetUtcOffset(dt).Hours)) :
                String.Format("2002-12-31T07:15:55+0{0}:00", Math.Abs(TimeZoneInfo.Local.GetUtcOffset(dt).Hours));
            CError.Equals(XmlConvert.ToString(dt, XmlDateTimeSerializationMode.Local), expDateTime, "Read in UnSpecified Time");
            return TEST_PASS;
        }

        //[Variation("ToString(DateTime,XmlDateTimeSerializationMode.RoundtripKind) - valid cases")]
        public int ToType44()
        {
            // Read in Universal Time 
            DateTime dt = new DateTime();
            dt = XmlConvert.ToDateTime("2002-12-31T07:15:55.0000000Z", XmlDateTimeSerializationMode.RoundtripKind);
            CError.Compare(dt.Kind, DateTimeKind.Utc, "Utc expected");
            String expDateTime = "2002-12-31T07:15:55Z";
            CError.Equals(XmlConvert.ToString(dt, XmlDateTimeSerializationMode.RoundtripKind), expDateTime, "Read in Universal Time");

            // Read in Local Time 
            dt = new DateTime();
            dt = XmlConvert.ToDateTime("2002-12-31T07:15:55.0000000-00:00", XmlDateTimeSerializationMode.RoundtripKind);
            if (TimeZoneInfo.Local.GetUtcOffset(dt).Hours == -8 || TimeZoneInfo.Local.GetUtcOffset(dt).Hours == 9)
            {
                CError.Equals(dt.Kind, DateTimeKind.Local, "Local expected");
                expDateTime = (TimeZoneInfo.Local.GetUtcOffset(dt).Hours < 0) ?
                    String.Format("2002-12-30T23:15:55-0{0}:00", Math.Abs(TimeZoneInfo.Local.GetUtcOffset(dt).Hours)) :
                    String.Format("2002-12-31T16:15:55+0{0}:00", Math.Abs(TimeZoneInfo.Local.GetUtcOffset(dt).Hours));
                CError.Equals(XmlConvert.ToString(dt, XmlDateTimeSerializationMode.RoundtripKind), expDateTime, "Read in Local Time");
            }

            // Read in Unspecified Time 
            dt = new DateTime();
            dt = XmlConvert.ToDateTime("2002-12-31T07:15:55.0000000", XmlDateTimeSerializationMode.RoundtripKind);
            CError.Equals(dt.Kind, DateTimeKind.Unspecified, "unspecified time expected");
            expDateTime = "2002-12-31T07:15:55";
            CError.Equals(XmlConvert.ToString(dt, XmlDateTimeSerializationMode.RoundtripKind), expDateTime, "Read in UnSpecified Time");
            return TEST_PASS;
        }

        //[Variation("ToString(DateTime,XmlDateTimeSerializationMode.Utc) - valid cases")]
        public int ToType45()
        {
            // Read in Universal Time 
            DateTime dt = new DateTime();
            dt = XmlConvert.ToDateTime("2002-12-31T07:15:55", XmlDateTimeSerializationMode.RoundtripKind);
            CError.Compare(dt.Kind, DateTimeKind.Unspecified, "Utc expected");
            String expDateTime = "2002-12-31T07:15:55Z";
            CError.Compare(XmlConvert.ToString(dt, XmlDateTimeSerializationMode.Utc), expDateTime, "Read in Universal Time");

            // Read in Local Time 
            dt = new DateTime();
            dt = XmlConvert.ToDateTime("2002-12-31T07:15:55.0000000-00:00", XmlDateTimeSerializationMode.RoundtripKind);
            CError.Compare(dt.Kind, DateTimeKind.Local, "Local expected");
            expDateTime = "2002-12-31T07:15:55Z";
            CError.Compare(XmlConvert.ToString(dt, XmlDateTimeSerializationMode.Utc), expDateTime, "Read in Local Time");

            // Read in Unspecified Time 
            dt = new DateTime();
            dt = XmlConvert.ToDateTime("2002-12-31T07:15:55.0000000", XmlDateTimeSerializationMode.RoundtripKind);
            CError.Compare(dt.Kind, DateTimeKind.Unspecified, "unspecified time expected");
            expDateTime = "2002-12-31T07:15:55Z";
            CError.Compare(XmlConvert.ToString(dt, XmlDateTimeSerializationMode.Utc), expDateTime, "Read in UnSpecified Time");
            return TEST_PASS;
        }

        //[Variation("ToString(DateTime,XmlDateTimeSerializationMode.Unspecified) - valid cases")]
        public int ToType46()
        {
            // Read in Universal Time 
            DateTime dt = new DateTime();
            dt = XmlConvert.ToDateTime("2002-12-31T07:15:55.0000000Z", XmlDateTimeSerializationMode.RoundtripKind);
            CError.Equals(dt.Kind, DateTimeKind.Utc, "Utc expected");
            String expDateTime = "2002-12-31T07:15:55";
            CError.Equals(XmlConvert.ToString(dt, XmlDateTimeSerializationMode.Unspecified), expDateTime, "Read in Universal Time");

            // Read in Local Time 
            dt = new DateTime();
            dt = XmlConvert.ToDateTime("2002-12-31T07:15:55.0000000-00:00", XmlDateTimeSerializationMode.RoundtripKind);
            CError.Equals(dt.Kind, DateTimeKind.Local, "Local expected");
            if (TimeZoneInfo.Local.GetUtcOffset(dt).Hours == -8 || TimeZoneInfo.Local.GetUtcOffset(dt).Hours == 9)
            {
                expDateTime = (TimeZoneInfo.Local.GetUtcOffset(dt).Hours < 0) ? "2002-12-30T23:15:55" : "2002-12-31T16:15:55";
                CError.Equals(XmlConvert.ToString(dt, XmlDateTimeSerializationMode.Unspecified), expDateTime, "Read in Local Time");
            }

            // Read in Unspecified Time 
            dt = new DateTime();
            dt = XmlConvert.ToDateTime("2002-12-31T07:15:55.0000000", XmlDateTimeSerializationMode.RoundtripKind);
            CError.Equals(dt.Kind, DateTimeKind.Unspecified, "unspecified time expected");
            expDateTime = "2002-12-31T07:15:55";
            CError.Equals(XmlConvert.ToString(dt, XmlDateTimeSerializationMode.Unspecified), expDateTime, "Read in UnSpecified Time");
            return TEST_PASS;
        }

        //[Variation("ToDateTime(string,XmlDateTimeSerializationMode.RoundtripKind)- valid cases")]
        public int ToType47()
        {
            TimeSpan offset2002_01_09 = TimeZoneInfo.Local.GetUtcOffset(new DateTime(2002, 01, 09));
            TimeSpan offsetNow = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);

            object[] array0 = { "2002-12-30", "2002-12-30T23:15:55.100", "2002-01-09T04:02:08.145", "23:15:55.1004555", "2002-01-09T04:02:08", "23:15:55", "2002-01-09T04:02:08", "2002-01-09T04:02:08Z", "2002-01-09Z", "04:02:08Z", "2002-01-09T04:02:08-05:00", "2002-01-09-05:00", "04:02:08-05:00", "0002-01-09T04:02:08.21", "0002-01-09", "2002-02" };
            object[] array1 = { new DateTime(2002, 12, 30), new DateTime(2002, 12, 30, 23, 15, 55, 100), new DateTime(2002, 1, 9, 4, 2, 8, 145), (new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 15, 55, 0)).AddTicks(1004555), new DateTime(2002, 1, 9, 4, 2, 8, 0), new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 15, 55, 0), (new DateTime(2002, 1, 9, 4, 2, 8, 0)).AddMilliseconds(0.1458925435), (new DateTime(2002, 1, 9, 4, 2, 8, 0)), (new DateTime(2002, 1, 9, 0, 0, 0, 0)), (new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 4, 2, 8, 0)), (new DateTime(2002, 1, 9, 4, 2, 8, 0)).Add(offset2002_01_09 + new TimeSpan(5, 0, 0)), (new DateTime(2002, 1, 9, 0, 0, 0, 0)).Add(offset2002_01_09 + new TimeSpan(5, 0, 0)), (new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 4, 2, 8, 0)).Add(offsetNow + new TimeSpan(5, 0, 0)), new DateTime(2, 1, 9, 4, 2, 8, 210), new DateTime(2, 1, 9, 0, 0, 0, 0), new DateTime(2002, 2, 1, 0, 0, 0, 0) };
            return TestValid(array0, array1, "datetime.Roundtrip");
        }
        //[Variation("ToDateTime(string,XmlDateTimeSerializationMode.Local)- valid cases")]
        public int ToType48()
        {
            TimeSpan offset2002_01_09 = TimeZoneInfo.Local.GetUtcOffset(new DateTime(2002, 01, 09));
            TimeSpan offsetNow = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);

            object[] array0 = { "2002-12-30", "2002-12-30T23:15:55.100", "2002-01-09T04:02:08.145", "23:15:55.1004555", "2002-01-09T04:02:08", "23:15:55", "2002-01-09T04:02:08", "2002-01-09T04:02:08Z", "2002-01-09Z", "2002-01-09T04:02:08-05:00", "2002-01-09-05:00", "04:02:08-05:00", "0002-01-09T04:02:08.21", "0002-01-09", "2002-02" };
            object[] array1 = { new DateTime(2002, 12, 30), new DateTime(2002, 12, 30, 23, 15, 55, 100), new DateTime(2002, 1, 9, 4, 2, 8, 145), (new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 15, 55, 0)).AddTicks(1004555), new DateTime(2002, 1, 9, 4, 2, 8, 0), new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 15, 55, 0), (new DateTime(2002, 1, 9, 4, 2, 8, 0)).AddMilliseconds(0.1458925435), (new DateTime(2002, 1, 9, 4, 2, 8, 0).Add(offset2002_01_09)), (new DateTime(2002, 1, 9).Add(offset2002_01_09)), (new DateTime(2002, 1, 9, 4, 2, 8, 0)).Add(offset2002_01_09 + new TimeSpan(5, 0, 0)), (new DateTime(2002, 1, 9, 0, 0, 0, 0)).Add(offset2002_01_09 + new TimeSpan(5, 0, 0)), (new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 4, 2, 8, 0)).Add(offsetNow + new TimeSpan(5, 0, 0)), new DateTime(2, 1, 9, 4, 2, 8, 210), new DateTime(2, 1, 9, 0, 0, 0, 0), new DateTime(2002, 2, 1, 0, 0, 0, 0) };

            return TestValid(array0, array1, "datetime.Local");
        }
        [Variation("ToDateTime(string,XmlDateTimeSerializationMode.Utc)- valid cases")]
        public int ToType49()
        {
            TimeSpan offset2002_01_09 = TimeZoneInfo.Local.GetUtcOffset(new DateTime(2002, 01, 09));
            TimeSpan offsetNow = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);

            object[] array0 = { "2002-12-30", "2002-12-30T23:15:55.100", "2002-01-09T04:02:08.145", "23:15:55.1004555", "2002-01-09T04:02:08", "23:15:55", "2002-01-09T04:02:08", "2002-01-09T04:02:08Z", "2002-01-09Z", "04:02:08Z", "2002-01-09T04:02:08-05:00", "2002-01-09-05:00", "04:02:08-05:00", "0002-01-09T04:02:08.21", "0002-01-09", "2002-02" };
            object[] array1 = { new DateTime(2002, 12, 30), new DateTime(2002, 12, 30, 23, 15, 55, 100), new DateTime(2002, 1, 9, 4, 2, 8, 145), (new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 15, 55, 0)).AddTicks(1004555), new DateTime(2002, 1, 9, 4, 2, 8, 0), new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 15, 55, 0), (new DateTime(2002, 1, 9, 4, 2, 8, 0)).AddMilliseconds(0.1458925435), (new DateTime(2002, 1, 9, 4, 2, 8, 0)), (new DateTime(2002, 1, 9, 0, 0, 0, 0)), (new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 4, 2, 8, 0)), (new DateTime(2002, 1, 9, 9, 2, 8, 0)), (new DateTime(2002, 1, 9, 5, 0, 0, 0)), (new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 4, 2, 8, 0)).Add(new TimeSpan(5, 0, 0)), new DateTime(2, 1, 9, 4, 2, 8, 210), new DateTime(2, 1, 9, 0, 0, 0, 0), new DateTime(2002, 2, 1, 0, 0, 0, 0) };
            return TestValid(array0, array1, "datetime.Utc");
        }

        //[Variation("ToDateTime(string,XmlDateTimeSerializationMode.Unspecified)- valid cases")]
        public int ToType50()
        {
            TimeSpan offset2002_01_09 = TimeZoneInfo.Local.GetUtcOffset(new DateTime(2002, 01, 09));
            TimeSpan offsetNow = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
            TimeSpan ts = (TimeZoneInfo.Local.IsDaylightSavingTime(DateTime.Now)) ? new TimeSpan(4, 0, 0) : new TimeSpan(5, 0, 0);

            object[] array0 = { "2002-12-30", "2002-12-30T23:15:55.100", "2002-01-09T04:02:08.145", "23:15:55.1004555", "2002-01-09T04:02:08", "23:15:55", "2002-01-09T04:02:08", "2002-01-09T04:02:08Z", "2002-01-09Z", "2008-02-29T04:02:08-05:00", "2012-02-29-05:00", "04:02:08-05:00", "0002-01-09T04:02:08.21", "0002-01-09", "2016-02-29" };
            object[] array1 = { new DateTime(2002, 12, 30), new DateTime(2002, 12, 30, 23, 15, 55, 100), new DateTime(2002, 1, 9, 4, 2, 8, 145), (new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 15, 55, 0)).AddTicks(1004555), new DateTime(2002, 1, 9, 4, 2, 8, 0), new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 15, 55, 0), (new DateTime(2002, 1, 9, 4, 2, 8, 0)).AddMilliseconds(0.1458925435), (new DateTime(2002, 1, 9, 4, 2, 8, 0)), (new DateTime(2002, 1, 9, 0, 0, 0, 0)), (new DateTime(2008, 2, 29, 4, 2, 8, 0)).Add(offsetNow + ts), (new DateTime(2012, 2, 29, 0, 0, 0, 0)).Add(offsetNow + ts), (new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 4, 2, 8, 0)).Add(offsetNow + new TimeSpan(5, 0, 0)), new DateTime(2, 1, 9, 4, 2, 8, 210), new DateTime(2, 1, 9, 0, 0, 0, 0), new DateTime(2016, 2, 29, 0, 0, 0, 0) };

            return TestValid(array0, array1, "datetime.Unspecified");
        }

        //[Variation("XmlConvert.ToDateTime(null, XmlDateTimeSerializationMode.Local", Param = XmlDateTimeSerializationMode.Local)]
        //[Variation("XmlConvert.ToDateTime(null, XmlDateTimeSerializationMode.RoundtripKind", Param = XmlDateTimeSerializationMode.RoundtripKind)]
        //[Variation("XmlConvert.ToDateTime(null, XmlDateTimeSerializationMode.Unspecified", Param = XmlDateTimeSerializationMode.Unspecified)]
        //[Variation("XmlConvert.ToDateTime(null, XmlDateTimeSerializationMode.Utc", Param = XmlDateTimeSerializationMode.Utc)]
        public int ToType51()
        {
            XmlDateTimeSerializationMode mode = (XmlDateTimeSerializationMode)this.CurVariation.Param;
            try
            {
                XmlConvert.ToDateTime(null, mode);
            }
            catch (NullReferenceException e)
            {
                CError.WriteLine(e.Message);
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        //[Variation("ToDateTimeOffset(String s) - valid cases", Param = "datetimeOffset")]
        //[Variation("ToDateTimeOffset(String s, String format) - valid cases", Param = "datetimeOffset.format")]
        //[Variation("ToDateTimeOffset(String s, String[] formats) - valid cases", Param = "datetimeOffset.formats")]
        public int ToType52()
        {
            var param = (string)CurVariation.Param;

            object[] array0 = { "2002-12-30", "23:15:55", "2002-01-09T04:02:08", "2002-01-09T04:02:08Z", "2002-01-09Z", "2002-01-09T04:02:08-05:00", "0002-01", "2016-02-29", "9999", "9999Z", "9999-12-31T12:59:59+14:00", "9999-12-31T12:59:59-11:00", "9999-12-31T12:59:59-10:59", "9999-12-31T12:59:59+13:59", "9999-12-31T23:59:59-00:00", "9999-12-31T23:59:59+14:00", "9998-12-31T12:59:59+14:00", "9998-12-31T12:59:59-14:00", "0002", "0001Z", "0002-01-01T00:00:00-14:00", "0002-01-01T00:00:00-13:59", "0002-01-01T00:00:00+00:00", "0002-01-01T00:00:00-00:00", "2008-02-29T23:59:59-14:00", "2012-02-29T23:59:59+14:00" };
            object[] array1 = { new DateTimeOffset(2002, 12, 30, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2002, 12, 30))), new DateTimeOffset(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 15, 55, TimeZoneInfo.Local.GetUtcOffset(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 15, 55))), (new DateTimeOffset(2002, 1, 9, 4, 2, 8, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2002, 1, 9)))).AddMilliseconds(0.1458925435), (new DateTimeOffset(2002, 1, 9, 4, 2, 8, TimeSpan.FromHours(0))).ToLocalTime(), (new DateTimeOffset(2002, 1, 9, 0, 0, 0, TimeSpan.FromHours(0))).ToLocalTime(), (new DateTimeOffset(2002, 1, 9, 4, 2, 8, new TimeSpan(-5, 0, 0))), new DateTimeOffset(2, 1, 1, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2, 1, 1))), new DateTimeOffset(2016, 2, 29, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2016, 2, 29))), new DateTimeOffset(9999, 1, 1, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(9999, 1, 1))), new DateTimeOffset(9999, 1, 1, 0, 0, 0, TimeSpan.FromHours(0)), new DateTimeOffset(9999, 12, 31, 12, 59, 59, TimeSpan.FromHours(14.0)), new DateTimeOffset(9999, 12, 31, 12, 59, 59, TimeSpan.FromHours(-11.0)), new DateTimeOffset(9999, 12, 31, 12, 59, 59, TimeSpan.FromHours(-10) + TimeSpan.FromMinutes(-59)), new DateTimeOffset(9999, 12, 31, 12, 59, 59, new TimeSpan(13, 59, 0)), new DateTimeOffset(9999, 12, 31, 23, 59, 59, TimeSpan.Zero), new DateTimeOffset(9999, 12, 31, 23, 59, 59, TimeSpan.FromHours(14.0)), new DateTimeOffset(9998, 12, 31, 12, 59, 59, TimeSpan.FromHours(14.0)), new DateTimeOffset(9998, 12, 31, 12, 59, 59, TimeSpan.FromHours(-14.0)), new DateTimeOffset(2, 1, 1, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2, 1, 1))), new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.FromHours(0)), new DateTimeOffset(2, 1, 1, 0, 0, 0, TimeSpan.FromHours(-14.0)), new DateTimeOffset(2, 1, 1, 0, 0, 0, TimeSpan.FromHours(-13) + TimeSpan.FromMinutes(-59)), new DateTimeOffset(2, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2008, 2, 29, 23, 59, 59, TimeSpan.FromHours(-14)), new DateTimeOffset(2012, 2, 29, 23, 59, 59, TimeSpan.FromHours(14)) };
            string[] format = { "yyyy-MM-dd", "HH:mm:ss", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ssZ", "yyyy-MM-ddZ", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM", "yyyy-MM-dd", "yyyy", "yyyyZ", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy", "yyyyZ", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz" };
            return TestValid(array0, array1, param, format);
        }

        //[Variation("ToDateTimeOffset(String s) with offset = hh:60 - valid", Param = "datetimeOffset")]
        public int ToType53()
        {
            var param = (string)CurVariation.Param;

            object[] array0 = { "9999-12-31T12:59:59-10:60", "9999-12-31T12:59:59+13:60", "9998-12-31T12:59:59-12:99", "9998-12-31T12:59:59+12:99", "0001-01-01T00:00:00-13:60", "2000-02-29T23:59:59.999999999999999+01:99", "2000-02-29T23:59:59.99999999999999999-00:99", "0001-01-01T00:00:00.99999999999999999999-13:60", "0001-01-01T00:00:00.99999999999999999-00:00", "9999-12-31T12:59:59.99999999999+13:60", "9999-12-31T12:59:59.99999999999999999999999999999-10:59", "2000-02-29T23:59:59.99999999999999999z", "0001-01-01T00:00:00.99999999999999999Z", "9999-12-31T12:59:59z", "9999-12-31T12:59:59.99999999999999999999999999999Z" };
            object[] array1 = { new DateTimeOffset(9999, 12, 31, 12, 59, 59, TimeSpan.FromHours(-11.0)), new DateTimeOffset(9999, 12, 31, 12, 59, 59, TimeSpan.FromHours(14.0)), new DateTimeOffset(9998, 12, 31, 12, 59, 59, TimeSpan.FromHours(-13) + TimeSpan.FromMinutes(-39)), new DateTimeOffset(9998, 12, 31, 12, 59, 59, TimeSpan.FromHours(13) + TimeSpan.FromMinutes(39)), new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.FromHours(-14.0)), new DateTimeOffset(2000, 3, 1, 0, 0, 0, TimeSpan.FromHours(2) + TimeSpan.FromMinutes(39)), new DateTimeOffset(2000, 3, 1, 0, 0, 0, TimeSpan.FromHours(-1) + TimeSpan.FromMinutes(-39)), new DateTimeOffset(1, 1, 1, 0, 0, 1, TimeSpan.FromHours(-14.0)), new DateTimeOffset(1, 1, 1, 0, 0, 1, TimeSpan.FromHours(-0)), new DateTimeOffset(9999, 12, 31, 13, 0, 0, TimeSpan.FromHours(+14.0)), new DateTimeOffset(9999, 12, 31, 13, 0, 0, TimeSpan.FromHours(-10) + TimeSpan.FromMinutes(-59)), new DateTimeOffset(2000, 3, 1, 0, 0, 0, TimeSpan.FromHours(0)), new DateTimeOffset(1, 1, 1, 0, 0, 1, TimeSpan.FromHours(-0)), new DateTimeOffset(9999, 12, 31, 12, 59, 59, TimeSpan.FromHours(0)), new DateTimeOffset(9999, 12, 31, 13, 0, 0, TimeSpan.FromHours(0)) };

            string[] format = { "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz" };
            return TestValid(array0, array1, param, format);
        }

        //[Variation("ToDateTimeOffset(String s, String format) with offset = hh:60 - invalid", Param = "datetimeOffset.format")]
        //[Variation("ToDateTimeOffset(String s, String[] formats) with offset = hh:60 - invalid", Param = "datetimeOffset.formats")]
        public int ToType54()
        {
            var param = (string)CurVariation.Param;
            string[] array = { "9999-12-31T12:59:59-10:60", "9999-12-31T12:59:59+13:60", "9998-12-31T12:59:59-13:60", "9998-12-31T12:59:59+13:60", "0001-01-01T00:00:00-13:60", "2000-02-29T23:59:59.999999999999999+13:60", "2000-02-29T23:59:59.99999999999999999-13:60", "0001-01-01T00:00:00.99999999999999999999-13:60", "0001-01-01T00:00:00.999999999999+00:60", "9999-12-31T12:59:59.99999999999+11:60", "9999-12-31T12:59:59.99999999999999999999999999999-10:60" };
            string[] format = { "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz" };
            return TestInvalid(array, param, format);
        }

        //[Variation("ToDateTimeOffset(String s) - invalid cases", Param = "datetimeOffset")]
        //[Variation("ToDateTimeOffset(String s, String format) - invalid cases", Param = "datetimeOffset.format")]
        //[Variation("ToDateTimeOffset(String s, String[] formats) - invalid cases", Param = "datetimeOffset.formats")]
        public int ToType55()
        {
            var param = (string)CurVariation.Param;
            string[] array = { "2002-30-12", "2002/30 12", "2002-12-32T23:15:55", "2002-12-10C23:15:55", "2002-01-09T04:61:08", "0000-01-09T04:02:08", "0000-01-09", "999-01-09T04:02:08", "999-01-09", "2002-01T04:02:08", "2002-01-", "2100-02-29T04:02:08", "2200-02-29T04:02:08", "2300-02-29T04:02:08", "2002-01-9T04:02:08", "2002-01-9", "2002-01-09T04:02:8", "02002-01-09T04:02:08", "02002-01-09", "02002-01-09T", ":02:08", "04:08", "04:02:8", "9999-12-31T12:59:59-12:00", "1/1/0001", "1:1:0001", "9999/12/31", "9999:12:31", "9998-12-31T12:59:59-15:00", "0001-01-01T00:00:00-99:00", "0001-01-01T00:00:00+00:165", "9998-12-31T12:59:59-13:61", "9998-12-31T12:59:59+13:61", "9999-12-31T12:59:59+15:00", "0001-01-01T00:00:00+00:01", "9999-12-31T23:59:59-00:01", "9999-12-31T23:59:59+14:01", "2100-02-29T23:59:59-14:00", "3000-02-29T23:59:59+14:00", "", null };

            string[] format = { "yyyy-MM-dd", "HH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", null, "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-dd", "HH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", null, null, "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz", "yyyy-MM-ddTHH:mm:sszzzzzz" };
            return TestInvalid(array, param, format);
        }

        //[Variation("ToString(DateTimeOffset) - valid cases")]
        public int ToType56()
        {
            var dto = new DateTimeOffset();
            CError.Equals(XmlConvert.ToString(dto), "0001-01-01T00:00:00Z", "datetimeOffset1");

            dto = new DateTimeOffset(2002, 12, 30, 0, 0, 0, 0, TimeSpan.FromHours(-8));
            CError.Equals(XmlConvert.ToString(dto), "2002-12-30T00:00:00-08:00", "datetimeOffset2");

            dto = new DateTimeOffset(2002, 12, 30, 23, 15, 55, 556, TimeSpan.Zero);
            CError.Equals(XmlConvert.ToString(dto), "2002-12-30T23:15:55.556Z", "datetimeOffset3");

            dto = new DateTimeOffset(1, 1, 1, 23, 59, 59, TimeSpan.Zero);
            dto = dto.AddTicks(9999999);
            CError.Equals(XmlConvert.ToString(dto), "0001-01-01T23:59:59.9999999Z", "millisecs");

            dto = new DateTimeOffset(2002, 12, 30, 23, 15, 55, TimeSpan.Zero);
            CError.Equals(XmlConvert.ToString(dto), "2002-12-30T23:15:55Z", "datetimeOffset4");

            dto = new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.FromHours(0));
            CError.Equals(XmlConvert.ToString(dto), "0001-01-01T00:00:00Z", "datetimeOffset5");

            dto = new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.FromHours(-14));
            CError.Equals(XmlConvert.ToString(dto), "0001-01-01T00:00:00-14:00", "datetimeOffset6");

            dto = new DateTimeOffset(9999, 12, 31, 23, 59, 59, TimeSpan.FromHours(14));
            CError.Equals(XmlConvert.ToString(dto), "9999-12-31T23:59:59+14:00", "datetimeOffset7");

            dto = new DateTimeOffset(9999, 12, 31, 23, 59, 59, TimeSpan.FromHours(0));
            CError.Equals(XmlConvert.ToString(dto), "9999-12-31T23:59:59Z", "datetimeOffset8");

            dto = DateTimeOffset.MaxValue;
            CError.Equals(XmlConvert.ToString(dto), "9999-12-31T23:59:59.9999999Z", "datetimeOffset10");

            dto = DateTimeOffset.MinValue;
            CError.Equals(XmlConvert.ToString(dto), "0001-01-01T00:00:00Z", "datetimeOffset11");

            dto = new DateTimeOffset(1, 1, 1, 0, 0, 0, 1, TimeSpan.FromHours(-0));
            CError.Equals(XmlConvert.ToString(dto), "0001-01-01T00:00:00.001Z", "datetimeOffset14");

            dto = new DateTimeOffset(9999, 12, 31, 23, 59, 59, 999, TimeSpan.FromHours(14));
            CError.Equals(XmlConvert.ToString(dto), "9999-12-31T23:59:59.999+14:00", "datetimeOffset15");

            dto = new DateTimeOffset(1, 1, 1, 0, 0, 0, 999, TimeSpan.FromHours(-14));
            CError.Equals(XmlConvert.ToString(dto), "0001-01-01T00:00:00.999-14:00", "datetimeOffset16");

            dto = new DateTimeOffset(9999, 12, 31, 23, 59, 59, 125, TimeSpan.FromHours(14));
            CError.Equals(XmlConvert.ToString(dto), "9999-12-31T23:59:59.125+14:00", "datetimeOffset17");

            dto = new DateTimeOffset(2000, 2, 29, 23, 59, 59, 999, TimeSpan.FromHours(-14));
            CError.Equals(XmlConvert.ToString(dto), "2000-02-29T23:59:59.999-14:00", "datetimeOffset18");

            dto = new DateTimeOffset(2000, 2, 29, 23, 59, 59, 444, TimeSpan.FromHours(+14));
            CError.Equals(XmlConvert.ToString(dto), "2000-02-29T23:59:59.444+14:00", "datetimeOffset19");

            dto = new DateTimeOffset(2000, 2, 29, 23, 59, 59, 999, TimeSpan.FromHours(-14));
            CError.Equals(XmlConvert.ToString(dto), "2000-02-29T23:59:59.999-14:00", "datetimeOffset20");
            return TEST_PASS;
        }

        //[Variation("ToString(DateTimeOffset, format) - valid cases")]
        public int ToType57()
        {
            var dto = new DateTimeOffset();
            CError.Equals(XmlConvert.ToString(dto, "yyyy-MM"), "0001-01", "datetimeOffset1");

            dto = new DateTimeOffset();
            CError.Equals(XmlConvert.ToString(dto, "yyyy"), "0001", "datetimeOffset2");

            dto = new DateTimeOffset();
            CError.Equals(XmlConvert.ToString(dto, null), "01/01/0001 00:00:00 +00:00", "datetimeOffset3");

            string s = XmlConvert.ToString(XmlConvert.ToDateTimeOffset("2002-12-30"), "yyyy-MM-dd");
            CError.Equals(s, "2002-12-30", "datetimeOffset");

            dto = new DateTimeOffset(2002, 12, 30, 23, 15, 55, TimeSpan.Zero);
            CError.Equals(XmlConvert.ToString(dto, "yyyy-MM-ddTHH:mm:sszzzzzz"), "2002-12-30T23:15:55+00:00", "datetimeOffset4");

            dto = new DateTimeOffset(1, 1, 1, 23, 59, 59, TimeSpan.Zero);
            dto = dto.AddTicks(9999999);
            CError.Equals(XmlConvert.ToString(dto, "yyyy-MM-ddTHH:mm:sszzzzzz"), "0001-01-01T23:59:59+00:00", "datetimeOffset5");

            dto = new DateTimeOffset(2002, 12, 30, 23, 15, 55, TimeSpan.Zero);
            CError.Equals(XmlConvert.ToString(dto, "yyyy-MM-ddTHH:mm:sszzzzzz"), "2002-12-30T23:15:55+00:00", "datetimeOffset6");

            dto = new DateTimeOffset(2002, 12, 30, 23, 15, 55, TimeSpan.Zero);
            CError.Equals(XmlConvert.ToString(dto, "yyyy-MM-dd"), "2002-12-30", "datetimeOffset7");

            dto = new DateTimeOffset(2002, 12, 30, 23, 15, 55, TimeSpan.Zero);
            CError.Equals(XmlConvert.ToString(dto, "HH:mm:ss"), "23:15:55", "datetimeOffset8");

            dto = new DateTimeOffset(2002, 12, 30, 23, 15, 55, TimeSpan.Zero);
            CError.Equals(XmlConvert.ToString(dto, "HH:mm:ssZ"), "23:15:55Z", "datetimeOffset9");

            dto = new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.FromMinutes(0));
            CError.Equals(XmlConvert.ToString(dto, "yyyy-MM-ddTHH:mm:sszzzzzz"), "0001-01-01T00:00:00+00:00", "datetimeOffset10");

            dto = new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.FromMinutes(-840));
            CError.Equals(XmlConvert.ToString(dto, "yyyy-MM-ddTHH:mm:sszzzzzz"), "0001-01-01T00:00:00-14:00", "datetimeOffset11");

            dto = new DateTimeOffset(9999, 12, 31, 23, 59, 59, TimeSpan.FromMinutes(840));
            CError.Equals(XmlConvert.ToString(dto, "yyyy-MM-ddTHH:mm:sszzzzzz"), "9999-12-31T23:59:59+14:00", "datetimeOffset12");

            dto = new DateTimeOffset(9999, 12, 31, 23, 59, 59, TimeSpan.FromMinutes(840));
            CError.Equals(XmlConvert.ToString(dto, null), "12/31/9999 23:59:59 +14:00", "datetimeOffset13");

            dto = new DateTimeOffset(9999, 12, 31, 23, 59, 59, TimeSpan.FromMinutes(0));
            CError.Equals(XmlConvert.ToString(dto, "yyyy-MM-ddTHH:mm:sszzzzzz"), "9999-12-31T23:59:59+00:00", "datetimeOffset14");

            dto = DateTimeOffset.MaxValue;
            CError.Equals(XmlConvert.ToString(dto, "yyyy-MM-ddTHH:mm:sszzzzzz"), "9999-12-31T23:59:59+00:00", "datetimeOffset16");

            dto = DateTimeOffset.MaxValue;
            CError.Equals(XmlConvert.ToString(dto, null), "12/31/9999 23:59:59 +00:00", "datetimeOffset18");

            dto = DateTimeOffset.MinValue;
            CError.Equals(XmlConvert.ToString(dto, "yyyy-MM-ddTHH:mm:sszzzzzz"), "0001-01-01T00:00:00+00:00", "datetimeOffset19");

            dto = new DateTimeOffset(1, 1, 1, 0, 0, 0, 1, TimeSpan.FromHours(-0));
            CError.Equals(XmlConvert.ToString(dto, "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzzzzz"), "0001-01-01T00:00:00.001+00:00", "datetimeOffset22");

            dto = new DateTimeOffset(9999, 12, 31, 23, 59, 59, 999, TimeSpan.FromHours(14));
            CError.Equals(XmlConvert.ToString(dto, "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzzzzz"), "9999-12-31T23:59:59.999+14:00", "datetimeOffset23");

            dto = new DateTimeOffset(1, 1, 1, 0, 0, 0, 999, TimeSpan.FromHours(-14));
            CError.Equals(XmlConvert.ToString(dto, "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzzzzz"), "0001-01-01T00:00:00.999-14:00", "datetimeOffset24");

            dto = new DateTimeOffset(9999, 12, 31, 23, 59, 59, 125, TimeSpan.FromHours(14));
            CError.Equals(XmlConvert.ToString(dto, "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzzzzz"), "9999-12-31T23:59:59.125+14:00", "datetimeOffset25");

            dto = new DateTimeOffset(2000, 2, 29, 23, 59, 59, 999, TimeSpan.FromHours(-14));
            CError.Equals(XmlConvert.ToString(dto, "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzzzzz"), "2000-02-29T23:59:59.999-14:00", "datetimeOffset26");

            dto = new DateTimeOffset(2000, 2, 29, 23, 59, 59, 444, TimeSpan.FromHours(+14));
            CError.Equals(XmlConvert.ToString(dto, "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzzzzz"), "2000-02-29T23:59:59.444+14:00", "datetimeOffset27");

            dto = new DateTimeOffset(9999, 12, 31, 23, 59, 59, 999, TimeSpan.FromHours(14));
            CError.Equals(XmlConvert.ToString(dto, "yyyy-MM-ddTHH:mm:ss.FFFFFFFzzzzzz"), "9999-12-31T23:59:59.999+14:00", "datetimeOffset28");
            return TEST_PASS;
        }

        //[Variation("1. Roundtrip: DateTimeOffset-String-DateTimeOffset", Param = 1)]
        //[Variation("2. Roundtrip: DateTimeOffset-String-DateTimeOffset", Param = 2)]
        //[Variation("3. Roundtrip: DateTimeOffset-String-DateTimeOffset", Param = 3)]
        //[Variation("4. Roundtrip: DateTimeOffset-String-DateTimeOffset", Param = 4)]
        //[Variation("5. Roundtrip: DateTimeOffset-String-DateTimeOffset", Param = 5)]
        public int ToType58()
        {
            var param = (int)CurVariation.Param;
            var d = new DateTimeOffset();
            switch (param)
            {
                case 1:
                    d = new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.FromHours(-8));
                    break;
                case 2:
                    d = new DateTimeOffset(DateTime.MaxValue.Ticks, TimeSpan.Zero);
                    break;
                case 3:
                    d = new DateTimeOffset(DateTime.MinValue.Ticks, TimeSpan.Zero);
                    break;
                case 4:
                    d = new DateTimeOffset();
                    break;
                case 5:
                    d = new DateTimeOffset(1, 1, 1, 0, 0, 0, 999, TimeSpan.FromHours(-14));
                    break;
            }
            string s = XmlConvert.ToString(d);
            DateTimeOffset dr = XmlConvert.ToDateTimeOffset(s);
            CError.Compare(d, dr, "DateTimeOffset");
            return TEST_PASS;
        }

        //[Variation("1. Roundtrip: String-DateTimeOffset-String", Param = "0001-01-01T00:00:00Z")]
        //[Variation("2. Roundtrip: String-DateTimeOffset-String", Param = "0001-01-01T00:00:00-14:00")]
        //[Variation("3. Roundtrip: String-DateTimeOffset-String", Param = "9998-12-31T12:59:59-14:00")]
        //[Variation("4. Roundtrip: String-DateTimeOffset-String", Param = "9999-12-31T23:59:59+14:00")]
        //[Variation("5. Roundtrip: String-DateTimeOffset-String", Param = "9999-12-31T23:59:59Z")]
        //[Variation("6. Roundtrip: String-DateTimeOffset-String", Param = "2000-02-29T23:59:59.9999999+13:59")]
        public int ToType59()
        {
            var s = (string)CurVariation.Param;
            DateTimeOffset d = XmlConvert.ToDateTimeOffset(s);
            string sr = XmlConvert.ToString(d);
            CError.Compare(sr, s, "DateTimeOffset");
            return TEST_PASS;
        }

        public int ToType6()
        {
            string[] array = { "-1", "1 2 ", "0x45", null, "1.2345", "-0.0", "\uDE34\uD9A2" };

            return TestInvalid(array, "char");
        }

        //[Variation("null params DateTime, DateTimeOffset - invalid cases")]
        public int ToType62()
        {
            try
            {
                XmlConvert.ToDateTimeOffset("2002-30-12", (string)null);
                CError.Compare(false, "Failed0");
            }
            catch (ArgumentNullException e)
            {
                CError.WriteLine(e.Message);
                try
                {
                    XmlConvert.ToDateTimeOffset("2002-30-12", (string[])null);
                    CError.Compare(false, "Failed1");
                }
                catch (ArgumentNullException ae)
                {
                    CError.WriteLine(ae);
                }
            }

            try
            {
                XmlConvert.ToDateTimeOffset(null, (string)null);
                CError.Compare(false, "Failed4");
            }
            catch (ArgumentNullException e)
            {
                CError.WriteLine(e.Message);
                try
                {
                    XmlConvert.ToDateTimeOffset(null, (string[])null);
                    CError.Compare(false, "Failed5");
                }
                catch (ArgumentNullException ae)
                {
                    CError.WriteLine(ae);
                }
            }

            return TEST_PASS;
        }

        public int ToType9()
        {
            object[] array0 = { "145896.2334", "-1.23", "  -458.238", "145896.233422154", "0.123456789012345678", "1234567890123456789", "100.0", "100." };
            object[] array1 = { (decimal)145896.2334, (decimal)-1.23, (decimal)-458.238, (decimal)145896.233422154, Convert.ToDecimal("0.123456789012345678", CultureInfo.InvariantCulture), (decimal)1234567890123456789, (decimal)100, (decimal)100 };
            return TestValid(array0, array1, "decimal");
        }

        #endregion

        #region Methods

        private TimeSpan GetOffsetFromUtc(DateTime dt)
        {
            TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(dt);
            return offset;
        }
        #endregion
    }
}
