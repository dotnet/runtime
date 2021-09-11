// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace System.Drawing
{
    /// <summary>
    /// Helpers to allow using System.Numerics types like the System.Drawing equivalents.
    /// </summary>
    internal static class NumericsExtensions
    {
        internal static void Translate(this ref Matrix3x2 matrix, Vector2 offset)
        {
            // Replicating what Matrix.Translate(float offsetX, float offsetY) does.
            matrix.M31 += (offset.X * matrix.M11) + (offset.Y * matrix.M21);
            matrix.M32 += (offset.X * matrix.M12) + (offset.Y * matrix.M22);
        }

        internal static bool IsEmpty(this Vector2 vector) => vector.X == 0 && vector.Y == 0;
    }
}
