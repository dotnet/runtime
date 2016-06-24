// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Reflection;

namespace System.Runtime.Loader.Tests
{
    public class TestClass
    {
    	public static Assembly LoadFromDefaultContext(string assemblyNameStr)
    	{
    		var assemblyName = new AssemblyName(assemblyNameStr);
            return Assembly.Load(assemblyName);
    	}
    }
}
