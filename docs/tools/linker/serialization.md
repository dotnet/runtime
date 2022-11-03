# Serialization

The linker cannot analyze the patterns typically used by reflection-based serializers. Such serializers should be annotated with `RequiresUnreferencedCodeAttribute`, and using them in a trimmed app will likely not work (or will work unpredictably). The linker will produce static analysis [warnings](https://docs.microsoft.com/dotnet/core/deploying/trimming-options#analysis-warnings) for these patterns.

If possible, avoid using reflection-based serializers with trimming, and prefer solutions based on source generators where the serialized types and all required members are statically referenced.

As a last resort, the linker does have limited heuristics that can be enabled to keep _some_ of the types and members required for serialization, but this provides no correctness guarantees; apps which use reflection-based serializers are still considered "broken" as far as the static analysis can tell, and it is up to you to make sure that the app works as intended.

Serialization discovery is disabled by default, and can be enabled by passing `--enable-serialization-discovery`.

## History

The linker has historically been used for Xamarin scenarios that use reflection-based serializers like XmlSerializer, since before the introduction of the trim analysis warnings. There were limited heuristics to satisfy some simple uses of serializers. To provide backwards compatibility for such scenarios, the linker has built-in heuristics that makes some simple cases "just work", albeit in an opaque and unpredictable way.

Consider disabling this behavior if possible, but it may be necessary when using legacy serializers that don't provide source generators or a similar solution that is statically analyzable. The following is a description of the heuristics for anyone who is unfortunate enough to have to rely on this behavior.

## Heuristics

There are four parts to the heuristics:
- Activation: which conditions cause the discovered roots and their recursive types to be kept
- Root discovery: logic to discover types and members are entry points to serialization
- Type graph: recursive logic to build a set of types to consider for serialization, starting from the roots
- Preservation logic: what the linker does with the discovered types

## Activation

The heuristics will keep detected serialized types only when it sees that the app has a call to a serializer constructor:

- `DataContractSerializer` or `DataContractJsonSerializer` ctors will cause types attributed with DataContractSerializer attributes and their type graph to be preserved
- `XmlSerializer` ctor will cause types attributed with XmlSerializer attributes and their type graph to be preserved

Even if the app contains attributed types for serialization, they will not be kept unless the serializer-specific construcrtor is called. Note that the preservation logic for a given serializer will be activated for all discovered types for that serializer, even if the constructor call doesn't actually serialize those types. For example:

```csharp
new XmlSerializer (typeof (Foo));
var t = typeof (Bar);

class Foo
{
    int removedField;
}

[XmlRoot]
class Bar
{
    int keptField;
}
```

Here, the call to the `XmlSerializer` ctor activates the serialization logic, causing `Bar` to be considered a root even though it is not the type being serialized (and `Foo` will not be considered a serializer root).

## Root discovery

The heuristics will discover types and members that satisfy _all_ of the following criteria:

- The type, or the declaring type of the member, is used

  There must be a statically discoverable reference to the type. In other words, if running the linker without the serialization heuristics removes a given type, then the heuristics will not discover it or any of its members as a serialization root.

- The type or member is attributed with a serializer-specific attribute.

  See the sections below about the attributes you can use for each serializer. The attribute must be present on the root type or member, including fields/properties/methods/events, public or private, though the serializers may not define attributes that can be placed on all member kinds.

Note that passing a type directly to a serializer constructor is _not_ enough to keep it. We do not use dataflow to discover types. For example:

```csharp
new XmlSerializer (typeof (RootType)); // Will not consider RootType as one of the root types
```

This pattern will not consider the type passed into the constructor as a root type, even though it is statically analyzable in theory.

### XMLSerializer attributes

On any member supported by the attribute:
- Any attribute named `Xml*Attribute` in the `System.Xml.Serialization` namespace
  - _except_ `XmlIgnoreAttribute`

### DataContractSerializer attributes

On types:
- `System.Runtime.Serialization.DataContractAttribute`

On properties, fields, or events:
- `System.Runtime.Serialization.DataMemberAttribute`

## Type graph

The heuristics will consider the following types based on the discovered roots:

- The root type itself
- The declaring type of a root member
- The property or field type of a root property or field

Starting with these types, the heuristics will recursively discover a set of types considered for serialization:

- Base types
  - including generic argument types
- Types of public instance properties defined on the type
  - a property is considered public if it has a public getter or setter
  - including public properties of the base type
  - including generic argument types
- Types of public instance fields defined on the type
  - including public fields of the base type
  - including generic argument types

Note that the types of implemented interfaces are not necessarily discovered.

## Preservation logic

For each discovered type (including root types and the recursive type graph), if the corresponding serializer is active, the linker marks the type and the following members:

- Public instance properties
  - including public or private getters and setters for such properties
- Public instance fields
- Public parameterless instance constructors

Note that in general, private members and static members are not preserved, nor are methods or events (other than the mentioned constructor).

In addition, the linker marks:
- Any discovered root members (from the attribute-based root discovery)
  - including private members
  - including static members
  - including fields/properties/methods/events

## What doesn't work

Most features of reflection-based serializers will not work even with these heuristics. The following is an incomplete list of scenarios which will not work, unless the involved types are attributed as described above:

- Serializing/deserializing types which are not attributed and don't have attributed members
- Passing `typeof(MyType)` (directly or indirectly) into serializer constructors or methods
- "Known type" mechanisms, such as:
  - [`KnownTypeAttribute`](https://docs.microsoft.com/dotnet/api/system.runtime.serialization.knowntypeattribute?view=net-5.0)
  - [`DataContractSerializer.KnownTypes`](https://docs.microsoft.com/dotnet/api/system.runtime.serialization.datacontractserializer.knowntypes?view=net-5.0)
  - `extraTypes` argument of the [`XmlSerializer ctor`](https://docs.microsoft.com/dotnet/api/system.xml.serialization.xmlserializer.-ctor?view=net-5.0#System_Xml_Serialization_XmlSerializer__ctor_System_Type_System_Type___)
- Serializing types which implement special interfaces
  - [`ISerializable`](https://docs.microsoft.com/dotnet/api/system.runtime.serialization.iserializable?view=net-5.0)
  - [`IXmlSerializable`](https://docs.microsoft.com/dotnet/api/system.xml.serialization.ixmlserializable?view=net-5.0)
- Serializer-specific handling of collection types
  - Types which implement [`ICollection`](https://docs.microsoft.com/dotnet/standard/serialization/examples-of-xml-serialization#serializing-a-class-that-implements-the-icollection-interface)
  - Deserializing [`collection interfaces`](https://docs.microsoft.com/dotnet/framework/wcf/feature-details/collection-types-in-data-contracts#using-collection-interface-types-and-read-only-collections) into serializer-specific default types
