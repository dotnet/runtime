# System.IO
This assembly no longer contains any code. It is provided only to permit type unification for libraries built against previous versions of .NET.

## Source
* All types previously part of this assembly (`Stream`, `BinaryReader`, `MemoryStream`, `StreamReader`, `TextWriter`, etc.) are now part of [System.Private.CoreLib](../System.Private.CoreLib/), exposed via [System.Runtime](../System.Runtime/).

* Most of the tests for types previously in this library are still in the [tests](tests/) subdirectory.

## Contribution Bar
- [x] We consider changes that add value to the tests.

## Deployment
The System.IO assembly is part of the shared framework, and ships with every new release of .NET.
