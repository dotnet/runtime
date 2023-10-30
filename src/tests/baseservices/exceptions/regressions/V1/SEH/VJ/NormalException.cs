// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;

class UserException : Exception{
	
	public UserException(){
		throw new ArithmeticException();	
	}
}

class ComplexByte {

	byte real = 0 ;
	byte imag = 0 ;

	byte getrealpart( )
	{
		return real;
	}

	byte getimagpart( )
	{
		return imag;
	}

	int equals( int realparam, int imagparam )
	{
		if (real != realparam)
		{
			return 0;
		}
		if (imag != imagparam)
		{
			return 0;
		}
		return 1;
	}

	int equals( ComplexByte cparm )
	{
		if (real != cparm.getrealpart())
		{
			return 0;
		}
		if(imag != cparm.getimagpart( ))
		{
			return 0;
		}
		return 1;
	}


	void assign( int realparm, int imagparm ) 
	{
		if( ( realparm > 127 ) || ( realparm < -128 ) )
			throw new ArgumentException();
		if( ( imagparm > 127 ) || ( imagparm < -128 ) )		
			throw new ArgumentException();
		real = (byte) realparm;
		imag = (byte) imagparm;		
	}

	void assign( ComplexByte cparm )
	{
		real = cparm.getrealpart( );
		imag = cparm.getimagpart( );
	}

	public void add( int realparm, int imagparm ) 
	{
		int rtemp, itemp;

		rtemp = real + realparm;
		if ( ( rtemp > 127 ) || ( rtemp < -128 ) )
			throw new ArithmeticException();
		itemp = imag + imagparm;
		if ( ( itemp > 127 ) || ( itemp < -128 ) )
			throw new ArithmeticException();	
		real = (byte)(rtemp);
		imag = (byte)(itemp);
	}

	void add( ComplexByte cparm ) 
	{
		int rtemp, itemp;
		rtemp = real + cparm.getrealpart();
		if ( ( rtemp > 127 ) || ( rtemp < -128 ) )
			throw new ArithmeticException();
		itemp = imag + cparm.getimagpart();
		if ( ( itemp > 127 ) || ( itemp < -128 ) )
			throw new ArithmeticException();	
		real = (byte)( rtemp);
		imag = (byte)( itemp );
	}


	void multiply( int realparm, int imagparm ) 
	{
		int rtemp, itemp;

		rtemp = ( real * realparm - imag * imagparm );
		itemp = (real * imagparm + realparm * imag );
		if ( ( rtemp > 127 ) || ( rtemp < -128 ) )
			throw new ArithmeticException();
		if ( ( itemp > 127 ) || ( itemp < -128 ) )
			throw new ArithmeticException();	
		real = (byte) rtemp;
		imag = (byte) itemp;
	}

	void multiply( ComplexByte cparm )  
	{
		int rtemp, itemp;
	
		rtemp = (real * cparm.getrealpart( ) - imag * cparm.getimagpart( ));
		itemp = (real * cparm.getimagpart( ) + cparm.getrealpart( ) * imag);

		if ( ( rtemp > 127 ) || ( rtemp < -128 ) )
			throw new ArithmeticException();
		if ( ( itemp > 127 ) || ( itemp < -128 ) )
			throw new ArithmeticException();	
		real = (byte) rtemp;
		imag = (byte) itemp;
	}

	public ComplexByte( )
	{
		this.real =  0 ;
		this.imag =  0 ;		
	}

	public ComplexByte( int realparm, int imagparm ) 
	{
		if ( ( realparm > 127 ) || ( realparm < -128 ) )
			throw new ArgumentException();
		if ( ( imagparm > 127 ) || ( imagparm < -128 ) )
			throw new ArgumentException();
		real = (byte) realparm;
		imag = (byte) imagparm;		
	}

	public ComplexByte( ComplexByte cparm )
	{
		this.real = cparm.getrealpart( );
		this.imag = cparm.getimagpart( );
	}

}

public class NormalException {

    [Fact]
    public static int TestEntryPoint() 
	{
		String s = "Done";
		int retVal = 100;
		int tryflag = 1;

		try {
			throw new UserException();
		}
		catch (ArithmeticException ){
			Console.WriteLine("AE was caught");	
			
		}

		try {
			ComplexByte c4 = new ComplexByte(  200, -200  );
		}
		catch ( ArgumentException ) {
			tryflag = 0;  // we caught it		
			Console.WriteLine( "Caught Argument Exception in Test Case 8" );
		}
		finally {
			if ( tryflag != 0 ) {
				retVal = 8;
			}
		}

		tryflag = 1;

		try {
				ComplexByte c4 = new ComplexByte(  100, -100  );
				c4.add( 200, -200 );
		}
		catch ( ArithmeticException ) {
				tryflag = 0;  // we caught it		
				Console.WriteLine( "Caught Arithmetic Exception in Test Case 9" );
		}
		finally {
			if ( tryflag != 0 ) {
				retVal = 9;
			}
		}
		
		
		Console.WriteLine(s);
		return retVal;		
    }  
	 
}  
