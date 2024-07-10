# Generics

Terminology
-----------

Type/Method instantiation == Type/Method instance == Inflated Type/Method.

Generic Type Definitions
------------------------

These are represented by a normal `MonoClass` structure with the `generic_container` field set. This field points to a `MonoGenericContainer` structure, which stores information about the generic parameters of the generic type.

Generic Type Instantiations
---------------------------

These are represented by a pair of `MonoGenericClass` and `MonoClass` structures. The `generic_class` field in MonoClass is used to link the two together. The reason for the split is to avoid allocating a large MonoClass if not needed.

It would have been better to name `MonoGenericClass` `MonoInflatedClass` or something similar.

Generic Method Definitions
--------------------------

These are represented by a `MonoMethod` structure with the `is_generic` field set to 1.

Generic Method Instantiations
-----------------------------

These are represented by a `MonoMethodInflated` structure, which is an extension of the `MonoMethod` structure. Its `is_inflated` field is set to 1.

One consequence of this design is that a method cannot be a pinvoke method/wrapper/dynamic method and an inflated method at the same time.

MonoGenericContext
------------------

This structure holds information of an instantiation of a set of generic parameters with generic arguments. It is used by both type and method instatiations.

Canonical generic instances
---------------------------

The runtime canonizes generic type/method instances, so for every set of generic arguments, there is only one type/method instance with those arguments. This is using caches in `metadata.c`.

Lifetime of inflated types/methods
----------------------------------

Inflated types and methods depend on the assembly of the generic type/method definition they are inflated from, along with the assemblies of their generic arguments. This is handled using the concept of 'image sets' in metadata.c. Every inflated type/method belongs to an image set, which is a set of MonoImages. When one of the assemblies in an image set is unloaded, all the inflated types/methods belonging to the image set are freed. Memory for inflated types/methods cannot be allocated from mempools, it is allocated from the heap. The `mono_class_alloc/alloc0` functions can be used to allocate memory from the appropriate place.

System.Reflection.Emit
----------------------

Generics support in System.Reflection.Emit (SRE) is very problematic because it is possible to create generic instances of not yet created dynamic types, i.e. if T is a generic TypeBuilder, it is possible to create T\<int\>. The latter is not a TypeBuilder any more, but a normal Type, which presents several problems:

-   this type needs to be kept in sync with the original TypeBuilder, i.e. if methods/fields are added to the TypeBuilder, this should be reflected in the instantiation.
-   this type cannot be used normally until its TypeBuilder is finished, ie. its not possible to create instances of it etc.

These problems are currently handled by a hierarchy of C# classes which inherit from the normal reflection classes:

-   `MonoGenericClass` represents an instantiation of a generic TypeBuilder. MS.NET calls this `TypeBuilderInstantiation`, a much better name.
-   `Method/Field/Event/PropertyOnTypeBuilderInst` represents a method/field etc. of a `MonoGenericClass`.
