// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Tests
{
    public class MemberInfoTests
    {
        [Fact]
        public void CustomAttributes_Get_ThrowsNotImplementedException()
        {
            var member = new SubMemberInfo();
            Assert.Throws<NotImplementedException>(() => member.CustomAttributes);
        }

        public static IEnumerable<object[]> CustomAttributes_CustomGetCustomAttributesData_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new CustomAttributeData[0] };
            yield return new object[] { new CustomAttributeData[] { null } };
            yield return new object[] { new CustomAttributeData[] { new SubCustomAttributeData() } };
        }

        [Theory]
        [MemberData(nameof(CustomAttributes_CustomGetCustomAttributesData_TestData))]
        public void CustomAttributes_GetCustomGetCustomAttributesData_Success(IList<CustomAttributeData> result)
        {
            var member = new CustomMemberInfo
            {
                GetCustomAttributesDataAction = () => result
            };
            Assert.Same(result, member.CustomAttributes);
        }

        [Fact]
        public void IsCollectible_Get_ReturnsTrue()
        {
            var member = new SubMemberInfo();
            Assert.True(member.IsCollectible);
        }

        [Fact]
        public void MetadataToken_Get_ThrowsInvalidOperationException()
        {
            var member = new SubMemberInfo();
            Assert.Throws<InvalidOperationException>(() => member.MetadataToken);
        }

        [Fact]
        public void Module_Get_ThrowsNotImplementedException()
        {
            var member = new SubMemberInfo();
            Assert.Throws<NotImplementedException>(() => member.Module);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var member = new SubMemberInfo();
            yield return new object[] { member, null, false };
            yield return new object[] { member, new object(), false };
            yield return new object[] { member, new SubMemberInfo(), false };
            yield return new object[] { member, member, true };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Invoke_ReturnsExpected(MemberInfo member, object obj, bool expected)
        {
            Assert.Equal(expected, member.Equals(obj));
            if (obj is MemberInfo other)
            {
                Assert.Equal(expected, member.GetHashCode().Equals(other.GetHashCode()));
            }
        }

        public static IEnumerable<object[]> OperatorEquals_TestData()
        {
            var member = new SubMemberInfo();
            yield return new object[] { null, null, true };
            yield return new object[] { null, member, false };
            yield return new object[] { member, null, false };
            yield return new object[] { member, new SubMemberInfo(), false };
            yield return new object[] { member, member, true };

            yield return new object[] { new AlwaysEqualsMemberInfo(), null, false };
            yield return new object[] { null, new AlwaysEqualsMemberInfo(), false };
            yield return new object[] { new AlwaysEqualsMemberInfo(), new SubMemberInfo(), true };
            yield return new object[] { new SubMemberInfo(), new AlwaysEqualsMemberInfo(), false };
            yield return new object[] { new AlwaysEqualsMemberInfo(), new AlwaysEqualsMemberInfo(), true };
        }

        [Theory]
        [MemberData(nameof(OperatorEquals_TestData))]
        public void OperatorEquals_Invoke_ReturnsExpected(MemberInfo member1, MemberInfo member2, bool expected)
        {
            Assert.Equal(expected, member1 == member2);
            Assert.Equal(!expected, member1 != member2);
        }

        [Fact]
        public void GetCustomAttributesData_Invoke_ThrowsNotImplementedException()
        {
            var member = new SubMemberInfo();
            Assert.Throws<NotImplementedException>(() => member.GetCustomAttributesData());
        }

        public static IEnumerable<object[]> HasSameMetadataDefinitionAs_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new SubMemberInfo() };
            yield return new object[] { typeof(TypeTests).GetMembers()[0] };
        }

        [Theory]
        [MemberData(nameof(HasSameMetadataDefinitionAs_TestData))]
        public void HasSameMetadataDefinitionAs_Invoke_ThrowsNotImplementedException(MemberInfo other)
        {
            var member = new SubMemberInfo();
            Assert.Throws<NotImplementedException>(() => member.HasSameMetadataDefinitionAs(other));
        }

        private class SubCustomAttributeData : CustomAttributeData
        {
        }

        private class CustomMemberInfo : SubMemberInfo
        {
            public Func<IList<CustomAttributeData>> GetCustomAttributesDataAction { get; set; }

            public override IList<CustomAttributeData> GetCustomAttributesData()
            {
                if (GetCustomAttributesDataAction == null)
                {
                    return base.GetCustomAttributesData();
                }

                return GetCustomAttributesDataAction();
            }
        }

        private class AlwaysEqualsMemberInfo : SubMemberInfo
        {
            public override bool Equals(object obj) => true;

            public override int GetHashCode() => base.GetHashCode();
        }

        private class SubMemberInfo : MemberInfo
        {
            public override MemberTypes MemberType => throw new NotImplementedException();

            public override string Name => throw new NotImplementedException();

            public override Type DeclaringType => throw new NotImplementedException();

            public override Type ReflectedType => throw new NotImplementedException();

            public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();

            public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();

            public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();
        }
    }
}
