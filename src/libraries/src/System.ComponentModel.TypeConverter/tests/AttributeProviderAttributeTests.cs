using Xunit;

namespace System.ComponentModel.Tests
{
    public class AttributeProviderAttributeTests
    {
        [Fact]
        public static void GetPropertyName()
        {
            var typeName = "type";
            var propertyName = "property";
            var attribute = new AttributeProviderAttribute(typeName, propertyName);

            Assert.Equal(propertyName, attribute.PropertyName);
        }

        [Fact]
        public static void GetTypeName_SetByString()
        {
            var typeName = "type";
            var attribute = new AttributeProviderAttribute(typeName);

            Assert.Equal(typeName, attribute.TypeName);
        }

        [Fact]
        public static void GetTypeName_SetByType()
        {
            var type = typeof(AttributeProviderAttribute);
            var attribute = new AttributeProviderAttribute(type);

            Assert.Equal(type.AssemblyQualifiedName, attribute.TypeName);
        }
    }
}
