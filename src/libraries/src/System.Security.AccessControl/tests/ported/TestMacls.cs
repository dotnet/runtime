//--------------------------------------------------------------------------
//
//		MACLs test
//
//		Runs all the MACLs test cases and reports the results
//
//		Copyright (C) Microsoft Corporation, 2003
//
//--------------------------------------------------------------------------


namespace System.Security.AccessControl.Test
{
	[Flags]
	public enum FlagsForAce : byte
	{
		None                         = 0x00,
		OI                = 0x01,
		CI             = 0x02,
		NP           = 0x04,
		IO                  = 0x08,
		IH                    = 0x10,
		SA             = 0x40,
		FA                 = 0x80,

		InheritanceFlags             = OI | CI | NP | IO,
		AuditFlags                   = SA | FA,
	}
}

