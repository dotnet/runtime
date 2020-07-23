// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test validates the behavior in the presence/absence of the PreserveBaseOverridesAttribute.
// Each test scenario will have 3 layers of type inheritance (T1,T2,T3), and we test the following scenarios:
//
//  Scenario A: Attribute placed T1's MethodDecl (attribute treated as NOP - only applicable to MethodImpls)
//  ==============================================================================
//  Scenario A.1: T2 explicitly overrides T1's, T3 explicitly overrides T1's MethodDecl
//  Scenario A.2: T2 explicitly overrides T1's, T3 explicitly overrides T2's MethodImpl
//  Scenario A.3: T2 explicitly overrides T1's, T3 implicitly overrides T1's MethodDecl
//  Scenario A.4: T2 explicitly overrides T1's, T3 implicitly overrides T2's MethodImpl
//  Scenario A.5: T2 implicitly overrides T1's, T3 explicitly overrides T1's method
//  Scenario A.6: T2 implicitly overrides T1's, T3 explicitly overrides T2's method
//  Scenario A.7: T2 implicitly overrides T1's, T3 implicitly overrides T1/T2's method
//
//  SCENARIO B: T2 has MethodImpl that overrides T1's MethodDecl, WITHOUT any attribute
//  ==============================================================================
//  Scenario B.1: T3 explicitly overrides T1's MethodDecl
//  Scenario B.2: T3 explicitly overrides T2's MethodImpl
//  Scenario B.3: T3 implicitly overrides T1's MethodDecl
//  Scenario B.4: T3 implicitly overrides T2's MethodImpl
//
//  SCENARIO C: T2 has MethodImpl that overrides T1's MethodDecl, WITH the attribute
//  ==============================================================================
//  Scenario C.1: T3 explicitly overrides T1's MethodDecl
//  Scenario C.2: T3 explicitly overrides T2's MethodImpl
//  Scenario C.3: T3 implicitly overrides T1's MethodDecl
//  Scenario C.4: T3 implicitly overrides T2's MethodImpl
//
//  SCENARIO D: T2 implicitly overrides T1's method, WITH the attribute (Attribute treated as NOP)
//  ==============================================================================
//  Scenario D.1: T3 explicitly overrides T1's method
//  Scenario D.2: T3 explicitly overrides T2's method
//  Scenario D.3: T3 implicitly overrides T1/T2's method
//
//  SCENARIO E: T2 implicitly overrides T1's method, WITHOUT the attribute
//  ==============================================================================
//  Scenario E.1: T3 explicitly overrides T1's method
//  Scenario E.2: T3 explicitly overrides T2's method
//  Scenario E.3: T3 implicitly overrides T1/T2's method
//

.assembly extern System.Console { }
.assembly extern System.Runtime { }
.assembly extern mscorlib { }
.assembly AttributeTesting { }


// Scenario A
.namespace A
{
    .class public auto ansi beforefieldinit T1
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        
        .method public hidebysig newslot virtual instance string Func1()
        {
            .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
            ldstr "T1"
            ret
        }
    }
    .class public auto ansi beforefieldinit T2Exp extends A.T1
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig newslot virtual instance string Func2()
        {
            .override method instance string class A.T1::Func1()
            ldstr "T2"
            ret
        }
    }
    .class public auto ansi beforefieldinit T2Imp extends A.T1
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig virtual instance string Func1()
        {
            ldstr "T2"
            ret
        }
    }
    
    //  Scenario A.1: T2 explicitly overrides T1's, T3 explicitly overrides T1's MethodDecl
    .class public auto ansi beforefieldinit T3_1 extends A.T2Exp
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig newslot virtual instance string Func3()
        {
            .override method instance string class A.T1::Func1()
            ldstr "T3"
            ret
        }
    }
    //  Scenario A.2: T2 explicitly overrides T1's, T3 explicitly overrides T2's MethodImpl
    .class public auto ansi beforefieldinit T3_2 extends A.T2Exp
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig newslot virtual instance string Func3()
        {
            .override method instance string class A.T2Exp::Func2()
            ldstr "T3"
            ret
        }
    }
    
    //  Scenario A.3: T2 explicitly overrides T1's, T3 implicitly overrides T1's MethodDecl
    .class public auto ansi beforefieldinit T3_3 extends A.T2Exp
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig virtual instance string Func1()
        {
            ldstr "T3"
            ret
        }
    }
    
    //  Scenario A.4: T2 explicitly overrides T1's, T3 implicitly overrides T2's MethodImpl
    .class public auto ansi beforefieldinit T3_4 extends A.T2Exp
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig virtual instance string Func2()
        {
            ldstr "T3"
            ret
        }
    }
    
    //  Scenario A.5: T2 implicitly overrides T1's, T3 explicitly overrides T1's method
    .class public auto ansi beforefieldinit T3_5 extends A.T2Imp
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig newslot virtual instance string Func3()
        {
            .override method instance string class A.T1::Func1()
            ldstr "T3"
            ret
        }
    }
    
    //  Scenario A.6: T2 implicitly overrides T1's, T3 explicitly overrides T2's method
    .class public auto ansi beforefieldinit T3_6 extends A.T2Imp
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig newslot virtual instance string Func3()
        {
            .override method instance string class A.T2Imp::Func1()
            ldstr "T3"
            ret
        }
    }

    //  Scenario A.7: T2 implicitly overrides T1's, T3 implicitly overrides T1/T2's method
    .class public auto ansi beforefieldinit T3_7 extends A.T2Imp
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig virtual instance string Func1()
        {
            ldstr "T3"
            ret
        }
    }    
}

// Scenario B
.namespace B
{
    .class public auto ansi beforefieldinit T1
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig newslot virtual instance string Func1()
        {
            ldstr "T1"
            ret
        }
    }
    .class public auto ansi beforefieldinit T2 extends B.T1
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig newslot virtual instance string Func2()
        {
            .override method instance string class B.T1::Func1()
            ldstr "T2"
            ret
        }
    }

    //  Scenario B.1: T3 explicitly overrides T1's MethodDecl
    .class public auto ansi beforefieldinit T3_1 extends B.T2
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig newslot virtual instance string Func3()
        {
            .override method instance string class B.T1::Func1()
            ldstr "T3"
            ret
        }
    }

    //  Scenario B.2: T3 explicitly overrides T2's MethodImpl
    .class public auto ansi beforefieldinit T3_2 extends B.T2
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig newslot virtual instance string Func3()
        {
            .override method instance string class B.T2::Func2()
            ldstr "T3"
            ret
        }
    }

    //  Scenario B.3: T3 implicitly overrides T1's MethodDecl
    .class public auto ansi beforefieldinit T3_3 extends B.T2
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig virtual instance string Func1()
        {
            ldstr "T3"
            ret
        }
    }
    
    //  Scenario B.4: T3 implicitly overrides T2's MethodImpl
    .class public auto ansi beforefieldinit T3_4 extends B.T2
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig virtual instance string Func2()
        {
            ldstr "T3"
            ret
        }
    }
}

// Scenario C
.namespace C
{
    .class public auto ansi beforefieldinit T1
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig newslot virtual instance string Func1()
        {
            ldstr "T1"
            ret
        }
    }
    .class public auto ansi beforefieldinit T2 extends C.T1
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig newslot virtual instance string Func2()
        {
            .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
            .override method instance string class C.T1::Func1()
            ldstr "T2"
            ret
        }
    }

    //  Scenario C.1: T3 explicitly overrides T1's MethodDecl
    .class public auto ansi beforefieldinit T3_1 extends C.T2
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig newslot virtual instance string Func3()
        {
            .override method instance string class C.T1::Func1()
            ldstr "T3"
            ret
        }
    }

    //  Scenario C.2: T3 explicitly overrides T2's MethodImpl
    .class public auto ansi beforefieldinit T3_2 extends C.T2
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig newslot virtual instance string Func3()
        {
            .override method instance string class C.T2::Func2()
            ldstr "T3"
            ret
        }
    }

    //  Scenario C.3: T3 implicitly overrides T1's MethodDecl
    .class public auto ansi beforefieldinit T3_3 extends C.T2
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig virtual instance string Func1()
        {
            ldstr "T3"
            ret
        }
    }
    
    //  Scenario C.4: T3 implicitly overrides T2's MethodImpl
    .class public auto ansi beforefieldinit T3_4 extends C.T2
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig virtual instance string Func2()
        {
            ldstr "T3"
            ret
        }
    }
}

// Scenario D
.namespace D
{
    .class public auto ansi beforefieldinit T1
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig newslot virtual instance string Func1()
        {
            ldstr "T1"
            ret
        }
    }
    .class public auto ansi beforefieldinit T2 extends D.T1
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig virtual instance string Func1()
        {
            .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
            ldstr "T2"
            ret
        }
    }

    //  Scenario D.1: T3 explicitly overrides T1's method
    .class public auto ansi beforefieldinit T3_1 extends D.T2
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig newslot virtual instance string Func3()
        {
            .override method instance string class D.T1::Func1()
            ldstr "T3"
            ret
        }
    }

    //  Scenario D.2: T3 explicitly overrides T2's method
    .class public auto ansi beforefieldinit T3_2 extends D.T2
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig newslot virtual instance string Func3()
        {
            .override method instance string class D.T2::Func1()
            ldstr "T3"
            ret
        }
    }

    //  Scenario D.3: T3 implicitly overrides T1/T2's method
    .class public auto ansi beforefieldinit T3_3 extends D.T2
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig virtual instance string Func1()
        {
            ldstr "T3"
            ret
        }
    }
}

// Scenario E
.namespace E
{
    .class public auto ansi beforefieldinit T1
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig newslot virtual instance string Func1()
        {
            ldstr "T1"
            ret
        }
    }
    .class public auto ansi beforefieldinit T2 extends E.T1
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig virtual instance string Func1()
        {
            ldstr "T2"
            ret
        }
    }

    //  Scenario E.1: T3 explicitly overrides T1's method
    .class public auto ansi beforefieldinit T3_1 extends E.T2
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig newslot virtual instance string Func3()
        {
            .override method instance string class E.T1::Func1()
            ldstr "T3"
            ret
        }
    }

    //  Scenario E.2: T3 explicitly overrides T2's method
    .class public auto ansi beforefieldinit T3_2 extends E.T2
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig newslot virtual instance string Func3()
        {
            .override method instance string class E.T2::Func1()
            ldstr "T3"
            ret
        }
    }

    //  Scenario E.3: T3 implicitly overrides T1/T2's method
    .class public auto ansi beforefieldinit T3_3 extends E.T2
    {
        .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
        .method public hidebysig virtual instance string Func1()
        {
            ldstr "T3"
            ret
        }
    }
}

.class public auto ansi beforefieldinit CMain extends [mscorlib]System.Object
{
  .method private hidebysig static bool CheckResult (string expected,string actual) cil managed 
  {
      // Method begins at RVA 0x217c
      // Code size 45 (0x2d)
      .maxstack 8
  
      // Console.WriteLine("EXPECTED: " + expected);
      IL_0000: ldstr "    EXPECTED: "
      IL_0005: ldarg.0
      IL_0006: call string [System.Runtime]System.String::Concat(string, string)
      IL_000b: call void [System.Console]System.Console::WriteLine(string)
      // Console.WriteLine("ACTUAL  : " + actual);
      IL_0010: ldstr "    ACTUAL  : "
      IL_0015: ldarg.1
      IL_0016: call string [System.Runtime]System.String::Concat(string, string)
      IL_001b: call void [System.Console]System.Console::WriteLine(string)
      // Console.WriteLine();
      IL_0020: call void [System.Console]System.Console::WriteLine()
      // return expected == actual;
      IL_0025: ldarg.0
      IL_0026: ldarg.1
      IL_0027: call bool [System.Runtime]System.String::op_Equality(string, string)
      // (no C# code)
      IL_002c: ret
  } // end of method Program::CheckResult

  
  // ===================================================
  
  .method private hidebysig static bool ScenarioA_Test1 () cil managed 
  {
      ldstr "ScenarioA_Test1"
      call void [System.Console]System.Console::WriteLine(string)
      
  	  ldstr "T3"
  	  newobj instance void A.T3_1::.ctor()
  	  callvirt instance string class A.T1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T2"
  	  newobj instance void A.T3_1::.ctor()
  	  callvirt instance string A.T2Exp::Func2()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
      
  	  ldstr "T3"
  	  newobj instance void A.T3_1::.ctor()
  	  callvirt instance string A.T3_1::Func3()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
  
  	  ldc.i4.1
  	  ret
    FAILED:
  	  ldc.i4.0
  	  ret
  }

  .method private hidebysig static bool ScenarioA_Test2 () cil managed 
  {
      ldstr "ScenarioA_Test2"
      call void [System.Console]System.Console::WriteLine(string)

  	  ldstr "T3"
  	  newobj instance void A.T3_2::.ctor()
  	  callvirt instance string class A.T1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T3"
  	  newobj instance void A.T3_2::.ctor()
  	  callvirt instance string A.T2Exp::Func2()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
      
  	  ldstr "T3"
  	  newobj instance void A.T3_2::.ctor()
  	  callvirt instance string A.T3_2::Func3()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
  
  	  ldc.i4.1
  	  ret
    FAILED:
  	  ldc.i4.0
  	  ret
  }

  .method private hidebysig static bool ScenarioA_Test3 () cil managed 
  {
      ldstr "ScenarioA_Test3"
      call void [System.Console]System.Console::WriteLine(string)

  	  ldstr "T3"
  	  newobj instance void A.T3_3::.ctor()
  	  callvirt instance string class A.T1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T2"
  	  newobj instance void A.T3_3::.ctor()
  	  callvirt instance string A.T2Exp::Func2()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
      
  	  ldstr "T3"
  	  newobj instance void A.T3_3::.ctor()
  	  callvirt instance string A.T3_3::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
  
  	  ldc.i4.1
  	  ret
    FAILED:
  	  ldc.i4.0
  	  ret
  }

  .method private hidebysig static bool ScenarioA_Test4 () cil managed 
  {
      ldstr "ScenarioA_Test4"
      call void [System.Console]System.Console::WriteLine(string)

  	  ldstr "T3"
  	  newobj instance void A.T3_4::.ctor()
  	  callvirt instance string class A.T1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T3"
  	  newobj instance void A.T3_4::.ctor()
  	  callvirt instance string A.T2Exp::Func2()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
      
  	  ldstr "T3"
  	  newobj instance void A.T3_4::.ctor()
  	  callvirt instance string A.T3_4::Func2()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
  
  	  ldc.i4.1
  	  ret
    FAILED:
  	  ldc.i4.0
  	  ret
  }

  .method private hidebysig static bool ScenarioA_Test5 () cil managed 
  {
      ldstr "ScenarioA_Test5"
      call void [System.Console]System.Console::WriteLine(string)

  	  ldstr "T3"
  	  newobj instance void A.T3_5::.ctor()
  	  callvirt instance string class A.T1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T3"
  	  newobj instance void A.T3_5::.ctor()
  	  callvirt instance string A.T2Imp::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
      
  	  ldstr "T3"
  	  newobj instance void A.T3_5::.ctor()
  	  callvirt instance string A.T3_5::Func3()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
  
  	  ldc.i4.1
  	  ret
    FAILED:
  	  ldc.i4.0
  	  ret
  }

  .method private hidebysig static bool ScenarioA_Test6 () cil managed 
  {
      ldstr "ScenarioA_Test6"
      call void [System.Console]System.Console::WriteLine(string)
  
  	  ldstr "T3"
  	  newobj instance void A.T3_6::.ctor()
  	  callvirt instance string class A.T1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T3"
  	  newobj instance void A.T3_6::.ctor()
  	  callvirt instance string A.T2Imp::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
      
  	  ldstr "T3"
  	  newobj instance void A.T3_6::.ctor()
  	  callvirt instance string A.T3_6::Func3()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldc.i4.1
  	  ret
    FAILED:
  	  ldc.i4.0
  	  ret
  }

  .method private hidebysig static bool ScenarioA_Test7 () cil managed 
  {
      ldstr "ScenarioA_Test7"
      call void [System.Console]System.Console::WriteLine(string)
  
  	  ldstr "T3"
  	  newobj instance void A.T3_7::.ctor()
  	  callvirt instance string class A.T1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T3"
  	  newobj instance void A.T3_7::.ctor()
  	  callvirt instance string A.T2Imp::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
      
  	  ldstr "T3"
  	  newobj instance void A.T3_7::.ctor()
  	  callvirt instance string A.T3_7::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldc.i4.1
  	  ret
    FAILED:
  	  ldc.i4.0
  	  ret
  }

  // ===================================================

  .method private hidebysig static bool ScenarioB_Test1 () cil managed 
  {
      ldstr "ScenarioB_Test1"
      call void [System.Console]System.Console::WriteLine(string)
  
  	  ldstr "T3"
  	  newobj instance void B.T3_1::.ctor()
  	  callvirt instance string class B.T1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T2"
  	  newobj instance void B.T3_1::.ctor()
  	  callvirt instance string B.T2::Func2()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
      
  	  ldstr "T3"
  	  newobj instance void B.T3_1::.ctor()
  	  callvirt instance string B.T3_1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldc.i4.1
  	  ret
    FAILED:
  	  ldc.i4.0
  	  ret
  }

  .method private hidebysig static bool ScenarioB_Test2 () cil managed 
  {
      ldstr "ScenarioB_Test2"
      call void [System.Console]System.Console::WriteLine(string)

  	  ldstr "T3"
  	  newobj instance void B.T3_2::.ctor()
  	  callvirt instance string class B.T1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T3"
  	  newobj instance void B.T3_2::.ctor()
  	  callvirt instance string B.T2::Func2()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
      
  	  ldstr "T3"
  	  newobj instance void B.T3_2::.ctor()
  	  callvirt instance string B.T3_2::Func2()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldc.i4.1
  	  ret
    FAILED:
  	  ldc.i4.0
  	  ret
  }

  .method private hidebysig static bool ScenarioB_Test3 () cil managed 
  {
      ldstr "ScenarioB_Test3"
      call void [System.Console]System.Console::WriteLine(string)

  	  ldstr "T3"
  	  newobj instance void B.T3_3::.ctor()
  	  callvirt instance string class B.T1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T2"
  	  newobj instance void B.T3_3::.ctor()
  	  callvirt instance string B.T2::Func2()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
      
  	  ldstr "T3"
  	  newobj instance void B.T3_3::.ctor()
  	  callvirt instance string B.T3_3::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldc.i4.1
  	  ret
    FAILED:
  	  ldc.i4.0
  	  ret
  }

  .method private hidebysig static bool ScenarioB_Test4 () cil managed 
  {
      ldstr "ScenarioB_Test4"
      call void [System.Console]System.Console::WriteLine(string)

  	  ldstr "T3"
  	  newobj instance void B.T3_4::.ctor()
  	  callvirt instance string class B.T1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T3"
  	  newobj instance void B.T3_4::.ctor()
  	  callvirt instance string B.T2::Func2()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
      
  	  ldstr "T3"
  	  newobj instance void B.T3_4::.ctor()
  	  callvirt instance string B.T3_4::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T3"
  	  newobj instance void B.T3_4::.ctor()
  	  callvirt instance string B.T3_4::Func2()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldc.i4.1
  	  ret
    FAILED:
  	  ldc.i4.0
  	  ret
  }

  // ===================================================

  .method private hidebysig static bool ScenarioC_Test1 () cil managed 
  {
      ldstr "ScenarioC_Test1"
      call void [System.Console]System.Console::WriteLine(string)
  
  	  ldstr "T3"
  	  newobj instance void C.T3_1::.ctor()
  	  callvirt instance string class C.T1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T3"
  	  newobj instance void C.T3_1::.ctor()
  	  callvirt instance string C.T2::Func2()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
      
  	  ldstr "T3"
  	  newobj instance void C.T3_1::.ctor()
  	  callvirt instance string C.T3_1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldc.i4.1
  	  ret
    FAILED:
  	  ldc.i4.0
  	  ret
  }

  .method private hidebysig static bool ScenarioC_Test2 () cil managed 
  {
      ldstr "ScenarioC_Test2"
      call void [System.Console]System.Console::WriteLine(string)

  	  ldstr "T3"
  	  newobj instance void C.T3_2::.ctor()
  	  callvirt instance string class C.T1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T3"
  	  newobj instance void C.T3_2::.ctor()
  	  callvirt instance string C.T2::Func2()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
      
  	  ldstr "T3"
  	  newobj instance void C.T3_2::.ctor()
  	  callvirt instance string C.T3_2::Func2()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldc.i4.1
  	  ret
    FAILED:
  	  ldc.i4.0
  	  ret
  }

  .method private hidebysig static bool ScenarioC_Test3 () cil managed 
  {
      ldstr "ScenarioC_Test3"
      call void [System.Console]System.Console::WriteLine(string)

  	  ldstr "T3"
  	  newobj instance void C.T3_3::.ctor()
  	  callvirt instance string class C.T1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T2"
  	  newobj instance void C.T3_3::.ctor()
  	  callvirt instance string C.T2::Func2()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
      
  	  ldstr "T3"
  	  newobj instance void C.T3_3::.ctor()
  	  callvirt instance string C.T3_3::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldc.i4.1
  	  ret
    FAILED:
  	  ldc.i4.0
  	  ret
  }

  .method private hidebysig static bool ScenarioC_Test4 () cil managed 
  {
      ldstr "ScenarioC_Test4"
      call void [System.Console]System.Console::WriteLine(string)

  	  ldstr "T3"
  	  newobj instance void C.T3_4::.ctor()
  	  callvirt instance string class C.T1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T3"
  	  newobj instance void C.T3_4::.ctor()
  	  callvirt instance string C.T2::Func2()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
      
  	  ldstr "T3"
  	  newobj instance void C.T3_4::.ctor()
  	  callvirt instance string C.T3_4::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T3"
  	  newobj instance void C.T3_4::.ctor()
  	  callvirt instance string C.T3_4::Func2()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldc.i4.1
  	  ret
    FAILED:
  	  ldc.i4.0
  	  ret
  }

  // ===================================================

  .method private hidebysig static bool ScenarioD_Test1 () cil managed 
  {
      ldstr "ScenarioD_Test1"
      call void [System.Console]System.Console::WriteLine(string)
  
  	  ldstr "T3"
  	  newobj instance void D.T3_1::.ctor()
  	  callvirt instance string class D.T1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T3"
  	  newobj instance void D.T3_1::.ctor()
  	  callvirt instance string D.T2::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
      
  	  ldstr "T3"
  	  newobj instance void D.T3_1::.ctor()
  	  callvirt instance string D.T3_1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T3"
  	  newobj instance void D.T3_1::.ctor()
  	  callvirt instance string D.T3_1::Func3()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldc.i4.1
  	  ret
    FAILED:
  	  ldc.i4.0
  	  ret
  }

  .method private hidebysig static bool ScenarioD_Test2 () cil managed 
  {
      ldstr "ScenarioD_Test2"
      call void [System.Console]System.Console::WriteLine(string)

  	  ldstr "T3"
  	  newobj instance void D.T3_2::.ctor()
  	  callvirt instance string class D.T1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T3"
  	  newobj instance void D.T3_2::.ctor()
  	  callvirt instance string D.T2::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
      
  	  ldstr "T3"
  	  newobj instance void D.T3_2::.ctor()
  	  callvirt instance string D.T3_2::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T3"
  	  newobj instance void D.T3_1::.ctor()
  	  callvirt instance string D.T3_1::Func3()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldc.i4.1
  	  ret
    FAILED:
  	  ldc.i4.0
  	  ret
  }

  .method private hidebysig static bool ScenarioD_Test3 () cil managed 
  {
      ldstr "ScenarioD_Test3"
      call void [System.Console]System.Console::WriteLine(string)

  	  ldstr "T3"
  	  newobj instance void D.T3_3::.ctor()
  	  callvirt instance string class D.T1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T3"
  	  newobj instance void D.T3_3::.ctor()
  	  callvirt instance string D.T2::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
      
  	  ldstr "T3"
  	  newobj instance void D.T3_3::.ctor()
  	  callvirt instance string D.T3_3::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldc.i4.1
  	  ret
    FAILED:
  	  ldc.i4.0
  	  ret
  }

  // ===================================================

  .method private hidebysig static bool ScenarioE_Test1 () cil managed 
  {
      ldstr "ScenarioE_Test1"
      call void [System.Console]System.Console::WriteLine(string)
  
  	  ldstr "T3"
  	  newobj instance void E.T3_1::.ctor()
  	  callvirt instance string class E.T1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T3"
  	  newobj instance void E.T3_1::.ctor()
  	  callvirt instance string E.T2::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
      
  	  ldstr "T3"
  	  newobj instance void E.T3_1::.ctor()
  	  callvirt instance string E.T3_1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T3"
  	  newobj instance void E.T3_1::.ctor()
  	  callvirt instance string E.T3_1::Func3()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldc.i4.1
  	  ret
    FAILED:
  	  ldc.i4.0
  	  ret
  }

  .method private hidebysig static bool ScenarioE_Test2 () cil managed 
  {
      ldstr "ScenarioE_Test2"
      call void [System.Console]System.Console::WriteLine(string)

  	  ldstr "T3"
  	  newobj instance void E.T3_2::.ctor()
  	  callvirt instance string class E.T1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T3"
  	  newobj instance void E.T3_2::.ctor()
  	  callvirt instance string E.T2::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
      
  	  ldstr "T3"
  	  newobj instance void E.T3_2::.ctor()
  	  callvirt instance string E.T3_2::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T3"
  	  newobj instance void E.T3_1::.ctor()
  	  callvirt instance string E.T3_1::Func3()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldc.i4.1
  	  ret
    FAILED:
  	  ldc.i4.0
  	  ret
  }

  .method private hidebysig static bool ScenarioE_Test3 () cil managed 
  {
      ldstr "ScenarioE_Test3"
      call void [System.Console]System.Console::WriteLine(string)

  	  ldstr "T3"
  	  newobj instance void E.T3_3::.ctor()
  	  callvirt instance string class E.T1::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldstr "T3"
  	  newobj instance void E.T3_3::.ctor()
  	  callvirt instance string E.T2::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED
      
  	  ldstr "T3"
  	  newobj instance void E.T3_3::.ctor()
  	  callvirt instance string E.T3_3::Func1()
  	  call bool CMain::CheckResult(string, string)
  	  brfalse.s FAILED

  	  ldc.i4.1
  	  ret
    FAILED:
  	  ldc.i4.0
  	  ret
  }

  .method public hidebysig static int32  Main( string[] args) cil managed
  {
    .entrypoint
    .maxstack  2
    .locals init ( bool result )
    
    ldc.i4.1
    stloc.0
    
    // ==================================================== //
    
    ldloc.0
    call bool CMain::ScenarioA_Test1()
    and
    stloc.0
    
    ldloc.0
    call bool CMain::ScenarioA_Test2()
    and
    stloc.0

    ldloc.0
    call bool CMain::ScenarioA_Test3()
    and
    stloc.0

    ldloc.0
    call bool CMain::ScenarioA_Test4()
    and
    stloc.0

    ldloc.0
    call bool CMain::ScenarioA_Test5()
    and
    stloc.0
    
    ldloc.0
    call bool CMain::ScenarioA_Test6()
    and
    stloc.0

    ldloc.0
    call bool CMain::ScenarioA_Test7()
    and
    stloc.0

    // ==================================================== //
    
    ldloc.0
    call bool CMain::ScenarioB_Test1()
    and
    stloc.0
    
    ldloc.0
    call bool CMain::ScenarioB_Test2()
    and
    stloc.0

    ldloc.0
    call bool CMain::ScenarioB_Test3()
    and
    stloc.0

    ldloc.0
    call bool CMain::ScenarioB_Test4()
    and
    stloc.0

    // ==================================================== //
    
    ldloc.0
    call bool CMain::ScenarioC_Test1()
    and
    stloc.0
    
    ldloc.0
    call bool CMain::ScenarioC_Test2()
    and
    stloc.0

    ldloc.0
    call bool CMain::ScenarioC_Test3()
    and
    stloc.0

    ldloc.0
    call bool CMain::ScenarioC_Test4()
    and
    stloc.0

    // ==================================================== //
    
    ldloc.0
    call bool CMain::ScenarioD_Test1()
    and
    stloc.0
    
    ldloc.0
    call bool CMain::ScenarioD_Test2()
    and
    stloc.0

    ldloc.0
    call bool CMain::ScenarioD_Test3()
    and
    stloc.0

    // ==================================================== //
    
    ldloc.0
    call bool CMain::ScenarioE_Test1()
    and
    stloc.0
    
    ldloc.0
    call bool CMain::ScenarioE_Test2()
    and
    stloc.0

    ldloc.0
    call bool CMain::ScenarioE_Test3()
    and
    stloc.0

    // ==================================================== //

    DONE:
    ldloc.0
    brtrue.s   PASS

    ldstr      "Test FAILED"
    call       void [System.Console]System.Console::WriteLine(string)
    ldc.i4.s   101
    ret
    
    PASS:
    ldstr      "Test PASSED"
    call       void [System.Console]System.Console::WriteLine(string)
    ldc.i4.s   100
    ret

    ldc.i4.s   100
    ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
