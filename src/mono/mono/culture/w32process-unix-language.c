/**
 * \file
 * System.Diagnostics.Process support, mono_w32process_ver_language_name
 */
#include <config.h>
#include <glib.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-publib.h>
#include <mono/metadata/w32process-internals.h>

#if !defined(ENABLE_NETCORE) && !defined(DISABLE_PROCESSES)

static guint32
copy_lang (gunichar2 *lang_out, guint32 lang_len, const gchar *text)
{
	gunichar2 *unitext;
	int chars = strlen (text);
	int ret;

	unitext = g_utf8_to_utf16 (text, -1, NULL, NULL, NULL);
	g_assert (unitext != NULL);

	if (chars < (lang_len - 1)) {
		memcpy (lang_out, (gpointer)unitext, chars * 2);
		lang_out[chars] = '\0';
		ret = chars;
	} else {
		memcpy (lang_out, (gpointer)unitext, (lang_len - 1) * 2);
		lang_out[lang_len] = '\0';
		ret = lang_len;
	}

	g_free (unitext);

	return(ret);
}

guint32
mono_w32process_ver_language_name (guint32 lang, gunichar2 *lang_out, guint32 lang_len)
{
	int primary, secondary;
	const char *name = NULL;

	primary = lang & 0x3FF;
	secondary = (lang >> 10) & 0x3F;

	switch(primary) {
	case 0x00:
		switch (secondary) {
		case 0x01: name = "Process Default Language"; break;
		}
		break;
	case 0x01:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "Arabic (Saudi Arabia)"; break;
		case 0x02: name = "Arabic (Iraq)"; break;
		case 0x03: name = "Arabic (Egypt)"; break;
		case 0x04: name = "Arabic (Libya)"; break;
		case 0x05: name = "Arabic (Algeria)"; break;
		case 0x06: name = "Arabic (Morocco)"; break;
		case 0x07: name = "Arabic (Tunisia)"; break;
		case 0x08: name = "Arabic (Oman)"; break;
		case 0x09: name = "Arabic (Yemen)"; break;
		case 0x0a: name = "Arabic (Syria)"; break;
		case 0x0b: name = "Arabic (Jordan)"; break;
		case 0x0c: name = "Arabic (Lebanon)"; break;
		case 0x0d: name = "Arabic (Kuwait)"; break;
		case 0x0e: name = "Arabic (U.A.E.)"; break;
		case 0x0f: name = "Arabic (Bahrain)"; break;
		case 0x10: name = "Arabic (Qatar)"; break;
		}
		break;
	case 0x02:
		switch (secondary) {
		case 0x00: name = "Bulgarian (Bulgaria)"; break;
		case 0x01: name = "Bulgarian"; break;
		}
		break;
	case 0x03:
		switch (secondary) {
		case 0x00: name = "Catalan (Spain)"; break;
		case 0x01: name = "Catalan"; break;
		}
		break;
	case 0x04:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "Chinese (Taiwan)"; break;
		case 0x02: name = "Chinese (PRC)"; break;
		case 0x03: name = "Chinese (Hong Kong S.A.R.)"; break;
		case 0x04: name = "Chinese (Singapore)"; break;
		case 0x05: name = "Chinese (Macau S.A.R.)"; break;
		}
		break;
	case 0x05:
		switch (secondary) {
		case 0x00: name = "Czech (Czech Republic)"; break;
		case 0x01: name = "Czech"; break;
		}
		break;
	case 0x06:
		switch (secondary) {
		case 0x00: name = "Danish (Denmark)"; break;
		case 0x01: name = "Danish"; break;
		}
		break;
	case 0x07:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "German (Germany)"; break;
		case 0x02: name = "German (Switzerland)"; break;
		case 0x03: name = "German (Austria)"; break;
		case 0x04: name = "German (Luxembourg)"; break;
		case 0x05: name = "German (Liechtenstein)"; break;
		}
		break;
	case 0x08:
		switch (secondary) {
		case 0x00: name = "Greek (Greece)"; break;
		case 0x01: name = "Greek"; break;
		}
		break;
	case 0x09:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "English (United States)"; break;
		case 0x02: name = "English (United Kingdom)"; break;
		case 0x03: name = "English (Australia)"; break;
		case 0x04: name = "English (Canada)"; break;
		case 0x05: name = "English (New Zealand)"; break;
		case 0x06: name = "English (Ireland)"; break;
		case 0x07: name = "English (South Africa)"; break;
		case 0x08: name = "English (Jamaica)"; break;
		case 0x09: name = "English (Caribbean)"; break;
		case 0x0a: name = "English (Belize)"; break;
		case 0x0b: name = "English (Trinidad and Tobago)"; break;
		case 0x0c: name = "English (Zimbabwe)"; break;
		case 0x0d: name = "English (Philippines)"; break;
		case 0x10: name = "English (India)"; break;
		case 0x11: name = "English (Malaysia)"; break;
		case 0x12: name = "English (Singapore)"; break;
		}
		break;
	case 0x0a:
		switch (secondary) {
		case 0x00: name = "Spanish (Spain)"; break;
		case 0x01: name = "Spanish (Traditional Sort)"; break;
		case 0x02: name = "Spanish (Mexico)"; break;
		case 0x03: name = "Spanish (International Sort)"; break;
		case 0x04: name = "Spanish (Guatemala)"; break;
		case 0x05: name = "Spanish (Costa Rica)"; break;
		case 0x06: name = "Spanish (Panama)"; break;
		case 0x07: name = "Spanish (Dominican Republic)"; break;
		case 0x08: name = "Spanish (Venezuela)"; break;
		case 0x09: name = "Spanish (Colombia)"; break;
		case 0x0a: name = "Spanish (Peru)"; break;
		case 0x0b: name = "Spanish (Argentina)"; break;
		case 0x0c: name = "Spanish (Ecuador)"; break;
		case 0x0d: name = "Spanish (Chile)"; break;
		case 0x0e: name = "Spanish (Uruguay)"; break;
		case 0x0f: name = "Spanish (Paraguay)"; break;
		case 0x10: name = "Spanish (Bolivia)"; break;
		case 0x11: name = "Spanish (El Salvador)"; break;
		case 0x12: name = "Spanish (Honduras)"; break;
		case 0x13: name = "Spanish (Nicaragua)"; break;
		case 0x14: name = "Spanish (Puerto Rico)"; break;
		case 0x15: name = "Spanish (United States)"; break;
		}
		break;
	case 0x0b:
		switch (secondary) {
		case 0x00: name = "Finnish (Finland)"; break;
		case 0x01: name = "Finnish"; break;
		}
		break;
	case 0x0c:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "French (France)"; break;
		case 0x02: name = "French (Belgium)"; break;
		case 0x03: name = "French (Canada)"; break;
		case 0x04: name = "French (Switzerland)"; break;
		case 0x05: name = "French (Luxembourg)"; break;
		case 0x06: name = "French (Monaco)"; break;
		}
		break;
	case 0x0d:
		switch (secondary) {
		case 0x00: name = "Hebrew (Israel)"; break;
		case 0x01: name = "Hebrew"; break;
		}
		break;
	case 0x0e:
		switch (secondary) {
		case 0x00: name = "Hungarian (Hungary)"; break;
		case 0x01: name = "Hungarian"; break;
		}
		break;
	case 0x0f:
		switch (secondary) {
		case 0x00: name = "Icelandic (Iceland)"; break;
		case 0x01: name = "Icelandic"; break;
		}
		break;
	case 0x10:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "Italian (Italy)"; break;
		case 0x02: name = "Italian (Switzerland)"; break;
		}
		break;
	case 0x11:
		switch (secondary) {
		case 0x00: name = "Japanese (Japan)"; break;
		case 0x01: name = "Japanese"; break;
		}
		break;
	case 0x12:
		switch (secondary) {
		case 0x00: name = "Korean (Korea)"; break;
		case 0x01: name = "Korean"; break;
		}
		break;
	case 0x13:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "Dutch (Netherlands)"; break;
		case 0x02: name = "Dutch (Belgium)"; break;
		}
		break;
	case 0x14:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "Norwegian (Bokmal)"; break;
		case 0x02: name = "Norwegian (Nynorsk)"; break;
		}
		break;
	case 0x15:
		switch (secondary) {
		case 0x00: name = "Polish (Poland)"; break;
		case 0x01: name = "Polish"; break;
		}
		break;
	case 0x16:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "Portuguese (Brazil)"; break;
		case 0x02: name = "Portuguese (Portugal)"; break;
		}
		break;
	case 0x17:
		switch (secondary) {
		case 0x01: name = "Romansh (Switzerland)"; break;
		}
		break;
	case 0x18:
		switch (secondary) {
		case 0x00: name = "Romanian (Romania)"; break;
		case 0x01: name = "Romanian"; break;
		}
		break;
	case 0x19:
		switch (secondary) {
		case 0x00: name = "Russian (Russia)"; break;
		case 0x01: name = "Russian"; break;
		}
		break;
	case 0x1a:
		switch (secondary) {
		case 0x00: name = "Croatian (Croatia)"; break;
		case 0x01: name = "Croatian"; break;
		case 0x02: name = "Serbian (Latin)"; break;
		case 0x03: name = "Serbian (Cyrillic)"; break;
		case 0x04: name = "Croatian (Bosnia and Herzegovina)"; break;
		case 0x05: name = "Bosnian (Latin, Bosnia and Herzegovina)"; break;
		case 0x06: name = "Serbian (Latin, Bosnia and Herzegovina)"; break;
		case 0x07: name = "Serbian (Cyrillic, Bosnia and Herzegovina)"; break;
		case 0x08: name = "Bosnian (Cyrillic, Bosnia and Herzegovina)"; break;
		}
		break;
	case 0x1b:
		switch (secondary) {
		case 0x00: name = "Slovak (Slovakia)"; break;
		case 0x01: name = "Slovak"; break;
		}
		break;
	case 0x1c:
		switch (secondary) {
		case 0x00: name = "Albanian (Albania)"; break;
		case 0x01: name = "Albanian"; break;
		}
		break;
	case 0x1d:
		switch (secondary) {
		case 0x00: name = "Swedish (Sweden)"; break;
		case 0x01: name = "Swedish"; break;
		case 0x02: name = "Swedish (Finland)"; break;
		}
		break;
	case 0x1e:
		switch (secondary) {
		case 0x00: name = "Thai (Thailand)"; break;
		case 0x01: name = "Thai"; break;
		}
		break;
	case 0x1f:
		switch (secondary) {
		case 0x00: name = "Turkish (Turkey)"; break;
		case 0x01: name = "Turkish"; break;
		}
		break;
	case 0x20:
		switch (secondary) {
		case 0x00: name = "Urdu (Islamic Republic of Pakistan)"; break;
		case 0x01: name = "Urdu"; break;
		}
		break;
	case 0x21:
		switch (secondary) {
		case 0x00: name = "Indonesian (Indonesia)"; break;
		case 0x01: name = "Indonesian"; break;
		}
		break;
	case 0x22:
		switch (secondary) {
		case 0x00: name = "Ukrainian (Ukraine)"; break;
		case 0x01: name = "Ukrainian"; break;
		}
		break;
	case 0x23:
		switch (secondary) {
		case 0x00: name = "Belarusian (Belarus)"; break;
		case 0x01: name = "Belarusian"; break;
		}
		break;
	case 0x24:
		switch (secondary) {
		case 0x00: name = "Slovenian (Slovenia)"; break;
		case 0x01: name = "Slovenian"; break;
		}
		break;
	case 0x25:
		switch (secondary) {
		case 0x00: name = "Estonian (Estonia)"; break;
		case 0x01: name = "Estonian"; break;
		}
		break;
	case 0x26:
		switch (secondary) {
		case 0x00: name = "Latvian (Latvia)"; break;
		case 0x01: name = "Latvian"; break;
		}
		break;
	case 0x27:
		switch (secondary) {
		case 0x00: name = "Lithuanian (Lithuania)"; break;
		case 0x01: name = "Lithuanian"; break;
		}
		break;
	case 0x28:
		switch (secondary) {
		case 0x01: name = "Tajik (Tajikistan)"; break;
		}
		break;
	case 0x29:
		switch (secondary) {
		case 0x00: name = "Farsi (Iran)"; break;
		case 0x01: name = "Farsi"; break;
		}
		break;
	case 0x2a:
		switch (secondary) {
		case 0x00: name = "Vietnamese (Viet Nam)"; break;
		case 0x01: name = "Vietnamese"; break;
		}
		break;
	case 0x2b:
		switch (secondary) {
		case 0x00: name = "Armenian (Armenia)"; break;
		case 0x01: name = "Armenian"; break;
		}
		break;
	case 0x2c:
		switch (secondary) {
		case 0x00: name = "Azeri (Latin) (Azerbaijan)"; break;
		case 0x01: name = "Azeri (Latin)"; break;
		case 0x02: name = "Azeri (Cyrillic)"; break;
		}
		break;
	case 0x2d:
		switch (secondary) {
		case 0x00: name = "Basque (Spain)"; break;
		case 0x01: name = "Basque"; break;
		}
		break;
	case 0x2e:
		switch (secondary) {
		case 0x01: name = "Upper Sorbian (Germany)"; break;
		case 0x02: name = "Lower Sorbian (Germany)"; break;
		}
		break;
	case 0x2f:
		switch (secondary) {
		case 0x00: name = "FYRO Macedonian (Former Yugoslav Republic of Macedonia)"; break;
		case 0x01: name = "FYRO Macedonian"; break;
		}
		break;
	case 0x32:
		switch (secondary) {
		case 0x00: name = "Tswana (South Africa)"; break;
		case 0x01: name = "Tswana"; break;
		}
		break;
	case 0x34:
		switch (secondary) {
		case 0x00: name = "Xhosa (South Africa)"; break;
		case 0x01: name = "Xhosa"; break;
		}
		break;
	case 0x35:
		switch (secondary) {
		case 0x00: name = "Zulu (South Africa)"; break;
		case 0x01: name = "Zulu"; break;
		}
		break;
	case 0x36:
		switch (secondary) {
		case 0x00: name = "Afrikaans (South Africa)"; break;
		case 0x01: name = "Afrikaans"; break;
		}
		break;
	case 0x37:
		switch (secondary) {
		case 0x00: name = "Georgian (Georgia)"; break;
		case 0x01: name = "Georgian"; break;
		}
		break;
	case 0x38:
		switch (secondary) {
		case 0x00: name = "Faroese (Faroe Islands)"; break;
		case 0x01: name = "Faroese"; break;
		}
		break;
	case 0x39:
		switch (secondary) {
		case 0x00: name = "Hindi (India)"; break;
		case 0x01: name = "Hindi"; break;
		}
		break;
	case 0x3a:
		switch (secondary) {
		case 0x00: name = "Maltese (Malta)"; break;
		case 0x01: name = "Maltese"; break;
		}
		break;
	case 0x3b:
		switch (secondary) {
		case 0x00: name = "Sami (Northern) (Norway)"; break;
		case 0x01: name = "Sami, Northern (Norway)"; break;
		case 0x02: name = "Sami, Northern (Sweden)"; break;
		case 0x03: name = "Sami, Northern (Finland)"; break;
		case 0x04: name = "Sami, Lule (Norway)"; break;
		case 0x05: name = "Sami, Lule (Sweden)"; break;
		case 0x06: name = "Sami, Southern (Norway)"; break;
		case 0x07: name = "Sami, Southern (Sweden)"; break;
		case 0x08: name = "Sami, Skolt (Finland)"; break;
		case 0x09: name = "Sami, Inari (Finland)"; break;
		}
		break;
	case 0x3c:
		switch (secondary) {
		case 0x02: name = "Irish (Ireland)"; break;
		}
		break;
	case 0x3e:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "Malay (Malaysia)"; break;
		case 0x02: name = "Malay (Brunei Darussalam)"; break;
		}
		break;
	case 0x3f:
		switch (secondary) {
		case 0x00: name = "Kazakh (Kazakhstan)"; break;
		case 0x01: name = "Kazakh"; break;
		}
		break;
	case 0x40:
		switch (secondary) {
		case 0x00: name = "Kyrgyz (Kyrgyzstan)"; break;
		case 0x01: name = "Kyrgyz (Cyrillic)"; break;
		}
		break;
	case 0x41:
		switch (secondary) {
		case 0x00: name = "Swahili (Kenya)"; break;
		case 0x01: name = "Swahili"; break;
		}
		break;
	case 0x42:
		switch (secondary) {
		case 0x01: name = "Turkmen (Turkmenistan)"; break;
		}
		break;
	case 0x43:
		switch (secondary) {
		case 0x00: name = "Uzbek (Latin) (Uzbekistan)"; break;
		case 0x01: name = "Uzbek (Latin)"; break;
		case 0x02: name = "Uzbek (Cyrillic)"; break;
		}
		break;
	case 0x44:
		switch (secondary) {
		case 0x00: name = "Tatar (Russia)"; break;
		case 0x01: name = "Tatar"; break;
		}
		break;
	case 0x45:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "Bengali (India)"; break;
		}
		break;
	case 0x46:
		switch (secondary) {
		case 0x00: name = "Punjabi (India)"; break;
		case 0x01: name = "Punjabi"; break;
		}
		break;
	case 0x47:
		switch (secondary) {
		case 0x00: name = "Gujarati (India)"; break;
		case 0x01: name = "Gujarati"; break;
		}
		break;
	case 0x49:
		switch (secondary) {
		case 0x00: name = "Tamil (India)"; break;
		case 0x01: name = "Tamil"; break;
		}
		break;
	case 0x4a:
		switch (secondary) {
		case 0x00: name = "Telugu (India)"; break;
		case 0x01: name = "Telugu"; break;
		}
		break;
	case 0x4b:
		switch (secondary) {
		case 0x00: name = "Kannada (India)"; break;
		case 0x01: name = "Kannada"; break;
		}
		break;
	case 0x4c:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "Malayalam (India)"; break;
		}
		break;
	case 0x4d:
		switch (secondary) {
		case 0x01: name = "Assamese (India)"; break;
		}
		break;
	case 0x4e:
		switch (secondary) {
		case 0x00: name = "Marathi (India)"; break;
		case 0x01: name = "Marathi"; break;
		}
		break;
	case 0x4f:
		switch (secondary) {
		case 0x00: name = "Sanskrit (India)"; break;
		case 0x01: name = "Sanskrit"; break;
		}
		break;
	case 0x50:
		switch (secondary) {
		case 0x00: name = "Mongolian (Mongolia)"; break;
		case 0x01: name = "Mongolian (Cyrillic)"; break;
		case 0x02: name = "Mongolian (PRC)"; break;
		}
		break;
	case 0x51:
		switch (secondary) {
		case 0x01: name = "Tibetan (PRC)"; break;
		case 0x02: name = "Tibetan (Bhutan)"; break;
		}
		break;
	case 0x52:
		switch (secondary) {
		case 0x00: name = "Welsh (United Kingdom)"; break;
		case 0x01: name = "Welsh"; break;
		}
		break;
	case 0x53:
		switch (secondary) {
		case 0x01: name = "Khmer (Cambodia)"; break;
		}
		break;
	case 0x54:
		switch (secondary) {
		case 0x01: name = "Lao (Lao PDR)"; break;
		}
		break;
	case 0x56:
		switch (secondary) {
		case 0x00: name = "Galician (Spain)"; break;
		case 0x01: name = "Galician"; break;
		}
		break;
	case 0x57:
		switch (secondary) {
		case 0x00: name = "Konkani (India)"; break;
		case 0x01: name = "Konkani"; break;
		}
		break;
	case 0x5a:
		switch (secondary) {
		case 0x00: name = "Syriac (Syria)"; break;
		case 0x01: name = "Syriac"; break;
		}
		break;
	case 0x5b:
		switch (secondary) {
		case 0x01: name = "Sinhala (Sri Lanka)"; break;
		}
		break;
	case 0x5d:
		switch (secondary) {
		case 0x01: name = "Inuktitut (Syllabics, Canada)"; break;
		case 0x02: name = "Inuktitut (Latin, Canada)"; break;
		}
		break;
	case 0x5e:
		switch (secondary) {
		case 0x01: name = "Amharic (Ethiopia)"; break;
		}
		break;
	case 0x5f:
		switch (secondary) {
		case 0x02: name = "Tamazight (Algeria, Latin)"; break;
		}
		break;
	case 0x61:
		switch (secondary) {
		case 0x01: name = "Nepali (Nepal)"; break;
		}
		break;
	case 0x62:
		switch (secondary) {
		case 0x01: name = "Frisian (Netherlands)"; break;
		}
		break;
	case 0x63:
		switch (secondary) {
		case 0x01: name = "Pashto (Afghanistan)"; break;
		}
		break;
	case 0x64:
		switch (secondary) {
		case 0x01: name = "Filipino (Philippines)"; break;
		}
		break;
	case 0x65:
		switch (secondary) {
		case 0x00: name = "Divehi (Maldives)"; break;
		case 0x01: name = "Divehi"; break;
		}
		break;
	case 0x68:
		switch (secondary) {
		case 0x01: name = "Hausa (Nigeria, Latin)"; break;
		}
		break;
	case 0x6a:
		switch (secondary) {
		case 0x01: name = "Yoruba (Nigeria)"; break;
		}
		break;
	case 0x6b:
		switch (secondary) {
		case 0x00:
		case 0x01: name = "Quechua (Bolivia)"; break;
		case 0x02: name = "Quechua (Ecuador)"; break;
		case 0x03: name = "Quechua (Peru)"; break;
		}
		break;
	case 0x6c:
		switch (secondary) {
		case 0x00: name = "Northern Sotho (South Africa)"; break;
		case 0x01: name = "Northern Sotho"; break;
		}
		break;
	case 0x6d:
		switch (secondary) {
		case 0x01: name = "Bashkir (Russia)"; break;
		}
		break;
	case 0x6e:
		switch (secondary) {
		case 0x01: name = "Luxembourgish (Luxembourg)"; break;
		}
		break;
	case 0x6f:
		switch (secondary) {
		case 0x01: name = "Greenlandic (Greenland)"; break;
		}
		break;
	case 0x78:
		switch (secondary) {
		case 0x01: name = "Yi (PRC)"; break;
		}
		break;
	case 0x7a:
		switch (secondary) {
		case 0x01: name = "Mapudungun (Chile)"; break;
		}
		break;
	case 0x7c:
		switch (secondary) {
		case 0x01: name = "Mohawk (Mohawk)"; break;
		}
		break;
	case 0x7e:
		switch (secondary) {
		case 0x01: name = "Breton (France)"; break;
		}
		break;
	case 0x7f:
		switch (secondary) {
		case 0x00: name = "Invariant Language (Invariant Country)"; break;
		}
		break;
	case 0x80:
		switch (secondary) {
		case 0x01: name = "Uighur (PRC)"; break;
		}
		break;
	case 0x81:
		switch (secondary) {
		case 0x00: name = "Maori (New Zealand)"; break;
		case 0x01: name = "Maori"; break;
		}
		break;
	case 0x83:
		switch (secondary) {
		case 0x01: name = "Corsican (France)"; break;
		}
		break;
	case 0x84:
		switch (secondary) {
		case 0x01: name = "Alsatian (France)"; break;
		}
		break;
	case 0x85:
		switch (secondary) {
		case 0x01: name = "Yakut (Russia)"; break;
		}
		break;
	case 0x86:
		switch (secondary) {
		case 0x01: name = "K'iche (Guatemala)"; break;
		}
		break;
	case 0x87:
		switch (secondary) {
		case 0x01: name = "Kinyarwanda (Rwanda)"; break;
		}
		break;
	case 0x88:
		switch (secondary) {
		case 0x01: name = "Wolof (Senegal)"; break;
		}
		break;
	case 0x8c:
		switch (secondary) {
		case 0x01: name = "Dari (Afghanistan)"; break;
		}
		break;

	default:
		name = "Language Neutral";

	}

	if (!name)
		name = "Language Neutral";

	return copy_lang (lang_out, lang_len, name);
}

#endif /* ENABLE_NETCORE && DISABLE_PROCESSES */

MONO_EMPTY_SOURCE_FILE (culture_w32_process_unix_language);
