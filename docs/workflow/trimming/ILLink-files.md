# ILLink Files

There are a few `ILLink.*.xml` files under `src` folders in `dotnet/runtime`. These files are used by the trimming tool for various reasons.

See https://github.com/mono/linker/blob/main/docs/data-formats.md for full documentation on these files.

## ILLink.Descriptors.xml

Descriptors are used to direct the trimming tool to always keep some items in the assembly, regardless of if the trimming tool can find any references to them.

We try to limit the usage of descriptor files as much as possible. Since using a descriptor means the code will always be preserved, even in the final application. Typically the main scenario they are used is when non-IL code (e.g. C/C++, JavaScript, etc.) is calling into IL code. The trimming tool isn't able to see the non-IL code, so it doesn't know which IL methods are necessary.

In some cases it is only necessary to preserve items only during `dotnet/runtime`'s build, but we don't want to unconditionally preserve them in an the final application. Examples of these cases are non-public methods only used by tests, or non-public methods that are called through Reflection by another assembly. To only preserve items during `dotnet/runtime`'s build, use a `ILLink.Descriptors.LibraryBuild.xml` file.

In almost all cases, when using a descriptors file, add a comment justifying why it is necessary.

## ILLink.Substitutions.xml

Substitutions direct the trimming tool to replace specific method's body with either a throw or return constant statements.

These files are mainly used to implement [feature switches](feature-switches.md).

They can also be used to hard-code constants depending on the platform we are building for, typically in `System.Private.CoreLib.dll` since, at this time, that is the only assembly we build specifically for each target architecture. In those cases, there are multiple `ILLink.Substitutions.{arch}.xml` files, which get included depending on the architecture. This is possible through an MSBuild Item `@(ILLinkSubstitutionsXmls)` which can conditionally get added to, and all the .xml files are combined into a final `ILLink.Substitutions.xml`, which is embedded into the assembly.

## ILLink.Suppressions.xml

When we build `dotnet/runtime`, we run the trimming tool to analyze our assemblies for code that is using patterns (like Reflection) that may be broken once the application is trimmed. When the trimming tool encounters code that isn't trim compatible, it issues a warning. Because we haven't addressed all these warnings in the code, we suppress the existing warnings in `ILLink.Suppressions.xml` files, and fail the build when an unsuppressed warning is encountered. This ensures that no new code can introduce new warnings while we are addressing the existing warnings.

If your new feature or bug fix is introducing new ILLink warnings, the warnings need to be addressed before your PR can be merged. No new suppressions should be added to an `ILLink.Suppressions.xml` file. To address the warnings, see [Linking the .NET Libraries](https://github.com/dotnet/designs/blob/main/accepted/2020/linking-libraries.md). Typically, either adding `[DynamicallyAccessedMembers]` or `[RequiresUnreferencedCode]` attributes are acceptable ways of addressing the warnings. If the warning is a false-positive (meaning it is trim compatible, but the trimming tool wasn't able to tell), it can be suppressed in code using an `[UnconditionalSuppressMessage]`.

ILLink warnings that are suppressed by the `ILLink.Suppressions.xml` file will still be emitted when the final application is published. This allows developers to see where their application might be broken when it is trimmed. Warnings that are suppressed by `[UnconditionalSuppressMessage]` attributes in `dotnet/runtime` code will never be emitted, during the `dotnet/runtime` build nor in the final application.

Sometimes it is beneficial to leave an ILLink warning as unsuppressed so the final application's developer sees the warning. An examples of this is using the [`Startup Hooks`](../../design/features/host-startup-hook.md) feature in .NET. By default this feature is disabled when trimming a .NET application. However, the application can re-enable the feature. When they do, an ILLInk warning is emitted when the application is trimmed telling them the feature may not work after trimming.

To suppress a warning only in the `dotnet/runtime` build, but keep emitting it in the final application, add the warning to a `ILLink.Suppressions.LibraryBuild.xml` file, and include a justification why this approach was taken.

## ILLink.LinkAttributes.xml

Attribute annotations direct the trimming tool to behave as if the specified item has the specified attribute.

This is mainly used to tell the trimming tool which attributes to remove from the trimmed application. This is useful because some attributes are only needed at development time. They aren't necessary at runtime. Trimming unnecessary attributes can make the application smaller.

Under the covers, the way this works is that the `ILLink.LinkAttributes.xml` tells the trimming tool to act like a `[RemoveAttributeInstances]` attribute is applied to the attribute type we want to remove. The trimming tool removes any instantiations of the attribute in all the assemblies of the application. However, if  the trimming tool encounters code that is trying to read the attribute at runtime, it doesn't trim the attribute instances. For example, if runtime code is reading the `ObsoleteAttribute`, `ObsoleteAttribute` instances won't be trimmed even if it was asked to be removed through this file.

This is also how the above `ILLink.Suppressions.xml` file works under the covers. It injects `[UnconditionalSuppressMessage]` attributes to tell the trimming tool to act as if there was a suppress attribute in code.