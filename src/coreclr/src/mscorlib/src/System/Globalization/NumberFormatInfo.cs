// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Globalization {
    using System.Security.Permissions;
    using System.Runtime.Serialization;
    using System.Text;
    using System;
    using System.Diagnostics.Contracts;
    //
    // Property             Default Description
    // PositiveSign           '+'   Character used to indicate positive values.
    // NegativeSign           '-'   Character used to indicate negative values.
    // NumberDecimalSeparator '.'   The character used as the decimal separator.
    // NumberGroupSeparator   ','   The character used to separate groups of
    //                              digits to the left of the decimal point.
    // NumberDecimalDigits    2     The default number of decimal places.
    // NumberGroupSizes       3     The number of digits in each group to the
    //                              left of the decimal point.
    // NaNSymbol             "NaN"  The string used to represent NaN values.
    // PositiveInfinitySymbol"Infinity" The string used to represent positive
    //                              infinities.
    // NegativeInfinitySymbol"-Infinity" The string used to represent negative
    //                              infinities.
    //
    //
    //
    // Property                  Default  Description
    // CurrencyDecimalSeparator  '.'      The character used as the decimal
    //                                    separator.
    // CurrencyGroupSeparator    ','      The character used to separate groups
    //                                    of digits to the left of the decimal
    //                                    point.
    // CurrencyDecimalDigits     2        The default number of decimal places.
    // CurrencyGroupSizes        3        The number of digits in each group to
    //                                    the left of the decimal point.
    // CurrencyPositivePattern   0        The format of positive values.
    // CurrencyNegativePattern   0        The format of negative values.
    // CurrencySymbol            "$"      String used as local monetary symbol.
    //

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    sealed public class NumberFormatInfo : ICloneable, IFormatProvider
    {
        // invariantInfo is constant irrespective of your current culture.
        private static volatile NumberFormatInfo invariantInfo;

        // READTHIS READTHIS READTHIS
        // This class has an exact mapping onto a native structure defined in COMNumber.cpp
        // DO NOT UPDATE THIS WITHOUT UPDATING THAT STRUCTURE. IF YOU ADD BOOL, ADD THEM AT THE END.
        // ALSO MAKE SURE TO UPDATE mscorlib.h in the VM directory to check field offsets.
        // READTHIS READTHIS READTHIS
        internal int[] numberGroupSizes = new int[] {3};
        internal int[] currencyGroupSizes = new int[] {3};
        internal int[] percentGroupSizes = new int[] {3};
        internal String positiveSign = "+";
        internal String negativeSign = "-";
        internal String numberDecimalSeparator = ".";
        internal String numberGroupSeparator = ",";
        internal String currencyGroupSeparator = ",";
        internal String currencyDecimalSeparator = ".";
        internal String currencySymbol = "\x00a4";  // U+00a4 is the symbol for International Monetary Fund.
        // The alternative currency symbol used in Win9x ANSI codepage, that can not roundtrip between ANSI and Unicode.
        // Currently, only ja-JP and ko-KR has non-null values (which is U+005c, backslash)
        // NOTE: The only legal values for this string are null and "\x005c"
        internal String ansiCurrencySymbol = null;
        internal String nanSymbol = "NaN";
        internal String positiveInfinitySymbol = "Infinity";
        internal String negativeInfinitySymbol = "-Infinity";
        internal String percentDecimalSeparator = ".";
        internal String percentGroupSeparator = ",";
        internal String percentSymbol = "%";
        internal String perMilleSymbol = "\u2030";

        [OptionalField(VersionAdded = 2)]
        internal String[] nativeDigits = {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9"};

        // an index which points to a record in Culture Data Table.
        // We shouldn't be persisting dataItem (since its useless & we weren't using it),
        // but since COMNumber.cpp uses it and since serialization isn't implimented, its stuck for now.
        [OptionalField(VersionAdded = 1)]
        internal int m_dataItem = 0;    // NEVER USED, DO NOT USE THIS! (Serialized in Everett)

        internal int numberDecimalDigits = 2;
        internal int currencyDecimalDigits = 2;
        internal int currencyPositivePattern = 0;
        internal int currencyNegativePattern = 0;
        internal int numberNegativePattern = 1;
        internal int percentPositivePattern = 0;
        internal int percentNegativePattern = 0;
        internal int percentDecimalDigits = 2;

#if !FEATURE_CORECLR
        [OptionalField(VersionAdded = 2)]
        internal int digitSubstitution = 1; // DigitShapes.None
#endif // !FEATURE_CORECLR        

        internal bool isReadOnly=false;
        // We shouldn't be persisting m_useUserOverride (since its useless & we weren't using it),
        // but since COMNumber.cpp uses it and since serialization isn't implimented, its stuck for now.
        [OptionalField(VersionAdded = 1)]
        internal bool m_useUserOverride=false;    // NEVER USED, DO NOT USE THIS! (Serialized in Everett)

        // Is this NumberFormatInfo for invariant culture?
        [OptionalField(VersionAdded = 2)]
        internal bool m_isInvariant=false;

        public NumberFormatInfo() : this(null) {
        }

#region Serialization
#if !FEATURE_CORECLR
        // Check if NumberFormatInfo was not set up ambiguously for parsing as number and currency
        // eg. if the NumberDecimalSeparator and the NumberGroupSeparator were the same. This check
        // is solely for backwards compatibility / version tolerant serialization
        [OptionalField(VersionAdded = 1)]
        internal bool validForParseAsNumber = true;     // NEVER USED, DO NOT USE THIS! (Serialized in Whidbey/Everett)
        [OptionalField(VersionAdded = 1)]
        internal bool validForParseAsCurrency = true;   // NEVER USED, DO NOT USE THIS! (Serialized in Whidbey/Everett)
#endif // !FEATURE_CORECLR        

        [OnSerializing]
        private void OnSerializing(StreamingContext ctx)
        {
#if !FEATURE_CORECLR
            // Update these legacy flags, so that 1.1/2.0 versions of the framework
            // can still throw while parsing; even when using a de-serialized
            // NumberFormatInfo from a 4.0+ version of the framework
            if (numberDecimalSeparator != numberGroupSeparator) {
                validForParseAsNumber = true;
            } else {
                validForParseAsNumber = false;
            }

            if ((numberDecimalSeparator != numberGroupSeparator) &&
                (numberDecimalSeparator != currencyGroupSeparator) &&
                (currencyDecimalSeparator != numberGroupSeparator) &&
                (currencyDecimalSeparator != currencyGroupSeparator)) {
                validForParseAsCurrency = true;
            } else {
                validForParseAsCurrency = false;
            }
#endif // !FEATURE_CORECLR
        }


        [OnDeserializing]
        private void OnDeserializing(StreamingContext ctx)
        {
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
        }
#endregion Serialization


        static private void VerifyDecimalSeparator(String decSep, String propertyName) {
            if (decSep==null) {
                throw new ArgumentNullException(propertyName,
                        Environment.GetResourceString("ArgumentNull_String"));
            }

            if (decSep.Length==0) {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyDecString"));
            }
            Contract.EndContractBlock();

        }

        static private void VerifyGroupSeparator(String groupSep, String propertyName) {
            if (groupSep==null) {
                throw new ArgumentNullException(propertyName,
                        Environment.GetResourceString("ArgumentNull_String"));
            }
            Contract.EndContractBlock();
        }

        static private void VerifyNativeDigits(String [] nativeDig, String propertyName) {
            if (nativeDig==null) {
                throw new ArgumentNullException(propertyName,
                        Environment.GetResourceString("ArgumentNull_Array"));
            }

            if (nativeDig.Length != 10)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidNativeDigitCount"), propertyName);
            }
            Contract.EndContractBlock();

            for(int i = 0; i < nativeDig.Length; i++)
            {
                if (nativeDig[i] == null)
                {
                    throw new ArgumentNullException(propertyName,
                            Environment.GetResourceString("ArgumentNull_ArrayValue"));
                }

            
                if (nativeDig[i].Length != 1) {
                    if(nativeDig[i].Length != 2) {
                        // Not 1 or 2 UTF-16 code points
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidNativeDigitValue"), propertyName);
                    } else if(!char.IsSurrogatePair(nativeDig[i][0], nativeDig[i][1])) {
                        // 2 UTF-6 code points, but not a surrogate pair
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidNativeDigitValue"), propertyName);
                    }
                }

                if (CharUnicodeInfo.GetDecimalDigitValue(nativeDig[i], 0) != i &&
                    CharUnicodeInfo.GetUnicodeCategory(nativeDig[i], 0) != UnicodeCategory.PrivateUse) {
                    // Not the appropriate digit according to the Unicode data properties
                    // (Digit 0 must be a 0, etc.).
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidNativeDigitValue"), propertyName);
                }
            }
        }

#if !FEATURE_CORECLR
         static private void VerifyDigitSubstitution(DigitShapes digitSub, String propertyName) {
            switch(digitSub)
            {
                case DigitShapes.Context:
                case DigitShapes.None:
                case DigitShapes.NativeNational:
                    // Success.
                    break;

                default:
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDigitSubstitution"), propertyName);
            }
        }
#endif // !FEATURE_CORECLR

        // We aren't persisting dataItem any more (since its useless & we weren't using it),
        // Ditto with m_useUserOverride.  Don't use them, we use a local copy of everything.
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal NumberFormatInfo(CultureData cultureData)
        {
            if (cultureData != null)
            {
                // We directly use fields here since these data is coming from data table or Win32, so we
                // don't need to verify their values (except for invalid parsing situations).
                cultureData.GetNFIValues(this);

                if (cultureData.IsInvariantCulture)
                {
                    // For invariant culture
                    this.m_isInvariant = true;
                }
            }
        }

        [Pure]
        private void VerifyWritable() {
            if (isReadOnly) {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
            }
            Contract.EndContractBlock();
        }

        // Returns a default NumberFormatInfo that will be universally
        // supported and constant irrespective of the current culture.
        // Used by FromString methods.
        //

        public static NumberFormatInfo InvariantInfo {
            get {
                if (invariantInfo == null) {
                    // Lazy create the invariant info. This cannot be done in a .cctor because exceptions can
                    // be thrown out of a .cctor stack that will need this.
                    NumberFormatInfo nfi = new NumberFormatInfo();
                    nfi.m_isInvariant = true;
                    invariantInfo = ReadOnly(nfi);
                }
                return invariantInfo;
            }
        }


        public static NumberFormatInfo GetInstance(IFormatProvider formatProvider) {
            // Fast case for a regular CultureInfo
            NumberFormatInfo info;
            CultureInfo cultureProvider = formatProvider as CultureInfo;
            if (cultureProvider != null && !cultureProvider.m_isInherited) {
                info = cultureProvider.numInfo;
                if (info != null) {
                    return info;
                }
                else {
                    return cultureProvider.NumberFormat;
                }
            }
            // Fast case for an NFI;
            info = formatProvider as NumberFormatInfo;
            if (info != null) {
                return info;
            }
            if (formatProvider != null) {
                info = formatProvider.GetFormat(typeof(NumberFormatInfo)) as NumberFormatInfo;
                if (info != null) {
                    return info;
                }
            }
            return CurrentInfo;
        }



        public Object Clone() {
            NumberFormatInfo n = (NumberFormatInfo)MemberwiseClone();
            n.isReadOnly = false;
            return n;
        }


         public int CurrencyDecimalDigits {
            get { return currencyDecimalDigits; }
            set {
                if (value < 0 || value > 99) {
                    throw new ArgumentOutOfRangeException(
                                "CurrencyDecimalDigits",
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    Environment.GetResourceString("ArgumentOutOfRange_Range"),
                                    0,
                                    99));
                }
                Contract.EndContractBlock();
                VerifyWritable();
                currencyDecimalDigits = value;
            }
        }


         public String CurrencyDecimalSeparator {
            get { return currencyDecimalSeparator; }
            set {
                VerifyWritable();
                VerifyDecimalSeparator(value, "CurrencyDecimalSeparator");
                currencyDecimalSeparator = value;
            }
        }


        public bool IsReadOnly {
            get {
                return isReadOnly;
            }
        }

        //
        // Check the values of the groupSize array.
        //
        // Every element in the groupSize array should be between 1 and 9
        // excpet the last element could be zero.
        //
        static internal void CheckGroupSize(String propName, int[] groupSize)
        {
            for (int i = 0; i < groupSize.Length; i++)
            {
                if (groupSize[i] < 1)
                {
                    if (i == groupSize.Length - 1 && groupSize[i] == 0)
                        return;
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidGroupSize"), propName);
                }
                else if (groupSize[i] > 9)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidGroupSize"), propName);
                }
            }
        }


         public int[] CurrencyGroupSizes {
            get {
                return ((int[])currencyGroupSizes.Clone());
            }
            set {
                if (value == null) {
                    throw new ArgumentNullException("CurrencyGroupSizes",
                        Environment.GetResourceString("ArgumentNull_Obj"));
                }
                Contract.EndContractBlock();
                VerifyWritable();                
                
                Int32[] inputSizes = (Int32[])value.Clone();
                CheckGroupSize("CurrencyGroupSizes", inputSizes);
                currencyGroupSizes = inputSizes;
            }

        }



         public int[] NumberGroupSizes {
            get {
                return ((int[])numberGroupSizes.Clone());
            }
            set {
                if (value == null) {
                    throw new ArgumentNullException("NumberGroupSizes",
                        Environment.GetResourceString("ArgumentNull_Obj"));
                }
                Contract.EndContractBlock();
                VerifyWritable();
                
                Int32[] inputSizes = (Int32[])value.Clone();
                CheckGroupSize("NumberGroupSizes", inputSizes);
                numberGroupSizes = inputSizes;
            }
        }


         public int[] PercentGroupSizes {
            get {
                return ((int[])percentGroupSizes.Clone());
            }
            set {
                if (value == null) {
                    throw new ArgumentNullException("PercentGroupSizes",
                        Environment.GetResourceString("ArgumentNull_Obj"));
                }
                Contract.EndContractBlock();
                VerifyWritable();
                Int32[] inputSizes = (Int32[])value.Clone();
                CheckGroupSize("PercentGroupSizes", inputSizes);
                percentGroupSizes = inputSizes;
            }

        }


         public String CurrencyGroupSeparator {
            get { return currencyGroupSeparator; }
            set {
                VerifyWritable();
                VerifyGroupSeparator(value, "CurrencyGroupSeparator");
                currencyGroupSeparator = value;
            }
        }


         public String CurrencySymbol {
            get { return currencySymbol; }
            set {
                if (value == null) {
                    throw new ArgumentNullException("CurrencySymbol",
                        Environment.GetResourceString("ArgumentNull_String"));
                }
                Contract.EndContractBlock();
                VerifyWritable();
                currencySymbol = value;
            }
        }

        // Returns the current culture's NumberFormatInfo.  Used by Parse methods.
        //

        public static NumberFormatInfo CurrentInfo {
            get {
                System.Globalization.CultureInfo culture = System.Threading.Thread.CurrentThread.CurrentCulture;
                if (!culture.m_isInherited) {
                    NumberFormatInfo info = culture.numInfo;
                    if (info != null) {
                        return info;
                    }
                }
                return ((NumberFormatInfo)culture.GetFormat(typeof(NumberFormatInfo)));
            }
        }


         public String NaNSymbol {
            get {
                return nanSymbol;
            }
            set {
                if (value == null) {
                    throw new ArgumentNullException("NaNSymbol",
                        Environment.GetResourceString("ArgumentNull_String"));
                }
                Contract.EndContractBlock();
                VerifyWritable();
                nanSymbol = value;
            }
        }



         public int CurrencyNegativePattern {
            get { return currencyNegativePattern; }
            set {
                if (value < 0 || value > 15) {
                    throw new ArgumentOutOfRangeException(
                                "CurrencyNegativePattern",
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    Environment.GetResourceString("ArgumentOutOfRange_Range"),
                                    0,
                                    15));
                }
                Contract.EndContractBlock();
                VerifyWritable();
                currencyNegativePattern = value;
            }
        }


         public int NumberNegativePattern {
            get { return numberNegativePattern; }
            set {
                //
                // NOTENOTE: the range of value should correspond to negNumberFormats[] in vm\COMNumber.cpp.
                //
                if (value < 0 || value > 4) {
                    throw new ArgumentOutOfRangeException(
                                "NumberNegativePattern",
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    Environment.GetResourceString("ArgumentOutOfRange_Range"),
                                    0,
                                    4));
                }
                Contract.EndContractBlock();
                VerifyWritable();
                numberNegativePattern = value;
            }
        }


         public int PercentPositivePattern {
            get { return percentPositivePattern; }
            set {
                //
                // NOTENOTE: the range of value should correspond to posPercentFormats[] in vm\COMNumber.cpp.
                //
                if (value < 0 || value > 3) {
                    throw new ArgumentOutOfRangeException(
                                "PercentPositivePattern",
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    Environment.GetResourceString("ArgumentOutOfRange_Range"),
                                    0,
                                    3));
                }
                Contract.EndContractBlock();
                VerifyWritable();
                percentPositivePattern = value;
            }
        }


         public int PercentNegativePattern {
            get { return percentNegativePattern; }
            set {
                //
                // NOTENOTE: the range of value should correspond to posPercentFormats[] in vm\COMNumber.cpp.
                //
                if (value < 0 || value > 11) {
                    throw new ArgumentOutOfRangeException(
                                "PercentNegativePattern",
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    Environment.GetResourceString("ArgumentOutOfRange_Range"),
                                    0,
                                    11));
                }
                Contract.EndContractBlock();
                VerifyWritable();
                percentNegativePattern = value;
            }
        }


         public String NegativeInfinitySymbol {
            get {
                return negativeInfinitySymbol;
            }
            set {
                if (value == null) {
                    throw new ArgumentNullException("NegativeInfinitySymbol",
                        Environment.GetResourceString("ArgumentNull_String"));
                }
                Contract.EndContractBlock();
                VerifyWritable();
                negativeInfinitySymbol = value;
            }
        }


         public String NegativeSign {
            get { return negativeSign; }
            set {
                if (value == null) {
                    throw new ArgumentNullException("NegativeSign",
                        Environment.GetResourceString("ArgumentNull_String"));
                }
                Contract.EndContractBlock();
                VerifyWritable();
                negativeSign = value;
            }
        }


         public int NumberDecimalDigits {
            get { return numberDecimalDigits; }
            set {
                if (value < 0 || value > 99) {
                    throw new ArgumentOutOfRangeException(
                                "NumberDecimalDigits",
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    Environment.GetResourceString("ArgumentOutOfRange_Range"),
                                    0,
                                    99));
                }
                Contract.EndContractBlock();
                VerifyWritable();
                numberDecimalDigits = value;
            }
        }


         public String NumberDecimalSeparator {
            get { return numberDecimalSeparator; }
            set {
                VerifyWritable();
                VerifyDecimalSeparator(value, "NumberDecimalSeparator");
                numberDecimalSeparator = value;
            }
        }


         public String NumberGroupSeparator {
            get { return numberGroupSeparator; }
            set {
                VerifyWritable();
                VerifyGroupSeparator(value, "NumberGroupSeparator");
                numberGroupSeparator = value;
            }
        }


         public int CurrencyPositivePattern {
            get { return currencyPositivePattern; }
            set {
                if (value < 0 || value > 3) {
                    throw new ArgumentOutOfRangeException(
                                "CurrencyPositivePattern",
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    Environment.GetResourceString("ArgumentOutOfRange_Range"),
                                    0,
                                    3));
                }
                Contract.EndContractBlock();
                VerifyWritable();
                currencyPositivePattern = value;
            }
        }


         public String PositiveInfinitySymbol {
            get {
                return positiveInfinitySymbol;
            }
            set {
                if (value == null) {
                    throw new ArgumentNullException("PositiveInfinitySymbol",
                        Environment.GetResourceString("ArgumentNull_String"));
                }
                Contract.EndContractBlock();
                VerifyWritable();
                positiveInfinitySymbol = value;
            }
        }


         public String PositiveSign {
            get { return positiveSign; }
            set {
                if (value == null) {
                    throw new ArgumentNullException("PositiveSign",
                        Environment.GetResourceString("ArgumentNull_String"));
                }
                Contract.EndContractBlock();
                VerifyWritable();
                positiveSign = value;
            }
        }


         public int PercentDecimalDigits {
            get { return percentDecimalDigits; }
            set {
                if (value < 0 || value > 99) {
                    throw new ArgumentOutOfRangeException(
                                "PercentDecimalDigits",
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    Environment.GetResourceString("ArgumentOutOfRange_Range"),
                                    0,
                                    99));
                }
                Contract.EndContractBlock();
                VerifyWritable();
                percentDecimalDigits = value;
            }
        }


         public String PercentDecimalSeparator {
            get { return percentDecimalSeparator; }
            set {
                VerifyWritable();
                VerifyDecimalSeparator(value, "PercentDecimalSeparator");
                percentDecimalSeparator = value;
            }
        }


         public String PercentGroupSeparator {
            get { return percentGroupSeparator; }
            set {
                VerifyWritable();
                VerifyGroupSeparator(value, "PercentGroupSeparator");
                percentGroupSeparator = value;
            }
        }


         public String PercentSymbol {
            get {
                return percentSymbol;
            }
            set {
                if (value == null) {
                    throw new ArgumentNullException("PercentSymbol",
                        Environment.GetResourceString("ArgumentNull_String"));
                }
                Contract.EndContractBlock();
                VerifyWritable();
                percentSymbol = value;
            }
        }


         public String PerMilleSymbol {
            get { return perMilleSymbol; }
            set {
                if (value == null) {
                    throw new ArgumentNullException("PerMilleSymbol",
                        Environment.GetResourceString("ArgumentNull_String"));
                }
                Contract.EndContractBlock();
                VerifyWritable();
                perMilleSymbol = value;
            }
        }


        [System.Runtime.InteropServices.ComVisible(false)]
        public String[] NativeDigits
        {
            get { return (String[])nativeDigits.Clone(); }
            set
            {
                VerifyWritable();
                VerifyNativeDigits(value, "NativeDigits");
                nativeDigits = value;
            }
        }

#if !FEATURE_CORECLR
        [System.Runtime.InteropServices.ComVisible(false)]
        public DigitShapes DigitSubstitution
        {
            get { return (DigitShapes)digitSubstitution; }
            set
            {
                VerifyWritable();
                VerifyDigitSubstitution(value, "DigitSubstitution");
                digitSubstitution = (int)value;
            }
        }
#endif // !FEATURE_CORECLR

        public Object GetFormat(Type formatType) {
            return formatType == typeof(NumberFormatInfo)? this: null;
        }

        public static NumberFormatInfo ReadOnly(NumberFormatInfo nfi) {
            if (nfi == null) {
                throw new ArgumentNullException("nfi");
            }
            Contract.EndContractBlock();
            if (nfi.IsReadOnly) {
                return (nfi);
            }
            NumberFormatInfo info = (NumberFormatInfo)(nfi.MemberwiseClone());
            info.isReadOnly = true;
            return info;
        }

        // private const NumberStyles InvalidNumberStyles = unchecked((NumberStyles) 0xFFFFFC00);
        private const NumberStyles InvalidNumberStyles = ~(NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite
                                                           | NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign
                                                           | NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint
                                                           | NumberStyles.AllowThousands | NumberStyles.AllowExponent
                                                           | NumberStyles.AllowCurrencySymbol | NumberStyles.AllowHexSpecifier);

        internal static void ValidateParseStyleInteger(NumberStyles style) {
            // Check for undefined flags
            if ((style & InvalidNumberStyles) != 0) {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidNumberStyles"), "style");
            }
            Contract.EndContractBlock();
            if ((style & NumberStyles.AllowHexSpecifier) != 0) { // Check for hex number
                if ((style & ~NumberStyles.HexNumber) != 0) {
                    throw new ArgumentException(Environment.GetResourceString("Arg_InvalidHexStyle"));
                }
            }
        }

        internal static void ValidateParseStyleFloatingPoint(NumberStyles style) {
            // Check for undefined flags
            if ((style & InvalidNumberStyles) != 0) {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidNumberStyles"), "style");
            }
            Contract.EndContractBlock();
            if ((style & NumberStyles.AllowHexSpecifier) != 0) { // Check for hex number
                throw new ArgumentException(Environment.GetResourceString("Arg_HexStyleNotSupported"));
            }
        }
    } // NumberFormatInfo
}









