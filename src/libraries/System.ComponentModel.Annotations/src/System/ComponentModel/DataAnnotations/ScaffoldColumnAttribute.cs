// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ScaffoldColumnAttribute : Attribute
    {
        public ScaffoldColumnAttribute(bool scaffold)
        {
            Scaffold = scaffold;
        }

        public bool Scaffold { get; }
    }
}
