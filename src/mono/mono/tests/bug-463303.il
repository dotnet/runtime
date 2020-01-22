.assembly extern mscorlib
{
  .ver 2:0:0:0
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
}

.assembly 'repro'
{
  .hash algorithm 0x00008004
  .ver  0:0:0:0
}
.module repro

.class interface public auto ansi abstract IBase
{
	.method public virtual  newslot  abstract instance default void S ()  cil managed 
	{
    }
}

.class interface public auto ansi abstract IDerived	implements IBase
{
}

.class interface public auto ansi abstract IDerivedDerived implements IDerived
{
}

.class public auto ansi MemberDeclaration2 extends [mscorlib]System.Object implements IDerivedDerived
{
	.method public specialname  rtspecialname instance default void '.ctor' ()  cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::'.ctor'()
		ret 
	}

	.method public final virtual newslot instance default void S ()  cil managed 
	{
		.override class IBase::S
		.maxstack 8
		ldstr "SUCCESS"
		call void class [mscorlib]System.Console::WriteLine(string)
		ret 
	}
}

.class private auto ansi C`1<(class IBase) X> extends [mscorlib]System.Object
{
    .method public specialname rtspecialname instance default void '.ctor' ()  cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::'.ctor'()
		ret 
	}

	.method public hidebysig instance default void Add (!X v)  cil managed 
	{
		.maxstack 2
		.locals init (!X V_0)
		ldarg.1 
		stloc.0 
		ldloca.s 0
		constrained. !0
		callvirt instance void class IBase::S()
		ret 
	}
}

.class private auto ansi TT	extends [mscorlib]System.Object
{
    .method public specialname  rtspecialname instance default void '.ctor' ()  cil managed 
	{
		.maxstack 8
		ldarg.0 
		call instance void object::'.ctor'()
		ret 
	}

	.method public static 
	default int32 Main ()  cil managed 
	{
		.entrypoint
		.maxstack 5
		.locals init (
			int32	V_0,
			class MemberDeclaration2	V_1,
			class C`1<class IDerivedDerived>	V_2)

		ldstr "Main"
		call void class [mscorlib]System.Console::WriteLine(string)
		newobj instance void class MemberDeclaration2::'.ctor'()
		stloc.1 
		newobj instance void class C`1<class IDerivedDerived>::'.ctor'()
		stloc.2 
		ldloc.2 
		ldloc.1 
		castclass IDerivedDerived
		callvirt instance void class C`1<class IDerivedDerived>::Add(!0)
		ldstr "Main [Done]"
		call void class [mscorlib]System.Console::WriteLine(string)
		ldloc.0 
		ret 
	}
}


