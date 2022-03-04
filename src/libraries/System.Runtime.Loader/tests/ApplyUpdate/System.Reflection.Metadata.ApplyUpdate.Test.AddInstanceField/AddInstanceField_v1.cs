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

	public int GetIntField => 0;
	
        private string field;

        private string field2;

        public void TestMethod () {
            field = "spqr";
            field2 = "4567";
        }

    }
}
