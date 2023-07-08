# Derived Interfaces and COM

In the ComInterfaceGenerator, we want to improve the experience when writing COM interfaces that derive from other COM interfaces. The built-in system has some quirks due to differences between how interfaces work in C# in comparison to COM interfaces in C++.

## COM Interface Inheritance in C++

In C++, developers can declare COM interfaces that derive from other COM interfaces as follows:

```cpp
struct IComInterface : public IUnknown
{
    STDMETHOD(Method)() = 0;
    STDMETHOD(Method2)() = 0;
};

struct IComInterface2 : public IComInterface
{
    STDMETHOD(Method3)() = 0;
};
```

This declaration style is regularly as a mechanism to add methods to COM objects without changing existing interfaces, which would be a breaking change. This inheritance mechanism results in the following vtable layouts:

| `IComInterface` VTable slot | Method name |
|-----------------------------|-------------|
| 0 | `IUnknown::QueryInterface` |
| 1 | `IUnknown::AddRef` |
| 2 | `IUnknown::Release` |
| 3 | `IComInterface::Method` |
| 4 | `IComInterface::Method2` |


| `IComInterface2` VTable slot | Method name |
|-----------------------------|-------------|
| 0 | `IUnknown::QueryInterface` |
| 1 | `IUnknown::AddRef` |
| 2 | `IUnknown::Release` |
| 3 | `IComInterface::Method` |
| 4 | `IComInterface::Method2` |
| 5 | `IComInterface2::Method3` |

As a result, it is very easy to call a method defined on `IComInterface` from an `IComInterface2*`. Specifically, calling a method on a base interface does not require a call to `QueryInterface` to get a pointer to the base interface. Additionally, C++ allows an implicit conversion from `IComInterface2*` to `IComInterface*`, which is well defined and allows avoiding a `QueryInterface` call again. As a result, in C or C++, you never need to call `QueryInterface` to get to the base type if you do not want to, which can allow some performance improvements.

> Note: WinRT interfaces do not follow this inheritance model. They are defined to follow the same model as the `[ComImport]`-based COM interop model in .NET.

## COM Interface Inheritance in .NET with `[ComImport]`

In .NET, C# code that looks like interface inheritance isn't actually interface inheritance. Let's look at the following code:

```csharp
interface I
{
    void Method1();
}
interface J : I
{
    void Method2();
}
```

This code does not say that "`J` implements `I`." It actually says "any type that implements `J` must also implement `I`." This difference leads to the fundamental design decision that makes interface inheritance in `[ComImport]`-based interop unergonomic. In .NET's COM interop, interfaces are always considered on their own; an interface's base interface list has no impact on any calculations to determing a vtable for a given .NET interface.

As a result, the natural equivalent of the above provided C++ COM interface example leads to a different vtable layout.

C# code:
```csharp
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IComInterface
{
    void Method();
    void Method2();
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IComInterface2 : IComInterface
{
    void Method3();
}
```

VTable layouts:

| `IComInterface` VTable slot | Method name |
|-----------------------------|-------------|
| 0 | `IUnknown::QueryInterface` |
| 1 | `IUnknown::AddRef` |
| 2 | `IUnknown::Release` |
| 3 | `IComInterface::Method` |
| 4 | `IComInterface::Method2` |


| `IComInterface2` VTable slot | Method name |
|-----------------------------|-------------|
| 0 | `IUnknown::QueryInterface` |
| 1 | `IUnknown::AddRef` |
| 2 | `IUnknown::Release` |
| 3 | `IComInterface2::Method3` |

As these vtables differ from the C++ example, this will lead to serious problems at runtime. The correct definition of these interfaces in C# for the `[ComImport]` interop system is as follows:

```csharp
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IComInterface
{
    void Method();
    void Method2();
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IComInterface2 : IComInterface
{
    new void Method();
    new void Method2();
    void Method3();
}
```

Each method from the base interface types must be redeclared, as at the metadata level, `IComInterface2` does not implement `IComInterface`, but only specifies that implementors of `IComInterface2` must also implement `IComInterface`. This design decision is an ugly wart on the built-in COM interop system, so we want to improve this in the new source-generated COM interop system.

## COM Interface Inheritance in Source-Generated COM

In the new source-generated COM model, we will enable writing COM interfaces using natural C# interface "inheritance" with some restrictions to ensure a valid model. Here's an example:

C# code:
```csharp
[GeneratedComInterface]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IComInterface
{
    void Method();
    void Method2();
}

[GeneratedComInterface]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IComInterface2 : IComInterface
{
    void Method3();
}
```

VTable slots:

| `IComInterface` VTable slot | Method name |
|-----------------------------|-------------|
| 0 | `IUnknown::QueryInterface` |
| 1 | `IUnknown::AddRef` |
| 2 | `IUnknown::Release` |
| 3 | `IComInterface::Method` |
| 4 | `IComInterface::Method2` |


| `IComInterface2` VTable slot | Method name |
|-----------------------------|-------------|
| 0 | `IUnknown::QueryInterface` |
| 1 | `IUnknown::AddRef` |
| 2 | `IUnknown::Release` |
| 3 | `IComInterface::Method` |
| 4 | `IComInterface::Method2` |
| 5 | `IComInterface2::Method3` |

As you can see, this new model will allow the "natural" pattern for authoring .NET interfaces in C# result in the "natural" vtable layout as if the type was written in C++. There is a restriction on this pattern to ensure an easy-to-construct model:

- A `[GeneratedComInterface]`-attributed type may inherit from at most one `[GeneratedComInterface]`-attribute type.

This ensures that our COM-interop .NET interfaces will always follow the model of single inheritance, which is the only direct inheritance model that COM allows.

### Designing for Performance

This new model has really nice ergonomics for authoring, but it has some performance deficits compared to the built-in model. In particular, when running the following code:

```csharp
IComInterface2 obj = /* get the COM object */;
obj.Method();
```

The `[ComImport]` code will not call `QueryInterface` for `IComInterface`, but the `[GeneratedComInterface]` model will. The `[ComImport]` pattern will not need to call `QueryInterface`, as the required shadowing method declarations means that the `.Method()` call resolves to `IComInterface2.Method`, whereas the `[GeneratedComInterface]`-based code resolves that call to `IComInterface.Method`. Since the `[GeneratedComInterface]` mechanism doesn't shadow the method, the runtime will try to cast `obj` to `IComInterface`, which will result in a `QueryInterface` call.

To reduce the number of `QueryInterface` calls, the ComInterfaceGenerator will automatically emit shadowing method declarations and corresponding method implementations for all methods from any `[GeneratedComInterface]`-attributed base type and its attributed base types recursively that are visible. This way, we can ensure that the least number of `QueryInterface` calls are required when using the `[GeneratedComInterface]`-based COM interop. Additionally, we will disallow declaring any methods with the `new` modifier on a `[GeneratedComInterface]`-attributed interface to ensure that the user does not try to shadow any base interface members.

What about when the marshallers used in a base interface method declaration are not accessible by the derived type? We can try to detect this case, but it makes it very fragile to determine which methods are shadowed and which are not. Additionally, removing a shadowing method is a binary breaking change. We have a few options:

1. Always try to emit shadowing methods with marshalling and error out (either with C# compiler errors or in the generator itself) when the marshallers are not accessible.
2. Don't emit shadowing methods when the marshallers aren't accessible.
3. Emit shadowing stubs that call the base interface method when the marshallers are not accessible.

Option 2 is very fragile and makes it really easy to accidentally cause a binary breaking change, so we should either go with Option 1 or 3. For simplicity of implementation, we will go with Option 1 until we get customer feedback that this is a serious issue, at which point we will consider switching to Option 3.
