// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Microsoft.VisualBasic
{
    internal sealed class VBTypeAttributeConverter : VBModifierAttributeConverter
    {
        private VBTypeAttributeConverter() { } // no  need to create an instance; use Default

        public static VBTypeAttributeConverter Default { get; } = new VBTypeAttributeConverter();
        protected override string[] Names { get; } = new[] { "Public", "Friend" };
        protected override object[] Values { get; } = new object[] { TypeAttributes.Public, TypeAttributes.NotPublic };
        protected override object DefaultValue => TypeAttributes.Public;
    }
}
