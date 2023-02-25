// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
internal class MCell<T>
{
    private T _t;
    public MCell(T t) { _t = t; }
    public MPair<T, R> GetMPair<R>(R r)
    {
        return new MPair<T, R>(_t, r);
    }
    public void Gather<A, B>(A a, B b)
    {
        MPair<A, B> p1 = new MPair<A, B>(a, b);
        MPair<T, A> p2 = new MPair<T, A>(_t, a);
        MPair<MPair<A, B>, MPair<T, A>> p3 = new MPair<MPair<A, B>, MPair<T, A>>(p1, p2);
        MPair<T, A> p4 = GetMPair<A>(a);
        MCell<A> c1 = new MCell<A>(a);
    }
    public MPair<T, T> GetDuplicate()
    {
        return GetMPair<T>(_t);
    }
}

internal class MPair<R, S> : MCell<R>
{
    private S _s;
    public MPair(R r, S s) : base(r) { _s = s; }
}

public class M
{
    [Fact]
    public static int TestEntryPoint()
    {
        MCell<int> c = new MCell<int>(1);
        MPair<int, string> p = c.GetMPair<string>("2");
        c.Gather<float, long>((float)0.5, (long)0);
        return 100;
    }
}
