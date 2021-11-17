# System.Runtime.Internal

The System.Runtime.Internal library is designed for internal usage in our test tree. This library exposes some public APIs from System.Private.CoreLib for usage in our tests that are not exposed via the Microsoft.NETCore.App framework. These API are either used by the hosting layer (so they don't need to be exposed publicly) or used by some old internal tooling that we're working to remove in future versions of .NET.
