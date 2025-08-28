// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler
{
    public class FieldPolicy
    {
        public virtual bool IsReadOnly(FieldDesc field) => field.IsInitOnly;
        public virtual bool IsStaticFieldRead(FieldDesc field) => true;
    }

    public sealed class FieldPolicyWithStaticInitOnly : FieldPolicy
    {
        public override bool IsReadOnly(FieldDesc field) => field.IsStatic && field.IsInitOnly;
    }
}
