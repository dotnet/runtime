// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace DefaultNamespace
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    internal interface IV2
    { }

    internal struct V2 : IV2
    {
        //public V2() {} 		ANDREIS: commented due compiler error SC0568
        public override bool Equals(Object o) { return false; }
        public override int GetHashCode() { return 0; }
    }

    // The legendary 37-byte value class.
    /*
    value class V3 
    {
    	long a;
    	long b;
    	long c;
    	long d;
    	int e;
    	short f;
    	ubyte g;
    	
    	public V3() {
    		a = 1;
    		b = -2;
    		c = 3;
    		d = -4;
    		e = 5;
    		f = (short)-6;
    		g = 7;
    	}
    	
    	public boolean Validate()
    	{
    		return a==1 && b==-2 && c==3 && d==-4 && e==5 && f==-6 && g==7;
    	}
    	
    	public boolean Equals(Object o) { return false; }
    	public int GetHashCode() { return 0; }
    }
    */

    public class jitAssert
    {
        internal const int Length = 3;

        internal static V2[] V2Array = new V2[Length];
        //	static V3[] V3Array = new V3[Length];

        public static int Main(String[] args)
        {
            for (int i = 0; i < Length; i++)
            {
                V2Array[i] = new V2();
                //			V3Array[i] = new V3();
            }
            return 100;
        }
    }
}
