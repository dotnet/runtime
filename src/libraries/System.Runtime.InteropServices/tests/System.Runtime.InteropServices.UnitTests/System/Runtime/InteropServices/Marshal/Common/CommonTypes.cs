// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.Tests.Common
{
    [ComVisible(true)]
    public class GenericClass<T> { }

    [StructLayout(LayoutKind.Sequential)]
    public class SequentialGenericClass<T>
    {
        public T field;
    }

    [ComVisible(true)]
    public class NonGenericClass { }

    [ComVisible(true)]
    public class AbstractClass { }

    [ComVisible(true)]
    public struct GenericStruct<T> { }

    [ComVisible(true)]
    public struct NonGenericStruct { }

    [ComVisible(true)]
    public interface IGenericInterface<T> { }

    [ComVisible(true)]
    public interface INonGenericInterface { }

    [ComVisible(false)]
    public interface INonComVisibleInterface { }

    [ComVisible(false)]
    public class NonComVisibleClass { }

    [ComVisible(false)]
    public struct NonComVisibleStruct { }
}
