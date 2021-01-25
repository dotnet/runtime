// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(object))]
[assembly: TypeForwardedTo(typeof(TypeInUnloadableAssembly))]

public static class TypeInForwardedAssembly
{
    public class PublicInner
    {
        public class PublicInnerInner {}
        protected class ProtectedInnerInner {}
        internal class InternalInnerInner{}
        private class PrivatelInnerInner{}
        protected internal class ProjectedInternalInnerInner{}
    }

    protected class ProtectedInner {}
    internal class InternalInner{}
    private class PrivatelInner{}
    protected internal class ProjectedInternalInner{}
}
