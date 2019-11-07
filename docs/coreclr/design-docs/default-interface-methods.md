# Default interface methods

Default interface methods is a runtime feature designed to support the [default interface methods](https://github.com/dotnet/csharplang/blob/21dc9561aeffc87a31da44588ce7ed6930ee3333/proposals/default-interface-methods.md) C# language feature posed for C# 8.0.

The major changes are:

* Interfaces are now allowed to have instance methods (both virtual and non-virtual). Previously we only allowed abstract virtual methods.
  * Interfaces obviously still can't have instance fields.
* Interface methods are allowed to MethodImpl other interface methods the interface _requires_ (but we require the `MethodImpl`s to be final to keep things simple) - i.e. an interface is allowed to provide (or override) an implementation of another interface's method

This speclet attempts to formalize the major places within the [ECMA-335 specification](https://www.ecma-international.org/publications/standards/Ecma-335.htm) that would need updating, should we rev the spec in the future.

It doesn't attempt to be an exhaustive list - there are many places within the spec that mention interfaces being just contracts that don't define implementation. This list should be complete enough to list places where interesting _implementation differences_ happen.

## Changes to the ECMA-335 specification

**Section** "I.8.5.3.2 Accessibility of members and nested types" is extended so that the definition of "referents that support the same type" includes "an exact type and all of the types that inherit from it, or implement it as an interface (either explicitly or implicitly)".

Examples:
`class Base : IFoo {}` / `class Derived : Base {}`: `Base` can access protected members of `IFoo`. `Derived` can also access protected members of `IFoo` because it inherits the interface.
`class Outer : IFoo { class Nested { } }`: `Nested` can access protected members of `IFoo`.
`interface IBar : IFoo { }`: `IBar` can access protected members of `IFoo` (same rules as for classes)

TODO: since we now allow protected/internal members on interfaces, do we need to adjust the existing interface method resolution algorithm to do accessibility checks (can a method in a class that can't access the interface method implement the method)? CoreCLR seems to let us do things like override internal methods from a different assembly so this doesn't seem to be enforced for classes either.

**Section** "I.8.9.5 Class type definition" [Note: the section on type initializers within the spec only seems to apply to object types and value types, not to interfaces, but the CLR has historically supported running .cctors when accessing static members of interfaces and the spec does mention interface type initializers as well. We might want to move the part about type initializers out of the section. End note.] The semantics of when and what triggers the execution of type initialization methods will be updated so that we support the strict semantic of type initializers when executing instance methods on interfaces (strict semantic currently only covers accessing static methods on interfaces):
Bullet 4 "If not marked BeforeFieldInit", item "c" is amended to include instance methods on interfaces, in addition to the existing value types.

**Section** "II.12 Semantics of interfaces" is extended to allow instance methods on interfaces.

**Section** "II.12.1 Implementing interfaces" is extended to say all virtual instance methods defined on an interface must be abstract, be marked with newslot and not have an associated MethodImpl which uses the method as its Impl, or final without newslot and with a MethodImpl that uses the method as its Impl entry.

**Section** "II.12.2 Implementing virtual methods on interfaces" is extended by an additional mechanism to provide interface method implementation - through inheritance of an existing implementation from an implemented interface.

[The general gist of the implementation is that default interface methods (either the slot defining method, or a MethodImpl for the interface method on another interface type) is always used as a fallback - only if the "old rules" didn't find an implementation, we apply the new rules and try to find an implementation on one of the interfaces.]
The algorithm is amended as follows:
* The existing algorithm to build interface table on the open type is left intact up to the last step "If the current class is not abstract and there are any interface methods that still have empty slots (i.e. slots with empty lists) for this class and all classes in its inheritance chain, then the program is invalid.". This is amended to become "If the current class is not abstract and there are any interface methods that still have empty slots (i.e. slots with empty lists) and the slot defining method is not abstract and there is no MethodImpl for the slot within the interfaces of the type's implicit or explicit interfaces, then the program is invalid." [Note: the default interface method resolution is disconnected from the interface table on the open type, as defined by the spec. The purpose of this change is not to fail loading at this stage.]
* The runtime resolution algorithm "When an interface method is invoked" is amended:
  * The original step 4 is moved after the following steps:
    * Create an empty list of candidate implementations of the interface method.
    * If the interface method itself is not abstract, add it to the list.
    * Apply all MethodImpls specified in the list of interfaces implicitly implemented by the runtime class of the instance through which the interface method is invoked and add the methods to the list.
    * Go over the owning types of each of the candidate methods in the list. If the owning type is less concrete than some other type in the list (there is another method in the list whose owning type requires the less concrete type), remove it from the list.
    * If there's more than one method in the list, throw AmbiguousImplementationException
    * If there's exactly one method in the list and the method is not abstract, call that method
    * If there's exactly one method in the list but the method is abstract, throw `EntryPointNotFoundException`.
    * If there's no method in the list and the interface is variant, repeat the above algorithm, looking for a variant match. Return the first variant match provided by a most specific interface.

**Section** "III.2.1 constrained. prefix" the paragraph starting with "This last case can only occur when method was defined on `System.Object`, `System.ValueType`, or `System.Enum`" is extended to also cover default interface method implementation. In the case the interface method implementation is provided by an interface, the implicit boxing becomes _observable_ to the program.

**Section** "III.4.2 callvirt" is extended to allow throwing `AmbiguousImplementationException` if the implementation of the interface method resolves at runtime to more than one default interface method. It's also extended to specify throwing `EntryPointNotFoundException` if the default interface implementation is abstract.

**Section** "III.4.18 ldvirtftn" is extended to allow throwing `AmbiguousImplementationException` if the implementation of the interface method resolves at runtime to more than one default interface method. It's also extended to specify throwing `EntryPointNotFoundException` if the default interface implementation is abstract.
