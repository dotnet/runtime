// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using System.IO;
using Xunit;

class UserException1 : Exception {
	public int ExceptionId;
	
	public UserException1(int id){
		ExceptionId = id;	
	}
}

class UserException2 : Exception {
	public int ExceptionId;
	
	public UserException2(int id){
		ExceptionId = id;	
	}
}

class UserException3 : Exception {
	public int ExceptionId;
	
	public UserException3(int id){
		ExceptionId = id;	
	}
}

class UserException4 : Exception {
	public int ExceptionId;
	
	public UserException4(int id){
		ExceptionId = id;	
	}
}


public class HandlerException {
	private int ThreadId;

	public HandlerException(int id){
		ThreadId = id;
	}
		
	
	[Fact]
	public static int TestEntryPoint() {
		String s = "Done";
		int retVal = 100;
		Thread mv_Thread;
		HandlerException [] he = new HandlerException[10];
		for (int i = 0 ; i < 10; i++){
			he[i] = new HandlerException(i);
			mv_Thread = new Thread(new ThreadStart(he[i].runtest));
			try {
				mv_Thread.Start();
			}
			catch (Exception ){
				Console.WriteLine("Exception was caught in main");
				retVal = 0;
			}
		}
		Console.WriteLine(s);
		return retVal;
	}
		
	public void runtest(){
		try {
			try {
				try {
					try {
						switch (ThreadId){
						case 0: throw new UserException1(ThreadId);
						case 1: throw new UserException2(ThreadId);
						case 2: throw new UserException3(ThreadId);
						case 3: throw new UserException4(ThreadId);
						default:
								throw new Exception();
						}
						
					}
					catch (UserException1 e){
					    lock(this)
						{
						    Console.WriteLine("Exception1 was caught by Thread " + e.ExceptionId );
						}
					    
						throw new UserException2(ThreadId);
					}
				}
				catch (UserException2 e) {
				    lock(this)
					{
					    Console.WriteLine("Exception2 was caught by Thread " + e.ExceptionId );
					}
				    throw new UserException3(ThreadId);
				}
			}
			catch (UserException3 e){
			    lock(this)
				{
				    Console.WriteLine("Exception3 was caught by Thread " + e.ExceptionId );
				}
			    throw new UserException4(ThreadId);
			}
		}
		catch (UserException4 e) {
		    lock(this)
			{
			    Console.WriteLine("Exception4 was caught by Thread " + e.ExceptionId );
			}
		}
		catch (Exception ){
		    lock(this)
			{
			    Console.WriteLine("Exception was caught");
			}
		}
	}
	
}
