using System;

public static class SyncTypeAndMethod
{
    public static int GetCompositeValue() => 100;

    public class CompositeType
    {
        public string Name { get; set; } = "Composite";
    }
}
