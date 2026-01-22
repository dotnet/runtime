// Stub to provide Xunit types when xunit isn't available at compile time
namespace Xunit
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class FactAttribute : System.Attribute { }

    public static class Assert
    {
        public static void Equal<T>(T expected, T actual)
        {
            if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new System.Exception($"Expected {expected}, but got {actual}");
            }
        }

        public static void True(bool condition)
        {
            if (!condition)
                throw new System.Exception("Expected true");
        }

        public static void False(bool condition)
        {
            if (condition)
                throw new System.Exception("Expected false");
        }

        public static void Null(object obj)
        {
            if (obj != null)
                throw new System.Exception("Expected null");
        }

        public static void NotNull(object obj)
        {
            if (obj == null)
                throw new System.Exception("Expected not null");
        }

        public static void Throws<T>(System.Action testCode) where T : System.Exception
        {
            try
            {
                testCode();
                throw new System.Exception($"Expected {typeof(T).Name} but no exception was thrown");
            }
            catch (T)
            {
                // Expected
            }
        }
    }
}
