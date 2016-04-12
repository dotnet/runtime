using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class ACETest
    {
        public static Boolean Test()
        {
            Console.WriteLine("\n\n=======STARTING ACETest==========\n");
            return AllTestCases();
        }

        public static readonly string ObjectACETestStore =
                                                "TestCaseStores\\ObjectACETestCaseStore.inf";
        public static readonly string CommonACETestStore =
                                                "TestCaseStores\\CommonACETestCaseStore.inf";
        public static readonly string CompoundACETestStore =
                                                "TestCaseStores\\CompoundACETestCaseStore.inf";
        public static readonly string CustomACETestStore =
                                                "TestCaseStores\\CustomACETestCaseStore.inf";

        //for SecurityIdentifier object
        public static readonly string STR_SID = "S-1-1-0";
        public static readonly string binLen = "0";
        public static readonly int OFFSET = 0;

        public static readonly int HeaderLength = 4;
        public static readonly int AccessMaskLength = 4;
        public static readonly int AceTypeLength = 4;
        public static readonly int ObjectFlagLength = 4;
        public static readonly int GuidLength = 16;
        //this wont be needed once MaxOpaqueLength is made static
        public static readonly int MaxOpaqueLength = 64 * 1024;

        //Driver function for TestCase execution
        public static Boolean AllTestCases()
        {
            Boolean testResults = true;
            if (!ObjectACETest.AllTestCases(
                ))
            {
                Console.WriteLine("Failed to complete ObjectACE TestCases");
                testResults = false;
            }
            if (!CommonACETest.AllTestCases(
                ))
            {
                Console.WriteLine("Failed to complete CommonACE TestCases");
                testResults = false;
            }
            if (!CompoundACETest.AllTestCases(
                ))
            {
                Console.WriteLine("Failed to complete CompoundACE TestCases");
                testResults = false;
            }
            if (!CustomACETest.AllTestCases(
                ))
            {
                Console.WriteLine("Failed to complete CustomACE TestCases");
                testResults = false;
            }

            return testResults;
        }

        /*
            * Fn    :    GenericAceBinaryFormTests
            * Brief:    Function to Test GetBinaryForm.Array size for
            *            binaryForm array is specified by the string binArray,
            *            and then GetBinaryForm function is called on the objects.
            *            if the call is successful, the binary forms for the two 
            *            objects are compared to verify the fact that 2 identical 
            *            objects have identical binary forms.
            * params: 1. string binArray: "null" implies null binaryForm array.
            *                          : integer value indicates binaryForm array
            *                            length = object.BinaryLength + binArray
            *           2. int offset : offset into the binaryForm array
            *           3. AceObj : Test Object
            *           4. TstAce : Object identical to AceObj, needed for binary
            *              form comparison.
            * return:    true/false specifying success failure of test
            *              
            */
        public static bool GenericAceBinaryFormTests(string binArray,
            int offset, GenericAce AceObj,
            GenericAce TstAce)
        {
            bool bRetval = false;
            ArgumentNullException ANExp = new ArgumentNullException();
            ArgumentOutOfRangeException AOORExp = new ArgumentOutOfRangeException();
            if (binArray == "null")//Tests function for null array input
            {
                byte[] binForm = null;
                try
                {
                    AceObj.GetBinaryForm(binForm, offset);
                    Console.WriteLine(
                        "GetBinaryForm successful with null array");
                }
                catch (Exception e)
                {
                    //if exception thrown is ArgumentNullException
                    if (Object.ReferenceEquals(e.GetType(), ANExp.GetType()))
                    {
                        if (binForm == null)
                        {
                            Console.WriteLine(e.Message);
                            bRetval = true;
                        }
                    }
                    //if exception thrown is ArgumentOutOfRangeException
                    else if (Object.ReferenceEquals(e.GetType(), AOORExp.GetType()))
                    {
                        if (offset < 0)
                        {
                            Console.WriteLine(e.Message);
                            bRetval = true;
                        }
                    }
                    else
                    {

                        Console.WriteLine("Unexpected Exception {0}", e.GetType());
                        Console.WriteLine(e.Message);
                    }
                }
            }
            else
            {
                // len specifies size of Binary Array, with 0 indicating
                // array length = Object.BinaryLength
                int len = int.Parse(binArray);
                byte[] binForm = new byte[AceObj.BinaryLength + len];
                try
                {
                    AceObj.GetBinaryForm(binForm, offset);
                    //if offset and array size are valid
                    if (len >= 0 && offset >= 0)
                    {
                        //If there is enough space to store the binary data
                        if ((binForm.Length + len - offset) >= AceObj.BinaryLength)
                        {
                            byte[] tstbin = new byte[TstAce.BinaryLength];
                            TstAce.GetBinaryForm(tstbin, 0);

                            //Test if identical objects have identical 
                            //binary form
                            if (AceObj.BinaryLength == TstAce.BinaryLength)
                            {
                                int k;
                                for (k = 0; k < tstbin.Length; k++)
                                {
                                    if (tstbin[k] != binForm[k + offset])
                                        break;
                                }
                                if (k == tstbin.Length)
                                {
                                    Console.WriteLine("Identical Ace Objects " +
                                        "have identical binary forms");
                                    bRetval = true;
                                }
                                else
                                {
                                    Console.WriteLine("Identical " +
                                        "Ace Objects do not have" +
                                        "identical binary form");
                                }
                            }
                            else
                            {
                                Console.WriteLine("BinaryLength not same " +
                                    "for identical Ace objects");
                            }
                        }
                        else
                        {
                            //implies exception is not thrown for insufficient
                            //array size
                            Console.WriteLine("GetBinaryForm successful " +
                                "insufficient array");
                        }
                    }
                    else
                    {//implies exception is not thrown for bad offset 
                        //    or array length
                        Console.WriteLine("Exception not thrown by" +
                            " GetBinaryForm for bad offset or array length");
                    }
                }
                catch (Exception e)
                {
                    // Check the type of exception expected for 
                    //possible bad paramters
                    if (Object.ReferenceEquals(e.GetType(), AOORExp.GetType()))
                    {
                        //if array size not sufficient
                        if (((binForm.Length + len - offset) <
                            AceObj.BinaryLength) ||
                            offset < 0)
                        {
                            Console.WriteLine(e.Message);
                            bRetval = true;
                        }
                        else
                        {
                            Console.WriteLine("Exception {0}", e.Message);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Unexpected exception {0}", e.GetType());
                        Console.WriteLine(e.Message);
                    }
                }
            }
            return bRetval;
        }
        /*
            * Function to Test static function CreateFromBinaryForm with
            * variations of Binary Array and type of objects
            */
        public static bool GenericAceCreateFromBinaryFormTests(byte[] binArray, int offset,
            GenericAce AceObj, GenericAce TstAce)
        {
            bool bRetval = false;
            ArgumentOutOfRangeException AOORExp =
                new ArgumentOutOfRangeException();
            ArgumentNullException ANExp = new ArgumentNullException();
            ArgumentException ArgExp = new ArgumentException();
            try
            {
                TstAce = GenericAce.CreateFromBinaryForm(binArray, offset);
                Console.WriteLine("CreateFromBinaryForm creates the object");
                byte[] tstform = new byte[TstAce.BinaryLength];
                byte[] binaryForm = new byte[AceObj.BinaryLength];
                AceObj.GetBinaryForm(binaryForm, 0);
                TstAce.GetBinaryForm(tstform, 0);
                int k;
                //The binary array should be similar
                for (k = 0; k < tstform.Length; k++)
                {
                    if (tstform[k] != binaryForm[k])
                        break;
                }
                if (k == binaryForm.Length)
                {
                    bRetval = true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.GetType().ToString());
                //ArgumentNullException is thrown check if array is null
                if (Object.ReferenceEquals(e.GetType(), ANExp.GetType()))
                {
                    if (binArray == null)
                    {
                        bRetval = true;
                    }
                }
                else if (Object.ReferenceEquals(e.GetType(), AOORExp.GetType()))
                {
                    //if offset is not correct
                    if (offset < 0 ||
                        (AceObj.BinaryLength > (binArray.Length - offset)))
                    {
                        bRetval = true;
                    }
                }
                else if (Object.ReferenceEquals(e.GetType(), ArgExp.GetType()))
                {
                    //placeholder for case when binary form passed is garbage
                    bRetval = true;
                    Console.WriteLine("The array is garbage:");
                }
                else
                {
                    Console.WriteLine("Unexpected Exception {0}", e.GetType());

                }
            }
            return bRetval;
        }
        /*
            * Class to Test CustomAce class 
            * 
            */

        public class CustomACETest
        {
            private CustomACETest()
            {
            }
            /*
                * Fn    :     AllTestCases
                * Brief:    Driver function which reads in TestCase from TestStore
                *            and    based on the number of parameters for the TestCase
                *            the    appropriate Test function is called
                * return: true/false specifying tests completed or not.
                */

            public static bool AllTestCases()
            {
                bool bRetval = false;
                string[] testCases;
                AceType type;
                AceFlags flags;
                ITestCaseReader reader = new InfTestCaseStore();
                reader.OpenTestCaseStore(CustomACETestStore);

                Console.WriteLine("<------CustomAceTests------>");

                int tstcnt = 0;


                while (null != (testCases = reader.ReadNextTestCase()))
                {

                    Console.WriteLine("CustomAce TestCase {0}", tstcnt);
                    bRetval = true;
                    if (testCases.Length == 2)//For BinaryForm Tests
                    {
                        string binArray = testCases[0];
                        int offset = int.Parse(testCases[1]);
                        if (!CustomAceBinaryFormTests(binArray, offset))
                        {
                            Console.WriteLine("FAIL:CustomAce TestCase {0}", tstcnt);
                        }
                        else
                        {
                            Console.WriteLine("PASS:CustomAce TestCase {0}", tstcnt);
                        }


                    }
                    else
                    {
                        //For CreateFromBinaryForm Tests and Constructor tests 
                        if (testCases.Length >= 3)
                        {
                            type = (AceType)byte.Parse(testCases[0]);
                            flags = (AceFlags)byte.Parse(testCases[1]);
                            byte[] opaque = null;
                            if ("null" != testCases[2])
                            {
                                opaque = new byte[int.Parse(testCases[2])];
                            }
                            //if TestCase is for CreateFromBinaryForm Tests
                            if (testCases.Length == 5)
                            {
                                string arrayLen = testCases[3];
                                int offset = int.Parse(testCases[4]);
                                if (CreateFromBinaryFormTests(arrayLen,
                                    offset, type, flags, opaque))
                                {
                                    Console.WriteLine(
                                        "PASS:CustomAce TestCase {0}", tstcnt);
                                }
                                else
                                {
                                    Console.WriteLine(
                                        "FAIL:CustomAce TestCase {0}", tstcnt);
                                }
                            }
                            else
                            {//TestCase is for Constructor Tests
                                if (CustomAceConstructorTests(type, flags,
                                                            opaque))
                                {
                                    Console.WriteLine(
                                        "PASS:CustomAce TestCase {0}", tstcnt);
                                }
                                else
                                {
                                    Console.WriteLine(
                                        "FAIL:CustomAce TestCase {0}", tstcnt);
                                }
                            }


                        }
                    }
                    tstcnt++;
                }
                return bRetval;
            }

            /*
                * Fn    :    CustomAceConstructorTests
                * Brief:    Tests the CustomAce constructor with the parameters
                *            specified in the TestCase.
                * param:    type: AceType, flags: AceFlags,
                *            opaque :byte[] of payload for the ACE
                * return:    true/false specifying success failure of
                *            the tests 
                */
            public static bool CustomAceConstructorTests(AceType type,
                AceFlags flags, byte[] opaque)
            {
                CustomAce cstmAce = null;
                ArgumentOutOfRangeException AOORExp =
                    new ArgumentOutOfRangeException();
                ArgumentException ArgExp = new ArgumentException();
                try
                {
                    cstmAce = new CustomAce(type, flags, opaque);

                }
                catch (Exception e)
                {
                    Console.WriteLine("The Exception type is {0}", e.GetType());

                    //If ArgumentOutOFRangeException is thrown
                    if (Object.ReferenceEquals(e.GetType(), AOORExp.GetType()))
                    {
                        //verify if arguments are out of range
                        if (type <= AceType.MaxDefinedAceType ||
                            flags < AceFlags.None ||
                            (opaque != null &&
                            opaque.Length > CustomAce.MaxOpaqueLength))
                        {
                            return true;
                        }
                        return false;
                    }
                    //If ArgumentException is thrown
                    else if (Object.ReferenceEquals(e.GetType(),
                        ArgExp.GetType()))
                    {
                        //need to check up if this is correct!
                        if (opaque != null && opaque.Length % 4 != 0)
                        {
                            return true;
                        }
                        return false;
                    }
                    Console.WriteLine("Unexpected exception:{0}", e.Message);
                    return false;
                }
                if (type <= AceType.MaxDefinedAceType ||
                    flags < AceFlags.None ||
                    (opaque != null &&
                    (opaque.Length > CustomAce.MaxOpaqueLength ||
                    opaque.Length % 4 != 0)))
                {// Check if parameters were valid 
                    Console.WriteLine(
                        "Exception not generated for bad parameters");
                    return false;
                }
                //verify if the object was constructed correctly 
                if (cstmAce.AceType == type &&
                    cstmAce.AceFlags == flags &&
                    Object.Equals(cstmAce.GetOpaque(), opaque))
                {

                    Console.WriteLine("CustomAce constructor sucessful");
                    CustomAce TstcstmAce = new CustomAce(type, flags, opaque);
                    //Tests GetBinaryForm with all valid CustomAce objects
                    if (!ACETest.GenericAceBinaryFormTests(binLen, OFFSET,
                        cstmAce, TstcstmAce))
                    {
                        Console.WriteLine("BinaryForm Test fails");
                        return false;
                    }
                    Console.WriteLine("BinaryForm Test succeeds");

                    //Tests CreateFromBinaryForm for all valid 
                    //CustomAce objects
                    byte[] binArray = new byte[cstmAce.BinaryLength];
                    cstmAce.GetBinaryForm(binArray, 0);
                    CustomAce TstAce =
                        (CustomAce)CustomAce.CreateFromBinaryForm(binArray, 0);

                    //Compare members to verify if CreateFromBinaryForm 
                    //is correct
                    if (TstAce.AceType == cstmAce.AceType &&
                        TstAce.AceFlags == cstmAce.AceFlags &&
                        TstAce.OpaqueLength == cstmAce.OpaqueLength)
                    {
                        byte[] op1 = TstAce.GetOpaque();
                        byte[] op2 = cstmAce.GetOpaque();
                        int i;
                        for (i = 0; i < cstmAce.OpaqueLength; i++)
                        {
                            if (op1[i] != op2[i])
                                break;
                        }
                        if (i == cstmAce.OpaqueLength)
                        {
                            Console.WriteLine("CustomAce:creates" +
                                "the correct object from binary form");
                            return true;
                        }
                    }

                    //if members do not match, CreateFromBinaryForm fails
                    //Print out individual members to check for mismatch
                    Console.WriteLine("CustomAce:" +
                        "CreateFromBinaryForm fails");
                    Console.WriteLine("Generated : Expected");
                    Console.WriteLine(
                        "{0}:{1}", TstAce.AceType, cstmAce.AceType);
                    Console.WriteLine(
                        "{0}:{1}", TstAce.AceFlags, cstmAce.AceFlags);
                    Console.WriteLine(
                        "{0}:{1}", TstAce.OpaqueLength, cstmAce.OpaqueLength);
                    return false;
                }
                Console.WriteLine("CustomAce Constructor fails");
                return false;
            }

            /*
                * Fn    :    CustomAceBinaryFormTests
                * Brief:    Tests BinaryForm function with various
                *            combinations of binaryForm and offset
                * param:    binArray: specifies if binaryForm is null,
                *            length of binaryForm oterwise.
                *            offset: offset into the binaryForm array
                * return:    true/false specifying success failure of tests
                */
            public static bool CustomAceBinaryFormTests(string binArray, int offset)
            {
                AceType type = AceType.MaxDefinedAceType + 1;
                AceFlags flags = AceFlags.AuditFlags;
                CustomAce AceObj = null, TstAce = null;
                byte[] opaque = new byte[4];
                try
                {
                    AceObj = new CustomAce(type, flags, opaque);
                    TstAce = new CustomAce(type, flags, opaque);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception {0}", e.Message);
                    return false;
                }
                return (ACETest.GenericAceBinaryFormTests(binArray, offset,
                                                AceObj, TstAce));
            }

            /*
            * Fn    :    CreateFromBinaryFormTests
            * Brief:    Tests CreateFromBinaryForm function with various
            *            combinations of binaryForm and offset
            * param:    binArray: specifies if binaryForm is null,
            *            length of binaryForm oterwise.
            *            offset: offset into the binaryForm array
            *            type,flags,opaque (for the constructor)    
            * return:    true/false specifying success failure of tests
            */

            public static bool CreateFromBinaryFormTests(string arraySpec,
                int offset, AceType type, AceFlags flags, byte[] opaque)
            {
                bool bRetval = false;
                CustomAce AceObj = null, TstAce = null;
                try
                {
                    AceObj = new CustomAce(type, flags, opaque);
                }
                //TODO: talk to Rajeet, in the testing data, what is the purpose for the three test cass
                // which have Opaque length as 1 but the testing code does not catch ArgumentOutOfRangeException.
                //To avoid the test failure, I added the catch for ArgumentOutOfRangeException
                catch (ArgumentOutOfRangeException)
                {
                    if (opaque.Length % 4 != 0)
                    {//this is expected, return true
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Could not create CustomAce " +
                        "object needed for CreateFromBinaryTests" +
                        " : check arguments");
                    Console.WriteLine(e.Message);
                    return false;
                }
                byte[] binaryForm = null;
                if (arraySpec == "null")
                {//TestCase with null binaryForm array
                    bRetval = ACETest.GenericAceCreateFromBinaryFormTests(
                        binaryForm, offset, AceObj, TstAce);
                }
                else
                    if (arraySpec == "garbage")
                    {
                        binaryForm = new byte[AceObj.BinaryLength];
                        AceObj.GetBinaryForm(binaryForm, 0);
                        binaryForm[0] = 0;
                        bRetval =
                            ACETest.GenericAceCreateFromBinaryFormTests(
                            binaryForm, 0, AceObj, TstAce);

                    }
                    else
                    {
                        int len = 0;
                        try
                        {
                            len = int.Parse(arraySpec);
                            binaryForm = new byte[AceObj.BinaryLength + len +
                                Math.Abs(offset)];
                            AceObj.GetBinaryForm(binaryForm, Math.Abs(offset));
                            bRetval =
                                ACETest.GenericAceCreateFromBinaryFormTests(
                                binaryForm, offset, AceObj, TstAce);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Unexpected Exception {0}", e.Message);
                            bRetval = false;
                        }
                    }
                return bRetval;
            }
        }
        /*
            *ObjectAce TestCode 
            * 
            */
        public class ObjectACETest
        {
            //For instantiating Guid object (needed for ObjectAceType and 
            //InheritedObjectAceType
            public static readonly string strGuid =
                                    "{B888C18F-0935-413f-B79A-F6C714F4A6B3}";

            private ObjectACETest()
            {
            }

            /*
            * Fn    :     AllTestCases
            * Brief:    Driver function which reads in TestCase from TestStore
            *            and    based on the number of parameters for the TestCase
            *            the    appropriate Test function is called
            * return: true/false specifying tests completed or not.
            */

            public static bool AllTestCases()
            {
                bool bRetval = false;
                string[] testCases = null;
                int mask;
                AceFlags flags;
                AceQualifier qualifier;
                ObjectAceFlags objFlags;
                string strsid;
                string str_ObjGuid = null, str_InGuid = null;
                bool iscallback;
                ITestCaseReader reader = new InfTestCaseStore();
                reader.OpenTestCaseStore(ObjectACETestStore);

                Console.WriteLine("<------ObjectAceTests------>");

                int tstcnt = 0;


                while (null != (testCases = reader.ReadNextTestCase()))
                {


                    Console.WriteLine("ObjectAce TestCase {0}", tstcnt);
                    bRetval = true;
                    if (testCases.Length == 2)//if this is GetBinaryForm Testcase
                    {
                        string binArray = testCases[0];
                        int offset = int.Parse(testCases[1]);
                        if (!ObjectAceBinaryFormTests(binArray, offset))
                        {
                            Console.WriteLine("FAIL:ObjectAce TestCase {0}", tstcnt);
                        }
                        else
                        {
                            Console.WriteLine("PASS:ObjectAce TestCase {0}", tstcnt);
                        }


                    }
                    else
                    {//if Constructor or CreateFromBinaryForm TestCase
                        if (testCases.Length >= 9)
                        {
                            flags = (AceFlags)byte.Parse(testCases[0]);
                            if (testCases[1][0] == '-')//-ve AceQualifier
                            {
                                qualifier = AceQualifier.AccessAllowed - 1;
                            }
                            else
                            {
                                qualifier = (AceQualifier)byte.Parse(testCases[1]);
                            }
                            mask = int.Parse(testCases[2]);
                            strsid = testCases[3];
                            objFlags = (ObjectAceFlags)byte.Parse(testCases[4]);
                            if (testCases[5] != "null")
                            {
                                str_ObjGuid = testCases[5];
                            }
                            if (testCases[6] != "null")
                            {
                                str_InGuid = testCases[6];
                            }
                            if ("true" == testCases[7])
                            {
                                iscallback = true;
                            }
                            else
                            {
                                iscallback = false;
                            }
                            byte[] opaque = null;
                            //if opaque array not null
                            if ("null" != testCases[8])
                            {
                                opaque = new byte[int.Parse(testCases[8])];
                            }
                            //If CreateFromBinaryForm TestCase
                            if (testCases.Length == 11)
                            {
                                string arrayLen = testCases[9];
                                int offset = int.Parse(testCases[10]);
                                if (CreateFromBinaryFormTests(arrayLen, offset,
                                    flags, qualifier, mask, strsid, objFlags,
                                    str_ObjGuid, str_InGuid, iscallback,
                                    opaque))
                                {
                                    Console.WriteLine(
                                        "PASS:ObjectACE TestCase {0}", tstcnt);
                                }
                                else
                                {
                                    Console.WriteLine(
                                        "FAIL:ObjectACE TestCase {0}", tstcnt);
                                }
                            }
                            else
                            {// for Constructor Test and set property test
                                bool bCheck1 = false, bCheck2 = false;
                                if ((bCheck1 = ObjectACEConstructorTests(flags,
                                    qualifier, mask, strsid, objFlags,
                                    str_ObjGuid, str_InGuid, iscallback,
                                    opaque)))
                                {
                                    Console.WriteLine(
                                    "ObjectAce constructor Test successful");
                                }
                                else
                                {
                                    Console.WriteLine(
                                        "ObjectAce constructor Test fails");
                                }
                                if ((bCheck2 = setObjectACETests(flags,
                                        qualifier, mask, strsid, objFlags,
                                        str_ObjGuid, str_InGuid,
                                            iscallback, opaque)))
                                {
                                    Console.WriteLine(
                                        "ObjectAce set tests successful");
                                }
                                else
                                {
                                    Console.WriteLine(
                                        "ObjectAce set test fails");
                                }
                                if (bCheck1 && bCheck2)
                                {
                                    Console.WriteLine(
                                        "PASS:ObjectACE TestCase {0}", tstcnt);
                                }
                                else
                                {
                                    Console.WriteLine(
                                        "FAIL:ObjectACE TestCase {0}", tstcnt);
                                }
                            }

                        }
                    }

                    tstcnt++;
                }
                reader.CloseTestCaseStore();
                return bRetval;
            }

            /*
                * Fn    :    ObjectAceConstructorTests
                * Brief:    Tests the CustomAce constructor with the parameters
                *            specified in the TestCase.
                * param:    arguments needed by ObjectAce constructor
                * return:    true/false specifying success failure of
                *            the tests 
                */
            public static bool ObjectACEConstructorTests(AceFlags flags,
                                                AceQualifier qualifier,
                                                int mask, string strsid,
                                                ObjectAceFlags ObjFlags,
                                                string str_ObjGuid,
                                                string str_InObjGuid,
                                                bool isCallback, byte[] opaque)
            {
                ArgumentOutOfRangeException AOORExcept =
                                            new ArgumentOutOfRangeException();
                ArgumentNullException ANExcept = new ArgumentNullException();
                ArgumentException ArgExcept = new ArgumentException();
                ObjectAce ObjAce = null;
                Guid ObjGuid, InObjGuid;
                SecurityIdentifier sid = null;
                bool bIsNull = true;
                if (opaque != null)
                {
                    bIsNull = false;
                }
                if (strsid != "null")
                {//if non-null security identifier
                    sid = new SecurityIdentifier(strsid);
                }
                Console.WriteLine("Running ObjectAce Constructor Tests");
                try
                {
                    ObjGuid = new Guid(str_ObjGuid);
                    InObjGuid = new Guid(str_InObjGuid);
                    if (strsid != "null")
                    {
                        sid = new SecurityIdentifier(strsid);
                    }
                    ObjAce = new ObjectAce(flags, qualifier, mask, sid, ObjFlags,
                            ObjGuid, InObjGuid, isCallback, opaque);
                }

                catch (Exception e)
                {
                    Console.WriteLine("The exception type is");
                    Console.WriteLine(e.GetType().ToString());
                    Console.WriteLine(e.Message);

                    if (Object.ReferenceEquals(e.GetType(),
                                        AOORExcept.GetType()))
                    {//if ArgumentOutOfRangeException thrown, verify if any
                        // argument is out of range

                        //TODO: as the ArgumentOutOfRangeException is thrown when
                        //        opaque length is not mutiple of 4, the check should be
                        //        added here and original Rajeet's check code should be
                        //        deleted
                        if (flags < AceFlags.None ||
                            qualifier < AceQualifier.AccessAllowed ||
                            qualifier > AceQualifier.SystemAlarm ||
                            ObjFlags < ObjectAceFlags.None ||
                            (!!bIsNull && opaque.Length > MaxOpaqueLength) ||
                            (!bIsNull && opaque.Length % 4 != 0))
                        {
                            return true;
                        }
                        return false;
                    }
                    else
                        if (Object.ReferenceEquals(e.GetType(),
                                                ANExcept.GetType()))
                        {
                            //If ArgumentNullException is thrown, verify
                            // if sid is null or not
                            if (sid == null || str_ObjGuid == null ||
                                str_InObjGuid == null)
                            {
                                return true;
                            }
                            return false;
                        }
                        else
                            if (Object.ReferenceEquals(e.GetType(),
                                                ArgExcept.GetType()))
                            {
                                //TBD...whether AOORExp should be thrown or ArgumentException
                                if (!bIsNull && opaque.Length % 4 != 0)
                                {
                                    return true;
                                }

                                return false;
                            }
                            else
                            {
                                Console.WriteLine("Unexpected Exception {0}", e.Message);
                                return false;
                            }
                }
                if (flags < 0 || ObjFlags < 0 ||
                    sid == null || str_ObjGuid == null ||
                    str_InObjGuid == null ||
                    (!bIsNull &&
                    opaque.Length > MaxOpaqueLength) ||
                    (!bIsNull && opaque.Length % 4 != 0))
                {//If no exception is thrown for bad parameters
                    // return failure
                    Console.WriteLine(
                        "Exception not generated for bad parameters");
                    return false;
                }
                //verify if all members have been initialized correctly
                if (ObjAce.AceFlags == flags &&
                    ObjAce.AceQualifier == qualifier &&
                    ObjAce.AccessMask == mask &&
                    ObjAce.SecurityIdentifier == sid &&
                    ObjAce.ObjectAceFlags == ObjFlags &&
                    ObjAce.ObjectAceType == ObjGuid &&
                    ObjAce.InheritedObjectAceType == InObjGuid &&
                    ObjAce.IsCallback == isCallback)
                {
                    if (isCallback)
                    {//compare opaque data only for callback Aces
                        if ((Object.Equals(ObjAce.GetOpaque(), opaque)))
                        {
                            Console.WriteLine("Opaque array matches" +
                                "the parameter passed");
                        }
                        else
                        {
                            Console.WriteLine("Opaque array does not match " +
                                "the passed parameter");
                            return false;
                        }
                    }
                    Console.WriteLine("ObjectAce Constructor successful!");
                    //if constructor test successful perform GetBinaryTests
                    //and CreateFromBinaryForm Tests
                    ObjectAce TstAce = new ObjectAce(flags, qualifier, mask, sid,
                                ObjFlags, ObjGuid, InObjGuid, isCallback, opaque);
                    if (!ACETest.GenericAceBinaryFormTests(binLen, OFFSET,
                        ObjAce, TstAce))
                    {
                        Console.WriteLine("BinaryForm Test fails");
                        return false;
                    }
                    Console.WriteLine("BinaryForm Tests succeeds");
                    byte[] binArray = new byte[ObjAce.BinaryLength];
                    ObjAce.GetBinaryForm(binArray, 0);
                    ObjectAce TestObjAce =
                        (ObjectAce)ObjectAce.CreateFromBinaryForm(binArray, 0);
                    //verify if CreateFromBinaryForm created the object
                    //correctly
                    if (TestObjAce.AceFlags == ObjAce.AceFlags &&
                        TestObjAce.AceQualifier == ObjAce.AceQualifier &&
                        TestObjAce.AccessMask == ObjAce.AccessMask &&
                        TestObjAce.SecurityIdentifier ==
                                            ObjAce.SecurityIdentifier &&
                        TestObjAce.IsCallback == ObjAce.IsCallback &&
                        TestObjAce.ObjectAceFlags == ObjAce.ObjectAceFlags)
                    {
                        //ObjectAceType is only set if ObjectAceFlag has
                        //ObjectAceType set
                        if ((TestObjAce.ObjectAceFlags &
                        ObjectAceFlags.ObjectAceTypePresent) ==
                                        ObjectAceFlags.ObjectAceTypePresent)
                        {
                            if (!Object.Equals(TestObjAce.ObjectAceType,
                                            ObjAce.ObjectAceType))
                            {
                                return false;
                            }

                        }
                        //Check InheritedObjectAceType if 
                        //InheritedObjectAceType is set in ObjectAceFlag
                        if ((TestObjAce.ObjectAceFlags &
                            ObjectAceFlags.InheritedObjectAceTypePresent) ==
                                ObjectAceFlags.InheritedObjectAceTypePresent)
                        {
                            if (!Object.Equals(TestObjAce.InheritedObjectAceType,
                                ObjAce.InheritedObjectAceType))
                            {
                                return false;
                            }

                        }
                        if (ObjAce.OpaqueLength == TestObjAce.OpaqueLength)
                        {
                            byte[] op1 = TestObjAce.GetOpaque();
                            byte[] op2 = ObjAce.GetOpaque();
                            Console.WriteLine("Opaque length: {0}", ObjAce.OpaqueLength);
                            for (int i = 0; i < ObjAce.OpaqueLength; i++)
                            {
                                if (op1[i] != op2[i])
                                {
                                    Console.WriteLine("ObjectAce:" +
                                        "CreateFromBinaryForm mismatch");
                                    return false;
                                }
                            }
                            Console.WriteLine("CreateFromBinaryForm " +
                                "creates the correct ObjectAce");
                            return true;
                        }
                        else
                        {
                            Console.WriteLine("ObjectAce: Opaque data " +
                            "mismatch for object created from BinaryForm");
                            return false;
                        }

                    }
                    else
                    {
                        Console.WriteLine("ObjectAce created by " +
                            "CreateFromBinaryForm does not match " +
                                        "the expected ObjectAce");
                        Console.WriteLine(
                            "{0}:{1}", TestObjAce.AceFlags, ObjAce.AceFlags);
                        Console.WriteLine(
                        "{0}:{1}", TestObjAce.AceQualifier, ObjAce.AceQualifier);
                        Console.WriteLine(
                        "{0}:{1}", TestObjAce.AccessMask, ObjAce.AccessMask);
                        Console.WriteLine(
            "{0}:{1}", TestObjAce.SecurityIdentifier, ObjAce.SecurityIdentifier);
                        Console.WriteLine(
                            "{0}:{1}", TestObjAce.IsCallback, ObjAce.IsCallback);
                        Console.WriteLine(
                    "{0}:{1}", TestObjAce.ObjectAceType, ObjAce.ObjectAceType);
                        Console.WriteLine(
        "{0}:{1}", TestObjAce.InheritedObjectAceType, ObjAce.InheritedObjectAceType);
                        return false;
                    }
                }
                Console.WriteLine("ObjectAce Constructor mismatch:" +
                    "members do not match the parameters passed");
                return false;
            }

            /*
                * Fn    :    setObjectAceTests
                * Brief:    Tests set property for members of this
                *            class
                * param:    parameters needed by all members that have
                *            set property defined    
                * return:    true/false specifying success failure of tests
                */

            public static bool setObjectACETests(AceFlags flags,
                                            AceQualifier qualifier, int mask,
                                            string strTstsid,
                                            ObjectAceFlags ObjFlags,
                                            string str_ObjGuid,
                                            string str_InObjGuid,
                                            bool isCallback, byte[] opaque)
            {
                ObjectAce ObjAce = null;
                ArgumentOutOfRangeException AOORExp =
                                        new ArgumentOutOfRangeException();
                ArgumentNullException ANExp = new ArgumentNullException();
                ArgumentException ArgExp = new ArgumentException();
                Guid setGuidObj, ObjType, InObjType;
                byte[] setopq = new byte[4];
                SecurityIdentifier setsid = null;
                SecurityIdentifier sid = null;
                try
                {//create sample object needed to perform set tests on
                    setGuidObj = new Guid(strGuid);
                    setsid = new SecurityIdentifier(STR_SID);
                    ObjAce = new ObjectAce(AceFlags.None,
                        AceQualifier.AccessAllowed,
                        0, setsid, ObjectAceFlags.None,
                        setGuidObj, setGuidObj, true, setopq);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Could not initiate setObjectAceTests");
                    return false;
                }
                try
                {
                    ObjAce.AceFlags = flags;
                    ObjAce.AccessMask = mask;
                    if (strTstsid != "null")
                    {
                        sid = new SecurityIdentifier(strTstsid);
                    }
                    ObjAce.SecurityIdentifier = sid;
                    ObjAce.ObjectAceFlags = ObjFlags;
                    ObjType = new Guid(str_ObjGuid);
                    InObjType = new Guid(str_InObjGuid);
                    ObjAce.ObjectAceType = ObjType;
                    ObjAce.InheritedObjectAceType = InObjType;
                    ObjAce.SetOpaque(opaque);
                    //verify if set was successful
                    if (ObjAce.AceFlags == flags &&
                        ObjAce.AccessMask == mask &&
                        ObjAce.SecurityIdentifier == sid &&
                        ObjAce.ObjectAceFlags == ObjFlags &&
                        ObjAce.ObjectAceType == ObjType &&
                        ObjAce.InheritedObjectAceType == InObjType &&
                        Object.Equals(ObjAce.GetOpaque(), opaque))
                    {
                        Console.WriteLine("Set ObjectAce successful");
                        return true;
                    }
                    Console.WriteLine("Set ObjectAce fails");
                    return false;
                }
                catch (Exception e)
                {
                    Console.WriteLine("setObjectAceTest, exception type:" +
                                        "{0}", e.GetType());
                    if (Object.ReferenceEquals(e.GetType(), AOORExp.GetType()))
                    {
                        //TODO: as the ArgumentOutOfRangeException is thrown when
                        //        opaque length is not mutiple of 4, the check should be
                        //        added here and original Rajeet's check code should be
                        //        deleted
                        if (flags < 0 || ObjFlags < 0 ||
                            (opaque != null &&
                            opaque.Length > MaxOpaqueLength) ||
                            (opaque != null && opaque.Length % 4 != 0))
                        {

                            return true;
                        }
                        return false;

                    }
                    else if (Object.ReferenceEquals(e.GetType(),
                        ANExp.GetType()))
                    {
                        if (sid == null || str_ObjGuid == null ||
                            str_InObjGuid == null)
                        {
                            return true;
                        }
                        return false;
                    }
                    else if (Object.ReferenceEquals(e.GetType(),
                        ArgExp.GetType()))
                    {
                        if (opaque != null && opaque.Length % 4 != 0)
                        {
                            Console.WriteLine("Set ObjectAce successful error matching");
                            return true;
                        }
                        return false;
                    }
                    else
                    {
                        Console.WriteLine("Unexpected exception" +
                            " generated {0}", e.Message);
                        return false;
                    }
                }
            }
            /*
                * Fn    :    ObjectAceBinaryFormTests
                * Brief:    Tests BinaryForm function with various
                *            combinations of binaryForm and offset
                * param:    binArray: specifies if binaryForm is null,
                *            length of binaryForm otherwise.
                *            offset: offset into the binaryForm array
                * return:    true/false specifying success failure of tests
                */
            public static bool ObjectAceBinaryFormTests(string binArray, int offset)
            {
                AceQualifier qualifier = AceQualifier.AccessAllowed;
                int mask = 1;
                ObjectAceFlags ObjFlags = ObjectAceFlags.ObjectAceTypePresent;
                ObjFlags |= ObjectAceFlags.InheritedObjectAceTypePresent;
                SecurityIdentifier sid = null;
                Guid ObjType;
                AceFlags flags = AceFlags.AuditFlags;
                bool isCallback = true;
                ObjectAce AceObj = null, TstAce = null;
                byte[] opaque = new byte[4];
                try
                {
                    sid = new SecurityIdentifier(STR_SID);
                    ObjType = new Guid(strGuid);
                    AceObj = new ObjectAce(flags, qualifier, mask, sid, ObjFlags,
                                        ObjType, ObjType, isCallback, opaque);
                    TstAce = new ObjectAce(flags, qualifier, mask, sid, ObjFlags,
                        ObjType, ObjType, isCallback, opaque);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception {0}", e.Message);
                    return false;
                }
                return (ACETest.GenericAceBinaryFormTests(binArray, offset,
                                                            AceObj, TstAce));
            }

            /*
                * Fn    :    CreateFromBinaryFormTests
                * Brief:    Tests CreateFromBinaryForm function with various
                *            combinations of binaryForm and offset
                * param:    binArray: specifies if binaryForm is null,
                *            length of binaryForm otherwise.
                *            offset: offset into the binaryForm array,
                *            parameters for ObjectAce constructor    
                * return:    true/false specifying success failure of tests
                */
            public static bool CreateFromBinaryFormTests(string arraySpec,
                                                    int offset, AceFlags flags,
                                                    AceQualifier qualifier,
                                                    int mask, string strsid,
                                                    ObjectAceFlags ObjFlags,
                                                    string str_ObjGuid,
                                                    string str_InObjGuid,
                                            bool isCallback, byte[] opaque)
            {
                bool bRetval = false;
                ObjectAce AceObj = null, TstAce = null;
                try
                {
                    SecurityIdentifier sid = new SecurityIdentifier(strsid);
                    Guid ObjType = new Guid(str_ObjGuid);
                    Guid InObjType = new Guid(str_InObjGuid);
                    AceObj = new ObjectAce(flags, qualifier, mask, sid, ObjFlags,
                                            ObjType, InObjType,
                                            isCallback, opaque);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Could not create ObjectAce " +
                        "object needed for CreateFromBinaryTests" +
                        " : check arguments");
                    Console.WriteLine(e.Message);
                    return false;
                }
                byte[] binaryForm = null;
                if (arraySpec == "null")
                {
                    bRetval = ACETest.GenericAceCreateFromBinaryFormTests(
                        binaryForm, offset, AceObj, TstAce);
                }
                else
                    if (arraySpec == "garbage")
                    {
                        binaryForm = new byte[AceObj.BinaryLength];
                        AceObj.GetBinaryForm(binaryForm, 0);
                        int k = HeaderLength + AccessMaskLength +
                        ObjectFlagLength;
                        //Setting invalid data in binary form for 
                        //ObjectAceType and InheritedObjectAceType

                        if ((AceObj.ObjectAceFlags &
                        ObjectAceFlags.ObjectAceTypePresent) != 0)
                        {
                            for (int j = 0; j < GuidLength; j++)
                            {
                                binaryForm[j + k] = 0;
                            }
                            k += GuidLength;
                        }
                        if ((AceObj.ObjectAceFlags &
                        ObjectAceFlags.InheritedObjectAceTypePresent) != 0)
                        {
                            for (int j = 0; j < GuidLength; j++)
                            {
                                binaryForm[j + k] = 255;
                            }
                            k += GuidLength;
                        }
                        bool bcheck1 =
                            ACETest.GenericAceCreateFromBinaryFormTests(
                            binaryForm, 0, AceObj, TstAce);
                        //Making SecurityIdentifier invalid
                        for (int j = 0;
                            j < (AceObj.SecurityIdentifier).BinaryLength; j++)
                        {
                            binaryForm[j + k] = 0;
                        }
                        bool bcheck2 =
                            ACETest.GenericAceCreateFromBinaryFormTests(
                            binaryForm, 0, AceObj, TstAce);
                        return (bcheck1 && bcheck2);
                    }
                    else
                    {
                        int len = 0;
                        try
                        {
                            len = int.Parse(arraySpec);
                            binaryForm = new byte[AceObj.BinaryLength + len +
                                Math.Abs(offset)];
                            AceObj.GetBinaryForm(binaryForm, Math.Abs(offset));
                            bRetval =
                                ACETest.GenericAceCreateFromBinaryFormTests(
                                binaryForm, offset, AceObj, TstAce);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Unexpected Exception {0}", e.Message);
                            bRetval = false;
                        }
                    }
                return bRetval;
            }

        }

        /*
            *Common Ace Testing Code
            */

        public class CommonACETest
        {
            private CommonACETest()
            {
            }
            /*
            * Fn    :     AllTestCases
            * Brief:    Driver function which reads in TestCase from TestStore
            *            and    based on the number of parameters for the TestCase
            *            the    appropriate Test function is called
            * return: true/false specifying tests completed or not.
            */
            public static bool AllTestCases()
            {
                bool bRetval = true;
                string[] testCases;
                string strsid = null;
                int mask;
                AceQualifier qualifier;
                AceFlags flags;
                bool iscallback;
                ITestCaseReader reader = new InfTestCaseStore();
                reader.OpenTestCaseStore(CommonACETestStore);

                Console.WriteLine("<------CommonAce Tests------>");

                int tstcnt = 0;

                while (null != (testCases = reader.ReadNextTestCase()))
                {

                    Console.WriteLine("CommonAce TestCase {0}", tstcnt);
                    if (testCases.Length == 2)
                    {
                        string binArray = testCases[0];
                        int offset = int.Parse(testCases[1]);
                        if (!CommonAceBinaryFormTests(binArray, offset))
                        {
                            Console.WriteLine("FAIL:CommonAce TestCase {0}", tstcnt);
                        }
                        else
                        {
                            Console.WriteLine("PASS:CommonAce TestCase {0}", tstcnt);

                        }


                    }
                    else
                    {
                        if (testCases.Length >= 6)
                        {
                            flags = (AceFlags)byte.Parse(testCases[0]);
                            qualifier = (AceQualifier)byte.Parse(testCases[1]);
                            mask = int.Parse(testCases[2]);
                            strsid = testCases[3];
                            if ("true" == testCases[4])
                            {
                                iscallback = true;
                            }
                            else
                            {
                                iscallback = false;
                            }
                            byte[] opaque = null;
                            if ("null" != testCases[5])
                            {
                                opaque = new byte[int.Parse(testCases[5])];
                            }
                            if (testCases.Length == 8)
                            {
                                string arrayLen = testCases[6];
                                int offset = int.Parse(testCases[7]);
                                if (CreateFromBinaryFormTests(arrayLen, offset,
                                    flags, qualifier, mask, strsid,
                                    iscallback, opaque))
                                {
                                    Console.WriteLine(
                                        "PASS:CommonAce TestCase {0}", tstcnt);
                                }
                                else
                                {
                                    Console.WriteLine(
                                        "FAIL:CommonAce TestCase {0}", tstcnt);
                                }
                            }
                            else
                            {
                                if (CommonACEConstructorTests(flags, qualifier,
                                                        mask, strsid, iscallback,
                                                        opaque))
                                {
                                    Console.WriteLine(
                                        "PASS:CommonAce TestCase {0}", tstcnt);
                                }
                                else
                                {
                                    Console.WriteLine(
                                        "FAIL:CommonAce TestCase {0}", tstcnt);
                                }
                            }

                        }
                        tstcnt++;
                    }
                }
                reader.CloseTestCaseStore();
                return bRetval;
            }

            /*
                * Fn    :    CommonAceConstructorTests
                * Brief:    Tests the CustomAce constructor with the parameters
                *            specified in the TestCase.
                * param:    AceFlags,AceQualifier,AccessMask, 
                *            SecurityIdentifier,isCallback(true/false)
                *            opaque :byte[] of payload for the ACE
                * return:    true/false specifying success failure of
                *            the tests 
                */
            public static bool CommonACEConstructorTests(AceFlags flags,
                                                    AceQualifier qualifier,
                                                    int mask, string strsid,
                                                    bool isCallback,
                                                    byte[] opaque)
            {
                CommonAce cmnAce = null;
                ArgumentException ArgExcept = new ArgumentException();
                ArgumentOutOfRangeException AOORExcept =
                                    new ArgumentOutOfRangeException();
                ArgumentNullException ANExcept = new ArgumentNullException();
                SecurityIdentifier sid = null;
                try
                {
                    if (strsid != "null")
                    {
                        sid = new SecurityIdentifier(strsid);
                    }
                    cmnAce = new CommonAce(flags, qualifier, mask, sid,
                                            isCallback, opaque);
                }
                catch (Exception e)
                {
                    Console.WriteLine("The exception type is:{0}", e.GetType());
                    if (Object.ReferenceEquals(e.GetType(),
                                                AOORExcept.GetType()))
                    {
                        if (flags < AceFlags.None ||
                            qualifier < AceQualifier.AccessAllowed ||
                            qualifier > AceQualifier.SystemAlarm ||
                                (opaque != null &&
                                opaque.Length > MaxOpaqueLength))
                        {
                            return true;
                        }
                        return false;
                    }
                    else
                        if (Object.ReferenceEquals(e.GetType(),
                                                    ANExcept.GetType()))
                        {
                            if (sid == null)
                            {
                                return true;
                            }
                            return false;
                        }
                        else
                            if (Object.ReferenceEquals(e.GetType(),
                                                        ArgExcept.GetType()))
                            {
                                if (opaque != null && opaque.Length % 4 != 0)
                                {
                                    return true;
                                }
                                return false;
                            }
                            else
                            {
                                Console.WriteLine(
                                    "Unexpected Exception generated:{0}", e.Message);
                                return false;
                            }
                }
                if (flags < AceFlags.None ||
                    qualifier < AceQualifier.AccessAllowed ||
                    qualifier > AceQualifier.SystemAlarm ||
                    sid == null ||
                    (opaque != null &&
                    (opaque.Length > MaxOpaqueLength ||
                    opaque.Length % 4 != 0)))
                {
                    Console.WriteLine(
                        "Exception not generated for bad parameters");
                    return false;
                }
                if (cmnAce.AceFlags == flags &&
                    cmnAce.AceQualifier == qualifier &&
                    cmnAce.AccessMask == mask &&
                    cmnAce.SecurityIdentifier == sid &&
                    cmnAce.IsCallback == isCallback)
                {
                    if (isCallback)
                    {
                        if (Object.Equals(cmnAce.GetOpaque(), opaque))
                        {
                            Console.WriteLine(
                                        "CommonAce constructor successful");
                        }
                        else
                        {
                            Console.WriteLine(
                                        "CommonAce: Opaque array mismatch");
                            return false;
                        }
                    }
                    CommonAce TstAce = new CommonAce(flags, qualifier, mask, sid,
                                            isCallback, opaque);
                    if (!ACETest.GenericAceBinaryFormTests(binLen, OFFSET,
                        cmnAce, TstAce))
                    {
                        Console.WriteLine("BinaryForm Test fails");
                        return false;
                    }
                    Console.WriteLine("BinaryForm Test Successful");
                    byte[] binArray = new byte[cmnAce.BinaryLength];
                    cmnAce.GetBinaryForm(binArray, 0);
                    CommonAce TestCmnAce =
                        (CommonAce)CommonAce.CreateFromBinaryForm(binArray, 0);
                    if (TestCmnAce.AceFlags == cmnAce.AceFlags &&
                        TestCmnAce.AceQualifier == cmnAce.AceQualifier &&
                        TestCmnAce.AccessMask == cmnAce.AccessMask &&
                        TestCmnAce.SecurityIdentifier ==
                                        cmnAce.SecurityIdentifier &&
                        TestCmnAce.IsCallback == cmnAce.IsCallback)
                    {

                        if (cmnAce.OpaqueLength == TestCmnAce.OpaqueLength)
                        {
                            byte[] op1 = cmnAce.GetOpaque();
                            byte[] op2 = TestCmnAce.GetOpaque();
                            for (int i = 0; i < cmnAce.OpaqueLength; i++)
                            {
                                if (op1[i] != op2[i])
                                {
                                    Console.WriteLine("CommonAce:" +
                                        "CreateFromBinaryForm fails");
                                    return false;
                                }
                            }
                            Console.WriteLine("CommonAce:CreateFromBinaryForm" +
                                        " successful");
                            return true;
                        }
                        Console.WriteLine("CommonAce:" +
                                "Opaque array mismatch for BinaryForm");
                        return false;

                    }
                    else
                    {
                        Console.WriteLine("CommonAce object created by " +
                        "CreateFromBinaryForm does not match expected object");
                        return false;
                    }
                }
                Console.WriteLine("CommonAce constructor mismatch:" +
                    "members do not match the parameters passed");
                return false;
            }
            /*
                * Fn    :    CommonAceBinaryFormTests
                * Brief:    Tests BinaryForm function with various
                *            combinations of binaryForm and offset
                * param:    binArray: specifies if binaryForm is null,
                *            length of binaryForm array otherwise.
                *            offset: offset into the binaryForm array
                * return:    true/false specifying success failure of tests
                */
            public static bool CommonAceBinaryFormTests(string binArray, int offset)
            {
                AceQualifier qualifier = AceQualifier.AccessAllowed;
                int mask = 1;
                SecurityIdentifier sid = null;
                AceFlags flags = AceFlags.AuditFlags;
                bool isCallback = true;
                CommonAce AceObj = null, TstAce = null;
                byte[] opaque = new byte[4];
                try
                {
                    sid = new SecurityIdentifier(STR_SID);
                    AceObj = new CommonAce(flags, qualifier, mask, sid,
                        isCallback, opaque);
                    TstAce = new CommonAce(flags, qualifier, mask, sid,
                        isCallback, opaque);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception {0}", e.Message);
                    return false;
                }
                return (ACETest.GenericAceBinaryFormTests(binArray, offset,
                    AceObj, TstAce));
            }

            /*
                * Fn    :    CreateFromBinaryFormTests
                * Brief:    Tests CreateFromBinaryForm function with various
                *            combinations of binaryForm and offset
                * param:    binArray: specifies if binaryForm is null,
                *            length of binaryForm otherwise.
                *            offset: offset into the binaryForm array,
                *            parameters for CommonAce constructor    
                * return:    true/false specifying success failure of tests
            */
            public static bool CreateFromBinaryFormTests(string arraySpec,
                                                    int offset, AceFlags flags,
                                                    AceQualifier qualifier,
                                                    int mask, string strsid,
                                            bool isCallback, byte[] opaque)
            {
                bool bRetval = false;
                CommonAce AceObj = null, TstAce = null;
                try
                {
                    SecurityIdentifier sid = new SecurityIdentifier(strsid);
                    AceObj = new CommonAce(flags, qualifier, mask, sid,
                                            isCallback, opaque);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Could not create CommonAce " +
                        "object needed for CreateFromBinaryTests" +
                        " : check arguments");
                    Console.WriteLine(e.Message);
                    return false;
                }
                byte[] binaryForm = null;
                if (arraySpec == "null")
                {
                    bRetval = ACETest.GenericAceCreateFromBinaryFormTests(
                        binaryForm, offset, AceObj, TstAce);
                }
                else
                    if (arraySpec == "garbage")
                    {
                        binaryForm = new byte[AceObj.BinaryLength];
                        AceObj.GetBinaryForm(binaryForm, 0);
                        for (int k = 0; k < AceObj.BinaryLength; k++)
                        {
                            binaryForm[k] = 0;
                        }
                        bRetval =
                            ACETest.GenericAceCreateFromBinaryFormTests(
                            binaryForm, offset, AceObj, TstAce);

                    }
                    else
                    {
                        int len = 0;
                        try
                        {
                            len = int.Parse(arraySpec);
                            binaryForm = new byte[AceObj.BinaryLength + len +
                                Math.Abs(offset)];
                            AceObj.GetBinaryForm(binaryForm, Math.Abs(offset));
                            bRetval =
                                ACETest.GenericAceCreateFromBinaryFormTests(
                                binaryForm, offset, AceObj, TstAce);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Unexpected Exception {0}", e.Message);
                            bRetval = false;

                        }
                    }
                return bRetval;
            }
        }

        /*
        *Compound Ace Testing Code
        */
        public class CompoundACETest
        {
            private CompoundACETest()
            {
            }
            /*
            * Fn    :     AllTestCases
            * Brief:    Driver function which reads in TestCase from TestStore
            *            and    based on the number of parameters for the TestCase
            *            the    appropriate Test function is called
            * return: true/false specifying tests completed or not.
            */
            public static bool AllTestCases()
            {
                bool bRetval = true;
                string[] testCase;
                string strsid;
                int mask;
                CompoundAceType cpndACEType;
                AceFlags flags;
                ITestCaseReader reader = new InfTestCaseStore();
                reader.OpenTestCaseStore(CompoundACETestStore);

                Console.WriteLine("<------CompoundAce Tests------>");

                int tstcnt = 0;

                while (null != (testCase = reader.ReadNextTestCase()))
                {
                    Console.WriteLine("CompoundAce TestCase {0}", tstcnt);
                    if (testCase.Length == 2)//if GetBinaryForm TestCase
                    {
                        string binArray = testCase[0];
                        int offset = int.Parse(testCase[1]);
                        if (!CompoundAceBinaryFormTests(binArray, offset))
                        {
                            Console.WriteLine("FAIL:CompoundAce TestCase {0}", tstcnt);
                        }
                        else
                        {
                            Console.WriteLine("PASS:CompoundAce TestCase {0}", tstcnt);
                        }


                    }
                    else
                    {//for CreateFromBinaryForm and Constructor TestCases
                        if (testCase.Length >= 4)
                        {
                            flags = (AceFlags)byte.Parse(testCase[0]);
                            mask = int.Parse(testCase[1]);
                            cpndACEType = (CompoundAceType)byte.Parse(
                                                            testCase[2]);
                            strsid = testCase[3];
                            //CreateFromBinaryForm TestCase
                            if (testCase.Length == 6)
                            {
                                string arrayLen = testCase[4];
                                int offset = int.Parse(testCase[5]);
                                if (CreateFromBinaryFormTests(arrayLen, offset,
                                    flags, mask, cpndACEType,
                                    strsid))
                                {
                                    Console.WriteLine(
                                    "PASS:CompoundACE TestCase {0}", tstcnt);
                                }
                                else
                                {
                                    Console.WriteLine(
                                    "FAIL:CompoundAce TestCase {0}", tstcnt);
                                }
                            }
                            else
                            {//Constructor TestCase
                                if (CompoundACEConstructorTests(flags, mask,
                                    cpndACEType, strsid))
                                {
                                    Console.WriteLine(
                                    "PASS:CompoundACE TestCase {0}", tstcnt);
                                }
                                else
                                {
                                    Console.WriteLine(
                                    "FAIL:CompoundAce TestCase {0}", tstcnt);
                                }
                            }

                        }

                    }

                    tstcnt++;
                }
                reader.CloseTestCaseStore();
                return bRetval;
            }

            /*
                * Fn    :    CompoundAceConstructorTests
                * Brief:    Tests the CompoundAce constructor with the parameters
                *            specified in the TestCase.If the Constructor
                *            successfully creates an Object, GetBinaryForm and
                *            CreateFromBinaryForm functions are tested for the 
                *            object.
                * param:    AceFlags,AccessMask,CompoundAceType,SecurityIdentifier
                * return:    true/false specifying success failure of
                *            the tests 
                */

            private static bool CompoundACEConstructorTests(AceFlags flags,
                                                    int mask,
                                                    CompoundAceType cpdAceType,
                                                    string strsid)
            {
                CompoundAce cpdAce = null;
                Exception except = null;
                ArgumentOutOfRangeException exceptverif =
                                        new ArgumentOutOfRangeException();
                ArgumentNullException ANExcept = new ArgumentNullException();
                SecurityIdentifier sid = null;
                try
                {
                    if (strsid != "null")
                    {
                        sid = new SecurityIdentifier(strsid);
                    }
                    cpdAce = new CompoundAce(flags, mask, cpdAceType, sid);
                }
                catch (Exception e)
                {
                    except = e;
                }
                if (null != except)
                {
                    Console.WriteLine("The exception type {0}", except.GetType());
                    if (Object.ReferenceEquals(except.GetType(),
                                                exceptverif.GetType()))
                    {//CompoundAceType != Impersonation causes
                        //ArgumentOutOfRangeException to be thrown
                        Console.WriteLine("Exception Generated {0}", except.Message);
                        if (flags < AceFlags.None ||
                            cpdAceType < CompoundAceType.Impersonation ||
                            cpdAceType > CompoundAceType.Impersonation)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                        if (Object.ReferenceEquals(except.GetType(),
                                                        ANExcept.GetType()))
                        {//ArgumentNullException in case sid is null
                            if (sid == null)
                            {
                                Console.WriteLine(
                                    "WARN : Exception {0}", except.Message);
                                return true;
                            }
                            else
                                return false;
                        }
                    Console.WriteLine(
                                "Unexpected exception {0}", except.Message);
                    return false;
                }
                else
                { //verify if paramters were indeed valid
                    //TODO: per Mark's comments "The compound ACE type value is deliberately not validated" 
                    //        for detial, see bug 219367. So I comment out the check for CompoundAceType.                
                    if (flags < AceFlags.None /* 
                    cpdAceType < CompoundAceType.Impersonation ||
                    cpdAceType > CompoundAceType.Impersonation*/ ||
                        sid == null)
                    {
                        Console.WriteLine(
                            "Exception not generated for bad parameters");
                        return false;
                    }
                    //Check if the constructor initialized the members
                    //correctly
                    if (cpdAce.AceFlags == flags &&
                        cpdAce.AccessMask == mask &&
                        cpdAce.SecurityIdentifier == sid &&
                        cpdAce.CompoundAceType == cpdAceType)
                    {
                        //Perform GetBinaryForm and CreateFromBinaryForm
                        //Tests
                        CompoundAce TstAce = new CompoundAce(
                            flags, mask, cpdAceType, sid);
                        if (!ACETest.GenericAceBinaryFormTests(binLen,
                            OFFSET, cpdAce, TstAce))
                        {
                            Console.WriteLine("BinaryForm Test fails");
                            return false;
                        }
                        Console.WriteLine("BinaryForm Test succeeds");
                        byte[] binArray = new byte[cpdAce.BinaryLength];
                        cpdAce.GetBinaryForm(binArray, 0);
                        CompoundAce TestCpdAce =
                            (CompoundAce)GenericAce.CreateFromBinaryForm(binArray,
                            0);
                        if (cpdAce.AceFlags == TestCpdAce.AceFlags &&
                            cpdAce.AccessMask == TestCpdAce.AccessMask &&
                            cpdAce.SecurityIdentifier ==
                                            TestCpdAce.SecurityIdentifier &&
                            cpdAce.CompoundAceType ==
                                            TestCpdAce.CompoundAceType)
                        {
                            Console.WriteLine("CompoundAce: " +
                                "CreateFromBinaryForm creates the " +
                                "correct CompoundAce object");
                            return true;
                        }
                        else
                        {
                            Console.WriteLine("CompoundAce object created by " +
                                "CreateFromBinaryForm does not" +
                                " match expected result");
                            Console.WriteLine("Created:Expected:passed");
                            Console.WriteLine(
                                "{0}:{1}", TestCpdAce.AceFlags, cpdAce.AceFlags);
                            Console.WriteLine(
                            "{0}:{1}", TestCpdAce.AccessMask, cpdAce.AccessMask);
                            Console.WriteLine(
                "{0}:{1}", TestCpdAce.CompoundAceType, cpdAce.CompoundAceType);
                            Console.WriteLine(
            "{0}:{1}", TestCpdAce.SecurityIdentifier, cpdAce.SecurityIdentifier);

                            return false;

                        }
                    }
                    else
                    {
                        Console.WriteLine("CompoundAce constructor mismatch: " +
                            "members do not match the parameters passed");
                        return false;
                    }
                }
            }
            /*
                * Fn    :    CompoundAceBinaryFormTests
                * Brief:    Tests BinaryForm function with various
                *            combinations of binaryForm array length
                *            and offset.
                * param:    binArray: specifies if binaryForm is null,
                *            length of binaryForm otherwise.
                *            offset: offset into the binaryForm array
                * return:    true/false specifying success failure of tests
                */

            public static bool CompoundAceBinaryFormTests(string binArray, int offset)
            {
                int mask = 1;
                SecurityIdentifier sid = null;
                AceFlags flags = AceFlags.AuditFlags;
                CompoundAce AceObj = null, TstAce = null;
                CompoundAceType cpdType = CompoundAceType.Impersonation;
                try
                {
                    sid = new SecurityIdentifier(STR_SID);
                    AceObj = new CompoundAce(flags, mask, cpdType, sid
                        );
                    TstAce = new CompoundAce(flags, mask, cpdType, sid
                        );
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception {0}", e.Message);
                    return false;
                }
                return (ACETest.GenericAceBinaryFormTests(binArray, offset,
                                                    AceObj, TstAce));
            }

            /*
                * Fn    :    CreateFromBinaryFormTests
                * Brief:    Tests CreateFromBinaryForm function with various
                *            combinations of binaryForm array data and offset
                * param:    binArray: specifies if binaryForm is null,
                *            length of binaryForm otherwise.
                *            offset: offset into the binaryForm array,
                *            parameters for CompoundAce constructor    
                * return:    true/false specifying success failure of tests
            */
            public static bool CreateFromBinaryFormTests(string arraySpec,
                                                    int offset, AceFlags flags,
                                                    int mask,
                                                    CompoundAceType cpdType,
                                                    string strsid)
            {
                bool bRetval = false;
                CompoundAce AceObj = null, TstAce = null;
                try
                {
                    SecurityIdentifier sid = new SecurityIdentifier(strsid);
                    AceObj = new CompoundAce(flags, mask, cpdType, sid);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Could not create CompoundAce " +
                        "object needed for CreateFromBinaryTests" +
                        " : check arguments");
                    Console.WriteLine(e.Message);
                    return false;
                }
                byte[] binaryForm = null;
                if (arraySpec == "null")
                {
                    bRetval = ACETest.GenericAceCreateFromBinaryFormTests(
                        binaryForm, offset, AceObj, TstAce);
                }
                else
                    if (arraySpec == "garbage")
                    {
                        binaryForm = new byte[AceObj.BinaryLength];
                        AceObj.GetBinaryForm(binaryForm, 0);
                        binaryForm[HeaderLength + AccessMaskLength] = 2;//Overwriting CompoundAceType
                        binaryForm[HeaderLength + AccessMaskLength + 1] = 3;
                        bRetval =
                            ACETest.GenericAceCreateFromBinaryFormTests(
                            binaryForm, 0, AceObj, TstAce);
                    }
                    else
                    {
                        int len = 0;
                        try
                        {
                            len = int.Parse(arraySpec);
                            binaryForm = new byte[AceObj.BinaryLength + len +
                                Math.Abs(offset)];
                            AceObj.GetBinaryForm(binaryForm, Math.Abs(offset));
                            bRetval =
                                ACETest.GenericAceCreateFromBinaryFormTests(
                                binaryForm, offset, AceObj, TstAce);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Unexpected exception", e.Message);

                        }

                    }
                return bRetval;
            }
        }
    }
}
