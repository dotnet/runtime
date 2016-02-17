// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace Microsoft.VisualBasic
{
    public enum CallType
    {
        Get = 2,
        Let = 4,
        Method = 1,
        Set = 8,
    }
    [Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute]
    public sealed partial class Constants
    {
        internal Constants() { }
        public const string vbBack = "\b";
        public const string vbCr = "\r";
        public const string vbCrLf = "\r\n";
        public const string vbFormFeed = "\f";
        public const string vbLf = "\n";
        [System.Obsolete("For a carriage return and line feed, use vbCrLf.  For the current platform's newline, use System.Environment.NewLine.")]
        public const string vbNewLine = "\r\n";
        public const string vbNullChar = "\0";
        public const string vbNullString = null;
        public const string vbTab = "\t";
        public const string vbVerticalTab = "\v";
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), AllowMultiple = false, Inherited = false)]
    public sealed partial class HideModuleNameAttribute : System.Attribute
    {
        public HideModuleNameAttribute() { }
    }
    [Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute]
    public sealed partial class Strings
    {
        internal Strings() { }
        public static int AscW(char String) { return default(int); }
        public static int AscW(string String) { return default(int); }
        public static char ChrW(int CharCode) { return default(char); }
    }
}
namespace Microsoft.VisualBasic.CompilerServices
{
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class Conversions
    {
        internal Conversions() { }
        public static object ChangeType(object Expression, System.Type TargetType) { return default(object); }
        public static bool ToBoolean(object Value) { return default(bool); }
        public static bool ToBoolean(string Value) { return default(bool); }
        public static byte ToByte(object Value) { return default(byte); }
        public static byte ToByte(string Value) { return default(byte); }
        public static char ToChar(object Value) { return default(char); }
        public static char ToChar(string Value) { return default(char); }
        public static char[] ToCharArrayRankOne(object Value) { return default(char[]); }
        public static char[] ToCharArrayRankOne(string Value) { return default(char[]); }
        public static System.DateTime ToDate(object Value) { return default(System.DateTime); }
        public static System.DateTime ToDate(string Value) { return default(System.DateTime); }
        public static decimal ToDecimal(bool Value) { return default(decimal); }
        public static decimal ToDecimal(object Value) { return default(decimal); }
        public static decimal ToDecimal(string Value) { return default(decimal); }
        public static double ToDouble(object Value) { return default(double); }
        public static double ToDouble(string Value) { return default(double); }
        public static T ToGenericParameter<T>(object Value) { return default(T); }
        public static int ToInteger(object Value) { return default(int); }
        public static int ToInteger(string Value) { return default(int); }
        public static long ToLong(object Value) { return default(long); }
        public static long ToLong(string Value) { return default(long); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(object Value) { return default(sbyte); }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(string Value) { return default(sbyte); }
        public static short ToShort(object Value) { return default(short); }
        public static short ToShort(string Value) { return default(short); }
        public static float ToSingle(object Value) { return default(float); }
        public static float ToSingle(string Value) { return default(float); }
        public static string ToString(bool Value) { return default(string); }
        public static string ToString(byte Value) { return default(string); }
        public static string ToString(char Value) { return default(string); }
        public static string ToString(System.DateTime Value) { return default(string); }
        public static string ToString(decimal Value) { return default(string); }
        public static string ToString(double Value) { return default(string); }
        public static string ToString(short Value) { return default(string); }
        public static string ToString(int Value) { return default(string); }
        public static string ToString(long Value) { return default(string); }
        public static string ToString(object Value) { return default(string); }
        public static string ToString(float Value) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(uint Value) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(ulong Value) { return default(string); }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInteger(object Value) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInteger(string Value) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToULong(object Value) { return default(ulong); }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToULong(string Value) { return default(ulong); }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUShort(object Value) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUShort(string Value) { return default(ushort); }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), AllowMultiple = false, Inherited = false)]
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class DesignerGeneratedAttribute : System.Attribute
    {
        public DesignerGeneratedAttribute() { }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class IncompleteInitialization : System.Exception
    {
        public IncompleteInitialization() { }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class NewLateBinding
    {
        internal NewLateBinding() { }
        public static object LateCall(object Instance, System.Type Type, string MemberName, object[] Arguments, string[] ArgumentNames, System.Type[] TypeArguments, bool[] CopyBack, bool IgnoreReturn) { return default(object); }
        public static object LateGet(object Instance, System.Type Type, string MemberName, object[] Arguments, string[] ArgumentNames, System.Type[] TypeArguments, bool[] CopyBack) { return default(object); }
        public static object LateIndexGet(object Instance, object[] Arguments, string[] ArgumentNames) { return default(object); }
        public static void LateIndexSet(object Instance, object[] Arguments, string[] ArgumentNames) { }
        public static void LateIndexSetComplex(object Instance, object[] Arguments, string[] ArgumentNames, bool OptimisticSet, bool RValueBase) { }
        public static void LateSet(object Instance, System.Type Type, string MemberName, object[] Arguments, string[] ArgumentNames, System.Type[] TypeArguments) { }
        public static void LateSet(object Instance, System.Type Type, string MemberName, object[] Arguments, string[] ArgumentNames, System.Type[] TypeArguments, bool OptimisticSet, bool RValueBase, Microsoft.VisualBasic.CallType CallType) { }
        public static void LateSetComplex(object Instance, System.Type Type, string MemberName, object[] Arguments, string[] ArgumentNames, System.Type[] TypeArguments, bool OptimisticSet, bool RValueBase) { }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class ObjectFlowControl
    {
        internal ObjectFlowControl() { }
        public static void CheckForSyncLockOnValueType(object Expression) { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public sealed partial class ForLoopControl
        {
            internal ForLoopControl() { }
            public static bool ForLoopInitObj(object Counter, object Start, object Limit, object StepValue, ref object LoopForResult, ref object CounterResult) { return default(bool); }
            public static bool ForNextCheckDec(decimal count, decimal limit, decimal StepValue) { return default(bool); }
            public static bool ForNextCheckObj(object Counter, object LoopObj, ref object CounterResult) { return default(bool); }
            public static bool ForNextCheckR4(float count, float limit, float StepValue) { return default(bool); }
            public static bool ForNextCheckR8(double count, double limit, double StepValue) { return default(bool); }
        }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class Operators
    {
        internal Operators() { }
        public static object AddObject(object Left, object Right) { return default(object); }
        public static object AndObject(object Left, object Right) { return default(object); }
        public static object CompareObjectEqual(object Left, object Right, bool TextCompare) { return default(object); }
        public static object CompareObjectGreater(object Left, object Right, bool TextCompare) { return default(object); }
        public static object CompareObjectGreaterEqual(object Left, object Right, bool TextCompare) { return default(object); }
        public static object CompareObjectLess(object Left, object Right, bool TextCompare) { return default(object); }
        public static object CompareObjectLessEqual(object Left, object Right, bool TextCompare) { return default(object); }
        public static object CompareObjectNotEqual(object Left, object Right, bool TextCompare) { return default(object); }
        public static int CompareString(string Left, string Right, bool TextCompare) { return default(int); }
        public static object ConcatenateObject(object Left, object Right) { return default(object); }
        public static bool ConditionalCompareObjectEqual(object Left, object Right, bool TextCompare) { return default(bool); }
        public static bool ConditionalCompareObjectGreater(object Left, object Right, bool TextCompare) { return default(bool); }
        public static bool ConditionalCompareObjectGreaterEqual(object Left, object Right, bool TextCompare) { return default(bool); }
        public static bool ConditionalCompareObjectLess(object Left, object Right, bool TextCompare) { return default(bool); }
        public static bool ConditionalCompareObjectLessEqual(object Left, object Right, bool TextCompare) { return default(bool); }
        public static bool ConditionalCompareObjectNotEqual(object Left, object Right, bool TextCompare) { return default(bool); }
        public static object DivideObject(object Left, object Right) { return default(object); }
        public static object ExponentObject(object Left, object Right) { return default(object); }
        public static object IntDivideObject(object Left, object Right) { return default(object); }
        public static object LeftShiftObject(object Operand, object Amount) { return default(object); }
        public static object ModObject(object Left, object Right) { return default(object); }
        public static object MultiplyObject(object Left, object Right) { return default(object); }
        public static object NegateObject(object Operand) { return default(object); }
        public static object NotObject(object Operand) { return default(object); }
        public static object OrObject(object Left, object Right) { return default(object); }
        public static object PlusObject(object Operand) { return default(object); }
        public static object RightShiftObject(object Operand, object Amount) { return default(object); }
        public static object SubtractObject(object Left, object Right) { return default(object); }
        public static object XorObject(object Left, object Right) { return default(object); }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2048), Inherited = false, AllowMultiple = false)]
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class OptionCompareAttribute : System.Attribute
    {
        public OptionCompareAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), Inherited = false, AllowMultiple = false)]
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class OptionTextAttribute : System.Attribute
    {
        public OptionTextAttribute() { }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class ProjectData
    {
        internal ProjectData() { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void ClearProjectError() { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void SetProjectError(System.Exception ex) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public static void SetProjectError(System.Exception ex, int lErl) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), Inherited = false, AllowMultiple = false)]
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class StandardModuleAttribute : System.Attribute
    {
        public StandardModuleAttribute() { }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class StaticLocalInitFlag
    {
        public short State;
        public StaticLocalInitFlag() { }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class Utils
    {
        internal Utils() { }
        public static System.Array CopyArray(System.Array arySrc, System.Array aryDest) { return default(System.Array); }
    }
}
