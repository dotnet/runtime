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
        private int _dummyPrimitive;
        public CornerRadius(double uniformRadius) { throw null; }
        public CornerRadius(double topLeft, double topRight, double bottomRight, double bottomLeft) { throw null; }
        public double BottomLeft { get { throw null; } set { } }
        public double BottomRight { get { throw null; } set { } }
        public double TopLeft { get { throw null; } set { } }
        public double TopRight { get { throw null; } set { } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(Windows.UI.Xaml.CornerRadius cornerRadius) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(Windows.UI.Xaml.CornerRadius cr1, Windows.UI.Xaml.CornerRadius cr2) { throw null; }
        public static bool operator !=(Windows.UI.Xaml.CornerRadius cr1, Windows.UI.Xaml.CornerRadius cr2) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial struct Duration
    {
        private int _dummyPrimitive;
        public Duration(System.TimeSpan timeSpan) { throw null; }
        public static Windows.UI.Xaml.Duration Automatic { get { throw null; } }
        public static Windows.UI.Xaml.Duration Forever { get { throw null; } }
        public bool HasTimeSpan { get { throw null; } }
        public System.TimeSpan TimeSpan { get { throw null; } }
        public Windows.UI.Xaml.Duration Add(Windows.UI.Xaml.Duration duration) { throw null; }
        public static int Compare(Windows.UI.Xaml.Duration t1, Windows.UI.Xaml.Duration t2) { throw null; }
        public override bool Equals(object value) { throw null; }
        public bool Equals(Windows.UI.Xaml.Duration duration) { throw null; }
        public static bool Equals(Windows.UI.Xaml.Duration t1, Windows.UI.Xaml.Duration t2) { throw null; }
        public override int GetHashCode() { throw null; }
        public static Windows.UI.Xaml.Duration operator +(Windows.UI.Xaml.Duration t1, Windows.UI.Xaml.Duration t2) { throw null; }
        public static bool operator ==(Windows.UI.Xaml.Duration t1, Windows.UI.Xaml.Duration t2) { throw null; }
        public static bool operator >(Windows.UI.Xaml.Duration t1, Windows.UI.Xaml.Duration t2) { throw null; }
        public static bool operator >=(Windows.UI.Xaml.Duration t1, Windows.UI.Xaml.Duration t2) { throw null; }
        public static implicit operator Windows.UI.Xaml.Duration (System.TimeSpan timeSpan) { throw null; }
        public static bool operator !=(Windows.UI.Xaml.Duration t1, Windows.UI.Xaml.Duration t2) { throw null; }
        public static bool operator <(Windows.UI.Xaml.Duration t1, Windows.UI.Xaml.Duration t2) { throw null; }
        public static bool operator <=(Windows.UI.Xaml.Duration t1, Windows.UI.Xaml.Duration t2) { throw null; }
        public static Windows.UI.Xaml.Duration operator -(Windows.UI.Xaml.Duration t1, Windows.UI.Xaml.Duration t2) { throw null; }
        public static Windows.UI.Xaml.Duration operator +(Windows.UI.Xaml.Duration duration) { throw null; }
        public Windows.UI.Xaml.Duration Subtract(Windows.UI.Xaml.Duration duration) { throw null; }
        public override string ToString() { throw null; }
    }
    public enum DurationType
    {
        Automatic = 0,
        TimeSpan = 1,
        Forever = 2,
    }
    public partial struct GridLength
    {
        private int _dummyPrimitive;
        public GridLength(double pixels) { throw null; }
        public GridLength(double value, Windows.UI.Xaml.GridUnitType type) { throw null; }
        public static Windows.UI.Xaml.GridLength Auto { get { throw null; } }
        public Windows.UI.Xaml.GridUnitType GridUnitType { get { throw null; } }
        public bool IsAbsolute { get { throw null; } }
        public bool IsAuto { get { throw null; } }
        public bool IsStar { get { throw null; } }
        public double Value { get { throw null; } }
        public override bool Equals(object oCompare) { throw null; }
        public bool Equals(Windows.UI.Xaml.GridLength gridLength) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(Windows.UI.Xaml.GridLength gl1, Windows.UI.Xaml.GridLength gl2) { throw null; }
        public static bool operator !=(Windows.UI.Xaml.GridLength gl1, Windows.UI.Xaml.GridLength gl2) { throw null; }
        public override string ToString() { throw null; }
    }
    public enum GridUnitType
    {
        Auto = 0,
        Pixel = 1,
        Star = 2,
    }
    public partial class LayoutCycleException : System.Exception
    {
        public LayoutCycleException() { }
        public LayoutCycleException(string message) { }
        public LayoutCycleException(string message, System.Exception innerException) { }
    }
    public partial struct Thickness
    {
        private int _dummyPrimitive;
        public Thickness(double uniformLength) { throw null; }
        public Thickness(double left, double top, double right, double bottom) { throw null; }
        public double Bottom { get { throw null; } set { } }
        public double Left { get { throw null; } set { } }
        public double Right { get { throw null; } set { } }
        public double Top { get { throw null; } set { } }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(Windows.UI.Xaml.Thickness thickness) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(Windows.UI.Xaml.Thickness t1, Windows.UI.Xaml.Thickness t2) { throw null; }
        public static bool operator !=(Windows.UI.Xaml.Thickness t1, Windows.UI.Xaml.Thickness t2) { throw null; }
        public override string ToString() { throw null; }
    }
}
namespace Windows.UI.Xaml.Automation
{
    public partial class ElementNotAvailableException : System.Exception
    {
        public ElementNotAvailableException() { }
        public ElementNotAvailableException(string message) { }
        public ElementNotAvailableException(string message, System.Exception innerException) { }
    }
    public partial class ElementNotEnabledException : System.Exception
    {
        public ElementNotEnabledException() { }
        public ElementNotEnabledException(string message) { }
        public ElementNotEnabledException(string message, System.Exception innerException) { }
    }
}
namespace Windows.UI.Xaml.Controls.Primitives
{
    public partial struct GeneratorPosition
    {
        private int _dummyPrimitive;
        public GeneratorPosition(int index, int offset) { throw null; }
        public int Index { get { throw null; } set { } }
        public int Offset { get { throw null; } set { } }
        public override bool Equals(object o) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(Windows.UI.Xaml.Controls.Primitives.GeneratorPosition gp1, Windows.UI.Xaml.Controls.Primitives.GeneratorPosition gp2) { throw null; }
        public static bool operator !=(Windows.UI.Xaml.Controls.Primitives.GeneratorPosition gp1, Windows.UI.Xaml.Controls.Primitives.GeneratorPosition gp2) { throw null; }
        public override string ToString() { throw null; }
    }
}
namespace Windows.UI.Xaml.Markup
{
    public partial class XamlParseException : System.Exception
    {
        public XamlParseException() { }
        public XamlParseException(string message) { }
        public XamlParseException(string message, System.Exception innerException) { }
    }
}
namespace Windows.UI.Xaml.Media
{
    public partial struct Matrix : System.IFormattable
    {
        private int _dummyPrimitive;
        public Matrix(double m11, double m12, double m21, double m22, double offsetX, double offsetY) { throw null; }
        public static Windows.UI.Xaml.Media.Matrix Identity { get { throw null; } }
        public bool IsIdentity { get { throw null; } }
        public double M11 { get { throw null; } set { } }
        public double M12 { get { throw null; } set { } }
        public double M21 { get { throw null; } set { } }
        public double M22 { get { throw null; } set { } }
        public double OffsetX { get { throw null; } set { } }
        public double OffsetY { get { throw null; } set { } }
        public override bool Equals(object o) { throw null; }
        public bool Equals(Windows.UI.Xaml.Media.Matrix value) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(Windows.UI.Xaml.Media.Matrix matrix1, Windows.UI.Xaml.Media.Matrix matrix2) { throw null; }
        public static bool operator !=(Windows.UI.Xaml.Media.Matrix matrix1, Windows.UI.Xaml.Media.Matrix matrix2) { throw null; }
        string System.IFormattable.ToString(string format, System.IFormatProvider provider) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider provider) { throw null; }
        public Windows.Foundation.Point Transform(Windows.Foundation.Point point) { throw null; }
    }
}
namespace Windows.UI.Xaml.Media.Animation
{
    public partial struct KeyTime
    {
        private int _dummyPrimitive;
        public System.TimeSpan TimeSpan { get { throw null; } }
        public override bool Equals(object value) { throw null; }
        public bool Equals(Windows.UI.Xaml.Media.Animation.KeyTime value) { throw null; }
        public static bool Equals(Windows.UI.Xaml.Media.Animation.KeyTime keyTime1, Windows.UI.Xaml.Media.Animation.KeyTime keyTime2) { throw null; }
        public static Windows.UI.Xaml.Media.Animation.KeyTime FromTimeSpan(System.TimeSpan timeSpan) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(Windows.UI.Xaml.Media.Animation.KeyTime keyTime1, Windows.UI.Xaml.Media.Animation.KeyTime keyTime2) { throw null; }
        public static implicit operator Windows.UI.Xaml.Media.Animation.KeyTime (System.TimeSpan timeSpan) { throw null; }
        public static bool operator !=(Windows.UI.Xaml.Media.Animation.KeyTime keyTime1, Windows.UI.Xaml.Media.Animation.KeyTime keyTime2) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial struct RepeatBehavior : System.IFormattable
    {
        private int _dummyPrimitive;
        public RepeatBehavior(double count) { throw null; }
        public RepeatBehavior(System.TimeSpan duration) { throw null; }
        public double Count { get { throw null; } set { } }
        public System.TimeSpan Duration { get { throw null; } set { } }
        public static Windows.UI.Xaml.Media.Animation.RepeatBehavior Forever { get { throw null; } }
        public bool HasCount { get { throw null; } }
        public bool HasDuration { get { throw null; } }
        public Windows.UI.Xaml.Media.Animation.RepeatBehaviorType Type { get { throw null; } set { } }
        public override bool Equals(object value) { throw null; }
        public bool Equals(Windows.UI.Xaml.Media.Animation.RepeatBehavior repeatBehavior) { throw null; }
        public static bool Equals(Windows.UI.Xaml.Media.Animation.RepeatBehavior repeatBehavior1, Windows.UI.Xaml.Media.Animation.RepeatBehavior repeatBehavior2) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(Windows.UI.Xaml.Media.Animation.RepeatBehavior repeatBehavior1, Windows.UI.Xaml.Media.Animation.RepeatBehavior repeatBehavior2) { throw null; }
        public static bool operator !=(Windows.UI.Xaml.Media.Animation.RepeatBehavior repeatBehavior1, Windows.UI.Xaml.Media.Animation.RepeatBehavior repeatBehavior2) { throw null; }
        string System.IFormattable.ToString(string format, System.IFormatProvider formatProvider) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider formatProvider) { throw null; }
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
    public partial struct Matrix3D : System.IFormattable
    {
        private int _dummyPrimitive;
        public Matrix3D(double m11, double m12, double m13, double m14, double m21, double m22, double m23, double m24, double m31, double m32, double m33, double m34, double offsetX, double offsetY, double offsetZ, double m44) { throw null; }
        public bool HasInverse { get { throw null; } }
        public static Windows.UI.Xaml.Media.Media3D.Matrix3D Identity { get { throw null; } }
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
        public bool Equals(Windows.UI.Xaml.Media.Media3D.Matrix3D value) { throw null; }
        public override int GetHashCode() { throw null; }
        public void Invert() { }
        public static bool operator ==(Windows.UI.Xaml.Media.Media3D.Matrix3D matrix1, Windows.UI.Xaml.Media.Media3D.Matrix3D matrix2) { throw null; }
        public static bool operator !=(Windows.UI.Xaml.Media.Media3D.Matrix3D matrix1, Windows.UI.Xaml.Media.Media3D.Matrix3D matrix2) { throw null; }
        public static Windows.UI.Xaml.Media.Media3D.Matrix3D operator *(Windows.UI.Xaml.Media.Media3D.Matrix3D matrix1, Windows.UI.Xaml.Media.Media3D.Matrix3D matrix2) { throw null; }
        string System.IFormattable.ToString(string format, System.IFormatProvider provider) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider provider) { throw null; }
    }
}
