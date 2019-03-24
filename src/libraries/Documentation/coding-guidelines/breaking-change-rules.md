Breaking Change Rules
=====================

* [Behavioral Changes](#behavioral-changes)
   * [Property, Field, Parameter and Return Values](#property-field-parameter-and-return-values)
   * [Exceptions](#exceptions)
   * [Platform Support](#platform-support)
   * [Code](#code)

* [Source and Binary Compatibility Changes](#source-and-binary-compatibility-changes)
   * [Assemblies](#assemblies)
   * [Types](#types)
   * [Members](#members)
   * [Signatures](#signatures)
   * [Attributes](#attributes)

## Behavioral Changes

### Property, Field, Parameter and Return Values
&#10003; **Allowed**
* Increasing the range of accepted values for a property or parameter if the member _is not_ `virtual`
 
    Note that the range can only increase to the extent that it does not impact the static type. e.g. it is OK to remove `if (x > 10) throw new ArgumentOutOfRangeException("x")`, but it is not OK to change the type of `x` from `int` to `long`.

* Returning a value of a more derived type for a property, field, return or `out` value

    Note, again, that the static type cannot change. e.g. it is OK to return a `string` instance where an `object` was returned previously, but it is not OK to change the return type from `object` to `string`.

&#10007; **Disallowed**  
* Increasing the range of accepted values for a property or parameter if the member _is_ `virtual`

    This is breaking because any existing overridden members will now not function correctly for the extended range of values.

* Decreasing the range of accepted values for a property or parameter, such as a change in parsing of input and throwing new errors (even if parsing behavior is not specified in the docs)

* Increasing the range of returned values for a property, field, return or `out` value

* Changing the returned values for a property, field, return or 'out' value, such as the value returned from `ToString`

    If you had an API which returned a value from 0-10, but actually intended to divide the value by two and forgot (return only 0-5) then changing the return to now give the correct value is a breaking.

* Changing the default value for a property, field or parameter (either via an overload or default value)

* Changing the value of an enum member

* Changing the precision of a numerical return value

### Exceptions
&#10003; **Allowed**
* Throwing a more derived exception than an existing exception

    For example, `CultureInfo.GetCultureInfo(String)` used to throw `ArgumentException` in .NET Framework 3.5. In .NET Framework 4.0, this was changed to throw `CultureNotFoundException` which derives from `ArgumentException`, and therefore is an acceptable change.

* Throwing a more specific exception than `NotSupportedException`, `NotImplementedException`, `NullReferenceException` or an exception that is considered unrecoverable

    Unrecoverable exceptions should not be getting caught and will be dealt with on a broad level by a high-level catch-all handler. Therefore, users are not expected to have code that catches these explicit exceptions. The unrecoverable exceptions are:

    * `StackOverflowException`
    * `SEHException`
    * `ExecutionEngineException`
    * `AccessViolationException`

* Throwing a new exception that only applies to a code-path which can only be observed with new parameter values, or state (that couldn't hit by existing code targeting the previous version)

* Removing an exception that was being thrown when the API allows more robust behavior or enables new scenarios

    For example, a Divide method which only worked on positive values, but threw an exception otherwise, can be changed to support all values and the exception is no longer thrown.

&#10007; **Disallowed**
* Throwing a new exception in any other case not listed above

* Removing an exception in any other case not listed above

### Platform Support

&#10003; **Allowed**
* An operation previously not supported on a specific platform, is now supported

&#10007; **Disallowed**
* An operation previously supported on a specific platform is no longer supported, or now requires a specific service-pack

### Code
&#10003; **Allowed**
* A change which is directly intended to increase performance of an operation

    The ability to modify the performance of an operation is essential in order to ensure we stay competitive, and we continue to give users operational benefits. This can break anything which relies upon the current speed of an operation, sometimes visible in badly built code relying upon asynchronous operations. Note that the performance change should have no affect on other behavior of the API in question, otherwise the change will be breaking.

* A change which indirectly, and often adversely, affects performance

    Assuming the change in question is not categorized as breaking for some other reason, this is acceptable. Often, actions need to be taken which may include extra operation calls, or new functionality. This will almost always affect performance, but may be essential to make the API in question function as expected.

* Changing the text of an error message

    Not only should users not rely on these text messages, but they change anyways based on culture

* Calling a brand new event that wasn't previously defined.

&#10007; **Disallowed**

* Adding the `checked` keyword to a code-block

    This may cause code in a block to begin to throwing exceptions, an unacceptable change.

* Changing the order in which events are fired

    Developers can reasonably expect events to fire in the same order.

* Removing the raising of an event on a given action

* Changing a synchronous API to asynchronous (and vice versa)

* Firing an existing event when it was never fired before

* Changing the number of times given events are called

## Source and Binary Compatibility Changes

### Assemblies
&#10003; **Allowed**
* Making an assembly portable when the same platforms are still supported

&#10007; **Disallowed**
* Changing the name of an assembly

* Changing the public key of an assembly

### Types
&#10003; **Allowed**
* Adding the `sealed` or `abstract` keyword to a type when there are _no accessible_ (public or protected) constructors

* Increasing the visibility of a type

* Introducing a new base class

    So long as it does not introduce any new abstract members or change the semantics or behavior of existing members, a type can be introduced into a hierarchy between two existing types. For example, between .NET Framework 1.1 and .NET Framework 2.0, we introduced `DbConnection` as a new base class for `SqlConnection` which previously derived from `Component`.

* Adding an interface implementation to a type
    This is acceptable because it will not adversely affect existing clients. Any changes which could be made to the type being changed in this situation, will have to work within the boundaries of acceptable changes defined here, in order for the new implementation to remain acceptable.
    Extreme caution is urged when adding interfaces that directly affect the ability of the designer or serializer to generate code or data, that cannot be consumed down-level. An example is the `ISerializable` interface.

* Removing an interface implementation from a type when the interface is already implemented lower in the hierarchy

* Moving a type from one assembly into another assembly

    The old assembly must be marked with `TypeForwardedToAttribute` pointing to the new location

* Changing a `struct` type to a `readonly struct` type

&#10007; **Disallowed**
* Adding the `sealed` or `abstract` keyword to a type when there _are accessible_ (public or protected) constructors

* Decreasing the visibility of a type

* Removing the implementation of an interface on a type

    It is not breaking when you added the implementation of an interface which derives from the removed interface. For example, you removed `IDisposable`, but implemented `IComponent`, which derives from `IDisposable`.

* Removing one or more base classes for a type, including changing `struct` to `class` and vice versa

* Changing the namespace or name of a type

* Changing a `readonly struct` type to a `struct` type

* Changing a `struct` type to a `ref struct` type and vice versa

* Changing the underlying type of an enum

    This is a compile-time and behavioral breaking change as well as a binary breaking change which can make attribute arguments unparsable.

### Members
&#10003; **Allowed**
* Adding an abstract member to a public type when there are _no accessible_ (`public` or `protected`) constructors, or the type is `sealed`

* Moving a member onto a class higher in the hierarchy tree of the type from which it was removed

* Increasing the visibility of a member that is not `virtual`

* Decreasing the visibility of a `protected` member when there are _no accessible_ (`public` or `protected`) constructors or the type is `sealed`

* Changing a member from `abstract` to `virtual`

* Introducing or removing an override

    Make note, that introducing an override might cause previous consumers to skip over the override when calling `base`.

* Change from `ref readonly` return to `ref` return (except for virtual methods or interfaces)

&#10007; **Disallowed**
* Adding an member to an interface

* Adding an abstract member to a type when there _are accessible_ (`public` or `protected`) constructors and the type is not `sealed`

* Adding a constructor to a class which previously had no constructor, without also adding the default constructor

* Adding an overload that precludes an existing overload, and defines different behavior

    This will break existing clients that were bound to the previous overload. For example, if you have a class that has a single version of a method that accepts a `uint`, an existing consumer will 
successfully bind to that overload, if simply passing an `int` value. However, if you add an overload that accepts an `int`, recompiling or via late-binding the application will now bind to the new overload. If different behavior results, then this is a breaking change.

* Removing or renaming a member, including a getter or setter from a property or enum members

* Decreasing the visibility of a `protected` member when there _are accessible_ (`public` or `protected`) constructors and the type is not `sealed`

* Adding or removing `abstract` from a member

* Removing the `virtual` keyword from a member

* Adding `virtual` to a member

    While this change would often work without breaking too many scenarios because C# compiler tends to emit `callvirt` IL instructions to call non-virtual methods (`callvirt` performs a null check, while a normal `call` won't), we can't rely on it. C# is not the only language we target and the C# compiler increasingly tries to optimize `callvirt` to a normal `call` whenever the target method is non-virtual and the `this` is provably not null (such as a method accessed through the `?.` null propagation operator). Making a method virtual would mean that consumer code would often end up calling it non-virtually.

* Change from `ref` return to `ref readonly` return

* Change from `ref readonly` return to `ref` return on a virtual method or interface

* Adding or removing `static` keyword from a member

* Adding a field to a struct that previously had no state

    Definite assignment rules allow use of uninitialized variables so long as the variable type is a stateless struct. If the struct is made stateful, code could now end up with uninitialized data. This is both potentially a source breaking and binary breaking change.

### Signatures
&#10003; **Allowed**
* Adding `params` to a parameter

* Removing `readonly` from a field, unless the static type of the field is a mutable value type

&#10007; **Disallowed**
* Adding `readonly` to a field

* Adding the `FlagsAttribute` to an enum

* Changing the type of a property, field, parameter or return value

* Adding, removing or changing the order of parameters

* Removing `params` from a parameter

* Adding or removing `in`, `out`, or `ref` keywords from a parameter

* Renaming a parameter (including case)

    This is considered breaking for two reasons:
    * It breaks late-bound scenarios, such as Visual Basic's late-binding feature and C#'s `dynamic`
    * It breaks source compatibility when developers use [named parameters](http://msdn.microsoft.com/en-us/library/dd264739.aspx).

* Changing a parameter modifier from `ref` to `out`, or vice versa

### Attributes
&#10003; **Allowed**
* Changing the value of an attribute that is _not observable_

&#10007; **Disallowed**

* Removing an attribute

    Although this item can be addressed on a case to case basis, removing an attribute will often be breaking. For example, `NonSerializedAttribute`

* Changing values of an attribute that _is observable_
