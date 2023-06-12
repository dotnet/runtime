// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

namespace test
{

    public class LargeArray114
    {

        [Fact]
        public static int TestEntryPoint()
        {

            System.String[] array = new System.String[114];
            array[0] = "string0000";
            array[1] = array[0] + "string0001";
            array[2] = array[1] + "string0002";
            array[3] = array[2] + "string0003";
            array[4] = array[3] + "string0004";
            array[5] = array[4] + "string0005";
            array[6] = array[5] + "string0006";
            array[7] = array[6] + "string0007";
            array[8] = array[7] + "string0008";
            array[9] = array[8] + "string0009";
            array[10] = array[9] + "string0010";
            array[11] = array[10] + "string0011";
            array[12] = array[11] + "string0012";
            array[13] = array[12] + "string0013";
            array[14] = array[13] + "string0014";
            array[15] = array[14] + "string0015";
            array[16] = array[15] + "string0016";
            array[17] = array[16] + "string0017";
            array[18] = array[17] + "string0018";
            array[19] = array[18] + "string0019";
            array[20] = array[19] + "string0020";
            array[21] = array[20] + "string0021";
            array[22] = array[21] + "string0022";
            array[23] = array[22] + "string0023";
            array[24] = array[23] + "string0024";
            array[25] = array[24] + "string0025";
            array[26] = array[25] + "string0026";
            array[27] = array[26] + "string0027";
            array[28] = array[27] + "string0028";
            array[29] = array[28] + "string0029";
            array[30] = array[29] + "string0030";
            array[31] = array[30] + "string0031";
            array[32] = array[31] + "string0032";
            array[33] = array[32] + "string0033";
            array[34] = array[33] + "string0034";
            array[35] = array[34] + "string0035";
            array[36] = array[35] + "string0036";
            array[37] = array[36] + "string0037";
            array[38] = array[37] + "string0038";
            array[39] = array[38] + "string0039";
            array[40] = array[39] + "string0040";
            array[41] = array[40] + "string0041";
            array[42] = array[41] + "string0042";
            array[43] = array[42] + "string0043";
            array[44] = array[43] + "string0044";
            array[45] = array[44] + "string0045";
            array[46] = array[45] + "string0046";
            array[47] = array[46] + "string0047";
            array[48] = array[47] + "string0048";
            array[49] = array[48] + "string0049";
            array[50] = array[49] + "string0050";
            array[51] = array[50] + "string0051";
            array[52] = array[51] + "string0052";
            array[53] = array[52] + "string0053";
            array[54] = array[53] + "string0054";
            array[55] = array[54] + "string0055";
            array[56] = array[55] + "string0056";
            array[57] = array[56] + "string0057";
            array[58] = array[57] + "string0058";
            array[59] = array[58] + "string0059";
            array[60] = array[59] + "string0060";
            array[61] = array[60] + "string0061";
            array[62] = array[61] + "string0062";
            array[63] = array[62] + "string0063";
            array[64] = array[63] + "string0064";
            array[65] = array[64] + "string0065";
            array[66] = array[65] + "string0066";
            array[67] = array[66] + "string0067";
            array[68] = array[67] + "string0068";
            array[69] = array[68] + "string0069";
            array[70] = array[69] + "string0070";
            array[71] = array[70] + "string0071";
            array[72] = array[71] + "string0072";
            array[73] = array[72] + "string0073";
            array[74] = array[73] + "string0074";
            array[75] = array[74] + "string0075";
            array[76] = array[75] + "string0076";
            array[77] = array[76] + "string0077";
            array[78] = array[77] + "string0078";
            array[79] = array[78] + "string0079";
            array[80] = array[79] + "string0080";
            array[81] = array[80] + "string0081";
            array[82] = array[81] + "string0082";
            array[83] = array[82] + "string0083";
            array[84] = array[83] + "string0084";
            array[85] = array[84] + "string0085";
            array[86] = array[85] + "string0086";
            array[87] = array[86] + "string0087";
            array[88] = array[87] + "string0088";
            array[89] = array[88] + "string0089";
            array[90] = array[89] + "string0090";
            array[91] = array[90] + "string0091";
            array[92] = array[91] + "string0092";
            array[93] = array[92] + "string0093";
            array[94] = array[93] + "string0094";
            array[95] = array[94] + "string0095";
            array[96] = array[95] + "string0096";
            array[97] = array[96] + "string0097";
            array[98] = array[97] + "string0098";
            array[99] = array[98] + "string0099";
            array[100] = array[99] + "string0100";
            array[101] = array[100] + "string0101";
            array[102] = array[101] + "string0102";
            array[103] = array[102] + "string0103";
            array[104] = array[103] + "string0104";
            array[105] = array[104] + "string0105";
            array[106] = array[105] + "string0106";
            array[107] = array[106] + "string0107";
            array[108] = array[107] + "string0108";
            array[109] = array[108] + "string0109";
            array[110] = array[109] + "string0110";
            array[111] = array[110] + "string0111";
            array[112] = array[111] + "string0112";
            array[113] = array[112] + "string0113";
            System.Console.WriteLine("Max String Length = " + array[113].Length);
            return 100;
        }
    }
}
