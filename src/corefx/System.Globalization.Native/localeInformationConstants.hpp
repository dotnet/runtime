//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// Enum that corresponds to CultureData.LocaleNumberData enum.
// The numeric values of the enum members match their Win32 counterparts. 
enum LocaleNumberData : int32_t
{
	LanguageId = 0x00000001,
	MeasurementSystem = 0x0000000D,
	FractionalDigitsCount = 0x00000011,
	NegativeNumberFormat = 0x00001010,
	MonetaryFractionalDigitsCount = 0x00000019,
	PositiveMonetaryNumberFormat = 0x0000001B,
	NegativeMonetaryNumberFormat = 0x0000001C,
	CalendarType = 0x00001009,
	FirstWeekOfYear = 0x0000100D,
	ReadingLayout = 0x00000070,
	NegativePercentFormat = 0x00000074,
	PositivePercentFormat = 0x00000075,
	Digit = 0x00000010,
	Monetary = 0x00000018
};

// Enum that corresponds to CultureData.LocaleStringData enum.
// The numeric values of the enum members match their Win32 counterparts. 
enum LocaleStringData : int32_t
{
	LocalizedDisplayName = 0x00000002,
	EnglishDisplayName = 0x00000072,
	NativeDisplayName = 0x00000073,
	LocalizedLanguageName = 0x0000006f,
	EnglishLanguageName = 0x00001001,
	NativeLanguageName = 0x00000004,
	EnglishCountryName = 0x00001002,
	NativeCountryName = 0x00000008,
	ListSeparator = 0x0000000C,
	DecimalSeparator = 0x0000000E,
	ThousandSeparator = 0x0000000F,
	Digits = 0x00000013,
	MonetarySymbol = 0x00000014,
	Iso4217MonetarySymbol = 0x00000015,
	MonetaryDecimalSeparator = 0x00000016,
	MonetaryThousandSeparator = 0x00000017,
	AMDesignator = 0x00000028,
	PMDesignator = 0x00000029,
	PositiveSign = 0x00000050,
	NegativeSign = 0x00000051,
	Iso639LanguageName = 0x00000059,
	Iso3166CountryName = 0x0000005A,
	NaNSymbol = 0x00000069,
	PositiveInfinitySymbol = 0x0000006a,
	PercentSymbol = 0x00000076,
	PerMilleSymbol = 0x00000077
};
