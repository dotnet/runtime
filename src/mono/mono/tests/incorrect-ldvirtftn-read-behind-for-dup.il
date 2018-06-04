/*
Based on this C#, which is based on MSDN.

public delegate void D();

public class B
{
	int i = 1;

	public void Instance ()
	{
		System.Console.WriteLine("Instance {0}", i);
	}

	public static void Static ()
	{
		System.Console.WriteLine("Static {0}", 2);
	}

	public virtual void Virtual ()
	{
		System.Console.WriteLine("Virtual {0}", i + 2);
	}
}

public class A
{
	public static void Main()
	{
		var b = new B ();
		var d1 = new D (b.Instance);
		var d2 = new D (B.Static);
		var d3 = new D (b.Virtual);
		d1 ();
		d2 ();
		d3 ();
	}
}
*/

.assembly extern mscorlib { }

.assembly '1' { }

.class sealed public D
extends [mscorlib]System.MulticastDelegate
{
.method public void '.ctor' (object 'object', native int 'method') runtime { }
.method public virtual newslot instance void Invoke () runtime { }
.method public virtual newslot instance class [mscorlib]System.IAsyncResult BeginInvoke (class [mscorlib]System.AsyncCallback callback, object 'object') runtime { }
.method public virtual newslot instance void EndInvoke (class [mscorlib]System.IAsyncResult result) runtime { }
}

.class public B
{
	.field int32 i

.method public instance void Instance ()
{
	.maxstack 99

	ldstr "B.Instance {0}"
	ldarg 0
	ldfld int32 B::i
	box [mscorlib]System.Int32
	call void class [mscorlib]System.Console::WriteLine(string, object)
	ret
}

.method public static void Static ()
{
	.maxstack 99

	ldstr "B.Static {0}"
	ldc.i4 2
	box [mscorlib]System.Int32
	call void class [mscorlib]System.Console::WriteLine(string, object)
	ret
}

.method public virtual newslot instance void Virtual1 ()
{
	.maxstack 99
	ldstr "B.Virtual1"
	call void class [mscorlib]System.Console::WriteLine(string)
	ret
}

.method public virtual newslot instance void Virtual ()
{
	.maxstack 99

	ldstr "B.Virtual {0}"
	ldarg 0
	ldfld int32 B::i
	ldc.i4 2
	add
	box [mscorlib]System.Int32
	call void class [mscorlib]System.Console::WriteLine(string, object)
	ret
}

.method public specialname rtspecialname instance void .ctor ()
{
	.maxstack 99

	ldarg 0
	ldc.i4 1
	stfld int32 B::i
	ldarg 0
	call instance void object::.ctor()
	ret
}
}

.class public C
{
	.field int32 i

.method public instance void Instance ()
{
	.maxstack 99

	ldstr "C.Instance {0}"
	ldarg 0
	ldfld int32 C::i
	box [mscorlib]System.Int32
	call void class [mscorlib]System.Console::WriteLine(string, object)
	ret
}

.method public static void Static ()
{
	.maxstack 99

	ldstr "C.Static {0}"
	ldc.i4 2
	box [mscorlib]System.Int32
	call void class [mscorlib]System.Console::WriteLine(string, object)
	ret
}

.method public virtual newslot instance void Virtual ()
{
	.maxstack 99

	ldstr "C.Virtual {0}"
	ldarg 0
	ldfld int32 C::i
	ldc.i4 2
	add
	box [mscorlib]System.Int32
	call void class [mscorlib]System.Console::WriteLine(string, object)
	ret
}

.method public specialname rtspecialname instance void .ctor ()
{
	.maxstack 99

	ldarg 0
	ldc.i4 1
	stfld int32 C::i
	ldarg 0
	call instance void object::.ctor()
	ret
}
}

.class public A
{

.method public static void Main ()
{
	.entrypoint
	.maxstack 99
	.locals init (
		class B b,
		class D d1,
		class D d2,
		class D d3,
		class C c,

		// pad out locals to hit the assert
		class C v00, class C v01, class C v02, class C v03, class C v04, class C v05, class C v06, class C v07,
		class C v08, class C v09, class C v0A, class C v0B, class C v0C, class C v0D, class C v0E, class C v0F,
		class C v10, class C v11, class C v12, class C v13, class C v14, class C v15, class C v16, class C v17,
		class C v18, class C v19, class C v1A, class C v1B, class C v1C, class C v1D, class C v1E, class C v1F,
		class C v20, class C v21, class C v22, class C v23, class C v24, class C v25, class C v26, class C v27
	)

	// b = new B ()
	newobj instance void class B::'.ctor'()
	stloc b

	// c = new C ()
	newobj instance void class C::.ctor()
	stloc c

	// d1 = new D (b, B::Instance)
	ldloc b
	ldftn instance void class B::Instance()
	newobj instance void class D::.ctor(object, native int)
	stloc d1

	// d2 = new D (null, B::Static)
	ldnull
	ldftn void class B::Static()
	newobj instance void class D::.ctor(object, native int)
	stloc d2

	// d3 = new D (c, {c, B::Virtual}) but JIT sees b
	ldloc c
	dup		// the dup the code is/was looking back for

// dup is 0x25
// Let's put in 0x25 that isn't dup.
// And mess up the compile-time but not runtime stack.
// Mislead the JIT and there is a runtime crash.
	ldloc b
	dup
	stloc.s 0x25
	stloc.s 0x25

	ldvirtftn instance void class B::Virtual1()
	newobj instance void class D::.ctor(object, native int)
	stloc d3

	// d1 ()
	ldloc d1
	callvirt instance void class D::Invoke()

	// d2 ()
	ldloc d2
	callvirt instance void class D::Invoke()

	// d3 ()
	ldloc d3
	callvirt instance void class D::Invoke()

	ret
}

}
