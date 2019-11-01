// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Globalization;

class Co1727
{
	public bool runTest()
	{
		int iCountTestcases = 0;
		int iCountErrors    = 0;

		///////////////// TEST 1
		//[]make new valid dictionary entry
		TestLibrary.Logging.WriteLine( "make new valid dictionary entry" );
		try
		{
			++iCountTestcases;
			DictionaryEntry de = new DictionaryEntry( 1, 2 );

			if ( ! 1.Equals( de.Key ) )
			{
				++iCountErrors;
				TestLibrary.Logging.WriteLine( "Err_001a,  incorrect key" );
			}

			if ( ! 2.Equals( de.Value ) )
			{
				++iCountErrors;
				TestLibrary.Logging.WriteLine( "Err_001b,  incorrect value" );
			}

		}
		catch (Exception ex)
		{
			++iCountErrors;
			TestLibrary.Logging.WriteLine( "Err_001c,  Unexpected exception was thrown ex: " + ex );
		}

		//////////////// TEST 2
		//[]make dictionary entry which should throw ArgumentNullException since key is null
		TestLibrary.Logging.WriteLine( "make dictionary entry which should throw ArgumentNullException since key is null" );
		try
		{
			++iCountTestcases;
			DictionaryEntry de = new DictionaryEntry( null, 1 );

			if ( de.Key != null )
			{
				++iCountErrors;
				TestLibrary.Logging.WriteLine( "Err_002a,  incorrect key" );
			}

			if ( !de.Value.Equals(1) )
			{
				++iCountErrors;
				TestLibrary.Logging.WriteLine( "Err_002b,  incorrect value" );
			}
		
		}
		catch (Exception ex)
		{
			++iCountErrors;
			TestLibrary.Logging.WriteLine( "Err_002c,  Excpected ArgumentNullException but thrown ex: " + ex.ToString() );
		}

		///////////////// TEST 3
		//[]make new valid dictionary entry with value null
		TestLibrary.Logging.WriteLine( "make new valid dictionary entry with value null" );
		try
		{
			++iCountTestcases;
			DictionaryEntry de = new DictionaryEntry( this, null );

			if ( ! this.Equals( de.Key ) )
			{
				++iCountErrors;
				TestLibrary.Logging.WriteLine( "Err_003a,  incorrect key" );
			}

			if ( de.Value != null )
			{
				++iCountErrors;
				TestLibrary.Logging.WriteLine( "Err_003b,  incorrect value" );
			}
		}
		catch (Exception ex)
		{
			++iCountErrors;
			TestLibrary.Logging.WriteLine( "Err_003c,  Unexpected exception was thrown ex: " + ex.ToString() );
		}


		return (iCountErrors == 0 );
	}

	public static int Main( String [] args )
	{
		Co1727 runClass;
		bool bResult;

		runClass = new Co1727();
		bResult  = runClass.runTest();

		if (bResult)
		{
			TestLibrary.Logging.WriteLine("PASS");
			return 100;
		}
		else
		{
			TestLibrary.Logging.WriteLine("FAIL");
			return 0;
		}
	}

}
