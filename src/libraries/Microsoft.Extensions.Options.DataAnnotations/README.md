# Microsoft.Extensions.Options

`Microsoft.Extensions.Options.DataAnnotations` provides additional DataAnnotations specific functionality related to Options..

Documentation can be found at https://learn.microsoft.com/dotnet/core/extensions/options.

## Example

As of .NET 6 Preview 2, we can validate options on application startup, to help us ensure we do not face misconfiguration issues during production in our applications. For example, for an app with settings below:

```cs
services
    .AddOptions<MyOptions>()
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

where

```cs
using System.ComponentModel.DataAnnotations;

public class MyOptions
{
    [Required(AllowEmptyStrings = false)]
    public string Name { get; set; }
}
```

upon startup we will get an error stating that the Name field is required.

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](../../libraries/README.md#primary-bar)

Although the types are mature, the code base continues to evolve for better performance.

## Deployment
[Microsoft.Extensions.Options.DataAnnotations](https://www.nuget.org/packages/Microsoft.Extensions.Options.DataAnnotations) is not included in the shared framework. The package is deployed as out-of-band (OOB) and needs to be installed into projects directly.
