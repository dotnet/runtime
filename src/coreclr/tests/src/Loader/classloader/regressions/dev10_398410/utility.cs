// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

public interface I
{
    string Print<T>();
}

public class MyBase
{
    public virtual string Print<T>() { return "MyBase.Print<" + typeof(T) + "> called"; }
}
