// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}

.assembly extern mscorlib { }
.assembly 'tail_pinvoke' { }
.namespace JitTest
{
  .class public auto ansi beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method private hidebysig static pinvokeimpl("PInvokeExampleNative" ansi winapi)
            native uint CreatePopupMenu() cil managed preservesig
    {
    }
    .method private hidebysig static pinvokeimpl("PInvokeExampleNative" ansi winapi)
            bool  DestroyMenu(native uint hMenu) cil managed preservesig
    {
    }
    .method private hidebysig static pinvokeimpl("PInvokeExampleNative" ansi winapi)
            bool  AppendMenu(native uint hMenu,
                             unsigned int32 uFlags,
                             unsigned int32 uID,
                             string item) cil managed preservesig
    {
    }
    .method private hidebysig static pinvokeimpl("PInvokeExampleNative" ansi winapi)
            int32  GetMenuString(native uint  hMenu,
                                 unsigned int32 uIDItem,
                                 class [mscorlib]System.Text.StringBuilder data,
                                 int32 nMaxCount,
                                 unsigned int32 uFlag) cil managed preservesig
    {
    }

    .method private hidebysig static native uint __CreatePopupMenu() cil managed
    {
    	tail. call      native uint JitTest.Test::CreatePopupMenu()
    	ret
    }
    .method private hidebysig static bool  __DestroyMenu(native uint hMenu) cil managed
    {
    	ldarg.0
    	ldftn  bool JitTest.Test::DestroyMenu(native uint)    	
		tail. calli       bool(native uint)
		ret
    }
    .method private hidebysig static bool  __AppendMenu(native uint hMenu,
                             unsigned int32 uFlags,
                             unsigned int32 uID,
                             string item) cil managed
    {
   		.locals (bool)
    	.try {
	    	ldarg.0
	    	ldarg.1
	    	ldarg.2
	    	ldarg.3
	        call       bool JitTest.Test::AppendMenu(native uint,
	                                                           unsigned int32,
	                                                           unsigned int32,
	                                                           string)
			stloc.0
			leave.s IL_END
        }
        catch [mscorlib]System.Exception
        {
        	ldc.i4.0
        	stloc.0
        	leave.s IL_END
        }
    IL_END:
        ldloc.0
    	ret
    }
    .method private hidebysig static int32  __GetMenuString(native uint hMenu,
                                 unsigned int32 uIDItem,
                                 class [mscorlib]System.Text.StringBuilder data,
                                 int32 nMaxCount,
                                 unsigned int32 uFlag) cil managed
    {
    	ldarga.s 0
    	pop
        jmp   int32 JitTest.Test::GetMenuString(native uint,
                                               unsigned int32,
                                               class [mscorlib]System.Text.StringBuilder,
                                               int32,
                                               unsigned int32)
    }

    .method public hidebysig static int32 Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  5
      .locals (native uint V_0,
               unsigned int32 V_1,
               class [mscorlib]System.Text.StringBuilder V_2,
               int32 V_3)
      IL_0000:  call       native uint JitTest.Test::__CreatePopupMenu()
      IL_0005:  stloc.0
      IL_0006:  ldloc.0
      IL_0007:  brtrue.s   IL_001b

      IL_0009:  ldstr      "CreatePopupMenu failed"
      IL_000e:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0013:  ldc.i4.s   101
      IL_0015:  stloc.3
      IL_0016:  br         IL_00af

      .try
      {
        IL_001b:  ldc.i4.0
        IL_001c:  stloc.1
        IL_001d:  br.s       IL_003c

        IL_001f:  ldloc.0
        IL_0020:  ldc.i4.0
        IL_0021:  ldloc.1
        IL_0022:  ldc.i4.1
        IL_0023:  add
        IL_0024:  ldloca.s   V_1
        IL_0026:  call       instance string [mscorlib]System.UInt32::ToString()
        IL_002b:  call       bool JitTest.Test::__AppendMenu(native uint,
                                                           unsigned int32,
                                                           unsigned int32,
                                                           string)
        IL_0030:  brtrue.s   IL_0038

        IL_0032:  newobj     instance void [mscorlib]System.Exception::.ctor()
        IL_0037:  throw

        IL_0038:  ldloc.1
        IL_0039:  ldc.i4.1
        IL_003a:  add
        IL_003b:  stloc.1
        IL_003c:  ldloc.1
        IL_003d:  ldc.i4.s   30
        IL_003f:  blt.un.s   IL_001f

        IL_0041:  ldloc.1
        IL_0042:  ldc.i4.1
        IL_0043:  sub
        IL_0044:  stloc.1
        IL_0045:  br.s       IL_008a

        IL_0047:  ldc.i4     0x3e8
        IL_004c:  newobj     instance void [mscorlib]System.Text.StringBuilder::.ctor(int32)
        IL_0051:  stloc.2
        IL_0052:  ldloc.0
        IL_0053:  ldloc.1
        IL_0054:  ldloc.2
        IL_0055:  ldc.i4     0x3e7
        IL_005a:  ldc.i4     0x400
        IL_005f:  call       int32 JitTest.Test::__GetMenuString(native uint,
                                                               unsigned int32,
                                                               class [mscorlib]System.Text.StringBuilder,
                                                               int32,
                                                               unsigned int32)
        IL_0064:  brtrue.s   IL_006c

        IL_0066:  newobj     instance void [mscorlib]System.Exception::.ctor()
        IL_006b:  throw

        IL_006c:  ldloc.2
        IL_006d:  callvirt   instance string [mscorlib]System.Text.StringBuilder::ToString()
        IL_0072:  ldloca.s   V_1
        IL_0074:  call       instance string [mscorlib]System.UInt32::ToString()
        IL_0079:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                         string)
        IL_007e:  brfalse.s  IL_0086

        IL_0080:  newobj     instance void [mscorlib]System.Exception::.ctor()
        IL_0085:  throw

        IL_0086:  ldloc.1
        IL_0087:  ldc.i4.1
        IL_0088:  sub
        IL_0089:  stloc.1
        IL_008a:  ldloc.1
        IL_008b:  ldc.i4.s   30
        IL_008d:  blt.un.s   IL_0047

        IL_008f:  leave.s    IL_00a0

      }  // end .try
      finally
      {
        IL_0091:  ldloc.0
        IL_0092:  call       bool JitTest.Test::__DestroyMenu(native uint)
        IL_0097:  brtrue.s   IL_009f

        IL_0099:  newobj     instance void [mscorlib]System.Exception::.ctor()
        IL_009e:  throw

        IL_009f:  endfinally
      }  // end handler
      IL_00a0:  ldstr      "=== PASSED ==="
      IL_00a5:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00aa:  ldc.i4.s   100
      IL_00ac:  stloc.3
      IL_00ad:  br.s       IL_00af

      IL_00af:  ldloc.3
      IL_00b0:  ret
    } // end of method Test::Main

    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test::.ctor

  } // end of class Test

} // end of namespace JitTest
