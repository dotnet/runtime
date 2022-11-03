# Redundant Warning Suppression Detection

Dynamic reflection patterns pose a serious challenge to the linker trimming capabilities. The tool is able to infer simple reflection patterns; but there are still cases which the tool will not be able to reason about. When the linker fails to recognize a certain pattern, a warning appears informing the user that the trimming process may break the functionality of the app.

There are cases where the developer is confident about the safety of a given pattern, but the linker is unable to reason about it and still produces a warning. The developer may use warning suppression to silence the warning. An example of such pattern may be using methods from a class using reflection.
```csharp
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(NameProvider))]
    public void Test()
    {
        PrintName(typeof(NameProvider));
    }

    [UnconditionalSuppressMessage("trim", "IL2070", Justification = "DynamicDependency attribute will instruct the linker to keep the public methods on NameProvider.")]
    public void PrintName(Type type)
    {
        string name = (string)type.GetMethod("GetName")!.Invoke(null, null)!;
        Console.WriteLine(name);
    }

    private class NameProvider
    {
        public static string GetName() => "NiceName";
        public static string suffix = "no really";
    }
```

## Redundant warnings
The warning suppression could present a challenge to the software development lifecycle. Let us again consider the above example of accessing methods of a private class. We can rewrite the code in such a way, that the linker is able to reason about it. Then, the warning is no longer issued and the suppression becomes redundant. We should remove it.

```csharp
    // Now the DynamicDependencyAttribute can be removed as well as the suppression here
    [UnconditionalSuppressMessage("trim", "IL2070", Justification = "DynamicDependency attribute will instruct the linker to keep the public methods on NameProvider.")]
    public void PrintName([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
        string name = (string)type.GetMethod("GetName")!.Invoke(null, null)!;
        Console.WriteLine(name);
    }
```

If we keep the warning suppression on this trimmer-compatible code, we will end up with a potentially dangerous case. Should we later add some trimmer-incompatible code within the scope of the suppression which triggers the suppressed warning, we will not be informed about it during the trimming process. That is, the warning issued by the linker will be silenced by the suppression we left over and it will not be displayed. This may result in a scenario in which the trimming completes with no warnings, yet errors occur at runtime.

This can be illustrated with the following example. Let us extend the above code to also print the value of `suffix` field.

```csharp
    [UnconditionalSuppressMessage("trim", "IL2070", Justification = "DynamicDependency attribute will instruct the linker to keep the public methods on NameProvider.")]
    public void PrintName([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
        string name = (string)type.GetMethod("GetName")!.Invoke(null, null)!;
        string suffix = (string)type.GetField("suffix")!.GetValue(null)!; // IL2070 - only public methods are guaranteed to be kept
        Console.WriteLine(name);
        Console.WriteLine(suffix);
    }
```

Now the code is again trimmer-incompatible, the `GetField("suffix")` call will trigger a `IL2070` warning. However, as we forgot to remove the suppression silencing the `IL2070` warnings, the warning will be suppressed. We will not be informed about this trimmer-incompatible pattern during trimming.


## Detecting redundant warning suppressions

In order to avoid the above scenario, we would like to have an option to detect and report the warning suppressions which are not tied to any warnings caused by trim-incompatible patterns.

This may be achieved by extending the linker tool functionality to check which suppression do in fact suppress warnings and reporting those which do not.

Running the tool with the redundant warning suppressions detection enabled will report all of the warning suppressions which do not suppress any warnings. The way to turn it on is TBD.

***NOTE:*** We will only process suppressions produced by the linker, other suppressions will be ignored.
### Example:
Let us again consider the example of the trimmer-compatible code with a redundant warning suppression.

```csharp
    [UnconditionalSuppressMessage("trim", "IL2070", Justification = "DynamicDependency attribute will instruct the linker to keep the public methods on NameProvider.")] // This should be removed
    public void PrintName([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
        string name = (string)type.GetMethod("GetName")!.Invoke(null, null)!;
        Console.WriteLine(name);
    }
```

In order to detect the warning suppression not tied to any trimmer-incompatible pattern we should run the linker with the redundant warning suppressions detection enabled.

The warning should be reported in the output of the command.

```
Trim analysis warning IL2021: Program.PrintName(Type): Unused UnconditionalSuppressMessageAttribute suppressing the IL2070 warning found. Consider removing the unused warning suppression.
```

## Other solutions

The proposed solution operates by extending the functionality of the linker tool. On one hand, this allows for reusing the existing components leading to a simple implementation, on the other hand this may lead to potential problems. The linker sees only a part of code which is actually used, that means that the solution would not be able to identify the warning suppressions on the code which is trimmed away. Also, the linker may visit parts of the code which were not authored by the developer (e.g. used libraries) and look for the redundant suppressions there. This may cause the output warnings to be noisy and not useful to the developer. Moreover, the dependencies identified by the linker may be different depending on the environment the tool is run in. Hence, the proposed solution may report different redundant suppressions on different environments.

Alternatively, we could make the analyzer do the unused warning suppressions detection. The analyzer operates on a single assembly level but it has a full view of the processed assembly, as opposed to the linker which only sees the code which is actually used within the assembly. Making the solution part of the analyzer, would allow us then to detect the unused warning suppression on assembly level with higher precision. Also, an advantage of such solution would be a shorter feedback loop; we would learn about the redundant suppressions way before we run the publish command. The drawback of this approach is the added complexity. We would not be able to reuse the existing components. What is more, the analyzer has a different view of the code than the linker. We may not be able to identify the same set of trimmer-incompatible patterns using the analyzer as we do using the linker.
