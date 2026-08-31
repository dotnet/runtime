//
// load-missing.il: Helper class used by load-exceptions.cs
//

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 2:0:0:0
}
.assembly extern t
{
  .ver 0:0:0:0
}
.assembly extern notFoundAssembly
{
  .ver 0:0:0:0
}

.assembly 'load-missing'
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.custom instance void [mscorlib]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 ) 
.imagebase 0x00400000
.file alignment 0x00000200
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x03310000

  .class auto ansi sealed public EmptyDelegate
         extends [mscorlib]System.MulticastDelegate
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(object 'object',
                                 native int 'method') runtime managed
    {
    }

    .method public hidebysig newslot virtual 
            instance void  Invoke() runtime managed
    {
    }

    .method public hidebysig newslot virtual 
            instance class [mscorlib]System.IAsyncResult 
            BeginInvoke(class [mscorlib]System.AsyncCallback callback,
                        object 'object') runtime managed
    {
    }

    .method public hidebysig newslot virtual 
            instance void  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
    {
    }

  }

  .class auto ansi sealed public DelegateWithMissingArgument
         extends [mscorlib]System.MulticastDelegate
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(object 'object',
                                 native int 'method') runtime managed
    {
    }

    .method public hidebysig newslot virtual 
            instance void  Invoke(class [t]Missing t) runtime managed
    {
    }

    .method public hidebysig newslot virtual 
            instance class [mscorlib]System.IAsyncResult 
            BeginInvoke(class [mscorlib]System.AsyncCallback callback,
                        object 'object') runtime managed
    {
    }

    .method public hidebysig newslot virtual 
            instance void  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
    {
    }

  }

.class public auto ansi beforefieldinit LoadMissing
       extends [mscorlib]System.Object
{

  .method public hidebysig static void  missing_class() cil managed
  {
   
    .maxstack  1
    .locals init (class [t]Missing.Foo1 V_0)
    IL_0000:  nop
    IL_0001:  newobj     instance void [t]Missing.Foo1::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ret
  }

  .method public hidebysig static void  missing_method() cil managed
  {
   
    .maxstack  1
    .locals init (class [t]Missing.Foo2 V_0)
    IL_0000:  nop
    IL_0001:  newobj     instance void [t]Missing.Foo2::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  callvirt   instance void [t]Missing.Foo2::missing()
    IL_000d:  nop
    IL_000e:  ret
  }

  .method public hidebysig static void  missing_ctor() cil managed
  {
   
    .maxstack  2
    .locals init (class [t]Missing.Foo2 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.5
    IL_0002:  newobj     instance void [t]Missing.Foo2::.ctor(int32)
    IL_0007:  stloc.0
    IL_0008:  ret
  }

  .method public hidebysig static void  missing_call() cil managed
  {
   
    .maxstack  1
    .locals init (class [t]Missing.Foo2 V_0)
    IL_0000:  nop
    IL_0001:  newobj     instance void [t]Missing.Foo2::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  callvirt   instance void [t]Missing.Foo2::missing()
    IL_000d:  nop
    IL_000e:  ret
  }

  .method public hidebysig static void  missing_jmp() cil managed
  {
   
    .maxstack  1
    .locals init (class [t]Missing.Foo2 V_0)

	jmp void [t]Missing.Foo2::static_missing ()
  }

  .method public hidebysig static void  missing_ldftn() cil managed
  {
   
    .maxstack  3
    .locals init (class [t]Missing.Foo2 V_0,
             class EmptyDelegate V_1)
    IL_0000:  nop
    IL_0001:  newobj     instance void [t]Missing.Foo2::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  ldftn      instance void [t]Missing.Foo2::missing()
    IL_000e:  newobj     instance void EmptyDelegate::.ctor(object,
                                                            native int)
    IL_0013:  stloc.1
    IL_0014:  ret
  }

  .method public hidebysig static void  missing_callvirt() cil managed
  {
   
    .maxstack  1
    .locals init (class [t]Missing.Foo5 V_0)
    IL_0000:  nop
    IL_0001:  newobj     instance void [t]Missing.Foo5::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  callvirt   instance void [t]Missing.Foo5::missing_virtual()
    IL_000d:  nop
    IL_000e:  ret
  }

  .method public hidebysig static object 
          missing_castclass_helper(object o) cil managed
  {
   
    .maxstack  1
    .locals init (object V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  castclass  [t]Missing.Foo1
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a

    IL_000a:  ldloc.0
    IL_000b:  ret
  }

  .method public hidebysig static void
          missing_castclass() cil managed
  {
   
    .maxstack  1
    .locals init (object V_0)

		ldnull
    	call       object LoadMissing::missing_castclass_helper(object)
		pop
		ret
  }

	.method public hidebysig static void
					missing_cpobj () cil managed
	{
		.maxstack 8
		.locals init (object v_0, object v_1)

		ldloca v_0
		ldloca v_1
		cpobj [t]Missing.Foo1
		ret
	}

	.method public hidebysig static void
					missing_initobj () cil managed
	{
		.maxstack 8
		.locals init (object v_0)

		ldloca v_0
		initobj [t]Missing.Foo1
		ret
	}

  .method public hidebysig static bool  missing_isinst_helper(object o) cil managed
  {
   
    .maxstack  2
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  isinst     [t]Missing.Foo1
    IL_0007:  ldnull
    IL_0008:  cgt.un
    IL_000a:  stloc.0
    IL_000b:  br.s       IL_000d

    IL_000d:  ldloc.0
    IL_000e:  ret
  }

  .method public hidebysig static void
          missing_isinst() cil managed
  {
   
    .maxstack  1
    .locals init (object V_0)

		ldnull
    	call       bool LoadMissing::missing_isinst_helper(object)
		pop
		ret
  }

  .method public hidebysig static void  missing_ldfld() cil managed
  {
   
    .maxstack  1
    .locals init (class [t]Missing.Foo4 V_0,
             int32 V_1)
    IL_0000:  nop
    IL_0001:  newobj     instance void [t]Missing.Foo4::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  ldfld     int32 [t]Missing.Foo4::i
    IL_000d:  stloc.1
    IL_000e:  nop
    IL_000f:  nop
    IL_0013:  ret
  }

  .method public hidebysig static void  missing_ldflda() cil managed
  {
   
    .maxstack  1
    .locals init (class [t]Missing.Foo4 V_0,
             int32& pinned V_1)
    IL_0000:  nop
    IL_0001:  newobj     instance void [t]Missing.Foo4::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  ldflda     int32 [t]Missing.Foo4::i
    IL_000d:  stloc.1
    IL_000e:  nop
    IL_000f:  nop
    IL_0010:  ldc.i4.0
    IL_0011:  conv.u
    IL_0012:  stloc.1
    IL_0013:  ret
  }

  .method public hidebysig static void  missing_ldsfld() cil managed
  {
   
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldsfld    int32 [t]Missing.Foo3::i
    IL_0006:  stloc.0
    IL_0007:  nop
    IL_0008:  nop
    IL_0009:  ldc.i4.0
    IL_000a:  conv.u
    IL_000b:  stloc.0
    IL_000c:  ret
  }

  .method public hidebysig static void  missing_ldsflda() cil managed
  {
   
    .maxstack  1
    .locals init (int32& pinned V_0)
    IL_0000:  nop
    IL_0001:  ldsflda    int32 [t]Missing.Foo3::i
    IL_0006:  stloc.0
    IL_0007:  nop
    IL_0008:  nop
    IL_0009:  ldc.i4.0
    IL_000a:  conv.u
    IL_000b:  stloc.0
    IL_000c:  ret
  }

  .method public hidebysig static void  missing_ldtoken_type() cil managed
  {
   
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldtoken    [t]Missing.Foo1
    IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000b:  call       void [mscorlib]System.Console::WriteLine(object)
    IL_0010:  nop
    IL_0011:  ret
  }

  .method public hidebysig static void  missing_ldtoken_method() cil managed
  {
   
    .maxstack  8
    IL_0000:  nop
    IL_0008:  ldtoken method instance void [t]Missing.Foo2::missing()
    IL_0010:  pop
    IL_0011:  ret
  }

  .method public hidebysig static void missing_assembly_in_fieldref () cil managed
  {
        .maxstack 8
        ldc.i4.0
        stsfld  int32 [t]X::a
        ret
  }

  .method public hidebysig static void missing_assembly_in_call () cil managed
  {
        .maxstack 8
        call void class [t]Gtk.Application::Init()
        ret
  }

  .method public hidebysig static void missing_assembly_in_newobj () cil managed
  {
        .maxstack 8
	newobj instance void class [mscorlib]System.Dingus::.ctor()
	pop
	ret
  }

  .method public hidebysig static void missing_external_type_reference_on_parameter() cil managed
  {
    .maxstack 8
    ldstr "Hello"
    call void class [mscorlib]System.String::Trim(class [mscorlib]MissingType)
    ret
  }

  .method public hidebysig static void  missing_ldtoken_field() cil managed
  {
   
    .maxstack  8
    IL_0000:  nop
    IL_0008:  ldtoken field int32 [t]Missing.Foo4::i
    IL_0010:  pop
    IL_0011:  ret
  }

  .method public hidebysig static void  missing_ldvirtftn() cil managed
  {
   
    .maxstack  3
    .locals init (class [t]Missing.Foo5 V_0,
             class EmptyDelegate V_1)
    IL_0000:  nop
    IL_0001:  newobj     instance void [t]Missing.Foo5::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  dup
    IL_0009:  ldvirtftn  instance void [t]Missing.Foo5::missing_virtual()
    IL_000f:  newobj     instance void EmptyDelegate::.ctor(object,
                                                            native int)
    IL_0014:  stloc.1
    IL_0015:  ret
  }

  .method public hidebysig static object missing_newarr_helper() cil managed
  {
   
    .maxstack  1
    .locals init (object V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.s   10
    IL_0003:  newarr     [t]Missing.Foo1
    IL_0008:  stloc.0
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.0
    IL_000c:  ret
  }

  .method public hidebysig static void  missing_newarr() cil managed
  {
   
    .maxstack  8
    IL_0000:  nop
    IL_0001:  call       object LoadMissing::missing_newarr_helper()
    IL_0006:  pop
    IL_0007:  ret
  }

  .method public hidebysig static void  missing_newobj() cil managed
  {
   
    .maxstack  2
    .locals init (class [t]Missing.Foo2 V_0)
    IL_0000:  nop
    IL_0001:  ldc.i4.5
    IL_0002:  newobj     instance void [t]Missing.Foo2::.ctor(int32)
    IL_0007:  stloc.0
    IL_0008:  ret
  }

  .method public hidebysig static void  missing_refanyval() cil managed
  {
   
    .maxstack  2
    .locals init (int32 v_0)

		ldloca v_0
		mkrefany int32
		refanyval [t]Missing.Foo1
		pop
		ret
	}

  .method public hidebysig static void  missing_sizeof() cil managed
  {
   
    .maxstack  8
    IL_0000:  nop
    IL_0001:  sizeof    [t]Missing.Foo1
    IL_0010:  pop
    IL_0011:  ret
  }

  .method public hidebysig static void  missing_stfld() cil managed
  {
   
    .maxstack  2
    .locals init (class [t]Missing.Foo4 V_0)
    IL_0000:  nop
    IL_0001:  newobj     instance void [t]Missing.Foo4::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.5
    IL_0009:  stfld      int32 [t]Missing.Foo4::i
    IL_000e:  ret
  }

  .method public hidebysig static void  missing_stsfld() cil managed
  {
   
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldc.i4.5
    IL_0002:  stsfld     int32 [t]Missing.Foo3::i
    IL_0007:  ret
  }

  .method public hidebysig static void missing_unbox () cil managed
  {
	  .maxstack 8
	
	  ldnull
      unbox [t]Missing.Foo6
      pop
      ret
  }	

  .method public hidebysig static void missing_unbox_any () cil managed
  {
	  .maxstack 8
	
	  	ldnull
      unbox.any [t]Missing.Foo6
      pop
      ret
  }	

	.method public hidebysig static void missing_local () cil managed
	{
		.maxstack 8
		.locals init (class [t]Missing.Foo1 v_0)

		ret
	}	

	.method public static void delegate_target () cil managed
	{
		ret
	}

	.method public hidebysig static void missing_delegate_ctor_argument () cil managed	   
	{
	    .maxstack  3
		.locals init (class [t]Missing.Foo2 V_0,
			          class EmptyDelegate V_1)
    IL_0000:  nop
    IL_0001:  newobj     instance void LoadMissing::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  ldftn      void LoadMissing::delegate_target()
    IL_000e:  newobj     instance void DelegateWithMissingArgument::.ctor(object,
                                                                          native int)
    IL_0013:  stloc.1
    IL_0014:  ret
	}

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
   
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }

}

.class public auto ansi beforefieldinit BrokenClass
       extends [t]Missing.Foo1
{
}

.class public auto ansi CCtorClass
        extends [mscorlib]System.Object     
  {                                         

    // method line 48
    .method public hidebysig  specialname  rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {                                                    
        .maxstack 8                                      
        IL_0000:  ldarg.0                                
        IL_0001:  call instance void object::'.ctor'()   
        IL_0006:  ret                                    
    } // end of method CCtorClass::.ctor                 

    // method line 49
    .method private static  specialname  rtspecialname 
           default void '.cctor' ()  cil managed       
    {                                                  
    IL_0000:  nop
    IL_0001:  newobj     instance void [t]Missing.Foo2::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  callvirt   instance void [t]Missing.Foo2::missing()
    IL_000d:  nop
    IL_000e:  ret
    } // end of method CCtorClass::.cctor                                    

    // method line 50
    .method public static  hidebysig 
           default void foo ()  cil managed 
    {                                       
        .maxstack 8                         
        IL_0000:  ret                       
    } // end of method CCtorClass::foo      

  } // end of class CCtorClass

.class interface public auto ansi abstract TestIface
{
	.method public virtual  hidebysig  newslot abstract instance default int32 foo ()  cil managed  {}
}

.class public auto ansi beforefieldinit BadMethodOverride1
{
	.method public virtual newslot default int32 foo ()
	{
		.override class TestIface::foo
		ldc.i4.0
		ret
	}

	.method public hidebysig  specialname  rtspecialname  instance default void '.ctor' ()  cil managed 
    {
		ldarg.0                                
		call instance void object::'.ctor'()   
		ret                                    
	}
}

.class public auto ansi beforefieldinit RandomBaseClass
{
	.method public virtual newslot default int32 foo ()
	{
		ldc.i4.0
		ret
	}

	.method public static int32 foo2 ()
	{
		ldc.i4.0
		ret
	}

	.method public hidebysig  specialname  rtspecialname  instance default void '.ctor' ()  cil managed 
    {
		ldarg.0                                
		call instance void object::'.ctor'()   
		ret                                    
	}
}

.class public auto ansi beforefieldinit BadMethodOverride2
{
	.method public virtual newslot default int32 foo ()
	{
		.override class RandomBaseClass::foo
		ldc.i4.0
		ret
	}

	.method public hidebysig  specialname  rtspecialname  instance default void '.ctor' ()  cil managed 
    {
		ldarg.0                                
		call instance void object::'.ctor'()   
		ret                                    
	}
}

.class public auto ansi beforefieldinit BadMethodOverride3 implements TestIface
{
	.method public default int32 foo ()
	{
		.override class TestIface::foo
		ldc.i4.0
		ret
	}

	.method public hidebysig  specialname  rtspecialname  instance default void '.ctor' ()  cil managed 
    {
		ldarg.0                                
		call instance void object::'.ctor'()   
		ret                                    
	}
}

.class public auto ansi beforefieldinit BadMethodOverride4 extends RandomBaseClass
{
	.method public virtual default int32 foo2 ()
	{
		.override class RandomBaseClass::foo2
		ldc.i4.0
		ret
	}

	.method public hidebysig  specialname  rtspecialname  instance default void '.ctor' ()  cil managed 
    {
		ldarg.0                                
		call instance void RandomBaseClass::'.ctor'()   
		ret                                    
	}
}

.class public auto ansi beforefieldinit BadOverridesDriver
{
	.method public static void bad_override1 ()
	{
		newobj instance void BadMethodOverride1::.ctor()
		pop
		ret
	}

	.method public static void bad_override2 ()
	{
		newobj instance void BadMethodOverride2::.ctor()
		pop
		ret
	}

	.method public static void bad_override3 ()
	{
		newobj instance void BadMethodOverride3::.ctor()
		pop
		ret
	}

	.method public static void bad_override4 ()
	{
		newobj instance void BadMethodOverride4::.ctor()
		pop
		ret
	}
}

.class public auto ansi beforefieldinit FieldWithMissingType
{
	.field  public class [t]Missing BrokenField
    .field  public static int32 WorkingField
}

.class public auto ansi beforefieldinit FieldWithMissingCustomAttribute
{
    .field	public object f
    .custom	instance void class [notFoundAssembly]SomeAttribute::'.ctor'() = (01 00 00 00 ) // ....
}
