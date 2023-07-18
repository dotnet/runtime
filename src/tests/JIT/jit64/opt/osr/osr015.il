// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'osr015' {}


.class public auto ansi beforefieldinit test
       extends [mscorlib]System.Object
{

  .method public static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .locals init (int32)
	
	.try
	{
		call void test::test_001()
		ldstr "Fail: test_001"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_001
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_001
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_001"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_001
	}
TRY_END_001:

	.try
	{
		call void test::test_002()
		ldstr "Fail: test_002"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_002
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_002
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_002"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_002
	}
TRY_END_002:

	.try
	{
		call void test::test_003()
		ldstr "Fail: test_003"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_003
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_003
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_003"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_003
	}
TRY_END_003:

	.try
	{
		call void test::test_004()
		ldstr "Fail: test_004"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_004
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_004
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_004"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_004
	}
TRY_END_004:

	.try
	{
		call void test::test_005()
		ldstr "Fail: test_005"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_005
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_005
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_005"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_005
	}
TRY_END_005:

	.try
	{
		call void test::test_006()
		ldstr "Fail: test_006"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_006
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_006
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_006"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_006
	}
TRY_END_006:

	.try
	{
		call void test::test_007()
		ldstr "Fail: test_007"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_007
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_007
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_007"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_007
	}
TRY_END_007:

	.try
	{
		call void test::test_008()
		ldstr "Fail: test_008"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_008
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_008
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_008"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_008
	}
TRY_END_008:

	.try
	{
		call void test::test_009()
		ldstr "Fail: test_009"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_009
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_009
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_009"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_009
	}
TRY_END_009:

	.try
	{
		call void test::test_010()
		ldstr "Fail: test_010"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_010
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_010
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_010"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_010
	}
TRY_END_010:



// int64

	.try
	{
		call void test::test_011()
		ldstr "Fail: test_011"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_011
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_011
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_011"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_011
	}
TRY_END_011:

	.try
	{
		call void test::test_012()
		ldstr "Fail: test_012"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_012
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_012
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_012"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_012
	}
TRY_END_012:

	.try
	{
		call void test::test_013()
		ldstr "Fail: test_013"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_013
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_013
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_013"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_013
	}
TRY_END_013:

	.try
	{
		call void test::test_014()
		ldstr "Fail: test_014"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_014
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_014
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_014"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_014
	}
TRY_END_014:

	.try
	{
		call void test::test_015()
		ldstr "Fail: test_015"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_015
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_015
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_015"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_015
	}
TRY_END_015:

	.try
	{
		call void test::test_016()
		ldstr "Fail: test_016"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_016
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_016
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_016"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_016
	}
TRY_END_016:

	.try
	{
		call void test::test_017()
		ldstr "Fail: test_017"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_017
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_017
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_017"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_017
	}
TRY_END_017:

	.try
	{
		call void test::test_018()
		ldstr "Fail: test_018"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_018
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_018
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_018"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_018
	}
TRY_END_018:

	.try
	{
		call void test::test_019()
		ldstr "Fail: test_019"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_019
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_019
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_019"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_019
	}
TRY_END_019:

	.try
	{
		call void test::test_020()
		ldstr "Fail: test_020"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_020
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_020
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_020"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_020
	}
TRY_END_020:




// uint32

	.try
	{
		call void test::test_021()
		ldstr "Fail: test_021"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_021
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_021
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_021"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_021
	}
TRY_END_021:

	.try
	{
		call void test::test_022()
		ldstr "Fail: test_022"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_022
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_022
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_022"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_022
	}
TRY_END_022:

	.try
	{
		call void test::test_023()
		ldstr "Fail: test_023"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_023
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_023
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_023"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_023
	}
TRY_END_023:

	.try
	{
		call void test::test_024()
		ldstr "Fail: test_024"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_024
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_024
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_024"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_024
	}
TRY_END_024:

	.try
	{
		call void test::test_025()
		ldstr "Fail: test_025"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_025
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_025
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_025"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_025
	}
TRY_END_025:

	.try
	{
		call void test::test_026()
		ldstr "Fail: test_026"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_026
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_026
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_026"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_026
	}
TRY_END_026:

	.try
	{
		call void test::test_027()
		ldstr "Fail: test_027"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_027
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_027
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_027"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_027
	}
TRY_END_027:

	.try
	{
		call void test::test_028()
		ldstr "Fail: test_028"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_028
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_028
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_028"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_028
	}
TRY_END_028:

	.try
	{
		call void test::test_029()
		ldstr "Fail: test_029"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_029
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_029
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_029"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_029
	}
TRY_END_029:

	.try
	{
		call void test::test_030()
		ldstr "Fail: test_030"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_030
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_030
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_030"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_030
	}
TRY_END_030:







	.try
	{
		call void test::test_001b()
		ldstr "Fail: test_001b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b001
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b001
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_001b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b001
	}
TRY_END_b001:

	.try
	{
		call void test::test_002b()
		ldstr "Fail: test_002b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b002
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b002
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_002b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b002
	}
TRY_END_b002:

	.try
	{
		call void test::test_003b()
		ldstr "Fail: test_003b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b003
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b003
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_003b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b003
	}
TRY_END_b003:

	.try
	{
		call void test::test_004b()
		ldstr "Fail: test_004b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b004
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b004
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_004b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b004
	}
TRY_END_b004:

	.try
	{
		call void test::test_005b()
		ldstr "Fail: test_005b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b005
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b005
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_005b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b005
	}
TRY_END_b005:

	.try
	{
		call void test::test_006b()
		ldstr "Fail: test_006b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b006
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b006
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_006b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b006
	}
TRY_END_b006:

	.try
	{
		call void test::test_007b()
		ldstr "Fail: test_007b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b007
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b007
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_007b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b007
	}
TRY_END_b007:

	.try
	{
		call void test::test_008b()
		ldstr "Fail: test_008b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b008
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b008
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_008b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b008
	}
TRY_END_b008:

	.try
	{
		call void test::test_009b()
		ldstr "Fail: test_009b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b009
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b009
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_009b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b009
	}
TRY_END_b009:

	.try
	{
		call void test::test_010b()
		ldstr "Fail: test_010b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b010
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b010
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_010b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b010
	}
TRY_END_b010:



// int64

	.try
	{
		call void test::test_011b()
		ldstr "Fail: test_011b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b011
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b011
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_011b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b011
	}
TRY_END_b011:

	.try
	{
		call void test::test_012b()
		ldstr "Fail: test_012b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b012
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b012
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_012b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b012
	}
TRY_END_b012:

	.try
	{
		call void test::test_013b()
		ldstr "Fail: test_013b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b013
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b013
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_013b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b013
	}
TRY_END_b013:

	.try
	{
		call void test::test_014b()
		ldstr "Fail: test_014b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b014
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b014
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_014b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b014
	}
TRY_END_b014:

	.try
	{
		call void test::test_015b()
		ldstr "Fail: test_015b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b015
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b015
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_015b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b015
	}
TRY_END_b015:

	.try
	{
		call void test::test_016b()
		ldstr "Fail: test_016b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b016
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b016
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_016b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b016
	}
TRY_END_b016:

	.try
	{
		call void test::test_017b()
		ldstr "Fail: test_017b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b017
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b017
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_017b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b017
	}
TRY_END_b017:

	.try
	{
		call void test::test_018b()
		ldstr "Fail: test_018b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b018
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b018
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_018b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b018
	}
TRY_END_b018:

	.try
	{
		call void test::test_019b()
		ldstr "Fail: test_019b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b019
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b019
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_019b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b019
	}
TRY_END_b019:

	.try
	{
		call void test::test_020b()
		ldstr "Fail: test_020b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b020
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b020
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_020b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b020
	}
TRY_END_b020:




// uint32

	.try
	{
		call void test::test_021b()
		ldstr "Fail: test_021b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b021
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b021
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_021b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b021
	}
TRY_END_b021:

	.try
	{
		call void test::test_022b()
		ldstr "Fail: test_022b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b022
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b022
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_022b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b022
	}
TRY_END_b022:

	.try
	{
		call void test::test_023b()
		ldstr "Fail: test_023b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b023
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b023
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_023b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b023
	}
TRY_END_b023:

	.try
	{
		call void test::test_024b()
		ldstr "Fail: test_024b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b024
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b024
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_024b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b024
	}
TRY_END_b024:

	.try
	{
		call void test::test_025b()
		ldstr "Fail: test_025b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b025
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b025
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_025b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b025
	}
TRY_END_b025:

	.try
	{
		call void test::test_026b()
		ldstr "Fail: test_026b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b026
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b026
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_026b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b026
	}
TRY_END_b026:

	.try
	{
		call void test::test_027b()
		ldstr "Fail: test_027b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b027
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b027
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_027b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b027
	}
TRY_END_b027:

	.try
	{
		call void test::test_028b()
		ldstr "Fail: test_028b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b028
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b028
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_028b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b028
	}
TRY_END_b028:

	.try
	{
		call void test::test_029b()
		ldstr "Fail: test_029b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b029
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b029
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_029b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b029
	}
TRY_END_b029:

	.try
	{
		call void test::test_030b()
		ldstr "Fail: test_030b"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_b030
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_b030
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_030b"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_b030
	}
TRY_END_b030:


	.try
	{
		call void test::test_001c()
		ldstr "Fail: test_001c"
		call void [System.Console]System.Console::WriteLine(string)
		ldc.i4 1
		stloc 0
		leave TRY_END_c001
	}
	catch [mscorlib]System.IndexOutOfRangeException
	{
		leave TRY_END_c001
	}
	catch [mscorlib]System.Exception
	{
		ldstr "Fail: test_001c"
		call void [System.Console]System.Console::WriteLine(string)
		call void [System.Console]System.Console::WriteLine(object)
		ldc.i4 1
		stloc 0
		leave TRY_END_c001
	}
TRY_END_c001:




ldloc 0
ldc.i4 100
add
		
	ret
  }


  .method public static void test_001() cil managed
  {
// Check for exception
// use int32
// Exceed array bounds with greater than or equals

	.locals init (int32[], int32)

	
	ldc.i4 100
	newarr int32
	stloc 0


FOR_START:
	ldloc 1
	ldc.i4 101
	bge FOR_END


	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4


	ldloc 1
	ldc.i4 1
	add
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }





  .method public static void test_001b() cil managed
  {
// Check for exception
// use int32
// C# like loop
// Exceed array bounds with less than

	.locals init (int32[], int32)

	
	ldc.i4 100
	newarr int32
	stloc 0



FOR_INIT:
	ldc.i4 0
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4
FOR_INC:
	ldloc 1
	ldc.i4 1
	add
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i4 101
	blt FOR_CODE




	ret   
		
  }



  .method public static void test_002() cil managed
  {
// Check for exception
// use int32
// Exceed array bounds with greater than


	.locals init (int32[], int32)

	
	ldc.i4 100
	newarr int32
	stloc 0


FOR_START:
	ldloc 1
	ldc.i4 100
	bgt FOR_END


	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4


	ldloc 1
	ldc.i4 1
	add
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }


  .method public static void test_002b() cil managed
  {
// Check for exception
// use int32
// C# like loop
// Exceed array bounds with less than equals


	.locals init (int32[], int32)

	
	ldc.i4 100
	newarr int32
	stloc 0

FOR_INIT:
	ldc.i4 0
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4
FOR_INC:
	ldloc 1
	ldc.i4 1
	add
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i4 100
	ble FOR_CODE
	ret   
		
  }

  .method public static void test_003() cil managed
  {
// Check for exception
// use int32
// Exceed array bounds with less than equals


	.locals init (int32[], int32)

	
	ldc.i4 100
	newarr int32
	stloc 0

	ldc.i4 99
	stloc 1


FOR_START:
	ldloc 1
	ldc.i4 -2
	ble FOR_END


	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4


	ldloc 1
	ldc.i4 1
	sub
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }


  .method public static void test_003b() cil managed
  {
// Check for exception
// use int32
// C# like loop
// Exceed array bounds with greater than


	.locals init (int32[], int32)

	
	ldc.i4 100
	newarr int32
	stloc 0

FOR_INIT:
	ldc.i4 99
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4
FOR_INC:
	ldloc 1
	ldc.i4 1
	sub
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i4 -2
	bgt FOR_CODE



	ret   
		
  }

  .method public static void test_004() cil managed
  {
// Check for exception
// use int32
// Exceed array bounds with less than


	.locals init (int32[], int32)

	
	ldc.i4 100
	newarr int32
	stloc 0

	ldc.i4 99
	stloc 1

FOR_START:
	ldloc 1
	ldc.i4 -1
	blt FOR_END


	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4


	ldloc 1
	ldc.i4 1
	sub
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }




  .method public static void test_004b() cil managed
  {
// Check for exception
// use int32
// C# like loop
// Exceed array bounds with greater than equals


	.locals init (int32[], int32)

	
	ldc.i4 100
	newarr int32
	stloc 0


FOR_INIT:
	ldc.i4 99
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4
FOR_INC:
	ldloc 1
	ldc.i4 1
	sub
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i4 -1
	bge FOR_CODE


	ret   
		
  }


  .method public static void test_005() cil managed
  {
// Check for exception
// use int32
// Exceed array bounds with greater than or equals
// Steping 4

	.locals init (int32[], int32)

	
	ldc.i4 100
	newarr int32
	stloc 0


FOR_START:
	ldloc 1
	ldc.i4 101
	bge FOR_END


	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4


	ldloc 1
	ldc.i4 4
	add
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }

  .method public static void test_005b() cil managed
  {
// Check for exception
// use int32
// C# like loop
// Exceed array bounds with less than
// Steping 4

	.locals init (int32[], int32)

	
	ldc.i4 100
	newarr int32
	stloc 0


FOR_INIT:
	ldc.i4 0
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4
FOR_INC:
	ldloc 1
	ldc.i4 4
	add
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i4 101
	blt FOR_CODE

	ret   
		
  }




  .method public static void test_006() cil managed
  {
// Check for exception
// use int32
// Exceed array bounds with greater than
// Steping 4


	.locals init (int32[], int32)

	
	ldc.i4 100
	newarr int32
	stloc 0


FOR_START:
	ldloc 1
	ldc.i4 100
	bgt FOR_END


	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4


	ldloc 1
	ldc.i4 4
	add
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }





  .method public static void test_006b() cil managed
  {
// Check for exception
// use int32
// C# like loop
// Exceed array bounds with less than equals
// Steping 4


	.locals init (int32[], int32)

	
	ldc.i4 100
	newarr int32
	stloc 0


FOR_INIT:
	ldc.i4 0
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4
FOR_INC:
	ldloc 1
	ldc.i4 4
	add
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i4 100
	ble FOR_CODE

	ret	
  }



  .method public static void test_007() cil managed
  {
// Check for exception
// use int32
// Exceed array bounds with less than equals
// Steping 4


	.locals init (int32[], int32)

	
	ldc.i4 100
	newarr int32
	stloc 0

	ldc.i4 99
	stloc 1


FOR_START:
	ldloc 1
	ldc.i4 -2
	ble FOR_END


	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4


	ldloc 1
	ldc.i4 4
	sub
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }


  .method public static void test_007b() cil managed
  {
// Check for exception
// use int32
// C# like loop
// Exceed array bounds with greater than
// Steping 4


	.locals init (int32[], int32)

	
	ldc.i4 100
	newarr int32
	stloc 0

FOR_INIT:
	ldc.i4 99
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4
FOR_INC:
	ldloc 1
	ldc.i4 4
	sub
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i4 -2
	bgt FOR_CODE


	ret   
		
  }


  .method public static void test_008() cil managed
  {
// Check for exception
// use int32
// Exceed array bounds with less than
// Steping 4


	.locals init (int32[], int32)

	
	ldc.i4 100
	newarr int32
	stloc 0

	ldc.i4 99
	stloc 1

FOR_START:
	ldloc 1
	ldc.i4 -1
	blt FOR_END


	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4


	ldloc 1
	ldc.i4 4
	sub
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }




  .method public static void test_008b() cil managed
  {
// Check for exception
// use int32
// C# like loop
// Exceed array bounds with greater than equals
// Steping 4


	.locals init (int32[], int32)

	
	ldc.i4 100
	newarr int32
	stloc 0

FOR_INIT:
	ldc.i4 99
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4
FOR_INC:
	ldloc 1
	ldc.i4 4
	sub
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i4 -1
	bge FOR_CODE

	ret   
		
  }

  .method public static void test_009() cil managed
  {
// Check for exception
// use int32
// take address of local

	.locals init (int32[], int32, int32*)
	
	ldc.i4 100
	newarr int32
	stloc 0

	ldloca 1
	stloc 2


FOR_START:
	ldloc 1
	ldc.i4 100
	bge FOR_END


	ldloc 2
	ldc.i4 100
	stind.i4
	

	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4


	ldloc 1
	ldc.i4 1
	add
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }


  .method public static void test_009b() cil managed
  {
// Check for exception
// use int32
// C# like loop
// take address of local

	.locals init (int32[], int32, int32*)
	
	ldc.i4 100
	newarr int32
	stloc 0

	ldloca 1
	stloc 2

FOR_INIT:
	ldc.i4 0
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 2
	ldc.i4 100
	stind.i4

	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4
FOR_INC:
	ldloc 1
	ldc.i4 1
	add
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i4 100
	blt FOR_CODE


	ret   
		
  }




  .method public static void test_010() cil managed
  {
// Check for exception
// use int32
// over write array

	.locals init (int32[], int32)

	
	ldc.i4 100
	newarr int32
	stloc 0


FOR_START:
	ldloc 1
	ldc.i4 100
	bge FOR_END


	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4

	ldloca 0
	ldc.i4 10
	newarr int32
	stind.ref


	ldloc 1
	ldc.i4 1
	add
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }





  .method public static void test_010b() cil managed
  {
// Check for exception
// use int32
// C# like loop
// over write array

	.locals init (int32[], int32)

	
	ldc.i4 100
	newarr int32
	stloc 0


FOR_INIT:
	ldc.i4 0
	stloc 1
	br FOR_TEST
FOR_CODE:

	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4

	ldloca 0
	ldc.i4 10
	newarr int32
	stind.ref

FOR_INC:
	ldloc 1
	ldc.i4 1
	add
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i4 100
	blt FOR_CODE


	ret   
		
  }








  .method public static void test_011() cil managed
  {
// Check for exception
// use int64
// Exceed array bounds with greater than or equals

	.locals init (int64[], int64)

	
	ldc.i8 100
	conv.i
	newarr int64
	stloc 0


FOR_START:
	ldloc 1
	ldc.i8 101
	bge FOR_END


	ldloc 0
	ldloc 1
	conv.i
	ldloc 1
	stelem.i8


	ldloc 1
	ldc.i8 1
	add
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }





  .method public static void test_011b() cil managed
  {
// Check for exception
// use int64
// C# like loop
// Exceed array bounds with less than

	.locals init (int64[], int64)

	
	ldc.i8 100
	conv.i
	newarr int64
	stloc 0



FOR_INIT:
	ldc.i8 0
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 0
	ldloc 1
	conv.i
	ldloc 1
	stelem.i8
FOR_INC:
	ldloc 1
	ldc.i8 1
	add
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i8 101
	blt FOR_CODE




	ret   
		
  }



  .method public static void test_012() cil managed
  {
// Check for exception
// use int64
// Exceed array bounds with greater than


	.locals init (int64[], int64)

	
	ldc.i8 100
	conv.i
	newarr int64
	stloc 0


FOR_START:
	ldloc 1
	ldc.i8 100
	bgt FOR_END


	ldloc 0
	ldloc 1
	conv.i
	ldloc 1
	stelem.i8


	ldloc 1
	ldc.i8 1
	add
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }


  .method public static void test_012b() cil managed
  {
// Check for exception
// use int64
// C# like loop
// Exceed array bounds with less than equals


	.locals init (int64[], int64)

	
	ldc.i8 100
	conv.i
	newarr int64
	stloc 0

FOR_INIT:
	ldc.i8 0
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 0
	ldloc 1
	conv.i
	ldloc 1
	stelem.i8
FOR_INC:
	ldloc 1
	ldc.i8 1
	add
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i8 100
	ble FOR_CODE
	ret   
		
  }

  .method public static void test_013() cil managed
  {
// Check for exception
// use int64
// Exceed array bounds with less than equals


	.locals init (int64[], int64)

	
	ldc.i8 100
	conv.i
	newarr int64
	stloc 0

	ldc.i8 99
	stloc 1


FOR_START:
	ldloc 1
	ldc.i8 -2
	ble FOR_END


	ldloc 0
	ldloc 1
	conv.i
	ldloc 1
	stelem.i8


	ldloc 1
	ldc.i8 1
	sub
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }


  .method public static void test_013b() cil managed
  {
// Check for exception
// use int64
// C# like loop
// Exceed array bounds with greater than


	.locals init (int64[], int64)

	
	ldc.i8 100
	conv.i
	newarr int64
	stloc 0

FOR_INIT:
	ldc.i8 99
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 0
	ldloc 1
	conv.i
	ldloc 1
	stelem.i8
FOR_INC:
	ldloc 1
	ldc.i8 1
	sub
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i8 -2
	bgt FOR_CODE



	ret   
		
  }

  .method public static void test_014() cil managed
  {
// Check for exception
// use int64
// Exceed array bounds with less than


	.locals init (int64[], int64)

	
	ldc.i8 100
	conv.i
	newarr int64
	stloc 0

	ldc.i8 99
	stloc 1

FOR_START:
	ldloc 1
	ldc.i8 -1
	blt FOR_END


	ldloc 0
	ldloc 1
	conv.i
	ldloc 1
	stelem.i8


	ldloc 1
	ldc.i8 1
	sub
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }




  .method public static void test_014b() cil managed
  {
// Check for exception
// use int64
// C# like loop
// Exceed array bounds with greater than equals


	.locals init (int64[], int64)

	
	ldc.i8 100
	conv.i
	newarr int64
	stloc 0


FOR_INIT:
	ldc.i8 99
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 0
	ldloc 1
	conv.i
	ldloc 1
	stelem.i8
FOR_INC:
	ldloc 1
	ldc.i8 1
	sub
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i8 -1
	bge FOR_CODE


	ret   
		
  }


  .method public static void test_015() cil managed
  {
// Check for exception
// use int64
// Exceed array bounds with greater than or equals
// Steping 4

	.locals init (int64[], int64)

	
	ldc.i8 100
	conv.i
	newarr int64
	stloc 0


FOR_START:
	ldloc 1
	ldc.i8 101
	bge FOR_END


	ldloc 0
	ldloc 1
	conv.i
	ldloc 1
	stelem.i8


	ldloc 1
	ldc.i8 4
	add
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }

  .method public static void test_015b() cil managed
  {
// Check for exception
// use int64
// C# like loop
// Exceed array bounds with less than
// Steping 4

	.locals init (int64[], int64)

	
	ldc.i8 100
	conv.i
	newarr int64
	stloc 0


FOR_INIT:
	ldc.i8 0
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 0
	ldloc 1
	conv.i
	ldloc 1
	stelem.i8
FOR_INC:
	ldloc 1
	ldc.i8 4
	add
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i8 101
	blt FOR_CODE

	ret   
		
  }




  .method public static void test_016() cil managed
  {
// Check for exception
// use int64
// Exceed array bounds with greater than
// Steping 4


	.locals init (int64[], int64)

	
	ldc.i8 100
	conv.i
	newarr int64
	stloc 0


FOR_START:
	ldloc 1
	ldc.i8 100
	bgt FOR_END


	ldloc 0
	ldloc 1
	conv.i
	ldloc 1
	stelem.i8


	ldloc 1
	ldc.i8 4
	add
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }





  .method public static void test_016b() cil managed
  {
// Check for exception
// use int64
// C# like loop
// Exceed array bounds with less than equals
// Steping 4


	.locals init (int64[], int64)

	
	ldc.i8 100
	conv.i
	newarr int64
	stloc 0


FOR_INIT:
	ldc.i8 0
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 0
	ldloc 1
	conv.i
	ldloc 1
	stelem.i8
FOR_INC:
	ldloc 1
	ldc.i8 4
	add
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i8 100
	ble FOR_CODE

	ret	
  }



  .method public static void test_017() cil managed
  {
// Check for exception
// use int64
// Exceed array bounds with less than equals
// Steping 4


	.locals init (int64[], int64)

	
	ldc.i8 100
	conv.i
	newarr int64
	stloc 0

	ldc.i8 99
	stloc 1


FOR_START:
	ldloc 1
	ldc.i8 -2
	ble FOR_END


	ldloc 0
	ldloc 1
	conv.i
	ldloc 1
	stelem.i8


	ldloc 1
	ldc.i8 4
	sub
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }


  .method public static void test_017b() cil managed
  {
// Check for exception
// use int64
// C# like loop
// Exceed array bounds with greater than
// Steping 4


	.locals init (int64[], int64)

	
	ldc.i8 100
	conv.i
	newarr int64
	stloc 0

FOR_INIT:
	ldc.i8 99
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 0
	ldloc 1
	conv.i
	ldloc 1
	stelem.i8
FOR_INC:
	ldloc 1
	ldc.i8 4
	sub
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i8 -2
	bgt FOR_CODE


	ret   
		
  }


  .method public static void test_018() cil managed
  {
// Check for exception
// use int64
// Exceed array bounds with less than
// Steping 4


	.locals init (int64[], int64)

	
	ldc.i8 100
	conv.i
	newarr int64
	stloc 0

	ldc.i8 99
	stloc 1

FOR_START:
	ldloc 1
	ldc.i8 -1
	blt FOR_END


	ldloc 0
	ldloc 1
	conv.i
	ldloc 1
	stelem.i8


	ldloc 1
	ldc.i8 4
	sub
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }




  .method public static void test_018b() cil managed
  {
// Check for exception
// use int64
// C# like loop
// Exceed array bounds with greater than equals
// Steping 4


	.locals init (int64[], int64)

	
	ldc.i8 100
	conv.i
	newarr int64
	stloc 0

FOR_INIT:
	ldc.i8 99
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 0
	ldloc 1
	conv.i
	ldloc 1
	stelem.i8
FOR_INC:
	ldloc 1
	ldc.i8 4
	sub
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i8 -1
	bge FOR_CODE

	ret   
		
  }

  .method public static void test_019() cil managed
  {
// Check for exception
// use int64
// take address of local

	.locals init (int64[], int64, int64*)
	
	ldc.i8 100
	conv.i
	newarr int64
	stloc 0

	ldloca 1
	stloc 2


FOR_START:
	ldloc 1
	ldc.i8 100
	bge FOR_END


	ldloc 2
	ldc.i8 100
	stind.i8
	

	ldloc 0
	ldloc 1
	conv.i
	ldloc 1
	stelem.i8


	ldloc 1
	ldc.i8 1
	add
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }


  .method public static void test_019b() cil managed
  {
// Check for exception
// use int64
// C# like loop
// take address of local

	.locals init (int64[], int64, int64*)
	
	ldc.i8 100
	conv.i
	newarr int64
	stloc 0

	ldloca 1
	stloc 2

FOR_INIT:
	ldc.i8 0
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 2
	ldc.i8 100
	stind.i8

	ldloc 0
	ldloc 1
	conv.i
	ldloc 1
	stelem.i8
FOR_INC:
	ldloc 1
	ldc.i8 1
	add
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i8 100
	blt FOR_CODE


	ret   
		
  }




  .method public static void test_020() cil managed
  {
// Check for exception
// use int64
// over write array

	.locals init (int64[], int64)

	
	ldc.i8 100
	conv.i
	newarr int64
	stloc 0


FOR_START:
	ldloc 1
	ldc.i8 100
	bge FOR_END


	ldloc 0
	ldloc 1
	conv.i
	ldloc 1
	stelem.i8

	ldloca 0
	ldc.i8 10
	conv.i
	newarr int64
	stind.ref


	ldloc 1
	ldc.i8 1
	add
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }





  .method public static void test_020b() cil managed
  {
// Check for exception
// use int64
// C# like loop
// over write array

	.locals init (int64[], int64)

	
	ldc.i8 100
	conv.i
	newarr int64
	stloc 0


FOR_INIT:
	ldc.i8 0
	stloc 1
	br FOR_TEST
FOR_CODE:

	ldloc 0
	ldloc 1
	conv.i
	ldloc 1
	stelem.i8

	ldloca 0
	ldc.i8 10
	conv.i
	newarr int64
	stind.ref

FOR_INC:
	ldloc 1
	ldc.i8 1
	add
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i8 100
	blt FOR_CODE


	ret   
		
  }




  .method public static void test_021() cil managed
  {
// Check for exception
// use uint32
// Exceed array bounds with greater than or equals

	.locals init (uint32[], uint32)

	
	ldc.i4 100
	newarr uint32
	stloc 0


FOR_START:
	ldloc 1
	ldc.i4 101
	bge FOR_END


	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4


	ldloc 1
	ldc.i4 1
	add
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }





  .method public static void test_021b() cil managed
  {
// Check for exception
// use uint32
// C# like loop
// Exceed array bounds with less than

	.locals init (uint32[], uint32)

	
	ldc.i4 100
	newarr uint32
	stloc 0



FOR_INIT:
	ldc.i4 0
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4
FOR_INC:
	ldloc 1
	ldc.i4 1
	add
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i4 101
	blt FOR_CODE




	ret   
		
  }



  .method public static void test_022() cil managed
  {
// Check for exception
// use uint32
// Exceed array bounds with greater than


	.locals init (uint32[], uint32)

	
	ldc.i4 100
	newarr uint32
	stloc 0


FOR_START:
	ldloc 1
	ldc.i4 100
	bgt FOR_END


	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4


	ldloc 1
	ldc.i4 1
	add
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }


  .method public static void test_022b() cil managed
  {
// Check for exception
// use uint32
// C# like loop
// Exceed array bounds with less than equals


	.locals init (uint32[], uint32)

	
	ldc.i4 100
	newarr uint32
	stloc 0

FOR_INIT:
	ldc.i4 0
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4
FOR_INC:
	ldloc 1
	ldc.i4 1
	add
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i4 100
	ble FOR_CODE
	ret   
		
  }

  .method public static void test_023() cil managed
  {
// Check for exception
// use uint32
// Exceed array bounds with less than equals


	.locals init (uint32[], uint32)

	
	ldc.i4 100
	newarr uint32
	stloc 0

	ldc.i4 99
	stloc 1


FOR_START:
	ldloc 1
	ldc.i4 -2
	ble FOR_END


	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4


	ldloc 1
	ldc.i4 1
	sub
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }


  .method public static void test_023b() cil managed
  {
// Check for exception
// use uint32
// C# like loop
// Exceed array bounds with greater than


	.locals init (uint32[], uint32)

	
	ldc.i4 100
	newarr uint32
	stloc 0

FOR_INIT:
	ldc.i4 99
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4
FOR_INC:
	ldloc 1
	ldc.i4 1
	sub
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i4 -2
	bgt FOR_CODE



	ret   
		
  }

  .method public static void test_024() cil managed
  {
// Check for exception
// use uint32
// Exceed array bounds with less than


	.locals init (uint32[], uint32)

	
	ldc.i4 100
	newarr uint32
	stloc 0

	ldc.i4 99
	stloc 1

FOR_START:
	ldloc 1
	ldc.i4 -1
	blt FOR_END


	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4


	ldloc 1
	ldc.i4 1
	sub
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }




  .method public static void test_024b() cil managed
  {
// Check for exception
// use uint32
// C# like loop
// Exceed array bounds with greater than equals


	.locals init (uint32[], uint32)

	
	ldc.i4 100
	newarr uint32
	stloc 0


FOR_INIT:
	ldc.i4 99
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4
FOR_INC:
	ldloc 1
	ldc.i4 1
	sub
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i4 -1
	bge FOR_CODE


	ret   
		
  }


  .method public static void test_025() cil managed
  {
// Check for exception
// use uint32
// Exceed array bounds with greater than or equals
// Steping 4

	.locals init (uint32[], uint32)

	
	ldc.i4 100
	newarr uint32
	stloc 0


FOR_START:
	ldloc 1
	ldc.i4 101
	bge FOR_END


	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4


	ldloc 1
	ldc.i4 4
	add
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }

  .method public static void test_025b() cil managed
  {
// Check for exception
// use uint32
// C# like loop
// Exceed array bounds with less than
// Steping 4

	.locals init (uint32[], uint32)

	
	ldc.i4 100
	newarr uint32
	stloc 0


FOR_INIT:
	ldc.i4 0
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4
FOR_INC:
	ldloc 1
	ldc.i4 4
	add
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i4 101
	blt FOR_CODE

	ret   
		
  }




  .method public static void test_026() cil managed
  {
// Check for exception
// use uint32
// Exceed array bounds with greater than
// Steping 4


	.locals init (uint32[], uint32)

	
	ldc.i4 100
	newarr uint32
	stloc 0


FOR_START:
	ldloc 1
	ldc.i4 100
	bgt FOR_END


	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4


	ldloc 1
	ldc.i4 4
	add
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }





  .method public static void test_026b() cil managed
  {
// Check for exception
// use uint32
// C# like loop
// Exceed array bounds with less than equals
// Steping 4


	.locals init (uint32[], uint32)

	
	ldc.i4 100
	newarr uint32
	stloc 0


FOR_INIT:
	ldc.i4 0
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4
FOR_INC:
	ldloc 1
	ldc.i4 4
	add
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i4 100
	ble FOR_CODE

	ret	
  }



  .method public static void test_027() cil managed
  {
// Check for exception
// use uint32
// Exceed array bounds with less than equals
// Steping 4


	.locals init (uint32[], uint32)

	
	ldc.i4 100
	newarr uint32
	stloc 0

	ldc.i4 99
	stloc 1


FOR_START:
	ldloc 1
	ldc.i4 -2
	ble FOR_END


	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4


	ldloc 1
	ldc.i4 4
	sub
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }


  .method public static void test_027b() cil managed
  {
// Check for exception
// use uint32
// C# like loop
// Exceed array bounds with greater than
// Steping 4


	.locals init (uint32[], uint32)

	
	ldc.i4 100
	newarr uint32
	stloc 0

FOR_INIT:
	ldc.i4 99
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4
FOR_INC:
	ldloc 1
	ldc.i4 4
	sub
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i4 -2
	bgt FOR_CODE


	ret   
		
  }


  .method public static void test_028() cil managed
  {
// Check for exception
// use uint32
// Exceed array bounds with less than
// Steping 4


	.locals init (uint32[], uint32)

	
	ldc.i4 100
	newarr uint32
	stloc 0

	ldc.i4 99
	stloc 1

FOR_START:
	ldloc 1
	ldc.i4 -1
	blt FOR_END


	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4


	ldloc 1
	ldc.i4 4
	sub
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }




  .method public static void test_028b() cil managed
  {
// Check for exception
// use uint32
// C# like loop
// Exceed array bounds with greater than equals
// Steping 4


	.locals init (uint32[], uint32)

	
	ldc.i4 100
	newarr uint32
	stloc 0

FOR_INIT:
	ldc.i4 99
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4
FOR_INC:
	ldloc 1
	ldc.i4 4
	sub
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i4 -1
	bge FOR_CODE

	ret   
		
  }

  .method public static void test_029() cil managed
  {
// Check for exception
// use uint32
// take address of local

	.locals init (uint32[], uint32, uint32*)
	
	ldc.i4 100
	newarr uint32
	stloc 0

	ldloca 1
	stloc 2


FOR_START:
	ldloc 1
	ldc.i4 100
	bge FOR_END


	ldloc 2
	ldc.i4 100
	stind.i4
	

	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4


	ldloc 1
	ldc.i4 1
	add
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }


  .method public static void test_029b() cil managed
  {
// Check for exception
// use uint32
// C# like loop
// take address of local

	.locals init (uint32[], uint32, uint32*)
	
	ldc.i4 100
	newarr uint32
	stloc 0

	ldloca 1
	stloc 2

FOR_INIT:
	ldc.i4 0
	stloc 1
	br FOR_TEST
FOR_CODE:
	ldloc 2
	ldc.i4 100
	stind.i4

	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4
FOR_INC:
	ldloc 1
	ldc.i4 1
	add
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i4 100
	blt FOR_CODE


	ret   
		
  }




  .method public static void test_030() cil managed
  {
// Check for exception
// use uint32
// over write array

	.locals init (uint32[], uint32)

	
	ldc.i4 100
	newarr uint32
	stloc 0


FOR_START:
	ldloc 1
	ldc.i4 100
	bge FOR_END


	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4

	ldloca 0
	ldc.i4 10
	newarr uint32
	stind.ref


	ldloc 1
	ldc.i4 1
	add
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }





  .method public static void test_030b() cil managed
  {
// Check for exception
// use uint32
// C# like loop
// over write array

	.locals init (uint32[], uint32)

	
	ldc.i4 100
	newarr uint32
	stloc 0


FOR_INIT:
	ldc.i4 0
	stloc 1
	br FOR_TEST
FOR_CODE:

	ldloc 0
	ldloc 1
	ldloc 1
	stelem.i4

	ldloca 0
	ldc.i4 10
	newarr uint32
	stind.ref

FOR_INC:
	ldloc 1
	ldc.i4 1
	add
	stloc 1

FOR_TEST:
	ldloc 1
	ldc.i4 100
	blt FOR_CODE


	ret   
		
  }




  .method public static void test_001c() cil managed
  {
// Check for exception
// use int32
// Exceed array bounds with greater than
// with a back branch that goes to a try region
// so Jit can't clone the loop check and transofm it
// to a postcondition loop.


	.locals init (int32[], int32)

	
	ldc.i4 100
	newarr int32
	stloc 0


FOR_START:
	ldloc 1
	ldc.i4 100
	bgt FOR_END

	.try
	{
        ldloc 0
        ldloc 1
        ldloc 1
        stelem.i4
        leave TRY_END_a
    }
    catch [mscorlib]System.NullReferenceException
    {
        leave TRY_END_a
    }
    TRY_END_a:

	ldloc 1
	ldc.i4 1
	add
	stloc 1
	br FOR_START

FOR_END:

	ret   
		
  }




}
