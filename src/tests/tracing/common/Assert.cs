// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Tracing.Tests.Common
{
    public static class Assert
    {
        public static void True(string name, bool condition)
        {
            if (!condition)
            {
                throw new Exception(
                    string.Format("Condition '{0}' is not true", name));
            }
        }

        public static void Equal<T>(string name, T left, T right) where T : IEquatable<T>
        {
            if (left == null && right != null)
            {
                throw new Exception(
                    string.Format("Values for '{0}' are not equal!  Left=NULL Right='{1}'", name, right));
            }
            else if (left != null && right == null)
            {
                throw new Exception(
                    string.Format("Values for '{0}' are not equal!  Left='{1}' Right=NULL", name, left));
            }
            else if (!left.Equals(right))
            {
                throw new Exception(
                    string.Format("Values for '{0}' are not equal! Left='{1}' Right='{2}'", name, left, right));
            }
        }

        public static void NotEqual<T>(string name, T left, T right) where T : IEquatable<T>
        {
            if (left == null && right == null)
            {
                throw new Exception(
                    string.Format("Values for '{0}' are equal! Left=NULL Right=NULL", name));
            }
            else if (left != null && left.Equals(right))
            {
                throw new Exception(
                    string.Format("Values for '{0}' are equal! Left='{1}' Right='{2}'", name, left, right));
            }
        }
    }
}
