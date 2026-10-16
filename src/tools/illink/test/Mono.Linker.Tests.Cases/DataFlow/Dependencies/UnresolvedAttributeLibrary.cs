using System;
using System.Diagnostics.CodeAnalysis;

namespace Mono.Linker.Tests.Cases.DataFlow.Dependencies
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class UnresolvedEnumAttribute : Attribute
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        public Type RequiredType { get; set; }

        public UnresolvedEnum EnumValue { get; set; }

        public int Other { get; set; }
    }

    public sealed class RetainedType
    {
        public void KeptMethod()
        {
        }
    }

    public static class UnresolvedAttributeLibrary
    {
        [UnresolvedEnum(RequiredType = typeof(RetainedType), EnumValue = UnresolvedEnum.Value, Other = 42)]
        public static void Use()
        {
        }
    }
}
