// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Drawing
{
    /// <summary>
    /// Represents an ordered pair of x and y coordinates that define a point in a two-dimensional plane.
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public struct PointF : IEquatable<PointF>
    {
        /// <summary>
        /// Creates a new instance of the <see cref='System.Drawing.PointF'/> class with member data left uninitialized.
        /// </summary>
        public static readonly PointF Empty;
        private float x; // Do not rename (binary serialization)
        private float y; // Do not rename (binary serialization)

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Drawing.PointF'/> class with the specified coordinates.
        /// </summary>
        public PointF(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Drawing.PointF'/> struct from the specified
        /// <see cref="System.Numerics.Vector2"/>.
        /// </summary>
        public PointF(Vector2 vector)
        {
            x = vector.X;
            y = vector.Y;
        }

        /// <summary>
        /// Creates a new <see cref="System.Numerics.Vector2"/> from this <see cref="System.Drawing.PointF"/>.
        /// </summary>
        public Vector2 ToVector2() => new Vector2(x, y);

        /// <summary>
        /// Gets a value indicating whether this <see cref='System.Drawing.PointF'/> is empty.
        /// </summary>
        [Browsable(false)]
        public readonly bool IsEmpty => x == 0f && y == 0f;

        /// <summary>
        /// Gets the x-coordinate of this <see cref='System.Drawing.PointF'/>.
        /// </summary>
        public float X
        {
            readonly get => x;
            set => x = value;
        }

        /// <summary>
        /// Gets the y-coordinate of this <see cref='System.Drawing.PointF'/>.
        /// </summary>
        public float Y
        {
            readonly get => y;
            set => y = value;
        }

        /// <summary>
        /// Converts the specified <see cref="System.Drawing.PointF"/> to a <see cref="System.Numerics.Vector2"/>.
        /// </summary>
        public static explicit operator Vector2(PointF point) => point.ToVector2();

        /// <summary>
        /// Converts the specified <see cref="System.Numerics.Vector2"/> to a <see cref="System.Drawing.PointF"/>.
        /// </summary>
        public static explicit operator PointF(Vector2 vector) => new PointF(vector);

        /// <summary>
        /// Translates a <see cref='System.Drawing.PointF'/> by a given <see cref='System.Drawing.Size'/> .
        /// </summary>
        public static PointF operator +(PointF pt, Size sz) => Add(pt, sz);

        /// <summary>
        /// Translates a <see cref='System.Drawing.PointF'/> by the negative of a given <see cref='System.Drawing.Size'/> .
        /// </summary>
        public static PointF operator -(PointF pt, Size sz) => Subtract(pt, sz);

        /// <summary>
        /// Translates a <see cref='System.Drawing.PointF'/> by a given <see cref='System.Drawing.SizeF'/> .
        /// </summary>
        public static PointF operator +(PointF pt, SizeF sz) => Add(pt, sz);

        /// <summary>
        /// Translates a <see cref='System.Drawing.PointF'/> by the negative of a given <see cref='System.Drawing.SizeF'/> .
        /// </summary>
        public static PointF operator -(PointF pt, SizeF sz) => Subtract(pt, sz);

        /// <summary>
        /// Compares two <see cref='System.Drawing.PointF'/> objects. The result specifies whether the values of the
        /// <see cref='System.Drawing.PointF.X'/> and <see cref='System.Drawing.PointF.Y'/> properties of the two
        /// <see cref='System.Drawing.PointF'/> objects are equal.
        /// </summary>
        public static bool operator ==(PointF left, PointF right) => left.X == right.X && left.Y == right.Y;

        /// <summary>
        /// Compares two <see cref='System.Drawing.PointF'/> objects. The result specifies whether the values of the
        /// <see cref='System.Drawing.PointF.X'/> or <see cref='System.Drawing.PointF.Y'/> properties of the two
        /// <see cref='System.Drawing.PointF'/> objects are unequal.
        /// </summary>
        public static bool operator !=(PointF left, PointF right) => !(left == right);

        /// <summary>
        /// Translates a <see cref='System.Drawing.PointF'/> by a given <see cref='System.Drawing.Size'/> .
        /// </summary>
        public static PointF Add(PointF pt, Size sz) => new PointF(pt.X + sz.Width, pt.Y + sz.Height);

        /// <summary>
        /// Translates a <see cref='System.Drawing.PointF'/> by the negative of a given <see cref='System.Drawing.Size'/> .
        /// </summary>
        public static PointF Subtract(PointF pt, Size sz) => new PointF(pt.X - sz.Width, pt.Y - sz.Height);

        /// <summary>
        /// Translates a <see cref='System.Drawing.PointF'/> by a given <see cref='System.Drawing.SizeF'/> .
        /// </summary>
        public static PointF Add(PointF pt, SizeF sz) => new PointF(pt.X + sz.Width, pt.Y + sz.Height);

        /// <summary>
        /// Translates a <see cref='System.Drawing.PointF'/> by the negative of a given <see cref='System.Drawing.SizeF'/> .
        /// </summary>
        public static PointF Subtract(PointF pt, SizeF sz) => new PointF(pt.X - sz.Width, pt.Y - sz.Height);

        public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is PointF && Equals((PointF)obj);

        public readonly bool Equals(PointF other) => this == other;

        public override readonly int GetHashCode() => HashCode.Combine(X.GetHashCode(), Y.GetHashCode());

        public override readonly string ToString() => "{X=" + x.ToString() + ", Y=" + y.ToString() + "}";
    }
}
