//--------------------------------------------------------------------------
//
//		Utility Class
//
//		Provide common functionality to the testing code 
//
//		Copyright (C) Microsoft Corporation, 2003
//
//--------------------------------------------------------------------------

using System;
using System.Collections;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Security.AccessControl.Test
{	
	public class Utils
	{

		/*
		* Method Name: UtilIsAceEqual
		*
		* Description:	compare if two ACEs are equal by byte by byte comparison
		*
		* Parameter:	ace1, ace2 -- the two ACEs to be comparied
		*
		* Return:		true if equal, otherwise false
		*/
		public static bool UtilIsAceEqual(GenericAce ace1, GenericAce ace2)
		{
			bool result = true;
			byte [] ace1BinaryForm;
			byte [] ace2BinaryForm;


			if(null != ace1 && null != ace2)
			{
				//check the BinaryLength
				if(ace1.BinaryLength != ace2.BinaryLength)
				{
					result = false;
				}
				else
				{
					ace1BinaryForm = new byte[ace1.BinaryLength];
					ace2BinaryForm = new byte[ace2.BinaryLength];
					ace1.GetBinaryForm(ace1BinaryForm, 0);
					ace2.GetBinaryForm(ace2BinaryForm, 0);
					if(!UtilIsBinaryFormEqual(ace1BinaryForm, ace2BinaryForm))
					{
						result = false;
					}
				}
			}
			else if (null == ace1 && null == ace2)
			{
				Console.WriteLine("Both aces are null");
			}
			else
				result = false;
			return result;
		}

		

		/*
		* Method Name: UtilIsBinaryFormEqual
		*
		* Description:	compare if two byte Arrays are equal by byte by byte comparison
		*
		* Parameter:	binaryForm1, binaryForm2 -- the two byte Arrays to be comparied
		*			offset -- start position in binaryForm1
		*
		* Return:		true if equal, otherwise false
		*/

		public static bool UtilIsBinaryFormEqual(byte[] binaryForm1, int offset, byte[] binaryForm2)
		{
			bool result = true;
			if(null == binaryForm1 && null == binaryForm2)
				result = true;
			else if ( null != binaryForm1 && null != binaryForm2)
			{
				if(binaryForm1.Length - offset != binaryForm2.Length)
				{
					result = false;
				}
				else
				{
					for(int i= 0; i < binaryForm2.Length; i++)
					{
						if(binaryForm1[offset + i] != binaryForm2[i])
						{
							result = false;
							break;
						}
					}
				}
			}
			else
				result = false;
			return result;
		}


		/*
		* Method Name: UtilIsBinaryFormEqual
		*
		* Description:	compare if two byte Arrays are equal by byte by byte comparison
		*
		* Parameter:	binaryForm1, binaryForm2 -- the two byte Arrays to be comparied
		*
		* Return:		true if equal, otherwise false
		*/

		public static bool UtilIsBinaryFormEqual(byte[] binaryForm1, byte[] binaryForm2)
		{
			return UtilIsBinaryFormEqual(binaryForm1, 0, binaryForm2);
		}

		/*
		* Method Name: UtilCreateRawAclFromString
		*
		* Description:	create a RawAcl from a string
		*			the string for RawAcl is in the following format: ACE[#ACE]
		*			where ACE is in format: aceFlags:aceQualifier:accessMask:sid:isCallback:opaqSize
		*
		* Parameter:	rawAclString -- the string define the RawAcl
		*
		* Return:		the RawAcl created from the string
		*/

		public static RawAcl  UtilCreateRawAclFromString(string rawAclString)
		{
			RawAcl rawAcl = null;
			byte revision = 0;
			int capacity = 1;
			CommonAce cAce = null;
			AceFlags aceFlags = AceFlags.None;
			AceQualifier aceQualifier = AceQualifier.AccessAllowed;
			int accessMask = 1;
			SecurityIdentifier sid = null;
			bool isCallback = false;
			int opaqueSize = 0;
			byte [] opaque = null;
			

			string[] parts = null;
			string[] subparts = null;
			char [] delimiter1 = new char[] {'#'};
			char [] delimiter2 = new char[] {':'};

			if(rawAclString != null)
			{
				rawAcl = new RawAcl(revision, capacity);
				
				parts = rawAclString.Split(delimiter1);
				for (int i = 0; i< parts.Length; i++)
				{
					subparts = parts[i].Split(delimiter2);
					if( subparts.Length != 6)
					{
						return null;
					}
					
					aceFlags = (AceFlags)byte.Parse(subparts[0]);
					aceQualifier = (AceQualifier) int.Parse(subparts[1]);
					accessMask = int.Parse(subparts[2]);
					sid = new SecurityIdentifier(UtilTranslateStringConstFormatSidToStandardFormatSid(subparts[3]));
					isCallback = bool.Parse(subparts[4]);
					if(!isCallback)
						opaque = null;
					else
					{
						opaqueSize = int.Parse(subparts[5]);
						opaque = new byte [opaqueSize];
					}
					cAce = new CommonAce(aceFlags, aceQualifier, accessMask, sid, isCallback, opaque);
					rawAcl.InsertAce(rawAcl.Count, cAce);					
				}				
			}
			return rawAcl;			
		}


		//utility function to create SecurityDescriptor binary form from sddl form
		public static int UtilCreateBinaryArrayFromSddl(string stringSd, out byte[] BinaryForm )
		{
			int ErrorCode;
			IntPtr ByteArray;
			uint ByteArraySize = 0;

			BinaryForm = new Byte[0];
			if ( 0 != Win32SecurityDescriptorLayer.ConvertStringSdToSd( stringSd, GenericSecurityDescriptor.Revision, out ByteArray, ref ByteArraySize ))
			{//native call succeed

				try
				{
					BinaryForm = new Byte[ByteArraySize];
					//
					// Extract the data from the returned pointer
					//
					Marshal.Copy( ByteArray, BinaryForm, 0, ( int )ByteArraySize );
				}
				finally
				{//make sure memory is freeed
					Win32SecurityDescriptorLayer.LocalFree( ByteArray );
				}
				return 0;
			}
			else
			{//native call fails
				ErrorCode = Marshal.GetLastWin32Error();

				if ( ErrorCode == Win32SecurityDescriptorLayer.ERROR_NOT_ENOUGH_MEMORY )
				{
					throw new OutOfMemoryException();
				}

				return ErrorCode;				
			}

		}

		//on Win2k, SecurityIdentifier constructor does not take String Constant Format Sid
		//this utility function translate it to StandardFormat
		public static string UtilTranslateStringConstFormatSidToStandardFormatSid(string sidStringConst)
		{
			string stFormatSid = null;
			if (sidStringConst == "BA")
				stFormatSid = "S-1-5-32-544";
			else if (sidStringConst == "BO")
				stFormatSid = "S-1-5-32-551";
			else if (sidStringConst == "BG")
				stFormatSid = "S-1-5-32-546";

			else if (sidStringConst == "AN")
				stFormatSid = "S-1-5-7";	

			else if (sidStringConst == "NO")
				stFormatSid = "S-1-5-32-556";	

			else if (sidStringConst == "SO")
				stFormatSid = "S-1-5-32-549";	

			else if (sidStringConst == "RD")
				stFormatSid = "S-1-5-32-555";	

			else if (sidStringConst == "SY")
				stFormatSid = "S-1-5-18";	

			else
				stFormatSid = sidStringConst;
			return stFormatSid;
		}
		

		/*
		* Method Name: UtilPrintBinaryForm
		*
		* Description:	print out a byte Array
		*
		* Parameter:	binaryForm -- the byte Array
		*
		* Return:		none
		*/
		
		internal static void UtilPrintBinaryForm(byte[] binaryForm)
		{
			Console.WriteLine();

			if(binaryForm != null)
			{
				Console.WriteLine("BinaryForm:");
				for (int i=0; i<binaryForm.Length; i++)
				{
					Console.WriteLine("{0}", binaryForm[i]);
				}
				Console.WriteLine();
			}
			else
				Console.WriteLine("BinaryForm: null");
		}		 
	 
	}
}
				

