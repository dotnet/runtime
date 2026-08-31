// Test runtime custom modifier behavior.
//
// Roslyn expects custom modifiers to append together when a generic type is
// instantiated.
//
// For example, if a method has the signature
//       .method public hidebysig virtual instance void Method (!0 modopt(Foo))
// and the type is instantiated with int32 modopt (Bar)
// the result should be a method with a signature
//       .method public hidebysig virtual instance void Method (int32 modopt(Foo) modopt(Bar))

.assembly extern mscorlib { }
.assembly 'custom-modifiers-append.1' { }

.class public Foo
{
    .method public hidebysig specialname rtspecialname instance void .ctor()
    {
	ldarg.0
	call instance void object::.ctor()
	ret
    }
}

.class public Bar
{
    .method public hidebysig specialname rtspecialname instance void .ctor()
    {
	ldarg.0
	call instance void object::.ctor()
	ret
    }
}


.class public BaseClass`1<T1> extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor()
    {
	ldarg.0
	call instance void object::.ctor()
	ret
    }

    .method public newslot virtual instance int32 TestMethod(!T1 modopt(Foo))
    {
	ldstr "In BaseClass::TestMethod (T1 modopt(Foo))"
	call void class [mscorlib]System.Console::WriteLine(string)
	ldc.i4.1
	ret
    }

    .method public newslot virtual instance int32 TestMethod(!T1)
    {
	ldstr "In BaseClass::TestMethod (T1)"
	call void class [mscorlib]System.Console::WriteLine(string)
	ldc.i4.2
	ret
    }
}

.class public MidClass extends class BaseClass`1<int32 modopt(Bar)>
{
    .method public hidebysig specialname rtspecialname instance void .ctor()
    {
	ldarg.0
	call instance void class BaseClass`1<int32 modopt(Bar)>::.ctor()
	ret
    }

    .method public hidebysig virtual instance int32 TestMethod(int32 modopt(Bar))
    {
	ldstr "In MidClass::TestMethod (int32 modopt(Bar))"
	call void class [mscorlib]System.Console::WriteLine(string)
	ldc.i4.s 21
	ret
    }
}

.class public LeafClass extends class MidClass
{
    .method public hidebysig specialname rtspecialname instance void .ctor()
    {
	ldarg.0
	call instance void class MidClass::.ctor()
	ret
    }

    .method public hidebysig virtual instance int32 TestMethod(int32 modopt(Bar) modopt(Foo))
    {
	ldstr "In LeafClass::TestMethod (int32 modopt(Bar) modopt(Foo))"
	call void class [mscorlib]System.Console::WriteLine(string)
	ldc.i4 32
	ret
    }
}

.class public MainClass
{
    .method public static hidebysig int32 Main()
    {
	.entrypoint
	.locals init (class BaseClass`1<int32> c)
	ldstr "HelloWorld"
	call void class [mscorlib]System.Console::WriteLine(string)
	newobj instance void class MidClass::.ctor()
	stloc.0
	ldloc.0
	ldc.i4.0
	// expect the call to BaseClass::TestMethod (T1 modopt(Foo))
	callvirt instance int32 class BaseClass`1<int32>::TestMethod(!0 modopt(Foo))
	ldc.i4.1
	bne.un bad1
	br after1
    bad1:
	ldc.i4.1
	ret
    after1:
	newobj instance void class MidClass::.ctor()
	stloc.0
	ldloc.0
	ldc.i4.0
	// expect the call to MidClass::TestMethod (int32 modopt(Bar))
	callvirt instance int32 class BaseClass`1<int32>::TestMethod(!0)
	ldc.i4.s 21
	bne.un bad2
	br after2
    bad2:
	ldc.i4.2
	ret
    after2:
	newobj instance void class LeafClass::.ctor()
	stloc.0
	ldloc.0
	ldc.i4.0
	// expect the call to LeafClass::TestMethod (int32 modopt(Bar) modopt(Foo))
	callvirt instance int32 class BaseClass`1<int32>::TestMethod(!0 modopt(Foo))
	ldc.i4.s 32
	bne.un bad3
	br after3
    bad3:
	ldc.i4.2
	ret
    after3:
	ldc.i4.0
	ret
    }
}
