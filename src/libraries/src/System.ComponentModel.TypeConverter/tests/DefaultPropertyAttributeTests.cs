using Xunit;

namespace System.ComponentModel.Tests
{
    public class DefaultPropertyAttributeTests
    {
        [Fact]
        public static void Equals_SameName()
        {
            var name = "name";
            var firstAttribute = new DefaultPropertyAttribute(name);
            var secondAttribute = new DefaultPropertyAttribute(name);

            Assert.True(firstAttribute.Equals(secondAttribute));
        }

        [Fact]
        public static void GetName()
        {
            var name = "name";
            var attribute = new DefaultPropertyAttribute(name);

            Assert.Equal(name, attribute.Name);
        }
    }
}
