using System;

public class C {
    public void F<T>() where T : class, new() { }
    public void G<T>() where T : struct { }
}