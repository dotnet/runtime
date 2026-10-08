// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public interface IStrongEnumerator<T>
{
}

public interface IStrongEnumerable<out T, TEnumerator>
    where TEnumerator : struct, IStrongEnumerator<T>
{
}
