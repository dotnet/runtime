using Xunit;

namespace System.ComponentModel.Tests
{
    public class DefaultEventAttributeTests
    {
        [Fact]
        public static void Equals_SameName()
        {
            var name = "name";
            var firstAttribute = new DefaultEventAttribute(name);
            var secondAttribute = new DefaultEventAttribute(name);

            Assert.True(firstAttribute.Equals(secondAttribute));
        }

        [Fact]
        public static void GetName()
        {
            var name = "name";
            var attribute = new DefaultEventAttribute(name);

            Assert.Equal(name, attribute.Name);
        }
    }
}
