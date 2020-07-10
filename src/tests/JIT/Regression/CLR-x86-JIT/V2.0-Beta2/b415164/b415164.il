// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ctor_null1 { }
 
.class private auto ansi sealed MyDelegate
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native unsigned int 'method') runtime managed { }
  .method public hidebysig virtual instance void
          Invoke() runtime managed { }
  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed { }
  .method public hidebysig newslot virtual 
          instance void  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed { }
}

.class public auto ansi explicit beforefieldinit TestClass
       extends [mscorlib]System.Object
{
  .field [0xFFFFF0] string m_strField
  .method public hidebysig instance void DelegateFunc() il managed
  {
    ldarg.0
    ldfld string TestClass::m_strField
    call  void [System.Console]System.Console::WriteLine(string)
    ret
  }
  .method public hidebysig static int32 Main(class System.String[] args) il managed
  {
    .entrypoint
    .maxstack  3
    .locals init (class MyDelegate V_0)
    		.try {
    			ldnull
    			ldftn      instance void TestClass::DelegateFunc()
    			newobj     instance void MyDelegate::.ctor(object,
                                                         native unsigned int)
			ldstr 	   "ArgumentException NOT thrown, Test FAILED"
			call       void [System.Console]System.Console::WriteLine(string)
			leave.s FAILURE
		} catch [mscorlib]System.ArgumentException
		{
			ldstr      "ArgumentException thrown, Test SUCCESS"
			call       void [System.Console]System.Console::WriteLine(string)
                        leave.s SUCCESS
		}

SUCCESS:
      ldc.i4 100
      ret
 
FAILURE:
      ldc.i4 101
      ret
  }
  .method public hidebysig specialname rtspecialname 
          instance void .ctor() il managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }
}
