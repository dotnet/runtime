using Xunit;

namespace System.ComponentModel.DataAnnotations
{
    public class ScaffoldColumnAttributeTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Can_construct_and_get_Scaffold(bool value)
        {
            var attribute = new ScaffoldColumnAttribute(value);
            Assert.Equal(value, attribute.Scaffold);
        }
    }
}
