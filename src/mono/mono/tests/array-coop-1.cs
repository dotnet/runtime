/*
array-coop-*.cs

Author:
    Jay Krell (jaykrell@microsoft.com)

Copyright 2018 Microsoft
Licensed under the MIT license. See LICENSE file in the project root for full license information.

This gets coverage of metadata/icall.c changes for coop conversion.
Some of these functions are inlined by the JIT, so reflection is used.

Using delegates/ldftn might also be possible -- but we cannot form delegates
to internal non-public functions.

As well, there is printf in the implementation to verify coverage.

System.Array.GetValue (int index) => ves_icall_System_Array_GetValueImpl
int System.Rank => GetRank () => ves_icall_System_Array_GetRank
System.Array.Clear (array, index, length) => ves_icall_System_Array_ClearInternal
System.Array.SetGenericValueImpl<T> => ves_icall_System_Array_SetGenericValueImpl
System.Array.GetGenericValueImpl<T> => ves_icall_System_Array_GetGenericValueImpl

See
https://docs.microsoft.com/en-us/dotnet/api/system.reflection.methodinfo.makegenericmethod?view=netframework-4.7.1.
https://msdn.microsoft.com/en-us/library/system.array.rank(v=vs.110).aspx
https://stackoverflow.com/questions/1067312/how-to-use-methodinfo-invoke-to-set-property-value
*/

using System;
using System.Reflection;
