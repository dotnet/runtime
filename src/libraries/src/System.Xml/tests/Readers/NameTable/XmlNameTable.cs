// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    ////////////////////////////////////////////////////////////////
    // Module
    //
    ////////////////////////////////////////////////////////////////
    [TestModule(Name = "Name Table", Desc = "Test for Get and Add methods")]
    public partial class CNameTableTestModule : CTestModule
    {
        //Accessors
        private string _TestData = null;
        public string TestData
        {
            get
            {
                return _TestData;
            }
        }

        public override int Init(object objParam)
        {
            int ret = base.Init(objParam);

            _TestData = Path.Combine(FilePathUtil.GetTestDataPath(), @"XmlReader");

            // Create global usage test files
            string strFile = String.Empty;
            NameTable_TestFiles.CreateTestFile(ref strFile, EREADER_TYPE.GENERIC);

            return ret;
        }

        public override int Terminate(object objParam)
        {
            return base.Terminate(objParam);
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase TCBase
    //
    ////////////////////////////////////////////////////////////////
    public partial class TCBase : CTestCase
    {
        public enum ENAMETABLE_VER
        {
            VERIFY_WITH_GETSTR,
            VERIFY_WITH_GETCHAR,
            VERIFY_WITH_ADDSTR,
            VERIFY_WITH_ADDCHAR,
        };

        private ENAMETABLE_VER _eNTVer;

        public ENAMETABLE_VER NameTableVer
        {
            get { return _eNTVer; }
            set { _eNTVer = value; }
        }

        public static string WRONG_EXCEPTION = "Catching Wrong Exception";

        protected static string BigStr = new String('Z', (1 << 20) - 1);

        protected XmlReader DataReader;

        public override int Init(object objParam)
        {
            if (GetDescription() == "VerifyWGetString")
            {
                NameTableVer = ENAMETABLE_VER.VERIFY_WITH_GETSTR;
            }

            else if (GetDescription() == "VerifyWGetChar")
            {
                NameTableVer = ENAMETABLE_VER.VERIFY_WITH_GETCHAR;
            }

            else if (GetDescription() == "VerifyWAddString")
            {
                NameTableVer = ENAMETABLE_VER.VERIFY_WITH_ADDSTR;
            }

            else if (GetDescription() == "VerifyWAddChar")
            {
                NameTableVer = ENAMETABLE_VER.VERIFY_WITH_ADDCHAR;
            }
            else
                throw (new Exception());

            int ival = base.Init(objParam);

            ReloadSource();

            if (TEST_PASS == ival)
            {
                while (DataReader.Read() == true) ;
            }

            return ival;
        }

        protected void ReloadSource()
        {
            if (DataReader != null)
            {
                DataReader.Dispose();
            }

            string strFile = NameTable_TestFiles.GetTestFileName(EREADER_TYPE.GENERIC);
            DataReader = XmlReader.Create(FilePathUtil.getStream(strFile), new XmlReaderSettings() { DtdProcessing = DtdProcessing.Ignore });//new XmlTextReader(strFile);
        }

        public void VerifyNameTable(object objActual, string str, char[] ach, int offset, int length)
        {
            VerifyNameTableGet(objActual, str, ach, offset, length);
            VerifyNameTableAdd(objActual, str, ach, offset, length);
        }


        public void VerifyNameTableGet(object objActual, string str, char[] ach, int offset, int length)
        {
            object objExpected = null;

            if (NameTableVer == ENAMETABLE_VER.VERIFY_WITH_GETSTR)
            {
                objExpected = DataReader.NameTable.Get(str);
                CError.WriteLine("VerifyNameTableWGetStr");
                CError.Compare(objActual, objExpected, "VerifyNameTableWGetStr");
            }
            else if (NameTableVer == ENAMETABLE_VER.VERIFY_WITH_GETCHAR)
            {
                objExpected = DataReader.NameTable.Get(ach, offset, length);
                CError.WriteLine("VerifyNameTableWGetChar");
                CError.Compare(objActual, objExpected, "VerifyNameTableWGetChar");
            }
        }

        public void VerifyNameTableAdd(object objActual, string str, char[] ach, int offset, int length)
        {
            object objExpected = null;

            if (NameTableVer == ENAMETABLE_VER.VERIFY_WITH_ADDSTR)
            {
                objExpected = DataReader.NameTable.Add(ach, offset, length);
                CError.WriteLine("VerifyNameTableWAddStr");
                CError.Compare(objActual, objExpected, "VerifyNameTableWAddStr");
            }
            else if (NameTableVer == ENAMETABLE_VER.VERIFY_WITH_ADDCHAR)
            {
                objExpected = DataReader.NameTable.Add(str);
                CError.WriteLine("VerifyNameTableWAddChar");
                CError.Compare(objActual, objExpected, "VerifyNameTableWAddChar");
            }
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase TCRecord	NameTable.Get
    //
    ////////////////////////////////////////////////////////////////
    //[TestCase(Name="NameTable(Get) VerifyWGetChar", Desc="VerifyWGetChar")]
    //[TestCase(Name="NameTable(Get) VerifyWGetString", Desc="VerifyWGetString")]
    //[TestCase(Name="NameTable(Get) VerifyWAddString", Desc="VerifyWAddString")]
    //[TestCase(Name="NameTable(Get) VerifyWAddChar", Desc="VerifyWAddChar")]
    public partial class TCRecordNameTableGet : TCBase
    {
        public static char[] chInv = { 'U', 'n', 'a', 't', 'o', 'm', 'i', 'z', 'e', 'd' };
        public static char[] chVal = { 'P', 'L', 'A', 'Y' };
        public static char[] chValW1EndExtra = { 'P', 'L', 'A', 'Y', 'Y' };
        public static char[] chValW1FrExtra = { 'P', 'P', 'L', 'A', 'Y' };
        public static char[] chValW1Fr1EndExtra = { 'P', 'P', 'L', 'A', 'Y', 'Y' };
        public static char[] chValWEndExtras = { 'P', 'L', 'A', 'Y', 'Y', 'Y' };
        public static char[] chValWFrExtras = { 'P', 'P', 'P', 'P', 'P', 'P', 'P', 'L', 'A', 'Y' };
        public static char[] chValWFrEndExtras = { 'P', 'P', 'P', 'P', 'P', 'P', 'P', 'L', 'A', 'Y', 'Y', 'Y' };

        public static string[] strPerVal =
            {
                "PLYA", "PALY", "PAYL", "PYLA", "PYAL",
                "LPAY", "LPYA", "LAPY", "LAYP", "LYPA", "LYAP",
                "ALPY", "ALYP", "APLY", "APYL", "AYLP", "AYPL",
                "YLPA", "YLAP", "YPLA", "YPAL", "YALP", "YAPL",
            };


        public static string[] strPerValCase =
            {
                "pLAY", "plAY", "plaY", "play",
                "plAY", "plaY",
                "pLaY", "pLay",
                "pLAy",
                "PlAY", "PlaY", "Play",
                "PLaY",
                "PLAy"
            };


        public static string strInv = "Unatomized";
        public static string strVal = "PLAY";


        [Variation("GetUnAutomized", Pri = 0)]
        public int Variation_1()
        {
            object objActual = DataReader.NameTable.Get(strInv);
            object objActual1 = DataReader.NameTable.Get(strInv);

            CError.Compare(objActual, null, CurVariation.Desc);
            CError.Compare(objActual1, null, CurVariation.Desc);

            CError.Compare(objActual, objActual1, CurVariation.Desc);
            VerifyNameTableGet(objActual, strInv, chInv, 0, chInv.Length);
            return TEST_PASS;
        }


        [Variation("Get Atomized String", Pri = 0)]
        public int Variation_2()
        {
            object objActual = DataReader.NameTable.Get(chVal, 0, chVal.Length);
            object objActual1 = DataReader.NameTable.Get(chVal, 0, chVal.Length);

            CError.Compare(objActual, objActual1, CurVariation.Desc);
            VerifyNameTable(objActual, strVal, chVal, 0, chVal.Length);
            return TEST_PASS;
        }


        [Variation("Get Atomized String with end padded", Pri = 0)]
        public int Variation_3()
        {
            object objActual = DataReader.NameTable.Get(chValW1EndExtra, 0, strVal.Length);
            object objActual1 = DataReader.NameTable.Get(chValW1EndExtra, 0, strVal.Length);

            CError.Compare(objActual, objActual1, CurVariation.Desc);
            VerifyNameTable(objActual, strVal, chValW1EndExtra, 0, strVal.Length);
            return TEST_PASS;
        }


        [Variation("Get Atomized String with front and end padded", Pri = 0)]
        public int Variation_4()
        {
            object objActual = DataReader.NameTable.Get(chValW1Fr1EndExtra, 1, strVal.Length);
            object objActual1 = DataReader.NameTable.Get(chValW1Fr1EndExtra, 1, strVal.Length);

            CError.Compare(objActual, objActual1, CurVariation.Desc);
            VerifyNameTable(objActual, strVal, chValW1Fr1EndExtra, 1, strVal.Length);
            return TEST_PASS;
        }


        [Variation("Get Atomized String with front padded", Pri = 0)]
        public int Variation_5()
        {
            object objActual = DataReader.NameTable.Get(chValW1FrExtra, 1, strVal.Length);
            object objActual1 = DataReader.NameTable.Get(chValW1FrExtra, 1, strVal.Length);

            CError.Compare(objActual, objActual1, CurVariation.Desc);
            VerifyNameTable(objActual, strVal, chValW1FrExtra, 1, strVal.Length);
            return TEST_PASS;
        }


        [Variation("Get Atomized String with end multi-padded", Pri = 0)]
        public int Variation_6()
        {
            object objActual = DataReader.NameTable.Get(chValWEndExtras, 0, strVal.Length);
            object objActual1 = DataReader.NameTable.Get(chValWEndExtras, 0, strVal.Length);

            CError.Compare(objActual, objActual1, CurVariation.Desc);
            VerifyNameTable(objActual, strVal, chValWEndExtras, 0, strVal.Length);
            return TEST_PASS;
        }


        [Variation("Get Atomized String with front and end multi-padded", Pri = 0)]
        public int Variation_7()
        {
            object objActual = DataReader.NameTable.Get(chValWFrEndExtras, 6, strVal.Length);
            object objActual1 = DataReader.NameTable.Get(chValWFrEndExtras, 6, strVal.Length);

            CError.Compare(objActual, objActual1, CurVariation.Desc);
            VerifyNameTable(objActual, strVal, chValWFrEndExtras, 6, strVal.Length);
            return TEST_PASS;
        }


        [Variation("Get Atomized String with front multi-padded", Pri = 0)]
        public int Variation_8()
        {
            object objActual = DataReader.NameTable.Get(chValWFrExtras, chValWFrExtras.Length - strVal.Length, strVal.Length);
            object objActual1 = DataReader.NameTable.Get(chValWFrExtras, chValWFrExtras.Length - strVal.Length, strVal.Length);

            CError.Compare(objActual, objActual1, CurVariation.Desc);
            VerifyNameTable(objActual, strVal, chValWFrExtras, chValWFrExtras.Length - strVal.Length, strVal.Length);
            return TEST_PASS;
        }


        [Variation("Get Invalid permutation of valid string", Pri = 0)]
        public int Variation_9()
        {
            for (int i = 0; i < strPerVal.Length; i++)
            {
                char[] ach = strPerVal[i].ToCharArray();

                object objActual = DataReader.NameTable.Get(ach, 0, ach.Length);
                object objActual1 = DataReader.NameTable.Get(ach, 0, ach.Length);

                CError.Compare(objActual, null, CurVariation.Desc);
                CError.Compare(objActual1, null, CurVariation.Desc);

                CError.Compare(objActual, objActual1, CurVariation.Desc);
                VerifyNameTableGet(objActual, strPerVal[i], ach, 0, ach.Length);
            }
            return TEST_PASS;
        }

        [Variation("Get Valid Super String")]
        public int Variation_10()
        {
            string filename = null;
            NameTable_TestFiles.CreateTestFile(ref filename, EREADER_TYPE.BIG_ELEMENT_SIZE);
            XmlReader rDataReader = XmlReader.Create(FilePathUtil.getStream(filename));

            while (rDataReader.Read() == true) ;
            XmlNameTable nt = rDataReader.NameTable;
            object objTest1 = nt.Get(BigStr + "Z");
            object objTest2 = nt.Get(BigStr + "X");
            object objTest3 = nt.Get(BigStr + "Y");

            if (objTest1 != null)
            {
                throw new CTestException(CTestBase.TEST_FAIL, "objTest1 is not null");
            }
            if (objTest2 == null)
            {
                throw new CTestException(CTestBase.TEST_FAIL, "objTest2 is null");
            }
            if (objTest3 == null)
            {
                throw new CTestException(CTestBase.TEST_FAIL, "objTest3 is  null");
            }

            if ((objTest1 == objTest2) || (objTest1 == objTest3) || (objTest2 == objTest3))
                throw new CTestException(CTestBase.TEST_FAIL, "objTest1 is equal to objTest2, or objTest3");
            return TEST_PASS;
        }

        [Variation("Get invalid Super String")]
        public int Variation_11()
        {
            int size = (1 << 24);
            string str = "";
            char[] ach = str.ToCharArray();

            bool fRetry = false;

            for (; ;)
            {
                try
                {
                    str = new String('Z', size);
                    ach = str.ToCharArray();
                }
                catch (OutOfMemoryException exc)
                {
                    size >>= 1;
                    CError.WriteLine(exc + " : " + exc.Message + " Retry with " + size);
                    fRetry = true;
                }

                if (size < (1 << 30))
                {
                    fRetry = true;
                }

                if (fRetry)
                {
                    CError.WriteLine("Tested size == " + size);
                    if (str == null)
                        CError.WriteLine("string is null");
                    break;
                }
            }

            object objActual = DataReader.NameTable.Get(ach, 0, ach.Length);
            object objActual1 = DataReader.NameTable.Get(ach, 0, ach.Length);

            CError.Compare(objActual, objActual1, CurVariation.Desc);
            VerifyNameTableGet(objActual, str, ach, 0, ach.Length);

            return TEST_PASS;
        }


        [Variation("Get empty string, valid offset and length = 0", Pri = 0)]
        public int Variation_12()
        {
            string str = String.Empty;
            char[] ach = str.ToCharArray();

            object objActual = DataReader.NameTable.Get(ach, 0, ach.Length);
            object objActual1 = DataReader.NameTable.Get(ach, 0, 0);
            object objActual2 = DataReader.NameTable.Get(str);

            CError.Compare(objActual, objActual1, "Char with StringEmpty");
            CError.Compare(String.Empty, objActual1, "Char with StringEmpty");
            CError.Compare(String.Empty, objActual2, "StringEmpty");

            VerifyNameTable(objActual, str, ach, 0, 0);
            return TEST_PASS;
        }


        [Variation("Get empty string, valid offset and length = 1", Pri = 0)]
        public int Variation_13()
        {
            char[] ach = new char[] { };

            try
            {
                object objActual = DataReader.NameTable.Get(ach, 0, 1);
            }
            catch (IndexOutOfRangeException exc)
            {
                CError.WriteLine(exc + " : " + exc.Message);
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }


        [Variation("Get null char[], valid offset and length = 0", Pri = 0)]
        public int Variation_14()
        {
            char[] ach = null;

            object objActual = DataReader.NameTable.Add(ach, 0, 0);
            CError.Compare(String.Empty, objActual, "Char with null");
            return TEST_PASS;
        }


        [Variation("Get null string", Pri = 0)]
        public int Variation_15()
        {
            string str = null;

            try
            {
                object objActual = DataReader.NameTable.Get(str);
            }
            catch (ArgumentNullException exc)
            {
                CError.WriteLine(exc + " : " + exc.Message);
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }


        [Variation("Get null char[], valid offset and length = 1", Pri = 0)]
        public int Variation_16()
        {
            char[] ach = null;

            try
            {
                object objActual = DataReader.NameTable.Add(ach, 0, 1);
            }
            catch (NullReferenceException exc)
            {
                CError.WriteLine(exc + " : " + exc.Message);
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }


        [Variation("Get valid string, invalid length, length = 0", Pri = 0)]
        public int Variation_17()
        {
            object objActual = DataReader.NameTable.Get(chVal, 0, 0);
            object objActual1 = DataReader.NameTable.Get(chVal, 0, 0);

            CError.WriteLine("Here " + chVal.ToString());
            CError.WriteLine("Here2 " + DataReader.NameTable.Get(chVal, 0, 0));
            if (DataReader.NameTable.Get(chVal, 0, 0) == String.Empty)
                CError.WriteLine("here");
            if (DataReader.NameTable.Get(chVal, 0, 0) == null)
                CError.WriteLine("null");
            CError.Compare(objActual, objActual1, CurVariation.Desc);
            VerifyNameTable(objActual, String.Empty, chVal, 0, 0);
            return TEST_PASS;
        }


        [Variation("Get valid string, invalid length, length = Length+1", Pri = 0)]
        public int Variation_18()
        {
            try
            {
                object objActual = DataReader.NameTable.Get(chVal, 0, chVal.Length + 1);
            }
            catch (IndexOutOfRangeException exc)
            {
                CError.WriteLine(exc + " : " + exc.Message);
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }


        [Variation("Get valid string, invalid length, length = max_int", Pri = 0)]
        public int Variation_19()
        {
            try
            {
                object objActual = DataReader.NameTable.Get(chVal, 0, Int32.MaxValue);
            }
            catch (IndexOutOfRangeException exc)
            {
                CError.WriteLine(exc + " : " + exc.Message);
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }


        [Variation("Get valid string, invalid length, length = -1", Pri = 0)]
        public int Variation_20()
        {
            object objActual = DataReader.NameTable.Get(chVal, 0, -1);
            CError.WriteLine("HERE " + objActual);
            return TEST_PASS;
        }


        [Variation("Get valid string, invalid offset > Length", Pri = 0)]
        public int Variation_21()
        {
            try
            {
                object objActual = DataReader.NameTable.Get(chVal, chVal.Length + 1, chVal.Length);
            }
            catch (IndexOutOfRangeException exc)
            {
                CError.WriteLine(exc + " : " + exc.Message);
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }


        [Variation("Get valid string, invalid offset = max_int", Pri = 0)]
        public int Variation_22()
        {
            try
            {
                object objActual = DataReader.NameTable.Get(chVal, Int32.MaxValue, chVal.Length);
            }
            catch (IndexOutOfRangeException exc)
            {
                CError.WriteLine(exc + " : " + exc.Message);
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }


        [Variation("Get valid string, invalid offset = Length", Pri = 0)]
        public int Variation_23()
        {
            try
            {
                object objActual = DataReader.NameTable.Get(chVal, chVal.Length, chVal.Length);
            }
            catch (IndexOutOfRangeException exc)
            {
                CError.WriteLine(exc + " : " + exc.Message);
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }


        [Variation("Get valid string, invalid offset -1", Pri = 0)]
        public int Variation_24()
        {
            try
            {
                object objActual = DataReader.NameTable.Get(chVal, -1, chVal.Length);
            }
            catch (IndexOutOfRangeException exc)
            {
                CError.WriteLine(exc + " : " + exc.Message);
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }


        [Variation("Get valid string, invalid offset and length", Pri = 0)]
        public int Variation_25()
        {
            try
            {
                object objActual = DataReader.NameTable.Get(chVal, -1, -1);
            }
            catch (IndexOutOfRangeException exc)
            {
                CError.WriteLine(exc + " : " + exc.Message);
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase TCRecord	NameTable.Add
    //
    ////////////////////////////////////////////////////////////////
    //[TestCase(Name="NameTable(Add) VerifyWGetString", Desc="VerifyWGetString")]
    //[TestCase(Name="NameTable(Add) VerifyWGetChar", Desc="VerifyWGetChar")]
    //[TestCase(Name="NameTable(Add) VerifyWAddString", Desc="VerifyWAddString")]
    //[TestCase(Name="NameTable(Add) VerifyWAddChar", Desc="VerifyWAddChar")]
    public partial class TCRecordNameTableAdd : TCBase
    {
        public static char[] chVal = { 'F', 'O', 'O' };
        public static char[] chValW1EndExtra = { 'F', 'O', 'O', 'O' };
        public static char[] chValW1FrExtra = { 'F', 'F', 'O', 'O', 'O' };
        public static char[] chValW1Fr1EndExtra = { 'F', 'F', 'O', 'O', 'O' };
        public static char[] chValWEndExtras = { 'F', 'O', 'O', 'O', 'O', 'O' };
        public static char[] chValWFrExtras = { 'F', 'F', 'F', 'F', 'F', 'F', 'F', 'O', 'O', 'O' };
        public static char[] chValWFrEndExtras = { 'F', 'F', 'F', 'F', 'F', 'F', 'F', 'O', 'O', 'O', 'O', 'O' };

        public static string[] strPerVal =
            {
                "OFO", "OOF"
            };


        public static string[] strPerValCase =
            {
                "fOO", "foO", "foo",
                "FoO", "Foo",
                "FOo"
            };


        public static string strVal = "FOO";

        public static string strWhitespaceVal = "WITH WHITESPACE";
        public static string strAlphaNumVal = "WITH1Number";
        public static string strSignVal = "+SIGN-";


        [Variation("Add a new atomized string (padded with chars at the end), valid offset and length = str_length", Pri = 0)]
        public int Variation_1()
        {
            ReloadSource();

            object objActual = DataReader.NameTable.Add(chValWEndExtras, 0, strVal.Length);
            object objActual1 = DataReader.NameTable.Add(chValWEndExtras, 0, strVal.Length);

            if (objActual == objActual1)
                CError.WriteLine(objActual + " and ", objActual1);

            CError.Compare(objActual, objActual1, CurVariation.Desc);
            VerifyNameTable(objActual, strVal, chValWEndExtras, 0, strVal.Length);
            return TEST_PASS;
        }


        [Variation("Add a new atomized string (padded with chars at both front and end), valid offset and length = str_length", Pri = 0)]
        public int Variation_2()
        {
            ReloadSource();

            object objActual = DataReader.NameTable.Add(chValWFrEndExtras, 6, strVal.Length);
            object objActual1 = DataReader.NameTable.Add(chValWFrEndExtras, 6, strVal.Length);

            CError.Compare(objActual, objActual1, CurVariation.Desc);
            VerifyNameTable(objActual, strVal, chValWFrExtras, 6, strVal.Length);
            return TEST_PASS;
        }


        [Variation("Add a new atomized string (padded with chars at the front), valid offset and length = str_length", Pri = 0)]
        public int Variation_3()
        {
            ReloadSource();

            object objActual = DataReader.NameTable.Add(chValWFrEndExtras, 6, strVal.Length);
            object objActual1 = DataReader.NameTable.Add(chValWFrEndExtras, 6, strVal.Length);

            CError.Compare(objActual, objActual1, CurVariation.Desc);
            VerifyNameTable(objActual, strVal, chValWFrEndExtras, 6, strVal.Length);
            return TEST_PASS;
        }


        [Variation("Add a new atomized string (padded a char at the end), valid offset and length = str_length", Pri = 0)]
        public int Variation_4()
        {
            ReloadSource();

            object objActual = DataReader.NameTable.Add(chValW1EndExtra, 0, strVal.Length);
            object objActual1 = DataReader.NameTable.Add(chValW1EndExtra, 0, strVal.Length);

            CError.Compare(objActual, objActual1, CurVariation.Desc);
            VerifyNameTable(objActual, strVal, chValW1EndExtra, 0, strVal.Length);

            return TEST_PASS;
        }


        [Variation("Add a new atomized string (padded with a char at both front and end), valid offset and length = str_length", Pri = 0)]
        public int Variation_5()
        {
            ReloadSource();

            object objActual = DataReader.NameTable.Add(chValW1Fr1EndExtra, 1, strVal.Length);
            object objActual1 = DataReader.NameTable.Add(chValW1Fr1EndExtra, 1, strVal.Length);

            CError.Compare(objActual, objActual1, CurVariation.Desc);
            VerifyNameTable(objActual, strVal, chValW1Fr1EndExtra, 1, strVal.Length);
            return TEST_PASS;
        }


        [Variation("Add a new atomized string (padded with a char at the front), valid offset and length = str_length", Pri = 0)]
        public int Variation_6()
        {
            ReloadSource();

            object objActual = DataReader.NameTable.Add(chValW1FrExtra, 1, strVal.Length);
            object objActual1 = DataReader.NameTable.Add(chValW1FrExtra, 1, strVal.Length);

            CError.Compare(objActual, objActual1, CurVariation.Desc);
            VerifyNameTable(objActual, strVal, chValW1FrExtra, 1, strVal.Length);
            return TEST_PASS;
        }


        [Variation("Add new string between 1M - 2M in size, valid offset and length")]
        public int Variation_7()
        {
            char[] chTest = BigStr.ToCharArray();
            object objActual1 = DataReader.NameTable.Add(chTest, 0, chTest.Length);
            object objActual2 = DataReader.NameTable.Add(chTest, 0, chTest.Length);

            CError.Compare(objActual1, objActual2, CurVariation.Desc);
            VerifyNameTable(objActual1, BigStr, chTest, 0, chTest.Length);

            return TEST_PASS;
        }


        [Variation("Add an existing atomized string (with Max string for test: 1-2M), valid offset and valid length")]
        public int Variation_8()
        {
            ////////////////////////////
            // Add strings again and verify

            string filename = null;
            NameTable_TestFiles.CreateTestFile(ref filename, EREADER_TYPE.BIG_ELEMENT_SIZE);
            XmlReader rDataReader = XmlReader.Create(FilePathUtil.getStream(filename));
            while (rDataReader.Read() == true) ;
            XmlNameTable nt = rDataReader.NameTable;

            string strTest = BigStr + "X";

            char[] chTest = strTest.ToCharArray();
            Object objActual1 = nt.Add(chTest, 0, chTest.Length);
            Object objActual2 = nt.Add(chTest, 0, chTest.Length);

            CError.Compare(objActual1, objActual2, "Comparing objActual1 and objActual2");
            CError.Compare(objActual1, nt.Get(chTest, 0, chTest.Length), "Comparing objActual1 and GetCharArray");
            CError.Compare(objActual1, nt.Get(strTest), "Comparing objActual1 and GetString");
            CError.Compare(objActual1, nt.Add(strTest), "Comparing objActual1 and AddString");

            NameTable_TestFiles.RemoveDataReader(EREADER_TYPE.BIG_ELEMENT_SIZE);

            return TEST_PASS;
        }


        [Variation("Add new string, and do Get with a combination of the same string in different order", Pri = 0)]
        public int Variation_9()
        {
            ReloadSource();

            // Add string
            Object objAdded = DataReader.NameTable.Add(strVal);

            // Look for permutations of strings, should be null.
            for (int i = 0; i < strPerVal.Length; i++)
            {
                char[] ach = strPerVal[i].ToCharArray();

                object objActual = DataReader.NameTable.Get(ach, 0, ach.Length);
                object objActual1 = DataReader.NameTable.Get(ach, 0, ach.Length);

                CError.Compare(objActual, null, CurVariation.Desc);
                CError.Compare(objActual, objActual1, CurVariation.Desc);
            }
            return TEST_PASS;
        }


        [Variation("Add new string, and Add a combination of the same string in different case, all are different objects", Pri = 0)]
        public int Variation_10()
        {
            ReloadSource();

            // Add string
            Object objAdded = DataReader.NameTable.Add(strVal);

            // Look for permutations of strings, should be null.
            for (int i = 0; i < strPerValCase.Length; i++)
            {
                char[] ach = strPerValCase[i].ToCharArray();

                object objActual = DataReader.NameTable.Add(ach, 0, ach.Length);
                object objActual1 = DataReader.NameTable.Add(ach, 0, ach.Length);

                CError.Compare(objActual, objActual1, CurVariation.Desc);
                VerifyNameTable(objActual1, strPerValCase[i], ach, 0, ach.Length);
                if (objAdded == objActual)
                {
                    throw new Exception("\n Object are the same for " + strVal + " and " + strPerValCase[i]);
                }
            }
            return TEST_PASS;
        }


        [Variation("Add 1M new string, and do Get with the last char different than the original string", Pri = 0)]
        public int Variation_11()
        {
            object objAdded = DataReader.NameTable.Add(BigStr + "M");
            object objActual = DataReader.NameTable.Get(BigStr + "D");

            CError.Compare(objActual, null, CurVariation.Desc);

            return TEST_PASS;
        }


        [Variation("Add new alpha numeric, valid offset, valid length", Pri = 0)]
        public int Variation_12()
        {
            ReloadSource();

            char[] chTest = strAlphaNumVal.ToCharArray();
            object objAdded = DataReader.NameTable.Add(chTest, 0, chTest.Length);
            object objActual1 = DataReader.NameTable.Add(chTest, 0, chTest.Length);

            CError.Compare(objActual1, objAdded, CurVariation.Desc);
            VerifyNameTable(objAdded, strAlphaNumVal, chTest, 0, chTest.Length);

            return TEST_PASS;
        }


        [Variation("Add new alpha numeric, valid offset, length= 0", Pri = 0)]
        public int Variation_13()
        {
            ReloadSource();

            char[] chTest = strAlphaNumVal.ToCharArray();
            object objAdded = DataReader.NameTable.Add(chTest, 0, 0);

            object objActual1 = DataReader.NameTable.Get(chVal, 0, chVal.Length);
            object objActual2 = DataReader.NameTable.Get(strVal);

            CError.Compare(objActual1, null, "Get should fail since Add with length=0 should fail");
            CError.Compare(objActual1, objActual2, "Both Get should fail");

            return TEST_PASS;
        }


        [Variation("Add new with whitespace, valid offset, valid length", Pri = 0)]
        public int Variation_14()
        {
            ReloadSource();

            char[] chTest = strWhitespaceVal.ToCharArray();

            object objAdded = DataReader.NameTable.Add(chTest, 0, chTest.Length);
            object objActual1 = DataReader.NameTable.Add(chTest, 0, chTest.Length);

            CError.Compare(objActual1, objAdded, CurVariation.Desc);
            VerifyNameTable(objAdded, strWhitespaceVal, chTest, 0, chTest.Length);

            return TEST_PASS;
        }


        [Variation("Add new with sign characters, valid offset, valid length", Pri = 0)]
        public int Variation_15()
        {
            ReloadSource();

            char[] chTest = strSignVal.ToCharArray();

            object objAdded = DataReader.NameTable.Add(chTest, 0, chTest.Length);
            object objActual1 = DataReader.NameTable.Add(chTest, 0, chTest.Length);

            CError.Compare(objActual1, objAdded, CurVariation.Desc);
            VerifyNameTable(objAdded, strSignVal, chTest, 0, chTest.Length);

            return TEST_PASS;
        }


        [Variation("Add new string between 1M - 2M in size, valid offset and length", Pri = 0)]
        public int Variation_16()
        {
            ReloadSource();

            char[] chTest = BigStr.ToCharArray();

            object objActual1 = DataReader.NameTable.Add(chTest, 0, chTest.Length);
            object objActual2 = DataReader.NameTable.Add(chTest, 0, chTest.Length);

            CError.Compare(objActual1, objActual2, CurVariation.Desc);
            VerifyNameTable(objActual1, BigStr, chTest, 0, chTest.Length);

            return TEST_PASS;
        }


        [Variation("Add new string, get object using permutations of upper & lowercase, should be null", Pri = 0)]
        public int Variation_17()
        {
            ReloadSource();

            // Add string
            Object objAdded = DataReader.NameTable.Add(strVal);

            // Look for permutations of strings, should be null.
            for (int i = 0; i < strPerValCase.Length; i++)
            {
                char[] ach = strPerValCase[i].ToCharArray();

                object objActual = DataReader.NameTable.Get(ach, 0, ach.Length);
                object objActual1 = DataReader.NameTable.Get(ach, 0, ach.Length);

                CError.Compare(objActual, null, CurVariation.Desc);
                CError.Compare(objActual, objActual1, CurVariation.Desc);
            }
            return TEST_PASS;
        }


        [Variation("Add an empty atomized string, valid offset and length = 0", Pri = 0)]
        public int Variation_18()
        {
            ReloadSource();

            string strEmpty = String.Empty;

            object objAdded = DataReader.NameTable.Add(strEmpty);
            object objAdded1 = DataReader.NameTable.Add(strEmpty.ToCharArray(), 0, strEmpty.Length);

            object objActual1 = DataReader.NameTable.Get(strEmpty.ToCharArray(), 0, strEmpty.Length);
            object objActual2 = DataReader.NameTable.Get(strEmpty);

            CError.WriteLine("String " + DataReader.NameTable.Get(strEmpty));
            CError.WriteLine("String " + objAdded1 + " String2 " + objAdded1);
            if (objAdded != objAdded1)
                CError.WriteLine("HERE");
            CError.Compare(objActual1, objActual2, CurVariation.Desc);
            VerifyNameTable(objActual1, strEmpty, strEmpty.ToCharArray(), 0, 0);

            return TEST_PASS;
        }


        [Variation("Add an empty atomized string (array char only), valid offset and length = 1", Pri = 0)]
        public int Variation_19()
        {
            try
            {
                char[] chTest = String.Empty.ToCharArray();
                object objAdded = DataReader.NameTable.Add(chTest, 0, 1);
            }
            catch (IndexOutOfRangeException)
            {
                return TEST_PASS;
            }

            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }


        [Variation("Add a NULL atomized string, valid offset and length = 0", Pri = 0)]
        public int Variation_20()
        {
            object objAdded = DataReader.NameTable.Add(null, 0, 0);
            VerifyNameTable(objAdded, String.Empty, (String.Empty).ToCharArray(), 0, 0);
            return TEST_PASS;
        }


        [Variation("Add a NULL atomized string, valid offset and length = 1", Pri = 0)]
        public int Variation_21()
        {
            try
            {
                object objAdded = DataReader.NameTable.Add(null, 0, 1);
            }
            catch (NullReferenceException)
            {
                return TEST_PASS;
            }

            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }


        [Variation("Add a valid atomized string, valid offset and length = 0", Pri = 0)]
        public int Variation_22()
        {
            ReloadSource();

            object objAdded = DataReader.NameTable.Add(chVal, 0, 0);

            object objActual1 = DataReader.NameTable.Get(chVal, 0, chVal.Length);
            object objActual2 = DataReader.NameTable.Get(strVal);

            CError.Compare(objActual1, null, "Get should fail since Add with length=0 should fail");
            CError.Compare(objActual1, objActual2, "Both Get should fail");

            return TEST_PASS;
        }


        [Variation("Add a valid atomized string, valid offset and length > valid_length", Pri = 0)]
        public int Variation_23()
        {
            try
            {
                object objAdded = DataReader.NameTable.Add(chVal, 0, chVal.Length * 2);
            }
            catch (IndexOutOfRangeException)
            {
                return TEST_PASS;
            }

            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }


        [Variation("Add a valid atomized string, valid offset and length = max_int", Pri = 0)]
        public int Variation_24()
        {
            try
            {
                object objAdded = DataReader.NameTable.Add(chVal, 0, Int32.MaxValue);
            }
            catch (IndexOutOfRangeException)
            {
                return TEST_PASS;
            }

            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }


        [Variation("Add a valid atomized string, valid offset and length = - 1", Pri = 0)]
        public int Variation_25()
        {
            try
            {
                object objAdded = DataReader.NameTable.Add(chVal, 0, -1);
            }
            catch (ArgumentOutOfRangeException)
            {
                return TEST_PASS;
            }

            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }


        [Variation("Add a valid atomized string, valid length and offset > str_length", Pri = 0)]
        public int Variation_26()
        {
            try
            {
                object objAdded = DataReader.NameTable.Add(chVal, chVal.Length * 2, chVal.Length);
            }
            catch (IndexOutOfRangeException)
            {
                return TEST_PASS;
            }

            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }


        [Variation("Add a valid atomized string, valid length and offset = max_int", Pri = 0)]
        public int Variation_27()
        {
            try
            {
                object objAdded = DataReader.NameTable.Add(chVal, Int32.MaxValue, chVal.Length);
            }
            catch (IndexOutOfRangeException)
            {
                return TEST_PASS;
            }

            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }


        [Variation("Add a valid atomized string, valid length and offset = str_length", Pri = 0)]
        public int Variation_28()
        {
            try
            {
                object objAdded = DataReader.NameTable.Add(chVal, chVal.Length, chVal.Length);
            }
            catch (IndexOutOfRangeException)
            {
                return TEST_PASS;
            }

            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }


        [Variation("Add a valid atomized string, valid length and offset = - 1", Pri = 0)]
        public int Variation_29()
        {
            try
            {
                object objAdded = DataReader.NameTable.Add(chVal, -1, chVal.Length);
            }
            catch (IndexOutOfRangeException)
            {
                return TEST_PASS;
            }

            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }


        [Variation("Add a valid atomized string, with both invalid offset and length", Pri = 0)]
        public int Variation_30()
        {
            try
            {
                object objAdded = DataReader.NameTable.Add(chVal, -1, -1);
            }
            catch (IndexOutOfRangeException)
            {
                return TEST_PASS;
            }

            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }
    }
}

