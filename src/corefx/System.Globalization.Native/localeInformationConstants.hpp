//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/// <remarks>
/// The numeric values of the enum members match their Win32 counterparts.  The CultureData Win32 PAL implementation
/// takes a dependency on this fact, in order to prevent having to construct a mapping from internal values to LCTypes.
/// </remarks>
enum LocaleNumberData : int32_t
{
	/// <summary>language id (coresponds to LOCALE_ILANGUAGE)</summary>
	LanguageId = 0x00000001,
	/// <summary>0 = metric, 1 = US (coresponds to LOCALE_IMEASURE)</summary>
	MeasurementSystem = 0x0000000D,
	/// <summary>number of fractional digits (coresponds to LOCALE_IDIGITS)</summary>
	FractionalDigitsCount = 0x00000011,
	/// <summary>negative number mode (coresponds to LOCALE_INEGNUMBER)</summary>
	NegativeNumberFormat = 0x00001010,
	/// <summary># local monetary digits (coresponds to LOCALE_ICURRDIGITS)</summary>
	MonetaryFractionalDigitsCount = 0x00000019,
	/// <summary>positive currency mode (coresponds to LOCALE_ICURRENCY)</summary>
	PositiveMonetaryNumberFormat = 0x0000001B,
	/// <summary>negative currency mode (coresponds to LOCALE_INEGCURR)</summary>
	NegativeMonetaryNumberFormat = 0x0000001C,
	/// <summary>type of calendar specifier (coresponds to LOCALE_ICALENDARTYPE)</summary>
	CalendarType = 0x00001009,
	/// <summary>first week of year specifier (coresponds to LOCALE_IFIRSTWEEKOFYEAR)</summary>
	FirstWeekOfYear = 0x0000100D,
	/// <summary>
	/// Returns one of the following 4 reading layout values:
	///  0 - Left to right (eg en-US)
	///  1 - Right to left (eg arabic locales)
	///  2 - Vertical top to bottom with columns to the left and also left to right (ja-JP locales)
	///  3 - Vertical top to bottom with columns proceeding to the right
	/// (coresponds to LOCALE_IREADINGLAYOUT)
	/// </summary>
	ReadingLayout = 0x00000070,
	/// <summary>Returns 0-11 for the negative percent format (coresponds to LOCALE_INEGATIVEPERCENT)</summary>
	NegativePercentFormat = 0x00000074,
	/// <summary>Returns 0-3 for the positive percent format (coresponds to LOCALE_IPOSITIVEPERCENT)</summary>
	PositivePercentFormat = 0x00000075,
	/// <summary>digit grouping (coresponds to LOCALE_SGROUPING)</summary>
	Digit = 0x00000010,
	/// <summary>monetary grouping (coresponds to LOCALE_SMONGROUPING)</summary>
	Monetary = 0x00000018
};

/// <remarks>
/// The numeric values of the enum members match their Win32 counterparts.  The CultureData Win32 PAL implementation
/// takes a dependency on this fact, in order to prevent having to construct a mapping from internal values to LCTypes.
/// </remarks>
enum LocaleStringData : int32_t
{
	/// <summary>localized name of locale, eg "German (Germany)" in UI language (coresponds to LOCALE_SLOCALIZEDDISPLAYNAME)</summary>
	LocalizedDisplayName = 0x00000002,
	/// <summary>Display name (language + country usually) in English, eg "German (Germany)" (coresponds to LOCALE_SENGLISHDISPLAYNAME)</summary>
	EnglishDisplayName = 0x00000072,
	/// <summary>Display name in native locale language, eg "Deutsch (Deutschland) (coresponds to LOCALE_SNATIVEDISPLAYNAME)</summary>
	NativeDisplayName = 0x00000073,
	/// <summary>Language Display Name for a language, eg "German" in UI language (coresponds to LOCALE_SLOCALIZEDLANGUAGENAME)</summary>
	LocalizedLanguageName = 0x0000006f,
	/// <summary>English name of language, eg "German" (coresponds to LOCALE_SENGLISHLANGUAGENAME)</summary>
	EnglishLanguageName = 0x00001001,
	/// <summary>native name of language, eg "Deutsch" (coresponds to LOCALE_SNATIVELANGUAGENAME)</summary>
	NativeLanguageName = 0x00000004,
	/// <summary>English name of country, eg "Germany" (coresponds to LOCALE_SENGLISHCOUNTRYNAME)</summary>
	EnglishCountryName = 0x00001002,
	/// <summary>native name of country, eg "Deutschland" (coresponds to LOCALE_SNATIVECOUNTRYNAME)</summary>
	NativeCountryName = 0x00000008,
	/// <summary>list item separator (coresponds to LOCALE_SLIST)</summary>
	ListSeparator = 0x0000000C,
	/// <summary>decimal separator (coresponds to LOCALE_SDECIMAL)</summary>
	DecimalSeparator = 0x0000000E,
	/// <summary>thousand separator (coresponds to LOCALE_STHOUSAND)</summary>
	ThousandSeparator = 0x0000000F,
	/// <summary>digit grouping (coresponds to LOCALE_SGROUPING)</summary>
	Digits = 0x00000013,
	/// <summary>local monetary symbol (coresponds to LOCALE_SCURRENCY)</summary>
	MonetarySymbol = 0x00000014,
	/// <summary>uintl monetary symbol (coresponds to LOCALE_SINTLSYMBOL)</summary>
	Iso4217MonetarySymbol = 0x00000015,
	/// <summary>monetary decimal separator (coresponds to LOCALE_SMONDECIMALSEP)</summary>
	MonetaryDecimalSeparator = 0x00000016,
	/// <summary>monetary thousand separator (coresponds to LOCALE_SMONTHOUSANDSEP)</summary>
	MonetaryThousandSeparator = 0x00000017,
	/// <summary>AM designator (coresponds to LOCALE_S1159)</summary>
	AMDesignator = 0x00000028,
	/// <summary>PM designator (coresponds to LOCALE_S2359)</summary>
	PMDesignator = 0x00000029,
	/// <summary>positive sign (coresponds to LOCALE_SPOSITIVESIGN)</summary>
	PositiveSign = 0x00000050,
	/// <summary>negative sign (coresponds to LOCALE_SNEGATIVESIGN)</summary>
	NegativeSign = 0x00000051,
	/// <summary>ISO abbreviated language name (coresponds to LOCALE_SISO639LANGNAME)</summary>
	Iso639LanguageName = 0x00000059,
	/// <summary>ISO abbreviated country name (coresponds to LOCALE_SISO3166CTRYNAME)</summary>
	Iso3166CountryName = 0x0000005A,
	/// <summary>Not a Number (coresponds to LOCALE_SNAN)</summary>
	NaNSymbol = 0x00000069,
	/// <summary>+ Infinity (coresponds to LOCALE_SPOSINFINITY)</summary>
	PositiveInfinitySymbol = 0x0000006a,
	/// <summary>Returns the percent symbol (coresponds to LOCALE_SPERCENT)</summary>
	PercentSymbol = 0x00000076,
	/// <summary>Returns the permille (U+2030) symbol (coresponds to LOCALE_SPERMILLE)</summary>
	PerMilleSymbol = 0x00000077
};
