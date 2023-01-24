// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime {}
.assembly extern System.Console {}
.assembly GenericCatchInterface {}

// IL in this file is based on this C# program with a few IL modifications:

// static class Program
// {
//     private static int retCode = 0;
//     static int Main()
//     {
//         try
//         {
//             try
//             {
//                 throw new MyException();
//             }
//             catch (MyException) // IL edit: Change to catch (IMyInterface)
//             {
//                 Console.WriteLine("FAIL");
//                 retCode++;
//             }
//         }
//         catch
//         {
//             Console.WriteLine("PASS");
//             retCode += 50;
//         }
//
//         GenericCatch<MyException>(); // IL edit: Change to GenericCatch<IMyInterface>()
//
//         return retCode;
//     }
//
//     [MethodImpl(MethodImplOptions.NoInlining)]
//     static void GenericCatch<T>() where T : Exception // IL edit: Remove constraint
//     {
//         try
//         {
//             try
//             {
//                 throw new MyException();
//             }
//             catch (T)
//             {
//                 Console.WriteLine("FAIL");
//                 retCode++;
//             }
//         }
//         catch
//         {
//             Console.WriteLine("PASS");
//             retCode += 50;
//         }
//     }
// }
// interface IMyInterface {}
// class MyException : Exception, IMyInterface {}


.class private abstract auto ansi sealed beforefieldinit GenericCatchInterfaceProgram
       extends [System.Runtime]System.Object
{
  // 
  .field private static int32 retCode

  .method private hidebysig static int32 Main() cil managed
  {
    .entrypoint
    .maxstack  2
    IL_0000:  newobj     instance void MyException::.ctor()
    IL_0005:  throw

    IL_0006:  pop
    IL_0007:  ldstr      "FAIL"
    IL_000c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0011:  ldsfld     int32 GenericCatchInterfaceProgram::retCode
    IL_0016:  ldc.i4.1
    IL_0017:  add
    IL_0018:  stsfld     int32 GenericCatchInterfaceProgram::retCode
    IL_001d:  leave.s    IL_001f

    IL_001f:  leave.s    IL_003b

    IL_0021:  pop
    IL_0022:  ldstr      "PASS"
    IL_0027:  call       void [System.Console]System.Console::WriteLine(string)
    IL_002c:  ldsfld     int32 GenericCatchInterfaceProgram::retCode
    IL_0031:  ldc.i4.s   50
    IL_0033:  add
    IL_0034:  stsfld     int32 GenericCatchInterfaceProgram::retCode
    IL_0039:  leave.s    IL_003b

    IL_003b:  call       void GenericCatchInterfaceProgram::GenericCatch<class IMyInterface>()
    IL_0040:  ldsfld     int32 GenericCatchInterfaceProgram::retCode
    IL_0045:  ret
    IL_0046:  
    .try IL_0000 to IL_0006 catch IMyInterface handler IL_0006 to IL_001f
    .try IL_0000 to IL_0021 catch [System.Runtime]System.Object handler IL_0021 to IL_003b
  }

  .method private hidebysig static void  GenericCatch<([System.Runtime]System.Object) T>() cil managed noinlining
  {
    .maxstack  2
    IL_0000:  newobj     instance void MyException::.ctor()
    IL_0005:  throw

    IL_0006:  pop
    IL_0007:  ldstr      "FAIL"
    IL_000c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0011:  ldsfld     int32 GenericCatchInterfaceProgram::retCode
    IL_0016:  ldc.i4.1
    IL_0017:  add
    IL_0018:  stsfld     int32 GenericCatchInterfaceProgram::retCode
    IL_001d:  leave.s    IL_001f

    IL_001f:  leave.s    IL_003b

    IL_0021:  pop
    IL_0022:  ldstr      "PASS"
    IL_0027:  call       void [System.Console]System.Console::WriteLine(string)
    IL_002c:  ldsfld     int32 GenericCatchInterfaceProgram::retCode
    IL_0031:  ldc.i4.s   50
    IL_0033:  add
    IL_0034:  stsfld     int32 GenericCatchInterfaceProgram::retCode
    IL_0039:  leave.s    IL_003b

    IL_003b:  ret
    IL_003c:  
    .try IL_0000 to IL_0006 catch !!T handler IL_0006 to IL_001f
    .try IL_0000 to IL_0021 catch [System.Runtime]System.Object handler IL_0021 to IL_003b
  }
}

.class interface private abstract auto ansi IMyInterface {}

.class private auto ansi beforefieldinit MyException
       extends [System.Runtime]System.Exception
       implements IMyInterface
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Exception::.ctor()
    IL_0006:  ret
  }
}