.assembly extern mscorlib
{
  .ver 1:0:5000:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'bug-81466-lib'
{
  .custom instance void class [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(bool, bool) =  (01 00 01 01 00 00 ) // ......

  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module 'bug-81466-lib.dll'


.namespace MainSpace
{
  .class interface public auto ansi abstract IA
  {

  } // end of class MainSpace.IA
}

.namespace MainSpace
{
  .class interface public auto ansi abstract IB
  {

    // method line 1
    .method public virtual  hidebysig  newslot  abstract 
           instance default bool Test ()  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method IB::Test

  } // end of class MainSpace.IB
}

.namespace MainSpace
{
  .class interface public auto ansi abstract IC
  	implements MainSpace.IA, MainSpace.IB  {

  } // end of class MainSpace.IC
}

.namespace MainSpace
{
  .class interface public auto ansi abstract ID
  	implements MainSpace.IC, MainSpace.IA, MainSpace.IB  {

  } // end of class MainSpace.ID
}

.namespace MainSpace
{
  .class public auto ansi beforefieldinit A
  	extends [mscorlib]System.Object
  	implements MainSpace.IA  {

    // method line 2
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        // Method begins at RVA 0x2050
	// Code size 7 (0x7)
	.maxstack 1
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::.ctor()
	IL_0006:  ret 
    } // end of method A::.ctor

  } // end of class MainSpace.A
}

.namespace MainSpace
{
  .class public auto ansi beforefieldinit AC
  	extends MainSpace.A
  	implements MainSpace.IC, MainSpace.IA, MainSpace.IB  {

    // method line 3
    .method public virtual  hidebysig  newslot 
           instance default bool Test ()  cil managed 
    {
        // Method begins at RVA 0x2064
	// Code size 16 (0x10)
	.maxstack 1
	.locals init (
		bool	V_0)
	IL_0000:  ldstr "AC.Test()"
	IL_0005:  call void class [mscorlib]System.Console::WriteLine(string)
	IL_000a:  ldc.i4.1 
	IL_000b:  stloc.0 
	IL_000c:  br.s IL_000e

	IL_000e:  ldloc.0 
	IL_000f:  ret 
    } // end of method AC::Test

    // method line 4
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        // Method begins at RVA 0x2080
	// Code size 7 (0x7)
	.maxstack 1
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class MainSpace.A::.ctor()
	IL_0006:  ret 
    } // end of method AC::.ctor

  } // end of class MainSpace.AC
}

.namespace MainSpace
{
  .class public auto ansi beforefieldinit CD
  	extends MainSpace.AC
  	implements MainSpace.ID, MainSpace.IC, MainSpace.IA, MainSpace.IB  {

    // method line 5
    .method public hidebysig  specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        // Method begins at RVA 0x2094
	// Code size 7 (0x7)
	.maxstack 1
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class MainSpace.AC::.ctor()
	IL_0006:  ret 
    } // end of method CD::.ctor

  } // end of class MainSpace.CD
}

