# Road to RTM of .NET Core

Our current focus is to get the bits in order for a high quality RTM release. This includes fixing bugs, finishing the cross-platform bring-up, as well as adding new APIs and features to create the end-to-end developer experience.

This document calls out in more detail what we're focusing for the next couple of months. Of course, it doesn't list all issues; but it highlights the big areas and gives you an idea of the theme.

## Finish porting `System.Console`

Originally, we subset `System.Console` quite aggressively. The reason being that not all app models required all APIs and many of them were non-trivial to port cross platforms.

Since then, we've learned quite a bit. Based on early adopter feedback, we've decided to extend the surface to almost full parity.

## Expose old reflection APIs on `System.Reflection.TypeInfo`

One of the key goals for .NET Core was ensuring that the framework is factored well, with sensible and sustainable dependencies. As a result we decided to remove the direct dependency from `System.Object` to `System.Reflection`, which comes from `Object.GetType()`. In order to do that, we created the new `System.Reflection.TypeInfo` class that represents the full type information, and made `System.Type` the representation for a type name. In order to reflect, code is expected to do something like:

```C#
object data = GetData();
TypeInfo typeInfo = data.GetType().GetTypeInfo();
```

Unfortunately, when we introduced `TypeInfo` we also tried to clean up the surface area of reflection, and as a result didn't expose all the APIs and concepts reflection used to have. For instance, we didn't include APIs that use `BindingFlags` and provide certain semantics, such as flatting the type hierarchy.

Based on extensive customer feedback we decided that we need to improve reflection so that porting code to .NET Core becomes easier. The current proposal is to:

1. Keep the split, i.e. don't add those APIs to `System.Type`
2. Add all the APIs that used to live on `System.Type` to `System.Reflection.TypeInfo`

While having any delta is unfortunate, we believe it is vital to be able to deliver a .NET stack that doesn't require reflection, especially for ahead-of-time (AOT) compilation scenarios, in order to reduce footprint.

Also, this delta would be quite easy to reason about. Missing a reflection API? Insert a call to `GetTypeInfo()`.

## Finish `System.Buffers`

Most performance optimizations around managed code boil down to reducing the number of allocations. This could be by writing better code that requires fewer objects, avoiding excessive boxing, or pooling objects.

Related to this is the handling of buffers, i.e. arrays. This is especially relevant in I/O heavy operations, such as web servers. Instead of creating buffers when needed, it can be beneficial to pool them. This can reduce GC pressure which improves overall throughput.

We've started [building a prototype](https://github.com/dotnet/corefx/tree/master/src/System.Buffers) that we intend to finish before RTM so that ASP.NET can take a dependency on it. 

## Finish `System.Runtime.Loader.AssemblyLoadContext`

For CoreCLR, we've added a new API that allows loading assemblies into different load contexts. This allows hosts to be able to load different versions of assemblies without having to unify them.

This API is heavily used by ASP.NET and thus needs to be finished before RTM.

## Splitting the `System.Runtime.InteropServices`

The .NET stack always had a comprehensive interop story for native code, especially on Windows. This includes support for calling C APIs via P/Invoke but also includes COM and, since Windows 8, WinRT.

In the past, we've centralized the handling of all these technologies in APIs exposed from `System.Runtime.InteropServices.dll`. However, many applications don't need all of it. This is especially true in cross-platform scenarios, because COM and WinRT interop doesn't make any sense for non-Windows applications.

We've decided to extract a smaller component that deals with P/Invoke only. This allows applications to call into C APIs without having to bring along the entire COM/WinRT interop infrastructure.
