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

