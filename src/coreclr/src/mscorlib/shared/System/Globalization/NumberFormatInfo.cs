// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;

namespace System.Globalization
{
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

    sealed public class NumberFormatInfo : IFormatProvider, ICloneable
    {
        // invariantInfo is constant irrespective of your current culture.
        private static volatile NumberFormatInfo s_invariantInfo;

        // READTHIS READTHIS READTHIS
        // This class has an exact mapping onto a native structure defined in COMNumber.cpp
        // DO NOT UPDATE THIS WITHOUT UPDATING THAT STRUCTURE. IF YOU ADD BOOL, ADD THEM AT THE END.
        // ALSO MAKE SURE TO UPDATE mscorlib.h in the VM directory to check field offsets.
        // READTHIS READTHIS READTHIS
        internal int[] numberGroupSizes = new int[] { 3 };
        internal int[] currencyGroupSizes = new int[] { 3 };
        internal int[] percentGroupSizes = new int[] { 3 };
        internal String positiveSign = "+";
        internal String negativeSign = "-";
        internal String numberDecimalSeparator = ".";
        internal String numberGroupSeparator = ",";
        internal String currencyGroupSeparator = ",";
        internal String currencyDecimalSeparator = ".";
        internal String currencySymbol = "\x00a4";  // U+00a4 is the symbol for International Monetary Fund.
        internal String nanSymbol = "NaN";
        internal String positiveInfinitySymbol = "Infinity";
        internal String negativeInfinitySymbol = "-Infinity";
        internal String percentDecimalSeparator = ".";
        internal String percentGroupSeparator = ",";
        internal String percentSymbol = "%";
        internal String perMilleSymbol = "\u2030";


        internal String[] nativeDigits = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        internal int numberDecimalDigits = 2;
        internal int currencyDecimalDigits = 2;
        internal int currencyPositivePattern = 0;
        internal int currencyNegativePattern = 0;
        internal int numberNegativePattern = 1;
        internal int percentPositivePattern = 0;
        internal int percentNegativePattern = 0;
        internal int percentDecimalDigits = 2;

        internal int digitSubstitution = (int)DigitShapes.None;

        internal bool isReadOnly = false;

        // Is this NumberFormatInfo for invariant culture?
        internal bool m_isInvariant = false;

        public NumberFormatInfo() : this(null)
        {
        }

        private static void VerifyDecimalSeparator(String decSep, String propertyName)
        {
            if (decSep == null)
            {
                throw new ArgumentNullException(propertyName,
                        SR.ArgumentNull_String);
            }

            if (decSep.Length == 0)
            {
                throw new ArgumentException(SR.Argument_EmptyDecString);
            }
        }

        private static void VerifyGroupSeparator(String groupSep, String propertyName)
        {
            if (groupSep == null)
            {
                throw new ArgumentNullException(propertyName,
                        SR.ArgumentNull_String);
            }
        }

        private static void VerifyNativeDigits(string[] nativeDig, string propertyName)
        {
            if (nativeDig == null)
            {
                throw new ArgumentNullException(propertyName, SR.ArgumentNull_Array);
            }

            if (nativeDig.Length != 10)
            {
                throw new ArgumentException(SR.Argument_InvalidNativeDigitCount, propertyName);
            }

            for (int i = 0; i < nativeDig.Length; i++)
            {
                if (nativeDig[i] == null)
                {
                    throw new ArgumentNullException(propertyName, SR.ArgumentNull_ArrayValue);
                }

                if (nativeDig[i].Length != 1)
                {
                    if (nativeDig[i].Length != 2)
                    {
                        // Not 1 or 2 UTF-16 code points
                        throw new ArgumentException(SR.Argument_InvalidNativeDigitValue, propertyName);
                    }
                    else if (!char.IsSurrogatePair(nativeDig[i][0], nativeDig[i][1]))
                    {
                        // 2 UTF-6 code points, but not a surrogate pair
                        throw new ArgumentException(SR.Argument_InvalidNativeDigitValue, propertyName);
                    }
                }

                if (CharUnicodeInfo.GetDecimalDigitValue(nativeDig[i], 0) != i &&
                    CharUnicodeInfo.GetUnicodeCategory(nativeDig[i], 0) != UnicodeCategory.PrivateUse)
                {
                    // Not the appropriate digit according to the Unicode data properties
                    // (Digit 0 must be a 0, etc.).
                    throw new ArgumentException(SR.Argument_InvalidNativeDigitValue, propertyName);
                }
            }
        }

        private static void VerifyDigitSubstitution(DigitShapes digitSub, string propertyName)
        {
            switch (digitSub)
            {
                case DigitShapes.Context:
                case DigitShapes.None:
                case DigitShapes.NativeNational:
                    // Success.
                    break;

                default:
                    throw new ArgumentException(SR.Argument_InvalidDigitSubstitution, propertyName);
            }
        }

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

        private void VerifyWritable()
        {
            if (isReadOnly)
            {
                throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
            }
        }

        // Returns a default NumberFormatInfo that will be universally
        // supported and constant irrespective of the current culture.
        // Used by FromString methods.
        //

        public static NumberFormatInfo InvariantInfo
        {
            get
            {
                if (s_invariantInfo == null)
                {
                    // Lazy create the invariant info. This cannot be done in a .cctor because exceptions can
                    // be thrown out of a .cctor stack that will need this.
                    NumberFormatInfo nfi = new NumberFormatInfo();
                    nfi.m_isInvariant = true;
                    s_invariantInfo = ReadOnly(nfi);
                }
                return s_invariantInfo;
            }
        }

        public static NumberFormatInfo GetInstance(IFormatProvider formatProvider)
        {
            return formatProvider == null ?
                CurrentInfo : // Fast path for a null provider
                GetProviderNonNull(formatProvider);

            NumberFormatInfo GetProviderNonNull(IFormatProvider provider)
            {
                // Fast path for a regular CultureInfo
                if (provider is CultureInfo cultureProvider && !cultureProvider._isInherited)
                {
                    return cultureProvider.numInfo ?? cultureProvider.NumberFormat;
                }

                return
                    provider as NumberFormatInfo ?? // Fast path for an NFI
                    provider.GetFormat(typeof(NumberFormatInfo)) as NumberFormatInfo ??
                    CurrentInfo;
            }
        }

        public Object Clone()
        {
            NumberFormatInfo n = (NumberFormatInfo)MemberwiseClone();
            n.isReadOnly = false;
            return n;
        }


        public int CurrencyDecimalDigits
        {
            get { return currencyDecimalDigits; }
            set
            {
                if (value < 0 || value > 99)
                {
                    throw new ArgumentOutOfRangeException(
                                nameof(CurrencyDecimalDigits),
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    SR.ArgumentOutOfRange_Range,
                                    0,
                                    99));
                }
                VerifyWritable();
                currencyDecimalDigits = value;
            }
        }


        public String CurrencyDecimalSeparator
        {
            get { return currencyDecimalSeparator; }
            set
            {
                VerifyWritable();
                VerifyDecimalSeparator(value, nameof(CurrencyDecimalSeparator));
                currencyDecimalSeparator = value;
            }
        }


        public bool IsReadOnly
        {
            get
            {
                return isReadOnly;
            }
        }

        //
        // Check the values of the groupSize array.
        //
        // Every element in the groupSize array should be between 1 and 9
        // excpet the last element could be zero.
        //
        internal static void CheckGroupSize(String propName, int[] groupSize)
        {
            for (int i = 0; i < groupSize.Length; i++)
            {
                if (groupSize[i] < 1)
                {
                    if (i == groupSize.Length - 1 && groupSize[i] == 0)
                        return;
                    throw new ArgumentException(SR.Argument_InvalidGroupSize, propName);
                }
                else if (groupSize[i] > 9)
                {
                    throw new ArgumentException(SR.Argument_InvalidGroupSize, propName);
                }
            }
        }


        public int[] CurrencyGroupSizes
        {
            get
            {
                return ((int[])currencyGroupSizes.Clone());
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(CurrencyGroupSizes),
                        SR.ArgumentNull_Obj);
                }
                VerifyWritable();

                Int32[] inputSizes = (Int32[])value.Clone();
                CheckGroupSize(nameof(CurrencyGroupSizes), inputSizes);
                currencyGroupSizes = inputSizes;
            }
        }



        public int[] NumberGroupSizes
        {
            get
            {
                return ((int[])numberGroupSizes.Clone());
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(NumberGroupSizes),
                        SR.ArgumentNull_Obj);
                }
                VerifyWritable();

                Int32[] inputSizes = (Int32[])value.Clone();
                CheckGroupSize(nameof(NumberGroupSizes), inputSizes);
                numberGroupSizes = inputSizes;
            }
        }


        public int[] PercentGroupSizes
        {
            get
            {
                return ((int[])percentGroupSizes.Clone());
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(PercentGroupSizes),
                        SR.ArgumentNull_Obj);
                }
                VerifyWritable();
                Int32[] inputSizes = (Int32[])value.Clone();
                CheckGroupSize(nameof(PercentGroupSizes), inputSizes);
                percentGroupSizes = inputSizes;
            }
        }


        public String CurrencyGroupSeparator
        {
            get { return currencyGroupSeparator; }
            set
            {
                VerifyWritable();
                VerifyGroupSeparator(value, nameof(CurrencyGroupSeparator));
                currencyGroupSeparator = value;
            }
        }


        public String CurrencySymbol
        {
            get { return currencySymbol; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(CurrencySymbol),
                        SR.ArgumentNull_String);
                }
                VerifyWritable();
                currencySymbol = value;
            }
        }

        // Returns the current culture's NumberFormatInfo.  Used by Parse methods.
        //

        public static NumberFormatInfo CurrentInfo
        {
            get
            {
                System.Globalization.CultureInfo culture = CultureInfo.CurrentCulture;
                if (!culture._isInherited)
                {
                    NumberFormatInfo info = culture.numInfo;
                    if (info != null)
                    {
                        return info;
                    }
                }
                return ((NumberFormatInfo)culture.GetFormat(typeof(NumberFormatInfo)));
            }
        }


        public String NaNSymbol
        {
            get
            {
                return nanSymbol;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(NaNSymbol),
                        SR.ArgumentNull_String);
                }
                VerifyWritable();
                nanSymbol = value;
            }
        }



        public int CurrencyNegativePattern
        {
            get { return currencyNegativePattern; }
            set
            {
                if (value < 0 || value > 15)
                {
                    throw new ArgumentOutOfRangeException(
                                nameof(CurrencyNegativePattern),
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    SR.ArgumentOutOfRange_Range,
                                    0,
                                    15));
                }
                VerifyWritable();
                currencyNegativePattern = value;
            }
        }


        public int NumberNegativePattern
        {
            get { return numberNegativePattern; }
            set
            {
                //
                // NOTENOTE: the range of value should correspond to negNumberFormats[] in vm\COMNumber.cpp.
                //
                if (value < 0 || value > 4)
                {
                    throw new ArgumentOutOfRangeException(
                                nameof(NumberNegativePattern),
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    SR.ArgumentOutOfRange_Range,
                                    0,
                                    4));
                }
                VerifyWritable();
                numberNegativePattern = value;
            }
        }


        public int PercentPositivePattern
        {
            get { return percentPositivePattern; }
            set
            {
                //
                // NOTENOTE: the range of value should correspond to posPercentFormats[] in vm\COMNumber.cpp.
                //
                if (value < 0 || value > 3)
                {
                    throw new ArgumentOutOfRangeException(
                                nameof(PercentPositivePattern),
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    SR.ArgumentOutOfRange_Range,
                                    0,
                                    3));
                }
                VerifyWritable();
                percentPositivePattern = value;
            }
        }


        public int PercentNegativePattern
        {
            get { return percentNegativePattern; }
            set
            {
                //
                // NOTENOTE: the range of value should correspond to posPercentFormats[] in vm\COMNumber.cpp.
                //
                if (value < 0 || value > 11)
                {
                    throw new ArgumentOutOfRangeException(
                                nameof(PercentNegativePattern),
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    SR.ArgumentOutOfRange_Range,
                                    0,
                                    11));
                }
                VerifyWritable();
                percentNegativePattern = value;
            }
        }


        public String NegativeInfinitySymbol
        {
            get
            {
                return negativeInfinitySymbol;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(NegativeInfinitySymbol),
                        SR.ArgumentNull_String);
                }
                VerifyWritable();
                negativeInfinitySymbol = value;
            }
        }


        public String NegativeSign
        {
            get { return negativeSign; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(NegativeSign),
                        SR.ArgumentNull_String);
                }
                VerifyWritable();
                negativeSign = value;
            }
        }


        public int NumberDecimalDigits
        {
            get { return numberDecimalDigits; }
            set
            {
                if (value < 0 || value > 99)
                {
                    throw new ArgumentOutOfRangeException(
                                nameof(NumberDecimalDigits),
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    SR.ArgumentOutOfRange_Range,
                                    0,
                                    99));
                }
                VerifyWritable();
                numberDecimalDigits = value;
            }
        }


        public String NumberDecimalSeparator
        {
            get { return numberDecimalSeparator; }
            set
            {
                VerifyWritable();
                VerifyDecimalSeparator(value, nameof(NumberDecimalSeparator));
                numberDecimalSeparator = value;
            }
        }


        public String NumberGroupSeparator
        {
            get { return numberGroupSeparator; }
            set
            {
                VerifyWritable();
                VerifyGroupSeparator(value, nameof(NumberGroupSeparator));
                numberGroupSeparator = value;
            }
        }


        public int CurrencyPositivePattern
        {
            get { return currencyPositivePattern; }
            set
            {
                if (value < 0 || value > 3)
                {
                    throw new ArgumentOutOfRangeException(
                                nameof(CurrencyPositivePattern),
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    SR.ArgumentOutOfRange_Range,
                                    0,
                                    3));
                }
                VerifyWritable();
                currencyPositivePattern = value;
            }
        }


        public String PositiveInfinitySymbol
        {
            get
            {
                return positiveInfinitySymbol;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(PositiveInfinitySymbol),
                        SR.ArgumentNull_String);
                }
                VerifyWritable();
                positiveInfinitySymbol = value;
            }
        }


        public String PositiveSign
        {
            get { return positiveSign; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(PositiveSign),
                        SR.ArgumentNull_String);
                }
                VerifyWritable();
                positiveSign = value;
            }
        }


        public int PercentDecimalDigits
        {
            get { return percentDecimalDigits; }
            set
            {
                if (value < 0 || value > 99)
                {
                    throw new ArgumentOutOfRangeException(
                                nameof(PercentDecimalDigits),
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    SR.ArgumentOutOfRange_Range,
                                    0,
                                    99));
                }
                VerifyWritable();
                percentDecimalDigits = value;
            }
        }


        public String PercentDecimalSeparator
        {
            get { return percentDecimalSeparator; }
            set
            {
                VerifyWritable();
                VerifyDecimalSeparator(value, nameof(PercentDecimalSeparator));
                percentDecimalSeparator = value;
            }
        }


        public String PercentGroupSeparator
        {
            get { return percentGroupSeparator; }
            set
            {
                VerifyWritable();
                VerifyGroupSeparator(value, nameof(PercentGroupSeparator));
                percentGroupSeparator = value;
            }
        }


        public String PercentSymbol
        {
            get
            {
                return percentSymbol;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(PercentSymbol),
                        SR.ArgumentNull_String);
                }
                VerifyWritable();
                percentSymbol = value;
            }
        }


        public String PerMilleSymbol
        {
            get { return perMilleSymbol; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(PerMilleSymbol),
                        SR.ArgumentNull_String);
                }
                VerifyWritable();
                perMilleSymbol = value;
            }
        }

        public string[] NativeDigits
        {
            get { return (String[])nativeDigits.Clone(); }
            set
            {
                VerifyWritable();
                VerifyNativeDigits(value, nameof(NativeDigits));
                nativeDigits = value;
            }
        }

        public DigitShapes DigitSubstitution
        {
            get { return (DigitShapes)digitSubstitution; }
            set
            {
                VerifyWritable();
                VerifyDigitSubstitution(value, nameof(DigitSubstitution));
                digitSubstitution = (int)value;
            }
        }

        public Object GetFormat(Type formatType)
        {
            return formatType == typeof(NumberFormatInfo) ? this : null;
        }

        public static NumberFormatInfo ReadOnly(NumberFormatInfo nfi)
        {
            if (nfi == null)
            {
                throw new ArgumentNullException(nameof(nfi));
            }
            if (nfi.IsReadOnly)
            {
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

        internal static void ValidateParseStyleInteger(NumberStyles style)
        {
            // Check for undefined flags
            if ((style & InvalidNumberStyles) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidNumberStyles, nameof(style));
            }
            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            { // Check for hex number
                if ((style & ~NumberStyles.HexNumber) != 0)
                {
                    throw new ArgumentException(SR.Arg_InvalidHexStyle);
                }
            }
        }

        internal static void ValidateParseStyleFloatingPoint(NumberStyles style)
        {
            // Check for undefined flags
            if ((style & InvalidNumberStyles) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidNumberStyles, nameof(style));
            }
            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            { // Check for hex number
                throw new ArgumentException(SR.Arg_HexStyleNotSupported);
            }
        }
    } // NumberFormatInfo
}









