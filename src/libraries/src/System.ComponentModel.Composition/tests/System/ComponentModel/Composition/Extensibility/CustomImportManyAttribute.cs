// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Composition.Extensibility
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class CustomImportManyAttribute : ImportManyAttribute
    {
        public CustomImportManyAttribute()
        {
        }

        public CustomImportManyAttribute(Type type)
            : base(type)
        {
        }
    }
}
