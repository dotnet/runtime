// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Runtime.CompilerServices.VisualC { }
.assembly CopyCtorUtil { }

.class auto ansi sealed public TestDelegate
       extends [mscorlib]System.MulticastDelegate
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.CallingConvention) = ( 01 00 03 00 00 00 00 00 ) 
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method TestDelegate::.ctor

  .method public hidebysig newslot virtual 
          instance int32  Invoke(valuetype [CopyCtorUtil]StructWithCtor modreq([mscorlib]System.Runtime.CompilerServices.IsCopyConstructed)* ptrStruct,
                                 valuetype [CopyCtorUtil]StructWithCtor modreq([mscorlib]System.Runtime.CompilerServices.IsCopyConstructed)& refStruct) runtime managed
  {
  } // end of method TestDelegate::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(valuetype [CopyCtorUtil]StructWithCtor modreq([mscorlib]System.Runtime.CompilerServices.IsCopyConstructed)* ptrStruct,
                      valuetype [CopyCtorUtil]StructWithCtor modreq([mscorlib]System.Runtime.CompilerServices.IsCopyConstructed)& refStruct,
                      class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method TestDelegate::BeginInvoke

  .method public hidebysig newslot virtual 
          instance int32  EndInvoke(valuetype [CopyCtorUtil]StructWithCtor modreq([mscorlib]System.Runtime.CompilerServices.IsCopyConstructed)& refStruct,
                                    class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method TestDelegate::EndInvoke

} // end of class TestDelegate

.class public sealed sequential ansi beforefieldinit StructWithCtor
       extends [mscorlib]System.ValueType
{
    .field public int32 _instanceField
    .field public static int32 CopyCtorCallCount
    .field public static int32 DtorCallCount

    .method public specialname static void  '<MarshalCopy>'(valuetype StructWithCtor* A_0,
                                                            valuetype StructWithCtor* A_1) cil managed
    {
        ldarg.0
        ldarg.1
        ldfld int32 StructWithCtor::_instanceField
        stfld int32 StructWithCtor::_instanceField

        ldsfld int32 StructWithCtor::CopyCtorCallCount
        ldc.i4.1
        add
        stsfld int32 StructWithCtor::CopyCtorCallCount
        ret
    }

    .method public specialname static void  '<MarshalDestroy>'(valuetype StructWithCtor* A_0) cil managed
    {
        ldsfld int32 StructWithCtor::DtorCallCount
        ldc.i4.1
        add
        stsfld int32 StructWithCtor::DtorCallCount
        ret
    }
}

.class public auto ansi beforefieldinit FunctionPointer
       extends [mscorlib]System.Object
{
    .method hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }

    .method public hidebysig static int32  Call_FunctionPointer(native int fptr,
                                                 valuetype StructWithCtor* arg1,
                                                 valuetype StructWithCtor& arg2) cil managed
    {
        .maxstack  8
        ldarg.1
        ldarg.2
        ldarg.0
        calli unmanaged stdcall int32 (valuetype StructWithCtor modreq([mscorlib]System.Runtime.CompilerServices.IsCopyConstructed)*, valuetype StructWithCtor modreq([mscorlib]System.Runtime.CompilerServices.IsCopyConstructed)&)
        ret
    }
}


