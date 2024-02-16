// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security;
using Xunit;

public class TestClass {

        static int iExitCode;
    
	void TestMain() 
	{
		int caught = 0;
		int fincount = 0;

		try {
			throw new ArgumentException();
//  		      	Console.WriteLine("Exception not thrown.");
//  			iExitCode = 1;
		}
		catch (ArithmeticException ) {
			Console.WriteLine("Caught wrong exception.");
			iExitCode = 2;
		}
	 	catch (ArgumentException ) {
			caught ++;
			try {
			    try{
				try{
				    try{
					try{
					    try{
						try{
						    try{
						      int [] arr = new int[1000];
						      	throw new SecurityException();
//							Console.WriteLine("Exception not thrown.");
//  							iExitCode = 1;
						    }
						    catch(DivideByZeroException ){
						    }
						    finally
						      {
							double [] s = new double[1];
						      }
						    
						}
						catch(DivideByZeroException ){
						}
					    }
					    catch(DivideByZeroException ){
					    }
					    finally
					      {
						Console.WriteLine("Somewhere in a finally");
						float [] j = new float[99];
					      }
					    
					}
					catch(DivideByZeroException ){
					}
				    }
				    catch(DivideByZeroException ){
				    }
				}
				catch(DivideByZeroException ){
				}
			    }
			    catch(DivideByZeroException ){
			    }
			}
			catch(DivideByZeroException ){
			}
			catch (SecurityException ) {
				caught ++;
			}
			catch (Exception ) {
			        Console.WriteLine("Didn't catch specific exception.");
				iExitCode = 3;
				try{
				    Console.WriteLine("Something new");
				}
				catch(DivideByZeroException ){
				}
				finally{
				    Console.WriteLine("Is this ever reached?");
				}
				
			}
			finally {
			  int [] i = new int[10];
			  try{
			    throw new NullReferenceException();
//  			    Console.WriteLine("Exception Not Thrown in Finally");		    		    
			  }
			  catch(NullReferenceException e){
			      Console.WriteLine(e.StackTrace);
			      caught++;			    
			  }
			  catch(Exception ){
			    Console.WriteLine("Correct Exception not caught");
			  }
			  finally
			    {
				GC.Collect();
		      		fincount++;
			    }
			  
			  fincount ++;
			}	
		}
		catch (Exception ) {
			Console.WriteLine("Didn't catch specific exception.");
			iExitCode = 3;
		}
		finally {
		        GC.Collect();
			fincount ++;
		}	

		try {
			try {
				throw new NullReferenceException();
//  				Console.WriteLine("Exception not thrown.");
//  				iExitCode = 1;
			}
			catch (NullReferenceException ) {
				caught ++;
				throw new OutOfMemoryException();
//  				Console.WriteLine("Exception not thrown.");
//  				iExitCode = 1;
			}
			catch (Exception ) {
				Console.WriteLine("Didn't catch specific exception.");
				iExitCode = 3;
			}
			finally {
				GC.Collect();
				fincount ++;
			}	
		}
		catch (OutOfMemoryException ) {
			caught ++;
		}
		finally {
			GC.Collect();
			fincount ++;
		}	

		if (caught != 5) {
			Console.WriteLine("Didn't catch enough exceptions.");
			iExitCode = 4;
		}
		if (fincount != 5) {
			Console.WriteLine("Didn't execute enough finallys.");
			iExitCode = 5;
		}
	}

	[Fact]
	public static int TestEntryPoint()
	{
		int retVal = 100;
		String str = "Done";
		(new TestClass()).TestMain();
		if (iExitCode == 0) {
			Console.WriteLine("Test Passed.");
		} else {
			Console.WriteLine("Test FAILED.");
			retVal = iExitCode;
		}	
		Console.WriteLine(str);
                return retVal;
}

};


