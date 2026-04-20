// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public struct LocalStruct { public int Value; }

public static class MultiInlinerConsumer
{
    public static int UseA()
    {
        var wrapper = new GenericWrapperA<LocalStruct>(new LocalStruct { Value = 1 });
        return wrapper.InvokeGetValue();
    }

    public static int UseB()
    {
        var wrapper = new GenericWrapperB<LocalStruct>(new LocalStruct { Value = 2 });
        return wrapper.InvokeGetValue();
    }
}
