//--------------------------------------------------------------------------
//
//		DiscretionaryAcl test cases
//
//		Tests the DiscretionaryAcl, the base abstract class GenericACL's and CommonAcl's functionality
//
//		Copyright (C) Microsoft Corporation, 2003
//
//
//--------------------------------------------------------------------------


using System;
using System.Collections;
using System.IO;
using Microsoft.Win32;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.InteropServices;

namespace System.Security.AccessControl.Test
{

    //----------------------------------------------------------------------------------------------
    /*
    *  Class to test DiscretionaryAcl and its abstract base classes CommonAcl and GenericAcl
    *
    *
    */
    //----------------------------------------------------------------------------------------------

    public class DiscretionaryAclTestCases
    {
        public static readonly string Constructor1TestCaseStore = "TestCaseStores\\DiscretionaryAcl_Constructor1.inf";
        public static readonly string Constructor2TestCaseStore = "TestCaseStores\\DisCretionaryAcl_Constructor2.inf";
        public static readonly string Constructor3TestCaseStore = "TestCaseStores\\DisCretionaryAcl_Constructor3.inf";
        public static readonly string AddAccessTestCaseStore = "TestCaseStores\\DisCretionaryAcl_AddAccess.inf";
        public static readonly string SetAccessTestCaseStore = "TestCaseStores\\DisCretionaryAcl_SetAccess.inf";
        public static readonly string RemoveAccessTestCaseStore = "TestCaseStores\\DisCretionaryAcl_RemoveAccess.inf";
        public static readonly string RemoveAccessSpecificTestCaseStore = "TestCaseStores\\DisCretionaryAcl_RemoveAccessSpecific.inf";

        //No creating objects!
        public DiscretionaryAclTestCases() { }

        public static Boolean Test()
        {
            Console.WriteLine("\n\n=======STARTING DiscretionaryAclTestCases==========\n");
            return AllTestCases();
        }

        /*
        * Method Name: AllTestCases
        *
        * Description:	call each private class's AllTestCases method to test each public\protected method\constructor\
        * 			properties\index
        *
        * Parameter:	testCasesPerformed -- sum of all the test cases performed
        *			testCasePassed -- total number of test cases passed
        *
        * Return:		none
        */

        public static Boolean AllTestCases()
        {

            Console.WriteLine("Running DiscretionaryAclTestCases");

            int testCasesPerformed = 0;
            int testCasesPassed = 0;


            Constructor1TestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


            Constructor2TestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


            Constructor3TestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


            RemoveInheritedAcesTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


            PurgeTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


            BinaryLengthTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


            AceCountTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


            IndexTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


            AddAccessTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


            SetAccessTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


            RemoveAccessTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


            RemoveAccessSpecificTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


            GetBinaryFormTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);

            return (testCasesPerformed == testCasesPassed);

        }

        public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
        {
            Console.WriteLine("Running DiscretionaryAclTestCases");

            Console.WriteLine("Running Constructor1TestCases");
            Constructor1TestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

            Console.WriteLine("Running Constructor2TestCases");
            Constructor2TestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

            Console.WriteLine("Running Constructor3TestCases");
            Constructor3TestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

            Console.WriteLine("Running RemoveInheritedAcesTestCases");
            RemoveInheritedAcesTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

            Console.WriteLine("Running PurgeTestCases");
            PurgeTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

            Console.WriteLine("Running AceCountTestCases");
            AceCountTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);

            Console.WriteLine("Running IndexTestCases");
            IndexTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);

            Console.WriteLine("Running AddAccessTestCases");
            AddAccessTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

            Console.WriteLine("Running SetAccessTestCases");
            SetAccessTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

            Console.WriteLine("Running RemoveAccessTestCases");
            RemoveAccessTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

            Console.WriteLine("Running RemoveAccessSpecificTestCases");
            RemoveAccessSpecificTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

            Console.WriteLine("Running GetBinaryFormTestCases.AllTestCases()");
            GetBinaryFormTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
        }

        public static void BVTTestCases(ref int testCasesPerformed, ref int testCasesPassed)
        {
            // No test cases yet
        }

        //----------------------------------------------------------------------------------------------
        /*
        *  Class to test DiscretionaryAcl constructor 1
        *		public DiscretionaryAcl( bool isContainer, bool isDS, int capacity )
        *
        *
        */
        //----------------------------------------------------------------------------------------------

        private class Constructor1TestCases
        {
            //No creating objects

            private Constructor1TestCases() { }

            /*
            * Method Name: AllTestCases
            *
            * Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
            *			DiscretionaryAcl constructor -- public DiscretionaryAcl( bool isContainer, bool isDS, int capacity )
            *
            * Parameter:	testCasesPerformed -- sum of all the test cases performed
            *			testCasePassed -- total number of test cases passed
            *
            * Return:		none
            */

            public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {

                Console.WriteLine("Running Constructor1TestCases");

                BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, false);
                AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
            }
            /*
            * Method Name: BasicValidationTestCases
            *
            * Description:	read in and execute basic testing cases 
            *
            * Parameter:	testCasesPerformed -- sum of all the test cases performed
            *			testCasePassed -- total number of test cases passed
            *
            * Return:		none
            */

            public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed, bool isBVT)
            {
                Console.WriteLine("Running BasicValidationTestCases");


                int bvtCaseCounter = 0;

                string[] testCase = null;
                ITestCaseReader reader = new InfTestCaseStore();
                reader.OpenTestCaseStore(Constructor1TestCaseStore);

                while (null != (testCase = reader.ReadNextTestCase()))
                {
                    //read isContainer
                    bool isContainer;
                    if (1 > testCase.Length)
                        throw new ArgumentOutOfRangeException();
                    else
                        isContainer = bool.Parse(testCase[0]);

                    // read isDS
                    bool isDS;
                    if (2 > testCase.Length)
                        throw new ArgumentOutOfRangeException();
                    else
                        isDS = bool.Parse(testCase[1]);

                    // read capacity
                    int capacity;
                    if (3 > testCase.Length)
                        throw new ArgumentOutOfRangeException();
                    else
                        capacity = int.Parse(testCase[2]);

                    testCasesPerformed++;


                    try
                    {
                        if (TestConstructor(isContainer, isDS, capacity))
                        {
                            Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                            testCasesPassed++;
                        }
                        else
                        {
                            Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception generated: " + e.Message, e);
                    }


                    bvtCaseCounter++;
                    if (isBVT && (bvtCaseCounter == 3))
                    {
                        //first 3 cases are BVT cases
                        break;
                    }
                }

                reader.CloseTestCaseStore();
            }

            /*
            * Method Name: AdditionalTestCases
            *
            * Description:	boundary cases, invalid cases etc.  
            *
            * Parameter:	testCasesPerformed -- sum of all the test cases performed
            *			testCasePassed -- total number of test cases passed
            *
            * Return:		none
            */

            public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                DiscretionaryAcl discretionaryAcl = null;
                bool isContainer = false;
                bool isDS = false;
                int capacity = 0;

                Console.WriteLine("Running AdditionalTestCases");



                //case 1, capacity = -1
                testCasesPerformed++;


                try
                {
                    capacity = -1;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, capacity);
                    Console.WriteLine("Should not allow creation of negative capacity DiscretionaryAcl");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                }
                catch (ArgumentOutOfRangeException)
                {
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 2, capacity = Int32.MaxValue/2
                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;
                    capacity = Int32.MaxValue / 2;
                    if (TestConstructor(isContainer, isDS, capacity))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                    {
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                    }

                }
                catch (OutOfMemoryException)
                {//most possibably there are not enough memory
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 3, capacity = Int32.MaxValue
                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = true;
                    capacity = Int32.MaxValue;
                    if (TestConstructor(isContainer, isDS, capacity))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                    {
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                    }

                }
                catch (OutOfMemoryException)
                {//usually there are not enough memory
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }

            }

            /*
            * Method Name: TestConstructor
            *
            * Description:	check test case passes or fails  
            *
            * Parameter:	isContainer, isDS, capacity -- parameters to pass to the DiscretionaryAcl constructor
            *
            * Return:		true if test pass, false otherwise
            */

            private static bool TestConstructor(bool isContainer, bool isDS, int capacity)
            {
                bool result = true;
                byte[] dAclBinaryForm = null;
                byte[] rAclBinaryForm = null;

                RawAcl rawAcl = null;

                DiscretionaryAcl discretionaryAcl = null;

                discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, capacity);
                rawAcl = new RawAcl(isDS ? GenericAcl.AclRevisionDS : GenericAcl.AclRevision, capacity);

                if (isContainer == discretionaryAcl.IsContainer &&
                    isDS == discretionaryAcl.IsDS &&
                    (isDS ? GenericAcl.AclRevisionDS : GenericAcl.AclRevision) == discretionaryAcl.Revision &&
                    0 == discretionaryAcl.Count &&
                    8 == discretionaryAcl.BinaryLength &&
                    true == discretionaryAcl.IsCanonical)
                {
                    dAclBinaryForm = new byte[discretionaryAcl.BinaryLength];
                    rAclBinaryForm = new byte[rawAcl.BinaryLength];
                    discretionaryAcl.GetBinaryForm(dAclBinaryForm, 0);
                    rawAcl.GetBinaryForm(rAclBinaryForm, 0);
                    if (!Utils.UtilIsBinaryFormEqual(dAclBinaryForm, rAclBinaryForm))
                        result = false;

                    //redundant index check
                    for (int i = 0; i < discretionaryAcl.Count; i++)
                    {
                        if (!Utils.UtilIsAceEqual(discretionaryAcl[i], rawAcl[i]))
                        {
                            result = false;
                            break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("the new DiscretionaryAcl is not expected");
                    result = false;
                }

                return result;


            }
        }

        //----------------------------------------------------------------------------------------------
        /*
        *  Class to test DiscretionaryAcl constructor 2
        *		public DiscretionaryAcl( bool isContainer, bool isDS, byte revision, int capacity )
        *
        *
        */
        //----------------------------------------------------------------------------------------------

        private class Constructor2TestCases
        {
            //No creating objects

            private Constructor2TestCases() { }

            /*
            * Method Name: AllTestCases
            *
            * Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
            *			DiscretionaryAcl constructor -- public DiscretionaryAcl( bool isContainer, bool isDS, byte revision, int capacity )
            *
            * Parameter:	testCasesPerformed -- sum of all the test cases performed
            *			testCasePassed -- total number of test cases passed
            *
            * Return:		none
            */

            public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                Console.WriteLine("Running Constructor2TestCases");

                BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, false);
                AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
            }

            /*
            * Method Name: BasicValidationTestCases
            *
            * Description:	read in and execute basic testing cases 
            *
            * Parameter:	testCasesPerformed -- sum of all the test cases performed
            *			testCasePassed -- total number of test cases passed
            *
            * Return:		none
            */
            public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed, bool isBVT)
            {
                Console.WriteLine("Running BasicValidationTestCases");


                int bvtCaseCounter = 0;

                string[] testCase = null;
                ITestCaseReader reader = new InfTestCaseStore();
                reader.OpenTestCaseStore(Constructor2TestCaseStore);

                while (null != (testCase = reader.ReadNextTestCase()))
                {
                    //read isContainer
                    bool isContainer;
                    if (1 > testCase.Length)
                        throw new ArgumentOutOfRangeException();
                    else
                        isContainer = bool.Parse(testCase[0]);

                    // read isDS
                    bool isDS;
                    if (2 > testCase.Length)
                        throw new ArgumentOutOfRangeException();
                    else
                        isDS = bool.Parse(testCase[1]);

                    // read revision
                    byte revision;
                    if (3 > testCase.Length)
                        throw new ArgumentOutOfRangeException();
                    else
                        revision = byte.Parse(testCase[2]);

                    // read capacity
                    int capacity;
                    if (4 > testCase.Length)
                        throw new ArgumentOutOfRangeException();
                    else
                        capacity = int.Parse(testCase[3]);

                    testCasesPerformed++;


                    try
                    {
                        if (TestConstructor(isContainer, isDS, revision, capacity))
                        {
                            Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                            testCasesPassed++;
                        }
                        else
                            Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception generated: " + e.Message, e);
                    }


                    bvtCaseCounter++;
                    if (isBVT && (bvtCaseCounter == 3))
                    {
                        //first 3 cases are BVT cases
                        break;
                    }
                }

                reader.CloseTestCaseStore();
            }

            /*
            * Method Name: AdditionalTestCases
            *
            * Description:	boundary cases, invalid cases etc.  
            *
            * Parameter:	testCasesPerformed -- sum of all the test cases performed
            *			testCasePassed -- total number of test cases passed
            *
            * Return:		none
            */
            public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                DiscretionaryAcl discretionaryAcl = null;
                bool isContainer = false;
                bool isDS = false;
                byte revision = 0;
                int capacity = 0;

                Console.WriteLine("Running AdditionalTestCases");



                //case 1, capacity = -1
                testCasesPerformed++;


                try
                {
                    capacity = -1;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, revision, capacity);
                    Console.WriteLine("Should not allow creation of negative capacity DiscretionaryAcl");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (ArgumentOutOfRangeException)
                {
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }



                //case 2, capacity = Int32.MaxValue/2
                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;
                    revision = 0;
                    capacity = Int32.MaxValue / 2;
                    if (TestConstructor(isContainer, isDS, revision, capacity))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                    {
                        Console.WriteLine("the new DiscretionaryAcl is not expected");
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                    }

                }
                catch (OutOfMemoryException)
                {//most possibably there are not enough memory
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 3, capacity = Int32.MaxValue
                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = true;
                    revision = 255;
                    capacity = Int32.MaxValue;
                    if (TestConstructor(isContainer, isDS, revision, capacity))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                    {
                        Console.WriteLine("the new DiscretionaryAcl is not expected");
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                    }

                }
                catch (OutOfMemoryException)
                {//usually there are not enough memory
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }

            }

            /*
            * Method Name: TestConstructor
            *
            * Description:	check test case passes or fails  
            *
            * Parameter:	isContainer, isDS, revision, capacity -- parameters to pass to the SystemAcl constructor
            *
            * Return:		true if test pass, false otherwise
            */

            private static bool TestConstructor(bool isContainer, bool isDS, byte revision, int capacity)
            {
                bool result = true;
                byte[] dAclBinaryForm = null;
                byte[] rAclBinaryForm = null;

                RawAcl rawAcl = null;

                DiscretionaryAcl discretionaryAcl = null;


                discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, revision, capacity);
                rawAcl = new RawAcl(revision, capacity);

                if (isContainer == discretionaryAcl.IsContainer &&
                    isDS == discretionaryAcl.IsDS &&
                    revision == discretionaryAcl.Revision &&
                    0 == discretionaryAcl.Count &&
                    8 == discretionaryAcl.BinaryLength &&
                    true == discretionaryAcl.IsCanonical)
                {
                    dAclBinaryForm = new byte[discretionaryAcl.BinaryLength];
                    rAclBinaryForm = new byte[rawAcl.BinaryLength];
                    discretionaryAcl.GetBinaryForm(dAclBinaryForm, 0);
                    rawAcl.GetBinaryForm(rAclBinaryForm, 0);
                    if (!Utils.UtilIsBinaryFormEqual(dAclBinaryForm, rAclBinaryForm))
                        result = false;

                    //redundant index check
                    for (int i = 0; i < discretionaryAcl.Count; i++)
                    {
                        if (!Utils.UtilIsAceEqual(discretionaryAcl[i], rawAcl[i]))
                        {
                            result = false;
                            break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("the new DiscretionaryAcl is not expected");
                    result = false;
                }

                return result;
            }
        }


        //----------------------------------------------------------------------------------------------
        /*
        *  Class to test DiscretionaryAcl constructor 3
        *		public DiscretionaryAcl( bool isContainer, bool isDS, RawAcl rawAcl )
        *
        *
        */
        //----------------------------------------------------------------------------------------------


        private class Constructor3TestCases
        {
            //No creating objects

            private Constructor3TestCases() { }

            public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                Console.WriteLine("Running Constructor3TestCases");

                BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, false);
                AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
                Additional2TestCases(ref testCasesPerformed, ref testCasesPassed);
            }

            public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed, bool isBVT)
            {

                Console.WriteLine("Running BasicValidationTestCases");


                int bvtCaseCounter = 0;

                RawAcl rawAcl = null;
                DiscretionaryAcl discretionaryAcl = null;
                bool isContainer = false;
                bool isDS = false;
                bool wasCanonicalInitially = false;

                string[] testCase = null;
                ITestCaseReader reader = new InfTestCaseStore();
                reader.OpenTestCaseStore(Constructor3TestCaseStore);

                while (null != (testCase = reader.ReadNextTestCase()))
                {
                    // read isContainer
                    if (1 > testCase.Length)
                        isContainer = false;
                    else
                        isContainer = bool.Parse(testCase[0]);

                    // read isDS
                    if (2 > testCase.Length)
                        isDS = false;
                    else
                        isDS = bool.Parse(testCase[1]);

                    //read initialRawAclStr
                    string initialRawAclStr;
                    if (3 > testCase.Length)
                        initialRawAclStr = "";
                    else
                        initialRawAclStr = testCase[2];

                    //read verifierRawAclStr
                    string verifierRawAclStr;
                    if (4 > testCase.Length)
                        verifierRawAclStr = "";
                    else
                        verifierRawAclStr = testCase[3];

                    // read wasCanonicalInitially
                    if (5 > testCase.Length)
                        wasCanonicalInitially = false;
                    else
                        wasCanonicalInitially = bool.Parse(testCase[4]);

                    //create a discretionaryAcl

                    rawAcl = Utils.UtilCreateRawAclFromString(initialRawAclStr);

                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    rawAcl = Utils.UtilCreateRawAclFromString(verifierRawAclStr);

                    testCasesPerformed++;


                    try
                    {
                        if (TestConstructor(discretionaryAcl, isContainer, isDS, wasCanonicalInitially, rawAcl))
                        {
                            Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                            testCasesPassed++;
                        }
                        else
                            Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception generated: " + e.Message, e);
                    }


                    bvtCaseCounter++;
                    if (isBVT && (bvtCaseCounter == 3))
                    {
                        //first 3 cases are BVT cases
                        break;
                    }
                }
            }


            /*
            * Method Name: TestConstructor
            *
            * Description:	check if the DiscretionaryAcl object's properties are as excepted, especially check the number of ACEs and order
            *			of ACEs
            *
            * Parameter:	discretionaryAcl -- the DiscretionaryAcl object to be checked
            *			rawAcl -- this validation rawAcl the SystemAcl object to be compared with
            *			isContainer, isDS, revision, wasCanonicalIntially -- expected properties to be comparied with the DiscretionaryAcl object
            *
            * Return:		true if test pass, false otherwise
            */
            private static bool TestConstructor(DiscretionaryAcl discretionaryAcl, bool isContainer, bool isDS, bool wasCanonicalInitially, RawAcl rawAcl)
            {
                bool result = true;
                byte[] dAclBinaryForm = null;
                byte[] rAclBinaryForm = null;

                if (discretionaryAcl.IsContainer == isContainer &&
                    discretionaryAcl.IsDS == isDS &&
                    discretionaryAcl.Revision == rawAcl.Revision &&
                    discretionaryAcl.Count == rawAcl.Count &&
                    discretionaryAcl.BinaryLength == rawAcl.BinaryLength &&
                    discretionaryAcl.IsCanonical == wasCanonicalInitially)
                {
                    dAclBinaryForm = new byte[discretionaryAcl.BinaryLength];
                    rAclBinaryForm = new byte[rawAcl.BinaryLength];
                    discretionaryAcl.GetBinaryForm(dAclBinaryForm, 0);
                    rawAcl.GetBinaryForm(rAclBinaryForm, 0);
                    if (!Utils.UtilIsBinaryFormEqual(dAclBinaryForm, rAclBinaryForm))
                        result = false;

                    //redundant index check
                    for (int i = 0; i < discretionaryAcl.Count; i++)
                    {
                        if (!Utils.UtilIsAceEqual(discretionaryAcl[i], rawAcl[i]))
                        {
                            result = false;
                            break;
                        }

                    }
                }
                else
                {
                    Console.WriteLine("the new DiscretionaryAcl is not expected");
                    Console.WriteLine("discretionaryAcl.IsContainter --" + discretionaryAcl.IsContainer);
                    Console.WriteLine("expected isContainer --" + isContainer);
                    Console.WriteLine("discretionaryAcl.IsDS --" + discretionaryAcl.IsDS);
                    Console.WriteLine("expected isDS --" + isDS);
                    Console.WriteLine("discretionaryAcl.WasCanonicalInitially --" + discretionaryAcl.IsCanonical);
                    Console.WriteLine("expected wasCanonicalInitially --" + wasCanonicalInitially);
                    Console.WriteLine("discretionaryAcl.Count --" + discretionaryAcl.Count);
                    Console.WriteLine("rawAcl.Count --" + rawAcl.Count);

                    result = false;
                }

                return result;
            }


            public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {

                bool isContainer = false;
                bool isDS = false;

                RawAcl rawAcl = null;
                DiscretionaryAcl discretionaryAcl = null;

                GenericAce gAce = null;
                byte revision = 0;
                int capacity = 0;

                //CustomAce constructor parameters
                AceType aceType = AceType.AccessAllowed;
                AceFlags aceFlag = AceFlags.None;
                byte[] opaque = null;

                //CompoundAce constructor additional parameters
                int accessMask = 0;
                CompoundAceType compoundAceType = CompoundAceType.Impersonation;
                string sid = "BG";

                //CommonAce constructor additional parameters
                AceQualifier aceQualifier = 0;

                //ObjectAce constructor additional parameters
                ObjectAceFlags objectAceFlag = 0;
                Guid objectAceType;
                Guid inheritedObjectAceType;

                Console.WriteLine("Running AdditionalTestCases");



                //case 1, an AccessAllowed ACE with a zero access mask is meaningless, will be removed
                testCasesPerformed++;


                try
                {
                    revision = 0;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 0,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
                    rawAcl.InsertAce(0, gAce);
                    isContainer = false;
                    isDS = false;

                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    //drop the Ace from rawAcl
                    rawAcl.RemoveAce(0);

                    //the only ACE is a meaningless ACE, will be removed
                    if (TestConstructor(discretionaryAcl, isContainer, isDS, true, rawAcl))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 2, an inherit-only AccessDenied ACE on an object ACL is meaningless, will be removed
                testCasesPerformed++;


                try
                {
                    revision = 0;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    //15 has all inheritance AceFlags but Inherited
                    gAce = new CommonAce((AceFlags)15, AceQualifier.AccessDenied, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
                    rawAcl.InsertAce(0, gAce);
                    isContainer = false;
                    isDS = false;

                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    rawAcl.RemoveAce(0);

                    //the only ACE is a meaningless ACE, will be removed
                    if (TestConstructor(discretionaryAcl, isContainer, isDS, true, rawAcl))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }



                //case 3, an inherit-only AccessAllowed ACE without ContainerInherit or ObjectInherit flags on a container object is meaningless, will be removed
                testCasesPerformed++;


                try
                {
                    revision = 0;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    //8 has inheritOnly
                    gAce = new CommonAce((AceFlags)8, AceQualifier.AccessAllowed, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
                    rawAcl.InsertAce(0, gAce);
                    isContainer = true;
                    isDS = false;

                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    rawAcl.RemoveAce(0);

                    //the only ACE is a meaningless ACE, will be removed
                    if (TestConstructor(discretionaryAcl, isContainer, isDS, true, rawAcl))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 4, 1 CustomAce
                testCasesPerformed++;


                try
                {
                    revision = 127;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    aceType = AceType.MaxDefinedAceType + 1;
                    aceFlag = AceFlags.None;
                    opaque = null;
                    gAce = new CustomAce(aceType, aceFlag, opaque);
                    rawAcl.InsertAce(0, gAce);
                    isContainer = false;
                    isDS = false;

                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    //Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical
                    if (TestConstructor(discretionaryAcl, isContainer, isDS, false, rawAcl))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }



                //case 5, 1 CompoundAce
                testCasesPerformed++;


                try
                {
                    revision = 127;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    // 2 has ContainerInherit
                    aceFlag = (AceFlags)2;
                    accessMask = 1;
                    compoundAceType = CompoundAceType.Impersonation;
                    gAce = new CompoundAce(aceFlag, accessMask, compoundAceType,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)));
                    rawAcl.InsertAce(0, gAce);
                    isContainer = true;
                    isDS = false;

                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    //Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical
                    if (TestConstructor(discretionaryAcl, isContainer, isDS, false, rawAcl))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }



                //case 6, 1 ObjectAce
                testCasesPerformed++;


                try
                {
                    revision = 127;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    aceFlag = (AceFlags)15; //all inheritance flags ored together but Inherited
                    aceQualifier = AceQualifier.AccessAllowed;
                    accessMask = 1;
                    objectAceFlag = ObjectAceFlags.ObjectAceTypePresent | ObjectAceFlags.InheritedObjectAceTypePresent;
                    objectAceType = new Guid("11111111-1111-1111-1111-111111111111");
                    inheritedObjectAceType = new Guid("22222222-2222-2222-2222-222222222222");
                    gAce = new ObjectAce(aceFlag, aceQualifier, accessMask,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), objectAceFlag, objectAceType, inheritedObjectAceType, false, null);
                    rawAcl.InsertAce(0, gAce);
                    isContainer = true;
                    isDS = true;

                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    if (TestConstructor(discretionaryAcl, isContainer, isDS, true, rawAcl))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 7, no Ace
                testCasesPerformed++;


                try
                {
                    revision = 127;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    isContainer = true;
                    isDS = false;

                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    if (TestConstructor(discretionaryAcl, isContainer, isDS, true, rawAcl))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 8, all Aces from case 1, and 3 to 6 
                testCasesPerformed++;


                try
                {
                    revision = 127;
                    capacity = 5;
                    sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")).ToString();
                    rawAcl = new RawAcl(revision, capacity);
                    //0 access Mask
                    gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 0,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid + 1.ToString())), false, null);
                    rawAcl.InsertAce(rawAcl.Count, gAce);

                    //an inherit-only AccessAllowed ACE without ContainerInherit or ObjectInherit flags on a container object is meaningless, will be removed

                    gAce = new CommonAce((AceFlags)8, AceQualifier.AccessAllowed, 1,
                    new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid + 2.ToString())), false, null);
                    rawAcl.InsertAce(rawAcl.Count, gAce);

                    // ObjectAce
                    aceFlag = (AceFlags)15; //all inheritance flags ored together but Inherited
                    aceQualifier = AceQualifier.AccessAllowed;
                    accessMask = 1;
                    objectAceFlag = ObjectAceFlags.ObjectAceTypePresent | ObjectAceFlags.InheritedObjectAceTypePresent;
                    objectAceType = new Guid("11111111-1111-1111-1111-111111111111");
                    inheritedObjectAceType = new Guid("22222222-2222-2222-2222-222222222222");
                    gAce = new ObjectAce(aceFlag, aceQualifier, accessMask,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid + 3.ToString())), objectAceFlag, objectAceType, inheritedObjectAceType, false, null);
                    rawAcl.InsertAce(rawAcl.Count, gAce);

                    // CustomAce
                    aceType = AceType.MaxDefinedAceType + 1;
                    aceFlag = AceFlags.None;
                    opaque = null;
                    gAce = new CustomAce(aceType, aceFlag, opaque);
                    rawAcl.InsertAce(rawAcl.Count, gAce);

                    // CompoundAce					
                    aceFlag = (AceFlags)2;
                    accessMask = 1;
                    compoundAceType = CompoundAceType.Impersonation;
                    gAce = new CompoundAce(aceFlag, accessMask, compoundAceType,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid + 4.ToString())));
                    rawAcl.InsertAce(rawAcl.Count, gAce);

                    isContainer = true;
                    isDS = false;

                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    rawAcl.RemoveAce(0);
                    rawAcl.RemoveAce(0);

                    //Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical

                    if (TestConstructor(discretionaryAcl, isContainer, isDS, false, rawAcl))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


            }

            /*
            * Method Name: Additional2TestCases
            *
            * Description:	more boundary cases, invalid cases etc.  
            *
            * Parameter:	testCasesPerformed -- sum of all the test cases performed
            *			testCasePassed -- total number of test cases passed
            *
            * Return:		none
            */

            public static void Additional2TestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                DiscretionaryAcl discretionaryAcl = null;
                bool isContainer = false;
                bool isDS = false;
                RawAcl rawAcl = null;

                Console.WriteLine("Running Additional2TestCases");




                //case 1, rawAcl = null
                testCasesPerformed++;


                try
                {
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    rawAcl = new RawAcl(isDS ? GenericAcl.AclRevisionDS : GenericAcl.AclRevision, 1);
                    if (TestConstructor(discretionaryAcl, isContainer, isDS, true, rawAcl))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }

            }
        }

        //----------------------------------------------------------------------------------------------
        /*
        *  Class to test DiscretionaryAcl property inherited from abstract base class CommonAcl
        *		public sealed override int BinaryLength
        *
        *
        */
        //----------------------------------------------------------------------------------------------

        private class BinaryLengthTestCases
        {
            //No creating objects

            private BinaryLengthTestCases() { }

            public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {

                Console.WriteLine("Running BinaryLengthTestCases");

                BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
                AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
            }

            public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                Console.WriteLine("Running BasicValidationTestCases");



                RawAcl rawAcl = null;
                DiscretionaryAcl discretionaryAcl = null;
                GenericAce gAce = null;
                byte revision = 0;
                int capacity = 0;
                string sid = "BG";

                //case 1, empty discretionaryAcl, binarylength should be 8
                testCasesPerformed++;


                try
                {
                    capacity = 1;
                    discretionaryAcl = new DiscretionaryAcl(false, false, capacity);
                    if (8 == discretionaryAcl.BinaryLength)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 2, discretionaryAcl with one Ace, binarylength should be 8 + the Ace's binarylength
                testCasesPerformed++;


                try
                {
                    revision = 0;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
                    rawAcl.InsertAce(0, gAce);
                    discretionaryAcl = new DiscretionaryAcl(true, false, rawAcl);
                    if (8 + gAce.BinaryLength == discretionaryAcl.BinaryLength)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 3, DiscretionaryAcl with two Aces
                testCasesPerformed++;


                try
                {
                    revision = 0;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    gAce = new CommonAce(AceFlags.None, AceQualifier.AccessDenied, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
                    rawAcl.InsertAce(0, gAce);
                    gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 2,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                    rawAcl.InsertAce(0, gAce);
                    discretionaryAcl = new DiscretionaryAcl(false, false, rawAcl);
                    if (8 + discretionaryAcl[0].BinaryLength + discretionaryAcl[1].BinaryLength == discretionaryAcl.BinaryLength)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


            }

            public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                RawAcl rawAcl = null;
                DiscretionaryAcl discretionaryAcl = null;
                GenericAce gAce = null;
                byte revision = 0;
                int capacity = 0;
                string sid = "BG";
                sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)).ToString();
                int expectedLength = 0;

                Console.WriteLine("Running AdditionalTestCases");



                //case 1, DiscretionaryAcl with huge number of Aces
                testCasesPerformed++;


                try
                {
                    revision = 0;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    expectedLength = 8;

                    for (int i = 0; i < 1820; i++)
                    {
                        gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, i + 1,
                            new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid + i.ToString())), false, null);
                        rawAcl.InsertAce(0, gAce);
                        expectedLength += gAce.BinaryLength;
                    }
                    discretionaryAcl = new DiscretionaryAcl(false, false, rawAcl);
                    if (expectedLength == discretionaryAcl.BinaryLength)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }

            }
        }

        //----------------------------------------------------------------------------------------------
        /*
        *  Class to test DiscretionaryAcl property inherited from abstract base class CommonAcl
        *		public sealed override int Count
        *
        *
        */
        //----------------------------------------------------------------------------------------------

        private class AceCountTestCases
        {
            //No creating objects

            private AceCountTestCases() { }

            public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                Console.WriteLine("Running AceCountTestCases");

                BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
                AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
            }

            public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                Console.WriteLine("Running BasicValidationTestCases");



                RawAcl rawAcl = null;
                GenericAce gAce = null;
                DiscretionaryAcl discretionaryAcl = null;
                bool isContainer = false;
                bool isDS = false;
                byte revision = 0;
                int capacity = 0;
                string sid = "BG";

                //case 1, empty DiscretionaryAcl
                testCasesPerformed++;


                try
                {
                    revision = 0;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    isContainer = false;
                    isDS = false;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    if (0 == discretionaryAcl.Count)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 2, DiscretionaryAcl with one Ace
                testCasesPerformed++;


                try
                {
                    revision = 0;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
                    rawAcl.InsertAce(0, gAce);
                    isContainer = false;
                    isDS = false;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    if (1 == discretionaryAcl.Count)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }



                //case 3, DiscretionaryAcl with two Aces
                testCasesPerformed++;


                try
                {
                    revision = 0;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    //223 has all AceFlags
                    gAce = new CommonAce((AceFlags)223, AceQualifier.AccessAllowed, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
                    rawAcl.InsertAce(0, gAce);
                    gAce = new CommonAce(AceFlags.None, AceQualifier.AccessDenied, 2,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                    rawAcl.InsertAce(0, gAce);
                    isContainer = true;
                    isDS = false;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    if (2 == discretionaryAcl.Count)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }

            }

            public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                RawAcl rawAcl = null;
                GenericAce gAce = null;
                byte revision = 0;
                int capacity = 0;
                string sid = "BG";
                DiscretionaryAcl discretionaryAcl = null;
                bool isContainer = false;
                bool isDS = false;

                Console.WriteLine("Running AdditionalTestCases");



                //case 1, DiscretionaryAcl with huge number of Aces
                testCasesPerformed++;


                try
                {
                    revision = 0;
                    capacity = 1;
                    sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)).ToString();
                    rawAcl = new RawAcl(revision, capacity);
                    for (int i = 0; i < 1820; i++)
                    {
                        gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, i + 1,
                            new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid + i.ToString())), false, null);
                        rawAcl.InsertAce(0, gAce);
                    }
                    isContainer = true;
                    isDS = false;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    if (1820 == discretionaryAcl.Count)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }

            }
        }

        //----------------------------------------------------------------------------------------------
        /*
        *  Class to test DiscretionaryAcl index inherited from abstract base class CommonAcl
        *		public sealed override GenericAce this[int index]
        *
        *
        */
        //----------------------------------------------------------------------------------------------


        private class IndexTestCases
        {
            //No creating objects

            private IndexTestCases() { }

            public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                Console.WriteLine("Running IndexTestCases");

                BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
                AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
            }

            public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                Console.WriteLine("Running BasicValidationTestCases");



                RawAcl rawAcl = null;
                DiscretionaryAcl discretionaryAcl = null;

                GenericAce gAce = null;
                GenericAce verifierGAce = null;
                string owner1 = "BO";
                string owner2 = "BA";
                string owner3 = "BG";
                int index = 0;

                // case 1, only one ACE, get at index 0
                testCasesPerformed++;


                try
                {
                    rawAcl = new RawAcl(1, 1);
                    index = 0;
                    gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);
                    rawAcl.InsertAce(0, gAce);
                    discretionaryAcl = new DiscretionaryAcl(false, false, rawAcl);
                    verifierGAce = discretionaryAcl[index];

                    if (TestIndex(gAce, verifierGAce))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                    {
                        Console.WriteLine("the newly set CommonAce is not equal to what we set");
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 2, two ACEs, index at Count -1
                testCasesPerformed++;


                try
                {
                    rawAcl = new RawAcl(1, 2);
                    //215 has all AceFlags but InheriteOnly
                    gAce = new CommonAce((AceFlags)(FlagsForAce.AuditFlags | FlagsForAce.OI | FlagsForAce.CI | FlagsForAce.NP | FlagsForAce.IH), AceQualifier.AccessAllowed, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);
                    rawAcl.InsertAce(0, gAce);
                    //199 has all AceFlags but InheritedOnly, Inherited
                    gAce = new CommonAce((AceFlags)(FlagsForAce.AuditFlags | FlagsForAce.OI | FlagsForAce.CI | FlagsForAce.NP), AceQualifier.AccessDenied, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner2)), false, null);
                    rawAcl.InsertAce(1, gAce);
                    discretionaryAcl = new DiscretionaryAcl(false, false, rawAcl);
                    gAce = rawAcl[1];
                    //the discretionaryAcl is non-container, all AceFlags except Inherited will be stripped
                    gAce.AceFlags = (AceFlags)FlagsForAce.None;
                    index = discretionaryAcl.Count - 1;

                    verifierGAce = discretionaryAcl[index];

                    if (TestIndex(gAce, verifierGAce))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                    {
                        Console.WriteLine("the newly set CommonAce is not equal to what we set");
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }



                //case 3, only three ACEs, index at Count/2
                testCasesPerformed++;


                try
                {
                    rawAcl = new RawAcl(1, 3);

                    //215 has all AceFlags except InheritOnly					
                    gAce = new CommonAce((AceFlags)(FlagsForAce.AuditFlags | FlagsForAce.OI | FlagsForAce.CI | FlagsForAce.NP | FlagsForAce.IH), AceQualifier.AccessAllowed, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);
                    rawAcl.InsertAce(0, gAce);

                    //199 has all AceFlags except InheritOnly and Inherited				
                    gAce = new CommonAce((AceFlags)(FlagsForAce.AuditFlags | FlagsForAce.OI | FlagsForAce.CI | FlagsForAce.NP), AceQualifier.AccessDenied, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner2)), false, null);
                    rawAcl.InsertAce(1, gAce);
                    gAce = new CommonAce((AceFlags)(FlagsForAce.AuditFlags | FlagsForAce.OI | FlagsForAce.CI | FlagsForAce.NP), AceQualifier.AccessAllowed, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner3)), false, null);
                    rawAcl.InsertAce(2, gAce);
                    discretionaryAcl = new DiscretionaryAcl(false, false, rawAcl);
                    gAce = rawAcl[1];
                    //the systemAcl is non-container, all AceFlags will be stripped
                    gAce.AceFlags = AceFlags.None;
                    index = discretionaryAcl.Count / 2;
                    verifierGAce = discretionaryAcl[index];

                    if (TestIndex(gAce, verifierGAce))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                    {
                        Console.WriteLine("the newly set CommonAce is not equal to what we set");
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }

            }

            public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                RawAcl rawAcl = null;
                DiscretionaryAcl discretionaryAcl = null;
                GenericAce gAce = null;
                GenericAce verifierGAce = null;
                string owner = null;
                int index = 0;

                Console.WriteLine("Running AdditionalTestCases");



                // case 1, no ACE, get index at -1
                testCasesPerformed++;


                try
                {
                    rawAcl = new RawAcl(1, 1);
                    index = -1;
                    discretionaryAcl = new DiscretionaryAcl(false, false, rawAcl);
                    verifierGAce = discretionaryAcl[index];
                    Console.WriteLine("Should not allow get index -1");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (ArgumentOutOfRangeException)
                {
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 2, get index at Count
                testCasesPerformed++;


                try
                {
                    rawAcl = new RawAcl(1, 1);
                    discretionaryAcl = new DiscretionaryAcl(false, false, rawAcl);
                    index = discretionaryAcl.Count;
                    verifierGAce = discretionaryAcl[index];
                    Console.WriteLine("Should not allow get index Count");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (ArgumentOutOfRangeException)
                {
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 3, set index at -1
                testCasesPerformed++;


                try
                {
                    rawAcl = new RawAcl(1, 1);
                    discretionaryAcl = new DiscretionaryAcl(false, false, rawAcl);
                    index = -1;
                    owner = "BG";
                    gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
                    discretionaryAcl[index] = gAce;
                    Console.WriteLine("Should not allow set index as -1");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (NotSupportedException)
                {
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 4, set index at Count
                testCasesPerformed++;


                try
                {
                    rawAcl = new RawAcl(1, 1);
                    discretionaryAcl = new DiscretionaryAcl(true, false, rawAcl);
                    index = discretionaryAcl.Count;
                    owner = "BG";
                    gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
                    discretionaryAcl[index] = gAce;
                    Console.WriteLine("Should not allow set index as Count");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (NotSupportedException)
                {
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 5, set null Ace
                testCasesPerformed++;


                try
                {
                    rawAcl = new RawAcl(1, 1);
                    gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
                    rawAcl.InsertAce(0, gAce);
                    discretionaryAcl = new DiscretionaryAcl(false, false, rawAcl);
                    index = 0;
                    gAce = null;
                    discretionaryAcl[index] = gAce;
                    Console.WriteLine("Should not allow set null ACE");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (NotSupportedException)
                {
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 6, set index at 0
                testCasesPerformed++;


                try
                {
                    rawAcl = new RawAcl(1, 1);
                    owner = "BG";
                    gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
                    rawAcl.InsertAce(0, gAce);

                    discretionaryAcl = new DiscretionaryAcl(false, false, rawAcl);
                    index = 0;
                    owner = "BA";
                    gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
                    discretionaryAcl[index] = gAce;
                    Console.WriteLine("Should throw not supported exception");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (NotSupportedException)
                {
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }

            }

            /*
            * Method Name: TestIndex
            *
            * Description:	check if the two ACEs' content content are equal. Also check they do not reference the same object
            *
            * Parameter:	gAce, verifierGAce -- the SystemAcl objects to be compared
            *
            * Return:		true if test pass, false otherwise
            */

            private static bool TestIndex(GenericAce gAce, GenericAce verifierGAce)
            {
                if (Utils.UtilIsAceEqual(gAce, verifierGAce))
                {//as operator == and != are overridden to by value, can not use != to test these two are not same object any more
                    gAce.AceFlags = AceFlags.InheritanceFlags | AceFlags.Inherited | AceFlags.AuditFlags;
                    if (gAce != verifierGAce)
                        return true;
                    else
                        return false;
                }
                else
                    return false;
            }
        }


        //----------------------------------------------------------------------------------------------
        /*
        *  Class to test DiscretionaryAcl method
        *		 public void AddAccess( AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags )
        *
        *
        */
        //----------------------------------------------------------------------------------------------

        private class AddAccessTestCases
        {
            //No creating objects

            private AddAccessTestCases() { }

            public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                Console.WriteLine("Running AddAccessTestCases");

                BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, false);
                AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
            }

            public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed, bool isBVT)
            {
                Console.WriteLine("Running BasicValidationTestCases");


                int bvtCaseCounter = 0;

                RawAcl rawAcl = null;
                DiscretionaryAcl discretionaryAcl = null;
                bool isContainer = false;
                bool isDS = false;
                int accessControlType = 0;
                string sid = null;
                int accessMask = 1;
                int inheritanceFlags = 0;
                int propagationFlags = 0;
                string initialRawAclStr = null;
                string verifierRawAclStr = null;

                string[] testCase = null;
                ITestCaseReader reader = new InfTestCaseStore();
                reader.OpenTestCaseStore(AddAccessTestCaseStore);

                while (null != (testCase = reader.ReadNextTestCase()))
                {

                    // read isContainer
                    if (1 > testCase.Length)
                        isContainer = false;
                    else
                        isContainer = bool.Parse(testCase[0]);

                    // read isDS
                    if (2 > testCase.Length)
                        isDS = false;
                    else
                        isDS = bool.Parse(testCase[1]);

                    // read accessControlType
                    if (3 > testCase.Length)
                        accessControlType = 0;
                    else
                        accessControlType = int.Parse(testCase[2]);

                    // read securityIdentifier
                    if (4 > testCase.Length)
                        sid = "";
                    else
                        sid = testCase[3];

                    // read accessMask
                    if (5 > testCase.Length)
                        accessMask = 0;
                    else
                        accessMask = int.Parse(testCase[4]);

                    // read inheritanceFlags
                    if (6 > testCase.Length)
                        inheritanceFlags = 0;
                    else
                        inheritanceFlags = int.Parse(testCase[5]);

                    // read propagationFlags
                    if (7 > testCase.Length)
                        propagationFlags = 0;
                    else
                        propagationFlags = int.Parse(testCase[6]);

                    //read initialRawAclStr
                    if (8 > testCase.Length)
                        initialRawAclStr = "";
                    else
                        initialRawAclStr = testCase[7];

                    //read verifierRawAclStr
                    if (9 > testCase.Length)
                        verifierRawAclStr = "";
                    else
                        verifierRawAclStr = testCase[8];

                    //create a discretionaryAcl

                    rawAcl = Utils.UtilCreateRawAclFromString(initialRawAclStr);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    rawAcl = Utils.UtilCreateRawAclFromString(verifierRawAclStr);

                    testCasesPerformed++;


                    try
                    {
                        if (TestAddAccess(discretionaryAcl, rawAcl, (AccessControlType)accessControlType,
                            new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags))
                        {
                            Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                            testCasesPassed++;
                        }
                        else
                            Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception generated: " + e.Message, e);
                    }


                    bvtCaseCounter++;
                    if (isBVT && (bvtCaseCounter == 3))
                    {
                        //first 3 cases are BVT cases
                        break;
                    }
                }
            }

            private static bool TestAddAccess(DiscretionaryAcl discretionaryAcl, RawAcl rawAcl, AccessControlType accessControlType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
            {
                bool result = true;
                byte[] dAclBinaryForm = null;
                byte[] rAclBinaryForm = null;

                discretionaryAcl.AddAccess(accessControlType, sid, accessMask, inheritanceFlags, propagationFlags);


                if (discretionaryAcl.Count == rawAcl.Count &&
                    discretionaryAcl.BinaryLength == rawAcl.BinaryLength)
                {

                    dAclBinaryForm = new byte[discretionaryAcl.BinaryLength];
                    rAclBinaryForm = new byte[rawAcl.BinaryLength];
                    discretionaryAcl.GetBinaryForm(dAclBinaryForm, 0);
                    rawAcl.GetBinaryForm(rAclBinaryForm, 0);
                    if (!Utils.UtilIsBinaryFormEqual(dAclBinaryForm, rAclBinaryForm))
                        result = false;

                    //redundant index check

                    for (int i = 0; i < discretionaryAcl.Count; i++)
                    {
                        if (!Utils.UtilIsAceEqual(discretionaryAcl[i], rawAcl[i]))
                        {
                            Console.WriteLine("*******Bug238842*******");

                            result = false;
                            break;
                        }
                    }
                }
                else
                    result = false;

                return result;
            }

            public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                RawAcl rawAcl = null;
                DiscretionaryAcl discretionaryAcl = null;
                bool isContainer = false;
                bool isDS = false;

                int accessControlType = 0;
                string sid = null;
                int accessMask = 1;
                int inheritanceFlags = 0;
                int propagationFlags = 0;
                GenericAce gAce = null;
                byte[] opaque = null;

                Console.WriteLine("Running AdditionalTestCases");



                //Case 1, non-Container, but InheritanceFlags is not None
                testCasesPerformed++;


                try
                {
                    isContainer = false;
                    isDS = false;
                    rawAcl = new RawAcl(0, 1);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    discretionaryAcl.AddAccess(AccessControlType.Allow,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), 1, InheritanceFlags.ContainerInherit, PropagationFlags.None);
                    Console.WriteLine("Should not allow add ace with InheritanceFlags not None to non-Container DiscretionaryAcl ");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                }
                catch (ArgumentException)
                {//this is expected
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case 2, non-Container, but PropagationFlags is not None
                testCasesPerformed++;


                try
                {
                    isContainer = false;
                    isDS = false;
                    rawAcl = new RawAcl(0, 1);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    discretionaryAcl.AddAccess(AccessControlType.Allow,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), 1, InheritanceFlags.None, PropagationFlags.InheritOnly);
                    Console.WriteLine("Should not allow add ace with PropagationFlags not None to non-Container DiscretionaryAcl ");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                }
                catch (ArgumentException)
                {//this is expected
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }



                //Case 3, Container, InheritanceFlags is None, PropagationFlags is InheritOnly
                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;
                    rawAcl = new RawAcl(0, 1);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    discretionaryAcl.AddAccess(AccessControlType.Allow,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), 1, InheritanceFlags.None, PropagationFlags.InheritOnly);
                    Console.WriteLine("Should not allow add ace with InheritanceFlags None, PropagationFlags InheritOnly to Container DiscretionaryAcl ");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                }
                catch (ArgumentException)
                {//this is expected
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case 4, Container, InheritanceFlags is None, PropagationFlags is NoPropagateInherit
                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;
                    rawAcl = new RawAcl(0, 1);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    discretionaryAcl.AddAccess(AccessControlType.Allow,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), 1, InheritanceFlags.None, PropagationFlags.NoPropagateInherit);
                    Console.WriteLine("Should not allow add ace with InheritanceFlags None, PropagationFlags NoPropagateInherit to Container DiscretionaryAcl ");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                }
                catch (ArgumentException)
                {//this is expected
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }



                //Case 5, Container, InheritanceFlags is None, PropagationFlags is NoPropagateInherit | InheritOnly
                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;
                    rawAcl = new RawAcl(0, 1);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    discretionaryAcl.AddAccess(AccessControlType.Allow,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), 1, InheritanceFlags.None, PropagationFlags.NoPropagateInherit | PropagationFlags.InheritOnly);
                    Console.WriteLine("Should not allow add ace with InheritanceFlags None, PropagationFlags NoPropagateInherit | InheritOnly to Container DiscretionaryAcl ");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                }
                catch (ArgumentException)
                {//this is expected
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case 6, accessMask = 0
                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;
                    accessControlType = 1;
                    sid = "BA";
                    accessMask = 0;
                    inheritanceFlags = 3;
                    propagationFlags = 3;
                    rawAcl = new RawAcl(0, 1);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    discretionaryAcl.AddAccess((AccessControlType)accessControlType,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);

                    Console.WriteLine("Should not allow add ace with 0 accessmask ");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                }
                catch (ArgumentException)
                {
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case 7, null sid
                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;
                    accessControlType = 1;
                    accessMask = 1;
                    inheritanceFlags = 3;
                    propagationFlags = 3;
                    rawAcl = new RawAcl(0, 1);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    discretionaryAcl.AddAccess((AccessControlType)accessControlType, null, accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);

                    Console.WriteLine("Should not allow add ace with null ");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                }
                catch (ArgumentNullException)
                {
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case 8, add one Access ACE to the DiscretionaryAcl with no ACE
                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;
                    accessControlType = 0;
                    sid = "BA";
                    accessMask = 1;
                    inheritanceFlags = 3;
                    propagationFlags = 3;
                    rawAcl = new RawAcl(0, 1);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    //15 = AceFlags.ObjectInherit |AceFlags.ContainerInherit | AceFlags.NoPropagateInherit | AceFlags.InheritOnly
                    gAce = new CommonAce((AceFlags)15, AceQualifier.AccessAllowed, accessMask,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
                    rawAcl.InsertAce(rawAcl.Count, gAce);
                    if (TestAddAccess(discretionaryAcl, rawAcl, (AccessControlType)accessControlType,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case 9, Container, InheritOnly ON, but ContainerInherit and ObjectInherit are both OFF
                //add meaningless Access ACE to the DiscretionaryAcl with no ACE, ace should not
                //be added. There are mutiple type of meaningless Ace, but as both AddAccess and Constructor3
                //call the same method to check the meaninglessness, only some sanitory cases are enough.
                //bug# 288116


                {

                    testCasesPerformed++;


                    try
                    {
                        isContainer = true;
                        isDS = false;

                        inheritanceFlags = 0;//InheritanceFlags.None
                        propagationFlags = 2; //PropagationFlags.InheritOnly

                        accessControlType = 0;
                        sid = "BA";
                        accessMask = 1;

                        rawAcl = new RawAcl(0, 1);
                        discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                        try
                        {
                            TestAddAccess(discretionaryAcl, rawAcl, (AccessControlType)accessControlType,
                                new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);

                            Console.WriteLine("Should not allow add container object InheritOnly ON, but ContainerInherit and ObjectInherit are both OFF ace");
                            Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                        }
                        catch (ArgumentException)
                        {
                            Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                            testCasesPassed++;
                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception generated: " + e.Message, e);
                    }


                }

                //Case 10, add Ace of NOT(AccessControlType.Allow |AccessControlType.Denied) to the DiscretionaryAcl with no ACE, 
                // should throw appropriate exception for wrong parameter, bug#287188

                {

                    testCasesPerformed++;


                    try
                    {
                        isContainer = true;
                        isDS = false;

                        inheritanceFlags = 1;//InheritanceFlags.ContainerInherit
                        propagationFlags = 2; //PropagationFlags.InheritOnly

                        accessControlType = 100;
                        sid = "BA";
                        accessMask = 1;

                        rawAcl = new RawAcl(0, 1);
                        discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                        try
                        {
                            discretionaryAcl.AddAccess((AccessControlType)accessControlType,
                                new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)),
                                accessMask,
                                (InheritanceFlags)inheritanceFlags,
                                (PropagationFlags)propagationFlags);
                            Console.WriteLine("Should not allow add NOT(AccessControlType.Allow |AccessControlType.Allow) ace ");
                            Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                            testCasesPassed++;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception generated: " + e.Message, e);
                    }


                }

                //Case 11, all the ACEs in the Dacl are non-qualified ACE, no merge

                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;

                    inheritanceFlags = 1;//InheritanceFlags.ContainerInherit
                    propagationFlags = 2; //PropagationFlags.InheritOnly

                    accessControlType = 0;
                    sid = "BA";
                    accessMask = 1;

                    rawAcl = new RawAcl(0, 1);
                    opaque = new byte[4];
                    gAce = new CustomAce(AceType.MaxDefinedAceType + 1, AceFlags.InheritanceFlags, opaque); ;
                    rawAcl.InsertAce(0, gAce);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    gAce = new CommonAce(AceFlags.ContainerInherit | AceFlags.InheritOnly,
                                            AceQualifier.AccessAllowed,
                                            accessMask,
                                            new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)),
                                            false,
                                            null);
                    rawAcl.InsertAce(0, gAce);

                    //After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
                    //forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
                    try
                    {

                        TestAddAccess(discretionaryAcl, rawAcl, (AccessControlType)accessControlType,
                            new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);

                        Console.WriteLine("Should not allow Add Ace to ACL with customAce/CompoundAce");
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case 12, add Ace to exceed binary length boundary, throw exception
                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;

                    inheritanceFlags = 1;//InheritanceFlags.ContainerInherit
                    propagationFlags = 2; //PropagationFlags.InheritOnly

                    accessControlType = 0;
                    sid = "BA";
                    accessMask = 1;

                    rawAcl = new RawAcl(0, 1);
                    opaque = new byte[GenericAcl.MaxBinaryLength + 1 - 8 - 4 - 16];
                    gAce = new CustomAce(AceType.MaxDefinedAceType + 1,
                        AceFlags.InheritanceFlags, opaque); ;
                    rawAcl.InsertAce(0, gAce);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);


                    //After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
                    //forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
                    try
                    {

                        discretionaryAcl.AddAccess((AccessControlType)accessControlType,
                            new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);

                        Console.WriteLine("Should not allow Add Ace to ACL with customAce/CompoundAce");
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


            }
        }


        //----------------------------------------------------------------------------------------------
        /*
        *  Class to test DiscretionaryAcl method
        *		 public void SetAccess( AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags )
        *
        *
        */
        //----------------------------------------------------------------------------------------------

        private class SetAccessTestCases
        {
            //No creating objects

            private SetAccessTestCases() { }

            public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                Console.WriteLine("Running SetAccessTestCases");

                BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, false);
                AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
            }

            public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed, bool isBVT)
            {

                Console.WriteLine("Running BasicValidationTestCases");


                int bvtCaseCounter = 0;

                RawAcl rawAcl = null;
                DiscretionaryAcl discretionaryAcl = null;
                bool isContainer = false;
                bool isDS = false;
                int accessControlType = 0;
                string sid = null;
                int accessMask = 1;
                int inheritanceFlags = 0;
                int propagationFlags = 0;
                string initialRawAclStr = null;
                string verifierRawAclStr = null;

                string[] testCase = null;
                ITestCaseReader reader = new InfTestCaseStore();
                reader.OpenTestCaseStore(SetAccessTestCaseStore);

                while (null != (testCase = reader.ReadNextTestCase()))
                {
                    // read isContainer
                    if (1 > testCase.Length)
                        isContainer = false;
                    else
                        isContainer = bool.Parse(testCase[0]);

                    // read isDS
                    if (2 > testCase.Length)
                        isDS = false;
                    else
                        isDS = bool.Parse(testCase[1]);

                    // read accessControlType
                    if (3 > testCase.Length)
                        accessControlType = 0;
                    else
                        accessControlType = int.Parse(testCase[2]);

                    // read securityIdentifier
                    if (4 > testCase.Length)
                        sid = "";
                    else
                        sid = testCase[3];

                    // read accessMask
                    if (5 > testCase.Length)
                        accessMask = 0;
                    else
                        accessMask = int.Parse(testCase[4]);

                    // read inheritanceFlags
                    if (6 > testCase.Length)
                        inheritanceFlags = 0;
                    else
                        inheritanceFlags = int.Parse(testCase[5]);

                    // read propagationFlags
                    if (7 > testCase.Length)
                        propagationFlags = 0;
                    else
                        propagationFlags = int.Parse(testCase[6]);

                    //read initialRawAclStr
                    if (8 > testCase.Length)
                        initialRawAclStr = "";
                    else
                        initialRawAclStr = testCase[7];

                    //read verifierRawAclStr
                    if (9 > testCase.Length)
                        verifierRawAclStr = "";
                    else
                        verifierRawAclStr = testCase[8];

                    //create a discretionaryAcl

                    rawAcl = Utils.UtilCreateRawAclFromString(initialRawAclStr);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    rawAcl = Utils.UtilCreateRawAclFromString(verifierRawAclStr);

                    testCasesPerformed++;


                    try
                    {
                        if (TestSetAccess(discretionaryAcl, rawAcl, (AccessControlType)accessControlType,
                            new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags))
                        {
                            Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                            testCasesPassed++;
                        }
                        else
                            Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception generated: " + e.Message, e);
                    }


                    bvtCaseCounter++;
                    if (isBVT && (bvtCaseCounter == 3))
                    {
                        //first 3 cases are BVT cases
                        break;
                    }
                }
            }

            private static bool TestSetAccess(DiscretionaryAcl discretionaryAcl, RawAcl rawAcl, AccessControlType accessControlType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
            {
                bool result = true;

                byte[] dAclBinaryForm = null;
                byte[] rAclBinaryForm = null;

                discretionaryAcl.SetAccess(accessControlType, sid, accessMask, inheritanceFlags, propagationFlags);
                if (discretionaryAcl.Count == rawAcl.Count &&
                    discretionaryAcl.BinaryLength == rawAcl.BinaryLength)
                {
                    dAclBinaryForm = new byte[discretionaryAcl.BinaryLength];
                    rAclBinaryForm = new byte[rawAcl.BinaryLength];
                    discretionaryAcl.GetBinaryForm(dAclBinaryForm, 0);
                    rawAcl.GetBinaryForm(rAclBinaryForm, 0);
                    if (!Utils.UtilIsBinaryFormEqual(dAclBinaryForm, rAclBinaryForm))
                        result = false;

                    //redundant index check

                    for (int i = 0; i < discretionaryAcl.Count; i++)
                    {
                        if (!Utils.UtilIsAceEqual(discretionaryAcl[i], rawAcl[i]))
                        {
                            result = false;
                            break;
                        }
                    }
                }
                else
                    result = false;

                return result;
            }

            public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                RawAcl rawAcl = null;
                DiscretionaryAcl discretionaryAcl = null;
                bool isContainer = false;
                bool isDS = false;

                int accessControlType = 0;
                string sid = null;
                int accessMask = 1;
                int inheritanceFlags = 0;
                int propagationFlags = 0;
                GenericAce gAce = null;
                byte[] opaque = null;

                Console.WriteLine("Running AdditionalTestCases");




                //Case 1, non-Container, but InheritanceFlags is not None
                testCasesPerformed++;


                try
                {
                    isContainer = false;
                    isDS = false;
                    rawAcl = new RawAcl(0, 1);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    discretionaryAcl.SetAccess(AccessControlType.Allow,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), 1, InheritanceFlags.ContainerInherit, PropagationFlags.None);
                    Console.WriteLine("Should not allow set access with InheritanceFlags not None to non-Container DiscretionaryAcl ");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                }
                catch (ArgumentException)
                {//this is expected
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case 2, non-Container, but PropagationFlags is not None
                testCasesPerformed++;


                try
                {
                    isContainer = false;
                    isDS = false;
                    rawAcl = new RawAcl(0, 1);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    discretionaryAcl.SetAccess(AccessControlType.Allow,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), 1, InheritanceFlags.None, PropagationFlags.InheritOnly);
                    Console.WriteLine("Should not allow set audit with PropagationFlags not None to non-Container DiscretionaryAcl ");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                }
                catch (ArgumentException)
                {//this is expected
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case 3, set one allowed ACE to the DiscretionaryAcl with no ACE
                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;
                    accessControlType = 1;
                    sid = "BA";
                    accessMask = 1;
                    inheritanceFlags = 3;
                    propagationFlags = 3;
                    rawAcl = new RawAcl(0, 1);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    //15 = AceFlags.ObjectInherit |AceFlags.ContainerInherit | AceFlags.NoPropagateInherit | AceFlags.InheritOnly
                    gAce = new CommonAce((AceFlags)15, AceQualifier.AccessDenied, accessMask,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
                    rawAcl.InsertAce(rawAcl.Count, gAce);
                    if (TestSetAccess(discretionaryAcl, rawAcl, (AccessControlType)accessControlType,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case 4, accessMask = 0
                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;
                    accessControlType = 1;
                    sid = "BA";
                    accessMask = 0;
                    inheritanceFlags = 3;
                    propagationFlags = 3;
                    rawAcl = new RawAcl(0, 1);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    discretionaryAcl.SetAccess((AccessControlType)accessControlType,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);

                    Console.WriteLine("Should not allow set access with 0 accessmask ");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                }
                catch (ArgumentException)
                {
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }



                //Case 5, null sid
                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;
                    accessControlType = 1;
                    accessMask = 1;
                    inheritanceFlags = 3;
                    propagationFlags = 3;
                    rawAcl = new RawAcl(0, 1);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    discretionaryAcl.SetAccess((AccessControlType)accessControlType, null, accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);

                    Console.WriteLine("Should not allow set access with null ");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                }
                catch (ArgumentNullException)
                {
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case6, all the ACEs in the Dacl are non-qualified ACE, no replacement

                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;

                    inheritanceFlags = 1;//InheritanceFlags.ContainerInherit
                    propagationFlags = 2; //PropagationFlags.InheritOnly

                    accessControlType = 0;
                    sid = "BA";
                    accessMask = 1;

                    rawAcl = new RawAcl(0, 1);
                    opaque = new byte[4];
                    gAce = new CustomAce(AceType.MaxDefinedAceType + 1, AceFlags.InheritanceFlags, opaque); ;
                    rawAcl.InsertAce(0, gAce);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    gAce = new CommonAce(AceFlags.ContainerInherit | AceFlags.InheritOnly,
                                            AceQualifier.AccessAllowed,
                                            accessMask,
                                            new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)),
                                            false,
                                            null);
                    rawAcl.InsertAce(0, gAce);

                    //After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
                    //forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
                    try
                    {

                        TestSetAccess(discretionaryAcl, rawAcl, (AccessControlType)accessControlType,
                            new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);

                        Console.WriteLine("Should not allow Set Ace to ACL with customAce/CompoundAce");
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case 7, set without replacement, exceed binary length boundary, throw exception

                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;

                    inheritanceFlags = 1;//InheritanceFlags.ContainerInherit
                    propagationFlags = 2; //PropagationFlags.InheritOnly

                    accessControlType = 0;
                    sid = "BA";
                    accessMask = 1;

                    rawAcl = new RawAcl(0, 1);
                    opaque = new byte[GenericAcl.MaxBinaryLength + 1 - 8 - 4 - 16];
                    gAce = new CustomAce(AceType.MaxDefinedAceType + 1,
                        AceFlags.InheritanceFlags, opaque); ;
                    rawAcl.InsertAce(0, gAce);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    gAce = new CommonAce(AceFlags.ContainerInherit | AceFlags.InheritOnly,
                                            AceQualifier.AccessAllowed,
                                            accessMask,
                                            new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)),
                                            false,
                                            null);

                    //After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
                    //forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
                    try
                    {

                        discretionaryAcl.SetAccess((AccessControlType)accessControlType,
                            new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);
                        Console.WriteLine("Should not allow Set Ace to ACL with customAce/CompoundAce");
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case 8, set without replacement, not exceed binary length boundary

                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;

                    inheritanceFlags = 1;//InheritanceFlags.ContainerInherit
                    propagationFlags = 2; //PropagationFlags.InheritOnly

                    accessControlType = 0;
                    sid = "BA";
                    accessMask = 1;

                    rawAcl = new RawAcl(0, 1);
                    opaque = new byte[GenericAcl.MaxBinaryLength + 1 - 8 - 4 - 28];
                    gAce = new CustomAce(AceType.MaxDefinedAceType + 1,
                        AceFlags.InheritanceFlags, opaque); ;
                    rawAcl.InsertAce(0, gAce);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    gAce = new CommonAce(AceFlags.ContainerInherit | AceFlags.InheritOnly,
                                            AceQualifier.AccessAllowed,
                                            accessMask + 1,
                                            new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)),
                                            false,
                                            null);
                    rawAcl.InsertAce(0, gAce);

                    //After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
                    //forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
                    try
                    {

                        TestSetAccess(discretionaryAcl, rawAcl, (AccessControlType)accessControlType,
                            new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask + 1, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);
                        Console.WriteLine("Should not allow Set Ace to ACL with customAce/CompoundAce");
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }



                //Case 9, set Ace of NOT(AccessControlType.Allow |AccessControlType.Denied) to the DiscretionaryAcl with no ACE, 
                // should throw appropriate exception for wrong parameter, bug#287188

                {

                    testCasesPerformed++;


                    try
                    {
                        isContainer = true;
                        isDS = false;

                        inheritanceFlags = 1;//InheritanceFlags.ContainerInherit
                        propagationFlags = 2; //PropagationFlags.InheritOnly

                        accessControlType = 100;
                        sid = "BA";
                        accessMask = 1;

                        rawAcl = new RawAcl(0, 1);
                        discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                        try
                        {
                            discretionaryAcl.SetAccess((AccessControlType)accessControlType,
                                new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)),
                                accessMask,
                                (InheritanceFlags)inheritanceFlags,
                                (PropagationFlags)propagationFlags);

                            Console.WriteLine("Should not allow set NOT(AccessControlType.Allow |AccessControlType.Allow) access ");
                            Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                            testCasesPassed++;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception generated: " + e.Message, e);
                    }


                }

            }
        }

        //----------------------------------------------------------------------------------------------
        /*
        *  Class to test DiscretionaryAcl method
        *		 public bool RemoveAccess( AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags )
        *
        *
        */
        //----------------------------------------------------------------------------------------------

        private class RemoveAccessTestCases
        {
            //No creating objects

            private RemoveAccessTestCases() { }

            public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                Console.WriteLine("Running RemoveAccessTestCases");

                BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, false);
                AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
            }

            public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed, bool isBVT)
            {

                Console.WriteLine("Running BasicValidationTestCases");


                int bvtCaseCounter = 0;

                RawAcl rawAcl = null;
                DiscretionaryAcl discretionaryAcl = null;
                bool isContainer = false;
                bool isDS = false;
                int accessControlType = 0;
                string sid = null;
                int accessMask = 1;
                int inheritanceFlags = 0;
                int propagationFlags = 0;
                string initialRawAclStr = null;
                string verifierRawAclStr = null;
                bool removePossible = false;

                string[] testCase = null;
                ITestCaseReader reader = new InfTestCaseStore();
                reader.OpenTestCaseStore(RemoveAccessTestCaseStore);

                while (null != (testCase = reader.ReadNextTestCase()))
                {
                    // read isContainer
                    if (1 > testCase.Length)
                        isContainer = false;
                    else
                        isContainer = bool.Parse(testCase[0]);

                    // read isDS
                    if (2 > testCase.Length)
                        isDS = false;
                    else
                        isDS = bool.Parse(testCase[1]);

                    // read accessControlType
                    if (3 > testCase.Length)
                        accessControlType = 0;
                    else
                        accessControlType = int.Parse(testCase[2]);

                    // read securityIdentifier
                    if (4 > testCase.Length)
                        sid = "";
                    else
                        sid = testCase[3];

                    // read accessMask
                    if (5 > testCase.Length)
                        accessMask = 0;
                    else
                        accessMask = int.Parse(testCase[4]);

                    // read inheritanceFlags
                    if (6 > testCase.Length)
                        inheritanceFlags = 0;
                    else
                        inheritanceFlags = int.Parse(testCase[5]);

                    // read propagationFlags
                    if (7 > testCase.Length)
                        propagationFlags = 0;
                    else
                        propagationFlags = int.Parse(testCase[6]);

                    //read initialRawAclStr
                    if (8 > testCase.Length)
                        initialRawAclStr = "";
                    else
                        initialRawAclStr = testCase[7];

                    //read verifierRawAclStr
                    if (9 > testCase.Length)
                        verifierRawAclStr = "";
                    else
                        verifierRawAclStr = testCase[8];

                    //read removePossible
                    if (10 > testCase.Length)
                        removePossible = false;
                    else
                        removePossible = bool.Parse(testCase[9]);

                    //create a discretionaryAcl

                    rawAcl = Utils.UtilCreateRawAclFromString(initialRawAclStr);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    rawAcl = Utils.UtilCreateRawAclFromString(verifierRawAclStr);

                    testCasesPerformed++;


                    try
                    {
                        if (TestRemoveAccess(discretionaryAcl, rawAcl, (AccessControlType)accessControlType,
                            new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags, removePossible))
                        {
                            Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                            testCasesPassed++;
                        }
                        else
                            Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception generated: " + e.Message, e);
                    }


                    bvtCaseCounter++;
                    if (isBVT && (bvtCaseCounter == 3))
                    {
                        //first 3 cases are BVT cases
                        break;
                    }
                }
            }

            private static bool TestRemoveAccess(DiscretionaryAcl discretionaryAcl, RawAcl rawAcl, AccessControlType accessControlType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, bool removePossible)
            {
                bool result = true;
                bool isRemoved = false;
                byte[] dAclBinaryForm = null;
                byte[] rAclBinaryForm = null;


                isRemoved = discretionaryAcl.RemoveAccess(accessControlType, sid, accessMask, inheritanceFlags, propagationFlags);
                if ((isRemoved == removePossible) &&
                    (discretionaryAcl.Count == rawAcl.Count) &&
                    discretionaryAcl.BinaryLength == rawAcl.BinaryLength)
                {
                    dAclBinaryForm = new byte[discretionaryAcl.BinaryLength];
                    rAclBinaryForm = new byte[rawAcl.BinaryLength];
                    discretionaryAcl.GetBinaryForm(dAclBinaryForm, 0);
                    rawAcl.GetBinaryForm(rAclBinaryForm, 0);
                    if (!Utils.UtilIsBinaryFormEqual(dAclBinaryForm, rAclBinaryForm))
                        result = false;

                    //redundant index check					
                    for (int i = 0; i < discretionaryAcl.Count; i++)
                    {
                        if (!Utils.UtilIsAceEqual(discretionaryAcl[i], rawAcl[i]))
                        {
                            result = false;
                            break;
                        }
                    }
                }
                else
                    result = false;

                return result;
            }

            public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                RawAcl rawAcl = null;
                DiscretionaryAcl discretionaryAcl = null;
                bool isContainer = false;
                bool isDS = false;

                int accessControlType = 0;
                string sid = null;
                int accessMask = 1;
                int inheritanceFlags = 0;
                int propagationFlags = 0;
                GenericAce gAce = null;
                bool removePossible = false;
                byte[] opaque = null;

                Console.WriteLine("Running AdditionalTestCases");




                //Case 1, remove one ACE from the DiscretionaryAcl with no ACE
                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;
                    accessControlType = 1;
                    sid = "BA";
                    accessMask = 1;
                    inheritanceFlags = 3;
                    propagationFlags = 3;
                    removePossible = true;
                    rawAcl = new RawAcl(0, 1);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    if (TestRemoveAccess(discretionaryAcl, rawAcl, (AccessControlType)accessControlType,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags, removePossible))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case 2, remove the last ACE from the DiscretionaryAcl
                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;
                    accessControlType = 1;
                    sid = "BA";
                    accessMask = 1;
                    inheritanceFlags = 3;
                    propagationFlags = 3;
                    removePossible = true;
                    rawAcl = new RawAcl(0, 1);
                    //15 = AceFlags.ObjectInherit |AceFlags.ContainerInherit | AceFlags.NoPropagateInherit | AceFlags.InheritOnly
                    gAce = new CommonAce((AceFlags)15, AceQualifier.AccessDenied, accessMask,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
                    rawAcl.InsertAce(rawAcl.Count, gAce);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    //remove the ace to create the validation rawAcl
                    rawAcl.RemoveAce(rawAcl.Count - 1);
                    if (TestRemoveAccess(discretionaryAcl, rawAcl, (AccessControlType)accessControlType,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags, removePossible))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case 3, accessMask = 0
                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;
                    accessControlType = 1;
                    sid = "BA";
                    accessMask = 0;
                    inheritanceFlags = 3;
                    propagationFlags = 3;
                    rawAcl = new RawAcl(0, 1);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    discretionaryAcl.RemoveAccess((AccessControlType)accessControlType,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);

                    Console.WriteLine("Should not allow remove access with 0 accessmask");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                }
                catch (ArgumentException)
                {
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case 4, null sid
                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;
                    accessControlType = 1;
                    accessMask = 1;
                    inheritanceFlags = 3;
                    propagationFlags = 3;
                    rawAcl = new RawAcl(0, 1);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    discretionaryAcl.RemoveAccess((AccessControlType)accessControlType, null, accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);

                    Console.WriteLine("Should not allow remove ace with null sid");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (ArgumentNullException)
                {
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case 5, all the ACEs in the Dacl are non-qualified ACE, no remove

                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;

                    inheritanceFlags = 1;//InheritanceFlags.ContainerInherit
                    propagationFlags = 2; //PropagationFlags.InheritOnly

                    accessControlType = 0;
                    sid = "BA";
                    accessMask = 1;
                    removePossible = true;

                    rawAcl = new RawAcl(0, 1);
                    opaque = new byte[4];
                    gAce = new CustomAce(AceType.MaxDefinedAceType + 1, AceFlags.InheritanceFlags, opaque); ;
                    rawAcl.InsertAce(0, gAce);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    //After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
                    //forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
                    try
                    {

                        TestRemoveAccess(discretionaryAcl, rawAcl,
                            (AccessControlType)accessControlType,
                            new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)),
                            accessMask,
                            (InheritanceFlags)inheritanceFlags,
                            (PropagationFlags)propagationFlags, removePossible);
                        Console.WriteLine("Should not allow RemoveAccess Ace to ACL with customAce/CompoundAce");
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }



                //Case 6, remove split cause overflow
                // This test is no longer relevant in CoreCLR
                // (Non-canonical ACLs cannot be modified)
                /*
				{
					
				testCasesPerformed ++;


				try
				{
					isContainer = true;
					isDS = false;

					inheritanceFlags = 2;//InheritanceFlags.ObjectInherit
					propagationFlags = 3; //PropagationFlags.NoPropagateInherit |  PropagationFlags.InheritOnly
					accessControlType = 0;
					accessMask = 1;
					sid = "BG";
					opaque = new byte[GenericAcl.MaxBinaryLength + 1 - 8 - 4 - 72];

					
					rawAcl = new RawAcl(0, 1);
					//binary length of this ACE is 36, binary length of sid "LA" is 28
					gAce = new CommonAce ( AceFlags.ObjectInherit | AceFlags.ContainerInherit | AceFlags.NoPropagateInherit,
											AceQualifier.AccessAllowed,
											3,
											new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)),
											false,
											null );
					rawAcl.InsertAce(0, gAce);
                    gAce = new CustomAce(AceType.MaxDefinedAceType + 1, AceFlags.None, opaque);
                    rawAcl.InsertAce(0, gAce);

					discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
					try
					{
						discretionaryAcl.RemoveAccess(AccessControlType.Allow, 
							new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), 
							1, 
							InheritanceFlags.ObjectInherit, 
							PropagationFlags.NoPropagateInherit |  PropagationFlags.InheritOnly);

						Console.WriteLine("Should not allow remove access to exceed binary length boundary ");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
					}
					catch(OverflowException)
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;							
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				}*/



                //Case 7, Remove Ace of NOT(AccessControlType.Allow |AccessControlType.Denied) to the DiscretionaryAcl with no ACE, 
                // should throw appropriate exception for wrong parameter, bug#287188

                {

                    testCasesPerformed++;


                    try
                    {
                        isContainer = true;
                        isDS = false;

                        inheritanceFlags = 1;//InheritanceFlags.ContainerInherit
                        propagationFlags = 2; //PropagationFlags.InheritOnly

                        accessControlType = 100;
                        sid = "BA";
                        accessMask = 1;

                        rawAcl = new RawAcl(0, 1);
                        discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                        try
                        {
                            discretionaryAcl.RemoveAccess((AccessControlType)accessControlType,
                                new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)),
                                accessMask,
                                (InheritanceFlags)inheritanceFlags,
                                (PropagationFlags)propagationFlags);

                            Console.WriteLine("Should not allow remove NOT(AccessControlType.Allow |AccessControlType.Allow) access ");
                            Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                            testCasesPassed++;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception generated: " + e.Message, e);
                    }

                }

            }
        }

        //----------------------------------------------------------------------------------------------
        /*
        *  Class to test DiscretionaryAcl method
        *		 public void RemoveAccessSpecific( AccessControlType accessType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags )
        *
        *
        */
        //----------------------------------------------------------------------------------------------

        private class RemoveAccessSpecificTestCases
        {
            //No creating objects

            private RemoveAccessSpecificTestCases() { }

            public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                Console.WriteLine("Running RemoveAccessSpecificTestCases");

                BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, false);
                AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
            }

            public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed, bool isBVT)
            {
                Console.WriteLine("Running BasicValidationTestCases");


                int bvtCaseCounter = 0;


                RawAcl rawAcl = null;
                DiscretionaryAcl discretionaryAcl = null;
                bool isContainer = false;
                bool isDS = false;
                int accessControlType = 0;
                string sid = null;
                int accessMask = 1;
                int inheritanceFlags = 0;
                int propagationFlags = 0;
                string initialRawAclStr = null;
                string verifierRawAclStr = null;

                string[] testCase = null;
                ITestCaseReader reader = new InfTestCaseStore();
                reader.OpenTestCaseStore(RemoveAccessSpecificTestCaseStore);

                while (null != (testCase = reader.ReadNextTestCase()))
                {
                    // read isContainer
                    if (1 > testCase.Length)
                        isContainer = false;
                    else
                        isContainer = bool.Parse(testCase[0]);

                    // read isDS
                    if (2 > testCase.Length)
                        isDS = false;
                    else
                        isDS = bool.Parse(testCase[1]);

                    // read accessControlType
                    if (3 > testCase.Length)
                        accessControlType = 0;
                    else
                        accessControlType = int.Parse(testCase[2]);

                    // read securityIdentifier
                    if (4 > testCase.Length)
                        sid = "";
                    else
                        sid = testCase[3];

                    // read accessMask
                    if (5 > testCase.Length)
                        accessMask = 0;
                    else
                        accessMask = int.Parse(testCase[4]);

                    // read inheritanceFlags
                    if (6 > testCase.Length)
                        inheritanceFlags = 0;
                    else
                        inheritanceFlags = int.Parse(testCase[5]);

                    // read propagationFlags
                    if (7 > testCase.Length)
                        propagationFlags = 0;
                    else
                        propagationFlags = int.Parse(testCase[6]);

                    //read initialRawAclStr
                    if (8 > testCase.Length)
                        initialRawAclStr = "";
                    else
                        initialRawAclStr = testCase[7];

                    //read verifierRawAclStr
                    if (9 > testCase.Length)
                        verifierRawAclStr = "";
                    else
                        verifierRawAclStr = testCase[8];

                    //create a discretionaryAcl

                    rawAcl = Utils.UtilCreateRawAclFromString(initialRawAclStr);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    rawAcl = Utils.UtilCreateRawAclFromString(verifierRawAclStr);

                    testCasesPerformed++;


                    try
                    {
                        if (TestRemoveAccessSpecific(discretionaryAcl, rawAcl, (AccessControlType)accessControlType,
                            new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags))
                        {
                            Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                            testCasesPassed++;
                        }
                        else
                            Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception generated: " + e.Message, e);
                    }


                    bvtCaseCounter++;
                    if (isBVT && (bvtCaseCounter == 3))
                    {
                        //first 3 cases are BVT cases
                        break;
                    }
                }
            }

            private static bool TestRemoveAccessSpecific(DiscretionaryAcl discretionaryAcl, RawAcl rawAcl, AccessControlType accessControlType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
            {
                bool result = true;

                byte[] dAclBinaryForm = null;
                byte[] rAclBinaryForm = null;

                discretionaryAcl.RemoveAccessSpecific(accessControlType, sid, accessMask, inheritanceFlags, propagationFlags);
                if (discretionaryAcl.Count == rawAcl.Count &&
                    discretionaryAcl.BinaryLength == rawAcl.BinaryLength)
                {
                    dAclBinaryForm = new byte[discretionaryAcl.BinaryLength];
                    rAclBinaryForm = new byte[rawAcl.BinaryLength];
                    discretionaryAcl.GetBinaryForm(dAclBinaryForm, 0);
                    rawAcl.GetBinaryForm(rAclBinaryForm, 0);
                    if (!Utils.UtilIsBinaryFormEqual(dAclBinaryForm, rAclBinaryForm))
                        result = false;

                    //redundant index check					
                    for (int i = 0; i < discretionaryAcl.Count; i++)
                    {
                        if (!Utils.UtilIsAceEqual(discretionaryAcl[i], rawAcl[i]))
                        {
                            result = false;
                            break;
                        }
                    }
                }
                else
                    result = false;

                return result;
            }

            public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                RawAcl rawAcl = null;
                DiscretionaryAcl discretionaryAcl = null;
                bool isContainer = false;
                bool isDS = false;

                int accessControlType = 0;
                string sid = null;
                int accessMask = 1;
                int inheritanceFlags = 0;
                int propagationFlags = 0;
                GenericAce gAce = null;
                byte[] opaque = null;

                Console.WriteLine("Running AdditionalTestCases");




                //Case 1, remove one ACE from the DiscretionaryAcl with no ACE
                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;
                    accessControlType = 1;
                    sid = "BA";
                    accessMask = 1;
                    inheritanceFlags = 3;
                    propagationFlags = 3;
                    rawAcl = new RawAcl(0, 1);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    if (TestRemoveAccessSpecific(discretionaryAcl, rawAcl, (AccessControlType)accessControlType,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case 2, remove the last one ACE from the DiscretionaryAcl
                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;
                    accessControlType = 0;
                    sid = "BA";
                    accessMask = 1;
                    inheritanceFlags = 3;
                    propagationFlags = 3;
                    rawAcl = new RawAcl(0, 1);
                    //15 = AceFlags.ObjectInherit |AceFlags.ContainerInherit | AceFlags.NoPropagateInherit | AceFlags.InheritOnly
                    gAce = new CommonAce((AceFlags)15, AceQualifier.AccessAllowed, accessMask,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
                    rawAcl.InsertAce(rawAcl.Count, gAce);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    //remove the ace to create the validation rawAcl
                    rawAcl.RemoveAce(rawAcl.Count - 1);
                    if (TestRemoveAccessSpecific(discretionaryAcl, rawAcl, (AccessControlType)accessControlType,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case 3, accessMask = 0
                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;
                    accessControlType = 1;
                    sid = "BA";
                    accessMask = 0;
                    inheritanceFlags = 3;
                    propagationFlags = 3;
                    rawAcl = new RawAcl(0, 1);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    discretionaryAcl.RemoveAccessSpecific((AccessControlType)accessControlType,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);

                    Console.WriteLine("Should not allow remove ace specific with 0 accessmask");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                }
                catch (ArgumentException)
                {
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case 4, null sid
                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;
                    accessControlType = 1;
                    accessMask = 1;
                    sid = "BA";
                    inheritanceFlags = 3;
                    propagationFlags = 3;
                    rawAcl = new RawAcl(0, 1);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    discretionaryAcl.RemoveAccessSpecific((AccessControlType)accessControlType, null, accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);

                    Console.WriteLine("Should not allow remove ace specific with null");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                }
                catch (ArgumentNullException)
                {
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case 5, all the ACEs in the Dacl are non-qualified ACE, no remove

                testCasesPerformed++;


                try
                {
                    isContainer = true;
                    isDS = false;

                    inheritanceFlags = 1;//InheritanceFlags.ContainerInherit
                    propagationFlags = 2; //PropagationFlags.InheritOnly

                    accessControlType = 0;
                    sid = "BA";
                    accessMask = 1;

                    rawAcl = new RawAcl(0, 1);
                    opaque = new byte[4];
                    gAce = new CustomAce(AceType.MaxDefinedAceType + 1, AceFlags.InheritanceFlags, opaque); ;
                    rawAcl.InsertAce(0, gAce);
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    //After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
                    //forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
                    try
                    {

                        TestRemoveAccessSpecific
                            (discretionaryAcl, rawAcl,
                            (AccessControlType)accessControlType,
                            new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)),
                            accessMask,
                            (InheritanceFlags)inheritanceFlags,
                            (PropagationFlags)propagationFlags);
                        Console.WriteLine("Should not allow RemoveAccessSpecific Ace to ACL with customAce/CompoundAce");
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }



                //Case 7, Remove Specific Ace of NOT(AccessControlType.Allow |AccessControlType.Denied) to the DiscretionaryAcl with no ACE, 
                // should throw appropriate exception for wrong parameter, bug#287188

                {

                    testCasesPerformed++;


                    try
                    {
                        isContainer = true;
                        isDS = false;

                        inheritanceFlags = 1;//InheritanceFlags.ContainerInherit
                        propagationFlags = 2; //PropagationFlags.InheritOnly

                        accessControlType = 100;
                        sid = "BA";
                        accessMask = 1;

                        rawAcl = new RawAcl(0, 1);
                        discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                        try
                        {
                            discretionaryAcl.RemoveAccessSpecific((AccessControlType)accessControlType,
                                new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)),
                                accessMask,
                                (InheritanceFlags)inheritanceFlags,
                                (PropagationFlags)propagationFlags);

                            Console.WriteLine("Should not allow RemoveAccessSpecific NOT(AccessControlType.Allow |AccessControlType.Allow) access ");
                            Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                            testCasesPassed++;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception generated: " + e.Message, e);
                    }

                }
            }
        }

        //----------------------------------------------------------------------------------------------
        /*
        *  Class to test DiscretionaryAcl method inherited from abstract base class CommonAcl
        *		 public void RemoveInheritedAces()
        *
        *
        */
        //----------------------------------------------------------------------------------------------


        private class RemoveInheritedAcesTestCases
        {
            //No creating objects

            private RemoveInheritedAcesTestCases() { }

            public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                Console.WriteLine("Running RemoveInheritedAcesTestCases");

                BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, false);
                AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
            }

            public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed, bool isBVT)
            {
                Console.WriteLine("Running BasicValidationTestCases");



                bool isContainer = false;
                bool isDS = false;

                RawAcl rawAcl = null;
                DiscretionaryAcl discretionaryAcl = null;

                GenericAce gAce = null;
                byte revision = 0;
                int capacity = 0;

                //CustomAce constructor parameters
                AceType aceType = AceType.AccessAllowed;
                AceFlags aceFlag = AceFlags.None;
                byte[] opaque = null;

                //CompoundAce constructor additional parameters
                int accessMask = 0;
                CompoundAceType compoundAceType = CompoundAceType.Impersonation;
                string sid = "BG";

                //CommonAce constructor additional parameters
                AceQualifier aceQualifier = 0;

                //ObjectAce constructor additional parameters
                ObjectAceFlags objectAceFlag = 0;
                Guid objectAceType;
                Guid inheritedObjectAceType;

                //case 1, no Ace
                testCasesPerformed++;


                try
                {
                    revision = 127;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    isContainer = true;
                    isDS = false;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    if (TestRemoveInheritedAces(discretionaryAcl))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 2, only have explicit Ace
                testCasesPerformed++;


                try
                {
                    revision = 0;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    //199  has all AceFlags except InheritOnly and Inherited
                    gAce = new CommonAce((AceFlags)199, AceQualifier.AccessAllowed, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
                    rawAcl.InsertAce(0, gAce);
                    isContainer = false;
                    isDS = false;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    if (TestRemoveInheritedAces(discretionaryAcl))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 3,  non-inherited CommonAce, ObjectAce, CompoundAce, CustomAce
                testCasesPerformed++;


                try
                {
                    revision = 127;
                    capacity = 5;
                    sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")).ToString();
                    rawAcl = new RawAcl(revision, capacity);
                    aceFlag = AceFlags.InheritanceFlags;
                    accessMask = 1;

                    //Access Allowed CommonAce
                    gAce = new CommonAce(aceFlag, AceQualifier.AccessAllowed, accessMask,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid + 1.ToString())), false, null);
                    rawAcl.InsertAce(0, gAce);
                    //Access Dennied CommonAce
                    gAce = new CommonAce(aceFlag, AceQualifier.AccessDenied, accessMask,
                    new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid + 2.ToString())), false, null);
                    rawAcl.InsertAce(0, gAce);
                    //CustomAce
                    aceType = AceType.MaxDefinedAceType + 1;
                    opaque = null;
                    gAce = new CustomAce(aceType, aceFlag, opaque);
                    rawAcl.InsertAce(2, gAce);
                    //CompoundAce
                    compoundAceType = CompoundAceType.Impersonation;
                    gAce = new CompoundAce(aceFlag, accessMask, compoundAceType,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid + 3.ToString())));
                    rawAcl.InsertAce(3, gAce);
                    //ObjectAce
                    aceQualifier = AceQualifier.AccessAllowed;
                    objectAceFlag = ObjectAceFlags.ObjectAceTypePresent | ObjectAceFlags.InheritedObjectAceTypePresent;
                    objectAceType = new Guid("11111111-1111-1111-1111-111111111111");
                    inheritedObjectAceType = new Guid("22222222-2222-2222-2222-222222222222");
                    gAce = new ObjectAce(aceFlag, aceQualifier, accessMask,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid + 4.ToString())), objectAceFlag, objectAceType, inheritedObjectAceType, false, null);
                    rawAcl.InsertAce(2, gAce);
                    isContainer = true;
                    isDS = false;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    //After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
                    //forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
                    try
                    {

                        TestRemoveInheritedAces(discretionaryAcl);

                        Console.WriteLine("Should not allow RemoveInheritedAces from ACL with customAce/CompoundAce");
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                if (isBVT)
                {
                    //first 3 cases are BVT cases
                    return;
                }

                //case 4,  all inherited CommonAce, ObjectAce, CompoundAce, CustomAce
                testCasesPerformed++;


                try
                {
                    revision = 127;
                    capacity = 5;
                    sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")).ToString();
                    rawAcl = new RawAcl(revision, capacity);
                    aceFlag = AceFlags.InheritanceFlags | AceFlags.Inherited;
                    accessMask = 1;

                    //Access Allowed CommonAce
                    gAce = new CommonAce(aceFlag, AceQualifier.AccessAllowed, accessMask,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid + 1.ToString())), false, null);
                    rawAcl.InsertAce(0, gAce);
                    //Access Dennied CommonAce
                    gAce = new CommonAce(aceFlag, AceQualifier.AccessDenied, accessMask,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid + 2.ToString())), false, null);
                    rawAcl.InsertAce(0, gAce);
                    //CustomAce
                    aceType = AceType.MaxDefinedAceType + 1;
                    opaque = null;
                    gAce = new CustomAce(aceType, aceFlag, opaque);
                    rawAcl.InsertAce(0, gAce);
                    //CompoundAce
                    compoundAceType = CompoundAceType.Impersonation;
                    gAce = new CompoundAce(aceFlag, accessMask, compoundAceType,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid + 3.ToString())));
                    rawAcl.InsertAce(0, gAce);
                    //ObjectAce
                    aceQualifier = AceQualifier.AccessAllowed;
                    objectAceFlag = ObjectAceFlags.ObjectAceTypePresent | ObjectAceFlags.InheritedObjectAceTypePresent;
                    objectAceType = new Guid("11111111-1111-1111-1111-111111111111");
                    inheritedObjectAceType = new Guid("22222222-2222-2222-2222-222222222222");
                    gAce = new ObjectAce(aceFlag, aceQualifier, accessMask,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid + 4.ToString())), objectAceFlag, objectAceType, inheritedObjectAceType, false, null);
                    rawAcl.InsertAce(0, gAce);
                    isContainer = true;
                    isDS = false;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    if (TestRemoveInheritedAces(discretionaryAcl))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 5, only have one inherit Ace
                testCasesPerformed++;


                try
                {
                    revision = 0;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    //215 has all AceFlags except InheritOnly
                    gAce = new CommonAce((AceFlags)215, AceQualifier.AccessDenied, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
                    rawAcl.InsertAce(0, gAce);
                    isContainer = false;
                    isDS = false;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    if (TestRemoveInheritedAces(discretionaryAcl))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }



                //case 6, have one explicit Ace and one inherited Ace
                testCasesPerformed++;


                try
                {
                    revision = 255;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    //199  has all AceFlags except InheritOnly and Inherited					
                    gAce = new CommonAce((AceFlags)(FlagsForAce.AuditFlags | FlagsForAce.OI | FlagsForAce.CI | FlagsForAce.NP), AceQualifier.AccessDenied, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
                    rawAcl.InsertAce(0, gAce);
                    //215  has all AceFlags except InheritOnly					
                    gAce = new CommonAce((AceFlags)(FlagsForAce.AuditFlags | FlagsForAce.OI | FlagsForAce.CI | FlagsForAce.NP | FlagsForAce.IH), AceQualifier.AccessAllowed, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                    rawAcl.InsertAce(1, gAce);
                    isContainer = true;
                    isDS = false;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    if (TestRemoveInheritedAces(discretionaryAcl))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 7, have two inherited Aces
                testCasesPerformed++;


                try
                {
                    revision = 255;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    //215  has all AceFlags except InheritOnly					
                    gAce = new CommonAce((AceFlags)215, AceQualifier.AccessAllowed, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
                    rawAcl.InsertAce(0, gAce);
                    sid = "BA";
                    //16 has Inherited
                    gAce = new CommonAce((AceFlags)16, AceQualifier.AccessDenied, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                    rawAcl.InsertAce(0, gAce);
                    isContainer = true;
                    isDS = false;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    if (TestRemoveInheritedAces(discretionaryAcl))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 8, 1 inherited CustomAce
                testCasesPerformed++;


                try
                {
                    revision = 127;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    aceType = AceType.MaxDefinedAceType + 1;
                    //215 has all AceFlags except InheritOnly
                    aceFlag = (AceFlags)215;
                    opaque = null;
                    gAce = new CustomAce(aceType, aceFlag, opaque);
                    rawAcl.InsertAce(0, gAce);
                    isContainer = false;
                    isDS = false;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    if (TestRemoveInheritedAces(discretionaryAcl))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }



                //case 9,  1 inherited CompoundAce
                testCasesPerformed++;


                try
                {
                    revision = 127;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    aceFlag = (AceFlags)223; //all flags ored together
                    accessMask = 1;
                    compoundAceType = CompoundAceType.Impersonation;
                    gAce = new CompoundAce(aceFlag, accessMask, compoundAceType,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)));
                    rawAcl.InsertAce(0, gAce);
                    isContainer = true;
                    isDS = false;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    if (TestRemoveInheritedAces(discretionaryAcl))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 10, 1 inherited ObjectAce
                testCasesPerformed++;


                try
                {
                    revision = 127;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    aceFlag = (AceFlags)223; //all flags ored together
                    aceQualifier = AceQualifier.AccessAllowed;
                    accessMask = 1;
                    objectAceFlag = ObjectAceFlags.ObjectAceTypePresent | ObjectAceFlags.InheritedObjectAceTypePresent;
                    objectAceType = new Guid("11111111-1111-1111-1111-111111111111");
                    inheritedObjectAceType = new Guid("22222222-2222-2222-2222-222222222222");
                    gAce = new ObjectAce(aceFlag, aceQualifier, accessMask,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), objectAceFlag, objectAceType, inheritedObjectAceType, false, null);
                    rawAcl.InsertAce(0, gAce);
                    isContainer = true;
                    isDS = true;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    if (TestRemoveInheritedAces(discretionaryAcl))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


            }

            public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                // No test cases yet
            }

            private static bool TestRemoveInheritedAces(DiscretionaryAcl discretionaryAcl)
            {

                GenericAce ace = null;


                discretionaryAcl.RemoveInheritedAces();
                for (int i = 0; i < discretionaryAcl.Count; i++)
                {
                    ace = discretionaryAcl[i];
                    if ((ace.AceFlags & AceFlags.Inherited) != 0)
                        return false;
                }
                return true;
            }
        }

        //----------------------------------------------------------------------------------------------
        /*
        *  Class to test DiscretionaryAcl method inherited from abstract base class CommonAcl
        *		 public void Purge( SecurityIdentifier sid )
        *
        *
        */
        //----------------------------------------------------------------------------------------------

        private class PurgeTestCases
        {
            //No creating objects

            private PurgeTestCases() { }

            public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                Console.WriteLine("Running PurgeTestCases");

                BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, false);
                AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
            }

            public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed, bool isBVT)
            {
                Console.WriteLine("Running BasicValidationTestCases");



                bool isContainer = false;
                bool isDS = false;

                RawAcl rawAcl = null;
                DiscretionaryAcl discretionaryAcl = null;
                int aceCount = 0;
                SecurityIdentifier sid = null;

                GenericAce gAce = null;
                byte revision = 0;
                int capacity = 0;

                //CustomAce constructor parameters
                AceType aceType = AceType.AccessAllowed;
                AceFlags aceFlag = AceFlags.None;
                byte[] opaque = null;

                //CompoundAce constructor additional parameters
                int accessMask = 0;
                CompoundAceType compoundAceType = CompoundAceType.Impersonation;
                string sidStr = "LA";

                //CommonAce constructor additional parameters
                AceQualifier aceQualifier = 0;

                //ObjectAce constructor additional parameters
                ObjectAceFlags objectAceFlag = 0;
                Guid objectAceType;
                Guid inheritedObjectAceType;

                //case 1, no Ace
                testCasesPerformed++;


                try
                {
                    revision = 127;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    isContainer = true;
                    isDS = false;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    aceCount = 0;
                    sidStr = "BG";
                    sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sidStr));

                    if (TestPurge(discretionaryAcl, sid, aceCount))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }



                //case 2, only have 1 explicit Ace of the sid
                testCasesPerformed++;


                try
                {
                    revision = 0;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    sidStr = "BG";
                    sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sidStr));
                    //199 has all aceflags but inheritonly and inherited					
                    gAce = new CommonAce((AceFlags)199, AceQualifier.AccessAllowed, 1, sid, false, null);
                    rawAcl.InsertAce(0, gAce);
                    isContainer = false;
                    isDS = false;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    aceCount = 0;

                    if (TestPurge(discretionaryAcl, sid, aceCount))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 3, only have 1 explicit Ace of different sid
                testCasesPerformed++;


                try
                {
                    revision = 0;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    //199 has all aceflags but inheritedonly and inherited
                    sidStr = "BG";
                    sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sidStr));
                    gAce = new CommonAce((AceFlags)199, AceQualifier.AccessDenied, 1, sid, false, null);
                    rawAcl.InsertAce(0, gAce);
                    isContainer = false;
                    isDS = false;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    aceCount = 1;
                    sidStr = "BA";
                    sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sidStr));

                    if (TestPurge(discretionaryAcl, sid, aceCount))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }



                if (isBVT)
                {
                    //first 3 cases are BVT cases
                    return;
                }

                //case 4, only have 1 inherited Ace of the sid
                testCasesPerformed++;


                try
                {
                    revision = 0;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    sidStr = "BG";
                    sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sidStr));
                    //215 has all aceflags but inheritedonly				
                    gAce = new CommonAce((AceFlags)215, AceQualifier.AccessAllowed, 1, sid, false, null);
                    rawAcl.InsertAce(0, gAce);
                    isContainer = false;
                    isDS = false;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    aceCount = 1;

                    if (TestPurge(discretionaryAcl, sid, aceCount))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 5, have one explicit Ace and one inherited Ace of the sid
                testCasesPerformed++;


                try
                {
                    revision = 255;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    sidStr = "BG";
                    sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sidStr));
                    //199 has all aceflags but inheritedonly and inherited
                    gAce = new CommonAce((AceFlags)(FlagsForAce.AuditFlags | FlagsForAce.OI | FlagsForAce.CI | FlagsForAce.NP), AceQualifier.AccessDenied, 1, sid, false, null);
                    rawAcl.InsertAce(0, gAce);
                    //215 has all aceflags but inheritedonly
                    gAce = new CommonAce((AceFlags)(FlagsForAce.AuditFlags | FlagsForAce.OI | FlagsForAce.CI | FlagsForAce.NP | FlagsForAce.IH), AceQualifier.AccessAllowed, 2, sid, false, null);
                    rawAcl.InsertAce(1, gAce);
                    isContainer = true;
                    isDS = false;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    aceCount = 1;

                    if (TestPurge(discretionaryAcl, sid, aceCount))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 6, have two explicit Aces of the sid
                testCasesPerformed++;


                try
                {
                    revision = 255;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    sidStr = "BG";
                    sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sidStr));
                    //207 has all AceFlags but inherited				
                    gAce = new CommonAce((AceFlags)207, AceQualifier.AccessAllowed, 1, sid, false, null);
                    rawAcl.InsertAce(0, gAce);
                    gAce = new CommonAce(AceFlags.None, AceQualifier.AccessDenied, 2, sid, false, null);
                    rawAcl.InsertAce(0, gAce);
                    isContainer = true;
                    isDS = false;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    aceCount = 0;

                    if (TestPurge(discretionaryAcl, sid, 0))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 7, 1 explicit CustomAce
                testCasesPerformed++;


                try
                {
                    revision = 127;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    aceType = AceType.MaxDefinedAceType + 1;
                    //199 has all AceFlags except InheritOnly and Inherited
                    aceFlag = (AceFlags)199;
                    opaque = null;
                    gAce = new CustomAce(aceType, aceFlag, opaque);
                    rawAcl.InsertAce(0, gAce);
                    isContainer = false;
                    isDS = false;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG"));
                    aceCount = 1;

                    //After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
                    //forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
                    try
                    {

                        TestPurge(discretionaryAcl, sid, aceCount);

                        Console.WriteLine("Should not allow Purge from ACL with customAce/CompoundAce");
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }



                //case 8,  1 explicit CompoundAce
                testCasesPerformed++;


                try
                {
                    revision = 127;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    //207 has all AceFlags but inherited				
                    aceFlag = (AceFlags)207;
                    accessMask = 1;
                    compoundAceType = CompoundAceType.Impersonation;
                    sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG"));
                    gAce = new CompoundAce(aceFlag, accessMask, compoundAceType, sid);
                    rawAcl.InsertAce(0, gAce);
                    isContainer = true;
                    isDS = false;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    aceCount = 0;

                    //After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
                    //forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
                    try
                    {

                        TestPurge(discretionaryAcl, sid, aceCount);

                        Console.WriteLine("Should not allow Purge from ACL with customAce/CompoundAce");
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 9, 1 explict ObjectAce
                testCasesPerformed++;


                try
                {
                    revision = 127;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG"));
                    //207 has all AceFlags but inherited						
                    aceFlag = (AceFlags)207;
                    aceQualifier = AceQualifier.AccessAllowed;
                    accessMask = 1;
                    objectAceFlag = ObjectAceFlags.ObjectAceTypePresent | ObjectAceFlags.InheritedObjectAceTypePresent;
                    objectAceType = new Guid("11111111-1111-1111-1111-111111111111");
                    inheritedObjectAceType = new Guid("22222222-2222-2222-2222-222222222222");
                    gAce = new ObjectAce(aceFlag, aceQualifier, accessMask, sid, objectAceFlag, objectAceType, inheritedObjectAceType, false, null);
                    rawAcl.InsertAce(0, gAce);
                    isContainer = true;
                    isDS = true;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                    aceCount = 0;

                    if (TestPurge(discretionaryAcl, sid, aceCount))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


            }

            public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                bool isContainer = false;
                bool isDS = false;

                RawAcl rawAcl = null;
                DiscretionaryAcl discretionaryAcl = null;

                byte revision = 0;
                int capacity = 0;

                Console.WriteLine("Running AdditionalTestCases");



                //case 1, null Sid
                testCasesPerformed++;


                try
                {
                    revision = 127;
                    capacity = 1;
                    rawAcl = new RawAcl(revision, capacity);
                    isContainer = true;
                    isDS = false;
                    discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                    discretionaryAcl.Purge(null);
                    Console.WriteLine("Should not allow purge null sid");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (ArgumentNullException)
                {
                    Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                    testCasesPassed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }

            }

            private static bool TestPurge(DiscretionaryAcl discretionaryAcl, SecurityIdentifier sid, int aceCount)
            {

                KnownAce ace = null;

                discretionaryAcl.Purge(sid);
                if (aceCount != discretionaryAcl.Count)
                    return false;
                for (int i = 0; i < discretionaryAcl.Count; i++)
                {
                    ace = discretionaryAcl[i] as KnownAce;
                    if (ace != null && ((ace.AceFlags & AceFlags.Inherited) == 0))
                    {
                        if (ace.SecurityIdentifier == sid)
                            return false;
                    }
                }
                return true;
            }
        }


        //as the binaryform has been checked in the constructor and add/set/remove/removespecific, only some corner cases covered here
        private class GetBinaryFormTestCases
        {
            //No creating objects
            private GetBinaryFormTestCases() { }

            public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {

                Console.WriteLine("Running GetBinaryFormTestCases.AllTestCases()");
                BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
                AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
            }

            public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                DiscretionaryAcl dAcl = null;
                RawAcl rAcl = null;
                GenericAce gAce = null;
                byte[] binaryForm = null;

                Console.WriteLine("Running BasicValidationTestCases");



                //Case 1, array binaryForm is null
                testCasesPerformed++;


                try
                {
                    rAcl = new RawAcl(GenericAcl.AclRevision, 1);
                    gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                    rAcl.InsertAce(0, gAce);
                    dAcl = new DiscretionaryAcl(true, false, rAcl);
                    try
                    {
                        dAcl.GetBinaryForm(binaryForm, 0);
                        Console.WriteLine("Should throw ArgumentNullException when binaryForm is null");
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                    }
                    catch (ArgumentNullException)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case 2, offset is negative
                testCasesPerformed++;


                try
                {
                    binaryForm = new byte[100];
                    rAcl = new RawAcl(GenericAcl.AclRevision, 1);
                    gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                    rAcl.InsertAce(0, gAce);
                    dAcl = new DiscretionaryAcl(true, false, rAcl);
                    try
                    {
                        dAcl.GetBinaryForm(binaryForm, -1);
                        Console.WriteLine("Should throw ArgumentOutOfRangeException when offset is negative");
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case 3, offset is equal to binaryForm length
                testCasesPerformed++;


                try
                {
                    binaryForm = new byte[100];
                    rAcl = new RawAcl(GenericAcl.AclRevision, 1);
                    gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                    rAcl.InsertAce(0, gAce);
                    dAcl = new DiscretionaryAcl(true, false, rAcl);
                    try
                    {
                        dAcl.GetBinaryForm(binaryForm, binaryForm.Length);
                        Console.WriteLine("Should throw ArgumentOutOfRangeException when offset equals to array length");
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case 4, offset is a big possitive number
                testCasesPerformed++;


                try
                {
                    rAcl = new RawAcl(GenericAcl.AclRevision, 1);
                    gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                    rAcl.InsertAce(0, gAce);
                    dAcl = new DiscretionaryAcl(true, false, rAcl);
                    binaryForm = new byte[dAcl.BinaryLength + 10000];

                    dAcl.GetBinaryForm(binaryForm, 10000);
                    //get the binaryForm of the original RawAcl
                    byte[] verifierBinaryForm = new byte[rAcl.BinaryLength];
                    rAcl.GetBinaryForm(verifierBinaryForm, 0);
                    if (Utils.UtilIsBinaryFormEqual(binaryForm, 10000, verifierBinaryForm))
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);

                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //Case 5, binaryForm array's size is insufficient
                testCasesPerformed++;


                try
                {
                    binaryForm = new byte[4];
                    rAcl = new RawAcl(GenericAcl.AclRevision, 1);
                    gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                    rAcl.InsertAce(0, gAce);
                    dAcl = new DiscretionaryAcl(true, false, rAcl);
                    try
                    {
                        dAcl.GetBinaryForm(binaryForm, 0);
                        Console.WriteLine("Should throw ArgumentOutOfRangeException when array has insufficient size");
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }

            }

            public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                // No test cases yet
            }

        }

    }
}



