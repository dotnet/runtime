// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.ComponentModel.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ScaffoldColumnAttribute : Attribute
    {
        public ScaffoldColumnAttribute(bool scaffold)
        {
            Scaffold = scaffold;
        }

        public bool Scaffold { get; private set; }
    }
}
