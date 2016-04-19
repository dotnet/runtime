//--------------------------------------------------------------------------
//
//		CommonSecurityDescriptor test cases
//
//		Tests the RawSecurityDescriptor and the base abstract class GenericSecurityDescriptor's functionality
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
using System.Globalization;

namespace System.Security.AccessControl.Test
{

	//----------------------------------------------------------------------------------------------
	/*
	*  Class to test CommonSecurityDescriptor and its abstract base class GenericSecurityDescriptor
	*
	*
	*/
	//----------------------------------------------------------------------------------------------

	public class CommonSecurityDescriptorTestCases
	{
		public static readonly string ConstructorTestCaseStore = "TestCaseStores\\CommonSecurityDescriptor_Constructor.inf";
        public static readonly string CreateFromRawSecurityDescriptorTestCaseStore = "TestCaseStores\\CommonSecurityDescriptor_CreateFromRawSecurityDescriptor.inf";
        public static readonly string CreateFromSddlFormTestCaseStore = "TestCaseStores\\CommonSecurityDescriptor_CreateFromSddlForm.inf";
        public static readonly string CreateFromBinaryFormTestCaseStore = "TestCaseStores\\CommonSecurityDescriptor_CreateFromBinaryForm.inf";
        public static readonly string OwnerTestCasesStore = "TestCaseStores\\CommonSecurityDescriptor_Owner.inf";
        public static readonly string GroupTestCasesStore = "TestCaseStores\\CommonSecurityDescriptor_Group.inf";
        public static readonly string SystemAclTestCasesStore = "TestCaseStores\\CommonSecurityDescriptor_SystemAcl.inf";
        public static readonly string DiscretionaryAclTestCasesStore = "TestCaseStores\\CommonSecurityDescriptor_DiscretionaryAcl.inf";
        public static readonly string SetSystemAclProtectionTestCasesStore = "TestCaseStores\\CommonSecurityDescriptor_SetSystemAclProtection.inf";
        public static readonly string SetDiscretionaryAclProtectionTestCasesStore = "TestCaseStores\\CommonSecurityDescriptor_SetDiscretionaryAclProtection.inf";
        public static readonly string PurgeAuditTestCasesStore = "TestCaseStores\\CommonSecurityDescriptor_PurgeAudit.inf";
        public static readonly string PurgeAccessControlTestCasesStore = "TestCaseStores\\CommonSecurityDescriptor_PurgeAccessControl.inf";
        public static readonly string WasSystemAclCanonicalInitiallyTestCasesStore = "TestCaseStores\\CommonSecurityDescriptor_WasSystemAclCanonicalInitially.inf";

        public static readonly string WasDiscretionaryAclCanonicalInitiallyTestCasesStore = "TestCaseStores\\CommonSecurityDescriptor_WasDiscretionaryAclCanonicalInitially.inf";

        public static readonly string BinaryLengthTestCasesStore = "TestCaseStores\\CommonSecurityDescriptor_BinaryLength.inf";
        public static readonly string GetSddlFormTestCasesStore = "TestCaseStores\\CommonSecurityDescriptor_GetSddlForm.inf";

        public static readonly string GetBinaryFormTestCasesStore = "TestCaseStores\\CommonSecurityDescriptor_GetBinaryForm.inf";                                               

		/*
		* Constructor
		*
		*/
		public CommonSecurityDescriptorTestCases() {}

        public static Boolean Test()
        {
            Console.WriteLine("\n\n=======STARTING CommonSecurityDescriptorTestCases==========\n");
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

			Console.WriteLine("Running CommonSecurityDescriptorTestCases");

            int testCasesPerformed = 0;
            int testCasesPassed = 0;


			ConstructorTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


			CreateFromRawSecurityDescriptorTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


			CreateFromSddlFormTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


			CreateFromBinaryFormTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);			


			OwnerTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);			


			GroupTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


			SystemAclTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


			DiscretionaryAclTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);
			

			SetSystemAclProtectionTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


			SetDiscretionaryAclProtectionTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);
		

			PurgeAuditTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


			PurgeAccessControlTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


			WasSystemAclCanonicalInitiallyTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


			WasDiscretionaryAclCanonicalInitiallyTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


			BinaryLengthTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


			GetSddlFormTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


			GetBinaryFormTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);

            return (testCasesPerformed == testCasesPassed);

		}

		public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
		{
			Console.WriteLine("Running CommonSecurityDescriptorTestCases");

			Console.WriteLine("Running ConstructorTestCases");
			ConstructorTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

			Console.WriteLine("Running CreateFromRawSecurityDescriptorTestCases");
			CreateFromRawSecurityDescriptorTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

			Console.WriteLine("Running CreateFromSddlFormTestCases");
			CreateFromSddlFormTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

			Console.WriteLine("Running CreateFromBinaryFormTestCases");
			CreateFromBinaryFormTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

			Console.WriteLine("Running OwnerTestCases");
			OwnerTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

			Console.WriteLine("Running GroupTestCases");
			GroupTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);
			
			Console.WriteLine("Running SystemAclTestCases");
			SystemAclTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);			

			Console.WriteLine("Running DiscretionaryAclTestCases");
			DiscretionaryAclTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

			Console.WriteLine("Running SetSystemAclProtectionTestCases");				
			SetSystemAclProtectionTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

			Console.WriteLine("Running SetDiscretionaryAclProtectionTestCases");		
			SetDiscretionaryAclProtectionTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

			Console.WriteLine("Running PurgeAuditTestCases");				
			PurgeAuditTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

			Console.WriteLine("Running PurgeAccessControlTestCases");			
			PurgeAccessControlTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

			Console.WriteLine("Running WasSystemAclCanonicalInitiallyTestCases");				
			WasSystemAclCanonicalInitiallyTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

			Console.WriteLine("Running WasDiscretionaryAclCanonicalInitiallyTestCases");				
			WasDiscretionaryAclCanonicalInitiallyTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

			Console.WriteLine("Running BinaryLengthTestCases");				
			BinaryLengthTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

			Console.WriteLine("Running GetSddlFormTestCases");
			GetSddlFormTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

			Console.WriteLine("Running GetBinaryFormTestCases");
			GetBinaryFormTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

		}

		public static void BVTTestCases(ref int testCasesPerformed, ref int testCasesPassed)
		{
			// No test cases yet
		}
		//utility function to calculate the BinaryLength of a CommonSecurityDescriptor
		private static int UtilsComputeBinaryLength(CommonSecurityDescriptor commonSecurityDescriptor, bool needCountDacl)
		{
			int verifierBinaryLength = 0;

			if(commonSecurityDescriptor != null)
			{
				verifierBinaryLength = 20; //intialize the binary length to header length

				if(commonSecurityDescriptor.Owner != null)
					verifierBinaryLength += commonSecurityDescriptor.Owner.BinaryLength;
				if(commonSecurityDescriptor.Group != null)
					verifierBinaryLength += commonSecurityDescriptor.Group.BinaryLength;
				if((commonSecurityDescriptor.ControlFlags & ControlFlags.SystemAclPresent) != 0 && commonSecurityDescriptor.SystemAcl != null)
					verifierBinaryLength += commonSecurityDescriptor.SystemAcl.BinaryLength;				
				if((commonSecurityDescriptor.ControlFlags & ControlFlags.DiscretionaryAclPresent) != 0 && commonSecurityDescriptor.DiscretionaryAcl != null && needCountDacl)
					verifierBinaryLength += commonSecurityDescriptor.DiscretionaryAcl.BinaryLength;
			}
				
			return verifierBinaryLength;
		}



		//verify the dacl is crafted with one Allow Everyone Everything ACE
		private static bool UtilsVerifyDaclWithCraftedAce(bool isContainer, bool isDS, DiscretionaryAcl dacl)
		{
			byte []craftedBForm;
			byte []binaryForm;
			
			DiscretionaryAcl craftedDacl = new DiscretionaryAcl(isContainer, isDS, 1);
			craftedDacl.AddAccess(AccessControlType.Allow,
				new SecurityIdentifier("S-1-1-0"),
				-1,
				isContainer? InheritanceFlags.ContainerInherit|InheritanceFlags.ObjectInherit : InheritanceFlags.None,
				PropagationFlags.None);
			craftedBForm = new byte[craftedDacl.BinaryLength];
			binaryForm = new byte[dacl.BinaryLength];
			if(craftedBForm == null || binaryForm == null)
			{
				Console.WriteLine("not enough memory to get dacl binary form");
				return false;
			}

			craftedDacl.GetBinaryForm(craftedBForm, 0);
			dacl.GetBinaryForm(binaryForm, 0);
			
			return Utils.UtilIsBinaryFormEqual(craftedBForm, binaryForm);
			
		}

		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test CommonScurityDescriptor constructor
		*	        public CommonSecurityDescriptor( bool isContainer, bool isDS, ControlFlags flags, SecurityIdentifier owner, SecurityIdentifier group, SystemAcl systemAcl, DiscretionaryAcl discretionaryAcl )
		*
		*
		*/
		//----------------------------------------------------------------------------------------------


		private class ConstructorTestCases
		{
			/*
			* Constructor
			*
			*/
			private ConstructorTestCases(){}

			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			CommonSecurityDescriptor constructor
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/
			
			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				
				Console.WriteLine("Running ConstructorTestCases");

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

				bool isContainer = false;
				bool isDS = false;
				int flags = 0;
				string ownerStr = null;
				string groupStr = null;
				string saclStr = null;
				string daclStr = null;
				
				string[] testCase = null;
				ITestCaseReader reader = new InfTestCaseStore();
				reader.OpenTestCaseStore(ConstructorTestCaseStore);
				while (null != (testCase = reader.ReadNextTestCase()))
				{					
					//read isContainer
					if (1 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isContainer = bool.Parse(testCase[0]);

					//read isDS
					if (2 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isDS = bool.Parse(testCase[1]);
												
					// read flags
					if (3 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						flags = int.Parse(testCase[2]);

					// read ownerStr
					if (4 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[3])
						ownerStr = null;
					else
						ownerStr = testCase[3];

					// read groupStr
					if (5 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[4])
                                          groupStr = null;
                                   else
                                          groupStr = testCase[4];

					// read saclStr
					if (6 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[5])
						saclStr = null;
                                   else
                                          saclStr = testCase[5];

					// read daclStr
					if (7 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[6])
						daclStr = null;
                                   else
                                          daclStr = testCase[6];

					testCasesPerformed ++;


					try
					{
						if (TestConstructor(isContainer, isDS, flags, ownerStr, groupStr, saclStr, daclStr))
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


					bvtCaseCounter ++;
					if(isBVT && (bvtCaseCounter == 3))
					{
						//first 3 cases are BVT cases
						break;
					}						
				}
				reader.CloseTestCaseStore();
			}

			public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{				
				//test cases include the exceptions from the constructor
				Console.WriteLine("Running AdditionalTestCases");



                            SystemAcl sacl = null;
                            DiscretionaryAcl dacl = null;
                            CommonSecurityDescriptor sd = null;
                            
                            // test case 1: SACL is not null, SACL.IsContainer is true, but isContainer parameter is false
                            testCasesPerformed ++;

                            sacl = new SystemAcl(true, true, 10);
                            try
                            {
                                sd = new CommonSecurityDescriptor(false, true, ControlFlags.SystemAclPresent, null, null, sacl, null);                                
                                Console.WriteLine("Should throw ArgumentException when systemAcl != null &&  systemAcl.IsContainer != isContainer");
                                Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
                            }
                            catch(ArgumentException)
                            {
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);					
				}


                            // test case 2: SACL is not null, SACL.IsContainer is false, but isContainer parameter is true
                            testCasesPerformed ++;

                            sacl = new SystemAcl(false, true, 10);
                            try
                            {
                                sd = new CommonSecurityDescriptor(true, true, ControlFlags.SystemAclPresent, null, null, sacl, null);                                
                                Console.WriteLine("Should throw ArgumentException when systemAcl != null && systemAcl.IsContainer != isContainer");
                                Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
                            }
                            catch(ArgumentException)
                            {
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);					
				}


                            // test case 3: DACL is not null, DACL.IsContainer is true, but isContainer parameter is false
                            testCasesPerformed ++;

                            dacl = new DiscretionaryAcl(true, true, 10);
                            try
                            {
                                sd = new CommonSecurityDescriptor(false, true, ControlFlags.DiscretionaryAclPresent, null, null, null, dacl);                                
                                Console.WriteLine("Should throw ArgumentException when discretionaryAcl != null && discretionaryAcl.IsContainer != isContainer");
                                Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
                            }
                            catch(ArgumentException)
                            {
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);					
				}


                            // test case 4: DACL is not null, DACL.IsContainer is false, but isContainer parameter is true
                            testCasesPerformed ++;

                            dacl = new DiscretionaryAcl(false, true, 10);
                            try
                            {
                                sd = new CommonSecurityDescriptor(true, true, ControlFlags.DiscretionaryAclPresent, null, null, null, dacl);                                
                                Console.WriteLine("Should throw ArgumentException when discretionaryAcl != null && discretionaryAcl.IsContainer != isContainer");
                                Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
                            }
                            catch(ArgumentException)
                            {
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);					
				}


                            // test case 5: SACL is not null, SACL.IsDS is true, but isDS parameter is false
                            testCasesPerformed ++;

                            sacl = new SystemAcl(true, true, 10);
                            try
                            {
                                sd = new CommonSecurityDescriptor(true, false, ControlFlags.SystemAclPresent, null, null, sacl, null);                                
                                Console.WriteLine("Should throw ArgumentException when systemAcl != null &&  systemAcl.IsDS != isDS");
                                Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
                            }
                            catch(ArgumentException)
                            {
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);					
				}


                            // test case 6: SACL is not null, SACL.IsDS is false, but isDS parameter is true
                            testCasesPerformed ++;

                            sacl = new SystemAcl(true, false, 10);
                            try
                            {
                                sd = new CommonSecurityDescriptor(true, true, ControlFlags.SystemAclPresent, null, null, sacl, null);                                
                                Console.WriteLine("Should throw ArgumentException when systemAcl != null && systemAcl.IsDS != isDS");
                                Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
                            }
                            catch(ArgumentException)
                            {
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);					
				}


                            // test case 7: DACL is not null, DACL.IsDS is true, but isDS parameter is false
                            testCasesPerformed ++;

                            dacl = new DiscretionaryAcl(true, true, 10);
                            try
                            {
                                sd = new CommonSecurityDescriptor(true, false, ControlFlags.DiscretionaryAclPresent, null, null, null, dacl);                                
                                Console.WriteLine("Should throw ArgumentException when discretionaryAcl != null && discretionaryAcl.IsDS != isDS");
                                Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
                            }
                            catch(ArgumentException)
                            {
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);					
				}


                            // test case 8: DACL is not null, DACL.IsDS is false, but isDS parameter is true
                            testCasesPerformed ++;

                            dacl = new DiscretionaryAcl(true, false, 10);
                            try
                            {
                                sd = new CommonSecurityDescriptor(true, true, ControlFlags.DiscretionaryAclPresent, null, null, null, dacl);                                
                                Console.WriteLine("Should throw ArgumentException when discretionaryAcl != null && discretionaryAcl.IsDS != isDS");
                                Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
                            }
                            catch(ArgumentException)
                            {
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);					
				}

                            
                
			}

			/*
			* Method Name: TestConstructor
			*
			* Description:	check test case passes or fails  
			*
			* Parameter:	isContainer, isDS, flags, owner, group, sacl, dacl -- parameters passed to CommonSecurityDescriptor constructor
			*
			* Return:		true if test pass, false otherwise
			*/

			private static bool TestConstructor(bool isContainer, bool isDS, int flags, string ownerStr, string groupStr, string saclStr, string daclStr)
			{							
				ControlFlags controlFlags = ControlFlags.OwnerDefaulted;
				SecurityIdentifier owner = null;
				SecurityIdentifier group = null;
				RawAcl rawAcl = null;
				SystemAcl sacl = null;
				DiscretionaryAcl dacl = null;				
				

				Console.WriteLine("isContainer: {0}, isDS: {1}, flags: {2}, ownerStr: {3}, groupStr: {4}, saclStr: {5}, daclStr: {6}",
					isContainer, isDS, flags, ownerStr, groupStr, saclStr, daclStr);
				
				controlFlags = (ControlFlags) flags;
				owner = (ownerStr != null) ? new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(ownerStr)) : null;
				group = (groupStr != null) ? new SecurityIdentifier (Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(groupStr)) : null;
					
				rawAcl = (saclStr != null) ? Utils.UtilCreateRawAclFromString(saclStr) : null;
				if(rawAcl == null)
					sacl = null;
				else
					sacl = new SystemAcl(isContainer, isDS, rawAcl);
					
				rawAcl = (daclStr != null) ? Utils.UtilCreateRawAclFromString(daclStr) : null;
				if(rawAcl == null)
					dacl = null;
				else
					dacl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

				return VerifyResult(isContainer, isDS, controlFlags, owner, group, sacl, dacl);

			}


			//create a CommonSecurityDescriptor object with the parameter and verify the correctness
			private static bool VerifyResult( bool isContainer, bool isDS, ControlFlags controlFlags, SecurityIdentifier owner, SecurityIdentifier group, SystemAcl sacl, DiscretionaryAcl dacl )
			{
				CommonSecurityDescriptor commonSecurityDescriptor = null;
				bool result = false;

				try
				{
										
					commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS,  controlFlags, owner, group, sacl, dacl);

					// verify the result
					if((isContainer == commonSecurityDescriptor.IsContainer) &&
						(isDS == commonSecurityDescriptor.IsDS) &&
						(((	(sacl != null)? (controlFlags|ControlFlags.SystemAclPresent):(controlFlags&(~ControlFlags.SystemAclPresent)))
						| ControlFlags.SelfRelative | ControlFlags.DiscretionaryAclPresent) == commonSecurityDescriptor.ControlFlags) &&
						(owner == commonSecurityDescriptor.Owner) &&
						(group == commonSecurityDescriptor.Group) &&
						(sacl == commonSecurityDescriptor.SystemAcl) &&
						(UtilsComputeBinaryLength(commonSecurityDescriptor, dacl != null) == commonSecurityDescriptor.BinaryLength))
					{

						if( dacl == null)
						{
							//check the contructor created an empty Dacl with correct IsContainer and isDS info
							if(isContainer == commonSecurityDescriptor.DiscretionaryAcl.IsContainer && 
								isDS == commonSecurityDescriptor.DiscretionaryAcl.IsDS && 
								commonSecurityDescriptor.DiscretionaryAcl.Count == 1 &&
								UtilsVerifyDaclWithCraftedAce(isContainer, isDS, commonSecurityDescriptor.DiscretionaryAcl))
							
							{
								result =  true;
							}
							else
							{
								Console.WriteLine("null dacl passed in, the expected IsContainer: {0}, IsDS: {1} and Count: 1 for the dacl created by the constructor \n the actual IsContainer: {2}, IsDS: {3} and count {4}", 
									isContainer, isDS, commonSecurityDescriptor.DiscretionaryAcl.IsContainer, commonSecurityDescriptor.DiscretionaryAcl.IsDS,  commonSecurityDescriptor.DiscretionaryAcl.Count);
								result = false;	
							}
						}
						else if (dacl == commonSecurityDescriptor.DiscretionaryAcl)
						{
							result = true;
						}
						else
						{
							Console.WriteLine("The dacl is not equal to the one passed to constructor ");
							result = false;							
	
						}						
					}
					else
					{
						Console.WriteLine("The IsContainer, IsDS, ControlFlags, Owner, Group or SystemAcl is not equal to the one passed to constructor ");

						Console.WriteLine("Expected IsContainer is: {0}, actual result : {1}", isContainer, commonSecurityDescriptor.IsContainer);					
						Console.WriteLine("Expected IsDS is: {0}, actual result : {1}", isDS, commonSecurityDescriptor.IsDS);	
						Console.WriteLine("Expected ConstrolFlags is: {0}, actual result : {1}", ((sacl != null)?(controlFlags|ControlFlags.SystemAclPresent):controlFlags),
							commonSecurityDescriptor.ControlFlags);	

						Console.WriteLine("Expected Owner is: {0}, actual result : {1}", owner, commonSecurityDescriptor.Owner);
						Console.WriteLine("Expected Group is: {0}, actual result : {1}", group, commonSecurityDescriptor.Group);						
						result = false;
					}
					
				}
				catch (ArgumentException e)
				{
					if((sacl != null && sacl.IsContainer != isContainer) || 
						(sacl != null && sacl.IsDS != isDS) ||
						(dacl != null && dacl.IsContainer != isContainer) ||
						(dacl != null && dacl.IsDS != isDS))

						result = true;
					else
					{
						// unexpected exception
						Console.WriteLine("Exception generated: " + e.Message, e);
						result = false;
					}
				}
				return result;				
				
			}				
		}


		//#####this method is changed to constructor		

		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test CommonSecurityDescriptor method
		*	        public static CommonSecurityDescriptor CreateFromRawSecurityDescriptor( bool isContainer, bool isDS, RawSecurityDescriptor rawSecurityDescriptor )
		*
		*
		*/
		//----------------------------------------------------------------------------------------------

		private class CreateFromRawSecurityDescriptorTestCases
		{
			/*
			* Constructor
			*
			*/
			private CreateFromRawSecurityDescriptorTestCases(){}


			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			CreateFromRawSecurityDescriptor
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/
			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{

				Console.WriteLine("Running CreateFromRawSecurityDescriptorTestCases");

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
				reader.OpenTestCaseStore(CreateFromRawSecurityDescriptorTestCaseStore);
				while (null != (testCase = reader.ReadNextTestCase()))
				{
					//read isContainer
					bool isContainer;
					if (1 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isContainer = bool.Parse(testCase[0]);

					//read isDS
					bool isDS;
					if (2 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isDS = bool.Parse(testCase[1]);
												
					// read rawSecurityDescriptorSddl
					string  rawSecurityDescriptorSddl;
					if (3 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[2])
						rawSecurityDescriptorSddl = null;
					else						
						rawSecurityDescriptorSddl = testCase[2];

					// read verifierSddl
					string  verifierSddl;
					if (4 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[3])
						verifierSddl = null;
					else						
						verifierSddl = testCase[3];					
                                   
					testCasesPerformed ++;


					try
					{
						if (TestCreateFromRawSecurityDescriptor(isContainer, isDS, rawSecurityDescriptorSddl, verifierSddl))
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


					bvtCaseCounter ++;
					if(isBVT && (bvtCaseCounter == 3))
					{
						//first 3 cases are BVT cases
						break;
					}						
				}
				reader.CloseTestCaseStore();
			}

			public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				//test cases include the exceptions from the constructor
				Console.WriteLine("Running AdditionalTestCases");



                            CommonSecurityDescriptor sd = null;

                            // test case 1: rawSecurityDescriptor is null
                            testCasesPerformed ++;

                            try
                            {
                                sd = new CommonSecurityDescriptor(true, true, (RawSecurityDescriptor)null);
                                Console.WriteLine("Should throw ArgumentNullException when rawSecurityDescriptor == null");
                                Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
                            }
                            catch(ArgumentNullException)
                            {
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);					
				}

			
			}

			/*
			* Method Name: TestCreateFromRawSecurityDescriptor
			*
			* Description:	check test case passes or fails  
			*
			* Parameter:	rawSecurityDescriptorSddl -- sddl string to create the RawSecurityDescriptor
			*			isContainer, isDS -- parameters used to create the CommonSeucrityDescriptor
			*			verifierSddl -- the validation sddl string
			*
			* Return:		true if test pass, false otherwise
			*/
			
			private static bool TestCreateFromRawSecurityDescriptor(bool isContainer, bool isDS, string rawSecurityDescriptorSddl, string verifierSddl)
			{							
				CommonSecurityDescriptor commonSecurityDescriptor = null;
				RawSecurityDescriptor rawSecurityDescriptor = null;
				bool result = false;
				string resultSddlForm = null;

				Console.WriteLine("isContainer: {0}, isDS: {1}, rawSecurityDescriptorSddl: {2}", isContainer, isDS, rawSecurityDescriptorSddl);

				rawSecurityDescriptor = new RawSecurityDescriptor(rawSecurityDescriptorSddl);
				commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS,  rawSecurityDescriptor);

				// verify the result
				if((isContainer == commonSecurityDescriptor.IsContainer) &&
					(isDS == commonSecurityDescriptor.IsDS))
				{
					resultSddlForm = commonSecurityDescriptor.GetSddlForm(AccessControlSections.All);

                    if (String.Compare(verifierSddl, resultSddlForm, StringComparison.CurrentCultureIgnoreCase) == 0)
					{
						result = true;
					}
					else
					{
						Console.WriteLine("Expected sddl form: {0}, actual result : {1}", verifierSddl, resultSddlForm);					
						result = false;
					}
				}
				else
				{

					Console.WriteLine("Expected IsContainer is: {0}, actual result : {1}", isContainer, commonSecurityDescriptor.IsContainer);					
					Console.WriteLine("Expected IsDS is: {0}, actual result : {1}", isDS, commonSecurityDescriptor.IsDS);	
					
					result = false;	
				}

				return result;
			}
		}
		
		//#####below two methods have been changed to constructor
		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test CommonSecurityDescriptor method
		*		public static CommonSecurityDescriptor CreateFromSddlForm( bool isContainer, bool isDS, string sddlForm )
		*
		*
		*/
		//----------------------------------------------------------------------------------------------


		private class CreateFromSddlFormTestCases
		{
			/*
			* Constructor
			*
			*/
			private CreateFromSddlFormTestCases(){}


			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			CreateFromSddlForm
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/
			
			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{

				Console.WriteLine("Running CreateFromSddlFormTestCases");

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
				reader.OpenTestCaseStore(CreateFromSddlFormTestCaseStore);
				while (null != (testCase = reader.ReadNextTestCase()))
				{

					//read isContainer
					bool isContainer;
					if (1 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isContainer = bool.Parse(testCase[0]);

					//read isDS
					bool isDS;
					if (2 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isDS = bool.Parse(testCase[1]);
												
					// read sddl
					string  sddl;
					if (3 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[2])
						sddl = null;
					else						
						sddl = testCase[2];

					// read verifierSddl
					string  verifierSddl;
					if (4 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[3])
						verifierSddl = null;
					else						
						verifierSddl = testCase[3];									
                                   
					testCasesPerformed ++;

					try
					{
						if (TestCreateFromSddlForm(isContainer, isDS, sddl, verifierSddl))
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


					bvtCaseCounter ++;
					if(isBVT && (bvtCaseCounter == 3))
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
				CommonSecurityDescriptor commonSecurityDescriptor = null;
				
				Console.WriteLine("Running AdditionalTestCases");
				

				
				// Case1, null sddl string

				testCasesPerformed ++;


				try
				{

					commonSecurityDescriptor = new CommonSecurityDescriptor(false, false, (string)null);

					// expect to throw exception but not
					Console.WriteLine("Should not allow null rawsecuritydescriptor created");				
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
				}
				catch(ArgumentNullException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
					//Utils.Print("Test case {0} FAILED!", testCasesPerformed, null);
				}



				// Case 2, empty string sddl				
				testCasesPerformed++;

				
				try
				{
					if (TestCreateFromSddlForm(true, false, "", ""))
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;
					}
					else
					{
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
					}
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				// Case 3, sddl form owner symbol exists but no content
				
				testCasesPerformed++;


				try
				{
					commonSecurityDescriptor = new CommonSecurityDescriptor(false, false, "O:G:SYD:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)");

					// expect to throw exception but not
					Console.WriteLine("Should not allow rawsecuritydescriptor created from garbage sddl form");				
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);					
				}
				catch(ArgumentException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}




				// Case 4, sddl form group symbol exists but no content
				
				testCasesPerformed++;


				try
				{
					commonSecurityDescriptor = new CommonSecurityDescriptor(false, false, "O:LAG:D:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)");

					// expect to throw exception but not
					Console.WriteLine("Should not allow rawsecuritydescriptor created from garbage sddl form");				
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);					
				}
				catch(ArgumentException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}



				// Case 5, garbage string sddl
				testCasesPerformed++;



				try
				{
					commonSecurityDescriptor = new CommonSecurityDescriptor(false, false, "ABCDEFGHIJKLMNOPQ");	

					// expect to throw exception but not
					Console.WriteLine("Should not allow commonsecuritydescriptor created from garbage sddl form");				
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);					
				}
				catch(ArgumentException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}



				// Case 6, on pre win2k OS, should throw PlatformNotSupportedException
				testCasesPerformed++;


				try
				{
					commonSecurityDescriptor = new CommonSecurityDescriptor(false, false, "O:BAG:BO");	
					
					 if ( !GenericSecurityDescriptor.IsSddlConversionSupported())
					 {// expect to throw exception but not
						Console.WriteLine("Should throw PlatformNotSupportedException on pre win2k platform");				
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);					
					 }
					 else
					 {
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;						 	
					 }
				}
				catch(PlatformNotSupportedException)
				{
					 if ( !GenericSecurityDescriptor.IsSddlConversionSupported())
					 {//this is expected
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;
					 }
					 else
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);					 	
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				
				// Case 7, sddl form with invalid owner sid
				
				testCasesPerformed++;


				try
				{
					commonSecurityDescriptor = new CommonSecurityDescriptor(false, false, "O:XXG:D:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)");	

					// expect to throw exception but not
					Console.WriteLine("Should not allow rawsecuritydescriptor created from garbage sddl form");				
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);					
				}
				catch(ArgumentException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}



				// Case 8, sddl form with invalid group sid
				
				testCasesPerformed++;


				try
				{
					commonSecurityDescriptor = new CommonSecurityDescriptor(false, false, "O:LAG:YYD:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)");	

					// expect to throw exception but not
					Console.WriteLine("Should not allow rawsecuritydescriptor created from garbage sddl form");				
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);					
				}
				catch(ArgumentException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


			}

			/*
			* Method Name: TestCreateFromSddlForm
			*
			* Description:	check test case passes or fails  
			*
			* Parameter:	sddl -- the sddl string to create CommonSecurityDescriptor object
			*			isContainer, isDS -- parameters passed to CreateFromSddlForm with string sddl
			*			verifierSddl -- the validation sddl string
			*
			* Return:		true if test pass, false otherwise
			*/

			private static bool TestCreateFromSddlForm(bool isContainer, bool isDS, string sddl, string verifierSddl)
			{				
				CommonSecurityDescriptor commonSecurityDescriptor = null;

				Console.WriteLine("isContainer: {0}, isDS: {1}, sddl: {2}", isContainer, isDS, sddl);
				commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, sddl);

				string resultSddlForm = commonSecurityDescriptor.GetSddlForm(AccessControlSections.All);
                if (String.Compare(verifierSddl, resultSddlForm, StringComparison.CurrentCultureIgnoreCase) == 0)
				{
					return true;
				}
				else
				{
					Console.WriteLine("Expected sddl form: {0}, actual result : {1}", verifierSddl, resultSddlForm);					
					return false;
				}
				
			}
		}

		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test CommonSecurityDescriptor method
		*		public static CommonSecurityDescriptor CreateFromBinaryForm( bool isContainer, bool isDS, byte[] binaryForm, int offset )
		*
		*
		*/
		//----------------------------------------------------------------------------------------------

		private class CreateFromBinaryFormTestCases
		{
			/*
			* Constructor
			*
			*/
			private CreateFromBinaryFormTestCases(){}

			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			CreateFromBinaryForm
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/
			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{

				Console.WriteLine("Running CreateFromBinaryFormTestCases");

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
				reader.OpenTestCaseStore(CreateFromBinaryFormTestCaseStore); 
				while (null != (testCase = reader.ReadNextTestCase()))
				{
					//read isContainer
					bool isContainer;
					if (1 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isContainer = bool.Parse(testCase[0]);

					//read isDS
					bool isDS;
					if (2 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isDS = bool.Parse(testCase[1]);
												
					// read sddl
					string  sddl;
					byte[] binaryform;					
					if (3 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[2])
						binaryform = null;
					else	
                                    {
						sddl = testCase[2];
                                          if(0 !=Utils.UtilCreateBinaryArrayFromSddl(sddl,  out binaryform))
                                            
                                            throw new ArgumentOutOfRangeException();
                                    }						

					// read offset
					int offset;
					if (4 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						offset = int.Parse(testCase[3]);
 

					// read verifierSddl
					string  verifierSddl;
					if (5 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[4])
						verifierSddl = null;
					else						
						verifierSddl = testCase[4];
                                  
					testCasesPerformed ++;

					try
					{
						if (TestCreateFromBinaryForm(isContainer, isDS, binaryform, offset, verifierSddl))
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


					bvtCaseCounter ++;
					if(isBVT && (bvtCaseCounter == 3))
					{
						//first 3 cases are BVT cases
						break;
					}						
				}
				reader.CloseTestCaseStore();
			}

			public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{					
			       CommonSecurityDescriptor commonSecurityDescriptor = null;
				
				Console.WriteLine("Running AdditionalTestCases");
				

				
				// Case1, null binary form

				testCasesPerformed ++;


				try
				{

					commonSecurityDescriptor = new CommonSecurityDescriptor(false, false, null, 0);

					// expect to throw exception but not
					Console.WriteLine("Should not allow null binary form to be passed in to create comon security descriptor.");				
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
				}
				catch(ArgumentNullException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}



                            // case 2: offset < 0
                            testCasesPerformed ++;


				try
				{

					commonSecurityDescriptor = new CommonSecurityDescriptor(false, false, new byte[24], -1);

					// expect to throw exception but not
					Console.WriteLine("Should not allow offset that is less than zero to be passed in to create comon security descriptor.");				
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
				}
				catch(ArgumentOutOfRangeException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}



                            // case 3: binaryForm.Length - offset < HeaderLength
                            testCasesPerformed ++;


				try
				{

					commonSecurityDescriptor = new CommonSecurityDescriptor(false, false, new byte[24], 5);

					// expect to throw exception but not
					Console.WriteLine("Should not allow binaryForm that is too small.");				
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
				}
				catch(ArgumentOutOfRangeException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}



                            // case 4: binaryForm[offset + 0] != Revision
                            testCasesPerformed ++;


				try
				{

					commonSecurityDescriptor = new CommonSecurityDescriptor(false, false, new byte[24], 0);

					// expect to throw exception but not
					Console.WriteLine("Should not allow security descriptor revision that is not equal to 1.");				
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
				}
				catch(ArgumentOutOfRangeException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}



                            // case 5: ControlFlags.SelfRelative is not set
                            testCasesPerformed ++;


				try
				{
					byte [] binaryForm = new byte[24];
					for(int i=0; i< binaryForm.Length; i++)
						binaryForm[i] = 0;
					//set correct Revision
					binaryForm[0] = 1;

					commonSecurityDescriptor = new CommonSecurityDescriptor(false, false, binaryForm, 0);

					// expect to throw exception but not
					Console.WriteLine("Should not allow security descriptor control flag that does not have SelfRelative bit set.");				
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
				}
				catch(ArgumentException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}



				// Case 6, parameter binaryForm is empty
				testCasesPerformed++;

				try
				{
					commonSecurityDescriptor = new CommonSecurityDescriptor(false, false, new byte[0], 0);

					// expect to throw exception but not
					Console.WriteLine("Should not allow commonsecurity descriptor created from empty array");					
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
				}
				catch(ArgumentOutOfRangeException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);					
				}



				// Case 7, an array of garbage
				testCasesPerformed++;


				try
				{
					byte[] binaryForm = {1, 0, 0, 128, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};					
					commonSecurityDescriptor = new CommonSecurityDescriptor(false, false, binaryForm, 0);

					// expect to throw exception
					Console.WriteLine("Should not allow creation from garbage array ");					
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
				}
				catch(ArgumentException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;
				}				
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);	
				}

		                            
			}


			/*
			* Method Name: TestCreateFromBinaryForm
			*
			* Description:	check test case passes or fails  
			*
			* Parameter:	binaryForm, offset -- the binary and start index to create CommonSecurityDescriptor object
			*			isContainer, isDS -- parameters passed to CreateFromBinarylForm with binaryForm and offset
			*			verifierSddl -- the validation sddl string
			*
			* Return:		true if test pass, false otherwise
			*/
			private static bool TestCreateFromBinaryForm(bool isContainer, bool isDS, byte[] binaryForm, int offset, string verifierSddl)
			{				
				CommonSecurityDescriptor commonSecurityDescriptor = null;
				byte[] tempBForm = null;

				Console.WriteLine("isContainer: {0}, isDS: {1}, offset : {2}", isContainer, isDS, offset);
				if(offset > 0)
				{//copy the binaryform to a new array, start at offset
				
				 	tempBForm = new byte[binaryForm.Length + offset];
					for(int i = 0; i < binaryForm.Length; i++)
					{
						tempBForm[i + offset] = binaryForm[i];
					}
					commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, tempBForm, offset);
				}
				else
				{
					commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, binaryForm, offset);
				}

				string resultSddlForm = commonSecurityDescriptor.GetSddlForm(AccessControlSections.All);
                if (String.Compare(verifierSddl, resultSddlForm, StringComparison.CurrentCultureIgnoreCase) == 0)
				{
					return true;
				}
				else
				{
					Console.WriteLine("Expected sddl form: {0}, actual result : {1}", verifierSddl, resultSddlForm);
					return false;
				}

			}
		}				

		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test CommonSecurityDescriptor property 
		*		public override SecurityIdentifier Owner
		*
		*
		*/
		//----------------------------------------------------------------------------------------------

		private class OwnerTestCases
		{
			/*
			* Constructor
			*
			*/
			private OwnerTestCases(){}

			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			Owner
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/
			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{

				Console.WriteLine("Running OwnerTestCases");

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
				reader.OpenTestCaseStore(OwnerTestCasesStore); 
				while (null != (testCase = reader.ReadNextTestCase()))
				{
					// read newOwner
					string newOwner;
					if (1 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[0])
						newOwner =  null;
					else
						newOwner= testCase[0];

					// assign isContainer, isDS, flags, owner, group, sacl, dacl to initialize a RawSecurityDescriptor object
					bool isContainer = false;
					bool isDS = false;
					int controlFlags = 1;
                                   string owner = "BA";
					string group = "BG";
                                   
					testCasesPerformed ++;

					try
					{
						if (TestOwner(isContainer, isDS, controlFlags, owner, newOwner, group))
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


					bvtCaseCounter ++;
					if(isBVT && (bvtCaseCounter == 3))
					{
						//first 3 cases are BVT cases
						break;
					}						
				}
				reader.CloseTestCaseStore();
			}

			public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				// No test cases yet
			}

			/*
			* Method Name: TestOwner
			*
			* Description:	check test case passes or fails  
			*
			* Parameter:	isContainer, isDS, owner, group, sacl, dacl -- parameters passed to CommonSecurityDescriptor constructor
			*			newOwner -- the value set to the CommonSecurityDescriptor object Owner property and used for validation
			*
			* Return:		true if test pass, false otherwise
			*/
			private static bool TestOwner(bool isContainer, bool isDS, int controlFlags, string ownerStr, string newOwnerStr, string groupStr)
			{				
				CommonSecurityDescriptor commonSecurityDescriptor = null;
				SecurityIdentifier owner = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(ownerStr));
				SecurityIdentifier newOwner = (newOwnerStr != null ? new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(newOwnerStr)): null);
				SecurityIdentifier group = new SecurityIdentifier (Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(groupStr));
				SystemAcl sacl = null;
				DiscretionaryAcl dacl = null;

				Console.WriteLine("isContainer: {0}, isDS: {1}, controlFlags: {2}, owner: {3}, group: {4}, newOwner: {5}",
					isContainer, isDS, controlFlags, ownerStr, groupStr, newOwnerStr);
				commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, (ControlFlags)controlFlags, owner, group, sacl, dacl);
				commonSecurityDescriptor.Owner = newOwner;

				// verify the result, we can use == here as SecurityIdentifier overrides the comparsison
				if(newOwner == commonSecurityDescriptor.Owner)
				{
					return true;
				}
				else
				{
					Console.WriteLine("The owner returned is not equal to what we set. Expected owner: {0}, actual result : {1}", newOwner, commonSecurityDescriptor.Owner);						
					return false;
				}
			
			}
		}


		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test CommonSecurityDescriptor property 
		*		public override SecurityIdentifier Group
		*
		*
		*/
		//----------------------------------------------------------------------------------------------

		private class GroupTestCases
		{
			/*
			* Constructor
			*
			*/
			private GroupTestCases(){}

			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			Group
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/
			
			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{

				Console.WriteLine("Running GroupTestCases");

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
				reader.OpenTestCaseStore(GroupTestCasesStore); 
				while (null != (testCase = reader.ReadNextTestCase()))
				{
					// read newGroup
					string newGroup;
					if (1 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[0])
						newGroup =  null;
					else
						newGroup= testCase[0];

					// assign isContainer, isDS, flags, owner, group, sacl, dacl to initialize a RawSecurityDescriptor object
					bool isContainer = false;
					bool isDS = false;
					int controlFlags = 1;
                                   string owner = "BA";
					string group = "BG";
                                   
					testCasesPerformed ++;

					try
					{
						if (TestGroup(isContainer, isDS, controlFlags, owner, group, newGroup))
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


					bvtCaseCounter ++;
					if(isBVT && (bvtCaseCounter == 3))
					{
						//first 3 cases are BVT cases
						break;
					}						
				}
				reader.CloseTestCaseStore();
			}

			public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				// No test cases yet
			}

			/*
			* Method Name: TestGroup
			*
			* Description:	check test case passes or fails  
			*
			* Parameter:	isContainer, isDS, owner, group, sacl, dacl -- parameters passed to CommonSecurityDescriptor constructor
			*			newGroup -- the value set to the CommonSecurityDescriptor object Group property and used for validation
			*
			* Return:		true if test pass, false otherwise
			*/
			
			private static bool TestGroup(bool isContainer, bool isDS, int controlFlags, string ownerStr, string groupStr, string newGroupStr)
			{				

				CommonSecurityDescriptor commonSecurityDescriptor = null;
				SecurityIdentifier owner = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(ownerStr));
				SecurityIdentifier newGroup = (newGroupStr != null ? new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(newGroupStr)): null);
				SecurityIdentifier group = new SecurityIdentifier (Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(groupStr));
				SystemAcl sacl = null;
				DiscretionaryAcl dacl = null;

				Console.WriteLine("isContainer: {0}, isDS: {1}, controlFlags: {2}, owner: {3}, group: {4}, newGroup: {5}",
					isContainer, isDS, controlFlags, ownerStr, groupStr, newGroupStr);				

				commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, (ControlFlags)controlFlags, owner, group, sacl, dacl);
				commonSecurityDescriptor.Group = newGroup;

				// verify the result, we can use == here as SecurityIdentifier overrides the comparsison
				if(newGroup == commonSecurityDescriptor.Group)
				{
					return true;
				}
				else
				{
					Console.WriteLine("The group returned is not equal to what we set. Expected group: {0}, actual result : {1}", newGroup, commonSecurityDescriptor.Group);						
					return false;
				}
			
			}
		}


		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test CommonSecurityDescriptor property
		*		public SystemAcl SystemAcl
		*
		*
		*/
		//----------------------------------------------------------------------------------------------

		private class SystemAclTestCases
		{
			/*
			* Constructor
			*
			*/
			private SystemAclTestCases(){}

			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			CommonSecurityDescriptor property SystemAcl
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/

			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{

				Console.WriteLine("Running SystemAclTestCases");

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
				
				CommonSecurityDescriptor commonSecurityDescriptor = null;


				SystemAcl sacl = null;

				string[] testCase = null;
				ITestCaseReader reader = new InfTestCaseStore();
				reader.OpenTestCaseStore(SystemAclTestCasesStore); 
				while (null != (testCase = reader.ReadNextTestCase()))
				{

					//read isContainerSD
					bool isContainerSD;
					if (1 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isContainerSD = bool.Parse(testCase[0]);

					//read isDSSD
					bool isDSSD;
					if (2 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isDSSD = bool.Parse(testCase[1]);
												
					// read sddl
					string  sddl;
					if (3 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[2])
						sddl = null;
					else						
						sddl = testCase[2];

					//read isContainerSacl
					bool isContainerSacl;
					if (4 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isContainerSacl = bool.Parse(testCase[3]);

					//read isDSSacl
					bool isDSSacl;
					if (5 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isDSSacl = bool.Parse(testCase[4]);
																	
					
					// read newSaclStr
					string newSaclStr;
					if (6 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ( "null" == testCase[5])
						newSaclStr = null;
					else						
						newSaclStr= testCase[5];

					if(newSaclStr != null) 
					{
						sacl = new SystemAcl(isContainerSacl, isDSSacl, Utils.UtilCreateRawAclFromString(newSaclStr));
					}
					else
						sacl = null;

					commonSecurityDescriptor = new CommonSecurityDescriptor(isContainerSD, isDSSD, sddl);
                                   
					testCasesPerformed ++;

					try
					{
						if (TestSystemAcl(commonSecurityDescriptor, sacl))
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


					bvtCaseCounter ++;
					if(isBVT && (bvtCaseCounter == 3))
					{
						//first 3 cases are BVT cases
						break;
					}					
				}
				reader.CloseTestCaseStore();
			}

			public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				// No test cases yet
			}


			/*
			* Method Name: TestSystemAcl
			*
			* Description:	check test case passes or fails  
			*
			* Parameter:	flags, owner, group, sacl, dacl -- parameters passed to CommonSecurityDescriptor constructor to create a RawSecurityDescriptor object
			*			sacl --  value set to the CommonSecurityDescriptor SystemAcl and used for validation
			*
			* Return:		true if test pass, false otherwise
			*/
			private static bool TestSystemAcl(CommonSecurityDescriptor commonSecurityDescriptor, SystemAcl sacl)
			{				
				bool result = false;
		
				try
				{
						
					commonSecurityDescriptor.SystemAcl = sacl;
					if(sacl == commonSecurityDescriptor.SystemAcl)
					{
						if(sacl != null && (commonSecurityDescriptor.ControlFlags & ControlFlags.SystemAclPresent) != 0 )
						{
							result = true;
						}
						else if (sacl == null && (commonSecurityDescriptor.ControlFlags & ControlFlags.SystemAclPresent )== 0)
						{
							result = true;
						}
						else
						{
							Console.WriteLine("SystemAclPresent ControlFlags is not set correctly");
							result = false;
						}
					}
					else
					{
						Console.WriteLine("The SystemAcl returned is not equal to what we set");
						result = false;
					}
				}
				catch(NullReferenceException)
				{
					if(sacl == null)
						Console.WriteLine("*******Bug245007*******");
				}
				catch (ArgumentException e)
				{
					if ((sacl.IsContainer != commonSecurityDescriptor.IsContainer) || (sacl.IsDS != commonSecurityDescriptor.IsDS))
					{
						result = true;
					}
					else
					{
						// unexpected exception
						Console.WriteLine("Exception generated: " + e.Message, e);
						result = false;		
					}
				}

				return result;
			}
		}


		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test CommonSecurityDescriptor property
		*		public DiscretionaryAcl DiscretionaryAcl
		*
		*
		*/
		//----------------------------------------------------------------------------------------------

		private class DiscretionaryAclTestCases
		{
			/*
			* Constructor
			*
			*/
			private DiscretionaryAclTestCases(){}

			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			CommonSecurityDescriptor property DiscretionaryAcl
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/

			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{

				Console.WriteLine("Running DiscretionaryAclTestCases");

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
				
				CommonSecurityDescriptor commonSecurityDescriptor = null;

				DiscretionaryAcl dacl = null;

				string[] testCase = null;
				ITestCaseReader reader = new InfTestCaseStore();
				reader.OpenTestCaseStore(DiscretionaryAclTestCasesStore); 
				while (null != (testCase = reader.ReadNextTestCase()))
				{
					//read isContainerSD
					bool isContainerSD;
					if (1 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isContainerSD = bool.Parse(testCase[0]);

					//read isDSSD
					bool isDSSD;
					if (2 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isDSSD = bool.Parse(testCase[1]);
												
					// read sddl
					string  sddl;
					if (3 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[2])
						sddl = null;
					else						
						sddl = testCase[2];

					//read isContainerDacl
					bool isContainerDacl;
					if (4 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isContainerDacl = bool.Parse(testCase[3]);

					//read isDSDacl
					bool isDSDacl;
					if (5 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isDSDacl = bool.Parse(testCase[4]);

					// read newDaclStr
					string newDaclStr;
					if (6 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ( "null" == testCase[5])
						newDaclStr = null;
					else						
						newDaclStr= testCase[5];

					if(newDaclStr != null) 
					{
						dacl = new DiscretionaryAcl(isContainerDacl, isDSDacl, Utils.UtilCreateRawAclFromString(newDaclStr));
					}
					else
						dacl = null;

					
					commonSecurityDescriptor = new CommonSecurityDescriptor(isContainerSD, isDSSD, sddl);

                                
					testCasesPerformed ++;

					try
					{
						if (TestDiscretionaryAcl(commonSecurityDescriptor, dacl))
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


					bvtCaseCounter ++;
					if(isBVT && (bvtCaseCounter == 3))
					{
						//first 3 cases are BVT cases
						break;
					}						
				}
				reader.CloseTestCaseStore();
			}

			public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				// No test cases yet
			}


			/*
			* Method Name: TestDiscretionaryAcl
			*
			* Description:	check test case passes or fails  
			*
			* Parameter:	flags, owner, group, sacl, dacl -- parameters passed to CommonSecurityDescriptor constructor to create a RawSecurityDescriptor object
			*			dacl --  value set to the CommonSecurityDescriptor DiscretionaryAcl and used for validation
			*
			* Return:		true if test pass, false otherwise
			*/
			private static bool TestDiscretionaryAcl(CommonSecurityDescriptor commonSecurityDescriptor, DiscretionaryAcl dacl)
			{				
				bool result = false;
				bool isContainer = false;
				bool isDS = false;
		
				try
				{
					//save IsContainer, IsDS
					isContainer = commonSecurityDescriptor.IsContainer;
					isDS = commonSecurityDescriptor.IsDS;
					
					commonSecurityDescriptor.DiscretionaryAcl = dacl;
					//per shawn, the controlflag will show DaclPresent when dacl assigned is null
					if((commonSecurityDescriptor.ControlFlags & ControlFlags.DiscretionaryAclPresent) != 0)
					{
						if(dacl == null)
						{//a dacl with Allow Everyone Everything Ace should be assigned
							if(commonSecurityDescriptor.DiscretionaryAcl.Count == 1 &&
							commonSecurityDescriptor.IsContainer == isContainer &&
							commonSecurityDescriptor.IsDS == isDS &&
							UtilsVerifyDaclWithCraftedAce(commonSecurityDescriptor.IsContainer, commonSecurityDescriptor.IsDS, commonSecurityDescriptor.DiscretionaryAcl))
							{
								return true;
							}
							else
							{
								Console.WriteLine("When set null dacl, a dacl with Allow Everyone Everything Ace is not  set correctly");							
								result = false;								
							}												
						}
						else if(dacl == commonSecurityDescriptor.DiscretionaryAcl)
						{
							result = true;
						}
						else 
						{
							Console.WriteLine("The DiscretionaryAcl returned is not equal to what we set");
							result = false;
						}
					}
					else
					{
						Console.WriteLine("DiscretionaryAcl ControlFlags is not set");							
						result = false;						
					}
				}
				catch(NullReferenceException)
				{
					if(dacl == null)
						Console.WriteLine("*******Bug245009*******");
				}
				catch (ArgumentException e)
				{
					if ((dacl.IsContainer != commonSecurityDescriptor.IsContainer) || (dacl.IsDS != commonSecurityDescriptor.IsDS))
					{
						result = true;
					}
					else
					{
						// unexpected exception
						Console.WriteLine("Exception generated: " + e.Message, e);
						result = false;		
					}
				}				

				return result;
			}
		}

		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test CommonSecurityDescriptor property
		*		public void SetSystemAclProtection( bool isProtected, bool preserveInheritance )
		*
		*
		*/
		//----------------------------------------------------------------------------------------------

		private class SetSystemAclProtectionTestCases
		{
			/*
			* Constructor
			*
			*/		
			private SetSystemAclProtectionTestCases(){}

			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			SetSystemAclProtection
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/
			
			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{

				Console.WriteLine("Running SetSystemAclProtectionTestCases");
				
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
				reader.OpenTestCaseStore( SetSystemAclProtectionTestCasesStore); 

				while (null != (testCase = reader.ReadNextTestCase()) )
				{

					//read isContainer
					bool isContainer;
					if (1 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isContainer = bool.Parse(testCase[0]);

					//read isDS
					bool isDS;
					if (2 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isDS = bool.Parse(testCase[1]);
																	
					// read sddl
					string sddl;
					if (3 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[2])
						sddl = null;
					else
						sddl = testCase[2];

					//read isProtected
					bool isProtected;
					if(4 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isProtected = bool.Parse(testCase[3]);

					//read preserveInheritance
					bool preserveInheritance;
					if(5 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						preserveInheritance = bool.Parse(testCase[4]);

					//read verifierSddl
					string verifierSddl;
					if(6 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[5])
						verifierSddl = null;
					else
						verifierSddl = testCase[5];

					testCasesPerformed ++;

					try
					{
						if (TestSetSystemAclProtection(isContainer, isDS, sddl, isProtected, preserveInheritance, verifierSddl))
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


					bvtCaseCounter ++;
					if(isBVT && (bvtCaseCounter == 3))
					{
						//first 3 cases are BVT cases
						break;
					}						
				}
				reader.CloseTestCaseStore();
			}

			public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				
				Console.WriteLine("Running AdditionalTestCases");


				CommonSecurityDescriptor sd = null;
				
                            // test case 1: SACL is null, isProtected is true, preserveInheritance is true
                            testCasesPerformed ++;

				try
				{
					sd = new CommonSecurityDescriptor(true, false, (ControlFlags)0, null, null, null, null);
					sd.SetSystemAclProtection(true, true);
					if((sd.ControlFlags & ControlFlags.SystemAclProtected) != 0)
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;						
					}
					else
					{
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
					}
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);					
				}


                            // test case 2: SACL is null, isProtected is true, preserveInheritance is false
                            testCasesPerformed ++;

				try
				{
					sd = new CommonSecurityDescriptor(true, false, (ControlFlags)0, null, null, null, null);
					sd.SetSystemAclProtection(true, false);
					if((sd.ControlFlags & ControlFlags.SystemAclProtected) != 0)
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;						
					}
					else
					{
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
					}
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);					
				}


                            // test case 3: SACL is null, isProtected is false, preserveInheritance is true
                            testCasesPerformed ++;

				try
				{
					sd = new CommonSecurityDescriptor(true, false, (ControlFlags)0, null, null, null, null);
					sd.SetSystemAclProtection(false, true);
					if((sd.ControlFlags & ControlFlags.SystemAclProtected) == 0)
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;						
					}
					else
					{
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
					}
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);					
				}


                            // test case 4: SACL is null, isProtected is false, preserveInheritance is false
                            testCasesPerformed ++;

				try
				{
					sd = new CommonSecurityDescriptor(true, false, (ControlFlags)0, null, null, null, null);
					sd.SetSystemAclProtection(false, false);
					if((sd.ControlFlags & ControlFlags.SystemAclProtected) == 0)
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;						
					}
					else
					{
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
					}
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);					
				}


			}

			/*
			* Method Name: TestSetSystemAclProtection
			*
			* Description:	check test case passes or fails  
			*
			* Parameter:	isContainer, isDS, sddl -- parameters passed to CommonSecurityDescriptor method CreateFromSddlForm to create a CommonSecurityDescriptor object
			*			isProtected, preserveInheritance --  value passed to the CommonSecurityDescriptor method SetSystemAclProtection and used for validation
			*			verifierSddl -- the validation sddl string
			*
			* Return:		true if test pass, false otherwise
			*/

			private static bool TestSetSystemAclProtection(bool isContainer, bool isDS, string sddl, bool isProtected, bool preserveInheritance, string verifierSddl)
			{
				CommonSecurityDescriptor commonSecurityDescriptor = null;
				string resultSddl = null;
				bool result = false;

				Console.WriteLine("isContainer: {0}, isDS: {1}, sddl: {2}, isProtected: {3}, preserveInheritance: {4}", isContainer, isDS, sddl, isProtected, preserveInheritance);
				commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, sddl);					   
				commonSecurityDescriptor.SetSystemAclProtection(isProtected, preserveInheritance);

				resultSddl = commonSecurityDescriptor.GetSddlForm(AccessControlSections.All);


				if(!isProtected && (commonSecurityDescriptor.ControlFlags & ControlFlags.SystemAclProtected) == 0)
				{
					if (resultSddl != verifierSddl)
					{
						Console.WriteLine("Expected sddl form: {0}, actual result : {1}", verifierSddl, resultSddl);
						result = false;
					}
					else
						result = true;
				}
				else if (isProtected && (commonSecurityDescriptor.ControlFlags & ControlFlags.SystemAclProtected) != 0)
				{
					if (resultSddl != verifierSddl)
					{
						Console.WriteLine("Expected sddl form: {0}, actual result : {1}", verifierSddl, resultSddl);
						result = false;
					}
					else
						result = true;						
				}
				else
				{
					Console.WriteLine("SystemAclProtected ControlFlags is not set correctly");							
					result = false;
				}

				return result;
			}			

		}
	

		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test CommonSecurityDescriptor property
		*		public void SetDiscretionaryAclProtection( bool isProtected, bool preserveInheritance )
		*
		*
		*/
		//----------------------------------------------------------------------------------------------

		private class SetDiscretionaryAclProtectionTestCases
		{
			/*
			* Constructor
			*
			*/
			private SetDiscretionaryAclProtectionTestCases(){}

			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			SetDiscretionaryAclProtection
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/
			
			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{

				Console.WriteLine("Running SetDiscretionaryAclProtectionTestCases");
				
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
				reader.OpenTestCaseStore(SetDiscretionaryAclProtectionTestCasesStore); 

				while (null != (testCase = reader.ReadNextTestCase()) )
				{
					//read isContainer
					bool isContainer;
					if (1 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isContainer = bool.Parse(testCase[0]);

					//read isDS
					bool isDS;
					if (2 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isDS = bool.Parse(testCase[1]);
					
					// read sddl
					string sddl;
					if (3 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[2])
						sddl = null;
					else
						sddl = testCase[2];

					//read isPretected
					bool isProtected;
					if(4 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isProtected = bool.Parse(testCase[3]);

					//read preserveInheritance
					bool preserveInheritance;
					if(5 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						preserveInheritance = bool.Parse(testCase[4]);

					//read verifierSddl
					string verifierSddl;
					if(6 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if("null" == testCase[5])
						verifierSddl = null;
					else					
						verifierSddl = testCase[5];

					testCasesPerformed ++;

					try
					{
						if (TestSetDiscretionaryAclProtection(isContainer, isDS, sddl, isProtected, preserveInheritance, verifierSddl))
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


					bvtCaseCounter ++;
					if(isBVT && (bvtCaseCounter == 3))
					{
						//first 3 cases are BVT cases
						break;
					}					
				}
				reader.CloseTestCaseStore();
			}

			public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				// No test cases yet
			}

			/*
			* Method Name: TestSetDiscretionaryAclProtection
			*
			* Description:	check test case passes or fails  
			*
			* Parameter:	isContainer, isDS, sddl -- parameters passed to CommonSecurityDescriptor method CreateFromSddlForm to create a CommonSecurityDescriptor object
			*			isProtected, preserveInheritance --  value passed to the CommonSecurityDescriptor method SetDiscretionaryAclProtection and used for validation
			*			verifierSddl -- the validation sddl string
			*
			* Return:		true if test pass, false otherwise
			*/
			private static bool TestSetDiscretionaryAclProtection(bool isContainer, bool isDS, string sddl, bool isProtected, bool preserveInheritance, string verifierSddl)
			{

				CommonSecurityDescriptor commonSecurityDescriptor = null;
				string resultSddl = null;
				bool result = false;

				Console.WriteLine("isContainer: {0}, isDS: {1}, sddl: {2}, isProtected: {3}, preserveInheritance: {4}", isContainer, isDS, sddl, isProtected, preserveInheritance);

				commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, sddl);					   
				commonSecurityDescriptor.SetDiscretionaryAclProtection(isProtected, preserveInheritance);

				resultSddl = commonSecurityDescriptor.GetSddlForm(AccessControlSections.All);

				if(!isProtected && (commonSecurityDescriptor.ControlFlags & ControlFlags.DiscretionaryAclProtected) == 0)
				{
					
					if (resultSddl != verifierSddl)
					{
						Console.WriteLine("Expected sddl form: {0}, actual result : {1}", verifierSddl, resultSddl);
						return false;
					}
					else
						result = true;						
				}
				else if (isProtected && (commonSecurityDescriptor.ControlFlags & ControlFlags.DiscretionaryAclProtected) != 0)
				{
					if (resultSddl != verifierSddl)
					{
						Console.WriteLine("Expected sddl form: {0}, actual result : {1}", verifierSddl, resultSddl);
						result = false;
					}
					else
						result = true;						
				}
				else
				{
					Console.WriteLine("DiscretionaryAclProtected ControlFlags is not set correctly");							
					result = false;
				}

				return result;				
			}			

		}


		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test CommonSecurityDescriptor property
		*		public void PurgeAudit( SecurityIdentifier sid )
		*
		*
		*/
		//----------------------------------------------------------------------------------------------

		private class PurgeAuditTestCases
		{
			/*
			* Constructor
			*
			*/
			private PurgeAuditTestCases(){}

			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			PurgeAudit
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/
			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{

				Console.WriteLine("Running PurgeAuditTestCases");
				
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
				reader.OpenTestCaseStore(PurgeAuditTestCasesStore); 
				while (null != (testCase = reader.ReadNextTestCase()))
				{
					//read isContainer
					bool isContainer;
					if (1 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isContainer = bool.Parse(testCase[0]);

					//read isDS
					bool isDS;
					if (2 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isDS = bool.Parse(testCase[1]);
					
					// read sddl
					string sddl;
					if (3 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[0])
						sddl = null;
					else
						sddl = testCase[2];

					// read sid
					string sid;
					if (4 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[1])
						sid = null;
					else
						sid = testCase[3];

					// read verifierSddl
					string verifierSddl;
					if(5 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if("null" == testCase[2])
						verifierSddl = null;
					else						
						verifierSddl = testCase[4];
					
					testCasesPerformed ++;

					try
					{
						if (TestPurgeAudit(isContainer, isDS, sddl, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), verifierSddl))
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


					bvtCaseCounter ++;
					if(isBVT && (bvtCaseCounter == 3))
					{
						//first 3 cases are BVT cases
						break;
					}					
				}
				reader.CloseTestCaseStore();
			}

			public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running AdditionalTestCases");


				CommonSecurityDescriptor sd = null;
				
                            // test case 1: sid is null
                            testCasesPerformed ++;

				try
				{
					sd = new CommonSecurityDescriptor(true, false, (ControlFlags)0, null, null, null, null);
					sd.PurgeAudit(null);

					// expect to throw exception but not
					Console.WriteLine("Should not allow purge null sid");				
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
				}
				catch(ArgumentNullException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;
				}							
				
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);					
				}


			}

			/*
			* Method Name: TestPurgeAudit
			*
			* Description:	check test case passes or fails  
			*
			* Parameter:	isContainer, isDS, sddl -- parameters passed to CommonSecurityDescriptor method CreateFromSddlForm to create a CommonSecurityDescriptor object
			*			sid --  value passed to the CommonSecurityDescriptor method PurgeAudit and used for validation
			*			verifierSddl -- the validation sddl string
			*
			* Return:		true if test pass, false otherwise
			*/
			private static bool TestPurgeAudit(bool isContainer, bool isDS, string sddl, SecurityIdentifier sid, string verifierSddl)
			{

				CommonSecurityDescriptor commonSecurityDescriptor = null;
				string resultSddl = null;

				Console.WriteLine("isContainer: {0}, isDS: {1}, sddl: {2}, sid: {3}", isContainer, isDS, sddl, sid);

				commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, sddl);					   
				commonSecurityDescriptor.PurgeAudit(sid);

				resultSddl = commonSecurityDescriptor.GetSddlForm(AccessControlSections.All);
				if (resultSddl != verifierSddl)
				{
					Console.WriteLine("Expected sddl form: {0}, actual result : {1}", verifierSddl, resultSddl);
					return false;
				}

				return true;
			}
		}

		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test CommonSecurityDescriptor property
		*		public void PurgeAccessControl( SecurityIdentifier sid )
		*
		*
		*/
		//----------------------------------------------------------------------------------------------

		private class PurgeAccessControlTestCases
		{
			/*
			* Constructor
			*
			*/
			private PurgeAccessControlTestCases(){}


			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			PurgeAccessControl
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/
			
			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{

				Console.WriteLine("Running PurgeAccessControlTestCases");
				
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
				reader.OpenTestCaseStore(PurgeAccessControlTestCasesStore); 
				while (null != (testCase = reader.ReadNextTestCase()))
				{
					//read isContainer
					bool isContainer;
					if (1 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isContainer = bool.Parse(testCase[0]);

					//read isDS
					bool isDS;
					if (2 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isDS = bool.Parse(testCase[1]);
					
					// read sddl
					string sddl;
					if (3 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[0])
						sddl = null;
					else
						sddl = testCase[2];

					// read sid
					string sid;
					if (4 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[1])
						sid = null;
					else
						sid = testCase[3];

					// read verifierSddl
					string verifierSddl;
					if(5 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if("null" == testCase[2])
						verifierSddl = null;
					else						
						verifierSddl = testCase[4];
					
					testCasesPerformed ++;

					try
					{
						if (TestPurgeAccessControl(isContainer, isDS, sddl, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), verifierSddl))
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


					bvtCaseCounter ++;
					if(isBVT && (bvtCaseCounter == 3))
					{
						//first 3 cases are BVT cases
						break;
					}						
				}
				reader.CloseTestCaseStore();
			}

			public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running AdditionalTestCases");


				CommonSecurityDescriptor sd = null;
				
                            // test case 1: sid is null
                            testCasesPerformed ++;

				try
				{
					sd = new CommonSecurityDescriptor(true, false, (ControlFlags)0, null, null, null, null);
					sd.PurgeAccessControl(null);

					// expect to throw exception but not
					Console.WriteLine("Should not allow purge null sid");				
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
				}
				catch(ArgumentNullException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;
				}							
				
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);					
				}


			}

			/*
			* Method Name: TestPurgeAccessControl
			*
			* Description:	check test case passes or fails  
			*
			* Parameter:	isContainer, isDS, sddl -- parameters passed to CommonSecurityDescriptor method CreateFromSddlForm to create a CommonSecurityDescriptor object
			*			sid --  value passed to the CommonSecurityDescriptor method PurgeAccessControl and used for validation
			*			verifierSddl -- the validation sddl string
			*
			* Return:		true if test pass, false otherwise
			*/
			private static bool TestPurgeAccessControl(bool isContainer, bool isDS, string sddl, SecurityIdentifier sid, string verifierSddl)
			{

				CommonSecurityDescriptor commonSecurityDescriptor = null;
				string resultSddl = null;

				Console.WriteLine("isContainer: {0}, isDS: {1}, sddl: {2}, sid: {3}", isContainer, isDS, sddl, sid);

				commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, sddl);					   
				commonSecurityDescriptor.PurgeAccessControl(sid);

				resultSddl = commonSecurityDescriptor.GetSddlForm(AccessControlSections.All);
				if (resultSddl != verifierSddl)
				{
					Console.WriteLine("Expected sddl form: {0}, actual result : {1}", verifierSddl, resultSddl);
					return false;
				}

				return true;
			}
		}

		//#####below two methods have been changed to properties by OM changes
		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test CommonSecurityDescriptor property
		*		public bool WasSystemAclCanonicalInitially()
		*
		*
		*/
		//----------------------------------------------------------------------------------------------


		private class WasSystemAclCanonicalInitiallyTestCases
		{
			/*
			* Constructor
			*
			*/
			private WasSystemAclCanonicalInitiallyTestCases(){}

			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			WasSystemAclCanonicalInitially
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/
			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{

				Console.WriteLine("Running WasSystemAclCanonicalInitiallyTestCases");
				
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
				reader.OpenTestCaseStore(WasSystemAclCanonicalInitiallyTestCasesStore); 
				while (null != (testCase = reader.ReadNextTestCase()))
				{
					//read isContainer
					bool isContainer;
					if (1 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isContainer = bool.Parse(testCase[0]);

					//read isDS
					bool isDS;
					if (2 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isDS = bool.Parse(testCase[1]);
					
					// read sddl
					string sddl;
					if (3 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						sddl = testCase[2];

					// read wasCanonicalInitially
					bool verifierWasCanonicalInitially;
					if (4 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						verifierWasCanonicalInitially = bool.Parse(testCase[3]);
					
					testCasesPerformed ++;

					try
					{
						if (TestWasSystemAclCanonicalInitially(isContainer, isDS, sddl, verifierWasCanonicalInitially))
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


					bvtCaseCounter ++;
					if(isBVT && (bvtCaseCounter == 3))
					{
						//first 3 cases are BVT cases
						break;
					}						
				}
				reader.CloseTestCaseStore();
			}

			public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				// No test cases yet
			}

			/*
			* Method Name: TestWasSystemAclCanonicalInitially
			*
			* Description:	check test case passes or fails  
			*
			* Parameter:	isContainer, isDS, sddl -- parameters passed to CommonSecurityDescriptor method CreateFromSddlForm to create a CommonSecurityDescriptor object
			*			verifierWasCanonicalInitially -- the validation value for WasSystemAclCanonicalInitially
			*
			* Return:		true if test pass, false otherwise
			*/
			private static bool TestWasSystemAclCanonicalInitially(bool isContainer, bool isDS, string sddl, bool verifierWasCanonicalInitially)
			{

				CommonSecurityDescriptor commonSecurityDescriptor = null;

				Console.WriteLine("isContainer: {0}, isDS: {1}, sddl: {2}", isContainer, isDS, sddl);


				commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, sddl);					   

				if (verifierWasCanonicalInitially != commonSecurityDescriptor.IsSystemAclCanonical)
				{
					Console.WriteLine("Expected WasCanonicalInitially: {0}, actual result : {1}", verifierWasCanonicalInitially, commonSecurityDescriptor.IsSystemAclCanonical);
					return false;
				}

				return true;
			}
		}


		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test CommonSecurityDescriptor property
		*		public bool WasSystemAclCanonicalInitially()
		*
		*
		*/
		//----------------------------------------------------------------------------------------------

		private class WasDiscretionaryAclCanonicalInitiallyTestCases
		{
			/*
			* Constructor
			*
			*/
			private WasDiscretionaryAclCanonicalInitiallyTestCases(){}


			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			WasSystemAclCanonicalInitially
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/
			
			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{

				Console.WriteLine("Running WasDiscretionaryAclCanonicalInitiallyTestCases");
				
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
				reader.OpenTestCaseStore(WasDiscretionaryAclCanonicalInitiallyTestCasesStore); 
				while (null != (testCase = reader.ReadNextTestCase()))
				{
					//read isContainer
					bool isContainer;
					if (1 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isContainer = bool.Parse(testCase[0]);

					//read isDS
					bool isDS;
					if (2 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isDS = bool.Parse(testCase[1]);
					
					// read sddl
					string sddl;
					if (3 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						sddl = testCase[2];

					// read wasCanonicalInitially
					bool verifierWasCanonicalInitially;
					if (4 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						verifierWasCanonicalInitially = bool.Parse(testCase[3]);
					
					testCasesPerformed ++;

					try
					{
						if (TestWasDiscretionaryAclCanonicalInitially(isContainer, isDS, sddl, verifierWasCanonicalInitially))
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


					bvtCaseCounter ++;
					if(isBVT && (bvtCaseCounter == 3))
					{
						//first 3 cases are BVT cases
						break;
					}					
				}
				reader.CloseTestCaseStore();
			}

			public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				// No test cases yet
			}

			/*
			* Method Name: TestWasDiscretionaryAclCanonicalInitially
			*
			* Description:	check test case passes or fails  
			*
			* Parameter:	isContainer, isDS, sddl -- parameters passed to CommonSecurityDescriptor method CreateFromSddlForm to create a CommonSecurityDescriptor object
			*			verifierWasCanonicalInitially -- the validation value for WasDiscretionaryAclCanonicalInitially
			*
			* Return:		true if test pass, false otherwise
			*/
			private static bool TestWasDiscretionaryAclCanonicalInitially(bool isContainer, bool isDS, string sddl, bool verifierWasCanonicalInitially)
			{

				CommonSecurityDescriptor commonSecurityDescriptor = null;
				Console.WriteLine("isContainer: {0}, isDS: {1}, sddl: {2}", isContainer, isDS, sddl);

				commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, sddl);					   

				if (verifierWasCanonicalInitially != commonSecurityDescriptor.IsDiscretionaryAclCanonical)
				{
					Console.WriteLine("Expected WasCanonicalInitially: {0}, actual result : {1}", verifierWasCanonicalInitially, commonSecurityDescriptor.IsDiscretionaryAclCanonical);
					return false;
				}

				return true;
			}
		}

		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test CommonSecurityDescriptor property inherited from abstract base class GenericSecurityDescriptor
		* 		public int BinaryLength
		*
		*
		*/
		//----------------------------------------------------------------------------------------------

		private class BinaryLengthTestCases
		{

			/*
			* Constructor
			*
			*/
			private BinaryLengthTestCases(){}



			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			CommonSecurityDescriptor property BinaryLength
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/
			
			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{

				Console.WriteLine("Running BinaryLengthTestCases");				
				BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, false);
				AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
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

			public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed, bool isBVT)
			{
				Console.WriteLine("Running BasicValidationTestCases");
				

				int bvtCaseCounter = 0;				
				
				bool isContainer = false;
				bool isDS = false;
				int flags = 0;
				string ownerStr = null;
				string groupStr = null;
				string saclStr = null;
				string daclStr = null;
				
				string[] testCase = null;
				ITestCaseReader reader = new InfTestCaseStore();
				reader.OpenTestCaseStore(BinaryLengthTestCasesStore); 
				while (null != (testCase = reader.ReadNextTestCase()))
				{
					//read isContainer
					if (1 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isContainer = bool.Parse(testCase[0]);

					//read isDS
					if (2 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isDS = bool.Parse(testCase[1]);
												
					// read flags
					if (3 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						flags = int.Parse(testCase[2]);

					// read ownerStr
					if (4 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[3])
						ownerStr = null;
					else
						ownerStr = testCase[3];

					// read groupStr
					if (5 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[4])
                                          groupStr = null;
                                   else
                                          groupStr = testCase[4];

					// read saclStr
					if (6 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[5])
						saclStr = null;
                                   else
                                          saclStr = testCase[5];

					// read daclStr
					if (7 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[6])
						daclStr = null;
                                   else
                                          daclStr = testCase[6];
                                   
					testCasesPerformed ++;

					try
					{
						if (TestBinaryLength(isContainer, isDS, flags, ownerStr, groupStr, saclStr, daclStr))
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


					bvtCaseCounter ++;
					if(isBVT && (bvtCaseCounter == 3))
					{
						//first 3 cases are BVT cases
						break;
					}					
				}
				reader.CloseTestCaseStore();
			}

			public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				// No test cases yet
			}



			/*
			* Method Name: TestBinaryLength
			*
			* Description:	check test case passes or fails  
			*
			* Parameter:	flags, ownerStr, groupStr, saclStr, daclStr -- parameters used to create CommonSecurityDescriptor
			*
			* Return:		true if test pass, false otherwise
			*/
			private static bool TestBinaryLength(bool isContainer, bool isDS, int flags, string ownerStr, string groupStr, string saclStr, string daclStr)
			{							
				CommonSecurityDescriptor commonSecurityDescriptor = null;
				bool result = false;

				Console.WriteLine("isContainer: {0}, isDS: {1}, flags: {2}, owner: {3}, group: {4}, sacl: {5}, dacl: {6}", 
					isContainer, isDS, flags, ownerStr, groupStr, saclStr, daclStr);
						
				ControlFlags controlFlags = ControlFlags.OwnerDefaulted;
				SecurityIdentifier owner = null;
				SecurityIdentifier group = null;
				RawAcl rawAcl = null;
				SystemAcl sacl = null;
				DiscretionaryAcl dacl = null;				

				//int verifierBinaryLength = 20; //intialize the binary length to header length
				int verifierBinaryLength = 0;

				controlFlags = (ControlFlags) flags;
				owner = (ownerStr != null) ? new SecurityIdentifier(ownerStr) : null;
				group = (groupStr != null) ? new SecurityIdentifier (groupStr) : null;
				
				rawAcl = (saclStr != null) ? Utils.UtilCreateRawAclFromString(saclStr) : null;
				if(rawAcl == null)
					sacl = null;
				else
					sacl = new SystemAcl(isContainer, isDS, rawAcl);
					
				rawAcl = (daclStr != null) ? Utils.UtilCreateRawAclFromString(daclStr) : null;
				if(rawAcl == null)
					dacl = null;
				else
					dacl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
										
				commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS,  controlFlags, owner, group, sacl, dacl);
								
				// verify the result
				verifierBinaryLength = UtilsComputeBinaryLength( commonSecurityDescriptor, dacl != null);
				if(verifierBinaryLength == commonSecurityDescriptor.BinaryLength) 
				{
					result = true;
				}
				else
				{
					Console.WriteLine("Expected BinaryLength: {0}, actual BinaryLength : {1}", verifierBinaryLength, commonSecurityDescriptor.BinaryLength);						
					result = false;
				}					

				return result;
				
			}
		}


		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test CommonSecurityDescriptor method inherited from abstract base class GenericSecurityDescriptor
		* 		public string GetSddlForm( bool owner, bool group, bool systemAcl, bool discretionaryAcl )
		*
		*
		*/
		//----------------------------------------------------------------------------------------------


		private class GetSddlFormTestCases
		{

			private GetSddlFormTestCases(){}

	
			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running GetSddlFormTestCases");
				BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, false);
				AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
			}

			public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed, bool isBVT)
			{
				Console.WriteLine("Running BasicValidationTestCases");
				

				int bvtCaseCounter = 0;				

				bool isContainer = false;
				bool isDS = false;
				int flags = 0;
				string ownerStr = null;
				string groupStr = null;
				string saclStr = null;
				string daclStr = null;
				
				bool getOwner = false;
				bool getGroup = false;
				bool getSacl = false;
				bool getDacl = false;
				
				string expectedSddl;
				
				
				string[] testCase = null;
				ITestCaseReader reader = new InfTestCaseStore();
				reader.OpenTestCaseStore(GetSddlFormTestCasesStore); 
				while (null != (testCase = reader.ReadNextTestCase()))
				{
					//--first part is to read the data to create the CommonSecurityDescriptor object

					//read isContainer
					if (1 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isContainer = bool.Parse(testCase[0]);

					//read isDS
					if (2 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isDS = bool.Parse(testCase[1]);
												
					// read flags
					if (3 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						flags = int.Parse(testCase[2]);

					// read ownerStr
					if (4 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[3])
						ownerStr = null;
					else
						ownerStr = testCase[3];

					// read groupStr
					if (5 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[4])
                                          groupStr = null;
                                   else
                                          groupStr = testCase[4];

					// read saclStr
					if (6 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[5])
						saclStr = null;
                                   else
                                          saclStr = testCase[5];

					// read daclStr
					if (7 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[6])
						daclStr = null;
                                   else
                                          daclStr = testCase[6];

					//--second part is to read which component of the SD is requested
					// read getOwner
					if (8 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
                                   else
                                          getOwner = bool.Parse(testCase[7]);
								   
					// read getGroup
					if (9 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
                                   else
                                          getGroup = bool.Parse(testCase[8]);

					// read getSacl
					if (10 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
                                   else
                                          getSacl = bool.Parse(testCase[9]);

					// read getDacl
					if (11 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
                                   else
                                          getDacl = bool.Parse(testCase[10]);
								   
					//--third part is to read the verification SD							   
					if (12 > testCase.Length)
						//sometimes the expected sddl form is empty
                                          expectedSddl = "";
					else if ("null" == testCase[11])
						expectedSddl = null;
                                   else
                                          expectedSddl = testCase[11];
                                   
					testCasesPerformed ++;


					try
					{
						if (TestGetSddlForm(isContainer, isDS, flags, ownerStr, groupStr, saclStr, daclStr, getOwner, getGroup, getSacl, getDacl, expectedSddl))
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


					bvtCaseCounter ++;
					if(isBVT && (bvtCaseCounter == 3))
					{
						//first 3 cases are BVT cases
						break;
					}						
				}
				reader.CloseTestCaseStore();
			}

			public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				// No test cases yet
			}

			private static bool TestGetSddlForm(bool isContainer, bool isDS, int flags, string ownerStr, string groupStr, string saclStr, string daclStr, bool getOwner, bool getGroup, bool getSacl, bool getDacl, string expectedSddl)
			{

				CommonSecurityDescriptor commonSecurityDescriptor = null;
				string resultSddl = null;
						
				ControlFlags controlFlags = ControlFlags.OwnerDefaulted;
				SecurityIdentifier owner = null;
				SecurityIdentifier group = null;
				RawAcl rawAcl = null;
				SystemAcl sacl = null;
				DiscretionaryAcl dacl = null;	

				AccessControlSections accControlSections = AccessControlSections.None;
				

				Console.WriteLine("isContainer: {0}, isDS: {1}, flags: {2}, ownerStr: {3}, groupStr: {4}, saclStr: {5}, daclStr: {6}",
					isContainer, isDS, flags, ownerStr, groupStr, saclStr, daclStr);
				
				controlFlags = (ControlFlags) flags;
				owner = (ownerStr != null) ? new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(ownerStr)) : null;
				group = (groupStr != null) ? new SecurityIdentifier (Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(groupStr)) : null;
					
				rawAcl = (saclStr != null) ? Utils.UtilCreateRawAclFromString(saclStr) : null;
				if(rawAcl == null)
					sacl = null;
				else
					sacl = new SystemAcl(isContainer, isDS, rawAcl);
					
				rawAcl = (daclStr != null) ? Utils.UtilCreateRawAclFromString(daclStr) : null;
				if(rawAcl == null)
					dacl = null;
				else
					dacl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
										
				commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS,  controlFlags, owner, group, sacl, dacl);

				if(getOwner)
					accControlSections |= AccessControlSections.Owner;
				if(getGroup)
					accControlSections |= AccessControlSections.Group;
				if(getSacl)
					accControlSections |= AccessControlSections.Audit;
				if(getDacl)
					accControlSections |= AccessControlSections.Access;
				
				resultSddl = commonSecurityDescriptor.GetSddlForm(accControlSections);

				if(expectedSddl == null || resultSddl == null)
				{
					if(expectedSddl == null && resultSddl == null)
					{
						return true;
					}
					else
					{
						Console.WriteLine("Expected sddl form: {0}, actual result : {1}", expectedSddl, resultSddl);							
						return false;
					}
				}
                else if (String.Compare(expectedSddl, resultSddl, StringComparison.CurrentCultureIgnoreCase) == 0)
				{
					return true;
				}
				else
				{
					Console.WriteLine("Expected sddl form: {0}, actual result : {1}", expectedSddl, resultSddl);						
					return false;
				}			
			}
		}


		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test CommonSecurityDescriptor method inherited from abstract base class GenericSecurityDescriptor
		* 		public void GetBinaryForm( byte[] binaryForm, int offset )
		*
		*
		*/
		//----------------------------------------------------------------------------------------------

		private class GetBinaryFormTestCases
		{
			/*
			* Constructor
			*
			*/
			private GetBinaryFormTestCases(){}

			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			CommonSecurityDescriptor method GetBinaryForm
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/

			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{

				Console.WriteLine("Running GetBinaryFormTestCases");
				BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, false);
				AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
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
			public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed, bool isBVT)
			{
				Console.WriteLine("Running BasicValidationTestCases");
				

				int bvtCaseCounter = 0;				
				
				string[] testCase = null;
				ITestCaseReader reader = new InfTestCaseStore();
				reader.OpenTestCaseStore(GetBinaryFormTestCasesStore); 
				while (null != (testCase = reader.ReadNextTestCase()))
				{
					//read isContainer
					bool isContainer;
					if (1 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isContainer = bool.Parse(testCase[0]);

					//read isDS
					bool isDS;
					if (2 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isDS = bool.Parse(testCase[1]);
					
					// read sddl
					string sddl;
					if (3 > testCase.Length)
                                          sddl = "";
					else if ("null" == testCase[2])
						sddl = null;
                                   else
                                          sddl = testCase[2];

					// read verifierSddl
					string verifierSddl;
					if (4 > testCase.Length)
                                          verifierSddl = "";
					else if ("null" == testCase[3])
						verifierSddl = null;
                                   else
                                          verifierSddl = testCase[3];								   

					// read offset
					int offset = 0;
					if (5 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						offset = int.Parse(testCase[4]);
                                   
					testCasesPerformed ++;

					try
					{
						if (TestGetBinaryForm(isContainer, isDS, sddl, verifierSddl, offset))
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


					bvtCaseCounter ++;
					if(isBVT && (bvtCaseCounter == 3))
					{
						//first 3 cases are BVT cases
						break;
					}						
				}
				reader.CloseTestCaseStore();
			}

			public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				// test case will be passing in binaryForm array that is null, length is invalid and offset is invalid
				CommonSecurityDescriptor commonSecurityDescriptor = null;
				string sddl =  null;
				byte [] binaryForm = null;

				Console.WriteLine("Running AdditionalTestCases");
				

	

				//Case 1, null byte array
				testCasesPerformed++;

				try
				{	
					sddl = "O:LAG:SYD:AI(A;ID;FA;;;BA)(A;ID;FA;;;BG)(A;ID;FA;;;SY)";
					commonSecurityDescriptor = new CommonSecurityDescriptor(true, false, sddl);
					binaryForm = null;
					commonSecurityDescriptor.GetBinaryForm(binaryForm, 0);
					Console.WriteLine("Should not allow calling GetBinaryForm with null byte array");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
				}
				catch (ArgumentNullException )
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}



				
				//case 2, empty byte array
				testCasesPerformed++;

				try
				{
					sddl = "O:LAG:SYD:AI(A;ID;FA;;;BA)(A;ID;FA;;;BG)(A;ID;FA;;;SY)";
					commonSecurityDescriptor = new CommonSecurityDescriptor(true, false, sddl);					
					binaryForm = new byte[0];
					commonSecurityDescriptor.GetBinaryForm(binaryForm, 0);
					Console.WriteLine("Should not allow calling GetBinaryForm with empty byte array");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
				}
				catch(ArgumentOutOfRangeException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;					
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}



				
				//case 3, negative offset 				
				testCasesPerformed++;

				try
				{
					sddl = "O:LAG:SYD:AI(A;ID;FA;;;BA)(A;ID;FA;;;BG)(A;ID;FA;;;SY)";					
					commonSecurityDescriptor = new CommonSecurityDescriptor(true, false, sddl);			
					binaryForm = new byte[100];
					commonSecurityDescriptor.GetBinaryForm(binaryForm, -1);
					Console.WriteLine("Should not allow calling GetBinaryForm with negative offset");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
				}
				catch(ArgumentOutOfRangeException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;					
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}



				//case 4, binaryForm.Length - offset < BinaryLength
				testCasesPerformed++;

				try
				{
					sddl = "O:LAG:SYD:AI(A;ID;FA;;;BA)(A;ID;FA;;;BG)(A;ID;FA;;;SY)";
					commonSecurityDescriptor = new CommonSecurityDescriptor(true, false, sddl);					
					binaryForm = new byte[commonSecurityDescriptor.BinaryLength];
					commonSecurityDescriptor.GetBinaryForm(binaryForm, 8);
					Console.WriteLine("Should not allow GetBinaryForm with insufficient array length");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
				}
				catch(ArgumentOutOfRangeException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;					
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


			}

			/*
			* Method Name: TestGetSddlForm
			*
			* Description:	check test case passes or fails  
			*
			* Parameter:	sddl -- the sddl string used to create the CommonSecurityDescriptor
			*			offset -- the start index of the byte array passed to GetBinaryForm
			*
			* Return:		true if test pass, false otherwise
			*/
			private static bool TestGetBinaryForm(bool isContainer, bool isDS, string sddl, string verifierSddl, int offset)
			{				
				CommonSecurityDescriptor commonSecurityDescriptor = null;
				CommonSecurityDescriptor verifierCommonSecurityDescriptor = null;
				string resultSddl = null;

				Console.WriteLine("isContainer: {0}, isDS: {1}, sddl: {2}", isContainer, isDS, sddl);


				commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, sddl);

				byte[] binaryForm = new byte[commonSecurityDescriptor.BinaryLength + offset];
				commonSecurityDescriptor.GetBinaryForm(binaryForm, offset);
				verifierCommonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, binaryForm, offset);
				resultSddl = verifierCommonSecurityDescriptor.GetSddlForm(AccessControlSections.All);
					
				if(resultSddl == null || verifierSddl == null)
				{
					if(resultSddl == null && verifierSddl == null)
					{
						return true;
					}
					else
					{
						Console.WriteLine("Expected sddl form: {0}, actual result : {1}", verifierSddl, resultSddl);							
						return false;
					}
				}
                else if (String.Compare(resultSddl, verifierSddl, StringComparison.CurrentCultureIgnoreCase) == 0)
				{
					return true;
				}
				else
				{
					Console.WriteLine("Expected sddl form: {0}, actual result : {1}", verifierSddl, resultSddl);						
					return false;
				}
					

				
			}						
		}
	}
}
