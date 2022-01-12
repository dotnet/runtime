// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Runtime.Extensions { auto }
.assembly extern mscorlib { auto }
.assembly extern Microsoft.VisualBasic { auto }

.assembly 'b1904'
{
}
.assembly extern xunit.core {}
.module 'b1904.exe'
.class private auto ansi b1904 extends ['mscorlib']System.Object
{
  .field private static int32 Count
  .method public static void RunTest() il managed forwardref
  {
    .maxstack  33
    .locals init (int32[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,] V_0,
             int32[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,] V_1,
             int32[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,] V_2)
    IL_0000:  ldc.i4.s   101
    IL_0002:  stsfld     int32 b1904::Count
    IL_0007:  ldc.i4.0
    IL_0008:  ldc.i4.0
    IL_0009:  ldc.i4.0
    IL_000a:  ldc.i4.0
    IL_000b:  ldc.i4.0
    IL_000c:  ldc.i4.0
    IL_000d:  ldc.i4.0
    IL_000e:  ldc.i4.0
    IL_000f:  ldc.i4.0
    IL_0010:  ldc.i4.0
    IL_0011:  ldc.i4.0
    IL_0012:  ldc.i4.0
    IL_0013:  ldc.i4.0
    IL_0014:  ldc.i4.0
    IL_0015:  ldc.i4.0
    IL_0016:  ldc.i4.0
    IL_0017:  ldc.i4.0
    IL_0018:  ldc.i4.0
    IL_0019:  ldc.i4.0
    IL_001a:  ldc.i4.0
    IL_001b:  ldc.i4.0
    IL_001c:  ldc.i4.0
    IL_001d:  ldc.i4.0
    IL_001e:  ldc.i4.0
    IL_001f:  ldc.i4.0
    IL_0020:  ldc.i4.0
    IL_0021:  ldc.i4.0
    IL_0022:  ldc.i4.0
    IL_0023:  ldc.i4.0
    IL_0024:  ldc.i4.0
    IL_0025:  ldc.i4.0
    IL_0026:  newobj     instance void int32[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]::.ctor(int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32)
    IL_002b:  stloc.0
    IL_002c:  ldc.i4.0
    IL_002d:  ldc.i4.0
    IL_002e:  ldc.i4.0
    IL_002f:  ldc.i4.0
    IL_0030:  ldc.i4.0
    IL_0031:  ldc.i4.0
    IL_0032:  ldc.i4.0
    IL_0033:  ldc.i4.0
    IL_0034:  ldc.i4.0
    IL_0035:  ldc.i4.0
    IL_0036:  ldc.i4.0
    IL_0037:  ldc.i4.0
    IL_0038:  ldc.i4.0
    IL_0039:  ldc.i4.0
    IL_003a:  ldc.i4.0
    IL_003b:  ldc.i4.0
    IL_003c:  ldc.i4.0
    IL_003d:  ldc.i4.0
    IL_003e:  ldc.i4.0
    IL_003f:  ldc.i4.0
    IL_0040:  ldc.i4.0
    IL_0041:  ldc.i4.0
    IL_0042:  ldc.i4.0
    IL_0043:  ldc.i4.0
    IL_0044:  ldc.i4.0
    IL_0045:  ldc.i4.0
    IL_0046:  ldc.i4.0
    IL_0047:  ldc.i4.0
    IL_0048:  ldc.i4.0
    IL_0049:  ldc.i4.0
    IL_004a:  ldc.i4.0
    IL_004b:  ldc.i4.0
    IL_004c:  newobj     instance void int32[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]::.ctor(int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32,
                                                                                     int32)
    IL_0051:  stloc.1
    IL_0052:  ldc.i4.0
    IL_0053:  ldc.i4.0
    IL_0054:  ldc.i4.0
    IL_0055:  ldc.i4.0
    IL_0056:  ldc.i4.0
    IL_0057:  ldc.i4.0
    IL_0058:  ldc.i4.0
    IL_0059:  ldc.i4.0
    IL_005a:  ldc.i4.0
    IL_005b:  ldc.i4.0
    IL_005c:  ldc.i4.0
    IL_005d:  ldc.i4.0
    IL_005e:  ldc.i4.0
    IL_005f:  ldc.i4.0
    IL_0060:  ldc.i4.0
    IL_0061:  ldc.i4.0
    IL_0062:  ldc.i4.0
    IL_0063:  ldc.i4.0
    IL_0064:  ldc.i4.0
    IL_0065:  ldc.i4.0
    IL_0066:  ldc.i4.0
    IL_0067:  ldc.i4.0
    IL_0068:  ldc.i4.0
    IL_0069:  ldc.i4.0
    IL_006a:  ldc.i4.0
    IL_006b:  ldc.i4.0
    IL_006c:  ldc.i4.0
    IL_006d:  ldc.i4.0
    IL_006e:  ldc.i4.0
    IL_006f:  ldc.i4.0
    IL_0070:  ldc.i4.0
    IL_0071:  ldc.i4.0
    IL_0072:  ldc.i4.0
    IL_0073:  newobj     instance void int32[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]::.ctor(int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32,
                                                                                      int32)
    IL_0078:  stloc.2
    IL_0079:  ret
    IL_007a:
  }

  .method public static void Main() il managed forwardref
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  2
    .locals init (int32 V_0,
             int32 V_1,
             class ['mscorlib']System.Exception V_2)
    IL_0000:  ldc.i4.s   100
    IL_0002:  stsfld     int32 b1904::Count
    .try{
        IL_0010:  call       void b1904::RunTest()
          leave.s IL_0083
    }
    catch [mscorlib]System.TypeLoadException
    {
    pop
        IL_0029:  ldstr      "Caught TypeLoadException exception: "
    IL_002e:  ldsfld     int32 b1904::Count
    IL_0033:  call       class System.String [mscorlib]System.Convert::ToString(int32)
    IL_0038:  call       class System.String ['mscorlib']System.String::Concat(class System.String,
                                                                               class System.String)
    IL_003d:  call       void ['mscorlib']System.Console::WriteLine(class System.String)
    IL_0042:  ldsfld     int32 b1904::Count
    IL_0047:  call       void ['System.Runtime.Extensions']System.Environment::set_ExitCode(int32)
    IL_004c:  leave.s    IL_0083
    }
    IL_0083:  call       int32 ['System.Runtime.Extensions']System.Environment::get_ExitCode()
              call       void ['System.Runtime.Extensions']System.Environment::Exit(int32)
              ret
  }

}
