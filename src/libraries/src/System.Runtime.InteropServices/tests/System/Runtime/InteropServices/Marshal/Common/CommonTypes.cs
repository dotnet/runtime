// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.Tests.Common
{
    [ComVisible(true)]
    public class GenericClass<T> { }

    [ComVisible(true)]
    public class NonGenericClass { }

    [ComVisible(true)]
    public class AbstractClass { }

    [ComVisible(true)]
    public struct GenericStruct<T> { }

    [ComVisible(true)]
    public struct NonGenericStruct { }

    [ComVisible(true)]
    public interface GenericInterface<T> { }

    [ComVisible(true)]
    public interface NonGenericInterface { }

    [ComVisible(false)]
    public interface NonComVisibleInterface { }

    [ComVisible(false)]
    public class NonComVisibleClass { }

    [ComVisible(false)]
    public struct NonComVisibleStruct { }
}
