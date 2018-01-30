using System;

namespace Tracing.Tests.Common
{
    public static class Assert
    {
        public static void Equal<T>(T left, T right) where T : IEquatable<T>
        {
            if (left == null && right != null)
            {
                throw new Exception(
                    string.Format("Values are not equal!  Left=NULL Right='{0}'", right));
            }
            else if (left != null && right == null)
            {
                throw new Exception(
                    string.Format("Values are not equal!  Left='{0}' Right=NULL", left));
            }
            else if (!left.Equals(right))
            {
                throw new Exception(
                    string.Format("Values are not equal! Left='{0}' Right='{1}'", left, right));
            }
        }

        public static void NotEqual<T>(T left, T right) where T : IEquatable<T>
        {
            if (left == null && right == null)
            {
                throw new Exception(
                    "Values are equal! Left=NULL Right=NULL");
            }
            else if (left != null && left.Equals(right))
            {
                throw new Exception(
                    string.Format("Values are equal! Left='{0}' Right='{1}'", left, right));
            }
        }
    }
}
