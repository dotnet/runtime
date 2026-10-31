// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A)
  .ver 4:0:0:0
}

.assembly System.Reflection.Emit.TestModifierOrder
{
  .ver 1:0:0:0
}

.module System.Reflection.Emit.TestModifierOrder.dll

// Interface methods whose signatures have optional modifiers before required ones, at the top level and nested.
// ilasm writes a type's modifiers in the reverse of the order they are listed here, so "int32 modreq(ModB)
// modopt(ModA)" is encoded with modopt(ModA) first.

.class public auto ansi beforefieldinit ModifierOrder.ModA extends [System.Runtime]System.Object { }
.class public auto ansi beforefieldinit ModifierOrder.ModB extends [System.Runtime]System.Object { }
.class public auto ansi beforefieldinit ModifierOrder.ModC extends [System.Runtime]System.Object { }
.class public auto ansi beforefieldinit ModifierOrder.ModD extends [System.Runtime]System.Object { }

.class public auto ansi beforefieldinit ModifierOrder.G`1<T> extends [System.Runtime]System.Object
{
  .field public !T f
}

.class interface public abstract auto ansi ModifierOrder.ITop
{
  .method public hidebysig newslot abstract virtual instance int32 M(int32 modreq(ModifierOrder.ModD) modopt(ModifierOrder.ModC) modreq(ModifierOrder.ModB) modopt(ModifierOrder.ModA) p) cil managed { }
}

.class interface public abstract auto ansi ModifierOrder.IPointer
{
  .method public hidebysig newslot abstract virtual instance int32 M(int32 modreq(ModifierOrder.ModB) modopt(ModifierOrder.ModA) * p) cil managed { }
}

.class interface public abstract auto ansi ModifierOrder.IByRef
{
  .method public hidebysig newslot abstract virtual instance int32 M(int32 modreq(ModifierOrder.ModB) modopt(ModifierOrder.ModA) & p) cil managed { }
}

.class interface public abstract auto ansi ModifierOrder.IArray
{
  .method public hidebysig newslot abstract virtual instance int32 M(int32 modreq(ModifierOrder.ModB) modopt(ModifierOrder.ModA) [] p) cil managed { }
}

.class interface public abstract auto ansi ModifierOrder.IGenericArgument
{
  .method public hidebysig newslot abstract virtual instance int32 M(class ModifierOrder.G`1<int32 modreq(ModifierOrder.ModB) modopt(ModifierOrder.ModA)> p) cil managed { }
}

.class interface public abstract auto ansi ModifierOrder.IReturnPointer
{
  .method public hidebysig newslot abstract virtual instance int32 modreq(ModifierOrder.ModB) modopt(ModifierOrder.ModA) * M() cil managed { }
}

.class interface public abstract auto ansi ModifierOrder.ITwoLevels
{
  .method public hidebysig newslot abstract virtual instance int32 M(int32 modreq(ModifierOrder.ModB) modopt(ModifierOrder.ModA) * modreq(ModifierOrder.ModD) modopt(ModifierOrder.ModC) p) cil managed { }
}

// Enough type definitions that ModE and ModF have TypeDef rows of at least 32, so their compressed tokens in a
// signature take two bytes.

.class public auto ansi beforefieldinit ModifierOrder.Filler01 extends [System.Runtime]System.Object { }
.class public auto ansi beforefieldinit ModifierOrder.Filler02 extends [System.Runtime]System.Object { }
.class public auto ansi beforefieldinit ModifierOrder.Filler03 extends [System.Runtime]System.Object { }
.class public auto ansi beforefieldinit ModifierOrder.Filler04 extends [System.Runtime]System.Object { }
.class public auto ansi beforefieldinit ModifierOrder.Filler05 extends [System.Runtime]System.Object { }
.class public auto ansi beforefieldinit ModifierOrder.Filler06 extends [System.Runtime]System.Object { }
.class public auto ansi beforefieldinit ModifierOrder.Filler07 extends [System.Runtime]System.Object { }
.class public auto ansi beforefieldinit ModifierOrder.Filler08 extends [System.Runtime]System.Object { }
.class public auto ansi beforefieldinit ModifierOrder.Filler09 extends [System.Runtime]System.Object { }
.class public auto ansi beforefieldinit ModifierOrder.Filler10 extends [System.Runtime]System.Object { }
.class public auto ansi beforefieldinit ModifierOrder.Filler11 extends [System.Runtime]System.Object { }
.class public auto ansi beforefieldinit ModifierOrder.Filler12 extends [System.Runtime]System.Object { }
.class public auto ansi beforefieldinit ModifierOrder.Filler13 extends [System.Runtime]System.Object { }
.class public auto ansi beforefieldinit ModifierOrder.Filler14 extends [System.Runtime]System.Object { }
.class public auto ansi beforefieldinit ModifierOrder.Filler15 extends [System.Runtime]System.Object { }
.class public auto ansi beforefieldinit ModifierOrder.Filler16 extends [System.Runtime]System.Object { }
.class public auto ansi beforefieldinit ModifierOrder.Filler17 extends [System.Runtime]System.Object { }
.class public auto ansi beforefieldinit ModifierOrder.Filler18 extends [System.Runtime]System.Object { }
.class public auto ansi beforefieldinit ModifierOrder.Filler19 extends [System.Runtime]System.Object { }
.class public auto ansi beforefieldinit ModifierOrder.Filler20 extends [System.Runtime]System.Object { }

.class public auto ansi beforefieldinit ModifierOrder.ModE extends [System.Runtime]System.Object { }
.class public auto ansi beforefieldinit ModifierOrder.ModF extends [System.Runtime]System.Object { }

.class interface public abstract auto ansi ModifierOrder.ITwoByteTokens
{
  .method public hidebysig newslot abstract virtual instance int32 M(int32 modreq(ModifierOrder.ModB) modopt(ModifierOrder.ModA) modreq(ModifierOrder.ModF) modopt(ModifierOrder.ModE) p) cil managed { }
}
