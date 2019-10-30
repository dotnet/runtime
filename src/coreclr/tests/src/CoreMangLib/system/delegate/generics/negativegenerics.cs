// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//Covers various negative binding cases for delegates and generics...
using System;


//Define some classes and method for use in our scenarios...
class A<T>{
	public virtual void GMeth<U>(){}
}

class B<T> : A<int>{
	public override void GMeth<U>(){}
}

//Define our delegate types...
delegate void Closed();
delegate void Open(B<int> b);
delegate void GClosed<T>();

class Test{
	public static int retVal=100;

	public static int Main(){
		//Try to create an open-instance delegate to a virtual generic method (@TODO - Need early bound case here too)
		//Try to create a generic delegate of a non-instantiated type
		//Try to create a delegate over a non-instantiated target type
		//Try to create a delegate over a non-instantiated target method
		//Try to create a delegate to a generic method by name

		//Does Closed() over GMeth<int> == Closed() over GMeth<double>??
		//Does GClosed<int>() over GMeth<int> == GClosed<double>() over GMeth<int>??

		Console.WriteLine("Done - {0}",retVal==100?"Passed":"Failed");
		return retVal;
	}
}
