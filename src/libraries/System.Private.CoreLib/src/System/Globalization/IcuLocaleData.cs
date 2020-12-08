// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

#pragma warning disable SA1001

// This file contains the handling of Windows OS specific culture features.

namespace System.Globalization
{
    internal enum IcuLocaleDataParts
    {
        Lcid = 0,
        AnsiCodePage = 1,
        OemCodePage = 2,
        MacCodePage = 3,
        EbcdicCodePage = 4,
        GeoId = 5,
        DigitSubstitutionOrListSeparator = 6,
        SpecificLocaleIndex = 7,
        ConsoleLocaleIndex = 8
    }

    internal static class IcuLocaleData
    {
        /*
        // Program used to generate and validate the culture data
        // input data needs to come sorted
        static void GenerateData (string[] cultures, (string, string)[] lcids)
        {
            var list = new List<(string, List<string>)> ();

            string prev = null;
            for (int i = 0; i < cultures.Length; ++i) {
                var raw = cultures[i];

                List<string> values;

                if (i > 0 && raw.StartsWith (prev)) {
                    values = list[^1].Item2;
                    list[^1] = (raw, values);
                } else {
                    values = new List<string> ();
                    list.Add((raw, values));
                }

                values.Add(raw);
                prev = raw;
                continue;
            }

            Console.WriteLine("private static ReadOnlySpan<byte> CultureNames => new byte[]");
            Console.WriteLine("{");

            var indexes = new List<(int, int, string)> ();
            int pos = 0;
            for (int i = 0; i < list.Count; ++i) {
                var row = list[i];

                for (int ii = 0; ii < row.Item2.Count; ++ii)  {
                    string value = row.Item2[ii];
                    indexes.Add ((pos, value.Length, value));
                }

                foreach (var ch in row.Item1) {
                    Console.Write($"(byte)'{ch}', ");
                }
                Console.WriteLine($"\t// {string.Join(' ', row.Item2)}");

                pos += row.Item1.Length;
            }

            Console.WriteLine("};");

            Console.WriteLine("private static readonly ushort[] s_localesNamesIndexes = new ushort [] {");
            int max_length = 0;
            foreach (var entry in indexes) {
                Debug.Assert(entry.Item1 < Math.Pow (2,12));
                Debug.Assert(entry.Item2 < Math.Pow (2, 4));
                Console.WriteLine($"{entry.Item1} << 4 | {entry.Item2},\t// {entry.Item3}");

                max_length = Math.Max(max_length, entry.Item2);
            }
            Console.WriteLine("};");

            Console.WriteLine($"const int LocaleLongestName = {max_length};");

            Console.WriteLine("private static readonly int[] s_lcids = new int[]");
            foreach (var entry in lcids) {
                string name = entry.Item2.ToLowerInvariant();
                int entryIndex = indexes.FindIndex(l => l.Item3 == name);
                if (entryIndex < 0) {
                    Console.WriteLine($"{entry.Item2} // {name}");
                    continue;
                }

                var str = indexes[entryIndex];
                Debug.Assert(str.Item1 < Math.Pow (2, 12));
                Debug.Assert(str.Item2 < Math.Pow (2, 4));

                Console.WriteLine($"{entry.Item1} << 16 | {str.Item1} << 4 | {str.Item2},\t // {name}");
            }
            Console.WriteLine("};");
        }
        */

        private static ReadOnlySpan<byte> CultureNames => new byte[]
        {
            (byte)'a', (byte)'a', (byte)'-', (byte)'d', (byte)'j',  // aa, aa-dj
            (byte)'a', (byte)'a', (byte)'-', (byte)'e', (byte)'r',  // aa-er
            (byte)'a', (byte)'a', (byte)'-', (byte)'e', (byte)'t',  // aa-et
            (byte)'a', (byte)'f', (byte)'-', (byte)'n', (byte)'a',  // af, af-na
            (byte)'a', (byte)'f', (byte)'-', (byte)'z', (byte)'a',  // af-za
            (byte)'a', (byte)'g', (byte)'q', (byte)'-', (byte)'c', (byte)'m',  // agq, agq-cm
            (byte)'a', (byte)'k', (byte)'-', (byte)'g', (byte)'h',  // ak, ak-gh
            (byte)'a', (byte)'m', (byte)'-', (byte)'e', (byte)'t',  // am, am-et
            (byte)'a', (byte)'r', (byte)'-', (byte)'0', (byte)'0', (byte)'1',  // ar, ar-001
            (byte)'a', (byte)'r', (byte)'-', (byte)'a', (byte)'e',  // ar-ae
            (byte)'a', (byte)'r', (byte)'-', (byte)'b', (byte)'h',  // ar-bh
            (byte)'a', (byte)'r', (byte)'-', (byte)'d', (byte)'j',  // ar-dj
            (byte)'a', (byte)'r', (byte)'-', (byte)'d', (byte)'z',  // ar-dz
            (byte)'a', (byte)'r', (byte)'-', (byte)'e', (byte)'g',  // ar-eg
            (byte)'a', (byte)'r', (byte)'-', (byte)'e', (byte)'r',  // ar-er
            (byte)'a', (byte)'r', (byte)'-', (byte)'i', (byte)'l',  // ar-il
            (byte)'a', (byte)'r', (byte)'-', (byte)'i', (byte)'q',  // ar-iq
            (byte)'a', (byte)'r', (byte)'-', (byte)'j', (byte)'o',  // ar-jo
            (byte)'a', (byte)'r', (byte)'-', (byte)'k', (byte)'m',  // ar-km
            (byte)'a', (byte)'r', (byte)'-', (byte)'k', (byte)'w',  // ar-kw
            (byte)'a', (byte)'r', (byte)'-', (byte)'l', (byte)'b',  // ar-lb
            (byte)'a', (byte)'r', (byte)'-', (byte)'l', (byte)'y',  // ar-ly
            (byte)'a', (byte)'r', (byte)'-', (byte)'m', (byte)'a',  // ar-ma
            (byte)'a', (byte)'r', (byte)'-', (byte)'m', (byte)'r',  // ar-mr
            (byte)'a', (byte)'r', (byte)'-', (byte)'o', (byte)'m',  // ar-om
            (byte)'a', (byte)'r', (byte)'-', (byte)'p', (byte)'s',  // ar-ps
            (byte)'a', (byte)'r', (byte)'-', (byte)'q', (byte)'a',  // ar-qa
            (byte)'a', (byte)'r', (byte)'-', (byte)'s', (byte)'a',  // ar-sa
            (byte)'a', (byte)'r', (byte)'-', (byte)'s', (byte)'d',  // ar-sd
            (byte)'a', (byte)'r', (byte)'-', (byte)'s', (byte)'o',  // ar-so
            (byte)'a', (byte)'r', (byte)'-', (byte)'s', (byte)'s',  // ar-ss
            (byte)'a', (byte)'r', (byte)'-', (byte)'s', (byte)'y',  // ar-sy
            (byte)'a', (byte)'r', (byte)'-', (byte)'t', (byte)'d',  // ar-td
            (byte)'a', (byte)'r', (byte)'-', (byte)'t', (byte)'n',  // ar-tn
            (byte)'a', (byte)'r', (byte)'-', (byte)'y', (byte)'e',  // ar-ye
            (byte)'a', (byte)'r', (byte)'n', (byte)'-', (byte)'c', (byte)'l',  // arn, arn-cl
            (byte)'a', (byte)'s', (byte)'-', (byte)'i', (byte)'n',  // as, as-in
            (byte)'a', (byte)'s', (byte)'a', (byte)'-', (byte)'t', (byte)'z',  // asa, asa-tz
            (byte)'a', (byte)'s', (byte)'t', (byte)'-', (byte)'e', (byte)'s',  // ast, ast-es
            (byte)'a', (byte)'z', (byte)'-', (byte)'c', (byte)'y', (byte)'r', (byte)'l', (byte)'-', (byte)'a', (byte)'z',  // az, az-cyrl, az-cyrl-az
            (byte)'a', (byte)'z', (byte)'-', (byte)'l', (byte)'a', (byte)'t', (byte)'n', (byte)'-', (byte)'a', (byte)'z',  // az-latn, az-latn-az
            (byte)'b', (byte)'a', (byte)'-', (byte)'r', (byte)'u',  // ba, ba-ru
            (byte)'b', (byte)'a', (byte)'s', (byte)'-', (byte)'c', (byte)'m',  // bas, bas-cm
            (byte)'b', (byte)'e', (byte)'-', (byte)'b', (byte)'y',  // be, be-by
            (byte)'b', (byte)'e', (byte)'m', (byte)'-', (byte)'z', (byte)'m',  // bem, bem-zm
            (byte)'b', (byte)'e', (byte)'z', (byte)'-', (byte)'t', (byte)'z',  // bez, bez-tz
            (byte)'b', (byte)'g', (byte)'-', (byte)'b', (byte)'g',  // bg, bg-bg
            (byte)'b', (byte)'i', (byte)'n', (byte)'-', (byte)'n', (byte)'g',  // bin, bin-ng
            (byte)'b', (byte)'m', (byte)'-', (byte)'l', (byte)'a', (byte)'t', (byte)'n', (byte)'-', (byte)'m', (byte)'l',  // bm, bm-latn, bm-latn-ml
            (byte)'b', (byte)'n', (byte)'-', (byte)'b', (byte)'d',  // bn, bn-bd
            (byte)'b', (byte)'n', (byte)'-', (byte)'i', (byte)'n',  // bn-in
            (byte)'b', (byte)'o', (byte)'-', (byte)'c', (byte)'n',  // bo, bo-cn
            (byte)'b', (byte)'o', (byte)'-', (byte)'i', (byte)'n',  // bo-in
            (byte)'b', (byte)'r', (byte)'-', (byte)'f', (byte)'r',  // br, br-fr
            (byte)'b', (byte)'r', (byte)'x', (byte)'-', (byte)'i', (byte)'n',  // brx, brx-in
            (byte)'b', (byte)'s', (byte)'-', (byte)'c', (byte)'y', (byte)'r', (byte)'l', (byte)'-', (byte)'b', (byte)'a',  // bs, bs-cyrl, bs-cyrl-ba
            (byte)'b', (byte)'s', (byte)'-', (byte)'l', (byte)'a', (byte)'t', (byte)'n', (byte)'-', (byte)'b', (byte)'a',  // bs-latn, bs-latn-ba
            (byte)'b', (byte)'y', (byte)'n', (byte)'-', (byte)'e', (byte)'r',  // byn, byn-er
            (byte)'c', (byte)'a', (byte)'-', (byte)'a', (byte)'d',  // ca, ca-ad
            (byte)'c', (byte)'a', (byte)'-', (byte)'e', (byte)'s', (byte)'-', (byte)'v', (byte)'a', (byte)'l', (byte)'e', (byte)'n', (byte)'c', (byte)'i', (byte)'a',  // ca-es, ca-es-valencia
            (byte)'c', (byte)'a', (byte)'-', (byte)'f', (byte)'r',  // ca-fr
            (byte)'c', (byte)'a', (byte)'-', (byte)'i', (byte)'t',  // ca-it
            (byte)'c', (byte)'e', (byte)'-', (byte)'r', (byte)'u',  // ce, ce-ru
            (byte)'c', (byte)'g', (byte)'g', (byte)'-', (byte)'u', (byte)'g',  // cgg, cgg-ug
            (byte)'c', (byte)'h', (byte)'r', (byte)'-', (byte)'c', (byte)'h', (byte)'e', (byte)'r', (byte)'-', (byte)'u', (byte)'s',  // chr, chr-cher, chr-cher-us
            (byte)'c', (byte)'o', (byte)'-', (byte)'f', (byte)'r',  // co, co-fr
            (byte)'c', (byte)'s', (byte)'-', (byte)'c', (byte)'z',  // cs, cs-cz
            (byte)'c', (byte)'u', (byte)'-', (byte)'r', (byte)'u',  // cu, cu-ru
            (byte)'c', (byte)'y', (byte)'-', (byte)'g', (byte)'b',  // cy, cy-gb
            (byte)'d', (byte)'a', (byte)'-', (byte)'d', (byte)'k',  // da, da-dk
            (byte)'d', (byte)'a', (byte)'-', (byte)'g', (byte)'l',  // da-gl
            (byte)'d', (byte)'a', (byte)'v', (byte)'-', (byte)'k', (byte)'e',  // dav, dav-ke
            (byte)'d', (byte)'e', (byte)'-', (byte)'a', (byte)'t',  // de, de-at
            (byte)'d', (byte)'e', (byte)'-', (byte)'b', (byte)'e',  // de-be
            (byte)'d', (byte)'e', (byte)'-', (byte)'c', (byte)'h',  // de-ch
            (byte)'d', (byte)'e', (byte)'-', (byte)'d', (byte)'e', (byte)'_', (byte)'p', (byte)'h', (byte)'o', (byte)'n', (byte)'e', (byte)'b',  // de-de, de-de_phoneb
            (byte)'d', (byte)'e', (byte)'-', (byte)'i', (byte)'t',  // de-it
            (byte)'d', (byte)'e', (byte)'-', (byte)'l', (byte)'i',  // de-li
            (byte)'d', (byte)'e', (byte)'-', (byte)'l', (byte)'u',  // de-lu
            (byte)'d', (byte)'j', (byte)'e', (byte)'-', (byte)'n', (byte)'e',  // dje, dje-ne
            (byte)'d', (byte)'s', (byte)'b', (byte)'-', (byte)'d', (byte)'e',  // dsb, dsb-de
            (byte)'d', (byte)'u', (byte)'a', (byte)'-', (byte)'c', (byte)'m',  // dua, dua-cm
            (byte)'d', (byte)'v', (byte)'-', (byte)'m', (byte)'v',  // dv, dv-mv
            (byte)'d', (byte)'y', (byte)'o', (byte)'-', (byte)'s', (byte)'n',  // dyo, dyo-sn
            (byte)'d', (byte)'z', (byte)'-', (byte)'b', (byte)'t',  // dz, dz-bt
            (byte)'e', (byte)'b', (byte)'u', (byte)'-', (byte)'k', (byte)'e',  // ebu, ebu-ke
            (byte)'e', (byte)'e', (byte)'-', (byte)'g', (byte)'h',  // ee, ee-gh
            (byte)'e', (byte)'e', (byte)'-', (byte)'t', (byte)'g',  // ee-tg
            (byte)'e', (byte)'l', (byte)'-', (byte)'c', (byte)'y',  // el, el-cy
            (byte)'e', (byte)'l', (byte)'-', (byte)'g', (byte)'r',  // el-gr
            (byte)'e', (byte)'n', (byte)'-', (byte)'0', (byte)'0', (byte)'1',  // en, en-001
            (byte)'e', (byte)'n', (byte)'-', (byte)'0', (byte)'2', (byte)'9',  // en-029
            (byte)'e', (byte)'n', (byte)'-', (byte)'1', (byte)'5', (byte)'0',  // en-150
            (byte)'e', (byte)'n', (byte)'-', (byte)'a', (byte)'g',  // en-ag
            (byte)'e', (byte)'n', (byte)'-', (byte)'a', (byte)'i',  // en-ai
            (byte)'e', (byte)'n', (byte)'-', (byte)'a', (byte)'s',  // en-as
            (byte)'e', (byte)'n', (byte)'-', (byte)'a', (byte)'t',  // en-at
            (byte)'e', (byte)'n', (byte)'-', (byte)'a', (byte)'u',  // en-au
            (byte)'e', (byte)'n', (byte)'-', (byte)'b', (byte)'b',  // en-bb
            (byte)'e', (byte)'n', (byte)'-', (byte)'b', (byte)'e',  // en-be
            (byte)'e', (byte)'n', (byte)'-', (byte)'b', (byte)'i',  // en-bi
            (byte)'e', (byte)'n', (byte)'-', (byte)'b', (byte)'m',  // en-bm
            (byte)'e', (byte)'n', (byte)'-', (byte)'b', (byte)'s',  // en-bs
            (byte)'e', (byte)'n', (byte)'-', (byte)'b', (byte)'w',  // en-bw
            (byte)'e', (byte)'n', (byte)'-', (byte)'b', (byte)'z',  // en-bz
            (byte)'e', (byte)'n', (byte)'-', (byte)'c', (byte)'a',  // en-ca
            (byte)'e', (byte)'n', (byte)'-', (byte)'c', (byte)'c',  // en-cc
            (byte)'e', (byte)'n', (byte)'-', (byte)'c', (byte)'h',  // en-ch
            (byte)'e', (byte)'n', (byte)'-', (byte)'c', (byte)'k',  // en-ck
            (byte)'e', (byte)'n', (byte)'-', (byte)'c', (byte)'m',  // en-cm
            (byte)'e', (byte)'n', (byte)'-', (byte)'c', (byte)'x',  // en-cx
            (byte)'e', (byte)'n', (byte)'-', (byte)'c', (byte)'y',  // en-cy
            (byte)'e', (byte)'n', (byte)'-', (byte)'d', (byte)'e',  // en-de
            (byte)'e', (byte)'n', (byte)'-', (byte)'d', (byte)'k',  // en-dk
            (byte)'e', (byte)'n', (byte)'-', (byte)'d', (byte)'m',  // en-dm
            (byte)'e', (byte)'n', (byte)'-', (byte)'e', (byte)'r',  // en-er
            (byte)'e', (byte)'n', (byte)'-', (byte)'f', (byte)'i',  // en-fi
            (byte)'e', (byte)'n', (byte)'-', (byte)'f', (byte)'j',  // en-fj
            (byte)'e', (byte)'n', (byte)'-', (byte)'f', (byte)'k',  // en-fk
            (byte)'e', (byte)'n', (byte)'-', (byte)'f', (byte)'m',  // en-fm
            (byte)'e', (byte)'n', (byte)'-', (byte)'g', (byte)'b',  // en-gb
            (byte)'e', (byte)'n', (byte)'-', (byte)'g', (byte)'d',  // en-gd
            (byte)'e', (byte)'n', (byte)'-', (byte)'g', (byte)'g',  // en-gg
            (byte)'e', (byte)'n', (byte)'-', (byte)'g', (byte)'h',  // en-gh
            (byte)'e', (byte)'n', (byte)'-', (byte)'g', (byte)'i',  // en-gi
            (byte)'e', (byte)'n', (byte)'-', (byte)'g', (byte)'m',  // en-gm
            (byte)'e', (byte)'n', (byte)'-', (byte)'g', (byte)'u',  // en-gu
            (byte)'e', (byte)'n', (byte)'-', (byte)'g', (byte)'y',  // en-gy
            (byte)'e', (byte)'n', (byte)'-', (byte)'h', (byte)'k',  // en-hk
            (byte)'e', (byte)'n', (byte)'-', (byte)'i', (byte)'d',  // en-id
            (byte)'e', (byte)'n', (byte)'-', (byte)'i', (byte)'e',  // en-ie
            (byte)'e', (byte)'n', (byte)'-', (byte)'i', (byte)'l',  // en-il
            (byte)'e', (byte)'n', (byte)'-', (byte)'i', (byte)'m',  // en-im
            (byte)'e', (byte)'n', (byte)'-', (byte)'i', (byte)'n',  // en-in
            (byte)'e', (byte)'n', (byte)'-', (byte)'i', (byte)'o',  // en-io
            (byte)'e', (byte)'n', (byte)'-', (byte)'j', (byte)'e',  // en-je
            (byte)'e', (byte)'n', (byte)'-', (byte)'j', (byte)'m',  // en-jm
            (byte)'e', (byte)'n', (byte)'-', (byte)'k', (byte)'e',  // en-ke
            (byte)'e', (byte)'n', (byte)'-', (byte)'k', (byte)'i',  // en-ki
            (byte)'e', (byte)'n', (byte)'-', (byte)'k', (byte)'n',  // en-kn
            (byte)'e', (byte)'n', (byte)'-', (byte)'k', (byte)'y',  // en-ky
            (byte)'e', (byte)'n', (byte)'-', (byte)'l', (byte)'c',  // en-lc
            (byte)'e', (byte)'n', (byte)'-', (byte)'l', (byte)'r',  // en-lr
            (byte)'e', (byte)'n', (byte)'-', (byte)'l', (byte)'s',  // en-ls
            (byte)'e', (byte)'n', (byte)'-', (byte)'m', (byte)'g',  // en-mg
            (byte)'e', (byte)'n', (byte)'-', (byte)'m', (byte)'h',  // en-mh
            (byte)'e', (byte)'n', (byte)'-', (byte)'m', (byte)'o',  // en-mo
            (byte)'e', (byte)'n', (byte)'-', (byte)'m', (byte)'p',  // en-mp
            (byte)'e', (byte)'n', (byte)'-', (byte)'m', (byte)'s',  // en-ms
            (byte)'e', (byte)'n', (byte)'-', (byte)'m', (byte)'t',  // en-mt
            (byte)'e', (byte)'n', (byte)'-', (byte)'m', (byte)'u',  // en-mu
            (byte)'e', (byte)'n', (byte)'-', (byte)'m', (byte)'w',  // en-mw
            (byte)'e', (byte)'n', (byte)'-', (byte)'m', (byte)'y',  // en-my
            (byte)'e', (byte)'n', (byte)'-', (byte)'n', (byte)'a',  // en-na
            (byte)'e', (byte)'n', (byte)'-', (byte)'n', (byte)'f',  // en-nf
            (byte)'e', (byte)'n', (byte)'-', (byte)'n', (byte)'g',  // en-ng
            (byte)'e', (byte)'n', (byte)'-', (byte)'n', (byte)'l',  // en-nl
            (byte)'e', (byte)'n', (byte)'-', (byte)'n', (byte)'r',  // en-nr
            (byte)'e', (byte)'n', (byte)'-', (byte)'n', (byte)'u',  // en-nu
            (byte)'e', (byte)'n', (byte)'-', (byte)'n', (byte)'z',  // en-nz
            (byte)'e', (byte)'n', (byte)'-', (byte)'p', (byte)'g',  // en-pg
            (byte)'e', (byte)'n', (byte)'-', (byte)'p', (byte)'h',  // en-ph
            (byte)'e', (byte)'n', (byte)'-', (byte)'p', (byte)'k',  // en-pk
            (byte)'e', (byte)'n', (byte)'-', (byte)'p', (byte)'n',  // en-pn
            (byte)'e', (byte)'n', (byte)'-', (byte)'p', (byte)'r',  // en-pr
            (byte)'e', (byte)'n', (byte)'-', (byte)'p', (byte)'w',  // en-pw
            (byte)'e', (byte)'n', (byte)'-', (byte)'r', (byte)'w',  // en-rw
            (byte)'e', (byte)'n', (byte)'-', (byte)'s', (byte)'b',  // en-sb
            (byte)'e', (byte)'n', (byte)'-', (byte)'s', (byte)'c',  // en-sc
            (byte)'e', (byte)'n', (byte)'-', (byte)'s', (byte)'d',  // en-sd
            (byte)'e', (byte)'n', (byte)'-', (byte)'s', (byte)'e',  // en-se
            (byte)'e', (byte)'n', (byte)'-', (byte)'s', (byte)'g',  // en-sg
            (byte)'e', (byte)'n', (byte)'-', (byte)'s', (byte)'h',  // en-sh
            (byte)'e', (byte)'n', (byte)'-', (byte)'s', (byte)'i',  // en-si
            (byte)'e', (byte)'n', (byte)'-', (byte)'s', (byte)'l',  // en-sl
            (byte)'e', (byte)'n', (byte)'-', (byte)'s', (byte)'s',  // en-ss
            (byte)'e', (byte)'n', (byte)'-', (byte)'s', (byte)'x',  // en-sx
            (byte)'e', (byte)'n', (byte)'-', (byte)'s', (byte)'z',  // en-sz
            (byte)'e', (byte)'n', (byte)'-', (byte)'t', (byte)'c',  // en-tc
            (byte)'e', (byte)'n', (byte)'-', (byte)'t', (byte)'k',  // en-tk
            (byte)'e', (byte)'n', (byte)'-', (byte)'t', (byte)'o',  // en-to
            (byte)'e', (byte)'n', (byte)'-', (byte)'t', (byte)'t',  // en-tt
            (byte)'e', (byte)'n', (byte)'-', (byte)'t', (byte)'v',  // en-tv
            (byte)'e', (byte)'n', (byte)'-', (byte)'t', (byte)'z',  // en-tz
            (byte)'e', (byte)'n', (byte)'-', (byte)'u', (byte)'g',  // en-ug
            (byte)'e', (byte)'n', (byte)'-', (byte)'u', (byte)'m',  // en-um
            (byte)'e', (byte)'n', (byte)'-', (byte)'u', (byte)'s',  // en-us
            (byte)'e', (byte)'n', (byte)'-', (byte)'v', (byte)'c',  // en-vc
            (byte)'e', (byte)'n', (byte)'-', (byte)'v', (byte)'g',  // en-vg
            (byte)'e', (byte)'n', (byte)'-', (byte)'v', (byte)'i',  // en-vi
            (byte)'e', (byte)'n', (byte)'-', (byte)'v', (byte)'u',  // en-vu
            (byte)'e', (byte)'n', (byte)'-', (byte)'w', (byte)'s',  // en-ws
            (byte)'e', (byte)'n', (byte)'-', (byte)'z', (byte)'a',  // en-za
            (byte)'e', (byte)'n', (byte)'-', (byte)'z', (byte)'m',  // en-zm
            (byte)'e', (byte)'n', (byte)'-', (byte)'z', (byte)'w',  // en-zw
            (byte)'e', (byte)'o', (byte)'-', (byte)'0', (byte)'0', (byte)'1',  // eo, eo-001
            (byte)'e', (byte)'s', (byte)'-', (byte)'4', (byte)'1', (byte)'9',  // es, es-419
            (byte)'e', (byte)'s', (byte)'-', (byte)'a', (byte)'r',  // es-ar
            (byte)'e', (byte)'s', (byte)'-', (byte)'b', (byte)'o',  // es-bo
            (byte)'e', (byte)'s', (byte)'-', (byte)'b', (byte)'r',  // es-br
            (byte)'e', (byte)'s', (byte)'-', (byte)'c', (byte)'l',  // es-cl
            (byte)'e', (byte)'s', (byte)'-', (byte)'c', (byte)'o',  // es-co
            (byte)'e', (byte)'s', (byte)'-', (byte)'c', (byte)'r',  // es-cr
            (byte)'e', (byte)'s', (byte)'-', (byte)'c', (byte)'u',  // es-cu
            (byte)'e', (byte)'s', (byte)'-', (byte)'d', (byte)'o',  // es-do
            (byte)'e', (byte)'s', (byte)'-', (byte)'e', (byte)'c',  // es-ec
            (byte)'e', (byte)'s', (byte)'-', (byte)'e', (byte)'s', (byte)'_', (byte)'t', (byte)'r', (byte)'a', (byte)'d', (byte)'n', (byte)'l',  // es-es, es-es_tradnl
            (byte)'e', (byte)'s', (byte)'-', (byte)'g', (byte)'q',  // es-gq
            (byte)'e', (byte)'s', (byte)'-', (byte)'g', (byte)'t',  // es-gt
            (byte)'e', (byte)'s', (byte)'-', (byte)'h', (byte)'n',  // es-hn
            (byte)'e', (byte)'s', (byte)'-', (byte)'m', (byte)'x',  // es-mx
            (byte)'e', (byte)'s', (byte)'-', (byte)'n', (byte)'i',  // es-ni
            (byte)'e', (byte)'s', (byte)'-', (byte)'p', (byte)'a',  // es-pa
            (byte)'e', (byte)'s', (byte)'-', (byte)'p', (byte)'e',  // es-pe
            (byte)'e', (byte)'s', (byte)'-', (byte)'p', (byte)'h',  // es-ph
            (byte)'e', (byte)'s', (byte)'-', (byte)'p', (byte)'r',  // es-pr
            (byte)'e', (byte)'s', (byte)'-', (byte)'p', (byte)'y',  // es-py
            (byte)'e', (byte)'s', (byte)'-', (byte)'s', (byte)'v',  // es-sv
            (byte)'e', (byte)'s', (byte)'-', (byte)'u', (byte)'s',  // es-us
            (byte)'e', (byte)'s', (byte)'-', (byte)'u', (byte)'y',  // es-uy
            (byte)'e', (byte)'s', (byte)'-', (byte)'v', (byte)'e',  // es-ve
            (byte)'e', (byte)'t', (byte)'-', (byte)'e', (byte)'e',  // et, et-ee
            (byte)'e', (byte)'u', (byte)'-', (byte)'e', (byte)'s',  // eu, eu-es
            (byte)'e', (byte)'w', (byte)'o', (byte)'-', (byte)'c', (byte)'m',  // ewo, ewo-cm
            (byte)'f', (byte)'a', (byte)'-', (byte)'i', (byte)'r',  // fa, fa-ir
            (byte)'f', (byte)'f', (byte)'-', (byte)'c', (byte)'m',  // ff, ff-cm
            (byte)'f', (byte)'f', (byte)'-', (byte)'g', (byte)'n',  // ff-gn
            (byte)'f', (byte)'f', (byte)'-', (byte)'l', (byte)'a', (byte)'t', (byte)'n', (byte)'-', (byte)'s', (byte)'n',  // ff-latn, ff-latn-sn
            (byte)'f', (byte)'f', (byte)'-', (byte)'m', (byte)'r',  // ff-mr
            (byte)'f', (byte)'f', (byte)'-', (byte)'n', (byte)'g',  // ff-ng
            (byte)'f', (byte)'i', (byte)'-', (byte)'f', (byte)'i',  // fi, fi-fi
            (byte)'f', (byte)'i', (byte)'l', (byte)'-', (byte)'p', (byte)'h',  // fil, fil-ph
            (byte)'f', (byte)'o', (byte)'-', (byte)'d', (byte)'k',  // fo, fo-dk
            (byte)'f', (byte)'o', (byte)'-', (byte)'f', (byte)'o',  // fo-fo
            (byte)'f', (byte)'r', (byte)'-', (byte)'0', (byte)'2', (byte)'9',  // fr, fr-029
            (byte)'f', (byte)'r', (byte)'-', (byte)'b', (byte)'e',  // fr-be
            (byte)'f', (byte)'r', (byte)'-', (byte)'b', (byte)'f',  // fr-bf
            (byte)'f', (byte)'r', (byte)'-', (byte)'b', (byte)'i',  // fr-bi
            (byte)'f', (byte)'r', (byte)'-', (byte)'b', (byte)'j',  // fr-bj
            (byte)'f', (byte)'r', (byte)'-', (byte)'b', (byte)'l',  // fr-bl
            (byte)'f', (byte)'r', (byte)'-', (byte)'c', (byte)'a',  // fr-ca
            (byte)'f', (byte)'r', (byte)'-', (byte)'c', (byte)'d',  // fr-cd
            (byte)'f', (byte)'r', (byte)'-', (byte)'c', (byte)'f',  // fr-cf
            (byte)'f', (byte)'r', (byte)'-', (byte)'c', (byte)'g',  // fr-cg
            (byte)'f', (byte)'r', (byte)'-', (byte)'c', (byte)'h',  // fr-ch
            (byte)'f', (byte)'r', (byte)'-', (byte)'c', (byte)'i',  // fr-ci
            (byte)'f', (byte)'r', (byte)'-', (byte)'c', (byte)'m',  // fr-cm
            (byte)'f', (byte)'r', (byte)'-', (byte)'d', (byte)'j',  // fr-dj
            (byte)'f', (byte)'r', (byte)'-', (byte)'d', (byte)'z',  // fr-dz
            (byte)'f', (byte)'r', (byte)'-', (byte)'f', (byte)'r',  // fr-fr
            (byte)'f', (byte)'r', (byte)'-', (byte)'g', (byte)'a',  // fr-ga
            (byte)'f', (byte)'r', (byte)'-', (byte)'g', (byte)'f',  // fr-gf
            (byte)'f', (byte)'r', (byte)'-', (byte)'g', (byte)'n',  // fr-gn
            (byte)'f', (byte)'r', (byte)'-', (byte)'g', (byte)'p',  // fr-gp
            (byte)'f', (byte)'r', (byte)'-', (byte)'g', (byte)'q',  // fr-gq
            (byte)'f', (byte)'r', (byte)'-', (byte)'h', (byte)'t',  // fr-ht
            (byte)'f', (byte)'r', (byte)'-', (byte)'k', (byte)'m',  // fr-km
            (byte)'f', (byte)'r', (byte)'-', (byte)'l', (byte)'u',  // fr-lu
            (byte)'f', (byte)'r', (byte)'-', (byte)'m', (byte)'a',  // fr-ma
            (byte)'f', (byte)'r', (byte)'-', (byte)'m', (byte)'c',  // fr-mc
            (byte)'f', (byte)'r', (byte)'-', (byte)'m', (byte)'f',  // fr-mf
            (byte)'f', (byte)'r', (byte)'-', (byte)'m', (byte)'g',  // fr-mg
            (byte)'f', (byte)'r', (byte)'-', (byte)'m', (byte)'l',  // fr-ml
            (byte)'f', (byte)'r', (byte)'-', (byte)'m', (byte)'q',  // fr-mq
            (byte)'f', (byte)'r', (byte)'-', (byte)'m', (byte)'r',  // fr-mr
            (byte)'f', (byte)'r', (byte)'-', (byte)'m', (byte)'u',  // fr-mu
            (byte)'f', (byte)'r', (byte)'-', (byte)'n', (byte)'c',  // fr-nc
            (byte)'f', (byte)'r', (byte)'-', (byte)'n', (byte)'e',  // fr-ne
            (byte)'f', (byte)'r', (byte)'-', (byte)'p', (byte)'f',  // fr-pf
            (byte)'f', (byte)'r', (byte)'-', (byte)'p', (byte)'m',  // fr-pm
            (byte)'f', (byte)'r', (byte)'-', (byte)'r', (byte)'e',  // fr-re
            (byte)'f', (byte)'r', (byte)'-', (byte)'r', (byte)'w',  // fr-rw
            (byte)'f', (byte)'r', (byte)'-', (byte)'s', (byte)'c',  // fr-sc
            (byte)'f', (byte)'r', (byte)'-', (byte)'s', (byte)'n',  // fr-sn
            (byte)'f', (byte)'r', (byte)'-', (byte)'s', (byte)'y',  // fr-sy
            (byte)'f', (byte)'r', (byte)'-', (byte)'t', (byte)'d',  // fr-td
            (byte)'f', (byte)'r', (byte)'-', (byte)'t', (byte)'g',  // fr-tg
            (byte)'f', (byte)'r', (byte)'-', (byte)'t', (byte)'n',  // fr-tn
            (byte)'f', (byte)'r', (byte)'-', (byte)'v', (byte)'u',  // fr-vu
            (byte)'f', (byte)'r', (byte)'-', (byte)'w', (byte)'f',  // fr-wf
            (byte)'f', (byte)'r', (byte)'-', (byte)'y', (byte)'t',  // fr-yt
            (byte)'f', (byte)'u', (byte)'r', (byte)'-', (byte)'i', (byte)'t',  // fur, fur-it
            (byte)'f', (byte)'y', (byte)'-', (byte)'n', (byte)'l',  // fy, fy-nl
            (byte)'g', (byte)'a', (byte)'-', (byte)'i', (byte)'e',  // ga, ga-ie
            (byte)'g', (byte)'d', (byte)'-', (byte)'g', (byte)'b',  // gd, gd-gb
            (byte)'g', (byte)'l', (byte)'-', (byte)'e', (byte)'s',  // gl, gl-es
            (byte)'g', (byte)'n', (byte)'-', (byte)'p', (byte)'y',  // gn, gn-py
            (byte)'g', (byte)'s', (byte)'w', (byte)'-', (byte)'c', (byte)'h',  // gsw, gsw-ch
            (byte)'g', (byte)'s', (byte)'w', (byte)'-', (byte)'f', (byte)'r',  // gsw-fr
            (byte)'g', (byte)'s', (byte)'w', (byte)'-', (byte)'l', (byte)'i',  // gsw-li
            (byte)'g', (byte)'u', (byte)'-', (byte)'i', (byte)'n',  // gu, gu-in
            (byte)'g', (byte)'u', (byte)'z', (byte)'-', (byte)'k', (byte)'e',  // guz, guz-ke
            (byte)'g', (byte)'v', (byte)'-', (byte)'i', (byte)'m',  // gv, gv-im
            (byte)'h', (byte)'a', (byte)'-', (byte)'l', (byte)'a', (byte)'t', (byte)'n', (byte)'-', (byte)'g', (byte)'h',  // ha, ha-latn, ha-latn-gh
            (byte)'h', (byte)'a', (byte)'-', (byte)'l', (byte)'a', (byte)'t', (byte)'n', (byte)'-', (byte)'n', (byte)'e',  // ha-latn-ne
            (byte)'h', (byte)'a', (byte)'-', (byte)'l', (byte)'a', (byte)'t', (byte)'n', (byte)'-', (byte)'n', (byte)'g',  // ha-latn-ng
            (byte)'h', (byte)'a', (byte)'w', (byte)'-', (byte)'u', (byte)'s',  // haw, haw-us
            (byte)'h', (byte)'e', (byte)'-', (byte)'i', (byte)'l',  // he, he-il
            (byte)'h', (byte)'i', (byte)'-', (byte)'i', (byte)'n',  // hi, hi-in
            (byte)'h', (byte)'r', (byte)'-', (byte)'b', (byte)'a',  // hr, hr-ba
            (byte)'h', (byte)'r', (byte)'-', (byte)'h', (byte)'r',  // hr-hr
            (byte)'h', (byte)'s', (byte)'b', (byte)'-', (byte)'d', (byte)'e',  // hsb, hsb-de
            (byte)'h', (byte)'u', (byte)'-', (byte)'h', (byte)'u', (byte)'_', (byte)'t', (byte)'e', (byte)'c', (byte)'h', (byte)'n', (byte)'l',  // hu, hu-hu, hu-hu_technl
            (byte)'h', (byte)'y', (byte)'-', (byte)'a', (byte)'m',  // hy, hy-am
            (byte)'i', (byte)'a', (byte)'-', (byte)'0', (byte)'0', (byte)'1',  // ia, ia-001
            (byte)'i', (byte)'a', (byte)'-', (byte)'f', (byte)'r',  // ia-fr
            (byte)'i', (byte)'b', (byte)'b', (byte)'-', (byte)'n', (byte)'g',  // ibb, ibb-ng
            (byte)'i', (byte)'d', (byte)'-', (byte)'i', (byte)'d',  // id, id-id
            (byte)'i', (byte)'g', (byte)'-', (byte)'n', (byte)'g',  // ig, ig-ng
            (byte)'i', (byte)'i', (byte)'-', (byte)'c', (byte)'n',  // ii, ii-cn
            (byte)'i', (byte)'s', (byte)'-', (byte)'i', (byte)'s',  // is, is-is
            (byte)'i', (byte)'t', (byte)'-', (byte)'c', (byte)'h',  // it, it-ch
            (byte)'i', (byte)'t', (byte)'-', (byte)'i', (byte)'t',  // it-it
            (byte)'i', (byte)'t', (byte)'-', (byte)'s', (byte)'m',  // it-sm
            (byte)'i', (byte)'u', (byte)'-', (byte)'c', (byte)'a', (byte)'n', (byte)'s', (byte)'-', (byte)'c', (byte)'a',  // iu, iu-cans, iu-cans-ca
            (byte)'i', (byte)'u', (byte)'-', (byte)'l', (byte)'a', (byte)'t', (byte)'n', (byte)'-', (byte)'c', (byte)'a',  // iu-latn, iu-latn-ca
            (byte)'j', (byte)'a', (byte)'-', (byte)'j', (byte)'p', (byte)'_', (byte)'r', (byte)'a', (byte)'d', (byte)'s', (byte)'t', (byte)'r',  // ja, ja-jp, ja-jp_radstr
            (byte)'j', (byte)'g', (byte)'o', (byte)'-', (byte)'c', (byte)'m',  // jgo, jgo-cm
            (byte)'j', (byte)'m', (byte)'c', (byte)'-', (byte)'t', (byte)'z',  // jmc, jmc-tz
            (byte)'j', (byte)'v', (byte)'-', (byte)'j', (byte)'a', (byte)'v', (byte)'a', (byte)'-', (byte)'i', (byte)'d',  // jv, jv-java, jv-java-id
            (byte)'j', (byte)'v', (byte)'-', (byte)'l', (byte)'a', (byte)'t', (byte)'n', (byte)'-', (byte)'i', (byte)'d',  // jv-latn, jv-latn-id
            (byte)'k', (byte)'a', (byte)'-', (byte)'g', (byte)'e', (byte)'_', (byte)'m', (byte)'o', (byte)'d', (byte)'e', (byte)'r', (byte)'n',  // ka, ka-ge, ka-ge_modern
            (byte)'k', (byte)'a', (byte)'b', (byte)'-', (byte)'d', (byte)'z',  // kab, kab-dz
            (byte)'k', (byte)'a', (byte)'m', (byte)'-', (byte)'k', (byte)'e',  // kam, kam-ke
            (byte)'k', (byte)'d', (byte)'e', (byte)'-', (byte)'t', (byte)'z',  // kde, kde-tz
            (byte)'k', (byte)'e', (byte)'a', (byte)'-', (byte)'c', (byte)'v',  // kea, kea-cv
            (byte)'k', (byte)'h', (byte)'q', (byte)'-', (byte)'m', (byte)'l',  // khq, khq-ml
            (byte)'k', (byte)'i', (byte)'-', (byte)'k', (byte)'e',  // ki, ki-ke
            (byte)'k', (byte)'k', (byte)'-', (byte)'k', (byte)'z',  // kk, kk-kz
            (byte)'k', (byte)'k', (byte)'j', (byte)'-', (byte)'c', (byte)'m',  // kkj, kkj-cm
            (byte)'k', (byte)'l', (byte)'-', (byte)'g', (byte)'l',  // kl, kl-gl
            (byte)'k', (byte)'l', (byte)'n', (byte)'-', (byte)'k', (byte)'e',  // kln, kln-ke
            (byte)'k', (byte)'m', (byte)'-', (byte)'k', (byte)'h',  // km, km-kh
            (byte)'k', (byte)'n', (byte)'-', (byte)'i', (byte)'n',  // kn, kn-in
            (byte)'k', (byte)'o', (byte)'-', (byte)'k', (byte)'p',  // ko, ko-kp
            (byte)'k', (byte)'o', (byte)'-', (byte)'k', (byte)'r',  // ko-kr
            (byte)'k', (byte)'o', (byte)'k', (byte)'-', (byte)'i', (byte)'n',  // kok, kok-in
            (byte)'k', (byte)'r', (byte)'-', (byte)'n', (byte)'g',  // kr, kr-ng
            (byte)'k', (byte)'s', (byte)'-', (byte)'a', (byte)'r', (byte)'a', (byte)'b', (byte)'-', (byte)'i', (byte)'n',  // ks, ks-arab, ks-arab-in
            (byte)'k', (byte)'s', (byte)'-', (byte)'d', (byte)'e', (byte)'v', (byte)'a', (byte)'-', (byte)'i', (byte)'n',  // ks-deva, ks-deva-in
            (byte)'k', (byte)'s', (byte)'b', (byte)'-', (byte)'t', (byte)'z',  // ksb, ksb-tz
            (byte)'k', (byte)'s', (byte)'f', (byte)'-', (byte)'c', (byte)'m',  // ksf, ksf-cm
            (byte)'k', (byte)'s', (byte)'h', (byte)'-', (byte)'d', (byte)'e',  // ksh, ksh-de
            (byte)'k', (byte)'u', (byte)'-', (byte)'a', (byte)'r', (byte)'a', (byte)'b', (byte)'-', (byte)'i', (byte)'q',  // ku, ku-arab, ku-arab-iq
            (byte)'k', (byte)'u', (byte)'-', (byte)'a', (byte)'r', (byte)'a', (byte)'b', (byte)'-', (byte)'i', (byte)'r',  // ku-arab-ir
            (byte)'k', (byte)'w', (byte)'-', (byte)'g', (byte)'b',  // kw, kw-gb
            (byte)'k', (byte)'y', (byte)'-', (byte)'k', (byte)'g',  // ky, ky-kg
            (byte)'l', (byte)'a', (byte)'-', (byte)'0', (byte)'0', (byte)'1',  // la, la-001
            (byte)'l', (byte)'a', (byte)'g', (byte)'-', (byte)'t', (byte)'z',  // lag, lag-tz
            (byte)'l', (byte)'b', (byte)'-', (byte)'l', (byte)'u',  // lb, lb-lu
            (byte)'l', (byte)'g', (byte)'-', (byte)'u', (byte)'g',  // lg, lg-ug
            (byte)'l', (byte)'k', (byte)'t', (byte)'-', (byte)'u', (byte)'s',  // lkt, lkt-us
            (byte)'l', (byte)'n', (byte)'-', (byte)'a', (byte)'o',  // ln, ln-ao
            (byte)'l', (byte)'n', (byte)'-', (byte)'c', (byte)'d',  // ln-cd
            (byte)'l', (byte)'n', (byte)'-', (byte)'c', (byte)'f',  // ln-cf
            (byte)'l', (byte)'n', (byte)'-', (byte)'c', (byte)'g',  // ln-cg
            (byte)'l', (byte)'o', (byte)'-', (byte)'l', (byte)'a',  // lo, lo-la
            (byte)'l', (byte)'r', (byte)'c', (byte)'-', (byte)'i', (byte)'q',  // lrc, lrc-iq
            (byte)'l', (byte)'r', (byte)'c', (byte)'-', (byte)'i', (byte)'r',  // lrc-ir
            (byte)'l', (byte)'t', (byte)'-', (byte)'l', (byte)'t',  // lt, lt-lt
            (byte)'l', (byte)'u', (byte)'-', (byte)'c', (byte)'d',  // lu, lu-cd
            (byte)'l', (byte)'u', (byte)'o', (byte)'-', (byte)'k', (byte)'e',  // luo, luo-ke
            (byte)'l', (byte)'u', (byte)'y', (byte)'-', (byte)'k', (byte)'e',  // luy, luy-ke
            (byte)'l', (byte)'v', (byte)'-', (byte)'l', (byte)'v',  // lv, lv-lv
            (byte)'m', (byte)'a', (byte)'s', (byte)'-', (byte)'k', (byte)'e',  // mas, mas-ke
            (byte)'m', (byte)'a', (byte)'s', (byte)'-', (byte)'t', (byte)'z',  // mas-tz
            (byte)'m', (byte)'e', (byte)'r', (byte)'-', (byte)'k', (byte)'e',  // mer, mer-ke
            (byte)'m', (byte)'f', (byte)'e', (byte)'-', (byte)'m', (byte)'u',  // mfe, mfe-mu
            (byte)'m', (byte)'g', (byte)'-', (byte)'m', (byte)'g',  // mg, mg-mg
            (byte)'m', (byte)'g', (byte)'h', (byte)'-', (byte)'m', (byte)'z',  // mgh, mgh-mz
            (byte)'m', (byte)'g', (byte)'o', (byte)'-', (byte)'c', (byte)'m',  // mgo, mgo-cm
            (byte)'m', (byte)'i', (byte)'-', (byte)'n', (byte)'z',  // mi, mi-nz
            (byte)'m', (byte)'k', (byte)'-', (byte)'m', (byte)'k',  // mk, mk-mk
            (byte)'m', (byte)'l', (byte)'-', (byte)'i', (byte)'n',  // ml, ml-in
            (byte)'m', (byte)'n', (byte)'-', (byte)'c', (byte)'y', (byte)'r', (byte)'l',  // mn, mn-cyrl
            (byte)'m', (byte)'n', (byte)'-', (byte)'m', (byte)'n',  // mn-mn
            (byte)'m', (byte)'n', (byte)'-', (byte)'m', (byte)'o', (byte)'n', (byte)'g', (byte)'-', (byte)'c', (byte)'n',  // mn-mong, mn-mong-cn
            (byte)'m', (byte)'n', (byte)'-', (byte)'m', (byte)'o', (byte)'n', (byte)'g', (byte)'-', (byte)'m', (byte)'n',  // mn-mong-mn
            (byte)'m', (byte)'n', (byte)'i', (byte)'-', (byte)'i', (byte)'n',  // mni, mni-in
            (byte)'m', (byte)'o', (byte)'h', (byte)'-', (byte)'c', (byte)'a',  // moh, moh-ca
            (byte)'m', (byte)'r', (byte)'-', (byte)'i', (byte)'n',  // mr, mr-in
            (byte)'m', (byte)'s', (byte)'-', (byte)'b', (byte)'n',  // ms, ms-bn
            (byte)'m', (byte)'s', (byte)'-', (byte)'m', (byte)'y',  // ms-my
            (byte)'m', (byte)'s', (byte)'-', (byte)'s', (byte)'g',  // ms-sg
            (byte)'m', (byte)'t', (byte)'-', (byte)'m', (byte)'t',  // mt, mt-mt
            (byte)'m', (byte)'u', (byte)'a', (byte)'-', (byte)'c', (byte)'m',  // mua, mua-cm
            (byte)'m', (byte)'y', (byte)'-', (byte)'m', (byte)'m',  // my, my-mm
            (byte)'m', (byte)'z', (byte)'n', (byte)'-', (byte)'i', (byte)'r',  // mzn, mzn-ir
            (byte)'n', (byte)'a', (byte)'q', (byte)'-', (byte)'n', (byte)'a',  // naq, naq-na
            (byte)'n', (byte)'b', (byte)'-', (byte)'n', (byte)'o',  // nb, nb-no
            (byte)'n', (byte)'b', (byte)'-', (byte)'s', (byte)'j',  // nb-sj
            (byte)'n', (byte)'d', (byte)'-', (byte)'z', (byte)'w',  // nd, nd-zw
            (byte)'n', (byte)'d', (byte)'s', (byte)'-', (byte)'d', (byte)'e',  // nds, nds-de
            (byte)'n', (byte)'d', (byte)'s', (byte)'-', (byte)'n', (byte)'l',  // nds-nl
            (byte)'n', (byte)'e', (byte)'-', (byte)'i', (byte)'n',  // ne, ne-in
            (byte)'n', (byte)'e', (byte)'-', (byte)'n', (byte)'p',  // ne-np
            (byte)'n', (byte)'l', (byte)'-', (byte)'a', (byte)'w',  // nl, nl-aw
            (byte)'n', (byte)'l', (byte)'-', (byte)'b', (byte)'e',  // nl-be
            (byte)'n', (byte)'l', (byte)'-', (byte)'b', (byte)'q',  // nl-bq
            (byte)'n', (byte)'l', (byte)'-', (byte)'c', (byte)'w',  // nl-cw
            (byte)'n', (byte)'l', (byte)'-', (byte)'n', (byte)'l',  // nl-nl
            (byte)'n', (byte)'l', (byte)'-', (byte)'s', (byte)'r',  // nl-sr
            (byte)'n', (byte)'l', (byte)'-', (byte)'s', (byte)'x',  // nl-sx
            (byte)'n', (byte)'m', (byte)'g', (byte)'-', (byte)'c', (byte)'m',  // nmg, nmg-cm
            (byte)'n', (byte)'n', (byte)'-', (byte)'n', (byte)'o',  // nn, nn-no
            (byte)'n', (byte)'n', (byte)'h', (byte)'-', (byte)'c', (byte)'m',  // nnh, nnh-cm
            (byte)'n', (byte)'o',  // no
            (byte)'n', (byte)'q', (byte)'o', (byte)'-', (byte)'g', (byte)'n',  // nqo, nqo-gn
            (byte)'n', (byte)'r', (byte)'-', (byte)'z', (byte)'a',  // nr, nr-za
            (byte)'n', (byte)'s', (byte)'o', (byte)'-', (byte)'z', (byte)'a',  // nso, nso-za
            (byte)'n', (byte)'u', (byte)'s', (byte)'-', (byte)'s', (byte)'s',  // nus, nus-ss
            (byte)'n', (byte)'y', (byte)'n', (byte)'-', (byte)'u', (byte)'g',  // nyn, nyn-ug
            (byte)'o', (byte)'c', (byte)'-', (byte)'f', (byte)'r',  // oc, oc-fr
            (byte)'o', (byte)'m', (byte)'-', (byte)'e', (byte)'t',  // om, om-et
            (byte)'o', (byte)'m', (byte)'-', (byte)'k', (byte)'e',  // om-ke
            (byte)'o', (byte)'r', (byte)'-', (byte)'i', (byte)'n',  // or, or-in
            (byte)'o', (byte)'s', (byte)'-', (byte)'g', (byte)'e',  // os, os-ge
            (byte)'o', (byte)'s', (byte)'-', (byte)'r', (byte)'u',  // os-ru
            (byte)'p', (byte)'a', (byte)'-', (byte)'a', (byte)'r', (byte)'a', (byte)'b', (byte)'-', (byte)'p', (byte)'k',  // pa, pa-arab, pa-arab-pk
            (byte)'p', (byte)'a', (byte)'-', (byte)'i', (byte)'n',  // pa-in
            (byte)'p', (byte)'a', (byte)'p', (byte)'-', (byte)'0', (byte)'2', (byte)'9',  // pap, pap-029
            (byte)'p', (byte)'l', (byte)'-', (byte)'p', (byte)'l',  // pl, pl-pl
            (byte)'p', (byte)'r', (byte)'g', (byte)'-', (byte)'0', (byte)'0', (byte)'1',  // prg, prg-001
            (byte)'p', (byte)'r', (byte)'s', (byte)'-', (byte)'a', (byte)'f',  // prs, prs-af
            (byte)'p', (byte)'s', (byte)'-', (byte)'a', (byte)'f',  // ps, ps-af
            (byte)'p', (byte)'t', (byte)'-', (byte)'a', (byte)'o',  // pt, pt-ao
            (byte)'p', (byte)'t', (byte)'-', (byte)'b', (byte)'r',  // pt-br
            (byte)'p', (byte)'t', (byte)'-', (byte)'c', (byte)'h',  // pt-ch
            (byte)'p', (byte)'t', (byte)'-', (byte)'c', (byte)'v',  // pt-cv
            (byte)'p', (byte)'t', (byte)'-', (byte)'g', (byte)'q',  // pt-gq
            (byte)'p', (byte)'t', (byte)'-', (byte)'g', (byte)'w',  // pt-gw
            (byte)'p', (byte)'t', (byte)'-', (byte)'l', (byte)'u',  // pt-lu
            (byte)'p', (byte)'t', (byte)'-', (byte)'m', (byte)'o',  // pt-mo
            (byte)'p', (byte)'t', (byte)'-', (byte)'m', (byte)'z',  // pt-mz
            (byte)'p', (byte)'t', (byte)'-', (byte)'p', (byte)'t',  // pt-pt
            (byte)'p', (byte)'t', (byte)'-', (byte)'s', (byte)'t',  // pt-st
            (byte)'p', (byte)'t', (byte)'-', (byte)'t', (byte)'l',  // pt-tl
            (byte)'q', (byte)'p', (byte)'s', (byte)'-', (byte)'l', (byte)'a', (byte)'t', (byte)'n', (byte)'-', (byte)'x', (byte)'-', (byte)'s', (byte)'h',  // qps-latn-x-sh
            (byte)'q', (byte)'p', (byte)'s', (byte)'-', (byte)'p', (byte)'l', (byte)'o', (byte)'c', (byte)'a',  // qps-ploc, qps-ploca
            (byte)'q', (byte)'p', (byte)'s', (byte)'-', (byte)'p', (byte)'l', (byte)'o', (byte)'c', (byte)'m',  // qps-plocm
            (byte)'q', (byte)'u', (byte)'c', (byte)'-', (byte)'l', (byte)'a', (byte)'t', (byte)'n', (byte)'-', (byte)'g', (byte)'t',  // quc, quc-latn, quc-latn-gt
            (byte)'q', (byte)'u', (byte)'z', (byte)'-', (byte)'b', (byte)'o',  // quz, quz-bo
            (byte)'q', (byte)'u', (byte)'z', (byte)'-', (byte)'e', (byte)'c',  // quz-ec
            (byte)'q', (byte)'u', (byte)'z', (byte)'-', (byte)'p', (byte)'e',  // quz-pe
            (byte)'r', (byte)'m', (byte)'-', (byte)'c', (byte)'h',  // rm, rm-ch
            (byte)'r', (byte)'n', (byte)'-', (byte)'b', (byte)'i',  // rn, rn-bi
            (byte)'r', (byte)'o', (byte)'-', (byte)'m', (byte)'d',  // ro, ro-md
            (byte)'r', (byte)'o', (byte)'-', (byte)'r', (byte)'o',  // ro-ro
            (byte)'r', (byte)'o', (byte)'f', (byte)'-', (byte)'t', (byte)'z',  // rof, rof-tz
            (byte)'r', (byte)'u', (byte)'-', (byte)'b', (byte)'y',  // ru, ru-by
            (byte)'r', (byte)'u', (byte)'-', (byte)'k', (byte)'g',  // ru-kg
            (byte)'r', (byte)'u', (byte)'-', (byte)'k', (byte)'z',  // ru-kz
            (byte)'r', (byte)'u', (byte)'-', (byte)'m', (byte)'d',  // ru-md
            (byte)'r', (byte)'u', (byte)'-', (byte)'r', (byte)'u',  // ru-ru
            (byte)'r', (byte)'u', (byte)'-', (byte)'u', (byte)'a',  // ru-ua
            (byte)'r', (byte)'w', (byte)'-', (byte)'r', (byte)'w',  // rw, rw-rw
            (byte)'r', (byte)'w', (byte)'k', (byte)'-', (byte)'t', (byte)'z',  // rwk, rwk-tz
            (byte)'s', (byte)'a', (byte)'-', (byte)'i', (byte)'n',  // sa, sa-in
            (byte)'s', (byte)'a', (byte)'h', (byte)'-', (byte)'r', (byte)'u',  // sah, sah-ru
            (byte)'s', (byte)'a', (byte)'q', (byte)'-', (byte)'k', (byte)'e',  // saq, saq-ke
            (byte)'s', (byte)'b', (byte)'p', (byte)'-', (byte)'t', (byte)'z',  // sbp, sbp-tz
            (byte)'s', (byte)'d', (byte)'-', (byte)'a', (byte)'r', (byte)'a', (byte)'b', (byte)'-', (byte)'p', (byte)'k',  // sd, sd-arab, sd-arab-pk
            (byte)'s', (byte)'d', (byte)'-', (byte)'d', (byte)'e', (byte)'v', (byte)'a', (byte)'-', (byte)'i', (byte)'n',  // sd-deva, sd-deva-in
            (byte)'s', (byte)'e', (byte)'-', (byte)'f', (byte)'i',  // se, se-fi
            (byte)'s', (byte)'e', (byte)'-', (byte)'n', (byte)'o',  // se-no
            (byte)'s', (byte)'e', (byte)'-', (byte)'s', (byte)'e',  // se-se
            (byte)'s', (byte)'e', (byte)'h', (byte)'-', (byte)'m', (byte)'z',  // seh, seh-mz
            (byte)'s', (byte)'e', (byte)'s', (byte)'-', (byte)'m', (byte)'l',  // ses, ses-ml
            (byte)'s', (byte)'g', (byte)'-', (byte)'c', (byte)'f',  // sg, sg-cf
            (byte)'s', (byte)'h', (byte)'i', (byte)'-', (byte)'l', (byte)'a', (byte)'t', (byte)'n', (byte)'-', (byte)'m', (byte)'a',  // shi, shi-latn, shi-latn-ma
            (byte)'s', (byte)'h', (byte)'i', (byte)'-', (byte)'t', (byte)'f', (byte)'n', (byte)'g', (byte)'-', (byte)'m', (byte)'a',  // shi-tfng, shi-tfng-ma
            (byte)'s', (byte)'i', (byte)'-', (byte)'l', (byte)'k',  // si, si-lk
            (byte)'s', (byte)'k', (byte)'-', (byte)'s', (byte)'k',  // sk, sk-sk
            (byte)'s', (byte)'l', (byte)'-', (byte)'s', (byte)'i',  // sl, sl-si
            (byte)'s', (byte)'m', (byte)'a', (byte)'-', (byte)'n', (byte)'o',  // sma, sma-no
            (byte)'s', (byte)'m', (byte)'a', (byte)'-', (byte)'s', (byte)'e',  // sma-se
            (byte)'s', (byte)'m', (byte)'j', (byte)'-', (byte)'n', (byte)'o',  // smj, smj-no
            (byte)'s', (byte)'m', (byte)'j', (byte)'-', (byte)'s', (byte)'e',  // smj-se
            (byte)'s', (byte)'m', (byte)'n', (byte)'-', (byte)'f', (byte)'i',  // smn, smn-fi
            (byte)'s', (byte)'m', (byte)'s', (byte)'-', (byte)'f', (byte)'i',  // sms, sms-fi
            (byte)'s', (byte)'n', (byte)'-', (byte)'l', (byte)'a', (byte)'t', (byte)'n', (byte)'-', (byte)'z', (byte)'w',  // sn, sn-latn, sn-latn-zw
            (byte)'s', (byte)'o', (byte)'-', (byte)'d', (byte)'j',  // so, so-dj
            (byte)'s', (byte)'o', (byte)'-', (byte)'e', (byte)'t',  // so-et
            (byte)'s', (byte)'o', (byte)'-', (byte)'k', (byte)'e',  // so-ke
            (byte)'s', (byte)'o', (byte)'-', (byte)'s', (byte)'o',  // so-so
            (byte)'s', (byte)'q', (byte)'-', (byte)'a', (byte)'l',  // sq, sq-al
            (byte)'s', (byte)'q', (byte)'-', (byte)'m', (byte)'k',  // sq-mk
            (byte)'s', (byte)'q', (byte)'-', (byte)'x', (byte)'k',  // sq-xk
            (byte)'s', (byte)'r', (byte)'-', (byte)'c', (byte)'y', (byte)'r', (byte)'l', (byte)'-', (byte)'b', (byte)'a',  // sr, sr-cyrl, sr-cyrl-ba
            (byte)'s', (byte)'r', (byte)'-', (byte)'c', (byte)'y', (byte)'r', (byte)'l', (byte)'-', (byte)'c', (byte)'s',  // sr-cyrl-cs
            (byte)'s', (byte)'r', (byte)'-', (byte)'c', (byte)'y', (byte)'r', (byte)'l', (byte)'-', (byte)'m', (byte)'e',  // sr-cyrl-me
            (byte)'s', (byte)'r', (byte)'-', (byte)'c', (byte)'y', (byte)'r', (byte)'l', (byte)'-', (byte)'r', (byte)'s',  // sr-cyrl-rs
            (byte)'s', (byte)'r', (byte)'-', (byte)'c', (byte)'y', (byte)'r', (byte)'l', (byte)'-', (byte)'x', (byte)'k',  // sr-cyrl-xk
            (byte)'s', (byte)'r', (byte)'-', (byte)'l', (byte)'a', (byte)'t', (byte)'n', (byte)'-', (byte)'b', (byte)'a',  // sr-latn, sr-latn-ba
            (byte)'s', (byte)'r', (byte)'-', (byte)'l', (byte)'a', (byte)'t', (byte)'n', (byte)'-', (byte)'c', (byte)'s',  // sr-latn-cs
            (byte)'s', (byte)'r', (byte)'-', (byte)'l', (byte)'a', (byte)'t', (byte)'n', (byte)'-', (byte)'m', (byte)'e',  // sr-latn-me
            (byte)'s', (byte)'r', (byte)'-', (byte)'l', (byte)'a', (byte)'t', (byte)'n', (byte)'-', (byte)'r', (byte)'s',  // sr-latn-rs
            (byte)'s', (byte)'r', (byte)'-', (byte)'l', (byte)'a', (byte)'t', (byte)'n', (byte)'-', (byte)'x', (byte)'k',  // sr-latn-xk
            (byte)'s', (byte)'s', (byte)'-', (byte)'s', (byte)'z',  // ss, ss-sz
            (byte)'s', (byte)'s', (byte)'-', (byte)'z', (byte)'a',  // ss-za
            (byte)'s', (byte)'s', (byte)'y', (byte)'-', (byte)'e', (byte)'r',  // ssy, ssy-er
            (byte)'s', (byte)'t', (byte)'-', (byte)'l', (byte)'s',  // st, st-ls
            (byte)'s', (byte)'t', (byte)'-', (byte)'z', (byte)'a',  // st-za
            (byte)'s', (byte)'v', (byte)'-', (byte)'a', (byte)'x',  // sv, sv-ax
            (byte)'s', (byte)'v', (byte)'-', (byte)'f', (byte)'i',  // sv-fi
            (byte)'s', (byte)'v', (byte)'-', (byte)'s', (byte)'e',  // sv-se
            (byte)'s', (byte)'w', (byte)'-', (byte)'c', (byte)'d',  // sw, sw-cd
            (byte)'s', (byte)'w', (byte)'-', (byte)'k', (byte)'e',  // sw-ke
            (byte)'s', (byte)'w', (byte)'-', (byte)'t', (byte)'z',  // sw-tz
            (byte)'s', (byte)'w', (byte)'-', (byte)'u', (byte)'g',  // sw-ug
            (byte)'s', (byte)'w', (byte)'c', (byte)'-', (byte)'c', (byte)'d',  // swc, swc-cd
            (byte)'s', (byte)'y', (byte)'r', (byte)'-', (byte)'s', (byte)'y',  // syr, syr-sy
            (byte)'t', (byte)'a', (byte)'-', (byte)'i', (byte)'n',  // ta, ta-in
            (byte)'t', (byte)'a', (byte)'-', (byte)'l', (byte)'k',  // ta-lk
            (byte)'t', (byte)'a', (byte)'-', (byte)'m', (byte)'y',  // ta-my
            (byte)'t', (byte)'a', (byte)'-', (byte)'s', (byte)'g',  // ta-sg
            (byte)'t', (byte)'e', (byte)'-', (byte)'i', (byte)'n',  // te, te-in
            (byte)'t', (byte)'e', (byte)'o', (byte)'-', (byte)'k', (byte)'e',  // teo, teo-ke
            (byte)'t', (byte)'e', (byte)'o', (byte)'-', (byte)'u', (byte)'g',  // teo-ug
            (byte)'t', (byte)'g', (byte)'-', (byte)'c', (byte)'y', (byte)'r', (byte)'l', (byte)'-', (byte)'t', (byte)'j',  // tg, tg-cyrl, tg-cyrl-tj
            (byte)'t', (byte)'h', (byte)'-', (byte)'t', (byte)'h',  // th, th-th
            (byte)'t', (byte)'i', (byte)'-', (byte)'e', (byte)'r',  // ti, ti-er
            (byte)'t', (byte)'i', (byte)'-', (byte)'e', (byte)'t',  // ti-et
            (byte)'t', (byte)'i', (byte)'g', (byte)'-', (byte)'e', (byte)'r',  // tig, tig-er
            (byte)'t', (byte)'k', (byte)'-', (byte)'t', (byte)'m',  // tk, tk-tm
            (byte)'t', (byte)'n', (byte)'-', (byte)'b', (byte)'w',  // tn, tn-bw
            (byte)'t', (byte)'n', (byte)'-', (byte)'z', (byte)'a',  // tn-za
            (byte)'t', (byte)'o', (byte)'-', (byte)'t', (byte)'o',  // to, to-to
            (byte)'t', (byte)'r', (byte)'-', (byte)'c', (byte)'y',  // tr, tr-cy
            (byte)'t', (byte)'r', (byte)'-', (byte)'t', (byte)'r',  // tr-tr
            (byte)'t', (byte)'s', (byte)'-', (byte)'z', (byte)'a',  // ts, ts-za
            (byte)'t', (byte)'t', (byte)'-', (byte)'r', (byte)'u',  // tt, tt-ru
            (byte)'t', (byte)'w', (byte)'q', (byte)'-', (byte)'n', (byte)'e',  // twq, twq-ne
            (byte)'t', (byte)'z', (byte)'m', (byte)'-', (byte)'a', (byte)'r', (byte)'a', (byte)'b', (byte)'-', (byte)'m', (byte)'a',  // tzm, tzm-arab, tzm-arab-ma
            (byte)'t', (byte)'z', (byte)'m', (byte)'-', (byte)'l', (byte)'a', (byte)'t', (byte)'n', (byte)'-', (byte)'d', (byte)'z',  // tzm-latn, tzm-latn-dz
            (byte)'t', (byte)'z', (byte)'m', (byte)'-', (byte)'l', (byte)'a', (byte)'t', (byte)'n', (byte)'-', (byte)'m', (byte)'a',  // tzm-latn-ma
            (byte)'t', (byte)'z', (byte)'m', (byte)'-', (byte)'t', (byte)'f', (byte)'n', (byte)'g', (byte)'-', (byte)'m', (byte)'a',  // tzm-tfng, tzm-tfng-ma
            (byte)'u', (byte)'g', (byte)'-', (byte)'c', (byte)'n',  // ug, ug-cn
            (byte)'u', (byte)'k', (byte)'-', (byte)'u', (byte)'a',  // uk, uk-ua
            (byte)'u', (byte)'r', (byte)'-', (byte)'i', (byte)'n',  // ur, ur-in
            (byte)'u', (byte)'r', (byte)'-', (byte)'p', (byte)'k',  // ur-pk
            (byte)'u', (byte)'z', (byte)'-', (byte)'a', (byte)'r', (byte)'a', (byte)'b', (byte)'-', (byte)'a', (byte)'f',  // uz, uz-arab, uz-arab-af
            (byte)'u', (byte)'z', (byte)'-', (byte)'c', (byte)'y', (byte)'r', (byte)'l', (byte)'-', (byte)'u', (byte)'z',  // uz-cyrl, uz-cyrl-uz
            (byte)'u', (byte)'z', (byte)'-', (byte)'l', (byte)'a', (byte)'t', (byte)'n', (byte)'-', (byte)'u', (byte)'z',  // uz-latn, uz-latn-uz
            (byte)'v', (byte)'a', (byte)'i', (byte)'-', (byte)'l', (byte)'a', (byte)'t', (byte)'n', (byte)'-', (byte)'l', (byte)'r',  // vai, vai-latn, vai-latn-lr
            (byte)'v', (byte)'a', (byte)'i', (byte)'-', (byte)'v', (byte)'a', (byte)'i', (byte)'i', (byte)'-', (byte)'l', (byte)'r',  // vai-vaii, vai-vaii-lr
            (byte)'v', (byte)'e', (byte)'-', (byte)'z', (byte)'a',  // ve, ve-za
            (byte)'v', (byte)'i', (byte)'-', (byte)'v', (byte)'n',  // vi, vi-vn
            (byte)'v', (byte)'o', (byte)'-', (byte)'0', (byte)'0', (byte)'1',  // vo, vo-001
            (byte)'v', (byte)'u', (byte)'n', (byte)'-', (byte)'t', (byte)'z',  // vun, vun-tz
            (byte)'w', (byte)'a', (byte)'e', (byte)'-', (byte)'c', (byte)'h',  // wae, wae-ch
            (byte)'w', (byte)'a', (byte)'l', (byte)'-', (byte)'e', (byte)'t',  // wal, wal-et
            (byte)'w', (byte)'o', (byte)'-', (byte)'s', (byte)'n',  // wo, wo-sn
            (byte)'x', (byte)'-', (byte)'i', (byte)'v', (byte)'_', (byte)'m', (byte)'a', (byte)'t', (byte)'h', (byte)'a', (byte)'n',  // x-iv_mathan
            (byte)'x', (byte)'h', (byte)'-', (byte)'z', (byte)'a',  // xh, xh-za
            (byte)'x', (byte)'o', (byte)'g', (byte)'-', (byte)'u', (byte)'g',  // xog, xog-ug
            (byte)'y', (byte)'a', (byte)'v', (byte)'-', (byte)'c', (byte)'m',  // yav, yav-cm
            (byte)'y', (byte)'i', (byte)'-', (byte)'0', (byte)'0', (byte)'1',
            (byte)'y', (byte)'o', (byte)'-', (byte)'b', (byte)'j',  // yo, yo-bj
            (byte)'y', (byte)'o', (byte)'-', (byte)'n', (byte)'g',  // yo-ng
            (byte)'y', (byte)'u', (byte)'e', (byte)'-', (byte)'h', (byte)'k',  // yue, yue-hk
            (byte)'z', (byte)'g', (byte)'h', (byte)'-', (byte)'t', (byte)'f', (byte)'n', (byte)'g', (byte)'-', (byte)'m', (byte)'a',  // zgh, zgh-tfng, zgh-tfng-ma
            (byte)'z', (byte)'h', (byte)'-', (byte)'c', (byte)'h', (byte)'s',  // zh, zh-chs
            (byte)'z', (byte)'h', (byte)'-', (byte)'c', (byte)'h', (byte)'t',  // zh-cht
            (byte)'z', (byte)'h', (byte)'-', (byte)'c', (byte)'n', (byte)'_', (byte)'p', (byte)'h', (byte)'o', (byte)'n', (byte)'e', (byte)'b',  // zh-cn, zh-cn_phoneb
            (byte)'z', (byte)'h', (byte)'-', (byte)'c', (byte)'n', (byte)'_', (byte)'s', (byte)'t', (byte)'r', (byte)'o', (byte)'k', (byte)'e',  // zh-cn_stroke
            (byte)'z', (byte)'h', (byte)'-', (byte)'h', (byte)'a', (byte)'n', (byte)'s', (byte)'-', (byte)'h', (byte)'k',  // zh-hans, zh-hans-hk
            (byte)'z', (byte)'h', (byte)'-', (byte)'h', (byte)'a', (byte)'n', (byte)'s', (byte)'-', (byte)'m', (byte)'o',  // zh-hans-mo
            (byte)'z', (byte)'h', (byte)'-', (byte)'h', (byte)'a', (byte)'n', (byte)'t',  // zh-hant
            (byte)'z', (byte)'h', (byte)'-', (byte)'h', (byte)'k', (byte)'_', (byte)'r', (byte)'a', (byte)'d', (byte)'s', (byte)'t', (byte)'r',  // zh-hk, zh-hk_radstr
            (byte)'z', (byte)'h', (byte)'-', (byte)'m', (byte)'o', (byte)'_', (byte)'r', (byte)'a', (byte)'d', (byte)'s', (byte)'t', (byte)'r',  // zh-mo, zh-mo_radstr
            (byte)'z', (byte)'h', (byte)'-', (byte)'m', (byte)'o', (byte)'_', (byte)'s', (byte)'t', (byte)'r', (byte)'o', (byte)'k', (byte)'e',  // zh-mo_stroke
            (byte)'z', (byte)'h', (byte)'-', (byte)'s', (byte)'g', (byte)'_', (byte)'p', (byte)'h', (byte)'o', (byte)'n', (byte)'e', (byte)'b',  // zh-sg, zh-sg_phoneb
            (byte)'z', (byte)'h', (byte)'-', (byte)'s', (byte)'g', (byte)'_', (byte)'s', (byte)'t', (byte)'r', (byte)'o', (byte)'k', (byte)'e',  // zh-sg_stroke
            (byte)'z', (byte)'h', (byte)'-', (byte)'t', (byte)'w', (byte)'_', (byte)'p', (byte)'r', (byte)'o', (byte)'n', (byte)'u', (byte)'n',  // zh-tw, zh-tw_pronun
            (byte)'z', (byte)'h', (byte)'-', (byte)'t', (byte)'w', (byte)'_', (byte)'r', (byte)'a', (byte)'d', (byte)'s', (byte)'t', (byte)'r',  // zh-tw_radstr
            (byte)'z', (byte)'u', (byte)'-', (byte)'z', (byte)'a',  // zu, zu-za
        };

        private const int LocaleLongestName = 14;
        private const int CulturesCount = 864;

        //
        // Table which holds index into LocalesNames data and length of the string for each locale
        // Values are binary searched and need to be sorted alphabetically
        //
        private static readonly ushort[] s_localesNamesIndexes = new ushort[CulturesCount]
        {
            0 << 4 | 2,     // aa
            0 << 4 | 5,     // aa-dj
            5 << 4 | 5,     // aa-er
            10 << 4 | 5,    // aa-et
            15 << 4 | 2,    // af
            15 << 4 | 5,    // af-na
            20 << 4 | 5,    // af-za
            25 << 4 | 3,    // agq
            25 << 4 | 6,    // agq-cm
            31 << 4 | 2,    // ak
            31 << 4 | 5,    // ak-gh
            36 << 4 | 2,    // am
            36 << 4 | 5,    // am-et
            41 << 4 | 2,    // ar
            41 << 4 | 6,    // ar-001
            47 << 4 | 5,    // ar-ae
            52 << 4 | 5,    // ar-bh
            57 << 4 | 5,    // ar-dj
            62 << 4 | 5,    // ar-dz
            67 << 4 | 5,    // ar-eg
            72 << 4 | 5,    // ar-er
            77 << 4 | 5,    // ar-il
            82 << 4 | 5,    // ar-iq
            87 << 4 | 5,    // ar-jo
            92 << 4 | 5,    // ar-km
            97 << 4 | 5,    // ar-kw
            102 << 4 | 5,   // ar-lb
            107 << 4 | 5,   // ar-ly
            112 << 4 | 5,   // ar-ma
            117 << 4 | 5,   // ar-mr
            122 << 4 | 5,   // ar-om
            127 << 4 | 5,   // ar-ps
            132 << 4 | 5,   // ar-qa
            137 << 4 | 5,   // ar-sa
            142 << 4 | 5,   // ar-sd
            147 << 4 | 5,   // ar-so
            152 << 4 | 5,   // ar-ss
            157 << 4 | 5,   // ar-sy
            162 << 4 | 5,   // ar-td
            167 << 4 | 5,   // ar-tn
            172 << 4 | 5,   // ar-ye
            177 << 4 | 3,   // arn
            177 << 4 | 6,   // arn-cl
            183 << 4 | 2,   // as
            183 << 4 | 5,   // as-in
            188 << 4 | 3,   // asa
            188 << 4 | 6,   // asa-tz
            194 << 4 | 3,   // ast
            194 << 4 | 6,   // ast-es
            200 << 4 | 2,   // az
            200 << 4 | 7,   // az-cyrl
            200 << 4 | 10,  // az-cyrl-az
            210 << 4 | 7,   // az-latn
            210 << 4 | 10,  // az-latn-az
            220 << 4 | 2,   // ba
            220 << 4 | 5,   // ba-ru
            225 << 4 | 3,   // bas
            225 << 4 | 6,   // bas-cm
            231 << 4 | 2,   // be
            231 << 4 | 5,   // be-by
            236 << 4 | 3,   // bem
            236 << 4 | 6,   // bem-zm
            242 << 4 | 3,   // bez
            242 << 4 | 6,   // bez-tz
            248 << 4 | 2,   // bg
            248 << 4 | 5,   // bg-bg
            253 << 4 | 3,   // bin
            253 << 4 | 6,   // bin-ng
            259 << 4 | 2,   // bm
            259 << 4 | 7,   // bm-latn
            259 << 4 | 10,  // bm-latn-ml
            269 << 4 | 2,   // bn
            269 << 4 | 5,   // bn-bd
            274 << 4 | 5,   // bn-in
            279 << 4 | 2,   // bo
            279 << 4 | 5,   // bo-cn
            284 << 4 | 5,   // bo-in
            289 << 4 | 2,   // br
            289 << 4 | 5,   // br-fr
            294 << 4 | 3,   // brx
            294 << 4 | 6,   // brx-in
            300 << 4 | 2,   // bs
            300 << 4 | 7,   // bs-cyrl
            300 << 4 | 10,  // bs-cyrl-ba
            310 << 4 | 7,   // bs-latn
            310 << 4 | 10,  // bs-latn-ba
            320 << 4 | 3,   // byn
            320 << 4 | 6,   // byn-er
            326 << 4 | 2,   // ca
            326 << 4 | 5,   // ca-ad
            331 << 4 | 5,   // ca-es
            331 << 4 | 14,  // ca-es-valencia
            345 << 4 | 5,   // ca-fr
            350 << 4 | 5,   // ca-it
            355 << 4 | 2,   // ce
            355 << 4 | 5,   // ce-ru
            360 << 4 | 3,   // cgg
            360 << 4 | 6,   // cgg-ug
            366 << 4 | 3,   // chr
            366 << 4 | 8,   // chr-cher
            366 << 4 | 11,  // chr-cher-us
            377 << 4 | 2,   // co
            377 << 4 | 5,   // co-fr
            382 << 4 | 2,   // cs
            382 << 4 | 5,   // cs-cz
            387 << 4 | 2,   // cu
            387 << 4 | 5,   // cu-ru
            392 << 4 | 2,   // cy
            392 << 4 | 5,   // cy-gb
            397 << 4 | 2,   // da
            397 << 4 | 5,   // da-dk
            402 << 4 | 5,   // da-gl
            407 << 4 | 3,   // dav
            407 << 4 | 6,   // dav-ke
            413 << 4 | 2,   // de
            413 << 4 | 5,   // de-at
            418 << 4 | 5,   // de-be
            423 << 4 | 5,   // de-ch
            428 << 4 | 5,   // de-de
            428 << 4 | 12,  // de-de_phoneb
            440 << 4 | 5,   // de-it
            445 << 4 | 5,   // de-li
            450 << 4 | 5,   // de-lu
            455 << 4 | 3,   // dje
            455 << 4 | 6,   // dje-ne
            461 << 4 | 3,   // dsb
            461 << 4 | 6,   // dsb-de
            467 << 4 | 3,   // dua
            467 << 4 | 6,   // dua-cm
            473 << 4 | 2,   // dv
            473 << 4 | 5,   // dv-mv
            478 << 4 | 3,   // dyo
            478 << 4 | 6,   // dyo-sn
            484 << 4 | 2,   // dz
            484 << 4 | 5,   // dz-bt
            489 << 4 | 3,   // ebu
            489 << 4 | 6,   // ebu-ke
            495 << 4 | 2,   // ee
            495 << 4 | 5,   // ee-gh
            500 << 4 | 5,   // ee-tg
            505 << 4 | 2,   // el
            505 << 4 | 5,   // el-cy
            510 << 4 | 5,   // el-gr
            515 << 4 | 2,   // en
            515 << 4 | 6,   // en-001
            521 << 4 | 6,   // en-029
            527 << 4 | 6,   // en-150
            533 << 4 | 5,   // en-ag
            538 << 4 | 5,   // en-ai
            543 << 4 | 5,   // en-as
            548 << 4 | 5,   // en-at
            553 << 4 | 5,   // en-au
            558 << 4 | 5,   // en-bb
            563 << 4 | 5,   // en-be
            568 << 4 | 5,   // en-bi
            573 << 4 | 5,   // en-bm
            578 << 4 | 5,   // en-bs
            583 << 4 | 5,   // en-bw
            588 << 4 | 5,   // en-bz
            593 << 4 | 5,   // en-ca
            598 << 4 | 5,   // en-cc
            603 << 4 | 5,   // en-ch
            608 << 4 | 5,   // en-ck
            613 << 4 | 5,   // en-cm
            618 << 4 | 5,   // en-cx
            623 << 4 | 5,   // en-cy
            628 << 4 | 5,   // en-de
            633 << 4 | 5,   // en-dk
            638 << 4 | 5,   // en-dm
            643 << 4 | 5,   // en-er
            648 << 4 | 5,   // en-fi
            653 << 4 | 5,   // en-fj
            658 << 4 | 5,   // en-fk
            663 << 4 | 5,   // en-fm
            668 << 4 | 5,   // en-gb
            673 << 4 | 5,   // en-gd
            678 << 4 | 5,   // en-gg
            683 << 4 | 5,   // en-gh
            688 << 4 | 5,   // en-gi
            693 << 4 | 5,   // en-gm
            698 << 4 | 5,   // en-gu
            703 << 4 | 5,   // en-gy
            708 << 4 | 5,   // en-hk
            713 << 4 | 5,   // en-id
            718 << 4 | 5,   // en-ie
            723 << 4 | 5,   // en-il
            728 << 4 | 5,   // en-im
            733 << 4 | 5,   // en-in
            738 << 4 | 5,   // en-io
            743 << 4 | 5,   // en-je
            748 << 4 | 5,   // en-jm
            753 << 4 | 5,   // en-ke
            758 << 4 | 5,   // en-ki
            763 << 4 | 5,   // en-kn
            768 << 4 | 5,   // en-ky
            773 << 4 | 5,   // en-lc
            778 << 4 | 5,   // en-lr
            783 << 4 | 5,   // en-ls
            788 << 4 | 5,   // en-mg
            793 << 4 | 5,   // en-mh
            798 << 4 | 5,   // en-mo
            803 << 4 | 5,   // en-mp
            808 << 4 | 5,   // en-ms
            813 << 4 | 5,   // en-mt
            818 << 4 | 5,   // en-mu
            823 << 4 | 5,   // en-mw
            828 << 4 | 5,   // en-my
            833 << 4 | 5,   // en-na
            838 << 4 | 5,   // en-nf
            843 << 4 | 5,   // en-ng
            848 << 4 | 5,   // en-nl
            853 << 4 | 5,   // en-nr
            858 << 4 | 5,   // en-nu
            863 << 4 | 5,   // en-nz
            868 << 4 | 5,   // en-pg
            873 << 4 | 5,   // en-ph
            878 << 4 | 5,   // en-pk
            883 << 4 | 5,   // en-pn
            888 << 4 | 5,   // en-pr
            893 << 4 | 5,   // en-pw
            898 << 4 | 5,   // en-rw
            903 << 4 | 5,   // en-sb
            908 << 4 | 5,   // en-sc
            913 << 4 | 5,   // en-sd
            918 << 4 | 5,   // en-se
            923 << 4 | 5,   // en-sg
            928 << 4 | 5,   // en-sh
            933 << 4 | 5,   // en-si
            938 << 4 | 5,   // en-sl
            943 << 4 | 5,   // en-ss
            948 << 4 | 5,   // en-sx
            953 << 4 | 5,   // en-sz
            958 << 4 | 5,   // en-tc
            963 << 4 | 5,   // en-tk
            968 << 4 | 5,   // en-to
            973 << 4 | 5,   // en-tt
            978 << 4 | 5,   // en-tv
            983 << 4 | 5,   // en-tz
            988 << 4 | 5,   // en-ug
            993 << 4 | 5,   // en-um
            998 << 4 | 5,   // en-us
            1003 << 4 | 5,  // en-vc
            1008 << 4 | 5,  // en-vg
            1013 << 4 | 5,  // en-vi
            1018 << 4 | 5,  // en-vu
            1023 << 4 | 5,  // en-ws
            1028 << 4 | 5,  // en-za
            1033 << 4 | 5,  // en-zm
            1038 << 4 | 5,  // en-zw
            1043 << 4 | 2,  // eo
            1043 << 4 | 6,  // eo-001
            1049 << 4 | 2,  // es
            1049 << 4 | 6,  // es-419
            1055 << 4 | 5,  // es-ar
            1060 << 4 | 5,  // es-bo
            1065 << 4 | 5,  // es-br
            1070 << 4 | 5,  // es-cl
            1075 << 4 | 5,  // es-co
            1080 << 4 | 5,  // es-cr
            1085 << 4 | 5,  // es-cu
            1090 << 4 | 5,  // es-do
            1095 << 4 | 5,  // es-ec
            1100 << 4 | 5,  // es-es
            1100 << 4 | 12, // es-es_tradnl
            1112 << 4 | 5,  // es-gq
            1117 << 4 | 5,  // es-gt
            1122 << 4 | 5,  // es-hn
            1127 << 4 | 5,  // es-mx
            1132 << 4 | 5,  // es-ni
            1137 << 4 | 5,  // es-pa
            1142 << 4 | 5,  // es-pe
            1147 << 4 | 5,  // es-ph
            1152 << 4 | 5,  // es-pr
            1157 << 4 | 5,  // es-py
            1162 << 4 | 5,  // es-sv
            1167 << 4 | 5,  // es-us
            1172 << 4 | 5,  // es-uy
            1177 << 4 | 5,  // es-ve
            1182 << 4 | 2,  // et
            1182 << 4 | 5,  // et-ee
            1187 << 4 | 2,  // eu
            1187 << 4 | 5,  // eu-es
            1192 << 4 | 3,  // ewo
            1192 << 4 | 6,  // ewo-cm
            1198 << 4 | 2,  // fa
            1198 << 4 | 5,  // fa-ir
            1203 << 4 | 2,  // ff
            1203 << 4 | 5,  // ff-cm
            1208 << 4 | 5,  // ff-gn
            1213 << 4 | 7,  // ff-latn
            1213 << 4 | 10, // ff-latn-sn
            1223 << 4 | 5,  // ff-mr
            1228 << 4 | 5,  // ff-ng
            1233 << 4 | 2,  // fi
            1233 << 4 | 5,  // fi-fi
            1238 << 4 | 3,  // fil
            1238 << 4 | 6,  // fil-ph
            1244 << 4 | 2,  // fo
            1244 << 4 | 5,  // fo-dk
            1249 << 4 | 5,  // fo-fo
            1254 << 4 | 2,  // fr
            1254 << 4 | 6,  // fr-029
            1260 << 4 | 5,  // fr-be
            1265 << 4 | 5,  // fr-bf
            1270 << 4 | 5,  // fr-bi
            1275 << 4 | 5,  // fr-bj
            1280 << 4 | 5,  // fr-bl
            1285 << 4 | 5,  // fr-ca
            1290 << 4 | 5,  // fr-cd
            1295 << 4 | 5,  // fr-cf
            1300 << 4 | 5,  // fr-cg
            1305 << 4 | 5,  // fr-ch
            1310 << 4 | 5,  // fr-ci
            1315 << 4 | 5,  // fr-cm
            1320 << 4 | 5,  // fr-dj
            1325 << 4 | 5,  // fr-dz
            1330 << 4 | 5,  // fr-fr
            1335 << 4 | 5,  // fr-ga
            1340 << 4 | 5,  // fr-gf
            1345 << 4 | 5,  // fr-gn
            1350 << 4 | 5,  // fr-gp
            1355 << 4 | 5,  // fr-gq
            1360 << 4 | 5,  // fr-ht
            1365 << 4 | 5,  // fr-km
            1370 << 4 | 5,  // fr-lu
            1375 << 4 | 5,  // fr-ma
            1380 << 4 | 5,  // fr-mc
            1385 << 4 | 5,  // fr-mf
            1390 << 4 | 5,  // fr-mg
            1395 << 4 | 5,  // fr-ml
            1400 << 4 | 5,  // fr-mq
            1405 << 4 | 5,  // fr-mr
            1410 << 4 | 5,  // fr-mu
            1415 << 4 | 5,  // fr-nc
            1420 << 4 | 5,  // fr-ne
            1425 << 4 | 5,  // fr-pf
            1430 << 4 | 5,  // fr-pm
            1435 << 4 | 5,  // fr-re
            1440 << 4 | 5,  // fr-rw
            1445 << 4 | 5,  // fr-sc
            1450 << 4 | 5,  // fr-sn
            1455 << 4 | 5,  // fr-sy
            1460 << 4 | 5,  // fr-td
            1465 << 4 | 5,  // fr-tg
            1470 << 4 | 5,  // fr-tn
            1475 << 4 | 5,  // fr-vu
            1480 << 4 | 5,  // fr-wf
            1485 << 4 | 5,  // fr-yt
            1490 << 4 | 3,  // fur
            1490 << 4 | 6,  // fur-it
            1496 << 4 | 2,  // fy
            1496 << 4 | 5,  // fy-nl
            1501 << 4 | 2,  // ga
            1501 << 4 | 5,  // ga-ie
            1506 << 4 | 2,  // gd
            1506 << 4 | 5,  // gd-gb
            1511 << 4 | 2,  // gl
            1511 << 4 | 5,  // gl-es
            1516 << 4 | 2,  // gn
            1516 << 4 | 5,  // gn-py
            1521 << 4 | 3,  // gsw
            1521 << 4 | 6,  // gsw-ch
            1527 << 4 | 6,  // gsw-fr
            1533 << 4 | 6,  // gsw-li
            1539 << 4 | 2,  // gu
            1539 << 4 | 5,  // gu-in
            1544 << 4 | 3,  // guz
            1544 << 4 | 6,  // guz-ke
            1550 << 4 | 2,  // gv
            1550 << 4 | 5,  // gv-im
            1555 << 4 | 2,  // ha
            1555 << 4 | 7,  // ha-latn
            1555 << 4 | 10, // ha-latn-gh
            1565 << 4 | 10, // ha-latn-ne
            1575 << 4 | 10, // ha-latn-ng
            1585 << 4 | 3,  // haw
            1585 << 4 | 6,  // haw-us
            1591 << 4 | 2,  // he
            1591 << 4 | 5,  // he-il
            1596 << 4 | 2,  // hi
            1596 << 4 | 5,  // hi-in
            1601 << 4 | 2,  // hr
            1601 << 4 | 5,  // hr-ba
            1606 << 4 | 5,  // hr-hr
            1611 << 4 | 3,  // hsb
            1611 << 4 | 6,  // hsb-de
            1617 << 4 | 2,  // hu
            1617 << 4 | 5,  // hu-hu
            1617 << 4 | 12, // hu-hu_technl
            1629 << 4 | 2,  // hy
            1629 << 4 | 5,  // hy-am
            1634 << 4 | 2,  // ia
            1634 << 4 | 6,  // ia-001
            1640 << 4 | 5,  // ia-fr
            1645 << 4 | 3,  // ibb
            1645 << 4 | 6,  // ibb-ng
            1651 << 4 | 2,  // id
            1651 << 4 | 5,  // id-id
            1656 << 4 | 2,  // ig
            1656 << 4 | 5,  // ig-ng
            1661 << 4 | 2,  // ii
            1661 << 4 | 5,  // ii-cn
            1666 << 4 | 2,  // is
            1666 << 4 | 5,  // is-is
            1671 << 4 | 2,  // it
            1671 << 4 | 5,  // it-ch
            1676 << 4 | 5,  // it-it
            1681 << 4 | 5,  // it-sm
            1686 << 4 | 2,  // iu
            1686 << 4 | 7,  // iu-cans
            1686 << 4 | 10, // iu-cans-ca
            1696 << 4 | 7,  // iu-latn
            1696 << 4 | 10, // iu-latn-ca
            1706 << 4 | 2,  // ja
            1706 << 4 | 5,  // ja-jp
            1706 << 4 | 12, // ja-jp_radstr
            1718 << 4 | 3,  // jgo
            1718 << 4 | 6,  // jgo-cm
            1724 << 4 | 3,  // jmc
            1724 << 4 | 6,  // jmc-tz
            1730 << 4 | 2,  // jv
            1730 << 4 | 7,  // jv-java
            1730 << 4 | 10, // jv-java-id
            1740 << 4 | 7,  // jv-latn
            1740 << 4 | 10, // jv-latn-id
            1750 << 4 | 2,  // ka
            1750 << 4 | 5,  // ka-ge
            1750 << 4 | 12, // ka-ge_modern
            1762 << 4 | 3,  // kab
            1762 << 4 | 6,  // kab-dz
            1768 << 4 | 3,  // kam
            1768 << 4 | 6,  // kam-ke
            1774 << 4 | 3,  // kde
            1774 << 4 | 6,  // kde-tz
            1780 << 4 | 3,  // kea
            1780 << 4 | 6,  // kea-cv
            1786 << 4 | 3,  // khq
            1786 << 4 | 6,  // khq-ml
            1792 << 4 | 2,  // ki
            1792 << 4 | 5,  // ki-ke
            1797 << 4 | 2,  // kk
            1797 << 4 | 5,  // kk-kz
            1802 << 4 | 3,  // kkj
            1802 << 4 | 6,  // kkj-cm
            1808 << 4 | 2,  // kl
            1808 << 4 | 5,  // kl-gl
            1813 << 4 | 3,  // kln
            1813 << 4 | 6,  // kln-ke
            1819 << 4 | 2,  // km
            1819 << 4 | 5,  // km-kh
            1824 << 4 | 2,  // kn
            1824 << 4 | 5,  // kn-in
            1829 << 4 | 2,  // ko
            1829 << 4 | 5,  // ko-kp
            1834 << 4 | 5,  // ko-kr
            1839 << 4 | 3,  // kok
            1839 << 4 | 6,  // kok-in
            1845 << 4 | 2,  // kr
            1845 << 4 | 5,  // kr-ng
            1850 << 4 | 2,  // ks
            1850 << 4 | 7,  // ks-arab
            1850 << 4 | 10, // ks-arab-in
            1860 << 4 | 7,  // ks-deva
            1860 << 4 | 10, // ks-deva-in
            1870 << 4 | 3,  // ksb
            1870 << 4 | 6,  // ksb-tz
            1876 << 4 | 3,  // ksf
            1876 << 4 | 6,  // ksf-cm
            1882 << 4 | 3,  // ksh
            1882 << 4 | 6,  // ksh-de
            1888 << 4 | 2,  // ku
            1888 << 4 | 7,  // ku-arab
            1888 << 4 | 10, // ku-arab-iq
            1898 << 4 | 10, // ku-arab-ir
            1908 << 4 | 2,  // kw
            1908 << 4 | 5,  // kw-gb
            1913 << 4 | 2,  // ky
            1913 << 4 | 5,  // ky-kg
            1918 << 4 | 2,  // la
            1918 << 4 | 6,  // la-001
            1924 << 4 | 3,  // lag
            1924 << 4 | 6,  // lag-tz
            1930 << 4 | 2,  // lb
            1930 << 4 | 5,  // lb-lu
            1935 << 4 | 2,  // lg
            1935 << 4 | 5,  // lg-ug
            1940 << 4 | 3,  // lkt
            1940 << 4 | 6,  // lkt-us
            1946 << 4 | 2,  // ln
            1946 << 4 | 5,  // ln-ao
            1951 << 4 | 5,  // ln-cd
            1956 << 4 | 5,  // ln-cf
            1961 << 4 | 5,  // ln-cg
            1966 << 4 | 2,  // lo
            1966 << 4 | 5,  // lo-la
            1971 << 4 | 3,  // lrc
            1971 << 4 | 6,  // lrc-iq
            1977 << 4 | 6,  // lrc-ir
            1983 << 4 | 2,  // lt
            1983 << 4 | 5,  // lt-lt
            1988 << 4 | 2,  // lu
            1988 << 4 | 5,  // lu-cd
            1993 << 4 | 3,  // luo
            1993 << 4 | 6,  // luo-ke
            1999 << 4 | 3,  // luy
            1999 << 4 | 6,  // luy-ke
            2005 << 4 | 2,  // lv
            2005 << 4 | 5,  // lv-lv
            2010 << 4 | 3,  // mas
            2010 << 4 | 6,  // mas-ke
            2016 << 4 | 6,  // mas-tz
            2022 << 4 | 3,  // mer
            2022 << 4 | 6,  // mer-ke
            2028 << 4 | 3,  // mfe
            2028 << 4 | 6,  // mfe-mu
            2034 << 4 | 2,  // mg
            2034 << 4 | 5,  // mg-mg
            2039 << 4 | 3,  // mgh
            2039 << 4 | 6,  // mgh-mz
            2045 << 4 | 3,  // mgo
            2045 << 4 | 6,  // mgo-cm
            2051 << 4 | 2,  // mi
            2051 << 4 | 5,  // mi-nz
            2056 << 4 | 2,  // mk
            2056 << 4 | 5,  // mk-mk
            2061 << 4 | 2,  // ml
            2061 << 4 | 5,  // ml-in
            2066 << 4 | 2,  // mn
            2066 << 4 | 7,  // mn-cyrl
            2073 << 4 | 5,  // mn-mn
            2078 << 4 | 7,  // mn-mong
            2078 << 4 | 10, // mn-mong-cn
            2088 << 4 | 10, // mn-mong-mn
            2098 << 4 | 3,  // mni
            2098 << 4 | 6,  // mni-in
            2104 << 4 | 3,  // moh
            2104 << 4 | 6,  // moh-ca
            2110 << 4 | 2,  // mr
            2110 << 4 | 5,  // mr-in
            2115 << 4 | 2,  // ms
            2115 << 4 | 5,  // ms-bn
            2120 << 4 | 5,  // ms-my
            2125 << 4 | 5,  // ms-sg
            2130 << 4 | 2,  // mt
            2130 << 4 | 5,  // mt-mt
            2135 << 4 | 3,  // mua
            2135 << 4 | 6,  // mua-cm
            2141 << 4 | 2,  // my
            2141 << 4 | 5,  // my-mm
            2146 << 4 | 3,  // mzn
            2146 << 4 | 6,  // mzn-ir
            2152 << 4 | 3,  // naq
            2152 << 4 | 6,  // naq-na
            2158 << 4 | 2,  // nb
            2158 << 4 | 5,  // nb-no
            2163 << 4 | 5,  // nb-sj
            2168 << 4 | 2,  // nd
            2168 << 4 | 5,  // nd-zw
            2173 << 4 | 3,  // nds
            2173 << 4 | 6,  // nds-de
            2179 << 4 | 6,  // nds-nl
            2185 << 4 | 2,  // ne
            2185 << 4 | 5,  // ne-in
            2190 << 4 | 5,  // ne-np
            2195 << 4 | 2,  // nl
            2195 << 4 | 5,  // nl-aw
            2200 << 4 | 5,  // nl-be
            2205 << 4 | 5,  // nl-bq
            2210 << 4 | 5,  // nl-cw
            2215 << 4 | 5,  // nl-nl
            2220 << 4 | 5,  // nl-sr
            2225 << 4 | 5,  // nl-sx
            2230 << 4 | 3,  // nmg
            2230 << 4 | 6,  // nmg-cm
            2236 << 4 | 2,  // nn
            2236 << 4 | 5,  // nn-no
            2241 << 4 | 3,  // nnh
            2241 << 4 | 6,  // nnh-cm
            2247 << 4 | 2,  // no
            2249 << 4 | 3,  // nqo
            2249 << 4 | 6,  // nqo-gn
            2255 << 4 | 2,  // nr
            2255 << 4 | 5,  // nr-za
            2260 << 4 | 3,  // nso
            2260 << 4 | 6,  // nso-za
            2266 << 4 | 3,  // nus
            2266 << 4 | 6,  // nus-ss
            2272 << 4 | 3,  // nyn
            2272 << 4 | 6,  // nyn-ug
            2278 << 4 | 2,  // oc
            2278 << 4 | 5,  // oc-fr
            2283 << 4 | 2,  // om
            2283 << 4 | 5,  // om-et
            2288 << 4 | 5,  // om-ke
            2293 << 4 | 2,  // or
            2293 << 4 | 5,  // or-in
            2298 << 4 | 2,  // os
            2298 << 4 | 5,  // os-ge
            2303 << 4 | 5,  // os-ru
            2308 << 4 | 2,  // pa
            2308 << 4 | 7,  // pa-arab
            2308 << 4 | 10, // pa-arab-pk
            2318 << 4 | 5,  // pa-in
            2323 << 4 | 3,  // pap
            2323 << 4 | 7,  // pap-029
            2330 << 4 | 2,  // pl
            2330 << 4 | 5,  // pl-pl
            2335 << 4 | 3,  // prg
            2335 << 4 | 7,  // prg-001
            2342 << 4 | 3,  // prs
            2342 << 4 | 6,  // prs-af
            2348 << 4 | 2,  // ps
            2348 << 4 | 5,  // ps-af
            2353 << 4 | 2,  // pt
            2353 << 4 | 5,  // pt-ao
            2358 << 4 | 5,  // pt-br
            2363 << 4 | 5,  // pt-ch
            2368 << 4 | 5,  // pt-cv
            2373 << 4 | 5,  // pt-gq
            2378 << 4 | 5,  // pt-gw
            2383 << 4 | 5,  // pt-lu
            2388 << 4 | 5,  // pt-mo
            2393 << 4 | 5,  // pt-mz
            2398 << 4 | 5,  // pt-pt
            2403 << 4 | 5,  // pt-st
            2408 << 4 | 5,  // pt-tl
            2413 << 4 | 13, // qps-latn-x-sh
            2426 << 4 | 8,  // qps-ploc
            2426 << 4 | 9,  // qps-ploca
            2435 << 4 | 9,  // qps-plocm
            2444 << 4 | 3,  // quc
            2444 << 4 | 8,  // quc-latn
            2444 << 4 | 11, // quc-latn-gt
            2455 << 4 | 3,  // quz
            2455 << 4 | 6,  // quz-bo
            2461 << 4 | 6,  // quz-ec
            2467 << 4 | 6,  // quz-pe
            2473 << 4 | 2,  // rm
            2473 << 4 | 5,  // rm-ch
            2478 << 4 | 2,  // rn
            2478 << 4 | 5,  // rn-bi
            2483 << 4 | 2,  // ro
            2483 << 4 | 5,  // ro-md
            2488 << 4 | 5,  // ro-ro
            2493 << 4 | 3,  // rof
            2493 << 4 | 6,  // rof-tz
            2499 << 4 | 2,  // ru
            2499 << 4 | 5,  // ru-by
            2504 << 4 | 5,  // ru-kg
            2509 << 4 | 5,  // ru-kz
            2514 << 4 | 5,  // ru-md
            2519 << 4 | 5,  // ru-ru
            2524 << 4 | 5,  // ru-ua
            2529 << 4 | 2,  // rw
            2529 << 4 | 5,  // rw-rw
            2534 << 4 | 3,  // rwk
            2534 << 4 | 6,  // rwk-tz
            2540 << 4 | 2,  // sa
            2540 << 4 | 5,  // sa-in
            2545 << 4 | 3,  // sah
            2545 << 4 | 6,  // sah-ru
            2551 << 4 | 3,  // saq
            2551 << 4 | 6,  // saq-ke
            2557 << 4 | 3,  // sbp
            2557 << 4 | 6,  // sbp-tz
            2563 << 4 | 2,  // sd
            2563 << 4 | 7,  // sd-arab
            2563 << 4 | 10, // sd-arab-pk
            2573 << 4 | 7,  // sd-deva
            2573 << 4 | 10, // sd-deva-in
            2583 << 4 | 2,  // se
            2583 << 4 | 5,  // se-fi
            2588 << 4 | 5,  // se-no
            2593 << 4 | 5,  // se-se
            2598 << 4 | 3,  // seh
            2598 << 4 | 6,  // seh-mz
            2604 << 4 | 3,  // ses
            2604 << 4 | 6,  // ses-ml
            2610 << 4 | 2,  // sg
            2610 << 4 | 5,  // sg-cf
            2615 << 4 | 3,  // shi
            2615 << 4 | 8,  // shi-latn
            2615 << 4 | 11, // shi-latn-ma
            2626 << 4 | 8,  // shi-tfng
            2626 << 4 | 11, // shi-tfng-ma
            2637 << 4 | 2,  // si
            2637 << 4 | 5,  // si-lk
            2642 << 4 | 2,  // sk
            2642 << 4 | 5,  // sk-sk
            2647 << 4 | 2,  // sl
            2647 << 4 | 5,  // sl-si
            2652 << 4 | 3,  // sma
            2652 << 4 | 6,  // sma-no
            2658 << 4 | 6,  // sma-se
            2664 << 4 | 3,  // smj
            2664 << 4 | 6,  // smj-no
            2670 << 4 | 6,  // smj-se
            2676 << 4 | 3,  // smn
            2676 << 4 | 6,  // smn-fi
            2682 << 4 | 3,  // sms
            2682 << 4 | 6,  // sms-fi
            2688 << 4 | 2,  // sn
            2688 << 4 | 7,  // sn-latn
            2688 << 4 | 10, // sn-latn-zw
            2698 << 4 | 2,  // so
            2698 << 4 | 5,  // so-dj
            2703 << 4 | 5,  // so-et
            2708 << 4 | 5,  // so-ke
            2713 << 4 | 5,  // so-so
            2718 << 4 | 2,  // sq
            2718 << 4 | 5,  // sq-al
            2723 << 4 | 5,  // sq-mk
            2728 << 4 | 5,  // sq-xk
            2733 << 4 | 2,  // sr
            2733 << 4 | 7,  // sr-cyrl
            2733 << 4 | 10, // sr-cyrl-ba
            2743 << 4 | 10, // sr-cyrl-cs
            2753 << 4 | 10, // sr-cyrl-me
            2763 << 4 | 10, // sr-cyrl-rs
            2773 << 4 | 10, // sr-cyrl-xk
            2783 << 4 | 7,  // sr-latn
            2783 << 4 | 10, // sr-latn-ba
            2793 << 4 | 10, // sr-latn-cs
            2803 << 4 | 10, // sr-latn-me
            2813 << 4 | 10, // sr-latn-rs
            2823 << 4 | 10, // sr-latn-xk
            2833 << 4 | 2,  // ss
            2833 << 4 | 5,  // ss-sz
            2838 << 4 | 5,  // ss-za
            2843 << 4 | 3,  // ssy
            2843 << 4 | 6,  // ssy-er
            2849 << 4 | 2,  // st
            2849 << 4 | 5,  // st-ls
            2854 << 4 | 5,  // st-za
            2859 << 4 | 2,  // sv
            2859 << 4 | 5,  // sv-ax
            2864 << 4 | 5,  // sv-fi
            2869 << 4 | 5,  // sv-se
            2874 << 4 | 2,  // sw
            2874 << 4 | 5,  // sw-cd
            2879 << 4 | 5,  // sw-ke
            2884 << 4 | 5,  // sw-tz
            2889 << 4 | 5,  // sw-ug
            2894 << 4 | 3,  // swc
            2894 << 4 | 6,  // swc-cd
            2900 << 4 | 3,  // syr
            2900 << 4 | 6,  // syr-sy
            2906 << 4 | 2,  // ta
            2906 << 4 | 5,  // ta-in
            2911 << 4 | 5,  // ta-lk
            2916 << 4 | 5,  // ta-my
            2921 << 4 | 5,  // ta-sg
            2926 << 4 | 2,  // te
            2926 << 4 | 5,  // te-in
            2931 << 4 | 3,  // teo
            2931 << 4 | 6,  // teo-ke
            2937 << 4 | 6,  // teo-ug
            2943 << 4 | 2,  // tg
            2943 << 4 | 7,  // tg-cyrl
            2943 << 4 | 10, // tg-cyrl-tj
            2953 << 4 | 2,  // th
            2953 << 4 | 5,  // th-th
            2958 << 4 | 2,  // ti
            2958 << 4 | 5,  // ti-er
            2963 << 4 | 5,  // ti-et
            2968 << 4 | 3,  // tig
            2968 << 4 | 6,  // tig-er
            2974 << 4 | 2,  // tk
            2974 << 4 | 5,  // tk-tm
            2979 << 4 | 2,  // tn
            2979 << 4 | 5,  // tn-bw
            2984 << 4 | 5,  // tn-za
            2989 << 4 | 2,  // to
            2989 << 4 | 5,  // to-to
            2994 << 4 | 2,  // tr
            2994 << 4 | 5,  // tr-cy
            2999 << 4 | 5,  // tr-tr
            3004 << 4 | 2,  // ts
            3004 << 4 | 5,  // ts-za
            3009 << 4 | 2,  // tt
            3009 << 4 | 5,  // tt-ru
            3014 << 4 | 3,  // twq
            3014 << 4 | 6,  // twq-ne
            3020 << 4 | 3,  // tzm
            3020 << 4 | 8,  // tzm-arab
            3020 << 4 | 11, // tzm-arab-ma
            3031 << 4 | 8,  // tzm-latn
            3031 << 4 | 11, // tzm-latn-dz
            3042 << 4 | 11, // tzm-latn-ma
            3053 << 4 | 8,  // tzm-tfng
            3053 << 4 | 11, // tzm-tfng-ma
            3064 << 4 | 2,  // ug
            3064 << 4 | 5,  // ug-cn
            3069 << 4 | 2,  // uk
            3069 << 4 | 5,  // uk-ua
            3074 << 4 | 2,  // ur
            3074 << 4 | 5,  // ur-in
            3079 << 4 | 5,  // ur-pk
            3084 << 4 | 2,  // uz
            3084 << 4 | 7,  // uz-arab
            3084 << 4 | 10, // uz-arab-af
            3094 << 4 | 7,  // uz-cyrl
            3094 << 4 | 10, // uz-cyrl-uz
            3104 << 4 | 7,  // uz-latn
            3104 << 4 | 10, // uz-latn-uz
            3114 << 4 | 3,  // vai
            3114 << 4 | 8,  // vai-latn
            3114 << 4 | 11, // vai-latn-lr
            3125 << 4 | 8,  // vai-vaii
            3125 << 4 | 11, // vai-vaii-lr
            3136 << 4 | 2,  // ve
            3136 << 4 | 5,  // ve-za
            3141 << 4 | 2,  // vi
            3141 << 4 | 5,  // vi-vn
            3146 << 4 | 2,  // vo
            3146 << 4 | 6,  // vo-001
            3152 << 4 | 3,  // vun
            3152 << 4 | 6,  // vun-tz
            3158 << 4 | 3,  // wae
            3158 << 4 | 6,  // wae-ch
            3164 << 4 | 3,  // wal
            3164 << 4 | 6,  // wal-et
            3170 << 4 | 2,  // wo
            3170 << 4 | 5,  // wo-sn
            3175 << 4 | 11, // x-iv_mathan
            3186 << 4 | 2,  // xh
            3186 << 4 | 5,  // xh-za
            3191 << 4 | 3,  // xog
            3191 << 4 | 6,  // xog-ug
            3197 << 4 | 3,  // yav
            3197 << 4 | 6,  // yav-cm
            3203 << 4 | 2,  // yi
            3203 << 4 | 6,  // yi-001
            3209 << 4 | 2,  // yo
            3209 << 4 | 5,  // yo-bj
            3214 << 4 | 5,  // yo-ng
            3219 << 4 | 3,  // yue
            3219 << 4 | 6,  // yue-hk
            3225 << 4 | 3,  // zgh
            3225 << 4 | 8,  // zgh-tfng
            3225 << 4 | 11, // zgh-tfng-ma
            3236 << 4 | 2,  // zh
            3236 << 4 | 6,  // zh-chs
            3242 << 4 | 6,  // zh-cht
            3248 << 4 | 5,  // zh-cn
            3248 << 4 | 12, // zh-cn_phoneb
            3260 << 4 | 12, // zh-cn_stroke
            3272 << 4 | 7,  // zh-hans
            3272 << 4 | 10, // zh-hans-hk
            3282 << 4 | 10, // zh-hans-mo
            3292 << 4 | 7,  // zh-hant
            3299 << 4 | 5,  // zh-hk
            3299 << 4 | 12, // zh-hk_radstr
            3311 << 4 | 5,  // zh-mo
            3311 << 4 | 12, // zh-mo_radstr
            3323 << 4 | 12, // zh-mo_stroke
            3335 << 4 | 5,  // zh-sg
            3335 << 4 | 12, // zh-sg_phoneb
            3347 << 4 | 12, // zh-sg_stroke
            3359 << 4 | 5,  // zh-tw
            3359 << 4 | 12, // zh-tw_pronun
            3371 << 4 | 12, // zh-tw_radstr
            3383 << 4 | 2,  // zu
            3383 << 4 | 5,  // zu-za
        };

        // c_threeLetterWindowsLanguageName is string containing 3-letter Windows language names
        // every 3-characters entry is matching locale name entry in c_localeNames

        private const string c_threeLetterWindowsLanguageName =
            "ZZZ" + // aa
            "ZZZ" + // aa-dj
            "ZZZ" + // aa-er
            "ZZZ" + // aa-et
            "AFK" + // af
            "ZZZ" + // af-na
            "AFK" + // af-za
            "ZZZ" + // agq
            "ZZZ" + // agq-cm
            "ZZZ" + // ak
            "ZZZ" + // ak-gh
            "AMH" + // am
            "AMH" + // am-et
            "ARA" + // ar
            "ZZZ" + // ar-001
            "ARU" + // ar-ae
            "ARH" + // ar-bh
            "ZZZ" + // ar-dj
            "ARG" + // ar-dz
            "ARE" + // ar-eg
            "ZZZ" + // ar-er
            "ZZZ" + // ar-il
            "ARI" + // ar-iq
            "ARJ" + // ar-jo
            "ZZZ" + // ar-km
            "ARK" + // ar-kw
            "ARB" + // ar-lb
            "ARL" + // ar-ly
            "ARM" + // ar-ma
            "ZZZ" + // ar-mr
            "ARO" + // ar-om
            "ZZZ" + // ar-ps
            "ARQ" + // ar-qa
            "ARA" + // ar-sa
            "ZZZ" + // ar-sd
            "ZZZ" + // ar-so
            "ZZZ" + // ar-ss
            "ARS" + // ar-sy
            "ZZZ" + // ar-td
            "ART" + // ar-tn
            "ARY" + // ar-ye
            "MPD" + // arn
            "MPD" + // arn-cl
            "ASM" + // as
            "ASM" + // as-in
            "ZZZ" + // asa
            "ZZZ" + // asa-tz
            "ZZZ" + // ast
            "ZZZ" + // ast-es
            "AZE" + // az
            "AZC" + // az-cyrl
            "AZC" + // az-cyrl-az
            "AZE" + // az-latn
            "AZE" + // az-latn-az
            "BAS" + // ba
            "BAS" + // ba-ru
            "ZZZ" + // bas
            "ZZZ" + // bas-cm
            "BEL" + // be
            "BEL" + // be-by
            "ZZZ" + // bem
            "ZZZ" + // bem-zm
            "ZZZ" + // bez
            "ZZZ" + // bez-tz
            "BGR" + // bg
            "BGR" + // bg-bg
            "ZZZ" + // bin
            "ZZZ" + // bin-ng
            "ZZZ" + // bm
            "ZZZ" + // bm-latn
            "ZZZ" + // bm-latn-ml
            "BNB" + // bn
            "BNB" + // bn-bd
            "BNG" + // bn-in
            "BOB" + // bo
            "BOB" + // bo-cn
            "ZZZ" + // bo-in
            "BRE" + // br
            "BRE" + // br-fr
            "ZZZ" + // brx
            "ZZZ" + // brx-in
            "BSB" + // bs
            "BSC" + // bs-cyrl
            "BSC" + // bs-cyrl-ba
            "BSB" + // bs-latn
            "BSB" + // bs-latn-ba
            "ZZZ" + // byn
            "ZZZ" + // byn-er
            "CAT" + // ca
            "ZZZ" + // ca-ad
            "CAT" + // ca-es
            "VAL" + // ca-es-valencia
            "ZZZ" + // ca-fr
            "ZZZ" + // ca-it
            "ZZZ" + // ce
            "ZZZ" + // ce-ru
            "ZZZ" + // cgg
            "ZZZ" + // cgg-ug
            "CRE" + // chr
            "CRE" + // chr-cher
            "CRE" + // chr-cher-us
            "COS" + // co
            "COS" + // co-fr
            "CSY" + // cs
            "CSY" + // cs-cz
            "ZZZ" + // cu
            "ZZZ" + // cu-ru
            "CYM" + // cy
            "CYM" + // cy-gb
            "DAN" + // da
            "DAN" + // da-dk
            "ZZZ" + // da-gl
            "ZZZ" + // dav
            "ZZZ" + // dav-ke
            "DEU" + // de
            "DEA" + // de-at
            "ZZZ" + // de-be
            "DES" + // de-ch
            "DEU" + // de-de
            "DEU" + // de-de_phoneb
            "ZZZ" + // de-it
            "DEC" + // de-li
            "DEL" + // de-lu
            "ZZZ" + // dje
            "ZZZ" + // dje-ne
            "DSB" + // dsb
            "DSB" + // dsb-de
            "ZZZ" + // dua
            "ZZZ" + // dua-cm
            "DIV" + // dv
            "DIV" + // dv-mv
            "ZZZ" + // dyo
            "ZZZ" + // dyo-sn
            "ZZZ" + // dz
            "ZZZ" + // dz-bt
            "ZZZ" + // ebu
            "ZZZ" + // ebu-ke
            "ZZZ" + // ee
            "ZZZ" + // ee-gh
            "ZZZ" + // ee-tg
            "ELL" + // el
            "ZZZ" + // el-cy
            "ELL" + // el-gr
            "ENU" + // en
            "ZZZ" + // en-001
            "ENB" + // en-029
            "ZZZ" + // en-150
            "ZZZ" + // en-ag
            "ZZZ" + // en-ai
            "ZZZ" + // en-as
            "ZZZ" + // en-at
            "ENA" + // en-au
            "ZZZ" + // en-bb
            "ZZZ" + // en-be
            "ZZZ" + // en-bi
            "ZZZ" + // en-bm
            "ZZZ" + // en-bs
            "ZZZ" + // en-bw
            "ENL" + // en-bz
            "ENC" + // en-ca
            "ZZZ" + // en-cc
            "ZZZ" + // en-ch
            "ZZZ" + // en-ck
            "ZZZ" + // en-cm
            "ZZZ" + // en-cx
            "ZZZ" + // en-cy
            "ZZZ" + // en-de
            "ZZZ" + // en-dk
            "ZZZ" + // en-dm
            "ZZZ" + // en-er
            "ZZZ" + // en-fi
            "ZZZ" + // en-fj
            "ZZZ" + // en-fk
            "ZZZ" + // en-fm
            "ENG" + // en-gb
            "ZZZ" + // en-gd
            "ZZZ" + // en-gg
            "ZZZ" + // en-gh
            "ZZZ" + // en-gi
            "ZZZ" + // en-gm
            "ZZZ" + // en-gu
            "ZZZ" + // en-gy
            "ENH" + // en-hk
            "ZZZ" + // en-id
            "ENI" + // en-ie
            "ZZZ" + // en-il
            "ZZZ" + // en-im
            "ENN" + // en-in
            "ZZZ" + // en-io
            "ZZZ" + // en-je
            "ENJ" + // en-jm
            "ZZZ" + // en-ke
            "ZZZ" + // en-ki
            "ZZZ" + // en-kn
            "ZZZ" + // en-ky
            "ZZZ" + // en-lc
            "ZZZ" + // en-lr
            "ZZZ" + // en-ls
            "ZZZ" + // en-mg
            "ZZZ" + // en-mh
            "ZZZ" + // en-mo
            "ZZZ" + // en-mp
            "ZZZ" + // en-ms
            "ZZZ" + // en-mt
            "ZZZ" + // en-mu
            "ZZZ" + // en-mw
            "ENM" + // en-my
            "ZZZ" + // en-na
            "ZZZ" + // en-nf
            "ZZZ" + // en-ng
            "ZZZ" + // en-nl
            "ZZZ" + // en-nr
            "ZZZ" + // en-nu
            "ENZ" + // en-nz
            "ZZZ" + // en-pg
            "ENP" + // en-ph
            "ZZZ" + // en-pk
            "ZZZ" + // en-pn
            "ZZZ" + // en-pr
            "ZZZ" + // en-pw
            "ZZZ" + // en-rw
            "ZZZ" + // en-sb
            "ZZZ" + // en-sc
            "ZZZ" + // en-sd
            "ZZZ" + // en-se
            "ENE" + // en-sg
            "ZZZ" + // en-sh
            "ZZZ" + // en-si
            "ZZZ" + // en-sl
            "ZZZ" + // en-ss
            "ZZZ" + // en-sx
            "ZZZ" + // en-sz
            "ZZZ" + // en-tc
            "ZZZ" + // en-tk
            "ZZZ" + // en-to
            "ENT" + // en-tt
            "ZZZ" + // en-tv
            "ZZZ" + // en-tz
            "ZZZ" + // en-ug
            "ZZZ" + // en-um
            "ENU" + // en-us
            "ZZZ" + // en-vc
            "ZZZ" + // en-vg
            "ZZZ" + // en-vi
            "ZZZ" + // en-vu
            "ZZZ" + // en-ws
            "ENS" + // en-za
            "ZZZ" + // en-zm
            "ENW" + // en-zw
            "ZZZ" + // eo
            "ZZZ" + // eo-001
            "ESN" + // es
            "ESJ" + // es-419
            "ESS" + // es-ar
            "ESB" + // es-bo
            "ZZZ" + // es-br
            "ESL" + // es-cl
            "ESO" + // es-co
            "ESC" + // es-cr
            "ESK" + // es-cu
            "ESD" + // es-do
            "ESF" + // es-ec
            "ESN" + // es-es
            "ESP" + // es-es_tradnl
            "ZZZ" + // es-gq
            "ESG" + // es-gt
            "ESH" + // es-hn
            "ESM" + // es-mx
            "ESI" + // es-ni
            "ESA" + // es-pa
            "ESR" + // es-pe
            "ZZZ" + // es-ph
            "ESU" + // es-pr
            "ESZ" + // es-py
            "ESE" + // es-sv
            "EST" + // es-us
            "ESY" + // es-uy
            "ESV" + // es-ve
            "ETI" + // et
            "ETI" + // et-ee
            "EUQ" + // eu
            "EUQ" + // eu-es
            "ZZZ" + // ewo
            "ZZZ" + // ewo-cm
            "FAR" + // fa
            "FAR" + // fa-ir
            "FUL" + // ff
            "ZZZ" + // ff-cm
            "ZZZ" + // ff-gn
            "FUL" + // ff-latn
            "FUL" + // ff-latn-sn
            "ZZZ" + // ff-mr
            "ZZZ" + // ff-ng
            "FIN" + // fi
            "FIN" + // fi-fi
            "FPO" + // fil
            "FPO" + // fil-ph
            "FOS" + // fo
            "ZZZ" + // fo-dk
            "FOS" + // fo-fo
            "FRA" + // fr
            "ZZZ" + // fr-029
            "FRB" + // fr-be
            "ZZZ" + // fr-bf
            "ZZZ" + // fr-bi
            "ZZZ" + // fr-bj
            "ZZZ" + // fr-bl
            "FRC" + // fr-ca
            "FRD" + // fr-cd
            "ZZZ" + // fr-cf
            "ZZZ" + // fr-cg
            "FRS" + // fr-ch
            "FRI" + // fr-ci
            "FRE" + // fr-cm
            "ZZZ" + // fr-dj
            "ZZZ" + // fr-dz
            "FRA" + // fr-fr
            "ZZZ" + // fr-ga
            "ZZZ" + // fr-gf
            "ZZZ" + // fr-gn
            "ZZZ" + // fr-gp
            "ZZZ" + // fr-gq
            "FRH" + // fr-ht
            "ZZZ" + // fr-km
            "FRL" + // fr-lu
            "FRO" + // fr-ma
            "FRM" + // fr-mc
            "ZZZ" + // fr-mf
            "ZZZ" + // fr-mg
            "FRF" + // fr-ml
            "ZZZ" + // fr-mq
            "ZZZ" + // fr-mr
            "ZZZ" + // fr-mu
            "ZZZ" + // fr-nc
            "ZZZ" + // fr-ne
            "ZZZ" + // fr-pf
            "ZZZ" + // fr-pm
            "FRR" + // fr-re
            "ZZZ" + // fr-rw
            "ZZZ" + // fr-sc
            "FRN" + // fr-sn
            "ZZZ" + // fr-sy
            "ZZZ" + // fr-td
            "ZZZ" + // fr-tg
            "ZZZ" + // fr-tn
            "ZZZ" + // fr-vu
            "ZZZ" + // fr-wf
            "ZZZ" + // fr-yt
            "ZZZ" + // fur
            "ZZZ" + // fur-it
            "FYN" + // fy
            "FYN" + // fy-nl
            "IRE" + // ga
            "IRE" + // ga-ie
            "GLA" + // gd
            "GLA" + // gd-gb
            "GLC" + // gl
            "GLC" + // gl-es
            "GRN" + // gn
            "GRN" + // gn-py
            "ZZZ" + // gsw
            "ZZZ" + // gsw-ch
            "GSW" + // gsw-fr
            "ZZZ" + // gsw-li
            "GUJ" + // gu
            "GUJ" + // gu-in
            "ZZZ" + // guz
            "ZZZ" + // guz-ke
            "ZZZ" + // gv
            "ZZZ" + // gv-im
            "HAU" + // ha
            "HAU" + // ha-latn
            "ZZZ" + // ha-latn-gh
            "ZZZ" + // ha-latn-ne
            "HAU" + // ha-latn-ng
            "HAW" + // haw
            "HAW" + // haw-us
            "HEB" + // he
            "HEB" + // he-il
            "HIN" + // hi
            "HIN" + // hi-in
            "HRV" + // hr
            "HRB" + // hr-ba
            "HRV" + // hr-hr
            "HSB" + // hsb
            "HSB" + // hsb-de
            "HUN" + // hu
            "HUN" + // hu-hu
            "HUN" + // hu-hu_technl
            "HYE" + // hy
            "HYE" + // hy-am
            "ZZZ" + // ia
            "ZZZ" + // ia-001
            "ZZZ" + // ia-fr
            "ZZZ" + // ibb
            "ZZZ" + // ibb-ng
            "IND" + // id
            "IND" + // id-id
            "IBO" + // ig
            "IBO" + // ig-ng
            "III" + // ii
            "III" + // ii-cn
            "ISL" + // is
            "ISL" + // is-is
            "ITA" + // it
            "ITS" + // it-ch
            "ITA" + // it-it
            "ZZZ" + // it-sm
            "IUK" + // iu
            "IUS" + // iu-cans
            "IUS" + // iu-cans-ca
            "IUK" + // iu-latn
            "IUK" + // iu-latn-ca
            "JPN" + // ja
            "JPN" + // ja-jp
            "JPN" + // ja-jp_radstr
            "ZZZ" + // jgo
            "ZZZ" + // jgo-cm
            "ZZZ" + // jmc
            "ZZZ" + // jmc-tz
            "JAV" + // jv
            "ZZZ" + // jv-java
            "ZZZ" + // jv-java-id
            "JAV" + // jv-latn
            "JAV" + // jv-latn-id
            "KAT" + // ka
            "KAT" + // ka-ge
            "KAT" + // ka-ge_modern
            "ZZZ" + // kab
            "ZZZ" + // kab-dz
            "ZZZ" + // kam
            "ZZZ" + // kam-ke
            "ZZZ" + // kde
            "ZZZ" + // kde-tz
            "ZZZ" + // kea
            "ZZZ" + // kea-cv
            "ZZZ" + // khq
            "ZZZ" + // khq-ml
            "ZZZ" + // ki
            "ZZZ" + // ki-ke
            "KKZ" + // kk
            "KKZ" + // kk-kz
            "ZZZ" + // kkj
            "ZZZ" + // kkj-cm
            "KAL" + // kl
            "KAL" + // kl-gl
            "ZZZ" + // kln
            "ZZZ" + // kln-ke
            "KHM" + // km
            "KHM" + // km-kh
            "KDI" + // kn
            "KDI" + // kn-in
            "KOR" + // ko
            "ZZZ" + // ko-kp
            "KOR" + // ko-kr
            "KNK" + // kok
            "KNK" + // kok-in
            "ZZZ" + // kr
            "ZZZ" + // kr-ng
            "ZZZ" + // ks
            "ZZZ" + // ks-arab
            "ZZZ" + // ks-arab-in
            "ZZZ" + // ks-deva
            "ZZZ" + // ks-deva-in
            "ZZZ" + // ksb
            "ZZZ" + // ksb-tz
            "ZZZ" + // ksf
            "ZZZ" + // ksf-cm
            "ZZZ" + // ksh
            "ZZZ" + // ksh-de
            "KUR" + // ku
            "KUR" + // ku-arab
            "KUR" + // ku-arab-iq
            "ZZZ" + // ku-arab-ir
            "ZZZ" + // kw
            "ZZZ" + // kw-gb
            "KYR" + // ky
            "KYR" + // ky-kg
            "ZZZ" + // la
            "ZZZ" + // la-001
            "ZZZ" + // lag
            "ZZZ" + // lag-tz
            "LBX" + // lb
            "LBX" + // lb-lu
            "ZZZ" + // lg
            "ZZZ" + // lg-ug
            "ZZZ" + // lkt
            "ZZZ" + // lkt-us
            "ZZZ" + // ln
            "ZZZ" + // ln-ao
            "ZZZ" + // ln-cd
            "ZZZ" + // ln-cf
            "ZZZ" + // ln-cg
            "LAO" + // lo
            "LAO" + // lo-la
            "ZZZ" + // lrc
            "ZZZ" + // lrc-iq
            "ZZZ" + // lrc-ir
            "LTH" + // lt
            "LTH" + // lt-lt
            "ZZZ" + // lu
            "ZZZ" + // lu-cd
            "ZZZ" + // luo
            "ZZZ" + // luo-ke
            "ZZZ" + // luy
            "ZZZ" + // luy-ke
            "LVI" + // lv
            "LVI" + // lv-lv
            "ZZZ" + // mas
            "ZZZ" + // mas-ke
            "ZZZ" + // mas-tz
            "ZZZ" + // mer
            "ZZZ" + // mer-ke
            "ZZZ" + // mfe
            "ZZZ" + // mfe-mu
            "MLG" + // mg
            "MLG" + // mg-mg
            "ZZZ" + // mgh
            "ZZZ" + // mgh-mz
            "ZZZ" + // mgo
            "ZZZ" + // mgo-cm
            "MRI" + // mi
            "MRI" + // mi-nz
            "MKI" + // mk
            "MKI" + // mk-mk
            "MYM" + // ml
            "MYM" + // ml-in
            "MNN" + // mn
            "MNN" + // mn-cyrl
            "MNN" + // mn-mn
            "MNG" + // mn-mong
            "MNG" + // mn-mong-cn
            "MNM" + // mn-mong-mn
            "ZZZ" + // mni
            "ZZZ" + // mni-in
            "MWK" + // moh
            "MWK" + // moh-ca
            "MAR" + // mr
            "MAR" + // mr-in
            "MSL" + // ms
            "MSB" + // ms-bn
            "MSL" + // ms-my
            "ZZZ" + // ms-sg
            "MLT" + // mt
            "MLT" + // mt-mt
            "ZZZ" + // mua
            "ZZZ" + // mua-cm
            "MYA" + // my
            "MYA" + // my-mm
            "ZZZ" + // mzn
            "ZZZ" + // mzn-ir
            "ZZZ" + // naq
            "ZZZ" + // naq-na
            "NOR" + // nb
            "NOR" + // nb-no
            "ZZZ" + // nb-sj
            "ZZZ" + // nd
            "ZZZ" + // nd-zw
            "ZZZ" + // nds
            "ZZZ" + // nds-de
            "ZZZ" + // nds-nl
            "NEP" + // ne
            "NEI" + // ne-in
            "NEP" + // ne-np
            "NLD" + // nl
            "ZZZ" + // nl-aw
            "NLB" + // nl-be
            "ZZZ" + // nl-bq
            "ZZZ" + // nl-cw
            "NLD" + // nl-nl
            "ZZZ" + // nl-sr
            "ZZZ" + // nl-sx
            "ZZZ" + // nmg
            "ZZZ" + // nmg-cm
            "NON" + // nn
            "NON" + // nn-no
            "ZZZ" + // nnh
            "ZZZ" + // nnh-cm
            "NOR" + // no
            "NQO" + // nqo
            "NQO" + // nqo-gn
            "ZZZ" + // nr
            "ZZZ" + // nr-za
            "NSO" + // nso
            "NSO" + // nso-za
            "ZZZ" + // nus
            "ZZZ" + // nus-ss
            "ZZZ" + // nyn
            "ZZZ" + // nyn-ug
            "OCI" + // oc
            "OCI" + // oc-fr
            "ORM" + // om
            "ORM" + // om-et
            "ZZZ" + // om-ke
            "ORI" + // or
            "ORI" + // or-in
            "ZZZ" + // os
            "ZZZ" + // os-ge
            "ZZZ" + // os-ru
            "PAN" + // pa
            "PAP" + // pa-arab
            "PAP" + // pa-arab-pk
            "PAN" + // pa-in
            "ZZZ" + // pap
            "ZZZ" + // pap-029
            "PLK" + // pl
            "PLK" + // pl-pl
            "ZZZ" + // prg
            "ZZZ" + // prg-001
            "PRS" + // prs
            "PRS" + // prs-af
            "PAS" + // ps
            "PAS" + // ps-af
            "PTB" + // pt
            "PTA" + // pt-ao
            "PTB" + // pt-br
            "ZZZ" + // pt-ch
            "ZZZ" + // pt-cv
            "ZZZ" + // pt-gq
            "ZZZ" + // pt-gw
            "ZZZ" + // pt-lu
            "ZZZ" + // pt-mo
            "ZZZ" + // pt-mz
            "PTG" + // pt-pt
            "ZZZ" + // pt-st
            "ZZZ" + // pt-tl
            "ENJ" + // qps-latn-x-sh
            "ENU" + // qps-ploc
            "JPN" + // qps-ploca
            "ARA" + // qps-plocm
            "QUT" + // quc
            "QUT" + // quc-latn
            "QUT" + // quc-latn-gt
            "QUB" + // quz
            "QUB" + // quz-bo
            "QUE" + // quz-ec
            "QUP" + // quz-pe
            "RMC" + // rm
            "RMC" + // rm-ch
            "ZZZ" + // rn
            "ZZZ" + // rn-bi
            "ROM" + // ro
            "ROD" + // ro-md
            "ROM" + // ro-ro
            "ZZZ" + // rof
            "ZZZ" + // rof-tz
            "RUS" + // ru
            "ZZZ" + // ru-by
            "ZZZ" + // ru-kg
            "ZZZ" + // ru-kz
            "RUM" + // ru-md
            "RUS" + // ru-ru
            "ZZZ" + // ru-ua
            "KIN" + // rw
            "KIN" + // rw-rw
            "ZZZ" + // rwk
            "ZZZ" + // rwk-tz
            "SAN" + // sa
            "SAN" + // sa-in
            "SAH" + // sah
            "SAH" + // sah-ru
            "ZZZ" + // saq
            "ZZZ" + // saq-ke
            "ZZZ" + // sbp
            "ZZZ" + // sbp-tz
            "SIP" + // sd
            "SIP" + // sd-arab
            "SIP" + // sd-arab-pk
            "ZZZ" + // sd-deva
            "ZZZ" + // sd-deva-in
            "SME" + // se
            "SMG" + // se-fi
            "SME" + // se-no
            "SMF" + // se-se
            "ZZZ" + // seh
            "ZZZ" + // seh-mz
            "ZZZ" + // ses
            "ZZZ" + // ses-ml
            "ZZZ" + // sg
            "ZZZ" + // sg-cf
            "ZZZ" + // shi
            "ZZZ" + // shi-latn
            "ZZZ" + // shi-latn-ma
            "ZZZ" + // shi-tfng
            "ZZZ" + // shi-tfng-ma
            "SIN" + // si
            "SIN" + // si-lk
            "SKY" + // sk
            "SKY" + // sk-sk
            "SLV" + // sl
            "SLV" + // sl-si
            "SMB" + // sma
            "SMA" + // sma-no
            "SMB" + // sma-se
            "SMK" + // smj
            "SMJ" + // smj-no
            "SMK" + // smj-se
            "SMN" + // smn
            "SMN" + // smn-fi
            "SMS" + // sms
            "SMS" + // sms-fi
            "SNA" + // sn
            "SNA" + // sn-latn
            "SNA" + // sn-latn-zw
            "SOM" + // so
            "ZZZ" + // so-dj
            "ZZZ" + // so-et
            "ZZZ" + // so-ke
            "SOM" + // so-so
            "SQI" + // sq
            "SQI" + // sq-al
            "ZZZ" + // sq-mk
            "ZZZ" + // sq-xk
            "SRM" + // sr
            "SRO" + // sr-cyrl
            "SRN" + // sr-cyrl-ba
            "SRB" + // sr-cyrl-cs
            "SRQ" + // sr-cyrl-me
            "SRO" + // sr-cyrl-rs
            "ZZZ" + // sr-cyrl-xk
            "SRM" + // sr-latn
            "SRS" + // sr-latn-ba
            "SRL" + // sr-latn-cs
            "SRP" + // sr-latn-me
            "SRM" + // sr-latn-rs
            "ZZZ" + // sr-latn-xk
            "ZZZ" + // ss
            "ZZZ" + // ss-sz
            "ZZZ" + // ss-za
            "ZZZ" + // ssy
            "ZZZ" + // ssy-er
            "SOT" + // st
            "ZZZ" + // st-ls
            "SOT" + // st-za
            "SVE" + // sv
            "ZZZ" + // sv-ax
            "SVF" + // sv-fi
            "SVE" + // sv-se
            "SWK" + // sw
            "ZZZ" + // sw-cd
            "SWK" + // sw-ke
            "ZZZ" + // sw-tz
            "ZZZ" + // sw-ug
            "ZZZ" + // swc
            "ZZZ" + // swc-cd
            "SYR" + // syr
            "SYR" + // syr-sy
            "TAI" + // ta
            "TAI" + // ta-in
            "TAM" + // ta-lk
            "ZZZ" + // ta-my
            "ZZZ" + // ta-sg
            "TEL" + // te
            "TEL" + // te-in
            "ZZZ" + // teo
            "ZZZ" + // teo-ke
            "ZZZ" + // teo-ug
            "TAJ" + // tg
            "TAJ" + // tg-cyrl
            "TAJ" + // tg-cyrl-tj
            "THA" + // th
            "THA" + // th-th
            "TIR" + // ti
            "TIR" + // ti-er
            "TIE" + // ti-et
            "ZZZ" + // tig
            "ZZZ" + // tig-er
            "TUK" + // tk
            "TUK" + // tk-tm
            "TSN" + // tn
            "TSB" + // tn-bw
            "TSN" + // tn-za
            "ZZZ" + // to
            "ZZZ" + // to-to
            "TRK" + // tr
            "ZZZ" + // tr-cy
            "TRK" + // tr-tr
            "TSO" + // ts
            "TSO" + // ts-za
            "TTT" + // tt
            "TTT" + // tt-ru
            "ZZZ" + // twq
            "ZZZ" + // twq-ne
            "TZA" + // tzm
            "ZZZ" + // tzm-arab
            "ZZZ" + // tzm-arab-ma
            "TZA" + // tzm-latn
            "TZA" + // tzm-latn-dz
            "ZZZ" + // tzm-latn-ma
            "TZM" + // tzm-tfng
            "TZM" + // tzm-tfng-ma
            "UIG" + // ug
            "UIG" + // ug-cn
            "UKR" + // uk
            "UKR" + // uk-ua
            "URD" + // ur
            "URI" + // ur-in
            "URD" + // ur-pk
            "UZB" + // uz
            "ZZZ" + // uz-arab
            "ZZZ" + // uz-arab-af
            "UZC" + // uz-cyrl
            "UZC" + // uz-cyrl-uz
            "UZB" + // uz-latn
            "UZB" + // uz-latn-uz
            "ZZZ" + // vai
            "ZZZ" + // vai-latn
            "ZZZ" + // vai-latn-lr
            "ZZZ" + // vai-vaii
            "ZZZ" + // vai-vaii-lr
            "ZZZ" + // ve
            "ZZZ" + // ve-za
            "VIT" + // vi
            "VIT" + // vi-vn
            "ZZZ" + // vo
            "ZZZ" + // vo-001
            "ZZZ" + // vun
            "ZZZ" + // vun-tz
            "ZZZ" + // wae
            "ZZZ" + // wae-ch
            "ZZZ" + // wal
            "ZZZ" + // wal-et
            "WOL" + // wo
            "WOL" + // wo-sn
            "IVL" + // x-iv_mathan
            "XHO" + // xh
            "XHO" + // xh-za
            "ZZZ" + // xog
            "ZZZ" + // xog-ug
            "ZZZ" + // yav
            "ZZZ" + // yav-cm
            "ZZZ" + // yi
            "ZZZ" + // yi-001
            "YOR" + // yo
            "ZZZ" + // yo-bj
            "YOR" + // yo-ng
            "ZZZ" + // yue
            "ZZZ" + // yue-hk
            "ZHG" + // zgh
            "ZHG" + // zgh-tfng
            "ZHG" + // zgh-tfng-ma
            "CHS" + // zh
            "CHS" + // zh-chs
            "CHT" + // zh-cht
            "CHS" + // zh-cn
            "CHS" + // zh-cn_phoneb
            "CHS" + // zh-cn_stroke
            "CHS" + // zh-hans
            "ZZZ" + // zh-hans-hk
            "ZZZ" + // zh-hans-mo
            "ZHH" + // zh-hant
            "ZHH" + // zh-hk
            "ZHH" + // zh-hk_radstr
            "ZHM" + // zh-mo
            "ZHM" + // zh-mo_radstr
            "ZHM" + // zh-mo_stroke
            "ZHI" + // zh-sg
            "ZHI" + // zh-sg_phoneb
            "ZHI" + // zh-sg_stroke
            "CHT" + // zh-tw
            "CHT" + // zh-tw_pronun
            "CHT" + // zh-tw_radstr
            "ZUL" + // zu
            "ZUL";  // zu-za

        private const int NUMERIC_LOCALE_DATA_COUNT_PER_ROW = 9;

        internal const int CommaSep              = 0 << 16;
        internal const int SemicolonSep          = 1 << 16;
        internal const int ArabicCommaSep        = 2 << 16;
        internal const int ArabicSemicolonSep    = 3 << 16;
        internal const int DoubleCommaSep        = 4 << 16;

        // s_nameIndexToNumericData is mapping from index in s_localeNamesIndices to locale data.
        // each row in the table will have the following data:
        //      Lcid, Ansi codepage, Oem codepage, MAC codepage, EBCDIC codepage, Geo Id, Digit Substitution | ListSeparator, specific locale index, Console locale index
        private static readonly int[] s_nameIndexToNumericData = new int[CulturesCount * NUMERIC_LOCALE_DATA_COUNT_PER_ROW]
        {
         // Lcid,  Ansi CP, Oem CP, MAC CP, EBCDIC CP, Geo Id, digit substitution | ListSeparator, Specific culture index, Console locale index  // index - locale name
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x49  , 1 | SemicolonSep      , 3   , 240 , // 0    - aa
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x3e  , 1 | SemicolonSep      , 1   , 240 , // 1    - aa-dj
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x47  , 1 | SemicolonSep      , 2   , 240 , // 2    - aa-er
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x49  , 1 | SemicolonSep      , 3   , 240 , // 3    - aa-et
            0x36   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd1  , 1 | SemicolonSep      , 6   , 6   , // 4    - af
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xfe  , 1 | SemicolonSep      , 5   , 240 , // 5    - af-na
            0x436  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd1  , 1 | SemicolonSep      , 6   , 6   , // 6    - af-za
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 8   , 240 , // 7    - agq
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 8   , 240 , // 8    - agq-cm
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x59  , 1 | SemicolonSep      , 10  , 240 , // 9    - ak
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x59  , 1 | SemicolonSep      , 10  , 240 , // 10   - ak-gh
            0x5e   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x49  , 1 | SemicolonSep      , 12  , 143 , // 11   - am
            0x45e  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x49  , 1 | SemicolonSep      , 12  , 143 , // 12   - am-et
            0x1    , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xcd  , 0 | SemicolonSep      , 33  , 143 , // 13   - ar
            0x1000 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x989e, 0 | SemicolonSep      , 14  , 240 , // 14   - ar-001
            0x3801 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xe0  , 0 | SemicolonSep      , 15  , 143 , // 15   - ar-ae
            0x3c01 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x11  , 0 | SemicolonSep      , 16  , 143 , // 16   - ar-bh
            0x1000 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x3e  , 0 | SemicolonSep      , 17  , 240 , // 17   - ar-dj
            0x1401 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x4   , 1 | SemicolonSep      , 18  , 300 , // 18   - ar-dz
            0xc01  , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x43  , 0 | SemicolonSep      , 19  , 143 , // 19   - ar-eg
            0x1000 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x47  , 0 | SemicolonSep      , 20  , 240 , // 20   - ar-er
            0x1000 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x75  , 0 | SemicolonSep      , 21  , 240 , // 21   - ar-il
            0x801  , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x79  , 0 | SemicolonSep      , 22  , 143 , // 22   - ar-iq
            0x2c01 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x7e  , 0 | SemicolonSep      , 23  , 143 , // 23   - ar-jo
            0x1000 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x32  , 0 | SemicolonSep      , 24  , 240 , // 24   - ar-km
            0x3401 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x88  , 0 | SemicolonSep      , 25  , 143 , // 25   - ar-kw
            0x3001 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x8b  , 0 | SemicolonSep      , 26  , 143 , // 26   - ar-lb
            0x1001 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x94  , 1 | SemicolonSep      , 27  , 143 , // 27   - ar-ly
            0x1801 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x9f  , 1 | SemicolonSep      , 28  , 300 , // 28   - ar-ma
            0x1000 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xa2  , 0 | SemicolonSep      , 29  , 240 , // 29   - ar-mr
            0x2001 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xa4  , 0 | SemicolonSep      , 30  , 143 , // 30   - ar-om
            0x1000 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xb8  , 0 | SemicolonSep      , 31  , 240 , // 31   - ar-ps
            0x4001 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xc5  , 0 | SemicolonSep      , 32  , 143 , // 32   - ar-qa
            0x401  , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xcd  , 0 | SemicolonSep      , 33  , 143 , // 33   - ar-sa
            0x1000 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xdb  , 0 | SemicolonSep      , 34  , 240 , // 34   - ar-sd
            0x1000 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xd8  , 0 | SemicolonSep      , 35  , 240 , // 35   - ar-so
            0x1000 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x114 , 0 | SemicolonSep      , 36  , 240 , // 36   - ar-ss
            0x2801 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xde  , 0 | SemicolonSep      , 37  , 143 , // 37   - ar-sy
            0x1000 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x29  , 0 | SemicolonSep      , 38  , 240 , // 38   - ar-td
            0x1c01 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xea  , 1 | SemicolonSep      , 39  , 300 , // 39   - ar-tn
            0x2401 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x105 , 0 | SemicolonSep      , 40  , 143 , // 40   - ar-ye
            0x7a   , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x2e  , 1 | CommaSep          , 42  , 42  , // 41   - arn
            0x47a  , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x2e  , 1 | CommaSep          , 42  , 42  , // 42   - arn-cl
            0x4d   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 44  , 143 , // 43   - as
            0x44d  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 44  , 143 , // 44   - as-in
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 46  , 240 , // 45   - asa
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 46  , 240 , // 46   - asa-tz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd9  , 1 | SemicolonSep      , 48  , 240 , // 47   - ast
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd9  , 1 | SemicolonSep      , 48  , 240 , // 48   - ast-es
            0x2c   , 0x4e6 , 0x359 , 0x2761, 0x51a9, 0x5   , 1 | SemicolonSep      , 53  , 53  , // 49   - az
            0x742c , 0x4e3 , 0x362 , 0x2717, 0x5190, 0x5   , 1 | SemicolonSep      , 51  , 51  , // 50   - az-cyrl
            0x82c  , 0x4e3 , 0x362 , 0x2717, 0x5190, 0x5   , 1 | SemicolonSep      , 51  , 51  , // 51   - az-cyrl-az
            0x782c , 0x4e6 , 0x359 , 0x2761, 0x51a9, 0x5   , 1 | SemicolonSep      , 53  , 53  , // 52   - az-latn
            0x42c  , 0x4e6 , 0x359 , 0x2761, 0x51a9, 0x5   , 1 | SemicolonSep      , 53  , 53  , // 53   - az-latn-az
            0x6d   , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xcb  , 1 | SemicolonSep      , 55  , 55  , // 54   - ba
            0x46d  , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xcb  , 1 | SemicolonSep      , 55  , 55  , // 55   - ba-ru
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 57  , 240 , // 56   - bas
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 57  , 240 , // 57   - bas-cm
            0x23   , 0x4e3 , 0x362 , 0x2717, 0x1f4 , 0x1d  , 1 | SemicolonSep      , 59  , 59  , // 58   - be
            0x423  , 0x4e3 , 0x362 , 0x2717, 0x1f4 , 0x1d  , 1 | SemicolonSep      , 59  , 59  , // 59   - be-by
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x107 , 1 | SemicolonSep      , 61  , 240 , // 60   - bem
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x107 , 1 | SemicolonSep      , 61  , 240 , // 61   - bem-zm
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 63  , 240 , // 62   - bez
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 63  , 240 , // 63   - bez-tz
            0x2    , 0x4e3 , 0x362 , 0x2717, 0x5221, 0x23  , 1 | SemicolonSep      , 65  , 65  , // 64   - bg
            0x402  , 0x4e3 , 0x362 , 0x2717, 0x5221, 0x23  , 1 | SemicolonSep      , 65  , 65  , // 65   - bg-bg
            0x66   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xaf  , 1 | SemicolonSep      , 67  , 240 , // 66   - bin
            0x466  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xaf  , 1 | SemicolonSep      , 67  , 240 , // 67   - bin-ng
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9d  , 1 | SemicolonSep      , 70  , 240 , // 68   - bm
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9d  , 1 | SemicolonSep      , 70  , 240 , // 69   - bm-latn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9d  , 1 | SemicolonSep      , 70  , 240 , // 70   - bm-latn-ml
            0x45   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x17  , 1 | CommaSep          , 72  , 143 , // 71   - bn
            0x845  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x17  , 1 | CommaSep          , 72  , 143 , // 72   - bn-bd
            0x445  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 73  , 143 , // 73   - bn-in
            0x51   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2d  , 1 | CommaSep          , 75  , 143 , // 74   - bo
            0x451  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2d  , 1 | CommaSep          , 75  , 143 , // 75   - bo-cn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | SemicolonSep      , 76  , 240 , // 76   - bo-in
            0x7e   , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x54  , 1 | SemicolonSep      , 78  , 78  , // 77   - br
            0x47e  , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x54  , 1 | SemicolonSep      , 78  , 78  , // 78   - br-fr
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | SemicolonSep      , 80  , 240 , // 79   - brx
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | SemicolonSep      , 80  , 240 , // 80   - brx-in
            0x781a , 0x4e2 , 0x354 , 0x2762, 0x366 , 0x19  , 1 | SemicolonSep      , 85  , 85  , // 81   - bs
            0x641a , 0x4e3 , 0x357 , 0x2762, 0x366 , 0x19  , 1 | SemicolonSep      , 83  , 83  , // 82   - bs-cyrl
            0x201a , 0x4e3 , 0x357 , 0x2762, 0x366 , 0x19  , 1 | SemicolonSep      , 83  , 83  , // 83   - bs-cyrl-ba
            0x681a , 0x4e2 , 0x354 , 0x2762, 0x366 , 0x19  , 1 | SemicolonSep      , 85  , 85  , // 84   - bs-latn
            0x141a , 0x4e2 , 0x354 , 0x2762, 0x366 , 0x19  , 1 | SemicolonSep      , 85  , 85  , // 85   - bs-latn-ba
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x47  , 1 | SemicolonSep      , 87  , 240 , // 86   - byn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x47  , 1 | SemicolonSep      , 87  , 240 , // 87   - byn-er
            0x3    , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd9  , 1 | SemicolonSep      , 90  , 90  , // 88   - ca
            0x1000 , 0x4e4 , 0x352 , 0x2   , 0x1f4 , 0x8   , 1 | SemicolonSep      , 89  , 240 , // 89   - ca-ad
            0x403  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd9  , 1 | SemicolonSep      , 90  , 90  , // 90   - ca-es
            0x803  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd9  , 1 | SemicolonSep      , 91  , 90  , // 91   - ca-es-valencia
            0x1000 , 0x4e4 , 0x352 , 0x2   , 0x1f4 , 0x54  , 1 | SemicolonSep      , 92  , 240 , // 92   - ca-fr
            0x1000 , 0x4e4 , 0x352 , 0x2   , 0x1f4 , 0x76  , 1 | SemicolonSep      , 93  , 240 , // 93   - ca-it
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xcb  , 1 | SemicolonSep      , 95  , 240 , // 94   - ce
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xcb  , 1 | SemicolonSep      , 95  , 240 , // 95   - ce-ru
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf0  , 1 | SemicolonSep      , 97  , 240 , // 96   - cgg
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf0  , 1 | SemicolonSep      , 97  , 240 , // 97   - cgg-ug
            0x5c   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf4  , 1 | CommaSep          , 100 , 240 , // 98   - chr
            0x7c5c , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf4  , 1 | CommaSep          , 100 , 240 , // 99   - chr-cher
            0x45c  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf4  , 1 | CommaSep          , 100 , 240 , // 100  - chr-cher-us
            0x83   , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x54  , 1 | SemicolonSep      , 102 , 102 , // 101  - co
            0x483  , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x54  , 1 | SemicolonSep      , 102 , 102 , // 102  - co-fr
            0x5    , 0x4e2 , 0x354 , 0x272d, 0x1f4 , 0x4b  , 1 | SemicolonSep      , 104 , 104 , // 103  - cs
            0x405  , 0x4e2 , 0x354 , 0x272d, 0x1f4 , 0x4b  , 1 | SemicolonSep      , 104 , 104 , // 104  - cs-cz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xcb  , 1 | SemicolonSep      , 106 , 240 , // 105  - cu
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xcb  , 1 | SemicolonSep      , 106 , 240 , // 106  - cu-ru
            0x52   , 0x4e4 , 0x352 , 0x2710, 0x4f3d, 0xf2  , 1 | SemicolonSep      , 108 , 108 , // 107  - cy
            0x452  , 0x4e4 , 0x352 , 0x2710, 0x4f3d, 0xf2  , 1 | SemicolonSep      , 108 , 108 , // 108  - cy-gb
            0x6    , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0x3d  , 1 | SemicolonSep      , 110 , 110 , // 109  - da
            0x406  , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0x3d  , 1 | SemicolonSep      , 110 , 110 , // 110  - da-dk
            0x1000 , 0x4e4 , 0x352 , 0x2   , 0x1f4 , 0x5d  , 1 | SemicolonSep      , 111 , 240 , // 111  - da-gl
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 113 , 240 , // 112  - dav
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 113 , 240 , // 113  - dav-ke
            0x7    , 0x4e4 , 0x352 , 0x2710, 0x4f31, 0x5e  , 1 | SemicolonSep      , 118 , 118 , // 114  - de
            0xc07  , 0x4e4 , 0x352 , 0x2710, 0x4f31, 0xe   , 1 | SemicolonSep      , 115 , 115 , // 115  - de-at
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f31, 0x15  , 1 | SemicolonSep      , 116 , 240 , // 116  - de-be
            0x807  , 0x4e4 , 0x352 , 0x2710, 0x4f31, 0xdf  , 1 | SemicolonSep      , 117 , 117 , // 117  - de-ch
            0x407  , 0x4e4 , 0x352 , 0x2710, 0x4f31, 0x5e  , 1 | SemicolonSep      , 118 , 118 , // 118  - de-de
            0x10407, 0x4e4 , 0x352 , 0x2710, 0x4f31, 0x5e  , 1 | SemicolonSep      , 118 , 118 , // 119  - de-de_phoneb
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x76  , 1 | SemicolonSep      , 120 , 240 , // 120  - de-it
            0x1407 , 0x4e4 , 0x352 , 0x2710, 0x4f31, 0x91  , 1 | SemicolonSep      , 121 , 121 , // 121  - de-li
            0x1007 , 0x4e4 , 0x352 , 0x2710, 0x4f31, 0x93  , 1 | SemicolonSep      , 122 , 122 , // 122  - de-lu
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xad  , 1 | SemicolonSep      , 124 , 240 , // 123  - dje
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xad  , 1 | SemicolonSep      , 124 , 240 , // 124  - dje-ne
            0x7c2e , 0x4e4 , 0x352 , 0x2710, 0x366 , 0x5e  , 1 | SemicolonSep      , 126 , 126 , // 125  - dsb
            0x82e  , 0x4e4 , 0x352 , 0x2710, 0x366 , 0x5e  , 1 | SemicolonSep      , 126 , 126 , // 126  - dsb-de
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 128 , 240 , // 127  - dua
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 128 , 240 , // 128  - dua-cm
            0x65   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xa5  , 1 | ArabicCommaSep    , 130 , 143 , // 129  - dv
            0x465  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xa5  , 1 | ArabicCommaSep    , 130 , 143 , // 130  - dv-mv
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd2  , 1 | SemicolonSep      , 132 , 240 , // 131  - dyo
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd2  , 1 | SemicolonSep      , 132 , 240 , // 132  - dyo-sn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x22  , 2 | SemicolonSep      , 134 , 240 , // 133  - dz
            0xc51  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x22  , 2 | SemicolonSep      , 134 , 240 , // 134  - dz-bt
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 136 , 240 , // 135  - ebu
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 136 , 240 , // 136  - ebu-ke
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x59  , 1 | SemicolonSep      , 138 , 240 , // 137  - ee
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x59  , 1 | SemicolonSep      , 138 , 240 , // 138  - ee-gh
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xe8  , 1 | SemicolonSep      , 139 , 240 , // 139  - ee-tg
            0x8    , 0x4e5 , 0x2e1 , 0x2716, 0x4f31, 0x62  , 1 | SemicolonSep      , 142 , 142 , // 140  - el
            0x1000 , 0x4e5 , 0x2e1 , 0x2716, 0x4f31, 0x3b  , 1 | SemicolonSep      , 141 , 240 , // 141  - el-cy
            0x408  , 0x4e5 , 0x2e1 , 0x2716, 0x4f31, 0x62  , 1 | SemicolonSep      , 142 , 142 , // 142  - el-gr
            0x9    , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xf4  , 1 | CommaSep          , 240 , 240 , // 143  - en
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x989e, 1 | CommaSep          , 144 , 240 , // 144  - en-001
            0x2409 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x993248, 1 | CommaSep        , 145 , 145 , // 145  - en-029
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x292d, 1 | CommaSep          , 146 , 240 , // 146  - en-150
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x2   , 1 | CommaSep          , 147 , 240 , // 147  - en-ag
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x12c , 1 | CommaSep          , 148 , 240 , // 148  - en-ai
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xa   , 1 | CommaSep          , 149 , 240 , // 149  - en-as
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xe   , 1 | CommaSep          , 150 , 240 , // 150  - en-at
            0xc09  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xc   , 1 | CommaSep          , 151 , 151 , // 151  - en-au
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x12  , 1 | CommaSep          , 152 , 240 , // 152  - en-bb
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x15  , 1 | CommaSep          , 153 , 240 , // 153  - en-be
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x26  , 1 | CommaSep          , 154 , 240 , // 154  - en-bi
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x14  , 1 | CommaSep          , 155 , 240 , // 155  - en-bm
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x16  , 1 | CommaSep          , 156 , 240 , // 156  - en-bs
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x13  , 1 | CommaSep          , 157 , 240 , // 157  - en-bw
            0x2809 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x18  , 1 | CommaSep          , 158 , 158 , // 158  - en-bz
            0x1009 , 0x4e4 , 0x352 , 0x2710, 0x25  , 0x27  , 1 | CommaSep          , 159 , 159 , // 159  - en-ca
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x137 , 1 | CommaSep          , 160 , 240 , // 160  - en-cc
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xdf  , 1 | CommaSep          , 161 , 240 , // 161  - en-ch
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x138 , 1 | CommaSep          , 162 , 240 , // 162  - en-ck
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x31  , 1 | CommaSep          , 163 , 240 , // 163  - en-cm
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x135 , 1 | CommaSep          , 164 , 240 , // 164  - en-cx
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x3b  , 1 | CommaSep          , 165 , 240 , // 165  - en-cy
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x5e  , 1 | CommaSep          , 166 , 240 , // 166  - en-de
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x3d  , 1 | CommaSep          , 167 , 240 , // 167  - en-dk
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x3f  , 1 | CommaSep          , 168 , 240 , // 168  - en-dm
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x47  , 1 | CommaSep          , 169 , 240 , // 169  - en-er
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x4d  , 1 | CommaSep          , 170 , 240 , // 170  - en-fi
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x4e  , 1 | CommaSep          , 171 , 240 , // 171  - en-fj
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x13b , 1 | CommaSep          , 172 , 240 , // 172  - en-fk
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x50  , 1 | CommaSep          , 173 , 240 , // 173  - en-fm
            0x809  , 0x4e4 , 0x352 , 0x2710, 0x4f3d, 0xf2  , 1 | CommaSep          , 174 , 174 , // 174  - en-gb
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x5b  , 1 | CommaSep          , 175 , 240 , // 175  - en-gd
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x144 , 1 | CommaSep          , 176 , 240 , // 176  - en-gg
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x59  , 1 | CommaSep          , 177 , 240 , // 177  - en-gh
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x5a  , 1 | CommaSep          , 178 , 240 , // 178  - en-gi
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x56  , 1 | CommaSep          , 179 , 240 , // 179  - en-gm
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x142 , 1 | CommaSep          , 180 , 240 , // 180  - en-gu
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x65  , 1 | CommaSep          , 181 , 240 , // 181  - en-gy
            0x3c09 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x68  , 1 | CommaSep          , 182 , 240 , // 182  - en-hk
            0x3809 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x6f  , 1 | SemicolonSep      , 183 , 240 , // 183  - en-id
            0x1809 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x44  , 1 | CommaSep          , 184 , 184 , // 184  - en-ie
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x75  , 1 | CommaSep          , 185 , 240 , // 185  - en-il
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x3b16, 1 | CommaSep          , 186 , 240 , // 186  - en-im
            0x4009 , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0x71  , 1 | CommaSep          , 187 , 187 , // 187  - en-in
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x72  , 1 | CommaSep          , 188 , 240 , // 188  - en-io
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x148 , 1 | CommaSep          , 189 , 240 , // 189  - en-je
            0x2009 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x7c  , 1 | CommaSep          , 190 , 190 , // 190  - en-jm
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x81  , 1 | CommaSep          , 191 , 240 , // 191  - en-ke
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x85  , 1 | CommaSep          , 192 , 240 , // 192  - en-ki
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xcf  , 1 | CommaSep          , 193 , 240 , // 193  - en-kn
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x133 , 1 | CommaSep          , 194 , 240 , // 194  - en-ky
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xda  , 1 | CommaSep          , 195 , 240 , // 195  - en-lc
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x8e  , 1 | CommaSep          , 196 , 240 , // 196  - en-lr
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x92  , 1 | CommaSep          , 197 , 240 , // 197  - en-ls
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x95  , 1 | CommaSep          , 198 , 240 , // 198  - en-mg
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xc7  , 1 | CommaSep          , 199 , 240 , // 199  - en-mh
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x97  , 1 | CommaSep          , 200 , 240 , // 200  - en-mo
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x151 , 1 | CommaSep          , 201 , 240 , // 201  - en-mp
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x14c , 1 | CommaSep          , 202 , 240 , // 202  - en-ms
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xa3  , 1 | CommaSep          , 203 , 240 , // 203  - en-mt
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xa0  , 1 | CommaSep          , 204 , 240 , // 204  - en-mu
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x9c  , 1 | CommaSep          , 205 , 240 , // 205  - en-mw
            0x4409 , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xa7  , 1 | CommaSep          , 206 , 206 , // 206  - en-my
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xfe  , 1 | CommaSep          , 207 , 240 , // 207  - en-na
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x150 , 1 | CommaSep          , 208 , 240 , // 208  - en-nf
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xaf  , 1 | CommaSep          , 209 , 240 , // 209  - en-ng
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xb0  , 1 | CommaSep          , 210 , 240 , // 210  - en-nl
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xb4  , 1 | CommaSep          , 211 , 240 , // 211  - en-nr
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x14f , 1 | CommaSep          , 212 , 240 , // 212  - en-nu
            0x1409 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xb7  , 1 | CommaSep          , 213 , 213 , // 213  - en-nz
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xc2  , 1 | CommaSep          , 214 , 240 , // 214  - en-pg
            0x3409 , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0xc9  , 1 | CommaSep          , 215 , 215 , // 215  - en-ph
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xbe  , 1 | CommaSep          , 216 , 240 , // 216  - en-pk
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x153 , 1 | CommaSep          , 217 , 240 , // 217  - en-pn
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xca  , 1 | CommaSep          , 218 , 240 , // 218  - en-pr
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xc3  , 1 | CommaSep          , 219 , 240 , // 219  - en-pw
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xcc  , 1 | CommaSep          , 220 , 240 , // 220  - en-rw
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x1e  , 1 | CommaSep          , 221 , 240 , // 221  - en-sb
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd0  , 1 | CommaSep          , 222 , 240 , // 222  - en-sc
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xdb  , 1 | CommaSep          , 223 , 240 , // 223  - en-sd
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xdd  , 1 | CommaSep          , 224 , 240 , // 224  - en-se
            0x4809 , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xd7  , 1 | CommaSep          , 225 , 225 , // 225  - en-sg
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x157 , 1 | CommaSep          , 226 , 240 , // 226  - en-sh
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd4  , 1 | CommaSep          , 227 , 240 , // 227  - en-si
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd5  , 1 | CommaSep          , 228 , 240 , // 228  - en-sl
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x114 , 1 | CommaSep          , 229 , 240 , // 229  - en-ss
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x78f7, 1 | CommaSep          , 230 , 240 , // 230  - en-sx
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x104 , 1 | CommaSep          , 231 , 240 , // 231  - en-sz
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x15d , 1 | CommaSep          , 232 , 240 , // 232  - en-tc
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x15b , 1 | CommaSep          , 233 , 240 , // 233  - en-tk
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xe7  , 1 | CommaSep          , 234 , 240 , // 234  - en-to
            0x2c09 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xe1  , 1 | CommaSep          , 235 , 235 , // 235  - en-tt
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xec  , 1 | CommaSep          , 236 , 240 , // 236  - en-tv
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xef  , 1 | CommaSep          , 237 , 240 , // 237  - en-tz
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xf0  , 1 | CommaSep          , 238 , 240 , // 238  - en-ug
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x9a55d40,1 | CommaSep        , 239 , 240 , // 239  - en-um
            0x409  , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xf4  , 1 | CommaSep          , 240 , 240 , // 240  - en-us
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xf8  , 1 | CommaSep          , 241 , 240 , // 241  - en-vc
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x15f , 1 | CommaSep          , 242 , 240 , // 242  - en-vg
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xfc  , 1 | CommaSep          , 243 , 240 , // 243  - en-vi
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xae  , 1 | CommaSep          , 244 , 240 , // 244  - en-vu
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x103 , 1 | CommaSep          , 245 , 240 , // 245  - en-ws
            0x1c09 , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0xd1  , 1 | CommaSep          , 246 , 246 , // 246  - en-za
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x107 , 1 | CommaSep          , 247 , 240 , // 247  - en-zm
            0x3009 , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0x108 , 1 | CommaSep          , 248 , 248 , // 248  - en-zw
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x989e, 1 | SemicolonSep      , 250 , 240 , // 249  - eo
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x989e, 1 | SemicolonSep      , 250 , 240 , // 250  - eo-001
            0xa    , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xd9  , 1 | SemicolonSep      , 262 , 262 , // 251  - es
            0x580a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x9a55d41, 1 | SemicolonSep   , 252 , 240 , // 252  - es-419
            0x2c0a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xb   , 1 | SemicolonSep      , 253 , 253 , // 253  - es-ar
            0x400a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x1a  , 1 | SemicolonSep      , 254 , 254 , // 254  - es-bo
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x20  , 1 | SemicolonSep      , 255 , 240 , // 255  - es-br
            0x340a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x2e  , 1 | SemicolonSep      , 256 , 256 , // 256  - es-cl
            0x240a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x33  , 1 | SemicolonSep      , 257 , 257 , // 257  - es-co
            0x140a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x36  , 1 | SemicolonSep      , 258 , 258 , // 258  - es-cr
            0x5c0a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x38  , 1 | SemicolonSep      , 259 , 240 , // 259  - es-cu
            0x1c0a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x41  , 1 | SemicolonSep      , 260 , 260 , // 260  - es-do
            0x300a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x42  , 1 | SemicolonSep      , 261 , 261 , // 261  - es-ec
            0xc0a  , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xd9  , 1 | SemicolonSep      , 262 , 262 , // 262  - es-es
            0x40a  , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xd9  , 1 | SemicolonSep      , 263 , 263 , // 263  - es-es_tradnl
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x45  , 1 | SemicolonSep      , 264 , 240 , // 264  - es-gq
            0x100a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x63  , 1 | SemicolonSep      , 265 , 265 , // 265  - es-gt
            0x480a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x6a  , 1 | SemicolonSep      , 266 , 266 , // 266  - es-hn
            0x80a  , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xa6  , 1 | CommaSep          , 267 , 267 , // 267  - es-mx
            0x4c0a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xb6  , 1 | SemicolonSep      , 268 , 268 , // 268  - es-ni
            0x180a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xc0  , 1 | SemicolonSep      , 269 , 269 , // 269  - es-pa
            0x280a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xbb  , 1 | SemicolonSep      , 270 , 270 , // 270  - es-pe
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xc9  , 1 | SemicolonSep      , 271 , 240 , // 271  - es-ph
            0x500a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xca  , 1 | SemicolonSep      , 272 , 272 , // 272  - es-pr
            0x3c0a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xb9  , 1 | SemicolonSep      , 273 , 273 , // 273  - es-py
            0x440a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x48  , 1 | SemicolonSep      , 274 , 274 , // 274  - es-sv
            0x540a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xf4  , 1 | CommaSep          , 275 , 275 , // 275  - es-us
            0x380a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xf6  , 1 | SemicolonSep      , 276 , 276 , // 276  - es-uy
            0x200a , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xf9  , 1 | SemicolonSep      , 277 , 277 , // 277  - es-ve
            0x25   , 0x4e9 , 0x307 , 0x272d, 0x1f4 , 0x46  , 1 | SemicolonSep      , 279 , 279 , // 278  - et
            0x425  , 0x4e9 , 0x307 , 0x272d, 0x1f4 , 0x46  , 1 | SemicolonSep      , 279 , 279 , // 279  - et-ee
            0x2d   , 0x4e4 , 0x352 , 0x2   , 0x1f4 , 0xd9  , 1 | SemicolonSep      , 281 , 240 , // 280  - eu
            0x42d  , 0x4e4 , 0x352 , 0x2   , 0x1f4 , 0xd9  , 1 | SemicolonSep      , 281 , 240 , // 281  - eu-es
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 283 , 240 , // 282  - ewo
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 283 , 240 , // 283  - ewo-cm
            0x29   , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x74  , 0 | ArabicSemicolonSep, 285 , 143 , // 284  - fa
            0x429  , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x74  , 0 | ArabicSemicolonSep, 285 , 143 , // 285  - fa-ir
            0x67   , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xd2  , 1 | SemicolonSep      , 290 , 290 , // 286  - ff
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x31  , 1 | SemicolonSep      , 287 , 240 , // 287  - ff-cm
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x64  , 1 | SemicolonSep      , 288 , 240 , // 288  - ff-gn
            0x7c67 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xd2  , 1 | SemicolonSep      , 290 , 290 , // 289  - ff-latn
            0x867  , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xd2  , 1 | SemicolonSep      , 290 , 290 , // 290  - ff-latn-sn
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xa2  , 1 | SemicolonSep      , 291 , 240 , // 291  - ff-mr
            0x467  , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xaf  , 1 | SemicolonSep      , 292 , 240 , // 292  - ff-ng
            0xb    , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0x4d  , 1 | SemicolonSep      , 294 , 294 , // 293  - fi
            0x40b  , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0x4d  , 1 | SemicolonSep      , 294 , 294 , // 294  - fi-fi
            0x64   , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0xc9  , 1 | SemicolonSep      , 296 , 296 , // 295  - fil
            0x464  , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0xc9  , 1 | SemicolonSep      , 296 , 296 , // 296  - fil-ph
            0x38   , 0x4e4 , 0x352 , 0x275f, 0x4f35, 0x51  , 1 | SemicolonSep      , 299 , 299 , // 297  - fo
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x3d  , 1 | SemicolonSep      , 298 , 240 , // 298  - fo-dk
            0x438  , 0x4e4 , 0x352 , 0x275f, 0x4f35, 0x51  , 1 | SemicolonSep      , 299 , 299 , // 299  - fo-fo
            0xc    , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x54  , 1 | SemicolonSep      , 316 , 316 , // 300  - fr
            0x1c0c , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x993248, 1 | SemicolonSep    , 301 , 316 , // 301  - fr-029
            0x80c  , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x15  , 1 | SemicolonSep      , 302 , 302 , // 302  - fr-be
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xf5  , 1 | SemicolonSep      , 303 , 240 , // 303  - fr-bf
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x26  , 1 | SemicolonSep      , 304 , 240 , // 304  - fr-bi
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x1c  , 1 | SemicolonSep      , 305 , 240 , // 305  - fr-bj
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x9a55c4f, 1 | SemicolonSep   , 306 , 240 , // 306  - fr-bl
            0xc0c  , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x27  , 1 | SemicolonSep      , 307 , 307 , // 307  - fr-ca
            0x240c , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x2c  , 1 | SemicolonSep      , 308 , 240 , // 308  - fr-cd
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x37  , 1 | SemicolonSep      , 309 , 240 , // 309  - fr-cf
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x2b  , 1 | SemicolonSep      , 310 , 240 , // 310  - fr-cg
            0x100c , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xdf  , 1 | SemicolonSep      , 311 , 311 , // 311  - fr-ch
            0x300c , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x77  , 1 | SemicolonSep      , 312 , 240 , // 312  - fr-ci
            0x2c0c , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x31  , 1 | SemicolonSep      , 313 , 240 , // 313  - fr-cm
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x3e  , 1 | SemicolonSep      , 314 , 240 , // 314  - fr-dj
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x4   , 1 | SemicolonSep      , 315 , 240 , // 315  - fr-dz
            0x40c  , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x54  , 1 | SemicolonSep      , 316 , 316 , // 316  - fr-fr
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x57  , 1 | SemicolonSep      , 317 , 240 , // 317  - fr-ga
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x13d , 1 | SemicolonSep      , 318 , 240 , // 318  - fr-gf
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x64  , 1 | SemicolonSep      , 319 , 240 , // 319  - fr-gn
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x141 , 1 | SemicolonSep      , 320 , 240 , // 320  - fr-gp
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x45  , 1 | SemicolonSep      , 321 , 240 , // 321  - fr-gq
            0x3c0c , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x67  , 1 | SemicolonSep      , 322 , 240 , // 322  - fr-ht
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x32  , 1 | SemicolonSep      , 323 , 240 , // 323  - fr-km
            0x140c , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x93  , 1 | SemicolonSep      , 324 , 324 , // 324  - fr-lu
            0x380c , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x9f  , 1 | SemicolonSep      , 325 , 240 , // 325  - fr-ma
            0x180c , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x9e  , 1 | SemicolonSep      , 326 , 326 , // 326  - fr-mc
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x7bda, 1 | SemicolonSep      , 327 , 240 , // 327  - fr-mf
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x95  , 1 | SemicolonSep      , 328 , 240 , // 328  - fr-mg
            0x340c , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x9d  , 1 | SemicolonSep      , 329 , 240 , // 329  - fr-ml
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x14a , 1 | SemicolonSep      , 330 , 240 , // 330  - fr-mq
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xa2  , 1 | SemicolonSep      , 331 , 240 , // 331  - fr-mr
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xa0  , 1 | SemicolonSep      , 332 , 240 , // 332  - fr-mu
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x14e , 1 | SemicolonSep      , 333 , 240 , // 333  - fr-nc
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xad  , 1 | SemicolonSep      , 334 , 240 , // 334  - fr-ne
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x13e , 1 | SemicolonSep      , 335 , 240 , // 335  - fr-pf
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xce  , 1 | SemicolonSep      , 336 , 240 , // 336  - fr-pm
            0x200c , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xc6  , 1 | SemicolonSep      , 337 , 240 , // 337  - fr-re
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xcc  , 1 | SemicolonSep      , 338 , 240 , // 338  - fr-rw
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xd0  , 1 | SemicolonSep      , 339 , 240 , // 339  - fr-sc
            0x280c , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xd2  , 1 | SemicolonSep      , 340 , 240 , // 340  - fr-sn
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xde  , 1 | SemicolonSep      , 341 , 240 , // 341  - fr-sy
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x29  , 1 | SemicolonSep      , 342 , 240 , // 342  - fr-td
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xe8  , 1 | SemicolonSep      , 343 , 240 , // 343  - fr-tg
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xea  , 1 | SemicolonSep      , 344 , 240 , // 344  - fr-tn
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xae  , 1 | SemicolonSep      , 345 , 240 , // 345  - fr-vu
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x160 , 1 | SemicolonSep      , 346 , 240 , // 346  - fr-wf
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x14b , 1 | SemicolonSep      , 347 , 240 , // 347  - fr-yt
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x76  , 1 | SemicolonSep      , 349 , 240 , // 348  - fur
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x76  , 1 | SemicolonSep      , 349 , 240 , // 349  - fur-it
            0x62   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xb0  , 1 | SemicolonSep      , 351 , 351 , // 350  - fy
            0x462  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xb0  , 1 | SemicolonSep      , 351 , 351 , // 351  - fy-nl
            0x3c   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x44  , 1 | SemicolonSep      , 353 , 353 , // 352  - ga
            0x83c  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x44  , 1 | SemicolonSep      , 353 , 353 , // 353  - ga-ie
            0x91   , 0x4e4 , 0x352 , 0x2710, 0x4f3d, 0xf2  , 1 | SemicolonSep      , 355 , 355 , // 354  - gd
            0x491  , 0x4e4 , 0x352 , 0x2710, 0x4f3d, 0xf2  , 1 | SemicolonSep      , 355 , 355 , // 355  - gd-gb
            0x56   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd9  , 1 | SemicolonSep      , 357 , 357 , // 356  - gl
            0x456  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd9  , 1 | SemicolonSep      , 357 , 357 , // 357  - gl-es
            0x74   , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xb9  , 1 | CommaSep          , 359 , 359 , // 358  - gn
            0x474  , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xb9  , 1 | CommaSep          , 359 , 359 , // 359  - gn-py
            0x84   , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xdf  , 1 | SemicolonSep      , 361 , 240 , // 360  - gsw
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xdf  , 1 | SemicolonSep      , 361 , 240 , // 361  - gsw-ch
            0x484  , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x54  , 1 | SemicolonSep      , 362 , 362 , // 362  - gsw-fr
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x91  , 1 | SemicolonSep      , 363 , 240 , // 363  - gsw-li
            0x47   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 365 , 143 , // 364  - gu
            0x447  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 365 , 143 , // 365  - gu-in
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 367 , 240 , // 366  - guz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 367 , 240 , // 367  - guz-ke
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x3b16, 1 | SemicolonSep      , 369 , 240 , // 368  - gv
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x3b16, 1 | SemicolonSep      , 369 , 240 , // 369  - gv-im
            0x68   , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xaf  , 1 | SemicolonSep      , 374 , 374 , // 370  - ha
            0x7c68 , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xaf  , 1 | SemicolonSep      , 374 , 374 , // 371  - ha-latn
            0x1000 , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0x59  , 1 | SemicolonSep      , 372 , 240 , // 372  - ha-latn-gh
            0x1000 , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0xad  , 1 | SemicolonSep      , 373 , 240 , // 373  - ha-latn-ne
            0x468  , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xaf  , 1 | SemicolonSep      , 374 , 374 , // 374  - ha-latn-ng
            0x75   , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xf4  , 1 | SemicolonSep      , 376 , 376 , // 375  - haw
            0x475  , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xf4  , 1 | SemicolonSep      , 376 , 376 , // 376  - haw-us
            0xd    , 0x4e7 , 0x35e , 0x2715, 0x1f4 , 0x75  , 1 | CommaSep          , 378 , 143 , // 377  - he
            0x40d  , 0x4e7 , 0x35e , 0x2715, 0x1f4 , 0x75  , 1 | CommaSep          , 378 , 143 , // 378  - he-il
            0x39   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 380 , 143 , // 379  - hi
            0x439  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 380 , 143 , // 380  - hi-in
            0x1a   , 0x4e2 , 0x354 , 0x2762, 0x1f4 , 0x6c  , 1 | SemicolonSep      , 383 , 383 , // 381  - hr
            0x101a , 0x4e2 , 0x354 , 0x2762, 0x366 , 0x19  , 1 | SemicolonSep      , 382 , 382 , // 382  - hr-ba
            0x41a  , 0x4e2 , 0x354 , 0x2762, 0x1f4 , 0x6c  , 1 | SemicolonSep      , 383 , 383 , // 383  - hr-hr
            0x2e   , 0x4e4 , 0x352 , 0x2710, 0x366 , 0x5e  , 1 | SemicolonSep      , 385 , 385 , // 384  - hsb
            0x42e  , 0x4e4 , 0x352 , 0x2710, 0x366 , 0x5e  , 1 | SemicolonSep      , 385 , 385 , // 385  - hsb-de
            0xe    , 0x4e2 , 0x354 , 0x272d, 0x1f4 , 0x6d  , 1 | SemicolonSep      , 387 , 387 , // 386  - hu
            0x40e  , 0x4e2 , 0x354 , 0x272d, 0x1f4 , 0x6d  , 1 | SemicolonSep      , 387 , 387 , // 387  - hu-hu
            0x1040e, 0x4e2 , 0x354 , 0x272d, 0x1f4 , 0x6d  , 1 | SemicolonSep      , 387 , 387 , // 388  - hu-hu_technl
            0x2b   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x7   , 1 | CommaSep          , 390 , 390 , // 389  - hy
            0x42b  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x7   , 1 | CommaSep          , 390 , 390 , // 390  - hy-am
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x54  , 1 | SemicolonSep      , 393 , 240 , // 391  - ia
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x989e, 1 | SemicolonSep      , 392 , 240 , // 392  - ia-001
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x54  , 1 | SemicolonSep      , 393 , 240 , // 393  - ia-fr
            0x69   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xaf  , 1 | SemicolonSep      , 395 , 240 , // 394  - ibb
            0x469  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xaf  , 1 | SemicolonSep      , 395 , 240 , // 395  - ibb-ng
            0x21   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x6f  , 1 | SemicolonSep      , 397 , 397 , // 396  - id
            0x421  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x6f  , 1 | SemicolonSep      , 397 , 397 , // 397  - id-id
            0x70   , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xaf  , 1 | SemicolonSep      , 399 , 399 , // 398  - ig
            0x470  , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xaf  , 1 | SemicolonSep      , 399 , 399 , // 399  - ig-ng
            0x78   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2d  , 1 | SemicolonSep      , 401 , 143 , // 400  - ii
            0x478  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2d  , 1 | SemicolonSep      , 401 , 143 , // 401  - ii-cn
            0xf    , 0x4e4 , 0x352 , 0x275f, 0x5187, 0x6e  , 1 | SemicolonSep      , 403 , 403 , // 402  - is
            0x40f  , 0x4e4 , 0x352 , 0x275f, 0x5187, 0x6e  , 1 | SemicolonSep      , 403 , 403 , // 403  - is-is
            0x10   , 0x4e4 , 0x352 , 0x2710, 0x4f38, 0x76  , 1 | SemicolonSep      , 406 , 406 , // 404  - it
            0x810  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xdf  , 1 | SemicolonSep      , 405 , 405 , // 405  - it-ch
            0x410  , 0x4e4 , 0x352 , 0x2710, 0x4f38, 0x76  , 1 | SemicolonSep      , 406 , 406 , // 406  - it-it
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f38, 0xd6  , 1 | SemicolonSep      , 407 , 240 , // 407  - it-sm
            0x5d   , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0x27  , 1 | CommaSep          , 412 , 412 , // 408  - iu
            0x785d , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x27  , 1 | CommaSep          , 410 , 143 , // 409  - iu-cans
            0x45d  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x27  , 1 | CommaSep          , 410 , 143 , // 410  - iu-cans-ca
            0x7c5d , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0x27  , 1 | CommaSep          , 412 , 412 , // 411  - iu-latn
            0x85d  , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0x27  , 1 | CommaSep          , 412 , 412 , // 412  - iu-latn-ca
            0x11   , 0x3a4 , 0x3a4 , 0x2711, 0x4f42, 0x7a  , 1 | CommaSep          , 414 , 414 , // 413  - ja
            0x411  , 0x3a4 , 0x3a4 , 0x2711, 0x4f42, 0x7a  , 1 | CommaSep          , 414 , 414 , // 414  - ja-jp
            0x40411, 0x3a4 , 0x3a4 , 0x2711, 0x4f42, 0x7a  , 1 | CommaSep          , 414 , 414 , // 415  - ja-jp_radstr
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 417 , 240 , // 416  - jgo
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 417 , 240 , // 417  - jgo-cm
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 419 , 240 , // 418  - jmc
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 419 , 240 , // 419  - jmc-tz
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x6f  , 1 | SemicolonSep      , 424 , 424 , // 420  - jv
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x6f  , 1 | SemicolonSep      , 422 , 424 , // 421  - jv-java
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x6f  , 1 | SemicolonSep      , 422 , 424 , // 422  - jv-java-id
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x6f  , 1 | SemicolonSep      , 424 , 424 , // 423  - jv-latn
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x6f  , 1 | SemicolonSep      , 424 , 424 , // 424  - jv-latn-id
            0x37   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x58  , 1 | SemicolonSep      , 426 , 426 , // 425  - ka
            0x437  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x58  , 1 | SemicolonSep      , 426 , 426 , // 426  - ka-ge
            0x10437, 0x0   , 0x1   , 0x2   , 0x1f4 , 0x58  , 1 | SemicolonSep      , 426 , 426 , // 427  - ka-ge_modern
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x4   , 1 | SemicolonSep      , 429 , 240 , // 428  - kab
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x4   , 1 | SemicolonSep      , 429 , 240 , // 429  - kab-dz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 431 , 240 , // 430  - kam
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 431 , 240 , // 431  - kam-ke
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 433 , 240 , // 432  - kde
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 433 , 240 , // 433  - kde-tz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x39  , 1 | SemicolonSep      , 435 , 240 , // 434  - kea
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x39  , 1 | SemicolonSep      , 435 , 240 , // 435  - kea-cv
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9d  , 1 | SemicolonSep      , 437 , 240 , // 436  - khq
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9d  , 1 | SemicolonSep      , 437 , 240 , // 437  - khq-ml
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 439 , 240 , // 438  - ki
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 439 , 240 , // 439  - ki-ke
            0x3f   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x89  , 1 | SemicolonSep      , 441 , 441 , // 440  - kk
            0x43f  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x89  , 1 | SemicolonSep      , 441 , 441 , // 441  - kk-kz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 443 , 240 , // 442  - kkj
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 443 , 240 , // 443  - kkj-cm
            0x6f   , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0x5d  , 1 | SemicolonSep      , 445 , 445 , // 444  - kl
            0x46f  , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0x5d  , 1 | SemicolonSep      , 445 , 445 , // 445  - kl-gl
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 447 , 240 , // 446  - kln
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 447 , 240 , // 447  - kln-ke
            0x53   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x28  , 2 | CommaSep          , 449 , 143 , // 448  - km
            0x453  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x28  , 2 | CommaSep          , 449 , 143 , // 449  - km-kh
            0x4b   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 451 , 143 , // 450  - kn
            0x44b  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 451 , 143 , // 451  - kn-in
            0x12   , 0x3b5 , 0x3b5 , 0x2713, 0x5161, 0x86  , 1 | CommaSep          , 454 , 454 , // 452  - ko
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x83  , 1 | SemicolonSep      , 453 , 240 , // 453  - ko-kp
            0x412  , 0x3b5 , 0x3b5 , 0x2713, 0x5161, 0x86  , 1 | CommaSep          , 454 , 454 , // 454  - ko-kr
            0x57   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 456 , 143 , // 455  - kok
            0x457  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 456 , 143 , // 456  - kok-in
            0x71   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xaf  , 1 | SemicolonSep      , 458 , 240 , // 457  - kr
            0x471  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xaf  , 1 | SemicolonSep      , 458 , 240 , // 458  - kr-ng
            0x60   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 2 | SemicolonSep      , 461 , 240 , // 459  - ks
            0x460  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 2 | SemicolonSep      , 461 , 240 , // 460  - ks-arab
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 2 | SemicolonSep      , 461 , 240 , // 461  - ks-arab-in
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 463 , 187 , // 462  - ks-deva
            0x860  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 463 , 187 , // 463  - ks-deva-in
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 465 , 240 , // 464  - ksb
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 465 , 240 , // 465  - ksb-tz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 467 , 240 , // 466  - ksf
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 467 , 240 , // 467  - ksf-cm
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x5e  , 1 | SemicolonSep      , 469 , 240 , // 468  - ksh
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x5e  , 1 | SemicolonSep      , 469 , 240 , // 469  - ksh-de
            0x92   , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x79  , 0 | ArabicSemicolonSep, 472 , 143 , // 470  - ku
            0x7c92 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x79  , 0 | ArabicSemicolonSep, 472 , 143 , // 471  - ku-arab
            0x492  , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x79  , 0 | ArabicSemicolonSep, 472 , 143 , // 472  - ku-arab-iq
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x74  , 0 | SemicolonSep      , 473 , 240 , // 473  - ku-arab-ir
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf2  , 1 | SemicolonSep      , 475 , 240 , // 474  - kw
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf2  , 1 | SemicolonSep      , 475 , 240 , // 475  - kw-gb
            0x40   , 0x4e3 , 0x362 , 0x2717, 0x5190, 0x82  , 1 | SemicolonSep      , 477 , 477 , // 476  - ky
            0x440  , 0x4e3 , 0x362 , 0x2717, 0x5190, 0x82  , 1 | SemicolonSep      , 477 , 477 , // 477  - ky-kg
            0x76   , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0x989e, 1 | CommaSep          , 479 , 143 , // 478  - la
            0x476  , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0x989e, 1 | CommaSep          , 479 , 143 , // 479  - la-001
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 481 , 240 , // 480  - lag
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 481 , 240 , // 481  - lag-tz
            0x6e   , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x93  , 1 | SemicolonSep      , 483 , 483 , // 482  - lb
            0x46e  , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x93  , 1 | SemicolonSep      , 483 , 483 , // 483  - lb-lu
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf0  , 1 | SemicolonSep      , 485 , 240 , // 484  - lg
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf0  , 1 | SemicolonSep      , 485 , 240 , // 485  - lg-ug
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf4  , 1 | SemicolonSep      , 487 , 240 , // 486  - lkt
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf4  , 1 | SemicolonSep      , 487 , 240 , // 487  - lkt-us
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2c  , 1 | SemicolonSep      , 490 , 240 , // 488  - ln
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9   , 1 | SemicolonSep      , 489 , 240 , // 489  - ln-ao
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2c  , 1 | SemicolonSep      , 490 , 240 , // 490  - ln-cd
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x37  , 1 | SemicolonSep      , 491 , 240 , // 491  - ln-cf
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2b  , 1 | SemicolonSep      , 492 , 240 , // 492  - ln-cg
            0x54   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x8a  , 1 | SemicolonSep      , 494 , 143 , // 493  - lo
            0x454  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x8a  , 1 | SemicolonSep      , 494 , 143 , // 494  - lo-la
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x74  , 2 | SemicolonSep      , 497 , 240 , // 495  - lrc
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x79  , 2 | SemicolonSep      , 496 , 240 , // 496  - lrc-iq
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x74  , 2 | SemicolonSep      , 497 , 240 , // 497  - lrc-ir
            0x27   , 0x4e9 , 0x307 , 0x272d, 0x1f4 , 0x8d  , 1 | SemicolonSep      , 499 , 499 , // 498  - lt
            0x427  , 0x4e9 , 0x307 , 0x272d, 0x1f4 , 0x8d  , 1 | SemicolonSep      , 499 , 499 , // 499  - lt-lt
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2c  , 1 | SemicolonSep      , 501 , 240 , // 500  - lu
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2c  , 1 | SemicolonSep      , 501 , 240 , // 501  - lu-cd
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 503 , 240 , // 502  - luo
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 503 , 240 , // 503  - luo-ke
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 505 , 240 , // 504  - luy
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 505 , 240 , // 505  - luy-ke
            0x26   , 0x4e9 , 0x307 , 0x272d, 0x1f4 , 0x8c  , 1 | SemicolonSep      , 507 , 507 , // 506  - lv
            0x426  , 0x4e9 , 0x307 , 0x272d, 0x1f4 , 0x8c  , 1 | SemicolonSep      , 507 , 507 , // 507  - lv-lv
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 509 , 240 , // 508  - mas
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 509 , 240 , // 509  - mas-ke
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 510 , 240 , // 510  - mas-tz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 512 , 240 , // 511  - mer
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 512 , 240 , // 512  - mer-ke
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xa0  , 1 | SemicolonSep      , 514 , 240 , // 513  - mfe
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xa0  , 1 | SemicolonSep      , 514 , 240 , // 514  - mfe-mu
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x95  , 1 | SemicolonSep      , 516 , 240 , // 515  - mg
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x95  , 1 | SemicolonSep      , 516 , 240 , // 516  - mg-mg
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xa8  , 1 | SemicolonSep      , 518 , 240 , // 517  - mgh
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xa8  , 1 | SemicolonSep      , 518 , 240 , // 518  - mgh-mz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 520 , 240 , // 519  - mgo
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 520 , 240 , // 520  - mgo-cm
            0x81   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xb7  , 1 | CommaSep          , 522 , 522 , // 521  - mi
            0x481  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xb7  , 1 | CommaSep          , 522 , 522 , // 522  - mi-nz
            0x2f   , 0x4e3 , 0x362 , 0x2717, 0x1f4 , 0x4ca2, 1 | SemicolonSep      , 524 , 524 , // 523  - mk
            0x42f  , 0x4e3 , 0x362 , 0x2717, 0x1f4 , 0x4ca2, 1 | SemicolonSep      , 524 , 524 , // 524  - mk-mk
            0x4c   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | SemicolonSep      , 526 , 143 , // 525  - ml
            0x44c  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | SemicolonSep      , 526 , 143 , // 526  - ml-in
            0x50   , 0x4e3 , 0x362 , 0x2717, 0x5190, 0x9a  , 1 | SemicolonSep      , 529 , 529 , // 527  - mn
            0x7850 , 0x4e3 , 0x362 , 0x2717, 0x5190, 0x9a  , 1 | SemicolonSep      , 529 , 529 , // 528  - mn-cyrl
            0x450  , 0x4e3 , 0x362 , 0x2717, 0x5190, 0x9a  , 1 | SemicolonSep      , 529 , 529 , // 529  - mn-mn
            0x7c50 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2d  , 1 | CommaSep          , 531 , 531 , // 530  - mn-mong
            0x850  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2d  , 1 | CommaSep          , 531 , 531 , // 531  - mn-mong-cn
            0xc50  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9a  , 1 | CommaSep          , 532 , 532 , // 532  - mn-mong-mn
            0x58   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 534 , 187 , // 533  - mni
            0x458  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 534 , 187 , // 534  - mni-in
            0x7c   , 0x4e4 , 0x352 , 0x2710, 0x25  , 0x27  , 1 | CommaSep          , 536 , 240 , // 535  - moh
            0x47c  , 0x4e4 , 0x352 , 0x2710, 0x25  , 0x27  , 1 | CommaSep          , 536 , 240 , // 536  - moh-ca
            0x4e   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 538 , 143 , // 537  - mr
            0x44e  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 538 , 143 , // 538  - mr-in
            0x3e   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xa7  , 1 | SemicolonSep      , 541 , 541 , // 539  - ms
            0x83e  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x25  , 1 | SemicolonSep      , 540 , 540 , // 540  - ms-bn
            0x43e  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xa7  , 1 | SemicolonSep      , 541 , 541 , // 541  - ms-my
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd7  , 1 | SemicolonSep      , 542 , 240 , // 542  - ms-sg
            0x3a   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xa3  , 1 | SemicolonSep      , 544 , 544 , // 543  - mt
            0x43a  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xa3  , 1 | SemicolonSep      , 544 , 544 , // 544  - mt-mt
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 546 , 240 , // 545  - mua
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 546 , 240 , // 546  - mua-cm
            0x55   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x1b  , 2 | SemicolonSep      , 548 , 240 , // 547  - my
            0x455  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x1b  , 2 | SemicolonSep      , 548 , 240 , // 548  - my-mm
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x74  , 2 | SemicolonSep      , 550 , 240 , // 549  - mzn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x74  , 2 | SemicolonSep      , 550 , 240 , // 550  - mzn-ir
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xfe  , 1 | SemicolonSep      , 552 , 240 , // 551  - naq
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xfe  , 1 | SemicolonSep      , 552 , 240 , // 552  - naq-na
            0x7c14 , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0xb1  , 1 | SemicolonSep      , 554 , 554 , // 553  - nb
            0x414  , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0xb1  , 1 | SemicolonSep      , 554 , 554 , // 554  - nb-no
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0xdc  , 1 | SemicolonSep      , 555 , 240 , // 555  - nb-sj
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x108 , 1 | SemicolonSep      , 557 , 240 , // 556  - nd
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x108 , 1 | SemicolonSep      , 557 , 240 , // 557  - nd-zw
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x5e  , 1 | SemicolonSep      , 559 , 240 , // 558  - nds
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x5e  , 1 | SemicolonSep      , 559 , 240 , // 559  - nds-de
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xb0  , 1 | SemicolonSep      , 560 , 240 , // 560  - nds-nl
            0x61   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xb2  , 1 | CommaSep          , 563 , 143 , // 561  - ne
            0x861  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 2 | SemicolonSep      , 562 , 240 , // 562  - ne-in
            0x461  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xb2  , 1 | CommaSep          , 563 , 143 , // 563  - ne-np
            0x13   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xb0  , 1 | SemicolonSep      , 569 , 569 , // 564  - nl
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x12e , 1 | SemicolonSep      , 565 , 240 , // 565  - nl-aw
            0x813  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x15  , 1 | SemicolonSep      , 566 , 566 , // 566  - nl-be
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x9a55d42, 1 | SemicolonSep   , 567 , 240 , // 567  - nl-bq
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x111 , 1 | SemicolonSep      , 568 , 240 , // 568  - nl-cw
            0x413  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xb0  , 1 | SemicolonSep      , 569 , 569 , // 569  - nl-nl
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xb5  , 1 | SemicolonSep      , 570 , 240 , // 570  - nl-sr
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x78f7, 1 | SemicolonSep      , 571 , 240 , // 571  - nl-sx
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 573 , 240 , // 572  - nmg
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 573 , 240 , // 573  - nmg-cm
            0x7814 , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0xb1  , 1 | SemicolonSep      , 575 , 575 , // 574  - nn
            0x814  , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0xb1  , 1 | SemicolonSep      , 575 , 575 , // 575  - nn-no
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 577 , 240 , // 576  - nnh
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 577 , 240 , // 577  - nnh-cm
            0x14   , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0xb1  , 1 | SemicolonSep      , 554 , 554 , // 578  - no
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x64  , 2 | ArabicCommaSep    , 580 , 143 , // 579  - nqo
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x64  , 2 | ArabicCommaSep    , 580 , 143 , // 580  - nqo-gn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd1  , 1 | SemicolonSep      , 582 , 240 , // 581  - nr
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd1  , 1 | SemicolonSep      , 582 , 240 , // 582  - nr-za
            0x6c   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd1  , 1 | SemicolonSep      , 584 , 584 , // 583  - nso
            0x46c  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd1  , 1 | SemicolonSep      , 584 , 584 , // 584  - nso-za
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x114 , 1 | SemicolonSep      , 586 , 240 , // 585  - nus
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x114 , 1 | SemicolonSep      , 586 , 240 , // 586  - nus-ss
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf0  , 1 | SemicolonSep      , 588 , 240 , // 587  - nyn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf0  , 1 | SemicolonSep      , 588 , 240 , // 588  - nyn-ug
            0x82   , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x54  , 1 | SemicolonSep      , 590 , 590 , // 589  - oc
            0x482  , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x54  , 1 | SemicolonSep      , 590 , 590 , // 590  - oc-fr
            0x72   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x49  , 1 | SemicolonSep      , 592 , 240 , // 591  - om
            0x472  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x49  , 1 | SemicolonSep      , 592 , 240 , // 592  - om-et
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 593 , 240 , // 593  - om-ke
            0x48   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 595 , 143 , // 594  - or
            0x448  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 595 , 143 , // 595  - or-in
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x58  , 1 | SemicolonSep      , 597 , 240 , // 596  - os
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x58  , 1 | SemicolonSep      , 597 , 240 , // 597  - os-ge
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xcb  , 1 | SemicolonSep      , 598 , 240 , // 598  - os-ru
            0x46   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 602 , 143 , // 599  - pa
            0x7c46 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xbe  , 2 | SemicolonSep      , 601 , 143 , // 600  - pa-arab
            0x846  , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xbe  , 2 | SemicolonSep      , 601 , 143 , // 601  - pa-arab-pk
            0x446  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 602 , 143 , // 602  - pa-in
            0x79   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x993248, 1 | CommaSep        , 604 , 145 , // 603  - pap
            0x479  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x993248, 1 | CommaSep        , 604 , 145 , // 604  - pap-029
            0x15   , 0x4e2 , 0x354 , 0x272d, 0x5190, 0xbf  , 1 | SemicolonSep      , 606 , 606 , // 605  - pl
            0x415  , 0x4e2 , 0x354 , 0x272d, 0x5190, 0xbf  , 1 | SemicolonSep      , 606 , 606 , // 606  - pl-pl
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x989e, 1 | SemicolonSep      , 608 , 240 , // 607  - prg
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x989e, 1 | SemicolonSep      , 608 , 240 , // 608  - prg-001
            0x8c   , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x3   , 2 | SemicolonSep      , 610 , 143 , // 609  - prs
            0x48c  , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x3   , 2 | SemicolonSep      , 610 , 143 , // 610  - prs-af
            0x63   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x3   , 2 | SemicolonSep      , 612 , 143 , // 611  - ps
            0x463  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x3   , 2 | SemicolonSep      , 612 , 143 , // 612  - ps-af
            0x16   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x20  , 1 | SemicolonSep      , 615 , 615 , // 613  - pt
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x9   , 1 | SemicolonSep      , 614 , 240 , // 614  - pt-ao
            0x416  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x20  , 1 | SemicolonSep      , 615 , 615 , // 615  - pt-br
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xdf  , 1 | SemicolonSep      , 616 , 240 , // 616  - pt-ch
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x39  , 1 | SemicolonSep      , 617 , 240 , // 617  - pt-cv
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x45  , 1 | SemicolonSep      , 618 , 240 , // 618  - pt-gq
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xc4  , 1 | SemicolonSep      , 619 , 240 , // 619  - pt-gw
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x93  , 1 | SemicolonSep      , 620 , 240 , // 620  - pt-lu
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x97  , 1 | SemicolonSep      , 621 , 240 , // 621  - pt-mo
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xa8  , 1 | SemicolonSep      , 622 , 240 , // 622  - pt-mz
            0x816  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xc1  , 1 | SemicolonSep      , 623 , 623 , // 623  - pt-pt
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xe9  , 1 | SemicolonSep      , 624 , 240 , // 624  - pt-st
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x6f60e7,1| SemicolonSep      , 625 , 240 , // 625  - pt-tl
            0x901  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x7c  , 1 | CommaSep          , 626 , 190 , // 626  - qps-latn-x-sh
            0x501  , 0x4e2 , 0x354 , 0x272d, 0x5190, 0xf4  , 1 | DoubleCommaSep    , 627 , 627 , // 627  - qps-ploc
            0x5fe  , 0x3a4 , 0x3a4 , 0x2711, 0x4f42, 0x7a  , 1 | CommaSep          , 628 , 628 , // 628  - qps-ploca
            0x9ff  , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xcd  , 0 | SemicolonSep      , 629 , 143 , // 629  - qps-plocm
            0x86   , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x63  , 1 | CommaSep          , 632 , 632 , // 630  - quc
            0x7c86 , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x63  , 1 | CommaSep          , 632 , 632 , // 631  - quc-latn
            0x486  , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x63  , 1 | CommaSep          , 632 , 632 , // 632  - quc-latn-gt
            0x6b   , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x1a  , 1 | CommaSep          , 634 , 634 , // 633  - quz
            0x46b  , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x1a  , 1 | CommaSep          , 634 , 634 , // 634  - quz-bo
            0x86b  , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0x42  , 1 | CommaSep          , 635 , 635 , // 635  - quz-ec
            0xc6b  , 0x4e4 , 0x352 , 0x2710, 0x4f3c, 0xbb  , 1 | CommaSep          , 636 , 636 , // 636  - quz-pe
            0x17   , 0x4e4 , 0x352 , 0x2710, 0x4f31, 0xdf  , 1 | SemicolonSep      , 638 , 638 , // 637  - rm
            0x417  , 0x4e4 , 0x352 , 0x2710, 0x4f31, 0xdf  , 1 | SemicolonSep      , 638 , 638 , // 638  - rm-ch
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x26  , 1 | SemicolonSep      , 640 , 240 , // 639  - rn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x26  , 1 | SemicolonSep      , 640 , 240 , // 640  - rn-bi
            0x18   , 0x4e2 , 0x354 , 0x272d, 0x5190, 0xc8  , 1 | SemicolonSep      , 643 , 643 , // 641  - ro
            0x818  , 0x4e2 , 0x354 , 0x2   , 0x1f4 , 0x98  , 1 | SemicolonSep      , 642 , 240 , // 642  - ro-md
            0x418  , 0x4e2 , 0x354 , 0x272d, 0x5190, 0xc8  , 1 | SemicolonSep      , 643 , 643 , // 643  - ro-ro
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 645 , 240 , // 644  - rof
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 645 , 240 , // 645  - rof-tz
            0x19   , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xcb  , 1 | SemicolonSep      , 651 , 651 , // 646  - ru
            0x1000 , 0x4e3 , 0x362 , 0x2   , 0x1f4 , 0x1d  , 1 | SemicolonSep      , 647 , 240 , // 647  - ru-by
            0x1000 , 0x4e3 , 0x362 , 0x2   , 0x1f4 , 0x82  , 1 | SemicolonSep      , 648 , 240 , // 648  - ru-kg
            0x1000 , 0x4e3 , 0x362 , 0x2   , 0x1f4 , 0x89  , 1 | SemicolonSep      , 649 , 240 , // 649  - ru-kz
            0x819  , 0x4e3 , 0x362 , 0x2   , 0x1f4 , 0x98  , 1 | SemicolonSep      , 650 , 240 , // 650  - ru-md
            0x419  , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xcb  , 1 | SemicolonSep      , 651 , 651 , // 651  - ru-ru
            0x1000 , 0x4e3 , 0x362 , 0x2   , 0x1f4 , 0xf1  , 1 | SemicolonSep      , 652 , 240 , // 652  - ru-ua
            0x87   , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xcc  , 1 | SemicolonSep      , 654 , 654 , // 653  - rw
            0x487  , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xcc  , 1 | SemicolonSep      , 654 , 654 , // 654  - rw-rw
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 656 , 240 , // 655  - rwk
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 656 , 240 , // 656  - rwk-tz
            0x4f   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 658 , 143 , // 657  - sa
            0x44f  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 658 , 143 , // 658  - sa-in
            0x85   , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xcb  , 1 | SemicolonSep      , 660 , 660 , // 659  - sah
            0x485  , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xcb  , 1 | SemicolonSep      , 660 , 660 , // 660  - sah-ru
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 662 , 240 , // 661  - saq
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 662 , 240 , // 662  - saq-ke
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 664 , 240 , // 663  - sbp
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 664 , 240 , // 664  - sbp-tz
            0x59   , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xbe  , 2 | SemicolonSep      , 667 , 143 , // 665  - sd
            0x7c59 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xbe  , 2 | SemicolonSep      , 667 , 143 , // 666  - sd-arab
            0x859  , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xbe  , 2 | SemicolonSep      , 667 , 143 , // 667  - sd-arab-pk
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 669 , 187 , // 668  - sd-deva
            0x459  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 669 , 187 , // 669  - sd-deva-in
            0x3b   , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0xb1  , 1 | SemicolonSep      , 672 , 672 , // 670  - se
            0xc3b  , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0x4d  , 1 | SemicolonSep      , 671 , 671 , // 671  - se-fi
            0x43b  , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0xb1  , 1 | SemicolonSep      , 672 , 672 , // 672  - se-no
            0x83b  , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0xdd  , 1 | SemicolonSep      , 673 , 673 , // 673  - se-se
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xa8  , 1 | SemicolonSep      , 675 , 240 , // 674  - seh
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xa8  , 1 | SemicolonSep      , 675 , 240 , // 675  - seh-mz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9d  , 1 | SemicolonSep      , 677 , 240 , // 676  - ses
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9d  , 1 | SemicolonSep      , 677 , 240 , // 677  - ses-ml
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x37  , 1 | SemicolonSep      , 679 , 240 , // 678  - sg
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x37  , 1 | SemicolonSep      , 679 , 240 , // 679  - sg-cf
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9f  , 1 | SemicolonSep      , 684 , 240 , // 680  - shi
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9f  , 1 | SemicolonSep      , 682 , 240 , // 681  - shi-latn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9f  , 1 | SemicolonSep      , 682 , 240 , // 682  - shi-latn-ma
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9f  , 1 | SemicolonSep      , 684 , 240 , // 683  - shi-tfng
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9f  , 1 | SemicolonSep      , 684 , 240 , // 684  - shi-tfng-ma
            0x5b   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2a  , 1 | SemicolonSep      , 686 , 143 , // 685  - si
            0x45b  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2a  , 1 | SemicolonSep      , 686 , 143 , // 686  - si-lk
            0x1b   , 0x4e2 , 0x354 , 0x272d, 0x5190, 0x8f  , 1 | SemicolonSep      , 688 , 688 , // 687  - sk
            0x41b  , 0x4e2 , 0x354 , 0x272d, 0x5190, 0x8f  , 1 | SemicolonSep      , 688 , 688 , // 688  - sk-sk
            0x24   , 0x4e2 , 0x354 , 0x272d, 0x5190, 0xd4  , 1 | SemicolonSep      , 690 , 690 , // 689  - sl
            0x424  , 0x4e2 , 0x354 , 0x272d, 0x5190, 0xd4  , 1 | SemicolonSep      , 690 , 690 , // 690  - sl-si
            0x783b , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0xdd  , 1 | SemicolonSep      , 693 , 693 , // 691  - sma
            0x183b , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0xb1  , 1 | SemicolonSep      , 692 , 692 , // 692  - sma-no
            0x1c3b , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0xdd  , 1 | SemicolonSep      , 693 , 693 , // 693  - sma-se
            0x7c3b , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0xdd  , 1 | SemicolonSep      , 696 , 696 , // 694  - smj
            0x103b , 0x4e4 , 0x352 , 0x2710, 0x4f35, 0xb1  , 1 | SemicolonSep      , 695 , 695 , // 695  - smj-no
            0x143b , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0xdd  , 1 | SemicolonSep      , 696 , 696 , // 696  - smj-se
            0x703b , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0x4d  , 1 | SemicolonSep      , 698 , 698 , // 697  - smn
            0x243b , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0x4d  , 1 | SemicolonSep      , 698 , 698 , // 698  - smn-fi
            0x743b , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0x4d  , 1 | SemicolonSep      , 700 , 700 , // 699  - sms
            0x203b , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0x4d  , 1 | SemicolonSep      , 700 , 700 , // 700  - sms-fi
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x108 , 1 | SemicolonSep      , 703 , 240 , // 701  - sn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x108 , 1 | SemicolonSep      , 703 , 240 , // 702  - sn-latn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x108 , 1 | SemicolonSep      , 703 , 240 , // 703  - sn-latn-zw
            0x77   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd8  , 1 | SemicolonSep      , 708 , 240 , // 704  - so
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x3e  , 1 | SemicolonSep      , 705 , 240 , // 705  - so-dj
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x49  , 1 | SemicolonSep      , 706 , 240 , // 706  - so-et
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 707 , 240 , // 707  - so-ke
            0x477  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd8  , 1 | SemicolonSep      , 708 , 240 , // 708  - so-so
            0x1c   , 0x4e2 , 0x354 , 0x272d, 0x5190, 0x6   , 1 | SemicolonSep      , 710 , 710 , // 709  - sq
            0x41c  , 0x4e2 , 0x354 , 0x272d, 0x5190, 0x6   , 1 | SemicolonSep      , 710 , 710 , // 710  - sq-al
            0x1000 , 0x4e2 , 0x354 , 0x272d, 0x5190, 0x4ca2, 1 | SemicolonSep      , 711 , 240 , // 711  - sq-mk
            0x1000 , 0x4e2 , 0x354 , 0x272d, 0x5190, 0x974941, 1 | SemicolonSep    , 712 , 240 , // 712  - sq-xk
            0x7c1a , 0x4e2 , 0x354 , 0x272d, 0x1f4 , 0x10f , 1 | SemicolonSep      , 724 , 724 , // 713  - sr
            0x6c1a , 0x4e3 , 0x357 , 0x2717, 0x5221, 0x10f , 1 | SemicolonSep      , 718 , 718 , // 714  - sr-cyrl
            0x1c1a , 0x4e3 , 0x357 , 0x2717, 0x5221, 0x19  , 1 | SemicolonSep      , 715 , 715 , // 715  - sr-cyrl-ba
            0xc1a  , 0x4e3 , 0x357 , 0x2717, 0x5221, 0x10d , 1 | SemicolonSep      , 716 , 716 , // 716  - sr-cyrl-cs
            0x301a , 0x4e3 , 0x357 , 0x2717, 0x5221, 0x10e , 1 | SemicolonSep      , 717 , 717 , // 717  - sr-cyrl-me
            0x281a , 0x4e3 , 0x357 , 0x2717, 0x5221, 0x10f , 1 | SemicolonSep      , 718 , 718 , // 718  - sr-cyrl-rs
            0x1000 , 0x4e3 , 0x357 , 0x2717, 0x5221, 0x974941, 1 | SemicolonSep    , 719 , 240 , // 719  - sr-cyrl-xk
            0x701a , 0x4e2 , 0x354 , 0x272d, 0x1f4 , 0x10f , 1 | SemicolonSep      , 724 , 724 , // 720  - sr-latn
            0x181a , 0x4e2 , 0x354 , 0x2762, 0x366 , 0x19  , 1 | SemicolonSep      , 721 , 721 , // 721  - sr-latn-ba
            0x81a  , 0x4e2 , 0x354 , 0x272d, 0x1f4 , 0x10d , 1 | SemicolonSep      , 722 , 722 , // 722  - sr-latn-cs
            0x2c1a , 0x4e2 , 0x354 , 0x272d, 0x1f4 , 0x10e , 1 | SemicolonSep      , 723 , 723 , // 723  - sr-latn-me
            0x241a , 0x4e2 , 0x354 , 0x272d, 0x1f4 , 0x10f , 1 | SemicolonSep      , 724 , 724 , // 724  - sr-latn-rs
            0x1000 , 0x4e2 , 0x354 , 0x272d, 0x1f4 , 0x974941, 1 | SemicolonSep    , 725 , 240 , // 725  - sr-latn-xk
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd1  , 1 | SemicolonSep      , 728 , 240 , // 726  - ss
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x104 , 1 | SemicolonSep      , 727 , 240 , // 727  - ss-sz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd1  , 1 | SemicolonSep      , 728 , 240 , // 728  - ss-za
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x47  , 1 | SemicolonSep      , 730 , 240 , // 729  - ssy
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x47  , 1 | SemicolonSep      , 730 , 240 , // 730  - ssy-er
            0x30   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd1  , 1 | SemicolonSep      , 733 , 240 , // 731  - st
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x92  , 1 | SemicolonSep      , 732 , 240 , // 732  - st-ls
            0x430  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd1  , 1 | SemicolonSep      , 733 , 240 , // 733  - st-za
            0x1d   , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0xdd  , 1 | SemicolonSep      , 737 , 737 , // 734  - sv
            0x1000 , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0x9906f5, 1 | SemicolonSep    , 735 , 240 , // 735  - sv-ax
            0x81d  , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0x4d  , 1 | SemicolonSep      , 736 , 736 , // 736  - sv-fi
            0x41d  , 0x4e4 , 0x352 , 0x2710, 0x4f36, 0xdd  , 1 | SemicolonSep      , 737 , 737 , // 737  - sv-se
            0x41   , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0x81  , 1 | SemicolonSep      , 740 , 740 , // 738  - sw
            0x1000 , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0x2c  , 1 | SemicolonSep      , 739 , 740 , // 739  - sw-cd
            0x441  , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0x81  , 1 | SemicolonSep      , 740 , 740 , // 740  - sw-ke
            0x1000 , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0xef  , 1 | SemicolonSep      , 741 , 240 , // 741  - sw-tz
            0x1000 , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0xf0  , 1 | SemicolonSep      , 742 , 240 , // 742  - sw-ug
            0x1000 , 0x0   , 0x1   , 0x0   , 0x1f4 , 0x2c  , 1 | CommaSep          , 744 , 240 , // 743  - swc
            0x1000 , 0x0   , 0x1   , 0x0   , 0x1f4 , 0x2c  , 1 | SemicolonSep      , 744 , 240 , // 744  - swc-cd
            0x5a   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xde  , 1 | CommaSep          , 746 , 143 , // 745  - syr
            0x45a  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xde  , 1 | CommaSep          , 746 , 143 , // 746  - syr-sy
            0x49   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 748 , 143 , // 747  - ta
            0x449  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | CommaSep          , 748 , 143 , // 748  - ta-in
            0x849  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x2a  , 1 | SemicolonSep      , 749 , 143 , // 749  - ta-lk
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xa7  , 1 | SemicolonSep      , 750 , 240 , // 750  - ta-my
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd7  , 1 | SemicolonSep      , 751 , 240 , // 751  - ta-sg
            0x4a   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | SemicolonSep      , 753 , 143 , // 752  - te
            0x44a  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x71  , 1 | SemicolonSep      , 753 , 143 , // 753  - te-in
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf0  , 1 | SemicolonSep      , 756 , 240 , // 754  - teo
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x81  , 1 | SemicolonSep      , 755 , 240 , // 755  - teo-ke
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf0  , 1 | SemicolonSep      , 756 , 240 , // 756  - teo-ug
            0x28   , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xe4  , 1 | SemicolonSep      , 759 , 759 , // 757  - tg
            0x7c28 , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xe4  , 1 | SemicolonSep      , 759 , 759 , // 758  - tg-cyrl
            0x428  , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xe4  , 1 | SemicolonSep      , 759 , 759 , // 759  - tg-cyrl-tj
            0x1e   , 0x36a , 0x36a , 0x2725, 0x5166, 0xe3  , 1 | CommaSep          , 761 , 143 , // 760  - th
            0x41e  , 0x36a , 0x36a , 0x2725, 0x5166, 0xe3  , 1 | CommaSep          , 761 , 143 , // 761  - th-th
            0x73   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x47  , 1 | SemicolonSep      , 763 , 143 , // 762  - ti
            0x873  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x47  , 1 | SemicolonSep      , 763 , 143 , // 763  - ti-er
            0x473  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x49  , 1 | SemicolonSep      , 764 , 143 , // 764  - ti-et
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x47  , 1 | SemicolonSep      , 766 , 240 , // 765  - tig
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x47  , 1 | SemicolonSep      , 766 , 240 , // 766  - tig-er
            0x42   , 0x4e2 , 0x354 , 0x272d, 0x5190, 0xee  , 1 | SemicolonSep      , 768 , 768 , // 767  - tk
            0x442  , 0x4e2 , 0x354 , 0x272d, 0x5190, 0xee  , 1 | SemicolonSep      , 768 , 768 , // 768  - tk-tm
            0x32   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd1  , 1 | SemicolonSep      , 771 , 771 , // 769  - tn
            0x832  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0x13  , 1 | SemicolonSep      , 770 , 770 , // 770  - tn-bw
            0x432  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd1  , 1 | SemicolonSep      , 771 , 771 , // 771  - tn-za
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xe7  , 1 | SemicolonSep      , 773 , 240 , // 772  - to
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xe7  , 1 | SemicolonSep      , 773 , 240 , // 773  - to-to
            0x1f   , 0x4e6 , 0x359 , 0x2761, 0x51a9, 0xeb  , 1 | SemicolonSep      , 776 , 776 , // 774  - tr
            0x1000 , 0x4e6 , 0x359 , 0x2761, 0x51a9, 0x3b  , 1 | SemicolonSep      , 775 , 240 , // 775  - tr-cy
            0x41f  , 0x4e6 , 0x359 , 0x2761, 0x51a9, 0xeb  , 1 | SemicolonSep      , 776 , 776 , // 776  - tr-tr
            0x31   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd1  , 1 | SemicolonSep      , 778 , 240 , // 777  - ts
            0x431  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd1  , 1 | SemicolonSep      , 778 , 240 , // 778  - ts-za
            0x44   , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xcb  , 1 | SemicolonSep      , 780 , 780 , // 779  - tt
            0x444  , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xcb  , 1 | SemicolonSep      , 780 , 780 , // 780  - tt-ru
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xad  , 1 | SemicolonSep      , 782 , 240 , // 781  - twq
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xad  , 1 | SemicolonSep      , 782 , 240 , // 782  - twq-ne
            0x5f   , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x4   , 1 | SemicolonSep      , 787 , 787 , // 783  - tzm
            0x1000 , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x9f  , 1 | SemicolonSep      , 785 , 240 , // 784  - tzm-arab
            0x45f  , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x9f  , 1 | SemicolonSep      , 785 , 240 , // 785  - tzm-arab-ma
            0x7c5f , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x4   , 1 | SemicolonSep      , 787 , 787 , // 786  - tzm-latn
            0x85f  , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0x4   , 1 | SemicolonSep      , 787 , 787 , // 787  - tzm-latn-dz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9f  , 1 | SemicolonSep      , 788 , 240 , // 788  - tzm-latn-ma
            0x785f , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9f  , 1 | SemicolonSep      , 790 , 316 , // 789  - tzm-tfng
            0x105f , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9f  , 1 | SemicolonSep      , 790 , 316 , // 790  - tzm-tfng-ma
            0x80   , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x2d  , 1 | CommaSep          , 792 , 143 , // 791  - ug
            0x480  , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0x2d  , 1 | CommaSep          , 792 , 143 , // 792  - ug-cn
            0x22   , 0x4e3 , 0x362 , 0x2721, 0x1f4 , 0xf1  , 1 | SemicolonSep      , 794 , 794 , // 793  - uk
            0x422  , 0x4e3 , 0x362 , 0x2721, 0x1f4 , 0xf1  , 1 | SemicolonSep      , 794 , 794 , // 794  - uk-ua
            0x20   , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xbe  , 1 | SemicolonSep      , 797 , 143 , // 795  - ur
            0x820  , 0x4e8 , 0x2d0 , 0x2   , 0x1f4 , 0x71  , 2 | SemicolonSep      , 796 , 240 , // 796  - ur-in
            0x420  , 0x4e8 , 0x2d0 , 0x2714, 0x4fc4, 0xbe  , 1 | SemicolonSep      , 797 , 143 , // 797  - ur-pk
            0x43   , 0x4e6 , 0x359 , 0x272d, 0x1f4 , 0xf7  , 1 | SemicolonSep      , 804 , 804 , // 798  - uz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x3   , 2 | SemicolonSep      , 800 , 240 , // 799  - uz-arab
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x3   , 2 | SemicolonSep      , 800 , 240 , // 800  - uz-arab-af
            0x7843 , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xf7  , 1 | SemicolonSep      , 802 , 802 , // 801  - uz-cyrl
            0x843  , 0x4e3 , 0x362 , 0x2717, 0x5190, 0xf7  , 1 | SemicolonSep      , 802 , 802 , // 802  - uz-cyrl-uz
            0x7c43 , 0x4e6 , 0x359 , 0x272d, 0x1f4 , 0xf7  , 1 | SemicolonSep      , 804 , 804 , // 803  - uz-latn
            0x443  , 0x4e6 , 0x359 , 0x272d, 0x1f4 , 0xf7  , 1 | SemicolonSep      , 804 , 804 , // 804  - uz-latn-uz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x8e  , 1 | SemicolonSep      , 809 , 240 , // 805  - vai
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x8e  , 1 | SemicolonSep      , 807 , 240 , // 806  - vai-latn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x8e  , 1 | SemicolonSep      , 807 , 240 , // 807  - vai-latn-lr
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x8e  , 1 | SemicolonSep      , 809 , 240 , // 808  - vai-vaii
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x8e  , 1 | SemicolonSep      , 809 , 240 , // 809  - vai-vaii-lr
            0x33   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd1  , 1 | SemicolonSep      , 811 , 240 , // 810  - ve
            0x433  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xd1  , 1 | SemicolonSep      , 811 , 240 , // 811  - ve-za
            0x2a   , 0x4ea , 0x4ea , 0x2710, 0x1f4 , 0xfb  , 1 | CommaSep          , 813 , 143 , // 812  - vi
            0x42a  , 0x4ea , 0x4ea , 0x2710, 0x1f4 , 0xfb  , 1 | CommaSep          , 813 , 143 , // 813  - vi-vn
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x989e, 1 | SemicolonSep      , 815 , 240 , // 814  - vo
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x989e, 1 | SemicolonSep      , 815 , 240 , // 815  - vo-001
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 817 , 240 , // 816  - vun
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xef  , 1 | SemicolonSep      , 817 , 240 , // 817  - vun-tz
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xdf  , 1 | SemicolonSep      , 819 , 240 , // 818  - wae
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xdf  , 1 | SemicolonSep      , 819 , 240 , // 819  - wae-ch
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x49  , 1 | SemicolonSep      , 821 , 240 , // 820  - wal
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x49  , 1 | SemicolonSep      , 821 , 240 , // 821  - wal-et
            0x88   , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xd2  , 1 | SemicolonSep      , 823 , 823 , // 822  - wo
            0x488  , 0x4e4 , 0x352 , 0x2710, 0x4f49, 0xd2  , 1 | SemicolonSep      , 823 , 823 , // 823  - wo-sn
            0x1007f, 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xf4  , 1 | CommaSep          , -1  , -1  , // 824  - x-iv_mathan
            0x34   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd1  , 1 | SemicolonSep      , 826 , 826 , // 825  - xh
            0x434  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd1  , 1 | SemicolonSep      , 826 , 826 , // 826  - xh-za
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf0  , 1 | SemicolonSep      , 828 , 240 , // 827  - xog
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0xf0  , 1 | SemicolonSep      , 828 , 240 , // 828  - xog-ug
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 830 , 240 , // 829  - yav
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x31  , 1 | SemicolonSep      , 830 , 240 , // 830  - yav-cm
            0x3d   , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x989e, 1 | SemicolonSep      , 832 , 240 , // 831  - yi
            0x43d  , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x989e, 1 | SemicolonSep      , 832 , 240 , // 832  - yi-001
            0x6a   , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xaf  , 1 | SemicolonSep      , 835 , 835 , // 833  - yo
            0x1000 , 0x4e4 , 0x1b5 , 0x2710, 0x1f4 , 0x1c  , 1 | SemicolonSep      , 834 , 240 , // 834  - yo-bj
            0x46a  , 0x4e4 , 0x1b5 , 0x2710, 0x25  , 0xaf  , 1 | SemicolonSep      , 835 , 835 , // 835  - yo-ng
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x68  , 1 | CommaSep          , 837 , 240 , // 836  - yue
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x68  , 1 | CommaSep          , 837 , 240 , // 837  - yue-hk
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9f  , 1 | SemicolonSep      , 840 , 316 , // 838  - zgh
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9f  , 1 | SemicolonSep      , 840 , 316 , // 839  - zgh-tfng
            0x1000 , 0x0   , 0x1   , 0x2   , 0x1f4 , 0x9f  , 1 | SemicolonSep      , 840 , 316 , // 840  - zgh-tfng-ma
            0x7804 , 0x3a8 , 0x3a8 , 0x2718, 0x1f4 , 0x2d  , 1 | CommaSep          , 844 , 844 , // 841  - zh
            0x4    , 0x3a8 , 0x3a8 , 0x0   , 0x1f4 , 0x2d  , 1 | CommaSep          , 844 , 844 , // 842  - zh-chs
            0x7c04 , 0x3b6 , 0x3b6 , 0x0   , 0x1f4 , 0x68  , 1 | CommaSep          , 851 , 851 , // 843  - zh-cht
            0x804  , 0x3a8 , 0x3a8 , 0x2718, 0x1f4 , 0x2d  , 1 | CommaSep          , 844 , 844 , // 844  - zh-cn
            0x50804, 0x3a8 , 0x3a8 , 0x2718, 0x1f4 , 0x2d  , 1 | CommaSep          , 844 , 844 , // 845  - zh-cn_phoneb
            0x20804, 0x3a8 , 0x3a8 , 0x2718, 0x1f4 , 0x2d  , 1 | CommaSep          , 844 , 844 , // 846  - zh-cn_stroke
            0x4    , 0x3a8 , 0x3a8 , 0x2718, 0x1f4 , 0x2d  , 1 | CommaSep          , 844 , 844 , // 847  - zh-hans
            0x1000 , 0x3a8 , 0x3a8 , 0x2718, 0x1f4 , 0x68  , 1 | SemicolonSep      , 848 , 240 , // 848  - zh-hans-hk
            0x1000 , 0x3a8 , 0x3a8 , 0x2718, 0x1f4 , 0x97  , 1 | SemicolonSep      , 849 , 240 , // 849  - zh-hans-mo
            0x7c04 , 0x3b6 , 0x3b6 , 0x2712, 0x1f4 , 0x68  , 1 | CommaSep          , 851 , 851 , // 850  - zh-hant
            0xc04  , 0x3b6 , 0x3b6 , 0x2712, 0x1f4 , 0x68  , 1 | CommaSep          , 851 , 851 , // 851  - zh-hk
            0x40c04, 0x3b6 , 0x3b6 , 0x2712, 0x1f4 , 0x68  , 1 | CommaSep          , 851 , 851 , // 852  - zh-hk_radstr
            0x1404 , 0x3b6 , 0x3b6 , 0x2712, 0x1f4 , 0x97  , 1 | CommaSep          , 853 , 853 , // 853  - zh-mo
            0x41404, 0x3b6 , 0x3b6 , 0x2712, 0x1f4 , 0x97  , 1 | CommaSep          , 853 , 853 , // 854  - zh-mo_radstr
            0x21404, 0x3b6 , 0x3b6 , 0x2712, 0x1f4 , 0x97  , 1 | CommaSep          , 853 , 853 , // 855  - zh-mo_stroke
            0x1004 , 0x3a8 , 0x3a8 , 0x2718, 0x1f4 , 0xd7  , 1 | CommaSep          , 856 , 856 , // 856  - zh-sg
            0x51004, 0x3a8 , 0x3a8 , 0x2718, 0x1f4 , 0xd7  , 1 | CommaSep          , 856 , 856 , // 857  - zh-sg_phoneb
            0x21004, 0x3a8 , 0x3a8 , 0x2718, 0x1f4 , 0xd7  , 1 | CommaSep          , 856 , 856 , // 858  - zh-sg_stroke
            0x404  , 0x3b6 , 0x3b6 , 0x2712, 0x1f4 , 0xed  , 1 | CommaSep          , 859 , 859 , // 859  - zh-tw
            0x30404, 0x3b6 , 0x3b6 , 0x2712, 0x1f4 , 0xed  , 1 | CommaSep          , 859 , 859 , // 860  - zh-tw_pronun
            0x40404, 0x3b6 , 0x3b6 , 0x2712, 0x1f4 , 0xed  , 1 | CommaSep          , 859 , 859 , // 861  - zh-tw_radstr
            0x35   , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd1  , 1 | SemicolonSep      , 863 , 863 , // 862  - zu
            0x435  , 0x4e4 , 0x352 , 0x2710, 0x1f4 , 0xd1  , 1 | SemicolonSep      , 863 , 863 , // 863  - zu-za
        };

        // Format of the data is LCDI | index to CultureNames | culture name length
        private static readonly int[] s_lcidToCultureNameIndices = new int[]
        {
            0x1 << 16 | 41 << 4 | 2,         // ar
            0x2 << 16 | 248 << 4 | 2,        // bg
            0x3 << 16 | 326 << 4 | 2,        // ca
            0x4 << 16 | 3272 << 4 | 7,       // zh-hans
            0x5 << 16 | 382 << 4 | 2,        // cs
            0x6 << 16 | 397 << 4 | 2,        // da
            0x7 << 16 | 413 << 4 | 2,        // de
            0x8 << 16 | 505 << 4 | 2,        // el
            0x9 << 16 | 515 << 4 | 2,        // en
            0xa << 16 | 1049 << 4 | 2,       // es
            0xb << 16 | 1233 << 4 | 2,       // fi
            0xc << 16 | 1254 << 4 | 2,       // fr
            0xd << 16 | 1591 << 4 | 2,       // he
            0xe << 16 | 1617 << 4 | 2,       // hu
            0xf << 16 | 1666 << 4 | 2,       // is
            0x10 << 16 | 1671 << 4 | 2,      // it
            0x11 << 16 | 1706 << 4 | 2,      // ja
            0x12 << 16 | 1829 << 4 | 2,      // ko
            0x13 << 16 | 2195 << 4 | 2,      // nl
            0x14 << 16 | 2247 << 4 | 2,      // no
            0x15 << 16 | 2330 << 4 | 2,      // pl
            0x16 << 16 | 2353 << 4 | 2,      // pt
            0x17 << 16 | 2473 << 4 | 2,      // rm
            0x18 << 16 | 2483 << 4 | 2,      // ro
            0x19 << 16 | 2499 << 4 | 2,      // ru
            0x1a << 16 | 1601 << 4 | 2,      // hr
            0x1b << 16 | 2642 << 4 | 2,      // sk
            0x1c << 16 | 2718 << 4 | 2,      // sq
            0x1d << 16 | 2859 << 4 | 2,      // sv
            0x1e << 16 | 2953 << 4 | 2,      // th
            0x1f << 16 | 2994 << 4 | 2,      // tr
            0x20 << 16 | 3074 << 4 | 2,      // ur
            0x21 << 16 | 1651 << 4 | 2,      // id
            0x22 << 16 | 3069 << 4 | 2,      // uk
            0x23 << 16 | 231 << 4 | 2,       // be
            0x24 << 16 | 2647 << 4 | 2,      // sl
            0x25 << 16 | 1182 << 4 | 2,      // et
            0x26 << 16 | 2005 << 4 | 2,      // lv
            0x27 << 16 | 1983 << 4 | 2,      // lt
            0x28 << 16 | 2943 << 4 | 2,      // tg
            0x29 << 16 | 1198 << 4 | 2,      // fa
            0x2a << 16 | 3141 << 4 | 2,      // vi
            0x2b << 16 | 1629 << 4 | 2,      // hy
            0x2c << 16 | 200 << 4 | 2,       // az
            0x2d << 16 | 1187 << 4 | 2,      // eu
            0x2e << 16 | 1611 << 4 | 3,      // hsb
            0x2f << 16 | 2056 << 4 | 2,      // mk
            0x30 << 16 | 2849 << 4 | 2,      // st
            0x31 << 16 | 3004 << 4 | 2,      // ts
            0x32 << 16 | 2979 << 4 | 2,      // tn
            0x33 << 16 | 3136 << 4 | 2,      // ve
            0x34 << 16 | 3186 << 4 | 2,      // xh
            0x35 << 16 | 3383 << 4 | 2,      // zu
            0x36 << 16 | 15 << 4 | 2,        // af
            0x37 << 16 | 1750 << 4 | 2,      // ka
            0x38 << 16 | 1244 << 4 | 2,      // fo
            0x39 << 16 | 1596 << 4 | 2,      // hi
            0x3a << 16 | 2130 << 4 | 2,      // mt
            0x3b << 16 | 2583 << 4 | 2,      // se
            0x3c << 16 | 1501 << 4 | 2,      // ga
            0x3d << 16 | 3203 << 4 | 2,      // yi
            0x3e << 16 | 2115 << 4 | 2,      // ms
            0x3f << 16 | 1797 << 4 | 2,      // kk
            0x40 << 16 | 1913 << 4 | 2,      // ky
            0x41 << 16 | 2874 << 4 | 2,      // sw
            0x42 << 16 | 2974 << 4 | 2,      // tk
            0x43 << 16 | 3084 << 4 | 2,      // uz
            0x44 << 16 | 3009 << 4 | 2,      // tt
            0x45 << 16 | 269 << 4 | 2,       // bn
            0x46 << 16 | 2308 << 4 | 2,      // pa
            0x47 << 16 | 1539 << 4 | 2,      // gu
            0x48 << 16 | 2293 << 4 | 2,      // or
            0x49 << 16 | 2906 << 4 | 2,      // ta
            0x4a << 16 | 2926 << 4 | 2,      // te
            0x4b << 16 | 1824 << 4 | 2,      // kn
            0x4c << 16 | 2061 << 4 | 2,      // ml
            0x4d << 16 | 183 << 4 | 2,       // as
            0x4e << 16 | 2110 << 4 | 2,      // mr
            0x4f << 16 | 2540 << 4 | 2,      // sa
            0x50 << 16 | 2066 << 4 | 2,      // mn
            0x51 << 16 | 279 << 4 | 2,       // bo
            0x52 << 16 | 392 << 4 | 2,       // cy
            0x53 << 16 | 1819 << 4 | 2,      // km
            0x54 << 16 | 1966 << 4 | 2,      // lo
            0x55 << 16 | 2141 << 4 | 2,      // my
            0x56 << 16 | 1511 << 4 | 2,      // gl
            0x57 << 16 | 1839 << 4 | 3,      // kok
            0x58 << 16 | 2098 << 4 | 3,      // mni
            0x59 << 16 | 2563 << 4 | 2,      // sd
            0x5a << 16 | 2900 << 4 | 3,      // syr
            0x5b << 16 | 2637 << 4 | 2,      // si
            0x5c << 16 | 366 << 4 | 3,       // chr
            0x5d << 16 | 1686 << 4 | 2,      // iu
            0x5e << 16 | 36 << 4 | 2,        // am
            0x5f << 16 | 3020 << 4 | 3,      // tzm
            0x60 << 16 | 1850 << 4 | 2,      // ks
            0x61 << 16 | 2185 << 4 | 2,      // ne
            0x62 << 16 | 1496 << 4 | 2,      // fy
            0x63 << 16 | 2348 << 4 | 2,      // ps
            0x64 << 16 | 1238 << 4 | 3,      // fil
            0x65 << 16 | 473 << 4 | 2,       // dv
            0x66 << 16 | 253 << 4 | 3,       // bin
            0x67 << 16 | 1203 << 4 | 2,      // ff
            0x68 << 16 | 1555 << 4 | 2,      // ha
            0x69 << 16 | 1645 << 4 | 3,      // ibb
            0x6a << 16 | 3209 << 4 | 2,      // yo
            0x6b << 16 | 2455 << 4 | 3,      // quz
            0x6c << 16 | 2260 << 4 | 3,      // nso
            0x6d << 16 | 220 << 4 | 2,       // ba
            0x6e << 16 | 1930 << 4 | 2,      // lb
            0x6f << 16 | 1808 << 4 | 2,      // kl
            0x70 << 16 | 1656 << 4 | 2,      // ig
            0x71 << 16 | 1845 << 4 | 2,      // kr
            0x72 << 16 | 2283 << 4 | 2,      // om
            0x73 << 16 | 2958 << 4 | 2,      // ti
            0x74 << 16 | 1516 << 4 | 2,      // gn
            0x75 << 16 | 1585 << 4 | 3,      // haw
            0x76 << 16 | 1918 << 4 | 2,      // la
            0x77 << 16 | 2698 << 4 | 2,      // so
            0x78 << 16 | 1661 << 4 | 2,      // ii
            0x79 << 16 | 2323 << 4 | 3,      // pap
            0x7a << 16 | 177 << 4 | 3,       // arn
            0x7c << 16 | 2104 << 4 | 3,      // moh
            0x7e << 16 | 289 << 4 | 2,       // br
            0x80 << 16 | 3064 << 4 | 2,      // ug
            0x81 << 16 | 2051 << 4 | 2,      // mi
            0x82 << 16 | 2278 << 4 | 2,      // oc
            0x83 << 16 | 377 << 4 | 2,       // co
            0x84 << 16 | 1521 << 4 | 3,      // gsw
            0x85 << 16 | 2545 << 4 | 3,      // sah
            0x86 << 16 | 2444 << 4 | 3,      // quc
            0x87 << 16 | 2529 << 4 | 2,      // rw
            0x88 << 16 | 3170 << 4 | 2,      // wo
            0x8c << 16 | 2342 << 4 | 3,      // prs
            0x91 << 16 | 1506 << 4 | 2,      // gd
            0x92 << 16 | 1888 << 4 | 2,      // ku
            0x401 << 16 | 137 << 4 | 5,      // ar-sa
            0x402 << 16 | 248 << 4 | 5,      // bg-bg
            0x403 << 16 | 331 << 4 | 5,      // ca-es
            0x404 << 16 | 3359 << 4 | 5,     // zh-tw
            0x405 << 16 | 382 << 4 | 5,      // cs-cz
            0x406 << 16 | 397 << 4 | 5,      // da-dk
            0x407 << 16 | 428 << 4 | 5,      // de-de
            0x408 << 16 | 510 << 4 | 5,      // el-gr
            0x409 << 16 | 998 << 4 | 5,      // en-us
            0x40a << 16 | 1100 << 4 | 5,     // es-es
            0x40b << 16 | 1233 << 4 | 5,     // fi-fi
            0x40c << 16 | 1330 << 4 | 5,     // fr-fr
            0x40d << 16 | 1591 << 4 | 5,     // he-il
            0x40e << 16 | 1617 << 4 | 5,     // hu-hu
            0x40f << 16 | 1666 << 4 | 5,     // is-is
            0x410 << 16 | 1676 << 4 | 5,     // it-it
            0x411 << 16 | 1706 << 4 | 5,     // ja-jp
            0x412 << 16 | 1834 << 4 | 5,     // ko-kr
            0x413 << 16 | 2215 << 4 | 5,     // nl-nl
            0x414 << 16 | 2158 << 4 | 5,     // nb-no
            0x415 << 16 | 2330 << 4 | 5,     // pl-pl
            0x416 << 16 | 2358 << 4 | 5,     // pt-br
            0x417 << 16 | 2473 << 4 | 5,     // rm-ch
            0x418 << 16 | 2488 << 4 | 5,     // ro-ro
            0x419 << 16 | 2519 << 4 | 5,     // ru-ru
            0x41a << 16 | 1606 << 4 | 5,     // hr-hr
            0x41b << 16 | 2642 << 4 | 5,     // sk-sk
            0x41c << 16 | 2718 << 4 | 5,     // sq-al
            0x41d << 16 | 2869 << 4 | 5,     // sv-se
            0x41e << 16 | 2953 << 4 | 5,     // th-th
            0x41f << 16 | 2999 << 4 | 5,     // tr-tr
            0x420 << 16 | 3079 << 4 | 5,     // ur-pk
            0x421 << 16 | 1651 << 4 | 5,     // id-id
            0x422 << 16 | 3069 << 4 | 5,     // uk-ua
            0x423 << 16 | 231 << 4 | 5,      // be-by
            0x424 << 16 | 2647 << 4 | 5,     // sl-si
            0x425 << 16 | 1182 << 4 | 5,     // et-ee
            0x426 << 16 | 2005 << 4 | 5,     // lv-lv
            0x427 << 16 | 1983 << 4 | 5,     // lt-lt
            0x428 << 16 | 2943 << 4 | 10,    // tg-cyrl-tj
            0x429 << 16 | 1198 << 4 | 5,     // fa-ir
            0x42a << 16 | 3141 << 4 | 5,     // vi-vn
            0x42b << 16 | 1629 << 4 | 5,     // hy-am
            0x42c << 16 | 210 << 4 | 10,     // az-latn-az
            0x42d << 16 | 1187 << 4 | 5,     // eu-es
            0x42e << 16 | 1611 << 4 | 6,     // hsb-de
            0x42f << 16 | 2056 << 4 | 5,     // mk-mk
            0x430 << 16 | 2854 << 4 | 5,     // st-za
            0x431 << 16 | 3004 << 4 | 5,     // ts-za
            0x432 << 16 | 2984 << 4 | 5,     // tn-za
            0x433 << 16 | 3136 << 4 | 5,     // ve-za
            0x434 << 16 | 3186 << 4 | 5,     // xh-za
            0x435 << 16 | 3383 << 4 | 5,     // zu-za
            0x436 << 16 | 20 << 4 | 5,       // af-za
            0x437 << 16 | 1750 << 4 | 5,     // ka-ge
            0x438 << 16 | 1249 << 4 | 5,     // fo-fo
            0x439 << 16 | 1596 << 4 | 5,     // hi-in
            0x43a << 16 | 2130 << 4 | 5,     // mt-mt
            0x43b << 16 | 2588 << 4 | 5,     // se-no
            0x43d << 16 | 3203 << 4 | 6,     // yi-001
            0x43e << 16 | 2120 << 4 | 5,     // ms-my
            0x43f << 16 | 1797 << 4 | 5,     // kk-kz
            0x440 << 16 | 1913 << 4 | 5,     // ky-kg
            0x441 << 16 | 2879 << 4 | 5,     // sw-ke
            0x442 << 16 | 2974 << 4 | 5,     // tk-tm
            0x443 << 16 | 3104 << 4 | 10,    // uz-latn-uz
            0x444 << 16 | 3009 << 4 | 5,     // tt-ru
            0x445 << 16 | 274 << 4 | 5,      // bn-in
            0x446 << 16 | 2318 << 4 | 5,     // pa-in
            0x447 << 16 | 1539 << 4 | 5,     // gu-in
            0x448 << 16 | 2293 << 4 | 5,     // or-in
            0x449 << 16 | 2906 << 4 | 5,     // ta-in
            0x44a << 16 | 2926 << 4 | 5,     // te-in
            0x44b << 16 | 1824 << 4 | 5,     // kn-in
            0x44c << 16 | 2061 << 4 | 5,     // ml-in
            0x44d << 16 | 183 << 4 | 5,      // as-in
            0x44e << 16 | 2110 << 4 | 5,     // mr-in
            0x44f << 16 | 2540 << 4 | 5,     // sa-in
            0x450 << 16 | 2073 << 4 | 5,     // mn-mn
            0x451 << 16 | 279 << 4 | 5,      // bo-cn
            0x452 << 16 | 392 << 4 | 5,      // cy-gb
            0x453 << 16 | 1819 << 4 | 5,     // km-kh
            0x454 << 16 | 1966 << 4 | 5,     // lo-la
            0x455 << 16 | 2141 << 4 | 5,     // my-mm
            0x456 << 16 | 1511 << 4 | 5,     // gl-es
            0x457 << 16 | 1839 << 4 | 6,     // kok-in
            0x458 << 16 | 2098 << 4 | 6,     // mni-in
            0x459 << 16 | 2573 << 4 | 10,    // sd-deva-in
            0x45a << 16 | 2900 << 4 | 6,     // syr-sy
            0x45b << 16 | 2637 << 4 | 5,     // si-lk
            0x45c << 16 | 366 << 4 | 11,     // chr-cher-us
            0x45d << 16 | 1686 << 4 | 10,    // iu-cans-ca
            0x45e << 16 | 36 << 4 | 5,       // am-et
            0x45f << 16 | 3020 << 4 | 11,    // tzm-arab-ma
            0x460 << 16 | 1850 << 4 | 7,     // ks-arab
            0x461 << 16 | 2190 << 4 | 5,     // ne-np
            0x462 << 16 | 1496 << 4 | 5,     // fy-nl
            0x463 << 16 | 2348 << 4 | 5,     // ps-af
            0x464 << 16 | 1238 << 4 | 6,     // fil-ph
            0x465 << 16 | 473 << 4 | 5,      // dv-mv
            0x466 << 16 | 253 << 4 | 6,      // bin-ng
            0x467 << 16 | 1228 << 4 | 5,     // ff-ng
            0x468 << 16 | 1575 << 4 | 10,    // ha-latn-ng
            0x469 << 16 | 1645 << 4 | 6,     // ibb-ng
            0x46a << 16 | 3214 << 4 | 5,     // yo-ng
            0x46b << 16 | 2455 << 4 | 6,     // quz-bo
            0x46c << 16 | 2260 << 4 | 6,     // nso-za
            0x46d << 16 | 220 << 4 | 5,      // ba-ru
            0x46e << 16 | 1930 << 4 | 5,     // lb-lu
            0x46f << 16 | 1808 << 4 | 5,     // kl-gl
            0x470 << 16 | 1656 << 4 | 5,     // ig-ng
            0x471 << 16 | 1845 << 4 | 5,     // kr-ng
            0x472 << 16 | 2283 << 4 | 5,     // om-et
            0x473 << 16 | 2963 << 4 | 5,     // ti-et
            0x474 << 16 | 1516 << 4 | 5,     // gn-py
            0x475 << 16 | 1585 << 4 | 6,     // haw-us
            0x476 << 16 | 1918 << 4 | 6,     // la-001
            0x477 << 16 | 2713 << 4 | 5,     // so-so
            0x478 << 16 | 1661 << 4 | 5,     // ii-cn
            0x479 << 16 | 2323 << 4 | 7,     // pap-029
            0x47a << 16 | 177 << 4 | 6,      // arn-cl
            0x47c << 16 | 2104 << 4 | 6,     // moh-ca
            0x47e << 16 | 289 << 4 | 5,      // br-fr
            0x480 << 16 | 3064 << 4 | 5,     // ug-cn
            0x481 << 16 | 2051 << 4 | 5,     // mi-nz
            0x482 << 16 | 2278 << 4 | 5,     // oc-fr
            0x483 << 16 | 377 << 4 | 5,      // co-fr
            0x484 << 16 | 1527 << 4 | 6,     // gsw-fr
            0x485 << 16 | 2545 << 4 | 6,     // sah-ru
            0x486 << 16 | 2444 << 4 | 11,    // quc-latn-gt
            0x487 << 16 | 2529 << 4 | 5,     // rw-rw
            0x488 << 16 | 3170 << 4 | 5,     // wo-sn
            0x48c << 16 | 2342 << 4 | 6,     // prs-af
            0x491 << 16 | 1506 << 4 | 5,     // gd-gb
            0x492 << 16 | 1888 << 4 | 10,    // ku-arab-iq
            0x501 << 16 | 2426 << 4 | 8,     // qps-ploc
            0x5fe << 16 | 2426 << 4 | 8,     // qps-ploca
            0x801 << 16 | 82 << 4 | 5,       // ar-iq
            0x803 << 16 | 331 << 4 | 14,     // ca-es-valencia
            0x804 << 16 | 3248 << 4 | 5,     // zh-cn
            0x807 << 16 | 423 << 4 | 5,      // de-ch
            0x809 << 16 | 668 << 4 | 5,      // en-gb
            0x80a << 16 | 1127 << 4 | 5,     // es-mx
            0x80c << 16 | 1260 << 4 | 5,     // fr-be
            0x810 << 16 | 1671 << 4 | 5,     // it-ch
            0x813 << 16 | 2200 << 4 | 5,     // nl-be
            0x814 << 16 | 2236 << 4 | 5,     // nn-no
            0x816 << 16 | 2398 << 4 | 5,     // pt-pt
            0x818 << 16 | 2483 << 4 | 5,     // ro-md
            0x819 << 16 | 2514 << 4 | 5,     // ru-md
            0x81a << 16 | 2793 << 4 | 10,    // sr-latn-cs
            0x81d << 16 | 2864 << 4 | 5,     // sv-fi
            0x820 << 16 | 3074 << 4 | 5,     // ur-in
            0x82c << 16 | 200 << 4 | 10,     // az-cyrl-az
            0x82e << 16 | 461 << 4 | 6,      // dsb-de
            0x832 << 16 | 2979 << 4 | 5,     // tn-bw
            0x83b << 16 | 2593 << 4 | 5,     // se-se
            0x83c << 16 | 1501 << 4 | 5,     // ga-ie
            0x83e << 16 | 2115 << 4 | 5,     // ms-bn
            0x843 << 16 | 3094 << 4 | 10,    // uz-cyrl-uz
            0x845 << 16 | 269 << 4 | 5,      // bn-bd
            0x846 << 16 | 2308 << 4 | 10,    // pa-arab-pk
            0x849 << 16 | 2911 << 4 | 5,     // ta-lk
            0x850 << 16 | 2078 << 4 | 10,    // mn-mong-cn
            0x859 << 16 | 2563 << 4 | 10,    // sd-arab-pk
            0x85d << 16 | 1696 << 4 | 10,    // iu-latn-ca
            0x85f << 16 | 3031 << 4 | 11,    // tzm-latn-dz
            0x860 << 16 | 1860 << 4 | 10,    // ks-deva-in
            0x861 << 16 | 2185 << 4 | 5,     // ne-in
            0x867 << 16 | 1213 << 4 | 10,    // ff-latn-sn
            0x86b << 16 | 2461 << 4 | 6,     // quz-ec
            0x873 << 16 | 2958 << 4 | 5,     // ti-er
            0x901 << 16 | 2413 << 4 | 13,    // qps-latn-x-sh
            0x9ff << 16 | 2435 << 4 | 9,     // qps-plocm
            0xc01 << 16 | 67 << 4 | 5,       // ar-eg
            0xc04 << 16 | 3299 << 4 | 5,     // zh-hk
            0xc07 << 16 | 413 << 4 | 5,      // de-at
            0xc09 << 16 | 553 << 4 | 5,      // en-au
            0xc0a << 16 | 1100 << 4 | 5,     // es-es
            0xc0c << 16 | 1285 << 4 | 5,     // fr-ca
            0xc1a << 16 | 2743 << 4 | 10,    // sr-cyrl-cs
            0xc3b << 16 | 2583 << 4 | 5,     // se-fi
            0xc50 << 16 | 2088 << 4 | 10,    // mn-mong-mn
            0xc51 << 16 | 484 << 4 | 5,      // dz-bt
            0xc6b << 16 | 2467 << 4 | 6,     // quz-pe
            0x1001 << 16 | 107 << 4 | 5,     // ar-ly
            0x1004 << 16 | 3335 << 4 | 5,    // zh-sg
            0x1007 << 16 | 450 << 4 | 5,     // de-lu
            0x1009 << 16 | 593 << 4 | 5,     // en-ca
            0x100a << 16 | 1117 << 4 | 5,    // es-gt
            0x100c << 16 | 1305 << 4 | 5,    // fr-ch
            0x101a << 16 | 1601 << 4 | 5,    // hr-ba
            0x103b << 16 | 2664 << 4 | 6,    // smj-no
            0x105f << 16 | 3053 << 4 | 11,   // tzm-tfng-ma
            0x1401 << 16 | 62 << 4 | 5,      // ar-dz
            0x1404 << 16 | 3311 << 4 | 5,    // zh-mo
            0x1407 << 16 | 445 << 4 | 5,     // de-li
            0x1409 << 16 | 863 << 4 | 5,     // en-nz
            0x140a << 16 | 1080 << 4 | 5,    // es-cr
            0x140c << 16 | 1370 << 4 | 5,    // fr-lu
            0x141a << 16 | 310 << 4 | 10,    // bs-latn-ba
            0x143b << 16 | 2670 << 4 | 6,    // smj-se
            0x1801 << 16 | 112 << 4 | 5,     // ar-ma
            0x1809 << 16 | 718 << 4 | 5,     // en-ie
            0x180a << 16 | 1137 << 4 | 5,    // es-pa
            0x180c << 16 | 1380 << 4 | 5,    // fr-mc
            0x181a << 16 | 2783 << 4 | 10,   // sr-latn-ba
            0x183b << 16 | 2652 << 4 | 6,    // sma-no
            0x1c01 << 16 | 167 << 4 | 5,     // ar-tn
            0x1c09 << 16 | 1028 << 4 | 5,    // en-za
            0x1c0a << 16 | 1090 << 4 | 5,    // es-do
            0x1c0c << 16 | 1254 << 4 | 6,    // fr-029
            0x1c1a << 16 | 2733 << 4 | 10,   // sr-cyrl-ba
            0x1c3b << 16 | 2658 << 4 | 6,    // sma-se
            0x2001 << 16 | 122 << 4 | 5,     // ar-om
            0x2009 << 16 | 748 << 4 | 5,     // en-jm
            0x200a << 16 | 1177 << 4 | 5,    // es-ve
            0x200c << 16 | 1435 << 4 | 5,    // fr-re
            0x201a << 16 | 300 << 4 | 10,    // bs-cyrl-ba
            0x203b << 16 | 2682 << 4 | 6,    // sms-fi
            0x2401 << 16 | 172 << 4 | 5,     // ar-ye
            0x2409 << 16 | 521 << 4 | 6,     // en-029
            0x240a << 16 | 1075 << 4 | 5,    // es-co
            0x240c << 16 | 1290 << 4 | 5,    // fr-cd
            0x241a << 16 | 2813 << 4 | 10,   // sr-latn-rs
            0x243b << 16 | 2676 << 4 | 6,    // smn-fi
            0x2801 << 16 | 157 << 4 | 5,     // ar-sy
            0x2809 << 16 | 588 << 4 | 5,     // en-bz
            0x280a << 16 | 1142 << 4 | 5,    // es-pe
            0x280c << 16 | 1450 << 4 | 5,    // fr-sn
            0x281a << 16 | 2763 << 4 | 10,   // sr-cyrl-rs
            0x2c01 << 16 | 87 << 4 | 5,      // ar-jo
            0x2c09 << 16 | 973 << 4 | 5,     // en-tt
            0x2c0a << 16 | 1055 << 4 | 5,    // es-ar
            0x2c0c << 16 | 1315 << 4 | 5,    // fr-cm
            0x2c1a << 16 | 2803 << 4 | 10,   // sr-latn-me
            0x3001 << 16 | 102 << 4 | 5,     // ar-lb
            0x3009 << 16 | 1038 << 4 | 5,    // en-zw
            0x300a << 16 | 1095 << 4 | 5,    // es-ec
            0x300c << 16 | 1310 << 4 | 5,    // fr-ci
            0x301a << 16 | 2753 << 4 | 10,   // sr-cyrl-me
            0x3401 << 16 | 97 << 4 | 5,      // ar-kw
            0x3409 << 16 | 873 << 4 | 5,     // en-ph
            0x340a << 16 | 1070 << 4 | 5,    // es-cl
            0x340c << 16 | 1395 << 4 | 5,    // fr-ml
            0x3801 << 16 | 47 << 4 | 5,      // ar-ae
            0x3809 << 16 | 713 << 4 | 5,     // en-id
            0x380a << 16 | 1172 << 4 | 5,    // es-uy
            0x380c << 16 | 1375 << 4 | 5,    // fr-ma
            0x3c01 << 16 | 52 << 4 | 5,      // ar-bh
            0x3c09 << 16 | 708 << 4 | 5,     // en-hk
            0x3c0a << 16 | 1157 << 4 | 5,    // es-py
            0x3c0c << 16 | 1360 << 4 | 5,    // fr-ht
            0x4001 << 16 | 132 << 4 | 5,     // ar-qa
            0x4009 << 16 | 733 << 4 | 5,     // en-in
            0x400a << 16 | 1060 << 4 | 5,    // es-bo
            0x4409 << 16 | 828 << 4 | 5,     // en-my
            0x440a << 16 | 1162 << 4 | 5,    // es-sv
            0x4809 << 16 | 923 << 4 | 5,     // en-sg
            0x480a << 16 | 1122 << 4 | 5,    // es-hn
            0x4c0a << 16 | 1132 << 4 | 5,    // es-ni
            0x500a << 16 | 1152 << 4 | 5,    // es-pr
            0x540a << 16 | 1167 << 4 | 5,    // es-us
            0x580a << 16 | 1049 << 4 | 6,    // es-419
            0x5c0a << 16 | 1085 << 4 | 5,    // es-cu
            0x641a << 16 | 300 << 4 | 7,     // bs-cyrl
            0x681a << 16 | 310 << 4 | 7,     // bs-latn
            0x6c1a << 16 | 2733 << 4 | 7,    // sr-cyrl
            0x701a << 16 | 2783 << 4 | 7,    // sr-latn
            0x703b << 16 | 2676 << 4 | 3,    // smn
            0x742c << 16 | 200 << 4 | 7,     // az-cyrl
            0x743b << 16 | 2682 << 4 | 3,    // sms
            0x7804 << 16 | 3236 << 4 | 2,    // zh
            0x7814 << 16 | 2236 << 4 | 2,    // nn
            0x781a << 16 | 300 << 4 | 2,     // bs
            0x782c << 16 | 210 << 4 | 7,     // az-latn
            0x783b << 16 | 2652 << 4 | 3,    // sma
            0x7843 << 16 | 3094 << 4 | 7,    // uz-cyrl
            0x7850 << 16 | 2066 << 4 | 7,    // mn-cyrl
            0x785d << 16 | 1686 << 4 | 7,    // iu-cans
            0x785f << 16 | 3053 << 4 | 8,    // tzm-tfng
            0x7c04 << 16 | 3292 << 4 | 7,    // zh-hant
            0x7c14 << 16 | 2158 << 4 | 2,    // nb
            0x7c1a << 16 | 2733 << 4 | 2,    // sr
            0x7c28 << 16 | 2943 << 4 | 7,    // tg-cyrl
            0x7c2e << 16 | 461 << 4 | 3,     // dsb
            0x7c3b << 16 | 2664 << 4 | 3,    // smj
            0x7c43 << 16 | 3104 << 4 | 7,    // uz-latn
            0x7c46 << 16 | 2308 << 4 | 7,    // pa-arab
            0x7c50 << 16 | 2078 << 4 | 7,    // mn-mong
            0x7c59 << 16 | 2563 << 4 | 7,    // sd-arab
            0x7c5c << 16 | 366 << 4 | 8,     // chr-cher
            0x7c5d << 16 | 1696 << 4 | 7,    // iu-latn
            0x7c5f << 16 | 3031 << 4 | 8,    // tzm-latn
            0x7c67 << 16 | 1213 << 4 | 7,    // ff-latn
            0x7c68 << 16 | 1555 << 4 | 7,    // ha-latn
            0x7c86 << 16 | 2444 << 4 | 8,    // quc-latn
            0x7c92 << 16 | 1888 << 4 | 7,    // ku-arab
            0x1007f << 16 | 3175 << 4 | 11,  // x-iv_mathan
            0x10407 << 16 | 428 << 4 | 5,    // de-de
            0x1040e << 16 | 1617 << 4 | 5,   // hu-hu
            0x10437 << 16 | 1750 << 4 | 5,   // ka-ge
            0x20804 << 16 | 3248 << 4 | 5,   // zh-cn
            0x21004 << 16 | 3335 << 4 | 5,   // zh-sg
            0x21404 << 16 | 3311 << 4 | 5,   // zh-mo
            0x30404 << 16 | 3359 << 4 | 5,   // zh-tw
            0x40404 << 16 | 3359 << 4 | 5,   // zh-tw
            0x40411 << 16 | 1706 << 4 | 5,   // ja-jp
            0x40c04 << 16 | 3299 << 4 | 5,   // zh-hk
            0x41404 << 16 | 3311 << 4 | 5,   // zh-mo
            0x50804 << 16 | 3248 << 4 | 5,   // zh-cn
            0x51004 << 16 | 3335 << 4 | 5,   // zh-sg
        };

        internal static string? LCIDToLocaleName(int culture)
        {
            int lo = 0;
            int hi = s_lcidToCultureNameIndices.Length - 1;

            // Binary search the array
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);

                int index = s_lcidToCultureNameIndices[i];
                int array_value = index >> 16;
                int order = array_value.CompareTo(culture);

                if (order == 0)
                {
                    return GetString(CultureNames.Slice((index >> 4) & 0xFFF, index & 0xF));
                }

                if (order < 0)
                {
                    lo = i + 1;
                }
                else
                {
                    hi = i - 1;
                }
            }

            return null;
        }

        internal static int GetLocaleDataNumericPart(string cultureName, IcuLocaleDataParts part)
        {
            int index = SearchCultureName(cultureName);
            if (index < 0)
            {
                return -1;
            }

            return s_nameIndexToNumericData[index * NUMERIC_LOCALE_DATA_COUNT_PER_ROW + (int) part];
        }

        internal static string? GetThreeLetterWindowsLanguageName(string cultureName)
        {
            int index = SearchCultureName(cultureName);
            if (index < 0)
            {
                return null;
            }

            Debug.Assert(CulturesCount == (c_threeLetterWindowsLanguageName.Length / 3));
            return c_threeLetterWindowsLanguageName.Substring(index * 3, 3);
        }

        private static string GetLocaleDataMappedCulture(string cultureName, IcuLocaleDataParts part)
        {
            int indexToIndicesTable = GetLocaleDataNumericPart(cultureName, part);
            if (indexToIndicesTable < 0)
            {
                return ""; // fallback to invariant
            }

            return GetString(GetCultureName(indexToIndicesTable));
        }

        internal static string GetSpecificCultureName(string cultureName)
        {
            return GetLocaleDataMappedCulture(cultureName, IcuLocaleDataParts.SpecificLocaleIndex);
        }

        internal static string GetConsoleUICulture(string cultureName)
        {
            return GetLocaleDataMappedCulture(cultureName, IcuLocaleDataParts.ConsoleLocaleIndex);
        }

        // Returns index of the culture or -1 if it fail finding any match
        private static int SearchCultureName(string name)
        {
            if (name.Length > LocaleLongestName)
                return -1;

            Span<byte> lower_case = stackalloc byte[name.Length];
            for (int i = 0; i < name.Length; ++i)
            {
                char ch = name[i];
                if (ch > 'z')
                    return -1;

                lower_case[i] = (byte)(ch | 0x20);
            }

            ReadOnlySpan<byte> lname = lower_case;

            int lo = 0;
            int hi = s_localesNamesIndexes.Length - 1;

            // Binary search the array
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);

                int order = GetCultureName(i).SequenceCompareTo(lname);

                if (order == 0) return i;
                if (order < 0)
                {
                    lo = i + 1;
                }
                else
                {
                    hi = i - 1;
                }
            }

            return ~lo;
        }

        private static ReadOnlySpan<byte> GetCultureName(int localeNameIndice)
        {
            ushort index = s_localesNamesIndexes[localeNameIndice];
            return CultureNames.Slice(index >> 4, index & 0xF);
        }

        private static string GetString(ReadOnlySpan<byte> buffer)
        {
            string result = string.FastAllocateString(buffer.Length);
            var s = new Span<char>(ref result.GetRawStringData(), buffer.Length);
            for (int i = 0; i < buffer.Length; i++)
            {
                s[i] = (char)buffer[i];
            }

            return result;
        }
    }
}
