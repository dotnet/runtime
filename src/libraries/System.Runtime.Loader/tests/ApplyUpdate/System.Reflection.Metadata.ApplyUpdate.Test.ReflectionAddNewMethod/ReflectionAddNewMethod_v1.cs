// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;
using CancellationToken = System.Threading.CancellationToken;

namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public class ReflectionAddNewMethod
    {
        public string ExistingMethod(string u, double f)
	{
            return u + f.ToString();;
        }

	public double AddedNewMethod(char c, float h, string w, CancellationToken ct = default, [CallerMemberName] string callerName = "")
	{
	    return ((double)Convert.ToInt32(c)) + h;
	}
    }
}
