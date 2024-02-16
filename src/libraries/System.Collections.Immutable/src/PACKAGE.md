## About

<!-- A description of the package and where one can find more documentation -->

This package provides collections that are thread safe and guaranteed to never change their contents, also known as immutable collections. Like strings, any methods that perform modifications will not change the existing instance but instead return a new instance. For efficiency reasons, the implementation uses a sharing mechanism to ensure that newly created instances share as much data as possible with the previous instance while ensuring that operations have a predictable time complexity.

The `System.Collections.Immutable` library is built-in as part of the shared framework in .NET Runtime. The package can be installed when you need to use it in other target frameworks.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

```C#
using System.Collections.Immutable;

// Create immutable set of strings
ImmutableHashSet<string> colors = ImmutableHashSet.Create("Red", "Green", "Blue");

// Create a new set by adding and removing items from the original set
ImmutableHashSet<string> colorsModified = colors.Remove("Red").Add("Orange");

foreach (string s in colorsModified)
{
    Console.WriteLine(s);
}

/* Example output:
 Blue
 Green
 Orange
 */
 ```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.Collections.Immutable.ImmutableArray`
* `System.Collections.Immutable.ImmutableArray<T>`
* `System.Collections.Immutable.ImmutableDictionary`
* `System.Collections.Immutable.ImmutableDictionary<TKey,TValue>`
* `System.Collections.Immutable.ImmutableHashSet`
* `System.Collections.Immutable.ImmutableHashSet<T>`
* `System.Collections.Immutable.ImmutableList`
* `System.Collections.Immutable.ImmutableList<T>`
* `System.Collections.Immutable.ImmutableQueue`
* `System.Collections.Immutable.ImmutableQueue<T>`
* `System.Collections.Immutable.ImmutableSortedDictionary`
* `System.Collections.Immutable.ImmutableSortedDictionary<TKey,TValue>`
* `System.Collections.Immutable.ImmutableSortedSet`
* `System.Collections.Immutable.ImmutableSortedSet<T>`
* `System.Collections.Immutable.ImmutableStack`
* `System.Collections.Immutable.ImmutableStack<T>`
* `System.Collections.Frozen.FrozenDictionary`
* `System.Collections.Frozen.FrozenDictionary<TKey, TValue>`
* `System.Collections.Frozen.FrozenSet`
* `System.Collections.Frozen.FrozenSet<T>`

## Additional Documentation

<!-- Links to further documentation -->

- [Collections and Data Structures](https://docs.microsoft.com/dotnet/standard/collections/)
- [API documentation](https://docs.microsoft.com/dotnet/api/system.collections.immutable)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Collections.Immutable is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
