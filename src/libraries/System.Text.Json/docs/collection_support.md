# JsonSerializer collection support in 3.1.x

All collections (any type that derives from IEnumerable) are supported for
serialization with JsonSerializer; we simply call the .GetEnumerator() method
of the collection, and write the elements if they are supported.
Deserialization is more complicated and is not supported for all collections.

This document gives a quick overview of which collections are supported.

## Built-in types

### System.Collections

| Type | Serialization | Deserialization |
| --- | --- | --- |
| ArrayList | Supported | Supported |
| BitArray | Supported | Not supported |
| Hashtable | Supported | Supported |
| Queue | Supported | Supported |
| SortedList | Supported | Supported |
| Stack | Supported | Supported |
| DictionaryEntry | Supported | Supported |
| ICollection | Supported | Supported |
| IDictionary | Supported | Supported |
| IEnumerable | Supported | Supported |
| IList | Supported | Supported |

### System.Collections.Generic

| Type | Serialization | Deserialization |
| --- | --- | --- |
| Dictionary<string, TValue> | Supported | Supported |
| HashSet<T> | Supported | Supported |
| LinkedList<T> | Supported | Supported |
| LinkedListNode<T> | Supported | Not supported |
| List<T> | Supported | Supported |
| Queue<T> | Supported | Supported |
| SortedDictionary<string,TValue> | Supported | Supported |
| SortedList<string,TValue> | Supported | Supported |
| SortedSet<T> | Supported | Supported |
| Stack<T> | Supported | Supported |
| KeyValuePair<TKey, TValue> | Supported | Supported |
| IAsyncEnumerable<T> | Not supported | Not supported |
| ICollection<T> | Supported | Supported |
| IDictionary<string,TValue> | Supported | Supported |
| IEnumerable<T> | Supported | Supported |
| IList<T> | Supported | Supported |
| IReadOnlyCollection<T> | Supported | Supported |
| IReadOnlyDictionary<string,TValue> | Supported | Supported |
| IReadOnlyList<T> | Supported | Supported |
| ISet<T> | Supported | Supported |

### System.Collections.Immutable

| Type | Serialization | Deserialization |
| --- | --- | --- |
| ImmutableArray<T> | Supported | Supported |
| ImmutableDictionary<string, TValue> | Supported | Supported |
| ImmutableHashSet<T> | Supported | Supported |
| ImmutableList<T> | Supported | Supported |
| ImmutableQueue<T> | Supported | Supported |
| ImmutableSortedDictionary<string, TValue> | Supported | Supported |
| ImmutableSortedSet<T> | Supported | Supported |
| ImmutableStack<T> | Supported | Supported |
| IImmutableDictionary<string, TValue> | Supported | Supported |
| IImmutableList<T> | Supported | Supported |
| IImmutableQueue<T> | Supported | Supported |
| IImmutableSet<T> | Supported | Supported |
| IImmutableStack<T> | Supported | Supported |

### System.Collections.Specialized

#### Deserialization heuristics

| Type | Serialization | Deserialization |
| --- | --- | --- |
| BitVector32 | Supported | Not supported |
| HybridDictionary | Supported | Supported |
| IOrderedDictionary | Supported | Not supported |
| ListDictionary | Supported | Supported |
| StringCollection | Supported | Not supported |
| StringDictionary | Supported | Not supported |
| NameValueCollection | Supported | Not supported |

### System.Collections.Concurrent

| Type | Serialization | Deserialization |
| --- | --- | --- |
| BlockingCollection<T> | Supported | Not supported |
| ConcurrentBag<T> | Supported | NotSupported |
| ConcurrentDictionary<string, TValue> | Supported | Supported |
| ConcurrentQueue<T> | Supported | Supported |
| ConcurrentStack<T> | Supported | Supported |

### System.Collections.ObjectModel

| Type | Serialization | Deserialization |
| --- | --- | --- |
| Collection<T> | Supported | Supported |
| ObservableCollection<T> | Supported | Supported |
| KeyedCollection<string, TValue> | Supported | Not supported |
| ReadOnlyCollection<T> | Supported | Not supported |
| ReadOnlyObservableCollection<T> | Supported | Not supported |
| ReadOnlyDictionary<string, TValue> | Supported | Not supported |

## Custom collections

For the purpose of serialization and deserialization in System.Text.Json, any
collection not in the BCL, (i.e. outside System.Collections[.[.*]]), is considered
a custom collection. This includes user-defined types and ASP.NET defined types,
e.g. those in
[Microsoft.Extensions.Primitives](https://docs.microsoft.com/dotnet/api/microsoft.extensions.primitives?view=dotnet-plat-ext-3.1).

A custom collection is supported for deserialization if it fulfils the following:

1. Is not an interface or abstract.
2. Has a parameter-less constructor
3. Implements one or more of IList, IList<T>, ICollection<T>, Stack<T>, Queue<T>,
   Stack, Queue, IDictionary, IDictionary<string, TValue>
4. The element type is supported in System.Text.Json

All custom collections are supported for serialization (everything that derives from IEnumerable),
as long as their element types are supported.

There are known issues with some custom collections where we don't offer rountrippable support.
These include:

- ExpandoObject: https://github.com/dotnet/corefx/issues/38007
- DynamicObject: https://github.com/dotnet/corefx/issues/41105 
- DataTable: https://github.com/dotnet/corefx/issues/38712 
- Microsoft.AspNetCore.Http.FormFile: https://github.com/dotnet/corefx/issues/41401 
