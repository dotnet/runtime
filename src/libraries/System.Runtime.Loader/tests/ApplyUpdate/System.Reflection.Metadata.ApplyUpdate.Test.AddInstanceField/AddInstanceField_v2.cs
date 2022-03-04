// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;


namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public class AddInstanceField
    {
        public AddInstanceField () {
        }

        public string GetField => field2;

	public int GetIntField => field3;
	
        private string field;

        private string field2;

	private int field3;

        public void TestMethod () {
            field = "spqr";
            field2 = "7890";
	    field3 = 123;
        }

    }
}
