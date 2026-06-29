// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This assembly provides closed-type hierarchies annotated with
// System.Runtime.CompilerServices.IsClosedTypeAttribute. That attribute is reserved for
// compiler use (applying it in C# source is a hard CS8335 error), and the 'closed' keyword
// that would make the C# compiler emit it does not exist yet, so the metadata is
// hand-authored in IL here. It is consumed by the reflection-based closed-type polymorphism
// inference tests in System.Text.Json.Tests.
//
// The IsClosedTypeAttribute blob carries a single named property 'DerivedTypes' of type
// System.Type[]. The fixture types are intentionally property-less: the tests assert on the
// emitted '$type' discriminator and the deserialized runtime type, which is sufficient to
// prove that polymorphism was inferred.

.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A)
}

.assembly System.Text.Json.ClosedTypeTestFixtures
{
  .ver 1:0:0:0
}

.module System.Text.Json.ClosedTypeTestFixtures.dll

.namespace System.Text.Json.Serialization.Tests
{
  // ---- Basic closed hierarchy ----------------------------------------------------------
  .class public abstract auto ansi beforefieldinit ClosedShape
         extends [System.Runtime]System.Object
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsClosedTypeAttribute::.ctor() = {
      property type[] 'DerivedTypes' = type[2](System.Text.Json.Serialization.Tests.ClosedCircle System.Text.Json.Serialization.Tests.ClosedSquare)
    }
    .method family hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
      .maxstack 8
      ldarg.0
      call instance void [System.Runtime]System.Object::.ctor()
      ret
    }
  }

  .class public auto ansi sealed beforefieldinit ClosedCircle
         extends System.Text.Json.Serialization.Tests.ClosedShape
  {
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
      .maxstack 8
      ldarg.0
      call instance void System.Text.Json.Serialization.Tests.ClosedShape::.ctor()
      ret
    }
  }

  .class public auto ansi sealed beforefieldinit ClosedSquare
         extends System.Text.Json.Serialization.Tests.ClosedShape
  {
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
      .maxstack 8
      ldarg.0
      call instance void System.Text.Json.Serialization.Tests.ClosedShape::.ctor()
      ret
    }
  }

  // ---- Empty closed hierarchy (inference must stay inert) ------------------------------
  .class public abstract auto ansi beforefieldinit EmptyClosedBase
         extends [System.Runtime]System.Object
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsClosedTypeAttribute::.ctor() = (
      01 00 00 00
    )
    .method family hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
      .maxstack 8
      ldarg.0
      call instance void [System.Runtime]System.Object::.ctor()
      ret
    }
  }

  .class public auto ansi sealed beforefieldinit EmptyClosedDerived
         extends System.Text.Json.Serialization.Tests.EmptyClosedBase
  {
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
      .maxstack 8
      ldarg.0
      call instance void System.Text.Json.Serialization.Tests.EmptyClosedBase::.ctor()
      ret
    }
  }

  // ---- Open generic closed hierarchy ---------------------------------------------------
  .class public abstract auto ansi beforefieldinit ClosedContainer`1<T>
         extends [System.Runtime]System.Object
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsClosedTypeAttribute::.ctor() = {
      property type[] 'DerivedTypes' = type[1](System.Text.Json.Serialization.Tests.ClosedBox`1)
    }
    .method family hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
      .maxstack 8
      ldarg.0
      call instance void [System.Runtime]System.Object::.ctor()
      ret
    }
  }

  .class public auto ansi sealed beforefieldinit ClosedBox`1<T>
         extends class System.Text.Json.Serialization.Tests.ClosedContainer`1<!T>
  {
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
      .maxstack 8
      ldarg.0
      call instance void class System.Text.Json.Serialization.Tests.ClosedContainer`1<!T>::.ctor()
      ret
    }
  }

  // ---- Discriminator collision (two derived types share the simple name 'Node') --------
  .class public abstract auto ansi beforefieldinit ClosedCollisionBase
         extends [System.Runtime]System.Object
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsClosedTypeAttribute::.ctor() = {
      property type[] 'DerivedTypes' = type[2](System.Text.Json.Serialization.Tests.ClosedCollisionHolderA/Node System.Text.Json.Serialization.Tests.ClosedCollisionHolderB/Node)
    }
    .method family hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
      .maxstack 8
      ldarg.0
      call instance void [System.Runtime]System.Object::.ctor()
      ret
    }
  }

  .class public abstract auto ansi sealed beforefieldinit ClosedCollisionHolderA
         extends [System.Runtime]System.Object
  {
    .class nested public auto ansi sealed beforefieldinit Node
           extends System.Text.Json.Serialization.Tests.ClosedCollisionBase
    {
      .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
      {
        .maxstack 8
        ldarg.0
        call instance void System.Text.Json.Serialization.Tests.ClosedCollisionBase::.ctor()
        ret
      }
    }
  }

  .class public abstract auto ansi sealed beforefieldinit ClosedCollisionHolderB
         extends [System.Runtime]System.Object
  {
    .class nested public auto ansi sealed beforefieldinit Node
           extends System.Text.Json.Serialization.Tests.ClosedCollisionBase
    {
      .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
      {
        .maxstack 8
        ldarg.0
        call instance void System.Text.Json.Serialization.Tests.ClosedCollisionBase::.ctor()
        ret
      }
    }
  }

  // ---- Accessibility rejection (public base, internal derived) -------------------------
  .class public abstract auto ansi beforefieldinit PublicClosedBase
         extends [System.Runtime]System.Object
  {
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsClosedTypeAttribute::.ctor() = {
      property type[] 'DerivedTypes' = type[2](System.Text.Json.Serialization.Tests.PublicClosedDerived System.Text.Json.Serialization.Tests.InternalClosedDerived)
    }
    .method family hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
      .maxstack 8
      ldarg.0
      call instance void [System.Runtime]System.Object::.ctor()
      ret
    }
  }

  .class public auto ansi sealed beforefieldinit PublicClosedDerived
         extends System.Text.Json.Serialization.Tests.PublicClosedBase
  {
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
      .maxstack 8
      ldarg.0
      call instance void System.Text.Json.Serialization.Tests.PublicClosedBase::.ctor()
      ret
    }
  }

  .class private auto ansi sealed beforefieldinit InternalClosedDerived
         extends System.Text.Json.Serialization.Tests.PublicClosedBase
  {
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
      .maxstack 8
      ldarg.0
      call instance void System.Text.Json.Serialization.Tests.PublicClosedBase::.ctor()
      ret
    }
  }
}
