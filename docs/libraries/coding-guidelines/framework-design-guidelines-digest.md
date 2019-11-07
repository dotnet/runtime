Framework Design Guidelines - Digest
====================================

This page is a distillation and a simplification of the most basic
guidelines described in detail in a book titled
[Framework Design Guidelines][FDG] by Krzysztof Cwalina and Brad Abrams.

Framework Design Guidelines were created in the early days of .NET Framework
development. They started as a small set of naming and design conventions but
have been enhanced, scrutinized, and refined to a point where they are generally
considered the canonical way to design frameworks at Microsoft. They carry the
experience and cumulative wisdom of thousands of developer hours over several
versions of the .NET Framework.

[FDG]: http://amazon.com/dp/0321545613

# General Design Principles

## Scenario Driven Design

Start the design process of your public API by defining the top scenarios for
each feature area. Write code you would like the end users to write when they
implement these scenarios using your API. Design your API based on the sample
code you wrote. For example, when designing API to measure elapsed time, you may
write the following scenario code samples:

```CSharp
// scenario #1 : measure time elapsed
Stopwatch watch = Stopwatch.StartNew();
DoSomething();
Console.WriteLine(watch.Elapsed);

// scenario #2 : reuse stopwatch
Dim watch As Stopwatch = Stopwatch.StartNew()
DoSomething();
Console.WriteLine(watch.ElapsedMilliseconds)
watch.Reset() watch.Start() DoSomething()
Console.WriteLine(watch.Elapsed)

// scenario #3: ...
```

## Usability Studies

Test usability of your API. Choose developers who are not familiar with your API
and have them implement the main scenarios. Try to identify which parts of your
API are not intuitive.

## Self Documenting API

Developers using your API should be able to implement main scenarios without
reading the documentation. Help users to discover what types they need to use in
main scenarios and what the semantics of the main methods are by choosing
intuitive names for most used types and members. Talk about naming choices
during specification reviews.

## Understand Your Customer

Realize that the majority of your customers are not like you. You should design
the API for your customer, not for developers working in your close working
group, who unlike majority of your customers are experts in the technology you
are trying to expose.

# Naming Guidelines

Casing and naming guidelines apply only to public and protected identifiers, and
privately implemented interface members. Teams are free to choose their own
guidelines for internal and private identifiers.

&#10003; **DO** use PascalCasing (capitalize the first letter of each word) for
all identifiers except parameter names. For example, use `TextColor` rather than
`Textcolor` or `Text_Color`.

&#10003; **DO** use camelCasing (capitalize first letters of each word except
for the first word) for all member parameter names. prefix descriptive type
parameter names with `T`.

```CSharp
public interface ISessionChannel<TSession>
    where TSession : ISession
{
    TSession Session { get; }
}
```

&#10003; **CONSIDER** using `T` as the type parameter name for types with one
single letter type parameter.

&#10003; **DO** use PascalCasing or camelCasing for any acronyms over two
characters long. For example, use `HtmlButton` rather than `HTMLButton`, but
`System.IO` instead of `System.Io`.

&#10007; **DO NOT** use acronyms that are not generally accepted in the field.

&#10003; **DO** use well-known acronyms only when absolutely necessary. For
example, use `UI` for User Interface and `Html` for Hyper-Text Markup Language.

&#10007; **DO NOT** use of shortenings or contractions as parts of identifier
names. For example, use `GetWindow` rather than `GetWin`.

&#10007; **DO NOT** use underscores, hyphens, or any other non-alphanumeric
characters.

&#10007; **DO NOT** use the Hungarian notation.

&#10003; **DO** name types and properties with nouns or noun phrases.

&#10003; **DO** name methods and events with verbs or verb phrases. Always give
events names that have a concept of before and after using the present particle
and simple past tense. For example, an event that is raised before a `Form`
closes should be named `Closing`. An event raised after a `Form` is closed
should be named `Closed`.

&#10007; **DO NOT** use the `Before` or `After` prefixes to indicate pre and
post events.

&#10003; **DO** use the following prefixes:
* `I` for interfaces.
* `T` for generic type parameters (except single letter parameters).

&#10003; **DO** use the following postfixes:

* `Exception` for types inheriting from `System.Exception`.
* `Collection` for types implementing `IEnumerable`.
* `Dictionary` for types implementing `IDictionary` or `IDictionary<K,V>`.
* `EventArgs` for types inheriting from `System.EventArgs`.
* `EventHandler` for types inheriting from `System.Delegate`.
* `Attribute` for types inheriting from `System.Attribute`.

&#10007; **DO NOT** use the postfixes listed above for any other types.

&#10007; **DO NOT** postfix type names with `Flags` or `Enum`.

&#10003; **DO** use plural noun phrases for flag enums (enums with values that
support bitwise operations) and singular noun phrases for non-flag enums.

&#10003; **DO** use the following template for naming namespaces:

    <Company>.<Technology>[.<Feature>].

For example, `Microsoft.Office.ClipGallery`. Operating System components should
use System namespaces instead for the <Company> namespaces.

&#10007; **DO NOT** use organizational hierarchies as the basis for namespace
hierarchies. Namespaces should correspond to scenarios regardless of what teams
contribute APIs for those scenarios.

# General Design Guidelines

&#10003; **DO** use the most derived type for return values and the least
derived type for input parameters. For example take `IEnumerable` as an input
parameter but return `Collection<string>` as the return type. Provide a clear
API entry point for every scenario. Every feature area should have preferably
one, but sometimes more, types that are the starting points for exploring given
technology. We call such types Aggregate Components. Implementation of large
majority of scenarios in given technology area should start with one of the
Aggregate Components.

&#10003; **DO** write sample code for your top scenarios. The first type used in
all these samples should be an Aggregate Component and the sample code should be
straightforward. If the code gets longer than several lines, you need to
redesign. Writing to an event log in Win32 API was around 100 lines of code.
Writing to .NET Framework EventLog takes one line of code.

&#10003; **DO** model higher level concepts (physical objects) rather than
system level tasks with Aggregate Components. For example `File`, `Directory`,
`Drive` are easier to understand than `Stream`, `Formatter`, `Comparer`.

&#10007; **DO NOT** require users of your APIs to instantiate multiple objects
in main scenarios. Simple tasks should be done with new statement.

&#10003; **DO** support so called ”Create-Set-Call” programming style in all
Aggregate Components. It should be possible to instantiate every component with
the default constructor, set one or more properties, and call simple methods or
respond to events.

```CSharp
var applicationLog = new EventLog();
applicationLog.Source = "MySource";
applicationLog.WriteEntry(exception.Message);
```

&#10007; **DO NOT** require extensive initialization before Aggregate Components
can be used. If some initialization is necessary, the exception resulting from
not having the component initialized should clearly explain what needs to be
done.

&#10003; **DO** carefully choose names for your types, methods, and parameters.
Think hard about the first name people will try typing in the code editor when
they explore the feature area. Reserve and use this name for the Aggregate
Component. A common mistake is to use the ”best” name for a base type. Run FxCop
on your libraries.

&#10003; **DO** ensure your library is CLS compliant. Apply `CLSCompliantAttribute`
to your assembly.

&#10003; **DO** prefer classes over interfaces.

&#10007; **DO NOT** seal types unless you have a strong reason to do it.

&#10007; **DO NOT** create mutable value types.

&#10007; **DO NOT** ship abstractions (interfaces or abstract classes) without
providing at least one concrete type implementing each abstraction. This helps
to validate the interface design.

&#10007; **DO NOT** ship interfaces without providing at least one API consuming
the interface (a method taking the interface as a parameter). This helps to
validate the interface design.

&#10007; **AVOID** public nested types.

&#10003; **DO** apply `FlagsAttribute` to flag enums.

&#10003; **DO** strongly prefer collections over arrays in public API.

&#10007; **DO NOT** use `ArrayList`, `List<T>`, `Hashtable`, or `Dictionary<K,V>`
in public APIs. Use `Collection<T>`, `ReadOnlyCollection<T>`,
`KeyedCollection<K,V>`, or `CollectionBase` subtypes instead. Note that the
generic collections are only supported in the Framework version 2.0 and above.

&#10007; **DO NOT** use error codes to report failures. Use Exceptions instead.

&#10007; **DO NOT** throw `Exception` or `SystemException`.

&#10007; **AVOID** catching the `Exception` base type.

&#10003; **DO** prefer throwing existing common general purpose exceptions like
`ArgumentNullException`, `ArgumentOutOfRangeException`,
`InvalidOperationException` instead of defining custom exceptions. throw the
most specific exception possible.

&#10003; **DO** ensure that exception messages are clear and actionable.

&#10003; **DO** use `EventHandler<T>` for events, instead of manually defining
event handler delegates.

&#10003; **DO** prefer event based APIs over delegate based APIs.

&#10003; **DO** prefer constructors over factory methods.

&#10007; **DO NOT** expose public fields. Use properties instead.

&#10003; **DO** prefer properties for concepts with logical backing store but
use methods in the following cases:

* The operation is a conversion (such as `Object.ToString()`)
* The operation is expensive (orders of magnitude slower than a field set would
  be)
* Obtaining a property value using the Get accessor has an observable side
  effect
* Calling the member twice in succession results in different results
* The member returns an array. Note: Members returning arrays should return
  copies of an internal master array, not a reference to the internal array.

&#10003; **DO** allow properties to be set in any order. Properties should be
stateless with respect to other properties.

&#10007; **DO NOT** make members virtual unless you have a strong reason to do
it.

&#10007; **AVOID** finalizers.

&#10003; **DO** implement `IDisposable` on all types acquiring native resources
and those that provide finalizers.

&#10003; **DO** be consistent in the ordering and naming of method parameters.
It is common to have a set of overloaded methods with an increasing number of
parameters to allow the developer to specify a desired level of information.

&#10003; **DO** make sure all the related overloads have a consistent parameter
order (same parameter shows in the same place in the signature) and naming
pattern. The only method in such a group that should be virtual is the one that
has the most parameters and only when extensibility is needed.

```CSharp
public class Foo
{
    private readonly string _defaultForA = "default value for a";
    private readonly int _defaultForB = 42;

    public void Bar()
    {
        Bar(_defaultForA, _defaultForB);
    }

    public void Bar(string a)
    {
        Bar(a, _defaultForB);
    }

    public void Bar(string a, int b)
    {
        // core implementation here
    }
}
```

&#10007; **AVOID** `out` and `ref` parameters.

# Resources

## FxCop

[FxCop](https://msdn.microsoft.com/en-us/library/bb429476.aspx) is a code analysis tool that checks managed code assemblies for
conformance to the [Framework Design Guidelines][FDG] (also see [MSDN](https://msdn.microsoft.com/en-us/library/ms229042.aspx)).

## Presentations

* [Overview of the Framework Design Guidelines](http://blogs.msdn.com/kcwalina/archive/2007/03/29/1989896.aspx)
* [TechEd 2007 Presentation about framework engineering](http://blogs.msdn.com/kcwalina/archive/2008/01/08/FrameworkEngineering.aspx)
