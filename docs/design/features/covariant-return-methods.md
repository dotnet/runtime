# Covariant Return Methods

Covariant return methods is a runtime feature designed to support the [covariant return types](https://github.com/dotnet/csharplang/blob/master/proposals/covariant-returns.md) and [records](https://github.com/dotnet/csharplang/blob/master/proposals/records.md) C# language features posed for C# 9.0.

This feature allows an overriding method to have a more derived reference type than the method it overrides. Covariant return methods can only be described through MethodImpl records, and as an initial implementation, will have the following limitations:
1. Covariant return methods will only be applicable to methods on reference types: the MethodDecl and MethodImpl records can only be on reference types. Methods on interfaces will not be supported.
2. Return types in covariant return methods can only be reference types: covariant interface return types are not supported.

Supporting interfaces comes with many complications (ex: interface equivalence, default interface methods, variance on generic interfaces, etc...), which is why the feature will initially only support classes.

MethodImpl checking will allow a return type to vary as long as the override is compatible with the return type of the method overriden (i.e. a derived type).

If a language wishes for the override to be semantically visible such that users of the more derived type may rely on the covariant return type it shall make the override a newslot method with appropriate visibility AND name to be used outside of the class.

For virtual method slot MethodImpl overrides, each slot shall be checked for compatible signature on type load. (Implementation note: This behavior can be triggered only if the type has a covariant return type override in its hierarchy, so as to make this pay for play.)

A new `ValidateMethodImplRemainsInEffectAttribute` shall be added. The presence of this attribute is to require the type loader to ensure that the MethodImpl records specified on the method have not lost their slot unifying behavior due to other actions. In other words, when a MethodImpl on type A overrides some method using a derived return type in the signature, any type deriving from A will be allowed to have a MethodImpl record that overrides the same method as long as the return type used in the signature is the same or more derived than the return type used in the MethodImpl signature on type A. This is used to allow the C# language to require that overrides have the consistent behaviors expected. The expectation is that C# would place this attribute on covariant override methods in classes.

## Implementation Notes

### Signature Checking

Signature checking for MethodImpl is done through the `MetaSig::CompareElementType` method, which is called from various places in the runtime when comparing method signatures. This method compares the signatures of two types, and will now take a boolean flag that would allow for derived type checking behavior. The boolean flag will be set to `TRUE` appropriately during comparison of the return type signatures between a MethodImpl and MethodDecl records.

The type signature checking algorithm will perform the following:
1. Traverse and compare the signatures for `type1` and `type2` recursively.
2. If the signatures mismatch at any given point, and the current element type for `type2` is `ELEMENT_TYPE_CLASS` or `ELEMENT_TYPE_GENERICINST`:
2.a. Check for covariant return eligibility
2.b. Compute the parent type's signature and parent type's generic substitution of `type2`
2.c. Perform a recursive call to re-compare `type1` with the new parent type signature of `type2`.

Note: if `ELEMENT_TYPE_INTERNAL` is encountered in either of the type signatures, both types will be fully loaded and compared for compatibility.

### VTable Slot Checking

This will be done during the call to `SetupMethodTable2` where we propagate inheritance (see methodtablebuilder.cpp around line 10642). TODO: add algorithm description here.

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
