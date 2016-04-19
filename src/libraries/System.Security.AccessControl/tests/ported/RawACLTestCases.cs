//--------------------------------------------------------------------------
//
//		RawAcl test cases
//
//		Tests the RawAcl and the base abstract class GenericACL's functionality
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


	using BOOL = System.Int32;
	using DWORD = System.UInt32;
	using ULONG = System.UInt32;
	internal sealed class Win32AclLayer
	{
		internal const int ERROR_NOT_ENOUGH_MEMORY = 0x8;
		internal const int VER_PLATFORM_WIN32_NT = 2;		

		[DllImport("Advapi32.dll", EntryPoint="InitializeAcl", CharSet=CharSet.Unicode, SetLastError=true)]
		internal static extern BOOL InitializeAclNative(
		byte[] acl,
		DWORD aclLength,
		DWORD aclRevision);

		[DllImport("Advapi32.dll", EntryPoint="AddAccessAllowedAceEx", CharSet=CharSet.Unicode, SetLastError=true)]
		internal static extern BOOL AddAccessAllowedAceExNative(
		byte[] acl,
		DWORD aclRevision,
		DWORD aceFlags,
		DWORD accessMask,
		byte[] sid);

		[DllImport("Advapi32.dll", EntryPoint="AddAccessDeniedAceEx", CharSet=CharSet.Unicode, SetLastError=true)]
		internal static extern BOOL AddAccessDeniedAceExNative(
		byte[] acl,
		DWORD aclRevision,
		DWORD aceFlags,
		DWORD accessMask,
		byte[] sid);

		[DllImport("Advapi32.dll", EntryPoint="AddAuditAccessAceEx", CharSet=CharSet.Unicode, SetLastError=true)]
		internal static extern BOOL AddAuditAccessAceExNative(
		byte[] acl,
		DWORD aclRevision,
		DWORD aceFlags,
		DWORD accessMask,
		byte[] sid,
		BOOL bAuditSccess,
		BOOL bAuditFailure);		
	}	

	public enum AceTypeToInitiate
	{
		CustomAce = 1,
		CompoundAce = 2,
		CommonAce = 3,
		ObjectAce = 4,
	}	

	//----------------------------------------------------------------------------------------------
	/*
	*  Class to test RawAcl and its abstract base class GenericAcl
	*
	*
	*/
	//----------------------------------------------------------------------------------------------


	public class RawAclTestCases
	{
		public static readonly string ConstructorTestCaseStore = "TestCaseStores\\RawAcl_Constructor.inf";	
		public static readonly string RemoveAceTestCaseStore = "TestCaseStores\\RawAcl_RemoveAce.inf";	
		
		/*
		* Constructor
		*
		*/
		public RawAclTestCases() {}

        public static Boolean Test()
        {
            Console.WriteLine("\n\n=======STARTING RawACLTestCases==========\n");
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
			Console.WriteLine("Running RawAclTestCases");

            int testCasesPerformed = 0;
            int testCasesPassed = 0;


			ConstructorTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


   			    CreateFromBinaryFormTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);		


			AceCountTestCases.AllTestCases(ref testCasesPerformed, ref  testCasesPassed);


			BinaryLengthTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);			


			GetBinaryFormTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


			IndexTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);			


			InsertAceTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);			


			RemoveAceTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);

		//properties\methods inherited from GenericAcl class

			ExplicitInterfaceCopyToTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


			CopyToTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


			IsSynchronizedTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


			SyncRootTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


			ExplicitInterfaceGetEnumeratorTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


			GetEnumeratorTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);

            return (testCasesPerformed == testCasesPassed);
			
		}

		public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
		{
			Console.WriteLine("Running RawAclTestCases");
			
			Console.WriteLine("Running ConstructorTestCases");
			ConstructorTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);

			Console.WriteLine("Running CreateFromBinaryFormTestCases");
			CreateFromBinaryFormTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
			
			Console.WriteLine("Running AceCountTestCases");
			AceCountTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);

			Console.WriteLine("Running BinaryLengthTestCases");
			BinaryLengthTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);

			Console.WriteLine("Running IndexTestCases");
			IndexTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);

			Console.WriteLine("Running InsertAceTestCases");
			InsertAceTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);

			Console.WriteLine("Running RemoveAceTestCases");
			RemoveAceTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);

			//properties\methods inherited from GenericAcl class
			Console.WriteLine("Running ExplicitInterfaceCopyToTestCases");			
			ExplicitInterfaceCopyToTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
			
			Console.WriteLine("Running CopyToTestCases");			
			CopyToTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);

			Console.WriteLine("Running IsSynchronizedTestCases");		
			IsSynchronizedTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
			
			Console.WriteLine("Running SyncRootTestCases");				
			SyncRootTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);

			Console.WriteLine("Running ExplicitInterfaceGetEnumeratorTestCases");				
			ExplicitInterfaceGetEnumeratorTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
			
			Console.WriteLine("Running GetEnumeratorTestCases");			
			GetEnumeratorTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);

		}

		public static void BVTTestCases(ref int testCasesPerformed, ref int testCasesPassed)
		{
			// No test cases yet
		}

		/*
		* Method Name: UtilCopyRawACL
		*
		* Description:	utility function, deep copy a rawAcl
		*
		* Parameter:	rawAcl -- the RawAcl to be copied
		*
		* Return:		the RawAcl objected created 
		*/
		private static RawAcl UtilCopyRawACL(RawAcl rawAcl)
		{
			byte []binaryForm = new byte[rawAcl.BinaryLength];
			rawAcl.GetBinaryForm(binaryForm, 0);
			return new RawAcl(binaryForm, 0);
		}

		/*
		* Method Name: UtilIsAclPartialEqual
		*
		* Description:	utility function, check if the two GenericAcl acl1 and acl2 are partially equal in the range from 
		* 			corresponding startaceindex to endaceindex
		*
		* Parameter:	acl1, acl2 -- the two GenericAcls to be checked
		*			acl1StartAceIndex, acl2StartAceIndex -- the start index of acl1 and acl2 to be compared
		*			acl1EndAceIndex, acl2EndAceIndex -- the end index of acl1 and acl2 to be compared
		*
		* Return:		true if equal, false if not equal
		*/
		private static bool UtilIsAclPartialEqual(GenericAcl acl1, GenericAcl acl2, int acl1StartAceIndex, int acl1EndAceIndex, int acl2StartAceIndex, int acl2EndAceIndex )
		{
			int index1 = 0;
			int index2 = 0;
			bool result = true;

			if(null != acl1 && null != acl2)
			{
				if(acl1StartAceIndex < 0 || acl1EndAceIndex < 0 || acl1StartAceIndex > acl1.Count -1 || acl1EndAceIndex > acl1.Count - 1 ||
					acl2StartAceIndex < 0 || acl2EndAceIndex < 0 || acl2StartAceIndex > acl2.Count -1 || acl2EndAceIndex > acl2.Count - 1 )
				{
					//the caller has garenteeed the index calculation is correct so if any above condition hold, 
					//that means the range of the index is invalid
					return true;
				}
				if(acl1EndAceIndex - acl1StartAceIndex != acl2EndAceIndex - acl2StartAceIndex)
				{
					result = false;
				}
				else
				{
					for (index1 = acl1StartAceIndex, index2 = acl2StartAceIndex; index1 <= acl1EndAceIndex; index1 ++, index2 ++)
					{
						if(!Utils.UtilIsAceEqual(acl1[index1], acl2[index2]))
						{
							result = false;
							break;
						}
					}
				}
			}
			else if ( null == acl1 && null == acl2)
			{
				Console.WriteLine("Both acls are null");
			}
			else
				result = false;
			
			return result;
		}

		/*
		* Method Name: UtilTestAceCollectionCopy
		*
		* Description:	check the array gAces has same ACEs as the RawAcl object rawAcl, This function
		*			is used by both ExplicitInterfaceCopyToTestCases and CopyToTestCases
		*
		* Parameter:	rawAcl -- the RawAcl object from which the ACE array is created
		*			gAces -- the ACEs array
		*
		* Return:		true if test pass, false otherwise
		*/

		private static bool UtilTestAceCollectionCopy(RawAcl rawAcl, GenericAce[] gAces, int offset)
		{
			bool result = true;
			if(offset != 0)
			{//check copyto does not write anything before offset
				for(int i = 0; i<offset; i++)
				{
					if(gAces[i] != null)
					{// gAces has been initialized to all null before copyto
						result = false;
						break;
					}
				}
			}
			for ( int i = 0 ; i < rawAcl.Count; i ++)
			{
				if(!Utils.UtilIsAceEqual(rawAcl[i], gAces[i + offset]))
				{
					result = false;
					break;
				}
			}
			if(offset + rawAcl.Count < gAces.Length)
			{//check copyto does not write to the rest of the array
				for(int i = 0; i<offset; i++)
				{
					if(gAces[i] != null)
					{// gAces has been initialized to all null before copyto
						result = false;
						break;
					}
				}				
			}
			return result;
		}

			
		/*
		* Method Name: UtilTestGetEnumerator
		*
		* Description:	Verify the enumerator will enumerate exactly all aces of the RawAcl, the caller will
		*			make sure each ace is unique for comparision. This function
		*			is used by both ExplicitInterfaceGetEnumeratorTestCases and GetEnumeratorTestCases
		*
		* Parameter:	enumerator -- the enumerator from create from the rawAcl
		*			rAcl -- a RawAcl object
		*			isExplicit -- true means IEnumerator, false means AceEnumerator
		*
		*
		* Return:		true if test pass, false otherwise
		*/


			private static bool UtilTestGetEnumerator(IEnumerator enumerator, RawAcl rAcl, bool isExplicit)
			{

				bool result = false;//assume failure
				GenericAce gAce = null;

				if(!(isExplicit?enumerator.MoveNext(): ((AceEnumerator)enumerator).MoveNext()))				
				{//enumerator is created from empty RawAcl
					if(0 != rAcl.Count )
						return false;
					else
						return true;
				}
				else if (0 == rAcl.Count)
				{//rawAcl is empty but enumerator is still enumerable
					return false;
				}
				else//non-empty rAcl, non-empty enumerator
				{
					//check all aces enumerated are in the RawAcl
					if(isExplicit)
					{
						enumerator.Reset();
					}
					else
					{
						((AceEnumerator)enumerator).Reset();						
					}
					while(isExplicit?enumerator.MoveNext(): ((AceEnumerator)enumerator).MoveNext())
					{
						gAce = (GenericAce) (isExplicit?enumerator.Current: ((AceEnumerator)enumerator).Current);
						//check this gAce exists in the RawAcl
						for(int i=0; i<rAcl.Count; i++)
						{
							if(GenericAce.ReferenceEquals(gAce, rAcl[i]))
							{//found
								result = true;
								break;
							}
						}
						if(!result)
						{//not exists in the RawAcl, failed
							return false;
						}
						//enumerate to next one
					}

					//check all aces of rAcl are enumerable by the enumerator
					result = false; //assume failure
					for(int i = 0; i<rAcl.Count; i++)
					{
						gAce = rAcl[i];
						//check this gAce is enumerable
						if(isExplicit)
						{
							enumerator.Reset();
						}
						else
						{
							((AceEnumerator)enumerator).Reset();						
						}
						
						while(isExplicit?enumerator.MoveNext(): ((AceEnumerator)enumerator).MoveNext())
						{
							if(GenericAce.ReferenceEquals((GenericAce)(isExplicit?enumerator.Current: ((AceEnumerator)enumerator).Current), gAce))
							{
								result = true;
								break;
							}
						}
						if(!result)
						{//not enumerable
							return false;
						}
						//check next ace in the rAcl
					}
					//now all passed
					return true;
				}

			}

		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test RawAcl constructor
		*	        public RawAcl( byte revision, int capacity )
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
			*			RawAcl constructor
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/
			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running ConstructorTestCases");

				BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
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
			public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running BasicValidationTestCases");
				

				
				string[] testCase = null;
				ITestCaseReader reader = new InfTestCaseStore();
				reader.OpenTestCaseStore(ConstructorTestCaseStore); 

				while (null != (testCase = reader.ReadNextTestCase()))
				{
					// read revision
					byte revision;
					if (1 > testCase.Length)
						throw new ArgumentOutOfRangeException();
					else
						revision = byte.Parse(testCase[0]);

					// read capacity
					int capacity;
					if (2 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else 
						capacity = int.Parse(testCase[1]);
                                   
					testCasesPerformed ++;


					try
					{
					
						if (TestConstructor(revision, capacity))
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

				reader.CloseTestCaseStore();
			}

			/*
			* Method Name: TestConstructor
			*
			* Description:	check test case passes or fails  
			*
			* Parameter:	revision -- the revision to create the RawAcl object
			*			capacity -- the capacity to create the RawAcl object
			*
			* Return:		true if test pass, false otherwise
			*/
			private static bool TestConstructor(byte revision, int capacity)
			{
				
				RawAcl rawAcl = null;
				

				rawAcl = new RawAcl(revision, capacity);
				if(revision == rawAcl.Revision && 0 == rawAcl.Count && 8 == rawAcl.BinaryLength)
					return true;
				else
				{
					Console.WriteLine("the newly created RawAcl is not expected");
					return false;
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
				byte revision = 0;
				int capacity = 0;

				Console.WriteLine("Running AdditionalTestCases");
				



				//case 1, capacity = -1
				testCasesPerformed ++;

				try
				{
					revision = 0;
					capacity = -1;
					rawAcl = new RawAcl(revision, capacity);
					Console.WriteLine("Should not allow creation of negative capacity RawAcl");
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
					revision = 0;
					capacity = Int32.MaxValue/2;
					if (TestConstructor(revision, capacity))
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;		
					}
					else
					{
						Console.WriteLine("the newly created RawAcl is not expected");
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
					revision = 0;
					capacity = Int32.MaxValue;
					if (TestConstructor(revision, capacity))
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;		
					}
					else
					{
						Console.WriteLine("the newly created RawAcl is not expected");
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
		}

		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test RawAcl method
		*		public RawAcl( byte[] binaryForm, int offset )
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
			*			RawAcl constructor
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/

			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running CreateFromBinaryFormTestCases");

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
				byte [] binaryForm = null;
				int offset = 0;
				
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
				
				//case 1, a valid binary representation with revision 0, 1 SystemAudit CommonAce
				testCasesPerformed ++;

			
				try
				{
					revision = 0;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
					rawAcl.InsertAce(0, gAce);
					binaryForm = new byte[rawAcl.BinaryLength];
					rawAcl.GetBinaryForm(binaryForm, 0);
					if (TestCreateFromBinaryForm(binaryForm, offset, revision, 1, rawAcl.BinaryLength))
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


				//case 2, a valid binary representation with revision 255, 1 AccessAllowed CommonAce
				testCasesPerformed ++;

				try
				{
					revision = 255;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
					rawAcl.InsertAce(0, gAce);
					binaryForm = new byte[rawAcl.BinaryLength];
					rawAcl.GetBinaryForm(binaryForm, 0);
					if (TestCreateFromBinaryForm(binaryForm, offset, revision, 1, rawAcl.BinaryLength))
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

				

				//case 3, a valid binary representation with revision 127, 1 CustomAce
				testCasesPerformed ++;

				try
				{

					revision = 127;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					aceType = AceType.MaxDefinedAceType + 1;
					aceFlag = (AceFlags)223; //all flags ored together
					opaque = null;
					gAce = new CustomAce( aceType, aceFlag, opaque);
					rawAcl.InsertAce(0, gAce);
					binaryForm = new byte[rawAcl.BinaryLength];
					rawAcl.GetBinaryForm(binaryForm, 0);
					if (TestCreateFromBinaryForm(binaryForm, offset, revision, 1, rawAcl.BinaryLength))
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



				//case 4, a valid binary representation with revision 1, 1 CompoundAce
				testCasesPerformed ++;

				try
				{
					revision = 127;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					aceFlag = (AceFlags)223; //all flags ored together
					accessMask = 1;
					compoundAceType = CompoundAceType.Impersonation;
					gAce = new CompoundAce( aceFlag, accessMask, compoundAceType, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)));
					rawAcl.InsertAce(0, gAce);
					binaryForm = new byte[rawAcl.BinaryLength];
					rawAcl.GetBinaryForm(binaryForm, 0);
					if (TestCreateFromBinaryForm(binaryForm, offset, revision, 1, rawAcl.BinaryLength))
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

				

				//case 5, a valid binary representation with revision 1, 1 ObjectAce
				testCasesPerformed ++;

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
					gAce = new ObjectAce( aceFlag, aceQualifier, accessMask, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), objectAceFlag, objectAceType, inheritedObjectAceType, false, null);
      					rawAcl.InsertAce(0, gAce);
					binaryForm = new byte[rawAcl.BinaryLength];
					rawAcl.GetBinaryForm(binaryForm, 0);
					if (TestCreateFromBinaryForm(binaryForm, offset, revision, 1, rawAcl.BinaryLength))
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


				//case 6, a valid binary representation with revision 1, no Ace
				testCasesPerformed ++;

				try
				{
					revision = 127;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					binaryForm = new byte[rawAcl.BinaryLength];
					rawAcl.GetBinaryForm(binaryForm, 0);
					if (TestCreateFromBinaryForm(binaryForm, offset, revision, 0, rawAcl.BinaryLength))
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


				//case 7, a valid binary representation with revision 1, and all Aces from case 1 to 5
				testCasesPerformed ++;

				try
				{
					revision = 127;
					capacity = 5;
					rawAcl = new RawAcl(revision, capacity);
					//SystemAudit CommonAce
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
					rawAcl.InsertAce(0, gAce);
					//Access Allowed CommonAce
					gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
					rawAcl.InsertAce(0, gAce);
					//CustomAce
					aceType = AceType.MaxDefinedAceType + 1;
					aceFlag = (AceFlags)223; //all flags ored together
					opaque = null;
					gAce = new CustomAce( aceType, aceFlag, opaque);
					rawAcl.InsertAce(0, gAce);
					//CompoundAce
					aceFlag = (AceFlags)223; //all flags ored together
					accessMask = 1;
					compoundAceType = CompoundAceType.Impersonation;
					gAce = new CompoundAce( aceFlag, accessMask, compoundAceType, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)));
					rawAcl.InsertAce(0, gAce);
					//ObjectAce
					aceFlag = (AceFlags)223; //all flags ored together
					aceQualifier = AceQualifier.AccessAllowed;					
					accessMask = 1;
					objectAceFlag = ObjectAceFlags.ObjectAceTypePresent | ObjectAceFlags.InheritedObjectAceTypePresent;
					objectAceType = new Guid("11111111-1111-1111-1111-111111111111");
					inheritedObjectAceType = new Guid("22222222-2222-2222-2222-222222222222");
					gAce = new ObjectAce( aceFlag, aceQualifier, accessMask, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), objectAceFlag, objectAceType, inheritedObjectAceType, false, null);
      					rawAcl.InsertAce(0, gAce);					
					binaryForm = new byte[rawAcl.BinaryLength];
					rawAcl.GetBinaryForm(binaryForm, 0);
					if (TestCreateFromBinaryForm(binaryForm, offset, revision, 5, rawAcl.BinaryLength))
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
				byte [] binaryForm = null;
				int offset = 0;

				Console.WriteLine("Running AdditionalTestCases");
				

				
				//case 1, binaryForm is null
				testCasesPerformed ++;

				try
				{
					binaryForm = null;
					offset = 0;					
					rawAcl = new RawAcl(binaryForm, offset);
					Console.WriteLine("Should not allow CreateFromBinaryForm succeed with null binaryForm");
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

				

				//case 2, binaryForm is empty
				testCasesPerformed ++;

				try
				{
					binaryForm = new byte[0];
					offset = 0;
					rawAcl = new RawAcl(binaryForm, offset);
					Console.WriteLine("Should not allow CreateFromBinaryForm succeed with empty binaryForm");
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

				

				//case 3, negative offset 				
				testCasesPerformed ++;

				try
				{
					binaryForm = new byte[100];
					offset = -1;
					rawAcl = new RawAcl(binaryForm, offset);
					Console.WriteLine("Should not allow CreateFromBinaryForm succeed with negative offset");
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

				

				//case 4, binaryForm length less than GenericAcl.HeaderLength
				testCasesPerformed ++;

				try
				{
					binaryForm = new byte[4];
					offset = 0;
					rawAcl = new RawAcl(binaryForm, offset);
					Console.WriteLine("Should not allow CreateFromBinaryForm succeed with binaryForm length less than GenericAcl.HeaderLength");
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


				//case 5, a RawAcl of length 64K. RawAcl length = HeaderLength + all ACE's  length
				// = HeaderLength + (HeaderLength + OpaqueLength) * num_of_custom_ace
				// = 8 + ( 4 + OpaqueLength) * num_of_custom_ace
				testCasesPerformed ++;


				GenericAce gAce = null;				
				byte revision = 0;
				int capacity = 0;
				string sid = "BG";

				//CustomAce constructor parameters
				AceType aceType = 0;
				AceFlags aceFlag = 0;
				byte [] opaque = null;	
				
				try
				{
					revision = 127;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					aceType = AceType.MaxDefinedAceType + 1;
					aceFlag = (AceFlags)223; //all flags ored together
					opaque = new byte[GenericAcl.MaxBinaryLength -3 - 8 - 4];//GenericAcl.MaxBinaryLength = 65535, is not multiple of 4
					gAce = new CustomAce( aceType, aceFlag, opaque);
					rawAcl.InsertAce(0, gAce);
					binaryForm = new byte[rawAcl.BinaryLength];
					rawAcl.GetBinaryForm(binaryForm, 0);
					if (TestCreateFromBinaryForm(binaryForm, offset, revision, 1, rawAcl.BinaryLength))
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


				{
				//case 6, a RawAcl of length 64K + 1. RawAcl length = HeaderLength + all ACE's  length
				// = HeaderLength + (HeaderLength + OpaqueLength) * num_of_custom_ace
				// = 8 + ( 4 + OpaqueLength) * num_of_custom_ace
				testCasesPerformed ++;


				gAce = null;
				sid = "BA";

				//CustomAce constructor parameters
				aceType = 0;
				aceFlag = 0;
				binaryForm = new byte[65536];
				
				try
				{
					revision = 127;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					rawAcl.GetBinaryForm(binaryForm, 0);
					//change the length bytes to 65535
					binaryForm[2] = 0xf;
					binaryForm[3] = 0xf;
					//change the aceCount to 1
					binaryForm[4] = 1;
					aceType = AceType.MaxDefinedAceType + 1;
					aceFlag = (AceFlags)223; //all flags ored together
					opaque = new byte[GenericAcl.MaxBinaryLength + 1 - 8 - 4];//GenericAcl.MaxBinaryLength = 65535, is not multiple of 4
					gAce = new CustomAce( aceType, aceFlag, opaque);
					gAce.GetBinaryForm(binaryForm, 8);

					TestCreateFromBinaryForm(binaryForm, 0, revision, 1, binaryForm.Length);
					Console.WriteLine("Should not allow create RawAcl from greater than GenericAcl.MaxBinaryLength byte array");				
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);					
				}
				catch(ArgumentException)
				{//should throw this exception
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;						
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				}
				
				//case 7, a valid binary representation with revision 255, 256 Access
				//CommonAce to test the correctness of  the process of the AceCount in the header
				testCasesPerformed ++;

				try
				{
					revision = 255;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					for(int i= 0; i < 256; i++)
					{
						gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, i + 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
						rawAcl.InsertAce(0, gAce);
					}
					binaryForm = new byte[rawAcl.BinaryLength + 1000];
					rawAcl.GetBinaryForm(binaryForm, 1000);
					if (TestCreateFromBinaryForm(binaryForm, 1000, revision, 256, rawAcl.BinaryLength))
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed, null);
						testCasesPassed++;
					}
					else
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed, null);					
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				//case 8, array containing garbage
				testCasesPerformed ++;

				try
				{
					binaryForm = new byte[]{1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};		
					TestCreateFromBinaryForm(binaryForm, offset, revision, 1, 12);
					Console.WriteLine("Should not allow create RawAcl from garbage byte array");				
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);			
				}
				catch(ArgumentOutOfRangeException )					
				{//this binary form shows the length will be 257, is bigger than the binary form array length
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;						
				}				
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				//case 9, array containing garbage
				testCasesPerformed ++;

				try
				{//binary form shows the length will be 1, actual length is 12
					binaryForm = new byte[]{1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1};		
					TestCreateFromBinaryForm(binaryForm, offset, revision, 1, 12);
					Console.WriteLine("Should not allow create RawAcl from garbage byte array");				
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);			
				}
				catch(ArgumentOutOfRangeException )					
				{//this binary form shows the length will be 1, is shorter than the binary form array length
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;						
				}				
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				//case 10, array containing garbage
				testCasesPerformed ++;

				try
				{
					binaryForm = new byte[]{1, 1, 12, 0, 1, 1, 1, 1, 1, 1, 1, 1};		
					TestCreateFromBinaryForm(binaryForm, offset, revision, 1, 12);

					Console.WriteLine("Should not allow create RawAcl from garbage byte array");				
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
					
				}
				catch(ArgumentOutOfRangeException )					
				{//this binary form shows first ACE length will be 257, is bigger than the ACE's binary form array length
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;						
				}				
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}

	
			}


			/*
			* Method Name: TestCreateFromBinaryForm
			*
			* Description:	check test case passes or fails  
			*
			* Parameter:	binaryForm -- a byte array of a RawAcl
			*			offset -- the starting index of the RawAcl in the byte array binaryForm
			*
			* Return:		true if test pass, false otherwise
			*/

			private static bool TestCreateFromBinaryForm(byte[] binaryForm, int offset, byte revision, int aceCount, int length)
			{
				
				RawAcl rawAcl = null;
				byte [] verifierBinaryForm = null;

				rawAcl = new RawAcl(binaryForm, offset);
				verifierBinaryForm = new byte[rawAcl.BinaryLength];
				rawAcl.GetBinaryForm(verifierBinaryForm, 0);
				if ((revision == rawAcl.Revision) && 
					Utils.UtilIsBinaryFormEqual(binaryForm, offset, verifierBinaryForm) &&
					(aceCount == rawAcl.Count) &&
					(length == rawAcl.BinaryLength))
				{
					return true;
				}
				else
					return false;
			}
		}

		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test RawAcl property
		*		public override int AceCount
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
			*			RawAcl property AceCount
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
				byte revision = 0;
				int capacity = 0;
				string sid = "BA";
				
				//case 1, empty RawAcl
				testCasesPerformed ++;

				try
				{
					revision = 0;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					if( 0 == rawAcl.Count)
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

				
				//case 2, RawAcl with one Ace
				testCasesPerformed ++;

				try
				{
					revision = 0;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);					
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
					rawAcl.InsertAce(0, gAce);
					if( 1 == rawAcl.Count)
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


				//case 3, RawAcl with two Aces
				testCasesPerformed ++;

				try
				{
					revision = 0;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);					
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
					rawAcl.InsertAce(0, gAce);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 2, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
					rawAcl.InsertAce(0, gAce);					
					if( 2 == rawAcl.Count)
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
				SecurityIdentifier sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA"));

				Console.WriteLine("Running AdditionalTestCases");
				

				
				//case 1, RawAcl with huge number of Aces
				testCasesPerformed ++;

				try
				{
					revision = 0;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					for(int i = 0; i < 1820; i ++)
					{
						//this ace binary length is 36, 1820 * 36 = 65520						
						gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, i + 1, sid, false, null);
						rawAcl.InsertAce(0, gAce);	
					}					
					if(  1820 == rawAcl.Count)
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
		*  Class to test RawAcl property
		*		public override int BinaryLength
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
			*			RawAcl property BinaryLength
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
				GenericAce gAce = null;				
				byte revision = 0;
				int capacity = 0;
				string sid = "BA";
				
				//case 1, empty RawAcl, binarylength should be 8
				testCasesPerformed ++;

				try
				{
					revision = 0;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					if( 8 == rawAcl.BinaryLength)
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


				//case 2, RawAcl with one Ace, binarylength should be 8 + the Ace's binarylength
				testCasesPerformed ++;

				try
				{
					revision = 0;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);					
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
					rawAcl.InsertAce(0, gAce);
					if( 8 + gAce.BinaryLength == rawAcl.BinaryLength)
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


				//case 3, RawAcl with two Aces
				testCasesPerformed ++;

				try
				{
					revision = 0;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);					
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
					rawAcl.InsertAce(0, gAce);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 2, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
					rawAcl.InsertAce(0, gAce);					
					if( 8 + rawAcl[0].BinaryLength + rawAcl[1].BinaryLength == rawAcl.BinaryLength)
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
				int expectedLength = 0;

				Console.WriteLine("Running AdditionalTestCases");
				

				
				//case 1, RawAcl with huge number of Aces
				testCasesPerformed ++;

				try
				{
					revision = 0;
					capacity = 1;
					rawAcl = new RawAcl(revision, capacity);
					expectedLength = 8;
					for(int i = 0; i < 1820; i ++)
					{	//this ace binary length is 36, 1820 * 36 = 65520		
						gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, i + 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
						rawAcl.InsertAce(0, gAce);
						expectedLength += gAce.BinaryLength;
					}
					if(  expectedLength == rawAcl.BinaryLength)
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
		*  Class to test RawAcl index
		*		public override GenericAce this[int index]
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
			*			RawAcl index
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
				GenericAce genericAce = null;
				GenericAce verifierGenericAce = null;
				string owner1 = "SY";	
				string owner2 = "BA";
				string owner3 = "BG";
				string owner4 = "BO";
				int index = 0;
				int previousCount = 0;
				int previousLength = 0;


				// case 1, only one ACE, get at index 0
				testCasesPerformed ++;

				
				try
				{				
					rawAcl = new RawAcl(1, 1);

					index = 0;					
					genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);
					rawAcl.InsertAce(0, genericAce);
					
					verifierGenericAce = rawAcl[index];
					if(genericAce == verifierGenericAce)
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;							
					}
					else
					{
						Console.WriteLine("the Ace is not equal to expected");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}

				
				//case 2, two ACEs, get at index Count -1
				testCasesPerformed ++;

				try
				{			
					rawAcl = new RawAcl(1, 2);
					genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);					
					rawAcl.InsertAce(0, genericAce);
					genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner2)), false, null);					
					rawAcl.InsertAce(1, genericAce);	
					
					index = rawAcl.Count - 1;					
					verifierGenericAce = rawAcl[index];
					if(genericAce == verifierGenericAce )
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;							
					}
					else
					{
						Console.WriteLine("the Ace is not equal to expected");
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

					genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);
					rawAcl.InsertAce(0, genericAce);
					genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner2)), false, null);
					rawAcl.InsertAce(1, genericAce);
					rawAcl.InsertAce(2,  new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner3)), false, null));

					index = rawAcl.Count/2;	
					verifierGenericAce = rawAcl[index];
					if(genericAce == verifierGenericAce)
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;							
					}
					else
					{
						Console.WriteLine("the Ace is not equal to expected");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}

				
				// case 4, only one ACE, set at index 0
				testCasesPerformed ++;

				
				try
				{				
					rawAcl = new RawAcl(1, 1);

					index = 0;					
					genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);
					rawAcl.InsertAce(0, genericAce);
					previousCount = rawAcl.Count;
					previousLength = rawAcl.BinaryLength - genericAce.BinaryLength;
					
					genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessDenied, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner2)), false, null);
					rawAcl[index] = genericAce;
					verifierGenericAce = rawAcl[index];
					if((genericAce == verifierGenericAce) && (previousCount == rawAcl.Count) && (previousLength + genericAce.BinaryLength == rawAcl.BinaryLength))
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;							
					}
					else
					{
						Console.WriteLine("the newly set CommonAce is not equal to what we set, or the count changed, or the binary length is not correct");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}

				
				//case 5, two ACEs, set at index Count -1
				testCasesPerformed ++;

				try
				{			
					rawAcl = new RawAcl(1, 2);
					genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);					
					rawAcl.InsertAce(0, genericAce);
					genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner2)), false, null);					
					rawAcl.InsertAce(1, genericAce);	
					previousCount = rawAcl.Count;
					previousLength = rawAcl.BinaryLength - genericAce.BinaryLength;
					
					genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessDenied, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner3)), false, null);
					index = rawAcl.Count - 1;					
					rawAcl[index] = genericAce;
					verifierGenericAce = rawAcl[index];
					if((genericAce == verifierGenericAce) && (previousCount == rawAcl.Count) && (previousLength + genericAce.BinaryLength == rawAcl.BinaryLength))
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;							
					}
					else
					{
						Console.WriteLine("the newly set CommonAce is not equal to what we set, or the count changed, or the binary length is not correct");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}

				
				//case 6, only three ACEs, index at Count/2
				testCasesPerformed ++;

				try
				{
					rawAcl = new RawAcl(1, 3);

					genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);
					rawAcl.InsertAce(0, genericAce);
					genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner2)), false, null);
					rawAcl.InsertAce(1, genericAce);
					rawAcl.InsertAce(2, new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner3)), false, null));

					previousCount = rawAcl.Count;
					previousLength = rawAcl.BinaryLength - genericAce.BinaryLength;
					
					index = rawAcl.Count/2;	
					genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessDenied, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner4)), false, null);
					rawAcl[index] = genericAce;
					verifierGenericAce = rawAcl[index];
					if((genericAce == verifierGenericAce) && (previousCount == rawAcl.Count) && (previousLength + genericAce.BinaryLength == rawAcl.BinaryLength))
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;							
					}
					else
					{
						Console.WriteLine("the newly set CommonAce is not equal to what we set, or the count changed, or the binary length is not correct");
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
				GenericAce genericAce = null;
				GenericAce verifierGenericAce = null;
				string owner = null;
				int index = 0;

				Console.WriteLine("Running AdditionalTestCases");
				

				
				// case 1, no ACE, get index at -1
				testCasesPerformed ++;

				try
				{			
					rawAcl = new RawAcl(1, 1);
					index = -1;					
					verifierGenericAce = rawAcl[index];
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

				
				//case 2, get index at Count
				testCasesPerformed ++;

				try
				{					
					rawAcl = new RawAcl(1, 1);
					index = rawAcl.Count;					
					verifierGenericAce = rawAcl[index];
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

				
				//case 3, set index at -1
				testCasesPerformed ++;

				try
				{
					rawAcl = new RawAcl(1, 1);
					index = -1;					
					owner = "BA";				
					genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);					
					rawAcl[index] = genericAce;
					Console.WriteLine("Should not allow set index as -1");
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


				//case 4, set index at Count
				testCasesPerformed ++;

				try
				{
					rawAcl = new RawAcl(1, 1);
					index = rawAcl.Count;					
					owner = "BA";				
					genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);					
					rawAcl[index] = genericAce;
					Console.WriteLine("Should not allow set index as Count");
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


				//case 5, set null Ace
				testCasesPerformed ++;

				try
				{
					rawAcl = new RawAcl(1, 1);
					index = 0;
					genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
					rawAcl.InsertAce(0, genericAce);
					genericAce = null;
					rawAcl[index] = genericAce;
					Console.WriteLine("Should not allow set null ACE");
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


				//case 6, set Ace causing binarylength overflow
				testCasesPerformed ++;

				try
				{
					byte []opaque = new byte[GenericAcl.MaxBinaryLength + 1 - 8 - 4];				
					rawAcl = new RawAcl(1, 1);
					index = 0;
					genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
					rawAcl.InsertAce(0, genericAce);
					genericAce = new CustomAce( AceType.MaxDefinedAceType + 1, (AceFlags)223, opaque);
					rawAcl[index] = genericAce;
					Console.WriteLine("Should not allow set ACE to beyard GenericAcl.MaxBinaryLength");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
				}
				catch(OverflowException )					
				{//this is expected since the racl is overflow now
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;						
				}		
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}

			}
		}

		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test RawAcl method
		*		public void InsertAce( int index, GenericAce ace )
		*
		*
		*/
		//----------------------------------------------------------------------------------------------


		private class InsertAceTestCases
		{

			/*
			* Constructor
			*
			*/
			private InsertAceTestCases(){}

			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			RawAcl method InsertAce
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/

			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running InsertAceTestCases");

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
				RawAcl rawAclVerifier = null;
				GenericAce ace = null;
				GenericAce aceVerifier = null;				
				int count = 0;	
				int index = 0;				

				byte revision = 0;
				int capacity = 1;
				int flags = 1;
				int qualifier = 0;
				int accessMask = 1;
				string sid = "BA";
				bool isCallback = false;
				int opaqueSize = 8;

				//test insert at 0
				testCasesPerformed ++;

				try
				{
					rawAcl = new RawAcl(revision, capacity);
					rawAclVerifier = new RawAcl(revision, capacity);
					ace = new CommonAce((AceFlags)flags, (AceQualifier)qualifier, accessMask, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), isCallback, new byte[opaqueSize]);
					index = 0;
					//save current count
					count = rawAcl.Count;
					rawAcl.InsertAce(index, ace);

					//verify the count number increase one
					if(rawAcl.Count != count + 1)
					{
						Console.WriteLine("ace count does not increase 1");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
					}
					else
					{
						//verify the inserted ace is equal to the originial ace
						aceVerifier = rawAcl[index];
						if(ace != aceVerifier)
						{
							Console.WriteLine("inserted ace is not the original ace");
							Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
						}
						else
						{
							//verify right side aces are equal
							if(!UtilIsAclPartialEqual(rawAcl, rawAclVerifier, index + 1, rawAcl.Count - 1, index, count - 1))
							{
								Console.WriteLine("right side aces are not equal");
								Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
							}
							else
							{
								Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
								testCasesPassed++;
								//insert the same ACE to rawAclVerifier for next test
								rawAclVerifier.InsertAce(index, ace);								
							}
						}
					}					
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				
				//test insert at Count
				testCasesPerformed ++;

				
				try
				{
					sid = "BA";
					ace = new CommonAce((AceFlags)flags, (AceQualifier)qualifier, accessMask, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), isCallback, new byte[opaqueSize]);
					count =  rawAcl.Count;
					index = count;
					rawAcl.InsertAce(index, ace);

					//verify the count number increase one
					if(rawAcl.Count != count + 1)
					{
						Console.WriteLine("ace count does not increase 1");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
					}
					else
					{
						//verify the inserted ace is equal to the originial ace
						aceVerifier = rawAcl[index];
						if(aceVerifier != ace)
						{
							Console.WriteLine("inserted ace is not the original ace");
							Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
						}
						else
						{
							//verify the left side aces are equal
							if(!UtilIsAclPartialEqual(rawAcl, rawAclVerifier, 0, index - 1, 0, index - 1) )
							{
								Console.WriteLine("left side aces are not equal");
								Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
							}
							else
							{
								
								Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
								testCasesPassed++;
								//insert the same ACe to rawAclVerifier for next test
								rawAclVerifier.InsertAce(index, ace);								
							}				
						}
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}



				//test insert at Count - 1
				testCasesPerformed ++;

				
				try
				{
					sid = "BG";
					ace = new CommonAce((AceFlags)flags, (AceQualifier)qualifier, accessMask, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), isCallback, new byte[opaqueSize]);
					count =  rawAcl.Count;
					index = count - 1;
					rawAcl.InsertAce(index, ace);

					//verify the count number increase one
					if(rawAcl.Count != count + 1)
					{
						Console.WriteLine("ace count does not increase 1");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
					}
					else
					{
						//verify the inserted ace is equal to the originial ace
						aceVerifier = rawAcl[index];
						if(aceVerifier != ace)
						{
							Console.WriteLine("inserted ace is not the original ace");
							Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
						}
						else
						{
							//verify the left and right side aces are equal
							if(!UtilIsAclPartialEqual(rawAcl, rawAclVerifier, 0, index - 1, 0, index - 1) ||!UtilIsAclPartialEqual(rawAcl, rawAclVerifier, index + 1, rawAcl.Count - 1, index, count - 1))
							{
								Console.WriteLine("left or right side aces are not equal");
								Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
							}
							else
							{
								Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
								testCasesPassed++;
								//insert the same ACe to rawAclVerifier for next test
								rawAclVerifier.InsertAce(index, ace);									
							}				
						}
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}



				//test insert at Count /2
				testCasesPerformed ++;

				
				try
				{
					sid = "BO";
					ace = new CommonAce((AceFlags)flags, (AceQualifier)qualifier, accessMask, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), isCallback, new byte[opaqueSize]);
					rawAcl.InsertAce(0, ace);
					rawAclVerifier.InsertAce(0, ace);						
					count =  rawAcl.Count;
					index = count/2;
					sid = "SO";
					ace = new CommonAce((AceFlags)flags, (AceQualifier)qualifier, accessMask, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid)), isCallback, new byte[opaqueSize]);					
					rawAcl.InsertAce(index, ace);

					//verify the count number increase one
					if(rawAcl.Count != count + 1)
					{
						Console.WriteLine("ace count does not increase 1");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
					}
					else
					{
						//verify the inserted ace is equal to the originial ace
						aceVerifier = rawAcl[index];
						if(aceVerifier != ace)
						{
							Console.WriteLine("inserted ace is not the original ace");
							Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
						}
						else
						{
							//verify the left and right side aces are equal
							if(!UtilIsAclPartialEqual(rawAcl, rawAclVerifier, 0, index - 1, 0, index - 1) ||!UtilIsAclPartialEqual(rawAcl, rawAclVerifier, index + 1, rawAcl.Count - 1, index, count - 1))
							{
								Console.WriteLine("left or right side aces are not equal");
								Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
							}
							else
							{
								Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
								testCasesPassed++;								
							}				
						}
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
				GenericAce genericAce = null;
				string owner = null;
				int index = 0;

				Console.WriteLine("Running AdditionalTestCases");
				

				
				// case 1, no ACE, insert at index -1
				testCasesPerformed ++;

				try
				{				
					rawAcl = new RawAcl(1, 1);
					index = -1;
					owner = "BA";
					genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);	
					rawAcl.InsertAce(index, genericAce);
					Console.WriteLine("Should not allow insert index -1");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);	
				}
				catch (ArgumentOutOfRangeException )
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;	
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}

				
				//case 2, no ACE, insert at  index Count + 1
				testCasesPerformed ++;

				try
				{				
					rawAcl = new RawAcl(1, 1);
					index = rawAcl.Count + 1;
					genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);	
					rawAcl.InsertAce(index, genericAce);
					Console.WriteLine("Should not allow insert index Count + 1");
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

				
				//case 3, one ACE, insert null ACE
				testCasesPerformed ++;

				try
				{
					rawAcl = new RawAcl(1, 1);
					index = 0;					
					owner = "BA";
					genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);					
					rawAcl.InsertAce(index, genericAce);	
					genericAce = null;
					rawAcl.InsertAce(index, genericAce);
					Console.WriteLine("Should not allow insert null ACE");
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


				//case 4, insert a big Ace to make RawAcl of length 64K + 1. RawAcl length = HeaderLength + all ACE's  length
				// = HeaderLength + (HeaderLength + OpaqueLength) * num_of_custom_ace
				// = 8 + ( 4 + OpaqueLength) * num_of_custom_ace

				testCasesPerformed ++;

				try
				{
					rawAcl = new RawAcl(1, 1);
					byte []opaque = new byte[GenericAcl.MaxBinaryLength + 1 - 8 - 4];
					GenericAce gAce = new CustomAce( AceType.MaxDefinedAceType + 1, (AceFlags)223, opaque);
					rawAcl.InsertAce(0, gAce);
					Console.WriteLine("Should not allow insert ACE to exceed Acl's MaxBinaryLength");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);					
				}
				catch(OverflowException )					
				{//this is expected since the racl is overflow now
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;						
				}				
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}

				
			}
		}

		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test RawAcl method
		*		public void RemoveAce( int index )
		*
		*
		*/
		//----------------------------------------------------------------------------------------------

		private class RemoveAceTestCases
		{
			/*
			* Constructor
			*
			*/
			private RemoveAceTestCases(){}

			/*
			* Method Name: AllTestCases
			*
			* Description:	call BasicValidationTestCases and AdditionalTestCases methods to run all test cases of 
			*			RawAcl method RemoveAce
			*
			* Parameter:	testCasesPerformed -- sum of all the test cases performed
			*			testCasePassed -- total number of test cases passed
			*
			* Return:		none
			*/

			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running RemoveAceTestCases");

				BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
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

			public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{

				Console.WriteLine("Running BasicValidationTestCases");
				

				
				string[] testCase = null;
				ITestCaseReader reader = new InfTestCaseStore();
				reader.OpenTestCaseStore(RemoveAceTestCaseStore); 

				while (null != (testCase = reader.ReadNextTestCase()))
				{
					//read the sddl to create RawSecurityDescriptor, from which the test RawAcl will be created
					string sddl;
					if(1 > testCase.Length)
						throw new ArgumentOutOfRangeException();
					else
						sddl = testCase[0];			
			
					testCasesPerformed ++;

					try
					{
						if (TestRemoveAce(sddl))
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

				reader.CloseTestCaseStore();
			}

			public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				// No test cases yet
			}

			/*
			* Method Name: TestRemoveAce
			*
			* Description:	create a RawSecurityDescriptor object from the sddl, fetch Dacl from the RawSecurity Descriptor,
			*			do different position remove and check test case passes or fails  
			*
			* Parameter:	sddl -- a SDDL to create the RawSecurityDescriptor object
			*
			* Return:		true if test pass, false otherwise
			*/

			private static bool TestRemoveAce(string sddl)
			{
				RawSecurityDescriptor rawSecurityDescriptor = null;
				RawAcl rawAcl = null;
				RawAcl rawAclVerifier = null;
				GenericAce ace = null;


				rawSecurityDescriptor = new RawSecurityDescriptor(sddl);
				rawAclVerifier = rawSecurityDescriptor.DiscretionaryAcl;			
				rawAcl = UtilCopyRawACL(rawAclVerifier);


				if (null != rawAcl && null != rawAclVerifier )
				{
					int index = 0;
					int count = 0;

					//save current count
					count = rawAcl.Count;


					//test remove at -1
					try
					{			
						Console.WriteLine("Begin remove at -1");
						index = -1;
						rawAcl.RemoveAce(index);
						Console.WriteLine("should not allow remove at -1");
						return false;
					}								
					catch (ArgumentOutOfRangeException )
					{
						//this is expected for -1, do nothing 
						Console.WriteLine("Remove at -1 pass");
					}

					//test remove at 0, only need to catch ArgumentOutOfRangeException if Count = 0

					Console.WriteLine("Begin remove at 0");		
					index = 0;
					try	
					{		
						//save a copy of the ace
						ace = rawAcl[index];
						rawAcl.RemoveAce(index);

						if(0 == count )
						{//should have thrown the exception
							Console.WriteLine("should not allow remove at 0");
							return false;
						}

						//verify the count number decrease one
						if(rawAcl.Count != count - 1)
						{
							Console.WriteLine("ace count does not decrease 1");
							return false;
						}

						//verify the rawAcl.BinaryLength is updated correctly
						if(rawAcl.BinaryLength != rawAclVerifier.BinaryLength - ace.BinaryLength)
						{
							Console.WriteLine("rawAcl binarylength is not correctly updated");
							return false;
						}
							

						//verify the removed ace is equal to the originial ace

						if(!Utils.UtilIsAceEqual(ace, rawAclVerifier[index]))
						{
							Console.WriteLine("removed ace is not the original ace");
							return false;
						}							

						//verify right side aces are equal
						if(!UtilIsAclPartialEqual(rawAcl, rawAclVerifier, index, rawAcl.Count - 1, index + 1, count - 1))
						{
							Console.WriteLine("right side aces are not equal");
							return false;
						}
						Console.WriteLine("Remove at 0 pass");

					}
					catch(ArgumentOutOfRangeException )
					{	
						if(0 == count)
						{
							//it is expected, do nothing
							Console.WriteLine("Remove at 0 pass");
							return true;
						}
						else
							return false;
					}

							
					//now insert that ace back
					rawAcl.InsertAce(index, ace);

					//test remove at Count/2, do not need to catch ArgumentOutOfRangeException


					index = count/2;
					//when count/2 = 0 it is reduandent
					if(0 != index)
					{
						Console.WriteLine("Begin remove at Count/2");
						//save a copy of the ace
						ace = rawAcl[index];							
						rawAcl.RemoveAce(index);

						//verify the count number decrease one
						if(rawAcl.Count != count - 1)
						{
							Console.WriteLine("ace count does not decrease 1");
							return false;
						}

						//verify the rawAcl.BinaryLength is updated correctly
						if(rawAcl.BinaryLength != rawAclVerifier.BinaryLength - ace.BinaryLength)
						{
							Console.WriteLine("rawAcl binarylength is not correctly updated");
							return false;
						}
							
						//verify the removed ace is equal to the originial ace

						if(!Utils.UtilIsAceEqual(ace, rawAclVerifier[index]))
						{
							Console.WriteLine("removed ace is not the original ace");
							return false;
						}	

						//verify the left and right side aces are equal
						if(!UtilIsAclPartialEqual(rawAcl, rawAclVerifier, 0, index - 1, 0, index - 1) ||!UtilIsAclPartialEqual(rawAcl, rawAclVerifier, index, rawAcl.Count - 1, index + 1, count - 1))
						{
							Console.WriteLine("left or right side aces are not equal");
							return false;
						}

						Console.WriteLine("Remove at Count/2 pass");
						//now insert that removed ace
						rawAcl.InsertAce(index, ace);							
					}


					//test remove at Count - 1, do not need to catch ArgumentOutOfRangeException

					index = count - 1;
					//when count -1 = -1, 0, or count/2, it is reduandent
					if(-1 != index && 0 != index && count/2 != index)
					{
						Console.WriteLine("Begin remove at Count - 1");	
						//save a copy of the ace
						ace = rawAcl[index];								
						rawAcl.RemoveAce(index);

						//verify the count number decrease one
						if(rawAcl.Count != count - 1)
						{
							Console.WriteLine("ace count does not decrease 1");
							return false;
						}

						//verify the rawAcl.BinaryLength is updated correctly
						if(rawAcl.BinaryLength != rawAclVerifier.BinaryLength - ace.BinaryLength)
						{
							Console.WriteLine("rawAcl binarylength is not correctly updated");
							return false;
						}
						
						//verify the removed ace is equal to the originial ace

						if(!Utils.UtilIsAceEqual(ace, rawAclVerifier[index]))
						{
							Console.WriteLine("removed ace is not the original ace");
							return false;
						}

						//verify the left and right side aces are equal
						if(!UtilIsAclPartialEqual(rawAcl, rawAclVerifier, 0, index - 1, 0, index - 1) ||!UtilIsAclPartialEqual(rawAcl, rawAclVerifier, index, rawAcl.Count - 1, index + 1, count - 1))
						{
							Console.WriteLine("left or right side aces are not equal");
							return false;
						}

						Console.WriteLine("Remove at Count - 1 pass");
						//now insert that inserted ace
							
						rawAcl.InsertAce(index, ace);							
					}


					//test remove at Count
					try
					{
						index = count;
						//when count  = 0, or count/2, it is reduandent
						if(0 != index && count/2 != index)
						{
							Console.WriteLine("Begin remove at Count");							
							//save a copy of the ace
							ace = rawAcl[index];
							rawAcl.RemoveAce(index);
							Console.WriteLine("should not allow remove at Count");
							return false;								
						}
					}
					catch(ArgumentOutOfRangeException )
					{
						//this is expected for Count, do nothing 
						Console.WriteLine("Remove at Count  pass");								
					}

					//pass all the test
					return true;
				}
				else 
				{
					Console.WriteLine("RawAcl generated: NULL or Ace generated: NULL");
					return false;
				}
			}
		}

		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test RawAcl method inherited from base class GenericAcl
		*		void ICollection.CopyTo( Array array, int index )
		*
		*
		*/
		//----------------------------------------------------------------------------------------------	

		private class ExplicitInterfaceCopyToTestCases
		{
			//No creating objects
			private ExplicitInterfaceCopyToTestCases(){}

			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{

				Console.WriteLine("Running ExplicitInterfaceCopyToTestCases");
				
				BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
				AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
			}

			public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running BasicValidationTestCases");
				

				
				ICollection myCollection = null;
				GenericAce gAce = null;
				RawAcl rAcl = null;
				GenericAce [] gAces = null;
		
				//Case 1, when collection is actually empty
				testCasesPerformed ++;

				
				try
				{
					rAcl = new RawAcl(1, 1);
					gAces = new GenericAce[rAcl.Count];
					myCollection = (ICollection) rAcl;
					myCollection.CopyTo(gAces, 0);

					if(UtilTestAceCollectionCopy(rAcl, gAces, 0))
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

				
				//Case 2, collection has one ACE
				testCasesPerformed ++;

				
				try
				{	
					rAcl = new RawAcl(0, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);
					gAces = new GenericAce[rAcl.Count];
					myCollection = (ICollection)rAcl;					
					myCollection.CopyTo(gAces, 0);

					if(UtilTestAceCollectionCopy(rAcl, gAces, 0))
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


				//Case 3, index = 3
				testCasesPerformed ++;

				
				try
				{	
					rAcl = new RawAcl(0, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
					rAcl.InsertAce(0, gAce);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BO")), false, null);
					rAcl.InsertAce(0, gAce);					
					gAces = new GenericAce[rAcl.Count + 5];
					//initialize to null
					for(int i=0; i< gAces.Length; i++)
						gAces [i]= null;
					myCollection = (ICollection)rAcl;					
					myCollection.CopyTo(gAces, 3);

					if(UtilTestAceCollectionCopy(rAcl, gAces, 3))
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
				ICollection myCollection = null;
				GenericAce gAce = null;
				RawAcl rAcl = null;
				GenericAce [] gAces = null;

				Console.WriteLine("Running AdditionalTestCases");
				

				
				// Case 1, null array
				testCasesPerformed ++;

				
				try
				{	
					rAcl = new RawAcl(0, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);
					myCollection = (ICollection)rAcl;					
					myCollection.CopyTo(gAces, 0);
					Console.WriteLine("Should not allow copy to null array");
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


				// Case 2, negative index
				testCasesPerformed ++;

				
				try
				{	
					rAcl = new RawAcl(0, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);
					gAces = new GenericAce[rAcl.Count];
					myCollection = (ICollection)rAcl;					
					myCollection.CopyTo(gAces, -1);
					Console.WriteLine("Should not allow copy to negative index");
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

				

				// Case 3, 0 size array
				testCasesPerformed ++;

				
				try
				{	
					rAcl = new RawAcl(0, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);
					gAces = new GenericAce[0];
					myCollection = (ICollection)rAcl;					
					myCollection.CopyTo(gAces, 0);
					Console.WriteLine("Should not allow copy to 0 size array");
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


				// Case 4, insufficient size array
				testCasesPerformed ++;

				
				try
				{	
					rAcl = new RawAcl(0, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
					rAcl.InsertAce(0, gAce);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BO")), false, null);
					rAcl.InsertAce(0, gAce);
					
					gAces = new GenericAce[rAcl.Count - 1];
					myCollection = (ICollection)rAcl;					
					myCollection.CopyTo(gAces, 0);
					Console.WriteLine("Should not allow copy to insufficient size array");
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

				
				//Case 5, RawAcl with huge number of Aces
			
				testCasesPerformed ++;

				
				try
				{
					rAcl = new RawAcl(0, GenericAcl.MaxBinaryLength);	
					for(int i = 0; i < 1820; i ++)
					{
						//this ace binary length is 36, 1820 * 36 = 65520
						gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, i + 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
						rAcl.InsertAce(0, gAce);
					}
					gAces = new GenericAce[rAcl.Count];
					myCollection = (ICollection)rAcl;					
					myCollection.CopyTo(gAces, 0);
					if(UtilTestAceCollectionCopy(rAcl, gAces, 0))
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

			

				// Case 6, test ICollection.CopyTo, array rank is not one. all the other cases are tested by type-friendly version CopyTo
				//on my machine, a BCL assert as resource Rank_MutiDimNotSupported not found

				testCasesPerformed ++;

				
				try
				{	
					rAcl = new RawAcl(0, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
					rAcl.InsertAce(0, gAce);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);					
					GenericAce [,]gAces2 = new GenericAce[1, 2];
					myCollection = (ICollection)rAcl;					
					myCollection.CopyTo(gAces2, 0);
					Console.WriteLine("Should not allow array rank not equal to 1");
					Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
				}
				catch(RankException)
				{
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


			}
			
		}


		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test RawAcl method inherited from base class GenericAcl
		*		public void CopyTo( GenericAce[] array, int index ) 
		*
		*
		*/
		//----------------------------------------------------------------------------------------------	
		private class CopyToTestCases
		{
			//No creating objects
			private CopyToTestCases(){}

			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running CopyToTestCases");
				
				BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
				AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
			}

			public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running BasicValidationTestCases");
				

				
				GenericAce gAce = null;
				RawAcl rAcl = null;
				GenericAce [] gAces = null;
		
				// Case 1, when collection is actually empty
				testCasesPerformed ++;

		
				try
				{
					rAcl = new RawAcl(1, 1);
					gAces = new GenericAce[rAcl.Count];
					rAcl.CopyTo(gAces, 0);
					if(UtilTestAceCollectionCopy(rAcl, gAces, 0))
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


				// Case 2, collection has one ACE
				testCasesPerformed ++;

						
				try
				{	
					rAcl = new RawAcl(0, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);
					gAces = new GenericAce[rAcl.Count];
					rAcl.CopyTo(gAces, 0);

					if(UtilTestAceCollectionCopy(rAcl, gAces, 0))
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


				//Case 3, index = 3
				testCasesPerformed ++;

				
				try
				{	
					rAcl = new RawAcl(0, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
					rAcl.InsertAce(0, gAce);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BO")), false, null);
					rAcl.InsertAce(0, gAce);					
					gAces = new GenericAce[rAcl.Count + 5];
					//initialize to null
					for(int i=0; i< gAces.Length; i++)
						gAces[i] = null;
					rAcl.CopyTo(gAces, 3);

					if(UtilTestAceCollectionCopy(rAcl, gAces, 3))
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
				GenericAce gAce = null;
				RawAcl rAcl = null;
				GenericAce [] gAces = null;

				Console.WriteLine("Running AdditionalTestCases");
				

				
				// case 1, null array
				testCasesPerformed ++;

				
				try
				{	
					rAcl = new RawAcl(0, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);
					rAcl.CopyTo(gAces, 0);
					Console.WriteLine("Should not allow null array");
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


				// case 2, negative index
				testCasesPerformed ++;

				
				try
				{	
					rAcl = new RawAcl(0, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);
					gAces = new GenericAce[rAcl.Count];
					rAcl.CopyTo(gAces, -1);
					Console.WriteLine("Should not allow negative index");
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

				
				// case 3, insufficient size array
				testCasesPerformed ++;

				
				try
				{	
					rAcl = new RawAcl(0, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);
					gAces = new GenericAce[0];
					rAcl.CopyTo(gAces, 0);
					Console.WriteLine("Should not allow insufficient size array");
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


				// Case 4, insufficient size array
				testCasesPerformed ++;

				
				try
				{	
					rAcl = new RawAcl(0, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
					rAcl.InsertAce(0, gAce);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BO")), false, null);
					rAcl.InsertAce(0, gAce);
					
					gAces = new GenericAce[rAcl.Count - 1];
					rAcl.CopyTo(gAces, 0);
					Console.WriteLine("Should not allow copy to insufficient size array");
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


				//case 5, RawAcl with huge number of Aces
				testCasesPerformed ++;

				try
				{
					rAcl = new RawAcl(0, GenericAcl.MaxBinaryLength);	
					for(int i = 0; i < 1820; i ++)
					{
						//this ace binary length is 36, 1820 * 36 = 65520						
						gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, i + 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
						rAcl.InsertAce(0, gAce);
					}
					gAces = new GenericAce[rAcl.Count];
					rAcl.CopyTo(gAces, 0);
					if(UtilTestAceCollectionCopy(rAcl, gAces, 0))
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
		*  Class to test RawAcl property inherited from base class GenericAcl
		*		public bool IsSynchronized
		*
		*
		*/
		//----------------------------------------------------------------------------------------------	
		private class IsSynchronizedTestCases
		{
			//No creating objects
			private IsSynchronizedTestCases(){}

			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running IsSynchronizedTestCases");
				
				BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
			}

			public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running BasicValidationTestCases");
				

				
				GenericAce gAce = null;
				RawAcl rAcl = null;
		
				// collection has one ACE. By code review, this properties always return false. So no addtional cases are needed
				testCasesPerformed ++;

			
				try
				{	
					rAcl = new RawAcl(0, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);

					if( false == rAcl.IsSynchronized)
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

				
			}
		}		


		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test RawAcl property inherited from base class GenericAcl
		*		public object SyncRoot
		*
		*
		*/
		//----------------------------------------------------------------------------------------------	
		private class SyncRootTestCases
		{
			
			//No creating objects
			private SyncRootTestCases(){}

			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{

				Console.WriteLine("Running SyncRootTestCases");
				
				BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
			}

			public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running BasicValidationTestCases");
				

				
				GenericAce gAce = null;
				RawAcl rAcl = null;
		
				// collection has one ACE. By code review, this properties always return false. So no addtional cases are needed
				testCasesPerformed ++;

				
				try
				{	
					rAcl = new RawAcl(0, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);

					if( null != rAcl.SyncRoot)
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

				
			}
		}		


		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test RawAcl method inherited from base class GenericAcl
		*		IEnumerator IEnumerable.GetEnumerator()
		*
		*
		*/
		//----------------------------------------------------------------------------------------------	

		private class ExplicitInterfaceGetEnumeratorTestCases
		{
			
			//No creating objects
			private ExplicitInterfaceGetEnumeratorTestCases(){}

			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running ExplicitInterfaceGetEnumeratorTestCases");
				
				BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
				AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
			}

			public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running BasicValidationTestCases");
				

				
				IEnumerable myEnumerable = null;
				GenericAce gAce = null;
				RawAcl rAcl = null;
				IEnumerator myEnumerator = null;
		
				//Case 1, when collection is actually empty
				testCasesPerformed ++;

				
				try
				{
					rAcl = new RawAcl(1, 1);
					myEnumerable = (IEnumerable)rAcl;
					myEnumerator = myEnumerable.GetEnumerator();
					
					if(UtilTestGetEnumerator(myEnumerator, rAcl, true))
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

				
				// Case 2, collection has one ACE
				testCasesPerformed ++;

				
				try
				{	
					rAcl = new RawAcl(0, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);
					myEnumerable= (IEnumerable)rAcl;
					myEnumerator = myEnumerable.GetEnumerator();
					
					if(UtilTestGetEnumerator(myEnumerator, rAcl, true))
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
				IEnumerable myEnumerable = null;
				GenericAce gAce = null;
				RawAcl rAcl = null;
				IEnumerator myEnumerator = null;

				Console.WriteLine("Running AdditionalTestCases");
				


				//Case 1, RawAcl with huge number of Aces
				testCasesPerformed ++;

				
				try
				{
					rAcl = new RawAcl(0, GenericAcl.MaxBinaryLength + 1);
					for(int i = 0; i < 1820; i ++)
					{
						//this ace binary length is 36, 1820 * 36 = 65520						
						gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, i + 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
						rAcl.InsertAce(0, gAce);	
					}
					myEnumerable= (IEnumerable)rAcl;
					myEnumerator = myEnumerable.GetEnumerator();

					if(UtilTestGetEnumerator(myEnumerator, rAcl, true))
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
		*  Class to test RawAcl method inherited from base class GenericAcl
		*		public AceEnumerator GetEnumerator()
		*
		*
		*/
		//----------------------------------------------------------------------------------------------	

		private class GetEnumeratorTestCases
		{
			//No creating objects
			private GetEnumeratorTestCases(){}

			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{

				Console.WriteLine("Running GetEnumeratorTestCases");
				
				BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
				AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
			}

			public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{

				Console.WriteLine("Running BasicValidationTestCases");
				

				
				GenericAce gAce = null;
				RawAcl rAcl = null;
				AceEnumerator aceEnumerator = null;
		
				// Case 1, when collection is actually empty
				testCasesPerformed ++;

				
				try
				{
					rAcl = new RawAcl(1, 1);
					aceEnumerator = rAcl.GetEnumerator();
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;					
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}

		
				
				//Case 2, collection has one ACE
				testCasesPerformed ++;

				
				try
				{	
					rAcl = new RawAcl(0, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);
					aceEnumerator = rAcl.GetEnumerator();
					if(UtilTestGetEnumerator(aceEnumerator, rAcl, false))
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
				GenericAce gAce = null;
				RawAcl rAcl = null;
				AceEnumerator aceEnumerator = null;

				Console.WriteLine("Running AdditionalTestCases");
				


				//Case 1, RawAcl with huge number of Aces
				
				testCasesPerformed ++;

				try
				{
					rAcl = new RawAcl(0, GenericAcl.MaxBinaryLength + 1);
					for(int i = 0; i < 1820; i ++)
					{
						//this ace binary length is 36, 1820 * 36 = 65520						
						gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, i + 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
						rAcl.InsertAce(0, gAce);	
					}					
					aceEnumerator = rAcl.GetEnumerator();
					if(UtilTestGetEnumerator(aceEnumerator, rAcl, false))
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
		*  Class to test RawAcl method
		*		public override void GetBinaryForm( byte[] binaryForm, int offset )
		*		Note: this method might need call native Win32API for validation
		*
		*
		*/
		//----------------------------------------------------------------------------------------------


		private class GetBinaryFormTestCases
		{
			//No creating objects
			private GetBinaryFormTestCases(){}

			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running GetBinaryFormTestCases");
				BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
				AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
			}

			public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running BasicValidationTestCases");
				


				RawAcl rAcl = null;
				GenericAce gAce = null;
				byte [] binaryForm = null;				
				
				//Case 1, a RawAcl with one AccessAllowed Ace
				testCasesPerformed ++;


				try
				{
					SecurityIdentifier sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA"));
					byte [] verifierBinaryForm = null;
					byte [] sidBinaryForm = new byte [sid.BinaryLength];
					sid.GetBinaryForm(sidBinaryForm, 0);

					rAcl = new RawAcl(GenericAcl.AclRevision, 1);				
					gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, sid, false, null);
					rAcl.InsertAce(0, gAce);
					binaryForm = new byte[rAcl.BinaryLength];
					verifierBinaryForm = new byte[rAcl.BinaryLength];
					rAcl.GetBinaryForm( binaryForm, 0);
					
					int errorCode;
					if( 0 ==Win32AclLayer.InitializeAclNative
						(verifierBinaryForm, (uint) rAcl.BinaryLength, (uint)GenericAcl.AclRevision))
					{
						errorCode = Marshal.GetLastWin32Error();						
						Console.WriteLine("P/Invoke to initialize the native Acl failed, errorCode {0}", errorCode);
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					}
					else if ( 0 == Win32AclLayer.AddAccessAllowedAceExNative
						(verifierBinaryForm, (uint) GenericAcl.AclRevision, (uint) AceFlags.None, (uint) 1, sidBinaryForm))
					{
						errorCode = Marshal.GetLastWin32Error();
						Console.WriteLine("P/Invoke to add the ace to the native Acl failed, errorCode {0}", errorCode);
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					}												
					else if(Utils.UtilIsBinaryFormEqual(binaryForm, verifierBinaryForm))
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;
					}
					else
					{
						Console.WriteLine("the binaryFrom does not match the one acquired by P/Invoke");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);							
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				//Case 2, a RawAcl with one AccessDenied Ace
				testCasesPerformed ++;


				try
				{
					SecurityIdentifier sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA"));
					byte [] verifierBinaryForm = null;
					byte [] sidBinaryForm = new byte [sid.BinaryLength];
					sid.GetBinaryForm(sidBinaryForm, 0);

					rAcl = new RawAcl(GenericAcl.AclRevision, 1);
					//31 include ObjectInherit, ContainerInherit, NoPropagateInherit, InheritOnly and Inherited
					gAce = new CommonAce((AceFlags)31, AceQualifier.AccessDenied, 1, sid, false, null);
					rAcl.InsertAce(0, gAce);
					binaryForm = new byte[rAcl.BinaryLength];
					verifierBinaryForm = new byte[rAcl.BinaryLength];
					rAcl.GetBinaryForm( binaryForm, 0);
					
					int errorCode;
					if( 0 ==Win32AclLayer.InitializeAclNative
						(verifierBinaryForm, (uint) rAcl.BinaryLength, (uint)GenericAcl.AclRevision))
					{
						errorCode = Marshal.GetLastWin32Error();						
						Console.WriteLine("P/Invoke to initialize the native Acl failed, errorCode {0}", errorCode);
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					}
					else if ( 0 == Win32AclLayer.AddAccessDeniedAceExNative
						(verifierBinaryForm, (uint) GenericAcl.AclRevision, (uint)31, (uint) 1, sidBinaryForm))
					{
						errorCode = Marshal.GetLastWin32Error();
						Console.WriteLine("P/Invoke to add the ace to the native Acl failed, errorCode {0}", errorCode);
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					}												
					else if(Utils.UtilIsBinaryFormEqual(binaryForm, verifierBinaryForm))
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;					
					}
					else
					{
						Console.WriteLine("the binaryFrom does not match the one acquired by P/Invoke");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);							
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				//Case 3, a RawAcl with one Audit Ace
				testCasesPerformed ++;


				try
				{
					SecurityIdentifier sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA"));
					byte [] verifierBinaryForm = null;
					byte [] sidBinaryForm = new byte [sid.BinaryLength];
					sid.GetBinaryForm(sidBinaryForm, 0);

					rAcl = new RawAcl(GenericAcl.AclRevision, 1);
					//223 include ObjectInherit, ContainerInherit, NoPropagateInherit, InheritOnly, Inherited, SuccessfulAccess and FailedAccess
					gAce = new CommonAce((AceFlags)223, AceQualifier.SystemAudit, 1, sid, false, null);
					rAcl.InsertAce(0, gAce);
					binaryForm = new byte[rAcl.BinaryLength];
					verifierBinaryForm = new byte[rAcl.BinaryLength];
					rAcl.GetBinaryForm( binaryForm, 0);
					
					int errorCode;
					if( 0 ==Win32AclLayer.InitializeAclNative
						(verifierBinaryForm, (uint) rAcl.BinaryLength, (uint)GenericAcl.AclRevision))
					{
						errorCode = Marshal.GetLastWin32Error();						
						Console.WriteLine("P/Invoke to initialize the native Acl failed, errorCode {0}", errorCode);
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					}
					else if ( 0 == Win32AclLayer.AddAuditAccessAceExNative
						(verifierBinaryForm, (uint) GenericAcl.AclRevision, (uint)223, (uint) 1, sidBinaryForm, 1, 1))
					{
						errorCode = Marshal.GetLastWin32Error();
						Console.WriteLine("P/Invoke to add the ace to the native Acl failed, errorCode {0}", errorCode);
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					}												
					else if(Utils.UtilIsBinaryFormEqual(binaryForm, verifierBinaryForm))
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;						
					}
					else
					{
						Console.WriteLine("the binaryFrom does not match the one acquired by P/Invoke");
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
				RawAcl rAcl = null;
				GenericAce gAce = null;
				byte [] binaryForm = null;
				
				Console.WriteLine("Running AdditionalTestCases");
				


				//Case 1, array binaryForm is null
				testCasesPerformed ++;


				try
				{
					rAcl = new RawAcl(GenericAcl.AclRevision, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);
					try
					{
						rAcl.GetBinaryForm( binaryForm, 0);					
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
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);
					try
					{
						rAcl.GetBinaryForm( binaryForm, -1);					
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
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);
					try
					{
						rAcl.GetBinaryForm( binaryForm, binaryForm.Length);					
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


				//Case , offset is a big possitive number
				testCasesPerformed ++;


				try
				{
					rAcl = new RawAcl(GenericAcl.AclRevision, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);
					gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);					
					rAcl.InsertAce(0, gAce);
					binaryForm = new byte[rAcl.BinaryLength + 10000];					

					rAcl.GetBinaryForm( binaryForm, 10000);
					//recreate the RawAcl from BinaryForm
					rAcl = new RawAcl(binaryForm, 10000);
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


				//Case 4, binaryForm array's size is insufficient
				testCasesPerformed ++;


				try
				{
					binaryForm = new byte[4];
					rAcl = new RawAcl(GenericAcl.AclRevision, 1);
					gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
					rAcl.InsertAce(0, gAce);
					try
					{
						rAcl.GetBinaryForm( binaryForm, 0);					
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

				
				//Case 5, an empty RawAcl
				testCasesPerformed ++;


				try
				{
					binaryForm = new byte[16];
					byte [] verifierBinaryForm = new byte [16];
					for(int i = 0; i < binaryForm.Length; i++)
					{
						binaryForm[i] = (byte) 0;
						verifierBinaryForm[i] = (byte) 0;
					}
					rAcl = new RawAcl(GenericAcl.AclRevision, 1);

					rAcl.GetBinaryForm( binaryForm, 0);
					int errorCode;
					if( 0 ==Win32AclLayer.InitializeAclNative
						(verifierBinaryForm, (uint) 8, (uint)GenericAcl.AclRevision))
					{
						errorCode = Marshal.GetLastWin32Error();						
						Console.WriteLine("P/Invoke to get the corresponding binaryForm failed, errorCode {0}", errorCode);
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);						
					}
					else if(Utils.UtilIsBinaryFormEqual(binaryForm, verifierBinaryForm))
					{
						Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
						testCasesPassed++;							
					}
					else
					{
						Console.WriteLine("the binaryFrom does not match the one acquired by P/Invoke");
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);							
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}


				//Case 6, a RawAcl with huge number of AccessAllowed, AccessDenied, and AuditAce
				testCasesPerformed ++;


				try
				{
					SecurityIdentifier sid = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid("BA"));
					
					byte [] verifierBinaryForm = null;
					byte [] sidBinaryForm = new byte [sid.BinaryLength];
					sid.GetBinaryForm(sidBinaryForm, 0);

					rAcl = new RawAcl(GenericAcl.AclRevision, GenericAcl.MaxBinaryLength + 1);
					for(int i = 0; i < 780; i ++)
					{
						//three aces binary length together is 24 + 36 + 24 = 84, 780 * 84 = 65520
						gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, i, sid, false, null);
						rAcl.InsertAce(rAcl.Count, gAce);

						gAce = new CommonAce((AceFlags)223, AceQualifier.SystemAudit, i + 1, sid, false, null);
						rAcl.InsertAce(rAcl.Count, gAce);

						gAce = new CommonAce((AceFlags)31, AceQualifier.AccessDenied, i + 2, sid, false, null);
						rAcl.InsertAce(rAcl.Count, gAce);
				
					}					

					binaryForm = new byte[rAcl.BinaryLength];
					verifierBinaryForm = new byte[rAcl.BinaryLength];
					rAcl.GetBinaryForm( binaryForm, 0);
					
					int errorCode;
					if( 0 ==Win32AclLayer.InitializeAclNative
						(verifierBinaryForm, (uint) rAcl.BinaryLength, (uint)GenericAcl.AclRevision))
					{
						errorCode = Marshal.GetLastWin32Error();						
						Console.WriteLine("P/Invoke to initialize the native Acl failed, errorCode {0}", errorCode);
						Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);							
					}
					else
					{
						int i = 0;
						for(i = 0; i < 780; i++)
						{
							if ( 0 == Win32AclLayer.AddAccessAllowedAceExNative
							(verifierBinaryForm, (uint) GenericAcl.AclRevision, (uint) AceFlags.None, (uint) i, sidBinaryForm))
							{
								errorCode = Marshal.GetLastWin32Error();
								Console.WriteLine("P/Invoke to add the ace to the native Acl failed, errorCode {0}", errorCode);
								break;
							}
							if ( 0 == Win32AclLayer.AddAuditAccessAceExNative
							(verifierBinaryForm, (uint) GenericAcl.AclRevision, (uint)223, (uint) (i + 1), sidBinaryForm, 1, 1))
							{
								errorCode = Marshal.GetLastWin32Error();
								Console.WriteLine("P/Invoke to add the ace to the native Acl failed, errorCode {0}", errorCode);
								break;
							}										
							if ( 0 == Win32AclLayer.AddAccessDeniedAceExNative
							(verifierBinaryForm, (uint) GenericAcl.AclRevision, (uint)31, (uint) (i + 2), sidBinaryForm))
							{
								errorCode = Marshal.GetLastWin32Error();
								Console.WriteLine("P/Invoke to add the ace to the native Acl failed, errorCode {0}", errorCode);
								break;
							}									

						}
						if(i == 780)
						{//the loop finishes
							if(Utils.UtilIsBinaryFormEqual(binaryForm, verifierBinaryForm))
							{
								Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
								testCasesPassed++;
							}
							else
							{
								Console.WriteLine("the binaryFrom does not match the one acquired by P/Invoke");
								
								Console.WriteLine("Managed binaryForm:");
								Utils.UtilPrintBinaryForm(binaryForm);
								Console.WriteLine("UnManaged binaryForm:");
								Utils.UtilPrintBinaryForm(verifierBinaryForm);
								Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);							
							}
						}
						else
						{//the loop not finishes
							Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);							
						}
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}

				
			}

		}
		
        }
}


    
