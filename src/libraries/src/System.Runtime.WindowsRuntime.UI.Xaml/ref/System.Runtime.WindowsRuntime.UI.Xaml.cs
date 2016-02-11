// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace Windows.UI.Xaml
{
    [global::System.Security.SecurityCriticalAttribute]
    [global::System.Runtime.InteropServices.StructLayoutAttribute(global::System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct CornerRadius
    {
        public CornerRadius(double uniformRadius) { throw new global::System.NotImplementedException(); }
        public CornerRadius(double topLeft, double topRight, double bottomRight, double bottomLeft) { throw new global::System.NotImplementedException(); }
        public double BottomLeft { get { return default(double); } set { } }
        public double BottomRight { get { return default(double); } set { } }
        public double TopLeft { get { return default(double); } set { } }
        public double TopRight { get { return default(double); } set { } }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(global::Windows.UI.Xaml.CornerRadius cornerRadius) { return default(bool); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(global::Windows.UI.Xaml.CornerRadius cr1, global::Windows.UI.Xaml.CornerRadius cr2) { return default(bool); }
        public static bool operator !=(global::Windows.UI.Xaml.CornerRadius cr1, global::Windows.UI.Xaml.CornerRadius cr2) { return default(bool); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { return default(string); }
    }
    [global::System.Security.SecurityCriticalAttribute]
    [global::System.Runtime.InteropServices.StructLayoutAttribute(global::System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Duration
    {
        public Duration(global::System.TimeSpan timeSpan) { throw new global::System.NotImplementedException(); }
        public static global::Windows.UI.Xaml.Duration Automatic { get { return default(global::Windows.UI.Xaml.Duration); } }
        public static global::Windows.UI.Xaml.Duration Forever { get { return default(global::Windows.UI.Xaml.Duration); } }
        public bool HasTimeSpan { get { return default(bool); } }
        public global::System.TimeSpan TimeSpan { get { return default(global::System.TimeSpan); } }
        public global::Windows.UI.Xaml.Duration Add(global::Windows.UI.Xaml.Duration duration) { return default(global::Windows.UI.Xaml.Duration); }
        public static int Compare(global::Windows.UI.Xaml.Duration t1, global::Windows.UI.Xaml.Duration t2) { return default(int); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override bool Equals(object value) { return default(bool); }
        public bool Equals(global::Windows.UI.Xaml.Duration duration) { return default(bool); }
        public static bool Equals(global::Windows.UI.Xaml.Duration t1, global::Windows.UI.Xaml.Duration t2) { return default(bool); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override int GetHashCode() { return default(int); }
        public static global::Windows.UI.Xaml.Duration operator +(global::Windows.UI.Xaml.Duration t1, global::Windows.UI.Xaml.Duration t2) { return default(global::Windows.UI.Xaml.Duration); }
        public static bool operator ==(global::Windows.UI.Xaml.Duration t1, global::Windows.UI.Xaml.Duration t2) { return default(bool); }
        public static bool operator >(global::Windows.UI.Xaml.Duration t1, global::Windows.UI.Xaml.Duration t2) { return default(bool); }
        public static bool operator >=(global::Windows.UI.Xaml.Duration t1, global::Windows.UI.Xaml.Duration t2) { return default(bool); }
        public static implicit operator global::Windows.UI.Xaml.Duration(global::System.TimeSpan timeSpan) { return default(global::Windows.UI.Xaml.Duration); }
        public static bool operator !=(global::Windows.UI.Xaml.Duration t1, global::Windows.UI.Xaml.Duration t2) { return default(bool); }
        public static bool operator <(global::Windows.UI.Xaml.Duration t1, global::Windows.UI.Xaml.Duration t2) { return default(bool); }
        public static bool operator <=(global::Windows.UI.Xaml.Duration t1, global::Windows.UI.Xaml.Duration t2) { return default(bool); }
        public static global::Windows.UI.Xaml.Duration operator -(global::Windows.UI.Xaml.Duration t1, global::Windows.UI.Xaml.Duration t2) { return default(global::Windows.UI.Xaml.Duration); }
        public static global::Windows.UI.Xaml.Duration operator +(global::Windows.UI.Xaml.Duration duration) { return default(global::Windows.UI.Xaml.Duration); }
        public global::Windows.UI.Xaml.Duration Subtract(global::Windows.UI.Xaml.Duration duration) { return default(global::Windows.UI.Xaml.Duration); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { return default(string); }
    }
    [global::System.Security.SecurityCriticalAttribute]
    public enum DurationType
    {
        Automatic = 0,
        Forever = 2,
        TimeSpan = 1,
    }
    [global::System.Security.SecurityCriticalAttribute]
    [global::System.Runtime.InteropServices.StructLayoutAttribute(global::System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct GridLength
    {
        public GridLength(double pixels) { throw new global::System.NotImplementedException(); }
        public GridLength(double value, global::Windows.UI.Xaml.GridUnitType type) { throw new global::System.NotImplementedException(); }
        public static global::Windows.UI.Xaml.GridLength Auto { get { return default(global::Windows.UI.Xaml.GridLength); } }
        public global::Windows.UI.Xaml.GridUnitType GridUnitType { get { return default(global::Windows.UI.Xaml.GridUnitType); } }
        public bool IsAbsolute { get { return default(bool); } }
        public bool IsAuto { get { return default(bool); } }
        public bool IsStar { get { return default(bool); } }
        public double Value { get { return default(double); } }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override bool Equals(object oCompare) { return default(bool); }
        public bool Equals(global::Windows.UI.Xaml.GridLength gridLength) { return default(bool); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(global::Windows.UI.Xaml.GridLength gl1, global::Windows.UI.Xaml.GridLength gl2) { return default(bool); }
        public static bool operator !=(global::Windows.UI.Xaml.GridLength gl1, global::Windows.UI.Xaml.GridLength gl2) { return default(bool); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { return default(string); }
    }
    [global::System.Security.SecurityCriticalAttribute]
    public enum GridUnitType
    {
        Auto = 0,
        Pixel = 1,
        Star = 2,
    }
    [global::System.Security.SecurityCriticalAttribute]
    public partial class LayoutCycleException : global::System.Exception
    {
        public LayoutCycleException() { }
        public LayoutCycleException(string message) { }
        public LayoutCycleException(string message, global::System.Exception innerException) { }
    }
    [global::System.Security.SecurityCriticalAttribute]
    [global::System.Runtime.InteropServices.StructLayoutAttribute(global::System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Thickness
    {
        public Thickness(double uniformLength) { throw new global::System.NotImplementedException(); }
        public Thickness(double left, double top, double right, double bottom) { throw new global::System.NotImplementedException(); }
        public double Bottom { get { return default(double); } set { } }
        public double Left { get { return default(double); } set { } }
        public double Right { get { return default(double); } set { } }
        public double Top { get { return default(double); } set { } }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(global::Windows.UI.Xaml.Thickness thickness) { return default(bool); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(global::Windows.UI.Xaml.Thickness t1, global::Windows.UI.Xaml.Thickness t2) { return default(bool); }
        public static bool operator !=(global::Windows.UI.Xaml.Thickness t1, global::Windows.UI.Xaml.Thickness t2) { return default(bool); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { return default(string); }
    }
}
namespace Windows.UI.Xaml.Automation
{
    [global::System.Security.SecurityCriticalAttribute]
    public partial class ElementNotAvailableException : global::System.Exception
    {
        public ElementNotAvailableException() { }
        public ElementNotAvailableException(string message) { }
        public ElementNotAvailableException(string message, global::System.Exception innerException) { }
    }
    [global::System.Security.SecurityCriticalAttribute]
    public partial class ElementNotEnabledException : global::System.Exception
    {
        public ElementNotEnabledException() { }
        public ElementNotEnabledException(string message) { }
        public ElementNotEnabledException(string message, global::System.Exception innerException) { }
    }
}
namespace Windows.UI.Xaml.Controls.Primitives
{
    [global::System.Security.SecurityCriticalAttribute]
    [global::System.Runtime.InteropServices.StructLayoutAttribute(global::System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct GeneratorPosition
    {
        public GeneratorPosition(int index, int offset) { throw new global::System.NotImplementedException(); }
        public int Index { get { return default(int); } set { } }
        public int Offset { get { return default(int); } set { } }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override bool Equals(object o) { return default(bool); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(global::Windows.UI.Xaml.Controls.Primitives.GeneratorPosition gp1, global::Windows.UI.Xaml.Controls.Primitives.GeneratorPosition gp2) { return default(bool); }
        public static bool operator !=(global::Windows.UI.Xaml.Controls.Primitives.GeneratorPosition gp1, global::Windows.UI.Xaml.Controls.Primitives.GeneratorPosition gp2) { return default(bool); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { return default(string); }
    }
}
namespace Windows.UI.Xaml.Markup
{
    [global::System.Security.SecurityCriticalAttribute]
    public partial class XamlParseException : global::System.Exception
    {
        public XamlParseException() { }
        public XamlParseException(string message) { }
        public XamlParseException(string message, global::System.Exception innerException) { }
    }
}
namespace Windows.UI.Xaml.Media
{
    [global::System.Security.SecurityCriticalAttribute]
    [global::System.Runtime.InteropServices.StructLayoutAttribute(global::System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Matrix : global::System.IFormattable
    {
        public Matrix(double m11, double m12, double m21, double m22, double offsetX, double offsetY) { throw new global::System.NotImplementedException(); }
        public static global::Windows.UI.Xaml.Media.Matrix Identity { get { return default(global::Windows.UI.Xaml.Media.Matrix); } }
        public bool IsIdentity { get { return default(bool); } }
        public double M11 { get { return default(double); } set { } }
        public double M12 { get { return default(double); } set { } }
        public double M21 { get { return default(double); } set { } }
        public double M22 { get { return default(double); } set { } }
        public double OffsetX { get { return default(double); } set { } }
        public double OffsetY { get { return default(double); } set { } }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override bool Equals(object o) { return default(bool); }
        public bool Equals(global::Windows.UI.Xaml.Media.Matrix value) { return default(bool); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(global::Windows.UI.Xaml.Media.Matrix matrix1, global::Windows.UI.Xaml.Media.Matrix matrix2) { return default(bool); }
        public static bool operator !=(global::Windows.UI.Xaml.Media.Matrix matrix1, global::Windows.UI.Xaml.Media.Matrix matrix2) { return default(bool); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        string System.IFormattable.ToString(string format, global::System.IFormatProvider provider) { return default(string); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { return default(string); }
        public string ToString(global::System.IFormatProvider provider) { return default(string); }
        public global::Windows.Foundation.Point Transform(global::Windows.Foundation.Point point) { return default(global::Windows.Foundation.Point); }
    }
}
namespace Windows.UI.Xaml.Media.Animation
{
    [global::System.Security.SecurityCriticalAttribute]
    [global::System.Runtime.InteropServices.StructLayoutAttribute(global::System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct KeyTime
    {
        public global::System.TimeSpan TimeSpan { get { return default(global::System.TimeSpan); } }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override bool Equals(object value) { return default(bool); }
        public bool Equals(global::Windows.UI.Xaml.Media.Animation.KeyTime value) { return default(bool); }
        public static bool Equals(global::Windows.UI.Xaml.Media.Animation.KeyTime keyTime1, global::Windows.UI.Xaml.Media.Animation.KeyTime keyTime2) { return default(bool); }
        public static global::Windows.UI.Xaml.Media.Animation.KeyTime FromTimeSpan(global::System.TimeSpan timeSpan) { return default(global::Windows.UI.Xaml.Media.Animation.KeyTime); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(global::Windows.UI.Xaml.Media.Animation.KeyTime keyTime1, global::Windows.UI.Xaml.Media.Animation.KeyTime keyTime2) { return default(bool); }
        public static implicit operator global::Windows.UI.Xaml.Media.Animation.KeyTime(global::System.TimeSpan timeSpan) { return default(global::Windows.UI.Xaml.Media.Animation.KeyTime); }
        public static bool operator !=(global::Windows.UI.Xaml.Media.Animation.KeyTime keyTime1, global::Windows.UI.Xaml.Media.Animation.KeyTime keyTime2) { return default(bool); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { return default(string); }
    }
    [global::System.Security.SecurityCriticalAttribute]
    [global::System.Runtime.InteropServices.StructLayoutAttribute(global::System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct RepeatBehavior : global::System.IFormattable
    {
        public RepeatBehavior(double count) { throw new global::System.NotImplementedException(); }
        public RepeatBehavior(global::System.TimeSpan duration) { throw new global::System.NotImplementedException(); }
        public double Count { get { return default(double); } set { } }
        public global::System.TimeSpan Duration { get { return default(global::System.TimeSpan); } set { } }
        public static global::Windows.UI.Xaml.Media.Animation.RepeatBehavior Forever { get { return default(global::Windows.UI.Xaml.Media.Animation.RepeatBehavior); } }
        public bool HasCount { get { return default(bool); } }
        public bool HasDuration { get { return default(bool); } }
        public global::Windows.UI.Xaml.Media.Animation.RepeatBehaviorType Type { get { return default(global::Windows.UI.Xaml.Media.Animation.RepeatBehaviorType); } set { } }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override bool Equals(object value) { return default(bool); }
        public bool Equals(global::Windows.UI.Xaml.Media.Animation.RepeatBehavior repeatBehavior) { return default(bool); }
        public static bool Equals(global::Windows.UI.Xaml.Media.Animation.RepeatBehavior repeatBehavior1, global::Windows.UI.Xaml.Media.Animation.RepeatBehavior repeatBehavior2) { return default(bool); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(global::Windows.UI.Xaml.Media.Animation.RepeatBehavior repeatBehavior1, global::Windows.UI.Xaml.Media.Animation.RepeatBehavior repeatBehavior2) { return default(bool); }
        public static bool operator !=(global::Windows.UI.Xaml.Media.Animation.RepeatBehavior repeatBehavior1, global::Windows.UI.Xaml.Media.Animation.RepeatBehavior repeatBehavior2) { return default(bool); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        string System.IFormattable.ToString(string format, global::System.IFormatProvider formatProvider) { return default(string); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { return default(string); }
        public string ToString(global::System.IFormatProvider formatProvider) { return default(string); }
    }
    [global::System.Security.SecurityCriticalAttribute]
    public enum RepeatBehaviorType
    {
        Count = 0,
        Duration = 1,
        Forever = 2,
    }
}
namespace Windows.UI.Xaml.Media.Media3D
{
    [global::System.Security.SecurityCriticalAttribute]
    [global::System.Runtime.InteropServices.StructLayoutAttribute(global::System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Matrix3D : global::System.IFormattable
    {
        public Matrix3D(double m11, double m12, double m13, double m14, double m21, double m22, double m23, double m24, double m31, double m32, double m33, double m34, double offsetX, double offsetY, double offsetZ, double m44) { throw new global::System.NotImplementedException(); }
        public bool HasInverse { get { return default(bool); } }
        public static global::Windows.UI.Xaml.Media.Media3D.Matrix3D Identity { get { return default(global::Windows.UI.Xaml.Media.Media3D.Matrix3D); } }
        public bool IsIdentity { get { return default(bool); } }
        public double M11 { get { return default(double); } set { } }
        public double M12 { get { return default(double); } set { } }
        public double M13 { get { return default(double); } set { } }
        public double M14 { get { return default(double); } set { } }
        public double M21 { get { return default(double); } set { } }
        public double M22 { get { return default(double); } set { } }
        public double M23 { get { return default(double); } set { } }
        public double M24 { get { return default(double); } set { } }
        public double M31 { get { return default(double); } set { } }
        public double M32 { get { return default(double); } set { } }
        public double M33 { get { return default(double); } set { } }
        public double M34 { get { return default(double); } set { } }
        public double M44 { get { return default(double); } set { } }
        public double OffsetX { get { return default(double); } set { } }
        public double OffsetY { get { return default(double); } set { } }
        public double OffsetZ { get { return default(double); } set { } }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override bool Equals(object o) { return default(bool); }
        public bool Equals(global::Windows.UI.Xaml.Media.Media3D.Matrix3D value) { return default(bool); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override int GetHashCode() { return default(int); }
        public void Invert() { }
        public static bool operator ==(global::Windows.UI.Xaml.Media.Media3D.Matrix3D matrix1, global::Windows.UI.Xaml.Media.Media3D.Matrix3D matrix2) { return default(bool); }
        public static bool operator !=(global::Windows.UI.Xaml.Media.Media3D.Matrix3D matrix1, global::Windows.UI.Xaml.Media.Media3D.Matrix3D matrix2) { return default(bool); }
        public static global::Windows.UI.Xaml.Media.Media3D.Matrix3D operator *(global::Windows.UI.Xaml.Media.Media3D.Matrix3D matrix1, global::Windows.UI.Xaml.Media.Media3D.Matrix3D matrix2) { return default(global::Windows.UI.Xaml.Media.Media3D.Matrix3D); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        string System.IFormattable.ToString(string format, global::System.IFormatProvider provider) { return default(string); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { return default(string); }
        public string ToString(global::System.IFormatProvider provider) { return default(string); }
    }
}
