# Nullability annotations

C# 8 provides an opt-in feature that allows for the compiler to  track reference type nullability in order to catch potential null dereferences.  We are starting to adopt that feature in both System.Private.CoreLib in coreclr and in the libraries in corefx, working up from the bottom of the stack.  We're doing this for three primary reasons, in order of importance:

- **To annotate the .NET Core surface area with appropriate nullability annotations.**  While this could be done solely in the reference assemblies, we're doing it first in the implementation to help validate the selected annotations.
- **To help validate the nullability feature itself.**  With millions of lines of C# code, we have a very large and robust codebase with which to try out the feature and find areas in which it shines and areas in which we can improve it.
- **To find null-related bugs in .NET Core itself.** We expect to find relatively few meaningful bugs, due to how relatively well-tested the codebases are and how long they've been around.

## Breaking Change Guidance

We are striving to get annotations correct the "first time" and are doing due-diligence in an attempt to do so.  However, we acknowledge that we are likely to need to augment and change some annotations in the future:

- **Mistakes.** Given the sheer number of APIs being reviewed and annotated, we are likely to make some errors, and we'd like to be able to fix them so that long-term customers get the greatest benefit.
- **Breadth.** We are unlikely to have the time to annotate all of the enormous number of APIs in .NET for an initial release, and we'd like to be able to finish the task in subsequent updates.
- **Feedback.** We may need to revisit some "gray area" decisions as to whether a parameter or return type should be nullable or non-nullable (more details later).

Any such additions or changes to annotations can impact the warnings consuming code receives if that code has opted in to nullability analysis and warnings. Even so, for at least the foreseeable future we may still do so.  We will be very thoughtful about when and how we do.

## Annotation Guidance

Nullability annotations are considered to represent intent: they represent the nullability contract for the member.  Any deviation from that intent on the part of an implementation should be considered an implementation bug, and the compiler will help to minimize the chances of such bugs via its flow analysis and nullability warnings. At the same time, it's important to recognize that the validation performed by the compiler isn't perfect; it can have both false positive warnings (suggesting that something may be null even when it isn't) and false negatives (not warning when something that may be null is dereferenced).  The compiler cannot guarantee that an API declared as returning a non-nullable reference never returns null, just as it can't validate that an implementation declared as accepting nulls always behaves correctly when given them.  When deciding how to annotate APIs, it's important then to consider the desired contract rather than the current implementation; in other words, prefer to first annotate the API surface area the way that's desired, and only then work to address any warnings in the codebase, rather than driving the API surface area annotations based on where those warnings lead.

- **DO** annotate all new APIs with the desired contract.
- **CONSIDER** changing that contract if overwhelming use suggests a different de facto contract. This is particularly relevant to virtual/abstract/interface methods defined in a library where all implementations may not be under your control, and derived implementations may not have adhered to the original intent.
- **DO** continue to validate all arguments as you would have prior to nullability warnings.  In particular, if you would have checked an argument for null and thrown an ArgumentNullException if it was null, continue to do so, even if the parameter is defined as non-nullable.
- **DO NOT** remove existing argument validation when annotating existing APIS.
- **AVOID** making any changes while annotating that impact the generated IL for an implementation (e.g. `some.Method()` to `some?.Method()`).  Any such changes should be thoroughly analyzed and reviewed as a bug fix.

The majority of reference type usage in our APIs is fairly clear as to whether it should be nullable or not. For parameters, these general guidelines cover the majority of cases:

- **DO** define a parameter as non-nullable if the method checks for null and throws an `Argument{Null}Exception` if `null` is passed in for that parameter (whether explicitly in that same method or implicitly as part of some method it calls), such that there's no way null could be passed in and have the method return successfully.
- **DO** define a parameter as non-nullable if the method fails to check for `null` but instead will always end up dereferencing the `null` and throwing a `NullReferenceException`.
- **DO** define a parameter as nullable if the parameter is explicitly documented to accept `null`.
- **DO** define a parameter as nullable if method checks the parameter for `null` and does something other than throw.  This may include normalizing the input, e.g. treating `null` as `string.Empty`.
- **DO** define a parameter as nullable if the parameter is optional and has a default value of `null`.
- **DO** prefer nullable over non-nullable if there's any disagreement between the previous guidelines.  For example, if a method has documentation that suggests `null` isn't accepted but the implementation explicitly checks for, normalizes, and accepts a `null` input, the parameter should be defined nullable.

However, there are some gray areas that require case-by-case analysis to determine intent. In particular, if a parameter isn't validated nor sanitized nor documented regarding null, but in some cases simply ignored such that a null doesn't currently cause any problems, several factors should be considered when determining whether to annotate it as null.
- Is null ever passed in our own code bases?  If yes, it likely should be nullable.
- Is null ever passed in prominent 3rd-party code bases?  If yes, it likely should be nullable.
- Is null likely to be interpreted as a default / nop placeholder by callers?  If yes, it likely should be nullable.
- Is null accepted by other methods in a similar area or that have a similar purposes in the same code base?  If yes, it likely should be nullable.
- If the method is largely oblivious to null and just happens to still work if null is passed, and if the API's purpose wouldn't make sense if null were used, the parameter likely should be non-nullable.

Things are generally easier when looking at return values (and out parameters), as those can largely be driven by what the API's implementation is capable of:

- **DO** define a return value or out parameter as nullable if it may be assigned `null` under any circumstance.
- **DO** define all other return values or out parameters as non-nullable.

Annotating one of our return types as non-nullable is equivalent to documenting a guarantee that it will never return null.  Violations of that guarantee are bugs to be fixed in the implementation. However, there is a huge gap here, in the form of overridable members…

### Virtual/Abstract Methods and Interfaces

For virtual members, annotating a return type as non-nullable places a requirement on all overrides to meet those same guarantees, just as any other documented behaviors of a virtuals apply to all overrides, whether those stated guarantees can be enforced by the compiler or not.  An override that doesn't abide by these guarantees has a bug.  For existing virtual APIs that have already documented a guarantee about a non-nullable return, it's expected that the return type will be annotated as non-nullable, and derived types must continue to respect that guarantee, albeit now with the compiler's assistance.

However, for existing virtual APIs that do not have any such strong guarantee documented but where the intent was for the return value to be non-null, it is a grayer area.  The most accurate return type would be T?, whereas the intent-based return type would be T.  For T?, the pros are that it accurately reflects that nulls may emerge, but at the expense of consumers that know a null will never emerge having to use `!` or some other suppression when dereferencing. For T, the pros are that it accurately conveys the intent to overriders and allows consumers to avoid needing any form of suppression, but ironically at the expense of potential increases in occurrences of NullReferenceExceptions due to consumers then not validating the return type to be non-null and not being able to trust in the meaning of a method returning non-nullable. As such, there are several factors to then consider when deciding which return type to use for an existing virtual/abstract/interface method:
1. How common is it that an existing override written before the guarantee was put in effect would return null?
2. How widespread are overrides of the method in question?  This contributes to (1).
3. How common is it to invoke the method via the base vs via a derived type that may narrow the return type to `T` from `T?`?
4. How common is it in the case of (3) for such invocations to then dereference the result rather than passing it off to something else that accepts a `T?`?

Object.ToString is arguably the most extreme case.  Answering the above questions:
1. It is fairly easy in any reasonably-sized code base to find cases, intentional or otherwise, where ToString returns null in some cases (we've found examples in corefx, Roslyn, NuGet, ASP.NET, and so on).  One of the most prevalent conditions for this are types that just return the value in a string field which may contain its default value of null, and in particular for structs where a ctor may not have even had a chance to run and validate an input.  Guidance in the docs suggests that ToString shouldn't return null or string.Empty, but even the docs don't follow its own guidance.
2. Thousands upon thousands of types we don't control override this method today.
3. It's common for helper routines to invoke via the base object.ToString, but many ToString uses are actually on derived types.  This is particularly true when working in a code base that both defines a type and consumes its ToString.
4. Based on examination of several large code bases, we believe it to be relatively rare that the result of an Object.ToString call (made on the base) to be directly dereferenced.  It's much more common to pass it to another method that accepts `string?`, such as `String.Concat`, `String.Format`, `Console.WriteLine`, logging utilities, and so on.  And while we advocate that ToString results shouldn't be assumed to be in a particular machine-readable format and parsed, it's certainly the case that code bases do, such as using `Substring` on the result, but in such cases, the caller needs to understand the format of what's being rendered, which generally means they're working with a derived type rather than calling through the base Object.ToString.

As such, for now, we will start with `Object.ToString` returning `string?`.  We can re-evaluate this decision as we get more experience with consumers of the feature.

In contrast, for `Exception.Message` which is also virtual, we plan to have it be non-nullable, even though technically a derived class could override it to return null, because doing so is so rare that we couldn't find any meaningful examples of doing so.

## Code Review Guidance

Code reviews for enabling the nullability warnings are particularly interesting in that they often differ significantly from general code reviews.  Typically, a code reviewer focuses only on the code actually being modified (e.g. the lines highlighted by the code diffing tool); however, enabling the nullability feature has a much broader impact, in that it effectively inverts the meaning of every reference type use in the codebase (or, more specifically, in the scope at which the nullability warning context was applied).  So, for example, if you turn on nullability for a whole file (`#enable nullable` at the top of the file) and then touch no other lines in the file, every method that accepts a `string` is now accepting a non-nullable `string`; whereas previously passing in `null` to that argument would be fine, now the compiler will warn about it, and to allow nulls, the argument must be changed to `string?`.  This means that enabling nullability checking requires reviewing all exposed APIs in that context, regardless of whether they were modified or not, as the contract exposed by the API may have been implicitly modified.

A code review for enabling nullability generally involves three passes:

- **Review all implementation changes made in the code.**  Except when explicitly fixing a bug (which should be rare), the annotations employed for nullability should have zero impact on the generated IL (other than potentially some added attributes in the metadata).  The most common changes are:

    - Adding `?` to reference type parameters and local symbols.  These inform the compiler that nulls are allowed.  For locals, they evaporate entirely at compile time.  For parameters, they impact the [Nullable(...)] attributes emitted into the metadata by the compiler, but have no effect on the implementation IL.

    - Adding `!` to reference type usage.  These essentially suppress the null warning, telling the compiler to treat the expression as if it's non-null.  These evaporate at compile-time.

    - Adding `Debug.Assert(reference != null);` statements.  These inform the compiler that the mentioned reference is non-null, which will cause the compiler to factor that in and have the effect of suppressing subsequent warnings on that reference (until the flow analysis suggests that could change).  As with any Debug.Assert, these evaporate at compile-time in release builds (where DEBUG isn't defined).
  
  - Most any other changes have the potential to change the IL, which should not be necessary for the feature.  In particular, it's common for `?`s on dereferences to sneak in, e.g. changing `someVar.SomeMethod()` to `someVar?.SomeMethod()`; that is a change to the IL, and should only be employed when there's an actual known bug that's important to fix, as otherwise we're incurring unnecessary cost.

  - Any `!`s added that should have been unnecessary and are required due to either a compiler issue or due to lack of expressibility about annotations should have a `// TODO-NULLABLE: http://link/to/relevant/issue` comment added on the same line.  Issues due to lack of expressability should link to https://github.com/dotnet/roslyn/issues/26761.  Issues due to lack of annotation support for never-returning methods should link to https://github.com/dotnet/csharplang/issues/538.

- **Review the API changes explicitly made.**  These are the ones that show up in the diff.  They should be reviewed to validate that they make sense from a contract perspective.  Do we expect/allow nulls everywhere a parameter reference type was augmented with `?`?  Do we potentially return nulls everywhere a return type was augmented with `?`?  Was anything else changed that could be an accidental breaking change (e.g. a value type parameter getting annotated to become a Nullable<T> instead of a T)?  Any APIs where the contract could be more constrained if more expressibility were present should have a `// TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761` comment.

- **Review all other exported APIs (e.g. public and protected on public types) for all reference types in both return and parameter positions.**  Anything that wasn't changed to be `?` is now defined as non-nullable.  For parameters, that means consuming code will now get a harsh warning if it tries to pass null, and thus these should be changed if null actually is allowed / expected.  For returns, it means the API will never return null; if it might return null in some circumstance, the API should be changed to return `?`.  This is the most time consuming and tedious part of the review.
