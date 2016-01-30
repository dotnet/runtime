// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;

namespace XmlCoreTest.Common
{
    [Flags]
    public enum CharType
    {
        None = 0x00,

        XmlChar = 0x01,
        SurrogateHighChar = 0x02,
        SurrogateLowChar = 0x04,

        WhiteSpace = 0x08,

        NameStartChar = 0x10,
        NameChar = 0x20,

        NCNameStartChar = 0x40,
        NCNameChar = 0x80,

        NameStartSurrogateHighChar = 0x100,
        NameStartSurrogateLowChar = 0x200,
        NameSurrogateHighChar = 0x400,
        NameSurrogateLowChar = 0x800,

        PubidChar = 0x1000
    }

    public class UnicodeCharHelper
    {
        private static CharType[] s_charTypeTable = new CharType[0x10000];
        private static CharType _currentCharType = CharType.None;
        static UnicodeCharHelper()
        {
            InitializeFourthEditionCharTypeTable();
        }
        //#region Fourth Edition Table
        static CharType CurrentCharType
        {
            get { return _currentCharType; }
            set { _currentCharType = value; }
        }
        static void addCharRange(char[] charrange)
        {
            if (charrange == null)
                return;
            if (charrange.Length != 2)
                return;

            for (char c = charrange[0]; c <= charrange[1]; ++c)
            {
                s_charTypeTable[c] |= CurrentCharType;
            }
        }
        private static void InitializeFourthEditionCharTypeTable()
        {
            CurrentCharType = CharType.XmlChar;// ranges
            {
                addCharRange(new char[] { '\u0009', '\u000a' }); addCharRange(new char[] { '\u000d', '\u000d' }); addCharRange(new char[] { '\u0020', '\ud7ff' }); addCharRange(new char[] { '\ue000', '\ufffd' });
            }
            CurrentCharType = CharType.SurrogateHighChar;// ranges
            {
                addCharRange(new char[] { '\ud800', '\udbff' });
            }
            CurrentCharType = CharType.SurrogateLowChar;// ranges
            {
                addCharRange(new char[] { '\udc00', '\udfff' });
            }
            //[3]    S    ::=    (#x20 | #x9 | #xD | #xA)+ 
            CurrentCharType = CharType.WhiteSpace;// ranges
            {
                addCharRange(new char[] { '\u0009', '\u000a' }); addCharRange(new char[] { '\u000d', '\u000d' }); addCharRange(new char[] { '\u0020', '\u0020' });
            }
            //[5]    Name    ::=    (Letter | '_' | ':') ...
            //[84]   Letter  ::=    BaseChar | Ideographic 
            CurrentCharType = CharType.NameStartChar;// ranges
            {
                addCharRange(new char[] { ':', ':' }); addCharRange(new char[] { '_', '_' });
                // BaseChar
                addCharRange(new char[] { '\u0041', '\u005A' }); addCharRange(new char[] { '\u0061', '\u007A' }); addCharRange(new char[] { '\u00C0', '\u00D6' }); addCharRange(new char[] { '\u00D8', '\u00F6' }); addCharRange(new char[] { '\u00F8', '\u00FF' }); addCharRange(new char[] { '\u0100', '\u0131' }); addCharRange(new char[] { '\u0134', '\u013E' }); addCharRange(new char[] { '\u0141', '\u0148' });
                addCharRange(new char[] { '\u014A', '\u017E' }); addCharRange(new char[] { '\u0180', '\u01C3' }); addCharRange(new char[] { '\u01CD', '\u01F0' }); addCharRange(new char[] { '\u01F4', '\u01F5' }); addCharRange(new char[] { '\u01FA', '\u0217' }); addCharRange(new char[] { '\u0250', '\u02A8' }); addCharRange(new char[] { '\u02BB', '\u02C1' }); addCharRange(new char[] { '\u0386', '\u0386' });
                addCharRange(new char[] { '\u0388', '\u038A' }); addCharRange(new char[] { '\u038C', '\u038C' }); addCharRange(new char[] { '\u038E', '\u03A1' }); addCharRange(new char[] { '\u03A3', '\u03CE' }); addCharRange(new char[] { '\u03D0', '\u03D6' }); addCharRange(new char[] { '\u03DA', '\u03DA' }); addCharRange(new char[] { '\u03DC', '\u03DC' }); addCharRange(new char[] { '\u03DE', '\u03DE' });
                addCharRange(new char[] { '\u03E0', '\u03E0' }); addCharRange(new char[] { '\u03E2', '\u03F3' }); addCharRange(new char[] { '\u0401', '\u040C' }); addCharRange(new char[] { '\u040E', '\u044F' }); addCharRange(new char[] { '\u0451', '\u045C' }); addCharRange(new char[] { '\u045E', '\u0481' }); addCharRange(new char[] { '\u0490', '\u04C4' }); addCharRange(new char[] { '\u04C7', '\u04C8' });
                addCharRange(new char[] { '\u04CB', '\u04CC' }); addCharRange(new char[] { '\u04D0', '\u04EB' }); addCharRange(new char[] { '\u04EE', '\u04F5' }); addCharRange(new char[] { '\u04F8', '\u04F9' }); addCharRange(new char[] { '\u0531', '\u0556' }); addCharRange(new char[] { '\u0559', '\u0559' }); addCharRange(new char[] { '\u0561', '\u0586' }); addCharRange(new char[] { '\u05D0', '\u05EA' });
                addCharRange(new char[] { '\u05F0', '\u05F2' }); addCharRange(new char[] { '\u0621', '\u063A' }); addCharRange(new char[] { '\u0641', '\u064A' }); addCharRange(new char[] { '\u0671', '\u06B7' }); addCharRange(new char[] { '\u06BA', '\u06BE' }); addCharRange(new char[] { '\u06C0', '\u06CE' }); addCharRange(new char[] { '\u06D0', '\u06D3' }); addCharRange(new char[] { '\u06D5', '\u06D5' });
                addCharRange(new char[] { '\u06E5', '\u06E6' }); addCharRange(new char[] { '\u0905', '\u0939' }); addCharRange(new char[] { '\u093D', '\u093D' }); addCharRange(new char[] { '\u0958', '\u0961' }); addCharRange(new char[] { '\u0985', '\u098C' }); addCharRange(new char[] { '\u098F', '\u0990' }); addCharRange(new char[] { '\u0993', '\u09A8' }); addCharRange(new char[] { '\u09AA', '\u09B0' });
                addCharRange(new char[] { '\u09B2', '\u09B2' }); addCharRange(new char[] { '\u09B6', '\u09B9' }); addCharRange(new char[] { '\u09DC', '\u09DD' }); addCharRange(new char[] { '\u09DF', '\u09E1' }); addCharRange(new char[] { '\u09F0', '\u09F1' }); addCharRange(new char[] { '\u0A05', '\u0A0A' }); addCharRange(new char[] { '\u0A0F', '\u0A10' }); addCharRange(new char[] { '\u0A13', '\u0A28' });
                addCharRange(new char[] { '\u0A2A', '\u0A30' }); addCharRange(new char[] { '\u0A32', '\u0A33' }); addCharRange(new char[] { '\u0A35', '\u0A36' }); addCharRange(new char[] { '\u0A38', '\u0A39' }); addCharRange(new char[] { '\u0A59', '\u0A5C' }); addCharRange(new char[] { '\u0A5E', '\u0A5E' }); addCharRange(new char[] { '\u0A72', '\u0A74' }); addCharRange(new char[] { '\u0A85', '\u0A8B' });
                addCharRange(new char[] { '\u0A8D', '\u0A8D' }); addCharRange(new char[] { '\u0A8F', '\u0A91' }); addCharRange(new char[] { '\u0A93', '\u0AA8' }); addCharRange(new char[] { '\u0AAA', '\u0AB0' }); addCharRange(new char[] { '\u0AB2', '\u0AB3' }); addCharRange(new char[] { '\u0AB5', '\u0AB9' }); addCharRange(new char[] { '\u0ABD', '\u0ABD' }); addCharRange(new char[] { '\u0AE0', '\u0AE0' });
                addCharRange(new char[] { '\u0B05', '\u0B0C' }); addCharRange(new char[] { '\u0B0F', '\u0B10' }); addCharRange(new char[] { '\u0B13', '\u0B28' }); addCharRange(new char[] { '\u0B2A', '\u0B30' }); addCharRange(new char[] { '\u0B32', '\u0B33' }); addCharRange(new char[] { '\u0B36', '\u0B39' }); addCharRange(new char[] { '\u0B3D', '\u0B3D' }); addCharRange(new char[] { '\u0B5C', '\u0B5D' });
                addCharRange(new char[] { '\u0B5F', '\u0B61' }); addCharRange(new char[] { '\u0B85', '\u0B8A' }); addCharRange(new char[] { '\u0B8E', '\u0B90' }); addCharRange(new char[] { '\u0B92', '\u0B95' }); addCharRange(new char[] { '\u0B99', '\u0B9A' }); addCharRange(new char[] { '\u0B9C', '\u0B9C' }); addCharRange(new char[] { '\u0B9E', '\u0B9F' }); addCharRange(new char[] { '\u0BA3', '\u0BA4' });
                addCharRange(new char[] { '\u0BA8', '\u0BAA' }); addCharRange(new char[] { '\u0BAE', '\u0BB5' }); addCharRange(new char[] { '\u0BB7', '\u0BB9' }); addCharRange(new char[] { '\u0C05', '\u0C0C' }); addCharRange(new char[] { '\u0C0E', '\u0C10' }); addCharRange(new char[] { '\u0C12', '\u0C28' }); addCharRange(new char[] { '\u0C2A', '\u0C33' }); addCharRange(new char[] { '\u0C35', '\u0C39' });
                addCharRange(new char[] { '\u0C60', '\u0C61' }); addCharRange(new char[] { '\u0C85', '\u0C8C' }); addCharRange(new char[] { '\u0C8E', '\u0C90' }); addCharRange(new char[] { '\u0C92', '\u0CA8' }); addCharRange(new char[] { '\u0CAA', '\u0CB3' }); addCharRange(new char[] { '\u0CB5', '\u0CB9' }); addCharRange(new char[] { '\u0CDE', '\u0CDE' }); addCharRange(new char[] { '\u0CE0', '\u0CE1' });
                addCharRange(new char[] { '\u0D05', '\u0D0C' }); addCharRange(new char[] { '\u0D0E', '\u0D10' }); addCharRange(new char[] { '\u0D12', '\u0D28' }); addCharRange(new char[] { '\u0D2A', '\u0D39' }); addCharRange(new char[] { '\u0D60', '\u0D61' }); addCharRange(new char[] { '\u0E01', '\u0E2E' }); addCharRange(new char[] { '\u0E30', '\u0E30' }); addCharRange(new char[] { '\u0E32', '\u0E33' });
                addCharRange(new char[] { '\u0E40', '\u0E45' }); addCharRange(new char[] { '\u0E81', '\u0E82' }); addCharRange(new char[] { '\u0E84', '\u0E84' }); addCharRange(new char[] { '\u0E87', '\u0E88' }); addCharRange(new char[] { '\u0E8A', '\u0E8A' }); addCharRange(new char[] { '\u0E8D', '\u0E8D' }); addCharRange(new char[] { '\u0E94', '\u0E97' }); addCharRange(new char[] { '\u0E99', '\u0E9F' });
                addCharRange(new char[] { '\u0EA1', '\u0EA3' }); addCharRange(new char[] { '\u0EA5', '\u0EA5' }); addCharRange(new char[] { '\u0EA7', '\u0EA7' }); addCharRange(new char[] { '\u0EAA', '\u0EAB' }); addCharRange(new char[] { '\u0EAD', '\u0EAE' }); addCharRange(new char[] { '\u0EB0', '\u0EB0' }); addCharRange(new char[] { '\u0EB2', '\u0EB3' }); addCharRange(new char[] { '\u0EBD', '\u0EBD' });
                addCharRange(new char[] { '\u0EC0', '\u0EC4' }); addCharRange(new char[] { '\u0F40', '\u0F47' }); addCharRange(new char[] { '\u0F49', '\u0F69' }); addCharRange(new char[] { '\u10A0', '\u10C5' }); addCharRange(new char[] { '\u10D0', '\u10F6' }); addCharRange(new char[] { '\u1100', '\u1100' }); addCharRange(new char[] { '\u1102', '\u1103' }); addCharRange(new char[] { '\u1105', '\u1107' });
                addCharRange(new char[] { '\u1109', '\u1109' }); addCharRange(new char[] { '\u110B', '\u110C' }); addCharRange(new char[] { '\u110E', '\u1112' }); addCharRange(new char[] { '\u113C', '\u113C' }); addCharRange(new char[] { '\u113E', '\u113E' }); addCharRange(new char[] { '\u1140', '\u1140' }); addCharRange(new char[] { '\u114C', '\u114C' }); addCharRange(new char[] { '\u114E', '\u114E' });
                addCharRange(new char[] { '\u1150', '\u1150' }); addCharRange(new char[] { '\u1154', '\u1155' }); addCharRange(new char[] { '\u1159', '\u1159' }); addCharRange(new char[] { '\u115F', '\u1161' }); addCharRange(new char[] { '\u1163', '\u1163' }); addCharRange(new char[] { '\u1165', '\u1165' }); addCharRange(new char[] { '\u1167', '\u1167' }); addCharRange(new char[] { '\u1169', '\u1169' });
                addCharRange(new char[] { '\u116D', '\u116E' }); addCharRange(new char[] { '\u1172', '\u1173' }); addCharRange(new char[] { '\u1175', '\u1175' }); addCharRange(new char[] { '\u119E', '\u119E' }); addCharRange(new char[] { '\u11A8', '\u11A8' }); addCharRange(new char[] { '\u11AB', '\u11AB' }); addCharRange(new char[] { '\u11AE', '\u11AF' }); addCharRange(new char[] { '\u11B7', '\u11B8' });
                addCharRange(new char[] { '\u11BA', '\u11BA' }); addCharRange(new char[] { '\u11BC', '\u11C2' }); addCharRange(new char[] { '\u11EB', '\u11EB' }); addCharRange(new char[] { '\u11F0', '\u11F0' }); addCharRange(new char[] { '\u11F9', '\u11F9' }); addCharRange(new char[] { '\u1E00', '\u1E9B' }); addCharRange(new char[] { '\u1EA0', '\u1EF9' }); addCharRange(new char[] { '\u1F00', '\u1F15' });
                addCharRange(new char[] { '\u1F18', '\u1F1D' }); addCharRange(new char[] { '\u1F20', '\u1F45' }); addCharRange(new char[] { '\u1F48', '\u1F4D' }); addCharRange(new char[] { '\u1F50', '\u1F57' }); addCharRange(new char[] { '\u1F59', '\u1F59' }); addCharRange(new char[] { '\u1F5B', '\u1F5B' }); addCharRange(new char[] { '\u1F5D', '\u1F5D' }); addCharRange(new char[] { '\u1F5F', '\u1F7D' });
                addCharRange(new char[] { '\u1F80', '\u1FB4' }); addCharRange(new char[] { '\u1FB6', '\u1FBC' }); addCharRange(new char[] { '\u1FBE', '\u1FBE' }); addCharRange(new char[] { '\u1FC2', '\u1FC4' }); addCharRange(new char[] { '\u1FC6', '\u1FCC' }); addCharRange(new char[] { '\u1FD0', '\u1FD3' }); addCharRange(new char[] { '\u1FD6', '\u1FDB' }); addCharRange(new char[] { '\u1FE0', '\u1FEC' });
                addCharRange(new char[] { '\u1FF2', '\u1FF4' }); addCharRange(new char[] { '\u1FF6', '\u1FFC' }); addCharRange(new char[] { '\u2126', '\u2126' }); addCharRange(new char[] { '\u212A', '\u212B' }); addCharRange(new char[] { '\u212E', '\u212E' }); addCharRange(new char[] { '\u2180', '\u2182' }); addCharRange(new char[] { '\u3041', '\u3094' }); addCharRange(new char[] { '\u30A1', '\u30FA' });
                addCharRange(new char[] { '\u3105', '\u312C' }); addCharRange(new char[] { '\uAC00', '\uD7A3' });
                //Ideographic 
                addCharRange(new char[] { '\u3007', '\u3007' }); addCharRange(new char[] { '\u3021', '\u3029' }); addCharRange(new char[] { '\u4E00', '\u9FA5' });
            }
            //[4]    NameChar  ::=    Letter | Digit | '.' | '-' | '_' | ':' | CombiningChar | Extender 
            //[84]   Letter    ::=    BaseChar | Ideographic 
            CurrentCharType = CharType.NameChar;// ranges
            {
                addCharRange(new char[] { ':', ':' }); addCharRange(new char[] { '_', '_' }); addCharRange(new char[] { '-', '-' }); addCharRange(new char[] { '.', '.' });
                // BaseChar
                addCharRange(new char[] { '\u0041', '\u005A' }); addCharRange(new char[] { '\u0061', '\u007A' }); addCharRange(new char[] { '\u00C0', '\u00D6' }); addCharRange(new char[] { '\u00D8', '\u00F6' }); addCharRange(new char[] { '\u00F8', '\u00FF' }); addCharRange(new char[] { '\u0100', '\u0131' }); addCharRange(new char[] { '\u0134', '\u013E' }); addCharRange(new char[] { '\u0141', '\u0148' });
                addCharRange(new char[] { '\u014A', '\u017E' }); addCharRange(new char[] { '\u0180', '\u01C3' }); addCharRange(new char[] { '\u01CD', '\u01F0' }); addCharRange(new char[] { '\u01F4', '\u01F5' }); addCharRange(new char[] { '\u01FA', '\u0217' }); addCharRange(new char[] { '\u0250', '\u02A8' }); addCharRange(new char[] { '\u02BB', '\u02C1' }); addCharRange(new char[] { '\u0386', '\u0386' });
                addCharRange(new char[] { '\u0388', '\u038A' }); addCharRange(new char[] { '\u038C', '\u038C' }); addCharRange(new char[] { '\u038E', '\u03A1' }); addCharRange(new char[] { '\u03A3', '\u03CE' }); addCharRange(new char[] { '\u03D0', '\u03D6' }); addCharRange(new char[] { '\u03DA', '\u03DA' }); addCharRange(new char[] { '\u03DC', '\u03DC' }); addCharRange(new char[] { '\u03DE', '\u03DE' });
                addCharRange(new char[] { '\u03E0', '\u03E0' }); addCharRange(new char[] { '\u03E2', '\u03F3' }); addCharRange(new char[] { '\u0401', '\u040C' }); addCharRange(new char[] { '\u040E', '\u044F' }); addCharRange(new char[] { '\u0451', '\u045C' }); addCharRange(new char[] { '\u045E', '\u0481' }); addCharRange(new char[] { '\u0490', '\u04C4' }); addCharRange(new char[] { '\u04C7', '\u04C8' });
                addCharRange(new char[] { '\u04CB', '\u04CC' }); addCharRange(new char[] { '\u04D0', '\u04EB' }); addCharRange(new char[] { '\u04EE', '\u04F5' }); addCharRange(new char[] { '\u04F8', '\u04F9' }); addCharRange(new char[] { '\u0531', '\u0556' }); addCharRange(new char[] { '\u0559', '\u0559' }); addCharRange(new char[] { '\u0561', '\u0586' }); addCharRange(new char[] { '\u05D0', '\u05EA' });
                addCharRange(new char[] { '\u05F0', '\u05F2' }); addCharRange(new char[] { '\u0621', '\u063A' }); addCharRange(new char[] { '\u0641', '\u064A' }); addCharRange(new char[] { '\u0671', '\u06B7' }); addCharRange(new char[] { '\u06BA', '\u06BE' }); addCharRange(new char[] { '\u06C0', '\u06CE' }); addCharRange(new char[] { '\u06D0', '\u06D3' }); addCharRange(new char[] { '\u06D5', '\u06D5' });
                addCharRange(new char[] { '\u06E5', '\u06E6' }); addCharRange(new char[] { '\u0905', '\u0939' }); addCharRange(new char[] { '\u093D', '\u093D' }); addCharRange(new char[] { '\u0958', '\u0961' }); addCharRange(new char[] { '\u0985', '\u098C' }); addCharRange(new char[] { '\u098F', '\u0990' }); addCharRange(new char[] { '\u0993', '\u09A8' }); addCharRange(new char[] { '\u09AA', '\u09B0' });
                addCharRange(new char[] { '\u09B2', '\u09B2' }); addCharRange(new char[] { '\u09B6', '\u09B9' }); addCharRange(new char[] { '\u09DC', '\u09DD' }); addCharRange(new char[] { '\u09DF', '\u09E1' }); addCharRange(new char[] { '\u09F0', '\u09F1' }); addCharRange(new char[] { '\u0A05', '\u0A0A' }); addCharRange(new char[] { '\u0A0F', '\u0A10' }); addCharRange(new char[] { '\u0A13', '\u0A28' });
                addCharRange(new char[] { '\u0A2A', '\u0A30' }); addCharRange(new char[] { '\u0A32', '\u0A33' }); addCharRange(new char[] { '\u0A35', '\u0A36' }); addCharRange(new char[] { '\u0A38', '\u0A39' }); addCharRange(new char[] { '\u0A59', '\u0A5C' }); addCharRange(new char[] { '\u0A5E', '\u0A5E' }); addCharRange(new char[] { '\u0A72', '\u0A74' }); addCharRange(new char[] { '\u0A85', '\u0A8B' });
                addCharRange(new char[] { '\u0A8D', '\u0A8D' }); addCharRange(new char[] { '\u0A8F', '\u0A91' }); addCharRange(new char[] { '\u0A93', '\u0AA8' }); addCharRange(new char[] { '\u0AAA', '\u0AB0' }); addCharRange(new char[] { '\u0AB2', '\u0AB3' }); addCharRange(new char[] { '\u0AB5', '\u0AB9' }); addCharRange(new char[] { '\u0ABD', '\u0ABD' }); addCharRange(new char[] { '\u0AE0', '\u0AE0' });
                addCharRange(new char[] { '\u0B05', '\u0B0C' }); addCharRange(new char[] { '\u0B0F', '\u0B10' }); addCharRange(new char[] { '\u0B13', '\u0B28' }); addCharRange(new char[] { '\u0B2A', '\u0B30' }); addCharRange(new char[] { '\u0B32', '\u0B33' }); addCharRange(new char[] { '\u0B36', '\u0B39' }); addCharRange(new char[] { '\u0B3D', '\u0B3D' }); addCharRange(new char[] { '\u0B5C', '\u0B5D' });
                addCharRange(new char[] { '\u0B5F', '\u0B61' }); addCharRange(new char[] { '\u0B85', '\u0B8A' }); addCharRange(new char[] { '\u0B8E', '\u0B90' }); addCharRange(new char[] { '\u0B92', '\u0B95' }); addCharRange(new char[] { '\u0B99', '\u0B9A' }); addCharRange(new char[] { '\u0B9C', '\u0B9C' }); addCharRange(new char[] { '\u0B9E', '\u0B9F' }); addCharRange(new char[] { '\u0BA3', '\u0BA4' });
                addCharRange(new char[] { '\u0BA8', '\u0BAA' }); addCharRange(new char[] { '\u0BAE', '\u0BB5' }); addCharRange(new char[] { '\u0BB7', '\u0BB9' }); addCharRange(new char[] { '\u0C05', '\u0C0C' }); addCharRange(new char[] { '\u0C0E', '\u0C10' }); addCharRange(new char[] { '\u0C12', '\u0C28' }); addCharRange(new char[] { '\u0C2A', '\u0C33' }); addCharRange(new char[] { '\u0C35', '\u0C39' });
                addCharRange(new char[] { '\u0C60', '\u0C61' }); addCharRange(new char[] { '\u0C85', '\u0C8C' }); addCharRange(new char[] { '\u0C8E', '\u0C90' }); addCharRange(new char[] { '\u0C92', '\u0CA8' }); addCharRange(new char[] { '\u0CAA', '\u0CB3' }); addCharRange(new char[] { '\u0CB5', '\u0CB9' }); addCharRange(new char[] { '\u0CDE', '\u0CDE' }); addCharRange(new char[] { '\u0CE0', '\u0CE1' });
                addCharRange(new char[] { '\u0D05', '\u0D0C' }); addCharRange(new char[] { '\u0D0E', '\u0D10' }); addCharRange(new char[] { '\u0D12', '\u0D28' }); addCharRange(new char[] { '\u0D2A', '\u0D39' }); addCharRange(new char[] { '\u0D60', '\u0D61' }); addCharRange(new char[] { '\u0E01', '\u0E2E' }); addCharRange(new char[] { '\u0E30', '\u0E30' }); addCharRange(new char[] { '\u0E32', '\u0E33' });
                addCharRange(new char[] { '\u0E40', '\u0E45' }); addCharRange(new char[] { '\u0E81', '\u0E82' }); addCharRange(new char[] { '\u0E84', '\u0E84' }); addCharRange(new char[] { '\u0E87', '\u0E88' }); addCharRange(new char[] { '\u0E8A', '\u0E8A' }); addCharRange(new char[] { '\u0E8D', '\u0E8D' }); addCharRange(new char[] { '\u0E94', '\u0E97' }); addCharRange(new char[] { '\u0E99', '\u0E9F' });
                addCharRange(new char[] { '\u0EA1', '\u0EA3' }); addCharRange(new char[] { '\u0EA5', '\u0EA5' }); addCharRange(new char[] { '\u0EA7', '\u0EA7' }); addCharRange(new char[] { '\u0EAA', '\u0EAB' }); addCharRange(new char[] { '\u0EAD', '\u0EAE' }); addCharRange(new char[] { '\u0EB0', '\u0EB0' }); addCharRange(new char[] { '\u0EB2', '\u0EB3' }); addCharRange(new char[] { '\u0EBD', '\u0EBD' });
                addCharRange(new char[] { '\u0EC0', '\u0EC4' }); addCharRange(new char[] { '\u0F40', '\u0F47' }); addCharRange(new char[] { '\u0F49', '\u0F69' }); addCharRange(new char[] { '\u10A0', '\u10C5' }); addCharRange(new char[] { '\u10D0', '\u10F6' }); addCharRange(new char[] { '\u1100', '\u1100' }); addCharRange(new char[] { '\u1102', '\u1103' }); addCharRange(new char[] { '\u1105', '\u1107' });
                addCharRange(new char[] { '\u1109', '\u1109' }); addCharRange(new char[] { '\u110B', '\u110C' }); addCharRange(new char[] { '\u110E', '\u1112' }); addCharRange(new char[] { '\u113C', '\u113C' }); addCharRange(new char[] { '\u113E', '\u113E' }); addCharRange(new char[] { '\u1140', '\u1140' }); addCharRange(new char[] { '\u114C', '\u114C' }); addCharRange(new char[] { '\u114E', '\u114E' });
                addCharRange(new char[] { '\u1150', '\u1150' }); addCharRange(new char[] { '\u1154', '\u1155' }); addCharRange(new char[] { '\u1159', '\u1159' }); addCharRange(new char[] { '\u115F', '\u1161' }); addCharRange(new char[] { '\u1163', '\u1163' }); addCharRange(new char[] { '\u1165', '\u1165' }); addCharRange(new char[] { '\u1167', '\u1167' }); addCharRange(new char[] { '\u1169', '\u1169' });
                addCharRange(new char[] { '\u116D', '\u116E' }); addCharRange(new char[] { '\u1172', '\u1173' }); addCharRange(new char[] { '\u1175', '\u1175' }); addCharRange(new char[] { '\u119E', '\u119E' }); addCharRange(new char[] { '\u11A8', '\u11A8' }); addCharRange(new char[] { '\u11AB', '\u11AB' }); addCharRange(new char[] { '\u11AE', '\u11AF' }); addCharRange(new char[] { '\u11B7', '\u11B8' });
                addCharRange(new char[] { '\u11BA', '\u11BA' }); addCharRange(new char[] { '\u11BC', '\u11C2' }); addCharRange(new char[] { '\u11EB', '\u11EB' }); addCharRange(new char[] { '\u11F0', '\u11F0' }); addCharRange(new char[] { '\u11F9', '\u11F9' }); addCharRange(new char[] { '\u1E00', '\u1E9B' }); addCharRange(new char[] { '\u1EA0', '\u1EF9' }); addCharRange(new char[] { '\u1F00', '\u1F15' });
                addCharRange(new char[] { '\u1F18', '\u1F1D' }); addCharRange(new char[] { '\u1F20', '\u1F45' }); addCharRange(new char[] { '\u1F48', '\u1F4D' }); addCharRange(new char[] { '\u1F50', '\u1F57' }); addCharRange(new char[] { '\u1F59', '\u1F59' }); addCharRange(new char[] { '\u1F5B', '\u1F5B' }); addCharRange(new char[] { '\u1F5D', '\u1F5D' }); addCharRange(new char[] { '\u1F5F', '\u1F7D' });
                addCharRange(new char[] { '\u1F80', '\u1FB4' }); addCharRange(new char[] { '\u1FB6', '\u1FBC' }); addCharRange(new char[] { '\u1FBE', '\u1FBE' }); addCharRange(new char[] { '\u1FC2', '\u1FC4' }); addCharRange(new char[] { '\u1FC6', '\u1FCC' }); addCharRange(new char[] { '\u1FD0', '\u1FD3' }); addCharRange(new char[] { '\u1FD6', '\u1FDB' }); addCharRange(new char[] { '\u1FE0', '\u1FEC' });
                addCharRange(new char[] { '\u1FF2', '\u1FF4' }); addCharRange(new char[] { '\u1FF6', '\u1FFC' }); addCharRange(new char[] { '\u2126', '\u2126' }); addCharRange(new char[] { '\u212A', '\u212B' }); addCharRange(new char[] { '\u212E', '\u212E' }); addCharRange(new char[] { '\u2180', '\u2182' }); addCharRange(new char[] { '\u3041', '\u3094' }); addCharRange(new char[] { '\u30A1', '\u30FA' });
                addCharRange(new char[] { '\u3105', '\u312C' }); addCharRange(new char[] { '\uAC00', '\uD7A3' });
                //Ideographic 
                addCharRange(new char[] { '\u3007', '\u3007' }); addCharRange(new char[] { '\u3021', '\u3029' }); addCharRange(new char[] { '\u4E00', '\u9FA5' });
                //Digit
                addCharRange(new char[] { '\u0030', '\u0039' }); addCharRange(new char[] { '\u0660', '\u0669' }); addCharRange(new char[] { '\u06F0', '\u06F9' }); addCharRange(new char[] { '\u0966', '\u096F' }); addCharRange(new char[] { '\u09E6', '\u09EF' }); addCharRange(new char[] { '\u0A66', '\u0A6F' }); addCharRange(new char[] { '\u0AE6', '\u0AEF' }); addCharRange(new char[] { '\u0B66', '\u0B6F' });
                addCharRange(new char[] { '\u0BE7', '\u0BEF' }); addCharRange(new char[] { '\u0C66', '\u0C6F' }); addCharRange(new char[] { '\u0CE6', '\u0CEF' }); addCharRange(new char[] { '\u0D66', '\u0D6F' }); addCharRange(new char[] { '\u0E50', '\u0E59' }); addCharRange(new char[] { '\u0ED0', '\u0ED9' }); addCharRange(new char[] { '\u0F20', '\u0F29' });
                //Combination
                addCharRange(new char[] { '\u0300', '\u0345' }); addCharRange(new char[] { '\u0360', '\u0361' }); addCharRange(new char[] { '\u0483', '\u0486' }); addCharRange(new char[] { '\u0591', '\u05A1' }); addCharRange(new char[] { '\u05A3', '\u05B9' }); addCharRange(new char[] { '\u05BB', '\u05BD' }); addCharRange(new char[] { '\u05BF', '\u05BF' }); addCharRange(new char[] { '\u05C1', '\u05C2' });
                addCharRange(new char[] { '\u05C4', '\u05C4' }); addCharRange(new char[] { '\u064B', '\u0652' }); addCharRange(new char[] { '\u0670', '\u0670' }); addCharRange(new char[] { '\u06D6', '\u06DC' }); addCharRange(new char[] { '\u06DD', '\u06DF' }); addCharRange(new char[] { '\u06E0', '\u06E4' }); addCharRange(new char[] { '\u06E7', '\u06E8' }); addCharRange(new char[] { '\u06EA', '\u06ED' });
                addCharRange(new char[] { '\u0901', '\u0903' }); addCharRange(new char[] { '\u093C', '\u093C' }); addCharRange(new char[] { '\u093E', '\u094C' }); addCharRange(new char[] { '\u094D', '\u094D' }); addCharRange(new char[] { '\u0951', '\u0954' }); addCharRange(new char[] { '\u0962', '\u0963' }); addCharRange(new char[] { '\u0981', '\u0983' }); addCharRange(new char[] { '\u09BC', '\u09BC' });
                addCharRange(new char[] { '\u09BE', '\u09BE' }); addCharRange(new char[] { '\u09BF', '\u09BF' }); addCharRange(new char[] { '\u09C0', '\u09C4' }); addCharRange(new char[] { '\u09C7', '\u09C8' }); addCharRange(new char[] { '\u09CB', '\u09CD' }); addCharRange(new char[] { '\u09D7', '\u09D7' }); addCharRange(new char[] { '\u09E2', '\u09E3' }); addCharRange(new char[] { '\u0A02', '\u0A02' });
                addCharRange(new char[] { '\u0A3C', '\u0A3C' }); addCharRange(new char[] { '\u0A3E', '\u0A3E' }); addCharRange(new char[] { '\u0A3F', '\u0A3F' }); addCharRange(new char[] { '\u0A40', '\u0A42' }); addCharRange(new char[] { '\u0A47', '\u0A48' }); addCharRange(new char[] { '\u0A4B', '\u0A4D' }); addCharRange(new char[] { '\u0A70', '\u0A71' }); addCharRange(new char[] { '\u0A81', '\u0A83' });
                addCharRange(new char[] { '\u0ABC', '\u0ABC' }); addCharRange(new char[] { '\u0ABE', '\u0AC5' }); addCharRange(new char[] { '\u0AC7', '\u0AC9' }); addCharRange(new char[] { '\u0ACB', '\u0ACD' }); addCharRange(new char[] { '\u0B01', '\u0B03' }); addCharRange(new char[] { '\u0B3C', '\u0B3C' }); addCharRange(new char[] { '\u0B3E', '\u0B43' }); addCharRange(new char[] { '\u0B47', '\u0B48' });
                addCharRange(new char[] { '\u0B4B', '\u0B4D' }); addCharRange(new char[] { '\u0B56', '\u0B57' }); addCharRange(new char[] { '\u0B82', '\u0B83' }); addCharRange(new char[] { '\u0BBE', '\u0BC2' }); addCharRange(new char[] { '\u0BC6', '\u0BC8' }); addCharRange(new char[] { '\u0BCA', '\u0BCD' }); addCharRange(new char[] { '\u0BD7', '\u0BD7' }); addCharRange(new char[] { '\u0C01', '\u0C03' });
                addCharRange(new char[] { '\u0C3E', '\u0C44' }); addCharRange(new char[] { '\u0C46', '\u0C48' }); addCharRange(new char[] { '\u0C4A', '\u0C4D' }); addCharRange(new char[] { '\u0C55', '\u0C56' }); addCharRange(new char[] { '\u0C82', '\u0C83' }); addCharRange(new char[] { '\u0CBE', '\u0CC4' }); addCharRange(new char[] { '\u0CC6', '\u0CC8' }); addCharRange(new char[] { '\u0CCA', '\u0CCD' });
                addCharRange(new char[] { '\u0CD5', '\u0CD6' }); addCharRange(new char[] { '\u0D02', '\u0D03' }); addCharRange(new char[] { '\u0D3E', '\u0D43' }); addCharRange(new char[] { '\u0D46', '\u0D48' }); addCharRange(new char[] { '\u0D4A', '\u0D4D' }); addCharRange(new char[] { '\u0D57', '\u0D57' }); addCharRange(new char[] { '\u0E31', '\u0E31' }); addCharRange(new char[] { '\u0E34', '\u0E3A' });
                addCharRange(new char[] { '\u0E47', '\u0E4E' }); addCharRange(new char[] { '\u0EB1', '\u0EB1' }); addCharRange(new char[] { '\u0EB4', '\u0EB9' }); addCharRange(new char[] { '\u0EBB', '\u0EBC' }); addCharRange(new char[] { '\u0EC8', '\u0ECD' }); addCharRange(new char[] { '\u0F18', '\u0F19' }); addCharRange(new char[] { '\u0F35', '\u0F35' }); addCharRange(new char[] { '\u0F37', '\u0F37' });
                addCharRange(new char[] { '\u0F39', '\u0F39' }); addCharRange(new char[] { '\u0F3E', '\u0F3E' }); addCharRange(new char[] { '\u0F3F', '\u0F3F' }); addCharRange(new char[] { '\u0F71', '\u0F84' }); addCharRange(new char[] { '\u0F86', '\u0F8B' }); addCharRange(new char[] { '\u0F90', '\u0F95' }); addCharRange(new char[] { '\u0F97', '\u0F97' }); addCharRange(new char[] { '\u0F99', '\u0FAD' });
                addCharRange(new char[] { '\u0FB1', '\u0FB7' }); addCharRange(new char[] { '\u0FB9', '\u0FB9' }); addCharRange(new char[] { '\u20D0', '\u20DC' }); addCharRange(new char[] { '\u20E1', '\u20E1' }); addCharRange(new char[] { '\u302A', '\u302F' }); addCharRange(new char[] { '\u3099', '\u3099' }); addCharRange(new char[] { '\u309A', '\u309A' });
                //Extender
                addCharRange(new char[] { '\u00B7', '\u00B7' }); addCharRange(new char[] { '\u02D0', '\u02D0' }); addCharRange(new char[] { '\u02D1', '\u02D1' }); addCharRange(new char[] { '\u0387', '\u0387' }); addCharRange(new char[] { '\u0640', '\u0640' }); addCharRange(new char[] { '\u0E46', '\u0E46' }); addCharRange(new char[] { '\u0EC6', '\u0EC6' }); addCharRange(new char[] { '\u3005', '\u3005' });
                addCharRange(new char[] { '\u3031', '\u3035' }); addCharRange(new char[] { '\u309D', '\u309E' }); addCharRange(new char[] { '\u30FC', '\u30FE' });
            };
            // Same thing as NameStartChar but no ':' character
            CurrentCharType = CharType.NCNameStartChar;// ranges
            {
                addCharRange(new char[] { '_', '_' });
                // BaseChar
                addCharRange(new char[] { '\u0041', '\u005A' }); addCharRange(new char[] { '\u0061', '\u007A' }); addCharRange(new char[] { '\u00C0', '\u00D6' }); addCharRange(new char[] { '\u00D8', '\u00F6' }); addCharRange(new char[] { '\u00F8', '\u00FF' }); addCharRange(new char[] { '\u0100', '\u0131' }); addCharRange(new char[] { '\u0134', '\u013E' }); addCharRange(new char[] { '\u0141', '\u0148' });
                addCharRange(new char[] { '\u014A', '\u017E' }); addCharRange(new char[] { '\u0180', '\u01C3' }); addCharRange(new char[] { '\u01CD', '\u01F0' }); addCharRange(new char[] { '\u01F4', '\u01F5' }); addCharRange(new char[] { '\u01FA', '\u0217' }); addCharRange(new char[] { '\u0250', '\u02A8' }); addCharRange(new char[] { '\u02BB', '\u02C1' }); addCharRange(new char[] { '\u0386', '\u0386' });
                addCharRange(new char[] { '\u0388', '\u038A' }); addCharRange(new char[] { '\u038C', '\u038C' }); addCharRange(new char[] { '\u038E', '\u03A1' }); addCharRange(new char[] { '\u03A3', '\u03CE' }); addCharRange(new char[] { '\u03D0', '\u03D6' }); addCharRange(new char[] { '\u03DA', '\u03DA' }); addCharRange(new char[] { '\u03DC', '\u03DC' }); addCharRange(new char[] { '\u03DE', '\u03DE' });
                addCharRange(new char[] { '\u03E0', '\u03E0' }); addCharRange(new char[] { '\u03E2', '\u03F3' }); addCharRange(new char[] { '\u0401', '\u040C' }); addCharRange(new char[] { '\u040E', '\u044F' }); addCharRange(new char[] { '\u0451', '\u045C' }); addCharRange(new char[] { '\u045E', '\u0481' }); addCharRange(new char[] { '\u0490', '\u04C4' }); addCharRange(new char[] { '\u04C7', '\u04C8' });
                addCharRange(new char[] { '\u04CB', '\u04CC' }); addCharRange(new char[] { '\u04D0', '\u04EB' }); addCharRange(new char[] { '\u04EE', '\u04F5' }); addCharRange(new char[] { '\u04F8', '\u04F9' }); addCharRange(new char[] { '\u0531', '\u0556' }); addCharRange(new char[] { '\u0559', '\u0559' }); addCharRange(new char[] { '\u0561', '\u0586' }); addCharRange(new char[] { '\u05D0', '\u05EA' });
                addCharRange(new char[] { '\u05F0', '\u05F2' }); addCharRange(new char[] { '\u0621', '\u063A' }); addCharRange(new char[] { '\u0641', '\u064A' }); addCharRange(new char[] { '\u0671', '\u06B7' }); addCharRange(new char[] { '\u06BA', '\u06BE' }); addCharRange(new char[] { '\u06C0', '\u06CE' }); addCharRange(new char[] { '\u06D0', '\u06D3' }); addCharRange(new char[] { '\u06D5', '\u06D5' });
                addCharRange(new char[] { '\u06E5', '\u06E6' }); addCharRange(new char[] { '\u0905', '\u0939' }); addCharRange(new char[] { '\u093D', '\u093D' }); addCharRange(new char[] { '\u0958', '\u0961' }); addCharRange(new char[] { '\u0985', '\u098C' }); addCharRange(new char[] { '\u098F', '\u0990' }); addCharRange(new char[] { '\u0993', '\u09A8' }); addCharRange(new char[] { '\u09AA', '\u09B0' });
                addCharRange(new char[] { '\u09B2', '\u09B2' }); addCharRange(new char[] { '\u09B6', '\u09B9' }); addCharRange(new char[] { '\u09DC', '\u09DD' }); addCharRange(new char[] { '\u09DF', '\u09E1' }); addCharRange(new char[] { '\u09F0', '\u09F1' }); addCharRange(new char[] { '\u0A05', '\u0A0A' }); addCharRange(new char[] { '\u0A0F', '\u0A10' }); addCharRange(new char[] { '\u0A13', '\u0A28' });
                addCharRange(new char[] { '\u0A2A', '\u0A30' }); addCharRange(new char[] { '\u0A32', '\u0A33' }); addCharRange(new char[] { '\u0A35', '\u0A36' }); addCharRange(new char[] { '\u0A38', '\u0A39' }); addCharRange(new char[] { '\u0A59', '\u0A5C' }); addCharRange(new char[] { '\u0A5E', '\u0A5E' }); addCharRange(new char[] { '\u0A72', '\u0A74' }); addCharRange(new char[] { '\u0A85', '\u0A8B' });
                addCharRange(new char[] { '\u0A8D', '\u0A8D' }); addCharRange(new char[] { '\u0A8F', '\u0A91' }); addCharRange(new char[] { '\u0A93', '\u0AA8' }); addCharRange(new char[] { '\u0AAA', '\u0AB0' }); addCharRange(new char[] { '\u0AB2', '\u0AB3' }); addCharRange(new char[] { '\u0AB5', '\u0AB9' }); addCharRange(new char[] { '\u0ABD', '\u0ABD' }); addCharRange(new char[] { '\u0AE0', '\u0AE0' });
                addCharRange(new char[] { '\u0B05', '\u0B0C' }); addCharRange(new char[] { '\u0B0F', '\u0B10' }); addCharRange(new char[] { '\u0B13', '\u0B28' }); addCharRange(new char[] { '\u0B2A', '\u0B30' }); addCharRange(new char[] { '\u0B32', '\u0B33' }); addCharRange(new char[] { '\u0B36', '\u0B39' }); addCharRange(new char[] { '\u0B3D', '\u0B3D' }); addCharRange(new char[] { '\u0B5C', '\u0B5D' });
                addCharRange(new char[] { '\u0B5F', '\u0B61' }); addCharRange(new char[] { '\u0B85', '\u0B8A' }); addCharRange(new char[] { '\u0B8E', '\u0B90' }); addCharRange(new char[] { '\u0B92', '\u0B95' }); addCharRange(new char[] { '\u0B99', '\u0B9A' }); addCharRange(new char[] { '\u0B9C', '\u0B9C' }); addCharRange(new char[] { '\u0B9E', '\u0B9F' }); addCharRange(new char[] { '\u0BA3', '\u0BA4' });
                addCharRange(new char[] { '\u0BA8', '\u0BAA' }); addCharRange(new char[] { '\u0BAE', '\u0BB5' }); addCharRange(new char[] { '\u0BB7', '\u0BB9' }); addCharRange(new char[] { '\u0C05', '\u0C0C' }); addCharRange(new char[] { '\u0C0E', '\u0C10' }); addCharRange(new char[] { '\u0C12', '\u0C28' }); addCharRange(new char[] { '\u0C2A', '\u0C33' }); addCharRange(new char[] { '\u0C35', '\u0C39' });
                addCharRange(new char[] { '\u0C60', '\u0C61' }); addCharRange(new char[] { '\u0C85', '\u0C8C' }); addCharRange(new char[] { '\u0C8E', '\u0C90' }); addCharRange(new char[] { '\u0C92', '\u0CA8' }); addCharRange(new char[] { '\u0CAA', '\u0CB3' }); addCharRange(new char[] { '\u0CB5', '\u0CB9' }); addCharRange(new char[] { '\u0CDE', '\u0CDE' }); addCharRange(new char[] { '\u0CE0', '\u0CE1' });
                addCharRange(new char[] { '\u0D05', '\u0D0C' }); addCharRange(new char[] { '\u0D0E', '\u0D10' }); addCharRange(new char[] { '\u0D12', '\u0D28' }); addCharRange(new char[] { '\u0D2A', '\u0D39' }); addCharRange(new char[] { '\u0D60', '\u0D61' }); addCharRange(new char[] { '\u0E01', '\u0E2E' }); addCharRange(new char[] { '\u0E30', '\u0E30' }); addCharRange(new char[] { '\u0E32', '\u0E33' });
                addCharRange(new char[] { '\u0E40', '\u0E45' }); addCharRange(new char[] { '\u0E81', '\u0E82' }); addCharRange(new char[] { '\u0E84', '\u0E84' }); addCharRange(new char[] { '\u0E87', '\u0E88' }); addCharRange(new char[] { '\u0E8A', '\u0E8A' }); addCharRange(new char[] { '\u0E8D', '\u0E8D' }); addCharRange(new char[] { '\u0E94', '\u0E97' }); addCharRange(new char[] { '\u0E99', '\u0E9F' });
                addCharRange(new char[] { '\u0EA1', '\u0EA3' }); addCharRange(new char[] { '\u0EA5', '\u0EA5' }); addCharRange(new char[] { '\u0EA7', '\u0EA7' }); addCharRange(new char[] { '\u0EAA', '\u0EAB' }); addCharRange(new char[] { '\u0EAD', '\u0EAE' }); addCharRange(new char[] { '\u0EB0', '\u0EB0' }); addCharRange(new char[] { '\u0EB2', '\u0EB3' }); addCharRange(new char[] { '\u0EBD', '\u0EBD' });
                addCharRange(new char[] { '\u0EC0', '\u0EC4' }); addCharRange(new char[] { '\u0F40', '\u0F47' }); addCharRange(new char[] { '\u0F49', '\u0F69' }); addCharRange(new char[] { '\u10A0', '\u10C5' }); addCharRange(new char[] { '\u10D0', '\u10F6' }); addCharRange(new char[] { '\u1100', '\u1100' }); addCharRange(new char[] { '\u1102', '\u1103' }); addCharRange(new char[] { '\u1105', '\u1107' });
                addCharRange(new char[] { '\u1109', '\u1109' }); addCharRange(new char[] { '\u110B', '\u110C' }); addCharRange(new char[] { '\u110E', '\u1112' }); addCharRange(new char[] { '\u113C', '\u113C' }); addCharRange(new char[] { '\u113E', '\u113E' }); addCharRange(new char[] { '\u1140', '\u1140' }); addCharRange(new char[] { '\u114C', '\u114C' }); addCharRange(new char[] { '\u114E', '\u114E' });
                addCharRange(new char[] { '\u1150', '\u1150' }); addCharRange(new char[] { '\u1154', '\u1155' }); addCharRange(new char[] { '\u1159', '\u1159' }); addCharRange(new char[] { '\u115F', '\u1161' }); addCharRange(new char[] { '\u1163', '\u1163' }); addCharRange(new char[] { '\u1165', '\u1165' }); addCharRange(new char[] { '\u1167', '\u1167' }); addCharRange(new char[] { '\u1169', '\u1169' });
                addCharRange(new char[] { '\u116D', '\u116E' }); addCharRange(new char[] { '\u1172', '\u1173' }); addCharRange(new char[] { '\u1175', '\u1175' }); addCharRange(new char[] { '\u119E', '\u119E' }); addCharRange(new char[] { '\u11A8', '\u11A8' }); addCharRange(new char[] { '\u11AB', '\u11AB' }); addCharRange(new char[] { '\u11AE', '\u11AF' }); addCharRange(new char[] { '\u11B7', '\u11B8' });
                addCharRange(new char[] { '\u11BA', '\u11BA' }); addCharRange(new char[] { '\u11BC', '\u11C2' }); addCharRange(new char[] { '\u11EB', '\u11EB' }); addCharRange(new char[] { '\u11F0', '\u11F0' }); addCharRange(new char[] { '\u11F9', '\u11F9' }); addCharRange(new char[] { '\u1E00', '\u1E9B' }); addCharRange(new char[] { '\u1EA0', '\u1EF9' }); addCharRange(new char[] { '\u1F00', '\u1F15' });
                addCharRange(new char[] { '\u1F18', '\u1F1D' }); addCharRange(new char[] { '\u1F20', '\u1F45' }); addCharRange(new char[] { '\u1F48', '\u1F4D' }); addCharRange(new char[] { '\u1F50', '\u1F57' }); addCharRange(new char[] { '\u1F59', '\u1F59' }); addCharRange(new char[] { '\u1F5B', '\u1F5B' }); addCharRange(new char[] { '\u1F5D', '\u1F5D' }); addCharRange(new char[] { '\u1F5F', '\u1F7D' });
                addCharRange(new char[] { '\u1F80', '\u1FB4' }); addCharRange(new char[] { '\u1FB6', '\u1FBC' }); addCharRange(new char[] { '\u1FBE', '\u1FBE' }); addCharRange(new char[] { '\u1FC2', '\u1FC4' }); addCharRange(new char[] { '\u1FC6', '\u1FCC' }); addCharRange(new char[] { '\u1FD0', '\u1FD3' }); addCharRange(new char[] { '\u1FD6', '\u1FDB' }); addCharRange(new char[] { '\u1FE0', '\u1FEC' });
                addCharRange(new char[] { '\u1FF2', '\u1FF4' }); addCharRange(new char[] { '\u1FF6', '\u1FFC' }); addCharRange(new char[] { '\u2126', '\u2126' }); addCharRange(new char[] { '\u212A', '\u212B' }); addCharRange(new char[] { '\u212E', '\u212E' }); addCharRange(new char[] { '\u2180', '\u2182' }); addCharRange(new char[] { '\u3041', '\u3094' }); addCharRange(new char[] { '\u30A1', '\u30FA' });
                addCharRange(new char[] { '\u3105', '\u312C' }); addCharRange(new char[] { '\uAC00', '\uD7A3' });
                //Ideographic 
                addCharRange(new char[] { '\u3007', '\u3007' }); addCharRange(new char[] { '\u3021', '\u3029' }); addCharRange(new char[] { '\u4E00', '\u9FA5' });
            }
            // Same thing as NameChar but no ':' character
            CurrentCharType = CharType.NCNameChar;// ranges
            {
                addCharRange(new char[] { '_', '_' }); addCharRange(new char[] { '-', '-' }); addCharRange(new char[] { '.', '.' });
                // BaseChar
                addCharRange(new char[] { '\u0041', '\u005A' }); addCharRange(new char[] { '\u0061', '\u007A' }); addCharRange(new char[] { '\u00C0', '\u00D6' }); addCharRange(new char[] { '\u00D8', '\u00F6' }); addCharRange(new char[] { '\u00F8', '\u00FF' }); addCharRange(new char[] { '\u0100', '\u0131' }); addCharRange(new char[] { '\u0134', '\u013E' }); addCharRange(new char[] { '\u0141', '\u0148' });
                addCharRange(new char[] { '\u014A', '\u017E' }); addCharRange(new char[] { '\u0180', '\u01C3' }); addCharRange(new char[] { '\u01CD', '\u01F0' }); addCharRange(new char[] { '\u01F4', '\u01F5' }); addCharRange(new char[] { '\u01FA', '\u0217' }); addCharRange(new char[] { '\u0250', '\u02A8' }); addCharRange(new char[] { '\u02BB', '\u02C1' }); addCharRange(new char[] { '\u0386', '\u0386' });
                addCharRange(new char[] { '\u0388', '\u038A' }); addCharRange(new char[] { '\u038C', '\u038C' }); addCharRange(new char[] { '\u038E', '\u03A1' }); addCharRange(new char[] { '\u03A3', '\u03CE' }); addCharRange(new char[] { '\u03D0', '\u03D6' }); addCharRange(new char[] { '\u03DA', '\u03DA' }); addCharRange(new char[] { '\u03DC', '\u03DC' }); addCharRange(new char[] { '\u03DE', '\u03DE' });
                addCharRange(new char[] { '\u03E0', '\u03E0' }); addCharRange(new char[] { '\u03E2', '\u03F3' }); addCharRange(new char[] { '\u0401', '\u040C' }); addCharRange(new char[] { '\u040E', '\u044F' }); addCharRange(new char[] { '\u0451', '\u045C' }); addCharRange(new char[] { '\u045E', '\u0481' }); addCharRange(new char[] { '\u0490', '\u04C4' }); addCharRange(new char[] { '\u04C7', '\u04C8' });
                addCharRange(new char[] { '\u04CB', '\u04CC' }); addCharRange(new char[] { '\u04D0', '\u04EB' }); addCharRange(new char[] { '\u04EE', '\u04F5' }); addCharRange(new char[] { '\u04F8', '\u04F9' }); addCharRange(new char[] { '\u0531', '\u0556' }); addCharRange(new char[] { '\u0559', '\u0559' }); addCharRange(new char[] { '\u0561', '\u0586' }); addCharRange(new char[] { '\u05D0', '\u05EA' });
                addCharRange(new char[] { '\u05F0', '\u05F2' }); addCharRange(new char[] { '\u0621', '\u063A' }); addCharRange(new char[] { '\u0641', '\u064A' }); addCharRange(new char[] { '\u0671', '\u06B7' }); addCharRange(new char[] { '\u06BA', '\u06BE' }); addCharRange(new char[] { '\u06C0', '\u06CE' }); addCharRange(new char[] { '\u06D0', '\u06D3' }); addCharRange(new char[] { '\u06D5', '\u06D5' });
                addCharRange(new char[] { '\u06E5', '\u06E6' }); addCharRange(new char[] { '\u0905', '\u0939' }); addCharRange(new char[] { '\u093D', '\u093D' }); addCharRange(new char[] { '\u0958', '\u0961' }); addCharRange(new char[] { '\u0985', '\u098C' }); addCharRange(new char[] { '\u098F', '\u0990' }); addCharRange(new char[] { '\u0993', '\u09A8' }); addCharRange(new char[] { '\u09AA', '\u09B0' });
                addCharRange(new char[] { '\u09B2', '\u09B2' }); addCharRange(new char[] { '\u09B6', '\u09B9' }); addCharRange(new char[] { '\u09DC', '\u09DD' }); addCharRange(new char[] { '\u09DF', '\u09E1' }); addCharRange(new char[] { '\u09F0', '\u09F1' }); addCharRange(new char[] { '\u0A05', '\u0A0A' }); addCharRange(new char[] { '\u0A0F', '\u0A10' }); addCharRange(new char[] { '\u0A13', '\u0A28' });
                addCharRange(new char[] { '\u0A2A', '\u0A30' }); addCharRange(new char[] { '\u0A32', '\u0A33' }); addCharRange(new char[] { '\u0A35', '\u0A36' }); addCharRange(new char[] { '\u0A38', '\u0A39' }); addCharRange(new char[] { '\u0A59', '\u0A5C' }); addCharRange(new char[] { '\u0A5E', '\u0A5E' }); addCharRange(new char[] { '\u0A72', '\u0A74' }); addCharRange(new char[] { '\u0A85', '\u0A8B' });
                addCharRange(new char[] { '\u0A8D', '\u0A8D' }); addCharRange(new char[] { '\u0A8F', '\u0A91' }); addCharRange(new char[] { '\u0A93', '\u0AA8' }); addCharRange(new char[] { '\u0AAA', '\u0AB0' }); addCharRange(new char[] { '\u0AB2', '\u0AB3' }); addCharRange(new char[] { '\u0AB5', '\u0AB9' }); addCharRange(new char[] { '\u0ABD', '\u0ABD' }); addCharRange(new char[] { '\u0AE0', '\u0AE0' });
                addCharRange(new char[] { '\u0B05', '\u0B0C' }); addCharRange(new char[] { '\u0B0F', '\u0B10' }); addCharRange(new char[] { '\u0B13', '\u0B28' }); addCharRange(new char[] { '\u0B2A', '\u0B30' }); addCharRange(new char[] { '\u0B32', '\u0B33' }); addCharRange(new char[] { '\u0B36', '\u0B39' }); addCharRange(new char[] { '\u0B3D', '\u0B3D' }); addCharRange(new char[] { '\u0B5C', '\u0B5D' });
                addCharRange(new char[] { '\u0B5F', '\u0B61' }); addCharRange(new char[] { '\u0B85', '\u0B8A' }); addCharRange(new char[] { '\u0B8E', '\u0B90' }); addCharRange(new char[] { '\u0B92', '\u0B95' }); addCharRange(new char[] { '\u0B99', '\u0B9A' }); addCharRange(new char[] { '\u0B9C', '\u0B9C' }); addCharRange(new char[] { '\u0B9E', '\u0B9F' }); addCharRange(new char[] { '\u0BA3', '\u0BA4' });
                addCharRange(new char[] { '\u0BA8', '\u0BAA' }); addCharRange(new char[] { '\u0BAE', '\u0BB5' }); addCharRange(new char[] { '\u0BB7', '\u0BB9' }); addCharRange(new char[] { '\u0C05', '\u0C0C' }); addCharRange(new char[] { '\u0C0E', '\u0C10' }); addCharRange(new char[] { '\u0C12', '\u0C28' }); addCharRange(new char[] { '\u0C2A', '\u0C33' }); addCharRange(new char[] { '\u0C35', '\u0C39' });
                addCharRange(new char[] { '\u0C60', '\u0C61' }); addCharRange(new char[] { '\u0C85', '\u0C8C' }); addCharRange(new char[] { '\u0C8E', '\u0C90' }); addCharRange(new char[] { '\u0C92', '\u0CA8' }); addCharRange(new char[] { '\u0CAA', '\u0CB3' }); addCharRange(new char[] { '\u0CB5', '\u0CB9' }); addCharRange(new char[] { '\u0CDE', '\u0CDE' }); addCharRange(new char[] { '\u0CE0', '\u0CE1' });
                addCharRange(new char[] { '\u0D05', '\u0D0C' }); addCharRange(new char[] { '\u0D0E', '\u0D10' }); addCharRange(new char[] { '\u0D12', '\u0D28' }); addCharRange(new char[] { '\u0D2A', '\u0D39' }); addCharRange(new char[] { '\u0D60', '\u0D61' }); addCharRange(new char[] { '\u0E01', '\u0E2E' }); addCharRange(new char[] { '\u0E30', '\u0E30' }); addCharRange(new char[] { '\u0E32', '\u0E33' });
                addCharRange(new char[] { '\u0E40', '\u0E45' }); addCharRange(new char[] { '\u0E81', '\u0E82' }); addCharRange(new char[] { '\u0E84', '\u0E84' }); addCharRange(new char[] { '\u0E87', '\u0E88' }); addCharRange(new char[] { '\u0E8A', '\u0E8A' }); addCharRange(new char[] { '\u0E8D', '\u0E8D' }); addCharRange(new char[] { '\u0E94', '\u0E97' }); addCharRange(new char[] { '\u0E99', '\u0E9F' });
                addCharRange(new char[] { '\u0EA1', '\u0EA3' }); addCharRange(new char[] { '\u0EA5', '\u0EA5' }); addCharRange(new char[] { '\u0EA7', '\u0EA7' }); addCharRange(new char[] { '\u0EAA', '\u0EAB' }); addCharRange(new char[] { '\u0EAD', '\u0EAE' }); addCharRange(new char[] { '\u0EB0', '\u0EB0' }); addCharRange(new char[] { '\u0EB2', '\u0EB3' }); addCharRange(new char[] { '\u0EBD', '\u0EBD' });
                addCharRange(new char[] { '\u0EC0', '\u0EC4' }); addCharRange(new char[] { '\u0F40', '\u0F47' }); addCharRange(new char[] { '\u0F49', '\u0F69' }); addCharRange(new char[] { '\u10A0', '\u10C5' }); addCharRange(new char[] { '\u10D0', '\u10F6' }); addCharRange(new char[] { '\u1100', '\u1100' }); addCharRange(new char[] { '\u1102', '\u1103' }); addCharRange(new char[] { '\u1105', '\u1107' });
                addCharRange(new char[] { '\u1109', '\u1109' }); addCharRange(new char[] { '\u110B', '\u110C' }); addCharRange(new char[] { '\u110E', '\u1112' }); addCharRange(new char[] { '\u113C', '\u113C' }); addCharRange(new char[] { '\u113E', '\u113E' }); addCharRange(new char[] { '\u1140', '\u1140' }); addCharRange(new char[] { '\u114C', '\u114C' }); addCharRange(new char[] { '\u114E', '\u114E' });
                addCharRange(new char[] { '\u1150', '\u1150' }); addCharRange(new char[] { '\u1154', '\u1155' }); addCharRange(new char[] { '\u1159', '\u1159' }); addCharRange(new char[] { '\u115F', '\u1161' }); addCharRange(new char[] { '\u1163', '\u1163' }); addCharRange(new char[] { '\u1165', '\u1165' }); addCharRange(new char[] { '\u1167', '\u1167' }); addCharRange(new char[] { '\u1169', '\u1169' });
                addCharRange(new char[] { '\u116D', '\u116E' }); addCharRange(new char[] { '\u1172', '\u1173' }); addCharRange(new char[] { '\u1175', '\u1175' }); addCharRange(new char[] { '\u119E', '\u119E' }); addCharRange(new char[] { '\u11A8', '\u11A8' }); addCharRange(new char[] { '\u11AB', '\u11AB' }); addCharRange(new char[] { '\u11AE', '\u11AF' }); addCharRange(new char[] { '\u11B7', '\u11B8' });
                addCharRange(new char[] { '\u11BA', '\u11BA' }); addCharRange(new char[] { '\u11BC', '\u11C2' }); addCharRange(new char[] { '\u11EB', '\u11EB' }); addCharRange(new char[] { '\u11F0', '\u11F0' }); addCharRange(new char[] { '\u11F9', '\u11F9' }); addCharRange(new char[] { '\u1E00', '\u1E9B' }); addCharRange(new char[] { '\u1EA0', '\u1EF9' }); addCharRange(new char[] { '\u1F00', '\u1F15' });
                addCharRange(new char[] { '\u1F18', '\u1F1D' }); addCharRange(new char[] { '\u1F20', '\u1F45' }); addCharRange(new char[] { '\u1F48', '\u1F4D' }); addCharRange(new char[] { '\u1F50', '\u1F57' }); addCharRange(new char[] { '\u1F59', '\u1F59' }); addCharRange(new char[] { '\u1F5B', '\u1F5B' }); addCharRange(new char[] { '\u1F5D', '\u1F5D' }); addCharRange(new char[] { '\u1F5F', '\u1F7D' });
                addCharRange(new char[] { '\u1F80', '\u1FB4' }); addCharRange(new char[] { '\u1FB6', '\u1FBC' }); addCharRange(new char[] { '\u1FBE', '\u1FBE' }); addCharRange(new char[] { '\u1FC2', '\u1FC4' }); addCharRange(new char[] { '\u1FC6', '\u1FCC' }); addCharRange(new char[] { '\u1FD0', '\u1FD3' }); addCharRange(new char[] { '\u1FD6', '\u1FDB' }); addCharRange(new char[] { '\u1FE0', '\u1FEC' });
                addCharRange(new char[] { '\u1FF2', '\u1FF4' }); addCharRange(new char[] { '\u1FF6', '\u1FFC' }); addCharRange(new char[] { '\u2126', '\u2126' }); addCharRange(new char[] { '\u212A', '\u212B' }); addCharRange(new char[] { '\u212E', '\u212E' }); addCharRange(new char[] { '\u2180', '\u2182' }); addCharRange(new char[] { '\u3041', '\u3094' }); addCharRange(new char[] { '\u30A1', '\u30FA' });
                addCharRange(new char[] { '\u3105', '\u312C' }); addCharRange(new char[] { '\uAC00', '\uD7A3' });
                //Ideographic 
                addCharRange(new char[] { '\u3007', '\u3007' }); addCharRange(new char[] { '\u3021', '\u3029' }); addCharRange(new char[] { '\u4E00', '\u9FA5' });
                //Digit
                addCharRange(new char[] { '\u0030', '\u0039' }); addCharRange(new char[] { '\u0660', '\u0669' }); addCharRange(new char[] { '\u06F0', '\u06F9' }); addCharRange(new char[] { '\u0966', '\u096F' }); addCharRange(new char[] { '\u09E6', '\u09EF' }); addCharRange(new char[] { '\u0A66', '\u0A6F' }); addCharRange(new char[] { '\u0AE6', '\u0AEF' }); addCharRange(new char[] { '\u0B66', '\u0B6F' });
                addCharRange(new char[] { '\u0BE7', '\u0BEF' }); addCharRange(new char[] { '\u0C66', '\u0C6F' }); addCharRange(new char[] { '\u0CE6', '\u0CEF' }); addCharRange(new char[] { '\u0D66', '\u0D6F' }); addCharRange(new char[] { '\u0E50', '\u0E59' }); addCharRange(new char[] { '\u0ED0', '\u0ED9' }); addCharRange(new char[] { '\u0F20', '\u0F29' });
                //Combination
                addCharRange(new char[] { '\u0300', '\u0345' }); addCharRange(new char[] { '\u0360', '\u0361' }); addCharRange(new char[] { '\u0483', '\u0486' }); addCharRange(new char[] { '\u0591', '\u05A1' }); addCharRange(new char[] { '\u05A3', '\u05B9' }); addCharRange(new char[] { '\u05BB', '\u05BD' }); addCharRange(new char[] { '\u05BF', '\u05BF' }); addCharRange(new char[] { '\u05C1', '\u05C2' });
                addCharRange(new char[] { '\u05C4', '\u05C4' }); addCharRange(new char[] { '\u064B', '\u0652' }); addCharRange(new char[] { '\u0670', '\u0670' }); addCharRange(new char[] { '\u06D6', '\u06DC' }); addCharRange(new char[] { '\u06DD', '\u06DF' }); addCharRange(new char[] { '\u06E0', '\u06E4' }); addCharRange(new char[] { '\u06E7', '\u06E8' }); addCharRange(new char[] { '\u06EA', '\u06ED' });
                addCharRange(new char[] { '\u0901', '\u0903' }); addCharRange(new char[] { '\u093C', '\u093C' }); addCharRange(new char[] { '\u093E', '\u094C' }); addCharRange(new char[] { '\u094D', '\u094D' }); addCharRange(new char[] { '\u0951', '\u0954' }); addCharRange(new char[] { '\u0962', '\u0963' }); addCharRange(new char[] { '\u0981', '\u0983' }); addCharRange(new char[] { '\u09BC', '\u09BC' });
                addCharRange(new char[] { '\u09BE', '\u09BE' }); addCharRange(new char[] { '\u09BF', '\u09BF' }); addCharRange(new char[] { '\u09C0', '\u09C4' }); addCharRange(new char[] { '\u09C7', '\u09C8' }); addCharRange(new char[] { '\u09CB', '\u09CD' }); addCharRange(new char[] { '\u09D7', '\u09D7' }); addCharRange(new char[] { '\u09E2', '\u09E3' }); addCharRange(new char[] { '\u0A02', '\u0A02' });
                addCharRange(new char[] { '\u0A3C', '\u0A3C' }); addCharRange(new char[] { '\u0A3E', '\u0A3E' }); addCharRange(new char[] { '\u0A3F', '\u0A3F' }); addCharRange(new char[] { '\u0A40', '\u0A42' }); addCharRange(new char[] { '\u0A47', '\u0A48' }); addCharRange(new char[] { '\u0A4B', '\u0A4D' }); addCharRange(new char[] { '\u0A70', '\u0A71' }); addCharRange(new char[] { '\u0A81', '\u0A83' });
                addCharRange(new char[] { '\u0ABC', '\u0ABC' }); addCharRange(new char[] { '\u0ABE', '\u0AC5' }); addCharRange(new char[] { '\u0AC7', '\u0AC9' }); addCharRange(new char[] { '\u0ACB', '\u0ACD' }); addCharRange(new char[] { '\u0B01', '\u0B03' }); addCharRange(new char[] { '\u0B3C', '\u0B3C' }); addCharRange(new char[] { '\u0B3E', '\u0B43' }); addCharRange(new char[] { '\u0B47', '\u0B48' });
                addCharRange(new char[] { '\u0B4B', '\u0B4D' }); addCharRange(new char[] { '\u0B56', '\u0B57' }); addCharRange(new char[] { '\u0B82', '\u0B83' }); addCharRange(new char[] { '\u0BBE', '\u0BC2' }); addCharRange(new char[] { '\u0BC6', '\u0BC8' }); addCharRange(new char[] { '\u0BCA', '\u0BCD' }); addCharRange(new char[] { '\u0BD7', '\u0BD7' }); addCharRange(new char[] { '\u0C01', '\u0C03' });
                addCharRange(new char[] { '\u0C3E', '\u0C44' }); addCharRange(new char[] { '\u0C46', '\u0C48' }); addCharRange(new char[] { '\u0C4A', '\u0C4D' }); addCharRange(new char[] { '\u0C55', '\u0C56' }); addCharRange(new char[] { '\u0C82', '\u0C83' }); addCharRange(new char[] { '\u0CBE', '\u0CC4' }); addCharRange(new char[] { '\u0CC6', '\u0CC8' }); addCharRange(new char[] { '\u0CCA', '\u0CCD' });
                addCharRange(new char[] { '\u0CD5', '\u0CD6' }); addCharRange(new char[] { '\u0D02', '\u0D03' }); addCharRange(new char[] { '\u0D3E', '\u0D43' }); addCharRange(new char[] { '\u0D46', '\u0D48' }); addCharRange(new char[] { '\u0D4A', '\u0D4D' }); addCharRange(new char[] { '\u0D57', '\u0D57' }); addCharRange(new char[] { '\u0E31', '\u0E31' }); addCharRange(new char[] { '\u0E34', '\u0E3A' });
                addCharRange(new char[] { '\u0E47', '\u0E4E' }); addCharRange(new char[] { '\u0EB1', '\u0EB1' }); addCharRange(new char[] { '\u0EB4', '\u0EB9' }); addCharRange(new char[] { '\u0EBB', '\u0EBC' }); addCharRange(new char[] { '\u0EC8', '\u0ECD' }); addCharRange(new char[] { '\u0F18', '\u0F19' }); addCharRange(new char[] { '\u0F35', '\u0F35' }); addCharRange(new char[] { '\u0F37', '\u0F37' });
                addCharRange(new char[] { '\u0F39', '\u0F39' }); addCharRange(new char[] { '\u0F3E', '\u0F3E' }); addCharRange(new char[] { '\u0F3F', '\u0F3F' }); addCharRange(new char[] { '\u0F71', '\u0F84' }); addCharRange(new char[] { '\u0F86', '\u0F8B' }); addCharRange(new char[] { '\u0F90', '\u0F95' }); addCharRange(new char[] { '\u0F97', '\u0F97' }); addCharRange(new char[] { '\u0F99', '\u0FAD' });
                addCharRange(new char[] { '\u0FB1', '\u0FB7' }); addCharRange(new char[] { '\u0FB9', '\u0FB9' }); addCharRange(new char[] { '\u20D0', '\u20DC' }); addCharRange(new char[] { '\u20E1', '\u20E1' }); addCharRange(new char[] { '\u302A', '\u302F' }); addCharRange(new char[] { '\u3099', '\u3099' }); addCharRange(new char[] { '\u309A', '\u309A' });
                //Extender
                addCharRange(new char[] { '\u00B7', '\u00B7' }); addCharRange(new char[] { '\u02D0', '\u02D0' }); addCharRange(new char[] { '\u02D1', '\u02D1' }); addCharRange(new char[] { '\u0387', '\u0387' }); addCharRange(new char[] { '\u0640', '\u0640' }); addCharRange(new char[] { '\u0E46', '\u0E46' }); addCharRange(new char[] { '\u0EC6', '\u0EC6' }); addCharRange(new char[] { '\u3005', '\u3005' });
                addCharRange(new char[] { '\u3031', '\u3035' }); addCharRange(new char[] { '\u309D', '\u309E' }); addCharRange(new char[] { '\u30FC', '\u30FE' });
            }

            // Leaving these here but these are not used for fourth edition as we have disabled the API below
            CurrentCharType = CharType.NameStartSurrogateHighChar;// ranges
            {
                addCharRange(new char[] { '\ud800', '\udb7f' });
            }

            // Leaving these here but these are not used for fourth edition as we have disabled the API below
            CurrentCharType = CharType.NameStartSurrogateLowChar;// ranges
            {
                addCharRange(new char[] { '\udc00', '\udfff' });
            }

            // Leaving these here but these are not used for fourth edition as we have disabled the API below
            CurrentCharType = CharType.NameSurrogateHighChar;// ranges
            {
                addCharRange(new char[] { '\ud800', '\udb7f' });
            }

            // Leaving these here but these are not used for fourth edition as we have disabled the API below
            CurrentCharType = CharType.NameSurrogateLowChar;// ranges
            {
                addCharRange(new char[] { '\udc00', '\udfff' });
            }

            //[13]    PubidChar    ::=    #x20 | #xD | #xA | [a-zA-Z0-9] | [-'()+,./:=?;!*#@$_%] 
            CurrentCharType = CharType.PubidChar;// ranges
            {
                addCharRange(new char[] { '\u0020', '\u0020' }); addCharRange(new char[] { '\u000D', '\u000D' }); addCharRange(new char[] { '\u000A', '\u000A' });
                addCharRange(new char[] { 'a', 'z' }); addCharRange(new char[] { 'A', 'Z' }); addCharRange(new char[] { '0', '9' }); addCharRange(new char[] { '-', '-' }); addCharRange(new char[] { '-', '-' });
                addCharRange(new char[] { '\'', '\'' }); addCharRange(new char[] { '(', '(' }); addCharRange(new char[] { ')', ')' }); addCharRange(new char[] { '+', '+' }); addCharRange(new char[] { ',', ',' });
                addCharRange(new char[] { '.', '.' }); addCharRange(new char[] { '/', '/' }); addCharRange(new char[] { ':', ':' }); addCharRange(new char[] { '=', '=' }); addCharRange(new char[] { '?', '?' });
                addCharRange(new char[] { ';', ';' }); addCharRange(new char[] { '!', '!' }); addCharRange(new char[] { '*', '*' }); addCharRange(new char[] { '#', '#' }); addCharRange(new char[] { '@', '@' });
                addCharRange(new char[] { '$', '$' }); addCharRange(new char[] { '_', '_' }); addCharRange(new char[] { '%', '%' });
            }
        }

        public static bool IsW3C_NameChar(char c)
        {
            return (s_charTypeTable[c] & CharType.NameChar) != 0;
        }

        public static bool IsW3C_StartNameChar(char c)
        {
            return (s_charTypeTable[c] & CharType.NameStartChar) != 0;
        }

        public static bool IsW3C_NCNameChar(char c)
        {
            return (s_charTypeTable[c] & CharType.NCNameChar) != 0;
        }

        public static bool IsW3C_StartNCNameChar(char c)
        {
            return (s_charTypeTable[c] & CharType.NCNameStartChar) != 0;
        }

        public static bool IsW3C_XmlChar(char c)
        {
            return (s_charTypeTable[c] & CharType.XmlChar) != 0;
        }

        public static bool IsW3C_PubidChar(char c)
        {
            return (s_charTypeTable[c] & CharType.PubidChar) != 0;
        }

        public static bool IsW3C_WhitespaceChar(char c)
        {
            return (s_charTypeTable[c] & CharType.WhiteSpace) != 0;
        }

        public static bool IsW3C_IsValidXmlSurrogateChar(char lowSurrogateChar, char highSurrogateChar)
        {
            return (s_charTypeTable[lowSurrogateChar] & CharType.SurrogateLowChar) != 0 &&
                (s_charTypeTable[highSurrogateChar] & CharType.SurrogateHighChar) != 0;
        }

        public static string GetValidCharacters(CharType charType)
        {
            StringBuilder sb = new StringBuilder();

            for (int charIdx = 0; charIdx < s_charTypeTable.Length; charIdx++)
            {
                if ((s_charTypeTable[charIdx] & charType) != 0)
                {
                    sb.Append((char)charIdx);
                }
            }

            return sb.ToString();
        }

        public static string GetInvalidCharacters(CharType charType)
        {
            StringBuilder sb = new StringBuilder();

            for (int charIdx = 0; charIdx < s_charTypeTable.Length; charIdx++)
            {
                if ((s_charTypeTable[charIdx] & charType) == 0)
                {
                    sb.Append((char)charIdx);
                }
            }

            return sb.ToString();
        }
    }
}
