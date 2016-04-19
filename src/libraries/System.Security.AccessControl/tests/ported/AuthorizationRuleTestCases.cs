//--------------------------------------------------------------------------
//
//		AuthorizationRule test cases
//
//		Tests the AuthorizationRule classes
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
	/// <summary>
	/// Does all the testing for the AuthorizationRule classes. 
	/// Note that the names and types of variables used here are those of the 
	/// Abstract AuthorizationRule class hierarchy but what we instantiate and use 
	/// are currently concrete subclasses from our test integrator layer. We try to 
	/// avoid using the concrete class names/types in our test variables to decouple
	/// the test from the test integrator layer being utilized as much as possible
	/// </summary>

	//----------------------------------------------------------------------------------------------
	/*
	*  Class to test AccessRule, AuditRule and their abstract base classes AuthorizationRule, addtionaly test AuthorizationRuleCollection
	*
	*
	*/
	//----------------------------------------------------------------------------------------------
		
	public class AuthorizationRuleTestCases
	{

		// This enum is used to control which type of IdentityReference will be used to test the constructor
		private enum IdentityReferenceType
		{
		 	SID = 0,
			NTACCOUNT = 1,
		}
		
		public static readonly string AccessRuleConstructorTestCaseStore = "TestCaseStores\\AuthorizationRule_AccessRuleConstructor.inf";
		public static readonly string AuditRuleConstructorTestCaseStore = "TestCaseStores\\AuthorizationRule_AuditRuleConstructor.inf";

		// No creating objects!
		private AuthorizationRuleTestCases(){}

        public static Boolean Test()
        {
            Console.WriteLine("\n\n=======STARTING AuthorizationRuleTestCases==========\n");
            return AllTestCases();
        }

		public static Boolean AllTestCases()
		{
	
			Console.WriteLine("Running AuthorizationRule.AllTestCases()");

            int testCasesPerformed = 0;
            int testCasesPassed = 0;


			AccessRuleConstructorTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);
			

			AuditRuleConstructorTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


			AuthorizationRuleCollectionCopyToTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);


			AuthorizationRuleCollectionIndexerTestCases.AllTestCases(ref testCasesPerformed, ref testCasesPassed);

            return (testCasesPerformed == testCasesPassed);

		}

		public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
		{
			Console.WriteLine("Running AccessRuleConstructorTestCases");	
			AccessRuleConstructorTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);
			
			Console.WriteLine("Running AuditRuleConstructorTestCases");
			AuditRuleConstructorTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, true);

			Console.WriteLine("Running AuthorizationRuleCollectionCopyToTestCases");
			AuthorizationRuleCollectionCopyToTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);		

			Console.WriteLine("Running AuthorizationRuleCollectionIndexerTestCases");
			AuthorizationRuleCollectionIndexerTestCases.BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);			
		}

		public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
		{
			// No test cases yet
		}

		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test AccessRule constructor
		*		protected AccessRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type )
		*
		*
		*/
		//----------------------------------------------------------------------------------------------
		
		private class AccessRuleConstructorTestCases
		{
			//No creating objects
			private AccessRuleConstructorTestCases(){}

			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running AccessRuleConstructorTestCases");			
				BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, false);
				AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
			}

			public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed, bool isBVT)
			{
				Console.WriteLine("Running BasicValidationTestCases");
				

				int bvtCaseCounter = 0;
				
				string[] testCase = null;
				ITestCaseReader reader = new InfTestCaseStore();
				reader.OpenTestCaseStore(AccessRuleConstructorTestCaseStore); 
				while (null != (testCase = reader.ReadNextTestCase()))
				{
					// read identityReferenceType
					int identityReferenceType;
					if(1 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						identityReferenceType = int.Parse(testCase[0]);
					
					// read sid
					string sid;
					if (2 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[1])
						sid = null;
					else
						sid = testCase[1];

					// read accessMask
					int accessMask;
					if (3 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						accessMask = int.Parse(testCase[2]);

					//read isInherited
					bool isInherited;
					if(4 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isInherited = bool.Parse(testCase[3]);

					// read inheritanceFlags
					int inheritanceFlags;
					if (5 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						inheritanceFlags = int.Parse(testCase[4]);

					// read propagationFlags
					int propagationFlags;
					if (6 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						propagationFlags = int.Parse(testCase[5]);
					
					// read accessControlType
					int accessControlType;
					if (7 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						accessControlType = int.Parse(testCase[6]);
					
					testCasesPerformed++;

					try
					{
						if (TestAccessRuleConstructor(identityReferenceType, sid, accessMask, isInherited, inheritanceFlags, propagationFlags, accessControlType))
						{
							Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
							testCasesPassed++;
						}
						else
							Console.WriteLine("Test case {0} FAILED!", testCasesPerformed);
					}
					catch(Exception e)
					{
						Console.WriteLine("Exception generated: "+ e.Message, e);
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

			//private static bool TestAccessRuleConstructor(SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType accessControlType)
			private static bool TestAccessRuleConstructor(int identityReferenceType, string sid, int accessMask, bool isInherited, int inheritanceFlags, int propagationFlags, int accessControlType)
			{

				FileSystemAccessRule accessRule = null;
				IdentityReference  identityReference = null;

				if (IdentityReferenceType.SID == (IdentityReferenceType)identityReferenceType)
				{
					if(null == sid)
						identityReference = null;
					else
						identityReference = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid));
				}
				else if(IdentityReferenceType.NTACCOUNT == (IdentityReferenceType) identityReferenceType)
				{
					identityReference= (new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid))).Translate(typeof(NTAccount));
				}
				else
				{
					Console.WriteLine("Invalid identityReference type, should be 0 - SecurityIdentifier or 1 - NTAccount");
					return false;
				}
				try
				{
					Console.WriteLine("IdentifyReference: {0}, accessMask: {1}, isInherited: {2}, inheritanceFlags: {3}, propogationFlags: {4}, accessControlType: {5}",
						identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, accessControlType);
					accessRule = new FileSystemAccessRule(identityReference, accessMask, isInherited, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags, (AccessControlType)accessControlType);
					Console.WriteLine("FileSystemAccessRule object is created successfully");
					
					if(null == identityReference )
					{
						Console.WriteLine("Should not allow null identity");
						return false;
					}
					if( identityReference.IsValidTargetType( typeof( System.Security.Principal.SecurityIdentifier )) == false )
					{
						Console.WriteLine("Should not allow non-translatable into SecurityIdentifier identity");
						return false;
					}
					if ( 0 == accessMask )		
					{
						Console.WriteLine("Should not allow accessMask 0");
						return false;
					}
					if((InheritanceFlags) inheritanceFlags < InheritanceFlags.None || (InheritanceFlags)inheritanceFlags > (InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit))
					{
						Console.WriteLine("Should not allow inheritanceFlags less than {0} or bigger than ", InheritanceFlags.None, (InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit));
						return false;						
					}

					if((PropagationFlags)propagationFlags < PropagationFlags.None || (PropagationFlags)propagationFlags > (PropagationFlags.NoPropagateInherit | PropagationFlags.InheritOnly))
					{
						Console.WriteLine("Should not allow propagationFlags less than {0} or bigger than ", PropagationFlags.None , (PropagationFlags.NoPropagateInherit | PropagationFlags.InheritOnly));
						return false;							
					}

					
					if ( (AccessControlType)accessControlType != AccessControlType.Allow && (AccessControlType)accessControlType != AccessControlType.Deny )
					{
						Console.WriteLine("Should not allow accessControlType beyard Allow and Deny");
						return false;
					}
			
					//object creation succeeds, use properties to check if the object is created correctly
					//properties checked: IdentityReference, AccessMask, IsInherited, InheritanceFlags, PropagationFlags, AccessControlType
					if( !identityReference.Equals(accessRule.IdentityReference))
					{
						Console.WriteLine("the Identityreference is not equal to the one passed to constructor");
						return false;
					}
					if(accessRule.AccessMask != accessMask)
					{
						Console.WriteLine("the AccessMask is not equal to the one passed to constructor");
						return false;
					}	
					if(accessRule.IsInherited != isInherited)
					{
						Console.WriteLine("the IsInherited is not equal to the one passed to constructor");
						return false;
					}						
					if(accessRule.InheritanceFlags != (InheritanceFlags)inheritanceFlags)
					{
						Console.WriteLine("the InheritanceFlags is not equal to the one passed to constructor");
						return false;
					}
					if(accessRule.PropagationFlags != (PropagationFlags)propagationFlags)
					{
						if((accessRule.InheritanceFlags != InheritanceFlags.None) || (accessRule.PropagationFlags != PropagationFlags.None))
						{
							Console.WriteLine("the PropagationFlags is not equal to the one passed to constructor");
							return false;
						}
					}
					if(accessRule.AccessControlType != (AccessControlType)accessControlType)
					{
						Console.WriteLine("the AccessControlType is not equal to the one passed to constructor");
						return false;
					}
					//all verifications pass
					return true;
				}
				catch (ArgumentNullException nullEx)
				{
					if(null == identityReference )
					{//this is expected
						return true;
					}
					else
					{
						Console.WriteLine("Exception generated: " + nullEx.Message, nullEx);
						return false;
					}
				}
				catch (ArgumentOutOfRangeException outOfRangeEx)
				{
					if (( (AccessControlType)accessControlType != AccessControlType.Allow && (AccessControlType)accessControlType != AccessControlType.Deny ) ||
						((InheritanceFlags) inheritanceFlags < InheritanceFlags.None || (InheritanceFlags)inheritanceFlags > (InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit))	||
						((PropagationFlags)propagationFlags < PropagationFlags.None || (PropagationFlags)propagationFlags > (PropagationFlags.NoPropagateInherit | PropagationFlags.InheritOnly))	)
					{//this is expected
						return true;
					}
					else
					{
						Console.WriteLine("Exception generated: " + outOfRangeEx.Message, outOfRangeEx);
						return false;
					}			
				}
				catch (ArgumentException argEx)
				{
					if( identityReference.IsValidTargetType( typeof( System.Security.Principal.SecurityIdentifier )) == false || ( 0 == accessMask ) )
					{//this is expected
						return true;
					}
					else
					{
						Console.WriteLine("Exception generated: " + argEx.Message, argEx);
						return false;
					}
				}
			}
		}

		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test AuditRule constructor
		*		protected AuditRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags auditFlags )
		*
		*
		*/
		//----------------------------------------------------------------------------------------------		

		private class AuditRuleConstructorTestCases
		{
			//No creating objects
			private AuditRuleConstructorTestCases(){}

			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running AuditRuleConstructorTestCases");			
				BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed, false);
				AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
			}

			public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed, bool isBVT)
			{
				Console.WriteLine("Running BasicValidationTestCases");


				int bvtCaseCounter = 0;
				
				string[] testCase = null;
				ITestCaseReader reader = new InfTestCaseStore();
				reader.OpenTestCaseStore(AuditRuleConstructorTestCaseStore); 
				while (null != (testCase = reader.ReadNextTestCase()))
				{
					// read identityReferenceType
					int identityReferenceType;
					if(1 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						identityReferenceType = int.Parse(testCase[0]);
					
					// read sid
					string sid;
					if (2 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else if ("null" == testCase[1])
						sid = null;
					else
						sid = testCase[1];

					// read accessMask
					int accessMask;
					if (3 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						accessMask = int.Parse(testCase[2]);

					//read isInherited
					bool isInherited;
					if(4 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						isInherited = bool.Parse(testCase[3]);

					// read inheritanceFlags
					int inheritanceFlags;
					if (5 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						inheritanceFlags = int.Parse(testCase[4]);

					// read propagationFlags
					int propagationFlags;
					if (6 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						propagationFlags = int.Parse(testCase[5]);
					
					// read auditFlags
					int auditFlags;
					if (7 > testCase.Length)
						throw new ArgumentOutOfRangeException(); 
					else
						auditFlags = int.Parse(testCase[6]);

					testCasesPerformed++;

					
					try
					{
						if (TestAuditRuleConstructor(identityReferenceType, sid, accessMask, isInherited, inheritanceFlags, propagationFlags, auditFlags))
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

			private static bool TestAuditRuleConstructor(int identityReferenceType, string sid, int accessMask, bool isInherited, int inheritanceFlags, int propagationFlags, int auditFlags)
			{
				FileSystemAuditRule auditRule = null;
				IdentityReference  identityReference = null;

				if (IdentityReferenceType.SID == (IdentityReferenceType)identityReferenceType)
				{
					if(null == sid)
						identityReference = null;
					else
						identityReference = new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid));
				}
				else if(IdentityReferenceType.NTACCOUNT == (IdentityReferenceType) identityReferenceType)
				{
					identityReference= (new SecurityIdentifier(Utils.UtilTranslateStringConstFormatSidToStandardFormatSid(sid))).Translate(typeof(NTAccount));
				}
				else
				{
					Console.WriteLine("Invalid identityReference type, should be 0 - SecurityIdentifier or 1 - NTAccount");
					return false;
				}
				try
				{
					Console.WriteLine("identifyReference: {0}, accessMask: {1}, isInherited: {2}, inheritanceFlags: {3}, propogationFlags: {4}, auditFlags: {5}",
						identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, auditFlags);
					
					auditRule = new FileSystemAuditRule(identityReference, accessMask, isInherited, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags, (AuditFlags)auditFlags);
					Console.WriteLine("FileSystemAccessRule object is created successfully");
					
					if(null == identityReference )
					{
						Console.WriteLine("Should not allow null identity");
						return false;
					}
					if( identityReference.IsValidTargetType( typeof( System.Security.Principal.SecurityIdentifier )) == false )
					{
						Console.WriteLine("Should not allow non-translatable into SecurityIdentifier identity");
						return false;
					}
					if ( 0 == accessMask )		
					{
						Console.WriteLine("Should not allow accessMask 0");
						return false;
					}

					if((InheritanceFlags) inheritanceFlags < InheritanceFlags.None || (InheritanceFlags)inheritanceFlags > (InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit))
					{
						Console.WriteLine("Should not allow inheritanceFlags less than {0} or bigger than ", InheritanceFlags.None, (InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit));
						return false;						
					}

					if((PropagationFlags)propagationFlags < PropagationFlags.None || (PropagationFlags)propagationFlags > (PropagationFlags.NoPropagateInherit | PropagationFlags.InheritOnly))
					{
						Console.WriteLine("Should not allow propagationFlags less than {0} or bigger than ", PropagationFlags.None , (PropagationFlags.NoPropagateInherit | PropagationFlags.InheritOnly));
						return false;							
					}
					
					if ( AuditFlags.Success  != (AuditFlags)auditFlags && AuditFlags.Failure != (AuditFlags)auditFlags && (AuditFlags)auditFlags != (AuditFlags.Failure|AuditFlags.Success)  )
					{
						Console.WriteLine("Should not allow auditFlags beyard Success and Failure, Bug 217883");
						return false;
					}
					
					//object creation succeeds, use properties to check if the object is created correctly
					if( !identityReference.Equals(auditRule.IdentityReference))
					{
						Console.WriteLine("the Identityreference is not equal to the one passed to constructor");
						return false;
					}
					if(auditRule.AccessMask != accessMask)
					{
						Console.WriteLine("the AccessMask is not equal to the one passed to constructor");
						return false;
					}	
					if(auditRule.IsInherited != isInherited)
					{
						Console.WriteLine("the IsInherited is not equal to the one passed to constructor");
						return false;
					}						
					if(auditRule.InheritanceFlags != (InheritanceFlags)inheritanceFlags)
					{
						Console.WriteLine("the InheritanceFlags is not equal to the one passed to constructor");
						return false;
					}
					if(auditRule.PropagationFlags != (PropagationFlags)propagationFlags)
					{
						if((auditRule.PropagationFlags != PropagationFlags.None) || (auditRule.InheritanceFlags != InheritanceFlags.None))
						{
							Console.WriteLine("the PropagationFlags is not equal to the one passed to constructor");
							return false;
						}
					}
					if(auditRule.AuditFlags != (AuditFlags)auditFlags)
					{
						Console.WriteLine("the AuditFlags is not equal to the one passed to constructor");
						return false;
					}
					//all verifications pass
					return true;
				}
				catch (ArgumentNullException nullEx)
				{
					if(null == identityReference )
					{//this is expected
						return true;
					}
					else
					{
						Console.WriteLine("Exception generated: " + nullEx.Message, nullEx);
						return false;
					}
				}
				catch (ArgumentOutOfRangeException outOfRangeEx)
				{
					if (( (AuditFlags)auditFlags != AuditFlags.Success &&  (AuditFlags)auditFlags != AuditFlags.Failure && (AuditFlags)auditFlags != (AuditFlags.Failure|AuditFlags.Success) )	||
						((InheritanceFlags) inheritanceFlags < InheritanceFlags.None || (InheritanceFlags)inheritanceFlags > (InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit))||
						((PropagationFlags)propagationFlags < PropagationFlags.None || (PropagationFlags)propagationFlags > (PropagationFlags.NoPropagateInherit | PropagationFlags.InheritOnly)))
					{//this is expected
						return true;
					}
					else
					{
						Console.WriteLine("Exception generated: " + outOfRangeEx.Message, outOfRangeEx);
						return false;
					}			
				}

				catch (ArgumentException argEx)
				{
					if( identityReference.IsValidTargetType( typeof( System.Security.Principal.SecurityIdentifier )) == false || ( 0 == accessMask ) || (AuditFlags.None == (AuditFlags)auditFlags))
					{//this is expected
						return true;
					}
					else
					{
						Console.WriteLine("Exception generated: " + argEx.Message, argEx);				
						return false;						
					}
				}								
			}
		}


		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test AuthorizationRuleCollectionCopyTo methods
		*		public void CopyTo( AuthorizationRule[] rules, int index )
		*
		*/
		//----------------------------------------------------------------------------------------------		

		private class AuthorizationRuleCollectionCopyToTestCases
		{
			
			//No creating objects
			private AuthorizationRuleCollectionCopyToTestCases(){}

			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running AuthorizationRuleCollectionCopyToTestCases");							
				BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
				AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
			}

			public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running BasicValidationTestCases");


				
			       AuthorizationRuleCollection ruleCollection = null;				
				AuthorizationRule[] rules = null;
				
				//Case 1,  when collection is actually empty
				FileSecurity leafObjectSecurity = new FileSecurity();
				ruleCollection = leafObjectSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
				rules = new AuthorizationRule[ruleCollection.Count];
				testCasesPerformed++;


				
				try
				{
					ruleCollection.CopyTo(rules, 0);
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);
				}

				

				//Case 2, collection is not empty
				leafObjectSecurity = new FileSecurity(AccessRuleConstructorTestCaseStore, AccessControlSections.Access);
				ruleCollection = leafObjectSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
				rules = new AuthorizationRule[ruleCollection.Count + 1];
				testCasesPerformed++;


				try
				{
					ruleCollection.CopyTo(rules, 1);
					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);				
				}
				
				
			}

			public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				// No test cases yet
			}

		}

		//----------------------------------------------------------------------------------------------
		/*
		*  Class to test AuthorizationRuleCollection Indexer
		*		public AuthorizationRule this[int index]
		*
		*/
		//----------------------------------------------------------------------------------------------		

		private class AuthorizationRuleCollectionIndexerTestCases
		{
			
			//No creating objects
			private AuthorizationRuleCollectionIndexerTestCases(){}

			public static void AllTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{

				Console.WriteLine("Running AuthorizationRuleCollectionIndexerTestCases");
				BasicValidationTestCases(ref testCasesPerformed, ref testCasesPassed);
				AdditionalTestCases(ref testCasesPerformed, ref testCasesPassed);
			}

			public static void BasicValidationTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				Console.WriteLine("Running BasicValidationTestCases");


				
			       AuthorizationRuleCollection ruleCollection = null;				
				AuthorizationRule accessRule = null;
				
				//Case 1,  when collection is actually empty
				FileSecurity leafObjectSecurity = new FileSecurity();
				ruleCollection = leafObjectSecurity.GetAccessRules(false, true, typeof(SecurityIdentifier));
				
				testCasesPerformed++;

				try
				{
					accessRule = ruleCollection[0];
					Console.WriteLine("Should not allow access index 0 of empty collection");
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
				
				
				//Case 2, collection is not empty, access all elements of the collection by index
				leafObjectSecurity = new FileSecurity(AccessRuleConstructorTestCaseStore, AccessControlSections.Access);
				ruleCollection = leafObjectSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
				testCasesPerformed++;

				
				try
				{
					for(int i = 0; i < ruleCollection.Count; i++)
					{
						accessRule = ruleCollection[i];
					}

					Console.WriteLine("Test case {0} PASSED!", testCasesPerformed);
					testCasesPassed++;
				}
				catch(Exception e)
				{
					Console.WriteLine("Exception generated: " + e.Message, e);					
				}
				

				//Case 3, collection is not empty, access element at index Count
				leafObjectSecurity = new FileSecurity(AccessRuleConstructorTestCaseStore, AccessControlSections.Access);
				ruleCollection = leafObjectSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
				testCasesPerformed++;

				try
				{
					accessRule = ruleCollection[ruleCollection.Count];
					Console.WriteLine("Should not allow access index at Count");
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

			public static void AdditionalTestCases(ref int testCasesPerformed, ref int testCasesPassed)
			{
				// No test cases yet
			}

		}
	}
}

