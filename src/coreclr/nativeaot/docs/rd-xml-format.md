Rd.xml File Format
==================

The NativeAOT compiler discovers methods to compile and types to generate by compiling the application entry point and its transitive dependencies. The compiler may miss types if an application uses reflection. For more information about the problem see [Reflection in AOT mode](reflection-in-aot-mode.md).
An rd.xml file can be supplemented to help the compiler find types that should be analyzed. This file is similar but more limited than the rd.xml file used by .NET Native.

Minimal Rd.xml configuration

```xml
<Directives xmlns="http://schemas.microsoft.com/netfx/2013/01/metadata">
  <Application>
    <Assembly Name="mscorlib" />
  </Application>
</Directives>
```

The compiler supports 2 top level directives `Application` or `Library`. Right now both of them can be used interchangeably and just define area where actual assembly configuration happens.
You can put multiple `<Assembly>` tags inside the `<Application>` directive to configure each assembly individually.

## Assembly directive

There are 3 forms how assembly can be configured
- Module metadata only;
- All types;
- Module metadata and selected types.

Module metadata only just need simple `<Assembly>` tag with short name of the assembly.
```xml
<Directives xmlns="http://schemas.microsoft.com/netfx/2013/01/metadata">
  <Application>
    <Assembly Name="mscorlib" />
  </Application>
</Directives>
```

All types in the assembly require adding `Dynamic` attribute with value `Required All`. *NOTE*: This is the only available value for this attribute.
```xml
<Directives xmlns="http://schemas.microsoft.com/netfx/2013/01/metadata">
  <Application>
    <Assembly Name="mscorlib" Dynamic="Required All" />
  </Application>
</Directives>
```
Note that if you have generic types in the assembly, then specific instantiation would not be present in generated code, and if you need one to be included,
then you should include these instantiation using nested `<Type>` tag.

Module metadata and selected types option based on module metadata only mode with added `<Type>` tags inside `<Assembly>`.
```xml
<Directives xmlns="http://schemas.microsoft.com/netfx/2013/01/metadata">
  <Application>
    <Assembly Name="MonoGame.Framework">
      <Type Name="Microsoft.Xna.Framework.Content.ListReader`1[[System.Char,mscorlib]]" Dynamic="Required All" />
    </Assembly>
  </Application>
</Directives>
```

## Types directives.
Type directive provides a way to specify what types are needed. Developer has two options here:
- Take all type methods;
- Select which methods should be rooted.

Take all type methods:
```xml
<Type Name="Microsoft.Xna.Framework.Content.ListReader`1[[System.Char,mscorlib]]" Dynamic="Required All" />
```

Example how specify typenames
```c#
// just int
System.Int32
// string[]
System.String[]
// string[][]
System.String[][]
// string[,]
System.String[,]
// List<int>
System.Collections.Generic.List`1[[System.Int32,System.Private.CoreLib]]
// Dictionary<int, string>.KeyCollection - notice all the generic arguments go to the nested type
System.Collections.Generic.Dictionary`2+KeyCollection[[System.Int32,System.Private.CoreLib],[System.String,System.Private.CoreLib]]
```

Note that it likely does not make sense to have generic type to be placed here, since code generated over specific instantiation of the generic type.
Example of invalid scenario:
```c#
// List<T>
System.Collections.Generic.List`1
```

To select which methods should be rooted add nested `<Method>` tags.
```xml
<Directives xmlns="http://schemas.microsoft.com/netfx/2013/01/metadata">
  <Application>
    <Assembly Name="System.Private.CoreLib">
      <Type Name="System.Collections.Generic.Dictionary`2[[System.Int32,System.Private.CoreLib],[System.String,System.Private.CoreLib]]">
        <Method Name="EnsureCapacity" />
      </Type>
    </Assembly>
  </Application>
</Directives>
```

Alternatively you can specify optional `<Parameter>` tag, if you want only specific overload. For example:
```xml
<Directives xmlns="http://schemas.microsoft.com/netfx/2013/01/metadata">
  <Application>
    <Assembly Name="System.Private.CoreLib">
      <Type Name="System.Collections.Generic.Dictionary`2[[System.Int32,System.Private.CoreLib],[System.String,System.Private.CoreLib]]">
        <Method Name="EnsureCapacity">
          <Parameter Name="System.Int32, System.Private.CoreLib" />
        </Method>
      </Type>
    </Assembly>
  </Application>
</Directives>
```

or if you want instantiate generic method you can pass `<GenericArgument>`.
```xml
<Directives xmlns="http://schemas.microsoft.com/netfx/2013/01/metadata">
  <Application>
    <Assembly Name="System.Private.CoreLib">
      <Type Name="System.Array">
        <Method Name="Empty">
          <GenericArgument Name="System.Int32, System.Private.CoreLib" />
        </Method>
      </Type>
    </Assembly>
  </Application>
</Directives>
```

Take note that methods are distinguished by their method name and parameters. The return value's type is not used in the method signature.

## Rooting structure marshalling data

The `Type` directive additionally support a `MarshalStructure` attribute. The only supported value for `MarshalStructure` is `Required All`. Specifying `MarshalStructure="Required All"` will ensure struct marshalling data structures get pregenerated.

This can fix code like:

```csharp
using System;
using System.Runtime.InteropServices;

Console.WriteLine(GetTypeSize(typeof(MyClass)));

// This is in a separate method since the compiler would be able to analyze `Marshal.SizeOf(typeof(MyClass))`,
// but since a method call is involved, the compiler will lose track of the specific type.
static int GetTypeSize(Type t) => Marshal.SizeOf(t);

[StructLayout(LayoutKind.Sequential)]
public class MyClass
{
    public string Field;
}
```

The above code would throw a "System.Runtime.InteropServices.MissingInteropDataException: MyClass is missing structure marshalling data. To enable structure marshalling data, add a MarshalStructure directive to the application rd.xml file.". To fix this exception, one would use following RD.XML:

```xml
<Directives>
  <Application>
    <Assembly Name="repro">
      <Type Name="MyClass" MarshalStructure="Required All" />
    </Assembly>
  </Application>
</Directives>
```

## Rooting delegate marshalling data

Similarly to the structure marshalling data above, the `Type` directive also supports a `MarshalDelegate` attribute. The only supported value for `MarshalDelegate` is `Required All`. Specifying `MarshalDelegate="Required All"` will ensure delegate marshalling data structures get pregenerated.

```xml
<Directives>
  <Application>
    <Assembly Name="repro">
      <Type Name="MyDelegate" MarshalDelegate="Required All" />
    </Assembly>
  </Application>
</Directives>
```

The above XML will force generation of delegate marshalling data for delegate type `MyDelegate` in assembly `repro`.
