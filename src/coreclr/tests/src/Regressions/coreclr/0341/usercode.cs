// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.CompilerServices;
using System.Security;

public class UserCode
{
	[SecuritySafeCritical]
	public static bool StubCriticalCallTransparent()
	{
		bool retVal = true;

		try
		{
			retVal = CriticalCallTransparent();

			if (!retVal)
			{
				TestLibrary.TestFramework.LogError("001", "Transparent UserCode should be able to call SecurityTreatAsSafe PlatformCode");
				retVal = false;
			}
		}
		catch (Exception e)
		{
			TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
			retVal = false;
		}

		return retVal;
	}

	[SecurityCritical]
	[MethodImplAttribute(MethodImplOptions.NoInlining)]
	public static bool CriticalCallTransparent()
	{
		return true;
	}
}

