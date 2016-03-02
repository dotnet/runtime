// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

namespace TCulture
{
	
	/// <summary>
	/// Culture:  A specific culture
	/// Note:     On cultures with a true for Valid can be set to a thread
	///              The true value is specifing the language as culture specific
	/// </summary>
	class Culture
	{
		private string strCultureName;
		private int intCultureID;
		private string strLanguage;
		private bool bValid;

		public Culture(string CN, int ID, string L, bool V)
		{
			strCultureName = CN;
			intCultureID = Convert.ToInt32(ID);
			strLanguage = L;
			bValid = V;
		}

		public string CultureName(){ return strCultureName; }
		public string Language(){ return strLanguage; }
		public int CultureID(){ return intCultureID; }
		public bool Valid(){ return bValid; }
	}
	
	
	public class CultureNames
	{
		private Culture[] cultures;
		public CultureNames()
		{
			CN_Setup();
		}
		
		public int GetLength(){	return cultures.Length;	}
		public string GetName(int i){ return cultures[i].CultureName(); }
		public string GetLanguage(int i){ return cultures[i].Language(); }
		public int GetID(int i){ return cultures[i].CultureID(); }
		public bool Valid(int i){ return cultures[i].Valid(); }

		private void CN_Setup()
		{
			cultures = new Culture[191];
			cultures[0] = new Culture("",0x007F,"invariant culture",true);
			cultures[1] = new Culture("af",0x0036,"Afrikaans",true);
			cultures[2] = new Culture("af-ZA",0x0436,"Afrikaans - South Africa",true);
			cultures[3] = new Culture("sq",0x001C,"Albanian",true);
			cultures[4] = new Culture("sq-AL",0x041C,"Albanian - Albania",true);
			cultures[5] = new Culture("ar",0x0001,"Arabic",true);
			cultures[6] = new Culture("ar-DZ",0x1401,"Arabic - Algeria",true);
			cultures[7] = new Culture("ar-BH",0x3C01,"Arabic - Bahrain",true);
			cultures[8] = new Culture("ar-EG",0x0C01,"Arabic - Egypt",true);
			cultures[9] = new Culture("ar-IQ",0x0801,"Arabic - Iraq",true);
			cultures[10] = new Culture("ar-JO",0x2C01,"Arabic - Jordan",true);
			cultures[11] = new Culture("ar-KW",0x3401,"Arabic - Kuwait",true);
			cultures[12] = new Culture("ar-LB",0x3001,"Arabic - Lebanon",true);
			cultures[13] = new Culture("ar-LY",0x1001,"Arabic - Libya",true);
			cultures[14] = new Culture("ar-MA",0x1801,"Arabic - Morocco",true);
			cultures[15] = new Culture("ar-OM",0x2001,"Arabic - Oman",true);
			cultures[16] = new Culture("ar-QA",0x4001,"Arabic - Qatar",true);
			cultures[17] = new Culture("ar-SA",0x0401,"Arabic - Saudi Arabia",true);
			cultures[18] = new Culture("ar-SY",0x2801,"Arabic - Syria",true);
			cultures[19] = new Culture("ar-TN",0x1C01,"Arabic - Tunisia",true);
			cultures[20] = new Culture("ar-AE",0x3801,"Arabic - United Arab Emirates",true);
			cultures[21] = new Culture("ar-YE",0x2401,"Arabic - Yemen",true);
			cultures[22] = new Culture("hy",0x002B,"Armenian",true);
			cultures[23] = new Culture("hy-AM",0x042B,"Armenian - Armenia",true);
			cultures[24] = new Culture("az",0x002C,"Azeri",true);
			cultures[25] = new Culture("az-Cyrl-AZ",0x082C,"Azeri (Cyrillic) - Azerbaijan",true);
			cultures[26] = new Culture("az-Latn-AZ",0x042C,"Azeri (Latin) - Azerbaijan",true);
			cultures[27] = new Culture("eu",0x002D,"Basque",true);
			cultures[28] = new Culture("eu-ES",0x042D,"Basque - Basque",true);
			cultures[29] = new Culture("be",0x0023,"Belarusian",true);
			cultures[30] = new Culture("be-BY",0x0423,"Belarusian - Belarus",true);
			cultures[31] = new Culture("bg",0x0002,"Bulgarian",true);
			cultures[32] = new Culture("bg-BG",0x0402,"Bulgarian - Bulgaria",true);
			cultures[33] = new Culture("ca",0x0003,"Catalan",true);
			cultures[34] = new Culture("ca-ES",0x0403,"Catalan - Catalan",true);
			cultures[35] = new Culture("hr",0x001A,"Croatian",true);
			cultures[36] = new Culture("hr-HR",0x041A,"Croatian - Croatia",true);
			cultures[37] = new Culture("cs",0x0005,"Czech",true);
			cultures[38] = new Culture("cs-CZ",0x0405,"Czech - Czech Republic",true);
			cultures[39] = new Culture("da",0x0006,"Danish",true);
			cultures[40] = new Culture("da-DK",0x0406,"Danish - Denmark",true);
			cultures[41] = new Culture("dv",0x0065,"Dhivehi",true);
			cultures[42] = new Culture("dv-MV",0x0465,"Dhivehi - Maldives",true);
			cultures[43] = new Culture("nl",0x0013,"Dutch",true);
			cultures[44] = new Culture("nl-BE",0x0813,"Dutch - Belgium",true);
			cultures[45] = new Culture("nl-NL",0x0413,"Dutch - The Netherlands",true);
			cultures[46] = new Culture("en",0x0009,"English",true);
			cultures[47] = new Culture("en-AU",0x0C09,"English - Australia",true);
			cultures[48] = new Culture("en-BZ",0x2809,"English - Belize",true);
			cultures[49] = new Culture("en-CA",0x1009,"English - Canada",true);
			cultures[50] = new Culture("en-029",0x2409,"English - Caribbean",true);
			cultures[51] = new Culture("en-IE",0x1809,"English - Ireland",true);
			cultures[52] = new Culture("en-JM",0x2009,"English - Jamaica",true);
			cultures[53] = new Culture("en-NZ",0x1409,"English - New Zealand",true);
			cultures[54] = new Culture("en-PH",0x3409,"English - Philippines",true);
			cultures[55] = new Culture("en-ZA",0x1C09,"English - South Africa",true);
			cultures[56] = new Culture("en-TT",0x2C09,"English - Trinidad and Tobago",true);
			cultures[57] = new Culture("en-GB",0x0809,"English - United Kingdom",true);
			cultures[58] = new Culture("en-US",0x0409,"English - United States",true);
			cultures[59] = new Culture("en-ZW",0x3009,"English - Zimbabwe",true);
			cultures[60] = new Culture("et",0x0025,"Estonian",true);
			cultures[61] = new Culture("et-EE",0x0425,"Estonian - Estonia",true);
			cultures[62] = new Culture("fo",0x0038,"Faroese",true);
			cultures[63] = new Culture("fo-FO",0x0438,"Faroese - Faroe Islands",true);
			cultures[64] = new Culture("fa",0x0029,"Farsi",true);
			cultures[65] = new Culture("fa-IR",0x0429,"Farsi - Iran",true);
			cultures[66] = new Culture("fi",0x000B,"Finnish",true);
			cultures[67] = new Culture("fi-FI",0x040B,"Finnish - Finland",true);
			cultures[68] = new Culture("fr",0x000C,"French",true);
			cultures[69] = new Culture("fr-BE",0x080C,"French - Belgium",true);
			cultures[70] = new Culture("fr-CA",0x0C0C,"French - Canada",true);
			cultures[71] = new Culture("fr-FR",0x040C,"French - France",true);
			cultures[72] = new Culture("fr-LU",0x140C,"French - Luxembourg",true);
			cultures[73] = new Culture("fr-MC",0x180C,"French - Monaco",true);
			cultures[74] = new Culture("fr-CH",0x100C,"French - Switzerland",true);
			cultures[75] = new Culture("gl",0x0056,"Galician",true);
			cultures[76] = new Culture("gl-ES",0x0456,"Galician - Galician",true);
			cultures[77] = new Culture("ka",0x0037,"Georgian",true);
			cultures[78] = new Culture("ka-GE",0x0437,"Georgian - Georgia",true);
			cultures[79] = new Culture("de",0x0007,"German",true);
			cultures[80] = new Culture("de-AT",0x0C07,"German - Austria",true);
			cultures[81] = new Culture("de-DE",0x0407,"German - Germany",true);
			cultures[82] = new Culture("de-LI",0x1407,"German - Liechtenstein",true);
			cultures[83] = new Culture("de-LU",0x1007,"German - Luxembourg",true);
			cultures[84] = new Culture("de-CH",0x0807,"German - Switzerland",true);
			cultures[85] = new Culture("el",0x0008,"Greek",true);
			cultures[86] = new Culture("el-GR",0x0408,"Greek - Greece",true);
			cultures[87] = new Culture("gu",0x0047,"Gujarati",true);
			cultures[88] = new Culture("gu-IN",0x0447,"Gujarati - India",true);
			cultures[89] = new Culture("he",0x000D,"Hebrew",true);
			cultures[90] = new Culture("he-IL",0x040D,"Hebrew - Israel",true);
			cultures[91] = new Culture("hi",0x0039,"Hindi",true);
			cultures[92] = new Culture("hi-IN",0x0439,"Hindi - India",true);
			cultures[93] = new Culture("hu",0x000E,"Hungarian",true);
			cultures[94] = new Culture("hu-HU",0x040E,"Hungarian - Hungary",true);
			cultures[95] = new Culture("is",0x000F,"Icelandic",true);
			cultures[96] = new Culture("is-IS",0x040F,"Icelandic - Iceland",true);
			cultures[97] = new Culture("id",0x0021,"Indonesian",true);
			cultures[98] = new Culture("id-ID",0x0421,"Indonesian - Indonesia",true);
			cultures[99] = new Culture("it",0x0010,"Italian",true);
			cultures[100] = new Culture("it-IT",0x0410,"Italian - Italy",true);
			cultures[101] = new Culture("it-CH",0x0810,"Italian - Switzerland",true);
			cultures[102] = new Culture("kn",0x004B,"Kannada",true);
			cultures[103] = new Culture("kn-IN",0x044B,"Kannada - India",true);
			cultures[104] = new Culture("kk",0x003F,"Kazakh",true);
			cultures[105] = new Culture("kk-KZ",0x043F,"Kazakh - Kazakhstan",true);
			cultures[106] = new Culture("kok",0x0057,"Konkani",true);
			cultures[107] = new Culture("kok-IN",0x0457,"Konkani - India",true);
			cultures[108] = new Culture("ky",0x0040,"Kyrgyz",true);
			cultures[109] = new Culture("ky-KG",0x0440,"Kyrgyz - Kazakhstan",true);
			cultures[110] = new Culture("lv",0x0026,"Latvian",true);
			cultures[111] = new Culture("lv-LV",0x0426,"Latvian - Latvia",true);
			cultures[112] = new Culture("lt",0x0027,"Lithuanian",true);
			cultures[113] = new Culture("lt-LT",0x0427,"Lithuanian - Lithuania",true);
			cultures[114] = new Culture("mk",0x002F,"Macedonian",true);
			cultures[115] = new Culture("mk-MK",0x042F,"Macedonian - FYROM",true);
			cultures[116] = new Culture("ms",0x003E,"Malay",true);
			cultures[117] = new Culture("ms-BN",0x083E,"Malay - Brunei",true);
			cultures[118] = new Culture("ms-MY",0x043E,"Malay - Malaysia",true);
			cultures[119] = new Culture("mr",0x004E,"Marathi",true);
			cultures[120] = new Culture("mr-IN",0x044E,"Marathi - India",true);
			cultures[121] = new Culture("mn",0x0050,"Mongolian",true);
			cultures[122] = new Culture("mn-MN",0x0450,"Mongolian - Mongolia",true);
			cultures[123] = new Culture("no",0x0014,"Norwegian",true);
			cultures[124] = new Culture("nb-NO",0x0414,"Norwegian (Bokm\u00e5l) - Norway",true);
			cultures[125] = new Culture("nn-NO",0x0814,"Norwegian (Nynorsk) - Norway",true);
			cultures[126] = new Culture("pl",0x0015,"Polish",true);
			cultures[127] = new Culture("pl-PL",0x0415,"Polish - Poland",true);
			cultures[128] = new Culture("pt",0x0016,"Portuguese",true);
			cultures[129] = new Culture("pt-BR",0x0416,"Portuguese - Brazil",true);
			cultures[130] = new Culture("pt-PT",0x0816,"Portuguese - Portugal",true);
			cultures[131] = new Culture("pa",0x0046,"Punjabi",true);
			cultures[132] = new Culture("pa-IN",0x0446,"Punjabi - India",true);
			cultures[133] = new Culture("ro",0x0018,"Romanian",true);
			cultures[134] = new Culture("ro-RO",0x0418,"Romanian - Romania",true);
			cultures[135] = new Culture("ru",0x0019,"Russian",true);
			cultures[136] = new Culture("ru-RU",0x0419,"Russian - Russia",true);
			cultures[137] = new Culture("sa",0x004F,"Sanskrit",true);
			cultures[138] = new Culture("sa-IN",0x044F,"Sanskrit - India",true);
			cultures[139] = new Culture("sr-Cyrl-CS",0x0C1A,"Serbian (Cyrillic) - Serbia",true);
			cultures[140] = new Culture("sr-Latn-CS",0x081A,"Serbian (Latin) - Serbia",true);
			cultures[141] = new Culture("sk",0x001B,"Slovak",true);
			cultures[142] = new Culture("sk-SK",0x041B,"Slovak - Slovakia",true);
			cultures[143] = new Culture("sl",0x0024,"Slovenian",true);
			cultures[144] = new Culture("sl-SI",0x0424,"Slovenian - Slovenia",true);
			cultures[145] = new Culture("es",0x000A,"Spanish",true);
			cultures[146] = new Culture("es-AR",0x2C0A,"Spanish - Argentina",true);
			cultures[147] = new Culture("es-BO",0x400A,"Spanish - Bolivia",true);
			cultures[148] = new Culture("es-CL",0x340A,"Spanish - Chile",true);
			cultures[149] = new Culture("es-CO",0x240A,"Spanish - Colombia",true);
			cultures[150] = new Culture("es-CR",0x140A,"Spanish - Costa Rica",true);
			cultures[151] = new Culture("es-DO",0x1C0A,"Spanish - Dominican Republic",true);
			cultures[152] = new Culture("es-EC",0x300A,"Spanish - Ecuador",true);
			cultures[153] = new Culture("es-SV",0x440A,"Spanish - El Salvador",true);
			cultures[154] = new Culture("es-GT",0x100A,"Spanish - Guatemala",true);
			cultures[155] = new Culture("es-HN",0x480A,"Spanish - Honduras",true);
			cultures[156] = new Culture("es-MX",0x080A,"Spanish - Mexico",true);
			cultures[157] = new Culture("es-NI",0x4C0A,"Spanish - Nicaragua",true);
			cultures[158] = new Culture("es-PA",0x180A,"Spanish - Panama",true);
			cultures[159] = new Culture("es-PY",0x3C0A,"Spanish - Paraguay",true);
			cultures[160] = new Culture("es-PE",0x280A,"Spanish - Peru",true);
			cultures[161] = new Culture("es-PR",0x500A,"Spanish - Puerto Rico",true);
			cultures[162] = new Culture("es-ES",0x0C0A,"Spanish - Spain",true);
			cultures[163] = new Culture("es-UY",0x380A,"Spanish - Uruguay",true);
			cultures[164] = new Culture("es-VE",0x200A,"Spanish - Venezuela",true);
			cultures[165] = new Culture("sw",0x0041,"Swahili",true);
			cultures[166] = new Culture("sw-KE",0x0441,"Swahili - Kenya",true);
			cultures[167] = new Culture("sv",0x001D,"Swedish",true);
			cultures[168] = new Culture("sv-FI",0x081D,"Swedish - Finland",true);
			cultures[169] = new Culture("sv-SE",0x041D,"Swedish - Sweden",true);
			cultures[170] = new Culture("syr",0x005A,"Syriac",true);
			cultures[171] = new Culture("syr-SY",0x045A,"Syriac - Syria",true);
			cultures[172] = new Culture("ta",0x0049,"Tamil",true);
			cultures[173] = new Culture("ta-IN",0x0449,"Tamil - India",true);
			cultures[174] = new Culture("tt",0x0044,"Tatar",true);
			cultures[175] = new Culture("tt-RU",0x0444,"Tatar - Russia",true);
			cultures[176] = new Culture("te",0x004A,"Telugu",true);
			cultures[177] = new Culture("te-IN",0x044A,"Telugu - India",true);
			cultures[178] = new Culture("th",0x001E,"Thai",true);
			cultures[179] = new Culture("th-TH",0x041E,"Thai - Thailand",true);
			cultures[180] = new Culture("tr",0x001F,"Turkish",true);
			cultures[181] = new Culture("tr-TR",0x041F,"Turkish - Turkey",true);
			cultures[182] = new Culture("uk",0x0022,"Ukrainian",true);
			cultures[183] = new Culture("uk-UA",0x0422,"Ukrainian - Ukraine",true);
			cultures[184] = new Culture("ur",0x0020,"Urdu",true);
			cultures[185] = new Culture("ur-PK",0x0420,"Urdu - Pakistan",true);
			cultures[186] = new Culture("uz",0x0043,"Uzbek",true);
			cultures[187] = new Culture("uz-Cyrl-UZ",0x0843,"Uzbek (Cyrillic) - Uzbekistan",true);
			cultures[188] = new Culture("uz-Latn-UZ",0x0443,"Uzbek (Latin) - Uzbekistan",true);
			cultures[189] = new Culture("vi",0x002A,"Vietnamese",true);
			cultures[190] = new Culture("vi-VN",0x042A,"Vietnamese - Vietnam",true);			
		}
	}
}
