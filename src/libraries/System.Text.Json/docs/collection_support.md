# [`JsonSerializer`](https://docs.microsoft.com/dotnet/api/system.text.json.jsonserializer?view=netcore-3.1) collection support in 3.1.x

All collections (any type that derives from [`IEnumerable`](https://docs.microsoft.com/dotnet/api/system.collections.ienumerable?view=netcore-3.1))
are supported for serialization with [`JsonSerializer`](https://docs.microsoft.com/dotnet/api/system.text.json.jsonserializer?view=netcore-3.1);
we simply call the [`.GetEnumerator()`](https://docs.microsoft.com/dotnet/api/system.collections.ienumerable.getenumerator?view=netcore-3.1)
method of the collection, and write the elements if they are supported. Deserialization is more complicated and is not supported for all collections.

This document gives a quick overview of which collections are supported.

## Built-in types

### [`System.Collections`](https://docs.microsoft.com/dotnet/api/system.collections?view=netcore-3.1)

| Type | Serialization | Deserialization |
| --- | --- | --- |
| [`ArrayList`](https://docs.microsoft.com/dotnet/api/system.collections.arraylist?view=netcore-3.1) | Supported | Supported |
| [`BitArray`](https://docs.microsoft.com/dotnet/api/system.collections.bitarray?view=netcore-3.1) | Supported | Not supported |
| [`Hashtable`](https://docs.microsoft.com/dotnet/api/system.collections.hashtable?view=netcore-3.1) | Supported | Supported |
| [`Queue`](https://docs.microsoft.com/dotnet/api/system.collections.queue?view=netcore-3.1) | Supported | Supported |
| [`SortedList`](https://docs.microsoft.com/dotnet/api/system.collections.sortedlist?view=netcore-3.1) | Supported | Supported |
| [`Stack`](https://docs.microsoft.com/dotnet/api/system.collections.stack?view=netcore-3.1)* | Supported | Supported |
| [`DictionaryEntry`](https://docs.microsoft.com/dotnet/api/system.collections.dictionaryentry?view=netcore-3.1) | Supported | Supported |
| [`ICollection`](https://docs.microsoft.com/dotnet/api/system.collections.icollection?view=netcore-3.1) | Supported | Supported |
| [`IDictionary`](https://docs.microsoft.com/dotnet/api/system.collections.idictionary?view=netcore-3.1) | Supported | Supported |
| [`IEnumerable`](https://docs.microsoft.com/dotnet/api/system.collections.ienumerable?view=netcore-3.1) | Supported | Supported |
| [`IList`](https://docs.microsoft.com/dotnet/api/system.collections.ilist?view=netcore-3.1) | Supported | Supported |

### [`System.Collections.Generic`](https://docs.microsoft.com/dotnet/api/system.collections.generic?view=netcore-3.1)

| Type | Serialization | Deserialization |
| --- | --- | --- |
| [`Dictionary<string, TValue>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.dictionary-2?view=netcore-3.1) | Supported | Supported |
| [`HashSet<T>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.hashset-1?view=netcore-3.1) | Supported | Supported |
| [`LinkedList<T>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.linkedlist-1?view=netcore-3.1) | Supported | Supported |
| [`LinkedListNode<T>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.linkedlistnode-1?view=netcore-3.1) | Supported | Not supported |
| [`List<T>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.list-1?view=netcore-3.1) | Supported | Supported |
| [`Queue<T>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.queue-1?view=netcore-3.1) | Supported | Supported |
| [`SortedDictionary<string, TValue>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.sorteddictionary-2?view=netcore-3.1) | Supported | Supported |
| [`SortedList<string, TValue>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.sortedlist-2?view=netcore-3.1) | Supported | Supported |
| [`SortedSet<T>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.sortedset-1?view=netcore-3.1) | Supported | Supported |
| [`Stack<T>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.stack-1?view=netcore-3.1)* | Supported | Supported |
| [`KeyValuePair<TKey, TValue>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.keyvaluepair-2?view=netcore-3.1) | Supported | Supported |
| [`IAsyncEnumerable<T>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.iasyncenumerable-1?view=netcore-3.1) | Not supported | Not supported |
| [`ICollection<T>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.icollection-1?view=netcore-3.1) | Supported | Supported |
| [`IDictionary<string, TValue>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.idictionary-2?view=netcore-3.1) | Supported | Supported |
| [`IEnumerable<T>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.ienumerable-1?view=netcore-3.1) | Supported | Supported |
| [`IList<T>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.ilist-1?view=netcore-3.1) | Supported | Supported |
| [`IReadOnlyCollection<T>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.ireadonlycollection-1?view=netcore-3.1) | Supported | Supported |
| [`IReadOnlyDictionary<string, TValue>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.ireadonlydictionary-2?view=netcore-3.1) | Supported | Supported |
| [`IReadOnlyList<T>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1?view=netcore-3.1) | Supported | Supported |
| [`ISet<T>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.iset-1?view=netcore-3.1) | Supported | Supported |

### [`System.Collections.Immutable`](https://docs.microsoft.com/dotnet/api/system.collections.immutable?view=netcore-3.1)

| Type | Serialization | Deserialization |
| --- | --- | --- |
| [`ImmutableArray<T>`](https://docs.microsoft.com/dotnet/api/system.collections.immutable.immutablearray-1?view=netcore-3.1) | Supported | Supported |
| [`ImmutableDictionary<string, TValue>`](https://docs.microsoft.com/dotnet/api/system.collections.immutable.immutabledictionary-2?view=netcore-3.1) | Supported | Supported |
| [`ImmutableHashSet<T>`](https://docs.microsoft.com/dotnet/api/system.collections.immutable.immutablehashset-1?view=netcore-3.1) | Supported | Supported |
| [`IImmutableList<T>`](https://docs.microsoft.com/dotnet/api/system.collections.immutable.iimmutablelist-1?view=netcore-3.1) | Supported | Supported |
| [`ImmutableQueue<T>`](https://docs.microsoft.com/dotnet/api/system.collections.immutable.immutablequeue-1?view=netcore-3.1) | Supported | Supported |
| [`ImmutableSortedDictionary<string, TValue>`](https://docs.microsoft.com/dotnet/api/system.collections.immutable.immutablesorteddictionary-2?view=netcore-3.1) | Supported | Supported |
| [`ImmutableSortedSet<T>`](https://docs.microsoft.com/dotnet/api/system.collections.immutable.immutablesortedset-1?view=netcore-3.1) | Supported | Supported |
| [`ImmutableStack<T>`](https://docs.microsoft.com/dotnet/api/system.collections.immutable.immutablestack-1?view=netcore-3.1)* | Supported | Supported |
| [`IImmutableDictionary<string, TValue>`](https://docs.microsoft.com/dotnet/api/system.collections.immutable.iimmutabledictionary-2?view=netcore-3.1) | Supported | Supported |
| [`IImmutableList<T>`](https://docs.microsoft.com/dotnet/api/system.collections.immutable.iimmutablelist-1?view=netcore-3.1) | Supported | Supported |
| [`IImmutableQueue<T>`](https://docs.microsoft.com/dotnet/api/system.collections.immutable.iimmutablequeue-1?view=netcore-3.1) | Supported | Supported |
| [`IImmutableSet<T>`](https://docs.microsoft.com/dotnet/api/system.collections.immutable.iimmutableset-1?view=netcore-3.1) | Supported | Supported |
| [`IImmutableStack<T>`](https://docs.microsoft.com/dotnet/api/system.collections.immutable.iimmutablestack-1?view=netcore-3.1)* | Supported | Supported |

### [`System.Collections.Specialized`](https://docs.microsoft.com/dotnet/api/system.collections.specialized?view=netcore-3.1)

| Type | Serialization | Deserialization |
| --- | --- | --- |
| [`BitVector32`](https://docs.microsoft.com/dotnet/api/system.collections.specialized.bitvector32?view=netcore-3.1)** | Supported | Not supported |
| [`HybridDictionary`](https://docs.microsoft.com/dotnet/api/system.collections.specialized.hybriddictionary?view=netcore-3.1) | Supported | Supported |
| [`IOrderedDictionary`](https://docs.microsoft.com/dotnet/api/system.collections.specialized.iordereddictionary?view=netcore-3.1) | Supported | Not supported |
| [`ListDictionary`](https://docs.microsoft.com/dotnet/api/system.collections.specialized.listdictionary?view=netcore-3.1) | Supported | Supported |
| [`StringCollection`](https://docs.microsoft.com/dotnet/api/system.collections.specialized.stringcollection?view=netcore-3.1) | Supported | Not supported |
| [`StringDictionary`](https://docs.microsoft.com/dotnet/api/system.collections.specialized.stringdictionary?view=netcore-3.1) | Supported | Not supported |
| [`NameValueCollection`](https://docs.microsoft.com/dotnet/api/system.collections.specialized.namevaluecollection?view=netcore-3.1) | Supported | Not supported |

### [`System.Collections.Concurrent`](https://docs.microsoft.com/dotnet/api/system.collections.concurrent?view=netcore-3.1)

| Type | Serialization | Deserialization |
| --- | --- | --- |
| [`BlockingCollection<T>`](https://docs.microsoft.com/dotnet/api/system.collections.concurrent.blockingcollection-1?view=netcore-3.1) | Supported | Not supported |
| [`ConcurrentBag<T>`](https://docs.microsoft.com/dotnet/api/system.collections.concurrent.concurrentbag-1?view=netcore-3.1) | Supported | NotSupported |
| [`ConcurrentDictionary<string, TValue>`](https://docs.microsoft.com/dotnet/api/system.collections.concurrent.concurrentdictionary-2?view=netcore-3.1) | Supported | Supported |
| [`ConcurrentQueue<T>`](https://docs.microsoft.com/dotnet/api/system.collections.concurrent.concurrentqueue-1?view=netcore-3.1) | Supported | Supported |
| [`ConcurrentStack<T>`](https://docs.microsoft.com/dotnet/api/system.collections.concurrent.stack-1?view=netcore-3.1)* | Supported | Supported |

### [`System.Collections.ObjectModel`](https://docs.microsoft.com/dotnet/api/system.collections.objectmodel?view=netcore-3.1)

| Type | Serialization | Deserialization |
| --- | --- | --- |
| [`Collection<T>`](https://docs.microsoft.com/dotnet/api/system.collections.objectmodel.collection-1?view=netcore-3.1) | Supported | Supported |
| [`ObservableCollection<T>`](https://docs.microsoft.com/dotnet/api/system.collections.objectmodel.observablecollection-1?view=netcore-3.1) | Supported | Supported |
| [`KeyedCollection<string, TValue>`](https://docs.microsoft.com/dotnet/api/system.collections.objectmodel.keyedcollection-2?view=netcore-3.1) | Supported | Not supported |
| [`ReadOnlyCollection<T>`](https://docs.microsoft.com/dotnet/api/system.collections.objectmodel.readonlycollection-1?view=netcore-3.1) | Supported | Not supported |
| [`ReadOnlyObservableCollection<T>`](https://docs.microsoft.com/dotnet/api/system.collections.objectmodel.readonlyobservablecollection-1?view=netcore-3.1) | Supported | Not supported |
| [`ReadOnlyDictionary<string, TValue>`](https://docs.microsoft.com/dotnet/api/system.collections.objectmodel.readonlydictionary-2?view=netcore-3.1) | Supported | Not supported |

## Custom collections

For the purpose of serialization and deserialization in System.Text.Json, any
collection not in the BCL, (i.e. outside `System.Collections[.[.\*]]`), is considered
a custom collection. This includes user-defined types and ASP.NET defined types,
e.g. those in
[Microsoft.Extensions.Primitives](https://docs.microsoft.com/dotnet/api/microsoft.extensions.primitives?view=dotnet-plat-ext-3.1).

A custom collection is supported for deserialization if it fulfils the following:

1. Is not an interface or abstract
2. Has a parameter-less constructor
3. Implements one or more of
   [`IList`](https://docs.microsoft.com/dotnet/api/system.collections.ilist?view=netcore-3.1),
   [`IList<T>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.ilist-1?view=netcore-3.1),
   [`ICollection<T>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.icollection-1?view=netcore-3.1),
   [`IDictionary`](https://docs.microsoft.com/dotnet/api/system.collections.idictionary?view=netcore-3.1),
   [`IDictionary<string, TValue>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.idictionary-2?view=netcore-3.1),
   [`Stack<T>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.stack-1?view=netcore-3.1)\*,
   [`Queue<T>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.queue-1?view=netcore-3.1),
   [`ConcurrentStack<T>`](https://docs.microsoft.com/dotnet/api/system.collections.concurrent.concurrentstack-1?view=netcore-3.1)\*,
   [`ConcurrentQueue<T>`](https://docs.microsoft.com/dotnet/api/system.collections.concurrent.concurrentqueue-1?view=netcore-3.1),
   [`Stack`](https://docs.microsoft.com/dotnet/api/system.collections.stack?view=netcore-3.1)\*,
   and [`Queue`](https://docs.microsoft.com/dotnet/api/system.collections.queue?view=netcore-3.1)
4. The element type is supported by [`JsonSerializer`](https://docs.microsoft.com/dotnet/api/system.text.json.jsonserializer?view=netcore-3.1)

All custom collections (everything that derives from [`IEnumerable`](https://docs.microsoft.com/dotnet/api/system.collections.ienumerable?view=netcore-3.1))
are supported for serialization, as long as their element types are supported.

There are known issues with some custom collections where we don't offer round-trippable support.
These include:

- Support for [`ExpandoObject`](https://docs.microsoft.com/dotnet/api/system.dynamic.expandoobject?view=netcore-3.1): https://github.com/dotnet/corefx/issues/38007
- Support for [`DynamicObject`](https://docs.microsoft.com/dotnet/api/system.dynamic.dynamicobject?view=netcore-3.1): https://github.com/dotnet/corefx/issues/41105
- Support for [`DataTable`](https://docs.microsoft.com/dotnet/api/system.data.datatable?view=netcore-3.1): https://github.com/dotnet/corefx/issues/38712
- Support for [`FormFile`](https://docs.microsoft.com/dotnet/api/microsoft.aspnetcore.http.formfile?view=aspnetcore-3.1): https://github.com/dotnet/corefx/issues/41401
- Support for [`IFormCollection`](https://docs.microsoft.com/dotnet/api/microsoft.aspnetcore.http.iformcollection?view=aspnetcore-3.1)
- Assigning `null` to value-type collections like [`ImmutableArray<T>`](https://docs.microsoft.com/dotnet/api/system.collections.immutable.immutablearray-1?view=netcore-3.1): https://github.com/dotnet/corefx/issues/42399

For more information, see the [open issues in System.Text.Json](https://github.com/dotnet/runtime/issues?q=is%3Aopen+is%3Aissue+label%3Aarea-System.Text.Json).

---

\* [`Stack`](https://docs.microsoft.com/dotnet/api/system.collections.stack?view=netcore-3.1),
[`Stack<T>`](https://docs.microsoft.com/dotnet/api/system.collections.generic.stack-1?view=netcore-3.1),
[`ImmutableStack<T>`](https://docs.microsoft.com/dotnet/api/system.collections.immutable.immutablestack-1?view=netcore-3.1),
[`IImmutableStack<T>`](https://docs.microsoft.com/dotnet/api/system.collections.immutable.iimmutablestack-1?view=netcore-3.1),
and [`ConcurrentStack<T>`](https://docs.microsoft.com/dotnet/api/system.collections.concurrent.concurrentstack-1?view=netcore-3.1)
instances; and instances of types that derive from them; are reversed on serialization. Thus, the serializer does not have round-trippable support
for these types. See https://github.com/dotnet/corefx/issues/41887.

\** No exception is thrown when deserializing [`BitVector32`](https://docs.microsoft.com/dotnet/api/system.collections.specialized.bitvector32?view=netcore-3.1),
but the [`Data`](https://docs.microsoft.com/en-us/dotnet/api/system.collections.specialized.bitvector32.data?view=netcore-3.1)
property skipped because it is read-only (doesn't have a public setter).
