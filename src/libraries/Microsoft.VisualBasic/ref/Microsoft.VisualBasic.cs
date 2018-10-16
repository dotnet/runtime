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
    public sealed partial class Collection : System.Collections.ICollection, System.Collections.IList
    {
        public Collection() { }
        public int Count { get { throw null; } }
        int System.Collections.ICollection.Count { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public object this[int Index] { get { throw null; } }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(2))]
        public object this[object Index] { get { throw null; } }
        public object this[string Key] { get { throw null; } }
        public void Add(object Item, string Key = null, object Before = null, object After = null) { }
        public void Clear() { }
        public bool Contains(string Key) { throw null; }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
        void System.Collections.IList.RemoveAt(int index) { }
        public void Remove(int Index) { }
        public void Remove(string Key) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), Inherited=false, AllowMultiple=false)]
    public sealed partial class ComClassAttribute : System.Attribute
    {
        public ComClassAttribute() { }
        public ComClassAttribute(string _ClassID) { }
        public ComClassAttribute(string _ClassID, string _InterfaceID) { }
        public ComClassAttribute(string _ClassID, string _InterfaceID, string _EventId) { }
        public string ClassID { get { throw null; } }
        public string EventID { get { throw null; } }
        public string InterfaceID { get { throw null; } }
        public bool InterfaceShadows { get { throw null; } set { } }
    }
    public enum CompareMethod
    {
        Binary = 0,
        Text = 1,
    }
    [Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute]
    public sealed partial class Constants
    {
        internal Constants() { }
        public const string vbBack = "\b";
        public const Microsoft.VisualBasic.CompareMethod vbBinaryCompare = Microsoft.VisualBasic.CompareMethod.Binary;
        public const string vbCr = "\r";
        public const string vbCrLf = "\r\n";
        public const string vbFormFeed = "\f";
        public const string vbLf = "\n";
        [System.ObsoleteAttribute("For a carriage return and line feed, use vbCrLf.  For the current platform's newline, use System.Environment.NewLine.")]
        public const string vbNewLine = "\r\n";
        public const string vbNullChar = "\0";
        public const string vbNullString = null;
        public const string vbTab = "\t";
        public const Microsoft.VisualBasic.CompareMethod vbTextCompare = Microsoft.VisualBasic.CompareMethod.Text;
        public const string vbVerticalTab = "\v";
    }
    public sealed partial class ControlChars
    {
        public const char Back = '\b';
        public const char Cr = '\r';
        public const string CrLf = "\r\n";
        public const char FormFeed = '\f';
        public const char Lf = '\n';
        public const string NewLine = "\r\n";
        public const char NullChar = '\0';
        public const char Quote = '"';
        public const char Tab = '\t';
        public const char VerticalTab = '\v';
        public ControlChars() { }
    }
    [Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute]
    public sealed partial class DateAndTime
    {
        internal DateAndTime() { }
        public static System.DateTime Now { get { throw null; } }
        public static System.DateTime Today { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), AllowMultiple=false, Inherited=false)]
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class HideModuleNameAttribute : System.Attribute
    {
        public HideModuleNameAttribute() { }
    }
    [Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute]
    public sealed partial class Information
    {
        internal Information() { }
        public static bool IsArray(object VarName) { throw null; }
        public static bool IsDate(object Expression) { throw null; }
        public static bool IsDBNull(object Expression) { throw null; }
        public static bool IsError(object Expression) { throw null; }
        public static bool IsNothing(object Expression) { throw null; }
        public static bool IsNumeric(object Expression) { throw null; }
        public static bool IsReference(object Expression) { throw null; }
        public static int LBound(System.Array Array, int Rank = 1) { throw null; }
        public static int QBColor(int Color) { throw null; }
        public static int RGB(int Red, int Green, int Blue) { throw null; }
        public static string SystemTypeName(string VbName) { throw null; }
        public static int UBound(System.Array Array, int Rank = 1) { throw null; }
        public static Microsoft.VisualBasic.VariantType VarType(object VarName) { throw null; }
        public static string VbTypeName(string UrtName) { throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), AllowMultiple=false, Inherited=false)]
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(2))]
    public sealed partial class MyGroupCollectionAttribute : System.Attribute
    {
        public MyGroupCollectionAttribute(string typeToCollect, string createInstanceMethodName, string disposeInstanceMethodName, string defaultInstanceAlias) { }
        public string CreateMethod { get { throw null; } }
        public string DefaultInstanceAlias { get { throw null; } }
        public string DisposeMethod { get { throw null; } }
        public string MyGroupName { get { throw null; } }
    }
    [Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute]
    public sealed partial class Strings
    {
        internal Strings() { }
        public static int Asc(char String) { throw null; }
        public static int Asc(string String) { throw null; }
        public static int AscW(char String) { throw null; }
        public static int AscW(string String) { throw null; }
        public static char Chr(int CharCode) { throw null; }
        public static char ChrW(int CharCode) { throw null; }
        public static string[] Filter(object[] Source, string Match, bool Include = true, [Microsoft.VisualBasic.CompilerServices.OptionCompareAttribute]Microsoft.VisualBasic.CompareMethod Compare = (Microsoft.VisualBasic.CompareMethod)(0)) { throw null; }
        public static string[] Filter(string[] Source, string Match, bool Include = true, [Microsoft.VisualBasic.CompilerServices.OptionCompareAttribute]Microsoft.VisualBasic.CompareMethod Compare = (Microsoft.VisualBasic.CompareMethod)(0)) { throw null; }
        public static int InStr(string String1, string String2, [Microsoft.VisualBasic.CompilerServices.OptionCompareAttribute] CompareMethod Compare = CompareMethod.Binary) { throw null; }
        public static int InStr(int StartPos, string String1, string String2, [Microsoft.VisualBasic.CompilerServices.OptionCompareAttribute] CompareMethod Compare = CompareMethod.Binary) { throw null; }
        public static int InStrRev(string StringCheck, string StringMatch, int Start = -1, [Microsoft.VisualBasic.CompilerServices.OptionCompareAttribute] CompareMethod Compare = CompareMethod.Binary) { throw null; }
        public static int Len(bool Expression) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static int Len(sbyte Expression) { throw null; }
        public static int Len(byte Expression) { throw null; }
        public static int Len(short Expression) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static int Len(ushort Expression) { throw null; }
        public static int Len(int Expression) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static int Len(uint Expression) { throw null; }
        public static int Len(long Expression) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static int Len(ulong Expression) { throw null; }
        public static int Len(decimal Expression) { throw null; }
        public static int Len(float Expression) { throw null; }
        public static int Len(double Expression) { throw null; }
        public static int Len(System.DateTime Expression) { throw null; }
        public static int Len(char Expression) { throw null; }
        public static int Len(string Expression) { throw null; }
        public static int Len(object Expression) { throw null; }
        public static string Left(string str, int Length) { throw null; }
        public static string LTrim(string str) { throw null; }
        public static string Mid(string str, int Start) { throw null; }
        public static string Mid(string str, int Start, int Length) { throw null; }
        public static string Right(string str, int Length) { throw null; }
        public static string RTrim(string str) { throw null; }
        public static string Trim(string str) { throw null; }
    }
    public enum VariantType
    {
        Array = 8192,
        Boolean = 11,
        Byte = 17,
        Char = 18,
        Currency = 6,
        DataObject = 13,
        Date = 7,
        Decimal = 14,
        Double = 5,
        Empty = 0,
        Error = 10,
        Integer = 3,
        Long = 20,
        Null = 1,
        Object = 9,
        Short = 2,
        Single = 4,
        String = 8,
        UserDefinedType = 36,
        Variant = 12,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(256), Inherited=false, AllowMultiple=false)]
    public sealed partial class VBFixedArrayAttribute : System.Attribute
    {
        public VBFixedArrayAttribute(int UpperBound1) { }
        public VBFixedArrayAttribute(int UpperBound1, int UpperBound2) { }
        public int[] Bounds { get { throw null; } }
        public int Length { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(256), Inherited=false, AllowMultiple=false)]
    public sealed partial class VBFixedStringAttribute : System.Attribute
    {
        public VBFixedStringAttribute(int Length) { }
        public int Length { get { throw null; } }
    }
    [Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute]
    public sealed partial class VBMath
    {
        internal VBMath() { }
        public static void Randomize() { }
        public static void Randomize(double Number) { }
        public static float Rnd() { throw null; }
        public static float Rnd(float Number) { throw null; }
    }
}
namespace Microsoft.VisualBasic.ApplicationServices
{
    public partial class StartupEventArgs : System.ComponentModel.CancelEventArgs
    {
        public StartupEventArgs(System.Collections.ObjectModel.ReadOnlyCollection<string> args) { }
        public System.Collections.ObjectModel.ReadOnlyCollection<string> CommandLine { get { throw null; } }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(2))]
    public partial class StartupNextInstanceEventArgs : System.EventArgs
    {
        public StartupNextInstanceEventArgs(System.Collections.ObjectModel.ReadOnlyCollection<string> args, bool bringToForegroundFlag) { }
        public bool BringToForeground { get { throw null; } set { } }
        public System.Collections.ObjectModel.ReadOnlyCollection<string> CommandLine { get { throw null; } }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(2))]
    public partial class UnhandledExceptionEventArgs : System.Threading.ThreadExceptionEventArgs
    {
        public UnhandledExceptionEventArgs(bool exitApplication, System.Exception exception) : base (default(System.Exception)) { }
        public bool ExitApplication { get { throw null; } set { } }
    }
}
namespace Microsoft.VisualBasic.CompilerServices
{
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class Conversions
    {
        internal Conversions() { }
        public static object ChangeType(object Expression, System.Type TargetType) { throw null; }
        [System.ObsoleteAttribute("do not use this method", true)]
        public static object FallbackUserDefinedConversion(object Expression, System.Type TargetType) { throw null; }
        public static string FromCharAndCount(char Value, int Count) { throw null; }
        public static string FromCharArray(char[] Value) { throw null; }
        public static string FromCharArraySubset(char[] Value, int StartIndex, int Length) { throw null; }
        public static bool ToBoolean(object Value) { throw null; }
        public static bool ToBoolean(string Value) { throw null; }
        public static byte ToByte(object Value) { throw null; }
        public static byte ToByte(string Value) { throw null; }
        public static char ToChar(object Value) { throw null; }
        public static char ToChar(string Value) { throw null; }
        public static char[] ToCharArrayRankOne(object Value) { throw null; }
        public static char[] ToCharArrayRankOne(string Value) { throw null; }
        public static System.DateTime ToDate(object Value) { throw null; }
        public static System.DateTime ToDate(string Value) { throw null; }
        public static decimal ToDecimal(bool Value) { throw null; }
        public static decimal ToDecimal(object Value) { throw null; }
        public static decimal ToDecimal(string Value) { throw null; }
        public static double ToDouble(object Value) { throw null; }
        public static double ToDouble(string Value) { throw null; }
        public static T ToGenericParameter<T>(object Value) { throw null; }
        public static int ToInteger(object Value) { throw null; }
        public static int ToInteger(string Value) { throw null; }
        public static long ToLong(object Value) { throw null; }
        public static long ToLong(string Value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(object Value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ToSByte(string Value) { throw null; }
        public static short ToShort(object Value) { throw null; }
        public static short ToShort(string Value) { throw null; }
        public static float ToSingle(object Value) { throw null; }
        public static float ToSingle(string Value) { throw null; }
        public static string ToString(bool Value) { throw null; }
        public static string ToString(byte Value) { throw null; }
        public static string ToString(char Value) { throw null; }
        public static string ToString(System.DateTime Value) { throw null; }
        public static string ToString(decimal Value) { throw null; }
        public static string ToString(decimal Value, System.Globalization.NumberFormatInfo NumberFormat) { throw null; }
        public static string ToString(double Value) { throw null; }
        public static string ToString(double Value, System.Globalization.NumberFormatInfo NumberFormat) { throw null; }
        public static string ToString(short Value) { throw null; }
        public static string ToString(int Value) { throw null; }
        public static string ToString(long Value) { throw null; }
        public static string ToString(object Value) { throw null; }
        public static string ToString(float Value) { throw null; }
        public static string ToString(float Value, System.Globalization.NumberFormatInfo NumberFormat) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(uint Value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static string ToString(ulong Value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInteger(object Value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ToUInteger(string Value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToULong(object Value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ToULong(string Value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUShort(object Value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ToUShort(string Value) { throw null; }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class BooleanType
    {
        internal BooleanType() { }
        public static System.Boolean FromObject(object Value) { throw null; }
        public static System.Boolean FromString(string Value) { throw null; }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class DecimalType
    {
        internal DecimalType() { }
        public static decimal FromBoolean(bool Value) { throw null; }
        public static decimal FromObject(object Value) { throw null; }
        public static decimal FromObject(object Value, System.Globalization.NumberFormatInfo NumberFormat) { throw null; }
        public static decimal FromString(string Value) { throw null; }
        public static decimal FromString(string Value, System.Globalization.NumberFormatInfo NumberFormat) { throw null; }
        public static decimal Parse(string Value, System.Globalization.NumberFormatInfo NumberFormat) { throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), AllowMultiple=false, Inherited=false)]
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class DesignerGeneratedAttribute : System.Attribute
    {
        public DesignerGeneratedAttribute() { }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class DoubleType
    {
        internal DoubleType() { }
        public static double FromObject(object Value) { throw null; }
        public static double FromObject(object Value, System.Globalization.NumberFormatInfo NumberFormat) { throw null; }
        public static double FromString(string Value) { throw null; }
        public static double FromString(string Value, System.Globalization.NumberFormatInfo NumberFormat) { throw null; }
        public static double Parse(string Value) { throw null; }
        public static double Parse(string Value, System.Globalization.NumberFormatInfo NumberFormat) { throw null; }
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
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static object FallbackCall(object Instance, string MemberName, object[] Arguments, string[] ArgumentNames, bool IgnoreReturn) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static object FallbackGet(object Instance, string MemberName, object[] Arguments, string[] ArgumentNames) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static void FallbackIndexSet(object Instance, object[] Arguments, string[] ArgumentNames) { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static void FallbackIndexSetComplex(object Instance, object[] Arguments, string[] ArgumentNames, bool OptimisticSet, bool RValueBase) { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static object FallbackInvokeDefault1(object Instance, object[] Arguments, string[] ArgumentNames, bool ReportErrors) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static object FallbackInvokeDefault2(object Instance, object[] Arguments, string[] ArgumentNames, bool ReportErrors) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static void FallbackSet(object Instance, string MemberName, object[] Arguments) { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("do not use this method", true)]
        public static void FallbackSetComplex(object Instance, string MemberName, object[] Arguments, bool OptimisticSet, bool RValueBase) { }
        public static object LateCall(object Instance, System.Type Type, string MemberName, object[] Arguments, string[] ArgumentNames, System.Type[] TypeArguments, bool[] CopyBack, bool IgnoreReturn) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static object LateCallInvokeDefault(object Instance, object[] Arguments, string[] ArgumentNames, bool ReportErrors) { throw null; }
        public static object LateGet(object Instance, System.Type Type, string MemberName, object[] Arguments, string[] ArgumentNames, System.Type[] TypeArguments, bool[] CopyBack) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static object LateGetInvokeDefault(object Instance, object[] Arguments, string[] ArgumentNames, bool ReportErrors) { throw null; }
        public static object LateIndexGet(object Instance, object[] Arguments, string[] ArgumentNames) { throw null; }
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
            public static bool ForLoopInitObj(object Counter, object Start, object Limit, object StepValue, ref object LoopForResult, ref object CounterResult) { throw null; }
            public static bool ForNextCheckDec(decimal count, decimal limit, decimal StepValue) { throw null; }
            public static bool ForNextCheckObj(object Counter, object LoopObj, ref object CounterResult) { throw null; }
            public static bool ForNextCheckR4(float count, float limit, float StepValue) { throw null; }
            public static bool ForNextCheckR8(double count, double limit, double StepValue) { throw null; }
        }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class Operators
    {
        internal Operators() { }
        public static object AddObject(object Left, object Right) { throw null; }
        public static object AndObject(object Left, object Right) { throw null; }
        public static object CompareObjectEqual(object Left, object Right, bool TextCompare) { throw null; }
        public static object CompareObjectGreater(object Left, object Right, bool TextCompare) { throw null; }
        public static object CompareObjectGreaterEqual(object Left, object Right, bool TextCompare) { throw null; }
        public static object CompareObjectLess(object Left, object Right, bool TextCompare) { throw null; }
        public static object CompareObjectLessEqual(object Left, object Right, bool TextCompare) { throw null; }
        public static object CompareObjectNotEqual(object Left, object Right, bool TextCompare) { throw null; }
        public static int CompareString(string Left, string Right, bool TextCompare) { throw null; }
        public static object ConcatenateObject(object Left, object Right) { throw null; }
        public static bool ConditionalCompareObjectEqual(object Left, object Right, bool TextCompare) { throw null; }
        public static bool ConditionalCompareObjectGreater(object Left, object Right, bool TextCompare) { throw null; }
        public static bool ConditionalCompareObjectGreaterEqual(object Left, object Right, bool TextCompare) { throw null; }
        public static bool ConditionalCompareObjectLess(object Left, object Right, bool TextCompare) { throw null; }
        public static bool ConditionalCompareObjectLessEqual(object Left, object Right, bool TextCompare) { throw null; }
        public static bool ConditionalCompareObjectNotEqual(object Left, object Right, bool TextCompare) { throw null; }
        public static object DivideObject(object Left, object Right) { throw null; }
        public static object ExponentObject(object Left, object Right) { throw null; }
        [System.ObsoleteAttribute("do not use this method", true)]
        public static object FallbackInvokeUserDefinedOperator(object vbOp, object[] arguments) { throw null; }
        public static object IntDivideObject(object Left, object Right) { throw null; }
        public static object LeftShiftObject(object Operand, object Amount) { throw null; }
        public static object ModObject(object Left, object Right) { throw null; }
        public static object MultiplyObject(object Left, object Right) { throw null; }
        public static object NegateObject(object Operand) { throw null; }
        public static object NotObject(object Operand) { throw null; }
        public static object OrObject(object Left, object Right) { throw null; }
        public static object PlusObject(object Operand) { throw null; }
        public static object RightShiftObject(object Operand, object Amount) { throw null; }
        public static object SubtractObject(object Left, object Right) { throw null; }
        public static object XorObject(object Left, object Right) { throw null; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(2048), Inherited=false, AllowMultiple=false)]
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class OptionCompareAttribute : System.Attribute
    {
        public OptionCompareAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), Inherited=false, AllowMultiple=false)]
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class OptionTextAttribute : System.Attribute
    {
        public OptionTextAttribute() { }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class ProjectData
    {
        internal ProjectData() { }
        public static void ClearProjectError() { }
        public static void SetProjectError(System.Exception ex) { }
        public static void SetProjectError(System.Exception ex, int lErl) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(4), Inherited=false, AllowMultiple=false)]
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
        public static System.Array CopyArray(System.Array arySrc, System.Array aryDest) { throw null; }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public sealed partial class Versioned
    {
        internal Versioned() { }
        public static bool IsNumeric(object Expression) { throw null; }
        public static string SystemTypeName(string VbName) { throw null; }
        public static string VbTypeName(string SystemName) { throw null; }
    }
}
namespace Microsoft.VisualBasic.Devices
{
    public partial class NetworkAvailableEventArgs : System.EventArgs
    {
        public NetworkAvailableEventArgs(bool networkAvailable) { }
        public bool IsNetworkAvailable { get { throw null; } }
    }
}
