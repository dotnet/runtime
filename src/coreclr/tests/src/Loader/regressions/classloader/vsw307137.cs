// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//


/*

A summary 

In V1 and Everett, sequential classes only affected unmanaged layout and the .size metadata was used to add padding to the 
unmanaged layout (IJW used this to implement their unmanaged structures. That's the only reason the .size metadata exists.
Somewhere along the line, C# started using the .size metadata to implemented fixed buffer arrays inside managed structures. 
They'd declare just one field of the array element type and use the .size metadata to allocate space for the other elements. 
Problem was, the .size metadata was never defined to expand the managed layout. This feature should never have worked.

So how did it work for them? Plain luck. In the CLR, if your structure consists of just one scalar-type field, the CLR 
classifies it internally as a "blittable" structure. That is, it makes the managed layout match the unmanaged layout byte for 
byte so that interop can optimize the marshaling of this structure to a bit-copy. Thus, because of this accidental reliance on
this internal optimization by the CLR, fixed buffers "worked" for plain old integer/float/double types. 
There was two cases where it wouldn't work: chars and booleans. That's because these datatypes don't translate byte for 
byte (different sizes, bools need normalizing.) So the blitting optimization doesn't kick in in those cases.

Someone found out that the was case for chars and opened VSW:147145. A couple months later, I unwittingly"fixed" this bug 
when I implemented the managed sequential layout feature. This feature now causes the .size metadata to expand out the 
managed layout too (whether this was a good idea is debatable but it got VSW:147145 off the CLR team's back even though 
they didn't understand how it got fixed..)

Now we have the boolean case. Booleans are also non-blittable (they have to be normalized so they aren't blittable even if you 
use a FieldMarshal override to force the native size to 1.) But because both the unmanaged size and managed size impose a 
minimum value on the .size metadata, the .size metadata can't go below 4 (sizeof(BOOL) in Win32 native).

CLR now overrides the .size metadata if it is less than the minimum size needed to hold the fields. 

*/

using System;

unsafe struct S
{
	fixed bool b[3];
}

public class Test
{	
	public static int  Main()
	{
		try
		{
			#pragma warning disable 219
			S s = new S();
			#pragma warning restore 219
			
			Console.WriteLine("PASS");
			return 100;
		}
		catch (Exception e)
		{
			Console.WriteLine("Caught unexpected excpetion: " + e);
			return 101;
		}
	}

}
