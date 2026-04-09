// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib {}
.assembly 'lib-219037'
{
  .ver 0:0:0:0
}
.class public auto ansi sealed LibType
       extends [mscorlib]System.ValueType
{
  .field public static initonly int32 f
  .method private specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       23 (0x17)
    .maxstack  100
    IL_0000:  ldstr      ".cctor"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldsfld     int32 ['lib-219037']Util::count
    IL_000f:  ldc.i4.1
    IL_0010:  add
    IL_0011:  stsfld     int32 ['lib-219037']Util::count
    IL_0016:  ret
  } // end of method LibType::.cctor

} // end of class LibType

.class public auto ansi Util
       extends [mscorlib]System.Object
{
  .field public static int32 count
} 
