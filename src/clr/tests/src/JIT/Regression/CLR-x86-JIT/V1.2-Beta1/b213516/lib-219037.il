
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
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
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
