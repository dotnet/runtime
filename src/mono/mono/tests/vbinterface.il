//
// This is a test to reproduce the class hierarchy bug generated
// by some VB applications.
//
// Bug: #79125
//
.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly extern System
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) // .z\V.4..
}
.assembly 'test2'
{
  .hash algorithm 0x00008004
  .ver  1:0:0:0
}
.module test2.exe // GUID = {656A0DD7-7485-4FA9-8136-4E05614D1FE9}


.namespace vbnc
{
  .class interface public auto ansi abstract IBaseObject
  {

    // method line 17
    .method public virtual  newslot  abstract  specialname 
           instance default class vbnc.IBaseObject get_Parent ()  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method IBaseObject::get_Parent

	.property instance class vbnc.IBaseObject Parent ()
	{
		.get instance default class vbnc.IBaseObject vbnc.IBaseObject::get_Parent () 
	}
  } // end of class vbnc.IBaseObject
}

.namespace vbnc
{
  .class public auto ansi abstract BaseObject
  	extends [mscorlib]System.Object
  	implements vbnc.IBaseObject  {

    // method line 18
    .method public specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        .custom instance void class [mscorlib]System.Diagnostics.DebuggerNonUserCodeAttribute::.ctor() =  (01 00 00 00 ) // ....

        // Method begins at RVA 0x22c0
	// Code size 9 (0x9)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::.ctor()
	IL_0006:  nop 
	IL_0007:  nop 
	IL_0008:  ret 
    } // end of method BaseObject::.ctor

    // method line 19
    .method private final  virtual  newslot  specialname 
           instance default class vbnc.IBaseObject get_pParent ()  cil managed 
    {
        // Method begins at RVA 0x22cc
	.override class vbnc.IBaseObject::get_Parent
	// Code size 7 (0x7)
	.maxstack 1
	.locals init (
		class vbnc.IBaseObject	V_0)
	IL_0000:  nop 
	IL_0001:  ldnull 
	IL_0002:  stloc.0 
	IL_0003:  br.s IL_0005

	IL_0005:  ldloc.0 
	IL_0006:  ret 
    } // end of method BaseObject::get_pParent

	.property instance class vbnc.IBaseObject pParent ()
	{
		.get instance default class vbnc.IBaseObject vbnc.BaseObject::get_pParent () 
	}
  } // end of class vbnc.BaseObject
}

.namespace vbnc
{
  .class interface public auto ansi abstract INameable
  	implements vbnc.IBaseObject  {

    // method line 20
    .method public virtual  newslot  abstract  specialname 
           instance default string get_Name ()  cil managed 
    {
        // Method begins at RVA 0x0
    } // end of method INameable::get_Name

	.property instance string Name ()
	{
		.get instance default string vbnc.INameable::get_Name () 
	}
  } // end of class vbnc.INameable
}

.namespace vbnc
{
  .class public auto ansi abstract MemberDeclaration
  	extends vbnc.BaseObject
  	implements vbnc.INameable  {

    .method public specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
        .custom instance void class [mscorlib]System.Diagnostics.DebuggerNonUserCodeAttribute::.ctor() =  (01 00 00 00 ) // ....

        // Method begins at RVA 0x22e0
	// Code size 9 (0x9)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class vbnc.BaseObject::.ctor()
	IL_0008:  ret 
    } // end of method MemberDeclaration::.ctor

    // method line 22
    .method public final  virtual  newslot  specialname 
           instance default string get_Name ()  cil managed 
    {
        // Method begins at RVA 0x22ec
	.override class vbnc.INameable::get_Name
	// Code size 11 (0xb)
	.maxstack 1
	.locals init (
		string	V_0)
	IL_0000:  nop 
	IL_0001:  ldstr ""
	IL_0006:  stloc.0 
	IL_0007:  br.s IL_0009

	IL_0009:  ldloc.0 
	IL_000a:  ret 
    } // end of method MemberDeclaration::get_Name

	.property instance string Name ()
	{
		.get instance default string vbnc.MemberDeclaration::get_Name () 
	}
  } // end of class vbnc.MemberDeclaration
}

.namespace vbnc
{
  .class public auto ansi ConstantDeclaration
  	extends vbnc.MemberDeclaration
  {

    .method public specialname  rtspecialname 
           instance default void .ctor ()  cil managed 
    {
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class vbnc.MemberDeclaration::.ctor()
	IL_0008:  ret 
    } // end of method ConstantDeclaration::.ctor
  } // end of class vbnc.ConstantDeclaration
}

.namespace vbnc
{
  .class private auto ansi sealed Module1
  	extends [mscorlib]System.Object
  {
    // method line 24
    .method public static 
           default void Main ()  cil managed 
    {
	.entrypoint
	.maxstack 20
	newobj instance void class vbnc.ConstantDeclaration::.ctor()
	pop
	IL_0002:  ret 
    } // end of method Module1::Main

  } // end of class vbnc.Module1
}

