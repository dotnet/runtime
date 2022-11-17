# Authoring Custom IL trimmer Steps

The IL trimmer behaviour can be altered not only using existing options but also by
adding custom steps to the processing pipeline. This advanced technique is not necessary
for most scenarious but can be used by additional framework or SDKs which need very
custom behaviour or have other special needs for the processing.

## Building

Custom steps can be registered to the trimmer as ordinary .NET dlls which implement the
required `Mono.Linker.Steps.IStep` interface. There is also more convenient base class
called `Mono.Linker.Steps.BaseStep` which implements the interface and exposes more
functionality.

To create such project you create a library project and add package reference to
`Microsoft.NET.ILLink` package.

Such library then need to be registered with the trimmer using `--custom-step` option or
using `_TrimmerCustomSteps` msbuild `ItemGroup`.

## Custom Step Example

A simple example below demonstrates how to create a trivial custom step which only reads
 custom data and outputs them to the log.

```csharp
using System;

using Mono.Linker;
using Mono.Linker.Steps;

namespace MyLinkerExtension
{
	public class FooStep : IStep
	{
		public void Process (LinkContext context)
		{
			if (context.TryGetCustomData ("TestKey", out var value))
				context.LogMessage ($"Custom step with custom data of {value}");
		}
	}
}
```

### Consuming External Data

When building a custom step which needs interaction with external values (for example for the custom step
configuration), there is an option `--custom-data` which allows passing the data to the trimmer. The data are
stored inside a trimmer context and can be obtained in the custom step using `context.TryGetCustomData` method.

### Reporting Custom Errors and Warnings

The common operation in the custom steps is to run additional checks on the IL to verify it meets
required constraints. The outcome of such operation is then usually some indication to the end user about
the incompatible code. This can be done by reporting the error or warning message using `context.LogMessage`
method. It's good practice to use error or warning code which does not conflict with existing codes as
listed [here](/docs/error-codes.md).
