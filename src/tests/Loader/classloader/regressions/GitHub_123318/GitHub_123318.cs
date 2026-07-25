// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// this test verifies that loading using the special marker type will not cause problems with generics with more than 8 parameters


using System;
using Xunit;

public class GitHub_123318
{
    [Fact]
    public static void Test()
    {
        var i = new MyEntity<string, string, string, string, string, string, string, string, string>(); // <-- Throws System.ExecutionEngineException in System.Reflection.MethodBaseInvoker.InvokeWithOneArg(...)
    }

    public sealed class MyEntity<TItem1, TItem2, TItem3, TItem4, TItem5, TItem6, TItem7, TItem8, TItem9> : IEditableEntity<TItem1, TItem2, TItem3, TItem4, TItem5, TItem6, TItem7, TItem8, TItem9>
    {
        public TItem1 Item1 {get; set;}
        public TItem2 Item2 {get; set;}
        public TItem3 Item3 {get; set;}
        public TItem4 Item4 {get; set;}
        public TItem5 Item5 {get; set;}
        public TItem6 Item6 {get; set;}
        public TItem7 Item7 {get; set;}
        public TItem8 Item8 {get; set;}
        public TItem9 Item9 {get; set;}
    }

    public interface IReadonlyEntity<TItem1, TItem2, TItem3, TItem4, TItem5, TItem6, TItem7, TItem8, TItem9>
    {
        TItem1 Item1 {get;}
        TItem2 Item2 {get;}
        TItem3 Item3 {get;}
        TItem4 Item4 {get;}
        TItem5 Item5 {get;}
        TItem6 Item6 {get;}
        TItem7 Item7 {get;}
        TItem8 Item8 {get;}
        TItem9 Item9 {get;}
    }

    public interface IEditableEntity<TItem1, TItem2, TItem3, TItem4, TItem5, TItem6, TItem7, TItem8, TItem9> : IReadonlyEntity<TItem1, TItem2, TItem3, TItem4, TItem5, TItem6, TItem7, TItem8, TItem9>
    {
        new TItem1 Item1 {get;set;}
        new TItem2 Item2 {get;set;}
        new TItem3 Item3 {get;set;}
        new TItem4 Item4 {get;set;}
        new TItem5 Item5 {get;set;}
        new TItem6 Item6 {get;set;}
        new TItem7 Item7 {get;set;}
        new TItem8 Item8 {get;set;}
        new TItem9 Item9 {get;set;}
    }
}
