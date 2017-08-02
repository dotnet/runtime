using Xunit;

namespace System.Data.Tests
{
    public class TypedTableBaseExtensionsTests
    {
        [Fact]
        public void AsEnumerable_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => TypedTableBaseExtensions.AsEnumerable<DataRow>(null));
        }

        [Fact]
        public void OrderBy_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => TypedTableBaseExtensions.OrderBy<DataRow, string>(null, row => "abc"));
            AssertExtensions.Throws<ArgumentNullException>("source", () => TypedTableBaseExtensions.OrderBy<DataRow, string>(null, row => "abc", StringComparer.CurrentCulture));
        }

        [Fact]
        public void OrderByDescending_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => TypedTableBaseExtensions.OrderByDescending<DataRow, string>(null, row => "abc"));
            AssertExtensions.Throws<ArgumentNullException>("source", () => TypedTableBaseExtensions.OrderByDescending<DataRow, string>(null, row => "abc", StringComparer.CurrentCulture));
        }

        [Fact]
        public void Select_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => TypedTableBaseExtensions.Select<DataRow, string>(null, row => "abc"));
        }

        [Fact]
        public void Where_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => TypedTableBaseExtensions.Where<DataRow>(null, row => true));
        }
    }
}