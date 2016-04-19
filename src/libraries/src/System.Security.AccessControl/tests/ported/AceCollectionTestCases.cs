//--------------------------------------------------------------------------
//
//		AceEnumerator test cases
//
//		Tests the AceEnumerator class
//
//		Copyright (C) Microsoft Corporation, 2003
//
//
//--------------------------------------------------------------------------

using System;
using System.Collections;
using System.Security.AccessControl;
using System.Security.Principal;

namespace System.Security.AccessControl.Test
{
    //----------------------------------------------------------------------------------------------
    /*
    *  Class to test AceEnumerator Class
    *
    *
    */
    //----------------------------------------------------------------------------------------------

    public class AceEnumeratorTestCases
    {

        // No creating objects!
        private AceEnumeratorTestCases() { }

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
         * 
         * 
        */

        public static Boolean Test()
        {
            Console.WriteLine("\n\n=======STARTING AceCollectionTestCases==========\n");
            return AllTestCases();
        }

        public static Boolean AllTestCases()
        {
            Console.WriteLine("Running AceEnumeratorTestCases");

            int testCasesPerformed = 0;
            int testCasesPassed = 0;

            AceEnumeratorPropertyMethodsTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);
            AceEnumeratorExplicitInterfaceCurrentTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);

            return (testCasesPerformed == testCasesPassed);
        }

        public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
        {
            Console.WriteLine("Running AceEnumeratorTestCases");

            Console.WriteLine("Running AceEnumeratorPropertyMethodsTestCases");
            AceEnumeratorPropertyMethodsTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);

            Console.WriteLine("Running AceEnumeratorExplicitInterfaceCurrentTestCases");
            AceEnumeratorExplicitInterfaceCurrentTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);


        }

        public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
        {
            // No test cases yet
        }


        //----------------------------------------------------------------------------------------------
        /*
        *  Class to test class AceEnumerator's methods/properties
        *		public GenericAce Current
        *		public bool MoveNext()
        *		public void Reset()
        *
        *
        */
        //----------------------------------------------------------------------------------------------	

        private class AceEnumeratorPropertyMethodsTestCases
        {


            /*
            * Constructor
            *
            */
            private AceEnumeratorPropertyMethodsTestCases() { }

            /*
            * Method Name: AllTestCases
            *
            * Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
            *			AceEnumerator's methods/properties
            *
            * Parameter:	testCasesPerformed -- sum of all the test cases performed
            *			testCasePassed -- total number of test cases passed
            *
            * Return:		none
            */
            public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                Console.WriteLine("Running AceEnumeratorPropertyMethodsTestCases");

                BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
                AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
            }

            /*
            * Method Name: TestAcessCurrentFailure
            *
            * Description:	 Test the Current property of an AceEnumerator which is positioned before the 
            *			first element or after the last element of the collection
            *
            * Parameter:	aceEnumerator -- the AceEnumerator object whose Current property to be tested
            *
            * Return:		true when expected exception is throw, false otherwise
            */
            private static bool TestAcessCurrentFailure(AceEnumerator aceEnumerator)
            {
                bool result = false;

                GenericAce gAce = null;
                try
                {
                    gAce = aceEnumerator.Current;
                    Console.WriteLine("Should throw InvalidOperationException when enumerator is positioned before the first element or after the last element of the collection");
                }
                catch (InvalidOperationException)
                {
                    //this is expected, do nothing
                    result = true;
                    Console.WriteLine("Access Current passed");
                }
                return result;

            }

            /*
            * Method Name: TestMoveNextFailure
            *
            * Description:	Test MoveNext of an AceEnumerator which is positioned after the last element of the collection.
            *			If MoveNext returns false as expected, further test accessing Current property throws expected exception
            *
            * Parameter:	aceEnumerator 	-- the AceEnumerator object whose MoveNext property to be tested
            *
            * Return:		true when MoveNext returns false and accessing Current throws expected exception, false otherwise
            */
            private static bool TestMoveNextFailure(AceEnumerator aceEnumerator)
            {
                bool result = false;

                GenericAce gAce = null;

                if (aceEnumerator.MoveNext())
                {
                    Console.WriteLine("MoveNext should return false after passing the last element");
                }
                else
                {
                    try
                    {
                        gAce = aceEnumerator.Current;
                        Console.WriteLine("Should throw InvalidOperationException after MoveNext pass the last element");
                    }
                    catch (InvalidOperationException)
                    {
                        //this is expected, do nothing
                        result = true;
                        Console.WriteLine("Access Current after call MoveNext passing the last element passed");
                    }
                }
                return result;
            }


            /*
            * Method Name: TestMoveNextSuccess
            *
            * Description:	Test MoveNext of an AceEnumerator which is positioned anywhere before the last element of the collection.
            *			If MoveNext returns true as expected, further test the Ace object returned by Current is compatible
            *			with the verifierGAce. If isRawAcl is true, test the Ace object and the verifierGAce references the same 
            *			GenericAce object. Otherwise, test the Ace object and the verifierGAce have same content.
            *
            * Parameter:	aceEnumerator	-- the AceEnumerator object whose MoveNext property to be tested
            *			isRawAcl			-- is the aceEnumerator from a RawAcl object or CommonAcl object
            *			verifierGAce		-- the GenericAce object to be compared with			
            *
            * Return:		true when MoveNext returns true and the Current properti returns a GenericAce object compatible with verifierGAce
            */
            private static bool TestMoveNextSuccess(AceEnumerator aceEnumerator, bool isRawAcl, GenericAce verifierGAce)
            {
                bool result = false;

                GenericAce gAce = null;

                if (!aceEnumerator.MoveNext())
                {
                    Console.WriteLine("MoveNext should return true before passing the last");
                }
                else
                {
                    gAce = aceEnumerator.Current;

                    if (isRawAcl)
                    {//from RawAcl
                        if (gAce != verifierGAce)
                        {
                            Console.WriteLine("Current Ace does not reference to the same Ace of the rawAcl");
                        }
                        else
                        {
                            result = true;
                            Console.WriteLine("Access Current passed");
                        }
                    }
                    else
                    {//from CommonAcl
                        //note it is the caller's responsiblity to verify gAce and verifierGAce do not reference same object					
                        if (!Utils.UtilIsAceEqual(gAce, verifierGAce))
                        {
                            Console.WriteLine("Current Ace has not same content as the ACE");
                        }
                        else
                        {
                            result = true;
                            Console.WriteLine("Access Current passed");
                        }
                    }
                }
                return result;

            }

            /*
            * Method Name: BasicValidationTestCases
            *
            * Description:	execute basic testing cases 
            *
            * Parameter:	testCasesPerformed -- sum of all the test cases performed
            *			testCasePassed -- total number of test cases passed
            *
            * Return:		none
            */
            public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                Console.WriteLine("Running BasicValidationTestCases");

                GenericAce gAce = null;
                RawAcl rAcl = null;
                AceEnumerator aceEnumerator = null;
                bool passed = false;


                //Case 1, Enumerator from empty RawAcl
                testCasesPerformed++;

                try
                {
                    passed = true;
                    Console.WriteLine("Begin collection from empty RawAcl case");
                    rAcl = new RawAcl(1, 1);
                    aceEnumerator = rAcl.GetEnumerator();

                    passed = passed && TestAcessCurrentFailure(aceEnumerator);

                    passed = passed && TestMoveNextFailure(aceEnumerator);

                    aceEnumerator.Reset();
                    passed = passed && TestAcessCurrentFailure(aceEnumerator);

                    if (passed)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (ArgumentOutOfRangeException)
                {
                    Console.WriteLine("*******Bug225478*******");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }

                //Case 2, collection from RawAcl has one ACE
                testCasesPerformed++;

                try
                {
                    passed = true;
                    Console.WriteLine("Begin 1 Ace collection from RawAcl case");

                    rAcl = new RawAcl(0, 1);
                    gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
                    rAcl.InsertAce(0, gAce);
                    aceEnumerator = rAcl.GetEnumerator();

                    passed = passed && TestAcessCurrentFailure(aceEnumerator);
                    passed = passed && TestMoveNextSuccess(aceEnumerator, true, rAcl[0]);
                    passed = passed && TestMoveNextFailure(aceEnumerator);
                    aceEnumerator.Reset();
                    passed = passed && TestAcessCurrentFailure(aceEnumerator);
                    passed = passed && TestMoveNextSuccess(aceEnumerator, true, rAcl[0]);
                    if (passed)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (ArgumentOutOfRangeException)
                {
                    Console.WriteLine("*******Bug225478*******");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }

                //Case 3, collection from CommonAcl has one ACE
                testCasesPerformed++;

                try
                {
                    passed = true;
                    Console.WriteLine("Begin 1 Ace collection from SystemAcl case");

                    rAcl = new RawAcl(0, 1);
                    gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
                    rAcl.InsertAce(0, gAce);
                    SystemAcl sAcl = new SystemAcl(true, false, rAcl);
                    aceEnumerator = sAcl.GetEnumerator();

                    passed = passed && TestAcessCurrentFailure(aceEnumerator);
                    passed = passed && TestMoveNextSuccess(aceEnumerator, false, sAcl[0]);


                    //test for CommonAcl, Current return a clone instead of a reference
                    //modify the AceFlags through the enumerator, then check if the SystemAcl is modified
                    aceEnumerator.Current.AceFlags = (AceFlags.Inherited | AceFlags.FailedAccess);

                    if (sAcl[0].AceFlags == (AceFlags.Inherited | AceFlags.FailedAccess))
                    {
                        Console.WriteLine("Ace returned by Current is not cloned");
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                        //do not need to do further check
                        return;
                    }
                    else
                    {
                        Console.WriteLine("Test ace returned by Current is cloned passed");
                    }

                    passed = passed && TestMoveNextFailure(aceEnumerator);
                    aceEnumerator.Reset();
                    passed = passed && TestAcessCurrentFailure(aceEnumerator);
                    passed = passed && TestMoveNextSuccess(aceEnumerator, false, sAcl[0]);
                    if (passed)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (ArgumentOutOfRangeException)
                {
                    Console.WriteLine("*******Bug225478*******");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }
                finally
                {//make sure this is called even Current does not clone the Ace for CommonAcl
                }

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
                GenericAce gAce = null;
                RawAcl rAcl = null;
                AceEnumerator aceEnumerator = null;
                int i = 0;
                bool passed = false;

                Console.WriteLine("Running AdditionalTestCases");

                //Case 1, RawAcl with huge number of Aces
                testCasesPerformed++;

                try
                {
                    passed = true;
                    Console.WriteLine("Begin GenericAcl.MaxBinaryLength + 1 Aces collection from RawAcl case");
                    rAcl = new RawAcl(0, 1820);
                    for (i = 0; i < 1820; i++)
                    {
                        gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, i + 1,
                            new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
                        rAcl.InsertAce(0, gAce);
                    }

                    aceEnumerator = rAcl.GetEnumerator();

                    passed = passed && TestAcessCurrentFailure(aceEnumerator);
                    //The reason not to call TestMoveNextSuccess is to avoid the success output info for each Ace
                    for (i = 0; i < rAcl.Count; i++)
                    {
                        gAce = null;
                        if (!aceEnumerator.MoveNext())
                        {
                            passed = false;
                            Console.WriteLine("MoveNext should return true before passing the last");
                            break;
                        }
                        else
                        {
                            gAce = aceEnumerator.Current;

                            if (gAce != rAcl[i])
                            {
                                passed = false;
                                Console.WriteLine("Current Ace does not reference to the same Ace of the rawAcl");
                                break;
                            }

                        }
                    }

                    passed = passed && TestMoveNextFailure(aceEnumerator);
                    aceEnumerator.Reset();
                    passed = passed && TestAcessCurrentFailure(aceEnumerator);
                    passed = passed && TestMoveNextSuccess(aceEnumerator, true, rAcl[0]);
                    if (passed)
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
        *  Class to test class AceEnumerator's methods/properties
        *		object IEnumerator.Current
        *		public bool MoveNext()
        *		public void Reset()
        *
        *
        */
        //----------------------------------------------------------------------------------------------	

        private class AceEnumeratorExplicitInterfaceCurrentTestCases
        {
            //No creating objects
            private AceEnumeratorExplicitInterfaceCurrentTestCases() { }

            public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                Console.WriteLine("Running AceEnumeratorExplicitInterfaceCurrentTestCases");

                BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
                AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
            }

            /*
            * Method Name: TestAcessCurrentFailure
            *
            * Description:	 Test the Current property of an AceEnumerator which is positioned before the 
            *			first element or after the last element of the collection
            *
            * Parameter:	aceEnumerator -- the AceEnumerator object whose Current property to be tested
            *
            * Return:		true when expected exception is throw, false otherwise
            */
            private static bool TestAcessCurrentFailure(AceEnumerator aceEnumerator)
            {
                bool result = false;
                GenericAce gAce = null;
                try
                {
                    gAce = (GenericAce)((IEnumerator)aceEnumerator).Current;
                    Console.WriteLine("Should throw InvalidOperationException when enumerator is positioned before the first element or after the last element of the collection");
                }
                catch (InvalidOperationException)
                {
                    //this is expected, do nothing
                    result = true;
                    Console.WriteLine("Access Current passed");
                }
                return result;
            }

            /*
            * Method Name: TestMoveNextFailure
            *
            * Description:	Test MoveNext of an AceEnumerator which is positioned after the last element of the collection.
            *			If MoveNext returns false as expected, further test accessing Current property throws expected exception
            *
            * Parameter:	aceEnumerator 	-- the AceEnumerator object whose MoveNext property to be tested
            *
            * Return:		true when MoveNext returns false and accessing Current throws expected exception, false otherwise
            */
            private static bool TestMoveNextFailure(AceEnumerator aceEnumerator)
            {
                bool result = false;
                GenericAce gAce = null;

                if (aceEnumerator.MoveNext())
                {
                    Console.WriteLine("MoveNext should return false after passing the last element");
                }
                else
                {
                    try
                    {
                        gAce = (GenericAce)((IEnumerator)aceEnumerator).Current;
                        Console.WriteLine("Should throw InvalidOperationException after MoveNext pass the last element");
                    }
                    catch (InvalidOperationException)
                    {
                        //this is expected, do nothing
                        result = true;
                        Console.WriteLine("Access Current after call MoveNext passing the last element passed");
                    }
                }
                return result;
            }

            /*
            * Method Name: TestMoveNextSuccess
            *
            * Description:	Test MoveNext of an AceEnumerator which is positioned anywhere before the last element of the collection.
            *			If MoveNext returns true as expected, further test the Ace object returned by Current is compatible
            *			with the verifierGAce. If isRawAcl is true, test the Ace object and the verifierGAce references the same 
            *			GenericAce object. Otherwise, test the Ace object and the verifierGAce have same content.
            *
            * Parameter:	aceEnumerator	-- the AceEnumerator object whose MoveNext property to be tested
            *			isRawAcl			-- is the aceEnumerator from a RawAcl object or CommonAcl object
            *			verifierGAce		-- the GenericAce object to be compared with			
            *
            * Return:		true when MoveNext returns true and the Current properti returns a GenericAce object compatible with verifierGAce
            */
            private static bool TestMoveNextSuccess(AceEnumerator aceEnumerator, bool isRawAcl, GenericAce verifierGAce)
            {
                bool result = false;
                GenericAce gAce = null;

                if (!aceEnumerator.MoveNext())
                {
                    Console.WriteLine("MoveNext should return true before passing the last");
                }
                else
                {
                    gAce = (GenericAce)((IEnumerator)aceEnumerator).Current;
                    if (isRawAcl)
                    {//from RawAcl
                        if (gAce != verifierGAce)
                        {
                            Console.WriteLine("Current Ace does not reference to the same Ace of the rawAcl");
                        }
                        else
                        {
                            result = true;
                            Console.WriteLine("Access Current passed");
                        }
                    }
                    else
                    {//from CommonAcl
                        //note it is the caller's responsiblity to verify gAce and verifierGAce do not reference same object					
                        if (!Utils.UtilIsAceEqual(gAce, verifierGAce))
                        {
                            Console.WriteLine("Current Ace has not same content as the ACE");
                        }
                        else
                        {
                            result = true;
                            Console.WriteLine("Access Current passed");
                        }
                    }
                }
                return result;
            }


            /*
            * Method Name: BasicValidationTestCases
            *
            * Description:	execute basic testing cases 
            *
            * Parameter:	testCasesPerformed -- sum of all the test cases performed
            *			testCasePassed -- total number of test cases passed
            *
            * Return:		none
            */
            public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
            {
                Console.WriteLine("Running BasicValidationTestCases");

                GenericAce gAce = null;
                RawAcl rAcl = null;
                AceEnumerator myEnumerator = null;
                bool passed = false;

                //Case 1, collection from empty RawAcl			
                testCasesPerformed++;

                try
                {
                    passed = true;
                    Console.WriteLine("Begin collection from empty RawAcl case");
                    rAcl = new RawAcl(1, 1);
                    myEnumerator = rAcl.GetEnumerator();

                    passed = passed && TestAcessCurrentFailure(myEnumerator);

                    passed = passed && TestMoveNextFailure(myEnumerator);

                    myEnumerator.Reset();
                    passed = passed && TestAcessCurrentFailure(myEnumerator);

                    if (passed)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (ArgumentOutOfRangeException)
                {
                    Console.WriteLine("*******Bug225478*******");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 2, collection from RawAcl has one ACE
                testCasesPerformed++;

                try
                {
                    passed = true;

                    Console.WriteLine("Begin 1 Ace collection from RawAcl case");

                    rAcl = new RawAcl(0, 1);
                    gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
                    rAcl.InsertAce(0, gAce);
                    myEnumerator = rAcl.GetEnumerator();

                    passed = passed && TestAcessCurrentFailure(myEnumerator);
                    passed = passed && TestMoveNextSuccess(myEnumerator, true, rAcl[0]);
                    passed = passed && TestMoveNextFailure(myEnumerator);
                    myEnumerator.Reset();
                    passed = passed && TestAcessCurrentFailure(myEnumerator);
                    passed = passed && TestMoveNextSuccess(myEnumerator, true, rAcl[0]);
                    if (passed)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (ArgumentOutOfRangeException)
                {
                    Console.WriteLine("*******Bug225478*******");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }


                //case 3, collection from SystemAcl has one ACE
                testCasesPerformed++;

                try
                {
                    passed = true;

                    Console.WriteLine("Begin 1 Ace collection from SystemAcl case");

                    rAcl = new RawAcl(0, 1);
                    gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1,
                        new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
                    rAcl.InsertAce(0, gAce);
                    SystemAcl sAcl = new SystemAcl(true, false, rAcl);

                    myEnumerator = sAcl.GetEnumerator();

                    passed = passed && TestAcessCurrentFailure(myEnumerator);
                    passed = passed && TestMoveNextSuccess(myEnumerator, false, sAcl[0]);

                    //test for CommonAcl, Current return a clone instead of a reference
                    ((GenericAce)((IEnumerator)myEnumerator).Current).AceFlags = (AceFlags.Inherited | AceFlags.FailedAccess);

                    if (sAcl[0].AceFlags == (AceFlags.Inherited | AceFlags.FailedAccess))
                    {
                        Console.WriteLine("Ace returned by Current is not cloned");
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                        return;
                    }
                    else
                    {
                        Console.WriteLine("Test ace returned by Current is cloned passed");
                    }

                    passed = passed && TestMoveNextFailure(myEnumerator);
                    myEnumerator.Reset();
                    passed = passed && TestAcessCurrentFailure(myEnumerator);
                    passed = passed && TestMoveNextSuccess(myEnumerator, false, sAcl[0]);
                    if (passed)
                    {
                        Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
                        testCasesPassed++;
                    }
                    else
                        Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (ArgumentOutOfRangeException)
                {
                    Console.WriteLine("*******Bug225478*******");
                    Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception generated: " + e.Message, e);
                }
                finally
                {//make sure this is called even Current does not clone the Ace for CommonAcl
                }

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
                GenericAce gAce = null;
                RawAcl rAcl = null;
                AceEnumerator myEnumerator = null;
                int i = 0;
                bool passed = false;

                Console.WriteLine("Running AdditionalTestCases");

                //case 1, RawAcl with huge number of Aces
                testCasesPerformed++;

                try
                {
                    passed = true;
                    Console.WriteLine("Begin GenericAcl.MaxBinaryLength + 1 Aces collection from RawAcl case");
                    rAcl = new RawAcl(0, 1820);
                    for (i = 0; i < 1820; i++)
                    {
                        gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, i + 1,
                            new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
                        rAcl.InsertAce(0, gAce);
                    }

                    myEnumerator = rAcl.GetEnumerator();

                    passed = passed && TestAcessCurrentFailure(myEnumerator);
                    for (i = 0; i < rAcl.Count; i++)
                    {

                        gAce = null;
                        if (!myEnumerator.MoveNext())
                        {
                            passed = false;
                            Console.WriteLine("MoveNext should return true before passing the last");
                            break;
                        }
                        else
                        {
                            gAce = (GenericAce)((IEnumerator)myEnumerator).Current;

                            if (gAce != rAcl[i])
                            {
                                passed = false;
                                Console.WriteLine("Current Ace does not reference to the same Ace of the rawAcl");
                                break;
                            }

                        }
                    }

                    passed = passed && TestMoveNextFailure(myEnumerator);
                    myEnumerator.Reset();
                    passed = passed && TestAcessCurrentFailure(myEnumerator);
                    passed = passed && TestMoveNextSuccess(myEnumerator, true, rAcl[0]);

                    if (passed)
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



    }
}

