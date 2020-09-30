// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.class public auto ansi beforefieldinit StaticFieldLayout.RvaStatics
       extends [CoreTestAssembly]System.Object
{
  .field private static int32 StaticInitedInt at D_00004000
  .data D_00004000 = bytearray (12 34 56 78) 
}

.class public auto ansi beforefieldinit StaticFieldLayout.FunctionPointerType
       extends [CoreTestAssembly]System.Object
{
  .field private static int32 StaticIntField;
  .field private static method object *(object) StaticMethodField;
}
