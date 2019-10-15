// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Windows.Foundation;

namespace System.Numerics
{
    /// <summary>
    /// Contains conversions from Vector2 to Windows.Foundation.Point and Size, which are all structurally identical.
    /// </summary>
    public static class VectorExtensions
    {
        /// <summary>
        /// Returns a Point whose coordinates are the same as the vector's.
        /// </summary>
        /// <param name="vector">The Vector2 to convert.</param>
        /// <returns>The Point.</returns>
        public static Point ToPoint(this Vector2 vector)
        {
            return new Point(vector.X, vector.Y);
        }

        /// <summary>
        /// Returns a Size whose width and height are equal to the vector's X and Y coordinates.
        /// </summary>
        /// <param name="vector">The Vector2 to convert.</param>
        /// <returns>The Size.</returns>
        public static Size ToSize(this Vector2 vector)
        {
            return new Size(vector.X, vector.Y);
        }

        /// <summary>
        /// Returns a Vector2 whose coordinates are the same as the Point's.
        /// </summary>
        /// <param name="point">The Point to convert.</param>
        /// <returns>The Vector2.</returns>
        public static Vector2 ToVector2(this Point point)
        {
            return new Vector2((float)point.X, (float)point.Y);
        }

        /// <summary>
        /// Returns a Vector2 whose coordinates are the height and width of the Size.
        /// </summary>
        /// <param name="size">The Size to convert.</param>
        /// <returns>The Vector2.</returns>
        public static Vector2 ToVector2(this Size size)
        {
            return new Vector2((float)size.Width, (float)size.Height);
        }
    }
}
