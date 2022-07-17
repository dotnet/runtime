// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
.assembly extern System.Runtime { }
.assembly UnitTestDelegates { }


// ========================================================================================
// Types that will be used as return types on the various methods
// ========================================================================================

.class public auto ansi beforefieldinit A
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit B extends A
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit C extends B
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

.class public auto ansi beforefieldinit GenRetType<X,Y>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit Dictionary<KEY,VAL>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

.class public auto ansi beforefieldinit GenDerive1<V1,V2> extends class GenRetType<!V1,!V2>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit GenDerive2<U1,U2,U3> extends class GenDerive1<!U1, class Dictionary<!U3,!U2>>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit GenDerive3<T1,T2> extends class GenDerive2<!T1,!T2,string>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

.class public auto ansi beforefieldinit NonGenericDerived1<ARG1,ARG2> extends class GenRetType<!ARG1,!ARG2>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit NonGenericDerived2<T> extends class NonGenericDerived1<!T,object>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit NonGenericDerived3 extends class NonGenericDerived2<int32>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit NonGenericDerived4 extends NonGenericDerived3
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

.class public auto ansi beforefieldinit GenToNonGen1<V1,V2> extends C
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit GenToNonGen2<U1,U2,U3> extends class GenToNonGen1<!U1, class Dictionary<!U3,!U2>>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit GenToNonGen3<T1,T2> extends class GenToNonGen2<!T1,!T2,string>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}


.class public auto ansi beforefieldinit NonGenThroughGen1<V1,V2> extends C
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit NonGenThroughGen2<U1,U2,U3> extends class NonGenThroughGen1<!U1, class Dictionary<!U3,!U2>>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit NonGenThroughGen3 extends class NonGenThroughGen2<object,int32,string>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}
.class public auto ansi beforefieldinit NonGenThroughGen4 extends NonGenThroughGen3
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

.class private auto ansi sealed En extends [System.Runtime]System.Enum
{
	.field public specialname rtspecialname int32 value__
}


// ========================================================================================
// Main base type with various virtual methods that will be overridden later
// ========================================================================================

.class public auto ansi beforefieldinit GenBaseType<A,B>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed  { ret }

  .method public hidebysig newslot virtual instance object MyFunc(string& res)
  {
    ldarg.1
    ldstr "object GenBaseType.MyFunc()"
    stind.ref
    newobj instance void A::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class B MyFunc(string& res)
  {
    ldarg.1
    ldstr "B GenBaseType.MyFunc()"
    stind.ref
    newobj instance void B::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class B GenToNonGen(string& res)
  {
    ldarg.1
    ldstr "B GenBaseType.GenToNonGen()"
    stind.ref
    newobj instance void B::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class B NonGenThroughGenFunc(string& res)
  {
    ldarg.1
    ldstr "B GenBaseType.NonGenThroughGenFunc()"
    stind.ref
    newobj instance void B::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class GenRetType<int32,object> MyGenFunc(string& res)
  {
    ldarg.1
    ldstr "GenRetType<int32,object> GenBaseType.MyGenFunc()"
    stind.ref
    newobj instance void class GenRetType<int32,object>::.ctor()
    ret
  }
  .method public hidebysig newslot virtual instance class GenRetType<!A, class Dictionary<string,!B>> MyGenFunc(string& res)
  {
    ldarg.1
    ldstr "GenRetType<!A, class Dictionary<string,!B>> GenBaseType.MyGenFunc()"
    stind.ref
    newobj instance void class GenRetType<!A, class Dictionary<string,!B>>::.ctor()
    ret
  }

  .method public hidebysig newslot virtual instance int32[] RetIntArr()
  {
    ldstr "base.retintarr"
    call void [System.Console]System.Console::WriteLine(string)
    ldnull
    ret
  }
}

// ========================================================================================
// SECOND LAYER type: overrides *SOME* virtuals on GenBaseType using MethodImpls with
// covariant return types (more derived return types)
// ========================================================================================

.class public auto ansi beforefieldinit GenMiddleType<U,V> extends class GenBaseType<!V,!U>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

  .method public hidebysig newslot virtual instance class NonGenThroughGen2<object,int32,string> NonGenThroughGenFunc(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class B class GenBaseType<!V,!U>::NonGenThroughGenFunc(string& res)
    ldarg.1
    ldstr "NonGenThroughGen2<object,int32,string> GenMiddleType.NonGenThroughGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenToNonGen1<!V, class Dictionary<string,object>> GenToNonGen(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class B class GenBaseType<!V,!U>::GenToNonGen(string& res)
    ldarg.1
    ldstr "GenToNonGen1<!V, class Dictionary<string,object>> GenMiddleType.GenToNonGen()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class NonGenericDerived1<int32,object> MyGenFunc(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class GenRetType<int32,object> class GenBaseType<!V,!U>::MyGenFunc(string& res)
    ldarg.1
    ldstr "NonGenericDerived1<int32,object> GenMiddleType.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenDerive1<!V, class Dictionary<string,!U>> MyGenFunc(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class GenRetType<!0, class Dictionary<string,!1>> class GenBaseType<!V,!U>::MyGenFunc(string& res)
    ldarg.1
    ldstr "GenDerive1<!U, class Dictionary<string,!V>> GenMiddleType.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }
}


// ========================================================================================
// THIRD LAYER type: overrides all virtuals from GenBaseType using MethodImpls with
// covariant return types (more derived return types than the ones used in GenMiddleType)
// ========================================================================================

.class public auto ansi beforefieldinit GenTestType<UNUSED1,UNUSED2,U,V> extends class GenMiddleType<!V,!U>
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }

  .method public hidebysig newslot virtual instance class NonGenThroughGen4 NonGenThroughGenFunc(string& res)
  {
    .override method instance class B class GenBaseType<!U,!V>::NonGenThroughGenFunc(string& res)
    ldarg.1
    ldstr "NonGenThroughGen4 TestType.NonGenThroughGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenToNonGen3<!U,object> GenToNonGen(string& res)
  {
    .override method instance class B class GenBaseType<!U,!V>::GenToNonGen(string& res)
    ldarg.1
    ldstr "GenToNonGen3<!U,object> TestType.GenToNonGen()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class NonGenericDerived4 MyGenFunc(string& res)
  {
    .override method instance class GenRetType<int32,object> class GenBaseType<!U,!V>::MyGenFunc(string& res)
    ldarg.1
    ldstr "NonGenericDerived4 TestType.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenDerive3<!U,!V> MyGenFunc(string& res)
  {
    .override method instance class GenRetType<!0, class Dictionary<string,!1>> class GenBaseType<!U,!V>::MyGenFunc(string& res)
    ldarg.1
    ldstr "GenDerive3<!U,!V> TestType.MyGenFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class GenRetType<!U,!V> MyFunc(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance object class GenBaseType<!U,!V>::MyFunc(string& res)
    ldarg.1
    ldstr "GenRetType<!U,!V> TestType.MyFunc()"
    stind.ref
    ldnull
    ret
  }

  .method public hidebysig newslot virtual instance class C MyFunc(string& res)
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.PreserveBaseOverridesAttribute::.ctor() = (01 00 00 00)
    .override method instance class B class GenBaseType<!U,!V>::MyFunc(string& res)
    ldarg.1
    ldstr "C TestType.MyFunc()"
    stind.ref
    ldnull
    ret
  }

  // ========================================================================================
  // Set of implicit overrides that should be ignored given there are explicit overrides from the MethodImpls
  // ========================================================================================
  .method public hidebysig virtual instance class B NonGenThroughGenFunc(string& res)
  {
    ldstr "Should never execute this method"
    newobj instance void [System.Runtime]System.Exception::.ctor(string)
    throw
  }

  .method public hidebysig virtual instance class B GenToNonGen(string& res)
  {
    ldstr "Should never execute this method"
    newobj instance void [System.Runtime]System.Exception::.ctor(string)
    throw
  }

  .method public hidebysig virtual instance class GenRetType<int32,object> MyGenFunc(string& res)
  {
    ldstr "Should never execute this method"
    newobj instance void [System.Runtime]System.Exception::.ctor(string)
    throw
  }

  .method public hidebysig virtual instance class GenRetType<!0, class Dictionary<string,!1>> MyGenFunc(string& res)
  {
    ldstr "Should never execute this method"
    newobj instance void [System.Runtime]System.Exception::.ctor(string)
    throw
  }

  .method public hidebysig virtual instance object MyFunc(string& res)
  {
    ldstr "Should never execute this method"
    newobj instance void [System.Runtime]System.Exception::.ctor(string)
    throw
  }

  .method public hidebysig virtual instance class B MyFunc(string& res)
  {
    ldstr "Should never execute this method"
    newobj instance void [System.Runtime]System.Exception::.ctor(string)
    throw
  }
}


// ========================================================================================
// Delegates that will be used for the various method calls
// ========================================================================================

.class private auto ansi sealed Del_Obj extends [System.Runtime]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Del_Obj::.ctor

  .method public hidebysig newslot virtual
          instance object  Invoke(string& res) runtime managed
  {
  } // end of method Del_Obj::Invoke

  .method public hidebysig newslot virtual
          instance class [System.Runtime]System.IAsyncResult
          BeginInvoke(string& res,
                      class [System.Runtime]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Del_Obj::BeginInvoke

  .method public hidebysig newslot virtual
          instance object  EndInvoke(string& res,
                                     class [System.Runtime]System.IAsyncResult result) runtime managed
  {
  } // end of method Del_Obj::EndInvoke

} // end of class Del_Obj

.class private auto ansi sealed Del_B extends [System.Runtime]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Del_B::.ctor

  .method public hidebysig newslot virtual
          instance class B  Invoke(string& res) runtime managed
  {
  } // end of method Del_B::Invoke

  .method public hidebysig newslot virtual
          instance class [System.Runtime]System.IAsyncResult
          BeginInvoke(string& res,
                      class [System.Runtime]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Del_B::BeginInvoke

  .method public hidebysig newslot virtual
          instance class B  EndInvoke(string& res,
                                      class [System.Runtime]System.IAsyncResult result) runtime managed
  {
  } // end of method Del_B::EndInvoke

} // end of class Del_B

.class private auto ansi sealed Del_GenRetType_int_object extends [System.Runtime]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Del_GenRetType_int_object::.ctor

  .method public hidebysig newslot virtual
          instance class GenRetType<int32,object>
          Invoke(string& res) runtime managed
  {
  } // end of method Del_GenRetType_int_object::Invoke

  .method public hidebysig newslot virtual
          instance class [System.Runtime]System.IAsyncResult
          BeginInvoke(string& res,
                      class [System.Runtime]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Del_GenRetType_int_object::BeginInvoke

  .method public hidebysig newslot virtual
          instance class GenRetType<int32,object>
          EndInvoke(string& res,
                    class [System.Runtime]System.IAsyncResult result) runtime managed
  {
  } // end of method Del_GenRetType_int_object::EndInvoke

} // end of class Del_GenRetType_int_object

.class private auto ansi sealed Del_GenRetType<ARG1,ARG2> extends [System.Runtime]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Del_GenRetType::.ctor

  .method public hidebysig newslot virtual
          instance class GenRetType<!ARG1,class Dictionary<string,!ARG2>>
          Invoke(string& res) runtime managed
  {
  } // end of method Del_GenRetType::Invoke

  .method public hidebysig newslot virtual
          instance class [System.Runtime]System.IAsyncResult
          BeginInvoke(string& res,
                      class [System.Runtime]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Del_GenRetType::BeginInvoke

  .method public hidebysig newslot virtual
          instance class GenRetType<!ARG1,class Dictionary<string,!ARG2>>
          EndInvoke(string& res,
                    class [System.Runtime]System.IAsyncResult result) runtime managed
  {
  } // end of method Del_GenRetType::EndInvoke

} // end of class Del_GenRetType

.class private auto ansi sealed Del_NonGenThroughGen2 extends [System.Runtime]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Del_NonGenThroughGen2::.ctor

  .method public hidebysig newslot virtual
          instance class NonGenThroughGen2<object,int32,string>
          Invoke(string& res) runtime managed
  {
  } // end of method Del_NonGenThroughGen2::Invoke

  .method public hidebysig newslot virtual
          instance class [System.Runtime]System.IAsyncResult
          BeginInvoke(string& res,
                      class [System.Runtime]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Del_NonGenThroughGen2::BeginInvoke

  .method public hidebysig newslot virtual
          instance class NonGenThroughGen2<object,int32,string>
          EndInvoke(string& res,
                    class [System.Runtime]System.IAsyncResult result) runtime managed
  {
  } // end of method Del_NonGenThroughGen2::EndInvoke

} // end of class Del_NonGenThroughGen2

.class private auto ansi sealed Del_GenToNonGen1<V> extends [System.Runtime]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Del_GenToNonGen1::.ctor

  .method public hidebysig newslot virtual
          instance class GenToNonGen1<!V,class Dictionary<string,object>>
          Invoke(string& res) runtime managed
  {
  } // end of method Del_GenToNonGen1::Invoke

  .method public hidebysig newslot virtual
          instance class [System.Runtime]System.IAsyncResult
          BeginInvoke(string& res,
                      class [System.Runtime]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Del_GenToNonGen1::BeginInvoke

  .method public hidebysig newslot virtual
          instance class GenToNonGen1<!V,class Dictionary<string,object>>
          EndInvoke(string& res,
                    class [System.Runtime]System.IAsyncResult result) runtime managed
  {
  } // end of method Del_GenToNonGen1::EndInvoke

} // end of class Del_GenToNonGen1

.class private auto ansi sealed Del_NonGenericDerived1 extends [System.Runtime]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Del_NonGenericDerived1::.ctor

  .method public hidebysig newslot virtual
          instance class NonGenericDerived1<int32,object>
          Invoke(string& res) runtime managed
  {
  } // end of method Del_NonGenericDerived1::Invoke

  .method public hidebysig newslot virtual
          instance class [System.Runtime]System.IAsyncResult
          BeginInvoke(string& res,
                      class [System.Runtime]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Del_NonGenericDerived1::BeginInvoke

  .method public hidebysig newslot virtual
          instance class NonGenericDerived1<int32,object>
          EndInvoke(string& res,
                    class [System.Runtime]System.IAsyncResult result) runtime managed
  {
  } // end of method Del_NonGenericDerived1::EndInvoke

} // end of class Del_NonGenericDerived1

.class private auto ansi sealed Del_GenDerive1<U,V> extends [System.Runtime]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Del_GenDerive1::.ctor

  .method public hidebysig newslot virtual
          instance class GenDerive1<!V,class Dictionary<string,!U>>
          Invoke(string& res) runtime managed
  {
  } // end of method Del_GenDerive1::Invoke

  .method public hidebysig newslot virtual
          instance class [System.Runtime]System.IAsyncResult
          BeginInvoke(string& res,
                      class [System.Runtime]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Del_GenDerive1::BeginInvoke

  .method public hidebysig newslot virtual
          instance class GenDerive1<!V,class Dictionary<string,!U>>
          EndInvoke(string& res,
                    class [System.Runtime]System.IAsyncResult result) runtime managed
  {
  } // end of method Del_GenDerive1::EndInvoke

} // end of class Del_GenDerive1

.class private auto ansi sealed Del_NonGenThroughGen4 extends [System.Runtime]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Del_NonGenThroughGen4::.ctor

  .method public hidebysig newslot virtual
          instance class NonGenThroughGen4
          Invoke(string& res) runtime managed
  {
  } // end of method Del_NonGenThroughGen4::Invoke

  .method public hidebysig newslot virtual
          instance class [System.Runtime]System.IAsyncResult
          BeginInvoke(string& res,
                      class [System.Runtime]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Del_NonGenThroughGen4::BeginInvoke

  .method public hidebysig newslot virtual
          instance class NonGenThroughGen4
          EndInvoke(string& res,
                    class [System.Runtime]System.IAsyncResult result) runtime managed
  {
  } // end of method Del_NonGenThroughGen4::EndInvoke

} // end of class Del_NonGenThroughGen4

.class private auto ansi sealed Del_GenToNonGen3<U> extends [System.Runtime]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Del_GenToNonGen3::.ctor

  .method public hidebysig newslot virtual
          instance class GenToNonGen3<!U,object>
          Invoke(string& res) runtime managed
  {
  } // end of method Del_GenToNonGen3::Invoke

  .method public hidebysig newslot virtual
          instance class [System.Runtime]System.IAsyncResult
          BeginInvoke(string& res,
                      class [System.Runtime]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Del_GenToNonGen3::BeginInvoke

  .method public hidebysig newslot virtual
          instance class GenToNonGen3<!U,object>
          EndInvoke(string& res,
                    class [System.Runtime]System.IAsyncResult result) runtime managed
  {
  } // end of method Del_GenToNonGen3::EndInvoke

} // end of class Del_GenToNonGen3

.class private auto ansi sealed Del_NonGenericDerived4 extends [System.Runtime]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Del_NonGenericDerived4::.ctor

  .method public hidebysig newslot virtual
          instance class NonGenericDerived4
          Invoke(string& res) runtime managed
  {
  } // end of method Del_NonGenericDerived4::Invoke

  .method public hidebysig newslot virtual
          instance class [System.Runtime]System.IAsyncResult
          BeginInvoke(string& res,
                      class [System.Runtime]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Del_NonGenericDerived4::BeginInvoke

  .method public hidebysig newslot virtual
          instance class NonGenericDerived4
          EndInvoke(string& res,
                    class [System.Runtime]System.IAsyncResult result) runtime managed
  {
  } // end of method Del_NonGenericDerived4::EndInvoke

} // end of class Del_NonGenericDerived4

.class private auto ansi sealed Del_GenDerive3<U,V> extends [System.Runtime]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Del_GenDerive3::.ctor

  .method public hidebysig newslot virtual
          instance class GenDerive3<!U,!V>
          Invoke(string& res) runtime managed
  {
  } // end of method Del_GenDerive3::Invoke

  .method public hidebysig newslot virtual
          instance class [System.Runtime]System.IAsyncResult
          BeginInvoke(string& res,
                      class [System.Runtime]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Del_GenDerive3::BeginInvoke

  .method public hidebysig newslot virtual
          instance class GenDerive3<!U,!V>
          EndInvoke(string& res,
                    class [System.Runtime]System.IAsyncResult result) runtime managed
  {
  } // end of method Del_GenDerive3::EndInvoke

} // end of class Del_GenDerive3

.class private auto ansi sealed Del_GenRetType_U_V<U,V> extends [System.Runtime]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Del_GenRetType_U_V::.ctor

  .method public hidebysig newslot virtual
          instance class GenRetType<!U,!V>
          Invoke(string& res) runtime managed
  {
  } // end of method Del_GenRetType_U_V::Invoke

  .method public hidebysig newslot virtual
          instance class [System.Runtime]System.IAsyncResult
          BeginInvoke(string& res,
                      class [System.Runtime]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Del_GenRetType_U_V::BeginInvoke

  .method public hidebysig newslot virtual
          instance class GenRetType<!U,!V>
          EndInvoke(string& res,
                    class [System.Runtime]System.IAsyncResult result) runtime managed
  {
  } // end of method Del_GenRetType_U_V::EndInvoke

} // end of class Del_GenRetType_U_V

.class private auto ansi sealed Del_C extends [System.Runtime]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method Del_C::.ctor

  .method public hidebysig newslot virtual
          instance class C  Invoke(string& res) runtime managed
  {
  } // end of method Del_C::Invoke

  .method public hidebysig newslot virtual
          instance class [System.Runtime]System.IAsyncResult
          BeginInvoke(string& res,
                      class [System.Runtime]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method Del_C::BeginInvoke

  .method public hidebysig newslot virtual
          instance class C  EndInvoke(string& res,
                                      class [System.Runtime]System.IAsyncResult result) runtime managed
  {
  } // end of method Del_C::EndInvoke

} // end of class Del_C



// ========================================================================================

.class private auto ansi beforefieldinit CMain extends [System.Runtime]System.Object
{
  .method private hidebysig static bool  CheckResults(string expected,
                                                      string a,
                                                      [opt] string b,
                                                      [opt] string c,
                                                      [opt] string d) cil managed
  {
    .param [3] = nullref
    .param [4] = nullref
    .param [5] = nullref
    // Code size       164 (0xa4)
    .maxstack  2
    .locals init (bool V_0)
    IL_0000:  ldarg.1
    IL_0001:  ldarg.0
    IL_0002:  call       bool [System.Runtime]System.String::op_Equality(string,
                                                                         string)
    IL_0007:  stloc.0
    IL_0008:  ldstr      "EXPECTED: "
    IL_000d:  ldarg.0
    IL_000e:  call       string [System.Runtime]System.String::Concat(string,
                                                                      string)
    IL_0013:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0018:  ldstr      "ACTUAL1 : "
    IL_001d:  ldarg.1
    IL_001e:  call       string [System.Runtime]System.String::Concat(string,
                                                                      string)
    IL_0023:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0028:  ldarg.2
    IL_0029:  call       bool [System.Runtime]System.String::IsNullOrEmpty(string)
    IL_002e:  brtrue.s   IL_004e

    IL_0030:  ldstr      "ACTUAL2 : "
    IL_0035:  ldarg.2
    IL_0036:  call       string [System.Runtime]System.String::Concat(string,
                                                                      string)
    IL_003b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0040:  ldloc.0
    IL_0041:  brfalse.s  IL_004c

    IL_0043:  ldarg.2
    IL_0044:  ldarg.0
    IL_0045:  call       bool [System.Runtime]System.String::op_Equality(string,
                                                                         string)
    IL_004a:  br.s       IL_004d

    IL_004c:  ldc.i4.0
    IL_004d:  stloc.0
    IL_004e:  ldarg.3
    IL_004f:  call       bool [System.Runtime]System.String::IsNullOrEmpty(string)
    IL_0054:  brtrue.s   IL_0074

    IL_0056:  ldstr      "ACTUAL3 : "
    IL_005b:  ldarg.3
    IL_005c:  call       string [System.Runtime]System.String::Concat(string,
                                                                      string)
    IL_0061:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0066:  ldloc.0
    IL_0067:  brfalse.s  IL_0072

    IL_0069:  ldarg.3
    IL_006a:  ldarg.0
    IL_006b:  call       bool [System.Runtime]System.String::op_Equality(string,
                                                                         string)
    IL_0070:  br.s       IL_0073

    IL_0072:  ldc.i4.0
    IL_0073:  stloc.0
    IL_0074:  ldarg.s    d
    IL_0076:  call       bool [System.Runtime]System.String::IsNullOrEmpty(string)
    IL_007b:  brtrue.s   IL_009d

    IL_007d:  ldstr      "ACTUAL4 : "
    IL_0082:  ldarg.s    d
    IL_0084:  call       string [System.Runtime]System.String::Concat(string,
                                                                      string)
    IL_0089:  call       void [System.Console]System.Console::WriteLine(string)
    IL_008e:  ldloc.0
    IL_008f:  brfalse.s  IL_009b

    IL_0091:  ldarg.s    d
    IL_0093:  ldarg.0
    IL_0094:  call       bool [System.Runtime]System.String::op_Equality(string,
                                                                         string)
    IL_0099:  br.s       IL_009c

    IL_009b:  ldc.i4.0
    IL_009c:  stloc.0
    IL_009d:  call       void [System.Console]System.Console::WriteLine()
    IL_00a2:  ldloc.0
    IL_00a3:  ret
  } // end of method Program::CheckResults


  // ============== Test using GenTestType ============== //
  // These test methods will callvirt each method using:
  //    1) The signature from GetBaseType
  //    2) The signature from GenMiddleType with covariant returns (when applicable)
  //    3) The signature from GenTestType with covariant returns
  // And verify that the override on GetTestType is the one that executes

  .method public hidebysig static bool  RunTest1() cil managed noinlining
  {
    .locals init (string res1, string res2, string res3, class GenTestType<int32,object,int32,object> thisPtr)

    newobj      instance void class GenTestType<int32,object,int32,object>::.ctor()
    stloc.s     thisPtr

    ldloc.s     thisPtr
    ldloc.s     thisPtr
    ldvirtftn   instance object class GenBaseType<int32,object>::MyFunc(string&)
    newobj      instance void Del_Obj::.ctor(object, native int)
    ldloca.s    0
    callvirt    instance object Del_Obj::Invoke(string&)
    pop

    ldloc.s     thisPtr
    ldloc.s     thisPtr
    ldvirtftn   instance class GenRetType<!2,!3> class GenTestType<int32,object,int32,object>::MyFunc(string&)
    newobj      instance void class Del_GenRetType_U_V<int32,object>::.ctor(object, native int)
    ldloca.s   1
    callvirt    instance class GenRetType<!0,!1> class Del_GenRetType_U_V<int32,object>::Invoke(string&)
    pop

    ldstr "GenRetType<!U,!V> TestType.MyFunc()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldnull
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  .method public hidebysig static bool  RunTest2() cil managed noinlining
  {
    .locals init (string res1, string res2, string res3, class GenTestType<int32,object,int32,object> thisPtr)

    newobj      instance void class GenTestType<int32,object,int32,object>::.ctor()
    stloc.s     thisPtr

    ldloc.s     thisPtr
    ldloc.s     thisPtr
    ldvirtftn   instance class B class GenBaseType<int32,object>::MyFunc(string&)
    newobj      instance void Del_B::.ctor(object, native int)
    ldloca.s   0
    callvirt    instance class B Del_B::Invoke(string&)
    pop

    ldloc.s     thisPtr
    ldloc.s     thisPtr
    ldvirtftn   instance class C class GenTestType<int32,object,int32,object>::MyFunc(string&)
    newobj      instance void Del_C::.ctor(object, native int)
    ldloca.s   1
    callvirt    instance class C Del_C::Invoke(string&)
    pop

    ldstr "C TestType.MyFunc()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldnull
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  .method public hidebysig static bool  RunTest3() cil managed noinlining
  {
    .locals init (string res1, string res2, string res3, class GenTestType<int32,object,int32,object> thisPtr)

    newobj      instance void class GenTestType<int32,object,int32,object>::.ctor()
    stloc.s     thisPtr

    ldloc.s     thisPtr
    ldloc.s     thisPtr
    ldvirtftn   instance class GenRetType<!0, class Dictionary<string,!1>> class GenBaseType<int32,object>::MyGenFunc(string&)
    newobj      instance void class Del_GenRetType<int32,object>::.ctor(object, native int)
    ldloca.s   0
    callvirt    instance class GenRetType<!0, class Dictionary<string,!1>> class Del_GenRetType<int32,object>::Invoke(string&)
    pop

    ldloc.s     thisPtr
    ldloc.s     thisPtr
    ldvirtftn   instance class GenDerive1<!1, class Dictionary<string,!0>> class GenMiddleType<object,int32>::MyGenFunc(string&)
    newobj      instance void class Del_GenDerive1<int32,object>::.ctor(object, native int)
    ldloca.s   1
    callvirt    instance class GenDerive1<!1, class Dictionary<string,!0>> class Del_GenDerive1<int32,object>::Invoke(string&)
    pop

    ldloc.s     thisPtr
    ldloc.s     thisPtr
    ldvirtftn   instance class GenDerive3<!2,!3> class GenTestType<int32,object,int32,object>::MyGenFunc(string&)
    newobj      instance void class Del_GenDerive3<int32,object>::.ctor(object, native int)
    ldloca.s   2
    callvirt    instance class GenDerive3<!0,!1> class Del_GenDerive3<int32,object>::Invoke(string&)
    pop

    ldstr "GenDerive3<!U,!V> TestType.MyGenFunc()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldnull
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  .method public hidebysig static bool  RunTest4() cil managed noinlining
  {
    .locals init (string res1, string res2, string res3, class GenTestType<int32,object,int32,object> thisPtr)

    newobj      instance void class GenTestType<int32,object,int32,object>::.ctor()
    stloc.s     thisPtr

    ldloc.s     thisPtr
    ldloc.s     thisPtr
    ldvirtftn   instance class GenRetType<int32,object> class GenBaseType<int32,object>::MyGenFunc(string&)
    newobj      instance void class Del_GenRetType_int_object::.ctor(object, native int)
    ldloca.s   0
    callvirt    instance class GenRetType<int32,object> class Del_GenRetType_int_object::Invoke(string&)
    pop

    ldloc.s     thisPtr
    ldloc.s     thisPtr
    ldvirtftn   instance class NonGenericDerived1<int32,object> class GenMiddleType<object,int32>::MyGenFunc(string&)
    newobj      instance void class Del_NonGenericDerived1::.ctor(object, native int)
    ldloca.s   1
    callvirt    instance class NonGenericDerived1<int32,object> class Del_NonGenericDerived1::Invoke(string&)
    pop

    ldloc.s     thisPtr
    ldloc.s     thisPtr
    ldvirtftn   instance class NonGenericDerived4 class GenTestType<int32,object,int32,object>::MyGenFunc(string&)
    newobj      instance void class Del_NonGenericDerived4::.ctor(object, native int)
    ldloca.s   2
    callvirt    instance class NonGenericDerived4 class Del_NonGenericDerived4::Invoke(string&)
    pop

    ldstr "NonGenericDerived4 TestType.MyGenFunc()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldnull
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  .method public hidebysig static bool  RunTest5() cil managed noinlining
  {
    .locals init (string res1, string res2, string res3, class GenTestType<int32,object,int32,object> thisPtr)

    newobj      instance void class GenTestType<int32,object,int32,object>::.ctor()
    stloc.s     thisPtr

    ldloc.s     thisPtr
    ldloc.s     thisPtr
    ldvirtftn   instance class B class GenBaseType<int32,object>::GenToNonGen(string&)
    newobj      instance void class Del_B::.ctor(object, native int)
    ldloca.s   0
    callvirt    instance class B class Del_B::Invoke(string&)
    pop

    ldloc.s     thisPtr
    ldloc.s     thisPtr
    ldvirtftn   instance class GenToNonGen1<!1, class Dictionary<string,object>> class GenMiddleType<object,int32>::GenToNonGen(string&)
    newobj      instance void class Del_GenToNonGen1<int32>::.ctor(object, native int)
    ldloca.s   1
    callvirt    instance class GenToNonGen1<!0, class Dictionary<string,object>> class Del_GenToNonGen1<int32>::Invoke(string&)
    pop

    ldloc.s     thisPtr
    ldloc.s     thisPtr
    ldvirtftn   instance class GenToNonGen3<!2,object> class GenTestType<int32,object,int32,object>::GenToNonGen(string&)
    newobj      instance void class Del_GenToNonGen3<int32>::.ctor(object, native int)
    ldloca.s   2
    callvirt    instance class GenToNonGen3<!0,object> class Del_GenToNonGen3<int32>::Invoke(string&)
    pop

    ldstr "GenToNonGen3<!U,object> TestType.GenToNonGen()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldnull
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  .method public hidebysig static bool  RunTest6() cil managed noinlining
  {
    .locals init (string res1, string res2, string res3, class GenTestType<int32,object,int32,object> thisPtr)

    newobj      instance void class GenTestType<int32,object,int32,object>::.ctor()
    stloc.s     thisPtr

    ldloc.s     thisPtr
    ldloc.s     thisPtr
    ldvirtftn   instance class B class GenBaseType<int32,object>::NonGenThroughGenFunc(string&)
    newobj      instance void class Del_B::.ctor(object, native int)
    ldloca.s   0
    callvirt    instance class B class Del_B::Invoke(string&)
    pop

    ldloc.s     thisPtr
    ldloc.s     thisPtr
    ldvirtftn   instance class NonGenThroughGen2<object,int32,string> class GenMiddleType<object,int32>::NonGenThroughGenFunc(string&)
    newobj      instance void class Del_NonGenThroughGen2::.ctor(object, native int)
    ldloca.s   1
    callvirt    instance class NonGenThroughGen2<object,int32,string> class Del_NonGenThroughGen2::Invoke(string&)
    pop

    ldloc.s     thisPtr
    ldloc.s     thisPtr
    ldvirtftn   instance class NonGenThroughGen4 class GenTestType<int32,object,int32,object>::NonGenThroughGenFunc(string&)
    newobj      instance void class Del_NonGenThroughGen4::.ctor(object, native int)
    ldloca.s   2
    callvirt    instance class NonGenThroughGen4 class Del_NonGenThroughGen4::Invoke(string&)
    pop

    ldstr "NonGenThroughGen4 TestType.NonGenThroughGenFunc()"
    ldloc.0
    ldloc.1
    ldloc.2
    ldnull
    call bool CMain::CheckResults(string,string,string,string,string)
    ret
  }

  .method private hidebysig static int32 Main(string[] args) cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  2
    .locals init ( bool result )

    ldc.i4.1
    stloc.0

    T1:
    call bool CMain::RunTest1()
    brtrue.s T2
    ldc.i4.0
    stloc.0

    T2:
    call bool CMain::RunTest2()
    brtrue.s T3
    ldc.i4.0
    stloc.0

    T3:
    call bool CMain::RunTest3()
    brtrue.s T4
    ldc.i4.0
    stloc.0

    T4:
    call bool CMain::RunTest4()
    brtrue.s T5
    ldc.i4.0
    stloc.0

    T5:
    call bool CMain::RunTest5()
    brtrue.s T6
    ldc.i4.0
    stloc.0

    T6:
    call bool CMain::RunTest6()
    brtrue.s DONE
    ldc.i4.0
    stloc.0

    // ===================================================================================== //

    DONE:
    ldloc.0
    brtrue.s   PASS

    ldstr      "Test FAILED"
    call       void [System.Console]System.Console::WriteLine(string)
    ldc.i4.s   101
    ret

    PASS:
    ldstr      "Test PASSED"
    call       void [System.Console]System.Console::WriteLine(string)
    ldc.i4.s   100
    ret

    ldc.i4.s   100
    ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void [System.Runtime]System.Object::.ctor()
    ret
  }
}
