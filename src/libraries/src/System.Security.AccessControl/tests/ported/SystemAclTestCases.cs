//--------------------------------------------------------------------------
//
//		SystemAcl test cases
//
//		Tests the SystemAcl, the base abstract class GenericACL's and CommonAcl's functionality
//
//		Copyright (C) Microsoft Corporation, 2003
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
	*  Class to test SystemAcl and its abstract base classes CommonAcl and GenericAcl
	*
	*
	*/
	//----------------------------------------------------------------------------------------------
	
	public class SystemAclTestCases
	{
		public static readonly string Constructor1TestCaseStore = "TestCaseStores\\SystemAcl_Constructor1.inf";
        public static readonly string Constructor2TestCaseStore = "TestCaseStores\\SystemAcl_Constructor2.inf";
        public static readonly string Constructor3TestCaseStore = "TestCaseStores\\SystemAcl_Constructor3.inf";
        public static readonly string AddAuditTestCaseStore = "TestCaseStores\\SystemAcl_AddAudit.inf";
        public static readonly string SetAuditTestCaseStore = "TestCaseStores\\SystemAcl_SetAudit.inf";
        public static readonly string RemoveAuditTestCaseStore = "TestCaseStores\\SystemAcl_RemoveAudit.inf";
        public static readonly string RemoveAuditSpecificTestCaseStore = "TestCaseStores\\SystemAcl_RemoveAuditSpecific.inf";
		
		/*
		* Constructor
		*
		*/
		public SystemAclTestCases() {}

        public static Boolean Test()
        {

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

			Console.WriteLine("Running SystemAclTestCases");

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


			AddAuditTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


			SetAuditTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


			RemoveAuditTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


			RemoveAuditSpecificTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


			GetBinaryFormTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);

            return (testCasesPerformed == testCasesPassed);


		}

		public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
		{
			Console.WriteLine("Running SystemAclTestCases");
			
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

			Console.WriteLine("Running BinaryLengthTestCases");
			BinaryLengthTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);

			Console.WriteLine("Running AceCountTestCases");
			AceCountTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
			
			Console.WriteLine("Running IndexTestCases");
			IndexTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);

			Console.WriteLine("Running AddAuditTestCases");
			AddAuditTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

			Console.WriteLine("Running SetAuditTestCases");
			SetAuditTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);
				
			Console.WriteLine("Running RemoveAuditTestCases");
			RemoveAuditTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);
			
			Console.WriteLine("Running RemoveAuditSpecificTestCases");
			RemoveAuditSpecificTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);
			
			Console.WriteLine("Running GetBinaryFormTestCases.AllTestCases()");
			GetBinaryFormTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
		}

		public static void BVTTestCases(ref int testCasesPerformed, ref int testCasesPassed)
		{
			// No test cases yet
		}

		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test SystemAcl constructor 1
		*		public SystemAcl( bool isContainer, bool isDS, int capacity )
		*
		*
		*/
		//----------------------------------------------------------------------------------------------
	
		private class Constructor1TestCases
		{
			/*
			* Constructor
			*
			*/
			private Constructor1TestCases(){}

			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			System Acl constructor -- public SystemAcl( bool isContainer, bool isDS, int capacity )
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
                                   
					testCasesPerformed ++;


					try
					{
						if (TestConstructor(isContainer, isDS, capacity))
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
				SystemAcl systemAcl = null;
				bool isContainer  = false;
				bool isDS = false;
				int capacity = 0;
				
				Console.WriteLine("Running AdditionalTestCases");
				


				//case 1, capacity = -1
				testCasesPerformed ++;

				
				try
				{
					capacity = -1;
					systemAcl = new SystemAcl(isContainer, isDS, capacity);
					Console.WriteLine("Should not allow creation of negative capacity SystemAcl");
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



				//case 2, capacity = Int32.MaxValue/2
				testCasesPerformed ++;


				try
				{
					isContainer = true;
					isDS = false;
					capacity = Int32.MaxValue/2;
					if (TestConstructor(isContainer, isDS, capacity))	
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;		
					}
					else
					{
						Console.WriteLine("the new SystemAcl is not expected");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
					}					
					
				}
				catch(OutOfMemoryException)
				{//most possibably there are not enough memory
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;					
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}



				//case 3, capacity = Int32.MaxValue
				testCasesPerformed ++;

				
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
						Console.WriteLine("the new SystemAcl is not expected");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
					}					
					
				}
				catch(OutOfMemoryException)
				{//usually there are not enough memory
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
			* Parameter:	isContainer, isDS, capacity -- parameters to pass to the SystemAcl constructor
			*
			* Return:		true if test pass, false otherwise
			*/
			private static bool TestConstructor(bool isContainer, bool isDS, int capacity)
			{
				bool result = true;
				byte [] sAclBinaryForm = null;
				byte [] rAclBinaryForm = null;				
				
				SystemAcl systemAcl = null;
				RawAcl rawAcl = null;
				

				systemAcl = new SystemAcl(isContainer, isDS, capacity);
				rawAcl = new RawAcl(isDS?GenericAcl.AclRevisionDS : GenericAcl.AclRevision, capacity);
				if(isContainer == systemAcl.IsContainer &&
					isDS == systemAcl.IsDS &&
					(isDS ? GenericAcl.AclRevisionDS : GenericAcl.AclRevision) == systemAcl.Revision &&
					0 == systemAcl.Count &&
					8 == systemAcl.BinaryLength &&
					true == systemAcl.IsCanonical)
				{
					sAclBinaryForm = new byte[systemAcl.BinaryLength];
					rAclBinaryForm = new byte[rawAcl.BinaryLength];

					systemAcl.GetBinaryForm(sAclBinaryForm, 0);
					rawAcl.GetBinaryForm(rAclBinaryForm, 0);
					
					if(!Utils.UtilIsBinaryFormEqual(sAclBinaryForm, rAclBinaryForm))
						result = false;	

					//redundant index check
					for (int i = 0; i < systemAcl.Count; i++)
					{
						if(!Utils.UtilIsAceEqual(systemAcl[i], rawAcl[i]))
						{
							result = false;
							break;
						}
					}					
				}
				else
				{
					Console.WriteLine("the newly created SystemAcl is not equal to what we set");
					result = false;		
				}
				return result;
			}
		}

		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test SystemAcl constructor 2
		*		public SystemAcl( bool isContainer, bool isDS, byte revision, int capacity )
		*
		*
		*/
		//----------------------------------------------------------------------------------------------

		private class Constructor2TestCases
		{

			/*
			* Constructor
			*
			*/
			private Constructor2TestCases(){}

			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			System Acl constructor -- public SystemAcl( bool isContainer, bool isDS, byte revision, int capacity )
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
                                   
					testCasesPerformed ++;


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
				SystemAcl systemAcl = null;
				bool isContainer  = false;
				bool isDS = false;
				byte revision = 0;
				int capacity = 0;

				Console.WriteLine("Running AdditionalTestCases");
				


				//case 1, capacity = -1
				testCasesPerformed ++;

				
				try
				{				
					capacity = -1;
					systemAcl = new SystemAcl(isContainer, isDS, revision, capacity);
					Console.WriteLine("Should not allow creation of negative capacity SystemAcl");
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



				//case 2, capacity = Int32.MaxValue/2
				testCasesPerformed ++;

				
				try
				{
					isContainer = true;
					isDS = false;
					revision = 0;
					capacity = Int32.MaxValue/2;
					if (TestConstructor(isContainer, isDS, revision, capacity))
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;		
					}
					else
					{
						Console.WriteLine("the new SystemAcl is not expected");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
					}					
					
				}
				catch(OutOfMemoryException)
				{//most possibably there are not enough memory
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;					
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}



				//case 3, capacity = Int32.MaxValue
				testCasesPerformed ++;

				
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
						Console.WriteLine("the new SystemAcl is not expected");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
					}					
					
				}
				catch(OutOfMemoryException)
				{//usually there are not enough memory
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
			* Parameter:	isContainer, isDS, revision, capacity -- parameters to pass to the SystemAcl constructor
			*
			* Return:		true if test pass, false otherwise
			*/

			private static bool TestConstructor(bool isContainer, bool isDS, byte revision, int capacity)
			{
				bool result = true;
				byte [] sAclBinaryForm = null;
				byte [] rAclBinaryForm = null;
				
				SystemAcl systemAcl = null;
				RawAcl rawAcl = null;
				

				systemAcl = new SystemAcl(isContainer, isDS, revision, capacity);
				rawAcl = new RawAcl(revision, capacity);
				
				if(isContainer == systemAcl.IsContainer &&
					isDS == systemAcl.IsDS &&
					revision == systemAcl.Revision &&
					0 == systemAcl.Count &&
					8 == systemAcl.BinaryLength &&
					true == systemAcl.IsCanonical)
				{
					sAclBinaryForm = new byte[systemAcl.BinaryLength];
					rAclBinaryForm = new byte[rawAcl.BinaryLength];

					systemAcl.GetBinaryForm(sAclBinaryForm, 0);
					rawAcl.GetBinaryForm(rAclBinaryForm, 0);
					
					if(!Utils.UtilIsBinaryFormEqual(sAclBinaryForm, rAclBinaryForm))
						result = false;	

					//redundant index check
					for (int i = 0; i < systemAcl.Count; i++)
					{
						if(!Utils.UtilIsAceEqual(systemAcl[i], rawAcl[i]))
						{
							result = false;
							break;
						}
					}
					
				}
				else
				{
					Console.WriteLine("the newly created SystemAcl is not equal to what we set");
					result = false;
				}
				return result;
			}
		}

		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test SystemAcl constructor 3
		*		public SystemAcl( bool isContainer, bool isDS, RawAcl rawAcl )
		*
		*
		*/
		//----------------------------------------------------------------------------------------------

		private class Constructor3TestCases
		{
			/*
			* Constructor
			*
			*/
			private Constructor3TestCases(){}

			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			System Acl constructor -- public SystemAcl( bool isContainer, bool isDS, RawAcl rawAcl )
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/

			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running Constructor3TestCases");

				BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, false);
				AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
				Additional2TestCases(ref testCasesPerformed, ref testCasesPassed);
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
				
				RawAcl rawAcl = null;
				SystemAcl systemAcl = null;
				
				bool isContainer = false;
				bool isDS = false;
				bool wasCanonicalInitially = false;

				string initialRawAclStr = null;
				string verifierRawAclStr = null;				

				string[] testCase = null;
				ITestCaseReader reader = new InfTestCaseStore();
				reader.OpenTestCaseStore(Constructor3TestCaseStore); 

				while (null != (testCase = reader.ReadNextTestCase()))
				{
					// read isContainer
					if (1 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isContainer = bool.Parse(testCase[0]);

					// read isDS
					if (2 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isDS = bool.Parse(testCase[1]);

					//read initialRawAclStr
					if (3 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						initialRawAclStr = testCase[2];

					//read verifierRawAclStr
					if (4 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						verifierRawAclStr = testCase[3];					

					// read wasCanonicalInitially
					if (5 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						wasCanonicalInitially = bool.Parse(testCase[4]);
					
					//create a systemAcl
					rawAcl = Utils.UtilCreateRawAclFromString(initialRawAclStr);
					systemAcl = null;
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					rawAcl = Utils.UtilCreateRawAclFromString(verifierRawAclStr);
										
					testCasesPerformed ++;


					try
					{
						if (TestConstructor(systemAcl, isContainer, isDS, wasCanonicalInitially, rawAcl) )
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
				
			}

			/*
			* Method Name: TestConstructor
			*
			* Description:	check if the SystemAcl object's properties are as excepted, especially check the number of ACEs and order
			*			of ACEs
			*
			* Parameter:	systemAcl -- the SystemAcl object to be checked
			*			rawAcl -- this validation rawAcl the SystemAcl object to be compared with
			*			isContainer, isDS, revision, wasCanonicalIntially -- expected properties to be comparied with the SystemAcl object
			*
			* Return:		true if test pass, false otherwise
			*/

			private static bool TestConstructor(SystemAcl systemAcl, bool isContainer, bool isDS, bool wasCanonicalInitially, RawAcl rawAcl)
			{
				bool result = true;
				byte [] sAclBinaryForm = null;
				byte [] rAclBinaryForm = null;

				if(systemAcl.IsContainer == isContainer &&
					systemAcl.IsDS == isDS &&
					systemAcl.Revision == rawAcl.Revision &&
					systemAcl.Count == rawAcl.Count &&
					systemAcl.BinaryLength == rawAcl.BinaryLength &&
					systemAcl.IsCanonical == wasCanonicalInitially)					
				{
					sAclBinaryForm = new byte[systemAcl.BinaryLength];
					rAclBinaryForm = new byte[rawAcl.BinaryLength];
					systemAcl.GetBinaryForm(sAclBinaryForm, 0);
					rawAcl.GetBinaryForm(rAclBinaryForm, 0);

					if(!Utils.UtilIsBinaryFormEqual(sAclBinaryForm, rAclBinaryForm))
						result = false;	

					//redundant index check
					for (int i = 0; i < systemAcl.Count; i++)
					{
						if(!Utils.UtilIsAceEqual(systemAcl[i], rawAcl[i]))
						{
							result = false;
							break;
						}
					}
					
				}
				else
				{

					Console.WriteLine("systemAcl.IsContainter --" + systemAcl.IsContainer);
					Console.WriteLine("expected isContainer --" + isContainer);
					Console.WriteLine("systemAcl.IsDS --" + systemAcl.IsDS);
					Console.WriteLine("expected isDS --" + isDS);
					Console.WriteLine("systemAcl.Revision --" + systemAcl.Revision);
					Console.WriteLine("expected Revision --" + rawAcl.Revision);					
					Console.WriteLine("systemAcl.Count --" + systemAcl.Count);
					Console.WriteLine("expected Count --" + rawAcl.Count);
					Console.WriteLine("systemAcl.BinaryLength --" + systemAcl.BinaryLength);
					Console.WriteLine("expected BinaryLength --" + rawAcl.BinaryLength);					
					Console.WriteLine("systemAcl.WasCanonicalInitially --" + systemAcl.IsCanonical);
					Console.WriteLine("expected wasCanonicalInitially --" + wasCanonicalInitially);
					result = false;
				}

				return result;
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
				Console.WriteLine("Running AdditionalTestCases");
				


				bool isContainer = false;
				bool isDS = false;
				
				RawAcl rawAcl = null;
				SystemAcl sAcl = null;
				
				GenericAce gAce = null;				
				byte revision = 0;
				int capacity = 0;

				//CustomAce constructor parameters
				AceType aceType = AceType.AccessAllowed;
				AceFlags aceFlag = AceFlags.None;
				byte [] opaque = null;	

				//CompoundAce constructor additional parameters
				int accessMask = 0;
				CompoundAceType compoundAceType = CompoundAceType.Impersonation;
				string sid = "BA";		

				//CommonAce constructor additional parameters
				AceQualifier aceQualifier = 0;

				//ObjectAce constructor additional parameters
				ObjectAceFlags objectAceFlag = 0;
				Guid objectAceType ;
				Guid inheritedObjectAceType;				

				//case 1, an SystemAudit ACE with a zero access mask is meaningless, will be removed
				testCasesPerformed ++;

				
				try
				{
					revision = 0;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					gAce = new CommonAce(AceFlags.AuditFlags, 
											AceQualifier.SystemAudit, 
											0, 
											new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), 
											false,
											null);
					rawAcl.InsertAce(0, gAce);
					isContainer = false;
					isDS = false;

					sAcl = new SystemAcl(isContainer, isDS, rawAcl);
					//the only ACE is a meaningless ACE, will be removed
					//drop the ace from the rawAcl
					rawAcl.RemoveAce(0);

					if (TestConstructor(sAcl, isContainer, isDS, true, rawAcl))
					{
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


				
				//case 2, an inherit-only SystemAudit ACE on an object ACL is meaningless, will be removed
				testCasesPerformed ++;

								
				try
				{
					revision = 0;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					gAce = new CommonAce(AceFlags.InheritanceFlags | AceFlags.AuditFlags,
											AceQualifier.SystemAudit, 
											1, 
											new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)),
											false, null);
					rawAcl.InsertAce(0, gAce);
					isContainer = false;
					isDS = false;
					sAcl = new SystemAcl(isContainer, isDS, rawAcl);

					//the only ACE is a meaningless ACE, will be removed
					rawAcl.RemoveAce(0);
					
					if(TestConstructor(sAcl, isContainer, isDS, true, rawAcl))
					{
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



				//case 3, an inherit-only SystemAudit ACE without ContainerInherit or ObjectInherit flags on a container object is meaningless, will be removed
				testCasesPerformed ++;


				try
				{
					revision = 0;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					//200 has inheritOnly, SuccessfulAccess and FailedAccess
					gAce = new CommonAce((AceFlags)200, 
											AceQualifier.SystemAudit, 
											1, 
											new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), 
											false, 
											null);
					rawAcl.InsertAce(0, gAce);
					isContainer = true;
					isDS = false;
					sAcl = new SystemAcl(isContainer, isDS, rawAcl);					

					//the only ACE is a meaningless ACE, will be removed
					rawAcl.RemoveAce(0);
					if (TestConstructor(sAcl, isContainer, isDS, true, rawAcl))
					{
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



				//case 4, a SystemAudit ACE without Success or Failure Flags is meaningless, will be removed
				testCasesPerformed ++;

				
				try
				{
					revision = 255;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					gAce = new CommonAce(AceFlags.None, 
								AceQualifier.SystemAudit, 
								1, 
								new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), 
								false, 
								null);
					rawAcl.InsertAce(0, gAce);
					isContainer = true;
					isDS = false;
					sAcl = new SystemAcl(isContainer, isDS, rawAcl);					

					//audit ACE does not specify either Success or Failure Flags is removed
					rawAcl.RemoveAce(0);

					if (TestConstructor(sAcl, isContainer, isDS, true, rawAcl))
					{
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



				//case 5, a CustomAce
				testCasesPerformed ++;

				
				try
				{
					revision = 127;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					aceType = AceType.MaxDefinedAceType + 1;
					aceFlag = AceFlags.AuditFlags; 
					opaque = null;
					gAce = new CustomAce( aceType, aceFlag, opaque);
					rawAcl.InsertAce(0, gAce);
					isContainer = false;
					isDS = false;

					sAcl = new SystemAcl(isContainer, isDS, rawAcl);										

					//Mark changed design to make ACL with any CustomAce, CompoundAce uncanonical
					if (TestConstructor(sAcl, isContainer, isDS, false, rawAcl))
					{
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




				//case 6, a CompoundAce
				testCasesPerformed ++;

				
				try
				{
					revision = 127;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					aceFlag = AceFlags.AuditFlags;
					accessMask = 1;
					compoundAceType = CompoundAceType.Impersonation;
					gAce = new CompoundAce( aceFlag, 
						accessMask, 
						compoundAceType, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)));
					rawAcl.InsertAce(0, gAce);
					isContainer = true;
					isDS = false;

					sAcl = new SystemAcl(isContainer, isDS, rawAcl);					

					//Mark changed design to make ACL with any CustomAce, CompoundAce uncanonical
					if (TestConstructor(sAcl, isContainer, isDS, false, rawAcl))
					{
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




				//case 7, a ObjectAce
				testCasesPerformed ++;

				try
				{
					revision = 127;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					aceFlag = AceFlags.InheritanceFlags | AceFlags.AuditFlags; 
					aceQualifier = AceQualifier.SystemAudit;					
					accessMask = 1;
					objectAceFlag = ObjectAceFlags.ObjectAceTypePresent | ObjectAceFlags.InheritedObjectAceTypePresent;
					objectAceType = new Guid("11111111-1111-1111-1111-111111111111");
					inheritedObjectAceType = new Guid("22222222-2222-2222-2222-222222222222");
					gAce = new ObjectAce( aceFlag, 
						aceQualifier, 
						accessMask, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), 
						objectAceFlag, objectAceType,
						inheritedObjectAceType, 
						false,
						null);
      					rawAcl.InsertAce(0, gAce);
					isContainer = true;
					isDS = true;

					sAcl = new SystemAcl(isContainer, isDS, rawAcl);						

					if (TestConstructor(sAcl, isContainer, isDS, true, rawAcl))
					{
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



				//case 8, no Ace
				testCasesPerformed ++;

				
				try
				{
					revision = 127;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					isContainer = true;
					isDS = false;

					sAcl = new SystemAcl(isContainer, isDS, rawAcl);					

					if (TestConstructor(sAcl, isContainer, isDS, true, rawAcl))
					{
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



				//case 9, Aces from case 1 to 7 
				testCasesPerformed ++;

				
				try
				{
					revision = 127;
					capacity = 5;
					sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)).ToString();
					rawAcl = new RawAcl(revision, capacity);

					//an SystemAudit ACE with a zero access mask
					//is meaningless, will be removed					
					gAce = new CommonAce(AceFlags.AuditFlags, 
											AceQualifier.SystemAudit, 
											0, 
											new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid) + 1.ToString()), 
											false,
											null);
					rawAcl.InsertAce(rawAcl.Count, gAce);


					//an inherit-only SystemAudit ACE without ContainerInherit or ObjectInherit flags on a container object
					//is meaningless, will be removed
					//200 has inheritOnly, SuccessfulAccess and FailedAccess
					gAce = new CommonAce((AceFlags)200, 
											AceQualifier.SystemAudit, 
											1, 
											new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid) + 2.ToString()), 
											false, 
											null);
					rawAcl.InsertAce(rawAcl.Count, gAce);

					//a SystemAudit ACE without Success or Failure Flags
					//is meaningless, will be removed					
					gAce = new CommonAce(AceFlags.None, 
								AceQualifier.SystemAudit, 
								1, 
								new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid) + 3.ToString()), 
								false, 
								null);
					rawAcl.InsertAce(rawAcl.Count, gAce);	

					//a ObjectAce
					aceFlag = AceFlags.InheritanceFlags | AceFlags.AuditFlags; 
					aceQualifier = AceQualifier.SystemAudit;					
					accessMask = 1;
					objectAceFlag = ObjectAceFlags.ObjectAceTypePresent | ObjectAceFlags.InheritedObjectAceTypePresent;
					objectAceType = new Guid("11111111-1111-1111-1111-111111111111");
					inheritedObjectAceType = new Guid("22222222-2222-2222-2222-222222222222");
					gAce = new ObjectAce( aceFlag, 
						aceQualifier, 
						accessMask, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid) + 4.ToString()), 
						objectAceFlag, objectAceType,
						inheritedObjectAceType, 
						false,
						null);
      					rawAcl.InsertAce(rawAcl.Count, gAce);					

					// a CustomAce
					gAce = new CustomAce( AceType.MaxDefinedAceType + 1,
						AceFlags.AuditFlags, 
						null);
					rawAcl.InsertAce(rawAcl.Count, gAce);

					//a CompoundAce
					gAce = new CompoundAce( AceFlags.AuditFlags, 
						1, 
						CompoundAceType.Impersonation, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid) + 5.ToString()));
					rawAcl.InsertAce(rawAcl.Count, gAce);					
			
					isContainer = true;
					isDS = false;

					sAcl = new SystemAcl(isContainer, isDS, rawAcl);
					//the first 3 Aces will be removed by SystemAcl constructor
					rawAcl.RemoveAce(0);
					rawAcl.RemoveAce(0);
					rawAcl.RemoveAce(0);

					//Mark changed design to make ACL with any CustomAce, CompoundAce uncanonical

					if (TestConstructor(sAcl, isContainer, isDS, false, rawAcl))
					{
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
				SystemAcl systemAcl = null;
				bool isContainer  = false;
				bool isDS = false;
				RawAcl rawAcl = null;

				Console.WriteLine("Running Additional2TestCases");
				


				//case 1, rawAcl = null
				testCasesPerformed ++;

				
				try
				{				
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					Console.WriteLine("Should not allow creation of systemAcl from null RawAcl");
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
		}
			
		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test SystemAcl property inherited from abstract base class CommonAcl
		*		public sealed override int BinaryLength
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
			*			BinaryLength property
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/

			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running BinaryLengthTestCases");

				BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);

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

			public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{

				Console.WriteLine("Running BasicValidationTestCases");
				


				RawAcl rawAcl = null;
				SystemAcl systemAcl = null;
				GenericAce gAce = null;				
				byte revision = 0;
				int capacity = 0;
				string sid = "BG";
				int expectedLength = 0;

				//case 1, empty systemAcl, binarylength should be 8
				testCasesPerformed ++;

					
				try
				{
					capacity = 1;
					systemAcl = new SystemAcl(false, false, capacity);
					expectedLength = 8;
					if( expectedLength == systemAcl.BinaryLength)
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


				//case 2, SystemAcl with one Ace, binarylength should be 8 + the Ace's binarylength
				testCasesPerformed ++;

				try
				{
					expectedLength = 8;

					revision = 0;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);					
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
					expectedLength += gAce.BinaryLength;
					rawAcl.InsertAce(0, gAce);
					systemAcl = new SystemAcl(true, false, rawAcl);
					if( expectedLength == systemAcl.BinaryLength)
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


				//case 3, SystemAcl with two Aces
				testCasesPerformed ++;

				try
				{
					expectedLength = 8;

					revision = 0;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);					
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
					expectedLength += gAce.BinaryLength;
					rawAcl.InsertAce(0, gAce);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 2, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					expectedLength += gAce.BinaryLength;
					rawAcl.InsertAce(0, gAce);
					systemAcl = new SystemAcl(false, false, rawAcl);
					if( expectedLength == systemAcl.BinaryLength)
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
				RawAcl rawAcl = null;
				SystemAcl systemAcl = null;
				GenericAce gAce = null;				
				byte revision = 0;
				int capacity = 0;
				string sid = "BA";
				sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)).ToString();
				int expectedLength = 0;
						
				Console.WriteLine("Running AdditionalTestCases");
				

				
				//case 1, SystemAcl with huge number of Aces
				testCasesPerformed ++;

				try
				{
					revision = 0;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					expectedLength = 8;
					for(int i = 0; i < 1820; i ++)
					{
						gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, i + 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid + i.ToString())), false, null);
						rawAcl.InsertAce(0, gAce);
						expectedLength += gAce.BinaryLength;
					}
					systemAcl = new SystemAcl(false, false, rawAcl);
					if(  expectedLength == systemAcl.BinaryLength)
					{
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


			}
		}


		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test SystemAcl property inherited from abstract base class CommonAcl
		*		public sealed override int Count
		*
		*
		*/
		//----------------------------------------------------------------------------------------------

		private class AceCountTestCases
		{
			/*
			* Constructor
			*
			*/

			private AceCountTestCases(){}

			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			Count property
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/
			
			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running AceCountTestCases");

				BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);

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

			public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running BasicValidationTestCases");
				

				
				RawAcl rawAcl = null;
				GenericAce gAce = null;
				SystemAcl systemAcl = null;
				bool isContainer = false;
				bool isDS = false;				
				byte revision = 0;
				int capacity = 0;
				string sid = "BA";
			
				//case 1, empty SystemAcl
				testCasesPerformed ++;

									
				try
				{
					revision = 0;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					isContainer = false;
					isDS = false;
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					if( 0 == systemAcl.Count)
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



				//case 2, SystemAcl with one Ace
				testCasesPerformed ++;

				
				try
				{
					revision = 0;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);					
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
					rawAcl.InsertAce(0, gAce);
					isContainer = false;
					isDS = false;
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					if( 1 == systemAcl.Count)
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



				//case 3, SystemAcl with two Aces
				testCasesPerformed ++;

				
				try
				{
					revision = 0;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);					
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
					rawAcl.InsertAce(0, gAce);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 2, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rawAcl.InsertAce(0, gAce);
					isContainer = true;
					isDS = false;
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					if( 2 == systemAcl.Count)
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
				RawAcl rawAcl = null;
				GenericAce gAce = null;				
				byte revision = 0;
				int capacity = 0;
				string sid = "BA";
				SystemAcl systemAcl = null;
				bool isContainer = false;
				bool isDS = false;

				Console.WriteLine("Running AdditionalTestCases");
				

				
				//case 1, SysemAcl with huge number of Aces
				testCasesPerformed ++;

				
				try
				{
					revision = 0;
					capacity = 1;
					sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)).ToString();
					rawAcl = new RawAcl(revision, capacity);	
					for(int i = 0; i < 1820; i ++)
					{
						gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, i + 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid + i.ToString())), false, null);
						rawAcl.InsertAce(0, gAce);
					}
					isContainer = true;
					isDS = false;
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					if(  1820 == systemAcl.Count)
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
		*  Class to test SystemAcl index inherited from abstract base class CommonAcl
		*		public sealed override GenericAce this[int index]
		*
		*
		*/
		//----------------------------------------------------------------------------------------------


		private class IndexTestCases
		{
			/*
			* Constructor
			*
			*/

			private IndexTestCases(){}

			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			Index
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/
			
			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running IndexTestCases");

				BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
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
			
			public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running BasicValidationTestCases");
				


				RawAcl rawAcl = null;
				SystemAcl systemAcl = null;
				
				GenericAce gAce = null;
				GenericAce verifierGAce = null;
				string owner1 = "BO";	
				string owner2 = "BA";
				string owner3 = "BG";
				int index = 0;
		

				// case 1, only one ACE, get at index 0
				testCasesPerformed ++;

				
				try
				{			
					rawAcl = new RawAcl(1, 1);
					index = 0;
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);
					rawAcl.InsertAce(0, gAce);
					systemAcl = new SystemAcl(false, false, rawAcl);
					verifierGAce = systemAcl[index];

					if(TestIndex(gAce, verifierGAce))
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
				testCasesPerformed ++;

				
				try
				{				
					rawAcl = new RawAcl(1, 2);
					//208 has SuccessfulAccess, FailedAccess and Inherited
					gAce = new CommonAce((AceFlags)208, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);
					rawAcl.InsertAce(0, gAce);
					//gAce = new CommonAce(AceFlags.FailedAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(owner2), false, null);					
					gAce = new CommonAce(AceFlags.FailedAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);
					rawAcl.InsertAce(1, gAce);
					systemAcl = new SystemAcl(false, false, rawAcl);
					gAce = rawAcl[1];					
					index = systemAcl.Count - 1;
					
					verifierGAce = systemAcl[index];

					if(TestIndex(gAce, verifierGAce))
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
				testCasesPerformed ++;

				
				try
				{
					rawAcl = new RawAcl(1, 3);

					//215 has all AceFlags except InheritOnly					
					gAce = new CommonAce((AceFlags)(FlagsForAce.AuditFlags |FlagsForAce.OI |FlagsForAce.CI |FlagsForAce.NP |FlagsForAce.IH), AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner2)), false, null);
					rawAcl.InsertAce(0, gAce);

					//208 has SuccessfulAccess, FailedAccess and Inherited
					gAce = new CommonAce((AceFlags)(FlagsForAce.AuditFlags | FlagsForAce.IH), AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);
					rawAcl.InsertAce(1, gAce);
					
					gAce = new CommonAce(AceFlags.FailedAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner3)), false, null);
					rawAcl.InsertAce(2, gAce);
					systemAcl = new SystemAcl(false, false, rawAcl);
					gAce = rawAcl[1];
					index = systemAcl.Count/2;						
					verifierGAce = systemAcl[index];

					if(TestIndex(gAce, verifierGAce))
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


				//case 4, 3 ACEs, test merge, index at Count -1
				testCasesPerformed ++;

				
				try
				{				
					rawAcl = new RawAcl(1, 2);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);	
					rawAcl.InsertAce(0, gAce);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);	
					rawAcl.InsertAce(1, gAce);
					gAce = new CommonAce(AceFlags.FailedAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
					rawAcl.InsertAce(2, gAce);
					systemAcl = new SystemAcl(false, false, rawAcl);
					gAce = rawAcl[1];
					gAce.AceFlags = AceFlags.SuccessfulAccess | AceFlags.FailedAccess;
					index = systemAcl.Count - 1;
					
					verifierGAce = systemAcl[index];

					if(TestIndex(gAce, verifierGAce))
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
				RawAcl rawAcl = null;
				SystemAcl systemAcl = null;
				GenericAce gAce = null;
				GenericAce verifierGAce = null;
				string owner = null;
				int index = 0;

				Console.WriteLine("Running AdditionalTestCases");
				

				
				// case 1, no ACE, get index at -1
				testCasesPerformed ++;

				
				try
				{				
					rawAcl = new RawAcl(1, 1);
					index = -1;
					systemAcl = new SystemAcl(false, false, rawAcl);
					verifierGAce = systemAcl[index];
					Console.WriteLine("Should not allow get index -1");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
				}
				catch(ArgumentOutOfRangeException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}

				
				//case 2, no ACE, get index at Count
				testCasesPerformed ++;

				
				try
				{				
					rawAcl = new RawAcl(1, 1);
					systemAcl = new SystemAcl(false, false, rawAcl);
					index = systemAcl.Count;					
					verifierGAce = systemAcl[index];
					Console.WriteLine("Should not allow get index Count");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
				}
				catch(ArgumentOutOfRangeException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}

				
				//case 3, no ACE, set index at -1
				testCasesPerformed ++;

				
				try
				{
					rawAcl = new RawAcl(1, 1);
					systemAcl = new SystemAcl(false, false, rawAcl);
					index = -1;					
					owner = "BA";				
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);					
					systemAcl[index] = gAce;
					Console.WriteLine("Should not allow set index as -1");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
				}
				catch(NotSupportedException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}

				
				//case 4, no ACE, set index at Count
				testCasesPerformed ++;

				
				try
				{
					rawAcl = new RawAcl(1, 1);
					systemAcl = new SystemAcl(true, false, rawAcl);
					index = systemAcl.Count;					
					owner = "BA";				
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);					
					systemAcl[index] = gAce;
					Console.WriteLine("Should not allow set index as Count");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
				}
				catch(NotSupportedException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				//case 5, set null Ace
				testCasesPerformed ++;

				try
				{
					rawAcl = new RawAcl(1, 1);
					gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
					rawAcl.InsertAce(0, gAce);
					systemAcl = new SystemAcl(false, false, rawAcl);
					index = 0;					
					gAce = null;
					systemAcl[index] = gAce;
					Console.WriteLine("Should not allow set null ACE");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
				}
				catch(NotSupportedException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				//case 6, set index at 0
				testCasesPerformed ++;

				
				try
				{
					rawAcl = new RawAcl(1, 1);
					owner = "BA";						
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
					rawAcl.InsertAce(0, gAce);
					
					systemAcl = new SystemAcl(false, false, rawAcl);
					index = 0;					
					owner = "BA";				
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);					
					systemAcl[index] = gAce;
					Console.WriteLine("Should throw not supported exception");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
				}
				catch(NotSupportedException)
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
				if( Utils.UtilIsAceEqual(gAce, verifierGAce))
				{//as operator == and != are overridden to by value, can not use != to test these two are not same object any more
					gAce.AceFlags =  AceFlags.InheritanceFlags | AceFlags.Inherited | AceFlags.AuditFlags;
					if(gAce != verifierGAce)
						return true;
					else
						return false;
				}
				else
				{					
					return false;	
				}
			}
		}

		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test SystemAcl method
		*		 public void AddAudit( AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags )
		*
		*
		*/
		//----------------------------------------------------------------------------------------------

		private class AddAuditTestCases
		{
			/*
			* Constructor
			*
			*/

			private AddAuditTestCases(){}

			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			AddAudit
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/
			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running AddAuditTestCases");

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

				RawAcl rawAcl = null;
				SystemAcl systemAcl = null;
				bool isContainer = false;
				bool isDS = false;
				int auditFlags = 0;
				string sid = null;
				int accessMask = 1;
				int inheritanceFlags = 0;
				int propagationFlags = 0;
				string initialRawAclStr = null;
				string verifierRawAclStr = null;				

				string[] testCase = null;
				ITestCaseReader reader = new InfTestCaseStore();
				reader.OpenTestCaseStore(AddAuditTestCaseStore); 

				while (null != (testCase = reader.ReadNextTestCase()))
				{
					// read isContainer
					if (1 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isContainer = bool.Parse(testCase[0]);

					// read isDS
					if (2 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isDS = bool.Parse(testCase[1]);

					// read auditFlags
					if (3 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						auditFlags = int.Parse(testCase[2]);

					// read securityIdentifier
					if (4 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						sid = testCase[3];

					// read accessMask
					if (5 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						accessMask = int.Parse(testCase[4]);

					// read inheritanceFlags
					if (6 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						inheritanceFlags = int.Parse(testCase[5]);

					// read propagationFlags
					if (7 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						propagationFlags = int.Parse(testCase[6]);

					//read initialRawAclStr
					if (8 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						initialRawAclStr = testCase[7];

					//read verifierRawAclStr
					if (9 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						verifierRawAclStr = testCase[8];					
					
					//create a systemAcl
					rawAcl = Utils.UtilCreateRawAclFromString(initialRawAclStr);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					rawAcl = Utils.UtilCreateRawAclFromString(verifierRawAclStr);
										
					testCasesPerformed ++;

					try
					{
						if (TestAddAudit(systemAcl, rawAcl, (AuditFlags)auditFlags, 
							new SecurityIdentifier (Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags) inheritanceFlags, (PropagationFlags)propagationFlags)) 
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
			}

			/*
			* Method Name: TestAddAudit
			*
			* Description:	check if after add the audit ace the systemAcl has same content, number of ACEs, content of 
			*			each ACE and the order of the ACEs, as the rawAcl.
			*
			* Parameter:	systemAcl -- the SystemAcl to add audit to
			*			rawAcl -- the validation RawAcl to compare with
			*			auditFlag, sid,  accessMask, inheritanceFlags, propagationFlags -- the paramether to pass to AddAudit
			*
			* Return:		true if test pass, false otherwise
			*/

			private static bool TestAddAudit(SystemAcl systemAcl, RawAcl rawAcl, AuditFlags auditFlag, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
			{
				bool result = true;

				byte [] sAclBinaryForm = null;
				byte [] rAclBinaryForm = null;

				systemAcl.AddAudit(auditFlag, sid, accessMask, inheritanceFlags, propagationFlags);
				if(systemAcl.Count == rawAcl.Count &&
					systemAcl.BinaryLength == rawAcl.BinaryLength)
				{

					sAclBinaryForm = new byte[systemAcl.BinaryLength];
					rAclBinaryForm = new byte[rawAcl.BinaryLength];
					systemAcl.GetBinaryForm(sAclBinaryForm, 0);
					rawAcl.GetBinaryForm(rAclBinaryForm, 0);					
					if(!Utils.UtilIsBinaryFormEqual(sAclBinaryForm, rAclBinaryForm))
						result = false;	

					//redundant index check
					for (int i = 0; i < systemAcl.Count; i++)
					{
						if(!Utils.UtilIsAceEqual(systemAcl[i], rawAcl[i]))
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
				RawAcl rawAcl = null;
				SystemAcl systemAcl = null;
				bool isContainer = false;
				bool isDS = false;
				
				int auditFlags = 0;
				string sid = null;
				int accessMask = 1;
				int inheritanceFlags = 0;
				int propagationFlags = 0;
				GenericAce gAce = null;
				byte [] opaque = null;

				Console.WriteLine("Running AdditionalTestCases");
				


				//Case 1, null sid
				testCasesPerformed ++;

				
				try
				{				
					isContainer = false;
					isDS = false;
					rawAcl = new RawAcl(0, 1);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);

					systemAcl.AddAudit(AuditFlags.Success, null, 1, InheritanceFlags.None, PropagationFlags.None);
					Console.WriteLine("Should not allow add Ace with null Sid");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					
				}
				catch(ArgumentNullException)
				{//this is expected
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				//Case 2, SystemAudit Ace but non AuditFlags
				testCasesPerformed ++;

				
				try
				{				
					isContainer = false;
					isDS = false;
					rawAcl = new RawAcl(0, 1);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);

					systemAcl.AddAudit(AuditFlags.None, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), 1, InheritanceFlags.None, PropagationFlags.None);
					Console.WriteLine("Should not allow add AuditAce with AuditFlags.None");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					
				}
				catch(ArgumentException)
				{//this is expected
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				//Case 3, 0 accessMask
				testCasesPerformed ++;

				
				try
				{				
					isContainer = false;
					isDS = false;
					rawAcl = new RawAcl(0, 1);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);

					systemAcl.AddAudit(AuditFlags.Success, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), 0, InheritanceFlags.None, PropagationFlags.None);
					Console.WriteLine("Should not allow add Ace with 0 access Mask");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					
				}
				catch(ArgumentException)
				{//this is expected
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}

								
							
				//Case 4, non-Container, but InheritanceFlags is not None
				testCasesPerformed ++;

				
				try
				{					
					isContainer = false;
					isDS = false;
					rawAcl = new RawAcl(0, 1);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);

					systemAcl.AddAudit(AuditFlags.Success, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), 1, InheritanceFlags.ContainerInherit, PropagationFlags.None);
					Console.WriteLine("Should not allow add audit with InheritanceFlags not None to non-Container systemAcl ");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					
				}
				catch(ArgumentException)
				{//this is expected
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}

				

				//Case 5, non-Container, but PropagationFlags is not None
				testCasesPerformed ++;

				
				try
				{				
					isContainer = false;
					isDS = false;
					rawAcl = new RawAcl(0, 1);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);

					systemAcl.AddAudit(AuditFlags.Success, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), 1, InheritanceFlags.None, PropagationFlags.InheritOnly);
					Console.WriteLine("Should not allow add audit with PropagationFlags not None to non-Container systemAcl ");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					
				}
				catch(ArgumentException)
				{//this is expected
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}



				//Case 6, Container, but InheritanceFlags is None, but PropagationFlags is InheritOnly
				testCasesPerformed ++;

				
				try
				{				
					isContainer = true;
					isDS = false;
					rawAcl = new RawAcl(0, 1);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);

					systemAcl.AddAudit(AuditFlags.Success, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), 1, InheritanceFlags.None, PropagationFlags.InheritOnly);
					Console.WriteLine("Should not allow add audit with InheritanceFlags None, PropagationFlags InheritOnly to Container systemAcl ");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					
				}
				catch(ArgumentException)
				{//this is expected
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				//Case 7, Container, but InheritanceFlags is None, but PropagationFlags is NoPropagateInherit
				testCasesPerformed ++;

				
				try
				{				
					isContainer = true;
					isDS = false;
					rawAcl = new RawAcl(0, 1);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);

					systemAcl.AddAudit(AuditFlags.Success, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), 1, InheritanceFlags.None, PropagationFlags.NoPropagateInherit);
					Console.WriteLine("Should not allow add audit with InheritanceFlags None, PropagationFlags NoPropagateInherit to Container systemAcl ");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					
				}
				catch(ArgumentException)
				{//this is expected
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}



				//Case 8, Container, but InheritanceFlags is None, but PropagationFlags is NoPropagateInherit | InheritOnly
				testCasesPerformed ++;

				
				try
				{				
					isContainer = true;
					isDS = false;
					rawAcl = new RawAcl(0, 1);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);

					systemAcl.AddAudit(AuditFlags.Success, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), 1, InheritanceFlags.None, PropagationFlags.NoPropagateInherit |PropagationFlags.InheritOnly);
					Console.WriteLine("Should not allow add audit with InheritanceFlags None, PropagationFlags NoPropagateInherit | InheritOnly to Container systemAcl ");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					
				}
				catch(ArgumentException)
				{//this is expected
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}

				
				//Case 9, add one audit ACE to the SystemAcl has no ACE
				testCasesPerformed ++;


				try
				{			
					isContainer = true;
					isDS = false;
					auditFlags = 1;
					sid = "BA";
					accessMask = 1;
					inheritanceFlags = 3;
					propagationFlags = 3;
					rawAcl = new RawAcl(0, 1);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					//79 = AceFlags.SuccessfulAccess | AceFlags.ObjectInherit |AceFlags.ContainerInherit | AceFlags.NoPropagateInherit | AceFlags.InheritOnly
					gAce = new CommonAce((AceFlags)79,  AceQualifier.SystemAudit, accessMask, 
					new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);					
					rawAcl.InsertAce(rawAcl.Count, gAce);
					if(TestAddAudit(systemAcl, rawAcl, (AuditFlags)auditFlags, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags) inheritanceFlags, (PropagationFlags) propagationFlags))
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


				//Case 10, all the ACEs in the Sacl are non-qualified ACE, no merge

				testCasesPerformed ++;


				try
				{
					isContainer = true;
					isDS = false;

					inheritanceFlags = 1;//InheritanceFlags.ContainerInherit
					propagationFlags = 2; //PropagationFlags.InheritOnly
					
					auditFlags = 3;
					sid = "BA";
					accessMask = 1;

					rawAcl = new RawAcl(0, 1);
					opaque = new byte[4];
					gAce = new CustomAce( AceType.MaxDefinedAceType + 1, AceFlags.InheritanceFlags | AceFlags.AuditFlags, opaque);;
					rawAcl.InsertAce(0, gAce);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);

					gAce = new CommonAce(AceFlags.ContainerInherit | AceFlags.InheritOnly | AceFlags.AuditFlags,
											AceQualifier.SystemAudit,
											accessMask,
											new SecurityIdentifier (Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)),
											false,
											null);
					rawAcl.InsertAce(0, gAce);

					//After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
					//forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
					try
					{

						TestAddAudit(systemAcl, rawAcl, (AuditFlags) auditFlags, 
							new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags) inheritanceFlags, (PropagationFlags) propagationFlags);

						Console.WriteLine("Should not allow Add Ace to ACL with customAce/CompoundAce");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	

					}
					catch(InvalidOperationException)
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;						
					}
					
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				//Case 11, add Ace to exceed binary length boundary, throw exception
				testCasesPerformed ++;


				try
				{
					isContainer = true;
					isDS = false;

					inheritanceFlags = 1;//InheritanceFlags.ContainerInherit
					propagationFlags = 2; //PropagationFlags.InheritOnly
					
					auditFlags = 3;
					sid = "BA";
					accessMask = 1;

					rawAcl = new RawAcl(0, 1);
					opaque = new byte[GenericAcl.MaxBinaryLength + 1 - 8 - 4 - 16];
					gAce = new CustomAce( AceType.MaxDefinedAceType + 1, 
						AceFlags.InheritanceFlags | AceFlags.AuditFlags, opaque);;
					rawAcl.InsertAce(0, gAce);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);


					//After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
					//forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
					try
					{

						systemAcl.AddAudit((AuditFlags)auditFlags, 
							new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags) inheritanceFlags, (PropagationFlags) propagationFlags);

						Console.WriteLine("Should not allow Add Ace to ACL with customAce/CompoundAce");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	

					}
					catch(InvalidOperationException)
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;						
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}

/*
				//Case 9, add one audit ACE with inheritanceFlags = -1, propagationFlags = -1, to the SystemAcl has no ACE
				//should fail, but currently, success
				testCasesPerformed ++;


				try
				{			
					isContainer = true;
					isDS = false;
					auditFlags = 1;
					sid = "BA";
					accessMask = 1;
					inheritanceFlags = -1;
					propagationFlags = -1;
					rawAcl = new RawAcl(0, 1);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					//79 = AceFlags.SuccessfulAccess | AceFlags.ObjectInherit |AceFlags.ContainerInherit | AceFlags.NoPropagateInherit | AceFlags.InheritOnly
					gAce = new CommonAce((AceFlags)79,  AceQualifier.SystemAudit, accessMask, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);					
					rawAcl.InsertAce(rawAcl.Count, gAce);
					if(TestAddAudit(systemAcl, rawAcl, (AuditFlags)auditFlags, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags) inheritanceFlags, (PropagationFlags) propagationFlags))
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

*/
			}
		}

		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test SystemAcl method
		*		 public void SetAudit( AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags )
		*
		*
		*/
		//----------------------------------------------------------------------------------------------


		private class SetAuditTestCases
		{
			//No creating objects

			private SetAuditTestCases(){}

			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			SetAudit
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/
			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running SetAuditTestCases");

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

				RawAcl rawAcl = null;
				SystemAcl systemAcl = null;
				bool isContainer = false;
				bool isDS = false;
				int auditFlags = 0;
				string sid = null;
				int accessMask = 1;
				int inheritanceFlags = 0;
				int propagationFlags = 0;
				string initialRawAclStr = null;
				string verifierRawAclStr = null;				


				string[] testCase = null;
				ITestCaseReader reader = new InfTestCaseStore();
				reader.OpenTestCaseStore(SetAuditTestCaseStore); 

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

					// read auditFlags
					if (3 > testCase.Length)
						auditFlags = 0;
					else
						auditFlags = int.Parse(testCase[2]);

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
					
					//create a systemAcl

					rawAcl = Utils.UtilCreateRawAclFromString(initialRawAclStr);

					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					rawAcl = Utils.UtilCreateRawAclFromString(verifierRawAclStr);

					testCasesPerformed ++;

					try
					{
						if (TestSetAudit(systemAcl, rawAcl, (AuditFlags)auditFlags, 
							new SecurityIdentifier (Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags) inheritanceFlags, (PropagationFlags)propagationFlags)) 
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
			}

			/*
			* Method Name: TestSetAudit
			*
			* Description:	check if after set the audit ace the systemAcl has same content, number of ACEs, content of 
			*			each ACE and the order of the ACEs, as the rawAcl.
			*
			* Parameter:	systemAcl -- the SystemAcl to set audit to
			*			rawAcl -- the validation RawAcl to compare with
			*			auditFlag, sid,  accessMask, inheritanceFlags, propagationFlags -- the paramether to pass to SetAudit
			*
			* Return:		true if test pass, false otherwise
			*/

			private static bool TestSetAudit(SystemAcl systemAcl, RawAcl rawAcl, AuditFlags auditFlag, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
			{
				bool result = true;

				byte [] sAclBinaryForm = null;
				byte [] rAclBinaryForm = null;				

				systemAcl.SetAudit(auditFlag, sid, accessMask, inheritanceFlags, propagationFlags);
				if(systemAcl.Count == rawAcl.Count &&
					systemAcl.BinaryLength == rawAcl.BinaryLength)
				{

					sAclBinaryForm = new byte[systemAcl.BinaryLength];
					rAclBinaryForm = new byte[rawAcl.BinaryLength];
					systemAcl.GetBinaryForm(sAclBinaryForm, 0);
					rawAcl.GetBinaryForm(rAclBinaryForm, 0);
					
					if(!Utils.UtilIsBinaryFormEqual(sAclBinaryForm, rAclBinaryForm))
						result = false;	

					//redundant index check
					for (int i = 0; i < systemAcl.Count; i++)
					{
						if(!Utils.UtilIsAceEqual(systemAcl[i], rawAcl[i]))
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

				RawAcl rawAcl = null;
				SystemAcl systemAcl = null;
				bool isContainer = false;
				bool isDS = false;
				
				int auditFlags = 0;
				string sid = null;
				int accessMask = 1;
				int inheritanceFlags = 0;
				int propagationFlags = 0;
				GenericAce gAce = null;
				byte[] opaque = null;

				Console.WriteLine("Running AdditionalTestCases");
				


				//Case 1, null sid
				testCasesPerformed ++;

				
				try
				{				
					isContainer = false;
					isDS = false;
					rawAcl = new RawAcl(0, 1);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);

					systemAcl.SetAudit(AuditFlags.Success, null, 1, InheritanceFlags.None, PropagationFlags.None);
					Console.WriteLine("Should not allow set Ace with null Sid");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					
				}
				catch(ArgumentNullException)
				{//this is expected
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				//Case 2, SystemAudit Ace but non AuditFlags
				testCasesPerformed ++;

				
				try
				{				
					isContainer = false;
					isDS = false;
					rawAcl = new RawAcl(0, 1);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);

					systemAcl.SetAudit(AuditFlags.None, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), 1, InheritanceFlags.None, PropagationFlags.None);
					Console.WriteLine("Should not allow set Ace with AuditFlags.None");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					
				}
				catch(ArgumentException)
				{//this is expected
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				//Case 3, 0 accessMask
				testCasesPerformed ++;

				
				try
				{				
					isContainer = false;
					isDS = false;
					rawAcl = new RawAcl(0, 1);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);

					systemAcl.SetAudit(AuditFlags.Success, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), 0, InheritanceFlags.None, PropagationFlags.None);
					Console.WriteLine("Should not allow set Ace with 0 access Mask");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					
				}
				catch(ArgumentException)
				{//this is expected
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}

								

				//Case 4, non-Container, but InheritanceFlags is not None
				testCasesPerformed ++;

				
				try
				{				
					isContainer = false;
					isDS = false;
					rawAcl = new RawAcl(0, 1);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);

					systemAcl.SetAudit(AuditFlags.Success, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), 1, InheritanceFlags.ContainerInherit, PropagationFlags.None);
					Console.WriteLine("Should not allow set audit with InheritanceFlags not None to non-Container systemAcl ");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					
				}
				catch(ArgumentException)
				{//this is expected
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}

					
				//Case 5, non-Container, but PropagationFlags is not None
				testCasesPerformed ++;

				try
				{
					isContainer = false;
					isDS = false;
					rawAcl = new RawAcl(0, 1);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);

					systemAcl.SetAudit(AuditFlags.Success, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), 1, InheritanceFlags.None, PropagationFlags.InheritOnly);
					Console.WriteLine("Should not allow set audit with PropagationFlags not None to non-Container systemAcl ");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					
				}
				catch(ArgumentException)
				{//this is expected
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				//Case 6, set one audit ACE to the SystemAcl with no ACE
				testCasesPerformed ++;

				try
				{
					isContainer = true;
					isDS = false;
					auditFlags = 1;
					sid = "BA";
					accessMask = 1;
					inheritanceFlags = 3;
					propagationFlags = 3;
					rawAcl = new RawAcl(0, 1);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					//79 = AceFlags.SuccessfulAccess | AceFlags.ObjectInherit |AceFlags.ContainerInherit | AceFlags.NoPropagateInherit | AceFlags.InheritOnly
					gAce = new CommonAce((AceFlags)79,  AceQualifier.SystemAudit, accessMask, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);					
					rawAcl.InsertAce(rawAcl.Count, gAce);
					if(TestSetAudit(systemAcl, rawAcl, (AuditFlags)auditFlags, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags) inheritanceFlags, (PropagationFlags) propagationFlags))
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


				//Case 7, all the ACEs in the Sacl are non-qualified ACE, no merge

				testCasesPerformed ++;


				try
				{
					isContainer = true;
					isDS = false;

					inheritanceFlags = 1;//InheritanceFlags.ContainerInherit
					propagationFlags = 2; //PropagationFlags.InheritOnly
					
					auditFlags = 3;
					sid = "BA";
					accessMask = 1;

					rawAcl = new RawAcl(0, 1);
					opaque = new byte[4];
					gAce = new CustomAce( AceType.MaxDefinedAceType + 1, AceFlags.InheritanceFlags | AceFlags.AuditFlags, opaque);;
					rawAcl.InsertAce(0, gAce);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);

					gAce = new CommonAce(AceFlags.ContainerInherit | AceFlags.InheritOnly | AceFlags.AuditFlags,
											AceQualifier.SystemAudit,
											accessMask,
											new SecurityIdentifier (Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)),
											false,
											null);
					rawAcl.InsertAce(0, gAce);

					//After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
					//forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
					try
					{

						TestSetAudit(systemAcl, rawAcl, (AuditFlags) auditFlags, 
							new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags) inheritanceFlags, (PropagationFlags) propagationFlags);

						Console.WriteLine("Should not allow Set Ace to ACL with customAce/CompoundAce");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	

					}
					catch(InvalidOperationException)
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;						
					}		
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				//Case 8, Set Ace to exceed binary length boundary, throw exception
				testCasesPerformed ++;


				try
				{
					isContainer = true;
					isDS = false;

					inheritanceFlags = 1;//InheritanceFlags.ContainerInherit
					propagationFlags = 2; //PropagationFlags.InheritOnly
					
					auditFlags = 3;
					sid = "BA";
					accessMask = 1;

					rawAcl = new RawAcl(0, 1);
					opaque = new byte[GenericAcl.MaxBinaryLength + 1 - 8 - 4 - 16];
					gAce = new CustomAce( AceType.MaxDefinedAceType + 1, 
						AceFlags.InheritanceFlags | AceFlags.AuditFlags, opaque);;
					rawAcl.InsertAce(0, gAce);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);

					//After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
					//forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
					try
					{

						systemAcl.SetAudit((AuditFlags)auditFlags, 
							new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags) inheritanceFlags, (PropagationFlags) propagationFlags);

						Console.WriteLine("Should not allow Set Ace to ACL with customAce/CompoundAce");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	

					}
					catch(InvalidOperationException)
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
		*  Class to test SystemAcl method
		*		 public bool RemoveAudit( AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags )
		*
		*
		*/
		//----------------------------------------------------------------------------------------------


		private class RemoveAuditTestCases
		{
			//No creating objects

			private RemoveAuditTestCases(){}

			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			RemoveAudit
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/

			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running RemoveAuditTestCases");

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
				
				RawAcl rawAcl = null;
				SystemAcl systemAcl = null;
				bool isContainer = false;
				bool isDS = false;
				int auditFlags = 0;
				string sid = null;
				int accessMask = 1;
				int inheritanceFlags = 0;
				int propagationFlags = 0;
				string initialRawAclStr = null;
				string verifierRawAclStr = null;
				bool removePossible = false;

				string[] testCase = null;
				ITestCaseReader reader = new InfTestCaseStore();
				reader.OpenTestCaseStore(RemoveAuditTestCaseStore); 

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

					// read auditFlags
					if (3 > testCase.Length)
						auditFlags = 0;
					else
						auditFlags = int.Parse(testCase[2]);

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
					
					//create a systemAcl

					rawAcl = Utils.UtilCreateRawAclFromString(initialRawAclStr);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					rawAcl = Utils.UtilCreateRawAclFromString(verifierRawAclStr);

					testCasesPerformed ++;

					try
					{									
						if (TestRemoveAudit(systemAcl, rawAcl, (AuditFlags)auditFlags, 
							new SecurityIdentifier (Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags) inheritanceFlags, (PropagationFlags)propagationFlags, removePossible)) 
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
			}

			/*
			* Method Name: TestRemoveAudit
			*
			* Description:	check if after remove the audit ace the systemAcl has same content, number of ACEs, content of 
			*			each ACE and the order of the ACEs, as the rawAcl.
			*
			* Parameter:	systemAcl -- the SystemAcl to remove audit from
			*			rawAcl -- the validation RawAcl to compare with
			*			auditFlag, sid,  accessMask, inheritanceFlags, propagationFlags -- the paramether to pass to SetAudit
			*			removePossible -- the expected return value from RemoveAudit
			* Return:		true if test pass, false otherwise
			*/


			private static bool TestRemoveAudit(SystemAcl systemAcl, RawAcl rawAcl, AuditFlags auditFlag, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, bool removePossible)
			{
				bool result = true;
				bool isRemoved = false;
				byte [] sAclBinaryForm = null;
				byte [] rAclBinaryForm = null;					

				isRemoved = systemAcl.RemoveAudit(auditFlag, sid, accessMask, inheritanceFlags, propagationFlags);
				if((isRemoved == removePossible) &&
					(systemAcl.Count == rawAcl.Count)&&
					(systemAcl.BinaryLength == rawAcl.BinaryLength))
				{

					sAclBinaryForm = new byte[systemAcl.BinaryLength];
					rAclBinaryForm = new byte[rawAcl.BinaryLength];
					systemAcl.GetBinaryForm(sAclBinaryForm, 0);
					rawAcl.GetBinaryForm(rAclBinaryForm, 0);
					
					if(!Utils.UtilIsBinaryFormEqual(sAclBinaryForm, rAclBinaryForm))
						result = false;		

					//redundant index check
					for (int i = 0; i < systemAcl.Count; i++)
					{
						if(!Utils.UtilIsAceEqual(systemAcl[i], rawAcl[i]))
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

				RawAcl rawAcl = null;
				SystemAcl systemAcl = null;
				bool isContainer = false;
				bool isDS = false;
				
				int auditFlags = 0;
				string sid = null;
				int accessMask = 1;
				int inheritanceFlags = 0;
				int propagationFlags = 0;
				GenericAce gAce = null;
				bool removePossible = false;
				byte [] opaque = null;

				Console.WriteLine("Running AdditionalTestCases");
				


				//Case 1, null sid
				testCasesPerformed ++;

				
				try
				{				
					isContainer = false;
					isDS = false;
					rawAcl = new RawAcl(0, 1);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);

					systemAcl.RemoveAudit(AuditFlags.Success, null, 1, InheritanceFlags.None, PropagationFlags.None);
					Console.WriteLine("Should not allow remove Ace with null Sid");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					
				}
				catch(ArgumentNullException)
				{//this is expected
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				//Case 2, SystemAudit Ace but non AuditFlags
				testCasesPerformed ++;

				
				try
				{				
					isContainer = false;
					isDS = false;
					rawAcl = new RawAcl(0, 1);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);

					systemAcl.RemoveAudit(AuditFlags.None, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), 1, InheritanceFlags.None, PropagationFlags.None);
					Console.WriteLine("Should not allow remove Ace with non AuditFlags");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					
				}
				catch(ArgumentException)
				{//this is expected
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				//Case 3, 0 accessMask
				testCasesPerformed ++;

				
				try
				{				
					isContainer = false;
					isDS = false;
					rawAcl = new RawAcl(0, 1);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);

					systemAcl.RemoveAudit(AuditFlags.Success, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), 0, InheritanceFlags.None, PropagationFlags.None);
					Console.WriteLine("Should not allow remove Ace with 0 accessMask");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					
				}
				catch(ArgumentException)
				{//this is expected
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}

								
				
				//Case 4, remove one audit ACE from the SystemAcl with no ACE
				testCasesPerformed ++;


				try
				{					
					isContainer = true;
					isDS = false;
					auditFlags = 1;
					sid = "BA";
					accessMask = 1;
					inheritanceFlags = 3;
					propagationFlags = 3;
					removePossible = true;
					rawAcl = new RawAcl(0, 1);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					if(TestRemoveAudit(systemAcl, rawAcl, (AuditFlags)auditFlags, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags) inheritanceFlags, (PropagationFlags) propagationFlags, removePossible))
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


				//Case 5, remove the last one ACE from the SystemAcl
				testCasesPerformed ++;


				try
				{
					isContainer = true;
					isDS = false;
					auditFlags = 1;
					sid = "BA";
					accessMask = 1;
					inheritanceFlags = 3;
					propagationFlags = 3;
					removePossible = true;
					rawAcl = new RawAcl(0, 1);
					//79 = AceFlags.SuccessfulAccess | AceFlags.ObjectInherit |AceFlags.ContainerInherit | AceFlags.NoPropagateInherit | AceFlags.InheritOnly
					gAce = new CommonAce((AceFlags)79,  AceQualifier.SystemAudit, accessMask, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);					
					rawAcl.InsertAce(rawAcl.Count, gAce);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					//remove the ace to create the validation rawAcl
					rawAcl.RemoveAce(rawAcl.Count - 1);
					if(TestRemoveAudit(systemAcl, rawAcl, (AuditFlags)auditFlags, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags) inheritanceFlags, (PropagationFlags) propagationFlags, removePossible))
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


				//Case 6, all the ACEs in the Sacl are non-qualified ACE, no remove

				testCasesPerformed ++;


				try
				{
					isContainer = true;
					isDS = false;

					inheritanceFlags = 1;//InheritanceFlags.ContainerInherit
					propagationFlags = 2; //PropagationFlags.InheritOnly
					
					auditFlags = 3;
					sid = "BA";
					accessMask = 1;

					rawAcl = new RawAcl(0, 1);
					opaque = new byte[4];
					gAce = new CustomAce( AceType.MaxDefinedAceType + 1, AceFlags.InheritanceFlags | AceFlags.AuditFlags, opaque);;
					rawAcl.InsertAce(0, gAce);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);

					//After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
					//forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
					try
					{

						TestRemoveAudit(systemAcl, rawAcl, (AuditFlags) auditFlags, 
							new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags) inheritanceFlags, (PropagationFlags) propagationFlags, true);

						Console.WriteLine("Should not allow RemoveAudit from ACL with customAce/CompoundAce");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	

					}
					catch(InvalidOperationException)
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;						
					}					
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}



				//Case 7, remove split cause overflow
                // Test case no longer relevant in CoreCLR
                // Non-canonical ACLs cannot be modified
                /*
				{

				testCasesPerformed ++;


				try
				{
					isContainer = true;
					isDS = false;

					inheritanceFlags = 2;//InheritanceFlags.ObjectInherit
					propagationFlags = 3; //PropagationFlags.NoPropagateInherit |  PropagationFlags.InheritOnly
					auditFlags = 1;
					accessMask = 1;
					sid = "BG";
					opaque = new byte[GenericAcl.MaxBinaryLength + 1 - 8 - 4 - 72];

					
					rawAcl = new RawAcl(0, 1);					
					gAce = new CommonAce ( AceFlags.ObjectInherit | AceFlags.ContainerInherit | AceFlags.NoPropagateInherit | AceFlags.AuditFlags,
											AceQualifier.SystemAudit,
											3,
											new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)),
											false,
											null );
					rawAcl.InsertAce(0, gAce);
					gAce = new CustomAce( AceType.MaxDefinedAceType + 1, AceFlags.AuditFlags, opaque);
					rawAcl.InsertAce(0, gAce);

					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					try
					{
						systemAcl.RemoveAudit((AuditFlags)auditFlags, 
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

			}
		}

		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test SystemAcl method
		*		 public void RemoveAuditSpecific( AuditFlags auditFlags, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags )
		*
		*
		*/
		//----------------------------------------------------------------------------------------------


		private class RemoveAuditSpecificTestCases
		{
			//No creating objects

			private RemoveAuditSpecificTestCases(){}

			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			RemoveAuditSpecific
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/

			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running RemoveAuditSpecificTestCases");

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
				
				RawAcl rawAcl = null;
				SystemAcl systemAcl = null;
				bool isContainer = false;
				bool isDS = false;
				int auditFlags = 0;
				string sid = null;
				int accessMask = 1;
				int inheritanceFlags = 0;
				int propagationFlags = 0;
				string initialRawAclStr = null;
				string verifierRawAclStr = null;				


				string[] testCase = null;
				ITestCaseReader reader = new InfTestCaseStore();
				reader.OpenTestCaseStore(RemoveAuditSpecificTestCaseStore); 

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

					// read auditFlags
					if (3 > testCase.Length)
						auditFlags = 0;
					else
						auditFlags = int.Parse(testCase[2]);

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
					
					//create a systemAcl

					rawAcl = Utils.UtilCreateRawAclFromString(initialRawAclStr);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					rawAcl = Utils.UtilCreateRawAclFromString(verifierRawAclStr);
										
					testCasesPerformed ++;

					try
					{	
						if (TestRemoveAuditSpecific(systemAcl, rawAcl, (AuditFlags)auditFlags, 
							new SecurityIdentifier (Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags) inheritanceFlags, (PropagationFlags)propagationFlags)) 
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
			}

			/*
			* Method Name: TestRemoveAuditSpecific
			*
			* Description:	check if after remove the specific audit ace the systemAcl has same content, number of ACEs, content of 
			*			each ACE and the order of the ACEs, as the rawAcl.
			*
			* Parameter:	systemAcl -- the SystemAcl to remove audit from
			*			rawAcl -- the validation RawAcl to compare with
			*			auditFlag, sid,  accessMask, inheritanceFlags, propagationFlags -- the paramether to pass to SetAudit
			*
			* Return:		true if test pass, false otherwise
			*/
			private static bool TestRemoveAuditSpecific(SystemAcl systemAcl, RawAcl rawAcl, AuditFlags auditFlag, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
			{
				bool result = true;
				byte [] sAclBinaryForm = null;
				byte [] rAclBinaryForm = null;					


				systemAcl.RemoveAuditSpecific(auditFlag, sid, accessMask, inheritanceFlags, propagationFlags);
				if(systemAcl.Count == rawAcl.Count &&
					systemAcl.BinaryLength == rawAcl.BinaryLength)
				{
					sAclBinaryForm = new byte[systemAcl.BinaryLength];
					rAclBinaryForm = new byte[rawAcl.BinaryLength];
					systemAcl.GetBinaryForm(sAclBinaryForm, 0);
					rawAcl.GetBinaryForm(rAclBinaryForm, 0);
					
					if(!Utils.UtilIsBinaryFormEqual(sAclBinaryForm, rAclBinaryForm))
						result = false;		

					//redundant index check					
					for (int i = 0; i < systemAcl.Count; i++)
					{
						if(!Utils.UtilIsAceEqual(systemAcl[i], rawAcl[i]))
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
				RawAcl rawAcl = null;
				SystemAcl systemAcl = null;
				bool isContainer = false;
				bool isDS = false;
				
				int auditFlags = 0;
				string sid = null;
				int accessMask = 1;
				int inheritanceFlags = 0;
				int propagationFlags = 0;
				GenericAce gAce = null;
				byte [] opaque = null;


				Console.WriteLine("Running AdditionalTestCases");
				

		
				//Case 1, remove one audit ACE from the SystemAcl with no ACE
				testCasesPerformed ++;


				try
				{					
					isContainer = true;
					isDS = false;
					auditFlags = 1;
					sid = "BA";
					accessMask = 1;
					inheritanceFlags = 3;
					propagationFlags = 3;
					rawAcl = new RawAcl(0, 1);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					if(TestRemoveAuditSpecific(systemAcl, rawAcl, (AuditFlags)auditFlags, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags) inheritanceFlags, (PropagationFlags) propagationFlags))
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

					

				//Case 2, remove the last one audit ACE from the SystemAcl
				testCasesPerformed ++;

				
				try
				{
					isContainer = true;
					isDS = false;
					auditFlags = 1;
					sid = "BA";
					accessMask = 1;
					inheritanceFlags = 3;
					propagationFlags = 3;
					rawAcl = new RawAcl(0, 1);
					//79 = AceFlags.SuccessfulAccess | AceFlags.ObjectInherit |AceFlags.ContainerInherit | AceFlags.NoPropagateInherit | AceFlags.InheritOnly
					gAce = new CommonAce((AceFlags)79,  AceQualifier.SystemAudit, accessMask, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);					
					rawAcl.InsertAce(rawAcl.Count, gAce);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					//remove the ace to create the validation rawAcl
					rawAcl.RemoveAce(rawAcl.Count - 1);
					if(TestRemoveAuditSpecific(systemAcl, rawAcl, (AuditFlags)auditFlags, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags) inheritanceFlags, (PropagationFlags) propagationFlags))
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
				testCasesPerformed ++;


				try
				{
					isContainer = true;
					isDS = false;
					auditFlags = 1;
					sid = "BA";
					accessMask = 0;
					inheritanceFlags = 3;
					propagationFlags = 3;
					rawAcl = new RawAcl(0, 1);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					systemAcl.RemoveAuditSpecific((AuditFlags)auditFlags, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags) inheritanceFlags, (PropagationFlags) propagationFlags);

					Console.WriteLine("Should not allow remove audit specific with 0 accessmask ");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
				
				}
				catch(ArgumentException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;					
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}

				

				//Case 4, Audit Qualifier None
				testCasesPerformed ++;


				try
				{					
					isContainer = true;
					isDS = false;
					auditFlags = 0;
					sid = "BA";
					accessMask = 1;
					inheritanceFlags = 3;
					propagationFlags = 3;
					rawAcl = new RawAcl(0, 1);				
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					systemAcl.RemoveAuditSpecific((AuditFlags)auditFlags, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags) inheritanceFlags, (PropagationFlags) propagationFlags);

					Console.WriteLine("Should not allow remove audit specific with no auditflags ");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
				
				}
				catch(ArgumentException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;					
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				//Case 5, null sid
				testCasesPerformed ++;

				
				try
				{
					isContainer = true;
					isDS = false;
					auditFlags = 1;
					accessMask = 1;
					sid = "BA";
					inheritanceFlags = 3;
					propagationFlags = 3;
					rawAcl = new RawAcl(0, 1);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					systemAcl.RemoveAuditSpecific ((AuditFlags)auditFlags, null, accessMask, (InheritanceFlags) inheritanceFlags, (PropagationFlags) propagationFlags);

					Console.WriteLine("Should not allow remove audit specific with null ");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
				
				}
				catch(ArgumentNullException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;					
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				//Case 6, all the ACEs in the Sacl are non-qualified ACE, no remove

				testCasesPerformed ++;


				try
				{
					isContainer = true;
					isDS = false;

					inheritanceFlags = 1;//InheritanceFlags.ContainerInherit
					propagationFlags = 2; //PropagationFlags.InheritOnly
					
					auditFlags = 3;
					sid = "BA";
					accessMask = 1;

					rawAcl = new RawAcl(0, 1);
					opaque = new byte[4];
					gAce = new CustomAce( AceType.MaxDefinedAceType + 1, 
						AceFlags.InheritanceFlags | AceFlags.AuditFlags, opaque);;
					rawAcl.InsertAce(0, gAce);
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);

					//After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
					//forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
					try
					{

						TestRemoveAuditSpecific(systemAcl, 
							rawAcl, 
							(AuditFlags) auditFlags, 
							new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), 
							accessMask, 
							(InheritanceFlags) inheritanceFlags, 
							(PropagationFlags) propagationFlags);

						Console.WriteLine("Should not allow RemoveAuditSpecific from ACL with customAce/CompoundAce");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	

					}
					catch(InvalidOperationException)
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
		*  Class to test SystemAcl method inherited from abstract base class CommonAcl
		*		 public void RemoveInheritedAces()
		*
		*
		*/
		//----------------------------------------------------------------------------------------------

		private class RemoveInheritedAcesTestCases
		{
			//No creating objects

			private RemoveInheritedAcesTestCases(){}

			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			RemoveInheritedAces
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/
			
			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{

				Console.WriteLine("Running RemoveInheritedAcesTestCases");

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
				

				
				bool isContainer = false;
				bool isDS = false;
				
				RawAcl rawAcl = null;
				SystemAcl systemAcl = null;
				
				GenericAce gAce = null;				
				byte revision = 0;
				int capacity = 0;

				//CustomAce constructor parameters
				AceType aceType = AceType.AccessAllowed;
				AceFlags aceFlag = AceFlags.None;
				byte [] opaque = null;	

				//CompoundAce constructor additional parameters
				int accessMask = 0;
				CompoundAceType compoundAceType = CompoundAceType.Impersonation;
				string sid = "BA";		

				//CommonAce constructor additional parameters
				AceQualifier aceQualifier = 0;

				//ObjectAce constructor additional parameters
				ObjectAceFlags objectAceFlag = 0;
				Guid objectAceType ;
				Guid inheritedObjectAceType;				

				//case 1, no Ace
				testCasesPerformed ++;

					
				try
				{
					revision = 127;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					isContainer = true;
					isDS = false;
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);

					if (TestRemoveInheritedAces(systemAcl))
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
				testCasesPerformed ++;

				
				try
				{
					revision = 0;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					//199 has all AceFlags except InheritOnly and Inherited
					gAce = new CommonAce((AceFlags)199, AceQualifier.SystemAudit, 1, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
					rawAcl.InsertAce(0, gAce);
					isContainer = false;
					isDS = false;
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);					

					if (TestRemoveInheritedAces(systemAcl))
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



				//case 3, only have one inherit Ace
				testCasesPerformed ++;

				
				try
				{
					revision = 0;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					//215 has all AceFlags except InheritOnly
					gAce = new CommonAce((AceFlags)215, AceQualifier.SystemAudit, 1, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
					rawAcl.InsertAce(0, gAce);
					isContainer = false;
					isDS = false;
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);					

					if (TestRemoveInheritedAces(systemAcl))
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


				if(isBVT )
				{
					//first 3 cases are BVT cases
					return;
				}					

				//case 4, have one explicit Ace and one inherited Ace
				testCasesPerformed ++;

				
				try
				{
					revision = 255;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					//199 has all AceFlags except InheritOnly and Inherited					
					gAce = new CommonAce((AceFlags)199, AceQualifier.SystemAudit, 1, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
					rawAcl.InsertAce(0, gAce);
					//215 has all AceFlags except InheritOnly					
					gAce = new CommonAce((AceFlags)215, AceQualifier.SystemAudit, 1, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);				
					rawAcl.InsertAce(1, gAce);					
					isContainer = true;
					isDS = false;
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);					

					if (TestRemoveInheritedAces(systemAcl))
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


				//case 5, have two inherited Aces
				testCasesPerformed ++;

				
				try
				{
					revision = 255;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					//215 has all AceFlags except InheritOnly					
					gAce = new CommonAce((AceFlags)215, AceQualifier.SystemAudit, 1, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
					rawAcl.InsertAce(0, gAce);
					sid = "BA";
					//16 has Inherited
					gAce = new CommonAce((AceFlags)16, AceQualifier.SystemAudit, 1, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);				
					rawAcl.InsertAce(0, gAce);					
					isContainer = true;
					isDS = false;
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);					

					if (TestRemoveInheritedAces(systemAcl))
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


				//case 6, 1 inherited CustomAce
				testCasesPerformed ++;

				
				try
				{
					revision = 127;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					aceType = AceType.MaxDefinedAceType + 1;
					aceFlag = (AceFlags)208; //SuccessfulAccess | FailedAccess | Inherited
					opaque = null;
					gAce = new CustomAce( aceType, aceFlag, opaque);
					rawAcl.InsertAce(0, gAce);
					isContainer = false;
					isDS = false;
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);					
					
					if (TestRemoveInheritedAces(systemAcl))
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



				//case 7,  1 inherited CompoundAce
				testCasesPerformed ++;

				
				try
				{
					revision = 127;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					aceFlag = (AceFlags)223; //all flags ored together
					accessMask = 1;
					compoundAceType = CompoundAceType.Impersonation;
					gAce = new CompoundAce( aceFlag, accessMask, compoundAceType, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)));
					rawAcl.InsertAce(0, gAce);
					isContainer = true;
					isDS = false;
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);					

					if (TestRemoveInheritedAces(systemAcl))
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


				//case 8, 1 inherited ObjectAce
				testCasesPerformed ++;

				
				try
				{
					revision = 127;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					aceFlag = (AceFlags)223; //all flags ored together
					aceQualifier = AceQualifier.SystemAudit;					
					accessMask = 1;
					objectAceFlag = ObjectAceFlags.ObjectAceTypePresent | ObjectAceFlags.InheritedObjectAceTypePresent;
					objectAceType = new Guid("11111111-1111-1111-1111-111111111111");
					inheritedObjectAceType = new Guid("22222222-2222-2222-2222-222222222222");
					gAce = new ObjectAce( aceFlag, aceQualifier, accessMask, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), objectAceFlag, objectAceType, inheritedObjectAceType, false, null);
      					rawAcl.InsertAce(0, gAce);
					isContainer = true;
					isDS = true;
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);					

					if (TestRemoveInheritedAces(systemAcl))
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

			/*
			* Method Name: TestRemoveInheritedAces
			*
			* Description:	check if after remove inherited aces, there is any inherited ace
			*
			* Parameter:	systemAcl -- the SystemAcl to remove inherited aces audit from
			*
			* Return:		true if test pass, false otherwise
			*/
			private static bool TestRemoveInheritedAces(SystemAcl systemAcl)
			{

				GenericAce ace = null;


				systemAcl.RemoveInheritedAces();
				for( int i = 0; i < systemAcl.Count; i ++)
				{
					ace = systemAcl[i];
					if((ace.AceFlags & AceFlags.Inherited) != 0)
						return false;
				}

				return true;
			}
		}


		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test SystemAcl method inherited from abstract base class CommonAcl
		*		 public void Purge( SecurityIdentifier sid )
		*
		*
		*/
		//----------------------------------------------------------------------------------------------

		private class PurgeTestCases
		{
			//No creating objects

			private PurgeTestCases(){}


			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			Purge
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/

			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running PurgeTestCases");

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
				


				bool isContainer = false;
				bool isDS = false;
				
				RawAcl rawAcl = null;
				SystemAcl systemAcl = null;
				int aceCount = 0;
				SecurityIdentifier sid = null;
				
				GenericAce gAce = null;				
				byte revision = 0;
				int capacity = 0;

				//CustomAce constructor parameters
				AceType aceType = AceType.AccessAllowed;
				AceFlags aceFlag = AceFlags.None;
				byte [] opaque = null;	

				//CompoundAce constructor additional parameters
				int accessMask = 0;
				CompoundAceType compoundAceType = CompoundAceType.Impersonation;
				string sidStr = "BG";		

				//CommonAce constructor additional parameters
				AceQualifier aceQualifier = 0;

				//ObjectAce constructor additional parameters
				ObjectAceFlags objectAceFlag = 0;
				Guid objectAceType ;
				Guid inheritedObjectAceType;				

				//case 1, no Ace
				testCasesPerformed ++;

				
				try
				{
					revision = 127;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					isContainer = true;
					isDS = false;
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					aceCount = 0;
					sidStr = "BG";
					sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sidStr));

					if (TestPurge(systemAcl, sid, aceCount))
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
				testCasesPerformed ++;

				
				try
				{
					revision = 0;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					sidStr = "BG";
					sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sidStr));
					//199 has all aceflags but inheritedonly and inherited					
					gAce = new CommonAce((AceFlags)199, AceQualifier.SystemAudit, 1, sid, false, null);
					rawAcl.InsertAce(0, gAce);
					isContainer = false;
					isDS = false;
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					aceCount = 0;

					if (TestPurge(systemAcl, sid, aceCount))
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
				testCasesPerformed ++;

				
				try
				{
					revision = 0;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					//199 has all aceflags but inheritedonly and inherited
					sidStr = "BG";
					sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sidStr));
					gAce = new CommonAce((AceFlags)199, AceQualifier.SystemAudit, 1, sid, false, null);
					rawAcl.InsertAce(0, gAce);
					isContainer = false;
					isDS = false;
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					aceCount = 1;
					sidStr = "BA";
					sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sidStr));

					if (TestPurge(systemAcl, sid, aceCount))
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


				if(isBVT )
				{
					//first 3 cases are BVT cases
					return;
				}					

				//case 4, only have 1 inherited Ace of the sid
				testCasesPerformed ++;

				try
				{
					revision = 0;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					sidStr = "BG";
					sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sidStr));
					//215 has all aceflags but inheritedonly				
					gAce = new CommonAce((AceFlags)215, AceQualifier.SystemAudit, 1, sid, false, null);
					rawAcl.InsertAce(0, gAce);
					isContainer = false;
					isDS = false;
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					aceCount = 1;

					if (TestPurge(systemAcl, sid, aceCount))
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
				testCasesPerformed ++;

				
				try
				{
					revision = 255;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					sidStr = "BG";
					sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sidStr));
					//199 has all aceflags but inheritedonly and inherited
					gAce = new CommonAce((AceFlags)199, AceQualifier.SystemAudit, 1, sid, false, null);
					rawAcl.InsertAce(0, gAce);
					//215 has all aceflags but inheritedonly
					gAce = new CommonAce((AceFlags)215, AceQualifier.SystemAudit, 2, sid, false, null);				
					rawAcl.InsertAce(1, gAce);					
					isContainer = true;
					isDS = false;
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					aceCount = 1;

					if (TestPurge(systemAcl, sid, aceCount))
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
				testCasesPerformed ++;

				
				try
				{
					revision = 255;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					sidStr = "BG";
					sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sidStr));			
					gAce = new CommonAce(AceFlags.FailedAccess, AceQualifier.SystemAudit, 1, sid, false, null);
					rawAcl.InsertAce(0, gAce);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 2, sid, false, null);				
					rawAcl.InsertAce(0, gAce);					
					isContainer = true;
					isDS = false;
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					aceCount = 0;

					if (TestPurge(systemAcl, sid, 0))
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
				testCasesPerformed ++;

				
				try
				{
					revision = 127;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					aceType = AceType.MaxDefinedAceType + 1;
					//199 has all aceflags but inheritedonly and inherited					
					aceFlag = (AceFlags)199; 
					opaque = null;
					gAce = new CustomAce( aceType, aceFlag, opaque);
					rawAcl.InsertAce(0, gAce);
					isContainer = false;
					isDS = false;
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG"));
					aceCount = 1;

					//After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
					//forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
					try
					{

						TestPurge(systemAcl, sid, aceCount);

						Console.WriteLine("Should not allow Purge from ACL with customAce/CompoundAce");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	

					}
					catch(InvalidOperationException)
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
				testCasesPerformed ++;

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
					gAce = new CompoundAce( aceFlag, accessMask, compoundAceType, sid);
					rawAcl.InsertAce(0, gAce);
					isContainer = true;
					isDS = false;
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					aceCount = 0;

					//After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
					//forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
					try
					{

						TestPurge(systemAcl, sid, aceCount);

						Console.WriteLine("Should not allow Purge from ACL with customAce/CompoundAce");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	

					}
					catch(InvalidOperationException)
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
				testCasesPerformed ++;

				
				try
				{
					revision = 127;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG"));
					//207 has all AceFlags but inherited						
					aceFlag = (AceFlags)207;
					aceQualifier = AceQualifier.SystemAudit;					
					accessMask = 1;
					objectAceFlag = ObjectAceFlags.ObjectAceTypePresent | ObjectAceFlags.InheritedObjectAceTypePresent;
					objectAceType = new Guid("11111111-1111-1111-1111-111111111111");
					inheritedObjectAceType = new Guid("22222222-2222-2222-2222-222222222222");
					gAce = new ObjectAce( aceFlag, aceQualifier, accessMask, sid, objectAceFlag, objectAceType, inheritedObjectAceType, false, null);
      					rawAcl.InsertAce(0, gAce);
					isContainer = true;
					isDS = true;
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
					aceCount = 0;

					if (TestPurge(systemAcl, sid, aceCount))
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

				Console.WriteLine("Running AdditionalTestCases");
				

				
				bool isContainer = false;
				bool isDS = false;
				
				RawAcl rawAcl = null;
				SystemAcl systemAcl = null;
								
				byte revision = 0;
				int capacity = 0;

				//case 1, null Sid
				testCasesPerformed ++;

				
				try
				{
					revision = 127;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					isContainer = true;
					isDS = false;
					systemAcl = new SystemAcl(isContainer, isDS, rawAcl);

					systemAcl.Purge(null);
					Console.WriteLine("Should not allow purge null sid");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
				}
				catch(ArgumentNullException)
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
			* Method Name: TestPurge
			*
			* Description:	check if after purge, there is any explicit ace with the same sid
			*
			* Parameter:	systemAcl -- the SystemAcl to purge explicit aces from
			*			sid -- the sid to pick the aces to purge
			*			aceCount -- the expected number of ACE after purge
			*
			* Return:		true if test pass, false otherwise
			*/

			private static bool TestPurge(SystemAcl systemAcl, SecurityIdentifier sid, int aceCount)
			{

				KnownAce ace = null;

				systemAcl.Purge(sid);
				if(aceCount != systemAcl.Count)
					return false;
				for( int i = 0; i < systemAcl.Count; i ++)
				{
					ace = systemAcl[i] as KnownAce;
					if(ace != null && ((ace.AceFlags & AceFlags.Inherited) == 0))
					{
						if(ace.SecurityIdentifier == sid )
							return false;
					}
				}
				return true;
			}
		}
		

		private class GetBinaryFormTestCases
		{
			//No creating objects
			private GetBinaryFormTestCases(){}

			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{

				Console.WriteLine("Running GetBinaryFormTestCases.AllTestCases()");
				BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
				AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
			}

			public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				SystemAcl sAcl = null;
				RawAcl rAcl = null;				
				GenericAce gAce = null;
				byte [] binaryForm = null;
				
				Console.WriteLine("Running BasicValidationTestCases");
				


				//Case 1, array binaryForm is null
				testCasesPerformed ++;


				try
				{
					rAcl = new RawAcl(GenericAcl.AclRevision, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);
					sAcl = new SystemAcl(false, false, rAcl);
					try
					{
						sAcl.GetBinaryForm( binaryForm, 0);					
						Console.WriteLine("Should throw ArgumentNullException when binaryForm is null");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					}
					catch(ArgumentNullException)
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
				testCasesPerformed ++;


				try
				{
					binaryForm = new byte[100];
					rAcl = new RawAcl(GenericAcl.AclRevision, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);
					sAcl = new SystemAcl(false, false, rAcl);
					try
					{
						sAcl.GetBinaryForm( binaryForm, -1);					
						Console.WriteLine("Should throw ArgumentOutOfRangeException when offset is negative");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					}
					catch(ArgumentOutOfRangeException)
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
				testCasesPerformed ++;


				try
				{
					binaryForm = new byte[100];
					rAcl = new RawAcl(GenericAcl.AclRevision, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);
					sAcl = new SystemAcl(false, false, rAcl);
					try
					{
						sAcl.GetBinaryForm( binaryForm, binaryForm.Length);					
						Console.WriteLine("Should throw ArgumentOutOfRangeException when offset equals to array length");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					}
					catch(ArgumentOutOfRangeException)
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
				testCasesPerformed ++;


				try
				{
					rAcl = new RawAcl(GenericAcl.AclRevision, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);					
					rAcl.InsertAce(0, gAce);
					sAcl = new SystemAcl(false, false, rAcl);
					binaryForm = new byte[sAcl.BinaryLength + 10000];					

					sAcl.GetBinaryForm( binaryForm, 10000);
					//get the binaryForm of the original RawAcl
					byte[] verifierBinaryForm = new byte[rAcl.BinaryLength];
					rAcl.GetBinaryForm(verifierBinaryForm, 0);
					if(Utils.UtilIsBinaryFormEqual(binaryForm, 10000, verifierBinaryForm))
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
				testCasesPerformed ++;


				try
				{
					binaryForm = new byte[4];
					rAcl = new RawAcl(GenericAcl.AclRevision, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, 
						new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);
					sAcl = new SystemAcl(false, false, rAcl);
					try
					{
						sAcl.GetBinaryForm( binaryForm, 0);					
						Console.WriteLine("Should throw ArgumentOutOfRangeException when array has insufficient size");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					}
					catch(ArgumentOutOfRangeException)
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


    
