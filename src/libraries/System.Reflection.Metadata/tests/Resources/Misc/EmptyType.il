// ILASM and then zero byte 0x330

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}

.assembly EmptyType
{
  .ver 0:0:0:0
}

.class public auto ansi beforefieldinit D
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{  

} 