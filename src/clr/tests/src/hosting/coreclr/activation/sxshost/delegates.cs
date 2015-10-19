using System;
using System.Security;
using System.Runtime.InteropServices;

[SecuritySafeCritical]
public delegate int ManagedDelegate(int level, int stackId, int maxStackHeight);

[SecuritySafeCritical]
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate int CdeclManagedDelegate(int level, int stackId, int maxStackHeight);

[SecuritySafeCritical]
public delegate int MainDelegate();

[SecuritySafeCritical]
public delegate void BlockDelegate();

[SecuritySafeCritical]
public delegate void PrepareStackDelegate(string arg, string interleaver, int id, int totalRts, int stackHeight);

[SecuritySafeCritical]
public delegate int BuildStackDelegate(int stackId);


