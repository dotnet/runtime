// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { }
.assembly extern helperdll { }

.assembly helperildll {}
.module helperildll.dll

.class auto ansi public beforefieldinit HelperILCode
       extends [mscorlib]System.Object
{
  .method public hidebysig static native int GetFunctionPointerFromOtherModule() cil managed noinlining
  {
     ldftn      void [helperdll]HelperClass::FunctionPointerReferencedMethod()
     ret
  }
  .method public hidebysig static void CallFunctionPointer(native int) cil managed noinlining
  {
     ldarg.0
     calli void()
     ret
  }
}
