// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Repro
{
    public class Program
    {

        static int Test(
                        int a00, int a01, int a02, int a03, int a04, int a05, int a06, int a07, int a08, int a09,
                        int a10, int a11, int a12, int a13, int a14, int a15, int a16, int a17, int a18, int a19,
                        int a20, int a21, int a22, int a23, int a24, int a25, int a26, int a27, int a28, int a29,        
                        int a30, int a31, int a32, int a33, int a34, int a35, int a36, int a37, int a38, int a39,
                        int a40, int a41, int a42, int a43, int a44, int a45, int a46, int a47, int a48, int a49,        
                        int a50, int a51, int a52, int a53, int a54, int a55, int a56, int a57, int a58, int a59,
                        int a60, int a61, int a62, int a63, int a64, int a65, int a66, int a67, int a68, int a69,        
                        int a70, int a71, int a72, int a73, int a74, int a75, int a76, int a77, int a78, int a79,
                        int a80, int a81, int a82, int a83, int a84, int a85, int a86, int a87, int a88, int a89,        
                        int a90, int a91, int a92, int a93, int a94, int a95, int a96, int a97, int a98, int a99,

                        int b00, int b01, int b02, int b03, int b04, int b05, int b06, int b07, int b08, int b09,
                        int b10, int b11, int b12, int b13, int b14, int b15, int b16, int b17, int b18, int b19,
                        int b20, int b21, int b22, int b23, int b24, int b25, int b26, int b27, int b28, int b29,        
                        int b30, int b31, int b32, int b33, int b34, int b35, int b36, int b37, int b38, int b39,
                        int b40, int b41, int b42, int b43, int b44, int b45, int b46, int b47, int b48, int b49,        
                        int b50, int b51, int b52, int b53, int b54, int b55, int b56, int b57, int b58, int b59,
                        int b60, int b61, int b62, int b63, int b64, int b65, int b66, int b67, int b68, int b69,        
                        int b70, int b71, int b72, int b73, int b74, int b75, int b76, int b77, int b78, int b79,
                        int b80, int b81, int b82, int b83, int b84, int b85, int b86, int b87, int b88, int b89,        
                        int b90, int b91, int b92, int b93, int b94, int b95, int b96, int b97, int b98, int b99)
        {
            int result = a00 + a30 + a60 + a90 + b20 + b50 + b80;
            // We will make one recursive call to Test()
            if (a00 == 1)
            {
                return result;
            }
            else
            {
                // Using the ? : operator causes us to spill the entire stack and reload it after each arg
                // This creates N^2 LclVar temps  200 * 200 = 40000
                // If the OutgoingArg variable number is setup after these 40,000 LclVars it will
                // cause the emitter to hit an IMPL_LIMITATION when storing into the OutGoingArg area:
                // This shows up as 
                //
                //     Unhandled Exception: System.InvalidProgramException: 
                //     at Repro.Program.Test(...
                //
                // Since our arguments are sorted this code simply shuffles the arguments downward
                // like this:  Test(a1, a2, a3 ...
                //
                return result +
                       Test(
                            (a00 < a01) ? a01 : a00,
                            (a01 < a02) ? a02 : a01,
                            (a02 < a03) ? a03 : a02,
                            (a03 < a04) ? a04 : a03,
                            (a04 < a05) ? a05 : a04,
                            (a05 < a06) ? a06 : a05,
                            (a06 < a07) ? a07 : a06,
                            (a07 < a08) ? a08 : a07,
                            (a08 < a09) ? a09 : a08,
                            (a09 < a10) ? a10 : a09,
                            (a10 < a11) ? a11 : a10,
                            (a11 < a12) ? a12 : a11,
                            (a12 < a13) ? a13 : a12,
                            (a13 < a14) ? a14 : a13,
                            (a14 < a15) ? a15 : a14,
                            (a15 < a16) ? a16 : a15,
                            (a16 < a17) ? a17 : a16,
                            (a17 < a18) ? a18 : a17,
                            (a18 < a19) ? a19 : a18,
                            (a19 < a10) ? a10 : a19,
                            (a20 < a21) ? a21 : a20,
                            (a21 < a22) ? a22 : a21,
                            (a22 < a23) ? a23 : a22,
                            (a23 < a24) ? a24 : a23,
                            (a24 < a25) ? a25 : a24,
                            (a25 < a26) ? a26 : a25,
                            (a26 < a27) ? a27 : a26,
                            (a27 < a28) ? a28 : a27,
                            (a28 < a29) ? a29 : a28,
                            (a29 < a20) ? a20 : a29,
                            (a30 < a31) ? a31 : a30,
                            (a31 < a32) ? a32 : a31,
                            (a32 < a33) ? a33 : a32,
                            (a33 < a34) ? a34 : a33,
                            (a34 < a35) ? a35 : a34,
                            (a35 < a36) ? a36 : a35,
                            (a36 < a37) ? a37 : a36,
                            (a37 < a38) ? a38 : a37,
                            (a38 < a39) ? a39 : a38,
                            (a39 < a30) ? a30 : a39,
                            (a40 < a41) ? a41 : a40,
                            (a41 < a42) ? a42 : a41,
                            (a42 < a43) ? a43 : a42,
                            (a43 < a44) ? a44 : a43,
                            (a44 < a45) ? a45 : a44,
                            (a45 < a46) ? a46 : a45,
                            (a46 < a47) ? a47 : a46,
                            (a47 < a48) ? a48 : a47,
                            (a48 < a49) ? a49 : a48,
                            (a49 < a40) ? a40 : a49,
                            (a50 < a51) ? a51 : a50,
                            (a51 < a52) ? a52 : a51,
                            (a52 < a53) ? a53 : a52,
                            (a53 < a54) ? a54 : a53,
                            (a54 < a55) ? a55 : a54,
                            (a55 < a56) ? a56 : a55,
                            (a56 < a57) ? a57 : a56,
                            (a57 < a58) ? a58 : a57,
                            (a58 < a59) ? a59 : a58,
                            (a59 < a50) ? a50 : a59,
                            (a60 < a61) ? a61 : a60,
                            (a61 < a62) ? a62 : a61,
                            (a62 < a63) ? a63 : a62,
                            (a63 < a64) ? a64 : a63,
                            (a64 < a65) ? a65 : a64,
                            (a65 < a66) ? a66 : a65,
                            (a66 < a67) ? a67 : a66,
                            (a67 < a68) ? a68 : a67,
                            (a68 < a69) ? a69 : a68,
                            (a69 < a60) ? a60 : a69,
                            (a70 < a71) ? a71 : a70,
                            (a71 < a72) ? a72 : a71,
                            (a72 < a73) ? a73 : a72,
                            (a73 < a74) ? a74 : a73,
                            (a74 < a75) ? a75 : a74,
                            (a75 < a76) ? a76 : a75,
                            (a76 < a77) ? a77 : a76,
                            (a77 < a78) ? a78 : a77,
                            (a78 < a79) ? a79 : a78,
                            (a79 < a70) ? a70 : a79,
                            (a80 < a81) ? a81 : a80,
                            (a81 < a82) ? a82 : a81,
                            (a82 < a83) ? a83 : a82,
                            (a83 < a84) ? a84 : a83,
                            (a84 < a85) ? a85 : a84,
                            (a85 < a86) ? a86 : a85,
                            (a86 < a87) ? a87 : a86,
                            (a87 < a88) ? a88 : a87,
                            (a88 < a89) ? a89 : a88,
                            (a89 < a80) ? a80 : a89,
                            (a90 < a91) ? a91 : a90,
                            (a91 < a92) ? a92 : a91,
                            (a92 < a93) ? a93 : a92,
                            (a93 < a94) ? a94 : a93,
                            (a94 < a95) ? a95 : a94,
                            (a95 < a96) ? a96 : a95,
                            (a96 < a97) ? a97 : a96,
                            (a97 < a98) ? a98 : a97,
                            (a98 < a99) ? a99 : a98,
                            (a99 < b00) ? b00 : a99,

                            (b00 < b01) ? b01 : b00,
                            (b01 < b02) ? b02 : b01,
                            (b02 < b03) ? b03 : b02,
                            (b03 < b04) ? b04 : b03,
                            (b04 < b05) ? b05 : b04,
                            (b05 < b06) ? b06 : b05,
                            (b06 < b07) ? b07 : b06,
                            (b07 < b08) ? b08 : b07,
                            (b08 < b09) ? b09 : b08,
                            (b09 < b10) ? b10 : b09,
                            (b10 < b11) ? b11 : b10,
                            (b11 < b12) ? b12 : b11,
                            (b12 < b13) ? b13 : b12,
                            (b13 < b14) ? b14 : b13,
                            (b14 < b15) ? b15 : b14,
                            (b15 < b16) ? b16 : b15,
                            (b16 < b17) ? b17 : b16,
                            (b17 < b18) ? b18 : b17,
                            (b18 < b19) ? b19 : b18,
                            (b19 < b10) ? b10 : b19,
                            (b20 < b21) ? b21 : b20,
                            (b21 < b22) ? b22 : b21,
                            (b22 < b23) ? b23 : b22,
                            (b23 < b24) ? b24 : b23,
                            (b24 < b25) ? b25 : b24,
                            (b25 < b26) ? b26 : b25,
                            (b26 < b27) ? b27 : b26,
                            (b27 < b28) ? b28 : b27,
                            (b28 < b29) ? b29 : b28,
                            (b29 < b20) ? b20 : b29,
                            (b30 < b31) ? b31 : b30,
                            (b31 < b32) ? b32 : b31,
                            (b32 < b33) ? b33 : b32,
                            (b33 < b34) ? b34 : b33,
                            (b34 < b35) ? b35 : b34,
                            (b35 < b36) ? b36 : b35,
                            (b36 < b37) ? b37 : b36,
                            (b37 < b38) ? b38 : b37,
                            (b38 < b39) ? b39 : b38,
                            (b39 < b30) ? b30 : b39,
                            (b40 < b41) ? b41 : b40,
                            (b41 < b42) ? b42 : b41,
                            (b42 < b43) ? b43 : b42,
                            (b43 < b44) ? b44 : b43,
                            (b44 < b45) ? b45 : b44,
                            (b45 < b46) ? b46 : b45,
                            (b46 < b47) ? b47 : b46,
                            (b47 < b48) ? b48 : b47,
                            (b48 < b49) ? b49 : b48,
                            (b49 < b40) ? b40 : b49,
                            (b50 < b51) ? b51 : b50,
                            (b51 < b52) ? b52 : b51,
                            (b52 < b53) ? b53 : b52,
                            (b53 < b54) ? b54 : b53,
                            (b54 < b55) ? b55 : b54,
                            (b55 < b56) ? b56 : b55,
                            (b56 < b57) ? b57 : b56,
                            (b57 < b58) ? b58 : b57,
                            (b58 < b59) ? b59 : b58,
                            (b59 < b50) ? b50 : b59,
                            (b60 < b61) ? b61 : b60,
                            (b61 < b62) ? b62 : b61,
                            (b62 < b63) ? b63 : b62,
                            (b63 < b64) ? b64 : b63,
                            (b64 < b65) ? b65 : b64,
                            (b65 < b66) ? b66 : b65,
                            (b66 < b67) ? b67 : b66,
                            (b67 < b68) ? b68 : b67,
                            (b68 < b69) ? b69 : b68,
                            (b69 < b60) ? b60 : b69,
                            (b70 < b71) ? b71 : b70,
                            (b71 < b72) ? b72 : b71,
                            (b72 < b73) ? b73 : b72,
                            (b73 < b74) ? b74 : b73,
                            (b74 < b75) ? b75 : b74,
                            (b75 < b76) ? b76 : b75,
                            (b76 < b77) ? b77 : b76,
                            (b77 < b78) ? b78 : b77,
                            (b78 < b79) ? b79 : b78,
                            (b79 < b70) ? b70 : b79,
                            (b80 < b81) ? b81 : b80,
                            (b81 < b82) ? b82 : b81,
                            (b82 < b83) ? b83 : b82,
                            (b83 < b84) ? b84 : b83,
                            (b84 < b85) ? b85 : b84,
                            (b85 < b86) ? b86 : b85,
                            (b86 < b87) ? b87 : b86,
                            (b87 < b88) ? b88 : b87,
                            (b88 < b89) ? b89 : b88,
                            (b89 < b80) ? b80 : b89,
                            (b90 < b91) ? b91 : b90,
                            (b91 < b92) ? b92 : b91,
                            (b92 < b93) ? b93 : b92,
                            (b93 < b94) ? b94 : b93,
                            (b94 < b95) ? b95 : b94,
                            (b95 < b96) ? b96 : b95,
                            (b96 < b97) ? b97 : b96,
                            (b97 < b98) ? b98 : b97,
                            (b98 < b99) ? b99 : b98,
                            (b99 < a00) ? a00 : b99);

            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            int result = Test(   0,   1,   2,   3,   4,   5,   6,   7,   8,   9,
                                10,  11,  12,  13,  14,  15,  16,  17,  18,  19,    
                                20,  21,  22,  23,  24,  25,  26,  27,  28,  29,            
                                30,  31,  32,  33,  34,  35,  36,  37,  38,  39,            
                                40,  41,  42,  43,  44,  45,  46,  47,  48,  49,            
                                50,  51,  52,  53,  54,  55,  56,  57,  58,  59,            
                                60,  61,  62,  63,  64,  65,  66,  67,  68,  69,            
                                70,  71,  72,  73,  74,  75,  76,  77,  78,  79,            
                                80,  81,  82,  83,  84,  85,  86,  87,  88,  89,            
                                90,  91,  92,  93,  94,  95,  96,  97,  98,  99,

                               100, 101, 102, 103, 104, 105, 106, 107, 108, 109,
                               110, 111, 112, 113, 114, 115, 116, 117, 118, 119,    
                               120, 121, 122, 123, 124, 125, 126, 127, 128, 129,            
                               130, 131, 132, 133, 134, 135, 136, 137, 138, 139,            
                               140, 141, 142, 143, 144, 145, 146, 147, 148, 149,            
                               150, 151, 152, 153, 154, 155, 156, 157, 158, 159,            
                               160, 161, 162, 163, 164, 165, 166, 167, 168, 169,            
                               170, 171, 172, 173, 174, 175, 176, 177, 178, 179,            
                               180, 181, 182, 183, 184, 185, 186, 187, 188, 189,            
                               190, 191, 192, 193, 194, 195, 196, 197, 198, 199);


            if (result == 1267)
            {
                Console.WriteLine("Test Passed");
                // Correct result                
                return 100;
            }
            else
            {
                Console.WriteLine("*** FAILED ***, result was " + result);
                // Incorrect result            
                return -1;
            }
        }
    }
}
