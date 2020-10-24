// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.Composition.ReflectionModel
{
    internal abstract class ReflectionItem
    {
        public abstract string? Name { get; }
        public abstract string GetDisplayName();
        public abstract Type ReturnType { get; }
        public abstract ReflectionItemType ItemType { get; }
    }
}
