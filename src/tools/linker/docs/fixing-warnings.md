
# Trim warnings in .NET 6

[In .NET Core 3.1 and 5.0 we introduced
trimming](https://devblogs.microsoft.com/dotnet/app-trimming-in-net-5/) as a new preview feature
for self-contained .NET core applications. Conceptually the feature is very simple: when
publishing the application the .NET SDK analyzes the entire application and removes all unused
code. In the time trimming has been in preview, we've learned that trimming is very powerful --
it can reduce application size by half or more. However, we've also learned about the
difficulties in safely trimming applications.

The most difficult question in trimming is what is unused, or more precisely, what is used. For
most standard C# code this is trivial -- the trimmer can easily walk method calls, field and
property references, etc, and determine what code is used. Unfortunately, some features, like
reflection, present a significant problem. Consider the following code:

```C#
string s = Console.ReadLine();
Type type = Type.GetType(s);
foreach (var m in type.GetMethods())
{
    Console.WriteLine(m.Name);
}
```

In this example, `Type.GetType` dynamically requests a type with an unknown name, and then prints
the names of all of its methods. Because there's no way to know at publish time what type name is
going to be used, there's no way for the trimmer to know which type to preserve in the output.
It's very likely that this code could have worked before trimming (as long as the input is
something known to exist in the target framework), but would probably produce a null reference
exception after trimming (due to `Type.GetType` returning null).

This is a frustrating situation. Trimming often works just fine but occasionally it can produce
breaking behavior, sometimes in rare code paths, and it can be very hard to trace down the cause.

For .NET 6 we want to bring a new feature to trimming: trim warnings. Trim warnings happen
because the trimmer sees a call which may access other code in the app but the trimmer can't
determine which code. This could mean that the trimmer would trim away code which is used at
runtime.

## Reacting to trim warnings

Trim warnings are meant to bring predictability to trimming. The problem with trimming is that some
code patterns can depend on code in a way that isn't understandable by the trimmer. Whenever the
trimmer encounters code like that, that's when you should expect a trim warning.

There are two big categories of warnings which you will likely see:

 1. `RequiresUnreferencedCode`
 2. `DynamicallyAccessedMembers`

### RequiresUnreferencedCode

[RequiresUnreferencedCode](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.requiresunreferencedcodeattribute?view=net-5.0) is simple: it's an attribute that can be placed on methods to indicate
that the method is not trim-compatible, meaning that it might use reflection or some other
mechanism to access code that may be trimmed away. This attribute is used when it's not possible
for the trimmer to understand what's necessary, and a blanket warning is needed. This would often
be true for methods which use the C# `dynamic` keyword, `Assembly.LoadFrom`, or other runtime
code generation technologies.
An example would be:

```C#
[RequiresUnreferencedCode("Use 'MethodFriendlyToTrimming' instead")]
void MethodWithAssemblyLoad() { ... }

void TestMethod()
{
    // IL2026: Using method 'MethodWithAssemblyLoad' which has 'RequiresUnreferencedCodeAttribute'
    // can break functionality when trimming application code. Use 'MethodFriendlyToTrimming' instead.
    MethodWithAssemblyLoad();
}
```

There aren't many workarounds for `RequiresUnreferencedCode`. The best way is to avoid calling
the method at all when trimming and use something else which is trim-compatible. If you're
writing a library and it's not in your control whether or not to call the method and you just
want to communicate to *your* caller, you can also add `RequiresUnreferencedCode` to your own
method. This silences all trimming warnings in your code, but will produce a warning whenever
someone calls your method.

If you can somehow determine that the call is safe, and all the code that's needed won't be
trimmed away, you can also suppress the warning using
[UnconditionalSuppressMessageAttribute](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.unconditionalsuppressmessageattribute?view=net-5.0).
For example:

```C#
[RequiresUnreferencedCode("Use 'MethodFriendlyToTrimming' instead")]
void MethodWithAssemblyLoad() { ... }

[UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode",
    Justification = "Everything referenced in the loaded assembly is manually preserved, so it's safe")]
void TestMethod()
{
    MethodWithAssemblyLoad(); // Warning suppressed
}
```

`UnconditionalSuppressMessage` is like `SuppressMessage` but it is preserved into IL, so the
trimmer can see the suppression even after build and publish. `SuppressMessage` and `#pragma`
directives are only present in source so they can't be used to silence warnings from the
trimmer. Be very careful when suppressing trim warnings: it's possible that the call may be
trim-compatible now, but as you change your code that may change and you may forget to review all
the suppressions.

### DynamicallyAccessedMembers

[DynamicallyAccessedMembers](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.dynamicallyaccessedmembersattribute?view=net-5.0) is usually about reflection. Unlike `RequiresUnreferencedCode`,
reflection can sometimes be understood by the trimmer as long as it's annotated correctly.
Let's take another look at the original example:

```C#
string s = Console.ReadLine();
Type type = Type.GetType(s);
foreach (var m in type.GetMethods())
{
    Console.WriteLine(m.Name);
}
```

In the example above, the real problem is `Console.ReadLine()`. Because *any* type could
be read the trimmer has no way to know if you need methods on `System.Tuple` or `System.Guid`
or any other type. On the other hand, if your code looked like,

```C#
Type type = typeof(System.Tuple);
foreach (var m in type.GetMethods())
{
    Console.WriteLine(m.Name);
}
```

This would be fine. Here the trimmer can see the exact type being referenced: `System.Tuple`. Now
it can use flow analysis to determine that it needs to keep all public methods. So where does
`DynamicallyAccessMembers` come in? What happens if the reflection is split across methods?

```C#
void Method1()
{
    Method2(typeof(System.Tuple));
}
void Method2(Type type)
{
    var methods = type.GetMethods();
    ...
}
```

If you compile the above, now you see a warning:

```
Trim analysis warning IL2070: net6.Program.Method2(Type): 'this' argument does not satisfy
'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethods()'. The
parameter 'type' of method 'net6.Program.Method2(Type)' does not have matching annotations. The
source value must declare at least the same requirements as those declared on the target
location it is assigned to.
```

For performance and stability flow analysis isn't performed between
methods, so annotation is needed to pass information upward, from the reflection call
(`GetMethods`) to the source of the `Type` (`typeof`). In the above example, the trimmer warning
is saying that `GetMethods` requires the `PublicMethods` annotation on types, but the `type`
variable doesn't have the same requirement. In other words, we need to pass the requirements from
`GetMethods` up to the caller:

```C#
void Method1()
{
    Method2(typeof(System.Tuple));
}
void Method2(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
{
    var methods = type.GetMethods();
    ...
}
```

Now the warning disappears, because the trimmer knows exactly which members to preserve, and
which type(s) to preserve them on. In general, this is the best way to deal with
`DynamicallyAccessedMembers` warnings: add annotations so the trimmer knows what to preserve.

As with `RequiresUnreferencedCode` warnings, adding `RequiresUnreferencedCode` or
`UnconditionalSuppressMessage` attributes also works, but none of these options make the code
compatible with trimming, while adding `DynamicallyAccessedMembers` does.

## Conclusion

This description should cover the most common situations you end up in while trimming your
application. Over time we'll continue to improve the diagnostic experience and tooling.

As we continue developing trimming we hope to see more code that's fully annotated, so users can
trim with confidence. Because trimming involves the whole application, trimming is as much a
feature of the ecosystem as it is of the product and we're depending on all developers to help
improve the ecosystem.
