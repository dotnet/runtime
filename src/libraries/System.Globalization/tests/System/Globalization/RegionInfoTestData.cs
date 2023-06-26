// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Tests;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Globalization.Tests
{
    public class RegionInfoTestData
    {
        public record CurrencyTestDataModel(string Locale, string ISOCode, string Symbol);

        public static IEnumerable<object[]> CurrencyDataOnWasm(bool isTestingRegionInfoProperty)
        {
            yield return new object[] { new CurrencyTestDataModel ( "ar-SA", "SAR", "\u0631.\u0633.\u200f" )};
            yield return new object[] { new CurrencyTestDataModel ( "am-ET", "ETB", "\u1265\u122D" )};
            yield return new object[] { new CurrencyTestDataModel ( "bg-BG", "BGN", "\u043B\u0432." )};
            yield return new object[] { new CurrencyTestDataModel ( "bn-BD", "BDT", "\u09F3" )};
            yield return new object[] { new CurrencyTestDataModel ( "bn-IN", "INR", "\u20B9" )};
            yield return new object[] { new CurrencyTestDataModel ( "ca-AD", "EUR", "€" )};
            yield return new object[] { new CurrencyTestDataModel ( "cs-CZ", "CZK", "K\u010D" )};
            yield return new object[] { new CurrencyTestDataModel ( "da-DK", "DKK", "kr." )};
            yield return new object[] { new CurrencyTestDataModel ( "de-CH", "CHF", "CHF" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-AE", "AED", "AED" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-AG", "XCD", "$" ) };
            yield return new object[] { new CurrencyTestDataModel ( "en-AU", "AUD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-BB", "BBD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-BI", "BIF", "FBu" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-BM", "BMD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-BS", "BSD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-BW", "BWP", "P" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-BZ", "BZD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-CM", "XAF", "FCFA" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-CK", "NZD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-DK", "DKK", "kr." )};
            yield return new object[] { new CurrencyTestDataModel ( "en-ER", "ERN", "Nfk" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-FJ", "FJD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-FK", "FKP", "£" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-FM", "USD", "US$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-GB", "GBP", "£" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-GH", "GHS", "GH₵" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-GI", "GIP", "£" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-GM", "GMD", "D" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-GY", "GYD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-HK", "HKD", "HK$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-IL", "ILS", "\u20AA" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-JM", "JMD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-KE", "KES", "Ksh" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-KY", "KYD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-LR", "LRD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-LS", "ZAR", "R" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-MG", "MGA", "Ar" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-MH", "USD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-MO", "MOP", "MOP$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-MU", "MUR", "Rs" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-MW", "MWK", "MK" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-MY", "MYR", "RM" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-NA", "NAD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-NG", "NGN", "\u20A6" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-PG", "PGK", "K" )};           
            yield return new object[] { new CurrencyTestDataModel ( "en-PH", "PHP", "\u20B1" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-PK", "PKR", "Rs" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-PR", "USD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-RW", "RWF", "RF" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-SB", "SBD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-SC", "SCR", "SR" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-SH", "SHP", "£" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-SL", "SLL", "SLL" )}; // ICU: le
            yield return new object[] { new CurrencyTestDataModel ( "en-SS", "SSP", "£" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-SX", "ANG", "NAf." )};
            yield return new object[] { new CurrencyTestDataModel ( "en-SZ", "SZL", "E" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-SD", "SDG", "SDG" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-SE", "SEK", "kr" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-SG", "SGD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-TK", "NZD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-TO", "TOP", "T$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-TT", "TTD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-TZ", "TZS", "TSh" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-UG", "UGX", "USh" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-UM", "USD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-US", "USD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-VI", "USD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-VU", "VUV", "VT" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-WS", "WST", "WS$" )};
            yield return new object[] { new CurrencyTestDataModel ( "en-ZM", "ZMW", "K" )};
            yield return new object[] { new CurrencyTestDataModel ( "es-419", "¤¤", "¤" )};
            yield return new object[] { new CurrencyTestDataModel ( "es-MX", "MXN", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "fa-IR", "IRR", "\u0631\u06CC\u0627\u0644" )};
            yield return new object[] { new CurrencyTestDataModel ( "fil-PH", "PHP", "\u20B1" )};
            yield return new object[] { new CurrencyTestDataModel ( "fr-CA", "CAD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "he-IL", "ILS", "\u20aa" )};
            yield return new object[] { new CurrencyTestDataModel ( "hr-BA", "BAM", "KM" )};
            yield return new object[] { new CurrencyTestDataModel ( "hr-HR", "HRK", "kn" )}; // ICU: HRK
            yield return new object[] { new CurrencyTestDataModel ( "hu-HU", "HUF", "Ft" )};
            yield return new object[] { new CurrencyTestDataModel ( "id-ID", "IDR", "Rp" )};
            yield return new object[] { new CurrencyTestDataModel ( "ja-JP", "JPY", "\uFFE5" )};
            yield return new object[] { new CurrencyTestDataModel ( "ko-KR", "KRW", "₩" )};
            yield return new object[] { new CurrencyTestDataModel ( "ms-BN", "BND", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "ms-MY", "MYR", "RM" )};
            yield return new object[] { new CurrencyTestDataModel ( "ms-SG", "SGD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "nb-NO", "NOK", "kr" )};
            yield return new object[] { new CurrencyTestDataModel ( "nl-AW", "AWG", "Afl." )};
            yield return new object[] { new CurrencyTestDataModel ( "pl-PL", "PLN", "z\u0142" )};
            yield return new object[] { new CurrencyTestDataModel ( "pt-BR", "BRL", "R$" )};
            yield return new object[] { new CurrencyTestDataModel ( "ro-RO", "RON", "RON" )};
            yield return new object[] { new CurrencyTestDataModel ( "ru-RU", "RUB", "₽" )};
            yield return new object[] { new CurrencyTestDataModel ( "sr-Cyrl-RS", "RSD", "RSD" )};
            yield return new object[] { new CurrencyTestDataModel ( "sr-Latn-RS", "RSD", "RSD" )};
            yield return new object[] { new CurrencyTestDataModel ( "sv-SE", "SEK", "kr" )};
            yield return new object[] { new CurrencyTestDataModel ( "sw-CD", "CDF", "FC" )};
            yield return new object[] { new CurrencyTestDataModel ( "sw-KE", "KES", "Ksh" )};
            yield return new object[] { new CurrencyTestDataModel ( "sw-TZ", "TZS", "TSh" )};
            yield return new object[] { new CurrencyTestDataModel ( "sw-UG", "UGX", "USh" )};
            yield return new object[] { new CurrencyTestDataModel ( "ta-LK", "LKR", "Rs." )};
            yield return new object[] { new CurrencyTestDataModel ( "ta-MY", "MYR", "RM" )};
            yield return new object[] { new CurrencyTestDataModel ( "ta-SG", "SGD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "th-TH", "THB", "\u0E3F" )};
            yield return new object[] { new CurrencyTestDataModel ( "tr-TR", "TRY", "\u20BA" )};
            yield return new object[] { new CurrencyTestDataModel ( "uk-UA", "UAH", "\u0433\u0440\u043d" )}; // ICU: \u20B4
            yield return new object[] { new CurrencyTestDataModel ( "vi-VN", "VND", "\u20AB" )};
            yield return new object[] { new CurrencyTestDataModel ( "zh-CN", "CNY", "\u00A5" )};
            yield return new object[] { new CurrencyTestDataModel ( "zh-SG", "SGD", "$" )};
            yield return new object[] { new CurrencyTestDataModel ( "zh-TW", "TWD", "$" )};
        }
    }
}
