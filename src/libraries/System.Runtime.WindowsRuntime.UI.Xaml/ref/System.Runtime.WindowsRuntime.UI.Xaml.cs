// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace Windows.UI.Xaml
{
    public partial struct CornerRadius
    {
        private int _dummy;
        public CornerRadius(double uniformRadius) { throw null; }
        public CornerRadius(double topLeft, double topRight, double bottomRight, double bottomLeft) { throw null; }
        public double BottomLeft { get { throw null; } set { } }
        public double BottomRight { get { throw null; } set { } }
        public double TopLeft { get { throw null; } set { } }
        public double TopRight { get { throw null; } set { } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(global::Windows.UI.Xaml.CornerRadius cornerRadius) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(global::Windows.UI.Xaml.CornerRadius cr1, global::Windows.UI.Xaml.CornerRadius cr2) { throw null; }
        public static bool operator !=(global::Windows.UI.Xaml.CornerRadius cr1, global::Windows.UI.Xaml.CornerRadius cr2) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial struct Duration
    {
        private int _dummy;
        public Duration(global::System.TimeSpan timeSpan) { throw null; }
        public static global::Windows.UI.Xaml.Duration Automatic { get { throw null; } }
        public static global::Windows.UI.Xaml.Duration Forever { get { throw null; } }
        public bool HasTimeSpan { get { throw null; } }
        public global::System.TimeSpan TimeSpan { get { throw null; } }
        public global::Windows.UI.Xaml.Duration Add(global::Windows.UI.Xaml.Duration duration) { throw null; }
        public static int Compare(global::Windows.UI.Xaml.Duration t1, global::Windows.UI.Xaml.Duration t2) { throw null; }
        public override bool Equals(object value) { throw null; }
        public bool Equals(global::Windows.UI.Xaml.Duration duration) { throw null; }
        public static bool Equals(global::Windows.UI.Xaml.Duration t1, global::Windows.UI.Xaml.Duration t2) { throw null; }
        public override int GetHashCode() { throw null; }
        public static global::Windows.UI.Xaml.Duration operator +(global::Windows.UI.Xaml.Duration t1, global::Windows.UI.Xaml.Duration t2) { throw null; }
        public static bool operator ==(global::Windows.UI.Xaml.Duration t1, global::Windows.UI.Xaml.Duration t2) { throw null; }
        public static bool operator >(global::Windows.UI.Xaml.Duration t1, global::Windows.UI.Xaml.Duration t2) { throw null; }
        public static bool operator >=(global::Windows.UI.Xaml.Duration t1, global::Windows.UI.Xaml.Duration t2) { throw null; }
        public static implicit operator global::Windows.UI.Xaml.Duration(global::System.TimeSpan timeSpan) { throw null; }
        public static bool operator !=(global::Windows.UI.Xaml.Duration t1, global::Windows.UI.Xaml.Duration t2) { throw null; }
        public static bool operator <(global::Windows.UI.Xaml.Duration t1, global::Windows.UI.Xaml.Duration t2) { throw null; }
        public static bool operator <=(global::Windows.UI.Xaml.Duration t1, global::Windows.UI.Xaml.Duration t2) { throw null; }
        public static global::Windows.UI.Xaml.Duration operator -(global::Windows.UI.Xaml.Duration t1, global::Windows.UI.Xaml.Duration t2) { throw null; }
        public static global::Windows.UI.Xaml.Duration operator +(global::Windows.UI.Xaml.Duration duration) { throw null; }
        public global::Windows.UI.Xaml.Duration Subtract(global::Windows.UI.Xaml.Duration duration) { throw null; }
        public override string ToString() { throw null; }
    }
    public enum DurationType
    {
        Automatic = 0,
        Forever = 2,
        TimeSpan = 1,
    }
    public partial struct GridLength
    {
        private int _dummy;
        public GridLength(double pixels) { throw null; }
        public GridLength(double value, global::Windows.UI.Xaml.GridUnitType type) { throw null; }
        public static global::Windows.UI.Xaml.GridLength Auto { get { throw null; } }
        public global::Windows.UI.Xaml.GridUnitType GridUnitType { get { throw null; } }
        public bool IsAbsolute { get { throw null; } }
        public bool IsAuto { get { throw null; } }
        public bool IsStar { get { throw null; } }
        public double Value { get { throw null; } }
        public override bool Equals(object oCompare) { throw null; }
        public bool Equals(global::Windows.UI.Xaml.GridLength gridLength) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(global::Windows.UI.Xaml.GridLength gl1, global::Windows.UI.Xaml.GridLength gl2) { throw null; }
        public static bool operator !=(global::Windows.UI.Xaml.GridLength gl1, global::Windows.UI.Xaml.GridLength gl2) { throw null; }
        public override string ToString() { throw null; }
    }
    public enum GridUnitType
    {
        Auto = 0,
        Pixel = 1,
        Star = 2,
    }
    public partial class LayoutCycleException : global::System.Exception
    {
        public LayoutCycleException() { }
        public LayoutCycleException(string message) { }
        public LayoutCycleException(string message, global::System.Exception innerException) { }
    }
    public partial struct Thickness
    {
        private int _dummy;
        public Thickness(double uniformLength) { throw null; }
        public Thickness(double left, double top, double right, double bottom) { throw null; }
        public double Bottom { get { throw null; } set { } }
        public double Left { get { throw null; } set { } }
        public double Right { get { throw null; } set { } }
        public double Top { get { throw null; } set { } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(global::Windows.UI.Xaml.Thickness thickness) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(global::Windows.UI.Xaml.Thickness t1, global::Windows.UI.Xaml.Thickness t2) { throw null; }
        public static bool operator !=(global::Windows.UI.Xaml.Thickness t1, global::Windows.UI.Xaml.Thickness t2) { throw null; }
        public override string ToString() { throw null; }
    }
}
namespace Windows.UI.Xaml.Automation
{
    public partial class ElementNotAvailableException : global::System.Exception
    {
        public ElementNotAvailableException() { }
        public ElementNotAvailableException(string message) { }
        public ElementNotAvailableException(string message, global::System.Exception innerException) { }
    }
    public partial class ElementNotEnabledException : global::System.Exception
    {
        public ElementNotEnabledException() { }
        public ElementNotEnabledException(string message) { }
        public ElementNotEnabledException(string message, global::System.Exception innerException) { }
    }
}
namespace Windows.UI.Xaml.Controls.Primitives
{
    public partial struct GeneratorPosition
    {
        private int _dummy;
        public GeneratorPosition(int index, int offset) { throw null; }
        public int Index { get { throw null; } set { } }
        public int Offset { get { throw null; } set { } }
        public override bool Equals(object o) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(global::Windows.UI.Xaml.Controls.Primitives.GeneratorPosition gp1, global::Windows.UI.Xaml.Controls.Primitives.GeneratorPosition gp2) { throw null; }
        public static bool operator !=(global::Windows.UI.Xaml.Controls.Primitives.GeneratorPosition gp1, global::Windows.UI.Xaml.Controls.Primitives.GeneratorPosition gp2) { throw null; }
        public override string ToString() { throw null; }
    }
}
namespace Windows.UI.Xaml.Markup
{
    public partial class XamlParseException : global::System.Exception
    {
        public XamlParseException() { }
        public XamlParseException(string message) { }
        public XamlParseException(string message, global::System.Exception innerException) { }
    }
}
namespace Windows.UI.Xaml.Media
{
    public partial struct Matrix : global::System.IFormattable
    {
        private int _dummy;
        public Matrix(double m11, double m12, double m21, double m22, double offsetX, double offsetY) { throw null; }
        public static global::Windows.UI.Xaml.Media.Matrix Identity { get { throw null; } }
        public bool IsIdentity { get { throw null; } }
        public double M11 { get { throw null; } set { } }
        public double M12 { get { throw null; } set { } }
        public double M21 { get { throw null; } set { } }
        public double M22 { get { throw null; } set { } }
        public double OffsetX { get { throw null; } set { } }
        public double OffsetY { get { throw null; } set { } }
        public override bool Equals(object o) { throw null; }
        public bool Equals(global::Windows.UI.Xaml.Media.Matrix value) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(global::Windows.UI.Xaml.Media.Matrix matrix1, global::Windows.UI.Xaml.Media.Matrix matrix2) { throw null; }
        public static bool operator !=(global::Windows.UI.Xaml.Media.Matrix matrix1, global::Windows.UI.Xaml.Media.Matrix matrix2) { throw null; }
        string System.IFormattable.ToString(string format, global::System.IFormatProvider provider) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(global::System.IFormatProvider provider) { throw null; }
        public global::Windows.Foundation.Point Transform(global::Windows.Foundation.Point point) { throw null; }
    }
}
namespace Windows.UI.Xaml.Media.Animation
{
    public partial struct KeyTime
    {
        private int _dummy;
        public global::System.TimeSpan TimeSpan { get { throw null; } }
        public override bool Equals(object value) { throw null; }
        public bool Equals(global::Windows.UI.Xaml.Media.Animation.KeyTime value) { throw null; }
        public static bool Equals(global::Windows.UI.Xaml.Media.Animation.KeyTime keyTime1, global::Windows.UI.Xaml.Media.Animation.KeyTime keyTime2) { throw null; }
        public static global::Windows.UI.Xaml.Media.Animation.KeyTime FromTimeSpan(global::System.TimeSpan timeSpan) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(global::Windows.UI.Xaml.Media.Animation.KeyTime keyTime1, global::Windows.UI.Xaml.Media.Animation.KeyTime keyTime2) { throw null; }
        public static implicit operator global::Windows.UI.Xaml.Media.Animation.KeyTime(global::System.TimeSpan timeSpan) { throw null; }
        public static bool operator !=(global::Windows.UI.Xaml.Media.Animation.KeyTime keyTime1, global::Windows.UI.Xaml.Media.Animation.KeyTime keyTime2) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial struct RepeatBehavior : global::System.IFormattable
    {
        private int _dummy;
        public RepeatBehavior(double count) { throw null; }
        public RepeatBehavior(global::System.TimeSpan duration) { throw null; }
        public double Count { get { throw null; } set { } }
        public global::System.TimeSpan Duration { get { throw null; } set { } }
        public static global::Windows.UI.Xaml.Media.Animation.RepeatBehavior Forever { get { throw null; } }
        public bool HasCount { get { throw null; } }
        public bool HasDuration { get { throw null; } }
        public global::Windows.UI.Xaml.Media.Animation.RepeatBehaviorType Type { get { throw null; } set { } }
        public override bool Equals(object value) { throw null; }
        public bool Equals(global::Windows.UI.Xaml.Media.Animation.RepeatBehavior repeatBehavior) { throw null; }
        public static bool Equals(global::Windows.UI.Xaml.Media.Animation.RepeatBehavior repeatBehavior1, global::Windows.UI.Xaml.Media.Animation.RepeatBehavior repeatBehavior2) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(global::Windows.UI.Xaml.Media.Animation.RepeatBehavior repeatBehavior1, global::Windows.UI.Xaml.Media.Animation.RepeatBehavior repeatBehavior2) { throw null; }
        public static bool operator !=(global::Windows.UI.Xaml.Media.Animation.RepeatBehavior repeatBehavior1, global::Windows.UI.Xaml.Media.Animation.RepeatBehavior repeatBehavior2) { throw null; }
        string System.IFormattable.ToString(string format, global::System.IFormatProvider formatProvider) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(global::System.IFormatProvider formatProvider) { throw null; }
    }
    public enum RepeatBehaviorType
    {
        Count = 0,
        Duration = 1,
        Forever = 2,
    }
}
namespace Windows.UI.Xaml.Media.Media3D
{
    public partial struct Matrix3D : global::System.IFormattable
    {
        private int _dummy;
        public Matrix3D(double m11, double m12, double m13, double m14, double m21, double m22, double m23, double m24, double m31, double m32, double m33, double m34, double offsetX, double offsetY, double offsetZ, double m44) { throw null; }
        public bool HasInverse { get { throw null; } }
        public static global::Windows.UI.Xaml.Media.Media3D.Matrix3D Identity { get { throw null; } }
        public bool IsIdentity { get { throw null; } }
        public double M11 { get { throw null; } set { } }
        public double M12 { get { throw null; } set { } }
        public double M13 { get { throw null; } set { } }
        public double M14 { get { throw null; } set { } }
        public double M21 { get { throw null; } set { } }
        public double M22 { get { throw null; } set { } }
        public double M23 { get { throw null; } set { } }
        public double M24 { get { throw null; } set { } }
        public double M31 { get { throw null; } set { } }
        public double M32 { get { throw null; } set { } }
        public double M33 { get { throw null; } set { } }
        public double M34 { get { throw null; } set { } }
        public double M44 { get { throw null; } set { } }
        public double OffsetX { get { throw null; } set { } }
        public double OffsetY { get { throw null; } set { } }
        public double OffsetZ { get { throw null; } set { } }
        public override bool Equals(object o) { throw null; }
        public bool Equals(global::Windows.UI.Xaml.Media.Media3D.Matrix3D value) { throw null; }
        public override int GetHashCode() { throw null; }
        public void Invert() { }
        public static bool operator ==(global::Windows.UI.Xaml.Media.Media3D.Matrix3D matrix1, global::Windows.UI.Xaml.Media.Media3D.Matrix3D matrix2) { throw null; }
        public static bool operator !=(global::Windows.UI.Xaml.Media.Media3D.Matrix3D matrix1, global::Windows.UI.Xaml.Media.Media3D.Matrix3D matrix2) { throw null; }
        public static global::Windows.UI.Xaml.Media.Media3D.Matrix3D operator *(global::Windows.UI.Xaml.Media.Media3D.Matrix3D matrix1, global::Windows.UI.Xaml.Media.Media3D.Matrix3D matrix2) { throw null; }
        string System.IFormattable.ToString(string format, global::System.IFormatProvider provider) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(global::System.IFormatProvider provider) { throw null; }
    }
}
