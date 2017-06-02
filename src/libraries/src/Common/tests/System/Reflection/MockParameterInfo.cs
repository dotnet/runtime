// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Reflection
{
    internal abstract class MockParameterInfo : ParameterInfo
    {
        public override ParameterAttributes Attributes => throw Unexpected;
        public override IEnumerable<CustomAttributeData> CustomAttributes => throw Unexpected;
        public override object DefaultValue => throw Unexpected;
        public override bool Equals(object obj) => throw Unexpected;
        public override object[] GetCustomAttributes(bool inherit) => throw Unexpected;
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw Unexpected;
        public override IList<CustomAttributeData> GetCustomAttributesData() => throw Unexpected;
        public override int GetHashCode() => throw Unexpected;
        public override Type[] GetOptionalCustomModifiers() => throw Unexpected;
        public override Type[] GetRequiredCustomModifiers() => throw Unexpected;
        public override bool HasDefaultValue => throw Unexpected;
        public override bool IsDefined(Type attributeType, bool inherit) => throw Unexpected;
        public override MemberInfo Member => throw Unexpected;
        public override int MetadataToken => throw Unexpected;
        public override string Name => throw Unexpected;
        public override Type ParameterType => throw Unexpected;
        public override int Position => throw Unexpected;
        public override object RawDefaultValue => throw Unexpected;
        public override string ToString() => throw Unexpected;

        protected virtual Exception Unexpected => new Exception("Did not expect to be called.");
   }
}
