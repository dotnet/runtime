// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler
{
    public class ReadOnlyFieldPolicy
    {
        public virtual bool IsReadOnly(FieldDesc field) => field.IsInitOnly;
    }

    public sealed class StaticReadOnlyFieldPolicy : ReadOnlyFieldPolicy
    {
        public override bool IsReadOnly(FieldDesc field) => field.IsStatic && field.IsInitOnly;
    }
}
