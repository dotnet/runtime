// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
    internal sealed class AesManagedTransform : BasicSymmetricCipher, ILiteSymmetricCipher
    {
        private const int BlockSizeBytes = AesImplementation.BlockSizeBytes;
        private const int BlockSizeInts = BlockSizeBytes / 4;

        private readonly bool _encrypting;

        private int[] _encryptKeyExpansion;
        private int[] _decryptKeyExpansion;

        private readonly int _Nr;
        private readonly int _Nk;

        private int[] _IV;
        private int[] _lastBlockBuffer;

        public AesManagedTransform(ReadOnlySpan<byte> key,
                                   ReadOnlySpan<byte> iv,
                                   bool encrypting)
            // AesManagedTransform doesn't use the base IV property, so just pass 'null'.
            : base(iv: null, BlockSizeBytes, BlockSizeBytes)
        {
            Debug.Assert(BitConverter.IsLittleEndian, "The logic of casting Span<int> to Span<byte> below assumes little endian");
            Debug.Assert(iv.Length == BlockSizeBytes);

            _encrypting = encrypting;
            _Nr = GetNumberOfRounds(key);
            _Nk = key.Length / 4;

            _IV = new int[BlockSizeInts];
            iv.CopyTo(MemoryMarshal.AsBytes(_IV.AsSpan()));

            GenerateKeyExpansion(key);

            _lastBlockBuffer = _IV.AsSpan().ToArray();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // We need to always zeroize the following fields because they contain sensitive data.
                // Note: Can't use CryptographicOperations.ZeroMemory since these are int[] and not byte[].
                if (_IV != null)
                {
                    Array.Clear(_IV);
                    _IV = null!;
                }
                if (_lastBlockBuffer != null)
                {
                    Array.Clear(_lastBlockBuffer);
                    _lastBlockBuffer = null!;
                }
                if (_encryptKeyExpansion != null)
                {
                    Array.Clear(_encryptKeyExpansion);
                    _encryptKeyExpansion = null!;
                }
                if (_decryptKeyExpansion != null)
                {
                    Array.Clear(_decryptKeyExpansion);
                    _decryptKeyExpansion = null!;
                }
            }

            base.Dispose(disposing);
        }

        public override int Transform(ReadOnlySpan<byte> input, Span<byte> output)
        {
            Debug.Assert(input.Length % BlockSizeBytes == 0);
            Debug.Assert(output.Length >= input.Length);

            // the below algorithm doesn't allow overlap, so rent a buffer to transform into
            if (input.Overlaps(output, out int offset) && offset != 0)
            {
                byte[] rented = CryptoPool.Rent(input.Length);
                int bytesWritten = 0;

                try
                {
                    bytesWritten = _encrypting ?
                        EncryptData(input, rented) :
                        DecryptData(input, rented);
                    rented.AsSpan(0, bytesWritten).CopyTo(output);
                    return bytesWritten;
                }
                finally
                {
                    CryptoPool.Return(rented, clearSize: bytesWritten);
                }
            }
            else
            {
                // with no overlap, we can just write directly to the output
                return _encrypting ?
                    EncryptData(input, output) :
                    DecryptData(input, output);
            }
        }

        public override int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output)
        {
            int bytesWritten = Transform(input, output);
            Reset();
            return bytesWritten;
        }

        //
        // resets the state of the transform
        //

        void ILiteSymmetricCipher.Reset(ReadOnlySpan<byte> iv) => throw new NotImplementedException(); // never invoked

        private void Reset()
        {
            _IV.AsSpan().CopyTo(_lastBlockBuffer);
        }

        //
        // Encrypts input into output using the AES encryption routine.
        // This method writes the encrypted data into the output buffer.
        //
        private int EncryptData(ReadOnlySpan<byte> input, Span<byte> output)
        {
            int inputCount = input.Length;

            Span<int> work = stackalloc int[BlockSizeInts];
            Span<int> temp = stackalloc int[BlockSizeInts];

            int workBaseIndex = 0;
            int iNumBlocks = inputCount / BlockSizeBytes;
            int transformCount = 0;
            for (int blockNum = 0; blockNum < iNumBlocks; ++blockNum)
            {
                input.Slice(workBaseIndex, BlockSizeBytes).CopyTo(MemoryMarshal.AsBytes(work));

                for (int i = 0; i < BlockSizeInts; ++i)
                {
                    // XOR with the last encrypted block
                    work[i] ^= _lastBlockBuffer[i];
                }

                Enc(work, temp);

                for (int i = 0; i < BlockSizeInts; ++i)
                {
                    output[transformCount++] = (byte)(temp[i] & 0xFF);
                    output[transformCount++] = (byte)(temp[i] >> 8 & 0xFF);
                    output[transformCount++] = (byte)(temp[i] >> 16 & 0xFF);
                    output[transformCount++] = (byte)(temp[i] >> 24 & 0xFF);
                }

                Debug.Assert(_lastBlockBuffer.Length == BlockSizeInts);
                temp.CopyTo(_lastBlockBuffer);

                workBaseIndex += BlockSizeBytes;
            }

            return inputCount;
        }

        //
        // Decrypts intput into output using the AES encryption routine.
        // This method writes the decrypted data into the output buffer.
        //
        private int DecryptData(ReadOnlySpan<byte> input, Span<byte> output)
        {
            int inputCount = input.Length;

            Span<int> work = stackalloc int[BlockSizeInts];
            Span<int> temp = stackalloc int[BlockSizeInts];

            int iNumBlocks = inputCount / BlockSizeBytes;
            int workBaseIndex = 0, index = 0, transformCount = 0;
            for (int blockNum = 0; blockNum < iNumBlocks; ++blockNum)
            {
                index = workBaseIndex;
                for (int i = 0; i < BlockSizeInts; ++i)
                {
                    int i0 = input[index++];
                    int i1 = input[index++];
                    int i2 = input[index++];
                    int i3 = input[index++];
                    work[i] = i3 << 24 | i2 << 16 | i1 << 8 | i0;
                }

                Dec(work, temp);

                index = workBaseIndex;
                for (int i = 0; i < BlockSizeInts; ++i)
                {
                    temp[i] ^= _lastBlockBuffer[i];
                    // save the input buffer
                    int i0 = input[index++];
                    int i1 = input[index++];
                    int i2 = input[index++];
                    int i3 = input[index++];
                    _lastBlockBuffer[i] = i3 << 24 | i2 << 16 | i1 << 8 | i0;
                }

                for (int i = 0; i < BlockSizeInts; ++i)
                {
                    output[transformCount++] = (byte)(temp[i] & 0xFF);
                    output[transformCount++] = (byte)(temp[i] >> 8 & 0xFF);
                    output[transformCount++] = (byte)(temp[i] >> 16 & 0xFF);
                    output[transformCount++] = (byte)(temp[i] >> 24 & 0xFF);
                }

                workBaseIndex += BlockSizeBytes;
            }

            return inputCount;
        }

        //
        // AES encryption function.
        //
        private void Enc(Span<int> work, Span<int> temp)
        {
            for (int i = 0; i < BlockSizeInts; ++i)
            {
                work[i] ^= _encryptKeyExpansion[i];
            }

            ReadOnlySpan<int> T = s_T;
            ReadOnlySpan<int> encryptindex = s_encryptindex;
            int encryptindexIndex;
            int encryptKeyExpansionIndex = BlockSizeInts;
            for (int r = 1; r < _Nr; ++r)
            {
                encryptindexIndex = 0;
                for (int i = 0; i < BlockSizeInts; ++i)
                {
                    temp[i] = T[0 + (work[i] & 0xFF)] ^
                              T[256 + ((work[encryptindex[encryptindexIndex]] >> 8) & 0xFF)] ^
                              T[512 + ((work[encryptindex[encryptindexIndex + BlockSizeInts]] >> 16) & 0xFF)] ^
                              T[768 + ((work[encryptindex[encryptindexIndex + (BlockSizeInts * 2)]] >> 24) & 0xFF)] ^
                              _encryptKeyExpansion[encryptKeyExpansionIndex];
                    encryptindexIndex++;
                    encryptKeyExpansionIndex++;
                }

                temp.CopyTo(work);
            }

            ReadOnlySpan<int> TF = s_TF;
            encryptindexIndex = 0;
            for (int i = 0; i < BlockSizeInts; ++i)
            {
                temp[i] = TF[0 + (work[i] & 0xFF)] ^
                          TF[256 + ((work[encryptindex[encryptindexIndex]] >> 8) & 0xFF)] ^
                          TF[512 + ((work[encryptindex[encryptindexIndex + BlockSizeInts]] >> 16) & 0xFF)] ^
                          TF[768 + ((work[encryptindex[encryptindexIndex + (BlockSizeInts * 2)]] >> 24) & 0xFF)] ^
                          _encryptKeyExpansion[encryptKeyExpansionIndex];
                encryptindexIndex++;
                encryptKeyExpansionIndex++;
            }
        }

        //
        // AES decryption function.
        //

        private void Dec(Span<int> work, Span<int> temp)
        {
            int keyIndex = BlockSizeInts * _Nr;
            for (int i = 0; i < BlockSizeInts; ++i)
            {
                work[i] ^= _decryptKeyExpansion[keyIndex];
                keyIndex++;
            }

            ReadOnlySpan<int> iT = s_iT;
            ReadOnlySpan<int> decryptindex = s_decryptindex;
            int decryptindexIndex;
            int decryptKeyExpansionIndex;
            for (int r = 1; r < _Nr; ++r)
            {
                keyIndex -= 2 * BlockSizeInts;
                decryptindexIndex = 0;
                decryptKeyExpansionIndex = keyIndex;
                for (int i = 0; i < BlockSizeInts; ++i)
                {
                    temp[i] = iT[0 + ((work[i]) & 0xFF)] ^
                              iT[256 + ((work[decryptindex[decryptindexIndex]] >> 8) & 0xFF)] ^
                              iT[512 + ((work[decryptindex[decryptindexIndex + BlockSizeInts]] >> 16) & 0xFF)] ^
                              iT[768 + ((work[decryptindex[decryptindexIndex + (BlockSizeInts * 2)]] >> 24) & 0xFF)] ^
                              _decryptKeyExpansion[decryptKeyExpansionIndex];
                    keyIndex++;
                    decryptindexIndex++;
                    decryptKeyExpansionIndex++;
                }

                temp.CopyTo(work);
            }

            ReadOnlySpan<int> iTF = s_iTF;
            keyIndex = 0;
            decryptindexIndex = 0;
            decryptKeyExpansionIndex = keyIndex;
            for (int i = 0; i < BlockSizeInts; ++i)
            {
                temp[i] = iTF[0 + ((work[i]) & 0xFF)] ^
                          iTF[256 + ((work[decryptindex[decryptindexIndex]] >> 8) & 0xFF)] ^
                          iTF[512 + ((work[decryptindex[decryptindexIndex + BlockSizeInts]] >> 16) & 0xFF)] ^
                          iTF[768 + ((work[decryptindex[decryptindexIndex + (BlockSizeInts * 2)]] >> 24) & 0xFF)] ^
                          _decryptKeyExpansion[decryptKeyExpansionIndex];
                decryptindexIndex++;
                decryptKeyExpansionIndex++;
            }
        }

        private static int GetNumberOfRounds(ReadOnlySpan<byte> key)
        {
            return (BlockSizeBytes > key.Length ? BlockSizeBytes : key.Length) switch
            {
                16 => 10, // 128 bits
                // 24 => 12, // 192 bits is not supported by SubtleCrypto, so the managed implementation doesn't support it either
                32 => 14, // 256 bits
                _ => throw new CryptographicException(SR.Cryptography_InvalidKeySize)
            };
        }

        //
        // Key expansion routine.
        //

        [MemberNotNull(nameof(_encryptKeyExpansion))]
        [MemberNotNull(nameof(_decryptKeyExpansion))]
        private void GenerateKeyExpansion(ReadOnlySpan<byte> key)
        {
            _encryptKeyExpansion = new int[BlockSizeInts * (_Nr + 1)];
            _decryptKeyExpansion = new int[BlockSizeInts * (_Nr + 1)];
            int iTemp;

            int index = 0;
            for (int i = 0; i < _Nk; ++i)
            {
                int i0 = key[index++];
                int i1 = key[index++];
                int i2 = key[index++];
                int i3 = key[index++];
                _encryptKeyExpansion[i] = i3 << 24 | i2 << 16 | i1 << 8 | i0;
            }

            if (_Nk <= 6)
            {
                for (int i = _Nk; i < BlockSizeInts * (_Nr + 1); ++i)
                {
                    iTemp = _encryptKeyExpansion[i - 1];

                    if (i % _Nk == 0)
                    {
                        iTemp = SubWord(rot3(iTemp));
                        iTemp ^= s_Rcon[(i / _Nk) - 1];
                    }

                    _encryptKeyExpansion[i] = _encryptKeyExpansion[i - _Nk] ^ iTemp;
                }
            }
            else
            {
                for (int i = _Nk; i < BlockSizeInts * (_Nr + 1); ++i)
                {
                    iTemp = _encryptKeyExpansion[i - 1];

                    if (i % _Nk == 0)
                    {
                        iTemp = SubWord(rot3(iTemp));
                        iTemp ^= s_Rcon[(i / _Nk) - 1];
                    }
                    else if (i % _Nk == 4)
                    {
                        iTemp = SubWord(iTemp);
                    }

                    _encryptKeyExpansion[i] = _encryptKeyExpansion[i - _Nk] ^ iTemp;
                }
            }

            for (int i = 0; i < BlockSizeInts; ++i)
            {
                _decryptKeyExpansion[i] = _encryptKeyExpansion[i];
                _decryptKeyExpansion[BlockSizeInts * _Nr + i] = _encryptKeyExpansion[BlockSizeInts * _Nr + i];
            }

            for (int i = BlockSizeInts; i < BlockSizeInts * _Nr; ++i)
            {
                int keyVal = _encryptKeyExpansion[i];
                int mul02 = MulX(keyVal);
                int mul04 = MulX(mul02);
                int mul08 = MulX(mul04);
                int mul09 = keyVal ^ mul08;
                _decryptKeyExpansion[i] = mul02 ^ mul04 ^ mul08 ^ rot3(mul02 ^ mul09) ^ rot2(mul04 ^ mul09) ^ rot1(mul09);
            }
        }

        private static int rot1(int val) => int.RotateLeft(val, 8);
        private static int rot2(int val) => int.RotateLeft(val, 16);
        private static int rot3(int val) => int.RotateLeft(val, 24);

        private static int SubWord(int a)
        {
            ReadOnlySpan<byte> sbox = Sbox;
            return sbox[a & 0xFF] |
                   sbox[a >> 8 & 0xFF] << 8 |
                   sbox[a >> 16 & 0xFF] << 16 |
                   sbox[a >> 24 & 0xFF] << 24;
        }

        private static int MulX(int x)
        {
            int u = x & unchecked((int)0x80808080);
            return ((x & unchecked((int)0x7f7f7f7f)) << 1) ^ ((u - (u >> 7 & 0x01FFFFFF)) & 0x1b1b1b1b);
        }

        private static ReadOnlySpan<byte> Sbox => new byte[] {
             99, 124, 119, 123, 242, 107, 111, 197,  48,   1, 103,  43, 254, 215, 171, 118,
            202, 130, 201, 125, 250,  89,  71, 240, 173, 212, 162, 175, 156, 164, 114, 192,
            183, 253, 147,  38,  54,  63, 247, 204,  52, 165, 229, 241, 113, 216,  49,  21,
              4, 199,  35, 195,  24, 150,   5, 154,   7,  18, 128, 226, 235,  39, 178, 117,
              9, 131,  44,  26,  27, 110,  90, 160,  82,  59, 214, 179,  41, 227,  47, 132,
             83, 209,   0, 237,  32, 252, 177,  91, 106, 203, 190,  57,  74,  76,  88, 207,
            208, 239, 170, 251,  67,  77,  51, 133,  69, 249,   2, 127,  80,  60, 159, 168,
             81, 163,  64, 143, 146, 157,  56, 245, 188, 182, 218,  33,  16, 255, 243, 210,
            205,  12,  19, 236,  95, 151,  68,  23, 196, 167, 126,  61, 100,  93,  25, 115,
             96, 129,  79, 220,  34,  42, 144, 136,  70, 238, 184,  20, 222,  94,  11, 219,
            224,  50,  58,  10,  73,   6,  36,  92, 194, 211, 172,  98, 145, 149, 228, 121,
            231, 200,  55, 109, 141, 213,  78, 169, 108,  86, 244, 234, 101, 122, 174,   8,
            186, 120,  37,  46,  28, 166, 180, 198, 232, 221, 116,  31,  75, 189, 139, 138,
            112,  62, 181, 102,  72,   3, 246,  14,  97,  53,  87, 185, 134, 193,  29, 158,
            225, 248, 152,  17, 105, 217, 142, 148, 155,  30, 135, 233, 206,  85,  40, 223,
            140, 161, 137,  13, 191, 230,  66, 104,  65, 153,  45,  15, 176,  84, 187,  22 };

        // Precompute the modulus operations: these are performance killers when called frequently
        private static readonly int[] s_encryptindex = new int[BlockSizeInts * 3] {
            1, 2, 3, 0,
            2, 3, 0, 1,
            3, 0, 1, 2,
        };

        private static readonly int[] s_decryptindex = new int[BlockSizeInts * 3] {
            3, 0, 1, 2,
            2, 3, 0, 1,
            1, 2, 3, 0,
        };

        private static readonly int[] s_Rcon = new int[] {
            0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x1b, 0x36,
            0x6c, 0xd8, 0xab, 0x4d, 0x9a, 0x2f, 0x5e, 0xbc, 0x63, 0xc6,
            0x97, 0x35, 0x6a, 0xd4, 0xb3, 0x7d, 0xfa, 0xef, 0xc5, 0x91 };

        private static readonly int[] s_T = new int[4 * 256]
            {
                // s_T1
                -1520213050,  -2072216328,  -1720223762,  -1921287178,    234025727,  -1117033514,  -1318096930,   1422247313,
                1345335392,     50397442,  -1452841010,   2099981142,    436141799,   1658312629,   -424957107,  -1703512340,
                1170918031,  -1652391393,   1086966153,  -2021818886,    368769775,   -346465870,   -918075506,    200339707,
                -324162239,   1742001331,    -39673249,   -357585083,  -1080255453,   -140204973,  -1770884380,   1539358875,
               -1028147339,    486407649,  -1366060227,   1780885068,   1513502316,   1094664062,     49805301,   1338821763,
                1546925160,   -190470831,    887481809,    150073849,  -1821281822,   1943591083,   1395732834,   1058346282,
                 201589768,   1388824469,   1696801606,   1589887901,    672667696,  -1583966665,    251987210,  -1248159185,
                 151455502,    907153956,  -1686077413,   1038279391,    652995533,   1764173646,   -843926913,  -1619692054,
                 453576978,  -1635548387,   1949051992,    773462580,    756751158,  -1301385508,   -296068428,    -73359269,
                -162377052,   1295727478,   1641469623,   -827083907,   2066295122,   1055122397,   1898917726,  -1752923117,
                -179088474,   1758581177,            0,    753790401,   1612718144,    536673507,   -927878791,   -312779850,
               -1100322092,   1187761037,   -641810841,   1262041458,   -565556588,   -733197160,   -396863312,   1255133061,
                1808847035,    720367557,   -441800113,    385612781,   -985447546,   -682799718,   1429418854,  -1803188975,
                -817543798,    284817897,    100794884,  -2122350594,   -263171936,   1144798328,  -1163944155,   -475486133,
                -212774494,    -22830243,  -1069531008,  -1970303227,  -1382903233,  -1130521311,   1211644016,     83228145,
                -541279133,  -1044990345,   1977277103,   1663115586,    806359072,    452984805,    250868733,   1842533055,
                1288555905,    336333848,    890442534,    804056259,   -513843266,  -1567123659,   -867941240,    957814574,
                1472513171,   -223893675,  -2105639172,   1195195770,  -1402706744,   -413311558,    723065138,  -1787595802,
               -1604296512,  -1736343271,   -783331426,   2145180835,   1713513028,   2116692564,  -1416589253,  -2088204277,
                -901364084,    703524551,   -742868885,   1007948840,   2044649127,   -497131844,    487262998,   1994120109,
                1004593371,   1446130276,   1312438900,    503974420,   -615954030,    168166924,   1814307912,   -463709000,
                1573044895,   1859376061,   -273896381,  -1503501628,  -1466855111,  -1533700815,    937747667,  -1954973198,
                 854058965,   1137232011,   1496790894,  -1217565222,  -1936880383,   1691735473,   -766620004,   -525751991,
               -1267962664,    -95005012,    133494003,    636152527,  -1352309302,  -1904575756,   -374428089,    403179536,
                -709182865,  -2005370640,   1864705354,   1915629148,    605822008,   -240736681,   -944458637,   1371981463,
                 602466507,   2094914977,  -1670089496,    555687742,   -582268010,   -591544991,  -2037675251,  -2054518257,
               -1871679264,   1111375484,   -994724495,  -1436129588,   -666351472,     84083462,     32962295,    302911004,
               -1553899070,   1597322602,   -111716434,   -793134743,  -1853454825,   1489093017,    656219450,  -1180787161,
                 954327513,    335083755,  -1281845205,    856756514,  -1150719534,   1893325225,  -1987146233,  -1483434957,
               -1231316179,    572399164,  -1836611819,    552200649,   1238290055,    -11184726,   2015897680,   2061492133,
               -1886614525,   -123625127,  -2138470135,    386731290,   -624967835,    837215959,   -968736124,  -1201116976,
               -1019133566,  -1332111063,   1999449434,    286199582,   -877612933,    -61582168,   -692339859,    974525996,

                // s_T2
               1667483301,   2088564868,   2004348569,   2071721613,   -218956019,   1802229437,   1869602481,   -976907948,
                 808476752,     16843267,   1734856361,    724260477,    -16849127,   -673729182,  -1414836762,   1987505306,
                -892694715,  -2105401443,   -909539008,   2105408135,    -84218091,   1499050731,   1195871945,   -252642549,
               -1381154324,   -724257945,  -1566416899,  -1347467798,  -1667488833,  -1532734473,   1920132246,  -1061119141,
               -1212713534,    -33693412,  -1819066962,    640044138,    909536346,   1061125697,   -134744830,   -859012273,
                 875849820,  -1515892236,   -437923532,   -235800312,   1903288979,   -656888973,    825320019,    353708607,
                  67373068,   -943221422,    589514341,  -1010590370,    404238376,  -1768540255,     84216335,  -1701171275,
                 117902857,    303178806,  -2139087973,   -488448195,   -336868058,    656887401,  -1296924723,   1970662047,
                 151589403,  -2088559202,    741103732,    437924910,    454768173,   1852759218,   1515893998,  -1600103429,
                1381147894,    993752653,   -690571423,  -1280082482,    690573947,   -471605954,    791633521,  -2071719017,
                1397991157,   -774784664,            0,   -303185620,    538984544,    -50535649,  -1313769016,   1532737261,
                1785386174,   -875852474,  -1094817831,    960066123,   1246401758,   1280088276,   1482207464,   -808483510,
                -791626901,   -269499094,  -1431679003,    -67375850,   1128498885,   1296931543,    859006549,  -2054876780,
                1162185423,   -101062384,     33686534,   2139094657,   1347461360,   1010595908,  -1616960070,  -1465365533,
                1364304627,  -1549574658,   1077969088,  -1886452342,  -1835909203,  -1650646596,    943222856,   -168431356,
               -1128504353,  -1229555775,   -623202443,    555827811,    269492272,        -6886,   -202113778,   -757940371,
                -842170036,    202119188,    320022069,   -320027857,   1600110305,  -1751698014,   1145342156,    387395129,
                -993750185,  -1482205710,   2122251394,   1027439175,   1684326572,   1566423783,    421081643,   1936975509,
                1616953504,  -2122245736,   1330618065,   -589520001,    572671078,    707417214,  -1869595733,  -2004350077,
                1179028682,   -286341335,  -1195873325,    336865340,   -555833479,   1583267042,    185275933,   -606360202,
                -522134725,    842163286,    976909390,    168432670,   1229558491,    101059594,    606357612,   1549580516,
               -1027432611,   -741098130,  -1397996561,   1650640038,  -1852753496,  -1785384540,   -454765769,   2038035083,
                -404237006,   -926381245,    926379609,   1835915959,  -1920138868,   -707415708,   1313774802,  -1448523296,
                1819072692,   1448520954,   -185273593,   -353710299,   1701169839,   2054878350,  -1364310039,    134746136,
               -1162186795,   2021191816,    623200879,    774790258,    471611428,  -1499047951,  -1263242297,   -960063663,
                -387396829,   -572677764,   1953818780,    522141217,   1263245021,  -1111662116,  -1953821306,  -1970663547,
                1886445712,   1044282434,  -1246400060,   1718013098,   1212715224,     50529797,   -151587071,    235805714,
                1633796771,    892693087,   1465364217,  -1179031088,  -2038032495,  -1044276904,    488454695,  -1633802311,
                -505292488,   -117904621,  -1734857805,    286335539,   1768542907,   -640046736,  -1903294583,  -1802226777,
               -1684329034,    505297954,  -2021190254,   -370554592,   -825325751,   1431677695,    673730680,   -538991238,
               -1936981105,  -1583261192,  -1987507840,    218962455,  -1077975590,   -421079247,   1111655622,   1751699640,
                1094812355,  -1718015568,    757946999,    252648977,  -1330611253,   1414834428,  -1145344554,    370551866,

                // s_T3
                1673962851,   2096661628,   2012125559,   2079755643,   -218165774,   1809235307,   1876865391,   -980331323,
                 811618352,     16909057,   1741597031,    727088427,    -18408962,   -675978537,  -1420958037,   1995217526,
                -896580150,  -2111857278,   -913751863,   2113570685,    -84994566,   1504897881,   1200539975,   -251982864,
               -1388188499,   -726439980,  -1570767454,  -1354372433,  -1675378788,  -1538000988,   1927583346,  -1063560256,
               -1217019209,    -35578627,  -1824674157,    642542118,    913070646,   1065238847,   -134937865,   -863809588,
                 879254580,  -1521355611,   -439274267,   -235337487,   1910674289,   -659852328,    828527409,    355090197,
                  67636228,   -946515257,    591815971,  -1013096765,    405809176,  -1774739050,     84545285,  -1708149350,
                 118360327,    304363026,  -2145674368,   -488686110,   -338876693,    659450151,  -1300247118,   1978310517,
                 152181513,  -2095210877,    743994412,    439627290,    456535323,   1859957358,   1521806938,  -1604584544,
                1386542674,    997608763,   -692624938,  -1283600717,    693271337,   -472039709,    794718511,  -2079090812,
                1403450707,   -776378159,            0,   -306107155,    541089824,    -52224004,  -1317418831,   1538714971,
                1792327274,   -879933749,  -1100490306,    963791673,   1251270218,   1285084236,   1487988824,   -813348145,
                -793023536,   -272291089,  -1437604438,    -68348165,   1132905795,   1301993293,    862344499,  -2062445435,
                1166724933,   -102166279,     33818114,   2147385727,   1352724560,   1014514748,  -1624917345,  -1471421528,
                1369633617,  -1554121053,   1082179648,  -1895462257,  -1841320558,  -1658733411,    946882616,   -168753931,
               -1134305348,  -1233665610,   -626035238,    557998881,    270544912,     -1762561,   -201519373,   -759206446,
                -847164211,    202904588,    321271059,   -322752532,   1606345055,  -1758092649,   1149815876,    388905239,
                -996976700,  -1487539545,   2130477694,   1031423805,   1690872932,   1572530013,    422718233,   1944491379,
                1623236704,  -2129028991,   1335808335,   -593264676,    574907938,    710180394,  -1875137648,  -2012511352,
                1183631942,   -288937490,  -1200893000,    338181140,   -559449634,   1589437022,    185998603,   -609388837,
                -522503200,    845436466,    980700730,    169090570,   1234361161,    101452294,    608726052,   1555620956,
               -1029743166,   -742560045,  -1404833876,   1657054818,  -1858492271,  -1791908715,   -455919644,   2045938553,
                -405458201,   -930397240,    929978679,   1843050349,  -1929278323,   -709794603,   1318900302,  -1454776151,
                1826141292,   1454176854,   -185399308,   -355523094,   1707781989,   2062847610,  -1371018834,    135272456,
               -1167075910,   2029029496,    625635109,    777810478,    473441308,  -1504185946,  -1267480652,   -963161658,
                -389340184,   -576619299,   1961401460,    524165407,   1268178251,  -1117659971,  -1962047861,  -1978694262,
                1893765232,   1048330814,  -1250835275,   1724688998,   1217452104,     50726147,   -151584266,    236720654,
                1640145761,    896163637,   1471084887,  -1184247623,  -2045275770,  -1046914879,    490350365,  -1641563746,
                -505857823,   -118811656,  -1741966440,    287453969,   1775418217,   -643206951,  -1912108658,  -1808554092,
               -1691502949,    507257374,  -2028629369,   -372694807,   -829994546,   1437269845,    676362280,   -542803233,
               -1945923700,  -1587939167,  -1995865975,    219813645,  -1083843905,   -422104602,   1115997762,   1758509160,
                1099088705,  -1725321063,    760903469,    253628687,  -1334064208,   1420360788,  -1150429509,    371997206,

                // s_T4
               -962239645,   -125535108,   -291932297,   -158499973,    -15863054,   -692229269,   -558796945,  -1856715323,
                1615867952,     33751297,   -827758745,   1451043627,   -417726722,  -1251813417,   1306962859,   -325421450,
               -1891251510,    530416258,  -1992242743,    -91783811,   -283772166,  -1293199015,  -1899411641,    -83103504,
                1106029997,  -1285040940,   1610457762,   1173008303,    599760028,   1408738468,   -459902350,  -1688485696,
                1975695287,   -518193667,   1034851219,   1282024998,   1817851446,   2118205247,   -184354825,  -2091922228,
                1750873140,   1374987685,   -785062427,   -116854287,   -493653647,  -1418471208,   1649619249,    708777237,
                 135005188,  -1789737017,   1181033251,  -1654733885,    807933976,    933336726,    168756485,    800430746,
                 235472647,    607523346,    463175808,   -549592350,   -853087253,   1315514151,   2144187058,   -358648459,
                 303761673,    496927619,   1484008492,    875436570,    908925723,   -592286098,  -1259447718,   1543217312,
               -1527360942,   1984772923,  -1218324778,   2110698419,   1383803177,   -583080989,   1584475951,    328696964,
               -1493871789,  -1184312879,            0,  -1054020115,   1080041504,   -484442884,   2043195825,  -1225958565,
                -725718422,  -1924740149,   1742323390,   1917532473,  -1797371318,  -1730917300,  -1326950312,  -2058694705,
               -1150562096,   -987041809,   1340451498,   -317260805,  -2033892541,  -1697166003,   1716859699,    294946181,
               -1966127803,   -384763399,     67502594,    -25067649,  -1594863536,   2017737788,    632987551,   1273211048,
               -1561112239,   1576969123,  -2134884288,     92966799,   1068339858,    566009245,   1883781176,   -251333131,
                1675607228,   2009183926,  -1351230758,   1113792801,    540020752,   -451215361,    -49351693,  -1083321646,
               -2125673011,    403966988,    641012499,  -1020269332,  -1092526241,    899848087,  -1999879100,    775493399,
               -1822964540,   1441965991,    -58556802,   2051489085,   -928226204,  -1159242403,    841685273,   -426413197,
               -1063231392,    429425025,  -1630449841,  -1551901476,   1147544098,   1417554474,   1001099408,    193169544,
               -1932900794,   -953553170,   1809037496,    675025940,  -1485185314,  -1126015394,    371002123,  -1384719397,
                -616832800,   1683370546,   1951283770,    337512970,  -1831122615,    201983494,   1215046692,  -1192993700,
               -1621245246,  -1116810285,   1139780780,   -995728798,    967348625,    832869781,   -751311644,   -225740423,
                -718084121,  -1958491960,   1851340599,   -625513107,     25988493,  -1318791723,  -1663938994,   1239460265,
                -659264404,  -1392880042,   -217582348,   -819598614,   -894474907,   -191989126,   1206496942,    270010376,
                1876277946,   -259491720,   1248797989,   1550986798,    941890588,   1475454630,   1942467764,  -1756248378,
                -886839064,  -1585652259,   -392399756,   1042358047,  -1763882165,   1641856445,    226921355,    260409994,
                -527404944,   2084716094,   1908716981,   -861247898,  -1864873912,    100991747,   -150866186,    470945294,
               -1029480095,   1784624437,  -1359390889,   1775286713,    395413126,  -1722236479,    975641885,    666476190,
                -650583583,   -351012616,    733190296,    573772049,   -759469719,  -1452221991,    126455438,    866620564,
                 766942107,   1008868894,    361924487,   -920589847,  -2025206066,  -1426107051,   1350051880,  -1518673953,
                  59739276,   1509466529,    159418761,    437718285,   1708834751,   -684595482,  -2067381694,   -793221016,
               -2101132991,    699439513,   1517759789,    504434447,   2076946608,  -1459858348,   1842789307,    742004246 };

        private static readonly int[] s_TF = new int[4 * 256]
            {
                        // s_TF1
                        99,          124,          119,          123,          242,          107,          111,          197,
                        48,            1,          103,           43,          254,          215,          171,          118,
                       202,          130,          201,          125,          250,           89,           71,          240,
                       173,          212,          162,          175,          156,          164,          114,          192,
                       183,          253,          147,           38,           54,           63,          247,          204,
                        52,          165,          229,          241,          113,          216,           49,           21,
                         4,          199,           35,          195,           24,          150,            5,          154,
                         7,           18,          128,          226,          235,           39,          178,          117,
                         9,          131,           44,           26,           27,          110,           90,          160,
                        82,           59,          214,          179,           41,          227,           47,          132,
                        83,          209,            0,          237,           32,          252,          177,           91,
                       106,          203,          190,           57,           74,           76,           88,          207,
                       208,          239,          170,          251,           67,           77,           51,          133,
                        69,          249,            2,          127,           80,           60,          159,          168,
                        81,          163,           64,          143,          146,          157,           56,          245,
                       188,          182,          218,           33,           16,          255,          243,          210,
                       205,           12,           19,          236,           95,          151,           68,           23,
                       196,          167,          126,           61,          100,           93,           25,          115,
                        96,          129,           79,          220,           34,           42,          144,          136,
                        70,          238,          184,           20,          222,           94,           11,          219,
                       224,           50,           58,           10,           73,            6,           36,           92,
                       194,          211,          172,           98,          145,          149,          228,          121,
                       231,          200,           55,          109,          141,          213,           78,          169,
                       108,           86,          244,          234,          101,          122,          174,            8,
                       186,          120,           37,           46,           28,          166,          180,          198,
                       232,          221,          116,           31,           75,          189,          139,          138,
                       112,           62,          181,          102,           72,            3,          246,           14,
                        97,           53,           87,          185,          134,          193,           29,          158,
                       225,          248,          152,           17,          105,          217,          142,          148,
                       155,           30,          135,          233,          206,           85,           40,          223,
                       140,          161,          137,           13,          191,          230,           66,          104,
                        65,          153,           45,           15,          176,           84,          187,           22,

                        // s_TF2
                     25344,        31744,        30464,        31488,        61952,        27392,        28416,        50432,
                     12288,          256,        26368,        11008,        65024,        55040,        43776,        30208,
                     51712,        33280,        51456,        32000,        64000,        22784,        18176,        61440,
                     44288,        54272,        41472,        44800,        39936,        41984,        29184,        49152,
                     46848,        64768,        37632,         9728,        13824,        16128,        63232,        52224,
                     13312,        42240,        58624,        61696,        28928,        55296,        12544,         5376,
                      1024,        50944,         8960,        49920,         6144,        38400,         1280,        39424,
                      1792,         4608,        32768,        57856,        60160,         9984,        45568,        29952,
                      2304,        33536,        11264,         6656,         6912,        28160,        23040,        40960,
                     20992,        15104,        54784,        45824,        10496,        58112,        12032,        33792,
                     21248,        53504,            0,        60672,         8192,        64512,        45312,        23296,
                     27136,        51968,        48640,        14592,        18944,        19456,        22528,        52992,
                     53248,        61184,        43520,        64256,        17152,        19712,        13056,        34048,
                     17664,        63744,          512,        32512,        20480,        15360,        40704,        43008,
                     20736,        41728,        16384,        36608,        37376,        40192,        14336,        62720,
                     48128,        46592,        55808,         8448,         4096,        65280,        62208,        53760,
                     52480,         3072,         4864,        60416,        24320,        38656,        17408,         5888,
                     50176,        42752,        32256,        15616,        25600,        23808,         6400,        29440,
                     24576,        33024,        20224,        56320,         8704,        10752,        36864,        34816,
                     17920,        60928,        47104,         5120,        56832,        24064,         2816,        56064,
                     57344,        12800,        14848,         2560,        18688,         1536,         9216,        23552,
                     49664,        54016,        44032,        25088,        37120,        38144,        58368,        30976,
                     59136,        51200,        14080,        27904,        36096,        54528,        19968,        43264,
                     27648,        22016,        62464,        59904,        25856,        31232,        44544,         2048,
                     47616,        30720,         9472,        11776,         7168,        42496,        46080,        50688,
                     59392,        56576,        29696,         7936,        19200,        48384,        35584,        35328,
                     28672,        15872,        46336,        26112,        18432,          768,        62976,         3584,
                     24832,        13568,        22272,        47360,        34304,        49408,         7424,        40448,
                     57600,        63488,        38912,         4352,        26880,        55552,        36352,        37888,
                     39680,         7680,        34560,        59648,        52736,        21760,        10240,        57088,
                     35840,        41216,        35072,         3328,        48896,        58880,        16896,        26624,
                     16640,        39168,        11520,         3840,        45056,        21504,        47872,         5632,

                        // s_TF3
                   6488064,      8126464,      7798784,      8060928,     15859712,      7012352,      7274496,     12910592,
                   3145728,        65536,      6750208,      2818048,     16646144,     14090240,     11206656,      7733248,
                  13238272,      8519680,     13172736,      8192000,     16384000,      5832704,      4653056,     15728640,
                  11337728,     13893632,     10616832,     11468800,     10223616,     10747904,      7471104,     12582912,
                  11993088,     16580608,      9633792,      2490368,      3538944,      4128768,     16187392,     13369344,
                   3407872,     10813440,     15007744,     15794176,      7405568,     14155776,      3211264,      1376256,
                    262144,     13041664,      2293760,     12779520,      1572864,      9830400,       327680,     10092544,
                    458752,      1179648,      8388608,     14811136,     15400960,      2555904,     11665408,      7667712,
                    589824,      8585216,      2883584,      1703936,      1769472,      7208960,      5898240,     10485760,
                   5373952,      3866624,     14024704,     11730944,      2686976,     14876672,      3080192,      8650752,
                   5439488,     13697024,            0,     15532032,      2097152,     16515072,     11599872,      5963776,
                   6946816,     13303808,     12451840,      3735552,      4849664,      4980736,      5767168,     13565952,
                  13631488,     15663104,     11141120,     16449536,      4390912,      5046272,      3342336,      8716288,
                   4521984,     16318464,       131072,      8323072,      5242880,      3932160,     10420224,     11010048,
                   5308416,     10682368,      4194304,      9371648,      9568256,     10289152,      3670016,     16056320,
                  12320768,     11927552,     14286848,      2162688,      1048576,     16711680,     15925248,     13762560,
                  13434880,       786432,      1245184,     15466496,      6225920,      9895936,      4456448,      1507328,
                  12845056,     10944512,      8257536,      3997696,      6553600,      6094848,      1638400,      7536640,
                   6291456,      8454144,      5177344,     14417920,      2228224,      2752512,      9437184,      8912896,
                   4587520,     15597568,     12058624,      1310720,     14548992,      6160384,       720896,     14352384,
                  14680064,      3276800,      3801088,       655360,      4784128,       393216,      2359296,      6029312,
                  12713984,     13828096,     11272192,      6422528,      9502720,      9764864,     14942208,      7929856,
                  15138816,     13107200,      3604480,      7143424,      9240576,     13959168,      5111808,     11075584,
                   7077888,      5636096,     15990784,     15335424,      6619136,      7995392,     11403264,       524288,
                  12189696,      7864320,      2424832,      3014656,      1835008,     10878976,     11796480,     12976128,
                  15204352,     14483456,      7602176,      2031616,      4915200,     12386304,      9109504,      9043968,
                   7340032,      4063232,     11862016,      6684672,      4718592,       196608,     16121856,       917504,
                   6356992,      3473408,      5701632,     12124160,      8781824,     12648448,      1900544,     10354688,
                  14745600,     16252928,      9961472,      1114112,      6881280,     14221312,      9306112,      9699328,
                  10158080,      1966080,      8847360,     15269888,     13500416,      5570560,      2621440,     14614528,
                   9175040,     10551296,      8978432,       851968,     12517376,     15073280,      4325376,      6815744,
                   4259840,     10027008,      2949120,       983040,     11534336,      5505024,     12255232,      1441792,

                        // s_TF4
                1660944384,   2080374784,   1996488704,   2063597568,   -234881024,   1795162112,   1862270976,   -989855744,
                 805306368,     16777216,   1728053248,    721420288,    -33554432,   -687865856,  -1426063360,   1979711488,
                -905969664,  -2113929216,   -922746880,   2097152000,   -100663296,   1493172224,   1191182336,   -268435456,
               -1392508928,   -738197504,  -1577058304,  -1358954496,  -1677721600,  -1543503872,   1912602624,  -1073741824,
               -1224736768,    -50331648,  -1828716544,    637534208,    905969664,   1056964608,   -150994944,   -872415232,
                 872415232,  -1526726656,   -452984832,   -251658240,   1895825408,   -671088640,    822083584,    352321536,
                  67108864,   -956301312,    587202560,  -1023410176,    402653184,  -1778384896,     83886080,  -1711276032,
                 117440512,    301989888,  -2147483648,   -503316480,   -352321536,    654311424,  -1308622848,   1962934272,
                 150994944,  -2097152000,    738197504,    436207616,    452984832,   1845493760,   1509949440,  -1610612736,
                1375731712,    989855744,   -704643072,  -1291845632,    687865856,   -486539264,    788529152,  -2080374784,
                1392508928,   -788529152,            0,   -318767104,    536870912,    -67108864,  -1325400064,   1526726656,
                1778384896,   -889192448,  -1107296256,    956301312,   1241513984,   1275068416,   1476395008,   -822083584,
                -805306368,   -285212672,  -1442840576,    -83886080,   1124073472,   1291845632,    855638016,  -2063597568,
                1157627904,   -117440512,     33554432,   2130706432,   1342177280,   1006632960,  -1627389952,  -1476395008,
                1358954496,  -1560281088,   1073741824,  -1895825408,  -1845493760,  -1660944384,    939524096,   -184549376,
               -1140850688,  -1241513984,   -637534208,    553648128,    268435456,    -16777216,   -218103808,   -771751936,
                -855638016,    201326592,    318767104,   -335544320,   1593835520,  -1761607680,   1140850688,    385875968,
               -1006632960,  -1493172224,   2113929216,   1023410176,   1677721600,   1560281088,    419430400,   1929379840,
                1610612736,  -2130706432,   1325400064,   -603979776,    570425344,    704643072,  -1879048192,  -2013265920,
                1174405120,   -301989888,  -1207959552,    335544320,   -570425344,   1577058304,    184549376,   -620756992,
                -536870912,    838860800,    973078528,    167772160,   1224736768,    100663296,    603979776,   1543503872,
               -1040187392,   -754974720,  -1409286144,   1644167168,  -1862270976,  -1795162112,   -469762048,   2030043136,
                -419430400,   -939524096,    922746880,   1828716544,  -1929379840,   -721420288,   1308622848,  -1459617792,
                1811939328,   1442840576,   -201326592,   -369098752,   1694498816,   2046820352,  -1375731712,    134217728,
               -1174405120,   2013265920,    620756992,    771751936,    469762048,  -1509949440,  -1275068416,   -973078528,
                -402653184,   -587202560,   1946157056,    520093696,   1258291200,  -1124073472,  -1962934272,  -1979711488,
                1879048192,   1040187392,  -1258291200,   1711276032,   1207959552,     50331648,   -167772160,    234881024,
                1627389952,    889192448,   1459617792,  -1191182336,  -2046820352,  -1056964608,    486539264,  -1644167168,
                -520093696,   -134217728,  -1744830464,    285212672,   1761607680,   -654311424,  -1912602624,  -1811939328,
               -1694498816,    503316480,  -2030043136,   -385875968,   -838860800,   1426063360,    671088640,   -553648128,
               -1946157056,  -1593835520,  -1996488704,    218103808,  -1090519040,   -436207616,   1107296256,   1744830464,
                1090519040,  -1728053248,    754974720,    251658240,  -1342177280,   1409286144,  -1157627904,    369098752 };

        private static readonly int[] s_iT = new int[4 * 256]
            {
                // s_iT1
                1353184337,   1399144830,  -1012656358,  -1772214470,   -882136261,   -247096033,  -1420232020,  -1828461749,
                1442459680,   -160598355,  -1854485368,    625738485,    -52959921,   -674551099,  -2143013594,  -1885117771,
                1230680542,   1729870373,  -1743852987,   -507445667,     41234371,    317738113,  -1550367091,   -956705941,
                -413167869,  -1784901099,   -344298049,   -631680363,    763608788,   -752782248,    694804553,   1154009486,
                1787413109,   2021232372,   1799248025,   -579749593,  -1236278850,    397248752,   1722556617,  -1271214467,
                 407560035,  -2110711067,   1613975959,   1165972322,   -529046351,  -2068943941,    480281086,  -1809118983,
                1483229296,    436028815,  -2022908268,  -1208452270,    601060267,   -503166094,   1468997603,    715871590,
                 120122290,     63092015,  -1703164538,  -1526188077,   -226023376,  -1297760477,  -1167457534,   1552029421,
                 723308426,  -1833666137,   -252573709,  -1578997426,   -839591323,   -708967162,    526529745,  -1963022652,
               -1655493068,  -1604979806,    853641733,   1978398372,    971801355,  -1427152832,    111112542,   1360031421,
                -108388034,   1023860118,  -1375387939,   1186850381,  -1249028975,     90031217,   1876166148,    -15380384,
                 620468249,  -1746289194,   -868007799,   2006899047,  -1119688528,  -2004121337,    945494503,   -605108103,
                1191869601,   -384875908,   -920746760,            0,  -2088337399,   1223502642,  -1401941730,   1316117100,
                 -67170563,   1446544655,    517320253,    658058550,   1691946762,    564550760,   -783000677,    976107044,
               -1318647284,    266819475,   -761860428,  -1634624741,   1338359936,  -1574904735,   1766553434,    370807324,
                 179999714,   -450191168,   1138762300,    488053522,    185403662,  -1379431438,  -1180125651,   -928440812,
               -2061897385,   1275557295,  -1143105042,    -44007517,  -1624899081,  -1124765092,   -985962940,    880737115,
                1982415755,   -590994485,   1761406390,   1676797112,   -891538985,    277177154,   1076008723,    538035844,
                2099530373,   -130171950,    288553390,   1839278535,   1261411869,   -214912292,   -330136051,   -790380169,
                1813426987,  -1715900247,    -95906799,    577038663,   -997393240,    440397984,   -668172970,   -275762398,
                -951170681,  -1043253031,    -22885748,    906744984,   -813566554,    685669029,    646887386,  -1530942145,
                -459458004,    227702864,  -1681105046,   1648787028,  -1038905866,   -390539120,   1593260334,   -173030526,
               -1098883681,   2090061929,  -1456614033,  -1290656305,    999926984,  -1484974064,   1852021992,   2075868123,
                 158869197,   -199730834,     28809964,  -1466282109,   1701746150,   2129067946,    147831841,   -420997649,
                -644094022,   -835293366,   -737566742,   -696471511,  -1347247055,    824393514,    815048134,  -1067015627,
                 935087732,  -1496677636,  -1328508704,    366520115,   1251476721,   -136647615,    240176511,    804688151,
               -1915335306,   1303441219,   1414376140,   -553347356,   -474623586,    461924940,  -1205916479,   2136040774,
                  82468509,   1563790337,   1937016826,    776014843,   1511876531,   1389550482,    861278441,    323475053,
               -1939744870,   2047648055,  -1911228327,  -1992551445,   -299390514,    902390199,   -303751967,   1018251130,
                1507840668,   1064563285,   2043548696,  -1086863501,   -355600557,   1537932639,    342834655,  -2032450440,
               -2114736182,   1053059257,    741614648,   1598071746,   1925389590,    203809468,  -1958134744,   1100287487,
                1895934009,   -558691320,  -1662733096,  -1866377628,   1636092795,   1890988757,   1952214088,   1113045200,

                // s_iT2
               -1477160624,   1698790995,  -1541989693,   1579629206,   1806384075,   1167925233,   1492823211,     65227667,
                 -97509291,   1836494326,   1993115793,   1275262245,   -672837636,   -886389289,   1144333952,  -1553812081,
                1521606217,    465184103,    250234264,  -1057071647,   1966064386,   -263421678,  -1756983901,   -103584826,
                1603208167,  -1668147819,   2054012907,   1498584538,  -2084645843,    561273043,   1776306473,   -926314940,
               -1983744662,   2039411832,   1045993835,   1907959773,   1340194486,  -1383534569,  -1407137434,    986611124,
                1256153880,    823846274,    860985184,   2136171077,   2003087840,  -1368671356,  -1602093540,    722008468,
                1749577816,    -45773031,   1826526343,   -126135625,   -747394269,     38499042,  -1893735593,  -1420466646,
                 686535175,  -1028313341,   2076542618,    137876389,  -2027409166,  -1514200142,   1778582202,  -2112426660,
                 483363371,  -1267095662,   -234359824,   -496415071,   -187013683,  -1106966827,   1647628575,    -22625142,
                1395537053,   1442030240,   -511048398,   -336157579,   -326956231,   -278904662,  -1619960314,    275692881,
               -1977532679,    115185213,     88006062,  -1108980410,  -1923837515,   1573155077,   -737803153,    357589247,
                 -73918172,   -373434729,   1128303052,  -1629919369,   1122545853,  -1953953912,   1528424248,   -288851493,
                 175939911,    256015593,    512030921,            0,  -2038429309,   -315936184,   1880170156,   1918528590,
                 -15794693,    948244310,   -710001378,    959264295,   -653325724,  -1503893471,   1415289809,    775300154,
                1728711857,   -413691121,  -1762741038,  -1852105826,   -977239985,    551313826,   1266113129,    437394454,
               -1164713462,    715178213,   -534627261,    387650077,    218697227,   -947129683,  -1464455751,  -1457646392,
                 435246981,    125153100,   -577114437,   1618977789,    637663135,   -177054532,    996558021,   2130402100,
                 692292470,   -970732580,    -51530136,   -236668829,   -600713270,  -2057092592,    580326208,    298222624,
                 608863613,   1035719416,    855223825,  -1591097491,    798891339,    817028339,   1384517100,   -473860144,
                 380840812,  -1183798887,   1217663482,   1693009698,  -1929598780,   1072734234,    746411736,  -1875696913,
                1313441735,   -784803391,  -1563783938,    198481974,  -2114607409,   -562387672,  -1900553690,  -1079165020,
               -1657131804,  -1837608947,   -866162021,   1182684258,    328070850,  -1193766680,   -147247522,  -1346141451,
               -2141347906,  -1815058052,    768962473,    304467891,  -1716729797,   2098729127,   1671227502,  -1153705093,
                2015808777,    408514292,  -1214583807,  -1706064984,   1855317605,   -419452290,   -809754360,   -401215514,
               -1679312167,    913263310,    161475284,   2091919830,  -1297862225,    591342129,  -1801075152,   1721906624,
               -1135709129,   -897385306,   -795811664,   -660131051,  -1744506550,   -622050825,   1355644686,   -158263505,
                -699566451,  -1326496947,   1303039060,     76997855,  -1244553501,  -2006299621,    523026872,   1365591679,
                -362898172,    898367837,   1955068531,   1091304238,    493335386,   -757362094,   1443948851,   1205234963,
                1641519756,    211892090,    351820174,   1007938441,    665439982,   -916342987,   -451091987,  -1320715716,
                -539845543,   1945261375,   -837543815,    935818175,   -839429142,  -1426235557,   1866325780,   -616269690,
                -206583167,   -999769794,    874788908,   1084473951,  -1021503886,    635616268,   1228679307,  -1794244799,
                  27801969,  -1291056930,   -457910116,  -1051302768,  -2067039391,  -1238182544,   1550600308,   1471729730,

                // s_iT3
                -195997529,   1098797925,    387629988,    658151006,  -1422144661,  -1658851003,    -89347240,   -481586429,
                 807425530,   1991112301,   -863465098,     49620300,   -447742761,    717608907,    891715652,   1656065955,
               -1310832294,  -1171953893,   -364537842,    -27401792,    801309301,   1283527408,   1183687575,   -747911431,
               -1895569569,  -1844079204,   1841294202,   1385552473,  -1093390973,   1951978273,   -532076183,   -913423160,
               -1032492407,  -1896580999,   1486449470,  -1188569743,   -507595185,  -1997531219,    550069932,   -830622662,
                -547153846,    451248689,   1368875059,   1398949247,   1689378935,   1807451310,  -2114052960,    150574123,
                1215322216,   1167006205,   -560691348,   2069018616,   1940595667,   1265820162,    534992783,   1432758955,
                -340654296,  -1255210046,   -981034373,    936617224,    674296455,  -1088179547,     50510442,    384654466,
                -813028580,   2041025204,    133427442,   1766760930,   -630862348,     84334014,    886120290,  -1497068802,
                 775200083,   -207445931,  -1979370783,   -156994069,  -2096416276,   1614850799,   1901987487,   1857900816,
                 557775242,   -577356538,   1054715397,   -431143235,   1418835341,   -999226019,    100954068,   1348534037,
               -1743182597,  -1110009879,   1082772547,   -647530594,   -391070398,  -1995994997,    434583643,   -931537938,
                2090944266,   1115482383,  -2064070370,            0,  -2146860154,    724715757,    287222896,   1517047410,
                 251526143,  -2062592456,  -1371726123,    758523705,    252339417,   1550328230,   1536938324,    908343854,
                 168604007,   1469255655,   -290139498,  -1692688751,  -1065332795,   -597581280,   2002413899,    303830554,
               -1813902662,  -1597971158,    574374880,    454171927,    151915277,  -1947030073,  -1238517336,    504678569,
                -245922535,   1974422535,  -1712407587,   2141453664,     33005350,   1918680309,   1715782971,    -77908866,
                1133213225,    600562886,   -306812676,   -457677839,    836225756,   1665273989,  -1760346078,   -964419567,
                1250262308,  -1143801795,   -106032846,    700935585,  -1642247377,  -1294142672,  -2045907886,  -1049112349,
               -1288999914,   1890163129,  -1810761144,   -381214108,    -56048500,   -257942977,   2102843436,    857927568,
                1233635150,    953795025,   -896729438,   -728222197,   -173617279,   2057644254,  -1210440050,  -1388337985,
                 976020637,   2018512274,   1600822220,   2119459398,  -1913208301,   -661591880,    959340279,  -1014827601,
                1570750080,   -798393197,   -714102483,    634368786,  -1396163687,    403744637,  -1662488989,   1004239803,
                 650971512,   1500443672,  -1695809097,   1334028442,  -1780062866,     -5603610,  -1138685745,    368043752,
                -407184997,   1867173430,  -1612000247,  -1339435396,  -1540247630,   1059729699,  -1513738092,  -1573535642,
                1316239292,  -2097371446,  -1864322864,  -1489824296,     82922136,   -331221030,   -847311280,  -1860751370,
                1299615190,   -280801872,  -1429449651,  -1763385596,   -778116171,   1783372680,    750893087,   1699118929,
                1587348714,  -1946067659,  -2013629580,    201010753,   1739807261,   -611167534,    283718486,   -697494713,
                -677737375,  -1590199796,   -128348652,    334203196,  -1446056409,   1639396809,    484568549,   1199193265,
                -761505313,   -229294221,    337148366,   -948715721,   -145495347,    -44082262,   1038029935,   1148749531,
               -1345682957,   1756970692,    607661108,  -1547542720,    488010435,   -490992603,   1009290057,    234832277,
               -1472630527,    201907891,  -1260872476,   1449431233,   -881106556,    852848822,   1816687708,  -1194311081,

                // s_iT4
                1364240372,   2119394625,    449029143,    982933031,   1003187115,    535905693,  -1398056710,   1267925987,
                 542505520,  -1376359050,  -2003732788,   -182105086,   1341970405,   -975713494,    645940277,  -1248877726,
                -565617999,    627514298,   1167593194,   1575076094,  -1023249105,  -2129465268,  -1918658746,   1808202195,
                  65494927,    362126482,  -1075086739,  -1780852398,   -735214658,   1490231668,   1227450848,  -1908094775,
                1969916354,   -193431154,  -1721024936,    668823993,  -1095348255,   -266883704,   -916018144,   2108963534,
                1662536415,   -444452582,  -1755303087,   1648721747,  -1310689436,  -1148932501,    -31678335,   -107730168,
                1884842056,  -1894122171,  -1803064098,   1387788411,  -1423715469,   1927414347,   -480800993,   1714072405,
               -1308153621,    788775605,  -2036696123,   -744159177,    821200680,    598910399,     45771267,   -312704490,
               -1976886065,  -1483557767,   -202313209,   1319232105,   1707996378,    114671109,   -786472396,   -997523802,
                 882725678,  -1566550541,     87220618,  -1535775754,    188345475,   1084944224,   1577492337,  -1118760850,
                1056541217,  -1774385443,   -575797954,   1296481766,  -1850372780,   1896177092,     74437638,   1627329872,
                 421854104,   -694687299,  -1983102144,   1735892697,  -1329773848,    126389129,   -415737063,   2044456648,
               -1589179780,   2095648578,   -121037180,            0,    159614592,    843640107,    514617361,   1817080410,
                 -33816818,    257308805,   1025430958,    908540205,    174381327,   1747035740,  -1680780197,    607792694,
                 212952842,  -1827674281,  -1261267218,    463376795,  -2142255680,   1638015196,   1516850039,    471210514,
                -502613357,  -1058723168,   1011081250,    303896347,    235605257,   -223492213,    767142070,    348694814,
                1468340721,  -1353971851,   -289677927,  -1543675777,   -140564991,   1555887474,   1153776486,   1530167035,
               -1955190461,   -874723805,  -1234633491,  -1201409564,   -674571215,   1108378979,    322970263,  -2078273082,
               -2055396278,   -755483205,  -1374604551,   -949116631,    491466654,   -588042062,    233591430,   2010178497,
                 728503987,  -1449543312,    301615252,   1193436393,  -1463513860,  -1608892432,   1457007741,    586125363,
               -2016981431,   -641609416,  -1929469238,  -1741288492,  -1496350219,  -1524048262,   -635007305,   1067761581,
                 753179962,   1343066744,   1788595295,   1415726718,   -155053171,  -1863796520,    777975609,  -2097827901,
               -1614905251,   1769771984,   1873358293,   -810347995,   -935618132,    279411992,   -395418724,   -612648133,
                -855017434,   1861490777,   -335431782,  -2086102449,   -429560171,  -1434523905,    554225596,   -270079979,
               -1160143897,   1255028335,   -355202657,    701922480,    833598116,    707863359,   -969894747,    901801634,
                1949809742,    -56178046,   -525283184,    857069735,   -246769660,   1106762476,   2131644621,    389019281,
                1989006925,   1129165039,   -866890326,   -455146346,  -1629243951,   1276872810,  -1044898004,   1182749029,
               -1660622242,     22885772,    -93096825,    -80854773,  -1285939865,  -1840065829,   -382511600,   1829980118,
               -1702075945,    930745505,   1502483704,   -343327725,   -823253079,  -1221211807,   -504503012,   2050797895,
               -1671831598,   1430221810,    410635796,   1941911495,   1407897079,   1599843069,   -552308931,   2022103876,
                -897453137,  -1187068824,    942421028,  -1033944925,    376619805,  -1140054558,    680216892,    -12479219,
                 963707304,    148812556,   -660806476,   1687208278,   2069988555,   -714033614,   1215585388,   -800958536 };

        private static readonly int[] s_iTF = new int[4 * 256]
            {
                        // s_iTF1
                        82,            9,          106,          213,           48,           54,          165,           56,
                       191,           64,          163,          158,          129,          243,          215,          251,
                       124,          227,           57,          130,          155,           47,          255,          135,
                        52,          142,           67,           68,          196,          222,          233,          203,
                        84,          123,          148,           50,          166,          194,           35,           61,
                       238,           76,          149,           11,           66,          250,          195,           78,
                         8,           46,          161,          102,           40,          217,           36,          178,
                       118,           91,          162,           73,          109,          139,          209,           37,
                       114,          248,          246,          100,          134,          104,          152,           22,
                       212,          164,           92,          204,           93,          101,          182,          146,
                       108,          112,           72,           80,          253,          237,          185,          218,
                        94,           21,           70,           87,          167,          141,          157,          132,
                       144,          216,          171,            0,          140,          188,          211,           10,
                       247,          228,           88,            5,          184,          179,           69,            6,
                       208,           44,           30,          143,          202,           63,           15,            2,
                       193,          175,          189,            3,            1,           19,          138,          107,
                        58,          145,           17,           65,           79,          103,          220,          234,
                       151,          242,          207,          206,          240,          180,          230,          115,
                       150,          172,          116,           34,          231,          173,           53,          133,
                       226,          249,           55,          232,           28,          117,          223,          110,
                        71,          241,           26,          113,           29,           41,          197,          137,
                       111,          183,           98,           14,          170,           24,          190,           27,
                       252,           86,           62,           75,          198,          210,          121,           32,
                       154,          219,          192,          254,          120,          205,           90,          244,
                        31,          221,          168,           51,          136,            7,          199,           49,
                       177,           18,           16,           89,           39,          128,          236,           95,
                        96,           81,          127,          169,           25,          181,           74,           13,
                        45,          229,          122,          159,          147,          201,          156,          239,
                       160,          224,           59,           77,          174,           42,          245,          176,
                       200,          235,          187,           60,          131,           83,          153,           97,
                        23,           43,            4,          126,          186,          119,          214,           38,
                       225,          105,           20,           99,           85,           33,           12,          125,

                        // s_iTF2
                     20992,         2304,        27136,        54528,        12288,        13824,        42240,        14336,
                     48896,        16384,        41728,        40448,        33024,        62208,        55040,        64256,
                     31744,        58112,        14592,        33280,        39680,        12032,        65280,        34560,
                     13312,        36352,        17152,        17408,        50176,        56832,        59648,        51968,
                     21504,        31488,        37888,        12800,        42496,        49664,         8960,        15616,
                     60928,        19456,        38144,         2816,        16896,        64000,        49920,        19968,
                      2048,        11776,        41216,        26112,        10240,        55552,         9216,        45568,
                     30208,        23296,        41472,        18688,        27904,        35584,        53504,         9472,
                     29184,        63488,        62976,        25600,        34304,        26624,        38912,         5632,
                     54272,        41984,        23552,        52224,        23808,        25856,        46592,        37376,
                     27648,        28672,        18432,        20480,        64768,        60672,        47360,        55808,
                     24064,         5376,        17920,        22272,        42752,        36096,        40192,        33792,
                     36864,        55296,        43776,            0,        35840,        48128,        54016,         2560,
                     63232,        58368,        22528,         1280,        47104,        45824,        17664,         1536,
                     53248,        11264,         7680,        36608,        51712,        16128,         3840,          512,
                     49408,        44800,        48384,          768,          256,         4864,        35328,        27392,
                     14848,        37120,         4352,        16640,        20224,        26368,        56320,        59904,
                     38656,        61952,        52992,        52736,        61440,        46080,        58880,        29440,
                     38400,        44032,        29696,         8704,        59136,        44288,        13568,        34048,
                     57856,        63744,        14080,        59392,         7168,        29952,        57088,        28160,
                     18176,        61696,         6656,        28928,         7424,        10496,        50432,        35072,
                     28416,        46848,        25088,         3584,        43520,         6144,        48640,         6912,
                     64512,        22016,        15872,        19200,        50688,        53760,        30976,         8192,
                     39424,        56064,        49152,        65024,        30720,        52480,        23040,        62464,
                      7936,        56576,        43008,        13056,        34816,         1792,        50944,        12544,
                     45312,         4608,         4096,        22784,         9984,        32768,        60416,        24320,
                     24576,        20736,        32512,        43264,         6400,        46336,        18944,         3328,
                     11520,        58624,        31232,        40704,        37632,        51456,        39936,        61184,
                     40960,        57344,        15104,        19712,        44544,        10752,        62720,        45056,
                     51200,        60160,        47872,        15360,        33536,        21248,        39168,        24832,
                      5888,        11008,         1024,        32256,        47616,        30464,        54784,         9728,
                     57600,        26880,         5120,        25344,        21760,         8448,         3072,        32000,

                        // s_iTF3
                   5373952,       589824,      6946816,     13959168,      3145728,      3538944,     10813440,      3670016,
                  12517376,      4194304,     10682368,     10354688,      8454144,     15925248,     14090240,     16449536,
                   8126464,     14876672,      3735552,      8519680,     10158080,      3080192,     16711680,      8847360,
                   3407872,      9306112,      4390912,      4456448,     12845056,     14548992,     15269888,     13303808,
                   5505024,      8060928,      9699328,      3276800,     10878976,     12713984,      2293760,      3997696,
                  15597568,      4980736,      9764864,       720896,      4325376,     16384000,     12779520,      5111808,
                    524288,      3014656,     10551296,      6684672,      2621440,     14221312,      2359296,     11665408,
                   7733248,      5963776,     10616832,      4784128,      7143424,      9109504,     13697024,      2424832,
                   7471104,     16252928,     16121856,      6553600,      8781824,      6815744,      9961472,      1441792,
                  13893632,     10747904,      6029312,     13369344,      6094848,      6619136,     11927552,      9568256,
                   7077888,      7340032,      4718592,      5242880,     16580608,     15532032,     12124160,     14286848,
                   6160384,      1376256,      4587520,      5701632,     10944512,      9240576,     10289152,      8650752,
                   9437184,     14155776,     11206656,            0,      9175040,     12320768,     13828096,       655360,
                  16187392,     14942208,      5767168,       327680,     12058624,     11730944,      4521984,       393216,
                  13631488,      2883584,      1966080,      9371648,     13238272,      4128768,       983040,       131072,
                  12648448,     11468800,     12386304,       196608,        65536,      1245184,      9043968,      7012352,
                   3801088,      9502720,      1114112,      4259840,      5177344,      6750208,     14417920,     15335424,
                   9895936,     15859712,     13565952,     13500416,     15728640,     11796480,     15073280,      7536640,
                   9830400,     11272192,      7602176,      2228224,     15138816,     11337728,      3473408,      8716288,
                  14811136,     16318464,      3604480,     15204352,      1835008,      7667712,     14614528,      7208960,
                   4653056,     15794176,      1703936,      7405568,      1900544,      2686976,     12910592,      8978432,
                   7274496,     11993088,      6422528,       917504,     11141120,      1572864,     12451840,      1769472,
                  16515072,      5636096,      4063232,      4915200,     12976128,     13762560,      7929856,      2097152,
                  10092544,     14352384,     12582912,     16646144,      7864320,     13434880,      5898240,     15990784,
                   2031616,     14483456,     11010048,      3342336,      8912896,       458752,     13041664,      3211264,
                  11599872,      1179648,      1048576,      5832704,      2555904,      8388608,     15466496,      6225920,
                   6291456,      5308416,      8323072,     11075584,      1638400,     11862016,      4849664,       851968,
                   2949120,     15007744,      7995392,     10420224,      9633792,     13172736,     10223616,     15663104,
                  10485760,     14680064,      3866624,      5046272,     11403264,      2752512,     16056320,     11534336,
                  13107200,     15400960,     12255232,      3932160,      8585216,      5439488,     10027008,      6356992,
                   1507328,      2818048,       262144,      8257536,     12189696,      7798784,     14024704,      2490368,
                  14745600,      6881280,      1310720,      6488064,      5570560,      2162688,       786432,      8192000,

                        // s_iTF4
                1375731712,    150994944,   1778384896,   -721420288,    805306368,    905969664,  -1526726656,    939524096,
               -1090519040,   1073741824,  -1560281088,  -1644167168,  -2130706432,   -218103808,   -687865856,    -83886080,
                2080374784,   -486539264,    956301312,  -2113929216,  -1694498816,    788529152,    -16777216,  -2030043136,
                 872415232,  -1912602624,   1124073472,   1140850688,  -1006632960,   -570425344,   -385875968,   -889192448,
                1409286144,   2063597568,  -1811939328,    838860800,  -1509949440,  -1040187392,    587202560,   1023410176,
                -301989888,   1275068416,  -1795162112,    184549376,   1107296256,   -100663296,  -1023410176,   1308622848,
                 134217728,    771751936,  -1593835520,   1711276032,    671088640,   -654311424,    603979776,  -1308622848,
                1979711488,   1526726656,  -1577058304,   1224736768,   1828716544,  -1962934272,   -788529152,    620756992,
                1912602624,   -134217728,   -167772160,   1677721600,  -2046820352,   1744830464,  -1744830464,    369098752,
                -738197504,  -1543503872,   1543503872,   -872415232,   1560281088,   1694498816,  -1241513984,  -1845493760,
                1811939328,   1879048192,   1207959552,   1342177280,    -50331648,   -318767104,  -1191182336,   -637534208,
                1577058304,    352321536,   1174405120,   1459617792,  -1493172224,  -1929379840,  -1660944384,  -2080374784,
               -1879048192,   -671088640,  -1426063360,            0,  -1946157056,  -1140850688,   -754974720,    167772160,
                -150994944,   -469762048,   1476395008,     83886080,  -1207959552,  -1291845632,   1157627904,    100663296,
                -805306368,    738197504,    503316480,  -1895825408,   -905969664,   1056964608,    251658240,     33554432,
               -1056964608,  -1358954496,  -1124073472,     50331648,     16777216,    318767104,  -1979711488,   1795162112,
                 973078528,  -1862270976,    285212672,   1090519040,   1325400064,   1728053248,   -603979776,   -369098752,
               -1761607680,   -234881024,   -822083584,   -838860800,   -268435456,  -1275068416,   -436207616,   1929379840,
               -1778384896,  -1409286144,   1946157056,    570425344,   -419430400,  -1392508928,    889192448,  -2063597568,
                -503316480,   -117440512,    922746880,   -402653184,    469762048,   1962934272,   -553648128,   1845493760,
                1191182336,   -251658240,    436207616,   1895825408,    486539264,    687865856,   -989855744,  -1996488704,
                1862270976,  -1224736768,   1644167168,    234881024,  -1442840576,    402653184,  -1107296256,    452984832,
                 -67108864,   1442840576,   1040187392,   1258291200,   -973078528,   -771751936,   2030043136,    536870912,
               -1711276032,   -620756992,  -1073741824,    -33554432,   2013265920,   -855638016,   1509949440,   -201326592,
                 520093696,   -587202560,  -1476395008,    855638016,  -2013265920,    117440512,   -956301312,    822083584,
               -1325400064,    301989888,    268435456,   1493172224,    654311424,  -2147483648,   -335544320,   1593835520,
                1610612736,   1358954496,   2130706432,  -1459617792,    419430400,  -1258291200,   1241513984,    218103808,
                 754974720,   -452984832,   2046820352,  -1627389952,  -1828716544,   -922746880,  -1677721600,   -285212672,
               -1610612736,   -536870912,    989855744,   1291845632,  -1375731712,    704643072,   -184549376,  -1342177280,
                -939524096,   -352321536,  -1157627904,   1006632960,  -2097152000,   1392508928,  -1728053248,   1627389952,
                 385875968,    721420288,     67108864,   2113929216,  -1174405120,   1996488704,   -704643072,    637534208,
                -520093696,   1761607680,    335544320,   1660944384,   1426063360,    553648128,    201326592,   2097152000 };
    }
}
