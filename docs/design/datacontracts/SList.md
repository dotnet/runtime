# Contract SList

This contract allows reading and iterating over an SList data structure.

## Data structures defined by contract
``` csharp
class SListReader
{
    public abstract TargetPointer GetHead(TargetPointer slistPointer);
    public abstract TargetPointer GetNext(TargetPointer entryInSList);
    public IEnumerator<TargetPointer> EnumerateList(TargetPointer slistPointer)
    {
        TargetPointer current = GetHead(slistPointer);
        
        while (current != TargetPointer.Null)
        {
            yield return current;
            current = GetNext(current);
        }
    }
    public IEnumerator<TargetPointer> EnumerateListFromEntry(TargetPointer entryInSList)
    {
        TargetPointer current = entryInSList;
        
        while (current != TargetPointer.Null)
        {
            yield return current;
            current = GetNext(current);
        }
    }
}
```

## Apis of contract
``` csharp
SListReader GetReader(string typeOfDataStructure);
```

## Version 1

``` csharp
private class SListReaderV1 : SListReader
{
    uint _offsetToSLinkField;
    Target Target;

    SListReaderV1(Target target, string typeToEnumerate)
    {
        Target = target;
        _offsetToSLinkField = Target.Contracts.GetFieldLayout(typeToEnumerate, "m_Link").Offset;
    }
    public override TargetPointer GetHead(TargetPointer slistPointer)
    {
        TargetPointer headPointer = new SListBase(Target, slistPointer).m_pHead;
        TargetPointer slinkInHeadObject = new SLink(Target, headPointer).m_pNext;
        if (slinkInHeadObject == TargetPointer.Null)
            return TargetPointer.Null;
        return slinkInHeadObject - _offsetToSLinkField;
    }

    public override TargetPointer GetNext(TargetPointer entryInSList)
    {
        if (entryInSList == TargetPointer.Null)
            throw new ArgumentException();
        
        TargetPointer slinkPointer = entryInSList + _offsetToSLinkField;
        TargetPointer slinkInObject = new SLink(Target, slinkPointer).m_pNext;
        if (slinkInObject == TargetPointer.Null)
            return TargetPointer.Null;
        return slinkInHeadObject - _offsetToSLinkField;
    }
}

SListReader GetReader(string typeOfDataStructure)
{
    return new SListReaderV1(typeOfDataStructure);
}
```
