// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Testing inliner on a small func that contains ldvirtftn and calli
.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'ldvirtftncalli'
{
}
.class private auto ansi beforefieldinit A
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance int32 f1() 
  {
    ldc.i4.1
    ret
  }

  .method public hidebysig newslot virtual 
          instance int16 f2() 
  {
    ldc.i4.1
    ret
  }
  .method public hidebysig newslot virtual 
          instance int64 f3() 
  {
    ldc.i4.1
    conv.i8
    ret
  }

  .method public hidebysig newslot virtual 
          instance unsigned int8 f4() 
  {
    ldc.i4.1
    ret
  }

  .method public hidebysig newslot virtual 
          instance float32 f5() 
  {
    ldc.r4     1.
    ret
  }

  .method public hidebysig newslot virtual 
          instance float64 f6() 
  {
    ldc.r8     1.
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void .ctor() 
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A::.ctor

} // end of class A

.class private auto ansi beforefieldinit B
       extends A
{
  .method public hidebysig virtual instance int32
          f1() 
  {
    ldc.i4.s   100
    ret
  }

  .method public hidebysig virtual instance int16
          f2() 
  {
    ldc.i4.s   100
    ret
  }

  .method public hidebysig virtual instance int64
          f3() 
  {
    ldc.i4.s   100
    conv.i8
    ret
  }

  .method public hidebysig virtual instance unsigned int8
          f4() 
  {
    ldc.i4.s   100
    ret
  }

  .method public hidebysig virtual instance float32
          f5() 
  {
    ldc.r4     100.
    ret
  }

  .method public hidebysig virtual instance float64
          f6() 
  {
    ldc.r8     100.
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void .ctor() 
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void A::.ctor()
    IL_0006:  ret
  } // end of method B::.ctor

} // end of class B

.class public auto ansi beforefieldinit Test_ldvirtftncalli
       extends [mscorlib]System.Object
{
  .field public static class B b1
  .method public hidebysig static int32 f1a() 
  {
    
    ldsfld     class B Test_ldvirtftncalli::b1
    dup
    ldvirtftn   instance int32 A::f1()
    calli instance int32 ()
    ret
  }

  .method public hidebysig static int32  f1b(class B b) 
  {
    
    ldarg.0
    dup
    ldvirtftn   instance int32 A::f1()
    calli instance int32 ()
    ret
  }

  .method public hidebysig static int32 f1c() 
  {
    
    newobj     instance void B::.ctor()
    dup
    ldvirtftn   instance int32 A::f1()
    calli instance int32 ()
    ret
  }

  .method public hidebysig static int16 f2a() 
  {
    
    ldsfld     class B Test_ldvirtftncalli::b1
    dup
    ldvirtftn   instance int16 A::f2()
    calli instance int16 ()
    ret
  }

  .method public hidebysig static int16  f2b(class B b) 
  {
    
    ldarg.0
    dup
    ldvirtftn   instance int16 A::f2()
    calli instance int16 ()
    ret
  }

  .method public hidebysig static int16 f2c() 
  {
    
    newobj     instance void B::.ctor()
    dup
    ldvirtftn   instance int16 A::f2()
    calli instance int16 ()
    ret
  }

  .method public hidebysig static int64 f3a() 
  {
    
    ldsfld     class B Test_ldvirtftncalli::b1
    dup
    ldvirtftn   instance int64 A::f3()
    calli instance int64 ()
    ret
  }

  .method public hidebysig static int64  f3b(class B b) 
  {
    
    ldarg.0
    dup
    ldvirtftn   instance int64 A::f3()
    calli instance int64 ()
    ret
  }

  .method public hidebysig static int64 f3c() 
  {
    
    newobj     instance void B::.ctor()
    dup
    ldvirtftn   instance int64 A::f3()
    calli instance int64 ()
    ret
  }

  .method public hidebysig static unsigned int8
          f4a() 
  {
    
    ldsfld     class B Test_ldvirtftncalli::b1
    dup
    ldvirtftn   instance unsigned int8 A::f4()
    calli instance unsigned int8 ()
    ret
  }

  .method public hidebysig static unsigned int8 
          f4b(class B b) 
  {
    
    ldarg.0
    dup
    ldvirtftn   instance unsigned int8 A::f4()
    calli instance unsigned int8 ()
    ret
  }

  .method public hidebysig static unsigned int8
          f4c() 
  {
    
    newobj     instance void B::.ctor()
    dup
    ldvirtftn   instance unsigned int8 A::f4()
    calli instance unsigned int8 ()
    ret
  }

  .method public hidebysig static float32
          f5a() 
  {
    
    ldsfld     class B Test_ldvirtftncalli::b1
    dup
    ldvirtftn   instance float32 A::f5()
    calli instance float32 ()
    ret
  }

  .method public hidebysig static float32 
          f5b(class B b) 
  {
    
    ldarg.0
    dup
    ldvirtftn   instance float32 A::f5()
    calli instance float32 ()
    ret  
  }

  .method public hidebysig static float32
          f5c() 
  {
    
    newobj     instance void B::.ctor()
    dup
    ldvirtftn   instance float32 A::f5()
    calli instance float32 ()
    ret
  }

  .method public hidebysig static float64
          f6a() 
  {
    
    ldsfld     class B Test_ldvirtftncalli::b1
    dup
    ldvirtftn   instance float64 A::f6()
    calli instance float64 ()
    ret
  }

  .method public hidebysig static float64 
          f6b(class B b) 
  {
    
    ldarg.0
    dup
    ldvirtftn   instance float64 A::f6()
    calli instance float64 ()
    ret
  }
  
  .method public hidebysig static float64
          f6c() 
  {
    
    newobj     instance void B::.ctor()
    dup
    ldvirtftn   instance float64 A::f6()
    calli instance float64 ()
    ret
  }

  .method public hidebysig static int32 Main() 
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       279 (0x117)
    .maxstack  20
    .locals (bool V_0,
             class B V_1,
             int32 V_2)
    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  call       int32 Test_ldvirtftncalli::f1a()
    IL_0007:  ldc.i4.s   100
    IL_0009:  beq.s      IL_000d

    IL_000b:  ldc.i4.0
    IL_000c:  stloc.0
    IL_000d:  newobj     instance void B::.ctor()
    IL_0012:  stloc.1
    IL_0013:  ldloc.1
    IL_0014:  call       int32 Test_ldvirtftncalli::f1b(class B)
    IL_0019:  ldc.i4.s   100
    IL_001b:  beq.s      IL_001f

    IL_001d:  ldc.i4.0
    IL_001e:  stloc.0
    IL_001f:  call       int32 Test_ldvirtftncalli::f1c()
    IL_0024:  ldc.i4.s   100
    IL_0026:  beq.s      IL_002a

    IL_0028:  ldc.i4.0
    IL_0029:  stloc.0
    IL_002a:  call       int16 Test_ldvirtftncalli::f2a()
    IL_002f:  ldc.i4.s   100
    IL_0031:  beq.s      IL_0035

    IL_0033:  ldc.i4.0
    IL_0034:  stloc.0
    IL_0035:  ldloc.1
    IL_0036:  call       int16 Test_ldvirtftncalli::f2b(class B)
    IL_003b:  ldc.i4.s   100
    IL_003d:  beq.s      IL_0041

    IL_003f:  ldc.i4.0
    IL_0040:  stloc.0
    IL_0041:  call       int16 Test_ldvirtftncalli::f2c()
    IL_0046:  ldc.i4.s   100
    IL_0048:  beq.s      IL_004c

    IL_004a:  ldc.i4.0
    IL_004b:  stloc.0
    IL_004c:  call       int64 Test_ldvirtftncalli::f3a()
    IL_0051:  ldc.i4.s   100
    IL_0053:  conv.i8
    IL_0054:  beq.s      IL_0058

    IL_0056:  ldc.i4.0
    IL_0057:  stloc.0
    IL_0058:  ldloc.1
    IL_0059:  call       int64 Test_ldvirtftncalli::f3b(class B)
    IL_005e:  ldc.i4.s   100
    IL_0060:  conv.i8
    IL_0061:  beq.s      IL_0065

    IL_0063:  ldc.i4.0
    IL_0064:  stloc.0
    IL_0065:  call       int64 Test_ldvirtftncalli::f3c()
    IL_006a:  ldc.i4.s   100
    IL_006c:  conv.i8
    IL_006d:  beq.s      IL_0071

    IL_006f:  ldc.i4.0
    IL_0070:  stloc.0
    IL_0071:  call       unsigned int8 Test_ldvirtftncalli::f4a()
    IL_0076:  ldc.i4.s   100
    IL_0078:  beq.s      IL_007c

    IL_007a:  ldc.i4.0
    IL_007b:  stloc.0
    IL_007c:  ldloc.1
    IL_007d:  call       unsigned int8 Test_ldvirtftncalli::f4b(class B)
    IL_0082:  ldc.i4.s   100
    IL_0084:  beq.s      IL_0088

    IL_0086:  ldc.i4.0
    IL_0087:  stloc.0
    IL_0088:  call       unsigned int8 Test_ldvirtftncalli::f4c()
    IL_008d:  ldc.i4.s   100
    IL_008f:  beq.s      IL_0093

    IL_0091:  ldc.i4.0
    IL_0092:  stloc.0
    IL_0093:  call       float32 Test_ldvirtftncalli::f5a()
    IL_0098:  ldc.r4     100.
    IL_009d:  beq.s      IL_00a1

    IL_009f:  ldc.i4.0
    IL_00a0:  stloc.0
    IL_00a1:  ldloc.1
    IL_00a2:  call       float32 Test_ldvirtftncalli::f5b(class B)
    IL_00a7:  ldc.r4     100.
    IL_00ac:  beq.s      IL_00b0

    IL_00ae:  ldc.i4.0
    IL_00af:  stloc.0
    IL_00b0:  call       float32 Test_ldvirtftncalli::f5c()
    IL_00b5:  ldc.r4     100.
    IL_00ba:  beq.s      IL_00be

    IL_00bc:  ldc.i4.0
    IL_00bd:  stloc.0
    IL_00be:  call       float64 Test_ldvirtftncalli::f6a()
    IL_00c3:  ldc.r8     100.
    IL_00cc:  beq.s      IL_00d0

    IL_00ce:  ldc.i4.0
    IL_00cf:  stloc.0
    IL_00d0:  ldloc.1
    IL_00d1:  call       float64 Test_ldvirtftncalli::f6b(class B)
    IL_00d6:  ldc.r8     100.
    IL_00df:  beq.s      IL_00e3

    IL_00e1:  ldc.i4.0
    IL_00e2:  stloc.0
    IL_00e3:  call       float64 Test_ldvirtftncalli::f6c()
    IL_00e8:  ldc.r8     100.
    IL_00f1:  beq.s      IL_00f5

    IL_00f3:  ldc.i4.0
    IL_00f4:  stloc.0
    IL_00f5:  ldloc.0
    IL_00f6:  brtrue.s   IL_0106

    IL_00f8:  ldstr      "FAILED"
    IL_00fd:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0102:  ldc.i4.1
    IL_0103:  stloc.2
    IL_0104:  br.s       IL_0115

    IL_0106:  ldstr      "PASSED"
    IL_010b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0110:  ldc.i4.s   100
    IL_0112:  stloc.2
    IL_0113:  br.s       IL_0115

    IL_0115:  ldloc.2
    IL_0116:  ret
  } // end of method Test::Main

  .method private hidebysig specialname rtspecialname static 
          void .cctor() 
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  newobj     instance void B::.ctor()
    IL_0005:  stsfld     class B Test_ldvirtftncalli::b1
    IL_000a:  ret
  } // end of method Test::.cctor

  .method public hidebysig specialname rtspecialname 
          instance void .ctor() 
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

}
