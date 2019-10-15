// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.CodeDom;

namespace Microsoft.VisualBasic
{
    internal sealed class VBMemberAttributeConverter : VBModifierAttributeConverter
    {
        private VBMemberAttributeConverter() { } // no  need to create an instance; use Default

        public static VBMemberAttributeConverter Default { get; } = new VBMemberAttributeConverter();

        protected override string[] Names { get; } = new[] { "Public", "Protected", "Protected Friend", "Friend", "Private" };

        protected override object[] Values { get; } =
            new object[] { MemberAttributes.Public, MemberAttributes.Family, MemberAttributes.FamilyOrAssembly, MemberAttributes.Assembly, MemberAttributes.Private };

        protected override object DefaultValue => MemberAttributes.Private;
    }
}
