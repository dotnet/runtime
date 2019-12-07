namespace System.Text.Json
{
    internal enum ResolvedReferenceHandling
    {
        //TODO: Consider adding WriteReference to use that enum value instead of writeAsReference.
        Ignore,
        Preserve,
        IsReference,
        None
    }
}
