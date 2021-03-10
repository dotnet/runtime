# Covariant Return Methods

Covariant return methods is a runtime feature designed to support the [covariant return types](https://github.com/dotnet/csharplang/blob/master/proposals/csharp-9.0/covariant-returns.md) and [records](https://github.com/dotnet/csharplang/blob/master/proposals/csharp-9.0/records.md) C# language features posed for C# 9.0.

This feature allows an overriding method to have a return type that is different than the one on the method it overrides, but compatible with it. The type compability rules are defined in ECMA I.8.7.1. Example: using a more derived return type.

Covariant return methods can only be described through MethodImpl records, and as an initial implementation will only be applicable to methods on reference types. Methods on interfaces and value types will not be supported (may be supported later in the future).

MethodImpl checking will allow a return type to vary as long as the override is compatible with the return type of the method overriden (ECMA I.8.7.1).

If a language wishes for the override to be semantically visible such that users of the more derived type may rely on the covariant return type it shall make the override a newslot method with appropriate visibility AND name to be used outside of the class.

For virtual method slot MethodImpl overrides, each slot shall be checked for compatible signature on type load. (Implementation note: This behavior can be triggered only if the type has a covariant return type override in its hierarchy, so as to make this pay for play.)

A new `PreserveBaseOverridesAttribute` shall be added. The presence of this attribute is to require the type loader to ensure that the MethodImpl records specified on the method have not lost their slot unifying behavior due to other actions. This is used to allow the C# language to require that overrides have the consistent behaviors expected. The expectation is that C# would place this attribute on covariant override methods in classes.

## Implementation Notes

### Return Type Checking

During enumeration of MethodImpls on a type (`MethodTableBuilder::EnumerateMethodImpls()`), if the signatures of the MethodImpl and the MethodDecl do not match:
1. We repeat the signature comparison a second time, but skip the comparison of the return type signatures. If the signatures for the rest of the method arguments match, we will conditionally treat that MethodImpl as a valid one, but flag it for a closer examination of the return type compatibility at a later stage of type loading (end of `CLASS_LOAD_EXACTPARENTS` stage).
2. At the end of the `CLASS_LOAD_EXACTPARENTS` type loading stage, examing each virtual method on the type, and if it has been flagged for further return type checking:
    + Load the `TypeHandle` of the return type of the method on base type.
    + Load the `TypeHandle` of the return type of the method on the current type being validated.
    + Verify that the second `TypeHandle` is compatible with the first `TypeHandle` using the `MethodTable::CanCastTo()` API. If they are not compatible, a TypeLoadException is thrown.

The only exception where `CanCastTo()` will return true for an incompatible type according to the ECMA rules is for structs implementing interfaces, so we explicitly check for that case and throw a TypeLoadException if we hit it.

Once a method is flagged for return type checking, every time the vtable slot containing that method gets overridden on a derived type, the new override will also be checked for compatiblity. This is to ensure that no derived type can implicitly override some virtual method that has already been overridden by some MethodImpl with a covariant return type.

### VTable Slot Unification

If a MethodImpl has the `PreserveBaseOverridesAttribute` attribute, it needs to propagate all applicable vtable slots on the type. This is to ensure that if we use the signature of one of the base type methods to call the overriding method, we still execute the overriding method.

Consider this case:
``` C#
     class A {
         RetType VirtualFunction() { }
     }
     class B : A {
         [PreserveBaseOverrides]
         DerivedRetType VirtualFunction() { .override A.VirtualFuncion }
     }
     class C : B {
         [PreserveBaseOverrides]
         MoreDerivedRetType VirtualFunction() { .override A.VirtualFunction }
     }
```

Given an object of type `C`, the attribute will ensure that:
``` C#
     callvirt RetType A::VirtualFunc()               -> executes the MethodImpl on C
     callvirt DerivedRetType B::VirtualFunc()        -> executes the MethodImpl on C
     callvirt MoreDerivedRetType C::VirtualFunc()    -> executes the MethodImpl on C
```

Without the attribute, the second callvirt would normally execute the MethodImpl on `B` (the MethodImpl on `C` does not override the vtable slot of `B`'s MethodImpl, but only overrides the declaring method's vtable slot.

This slot unification step will also take place during the last step of type loading (end of `CLASS_LOAD_EXACTPARENTS` stage).

### [Future] Interface Support

An interface method may be both non-final and have a MethodImpl that declares that it overrides another interface method. If it does, NO other interface method may .override it. Instead further overrides must override the method that it overrode. Also the overriding method may only override 1 method.

The default interface method resolution algorithm shall change from:

``` console
Given interface method M and type T.
Let MSearch = M
Let MFound = Most specific implementation within the interfaces for MSearch within type T. If multiple implementations are found, throw Ambiguous match exception.
Return MFound
```

To:

``` console
Given interface method M and type T.
Let MSearch = M

If (MSearch overrides another method MBase)
    Let MSearch = MBase

Let MFound = Most specific implementation within the interfaces for MSearch within type T. If multiple implementations are found, throw Ambiguous match exception.
Let M Code = NULL

If ((MFound != Msearch) and (MFound is not final))
    Let M ClassVirtual = ResolveInterfaceMethod for MFound to virtual override on class T without using Default interface method implementation or return NULL if not found.
    If (M ClassVirtual != NULL)
        Let M Code= ResolveVirtualMethod for MFound on class T to implementation method

If (M Code != NULL)
    Let M Code = MFound

Check M Code For signature <compatible-with> interface method M.

Return M Code
```
