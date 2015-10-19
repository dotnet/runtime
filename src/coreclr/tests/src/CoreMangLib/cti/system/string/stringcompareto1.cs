using System;
using System.Collections.Generic;
using System.Text;
using TestLibrary;

namespace StringCompareTo1
{
    public class StringCompareTo1
    {
        private const int c_MIN_STRING_LEN = 8;
        private const int c_MAX_STRING_LEN = 256;

        public static int Main()
        {
            StringCompareTo1 sco = new StringCompareTo1();

            TestLibrary.TestFramework.BeginTestCase("StringCompareTo1");

            if (sco.RunTests())
            {
                TestLibrary.TestFramework.EndTestCase();
                TestLibrary.TestFramework.LogInformation("PASS");
                return 100;
            }
            else
            {
                TestLibrary.TestFramework.EndTestCase();
                TestLibrary.TestFramework.LogInformation("FAIL");
                return 0;
            }
        }

        public bool RunTests()
        {
            bool retVal = true;

            TestLibrary.TestFramework.LogInformation("[Positive]");
            retVal = PosTest1() && retVal;
            retVal = PosTest2() && retVal;
            retVal = PosTest3() && retVal;
            retVal = PosTest4() && retVal;
            retVal = PosTest5() && retVal;
            retVal = PosTest6() && retVal;
            retVal = PosTest7() && retVal;
            retVal = PosTest8() && retVal;
            retVal = PosTest9() && retVal;
            retVal = PosTest10() && retVal;
            retVal = PosTest11() && retVal;
            retVal = PosTest12() && retVal;
            retVal = PosTest13() && retVal;
            retVal = PosTest14() && retVal;
            retVal = PosTest15() && retVal;
            retVal = PosTest16() && retVal;
            retVal = PosTest17() && retVal;
            retVal = PosTest18() && retVal;
            retVal = PosTest19() && retVal;
            retVal = PosTest20() && retVal;
            retVal = PosTest21() && retVal;

            TestLibrary.TestFramework.LogInformation("[Negative]");
            retVal = NegTest1() && retVal;
            retVal = NegTest2() && retVal;
            retVal = NegTest3() && retVal;

            return retVal;
        }

        #region Positive Testing

        public bool PosTest1()
        {
            bool retVal = true;
            string strA;
            string strB;
            TestLibrary.TestFramework.BeginScenario("PosTest1:nullstring CompareTo null");

            try
            {
                strA = "";
                strB = null;

                int ActualResult = string.CompareOrdinal(strA, strB);
                if (ActualResult<=0)
                {
                    TestLibrary.TestFramework.LogError("001", "nullstrings CompareTo null ExpectResult is greater 0,ActualResult is (" + ActualResult + ")");
                    retVal = false;
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
                retVal = false;
            }
            return retVal;
        }

        public bool PosTest2()
        {
            bool retVal = true;
            string strA;
            string strB;
            TestLibrary.TestFramework.BeginScenario("PosTest2:nullstring CompareTo a space string");

            try
            {
                strA = "";
                strB = " "; 

                int ActualResult = string.CompareOrdinal(strA, strB);
                if (ActualResult >= 0)
                {
                    TestLibrary.TestFramework.LogError("003", "nullstring CompareTo a space string ExpectResult is less 0,ActualResult is (" + ActualResult + ")");
                    retVal = false;
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
                retVal = false;
            }
            return retVal;
        }

        public bool PosTest3()
        {
            bool retVal = true;
            string strA;
            string strB;
            string strBasic;
            Random rand = new Random(-55);
            char CharTab = '\t';
            int ActualResult;
            TestLibrary.TestFramework.BeginScenario("PosTest3:Two like strings embedded differet tabs CompareTo ");

            try
            {
                strBasic = TestLibrary.Generator.GetString(-55, false,c_MIN_STRING_LEN,c_MAX_STRING_LEN);
                strA = strBasic + new string(CharTab,rand.Next(1,10));
                strB = strBasic + new string(CharTab,rand.Next(11,20));

                ActualResult = string.CompareOrdinal(strA, strB);
                if (ActualResult >= 0)
                {
                    TestLibrary.TestFramework.LogError("005", "Two like strings embedded differet tabs CompareTo ExpectResult is less 0,ActualResult is (" + ActualResult + ")");
                    retVal = false;
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
                retVal = false;
            }
            return retVal;
        }

        public bool PosTest4()
        {
            bool retVal = true;
            string strA;
            string strB;
            int ActualResult;

            TestLibrary.TestFramework.BeginScenario("PosTest4:one space CompareTo two spaces");

            try
            {
                strA = " ";
                strB = "  ";
                ActualResult = strA.CompareTo(strB);
                int ExpectedResult = GlobLocHelper.OSCompare(strA, strB); // -1
                if (ActualResult != ExpectedResult)
                {
                    TestLibrary.TestFramework.LogError("007", "one space CompareTo two spaces ExpectResult is less 0 ActualResult is (" + ActualResult + ")");
                }
            }
            catch(Exception e)
            {
                TestLibrary.TestFramework.LogError("008","Unexpected exception" +e);
                retVal = false;
            }
            return retVal;
        }

        public bool PosTest5()
        {
            bool retVal = true;
            string strA;
            string strB;
            int ActualResult;

            TestLibrary.TestFramework.BeginScenario("PosTest5:Two like strings CompareTo");

            try
            {
                strA = "helloword!";
                strB = "helloword!";
                ActualResult = strA.CompareTo(strB);
                int ExpectedResult = GlobLocHelper.OSCompare(strA, strB); // 0
                if (ActualResult != ExpectedResult)
                {
                    TestLibrary.TestFramework.LogError("009", "Two like strings CompareTo ExpectResult is equel 0 ActualResult is (" + ActualResult + ")");
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("010", "Unexpected exception" + e);
                retVal = false;
            }
            return retVal;
        }

        public bool PosTest6()
        {
            bool retVal = true;
            string strA;
            string strB;
            int ActualResult;

            TestLibrary.TestFramework.BeginScenario("PosTest6:One string with lower chars CompareTo the uppers ");

            try
            {
                strA = "helloWord";
                strB = "helloword";
                ActualResult = strA.CompareTo(strB);
                int ExpectedResult = GlobLocHelper.OSCompare(strA, strB); // 1
                if (ActualResult != ExpectedResult)
                {
                    TestLibrary.TestFramework.LogError("011", "One string with lower chars CompareTo the uppers ExpectResult is greater 0 ActualResult is (" + ActualResult + ")");
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("012", "Unexpected exception" + e);
                retVal = false;
            }
            return retVal;
        }

        public bool PosTest7()
        {
            bool retVal = true;
            string strA;
            string strB;
            int ActualResult;

            TestLibrary.TestFramework.BeginScenario("PosTest7:string CompareTo its with space one");

            try
            {
                strA = "helloword";
                strB = " helloword";
                ActualResult = strA.CompareTo(strB);
                int ExpectedResult = GlobLocHelper.OSCompare(strA, strB); // 1
                if (ActualResult != ExpectedResult)
                {
                    TestLibrary.TestFramework.LogError("013", "string CompareTo its with space one ExpectResult is greater 0 ActualResult is (" + ActualResult + ")");
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("014", "Unexpected exception" + e);
                retVal = false;
            }
            return retVal;
        }

        public bool PosTest8()
        {
            bool retVal = true;
            string strA;
            string strB;
            int ActualResult;

            TestLibrary.TestFramework.BeginScenario("PosTest8:string CompareTo its with space two");

            try
            {
                strA = "helloword";
                strB = "hello word";
                ActualResult = strA.CompareTo(strB);
                int ExpectedResult = GlobLocHelper.OSCompare(strA, strB); // 1
                if (ActualResult != ExpectedResult)
                {
                    TestLibrary.TestFramework.LogError("015", "string CompareTo its with space one ExpectResult is greater 0 ActualResult is (" + ActualResult + ")");
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("016", "Unexpected exception" + e);
                retVal = false;
            }
            return retVal;
        }

        public bool PosTest9()
        {
            bool retVal = true;
            string strA;
            string strB;
            int ActualResult;

            TestLibrary.TestFramework.BeginScenario("PosTest9:string CompareTo its with space three");

            try
            {
                strA = "helloword";
                strB = "helloword ";
                ActualResult = strA.CompareTo(strB);
                int ExpectedResult = GlobLocHelper.OSCompare(strA, strB); // -1
                if (ActualResult != ExpectedResult)
                {
                    TestLibrary.TestFramework.LogError("017", "string CompareTo its with space three ExpectResult is less 0 ActualResult is (" + ActualResult + ")");
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("018", "Unexpected exception" + e);
                retVal = false;
            }
            return retVal;
        }

        public bool PosTest10()
        {
            bool retVal = true;
            string strA;
            string strB;
            int ActualResult;

            TestLibrary.TestFramework.BeginScenario("PosTest10:strings with one char CompareTo");
            try
            {
                strA = "A";
                strB = "a";
                ActualResult = strA.CompareTo(strB);
                int ExpectedResult = GlobLocHelper.OSCompare(strA, strB); // 1
                if (ActualResult != ExpectedResult)
                {
                    TestLibrary.TestFramework.LogError("019", "strings with one char CompareTo ExpectResult is greater 0,ActualResult is (" + ActualResult + ")");
                    retVal = false;
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("020", "Unexpected Exception" + e);
                retVal = false;
            }
            return retVal;
        }

        public bool PosTest11()
        {
            bool retVal = true;
            string strA;
            string strB;
            int ActualResult;

            TestLibrary.TestFramework.BeginScenario("PosTest11:string CompareTo its with newline symbol one");
            try
            {
                strA = "hello\nword";
                strB = "helloword";
                ActualResult = strA.CompareTo(strB);
                int ExpectedResult = GlobLocHelper.OSCompare(strA, strB); // -1
                if (ActualResult != ExpectedResult)
                {
                    TestLibrary.TestFramework.LogError("021", "string CompareTo its with newline symbol one ExpectResult is less 0,ActualResult is (" + ActualResult + ")");
                    retVal = false;
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("022", "Unexpected Exception" + e);
                retVal = false;
            }
            return retVal;
        }

        public bool PosTest12()
        {
            bool retVal = true;
            string strA;
            string strB;
            int ActualResult;

            TestLibrary.TestFramework.BeginScenario("PosTest12:string CompareTo its with newline symbol two");
            try
            {
                strA = "helloword\n";
                strB = "helloword";
                ActualResult = strA.CompareTo(strB);
                int ExpectedResult = GlobLocHelper.OSCompare(strA, strB); // 1
                if (ActualResult != ExpectedResult)
                {
                    TestLibrary.TestFramework.LogError("023", "string CompareTo its with newline symbol two ExpectResult is great 0,ActualResult is (" + ActualResult + ")");
                    retVal = false;
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("024", "Unexpected Exception" + e);
                retVal = false;
            }
            return retVal;
        }

        public bool PosTest13()
        {
            bool retVal = true;
            string strA;
            string strB;
            int ActualResult;

            TestLibrary.TestFramework.BeginScenario("PosTest13:string CompareTo its with newline symbol three");
            try
            {
                strA = "\nhelloword";
                strB = "helloword";
                ActualResult = strA.CompareTo(strB);
                int ExpectedResult = GlobLocHelper.OSCompare(strA, strB); // -1
                if (ActualResult != ExpectedResult)
                {
                    TestLibrary.TestFramework.LogError("025", "string CompareTo its with newline symbol three ExpectResult is less 0,ActualResult is (" + ActualResult + ")");
                    retVal = false;
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("026", "Unexpected Exception" + e);
                retVal = false;
            }
            return retVal;
        }

        public bool PosTest14()
        {
            bool retVal = true;
            string strA;
            string strB;
            int ActualResult;

            TestLibrary.TestFramework.BeginScenario("PosTest14:strings embedded nulls CompareTo one");
            try
            {
                strA = "hello\0word";
                strB = "helloword";
                ActualResult = strA.CompareTo(strB);
                int ExpectedResult = GlobLocHelper.OSCompare(strA, strB); // 0
                if (ActualResult != ExpectedResult)
                {
                    TestLibrary.TestFramework.LogError("027", "strings embedded nulls CompareTo one ExpectResult is equel 0,ActualResult is (" + ActualResult + ")");
                    retVal = false;
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("028", "Unexpected Exception" + e);
                retVal = false;
            }
            return retVal;
        }

        public bool PosTest15()
        {
            bool retVal = true;
            string strA;
            string strB;
            int ActualResult;

            TestLibrary.TestFramework.BeginScenario("PosTest15:strings embedded nulls CompareTo two");
            try
            {
                strA = "helloword\0";
                strB = "helloword";
                ActualResult = strA.CompareTo(strB);
                int ExpectedResult = GlobLocHelper.OSCompare(strA, strB); // 0
                if (ActualResult != ExpectedResult)
                {
                    TestLibrary.TestFramework.LogError("029", "strings embedded nulls CompareTo two ExpectResult is equel 0,ActualResult is (" + ActualResult + ")");
                    retVal = false;
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("030", "Unexpected Exception" + e);
                retVal = false;
            }
            return retVal;
        }

        public bool PosTest16()
        {
            bool retVal = true;
            string strA;
            string strB;
            int ActualResult;

            TestLibrary.TestFramework.BeginScenario("PosTest16:strings embedded nulls CompareTo three");
            try
            {
                strA = "\0helloword";
                strB = "helloword";
                ActualResult = strA.CompareTo(strB);
                int ExpectedResult = GlobLocHelper.OSCompare(strA, strB); // 0
                if (ActualResult != ExpectedResult)
                {
                    TestLibrary.TestFramework.LogError("031", "strings embedded nulls CompareTo three ExpectResult is equel 0,ActualResult is (" + ActualResult + ")");
                    retVal = false;
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("032", "Unexpected Exception" + e);
                retVal = false;
            }
            return retVal;
        }

        public bool PosTest17()
        {
            bool retVal = true;
            string strA;
            string strB;
            int ActualResult;

            TestLibrary.TestFramework.BeginScenario("PosTest17:Globalized strings CompareTo");
            try
            {
                strA = "A\u0300";
                strB = "\u00C0";
                ActualResult = strA.CompareTo(strB);
                int ExpectedResult = GlobLocHelper.OSCompare(strA, strB); // 0
                if (ActualResult != ExpectedResult)
                {
                    TestLibrary.TestFramework.LogError("033", "Globalized strings CompareTo ExpectResult is equel 0,ActualResult is (" + ActualResult + ")");
                    retVal = false;
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("034", "Unexpected Exception" + e);
                retVal = false;
            }
            return retVal;
        }

        public bool PosTest18()
        {
            bool retVal = true;
            string strA;
            string strB;
            int ActualResult;

            TestLibrary.TestFramework.BeginScenario("PosTest18:Two different strings CompareTo one");
            try
            {
                strA = "A";
                strB = "£Á";
                ActualResult = strA.CompareTo(strB);
                int ExpectedResult = GlobLocHelper.OSCompare(strA, strB); // 1
                if (ActualResult != ExpectedResult)
                {
                    TestLibrary.TestFramework.LogError("035", "Two different strings CompareTo one ExpectResult is less 0,ActualResult is (" + ActualResult + ")");
                    retVal = false;
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("036", "Unexpected Exception" + e);
                retVal = false;
            }
            return retVal;
        }

        public bool PosTest19()
        {
            bool retVal = true;
            string strA;
            string strB;
            int ActualResult;

            TestLibrary.TestFramework.BeginScenario("PosTest19:Two different strings CompareTo two");
            try
            {
                strA = "\uD801\uDC00";
                strB = "\uD801\uDC28";
                ActualResult = strA.CompareTo(strB);
                int ExpectedResult = GlobLocHelper.OSCompare(strA, strB); // -1
                if (ActualResult != ExpectedResult)
                {
                    TestLibrary.TestFramework.LogError("037", "Two different strings CompareTo two ExpectResult is less 0,ActualResult is (" + ActualResult + ")");
                    retVal = false;
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("038", "Unexpected Exception" + e);
                retVal = false;
            }
            return retVal;
        }

        public bool PosTest20()
        {
            bool retVal = true;
            int ActualResult;
            string strA;
            string strB;

            TestLibrary.TestFramework.BeginScenario("PosTest20: Two different strings CompareTo three");

            try
            {
                strA = "\\\\my documents\\my files\\";
                strB = @"\\my documents\my files\";
                ActualResult = strA.CompareTo(strB);
                int ExpectedResult = GlobLocHelper.OSCompare(strA, strB); // 0
                if (ActualResult != ExpectedResult)
                {
                    TestLibrary.TestFramework.LogError("039", "Two different strings CompareTo three Expected Result is equel 0,Actual Result is ( " + ActualResult + ")");
                    retVal = false;
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("040", "Unexpected exception: " + e);
                retVal = false;
            }
            return retVal;
        }

        public bool PosTest21()
        {
            bool retVal = true;
            int ActualResult;
            string strA;
            string strB;

            TestLibrary.TestFramework.BeginScenario("PosTest21: Tab CompareTo four spaces ");

            try
            {
                strA = "\t";
                strB = "    ";
                ActualResult = strA.CompareTo(strB);
                int ExpectedResult = GlobLocHelper.OSCompare(strA, strB); // 1
                if (ActualResult != ExpectedResult)
                {
                    TestLibrary.TestFramework.LogError("041", "Tab CompareTo four spaces Expected Result is greater 0,Actual Result is ( " + ActualResult + ")");
                    retVal = false;
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("042", "Unexpected exception: " + e);
                retVal = false;
            }
            return retVal;
        }



        #endregion       

        #region Negative Testing
        public bool NegTest1()
        {
            bool retVal = true;
            string strA;
            string strB;
            int ActualResult;

            TestLibrary.TestFramework.BeginScenario("NegTest1:null CompareTo null");
            try
            {
                strA = null;
                strB = null;
                ActualResult = strA.CompareTo(strB);
                retVal = false;
            }
            catch(NullReferenceException)
            {
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("N001", "Unexpected exception:" + e);
                retVal = false;
            }
            return retVal;
        }

        public bool NegTest2()
        {
            bool retVal = true;
            string strA;
            string strB;
            int ActualResult;

            TestLibrary.TestFramework.BeginScenario("NegTest2:null CompareTo null string");
            try
            {
                strA = null;
                strB = "";
                ActualResult = strA.CompareTo(strB);
                retVal = false;
            }
            catch (NullReferenceException)
            {
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("N002", "Unexpected exception:" + e);
                retVal = false;
            }
            return retVal;
        }

        public bool NegTest3()
        {
            bool retVal = true;
            string strA;
            string strB;
            int ActualResult;

            TestLibrary.TestFramework.BeginScenario("NegTest3:null CompareTo not null string");
            try
            {
                strA = null;
                strB = TestLibrary.Generator.GetString(-55, false,c_MIN_STRING_LEN,c_MAX_STRING_LEN);
                ActualResult = strA.CompareTo(strB);
                retVal = false;
            }
            catch (NullReferenceException)
            {
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("N003", "Unexpected exception:" + e);
                retVal = false;
            }
            return retVal;
        }

        #endregion

    }
}
