

.module 'fault.exe'

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern legacy library mscorlib {}
.assembly 'fault'
{
  
  
}
.namespace VATestLib
{
  .class public auto ansi FormatSpecException extends [mscorlib]System.ApplicationException
  {
    .method public specialname rtspecialname instance void .ctor() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldstr      "Invalid format spec"
      IL_0006:  call       instance void [mscorlib]System.ApplicationException::.ctor(class System.String)
      IL_000b:  ret
    } 

  } 

  .class public auto ansi Complex extends [mscorlib]System.Object
  {
    .field private float64 m_dReal
    .field private float64 m_dImg
    .method public specialname rtspecialname instance void .ctor() il managed
    {
      .maxstack  4
      .locals ([0] float64 V_0)
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ldarg.0
      IL_0007:  ldarg.0
      IL_0008:  ldc.r8     0.
      IL_0011:  dup
      IL_0012:  stloc.0
      IL_0013:  stfld      float64 VATestLib.Complex::m_dImg
      IL_0018:  ldloc.0
      IL_0019:  stfld      float64 VATestLib.Complex::m_dReal
      IL_001e:  ret
    } 

    .method public specialname rtspecialname instance void  .ctor(float64 real,
                                                                  float64 img) il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ldarg.0
      IL_0007:  ldarg.1
      IL_0008:  stfld      float64 VATestLib.Complex::m_dReal
      IL_000d:  ldarg.0
      IL_000e:  ldarg.2
      IL_000f:  stfld      float64 VATestLib.Complex::m_dImg
      IL_0014:  ret
    } 

    .method public virtual instance class System.String ToString() il managed
    {
      .maxstack  3
      .locals ([0] class System.String[] V_0)
      IL_0000:  ldc.i4.5
      IL_0001:  newarr     [mscorlib]System.String
      IL_0006:  stloc.0
      IL_0007:  ldloc.0
      IL_0008:  ldc.i4.0
      IL_0009:  ldstr      "C("
      IL_000e:  stelem.ref
      IL_000f:  ldloc.0
      IL_0010:  ldc.i4.1
      IL_0011:  ldarg.0
      IL_0012:  ldflda     float64 VATestLib.Complex::m_dReal
      IL_0017:  call       instance class System.String [mscorlib]System.Double::ToString()
      IL_001c:  stelem.ref
      IL_001d:  ldloc.0
      IL_001e:  ldc.i4.2
      IL_001f:  ldstr      ", "
      IL_0024:  stelem.ref
      IL_0025:  ldloc.0
      IL_0026:  ldc.i4.3
      IL_0027:  ldarg.0
      IL_0028:  ldflda     float64 VATestLib.Complex::m_dImg
      IL_002d:  call       instance class System.String [mscorlib]System.Double::ToString()
      IL_0032:  stelem.ref
      IL_0033:  ldloc.0
      IL_0034:  ldc.i4.4
      IL_0035:  ldstr      ")"
      IL_003a:  stelem.ref
      IL_003b:  ldloc.0
      IL_003c:  call       class System.String [mscorlib]System.String::Concat(class System.String[])
      IL_0041:  ret
    } 

  } 

  .class value public auto ansi sealed SComplex extends [mscorlib]System.ValueType
  {
    .field private float64 m_dReal
    .field private float64 m_dImg
    .method public specialname rtspecialname instance void  .ctor(float64 real,
                                                                  float64 img) il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  initobj    VATestLib.SComplex
      IL_0007:  ldarg.0
      IL_0008:  ldarg.1
      IL_0009:  stfld      float64 VATestLib.SComplex::m_dReal
      IL_000e:  ldarg.0
      IL_000f:  ldarg.2
      IL_0010:  stfld      float64 VATestLib.SComplex::m_dImg
      IL_0015:  ret
    } 

    .method public virtual instance class System.String ToString() il managed
    {
      .maxstack  3
      .locals ([0] class System.String[] V_0)
      IL_0000:  ldc.i4.5
      IL_0001:  newarr     [mscorlib]System.String
      IL_0006:  stloc.0
      IL_0007:  ldloc.0
      IL_0008:  ldc.i4.0
      IL_0009:  ldstr      "C("
      IL_000e:  stelem.ref
      IL_000f:  ldloc.0
      IL_0010:  ldc.i4.1
      IL_0011:  ldarg.0
      IL_0012:  ldflda     float64 VATestLib.SComplex::m_dReal
      IL_0017:  call       instance class System.String [mscorlib]System.Double::ToString()
      IL_001c:  stelem.ref
      IL_001d:  ldloc.0
      IL_001e:  ldc.i4.2
      IL_001f:  ldstr      ", "
      IL_0024:  stelem.ref
      IL_0025:  ldloc.0
      IL_0026:  ldc.i4.3
      IL_0027:  ldarg.0
      IL_0028:  ldflda     float64 VATestLib.SComplex::m_dImg
      IL_002d:  call       instance class System.String [mscorlib]System.Double::ToString()
      IL_0032:  stelem.ref
      IL_0033:  ldloc.0
      IL_0034:  ldc.i4.4
      IL_0035:  ldstr      ")"
      IL_003a:  stelem.ref
      IL_003b:  ldloc.0
      IL_003c:  call       class System.String [mscorlib]System.String::Concat(class System.String[])
      IL_0041:  ret
    } 

  } 

  .class public auto ansi VATest extends [mscorlib]System.Object
  {
    .method public static vararg class System.String  FormatString(class System.String format) il managed
    {
      .maxstack  4
      .locals ([0] int32 length,
               [1] wchar[] chars,
               [2] value class [mscorlib]System.ArgIterator args,
               [3] class System.String result,
               [4] int32 I,
               [5] bool bLong,
               [6] class System.Object 'box',
               [7] wchar V_7,
               [8] int32 V_8)
      IL_0000:  ldarg.0
      IL_0001:  call       instance int32 [mscorlib]System.String::get_Length()
      IL_0006:  stloc.0
      IL_0007:  ldarg.0
      IL_0008:  call       instance wchar[] [mscorlib]System.String::ToCharArray()
      IL_000d:  stloc.1
      IL_000e:  ldloca.s   args
      IL_0010:  arglist
      IL_0012:  call       instance void [mscorlib]System.ArgIterator::.ctor(value class [mscorlib]System.RuntimeArgumentHandle)
      IL_0017:  ldnull
      IL_001c:  stloc.3
      IL_001d:  ldc.i4.0
      IL_001e:  stloc.s    I
      IL_0020:  br         IL_02c6

      IL_0025:  ldloc.1
      IL_0026:  ldloc.s    I
      IL_0028:  ldelem.u2
      IL_0029:  ldc.i4.s   37
      IL_002b:  beq.s      IL_0046

      IL_002d:  ldloc.3
      IL_002e:  ldloc.1
      IL_002f:  ldloc.s    I
      IL_0031:  ldelema    [mscorlib]System.Char
      		ldobj    [mscorlib]System.Char
      
      IL_0036:  box        [mscorlib]System.Char
      IL_003b:  call       class System.String [mscorlib]System.String::Concat(class System.Object,
                                                                               class System.Object)
      IL_0040:  stloc.3
      IL_0041:  br         IL_02c0

      IL_0046:  ldloc.s    I
      IL_0048:  ldc.i4.1
      IL_0049:  add
      IL_004a:  stloc.s    I
      IL_004c:  ldloc.s    I
      IL_004e:  ldloc.0
      IL_004f:  blt.s      IL_0057

      IL_0051:  newobj     instance void VATestLib.FormatSpecException::.ctor()
      IL_0056:  throw

      IL_0057:  ldc.i4.0
      IL_0058:  stloc.s    bLong
      IL_005a:  ldloc.1
      IL_005b:  ldloc.s    I
      IL_005d:  ldelem.u2
      IL_005e:  ldc.i4.s   108
      IL_0060:  bne.un.s   IL_0076

      IL_0062:  ldc.i4.1
      IL_0063:  stloc.s    bLong
      IL_0065:  ldloc.s    I
      IL_0067:  ldc.i4.1
      IL_0068:  add
      IL_0069:  stloc.s    I
      IL_006b:  ldloc.s    I
      IL_006d:  ldloc.0
      IL_006e:  blt.s      IL_0076

      IL_0070:  newobj     instance void VATestLib.FormatSpecException::.ctor()
      IL_0075:  throw

      IL_0076:  ldloc.1
      IL_0077:  ldloc.s    I
      IL_0079:  ldelem.u2
      IL_007a:  stloc.s    V_7
      IL_007c:  ldloc.s    V_7
      IL_007e:  ldc.i4.s   111
      IL_0080:  bgt.s      IL_00b3

      IL_0082:  ldloc.s    V_7
      IL_0084:  ldc.i4.s   98
      IL_0086:  sub
      IL_0087:  switch     ( 
                            IL_00e1,
                            IL_02ba,
                            IL_0128,
                            IL_02ba,
                            IL_01da)
      IL_00a0:  ldloc.s    V_7
      IL_00a2:  ldc.i4.s   111
      IL_00a4:  beq        IL_0278

      IL_00a9:  br         IL_02ba

      IL_00ae:  br         IL_02ba

      IL_00b3:  ldloc.s    V_7
      IL_00b5:  ldc.i4.s   115
      IL_00b7:  sub
      IL_00b8:  switch     ( 
                            IL_0233,
                            IL_02ba,
                            IL_0181)
      IL_00c9:  ldloc.s    V_7
      IL_00cb:  ldc.i4.s   120
      IL_00cd:  beq        IL_0253

      IL_00d2:  br         IL_02ba

      IL_00d7:  br         IL_02ba

      IL_00dc:  br         IL_02ba

      IL_00e1:  ldloc.s    bLong
      IL_00e3:  brfalse.s  IL_00eb

      IL_00e5:  newobj     instance void VATestLib.FormatSpecException::.ctor()
      IL_00ea:  throw

      IL_00eb:  ldloca.s   args
      IL_00ed:  ldtoken    [mscorlib]System.Boolean
      IL_00f2:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_00f7:  callvirt   instance value class [mscorlib]System.RuntimeTypeHandle [mscorlib]System.Type::get_TypeHandle()
      IL_00fc:  call       instance typedref [mscorlib]System.ArgIterator::GetNextArg(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0101:  refanyval  [mscorlib]System.Boolean
      IL_0106:  ldind.i1
      IL_0107:  brfalse.s  IL_0117

      IL_0109:  ldloc.3
      IL_010a:  ldstr      "true"
      IL_010f:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0114:  stloc.3
      IL_0115:  br.s       IL_0123

      IL_0117:  ldloc.3
      IL_0118:  ldstr      "false"
      IL_011d:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0122:  stloc.3
      IL_0123:  br         IL_02c0

      IL_0128:  ldloc.s    bLong
      IL_012a:  brfalse.s  IL_0155

      IL_012c:  ldloc.3
      IL_012d:  ldloca.s   args
      IL_012f:  ldtoken    [mscorlib]System.Int64
      IL_0134:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0139:  callvirt   instance value class [mscorlib]System.RuntimeTypeHandle [mscorlib]System.Type::get_TypeHandle()
      IL_013e:  call       instance typedref [mscorlib]System.ArgIterator::GetNextArg(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0143:  refanyval  [mscorlib]System.Int64
      IL_0148:  call       instance class System.String [mscorlib]System.Int64::ToString()
      IL_014d:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0152:  stloc.3
      IL_0153:  br.s       IL_017c

      IL_0155:  ldloc.3
      IL_0156:  ldloca.s   args
      IL_0158:  ldtoken    [mscorlib]System.Int32
      IL_015d:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0162:  callvirt   instance value class [mscorlib]System.RuntimeTypeHandle [mscorlib]System.Type::get_TypeHandle()
      IL_0167:  call       instance typedref [mscorlib]System.ArgIterator::GetNextArg(value class [mscorlib]System.RuntimeTypeHandle)
      IL_016c:  refanyval  [mscorlib]System.Int32
      IL_0171:  call       instance class System.String [mscorlib]System.Int32::ToString()
      IL_0176:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_017b:  stloc.3
      IL_017c:  br         IL_02c0

      IL_0181:  ldloc.s    bLong
      IL_0183:  brfalse.s  IL_01ae

      IL_0185:  ldloc.3
      IL_0186:  ldloca.s   args
      IL_0188:  ldtoken    [mscorlib]System.UInt64
      IL_018d:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0192:  callvirt   instance value class [mscorlib]System.RuntimeTypeHandle [mscorlib]System.Type::get_TypeHandle()
      IL_0197:  call       instance typedref [mscorlib]System.ArgIterator::GetNextArg(value class [mscorlib]System.RuntimeTypeHandle)
      IL_019c:  refanyval  [mscorlib]System.UInt64
      IL_01a1:  call       instance class System.String [mscorlib]System.UInt64::ToString()
      IL_01a6:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_01ab:  stloc.3
      IL_01ac:  br.s       IL_01d5

      IL_01ae:  ldloc.3
      IL_01af:  ldloca.s   args
      IL_01b1:  ldtoken    [mscorlib]System.UInt32
      IL_01b6:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_01bb:  callvirt   instance value class [mscorlib]System.RuntimeTypeHandle [mscorlib]System.Type::get_TypeHandle()
      IL_01c0:  call       instance typedref [mscorlib]System.ArgIterator::GetNextArg(value class [mscorlib]System.RuntimeTypeHandle)
      IL_01c5:  refanyval  [mscorlib]System.UInt32
      IL_01ca:  call       instance class System.String [mscorlib]System.UInt32::ToString()
      IL_01cf:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_01d4:  stloc.3
      IL_01d5:  br         IL_02c0

      IL_01da:  ldloc.s    bLong
      IL_01dc:  brfalse.s  IL_0207

      IL_01de:  ldloc.3
      IL_01df:  ldloca.s   args
      IL_01e1:  ldtoken    [mscorlib]System.Double
      IL_01e6:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_01eb:  callvirt   instance value class [mscorlib]System.RuntimeTypeHandle [mscorlib]System.Type::get_TypeHandle()
      IL_01f0:  call       instance typedref [mscorlib]System.ArgIterator::GetNextArg(value class [mscorlib]System.RuntimeTypeHandle)
      IL_01f5:  refanyval  [mscorlib]System.Double
      IL_01fa:  call       instance class System.String [mscorlib]System.Double::ToString()
      IL_01ff:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0204:  stloc.3
      IL_0205:  br.s       IL_022e

      IL_0207:  ldloc.3
      IL_0208:  ldloca.s   args
      IL_020a:  ldtoken    [mscorlib]System.Single
      IL_020f:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0214:  callvirt   instance value class [mscorlib]System.RuntimeTypeHandle [mscorlib]System.Type::get_TypeHandle()
      IL_0219:  call       instance typedref [mscorlib]System.ArgIterator::GetNextArg(value class [mscorlib]System.RuntimeTypeHandle)
      IL_021e:  refanyval  [mscorlib]System.Single
      IL_0223:  call       instance class System.String [mscorlib]System.Single::ToString()
      IL_0228:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_022d:  stloc.3
      IL_022e:  br         IL_02c0

      IL_0233:  ldloc.s    bLong
      IL_0235:  brfalse.s  IL_023d

      IL_0237:  newobj     instance void VATestLib.FormatSpecException::.ctor()
      IL_023c:  throw

      IL_023d:  ldloc.3
      IL_023e:  ldloca.s   args
      IL_0240:  call       instance typedref [mscorlib]System.ArgIterator::GetNextArg()
      IL_0245:  refanyval  [mscorlib]System.String
      IL_024a:  ldind.ref
      IL_024b:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0250:  stloc.3
      IL_0251:  br.s       IL_02c0

      IL_0253:  ldloc.s    bLong
      IL_0255:  brfalse.s  IL_025d

      IL_0257:  newobj     instance void VATestLib.FormatSpecException::.ctor()
      IL_025c:  throw

      IL_025d:  ldloc.3
      IL_025e:  ldloca.s   args
      IL_0260:  call       instance typedref [mscorlib]System.ArgIterator::GetNextArg()
      IL_0265:  refanyval  VATestLib.Complex
      IL_026a:  ldind.ref
      IL_026b:  callvirt   instance class System.String VATestLib.Complex::ToString()
      IL_0270:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0275:  stloc.3
      IL_0276:  br.s       IL_02c0

      IL_0278:  ldloc.s    bLong
      IL_027a:  brfalse.s  IL_0282

      IL_027c:  newobj     instance void VATestLib.FormatSpecException::.ctor()
      IL_0281:  throw

      IL_0282:  ldloca.s   args
      IL_0284:  ldtoken    [mscorlib]System.Object
      IL_0289:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_028e:  callvirt   instance value class [mscorlib]System.RuntimeTypeHandle [mscorlib]System.Type::get_TypeHandle()
      IL_0293:  call       instance typedref [mscorlib]System.ArgIterator::GetNextArg(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0298:  refanyval  [mscorlib]System.Object
      IL_029d:  ldind.ref
      IL_029e:  stloc.s    'box'
      IL_02a0:  ldloc.3
      IL_02a1:  ldloc.s    'box'
      IL_02a3:  unbox      [mscorlib]System.Int32
      IL_02a8:  ldind.i4
      IL_02a9:  stloc.s    V_8
      IL_02ab:  ldloca.s   V_8
      IL_02ad:  call       instance class System.String [mscorlib]System.Int32::ToString()
      IL_02b2:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_02b7:  stloc.3
      IL_02b8:  br.s       IL_02c0

      IL_02ba:  newobj     instance void VATestLib.FormatSpecException::.ctor()
      IL_02bf:  throw

      IL_02c0:  ldloc.s    I
      IL_02c2:  ldc.i4.1
      IL_02c3:  add
      IL_02c4:  stloc.s    I
      IL_02c6:  ldloc.s    I
      IL_02c8:  ldloc.0
      IL_02c9:  blt        IL_0025

      IL_02ce:  ldloc.3
      IL_02cf:  ret
    } 

    .method public specialname rtspecialname instance void .ctor() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } 

  } 

} 

.namespace App
{
  .class auto ansi App extends [mscorlib]System.Object
  {
    .class value auto ansi serializable sealed nested family App$ExecPath extends [mscorlib]System.Enum
    {
      .field public specialname rtspecialname int32 value__
      .field public static literal value class App.App/App$ExecPath PATH_NO_EXCEPTION = int32(0x00000000)
      .field public static literal value class App.App/App$ExecPath PATH_APP_EXCEPTION = int32(0x00000001)
      .field public static literal value class App.App/App$ExecPath PATH_AI_EXCEPTION = int32(0x00000002)
      .field public static literal value class App.App/App$ExecPath PATH_EE_EXCEPTION = int32(0x00000003)
      .field public static literal value class App.App/App$ExecPath PATH_FIRST = int32(0x00000000)
      .field public static literal value class App.App/App$ExecPath PATH_LAST = int32(0x00000003)
    } 

    .field private static initonly class System.String formatSpec
    .field private static initonly class System.String invalidFormatSpec
    .field private static initonly class System.String expResult
    .method family static bool  vaFault(value class App.App/App$ExecPath execPath) il managed
    {
      .maxstack  10
      .locals ([0] bool bRetVal,
               [1] int32 val,
               [2] class System.Object obj1,
               [3] class System.Object obj2,
               [4] bool V_4,
               [5] value class App.App/App$ExecPath V_5)
      IL_0000:  ldc.i4.0
      IL_0001:  stloc.0
      IL_0002:  ldc.i4.s   -11
      IL_0004:  stloc.1
      IL_0005:  ldloc.s   val
      IL_0007:  box        [mscorlib]System.Int32
      IL_000c:  stloc.2
      IL_000d:  ldloc.2
      IL_000e:  stloc.3
      .try
      {
        .try
        {
          IL_000f:  ldarg.0
          IL_0010:  stloc.s    V_5
          IL_0012:  ldloc.s    V_5
          IL_0014:  switch     ( 
                                IL_002e,
                                IL_0080,
                                IL_00bc,
                                IL_00f7)
          IL_0029:  br         IL_0133

          IL_002e:  ldsfld     class System.String App.App::formatSpec
          IL_0033:  ldc.i4.s   -11
          IL_0035:  ldc.i4     0x144
          IL_003a:  ldc.i4.s   -11
          IL_003c:  conv.i8
          IL_003d:  ldc.i4     0x144
          IL_0042:  conv.i8
          IL_0043:  ldstr      "string"
          IL_0048:  ldc.r8     1.5
          IL_0051:  ldc.r8     2.
          IL_005a:  newobj     instance void VATestLib.Complex::.ctor(float64,
                                                                      float64)
          IL_005f:  ldloc.2
          IL_0060:  ldloc.3
          IL_0061:  ldc.i4.1
          IL_0062:  call       vararg class System.String VATestLib.VATest::FormatString(class System.String,
                                                                                         ...,
                                                                                         int32,
                                                                                         unsigned int32,
                                                                                         int64,
                                                                                         unsigned int64,
                                                                                         class System.String,
                                                                                         class VATestLib.Complex,
                                                                                         class System.Object,
                                                                                         class System.Object,
                                                                                         bool)
          IL_0067:  ldsfld     class System.String App.App::expResult
          IL_006c:  call       bool [mscorlib]System.String::Equals(class System.String,
                                                                    class System.String)
          IL_0071:  brtrue.s   IL_007b

          IL_0073:  ldc.i4.0
          IL_0074:  stloc.s    V_4
          IL_0076:  leave      IL_0198

          IL_007b:  br         IL_0133

          IL_0080:  ldsfld     class System.String App.App::invalidFormatSpec
          IL_0085:  ldc.i4.s   -11
          IL_0087:  ldc.i4     0x144
          IL_008c:  ldc.i4.s   -11
          IL_008e:  conv.i8
          IL_008f:  ldc.i4     0x144
          IL_0094:  conv.i8
          IL_0095:  ldstr      "string"
          IL_009a:  ldc.r8     1.5
          IL_00a3:  ldc.r8     2.
          IL_00ac:  newobj     instance void VATestLib.Complex::.ctor(float64,
                                                                      float64)
          IL_00b1:  ldloc.2
          IL_00b2:  ldloc.3
          IL_00b3:  ldc.i4.1
          IL_00b4:  call       vararg class System.String VATestLib.VATest::FormatString(class System.String,
                                                                                         ...,
                                                                                         int32,
                                                                                         unsigned int32,
                                                                                         int64,
                                                                                         unsigned int64,
                                                                                         class System.String,
                                                                                         class VATestLib.Complex,
                                                                                         class System.Object,
                                                                                         class System.Object,
                                                                                         bool)
          IL_00b9:  pop
          IL_00ba:  br.s       IL_0133

          IL_00bc:  ldsfld     class System.String App.App::formatSpec
          IL_00c1:  ldc.i4.s   -11
          IL_00c3:  ldc.i4     0x144
          IL_00c8:  ldc.i4.s   -11
          IL_00ca:  conv.i8
          IL_00cb:  ldc.i4     0x144
          IL_00d0:  conv.i8
          IL_00d1:  ldstr      "string"
          IL_00d6:  ldc.r8     1.5
          IL_00df:  ldc.r8     2.
          IL_00e8:  newobj     instance void VATestLib.Complex::.ctor(float64,
                                                                      float64)
          IL_00ed:  ldloc.2
          IL_00ee:  ldloc.3
          IL_00ef:  call       vararg class System.String VATestLib.VATest::FormatString(class System.String,
                                                                                         ...,
                                                                                         int32,
                                                                                         unsigned int32,
                                                                                         int64,
                                                                                         unsigned int64,
                                                                                         class System.String,
                                                                                         class VATestLib.Complex,
                                                                                         class System.Object,
                                                                                         class System.Object)
          IL_00f4:  pop
          IL_00f5:  br.s       IL_0133

          IL_00f7:  ldsfld     class System.String App.App::formatSpec
          IL_00fc:  ldc.i4.s   -11
          IL_00fe:  ldc.i4     0x144
          IL_0103:  ldc.i4.s   -11
          IL_0105:  conv.i8
          IL_0106:  ldc.i4     0x144
          IL_010b:  conv.i8
          IL_010c:  ldstr      "string"
          IL_0111:  ldc.r8     1.5
          IL_011a:  ldc.r8     2.
          IL_0123:  newobj     instance void VATestLib.Complex::.ctor(float64,
                                                                      float64)
          IL_0128:  ldloc.2
          IL_0129:  ldc.i4.0
          IL_012a:  ldc.i4.1
          IL_012b:  call       vararg class System.String VATestLib.VATest::FormatString(class System.String,
                                                                                         ...,
                                                                                         int32,
                                                                                         unsigned int32,
                                                                                         int64,
                                                                                         unsigned int64,
                                                                                         class System.String,
                                                                                         class VATestLib.Complex,
                                                                                         class System.Object,
                                                                                         bool,
                                                                                         bool)
          IL_0130:  pop
          IL_0131:  br.s       IL_0133

          IL_0133:  leave.s    IL_018b

        }  
        fault
        {
          IL_0135:  ldstr      "Fault handler."
          IL_013a:  call       void [System.Console]System.Console::WriteLine(class System.String)
          IL_013f:  ldarg.0
          IL_0140:  ldc.i4.0
          IL_0141:  beq.s      IL_018a

          IL_0143:  ldsfld     class System.String App.App::formatSpec
          IL_0148:  ldc.i4.s   -11
          IL_014a:  ldc.i4     0x144
          IL_014f:  ldc.i4.s   -11
          IL_0151:  conv.i8
          IL_0152:  ldc.i4     0x144
          IL_0157:  conv.i8
          IL_0158:  ldstr      "string"
          IL_015d:  ldc.r8     1.5
          IL_0166:  ldc.r8     2.
          IL_016f:  newobj     instance void VATestLib.Complex::.ctor(float64,
                                                                      float64)
          IL_0174:  ldloc.2
          IL_0175:  ldloc.3
          IL_0176:  ldc.i4.1
          IL_0177:  call       vararg class System.String VATestLib.VATest::FormatString(class System.String,
                                                                                         ...,
                                                                                         int32,
                                                                                         unsigned int32,
                                                                                         int64,
                                                                                         unsigned int64,
                                                                                         class System.String,
                                                                                         class VATestLib.Complex,
                                                                                         class System.Object,
                                                                                         class System.Object,
                                                                                         bool)
          IL_017c:  ldsfld     class System.String App.App::expResult
          IL_0181:  call       bool [mscorlib]System.String::Equals(class System.String,
                                                                    class System.String)
          IL_0186:  brfalse.s  IL_018a

          IL_0188:  ldc.i4.1
          IL_0189:  stloc.0
          IL_018a:  endfinally
        }  
        IL_018b:  leave.s    IL_0190

      }  
      catch [mscorlib]System.Exception 
      {
        IL_018d:  pop
        IL_018e:  leave.s    IL_0190

      }  
      IL_0190:  ldarg.0
      IL_0191:  ldc.i4.0
      IL_0192:  bne.un.s   IL_0196

      IL_0194:  ldc.i4.1
      IL_0195:  stloc.0
      IL_0196:  ldloc.0
      IL_0197:  ret

      IL_0198:  ldloc.s    V_4
      IL_019a:  ret
    } 

    .method public static int32  Main(class System.String[] args) il managed
    {
      .entrypoint
      .maxstack  3
      .locals ([0] value class App.App/App$ExecPath arg)
      IL_0000:  ldc.i4.0
      IL_0001:  stloc.0
      IL_0002:  br.s       IL_002d

      IL_0004:  ldloc.0
      IL_0005:  call       bool App.App::vaFault(value class App.App/App$ExecPath)
      IL_000a:  brtrue.s   IL_0029

      IL_000c:  ldstr      "vaFault("
      IL_0011:  ldloca.s   arg
      IL_0013:  call       instance class System.String [mscorlib]System.Int32::ToString()
      IL_0018:  ldstr      ") failed."
      IL_001d:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String,
                                                                               class System.String)
      IL_0022:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0027:  ldc.i4.1
      IL_0028:  ret

      IL_0029:  ldloc.0
      IL_002a:  ldc.i4.1
      IL_002b:  add
      IL_002c:  stloc.0
      IL_002d:  ldloc.0
      IL_002e:  ldc.i4.3
      IL_002f:  ble.s      IL_0004

      IL_0031:  ldstr      "*** vaFault test passed ***"
      IL_0036:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_003b:  ldc.i4    0x64
      IL_003c:  ret
    } 

    .method public specialname rtspecialname static void .cctor() il managed
    {
      .maxstack  8
      IL_0000:  ldstr      "%d %u %ld %lu %s %x %o %o %b"
      IL_0005:  stsfld     class System.String App.App::formatSpec
      IL_000a:  ldstr      "%d %u %ld %lu %s %x %%o %b"
      IL_000f:  stsfld     class System.String App.App::invalidFormatSpec
      IL_0014:  ldstr      "-11 324 -11 324 string C(1"
      IL_0019:  call       class [mscorlib]System.Threading.Thread [mscorlib]System.Threading.Thread::get_CurrentThread()
      IL_001e:  callvirt   instance class [mscorlib]System.Globalization.CultureInfo [mscorlib]System.Threading.Thread::get_CurrentCulture()
      IL_0023:  callvirt   instance class [mscorlib]System.Globalization.NumberFormatInfo [mscorlib]System.Globalization.CultureInfo::get_NumberFormat()
      IL_0028:  callvirt   instance string [mscorlib]System.Globalization.NumberFormatInfo::get_NumberDecimalSeparator()
      IL_002d:  ldstr      "5, 2) -11 -11 true"
      IL_0032:  call       string [mscorlib]System.String::Concat(string,
                                                                  string,
                                                                  string)
      IL_0037:  stsfld     string App.App::expResult
      IL_003c:  ret
    } 

    .method public specialname rtspecialname instance void .ctor() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } 

  } 

} 

