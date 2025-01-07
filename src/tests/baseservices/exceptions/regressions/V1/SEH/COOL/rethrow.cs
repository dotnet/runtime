// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Xunit;

public class UserException1 : Exception {
	int ExceptionId;
	
    public UserException1()
    {
    }
    
	public UserException1(int id){
		ExceptionId = id;	
	}

public class UserException2 : UserException1 {
	new int ExceptionId;
	
    public UserException2() 
    {
    }
	public UserException2(int id) : base(id)
	{
		ExceptionId = id;	
	}

public class UserException3 : UserException2 {
	new int ExceptionId;

    public UserException3()
    {
    }
	
	public UserException3(int id) : base(id)
	{
		ExceptionId = id;	
	}

public class UserException4 : UserException3 {
	new int ExceptionId;
	
    public UserException4()
    {
    }
	public UserException4(int id) : base(id)
	{
		ExceptionId = id;	
	}

public class RethrowException {
	private int ThreadId;

	public RethrowException(int id){
		ThreadId = id;
	}
		
	
	[Fact]
	public static int TestEntryPoint() {
	  String s = "Done";
	    System.IO.TextWriter t = Console.Out;
	    Console.SetOut(t);
	    int retVal = 101;
	    	Thread[] mv_Thread = new Thread[10];
		RethrowException[] he = new RethrowException[12];
		for (int i = 0 ; i < 10 ; i++){
			he[i] = new RethrowException(i);
			mv_Thread[i] = new Thread(new ThreadStart(he[i].runtest));
			try {
				mv_Thread[i].Start();
			}
			catch (Exception ){
				Console.WriteLine("Exception was caught in main");
			}
		}
		for (int i = 0; i < 10; i++){
		    mv_Thread[i].Join();
		}
		Console.WriteLine("\n\n Test Passed");
		Console.WriteLine(s);
		retVal = 100;
                return retVal;
	}
		
	public void runtest(){
		try {
			try {
				try {
					try {
						switch (ThreadId % 4){
						case 0: throw new UserException1(ThreadId);
						case 1: throw new UserException2(ThreadId);
						case 2: throw new UserException3(ThreadId);
						case 3: throw new UserException4(ThreadId);
						default:
								throw new Exception();
						}
						
					}
					catch (UserException4 e){
					    lock(this)
						{
						    Console.WriteLine("Exception4 was caught by Thread " + e.ExceptionId );
						    throw ;
						}
					}
				}
				catch (UserException3 e) {
				    lock(this)
					{
					    Console.WriteLine("Exception3 was caught by Thread " + e.ExceptionId );
					    throw ;
					}
				    
				}
			}
			catch (UserException2 e){
			    lock(this)
				{
				    Console.WriteLine("Exception2 was caught by Thread " + e.ExceptionId );
				    throw ;
				}
			}
		}
		catch (UserException1 e) {
		    lock(this)
			{
			    Console.WriteLine("Exception1 was caught by Thread " + e.ExceptionId );
			}
		}
		catch (Exception ){
		    lock(this)
			{
			    Console.WriteLine("Exception was caught");
			}
		}

	}
	
} // REthrow
} //UserException1
} //UserException2
} //UserException3
} //UserException4

