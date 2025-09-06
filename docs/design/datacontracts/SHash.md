# Contract SHash

This contract is for creating and using the SHash data structure.

## APIs of contract

```csharp
public interface ITraits<TKey, TEntry>
{
    abstract TKey GetKey(TEntry entry);
    abstract bool Equals(TKey left, TKey right);
    abstract uint Hash(TKey key);
    abstract bool IsNull(TEntry entry);
    abstract TEntry Null();
    abstract bool IsDeleted(TEntry entry);
}

public interface ISHash<TKey, TEntry> where TEntry : IData<TEntry>
{

}
```

``` csharp
TEntry LookupSHash<TKey, TEntry>(ISHash<TKey, TEntry> hashTable, TKey key) where TEntry : IData<TEntry>;
SHash<TKey, TEntry> CreateSHash<TKey, TEntry>(Target target, TargetPointer address, Target.TypeInfo type, ITraits<TKey, TEntry> traits) where TEntry : IData<TEntry>;
```

## Version 1

In order to properly populate an SHash, we need to know the size of each element, which varies from instantiation to instantiation of SHash. Therefore, we pass as an argument the DataType ```type``` which contains the particular offsets.

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `type` | `Table` | Address of the SHash table |
| `type` | `TableSize` | Number of entries in the table |
| `type` | `EntrySize` | Size in bytes of each table entry |

``` csharp

private class SHash<TKey, TEntry> : ISHash<TKey, TEntry> where TEntry : IData<TEntry>
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
    uint entrySize = target.Read<uint>(address + /* type::EntrySize offset */);
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
TEntry ISHash.LookupSHash<TKey, TEntry>(ISHash<TKey, TEntry> hashTable, TKey key)
{
    SHash<TKey, TEntry> shashTable = (SHash<TKey, TEntry>)hashTable;
    if (shashTable.TableSize == 0)
        return shashTable.Traits!.Null();

    uint hash = shashTable.Traits!.Hash(key);
    uint index = hash % shashTable.TableSize;
    uint increment = 0;
    while (true)
    {
        TEntry current = shashTable.Entries![(int)index];
        if (shashTable.Traits.IsNull(current))
            return shashTable.Traits.Null();
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
