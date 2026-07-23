# Contract SHash

This contract is for creating and using the SHash data structure.

## APIs of contract

```csharp
public interface ITraits<TKey, TEntry>
{
    TKey GetKey(TEntry entry);
    bool Equals(TKey left, TKey right);
    uint Hash(TKey key);
    bool IsNull(TEntry entry);
    bool IsDeleted(TEntry entry);
}

public interface ISHash<TKey, TEntry> where TEntry : class, IData<TEntry>
{

}
```

``` csharp
TEntry? LookupSHash<TKey, TEntry>(ISHash<TKey, TEntry> hashTable, TKey key) where TEntry : class, IData<TEntry>;
SHash<TKey, TEntry> CreateSHash<TKey, TEntry>(Target target, TargetPointer address, Target.TypeInfo type, ITraits<TKey, TEntry> traits) where TEntry : class, IData<TEntry>;
```

`LookupSHash` returns `null` when no entry matches the key (rather than a
sentinel produced by the traits). `TEntry` is therefore constrained to be a
reference type (`class`).

## Version 1

In order to properly populate an SHash, we need to know the size of each element, which varies from instantiation to instantiation of SHash. Therefore, we pass as an argument the DataType ```type``` which contains the particular offsets.

<!-- BEGIN GENERATED: usage contract=SHash version=c1 -->
### Data descriptors used

_None._

### Global variables used

_None._

### Contracts used

_None._
<!-- END GENERATED: usage contract=SHash version=c1 -->


``` csharp

private class SHash<TKey, TEntry> : ISHash<TKey, TEntry> where TEntry : class, IData<TEntry>
{
    public TargetPointer Table { get; set; }
    public uint TableSize { get; set; }
    public uint EntrySize { get; set; }
    public List<TEntry>? Entries { get; set; }
    public ITraits<TKey, TEntry>? Traits { get; set; }
}

ISHash<TKey, TEntry> ISHash.CreateSHash<TKey, TEntry>(Target target, TargetPointer address, Target.TypeInfo type, ITraits<TKey, TEntry> traits)
{
    TargetPointer table = target.ReadPointer(address + /* type::Table offset */);
    uint tableSize = target.Read<uint>(address + /* type::TableSize offset */);
    uint entrySize = type.Size ?? 0;
    List<TEntry> entries = [];
    for (int i = 0; i < tableSize; i++)
    {
        TargetPointer entryAddress = table + (ulong)(i * entrySize);
        TEntry entry = new TEntry(entryAddress);
        entries.Add(entry);
    }
    return new SHash<TKey, TEntry>
    {
        Table = table,
        TableSize = tableSize,
        EntrySize = entrySize,
        Traits = traits,
        Entries = entries
    };
}
TEntry? ISHash.LookupSHash<TKey, TEntry>(ISHash<TKey, TEntry> hashTable, TKey key)
{
    SHash<TKey, TEntry> shashTable = (SHash<TKey, TEntry>)hashTable;
    if (shashTable.TableSize == 0)
        return null;

    uint hash = shashTable.Traits!.Hash(key);
    uint index = hash % shashTable.TableSize;
    uint increment = 0;
    while (true)
    {
        TEntry current = shashTable.Entries![(int)index];
        if (shashTable.Traits.IsNull(current))
            return null;
        // we don't support the removal of entries
        if (!shashTable.Traits.IsDeleted(current) && shashTable.Traits.Equals(key, shashTable.Traits.GetKey(current)))
            return current;

        if (increment == 0)
            increment = (hash % (shashTable.TableSize - 1)) + 1;

        index += increment;
        if (index >= shashTable.TableSize)
            index -= shashTable.TableSize;
    }
}
```
